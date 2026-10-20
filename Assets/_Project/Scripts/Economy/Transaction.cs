#nullable enable
using System;

namespace AF.Economy
{
    /// <summary>Etiqueta de dificultad del nivel, que fija la Cosecha (05 §2.1; <c>metrics.label</c> de 08 §4.1).</summary>
    public enum RewardTier
    {
        /// <summary><c>normal</c>: <c>eco_reward_normal</c>.</summary>
        Normal = 0,

        /// <summary><c>hard</c> (Difícil): <c>eco_reward_hard</c>.</summary>
        Hard = 1,

        /// <summary><c>very_hard</c> (Muy difícil): <c>eco_reward_very_hard</c>.</summary>
        VeryHard = 2,
    }

    /// <summary>Conversión de la etiqueta del paquete de niveles.</summary>
    public static class RewardTiers
    {
        /// <summary>
        /// Convierte <c>normal</c>, <c>hard</c> o <c>very_hard</c> (08 §4.1, <c>schemas/af-level-1.schema.json</c>). Sin
        /// etiqueta (null o vacía) es <see cref="RewardTier.Normal"/>; cualquier otro texto devuelve false.
        /// </summary>
        public static bool TryParse(string? label, out RewardTier tier)
        {
            switch (label)
            {
                case null:
                case "":
                case "normal":
                    tier = RewardTier.Normal;
                    return true;
                case "hard":
                    tier = RewardTier.Hard;
                    return true;
                case "very_hard":
                    tier = RewardTier.VeryHard;
                    return true;
                default:
                    tier = RewardTier.Normal;
                    return false;
            }
        }
    }

    /// <summary>Fuente de semillas, con el valor <c>source</c> de <c>earn_virtual_currency</c> (04 §11).</summary>
    public enum SeedSource
    {
        /// <summary><c>win</c>: victoria sin etiqueta.</summary>
        Win = 0,

        /// <summary><c>hard</c>: victoria Difícil.</summary>
        Hard = 1,

        /// <summary><c>very_hard</c>: victoria Muy difícil.</summary>
        VeryHard = 2,

        /// <summary><c>double_harvest</c>: Cosecha doble.</summary>
        DoubleHarvest = 3,

        /// <summary><c>streak</c>: Crecida.</summary>
        Streak = 4,

        /// <summary><c>calendar</c>: Bitácora del río.</summary>
        Calendar = 5,

        /// <summary><c>basket</c>: Canasta.</summary>
        Basket = 6,

        /// <summary><c>daily_challenge</c>: Reto del río.</summary>
        DailyChallenge = 7,

        /// <summary><c>piggy</c>: Cántaro.</summary>
        Piggy = 8,

        /// <summary><c>iap</c>: compra.</summary>
        Iap = 9,

        /// <summary><c>refund</c>: devolución.</summary>
        Refund = 10,

        /// <summary><c>event</c>: evento.</summary>
        Event = 11,
    }

    /// <summary>Sumidero de semillas, con el valor <c>item_name</c> de <c>spend_virtual_currency</c> (04 §11; 05 §2.2).</summary>
    public enum SeedSink
    {
        /// <summary><c>undo_refill</c>: +<c>undo_refill_amount</c> Mareas al stock.</summary>
        UndoRefill = 0,

        /// <summary><c>extra_cuia</c>: Cuia extra.</summary>
        ExtraCuia = 1,

        /// <summary><c>hint</c>: Morpho.</summary>
        Hint = 2,

        /// <summary><c>shuffle</c>: Pororoca.</summary>
        Shuffle = 3,

        /// <summary><c>reveal</c>: Luciérnaga.</summary>
        Reveal = 4,

        /// <summary><c>skip</c>: Salto del Boto.</summary>
        Skip = 5,

        /// <summary><c>continue_cuia</c>: Cuia de rescate (se cotiza con <see cref="Wallet.QuoteRescue"/>).</summary>
        ContinueCuia = 6,

        /// <summary><c>canoes_refill</c>: Llenar canoas.</summary>
        CanoesRefill = 7,
    }

    /// <summary>Identificadores de analítica de <see cref="SeedSource"/> y <see cref="SeedSink"/> (04 §11).</summary>
    public static class EconomyIds
    {
        private static readonly string[] Sources =
        {
            "win", "hard", "very_hard", "double_harvest", "streak", "calendar", "basket", "daily_challenge", "piggy", "iap",
            "refund", "event",
        };

        private static readonly string[] Sinks =
        {
            "undo_refill", "extra_cuia", "hint", "shuffle", "reveal", "skip", "continue_cuia", "canoes_refill",
        };

