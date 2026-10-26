#nullable enable
using System.Globalization;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace AF.UI
{
    /// <summary>
    /// Memoria del proceso en momentos fijos (observación 8 de la prueba completa del 2026-10-25; informe
    /// <c>docs/dev/memoria-2026-10-26.md</c>; docs/dev/prototipo-v0.md), solo en desarrollo, como las demás sondas:
    /// <c>AF_PERF {"event":"mem","at":"lobby","t":…,"mono_used_mib":…,"mono_heap_mib":…,"unity_alloc_mib":…,
    /// "unity_reserved_mib":…,"gfx_mib":…,"sys_mib":…,"tex_mib":…,"tex_count":…,"peak_mono_heap_mib":…,"peak_unity_reserved_mib":…,
    /// "peak_sys_mib":…}</c>.
    /// <list type="bullet">
    /// <item><c>at</c>: <c>boot</c> (al cortar la pantalla de arranque, con <c>heap_steps_mib</c> y <c>used_steps_mib</c>: el heap
    /// gestionado reservado y el usado al acabar cada bloque de la carga, en el orden de <c>AF_PERF boot_steps</c>), <c>lobby</c> (cada
    /// vez que se ve el inicio), <c>level</c> (con <c>level</c>: el primer nivel de la sesión y después cada
    /// <see cref="MemEveryLevels"/>, por número de nivel, sin repetir al reiniciar), <c>map</c> (al abrir el Mapa) y <c>daily</c> (al
    /// abrir el Reto del río, con <c>pool_read</c>, si esa apertura leyó el pool, y <c>open_ms</c>, lo que tardó la apertura entera).</item>
    /// <item>Cifras en MiB con un decimal: <c>mono_used</c> y <c>mono_heap</c>, el heap gestionado usado y reservado
    /// (<see cref="Profiler.GetMonoUsedSizeLong"/>, <see cref="Profiler.GetMonoHeapSizeLong"/>; con IL2CPP el reservado no baja: es
    /// el pico); <c>unity_alloc</c> y <c>unity_reserved</c>, la memoria nativa de Unity
    /// (<see cref="Profiler.GetTotalAllocatedMemoryLong"/>, <see cref="Profiler.GetTotalReservedMemoryLong"/>); <c>gfx</c>, la del
    /// driver de gráficos (<see cref="Profiler.GetAllocatedMemoryForGraphicsDriver"/>, solo en desarrollo); <c>sys</c>, la que el
    /// sistema cuenta al proceso (contador «System Used Memory», el que más se parece al PSS de <c>dumpsys meminfo</c>); <c>tex</c> y
    /// <c>tex_count</c>, las texturas cargadas (contadores «Texture Memory» y «Texture Count»). Un contador que no existe en la
    /// plataforma sale como <c>null</c>.</item>
    /// <item><c>peak_*</c>: el máximo de las muestras de la sesión (el pico entre dos muestras no se ve: con IL2CPP el heap reservado
    /// ya lo recoge).</item>
    /// </list>
    /// Para leerlas en el teléfono: <c>adb logcat -d -s Unity | grep "AF_PERF {\"event\":\"mem\""</c>; junto a
    /// <c>adb shell dumpsys meminfo -d com.amazonfill.dev</c> en los mismos momentos.
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>La línea <c>mem</c> de nivel sale con el primer nivel de la sesión y después con cada nivel múltiplo de este.</summary>
        public const int MemEveryLevels = 10;

        private const double Mib = 1024.0 * 1024.0;

        private static ProfilerRecorder s_sysRecorder;
        private static ProfilerRecorder s_texRecorder;
        private static ProfilerRecorder s_texCountRecorder;
        private static bool s_memRecorders;
        private static MemSample s_memPeak = MemSample.Missing;
        private static int s_memLastLevel = -1;

        /// <summary>Última línea <c>AF_PERF mem</c> escrita en el proceso (tests; null sin sonda).</summary>
        public static string? LastMemLine { get; private set; }

        /// <summary>
        /// Empieza a leer los contadores del perfilador («System Used Memory», «Texture Memory», «Texture Count»). Solo en desarrollo;
        /// lo llama <see cref="Initialize"/> en el arranque para que, en la primera muestra, ya tengan el valor de un fotograma.
        /// </summary>
        internal static void StartMemRecorders()
        {
            if (!Enabled || s_memRecorders)
            {
                return;
            }

            s_memRecorders = true;
            s_sysRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            s_texRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");
            s_texCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Count");
        }

        /// <summary>Lee la memoria de ahora (sin escribir nada).</summary>
        public static MemSample SampleMemory()
        {
            StartMemRecorders();
            return new MemSample(
                Profiler.GetMonoUsedSizeLong(),
                Profiler.GetMonoHeapSizeLong(),
                Profiler.GetTotalAllocatedMemoryLong(),
                Profiler.GetTotalReservedMemoryLong(),
                Profiler.GetAllocatedMemoryForGraphicsDriver(),
                Recorded(s_sysRecorder),
                Recorded(s_texRecorder),
                Recorded(s_texCountRecorder));
        }

        /// <summary>
        /// Escribe <c>AF_PERF mem</c> en el momento <paramref name="at"/> (<c>boot</c>, <c>lobby</c>, <c>level</c>, <c>map</c>,
        /// <c>daily</c>); <paramref name="level"/> &gt; 0 lo añade y <paramref name="extraJson"/> (miembros <c>"clave":valor</c>
        /// separados por comas, sin llaves) va al final. Devuelve la línea (null fuera de desarrollo).
        /// </summary>
        public static string? Mem(string at, int level = 0, string? extraJson = null)
        {
            if (!Enabled)
            {
                return null;
            }

            MemSample now = SampleMemory();
            s_memPeak = MemSample.Max(s_memPeak, now);
            string line = FormatMem(at, (long)(Time.realtimeSinceStartupAsDouble * 1000.0), level, now, s_memPeak, extraJson);
            LastMemLine = line;
            Write(line);
            return line;
        }

        /// <summary>
        /// Al cargar el nivel <paramref name="level"/>: <c>AF_PERF mem</c> con <c>at</c> = <c>level</c> si es el primero de la sesión
        /// o un múltiplo de <see cref="MemEveryLevels"/>, y no el mismo que la última vez (reiniciar no repite). Devuelve la línea o null.
        /// </summary>
        public static string? MemLevelLoaded(int level)
        {
            if (!Enabled || level == s_memLastLevel)
            {
                return null;
            }

            bool first = s_memLastLevel < 0;
            if (!first && level % MemEveryLevels != 0)
            {
                return null;
            }

            s_memLastLevel = level;
            return Mem("level", level);
        }

        /// <summary>Olvida los picos y el último nivel medido (tests y una escena nueva de los tests).</summary>
        internal static void ResetMem()
        {
            s_memPeak = MemSample.Missing;
            s_memLastLevel = -1;
            LastMemLine = null;
        }

        /// <summary>Texto de la línea <c>AF_PERF mem</c> (puro: no lee nada de Unity).</summary>
        internal static string FormatMem(string at, long tMs, int level, in MemSample s, in MemSample peak, string? extraJson)
        {
            var sb = new StringBuilder(512);
            sb.Append("AF_PERF {\"event\":\"mem\",\"at\":\"").Append(at).Append('"');
            sb.Append(",\"t\":").Append(tMs.ToString(CultureInfo.InvariantCulture));
            if (level > 0)
            {
                sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            }

            AppendMib(sb, "mono_used_mib", s.MonoUsed);
            AppendMib(sb, "mono_heap_mib", s.MonoHeap);
            AppendMib(sb, "unity_alloc_mib", s.UnityAllocated);
            AppendMib(sb, "unity_reserved_mib", s.UnityReserved);
            AppendMib(sb, "gfx_mib", s.Gfx);
            AppendMib(sb, "sys_mib", s.System);
            AppendMib(sb, "tex_mib", s.Textures);
            sb.Append(",\"tex_count\":");
            if (s.TextureCount >= 0)
            {
                sb.Append(s.TextureCount.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append("null");
            }

            AppendMib(sb, "peak_mono_heap_mib", peak.MonoHeap);
            AppendMib(sb, "peak_unity_reserved_mib", peak.UnityReserved);
            AppendMib(sb, "peak_sys_mib", peak.System);
            if (!string.IsNullOrEmpty(extraJson))
            {
                sb.Append(',').Append(extraJson);
            }

            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Miembros JSON <c>"heap_steps_mib":[…],"used_steps_mib":[…]</c> de los bloques de la carga (−1 sin marca).</summary>
        internal static string FormatBootStepHeaps(long[] heap, long[] used)
        {
            var sb = new StringBuilder(160);
            AppendMibArray(sb, "heap_steps_mib", heap);
            sb.Append(',');
            AppendMibArray(sb, "used_steps_mib", used);
            return sb.ToString();
        }

        /// <summary>
        /// Miembros JSON (con coma delante) de los lienzos del arte por código desde el arranque (<see cref="AF.Presentation.PixelBuffers"/>):
        /// <c>canvas_mib</c> (lo que pidieron), <c>canvas_new_mib</c> (lo que se asignó de verdad) y <c>canvas_reused</c>.
        /// </summary>
        internal static string FormatCanvasStats()
        {
            var sb = new StringBuilder(96);
            AppendMib(sb, "canvas_mib", AF.Presentation.PixelBuffers.CanvasBytes);
            AppendMib(sb, "canvas_new_mib", AF.Presentation.PixelBuffers.NewBytes);
            sb.Append(",\"canvas_reused\":").Append(AF.Presentation.PixelBuffers.Reused.ToString(CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        private static void AppendMibArray(StringBuilder sb, string name, long[] values)
        {
            sb.Append('"').Append(name).Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(values[i] < 0 ? "-1" : (values[i] / Mib).ToString("0.0", CultureInfo.InvariantCulture));
            }

            sb.Append(']');
        }

        private static void AppendMib(StringBuilder sb, string name, long bytes)
        {
            sb.Append(",\"").Append(name).Append("\":");
            if (bytes < 0)
            {
                sb.Append("null");
                return;
            }

            sb.Append((bytes / Mib).ToString("0.0", CultureInfo.InvariantCulture));
        }

        // El último valor de un contador, o −1 si no existe en esta plataforma (o aún no midió ningún fotograma).
        private static long Recorded(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.CurrentValue : -1;
        }
    }

    /// <summary>Una muestra de memoria de <see cref="DevProbe.SampleMemory"/> (bytes; −1 = el contador no existe).</summary>
    public readonly struct MemSample
    {
        /// <summary>Crea la muestra.</summary>
        public MemSample(long monoUsed, long monoHeap, long unityAllocated, long unityReserved, long gfx, long system, long textures, long textureCount)
        {
            MonoUsed = monoUsed;
            MonoHeap = monoHeap;
            UnityAllocated = unityAllocated;
            UnityReserved = unityReserved;
            Gfx = gfx;
            System = system;
            Textures = textures;
            TextureCount = textureCount;
        }

        /// <summary>Heap gestionado usado.</summary>
        public long MonoUsed { get; }

        /// <summary>Heap gestionado reservado.</summary>
        public long MonoHeap { get; }

        /// <summary>Memoria nativa de Unity en uso.</summary>
        public long UnityAllocated { get; }

        /// <summary>Memoria nativa de Unity reservada.</summary>
        public long UnityReserved { get; }

        /// <summary>Memoria del driver de gráficos (0 fuera de desarrollo).</summary>
        public long Gfx { get; }

        /// <summary>Memoria que el sistema cuenta al proceso («System Used Memory»; −1 si no existe).</summary>
        public long System { get; }

        /// <summary>Memoria de las texturas cargadas («Texture Memory»; −1 si no existe).</summary>
        public long Textures { get; }

        /// <summary>Texturas cargadas («Texture Count»; −1 si no existe).</summary>
        public long TextureCount { get; }

        /// <summary>Sin ninguna muestra: todos los campos a −1 (un contador que nunca existe sale como <c>null</c> en el pico).</summary>
        public static readonly MemSample Missing = new MemSample(-1, -1, -1, -1, -1, -1, -1, -1);

        /// <summary>El máximo campo a campo.</summary>
        public static MemSample Max(in MemSample a, in MemSample b)
        {
            return new MemSample(
                a.MonoUsed > b.MonoUsed ? a.MonoUsed : b.MonoUsed,
                a.MonoHeap > b.MonoHeap ? a.MonoHeap : b.MonoHeap,
                a.UnityAllocated > b.UnityAllocated ? a.UnityAllocated : b.UnityAllocated,
                a.UnityReserved > b.UnityReserved ? a.UnityReserved : b.UnityReserved,
                a.Gfx > b.Gfx ? a.Gfx : b.Gfx,
                a.System > b.System ? a.System : b.System,
                a.Textures > b.Textures ? a.Textures : b.Textures,
                a.TextureCount > b.TextureCount ? a.TextureCount : b.TextureCount);
        }
    }
}
