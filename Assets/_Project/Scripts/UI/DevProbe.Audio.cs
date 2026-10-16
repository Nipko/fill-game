#nullable enable
using System.Globalization;
using System.Text;
using AF.Audio;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sonda del audio provisional (tarea AU1; docs/dev/prototipo-v0.md), solo en desarrollo, para probar en el teléfono por
    /// logcat lo que no se puede oír desde el ordenador:
    /// <list type="bullet">
    /// <item><c>AF_STATE {"event":"sfx","t":…,"id":"sfx_select","gain_db":-36.0,"tint":4}</c> en cada efecto que suena (con Sonido
    /// apagado, en silencio o con un anuncio delante no sale ninguno): su ID de la lista, su pico en dBFS y, en los afinados, la
    /// fruta (índice de motor 1-14).</item>
    /// <item><c>AF_STATE {"event":"haptic","t":…,"kind":"haptic_seal"}</c> en cada vibración (con Vibración apagada no sale).</item>
    /// <item><c>AF_STATE {"event":"music","t":…,"world":3,"layers":1,"night":false,"ducked":false,"menu":false}</c> cada vez que
    /// cambia la música (tarea AU2): el mundo del lecho (0 en el inicio, con <c>menu</c> true), las capas de percusión pedidas, la
    /// variante nocturna de Muy difícil y la atenuación de 6 dB (toada o modal). Como <c>sfx</c>, en el Editor solo con
    /// <see cref="AudioLinesInEditor"/>.</item>
    /// <item><c>AF_STATE {"event":"ambient","t":…,"world":4,"rain":true,"level":95}</c> al cargar un nivel (y con world 0 al abrir
    /// el inicio): el agua del mundo y si el nivel tiene lluvia.</item>
    /// <item><c>AF_PERF {"event":"audio_build","worker_ms":…,"main_ms":…,"clips":…,"pcm_kib":…}</c> una vez, cuando los
    /// clips quedan listos: lo que tardó el hilo de trabajo en generar el PCM, lo que pagó el hilo principal al crearlos y la
    /// memoria PCM (en float).</item>
    /// </list>
    /// </summary>
    public sealed partial class DevProbe : IAudioProbe
    {
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
        public void Sfx(string id, float gainDb, int tint)
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

            sb.Append('}');
            LastSfxLine = sb.ToString();
            Debug.Log(LastSfxLine);
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
            Debug.Log(LastHapticLine);
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
            Debug.Log(sb.ToString());
        }

        /// <summary>Último <c>AF_STATE music</c> escrito (tests; null sin sonda).</summary>
        public string? LastMusicLine { get; private set; }

        /// <summary>Último <c>AF_STATE ambient</c> escrito (tests; null sin sonda).</summary>
        public string? LastAmbientLine { get; private set; }

        /// <inheritdoc />
        public void Music(in MusicReport report)
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
            sb.Append(",\"menu\":").Append(report.Menu ? "true" : "false").Append('}');
            LastMusicLine = sb.ToString();
            Debug.Log(LastMusicLine);
        }

        /// <inheritdoc />
        public void Ambient(int world, bool rain, int level)
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
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture)).Append('}');
            LastAmbientLine = sb.ToString();
            Debug.Log(LastAmbientLine);
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
