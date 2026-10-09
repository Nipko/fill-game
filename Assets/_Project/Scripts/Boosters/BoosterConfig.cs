#nullable enable
using System;
using AF.Economy;
using AF.Platform;

namespace AF.Boosters
{
    /// <summary>
    /// Cifras de los comodines, del rescate y del embudo de bloqueo (GDD 04 §2, §5.2 y §6; D-008) con los <b>defaults
    /// compilados</b> de la tabla única de 08 §9 y sus nombres exactos. Ninguna cifra sale de aquí: <c>BoosterConfigTests</c>
    /// las contrasta con 08 §9, igual que <c>EconomyConfigTests</c> con <see cref="EconomyConfig"/>. Los precios en semillas
    /// (Cuia extra, recarga de Marea, rescate) son de <see cref="EconomyConfig"/>; aquí van cupos, desbloqueos, topes y el
    /// presupuesto del solver. Cuando exista <c>AF.RemoteConfig</c>, sus valores se copiarán en una instancia al abrir cada
    /// pantalla (04 §10.2).
    /// </summary>
    public sealed class BoosterConfig
    {
        /// <summary>Capacidad de la cuia que añaden Cuia extra y Cuia de rescate: siempre 4 (04 §2.2).</summary>
        public const int AddedCuiaCapacity = 4;

        /// <summary>Cuias extra que Capi regala en el nivel de desbloqueo (04 §2.2 y §4: «1 Cuia extra gratis al stock»).</summary>
        public const int ExtraCuiaTutorialUnits = 1;

        /// <summary>Una instancia nueva con los defaults de 08 §9.</summary>
        public static BoosterConfig Defaults => new BoosterConfig();

        /// <summary><c>undo_free_per_level</c>: Mareas gratis por nivel desde el 11 (04 §2.1).</summary>
        [RemoteConfigKey("undo_free_per_level")]
        public int UndoFreePerLevel { get; set; } = 3;

        /// <summary><c>undo_unlimited_until_level</c>: Marea ilimitada y sin contador hasta este nivel (04 §2.1; 02 §13).</summary>
        [RemoteConfigKey("undo_unlimited_until_level")]
        public int UndoUnlimitedUntilLevel { get; set; } = 10;

        /// <summary><c>extra_cuia_unlock_level</c>: la ranura de Cuia extra aparece en este nivel, con 1 regalada (04 §2.2).</summary>
        [RemoteConfigKey("extra_cuia_unlock_level")]
        public int ExtraCuiaUnlockLevel { get; set; } = 8;

        /// <summary><c>extra_cuia_max_per_level</c>: Cuias extra por nivel, por cualquier vía (04 §2.2).</summary>
        [RemoteConfigKey("extra_cuia_max_per_level")]
        public int ExtraCuiaMaxPerLevel { get; set; } = 1;

        /// <summary><c>shuffle_unlock_level</c>: ranura reservada de Pororoca (04 §2.4; se implementa en I6).</summary>
        [RemoteConfigKey("shuffle_unlock_level")]
        public int ShuffleUnlockLevel { get; set; } = 30;

        /// <summary><c>rescue_max_per_level</c>: Cuias de rescate por nivel (04 §6.2; 02 §7).</summary>
        [RemoteConfigKey("rescue_max_per_level")]
        public int RescueMaxPerLevel { get; set; } = 1;

        /// <summary><c>rescue_check_solvable</c>: verificación «la compra cumple» antes de ofrecer el rescate (04 §6.4).</summary>
        [RemoteConfigKey("rescue_check_solvable")]
        public bool RescueCheckSolvable { get; set; } = true;

        /// <summary><c>hint_budget_nodes</c>: presupuesto del A* en cliente (04 §6.4: 40.000 nodos).</summary>
        [RemoteConfigKey("hint_budget_nodes")]
        public int HintBudgetNodes { get; set; } = 40000;

        /// <summary><c>hint_weight</c>: peso del A* en cliente (03 §5).</summary>
        [RemoteConfigKey("hint_weight")]
        public double HintWeight { get; set; } = 2.0;

        /// <summary>
        /// <c>restart_confirm_min_pours</c>: con estos vertidos o más, Volver a la orilla pide confirmación y cuenta como fallo
        /// blando (04 §2.7 y §5.2).
        /// </summary>
        [RemoteConfigKey("restart_confirm_min_pours")]
        public int RestartConfirmMinPours { get; set; } = 8;

