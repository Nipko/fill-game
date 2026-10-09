#nullable enable
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M7 Piraña (GDD 02 §9.1 M7 y §9.2; 03 §3 y §9; propuesta de D-007 b.2 M7 y b.2 M10 regla 7; anexo B, etapas (e) y (f′);
    /// D-007, Q10 y Q18 B; respuesta Q-C del dueño; debut 183): la cuia marcada es la cueva de la piraña, con su contador.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Estado en la cueva (03 §9, en la clave canónica): <c>k</c> y el contador de vertidos útiles desde el
    /// último mordisco (0..k − 1). El «último destino» de 02 §9.1 es el del propio vertido (el mordisco ocurre en su etapa
    /// (e)), así que no hace falta guardarlo.</description></item>
    /// <item><description>Etapa (e) de R10, una vez por vertido y nunca en un fútil (Q18 B): suma 1; al llegar a <c>k</c> el
    /// contador vuelve a 0 y la piraña muerde la dosis superior de la cuia que recibió el vertido y la lleva a la cima de la
    /// cueva (sin regla de color), salvo que esa cuia haya quedado sellada (02 §9.1: «sellar la cuia destino en el vertido
    /// n·k evita el mordisco»), sea un Matapi (propuesta b.2 M10, regla 7) o sea la propia cueva, o que la cueva esté llena
    /// (respuesta Q-C del dueño): entonces el mordisco se pierde y el contador sigue.</description></item>
    /// <item><description>La dosis mordida es siempre una de las que el vertido acaba de dejar en la cima de la cuia que
    /// recibió: el paso vertido + mordisco reparte el mismo vertido entre esa cuia y la cueva, así que cada término de
    /// <c>H</c> baja como mucho en 1 y <c>H</c> sigue siendo admisible y consistente (el par es exacto; ver
    /// <c>AF.Solver.Search.SolverHeuristic</c>).</description></item>
    /// <item><description>La cueva es una cuia normal como origen y como destino (02 §9.1: «el jugador vierte desde la cueva
    /// con la regla normal»); como Cuia chica, termina vacía (R12). Lo que el mordisco selle o suelte (la cueva llena de una
    /// fruta, una Liana) ocurre en el mismo vertido por el punto fijo (f′).</description></item>
    /// <item><description>Marea restaura el estado anterior entero: el contador y el mordisco (02 §9.2).</description></item>
    /// <item><description>Validación: <c>k</c> de 2 a 5 (08 §9) y el contador a 0; sin dosis ocultas en el nivel (D-007, Q14
    /// A); sin Remolino en el nivel [P: la cuia que recibe no tendría el vertido en la cima]. Un contador por nivel (sin
    /// Lluvia ni otra Piraña; propuesta b.2 M7) lo comprueba <see cref="LevelValidation"/>.</description></item>
    /// </list>
    /// </remarks>
    internal sealed class PiranaMechanic : MechanicBase
    {
        public override bool AdvanceCounter(in BoardSlice board, int index, ref PourEvents events)
        {
            Cuia cave = board[index];
            events.CountersAdvanced = true;
            int counter = cave.MechanicCounter + 1;
            if (counter < cave.MechanicK)
            {
                board.Set(index, cave.WithMechanicCounter(counter));
                return false;
            }

            cave = cave.WithMechanicCounter(0);
            board.Set(index, cave);
            int bitten = events.Receiver;
            Cuia victim = board[bitten];
            if (bitten == index || cave.IsFull || victim.IsEmpty || victim.Mechanic == MechanicType.Matapi || board.IsSealed(bitten))
            {
                events.RecordBiteLost(index); // respuesta Q-C; sellado (02 §9.1); Matapi (regla 7)
                return false;
            }

            int color = victim.TopColor;
            board.Set(bitten, victim.RemoveTop(1));
            board.Set(index, cave.AddOnTop(color, 1));
            events.RecordBite(bitten, index);
            return true;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia cave = state[index];
            if (cave.MechanicK < 2 || cave.MechanicK > 5)
            {
                return "08 §9: k va de 2 a 5";
            }

            if (cave.MechanicCounter != 0)
            {
                return "el contador de la Piraña empieza en 0 (08 §4.1: attrs no lo lleva)";
            }

            if (state.HasHidden)
            {
                return "D-007, Q14 A: un nivel con Piraña no tiene dosis ocultas";
            }

            for (int j = 0; j < state.Count; j++)
            {
                if (state[j].Mechanic == MechanicType.Remolino)
                {
                    return "un nivel con Piraña no tiene Remolino [P: la cuia que recibe no tendría el vertido en la cima]";
                }
            }

            return null;
        }
    }
}
