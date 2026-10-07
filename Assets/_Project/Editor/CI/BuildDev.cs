#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AF.EditorTools
{
    /// <summary>
    /// Build de desarrollo del prototipo v0 (docs/dev/prototipo-v0.md d): <c>Builds/Android/dev.apk</c> como Development
    /// Build (IL2CPP, ARM64; <c>Debug.isDebugBuild</c> activa las líneas <c>AF_LAYOUT</c>/<c>AF_STATE</c>) con las escenas
    /// habilitadas de Build Settings, que deben incluir <c>Assets/_Project/Scenes/Game.unity</c>.
    ///
    /// Uso en batchmode (sin ningún Editor abierto en la carpeta):
    ///   unity run &lt;repo&gt; --architecture arm64 --log-file Logs/build-dev.log --no-tail --timeout 5400
    ///             --non-interactive -- -nographics -buildTarget Android -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev
    ///
    /// Códigos de salida (solo en batchmode):
    ///   0  APK construido
    ///   2  los Player Settings no son los fijados (ejecuta antes ProjectSetup.ApplyAndroidDefaults)
    ///   3  falta el módulo Android o no se pudo cambiar la plataforma activa
    ///   4  falló el build o hubo una excepción (lee el log)
    ///   5  Build Settings no incluye Assets/_Project/Scenes/Game.unity (ejecuta AF.UI.Editor.GameSceneBuilder.CreateGameScene)
    /// Firma: clave de depuración de Unity. Nunca se sube a Google Play.
    /// </summary>
    public static class BuildDev
    {
        /// <summary>APK de desarrollo.</summary>
        public const string ApkPath = BuildSmoke.OutputDir + "/dev.apk";

        /// <summary>Escena del juego que debe estar en Build Settings.</summary>
        public const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        private const string LogPrefix = "[AF.BuildDev] ";

        private const int ExitOk = 0;
        private const int ExitSettings = 2;
        private const int ExitPlatform = 3;
        private const int ExitBuildFailed = 4;
        private const int ExitNoGameScene = 5;

        [MenuItem("Amazon Fill/CI/Build de desarrollo Android (dev.apk)")]
        public static void BuildAndroidDev()
        {
            int code;
            try
            {
                code = Run();
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + "excepción: " + e);
                code = ExitBuildFailed;
            }

            if (code == ExitOk)
            {
                Debug.Log(LogPrefix + "OK: " + ApkPath);
            }
            else
            {
                Debug.LogError(LogPrefix + "FALLO (código " + code + ").");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }

        private static int Run()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError(LogPrefix + "este Editor no tiene el módulo Android Build Support.");
                return ExitPlatform;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android))
            {
                Debug.LogError(LogPrefix + "no se pudo cambiar la plataforma activa a Android.");
                return ExitPlatform;
            }

            List<string> problems = ProjectSetup.Validate();
            if (problems.Count > 0)
            {
                Debug.LogError(LogPrefix + "Player Settings distintos de los fijados: " + string.Join("; ", problems)
                    + ". Ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults.");
                return ExitSettings;
            }

            var scenes = new List<string>();
            bool hasGame = false;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled || string.IsNullOrEmpty(scene.path))
                {
                    continue;
                }

                scenes.Add(scene.path);
                if (string.Equals(scene.path, GameScenePath, StringComparison.Ordinal) && File.Exists(scene.path))
                {
                    hasGame = true;
                }
            }

            if (!hasGame)
            {
                Debug.LogError(LogPrefix + "Build Settings no incluye " + GameScenePath
                    + ". Ejecuta AF.UI.Editor.GameSceneBuilder.CreateGameScene.");
                return ExitNoGameScene;
            }

            Debug.Log(LogPrefix + ProjectSetup.Describe() + ", escenas=" + string.Join(",", scenes));

            bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
            Directory.CreateDirectory(BuildSmoke.OutputDir);
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                if (File.Exists(ApkPath))
                {
                    File.Delete(ApkPath);
                }

                var options = new BuildPlayerOptions
                {
                    scenes = scenes.ToArray(),
                    locationPathName = ApkPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.Development,
                };

                Debug.Log(LogPrefix + "construyendo " + ApkPath + " (Development) ...");
                Stopwatch watch = Stopwatch.StartNew();
                BuildReport report = BuildPipeline.BuildPlayer(options);
                watch.Stop();

                BuildSummary summary = report.summary;
                bool ok = summary.result == BuildResult.Succeeded && File.Exists(ApkPath);
                double megabytes = ok ? new FileInfo(ApkPath).Length / (1024.0 * 1024.0) : 0.0;
                string line = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}APK {1}: resultado={2}, errores={3}, avisos={4}, tiempo={5:F1} s, tamano={6:F2} MB",
                    LogPrefix, ApkPath, summary.result, summary.totalErrors, summary.totalWarnings,
                    watch.Elapsed.TotalSeconds, megabytes);
                if (ok)
                {
                    Debug.Log(line);
                    return ExitOk;
                }

                Debug.LogError(line);
                return ExitBuildFailed;
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousAppBundle;
            }
        }
    }
}
