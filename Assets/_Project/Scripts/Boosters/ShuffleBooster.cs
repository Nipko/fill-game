#nullable enable
using System;
using AF.Core.Board;
using AF.Economy;
using AF.Platform;

namespace AF.Boosters
{
    /// <summary>
    /// Cifras de la Pororoca que no están en <see cref="BoosterConfig"/> ni en <see cref="EconomyConfig"/> (08 §9, con sus
    /// nombres exactos y sus defaults compilados): topes por nivel y por día, holgura del par, presupuesto del A* y si la ola
    /// revela las capas ocultas. <c>shuffle_unlock_level</c> está en <see cref="BoosterConfig.ShuffleUnlockLevel"/> y
    /// <c>shuffle_seed_price</c> en <see cref="EconomyConfig.ShuffleSeedPrice"/>. Es una clase aparte para no tocar
    /// <c>BoosterConfig.cs</c> (encargo del Spark §3.1); <c>ShuffleBoosterTests.EveryShuffleDefault_IsTheOneOfThe08_9Table</c> las contrasta
    /// con la tabla de 08 §9.
    /// </summary>
    public sealed class ShuffleConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9.</summary>
        public static ShuffleConfig Defaults => new ShuffleConfig();

        /// <summary><c>shuffle_max_per_level</c>: Pororocas por nivel, también con stock (04 §2.4: 1).</summary>
        [RemoteConfigKey("shuffle_max_per_level")]
        public int ShuffleMaxPerLevel { get; set; } = 1;

        /// <summary><c>shuffle_max_per_day</c>: Pororocas por día local, también con stock (04 §2.4: 3).</summary>
        [RemoteConfigKey("shuffle_max_per_day")]
        public int ShuffleMaxPerDay { get; set; } = 3;

        /// <summary><c>shuffle_par_slack</c>: par nuevo ≤ par actual + holgura (04 §2.4 y 03 §16: 2).</summary>
        [RemoteConfigKey("shuffle_par_slack")]
        public int ShuffleParSlack { get; set; } = 2;

        /// <summary><c>shuffle_budget_nodes</c>: estados de cada A* de verificación (03 §5: 40.000).</summary>
        [RemoteConfigKey("shuffle_budget_nodes")]
        public int ShuffleBudgetNodes { get; set; } = 40000;

        /// <summary><c>shuffle_reveals_murky</c>: la ola revela todas las capas ocultas del nivel (04 §2.4 a: true).</summary>
        [RemoteConfigKey("shuffle_reveals_murky")]
        public bool ShuffleRevealsMurky { get; set; } = true;

        /// <summary>Comprueba que los valores tienen sentido (no que coincidan con 08 §9: eso lo hacen los tests).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            if (ShuffleMaxPerLevel < 0 || ShuffleMaxPerDay < 0)
            {
                throw new InvalidOperationException("ShuffleConfig: los topes no pueden ser negativos.");
            }

