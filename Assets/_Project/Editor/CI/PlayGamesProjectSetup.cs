#nullable enable
using System;
using System.IO;
using GooglePlayGames.Editor;
using UnityEditor;
using UnityEngine;

namespace AF.EditorTools
{
    /// <summary>
    /// El <c>APP_ID</c> de Play Games (D-042; <c>docs/dev/play-games.md</c> §9, paso 3) aplicado por código, como
    /// <c>Window › Google Play Games › Setup › Android setup</c> con los recursos de Play Console y <b>sin Web Client ID</b>
    /// (regla dura 3: los archivos los escribe el plugin, no se editan a mano). Se lanza con
    /// <c>unity run &lt;repo&gt; ... -- -nographics -buildTarget Android -executeMethod AF.EditorTools.PlayGamesProjectSetup.Apply</c>
    /// (o desde el menú) y se puede repetir. Llama a <c>GPGSAndroidSetupUI.PerformSetup(webClientId, appId, nearbySvcId)</c>, el
    /// punto de entrada del plugin para builds automáticos, que escribe:
    /// <list type="bullet">
    /// <item><c>ProjectSettings/GooglePlayGameSettings.txt</c> (<c>proj.AppId</c>, <c>and.ClientId</c> vacío, Setup hecho);</item>
    /// <item><c>Assets/GooglePlayGames/Resources/PlayGamesSettings.asset</c> (<c>mAppId</c>);</item>
    /// <item><c>Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml</c> (la meta-data
    /// <c>com.google.android.gms.games.APP_ID</c>).</item>
    /// </list>
    /// No usa la variante con el XML de recursos porque esa llama a <c>CheckBundleId</c>, que, con el <c>package_name</c> del XML
    /// distinto del de los builds de prueba, cambiaría el <c>applicationId</c> del proyecto al definitivo (en batchmode el diálogo
    /// no pregunta): el definitivo solo vive en la variable de CI (regla dura 6). Tampoco hace falta su resolución de EDM4U ni su
    /// clase de constantes (el XML no trae logros ni marcadores). Sin Nearby (D-042): <c>nearbySvcId</c> nulo.
    /// Escribe <c>[AF.PlayGamesProjectSetup] OK</c> al terminar.
    /// </summary>
    public static class PlayGamesProjectSetup
    {
        /// <summary>El <c>app_id</c> del proyecto de Play Games de Tintalú en Play Console (no es secreto: va en el manifiesto).</summary>
        public const string AppId = "459140125724";

        /// <summary>Dónde escribe el plugin su manifiesto.</summary>
        public const string ManifestPath = "Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml";

        /// <summary>Dónde escribe el plugin su asset de ajustes (lo lee en el teléfono <c>PlayGamesSettings.LoadInstance</c>).</summary>
        public const string SettingsAssetPath = "Assets/GooglePlayGames/Resources/PlayGamesSettings.asset";

        /// <summary>Los ajustes de proyecto del plugin.</summary>
        public const string ProjectSettingsPath = "ProjectSettings/GooglePlayGameSettings.txt";

        private const string LogPrefix = "[AF.PlayGamesProjectSetup] ";

        /// <summary>Aplica el Setup y lo comprueba; lanza si no queda como debe.</summary>
        [MenuItem("Amazon Fill/CI/Aplicar el APP_ID de Play Games")]
        public static void Apply()
        {
            if (!GPGSAndroidSetupUI.PerformSetup(string.Empty, AppId, null))
            {
                throw new InvalidOperationException(LogPrefix + "el Setup del plugin de Play Games ha fallado (mira el log: ¿SDK de Android?).");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string? problem = Validate();
            if (problem != null)
            {
                throw new InvalidOperationException(LogPrefix + problem);
            }

            Debug.Log(LogPrefix + "OK. APP_ID de Play Games " + AppId + ", sin Web Client ID ni Nearby.");
        }

        /// <summary>Null si los archivos versionados del plugin llevan el <c>APP_ID</c>, sin Web Client ID ni Nearby; si no, qué falla.</summary>
        public static string? Validate()
        {
            if (!File.Exists(ManifestPath))
            {
                return "falta " + ManifestPath + ".";
            }

            if (!File.Exists(SettingsAssetPath))
            {
                return "falta " + SettingsAssetPath + ".";
            }

            return Validate(File.ReadAllText(ManifestPath), File.ReadAllText(SettingsAssetPath));
        }

        /// <summary>La regla sobre el texto del manifiesto y del asset, sin tocar archivos.</summary>
        public static string? Validate(string manifest, string settingsAsset)
        {
            // El plugin escribe "\u003" delante del número para que Android lo lea como texto ("4591..." = "459...").
            if (manifest.IndexOf("android:value=\"\\u003" + AppId + "\"", StringComparison.Ordinal) < 0)
            {
                return "el manifiesto del plugin no lleva com.google.android.gms.games.APP_ID = " + AppId + ".";
            }

            if (manifest.IndexOf("com.google.android.gms.nearby", StringComparison.Ordinal) >= 0)
            {
                return "el manifiesto del plugin lleva Nearby (D-042).";
            }

            if (settingsAsset.IndexOf("mAppId: " + AppId, StringComparison.Ordinal) < 0)
            {
                return SettingsAssetPath + " no lleva mAppId " + AppId + ".";
            }

            if (!HasEmptyField(settingsAsset, "mWebClientId") || !HasEmptyField(settingsAsset, "mNearbyServiceId"))
            {
                return SettingsAssetPath + " lleva Web Client ID o Nearby (play-games.md §10; D-042).";
            }

            return null;
        }

        private static bool HasEmptyField(string yaml, string field)
        {
            foreach (string raw in yaml.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith(field + ":", StringComparison.Ordinal))
                {
                    return line.Length == field.Length + 1;
                }
            }

            return true;
        }
    }
}
