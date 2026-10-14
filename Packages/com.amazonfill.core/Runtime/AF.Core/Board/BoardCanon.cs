#nullable enable
using System;

namespace AF.Core.Board
{
    /// <summary>
    /// Canonicalización y hash de 64 bits del estado (GDD 03 §3; 08 §2.2): las cuias ordenadas eliminan las
    /// T! permutaciones de un mismo tablero. Lo usan el solver (visitados) y las herramientas (anti-repetición).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Orden canónico: <see cref="Cuia.Packed"/> ascendente. <c>Packed</c> incluye la capacidad,
    /// las dosis, la máscara de ocultos y los atributos de la mecánica (tipo, <c>key</c>, id de pareja, <c>n</c>, <c>k</c>,
    /// contador y banderas) y, en la Flor de vitória-régia, <c>key2</c> en los bits de <c>n</c> y <c>k</c> (K3b), así que dos
    /// cuias con el mismo <c>Packed</c> son idénticas, el orden es total y la clave es la
    /// <c>(cap, attr, contenido)</c> más los contadores de 03 §3. Una cuia sin mecánica tiene esos bits a 0: los tableros
    /// sin mecánicas conservan su orden y su hash.</description></item>
    /// <item><description>Remolino (03 §3: «las dos cuias de un Remolino se canonicalizan como unidad»): el id de pareja va en
    /// <c>Packed</c>, así que el emparejamiento se conserva; dos tableros que solo difieren en la numeración de las parejas
    /// dan claves distintas (deduplicación incompleta, nunca incorrecta).</description></item>
    /// <item><description>Hash: FNV-1a de 64 bits sobre los 8 bytes little-endian de <c>Packed</c> de cada cuia, en
    /// orden canónico. Solo aritmética entera: igual en CoreCLR, Mono e IL2CPP.</description></item>
    /// <item><description>Sin asignaciones: sirven en el bucle caliente del solver.</description></item>
    /// </list>
    /// </remarks>
    public static class BoardCanon
    {
        /// <summary>Base de desplazamiento de FNV-1a de 64 bits.</summary>
        public const ulong FnvOffsetBasis = 14695981039346656037UL;

        /// <summary>Primo de FNV-1a de 64 bits.</summary>
        public const ulong FnvPrime = 1099511628211UL;

        /// <summary>Ordena <paramref name="count"/> cuias desde <paramref name="offset"/> por <see cref="Cuia.Packed"/> ascendente (inserción).</summary>
        public static void Sort(Cuia[] cuias, int offset, int count)
        {
            CheckRange(cuias, offset, count);
            int end = offset + count;
            for (int i = offset + 1; i < end; i++)
            {
                Cuia key = cuias[i];
                ulong packed = key.Packed;
                int j = i - 1;
                while (j >= offset && cuias[j].Packed > packed)
                {
                    cuias[j + 1] = cuias[j];
                    j--;
                }

                cuias[j + 1] = key;
            }
        }

        /// <summary>
        /// Hash FNV-1a de 64 bits de <paramref name="count"/> cuias desde <paramref name="offset"/>, en el orden
        /// en que están. Para el hash canónico, ordénalas antes con <see cref="Sort"/>.
        /// </summary>
        public static ulong Hash(Cuia[] cuias, int offset, int count)
        {
            CheckRange(cuias, offset, count);
            ulong hash = FnvOffsetBasis;
            int end = offset + count;
            unchecked
            {
                for (int i = offset; i < end; i++)
                {
                    ulong packed = cuias[i].Packed;
                    for (int shift = 0; shift < 64; shift += 8)
                    {
                        hash ^= (packed >> shift) & 0xFFUL;
                        hash *= FnvPrime;
                    }
                }
            }

            return hash;
        }

        private static void CheckRange(Cuia[] cuias, int offset, int count)
        {
            if (cuias == null)
            {
                throw new ArgumentNullException(nameof(cuias));
            }

            if (offset < 0 || count < 0 || offset > cuias.Length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "El tramo [offset, offset + count) se sale del array.");
            }
        }
    }
}
