#nullable enable
using System.Collections;
using System.Globalization;
using System.IO;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión del estreno de la Tinaja con sus comodines (tarea TJ4; <c>[Explicit]</c>, no forman parte de la
    /// suite), a 360 × 780 dp con el arte provisional:
    /// <c>tj4_01_card_l323</c> (la tarjeta del estreno: el medallón con la vasija, la línea 1 del tutorial y la 2, la de su cuia,
    /// en su renglón; sin chip «?»), <c>tj4_02_reveal_select_l323</c> (la bandeja con Revelar y el modo de selección: solo late
    /// la tinaja), <c>tj4_03_reveal_glow_l323</c> (la luciérnaga posada en la boca y la barriga encendida con la primera banda),
    /// <c>tj4_04_reveal_done_l323</c> (las dos capas reveladas como bandas, la ranura ya sin ocultas),
    /// <c>tj4_05_morpho_pour_l323</c> (Morpho en el S0 de b.2: la mariposa y los anillos),
    /// <c>tj4_06_tip_l324</c> (el consejo del 324 en el aviso) y <c>tj4_07_morpho_no_safe_l323</c> (la tarjeta «No veo un vertido
    /// seguro: la luciérnaga ilumina la tinaja», con Deshacer y Volver a la orilla), cada una con su zoom donde hace falta.
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.TinajaDebutSnapshotTests</c>
    /// sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class TinajaDebutSnapshotTests
    {
        private const int Cucharon = TinajaVisualTests.CucharonIndex;
        private const int Tinaja = TinajaVisualTests.TinajaIndex;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureDebutCard()
        {
            string dir = SnapshotKit.Prepare();
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGameWithPack(TinajaBoostersTests.DebutPack(), 323, new MemoryProgressStore(323, 322));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard));
            PlayModeHelpers.Step(boot, 800);
            Save(dir, "tj4_01_card_l323", boot);
            LevelCardView card = boot.View!.LevelCard;
            TestContext.WriteLine("AF_TJ4 tj4_01: tutorial " + card.TutorialLines.ToString(CultureInfo.InvariantCulture) + " líneas a "
                + card.TutorialBaseSp.ToString("0", CultureInfo.InvariantCulture) + " sp; línea 2 «" + card.ScoopText + "»");
        }

        [UnityTest]
        public IEnumerator CaptureReveal()
        {
            string dir = SnapshotKit.Prepare();
            Profile profile = FunnelSetup.Profile(500);
            profile.Stock.Reveal = 1;
            yield return Load(TinajaVisualTests.Pack323(Cucharon, Tinaja), 323, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            TinajaOverlay jar = TinajaBoostersTests.JarOf(view.Views[Tinaja]);

            Assert.That(boot.PressReveal(), Is.True);
            PlayModeHelpers.Step(boot, (int)RevealPulse.PeriodMs);
            Save(dir, "tj4_02_reveal_select_l323", boot);
            SaveCrop(dir, "tj4_02_reveal_tray_l323_zoom", (RectTransform)view.Tray.RevealButton.transform, 150f, 40f);

            boot.TapCuia(Tinaja);
            while (!view.RevealMarksStarted)
            {
                PlayModeHelpers.TickExact(boot, 4);
            }

            // A mitad: la luz llena la barriga y la banda de arriba ya se ve; la de abajo, aún no del todo.
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(TinajaOverlay.FireflyMs * view.Animator.StepSpeedBase * 0.4f));
            Assert.That(jar.FireflyAnimating, Is.True);
            Save(dir, "tj4_03_reveal_glow_l323", boot);
            SaveCrop(dir, "tj4_03_reveal_glow_l323_zoom", view.Views[Tinaja].Root, 50f, 30f);

            TinajaBoostersTests.StepUntilRevealDone(boot);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(jar.BandMask, Is.EqualTo(0b11));
            Save(dir, "tj4_04_reveal_done_l323", boot);
            SaveCrop(dir, "tj4_04_reveal_done_l323_zoom", view.Views[Tinaja].Root, 50f, 30f);
        }

        [UnityTest]
        public IEnumerator CaptureMorpho()
        {
            string dir = SnapshotKit.Prepare();
            Profile profile = FunnelSetup.Profile(500);
            profile.Stock.Hint = 3;
            yield return Load(TinajaBoostersTests.S0Pack(), 323, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            yield return TinajaBoostersTests.PressAndWait(boot);
            Assert.That(boot.Flow!.LastHint!.Value.Answer, Is.EqualTo(HintAnswer.Pour));
            PlayModeHelpers.TickExact(boot, (int)(MorphoFx.FlightMs + MorphoFx.PerchMs + MorphoFx.HopMs) + 40);
            Save(dir, "tj4_05_morpho_pour_l323", boot);

            var morpho = new MorphoBooster(BoosterConfig.Defaults, new ManualClock(FunnelSetup.T0));
            BoardState board = new BoardState(new[]
            {
                Cuia.Create(3, new[] { 3, 3, 1 }, 0x2), Cuia.Create(3, new[] { 2, 1 }, 0x1), Cuia.Create(3, new[] { 3, 2, 2 }), Cuia.Create(3, new[] { 1 }),
                Cuia.Empty(LevelValidation.TinajaCapacity).WithMechanic(MechanicType.Tinaja),
            });
            HintResult noSafe = morpho.Compute(board, null, PourMode.Run, true);
            Assert.That(noSafe.Answer, Is.EqualTo(HintAnswer.NoSafePour));
            view.OnHint(noSafe);
            PlayModeHelpers.Step(boot, 400);
            Save(dir, "tj4_07_morpho_no_safe_l323", boot);
        }

        [UnityTest]
        public IEnumerator CaptureTip()
        {
            string dir = SnapshotKit.Prepare();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            int level = TinajaTutorial.TipLevel;
            yield return PlayModeHelpers.LoadGameWithPack(TinajaBoostersTests.PackAt(TinajaVisualTests.Pack323(Cucharon, Tinaja), level), level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 600);
            Assert.That(boot.View!.ToastText, Is.EqualTo(UiText.TutorialTinajaTip));
            Save(dir, "tj4_06_tip_l324", boot);
        }

        private static IEnumerator Load(string pack, int level, Profile profile)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGameWithPack(pack, level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 6000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            PlayModeHelpers.Step(boot, 300);
        }

        private static void Save(string dir, string name, GameBootstrap boot)
        {
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, boot);
            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }

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
