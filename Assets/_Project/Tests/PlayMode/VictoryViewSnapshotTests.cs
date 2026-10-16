#nullable enable
using System.Collections;
using System.Globalization;
using System.IO;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la victoria con vista (tarea M2.2; D-013, punto 8, Q6-A; 02 §6; 07 §5.4), con las siluetas provisionales:
    /// <list type="bullet">
    /// <item><c>victory_view_l20_{000|125|end}</c>: la llegada a Alter do Chão al abrirse la victoria, a mitad del fundido y con
    /// la victoria entera (360 × 780 dp);</item>
    /// <item><c>victory_view_l10_end</c>: el 10, que no es una llegada (la cuenta el Mapa): el tablero de 2 filas se va sobre
    /// el estuario de Belém, sin fundido del fondo;</item>
    /// <item><c>victory_view_l130_end</c>: la llegada al río de la frontera, en el Trapecio;</item>
    /// <item><c>victory_view_l131_{000|end}</c>: una victoria sin llegada en un tablero de 3 filas del Trapecio (11 cuias a
    /// 360 × 780 dp; el sitio de siempre, sin el panel delante);</item>
    /// <item><c>victory_view_l131_640_end</c>: el mismo nivel a 360 × 640 dp, donde sus 11 cuias van en 2 filas (D-006: con
    /// 11-14, la que dé la cuia más ancha), recortada a la pantalla de 640 dp (1080 × 1920 px);</item>
    /// <item><c>victory_view_16_640_{000|end}</c>: 3 filas a 360 × 640 dp, con el paquete de prueba de 16 cuias (6/5/5) en el
    /// 131, porque en 1-200 ningún nivel pasa de 12 cuias: la victoria al abrirse (la ola aún salta y el tablero sigue entero)
    /// y entera (la cabecera, el sitio y la cinta con el panel más alto);</item>
    /// <item><c>victory_view_off_l131_end</c>: la misma victoria con <c>ui_victory_view</c> apagada, para comparar.</item>
    /// </list>
    /// Cada captura escribe su línea <c>AF_VICTORY_VIEW</c> con el alfa del tablero, la parada, el héroe, el horizonte y la cinta.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.VictoryViewSnapshotTests</c>
    /// sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de la victoria con vista (M2.2); necesitan dispositivo gráfico.")]
    public sealed class VictoryViewSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.VictoryViewOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureTheVictoryView()
        {
            yield return Capture(20, false, true, new[] { 0, 125 }, "victory_view_l20");
            yield return Capture(10, false, true, new int[0], "victory_view_l10");
            yield return Capture(130, false, true, new int[0], "victory_view_l130");
            yield return Capture(131, false, true, new[] { 0 }, "victory_view_l131");
            yield return Capture(131, true, true, new int[0], "victory_view_l131_640");
            yield return Capture(131, true, true, new[] { 0 }, "victory_view_16_640", VictoryViewTests.SixteenCuiaWinPack(131));
            yield return Capture(131, false, false, new int[0], "victory_view_off_l131");
        }

        // Gana el nivel (del paquete «pack» si lo hay), captura en cada instante de «at» (ms desde la victoria, en orden) y con
        // la victoria entera («_end»).
        private static IEnumerator Capture(int level, bool small, bool view, int[] at, string name, string? pack = null)
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            GameScreen.VictoryViewOverride = view;
            string dir = SnapshotKit.Prepare();
            if (small)
            {
                GameScreen.SimulatedScreen = HudLabelTests.Screen640; // tras Prepare, que pone la de 360 × 780
            }

            if (pack != null)
            {
                LaunchOptions.LevelPackJsonOverride = pack;
            }

            yield return VictoryArrivalTests.Win(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen screen = boot.View!;
            SnapshotKit.Attach(boot);
            int elapsed = 0;
            foreach (int ms in at)
            {
                PlayModeHelpers.TickExact(boot, ms - elapsed);
                elapsed = ms;
                yield return Save(dir, name + "_" + ms.ToString("000", CultureInfo.InvariantCulture), screen, small);
            }

            PlayModeHelpers.Step(boot, 1600);
            if (view)
            {
                Assert.That(screen.BoardAlpha, Is.EqualTo(0f), name);
            }

            yield return Save(dir, name + "_end", screen, small);
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }

        // Con la pantalla de 360 × 640 dp, solo lo que ve el jugador: las filas de arriba de esa pantalla (como
        // FactLineSnapshotTests; revisión de M2.2).
        private static IEnumerator Save(string dir, string name, GameScreen view, bool small)
        {
            yield return SnapshotKit.Wait(0.2f);
            if (small)
            {
                SaveTop(dir, name, (int)HudLabelTests.Screen640.y);
            }
            else
            {
                SnapshotKit.Save(dir, name);
            }

            StopBackdrop b = view.StopBackdrop;
            Rect hero = view.HeroRect;
            Debug.Log("AF_VICTORY_VIEW " + name + ": tablero " + F(view.BoardAlpha) + ", filas " + (view.Layout?.Rows ?? 0) + ", parada "
                + (b.Stop?.Id ?? "-") + (b.Fading ? " (fundiendo)" : string.Empty) + ", héroe " + b.HeroSource + " " + F(hero.yMin) + "-"
                + F(hero.yMax) + " dp, horizonte " + F(view.HorizonY) + " dp, cinta " + F(view.VictoryPanelTopDp - 38f) + " dp");
        }

        // Guarda solo las heightPx filas de arriba de la captura (la pantalla simulada de 360 × 640 dp).
        private static void SaveTop(string dir, string name, int heightPx)
        {
            var full = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Assert.That(full.LoadImage(SnapshotKit.Capture()), Is.True, name);
            int h = Mathf.Min(heightPx, full.height);
            var top = new Texture2D(full.width, h, TextureFormat.RGB24, false);
            top.SetPixels(full.GetPixels(0, full.height - h, full.width, h));
            top.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), top.EncodeToPNG());
            Object.Destroy(full);
            Object.Destroy(top);
        }

        private static string F(float v)
        {
            return v.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
