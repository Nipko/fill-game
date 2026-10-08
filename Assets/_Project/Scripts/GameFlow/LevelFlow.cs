#nullable enable
using System;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>
    /// Máquina de estados de la pantalla de nivel (GDD 02 §2) del prototipo v0: LOADING → INTRO → PLAYING →
    /// CELEBRATION → VICTORY_SCREEN y PLAYING → STUCK_MODAL, con Marea (04 §2.1) y Volver a la orilla (04 §2.7).
    /// Sin motor: el tiempo entra por <see cref="Tick"/> y la vista solo informa si su cola de animación está vacía.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Las reglas viven en el núcleo: cada toque pasa por <see cref="TapController"/> y
    /// <see cref="LevelSession"/>; victoria y bloqueo son los de <see cref="LevelSession.Status"/> (R10, R12, R13).</description></item>
    /// <item><description>Cada nivel se juega con el modo de vertido de <see cref="PourMode"/> (D-005: por defecto
    /// <see cref="AF.Core.Rules.PourMode.Run"/>, la racha que cabe), fijado al cargarlo y que nunca cambia a mitad del
    /// nivel.</description></item>
    /// <item><description>Tras la victoria o el bloqueo lógico el input queda bloqueado al instante (R15) y el flujo espera a que
    /// la cola visual se vacíe (02 §3.2) antes de celebrar o de abrir el modal; el modal se abre
    /// <c>stuck_modal_delay_ms</c> después (R15).</description></item>
    /// <item><description>El progreso se guarda en el instante lógico de la victoria: siguiente nivel, y tras el último del
    /// paquete, el primero (docs/dev/prototipo-v0.md c).</description></item>
    /// <item><description>VICTORY_SCREEN sin auto-avance: <c>victory_autonext_s</c> = 0 (02 §6; 07 §5.4); solo Siguiente
    /// avanza.</description></item>
    /// <item><description>Marea solo existe en su forma ilimitada de los niveles 1-<c>undo_unlimited_until_level</c> (04 §2.1):
    /// el contador "3/3" desde el 11 no está en v0, así que el flujo rechaza paquetes con niveles mayores.</description></item>
    /// </list>
    /// No es seguro entre hilos.
    /// </remarks>
    public sealed class LevelFlow
    {
        private readonly LevelPack _pack;
        private readonly IProgressStore _progress;
        private readonly ILevelFlowListener _listener;
        private readonly TapController _taps;

        private LevelSession? _session;
        private LevelDefinition? _level;
        private PourMode _pourMode;
        private Outcome _pending;
        private bool _drained;
        private int _pendingWaitMs;
        private int _sinceDrainedMs;
        private int _phaseMs;
        private int _introMs;

        /// <summary>Crea el flujo. No carga ningún nivel hasta <see cref="Load"/> o <see cref="LoadSaved"/>.</summary>
        /// <param name="pack">Paquete de niveles.</param>
        /// <param name="progress">Progreso guardado.</param>
        /// <param name="listener">Vista (y sonda de desarrollo).</param>
        /// <param name="tapSwitchSelection">Valor de <c>tap_switch_selection</c> (08 §9; C5).</param>
        /// <param name="pourMode">Modo de vertido de los niveles que se carguen (<c>rules_pour_mode</c>, D-005; por defecto
        /// <see cref="PourModes.Default"/>, la racha que cabe; <see cref="AF.Core.Rules.PourMode.Single"/> solo en pruebas).
        /// Ver <see cref="PourMode"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pourMode"/> no es un valor definido.</exception>
        public LevelFlow(LevelPack pack, IProgressStore progress, ILevelFlowListener listener, bool tapSwitchSelection = GameConfig.TapSwitchSelection, PourMode pourMode = PourModes.Default)
        {
            _pack = pack ?? throw new ArgumentNullException(nameof(pack));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            for (int i = 0; i < pack.Count; i++)
            {
                int level = pack.Levels[i].Level;
                if (!GameConfig.UndoUnlimited(level))
                {
                    throw new NotSupportedException(
                        "El nivel " + level.ToString(System.Globalization.CultureInfo.InvariantCulture) + " necesita Marea con contador "
                        + "(3 por nivel desde el " + (GameConfig.UndoUnlimitedUntilLevel + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + ", 04 §2.1), que el prototipo v0 no tiene: solo admite niveles 1-"
                        + GameConfig.UndoUnlimitedUntilLevel.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
                }
            }

            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _taps = new TapController(tapSwitchSelection);
            PourMode = pourMode;
            Phase = FlowPhase.Loading;
        }

        private enum Outcome
        {
            None = 0,
            Win = 1,
            Stuck = 2,
        }

        /// <summary>Estado de la pantalla (02 §2).</summary>
        public FlowPhase Phase { get; private set; }

        /// <summary>
        /// Modo de vertido (<c>rules_pour_mode</c>, D-005: por defecto run) con el que se crea la sesión en cada
        /// <see cref="Load"/>. Se fija al cargar el nivel y nunca cambia a mitad. Un
        /// cambio se aplica desde la siguiente carga de nivel, nunca a mitad: el nivel en curso conserva el de su sesión
        /// (<see cref="LevelSession.PourMode"/>), porque el historial de Marea y el par dependen de él. Volver a la orilla
        /// tampoco lo cambia. La cantidad de cada vertido la decide el núcleo con este modo (<c>PourRules.Amount</c>).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El valor no es un <see cref="AF.Core.Rules.PourMode"/> definido.</exception>
        public PourMode PourMode
        {
            get => _pourMode;
            set
            {
                if (!PourModes.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Modo de vertido desconocido: Single o Run.");
                }

                _pourMode = value;
            }
        }

        /// <summary>Paquete de niveles.</summary>
        public LevelPack Pack => _pack;

        /// <summary>Nivel cargado.</summary>
        /// <exception cref="InvalidOperationException">Aún no hay nivel.</exception>
        public LevelDefinition Level => _level ?? throw new InvalidOperationException("No hay ningún nivel cargado.");

        /// <summary>Número del nivel cargado (0 si no hay).</summary>
        public int LevelNumber => _level == null ? 0 : _level.Level;

        /// <summary>Partida del núcleo del nivel cargado.</summary>
        /// <exception cref="InvalidOperationException">Aún no hay nivel.</exception>
        public LevelSession Session => _session ?? throw new InvalidOperationException("No hay ningún nivel cargado.");

        /// <summary>Hay un nivel cargado.</summary>
        public bool HasLevel => _session != null;

        /// <summary>Cuia seleccionada (−1 = ninguna).</summary>
        public int Selected => _taps.Selected;

        /// <summary>Duración de la intro en curso (ms).</summary>
        public int IntroDurationMs => _introMs;

        /// <summary>El tablero acepta toques: PLAYING sin victoria ni bloqueo lógico pendiente (R15).</summary>
        public bool AcceptsBoardInput => Phase == FlowPhase.Playing && _pending == Outcome.None && _session != null;

        /// <summary>Victoria o bloqueo lógico a la espera de que la vista termine (input bloqueado).</summary>
        public bool IsResolving => Phase == FlowPhase.Playing && _pending != Outcome.None;

        /// <summary>Marea está disponible: hay historial y el estado lo permite (PLAYING sin pendientes o STUCK_MODAL).</summary>
        public bool CanUndo => _session != null && _session.CanUndo && (AcceptsBoardInput || Phase == FlowPhase.StuckModal);

        /// <summary>Volver a la orilla está disponible (PLAYING sin pendientes o STUCK_MODAL).</summary>
        public bool CanRestart => _session != null && (AcceptsBoardInput || Phase == FlowPhase.StuckModal);

        /// <summary>Nivel de arranque: <paramref name="requested"/> si el paquete lo tiene; si no, el primero.</summary>
        public int ResolveLevel(int requested)
        {
            LevelDefinition? def = _pack.Find(requested);
            return def == null ? _pack.First.Level : def.Level;
        }

        /// <summary>Carga el nivel guardado en el progreso (o el primero).</summary>
        public void LoadSaved()
        {
            Load(ResolveLevel(_progress.LoadNextLevel()));
        }

        /// <summary>
        /// LOADING del nivel <paramref name="level"/> (o del primero si no existe) y arranque de la intro. La sesión se crea
        /// con el <see cref="PourMode"/> vigente (por defecto run, D-005), que queda fijo hasta la siguiente carga: nunca
        /// cambia a mitad del nivel.
        /// </summary>
        public void Load(int level)
        {
            LevelDefinition def = _pack.Find(level) ?? _pack.First;
            Phase = FlowPhase.Loading;
            _level = def;
            _session = new LevelSession(def.CreateBoard(), GameConfig.FutileRuleEnabled, _pourMode);
            _taps.Clear();
            ResetPending();
            _listener.OnLevelLoaded(def.Level);
            StartIntro(GameConfig.TintDurationMs(def.Level), false);
        }

        /// <summary>
        /// Avanza el reloj <paramref name="deltaMs"/> milisegundos. <paramref name="viewIdle"/> indica que la cola de
        /// animación de la vista está vacía (02 §3.2).
        /// </summary>
        public void Tick(int deltaMs, bool viewIdle)
        {
            if (deltaMs < 0)
            {
                deltaMs = 0;
            }

            switch (Phase)
            {
                case FlowPhase.Intro:
                    _phaseMs += deltaMs;
                    if (_phaseMs >= _introMs)
                    {
                        FinishIntro(false);
                    }

                    break;

                case FlowPhase.Playing:
                    TickPending(deltaMs, viewIdle);
                    break;

                case FlowPhase.Celebration:
                    _phaseMs += deltaMs;
                    if (_phaseMs >= GameConfig.AnimCelebrationMs)
                    {
                        ShowVictory();
                    }

                    break;
            }
        }

        /// <summary>
        /// Toque en cualquier punto de la pantalla mientras no hay input de juego: salta Sumergir y teñir (02 §4.2; el
        /// toque no se interpreta como selección, 02 §14) o la celebración (02 §2). Devuelve true si se consumió.
        /// </summary>
        public bool TapAnywhere()
        {
            if (Phase == FlowPhase.Intro && GameConfig.AnimTintSkippable)
            {
                FinishIntro(true);
                return true;
            }

            if (Phase == FlowPhase.Celebration)
            {
                ShowVictory();
                return true;
            }

            return false;
        }

        /// <summary>Toque en la cuia <paramref name="index"/> (02 §3.1, C1-C6).</summary>
        public TapResult TapCuia(int index)
        {
            if (!AcceptsBoardInput || _session == null)
            {
                return TapResult.Ignored(index, _taps.Selected);
            }

            TapResult result = _taps.OnCuiaTap(_session, index);
            if (result.Kind == TapKind.Ignored)
            {
                return result;
            }

            _listener.OnTap(result);
            if (result.Kind == TapKind.Poured)
            {
                EvaluateStatus();
            }

            return result;
        }

        /// <summary>C7: toque en el fondo; quita la selección si la hay.</summary>
        public TapResult TapBackground()
        {
            if (!AcceptsBoardInput)
            {
                return TapResult.Ignored(-1, _taps.Selected);
            }

            TapResult result = _taps.OnBackgroundTap();
            if (result.Kind != TapKind.Ignored)
            {
                _listener.OnTap(result);
            }

            return result;
        }

        /// <summary>
        /// Botón Marea de la bandeja o del modal (C8 de 02 §3.1): con una cuia seleccionada, primero la deselecciona
        /// (la vista la baja) y después ejecuta <see cref="Undo"/>.
        /// </summary>
        public bool PressUndo()
        {
            ClearSelection();
            return Undo();
        }

        /// <summary>
        /// Botón Volver a la orilla de la cabecera o del modal (C8 de 02 §3.1): deselecciona y ejecuta
        /// <see cref="Restart"/>.
        /// </summary>
        public bool PressRestart()
        {
            ClearSelection();
            return Restart();
        }

        /// <summary>
        /// Quita la selección sin pasar por el input (02 §14: al volver de segundo plano la selección se limpia).
        /// </summary>
        public void ClearSelection()
        {
            TapResult result = _taps.OnBackgroundTap();
            if (result.Kind != TapKind.Ignored)
            {
                _listener.OnTap(result);
            }
        }

        /// <summary>
        /// Marea (04 §2.1): deshace el último vertido. Desde STUCK_MODAL cierra el modal y vuelve a PLAYING
        /// (dirección 9, R15). Ilimitada en los niveles 1-10 (<c>undo_unlimited_until_level</c>). C8: quita la selección.
        /// </summary>
        public bool Undo()
        {
            if (!CanUndo || _session == null)
            {
                return false;
            }

            bool fromModal = Phase == FlowPhase.StuckModal;
            Pour undone = _session.GetAppliedPour(_session.AppliedPours - 1);
            _taps.Clear();
            _session.Undo();
            ResetPending();
            Phase = FlowPhase.Playing;
            _listener.OnUndo(undone, fromModal);
            EvaluateStatus();
            return true;
        }

        /// <summary>
        /// Volver a la orilla (04 §2.7): mismo tablero, historial vacío e INTRO corta. Gratis e ilimitado; sin la
        /// confirmación de ≥ 8 vertidos (fuera del prototipo v0). C8: quita la selección.
        /// </summary>
        public bool Restart()
        {
            if (!CanRestart || _session == null)
            {
                return false;
            }

            bool fromModal = Phase == FlowPhase.StuckModal;
            _taps.Clear();
            _session.Restart();
            ResetPending();
            _listener.OnRestart(fromModal);
            StartIntro(GameConfig.AnimTintDurationMsLate, true);
            return true;
        }

        /// <summary>Siguiente (VICTORY_SCREEN → LOADING(N+1)); tras el último nivel del paquete, el primero.</summary>
        public bool Next()
        {
            if (Phase != FlowPhase.VictoryScreen)
            {
                return false;
            }

            Load(_pack.NextLevelAfter(LevelNumber));
            return true;
        }

        private void StartIntro(int durationMs, bool isShort)
        {
            Phase = FlowPhase.Intro;
            _phaseMs = 0;
            _introMs = durationMs < 0 ? 0 : durationMs;
            _listener.OnIntroStarted(_introMs, isShort);
        }

        private void FinishIntro(bool skipped)
        {
            Phase = FlowPhase.Playing;
            _phaseMs = 0;
            _listener.OnIntroFinished(skipped);
            EvaluateStatus();
        }

        private void EvaluateStatus()
        {
            if (_session == null || _pending != Outcome.None)
            {
                return;
            }

            switch (_session.Status)
            {
                case LevelStatus.Won:
                    BeginPending(Outcome.Win);
                    _progress.SaveNextLevel(_pack.NextLevelAfter(LevelNumber));
                    _listener.OnWin();
                    break;

                case LevelStatus.Stuck:
                    BeginPending(Outcome.Stuck);
                    _listener.OnStuck();
                    break;
            }
        }

        private void BeginPending(Outcome outcome)
        {
            _taps.Clear();
            _pending = outcome;
            _drained = false;
            _pendingWaitMs = 0;
            _sinceDrainedMs = 0;
        }

        private void ResetPending()
        {
            _pending = Outcome.None;
            _drained = false;
            _pendingWaitMs = 0;
            _sinceDrainedMs = 0;
        }

        private void TickPending(int deltaMs, bool viewIdle)
        {
            if (_pending == Outcome.None)
            {
                return;
            }

            if (!_drained)
            {
                _pendingWaitMs += deltaMs;
                if (!viewIdle && _pendingWaitMs < GameConfig.MaxViewDrainWaitMs)
                {
                    return;
                }

                _drained = true;
                _sinceDrainedMs = 0;
                if (_pending == Outcome.Win)
                {
                    StartCelebration();
                }

                return;
            }

            if (_pending == Outcome.Stuck)
            {
                _sinceDrainedMs += deltaMs;
                if (_sinceDrainedMs >= GameConfig.StuckModalDelayMs)
                {
                    _pending = Outcome.None;
                    Phase = FlowPhase.StuckModal;
                    _phaseMs = 0;
                    _listener.OnStuckModalOpened();
                }
            }
        }

        private void StartCelebration()
        {
            _pending = Outcome.None;
            Phase = FlowPhase.Celebration;
            _phaseMs = 0;
            _listener.OnCelebrationStarted();
        }

        private void ShowVictory()
        {
            Phase = FlowPhase.VictoryScreen;
            _phaseMs = 0;
            _listener.OnVictoryScreen();
        }
    }
}
