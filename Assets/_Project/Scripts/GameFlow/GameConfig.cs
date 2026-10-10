#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Defaults compilados de Remote Config del flujo de nivel (los de comodines y rescate están en
    /// <c>AF.Boosters.BoosterConfig</c>; los de la economía, en <c>AF.Economy.EconomyConfig</c>), con el nombre exacto de la clave en la tabla
    /// única de GDD 08 §9 (ningún valor se inventa aquí). Cuando exista <c>AF.RemoteConfig</c>, estos valores pasan a
    /// leerse con <c>RC.Get&lt;T&gt;(key)</c> y esta clase solo queda como respaldo.
    /// </summary>
    public static class GameConfig
    {
        /// <summary><c>futile_rule_enabled</c> (02 §1.4, R16).</summary>
        public const bool FutileRuleEnabled = true;

        /// <summary>
        /// <c>rules_pour_mode</c> (08 §9; 02 §1.2, R9; decisión del dueño D-005 del 2026-10-08, que sustituye a D-001):
        /// <c>"run"</c>, la racha visible del mismo color que cabe en el destino, con vertido parcial (como Magic Sort), o
        /// <c>"single"</c>, una dosis por vertido, que queda solo para pruebas. Se convierte con
        /// <see cref="AF.Core.Rules.PourModes.TryParse"/>, la única conversión (un valor desconocido se trata como
        /// <see cref="AF.Core.Rules.PourModes.Default"/> y lo avisa quien lo lee), y se aplica al cargar cada nivel, nunca a
        /// mitad (<see cref="LevelFlow.PourMode"/>).
        /// </summary>
        public const string RulesPourMode = "run";

        /// <summary><c>tap_switch_selection</c> (02 §3.1, C5).</summary>
        public const bool TapSwitchSelection = true;

        /// <summary><c>queue_max_lag_pours</c> (02 §3.2).</summary>
        public const int QueueMaxLagPours = 2;

        /// <summary><c>stuck_modal_delay_ms</c> (02 R15).</summary>
        public const int StuckModalDelayMs = 400;

        /// <summary><c>anim_lift_ms</c> (02 §4.1; D-006: Magic Sort sube en ≤ 100 ms, ficha §2.4).</summary>
        public const int AnimLiftMs = 100;

        /// <summary><c>anim_travel_ms</c> (02 §4.1; D-006: viaje e inclinación hasta la boca de 200-300 ms en Magic Sort).</summary>
        public const int AnimTravelMs = 300;

        /// <summary><c>anim_return_ms</c> (02 §4.1; D-006: vuelta directa de ≈ 300 ms en Magic Sort).</summary>
        public const int AnimReturnMs = 300;

        /// <summary>
        /// <c>anim_pour_dose_ms</c> (02 §4.1; 08 §5.2; D-006: ≈ 285 ms por dosis en Magic Sort; rango 80-300 en 08 §9). De la
        /// salida al reposo: 870 ms con 1 dosis, 1.150 con 2 y 1.430 con 3.
        /// </summary>
        public const int AnimPourDoseMs = 280;

        /// <summary><c>anim_pour_tail_ms</c> (02 §4.1; 08 §5.2; D-006).</summary>
        public const int AnimPourTailMs = 80;

        /// <summary><c>anim_speed_factor_early</c> (02 §4).</summary>
        public const float AnimSpeedFactorEarly = 1.0f;

        /// <summary>
        /// <c>anim_speed_factor_late</c> (02 §4; 08 §9, rango 0,6-1,0): 1,0, como en los niveles 1-20. Los tiempos de
        /// Magic Sort que copia D-006 (punto 2: ≈ 0,9 s de la salida al reposo con 1 dosis; ficha §2.6) se midieron en los
        /// niveles 387-389, así que desde el nivel 21 tampoco se acelera (revisión de D-006: con 0,8 el vertido de 1 dosis
        /// bajaba a 696 ms). Si el dueño decide que desde el 21 se vierta más rápido que Magic Sort, se anota en D-006.
        /// </summary>
        public const float AnimSpeedFactorLate = 1.0f;

        /// <summary><c>anim_fast_from_level</c> (02 §4).</summary>
        public const int AnimFastFromLevel = 21;

        /// <summary><c>anim_tint_duration_ms_early</c>: Sumergir y teñir en los niveles 1-20 (02 §4.2; 07 §5.1).</summary>
        public const int AnimTintDurationMsEarly = 1400;

        /// <summary><c>anim_tint_duration_ms_late</c>: versión corta (02 §4.2; también el INTRO corto de un reinicio, 04 §2.7).</summary>
        public const int AnimTintDurationMsLate = 800;

        /// <summary><c>anim_tint_fast_from_level</c> (02 §4.2).</summary>
        public const int AnimTintFastFromLevel = 21;

        /// <summary><c>anim_tint_skippable</c>: nunca false en producción (08 §9).</summary>
        public const bool AnimTintSkippable = true;

        /// <summary>
        /// Con «Reducir movimiento» activo, la intro se reemplaza por un fundido de 0,25 s (07 §5.1 y §7.8; guion de la intro
        /// §9). No es una clave de 08 §9: el ajuste aún no existe y lo activa quien lo tenga (<see cref="LevelFlow.ReduceMotion"/>).
        /// </summary>
        public const int ReducedMotionIntroMs = 250;

        /// <summary><c>anim_celebration_ms</c> (02 §2, CELEBRATION).</summary>
        public const int AnimCelebrationMs = 900;

        /// <summary><c>anim_confetti_max_particles</c> (07 §5.4).</summary>
        public const int AnimConfettiMaxParticles = 60;

        /// <summary>
        /// La Cosecha de la pantalla de victoria cuenta hacia arriba en 400 ms (02 §6: «la cifra cuenta hacia arriba en
        /// 400 ms»; D-008 punto 3). No es una clave de 08 §9.
        /// </summary>
        public const int HarvestCountMs = 400;

        /// <summary>
        /// <c>ui_show_difficulty_subtitles</c> (08 §9, fila combinada con <c>ui_river_fact_every_n_levels</c>,
        /// <c>ui_murky_bonus_enabled</c> y <c>ui_mascot_rescue_anim</c>; default true): muestra los subtítulos temáticos
        /// «Aguas bravas» y «Noche de jaguar» bajo la etiqueta honesta (02 §10; glosario §6.1). Constante sin
        /// <c>[RemoteConfigKey]</c>, como el resto de esta clase: <c>EconomyConfigTests</c> solo recorre <c>EconomyConfig</c>.
        /// </summary>
        public const bool UiShowDifficultySubtitles = true;

        /// <summary>
        /// <c>live_content_end_message</c> = <c>{"next_batch_eta_days":14}</c> (08 §9): días estimados hasta los próximos 100
        /// niveles, que muestra la pantalla «El río sigue creciendo» (06 §9; 08 §6.2).
        /// </summary>
        public const int LiveContentEndNextBatchEtaDays = 14;

        /// <summary>
        /// Tope de seguridad [P del tech-lead] de la espera a que la cola visual se vacíe antes de la celebración o del
        /// modal: 3000 ms. No es un parámetro de diseño: 02 §3.2 dice que la espera no tiene un máximo de diseño y depende de
        /// lo que quede en la cola (duraciones de 02 §4.1). Medido en el nivel 2 del tutorial, tocando cada 1,0 s la
        /// solución en <c>run</c>: la cola se vacía ≈ 2,0 s después del último toque, porque el último vertido (1→2, 3
        /// dosis) espera a que termine el anterior (2→4, 3 dosis), que toca la misma cuia. Si el tope se agota, la vista
        /// salta a su estado final. Como referencia de la implementación (no de 02 §3.2), un vertido de 3 dosis con su
        /// sellado dura ≈ 1,56 s con los tiempos de D-006.
        /// </summary>
        public const int MaxViewDrainWaitMs = 3000;

        /// <summary>Duración de Sumergir y teñir para el nivel <paramref name="level"/> (02 §4.2).</summary>
        public static int TintDurationMs(int level)
        {
            return level < AnimTintFastFromLevel ? AnimTintDurationMsEarly : AnimTintDurationMsLate;
        }

        /// <summary>Factor de velocidad de las animaciones de juego para el nivel <paramref name="level"/> (02 §4).</summary>
        public static float SpeedFactor(int level)
        {
            return level < AnimFastFromLevel ? AnimSpeedFactorEarly : AnimSpeedFactorLate;
        }
    }
}