            if (ShuffleParSlack < 0 || ShuffleBudgetNodes < 1)
            {
                throw new InvalidOperationException("ShuffleConfig: shuffle_par_slack >= 0 y shuffle_budget_nodes >= 1.");
            }
        }
    }

    /// <summary>Estado del botón «Barajar» de la bandeja (04 §2.4; 07 §7.4).</summary>
    public enum ShuffleState
    {
        /// <summary>Antes de <c>shuffle_unlock_level</c> (30) la ranura no existe y la bandeja se centra (07 §7.4).</summary>
        Hidden = 0,

        /// <summary>Se puede usar: del stock o pagando.</summary>
        Ready = 1,

        /// <summary>Ya se usó <c>shuffle_max_per_level</c> veces en este nivel: gris (<c>af_booster_denied.reason = cap_level</c>).</summary>
        UsedLevel = 2,

        /// <summary>Ya se usó <c>shuffle_max_per_day</c> veces hoy: gris (<c>cap_day</c>).</summary>
        UsedToday = 3,

        /// <summary>
        /// El nivel lleva Lluvia o Piraña (04 §2.4 d; D-007) o tiene ocultas sin <c>shuffle_reveals_murky</c>: gris
        /// (<c>unsupported</c>).
        /// </summary>
        Unsupported = 4,

        /// <summary>Quedan menos de 2 colores movibles: gris y «Hay poca agua libre para la ola» (04 §2.4 b; <c>not_eligible</c>).</summary>
        LowWater = 5,

        /// <summary>
        /// Fuera de PLAYING o con la victoria o el bloqueo pendientes: gris. No se usa en el modal de bloqueo (04 §2.4: «a 300
        /// semillas sería un rescate barato»; <c>not_eligible</c>).
        /// </summary>
        Disabled = 6,
    }

    /// <summary>Con qué se paga una Pororoca (04 §1.1 y §2.4: «consume primero el stock»).</summary>
    public enum ShufflePayment
    {
        /// <summary>Una del stock (<c>stock.shuffle</c>: la de cada 7 Retos del río seguidos).</summary>
        Stock = 0,

        /// <summary><c>shuffle_seed_price</c> semillas (300; 08 §9).</summary>
        Seeds = 1,
    }

    /// <summary>Lo que muestra el botón «Barajar»: stock «+N», precio o «Te faltan {n}» (04 §1.1 y §2.4).</summary>
    public readonly struct ShuffleOffer
    {
        /// <summary>Crea la oferta.</summary>
        public ShuffleOffer(ShuffleState state, int stock, int price, int missing)
        {
            State = state;
            Stock = stock;
            Price = price;
            Missing = missing;
        }

        /// <summary>Estado.</summary>
        public ShuffleState State { get; }

        /// <summary>Pororocas en el stock (se gastan antes que las semillas).</summary>
        public int Stock { get; }

        /// <summary>Precio en semillas (<c>shuffle_seed_price</c>, 300).</summary>
        public int Price { get; }

        /// <summary>Semillas que faltan si no hay stock (0 si alcanza): precio en rojo y «Te faltan {n}».</summary>
        public int Missing { get; }

        /// <summary>Se paga con el stock.</summary>
        public bool FromStock => Stock > 0;

        /// <summary>La ranura existe.</summary>
        public bool Shown => State != ShuffleState.Hidden;

        /// <summary>Se puede tocar (lista, y con stock o saldo).</summary>
        public bool CanUse => State == ShuffleState.Ready && (FromStock || Missing == 0);

        /// <summary>Con qué se pagaría ahora: el stock si lo hay; si no, las semillas.</summary>
        public ShufflePayment Payment => FromStock ? ShufflePayment.Stock : ShufflePayment.Seeds;

        /// <summary>
        /// Pide la hoja de confirmación de una línea (04 §2.4; regla general «confirmación para gastos ≥ 200 semillas» [P]): solo
        /// si se paga con semillas y el precio llega a <see cref="ShuffleBooster.ConfirmMinSeeds"/>. Con el stock no se gastan
        /// semillas y no se pregunta [P] (<c>docs/dev/pororoca.md</c>, P-6).
        /// </summary>
        public bool NeedsConfirmation => State == ShuffleState.Ready && !FromStock && Price >= ShuffleBooster.ConfirmMinSeeds;

        /// <summary>Motivo de <c>af_booster_denied.reason</c> (04 §11) del estado, o null si se puede usar.</summary>
        public string? DeniedReason
        {
            get
            {
                switch (State)
                {
                    case ShuffleState.Ready:
                        return FromStock || Missing == 0 ? null : "no_balance";
                    case ShuffleState.UsedLevel:
                        return "cap_level";
                    case ShuffleState.UsedToday:
                        return "cap_day";
                    case ShuffleState.Unsupported:
                        return "unsupported";
                    default:
                        return "not_eligible";
                }
            }
        }
    }

    /// <summary>Por qué el tablero no admite la ola, visto desde el cliente (lo calcula <c>AF.Solver</c> y llega como primitivo).</summary>
    public enum ShuffleBoardBlock
    {
        /// <summary>Se puede barajar.</summary>
        None = 0,

        /// <summary>Lluvia o Piraña en el nivel, u ocultas sin <c>shuffle_reveals_murky</c> (04 §2.4 a y d).</summary>
        Unsupported = 1,

        /// <summary>Menos de 2 colores movibles (04 §2.4 b).</summary>
        LowWater = 2,
    }

    /// <summary>Cómo salió la ola: <c>af_shuffle_result.method</c> (04 §11).</summary>
    public enum ShuffleWaveMethod
    {
        /// <summary>Reconstrucción inversa verificada (<c>inverse</c>).</summary>
        Inverse = 0,

        /// <summary>Permutar + verificar (<c>permute</c>).</summary>
        Permute = 1,

        /// <summary>Ningún tablero verificado: «La ola no pudo remover el río» y no se cobra (<c>failed</c>).</summary>
        Failed = 2,
    }

    /// <summary>
    /// Resultado de una Pororoca en el cliente (04 §2.4 y §11, <c>af_shuffle_result</c>): el tablero nuevo (sin ocultas; null si
    /// falló), el método, el par antes y después (−1 si el A* no lo encontró) y los intentos. Lo produce <c>ShuffleWave</c> con
    /// <c>AF.Solver</c>; aquí solo los datos, para que la vista y la analítica no dependan del solver.
    /// </summary>
    public readonly struct ShuffleOutcome
    {
        /// <summary>Crea el resultado.</summary>
        public ShuffleOutcome(ShuffleWaveMethod method, BoardState? board, int parBefore, int parAfter, int tries, int revealed)
        {
            Method = method;
            Board = board;
            ParBefore = parBefore;
            ParAfter = parAfter;
            Tries = tries;
            Revealed = revealed;
        }

        /// <summary>Método.</summary>
        public ShuffleWaveMethod Method { get; }

        /// <summary>Tablero barajado (null si falló).</summary>
        public BoardState? Board { get; }

        /// <summary><c>par_before</c>: longitud de la solución del A* desde el tablero de partida (−1 si no la encontró).</summary>
        public int ParBefore { get; }

        /// <summary><c>par_after</c>: la del tablero barajado (−1 si falló).</summary>
        public int ParAfter { get; }

        /// <summary><c>tries</c>: intentos inversos más permutaciones.</summary>
        public int Tries { get; }

        /// <summary><c>revealed_murky</c>: dosis ocultas que la ola reveló.</summary>
        public int Revealed { get; }

        /// <summary>Hay tablero nuevo.</summary>
        public bool Succeeded => Board != null && Method != ShuffleWaveMethod.Failed;

        /// <summary>Valor de <c>af_shuffle_result.method</c>: <c>inverse</c>, <c>permute</c> o <c>failed</c>.</summary>
        public string MethodId => Method == ShuffleWaveMethod.Inverse ? "inverse" : Method == ShuffleWaveMethod.Permute ? "permute" : "failed";
    }

    /// <summary>
    /// Pororoca · Barajar (GDD 04 §2.4; 03 §16; 08 §9 <c>shuffle_*</c>): ranura desde el nivel 30, 300 semillas o una del stock
    /// (la de 7 Retos del río seguidos), 1 por nivel y 3 por día también con stock, solo mientras quede algún vertido útil (no en
    /// el modal de bloqueo), deshabilitada en los niveles con Lluvia o Piraña, con la Tinaja fija, sin anuncio y con la hoja de
    /// confirmación al pagar con semillas. El barajado es de <c>AF.Solver</c> (<c>Shuffler</c>) y su aplicación de
    /// <c>AF.Core</c> (<c>LevelSession.ApplyShuffle</c>): aquí, la oferta del botón y el cobro. Sin motor y sin <c>AF.Levels</c>
    /// (08 §2.2): lo que necesita del nivel y del tablero entra como primitivos, como en <see cref="ExtraCuiaBooster"/>.
    /// </summary>
    public static class ShuffleBooster
    {
        /// <summary>
        /// Gasto en semillas desde el que se pide la hoja de confirmación de una línea (04 §2.4: «regla general: confirmación para
        /// gastos ≥200 semillas [P]»). No está en 08 §9: constante hasta que el dueño la lleve a la tabla.
        /// </summary>
        public const int ConfirmMinSeeds = 200;

        /// <summary>La ranura existe en <paramref name="level"/> (desde <c>shuffle_unlock_level</c>, 30).</summary>
        public static bool IsUnlocked(int level, BoosterConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return level >= config.ShuffleUnlockLevel;
        }

        /// <summary>Precio en semillas (<c>shuffle_seed_price</c>; sin cartera, el default de 08 §9).</summary>
        public static int Price(Wallet? wallet)
        {
            return wallet?.Price(SeedSink.Shuffle) ?? EconomyConfig.Defaults.ShuffleSeedPrice;
        }

        /// <summary>
        /// Estado del botón en <paramref name="level"/>, con <paramref name="usedThisLevel"/> Pororocas ya usadas en el nivel y
        /// <paramref name="usedToday"/> hoy, <paramref name="playing"/> (PLAYING sin victoria ni bloqueo pendiente: 04 §2.4, «solo
        /// mientras quede ≥ 1 vertido legal»), lo que dice el tablero (<paramref name="board"/>), el stock y el saldo. En orden:
        /// la ranura no existe, el nivel no la admite, el tope del nivel, el del día, poca agua, fuera de juego y lista.
        /// </summary>
        public static ShuffleOffer Evaluate(
            int level, int usedThisLevel, int usedToday, bool playing, ShuffleBoardBlock board, Wallet? wallet, BoosterConfig config, ShuffleConfig shuffle)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (shuffle == null)
            {
                throw new ArgumentNullException(nameof(shuffle));
            }

            int stock = wallet?.Stock.Get(BoosterKind.Shuffle) ?? 0;
            int price = Price(wallet);
            int missing = stock > 0 ? 0 : (wallet == null ? price : wallet.Missing(price));
            ShuffleState state;
            if (!IsUnlocked(level, config))
            {
                state = ShuffleState.Hidden;
            }
            else if (board == ShuffleBoardBlock.Unsupported)
            {
                state = ShuffleState.Unsupported;
            }
            else if (usedThisLevel >= shuffle.ShuffleMaxPerLevel)
            {
                state = ShuffleState.UsedLevel;
            }
            else if (usedToday >= shuffle.ShuffleMaxPerDay)
            {
                state = ShuffleState.UsedToday;
            }
            else if (board == ShuffleBoardBlock.LowWater)
            {
                state = ShuffleState.LowWater;
            }
            else if (!playing)
            {
                state = ShuffleState.Disabled;
            }
            else
            {
                state = ShuffleState.Ready;
            }

            return new ShuffleOffer(state, stock, price, missing);
        }

        /// <summary><paramref name="payment"/> se puede cobrar ahora: hay stock, o el saldo llega al precio.</summary>
        public static bool CanPay(Wallet wallet, ShufflePayment payment)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            switch (payment)
            {
                case ShufflePayment.Stock:
                    return wallet.Stock.Has(BoosterKind.Shuffle);
                case ShufflePayment.Seeds:
                    return wallet.Missing(wallet.Price(SeedSink.Shuffle)) == 0;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Cobra una Pororoca: una del stock (<see cref="Transaction.UseStock"/>) o <c>shuffle_seed_price</c> semillas
        /// (<see cref="Transaction.Buy"/> de <see cref="SeedSink.Shuffle"/>). Quien llama ya aplicó y guardó el tablero nuevo
        /// (aplicar → guardar → cobrar, como la Luciérnaga: nunca se cobra sin entregar) y nunca cobra si la ola falló (04 §2.4:
        /// «no se cobra»). Si se rechaza, no cambia nada.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="payment"/> no es un valor definido.</exception>
        public static TransactionResult Pay(Wallet wallet, ShufflePayment payment)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            switch (payment)
            {
                case ShufflePayment.Stock:
                    return wallet.Apply(Transaction.UseStock(BoosterKind.Shuffle));
                case ShufflePayment.Seeds:
                    return wallet.Apply(Transaction.Buy(SeedSink.Shuffle));
                default:
                    throw new ArgumentOutOfRangeException(nameof(payment), payment, "Pago de la Pororoca desconocido: Stock o Seeds.");
            }
        }

        /// <summary>
        /// Parámetro <c>source</c> de <c>af_booster_used</c> (04 §11): <c>stock</c> o <c>seeds</c> (la Pororoca no tiene video ni
        /// uso gratis por nivel).
        /// </summary>
        public static string SourceId(ShufflePayment payment)
        {
            return payment == ShufflePayment.Stock ? "stock" : "seeds";
        }
    }
}
