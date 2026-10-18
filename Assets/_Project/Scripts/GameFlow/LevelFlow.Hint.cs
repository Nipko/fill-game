#nullable enable
using System;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.Platform;

namespace AF.GameFlow
{
    /// <summary>
    /// Morpho · Pista en el flujo (tarea MO1; 04 §2.3; 03 §5; 08 §9 <c>hint_*</c> y <c>rv_hint</c>; D-019, Consecuencias): las
    /// reglas de uso, la consulta en el hilo de trabajo de <see cref="MorphoBooster"/>, el corte por <c>hint_timeout_ms</c> que
    /// marca este hilo con <see cref="Clock"/> en cada <see cref="Tick"/>, el orden de respuestas y el cobro. Sin interfaz: la
    /// ranura, la mariposa, los anillos y los avisos son de MO2 (<see cref="IHintListener"/>).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>Reglas de uso</b> (<see cref="Hint"/>): desde <c>hint_unlock_level</c> (12), si el nivel declara
    /// <c>hint_supported</c>, en PLAYING con algún vertido legal (en Aguas estancadas no está), sin otra consulta en curso y
    /// pasado el enfriamiento de <see cref="BoosterConfig.HintCooldownMs"/> [P]; se paga con el stock o con
    /// <c>hint_seed_price</c> semillas (el video <c>rv_hint</c> da una unidad al stock: <see cref="GrantHintVideoUnit"/>).</description></item>
    /// <item><description><b>Nunca cobra sin dar un vertido:</b> el cobro va al entregar una respuesta
    /// <see cref="HintAnswer.Pour"/> (y si no se puede cobrar, no se entrega); «Desde aquí no hay salida», «No veo un vertido
    /// seguro» y «No encuentro un camino» no cobran. Tampoco se cobra lo que se abandona.</description></item>
    /// <item><description><b>Recalcula:</b> la consulta se hace sobre el tablero del momento; si el tablero cambia antes de la
    /// respuesta (vertido, Marea, Volver a la orilla, Luciérnaga, Cuia extra o de rescate, otro nivel) se abandona sin cobrar, y
    /// la siguiente pista se calcula con lo nuevo, también con la cuia añadida (04 §2.3 c). La pista resaltada se apaga en
    /// cuanto el tablero cambia (04 §2.3 d: sin reembolso).</description></item>
    /// <item><description><b>Hilo principal:</b> <see cref="PressHint"/> y <see cref="Tick"/> nunca esperan al hilo de
    /// trabajo.</description></item>
    /// </list>
    /// </remarks>
    public sealed partial class LevelFlow
    {
        private IClock _clock = SystemClock.Instance;
        private MorphoBooster? _morpho;

        // Consulta en curso: la sesión y el tablero sobre los que se pidió.
        private bool _hintPending;
        private LevelSession? _hintSession;
        private BoardState? _hintBoard;
        private DateTime _hintStartedUtc;

        // Enfriamiento: hora a partir de la cual se puede volver a pedir.
        private DateTime _hintReadyUtc = DateTime.MinValue;

        // Pista resaltada: el vertido y el tablero en que vale.
        private bool _hintActive;
        private Pour _activeHint;
        private LevelSession? _activeHintSession;
        private BoardState? _activeHintBoard;

        private HintResult _lastHint;
        private bool _hasLastHint;
        private int _hintsUsed;

        /// <summary>
        /// Reloj del hilo principal para <c>hint_timeout_ms</c> y el enfriamiento (por defecto <see cref="SystemClock"/>; el juego
        /// pasa el mismo que la cartera y los tests un <see cref="ManualClock"/>).
        /// </summary>
        public IClock Clock
        {
            get => _clock;
            set
            {
                _clock = value ?? throw new ArgumentNullException(nameof(value));
                if (_morpho != null)
                {
                    _morpho.Clock = value;
                }
            }
        }

        /// <summary>
        /// Morpho de este flujo (se crea al primer uso con <see cref="Boosters"/> y <see cref="Clock"/>). Los tests pueden poner
        /// otro (p. ej. con un gancho que retiene el hilo de trabajo); al cambiarlo, la consulta en curso se abandona.
        /// </summary>
        public MorphoBooster Morpho
        {
            get => _morpho ??= new MorphoBooster(_boosters, _clock);
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                AbandonHint();
                _morpho = value;
            }
        }

