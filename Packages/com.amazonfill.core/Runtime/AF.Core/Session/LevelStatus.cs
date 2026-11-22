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
        /// No es victoria, hay al menos un vertido útil y, en <see cref="LevelSession"/>, R13 ampliada no se cumple: el cierre
        /// acotado no da el tablero por perdido, o lo da pero aún queda un vertido legal a un tablero no visto en el intento
        /// (D-014 y su añadido del 2026-11-06).
        /// </summary>
        Playing = 0,

        /// <summary>Victoria (R12): toda cuia está vacía o sellada.</summary>
        Won = 1,

        /// <summary>
        /// Aguas estancadas: sin vertidos útiles y sin victoria (R13); o, en <see cref="LevelSession"/>, con vertidos útiles pero
        /// sin salida y repitiendo: los estados alcanzables con todos los vertidos legales son como mucho
        /// <see cref="DeadEndProbe.MaxStates"/> (64 [P]) y ninguno es victoria (R13 ampliada, D-014; el bucle del nivel 66;
        /// opción (a) de la pregunta 46 de <c>docs/GDD.md</c> §6.3, decidida el 2026-10-13; D-020), y todo vertido legal lleva a
        /// un tablero ya visto en el intento (añadido de D-014 del 2026-11-06; el nivel 26 del añadido de D-059 del
        /// 2026-10-31). <c>BoardRules.EvaluateStatus</c> y <c>PourRules.IsStuck</c> solo miran R13.
        /// </summary>
        Stuck = 2,
    }
}
