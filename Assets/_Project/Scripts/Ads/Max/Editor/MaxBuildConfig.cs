#if UNITY_ANDROID
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using AppLovinMax.ThirdParty.MiniJson;
using UnityEditor.Android;
using UnityEngine;

namespace AF.Ads.Max.Editor
{
    /// <summary>
    /// La clave del SDK de MAX y las unidades de anuncio en el build, sin pasar por el repositorio (D-042; regla dura 5;
    /// <c>.claude/rules/sdk.md</c>). Tras el post-proceso de AppLovin (que escribe <c>res/raw/applovin_settings.json</c> con la clave de
    /// <c>AppLovinSettings</c>, vacía en el repositorio), este paso:
    /// <list type="number">
    /// <item>lee la configuración local: las variables de entorno <c>AF_MAX_SDK_KEY</c>, <c>AF_MAX_ANDROID_REWARDED_UNIT</c> y
    /// <c>AF_MAX_ANDROID_INTERSTITIAL_UNIT</c> (secretos del CI) o, si faltan, <c>Assets/_LocalSecrets/applovin-max.json</c> (ignorado por
    /// git; lo coloca el dueño) con <c>sdk_key</c>, <c>android_rewarded_unit</c> y <c>android_interstitial_unit</c>;</item>
    /// <item>con las tres, pone la clave en <c>applovin_settings.json</c> y escribe <c>res/values/af_max_config.xml</c> con las unidades y
    /// <c>af_max_configured</c> = true, que lee <c>AF.Ads.Max.MaxConfig</c> en el teléfono;</item>
    /// <item>sin ellas, no escribe nada: el build arranca con los anuncios simulados (desarrollo) o sin anuncios (release).</item>
    /// </list>
    /// Nunca escribe la clave ni las unidades en el log del build.
    /// </summary>
    public sealed class MaxBuildConfig : IPostGenerateGradleAndroidProject
    {
        /// <summary>El archivo local con la configuración (fuera del repositorio).</summary>
        public const string LocalSecretsPath = "Assets/_LocalSecrets/applovin-max.json";

        /// <summary>Después del post-proceso de AppLovin (<c>int.MaxValue - 10</c>).</summary>
        public int callbackOrder => int.MaxValue - 5;

        /// <inheritdoc />
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            Values values = Load(out string source);
            string valuesDir = Path.Combine(path, "src/main/res/values");
            string configXml = Path.Combine(valuesDir, "af_max_config.xml");
            if (!values.Complete)
            {
                if (File.Exists(configXml))
                {
                    File.Delete(configXml);
                }

                Debug.Log("[AF.MaxBuildConfig] sin configuracion de MAX (" + source + "): anuncios simulados en desarrollo, ninguno en release.");
                return;
            }

            string settingsPath = Path.Combine(path, "src/main/res/raw/applovin_settings.json");
            var settings = File.Exists(settingsPath) ? Json.Deserialize(File.ReadAllText(settingsPath)) as Dictionary<string, object> : null;
            settings ??= new Dictionary<string, object>();
            settings["sdk_key"] = values.SdkKey;
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            File.WriteAllText(settingsPath, Json.Serialize(settings) + "\n");

            Directory.CreateDirectory(valuesDir);
            var xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n");
            xml.Append("    <string name=\"af_max_configured\" translatable=\"false\">true</string>\n");
            xml.Append("    <string name=\"af_max_rewarded_unit\" translatable=\"false\">").Append(SecurityElement.Escape(values.RewardedUnit)).Append("</string>\n");
            xml.Append("    <string name=\"af_max_interstitial_unit\" translatable=\"false\">").Append(SecurityElement.Escape(values.InterstitialUnit)).Append("</string>\n");
            xml.Append("</resources>\n");
            File.WriteAllText(configXml, xml.ToString());
            Debug.Log("[AF.MaxBuildConfig] configuracion de MAX aplicada (" + source + ").");
        }

        /// <summary>Lee la configuración: primero las variables de entorno, luego el archivo local.</summary>
        internal static Values Load(out string source)
        {
            var env = new Values
            {
                SdkKey = Environment.GetEnvironmentVariable("AF_MAX_SDK_KEY") ?? string.Empty,
                RewardedUnit = Environment.GetEnvironmentVariable("AF_MAX_ANDROID_REWARDED_UNIT") ?? string.Empty,
                InterstitialUnit = Environment.GetEnvironmentVariable("AF_MAX_ANDROID_INTERSTITIAL_UNIT") ?? string.Empty,
            };
            if (env.Complete)
            {
                source = "entorno";
                return env;
            }

            if (!File.Exists(LocalSecretsPath))
            {
                source = "sin archivo local";
                return new Values();
            }

            try
            {
                LocalFile file = JsonUtility.FromJson<LocalFile>(File.ReadAllText(LocalSecretsPath));
                source = "archivo local";
                return new Values
                {
                    SdkKey = file?.sdk_key?.Trim() ?? string.Empty,
                    RewardedUnit = file?.android_rewarded_unit?.Trim() ?? string.Empty,
                    InterstitialUnit = file?.android_interstitial_unit?.Trim() ?? string.Empty,
                };
            }
            catch (Exception)
            {
                source = "archivo local ilegible";
                return new Values();
            }
        }

        internal sealed class Values
        {
            public string SdkKey = string.Empty;
            public string RewardedUnit = string.Empty;
            public string InterstitialUnit = string.Empty;

            public bool Complete => SdkKey.Length > 0 && RewardedUnit.Length > 0 && InterstitialUnit.Length > 0;
        }

        [Serializable]
        private sealed class LocalFile
        {
#pragma warning disable 0649 // los rellena JsonUtility
            public string? sdk_key;
            public string? android_rewarded_unit;
            public string? android_interstitial_unit;
#pragma warning restore 0649
        }
    }
}
#endif
