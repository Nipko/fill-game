#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de TiTino en la victoria (tarea TI6a; propuesta de TiTino d.4 y paso 6a de (i): «hay que medirlo con las
    /// capturas del paso 6a»), en <c>Logs/snapshots/es-419/</c>:
    /// <list type="bullet">
    /// <item><c>ti6a_l40_1fila_{780|640}_{100|130}</c>: el hito del 40 con un tablero de una fila a los 1,60 s de abrirse la
    /// pantalla de victoria (la fila 0,80 + 1,60 s = la 2,40): con el bocadillo si la caja mide ≥ 104 dp y la frase cabe; con
    /// 56-103 dp (640 al 130 %), solo el gesto; la garza del fondo ya fundida;</item>
    /// <item><c>ti6a_l40_entrada_780_100</c>: la rama a mitad de su entrada, a los 150 ms de abrirse la pantalla de victoria
    /// (la fila 0,80 + 0,15 s = la 0,95), con el panel ya arriba;</item>
    /// <item><c>ti6a_l40_2filas_780_100</c> y <c>ti6a_l39_1fila_780_100</c>: un tablero de dos filas en el hito y una victoria
    /// que no es hito (solo el gesto);</item>
    /// <item><c>ti6a_l44_sinfruta_780_100</c>: una victoria que no es hito y va sin línea de fruta (como 261-300 y 321-700), en
    /// la que le toca la frase de 1 de cada 4 (D-016, punto 5);</item>
    /// <item><c>ti6a_l40_3filas_{780|640}_100</c>: con 3 filas no sale y el hito lleva su dato;</item>
    /// <item><c>ti6a_real_l{40|60|80|140}_{780|640}_100</c>: los hitos del catálogo real tal como los ve el juego (con la ranura
    /// del Dato del río), con su caja medida; y <c>ti6a_real_l…_sinranura</c>, los mismos sin la ranura.</item>
    /// </list>
    /// Salvo en las del catálogo real con la ranura, la victoria va sin la ranura del Dato del río
    /// (<see cref="SlothVictoryTests.NoSlot"/>). En los hitos reales (2 filas) la caja de TiTino mide 0 dp a 360 × 780 y a
    /// 360 × 640, con la ranura y sin ella: TiTino no sale (decisión pendiente del dueño; TI6a).
    /// Cada captura comprueba que TiTino no se cruza con nada (<see cref="SlothVictoryTests.AssertNoIntersection"/>) y escribe
    /// una línea <c>AF_SLOTH</c> con la caja, el modo, el tamaño y el bocadillo. Explícito: necesita gráficos (<c>unity test
    /// &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.SlothVictorySnapshotTests</c> sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de TiTino en la victoria (TI6a); necesitan dispositivo gráfico.")]
    public sealed class SlothVictorySnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            SlothVictoryTests.NoSlot(true);
        }

        [TearDown]
        public void TearDown()
        {
            SlothVictoryTests.NoSlot(false);
            SnapshotKit.Release();
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureOneRowHito()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string name = "ti6a_l40_1fila_" + (small ? "640" : "780") + "_" + (scale > 1.01f ? "130" : "100");
                    yield return Capture(name, SlothVictoryTests.OneRowPack(40), 40, small, scale, 1600);
                    GameScreen view = GameBootstrap.Current!.View!;
                    SlothVictory sloth = view.SlothView;
                    if (view.SlothBoxDp >= GameScreen.SlothSpeakBoxDp && view.SlothCouldSpeak)
                    {
                        Assert.That(sloth.BubbleShown, Is.True, name + ": caja de ≥ 104 dp y la frase cabe: el bocadillo, a la vista");
                    }
                    else
                    {
                        // 56-103 dp (o la frase no cabe): solo el gesto, sin bocadillo.
                        Assert.That(view.SlothBoxDp, Is.GreaterThanOrEqualTo(SlothVictory.GestureBoxDp), name);
                        Assert.That(sloth.Visible, Is.True, name + ": TiTino, a la vista");
                        Assert.That(sloth.Mode, Is.EqualTo(view.SlothBoxDp >= GameScreen.SlothSpeakBoxDp ? SlothVictoryMode.Speak : SlothVictoryMode.Gesture), name);
                        Assert.That(sloth.HasBubble, Is.False, name + ": sin bocadillo");
                    }

                    Release();
                }
            }

            yield return Capture("ti6a_l40_entrada_780_100", SlothVictoryTests.OneRowPack(40), 40, false, 1f, 150);
            Release();
        }

        [UnityTest]
        public IEnumerator CaptureRowsAndNonHito()
        {
            yield return Capture("ti6a_l40_2filas_780_100", PlayModeHelpers.Level7BoardPack(2, "w02_tapajos", "clear", 40), 40, false, 1f, 1600);
            Release();
            yield return Capture("ti6a_l39_1fila_780_100", SlothVictoryTests.OneRowPack(39), 39, false, 1f, 1600);
            Release();
            yield return Capture("ti6a_l44_sinfruta_780_100", SlothVictoryTests.OneRowPack(44), 44, false, 1f, 1600, v => v.FruitKeyOf = null);
            Assert.That(GameBootstrap.Current!.View!.SlothView.BubbleShown, Is.True, "sin línea de fruta, el 44 le toca: habla");
            Release();
            foreach (bool small in new[] { false, true })
            {
                string name = "ti6a_l40_3filas_" + (small ? "640" : "780") + "_100";
                yield return Capture(name, SlothVictoryTests.FifteenCuiaPack(40), 40, small, 1f, 1600);
                GameScreen view = GameBootstrap.Current!.View!;
                Assert.That(view.SlothView.Visible, Is.False, name + ": con 3 filas no sale");
                Assert.That(view.CurrentVictoryLine.ArrivalFact, Is.True, name + ": el hito lleva su dato");
                Release();
            }
        }

        [UnityTest]
        public IEnumerator CaptureRealHitos()
        {
            foreach (bool slot in new[] { true, false })
            {
                SlothVictoryTests.NoSlot(!slot);
                foreach (bool small in new[] { false, true })
                {
                    foreach (int level in new[] { 40, 60, 80, 140 })
                    {
                        string name = "ti6a_real_l" + level + "_" + (small ? "640" : "780") + "_100" + (slot ? string.Empty : "_sinranura");
                        yield return Capture(name, null, level, small, 1f, 1600);
                        Release();
                    }
                }
            }
        }

        // Carga (con el paquete json o el catálogo real), gana, avanza ms desde la victoria, comprueba los solapes y captura.
        private static IEnumerator Capture(string name, string? json, int level, bool small, float scale, int ms,
            System.Action<GameScreen>? before = null)
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            if (small)
            {
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            }

            LaunchOptions.FontScaleOverride = scale;
            if (json != null)
            {
                yield return SlothVictoryTests.WinPack(json, level, before);
            }
            else
            {
                yield return VictoryArrivalTests.Win(level);
            }

            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SnapshotKit.Attach(boot);
            for (int t = 0; t < ms; t += PlayModeHelpers.FrameMs)
            {
                SlothVictoryTests.AssertNoIntersection(view, name + " a los " + t + " ms");
                PlayModeHelpers.TickExact(boot, Mathf.Min(PlayModeHelpers.FrameMs, ms - t));
            }

            SlothVictoryTests.AssertNoIntersection(view, name);
            SlothVictory sloth = view.SlothView;
            VictoryLine line = view.CurrentVictoryLine;
            Debug.Log("AF_SLOTH " + name + ": caja " + SlothVictoryTests.F(view.SlothBandTopDp) + " → " + SlothVictoryTests.F(view.SlothBandBottomDp)
                + " (" + SlothVictoryTests.F(view.SlothBoxDp) + " dp), " + view.Layout!.Rows + " filas, TiTino " + sloth.Mode + " a "
                + SlothVictoryTests.F(sloth.SizeDp) + " dp, rama en " + SlothVictoryTests.F(sloth.RootX) + "/" + SlothVictoryTests.F(sloth.RootY)
                + ", bocadillo " + (sloth.HasBubble ? sloth.BubbleRect + " en " + sloth.BubbleLines + " líneas a " + SlothVictoryTests.F(sloth.BubbleFontDp) + " dp" : "no")
                + ", sin el tope de la cabecera " + SlothVictoryTests.F(view.SlothBoxUnclampedDp) + " dp"
                + ", línea: habla " + line.SlothSpeaks + ", dato de llegada " + line.ArrivalFact + ", héroe " + view.HeroRect);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
        }

        private static void Release()
        {
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }
    }
}
