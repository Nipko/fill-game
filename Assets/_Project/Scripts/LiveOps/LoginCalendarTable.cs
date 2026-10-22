#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Economy;
using AF.Levels.Json;

namespace AF.LiveOps
{
    /// <summary>
    /// Opción de una casilla de la Bitácora del río (D-033, añadido del 2026-10-20, punto 2: «en orden fijo semillas / comodín /
    /// mezcla»). El id es el <c>choice</c> de <c>af_calendar_claim</c> [P] (D-033: <c>seeds</c>/<c>booster</c>/<c>mix</c>).
    /// </summary>
    public enum LoginCalendarChoice
    {
        /// <summary><c>seeds</c>: solo semillas.</summary>
        Seeds = 0,

        /// <summary><c>booster</c>: comodines (con semillas en los hitos).</summary>
        Booster = 1,

        /// <summary><c>mix</c>: comodines y semillas.</summary>
        Mix = 2,
    }

    /// <summary>
    /// Lo que va fijo en una casilla, igual en sus tres opciones (D-033, punto 2: «En los hitos 7, 14, 21 y 30 la Canasta, la
    /// Canasta grande y el marco van fijos en las tres opciones»). Banderas.
    /// </summary>
    [Flags]
    public enum LoginCalendarExtras
    {
        /// <summary>Nada fijo.</summary>
        None = 0,

        /// <summary>Una Canasta (días 7 y 21; 06 §4.2).</summary>
        Basket = 1,

        /// <summary>La Canasta grande (día 30; 06 §4.2, regla 2; GDD.md §6.1 #13).</summary>
        GrandBasket = 2,

        /// <summary>El marco de país (día 30; 06 §4.2, regla 4; Q-M6: se guarda como derecho y se enseña en la 1.1).</summary>
        Frame = 4,
    }

    /// <summary>
    /// Premio de una opción (o el canon) de la Bitácora: semillas y comodines al stock. Es un valor: crearlo no asigna memoria.
    /// Solo usa Marea, Morpho, Luciérnaga y Cuia extra (la tabla de 06 §4.2 y de D-033 no lleva Pororoca). Una unidad de
    /// <see cref="BoosterKind.Undo"/> es una Marea, la misma unidad que el stock (Q-B3 [P]).
    /// </summary>
    public readonly struct LoginCalendarReward : IEquatable<LoginCalendarReward>
    {
        /// <summary>Crea el premio.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Alguna cifra es negativa o lleva Pororoca.</exception>
        public LoginCalendarReward(int seeds, StockDelta stock)
        {
            if (seeds < 0 || stock.HasNegative || stock.Get(BoosterKind.Shuffle) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stock), "Un premio de la Bitácora solo suma semillas, Marea, Morpho, Luciérnaga y Cuia extra.");
            }

