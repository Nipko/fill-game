#nullable enable
using System.Collections;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Aguas estancadas por bucle en la partida (02 R13 ampliada; D-014, decisión del dueño del 2026-10-13; T51 en el núcleo):
    /// el nivel 66 real de la campaña (<c>m03_v1_n0066</c>) con la secuencia que el dueño jugó en el teléfono (hoja
    /// <c>docs/dev/traspaso/pruebas-telefono-2026-10-13.md</c>, «Defecto encontrado por el dueño»), con toques de verdad. La
    /// partida entra en Aguas estancadas justo tras el 9.º vertido (1 → 5), como fija
    /// <c>DeadEndProbeTests.R13b_Level66_TheOwnersSequence_IsStuckRightAfterTheNinthPour</c>, aunque 2 → 1 y 0 → 7 (los vertidos
    /// 10.º y 11.º del dueño) sigan siendo útiles para R13 de reglas; los vertidos que siguen se ignoran (R15) y el modal es el
    /// embudo de D-008 para un nivel ≥ 31: Marea primero (04 §6.2, regla 0), «Continuar: +1 cuia» y «Rendirse».
    /// </summary>
    public sealed class StuckLoopTests
    {
        /// <summary>Primer intento del dueño en el 66 (logcat del 2026-10-13): 3>6 4>3 7>4 0>5 1>5 2>0 2>1 5>2 1>5 2>1 0>7 1>2.</summary>
        private static readonly int[][] OwnersSequence =
        {
            new[] { 3, 6 }, new[] { 4, 3 }, new[] { 7, 4 }, new[] { 0, 5 }, new[] { 1, 5 }, new[] { 2, 0 },
            new[] { 2, 1 }, new[] { 5, 2 }, new[] { 1, 5 }, new[] { 2, 1 }, new[] { 0, 7 }, new[] { 1, 2 },
        };

        /// <summary>El vertido tras el que el 66 queda perdido de verdad (1 → 5, el 9.º; índice 8).</summary>
        private const int LastPlayable = 8;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level66_TheOwnersSequence_AguasEstancadasRightAfterTheNinthPour_TheRestIsIgnored_TheFunnelOfD008()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1000));
            yield return PlayModeHelpers.LoadGame(66, new MemoryProgressStore(66, 65));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(flow.LevelNumber, Is.EqualTo(66));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));

            // El 66 del pack (mundo_03_v1.json): 10 cuias de 4 salvo la 5 (Cuia chica de 2) y una Hoja de fruta 3 en la 6, en
            // el modo de vertido por defecto (D-005).
            BoardState start = flow.Session.Current;
            Assert.That(start.Count, Is.EqualTo(10));
            Assert.That(start[5].Capacity, Is.EqualTo(2));
            Assert.That(start[6].Mechanic, Is.EqualTo(MechanicType.Hoja));
            Assert.That(start[6].MechanicKey, Is.EqualTo(3));
            Assert.That(flow.Session.PourMode, Is.EqualTo(PourMode.Run));

            // Los 8 primeros vertidos dejan el nivel en juego (tras el 6.º, el 7.º y el 8.º aún se gana, con un fútil: D-014,
            // pregunta 46 (a)); el 9.º lo bloquea en el mismo toque.
            for (int i = 0; i <= LastPlayable; i++)
            {
                string what = "vertido " + (i + 1) + " (" + OwnersSequence[i][0] + " → " + OwnersSequence[i][1] + ")";
                Assert.That(boot.TapCuia(OwnersSequence[i][0]).Kind, Is.EqualTo(TapKind.Selected), "selección del " + what);
                TapResult poured = boot.TapCuia(OwnersSequence[i][1]);
                Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured), what);
                LevelStatus expected = i == LastPlayable ? LevelStatus.Stuck : LevelStatus.Playing;
                Assert.That(poured.Pour.Status, Is.EqualTo(expected), what);
                Assert.That(flow.Session.Status, Is.EqualTo(expected), what);
                if (i < LastPlayable)
                {
                    PlayModeHelpers.StepUntilIdle(boot, 6000);
                    Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "tras el " + what);
                    Assert.That(flow.AcceptsBoardInput, Is.True, "tras el " + what);
                }
            }

            // Solo por D-014: R13 de reglas aún ve vertidos útiles (los vaivenes 2 → 1 y 0 → 7, los vertidos 10.º y 11.º del
            // dueño; 7 → 0 no es legal aquí porque la 0 está llena). La entrada queda bloqueada desde el instante lógico del
            // bloqueo, también mientras se anima el 9.º (R15).
            BoardState stuck = flow.Session.Current;
            Assert.That(PourRules.HasUsefulMove(stuck, true), Is.True);
            Assert.That(PourRules.IsStuck(stuck, true), Is.False, "R13 de reglas no lo declara: es el bucle de D-014");
            Assert.That(flow.AcceptsBoardInput, Is.False);
            Assert.That(boot.TapCuia(OwnersSequence[LastPlayable + 1][0]).Kind, Is.EqualTo(TapKind.Ignored), "mientras se anima el 9.º");

            // Aguas estancadas se abre tras la animación y stuck_modal_delay_ms (R15).
            WaitForStuckModal(boot, "tras el 9.º vertido");
            Assert.That(view.StuckVisible, Is.True);

            // Los vertidos que siguen en la secuencia del dueño (10.º a 12.º) se ignoran: ya no hay bucle.
            for (int i = LastPlayable + 1; i < OwnersSequence.Length; i++)
            {
                Assert.That(boot.TapCuia(OwnersSequence[i][0]).Kind, Is.EqualTo(TapKind.Ignored), "selección del vertido " + (i + 1));
                Assert.That(boot.TapCuia(OwnersSequence[i][1]).Kind, Is.EqualTo(TapKind.Ignored), "vertido " + (i + 1));
                PlayModeHelpers.Step(boot, 100);
            }

            Assert.That(flow.Session.AppliedPours, Is.EqualTo(LastPlayable + 1));
            Assert.That(flow.Session.Current.Equals(stuck), Is.True, "el tablero no cambia");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));

            // El embudo de D-008 en el 66 (≥ 31; 04 §6.2): Marea primero, con el cupo del nivel (3 desde el 11) sin gastar;
            // «Continuar: +1 cuia» a la venta (con una cuia más el 66 se gana: la compra cumple, 04 §6.4) por 300, el primer
            // rescate del día; «Rendirse» al pie, sin «Volver a la orilla (gratis)», y la línea de canoa.
            StuckModalView modal = view.Stuck.View!;
            UndoStatus undo = flow.UndoStatus;
            Assert.That(undo.FreeLeft, Is.EqualTo(3), "cupo de Marea del nivel (desde el 11)");
            Assert.That(modal.Undo, Is.EqualTo(StuckUndoKind.Undo), "regla 0: Marea primero");
            Assert.That(modal.UndoBadge, Is.EqualTo(undo.FreeLeft + undo.Stock));
            Assert.That(view.Stuck.UndoButton.gameObject.activeSelf && view.Stuck.UndoButton.interactable, Is.True);
            Assert.That(modal.Rescue, Is.EqualTo(StuckRescueKind.Available), "«Continuar: +1 cuia»");
            Assert.That(modal.Unsolvable, Is.False, "con una cuia más se gana");
            Assert.That(view.Stuck.RescueButton.gameObject.activeSelf, Is.True);
            Assert.That(view.Stuck.RescuePriceShown, Is.EqualTo(300), "primer rescate del día (rescue_first_day_mult)");
            Assert.That(modal.Footer, Is.EqualTo(StuckFooter.GiveUp));
            Assert.That(view.Stuck.GiveUpButton.gameObject.activeSelf, Is.True, "«Rendirse» desde el 31");
            Assert.That(view.Stuck.RestartButton.gameObject.activeSelf, Is.False);
            Assert.That(view.Stuck.CanoeLineText, Is.EqualTo(UiText.StuckCanoeLost));

            // Marea desde el bloqueo por bucle (04 §2.1): vuelve a jugar con 8 vertidos; el mismo 1 → 5 lo vuelve a declarar.
            view.Stuck.UndoButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(LastPlayable));
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(flow.UndoStatus.FreeLeft, Is.EqualTo(2));
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            Assert.That(flow.AcceptsBoardInput, Is.True);
            Assert.That(boot.TapCuia(OwnersSequence[LastPlayable][0]).Kind, Is.EqualTo(TapKind.Selected));
            TapResult again = boot.TapCuia(OwnersSequence[LastPlayable][1]);
            Assert.That(again.Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(again.Pour.Status, Is.EqualTo(LevelStatus.Stuck));
            WaitForStuckModal(boot, "tras repetir el 9.º vertido");
            Assert.That(view.Stuck.View!.UndoBadge, Is.EqualTo(flow.UndoStatus.FreeLeft + flow.UndoStatus.Stock));
        }

        // Avanza hasta que se abre Aguas estancadas (como mucho 5 s) y deja que el panel termine de subir.
        private static void WaitForStuckModal(GameBootstrap boot, string what)
        {
            LevelFlow flow = boot.Flow!;
            for (int ms = 0; ms < 5000 && flow.Phase != FlowPhase.StuckModal; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal), what);
            PlayModeHelpers.Step(boot, 300); // el panel termina de subir (250 ms)
        }
    }
}
