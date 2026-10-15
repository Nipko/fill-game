#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Defaults compilados del Mapa del río (D-013) y de la línea de la victoria, con el patrón de <see cref="GameConfig"/>: las
    /// claves de Remote Config con el nombre exacto de la tabla única de GDD 08 §9 (ningún valor se inventa aquí) y, aparte, las
    /// constantes [P] de la propuesta y del plan del Mapa, cada una con su fuente. Cuando exista <c>AF.RemoteConfig</c>, las
    /// claves pasan a leerse con <c>RC.Get&lt;T&gt;(key)</c> y esta clase queda como respaldo. Sin motor.
    /// </summary>
    public static class MapConfig
    {
        /// <summary>Valor <c>stops</c> de <c>ui_map_mode</c>: 100 paradas, una cada 10 niveles.</summary>
        public const string MapModeStops = "stops";

        /// <summary>Valor <c>worlds</c> de <c>ui_map_mode</c>: solo las grandes paradas, una por mundo.</summary>
        public const string MapModeWorlds = "worlds";

        /// <summary>Valor <c>l10</c> de <c>ui_map_autopen</c>: el Mapa solo sale solo en el 10.</summary>
        public const string AutopenL10 = "l10";

        /// <summary>Valor <c>l10_world</c> de <c>ui_map_autopen</c>: en el 10 y con el viaje de mundo dentro del banner.</summary>
        public const string AutopenL10World = "l10_world";

        /// <summary>
        /// <c>ui_map_mode</c> (08 §9; D-013, Q1-C de la propuesta del Mapa): <see cref="MapModeStops"/> o
        /// <see cref="MapModeWorlds"/>, un filtro de la misma pantalla y del mismo catálogo [P].
        /// </summary>
        public const string UiMapMode = MapModeStops;

        /// <summary>
        /// <c>ui_map_autopen</c> (08 §9; D-013, Q4-D): <see cref="AutopenL10World"/> o <see cref="AutopenL10"/> [P]
        /// (<see cref="MapMoments.TravelInBanner"/>).
        /// </summary>
        public const string UiMapAutopen = AutopenL10World;

        /// <summary><c>ui_bg_per_stop</c> (08 §9; D-013, Q5-A): fondo por parada; false = el de la gran parada del mundo [P].</summary>
        public const bool UiBgPerStop = true;

        /// <summary><c>ui_stop_label</c> (08 §9; D-013): rótulo del lugar en el primer nivel de cada parada [P].</summary>
        public const bool UiStopLabel = true;

        /// <summary><c>ui_victory_view</c> (08 §9; D-013, Q6-A): victoria con vista [P].</summary>
        public const bool UiVictoryView = true;

        /// <summary>
        /// <c>anim_map_world_ms</c> (08 §9; D-013; rango 3000-8000): tope del viaje de mundo en la pantalla del Mapa. Dentro del
        /// banner manda <see cref="WorldTravelInBannerMaxMs"/>.
        /// </summary>
        public const int AnimMapWorldMs = 6000;

        /// <summary><c>ui_map_quiz</c> (08 §9; D-013, Q12-A): «¿Verdad o mito?», apagado en la 1.0 (<c>off</c>) [P].</summary>
        public const string UiMapQuiz = "off";

        /// <summary>
        /// <c>ui_river_fact_every_n_levels</c> (08 §9, fila combinada con <c>ui_show_difficulty_subtitles</c>; D-013; pregunta 45,
        /// (a), D-022): cadencia de la llegada del Mapa con su dato, cada 10 niveles, la de las paradas
        /// (<see cref="VictoryContext.RiverFactEveryN"/>).
        /// </summary>
        public const int UiRiverFactEveryNLevels = 10;

        /// <summary>
        /// Política del dato de llegada en la victoria (arquitectura de la tanda 4, §2): <see cref="RiverFactPolicy.Canon"/>, lo
        /// que dice hoy el canon (llegada con dato cada 10 niveles; pregunta 45, (a), D-022). Con la (b) se cambia esta constante;
        /// con la (c) o la (d), haría falta una tarea aparte. No es una clave de 08 §9.
        /// </summary>
        public const RiverFactPolicy RiverPolicy = RiverFactPolicy.Canon;

        /// <summary>
        /// Política de la línea en los hitos de TiTino [P] (P-M3 del plan del Mapa; T6-A): <see cref="HitoLinePolicy.ArrivalEverywhere"/>
        /// hasta que TI6a pinte a TiTino en la victoria y la pase a <see cref="HitoLinePolicy.SlothHito"/>. No es una clave de 08 §9.
        /// </summary>
        public const HitoLinePolicy HitoPolicy = HitoLinePolicy.ArrivalEverywhere;

        /// <summary>Franja de llegada de la victoria del <c>10k</c>: 1.200 ms (D-013, punto 8; 02 §6, fila Llegada) [P].</summary>
        public const int ArrivalStripMs = 1200;

        /// <summary>Fundido del fondo al cambiar de parada: 250 ms (D-013, punto 9) [P].</summary>
        public const int StopCrossfadeMs = 250;

        /// <summary>Llenado de la mini cuia del sitio en la llegada: 600 ms (02 §6, fila Llegada) [P].</summary>
        public const int ArrivalSampleFillMs = 600;

        /// <summary>Fundido que sustituye a las animaciones del Mapa con Reducir movimiento: 150 ms (propuesta del Mapa §4.2) [P].</summary>
        public const int ReducedMotionFadeMs = 150;

        /// <summary>Ponerse al día en el Mapa: 150 ms por boya (propuesta del Mapa §4.2) [P].</summary>
        public const int CatchUpPerBuoyMs = 150;

        /// <summary>Tope de ponerse al día en el Mapa: 1.500 ms (propuesta del Mapa §4.2) [P].</summary>
        public const int CatchUpMaxMs = 1500;

        /// <summary>Izado de la bandera al cambiar de país: 800 ms (08 §9, columna «Uso» de <c>anim_map_world_ms</c>) [P].</summary>
        public const int FlagRaiseMs = 800;

        /// <summary>Tope del viaje de mundo dentro del banner: ≤ 4 s (D-013, punto 7; Q4-D) [P].</summary>
        public const int WorldTravelInBannerMaxMs = 4000;

        /// <summary>Tope de la secuencia del Mapa del 10: 10 s (06 §2.3) [P].</summary>
        public const int L10MapMaxMs = 10000;

        /// <summary>Giro de la vista Norte arriba: 300 ms (propuesta del Mapa §3.1) [P].</summary>
        public const int NorthUpRotateMs = 300;

        /// <summary>Escala de la vista Norte arriba: 0,76 (propuesta del Mapa §3.1) [P].</summary>
        public const float NorthUpScale = 0.76f;

        /// <summary>Fundido del llenado de un afluente al pasar su boca: 300 ms (propuesta del Mapa §3.4) [P].</summary>
        public const int PassbyFillFadeMs = 300;

        /// <summary>Reposo del Mapa sin toques antes de dormir el render: 30 s (D-013, punto 6) [P].</summary>
        public const int MapIdleSleepMs = 30000;

        /// <summary>Objetivo táctil mínimo del Mapa: 48 dp (propuesta del Mapa §3.2) [P].</summary>
        public const int MinTapDp = 48;

        /// <summary>Distancia mínima entre marcadores antes de agruparlos: 48 dp (propuesta del Mapa §3.2) [P].</summary>
        public const int ClusterMinDp = 48;
    }
}
