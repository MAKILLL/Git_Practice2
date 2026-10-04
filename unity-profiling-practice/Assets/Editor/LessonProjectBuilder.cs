using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class LessonProjectBuilder
{
    private const string ScenePath = "Assets/Scenes/ProfilingLab.unity";

    public static void BuildCapturePlayer()
    {
        BuildPlayer(BuildOptions.Development);
    }

    public static void BuildAll()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Materials");
        var floorMaterial = CreateMaterial("Floor", new Color(0.055f, 0.08f, 0.12f), 0.15f);
        var routeMaterial = CreateMaterial("Route", new Color(0.08f, 0.72f, 0.78f), 0.55f);
        var agentMaterial = CreateMaterial("Agent", new Color(1f, 0.45f, 0.12f), 0.45f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.31f, 0.36f, 0.44f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.025f, 0.04f, 0.07f);
        RenderSettings.fogDensity = 0.012f;

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.018f, 0.03f, 0.055f);
        camera.fieldOfView = 48f;
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 19f, -24f), Quaternion.Euler(31f, 0f, 0f));

        var lightObject = new GameObject("Key Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(0.78f, 0.88f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Profiling Grid";
        floor.transform.localScale = new Vector3(2.8f, 1f, 2.1f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

        var routes = new GameObject("Visible Route Nodes");
        for (var index = 0; index < 36; index++)
        {
            var angle = index * Mathf.PI * 2f / 36f;
            var node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            node.name = $"RouteNode_{index:00}";
            node.transform.SetParent(routes.transform);
            node.transform.position = new Vector3(Mathf.Cos(angle) * 9f, 0.08f, Mathf.Sin(angle * 1.37f) * 5.6f);
            node.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);
            node.GetComponent<Renderer>().sharedMaterial = routeMaterial;
            Object.DestroyImmediate(node.GetComponent<Collider>());
        }

        var agents = new GameObject("Traffic Agents");
        for (var index = 0; index < 48; index++)
        {
            var agent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            agent.name = $"Agent_{index:00}";
            agent.transform.SetParent(agents.transform);
            agent.transform.localScale = new Vector3(0.22f, 0.32f, 0.22f);
            agent.GetComponent<Renderer>().sharedMaterial = agentMaterial;
            Object.DestroyImmediate(agent.GetComponent<Collider>());
            agent.AddComponent<TrafficAgent>().Configure(4.5f + index % 6, 0.22f + index % 5 * 0.035f, index * 0.41f);
        }

        new GameObject("Lesson Profiler Controller").AddComponent<LessonProfilerController>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.companyName = "Mire Education";
        PlayerSettings.productName = "Unity Profiling Lab";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.enableFrameTimingStats = true;
        QualitySettings.vSyncCount = 0;
        AssetDatabase.SaveAssets();

        BuildPlayer(BuildOptions.Development | BuildOptions.ConnectWithProfiler);
    }

    private static void BuildPlayer(BuildOptions buildOptions)
    {
        Directory.CreateDirectory("Build");
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.GetFullPath("Build/UnityProfilingLab.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = buildOptions
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new BuildFailedException($"Build failed: {report.summary.result}");
        }

        Debug.Log($"[LessonProjectBuilder] Development Build created: {options.locationPathName}; options: {buildOptions}");
    }

    private static Material CreateMaterial(string name, Color color, float metallic)
    {
        var path = $"Assets/Materials/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", 0.68f);
        EditorUtility.SetDirty(material);
        return material;
    }
}
