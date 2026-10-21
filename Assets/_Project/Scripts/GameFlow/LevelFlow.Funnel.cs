#nullable enable
using System.Diagnostics.CodeAnalysis;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;

namespace AF.GameFlow
{
    /// <summary>
    /// Comodines de la bandeja (B2-1: Marea con su recarga y Cuia extra) y embudo de bloqueo de D-008 (B2-2 y B2-3; 04 §6.2,
    /// §5.2 y §7; 02 §2): Aguas estancadas → ¿Seguro? → Nivel fallido → Sin canoas, con la Cuia de rescate, el fallo duro y
    /// blando, «Reintentar» con la variante aliviada y la salida desde la pausa. El modal lo arma
    /// <see cref="StuckModalModel"/> con <see cref="BuildStuckContext"/>; los anuncios y las compras los pide la vista (por
    /// <c>AF.Ads</c> y <c>AF.IAP</c>) y aquí solo se aplica su resultado.
    /// </summary>
    public sealed partial class LevelFlow
    {
        /// <summary>
        /// <see cref="LastFunnelError"/>: no se pudo rehacer el historial con la cuia añadida (Cuia extra, Cuia de rescate o
        /// Balsa), así que ni se añadió la cuia ni se cobró.
        /// </summary>
        public const string AddCuiaReplayError = "add_cuia_replay";

        private RescueCheck _rescueCheck;
        private RescueQuote? _quote;
        private FlowPhase _noCanoesReturn = FlowPhase.LevelFailed;
        private bool _balsaShown;

        /// <summary>
        /// Por qué la última acción del embudo o de la bandeja no se aplicó sin que sea cosa del jugador (null si no falló
        /// así). Hoy solo <see cref="AddCuiaReplayError"/>: la acción devuelve su «no aplicado» de siempre (null o false), no
        /// cobra nada ni cambia la sesión ni la fase (04 §2.5 d: nunca se cobra sin entregar). Se borra al empezar cada acción
        /// del embudo. <c>AF.GameFlow</c> no tiene motor: lo registra quien llama (<c>GameBootstrap</c>).
        /// </summary>
        public string? LastFunnelError { get; private set; }

        /// <summary>Resultado de «la compra cumple» del bloqueo en curso (04 §6.4).</summary>
        public RescueCheck RescueCheck => _rescueCheck;

        /// <summary>Precio del rescate congelado al abrir el modal (04 §6.6), o null antes del 31 o sin cartera.</summary>
        public RescueQuote? FrozenRescueQuote => _quote;

        /// <summary>
        /// La Balsa de rescate ya se enseñó en este bloqueo (<see cref="MarkBalsaShown"/>): la vista la sigue ofreciendo
        /// (<c>balsa_ok</c> congelado, 04 §6.6) sin contar otra impresión, también en el modal reabierto al volver a la app
        /// (02 §14; D-008: «se reabre tal cual»). Se olvida al salir del bloqueo (Marea, rescate, reinicio o nivel nuevo).
        /// </summary>
        public bool BalsaShownThisBlock => _balsaShown;

        /// <summary>Desde este nivel hay canoas, rescate y el embudo de D-008 (<c>rescue_min_level</c>).</summary>
        public bool CanoesLevel => LevelNumber >= RescueMinLevel;

        /// <summary>
        /// Las canoas cuentan en este nivel (<see cref="Wallet.CanoesActive"/>): desde el 31 y con <c>biz_feature_lives</c>
        /// encendido (08 §9). Es lo que enseña la barra de la cartera; sin cartera, false. El embudo de D-008 (fallo duro al
        /// salir, ¿Seguro?, Nivel fallido) y el rescate siguen <see cref="CanoesLevel"/>.
        /// </summary>
        public bool CanoesActive => _wallet != null && _wallet.CanoesActive(LevelNumber);

        /// <summary>
        /// Hay una canoa en juego: con las canoas activas (<see cref="CanoesActive"/>: desde el 31, con cartera y con
        /// <c>biz_feature_lives</c>), sin Canoas infinitas y con alguna canoa (0 si, por ejemplo, las infinitas caducaron a mitad
        /// de nivel). Es lo que cobra el fallo duro (<see cref="Transaction.HardFail"/>); sin canoa en juego no se anuncia ninguna
        /// pérdida y «Rendirse» pasa directo a Nivel fallido (D-008, paso 2 [P]).
        /// </summary>
        public bool CanoeAtStake
        {
            get
            {
                if (_wallet == null || !CanoesActive)
                {
                    return false;
                }

                CanoesStatus canoes = _wallet.Canoes;
                return !canoes.IsInfinite && canoes.Count > 0;
            }
        }

