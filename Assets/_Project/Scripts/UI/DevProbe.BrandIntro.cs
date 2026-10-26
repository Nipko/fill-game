#nullable enable
using System.Globalization;
using System.Text;

namespace AF.UI
{
    /// <summary>
    /// Sonda de la intro de la marca (D-051, punto 3; <see cref="BrandIntro"/>), solo en desarrollo:
    /// <c>AF_STATE {"event":"brand_intro","t":…,"intro_ms":2540,"anim_ms":2500,"shown_ms":2500,"cut":false,"frames":150,
    /// "reduce_motion":false,"begun":true,"first_frame_ms":40,"start_ms":1450,"emblem_ms":12,"emblem_cache":"hit","yields":5,"loading_ms":0,"wait_capped":false}</c>
    /// al quitarse: al terminar la carga, si la intro ya se vio entera, o al acabar la intro.
    /// <list type="bullet">
    /// <item><c>intro_ms</c>: tiempo a la vista (ms reales, desde la creación de la pantalla de arranque, el origen de
    /// <c>load_ms</c>, hasta que se quita: el mayor de la carga y la intro entera); <c>anim_ms</c>: lo que dura la intro entera;
    /// <c>shown_ms</c>: hasta dónde llegó.</item>
    /// <item><c>cut</c>: se quitó antes de verse entera (solo si el arranque se paró antes); <c>frames</c>: fotogramas pintados con
    /// la intro a la vista; <c>reduce_motion</c>: salió quieta; <c>begun</c>: false si el arranque se paró antes de leer el
    /// perfil.</item>
    /// <item>Jornada 3 de D-051 (2026-10-26): <c>first_frame_ms</c>, de la creación de la pantalla a su primer fotograma (desde ahí
    /// cuenta el reloj de la animación); <c>start_ms</c>, de que arrancó Unity a la creación de la pantalla (en el teléfono, el
    /// arranque del motor, que no depende del juego); <c>emblem_ms</c>, lo que costó tener la escena de la marca, y
    /// <c>emblem_cache</c>, de dónde salió (<c>hit</c>: de la caché de disco; <c>miss</c>: dibujada y guardada; <c>off</c>: sin caché,
    /// en el Editor); <c>yields</c>, los fotogramas que la carga dejó pintar para que la intro se viera moverse.</item>
    /// <item>Añadido de D-051 del 2026-10-26: la intro se ve entera (<c>anim_ms</c> 2.500 [P]) aunque la carga acabe antes, así que
    /// <c>intro_ms</c> ≥ <c>anim_ms</c> y <c>cut</c> es false (solo es true si el arranque se paró antes); <c>loading_ms</c>, lo que
    /// se vio «Cargando…» bajo el nombre porque la carga duró más que la intro (0 si no salió); <c>wait_capped</c>, la espera a la
    /// intro llegó a su tope de seguridad (<c>anim_ms</c> + 2 s tras la carga) y se quitó antes (no debería pasar).</item>
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
            sb.Append(",\"loading_ms\":").Append(report.LoadingMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"wait_capped\":").Append(report.WaitCapped ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }
    }
}
