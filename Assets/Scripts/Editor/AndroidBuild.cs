#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidBuild
{
    private const string OutputDirectory = "Builds/Android";

    [MenuItem("MafiaUnity/Android/Build APK")]
    public static void BuildApk()
    {
        Directory.CreateDirectory(OutputDirectory);

        PlayerSettings.productName = "MafiaUnity Android";
        PlayerSettings.companyName = "MafiaUnity Team";
        PlayerSettings.applicationIdentifier = "com.mafiaunity.android";
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

        var scenes = new[] { "Assets/Scenes/Game.unity" };
        var output = Path.Combine(OutputDirectory, "MafiaUnity-Android.apk");

        var report = BuildPipeline.BuildPlayer(
            scenes,
            output,
            BuildTarget.Android,
            BuildOptions.None);

        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Android build failed: " + report.summary.result);

        Debug.Log("MafiaUnity Android APK created: " + output);
    }
}
#endif
