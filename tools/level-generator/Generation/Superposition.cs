// Superposición de mecánicas sobre un tablero base (GDD 03 §9, consecuencia 2: «superposición sobre un tablero base y
// verificación con el solver extendido»). Aquí solo se colocan los atributos y se mueven dosis; la verificación (BoardBuilder,
// LevelValidation, SafePath y A*) la hace SlotGenerator con AF.Core/AF.Solver. Una mecánica por cuia.
//   Hoja (M1)      sobre una cuia vacía de 4, con llave = un color de 4 dosis (la del debut, aguaje: TintAssigner).
//   Matapi (M10)   vacío, o con 1-3 dosis de un solo color y cap = U_c (D-007, regla 3).
//   Lluvia (M6)    sobre una cuia vacía de 4: se quitan n gotas (2, o 1) de agua de las cimas (03 §9: hacia atrás) y se
//                  escribe k (3) siempre [P]; el agua es el tinte del mundo (TintAssigner).
//   Liana (M3)     con fruta sobre una cuia con contenido; la fruta no está dentro y puede sellar fuera (D-007, regla 4).
//   Remolino (M4)  par entre dos cuias sin mecánica (nunca un Matapi: una mecánica por cuia).
//   Brote (M2)     sobre una cuia con 1-3 dosis visibles (sin Barro); su color es la cima (la del debut, camu camu).
//   Barro (M5)     en la capa 2 o 3 (índice 1-2) de una cuia de 4 sin mecánica; la dosis desplazada se recoloca en la cima
//                  de otra cuia con hueco para conservar U_c (lectura de «retira una unidad» de 03 §9 [P]).
//   Piraña (M7)    cueva = Cuia chica de 2 vacía (03 §9), de 4 si hay Liana (bloqueo C1-9), con k = 4 escrito [P].
//   Aguas turbias  1-2 cuias sin mecánica con 2-3 dosis: se ocultan las de debajo de la cima (T3), con T4 y como mucho 4
//                  dosis (T2); en el 100 (Q9) una cuia con 2 dosis ocultas.
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal sealed class SuperpositionContext
{
    /// <summary>Color abstracto → tinte obligatorio (llave de un estreno o agua del mundo).</summary>
    public Dictionary<int, string> FixedTints { get; } = [];

    public required LevelPlan Plan { get; init; }
    public required CampaignSpec Spec { get; init; }
    public required int Colors { get; init; }
    public required int[] Units { get; init; }

    /// <summary>La Piraña va con cueva de 4 (Liana en el nivel: bloqueo C1-9).</summary>
    public bool UniformCaps { get; init; }

    /// <summary>Q9: estreno de Aguas turbias en el hito 100 (1 cuia, 2 dosis ocultas).</summary>
    public bool MurkyDebut { get; init; }
}

internal static class Superposition
{
    /// <summary>Orden de colocación: primero las que necesitan vacíos o pares, al final las que tocan el contenido.</summary>
    public static readonly string[] Order = ["Re", "Ho", "Ma", "Ll", "Li", "Br", "Ba", "Pi", "Tu"];

    public static string? Apply(GenBoard b, MechanicInfo m, SuperpositionContext ctx, Prng rng)
    {
        return m.Code switch
        {
            "Ho" => Hoja(b, m, ctx, rng),
            "Ma" => Matapi(b, ctx, rng),
            "Ll" => Lluvia(b, m, ctx, rng),
            "Li" => Liana(b, m, ctx, rng),
            "Re" => Remolino(b, rng),
            "Br" => Brote(b, m, ctx, rng),
            "Ba" => Barro(b, rng),
            "Pi" => Pirana(b, m, ctx),
            "Tu" => Murky(b, ctx, rng),
            _ => $"mecanica sin superposicion: {m.Code}",
        };
    }

    private static bool Free(GenBoard b, int i) => b.Attrs[i].IsNone && !b.HasHidden(i);

    private static List<int> EmptyFours(GenBoard b) =>
        Enumerable.Range(0, b.Count).Where(i => b.Cuias[i].Count == 0 && b.Caps[i] == Cuia.DefaultCapacity && Free(b, i)).ToList();

    private static List<int> FourColors(SuperpositionContext ctx) =>
        Enumerable.Range(1, ctx.Colors).Where(c => ctx.Units[c - 1] == Cuia.DefaultCapacity).ToList();

    private static T Pick<T>(List<T> list, Prng rng) => list[rng.NextInt(list.Count)];

