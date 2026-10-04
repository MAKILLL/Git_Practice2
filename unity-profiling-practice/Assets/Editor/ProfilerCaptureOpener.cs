using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;

public static class ProfilerCaptureOpener
{
    private const string MarkerName = "Lesson.RebuildRoutes";

    public static void OpenSuppliedCapture()
    {
        var capturePath = Path.GetFullPath(ReadArgument("-lessonCapturePath"));
        if (!File.Exists(capturePath))
        {
            throw new FileNotFoundException("Profiler capture was not found.", capturePath);
        }

        var profilerDriver = typeof(Editor).Assembly.GetType("UnityEditorInternal.ProfilerDriver", true);
        profilerDriver.GetMethod(
            "LoadProfile",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(bool) },
            null)!.Invoke(null, new object[] { capturePath, false });

        var window = EditorWindow.GetWindow<ProfilerWindow>("Profiler");
        var requestedView = ReadArgument("-lessonProfilerView");
        var preferredFrame = string.Equals(requestedView, "Timeline", StringComparison.Ordinal) ? 360L : 120L;
        var marker = FindMarkerFrame(profilerDriver, window.firstAvailableFrameIndex, window.lastAvailableFrameIndex, preferredFrame);
        window.selectedFrameIndex = marker.FrameIndex;
        window.position = new Rect(20f, 20f, 1000f, 700f);
        window.Show();
        window.Focus();
        WriteEvidence(marker);
        EditorApplication.delayCall += () => ConfigureView(window, requestedView, marker, capturePath);
    }

    private static void ConfigureView(ProfilerWindow window, string requestedView, MarkerSample marker, string capturePath)
    {
        var profilerWindowType = typeof(ProfilerWindow);
        var getModule = profilerWindowType.GetMethod("GetProfilerModuleByType", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var moduleTypeName = string.Equals(requestedView, "Memory", StringComparison.Ordinal)
            ? "UnityEditorInternal.Profiling.MemoryProfilerModule"
            : "UnityEditorInternal.Profiling.CPUProfilerModule";
        var moduleType = typeof(Editor).Assembly.GetType(moduleTypeName, true);
        var module = getModule.Invoke(window, new object[] { moduleType });
        profilerWindowType.GetProperty("selectedModule", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, module);

        if (string.Equals(requestedView, "Timeline", StringComparison.Ordinal))
        {
            var viewType = typeof(Editor).Assembly.GetType("UnityEditorInternal.ProfilerViewType", true);
            var timeline = Enum.Parse(viewType, "Timeline");
            moduleType.GetMethod("CPUViewTypeChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(module, new[] { timeline });
        }

        if (!string.Equals(requestedView, "Memory", StringComparison.Ordinal))
        {
            var controller = window.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
            controller.SetSelection(new ProfilerTimeSampleSelection(
                marker.FrameIndex,
                string.Empty,
                marker.ThreadName,
                marker.ThreadId,
                marker.SampleIndex,
                MarkerName));
        }
        window.Repaint();
        EditorApplication.delayCall += () =>
        {
            window.Repaint();
            var selectedIdentifier = window.selectedModuleIdentifier;
            var viewField = moduleType.BaseType!.GetField("m_ViewType", BindingFlags.Instance | BindingFlags.NonPublic);
            var actualView = viewField?.GetValue(module)?.ToString() ?? "Simple";
            Debug.Log($"[Lesson] Profiler ready: view={requestedView}; selectedModule={selectedIdentifier}; actualView={actualView}; capture={capturePath}; marker frame {marker.FrameIndex}, sample {marker.SampleIndex}, {marker.DurationMilliseconds:F2} ms");
        };
    }

    private static MarkerSample FindMarkerFrame(Type profilerDriver, long firstFrame, long lastFrame, long preferredFrame)
    {
        var getRawFrameDataView = profilerDriver.GetMethod(
            "GetRawFrameDataView",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(int), typeof(int) },
            null) ?? throw new MissingMethodException("ProfilerDriver.GetRawFrameDataView(int, int)");

        for (var offset = 0L; offset <= lastFrame - firstFrame; offset++)
        {
            var frame = preferredFrame + offset;
            if (frame > lastFrame)
            {
                frame = firstFrame + offset - (lastFrame - preferredFrame + 1);
            }
            using var view = (RawFrameDataView)getRawFrameDataView.Invoke(null, new object[] { (int)frame, 0 });
            if (!view.valid)
            {
                continue;
            }

            for (var sample = 0; sample < view.sampleCount; sample++)
            {
                if (string.Equals(view.GetSampleName(sample), MarkerName, StringComparison.Ordinal))
                {
                    return new MarkerSample(frame, sample, view.threadName, view.threadId, view.GetSampleStartTimeMs(sample), view.GetSampleTimeMs(sample));
                }
            }
        }

        throw new InvalidDataException($"Marker '{MarkerName}' was not found in frames {firstFrame}..{lastFrame}.");
    }

    private static void WriteEvidence(MarkerSample marker)
    {
        var evidencePath = ReadArgument("-lessonEvidencePath");
        if (string.IsNullOrEmpty(evidencePath))
        {
            return;
        }

        evidencePath = Path.GetFullPath(evidencePath);
        Directory.CreateDirectory(Path.GetDirectoryName(evidencePath)!);
        File.WriteAllText(evidencePath,
            $"Marker: {MarkerName}{Environment.NewLine}" +
            $"Frame index: {marker.FrameIndex}{Environment.NewLine}" +
            $"Profiler UI frame: {marker.FrameIndex + 1}{Environment.NewLine}" +
            $"Thread: {marker.ThreadName}{Environment.NewLine}" +
            $"Thread ID: {marker.ThreadId}{Environment.NewLine}" +
            $"Raw sample index: {marker.SampleIndex}{Environment.NewLine}" +
            $"Start: {marker.StartMilliseconds:F3} ms{Environment.NewLine}" +
            $"Duration: {marker.DurationMilliseconds:F3} ms{Environment.NewLine}");
    }

    private static string ReadArgument(string name)
    {
        var arguments = Environment.GetCommandLineArgs();
        for (var index = 0; index < arguments.Length - 1; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.Ordinal))
            {
                return arguments[index + 1];
            }
        }

        return string.Empty;
    }

    private readonly struct MarkerSample
    {
        public MarkerSample(long frameIndex, int sampleIndex, string threadName, ulong threadId, double startMilliseconds, double durationMilliseconds)
        {
            FrameIndex = frameIndex;
            SampleIndex = sampleIndex;
            ThreadName = threadName;
            ThreadId = threadId;
            StartMilliseconds = startMilliseconds;
            DurationMilliseconds = durationMilliseconds;
        }

        public long FrameIndex { get; }
        public int SampleIndex { get; }
        public string ThreadName { get; }
        public ulong ThreadId { get; }
        public double StartMilliseconds { get; }
        public double DurationMilliseconds { get; }
    }
}
