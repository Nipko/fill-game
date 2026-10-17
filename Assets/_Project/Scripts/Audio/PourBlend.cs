#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Mezcla de los 4 bucles grabados del chorro (<c>sfx_pour_loop_l1..l4</c>; tarea AU3): el nivel de la cuia de destino
    /// (<see cref="PourLoopSynth.LevelForDoses"/>: 0 con 1 dosis … 1 llena) se lleva a una posición 0-3 sobre los bucles
    /// (<c>l1</c> casi vacía, <c>l2</c> un tercio, <c>l3</c> dos tercios, <c>l4</c> casi llena) y suenan los dos vecinos con un
    /// fundido de potencia constante. Cada bucle se afina como mucho ± <see cref="MaxSemitones"/> semitonos alrededor de su punto
    /// (−2 al empezar a entrar, 0 en su punto, +2 al acabar de salir), así que el chorro sube de tono sin escalones y sin la
    /// aceleración de hasta 2,4× del bucle por código. Sin motor, sin asignaciones.
    /// </summary>
    public static class PourBlend
    {
        /// <summary>Bucles grabados.</summary>
        public const int Loops = AudioClipIds.PourLoops;

        /// <summary>Afinación máxima de un bucle (semitonos, en los dos sentidos).</summary>
        public const float MaxSemitones = 2f;

        /// <summary>Posición 0-3 sobre los bucles para el nivel <paramref name="level"/> (0-1, recortado).</summary>
        public static float Position(float level)
        {
            float l = float.IsNaN(level) ? 0f : level < 0f ? 0f : level > 1f ? 1f : level;
            return l * (Loops - 1);
        }

        /// <summary>
        /// Ganancia del bucle <paramref name="loop"/> (0-3) en la posición <paramref name="position"/> (0-3): 1 en su punto, 0 a un
        /// bucle de distancia o más, y fundido de potencia constante (coseno y seno) con su vecino: la suma de los cuadrados es 1.
        /// </summary>
        public static float Weight(float position, int loop)
        {
            float d = position - loop;
            if (d <= -1f || d >= 1f)
            {
                return 0f;
            }

            double t = d < 0f ? -d : d;
            return (float)Math.Cos(t * Math.PI * 0.5);
        }

        /// <summary>
        /// Afinación del bucle <paramref name="loop"/> en la posición <paramref name="position"/> (semitonos): la distancia a su punto
        /// por <see cref="MaxSemitones"/>, recortada a ± <see cref="MaxSemitones"/>.
        /// </summary>
        public static float Semitones(float position, int loop)
        {
            float s = (position - loop) * MaxSemitones;
            return s < -MaxSemitones ? -MaxSemitones : s > MaxSemitones ? MaxSemitones : s;
        }

        /// <summary>Velocidad de reproducción (<c>AudioSource.pitch</c>) para <paramref name="semitones"/> semitonos.</summary>
        public static float Pitch(float semitones)
        {
            return (float)Math.Pow(2.0, semitones / 12.0);
        }
    }
}
