#nullable enable
using System.Collections.Generic;
using AF.GameFlow;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de TiTino (D-016, puntos 4 y 5; glosario §10; propuesta de TiTino e.3, e.4 y g.3; tarea TI3). Parcial de
    /// <see cref="UiText"/>:
    /// <list type="bullet">
    /// <item>de <c>strings.csv</c>, sus 30 frases (<c>sloth_line_&lt;momento&gt;_&lt;nn&gt;</c>, las claves de
    /// <see cref="SlothLines"/>) y la fila de Ajustes «Mensajes de TiTino» (<c>settings_sloth_messages</c>);</item>
    /// <item>de <c>facts.csv</c> (<see cref="Loc.Fact(string)"/>), los 11 datos del perezoso
    /// (<c>fauna.bradypus_variegatus.dato_&lt;nn&gt;</c>; pregunta 49, (a), D-022).</item>
    /// </list>
    /// Todo nace <c>pendiente_asesor</c>: en release, <see cref="TrySlothLine"/> no da la frase hasta que el asesor la apruebe y
    /// <see cref="SlothFact"/> devuelve «» (D-018, Q10-A). Los textos con <c>{n}</c> asignan su cadena: solo al enseñar la frase.
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>settings_sloth_messages</c> [P] (glosario §10): la fila «Mensajes de TiTino» de Ajustes (02 §12).</summary>
        public static string SettingsSlothMessages => Loc.T(Keys.SettingsSlothMessages);

        /// <summary>Las 30 claves de las frases de TiTino (tests: todas en <c>strings.csv</c>).</summary>
        public static IEnumerable<string> SlothLineKeys => SlothLines.AllKeys;

        /// <summary>Las 11 claves de los datos del perezoso en <c>facts.csv</c>.</summary>
        public static IReadOnlyList<string> SlothFactKeys => SlothLines.SlothFactKeys;

        /// <summary>
        /// El texto de la frase de <paramref name="line"/> en el idioma de la UI (con su <c>{n}</c>: la Crecida o el nivel), o
        /// false si no habla o si la frase no se puede enseñar: sin texto en este idioma o, en release
        /// (<see cref="Loc.ShowUnapproved"/> apagado), sin aprobar por el asesor o, en otro idioma, sin revisión nativa
        /// (<see cref="LocCatalog.IsApproved"/>). Nunca mezcla idiomas.
        /// </summary>
        public static bool TrySlothLine(in SlothLine line, out string text)
        {
            text = string.Empty;
            string? key = line.Key;
            if (key == null || !SlothLineShown(key))
            {
                return false;
            }

            // {n} es la Crecida o el nivel: sin separador de miles, como «Nivel {n}» del HUD (UiText.LevelN).
            text = line.Arg > 0 ? Format(key, LocArgs.OfDigits("n", line.Arg)) : Loc.T(key);
            return text.Length > 0;
        }

        // Una frase de TiTino se enseña si tiene texto y, en release, con la regla de aprobación por idioma (08 §4.7; D-018,
        // Q5-A y Q10-A; LocTable.IsApprovedAt): el español en aprobado_es y, en otro idioma, además revisado_nativo.
        private static bool SlothLineShown(string key)
        {
            return Loc.Has(key) && (Loc.ShowUnapproved || Loc.Catalog.IsApproved(key, Loc.Locale));
        }

        /// <summary>Dato del perezoso de <paramref name="key"/> (<c>facts.csv</c>), o «» si falta o no está aprobado en release.</summary>
        public static string SlothFact(string key)
        {
            return key == null ? string.Empty : Loc.Fact(key);
        }

        /// <summary>
        /// El asesor aprobó el dato del color (<see cref="SlothLines.ColorFactKey"/>, dato_11), que necesita la frase n.º 3 (regla 8
        /// de e.2): <see cref="SlothVictoryFacts.ColorFactApproved"/>.
        /// </summary>
        public static bool SlothColorFactApproved => Loc.StatusOf(SlothLines.ColorFactKey, LocLocale.SourceLocale) >= LocStatus.ApprovedEs;

        /// <summary>Claves de <c>strings.csv</c> (parcial de <see cref="UiText.Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>settings_sloth_messages</c> [P] (glosario §10).</summary>
            public const string SettingsSlothMessages = "settings_sloth_messages";
        }
    }
}
