#nullable enable
using System;
using AF.Ads;
using AF.Boosters;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>Lo que necesita la partida del Reto del juego que la aloja (<c>GameBootstrap.SetUpDaily</c> o un test).</summary>
    public sealed class DailyPlayContext
    {
        /// <summary>Crea el contexto.</summary>
        public DailyPlayContext(
            IDailyChallengeStore dailyStore,
            IClock clock,
            IProgressStore campaignProgress,
            Wallet dailyWallet,
            GameServices? services,
            ProfileSaveBatch? saveBatch,
            SettingsData? settings,
            bool tapSwitchSelection,
            PourMode pourMode,
            BoosterConfig? campaignBoosters = null)
        {
            DailyStore = dailyStore ?? throw new ArgumentNullException(nameof(dailyStore));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            CampaignProgress = campaignProgress ?? throw new ArgumentNullException(nameof(campaignProgress));
            DailyWallet = dailyWallet ?? throw new ArgumentNullException(nameof(dailyWallet));
            Services = services;
            SaveBatch = saveBatch;
            Settings = settings;
            TapSwitchSelection = tapSwitchSelection;
            PourMode = pourMode;
            CampaignBoosters = campaignBoosters;
        }

        /// <summary>Bloque <c>daily_challenge</c> del perfil (en el juego, E4; en los tests, en memoria).</summary>
        public IDailyChallengeStore DailyStore { get; }

        /// <summary>Reloj del juego (el mismo que la cartera).</summary>
        public IClock Clock { get; }

        /// <summary>Progreso de la campaña (solo se lee: 06 §4.3).</summary>
        public IProgressStore CampaignProgress { get; }

        /// <summary>Cartera del Reto (<see cref="LevelFlow.CreateDailyWallet"/>) sobre el perfil de la campaña.</summary>
        public Wallet DailyWallet { get; }

        /// <summary>Servicios del HUD con la cartera del Reto (o null: sin saldos ni videos).</summary>
        public GameServices? Services { get; }

        /// <summary>Lote de guardado del perfil (o null).</summary>
        public ProfileSaveBatch? SaveBatch { get; }

        /// <summary>Ajustes del jugador (sonda de desarrollo), o null.</summary>
        public SettingsData? Settings { get; }

        /// <summary><c>tap_switch_selection</c>.</summary>
        public bool TapSwitchSelection { get; }

        /// <summary><c>rules_pour_mode</c> (D-005).</summary>
        public PourMode PourMode { get; }

        /// <summary>Cifras de los comodines de la campaña (null = las de 08 §9).</summary>
        public BoosterConfig? CampaignBoosters { get; }
    }

    /// <summary>
    /// Partida del Reto del río sobre el tablero (tarea R9; 06 §4.1 «Pantalla»: «el tablero con el HUD normal, la cabecera
    /// "Reto del río" y sin barra de Crecida»; nota §9). Compone sin editar la pantalla de campaña:
    /// <list type="bullet">
    /// <item>una <see cref="GameScreen"/> propia (el HUD normal: bandeja de comodines, pausa, Volver a la orilla, Aguas
    /// estancadas), construida la primera vez que se abre y reutilizada, con sus acciones (<see cref="DailyGameActions"/>) sobre el
    /// flujo del Reto (<see cref="LevelFlow.CreateDaily"/>, R8), sin Mapa, fauna, paradas ni Frutas del río;</item>
    /// <item>esta clase como oyente del flujo: reenvía todo a la vista salvo la pantalla de victoria de campaña (Cosecha,
    /// Siguiente), que sustituye por el calendario de sellos (<see cref="DailyStampCalendar"/>), y pone la cabecera «Reto del
    /// río» en la insignia del nivel (<see cref="GameScreen.LevelTitle"/>): nunca «Nivel N» (el nivel efectivo de R8 no se
    /// enseña);</item>
    /// <item>el fondo es el genérico del río: esta vista no recibe <c>RiverStops</c> (el fondo por parada se elige con el número
    /// de nivel, y aquí sería el nivel efectivo, que no es un lugar del Mapa). El kit del mundo del día (R-Q2: rota por los
    /// mundos 1-9 según la fecha) queda pendiente de que <c>GameScreen</c> acepte un mundo en lugar de un nivel;</item>
    /// <item>la Crecida aún no existe en el cliente: no hay barra que quitar (cuando llegue a <see cref="GameScreen"/>, hará falta
    /// el enganche E25 para ocultarla con <see cref="LevelFlow.IsDaily"/>).</item>
    /// </list>
    /// Resultado (R7b): la victoria llama a <see cref="DailyChallengeService.ClaimWin"/> (que reclama la fecha de inicio, R-Q3);
    /// salir sin ganar, a <see cref="LevelFlow.ExitDaily"/>, que avisa con <see cref="DailyPlayOutcome.Exit"/> y aquí se registra con
    /// <see cref="DailyChallengeService.RecordExitWithoutWin"/> (R-Q9). La partida se guarda tras cada vertido en
    /// <see cref="DailyLevelStateStore"/> y se reanuda al reabrir el mismo día (R-Q4).
    /// <para>Quien la aloja la avanza con <see cref="Tick"/> (el <c>DailyChallengeRunner</c> en el juego) y se entera del cierre
    /// por la acción <c>onClosed</c>.</para>
    /// </summary>
    public sealed class DailyChallengeScreen : ILevelFlowListener, IHintListener, IRevealListener, ILevelCardListener, IWorldBannerListener
    {
        /// <summary>Tamaño mínimo de la cabecera «Reto del río» en la insignia (sp): cabe en los 106 dp de la insignia.</summary>
        public const float HeaderMinSp = 14f;

        private readonly Transform _host;
        private readonly ProceduralArt _art;
        private readonly DailyChallengeService _service;
        private readonly DailyPlayContext _context;
        private readonly Action _onClosed;
        private readonly DailyGameActions _actions;
        private readonly DailyStampCalendar _calendar;
        private GameScreen? _view;
        private LevelFlow? _flow;
        private DailyLevelStateStore? _stateStore;
        private float _msCarry;
        private double _playMs;
        private string? _levelStartBoard;
        private int _levelStartBand;
        private bool _won;
        private bool _calendarFromCard;

        /// <summary>Crea la pantalla (sin construir aún el tablero: se construye al abrir la primera partida).</summary>
        public DailyChallengeScreen(Transform host, ProceduralArt art, DailyChallengeService service, DailyPlayContext context, Action onClosed)
        {
            _host = host ? host : throw new ArgumentNullException(nameof(host));
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _onClosed = onClosed ?? throw new ArgumentNullException(nameof(onClosed));
            _actions = new DailyGameActions(this);
            _calendar = new DailyStampCalendar(host, art, () => GoHome(), () => PressDoubleHarvest());
        }

        /// <summary>Hay una partida (o el calendario desde la tarjeta) abierta.</summary>
        public bool IsOpen => _flow != null || (_calendarFromCard && _calendar.Visible);

        /// <summary>Flujo de la partida en curso, o null.</summary>
        public LevelFlow? Flow => _flow;

        /// <summary>Vista del tablero (null hasta la primera partida).</summary>
        public GameScreen? View => _view;

        /// <summary>
        /// Prepara el flujo y la vista de cada partida antes de construir el tablero (bloque B9: Reducir movimiento, su factor, Alto
        /// contraste y el lector de pantalla, que pone <c>GameBootstrap.A11y</c>); null, nada.
        /// </summary>
        public Action<LevelFlow, GameScreen>? Prepare { get; set; }

        /// <summary>Almacén de la partida abierta (R-Q4), o null.</summary>
        public DailyLevelStateStore? StateStore => _stateStore;

        /// <summary>Calendario de sellos.</summary>
        public DailyStampCalendar Calendar => _calendar;

        /// <summary>Acciones de la vista (los mismos puntos de entrada que usan los tests).</summary>
        public IGameActions Actions => _actions;

        /// <summary>Servicio del Reto.</summary>
        public DailyChallengeService Service => _service;

        /// <summary>La partida abierta se reanudó desde lo guardado (R-Q4).</summary>
        public bool Resumed { get; private set; }

        /// <summary>Se ganó la partida abierta.</summary>
        public bool Won => _won;

        /// <summary>Resultado del último cobro (tras la victoria), o null.</summary>
        public ClaimOutcome? LastClaimOutcome { get; private set; }

        /// <summary>Detalle del último cobro, o null.</summary>
        public DailyClaimReport? LastClaim { get; private set; }

        /// <summary>
        /// La partida abierta terminó con «Salir» sin ganar. R-Q16 (a): la partida sigue reanudable el mismo día
        /// (<see cref="DailyChallengeService.RecordExitWithoutWin"/> la devuelve) y la tarjeta dice «Seguir»; salir no es un
        /// resultado: el <c>fail</c> del día lo informa <see cref="DailyChallengeService.DayEnded"/>, una vez y solo si el día
        /// termina sin victoria (R-Q15 a).
        /// </summary>
        public bool ExitedWithoutWin { get; private set; }

        /// <summary>
        /// Abre la partida de hoy para un jugador de nivel máximo <paramref name="maxLevel"/>: la empieza o, si hay una de hoy
        /// guardada, la reanuda (R-Q4); una de otro día se descarta antes (<see cref="DailyChallengeService.Start"/>). False, sin
        /// cambiar nada, si la tarjeta está oculta (D-008) o ya hay una abierta.
        /// </summary>
        public bool Open(int maxLevel)
        {
            if (IsOpen || _service.CardState(maxLevel) == DailyCardState.Hidden)
            {
                return false;
            }

            DailyPool pool = _service.Pool!;
            DailyPlayInProgress play = _service.Start(maxLevel);
            LevelDefinition board = pool.FindById(play.BoardId)
                ?? throw new InvalidOperationException("El pool no trae el tablero " + play.BoardId + ".");
            var stateStore = new DailyLevelStateStore(_context.DailyStore, _context.Clock);
            LevelStateData? saved = stateStore.Load();
            int effective = LevelFlow.DailyEffectiveLevel(maxLevel, saved, board);
            LevelFlow flow = LevelFlow.CreateDaily(
                board, effective, _context.CampaignProgress, this, _context.DailyWallet, _context.CampaignBoosters,
                _context.TapSwitchSelection, _context.PourMode);
            flow.StateStore = stateStore;
            flow.SaveBatch = _context.SaveBatch;
            flow.Clock = _context.Clock;
            flow.DailyResult += OnDailyResult;
            _flow = flow;
            _stateStore = stateStore;
            _won = false;
            _msCarry = 0f;
            _playMs = 0d;
            LastClaim = null;
            LastClaimOutcome = null;
            HarvestDoubled = false;
            ExitedWithoutWin = false;
            _calendar.Hide();
            _calendarFromCard = false;

            GameScreen view = EnsureView();
            Prepare?.Invoke(flow, view); // B9: antes de que StartDaily/ResumeDaily construya el tablero
            view.Bind(flow, _context.Services);
            view.SetVisible(true);
            _levelStartBoard = play.BoardId; // level_start pendiente: sale al terminar Llenar y teñir (08 §8.2), una vez por partida
            _levelStartBand = play.Band;
            Resumed = saved != null && flow.ResumeDaily(saved);
            if (!Resumed)
            {
                if (saved != null)
                {
                    stateStore.Clear(); // no se pudo rehacer: se empieza de nuevo el mismo tablero
                }

                flow.StartDaily();
            }

            if (Resumed && flow.Phase != FlowPhase.Intro)
            {
                ProbeLevelStart(); // reanudada sin intro: no hay Llenar y teñir que esperar
            }

            ApplyHeader();
            return true;
        }

        /// <summary>
        /// Calendario del mes desde la tarjeta (Sellado o Pendiente de validar) [P]: solo los sellos, sin premio. False si hay una
        /// partida abierta.
        /// </summary>
        public bool OpenCalendar()
        {
            if (_flow != null)
            {
                return false;
            }

            _calendarFromCard = true;
            _calendar.Show(_context.Clock.LocalDate(), _context.DailyStore.Load(), null, false);
            return true;
        }

        /// <summary>
        /// Avanza la vista y el flujo <paramref name="dtMs"/> ms (como <c>GameBootstrap.Update</c> con la campaña) y el calendario.
        /// Sin asignaciones por fotograma.
        /// </summary>
        public void Tick(float dtMs)
        {
            _calendar.Tick(dtMs);
            LevelFlow? flow = _flow;
            GameScreen? view = _view;
            if (flow == null || view == null)
            {
                return;
            }

            view.Tick(dtMs);
            if (!_won)
            {
                _playMs += dtMs; // t_ms de af_daily_challenge: tiempo de la partida abierta hasta ganar (R10)
            }

            _msCarry += dtMs;
            int whole = (int)_msCarry;
            _msCarry -= whole;
            flow.Tick(whole, view.IsViewIdle);
        }

        /// <summary>
        /// Salir (pausa › Salir, la X de Nivel fallido, Atrás): sin ganar, <see cref="LevelFlow.ExitDaily"/> (sin canoa ni fallo, 04
        /// §7.1). Con R-Q16 salir no es un resultado: la partida queda reanudable el mismo día y el <c>fail</c> lo informa el servicio si
        /// el día termina sin victoria (R-Q15). Ya ganado, solo se cierra. Vuelve al inicio. False si no hay nada abierto.
        /// </summary>
        public bool Exit()
        {
            if (_flow == null)
            {
                if (_calendarFromCard && _calendar.Visible)
                {
                    CloseAll();
                    return true;
                }

                return false;
            }

            if (!_won && !_flow.ExitDaily())
            {
                // ExitDaily no aplica (p. ej. aún sin tablero): se registra igual si la partida sigue abierta en el servicio.
                RecordExit();
            }

            CloseAll();
            return true;
        }

        /// <summary>«Inicio» del calendario: cierra la partida ganada (o el calendario de la tarjeta) y vuelve al inicio.</summary>
        public bool GoHome()
        {
            if (_flow != null && !_won)
            {
                return Exit();
            }

            if (!IsOpen)
            {
                return false;
            }

            CloseAll();
            return true;
        }

        /// <summary>
        /// Tope de la Cosecha doble del Reto (E9): el premio más alto que puede pagar el Reto,
        /// <c>live_daily_challenge_reward.seeds</c> × <c>murky_bonus_mult</c> (08 §9; 60 × 2). Un premio mayor no se dobla.
        /// </summary>
        public int MaxDoubleHarvestSeeds
        {
            get
            {
                DailyChallengeReward reward = _service.Config.LiveDailyChallengeReward;
                return reward.Seeds * Math.Max(1, reward.MurkyBonusMult);
            }
        }

        /// <summary>La Cosecha doble del Reto ya se cobró en esta victoria (E9).</summary>
        public bool HarvestDoubled { get; private set; }

        /// <summary>
        /// La Cosecha doble del Reto se ofrece (E9; 04 §2.8; 06 §4.1): tras ganar con el premio <b>confirmado</b> y pagado (nunca
        /// con «pendiente de validar» ni sin premio), una vez por victoria, desde <c>ads_rv_double_harvest_from_level</c> (el nivel
        /// efectivo del Reto) y con video disponible en <c>rv_double_harvest</c> (reglas, tope diario de 5 compartido con la
        /// campaña, cooldown y fill de <c>AF.Ads</c>): sin anuncio el botón no aparece (02 §6).
        /// </summary>
        public bool DoubleHarvestOffered
        {
            get
            {
                LevelFlow? flow = _flow;
                GameServices? services = _context.Services;
                DailyClaimReport? claim = LastClaim;
                return DailyStampCalendar.DoubleHarvestHookReady && _won && !HarvestDoubled && flow != null && services != null
                    && LastClaimOutcome == ClaimOutcome.Confirmed && claim.HasValue && claim.Value.Outcome == ClaimOutcome.Confirmed
                    && claim.Value.SeedsPaid > 0 && claim.Value.SeedsPaid <= MaxDoubleHarvestSeeds
                    && flow.LevelNumber >= _context.DailyWallet.Config.AdsRvDoubleHarvestFromLevel
                    && services.RewardedReady(AdPlacement.DoubleHarvest);
            }
        }

        /// <summary>
        /// «Cosecha doble» del calendario (E9): el anuncio simulado de <c>rv_double_harvest</c> y, solo si se vio entero, otras
        /// tantas semillas como el premio del día con <see cref="Transaction.DoubleHarvestDaily"/>. False si no se ofrece.
        /// </summary>
        public bool PressDoubleHarvest()
        {
            if (!DoubleHarvestOffered)
            {
                return false;
            }

            int seeds = LastClaim!.Value.SeedsPaid;
            int level = _flow!.LevelNumber;
            _context.Services!.Ads.ShowRewarded(AdPlacement.DoubleHarvest, r =>
            {
                if (!r.IsRewarded || HarvestDoubled)
                {
                    return;
                }

                TransactionResult result = _context.DailyWallet.Apply(Transaction.DoubleHarvestDaily(seeds, level));
                if (!result.IsApplied)
                {
                    return;
                }

                HarvestDoubled = true;
                DevProbe.DailyEarn(result.SeedsDelta, DailyProbeFormat.DoubleHarvestSourceId); // 08 §8.2: source = double_harvest
                _calendar.ShowDoubled(seeds + result.SeedsDelta);
            });
            return true;
        }

        /// <summary>
        /// Atrás de Android con el Reto abierto (enganche E22): solo cierra el calendario (el de la tarjeta y el de después de
        /// ganar, como «Inicio»). Con el tablero en juego no hace nada y devuelve false, como en un nivel de campaña (02 §3; 04
        /// §6.6: Atrás no saca del nivel).
        /// </summary>
        public bool HandleBack()
        {
            return _calendar.Visible && GoHome();
        }

        // ---- ILevelFlowListener (todo a la vista salvo la pantalla de victoria de campaña) ----

        /// <inheritdoc />
        public void OnLevelLoaded(int level)
        {
            RequireView().OnLevelLoaded(level);
            ApplyHeader();
        }

        /// <inheritdoc />
        public void OnIntroStarted(int durationMs, bool isShort)
        {
            RequireView().OnIntroStarted(durationMs, isShort);
        }

        /// <inheritdoc />
        public void OnIntroFinished(bool skipped)
        {
            RequireView().OnIntroFinished(skipped);
            ProbeLevelStart();
        }

        /// <inheritdoc />
        public void OnTap(TapResult result)
        {
            RequireView().OnTap(result);
        }

        /// <inheritdoc />
        public void OnUndo(Pour undone, bool fromStuckModal)
        {
            RequireView().OnUndo(undone, fromStuckModal);
        }

        /// <inheritdoc />
        public void OnRestart(bool fromStuckModal)
        {
            RequireView().OnRestart(fromStuckModal);
        }

        /// <inheritdoc />
        public void OnWin()
        {
            RequireView().OnWin();
        }

        /// <inheritdoc />
        public void OnStuck()
        {
            RequireView().OnStuck();
        }

        /// <inheritdoc />
        public void OnCelebrationStarted()
        {
            RequireView().OnCelebrationStarted();
        }

        /// <summary>En lugar de la victoria de campaña (Cosecha, Siguiente): el calendario con el sello que cae y el premio.</summary>
        public void OnVictoryScreen()
        {
            // R-Q14 (a): la Pororoca de la racha entra en el stock aunque aún no se pueda usar; antes del nivel en que se
            // desbloquea, el calendario lo dice.
            LevelFlow? flow = _flow;
            int shuffleUnlock = flow != null && flow.LevelNumber < flow.Boosters.ShuffleUnlockLevel ? flow.Boosters.ShuffleUnlockLevel : 0;
            _calendar.Show(_context.Clock.LocalDate(), _context.DailyStore.Load(), LastClaim, DoubleHarvestOffered, shuffleUnlock);
        }

        /// <inheritdoc />
        public void OnStuckModalOpened()
        {
            RequireView().OnStuckModalOpened();
        }

        /// <inheritdoc />
        public void OnCuiaAdded(int index, AddedCuiaKind kind)
        {
            RequireView().OnCuiaAdded(index, kind);
        }

        /// <inheritdoc />
        public void OnExtraCuiaGifted(int units)
        {
            RequireView().OnExtraCuiaGifted(units);
        }

        /// <inheritdoc />
        public void OnFunnelPhase(FlowPhase phase)
        {
            RequireView().OnFunnelPhase(phase);
        }

        /// <inheritdoc />
        public void OnFailResolved(FailOutcome outcome)
        {
            RequireView().OnFailResolved(outcome);
        }

        /// <inheritdoc />
        public void OnRiverLowered(string reliefType, int bannerMs)
        {
            RequireView().OnRiverLowered(reliefType, bannerMs);
        }

        /// <inheritdoc />
        public void OnHarvestDoubled(int totalSeeds)
        {
            RequireView().OnHarvestDoubled(totalSeeds);
        }

        /// <inheritdoc />
        public void OnAdGate()
        {
            RequireView().OnAdGate();
        }

        /// <inheritdoc />
        public void OnResumed(FlowPhase phase)
        {
            RequireView().OnResumed(phase);
            ApplyHeader();
        }

        /// <inheritdoc />
        public void OnHintThinking(bool thinking)
        {
            RequireView().OnHintThinking(thinking);
        }

        /// <inheritdoc />
        public void OnHint(in HintResult result)
        {
            RequireView().OnHint(in result);
        }

        /// <inheritdoc />
        public void OnHintCleared()
        {
            RequireView().OnHintCleared();
        }

        /// <inheritdoc />
        public void OnHintGifted(int units)
        {
            RequireView().OnHintGifted(units);
        }

        /// <inheritdoc />
        public void OnRevealGifted(int units)
        {
            RequireView().OnRevealGifted(units);
        }

        /// <inheritdoc />
        public void OnRevealSelection(bool selecting)
        {
            RequireView().OnRevealSelection(selecting);
        }

        /// <inheritdoc />
        public void OnRevealed(int cuia, int layerMask)
        {
            RequireView().OnRevealed(cuia, layerMask);
        }

        /// <inheritdoc />
        public void OnLevelCard(LevelCardInfo card)
        {
            RequireView().OnLevelCard(card); // el Reto no tiene tarjeta previa (LevelCardsEnabled = false, R8)
        }

        /// <inheritdoc />
        public void OnWorldBanner(int world, int level)
        {
            RequireView().OnWorldBanner(world, level); // ni banner de mundo (WorldBannersEnabled = false, R8)
        }

        private void OnDailyResult(DailyPlayResult result)
        {
            switch (result.Outcome)
            {
                case DailyPlayOutcome.Win:
                    _won = true;
                    try
                    {
                        LastClaimOutcome = _service.ClaimWin();
                        DailyClaimReport claim = _service.Rules.LastClaim;
                        LastClaim = claim;
                        ProbeWin(claim);
                    }
                    catch (InvalidOperationException e)
                    {
                        Debug.LogError("[AF.Daily] victoria sin partida en curso en el servicio: " + e.Message);
                    }

                    break;
                case DailyPlayOutcome.Exit:
                    RecordExit();
                    break;
            }
        }

        /// <summary>
        /// <c>t_ms</c> de la victoria en la sonda: lo jugado desde que se abrió la partida; null si se reanudó (lo jugado antes de
        /// cerrar la app no se guarda en el registro) [P].
        /// </summary>
        public long? PlayTimeMs => Resumed ? (long?)null : (long)_playMs;

        // R10: AF_STATE level_start (level_name d{b}_n{nnnn}, 08 §8.2) al terminar Llenar y teñir, o al reanudar sin intro; una vez
        // por partida abierta: «Volver a la orilla» repite la intro pero no la escribe otra vez [P].
        private void ProbeLevelStart()
        {
            string? board = _levelStartBoard;
            if (board == null)
            {
                return;
            }

            _levelStartBoard = null;
            DevProbe.DailyLevelStart(board, _levelStartBand);
        }

        // R10: AF_STATE daily_challenge result=win (nada sin premio: rejugada de un día cobrado o reloj atrasado) y, si se pagó ahora,
        // earn_virtual_currency source=daily_challenge. La Pororoca de la racha no tiene evento en 08 §8.2: no se inventa.
        private void ProbeWin(in DailyClaimReport claim)
        {
            DevProbe.DailyWin(claim, PlayTimeMs);
            DevProbe.DailyEarn(claim.SeedsPaid);
        }

        private void RecordExit()
        {
            if (_service.Resumable == null && _context.DailyStore.Load().InProgress == null)
            {
                return;
            }

            try
            {
                _service.RecordExitWithoutWin();
                ExitedWithoutWin = true;
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError("[AF.Daily] salida sin partida en curso en el servicio: " + e.Message);
            }
        }

        private void CloseAll()
        {
            LevelFlow? flow = _flow;
            if (flow != null)
            {
                flow.DailyResult -= OnDailyResult;
            }

            _flow = null;
            _stateStore = null;
            _calendarFromCard = false;
            _levelStartBoard = null; // salir durante la intro: la partida no llegó a empezar
            _calendar.Hide();
            _view?.SetVisible(false);
            _onClosed();
        }

        private void ApplyHeader()
        {
            GameScreen? view = _view;
            if (view == null)
            {
                return;
            }

            // 06 §4.1: cabecera «Reto del río»; el nivel efectivo (R8) nunca se enseña como «Nivel N».
            view.LevelTitle.fontSizeMin = Mathf.Min(view.LevelTitle.fontSizeMin, HeaderMinSp);
            view.LevelTitle.text = DailyText.Title;
        }

        private GameScreen EnsureView()
        {
            if (_view == null)
            {
                var probe = new DevProbe { Settings = _context.Settings ?? new SettingsData() };
                _view = new GameScreen(_host, _art, probe, _actions);
                _view.SetVisible(false);
            }

            return _view;
        }

        private GameScreen RequireView()
        {
            return _view ?? throw new InvalidOperationException("La partida del Reto no tiene vista.");
        }

        /// <summary>
        /// Acciones de la vista del Reto (<see cref="IGameActions"/>) sobre su flujo: el tablero, Marea, Volver a la orilla, Cuia
        /// extra, Morpho (con stock o semillas) y la recarga de Marea con semillas, como en la campaña (R-Q8); «Salir» y la X de
        /// Nivel fallido salen del Reto. No hay canoas, Cuia de rescate, Balsa, rendición, Mapa, ficha de fruta ni Siguiente; los
        /// videos (Marea, Morpho) quedan fuera del prototipo del Reto [P].
        /// </summary>
        internal sealed class DailyGameActions : IGameActions, IShuffleActions
        {
            private readonly DailyChallengeScreen _owner;

            public DailyGameActions(DailyChallengeScreen owner)
            {
                _owner = owner;
            }

            private LevelFlow? Flow => _owner._flow;

            public TapResult TapCuia(int index)
            {
                LevelFlow? flow = Flow;
                return flow == null ? TapResult.Ignored(index, -1) : flow.TapCuia(index);
            }

            public void TapBackground()
            {
                Flow?.TapBackground();
            }

            public bool TapAnywhere()
            {
                LevelFlow? flow = Flow;
                return flow != null && flow.TapAnywhere();
            }

            public bool PressUndo()
            {
                LevelFlow? flow = Flow;
                if (flow == null)
                {
                    return false;
                }

                _owner._view?.SettleReveal();
                return flow.PressUndo();
            }

            public bool PressRestart()
            {
                LevelFlow? flow = Flow;
                if (flow == null)
                {
                    return false;
                }

                _owner._view?.SettleReveal();
                return flow.PressRestart();
            }

            public bool PressNext()
            {
                return _owner._won && _owner.GoHome();
            }

            public bool PressHome()
            {
                return _owner._won && _owner.GoHome();
            }

            public bool PressDoubleHarvest()
            {
                return _owner.PressDoubleHarvest();
            }

            public bool PressExtraCuia()
            {
                LevelFlow? flow = Flow;
                if (flow == null || !flow.AcceptsBoardInput)
                {
                    return false;
                }

                TransactionResult? result = flow.UseExtraCuia();
                return result != null && result.IsApplied;
            }

            public bool PressReveal()
            {
                return false; // el Reto no lleva dosis ocultas (03 §16): la Luciérnaga no aparece
            }

            public bool PressShuffle()
            {
                // La Pororoca también en el Reto (06 §4.1: los comodines de la campaña; bloque B2), con los mismos topes: el de
                // hoy va en daily.shuffles_today del mismo perfil.
                LevelFlow? flow = Flow;
                if (flow == null || !flow.HasLevel || !flow.Shuffle.CanUse)
                {
                    return false;
                }

                _owner._view?.SettleReveal();
                return flow.UseShuffle().HasValue;
            }

            public bool PressRevealVideo()
            {
                return false;
            }

            public bool PressHint()
            {
                LevelFlow? flow = Flow;
                if (flow == null || !flow.HasLevel)
                {
                    return false;
                }

                HintOffer offer = flow.Hint;
                if (offer.State == HintState.Unsupported)
                {
                    _owner._view?.ShowHintUnsupported();
                    return false;
                }

                if (offer.State != HintState.Ready || flow.IsResolving)
                {
                    return false;
                }

                if (offer.CanUse)
                {
                    return flow.PressHint();
                }

                _owner._view?.ShowHintMissing(offer.Missing);
                return false;
            }

            public bool PressHintVideo()
            {
                return false;
            }

            public bool PressUndoRefillVideo()
            {
                return false;
            }

            public bool PressUndoRefillSeeds()
            {
                TransactionResult? result = Flow?.RefillUndoWithSeeds();
                return result != null && result.IsApplied;
            }

            public bool PressUndoGift()
            {
                LevelFlow? flow = Flow;
                return flow != null && flow.GrantUndoGift();
            }

            public bool PressRescue()
            {
                return false; // sin Cuia de rescate en el Reto (06 §4.1)
            }

            public bool PressHandful()
            {
                return false;
            }

            public bool PressBalsa()
            {
                return false;
            }

            public bool PressGiveUp()
            {
                return false;
            }

            public bool ConfirmGiveUp()
            {
                return false;
            }

            public bool PressRetry()
            {
                return false;
            }

            public bool PressLevelFailedClose()
            {
                return _owner.Exit();
            }

            public bool PressExtraCanoeVideo()
            {
                return false;
            }

            public bool PressRefillCanoes()
            {
                return false;
            }

            public bool PressInfiniteCanoes()
            {
                return false;
            }

            public bool PressNoCanoesHome()
            {
                return false;
            }

            public bool PressNoCanoesMap()
            {
                return false;
            }

            public bool LogbookVictoryRowDue => false; // la victoria del Reto no lleva barras (Q-C2)

            public bool OpenLogbookFromVictory()
            {
                return false;
            }

            public bool PressNoCanoesClose()
            {
                return false;
            }

            public bool PressQuit()
            {
                LevelFlow? flow = Flow;
                return flow != null && flow.CanQuit && _owner.Exit();
            }

            public bool OpenRiverMap(MapOpenSource source)
            {
                return false;
            }

            public bool CloseRiverMap()
            {
                return false;
            }

            public bool MarkMapSeen(int level)
            {
                return false;
            }

            public bool OpenFruitSheet(string subject, FruitSheetSource source)
            {
                return false;
            }

            public bool CloseFruitSheet()
            {
                return false;
            }
        }
    }
}
