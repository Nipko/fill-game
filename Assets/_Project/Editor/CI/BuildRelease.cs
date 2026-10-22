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
    /// Build de release Android SIN FIRMA de release (bloque B10 de la tanda 5; 08 §12.1 etapa 3 y §5.4): AAB (y, con
    /// <c>-afApk</c>, también un APK) sin Development Build, IL2CPP ARM64, Managed Stripping High con
    /// <c>Assets/Plugins/link.xml</c> y Strip Engine Code, con las escenas habilitadas de Build Settings (que deben incluir
    /// <c>Assets/_Project/Scenes/Game.unity</c>). Firma con la clave de depuración de Unity: nunca toca keystores ni secretos
    /// (CLAUDE.md, regla dura 5); la firma de subida es del dueño. Nunca se sube a Google Play (regla dura 6).
    ///
    /// Identidad del build (regla dura 6; 08 §13.1 #1):
    ///   - Release: <c>-afApplicationId &lt;id&gt;</c> (o la variable de entorno <c>AF_RELEASE_APPLICATION_ID</c>), distinto
    ///     del provisional <c>com.amazonfill.dev</c> y sin la cadena "amazon"; el Product Name pasa a <see cref="ReleaseProductName"/>.
    ///     El ID definitivo, <c>com.&lt;estudio&gt;.tintalu</c>, solo existe tras el dictamen de marca: hasta entonces no hay build
    ///     de release posible y este método sale con 7.
    ///   - Ensayo (<c>-afRehearsal</c>): la misma configuración de release con la identidad de prueba (<c>com.amazonfill.dev</c>,
    ///     "Tintalú (dev)"). Sirve para medir el tamaño con el stripping y jugar el APK en el teléfono; sale como
    ///     <c>rehearsal.aab</c>/<c>rehearsal.apk</c> y la compuerta del manifiesto lo trata como build de prueba
    ///     (<c>ci/check-manifest.ps1 -Mode dev</c>), nunca como release.
    /// El ID y el nombre de release se ponen solo durante el build y se restauran al terminar, así que
    /// <c>ProjectSettings/</c> sigue con los valores de <see cref="ProjectSetup"/>.
    ///
    /// Sin Development Build, Unity construye la variante <c>release</c> de Gradle, y
    /// <c>Assets/Plugins/Android/settingsTemplate.gradle</c> excluye de ella <c>firebase-appcheck-debug</c> (release y ensayo;
    /// D-033, añadido del 2026-10-22, punto 9). Compuerta: <c>ci/check-release-classes.ps1</c>.
    ///
    /// Uso en batchmode (sin ningún Editor abierto en la carpeta; en el escritorio x64, <c>--architecture x86_64</c>):
    ///   unity run &lt;repo&gt; --architecture arm64 --log-file Logs/build-release.log --no-tail --timeout 5400 --non-interactive
    ///             -- -nographics -buildTarget Android -executeMethod AF.EditorTools.BuildRelease.BuildAndroid -afRehearsal -afApk
    /// En el CI (game-ci/unity-builder, <c>buildMethod</c>), la ruta de salida llega con <c>-customBuildPath</c> y el
    /// versionCode con <c>-androidVersionCode</c>.
    ///
    /// Códigos de salida (solo en batchmode):
    ///   0  build construido
    ///   2  los Player Settings no son los fijados (ejecuta antes ProjectSetup.ApplyAndroidDefaults)
    ///   3  falta el módulo Android o no se pudo cambiar la plataforma activa
    ///   4  falló el build o hubo una excepción (lee el log)
    ///   5  Build Settings no incluye Assets/_Project/Scenes/Game.unity
    ///   6  compuerta de release de los textos (<see cref="LocReleaseGate"/>; 08 §4.7)
    ///   7  identidad inválida: sin <c>-afApplicationId</c> ni <c>-afRehearsal</c>, el ID provisional o con "amazon" en un
    ///      release, un ID con formato inválido, o <c>-afApplicationId</c> junto con <c>-afRehearsal</c>
    ///   8  argumentos incorrectos (<c>-androidVersionCode</c> no es un entero positivo)
    /// </summary>
    public static class BuildRelease
    {
        /// <summary>Nombre visible del build de release (glosario §11: el nombre visible lleva la marca).</summary>
        public const string ReleaseProductName = "Tintalú";

        /// <summary>Carpeta de salida local (la misma que BuildSmoke y BuildDev, ignorada por git).</summary>
        public const string OutputDir = BuildSmoke.OutputDir;

        /// <summary>Variable de entorno alternativa a <c>-afApplicationId</c>.</summary>
        public const string ApplicationIdEnv = "AF_RELEASE_APPLICATION_ID";

        private const string LogPrefix = "[AF.BuildRelease] ";

        private const int ExitOk = 0;
        private const int ExitSettings = 2;
        private const int ExitPlatform = 3;
        private const int ExitBuildFailed = 4;
        private const int ExitNoGameScene = 5;
        private const int ExitLocGate = 6;
        private const int ExitIdentity = 7;
        private const int ExitArgs = 8;

        /// <summary>Opciones leídas de la línea de comandos.</summary>
        internal sealed class Options
        {
            public string? ApplicationId;
            public bool Rehearsal;
            public bool Apk;
            public string? CustomBuildPath;
            public int VersionCode;
        }

        [MenuItem("Amazon Fill/CI/Ensayo de release Android (rehearsal.aab + apk)")]
        public static void RehearsalFromMenu()
        {
            Execute(new Options { Rehearsal = true, Apk = true });
        }

        /// <summary>Punto de entrada de batchmode y del CI.</summary>
        public static void BuildAndroid()
        {
            Options? options;
            string? error = ParseArgs(Environment.GetCommandLineArgs(), Environment.GetEnvironmentVariable(ApplicationIdEnv), out options);
            if (error != null || options == null)
            {
                Debug.LogError(LogPrefix + "argumentos incorrectos: " + error);
                Finish(ExitArgs);
                return;
            }

            Execute(options);
        }

        private static void Execute(Options options)
        {
            int code;
            try
            {
                code = Run(options);
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + "excepción: " + e);
                code = ExitBuildFailed;
            }

            Finish(code);
        }

        private static void Finish(int code)
        {
            if (code == ExitOk)
            {
                Debug.Log(LogPrefix + "OK.");
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

        /// <summary>
        /// Lee <c>-afApplicationId &lt;id&gt;</c>, <c>-afRehearsal</c>, <c>-afApk</c>, <c>-customBuildPath &lt;ruta&gt;</c> y
        /// <c>-androidVersionCode &lt;n&gt;</c>. El ID de <paramref name="envApplicationId"/> solo se usa si no hay
        /// <c>-afApplicationId</c>. Devuelve el error, o null.
        /// </summary>
        internal static string? ParseArgs(string[] args, string? envApplicationId, out Options? options)
        {
            var o = new Options();
            options = null;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (string.Equals(a, "-afRehearsal", StringComparison.Ordinal))
                {
                    o.Rehearsal = true;
                }
                else if (string.Equals(a, "-afApk", StringComparison.Ordinal))
                {
                    o.Apk = true;
                }
                else if (string.Equals(a, "-afApplicationId", StringComparison.Ordinal)
                    || string.Equals(a, "-customBuildPath", StringComparison.Ordinal)
                    || string.Equals(a, "-androidVersionCode", StringComparison.Ordinal))
                {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    {
                        return a + " sin valor";
                    }

                    string value = args[++i];
                    if (a == "-afApplicationId")
                    {
                        o.ApplicationId = value;
                    }
                    else if (a == "-customBuildPath")
                    {
                        o.CustomBuildPath = value;
                    }
                    else if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out o.VersionCode) || o.VersionCode <= 0)
                    {
                        return "-androidVersionCode '" + value + "' no es un entero positivo";
                    }
                }
            }

            if (string.IsNullOrEmpty(o.ApplicationId) && !string.IsNullOrEmpty(envApplicationId))
            {
                o.ApplicationId = envApplicationId!.Trim();
            }

            options = o;
            return null;
        }

        /// <summary>
        /// Comprueba la identidad del build (regla dura 6; 08 §13.1 #1). Devuelve el error, o null. En un ensayo el ID es
        /// siempre el provisional; en un release, uno con formato de paquete Android, distinto del provisional y sin "amazon".
        /// </summary>
        internal static string? CheckIdentity(Options o)
        {
            if (o.Rehearsal)
            {
                return string.IsNullOrEmpty(o.ApplicationId)
                    ? null
                    : "-afRehearsal usa siempre " + ProjectSetup.ApplicationId + ": no admite -afApplicationId ni " + ApplicationIdEnv;
            }

            if (string.IsNullOrEmpty(o.ApplicationId))
            {
                return "falta -afApplicationId (o " + ApplicationIdEnv + "). El ID definitivo solo se fija tras el dictamen de marca; "
                    + "para medir o probar la configuración de release usa -afRehearsal";
            }

            string id = o.ApplicationId!;
            if (string.Equals(id, ProjectSetup.ApplicationId, StringComparison.OrdinalIgnoreCase))
            {
                return "el ID provisional " + ProjectSetup.ApplicationId + " nunca llega a un build de release (regla dura 6)";
            }

            if (id.IndexOf("amazon", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "el ID de release no lleva la cadena 'amazon' (08 §13.1 #1): " + id;
            }

            if (!IsValidPackageName(id))
            {
                return "'" + id + "' no es un nombre de paquete Android válido";
            }

            return null;
        }

        /// <summary>Al menos dos segmentos; cada uno empieza por letra y sigue con letras, dígitos o '_' (ASCII).</summary>
        internal static bool IsValidPackageName(string id)
        {
            string[] parts = id.Split('.');
            if (parts.Length < 2)
            {
                return false;
            }

            foreach (string part in parts)
            {
                if (part.Length == 0 || !IsAsciiLetter(part[0]))
                {
                    return false;
                }

                foreach (char c in part)
                {
                    if (!IsAsciiLetter(c) && !(c >= '0' && c <= '9') && c != '_')
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsAsciiLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        /// <summary>Ruta del AAB (o del APK con <paramref name="apk"/>): <c>-customBuildPath</c> si llega, si no Builds/Android.</summary>
        internal static string OutputPath(Options o, bool apk)
        {
            string ext = apk ? ".apk" : ".aab";
            if (!string.IsNullOrEmpty(o.CustomBuildPath))
            {
                string custom = o.CustomBuildPath!.Replace('\\', '/');
                string current = Path.GetExtension(custom);
                if (string.Equals(current, ".aab", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(current, ".apk", StringComparison.OrdinalIgnoreCase))
                {
                    return custom.Substring(0, custom.Length - current.Length) + ext;
                }

                return custom.TrimEnd('/') + "/" + (o.Rehearsal ? "rehearsal" : "release") + ext;
            }

            return OutputDir + "/" + (o.Rehearsal ? "rehearsal" : "release") + ext;
        }

        private static int Run(Options o)
        {
            string? identityError = CheckIdentity(o);
            if (identityError != null)
            {
                Debug.LogError(LogPrefix + identityError + ".");
                return ExitIdentity;
            }

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

            string applicationId = o.Rehearsal ? ProjectSetup.ApplicationId : o.ApplicationId!;
            if (!LocReleaseGate.Check(applicationId, LogPrefix))
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

            NamedBuildTarget android = NamedBuildTarget.Android;
            string previousId = PlayerSettings.GetApplicationIdentifier(android);
            string previousName = PlayerSettings.productName;
            int previousVersionCode = PlayerSettings.Android.bundleVersionCode;
            bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                if (!o.Rehearsal)
                {
                    PlayerSettings.SetApplicationIdentifier(android, applicationId);
                    PlayerSettings.productName = ReleaseProductName;
                }

                if (o.VersionCode > 0)
                {
                    PlayerSettings.Android.bundleVersionCode = o.VersionCode;
                }

                Debug.Log(LogPrefix + (o.Rehearsal ? "ENSAYO (identidad de prueba). " : "RELEASE sin firma. ")
                    + ProjectSetup.Describe() + ", versionCode=" + PlayerSettings.Android.bundleVersionCode
                    + ", escenas=" + string.Join(",", scenes));

                bool ok = BuildOne(scenes, OutputPath(o, false), true);
                if (ok && o.Apk)
                {
                    ok = BuildOne(scenes, OutputPath(o, true), false);
                }

                return ok ? ExitOk : ExitBuildFailed;
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousAppBundle;
                PlayerSettings.SetApplicationIdentifier(android, previousId);
                PlayerSettings.productName = previousName;
                PlayerSettings.Android.bundleVersionCode = previousVersionCode;
            }
        }

        private static bool BuildOne(List<string> scenes, string path, bool appBundle)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            EditorUserBuildSettings.buildAppBundle = appBundle;
            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
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
            double megabytes = ok ? new FileInfo(path).Length / 1000000.0 : 0.0;
            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1} {2}: resultado={3}, errores={4}, avisos={5}, tiempo={6:F1} s, tamano={7:F2} MB (decimales)",
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
