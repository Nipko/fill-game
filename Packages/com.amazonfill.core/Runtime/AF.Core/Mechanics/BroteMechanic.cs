#nullable enable
using System.Globalization;
using AF.Core.Board;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M2 Brote (GDD 02 §9.1 y §9.2; 03 §9: 1 bit; D-007: la regla no cambia y el debut pasa del 63 al 163): cuia con 1-3
    /// dosis visibles y un tapón-brote en la boca del color de su cima. Con <c>n</c> = 2 es el <b>Brote de dos hojas</b>
    /// (variante de la 1.0, debut 343; 02 R10 b; propuesta de D-007 b.3; firma de <c>pack_schema</c> 3, pregunta 8).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Cerrado no es origen (<c>bloqueada_origen</c>).</description></item>
    /// <item><description>Como destino sigue la regla normal (no llena y con la misma cima): solo puede recibir su color.</description></item>
    /// <item><description>Al recibir su color germina en la etapa (b) de R10 y la cuia pasa a ser normal
    /// (<see cref="MechanicType.None"/>; 02 R16 para D-007).</description></item>
    /// <item><description>Como destino cambia al recibir (anexo B); nunca está vacío, así que tampoco podría ser fútil.</description></item>
    /// <item><description>Marea restaura el tapón (estado anterior); Volver a la orilla también.</description></item>
    /// <item><description>Validación: 1-3 dosis visibles, sin ocultas y con hueco para las dosis que lo abren (sin llenar,
    /// para que pueda recibir).</description></item>
    /// </list>
    /// <para><b>Brote de dos hojas</b> (02 R10 b: «resta las dosis recibidas»; b.3: «germina al recibir 2 dosis de su color, en
    /// uno o varios vertidos»): <see cref="Cuia.MechanicN"/> guarda las hojas cerradas que le faltan (2 al empezar, 1 tras
    /// recibir una dosis; 0 en el Brote normal, que equivale a una). Cada vertido de su color resta las dosis recibidas, en la
    /// etapa (b); al llegar a 0 germina como el Brote normal. En <c>run</c> un tramo de 2 lo abre de una vez (D-005; 02 R9); en
    /// <c>single</c> hacen falta dos vertidos. El contador va en la cuia (<see cref="Cuia.Packed"/>), así que entra en el hash y en
    /// la clave canónica (03 §3) sin código propio. El validador exige espacio ≥ 2 (b.3): con capacidad 4, empieza con 2 dosis
    /// como mucho. Es seguro: cerrado no es origen y el color que entra no puede salir. Abrir una hoja sin germinar es el evento
    /// <see cref="AF.Core.Rules.PourEvents.BroteLeafOpenedMask"/>.</para>
    /// </remarks>
    internal sealed class BroteMechanic : MechanicBase
    {
        /// <summary><c>n</c> del Brote de dos hojas (firma de <c>pack_schema</c> 3: <c>n</c> ∈ {1, 2}; con 1 es el Brote normal).</summary>
        public const int TwoLeaves = 2;

        public override bool BlocksSource(Cuia self)
        {
            return true;
        }

        public override bool ChangesOnReceive(Cuia self)
        {
            return true;
        }

        public override Cuia OnReceived(Cuia self, int color, int amount)
        {
            // Tras recibir, la cima es el color recibido: si es el del tapón (la regla normal lo garantiza), resta las dosis
            // recibidas de las hojas que le faltan (0 = Brote normal, una hoja) y germina al llegar a 0 (02 R10 b).
            if (self.TopColor != color)
            {
                return self;
            }

            int leaves = self.MechanicN;
            int remaining = (leaves == 0 ? 1 : leaves) - amount;
            return remaining <= 0 ? self.WithoutMechanic() : self.WithMechanicN(remaining);
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

            // n = 0 es el Brote normal (una hoja) y n = 2, el de dos hojas con las dos cerradas (BoardBuilder lee n ∈ {1, 2} así);
            // n = 1 solo existe a mitad de partida, tras abrir una hoja.
            int leaves = brote.MechanicN;
            if (leaves != 0 && leaves != TwoLeaves)
            {
                return "propuesta de D-007 b.3: el Brote empieza con 1 o 2 hojas cerradas (n = "
                    + leaves.ToString(CultureInfo.InvariantCulture) + " no es un tablero inicial)";
            }

            if (leaves == TwoLeaves && brote.Space < TwoLeaves)
            {
                return "02 R10 b y propuesta de D-007 b.3: el Brote de dos hojas empieza con espacio para 2 dosis (aquí "
                    + brote.Space.ToString(CultureInfo.InvariantCulture) + ")";
            }

            return null;
        }
    }
}
