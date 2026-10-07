#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Defaults compilados de Remote Config que usa el prototipo v0, con el nombre exacto de la clave en la tabla
    /// única de GDD 08 §9 (ningún valor se inventa aquí). Cuando exista <c>AF.RemoteConfig</c>, estos valores pasan a
    /// leerse con <c>RC.Get&lt;T&gt;(key)</c> y esta clase solo queda como respaldo.
    /// </summary>
    public static class GameConfig
    {
        /// <summary><c>futile_rule_enabled</c> (02 §1.4, R16).</summary>
        public const bool FutileRuleEnabled = true;

        /// <summary>
        /// <c>rules_pour_mode</c> (08 §9; 02 §1.2, R9; decisión del dueño D-001 del 2026-10-06): <c>"single"</c>, una dosis
        /// por vertido, o <c>"run"</c>, la racha visible que cabe. Se convierte con
        /// <see cref="AF.Core.Rules.PourModes.TryParse"/>, la única conversión (un valor desconocido se trata como
        /// <see cref="AF.Core.Rules.PourModes.Default"/> y lo avisa quien lo lee), y se aplica al cargar cada nivel, nunca a
        /// mitad (<see cref="LevelFlow.PourMode"/>).
        /// </summary>
        public const string RulesPourMode = "single";

        /// <summary><c>tap_switch_selection</c> (02 §3.1, C5).</summary>
        public const bool TapSwitchSelection = true;

        /// <summary><c>queue_max_lag_pours</c> (02 §3.2).</summary>
        public const int QueueMaxLagPours = 2;

        /// <summary><c>stuck_modal_delay_ms</c> (02 R15).</summary>
        public const int StuckModalDelayMs = 400;

        /// <summary><c>anim_lift_ms</c> (02 §4.1).</summary>
        public const int AnimLiftMs = 120;

        /// <summary><c>anim_travel_ms</c> (02 §4.1).</summary>
        public const int AnimTravelMs = 200;

        /// <summary><c>anim_return_ms</c> (02 §4.1).</summary>
        public const int AnimReturnMs = 160;

        /// <summary><c>anim_pour_dose_ms</c> (02 §4.1; 08 §5.2).</summary>
        public const int AnimPourDoseMs = 100;

        /// <summary><c>anim_pour_tail_ms</c> (02 §4.1; 08 §5.2).</summary>
        public const int AnimPourTailMs = 80;

        /// <summary><c>anim_speed_factor_early</c> (02 §4).</summary>
        public const float AnimSpeedFactorEarly = 1.0f;

        /// <summary><c>anim_speed_factor_late</c> (02 §4).</summary>
        public const float AnimSpeedFactorLate = 0.8f;

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

        /// <summary><c>anim_celebration_ms</c> (02 §2, CELEBRATION).</summary>
        public const int AnimCelebrationMs = 900;

        /// <summary><c>anim_confetti_max_particles</c> (07 §5.4).</summary>
        public const int AnimConfettiMaxParticles = 60;

        /// <summary><c>undo_unlimited_until_level</c>: Marea ilimitada y sin contador hasta este nivel (04 §2.1).</summary>
        public const int UndoUnlimitedUntilLevel = 10;

        /// <summary>
        /// Espera máxima a que la cola visual se vacíe antes de la celebración o del modal (02 §3.2: "máximo ~1 s").
        /// Es un tope de seguridad de la implementación, no un parámetro de diseño: la cola normal termina antes
        /// (el último vertido con su sellado dura ~1,2 s), y si se agota la vista salta a su estado final. Pendiente del
        /// visto bueno del tech-lead: con 1000 ms el sellado del último vertido se cortaría.
        /// </summary>
        public const int MaxViewDrainWaitMs = 2000;

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

        /// <summary>Marea es ilimitada en este nivel (04 §2.1; 02 §13).</summary>
        public static bool UndoUnlimited(int level)
        {
            return level <= UndoUnlimitedUntilLevel;
        }
    }
}
