#nullable enable
using AF.Core.Rules;

namespace AF.GameFlow
{
    /// <summary>
    /// Eventos de <see cref="LevelFlow"/> para la vista (y para la sonda de desarrollo). Se llaman de forma síncrona,
    /// en el orden en que ocurren, con el modelo ya actualizado (02 §3.2: el modelo es la verdad, la vista lo persigue).
    /// </summary>
    public interface ILevelFlowListener
    {
        /// <summary>LOADING: el nivel está en memoria y la sesión creada; la vista construye el tablero.</summary>
        void OnLevelLoaded(int level);

        /// <summary>Empieza Sumergir y teñir (<paramref name="isShort"/> = intro corta de un reinicio).</summary>
        void OnIntroStarted(int durationMs, bool isShort);

        /// <summary>Fin de la intro: el input de juego se habilita en este mismo fotograma (02 §4.2).</summary>
        void OnIntroFinished(bool skipped);

        /// <summary>Toque procesado sobre una cuia o el fondo (selección, deselección, vertido o rechazo).</summary>
        void OnTap(TapResult result);

        /// <summary>Marea: se deshizo <paramref name="undone"/> (04 §2.1).</summary>
        void OnUndo(Pour undone, bool fromStuckModal);

        /// <summary>Volver a la orilla: el tablero vuelve al inicial (04 §2.7).</summary>
        void OnRestart(bool fromStuckModal);

        /// <summary>Victoria lógica (R12): input bloqueado; la vista termina su cola antes de celebrar.</summary>
        void OnWin();

        /// <summary>Aguas estancadas lógico (R13): input bloqueado desde este instante (R15).</summary>
        void OnStuck();

        /// <summary>CELEBRATION.</summary>
        void OnCelebrationStarted();

        /// <summary>VICTORY_SCREEN.</summary>
        void OnVictoryScreen();

        /// <summary>STUCK_MODAL: se abre el modal Aguas estancadas, <c>stuck_modal_delay_ms</c> después de la cola (R15).</summary>
        void OnStuckModalOpened();
    }
}
