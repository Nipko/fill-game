#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AF.Economy;
using AF.Levels;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Los extras del modo de captura (D-066; spark-marketing §6 E1; <see cref="CaptureOptions"/>): solo cuentan en desarrollo (en
    /// release, <see cref="CaptureOptions.Inactive"/> aunque llegue el extra, como <c>af_fake_store</c>), cada uno enciende el modo,
    /// los valores fuera de rango se llevan al borde o se ignoran con su error, el paquete se decodifica del base64 o se lee de un
    /// archivo sin salir de la carpeta de la app, y el almacén de la toma lee el perfil y nunca lo escribe.
    /// </summary>
    public sealed class CaptureOptionsTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            CaptureLog.Reset();
        }

        [Test]
        public void Release_IgnoresTheExtras_EvenWithAnOverride()
        {
            LaunchOptions.CaptureOverride = new CaptureOptions { Active = true, HideHud = true, Autoplay = true };
            CaptureOptions release = CaptureOptions.Resolve(false);
            Assert.That(release.Active, Is.False, "en release no hay modo de captura");
            Assert.That(release, Is.Not.SameAs(LaunchOptions.CaptureOverride));
            Assert.That(CaptureOptions.Resolve(false), Is.Not.SameAs(release), "cada vez un objeto nuevo: nadie comparte uno que se pueda cambiar");
            Assert.That(release.Active || release.HideHud || release.Autoplay || release.HasBoard, Is.False);
            Assert.That(LaunchOptions.ResolveLeagueFake(false, true), Is.False, "en release, la Liga nunca es la falsa");
        }

        [Test]
        public void Development_UsesTheOverride_AndWithoutExtrasIsInactive()
        {
            var forced = new CaptureOptions { Active = true, HideHud = true };
            LaunchOptions.CaptureOverride = forced;
            Assert.That(CaptureOptions.Resolve(true), Is.SameAs(forced));
            LaunchOptions.CaptureOverride = null;
            Assert.That(CaptureOptions.Resolve(true).Active, Is.False, "fuera de Android no hay intent: sin extras");
            Assert.That(LaunchOptions.ResolveLeagueFake(true, true), Is.True, "una toma no puntúa en la Liga real");
        }

        [Test]
        public void Reset_ClearsTheOverride()
        {
            LaunchOptions.CaptureOverride = new CaptureOptions { Active = true };
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.CaptureOverride, Is.Null);
        }

        [Test]
        public void NoExtras_IsInactive()
        {
            CaptureOptions o = Read(new Dictionary<string, object>());
            Assert.That(o.Active, Is.False);
            Assert.That(o.HideHud || o.Autoplay || o.HasBoard || o.Screen != CaptureScreen.None, Is.False);
        }

        [TestCase(CaptureOptions.ExtraCapture)]
        [TestCase(CaptureOptions.ExtraNoHud)]
        [TestCase(CaptureOptions.ExtraAutoplay)]
        public void EachBoolExtra_TurnsTheModeOn(string extra)
        {
            Assert.That(Read(new Dictionary<string, object> { { extra, true } }).Active, Is.True);
        }

        [TestCase(CaptureOptions.ExtraPauseMs, 500)]
        [TestCase(CaptureOptions.ExtraJitterMs, 0)]
        [TestCase(CaptureOptions.ExtraSelectMs, 100)]
        [TestCase(CaptureOptions.ExtraStartMs, 0)]
        [TestCase(CaptureOptions.ExtraSeed, 7)]
        [TestCase(CaptureOptions.ExtraStopBefore, 0)]
        [TestCase(CaptureOptions.ExtraSlowMo, 100)]
        [TestCase(CaptureOptions.ExtraWorld, 2)]
        [TestCase(CaptureOptions.ExtraBgLevel, 58)]
        public void AnyIntExtra_TurnsTheModeOn(string extra, int value)
        {
            Assert.That(Read(new Dictionary<string, object> { { extra, value } }).Active, Is.True, "cualquier af_capture* enciende el modo");
        }

        [TestCase(CaptureOptions.ExtraBoard)]
        [TestCase(CaptureOptions.ExtraBoardPath)]
        [TestCase(CaptureOptions.ExtraSkin)]
        [TestCase(CaptureOptions.ExtraScreen)]
        public void AnyStringExtra_TurnsTheModeOn(string extra)
        {
            Assert.That(Read(new Dictionary<string, object> { { extra, "x" } }).Active, Is.True);
        }

        [Test]
        public void LegacyProgress_IsNeverMigratedDuringACapture()
        {
            Assert.That(GameBootstrap.MigratesLegacyProgress(false, false, false), Is.True, "el juego de siempre migra af_v0_level");
            Assert.That(GameBootstrap.MigratesLegacyProgress(false, false, true), Is.False, "una toma no: borraría la clave sin guardar");
            Assert.That(GameBootstrap.MigratesLegacyProgress(true, false, false), Is.False);
            Assert.That(GameBootstrap.MigratesLegacyProgress(false, true, false), Is.False);
        }

        [Test]
        public void Base64_TooLong_IsRejectedBeforeDecoding()
        {
            string huge = new string('A', (CaptureOptions.MaxBoardBytes / 3 * 4) + 100);
            Assert.That(CaptureOptions.DecodeBase64(huge, out string? error), Is.Null);
            Assert.That(error, Does.Contain("pasa de"));
        }

        [Test]
        public void BoardFile_WithInvalidUtf8_IsAnError()
        {
            string root = Path.Combine(Path.GetTempPath(), "af_capture_utf8_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                File.WriteAllBytes(Path.Combine(root, "bad.json"), new byte[] { 0x7B, 0xFF, 0xFE, 0x7D });
                Assert.That(CaptureOptions.ReadBoardFile(root, "bad.json", out string? error), Is.Null);
                Assert.That(error, Does.Contain("no se pudo leer"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void ReadsEveryExtra()
        {
            CaptureOptions o = Read(new Dictionary<string, object>
            {
                { CaptureOptions.ExtraNoHud, true },
                { CaptureOptions.ExtraAutoplay, true },
                { CaptureOptions.ExtraBoardPath, " af_capture/board.json " },
                { CaptureOptions.ExtraPauseMs, 900 },
                { CaptureOptions.ExtraJitterMs, 0 },
                { CaptureOptions.ExtraSelectMs, 300 },
                { CaptureOptions.ExtraStartMs, 1500 },
                { CaptureOptions.ExtraStopBefore, 1 },
                { CaptureOptions.ExtraSeed, 42 },
                { CaptureOptions.ExtraSlowMo, 25 },
                { CaptureOptions.ExtraWorld, 3 },
                { CaptureOptions.ExtraSkin, "cuia_vidrio" },
                { CaptureOptions.ExtraScreen, "map" },
            });
            Assert.That(o.Active && o.HideHud && o.Autoplay, Is.True);
            Assert.That(o.BoardPath, Is.EqualTo("af_capture/board.json"));
            Assert.That(o.BoardBase64, Is.Null);
            Assert.That(o.PauseMs, Is.EqualTo(900));
            Assert.That(o.JitterMs, Is.EqualTo(0));
            Assert.That(o.SelectMs, Is.EqualTo(300));
            Assert.That(o.StartMs, Is.EqualTo(1500));
            Assert.That(o.StopBefore, Is.EqualTo(1));
            Assert.That(o.Seed, Is.EqualTo(42));
            Assert.That(o.TimeScale, Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(o.World, Is.EqualTo(3));
            Assert.That(o.BackdropLevel, Is.EqualTo(WorldTable.FirstLevel(3)), "el fondo de la gran parada del mundo");
            Assert.That(o.Skin, Is.EqualTo("cuia_vidrio"));
            Assert.That(o.Screen, Is.EqualTo(CaptureScreen.Map));
            Assert.That(o.Problems, Is.Empty);
            StringAssert.Contains("\"screen\":\"map\"", o.Describe());
        }

        [Test]
        public void Defaults_WhenOnlyTheModeIsOn()
        {
            CaptureOptions o = Read(new Dictionary<string, object> { { CaptureOptions.ExtraCapture, true } });
            Assert.That(o.PauseMs, Is.EqualTo(CaptureOptions.DefaultPauseMs));
            Assert.That(o.JitterMs, Is.EqualTo(CaptureOptions.DefaultJitterMs));
            Assert.That(o.SelectMs, Is.EqualTo(CaptureOptions.DefaultSelectMs));
            Assert.That(o.StartMs, Is.EqualTo(CaptureOptions.DefaultStartMs));
            Assert.That(o.TimeScale, Is.EqualTo(1f));
            Assert.That(o.StopBefore, Is.EqualTo(0));
            Assert.That(o.BackdropLevel, Is.EqualTo(0));
            Assert.That(o.Screen, Is.EqualTo(CaptureScreen.None));
            Assert.That(o.HideHud || o.Autoplay || o.HasBoard, Is.False);
        }

        [Test]
        public void OutOfRange_IsClampedOrIgnored_WithItsProblem()
        {
            CaptureOptions o = Read(new Dictionary<string, object>
            {
                { CaptureOptions.ExtraSlowMo, 1 },
                { CaptureOptions.ExtraPauseMs, 999999 },
                { CaptureOptions.ExtraStopBefore, -3 },
                { CaptureOptions.ExtraWorld, 99 },
                { CaptureOptions.ExtraBgLevel, 5000 },
                { CaptureOptions.ExtraScreen, "tienda" },
            });
            Assert.That(o.Active, Is.True);
            Assert.That(o.TimeScale, Is.EqualTo(CaptureOptions.MinSlowMoPercent / 100f).Within(1e-6f));
            Assert.That(o.PauseMs, Is.EqualTo(CaptureOptions.MaxRhythmMs));
            Assert.That(o.StopBefore, Is.EqualTo(0));
            Assert.That(o.World, Is.EqualTo(0));
            Assert.That(o.BgLevel, Is.EqualTo(0));
            Assert.That(o.Screen, Is.EqualTo(CaptureScreen.None));
            Assert.That(o.Problems.Count, Is.EqualTo(6));
        }

        [Test]
        public void BgLevel_WinsOverWorld()
        {
            CaptureOptions o = Read(new Dictionary<string, object> { { CaptureOptions.ExtraWorld, 2 }, { CaptureOptions.ExtraBgLevel, 58 } });
            Assert.That(o.BackdropLevel, Is.EqualTo(58));
        }

        [TestCase("map", CaptureScreen.Map)]
        [TestCase("league", CaptureScreen.League)]
        [TestCase("daily", CaptureScreen.Daily)]
        [TestCase("logbook", CaptureScreen.Logbook)]
        [TestCase("market", CaptureScreen.Market)]
        public void Screens_RoundTrip(string id, CaptureScreen screen)
        {
            Assert.That(CaptureOptions.TryParseScreen(id, out CaptureScreen parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(screen));
            Assert.That(CaptureOptions.ScreenId(screen), Is.EqualTo(id));
        }

        [Test]
        public void Base64_StandardAndUrlSafe_WithOrWithoutPadding()
        {
            const string json = "{\"pack_id\":\"ñandú?>>\"}";
            string standard = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            string url = standard.TrimEnd('=').Replace('+', '-').Replace('/', '_');
            Assert.That(CaptureOptions.DecodeBase64(standard, out string? e1), Is.EqualTo(json));
            Assert.That(e1, Is.Null);
            Assert.That(CaptureOptions.DecodeBase64(url, out string? e2), Is.EqualTo(json));
            Assert.That(e2, Is.Null);
            Assert.That(CaptureOptions.DecodeBase64("esto no es base64!", out string? e3), Is.Null);
            Assert.That(e3, Does.Contain(CaptureOptions.ExtraBoard));
        }

        [Test]
        public void BoardFile_OnlyInsideTheAppFolder()
        {
            string root = Path.Combine(Path.GetTempPath(), "af_capture_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "af_capture"));
            try
            {
                File.WriteAllText(Path.Combine(root, "af_capture", "board.json"), "{}", Encoding.UTF8);
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "af_capture_outside.json"), "{}", Encoding.UTF8);
                Assert.That(CaptureOptions.ReadBoardFile(root, "af_capture/board.json", out string? ok), Is.EqualTo("{}"));
                Assert.That(ok, Is.Null);
                Assert.That(CaptureOptions.ReadBoardFile(root, "../af_capture_outside.json", out string? up), Is.Null, "no sale de la carpeta");
                Assert.That(up, Does.Contain("dentro de la carpeta"));
                Assert.That(CaptureOptions.ReadBoardFile(root, Path.Combine(Path.GetTempPath(), "af_capture_outside.json"), out string? abs), Is.Null,
                    "una ruta absoluta no se lee");
                Assert.That(abs, Is.Not.Null);
                Assert.That(CaptureOptions.ReadBoardFile(root, "af_capture/missing.json", out string? missing), Is.Null);
                Assert.That(missing, Does.Contain("no existe"));
                Assert.That(CaptureOptions.ReadBoardFile(null, "x.json", out string? noRoot), Is.Null);
                Assert.That(noRoot, Is.Not.Null);
            }
            finally
            {
                Directory.Delete(root, true);
                File.Delete(Path.Combine(Path.GetTempPath(), "af_capture_outside.json"));
            }
        }

        [Test]
        public void ReadBoardJson_PrefersTheBase64()
        {
            var o = new CaptureOptions { Active = true, BoardBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"a\":1}")), BoardPath = "nope.json" };
            Assert.That(o.ReadBoardJson("C:/no-existe", out string? error), Is.EqualTo("{\"a\":1}"));
            Assert.That(error, Is.Null);
        }

        [Test]
        public void CaptureStore_ReadsTheProfile_AndNeverWritesIt()
        {
            Profile saved = Wallet.NewProfile(new ManualClock(new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc)));
            saved.Progress.MaxLevel = 42;
            var inner = new MemoryProfileStore(saved);
            string? before = inner.Json;
            var store = new CaptureProfileStore(inner);
            ProfileLoadResult loaded = store.Load();
            Assert.That(loaded.Profile!.Progress.MaxLevel, Is.EqualTo(42));
            loaded.Profile.Progress.MaxLevel = 99;
            store.Save(loaded.Profile);
            store.Delete();
            Assert.That(inner.SaveCount, Is.EqualTo(0));
            Assert.That(inner.Json, Is.EqualTo(before), "el perfil guardado sigue igual");
            Assert.That(store.DiscardedSaves, Is.EqualTo(1));
        }

        [Test]
        public void CaptureLog_LinesAreJsonWithEventAndTime()
        {
            string line = CaptureLog.Write("pour", "\"i\":1");
            StringAssert.StartsWith("AF_CAPTURE {\"event\":\"pour\",\"t\":", line);
            StringAssert.EndsWith(",\"i\":1}", line);
            string error = CaptureLog.Error("board", "comillas \" y\nsalto");
            StringAssert.Contains("\"what\":\"board\"", error);
            StringAssert.Contains("comillas \\\" y\\nsalto", error);
            Assert.That(Encoding.UTF8.GetByteCount(CaptureLog.Error("x", new string('á', 2000))), Is.LessThan(960), "cabe en el tope de la sonda");
        }

        private static CaptureOptions Read(Dictionary<string, object> extras)
        {
            return CaptureOptions.Read(
                name => extras.TryGetValue(name, out object? v) && v is bool b && b,
                (name, missing) => extras.TryGetValue(name, out object? v) && v is int i ? i : missing,
                name => extras.TryGetValue(name, out object? v) ? v as string : null);
        }
    }
}
