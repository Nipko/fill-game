#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Inventario de las texturas y los RenderTexture vivos por momento (observación 6 de la jornada 6 de la prueba completa,
    /// bloque 19; informe <c>docs/dev/memoria.md</c>). Manual (<c>[Explicit]</c>): carga el juego, pasa por el inicio, el Mapa y
    /// un nivel, y en cada momento escribe una línea <c>AF_MEMINV</c> por textura creada desde que empezó el test (nombre, tamaño,
    /// formato, mipmaps, si guarda copia en CPU y los bytes estimados en la GPU) y una línea de totales. Se lanza sola:
    /// <code>
    /// scripts/unity-test.ps1 -Mode PlayMode -Architecture x86_64 -Filter 'AF.Tests.PlayMode.MemoryInventoryTests' -LogFile Logs/meminv.log
    /// grep AF_MEMINV Logs/meminv.log
    /// </code>
    /// Las texturas del Editor (las que ya existían antes del test) no cuentan; lo que se ve es lo que crea el juego.
    /// </summary>
    public sealed class MemoryInventoryTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Monday = new DateTime(2026, 10, 19, 10, 0, 0);

        [TearDown]
        public void TearDown()
        {
            DevProbe.ResetMem();
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
        }

        [UnityTest]
        [Explicit("Inventario manual de memoria en el Editor (docs/dev/memoria.md).")]
        public IEnumerator Inventory_Moments()
        {
            var before = new HashSet<int>();
            foreach (UnityEngine.Object o in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
            {
                before.Add(o.GetInstanceID());
            }

            var clock = new ManualClock(Utc(Monday), Bogota);
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(74)));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return Frames(30);
            Report("lobby", before);

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return Frames(30);
            Report("map", before);
            Assert.That(boot.CloseRiverMap(), Is.True);
            yield return Frames(10);

            Assert.That(boot.OpenAvatarScreen("lobby"), Is.True);
            yield return Frames(60);
            Report("avatars", before);
            Assert.That(boot.CloseAvatarScreen(), Is.True);
            yield return Frames(10);

            Assert.That(boot.PressPlay(), Is.True);
            yield return Frames(90);
            Report("level75", before);
        }

        private static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        private static void Report(string at, HashSet<int> before)
        {
            GC.Collect();
            CultureInfo inv = CultureInfo.InvariantCulture;
            var rows = new List<(long gpu, string line)>();
            long gpuTotal = 0, cpuTotal = 0, mipTotal = 0, rtTotal = 0;
            int count = 0;
            foreach (Texture t in Resources.FindObjectsOfTypeAll<Texture>())
            {
                if (before.Contains(t.GetInstanceID()))
                {
                    continue;
                }

                count++;
                GraphicsFormat fmt = t.graphicsFormat;
                int mips = t.mipmapCount;
                long gpu = 0, top = 0;
                bool rt = t is RenderTexture;
                if (rt)
                {
                    var r = (RenderTexture)t;
                    gpu = Profiler.GetRuntimeMemorySizeLong(r);
                    top = gpu;
                    rtTotal += gpu;
                }
                else
                {
                    for (int m = 0; m < Math.Max(1, mips); m++)
                    {
                        int w = Math.Max(1, t.width >> m), h = Math.Max(1, t.height >> m);
                        long b = fmt == GraphicsFormat.None ? 0 : (long)GraphicsFormatUtility.ComputeMipmapSize(w, h, fmt);
                        gpu += b;
                        if (m == 0)
                        {
                            top = b;
                        }
                    }
                }

                bool readable = t.isReadable;
                long cpu = readable && !rt ? gpu : 0;
                gpuTotal += gpu;
                cpuTotal += cpu;
                mipTotal += gpu - top;
                rows.Add((gpu + cpu, "AF_MEMINV {\"at\":\"" + at + "\",\"name\":\"" + Escape(t.name) + "\",\"type\":\"" + t.GetType().Name
                    + "\",\"w\":" + t.width.ToString(inv) + ",\"h\":" + t.height.ToString(inv) + ",\"fmt\":\"" + fmt + "\",\"mips\":" + mips.ToString(inv)
                    + ",\"readable\":" + (readable ? "true" : "false") + ",\"gpu_kib\":" + (gpu / 1024).ToString(inv)
                    + ",\"cpu_kib\":" + (cpu / 1024).ToString(inv) + ",\"runtime_kib\":" + (Profiler.GetRuntimeMemorySizeLong(t) / 1024).ToString(inv) + "}"));
            }

            rows.Sort((a, b) => b.gpu.CompareTo(a.gpu));
            var sb = new StringBuilder();
            for (int i = 0; i < rows.Count; i++)
            {
                sb.AppendLine(rows[i].line);
            }

            Debug.Log(sb.ToString());
            var byType = new SortedDictionary<string, (int n, long bytes)>(StringComparer.Ordinal);
            foreach (UnityEngine.Object o in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
            {
                if (before.Contains(o.GetInstanceID()))
                {
                    continue;
                }

                string type = o.GetType().Name;
                byType.TryGetValue(type, out (int n, long bytes) acc);
                byType[type] = (acc.n + 1, acc.bytes + Profiler.GetRuntimeMemorySizeLong(o));
            }

            var types = new StringBuilder("AF_MEMINV {\"at\":\"" + at + "\",\"by_type\":{");
            bool first = true;
            foreach (KeyValuePair<string, (int n, long bytes)> kv in byType)
            {
                if (kv.Value.bytes < 64 * 1024)
                {
                    continue;
                }

                types.Append(first ? "" : ",").Append('"').Append(kv.Key).Append("\":[").Append(kv.Value.n.ToString(inv)).Append(',')
                    .Append(Mib(kv.Value.bytes).ToString("0.00", inv)).Append(']');
                first = false;
            }

            Debug.Log(types.Append("}}").ToString());
            MemSample s = DevProbe.SampleMemory();
            Debug.Log("AF_MEMINV {\"at\":\"" + at + "\",\"total\":true,\"count\":" + count.ToString(inv)
                + ",\"gpu_mib\":" + Mib(gpuTotal).ToString("0.0", inv) + ",\"cpu_copy_mib\":" + Mib(cpuTotal).ToString("0.0", inv)
                + ",\"mips_mib\":" + Mib(mipTotal).ToString("0.0", inv) + ",\"rt_mib\":" + Mib(rtTotal).ToString("0.0", inv)
                + ",\"current_texture_mib\":" + Mib((long)Texture.currentTextureMemory).ToString("0.0", inv)
                + ",\"mono_used_mib\":" + Mib(s.MonoUsed).ToString("0.0", inv) + "}");
        }

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static double Mib(long bytes)
        {
            return bytes / (1024.0 * 1024.0);
        }

        private static Profile ProfileAt(int maxLevel)
        {
            Profile profile = AF.Economy.Wallet.NewProfile(new ManualClock(Utc(Monday), Bogota));
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            return profile;
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }
    }
}
