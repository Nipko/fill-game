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
    /// Capturas de revisión de la Tinaja y su cuia (tarea TJ2; <c>[Explicit]</c>, no forman parte de la suite), con el tablero
    /// del 323 (<see cref="TinajaVisualTests.Pack323"/>) y la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp):
    /// <c>tj2_01_l323_inicio</c> (tras la intro: la vasija con la boca de jenipapo, «×3», su cuia colgada del poste y el jirau
    /// bajo las dos ranuras, sin el vidrio debajo y sin nada entre las dos piezas), <c>tj2_02_l323_seleccionada</c> (la tinaja
    /// elevada como origen, 8 dp con su balanceo, sin cambiar: D-002),
    /// <c>tj2_03_l323_sacar</c> (a mitad de sacar: desde TJ3, su cuia en la boca de la tinaja), <c>tj2_04_l323_sacada</c> (tras
    /// sacar: la boca de lulo, «×2» y su cuia con el jenipapo), <c>tj2_05_l323_marea_banda</c> (Marea: la boca de jenipapo y la
    /// banda de luz del lulo, revelado, a su altura), <c>tj2_06_l323_bandas</c> (todo revelado, como tras la Luciérnaga: dos
    /// bandas), <c>tj2_07_l323_vacia</c> (la suelta: la tinaja vacía boca abajo sobre el jirau y su cuia suelta),
    /// <c>tj2_08_l323_sin_arte</c> (sin el arte por código, forzado; desde D-052 sale en todos los builds: el vidrio con la cima y el lulo revelado en su
    /// líquido, las ocultas del barro y nunca turbias, y solo el chip en el hombro del vidrio: sin boca, sin bandas ni iconos
    /// dobles) y <c>tj2_09_l323_pareja_corte</c> (la pareja en la 4 y la 5, donde el reparto 5/5 de D-006 las partiría: van
    /// juntas, 6/4, con la fila apretada para que la vasija y el jirau no lleguen al marco ni salgan de la repisa). Sirven para mirar a 47-48 dp la vasija (más alta que ancha, lisa, sin «?» ni
    /// pardo), la boca y su icono, el chip, las bandas, el jirau y el asa sin nudo entre las dos piezas. Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.TinajaSnapshotTests</c> sin <c>-nographics</c>;
    /// los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class TinajaSnapshotTests
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
        public IEnumerator CaptureStates()
        {
            string dir = SnapshotKit.Prepare();
            yield return Load(Cucharon, Tinaja);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            Save(dir, "tj2_01_l323_inicio", boot);

            Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 240);
            Save(dir, "tj2_02_l323_seleccionada", boot);

            Assert.That(PourRules.IsLegal(flow.Session.Current, Tinaja, Cucharon), Is.True);
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, 420);
            Save(dir, "tj2_03_l323_sacar", boot);

            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(flow.Session.Current[Tinaja].Count, Is.EqualTo(2));
            Save(dir, "tj2_04_l323_sacada", boot);

            Assert.That(flow.Undo(), Is.True, "Marea");
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(flow.Session.Current[Tinaja].Count, Is.EqualTo(3));
            Assert.That(flow.Session.Current[Tinaja].IsHiddenAt(1), Is.False, "lo revelado sigue revelado (R7, I5)");
            Save(dir, "tj2_05_l323_marea_banda", boot);

            // Solo para la captura: los estados que piden la Luciérnaga y la suelta, pintados en la vista.
            CuiaView tv = boot.View!.Views[Tinaja];
            CuiaView cv = boot.View.Views[Cucharon];
            Cuia current = flow.Session.Current[Tinaja];
            tv.ShowContent(current.WithRevealed(current.HiddenMask), false);
            PlayModeHelpers.Step(boot, 50);
            Save(dir, "tj2_06_l323_bandas", boot);

            tv.ShowContent(Cuia.Empty(LevelValidation.TinajaCapacity).WithMechanic(MechanicType.Tinaja), false);
            cv.ShowContent(Cuia.Create(LevelValidation.CucharonCapacity, new[] { 11, 9 }), false);
            PlayModeHelpers.Step(boot, 50);
            Save(dir, "tj2_07_l323_vacia", boot);

            MapArtRule.ForceProvisional = false; // forzado: hoy el arte por código sale en todos los builds, release incluido (D-052)
            tv.ShowContent(flow.Session.Current[Tinaja], false);
            cv.ShowContent(flow.Session.Current[Cucharon], false);
            PlayModeHelpers.Step(boot, 50);
            Save(dir, "tj2_08_l323_sin_arte", boot);
            MapArtRule.ForceProvisional = true;
        }

        [UnityTest]
        public IEnumerator CapturePairAcrossTheCut()
        {
            string dir = SnapshotKit.Prepare();
            yield return Load(4, 5);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardLayoutResult layout = boot.View!.Layout!;
            Assert.That(layout.RowOf[4], Is.EqualTo(layout.RowOf[5]));
            Save(dir, "tj2_09_l323_pareja_corte", boot);
            TestContext.WriteLine("AF_TJ2 tj2_09: filas " + layout.PerRow[0].ToString(CultureInfo.InvariantCulture) + "/" + layout.PerRow[1].ToString(CultureInfo.InvariantCulture));
        }

        // Carga el 323 con la pareja en (cucharon, tinaja), salta la intro y deja la vista quieta en Playing.
        private static IEnumerator Load(int cucharon, int tinaja)
        {
            yield return PlayModeHelpers.LoadGameWithPack(TinajaVisualTests.Pack323(cucharon, tinaja), 323, new MemoryProgressStore(323, 322));
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

        // Guarda la captura y anota el tamaño de la cuia, la boca y el chip.
        private static void Save(string dir, string name, GameBootstrap boot)
        {
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, boot);
            for (int i = 0; i < boot.View!.Views.Length; i++)
            {
                if (boot.View.Views[i].OverlayOf(MechanicType.Tinaja) is TinajaOverlay tin)
                {
                    float u = boot.View.Views[i].Unit;
                    TestContext.WriteLine("AF_TJ2 " + name + ": tinaja " + i.ToString(CultureInfo.InvariantCulture) + " de "
                        + (u * CuiaGeometry.WidthU).ToString("0.0", CultureInfo.InvariantCulture) + " dp; boca "
                        + (tin.MouthRectU.width * u).ToString("0.0", CultureInfo.InvariantCulture) + " dp ("
                        + tin.MouthTint.ToString(CultureInfo.InvariantCulture) + "); chip x" + tin.ChipCount.ToString(CultureInfo.InvariantCulture)
                        + "; bandas " + tin.BandMask.ToString(CultureInfo.InvariantCulture) + "; boca abajo " + tin.UpsideDown
                        + "; pareja " + tin.PartnerIndex.ToString(CultureInfo.InvariantCulture) + "; jirau " + tin.JirauVisible + " " + tin.JirauRect
                        + " (pie " + boot.View.Views[i].OverlayHost.Board!.Foot(i) + ")");
                }
            }

            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }
    }
}
