#nullable enable
using System.Collections;
using AF.Boosters;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Save;
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
    /// Con Cuia extra y después el rescate, ninguna cuia generada cambia de fila (prueba del 2026-10-09, T-C).
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
            Assert.That(layout.CuiaWidth, Is.EqualTo(40.66f).Within(0.05f), "con la franja de la tropa (D-046; antes 46,9)");
            Assert.That(layout.CuiaHeight, Is.EqualTo(113.83f).Within(0.1f), "antes 131,3");

            // Las vistas usan ese tamaño: u = ancho / 2 y alto de 5,60 u.
            for (int i = 0; i < view.Views.Length; i++)
            {
                Assert.That(view.Views[i].Unit * 2f, Is.EqualTo(layout.CuiaWidth).Within(0.01f), "cuia " + i);
                Assert.That(view.Views[i].HeightU * view.Views[i].Unit, Is.EqualTo(layout.CuiaHeight).Within(0.05f), "cuia " + i);
            }
        }

        [UnityTest]
        public IEnumerator Level49_TenCuias_ExtraThenRescue_NoGeneratedCuiaChangesRow()
        {
            // Prueba del 2026-10-09, T-C (D-006): 10 cuias generadas en 5/5; la Cuia extra va abajo (5/6) y la de rescate, a
            // la derecha de la fila de arriba (6/6). Antes, los índices se asignaban fila a fila y la 6.ª cuia generada
            // (índice 5, llena) saltaba a la fila de arriba al llegar a 6/6.
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1000));
            yield return PlayModeHelpers.LoadGameWithPack(TenCuiaPack49(), 49, new MemoryProgressStore(49, 48));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 3000);
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.Views.Length, Is.EqualTo(10));
            Assert.That(view.Layout!.PerRow, Is.EqualTo(new[] { 5, 5 }));
            int[] generatedRows = (int[])view.Layout.RowOf.Clone();
            Assert.That(generatedRows[5], Is.EqualTo(1), "la 6.ª cuia, en la fila de abajo");
            AssertViewsOnTheirRows(view, "al empezar");

            // Cuia extra (04 §2.2): va a la fila más corta; a igualdad, la de abajo [P].
            TransactionResult? extra = flow.UseExtraCuia();
            Assert.That(extra, Is.Not.Null, "Cuia extra disponible: " + flow.LastFunnelError);
            Assert.That(extra!.IsApplied, Is.True);
            Assert.That(view.Views.Length, Is.EqualTo(11));
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.Relayouting, Is.False);
            Assert.That(view.Layout!.PerRow, Is.EqualTo(new[] { 5, 6 }));
            Assert.That(view.Layout.RowOf[10], Is.EqualTo(1), "la Cuia extra, abajo");
            AssertGeneratedRowsKept(view, generatedRows, "tras la Cuia extra");
            AssertViewsOnTheirRows(view, "tras la Cuia extra");
            float footY5 = view.Views[5].FootPosition.y;

            // Aguas estancadas: 7 → 9 (camu camu al hueco) y 8 → 10 (bacaba a la Cuia extra); ninguna cima se repite.
            Pour(boot, 7, 9);
            Pour(boot, 8, 10);
            for (int elapsed = 0; elapsed < 5000 && flow.Phase != FlowPhase.StuckModal; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            PlayModeHelpers.Step(boot, 300); // el panel termina de subir (250 ms)
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Available));

            // Rescate (04 §6.2): la cuia nueva va a la derecha de la fila de arriba, la más corta (6/6).
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.Views.Length, Is.EqualTo(12));
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.Relayouting, Is.False);
            BoardLayoutResult l = view.Layout!;
            Assert.That(l.Rows, Is.EqualTo(2), "las filas no cambian a mitad de nivel (D-006)");
            Assert.That(l.PerRow, Is.EqualTo(new[] { 6, 6 }));
            Assert.That(l.RowOf[5], Is.EqualTo(1), "la 6.ª cuia sigue en la fila de abajo");
            Assert.That(l.RowOf[10], Is.EqualTo(1), "la Cuia extra no cambia de fila");
            Assert.That(l.RowOf[11], Is.EqualTo(0), "la de rescate, arriba");
            Assert.That(l.CenterX[11], Is.GreaterThan(l.CenterX[4]), "a la derecha de la fila de arriba");
            AssertGeneratedRowsKept(view, generatedRows, "tras el rescate");
            AssertViewsOnTheirRows(view, "tras el rescate");

            // Con 5/6 y con 6/6 la fila más larga tiene 6: misma ranura, mismo ancho y mismas filas, así que la cuia 5 no se
            // mueve en vertical con el rescate (con 5/5, antes de la Cuia extra, las cuias eran más anchas y altas).
            Assert.That(view.Views[5].FootPosition.y, Is.EqualTo(footY5).Within(0.01f), "la y de la cuia 5 no cambia con el rescate");
        }

        // Selecciona from, vierte en to (02 §3.1) y espera a que la cola visual termine.
        private static void Pour(GameBootstrap boot, int from, int to)
        {
            Assert.That(boot.TapCuia(from).Kind, Is.EqualTo(TapKind.Selected), "selecciona " + from);
            Assert.That(boot.TapCuia(to).Kind, Is.EqualTo(TapKind.Poured), from + " → " + to);
            PlayModeHelpers.StepUntilIdle(boot, 5000);
        }

        private static void AssertGeneratedRowsKept(GameScreen view, int[] generatedRows, string when)
        {
            for (int i = 0; i < generatedRows.Length; i++)
            {
                Assert.That(view.Layout!.RowOf[i], Is.EqualTo(generatedRows[i]), "fila de la cuia generada " + i + " " + when);
            }
        }

        // Cada vista, en el pie de su fila y en su centro del layout.
        private static void AssertViewsOnTheirRows(GameScreen view, string when)
        {
            BoardLayoutResult l = view.Layout!;
            for (int i = 0; i < view.Views.Length; i++)
            {
                Vector2 foot = view.Views[i].FootPosition;
                Assert.That(-foot.y, Is.EqualTo(l.RowFootY[l.RowOf[i]]).Within(0.01f), "pie de la cuia " + i + " " + when);
                Assert.That(foot.x, Is.EqualTo(l.CenterX[i]).Within(0.01f), "centro de la cuia " + i + " " + when);
            }
        }

        /// <summary>
        /// Nivel 49 (mundo 3, de 10 cuias como en la campaña) con un tablero hecho para el test: 8 tintes del mundo 3 con 4
        /// dosis cada uno, 7 cuias llenas sin sellar con la cima en guanábana, tapereba, aguaje o urucum, dos con 2 dosis
        /// (acaí bajo camu camu y agua del Tapajós bajo bacaba) y una vacía. Tras la Cuia extra, 7 → 9 y 8 → 10 dejan el
        /// tablero sin vertidos útiles (R15: ninguna cuia con hueco comparte cima con otra) y, con una cuia más, tiene
        /// solución, así que Aguas estancadas ofrece el rescate (04 §6.2).
        /// </summary>
        private static string TenCuiaPack49()
        {
            string[] ids = { "guanabana", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "agua_tapajos" };
            int[][] cuias =
            {
                new[] { 6, 7, 8, 1 },
                new[] { 7, 8, 10, 3 },
                new[] { 8, 10, 6, 4 },
                new[] { 10, 6, 7, 5 },
                new[] { 3, 4, 5, 1 },
                new[] { 4, 5, 1, 3 },
                new[] { 5, 1, 3, 4 },
                new[] { 8, 6 },
                new[] { 10, 7 },
                new int[0],
            };
            return PlayModeHelpers.BoardPack(3, "w03_manaos", "black", 49, ids, cuias);
        }
    }
}
