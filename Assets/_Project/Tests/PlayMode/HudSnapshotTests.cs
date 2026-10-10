#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Rules;
using AF.GameFlow;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión del HUD de etiquetas (tarea T09; 02 §5 y §10; 07 §7.4 y §7.7; propuesta §c.1 y §c.5) con el
    /// catálogo real y la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp):
    /// <c>44_hud_murky_l100</c> (Muy difícil y chip «?»), <c>45_hud_hard_l40</c> (chip y marco ámbar), <c>46_hud_veryhard_l60</c>
    /// (chip y marco índigo y el velo del 15 %), <c>47_next_chip_l39</c> («Difícil ×3» sobre Siguiente), <c>48_counter_lluvia_l143</c>
    /// (contador de la Lluvia tras un vertido útil) y <c>49_counter_pirana_l183</c> (contador de la Piraña), más las variantes de
    /// la banda: <c>45b_hud_tucked_l106</c> (3 filas), <c>46b_hud_tucked_l200</c> (3 filas con el contador),
    /// <c>46c_hud_l200_16x9</c>, <c>46d_hud_tucked_16_16x9</c> y <c>46g_hud_l160_16x9</c> (16:9, 1080 × 1920 px, arriba de la
    /// captura; en el 160 el contador va junto a la cabecera compactada),
    /// <c>46e_hud_veryhard_l160</c> y <c>46f_hud_veryhard_l180</c> (el marco índigo junto al Huito, C13),
    /// <c>47b_next_chip_l59</c> («Muy difícil ×4») y <c>47c_next_chip_l59_16x9</c> (16:9), y <c>48b_counter_lluvia_l194</c> y
    /// <c>48c_counter_lluvia_l194_lifted</c> (el contador fuera del cielo de la nube de la fila 1, en reposo y con la cuia
    /// elevada). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.HudSnapshotTests</c> sin <c>-nographics</c>; los PNG van a <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class HudSnapshotTests
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
        public IEnumerator CaptureHudLabels()
        {
            string dir = SnapshotKit.Prepare();

            yield return Shot(dir, 100, "44_hud_murky_l100");
            yield return Shot(dir, 40, "45_hud_hard_l40");
            yield return Shot(dir, 106, "45b_hud_tucked_l106");
            yield return Shot(dir, 60, "46_hud_veryhard_l60");
            yield return Shot(dir, 200, "46b_hud_tucked_l200");

            // C13 (abierta): el índigo del marco (#24305E) está a ΔE00 3,0 del Huito (#1F2F6B). El 160 y el 180 son Muy difícil
            // y llevan jenipapo: se mira si el marco junto a una cuia de Huito se confunde con el tinte.
            yield return Shot(dir, 160, "46e_hud_veryhard_l160");
            yield return Shot(dir, 180, "46f_hud_veryhard_l180");

            // 16:9 (1080 × 1920 px): GameScreen lo maqueta en 438,75 × 780 dp, en la parte de arriba de la captura.
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Shot(dir, 200, "46c_hud_l200_16x9");
            yield return Shot(dir, 160, "46g_hud_l160_16x9");
            LaunchOptions.LevelPackJsonOverride = HudSetup.SixteenCuiaVeryHardPack();
            yield return Shot(dir, 1, "46d_hud_tucked_16_16x9");
            LaunchOptions.LevelPackJsonOverride = null;
            GameScreen.SimulatedScreen = new Vector4(SnapshotKit.Width, SnapshotKit.Height, 96f, 48f);

            // Siguiente con el chip del nivel siguiente (ganar el 39 y el 59).
            yield return HudSetup.LoadPlaying(39);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1200);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "47_next_chip_l39");

            yield return HudSetup.LoadPlaying(59);
            boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1200);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "47b_next_chip_l59");

            // El mismo chip colgado de Siguiente en 16:9 (1080 × 1920 px: 438,75 × 780 dp, en la parte de arriba de la captura).
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return HudSetup.LoadPlaying(59);
            boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1200);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "47c_next_chip_l59_16x9");
            GameScreen.SimulatedScreen = new Vector4(SnapshotKit.Width, SnapshotKit.Height, 96f, 48f);

            // Lluvia del 194 en la fila 1 de un tablero de 3 filas: el contador, pegado bajo la placa, a la izquierda del cielo de
            // la nube; en reposo y con la cuia de la Lluvia seleccionada (elevada).
            yield return HudSetup.LoadPlaying(194);
            boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 100);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "48b_counter_lluvia_l194");
            int rain = boot.Flow!.Session.Counter.CuiaIndex;
            Assert.That(HudSetup.FillIfEmpty(boot, rain), Is.True, "la Lluvia del 194 empieza vacía: antes de elevarla recibe un vertido");
            boot.TapCuia(rain);
            PlayModeHelpers.Step(boot, 400);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "48c_counter_lluvia_l194_lifted");

            // Lluvia del 143 tras un vertido útil (3 → 2, dos gotas pendientes) y Piraña del 183.
            yield return HudSetup.LoadPlaying(143);
            boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            HudSetup.FindUsefulThenFutile(boot.Flow!, out Pour useful, out Pour _);
            Assert.That(HudSetup.Tap(boot, useful), Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 200);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "48_counter_lluvia_l143");

            yield return Shot(dir, 183, "49_counter_pirana_l183");
            Assert.That(File.Exists(Path.Combine(dir, "49_counter_pirana_l183.png")), Is.True);
        }

        private static IEnumerator Shot(string dir, int level, string name)
        {
            yield return HudSetup.LoadPlaying(level);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 100);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            GameScreen view = boot.View!;
            Debug.Log("AF_HUD " + name + ": forma " + view.HudLabelsMode + ", filas " + view.Layout!.Rows + ", TopRowY " + view.TopRowDp
                + ", panel " + view.Layout.PanelY + ", marco " + view.DifficultyFrameTopDp + (view.DifficultyFrameTucked ? " (recogido)" : string.Empty)
                + ", sello " + view.HudDifficultyChip.SealShown + ", contador junto a la cabecera " + view.HudCounterBesideWallet
                + ", cabecera compactada " + view.WalletSqueezed);
        }
    }
}
