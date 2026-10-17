#nullable enable
using AF.Core.Mechanics;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Fábrica de overlays (contrato de overlays, arquitectura del paso 1 §5): un switch fijo, una clase por mecánica y por
    /// archivo, para que cada tarea de mecánica reescriba solo su archivo. El Barro y Aguas turbias no son tipos de mecánica
    /// (están en el contenido): los pintan <see cref="MudMarks"/> y <see cref="MurkyMarks"/>.
    /// </summary>
    public static class CuiaOverlays
    {
        /// <summary>Crea el overlay de <paramref name="type"/>, o null para <see cref="MechanicType.None"/> (y cualquier tipo sin overlay).</summary>
        public static CuiaOverlay? Create(MechanicType type)
        {
            switch (type)
            {
                case MechanicType.Hoja:
                    return new HojaOverlay();
                case MechanicType.Brote:
                    return new BroteOverlay();
                case MechanicType.Liana:
                    return new LianaOverlay();
                case MechanicType.Matapi:
                    return new MatapiOverlay();
                case MechanicType.Remolino:
                    return new RemolinoOverlay();
                case MechanicType.Lluvia:
                    return new LluviaOverlay();
                case MechanicType.Pirana:
                    return new PiranaOverlay();
                case MechanicType.HojaAtada:
                    return new HojaOverlay(true); // la variante atada de la Hoja (VA1a), en el mismo archivo
                default:
                    return null;
            }
        }
    }
}
