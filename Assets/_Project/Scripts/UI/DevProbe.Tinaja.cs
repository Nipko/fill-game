#nullable enable
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// Sonda de la Tinaja (tarea TJ4; propuesta de la Tinaja, c.6 «UI/DevProbe.cs y AF_STATE»; M13, D-019), solo en desarrollo:
    /// cada línea <c>AF_STATE</c> de un nivel con tinaja lleva, tras los campos del tablero, el objeto
    /// <c>"tinaja":{"cuia":2,"partner":1,"doses":3,"hidden":2,"revealed":0,"mouth":9,"hung":true,"show_contents":false}</c>,
    /// para jugar y comprobar por <c>adb</c> el sacar, el revelado y la suelta:
    /// <list type="bullet">
    /// <item><description><c>cuia</c>: índice de la tinaja; <c>partner</c>: su cuia chica (la del tablero inicial: tras la suelta
    /// ya no lleva el tipo), −1 si no se encuentra;</description></item>
    /// <item><description><c>doses</c>: las dosis que quedan dentro, contando la boca (el chip «×n»);</description></item>
    /// <item><description><c>hidden</c>: las que siguen ocultas; <c>revealed</c>: las de debajo de la boca ya a la vista (las
    /// bandas: reveladas al sacar, con la Luciérnaga o desde el intento anterior, Q-Ti6);</description></item>
    /// <item><description><c>mouth</c>: el tinte de la boca (0 vacía);</description></item>
    /// <item><description><c>hung</c>: su cuia sigue colgada del jirau (true) o ya se soltó como una Cuia chica normal (false,
    /// R10 f);</description></item>
    /// <item><description><c>show_contents</c>: <c>ui_tinaja_show_contents</c> (08 §9; D-019, Q-Ti5-A: false).</description></item>
    /// </list>
    /// Sin tinaja no se escribe nada. El campo <c>murky</c> del tablero no cuenta la tinaja (sus ocultas no son agua turbia).
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>
        /// Campo <c>"tinaja":{…}</c> de la línea <c>AF_STATE</c> (empieza con coma) si <paramref name="current"/> lleva tinaja;
        /// si no, nada. Puro: no depende de <see cref="LevelFlow"/> ni de <see cref="Enabled"/>.
        /// </summary>
        internal static void AppendTinajaFields(StringBuilder sb, BoardState initial, BoardState current)
        {
            int t = IndexOf(current, MechanicType.Tinaja);
            if (t < 0)
            {
                return;
            }

            Cuia jar = current[t];
            int partner = IndexOf(current, MechanicType.Cucharon);
            bool hung = partner >= 0;
            if (!hung)
            {
                partner = IndexOf(initial, MechanicType.Cucharon);
            }

            int doses = jar.Count;
            int hidden = 0;
            int revealed = 0;
            for (int k = 0; k < doses - 1; k++)
            {
                if (jar.IsHiddenAt(k))
                {
                    hidden++;
                }
                else
                {
                    revealed++;
                }
            }

            int mouth = doses > 0 && !jar.IsHiddenAt(doses - 1) ? jar.ColorAt(doses - 1) : 0;
            CultureInfo inv = CultureInfo.InvariantCulture;
            sb.Append(",\"tinaja\":{\"cuia\":").Append(t.ToString(inv));
            sb.Append(",\"partner\":").Append(partner.ToString(inv));
            sb.Append(",\"doses\":").Append(doses.ToString(inv));
            sb.Append(",\"hidden\":").Append(hidden.ToString(inv));
            sb.Append(",\"revealed\":").Append(revealed.ToString(inv));
            sb.Append(",\"mouth\":").Append(mouth.ToString(inv));
            sb.Append(",\"hung\":").Append(hung ? "true" : "false");
            sb.Append(",\"show_contents\":").Append(TinajaTutorial.ShowsContents ? "true" : "false").Append('}');
        }

        /// <summary>
        /// El tablero tiene dosis de Aguas turbias: alguna oculta en una cuia que no es la tinaja (sus ocultas no son agua turbia;
        /// contrato 27; M13). Puro.
        /// </summary>
        internal static bool HasMurkyWater(BoardState board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].HiddenMask != 0 && board[i].Mechanic != MechanicType.Tinaja)
                {
                    return true;
                }
            }

            return false;
        }

        private static int IndexOf(BoardState board, MechanicType type)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].Mechanic == type)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
