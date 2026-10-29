#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AF.UI
{
    /// <summary>
    /// Lo que la sonda <c>AF_PERF mem</c> añade para la observación 14 de la jornada 6 (<c>docs/dev/memoria.md</c> §7), solo en
    /// desarrollo:
    /// <list type="bullet">
    /// <item><b>En la línea <c>boot</c></b> (<see cref="FormatGraphicsInfo"/>): con qué se pinta (<c>gfx_api</c>: Vulkan u OpenGLES3;
    /// <c>gfx_device</c>, <c>gfx_version</c>, <c>render_thread</c>), el tamaño de la pantalla y el de lo que se pinta
    /// (<c>screen_px</c>, <c>render_px</c>: si difieren hay un búfer intermedio), <c>msaa</c> y, si el portátil lanzó el juego con
    /// argumentos del reproductor (<c>-e unity "-force-gles -memorysetup-…"</c>), <c>unity_args</c>, para que cada medida diga con qué
    /// se tomó.</item>
    /// <item><b>En todas las líneas, con <c>--ei af_mem_os 1</c></b> (<see cref="OsMemEnabled"/>): el resumen de
    /// <c>dumpsys meminfo</c> del mismo momento, leído en el proceso con <c>android.os.Debug.getMemoryInfo</c>
    /// (<see cref="FormatOsMem"/>): <c>os_pss_mib</c> (TOTAL PSS), <c>os_java_mib</c>, <c>os_native_mib</c>, <c>os_code_mib</c>,
    /// <c>os_graphics_mib</c> (Graphics: <c>EGL mtrack</c> + <c>GL mtrack</c>), <c>os_private_other_mib</c>, <c>os_system_mib</c> y
    /// <c>os_read_ms</c> (lo que tardó la lectura: decenas de ms, por eso va con su extra y no siempre).</item>
    /// </list>
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>Extra de intent (int) que enciende el resumen de memoria del sistema en cada <c>AF_PERF mem</c>.</summary>
        public const string IntentExtraMemOs = "af_mem_os";

        /// <summary>Extra de intent (texto) con los argumentos del reproductor de Unity (lo lee <c>UnityPlayerGameActivity</c>).</summary>
        public const string IntentExtraUnityArgs = "unity";

        // −1: sin leer; 0 apagado; 1 encendido.
        private static int s_memOs = -1;

        /// <summary>Resumen de memoria del sistema en cada línea <c>mem</c> (<c>--ei af_mem_os 1</c>; solo en desarrollo en Android).</summary>
        public static bool OsMemEnabled
        {
            get
            {
                if (s_memOs < 0)
                {
                    s_memOs = Enabled && LaunchOptions.ReadIntentInt(IntentExtraMemOs, 0) == 1 ? 1 : 0;
                }

                return s_memOs == 1;
            }
        }

        /// <summary>
        /// Miembros JSON (con coma delante) de con qué se pinta, para la línea <c>boot</c>. Lee <see cref="SystemInfo"/>,
        /// <see cref="Screen"/> y <see cref="Display.main"/> (hilo principal).
        /// </summary>
        public static string GraphicsInfoJson()
        {
            Display display = Display.main;
            return FormatGraphicsInfo(
                SystemInfo.graphicsDeviceType.ToString(),
                SystemInfo.graphicsDeviceName,
                SystemInfo.graphicsDeviceVersion,
                SystemInfo.renderingThreadingMode.ToString(),
                Screen.width,
                Screen.height,
                display != null ? display.renderingWidth : -1,
                display != null ? display.renderingHeight : -1,
                QualitySettings.antiAliasing,
                LaunchOptions.ReadIntentString(IntentExtraUnityArgs));
        }

        /// <summary>Texto de <see cref="GraphicsInfoJson"/> (puro: no lee nada de Unity). <paramref name="unityArgs"/> null no sale.</summary>
        internal static string FormatGraphicsInfo(string api, string device, string version, string renderThread, int screenW, int screenH,
            int renderW, int renderH, int msaa, string? unityArgs)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(256);
            AppendString(sb, "gfx_api", api);
            AppendString(sb, "gfx_device", device);
            AppendString(sb, "gfx_version", version);
            AppendString(sb, "render_thread", renderThread);
            sb.Append(",\"screen_px\":[").Append(screenW.ToString(inv)).Append(',').Append(screenH.ToString(inv)).Append(']');
            sb.Append(",\"render_px\":[").Append(renderW.ToString(inv)).Append(',').Append(renderH.ToString(inv)).Append(']');
            sb.Append(",\"msaa\":").Append(msaa.ToString(inv));
            if (!string.IsNullOrEmpty(unityArgs))
            {
                AppendString(sb, "unity_args", unityArgs!);
            }

            return sb.ToString();
        }

        /// <summary>
        /// El resumen de <c>dumpsys meminfo</c> del proceso (KB; −1 si no se pudo leer). Solo en Android; en otro sitio, todo −1.
        /// </summary>
        public static OsMemSummary SampleOsMemory()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var watch = Stopwatch.StartNew();
            try
            {
                using (var info = new AndroidJavaObject("android.os.Debug$MemoryInfo"))
                using (var debug = new AndroidJavaClass("android.os.Debug"))
                {
                    debug.CallStatic("getMemoryInfo", info);
                    return new OsMemSummary(
                        Stat(info, "summary.total-pss"),
                        Stat(info, "summary.java-heap"),
                        Stat(info, "summary.native-heap"),
                        Stat(info, "summary.code"),
                        Stat(info, "summary.graphics"),
                        Stat(info, "summary.private-other"),
                        Stat(info, "summary.system"),
                        watch.ElapsedMilliseconds);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[AF.DevProbe] no se pudo leer android.os.Debug.getMemoryInfo: " + e.Message);
                return OsMemSummary.Missing;
            }
#else
            return OsMemSummary.Missing;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static long Stat(AndroidJavaObject info, string name)
        {
            string? value = info.Call<string>("getMemoryStat", name);
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long kb) ? kb : -1;
        }
