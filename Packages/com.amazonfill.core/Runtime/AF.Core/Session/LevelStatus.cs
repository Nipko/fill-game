#nullable enable

namespace AF.Core.Session
{
    /// <summary>
    /// Estado lógico de la partida tras cada cambio, evaluado en el orden de R10 ((g) y (h) del anexo B de D-007): victoria
    /// y después bloqueo.
    /// La máquina de estados de pantalla (LOADING, INTRO, CELEBRATION…, GDD 02 §2) vive fuera del núcleo.
    /// </summary>
    public enum LevelStatus
    {
        /// <summary>
        /// No es victoria, hay al menos un vertido útil y, en <see cref="LevelSession"/>, el cierre acotado de R13 ampliada no da
        /// el tablero por perdido (D-014).
        /// </summary>
        Playing = 0,

        /// <summary>Victoria (R12): toda cuia está vacía o sellada.</summary>
        Won = 1,

        /// <summary>
        /// Aguas estancadas: sin vertidos útiles y sin victoria (R13); o, en <see cref="LevelSession"/>, con vertidos útiles pero
        /// sin salida: los estados alcanzables con todos los vertidos legales son como mucho <see cref="DeadEndProbe.MaxStates"/>
        /// (64 [P]) y ninguno es victoria (R13 ampliada, D-014; el bucle del nivel 66; opción (a) de la pregunta 46, pendiente
        /// del dueño). <c>BoardRules.EvaluateStatus</c> y <c>PourRules.IsStuck</c> solo miran R13.
        /// </summary>
        Stuck = 2,
    }
}
