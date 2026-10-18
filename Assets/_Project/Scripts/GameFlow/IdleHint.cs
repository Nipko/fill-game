#nullable enable
using System;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Platform;

namespace AF.GameFlow
{
    /// <summary>Lo que pide <see cref="IdleHint.Tick"/> a la vista en este fotograma.</summary>
    public enum IdleHintSignal
    {
        /// <summary>Nada nuevo.</summary>
        None = 0,

        /// <summary>
        /// Pulsa una vez el origen <see cref="IdleHint.PulseSource"/> (escala <see cref="IdleHint.PulseScale"/> durante
        /// <see cref="IdleHint.PulseMs"/>): el solver propone desde aquí un vertido seguro.
        /// </summary>
        Pulse = 1,

        /// <summary>Hubo un toque o el tablero ya no está en juego: el pulso en curso, si lo hay, se apaga.</summary>
        Interrupted = 2,

        /// <summary>
        /// El solver respondió sin vertido (respuestas 2-4 de 02 T5: sin salida, ningún vertido seguro o sin decidir a tiempo):
        /// no pulsa nada (D-019). Solo para la sonda.
        /// </summary>
        Silent = 3,
    }

    /// <summary>
    /// Pista por inactividad (tarea MO3; 02 §3.4; 04 §2.3; 08 §9 <c>hint_idle_from_level</c> = 3 y <c>hint_idle_delay_s</c> = 12;
    /// D-002: ayuda explícita que solo señala un origen; D-019, Consecuencias): en <b>todos los niveles desde el 3</b>, tras 12 s
    /// sin toques en PLAYING, pregunta al solver de Morpho (<see cref="MorphoBooster"/>, MO1) desde el estado actual y, si propone
    /// un vertido (seguro en todas las realidades con dosis ocultas), pide a la vista que su cuia de <b>origen</b> pulse una vez.
    /// Nunca enseña el destino ni cobra (no toca la cartera ni el stock) y no cuenta para el enfriamiento de Morpho. Sin motor:
    /// la vista (<c>GameScreen.Hint.cs</c>) le pasa los toques y si está ocupada, y pinta el pulso.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>Nunca en posición perdida</b> (research/01 Especificación 15): solo pulsa con la respuesta
    /// <see cref="HintAnswer.Pour"/>. En los tres casos sin respuesta de 02 T5 («Desde aquí no hay salida», «No veo un vertido
    /// seguro» y «No encuentro un camino», también al agotar <c>hint_timeout_ms</c>) no pulsa nada (D-019).</description></item>
    /// <item><description><b>Inactividad:</b> cuenta con el <see cref="IClock"/> del flujo (<see cref="LevelFlow.Clock"/>)
    /// desde el último toque o cambio del flujo (otra sesión, otro tablero, otra selección, otra fase, Morpho o la Luciérnaga) y
    /// solo mientras el nivel está en juego: tablero que acepta toques, nivel con <c>hint_supported</c>, sin Morpho pensando ni
    /// pista de Morpho resaltada, sin la selección de la Luciérnaga y con la vista quieta. Fuera de eso, la cuenta vuelve a
    /// empezar.</description></item>
    /// <item><description><b>Una vez</b> por cada tramo sin toques (04 §2.3: «pulsa una vez») [P]: después no vuelve a
    /// preguntar hasta el siguiente toque o cambio.</description></item>
    /// <item><description><b>Apagada</b> con <c>hint_idle_delay_s</c> = 0 o con el interruptor de Ajustes
    /// (<c>settings.idle_hint</c> = false; 02 §12: equivale a 0 en el dispositivo): <see cref="DelayMs"/>.</description></item>
    /// <item><description><b>Hilo:</b> su propio <see cref="MorphoBooster"/> (el solver y sus tablas son de esta instancia), así
    /// que pulsar Pista nunca espera a la consulta de la pista por inactividad; un toque la abandona sin esperar al hilo de
    /// trabajo. <see cref="Tick"/> no asigna memoria salvo al lanzar una consulta (una por tramo).</description></item>
    /// </list>
    /// </remarks>
    public sealed class IdleHint
    {
        /// <summary>Escala del pulso del origen (02 §3.4 y 04 §2.3: «escala 1,04»).</summary>
        public const float PulseScale = 1.04f;

        /// <summary>Duración del pulso (02 §3.4 y 04 §2.3: «600 ms»), sube y baja una vez.</summary>
        public const int PulseMs = 600;

        private readonly Action? _workerHook;
        private MorphoBooster? _morpho;

        // Lo último que se vio del flujo: si cambia, cuenta como un toque.
        private LevelSession? _session;
        private BoardState? _board;
        private int _selected = int.MinValue;
        private FlowPhase _phase = FlowPhase.Loading;
        private bool _morphoBusy;
        private bool _revealSelecting;

        private bool _armed;
        private DateTime _sinceUtc;
        private bool _querying;
        private bool _spent;
        private BoardState? _queryBoard;

