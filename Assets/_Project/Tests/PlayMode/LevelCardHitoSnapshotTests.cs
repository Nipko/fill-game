#nullable enable
using System.Collections;
using System.IO;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la tarjeta previa del hito p20 con «Si lo superas, llegas a {lugar}» y la silueta del sitio (tarea M2.5; 02 §10
    /// y 07 §7.7; D-013, punto 8), con el catálogo real y la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp):
    /// <c>card_hito_40</c> (el Encuentro de las Aguas, con su pieza por código) y <c>card_hito_100</c> (la tarjeta combinada, con
    /// Amacayacu); para la revisión, el 60 (Anavilhanas), el 160 (Allpahuayo-Mishana), el 100 al 130 % y en 16:9 al 130 %, y el
    /// 40 al 130 %. La silueta provisional solo sale en el Editor y en desarrollo (<c>MapArtRule</c>). Cada captura escribe su
    /// línea <c>AF_M25</c> con la sonda de la tarjeta. Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode
    /// --filter AF.Tests.PlayMode.LevelCardHitoSnapshotTests</c> sin <c>-nographics</c>); los PNG quedan en
    /// <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de la tarjeta del hito p20 (M2.5); necesitan dispositivo gráfico.")]
    public sealed class LevelCardHitoSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureHitoCards()
        {
            string dir = SnapshotKit.Prepare();
            yield return Capture(dir, 40, "card_hito_40");
            yield return Capture(dir, 100, "card_hito_100");
            yield return Capture(dir, 60, "card_hito_60");
            yield return Capture(dir, 160, "card_hito_160");
            yield return Capture(dir, 40, "card_hito_40_130", 1.3f);
            yield return Capture(dir, 100, "card_hito_100_130", 1.3f);

            // 16:9 (1080 × 1920 px; GameScreen lo maqueta en 438,75 × 780 dp).
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Capture(dir, 100, "card_hito_100_16x9_130", 1.3f);
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            Assert.That(File.Exists(Path.Combine(dir, "card_hito_40.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(dir, "card_hito_100.png")), Is.True);
        }

        private static IEnumerator Capture(string dir, int level, string name, float fontScale = 1f)
        {
            yield return CardSetup.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.LevelCard.Visible, Is.True, "nivel " + level);
            Assert.That(view.LevelCard.HitoText, Is.Not.Null, "la fila del hito en el " + level);
            if (fontScale != 1f)
            {
                view.CardFontScale = fontScale;
                PlayModeHelpers.Step(boot, 50);
            }

            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_M25 " + name + ": " + view.LastCardProbe);
        }
    }
}
