#nullable enable
using System;
using System.IO;
using AF.Events;
using AF.Platform;
using AF.Save;

namespace AF.Economy
{
    /// <summary>Cotización de la Cuia de rescate en un nivel (04 §6.2): lo que muestra el botón «Continuar: +1 cuia».</summary>
    public readonly struct RescueQuote
    {
        /// <summary>Crea la cotización.</summary>
        public RescueQuote(int price, int basePrice)
        {
            Price = price;
            BasePrice = basePrice;
        }

        /// <summary>Precio a pagar (300 si es el primero del día local; si no, 600).</summary>
        public int Price { get; }

        /// <summary>Precio del tramo, sin descuento (<c>rescue_price_bands</c>).</summary>
        public int BasePrice { get; }

        /// <summary>Lleva el descuento del primer rescate del día (<c>af_stuck_shown.discount</c>, 04 §11).</summary>
        public bool IsDiscounted => Price < BasePrice;
    }

    /// <summary>
    /// Cartera local de semillas, canoas y stock de comodines (GDD 08 §2.2 <c>AF.Economy</c>; 04 §1.1, §6.2 y §7; 05 §2).
    /// Toda mutación pasa por <see cref="Apply"/>: valida contra el estado de este instante, y si la rechaza no toca nada;
    /// si la aplica, escribe la regeneración de canoas y el cambio de día pendientes, aplica el cambio, marca
    /// <c>economy_ts</c> (08 §4.3) y guarda el perfil con el <see cref="IProfileStore"/> (tarea B1-2). La hora sale del
    /// <see cref="IClock"/>; los saldos que se leen ya llevan la regeneración aplicada y no cambian el perfil.
    /// <para>
    /// Las Canoas infinitas evitan gastar canoas, no los anuncios (D-008). El saldo inicial es 0: el GDD no fija otro.
    /// Se usa desde un solo hilo (el principal).
    /// </para>
    /// </summary>
    public sealed class Wallet
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;
        private readonly IClock _clock;

