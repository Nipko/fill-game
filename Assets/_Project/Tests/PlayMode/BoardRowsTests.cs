#nullable enable
using System.Collections;
using AF.Core.Session;
using AF.GameFlow;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Filas del tablero en la escena real (D-006, punto 4; 02 §5): con 13 cuias en el teléfono de pruebas (1080 × 2340 px,
    /// 360 × 780 dp, zona segura de <c>VisualSnapshotTests</c>) el tablero va en 3 filas de 5/4/4, como Magic Sort en el
    /// nivel 388 (ficha §2.1), con cuias de 46,9 × 131,3 dp (Magic Sort con 13-14 botellas: 45 × 138 dp en nuestra escala).
    /// </summary>
    public sealed class BoardRowsTests
    {
        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator ThirteenCuias_ThreeRowsOf5_4_4_At360x780()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGameWithPack(PlayModeHelpers.ThirteenCuiaPack(), 1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);

            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Session.Current.Count, Is.EqualTo(13));
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Playing), "ni resuelto ni en Aguas estancadas al empezar");
            GameScreen view = boot.View!;
            BoardLayoutResult layout = view.Layout!;
            Assert.That(layout.Rows, Is.EqualTo(3));
            Assert.That(layout.PerRow, Is.EqualTo(new[] { 5, 4, 4 }));
            Assert.That(layout.CuiaWidth, Is.EqualTo(46.9f).Within(0.05f));
            Assert.That(layout.CuiaHeight, Is.EqualTo(131.3f).Within(0.1f));

            // Las vistas usan ese tamaño: u = ancho / 2 y alto de 5,60 u.
            for (int i = 0; i < view.Views.Length; i++)
            {
                Assert.That(view.Views[i].Unit * 2f, Is.EqualTo(layout.CuiaWidth).Within(0.01f), "cuia " + i);
                Assert.That(view.Views[i].HeightU * view.Views[i].Unit, Is.EqualTo(layout.CuiaHeight).Within(0.05f), "cuia " + i);
            }
        }
    }
}
