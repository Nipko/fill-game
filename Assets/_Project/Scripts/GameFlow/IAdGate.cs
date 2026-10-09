#nullable enable
using System;

namespace AF.GameFlow
{
    /// <summary>
    /// AD_GATE (02 §2; 05 §6.1 y §6.3; D-008 punto 4; tarea B2-4): lo que el flujo necesita de los anuncios para la cadencia
    /// de intersticiales, sin depender de <c>AF.Ads</c> ni de la vista. En el juego lo implementa <c>AF.UI</c> sobre
    /// <c>AdController</c> (anuncios simulados de la fase 2, sin SDK); en los tests, un doble.
    /// <list type="bullet">
    /// <item><description><see cref="RecordWin"/> en el instante lógico de cada victoria de campaña (cuenta para
    /// <c>levels_since_interstitial</c>, también si después se toca «Inicio» o se cierra la app).</description></item>
    /// <item><description><see cref="Run"/> solo tras tocar «Siguiente» en VICTORY_SCREEN: muestra el intersticial si las
    /// reglas de 05 §6.3 lo permiten (contexto <c>VICTORY_SCREEN</c>) y llama a <c>onDone</c> siempre, una sola vez (al
    /// cerrarse el anuncio o enseguida si no toca). Nunca se llama al reintentar, al rendirse, en Nivel fallido ni en Sin
    /// canoas (05 §10; D-008).</description></item>
    /// </list>
    /// </summary>
    public interface IAdGate
    {
        /// <summary>Se ganó el nivel <paramref name="level"/> de la campaña.</summary>
        void RecordWin(int level);

        /// <summary>AD_GATE tras la victoria de <paramref name="wonLevel"/>; <paramref name="onDone"/> se llama siempre, una vez.</summary>
        void Run(int wonLevel, Action onDone);
    }
}