        /// <summary>Crea la cartera sobre <paramref name="profile"/>, que guarda en <paramref name="store"/>.</summary>
        /// <exception cref="InvalidOperationException">La configuración no es válida (<see cref="EconomyConfig.Validate"/>).</exception>
        public Wallet(Profile profile, IProfileStore store, IClock clock, EconomyConfig? config = null)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Config = config ?? EconomyConfig.Defaults;
            Config.Validate();
            Stock = new BoosterStock(_profile.Stock);
        }

        /// <summary>Se aplicó una transacción que cambió el perfil (después de guardarlo). Para la UI.</summary>
        public event Action<TransactionResult>? Changed;

        /// <summary>
        /// Bus de eventos (08 §2.1, principio 3; D-033, Q-T1 (a); bloque B7): cada transacción aplicada que mueve semillas publica
        /// <see cref="SeedsEarned"/> (<c>earn_virtual_currency</c>, con la fuente de la transacción) o <see cref="SeedsSpent"/>
        /// (<c>spend_virtual_currency</c>, con el sumidero), después de <see cref="Changed"/>. Null = sin bus.
        /// </summary>
        public EventBus? Events { get; set; }

        /// <summary>
        /// <c>biz_feature_lives</c> (08 §9; bloque B7): con false, las canoas no cuentan en ningún nivel, como antes del 31 (ni se
        /// gastan ni bloquean el inicio), y desde el 31 el embudo de D-008 funciona como en 1-30 (D-038; lo decide
        /// <c>LevelFlow.CanoeFunnel</c>). True por defecto.
        /// </summary>
        public bool LivesEnabled { get; set; } = true;

        /// <summary>Cifras con las que trabaja la cartera.</summary>
        public EconomyConfig Config { get; }

        /// <summary>Saldo de semillas.</summary>
        public int Seeds => _profile.Wallet.Seeds;

        /// <summary>Stock de comodines.</summary>
        public BoosterStock Stock { get; }

        /// <summary>Canoas en este instante, con la regeneración aplicada (sin ella con el anti-reloj activo: <see cref="CanoesFrozen"/>).</summary>
        public CanoesStatus Canoes => CanoeRules.Peek(_profile.Wallet.Canoes, _clock.UtcNow, Config.CanoesMax, Config.CanoesRegen, CanoesFrozen);

        /// <summary>
        /// Anti-reloj (04 §7.1, «Reloj»; 08 §10.1; bloque B8): el reloj de la cartera es un <see cref="IClockTrust"/>
        /// (<c>AF.Platform.GuardedClock</c>) con el desfase del servidor por encima de <c>sec_clock_max_drift_h</c>, así que las canoas
        /// no regeneran hasta reconectar con un reloj sano. Con cualquier otro reloj, false.
        /// </summary>
        public bool CanoesFrozen => _clock is IClockTrust trust && trust.DriftExceeded;

        /// <summary>Cuias de rescate compradas hoy (día local; 0 si los contadores son de un día anterior).</summary>
        public int RescuesToday => DailyCounters.IsStale(_profile.Daily, _clock.LocalDateKey()) ? 0 : _profile.Daily.RescuesToday;

        /// <summary>
        /// Pororocas usadas hoy (<c>daily.shuffles_today</c>, día local; 0 si los contadores son de un día anterior): el tope de
        /// <c>shuffle_max_per_day</c>, que vale también para el stock (04 §2.4). Lo sube <see cref="Apply"/> al cobrar una, con
        /// semillas (<see cref="SeedSink.Shuffle"/>) o del stock (bloque B2).
        /// </summary>
        public int ShufflesToday => DailyCounters.IsStale(_profile.Daily, _clock.LocalDateKey()) ? 0 : _profile.Daily.ShufflesToday;

        /// <summary>
        /// Un cambio quedó sin guardar porque falló la escritura; se reintenta en la próxima mutación o con <see cref="Flush"/>.
        /// Solo ve los fallos del <c>IProfileStore</c> que recibe: con un almacén que agrupa escrituras
        /// (<c>AF.GameFlow.ProfileSaveBatch</c>, dentro de un lote) el guardado solo se anota y no falla aquí; si la escritura
        /// del cierre del lote falla, esto sigue en false y el pendiente lo lleva <c>ProfileSaveBatch.PendingSave</c>. En el
        /// juego la cartera guarda por <c>ProfileSaveBatch.Durable</c> (T-D): cada mutación espera al escritor de segundo plano y
        /// escribe en síncrono al cerrar su acción.
        /// </summary>
        public bool PendingSave { get; private set; }

        /// <summary>Perfil de primera ejecución con <c>canoes_max</c> canoas (04 §7.1: debut con 5/5) y 0 semillas.</summary>
        public static Profile NewProfile(IClock clock, EconomyConfig? config = null)
        {
            return Profile.CreateNew(clock, (config ?? EconomyConfig.Defaults).CanoesMax);
        }

        /// <summary>Las canoas cuentan en este nivel (desde <c>rescue_min_level</c>; antes no se muestran ni se gastan).</summary>
        public bool CanoesActive(int level)
        {
            return LivesEnabled && level >= Config.RescueMinLevel;
        }

        /// <summary>
        /// Se puede empezar o reiniciar el nivel (04 §7.1 "Qué bloquea"): antes del 31, siempre; desde el 31, con alguna canoa
        /// o con Canoas infinitas. Si no, se abre Sin canoas.
        /// </summary>
        public bool CanStartLevel(int level)
        {
            if (!CanoesActive(level))
            {
                return true;
            }

            CanoesStatus canoes = Canoes;
            return canoes.IsInfinite || canoes.Count > 0;
        }

        /// <summary>Semillas que faltan para pagar <paramref name="price"/> (0 si alcanza).</summary>
        public int Missing(int price)
        {
            return Math.Max(0, price - Seeds);
        }

        /// <summary>Precio fijo de un sumidero (08 §9).</summary>
        public int Price(SeedSink sink)
        {
            return Config.SeedPrice(sink);
        }

        /// <summary>
        /// Cotiza la Cuia de rescate en <paramref name="level"/> (04 §6.2): el precio del tramo (600), o
        /// <c>round(600 × rescue_first_day_mult)</c> = 300 si hoy no se ha comprado ninguna. Null antes del 31. Que se
        /// ofrezca (1 por nivel, «la compra cumple») lo decide <c>AF.Boosters</c>.
        /// </summary>
        public RescueQuote? QuoteRescue(int level)
        {
            int? basePrice = Config.RescueBasePrice(level);
            if (!basePrice.HasValue)
            {
                return null;
            }

            int price = RescuesToday == 0 ? Config.RescueFirstOfDayPrice(basePrice.Value) : basePrice.Value;
            return new RescueQuote(price, basePrice.Value);
        }

        /// <summary>Aplica <paramref name="transaction"/> o la rechaza sin cambiar nada.</summary>
        public TransactionResult Apply(Transaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            DateTime now = _clock.UtcNow;
            string today = _clock.LocalDateKey();
            bool frozen = CanoesFrozen;
            CanoesStatus canoes = CanoeRules.Peek(_profile.Wallet.Canoes, now, Config.CanoesMax, Config.CanoesRegen, frozen);
            string? token = transaction.PurchaseToken;
            bool recordsToken = false;

            int seedsDelta = 0;
            StockDelta stock = transaction.Stock;
            int canoeDelta = 0;
            bool fill = false;
            bool rescue = false;
            bool shuffle = (transaction.Kind == TransactionKind.Buy && transaction.Sink == SeedSink.Shuffle)
                || (transaction.Kind == TransactionKind.UseStock && transaction.Booster == BoosterKind.Shuffle); // B2: shuffles_today
            switch (transaction.Kind)
            {
                case TransactionKind.Win:
                    seedsDelta = Config.WinReward(transaction.Tier);
                    break;
                case TransactionKind.DoubleHarvest:
                    // La del río crecido no es la ubicación de vídeo: sin su compuerta (Q-C14 (a), E35).
                    if (!transaction.RiverFull && transaction.Level < Config.AdsRvDoubleHarvestFromLevel)
                    {
                        return Reject(transaction, RejectReason.NotUnlocked);
                    }

                    // La de campaña dobla la Cosecha de la etiqueta; la del Reto (E9) trae las semillas del premio del día.
                    seedsDelta = transaction.Seeds > 0 ? transaction.Seeds : Config.WinReward(transaction.Tier);
                    break;
                case TransactionKind.Earn:
                case TransactionKind.Purchase:
                    if (token != null)
                    {
                        // 08 §10.3: un token se acredita una sola vez (applied_purchases), también tras restoreLedger (B8).
                        if (_profile.AppliedPurchases.Contains(token))
                        {
                            return Reject(transaction, RejectReason.AlreadyApplied);
                        }

                        recordsToken = true;
                    }

                    seedsDelta = transaction.Seeds;
                    rescue = transaction.AppliesRescue;
                    break;
                case TransactionKind.Revoke:
                {
                    // B8: lo concedido sale sin bajar de 0 (08 §10.1 y §10.2).
                    seedsDelta = -Math.Min(transaction.Seeds, _profile.Wallet.Seeds);
                    StockDelta take = StockDelta.None;
                    for (int k = 0; k < BoosterKinds.Count; k++)
                    {
                        var kind = (BoosterKind)k;
                        int units = Math.Min(transaction.Stock.Get(kind), BoosterKinds.Read(_profile.Stock, kind));
                        if (units > 0)
                        {
                            take = take.Plus(kind, -units);
                        }
                    }

                    // Un token ya retirado no se retira dos veces (revisión de B8).
                    if (token != null && _profile.VoidedPurchases.Contains(token))
                    {
                        return Reject(transaction, RejectReason.AlreadyApplied);
                    }

                    stock = take;
                    recordsToken = token != null;
                    break;
                }
                case TransactionKind.Buy:
                    seedsDelta = -Config.SeedPrice(transaction.Sink);
                    if (transaction.Sink == SeedSink.UndoRefill)
                    {
                        stock = StockDelta.Of(BoosterKind.Undo, Config.UndoRefillAmount);
                    }
                    else if (transaction.Sink == SeedSink.CanoesRefill)
                    {
                        if (!Config.CanoesRefillSeedsEnabled)
                        {
                            return Reject(transaction, RejectReason.Disabled);
                        }

                        if (canoes.IsFull)
                        {
                            return Reject(transaction, RejectReason.CanoesFull);
                        }

                        fill = true;
                    }

                    break;
                case TransactionKind.Rescue:
                {
                    int? basePrice = Config.RescueBasePrice(transaction.Level);
                    if (!basePrice.HasValue)
                    {
                        return Reject(transaction, RejectReason.NotUnlocked);
                    }

                    // Vale el precio con o sin descuento: el modal pudo abrirse antes de un cambio de día (04 §10.2).
                    if (transaction.Seeds != basePrice.Value && transaction.Seeds != Config.RescueFirstOfDayPrice(basePrice.Value))
                    {
                        return Reject(transaction, RejectReason.InvalidPrice);
                    }

                    seedsDelta = -transaction.Seeds;
                    rescue = true;
                    break;
                }
                case TransactionKind.GrantStock:
                case TransactionKind.UseStock:
                    break;
                case TransactionKind.HardFail:
                    if (CanoesActive(transaction.Level) && !canoes.IsInfinite && canoes.Count > 0)
                    {
                        canoeDelta = -1;
                    }

                    break;
                case TransactionKind.AddCanoe:
                    if (canoes.IsFull)
                    {
                        return Reject(transaction, RejectReason.CanoesFull);
                    }

                    canoeDelta = 1;
                    break;
                case TransactionKind.InfiniteCanoes:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(transaction), transaction.Kind, null);
            }

            long seedsAfter = (long)_profile.Wallet.Seeds + seedsDelta;
            if (seedsAfter < 0)
            {
                return Reject(transaction, RejectReason.InsufficientSeeds, (int)Math.Min(int.MaxValue, -seedsAfter));
            }

            if (seedsAfter > int.MaxValue)
            {
                return Reject(transaction, RejectReason.Overflow);
            }

            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                var kind = (BoosterKind)k;
                long after = (long)BoosterKinds.Read(_profile.Stock, kind) + stock.Get(kind);
                if (after < 0)
                {
                    return Reject(transaction, RejectReason.InsufficientStock);
                }

                if (after > int.MaxValue)
                {
                    return Reject(transaction, RejectReason.Overflow);
                }
            }

            bool grantsNoAds = transaction.GrantsNoAds && !_profile.Entitlements.NoAds;
            bool revokesNoAds = transaction.RevokesNoAds && _profile.Entitlements.NoAds;
            bool changes = seedsDelta != 0 || !stock.IsZero || canoeDelta != 0 || fill || rescue || grantsNoAds
                || transaction.InfiniteCanoes > TimeSpan.Zero || recordsToken || revokesNoAds;
            if (!changes)
            {
                return new TransactionResult(transaction, TransactionStatus.Applied, RejectReason.None, 0, Seeds, 0, false, false, false);
            }

            CanoesData canoeData = _profile.Wallet.Canoes;
            CanoeRules.Regenerate(canoeData, now, Config.CanoesMax, Config.CanoesRegen, frozen);
            DailyCounters.RollOver(_profile.Daily, today);

            _profile.Wallet.Seeds = (int)seedsAfter;
            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                var kind = (BoosterKind)k;
                int delta = stock.Get(kind);
                if (delta != 0)
                {
                    BoosterKinds.Write(_profile.Stock, kind, BoosterKinds.Read(_profile.Stock, kind) + delta);
                }
            }

            if (canoeDelta < 0)
            {
                CanoeRules.Spend(canoeData, now, Config.CanoesMax, Config.CanoesRegen);
            }
            else if (canoeDelta > 0)
            {
                CanoeRules.Add(canoeData, canoeDelta, Config.CanoesMax);
            }

            if (fill)
            {
                CanoeRules.Fill(canoeData, Config.CanoesMax);
            }

            if (transaction.InfiniteCanoes > TimeSpan.Zero)
            {
                CanoeRules.ExtendInfinite(canoeData, now, transaction.InfiniteCanoes);
            }

            if (shuffle)
            {
                _profile.Daily.ShufflesToday++; // 04 §2.4: 3 al día, también con el stock (B2)
            }

            if (rescue)
            {
                _profile.Daily.RescuesToday++;
                _profile.Stats.Rescues++;
            }

            if (grantsNoAds)
            {
                _profile.Entitlements.NoAds = true;
            }

            if (revokesNoAds)
            {
                _profile.Entitlements.NoAds = false;
            }

            if (recordsToken)
            {
                // B8: el token va en la misma escritura que lo que acredita o retira (orden ordinal, como en el JSON).
                InsertSorted(transaction.Kind == TransactionKind.Revoke ? _profile.VoidedPurchases : _profile.AppliedPurchases, token!);
            }

            _profile.EconomyTs = now;
            bool saved = TrySave();
            var result = new TransactionResult(
                transaction, TransactionStatus.Applied, RejectReason.None, seedsDelta, Seeds, 0, canoeDelta < 0, true, saved);
            Changed?.Invoke(result);
            PublishSeeds(transaction, seedsDelta);
            return result;
        }

        // earn_virtual_currency / spend_virtual_currency (08 §8.2) por el bus: la fuente o el sumidero de la transacción.
        private void PublishSeeds(Transaction transaction, int seedsDelta)
        {
            EventBus? bus = Events;
            if (bus == null || seedsDelta == 0)
            {
                return;
            }

            if (seedsDelta > 0)
            {
                bus.Publish(new SeedsEarned(transaction.Source.ToId(), seedsDelta, Seeds));
            }
            else
            {
                bus.Publish(new SeedsSpent(transaction.Sink.ToId(), -seedsDelta, Seeds));
            }
        }

        /// <summary>Reintenta un guardado pendiente. Devuelve si no queda nada por guardar.</summary>
        public bool Flush()
        {
            return !PendingSave || TrySave();
        }

        private bool TrySave()
        {
            try
            {
                _store.Save(_profile);
                PendingSave = false;
                return true;
            }
            catch (IOException)
            {
                PendingSave = true;
            }
            catch (UnauthorizedAccessException)
            {
                PendingSave = true;
            }
            catch (InvalidOperationException)
            {
                // El almacén protege un perfil más nuevo o una migración fallida (IProfileStore.Save): se sigue en memoria.
                PendingSave = true;
            }

            return false;
        }

        private static void InsertSorted(System.Collections.Generic.List<string> list, string value)
        {
            if (!list.Contains(value))
            {
                list.Add(value);
                list.Sort(StringComparer.Ordinal);
            }
        }

        private TransactionResult Reject(Transaction transaction, RejectReason reason, int missing = 0)
        {
            return new TransactionResult(transaction, TransactionStatus.Rejected, reason, 0, Seeds, missing, false, false, false);
        }
    }
}
