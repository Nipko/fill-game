#nullable enable
using UnityEngine;

namespace AF.Analytics.Unity
{
    /// <summary>
    /// Sumidero de desarrollo (bloque B7): cada evento sale a logcat (y a la consola del Editor) como una línea
    /// <c>AF_EVENT {"event":"…",…}</c>, con los parámetros específicos primero y después los comunes (formato
    /// <see cref="AnalyticsEvent.ToJson"/>) [P]. Prefijo propio para no duplicar las sondas <c>AF_STATE</c> de <c>DevProbe</c>,
    /// que siguen igual. Solo escribe en builds de desarrollo y en el Editor (<see cref="Enabled"/>): en release no hace nada.
    /// Es diagnóstico local (no sale del dispositivo), así que no pide consentimiento.
    /// </summary>
    public sealed class LogcatAnalyticsSink : IAnalyticsSink
    {
        /// <summary>Prefijo de la línea.</summary>
        public const string Prefix = "AF_EVENT ";

        /// <summary>Escribe: builds de desarrollo y Editor.</summary>
        public static bool Enabled => Debug.isDebugBuild || Application.isEditor;

        /// <inheritdoc />
        public bool RequiresConsent => false;

        /// <summary>La línea que se escribe para <paramref name="e"/>.</summary>
        public static string Line(AnalyticsEvent e) => Prefix + e.ToJson();

        /// <inheritdoc />
        public void Send(AnalyticsEvent e)
        {
            if (Enabled)
            {
                Debug.Log(Line(e));
            }
        }

        /// <inheritdoc />
        public void SetUserProperty(string name, string value)
        {
            if (Enabled)
            {
                var sb = new System.Text.StringBuilder(Prefix).Append("{\"user_property\":");
                AnalyticsParam.AppendString(sb, name);
                sb.Append(",\"value\":");
                AnalyticsParam.AppendString(sb, value);
                Debug.Log(sb.Append('}').ToString());
            }
        }
    }
}
