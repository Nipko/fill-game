#nullable enable
using System;
using System.Text;

namespace AF.Localization
{
    /// <summary>
    /// Pseudolocalización en-XA (propuesta de idiomas §3.8; D-018, nivel 2 de la sección 6), solo en desarrollo y en los
    /// tests: el texto fuente en español con las vocales, la n, la c y la y acentuadas, alargado un
    /// <see cref="ExpansionPercent"/> % [P] y entre corchetes: «Nivel {level}» → «[Ñívél {level} ··]». Revela el texto que no
    /// pasa por la tabla (sale sin corchetes), las frases pegadas a trozos (corchetes en mitad de la frase), los desbordes y los
    /// glifos que faltan. Solo usa caracteres de Latin-1 (<see cref="Characters"/>), que ya están en el atlas y en el cálculo
    /// de glifos de B1. No toca los marcadores (<c>{n}</c>, <c>{sep}</c>, la sintaxis de los plurales, cuyas formas sí se
    /// transforman) ni las etiquetas de TextMeshPro (<c>&lt;b&gt;</c>, <c>&lt;/b&gt;</c>, <c>&lt;color=…&gt;</c>).
    /// </summary>
    public static class LocPseudo
    {
        /// <summary>Código del idioma falso (el de Android).</summary>
        public const string Locale = LocLocale.Pseudo;

        /// <summary>Cuánto se alarga el texto visible, en % [P]: en el rango de +30-40 % de la propuesta (§3.7).</summary>
        public const int ExpansionPercent = 35;

        /// <summary>Carácter de relleno del alargamiento (U+00B7, Latin-1).</summary>
        public const char Padding = '·';

        private const string Plain = "aeiouncyAEIOUNCY";
        private const string Accented = "áéíóúñçýÁÉÍÓÚÑÇÝ";

        /// <summary>Todos los caracteres que añade (corchetes, relleno, espacio y letras acentuadas), para los glifos (B1).</summary>
        public const string Characters = "[] " + "·" + Accented;

        /// <summary>
        /// en-XA de <paramref name="text"/>. Vacío → vacío (una traducción que falta sigue faltando). Determinista: el relleno
        /// es <c>⌈visibles × 35 / 100⌉</c> caracteres al final, un espacio y puntos medios.
        /// </summary>
        public static string Transform(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (text.Length == 0)
            {
                return text;
            }

            var sb = new StringBuilder(text.Length + (text.Length / 2) + 4);
            sb.Append('[');
            int visible = Emit(sb, text, 0, text.Length);
            int padding = ((visible * ExpansionPercent) + 99) / 100;
            if (padding == 1)
            {
                sb.Append(Padding);
            }
            else if (padding > 1)
            {
                sb.Append(' ');
                sb.Append(Padding, padding - 1);
            }

            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>La letra acentuada de Latin-1 que corresponde a <paramref name="c"/>, o <paramref name="c"/> si no hay.</summary>
        public static char Accent(char c)
        {
            int i = Plain.IndexOf(c);
            return i < 0 ? c : Accented[i];
        }

        /// <summary>Copia <c>s[start..end)</c> transformado y devuelve cuántos caracteres visibles tiene.</summary>
        private static int Emit(StringBuilder sb, string s, int start, int end)
        {
            int visible = 0;
            int i = start;
            while (i < end)
            {
                char c = s[i];
                if (c == '{' && LocFormat.TryParseMarker(s, i, end, out LocFormat.Marker m))
                {
                    if (!m.IsPlural)
                    {
                        sb.Append(s, m.Open, m.End - m.Open);
                    }
                    else
                    {
                        // La cabecera «{n:plural:» tal cual; cada forma, transformada; cuenta la forma más larga.
                        sb.Append(s, m.Open, m.FormsStart - m.Open);
                        int longest = 0;
                        int formStart = m.FormsStart;
                        while (true)
                        {
                            int formEnd = LocFormat.FormEnd(s, formStart, m.FormsEnd);
                            int formVisible = Emit(sb, s, formStart, formEnd);
                            longest = formVisible > longest ? formVisible : longest;
                            if (formEnd >= m.FormsEnd)
                            {
                                break;
                            }

                            sb.Append('|');
                            formStart = formEnd + 1;
                        }

                        sb.Append('}');
                        visible += longest;
                    }

                    i = m.End;
                    continue;
                }

                if (c == '<' && TryTag(s, i, end, out int tagEnd))
                {
                    sb.Append(s, i, tagEnd - i);
                    i = tagEnd;
                    continue;
                }

                sb.Append(Accent(c));
                if (c != '\n')
                {
                    visible++;
                }

                i++;
            }

            return visible;
        }

        /// <summary>Etiqueta de TextMeshPro en <paramref name="open"/>: «&lt;», «/» opcional, una letra y hasta «&gt;» en la misma línea.</summary>
        private static bool TryTag(string s, int open, int limit, out int end)
        {
            end = open;
            int i = open + 1;
            if (i < limit && s[i] == '/')
            {
                i++;
            }

            if (i >= limit || !((s[i] >= 'a' && s[i] <= 'z') || (s[i] >= 'A' && s[i] <= 'Z')))
            {
                return false;
            }

            for (; i < limit; i++)
            {
                char c = s[i];
                if (c == '>')
                {
                    end = i + 1;
                    return true;
                }

                if (c == '<' || c == '\n' || c == '{' || c == '}')
                {
                    return false;
                }
            }

            return false;
        }
    }
}
