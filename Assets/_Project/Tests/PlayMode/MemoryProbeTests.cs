#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using AF.GameFlow;
using AF.Levels;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Memoria (observación 8 de la prueba completa del 2026-10-25; <c>docs/dev/memoria-2026-10-26.md</c>): la sonda
    /// <c>AF_PERF mem</c> (<see cref="DevProbe.Mem"/>; formato, momentos y la regla de «cada 10 niveles»), el pool del Reto que ya no
    /// se lee al arrancar sino al abrirlo (con el mismo tablero del día) y, como prueba manual <c>[Explicit]</c>,
    /// <see cref="Measure_Moments"/>: las cifras del Editor por momento que recoge el informe.
    /// </summary>
    public sealed class MemoryProbeTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Monday = new DateTime(2026, 10, 19, 10, 0, 0);

        private static readonly Regex MemLine = new Regex(
            "^AF_PERF \\{\"event\":\"mem\",\"at\":\"([a-z_]+)\",\"t\":\\d+(,\"level\":\\d+)?,\"mono_used_mib\":\\d+\\.\\d,\"mono_heap_mib\":\\d+\\.\\d,"
            + "\"unity_alloc_mib\":\\d+\\.\\d,\"unity_reserved_mib\":\\d+\\.\\d,\"gfx_mib\":(\\d+\\.\\d|null),\"sys_mib\":(\\d+\\.\\d|null),"
            + "\"tex_mib\":(\\d+\\.\\d|null),\"tex_count\":(\\d+|null),\"peak_mono_heap_mib\":\\d+\\.\\d,\"peak_unity_reserved_mib\":\\d+\\.\\d,"
            + "\"peak_sys_mib\":(\\d+\\.\\d|null)(,.*)?\\}$");

        [SetUp]
        public void SetUp()
        {
            DevProbe.ResetMem();
        }

        [TearDown]
        public void TearDown()
        {
            DevProbe.ResetMem();
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
        }

        [Test]
        public void FormatMem_MiBWithOneDecimal_NullForMissingCounters_LevelAndExtraOptional()
        {
            const long MiB = 1024L * 1024L;
            var s = new MemSample(10 * MiB + MiB / 2, 64 * MiB, 200 * MiB, 300 * MiB, 0, -1, 12 * MiB, 340);
            var peak = new MemSample(0, 80 * MiB, 0, 310 * MiB, 0, -1, 0, 0);

            string line = DevProbe.FormatMem("level", 1234, 40, s, peak, "\"pool_read\":false");

            Assert.That(line, Is.EqualTo(
                "AF_PERF {\"event\":\"mem\",\"at\":\"level\",\"t\":1234,\"level\":40,\"mono_used_mib\":10.5,\"mono_heap_mib\":64.0,"
                + "\"unity_alloc_mib\":200.0,\"unity_reserved_mib\":300.0,\"gfx_mib\":0.0,\"sys_mib\":null,\"tex_mib\":12.0,\"tex_count\":340,"
                + "\"peak_mono_heap_mib\":80.0,\"peak_unity_reserved_mib\":310.0,\"peak_sys_mib\":null,\"pool_read\":false}"));
            Assert.That(MemLine.IsMatch(line), Is.True);
            Assert.That(DevProbe.FormatMem("map", 5, 0, s, peak, null), Does.Not.Contain("\"level\""));
        }

        /// <summary>
        /// El detalle de la jornada 6 (bloque 19; <c>docs/dev/memoria.md</c>): texturas según <c>Texture.currentTextureMemory</c>, lo
        /// reservado sin usar, los contadores (null si no existen) y los RenderTexture con los mayores por nombre y tamaño.
        /// </summary>
        [Test]
        public void FormatMemDetail_CountersNullWhenMissing_RenderTexturesWithTheirSize()
        {
            const long MiB = 1024L * 1024L;
            var d = new MemDetail(48 * MiB, 3 * MiB, 120 * MiB, 7 * MiB, MiB / 2, -1, 600 * MiB, -1, 20 * MiB, 2,
                new[] { "_CameraTargetAttachmentA 1080x2340 9.6", "q\"x 4x4 0.0" });

            Assert.That(DevProbe.FormatMemDetail(d), Is.EqualTo(
                "\"tex_cur_mib\":48.0,\"unity_unused_mib\":3.0,\"gfx_reserved_mib\":120.0,\"audio_mib\":7.0,\"mesh_mib\":0.5,"
                + "\"profiler_mib\":null,\"app_resident_mib\":600.0,\"app_committed_mib\":null,\"rt_mib\":20.0,\"rt_count\":2,"
                + "\"rt_top\":[\"_CameraTargetAttachmentA 1080x2340 9.6\",\"q\\\"x 4x4 0.0\"]"));
            Assert.That(DevProbe.FormatMemDetail(new MemDetail(0, 0, -1, -1, -1, -1, -1, -1, 0, 0, null)), Does.EndWith("\"rt_count\":0,\"rt_top\":[]"));
        }

        [Test]
        public void Mem_CarriesTheDetail_BeforeTheMomentMembers()
        {
            string? line = DevProbe.Mem("daily", 0, "\"pool_read\":true");
            Assert.That(MemLine.IsMatch(line!), Is.True, line);
            Assert.That(line, Does.Match(",\"tex_cur_mib\":\\d+\\.\\d,\"unity_unused_mib\":\\d+\\.\\d,"));
            Assert.That(line, Does.Contain(",\"rt_count\":").And.Contain(",\"rt_top\":["));
            Assert.That(line, Does.EndWith("],\"pool_read\":true}"), "lo propio del momento, al final");
        }

        /// <summary>
        /// Observación 14 de la jornada 6 (<c>docs/dev/memoria.md</c> §7): la línea <c>boot</c> dice con qué se pinta y con qué
        /// argumentos del reproductor se lanzó (si los hay), y con <c>af_mem_os</c> cada línea lleva el resumen de
        /// <c>dumpsys meminfo</c>, null si no se pudo leer (en el Editor, siempre).
        /// </summary>
        [Test]
        public void FormatGraphicsInfo_ApiScreenRenderAndUnityArgsOnlyWhenGiven()
        {
            Assert.That(DevProbe.FormatGraphicsInfo("Vulkan", "Adreno (TM) 830", "Vulkan 1.3.284 [0x\"1\"]", "NativeGraphicsJobs", 1080, 2340, 1080, 2340, 1,
                    "-force-gles -memorysetup-main-allocator-block-size=4194304"),
                Is.EqualTo(",\"gfx_api\":\"Vulkan\",\"gfx_device\":\"Adreno (TM) 830\",\"gfx_version\":\"Vulkan 1.3.284 [0x\\\"1\\\"]\","
                    + "\"render_thread\":\"NativeGraphicsJobs\",\"screen_px\":[1080,2340],\"render_px\":[1080,2340],\"msaa\":1,"
                    + "\"unity_args\":\"-force-gles -memorysetup-main-allocator-block-size=4194304\""));
            Assert.That(DevProbe.FormatGraphicsInfo("OpenGLES3", "x", "y", "MultiThreaded", 720, 1600, -1, -1, 0, null), Does.Not.Contain("unity_args"));
            Assert.That(DevProbe.FormatGraphicsInfo("OpenGLES3", "x", "y", "MultiThreaded", 720, 1600, -1, -1, 0, ""), Does.EndWith("\"msaa\":0"));
            Assert.That(DevProbe.FormatGraphicsInfo("OpenGLES3", "a\nb\r\tc\u0001", "y", "MultiThreaded", 720, 1600, -1, -1, 0, null),
                Does.Contain(",\"gfx_device\":\"a\\nb\\r\\tc\\u0001\","), "los caracteres de control, escapados");
            Assert.That(DevProbe.GraphicsInfoJson(), Does.StartWith(",\"gfx_api\":\"").And.Contain("\"screen_px\":["));
        }

        [Test]
        public void FormatOsMem_MiBFromKb_NullWhenMissing()
        {
            var s = new OsMemSummary(611883, 20976, 36824, 101940, 139904, 270696, 36215, 23);
            Assert.That(DevProbe.FormatOsMem(s), Is.EqualTo(
                ",\"os_pss_mib\":597.5,\"os_java_mib\":20.5,\"os_native_mib\":36.0,\"os_code_mib\":99.6,\"os_graphics_mib\":136.6,"
                + "\"os_private_other_mib\":264.4,\"os_system_mib\":35.4,\"os_read_ms\":23"));
            Assert.That(DevProbe.FormatOsMem(OsMemSummary.Missing), Is.EqualTo(
                ",\"os_pss_mib\":null,\"os_java_mib\":null,\"os_native_mib\":null,\"os_code_mib\":null,\"os_graphics_mib\":null,"
                + "\"os_private_other_mib\":null,\"os_system_mib\":null,\"os_read_ms\":0"));
        }

        [Test]
        public void Mem_WithOsMem_AddsTheSummaryBeforeTheMomentMembers_AndNotWithout()
        {
            try
            {
                DevProbe.ResetMemOs(false);
                Assert.That(DevProbe.Mem("lobby"), Does.Not.Contain("os_pss_mib"));
                DevProbe.ResetMemOs(true);
                string? line = DevProbe.Mem("daily", 0, "\"pool_read\":true");
                Assert.That(MemLine.IsMatch(line!), Is.True, line);
                Assert.That(line, Does.Contain(",\"os_pss_mib\":null,").And.Contain("\"os_read_ms\":0,\"pool_read\":true}"), "en el Editor no hay meminfo");
            }
            finally
            {
                DevProbe.ResetMemOs();
            }
        }

        [Test]
        public void FormatBootStepHeaps_OneValuePerStep_MinusOneWithoutMark()
        {
            const long MiB = 1024L * 1024L;
            string json = DevProbe.FormatBootStepHeaps(new[] { 32 * MiB, -1L }, new[] { 20 * MiB + MiB / 4, -1L });
            Assert.That(json, Is.EqualTo("\"heap_steps_mib\":[32.0,-1],\"used_steps_mib\":[20.3,-1]"));
        }

        [Test]
        public void Mem_InTheEditor_WritesTheLine_AndKeepsPeaks()
        {
            string? line = DevProbe.Mem("lobby");
            Assert.That(line, Is.Not.Null, "el Editor cuenta como desarrollo");
            Assert.That(MemLine.Match(line!).Groups[1].Value, Is.EqualTo("lobby"));
            Assert.That(DevProbe.LastMemLine, Is.EqualTo(line));
            MemSample now = DevProbe.SampleMemory();
            Assert.That(now.MonoHeap, Is.GreaterThan(0));
            Assert.That(now.MonoUsed, Is.GreaterThan(0));
            Assert.That(now.MonoUsed, Is.LessThanOrEqualTo(now.MonoHeap));
        }

        [Test]
        public void MemLevelLoaded_FirstLevel_ThenEveryTenth_WithoutRepeatingOnRestart()
        {
            Assert.That(DevProbe.MemLevelLoaded(61), Does.Contain("\"at\":\"level\",").And.Contain("\"level\":61"), "el primero de la sesión");
            Assert.That(DevProbe.MemLevelLoaded(61), Is.Null, "reiniciar no repite");
            Assert.That(DevProbe.MemLevelLoaded(62), Is.Null);
            Assert.That(DevProbe.MemLevelLoaded(69), Is.Null);
            Assert.That(DevProbe.MemLevelLoaded(70), Does.Contain("\"level\":70"));
            Assert.That(DevProbe.MemLevelLoaded(70), Is.Null);
            Assert.That(DevProbe.MemLevelLoaded(71), Is.Null);
            Assert.That(DevProbe.MemLevelLoaded(80), Does.Contain("\"level\":80"));
            Assert.That(DevProbe.MemEveryLevels, Is.EqualTo(10));
        }

        /// <summary>
        /// El pool de la escena (<c>daily_pool_v1.json</c>) no se lee al arrancar ni para la tarjeta del inicio: se lee al abrir el
        /// Reto, con el mismo tablero del día que el pool leído entero, y se suelta al cerrarlo.
        /// </summary>
        [UnityTest]
        public IEnumerator ScenePool_NotReadAtBoot_ReadWhenTheDailyOpens_SameBoardOfTheDay()
        {
            var clock = new ManualClock(Utc(Monday), Bogota);
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(120)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.SetUpDaily(), Is.True);
            boot.DailyRunner!.enabled = false;
            DailyChallengeService service = boot.DailyService!;

            Assert.That(boot.DailyInBuild, Is.True);
            Assert.That(boot.RefreshDailyCard(), Is.EqualTo(DailyCardState.Play));
            Assert.That(service.PoolLoaded, Is.False, "ni el arranque ni la tarjeta leen el pool");
            Assert.That(service.PoolSource.Loads, Is.EqualTo(0));

            DailyPool eager = DailyPoolReader.Read(boot.DailyPoolAsset!.text);
            LevelDefinition expected = DailyCalendar.Today(clock, 120, service.Config, eager).Board(eager)!;

            Assert.That(boot.PressDailyCard(), Is.True);
            Assert.That(service.PoolLoaded, Is.True);
            Assert.That(service.PoolSource.Loads, Is.EqualTo(1));
            Assert.That(service.Resumable!.BoardId, Is.EqualTo(expected.Id), "el mismo tablero del día (D-031)");
            Assert.That(DevProbe.LastMemLine, Does.Contain("\"at\":\"daily\"").And.Contain("\"pool_read\":true,\"open_ms\":"));

            service.ReleasePool();
            Assert.That(service.PoolLoaded, Is.False);
            Assert.That(service.TodayBoard(120)!.Id, Is.EqualTo(expected.Id), "leído otra vez, el mismo");
        }

        /// <summary>
        /// Pool perezoso roto (D-008; decisión del integrador del 2026-10-26: se descubre al abrir el Reto, en release lo valida el
        /// CI): antes de tocar, la tarjeta se ve y el Reto está en el build; al tocarla no se abre, y tras un tick la tarjeta queda
        /// oculta, sin Reto en el build ni en Sin canoas (y, como la presentación del 7 depende de <c>DailyInBuild</c>, sin ella).
        /// </summary>
        [UnityTest]
        public IEnumerator BrokenScenePool_FoundWhenTheDailyOpens_HidesTheCard()
        {
            var clock = new ManualClock(Utc(Monday), Bogota);
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(120)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpDaily(), Is.True);
            boot.DailyRunner!.enabled = false;
            TextAsset? scenePool = boot.DailyPoolAsset;
            try
            {
                boot.ConfigureDailyPool(new TextAsset("{\"pack_schema\":2,\"kind\":\"daily\",\"levels\":[]"));
                Assert.That(boot.RefreshDailyCard(), Is.EqualTo(DailyCardState.Play));
                DailyChallengeCard card = boot.DailyCard!;
                Assert.That(card.Visible, Is.True, "antes de tocarla, el pool aún no se ha leído");
                Assert.That(boot.DailyInBuild, Is.True);

                LogAssert.Expect(LogType.Error, new Regex("pool del Reto del río no válido"));
                Assert.That(boot.PressDailyCard(), Is.False, "el pool no se puede leer: no se abre");
                boot.TickDaily(PlayModeHelpers.FrameMs);

                Assert.That(card.State, Is.EqualTo(DailyCardState.Hidden));
                Assert.That(card.Visible, Is.False, "D-008: oculta, no muda");
                Assert.That(boot.DailyInBuild, Is.False, "ni la presentación del 7 (GameScreen la pide con DailyInBuild)");
                Assert.That(boot.NoCanoesDailyAvailable, Is.False, "ni la línea de Sin canoas");
                Assert.That(boot.DailyScreen!.IsOpen, Is.False);
                Assert.That(boot.Lobby!.Visible, Is.True, "el inicio vuelve");
            }
            finally
            {
                boot.ConfigureDailyPool(scenePool);
            }
        }

        /// <summary>
        /// Cifras del Editor por momento (informe <c>docs/dev/memoria-2026-10-26.md</c>): una línea <c>AF_MEMTEST</c> por momento con
        /// la muestra tal cual y tras recoger la basura (lo vivo), y las líneas <c>AF_PERF mem</c> de la sonda. Manual: se lanza sola
        /// (<c>--filter AF.Tests.PlayMode.MemoryProbeTests.Measure_Moments</c>) en un Editor recién abierto, antes y después de cada
        /// cambio. Las cifras del Editor incluyen el propio Editor: sirven las diferencias entre momentos, no los absolutos. Con
        /// <paramref name="eagerPool"/>, el pool del Reto se lee en el inicio, como antes del 2026-10-26 (para comparar).
        /// </summary>
        [UnityTest]
        [Explicit("Medida manual de memoria en el Editor (informe de 2026-10-26).")]
        public IEnumerator Measure_Moments([Values(false, true)] bool eagerPool)
        {
            var clock = new ManualClock(Utc(Monday), Bogota);
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            Report("pre");
            long usedBefore = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            int gcBefore = GC.CollectionCount(0);
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(120)));
            GameBootstrap boot = GameBootstrap.Current!;
            Alloc("boot", usedBefore, gcBefore);
            var steps = new System.Text.StringBuilder("AF_MEMTEST {\"at\":\"boot_steps_used\",\"before_mib\":" + Mib(usedBefore).ToString("0.0", CultureInfo.InvariantCulture));
            for (int i = 0; i < GameBootstrap.BootStepCount; i++)
            {
                steps.Append(",\"s").Append(i).Append("\":").Append(boot.BootStepUsed(i) < 0 ? "-1" : Mib(boot.BootStepUsed(i)).ToString("0.0", CultureInfo.InvariantCulture));
            }

            Debug.Log(steps.Append('}').ToString());
            Debug.Log("AF_MEMTEST {\"at\":\"canvas\"" + DevProbe.FormatCanvasStats() + "}");
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            boot.SetUpDaily();
            if (eagerPool)
            {
                GC.Collect();
                long u = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
                int g = GC.CollectionCount(0);
                Assert.That(boot.DailyService!.Pool, Is.Not.Null, "como antes: el pool leído en el arranque");
                Alloc("pool_read", u, g);
                GC.Collect();
                Debug.Log("AF_MEMTEST {\"at\":\"pool_kept\",\"live_mib\":" + Mib(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - u).ToString("0.0", CultureInfo.InvariantCulture)
                    + "}");
            }

            Report(eagerPool ? "lobby_eager_pool" : "lobby");
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            Report("map");
            Assert.That(boot.CloseRiverMap(), Is.True);
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            boot.RefreshDailyCard();
            Assert.That(boot.PressDailyCard(), Is.True);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            Report("daily");
        }

        private static void Report(string at)
        {
            MemSample raw = DevProbe.SampleMemory();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            MemSample live = DevProbe.SampleMemory();
            CultureInfo inv = CultureInfo.InvariantCulture;
            Debug.Log("AF_MEMTEST {\"at\":\"" + at + "\",\"mono_used_mib\":" + Mib(raw.MonoUsed).ToString("0.0", inv)
                + ",\"mono_live_mib\":" + Mib(live.MonoUsed).ToString("0.0", inv)
                + ",\"mono_heap_mib\":" + Mib(live.MonoHeap).ToString("0.0", inv)
                + ",\"unity_alloc_mib\":" + Mib(live.UnityAllocated).ToString("0.0", inv)
                + ",\"gfx_mib\":" + Mib(live.Gfx).ToString("0.0", inv)
                + ",\"tex_mib\":" + Mib(live.Textures).ToString("0.0", inv)
                + ",\"tex_count\":" + live.TextureCount.ToString(inv)
                + ",\"alloc_thread_mib\":" + Mib(GC.GetAllocatedBytesForCurrentThread()).ToString("0.0", inv) + "}");
        }

        // Lo asignado desde usedBefore (el heap usado crece hasta la siguiente recogida; si hubo una, la cifra es un mínimo).
        private static void Alloc(string at, long usedBefore, int gcBefore)
        {
            long now = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            int gcs = GC.CollectionCount(0) - gcBefore;
            Debug.Log("AF_MEMTEST {\"at\":\"alloc_" + at + "\",\"alloc_mib\":" + Mib(now - usedBefore).ToString("0.0", CultureInfo.InvariantCulture)
                + ",\"gcs\":" + gcs.ToString(CultureInfo.InvariantCulture) + "}");
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
