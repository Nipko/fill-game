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

        /// <summary>Volver a la orilla o «Reintentar»: el tablero vuelve al inicial (04 §2.7).</summary>
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

        /// <summary>
        /// Se añadió la cuia <paramref name="index"/> (vacía, capacidad 4) por Cuia extra o Cuia de rescate: va a la fila más
        /// corta y solo cambia el ancho (D-006; re-layout de 300 ms y Capi de 600 ms, 02 §4.1). Con el rescate, el modal ya
        /// se cerró y la partida sigue (PLAYING).
        /// </summary>
        void OnCuiaAdded(int index, AddedCuiaKind kind);

        /// <summary>Capi regala <paramref name="units"/> Cuias extra al stock (nivel 8; 04 §2.2).</summary>
        void OnExtraCuiaGifted(int units);

        /// <summary>
        /// El embudo de D-008 pasa a <paramref name="phase"/>: <see cref="FlowPhase.GiveUpConfirm"/>,
        /// <see cref="FlowPhase.LevelFailed"/>, <see cref="FlowPhase.NoCanoes"/>, o de vuelta a
        /// <see cref="FlowPhase.Playing"/> al cerrar Sin canoas abierta desde la cabecera.
        /// </summary>
        void OnFunnelPhase(FlowPhase phase);

        /// <summary>FAIL_RESOLVE: se contó un fallo duro o blando (04 §5.2).</summary>
        void OnFailResolved(FailOutcome outcome);

        /// <summary>
        /// Se sirve la variante aliviada (<c>relief[]</c>): la vista muestra «El río bajó» durante <paramref name="bannerMs"/>
        /// antes del teñido (04 §2.7; 07 §5.5). Llega tras <see cref="OnLevelLoaded"/> y antes de <see cref="OnIntroStarted"/>,
        /// cuya duración ya incluye el cartel.
        /// </summary>
        void OnRiverLowered(string reliefType, int bannerMs);
    }
}
