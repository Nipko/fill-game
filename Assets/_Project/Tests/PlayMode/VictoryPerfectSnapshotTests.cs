#nullable enable
using System.Collections;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de las dos cintas de victoria de DC14 (Magic Sort §10.2 #25, recomendada A; D-029), en el 31 (tablero 6 del
    /// tutorial), a los 1,6 s de abrirse la pantalla de victoria:
    /// <list type="bullet">
    /// <item><c>Logs/snapshots/es-419/dc14_01_perfect_780_100</c> y <c>dc14_02_rescue_780_100</c>: sin y con la Cuia de rescate
    /// («¡Perfecto!» y «¡Nivel completado!»);</item>
    /// <item><c>Logs/snapshots/en-XA/dc14_03_perfect_640_130</c> y <c>dc14_04_rescue_640_130</c>: las mismas en en-XA, en 640 dp
    /// de alto y al 130 %;</item>
    /// <item><c>Logs/snapshots/es-419/dc14_05_perfect_640_130</c>: «¡Perfecto!» en es-419 en 640 dp de alto y al 130 %.</item>
    /// </list>
    /// Cada una comprueba que el título cabe en la cinta (<see cref="VictoryPerfectTests.AssertRibbonFits"/>). Explícito:
    /// necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.VictoryPerfectSnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de la cinta de victoria de DC14; necesitan dispositivo gráfico.")]
    public sealed class VictoryPerfectSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureTheTwoRibbons()
        {
            yield return Capture("dc14_01_perfect_780_100", LocLocale.SourceLocale, false, 1f, false);
            yield return Capture("dc14_02_rescue_780_100", LocLocale.SourceLocale, false, 1f, true);
            yield return Capture("dc14_03_perfect_640_130", LocLocale.Pseudo, true, 1.3f, false);
            yield return Capture("dc14_04_rescue_640_130", LocLocale.Pseudo, true, 1.3f, true);
            yield return Capture("dc14_05_perfect_640_130", LocLocale.SourceLocale, true, 1.3f, false);
        }

        // Carga el 31, rescata si toca, gana, deja entrar la victoria 1,6 s, comprueba el título y captura.
        private static IEnumerator Capture(string name, string locale, bool small, float scale, bool rescue)
        {
            FunnelSetup.Hooks();
            string dir = SnapshotKit.Prepare(locale);
            if (small)
            {
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            }

            LaunchOptions.FontScaleOverride = scale;
            yield return VictoryPerfectTests.Load31();
            GameBootstrap boot = GameBootstrap.Current!;
            if (rescue)
            {
                VictoryPerfectTests.Rescue(boot);
            }

            VictorySetup.WinToVictoryScreen(boot);
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 1600);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.IsPerfect, Is.EqualTo(!rescue), name);
            Assert.That(view.VictoryRibbonTitle.text, Is.EqualTo(rescue ? UiText.LevelComplete : UiText.VictoryPerfect), name);
            VictoryPerfectTests.AssertRibbonFits(view, scale, name);
            Debug.Log("AF_DC14 " + name + ": «" + view.VictoryRibbonTitle.text + "» a " + view.VictoryRibbonTitle.fontSize.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                + " dp en " + view.VictoryRibbonTitle.textInfo.lineCount + " líneas, cinta de " + view.VictoryRibbonRect.rect.height.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " dp");
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }
    }
}
