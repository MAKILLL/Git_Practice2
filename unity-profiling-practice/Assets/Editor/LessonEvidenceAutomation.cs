using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LessonEvidenceAutomation
{
    private const string ScenePath = "Assets/Scenes/ProfilingLab.unity";
    private static string commandPath = string.Empty;
    private static string statePath = string.Empty;
    private static string evidenceDirectory = string.Empty;

    public static void Start()
    {
        commandPath = ReadPathArgument("-lessonCommandPath");
        statePath = ReadPathArgument("-lessonStatePath");
        evidenceDirectory = ReadPathArgument("-lessonEvidencePath");
        var captureStorage = ReadPathArgument("-lessonCaptureStorage");
        var captureStorageSetting = ReadArgument("-lessonCaptureStorageSetting");
        Directory.CreateDirectory(evidenceDirectory);
        Directory.CreateDirectory(captureStorage);
        EditorPrefs.SetInt("Profiler.FrameCount", 2000);
        EditorPrefs.SetString("Profiler.CaptureStoragePath", captureStorageSetting);
        EditorApplication.update -= ProcessCommand;
        EditorApplication.update += ProcessCommand;
        OpenSceneWorkspace();
        WriteState("ready", "start", $"frameCount={EditorPrefs.GetInt("Profiler.FrameCount")}; captureStorage={captureStorage}");
    }

    private static void ProcessCommand()
    {
        if (!File.Exists(commandPath))
        {
            return;
        }

        var command = File.ReadAllText(commandPath).Trim();
        File.Delete(commandPath);
        if (command.Length == 0)
        {
            return;
        }

        try
        {
            Execute(command);
        }
        catch (Exception exception)
        {
            WriteState("error", command, exception.ToString());
        }
    }

    private static void Execute(string command)
    {
        switch (command)
        {
            case "scene":
                OpenSceneWorkspace();
                WriteState("ready", command, $"scene={ScenePath}; hierarchy=visible; project=visible");
                break;
            case "build_profiles":
                LessonEvidenceWindows.OpenBuildProfiles();
                WriteState("ready", command, "sceneList=ProfilingLab; developmentBuild=true; autoconnectProfiler=true");
                break;
            case "preferences":
                EditorPrefs.SetInt("Profiler.FrameCount", 2000);
                SettingsService.OpenUserPreferences("Preferences/Analysis/Profiler");
                WriteState("ready", command, "path=Preferences/Analysis/Profiler; frameCount=2000");
                break;
            case "profiler_live":
                WriteState("ready", command, LessonProfilerEvidence.OpenLive(evidenceDirectory));
                break;
            case "profiler_stop":
                WriteState("ready", command, LessonProfilerEvidence.StopRecording());
                break;
            case "save":
                WriteState("ready", command, LessonProfilerEvidence.SaveCapture(evidenceDirectory));
                break;
            case "import":
                WriteState("dialog", command, "title=Import Profiler Capture; filter=.data,.raw");
                LessonProfilerEvidence.OpenImportDialog(evidenceDirectory);
                WriteState("ready", command, "import completed; capturesList refreshed");
                break;
            case "import_confirmed":
                WriteState("ready", command, LessonProfilerEvidence.ImportCapture(evidenceDirectory));
                break;
            case "hierarchy_spike":
            case "hierarchy_filter":
            case "timeline_overview":
            case "timeline_selected":
            case "timeline_normal":
                WriteState("ready", command, LessonProfilerEvidence.ConfigureCpu(command, evidenceDirectory));
                break;
            case "memory":
                WriteState("ready", command, LessonProfilerEvidence.SelectMemory());
                break;
            case "gpu":
                WriteState("ready", command, LessonProfilerEvidence.SelectGpu());
                break;
            default:
                throw new InvalidOperationException($"Unknown evidence command: {command}");
        }
    }

    private static void OpenSceneWorkspace()
    {
        CloseAuxiliaryWindows();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
        EditorApplication.ExecuteMenuItem("Window/General/Project");
        SceneView.lastActiveSceneView?.Focus();
    }

    private static void CloseAuxiliaryWindows()
    {
        foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            var title = window.titleContent.text;
            if (title == "Profiler" || title == "Build Profiles" || title == "Preferences")
            {
                window.Close();
            }
        }
    }

    private static void WriteState(string status, string command, string details)
    {
        File.WriteAllText(statePath, $"{status}|{command}|{DateTime.UtcNow:O}|{details}");
        Debug.Log($"[LessonEvidence] {status}: {command}; {details}");
    }

    private static string ReadPathArgument(string name)
    {
        var arguments = Environment.GetCommandLineArgs();
        for (var index = 0; index < arguments.Length - 1; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.Ordinal))
            {
                return Path.GetFullPath(arguments[index + 1]);
            }
        }

        throw new ArgumentException($"Missing command-line argument: {name}");
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

        throw new ArgumentException($"Missing command-line argument: {name}");
    }
}