#endif

        /// <summary>Miembros JSON (con coma delante) del resumen <paramref name="s"/>, en MiB (puro: no lee nada de Unity).</summary>
        internal static string FormatOsMem(in OsMemSummary s)
        {
            var sb = new StringBuilder(220);
            AppendMib(sb, "os_pss_mib", BytesFromKb(s.TotalPssKb));
            AppendMib(sb, "os_java_mib", BytesFromKb(s.JavaHeapKb));
            AppendMib(sb, "os_native_mib", BytesFromKb(s.NativeHeapKb));
            AppendMib(sb, "os_code_mib", BytesFromKb(s.CodeKb));
            AppendMib(sb, "os_graphics_mib", BytesFromKb(s.GraphicsKb));
            AppendMib(sb, "os_private_other_mib", BytesFromKb(s.PrivateOtherKb));
            AppendMib(sb, "os_system_mib", BytesFromKb(s.SystemKb));
            sb.Append(",\"os_read_ms\":").Append(s.ReadMs.ToString(CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        private static long BytesFromKb(long kb)
        {
            return kb < 0 ? -1 : kb * 1024;
        }

        // Cadena JSON: escapa la barra, las comillas y los caracteres de control (\n, \r, \t y el resto por debajo de 0x20 como \uXXXX),
        // que pueden venir en el nombre del driver o en los argumentos del reproductor.
        private static void AppendString(StringBuilder sb, string name, string value)
        {
            sb.Append(",\"").Append(name).Append("\":\"");
            string text = value ?? string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < (char)0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            sb.Append('"');
        }

        /// <summary>Olvida si el extra <c>af_mem_os</c> ya se leyó (tests).</summary>
        internal static void ResetMemOs(bool? value = null)
        {
            s_memOs = value.HasValue ? (value.Value ? 1 : 0) : -1;
        }
    }

    /// <summary>El resumen de <c>dumpsys meminfo</c> del proceso, en KB (−1 = no se pudo leer).</summary>
    public readonly struct OsMemSummary
    {
        /// <summary>Crea el resumen.</summary>
        public OsMemSummary(long totalPssKb, long javaHeapKb, long nativeHeapKb, long codeKb, long graphicsKb, long privateOtherKb, long systemKb, long readMs)
        {
            TotalPssKb = totalPssKb;
            JavaHeapKb = javaHeapKb;
            NativeHeapKb = nativeHeapKb;
            CodeKb = codeKb;
            GraphicsKb = graphicsKb;
            PrivateOtherKb = privateOtherKb;
            SystemKb = systemKb;
            ReadMs = readMs;
        }

        /// <summary>TOTAL PSS.</summary>
        public long TotalPssKb { get; }

        /// <summary>Java Heap.</summary>
        public long JavaHeapKb { get; }

        /// <summary>Native Heap.</summary>
        public long NativeHeapKb { get; }

        /// <summary>Code.</summary>
        public long CodeKb { get; }

        /// <summary>Graphics (EGL mtrack + GL mtrack).</summary>
        public long GraphicsKb { get; }

        /// <summary>Private Other.</summary>
        public long PrivateOtherKb { get; }

        /// <summary>System.</summary>
        public long SystemKb { get; }

        /// <summary>Lo que tardó la lectura (ms).</summary>
        public long ReadMs { get; }

        /// <summary>Sin lectura.</summary>
        public static readonly OsMemSummary Missing = new OsMemSummary(-1, -1, -1, -1, -1, -1, -1, 0);
    }
}
