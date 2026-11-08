#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La memoria del Mapa del río v2 al abrirlo y cerrarlo varias veces (observación 5 de la prueba de la 1.1 del 2026-11-07:
    /// en el teléfono, <c>GL mtrack</c> no vuelve al cerrar). En el Editor, lo que el Mapa crea vuelve al cerrarlo y no crece de una
    /// vuelta a otra:
    /// <list type="bullet">
    /// <item>las texturas propias del Mapa v2 (<c>map_tex_kib</c>), los <see cref="CanvasRenderer"/> activos y los vértices, a 0; los
    /// lienzos del Mapa, a lo que pesan vacíos (sus lotes se sueltan);</item>
    /// <item>los <see cref="Texture2D"/>, <see cref="Mesh"/>, <see cref="Material"/>, <see cref="Sprite"/> y <see cref="RenderTexture"/>
    /// vivos, iguales tras la 2.ª y la 3.ª vuelta que tras la 1.ª; frente al inicio de antes, solo los iconos del Mapa de la 1.0
    /// (<c>docs/dev/memoria.md</c>: «el Mapa deja sus iconos», ≈ 0,4 MiB), las mallas de sus textos y el material de los países;</item>
    /// <item>la línea <c>AF_PERF mem</c> de <c>map_close</c> lo dice con <c>map_open_n</c> y los <c>map_*</c> a cero.</item>
    /// </list>
    /// El diagnóstico con cifras (<see cref="Diagnose_OpenCloseCycles"/>, manual) escribe una línea <c>AF_MAPMEM</c> por momento:
    /// <code>
    /// scripts/unity-test.ps1 -Mode PlayMode -Architecture x86_64 -Filter 'AF.Tests.PlayMode.MapMemoryCycleTests.Diagnose_OpenCloseCycles' -LogFile Logs/mapmem.log
    /// grep AF_MAPMEM Logs/mapmem.log
    /// </code>
    /// </summary>
    public sealed class MapMemoryCycleTests
    {
        private const long KiB = 1024L;
        private static readonly DateTime T0 = new DateTime(2026, 11, 7, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            DevProbe.ResetMem();
        }

        [Test]
        public void FormatGfxInventory_CountsAndKib()
        {
            var g = new GfxInventory(318, 56552 * KiB + 500, 445, 859 * KiB, 40, 110 * KiB, 143, 42, 208 * KiB);
            Assert.That(DevProbe.FormatGfxInventory(g), Is.EqualTo(
                ",\"obj_tex2d\":318,\"obj_tex2d_kib\":56552,\"obj_mesh\":445,\"obj_mesh_kib\":859,\"obj_mat\":40,\"obj_mat_kib\":110,"
                + "\"obj_sprite\":143,\"obj_canvas\":42,\"obj_canvas_kib\":208"));
        }

        [Test]
        public void FormatMapGfx_TheRound_AndWhatTheMapHolds()
        {
            var m = new MapGfxSample(3, 2700 * KiB, 494 * KiB + 10, 7, 499, 27258);
            Assert.That(DevProbe.FormatMapGfx(m), Is.EqualTo(
                ",\"map_open_n\":3,\"map_tex_kib\":2700,\"map_canvas\":7,\"map_canvas_kib\":494,\"map_renderers\":499,\"map_verts\":27258"));
        }

        [UnityTest]
        public IEnumerator OpenAndClose_ThreeTimes_TheMapLetsGo_AndNothingGrows()
        {
            yield return LoadAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            GfxInventory lobby = Inventory();
            int rtLobby = Resources.FindObjectsOfTypeAll<RenderTexture>().Length;
            int opensBefore = boot.MapOpens;
            GfxInventory first = default;
            for (int round = 1; round <= 3; round++)
            {
                Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
                yield return Frames(20);
                MapGfxSample open = map.SampleGfx(boot.MapOpens);
                Assert.That(open.OpenCount, Is.EqualTo(opensBefore + round));
                Assert.That(open.TextureBytes, Is.GreaterThan(0L), "el arte del Mapa v2, vivo con el Mapa abierto");
                Assert.That(open.Vertices, Is.GreaterThan(1000), "la tierra, el río, la várzea, las orillas y la selva");
                Assert.That(open.Renderers, Is.GreaterThan(100));
                Assert.That(open.Canvases, Is.GreaterThan(1));

                map.ViewBasinButton.onClick.Invoke(); // la otra vista rehace las mallas
                yield return Frames(5);
                map.ViewWorldButton.onClick.Invoke();
                yield return Frames(5);
                Assert.That(boot.CloseRiverMap(), Is.True);
                yield return Frames(10);

                MapGfxSample closed = map.SampleGfx(boot.MapOpens);
                Assert.That(closed.TextureBytes, Is.EqualTo(0L), "vuelta " + round + ": las texturas del Mapa v2 se sueltan");
                Assert.That(closed.Renderers, Is.EqualTo(0), "vuelta " + round + ": ningún CanvasRenderer del Mapa activo");
                Assert.That(closed.Vertices, Is.EqualTo(0), "vuelta " + round + ": ninguna malla del Mapa activa");
                Assert.That(closed.Canvases, Is.EqualTo(0));
                Assert.That(closed.CanvasBytes, Is.LessThan(64L * KiB), "vuelta " + round + ": los lienzos sueltan sus lotes");
                Assert.That(closed.CanvasBytes, Is.LessThan(open.CanvasBytes));
                Assert.That(Resources.FindObjectsOfTypeAll<RenderTexture>().Length, Is.EqualTo(rtLobby), "el Mapa no crea RenderTexture");
                string? line = DevProbe.LastMemLine;
                Assert.That(line, Does.Contain("\"at\":\"map_close\"").And.Contain("\"map_open_n\":" + (opensBefore + round).ToString(CultureInfo.InvariantCulture))
                    .And.Contain("\"map_tex_kib\":0,").And.Contain("\"map_renderers\":0,\"map_verts\":0").And.Contain("\"obj_tex2d\":"));

                GfxInventory now = Inventory();
                if (round == 1)
                {
                    first = now;
                    Assert.That(now.Texture2DCount - lobby.Texture2DCount, Is.LessThanOrEqualTo(12), "solo los iconos del Mapa de la 1.0 se quedan");
                    Assert.That(now.Texture2DBytes - lobby.Texture2DBytes, Is.LessThanOrEqualTo(512L * KiB));
                    Assert.That(now.MaterialCount - lobby.MaterialCount, Is.LessThanOrEqualTo(1), "el material de los países, uno para siempre");
                    continue;
                }

                Assert.That(now.Texture2DCount, Is.EqualTo(first.Texture2DCount), "vuelta " + round + ": sin texturas de más");
                Assert.That(now.Texture2DBytes, Is.EqualTo(first.Texture2DBytes));
                Assert.That(now.MeshCount, Is.EqualTo(first.MeshCount), "vuelta " + round + ": sin mallas de más");
                Assert.That(now.MaterialCount, Is.EqualTo(first.MaterialCount), "vuelta " + round + ": sin materiales de más");
                Assert.That(now.SpriteCount, Is.EqualTo(first.SpriteCount), "vuelta " + round + ": sin sprites de más");
                Assert.That(now.CanvasCount, Is.EqualTo(first.CanvasCount));
                Assert.That(now.CanvasBytes, Is.LessThanOrEqualTo(first.CanvasBytes + (8L * KiB)));
            }
        }

        [UnityTest]
        public IEnumerator SettledAndAfterLines_ComeWithTheMapAtWorkAndAtRest()
        {
            yield return LoadAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return WaitForLine("map_settled", GameBootstrap.MapSettledMs + 2000f);
            string settled = DevProbe.LastMemLine!;
            Assert.That(settled, Does.Contain("\"level\":58").And.Contain("\"map_open_n\":1,").And.Not.Contain("\"map_verts\":0}"));
            Assert.That(boot.CloseRiverMap(), Is.True);
            string close = DevProbe.LastMemLine!; // aún con el arte del Mapa: Object.Destroy lo suelta al final del fotograma
            Assert.That(close, Does.Contain("\"at\":\"map_close\""));
            yield return WaitForLine("map_after", GameBootstrap.MapAfterMs + 2000f);
            string after = DevProbe.LastMemLine!;
            Assert.That(after, Does.Contain("\"map_open_n\":1,\"map_tex_kib\":0,").And.EndWith("\"map_renderers\":0,\"map_verts\":0}"));
            Assert.That(Member(after, "obj_tex2d"), Is.LessThanOrEqualTo(Member(close, "obj_tex2d") - 6),
                "map_after ya no cuenta el arte del Mapa v2 (el atlas, los primeros planos, los Andes, la luz y la niebla, como poco)");
        }

        [UnityTest]
        public IEnumerator Opening_BorrowsTheCanvasBuffers_AndKeepsNone()
        {
            yield return LoadAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(PixelBuffers.Pooling, Is.False, "el arranque ya soltó sus búferes");
            long asked = PixelBuffers.CanvasBytes;
            long fresh = PixelBuffers.NewBytes;
            int reused = PixelBuffers.Reused;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            long askedOpen = PixelBuffers.CanvasBytes - asked;
            long freshOpen = PixelBuffers.NewBytes - fresh;
            Debug.Log("AF_MAPMEM {\"at\":\"pixel_buffers\",\"asked_kib\":" + (askedOpen / KiB).ToString(CultureInfo.InvariantCulture)
                + ",\"new_kib\":" + (freshOpen / KiB).ToString(CultureInfo.InvariantCulture)
                + ",\"reused\":" + (PixelBuffers.Reused - reused).ToString(CultureInfo.InvariantCulture) + "}");
            Assert.That(askedOpen, Is.GreaterThan(8L * 1024L * KiB), "el río vivo, los pueblos, Capi y la tripulación pintan al abrir");
            Assert.That(PixelBuffers.Reused - reused, Is.GreaterThan(0), "los lienzos del Mapa reutilizan búferes mientras se abre");
            Assert.That(freshOpen, Is.LessThan(askedOpen));
            Assert.That(PixelBuffers.Pooling, Is.False, "al acabar de abrir ya no se guardan");
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0L), "ningún búfer queda vivo");
            Assert.That(boot.CloseRiverMap(), Is.True);
        }

        [UnityTest]
        public IEnumerator AfMapV1_OpensTheMapOf10_ForTheReferenceMeasure()
        {
            LaunchOptions.MapV1Override = true;
            yield return LoadAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.MapLiveSettings.UiMapV2, Is.False, "af_map_v1 apaga ui_map_v2 en desarrollo");
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
            Assert.That(boot.RiverMapView!.LiveActive, Is.False, "el Mapa de la 1.0");
            Assert.That(boot.RiverMapView.SampleGfx(boot.MapOpens).TextureBytes, Is.EqualTo(0L), "sin el arte del Mapa v2");
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(LaunchOptions.ResolveMapV1(false), Is.False, "nunca en release");
        }

        // El valor entero del miembro "name" de una línea de la sonda.
        private static long Member(string line, string name)
        {
            string key = "\"" + name + "\":";
            int i = line.IndexOf(key, StringComparison.Ordinal);
            Assert.That(i, Is.GreaterThanOrEqualTo(0), name + " en " + line);
            i += key.Length;
            int j = i;
            while (j < line.Length && (char.IsDigit(line[j]) || line[j] == '-'))
            {
                j++;
            }

            return long.Parse(line.Substring(i, j - i), CultureInfo.InvariantCulture);
        }

        private static IEnumerator WaitForLine(string at, float maxMs)
        {
            float start = Time.realtimeSinceStartup;
            while ((DevProbe.LastMemLine == null || !DevProbe.LastMemLine.Contains("\"at\":\"" + at + "\""))
                && (Time.realtimeSinceStartup - start) * 1000f < maxMs)
            {
                yield return null;
            }

            Assert.That(DevProbe.LastMemLine, Does.Contain("\"at\":\"" + at + "\""), "sale la línea " + at);
        }

        [UnityTest]
        [Explicit("Diagnóstico manual: AF_MAPMEM en el log (docs/dev/memoria.md).")]
        public IEnumerator Diagnose_OpenCloseCycles()
        {
            var recorders = new ProfilerRecorder[Counters.Length];
            for (int i = 0; i < Counters.Length; i++)
            {
                recorders[i] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, Counters[i]);
            }

            try
            {
                yield return LoadAt(58);
                GameBootstrap boot = GameBootstrap.Current!;
                yield return Frames(10);
                Report("lobby", boot, recorders);
                var before = new HashSet<int>();
                foreach (UnityEngine.Object o in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
                {
                    before.Add(o.GetInstanceID());
                }

                for (int round = 1; round <= 3; round++)
                {
                    Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
                    yield return Frames(20);
                    Report("open" + round, boot, recorders);
                    boot.RiverMapView!.ViewBasinButton.onClick.Invoke();
                    yield return Frames(5);
                    boot.RiverMapView!.ViewWorldButton.onClick.Invoke();
                    yield return Frames(5);
                    Assert.That(boot.CloseRiverMap(), Is.True);
                    yield return Frames(10);
                    Report("close" + round, boot, recorders);
                    if (round != 1)
                    {
                        continue;
                    }

                    // Lo que el Mapa deja vivo tras la primera vuelta (ni GameObject ni componentes).
                    foreach (UnityEngine.Object o in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
                    {
                        if (!before.Contains(o.GetInstanceID()) && !(o is GameObject) && !(o is Component))
                        {
                            Debug.Log("AF_MAPNEW " + o.GetType().Name + " '" + o.name + "' "
                                + Profiler.GetRuntimeMemorySizeLong(o).ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }
            }
            finally
            {
                for (int i = 0; i < recorders.Length; i++)
                {
                    recorders[i].Dispose();
                }
            }
        }

        private static readonly string[] Counters = { "Texture Memory", "Mesh Memory", "Gfx Used Memory", "System Used Memory" };

        private static GfxInventory Inventory()
        {
            GC.Collect();
            return DevProbe.SampleGfxInventory();
        }

        private static IEnumerator LoadAt(int playLevel)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = playLevel;
            profile.Progress.MaxLevel = playLevel - 1;
            profile.Progress.MapSeenLevel = playLevel - 1; // sin «ponerse al día»
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            yield return Frames(10);
        }

        private static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        private static void Report(string at, GameBootstrap boot, ProfilerRecorder[] recorders)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder("AF_MAPMEM {\"at\":\"").Append(at).Append('"');
            sb.Append(DevProbe.FormatGfxInventory(Inventory()));
            sb.Append(DevProbe.FormatMapGfx(boot.RiverMapView!.SampleGfx(boot.MapOpens)));
            sb.Append(",\"rt\":").Append(Resources.FindObjectsOfTypeAll<RenderTexture>().Length.ToString(inv));
            sb.Append(",\"unity_alloc_mib\":").Append((Profiler.GetTotalAllocatedMemoryLong() / 1048576.0).ToString("0.0", inv));
            sb.Append(",\"mono_heap_mib\":").Append((Profiler.GetMonoHeapSizeLong() / 1048576.0).ToString("0.0", inv));
            sb.Append(",\"pixel_new_mib\":").Append((PixelBuffers.NewBytes / 1048576.0).ToString("0.0", inv));
            sb.Append(",\"mono_used_mib\":").Append((Profiler.GetMonoUsedSizeLong() / 1048576.0).ToString("0.0", inv));
            for (int i = 0; i < Counters.Length; i++)
            {
                sb.Append(",\"").Append(Counters[i].Replace(' ', '_')).Append("_mib\":")
                    .Append((recorders[i].Valid ? recorders[i].LastValue / 1048576.0 : -1).ToString("0.0", inv));
            }

            sb.Append('}');
            Debug.Log(sb.ToString());
        }
    }
}
