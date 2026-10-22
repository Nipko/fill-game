#nullable enable
using System;
using AF.Platform;

namespace AF.GameFlow
{
    /// <summary>
    /// Pantallas que pueden salir solas al entrar al inicio, en el orden de prioridad de 06 §4.7 («Bitácora pendiente → evento que
    /// empieza o termina → oferta → resultado de liga»). En la 1.0 solo existe la Bitácora; las demás llegan con sus sistemas
    /// (1.1-1.2) y se registran igual (<see cref="TakeoverQueue.Register"/>).
    /// </summary>
    public enum TakeoverKind
    {
        /// <summary>Ninguna.</summary>
        None = 0,

        /// <summary>La libreta de la Bitácora con la casilla de hoy pendiente (06 §4.2; D-033, añadido del 2026-10-20, punto 5).</summary>
        Logbook = 1,

        /// <summary>Un evento que empieza o termina (06 §5; 1.2).</summary>
        Event = 2,

        /// <summary>Una oferta (05 §5; la primera oferta temporal llega en la 1.1/1.2, Q-E2).</summary>
        Offer = 3,

        /// <summary>El resultado de la liga (06 §6; 1.2).</summary>
        League = 4,
    }

    /// <summary>Cifras de la cola de takeovers con los defaults de la tabla única de 08 §9.</summary>
    public sealed class TakeoverConfig
    {
        /// <summary>Una instancia nueva con los defaults (cada llamada devuelve otra).</summary>
        public static TakeoverConfig Defaults => new TakeoverConfig();

        /// <summary>
        /// <c>ui_max_takeovers_per_session</c> (08 §9: int, 1, rango 1, dueño 05): «Un solo pop-up fuera de hitos por sesión»
        /// (06 §2.3, §4.7 y §5.1).
        /// </summary>
        [RemoteConfigKey("ui_max_takeovers_per_session")]
        public int UiMaxTakeoversPerSession { get; set; } = 1;