        /// <summary>Salir del nivel con el menú es fallo duro y lleva a Nivel fallido (desde el 31; D-008, P16).</summary>
        public bool QuitIsHardFail => CanoesLevel;

        /// <summary>Salir del nivel con el menú cuesta una canoa: fallo duro con una canoa en juego (<see cref="CanoeAtStake"/>).</summary>
        public bool QuitCostsCanoe => QuitIsHardFail && CanoeAtStake;

        /// <summary>
        /// Se puede salir con el menú de pausa: PLAYING (sin victoria pendiente), INTRO o STUCK_MODAL (02 §2: «PLAYING o
        /// STUCK_MODAL ── pausa › Salir»).
        /// </summary>
        public bool CanQuit => _session != null
            && ((Phase == FlowPhase.Playing && _pending != Outcome.Win) || Phase == FlowPhase.Intro || Phase == FlowPhase.StuckModal);

        /// <summary>Volver a la orilla desde la cabecera pide confirmación (≥ <c>restart_confirm_min_pours</c> vertidos; 04 §2.7).</summary>
        public bool RestartNeedsConfirm => AcceptsBoardInput && _session!.AppliedPours >= _boosters.RestartConfirmMinPours;

        /// <summary>El rescate se puede comprar ahora (modal o ¿Seguro?, con la compra verificada y sin el tope del nivel).</summary>
        public bool RescueOffered => (Phase == FlowPhase.StuckModal || Phase == FlowPhase.GiveUpConfirm) && RescueAvailable;

        /// <summary>Botón «+1 cuia» de la bandeja (04 §2.2).</summary>
        public ExtraCuiaOffer ExtraCuia => _session == null
            ? new ExtraCuiaOffer(ExtraCuiaState.Hidden, 0, 0, 0)
            : ExtraCuiaBooster.Evaluate(LevelNumber, _attempt.ExtraCuias, AcceptsBoardInput && _session.Status == LevelStatus.Playing, _session.Current.Count, _wallet, _boosters);

        private int RescueMinLevel => _wallet?.Config.RescueMinLevel ?? EconomyConfig.Defaults.RescueMinLevel;

        private bool UndoAllowedHere => AcceptsBoardInput || Phase == FlowPhase.StuckModal;

        private bool RescueAvailable => CanoesLevel && _quote.HasValue && _session != null && _session.Current.Count < BoardState.MaxCuias
            && _attempt.Rescues < _boosters.RescueMaxPerLevel && RescueBooster.IsOffered(_rescueCheck);

        /// <summary>
        /// Contexto del modal (04 §6.1) con lo que sabe la vista: si hay video de <c>rv_undo_refill</c>, si la Balsa se puede
        /// ofrecer hoy (<c>balsa_ok</c>) y la geo.
        /// </summary>
        public StuckContext BuildStuckContext(bool undoAdReady, bool balsaOk, GeoTier geo)
        {
            UndoStatus undo = UndoStatus;
            CanoesStatus canoes = _wallet?.Canoes ?? default;
            RescueQuote quote = _quote ?? default;
            StreakSnapshot? streak = PeekStreak(); // la línea de la Hoja escudo (04 §6.5; B1b): lee el almacén, solo al armar el modal
            return new StuckContext
            {
                Level = LevelNumber,
                RescueMinLevel = RescueMinLevel,
                UndoUnlimited = undo.Unlimited,
                UndoFree = undo.FreeLeft,
                UndoStock = undo.Stock,
                HasHistory = _session != null && _session.CanUndo,
                UndoAdReady = undoAdReady,
                Seeds = _wallet?.Seeds ?? 0,
                UndoRefillPrice = _wallet?.Price(SeedSink.UndoRefill) ?? EconomyConfig.Defaults.UndoRefillSeedPrice,
                UndoRefillAmount = _wallet?.Config.UndoRefillAmount ?? EconomyConfig.Defaults.UndoRefillAmount,
                Canoes = CanoesActive ? canoes.Count : 0, // con biz_feature_lives apagado no hay canoa en juego: sin línea de canoa
                InfiniteCanoes = canoes.IsInfinite,
                RescuesLevel = _attempt.Rescues,
                RescueMaxPerLevel = _boosters.RescueMaxPerLevel,
                RescueCheck = _session != null && _session.Current.Count >= BoardState.MaxCuias ? RescueCheck.NoRoom : _rescueCheck,
                RescuePrice = quote.Price,
                RescueBasePrice = quote.BasePrice,
                BalsaOk = balsaOk,
                Geo = geo,
                UndoGiftUsed = _attempt.UndoGiftUsed,
                StreakActive = streak.HasValue && streak.Value.Active,
                StreakWins = streak.HasValue ? streak.Value.Wins : 0,
                ShieldAvailable = streak.HasValue && streak.Value.ShieldAvailable,
                Skip = SkipOffer, // rama D (LevelFlow.Skip.cs; bloque B3)
            };
        }

