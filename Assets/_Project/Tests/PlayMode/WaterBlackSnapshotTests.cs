#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Board;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión del agua negra en ámbar-té translúcido (D-029; guion de la intro §18, Q14-A; tarea DC13;
    /// <c>[Explicit]</c>, no forman parte de la suite): <c>dc13_l61_llenar</c> (el 61, mundo 3, en «Llenar y teñir» con el agua
    /// del río ya en las cuias y sin teñir, junto a las del tinte 14 de la capa de abajo), <c>dc13_l61_tablero</c> (el tablero en
    /// reposo con, en las vistas de sus cuias vacías, el agua negra de ahora y la de antes, #4A2C14, al mismo 70 %, y en una cuia
    /// del tinte 14 el Barro debajo: el tinte 14, el Barro y las dos aguas a la vista) y <c>dc13_l40_llegada_encontro</c> (la
    /// franja de llegada del Encuentro de las Aguas tras ganar el 40, con su mini cuia negra y blanca). Una línea
    /// <c>AF_WATERBLACK</c> por captura con los colores y sus L*. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode
    /// --filter AF.Tests.PlayMode.WaterBlackSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en
    /// <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class WaterBlackSnapshotTests
    {
        // El agua negra de antes (07 §2.1 hasta DC13), para compararla en la misma captura.
        private static readonly Color Previous = Palette.Hex(0x4A2C14);

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.ArrivalDisabledForTests = false;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureWorld3Board()
        {
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGame(61, new MemoryProgressStore(61, 60));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Level.WaterBase, Is.EqualTo("black"), "el 61 va con agua negra (mundo 3)");

            // «Llenar y teñir» con el agua del río en las cuias, antes de teñirse.
            view.DebugReplayIntro(1400, false);
            IntroChoreography plan = view.Intro.Plan;
            float at = Mathf.Min(plan.LastFillEndMs + 20f, plan.TotalMs - 1f);
            view.Intro.Seek(at);
            Log("dc13_l61_llenar", "intro a " + at.ToString("0") + " ms de " + plan.TotalMs.ToString("0") + " (fin del llenado "
                + plan.LastFillEndMs.ToString("0") + ", teñido desde " + plan.DStartMs.ToString("0") + "); agua " + Describe(view.IntroWaterColor));
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "dc13_l61_llenar");

            view.Intro.Complete();
            PlayModeHelpers.Step(boot, 600);

            // En reposo: el agua de ahora y la de antes en las cuias vacías (al 70 %, como las pinta la intro), y el Barro bajo
            // el tinte 14 en una cuia que lo tiene.
            BoardState board = boot.Flow.Session.Current;
            int shown = 0;
            int withTint14 = -1;
            for (int i = 0; i < board.Count; i++)
            {
                Cuia c = board[i];
                if (c.Count == 0 && shown < 2)
                {
                    Color water = shown == 0 ? Palette.WaterBlack : Previous;
                    for (int k = 0; k < c.Capacity; k++)
                    {
                        view.Views[i].SetDose(k, ColorMath.WithAlpha(water, IntroAnimation.WaterAlpha), 1f, 0);
                    }

                    Log("dc13_l61_tablero", "cuia " + i + (shown == 0 ? ": ahora " : ": antes ") + Describe(water));
                    shown++;
                }
                else if (withTint14 < 0 && c.Count == c.Capacity && Has(c, 14))
                {
                    withTint14 = i;
                }
            }

            Assert.That(shown, Is.EqualTo(2), "el 61 tiene dos cuias vacías");
            Assert.That(withTint14, Is.GreaterThanOrEqualTo(0), "una cuia llena con el tinte 14");
            Cuia mud = Cuia.Create(board[withTint14].Capacity, new[] { Cuia.MudColor, Cuia.MudColor, 14, 14 });
            view.Views[withTint14].ShowContent(mud, false);
            Log("dc13_l61_tablero", "cuia " + withTint14 + ": Barro " + Describe(Palette.Mud) + " bajo el tinte 14 " + Describe(Palette.Tint(14)));
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "dc13_l61_tablero");
            Assert.That(File.Exists(Path.Combine(dir, "dc13_l61_tablero.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureEncounterArrival()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            MapArtRule.ForceProvisional = true;
            string dir = SnapshotKit.Prepare();
            yield return VictoryArrivalTests.Win(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(view.Arrival, Is.Not.Null);
            Assert.That(view.Arrival!.Completed, Is.True, "la franja terminada");
            Color first = ArrivalStrip.SampleWater("black_white", out Color? second);
            Log("dc13_l40_llegada_encontro", "mini cuia " + Describe(view.Arrival.SampleColor) + "; negra " + Describe(first) + ", blanca "
                + Describe(second ?? Color.clear));
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "dc13_l40_llegada_encontro");
        }

        private static bool Has(Cuia c, int tint)
        {
            for (int k = 0; k < c.Count; k++)
            {
                if (c.ColorAt(k) == tint)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Describe(Color c)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(c) + " (L* " + ColorMath.LabLightness(c).ToString("0.0") + ")";
        }

        private static void Log(string name, string text)
        {
            Debug.Log("AF_WATERBLACK " + name + ": " + text);
        }
    }
}
