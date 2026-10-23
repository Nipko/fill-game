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
    /// Build de release Android (bloque B10 de la tanda 5 y paso s32; 08 §12.1 etapa 3 y §5.4): AAB (y, con
    /// <c>-afApk</c>, también un APK) sin Development Build, IL2CPP ARM64, Managed Stripping High con
    /// <c>Assets/Plugins/link.xml</c> y Strip Engine Code, con las escenas habilitadas de Build Settings (que deben incluir
    /// <c>Assets/_Project/Scenes/Game.unity</c>). Nunca se sube a Google Play (regla dura 6).
    ///
    /// Firma (paso s32; 08 §12.1 etapa 3 y §13.1 #3; <c>docs/dev/release.md</c>):
    ///   - Sin firma configurada, Unity firma con su clave de depuración (job <c>build-android</c> de <c>ci.yml</c>).
    ///   - Con firma, la clave de subida llega por las variables de entorno <c>ANDROID_KEYSTORE_NAME</c> (ruta del keystore,
    ///     fuera del repositorio), <c>ANDROID_KEYSTORE_PASS</c>, <c>ANDROID_KEYALIAS_NAME</c> y <c>ANDROID_KEYALIAS_PASS</c>,
    ///     las que game-ci/unity-builder pasa al contenedor; si una falta o está vacía, vale el argumento homónimo
    ///     (<c>-androidKeystoreName</c>, etc.) que game-ci añade a la línea de comandos. El método no crea ni lee el keystore
    ///     (solo comprueba que existe), no escribe las contraseñas en el log y, al terminar, deja los Player Settings de la
    ///     firma como estaban: las contraseñas no se serializan en <c>ProjectSettings/</c>, y la ruta y el alias se
    ///     restauran, así que <c>ProjectSettings/</c> sigue sin keystore propio (<see cref="ProjectSetup"/>).
    ///   - <c>-afRequireSigning</c> (workflow <c>release.yml</c>) exige la firma completa: sin ella el build no empieza.
    ///   - La firma va solo con un release: un ensayo con firma, o una firma a medias, es un error.
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
    ///   8  argumentos incorrectos (<c>-androidVersionCode</c> no es un entero positivo, <c>-afVersionName</c> no es
    ///      MAYOR.MENOR.PARCHE)
    ///   9  firma inválida: <c>-afRequireSigning</c> sin firma completa, firma a medias, keystore inexistente o firma en un
    ///      ensayo
    /// </summary>
    public static class BuildRelease
    {
        /// <summary>Nombre visible del build de release (glosario §11: el nombre visible lleva la marca).</summary>
        public const string ReleaseProductName = "Tintalú";

        /// <summary>Carpeta de salida local (la misma que BuildSmoke y BuildDev, ignorada por git).</summary>
        public const string OutputDir = BuildSmoke.OutputDir;

        /// <summary>Variable de entorno alternativa a <c>-afApplicationId</c>.</summary>
        public const string ApplicationIdEnv = "AF_RELEASE_APPLICATION_ID";

        /// <summary>Ruta del keystore de subida (variable que game-ci/unity-builder pasa al contenedor).</summary>
        public const string KeystoreNameEnv = "ANDROID_KEYSTORE_NAME";

        /// <summary>Contraseña del keystore.</summary>
        public const string KeystorePassEnv = "ANDROID_KEYSTORE_PASS";

        /// <summary>Alias de la clave de subida.</summary>
        public const string KeyaliasNameEnv = "ANDROID_KEYALIAS_NAME";

        /// <summary>Contraseña de la clave de subida.</summary>
        public const string KeyaliasPassEnv = "ANDROID_KEYALIAS_PASS";

        private const string LogPrefix = "[AF.BuildRelease] ";

        private const int ExitOk = 0;
        private const int ExitSettings = 2;
        private const int ExitPlatform = 3;
        private const int ExitBuildFailed = 4;
        private const int ExitNoGameScene = 5;
        private const int ExitLocGate = 6;
        private const int ExitIdentity = 7;
        private const int ExitArgs = 8;
        private const int ExitSigning = 9;

        /// <summary>Opciones leídas de la línea de comandos.</summary>
        internal sealed class Options
        {
            public string? ApplicationId;
            public bool Rehearsal;
            public bool Apk;
            public string? CustomBuildPath;
            public int VersionCode;
            public string? VersionName;
            public bool RequireSigning;
            public string? KeystorePath;
            public string? KeystorePass;
            public string? KeyaliasName;
            public string? KeyaliasPass;
        }

        /// <summary>Firma completa y comprobada (ver <see cref="CheckSigning"/>).</summary>
        internal sealed class Signing
        {
            public Signing(string keystorePath, string keystorePass, string keyaliasName, string keyaliasPass)
            {
                KeystorePath = keystorePath;
                KeystorePass = keystorePass;
                KeyaliasName = keyaliasName;
                KeyaliasPass = keyaliasPass;
            }

            public string KeystorePath { get; }
            public string KeystorePass { get; }
            public string KeyaliasName { get; }
            public string KeyaliasPass { get; }
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
            string? error = ParseArgsWithEnv(Environment.GetCommandLineArgs(), Environment.GetEnvironmentVariable, out options);
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

        /// <summary>Como <see cref="ParseArgsWithEnv"/>, con solo <see cref="ApplicationIdEnv"/> en el entorno.</summary>
        internal static string? ParseArgs(string[] args, string? envApplicationId, out Options? options)
        {
            return ParseArgsWithEnv(args, name => name == ApplicationIdEnv ? envApplicationId : null, out options);
        }

        /// <summary>
        /// Lee <c>-afApplicationId &lt;id&gt;</c>, <c>-afRehearsal</c>, <c>-afApk</c>, <c>-afRequireSigning</c>,
        /// <c>-afVersionName &lt;M.m.p&gt;</c>, <c>-customBuildPath &lt;ruta&gt;</c>, <c>-androidVersionCode &lt;n&gt;</c> y los
        /// de la firma de game-ci (<c>-androidKeystoreName</c>, <c>-androidKeystorePass</c>, <c>-androidKeyaliasName</c>,
        /// <c>-androidKeyaliasPass</c>; su valor puede ir vacío o empezar por '-'). El ID de <see cref="ApplicationIdEnv"/>
        /// solo se usa si no hay <c>-afApplicationId</c>; en la firma mandan las variables de entorno no vacías y los
        /// argumentos son el respaldo. Devuelve el error, o null; el error nunca lleva una contraseña.
        /// </summary>
        internal static string? ParseArgsWithEnv(string[] args, Func<string, string?> env, out Options? options)
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
                else if (string.Equals(a, "-afRequireSigning", StringComparison.Ordinal))
                {
                    o.RequireSigning = true;
                }
                else if (string.Equals(a, "-androidKeystoreName", StringComparison.Ordinal)
                    || string.Equals(a, "-androidKeystorePass", StringComparison.Ordinal)
                    || string.Equals(a, "-androidKeyaliasName", StringComparison.Ordinal)
                    || string.Equals(a, "-androidKeyaliasPass", StringComparison.Ordinal))
                {
                    // game-ci pasa siempre estos cuatro, vacíos si no hay firma: el valor se toma tal cual.
                    string value = i + 1 < args.Length ? args[++i] : string.Empty;
                    if (a == "-androidKeystoreName")
                    {
                        o.KeystorePath = value;
                    }
                    else if (a == "-androidKeystorePass")
                    {
                        o.KeystorePass = value;
                    }
                    else if (a == "-androidKeyaliasName")
                    {
                        o.KeyaliasName = value;
                    }
                    else
                    {
                        o.KeyaliasPass = value;
                    }
                }
                else if (string.Equals(a, "-afApplicationId", StringComparison.Ordinal)
                    || string.Equals(a, "-afVersionName", StringComparison.Ordinal)
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
                    else if (a == "-afVersionName")
                    {
                        if (!IsValidVersionName(value))
                        {
                            return "-afVersionName '" + value + "' no es MAYOR.MENOR.PARCHE (p. ej. 1.0.0)";
                        }

                        o.VersionName = value;
                    }
                    else if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out o.VersionCode) || o.VersionCode <= 0)
                    {
                        return "-androidVersionCode '" + value + "' no es un entero positivo";
                    }
                }
            }

            string? envApplicationId = env(ApplicationIdEnv);
            if (string.IsNullOrEmpty(o.ApplicationId) && !string.IsNullOrEmpty(envApplicationId))
            {
                o.ApplicationId = envApplicationId!.Trim();
            }

            o.KeystorePath = FromEnv(env, KeystoreNameEnv, o.KeystorePath);
            o.KeystorePass = FromEnv(env, KeystorePassEnv, o.KeystorePass);
            o.KeyaliasName = FromEnv(env, KeyaliasNameEnv, o.KeyaliasName);
            o.KeyaliasPass = FromEnv(env, KeyaliasPassEnv, o.KeyaliasPass);

            options = o;
            return null;
        }

        private static string? FromEnv(Func<string, string?> env, string name, string? fallback)
        {
            string? value = env(name);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        /// <summary>MAYOR.MENOR.PARCHE: números de 1 a 4 cifras, sin ceros a la izquierda (salvo el propio 0).</summary>
        internal static bool IsValidVersionName(string value)
        {
            string[] parts = value.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            foreach (string part in parts)
            {
                if (part.Length == 0 || part.Length > 4 || (part.Length > 1 && part[0] == '0'))
                {
                    return false;
                }

                foreach (char c in part)
                {
                    if (c < '0' || c > '9')
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Comprueba la firma (paso s32). Devuelve el error, o null; <paramref name="signing"/> queda null si el build va con
        /// la clave de depuración. Los mensajes nombran las variables que faltan, nunca sus valores.
        /// </summary>
        internal static string? CheckSigning(Options o, Func<string, bool> fileExists, out Signing? signing)
        {
            signing = null;
            var missing = new List<string>();
            int present = 0;
            CountPart(o.KeystorePath, KeystoreNameEnv, missing, ref present);
            CountPart(o.KeystorePass, KeystorePassEnv, missing, ref present);
            CountPart(o.KeyaliasName, KeyaliasNameEnv, missing, ref present);
            CountPart(o.KeyaliasPass, KeyaliasPassEnv, missing, ref present);

            if (present == 0)
            {
                return o.RequireSigning ? "-afRequireSigning sin firma: faltan " + string.Join(", ", missing) : null;
            }

            if (o.Rehearsal)
            {
                return "un ensayo (-afRehearsal) nunca va firmado con la clave de subida";
            }

            if (missing.Count > 0)
            {
                return "firma a medias: faltan " + string.Join(", ", missing);
            }

            if (!fileExists(o.KeystorePath!))
            {
                return "no existe el keystore indicado en " + KeystoreNameEnv;
            }

            signing = new Signing(o.KeystorePath!, o.KeystorePass!, o.KeyaliasName!, o.KeyaliasPass!);
            return null;
        }

        private static void CountPart(string? value, string name, List<string> missing, ref int present)
        {
            if (string.IsNullOrEmpty(value))
            {
                missing.Add(name);
            }
            else
            {
                present++;
            }
        }

        /// <summary>
        /// Ajustes de firma de los Player Settings, para restaurarlos tras el build. La ruta y el alias se guardan y se
        /// restauran con su valor serializado en bruto (<c>AndroidKeystoreName</c> y <c>AndroidKeyaliasName</c> de
        /// <c>ProjectSettings.asset</c>) por <see cref="SerializedObject"/>: el setter de
        /// <c>PlayerSettings.Android.keystoreName</c> convierte "" en "{inproject}: " y dejaría un cambio en
        /// <c>ProjectSettings/</c>. Las contraseñas no se serializan nunca.
        /// </summary>
        internal readonly struct SigningSettings
        {
            private const string KeystoreNameProperty = "AndroidKeystoreName";
            private const string KeyaliasNameProperty = "AndroidKeyaliasName";

            public SigningSettings(bool useCustomKeystore, string keystoreName, string keystorePass, string keyaliasName, string keyaliasPass)
            {
                UseCustomKeystore = useCustomKeystore;
                KeystoreName = keystoreName;
                KeystorePass = keystorePass;
                KeyaliasName = keyaliasName;
                KeyaliasPass = keyaliasPass;
            }

            public bool UseCustomKeystore { get; }

            /// <summary>Valor serializado en bruto de <c>AndroidKeystoreName</c>.</summary>
            public string KeystoreName { get; }

            public string KeystorePass { get; }

            /// <summary>Valor serializado en bruto de <c>AndroidKeyaliasName</c>.</summary>
            public string KeyaliasName { get; }

            public string KeyaliasPass { get; }

            public static SigningSettings Capture()
            {
                SerializedObject so = PlayerSettingsObject();
                return new SigningSettings(
                    PlayerSettings.Android.useCustomKeystore,
                    RawString(so, KeystoreNameProperty),
                    PlayerSettings.Android.keystorePass ?? string.Empty,
                    RawString(so, KeyaliasNameProperty),
                    PlayerSettings.Android.keyaliasPass ?? string.Empty);
            }

            public void Restore()
            {
                PlayerSettings.Android.useCustomKeystore = UseCustomKeystore;
                PlayerSettings.Android.keystorePass = KeystorePass;
                PlayerSettings.Android.keyaliasPass = KeyaliasPass;
                SerializedObject so = PlayerSettingsObject();
                so.Update();
                RawProperty(so, KeystoreNameProperty).stringValue = KeystoreName;
                RawProperty(so, KeyaliasNameProperty).stringValue = KeyaliasName;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            private static SerializedObject PlayerSettingsObject()
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (assets.Length == 0 || assets[0] == null)
                {
                    throw new InvalidOperationException("no se pudo cargar ProjectSettings/ProjectSettings.asset");
                }

                return new SerializedObject(assets[0]);
            }

            private static SerializedProperty RawProperty(SerializedObject so, string name)
            {
                SerializedProperty? property = so.FindProperty(name);
                if (property == null || property.propertyType != SerializedPropertyType.String)
                {
                    throw new InvalidOperationException("PlayerSettings sin la propiedad de texto " + name);
                }

                return property;
            }

            private static string RawString(SerializedObject so, string name)
            {
                return RawProperty(so, name).stringValue ?? string.Empty;
            }
        }

        /// <summary>Pone la clave de subida en los Player Settings (en memoria, durante el build) y devuelve los anteriores.</summary>
        internal static SigningSettings ApplySigning(Signing signing)
        {
            SigningSettings previous = SigningSettings.Capture();
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(signing.KeystorePath).Replace('\\', '/');
            PlayerSettings.Android.keystorePass = signing.KeystorePass;
            PlayerSettings.Android.keyaliasName = signing.KeyaliasName;
            PlayerSettings.Android.keyaliasPass = signing.KeyaliasPass;
            return previous;
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

            string? signingError = CheckSigning(o, File.Exists, out Signing? signing);
            if (signingError != null)
            {
                Debug.LogError(LogPrefix + signingError + ".");
                return ExitSigning;
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
            string previousVersionName = PlayerSettings.bundleVersion;
            bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
            SigningSettings? previousSigning = null;
            try
            {
                if (signing != null)
                {
                    previousSigning = ApplySigning(signing);
                }

                if (!string.IsNullOrEmpty(o.VersionName))
                {
                    PlayerSettings.bundleVersion = o.VersionName;
                }

                if (!o.Rehearsal)
                {
                    PlayerSettings.SetApplicationIdentifier(android, applicationId);
                    PlayerSettings.productName = ReleaseProductName;
                }

                if (o.VersionCode > 0)
                {
                    PlayerSettings.Android.bundleVersionCode = o.VersionCode;
                }

                string kind = o.Rehearsal ? "ENSAYO (identidad de prueba). "
                    : signing != null ? "RELEASE firmado con la clave de subida. " : "RELEASE sin firma (clave de depuracion). ";
                Debug.Log(LogPrefix + kind
                    + ProjectSetup.Describe() + ", versionName=" + PlayerSettings.bundleVersion
                    + ", versionCode=" + PlayerSettings.Android.bundleVersionCode
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
                previousSigning?.Restore();
                PlayerSettings.bundleVersion = previousVersionName;
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
