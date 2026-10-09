#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;

namespace AF.Presentation.Mechanics
{
    /// <summary>Overlay de M3 Liana (02 §9.1; D-007): esqueleto del contrato; el aspecto llega con su arte.</summary>
    public sealed class LianaOverlay : CuiaOverlay
    {
        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Liana;

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
