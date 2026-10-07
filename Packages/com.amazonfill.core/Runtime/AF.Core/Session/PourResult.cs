#nullable enable
using AF.Core.Rules;

namespace AF.Core.Session
{
    /// <summary>Resultado de un vertido aceptado por <see cref="LevelSession.TryPour"/>.</summary>
    public readonly struct PourResult
    {
        /// <summary>Crea un resultado.</summary>
        public PourResult(Pour pour, bool targetSealed, LevelStatus status)
        {
            Pour = pour;
            TargetSealed = targetSealed;
            Status = status;
        }

        /// <summary>Vertido ejecutado, con la cantidad real movida (R9).</summary>
        public Pour Pour { get; }

        /// <summary>
        /// La cuia destino quedó sellada con este vertido (R5, R10 c). El origen no puede sellar al verter:
        /// pierde dosis y deja de estar llena.
        /// </summary>
        public bool TargetSealed { get; }

        /// <summary>Estado de la partida tras el vertido (R10 e, f).</summary>
        public LevelStatus Status { get; }
    }
}
