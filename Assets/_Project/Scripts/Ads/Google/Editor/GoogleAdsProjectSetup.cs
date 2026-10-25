#nullable enable
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AF.Ads.Google.Editor
{
    /// <summary>
    /// Los ajustes versionados del plugin Google Mobile Ads 11.5.0 (D-049), aplicados por código en lugar de a mano en
    /// Assets › Google Mobile Ads › Settings (regla dura 3). Se lanza con
    /// <c>unity run &lt;repo&gt; ... -- -nographics -executeMethod AF.Ads.Google.Editor.GoogleAdsProjectSetup.Apply</c> (o desde el menú) y
    /// deja <c>Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset</c> (lo crea el plugin con su <c>LoadInstance</c>) con:
    /// <list type="bullet">
    /// <item>el <b>App ID de Android</b> <see cref="GoogleAdUnits.AndroidAppId"/> (no es secreto; el pre-proceso del plugin lo escribe en
    /// el manifiesto, y sin él el build se para y <c>MobileAdsInitProvider</c> cerraría la app al arrancar);</item>
    /// <item>el SDK Android <b>Standard</b> (<c>play-services-ads</c> 25.4.0, no el «Next-Gen»), el pre-proceso de Gradle encendido y
    /// las optimizaciones de inicialización y carga del SDK encendidas (los valores por defecto del plugin).</item>
    /// </list>
    /// <c>BuildDev</c> y <c>BuildRelease</c> llaman a <see cref="CheckForBuild"/> antes de construir: si el asset no está como debe,
    /// lo aplica y, si aun así falla, paran el build. Escribe <c>[AF.GoogleAdsProjectSetup] OK</c> al terminar.
    /// </summary>
    public static class GoogleAdsProjectSetup
    {
        /// <summary>Dónde crea el plugin su asset de ajustes.</summary>
        public const string SettingsAssetPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

        /// <summary>La clase (interna) de ajustes del plugin.</summary>
        public const string SettingsTypeName = "GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor";

        private const string LogPrefix = "[AF.GoogleAdsProjectSetup] ";

        /// <summary>Aplica y comprueba los ajustes.</summary>
        [MenuItem("Amazon Fill/CI/Aplicar ajustes de Google Mobile Ads")]
        public static void Apply()
        {
            ScriptableObject settings = LoadOrCreate();
            var so = new SerializedObject(settings);
            Set(so, "adMobAndroidAppId", GoogleAdUnits.AndroidAppId);
            Set(so, "enableGradleBuildPreProcessor", true);
            Set(so, "enableKotlinXCoroutinesPackagingOption", true);
            Set(so, "disableOptimizeInitialization", false);
            Set(so, "disableOptimizeAdLoading", false);
            Set(so, "overrideDefaultGmaAndroidSdk", false);
            SetInt(so, "selectedGmaAndroidSdk", 0); // Standard
            if (so.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            string? problem = Validate();
            if (problem != null)
            {
                throw new InvalidOperationException(LogPrefix + problem);
            }

            Debug.Log(LogPrefix + "OK. App ID de Android fijado, SDK Standard, pre-proceso de Gradle encendido.");
        }

        /// <summary>Null si el asset del proyecto tiene los ajustes de D-049; si no, qué falla.</summary>
        public static string? Validate()
        {
            ScriptableObject? settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsAssetPath);
            if (settings == null)
            {
                return "falta " + SettingsAssetPath + ".";
            }

            var so = new SerializedObject(settings);
            return Validate(
                Str(so, "adMobAndroidAppId"),
                Bool(so, "enableGradleBuildPreProcessor"),
                Bool(so, "overrideDefaultGmaAndroidSdk"),
                Int(so, "selectedGmaAndroidSdk"));
        }

        /// <summary>
        /// La regla, sin tocar archivos: el App ID de Android es exactamente <see cref="GoogleAdUnits.AndroidAppId"/>, el pre-proceso
        /// de Gradle está encendido (resuelve las dependencias con EDM4U en cada build) y el SDK es el Standard.
        /// </summary>
        public static string? Validate(string? androidAppId, bool gradlePreProcessor, bool overrideSdk, int selectedSdk)
        {
            if (string.IsNullOrEmpty(androidAppId))
            {
                return "App ID de Android vacio (el build saldria sin anuncios y MobileAdsInitProvider cerraria la app).";
            }

            if (!string.Equals(androidAppId, GoogleAdUnits.AndroidAppId, StringComparison.Ordinal))
            {
                return "App ID de Android distinto del de AdMob de Tintalu (D-049).";
            }

            if (!gradlePreProcessor)
            {
                return "pre-proceso de Gradle del plugin apagado.";
            }

            if (overrideSdk && selectedSdk != 0)
            {
                return "SDK Android Next-Gen elegido (D-049 aprueba play-services-ads 25.4.0, el Standard).";
            }

            return null;
        }

        /// <summary>
        /// Compuerta de <c>BuildDev</c> y <c>BuildRelease</c>: si el asset no tiene los ajustes de D-049, los aplica; false (y el error
        /// en el log) si aun así no quedan, para que el build nunca salga con el App ID vacío.
        /// </summary>
        public static bool CheckForBuild(string logPrefix)
        {
            string? problem = Validate();
            if (problem != null)
            {
                Debug.LogWarning(logPrefix + "ajustes de Google Mobile Ads distintos de los fijados (" + problem + "): se aplican.");
                try
                {
                    Apply();
                }
                catch (Exception e)
                {
                    Debug.LogError(logPrefix + e.Message);
                }

                problem = Validate();
            }

            if (problem == null)
            {
                return true;
            }

            Debug.LogError(logPrefix + "ajustes de Google Mobile Ads distintos de los fijados: " + problem
                + " Ejecuta AF.Ads.Google.Editor.GoogleAdsProjectSetup.Apply.");
            return false;
        }

        private static ScriptableObject LoadOrCreate()
        {
            ScriptableObject? existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsAssetPath);
            if (existing != null)
            {
                return existing;
            }

            Type type = Type.GetType(SettingsTypeName, throwOnError: false)
                ?? throw new InvalidOperationException(LogPrefix + "no se encuentra " + SettingsTypeName + " (¿falta el paquete com.google.ads.mobile?).");
            MethodInfo load = type.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException(LogPrefix + "el plugin no tiene GoogleMobileAdsSettings.LoadInstance.");
            return (ScriptableObject)load.Invoke(null, null);
        }

        private static SerializedProperty Prop(SerializedObject so, string name)
        {
            return so.FindProperty(name) ?? throw new InvalidOperationException(LogPrefix + "el asset no tiene el campo " + name + ".");
        }

        private static void Set(SerializedObject so, string name, string value) => Prop(so, name).stringValue = value;

        private static void Set(SerializedObject so, string name, bool value) => Prop(so, name).boolValue = value;

        private static void SetInt(SerializedObject so, string name, int value) => Prop(so, name).intValue = value;

        private static string? Str(SerializedObject so, string name) => so.FindProperty(name)?.stringValue;

        private static bool Bool(SerializedObject so, string name) => so.FindProperty(name)?.boolValue ?? false;

        private static int Int(SerializedObject so, string name) => so.FindProperty(name)?.intValue ?? 0;
    }
}
