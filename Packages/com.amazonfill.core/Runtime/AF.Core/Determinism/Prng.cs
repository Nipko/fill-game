#nullable enable
using System;
using System.Globalization;
using System.Text;

namespace AF.Core.Determinism
{
    /// <summary>
    /// PRNG determinista del núcleo (GDD 08 §2.1 principio 5; 09 R26, decidido el 2026-10-06: PRNG nuevo en C#, paridad
    /// con el prototipo sobre los estados exportados): <b>xoshiro256**</b> de David Blackman y Sebastiano Vigna
    /// («Scrambled Linear Pseudorandom Number Generators», ACM TOMS 47(4), 2021; código de referencia de dominio público en
    /// prng.di.unimi.it), con el estado inicial rellenado por <b>SplitMix64</b> (Steele, Lea y Flood, OOPSLA 2014), como
    /// recomiendan los autores. Escrito desde cero; solo enteros, igual en CoreCLR, Mono e IL2CPP.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Semilla de un nivel: FNV-1a de 64 bits de la cadena UTF-8 <c>amazonfill:v1:m{mm}:v{v}:n{nnnn}</c>
    /// (08 §4.1, con ceros a la izquierda; <see cref="LevelSeed"/>).</description></item>
    /// <item><description>Sub-semillas (intentos del generador, partidas del bot…): <see cref="SubSeed"/>, una mezcla
    /// SplitMix64 de la semilla y del número de flujo.</description></item>
    /// <item><description><see cref="NextInt(int)"/> sin sesgo: multiplicación de 32 × 32 bits con rechazo (método de
    /// Lemire, «Fast Random Integer Generation in an Interval», ACM TOMACS 29(1), 2019).</description></item>
    /// <item><description>Sin asignaciones tras construirlo: una instancia se resiembra con <see cref="Reseed"/>.</description></item>
    /// </list>
    /// No es seguro entre hilos: una instancia por hilo. Nunca <c>System.Random</c> (BannedSymbols).
    /// </remarks>
    public sealed class Prng
    {
        /// <summary>Base de desplazamiento de FNV-1a de 64 bits.</summary>
        public const ulong FnvOffsetBasis = 14695981039346656037UL;

        /// <summary>Primo de FNV-1a de 64 bits.</summary>
        public const ulong FnvPrime = 1099511628211UL;

        private const ulong Golden = 0x9E3779B97F4A7C15UL;

        private ulong _s0;
        private ulong _s1;
        private ulong _s2;
        private ulong _s3;

        /// <summary>Crea un generador sembrado con <paramref name="seed"/>.</summary>
        public Prng(ulong seed)
        {
            Reseed(seed);
        }

        /// <summary>Crea un generador sembrado con la FNV-1a de 64 bits de <paramref name="seedText"/> (UTF-8).</summary>
        public Prng(string seedText)
            : this(SeedFrom(seedText))
        {
        }

        /// <summary>Vuelve a sembrar sin asignar: el estado se rellena con cuatro salidas de SplitMix64.</summary>
        public void Reseed(ulong seed)
        {
            ulong state = seed;
            _s0 = SplitMix64(ref state);
            _s1 = SplitMix64(ref state);
            _s2 = SplitMix64(ref state);
            _s3 = SplitMix64(ref state);
        }

        /// <summary>Siguientes 64 bits de xoshiro256**.</summary>
        public ulong NextULong()
        {
            unchecked
            {
                ulong result = RotateLeft(_s1 * 5UL, 7) * 9UL;
                ulong t = _s1 << 17;
                _s2 ^= _s0;
                _s3 ^= _s1;
                _s1 ^= _s2;
                _s0 ^= _s3;
                _s2 ^= t;
                _s3 = RotateLeft(_s3, 45);
                return result;
            }
        }

        /// <summary>Siguientes 32 bits: los 32 altos de <see cref="NextULong"/> (los de mejor calidad).</summary>
        public uint NextUInt()
        {
            return (uint)(NextULong() >> 32);
        }

        /// <summary>Entero uniforme en [0, <paramref name="bound"/>), sin sesgo (Lemire con rechazo).</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bound"/> &lt; 1.</exception>
        public int NextInt(int bound)
        {
            if (bound < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bound), bound, "La cota debe ser al menos 1.");
            }

            unchecked
            {
                uint b = (uint)bound;
                ulong m = (ulong)NextUInt() * b;
                uint low = (uint)m;
                if (low < b)
                {
                    uint threshold = (0u - b) % b; // 2^32 mod b
                    while (low < threshold)
                    {
                        m = (ulong)NextUInt() * b;
                        low = (uint)m;
                    }
                }

                return (int)(m >> 32);
            }
        }

        /// <summary>Entero uniforme en [<paramref name="min"/>, <paramref name="maxExclusive"/>).</summary>
        public int NextInt(int min, int maxExclusive)
        {
            if (maxExclusive <= min)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "El intervalo está vacío.");
            }

            return min + NextInt(maxExclusive - min);
        }

        /// <summary>FNV-1a de 64 bits de <paramref name="text"/> codificado en UTF-8 (sin BOM).</summary>
        public static ulong SeedFrom(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            byte[] bytes = Encoding.UTF8.GetBytes(text);
            ulong hash = FnvOffsetBasis;
            unchecked
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    hash ^= bytes[i];
                    hash *= FnvPrime;
                }
            }

            return hash;
        }

        /// <summary>
        /// Cadena de semilla de un nivel de campaña: <c>amazonfill:v1:m{mm}:v{v}:n{nnnn}</c> con ceros a la izquierda
        /// (08 §4.1; glosario §11), p. ej. <c>amazonfill:v1:m03:v1:n0043</c>.
        /// </summary>
        public static string LevelSeedText(int world, int lap, int level)
        {
            if (world < 1 || world > 99 || lap < 1 || level < 1 || level > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "Mundo 1-99, vuelta ≥ 1 y nivel 1-9999.");
            }

            return "amazonfill:v1:m" + world.ToString("D2", CultureInfo.InvariantCulture)
                + ":v" + lap.ToString(CultureInfo.InvariantCulture)
                + ":n" + level.ToString("D4", CultureInfo.InvariantCulture);
        }

        /// <summary>Semilla de un nivel de campaña: <see cref="SeedFrom"/> de <see cref="LevelSeedText"/>.</summary>
        public static ulong LevelSeed(int world, int lap, int level)
        {
            return SeedFrom(LevelSeedText(world, lap, level));
        }

        /// <summary>
        /// Sub-semilla <paramref name="stream"/> de <paramref name="seed"/> (intento del generador, partidas del bot,
        /// muestreos…): una salida de SplitMix64 desde <c>seed + (stream + 1)·φ</c>. Flujos distintos dan semillas
        /// independientes; la misma pareja da siempre la misma.
        /// </summary>
        public static ulong SubSeed(ulong seed, ulong stream)
        {
            ulong state = unchecked(seed + (stream * Golden));
            return SplitMix64(ref state);
        }

        private static ulong SplitMix64(ref ulong state)
        {
            unchecked
            {
                state += Golden;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        private static ulong RotateLeft(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }
    }
}