        /// <summary>Crea la pista por inactividad.</summary>
        /// <param name="workerHook">Solo tests: se ejecuta en el hilo de trabajo antes de cada búsqueda (p. ej. para retenerla
        /// y forzar el corte por <c>hint_timeout_ms</c>).</param>
        public IdleHint(Action? workerHook = null)
        {
            _workerHook = workerHook;
        }

        /// <summary>Cuia de origen del último pulso (−1 si aún no hubo ninguno en este tramo).</summary>
        public int PulseSource { get; private set; } = -1;

        /// <summary>Pulsos pedidos desde que se creó (tests y sonda).</summary>
        public int PulseCount { get; private set; }

        /// <summary>Consultas lanzadas al solver desde que se creó (tests).</summary>
        public int QueryCount { get; private set; }

        /// <summary>Respuesta de la última consulta terminada (null si aún no hubo ninguna).</summary>
        public HintAnswer? LastAnswer { get; private set; }

        /// <summary>Hay una consulta en curso en el hilo de trabajo.</summary>
        public bool Querying => _querying;

        /// <summary>La cuenta de inactividad corre (el nivel está en juego y la pista aplica).</summary>
        public bool Armed => _armed;

        /// <summary>Ya respondió en este tramo sin toques: no vuelve a preguntar hasta el siguiente toque.</summary>
        public bool Spent => _spent;

        /// <summary>El Morpho propio de la pista (se crea en la primera consulta con las cifras y el reloj del flujo).</summary>
        public MorphoBooster? Morpho => _morpho;

        /// <summary>
        /// Milisegundos sin toques tras los que pulsa: <c>hint_idle_delay_s</c> × 1000, o 0 (apagada) si el valor es 0 o si el
        /// jugador la apagó en Ajustes (<paramref name="settingOn"/> = <c>settings.idle_hint</c>; 02 §12).
        /// </summary>
        public static int DelayMs(BoosterConfig config, bool settingOn)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return settingOn && config.HintIdleDelayS > 0 ? config.HintIdleDelayS * 1000 : 0;
        }

        /// <summary>La pista por inactividad existe en <paramref name="level"/> (desde <c>hint_idle_from_level</c>, sin tope).</summary>
        public static bool AppliesTo(int level, BoosterConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return level >= config.HintIdleFromLevel;
        }

        /// <summary>
        /// Con esta respuesta del solver pulsa un origen: solo con <see cref="HintAnswer.Pour"/>; nunca en los tres casos sin
        /// respuesta (D-019; 02 T5).
        /// </summary>
        public static bool PulsesOn(HintAnswer answer)
        {
            return answer == HintAnswer.Pour;
        }

        /// <summary>
        /// Un fotograma (hilo principal): mira si hubo un toque (<paramref name="touched"/>, lo que la vista vio en la pantalla) o
        /// si el flujo cambió, cuenta la inactividad, lanza la consulta al pasar <see cref="DelayMs"/> y sondea su respuesta. Nunca
        /// espera al hilo de trabajo.
        /// </summary>
        /// <param name="flow">El flujo del nivel.</param>
        /// <param name="settingOn"><c>settings.idle_hint</c> (Ajustes › Pista por inactividad).</param>
        /// <param name="touched">La vista vio un toque en este fotograma.</param>
        /// <param name="viewBusy">La vista está ocupada (animaciones, menús, avisos o la tarjeta de Morpho): no cuenta como
        /// inactividad.</param>
        public IdleHintSignal Tick(LevelFlow flow, bool settingOn, bool touched, bool viewBusy)
        {
            if (flow == null)
            {
                throw new ArgumentNullException(nameof(flow));
            }

            DateTime now = flow.Clock.UtcNow;
            bool changed = Remember(flow);
            bool eligible = !viewBusy && Eligible(flow, settingOn);
            if (touched || changed || !eligible)
            {
                return Rearm(eligible, now);
            }

            if (!_armed)
            {
                _armed = true;
                _sinceUtc = now;
                return IdleHintSignal.None;
            }

            if (_spent)
            {
                return IdleHintSignal.None;
            }

            return _querying ? PollQuery(flow) : MaybeStart(flow, settingOn, now);
        }

        /// <summary>Nivel nuevo o reabierto (o la vista se rehízo): sin consulta, sin pulso y la cuenta desde cero.</summary>
        public void Reset()
        {
            AbandonQuery();
            _armed = false;
            _spent = false;
            PulseSource = -1;
            _session = null;
            _board = null;
            _selected = int.MinValue;
            _phase = FlowPhase.Loading;
            _morphoBusy = false;
            _revealSelecting = false;
        }

        /// <summary>
        /// Solo tests: pone otro Morpho (p. ej. con un gancho que retiene el hilo de trabajo). La consulta en curso se abandona.
        /// </summary>
        public void UseMorpho(MorphoBooster morpho)
        {
            AbandonQuery();
            _morpho = morpho ?? throw new ArgumentNullException(nameof(morpho));
        }

