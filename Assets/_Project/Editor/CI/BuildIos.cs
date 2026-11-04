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

        // Mientras ExportXcode construye un Development Build (lo mira el post-proceso).
        private static bool s_development;

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
            BuildReport report;
            s_development = true; // el post-proceso añade el .storekit local (solo desarrollo)
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                s_development = false;
            }

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
            ApplyConsentDefaults(pathToBuiltProject);
            WriteTrackingUsageDescription(pathToBuiltProject);
            if (s_development)
            {
                WriteLocalStoreKit(pathToBuiltProject);
            }

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

        /// <summary>Configuración local de StoreKit del proyecto de Xcode de desarrollo (fase M3 de <c>mac-ios.md</c>).</summary>
        public const string StoreKitFileName = "Tintalu.storekit";

        /// <summary>
        /// Solo desarrollo: escribe <see cref="StoreKitFileName"/> con los productos del catálogo del juego (<c>StoreCatalog.Default</c>:
        /// el SKU, consumible o no y el precio en USD) y lo engancha al esquema de Xcode, con la variable <c>af_storekit_local</c>
        /// (<c>GameBootstrap.IntentExtraStoreKitLocal</c>): lanzado con ▶ desde Xcode, el juego compra con StoreKit 2 contra este archivo,
        /// sin el App Store, y el backend falso acredita. Lanzado con <c>devicectl</c>, StoreKit no lo usa (es una función de Xcode).
        /// </summary>
        internal static void WriteLocalStoreKit(string pathToBuiltProject)
        {
            File.WriteAllText(Path.Combine(pathToBuiltProject, StoreKitFileName), StoreKitJson(AF.IAP.StoreCatalog.Default));
#if UNITY_IOS
            // Xcode solo aplica un .storekit que es parte del proyecto (en el navegador), no uno que solo nombra el esquema. Sin destino:
            // no va dentro de la app. (UnityEditor.iOS.Xcode existe con el módulo iOS; en Windows sin él, esto no se compila.)
            string pbxPath = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var pbx = new UnityEditor.iOS.Xcode.PBXProject();
            pbx.ReadFromFile(pbxPath);
            if (pbx.FindFileGuidByProjectPath(StoreKitFileName) == null)
            {
                pbx.AddFile(StoreKitFileName, StoreKitFileName, UnityEditor.iOS.Xcode.PBXSourceTree.Source);
                pbx.WriteToFile(pbxPath);
            }
#endif
            string scheme = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/xcshareddata/xcschemes/Unity-iPhone.xcscheme");
            if (!File.Exists(scheme))
            {
                Debug.LogWarning(LogPrefix + "sin el esquema " + scheme + ": el .storekit queda sin enganchar.");
                return;
            }

            string text = File.ReadAllText(scheme);
            if (!text.Contains(StoreKitFileName))
            {
                const string Launch = "<LaunchAction ";
                int at = text.IndexOf(Launch, StringComparison.Ordinal);
                int open = at < 0 ? -1 : text.IndexOf('>', at);
                if (open < 0)
                {
                    throw new InvalidOperationException(LogPrefix + "esquema de Xcode sin <LaunchAction>: " + scheme);
                }

                string inner = "\n      <StoreKitConfigurationFileReference identifier = \"../../" + StoreKitFileName + "\">\n"
                    + "      </StoreKitConfigurationFileReference>\n"
                    + "      <EnvironmentVariables>\n"
                    + "         <EnvironmentVariable key = \"af_storekit_local\" value = \"1\" isEnabled = \"YES\">\n"
                    + "         </EnvironmentVariable>\n"
                    + "      </EnvironmentVariables>";
                text = text.Substring(0, open + 1) + inner + text.Substring(open + 1);
                File.WriteAllText(scheme, text);
            }

            Debug.Log(LogPrefix + StoreKitFileName + ": " + AF.IAP.StoreCatalog.Default.Products.Count + " productos, enganchado al esquema (af_storekit_local).");
        }

        /// <summary>El <c>.storekit</c> (formato 4.0 de Xcode) con los productos de <paramref name="catalog"/>, en USD y en inglés.</summary>
        internal static string StoreKitJson(AF.IAP.StoreCatalog catalog)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{\n  \"identifier\" : \"5A7B0C1D-AF00-4F00-8000-7417A1A0D0E1\",\n  \"nonRenewingSubscriptions\" : [],\n  \"products\" : [\n");
            int n = 0;
            foreach (AF.IAP.StoreProduct p in catalog.Products)
            {
                if (n > 0)
                {
                    sb.Append(",\n");
                }

                n++;
                string type = p.Kind == AF.IAP.ProductKind.NonConsumable ? "NonConsumable" : "Consumable";
                sb.Append("    {\n")
                    .Append("      \"displayPrice\" : \"").Append(p.UsdPrice.ToString("0.00", CultureInfo.InvariantCulture)).Append("\",\n")
                    .Append("      \"familyShareable\" : false,\n")
                    .Append("      \"internalID\" : \"").Append((6700000000 + n).ToString(CultureInfo.InvariantCulture)).Append("\",\n")
                    .Append("      \"localizations\" : [{ \"description\" : \"").Append(p.Sku).Append("\", \"displayName\" : \"").Append(p.Sku)
                    .Append("\", \"locale\" : \"en_US\" }],\n")
                    .Append("      \"productID\" : \"").Append(p.Sku).Append("\",\n")
                    .Append("      \"referenceName\" : \"").Append(p.Sku).Append("\",\n")
                    .Append("      \"type\" : \"").Append(type).Append("\"\n")
                    .Append("    }");
            }

            sb.Append("\n  ],\n  \"settings\" : { \"_locale\" : \"en_US\", \"_storefront\" : \"USA\" },\n  \"subscriptionGroups\" : [],\n")
                .Append("  \"version\" : { \"major\" : 4, \"minor\" : 0 }\n}\n");
            return sb.ToString();
        }

        /// <summary>Clave de <c>strings.csv</c> con el texto que iOS pone dentro de su aviso de rastreo (ATT, fase M3).</summary>
        public const string TrackingUsageKey = "att_usage_description";

        /// <summary>Tabla de textos de la UI (contrato 32).</summary>
        public const string StringsCsvPath = "Assets/_Project/Data/Loc/strings.csv";

        /// <summary>
        /// Carpeta <c>.lproj</c> de cada idioma del juego (D-018) con su columna de <c>strings.csv</c>. El español va dos veces:
        /// <c>es-419</c> y <c>es</c>, para que un iPhone en español de España (u otro sin fila propia) no caiga al inglés.
        /// </summary>
        public static readonly KeyValuePair<string, string>[] TrackingLocales =
        {
            new KeyValuePair<string, string>("es-419", "es-419"),
            new KeyValuePair<string, string>("es", "es-419"),
            new KeyValuePair<string, string>("en", "en"),
            new KeyValuePair<string, string>("pt-BR", "pt-BR"),
        };

        /// <summary>
        /// <c>NSUserTrackingUsageDescription</c> (sin ella, pedir el ATT cierra la app): en el <c>Info.plist</c>, en inglés (el idioma
        /// base del proyecto de Unity), y en <c>&lt;idioma&gt;.lproj/InfoPlist.strings</c> de cada idioma, desde
        /// <see cref="TrackingUsageKey"/> de <c>strings.csv</c> (los textos no viven en el código). Las carpetas entran en el proyecto
        /// de Xcode como referencias de carpeta, para que la app las lleve tal cual. Falla cerrado si falta un texto.
        /// </summary>
        internal static void WriteTrackingUsageDescription(string pathToBuiltProject)
        {
            Dictionary<string, string> texts = TrackingUsageTexts(File.ReadAllBytes(StringsCsvPath));
            string plist = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plist))
            {
                throw new InvalidOperationException(LogPrefix + "falta " + plist + ".");
            }

            File.WriteAllText(plist, SetPlistString(File.ReadAllText(plist), "NSUserTrackingUsageDescription", texts["en"]));