        /// <summary>Comprueba que los valores tienen sentido (rango de 08 §9: 1).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            if (UiMaxTakeoversPerSession != 1)
            {
                throw new InvalidOperationException("TakeoverConfig: ui_max_takeovers_per_session admite solo 1 (rango de 08 §9).");
            }
        }
    }

    /// <summary>
    /// Cola de takeovers (tarea B4; 06 §2.3, regla final, y §4.7; D-033, añadido del 2026-10-20, puntos 5 y 11; Q-M4 (a)), sin
    /// motor y sin asignaciones al consultarla:
    /// <list type="bullet">
    /// <item><description>Solo se consulta <b>al entrar al inicio</b> (D-033, punto 11: «<c>TakeoverQueue</c> aplica el orden de
    /// 06 §4.7 solo al entrar al Inicio»): la vista llama a <see cref="TakeAtLobby"/> en cada entrada. La victoria y su panel no
    /// son takeovers.</description></item>
    /// <item><description>Como mucho <c>ui_max_takeovers_per_session</c> (1) por sesión, <b>fuera de las secuencias de hito</b>
    /// (la del nivel 10: Victoria → libreta → Mapa → Inicio; 06 §2.3): lo que sale dentro de una secuencia de hito no gasta el
    /// cupo (<see cref="MarkSeen"/>).</description></item>
    /// <item><description>Gana el de más prioridad que esté pendiente (<see cref="TakeoverKind"/>, en el orden de 06 §4.7) y que no
    /// se haya visto ya en la sesión: si el jugador abrió la libreta a mano (tarjeta del inicio) o le salió en la secuencia del
    /// 10, no vuelve a salir sola en la misma sesión [P] (Q-B9).</description></item>
    /// <item><description><b>Sesión</b> (Q-M4 (a)): arranque en frío (crear la cola) o vuelta tras ≥ <see cref="SessionGapMinutes"/>
    /// min en segundo plano (<see cref="OnBackground"/> y <see cref="OnForeground"/>). Una sesión nueva repone el cupo y olvida lo
    /// visto. Cuando se integre el <c>SessionTracker</c> de B6, el arranque puede llamar a <see cref="BeginSession"/> con sus
    /// sesiones en lugar de usar el reloj propio (Q-B10).</description></item>
    /// </list>
    /// No es seguro entre hilos.
    /// </summary>
    public sealed class TakeoverQueue
    {
        /// <summary>Minutos en segundo plano a partir de los cuales la vuelta es una sesión nueva (Q-M4 (a): «≥ 30 min»).</summary>
        public const int SessionGapMinutes = 30;

        private const int KindCount = 5;

        private readonly Func<bool>?[] _pending = new Func<bool>?[KindCount];
        private readonly bool[] _seen = new bool[KindCount];
        private DateTime? _backgroundAt;

        /// <summary>Crea la cola: es la primera sesión (arranque en frío).</summary>
        public TakeoverQueue(TakeoverConfig? config = null)
        {
            Config = config ?? TakeoverConfig.Defaults;
            Config.Validate();
            Session = 1;
        }

        /// <summary>Cifras.</summary>
        public TakeoverConfig Config { get; }

        /// <summary>Sesión en curso (1 la del arranque).</summary>
        public int Session { get; private set; }

        /// <summary>Takeovers que ya salieron solos en esta sesión.</summary>
        public int ShownThisSession { get; private set; }

        /// <summary>Queda cupo en la sesión.</summary>
        public bool QuotaLeft => ShownThisSession < Config.UiMaxTakeoversPerSession;

        /// <summary>
        /// Registra (o quita, con null) quién dice si <paramref name="kind"/> está pendiente. La función se llama en cada
        /// <see cref="Peek"/>: debe ser barata y no asignar memoria.
        /// </summary>
        public void Register(TakeoverKind kind, Func<bool>? pending)
        {
            int i = Index(kind);
            if (i == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "None no es un takeover.");
            }

            _pending[i] = pending;
        }

        /// <summary>
        /// El takeover que saldría ahora al entrar al inicio, o <see cref="TakeoverKind.None"/>: el de más prioridad pendiente y
        /// no visto en la sesión, si queda cupo. No cambia nada.
        /// </summary>
        public TakeoverKind Peek()
        {
            if (!QuotaLeft)
            {
                return TakeoverKind.None;
            }

            for (int i = 1; i < KindCount; i++)
            {
                Func<bool>? pending = _pending[i];
                if (!_seen[i] && pending != null && pending())
                {
                    return (TakeoverKind)i;
                }
            }

            return TakeoverKind.None;
        }

        /// <summary>
        /// Al entrar al inicio (fuera de una secuencia de hito): <see cref="Peek"/> y, si sale uno, lo cuenta en el cupo y lo marca
        /// como visto. Quien llama lo enseña.
        /// </summary>
        public TakeoverKind TakeAtLobby()
        {
            TakeoverKind kind = Peek();
            if (kind != TakeoverKind.None)
            {
                MarkShown(kind);
            }

            return kind;
        }

        /// <summary><paramref name="kind"/> salió solo: gasta cupo y queda visto en la sesión.</summary>
        public void MarkShown(TakeoverKind kind)
        {
            int i = Index(kind);
            if (i == 0)
            {
                return;
            }

            _seen[i] = true;
            ShownThisSession++;
        }

        /// <summary>
        /// El jugador vio <paramref name="kind"/> sin que fuera un takeover (lo abrió a mano o salió en una secuencia de hito): no
        /// gasta cupo, pero ya no sale solo en esta sesión.
        /// </summary>
        public void MarkSeen(TakeoverKind kind)
        {
            int i = Index(kind);
            if (i != 0)
            {
                _seen[i] = true;
            }
        }

        /// <summary>Ya salió o se vio <paramref name="kind"/> en esta sesión.</summary>
        public bool WasSeen(TakeoverKind kind)
        {
            return _seen[Index(kind)];
        }

        /// <summary>Empieza otra sesión: cupo entero y nada visto.</summary>
        public void BeginSession()
        {
            Session++;
            ShownThisSession = 0;
            Array.Clear(_seen, 0, _seen.Length);
        }

        /// <summary>La app pasa a segundo plano en <paramref name="utcNow"/> (la primera vez cuenta; otra llamada no la mueve).</summary>
        public void OnBackground(DateTime utcNow)
        {
            if (!_backgroundAt.HasValue)
            {
                _backgroundAt = utcNow;
            }
        }

        /// <summary>
        /// La app vuelve en <paramref name="utcNow"/>: con ≥ <see cref="SessionGapMinutes"/> min fuera, empieza otra sesión (Q-M4
        /// (a)). Con el reloj hacia atrás no. Devuelve si empezó una.
        /// </summary>
        public bool OnForeground(DateTime utcNow)
        {
            DateTime? since = _backgroundAt;
            _backgroundAt = null;
            if (!since.HasValue || utcNow - since.Value < TimeSpan.FromMinutes(SessionGapMinutes))
            {
                return false;
            }

            BeginSession();
            return true;
        }

        /// <summary>Id de <paramref name="kind"/> para la sonda: <c>none</c>, <c>logbook</c>, <c>event</c>, <c>offer</c> o <c>league</c>.</summary>
        public static string IdOf(TakeoverKind kind)
        {
            switch (kind)
            {
                case TakeoverKind.Logbook: return "logbook";
                case TakeoverKind.Event: return "event";
                case TakeoverKind.Offer: return "offer";
                case TakeoverKind.League: return "league";
                default: return "none";
            }
        }

        private static int Index(TakeoverKind kind)
        {
            int i = (int)kind;
            if (i < 0 || i >= KindCount)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            return i;
        }
    }
}
