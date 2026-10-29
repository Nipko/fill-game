#nullable enable
using System.Globalization;
using System.Text;
using AF.Audio;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sonda del audio (tareas AU1-AU3; docs/dev/prototipo-v0.md), solo en desarrollo, para probar en el teléfono por logcat lo
    /// que no se puede oír desde el ordenador:
    /// <list type="bullet">
    /// <item><c>AF_STATE {"event":"sfx","t":…,"id":"sfx_select","gain_db":-36.0,"tint":4,"src":"clip"}</c> en cada efecto que
    /// suena (con Sonido apagado, en silencio o con un anuncio delante no sale ninguno): su ID de la lista, su pico en dBFS, en
    /// los afinados la fruta (índice de motor 1-14) y, desde AU3, de dónde sale: <c>clip</c> (el WAV de la entrega) o
    /// <c>synth</c> (el audio por código de respaldo). Los remates salen igual, con su id (<c>sting_arrival</c>…).</item>
    /// <item><c>AF_STATE {"event":"haptic","t":…,"kind":"haptic_seal"}</c> en cada vibración (con Vibración apagada no sale).</item>
    /// <item><c>AF_STATE {"event":"music","t":…,"world":3,"layers":1,"night":false,"ducked":false,"menu":false,"clip":"mus_w03_bed"}</c>
    /// cada vez que cambia la música (tarea AU2): el mundo del lecho (0 en el inicio, con <c>menu</c> true), las capas de
    /// percusión pedidas, la variante nocturna de Muy difícil, la atenuación de 6 dB (toada o modal) y, desde AU3, el clip del
    /// lecho o del tema de menú (<c>synth</c> con el respaldo por código; vacío sin música). Como <c>sfx</c>, en el Editor solo
    /// con <see cref="AudioLinesInEditor"/>.</item>
    /// <item><c>AF_STATE {"event":"ambient","t":…,"world":4,"rain":true,"level":95,"clip":"amb_w04_water","rain_clip":"amb_rain_light"}</c>
    /// al cargar un nivel (y con world 0 al abrir el inicio): el agua del mundo, si el nivel tiene lluvia y, desde AU3, sus clips
    /// (<c>synth</c> con el respaldo por código; <c>rain_clip</c> solo si llueve).</item>
    /// <item><c>AF_PERF {"event":"audio_build","worker_ms":…,"main_ms":…,"clips":…,"pcm_kib":…}</c> una vez, cuando los
    /// clips quedan listos: lo que tardó el hilo de trabajo en generar el PCM, lo que pagó el hilo principal al crearlos y la
    /// memoria PCM (en float).</item>
    /// <item><c>AF_STATE {"event":"audio_focus","t":…,"state":"loss_transient","music_gain":0.00,"paused":true}</c> en cada cambio del
    /// foco de audio del teléfono (observación 10 de la jornada 6; D-026, añadido del 2026-10-29): <c>gain</c> (el juego lo tiene),
    /// <c>duck</c> (otro suena por encima: la música baja, <c>music_gain</c> 0.20), <c>loss_transient</c> (una alarma, el timbre de
    /// una llamada: pausa), <c>loss</c> (otra app de música: pausa), <c>denied</c> (Android no lo dio), <c>yielded</c> (ya sonaba música de otra app: la del juego calla, los efectos suenan) o <c>none</c> (sin pedir:
    /// segundo plano o Música apagada). <c>music_gain</c> es lo que deja el foco (sin el fundido de vuelta).</item>
    /// </list>
    /// </summary>
    public sealed partial class DevProbe : IAudioProbe, IAudioFocusProbe
    {
        /// <summary>Último <c>AF_STATE audio_focus</c> escrito (tests; null sin sonda).</summary>
        public string? LastAudioFocusLine { get; private set; }

        /// <inheritdoc />
        public void AudioFocus(string state, float musicGain, bool paused)
        {
            // Pocas veces (una alarma, una llamada): también en el Editor, sin AudioLinesInEditor.
            if (!Enabled)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"audio_focus\"");
            AppendTime(sb);
            sb.Append(",\"state\":\"").Append(state).Append('"');
            sb.Append(",\"music_gain\":").Append(musicGain.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(",\"paused\":").Append(paused ? "true" : "false").Append('}');
            LastAudioFocusLine = sb.ToString();
            Write(LastAudioFocusLine);
        }

        /// <summary>
        /// Las líneas <c>sfx</c> y <c>haptic</c> también en el Editor (tests de la sonda). Por defecto no: en el Editor cada efecto
        /// escribiría una línea en el bucle de juego (Debug.Log asigna memoria y los tests de «sin asignaciones» de la intro y de
        /// los pasos de mecánica lo miden); en el teléfono de desarrollo, sí.
        /// </summary>
        public static bool AudioLinesInEditor { get; set; }

        private static bool AudioLinesOn => Enabled && (!Application.isEditor || AudioLinesInEditor);

        /// <summary>Último <c>AF_STATE sfx</c> escrito (tests; null sin sonda).</summary>
        public string? LastSfxLine { get; private set; }

        /// <summary>Último <c>AF_STATE haptic</c> escrito (tests; null sin sonda).</summary>
        public string? LastHapticLine { get; private set; }

        /// <inheritdoc />
        public void Sfx(string id, float gainDb, int tint, bool clip)
        {
            if (!AudioLinesOn)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"sfx\"");
            AppendTime(sb);
            sb.Append(",\"id\":\"").Append(id).Append('"');
            sb.Append(",\"gain_db\":").Append(gainDb.ToString("0.0", CultureInfo.InvariantCulture));
            if (tint > 0)
            {
                sb.Append(",\"tint\":").Append(tint.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append(",\"src\":\"").Append(clip ? "clip" : AudioProbeIds.Synth).Append("\"}");
            LastSfxLine = sb.ToString();
            Write(LastSfxLine);
        }

        /// <inheritdoc />
        public void Haptic(string kind)
        {
            if (!AudioLinesOn)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"haptic\"");
            AppendTime(sb);
            sb.Append(",\"kind\":\"").Append(kind).Append("\"}");
            LastHapticLine = sb.ToString();
            Write(LastHapticLine);
        }

        /// <inheritdoc />
        public void Build(double workerMs, double mainMs, int clips, long pcmKiB)
        {
            if (!Enabled)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_PERF {\"event\":\"audio_build\"");
            sb.Append(",\"worker_ms\":").Append(workerMs.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(",\"main_ms\":").Append(mainMs.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(",\"clips\":").Append(clips.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"pcm_kib\":").Append(pcmKiB.ToString(CultureInfo.InvariantCulture)).Append('}');
            Write(sb.ToString());
        }

        /// <summary>Último <c>AF_STATE music</c> escrito (tests; null sin sonda).</summary>
        public string? LastMusicLine { get; private set; }

        /// <summary>Último <c>AF_STATE ambient</c> escrito (tests; null sin sonda).</summary>
        public string? LastAmbientLine { get; private set; }

        /// <inheritdoc />
        public void Music(in MusicReport report, string clip)
        {
            // Como sfx: cambia al sellar, en el bucle de juego; en el Editor solo con AudioLinesInEditor.
            if (!AudioLinesOn)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"music\"");
            AppendTime(sb);
            sb.Append(",\"world\":").Append(report.World.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"layers\":").Append(report.Layers.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"night\":").Append(report.Night ? "true" : "false");
            sb.Append(",\"ducked\":").Append(report.Ducked ? "true" : "false");
            sb.Append(",\"menu\":").Append(report.Menu ? "true" : "false");
            sb.Append(",\"clip\":\"").Append(clip).Append("\"}");
            LastMusicLine = sb.ToString();
            Write(LastMusicLine);
        }

        /// <inheritdoc />
        public void Ambient(int world, bool rain, int level, string waterClip, string rainClip)
        {
            if (!Enabled)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"ambient\"");
            AppendTime(sb);
            sb.Append(",\"world\":").Append(world.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"rain\":").Append(rain ? "true" : "false");
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"clip\":\"").Append(waterClip).Append('"');
            if (rain)
            {
                sb.Append(",\"rain_clip\":\"").Append(rainClip).Append('"');
            }

            sb.Append('}');
            LastAmbientLine = sb.ToString();
            Write(LastAmbientLine);
        }

        // Sonido, Música y Vibración guardados, en la línea AF_STATE settings (AU1).
        private void AppendAudioSettings(StringBuilder sb)
        {
            if (Settings == null)
            {
                return;
            }

            sb.Append(",\"sound\":").Append(Settings.Sound ? "true" : "false");
            sb.Append(",\"music\":").Append(Settings.Music ? "true" : "false");
            sb.Append(",\"haptics\":").Append(Settings.Haptics ? "true" : "false");
        }
    }
}