#if UNITY_IOS
            string pbxPath = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var pbx = new UnityEditor.iOS.Xcode.PBXProject();
            pbx.ReadFromFile(pbxPath);
            string target = pbx.GetUnityMainTargetGuid();
#endif
            foreach (KeyValuePair<string, string> locale in TrackingLocales)
            {
                string folder = locale.Key + ".lproj";
                Directory.CreateDirectory(Path.Combine(pathToBuiltProject, folder));
                File.WriteAllText(Path.Combine(pathToBuiltProject, folder, "InfoPlist.strings"),
                    "/* " + TrackingUsageKey + " (strings.csv): lo escribe AF.EditorTools.BuildIos */\n"
                    + "\"NSUserTrackingUsageDescription\" = \"" + EscapeStrings(texts[locale.Value]) + "\";\n",
                    new System.Text.UTF8Encoding(false));
#if UNITY_IOS
                if (pbx.FindFileGuidByProjectPath(folder) == null)
                {
                    string guid = pbx.AddFolderReference(folder, folder);
                    pbx.AddFileToBuild(target, guid);
                }
#endif
            }

#if UNITY_IOS
            pbx.WriteToFile(pbxPath);
#endif
            Debug.Log(LogPrefix + "NSUserTrackingUsageDescription: Info.plist (en) e InfoPlist.strings de " + TrackingLocales.Length + " idiomas.");
        }

        /// <summary>El texto de <see cref="TrackingUsageKey"/> por columna de idioma; lanza si falta la fila o una traducción.</summary>
        public static Dictionary<string, string> TrackingUsageTexts(byte[] csv)
        {
            IReadOnlyList<AF.Localization.LocCsvRow> rows = AF.Localization.LocCsv.Parse(AF.Localization.LocCsv.Decode(csv, StringsCsvPath), StringsCsvPath);
            IReadOnlyList<string> header = rows[0].Fields;
            foreach (AF.Localization.LocCsvRow row in rows)
            {
                if (row.Fields.Count == 0 || row.Fields[0] != TrackingUsageKey)
                {
                    continue;
                }

                var texts = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> locale in TrackingLocales)
                {
                    int column = -1;
                    for (int i = 0; i < header.Count; i++)
                    {
                        if (header[i] == locale.Value)
                        {
                            column = i;
                            break;
                        }
                    }

                    string text = column >= 0 && column < row.Fields.Count ? row.Fields[column] : string.Empty;
                    if (text.Length == 0)
                    {
                        throw new InvalidOperationException(LogPrefix + TrackingUsageKey + " sin texto en " + locale.Value + " (strings.csv).");
                    }

                    texts[locale.Value] = text;
                }

                return texts;
            }

            throw new InvalidOperationException(LogPrefix + "strings.csv sin la fila " + TrackingUsageKey + ".");
        }

        // Comillas y barras escapadas para un .strings; los saltos de línea, como \n.
        private static string EscapeStrings(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }

        /// <summary>La clave de texto con su valor (escapado para XML): la cambia si está; si no, la añade antes del último &lt;/dict&gt;.</summary>
        public static string SetPlistString(string plist, string key, string value)
        {
            string escaped = System.Security.SecurityElement.Escape(value) ?? string.Empty;
            var existing = new System.Text.RegularExpressions.Regex(
                "<key>" + System.Text.RegularExpressions.Regex.Escape(key) + @"</key>\s*<string>[^<]*</string>");
            if (existing.IsMatch(plist))
            {
                return existing.Replace(plist, _ => "<key>" + key + "</key>\n    <string>" + escaped + "</string>", 1);
            }

            int end = plist.LastIndexOf("</dict>", StringComparison.Ordinal);
            if (end < 0)
            {
                throw new InvalidOperationException(LogPrefix + "Info.plist sin </dict>.");
            }

            return plist.Substring(0, end) + "  <key>" + key + "</key>\n  <string>" + escaped + "</string>\n" + plist.Substring(end);
        }

        /// <summary>
        /// Lo que el <c>Info.plist</c> deja apagado hasta el consentimiento (08 §2.3 y §7; 05 §7): las mismas claves que los
        /// <c>meta-data</c> de <c>Assets/Plugins/Android/LauncherManifest.xml</c>, con sus nombres de iOS. El juego enciende Analytics y
        /// Crashlytics desde C# cuando el jugador consiente (<c>FirebaseAnalyticsSink</c>), como en Android. Además, sin el IDFV
        /// [P]: es el identificador de iOS que Android no tiene, y se apaga como el del anuncio.
        /// </summary>
        public static readonly KeyValuePair<string, bool>[] ConsentDefaults =
        {
            new KeyValuePair<string, bool>("FIREBASE_ANALYTICS_COLLECTION_ENABLED", false),
            new KeyValuePair<string, bool>("GOOGLE_ANALYTICS_ADID_COLLECTION_ENABLED", false),
            new KeyValuePair<string, bool>("GOOGLE_ANALYTICS_IDFV_COLLECTION_ENABLED", false),
            new KeyValuePair<string, bool>("GOOGLE_ANALYTICS_DEFAULT_ALLOW_ANALYTICS_STORAGE", false),
            new KeyValuePair<string, bool>("GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_STORAGE", false),
            new KeyValuePair<string, bool>("GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_USER_DATA", false),
            new KeyValuePair<string, bool>("GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_PERSONALIZATION_SIGNALS", false),
            new KeyValuePair<string, bool>("FirebaseCrashlyticsCollectionEnabled", false),
            new KeyValuePair<string, bool>("GADDelayAppMeasurementInit", true),
        };

        /// <summary>
        /// El <c>meta-data</c> de Android (<c>LauncherManifest.xml</c>) del que sale cada clave de <see cref="ConsentDefaults"/>, o null
        /// si es solo de iOS (el IDFV). Lo comprueba <c>IosConsentDefaultsTests</c>: un cambio en Android no deja atrás a iOS.
        /// </summary>
        public static readonly Dictionary<string, string?> AndroidMetaDataFor = new Dictionary<string, string?>
        {
            { "FIREBASE_ANALYTICS_COLLECTION_ENABLED", "firebase_analytics_collection_enabled" },
            { "GOOGLE_ANALYTICS_ADID_COLLECTION_ENABLED", "google_analytics_adid_collection_enabled" },
            { "GOOGLE_ANALYTICS_IDFV_COLLECTION_ENABLED", null },
            { "GOOGLE_ANALYTICS_DEFAULT_ALLOW_ANALYTICS_STORAGE", "google_analytics_default_allow_analytics_storage" },
            { "GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_STORAGE", "google_analytics_default_allow_ad_storage" },
            { "GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_USER_DATA", "google_analytics_default_allow_ad_user_data" },
            { "GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_PERSONALIZATION_SIGNALS", "google_analytics_default_allow_ad_personalization_signals" },
            { "FirebaseCrashlyticsCollectionEnabled", "firebase_crashlytics_collection_enabled" },
            { "GADDelayAppMeasurementInit", "com.google.android.gms.ads.DELAY_APP_MEASUREMENT_INIT" },
        };

        /// <summary>
        /// Escribe <see cref="ConsentDefaults"/> en el <c>Info.plist</c> (sustituye el valor si la clave ya estaba). Sin el
        /// <c>Info.plist</c>, lanza: un build sin estas claves recogería datos antes del consentimiento.
        /// </summary>
        internal static void ApplyConsentDefaults(string pathToBuiltProject)
        {
            string plist = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plist))
            {
                // Falla cerrado (08 §7): sin estas claves el build saldría con la recogida encendida por defecto.
                throw new InvalidOperationException(LogPrefix + "falta " + plist + ": sin las claves de consentimiento no hay build.");
            }

            string text = File.ReadAllText(plist);
            foreach (KeyValuePair<string, bool> entry in ConsentDefaults)
            {
                text = SetPlistBool(text, entry.Key, entry.Value);
            }

            File.WriteAllText(plist, text);
            Debug.Log(LogPrefix + "Info.plist: " + ConsentDefaults.Length + " claves de consentimiento (Analytics, IDFA/IDFV y Crashlytics apagados hasta el consentimiento).");
        }

        // La clave booleana con su valor: la cambia si está; si no, la añade antes del último </dict>.
        public static string SetPlistBool(string plist, string key, bool value)
        {
            string tag = value ? "<true/>" : "<false/>";
            var existing = new System.Text.RegularExpressions.Regex(
                "<key>" + System.Text.RegularExpressions.Regex.Escape(key) + @"</key>\s*<(true|false)\s*/>");
            if (existing.IsMatch(plist))
            {
                return existing.Replace(plist, "<key>" + key + "</key>\n    " + tag, 1);
            }

            int end = plist.LastIndexOf("</dict>", StringComparison.Ordinal);
            if (end < 0)
            {
                throw new InvalidOperationException(LogPrefix + "Info.plist sin </dict>.");
            }

            return plist.Substring(0, end) + "  <key>" + key + "</key>\n  " + tag + "\n" + plist.Substring(end);
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
