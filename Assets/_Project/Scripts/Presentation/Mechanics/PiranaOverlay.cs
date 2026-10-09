#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;

namespace AF.Presentation.Mechanics
{
    /// <summary>Overlay de M7 Piraña (02 §9.1): esqueleto del contrato; el aspecto llega con su arte.</summary>
    public sealed class PiranaOverlay : CuiaOverlay
    {
        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Pirana;

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
