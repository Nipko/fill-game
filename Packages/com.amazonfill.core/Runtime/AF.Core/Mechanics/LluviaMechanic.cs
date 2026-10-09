#nullable enable
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M6 Lluvia (GDD 02 R3, §9.1 M6 y §9.2; 03 §3 y §9; propuesta de D-007 b.2 M6; anexo B, etapas (e) y (f′); D-007, Q10 y
    /// Q18 B; respuesta Q-B del dueño; debut 143): cuia abierta al cielo con nube y gotas pendientes.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Estado en la cuia (03 §9: «<c>gotas_pendientes</c> y contador <c>vertidos mod k</c>, ambos en la
    /// clave canónica»): <c>key</c> = tinte agua del mundo, <c>n</c> = gotas pendientes, <c>k</c> = ritmo y el contador de
    /// vertidos útiles desde la última gota (0..k − 1).</description></item>
    /// <item><description>Etapa (e) de R10 (anexo B), una vez por vertido y solo si el vertido no es fútil (Q18 B: un fútil no
    /// avanza el contador): suma 1; al llegar a <c>k</c> cae encima una dosis de <c>key</c>, sin regla de color, si hay hueco,
    /// y el contador vuelve a 0. Si la cuia está llena, la gota espera al siguiente vertido útil: el contador se queda en
    /// <c>k − 1</c>, que se comporta igual (el siguiente vertido útil vuelve a intentarlo) y no crea un estado más.</description></item>
    /// <item><description>Con la última gota se va la nube: la cuia pasa a <see cref="MechanicType.None"/> (02 R16 para D-007).
    /// Lo que la gota selle o suelte ocurre en el mismo vertido por el punto fijo (f′) (test obligatorio del anexo B).</description></item>
    /// <item><description>Una gota nunca elimina cortes de color (03 §9): <c>H</c> sigue siendo admisible y consistente.</description></item>
    /// <item><description><c>U_agua</c> = dosis iniciales + gotas pendientes = 4 (02 R3): lo cuentan <see cref="BoardBuilder"/>
    /// y <see cref="BoardState"/>. La cuia de Lluvia solo sella con agua (<see cref="CuiaRules.IsSealed"/>), y con gotas
    /// pendientes impide la victoria (R12: el agua aún no está entera en el tablero).</description></item>
    /// <item><description>Como origen y como destino, la regla normal. Con gotas pendientes nunca es fútil como destino
    /// (<c>cambia_al_recibir</c>, anexo B; regresión 4 de 04 §5.1) ni, por la respuesta Q-B del dueño, como origen: así el
    /// vaivén que hace pasar el tiempo hasta la gota que falta cuenta como útil y R14 se cumple. La exención acaba al caer la
    /// última gota (la cuia ya es normal).</description></item>
    /// <item><description>Marea restaura el estado anterior entero: el contador y la gota caída (02 §9.2).</description></item>
    /// <item><description>Validación: <c>key</c> es el agua del mundo (10 o 14, 02 §9.1); <c>n</c> + dosis iniciales de agua =
    /// 4; <c>k</c> de 2 a 5 (08 §9) y el contador a 0; sin dosis ocultas en el nivel (D-007, Q14 A). Un contador por nivel
    /// (sin otra Lluvia ni Piraña, propuesta b.2 M6) lo comprueba <see cref="LevelValidation"/>.</description></item>
    /// </list>
    /// </remarks>
    internal sealed class LluviaMechanic : MechanicBase
    {
        public override bool ChangesOnReceive(Cuia self)
        {
            return self.MechanicN > 0;
        }

        public override bool ExemptAsSource(Cuia self)
        {
            return self.MechanicN > 0; // respuesta Q-B: la exención de origen dura mientras queden gotas
        }

        public override bool AdvanceCounter(in BoardSlice board, int index, ref PourEvents events)
        {
            Cuia rain = board[index];
            int pending = rain.MechanicN;
            if (pending == 0)
            {
                return false; // no debería ocurrir: con la última gota la cuia pasa a ser normal
            }

            events.CountersAdvanced = true;
            int counter = rain.MechanicCounter + 1;
            int k = rain.MechanicK;
            if (counter < k)
            {
                board.Set(index, rain.WithMechanicCounter(counter));
                return false;
            }

            if (rain.IsFull)
            {
                // 02 §9.1 M6: «si está llena, la gota espera al siguiente vertido».
                board.Set(index, rain.WithMechanicCounter(k - 1));
                events.RecordDropWaiting(index);
                return false;
            }

            Cuia wet = rain.AddOnTop(rain.MechanicKey, 1);
            bool last = pending == 1;
            board.Set(index, last ? wet.WithoutMechanic() : wet.WithMechanicN(pending - 1).WithMechanicCounter(0));
            events.RecordDrop(index, last);
            return true;
        }

        public override bool BlocksVictory(Cuia self)
        {
            return self.MechanicN > 0;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia rain = state[index];
            int key = rain.MechanicKey;
            if (key != LevelValidation.TapajosWater && key != LevelValidation.RioNegroWater)
            {
                return "02 §9.1 M6: key es el tinte agua del mundo (10, Agua del Tapajós; 14, Agua del Río Negro), no " + Num(key);
            }

            if (rain.MechanicN < 1)
            {
                return "02 §9.1 M6: la Lluvia empieza con al menos una gota pendiente (n ≥ 1)";
            }

            if (rain.MechanicK < 2 || rain.MechanicK > 5)
            {
                return "08 §9: k va de 2 a 5";
            }

            if (rain.MechanicCounter != 0)
            {
                return "el contador de la Lluvia empieza en 0 (08 §4.1: attrs no lo lleva)";
            }

            int units = state.UnitsOf(key);
            if (units != LevelValidation.LluviaWaterUnits)
            {
                return "02 §9.1 M6 (R3): U_agua = dosis iniciales + gotas pendientes = 4, y aquí son " + Num(units - rain.MechanicN) + " + " + Num(rain.MechanicN);
            }

            if (state.HasHidden)
            {
                return "D-007, Q14 A: un nivel con Lluvia no tiene dosis ocultas";
            }

            return null;
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
