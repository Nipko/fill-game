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

        /// <summary><c>play</c>.</summary>
        public const string Play = "Jugar";

        /// <summary><c>currency</c> (nombre del contador de semillas para el lector de pantalla).</summary>
        public const string Seeds = "Semillas";

        /// <summary><c>lives</c>.</summary>
        public const string Canoes = "Canoas";

        /// <summary><c>settings</c>.</summary>
        public const string Settings = "Ajustes";

        /// <summary>
        /// «Inicio» [P]: botón secundario de la victoria y pestaña de la pantalla de inicio (06 §4.7; D-008 puntos 3 y 6). Aún
        /// sin clave en el glosario §10 (pendiente con los textos de P14).
        /// </summary>
        public const string Home = "Inicio";

        /// <summary><c>back</c>.</summary>
        public const string Back = "Volver";

        /// <summary><c>difficulty_hard</c> (chip del botón Jugar [P], 06 §4.7).</summary>
        public const string DifficultyHard = "Difícil";

        /// <summary><c>very_hard</c> (chip del botón Jugar [P], 06 §4.7).</summary>
        public const string DifficultyVeryHard = "Muy difícil";

        /// <summary>Título de la pantalla de final de contenido (06 §9; 08 §6.2).</summary>
        public const string ContentEndTitle = "El río sigue creciendo";

        /// <summary>
        /// Texto [P] de «El río sigue creciendo» con <c>live_content_end_message.next_batch_eta_days</c> (06 §9: «fecha estimada
        /// de los próximos 100 niveles»), redactado sobre el push «Nuevos niveles» de 06 §8.3. Pendiente de aprobación (P14).
        /// </summary>
        public const string ContentEndEta = "Río arriba llegan 100 niveles nuevos en unos {0} días.";

        /// <summary>Título de Sin canoas (04 §7.3).</summary>
        public const string NoCanoesTitle = "Sin canoas";

        /// <summary>Cronómetro de Sin canoas (04 §7.3; D-008 paso 4).</summary>
        public const string NoCanoesNext = "Tu próxima canoa vuelve en";

        /// <summary>"Nivel {n}".</summary>
        public static string Level(int level)
        {
            return string.Format(CultureInfo.InvariantCulture, LevelN, level);
        }

        /// <summary>«Jugar · Nivel {n}» (<c>play</c> + <c>level_n</c>; 06 §4.7; D-008 punto 6).</summary>
        public static string PlayLevel(int level)
        {
            return Play + " · " + Level(level);
        }

        /// <summary>Etiqueta del chip de dificultad para <c>difficulty.label</c>, o null si es <c>normal</c>.</summary>
        public static string? DifficultyChip(string label)
        {
            switch (label)
            {
                case "hard": return DifficultyHard;
                case "very_hard": return DifficultyVeryHard;
                default: return null;
            }
        }

        /// <summary>Texto de «El río sigue creciendo» con los días estimados.</summary>
        public static string ContentEnd(int etaDays)
        {
            return string.Format(CultureInfo.InvariantCulture, ContentEndEta, etaDays);
        }

        /// <summary>
        /// Escribe <paramref name="value"/> (≥ 0) en <paramref name="buffer"/> desde <paramref name="pos"/>, con separador de
        /// miles «.» si <paramref name="grouped"/> (glosario §10: «1.000»). Devuelve la posición siguiente. Sin asignaciones:
        /// los contadores del inicio lo usan con <c>TMP_Text.SetCharArray</c>.
        /// </summary>
        public static int WriteNumber(char[] buffer, int pos, int value, bool grouped)
        {
            if (value < 0)
            {
                value = 0;
            }

            int digits = 1;
            for (int v = value; v >= 10; v /= 10)
            {
                digits++;
            }

            int length = grouped ? digits + ((digits - 1) / 3) : digits;
            int end = pos + length;
            int at = end - 1;
            int written = 0;
            do
            {
                if (grouped && written > 0 && written % 3 == 0)
                {
                    buffer[at--] = '.';
                }

                buffer[at--] = (char)('0' + (value % 10));
                value /= 10;
                written++;
            }
            while (value > 0);

            return end;
        }

        /// <summary>
        /// Escribe un cronómetro (04 §7.3: «mm:ss»; «h:mm:ss» desde una hora, p. ej. Canoas infinitas acumuladas) con
        /// <paramref name="totalSeconds"/> ≥ 0. Devuelve la posición siguiente. Sin asignaciones.
        /// </summary>
        public static int WriteClock(char[] buffer, int pos, long totalSeconds)
        {
            if (totalSeconds < 0)
            {
                totalSeconds = 0;
            }

            long hours = totalSeconds / 3600;
            int minutes = (int)((totalSeconds / 60) % 60);
            int seconds = (int)(totalSeconds % 60);
            if (hours > 0)
            {
                pos = WriteNumber(buffer, pos, (int)System.Math.Min(hours, 99), false);
                buffer[pos++] = ':';
            }

            buffer[pos++] = (char)('0' + (minutes / 10));
            buffer[pos++] = (char)('0' + (minutes % 10));
            buffer[pos++] = ':';
            buffer[pos++] = (char)('0' + (seconds / 10));
            buffer[pos++] = (char)('0' + (seconds % 10));
            return pos;
        }
    }
}