        /// <summary>
        /// La vista enseñó la Balsa de rescate en Aguas estancadas (la impresión ya contó para <c>af_rescue_daily_cap</c>):
        /// queda anotado en <c>level_state</c> (marca <c>b</c> de <c>history</c>) para reabrir el modal tal cual. False fuera de
        /// Aguas estancadas o ¿Seguro?, o si ya estaba anotado. La vista lo llama en el mismo lote que la impresión.
        /// </summary>
        public bool MarkBalsaShown()
        {
            LastFunnelError = null;
            if (_balsaShown || !(Phase == FlowPhase.StuckModal || Phase == FlowPhase.GiveUpConfirm))
            {
                return false;
            }

            using (Saves())
            {
                _balsaShown = true;
                PersistState();
            }

            return true;
        }

        /// <summary>
        /// Cuia extra (04 §2.2): con el tablero en juego y algún vertido útil, gasta una del stock o
        /// <c>extra_cuia_seed_price</c> semillas y añade una cuia vacía de capacidad 4 (fila más corta; persiste al reiniciar;
        /// Marea no la quita; conserva las capas ya reveladas, 02 §11 T6). Null si no se puede usar ahora, también si el
        /// historial no se puede rehacer con la cuia (<see cref="LastFunnelError"/>; no se cobra nada); un resultado rechazado
        /// si faltan semillas (no cambia nada).
        /// </summary>
        public TransactionResult? UseExtraCuia()
        {
            LastFunnelError = null;
            ExtraCuiaOffer offer = ExtraCuia;
            if (_wallet == null || offer.State != ExtraCuiaState.Ready)
            {
                return null;
            }

            // Primero la sesión con la cuia, sin tocar la actual: si no se puede rehacer, ni se cobra ni se añade (04 §2.5 d).
            if (!TryRebuildWithAddedCuia(out LevelSession? rebuilt))
            {
                LastFunnelError = AddCuiaReplayError;
                return null;
            }

            ClearSelection();
            using (Saves())
            {
                TransactionResult paid = ExtraCuiaBooster.Pay(_wallet);
                if (!paid.IsApplied)
                {
                    return paid; // la sesión rehecha no se instaló: sigue la de antes
                }

                int index = InstallAddedCuia(rebuilt, AddedCuiaKind.Extra);
                _listener.OnCuiaAdded(index, AddedCuiaKind.Extra);
                PersistState(); // el cobro y la cuia añadida, en la misma escritura del perfil
                return paid;
            }
        }

        /// <summary>«+3 Deshacer · 30 semillas» (04 §2.1): +<c>undo_refill_amount</c> Mareas al stock. Null sin cartera o fuera de juego.</summary>
        public TransactionResult? RefillUndoWithSeeds()
        {
            LastFunnelError = null;
            if (_wallet == null || _session == null || !UndoAllowedHere)
            {
                return null;
            }

            return _wallet.Apply(Transaction.Buy(SeedSink.UndoRefill));
        }

        /// <summary>
        /// Recompensa de «+3 Deshacer · Ver video» (<c>rv_undo_refill</c>; la vista ya vio el anuncio entero con
        /// <c>AdController.ShowRewarded</c>): +<c>undo_refill_amount</c> Mareas al stock.
        /// </summary>
        public bool GrantUndoRefill()
        {
            LastFunnelError = null;
            return _wallet != null && _wallet.Apply(Transaction.GrantStock(BoosterKind.Undo, _wallet.Config.UndoRefillAmount)).IsApplied;
        }

