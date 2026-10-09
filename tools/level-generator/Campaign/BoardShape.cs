// Forma y distancia de Hamming de un tablero para la anti-repetición (GDD 03 §3 «forma(estado)» y §17 #9). La comparten
// tools/level-generator (colocación) y tools/level-qa (comprobación), para que los dos midan lo mismo.
//   forma    = multiconjunto de (capacidad, mecánica, patrón de colores de la cuia renombrados por primera aparición dentro
//              de la cuia) [P]: no cambia al permutar colores ni cuias;
//   Hamming  = posiciones (cuia, capa) en orden visual con los colores renombrados por primera aparición en cada tablero;
//              diferencias / posiciones ocupadas en alguno de los dos [P].
using System.Text;

namespace AmazonFill.Tools.LevelGenerator.Campaign;

internal static class BoardShape
{
    public const int ShapeWindow = 200;
    public const int HammingWindow = 20;
    public const double MinHamming = 0.25;

    public static string Shape(int[] caps, int[][] cuias, int[] mechanicTypes)
    {
        var parts = new List<string>(cuias.Length);
        for (int i = 0; i < cuias.Length; i++)
        {
            var local = new Dictionary<int, int>();
            var pattern = new StringBuilder();
            foreach (int c in cuias[i])
            {
                if (!local.TryGetValue(c, out int id))
                {
                    id = c == 15 ? 25 : local.Count;
                    local[c] = id;
                }

                pattern.Append((char)('a' + id));
            }

            parts.Add($"{caps[i]}{mechanicTypes[i]}:{pattern}");
        }

        parts.Sort(StringComparer.Ordinal);
        return string.Join("|", parts);
    }

    public static double Hamming(int[][] a, int[][] b)
    {
        int[][] ra = Renamed(a);
        int[][] rb = Renamed(b);
        int cuias = Math.Max(ra.Length, rb.Length);
        int occupied = 0;
        int diff = 0;
        for (int i = 0; i < cuias; i++)
        {
            for (int k = 0; k < 6; k++)
            {
                int x = i < ra.Length && k < ra[i].Length ? ra[i][k] : 0;
                int y = i < rb.Length && k < rb[i].Length ? rb[i][k] : 0;
                if (x == 0 && y == 0)
                {
                    continue;
                }

                occupied++;
                diff += x != y ? 1 : 0;
            }
        }

        return occupied == 0 ? 0 : (double)diff / occupied;
    }

    private static int[][] Renamed(int[][] cuias)
    {
        var names = new Dictionary<int, int>();
        return cuias.Select(c => c.Select(x =>
        {
            if (!names.TryGetValue(x, out int id))
            {
                id = names.Count + 1;
                names[x] = id;
            }

            return id;
        }).ToArray()).ToArray();
    }
}
