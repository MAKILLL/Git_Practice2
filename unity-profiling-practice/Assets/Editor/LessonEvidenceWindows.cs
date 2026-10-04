using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class LessonEvidenceWindows
{
    public static void OpenBuildProfiles()
    {
        EditorUserBuildSettings.development = true;
        EditorUserBuildSettings.connectProfiler = true;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
        var type = Type.GetType("UnityEditor.Build.Profile.BuildProfileWindow, UnityEditor.BuildProfileModule");
        if (type == null)
        {
            EditorApplication.ExecuteMenuItem("File/Build Profiles...");
        }
        else
        {
            var window = EditorWindow.GetWindow(type, false, "Build Profiles", true);
            window.position = new Rect(20f, 20f, 1000f, 700f);
            window.Show();
            window.Focus();
        }

        Debug.Log("[Lesson] Build Profiles opened");
    }
}