        /// <summary>Rama F [P] (04 §6.3): sin cupo y sin rescate que vender, una recarga gratis de +1 Marea, una vez por nivel.</summary>
        public bool GrantUndoGift()
        {
            LastFunnelError = null;
            if (_wallet == null || Phase != FlowPhase.StuckModal || _attempt.UndoGiftUsed || _rescueCheck != RescueCheck.Unsolvable)
            {
                return false;
            }

            using (Saves())
            {
                if (!_wallet.Apply(Transaction.GrantStock(BoosterKind.Undo, 1)).IsApplied)
                {
                    return false;
                }

                _attempt.UndoGiftUsed = true;
                PersistState();
                return true;
            }
        }

        /// <summary>
        /// «Continuar: +1 cuia» en Aguas estancadas o en ¿Seguro? (04 §6.2 <c>RESCUE</c>): cobra el precio congelado
        /// (<see cref="Transaction.Rescue"/>), añade la cuia, cierra el embudo y la partida sigue en PLAYING; conserva la
        /// Crecida, las capas ya reveladas (02 §11 T6) y no cuesta canoa. El cobro y el <c>level_state</c> con la cuia van en la
        /// misma escritura del perfil: un cierre entre los dos no reabre el modal ya cobrado. Null si no se ofrece, también si
        /// el historial no se puede rehacer con la cuia (<see cref="LastFunnelError"/>; no se cobra nada); un resultado
        /// rechazado si faltan semillas.
        /// </summary>
        public TransactionResult? BuyRescue()
        {
            LastFunnelError = null;
            if (_wallet == null || !RescueOffered)
            {
                return null;
            }

            // Primero la sesión con la cuia, sin tocar la actual: si no se puede rehacer, ni se cobra ni se añade (04 §2.5 d).
            if (!TryRebuildWithAddedCuia(out LevelSession? rebuilt))
            {
                LastFunnelError = AddCuiaReplayError;
                return null;
            }

            using (Saves())
            {
                TransactionResult paid = _wallet.Apply(Transaction.Rescue(LevelNumber, _quote!.Value.Price));
                if (paid.IsApplied)
                {
                    ApplyRescueCuia(rebuilt);
                }

                return paid; // rechazado: la sesión rehecha no se instaló y sigue la de antes
            }
        }

        /// <summary>
        /// Balsa de rescate comprada (<c>af_rescue</c>; la tienda ya acreditó semillas, Cuia extra al stock y Canoas infinitas):
        /// aplica la Cuia de rescate sin coste en semillas (04 §6.2 <c>BALSA_499</c>). False si no se puede, también si el
        /// historial no se puede rehacer con la cuia (<see cref="LastFunnelError"/>).
        /// </summary>
        public bool ApplyPaidRescue()
        {
            LastFunnelError = null;
            if (!(Phase == FlowPhase.StuckModal || Phase == FlowPhase.GiveUpConfirm) || _session == null
                || _session.Current.Count >= BoardState.MaxCuias || _attempt.Rescues >= _boosters.RescueMaxPerLevel)
            {
                return false;
            }

            if (!TryRebuildWithAddedCuia(out LevelSession? rebuilt))
            {
                LastFunnelError = AddCuiaReplayError;
                return false;
            }

            ApplyRescueCuia(rebuilt);
            return true;
        }

        /// <summary>
        /// «Rendirse» [P] desde el 31 (D-008, paso 1 → 2): abre ¿Seguro?; sin canoa en juego (Canoas infinitas, o 0 canoas si
        /// las infinitas caducaron a mitad de nivel, o <c>biz_feature_lives</c> apagado: <see cref="CanoeAtStake"/>) no hay nada
        /// que confirmar y pasa directo al fallo duro y a Nivel fallido.
        /// </summary>
        public bool PressGiveUp()
        {
            LastFunnelError = null;
            if (Phase != FlowPhase.StuckModal || !CanoesLevel)
            {
                return false;
            }

            if (!CanoeAtStake)
            {
                FailToLevelFailed();
                return true;
            }

            Phase = FlowPhase.GiveUpConfirm;
            _listener.OnFunnelPhase(FlowPhase.GiveUpConfirm);
            PersistState();
            return true;
        }

