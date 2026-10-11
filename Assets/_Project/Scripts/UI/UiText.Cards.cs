#nullable enable
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// Textos ES-419 de la tarjeta previa de 1 toque y del tutorial de una pantalla de cada estreno (tarea T13 del paso 1; 02 §8,
    /// §9.1, §11 T4, §12 y §13; propuesta-mecanicas-dificultad.md §b.0 #4 y §b.2; 04 §2.5). Cada tutorial ocupa 2 líneas como
    /// máximo a 360 dp con la fuente al 100 %, en 16, 14 o 12 sp (02 §12: «textos del tutorial ≤2 líneas»; mínimo de 12 sp): lo
    /// comprueba <c>LevelCardScreenTests</c>. Los 11 textos son los canónicos (ninguno ha hecho falta acortarlo: caben en 2 líneas
    /// en la caja de la tarjeta). Todos nombran frutas, objetos o fauna, así que quedan pendientes de revisión del asesor cultural
    /// (07 §8.2); los de §b.2 son [P].
    /// </summary>
    public static partial class UiText
    {
        /// <summary>Cuia chica (02 §8; estreno en el 14). Pendiente de revisión del asesor cultural (07 §8.2).</summary>
        public const string TutorialChica = "Esta cuia chica solo guarda dos dosis: úsala para apartar, no para terminar";

        /// <summary>
        /// Cuia alta (02 §8; estreno en el 27) [P]: «agua» en lugar de «fruta» (pregunta 2 del paso 1, opción B por defecto: en el
        /// nivel 27 la de cinco dosis es el Agua del Tapajós, que no es una fruta). Pendiente de revisión del asesor cultural.
        /// </summary>
        public const string TutorialAlta = "Esta agua tiene cinco dosis: solo cabe en la cuia alta";

        /// <summary>
        /// Hoja (02 §9.1 M1; estreno en el 43, con la llave aguaje, índice 4). Sin dedo guía (D-002). Pendiente de revisión del
        /// asesor cultural.
        /// </summary>
        public const string TutorialHoja = "Esta hoja solo deja pasar aguaje. Vierte aguaje para abrirla.";

        /// <summary>Matapi [P] (§b.2 M10; estreno en el 63). Pendiente de revisión del asesor cultural.</summary>
        public const string TutorialMatapi = "Lo que entra al matapi no sale. Llénalo con una sola fruta.";

        /// <summary>
        /// Liana con fruta [P] (§b.2 M3; estreno en el 83, con la llave taperebá, índice 3). Pendiente de revisión del asesor
        /// cultural.
        /// </summary>
        public const string TutorialLiana = "La liana se suelta cuando selles una cuia de taperebá.";

        /// <summary>
        /// Regla T4 de Aguas turbias (02 §11 T4: «se comunica al jugador en el tutorial del debut de Aguas turbias»): obligatoria
        /// en la tarjeta del 100, nunca se acorta ni se omite.
        /// </summary>
        public const string TutorialMurkyRule = "Debajo nunca se repite la fruta de encima.";

        /// <summary>Aguas turbias [P] (§b.2; estreno en el 100, en la tarjeta combinada), con la línea T4. Pendiente del asesor cultural.</summary>
        public const string TutorialMurky = "El agua turbia esconde frutas. " + TutorialMurkyRule;

        /// <summary>Remolino (02 §9.1 M4; estreno en el 103). Pendiente de revisión del asesor cultural.</summary>
        public const string TutorialRemolino = "Las dos cuias del remolino están unidas por debajo: lo que viertes en una aparece en el fondo de la otra.";

        /// <summary>Barro (02 §9.1 M5; estreno en el 123), con «…» U+2026, que está en el atlas. Pendiente del asesor cultural.</summary>
        public const string TutorialBarro = "El barro acepta cualquier fruta; el primer vertido lo lava… y entierra lo que hay debajo.";

        /// <summary>Lluvia (02 §9.1 M6; estreno en el 143). Pendiente de revisión del asesor cultural.</summary>
        public const string TutorialLluvia = "La lluvia llena esta cuia cada tres vertidos: deja arriba el agua o saca la fruta antes de que la tape.";

        /// <summary>
        /// Brote (02 §9.1 M2; estreno en el 163 por D-007, con su cima de camu camu, índice 6). Pendiente de revisión del asesor
        /// cultural.
        /// </summary>
        public const string TutorialBrote = "El brote está cerrado: vierte más camu camu encima para que germine.";

        /// <summary>Piraña (02 §9.1 M7; estreno en el 183). Pendiente de revisión del asesor cultural.</summary>
        public const string TutorialPirana = "Cada cuatro vertidos la piraña muerde la cuia que acabas de llenar y se lleva una dosis a su cueva.";

        /// <summary>
        /// Línea de la Luciérnaga en la tarjeta del 100 (04 §2.5: «Una luciérnaga ilumina las de una cuia»). Nombra fauna:
        /// pendiente de revisión del asesor cultural.
        /// </summary>
        public const string FireflyLine = "Una luciérnaga ilumina las de una cuia";

        /// <summary>Regalo de la Luciérnaga del estreno de Aguas turbias (04 §2.5) [P]. Pendiente de revisión del asesor cultural.</summary>
        public const string FireflyGift = "+1 Luciérnaga";

        /// <summary>
        /// Tutorial de una pantalla del estreno <paramref name="introMechanic"/> (<c>intro_mechanic</c> de D-007), o null si no es
        /// ninguno de los 11 de la 1.0.
        /// </summary>
        public static string? Tutorial(string? introMechanic)
        {
            switch (introMechanic)
            {
                case "chica": return TutorialChica;
                case "alta": return TutorialAlta;
                case "hoja": return TutorialHoja;
                case "matapi": return TutorialMatapi;
                case "liana": return TutorialLiana;
                case LevelFlow.MurkyIntroMechanic: return TutorialMurky;
                case "remolino": return TutorialRemolino;
                case "barro": return TutorialBarro;
                case "lluvia": return TutorialLluvia;
                case "brote": return TutorialBrote;
                case "pirana": return TutorialPirana;
                default: return null;
            }
        }
    }
}
