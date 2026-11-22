#nullable enable
using System.Globalization;

namespace AF.Tests
{
    /// <summary>
    /// Paquetes de prueba de la curva 2 de la 1.1 (D-068, fase F4; 08 §4.1, <c>pack_schema</c> 5): hasta F5 no hay packs de la
    /// curva publicados, así que los tests de EditMode y de PlayMode los construyen aquí, en JSON, con los tableros de los tests
    /// compartidos del núcleo (<c>AguapeTests</c> y <c>LanchaTests</c>), que pasan <c>LevelValidation</c>. Solo cadenas: sin
    /// referencias a los ensamblados del juego.
    /// </summary>
    public static class CurveFixtures
    {
        /// <summary>Tablero corriente de 4 cuias (el nivel 1 de 02 §13 con una vacía más): sin mecánicas.</summary>
        public const string PlainCuias = "[[8, 8, 8, 4], [4, 4, 4, 8], [], []]";

        /// <summary>
        /// Aguapé (02 §9.1 M8): el grupo 3 en las cuias 0 y 1 (la 0 sella con un 1), un origen que sella la 0 y la 2 recibe; 7
        /// cuias en filas fijas 4/3 (<c>rows</c>). Se gana: 2→1, 2→0 (sella y suelta), 3→4, 3→1 (sella), 3→4, 2→4 y 6→5.
        /// </summary>
        public const string AguapeCuias = "[[1, 1, 1], [2, 2], [3, 3, 1, 2], [3, 2, 3], [], [4, 4], [4, 4]]";

        /// <summary>attrs del Aguapé de <see cref="AguapeCuias"/>; con <c>n</c> = 2 no valida (el espeso pide 4-5 cuias).</summary>
        public const string AguapeAttrs = "[{\"type\": \"aguape\", \"group\": 3}, {\"type\": \"aguape\", \"group\": 3}, null, null, null, null, null]";

        /// <summary>
        /// Aguapé espeso (02 §9.1 M8, variante; n = 2): el grupo 0 en las cuias 0-3, que sellan recibiendo con frutas distintas. Se
        /// gana: 4→2 (primer sello: se aclara), 4→1 (segundo: se suelta), 4→0 y 5→3.
        /// </summary>
        public const string ThickCuias = "[[1, 1, 1], [2, 2, 2], [3, 3], [4, 4], [1, 2, 3, 3], [4, 4], []]";

        /// <summary>attrs del espeso de <see cref="ThickCuias"/>.</summary>
        public const string ThickAttrs = "[{\"type\": \"aguape\", \"group\": 0, \"n\": 2}, {\"type\": \"aguape\", \"group\": 0, \"n\": 2}, "
            + "{\"type\": \"aguape\", \"group\": 0, \"n\": 2}, {\"type\": \"aguape\", \"group\": 0, \"n\": 2}, null, null, null]";

        /// <summary>
        /// Lancha (02 §9.1 M11): carga de 2 taperebá con k = 2; un vacío al empezar. Se gana: 1→2 (sella y deja la 1 vacía: la
        /// lancha atraca), 3→1 y 0→1.
        /// </summary>
        public const string LanchaCuias = "[[3, 3], [1, 1], [1, 1], [3, 3], []]";

        /// <summary>attrs de la Lancha de <see cref="LanchaCuias"/> (k = 2).</summary>
        public const string LanchaAttrs = "[{\"type\": \"lancha\", \"k\": 2}, null, null, null, null]";

        /// <summary>Un nivel de campaña <c>m{mm}_v1_n{nnnn}</c> con el tablero dado; <paramref name="extra"/> va al final del nivel.</summary>
        public static string Level(int world, int level, string cuias, string? attrs = null, string extra = "", string idSuffix = "", string replaces = "")
        {
            int count = Count(cuias);
            string caps = "[" + Repeat("4", count) + "]";
            string id = Id(world, level) + idSuffix;
            return "{ \"id\": \"" + id + "\", \"level\": " + N(level) + ", \"world\": " + N(world) + ", \"lap\": 1, \"water_base\": \"black\", "
                + "\"palette\": [\"acai\", \"aguaje\"], \"units_per_color\": 4, \"caps\": " + caps + ", \"cuias\": " + cuias
                + ", \"attrs\": " + (attrs ?? "[" + Repeat("null", count) + "]") + ", \"murky\": false, \"hidden\": []"
                + (replaces.Length > 0 ? ", \"replaces\": \"" + replaces + "\"" : string.Empty)
                + ", \"difficulty\": { \"par\": 6, \"label\": \"hard\", \"band\": \"challenge\" }, \"relief\": []" + extra + " }";
        }