        /// <summary>X o «Rendirse» en ¿Seguro? (D-008, paso 2 → 3): fallo duro (−1 canoa) y Nivel fallido.</summary>
        public bool ConfirmGiveUp()
        {
            LastFunnelError = null;
            if (Phase != FlowPhase.GiveUpConfirm)
            {
                return false;
            }

            FailToLevelFailed();
            return true;
        }

        /// <summary>
        /// «Reintentar» en Nivel fallido (D-008, paso 3): con canoas o Canoas infinitas, el mismo tablero (misma semilla, o la
        /// variante aliviada al tercer fallo) con la intro corta; con 0 canoas, Sin canoas (paso 4).
        /// </summary>
        public bool PressRetry()
        {
            LastFunnelError = null;
            if (Phase != FlowPhase.LevelFailed || _session == null)
            {
                return false;
            }

            if (CanoesBlockStart())
            {
                EnterNoCanoes(FlowPhase.LevelFailed);
                return true;
            }

            _listener.OnRestart(true);
            BeginAttempt();
            return true;
        }

        /// <summary>
        /// X de Sin canoas: vuelve a donde se abrió (Nivel fallido, o el tablero si se abrió con Volver a la orilla desde la
        /// cabecera). «Volver al inicio» lo lleva <see cref="AppFlow.ExitToLobby"/>.
        /// </summary>
        public bool CloseNoCanoes()
        {
            LastFunnelError = null;
            if (Phase != FlowPhase.NoCanoes)
            {
                return false;
            }

            ExitNoCanoes();
            return true;
        }

        /// <summary>
        /// Pausa › «Salir» confirmado desde el 31 («¿Salir del nivel? Perderás 1 canoa», aprobado: P14 de D-008, D-029; sin la línea de canoa si no
        /// hay canoa en juego, <see cref="CanoeAtStake"/>): fallo duro, también con 0 vertidos (P16 de D-008, aprobada: D-029), y Nivel fallido, cuya
        /// X lleva al inicio. En 1-30 no es fallo: devuelve false y la salida la hace <see cref="AppFlow.ExitToLobby"/> sin coste.
        /// </summary>
        public bool ConfirmQuit()
        {
            LastFunnelError = null;
            if (!CanQuit || !QuitIsHardFail)
            {
                return false;
            }

            ClearSelection();
            FailToLevelFailed();
            return true;
        }

        // Instala la sesión rehecha con la Cuia de rescate (ya cobrada o de la Balsa), cierra el embudo y sigue en PLAYING.
        private void ApplyRescueCuia(LevelSession rebuilt)
        {
            using (Saves())
            {
                int index = InstallAddedCuia(rebuilt, AddedCuiaKind.Rescue);
                _taps.Clear();
                ResetPending();
                ResetStuck();
                Phase = FlowPhase.Playing;
                _listener.OnCuiaAdded(index, AddedCuiaKind.Rescue);
                EvaluateStatus();
                PersistState();
            }
        }

        // Primer paso de añadir una cuia vacía de capacidad 4 al final del tablero (04 §2.2), que persiste al reiniciar y que
        // Marea no quita (02 §14). Hasta que el núcleo tenga LevelSession.AddEmptyCuia (tarea C1 de la pista A), la sesión se
        // rehace con el tablero inicial + la cuia y se vuelven a aplicar los vertidos del historial con las reglas del núcleo:
        // la cuia nueva va al final, así que los índices y las cantidades no cambian y el historial de Marea se conserva.
        // Después se restauran las capas reveladas de cada cuia antigua (02 §11 T6: el conocimiento no se revierte; ver
        // TryRestoreRevealed). Construye la sesión sin tocar la actual ni el intento y no lanza: false si algún vertido no se
        // puede rehacer igual (otra cantidad) o alguna máscara no se puede restaurar; entonces quien llama no cobra ni añade
        // nada (04 §2.5 d). Quien llama cobra después, si hace falta, e instala con InstallAddedCuia solo si el cobro se aplicó:
        // así un cobro rechazado deja la sesión anterior tal cual, sin nada que deshacer.
        private bool TryRebuildWithAddedCuia([NotNullWhen(true)] out LevelSession? rebuilt)
        {
            rebuilt = null;
            LevelSession? old = _session;
            if (old == null || old.Initial.Count >= BoardState.MaxCuias)
            {
                return false;
            }

            var session = new LevelSession(RescueBooster.WithEmptyCuia(old.Initial), old.FutileRuleEnabled, old.PourMode);
            for (int i = 0; i < old.AppliedPours; i++)
            {
                Pour pour = old.GetAppliedPour(i);
                if (!session.TryPour(pour.Source, pour.Target, out PourResult replayed) || replayed.Pour.Amount != pour.Amount)
                {
                    return false;
                }
            }

            for (int c = 0; c < old.Initial.Count; c++)
            {
                int layers = old.RevealedLayers(c);
                if (layers != 0 && !TryRestoreRevealed(session, c, layers))
                {
                    return false;
                }
            }

            rebuilt = session;
            return true;
        }

