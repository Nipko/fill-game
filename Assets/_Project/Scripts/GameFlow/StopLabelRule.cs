#nullable enable
using System;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>
    /// En qué nivel sale el rótulo del lugar, «{lugar} · {país}» (<c>stop_label</c>; D-013, punto 8; 07 §2.1; propuesta del Mapa
    /// §10.5, punto 5; tarea M2.3): en el primer nivel de cada parada desde la 1 (<c>10n + 1</c> con n ≥ 1) que tenga parada en el
    /// catálogo, en el primer intento y solo si el nivel no tiene ya su propia presentación: ni banner de mundo
    /// (<see cref="WorldBanners"/>), ni Fruta nueva (<see cref="SightingRule.FruitDebutLevels"/>), ni la tarjeta «¡Miquito a la
    /// vista!», ni estreno de mecánica (<see cref="SightingRule.MechanicDebutLevels"/>), ni el tutorial (1-12; 02 §13). Como mucho
    /// una tarjeta por nivel, con la prioridad tutorial &gt; Fruta nueva &gt; miquito &gt; rótulo (07 §6.4; guion de la intro
    /// §14). En 1-200 sale en 31, 51, 71, 91, 101, 121, 131, 151, 161, 171, 181 y 191 (el 31, por P-M6 (a)). La clave
    /// <c>ui_stop_label</c> (08 §9) lo apaga (<see cref="MapConfig.UiStopLabel"/>). Regla pura, determinista y sin asignaciones;
    /// cuándo entra en la cola de avisos y cuánto dura (<see cref="NoticeTiming.ReadMs(string)"/>), en GameScreen.Notices.cs.
    /// </summary>
    public static class StopLabelRule
    {
        /// <summary>Último nivel del tutorial (02 §13: los hitos con su propia presentación van del 1 al 12).</summary>
        public const int TutorialLastLevel = 12;

        /// <summary>
        /// El nivel <paramref name="level"/> es el primero de una parada desde la 1: <c>10n + 1</c> con n ≥ 1 (11, 21, 31…). No mira
        /// el catálogo.
        /// </summary>
        public static bool IsStopFirstLevel(int level)
        {
            return level > RiverStopCatalog.LevelsPerStop && (level - 1) % RiverStopCatalog.LevelsPerStop == 0;
        }

        /// <summary>
        /// El nivel ya lleva otra presentación que gana al rótulo, sin contar la tarjeta del miquito: el tutorial (1-12), el banner
        /// de mundo, la Fruta nueva o un estreno de mecánica.
        /// </summary>
        public static bool IsTaken(int level)
        {
            return level <= TutorialLastLevel || WorldBanners.ShowsOn(level)
                || Array.IndexOf(SightingRule.FruitDebutLevels, level) >= 0
                || Array.IndexOf(SightingRule.MechanicDebutLevels, level) >= 0;
        }

        /// <summary>
        /// Sale el rótulo al habilitarse el toque en <paramref name="level"/>, con <c>ui_stop_label</c> de 08 §9
        /// (<see cref="MapConfig.UiStopLabel"/>). <paramref name="monkeyCard"/>: el nivel saca la tarjeta del miquito (la vista
        /// sabe si tiene su arte; <see cref="MonkeyCardOn"/> da la regla sin el arte).
        /// </summary>
        public static bool ShowsOn(int level, RiverStopCatalog? stops, bool firstAttempt, bool monkeyCard)
        {
            return ShowsOn(level, stops, firstAttempt, monkeyCard, MapConfig.UiStopLabel);
        }

        /// <summary>
        /// Como <see cref="ShowsOn(int, RiverStopCatalog?, bool, bool)"/>, con la clave <paramref name="enabled"/> explícita
        /// (tests y, cuando exista AF.RemoteConfig, el valor remoto). Sin catálogo, sin parada para el nivel, en un reintento o
        /// con la clave apagada, no sale.
        /// </summary>
        public static bool ShowsOn(int level, RiverStopCatalog? stops, bool firstAttempt, bool monkeyCard, bool enabled)
        {
            return enabled && firstAttempt && !monkeyCard && stops != null && IsStopFirstLevel(level) && !IsTaken(level)
                && stops.StopOf(level) != null;
        }

        /// <summary>
        /// La tarjeta «¡Miquito a la vista!» sale en <paramref name="level"/> en el primer intento según el catálogo
        /// <paramref name="fauna"/> (la regla de <see cref="SightingRule.ShowsCard"/> con la especie del tramo, solo si vive en el
        /// mundo del nivel, como <see cref="IntroCast.Resolve"/>), sin mirar si hay arte: eso lo sabe la vista. False sin
        /// catálogo, fuera de 1-1.000 o en un tramo sin fauna.
        /// </summary>
        public static bool MonkeyCardOn(int level, FaunaCatalog? fauna)
        {
            if (fauna == null || level < 1 || level > WorldTable.LastLevel || fauna.IsNoFauna(level))
            {
                return false;
            }

            FaunaSpecies? species = fauna.ForLevel(level);
            return species != null && species.LivesIn(WorldTable.WorldOf(level)) && SightingRule.ShowsCard(level, species.CardLevel, true);
        }
    }
}
