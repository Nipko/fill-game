#nullable enable
using System;
using System.Collections;
using System.IO;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de TiTino en la ventanita del inicio (tarea TI6c; propuesta de TiTino d.7; D-013, Q17-A), con el rig provisional
    /// (solo en el Editor y en desarrollo) y un reloj manual:
    /// <list type="bullet">
    /// <item><c>home_sloth_morning</c>: las 08:00 de la primera apertura del día, con <c>sloth_wave</c> (el rig bajado
    /// <see cref="HomeSloth.WaveDropDp"/> dp) y «¡Buenos días!…» (360 × 780 dp); <c>home_sloth_morning_640</c>, lo mismo a 360 ×
    /// 640, con la garza del decorado bajo el marco y el pico del bocadillo con su sombra;</item>
    /// <item><c>home_sloth_return</c>: el saludo de regreso tras 4 días fuera, a las 18:00 (360 × 780);</item>
    /// <item><c>home_sloth_sunbathe</c> (15:00), <c>home_sloth_idle</c> (17:00) y <c>home_sloth_sleep</c> (23:00), sin saludo
    /// (360 × 640);</item>
    /// <item><c>home_sloth_night_asleep</c>: las 23:00, dormido y callado con el «Zzz» (360 × 780); <c>home_sloth_night_tap</c>:
    /// lo mismo al tocarlo, despierto con su mensaje (añadido de D-050 del 2026-10-26);</item>
    /// <item><c>home_sloth_fact_130</c>: el toque con <c>sloth_look</c> y el Dato del perezoso más largo en su tarjeta con el
    /// retrato (nunca en el bocadillo), con el texto al 130 % (360 × 640).</item>
    /// </list>
    /// Cada una va con su recorte de la esquina de la ventanita (<c>…_crop</c>), la ventanita ampliada × 4 (<c>…_window</c>) y
    /// escribe su línea <c>AF_HOME_SLOTH</c>.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.HomeSlothSnapshotTests</c>
    /// sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de TiTino en la ventanita del inicio (TI6c); necesitan dispositivo gráfico.")]
    public sealed class HomeSlothSnapshotTests
    {
        private static readonly DateTime Day0 = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            HomeSloth.ManualTickForTests = true;
        }

        [TearDown]
        public void TearDown()
        {
            HomeSloth.ManualTickForTests = false;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureTiTinoInTheHomeWindow()
        {
            string dir = SnapshotKit.Prepare();

            // 08:00, primera apertura del día: sloth_wave a mitad del gesto y el saludo de la mañana.
            yield return Capture(dir, "home_sloth_morning", 0, 8, null, false, 1f, 400);
            yield return Capture(dir, "home_sloth_morning_640", 0, 8, null, false, 1f, 400, 1920);

            // 18:00 tras 4 días fuera: el saludo de regreso.
            yield return Capture(dir, "home_sloth_return", 0, 18, "2026-10-11", false, 1f, 400);

            // Sin saludo (ya se vio hoy): la pose de la hora, a 360 × 640.
            yield return Capture(dir, "home_sloth_sunbathe", 0, 15, "2026-10-15", false, 1f, 1500, 1920);
            yield return Capture(dir, "home_sloth_idle", 0, 17, "2026-10-15", false, 1f, 1500, 1920);
            yield return Capture(dir, "home_sloth_sleep", 0, 23, "2026-10-15", false, 1f, 1500, 1920);

            // De noche (añadido de D-050 del 2026-10-26): dormido y callado, con el «Zzz» (360 × 780); y al tocarlo, despierto
            // (sloth_wake, ya con los ojos abiertos) con el mensaje siguiente.
            yield return Capture(dir, "home_sloth_night_asleep", 0, 23, "2026-10-15", false, 1f, 1500);
            yield return Capture(dir, "home_sloth_night_tap", 0, 23, "2026-10-15", true, 1f, 1200);

            // El toque con el dato más largo de g.3, con el texto al 130 % y a 360 × 640.
            int longestOffset = 0;
            int longest = -1;
            for (int offset = 0; offset < 11; offset++)
            {
                SlothLines.TryDayNumber(ClockExtensions.FormatDateKey(Day0.AddDays(offset)), out int day);
                string text = HomeSloth.FactFor(day, out _);
                if (text.Length > longest)
                {
                    longest = text.Length;
                    longestOffset = offset;
                }
            }

            string seen = ClockExtensions.FormatDateKey(Day0.AddDays(longestOffset));
            yield return Capture(dir, "home_sloth_fact_130", longestOffset, 15, seen, true, 1.3f, 450, 1920);
        }

        private static IEnumerator Capture(string dir, string name, int dayOffset, int hour, string? lastSeen, bool tap, float textScale,
            int stepMs, int heightPx = SnapshotKit.Height)
        {
            LaunchOptions.FontScaleOverride = textScale;
            GameScreen.SimulatedScreen = new Vector4(SnapshotKit.Width, heightPx, 96f, 48f);
            var clock = new ManualClock(Day0.AddDays(dayOffset).AddHours(hour));
            LaunchOptions.ClockOverride = clock;
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.NextLevel = 58;
            profile.Progress.MaxLevel = 57;
            profile.Wallet.Seeds = 1250;
            if (lastSeen != null)
            {
                profile.Sloth.LastSeenDate = lastSeen;
                if (lastSeen == ClockExtensions.FormatDateKey(Day0.AddDays(dayOffset)))
                {
                    profile.Sloth.LastGreetingDate = lastSeen; // hoy ya saludó: solo la pose
                }
            }

            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            SnapshotKit.AttachLobby(boot);
            yield return SnapshotKit.Wait(0.3f);
            HomeSloth sloth = lobby.HomeWindow.Sloth!;
            sloth.Tick(16f);
            if (tap)
            {
                Assert.That(sloth.Tap(), Is.True, name);
            }

            for (int t = 0; t < stepMs; t += 16)
            {
                sloth.Tick(16f);
            }

            Assert.That(lobby.HomeWindow.Visible, Is.True, name);
            Assert.That(sloth.Rig, Is.Not.Null, name + ": el rig provisional");
            SnapshotKit.SaveTop(dir, name, heightPx);
            if (tap)
            {
                Assert.That(sloth.BubbleShown, Is.True, name + ": el toque trae un mensaje (D-050: el dato, en el bocadillo)");
            }

            SaveCrop(dir, name + "_crop");
            SaveWindowZoom(dir, name + "_window", lobby);
            Debug.Log("AF_HOME_SLOTH " + name + ": " + hour + ":00, pose " + SlothRig.IdOf(sloth.Pose) + ", saludo "
                + (sloth.LastGreeting.Key ?? (sloth.LastGreeting.Greeting ? "(solo gesto)" : "-")) + ", bocadillo «" + sloth.BubbleText
                + "» (" + sloth.Note + "), dormido " + sloth.Asleep + ", Zzz " + sloth.ZzzShown + ", texto al " + Mathf.RoundToInt(textScale * 100f) + " %");
        }

        // TiTino en su rama (su caja, con 4 dp de margen; D-050), ampliado × 2 sin suavizar.
        private static void SaveWindowZoom(string dir, string name, LobbyScreen lobby)
        {
            var full = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Assert.That(full.LoadImage(SnapshotKit.Capture()), Is.True, name);
            var root = (RectTransform)lobby.Canvas.transform;
            var corners = new Vector3[4];
            lobby.HomeWindow.Root.GetWorldCorners(corners);
            Rect bounds = root.rect;
            Vector3 topLeft = root.InverseTransformPoint(corners[1]);
            float pxPerDp = full.width / lobby.CanvasWidth;
            const float marginDp = 4f;
            int side = Mathf.RoundToInt((Mathf.Max(lobby.HomeWindow.Root.rect.width, lobby.HomeWindow.Root.rect.height) + (2f * marginDp)) * pxPerDp);
            int x0 = Mathf.Clamp(Mathf.RoundToInt((topLeft.x - bounds.xMin - marginDp) * pxPerDp), 0, full.width - side);
            int yTop = Mathf.Clamp(Mathf.RoundToInt((bounds.yMax - topLeft.y - marginDp) * pxPerDp), 0, full.height - side);
            Color[] src = full.GetPixels(x0, full.height - yTop - side, side, side);
            const int zoom = 2;
            var big = new Texture2D(side * zoom, side * zoom, TextureFormat.RGB24, false);
            var dst = new Color[side * zoom * side * zoom];
            for (int y = 0; y < side * zoom; y++)
            {
                for (int x = 0; x < side * zoom; x++)
                {
                    dst[(y * side * zoom) + x] = src[((y / zoom) * side) + (x / zoom)];
                }
            }

            big.SetPixels(dst);
            big.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), big.EncodeToPNG());
            Object.Destroy(full);
            Object.Destroy(big);
        }

        // La esquina de arriba a la izquierda (la ventanita, su píldora y el bocadillo), a tamaño real: 600 × 760 px.
        private static void SaveCrop(string dir, string name)
        {
            var full = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Assert.That(full.LoadImage(SnapshotKit.Capture()), Is.True, name);
            const int w = 600;
            const int h = 760;
            var crop = new Texture2D(w, h, TextureFormat.RGB24, false);
            crop.SetPixels(full.GetPixels(0, full.height - h, w, h)); // las filas de la textura empiezan por abajo
            crop.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), crop.EncodeToPNG());
            Object.Destroy(full);
            Object.Destroy(crop);
        }
    }
}
