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
        /// <summary>Hay al menos un vertido útil y no es victoria.</summary>
        Playing = 0,

        /// <summary>Victoria (R12): toda cuia está vacía o sellada.</summary>
        Won = 1,

        /// <summary>Aguas estancadas (R13): sin vertidos útiles y sin victoria.</summary>
        Stuck = 2,
    }
}
