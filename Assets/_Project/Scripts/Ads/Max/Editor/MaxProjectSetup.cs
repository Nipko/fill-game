#nullable enable
using System;
using AppLovinMax.Scripts.IntegrationManager.Editor;
using UnityEditor;
using UnityEngine;

namespace AF.Ads.Max.Editor
{
    /// <summary>
    /// Los ajustes versionados del plugin de AppLovin MAX 8.6.6 (D-042), aplicados por código en lugar de a mano en el Integration
    /// Manager (regla dura 3). Se lanza con
    /// <c>unity run &lt;repo&gt; ... -- -nographics -executeMethod AF.Ads.Max.Editor.MaxProjectSetup.Apply</c> y deja:
    /// <list type="bullet">
    /// <item><c>Assets/MaxSdk/Resources/AppLovinSettings.asset</c> (lo crea el plugin): <b>Quality Service apagado</b> (el plugin de
    /// Gradle de AppLovin manda la clave del SDK a <c>api2.safedk.com</c> en cada build, instrumenta la app y puede romper el build con
    /// AGP 9, 08 §7 #1) y la clave del SDK <b>vacía</b> (la pone <see cref="MaxBuildConfig"/> en el build desde fuera del repositorio).</item>
    /// <item><c>ProjectSettings/AppLovinInternalSettings.json</c> (JSON del plugin): el flujo de términos y privacidad de MAX con la
    /// automatización de Google UMP <b>apagado</b> desde D-049 (los anuncios van con AdMob directo y su UMP, y no debe haber dos avisos
    /// de consentimiento; MAX queda instalado sin servir), con las URL de la política y los términos de la web [P] por si vuelve, sin
    /// la alerta previa de términos en el RGPD y sin geografía de depuración.</item>
    /// </list>
    /// Escribe <c>[AF.MaxProjectSetup] OK</c> al terminar.
    /// </summary>
    public static class MaxProjectSetup
    {
        /// <summary>Política de privacidad que enlaza el mensaje de UMP [P] (la web de <c>infra/web</c>, D-041).</summary>
        public const string PrivacyPolicyUrl = "https://tintalu.com/privacidad/";

        /// <summary>Términos que enlaza el flujo de MAX [P].</summary>
        public const string TermsOfServiceUrl = "https://tintalu.com/terminos/";

        private const string LogPrefix = "[AF.MaxProjectSetup] ";

        /// <summary>Aplica y comprueba los ajustes.</summary>
        [MenuItem("Amazon Fill/CI/Aplicar ajustes de AppLovin MAX")]
        public static void Apply()
        {
            AppLovinSettings settings = AppLovinSettings.Instance;
            settings.QualityServiceEnabled = false;
            settings.SdkKey = string.Empty;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            AppLovinInternalSettings internalSettings = AppLovinInternalSettings.Instance;
            internalSettings.ConsentFlowEnabled = false; // D-049: un solo UMP, el de Google Mobile Ads
            internalSettings.ConsentFlowPrivacyPolicyUrl = PrivacyPolicyUrl;
            internalSettings.ConsentFlowTermsOfServiceUrl = TermsOfServiceUrl;
            internalSettings.ShouldShowTermsAndPrivacyPolicyAlertInGDPR = false;
            internalSettings.DebugUserGeography = MaxSdkBase.ConsentFlowUserGeography.Unknown;
            internalSettings.Save();

            string? problem = Validate();
            if (problem != null)
            {
                throw new InvalidOperationException(LogPrefix + problem);
            }

            Debug.Log(LogPrefix + "OK. Quality Service apagado, clave vacia en el repositorio, flujo de UMP de MAX apagado (D-049).");
        }

        /// <summary>Null si los ajustes del proyecto son los de D-042 con el cambio de D-049; si no, qué falla.</summary>
        public static string? Validate()
        {
            AppLovinSettings settings = AppLovinSettings.Instance;
            AppLovinInternalSettings internalSettings = AppLovinInternalSettings.Instance;
            return Validate(settings.QualityServiceEnabled, settings.SdkKey, internalSettings.ConsentFlowEnabled,
                internalSettings.ConsentFlowPrivacyPolicyUrl);
        }

        /// <summary>
        /// La regla, sin tocar los archivos: Quality Service apagado, <b>ninguna clave del SDK en <c>AppLovinSettings.asset</c></b> (la clave
        /// solo entra en el build, por <see cref="MaxBuildConfig"/>; nunca se escribe en el Integration Manager) y el flujo de UMP de
        /// MAX apagado (D-049: el UMP es el de Google Mobile Ads), con la URL de la política guardada.
        /// </summary>
        public static string? Validate(bool qualityServiceEnabled, string? sdkKey, bool consentFlowEnabled, string? privacyPolicyUrl)
        {
            if (!string.IsNullOrEmpty(sdkKey))
            {
                return "AppLovinSettings.asset lleva una clave del SDK: no se escribe en el Integration Manager; va en "
                    + "Assets/_LocalSecrets/applovin-max.json o en AF_MAX_SDK_KEY.";
            }

            if (qualityServiceEnabled)
            {
                return "Quality Service encendido (manda la clave a safedk.com en cada build).";
            }

            if (consentFlowEnabled)
            {
                return "flujo de consentimiento (UMP) de MAX encendido: con D-049 el UMP es el de Google Mobile Ads (dos avisos).";
            }

            if (privacyPolicyUrl != PrivacyPolicyUrl)
            {
                return "sin la URL de la politica en el flujo de MAX.";
            }

            return null;
        }

        /// <summary>
        /// Compuerta de <c>BuildDev</c> y <c>BuildRelease</c>: false (y el error en el log, sin la clave) si los ajustes de MAX no son los
        /// de D-042; el build se para con el código de «ajustes distintos de los fijados».
        /// </summary>
        public static bool CheckForBuild(string logPrefix)
        {
            string? problem = Validate();
            if (problem == null)
            {
                return true;
            }

            Debug.LogError(logPrefix + "ajustes de AppLovin MAX distintos de los fijados: " + problem
                + " Ejecuta AF.Ads.Max.Editor.MaxProjectSetup.Apply.");
            return false;
        }
    }

    /// <summary>
    /// Que el plugin de MAX no se actualice solo (D-042: versión fijada 8.6.6, regla dura 4): al cargar el Editor apaga su
    /// actualizador automático (<c>com.applovin.auto_update_enabled</c>, preferencia del Editor de esta máquina) y marca la comprobación
    /// diaria como hecha, para que no pregunte ni descargue otra versión.
    /// </summary>
    [InitializeOnLoad]
    internal static class MaxEditorGuard
    {
        static MaxEditorGuard()
        {
            EditorPrefs.SetBool(AppLovinAutoUpdater.KeyAutoUpdateEnabled, false);
            EditorPrefs.SetInt("com.applovin.last_update_check_time_v2", (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
        }
    }
}