        /// <summary>Valor de <c>source</c> en <c>earn_virtual_currency</c>.</summary>
        public static string ToId(this SeedSource source)
        {
            int i = (int)source;
            if (i < 0 || i >= Sources.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }

            return Sources[i];
        }

        /// <summary>Valor de <c>item_name</c> en <c>spend_virtual_currency</c>.</summary>
        public static string ToId(this SeedSink sink)
        {
            int i = (int)sink;
            if (i < 0 || i >= Sinks.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(sink), sink, null);
            }

            return Sinks[i];
        }

        /// <summary>Fuente de una victoria según su etiqueta.</summary>
        public static SeedSource WinSource(RewardTier tier)
        {
            switch (tier)
            {
                case RewardTier.Normal: return SeedSource.Win;
                case RewardTier.Hard: return SeedSource.Hard;
                case RewardTier.VeryHard: return SeedSource.VeryHard;
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }
    }

    /// <summary>Tipo de <see cref="Transaction"/>.</summary>
    public enum TransactionKind
    {
        /// <summary>Cosecha de una victoria.</summary>
        Win = 0,

        /// <summary>Cosecha doble: otra Cosecha igual.</summary>
        DoubleHarvest = 1,

        /// <summary>Semillas de otra fuente, con cantidad explícita.</summary>
        Earn = 2,

        /// <summary>Compra con semillas a precio fijo.</summary>
        Buy = 3,

        /// <summary>Cuia de rescate al precio cotizado.</summary>
        Rescue = 4,

        /// <summary>Unidades de stock regaladas o premiadas.</summary>
        GrantStock = 5,

        /// <summary>Gasta una unidad de stock.</summary>
        UseStock = 6,

        /// <summary>Parte económica del fallo duro: la canoa.</summary>
        HardFail = 7,

        /// <summary>+1 canoa (Canoa extra).</summary>
        AddCanoe = 8,

        /// <summary>Canoas infinitas.</summary>
        InfiniteCanoes = 9,

        /// <summary>Contenido de una compra con dinero.</summary>
        Purchase = 10,
    }

    /// <summary>
    /// Petición de cambio en la cartera, que solo aplica <see cref="Wallet.Apply"/> (08 §2.2 <c>AF.Economy</c>: toda
    /// mutación pasa por <c>Wallet.Apply(Transaction)</c>). Es un valor inmutable. Las cantidades que fija el GDD
    /// (Cosecha, precios, recarga de Marea, tope de canoas) no viajan en la transacción: las pone la
    /// <see cref="EconomyConfig"/> de la cartera al aplicarla. Solo llevan cantidad explícita el precio cotizado del
    /// rescate (04 §10.2: el precio no cambia con el modal abierto), las semillas de otras fuentes y las compras.
    /// </summary>
    public sealed class Transaction
    {
        private Transaction(TransactionKind kind)
        {
            Kind = kind;
        }

        /// <summary>Tipo.</summary>
        public TransactionKind Kind { get; private set; }

        /// <summary>Nivel en juego (0 si no aplica).</summary>
        public int Level { get; private set; }

        /// <summary>Etiqueta del nivel (<see cref="TransactionKind.Win"/> y <see cref="TransactionKind.DoubleHarvest"/>).</summary>
        public RewardTier Tier { get; private set; }

        /// <summary>Fuente de semillas (victorias, Cosecha doble, <see cref="TransactionKind.Earn"/> y compras).</summary>
        public SeedSource Source { get; private set; }

        /// <summary>Sumidero (<see cref="TransactionKind.Buy"/> y <see cref="TransactionKind.Rescue"/>).</summary>
        public SeedSink Sink { get; private set; }

        /// <summary>
        /// Semillas explícitas: las ganadas (<see cref="TransactionKind.Earn"/>, <see cref="TransactionKind.Purchase"/>), las de la
        /// Cosecha doble del Reto del río (<see cref="DoubleHarvestDaily"/>) o el precio cotizado del rescate. 0 en el resto.
        /// </summary>
        public int Seeds { get; private set; }

        /// <summary>Comodín (<see cref="TransactionKind.GrantStock"/> y <see cref="TransactionKind.UseStock"/>).</summary>
        public BoosterKind Booster { get; private set; }

        /// <summary>Stock que entra (<see cref="TransactionKind.GrantStock"/> y <see cref="TransactionKind.Purchase"/>).</summary>
        public StockDelta Stock { get; private set; }

        /// <summary>Canoas infinitas que se añaden.</summary>
        public TimeSpan InfiniteCanoes { get; private set; }

