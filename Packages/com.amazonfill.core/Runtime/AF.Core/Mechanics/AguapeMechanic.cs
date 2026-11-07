#nullable enable
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M8 Aguapé (1.1; D-068, P4 A, P5 A y P13 A [P]; 02 §9.1 M8; 03 §9; propuesta de la curva, §4.2 a): una mata de aguapé sujeta
    /// un grupo de 2-5 cuias contiguas. Es el «hielo» de Magic Sort con la regla del río: la congelada que recibe pero no da.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>Filtro de origen</b> (regla 2): mientras la mata siga, las cuias del grupo no son origen (R11, rechazo
    /// sin texto); como destino siguen la regla normal (<see cref="TargetVerdict.Normal"/>). Su contenido se ve.</description></item>
    /// <item><description><b>Suelta</b> (regla 3; R10, etapa d, con las Lianas): en el mismo vertido en que <c>n</c> cuias del grupo
    /// quedan selladas (<c>n</c> = 1; 2 en el Aguapé espeso), se suelta todo el grupo, la cuia que selló incluida. Se deduce del
    /// estado: las cuias del grupo guardan su mecánica (con el grupo y <c>n</c>) hasta la suelta, también la que ya selló, y la
    /// suelta las deja normales (principio de 02 R16 para D-007). <see cref="BoardRules"/> decide todas las sueltas de una pasada
    /// antes de aplicarlas, así que la cuia que se suelta primero no deja de contar para las demás de su grupo.</description></item>
    /// <item><description><b><c>futil</c>, R13 y R14</b> (regla 5): bajo la mata no es origen, así que no hay vaivén; como destino
    /// rige la <c>futil</c> normal, sin exención (anexo B de D-007).</description></item>
    /// <item><description><b>Ayudas</b> (regla 7): Marea restaura el estado anterior entero, así que si deshace el sello que la
    /// soltó, vuelve la mata; la Pororoca deja fijas las cuias bajo la mata (<c>Shuffler.IsFixed</c>: toda cuia con mecánica que
    /// no sea Remolino).</description></item>
    /// <item><description><b>Validación</b> (regla 6; 03 §17 #6), una vez por grupo, en su primera cuia: el grupo es contiguo (índices
    /// consecutivos de <c>cuias</c>; que no se parta entre filas lo comprueba <see cref="LevelValidation.ValidateRows"/>), tiene
    /// de 2 a 5 cuias (de 4 a 5 en el espeso) y el mismo <c>n</c>, y al menos una de sus cuias (dos, con frutas distintas, en el
    /// espeso) puede sellar solo recibiendo: está vacía o es monocolor de una fruta <c>c</c>, y su capacidad es la <c>U_c</c> de
    /// esa fruta. Ninguna empieza sellada (03 §17 #5) y el nivel no empieza atascado (02 §9.2 regla 1): las comprueba
    /// <see cref="LevelValidation"/> para todo el tablero.</description></item>
    /// </list>
    /// </remarks>
    internal sealed class AguapeMechanic : MechanicBase
    {
        /// <summary><c>n</c> del Aguapé espeso: dos sellos dentro del grupo (P13 A).</summary>
        public const int ThickSeals = 2;

        /// <summary>Cuias mínimas de un grupo (02 §9.1 M8, regla 1).</summary>
        public const int MinGroupSize = 2;

        /// <summary>Cuias máximas de un grupo (02 §9.1 M8, regla 1).</summary>
        public const int MaxGroupSize = 5;

        /// <summary>Cuias mínimas de un grupo espeso (02 §9.1 M8: «un grupo de 4-5 cuias»).</summary>
        public const int MinThickGroupSize = 4;

        public override bool BlocksSource(Cuia self)
        {
            return true;
        }

        public override bool ReleasedBySeal(in BoardSlice board, int index)
        {
            Cuia self = board[index];
            int group = self.MechanicPairId;
            int needed = self.MechanicN;
            int seals = 0;
            for (int j = 0; j < board.Count; j++)
            {
                Cuia cuia = board[j];
                if (cuia.Mechanic == MechanicType.Aguape && cuia.MechanicPairId == group && board.IsSealed(j))
                {
                    seals++;
                    if (seals >= needed)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia self = state[index];
            int group = self.MechanicPairId;
            int first = FirstOfGroup(state, group);
            if (first != index)
            {
                return null; // el grupo se valida una vez, en su primera cuia
            }

            int size = 0;
            int last = index;
            for (int j = index; j < state.Count; j++)
            {
                Cuia cuia = state[j];
                if (cuia.Mechanic == MechanicType.Aguape && cuia.MechanicPairId == group)
                {
                    size++;
                    last = j;
                    if (cuia.MechanicN != self.MechanicN)
                    {
                        return "02 §9.1 M8: todas las cuias del grupo llevan el mismo n";
                    }
                }
            }

            if (last - index + 1 != size)
            {
                return "02 §9.1 M8 (regla 1): el grupo " + Num(group) + " no es contiguo (cuias " + Num(index) + " a " + Num(last) + ")";
            }

            bool thick = self.MechanicN == ThickSeals;
            int min = thick ? MinThickGroupSize : MinGroupSize;
            if (size < min || size > MaxGroupSize)
            {
                return "02 §9.1 M8 (regla 1): un grupo " + (thick ? "espeso tiene de 4 a 5 cuias" : "tiene de 2 a 5 cuias") + "; el " + Num(group) + " tiene " + Num(size);
            }

            int[] units = state.UnitsTable;
            if (!thick)
            {
                for (int j = index; j <= last; j++)
                {
                    if (SealableColors(state[j], units) != 0)
                    {
                        return null;
                    }
                }

                return "02 §9.1 M8 (regla 6): ninguna cuia del grupo puede sellar solo recibiendo";
            }

            for (int a = index; a <= last; a++)
            {
                int first2 = SealableColors(state[a], units);
                if (first2 == 0)
                {
                    continue;
                }

                for (int b = a + 1; b <= last; b++)
                {
                    int second = SealableColors(state[b], units);
                    if (second != 0 && (first2 != second || (first2 & (first2 - 1)) != 0))
                    {
                        return null; // dos cuias que pueden sellar con dos frutas distintas
                    }
                }
            }

            return "02 §9.1 M8 (regla 6): en el Aguapé espeso dos cuias del grupo tienen que poder sellar solo recibiendo, con frutas distintas";
        }

        /// <summary>
        /// Máscara de las frutas con las que la cuia puede sellar solo recibiendo (bit <c>c</c>): vacía, cualquier fruta con
        /// <c>U_c</c> = su capacidad; monocolor de una fruta <c>c</c> sin ocultas ni Barro, <c>c</c> si <c>U_c</c> = su capacidad.
        /// </summary>
        internal static int SealableColors(Cuia cuia, int[] units)
        {
            int capacity = cuia.Capacity;
            if (cuia.IsEmpty)
            {
                int mask = 0;
                for (int c = Cuia.MinColor; c <= Cuia.MaxTintColor; c++)
                {
                    if (units[c] == capacity)
                    {
                        mask |= 1 << c;
                    }
                }

                return mask;
            }

            int top = cuia.TopColor;
            if (top > Cuia.MaxTintColor || cuia.HasHidden || !cuia.IsMonocolor || cuia.IsFull || units[top] != capacity)
            {
                return 0;
            }

            return 1 << top;
        }

        /// <summary>
        /// Cuias selladas del grupo <paramref name="group"/> que aún llevan la mata (0, o 1 en un espeso que se aclaró con el primer
        /// sello). Para la presentación (02 §9.1 M8, regla 9: el espeso se aclara con el primer sello).
        /// </summary>
        internal static int SealsInGroup(BoardState state, int group)
        {
            int seals = 0;
            for (int j = 0; j < state.Count; j++)
            {
                Cuia cuia = state[j];
                if (cuia.Mechanic == MechanicType.Aguape && cuia.MechanicPairId == group && state.IsSealed(j))
                {
                    seals++;
                }
            }

            return seals;
        }

        private static int FirstOfGroup(BoardState state, int group)
        {
            for (int j = 0; j < state.Count; j++)
            {
                Cuia cuia = state[j];
                if (cuia.Mechanic == MechanicType.Aguape && cuia.MechanicPairId == group)
                {
                    return j;
                }
            }

            return -1;
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
