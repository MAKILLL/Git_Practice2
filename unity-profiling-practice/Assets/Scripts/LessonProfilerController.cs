using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

public sealed class LessonProfilerController : MonoBehaviour
{
    private const int SpikeIntervalFrames = 120;
    private const int RouteCount = 80;
    private static readonly ProfilerMarker RebuildRoutesMarker = new("Lesson.RebuildRoutes");

    private readonly RouteField routeField = new();
    private readonly Stopwatch stopwatch = new();
    private string capturePath = string.Empty;
    private string screenshotPath = string.Empty;
    private string reportPath = string.Empty;
    private float autoQuitSeconds;
    private double lastSpikeMilliseconds;
    private double totalSpikeMilliseconds;
    private int spikeCount;
    private int checksum;
    private bool screenshotTaken;
    private bool finishing;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        capturePath = ReadPathArgument("-lessonCapturePath");
        screenshotPath = ReadPathArgument("-lessonScreenshotPath");
        reportPath = ReadPathArgument("-lessonReportPath");
        float.TryParse(ReadArgument("-lessonAutoQuitSeconds"), NumberStyles.Float, CultureInfo.InvariantCulture, out autoQuitSeconds);

        if (!string.IsNullOrEmpty(capturePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(capturePath)!);
            Profiler.logFile = capturePath;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
            Debug.Log($"[Lesson] Native profiler capture started: {capturePath}");
        }
    }

    private void Update()
    {
        if (Time.frameCount % SpikeIntervalFrames == 0)
        {
            stopwatch.Restart();
            using (RebuildRoutesMarker.Auto())
            {
                checksum = routeField.RebuildRoutes(RouteCount, 20260916 + spikeCount);
            }

            stopwatch.Stop();
            lastSpikeMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            totalSpikeMilliseconds += lastSpikeMilliseconds;
            spikeCount++;
            Debug.Log($"[Lesson] Route rebuild #{spikeCount}: {lastSpikeMilliseconds:F1} ms, checksum {checksum}");
        }

        if (!screenshotTaken && !string.IsNullOrEmpty(screenshotPath) && Time.unscaledTime >= 3f)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
            ScreenCapture.CaptureScreenshot(screenshotPath);
            screenshotTaken = true;
        }

        if (!finishing && ((autoQuitSeconds > 0f && Time.unscaledTime >= autoQuitSeconds) ||
                          (!string.IsNullOrEmpty(capturePath) && spikeCount >= 5)))
        {
            finishing = true;
            FinishRun();
        }
    }

    private void OnGUI()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 17 };
        }

        var panel = new Rect(24f, 24f, 510f, 164f);
        GUI.Box(panel, string.Empty);
        GUI.Label(new Rect(44f, 38f, 470f, 34f), "UNITY PROFILING LAB", titleStyle);
        GUI.Label(new Rect(44f, 76f, 470f, 28f), "Baseline: smooth 60 FPS between route updates", textStyle);
        GUI.Label(new Rect(44f, 104f, 470f, 28f), $"CPU spike every 120 frames: {lastSpikeMilliseconds:F1} ms", textStyle);
        GUI.Label(new Rect(44f, 132f, 470f, 28f), "Profiler marker: Lesson.RebuildRoutes", textStyle);
        GUI.Label(new Rect(44f, 160f, 470f, 22f), $"Completed spikes: {spikeCount}   Checksum: {checksum}", textStyle);
    }

    private void FinishRun()
    {
        if (!string.IsNullOrEmpty(capturePath))
        {
            Profiler.enableBinaryLog = false;
            Profiler.enabled = false;
        }

        if (!string.IsNullOrEmpty(reportPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            var average = spikeCount == 0 ? 0d : totalSpikeMilliseconds / spikeCount;
            File.WriteAllText(reportPath,
                $"Unity: {Application.unityVersion}{Environment.NewLine}" +
                $"Platform: {SystemInfo.operatingSystem}{Environment.NewLine}" +
                $"CPU: {SystemInfo.processorType}{Environment.NewLine}" +
                $"GPU: {SystemInfo.graphicsDeviceName}{Environment.NewLine}" +
                $"Resolution: {Screen.width}x{Screen.height}{Environment.NewLine}" +
                $"Spike interval: {SpikeIntervalFrames} frames{Environment.NewLine}" +
                $"Routes per spike: {RouteCount}{Environment.NewLine}" +
                $"Spikes completed: {spikeCount}{Environment.NewLine}" +
                $"Average measured spike: {average:F2} ms{Environment.NewLine}" +
                $"Last checksum: {checksum}{Environment.NewLine}");
        }

        Application.Quit(0);
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

    private static string ReadPathArgument(string name)
    {
        var value = ReadArgument(name);
        return string.IsNullOrEmpty(value) ? string.Empty : Path.GetFullPath(value);
    }
}
