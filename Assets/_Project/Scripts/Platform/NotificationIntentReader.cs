#nullable enable
using System;
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
        /// Si la intención actual de la actividad viene de tocar un aviso, devuelve su <paramref name="id"/>, lo que el paquete
        /// pudo reconstruir de su <c>IntentData</c> (<paramref name="packageType"/>, null si no pudo) y si Android la relanzó desde
        /// Recientes (<paramref name="fromHistory"/>: <c>FLAG_ACTIVITY_LAUNCHED_FROM_HISTORY</c>), y la marca como leída en el
        /// proceso. False si no viene de un aviso o ya se leyó en este proceso.
        /// </summary>
        bool TryTakeOpened(out int id, out string? packageType, out bool fromHistory);
    }

    /// <summary>
    /// Dónde se guarda la marca de la última intención aceptada (<c>id:ticks</c> de su hora de entrega), para no aceptarla dos veces
    /// ni siquiera en otro proceso (en el teléfono, <c>PlayerPrefs</c>).
    /// </summary>
    public interface IIntentMarkStore
    {
        /// <summary>La marca guardada, o null.</summary>
        string? Load();

        /// <summary>Guarda <paramref name="mark"/>.</summary>
        void Save(string mark);
    }

    /// <summary>Marca en memoria (tests y editor).</summary>
    public sealed class InMemoryIntentMarkStore : IIntentMarkStore
    {
        /// <summary>La marca, o null.</summary>
        public string? Mark { get; set; }

        /// <inheritdoc />
        public string? Load() => Mark;

        /// <inheritdoc />
        public void Save(string mark) => Mark = mark;
    }

    /// <summary>
    /// Lee el <c>type</c> del aviso tocado (observación 19 de la jornada 6; D-062), sin motor.
    /// <list type="bullet">
    /// <item><b>En caliente</b> el lector anterior descartaba la intención cuyo id era el de la última leída, y los avisos repiten id
    /// (el primero del plan es siempre el mismo). Ahora un toque nuevo con el mismo id cuenta.</item>
    /// <item><b>En frío</b> el tipo salía de reconstruir el aviso con el paquete (<c>GetLastNotificationIntent</c>), que en un
    /// proceso nuevo ya no lo encuentra. Ahora sale primero de la lista de lo programado (<see cref="Remember"/>).</item>
    /// <item><b>Nunca dos veces la misma</b> (revisión de la observación 19): la intención base de la tarea conserva el extra del
    /// aviso aunque el proceso lo quite, y Android la reutiliza al recrear la actividad desde Recientes tras matar el proceso. Por
    /// eso se rechaza (1) la intención relanzada desde Recientes, (2) un id cuyo aviso recordado aún no ha sonado (su hora de
    /// entrega es posterior a ahora, con <see cref="FireToleranceSeconds"/> de margen: el plan nuevo lo reprogramó) y (3) la misma
    /// intención ya aceptada (id y hora de entrega), con la marca guardada en <see cref="IIntentMarkStore"/>.</item>
    /// </list>
    /// Con el id fuera de la lista se usa el tipo del paquete o <see cref="UnknownType"/>: el aviso cuenta (<c>push_open</c> sale
    /// siempre) y va al inicio.
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

        /// <summary>Rechazada: relanzada desde Recientes.</summary>
        public const string RejectHistory = "history";

        /// <summary>Rechazada: el aviso recordado con ese id aún no ha sonado.</summary>
        public const string RejectNotFired = "not_fired";

        /// <summary>Rechazada: esa intención ya se aceptó.</summary>
        public const string RejectConsumed = "consumed";

        /// <summary>Segundos de margen sobre la hora de entrega (el paquete la pasa a milisegundos).</summary>
        public const int FireToleranceSeconds = 2;

        private readonly INotificationIntentPort _port;
        private readonly IClock _clock;
        private readonly IIntentMarkStore _marks;
        private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();

        /// <summary>Crea el lector sobre <paramref name="port"/>, con el reloj y el almacén de la marca.</summary>
        public NotificationIntentReader(INotificationIntentPort port, IClock clock, IIntentMarkStore marks)
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _marks = marks ?? throw new ArgumentNullException(nameof(marks));
        }

        /// <summary>La línea de diagnóstico de la última lectura con intención de aviso (aceptada o no), o null.</summary>
        public string? LastDiagnostic { get; private set; }

        /// <summary>
        /// El aviso <paramref name="id"/> se programó con <paramref name="type"/> para <paramref name="fireAtUtc"/> (al programarlo,
        /// y al cargar la lista guardada al arrancar). El último gana.
        /// </summary>
        public void Remember(int id, string type, DateTime fireAtUtc)
        {
            if (!string.IsNullOrEmpty(type))
            {
                _entries[id] = new Entry(type, fireAtUtc);
            }
        }

        /// <summary>
        /// El tipo del aviso con el que se abrió la app, o null si no se abrió desde uno o la intención no vale (Recientes, aviso aún
        /// sin sonar o ya aceptado). Escribe en <see cref="LastDiagnostic"/> la línea <c>push_intent</c>.
        /// </summary>
        public string? Take()
        {
            LastDiagnostic = null;
            if (!_port.TryTakeOpened(out int id, out string? packageType, out bool fromHistory))
            {
                return null;
            }

            bool known = _entries.TryGetValue(id, out Entry entry);
            if (fromHistory)
            {
                return Reject(id, known ? entry.Type : packageType, RejectHistory, packageType);
            }

            if (known)
            {
                if (entry.FireAtUtc > _clock.UtcNow.AddSeconds(FireToleranceSeconds))
                {
                    return Reject(id, entry.Type, RejectNotFired, packageType);
                }

                string mark = Mark(id, entry.FireAtUtc);
                if (string.Equals(_marks.Load(), mark, StringComparison.Ordinal))
                {
                    return Reject(id, entry.Type, RejectConsumed, packageType);
                }

                _marks.Save(mark);
                LastDiagnostic = Diagnostic(id, entry.Type, SourceLog, packageType, null);
                return entry.Type;
            }

            string type = string.IsNullOrEmpty(packageType) ? UnknownType : packageType!;
            LastDiagnostic = Diagnostic(id, type, string.IsNullOrEmpty(packageType) ? SourceNone : SourcePackage, packageType, null);
            return type;
        }

        /// <summary>Marca de una intención aceptada: <c>id:ticks</c> de su hora de entrega (UTC).</summary>
        public static string Mark(int id, DateTime fireAtUtc)
        {
            return id.ToString(CultureInfo.InvariantCulture) + ":" + fireAtUtc.Ticks.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Línea de diagnóstico (solo identificadores): <c>AF_STATE {"event":"push_intent","id":…,"type":…,"via":…,"package":…}</c>
        /// y, si se rechazó, <c>"rejected":"history|not_fired|consumed"</c>.
        /// </summary>
        public static string Diagnostic(int id, string? type, string via, string? packageType, string? rejected)
        {
            var sb = new StringBuilder(140);
            sb.Append("AF_STATE {\"event\":\"push_intent\",\"id\":").Append(id.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"type\":");
            Quoted(sb, type);
            sb.Append(",\"via\":\"").Append(via).Append("\",\"package\":");
            Quoted(sb, packageType);
            if (rejected != null)
            {
                sb.Append(",\"rejected\":\"").Append(rejected).Append('"');
            }

            return sb.Append('}').ToString();
        }

        private string? Reject(int id, string? type, string reason, string? packageType)
        {
            LastDiagnostic = Diagnostic(id, type, type == null ? SourceNone : SourceLog, packageType, reason);
            return null;
        }

        private static void Quoted(StringBuilder sb, string? value)
        {
            if (value == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append('"').Append(value).Append('"');
            }
        }

        private readonly struct Entry
        {
            public Entry(string type, DateTime fireAtUtc)
            {
                Type = type;
                FireAtUtc = fireAtUtc;
            }

            public string Type { get; }

            public DateTime FireAtUtc { get; }
        }
    }
}
