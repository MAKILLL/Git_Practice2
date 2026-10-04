using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;

internal static class LessonProfilerEvidence
{
    private const string MarkerName = "Lesson.RebuildRoutes";
    private static readonly Type ProfilerDriverType = typeof(Editor).Assembly.GetType("UnityEditorInternal.ProfilerDriver", true);

    internal static string OpenLive(string evidenceDirectory)
    {
        var window = OpenProfiler();
        InvokeWindow(window, "SetRecordingEnabled", true);
        var targetId = (int)ProfilerDriverType.GetProperty("connectedProfiler", BindingFlags.Static | BindingFlags.Public)!.GetValue(null);
        var targetName = (string)ProfilerDriverType.GetMethod("GetConnectionIdentifier", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, new object[] { targetId });
        var value = $"targetPlayer={targetName}; targetId={targetId}; recording={ReadDriverBool("enabled")}; frameRange={window.firstAvailableFrameIndex}..{window.lastAvailableFrameIndex}";
        File.WriteAllText(Path.Combine(evidenceDirectory, "live_connection.txt"), value + Environment.NewLine);
        return value;
    }

    internal static string StopRecording()
    {
        var window = OpenProfiler();
        InvokeWindow(window, "SetRecordingEnabled", false);
        return $"recording={ReadDriverBool("enabled")}; frameRange={window.firstAvailableFrameIndex}..{window.lastAvailableFrameIndex}";
    }

    internal static string SaveCapture(string evidenceDirectory)
    {
        var window = OpenProfiler();
        InvokeWindow(window, "SaveProfilingData");
        var captureDirectory = ReadArgument("-lessonCaptureStorage");
        var saved = Directory.GetFiles(captureDirectory, "*.data").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault() ?? string.Empty;
        var value = $"menu=Profiler toolbar/Save current profiling information; saved={saved}; capturesList=visible";
        File.WriteAllText(Path.Combine(evidenceDirectory, "save_import.txt"), value + Environment.NewLine);
        return value;
    }