        /// <summary>Sonda <c>AF_PERF morpho</c> (en el juego, <c>DevProbe</c>; null = sin sonda).</summary>
        public IHintProbe? HintProbe { get; set; }

        /// <summary>Estado del botón «Pista» (04 §2.3): stock, precio, «Te faltan», enfriamiento y si se puede usar.</summary>
        public HintOffer Hint => MorphoBooster.Evaluate(
            LevelNumber, _level?.HintSupported ?? false, HintHasLegalPour, _hintPending || (_morpho?.IsBusy ?? false), HintCooldownLeftMs(), _wallet, _boosters);

        /// <summary>Morpho está buscando (la vista puede enseñar que piensa; el tablero sigue aceptando toques).</summary>
        public bool HintThinking => _hintPending;

        /// <summary>Hay una pista resaltada (<see cref="ActiveHint"/>), válida mientras el tablero no cambie.</summary>
        public bool HasActiveHint => _hintActive;

        /// <summary>Vertido de la pista resaltada (solo con <see cref="HasActiveHint"/>).</summary>
        public Pour ActiveHint => _activeHint;

        /// <summary>Última respuesta entregada (null si aún no hay ninguna).</summary>
        public HintResult? LastHint => _hasLastHint ? _lastHint : (HintResult?)null;

        /// <summary>Pistas cobradas desde que se cargó el nivel (para la frase de TiTino «nunca tras Morpho»).</summary>
        public int HintsUsed => _hintsUsed;

        private bool HintHasLegalPour => AcceptsBoardInput && _session!.Status == LevelStatus.Playing;

        /// <summary>
        /// Botón «Pista» (04 §2.3): si <see cref="Hint"/> lo permite, quita la selección (C8) y lanza la consulta desde el tablero
        /// actual en el hilo de trabajo, sin cobrar todavía; la respuesta llega en un <see cref="Tick"/> posterior
        /// (<see cref="IHintListener.OnHint"/>). False si no se puede usar ahora (sin cartera, sin stock ni saldo: la vista ofrece
        /// el video o «Te faltan {n}»).
        /// </summary>
        public bool PressHint()
        {
            LevelSession? session = _session;
            if (_wallet == null || session == null || !Hint.CanUse)
            {
                return false;
            }

            ClearSelection();
            if (!Morpho.Start(session.Current, session.Initial, session.PourMode, session.FutileRuleEnabled, LevelNumber))
            {
                return false;
            }

            ClearActiveHint();
            _hintPending = true;
            _hintSession = session;
            _hintBoard = session.Current;
            _hintStartedUtc = _clock.UtcNow;
            (_listener as IHintListener)?.OnHintThinking(true);
            return true;
        }

        /// <summary>
        /// Recompensa del video <c>rv_hint</c> (04 §2.3: «1 pista»; la vista ya vio el anuncio entero): +1 Morpho al stock, que
        /// después se gasta con <see cref="PressHint"/>. False sin cartera.
        /// </summary>
        public bool GrantHintVideoUnit()
        {
            return _wallet != null && _wallet.Apply(Transaction.GrantStock(BoosterKind.Hint, 1)).IsApplied;
        }

        // Cada Tick: abandona la consulta si el tablero cambió, sondea el hilo (marca el corte al pasar hint_timeout_ms) y entrega
        // la respuesta; apaga la pista resaltada si el tablero cambió.
        private void TickHint()
        {
            if (!_hintPending && !_hintActive)
            {
                return;
            }

            if (_hintPending)
            {
                if (!SameHintBoard(_hintSession, _hintBoard))
                {
                    AbandonHint();
                }
                else
                {
                    switch (Morpho.Poll(out HintResult result))
                    {
                        case MorphoPoll.Done:
                            DeliverHint(result);
                            break;
                        case MorphoPoll.Idle:
                            AbandonHint();
                            break;
                    }
                }
            }

            if (_hintActive && !SameHintBoard(_activeHintSession, _activeHintBoard))
            {
                ClearActiveHint();
            }
        }

