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
    ///      o los de Google Mobile Ads no tienen el App ID de AdMob y no se pudieron aplicar (AF.Ads.Google.Editor.GoogleAdsProjectSetup; D-049)
    ///   3  falta el módulo Android o no se pudo cambiar la plataforma activa
    ///   4  falló el build o hubo una excepción (lee el log)
    ///   5  Build Settings no incluye Assets/_Project/Scenes/Game.unity (ejecuta AF.UI.Editor.GameSceneBuilder.CreateGameScene)
    ///   6  compuerta de release de los textos (<see cref="LocReleaseGate"/>; 08 §4.7): con un applicationId distinto de
    ///      com.amazonfill.dev, un idioma ofrecido con la UI incompleta o texto cultural sin aprobado_es. Con
    ///      com.amazonfill.dev solo avisa en el log. Va antes de comprobar los Player Settings.
    /// Firma: clave de depuración de Unity. Nunca se sube a Google Play.
    ///
    /// Antes de construir borra el <c>dev.apk</c> anterior y el APK de depuración que dejó Gradle en el proyecto que exporta Unity
    /// (<see cref="GradleDebugApkDirs"/>), y lo dice en el log (tarea FX10, observación 2 de la prueba de la ola 6 en el
    /// teléfono): <c>dev.apk</c> es una copia de ese APK, y con el build incremental Gradle lo actualiza en su sitio y deja huecos
    /// entre las entradas (37,6 MB de 105,6 en el portátil). Sin el APK anterior, Gradle lo vuelve a empaquetar entero. No cambia
    /// los códigos de salida: si no se puede borrar, es una excepción (código 4), como cualquier otro fallo del build.
    /// </summary>
    public static class BuildDev
    {
        /// <summary>APK de desarrollo.</summary>
        public const string ApkPath = BuildSmoke.OutputDir + "/dev.apk";

        /// <summary>
        /// Carpetas donde deja Gradle el APK de depuración, relativas al proyecto, una por backend de scripting
        /// (<c>Library/Bee/Android/Prj/&lt;IL2CPP|Mono2x&gt;/Gradle/…</c>): <c>dev.apk</c> es una copia de ese APK.
        /// </summary>
        public const string GradleProjectsDir = "Library/Bee/Android/Prj";

        /// <summary>Ruta, dentro del proyecto Gradle de cada backend, de la carpeta del APK de depuración.</summary>
        public const string GradleDebugApkDir = "Gradle/launcher/build/outputs/apk/debug";

        /// <summary>Escena del juego que debe estar en Build Settings.</summary>
        public const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        private const string LogPrefix = "[AF.BuildDev] ";

        private const int ExitOk = 0;
        private const int ExitSettings = 2;
        private const int ExitPlatform = 3;
        private const int ExitBuildFailed = 4;
        private const int ExitNoGameScene = 5;
        private const int ExitLocGate = 6;

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

            // D-049: el App ID de AdMob en GoogleMobileAdsSettings.asset (si falta, se aplica; nunca un build con el App ID vacío).
            if (!AF.Ads.Google.Editor.GoogleAdsProjectSetup.CheckForBuild(LogPrefix))
            {
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
                DeletePreviousApks(ApkPath, GradleProjectsDir);

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

        /// <summary>
        /// Las carpetas del APK de depuración de Gradle que existen ahora (<paramref name="projectsDir"/>/&lt;backend&gt;/
        /// <see cref="GradleDebugApkDir"/>; en el build, <see cref="GradleProjectsDir"/>), en orden ordinal.
        /// </summary>
        public static List<string> GradleDebugApkDirs(string projectsDir)
        {
            var dirs = new List<string>();
            if (!Directory.Exists(projectsDir))
            {
                return dirs;
            }

            string[] backends = Directory.GetDirectories(projectsDir);
            Array.Sort(backends, StringComparer.Ordinal);
            foreach (string backend in backends)
            {
                string dir = Path.Combine(backend, GradleDebugApkDir).Replace('\\', '/');
                if (Directory.Exists(dir))
                {
                    dirs.Add(dir);
                }
            }

            return dirs;
        }

        /// <summary>
        /// Borra el APK anterior <paramref name="apkPath"/> (en el build, <see cref="ApkPath"/>) y los APK de depuración de Gradle
        /// bajo <paramref name="projectsDir"/> (<see cref="GradleDebugApkDirs"/>) y lo dice en el log, una línea por archivo (o
        /// que no había APK anterior). Devuelve cuántos borró (tarea FX10).
        /// </summary>
        internal static int DeletePreviousApks(string apkPath, string projectsDir)
        {
            int deleted = 0;
            if (File.Exists(apkPath))
            {
                long bytes = new FileInfo(apkPath).Length;
                File.Delete(apkPath);
                deleted++;
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}borrado el {1} anterior ({2:F2} MB): el nuevo se escribe entero.",
                    LogPrefix, apkPath, bytes / (1024.0 * 1024.0)));
            }
            else
            {
                Debug.Log(LogPrefix + "no hay " + apkPath + " anterior.");
            }

            foreach (string dir in GradleDebugApkDirs(projectsDir))
            {
                string[] apks = Directory.GetFiles(dir, "*.apk");
                Array.Sort(apks, StringComparer.Ordinal);
                foreach (string apk in apks)
                {
                    long bytes = new FileInfo(apk).Length;
                    File.Delete(apk);
                    deleted++;
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "{0}borrado el APK anterior de Gradle {1} ({2:F2} MB): Gradle lo empaqueta entero, sin los huecos del build incremental.",
                        LogPrefix, apk.Replace('\\', '/'), bytes / (1024.0 * 1024.0)));
                }
            }

            return deleted;
        }
    }
}
