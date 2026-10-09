#nullable enable
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// Tabla estática de despacho de <see cref="IMechanic"/>, indexada por el código de mecánica de la cuia (bits 39-42 de
    /// <see cref="Cuia.Packed"/>). Una instancia sin estado por tipo, creada una sola vez; los códigos sin mecánica asignada
    /// usan <see cref="MechanicBase"/> (comportamiento de cuia normal).
    /// </summary>
    internal static class MechanicTable
    {
        /// <summary>Número de códigos representables (4 bits).</summary>
        public const int Size = 16;

        private static readonly IMechanic Normal = new NormalMechanic();
        private static readonly IMechanic[] Table = Build();

        /// <summary>Mecánica de <paramref name="cuia"/> (la normal si no lleva ninguna).</summary>
        public static IMechanic Of(Cuia cuia)
        {
            return Table[cuia.MechanicCode];
        }

        /// <summary>
        /// Las reglas de <paramref name="type"/> ya están portadas al núcleo. Un tablero con una mecánica sin portar se
        /// rechaza al construirlo (fallar cerrado): con la tabla por defecto se jugaría como una cuia normal.
        /// </summary>
        public static bool IsPorted(MechanicType type)
        {
            return type == MechanicType.None || (MechanicTypes.IsDefined(type) && !ReferenceEquals(Table[(int)type], Normal));
        }

        private static IMechanic[] Build()
        {
            var table = new IMechanic[Size];
            for (int i = 0; i < table.Length; i++)
            {
                table[i] = Normal;
            }

            table[(int)MechanicType.Hoja] = new HojaMechanic();
            table[(int)MechanicType.Brote] = new BroteMechanic();
            table[(int)MechanicType.Matapi] = new MatapiMechanic();
            table[(int)MechanicType.Liana] = new LianaMechanic();
            return table;
        }

        /// <summary>Cuia normal: todos los ganchos con el comportamiento de 02 §1 sin mecánicas.</summary>
        private sealed class NormalMechanic : MechanicBase
        {
        }
    }

    /// <summary>
    /// Comportamiento por defecto de cada gancho de <see cref="IMechanic"/>: el de una cuia normal (02 §1). Cada mecánica
    /// solo sobrescribe lo que cambia.
    /// </summary>
    internal abstract class MechanicBase : IMechanic
    {
        public virtual bool BlocksSource(Cuia self)
        {
            return false;
        }

        public virtual TargetVerdict CheckTarget(in BoardSlice board, int source, int target)
        {
            return TargetVerdict.Normal;
        }

        public virtual int ReceiveSpace(in BoardSlice board, int target)
        {
            return board[target].Space;
        }

        public virtual int ReceiverOf(in BoardSlice board, int target)
        {
            return target;
        }

        public virtual int Receive(in BoardSlice board, int target, int color, int amount)
        {
            board.Set(target, board[target].AddOnTop(color, amount));
            return target;
        }

        public virtual bool ChangesOnReceive(Cuia self)
        {
            return false;
        }

        public virtual bool ExemptAsSource(Cuia self)
        {
            return false;
        }

        public virtual Cuia OnReceived(Cuia self, int color)
        {
            return self;
        }

        public virtual bool ReleasedBySeal(in BoardSlice board, int index)
        {
            return false;
        }

        public virtual bool AdvanceCounter(in BoardSlice board, int index, ref PourEvents events)
        {
            return false;
        }

        public virtual bool CheckCondition(in BoardSlice board, int index, ref PourEvents events)
        {
            return false;
        }

        public virtual bool BlocksVictory(Cuia self)
        {
            return false;
        }

        public virtual string? ValidateInitial(BoardState state, int index)
        {
            return null;
        }
    }
}
