#nullable enable
using AF.Audio;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Sonido del vertido (07 §9.2; 02 §4; tarea AU1): parcial de <see cref="BoardAnimator"/> para que sus ganchos sean dos líneas.
    /// Cuando el chorro empieza a caer suenan <c>sfx_pour_start</c> y el bucle <c>sfx_pour_loop</c>, que dura lo que dura el
    /// chorro (también si un vertido encadenado lo alarga) y sube de tono con el **nivel de la cuia de destino** (lo que ya se
    /// ve lleno: dosis de antes + las que van entrando, sobre su capacidad), no con el tiempo; al cortarse el caudal suena
    /// <c>sfx_pour_end</c>. Si el trabajo salta a su final (Marea, otro nivel, la vuelta de segundo plano), el bucle se calla sin
    /// más. Marea no tiene chorro: su sonido lo pone la UI. Sin asignaciones.
    /// </summary>
    public sealed partial class BoardAnimator
    {
        // Fotograma del chorro (entre su arranque y el final de su cola): empieza el sonido, sigue el nivel o lo corta.
        private static void PourAudio(ref Job job, CuiaView target, float pt, float time)
        {
            if (job.IsUndo || job.PourEnded)
            {
                return;
            }

            float level = target.Capacity > 0 ? Mathf.Clamp01((job.FillBase + Mathf.Max(0f, pt)) / target.Capacity) : 0f;
            if (!job.PourSounding)
            {
                job.PourSounding = true;
                job.PourVoice = job.Stream;
                AudioCues.PourBegin(job.PourVoice, level);
                return;
            }

            if (time >= job.FlowEnd)
            {
                job.PourSounding = false;
                job.PourEnded = true;
                AudioCues.PourEnd(job.PourVoice);
                return;
            }

            AudioCues.PourLevel(job.PourVoice, level);
        }

        // El chorro se suelta sin haber llegado a su corte (salto del trabajo): se calla sin sfx_pour_end.
        private static void PourAudioCut(ref Job job)
        {
            if (!job.PourSounding)
            {
                return;
            }

            job.PourSounding = false;
            job.PourEnded = true;
            AudioCues.PourCut(job.PourVoice);
        }
    }
}
