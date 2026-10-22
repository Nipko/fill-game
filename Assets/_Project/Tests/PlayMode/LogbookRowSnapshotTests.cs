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
    /// Capturas de la fila «Bitácora · elige tu premio» de la victoria (cierre de B11; D-033, añadido del 2026-10-20, puntos 5 y 10)
    /// en <c>Logs/snapshots/es-419/</c>, a 360 × 780 dp: <c>b11c_01_logbook_row_780_100</c> (la victoria del 26 con la Crecida, la
    /// Canasta y la fila) y <c>b11c_02_logbook_row_780_130</c> (al 130 %). En 360 × 640 dp la fila no cabe sin el escalón 3 y no
    /// sale (<c>LogbookVictoryRowTests</c>). Las cita la fila <c>logbook_victory_row</c> de <c>strings.csv</c>. Explícito: necesita
    /// gráficos.
    /// </summary>
    [Explicit("Capturas del cierre de B11; necesitan dispositivo gráfico.")]
    public sealed class LogbookRowSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            GameBootstrap.LogbookOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameBootstrap.LogbookOverride = null;
            GameBootstrap.ResetLogbookOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator VictoryWithTheLogbookRow([Values(100, 130)] int percent)
        {
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            yield return FactLineTests.Win(26, null, null, percent / 100f);
            GameBootstrap boot = GameBootstrap.Current!;
            if (percent == 100)
            {
                Assert.That(boot.View!.VictoryLogbookShown, Is.True);
            }

            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 1700);
            yield return SnapshotKit.Wait(0.2f);
            string name = percent == 100 ? "b11c_01_logbook_row_780_100" : "b11c_02_logbook_row_780_130";
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_B11C_SNAPSHOT " + name);
        }
    }
}
