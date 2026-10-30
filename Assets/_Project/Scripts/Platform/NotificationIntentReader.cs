#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Platform
{
    /// <summary>
    /// Lo que el sistema sabe de la intención con la que se abrió (o se volvió a) la actividad (en el teléfono, la de Android a través
    /// de <c>com.unity.mobile.notifications</c>; en los tests, uno falso).
    /// </summary>
    public interface INotificationIntentPort
    {
        /// <summary>
        /// Si la intención actual de la actividad viene de tocar un aviso, devuelve su <paramref name="id"/> y lo que el paquete
        /// pudo reconstruir de su <c>IntentData</c> (<paramref name="packageType"/>, null si no pudo), y la marca como leída para
        /// que no se vuelva a contar: la misma intención sigue en la actividad hasta que llega otra. False si no viene de un aviso
        /// o ya se leyó.
        /// </summary>
        bool TryTakeOpened(out int id, out string? packageType);
    }

    /// <summary>
    /// Lee el <c>type</c> del aviso tocado (observación 19 de la jornada 6; D-062), sin motor. Dos fallos del lector anterior:
    /// <list type="bullet">
    /// <item><b>En caliente:</b> descartaba la intención cuyo id era el de la última leída, y los avisos repiten id (el primero del
    /// plan es siempre el mismo), así que el segundo toque en un proceso no contaba. Ahora la intención se marca como leída en la
    /// propia actividad (<see cref="INotificationIntentPort.TryTakeOpened"/>): un toque nuevo con el mismo id cuenta y la misma
    /// intención no cuenta dos veces.</item>
    /// <item><b>En frío:</b> el tipo salía de reconstruir el aviso con el paquete (<c>GetLastNotificationIntent</c>), que lo busca
    /// en la bandeja (ya no está: se quita al tocarlo), en su memoria (un proceso nuevo no lo tiene: solo carga los que aún no han
    /// sonado) o en su copia guardada, que su limpieza borra en segundo plano al arrancar. Ahora el tipo sale primero de la lista de
    /// lo programado (<see cref="Remember"/>, la de <c>af_push_sent</c> en el teléfono) y solo después del paquete.</item>
    /// </list>
    /// Si no se sabe el tipo, se devuelve <see cref="UnknownType"/>: el aviso cuenta igual (<c>push_open</c> sale siempre) y va al
    /// inicio.
    /// </summary>
    public sealed class NotificationIntentReader
    {
        /// <summary><c>type</c> de un aviso tocado cuyo tipo no se pudo leer [P].</summary>
        public const string UnknownType = "unknown";

        /// <summary>Origen del tipo: la lista de lo programado.</summary>
        public const string SourceLog = "log";

        /// <summary>Origen del tipo: la reconstrucción del paquete.</summary>
        public const string SourcePackage = "package";

        /// <summary>Origen del tipo: ninguno (<see cref="UnknownType"/>).</summary>
        public const string SourceNone = "none";

        private readonly INotificationIntentPort _port;
        private readonly Dictionary<int, string> _types = new Dictionary<int, string>();

        /// <summary>Crea el lector sobre <paramref name="port"/>.</summary>
        public NotificationIntentReader(INotificationIntentPort port)
        {
            _port = port ?? throw new System.ArgumentNullException(nameof(port));
        }

        /// <summary>La última lectura con aviso (para la línea de diagnóstico), o null.</summary>
        public string? LastDiagnostic { get; private set; }

        /// <summary>
        /// El aviso <paramref name="id"/> se programó con <paramref name="type"/> (al programarlo, y al cargar la lista guardada al
        /// arrancar). El último gana: al reprogramar se cancela todo, también lo que hay en la bandeja.
        /// </summary>
        public void Remember(int id, string type)
        {
            if (!string.IsNullOrEmpty(type))
            {
                _types[id] = type;
            }
        }

        /// <summary>
        /// El tipo del aviso con el que se abrió la app, o null si no se abrió desde uno (o esa intención ya se leyó). Escribe en
        /// <see cref="LastDiagnostic"/> la línea <c>AF_STATE {"event":"push_intent","id":…,"type":…,"via":…}</c>.
        /// </summary>
        public string? Take()
        {
            if (!_port.TryTakeOpened(out int id, out string? packageType))
            {
                return null;
            }

            string type;
            string via;
            if (_types.TryGetValue(id, out string? known))
            {
                type = known;
                via = SourceLog;
            }
            else if (!string.IsNullOrEmpty(packageType))
            {
                type = packageType!;
                via = SourcePackage;
            }
            else
            {
                type = UnknownType;
                via = SourceNone;
            }

            LastDiagnostic = Diagnostic(id, type, via, packageType);
            return type;
        }

        /// <summary>Línea de diagnóstico de una intención leída (solo identificadores).</summary>
        public static string Diagnostic(int id, string type, string via, string? packageType)
        {
            var sb = new StringBuilder(120);
            sb.Append("AF_STATE {\"event\":\"push_intent\",\"id\":").Append(id.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"type\":\"").Append(type).Append("\",\"via\":\"").Append(via).Append("\",\"package\":");
            if (packageType == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append('"').Append(packageType).Append('"');
            }

            return sb.Append('}').ToString();
        }
    }
}