        // El nivel está en juego y la pista aplica: PLAYING con el tablero aceptando toques (sin victoria ni bloqueo pendientes)
        // y algún vertido legal, nivel desde hint_idle_from_level con hint_supported, la pista encendida, Morpho callado y sin
        // la selección de la Luciérnaga.
        private static bool Eligible(LevelFlow flow, bool settingOn)
        {
            if (!flow.HasLevel || !flow.AcceptsBoardInput || flow.Session.Status != LevelStatus.Playing)
            {
                return false;
            }

            BoosterConfig config = flow.Boosters;
            return DelayMs(config, settingOn) > 0 && AppliesTo(flow.LevelNumber, config) && flow.Level.HintSupported
                && !flow.HintThinking && !flow.HasActiveHint && !flow.RevealSelecting;
        }

        // Guarda lo que se ve del flujo y dice si cambió desde el fotograma anterior (eso también es actividad del jugador:
        // tocar una cuia, verter, la Marea, la Luciérnaga, Morpho, un nivel nuevo…).
        private bool Remember(LevelFlow flow)
        {
            LevelSession? session = flow.HasLevel ? flow.Session : null;
            BoardState? board = session?.Current;
            int selected = flow.Selected;
            FlowPhase phase = flow.Phase;
            bool morpho = flow.HintThinking || flow.HasActiveHint;
            bool reveal = flow.RevealSelecting;
            bool changed = !ReferenceEquals(session, _session) || !ReferenceEquals(board, _board) || selected != _selected
                || phase != _phase || morpho != _morphoBusy || reveal != _revealSelecting;
            _session = session;
            _board = board;
            _selected = selected;
            _phase = phase;
            _morphoBusy = morpho;
            _revealSelecting = reveal;
            return changed;
        }

        // Toque, cambio o fuera de juego: la consulta se abandona, el pulso en curso se apaga y la cuenta empieza de nuevo (si
        // el nivel sigue en juego) desde ahora.
        private IdleHintSignal Rearm(bool eligible, DateTime now)
        {
            AbandonQuery();
            bool hadPulse = PulseSource >= 0;
            PulseSource = -1;
            _spent = false;
            _armed = eligible;
            _sinceUtc = now;
            return hadPulse ? IdleHintSignal.Interrupted : IdleHintSignal.None;
        }

        private IdleHintSignal MaybeStart(LevelFlow flow, bool settingOn, DateTime now)
        {
            double idleMs = (now - _sinceUtc).TotalMilliseconds;
            if (idleMs < 0)
            {
                // Reloj del dispositivo hacia atrás: la cuenta empieza otra vez desde ahora.
                _sinceUtc = now;
                return IdleHintSignal.None;
            }

            if (idleMs < DelayMs(flow.Boosters, settingOn))
            {
                return IdleHintSignal.None;
            }

            LevelSession session = flow.Session;
            MorphoBooster morpho = MorphoFor(flow);
            if (!morpho.Start(session.Current, session.Initial, session.PourMode, session.FutileRuleEnabled, flow.LevelNumber))
            {
                return IdleHintSignal.None; // aún para una consulta abandonada: se vuelve a intentar en el siguiente fotograma
            }

            _querying = true;
            _queryBoard = session.Current;
            QueryCount++;
            return IdleHintSignal.None;
        }

        private IdleHintSignal PollQuery(LevelFlow flow)
        {
            MorphoBooster morpho = MorphoFor(flow);
            switch (morpho.Poll(out HintResult result))
            {
                case MorphoPoll.Done:
                    _querying = false;
                    _spent = true;
                    LastAnswer = result.Answer;
                    BoardState current = flow.Session.Current;
                    if (PulsesOn(result.Answer) && ReferenceEquals(current, _queryBoard)
                        && PourRules.IsLegal(current, result.Move.Source, result.Move.Target))
                    {
                        PulseSource = result.Move.Source; // solo el origen: el destino nunca se enseña
                        PulseCount++;
                        return IdleHintSignal.Pulse;
                    }

                    return IdleHintSignal.Silent;
                case MorphoPoll.Idle:
                    _querying = false; // no debería pasar (la consulta no se abandonó): se vuelve a lanzar
                    return IdleHintSignal.None;
                default:
                    return IdleHintSignal.None;
            }
        }

        private MorphoBooster MorphoFor(LevelFlow flow)
        {
            MorphoBooster morpho = _morpho ??= new MorphoBooster(flow.Boosters, flow.Clock, _workerHook);
            if (!ReferenceEquals(morpho.Clock, flow.Clock))
            {
                morpho.Clock = flow.Clock; // hint_timeout_ms con el mismo reloj que el flujo
            }

            return morpho;
        }

        private void AbandonQuery()
        {
            if (!_querying)
            {
                return;
            }

            _querying = false;
            _queryBoard = null;
            _morpho?.Abandon();
        }
    }
}
