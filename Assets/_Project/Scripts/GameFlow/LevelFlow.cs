#nullable enable
using System;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>
    /// Máquina de estados de la pantalla de nivel (GDD 02 §2): LOADING → INTRO → PLAYING → CELEBRATION → VICTORY_SCREEN y
    /// PLAYING → STUCK_MODAL, con Marea (04 §2.1), Volver a la orilla (04 §2.7), la bandeja de comodines (B2-1), el
    /// embudo de bloqueo de D-008 (B2-2 y B2-3, en <c>LevelFlow.Funnel.cs</c>), la victoria con la Cosecha y AD_GATE (B2-4,
    /// en <c>LevelFlow.Victory.cs</c>), el nivel en curso guardado tras cada vertido y reabierto al volver a la app (B2-5,
    /// en <c>LevelFlow.Resume.cs</c>), la tarjeta previa de 1 toque LEVEL_CARD (02 §10; en <c>LevelFlow.Cards.cs</c>) y la
    /// Luciérnaga (04 §2.5; en <c>LevelFlow.Reveal.cs</c>). Sin motor: el tiempo entra por <see cref="Tick"/> y la vista solo
    /// informa si su cola de animación está vacía.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Las reglas viven en el núcleo: cada toque pasa por <see cref="TapController"/> y
    /// <see cref="LevelSession"/>; victoria y bloqueo son los de <see cref="LevelSession.Status"/> (R10, R12, R13).</description></item>
    /// <item><description>Cada nivel se juega con el modo de vertido de <see cref="PourMode"/> (D-005: por defecto
    /// <see cref="AF.Core.Rules.PourMode.Run"/>), fijado al cargarlo y que nunca cambia a mitad del nivel.</description></item>
    /// <item><description>Tras la victoria o el bloqueo lógico el input queda bloqueado al instante (R15) y el flujo espera a que
    /// la cola visual se vacíe (02 §3.2) antes de celebrar o de abrir el modal; el modal se abre
    /// <c>stuck_modal_delay_ms</c> después (R15). La verificación «la compra cumple» (04 §6.4) corre en el instante lógico del
    /// bloqueo, dentro de esa espera.</description></item>
    /// <item><description>Los niveles salen de un <see cref="LevelCatalog"/> con números de campaña. El progreso se guarda en el
    /// instante lógico de la victoria: <c>max_level</c> = N y <c>next_level</c> = N + 1 (08 §4.3).</description></item>
    /// <item><description>VICTORY_SCREEN sin auto-avance por tiempo: <c>victory_autonext_s</c> = 0 (02 §6). Avanza Siguiente o,
    /// tras la Cosecha doble, la vista por el mismo camino que Siguiente (D-011, punto 4: es la respuesta al toque del jugador,
    /// no un temporizador de la pantalla).</description></item>
    /// <item><description>Marea con su cupo (<see cref="UndoQuota"/>): ilimitada en 1-10 y 3 por nivel desde el 11, contando el
    /// número de campaña (con <c>af_level_offset</c> [P], el tablero del nivel 1 se juega como el 31 con 3 Mareas).</description></item>
    /// <item><description>Sin <see cref="Wallet"/> (tests del núcleo del flujo) no hay stock, compras ni canoas: Marea solo con el
    /// cupo y nada cuesta canoa.</description></item>
    /// </list>
    /// No es seguro entre hilos.
    /// </remarks>
    public sealed partial class LevelFlow
    {
        private readonly LevelCatalog _catalog;
        private readonly IProgressStore _progress;
        private readonly ILevelFlowListener _listener;
        private readonly TapController _taps;
        private readonly Wallet? _wallet;
        private readonly BoosterConfig _boosters;
        private readonly UndoQuota _undo;
        private readonly RescueBooster _rescue;
        private readonly LevelAttempt _attempt = new LevelAttempt();

        private LevelSession? _session;
        private LevelDefinition? _level;
        private int _levelNumber;
        private PourMode _pourMode;
        private Outcome _pending;
        private bool _drained;
        private int _pendingWaitMs;
        private int _sinceDrainedMs;
        private int _phaseMs;
        private int _introMs;

        /// <summary>Crea el flujo sobre un solo paquete (catálogo sin desplazamiento; tests y prototipo v0).</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pourMode"/> no es un valor definido.</exception>
        public LevelFlow(
            LevelPack pack,
            IProgressStore progress,
            ILevelFlowListener listener,
            bool tapSwitchSelection = GameConfig.TapSwitchSelection,
            PourMode pourMode = PourModes.Default,
            Wallet? wallet = null,
            BoosterConfig? boosters = null)
            : this(LevelCatalog.FromPack(pack ?? throw new ArgumentNullException(nameof(pack))), progress, listener, tapSwitchSelection, pourMode, wallet, boosters)
        {
        }

        /// <summary>Crea el flujo. No carga ningún nivel hasta <see cref="Load"/> o <see cref="LoadSaved"/>.</summary>
        /// <param name="catalog">Catálogo de niveles (índice nivel → paquete, 08 §2.2).</param>
        /// <param name="progress">Progreso guardado.</param>
        /// <param name="listener">Vista (y sonda de desarrollo).</param>
        /// <param name="tapSwitchSelection">Valor de <c>tap_switch_selection</c> (08 §9; C5).</param>
        /// <param name="pourMode">Modo de vertido de los niveles que se carguen (<c>rules_pour_mode</c>, D-005).</param>
        /// <param name="wallet">Cartera del jugador (stock, semillas y canoas); null en los tests del flujo sin economía.</param>
        /// <param name="boosters">Cifras de los comodines (por defecto, las de 08 §9).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pourMode"/> no es un valor definido.</exception>
        public LevelFlow(
            LevelCatalog catalog,
            IProgressStore progress,
            ILevelFlowListener listener,
            bool tapSwitchSelection = GameConfig.TapSwitchSelection,
            PourMode pourMode = PourModes.Default,
            Wallet? wallet = null,
            BoosterConfig? boosters = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _wallet = wallet;
            _boosters = boosters ?? BoosterConfig.Defaults;
            _boosters.Validate();
            _undo = new UndoQuota(_boosters);
            _rescue = new RescueBooster(_boosters);
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
        /// <see cref="Load"/>. Se fija al cargar el nivel y nunca cambia a mitad; Volver a la orilla tampoco lo cambia.
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

        /// <summary>Catálogo de niveles.</summary>
        public LevelCatalog Catalog => _catalog;

        /// <summary>Cartera (null en los tests del flujo sin economía).</summary>
        public Wallet? Wallet => _wallet;

        /// <summary>Cifras de los comodines.</summary>
        public BoosterConfig Boosters => _boosters;

        /// <summary>Lo que el nivel recuerda entre intentos: <c>fails_level</c>, variante, cuias añadidas.</summary>
        public LevelAttempt Attempt => _attempt;

        /// <summary>Nivel cargado.</summary>
        /// <exception cref="InvalidOperationException">Aún no hay nivel.</exception>
        public LevelDefinition Level => _level ?? throw new InvalidOperationException("No hay ningún nivel cargado.");

        /// <summary>Número de campaña del nivel cargado (0 si no hay); con <c>af_level_offset</c>, el desplazado.</summary>
        public int LevelNumber => _level == null ? 0 : _levelNumber;

        /// <summary>Partida del núcleo del nivel cargado.</summary>
        /// <exception cref="InvalidOperationException">Aún no hay nivel.</exception>
        public LevelSession Session => _session ?? throw new InvalidOperationException("No hay ningún nivel cargado.");

        /// <summary>Hay un nivel cargado.</summary>
        public bool HasLevel => _session != null;

        /// <summary>Cuia seleccionada (−1 = ninguna).</summary>
        public int Selected => _taps.Selected;

        /// <summary>Duración de la intro en curso (ms), con el cartel «El río bajó» si lo hay.</summary>
        public int IntroDurationMs => _introMs;

        /// <summary>
        /// «Reducir movimiento» (07 §7.8): la intro pasa a un fundido de <see cref="GameConfig.ReducedMotionIntroMs"/> ms. El
        /// ajuste aún no existe en el perfil; por defecto, apagado.
        /// </summary>
        public bool ReduceMotion { get; set; }

        /// <summary>Cuias generadas del tablero del intento (sin las añadidas): fijan las filas (D-006).</summary>
        public int GeneratedCuias => _session == null ? 0 : _session.Current.Count - _attempt.Added.Count;

        /// <summary>El tablero acepta toques: PLAYING sin victoria ni bloqueo lógico pendiente (R15).</summary>
        public bool AcceptsBoardInput => Phase == FlowPhase.Playing && _pending == Outcome.None && _session != null;

        /// <summary>Victoria o bloqueo lógico a la espera de que la vista termine (input bloqueado).</summary>
        public bool IsResolving => Phase == FlowPhase.Playing && _pending != Outcome.None;

        /// <summary>
        /// Victoria lógica a la espera de que la vista termine el último vertido (PLAYING con la victoria pendiente, antes de
        /// CELEBRATION): el input ya está bloqueado (R15) y la Cosecha ya se cobró (02 §14), pero la vista aún no da la victoria
        /// por hecha (D-011, punto 1): el saldo de arriba no enseña la Cosecha y la bandeja se queda como estaba. Falso con un
        /// bloqueo pendiente (<see cref="IsResolving"/> sí es cierto) y en cualquier otra fase.
        /// </summary>
        public bool IsResolvingWin => Phase == FlowPhase.Playing && _pending == Outcome.Win;

        /// <summary>Estado del botón de Marea: ilimitada, cupo gratis y stock (04 §1.1).</summary>
        public UndoStatus UndoStatus => _undo.Status(_wallet?.Stock.Get(BoosterKind.Undo) ?? 0);

        /// <summary>
        /// Marea se puede usar ahora: hay historial, el estado lo permite (PLAYING sin pendientes o STUCK_MODAL) y queda cupo
        /// o stock (o es ilimitada).
        /// </summary>
        public bool CanUndo => _session != null && _session.CanUndo && UndoAllowedHere && UndoStatus.HasQuota;

        /// <summary>
        /// Volver a la orilla está disponible: PLAYING sin pendientes (cabecera o pausa) o, en 1-30, el pie del modal (rama C).
        /// Desde el 31 el pie del modal es «Rendirse» (D-008).
        /// </summary>
        public bool CanRestart => _session != null && (AcceptsBoardInput || (Phase == FlowPhase.StuckModal && !CanoesLevel));

        /// <summary>Nivel de arranque: <paramref name="requested"/> si el catálogo lo tiene; si no, el primero.</summary>
        public int ResolveLevel(int requested)
        {
            return _catalog.Contains(requested) ? requested : _catalog.FirstLevel;
        }

        /// <summary>Carga el nivel guardado en el progreso (o el primero).</summary>
        public void LoadSaved()
        {
            Load(ResolveLevel(_progress.LoadNextLevel()));
        }

        /// <summary>
        /// LOADING del nivel <paramref name="level"/> (o del primero si no existe) y arranque de la intro, con el cupo de Marea
        /// entero. Volver a entrar en el mismo nivel (tras salir al inicio) conserva <c>fails_level</c> y la variante aliviada,
        /// no las cuias añadidas (04 §2.2: desaparecen al abandonar el nivel). En el nivel 8, la primera vez, Capi regala una
        /// Cuia extra (04 §2.2), en el 12, 3 Morphos (04 §2.3; TU1b), y en el estreno de Aguas turbias (100), una Luciérnaga
        /// (04 §2.5); los regalos quedan anotados en
        /// <c>level_state</c> y no se repiten al salir y volver a entrar. Con <see cref="LevelCardsEnabled"/>, un nivel con
        /// etiqueta o con estreno pasa antes por la tarjeta previa (LEVEL_CARD, 02 §10) y la intro espera a
        /// <see cref="DismissLevelCard"/>. Con <see cref="WorldBannersEnabled"/>, el primer nivel de un mundo (desde el 2), al entrar
        /// en él, abre antes el banner de mundo (<see cref="WorldBanner"/>; en LEVEL_CARD, antes de la tarjeta).
        /// </summary>
        public void Load(int level)
        {
            using (Saves())
            {
                int number = ResolveLevel(level);
                LevelDefinition def = _catalog.Serve(number)!; // con el kill switch (KS1): el override o el publicado con aviso
                bool firstEntry = _attempt.Level != number; // se entra en el nivel (no se vuelve a entrar en él): el banner de mundo
                EndRevealSelection();
                ResetHint();
                ResetLevelCard();
                Phase = FlowPhase.Loading;
                _level = def;
                _levelNumber = number;
                _seed = LevelStateSnapshot.SeedOf(def);
                ResetHarvest();
                _attempt.Enter(number);
                _undo.ResetForLevel(number);
                ResetStuck();
                _session = new LevelSession(BoardFor(def, _attempt.ReliefIndex), GameConfig.FutileRuleEnabled, _pourMode);
                _taps.Clear();
                ResetPending();
                _listener.OnLevelLoaded(number);
                if (ExtraCuiaBooster.ShouldGift(number, _progress.LoadMaxLevel(), _attempt.ExtraCuiaGifted, _wallet, _boosters)
                    && _wallet!.Apply(Transaction.GrantStock(BoosterKind.ExtraCuia, BoosterConfig.ExtraCuiaTutorialUnits)).IsApplied)
                {
                    _attempt.ExtraCuiaGifted = true;
                    _listener.OnExtraCuiaGifted(BoosterConfig.ExtraCuiaTutorialUnits);
                }

                GiftHintOnLoad(number); // los 3 Morphos del 12, una vez (04 §2.3; TU1b)
                bool revealGifted = GiftRevealOnLoad(def, number);
                int introMs = ReduceMotion ? GameConfig.ReducedMotionIntroMs : GameConfig.TintDurationMs(number);
                if (!TryShowWorldBanner(number, firstEntry, introMs, revealGifted)
                    && !TryShowLevelCard(introMs, false, revealGifted, null, 0))
                {
                    StartIntro(introMs, false);
                }

                PersistState(); // con los regalos, en la misma escritura del perfil
            }
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
                case FlowPhase.LevelCard:
                    TickWorldBannerExit(deltaMs); // la salida del banner de mundo, antes de la tarjeta previa o la intro
                    break;

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

                case FlowPhase.NoCanoes:
                    // 04 §7.3 [P]: en cuanto hay una canoa (espera, video, semillas o compra) se vuelve a donde se abrió.
                    if (!CanoesBlockStart())
                    {
                        ExitNoCanoes();
                    }

                    break;
            }

            TickHint(); // Morpho (MO1): respuesta del hilo de trabajo, corte por hint_timeout_ms y pista que se apaga
        }

        /// <summary>
        /// Toque en cualquier punto de la pantalla mientras no hay input de juego: cierra la tarjeta previa (LEVEL_CARD, 1
        /// toque), salta Sumergir y teñir (02 §4.2; el toque no se interpreta como selección, 02 §14) o la celebración (02 §2).
        /// Devuelve true si se consumió.
        /// </summary>
        public bool TapAnywhere()
        {
            if (Phase == FlowPhase.LevelCard)
            {
                return DismissLevelCard();
            }

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

        /// <summary>
        /// Toque en la cuia <paramref name="index"/> (02 §3.1, C1-C6). En LEVEL_CARD cierra la tarjeta (1 toque en cualquier
        /// punto) sin seleccionar nada; con el modo de selección de la Luciérnaga abierto, elige la cuia que se revela (o cancela
        /// si no tiene ocultas; 04 §2.5) y tampoco selecciona: en los dos casos devuelve <see cref="TapKind.Ignored"/>.
        /// </summary>
        public TapResult TapCuia(int index)
        {
            if (Phase == FlowPhase.LevelCard)
            {
                DismissLevelCard();
                return TapResult.Ignored(index, _taps.Selected);
            }

            if (!AcceptsBoardInput || _session == null)
            {
                return TapResult.Ignored(index, _taps.Selected);
            }

            if (_revealSelecting)
            {
                return TapRevealTarget(index);
            }

            TapResult result = _taps.OnCuiaTap(_session, index);
            if (result.Kind == TapKind.Ignored)
            {
                return result;
            }

            _listener.OnTap(result);
            if (result.Kind == TapKind.Poured)
            {
                // 02 R8: el nivel en curso se guarda tras cada vertido, en una sola escritura del perfil; la victoria lo borra
                // y cobra la Cosecha en esa misma escritura.
                using (Saves())
                {
                    EvaluateStatus();
                    PersistState();
                }
            }

            return result;
        }

        /// <summary>
        /// C7: toque en el fondo; quita la selección si la hay. En LEVEL_CARD cierra la tarjeta y con la Luciérnaga eligiendo
        /// cuia cancela sin coste (04 §2.5: «tocar fuera cancela sin costo»).
        /// </summary>
        public TapResult TapBackground()
        {
            if (Phase == FlowPhase.LevelCard)
            {
                DismissLevelCard();
                return TapResult.Ignored(-1, _taps.Selected);
            }

            if (!AcceptsBoardInput)
            {
                return TapResult.Ignored(-1, _taps.Selected);
            }

            if (_revealSelecting)
            {
                EndRevealSelection();
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
        /// Botón Volver a la orilla de la cabecera, de la pausa o del pie del modal en 1-30 (C8 de 02 §3.1): deselecciona y
        /// ejecuta <see cref="Restart"/>.
        /// </summary>
        public bool PressRestart()
        {
            ClearSelection();
            return Restart();
        }

        /// <summary>
        /// Quita la selección sin pasar por el input (02 §14: al volver de segundo plano la selección se limpia), también el
        /// modo de selección de la Luciérnaga, sin coste.
        /// </summary>
        public void ClearSelection()
        {
            EndRevealSelection();
            TapResult result = _taps.OnBackgroundTap();
            if (result.Kind != TapKind.Ignored)
            {
                _listener.OnTap(result);
            }
        }

        /// <summary>
        /// Marea (04 §2.1): deshace el último vertido y gasta una del cupo o del stock (ilimitada en 1-10). Desde
        /// STUCK_MODAL cierra el modal y vuelve a PLAYING (dirección 9, R15); no es fallo. C8: quita la selección. Sin cupo ni
        /// stock devuelve false: la vista ofrece la recarga.
        /// </summary>
        public bool Undo()
        {
            if (!CanUndo || _session == null)
            {
                return false;
            }

            using (Saves())
            {
                UndoSource source = _undo.Consume(_wallet);
                if (source == UndoSource.None)
                {
                    return false;
                }

                bool fromModal = Phase == FlowPhase.StuckModal;
                Pour undone = _session.GetAppliedPour(_session.AppliedPours - 1);
                _taps.Clear();
                EndRevealSelection();
                _session.Undo();
                ResetPending();
                ResetStuck();
                Phase = FlowPhase.Playing;
                _listener.OnUndo(undone, fromModal);
                EvaluateStatus();
                PersistState(); // con la Marea del stock, en la misma escritura del perfil
                return true;
            }
        }

        /// <summary>
        /// Volver a la orilla (04 §2.7): mismo tablero (misma semilla), historial vacío, cupo de Marea entero e INTRO corta;
        /// conserva las cuias añadidas. Desde la cabecera es gratis: con ≥ <c>restart_confirm_min_pours</c> vertidos cuenta
        /// como fallo blando (la vista pide antes la confirmación de una línea: <see cref="RestartNeedsConfirm"/>); desde el 31
        /// con 0 canoas abre Sin canoas (04 §7.1 «Qué bloquea»). Desde el pie del modal en 1-30 es el fallo duro de la rama C
        /// (sin canoa). Al tercer fallo se sirve la variante aliviada (<c>relief[0]</c>), si el nivel la tiene.
        /// </summary>
        public bool Restart()
        {
            if (!CanRestart || _session == null)
            {
                return false;
            }

            using (Saves())
            {
                bool fromModal = Phase == FlowPhase.StuckModal;
                EndRevealSelection();
                if (!fromModal && CanoesBlockStart())
                {
                    _taps.Clear();
                    EnterNoCanoes(FlowPhase.Playing);
                    return true;
                }

                _taps.Clear();
                if (fromModal)
                {
                    Fail(FailKind.Hard);
                }
                else if (_session.AppliedPours >= _boosters.RestartConfirmMinPours)
                {
                    Fail(FailKind.Soft);
                }

                _listener.OnRestart(fromModal);
                BeginAttempt();
                return true;
            }
        }

        /// <summary>
        /// Siguiente (VICTORY_SCREEN → LOADING(N+1)) si el catálogo tiene N + 1, sin AD_GATE; si no (fin del contenido),
        /// devuelve false y no cambia nada. En el juego la navegación (AD_GATE, «El río sigue creciendo» y Sin canoas) la lleva
        /// <see cref="AppFlow.PressNext"/>.
        /// </summary>
        public bool Next()
        {
            if (Phase != FlowPhase.VictoryScreen || !_catalog.Contains(LevelNumber + 1))
            {
                return false;
            }

            Load(LevelNumber + 1);
            return true;
        }

        // Tablero del intento: el base o la variante relief[index], con las cuias ya añadidas al final (persisten).
        private BoardState BoardFor(LevelDefinition def, int reliefIndex)
        {
            BoardState board = reliefIndex >= 0 && reliefIndex < def.Relief.Count ? def.Relief[reliefIndex].CreateBoard() : def.CreateBoard();
            for (int i = 0; i < _attempt.Added.Count && board.Count < BoardState.MaxCuias; i++)
            {
                board = RescueBooster.WithEmptyCuia(board);
            }

            return board;
        }

        // Nuevo intento del mismo nivel (Volver a la orilla, Reintentar, rama C): misma semilla, o la variante aliviada si
        // toca por primera vez (cartel «El río bajó» antes del teñido), cupo de Marea entero e INTRO corta. Sin tarjeta previa
        // salvo con LevelCardOnRetry [P]: entonces la tarjeta va antes del cartel.
        private void BeginAttempt()
        {
            LevelDefinition def = Level;
            EndRevealSelection();
            ResetLevelCard();
            _undo.Restore();
            ResetPending();
            ResetStuck();
            int shortIntro = ReduceMotion ? GameConfig.ReducedMotionIntroMs : GameConfig.AnimTintDurationMsLate;
            int relief = FailResolve.ChooseRelief(def, _attempt, _boosters);
            if (relief >= 0 && relief != _attempt.ReliefIndex)
            {
                _attempt.ReliefIndex = relief;
                _session = new LevelSession(BoardFor(def, relief), GameConfig.FutileRuleEnabled, _session!.PourMode);
                Phase = FlowPhase.Loading;
                _listener.OnLevelLoaded(LevelNumber);
                int banner = Math.Max(0, _boosters.AnimRiverLoweredBannerMs);
                string reliefType = def.Relief[relief].Type;
                if (!(LevelCardOnRetry && TryShowLevelCard(banner + shortIntro, true, false, reliefType, banner)))
                {
                    _listener.OnRiverLowered(reliefType, banner);
                    StartIntro(banner + shortIntro, true);
                }

                PersistState();
                return;
            }

            _session!.Restart();
            if (!(LevelCardOnRetry && TryShowLevelCard(shortIntro, true, false, null, 0)))
            {
                StartIntro(shortIntro, true);
            }

            PersistState();
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
                    ResolveWin();
                    break;

                case LevelStatus.Stuck:
                    BeginPending(Outcome.Stuck);
                    CheckRescue();
                    _listener.OnStuck();
                    break;
            }
        }

        private void BeginPending(Outcome outcome)
        {
            EndRevealSelection();
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
                    OpenStuckModal();
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