        /// <summary>La compra aplica una Cuia de rescate (Balsa de rescate, 05 §4.1): cuenta para <c>rescues_today</c>.</summary>
        public bool AppliesRescue { get; private set; }

        /// <summary>La compra da el derecho Sin anuncios (<c>entitlements.no_ads</c>).</summary>
        public bool GrantsNoAds { get; private set; }

        /// <summary>SKU de la compra (05 §4.1), o null.</summary>
        public string? Sku { get; private set; }

        /// <summary>
        /// Cosecha doble del río crecido (<see cref="DoubleHarvestRiver"/>): sin anuncio y sin la compuerta
        /// <c>ads_rv_double_harvest_from_level</c> (Q-C14 (a) [P]).
        /// </summary>
        public bool RiverFull { get; private set; }

        /// <summary>Cosecha de una victoria en el nivel <paramref name="level"/>: 20, 60 u 80 según la etiqueta (05 §2.1).</summary>
        public static Transaction Win(int level, RewardTier tier)
        {
            return new Transaction(TransactionKind.Win) { Level = RequireLevel(level), Tier = tier, Source = EconomyIds.WinSource(tier) };
        }

        /// <summary>
        /// Cosecha doble (04 §2.8): otra Cosecha igual a la de la victoria. Se rechaza antes de
        /// <c>ads_rv_double_harvest_from_level</c> (11). El tope diario del video es cosa de <c>AF.Ads</c>.
        /// </summary>
        public static Transaction DoubleHarvest(int level, RewardTier tier)
        {
            return new Transaction(TransactionKind.DoubleHarvest) { Level = RequireLevel(level), Tier = tier, Source = SeedSource.DoubleHarvest };
        }

        /// <summary>
        /// Cosecha doble del río crecido (04 §2.8: «durante río crecido… se aplica sin video»; 05 §2.4; nota
        /// <c>docs/dev/crecida-canasta.md</c> C8, Q-C1 (a) y Q-C14 (a) [P], enganche E35): otra Cosecha igual a la de la victoria,
        /// con <c>source</c> = <c>double_harvest</c>. A diferencia de <see cref="DoubleHarvest"/>, no pasa por la compuerta
        /// <c>ads_rv_double_harvest_from_level</c>: no es la ubicación de vídeo <c>rv_double_harvest</c> y no gasta su tope
        /// (<c>ads_rv_cap_double_harvest</c>, cosa de <c>AF.Ads</c>). Solo la pide la Crecida
        /// (<see cref="StreakOutcome.AppendTransactions"/>), que ya decidió si toca.
        /// </summary>
        internal static Transaction DoubleHarvestRiver(int level, RewardTier tier)
        {
            return new Transaction(TransactionKind.DoubleHarvest)
            {
                Level = RequireLevel(level), Tier = tier, Source = SeedSource.DoubleHarvest, RiverFull = true,
            };
        }

        /// <summary>
        /// Cosecha doble del Reto del río (04 §2.8; 06 §4.1, «Premio»: «Cosecha doble disponible»; enganche E9): otras
        /// <paramref name="seeds"/> semillas, las del premio del Reto recién cobrado (60 → 120 [P]), con <c>source</c> =
        /// <c>double_harvest</c>. <paramref name="level"/> es el nivel efectivo del Reto (<c>max_level</c> + 1): como la de
        /// campaña, se rechaza antes de <c>ads_rv_double_harvest_from_level</c> (11). El tope diario (<c>ads_rv_cap_double_harvest</c>,
        /// 5, compartido con la campaña: es la misma ubicación <c>rv_double_harvest</c>) y el cooldown son cosa de <c>AF.Ads</c>.
        /// </summary>
        public static Transaction DoubleHarvestDaily(int seeds, int level)
        {
            return new Transaction(TransactionKind.DoubleHarvest)
            {
                Level = RequireLevel(level), Seeds = RequirePositive(seeds, nameof(seeds)), Source = SeedSource.DoubleHarvest,
            };
        }

        /// <summary>
        /// Semillas de una fuente sin cantidad fija en la economía (Crecida, Bitácora, Canasta, Reto del río, compras,
        /// devoluciones…). Las victorias y la Cosecha doble van por <see cref="Win"/> y <see cref="DoubleHarvest"/>.
        /// </summary>
        public static Transaction Earn(SeedSource source, int seeds)
        {
            if (source == SeedSource.Win || source == SeedSource.Hard || source == SeedSource.VeryHard || source == SeedSource.DoubleHarvest)
            {
                throw new ArgumentException("Las victorias y la Cosecha doble usan Transaction.Win y Transaction.DoubleHarvest.", nameof(source));
            }

            return new Transaction(TransactionKind.Earn) { Source = source, Seeds = RequirePositive(seeds, nameof(seeds)) };
        }

