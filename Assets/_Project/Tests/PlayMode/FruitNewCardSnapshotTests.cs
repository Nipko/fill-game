#nullable enable
using System.Collections;
using System.Globalization;
using System.IO;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la tarjeta «Fruta nueva» (tarea F5; 06 §4.5): <c>f5_02_fruta_nueva_l4</c> … <c>f5_11_fruta_nueva_l201</c>, en
    /// los estrenos de tinte de 1-300 sin tutorial (4 guanábana, 7 bacaba, 11 Agua del Tapajós, 21 camu camu, 41 achiote, 61
    /// Río Negro, 81 copoazú, 111 lulo, 141 huito y 201 cacao), por su camino de verdad (al habilitarse el toque, cuando la tropa se
    /// ha ido), a 360 × 780 dp (en el 111 y el 201, de 3 filas, como placa de una línea clavada al marco [P]); y
    /// <c>f5_12_fruta_nueva_l4_130</c> y <c>f5_13_fruta_nueva_l141_640_130</c>, con la escala de texto al 130 % (la frase en dos
    /// líneas, sobre el muelle si no cabe en la franja; la segunda, a 360 × 640 dp). Sirven para mirar la
    /// legibilidad (≥ 12 sp), la muestra con su patrón, que no tape cuias ni bandeja y la línea de D-003. Cada captura escribe en
    /// el log su línea <c>AF_FRUIT_NEW</c> con la sonda, el sitio y las medidas. Explícito: necesita gráficos (<c>unity test
    /// &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.FruitNewCardSnapshotTests</c> sin <c>-nographics</c>); los PNG
    /// quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de la tarjeta «Fruta nueva» (F5); necesitan dispositivo gráfico.")]
    public sealed class FruitNewCardSnapshotTests
    {
        // Sin el 2 (antes f5_01): lo ocupa la instrucción del tutorial con TiTino y sale una tarjeta por nivel como mucho (TU1a;
        // D-023, punto 39). Los nombres de las demás capturas no cambian.
        private static readonly int[] Levels = { 4, 7, 11, 21, 41, 61, 81, 111, 141, 201 };

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.FruitNewOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.FruitNewOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureFruitNewCards()
        {
            string dir = SnapshotKit.Prepare();
            for (int i = 0; i < Levels.Length; i++)
            {
                int level = Levels[i];
                yield return Capture(dir, level, "f5_" + (i + 2).ToString("00", CultureInfo.InvariantCulture) + "_fruta_nueva_l" + level, false, 1f);
            }

            yield return Capture(dir, 4, "f5_12_fruta_nueva_l4_130", false, 1.3f);
            yield return Capture(dir, 141, "f5_13_fruta_nueva_l141_640_130", true, 1.3f);
            Assert.That(File.Exists(Path.Combine(dir, "f5_02_fruta_nueva_l4.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(dir, "f5_13_fruta_nueva_l141_640_130.png")), Is.True);
        }

        private static IEnumerator Capture(string dir, int level, string name, bool short640, float scale)
        {
            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = short640 ? HudLabelTests.Screen640
                : HudLabelTests.Screen780;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            for (int t = 0; t < 15000 && boot.Flow!.Phase != FlowPhase.Playing; t += 100)
            {
                boot.TapAnywhere(); // cierra el banner de mundo, si lo hay, y salta la intro
                PlayModeHelpers.Step(boot, 100);
            }

            for (int t = 0; t < 8000 && !view.FruitNewView.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.FruitNewView.Visible, Is.True, name);
            PlayModeHelpers.Step(boot, 400); // la entrada de 200 ms y las etiquetas del HUD ya apartadas
            Assert.That(view.FruitNewView.Alpha, Is.EqualTo(1f), name);
            yield return SnapshotKit.Wait(0.2f);
            if (short640)
            {
                SnapshotKit.SaveTop(dir, name, (int)HudLabelTests.Screen640.y); // GameScreen maqueta la pantalla de 640 dp arriba
            }
            else
            {
                SnapshotKit.Save(dir, name);
            }
            FruitNewCard card = view.FruitNewView;
            CultureInfo inv = CultureInfo.InvariantCulture;
            Debug.Log("AF_FRUIT_NEW " + name + ": «" + card.Name + "» / «" + card.Tag + "», " + card.Height.ToString("0.0", inv) + " × "
                + card.Root.rect.width.ToString("0.0", inv) + " dp, " + view.FruitNewMode
                + ", nombre a " + card.Texts[0].fontSize.ToString("0.0", inv) + " dp y frase a " + card.Texts[1].fontSize.ToString("0.0", inv)
                + " dp (escala " + scale.ToString("0.0", inv) + "), " + view.LastFruitNewProbe);
        }
    }
}
