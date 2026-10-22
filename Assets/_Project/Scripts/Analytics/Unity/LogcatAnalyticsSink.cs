#nullable enable
using UnityEngine;

namespace AF.Analytics.Unity
{
    /// <summary>
    /// Sumidero de desarrollo (bloque B7): cada evento sale a logcat (y a la consola del Editor) como una línea
    /// <c>AF_EVENT {"event":"…",…}</c>, con los parámetros específicos primero y después los comunes (formato
    /// <see cref="AnalyticsEvent.ToJson"/>) [P]. Prefijo propio para no duplicar las sondas <c>AF_STATE</c> de <c>DevProbe</c>,
    /// que siguen igual. Solo escribe en builds de desarrollo y en el Editor (<see cref="Enabled"/>): en release no hace nada.
    /// Es diagnóstico local (no sale del dispositivo), así que no pide consentimiento y escribe siempre (Q-B7-20 (a)).
    /// <para>
    /// <b>Sin consentimiento de analítica</b> (<see cref="ICollectionAwareSink"/>; B12 y F3-6; observación 5 de la hoja del
    /// 2026-10-22), la línea lleva el prefijo <see cref="UnsentPrefix"/> (<c>AF_EVENT_UNSENT {…}</c>): el evento se queda en
    /// logcat y ningún sumidero que envía fuera del dispositivo (Firebase) lo recibe, porque <see cref="AnalyticsService"/> no
    /// se lo entrega y Firebase tiene la recogida apagada. Con el consentimiento, <see cref="Prefix"/>.
    /// </para>
    /// </summary>
    public sealed class LogcatAnalyticsSink : IAnalyticsSink, ICollectionAwareSink
    {
        /// <summary>Prefijo de la línea con el consentimiento de analítica (el evento va también a los sumideros que envían).</summary>
        public const string Prefix = "AF_EVENT ";

        /// <summary>Prefijo de la línea sin el consentimiento: el evento solo queda en logcat; no se envía a ningún sitio.</summary>
        public const string UnsentPrefix = "AF_EVENT_UNSENT ";

        private bool _collection;

        /// <summary>Escribe: builds de desarrollo y Editor.</summary>
        public static bool Enabled => Debug.isDebugBuild || Application.isEditor;

        /// <inheritdoc />
        public bool RequiresConsent => false;

        /// <summary>Hay consentimiento de analítica ahora (lo fija <see cref="AnalyticsService"/>; apagado al crearlo).</summary>
        public bool CollectionEnabled => _collection;

        /// <summary>La línea que se escribe para <paramref name="e"/> con el consentimiento.</summary>
        public static string Line(AnalyticsEvent e) => Prefix + e.ToJson();

        /// <summary>La línea que se escribe para <paramref name="e"/>: con <paramref name="sent"/> = false, la de «no enviado».</summary>
        public static string Line(AnalyticsEvent e, bool sent) => (sent ? Prefix : UnsentPrefix) + e.ToJson();

        /// <inheritdoc />
        public void SetCollectionEnabled(bool enabled) => _collection = enabled;

        /// <inheritdoc />
        public void Send(AnalyticsEvent e)
        {
            if (Enabled)
            {
                Debug.Log(Line(e, _collection));
            }
        }

        /// <inheritdoc />
        public void SetUserProperty(string name, string value)
        {
            if (Enabled)
            {
                var sb = new System.Text.StringBuilder(_collection ? Prefix : UnsentPrefix).Append("{\"user_property\":");
                AnalyticsParam.AppendString(sb, name);
                sb.Append(",\"value\":");
                AnalyticsParam.AppendString(sb, value);
                Debug.Log(sb.Append('}').ToString());
            }
        }
    }
}
