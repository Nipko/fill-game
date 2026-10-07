#nullable enable
using System.Globalization;

namespace AF.UI
{
    /// <summary>
    /// Textos ES-419 del prototipo v0 con su clave del glosario (§5.1, §10) y de GDD 04 §6.5. La localización
    /// (Unity Localization, ES-419 / PT-BR / EN) queda fuera de v0: cuando llegue, estas constantes pasan a ser las
    /// claves de las String Tables.
    /// </summary>
    public static class UiText
    {
        /// <summary><c>level_n</c>.</summary>
        public const string LevelN = "Nivel {0}";

        /// <summary><c>undo</c>: etiqueta funcional de Marea en la bandeja (glosario §0.2).</summary>
        public const string Undo = "Deshacer";

        /// <summary><c>restart</c>: etiqueta funcional de Volver a la orilla.</summary>
        public const string Restart = "Reiniciar";

        /// <summary><c>next</c>.</summary>
        public const string Next = "Siguiente";

        /// <summary><c>level_complete</c>.</summary>
        public const string LevelComplete = "¡Nivel completado!";

        /// <summary><c>stuck_title</c> (= <c>no_moves</c>).</summary>
        public const string StuckTitle = "No hay más vertidos";

        /// <summary><c>stuck_subtitle</c>.</summary>
        public const string StuckSubtitle = "Aguas estancadas";

        /// <summary><c>stuck_undo</c>.</summary>
        public const string StuckUndo = "Deshacer · Marea";

        /// <summary><c>stuck_restart</c>.</summary>
        public const string StuckRestart = "Volver a la orilla (gratis)";

        /// <summary>"Nivel {n}".</summary>
        public static string Level(int level)
        {
            return string.Format(CultureInfo.InvariantCulture, LevelN, level);
        }
    }
}
