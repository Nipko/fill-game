#nullable enable
using System.Collections;
using AF.Boosters;
using AF.GameFlow;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// «¡Perfecto!» en la cinta de victoria (tarea DC14; Magic Sort §10.2 #25, recomendada A; D-029), en la escena: sin la Cuia
    /// de rescate la cinta dice <c>victory_perfect</c>; con ella, <c>level_complete</c>; se decide en cada victoria (tras una con
    /// rescate, la siguiente sin él vuelve a ser perfecta) y sustituye al título sin cambiar el alto de la cinta. En el 31
    /// (tablero 6 del tutorial con <c>af_level_offset</c> = 25), como <see cref="FunnelPlayModeTests"/>.
    /// </summary>
    public sealed class VictoryPerfectTests
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
        }

        [UnityTest]
        public IEnumerator WithoutTheRescue_TheRibbonSaysPerfect()
        {
            yield return Load31();
            GameBootstrap boot = GameBootstrap.Current!;
            VictorySetup.WinToVictoryScreen(boot);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.IsPerfect, Is.True);
            Assert.That(UiText.VictoryPerfect, Is.Not.Empty.And.Not.EqualTo(UiText.LevelComplete));
            Assert.That(view.VictoryRibbonTitle.text, Is.EqualTo(UiText.VictoryPerfect));
            if (Loc.Locale == LocLocale.SourceLocale)
            {
                Assert.That(view.VictoryRibbonTitle.text, Is.EqualTo("¡Perfecto!"));
            }
        }

        [UnityTest]
        public IEnumerator WithTheRescue_TheUsualTitle_AndTheNextVictoryIsPerfectAgain()
        {
            yield return Load31();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Rescue(boot);
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.Flow!.IsPerfect, Is.False, "el intento ganado llevó la Cuia de rescate");
            Assert.That(view.VictoryRibbonTitle.text, Is.EqualTo(UiText.LevelComplete));
            PlayModeHelpers.Step(boot, 900); // «Siguiente» ya entró (fila 1,40)
            float ribbonH = view.VictoryRibbonRect.rect.height;

            // El 32 (tablero 7) sin rescate: la cinta vuelve a decir «¡Perfecto!», con el mismo alto (sustituye al título).
            view.NextButton.onClick.Invoke();
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(32));
            Assert.That(VictorySetup.InNewLevel(boot.Flow.Phase), Is.True, "en su INTRO, LEVEL_CARD o PLAYING: " + boot.Flow.Phase);
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.Flow.IsPerfect, Is.True);
            Assert.That(view.VictoryRibbonTitle.text, Is.EqualTo(UiText.VictoryPerfect));
            PlayModeHelpers.Step(boot, 900);
            Assert.That(view.VictoryRibbonRect.rect.height, Is.EqualTo(ribbonH).Within(0.01f), "sin línea nueva: el mismo alto de la cinta");
        }

        [UnityTest]
        public IEnumerator ThePerfectTitle_FitsTheRibbon([Values(LocLocale.SourceLocale, LocLocale.Pseudo)] string locale, [Values(100, 130)] int percent)
        {
            // Cabe a 22 sp (× la escala) o más y al 130 % en es-419 y en-XA, en el ancho de 640 dp de alto (360 dp de ancho).
            float scale = percent / 100f;
            SnapshotKit.Prepare(locale);
            try
            {
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
                LaunchOptions.FontScaleOverride = scale;
                yield return Load31();
                GameBootstrap boot = GameBootstrap.Current!;
                VictorySetup.WinToVictoryScreen(boot);
                PlayModeHelpers.Step(boot, 900);
                GameScreen view = boot.View!;
                Assert.That(view.VictoryRibbonTitle.text, Is.EqualTo(UiText.VictoryPerfect));
                AssertRibbonFits(view, scale, locale + " " + percent + " %");
            }
            finally
            {
                SnapshotKit.Release();
            }
        }

        /// <summary>El título de la cinta no se sale de su caja ni baja de 22 sp (× la escala) (guía 5.5; tarea L4).</summary>
        internal static void AssertRibbonFits(GameScreen view, float scale, string what)
        {
            TMPro.TextMeshProUGUI title = view.VictoryRibbonTitle;
            title.ForceMeshUpdate();
            float min = view.VictoryCompact >= GameScreen.VictoryCompactMargins ? GameScreen.RibbonTitleCompactMinSp : 22f; // D-032
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo((min * scale) - 0.01f), what + ": " + min + " sp (× escala) o más");
            Assert.That(new LocInventory(Loc.Locale, scale).ProblemsOf(title), Is.Empty, what + ": el título no se sale de su caja");
            Assert.That(title.textInfo.lineCount, Is.InRange(1, 2), what + ": en una o dos líneas");
        }

        /// <summary>El 31 (tablero 6 del tutorial) con 1.000 semillas y 5 canoas, en una segunda sesión.</summary>
        internal static IEnumerator Load31()
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1000));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
        }

        /// <summary>Aguas estancadas (3 → 4) y «Continuar: +1 cuia»: la partida sigue con la Cuia de rescate.</summary>
        internal static void Rescue(GameBootstrap boot)
        {
            FunnelSetup.GetStuck(boot);
            GameScreen view = boot.View!;
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Available));
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.Flow.Attempt.Rescues, Is.EqualTo(1));
            PlayModeHelpers.Step(boot, 700); // el re-layout de la cuia nueva (300 ms) y Capi
        }
    }
}