    private static string? Hoja(GenBoard b, MechanicInfo m, SuperpositionContext ctx, Prng rng)
    {
        List<int> empties = EmptyFours(b);
        if (empties.Count == 0)
        {
            return "Hoja: no hay vacio de 4";
        }

        int i = Pick(empties, rng);
        int key = Pick(FourColors(ctx), rng);
        b.Attrs[i] = new MechanicSpec(MechanicType.Hoja, key: key);
        FixDebutKey(ctx, m, key);
        return null;
    }

    private static string? Matapi(GenBoard b, SuperpositionContext ctx, Prng rng)
    {
        var options = new List<int>(EmptyFours(b));
        for (int i = 0; i < b.Count; i++)
        {
            List<int> c = b.Cuias[i];
            if (c.Count is >= 1 and <= 3 && Free(b, i) && b.Caps[i] == Cuia.DefaultCapacity && c.All(x => x == c[0])
                && c[0] != Cuia.MudColor && ctx.Units[c[0] - 1] == b.Caps[i])
            {
                options.Add(i);
            }
        }

        if (options.Count == 0)
        {
            return "Matapi: no hay vacio ni cuia monocolor de 1-3 dosis";
        }

        options.Sort();
        b.Attrs[Pick(options, rng)] = new MechanicSpec(MechanicType.Matapi);
        return null;
    }

    private static string? Lluvia(GenBoard b, MechanicInfo m, SuperpositionContext ctx, Prng rng)
    {
        List<int> empties = EmptyFours(b);
        if (empties.Count == 0)
        {
            return "Lluvia: no hay vacio de 4";
        }

        int cloud = Pick(empties, rng);
        foreach (int drops in new[] { 2, 1 })
        {
            // Agua: un color de 4 dosis con al menos `drops` dosis en las cimas (rachas visibles).
            var waters = FourColors(ctx).Where(c => TopDoses(b, c) >= drops).ToList();
            if (waters.Count == 0)
            {
                continue;
            }

            int water = Pick(waters, rng);
            int left = drops;
            while (left > 0)
            {
                var tops = Enumerable.Range(0, b.Count).Where(i => Free(b, i) && b.Cuias[i].Count > 0 && b.Top(i) == water).ToList();
                int i = Pick(tops, rng);
                b.Cuias[i].RemoveAt(b.Cuias[i].Count - 1);
                left--;
            }

            b.Attrs[cloud] = new MechanicSpec(MechanicType.Lluvia, key: water, n: drops, k: m.K);
            ctx.FixedTints[water] = ctx.Plan.World.WaterTint;
            return null;
        }

        return "Lluvia: ningun color tiene dosis en las cimas para quitar las gotas";
    }

    private static int TopDoses(GenBoard b, int color)
    {
        int total = 0;
        for (int i = 0; i < b.Count; i++)
        {
            if (!Free(b, i))
            {
                continue;
            }

            List<int> c = b.Cuias[i];
            for (int k = c.Count - 1; k >= 0 && c[k] == color; k--)
            {
                total++;
            }
        }

        return total;
    }

    private static string? Liana(GenBoard b, MechanicInfo m, SuperpositionContext ctx, Prng rng)
    {
        var options = Enumerable.Range(0, b.Count).Where(i => b.Cuias[i].Count > 0 && b.Caps[i] == Cuia.DefaultCapacity && Free(b, i)
            && !b.Cuias[i].Contains(Cuia.MudColor)).ToList();
        if (options.Count == 0)
        {
            return "Liana: no hay cuia con contenido";
        }

        int i = Pick(options, rng);
        var keys = FourColors(ctx).Where(c => !b.Cuias[i].Contains(c)).ToList();
        if (keys.Count == 0)
        {
            return "Liana: ningun color puede ser la fruta";
        }

        int key = Pick(keys, rng);
        b.Attrs[i] = new MechanicSpec(MechanicType.Liana, key: key);
        FixDebutKey(ctx, m, key);
        return null;
    }

    private static string? Remolino(GenBoard b, Prng rng)
    {
        var options = Enumerable.Range(0, b.Count).Where(i => Free(b, i) && b.Caps[i] == Cuia.DefaultCapacity).ToList();
        if (options.Count < 2)
        {
            return "Remolino: faltan cuias libres";
        }

        int a = Pick(options, rng);
        options.Remove(a);
        int c = Pick(options, rng);
        b.Attrs[a] = new MechanicSpec(MechanicType.Remolino, partner: c);
        b.Attrs[c] = new MechanicSpec(MechanicType.Remolino, partner: a);
        return null;
    }

