#nullable enable
using System.Globalization;
using System.Text;

namespace AF.UI
{
    /// <summary>
    /// Sonda de la intro de la marca (D-051, punto 3; <see cref="BrandIntro"/>), solo en desarrollo:
    /// <c>AF_STATE {"event":"brand_intro","t":…,"intro_ms":412,"anim_ms":650,"shown_ms":412,"cut":true,"frames":3,
    /// "reduce_motion":false,"begun":true,"first_frame_ms":40,"start_ms":1450,"emblem_ms":12,"emblem_cache":"hit","yields":3}</c>
    /// al quitarse, al terminar la carga.
    /// <list type="bullet">
    /// <item><c>intro_ms</c>: tiempo a la vista (ms reales, desde el primer fotograma de la pantalla de arranque hasta el fin de la
    /// carga; coincide con <c>load_ms</c> de <c>AF_STATE boot</c>: la intro no alarga nada); <c>anim_ms</c>: lo que dura la
    /// animación entera; <c>shown_ms</c>: hasta dónde llegó.</item>
    /// <item><c>cut</c>: la carga terminó antes que la animación y se cortó a medias (si es false sin Reducir movimiento, la
    /// carga duró más y la intro esperó quieta en su último fotograma); <c>frames</c>: fotogramas pintados con la intro a la
    /// vista (la carga va en bloques síncronos: pocos); <c>reduce_motion</c>: salió quieta; <c>begun</c>: false si el arranque se
    /// paró antes de leer el perfil.</item>
    /// <item>Jornada 3 de D-051 (2026-10-26): <c>first_frame_ms</c>, de la creación de la pantalla a su primer fotograma (desde ahí
    /// cuenta el reloj de la animación); <c>start_ms</c>, de que arrancó Unity a la creación de la pantalla (en el teléfono, el
    /// arranque del motor, que no depende del juego); <c>emblem_ms</c>, lo que costó tener la escena de la marca, y
    /// <c>emblem_cache</c>, de dónde salió (<c>hit</c>: de la caché de disco; <c>miss</c>: dibujada y guardada; <c>off</c>: sin caché,
    /// en el Editor); <c>yields</c>, los fotogramas que la carga dejó pintar para que la intro se viera moverse.</item>
    /// </list>
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>Escribe <c>AF_STATE brand_intro</c> (al quitarse la intro de la marca).</summary>
        public void BrandIntro(BrandIntroReport report)
        {
            string? line = FormatBrandIntro(report);
            if (line != null)
            {
                Write(line);
            }
        }

        /// <summary>La línea <c>AF_STATE brand_intro</c> de <paramref name="report"/> (null fuera de desarrollo).</summary>
        public string? FormatBrandIntro(BrandIntroReport report)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"brand_intro\"");
            AppendTime(sb);
            sb.Append(",\"intro_ms\":").Append(report.IntroMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"anim_ms\":").Append(global::AF.UI.BrandIntro.AnimMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"shown_ms\":").Append(report.ShownMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"cut\":").Append(report.Cut ? "true" : "false");
            sb.Append(",\"frames\":").Append(report.Frames.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"reduce_motion\":").Append(report.ReduceMotion ? "true" : "false");
            sb.Append(",\"begun\":").Append(report.Begun ? "true" : "false");
            sb.Append(",\"first_frame_ms\":").Append(report.FirstFrameMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"start_ms\":").Append(report.StartMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"emblem_ms\":").Append(report.EmblemMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"emblem_cache\":\"").Append(GameBootstrap.CacheId(report.EmblemCache)).Append('"');
            sb.Append(",\"yields\":").Append(report.Yields.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }
    }
}
