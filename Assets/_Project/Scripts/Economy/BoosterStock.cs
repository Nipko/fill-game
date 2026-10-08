#nullable enable
using System;
using AF.Save;

namespace AF.Economy
{
    /// <summary>
    /// Comodines con stock consumible (04 §1.1 y §10.1: <c>stock.undo</c>, <c>hint</c>, <c>reveal</c>, <c>extra_cuia</c>,
    /// <c>shuffle</c>). El Salto del Boto y la Cuia de rescate no tienen stock: se pagan en el momento.
    /// </summary>
    public enum BoosterKind
    {
        /// <summary>Marea · Deshacer (<c>undo</c>).</summary>
        Undo = 0,

        /// <summary>Morpho · Pista (<c>hint</c>).</summary>
        Hint = 1,

        /// <summary>Luciérnaga · Revelar (<c>reveal</c>).</summary>
        Reveal = 2,

        /// <summary>Cuia extra · +1 cuia (<c>extra_cuia</c>).</summary>
        ExtraCuia = 3,

        /// <summary>Pororoca · Barajar (<c>shuffle</c>).</summary>
        Shuffle = 4,
    }

    /// <summary>Identificadores de <see cref="BoosterKind"/> en el guardado y en la analítica (04 §10.1 y §11).</summary>
    public static class BoosterKinds
    {
        /// <summary>Número de tipos.</summary>
        public const int Count = 5;

        /// <summary><c>undo</c>, <c>hint</c>, <c>reveal</c>, <c>extra_cuia</c> o <c>shuffle</c>.</summary>
        public static string ToId(this BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Undo: return "undo";
                case BoosterKind.Hint: return "hint";
                case BoosterKind.Reveal: return "reveal";
                case BoosterKind.ExtraCuia: return "extra_cuia";
                case BoosterKind.Shuffle: return "shuffle";
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        internal static int Read(StockData stock, BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Undo: return stock.Undo;
                case BoosterKind.Hint: return stock.Hint;
                case BoosterKind.Reveal: return stock.Reveal;
                case BoosterKind.ExtraCuia: return stock.ExtraCuia;
                case BoosterKind.Shuffle: return stock.Shuffle;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        internal static void Write(StockData stock, BoosterKind kind, int value)
        {
            switch (kind)
            {
                case BoosterKind.Undo: stock.Undo = value; break;
                case BoosterKind.Hint: stock.Hint = value; break;
                case BoosterKind.Reveal: stock.Reveal = value; break;
                case BoosterKind.ExtraCuia: stock.ExtraCuia = value; break;
                case BoosterKind.Shuffle: stock.Shuffle = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }

    /// <summary>Cambio de unidades de stock por tipo de comodín (valor inmutable).</summary>
    public readonly struct StockDelta : IEquatable<StockDelta>
    {
        private readonly int _undo;
        private readonly int _hint;
        private readonly int _reveal;
        private readonly int _extraCuia;
        private readonly int _shuffle;

        private StockDelta(int undo, int hint, int reveal, int extraCuia, int shuffle)
        {
            _undo = undo;
            _hint = hint;
            _reveal = reveal;
            _extraCuia = extraCuia;
            _shuffle = shuffle;
        }

        /// <summary>Sin cambios.</summary>
        public static StockDelta None => default;

        /// <summary>Todos los tipos a 0.</summary>
        public bool IsZero => _undo == 0 && _hint == 0 && _reveal == 0 && _extraCuia == 0 && _shuffle == 0;

        /// <summary>Hay alguna unidad negativa.</summary>
        public bool HasNegative => _undo < 0 || _hint < 0 || _reveal < 0 || _extraCuia < 0 || _shuffle < 0;

        /// <summary><paramref name="units"/> unidades de <paramref name="kind"/> y nada más.</summary>
        public static StockDelta Of(BoosterKind kind, int units)
        {
            return default(StockDelta).Plus(kind, units);
        }

        /// <summary>Unidades de <paramref name="kind"/>.</summary>
        public int Get(BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Undo: return _undo;
                case BoosterKind.Hint: return _hint;
                case BoosterKind.Reveal: return _reveal;
                case BoosterKind.ExtraCuia: return _extraCuia;
                case BoosterKind.Shuffle: return _shuffle;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        /// <summary>Copia con <paramref name="units"/> unidades más de <paramref name="kind"/>.</summary>
        public StockDelta Plus(BoosterKind kind, int units)
        {
            switch (kind)
            {
                case BoosterKind.Undo: return new StockDelta(checked(_undo + units), _hint, _reveal, _extraCuia, _shuffle);
                case BoosterKind.Hint: return new StockDelta(_undo, checked(_hint + units), _reveal, _extraCuia, _shuffle);
                case BoosterKind.Reveal: return new StockDelta(_undo, _hint, checked(_reveal + units), _extraCuia, _shuffle);
                case BoosterKind.ExtraCuia: return new StockDelta(_undo, _hint, _reveal, checked(_extraCuia + units), _shuffle);
                case BoosterKind.Shuffle: return new StockDelta(_undo, _hint, _reveal, _extraCuia, checked(_shuffle + units));
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        /// <inheritdoc />
        public bool Equals(StockDelta other)
        {
            return _undo == other._undo && _hint == other._hint && _reveal == other._reveal
                && _extraCuia == other._extraCuia && _shuffle == other._shuffle;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is StockDelta other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _undo;
                hash = (hash * 397) ^ _hint;
                hash = (hash * 397) ^ _reveal;
                hash = (hash * 397) ^ _extraCuia;
                return (hash * 397) ^ _shuffle;
            }
        }
    }

    /// <summary>
    /// Vista de solo lectura del stock de comodines del perfil (04 §1.1). Se cambia con <see cref="Wallet.Apply"/>
    /// (<see cref="Transaction.GrantStock"/>, <see cref="Transaction.UseStock"/>, la recarga de Marea o una compra).
    /// </summary>
    public sealed class BoosterStock
    {
        private readonly StockData _data;

        internal BoosterStock(StockData data)
        {
            _data = data;
        }

        /// <summary>Unidades de <paramref name="kind"/> en stock.</summary>
        public int Get(BoosterKind kind)
        {
            return BoosterKinds.Read(_data, kind);
        }

        /// <summary>Hay al menos una unidad de <paramref name="kind"/>.</summary>
        public bool Has(BoosterKind kind)
        {
            return Get(kind) > 0;
        }
    }
}
