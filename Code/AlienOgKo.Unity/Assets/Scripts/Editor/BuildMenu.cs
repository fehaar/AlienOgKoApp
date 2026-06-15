using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildMenu
{
    const string ScenePath = "Assets/Scenes/map.unity";
    const string OutputDir  = "Builds/Android";
    const string ApkName    = "AlienOgKo.apk";

    [MenuItem("AlienOgKo/Build Android")]
    static void BuildAndroid()
    {
        Directory.CreateDirectory(OutputDir);

        var options = new BuildPlayerOptions
        {
            scenes           = new[] { ScenePath },
            locationPathName = Path.Combine(OutputDir, ApkName),
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        var report  = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"Android build succeeded: {summary.totalSize / 1024 / 1024} MB  →  {options.locationPathName}");
        else
            Debug.LogError($"Android build failed after {summary.totalErrors} error(s).");
    }
}