        // El nivel sigue en juego con la misma sesión y el mismo tablero (BoardState es inmutable: cada cambio crea otro).
        private bool SameHintBoard(LevelSession? session, BoardState? board)
        {
            return Phase == FlowPhase.Playing && _pending == Outcome.None && _session != null
                && ReferenceEquals(_session, session) && ReferenceEquals(_session.Current, board);
        }

        // Entrega la respuesta: el vertido se cobra al darlo (nunca sin darlo) y queda resaltado; las otras tres, gratis.
        private void DeliverHint(HintResult result)
        {
            _hintPending = false;
            int waitMs = (int)Math.Max(0, Math.Min(int.MaxValue, (_clock.UtcNow - _hintStartedUtc).TotalMilliseconds));
            if (result.Answer == HintAnswer.Pour)
            {
                Wallet? wallet = _wallet;
                LevelSession session = _session!;
                Pour move = result.Move;
                if (wallet == null || !PourRules.IsLegal(session.Current, move.Source, move.Target) || !MorphoBooster.CanPay(wallet))
                {
                    // No debería pasar (el tablero no cambió y se pudo pagar al pedirla): sin vertido válido o sin pago, ni se da
                    // ni se cobra.
                    (_listener as IHintListener)?.OnHintThinking(false);
                    return;
                }

                using (Saves())
                {
                    if (!MorphoBooster.Pay(wallet).IsApplied)
                    {
                        (_listener as IHintListener)?.OnHintThinking(false);
                        return;
                    }
                }

                result = result.AsCharged();
                _hintsUsed++;
                _hintActive = true;
                _activeHint = move;
                _activeHintSession = session;
                _activeHintBoard = session.Current;
            }

            _hintReadyUtc = _clock.UtcNow + TimeSpan.FromMilliseconds(BoosterConfig.HintCooldownMs);
            _lastHint = result;
            _hasLastHint = true;
            if (_listener is IHintListener hint)
            {
                hint.OnHintThinking(false);
                hint.OnHint(result);
            }

            HintProbe?.Morpho(result, LevelNumber, waitMs, _boosters.HintTimeoutMs);
        }

        // Abandona la consulta en curso, sin respuesta ni cobro.
        private void AbandonHint()
        {
            if (!_hintPending)
            {
                return;
            }

            _hintPending = false;
            _hintSession = null;
            _hintBoard = null;
            _morpho?.Abandon();
            (_listener as IHintListener)?.OnHintThinking(false);
        }

        private void ClearActiveHint()
        {
            if (!_hintActive)
            {
                return;
            }

            _hintActive = false;
            _activeHintSession = null;
            _activeHintBoard = null;
            (_listener as IHintListener)?.OnHintCleared();
        }

        // Regalo del 12 (04 §2.3; TU1b): al cargar el nivel de desbloqueo aún no superado, si no se regaló ya en él, se acreditan
        // hint_tutorial_units Morphos al stock y queda anotado en el intento (marca m de level_state, que Load guarda en
        // la misma escritura del perfil). Al reanudar no pasa por aquí (Resume no llama a Load) y el intento vuelve con la marca.
        private void GiftHintOnLoad(int number)
        {
            if (!MorphoBooster.ShouldGift(number, _progress.LoadMaxLevel(), _attempt.HintGifted, _wallet, _boosters)
                || !_wallet!.Apply(Transaction.GrantStock(BoosterKind.Hint, _boosters.HintTutorialUnits)).IsApplied)
            {
                return;
            }

            _attempt.HintGifted = true;
            (_listener as IHintListener)?.OnHintGifted(_boosters.HintTutorialUnits);
        }

        // Nivel nuevo o reabierto: sin consulta, sin pista resaltada y sin pistas usadas. El enfriamiento sigue (es por reloj).
        private void ResetHint()
        {
            AbandonHint();
            ClearActiveHint();
            _hintsUsed = 0;
        }

        private int HintCooldownLeftMs()
        {
            if (_hintReadyUtc == DateTime.MinValue)
            {
                return 0;
            }

            double left = (_hintReadyUtc - _clock.UtcNow).TotalMilliseconds;
            // Con el reloj del dispositivo hacia atrás, nunca más que el enfriamiento entero.
            return left <= 0 ? 0 : (int)Math.Ceiling(Math.Min(left, BoosterConfig.HintCooldownMs));
        }
    }
}