        /// <summary>
        /// Compra con semillas a precio fijo de 08 §9 (05 §2.2): Cuia extra, Morpho, Pororoca, Luciérnaga, Salto del Boto,
        /// recarga de Marea (que suma <c>undo_refill_amount</c> al stock) o Llenar canoas (hasta el tope). Para la Cuia de
        /// rescate, <see cref="Rescue"/>.
        /// </summary>
        public static Transaction Buy(SeedSink sink)
        {
            if (sink == SeedSink.ContinueCuia)
            {
                throw new ArgumentException("La Cuia de rescate usa Transaction.Rescue con el precio cotizado.", nameof(sink));
            }

            if (sink < SeedSink.UndoRefill || sink > SeedSink.CanoesRefill)
            {
                throw new ArgumentOutOfRangeException(nameof(sink), sink, null);
            }

            return new Transaction(TransactionKind.Buy) { Sink = sink };
        }

        /// <summary>
        /// Cuia de rescate en el nivel <paramref name="level"/> al precio <paramref name="quotedPrice"/> que mostró el modal
        /// (<see cref="Wallet.QuoteRescue"/>; 04 §6.2 y §10.2). Suma 1 a <c>rescues_today</c> y a <c>stats.rescues</c>. El tope
        /// de 1 por nivel y la verificación «la compra cumple» son de <c>AF.Boosters</c>.
        /// </summary>
        public static Transaction Rescue(int level, int quotedPrice)
        {
            return new Transaction(TransactionKind.Rescue)
            {
                Level = RequireLevel(level),
                Sink = SeedSink.ContinueCuia,
                Seeds = RequirePositive(quotedPrice, nameof(quotedPrice)),
            };
        }

        /// <summary><paramref name="units"/> unidades de <paramref name="booster"/> al stock (regalos del tutorial, Canasta, Bitácora…).</summary>
        public static Transaction GrantStock(BoosterKind booster, int units)
        {
            return new Transaction(TransactionKind.GrantStock)
            {
                Booster = booster,
                Stock = StockDelta.Of(booster, RequirePositive(units, nameof(units))),
            };
        }

        /// <summary>Gasta 1 unidad de stock de <paramref name="booster"/> (se rechaza si no hay).</summary>
        public static Transaction UseStock(BoosterKind booster)
        {
            return new Transaction(TransactionKind.UseStock) { Booster = booster, Stock = StockDelta.Of(booster, -1) };
        }

        /// <summary>
        /// Parte económica del fallo duro (04 §5.2; <c>failHard</c> de 04 §6.2 con D-008): −1 canoa desde
        /// <c>rescue_min_level</c> (31), salvo con Canoas infinitas o sin canoas que perder (una partida empezada nunca se
        /// corta por canoas, 04 §7.1). Nunca se rechaza; <see cref="TransactionResult.CanoeLost"/> dice si costó canoa.
        /// <c>fails_level</c>, la Crecida y la Hoja escudo son de <c>AF.GameFlow</c>.
        /// </summary>
        public static Transaction HardFail(int level)
        {
            return new Transaction(TransactionKind.HardFail) { Level = RequireLevel(level) };
        }

        /// <summary>+1 canoa (Canoa extra por video, 04 §7.1; el tope diario es de <c>AF.Ads</c>). Se rechaza con las canoas llenas.</summary>
        public static Transaction AddCanoe()
        {
            return new Transaction(TransactionKind.AddCanoe);
        }

