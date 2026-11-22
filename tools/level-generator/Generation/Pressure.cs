// Escalón de presión de la curva 2 (03 §12, «Escala larga»; D-068 P3 A; propuesta de la curva de la 1.1 §4.1) [P]. Es la suma
// de las palancas que lleva el nivel, ninguna «una cuia más»:
//   vacíos:      2 o más vacíos de 4 libres → 0; 1 → 1; 1 y una Cuia chica, o el vacío bajo un Aguapé → 2;
//   congeladas:  ningún grupo de Aguapé → 0; 1 grupo → 1; 2 grupos o 1 Aguapé espeso → 2;
//   ocultas:     ninguna → 0; 1-2 dosis → 1; 3-4 → 2 (Aguas turbias, sin la tinaja);
//   mecánicas:   1 (o ninguna) → 0; 2 → 1; 3 → 2 (sin contar las Aguas turbias ni el Aguapé, que ya tienen su palanca [P], ni las
//                capacidades);
//   disparador o contador (Liana, Liana de cuenta, Flor, Lluvia, Piraña y la Lancha) → 1; dos encadenados, 1.2;
//   colores K, solo en los hitos: ≤ 10 → 0; 11 → 1; 12 o más → 2.
// Solo guía e informe (difficulty.pressure, 08 §4.1): la etiqueta la sigue poniendo el bot (D-009). La lectura de cada palanca
// es [P] de esta herramienta.
using AF.Core.Board;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class Pressure
{
    /// <summary>Códigos que cuentan como disparador o contador (03 §12): Liana, Liana de cuenta, Flor, Lluvia, Piraña y Lancha.</summary>
    public static readonly string[] Triggers = ["Li", "Ln", "Fl", "Ll", "Pi", "La"];

    /// <summary>Escalón con las palancas ya leídas.</summary>
    /// <param name="occupied">El vacío lo sustituye una Cuia chica o lo ocupa un Aguapé (la palanca de vacíos en 2).</param>
    public static int Of(int freeEmpties, bool occupied, int aguapeLever, int hiddenUnits, IEnumerable<string> codes, int k, bool milestone)
    {
        var list = codes.Where(c => c is not (MechanicCodes.Murky or MechanicCodes.Aguape or MechanicCodes.AguapeThick)).Distinct(StringComparer.Ordinal).ToList();
        int empties = freeEmpties >= 2 ? 0 : occupied ? 2 : 1;
        int hidden = hiddenUnits == 0 ? 0 : hiddenUnits <= 2 ? 1 : 2;
        int mechanics = list.Count <= 1 ? 0 : list.Count == 2 ? 1 : 2;
        int trigger = list.Any(c => Triggers.Contains(c)) ? 1 : 0;
        int colors = !milestone || k <= 10 ? 0 : k == 11 ? 1 : 2;
        return Math.Min(9, empties + aguapeLever + hidden + mechanics + trigger + colors);
    }

    /// <summary>Escalón de un tablero publicado.</summary>
    public static int OfBoard(BoardData b, int k, bool milestone)
    {
        var codes = new List<string>();
        foreach (MechanicSpec a in b.Attrs.Where(a => !a.IsNone))
        {
            if (MechanicCodes.CodeOf(a) is string code)
            {
                codes.Add(code);
            }
        }

        if (b.Cuias.Any(c => c.Contains(Cuia.MudColor)))
        {
            codes.Add(MechanicCodes.Barro);
        }

        bool tinaja = b.Attrs.Any(a => a.Type == MechanicType.Tinaja);
        int hidden = tinaja ? 0 : b.Hidden.Length;
        int free = Enumerable.Range(0, b.Cuias.Length).Count(i => b.Cuias[i].Length == 0 && b.Caps[i] >= Cuia.DefaultCapacity && b.Attrs[i].IsNone);
        bool chica = Enumerable.Range(0, b.Caps.Length).Any(i => b.Caps[i] < Cuia.DefaultCapacity && b.Attrs[i].Type is not (MechanicType.Pirana or MechanicType.Tinaja or MechanicType.Cucharon));
        bool aguapeEmpty = Enumerable.Range(0, b.Cuias.Length).Any(i => b.Cuias[i].Length == 0 && b.Caps[i] >= Cuia.DefaultCapacity && b.Attrs[i].Type == MechanicType.Aguape);
        return Of(free, chica || aguapeEmpty, AguapeLever(b.Attrs), hidden, codes, k, milestone);
    }

    /// <summary>Palanca de congeladas: ningún grupo → 0; 1 grupo → 1; 2 grupos o un Aguapé espeso → 2 (03 §12).</summary>
    public static int AguapeLever(IEnumerable<MechanicSpec> attrs)
    {
        var aguape = attrs.Where(a => a.Type == MechanicType.Aguape).ToList();
        int groups = aguape.Select(a => a.Group).Distinct().Count();
        return groups == 0 ? 0 : groups >= 2 || aguape.Any(a => a.N == MechanicCodes.AguapeThickSeals) ? 2 : 1;
    }

    /// <summary>
    /// Escalón que se espera de una celda del calendario (para el aviso PRESSURE de --validate-spec): el hito con 1 vacío (bajo la
    /// mata si lleva el Aguapé [E]), una dosis oculta si lleva Aguas turbias, el grupo del Aguapé (2 con el espeso) y el K del
    /// plan. Es una estimación: el tablero publicado da el suyo.
    /// </summary>
    public static int OfCell(CalendarCell c, int k, bool milestone)
    {
        int aguape = c.Has(MechanicCodes.AguapeThick) ? 2 : c.Has(MechanicCodes.Aguape) ? 1 : 0;
        return Of(1, c.HasCapacity("Ch") || aguape > 0, aguape, c.Has(MechanicCodes.Murky) ? 1 : 0, c.Mechanics.Select(m => m.Code), k, milestone);
    }
}
