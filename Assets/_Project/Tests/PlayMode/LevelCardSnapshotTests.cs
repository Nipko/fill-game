#nullable enable
using System.Collections;
using System.IO;
using AF.GameFlow;
using AF.Presentation.Mechanics;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de la tarjeta previa y de los tutoriales (tarea T13 del paso 1; 02 §10, §12 y §13; §c.1; 07 §7.7),
    /// con el catálogo real y la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp): <c>60_card_hard_l40</c>,
    /// <c>61_card_veryhard_l60</c>, <c>62_debut_hoja_l43</c>, <c>63_debut_murky_l100</c> (la combinada) y
    /// <c>64_debut_pirana_l183</c> (con el mordisco animado al empezar; sus instantes, en <see cref="CapturePiranaBite"/>: 64c-64h);
    /// y, para la revisión, el resto de estrenos (62b-62i), el 106 con el chip «?» (61b), el Matapi
    /// en su ranura de agua (61c, 72) y tres iconos (61d, 192), la fuente al 130 % (60b, 63b y 64b), 16:9 (60c y 63c) y el
    /// instante de la salida (60d). Necesitan gráficos: <c>unity test &lt;repo&gt;
    /// --mode PlayMode --filter AF.Tests.PlayMode.LevelCardSnapshotTests</c> sin <c>-nographics</c>; los PNG van a
    /// <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class LevelCardSnapshotTests
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
        public IEnumerator CaptureCards()
        {
            string dir = SnapshotKit.Prepare();
            yield return Capture(dir, 40, "60_card_hard_l40");
            yield return Capture(dir, 60, "61_card_veryhard_l60");
            yield return Capture(dir, 106, "61b_card_murky_l106");
            yield return Capture(dir, 72, "61c_card_matapi_l72");
            yield return Capture(dir, 192, "61d_card_icons_l192");
            yield return Capture(dir, 43, "62_debut_hoja_l43");
            yield return Capture(dir, 14, "62b_debut_chica_l14");
            yield return Capture(dir, 27, "62c_debut_alta_l27");
            yield return Capture(dir, 63, "62d_debut_matapi_l63");
            yield return Capture(dir, 83, "62e_debut_liana_l83");
            yield return Capture(dir, 103, "62f_debut_remolino_l103");
            yield return Capture(dir, 123, "62g_debut_barro_l123");
            yield return Capture(dir, 143, "62h_debut_lluvia_l143");
            yield return Capture(dir, 163, "62i_debut_brote_l163");
            yield return Capture(dir, 100, "63_debut_murky_l100");
            yield return Capture(dir, 183, "64_debut_pirana_l183");

            // Fuente al 130 % (02 §12): los contenedores crecen y nada se recorta.
            yield return Capture(dir, 40, "60b_card_hard_l40_130", 1.3f);
            yield return Capture(dir, 100, "63b_debut_murky_l100_130", 1.3f);
            yield return Capture(dir, 183, "64b_debut_pirana_l183_130", 1.3f);

            // 16:9 (1080 × 1920 px; GameScreen lo maqueta en 438,75 × 780 dp).
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Capture(dir, 40, "60c_card_hard_l40_16x9");
            yield return Capture(dir, 100, "63c_debut_murky_l100_16x9_130", 1.3f);
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;

            // La salida: 70 ms después del toque, con la intro de verdad ya en marcha detrás.
            yield return CardSetup.Load(40);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            CardSetup.TapCard(boot.View!.LevelCard);
            PlayModeHelpers.Step(boot, 70);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "60d_card_dismiss_l40");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(File.Exists(Path.Combine(dir, "64_debut_pirana_l183.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator CapturePiranaBite()
        {
            // D-012, punto 1: el mordisco animado de la tarjeta del 183 en varios instantes (la cuia recibe la dosis, el cardumen
            // llega, muerde, se lleva el bocado y el final con la tarjeta abierta) y la versión quieta de Reducir movimiento.
            string dir = SnapshotKit.Prepare();
            yield return BiteAt(dir, PiranaBiteDemo.ReceiveStartMs + (PiranaBiteDemo.ReceiveMs * 0.55f), "64c_bite_l183_receive");
            float bite = PiranaOverlay.BiteStepMs * PiranaBiteDemo.BiteSpeed;
            yield return BiteAt(dir, PiranaBiteDemo.BiteStartMs + (bite * 0.30f), "64d_bite_l183_reach");
            yield return BiteAt(dir, PiranaBiteDemo.BiteStartMs + (bite * 0.55f), "64e_bite_l183_bite");
            yield return BiteAt(dir, PiranaBiteDemo.BiteStartMs + (bite * 0.78f), "64f_bite_l183_carry");
            yield return BiteAt(dir, PiranaBiteDemo.TotalMs + 600f, "64g_bite_l183_end");

            yield return CardSetup.Load(183);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Flow!.ReduceMotion = true;
            boot.Flow.Load(183);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(boot.View!.LevelCard.BiteDemo!.Still, Is.True);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "64h_bite_l183_still");
        }

        private static IEnumerator BiteAt(string dir, float ms, string name)
        {
            yield return CardSetup.Load(183);
            GameBootstrap boot = GameBootstrap.Current!;
            PiranaBiteDemo bite = boot.View!.LevelCard.BiteDemo!;
            PlayModeHelpers.TickExact(boot, Mathf.Max(0, Mathf.RoundToInt(ms - bite.ElapsedMs)));
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_A4 " + name + ": " + bite.ElapsedMs.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms");
        }

        private static IEnumerator Capture(string dir, int level, string name, float fontScale = 1f)
        {
            yield return CardSetup.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.LevelCard.Visible, Is.True, "nivel " + level);
            if (fontScale != 1f)
            {
                view.CardFontScale = fontScale;
                PlayModeHelpers.Step(boot, 50);
            }

            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_T13 " + name + ": " + view.LastCardProbe);
        }
    }
}
