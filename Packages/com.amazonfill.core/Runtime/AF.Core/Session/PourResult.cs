#nullable enable
using AF.Core.Rules;

namespace AF.Core.Session
{
    /// <summary>Resultado de un vertido aceptado por <see cref="LevelSession.TryPour"/>.</summary>
    public readonly struct PourResult
    {
        /// <summary>Crea un resultado sin el detalle de las etapas de R10 (<see cref="Events"/> queda por defecto).</summary>
        public PourResult(Pour pour, bool targetSealed, LevelStatus status)
            : this(pour, targetSealed, status, default)
        {
        }

        /// <summary>Crea un resultado con lo ocurrido en cada etapa de R10 (anexo B de D-007).</summary>
        public PourResult(Pour pour, bool targetSealed, LevelStatus status, PourEvents events)
        {
            Pour = pour;
            TargetSealed = targetSealed;
            Status = status;
            Events = events;
        }

        /// <summary>Vertido ejecutado, con la cantidad real movida (R9).</summary>
        public Pour Pour { get; }

        /// <summary>
        /// La cuia destino quedó sellada con este vertido (R5, R10 c). El origen no puede sellar al verter:
        /// pierde dosis y deja de estar llena.
        /// </summary>
        public bool TargetSealed { get; }

        /// <summary>Estado de la partida tras el vertido (R10 g, h).</summary>
        public LevelStatus Status { get; }

        /// <summary>
        /// Etapas de R10 en el orden del anexo B: revelado, aperturas (Hoja, Brote), sellados, liberaciones (Liana),
        /// contadores y condiciones, para que la presentación las anime en ese orden.
        /// </summary>
        public PourEvents Events { get; }

        /// <summary>El vertido lavó el Barro de la cima de la cuia que recibió (evento BarroWashed; 02 §9.1 M5).</summary>
        public bool BarroWashed => Events.BarroWashedMask != 0;
    }
}
