#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AF.EditorTools
{
    /// <summary>
    /// Exporta el proyecto de Xcode de desarrollo (fase M1 de <c>docs/dev/traspaso/mac-ios.md</c>, D-067) en
    /// <see cref="XcodeDir"/>: Development Build con las escenas habilitadas de Build Settings, como <see cref="BuildDev"/> en Android.
    /// Xcode lo compila, lo firma con el Apple ID del dueño y lo instala en el iPhone (<c>docs/dev/mac-setup.md</c> §6).
    ///
    /// Uso en la Mac, en batchmode (sin ningún Editor abierto en la carpeta; <c>-buildTarget iOS</c> para que el Editor arranque ya en
    /// iOS y cargue <see cref="OnPostprocessBuild"/>):
    ///   Unity -batchmode -nographics -quit -buildTarget iOS -projectPath . -logFile Logs/build-ios.log
    ///         -executeMethod AF.EditorTools.BuildIos.ExportXcode
    ///
    /// Códigos de salida (solo en batchmode):
    ///   0  proyecto de Xcode exportado
    ///   2  los Player Settings de iOS no son los fijados (ejecuta antes ProjectSetup.ApplyIosDefaults)
    ///   3  falta el módulo iOS o no se pudo cambiar la plataforma activa
    ///   4  falló el build o hubo una excepción (lee el log)
    ///   5  Build Settings no incluye Assets/_Project/Scenes/Game.unity
    /// Nunca se sube a App Store Connect: publica el dueño.
    /// </summary>
    public static class BuildIos
    {
        /// <summary>Carpeta de los builds de iOS (ignorada por git, como <c>Builds/Android</c>).</summary>
        public const string OutputDir = "Builds/iOS";

        /// <summary>El proyecto de Xcode exportado (con <c>Unity-iPhone.xcodeproj</c> dentro).</summary>
        public const string XcodeDir = OutputDir + "/Xcode";

        /// <summary>
        /// Icono de iOS, apariencia clara: el de App Store de la identidad v2 (D-045), 1024 × 1024 sin alfa (mac-ios.md, M2).
        /// </summary>
        public const string IconPath = "docs/art/marca-oficial/v2/out/png/app-store-1024.png";

        /// <summary>
        /// Apariencia oscura: el icono sin el cielo ni la selva, sobre transparente (<c>generar.py</c>, <c>ios_oscuro</c>; lo exporta
        /// <c>exportar.py --ios</c>). iOS pone su degradado oscuro.
        /// </summary>
        public const string DarkIconPath = "docs/art/marca-oficial/v2/out/png/ios-oscuro-1024.png";

        /// <summary>Apariencia tintada: la monocroma oficial, blanca sobre transparente (<c>ios_tintado</c>); iOS la tiñe.</summary>
        public const string TintedIconPath = "docs/art/marca-oficial/v2/out/png/ios-tintado-1024.png";

        /// <summary>El catálogo de iconos del proyecto de Xcode que exporta Unity.</summary>
        public const string AppIconSetDir = "Unity-iPhone/Images.xcassets/AppIcon.appiconset";

        private const string LogPrefix = "[AF.BuildIos] ";

        private const int ExitOk = 0;
        private const int ExitSettings = 2;
        private const int ExitPlatform = 3;
        private const int ExitBuildFailed = 4;
        private const int ExitNoGameScene = 5;

        [MenuItem("Amazon Fill/CI/Exportar el proyecto de Xcode (dev)")]
        public static void ExportXcode()
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
                Debug.Log(LogPrefix + "OK: " + XcodeDir);
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
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.LogError(LogPrefix + "este Editor no tiene el módulo iOS Build Support.");
                return ExitPlatform;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.iOS, BuildTarget.iOS))
            {
                Debug.LogError(LogPrefix + "no se pudo cambiar la plataforma activa a iOS.");
                return ExitPlatform;
            }

            List<string> problems = ProjectSetup.ValidateIos();
            if (problems.Count > 0)
            {
                Debug.LogError(LogPrefix + "Player Settings de iOS distintos de los fijados: " + string.Join("; ", problems)
                    + ". Ejecuta AF.EditorTools.ProjectSetup.ApplyIosDefaults.");
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
                if (string.Equals(scene.path, BuildDev.GameScenePath, StringComparison.Ordinal) && File.Exists(scene.path))
                {
                    hasGame = true;
                }
            }

            if (!hasGame)
            {
                Debug.LogError(LogPrefix + "Build Settings no incluye " + BuildDev.GameScenePath
                    + ". Ejecuta AF.UI.Editor.GameSceneBuilder.CreateGameScene.");
                return ExitNoGameScene;
            }

            Debug.Log(LogPrefix + ProjectSetup.DescribeIos() + ", escenas=" + string.Join(",", scenes));
            Directory.CreateDirectory(OutputDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = XcodeDir,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.Development,
            };

            Debug.Log(LogPrefix + "exportando " + XcodeDir + " (Development) ...");
            Stopwatch watch = Stopwatch.StartNew();
            BuildReport report = BuildPipeline.BuildPlayer(options);
            watch.Stop();

            BuildSummary summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded && Directory.Exists(Path.Combine(XcodeDir, "Unity-iPhone.xcodeproj"));
            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{0}Xcode {1}: resultado={2}, errores={3}, avisos={4}, tiempo={5:F1} s",
                LogPrefix, XcodeDir, summary.result, summary.totalErrors, summary.totalWarnings, watch.Elapsed.TotalSeconds);
            if (ok)
            {
                Debug.Log(line);
                return ExitOk;
            }

            Debug.LogError(line);
            return ExitBuildFailed;
        }

        /// <summary>
        /// Tras exportar para iOS, pone el icono de 1024 del catálogo en sus tres apariencias (<see cref="IconPath"/>,
        /// <see cref="DarkIconPath"/> y <see cref="TintedIconPath"/>; Xcode 14+ genera los demás tamaños). Sin el claro solo avisa: el
        /// proyecto se exporta con el icono por defecto de Unity; sin una de las otras dos, iOS la genera del claro.
        /// </summary>
        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            RaisePodsDeploymentTarget(pathToBuiltProject);
            SkipCrashlyticsWithoutConfig(pathToBuiltProject);
            DisableUserScriptSandboxing(pathToBuiltProject);
            EnsureGadApplicationId(pathToBuiltProject);

            string iconSet = Path.Combine(pathToBuiltProject, AppIconSetDir);
            if (!File.Exists(IconPath) || !Directory.Exists(iconSet))
            {
                Debug.LogWarning(LogPrefix + "sin icono (" + IconPath + " o " + iconSet + " no existen): queda el de Unity.");
                return;
            }

            foreach (string old in Directory.GetFiles(iconSet, "*.png"))
            {
                File.Delete(old);
            }

            string images = IconEntry(IconPath, iconSet, "Icon-1024.png", null)
                + IconEntry(DarkIconPath, iconSet, "Icon-1024-dark.png", "dark")
                + IconEntry(TintedIconPath, iconSet, "Icon-1024-tinted.png", "tinted");
            File.WriteAllText(Path.Combine(iconSet, "Contents.json"),
                "{\n  \"images\" : [\n" + images.TrimEnd(',', '\n') + "\n  ],\n"
                + "  \"info\" : {\n    \"author\" : \"xcode\",\n    \"version\" : 1\n  }\n}\n");
            Debug.Log(LogPrefix + "icono: " + IconPath + (File.Exists(DarkIconPath) ? ", oscuro" : string.Empty)
                + (File.Exists(TintedIconPath) ? ", tintado" : string.Empty) + " -> " + iconSet);
        }

        // Copia el PNG de 1024 al catálogo y devuelve su entrada de Contents.json (con la apariencia, si la tiene); vacía si no existe.
        private static string IconEntry(string source, string iconSet, string fileName, string? luminosity)
        {
            if (!File.Exists(source))
            {
                Debug.LogWarning(LogPrefix + "sin " + source + ": iOS genera esa apariencia del icono claro.");
                return string.Empty;
            }

            File.Copy(source, Path.Combine(iconSet, fileName), true);
            string appearance = luminosity == null
                ? string.Empty
                : "      \"appearances\" : [\n        {\n          \"appearance\" : \"luminosity\",\n          \"value\" : \"" + luminosity
                    + "\"\n        }\n      ],\n";
            return "    {\n" + appearance + "      \"filename\" : \"" + fileName + "\",\n      \"idiom\" : \"universal\",\n"
                + "      \"platform\" : \"ios\",\n      \"size\" : \"1024x1024\"\n    },\n";
        }

        /// <summary>
        /// App ID de AdMob de EJEMPLO de Google para iOS (el público de su documentación). Va en el <c>Info.plist</c> solo mientras el
        /// proyecto no tenga el suyo (<c>adMobIOSAppId</c> vacío en <c>GoogleMobileAdsSettings.asset</c>; el real lo crea el dueño en
        /// AdMob en M3): sin <c>GADApplicationIdentifier</c>, el SDK de Google Mobile Ads cierra la app al arrancar
        /// (<c>GADInvalidInitializationException</c>), aunque el juego no lo inicie. En M1 los anuncios de iOS son los falsos
        /// (<c>GoogleAdProvider</c> va detrás de <c>UNITY_ANDROID</c>), así que con este ID no se pide ningún anuncio.
        /// </summary>
        public const string SampleGadApplicationId = "ca-app-pub-3940256099942544~1458002511";

        /// <summary>
        /// Pone <see cref="SampleGadApplicationId"/> en el <c>Info.plist</c> si el plugin no escribió ningún
        /// <c>GADApplicationIdentifier</c> (con el App ID de iOS vacío, su post-proceso solo deja un error en el log).
        /// </summary>
        internal static void EnsureGadApplicationId(string pathToBuiltProject)
        {
            string plist = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plist))
            {
                return;
            }

            string text = File.ReadAllText(plist);
            if (text.Contains("<key>GADApplicationIdentifier</key>"))
            {
                Debug.Log(LogPrefix + "Info.plist ya tiene GADApplicationIdentifier.");
                return;
            }

            // El ID de ejemplo solo vale en desarrollo: un build de release sin el App ID real de iOS falla aquí (revisión de M2).
            if (!EditorUserBuildSettings.development)
            {
                throw new BuildFailedException(LogPrefix + "build de release sin GADApplicationIdentifier: pon adMobIOSAppId en "
                    + "GoogleMobileAdsSettings.asset (M3).");
            }

            int end = text.LastIndexOf("</dict>", StringComparison.Ordinal);
            if (end < 0)
            {
                throw new InvalidOperationException(LogPrefix + "Info.plist sin </dict>: " + plist);
            }

            string entry = "  <key>GADApplicationIdentifier</key>\n  <string>" + SampleGadApplicationId + "</string>\n";
            File.WriteAllText(plist, text.Substring(0, end) + entry + text.Substring(end));
            Debug.LogWarning(LogPrefix + "Info.plist: GADApplicationIdentifier de EJEMPLO de Google (" + SampleGadApplicationId
                + "); el real de iOS llega en M3.");
        }

        /// <summary>
        /// Xcode 27 ejecuta los scripts de compilación en una sandbox (<c>ENABLE_USER_SCRIPT_SANDBOXING</c>), y el paso de IL2CPP que
        /// exporta Unity no puede leer su propio <c>il2cpp</c> ni escribir en <c>Il2CppTempDirArtifacts</c>. Se apaga en todas las
        /// configuraciones del proyecto de Xcode exportado (no en los Pods).
        /// </summary>
        internal static void DisableUserScriptSandboxing(string pathToBuiltProject)
        {
            string pbxproj = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/project.pbxproj");
            if (!File.Exists(pbxproj))
            {
                return;
            }

            // Fuera lo que hubiera y, después, un NO al abrir cada bloque buildSettings (las configuraciones que no lo nombran
            // heredarían el YES por defecto de Xcode).
            string text = System.Text.RegularExpressions.Regex.Replace(
                File.ReadAllText(pbxproj), @"[ \t]*ENABLE_USER_SCRIPT_SANDBOXING = [A-Za-z]+;\n", string.Empty);
            int changed = 0;
            text = System.Text.RegularExpressions.Regex.Replace(text, @"buildSettings = \{\n", m =>
            {
                changed++;
                return m.Value + "\t\t\t\tENABLE_USER_SCRIPT_SANDBOXING = NO;\n";
            });
            File.WriteAllText(pbxproj, text);
            Debug.Log(LogPrefix + "ENABLE_USER_SCRIPT_SANDBOXING = NO en " + changed + " configuraciones del proyecto de Xcode.");
        }

        /// <summary>
        /// Sin <c>GoogleService-Info.plist</c> (lo pone el dueño en M2; es un secreto, regla dura 5), el paso «Crashlytics Run Script»
        /// que añade el plugin de Firebase hace fallar a Xcode. Mientras falte, el paso termina sin hacer nada; con el archivo, corre
        /// igual que siempre. En iOS el juego no arranca Firebase todavía (<c>FirebaseBoot</c> va detrás de <c>UNITY_ANDROID</c>).
        /// </summary>
        internal static void SkipCrashlyticsWithoutConfig(string pathToBuiltProject)
        {
            string pbxproj = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/project.pbxproj");
            if (!File.Exists(pbxproj))
            {
                return;
            }

            const string PhaseName = "name = \"Crashlytics Run Script\";";
            const string ScriptStart = "shellScript = \"";
            const string Guard = "if [ ! -f \\\"${PROJECT_DIR}/GoogleService-Info.plist\\\" ]; then "
                + "echo \\\"AF: sin GoogleService-Info.plist, se salta Crashlytics\\\"; exit 0; fi\\n";
            string text = File.ReadAllText(pbxproj);
            int guarded = 0;
            int phase = text.IndexOf(PhaseName, StringComparison.Ordinal);
            while (phase >= 0)
            {
                int script = text.IndexOf(ScriptStart, phase, StringComparison.Ordinal);
                if (script < 0)
                {
                    break;
                }

                int at = script + ScriptStart.Length;
                if (string.CompareOrdinal(text, at, Guard, 0, Guard.Length) != 0)
                {
                    text = text.Substring(0, at) + Guard + text.Substring(at);
                    guarded++;
                }

                phase = text.IndexOf(PhaseName, at, StringComparison.Ordinal);
            }

            File.WriteAllText(pbxproj, text);
            Debug.Log(LogPrefix + "Crashlytics Run Script: " + guarded + " pasos se saltan mientras no haya GoogleService-Info.plist.");
        }

        /// <summary>
        /// Sube el iOS mínimo de los Pods (que EDM4U instala antes de este post-proceso) al del juego,
        /// <see cref="ProjectSetup.IosMinimumVersion"/>: Xcode 27 no compila objetivos por debajo de iOS 15 y los podspec de Google
        /// Mobile Ads, UMP y Unity Ads piden 12.0 o 13.0. Los que ya piden más se quedan como están.
        /// </summary>
        internal static void RaisePodsDeploymentTarget(string pathToBuiltProject)
        {
            string pbxproj = Path.Combine(pathToBuiltProject, "Pods/Pods.xcodeproj/project.pbxproj");
            if (!File.Exists(pbxproj))
            {
                Debug.Log(LogPrefix + "sin Pods (" + pbxproj + "): nada que subir.");
                return;
            }

            double minimum = double.Parse(ProjectSetup.IosMinimumVersion, CultureInfo.InvariantCulture);
            int raised = 0;
            string text = System.Text.RegularExpressions.Regex.Replace(
                File.ReadAllText(pbxproj),
                @"IPHONEOS_DEPLOYMENT_TARGET = ([0-9]+(?:\.[0-9]+)?);",
                m =>
                {
                    if (double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) >= minimum)
                    {
                        return m.Value;
                    }

                    raised++;
                    return "IPHONEOS_DEPLOYMENT_TARGET = " + ProjectSetup.IosMinimumVersion + ";";
                });
            File.WriteAllText(pbxproj, text);
            Debug.Log(LogPrefix + "Pods: " + raised + " ajustes de IPHONEOS_DEPLOYMENT_TARGET subidos a " + ProjectSetup.IosMinimumVersion + ".");
        }
    }
}
