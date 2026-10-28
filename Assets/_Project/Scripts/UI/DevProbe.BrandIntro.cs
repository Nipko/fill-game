#nullable enable
using System.Globalization;
using System.Text;

namespace AF.UI
{
    /// <summary>
    /// Sonda de la intro de la marca (D-051, punto 3; <see cref="BrandIntro"/>), solo en desarrollo:
    /// <c>AF_STATE {"event":"brand_intro","t":…,"intro_ms":3790,"anim_ms":3500,"shown_ms":3500,"cut":false,"frames":215,
    /// "reduce_motion":false,"begun":true,"first_frame_ms":40,"start_ms":1450,"emblem_ms":12,"emblem_cache":"hit","yields":4,
    /// "visible_ms":60,"visible_by":"gpu","play_ms":250,"motion_frames":174,"frame_p95_ms":17.6,"frame_max_ms":24.1,"loading_ms":0,"wait_capped":false}</c>
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
    /// <item>Añadido de D-051 del 2026-10-26: la intro se ve entera (<c>anim_ms</c> 2.500 [P]; 3.500 desde el 2026-10-28) aunque la carga acabe antes, así que
    /// <c>intro_ms</c> ≥ <c>anim_ms</c> y <c>cut</c> es false (solo es true si el arranque se paró antes); <c>loading_ms</c>, lo que
    /// se vio «Cargando…» bajo el nombre porque la carga duró más que la intro (0 si no salió); <c>wait_capped</c>, la espera a la
    /// intro llegó a su tope de seguridad (<c>anim_ms</c> + 2 s tras la carga) y se quitó antes (no debería pasar).</item>
    /// <item>Jornada 4 (2026-10-28; añadido de D-051 del 2026-10-28, <c>anim_ms</c> 3.500): <c>visible_ms</c>, de la creación a
    /// cuando se supo que su primer fotograma ya estaba en la pantalla (la GPU terminó una lectura pedida tras él; cota por arriba),
    /// y <c>visible_by</c> (<c>gpu</c>; <c>cap</c> si la GPU no respondió en 1,2 s; <c>no_readback</c> sin lecturas, en el Editor);
    /// <c>play_ms</c>, de la creación a cuando empezó a moverse (desde ahí cuenta su reloj); <c>motion_frames</c>, fotogramas
    /// pintados mientras se movía (del chorro al nombre entero, 2,9 s), y <c>frame_p95_ms</c> y <c>frame_max_ms</c>, el percentil 95
    /// y el máximo del tiempo entre esos fotogramas (a 60 fps, ~16,7; -1 sin datos).</item>
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
            sb.Append(",\"visible_ms\":").Append(report.VisibleMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"visible_by\":\"").Append(VisibleById(report.VisibleBy)).Append('"');
            sb.Append(",\"play_ms\":").Append(report.PlayMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"motion_frames\":").Append(report.MotionFrames.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"frame_p95_ms\":").Append(report.FrameP95Ms.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(",\"frame_max_ms\":").Append(report.FrameMaxMs.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(",\"loading_ms\":").Append(report.LoadingMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"wait_capped\":").Append(report.WaitCapped ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>El nombre de la sonda de <paramref name="by"/> (<c>none</c>, <c>gpu</c>, <c>cap</c> o <c>no_readback</c>).</summary>
        internal static string VisibleById(BrandIntroVisibleBy by)
        {
            switch (by)
            {
                case BrandIntroVisibleBy.Gpu:
                    return "gpu";
                case BrandIntroVisibleBy.Cap:
                    return "cap";
                case BrandIntroVisibleBy.NoReadback:
                    return "no_readback";
                default:
                    return "none";
            }
        }
    }
}
