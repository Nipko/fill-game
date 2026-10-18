#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// El largo de las líneas de la sonda (tarea FX10, observación 1 de la prueba de la ola 6 en el teléfono): en el teléfono,
    /// logcat corta cada mensaje de Unity en unos 1.024 caracteres (1.066 con su prefijo), así que el tope de 3.900 bytes de FX7
    /// no bastaba y llegaban rotas la sonda del Mapa con <c>af_max_level 560</c> (entera y, con el Muestrario, sus partes) y la de
    /// Ajustes con los botones <c>fruit_*</c>. Ninguna línea de la sonda pasa de <see cref="MaxLineBytes"/> bytes de UTF-8, contada
    /// entera (la etiqueta, <c>AF_STATE </c> o <c>AF_LAYOUT </c>, la cabecera y <c>"part"</c>/<c>"parts"</c>): las tildes cuentan
    /// dos bytes.
    /// <list type="bullet">
    /// <item><description><c>AF_LAYOUT</c>, con el esquema de FX7 (<see cref="LayoutLines"/>): las cuias en la parte 1 y los
    /// botones repartidos en orden.</description></item>
    /// <item><description>Las demás (<c>AF_STATE</c> y <c>AF_PERF</c> de <see cref="DevProbe"/>), con <see cref="Write"/>: una
    /// línea que cabe sale como siempre; si no, en partes seguidas (<see cref="ProbeLines"/>), cada una con la cabecera
    /// (el primer miembro, <c>"event":…</c>, y los <c>t</c>, <c>level</c>, <c>sw</c> y <c>sh</c> que lo siguen), después
    /// <c>"part":k,"parts":n</c> y los demás miembros del JSON, en orden y enteros, tantos como quepan. Juntas (la cabecera y
    /// los miembros de cada parte, de la 1 a la n, sin <c>part</c> ni <c>parts</c>) dan la línea entera.</description></item>
    /// </list>
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>
        /// Tope de bytes (UTF-8) de una línea de la sonda, contada entera (tarea FX10): por debajo de los ~1.024 caracteres en que
        /// logcat corta un mensaje de Unity en el teléfono, con margen.
        /// </summary>
        public const int MaxLineBytes = 960;

        /// <summary>
        /// Escribe en el log una línea de la sonda (<c>AF_STATE</c> o <c>AF_PERF</c>): entera si cabe en <see cref="MaxLineBytes"/>
        /// y, si no, en las partes de <see cref="ProbeLines"/>. Sin asignaciones si cabe (salvo las del propio log).
        /// </summary>
        internal static void Write(string line)
        {
            if (Encoding.UTF8.GetByteCount(line) <= MaxLineBytes)
            {
                Debug.Log(line);
                return;
            }

            string[] parts = ProbeLines(line, MaxLineBytes);
            for (int i = 0; i < parts.Length; i++)
            {
                Debug.Log(parts[i]);
            }
        }

        /// <summary>
        /// Las líneas en que sale <paramref name="line"/> (<c>ETIQUETA {miembros JSON}</c>) con un tope de
        /// <paramref name="maxBytes"/> bytes de UTF-8: ella misma si cabe; si no, partes con la cabecera (el primer miembro y los
        /// <c>t</c>, <c>level</c>, <c>sw</c> y <c>sh</c> que lo siguen), <c>"part":k,"parts":n</c> y los demás miembros en orden,
        /// tantos como quepan en cada una. Un miembro no se parte: si ni solo cabe con la cabecera, va solo en su parte y esa parte
        /// pasa del tope (con los datos reales no ocurre: el miembro más largo, las cuias de 16 dosis de colores, ocupa unos 300).
        /// Una línea que no es <c>ETIQUETA {…}</c> sale tal cual.
        /// </summary>
        public static string[] ProbeLines(string line, int maxBytes)
        {
            Encoding utf8 = Encoding.UTF8;
            if (utf8.GetByteCount(line) <= maxBytes)
            {
                return new[] { line };
            }

            int open = line.IndexOf('{');
            if (open < 0 || line[line.Length - 1] != '}')
            {
                return new[] { line };
            }

            var members = new List<string>();
            SplitMembers(line, open + 1, line.Length - 1, members);
            int head = 1;
            while (head < members.Count && IsHeadKey(members[head]))
            {
                head++;
            }

            if (members.Count <= head)
            {
                return new[] { line };
            }

            string prefix = line.Substring(0, open + 1) + string.Join(",", members.GetRange(0, head));
            int budget = maxBytes - utf8.GetByteCount(prefix) - utf8.GetByteCount(",\"part\":0000,\"parts\":0000}");
            var groups = new List<int>(); // primer miembro de cada parte
            int used = 0;
            for (int i = head; i < members.Count; i++)
            {
                int b = 1 + utf8.GetByteCount(members[i]); // con su coma
                if (groups.Count == 0 || used + b > budget)
                {
                    groups.Add(i);
                    used = b;
                }
                else
                {
                    used += b;
                }
            }

            int parts = groups.Count;
            var lines = new string[parts];
            var sb = new StringBuilder(maxBytes);
            for (int p = 0; p < parts; p++)
            {
                int to = p + 1 < parts ? groups[p + 1] : members.Count;
                sb.Clear();
                sb.Append(prefix);
                sb.Append(",\"part\":").Append((p + 1).ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"parts\":").Append(parts.ToString(CultureInfo.InvariantCulture));
                for (int i = groups[p]; i < to; i++)
                {
                    sb.Append(',').Append(members[i]);
                }

                sb.Append('}');
                lines[p] = sb.ToString();
            }

            return lines;
        }

        // Los miembros del objeto JSON entre from (tras la llave) y to (la llave de cierre), separados por las comas de primer
        // nivel (fuera de cadenas, objetos y listas).
        private static void SplitMembers(string line, int from, int to, List<string> members)
        {
            int depth = 0;
            bool inString = false;
            int start = from;
            for (int i = from; i < to; i++)
            {
                char c = line[i];
                if (inString)
                {
                    if (c == '\\')
                    {
                        i++; // el carácter escapado
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        break;
                    case '{':
                    case '[':
                        depth++;
                        break;
                    case '}':
                    case ']':
                        depth--;
                        break;
                    case ',':
                        if (depth == 0)
                        {
                            members.Add(line.Substring(start, i - start));
                            start = i + 1;
                        }

                        break;
                }
            }

            if (to > start)
            {
                members.Add(line.Substring(start, to - start));
            }
        }

        // Los miembros que repite cada parte detrás del primero: t, level, sw y sh.
        private static bool IsHeadKey(string member)
        {
            return member.StartsWith("\"t\":", System.StringComparison.Ordinal)
                || member.StartsWith("\"level\":", System.StringComparison.Ordinal)
                || member.StartsWith("\"sw\":", System.StringComparison.Ordinal)
                || member.StartsWith("\"sh\":", System.StringComparison.Ordinal);
        }
    }
}
