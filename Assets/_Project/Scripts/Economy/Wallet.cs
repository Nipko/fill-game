#nullable enable
using System;
using System.IO;
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

        /// <summary>Se aplicó una transacción que cambió el perfil (después de guardarlo). Para la UI y, más adelante, la analítica.</summary>
        public event Action<TransactionResult>? Changed;

        /// <summary>Cifras con las que trabaja la cartera.</summary>
        public EconomyConfig Config { get; }

        /// <summary>Saldo de semillas.</summary>
        public int Seeds => _profile.Wallet.Seeds;

        /// <summary>Stock de comodines.</summary>
        public BoosterStock Stock { get; }

        /// <summary>Canoas en este instante, con la regeneración aplicada.</summary>
        public CanoesStatus Canoes => CanoeRules.Peek(_profile.Wallet.Canoes, _clock.UtcNow, Config.CanoesMax, Config.CanoesRegen);

        /// <summary>Cuias de rescate compradas hoy (día local; 0 si los contadores son de un día anterior).</summary>
        public int RescuesToday => DailyCounters.IsStale(_profile.Daily, _clock.LocalDateKey()) ? 0 : _profile.Daily.RescuesToday;

        /// <summary>
        /// Un cambio quedó sin guardar porque falló la escritura; se reintenta en la próxima mutación o con <see cref="Flush"/>.
        /// Solo ve los fallos del <c>IProfileStore</c> que recibe: con un almacén que agrupa escrituras
        /// (<c>AF.GameFlow.ProfileSaveBatch</c>, dentro de un lote) el guardado solo se anota y no falla aquí; si la escritura
        /// del cierre del lote falla, esto sigue en false y el pendiente lo lleva <c>ProfileSaveBatch.PendingSave</c>.
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
            return level >= Config.RescueMinLevel;
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
            CanoesStatus canoes = CanoeRules.Peek(_profile.Wallet.Canoes, now, Config.CanoesMax, Config.CanoesRegen);

            int seedsDelta = 0;
            StockDelta stock = transaction.Stock;
            int canoeDelta = 0;
            bool fill = false;
            bool rescue = false;
            switch (transaction.Kind)
            {
                case TransactionKind.Win:
                    seedsDelta = Config.WinReward(transaction.Tier);
                    break;
                case TransactionKind.DoubleHarvest:
                    if (transaction.Level < Config.AdsRvDoubleHarvestFromLevel)
                    {
                        return Reject(transaction, RejectReason.NotUnlocked);
                    }

                    seedsDelta = Config.WinReward(transaction.Tier);
                    break;
                case TransactionKind.Earn:
                case TransactionKind.Purchase:
                    seedsDelta = transaction.Seeds;
                    rescue = transaction.AppliesRescue;
                    break;
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
            bool changes = seedsDelta != 0 || !stock.IsZero || canoeDelta != 0 || fill || rescue || grantsNoAds
                || transaction.InfiniteCanoes > TimeSpan.Zero;
            if (!changes)
            {
                return new TransactionResult(transaction, TransactionStatus.Applied, RejectReason.None, 0, Seeds, 0, false, false, false);
            }

            CanoesData canoeData = _profile.Wallet.Canoes;
            CanoeRules.Regenerate(canoeData, now, Config.CanoesMax, Config.CanoesRegen);
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

            if (rescue)
            {
                _profile.Daily.RescuesToday++;
                _profile.Stats.Rescues++;
            }

            if (grantsNoAds)
            {
                _profile.Entitlements.NoAds = true;
            }

            _profile.EconomyTs = now;
            bool saved = TrySave();
            var result = new TransactionResult(
                transaction, TransactionStatus.Applied, RejectReason.None, seedsDelta, Seeds, 0, canoeDelta < 0, true, saved);
            Changed?.Invoke(result);
            return result;
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

        private TransactionResult Reject(Transaction transaction, RejectReason reason, int missing = 0)
        {
            return new TransactionResult(transaction, TransactionStatus.Rejected, reason, 0, Seeds, missing, false, false, false);
        }
    }
}
