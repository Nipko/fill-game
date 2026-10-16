#nullable enable
using System;
using AF.GameFlow;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la línea de fruta de Frutas del río en la victoria (D-017; 02 §6, fila «Dato del río»; glosario §10; propuesta
    /// de Frutas §2.2 y §2.6; tarea F3). Parcial de <see cref="UiText"/>:
    /// <list type="bullet">
    /// <item>de <c>strings.csv</c>, la cabecera <c>river_fact</c> («Dato del río») y el contador <c>fact_progress</c> [P]
    /// («{name} · {k}/{n}»), dos etiquetas de la misma fila, nunca una cadena pegada a otra;</item>
    /// <item>de <c>facts.csv</c> (<see cref="Loc.Fact(string)"/>), el texto de la línea (<c>fact.&lt;tinte&gt;.&lt;kk&gt;</c>) y el
    /// nombre visible del tinte (<c>tint.&lt;tinte&gt;.name</c>), que en release es «» mientras no los apruebe el asesor (D-018,
    /// Q10-A).</item>
    /// </list>
    /// <see cref="TryFruitLine"/> solo da textos si están todos en el idioma de la UI: nunca mezcla idiomas ni enseña marcas de
    /// clave que falta. Los textos con argumentos asignan su cadena: solo al enseñar la línea, nunca por fotograma.
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>river_fact</c> (glosario §10): «Dato del río», cabecera de la línea de fruta.</summary>
        public static string RiverFact => Loc.T(Keys.RiverFact);

        /// <summary>
        /// <c>fact_progress</c> [P] (glosario §10): «Açaí · 3/20», el nombre del tinte <paramref name="name"/> con la línea
        /// <paramref name="k"/> de las <paramref name="n"/> de su serie, sin separador de miles.
        /// </summary>
        public static string FactProgress(string name, int k, int n)
        {
            return Format(Keys.FactProgress, LocArgs.Of("name", name ?? throw new ArgumentNullException(nameof(name)))
                .AndDigits("k", NonNegative(k)).AndDigits("n", NonNegative(n)));
        }

        /// <summary>Nombre visible del tinte de la serie de <paramref name="position"/> (<c>tint.&lt;tinte&gt;.name</c>), o «».</summary>
        public static string FruitName(in FactPosition position)
        {
            string? key = position.NameKey;
            return key != null ? Loc.Fact(key) : string.Empty;
        }

        /// <summary>
        /// Los textos de la línea de fruta de <paramref name="position"/> en el idioma de la UI, o false si falta alguno (D-018,
        /// Q10-A: una línea no se enseña a medias ni en dos idiomas):
        /// <list type="bullet">
        /// <item><paramref name="header"/>: <c>river_fact</c>;</item>
        /// <item><paramref name="progress"/>: <c>fact_progress</c> con el nombre del tinte y k/n, o solo el nombre en la última
        /// línea de la serie, donde el contador pasa a un sello (<paramref name="sealed"/>; Q5-A);</item>
        /// <item><paramref name="text"/>: la línea (<c>fact.&lt;tinte&gt;.&lt;kk&gt;</c>).</item>
        /// </list>
        /// Cada texto tiene que existir en el idioma de la UI (<see cref="Loc.Has"/>: sin «[clave]» de desarrollo) y, en release
        /// (<see cref="Loc.ShowUnapproved"/> apagado), estar aprobado: los de <c>facts.csv</c> ya los oculta
        /// <see cref="Loc.Fact(string)"/>; los de UI, por su estado (≥ <c>aprobado_es</c>).
        /// </summary>
        public static bool TryFruitLine(in FactPosition position, out string header, out string progress, out string text, out bool @sealed)
        {
            header = string.Empty;
            progress = string.Empty;
            text = string.Empty;
            @sealed = position.IsLast;
            string? key = position.Key;
            if (key == null)
            {
                return false;
            }

            string line = Loc.Fact(key);
            string name = FruitName(in position);
            if (line.Length == 0 || name.Length == 0 || !Visible(Keys.RiverFact) || (!@sealed && !Visible(Keys.FactProgress)))
            {
                return false;
            }

            header = RiverFact;
            progress = @sealed ? name : FactProgress(name, position.K, position.Lines);
            text = line;
            return true;
        }

        // Un texto de UI que se puede enseñar en el idioma de ahora: con texto en su columna y, en release, aprobado.
        private static bool Visible(string key)
        {
            return Loc.Has(key) && (Loc.ShowUnapproved || Loc.StatusOf(key, Loc.Locale) >= LocStatus.ApprovedEs);
        }

        /// <summary>Claves de <c>strings.csv</c> (parcial de <see cref="UiText.Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>river_fact</c> (glosario §10).</summary>
            public const string RiverFact = "river_fact";

            /// <summary><c>fact_progress</c> [P] (glosario §10).</summary>
            public const string FactProgress = "fact_progress";
        }
    }
}