            Seeds = seeds;
            Stock = stock;
        }

        /// <summary>Semillas.</summary>
        public int Seeds { get; }

        /// <summary>Comodines al stock.</summary>
        public StockDelta Stock { get; }

        /// <summary>No da nada.</summary>
        public bool IsEmpty => Seeds == 0 && Stock.IsZero;

        /// <summary>Solo semillas.</summary>
        public static LoginCalendarReward OfSeeds(int seeds)
        {
            return new LoginCalendarReward(seeds, StockDelta.None);
        }

        /// <summary><paramref name="units"/> unidades de <paramref name="kind"/> y <paramref name="seeds"/> semillas.</summary>
        public static LoginCalendarReward Of(BoosterKind kind, int units, int seeds = 0)
        {
            return new LoginCalendarReward(seeds, StockDelta.Of(kind, units));
        }

        /// <summary>Copia con <paramref name="units"/> unidades más de <paramref name="kind"/>.</summary>
        public LoginCalendarReward Plus(BoosterKind kind, int units)
        {
            return new LoginCalendarReward(Seeds, Stock.Plus(kind, units));
        }

        /// <summary>Copia con <paramref name="seeds"/> semillas más.</summary>
        public LoginCalendarReward PlusSeeds(int seeds)
        {
            return new LoginCalendarReward(checked(Seeds + seeds), Stock);
        }

        /// <summary>
        /// Valor en semillas-equivalentes (D-033, punto 1: «Marea = 30; cualquier otro comodín = 100», la tasa de
        /// <c>eco_calendar_locked_booster_seeds</c>): <c>reward_value</c> de <c>af_calendar_claim</c>.
        /// </summary>
        public int Value(LoginCalendarValueRate rate)
        {
            int other = Stock.Get(BoosterKind.Hint) + Stock.Get(BoosterKind.Reveal) + Stock.Get(BoosterKind.ExtraCuia);
            return checked(Seeds + (Stock.Get(BoosterKind.Undo) * rate.Undo) + (other * rate.Other));
        }

        /// <summary>
        /// JSON estable [P]: <c>{"seeds":60}</c>, <c>{"undo":2}</c>, <c>{"seeds":30,"undo":1}</c>: <c>seeds</c> si las hay y después
        /// los comodines con unidades, en el orden de <see cref="BoosterKind"/> (<c>undo</c>, <c>hint</c>, <c>reveal</c>,
        /// <c>extra_cuia</c>), con los ids de 04 §10.1. El mismo formato que lee <see cref="LoginCalendarTable.Parse"/>.
        /// </summary>
        public string ToJson()
        {
            var sb = new StringBuilder(32);
            AppendJson(sb);
            return sb.ToString();
        }

        /// <summary>Añade <see cref="ToJson"/> a <paramref name="sb"/>.</summary>
        public void AppendJson(StringBuilder sb)
        {
            if (sb == null)
            {
                throw new ArgumentNullException(nameof(sb));
            }

            sb.Append('{');
            bool first = true;
            if (Seeds > 0 || Stock.IsZero)
            {
                sb.Append("\"seeds\":").Append(Seeds.ToString(CultureInfo.InvariantCulture));
                first = false;
            }

            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                int units = Stock.Get((BoosterKind)k);
                if (units <= 0)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(',');
                }

                sb.Append('"').Append(((BoosterKind)k).ToId()).Append("\":").Append(units.ToString(CultureInfo.InvariantCulture));
                first = false;
            }

            sb.Append('}');
        }

        /// <inheritdoc />
        public bool Equals(LoginCalendarReward other)
        {
            return Seeds == other.Seeds && Stock.Equals(other.Stock);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is LoginCalendarReward other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (Seeds * 397) ^ Stock.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToJson();
        }
    }

    /// <summary>
    /// Tasa de valor de la Bitácora (D-033, punto 1): <c>eco_calendar_locked_booster_seeds</c> = <c>{"undo3":30,"other":100}</c>
    /// (08 §9). D-033 la lee como «Marea = 30; cualquier otro comodín = 100» por unidad, que es como cuadra su tabla de opciones
    /// (2 Marea = 60, 1 Marea + 30 = 60, 2 Marea + 40 = 100): <see cref="Undo"/> vale por cada Marea [P] (nota
    /// <c>docs/dev/bitacora.md</c>, Q-B3).
    /// </summary>
    public readonly struct LoginCalendarValueRate : IEquatable<LoginCalendarValueRate>
    {
        /// <summary>Crea la tasa.</summary>
        public LoginCalendarValueRate(int undo, int other)
        {
            Undo = undo;
            Other = other;
        }

        /// <summary>La de 08 §9: <c>{"undo3":30,"other":100}</c>.</summary>
        public static LoginCalendarValueRate Default => new LoginCalendarValueRate(30, 100);

        /// <summary><c>undo3</c>: semillas-equivalentes de una Marea.</summary>
        public int Undo { get; }

        /// <summary><c>other</c>: semillas-equivalentes de un Morpho, una Luciérnaga o una Cuia extra.</summary>
        public int Other { get; }

        /// <summary>Escribe la tasa con la sintaxis de 08 §9: <c>{"undo3":30,"other":100}</c>.</summary>
        public string Format()
        {
            return "{\"undo3\":" + Undo.ToString(CultureInfo.InvariantCulture) + ",\"other\":" + Other.ToString(CultureInfo.InvariantCulture) + "}";
        }

        /// <summary>
        /// Lee la sintaxis de 08 §9 (<c>{"undo3":30,"other":100}</c>, en cualquier orden y con espacios): las dos claves, enteros
        /// positivos y nada más. Si no, false y <see cref="Default"/>.
        /// </summary>
        public static bool TryParse(string? json, out LoginCalendarValueRate rate)
        {
            rate = Default;
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            JsonValue root;
            try
            {
                root = JsonParser.Parse(json!);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                return false;
            }

            if (root.Kind != JsonKind.Object || root.Members.Count != 2)
            {
                return false;
            }

            JsonValue? undo = root.Get("undo3");
            JsonValue? other = root.Get("other");
            if (undo == null || other == null || !TryPositive(undo, out int u) || !TryPositive(other, out int o))
            {
                return false;
            }

            rate = new LoginCalendarValueRate(u, o);
            return true;
        }

        /// <inheritdoc />
        public bool Equals(LoginCalendarValueRate other)
        {
            return Undo == other.Undo && Other == other.Other;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is LoginCalendarValueRate other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (Undo * 397) ^ Other;
        }

        private static bool TryPositive(JsonValue node, out int value)
        {
            value = 0;
            if (node.Kind != JsonKind.Number)
            {
                return false;
            }

            try
            {
                value = node.AsInt();
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                return false;
            }

            return value > 0;
        }
    }

    /// <summary>
    /// Una casilla de la Bitácora: su día (1-30), el canon (la tabla retocada de 06 §4.2 con D-033, punto 2, de respaldo y para
    /// <c>tools/econ_sim.py --calendar-pick canon</c>), sus tres opciones en orden fijo y lo que va fijo.
    /// </summary>
    public sealed class LoginCalendarCell
    {
        private readonly LoginCalendarReward[] _options;

        /// <summary>Crea la casilla.</summary>
        public LoginCalendarCell(int day, LoginCalendarReward canon, LoginCalendarReward seeds, LoginCalendarReward booster, LoginCalendarReward mix, LoginCalendarExtras extras)
        {
            if (day < 1 || day > LoginCalendarTable.Days)
            {
                throw new ArgumentOutOfRangeException(nameof(day), "Las casillas van de 1 a 30.");
            }

            Day = day;
            Canon = canon;
            _options = new[] { seeds, booster, mix };
            Extras = extras;
        }

        /// <summary>Día (1-30).</summary>
        public int Day { get; }

        /// <summary>El premio único de la tabla retocada (06 §4.2 con D-033, punto 2), sin lo fijo.</summary>
        public LoginCalendarReward Canon { get; }

        /// <summary>Lo que va fijo en las tres opciones.</summary>
        public LoginCalendarExtras Extras { get; }

        /// <summary>Es un hito (7, 14, 21 o 30): su casilla es más grande en la libreta (06 §4.2 «Pantalla»).</summary>
        public bool IsMilestone => LoginCalendarTable.IsMilestone(Day);

        /// <summary>La opción <paramref name="choice"/> tal como está en la tabla (sin sustituir comodines bloqueados).</summary>
        public LoginCalendarReward Option(LoginCalendarChoice choice)
        {
            int i = (int)choice;
            if (i < 0 || i >= LoginCalendarTable.Choices)
            {
                throw new ArgumentOutOfRangeException(nameof(choice));
            }

            return _options[i];
        }
    }

    /// <summary>
    /// Tabla de la Bitácora «1 de 3» (D-033, añadido del 2026-10-20, puntos 1-3; Q-M1 (a)): 30 casillas × 3 opciones de igual
    /// valor, más el canon. Vive en Remote Config como <c>eco_calendar_table_v2</c> [P] (D-033: «30 casillas × 3 opciones y el
    /// canon»; la v1 queda de respaldo en RC, pero esta build no la lee: Q-B4). <see cref="Default"/> es la tabla aprobada por el
    /// dueño, sin cambiar ni una cifra. Formato JSON [P] en <see cref="Format"/> y <see cref="Parse"/>.
    /// </summary>
    public sealed class LoginCalendarTable
    {
        /// <summary>Casillas por ciclo (06 §4.2).</summary>
        public const int Days = 30;

        /// <summary>Opciones por casilla (D-033: «1 de 3»).</summary>
        public const int Choices = 3;

        /// <summary>Total de semillas del canon por ciclo (06 §4.2: «3.000 semillas»; D-033, punto 2: «Los totales del ciclo no cambian»).</summary>
        public const int CanonSeedsPerCycle = 3000;

        private readonly LoginCalendarCell[] _cells;

        private LoginCalendarTable(LoginCalendarCell[] cells)
        {
            _cells = cells;
        }

        /// <summary>
        /// La tabla de D-033 (añadido del 2026-10-20, punto 2), con el canon retocado: 1, 3, 8, 9 y 15 → 60; 2, 10, 17, 19 y 27 →
        /// «1 Marea + 30» (añadido del 2026-10-21: vale 60, como sus opciones); 20, 26 y 28 → 100; 30 → 600 (+ Canasta grande +
        /// marco). Cada llamada devuelve otra instancia.
        /// </summary>
        public static LoginCalendarTable Default => BuildDefault();

        /// <summary>La casilla del día <paramref name="day"/> (1-30).</summary>
        public LoginCalendarCell Cell(int day)
        {
            if (day < 1 || day > Days)
            {
                throw new ArgumentOutOfRangeException(nameof(day), "Las casillas van de 1 a 30.");
            }

            return _cells[day - 1];
        }

        /// <summary>El día es un hito (7, 14, 21 o 30; 06 §4.2).</summary>
        public static bool IsMilestone(int day)
        {
            return day == 7 || day == 14 || day == 21 || day == 30;
        }

        /// <summary>Id de <paramref name="choice"/>: <c>seeds</c>, <c>booster</c> o <c>mix</c> (<c>choice</c> de <c>af_calendar_claim</c> [P]).</summary>
        public static string ChoiceId(LoginCalendarChoice choice)
        {
            switch (choice)
            {
                case LoginCalendarChoice.Seeds: return "seeds";
                case LoginCalendarChoice.Booster: return "booster";
                case LoginCalendarChoice.Mix: return "mix";
                default: throw new ArgumentOutOfRangeException(nameof(choice), choice, null);
            }
        }

        /// <summary>Total de semillas del canon en un ciclo (sin lo fijo).</summary>
        public int CanonSeeds()
        {
            int total = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                total += _cells[i].Canon.Seeds;
            }

            return total;
        }

        /// <summary>Unidades de <paramref name="kind"/> del canon en un ciclo (sin lo fijo).</summary>
        public int CanonUnits(BoosterKind kind)
        {
            int total = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                total += _cells[i].Canon.Stock.Get(kind);
            }

            return total;
        }

        /// <summary>Casillas con <paramref name="extra"/> fijo en un ciclo.</summary>
        public int CountExtras(LoginCalendarExtras extra)
        {
            int total = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                if ((_cells[i].Extras & extra) == extra)
                {
                    total++;
                }
            }

            return total;
        }

        /// <summary>
        /// Comprueba la forma (no las cifras de D-033: eso lo hacen los tests): 30 casillas en orden; en cada una, las tres
        /// opciones con premio, la de semillas solo con semillas, las tres distintas y del mismo valor con <paramref name="rate"/>
        /// (D-033: «3 premios de igual valor»), y Canasta y Canasta grande solo en hitos y nunca las dos en la misma casilla.
        /// </summary>
        /// <exception cref="InvalidOperationException">La tabla no tiene esa forma.</exception>
        public void Validate(LoginCalendarValueRate rate)
        {
            Require(_cells.Length == Days, "debe tener 30 casillas");
            for (int i = 0; i < _cells.Length; i++)
            {
                LoginCalendarCell cell = _cells[i];
                string at = "casilla " + (i + 1).ToString(CultureInfo.InvariantCulture);
                Require(cell.Day == i + 1, at + ": día fuera de orden");
                LoginCalendarReward seeds = cell.Option(LoginCalendarChoice.Seeds);
                LoginCalendarReward booster = cell.Option(LoginCalendarChoice.Booster);
                LoginCalendarReward mix = cell.Option(LoginCalendarChoice.Mix);
                Require(!seeds.IsEmpty && !booster.IsEmpty && !mix.IsEmpty && !cell.Canon.IsEmpty, at + ": opción vacía");
                Require(seeds.Stock.IsZero, at + ": la opción de semillas solo lleva semillas");
                Require(!booster.Stock.IsZero && !mix.Stock.IsZero, at + ": la de comodín y la mezcla llevan algún comodín");
                Require(!seeds.Equals(booster) && !seeds.Equals(mix) && !booster.Equals(mix), at + ": nunca dos opciones iguales");
                int value = seeds.Value(rate);
                Require(booster.Value(rate) == value && mix.Value(rate) == value, at + ": las tres opciones valen lo mismo");
                bool basket = (cell.Extras & LoginCalendarExtras.Basket) != 0;
                bool grand = (cell.Extras & LoginCalendarExtras.GrandBasket) != 0;
                Require(!(basket && grand), at + ": Canasta y Canasta grande a la vez");
                Require((!basket && !grand && (cell.Extras & LoginCalendarExtras.Frame) == 0) || IsMilestone(cell.Day), at + ": lo fijo solo va en los hitos");
            }
        }

        /// <summary>
        /// Escribe la tabla con el formato de <c>eco_calendar_table_v2</c> [P]:
        /// <c>{"days":[{"day":1,"canon":{"seeds":60},"options":[{"seeds":60},{"undo":2},{"seeds":30,"undo":1}],"extras":[]},…]}</c>,
        /// con <c>extras</c> ⊂ {<c>basket</c>, <c>grand_basket</c>, <c>frame</c>}.
        /// </summary>
        public string Format()
        {
            var sb = new StringBuilder(4096);
            sb.Append("{\"days\":[");
            for (int i = 0; i < _cells.Length; i++)
            {
                LoginCalendarCell cell = _cells[i];
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append("{\"day\":").Append(cell.Day.ToString(CultureInfo.InvariantCulture)).Append(",\"canon\":");
                cell.Canon.AppendJson(sb);
                sb.Append(",\"options\":[");
                for (int c = 0; c < Choices; c++)
                {
                    if (c > 0)
                    {
                        sb.Append(',');
                    }

                    cell.Option((LoginCalendarChoice)c).AppendJson(sb);
                }

                sb.Append("],\"extras\":[");
                bool first = true;
                AppendExtra(sb, cell.Extras, LoginCalendarExtras.Basket, "basket", ref first);
                AppendExtra(sb, cell.Extras, LoginCalendarExtras.GrandBasket, "grand_basket", ref first);
                AppendExtra(sb, cell.Extras, LoginCalendarExtras.Frame, "frame", ref first);
                sb.Append("]}");
            }

            return sb.Append("]}").ToString();
        }

        /// <summary>
        /// Lee <c>eco_calendar_table_v2</c> (el formato de <see cref="Format"/>) y lo valida con <paramref name="rate"/>
        /// (<see cref="Validate"/>).
        /// </summary>
        /// <exception cref="FormatException">El JSON no tiene esa forma o la tabla no es válida.</exception>
        public static LoginCalendarTable Parse(string json, LoginCalendarValueRate rate)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            JsonValue root;
            try
            {
                root = JsonParser.Parse(json);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                throw new FormatException("eco_calendar_table_v2: " + e.Message, e);
            }

            JsonValue? days = root.Kind == JsonKind.Object ? root.Get("days") : null;
            if (days == null || days.Kind != JsonKind.Array || days.Items.Count != Days)
            {
                throw new FormatException("eco_calendar_table_v2: se esperaba {\"days\":[…30 casillas…]}.");
            }

            var cells = new LoginCalendarCell[Days];
            try
            {
                for (int i = 0; i < Days; i++)
                {
                    cells[i] = ReadCell(days.Items[i], i + 1);
                }
            }
            catch (Exception e) when (!(e is FormatException) && !(e is OutOfMemoryException))
            {
                // Un número que no cabe en int, un premio con Pororoca o una cifra negativa: el mismo error de formato.
                throw new FormatException("eco_calendar_table_v2: " + e.Message, e);
            }

            var table = new LoginCalendarTable(cells);
            try
            {
                table.Validate(rate);
            }
            catch (InvalidOperationException e)
            {
                throw new FormatException(e.Message, e);
            }

            return table;
        }

        /// <summary>
        /// Lee <paramref name="json"/> sin lanzar: si no es válido (o es null) devuelve <see cref="Default"/> y el motivo en
        /// <paramref name="error"/> (la tabla de RC nunca deja la Bitácora vacía).
        /// </summary>
        public static LoginCalendarTable ParseOrDefault(string? json, LoginCalendarValueRate rate, out string? error)
        {
            error = null;
            if (string.IsNullOrEmpty(json))
            {
                return Default;
            }

            try
            {
                return Parse(json!, rate);
            }
            catch (FormatException e)
            {
                error = e.Message;
                return Default;
            }
        }

        private static LoginCalendarCell ReadCell(JsonValue item, int expected)
        {
            if (item.Kind != JsonKind.Object)
            {
                throw new FormatException("eco_calendar_table_v2: la casilla " + expected + " no es un objeto.");
            }

            JsonValue? day = item.Get("day");
            JsonValue? options = item.Get("options");
            if (day == null || options == null || options.Kind != JsonKind.Array || options.Items.Count != Choices)
            {
                throw new FormatException("eco_calendar_table_v2: la casilla " + expected + " necesita day y 3 options.");
            }

            return new LoginCalendarCell(
                RequireDay(day, expected),
                ReadReward(item.Get("canon"), expected),
                ReadReward(options.Items[0], expected),
                ReadReward(options.Items[1], expected),
                ReadReward(options.Items[2], expected),
                ReadExtras(item.Get("extras"), expected));
        }

        private static void AppendExtra(StringBuilder sb, LoginCalendarExtras extras, LoginCalendarExtras flag, string id, ref bool first)
        {
            if ((extras & flag) == 0)
            {
                return;
            }

            if (!first)
            {
                sb.Append(',');
            }

            sb.Append('"').Append(id).Append('"');
            first = false;
        }

        private static int RequireDay(JsonValue day, int expected)
        {
            int value = day.AsInt();
            if (value != expected)
            {
                throw new FormatException("eco_calendar_table_v2: la casilla " + expected + " dice day = " + value + ".");
            }

            return value;
        }

        private static LoginCalendarReward ReadReward(JsonValue? node, int day)
        {
            if (node == null || node.Kind != JsonKind.Object)
            {
                throw new FormatException("eco_calendar_table_v2: premio de la casilla " + day + " no es un objeto.");
            }

            int seeds = 0;
            StockDelta stock = StockDelta.None;
            IReadOnlyList<KeyValuePair<string, JsonValue>> members = node.Members;
            for (int m = 0; m < members.Count; m++)
            {
                string key = members[m].Key;
                int units = members[m].Value.AsInt();
                if (units < 0)
                {
                    throw new FormatException("eco_calendar_table_v2: cifra negativa en la casilla " + day + ".");
                }

                if (key == "seeds")
                {
                    seeds = units;
                }
                else if (TryBooster(key, out BoosterKind kind))
                {
                    stock = stock.Plus(kind, units);
                }
                else
                {
                    throw new FormatException("eco_calendar_table_v2: clave '" + key + "' desconocida en la casilla " + day + ".");
                }
            }

            return new LoginCalendarReward(seeds, stock);
        }

        // Ids de 04 §10.1 de los comodines que admite la Bitácora (sin Pororoca).
        private static bool TryBooster(string id, out BoosterKind kind)
        {
            switch (id)
            {
                case "undo": kind = BoosterKind.Undo; return true;
                case "hint": kind = BoosterKind.Hint; return true;
                case "reveal": kind = BoosterKind.Reveal; return true;
                case "extra_cuia": kind = BoosterKind.ExtraCuia; return true;
                default: kind = BoosterKind.Undo; return false;
            }
        }

        private static LoginCalendarExtras ReadExtras(JsonValue? node, int day)
        {
            if (node == null)
            {
                return LoginCalendarExtras.None;
            }

            if (node.Kind != JsonKind.Array)
            {
                throw new FormatException("eco_calendar_table_v2: extras de la casilla " + day + " no es una lista.");
            }

            LoginCalendarExtras extras = LoginCalendarExtras.None;
            for (int i = 0; i < node.Items.Count; i++)
            {
                string id = node.Items[i].AsString();
                switch (id)
                {
                    case "basket": extras |= LoginCalendarExtras.Basket; break;
                    case "grand_basket": extras |= LoginCalendarExtras.GrandBasket; break;
                    case "frame": extras |= LoginCalendarExtras.Frame; break;
                    default: throw new FormatException("eco_calendar_table_v2: extra '" + id + "' desconocido en la casilla " + day + ".");
                }
            }

            return extras;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("eco_calendar_table_v2: " + message + ".");
            }
        }

        // D-033, añadido del 2026-10-20, punto 2 (tabla de opciones) y retoque del canon. Opciones en orden semillas / comodín /
        // mezcla. Marea = Undo, Morpho = Hint, Luciérnaga = Reveal, Cuia extra = ExtraCuia.
        private static LoginCalendarTable BuildDefault()
        {
            var cells = new LoginCalendarCell[Days];
            for (int day = 1; day <= Days; day++)
            {
                cells[day - 1] = DefaultCell(day);
            }

            return new LoginCalendarTable(cells);
        }

        private static LoginCalendarCell DefaultCell(int day)
        {
            const BoosterKind Undo = BoosterKind.Undo;
            const BoosterKind Hint = BoosterKind.Hint;
            const BoosterKind Cuia = BoosterKind.ExtraCuia;
            const BoosterKind Reveal = BoosterKind.Reveal;
            switch (day)
            {
                // Valor 60: 60 | 2 Marea | 1 Marea + 30. Canon: 60 (1, 3, 8, 9, 15) o 1 Marea + 30 (2, 10, 17, 19, 27; D-033, añadido del 2026-10-21).
                case 1: case 3: case 8: case 9: case 15:
                    return new LoginCalendarCell(day, S(60), S(60), LoginCalendarReward.Of(Undo, 2), LoginCalendarReward.Of(Undo, 1, 30), LoginCalendarExtras.None);
                case 2: case 10: case 17: case 19: case 27:
                    return new LoginCalendarCell(day, LoginCalendarReward.Of(Undo, 1, 30), S(60), LoginCalendarReward.Of(Undo, 2), LoginCalendarReward.Of(Undo, 1, 30), LoginCalendarExtras.None);

                // Valor 100: 100 | 1 Morpho | 2 Marea + 40. Canon: 100 (5, 11, 13, 16, 18, 20, 22, 24, 26, 28) o 1 Morpho (4, 12, 25).
                case 5: case 11: case 13: case 16: case 18: case 20: case 22: case 24: case 26: case 28:
                    return new LoginCalendarCell(day, S(100), S(100), LoginCalendarReward.Of(Hint, 1), LoginCalendarReward.Of(Undo, 2, 40), LoginCalendarExtras.None);
                case 4: case 12: case 25:
                    return new LoginCalendarCell(day, LoginCalendarReward.Of(Hint, 1), S(100), LoginCalendarReward.Of(Hint, 1), LoginCalendarReward.Of(Undo, 2, 40), LoginCalendarExtras.None);

                // 6 y 23: 100 | 1 Cuia extra | 2 Marea + 40. Canon: 1 Cuia extra.
                case 6: case 23:
                    return new LoginCalendarCell(day, LoginCalendarReward.Of(Cuia, 1), S(100), LoginCalendarReward.Of(Cuia, 1), LoginCalendarReward.Of(Undo, 2, 40), LoginCalendarExtras.None);

                // 29: 100 | 1 Luciérnaga | 1 Morpho. Canon: 1 Luciérnaga.
                case 29:
                    return new LoginCalendarCell(day, LoginCalendarReward.Of(Reveal, 1), S(100), LoginCalendarReward.Of(Reveal, 1), LoginCalendarReward.Of(Hint, 1), LoginCalendarExtras.None);

                // 7: 250 + Canasta | 250 | 2 Morpho + 50 | 5 Marea + 100.
                case 7:
                    return new LoginCalendarCell(day, S(250), S(250), LoginCalendarReward.Of(Hint, 2, 50), LoginCalendarReward.Of(Undo, 5, 100), LoginCalendarExtras.Basket);

                // 14: 300 + 1 Cuia extra (canon) | 400 | 300 + 1 Cuia extra | 2 Morpho + 1 Cuia extra + 100.
                case 14:
                    return new LoginCalendarCell(day, LoginCalendarReward.Of(Cuia, 1, 300), S(400), LoginCalendarReward.Of(Cuia, 1, 300),
                        LoginCalendarReward.Of(Hint, 2, 100).Plus(Cuia, 1), LoginCalendarExtras.None);

                // 21: 400 + Canasta | 400 | 2 Cuia extra + 2 Morpho | 1 Cuia extra + 1 Morpho + 200.
                case 21:
                    return new LoginCalendarCell(day, S(400), S(400), LoginCalendarReward.Of(Cuia, 2).Plus(Hint, 2),
                        LoginCalendarReward.Of(Cuia, 1, 200).Plus(Hint, 1), LoginCalendarExtras.Basket);

                // 30: 600 + Canasta grande + marco | 600 | 2 Cuia extra + 1 Morpho + 300 | 3 Morpho + 300.
                case 30:
                    return new LoginCalendarCell(day, S(600), S(600), LoginCalendarReward.Of(Cuia, 2, 300).Plus(Hint, 1),
                        LoginCalendarReward.Of(Hint, 3, 300), LoginCalendarExtras.GrandBasket | LoginCalendarExtras.Frame);

                default:
                    throw new ArgumentOutOfRangeException(nameof(day));
            }
        }

        private static LoginCalendarReward S(int seeds)
        {
            return LoginCalendarReward.OfSeeds(seeds);
        }
    }
}