        /// <summary>Paquete con los niveles dados (ya en JSON), del mundo <paramref name="world"/>.</summary>
        public static string Pack(string packId, string kind, int schema, int world, params string[] levels)
        {
            return "{ \"pack_schema\": " + N(schema) + ", \"pack_id\": \"" + packId + "\", \"kind\": \"" + kind + "\", \"world\": " + N(world)
                + ", \"world_id\": \"w07_tefe\", \"lap\": 1, \"water_base\": \"black\", \"levels\": [ " + string.Join(", ", levels) + " ] }";
        }

        /// <summary>Id de campaña del slot (<c>m07_v1_n0303</c>).</summary>
        public static string Id(int world, int level)
        {
            return "m" + world.ToString("00", CultureInfo.InvariantCulture) + "_v1_n" + level.ToString("0000", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Campaña del mundo 7 de prueba (la curva 1): los slots <paramref name="from"/>-<paramref name="to"/> con el tablero
        /// corriente, <c>pack_schema</c> 4.
        /// </summary>
        public static string Campaign(int from, int to)
        {
            var levels = new string[to - from + 1];
            for (int l = from; l <= to; l++)
            {
                levels[l - from] = Level(7, l, PlainCuias);
            }

            return Pack("mundo_07_v1", "campaign", 4, 7, levels);
        }

        /// <summary>
        /// La curva 2 del mundo 7 de prueba (<c>curva_07_v1</c>, <c>kind</c> = <c>curve</c>): el 302 corriente y el 303 con el
        /// estreno del Aguapé (con <c>rows</c> 4/3 y <c>pressure</c> 4).
        /// </summary>
        public static string Curve()
        {
            return Pack("curva_07_v1", "curve", 5, 7,
                Level(7, 302, PlainCuias, idSuffix: "_c2", replaces: Id(7, 302), extra: ", \"intro_mechanic\": null"),
                AguapeCurveLevel(303));
        }

        /// <summary>El tablero de la curva 2 del slot <paramref name="level"/> con el estreno del Aguapé, <c>rows</c> 4/3 y presión 4.</summary>
        public static string AguapeCurveLevel(int level)
        {
            return Level(7, level, AguapeCuias, AguapeAttrs, ", \"rows\": [4, 3], \"intro_mechanic\": \"aguape\", \"skippable\": false", "_c2", Id(7, level))
                .Replace("\"label\": \"hard\", \"band\": \"challenge\" }", "\"label\": \"hard\", \"band\": \"challenge\", \"pressure\": 4 }");
        }

        /// <summary>La reserva de la curva 2 de prueba (<c>reserva_curva_07_v1</c>): el 303 sin el Aguapé.</summary>
        public static string CurveReserve()
        {
            return Pack("reserva_curva_07_v1", "reserve", 5, 7,
                Level(7, 303, PlainCuias, idSuffix: "_c2_r1", replaces: Id(7, 303) + "_c2"));
        }

        private static int Count(string cuias)
        {
            int depth = 0;
            int count = 0;
            for (int i = 0; i < cuias.Length; i++)
            {
                char c = cuias[i];
                if (c == '[')
                {
                    depth++;
                    if (depth == 2)
                    {
                        count++;
                    }
                }
                else if (c == ']')
                {
                    depth--;
                }
            }

            return count;
        }

        private static string Repeat(string item, int count)
        {
            var parts = new string[count];
            for (int i = 0; i < count; i++)
            {
                parts[i] = item;
            }

            return string.Join(", ", parts);
        }

        private static string N(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
