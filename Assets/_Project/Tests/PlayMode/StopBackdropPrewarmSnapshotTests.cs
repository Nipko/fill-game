#nullable enable
using System.Collections;
using System.Globalization;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// «Sin cambios visibles» de <see cref="StopBackdrop.Prewarm"/> en la pantalla de juego real (tarea FX4, cambio 2; revisión
    /// de FX4): el 71 (Tefé) se carga dos veces en el mismo estado, la primera sin la preparación (<see cref="GameScreen.StopPrewarmEnabled"/>
    /// = false, como antes de FX4; si es lo primero que se ejecuta en el proceso, en frío) y la segunda con ella, y las dos
    /// capturas (<c>fx4_fondo_l71_sin_prewarm</c> y <c>fx4_fondo_l71_con_prewarm</c>) son iguales píxel a píxel: el panorama, la
    /// silueta del héroe, la banda de agua, el LUT tras <see cref="StopBackdrop.Apply"/> y lo que Prewarm activa y restaura (la
    /// raíz, el juego de capas de detrás y su alfa). Cada captura se toma dos veces: en la intro (a los 50 ms) y ya en juego.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.StopBackdropPrewarmSnapshotTests</c> sin <c>-nographics</c>); los PNG quedan en
    /// <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Comparación de capturas del 71 con y sin Prewarm (FX4); necesita dispositivo gráfico.")]
    public sealed class StopBackdropPrewarmSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            GameScreen.StopPrewarmEnabled = true;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Level71_WithAndWithoutPrewarm_LooksTheSame()
        {
            string dir = SnapshotKit.Prepare();
            var without = new Color32[2][];
            var with = new Color32[2][];
            GameScreen.StopPrewarmEnabled = false;
            yield return Capture(dir, "fx4_fondo_l71_sin_prewarm", without, false);
            GameScreen.StopPrewarmEnabled = true;
            yield return Capture(dir, "fx4_fondo_l71_con_prewarm", with, true);

            string[] moments = { "intro", "juego" };
            for (int m = 0; m < 2; m++)
            {
                int differ = 0;
                int maxDelta = 0;
                Color32[] a = without[m];
                Color32[] b = with[m];
                Assert.That(b.Length, Is.EqualTo(a.Length));
                for (int i = 0; i < a.Length; i++)
                {
                    int d = Mathf.Max(Mathf.Abs(a[i].r - b[i].r), Mathf.Max(Mathf.Abs(a[i].g - b[i].g), Mathf.Abs(a[i].b - b[i].b)));
                    if (d > 0)
                    {
                        differ++;
                        maxDelta = Mathf.Max(maxDelta, d);
                    }
                }

                string line = "AF_FX4 71 " + moments[m] + ": " + differ.ToString(CultureInfo.InvariantCulture) + " de "
                    + a.Length.ToString(CultureInfo.InvariantCulture) + " píxeles distintos (máx. "
                    + maxDelta.ToString(CultureInfo.InvariantCulture) + "/255)";
                Debug.Log(line);
                TestContext.WriteLine(line);
                Assert.That(differ, Is.EqualTo(0), "71 " + moments[m] + ": con y sin Prewarm, la misma imagen");
            }
        }

        private static IEnumerator Capture(string dir, string name, Color32[][] pixels, bool warmed)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(71, new MemoryProgressStore(71, 70));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            StopBackdrop b = boot.View!.StopBackdrop;
            Assert.That(b.Stop, Is.Not.Null, name);
            Assert.That(b.Stop!.Id, Is.EqualTo("tefe"), name);
            Assert.That(b.Warmed, Is.EqualTo(warmed), name);
            Debug.Log("AF_FX4 " + name + ": build_ms " + b.BuildMs.ToString("0.###", CultureInfo.InvariantCulture) + ", warm_ms "
                + b.WarmMs.ToString("0.###", CultureInfo.InvariantCulture));

            PlayModeHelpers.Step(boot, 50);
            yield return SnapshotKit.Wait(0.2f);
            pixels[0] = SnapshotKit.SavePixels(dir, name + "_intro");

            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), name);
            yield return SnapshotKit.Wait(0.2f);
            pixels[1] = SnapshotKit.SavePixels(dir, name + "_juego");
        }
    }
}