    internal static void OpenImportDialog(string evidenceDirectory)
    {
        var window = OpenProfiler();
        var field = typeof(ProfilerWindow).GetField("m_CapturesListViewController", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var controller = field.GetValue(window);
        File.AppendAllText(Path.Combine(evidenceDirectory, "save_import.txt"), "menu=Profiler/Captures List/Import; dialog=Import Profiler Capture" + Environment.NewLine);
        controller!.GetType().GetMethod("ImportCapture", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(controller, null);
    }

    internal static string ImportCapture(string evidenceDirectory)
    {
        var importPath = ReadArgument("-lessonImportPath");
        var profilerUserSettings = typeof(Editor).Assembly.GetType("UnityEditor.Profiling.ProfilerUserSettings", true);
        profilerUserSettings.GetProperty("LastImportPath", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(null, importPath);
        var window = OpenProfiler();
        var loadProfile = ProfilerDriverType.GetMethod("LoadProfile", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(bool) }, null)!;
        loadProfile.Invoke(null, new object[] { importPath, false });
        var value = $"imported={importPath}; source=Captures List/Import Profiler Capture; loadedFrames={window.firstAvailableFrameIndex}..{window.lastAvailableFrameIndex}";
        File.AppendAllText(Path.Combine(evidenceDirectory, "save_import.txt"), value + Environment.NewLine);
        window.Repaint();
        return value;
    }

    internal static string ConfigureCpu(string mode, string evidenceDirectory)
    {
        var window = OpenProfiler();
        var marker = FindMarker(window);
        var module = SelectModule(window, "UnityEditorInternal.Profiling.CPUProfilerModule");
        SetCpuView(module, mode.StartsWith("timeline", StringComparison.Ordinal) ? "Timeline" : "Hierarchy");
        var controller = window.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
        controller.sampleNameSearchFilter = mode == "hierarchy_filter" ? MarkerName : string.Empty;

        if (mode == "timeline_overview")
        {
            window.selectedFrameIndex = marker.FrameIndex;
            controller.ClearSelection();
        }
        else if (mode == "timeline_normal")
        {
            window.selectedFrameIndex = Math.Max(window.firstAvailableFrameIndex, marker.FrameIndex - 1);
            controller.ClearSelection();
        }
        else
        {
            window.selectedFrameIndex = marker.FrameIndex;
            controller.SetSelection(new ProfilerTimeSampleSelection(marker.FrameIndex, string.Empty, marker.ThreadName, marker.ThreadId, marker.SampleIndex, MarkerName));
        }

        window.Repaint();
        var value = $"mode={mode}; markerFrame={marker.FrameIndex}; uiFrame={marker.FrameIndex + 1}; selectedFrame={window.selectedFrameIndex}; durationMs={marker.DurationMilliseconds:F3}; sample={marker.SampleIndex}; thread={marker.ThreadName}";
        File.AppendAllText(Path.Combine(evidenceDirectory, "selected_frames.txt"), value + Environment.NewLine);
        return value;
    }

    internal static string SelectMemory()
    {
        var window = OpenProfiler();
        SelectModule(window, "UnityEditorInternal.Profiling.MemoryProfilerModule");
        window.Repaint();
        return $"module={window.selectedModuleIdentifier}; frame={window.selectedFrameIndex}";
    }

    internal static string SelectGpu()
    {
        var window = OpenProfiler();
        SelectModule(window, "UnityEditorInternal.Profiling.GPUProfilerModule");
        window.Repaint();
        return $"module={window.selectedModuleIdentifier}; frame={window.selectedFrameIndex}; gpuFrameTime=unavailable-in-capture";
    }

    internal static ProfilerWindow OpenProfiler()
    {
        var window = EditorWindow.GetWindow<ProfilerWindow>(false, "Profiler", true);
        window.position = new Rect(0f, 0f, 1500f, 820f);
        window.Show();
        window.Focus();
        return window;
    }

    private static object SelectModule(ProfilerWindow window, string moduleTypeName)
    {
        var profilerWindowType = typeof(ProfilerWindow);
        var moduleType = typeof(Editor).Assembly.GetType(moduleTypeName, true);
        var module = profilerWindowType.GetMethod("GetProfilerModuleByType", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { moduleType });
        profilerWindowType.GetProperty("selectedModule", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, module);
        return module!;
    }

    private static void SetCpuView(object module, string viewName)
    {
        var viewType = typeof(Editor).Assembly.GetType("UnityEditorInternal.ProfilerViewType", true);
        module.GetType().GetMethod("CPUViewTypeChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(module, new[] { Enum.Parse(viewType, viewName) });
    }

    private static MarkerSample FindMarker(ProfilerWindow window)
    {
        var getRawFrame = ProfilerDriverType.GetMethod("GetRawFrameDataView", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int), typeof(int) }, null)!;
        for (var frame = window.lastAvailableFrameIndex; frame >= window.firstAvailableFrameIndex; frame--)
        {
            using var view = (RawFrameDataView)getRawFrame.Invoke(null, new object[] { (int)frame, 0 });
            if (!view.valid)
            {
                continue;
            }

            for (var sample = 0; sample < view.sampleCount; sample++)
            {
                if (string.Equals(view.GetSampleName(sample), MarkerName, StringComparison.Ordinal))
                {
                    return new MarkerSample(frame, sample, view.threadName, view.threadId, view.GetSampleTimeMs(sample));
                }
            }
        }

        throw new InvalidDataException($"Marker '{MarkerName}' was not found in frames {window.firstAvailableFrameIndex}..{window.lastAvailableFrameIndex}.");
    }

    private static void InvokeWindow(ProfilerWindow window, string method, params object[] arguments) =>
        typeof(ProfilerWindow).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, arguments);

    private static bool ReadDriverBool(string propertyName) =>
        (bool)ProfilerDriverType.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public)!.GetValue(null);

    private static string ReadArgument(string name)
    {
        var arguments = Environment.GetCommandLineArgs();
        for (var index = 0; index < arguments.Length - 1; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.Ordinal))
            {
                return Path.GetFullPath(arguments[index + 1]);
            }
        }

        return string.Empty;
    }

    private readonly struct MarkerSample
    {
        internal MarkerSample(long frameIndex, int sampleIndex, string threadName, ulong threadId, double durationMilliseconds)
        {
            FrameIndex = frameIndex;
            SampleIndex = sampleIndex;
            ThreadName = threadName;
            ThreadId = threadId;
            DurationMilliseconds = durationMilliseconds;
        }

        internal long FrameIndex { get; }
        internal int SampleIndex { get; }
        internal string ThreadName { get; }
        internal ulong ThreadId { get; }
        internal double DurationMilliseconds { get; }
    }
}
