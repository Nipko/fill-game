#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Levels
{
    /// <summary>Tipo de un tramo de orilla del Mapa v2 (<c>banks[].kind</c>; D-069, M2-1; propuesta del Mapa v2 §3.1, capa 2).</summary>
    public enum RiverBankKind
    {
        /// <summary>Playa de arena en la orilla de dentro de un meandro.</summary>
        Beach = 0,

        /// <summary>Barranco de tierra roja en la orilla de fuera.</summary>
        Bluff = 1,
    }

    /// <summary>
    /// Un tramo de orilla de <c>af-river-map/2</c> (<c>banks[]</c>; contrato 29; D-069, M2-1): en la línea <see cref="Line"/>, de
    /// <see cref="S0"/> a <see cref="S1"/>, en la orilla izquierda o derecha mirando río abajo (s creciente) y de
    /// <see cref="WidthDp"/> dp en la vista Mundo [P].
    /// </summary>
    public readonly struct RiverBank
    {
        /// <summary>Crea el tramo (el lector ya lo validó).</summary>
        public RiverBank(int line, float s0, float s1, bool left, RiverBankKind kind, float widthDp)
        {
            Line = line;
            S0 = s0;
            S1 = s1;
            Left = left;
            Kind = kind;
            WidthDp = widthDp;
        }

        /// <summary>Índice de la línea.</summary>
        public int Line { get; }

        /// <summary>Principio del tramo (fracción de la línea).</summary>
        public float S0 { get; }

        /// <summary>Final del tramo.</summary>
        public float S1 { get; }

        /// <summary>Orilla izquierda mirando río abajo (si no, la derecha).</summary>
        public bool Left { get; }

        /// <summary>Playa o barranco.</summary>
        public RiverBankKind Kind { get; }

        /// <summary>Ancho en la vista Mundo (dp).</summary>
        public float WidthDp { get; }
    }

    /// <summary>Tipo de un punto de vegetación (<c>veg.info</c>, bits 0-1).</summary>
    public enum RiverVegKind
    {
        /// <summary>Copa de la selva.</summary>
        Canopy = 0,

        /// <summary>Palmera (açaí, aguaje o, en la sabana, burití).</summary>
        Palm = 1,

        /// <summary>Árbol en flor (ipê amarillo o rosa).</summary>
        Flower = 2,
    }

    /// <summary>
    /// Vegetación del Mapa v2 (<c>veg</c> de <c>af-river-map/2</c>; contrato 29; D-069, M2-1; propuesta del Mapa v2 §3.1, capa 5 y
    /// §9.1): un punto por celda de <see cref="CellKm"/> km, determinista (lo dispersa <c>tools/river-map/geometry.py</c>), con su
    /// tipo, tamaño (0-2), capa (<see cref="Lod"/>: 0 la gruesa, una celda de cada cuatro) y tono (0-7), y su distancia al eje del río
    /// más cercano. El cliente decide por zoom cuáles caen bajo el agua o las orillas y cuáles dibuja. Inmutable.
    /// </summary>
    public sealed class RiverVegetation
    {
        /// <summary>Unidad de <c>info[2i+1]</c>: km por paso (08 §4.5).</summary>
        public const float DistanceStepKm = 2f;

        /// <summary>Tope de la distancia (255 pasos).</summary>
        public const float MaxDistanceKm = 255f * DistanceStepKm;

        /// <summary>Sin vegetación (un Mapa sin <c>veg</c>, solo en los tests).</summary>
        public static readonly RiverVegetation Empty = new RiverVegetation(0f, Array.Empty<float>(), Array.Empty<byte>(), 1f);

        private readonly float[] _xy;
        private readonly byte[] _info;

        /// <summary>Crea la vegetación (el lector ya la validó).</summary>
        public RiverVegetation(float cellKm, float[] xy, byte[] info, float kmPerUnit)
        {
            CellKm = cellKm;
            _xy = xy;
            _info = info;
            KmPerUnit = kmPerUnit;
        }

        /// <summary>Lado de la celda (km).</summary>
        public float CellKm { get; }

        /// <summary>Km por unidad del marco (W = 1): el ancho del marco en km.</summary>
        public float KmPerUnit { get; }

        /// <summary>Puntos.</summary>
        public int Count => _xy.Length / 2;

        /// <summary>x0, y0, x1, y1… en unidades del marco (W = 1, y hacia abajo). No lo modifiques.</summary>
        public float[] Xy => _xy;

        /// <summary>Tipo del punto <paramref name="i"/>.</summary>
        public RiverVegKind Kind(int i)
        {
            int k = _info[2 * i] & 3;
            return k > 2 ? RiverVegKind.Canopy : (RiverVegKind)k;
        }

        /// <summary>Tamaño del punto <paramref name="i"/> (0-2).</summary>
        public int Size(int i)
        {
            return Math.Min(2, (_info[2 * i] >> 2) & 3);
        }

        /// <summary>Capa del punto <paramref name="i"/>: 0 la gruesa (se ve con poco zoom), 1 la fina.</summary>
        public int Lod(int i)
        {
            return (_info[2 * i] >> 4) & 1;
        }

        /// <summary>Tono del punto <paramref name="i"/> (0-7).</summary>
        public int Tone(int i)
        {
            return (_info[2 * i] >> 5) & 7;
        }

        /// <summary>Distancia del punto <paramref name="i"/> al eje del río más cercano (km, con tope <see cref="MaxDistanceKm"/>).</summary>
        public float DistanceKm(int i)
        {
            return _info[(2 * i) + 1] * DistanceStepKm;
        }
    }

    /// <summary>
    /// Lo que el Mapa v2 añade a la geometría (<c>af-river-map/2</c>; D-069, M2-1): las orillas por línea, ordenadas por s, para
    /// buscarlas sin asignar memoria.
    /// </summary>
    public sealed class RiverBanks
    {
        /// <summary>Sin orillas.</summary>
        public static readonly RiverBanks Empty = new RiverBanks(Array.Empty<RiverBank>(), 0);

        private readonly RiverBank[] _all;
        private readonly int[] _first;
        private readonly int[] _count;

        /// <summary>Crea el índice (los tramos, por línea y por s0, como los da la herramienta).</summary>
        public RiverBanks(RiverBank[] all, int lineCount)
        {
            _all = all;
            _first = new int[lineCount];
            _count = new int[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                _first[i] = -1;
            }

            for (int i = 0; i < all.Length; i++)
            {
                int line = all[i].Line;
                if (_first[line] < 0)
                {
                    _first[line] = i;
                }

                _count[line]++;
            }
        }

        /// <summary>Tramos en total.</summary>
        public int Count => _all.Length;

        /// <summary>Todos los tramos.</summary>
        public IReadOnlyList<RiverBank> All => _all;

        /// <summary>Tramos de la línea <paramref name="line"/> (seguidos desde <see cref="FirstOf"/>).</summary>
        public int CountOf(int line)
        {
            return line >= 0 && line < _count.Length ? _count[line] : 0;
        }

        /// <summary>Primer tramo de la línea <paramref name="line"/> en <see cref="All"/> (−1 si no tiene).</summary>
        public int FirstOf(int line)
        {
            return line >= 0 && line < _first.Length ? _first[line] : -1;
        }

        /// <summary>Tramo <paramref name="i"/>.</summary>
        public RiverBank At(int i)
        {
            return _all[i];
        }
    }
}
