#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;

namespace AF.Presentation.Mechanics
{
    /// <summary>Overlay de M10 Matapi (D-007): esqueleto del contrato; el aspecto llega con su arte.</summary>
    public sealed class MatapiOverlay : CuiaOverlay
    {
        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Matapi;

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            Visible = cuia.Mechanic == Type;
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
        }
    }
}
