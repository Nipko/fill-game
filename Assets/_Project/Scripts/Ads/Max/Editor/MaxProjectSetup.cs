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
    /// automatización de Google UMP (08 §7 #2) encendido, con las URL de la política y los términos de la web [P], sin la alerta previa
    /// de términos en el RGPD y sin geografía de depuración.</item>
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
            internalSettings.ConsentFlowEnabled = true;
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

            Debug.Log(LogPrefix + "OK. Quality Service apagado, clave vacia en el repositorio, flujo de UMP encendido.");
        }

        /// <summary>Null si los ajustes son los de D-042; si no, qué falla.</summary>
        public static string? Validate()
        {
            AppLovinSettings settings = AppLovinSettings.Instance;
            if (settings.QualityServiceEnabled)
            {
                return "Quality Service encendido.";
            }

            if (!string.IsNullOrEmpty(settings.SdkKey))
            {
                return "AppLovinSettings lleva una clave del SDK: debe ir fuera del repositorio.";
            }

            AppLovinInternalSettings internalSettings = AppLovinInternalSettings.Instance;
            if (!internalSettings.ConsentFlowEnabled || internalSettings.ConsentFlowPrivacyPolicyUrl != PrivacyPolicyUrl)
            {
                return "flujo de consentimiento (UMP) apagado o sin la URL de la politica.";
            }

            return null;
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