        /// <summary>Canoas infinitas durante <paramref name="duration"/>, sumadas a las que haya (04 §7.1).</summary>
        public static Transaction InfiniteCanoesFor(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), "La duración debe ser positiva.");
            }

            return new Transaction(TransactionKind.InfiniteCanoes) { InfiniteCanoes = duration };
        }

        /// <summary>
        /// Contenido de una compra con dinero ya confirmada (05 §4.1; <c>AF.IAP</c>): semillas, stock, Canoas infinitas, un
        /// rescate aplicado y el derecho Sin anuncios. Una compra cancelada o fallida no llega aquí (nunca descuenta).
        /// </summary>
        public static Transaction Purchase(
            string sku,
            int seeds = 0,
            StockDelta stock = default,
            TimeSpan infiniteCanoes = default,
            bool appliesRescue = false,
            bool grantsNoAds = false)
        {
            if (string.IsNullOrEmpty(sku))
            {
                throw new ArgumentException("Falta el SKU.", nameof(sku));
            }

            if (seeds < 0 || stock.HasNegative || infiniteCanoes < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(seeds), "Una compra solo suma.");
            }

            return new Transaction(TransactionKind.Purchase)
            {
                Sku = sku,
                Source = SeedSource.Iap,
                Seeds = seeds,
                Stock = stock,
                InfiniteCanoes = infiniteCanoes,
                AppliesRescue = appliesRescue,
                GrantsNoAds = grantsNoAds,
            };
        }

        private static int RequireLevel(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Los niveles empiezan en 1.");
            }

            return level;
        }

        private static int RequirePositive(int value, string name)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(name, "Debe ser mayor que 0.");
            }

            return value;
        }
    }

    /// <summary>Resultado de <see cref="Wallet.Apply"/>.</summary>
    public enum TransactionStatus
    {
        /// <summary>Aplicada (puede no haber cambiado nada, p. ej. un fallo duro sin canoa en juego).</summary>
        Applied = 0,

        /// <summary>Rechazada: el perfil no cambió.</summary>
        Rejected = 1,
    }

    /// <summary>Por qué se rechazó una transacción.</summary>
    public enum RejectReason
    {
        /// <summary>No se rechazó.</summary>
        None = 0,

        /// <summary>Faltan semillas (<see cref="TransactionResult.Missing"/>; «Te faltan {n}», 04 §1.1).</summary>
        InsufficientSeeds = 1,

        /// <summary>No hay stock de ese comodín.</summary>
        InsufficientStock = 2,

        /// <summary>Las canoas ya están llenas.</summary>
        CanoesFull = 3,

        /// <summary>Todavía no está disponible en este nivel.</summary>
        NotUnlocked = 4,

        /// <summary>Apagado por configuración (<c>canoes_refill_seeds_enabled</c>).</summary>
        Disabled = 5,

        /// <summary>El precio cotizado no es ninguno de los válidos para ese nivel.</summary>
        InvalidPrice = 6,

        /// <summary>El saldo o el stock se saldría del rango de un entero.</summary>
        Overflow = 7,
    }

    /// <summary>Lo que devuelve <see cref="Wallet.Apply"/>.</summary>
    public sealed class TransactionResult
    {
        internal TransactionResult(
            Transaction transaction,
            TransactionStatus status,
            RejectReason reason,
            int seedsDelta,
            int seedsBalance,
            int missing,
            bool canoeLost,
            bool changed,
            bool saved)
        {
            Transaction = transaction;
            Status = status;
            Reason = reason;
            SeedsDelta = seedsDelta;
            SeedsBalance = seedsBalance;
            Missing = missing;
            CanoeLost = canoeLost;
            Changed = changed;
            Saved = saved;
        }

        /// <summary>La transacción.</summary>
        public Transaction Transaction { get; }

        /// <summary>Aplicada o rechazada.</summary>
        public TransactionStatus Status { get; }

        /// <summary>Motivo del rechazo (<see cref="RejectReason.None"/> si se aplicó).</summary>
        public RejectReason Reason { get; }

        /// <summary>Se aplicó.</summary>
        public bool IsApplied => Status == TransactionStatus.Applied;

        /// <summary>Semillas que entraron (+) o salieron (−); 0 si se rechazó.</summary>
        public int SeedsDelta { get; }

        /// <summary>Saldo de semillas después (o el de siempre, si se rechazó).</summary>
        public int SeedsBalance { get; }

        /// <summary>Semillas que faltaban (con <see cref="RejectReason.InsufficientSeeds"/>), para «Te faltan {n}».</summary>
        public int Missing { get; }

        /// <summary>El fallo duro costó una canoa (para <c>af_level_fail.canoe_lost</c>, 04 §11).</summary>
        public bool CanoeLost { get; }

        /// <summary>El perfil cambió (y se marcó <c>economy_ts</c>).</summary>
        public bool Changed { get; }

        /// <summary>
        /// El <c>IProfileStore</c> de la cartera aceptó el guardado sin error (false si no hubo cambio o si falló la escritura).
        /// Con un almacén que agrupa escrituras (<c>AF.GameFlow.ProfileSaveBatch</c>, como en el juego) y dentro de un lote,
        /// significa «anotado», no «en disco»: la escritura real ocurre al cerrar el lote más externo y, si falla, solo lo dice
        /// <c>ProfileSaveBatch.PendingSave</c> (el cambio sigue en memoria y sale con el siguiente guardado o con
        /// <c>Flush</c>). Quien necesite saber si quedó en disco mira el lote tras cerrarlo.
        /// </summary>
        public bool Saved { get; }
    }
}
