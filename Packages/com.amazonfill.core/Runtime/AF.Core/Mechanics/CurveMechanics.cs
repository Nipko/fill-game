#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// Consultas de las mecánicas de la 1.1 (D-068 [P]; 02 §9.1 M8 y M11) para el cliente (F4: la mata, el muelle, los textos) y
    /// las herramientas (F3: superposición y QA). Todo se deduce del estado del tablero; no cambian nada. No son código caliente.
    /// </summary>
    public static class CurveMechanics
    {
        /// <summary><c>n</c> del Aguapé espeso (P13 A): dos sellos dentro del grupo.</summary>
        public const int AguapeThickSeals = AguapeMechanic.ThickSeals;

        /// <summary>Cuias mínimas de un grupo de Aguapé (02 §9.1 M8).</summary>
        public const int AguapeMinGroupSize = AguapeMechanic.MinGroupSize;

        /// <summary>Cuias máximas de un grupo de Aguapé (02 §9.1 M8).</summary>
        public const int AguapeMaxGroupSize = AguapeMechanic.MaxGroupSize;

        /// <summary>Cuias mínimas de un grupo de Aguapé espeso (02 §9.1 M8).</summary>
        public const int AguapeMinThickGroupSize = AguapeMechanic.MinThickGroupSize;

        /// <summary>Dosis mínimas de la carga de la Lancha (02 §9.1 M11).</summary>
        public const int LanchaMinCargo = LanchaMechanic.MinCargo;

        /// <summary>Dosis máximas de la carga de la Lancha (02 §9.1 M11).</summary>
        public const int LanchaMaxCargo = LanchaMechanic.MaxCargo;

        /// <summary>
        /// Cuias selladas del grupo de Aguapé <paramref name="group"/> que aún llevan la mata: 0 mientras no sella ninguna; en el
        /// espeso, 1 tras el primer sello (la mata se aclara, 02 §9.1 M8 regla 9). Con <c>n</c> sellos el grupo ya se soltó y sus
        /// cuias no llevan mecánica, así que devuelve 0.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> es null.</exception>
        public static int AguapeSeals(BoardState state, int group)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return AguapeMechanic.SealsInGroup(state, group);
        }

        /// <summary>
        /// Máscara de las frutas (bit <c>c</c>) con las que la cuia <paramref name="index"/> podría sellar solo recibiendo: la
        /// condición de la regla 6 de 02 §9.1 M8 que usa la validación del Aguapé (y la superposición del generador).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> es null.</exception>
        public static int SealableByReceiving(BoardState state, int index)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return AguapeMechanic.SealableColors(state[index], state.UnitsTable);
        }

        /// <summary>
        /// Cuias vacías que cuentan para el atraque de la Lancha (02 §9.1 M11 [P]: toda cuia sin dosis, sin contar la lancha), con
        /// la lancha en <paramref name="lancha"/>. Es el «te faltan» del muelle: <c>k</c> menos este número.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> es null.</exception>
        public static int LanchaEmpties(BoardState state, int lancha)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var board = new BoardSlice(state.CuiasArray, 0, state.Count, state.UnitsTable);
            return LanchaMechanic.EmptyCuias(in board, lancha);
        }
    }
}
