#nullable enable
using System.Collections;
using System.Globalization;
using System.IO;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
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
    /// Capturas de revisión de la Hoja atada y de las tarjetas de estreno de las variantes del mundo 6 (tarea VA1a;
    /// <c>[Explicit]</c>, no forman parte de la suite), con el paquete <c>mundo_06_v1.json</c> y la pantalla simulada de
    /// <see cref="SnapshotKit"/> (360 × 780 dp): <c>62j_debut_liana_n_l243</c> (la tarjeta de la Liana de cuenta) y
    /// <c>62m_liana_n_l243_board</c> (el tablero del 243 tras la tarjeta, para comprobar que las cuentas del icono y las de la
    /// cuia son la misma pieza), <c>62k_debut_hoja_atada_l283</c> y <c>62l_debut_hoja_atada_l283_130</c> (la de la Hoja
    /// atada, con la fuente al 130 %);
    /// <c>54_hoja_atada_l283</c> (el tablero del 283 tras la intro: la Hoja atada de aguaje en la fila de abajo de un tablero de
    /// 3 filas), <c>54b_hoja_atada_selected_l283</c> (elevada como origen, sin cambiar: D-002) y <c>54c_hoja_atada_pour_l283</c>
    /// (vertiendo, inclinada); y, en un paquete de prueba con la Hoja y la Hoja atada juntas en la fila de arriba (b.3 no las
    /// junta en un nivel; aquí solo para compararlas), <c>54d_hoja_vs_atada</c>, <c>54e_hoja_atada_receive</c> (a mitad del
    /// vertido que la llena) y <c>54f_hoja_atada_sealed</c> (sellada, con el cordel y la llave, b.3, «conserva la cuerda tras
    /// sellar», y el medallón bajado bajo el canto de la tapa); y <c>54g_hoja_atada_14_last_column</c>, con 14 cuias (5/5/4)
    /// y la Hoja atada en la última columna de la fila 1
    /// (reflejada al hombro izquierdo, lejos del marco). Sirven para mirar a 47-56 dp que se distinga de la Hoja, que la muestra
    /// del medallón diga la fruta, que no tape la boca ni el líquido, los solapes con las vecinas, con el marco y con la fila de
    /// arriba y la línea D-003. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.HojaAtadaSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class HojaAtadaSnapshotTests
    {
        // Paquete de comparación: la cuia 0 trae el taperebá arriba (sella la Hoja atada), la 1 es la Hoja y la 2 la Hoja atada.
        private const int CompareSource = 0;
        private const int CompareHoja = 1;
        private const int CompareAtada = 2;

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
            yield return CaptureCard(dir, 243, "62j_debut_liana_n_l243", 1f);
            yield return CaptureCard(dir, 283, "62k_debut_hoja_atada_l283", 1f);
            yield return CaptureCard(dir, 283, "62l_debut_hoja_atada_l283_130", 1.3f);
            Assert.That(File.Exists(Path.Combine(dir, "62k_debut_hoja_atada_l283.png")), Is.True);

            // El 243 jugando, sin la tarjeta: la cuenta de la cuia junto a las del icono de 62j (revisión de VA1a).
            FunnelSetup.Hooks();
            LaunchOptions.LevelPackJsonOverride = HojaAtadaVisualTests.World6Pack();
            yield return Load(243);
            GameBootstrap boot = GameBootstrap.Current!;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            Assert.That(liana, Is.GreaterThanOrEqualTo(0));
            Save(dir, "62m_liana_n_l243_board", boot, liana);
        }

        [UnityTest]
        public IEnumerator CaptureLevel283()
        {
            string dir = SnapshotKit.Prepare();
            LaunchOptions.LevelPackJsonOverride = HojaAtadaVisualTests.World6Pack();
            yield return Load(283);
            GameBootstrap boot = GameBootstrap.Current!;
            int atada = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.HojaAtada);
            Save(dir, "54_hoja_atada_l283", boot, atada);

            Assert.That(boot.TapCuia(atada).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 240);
            Save(dir, "54b_hoja_atada_selected_l283", boot, atada);

            BoardState board = boot.Flow!.Session.Current;
            int target = -1;
            for (int j = 0; j < board.Count && target < 0; j++)
            {
                if (j != atada && PourRules.IsLegal(board, atada, j))
                {
                    target = j;
                }
            }

            Assert.That(target, Is.GreaterThanOrEqualTo(0));
            Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, 420);
            Save(dir, "54c_hoja_atada_pour_l283", boot, atada);
        }

        [UnityTest]
        public IEnumerator CaptureHojaVsAtada()
        {
            string dir = SnapshotKit.Prepare();
            LaunchOptions.LevelPackJsonOverride = ComparePack();
            yield return Load(283);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Session.Current[CompareHoja].Mechanic, Is.EqualTo(MechanicType.Hoja));
            Assert.That(boot.Flow.Session.Current[CompareAtada].Mechanic, Is.EqualTo(MechanicType.HojaAtada));
            Save(dir, "54d_hoja_vs_atada", boot, CompareAtada);

            Assert.That(boot.TapCuia(CompareSource).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(CompareAtada).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, 520);
            Save(dir, "54e_hoja_atada_receive", boot, CompareAtada);

            MechanicTestKit.StepToIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(boot.Flow.Session.Current.IsSealed(CompareAtada), Is.True, "la Hoja atada, sellada");
            Save(dir, "54f_hoja_atada_sealed", boot, CompareAtada);
        }

        [UnityTest]
        public IEnumerator CaptureFourteenLastColumn()
        {
            string dir = SnapshotKit.Prepare();
            LaunchOptions.LevelPackJsonOverride = HojaAtadaVisualTests.FourteenCuiaPack();
            yield return Load(283);
            GameBootstrap boot = GameBootstrap.Current!;
            int atada = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.HojaAtada);
            Assert.That(atada, Is.EqualTo(HojaAtadaVisualTests.FourteenAtada));
            Save(dir, "54g_hoja_atada_14_last_column", boot, atada);
            TestContext.WriteLine("AF_VA1A 54g: margen al marco " + HojaAtadaVisualTests.FrameMarginDp(boot, boot.View!.Views[atada]).ToString("0.0", CultureInfo.InvariantCulture) + " dp");
        }

        private static IEnumerator CaptureCard(string dir, int level, string name, float fontScale)
        {
            LaunchOptions.LevelPackJsonOverride = HojaAtadaVisualTests.World6Pack();
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 300);
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
            Debug.Log("AF_VA1A " + name + ": " + view.LastCardProbe);
        }

        // Carga el nivel, salta la intro y deja la vista quieta en Playing.
        private static IEnumerator Load(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            PlayModeHelpers.Step(boot, 300);
        }

        // Guarda la captura y anota dónde queda la llave de la Hoja atada (u y px de la captura, a 3 px por dp).
        private static void Save(string dir, string name, GameBootstrap boot, int atada)
        {
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, boot);
            CuiaView view = boot.View!.Views[atada];
            if (view.OverlayOf(MechanicType.HojaAtada) is HojaOverlay leaf)
            {
                float u = view.Unit;
                Rect key = leaf.TiedKeyRectU;
                TestContext.WriteLine("AF_VA1A " + name + ": cuia " + atada + " de " + u.ToString("0.0", CultureInfo.InvariantCulture)
                    + " dp/u; llave x " + key.xMin.ToString("0.00", CultureInfo.InvariantCulture) + ".." + key.xMax.ToString("0.00", CultureInfo.InvariantCulture)
                    + " u, medallón " + leaf.KeyTopAboveLipU.ToString("0.00", CultureInfo.InvariantCulture) + " u y fruta "
                    + leaf.FruitTopAboveLipU.ToString("0.00", CultureInfo.InvariantCulture) + " u sobre lo alto ("
                    + (leaf.FruitTopAboveLipU * u * 3f).ToString("0.0", CultureInfo.InvariantCulture) + " px), hoja a "
                    + leaf.TagAngleDeg.ToString("0", CultureInfo.InvariantCulture) + "°, lado " + leaf.TiedSide.ToString("0", CultureInfo.InvariantCulture) + " (se ve en " + leaf.TiedShownSide.ToString("0", CultureInfo.InvariantCulture) + ")"
                    + ", apartada " + leaf.TiedPourAway.ToString("0.00", CultureInfo.InvariantCulture) + ", placa "
                    + (ProceduralArt.HojaBadgeWidthU * HojaOverlay.TiedBadgeScale * u).ToString("0.0", CultureInfo.InvariantCulture) + " dp");
            }

            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }

        // Paquete de comparación (pack_schema 3) en el 283 del mundo 6: 7 cuias de 4 en 2 filas (4/3; D-006) con la Hoja de aguaje
        // (vacía) en la 1 y la Hoja atada de taperebá (con 3) en la 2, junto a ella en la fila de arriba; la 0 lleva el cuarto
        // taperebá arriba, así que el primer vertido 0 → 2 la sella. 4 dosis de taperebá, aguaje, bacaba y açaí.
        private static string ComparePack()
        {
            return "{ \"pack_schema\": 3, \"pack_id\": \"test_m06_v1_n0283\", \"kind\": \"campaign\", \"world\": 6, \"world_id\": \"w06_oriente\", "
                + "\"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"m06_v1_n0283\", \"level\": 283, \"world\": 6, \"lap\": 1, "
                + "\"water_base\": \"white\", \"palette\": [\"tapereba\", \"aguaje\", \"bacaba\", \"acai\"], \"units_per_color\": 4, "
                + "\"caps\": [4, 4, 4, 4, 4, 4, 4], "
                + "\"cuias\": [[4, 8, 7, 3], [], [3, 3, 3], [8, 7, 4, 4], [7, 8, 4], [8, 7], []], "
                + "\"attrs\": [null, { \"type\": \"hoja\", \"key\": 4 }, { \"type\": \"hoja_atada\", \"key\": 3 }, null, null, null, null], "
                + "\"murky\": false, \"hidden\": [], \"difficulty\": { \"par\": 1 }, \"relief\": [] } ] }";
        }
    }
}
