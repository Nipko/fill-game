#nullable enable
using System;
using AF.Core.Mechanics;

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
    /// <item><description>Aguapé (1.1, D-068 [P]; 03 §3: «la clave canónica conserva el grupo, con los ids renumerados en orden
    /// canónico»): el id de grupo va en <c>Packed</c> (bits 47-49), así que dos tableros que solo difieren en cómo se numeran sus
    /// grupos darían claves distintas. <see cref="Sort"/> los renumera: ordena los grupos por la lista ordenada de los
    /// <c>Packed</c> de sus cuias sin el id (comparación lexicográfica y, a igualdad de prefijo, la lista más corta primero),
    /// les da los ids 0, 1, 2… en ese orden y vuelve a ordenar las cuias. Dos grupos empatados tienen las mismas cuias, así que
    /// cualquier desempate da el mismo tablero: es una forma canónica completa para los grupos (no para las parejas de
    /// Remolino, que siguen como arriba). Sin Aguapé no hace nada más: los tableros de la 1.0 conservan su orden y su hash
    /// (golden de <c>CanonicalHashGoldenTests</c> sobre todos los packs publicados).</description></item>
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

        /// <summary>Máximo de grupos de Aguapé (3 bits de id).</summary>
        private const int MaxGroups = Cuia.MaxMechanicField + 1;

        /// <summary>Bits del id de grupo en <see cref="Cuia.Packed"/> (los del id de pareja, 47-49).</summary>
        private const ulong GroupBits = 0x7UL << 47;

        /// <summary>
        /// Ordena <paramref name="count"/> cuias desde <paramref name="offset"/> por <see cref="Cuia.Packed"/> ascendente (inserción)
        /// y, si hay Aguapé, renumera sus grupos en orden canónico (<see cref="RelabelGroups"/>) y vuelve a ordenar.
        /// </summary>
        public static void Sort(Cuia[] cuias, int offset, int count)
        {
            CheckRange(cuias, offset, count);
            InsertionSort(cuias, offset, count);
            if (RelabelGroups(cuias, offset, count))
            {
                InsertionSort(cuias, offset, count);
            }
        }

        /// <summary>
        /// Renumera en su sitio, sin reordenar, los ids de grupo del Aguapé de <paramref name="count"/> cuias desde
        /// <paramref name="offset"/> en orden canónico (ver la clase). Los nuevos ids solo dependen del multiconjunto de cuias, no
        /// de su orden ni de los ids de entrada. Devuelve false, sin tocar nada, si no hay ningún Aguapé. Sin asignaciones en el
        /// montón (búferes en la pila); no valida argumentos (bucle caliente). La usa también el solver para traducir los índices
        /// canónicos de su solución a los reales.
        /// </summary>
        internal static bool RelabelGroups(Cuia[] cuias, int offset, int count)
        {
            int end = offset + count;
            int present = 0;
            for (int i = offset; i < end; i++)
            {
                Cuia cuia = cuias[i];
                if (cuia.Mechanic == MechanicType.Aguape)
                {
                    present |= 1 << cuia.MechanicPairId;
                }
            }

            if (present == 0)
            {
                return false;
            }

            // keys[g * MaxStride ..] = Packed sin el id de las cuias del grupo g, en orden ascendente.
            const int MaxStride = 16;
            Span<ulong> keys = stackalloc ulong[MaxGroups * MaxStride];
            Span<int> lengths = stackalloc int[MaxGroups];
            Span<int> order = stackalloc int[MaxGroups];
            Span<int> relabel = stackalloc int[MaxGroups];
            for (int i = offset; i < end; i++)
            {
                Cuia cuia = cuias[i];
                if (cuia.Mechanic != MechanicType.Aguape)
                {
                    continue;
                }

                int group = cuia.MechanicPairId;
                ulong key = cuia.Packed & ~GroupBits;
                int start = group * MaxStride;
                int j = lengths[group]++;
                while (j > 0 && keys[start + j - 1] > key)
                {
                    keys[start + j] = keys[start + j - 1];
                    j--;
                }

                keys[start + j] = key;
            }

            int groups = 0;
            for (int g = 0; g < MaxGroups; g++)
            {
                if ((present & (1 << g)) == 0)
                {
                    continue;
                }

                int j = groups++;
                while (j > 0 && CompareGroups(keys, lengths, order[j - 1], g) > 0)
                {
                    order[j] = order[j - 1];
                    j--;
                }

                order[j] = g;
            }

            for (int rank = 0; rank < groups; rank++)
            {
                relabel[order[rank]] = rank;
            }

            for (int i = offset; i < end; i++)
            {
                Cuia cuia = cuias[i];
                if (cuia.Mechanic == MechanicType.Aguape)
                {
                    cuias[i] = cuia.WithMechanicPairBits(relabel[cuia.MechanicPairId]);
                }
            }

            return true;
        }

        /// <summary>Orden de dos grupos: lexicográfico sobre sus claves ordenadas y, a igualdad de prefijo, el más corto antes.</summary>
        private static int CompareGroups(Span<ulong> keys, Span<int> lengths, int a, int b)
        {
            const int MaxStride = 16;
            int lengthA = lengths[a];
            int lengthB = lengths[b];
            int common = lengthA < lengthB ? lengthA : lengthB;
            for (int k = 0; k < common; k++)
            {
                ulong x = keys[(a * MaxStride) + k];
                ulong y = keys[(b * MaxStride) + k];
                if (x != y)
                {
                    return x < y ? -1 : 1;
                }
            }

            return lengthA.CompareTo(lengthB);
        }

        private static void InsertionSort(Cuia[] cuias, int offset, int count)
        {
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