    private static string? Brote(GenBoard b, MechanicInfo m, SuperpositionContext ctx, Prng rng)
    {
        var options = Enumerable.Range(0, b.Count).Where(i => b.Cuias[i].Count is >= 1 and <= 3 && b.Cuias[i].Count < b.Caps[i]
            && Free(b, i) && !b.Cuias[i].Contains(Cuia.MudColor)).ToList();
        if (options.Count == 0)
        {
            return "Brote: no hay cuia con 1-3 dosis";
        }

        int i = Pick(options, rng);
        b.Attrs[i] = new MechanicSpec(MechanicType.Brote);
        FixDebutKey(ctx, m, b.Top(i));
        return null;
    }

    private static string? Barro(GenBoard b, Prng rng)
    {
        var spots = new List<(int Cuia, int Layer)>();
        for (int i = 0; i < b.Count; i++)
        {
            if (b.Caps[i] != Cuia.DefaultCapacity || !Free(b, i) || b.Cuias[i].Contains(Cuia.MudColor))
            {
                continue;
            }

            for (int k = 1; k <= 2 && k < b.Cuias[i].Count; k++)
            {
                spots.Add((i, k));
            }
        }

        if (spots.Count == 0)
        {
            return "Barro: no hay capa 2-3 en una cuia de 4";
        }

        (int cuia, int layer) = Pick(spots, rng);
        int displaced = b.Cuias[cuia][layer];
        var targets = Enumerable.Range(0, b.Count).Where(i => i != cuia && b.Attrs[i].IsNone && b.Cuias[i].Count < b.Caps[i]).ToList();
        if (targets.Count == 0)
        {
            return "Barro: no hay hueco para la dosis desplazada";
        }

        // Preferir una cuia a medias (no gastar un vacío) si la hay.
        var partial = targets.Where(i => b.Cuias[i].Count > 0).ToList();
        int target = Pick(partial.Count > 0 ? partial : targets, rng);
        b.Cuias[cuia][layer] = Cuia.MudColor;
        b.Cuias[target].Add(displaced);
        return null;
    }

    private static string? Pirana(GenBoard b, MechanicInfo m, SuperpositionContext ctx)
    {
        int cap = ctx.UniformCaps ? Cuia.DefaultCapacity : m.CaveCap;
        b.AddEmpty(cap, new MechanicSpec(MechanicType.Pirana, k: m.K));
        return null;
    }

    /// <summary>
    /// Dosis ocultas que pone el generador [P]: 3 como mucho (el tope de 02 T2 es 4). Con 3 hay ≤ 6 realidades y la ruta segura
    /// (03 §8) cuesta segundos en lugar de minutos por candidato.
    /// </summary>
    public const int MaxHiddenGenerated = 3;

    private static string? Murky(GenBoard b, SuperpositionContext ctx, Prng rng)
    {
        // Cuias que pueden ser turbias: sin mecánica, sin Barro, 2-3 dosis (T3) y T4 en las capas que se ocultan.
        var options = new List<(int Cuia, int Hidden)>();
        for (int i = 0; i < b.Count; i++)
        {
            List<int> c = b.Cuias[i];
            if (!b.Attrs[i].IsNone || c.Contains(Cuia.MudColor) || c.Count < 2 || c.Count > 3 || c.Count >= b.Caps[i])
            {
                continue;
            }

            int hide = c.Count - 1;
            bool t4 = true;
            for (int k = 0; k < hide; k++)
            {
                t4 &= c[k] != c[k + 1];
            }

            if (t4 && (!ctx.MurkyDebut || hide == 2))
            {
                options.Add((i, hide));
            }
        }

        if (options.Count == 0)
        {
            return "Aguas turbias: no hay cuia de 2-3 dosis con T4";
        }

        int wanted = ctx.MurkyDebut ? 1 : 1 + rng.NextInt(2);
        int total = 0;
        for (int n = 0; n < wanted && options.Count > 0; n++)
        {
            (int cuia, int hide) = Pick(options, rng);
            options.RemoveAll(o => o.Cuia == cuia);
            if (total + hide > MaxHiddenGenerated)
            {
                continue;
            }

            for (int k = 0; k < hide; k++)
            {
                b.Hidden.Add((cuia, k));
            }

            total += hide;
        }

        return total == 0 ? "Aguas turbias: ninguna cuia cabe en el tope de 4 dosis" : null;
    }

    private static void FixDebutKey(SuperpositionContext ctx, MechanicInfo m, int color)
    {
        if (ctx.Plan.DebutBoard && m.DebutKey is not null && ctx.Plan.Mechanics.Contains(m))
        {
            ctx.FixedTints[color] = m.DebutKey;
        }
    }
}