        /// <summary><c>relief_after_fails</c>: al llegar a estos fallos se sirve <c>relief[0]</c> (04 §2.7; 03 §13).</summary>
        [RemoteConfigKey("relief_after_fails")]
        public int ReliefAfterFails { get; set; } = 3;

        /// <summary><c>anim_river_lowered_banner_ms</c>: cartel «El río bajó» antes del teñido (04 §2.7; 07 §5.5).</summary>
        [RemoteConfigKey("anim_river_lowered_banner_ms")]
        public int AnimRiverLoweredBannerMs { get; set; } = 2000;

        /// <summary><c>stuck_order_t1</c>: orden del modal en geo T1 (04 §6.6).</summary>
        [RemoteConfigKey("stuck_order_t1")]
        public string StuckOrderT1 { get; set; } = "undo,rescue,balsa,pack";

        /// <summary><c>stuck_order_t3</c>: orden del modal en geo T3, LatAm (04 §6.6).</summary>
        [RemoteConfigKey("stuck_order_t3")]
        public string StuckOrderT3 { get; set; } = "undo_ad,rescue,pack,balsa";

        /// <summary>
        /// <c>stuck_show_shield_status</c>: línea de la Hoja escudo en el modal (04 §8). La Crecida aún no existe en el cliente:
        /// la línea no se pinta hasta que llegue (06 §4.3).
        /// </summary>
        [RemoteConfigKey("stuck_show_shield_status")]
        public bool StuckShowShieldStatus { get; set; } = true;

        /// <summary>Peso entero para <c>SolverOptions</c> (<c>hint_weight</c> redondeado, entre 1 y 16).</summary>
        public int SolverWeight => Math.Max(1, Math.Min(16, (int)Math.Round(HintWeight, MidpointRounding.AwayFromZero)));

        /// <summary>Marea es ilimitada (sin contador) en <paramref name="level"/> (04 §2.1).</summary>
        public bool UndoUnlimited(int level)
        {
            return level <= UndoUnlimitedUntilLevel;
        }

        /// <summary>
        /// En geo <paramref name="t1"/> (T1) o T3, la Balsa de rescate va antes que el Puñado según <c>stuck_order_t1</c> /
        /// <c>stuck_order_t3</c> (04 §6.2: T3 → Puñado y Balsa; T1 → Balsa y Puñado).
        /// </summary>
        public bool BalsaBeforeHandful(bool t1)
        {
            string order = t1 ? StuckOrderT1 : StuckOrderT3;
            int balsa = order.IndexOf("balsa", StringComparison.Ordinal);
            int pack = order.IndexOf("pack", StringComparison.Ordinal);
            return balsa >= 0 && (pack < 0 || balsa < pack);
        }

        /// <summary>La recarga de Marea por video va primero (<c>undo_ad</c> al frente del orden de la geo; 04 §6.6).</summary>
        public bool UndoVideoFirst(bool t1)
        {
            string order = t1 ? StuckOrderT1 : StuckOrderT3;
            return order.StartsWith("undo_ad", StringComparison.Ordinal);
        }

        /// <summary>Comprueba que los valores tienen sentido (no que coincidan con 08 §9: eso lo hacen los tests).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            Require(UndoFreePerLevel >= 0 && UndoUnlimitedUntilLevel >= 0, "undo_* no puede ser negativo");
            Require(ExtraCuiaUnlockLevel >= 1 && ShuffleUnlockLevel >= 1, "los niveles empiezan en 1");
            Require(ExtraCuiaMaxPerLevel >= 0 && RescueMaxPerLevel >= 0, "los topes no pueden ser negativos");
            Require(HintBudgetNodes >= 1, "hint_budget_nodes debe ser >= 1");
            Require(HintWeight >= 1 && HintWeight <= 16, "hint_weight va de 1 a 16");
            Require(RestartConfirmMinPours >= 1 && ReliefAfterFails >= 1, "umbrales >= 1");
            Require(AnimRiverLoweredBannerMs >= 0, "anim_river_lowered_banner_ms no puede ser negativo");
            Require(StuckOrderT1 != null && StuckOrderT3 != null, "stuck_order_* no puede ser null");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("BoosterConfig: " + message + ".");
            }
        }
    }
}
