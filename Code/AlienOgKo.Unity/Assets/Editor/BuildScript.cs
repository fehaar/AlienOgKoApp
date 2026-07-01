using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    const string AndroidPlayerRoot =
        "/home/fehaar/Unity/Hub/Editor/6000.5.1f1/Editor/Data/PlaybackEngines/AndroidPlayer";

    [MenuItem("AlienOgKo/Build Android APK")]
    public static void BuildAndroid()
    {
        ConfigureAndroid();

        string outputPath = Path.GetFullPath("Builds/Android/AlienOgKo.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/bounce.unity" },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"[BuildScript] APK built → {outputPath} ({report.summary.totalSize / 1024 / 1024} MB)");
        else
            Debug.LogError($"[BuildScript] Build failed — {report.summary.totalErrors} error(s)");

        if (Application.isBatchMode)
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static void ConfigureAndroid()
    {
        EditorPrefs.SetString("AndroidSdkRoot",     AndroidPlayerRoot + "/SDK");
        EditorPrefs.SetString("AndroidNdkRootR23b", AndroidPlayerRoot + "/NDK");
        EditorPrefs.SetString("JdkPath",            AndroidPlayerRoot + "/OpenJDK");

        PlayerSettings.productName           = "AlienOgKo";
        PlayerSettings.applicationIdentifier = "dk.gosuman.alienogko";
        PlayerSettings.bundleVersion         = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
    }
}
