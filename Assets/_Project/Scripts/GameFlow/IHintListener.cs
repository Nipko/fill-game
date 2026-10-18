#nullable enable
using AF.Boosters;

namespace AF.GameFlow
{
    /// <summary>
    /// Avisos opcionales de <see cref="LevelFlow"/> para Morpho · Pista (04 §2.3; MO1). El flujo los consulta con
    /// <c>listener as IHintListener</c>, así que <see cref="ILevelFlowListener"/> no cambia. Se llaman de forma síncrona en el
    /// hilo principal (desde <see cref="LevelFlow.PressHint"/> o <see cref="LevelFlow.Tick"/>), con el modelo ya actualizado. La
    /// interfaz (ranura, mariposa, anillos y avisos) es de MO2.
    /// </summary>
    public interface IHintListener
    {
        /// <summary>
        /// Morpho empieza a buscar (<paramref name="thinking"/> = true) o deja de hacerlo: con una respuesta (justo antes de
        /// <see cref="OnHint"/>) o porque la consulta se abandonó al cambiar el tablero (sin respuesta ni cobro).
        /// </summary>
        void OnHintThinking(bool thinking);

        /// <summary>
        /// La respuesta de Morpho, en el orden de 04 §2.3. Con <see cref="HintAnswer.Pour"/> ya está cobrada y
        /// <see cref="LevelFlow.ActiveHint"/> es el vertido que se resalta; las otras tres no cobran.
        /// </summary>
        void OnHint(in HintResult result);

        /// <summary>La pista resaltada se apaga: el tablero cambió (vertido u otra acción) o el nivel dejó de estar en juego (04 §2.3 d).</summary>
        void OnHintCleared();

        /// <summary>
        /// Se acreditaron <paramref name="units"/> Morphos al stock al cargar el nivel de desbloqueo por primera vez (04 §2.3: 3
        /// en el 12; tarea TU1b). Llega en LOADING, tras <see cref="ILevelFlowListener.OnLevelLoaded"/>; la presentación de TiTino
        /// sale después de la intro (instrucción del tutorial).
        /// </summary>
        void OnHintGifted(int units);
    }
}
