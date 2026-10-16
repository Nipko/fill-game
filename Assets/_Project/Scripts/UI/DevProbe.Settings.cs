#nullable enable
using System.Collections.Generic;
using System.Text;
using AF.Presentation;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sonda de la pantalla de Ajustes (tarea L3; docs/dev/prototipo-v0.md), solo en desarrollo:
    /// <list type="bullet">
    /// <item><c>AF_STATE {"event":"settings","t":…,"language":"auto","patterns":"auto","offered":["es-419","en-XA"]}</c> al
    /// abrirla: lo guardado en <c>settings.language</c> y <c>settings.patterns</c> (08 §4.3) y los idiomas que ofrece. Elegir un
    /// idioma que cambia el de la UI la rehace y Ajustes sigue abierta (FXT): sale otra vez esta línea, ya en el idioma nuevo, y
    /// no sale <c>settings_closed</c>.</item>
    /// <item><c>AF_STATE {"event":"settings_change","t":…,"key":"patterns","value":"on","on":true,"opacity":0.20}</c> al elegir
    /// (las claves de <c>af_settings_change</c> de 08 §8.2: <c>language</c>, <c>patterns</c> o <c>sloth_messages</c>, este con
    /// <c>true</c> o <c>false</c>; tarea TI3); con <c>patterns</c>, el interruptor del nivel cargado y la opacidad del shader ya
    /// aplicados (<c>on</c> es null sin nivel cargado).</item>
    /// <item><c>AF_STATE {"event":"settings_closed","t":…}</c> al cerrarla (X o Atrás de Android, que la cierra a la primera
    /// también tras un arranque en frío: FXT).</item>
    /// <item><c>AF_LAYOUT</c> con <c>"screen":"settings"</c> y los ids <c>close</c>, <c>lang_&lt;idioma&gt;</c> (<c>lang_auto</c>,
    /// <c>lang_es-419</c>…), <c>patterns_&lt;auto|on|off&gt;</c> y <c>sloth_messages</c>, para tocarlos con
    /// <c>adb shell input tap</c>.</item>
    /// </list>
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>Texto de <paramref name="value"/> en la sonda y en el perfil: <c>auto</c>, <c>on</c> u <c>off</c>.</summary>
        public static string PatternsKey(PatternsSetting value)
        {
            switch (value)
            {
                case PatternsSetting.On: return "on";
                case PatternsSetting.Off: return "off";
                default: return "auto";
            }
        }

        /// <summary>Escribe <c>AF_STATE settings</c> al abrir Ajustes.</summary>
        public void SettingsOpened(string language, PatternsSetting patterns, IReadOnlyList<string> offered)
        {
            if (!Enabled)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"settings\"");
            AppendTime(sb);
            sb.Append(",\"language\":\"").Append(language).Append('"');
            sb.Append(",\"patterns\":\"").Append(PatternsKey(patterns)).Append('"');
            sb.Append(",\"offered\":[");
            for (int i = 0; i < offered.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append('"').Append(offered[i]).Append('"');
            }

            sb.Append("]}");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Escribe <c>AF_STATE settings_change</c> (clave y valor; con <c>patterns</c>, <paramref name="patternsOn"/> = el
        /// interruptor aplicado al nivel cargado, o null sin nivel, y la opacidad del shader).
        /// </summary>
        public void SettingsChange(string key, string value, bool? patternsOn)
        {
            string? line = FormatSettingsChange(key, value, patternsOn);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE settings_change</c>; null si la sonda no escribe.</summary>
        public string? FormatSettingsChange(string key, string value, bool? patternsOn)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"settings_change\"");
            AppendTime(sb);
            sb.Append(",\"key\":\"").Append(key).Append('"');
            sb.Append(",\"value\":\"").Append(value).Append('"');
            if (key == "patterns")
            {
                sb.Append(",\"on\":").Append(patternsOn.HasValue ? (patternsOn.Value ? "true" : "false") : "null");
                sb.Append(",\"opacity\":");
                AppendUnit2(sb, LiquidGraphic.ShaderPatternOpacity);
            }

            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Escribe <c>AF_STATE settings_closed</c> al cerrar Ajustes.</summary>
        public void SettingsClosed()
        {
            if (!Enabled)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"settings_closed\"");
            AppendTime(sb);
            sb.Append('}');
            Debug.Log(sb.ToString());
        }
    }
}
