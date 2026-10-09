#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Reloj de los shaders de UI con movimiento propio (<c>AF/UI/Liquid</c>: onda del menisco; <c>AF/UI/Stream</c>: vetas del
    /// chorro). En el juego usan <c>_Time.y</c>; para las capturas deterministas (guion de la intro, A14) se congela con el
    /// tiempo de la intro (o con 0 fuera de ella): los globales <c>_AFTime</c> (s) y <c>_AFClockFrozen</c> (0/1) sustituyen a
    /// <c>_Time.y</c> mientras <c>_AFClockFrozen</c> vale 1. Sin asignaciones.
    /// </summary>
    public static class ShaderClock
    {
        private static readonly int TimeId = Shader.PropertyToID("_AFTime");
        private static readonly int FrozenId = Shader.PropertyToID("_AFClockFrozen");

        /// <summary>El reloj de los shaders está congelado.</summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>Tiempo congelado (s).</summary>
        public static float Seconds { get; private set; }

        /// <summary>Congela el reloj de los shaders en <paramref name="seconds"/>.</summary>
        public static void Freeze(float seconds)
        {
            if (IsFrozen && Seconds == seconds)
            {
                return;
            }

            IsFrozen = true;
            Seconds = seconds;
            Shader.SetGlobalFloat(TimeId, seconds);
            Shader.SetGlobalFloat(FrozenId, 1f);
        }

        /// <summary>Devuelve los shaders al reloj del motor.</summary>
        public static void Release()
        {
            IsFrozen = false;
            Shader.SetGlobalFloat(FrozenId, 0f);
        }

        /// <summary>Valores globales que leen los shaders (tests): tiempo y si está congelado.</summary>
        public static void ReadGlobals(out float seconds, out float frozen)
        {
            seconds = Shader.GetGlobalFloat(TimeId);
            frozen = Shader.GetGlobalFloat(FrozenId);
        }
    }
}
