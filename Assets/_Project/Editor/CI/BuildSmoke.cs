#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace AF.EditorTools
{
    /// <summary>
    /// Build de humo Android (compuerta s26 de docs/setup/plan-instalacion.md): construye
    /// Builds/Android/smoke.aab y Builds/Android/smoke.apk (IL2CPP, ARM64) a partir de una escena mínima
    /// que genera al vuelo en Assets/_SmokeTemp/ y borra al terminar. No depende de las escenas del juego.
    ///
    /// Uso en batchmode (sin ningún Editor abierto en la carpeta). Comando validado en la compuerta del día 1
    /// (docs/setup/compuerta-dia-1.md) con el editor ARM64 nativo:
    ///   unity run &lt;repo&gt; --architecture arm64 --log-file Logs/smoke-android-N.log --no-tail --timeout 5400
    ///             --non-interactive -- -nographics -buildTarget Android -executeMethod AF.EditorTools.BuildSmoke.BuildAndroid
    ///   Unity.exe -batchmode -nographics -quit -projectPath . -buildTarget Android -logFile Logs/build-smoke.log
    ///             -executeMethod AF.EditorTools.BuildSmoke.BuildAndroid
    ///
    /// Códigos de salida (solo en batchmode):
    ///   0  AAB y APK construidos
    ///   2  los Player Settings no son los fijados (ejecuta antes ProjectSetup.ApplyAndroidDefaults)
    ///   3  falta el módulo Android o no se pudo cambiar la plataforma activa
    ///   4  falló algún build o hubo una excepción (lee el log)
    ///   6  compuerta de release de los textos (<see cref="LocReleaseGate"/>; 08 §4.7): con un applicationId distinto de
    ///      com.amazonfill.dev, un idioma ofrecido con la UI incompleta o texto cultural sin aprobado_es. Con
    ///      com.amazonfill.dev solo avisa en el log. Va antes de comprobar los Player Settings.
    /// En el Editor interactivo (menú "Amazon Fill/CI") solo escribe en la consola y no cierra nada.
    /// Firma: clave de depuración de Unity (sin firma de release). Nunca se sube a Google Play.
    /// </summary>
    public static class BuildSmoke
    {
        public const string OutputDir = "Builds/Android";
        public const string AabPath = OutputDir + "/smoke.aab";
        public const string ApkPath = OutputDir + "/smoke.apk";

        const string TempSceneFolder = "Assets/_SmokeTemp";
        const string TempScenePath = TempSceneFolder + "/Smoke.unity";
        const string LogPrefix = "[AF.BuildSmoke] ";

        const int ExitOk = 0;
        const int ExitSettings = 2;
        const int ExitPlatform = 3;
        const int ExitBuildFailed = 4;
        const int ExitLocGate = 6;

        [MenuItem("Amazon Fill/CI/Build de humo Android (AAB + APK)")]
        public static void BuildAndroid()
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
                Debug.Log(LogPrefix + "OK: " + AabPath + " y " + ApkPath);
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

        static int Run()
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

            if (!LocReleaseGate.Check(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), LogPrefix))
            {
                return ExitLocGate;
            }

            List<string> problems = ProjectSetup.Validate();
            if (problems.Count > 0)
            {
                Debug.LogError(LogPrefix + "Player Settings distintos de los fijados: " + string.Join("; ", problems)
                    + ". Ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults.");
                return ExitSettings;
            }

            Debug.Log(LogPrefix + ProjectSetup.Describe());

            SceneSetup[]? previousScenes = null;
            if (!Application.isBatchMode)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning(LogPrefix + "cancelado: hay escenas sin guardar.");
                    return ExitBuildFailed;
                }

                previousScenes = EditorSceneManager.GetSceneManagerSetup();
            }

            bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
            Directory.CreateDirectory(OutputDir);
            try
            {
                CreateMinimalScene();
                bool aabOk = BuildOne(true, AabPath);
                bool apkOk = BuildOne(false, ApkPath);
                return aabOk && apkOk ? ExitOk : ExitBuildFailed;
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousAppBundle;
                if (previousScenes != null && previousScenes.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousScenes);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }

                if (AssetDatabase.IsValidFolder(TempSceneFolder))
                {
                    AssetDatabase.DeleteAsset(TempSceneFolder);
                }
            }
        }

        /// <summary>Escena mínima: cámara y luz por defecto de Unity. Se borra al terminar.</summary>
        static void CreateMinimalScene()
        {
            if (AssetDatabase.IsValidFolder(TempSceneFolder))
            {
                AssetDatabase.DeleteAsset(TempSceneFolder);
            }

            AssetDatabase.CreateFolder("Assets", "_SmokeTemp");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, TempScenePath))
            {
                throw new InvalidOperationException(LogPrefix + "no se pudo guardar la escena mínima en " + TempScenePath);
            }
        }

        static bool BuildOne(bool appBundle, string path)
        {
            EditorUserBuildSettings.buildAppBundle = appBundle;
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { TempScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            string kind = appBundle ? "AAB" : "APK";
            Debug.Log(LogPrefix + "construyendo " + kind + " en " + path + " ...");
            Stopwatch watch = Stopwatch.StartNew();
            BuildReport report = BuildPipeline.BuildPlayer(options);
            watch.Stop();

            BuildSummary summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded && File.Exists(path);
            double megabytes = ok ? new FileInfo(path).Length / (1024.0 * 1024.0) : 0.0;
            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1} {2}: resultado={3}, errores={4}, avisos={5}, tiempo={6:F1} s, tamano={7:F2} MB",
                LogPrefix, kind, path, summary.result, summary.totalErrors, summary.totalWarnings,
                watch.Elapsed.TotalSeconds, megabytes);
            if (ok)
            {
                Debug.Log(line);
            }
            else
            {
                Debug.LogError(line);
            }

            return ok;
        }
    }
}
