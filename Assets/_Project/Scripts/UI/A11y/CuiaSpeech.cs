#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;

namespace AF.UI
{
    /// <summary>
    /// Lo que se dice o se enseña de una cuia (bloque B9), sacado del modelo (02 §3.2: la vista persigue al modelo; aquí no se
    /// repite ninguna regla de vertido):
    /// <list type="bullet">
    /// <item><see cref="Label"/>: su etiqueta del lector de pantalla (02 §12, fila «Lector de pantalla»): las dosis de abajo
    /// arriba y los huecos («cuia 3: açaí, açaí, aguaje, 1 hueco»); una oculta se lee «turbia»; la Tinaja, sin nombres ocultos ni
    /// «turbia» ni huecos («tinaja: camu camu en la boca, 3 dosis dentro»), y su cuia, como una cuia («su cuia chica: açaí, 1
    /// hueco»; D-019); con mecánica, detrás el nombre de la mecánica (08 §14.2; Q-B9-14: «cuia 3: açaí, 1 hueco. Hoja»), con
    /// <see cref="MechanicId"/>;</item>
    /// <item><see cref="Names"/>: los nombres de C9 (02 §3.1 y §12), de arriba abajo [P] y uno por línea: en una turbia, «turbia»
    /// por cada oculta (sin decir cuál es); en la Tinaja, solo la boca y las capas ya reveladas, nunca las ocultas.</item>
    /// </list>
    /// El número de la cuia es su ranura empezando en 1 (el índice del tablero + 1) [P]. Una fruta sin nombre en este idioma o
    /// build (<c>tint.&lt;tinte&gt;.name</c> aún sin aprobar en release) se dice «fruta» [P] (Q-B9-7). Asignan cadenas: solo cuando cambia
    /// el tablero, nunca por fotograma.
    /// </summary>
    public static class CuiaSpeech
    {
        /// <summary>Etiqueta de la cuia <paramref name="index"/> de <paramref name="state"/> para el lector de pantalla.</summary>
        /// <exception cref="ArgumentOutOfRangeException">El índice está fuera del tablero.</exception>
        public static string Label(BoardState state, int index)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if ((uint)index >= (uint)state.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice de cuia fuera del tablero.");
            }

            Cuia cuia = state[index];
            if (cuia.Mechanic == MechanicType.Tinaja)
            {
                // La boca es la dosis de arriba y siempre está a la vista (M13); las de debajo, ocultas: ni nombre ni «turbia».
                string mouth = cuia.IsEmpty ? string.Empty : DoseName(cuia, cuia.Count - 1);
                return A11yText.Tinaja(mouth, cuia.Count);
            }

            string doses = string.Empty;
            for (int k = 0; k < cuia.Count; k++)
            {
                doses = A11yText.Append(doses, DoseName(cuia, k));
            }

            if (cuia.Mechanic == MechanicType.Cucharon)
            {
                return A11yText.Cucharon(doses, cuia.Count, cuia.Space);
            }

            string label = A11yText.Cuia(index + 1, doses, cuia.Count, cuia.Space);
            return A11yText.WithMechanic(label, A11yText.MechanicName(MechanicId(cuia)));
        }

        /// <summary>
        /// Id del glosario (§6.5; <c>mechanic.&lt;id&gt;</c>) de la mecánica de <paramref name="cuia"/> que se dice en su etiqueta
        /// (Q-B9-14), o «» si no se dice ninguna: una cuia normal, y la Tinaja y su cuia chica, que ya se nombran en la suya. El
        /// Brote de dos hojas y la Liana de cuenta se distinguen por <c>n</c> (<see cref="Cuia.MechanicN"/>), como en el modelo.
        /// </summary>
        public static string MechanicId(Cuia cuia)
        {
            switch (cuia.Mechanic)
            {
                case MechanicType.Hoja: return "hoja";
                case MechanicType.HojaAtada: return "hoja_atada";
                case MechanicType.Brote: return cuia.MechanicN > 0 ? "brote_n2" : "brote";
                case MechanicType.Liana: return cuia.MechanicN > 0 ? "liana_n" : "liana";
                case MechanicType.Matapi: return "matapi";
                case MechanicType.Remolino: return "remolino";
                case MechanicType.Lluvia: return "lluvia";
                case MechanicType.Pirana: return "pirana";
                case MechanicType.Flor: return "flor";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Los nombres de C9 de la cuia <paramref name="index"/>, de arriba abajo y uno por línea; «» si no hay ninguno que enseñar
        /// (vacía, o una Tinaja sin nada revelado que no sea…; nunca pasa: la boca se ve siempre).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El índice está fuera del tablero.</exception>
        public static string Names(BoardState state, int index)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if ((uint)index >= (uint)state.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice de cuia fuera del tablero.");
            }

            Cuia cuia = state[index];
            bool tinaja = cuia.Mechanic == MechanicType.Tinaja;
            string names = string.Empty;
            for (int k = cuia.Count - 1; k >= 0; k--)
            {
                if (tinaja && k < cuia.Count - 1 && cuia.IsHiddenAt(k))
                {
                    continue; // D-019: en la Tinaja, nunca las ocultas (ni como «turbia»)
                }

                names = A11yText.AppendLine(names, DoseName(cuia, k));
            }

            return names;
        }

        /// <summary>Nombre de la fruta que se dice de la dosis de arriba de la cuia <paramref name="index"/> (el anuncio de la selección), o «».</summary>
        public static string TopName(BoardState state, int index)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if ((uint)index >= (uint)state.Count || state[index].IsEmpty)
            {
                return string.Empty;
            }

            Cuia cuia = state[index];
            return DoseName(cuia, cuia.Count - 1);
        }

        // La dosis k (desde abajo): «turbia» si está oculta (la boca de la Tinaja nunca lo está), «barro» si es Barro, o el nombre de su fruta.
        private static string DoseName(Cuia cuia, int k)
        {
            if (cuia.IsHiddenAt(k) && !(cuia.Mechanic == MechanicType.Tinaja && k == cuia.Count - 1))
            {
                return A11yText.Murky;
            }

            int color = cuia.ColorAt(k);
            return color == Cuia.MudColor ? A11yText.Mud : A11yText.FruitName(color);
        }
    }
}
