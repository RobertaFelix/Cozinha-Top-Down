#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

// Also runs before a player build. No console component is attached to a scene/prefab.
public sealed class DevConsoleSafetyChecks : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;
    public void OnPreprocessBuild(BuildReport report) { VerifyPlayerExclusion(); }

    [MenuItem("UnderGrub/Developer Console Checks/Check player exclusion")]
    public static void VerifyPlayerExclusion()
    {
        var player = CompilationPipeline.GetAssemblies(AssembliesType.Player);
        foreach (var assembly in player)
            if (assembly.sourceFiles.Any(IsConsoleSource))
                throw new BuildFailedException("Developer console source was included in a player assembly: " + assembly.name);
        var editor = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
        if (!editor.Any(a => a.sourceFiles.Any(IsConsoleSource)))
            throw new InvalidOperationException("Developer console sources were not found in an editor assembly.");
        Debug.Log("Developer console build check passed: all console sources are Editor-only and absent from player assemblies.");
    }

    private static bool IsConsoleSource(string path) => path.Replace('\\', '/').Contains("Assets/Editor/DevConsole/");
}
#endif
