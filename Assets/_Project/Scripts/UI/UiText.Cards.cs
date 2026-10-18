#nullable enable
using AF.GameFlow;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la tarjeta previa de 1 toque y del tutorial de una pantalla de cada estreno (tarea T13 del paso 1; 02 §8,
    /// §9.1, §11 T4, §12 y §13; propuesta-mecanicas-dificultad.md §b.0 #4 y §b.2; 04 §2.5), de <c>strings.csv</c> con las claves
    /// <c>tutorial_&lt;intro_mechanic&gt;</c> [P] (el glosario §10 aún no les da clave). Cada tutorial ocupa 2 líneas como
    /// máximo a 360 dp con la fuente al 100 %, en 16, 14 o 12 sp (02 §12: «textos del tutorial ≤2 líneas»; mínimo de 12 sp): lo
    /// comprueba <c>LevelCardScreenTests</c>. Los textos son los canónicos, salvo el de la Piraña, que desde D-012 dice que
    /// sellar a tiempo evita el mordisco [P]. Todos nombran frutas, objetos o fauna, así que quedan pendientes de revisión del
    /// asesor cultural (07 §8.2; estado <c>pendiente_asesor</c> en la tabla); los de §b.2 son [P].
    /// </summary>
    public static partial class UiText
    {
        /// <summary>Cuia chica (02 §8; estreno en el 14). Pendiente de revisión del asesor cultural (07 §8.2).</summary>
        public static string TutorialChica => Loc.T(Keys.TutorialChica);

        /// <summary>
        /// Cuia alta (02 §8; estreno en el 27) [P]: «agua» en lugar de «fruta» (pregunta 2 del paso 1, opción B por defecto: en el
        /// nivel 27 la de cinco dosis es el Agua del Tapajós, que no es una fruta). Pendiente de revisión del asesor cultural.
        /// </summary>
        public static string TutorialAlta => Loc.T(Keys.TutorialAlta);

        /// <summary>
        /// Hoja (02 §9.1 M1; estreno en el 43, con la llave aguaje, índice 4). Sin dedo guía (D-002). Pendiente de revisión del
        /// asesor cultural.
        /// </summary>
        public static string TutorialHoja => Loc.T(Keys.TutorialHoja);

        /// <summary>
        /// Hoja atada [P] (variante de la Hoja, 02 §9.1 «Variantes» y §1.2; propuesta de D-007 b.3; estreno en el 283, VA1a):
        /// la regla entera en una frase, sin nombrar la fruta (la lleva la hoja): nunca cae, solo recibe su fruta y también es
        /// origen. Pendiente de revisión del asesor cultural (el nombre «Hoja atada» también, glosario §6.5).
        /// </summary>
        public static string TutorialHojaAtada => Loc.T(Keys.TutorialHojaAtada);

        /// <summary>Matapi [P] (§b.2 M10; estreno en el 63). Pendiente de revisión del asesor cultural.</summary>
        public static string TutorialMatapi => Loc.T(Keys.TutorialMatapi);

        /// <summary>
        /// Liana con fruta [P] (§b.2 M3; estreno en el 83, con la llave taperebá, índice 3). Pendiente de revisión del asesor
        /// cultural.
        /// </summary>
        public static string TutorialLiana => Loc.T(Keys.TutorialLiana);

        /// <summary>
        /// Liana de cuenta [P] (variante de la Liana, 02 §9.1 M3; propuesta de D-007 b.3; estreno en el 243, VA1a): se suelta al
        /// sellar n cuias, que se leen en sus cuentas perla (<c>LianaOverlay</c>). Pendiente de revisión del asesor cultural.
        /// </summary>
        public static string TutorialLianaN => Loc.T(Keys.TutorialLianaN);

        /// <summary>
        /// Regla T4 de Aguas turbias (02 §11 T4: «se comunica al jugador en el tutorial del debut de Aguas turbias»): obligatoria
        /// en la tarjeta del 100, nunca se acorta ni se omite.
        /// </summary>
        public static string TutorialMurkyRule => Loc.T(Keys.TutorialMurkyRule);

        /// <summary>
        /// Aguas turbias [P] (§b.2; estreno en el 100, en la tarjeta combinada), con la línea T4 (<see cref="TutorialMurkyRule"/>)
        /// al final. Una sola frase de la tabla, sin pegar la regla (D-018, tarea L1: cada idioma la escribe entera); los tests
        /// comprueban que termina en la regla. Pendiente del asesor cultural.
        /// </summary>
        public static string TutorialMurky => Loc.T(Keys.TutorialMurky);

        /// <summary>Remolino (02 §9.1 M4; estreno en el 103). Pendiente de revisión del asesor cultural.</summary>
        public static string TutorialRemolino => Loc.T(Keys.TutorialRemolino);

        /// <summary>Barro (02 §9.1 M5; estreno en el 123), con «…» U+2026, que está en el atlas. Pendiente del asesor cultural.</summary>
        public static string TutorialBarro => Loc.T(Keys.TutorialBarro);

        /// <summary>Lluvia (02 §9.1 M6; estreno en el 143). Pendiente de revisión del asesor cultural.</summary>
        public static string TutorialLluvia => Loc.T(Keys.TutorialLluvia);

        /// <summary>
        /// Brote (02 §9.1 M2; estreno en el 163 por D-007, con su cima de camu camu, índice 6). Pendiente de revisión del asesor
        /// cultural.
        /// </summary>
        public static string TutorialBrote => Loc.T(Keys.TutorialBrote);

        /// <summary>
        /// Brote de dos hojas [P] (variante del Brote, 02 §9.1 «Variantes» y R10 b; propuesta de D-007 b.3: «Brote con dos hojas
        /// cerradas; cada dosis abre una»; estreno en el 343, VA1b): cada dosis de su fruta abre una hoja y con las dos germina, sin
        /// nombrar la fruta (la dice el color del capullo). Pendiente de revisión del asesor cultural (el nombre también, glosario
        /// §6.5).
        /// </summary>
        public static string TutorialBroteN2 => Loc.T(Keys.TutorialBroteN2);

        /// <summary>
        /// Flor de vitória-régia [P] (variante de la Liana con fruta, 02 §9.1 «Variantes» y R10 d; propuesta de D-007 b.3: dos
        /// llaves en lógica Y, «con una sola, la flor está blanca y entreabierta»; estreno en el 403, VA1b): se abre al sellar las
        /// dos frutas que enseña. Pendiente de revisión del asesor cultural (el nombre también, glosario §6.5).
        /// </summary>
        public static string TutorialFlor => Loc.T(Keys.TutorialFlor);

        /// <summary>
        /// Piraña (02 §9.1 M7; estreno en el 183) [P] (D-012, punto 1: opción C + D): que se lleva una dosis a su cueva lo enseña
        /// el mordisco animado de la tarjeta (<c>PiranaBiteDemo</c>), así que el texto dice cuándo muerde y que sellar a tiempo
        /// evita el mordisco (la regla del núcleo: la cuia que se sella con ese vertido no se muerde). Conserva «la cuia que
        /// acabas de llenar» del canónico de 02 §9.1 M7: muerde la que recibe el cuarto vertido (<c>PiranaMechanic</c>,
        /// <c>events.Receiver</c>), no cualquiera que se haya llenado antes. Cabe en 2 líneas a 360 dp
        /// (Debut_AllElevenTexts_TwoLinesMax). Pendiente de revisión del asesor cultural.
        /// </summary>
        public static string TutorialPirana => Loc.T(Keys.TutorialPirana);

        /// <summary>
        /// Línea de la Luciérnaga en la tarjeta del 100 (04 §2.5: «Una luciérnaga ilumina las de una cuia»). Nombra fauna:
        /// pendiente de revisión del asesor cultural.
        /// </summary>
        public static string FireflyLine => Loc.T(Keys.FireflyLine);

        /// <summary>Regalo de la Luciérnaga del estreno de Aguas turbias (04 §2.5) [P]. Pendiente de revisión del asesor cultural.</summary>
        public static string FireflyGift => Loc.T(Keys.FireflyGift);

        /// <summary>
        /// Tutorial de una pantalla del estreno <paramref name="introMechanic"/> (<c>intro_mechanic</c> de D-007), o null si no es
        /// ninguno de los 11 de 1-200 ni de las variantes con cliente (Liana de cuenta y Hoja atada, VA1a; Brote de dos hojas y
        /// Flor de vitória-régia, VA1b).
        /// </summary>
        public static string? Tutorial(string? introMechanic)
        {
            switch (introMechanic)
            {
                case "chica": return TutorialChica;
                case "alta": return TutorialAlta;
                case "hoja": return TutorialHoja;
                case LevelCardInfo.HojaAtadaIntro: return TutorialHojaAtada;
                case "matapi": return TutorialMatapi;
                case "liana": return TutorialLiana;
                case LevelCardInfo.LianaCountIntro: return TutorialLianaN;
                case LevelFlow.MurkyIntroMechanic: return TutorialMurky;
                case "remolino": return TutorialRemolino;
                case "barro": return TutorialBarro;
                case "lluvia": return TutorialLluvia;
                case "brote": return TutorialBrote;
                case LevelCardInfo.BroteTwoLeavesIntro: return TutorialBroteN2;
                case LevelCardInfo.FlorIntro: return TutorialFlor;
                case "pirana": return TutorialPirana;
                default: return null;
            }
        }
    }
}
