#nullable enable
using System;
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
        private RescueCheck _rescueCheck;
        private RescueQuote? _quote;
        private FlowPhase _noCanoesReturn = FlowPhase.LevelFailed;

        /// <summary>Resultado de «la compra cumple» del bloqueo en curso (04 §6.4).</summary>
        public RescueCheck RescueCheck => _rescueCheck;

        /// <summary>Precio del rescate congelado al abrir el modal (04 §6.6), o null antes del 31 o sin cartera.</summary>
        public RescueQuote? FrozenRescueQuote => _quote;

        /// <summary>Desde este nivel hay canoas, rescate y el embudo de D-008 (<c>rescue_min_level</c>).</summary>
        public bool CanoesLevel => LevelNumber >= RescueMinLevel;

        /// <summary>Salir del nivel con el menú cuesta una canoa y es fallo duro (desde el 31; D-008, P16).</summary>
        public bool QuitCostsCanoe => CanoesLevel;

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
                Canoes = canoes.Count,
                InfiniteCanoes = canoes.IsInfinite,
                RescuesLevel = _attempt.Rescues,
                RescueMaxPerLevel = _boosters.RescueMaxPerLevel,
                RescueCheck = _session != null && _session.Current.Count >= BoardState.MaxCuias ? RescueCheck.NoRoom : _rescueCheck,
                RescuePrice = quote.Price,
                RescueBasePrice = quote.BasePrice,
                BalsaOk = balsaOk,
                Geo = geo,
                UndoGiftUsed = _attempt.UndoGiftUsed,
            };
        }

        /// <summary>
        /// Cuia extra (04 §2.2): con el tablero en juego y algún vertido útil, gasta una del stock o
        /// <c>extra_cuia_seed_price</c> semillas y añade una cuia vacía de capacidad 4 (fila más corta; persiste al reiniciar;
        /// Marea no la quita). Null si no se puede usar ahora; un resultado rechazado si faltan semillas (no cambia nada).
        /// </summary>
        public TransactionResult? UseExtraCuia()
        {
            ExtraCuiaOffer offer = ExtraCuia;
            if (_wallet == null || offer.State != ExtraCuiaState.Ready)
            {
                return null;
            }

            ClearSelection();
            TransactionResult paid = ExtraCuiaBooster.Pay(_wallet);
            if (!paid.IsApplied)
            {
                return paid;
            }

            int index = AddCuia(AddedCuiaKind.Extra);
            _listener.OnCuiaAdded(index, AddedCuiaKind.Extra);
            PersistState();
            return paid;
        }

        /// <summary>«+3 Deshacer · 30 semillas» (04 §2.1): +<c>undo_refill_amount</c> Mareas al stock. Null sin cartera o fuera de juego.</summary>
        public TransactionResult? RefillUndoWithSeeds()
        {
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
            return _wallet != null && _wallet.Apply(Transaction.GrantStock(BoosterKind.Undo, _wallet.Config.UndoRefillAmount)).IsApplied;
        }

        /// <summary>Rama F [P] (04 §6.3): sin cupo y sin rescate que vender, una recarga gratis de +1 Marea, una vez por nivel.</summary>
        public bool GrantUndoGift()
        {
            if (_wallet == null || Phase != FlowPhase.StuckModal || _attempt.UndoGiftUsed || _rescueCheck != RescueCheck.Unsolvable)
            {
                return false;
            }

            if (!_wallet.Apply(Transaction.GrantStock(BoosterKind.Undo, 1)).IsApplied)
            {
                return false;
            }

            _attempt.UndoGiftUsed = true;
            PersistState();
            return true;
        }

        /// <summary>
        /// «Continuar: +1 cuia» en Aguas estancadas o en ¿Seguro? (04 §6.2 <c>RESCUE</c>): cobra el precio congelado
        /// (<see cref="Transaction.Rescue"/>), añade la cuia, cierra el embudo y la partida sigue en PLAYING; conserva la
        /// Crecida y no cuesta canoa. Null si no se ofrece; un resultado rechazado si faltan semillas.
        /// </summary>
        public TransactionResult? BuyRescue()
        {
            if (_wallet == null || !RescueOffered)
            {
                return null;
            }

            TransactionResult paid = _wallet.Apply(Transaction.Rescue(LevelNumber, _quote!.Value.Price));
            if (paid.IsApplied)
            {
                ApplyRescueCuia();
            }

            return paid;
        }

        /// <summary>
        /// Balsa de rescate comprada (<c>af_rescue</c>; la tienda ya acreditó semillas, Cuia extra al stock y Canoas infinitas):
        /// aplica la Cuia de rescate sin coste en semillas (04 §6.2 <c>BALSA_499</c>).
        /// </summary>
        public bool ApplyPaidRescue()
        {
            if (!(Phase == FlowPhase.StuckModal || Phase == FlowPhase.GiveUpConfirm) || _session == null
                || _session.Current.Count >= BoardState.MaxCuias || _attempt.Rescues >= _boosters.RescueMaxPerLevel)
            {
                return false;
            }

            ApplyRescueCuia();
            return true;
        }

        /// <summary>
        /// «Rendirse» [P] desde el 31 (D-008, paso 1 → 2): abre ¿Seguro?; con Canoas infinitas no hay canoa en juego y pasa
        /// directo al fallo duro y a Nivel fallido.
        /// </summary>
        public bool PressGiveUp()
        {
            if (Phase != FlowPhase.StuckModal || !CanoesLevel)
            {
                return false;
            }

            if (_wallet != null && _wallet.Canoes.IsInfinite)
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
            if (Phase != FlowPhase.NoCanoes)
            {
                return false;
            }

            ExitNoCanoes();
            return true;
        }

        /// <summary>
        /// Pausa › «Salir» confirmado desde el 31 («¿Salir del nivel? Perderás 1 canoa» [P]; D-008): fallo duro, también con 0
        /// vertidos [P] (P16), y Nivel fallido, cuya X lleva al inicio. En 1-30 no es fallo: devuelve false y la salida la
        /// hace <see cref="AppFlow.ExitToLobby"/> sin coste.
        /// </summary>
        public bool ConfirmQuit()
        {
            if (!CanQuit || !QuitCostsCanoe)
            {
                return false;
            }

            ClearSelection();
            FailToLevelFailed();
            return true;
        }

        private void ApplyRescueCuia()
        {
            int index = AddCuia(AddedCuiaKind.Rescue);
            _taps.Clear();
            ResetPending();
            ResetStuck();
            Phase = FlowPhase.Playing;
            _listener.OnCuiaAdded(index, AddedCuiaKind.Rescue);
            EvaluateStatus();
            PersistState();
        }

        // Añade una cuia vacía de capacidad 4 al final del tablero (04 §2.2), que persiste al reiniciar y que Marea no quita
        // (02 §14). Hasta que el núcleo tenga LevelSession.AddEmptyCuia (tarea C1 de la pista A), la sesión se rehace con el
        // tablero inicial + la cuia y se vuelven a aplicar los vertidos del historial con las reglas del núcleo: la cuia nueva
        // va al final, así que los índices y las cantidades no cambian y el historial de Marea se conserva.
        private int AddCuia(AddedCuiaKind kind)
        {
            LevelSession old = _session ?? throw new InvalidOperationException("No hay ningún nivel cargado.");
            var session = new LevelSession(RescueBooster.WithEmptyCuia(old.Initial), old.FutileRuleEnabled, old.PourMode);
            for (int i = 0; i < old.AppliedPours; i++)
            {
                Pour pour = old.GetAppliedPour(i);
                if (!session.TryPour(pour.Source, pour.Target, out PourResult replayed) || replayed.Pour.Amount != pour.Amount)
                {
                    throw new InvalidOperationException("No se pudo rehacer el historial con la cuia añadida (vertido " + i + ").");
                }
            }

            _session = session;
            _attempt.AddCuia(kind);
            return session.Current.Count - 1;
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
            Phase = FlowPhase.StuckModal;
            _phaseMs = 0;
            _quote = _wallet?.QuoteRescue(LevelNumber); // el precio se congela al abrir el modal (04 §6.6)
            _listener.OnStuckModalOpened();
            PersistState(); // 02 §14: cerrar la app con el modal abierto lo reabre al volver
        }

        private void ResetStuck()
        {
            _rescueCheck = RescueCheck.NotChecked;
            _quote = null;
        }

        private void Fail(FailKind kind)
        {
            FailOutcome outcome = FailResolve.Resolve(_attempt, kind, LevelNumber, _wallet);
            _listener.OnFailResolved(outcome);
        }

        // Fallo duro desde el 31 (rendición confirmada o «Salir»): FAIL_RESOLVE y Nivel fallido (D-008, paso 3).
        private void FailToLevelFailed()
        {
            _taps.Clear();
            ResetPending();
            Fail(FailKind.Hard);
            Phase = FlowPhase.LevelFailed;
            _listener.OnFunnelPhase(FlowPhase.LevelFailed);
            PersistState(); // el fallo ya se cobró: reabrir Nivel fallido nunca lo vuelve a cobrar
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
