#nullable enable
using AF.Core.Board;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M2 Brote (GDD 02 §9.1 y §9.2; 03 §9: 1 bit; D-007: la regla no cambia y el debut pasa del 63 al 163): cuia con 1-3
    /// dosis visibles y un tapón-brote en la boca del color de su cima.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Cerrado no es origen (<c>bloqueada_origen</c>).</description></item>
    /// <item><description>Como destino sigue la regla normal (no llena y con la misma cima): solo puede recibir su color.</description></item>
    /// <item><description>Al recibir su color germina en la etapa (b) de R10 y la cuia pasa a ser normal
    /// (<see cref="MechanicType.None"/>; 02 R16 para D-007).</description></item>
    /// <item><description>Como destino cambia al recibir (anexo B); nunca está vacío, así que tampoco podría ser fútil.</description></item>
    /// <item><description>Marea restaura el tapón (estado anterior); Volver a la orilla también.</description></item>
    /// <item><description>Validación: 1-3 dosis visibles, sin ocultas y sin llenar (para que pueda recibir).</description></item>
    /// </list>
    /// </remarks>
    internal sealed class BroteMechanic : MechanicBase
    {
        public override bool BlocksSource(Cuia self)
        {
            return true;
        }

        public override bool ChangesOnReceive(Cuia self)
        {
            return true;
        }

        public override Cuia OnReceived(Cuia self, int color)
        {
            // Tras recibir, la cima es el color recibido: si es el del tapón (la regla normal lo garantiza), germina.
            return self.TopColor == color ? self.WithoutMechanic() : self;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia brote = state[index];
            if (brote.Count < 1 || brote.Count > 3)
            {
                return "02 §9.1 M2: el Brote empieza con 1-3 dosis";
            }

            if (brote.HasHidden)
            {
                return "02 §9.1 M2: las dosis del Brote son visibles";
            }

            if (brote.IsFull)
            {
                return "02 §9.1 M2: el Brote no puede empezar lleno (no podría recibir su color)";
            }

            return null;
        }
    }
}