        // Segundo paso: instala la sesión rehecha y anota la cuia en el intento. Devuelve el índice de la cuia nueva.
        private int InstallAddedCuia(LevelSession rebuilt, AddedCuiaKind kind)
        {
            _session = rebuilt;
            _attempt.AddCuia(kind);
            return rebuilt.Current.Count - 1;
        }

        private void CheckRescue()
        {
            _rescueCheck = RescueCheck.NotChecked;
            if (_wallet == null || !CanoesLevel || _session == null || _attempt.Rescues >= _boosters.RescueMaxPerLevel)
            {
                return;
            }

            _rescueCheck = _rescue.Check(_session.Current, _session.PourMode, _session.FutileRuleEnabled);
        }

        private void OpenStuckModal()
        {
            using (Saves()) // la impresión de la Balsa que anota la vista y el level_state, en una escritura
            {
                Phase = FlowPhase.StuckModal;
                _phaseMs = 0;
                _quote = _wallet?.QuoteRescue(LevelNumber); // el precio se congela al abrir el modal (04 §6.6)
                _listener.OnStuckModalOpened();
                PersistState(); // 02 §14: cerrar la app con el modal abierto lo reabre al volver
            }
        }

        private void ResetStuck()
        {
            _rescueCheck = RescueCheck.NotChecked;
            _quote = null;
            _balsaShown = false;
        }

        private void Fail(FailKind kind)
        {
            FailOutcome outcome = FailResolve.Resolve(_attempt, kind, LevelNumber, _wallet);
            OnHardFailStreak(kind); // Crecida y Hoja escudo, antes de avisar a la vista (E29, E31; LevelFlow.Streak.cs)
            OnFailCalendar(kind); // Bitácora: el fallo duro también termina el nivel del día (B4; LevelFlow.Calendar.cs)
            PublishLevelFailed(outcome); // af_level_fail y, si es duro, level_end por el bus (B7; LevelFlow.Events.cs)
            _listener.OnFailResolved(outcome);
        }

        // Fallo duro desde el 31 (rendición confirmada o «Salir»): FAIL_RESOLVE y Nivel fallido (D-008, paso 3). La canoa y el
        // level_state en LEVEL_FAILED van en la misma escritura del perfil: un cierre entre los dos no reabre ¿Seguro? o el
        // modal con la canoa ya cobrada (y reabrir Nivel fallido nunca lo vuelve a cobrar).
        private void FailToLevelFailed()
        {
            using (Saves())
            {
                _taps.Clear();
                ResetPending();
                Fail(FailKind.Hard);
                Phase = FlowPhase.LevelFailed;
                _listener.OnFunnelPhase(FlowPhase.LevelFailed);
                PersistState();
            }
        }

        // 04 §7.1 «Qué bloquea»: desde el 31, sin canoas ni Canoas infinitas no se empieza ni se reinicia un nivel.
        private bool CanoesBlockStart()
        {
            return _wallet != null && CanoesLevel && !_wallet.CanStartLevel(LevelNumber);
        }

        private void EnterNoCanoes(FlowPhase returnTo)
        {
            _noCanoesReturn = returnTo;
            Phase = FlowPhase.NoCanoes;
            _listener.OnFunnelPhase(FlowPhase.NoCanoes);
            PersistState();
        }

        private void ExitNoCanoes()
        {
            Phase = _noCanoesReturn;
            _listener.OnFunnelPhase(Phase);
            PersistState();
        }
    }
}
