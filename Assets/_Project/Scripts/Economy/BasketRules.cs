#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Economy
{
    /// <summary>Tipo de Canasta (06 §4.4; GDD.md §6.1 #13; 05 §2.1).</summary>
    public enum BasketKind
    {
        /// <summary>Sin Canasta.</summary>
        None = 0,

        /// <summary>Canasta de nivel (10, 20, 30…): semillas y, desde el 20, un comodín del ciclo.</summary>
        Normal = 1,

        /// <summary>Canasta grande de los niveles 100, 200…: 150 semillas + 1 Cuia extra + 1 Morpho + 3 Marea.</summary>
        Grand = 2,

        /// <summary>Canasta del hito 6 de la Crecida (06 §4.3): 50 semillas + el comodín de la próxima Canasta de nivel.</summary>
        Streak = 3,
    }

    /// <summary>
    /// Contenido de una Canasta, visible antes de abrirla y sin azar (06 §4.4; dirección 15). Es un valor: crearlo no asigna
    /// memoria. Se paga con <see cref="Wallet.Apply"/> (<see cref="AppendTransactions"/>).
    /// </summary>
    public readonly struct BasketContents : IEquatable<BasketContents>
    {
        /// <summary>Crea el contenido.</summary>
        public BasketContents(BasketKind kind, int level, int seeds, StockDelta stock)
        {
            if (seeds < 0 || stock.HasNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(seeds), "Una Canasta solo suma.");
            }

            Kind = kind;
            Level = level;
            Seeds = seeds;
            Stock = stock;
        }

        /// <summary>Sin Canasta.</summary>
        public static BasketContents None => default;

        /// <summary>Tipo.</summary>
        public BasketKind Kind { get; }

        /// <summary>
        /// Nivel de la Canasta: el ganado para <see cref="BasketKind.Normal"/> y <see cref="BasketKind.Grand"/>; para
        /// <see cref="BasketKind.Streak"/>, el de la Canasta de nivel cuyo comodín trae (Q-C3, Q-C5). 0 sin Canasta.
        /// </summary>
        public int Level { get; }

        /// <summary>Semillas.</summary>
        public int Seeds { get; }

        /// <summary>Comodines que entran al stock.</summary>
        public StockDelta Stock { get; }

        /// <summary>No hay Canasta.</summary>
        public bool IsNone => Kind == BasketKind.None;

        /// <summary>Copia con <paramref name="seeds"/> semillas (el tope de la Crecida puede recortar las de la Canasta del hito 6).</summary>
        public BasketContents WithSeeds(int seeds)
        {
            return new BasketContents(Kind, Level, seeds, Stock);
        }

        /// <summary>
        /// Añade a <paramref name="into"/> las transacciones que la pagan: <see cref="Transaction.Earn"/> con
        /// <paramref name="source"/> (<see cref="SeedSource.Basket"/> para la de nivel; <see cref="SeedSource.Streak"/> para la
        /// del hito 6, cuyas semillas cuentan en el tope de la Crecida: Q-C13) si hay semillas, y un
        /// <see cref="Transaction.GrantStock"/> por cada comodín, en el orden de <see cref="BoosterKind"/>.
        /// </summary>
        public void AppendTransactions(List<Transaction> into, SeedSource source)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            if (IsNone)
            {
                return;
            }

            if (Seeds > 0)
            {
                into.Add(Transaction.Earn(source, Seeds));
            }

            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                int units = Stock.Get((BoosterKind)k);
                if (units > 0)
                {
                    into.Add(Transaction.GrantStock((BoosterKind)k, units));
                }
            }
        }

        /// <summary>Las transacciones de <see cref="AppendTransactions"/> en una lista nueva.</summary>
        public List<Transaction> ToTransactions(SeedSource source = SeedSource.Basket)
        {
            var list = new List<Transaction>(4);
            AppendTransactions(list, source);
            return list;
        }

        /// <summary>
        /// <c>contents_json</c> de <c>af_basket_open</c> y <c>af_streak_milestone</c> (08 §8.2), estable [P]:
        /// <c>{"kind":"normal","seeds":50,"undo":3}</c>, con <c>kind</c> ∈ {<c>normal</c>, <c>grand</c>, <c>streak</c>},
        /// <c>seeds</c> siempre y después solo los comodines con unidades, en el orden <c>undo</c>, <c>hint</c>, <c>reveal</c>,
        /// <c>extra_cuia</c>, <c>shuffle</c>. Sin Canasta: <c>{}</c>.
        /// </summary>
        public string ContentsJson()
        {
            if (IsNone)
            {
                return "{}";
            }

            var sb = new StringBuilder("{\"kind\":\"");
            sb.Append(KindId(Kind)).Append("\",\"seeds\":").Append(Seeds.ToString(CultureInfo.InvariantCulture));
            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                int units = Stock.Get((BoosterKind)k);
                if (units > 0)
                {
                    sb.Append(",\"").Append(((BoosterKind)k).ToId()).Append("\":").Append(units.ToString(CultureInfo.InvariantCulture));
                }
            }

            return sb.Append('}').ToString();
        }

        /// <summary><c>normal</c>, <c>grand</c> o <c>streak</c> (<c>contents_json</c> [P]).</summary>
        public static string KindId(BasketKind kind)
        {
            switch (kind)
            {
                case BasketKind.Normal: return "normal";
                case BasketKind.Grand: return "grand";
                case BasketKind.Streak: return "streak";
                case BasketKind.None: return "none";
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        /// <inheritdoc />
        public bool Equals(BasketContents other)
        {
            return Kind == other.Kind && Level == other.Level && Seeds == other.Seeds && Stock.Equals(other.Stock);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is BasketContents other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (((((int)Kind * 397) ^ Level) * 397) ^ Seeds) * 397 ^ Stock.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ContentsJson();
        }
    }

    /// <summary>
    /// Reglas de la Canasta (06 §4.4; 05 §2.1; 04 §3; GDD.md §6.1 #13): qué trae cada una, deducido del nivel, sin estado
    /// propio (Q-M9 (a)) y sin azar. Las consultas no asignan memoria.
    /// </summary>
    public static class BasketRules
    {
        /// <summary>Cada cuántos niveles de campaña hay Canasta (06 §4.4).</summary>
        public const int Every = 10;

        /// <summary>Cada cuántos niveles la Canasta es la grande (06 §4.4; GDD.md §6.1 #13).</summary>
        public const int GrandEvery = 100;

        /// <summary>La Canasta de este nivel trae solo semillas (06 §4.4: «nivel 10 → 50 semillas»).</summary>
        public const int SeedsOnlyLevel = 10;

        /// <summary>Primer nivel con comodín del ciclo (06 §4.4: «niveles 20, 30, 40, 50, 60 y se repite»).</summary>
        public const int FirstCycleLevel = 20;

        /// <summary>Canasta grande [P] (Q-C12 (a); GDD.md §6.1 #13, decidida el 2026-10-06): semillas.</summary>
        public const int GrandSeeds = 150;

        /// <summary>Canasta grande: Cuias extra.</summary>
        public const int GrandExtraCuia = 1;

        /// <summary>Canasta grande: Morphos.</summary>
        public const int GrandHint = 1;

        /// <summary>Canasta grande: Mareas.</summary>
        public const int GrandUndo = 3;

        /// <summary>Contenido de la Canasta grande (sin nivel).</summary>
        public static StockDelta GrandStock =>
            StockDelta.Of(BoosterKind.Undo, GrandUndo).Plus(BoosterKind.Hint, GrandHint).Plus(BoosterKind.ExtraCuia, GrandExtraCuia);

        /// <summary>
        /// La Canasta que se gana al superar el nivel de campaña <paramref name="level"/>:
        /// <list type="bullet">
        /// <item>ninguna si no es múltiplo de 10 o está por debajo de <paramref name="unlockLevel"/> (<c>live_unlock_level_daily</c>);</item>
        /// <item>en el 10, solo <c>eco_basket_seeds</c>;</item>
        /// <item>en 100, 200…, la grande, que sustituye a la normal y ocupa su ranura del ciclo (Q-C4 (a));</item>
        /// <item>en el resto, <c>eco_basket_seeds</c> + <c>eco_basket_cycle[(L/10 − 2) mod n]</c>: 20 → 3 Marea, 30 → Morpho,
        /// 40 → Cuia extra, 50 → 3 Marea, 60 → Morpho, 70 → 3 Marea… (04 §3: Cuia extra en 40 y 90, Morpho en 30, 60, 80).</item>
        /// </list>
        /// Solo campaña: el Reto del río no da Canasta (lo decide quien llama).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> &lt; 1.</exception>
        /// <exception cref="InvalidOperationException">El ciclo de <paramref name="config"/> está vacío o tiene una ficha desconocida.</exception>
        public static BasketContents For(int level, int unlockLevel, BasketConfig config)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Los niveles empiezan en 1.");
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (level % Every != 0 || level < unlockLevel)
            {
                return BasketContents.None;
            }

            if (level == SeedsOnlyLevel)
            {
                return new BasketContents(BasketKind.Normal, level, config.EcoBasketSeeds, StockDelta.None);
            }

            if (level % GrandEvery == 0)
            {
                return new BasketContents(BasketKind.Grand, level, GrandSeeds, GrandStock);
            }

            return new BasketContents(BasketKind.Normal, level, config.EcoBasketSeeds, CycleStock(level, config));
        }

        /// <summary>
        /// Canasta del hito 6 de la Crecida (06 §4.3; Q-C3 (b) y Q-C5 (a)): <c>eco_basket_seeds</c> + el comodín del ciclo de la
        /// <b>próxima Canasta de nivel</b>, la del primer múltiplo de 10 mayor que <paramref name="maxLevel"/> (el nivel máximo
        /// superado tras esta victoria; como mínimo el 20). Si esa ranura es la de una Canasta grande, trae el comodín del ciclo
        /// en esa ranura (<c>undo3</c> en el 100), nunca la grande. Se deduce de <c>max_level</c>: sin estado nuevo. Las semillas
        /// las recorta después el tope diario de la Crecida (<see cref="BasketContents.WithSeeds"/>).
        /// </summary>
        public static BasketContents HitSix(int maxLevel, BasketConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            int slot = NextLevelSlot(maxLevel);
            return new BasketContents(BasketKind.Streak, slot, config.EcoBasketSeeds, CycleStock(slot, config));
        }

        /// <summary>Nivel de la próxima Canasta de nivel tras <paramref name="maxLevel"/> (múltiplo de 10 mayor; como mínimo el 20).</summary>
        public static int NextLevelSlot(int maxLevel)
        {
            int next = (Math.Max(0, maxLevel) / Every + 1) * Every;
            return Math.Max(FirstCycleLevel, next);
        }

        /// <summary>Índice de <c>eco_basket_cycle</c> que toca en el nivel <paramref name="level"/> (múltiplo de 10, ≥ 20).</summary>
        public static int CycleIndex(int level, int cycleLength)
        {
            if (cycleLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cycleLength));
            }

            int i = (level / Every - FirstCycleLevel / Every) % cycleLength;
            return i < 0 ? i + cycleLength : i;
        }

        private static StockDelta CycleStock(int level, BasketConfig config)
        {
            IReadOnlyList<string>? cycle = config.EcoBasketCycle;
            if (cycle == null || cycle.Count == 0)
            {
                throw new InvalidOperationException("BasketRules: eco_basket_cycle vacío (BasketConfig.Validate).");
            }

            string token = cycle[CycleIndex(level, cycle.Count)];
            if (!BasketConfig.TryParseToken(token, out BoosterKind kind, out int units))
            {
                throw new InvalidOperationException("BasketRules: ficha de eco_basket_cycle desconocida (BasketConfig.Validate).");
            }

            return StockDelta.Of(kind, units);
        }
    }
}
