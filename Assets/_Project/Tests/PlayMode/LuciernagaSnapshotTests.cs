#nullable enable
using System.Collections;
using System.IO;
using AF.Ads;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de la Luciérnaga · Revelar (T14; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>65_reveal_tray_l100</c> (la bandeja de 3 ranuras de 64 dp con «Revelar +1»; recortes de la bandeja a tamaño real y
    /// de la ranura a 48 dp, con el glifo a 24 dp), <c>65b_reveal_tray_missing_l106</c> (sin saldo ni video: los precios en
    /// rojo y los dos «Te faltan {n}» sin pisarse; <c>65d</c>, con un precio de 4 cifras, el aviso en dos líneas),
    /// <c>65c_reveal_tray_video_l106</c> (sin saldo: «+1» con el icono de video),
    /// <c>66_reveal_select_l100</c> (el modo de selección: solo late la cuia turbia, con el aro abierto por abajo),
    /// <c>67a_reveal_firefly_flight_l100</c> (la luciérnaga a mitad del vuelo; <c>67a_reveal_tray_spent_l100_zoom</c>, la ranura
    /// ya gastada: gris y sin precio), <c>67b_reveal_firefly_light_l100</c> (posada en el borde, con su luz bajando por el
    /// vidrio), <c>67c_reveal_firefly_done_l100</c> (la cuia ya revelada y la luciérnaga que se va) y <c>68_gift_toast_l100</c>
    /// («+1 Luciérnaga» al empezar la intro; <c>68b</c>, al cerrar la tarjeta previa). De cada una, un recorte a tamaño real
    /// (<c>*_zoom</c>) para mirar solapes, legibilidad y la línea de D-003. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode
    /// PlayMode --filter AF.Tests.PlayMode.LuciernagaSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en
    /// <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class LuciernagaSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureRevealL100()
        {
            string dir = SnapshotKit.Prepare();
            yield return Load(100, FunnelSetup.Profile(0), true);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;

            // 65: la bandeja de un nivel turbio.
            SnapshotKit.Save(dir, "65_reveal_tray_l100");
            SaveCrop(dir, "65_reveal_tray_l100_zoom", (RectTransform)view.Tray.RevealButton.transform, 150f, 40f);

            // La ranura a 48 dp (el glifo a 24 dp; 07 §7.6: «legible a 24 dp»).
            var reveal = (RectTransform)view.Tray.RevealButton.transform;
            reveal.localScale = new Vector3(0.75f, 0.75f, 1f);
            SaveCrop(dir, "65_reveal_slot48_l100_zoom", reveal, 12f, 12f);
            reveal.localScale = Vector3.one;

            // 66: el modo de selección, en la cresta del latido.
            Assert.That(boot.PressReveal(), Is.True);
            PlayModeHelpers.Step(boot, (int)RevealPulse.PeriodMs);
            SnapshotKit.Save(dir, "66_reveal_select_l100");
            SaveCrop(dir, "66_reveal_select_l100_zoom", view.Views[0].Root, 40f, 40f);

            // 67: la luciérnaga sale del botón, se posa y su luz baja por el vidrio.
            boot.TapCuia(0);
            float flight = RevealFireflyFx.FlightMs * view.Animator.StepSpeedBase;
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(flight * 0.45f));
            SnapshotKit.Save(dir, "67a_reveal_firefly_flight_l100");
            SaveCrop(dir, "67a_reveal_firefly_flight_l100_zoom", view.RevealFirefly.Root, 45f, 45f);
            SaveCrop(dir, "67a_reveal_tray_spent_l100_zoom", (RectTransform)view.Tray.RevealButton.transform, 150f, 40f);
            while (!view.RevealMarksStarted)
            {
                PlayModeHelpers.TickExact(boot, 4);
            }

            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(600f * view.Animator.StepSpeedBase * 0.35f));
            SnapshotKit.Save(dir, "67b_reveal_firefly_light_l100");
            SaveCrop(dir, "67b_reveal_firefly_light_l100_zoom", view.Views[0].Root, 40f, 40f);
            while (view.Views[0].Murky.IsAnimating)
            {
                PlayModeHelpers.TickExact(boot, 4);
            }

            PlayModeHelpers.TickExact(boot, 60);
            SnapshotKit.Save(dir, "67c_reveal_firefly_done_l100");
            SaveCrop(dir, "67c_reveal_firefly_done_l100_zoom", view.Views[0].Root, 40f, 40f);
            for (int t = 0; t < 2000 && view.RevealAnimating; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            SnapshotKit.Save(dir, "67d_reveal_after_l100");
            Assert.That(File.Exists(Path.Combine(dir, "67b_reveal_firefly_light_l100.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureTrayL106()
        {
            string dir = SnapshotKit.Prepare();

            // Sin saldo ni video: Cuia extra y Revelar con el precio en rojo y su «Te faltan {n}».
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            yield return Load(106, FunnelSetup.Profile(50), true);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 300);
            SnapshotKit.Save(dir, "65b_reveal_tray_missing_l106");
            SaveCrop(dir, "65b_reveal_tray_missing_l106_zoom", (RectTransform)boot.View!.Tray.RevealButton.transform, 150f, 40f);

            // Con un precio de 4 cifras (fuera de los rangos de 08 §9; la API de la bandeja): «Te faltan 1.450» en dos líneas,
            // sobre su ranura y sin pisar el aviso de la vecina (limpieza L2b).
            BoosterTray tray = boot.View!.Tray;
            tray.SetRevealBadge(0, 1500, 50, false, true, false);
            SnapshotKit.Save(dir, "65d_reveal_tray_missing_4digits_l106");
            SaveCrop(dir, "65d_reveal_tray_missing_4digits_l106_zoom", (RectTransform)tray.RevealButton.transform, 150f, 60f);

            // Sin saldo y con video: «+1» con el icono de video.
            LaunchOptions.AdFillOverride = null;
            yield return Load(106, FunnelSetup.Profile(0), true);
            boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 300);
            SnapshotKit.Save(dir, "65c_reveal_tray_video_l106");
            SaveCrop(dir, "65c_reveal_tray_video_l106_zoom", (RectTransform)boot.View!.Tray.RevealButton.transform, 150f, 40f);
            Assert.That(boot.View.Tray.RevealShowsVideo, Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureGiftToastL100()
        {
            string dir = SnapshotKit.Prepare();

            // Sin tarjeta: el aviso sale con la intro.
            yield return Load(100, FunnelSetup.Profile(0), false);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 400);
            Assert.That(boot.View!.ToastText, Is.EqualTo(UiText.RevealGiftToast));
            SnapshotKit.Save(dir, "68_gift_toast_l100");

            // Con la tarjeta previa encendida: al cerrarla.
            LaunchOptions.LevelCardsOverride = true;
            yield return Load(100, FunnelSetup.Profile(0), false);
            boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard));
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 400);
            Assert.That(boot.View!.ToastText, Is.EqualTo(UiText.RevealGiftToast));
            SnapshotKit.Save(dir, "68b_gift_toast_after_card_l100");
        }

        // Carga el nivel con el perfil, une la cámara de las capturas y, con skipIntro, avanza hasta Playing con la vista quieta.
        private static IEnumerator Load(int level, Profile profile, bool skipIntro)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            if (!skipIntro)
            {
                yield break;
            }

            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 100);
        }

        // Recorte a tamaño real (3 px/dp) alrededor de target, con marginX y marginY dp de margen.
        private static void SaveCrop(string dir, string name, RectTransform target, float marginX, float marginY)
        {
            Camera cam = SnapshotKit.CaptureCamera!;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture rt = cam.targetTexture;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = cam.WorldToScreenPoint(corners[i]);
                xMin = Mathf.Min(xMin, p.x);
                yMin = Mathf.Min(yMin, p.y);
                xMax = Mathf.Max(xMax, p.x);
                yMax = Mathf.Max(yMax, p.y);
            }

            int x0 = Mathf.Clamp(Mathf.FloorToInt(xMin - (marginX * 3f)), 0, rt.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(yMin - (marginY * 3f)), 0, rt.height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(xMax + (marginX * 3f)), x0 + 1, rt.width);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(yMax + (marginY * 3f)), y0 + 1, rt.height);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(x1 - x0, y1 - y0, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x0, y0, x1 - x0, y1 - y0), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
