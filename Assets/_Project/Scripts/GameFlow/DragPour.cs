#nullable enable

namespace AF.GameFlow
{
    /// <summary>Qué hacer al soltar el dedo fuera de la cuia que se pulsó (<see cref="DragPour.Resolve"/>).</summary>
    public enum DragOutcome
    {
        /// <summary>Nada: soltar fuera cancela (sobre el fondo, sobre otra cosa o sin arrastrar lo bastante).</summary>
        Cancel = 0,

        /// <summary>Vertido X→Y: se interpreta como el toque en X y el toque en Y (C1 y después C4-C6).</summary>
        Pour = 1,
    }

    /// <summary>
    /// C10, el arrastre (02 §3.1; GDD §2 #3; tarea B11): «Dedo baja en X y sube en Y ≠ X tras desplazarse ≥ 24 dp → se interpreta
    /// como X→Y (compatibilidad con hábito de arrastre) [P]; soltar sobre el fondo cancela». La elevación empieza en el
    /// touch-down y la decisión tap/arrastre se toma en el touch-up (02 §3.1). Interruptor <c>drag_pour_enabled</c> (08 §9: bool,
    /// default true; <see cref="EnabledDefault"/>). Regla pura, sin motor y sin asignaciones; la vista (<c>GameScreen.OnCuiaUp</c>,
    /// parche B11-drag) le pasa dónde soltó y cuánto se movió, y con <see cref="DragOutcome.Pour"/> llama al toque en X y al toque en
    /// Y: las reglas del vertido siguen siendo las de <see cref="TapController"/> y del núcleo, aquí no se repite ninguna.
    /// <para>Solo arrastra quien pulsó una cuia sin nada seleccionado y que puede ser origen (la que se eleva en el touch-down,
    /// C1) [P]: con una cuia ya seleccionada, soltar fuera cancela como antes. Soltar dentro de la propia X es un toque, y eso lo
    /// resuelve la vista antes de preguntar aquí.</para>
    /// </summary>
    public static class DragPour
    {
        /// <summary>Desplazamiento mínimo del arrastre (dp; 02 §3.1, C10).</summary>
        public const float MinDistanceDp = 24f;

        /// <summary>Default de <c>drag_pour_enabled</c> (08 §9).</summary>
        public const bool EnabledDefault = true;

        /// <summary>
        /// Qué hacer al soltar fuera de <paramref name="downIndex"/>: con <paramref name="enabled"/> (<c>drag_pour_enabled</c>),
        /// <paramref name="liftedAtDown"/> (la cuia se elevó en el touch-down: no había selección y puede ser origen), una cuia
        /// <paramref name="upIndex"/> ≠ X bajo el dedo (−1 = fondo u otra cosa) y al menos <see cref="MinDistanceDp"/> dp de
        /// desplazamiento (<paramref name="distanceDp"/>), un vertido; si no, cancelar. Sin asignaciones.
        /// </summary>
        public static DragOutcome Resolve(bool enabled, bool liftedAtDown, int downIndex, int upIndex, float distanceDp)
        {
            if (!enabled || !liftedAtDown || downIndex < 0 || upIndex < 0 || upIndex == downIndex || !(distanceDp >= MinDistanceDp))
            {
                return DragOutcome.Cancel;
            }

            return DragOutcome.Pour;
        }
    }
}
