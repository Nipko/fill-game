#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Ranura del Dato del río de la victoria (02 §6; Q7-A de D-017; arquitectura de la tanda 4, §2; tarea M1.3b) en la escena:
    /// el alto por idioma y escala (cadenas del peor caso de 90 y 117 caracteres) con su caché; sin proveedores no hay reserva y
    /// la victoria es la de siempre; con uno activo, la ranura va bajo «Inicio» con el alto fijo y «Siguiente» queda a la misma
    /// altura en el 1, el 11 (Cosecha doble) y el 39 (chip del nivel siguiente y Cosecha doble); «Siguiente» se habilita y se
    /// ve en el mismo milisegundo con y sin ranura, y el contenido aparece a los 1,40 s (sin fundido con Reducir movimiento);
    /// y la caja de TiTino es la franja entre la cabecera y la cima de las cuias tras ShiftToClear.
    /// </summary>
    public sealed class VictorySlotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [Test]
        public void WorstCaseStrings_HaveTheCapLengthOfTheirLocale()
        {
            Assert.That(VictoryLineSlot.WorstCaseText(LocLocale.SpanishLatinAmerica).Length, Is.EqualTo(90), "es-419: tope de un dato (02 §6)");
            Assert.That(VictoryLineSlot.WorstCaseText(LocLocale.English).Length, Is.EqualTo(117));
            Assert.That(VictoryLineSlot.WorstCaseText(LocLocale.PortugueseBrazil).Length, Is.EqualTo(117));
            Assert.That(VictoryLineSlot.WorstCaseText(LocLocale.Pseudo).Length, Is.EqualTo(117));
            Assert.That(VictoryLineSlot.WorstCaseText(LocLocale.Pseudo), Does.StartWith("[").And.EndWith("]"), "en-XA, entre corchetes");
            Assert.That(VictoryLineSlot.WorstCaseChars(LocLocale.SpanishLatinAmerica), Is.EqualTo(90));
            Assert.That(VictoryLineSlot.WorstCaseChars(LocLocale.PortugueseBrazil), Is.EqualTo(117));
        }

        [UnityTest]
        public IEnumerator Height_DependsOnLocaleAndScale_AndIsMeasuredOncePerCombination()
        {
            yield return HudSetup.LoadAtIntro(1);
            GameScreen view = GameBootstrap.Current!.View!;
            VictoryLineSlot slot = view.VictorySlot;
            Assert.That(slot.Built, Is.False, "sin proveedores ni medidas, la ranura no crea nada");

            float es = view.VictorySlotContentHeight(LocLocale.SpanishLatinAmerica, 1f);
            Assert.That(slot.MeasureCount, Is.EqualTo(1));
            Assert.That(view.VictorySlotContentHeight(LocLocale.SpanishLatinAmerica, 1f), Is.EqualTo(es), "de la caché");
            Assert.That(slot.MeasureCount, Is.EqualTo(1), "la misma combinación no se vuelve a medir");

            float en = view.VictorySlotContentHeight(LocLocale.English, 1f);
            float pt = view.VictorySlotContentHeight(LocLocale.PortugueseBrazil, 1f);
            float xa = view.VictorySlotContentHeight(LocLocale.Pseudo, 1f);
            float es13 = view.VictorySlotContentHeight(LocLocale.SpanishLatinAmerica, 1.3f);
            float xa13 = view.VictorySlotContentHeight(LocLocale.Pseudo, 1.3f);
            Assert.That(slot.MeasureCount, Is.EqualTo(6));
            view.VictorySlotContentHeight(LocLocale.Pseudo, 1.3f);
            view.VictorySlotContentHeight(LocLocale.English, 1f);
            Assert.That(slot.MeasureCount, Is.EqualTo(6), "otra vez de la caché");
            TestContext.WriteLine("H del contenido (dp, ancho " + view.VictorySlotWidthDp + "): es " + es + ", en " + en + ", pt-BR " + pt
                + ", en-XA " + xa + "; al 130 %: es " + es13 + ", en-XA " + xa13);

            // Cabecera de 32 dp y al menos dos renglones de 14 sp: 90 caracteres no caben en uno de 296 dp.
            float twoLines = VictoryLineSlot.HeaderDp + (2f * VictoryLineSlot.TextSp);
            Assert.That(view.VictorySlotWidthDp, Is.EqualTo(296f), "panel de 328 dp con 16 de margen a cada lado");
            Assert.That(es, Is.GreaterThanOrEqualTo(twoLines));
            Assert.That(es, Is.GreaterThanOrEqualTo(VictoryLineSlot.MinContentDp), "toda la línea es un botón de 48 dp");
            Assert.That(en, Is.GreaterThanOrEqualTo(es), "117 caracteres no ocupan menos que 90");
            Assert.That(pt, Is.GreaterThanOrEqualTo(es));
            Assert.That(xa, Is.GreaterThanOrEqualTo(es));
            Assert.That(es13, Is.GreaterThan(es), "al 130 % el texto crece");
            Assert.That(xa13, Is.GreaterThan(xa));
            Assert.That(slot.ReserveFor(LocLocale.SpanishLatinAmerica, 1f, view.VictorySlotWidthDp),
                Is.EqualTo(es + VictoryLineSlot.RuleGapDp + VictoryLineSlot.RuleDp + VictoryLineSlot.RuleBelowDp));
        }

        [UnityTest]
        public IEnumerator WithoutActiveProviders_ThereIsNoReserve()
        {
            yield return HudSetup.LoadAtIntro(1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            var idle = new SlotProbe { Active = false };
            view.RegisterVictoryLineProvider(idle);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 100);

            Assert.That(view.VictorySlot.Reserved, Is.False, "un proveedor inactivo no reserva");
            Assert.That(view.VictorySlot.Built, Is.False, "ni crea la ranura");
            Assert.That(view.VictorySlot.ReserveDp, Is.EqualTo(0f));
            Assert.That(view.VictoryPanelHeight, Is.EqualTo(218f), "la victoria de siempre en el 1 (sin Cosecha doble ni chip)");
            Assert.That(view.VictoryPanelBottomDp - 18f, Is.EqualTo(view.HomeBottomDp).Within(0.01f), "Inicio sigue al pie del panel");
            Assert.That(idle.Shows, Is.EqualTo(0));
            Assert.That(idle.Layouts, Is.EqualTo(0));
            Assert.That(idle.Hides, Is.GreaterThan(0), "lo suyo, oculto");
            Assert.That(view.CurrentVictoryLine.FruitKey, Is.Null, "sin calendario de frutas, sin línea de fruta");
        }

        [UnityTest]
        public IEnumerator WithAnActiveProvider_TheSlotGoesBelowHome_AndNextStaysPutAcrossLevels()
        {
            // Referencia sin ranura (nivel 1).
            yield return HudSetup.LoadAtIntro(1);
            GameBootstrap boot = GameBootstrap.Current!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 100);
            float nextWithout = boot.View!.NextTopDp;

            var nexts = new List<float>();
            float reserve = -1f;
            foreach (int level in new[] { 1, 11, 39 })
            {
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
                yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
                boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                GameScreen view = boot.View!;
                var probe = new SlotProbe();
                view.RegisterVictoryLineProvider(probe);
                VictorySetup.WinToVictoryScreen(boot);
                PlayModeHelpers.Step(boot, 100);

                VictoryLineSlot slot = view.VictorySlot;
                float content = view.VictorySlotContentHeight(LocLocale.SpanishLatinAmerica, 1f);
                Assert.That(slot.Reserved, Is.True);
                Assert.That(slot.ContentDp, Is.EqualTo(content), "el alto fijo del idioma y la escala, de la caché");
                Assert.That(slot.ReserveDp, Is.EqualTo(content + VictoryLineSlot.RuleGapDp + VictoryLineSlot.RuleDp + VictoryLineSlot.RuleBelowDp));
                Assert.That(view.SlotTopDp, Is.EqualTo(view.HomeBottomDp).Within(0.01f), "bajo Inicio");
                Assert.That(view.VictoryPanelBottomDp - 18f, Is.EqualTo(view.SlotTopDp + slot.ReserveDp).Within(0.01f), "dentro del panel");
                Assert.That(probe.Layouts, Is.GreaterThan(0));
                Assert.That(probe.LastWidth, Is.EqualTo(view.VictorySlotWidthDp));
                Assert.That(probe.LastHeight, Is.EqualTo(content));
                Assert.That(probe.Content, Is.SameAs(slot.Content));
                Assert.That(probe.Shows, Is.EqualTo(1), "una vez por victoria");
                Assert.That(probe.LastLevel, Is.EqualTo(level));
                Assert.That(slot.Shown, Is.True);
                if (level == 39)
                {
                    Assert.That(view.NextChip.Visible, Is.True, "el 40 es Difícil: chip sobre Siguiente");
                }

                TestContext.WriteLine("nivel " + level + ": Siguiente en " + view.NextTopDp + " dp, panel de " + view.VictoryPanelHeight
                    + " dp, Cosecha doble " + view.DoubleHarvestShown);
                if (reserve < 0f)
                {
                    reserve = slot.ReserveDp;
                }

                nexts.Add(view.NextTopDp);
            }

            for (int i = 1; i < nexts.Count; i++)
            {
                Assert.That(nexts[i], Is.EqualTo(nexts[0]).Within(0.01f), "Siguiente a la misma altura en el 1, el 11 y el 39");
            }

            Assert.That(nextWithout - nexts[0], Is.EqualTo(reserve).Within(0.01f), "Siguiente sube una vez, lo que mide la ranura");
        }

        [UnityTest]
        public IEnumerator NextEnablesInTheSameMillisecond_AndTheContentArrivesAt140s()
        {
            int[] without = new int[3];
            int[] with = new int[3];
            yield return MeasureNext(null, without);
            var probe = new SlotProbe();
            yield return MeasureNext(probe, with);
            TestContext.WriteLine("Siguiente (ms desde la victoria): habilitado " + with[0] + ", visible " + with[1] + ", entero " + with[2]);
            Assert.That(with[0], Is.EqualTo(without[0]), "Siguiente se habilita en el mismo milisegundo");
            Assert.That(with[1], Is.EqualTo(without[1]), "empieza a verse en el mismo milisegundo");
            Assert.That(with[2], Is.EqualTo(without[2]), "entero en el mismo milisegundo");
            Assert.That(with[2], Is.LessThanOrEqualTo(1400), "entero a los 1,40 s (VA-13)");

            GameBootstrap boot = GameBootstrap.Current!;
            VictoryLineSlot slot = boot.View!.VictorySlot;
            Assert.That(probe.LastSince, Is.GreaterThanOrEqualTo(1600f), "el proveedor recibe el tiempo de la victoria");
            Assert.That(slot.ContentAlpha, Is.EqualTo(1f), "a los 1,60 s, el contenido entero");
        }

        [UnityTest]
        public IEnumerator TheContentFadesInFrom140s_OrCutsWithReducedMotion()
        {
            foreach (bool reduce in new[] { false, true })
            {
                yield return HudSetup.LoadAtIntro(1);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                view.RegisterVictoryLineProvider(new SlotProbe());
                boot.Flow!.ReduceMotion = reduce;
                VictorySetup.WinToVictoryScreen(boot);
                VictoryLineSlot slot = view.VictorySlot;
                Assert.That(slot.SinceShownMs, Is.EqualTo(0f));
                PlayModeHelpers.TickExact(boot, 1399);
                Assert.That(slot.ContentAlpha, Is.EqualTo(0f), "nada antes de los 1,40 s");
                PlayModeHelpers.TickExact(boot, 101);
                if (reduce)
                {
                    Assert.That(slot.ContentAlpha, Is.EqualTo(1f), "con Reducir movimiento, sin fundido");
                }
                else
                {
                    Assert.That(slot.ContentAlpha, Is.EqualTo(0.5f).Within(0.01f), "a mitad del fundido de 200 ms");
                    PlayModeHelpers.TickExact(boot, 100);
                    Assert.That(slot.ContentAlpha, Is.EqualTo(1f));
                }

                Assert.That(slot.DismissedMs, Is.LessThan(0f));
                view.NextButton.onClick.Invoke();
                Assert.That(slot.DismissedMs, Is.GreaterThanOrEqualTo(1500f), "Siguiente cierra la victoria: el punto común de river_fact_shown");
            }
        }

        [UnityTest]
        public IEnumerator SlothBox_IsTheFreeBandBetweenHeaderAndCuias_AfterShiftToClear()
        {
            yield return HudSetup.LoadAtIntro(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            view.SlothVoice = true;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 400); // el tablero ya llegó a donde lo lleva ShiftToClear

            // Cima del cuerpo de cada cuia (su pie menos su alto) con la elevación, en dp desde arriba del lienzo, con el tablero
            // donde está ahora (el contenedor sube lo que desplaza ShiftToClear).
            float shift = view.BoardContainer.anchoredPosition.y;
            Assert.That(shift, Is.GreaterThanOrEqualTo(0f));
            float highest = float.MaxValue;
            for (int i = 0; i < view.Views.Length; i++)
            {
                CuiaView cuia = view.Views[i];
                highest = Mathf.Min(highest, view.Layout!.FootY[i] - (cuia.HeightU * cuia.Unit) - CuiaView.LiftDp - shift);
            }

            TestContext.WriteLine("caja de TiTino en el 40: " + view.SlothBandTopDp + " → " + view.SlothBandBottomDp + " dp (" + view.SlothBoxDp + ")");
            Assert.That(view.SlothBandTopDp, Is.GreaterThan(0f));
            Assert.That(view.SlothBandBottomDp, Is.EqualTo(Mathf.Max(view.SlothBandTopDp, highest)).Within(0.5f), "la cima de las cuias con el tablero ya desplazado");
            Assert.That(view.SlothCouldSpeak, Is.EqualTo(view.SlothBoxDp >= GameScreen.SlothSpeakBoxDp), "el 40 es un hito: habla si su caja mide ≥ 104 dp");
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.False, "con ArrivalEverywhere (hasta TI6a) los hitos son victorias normales");
            Assert.That(view.CurrentVictoryLine.SlothBig, Is.True, "el 40 es un hito");
        }

        // Victoria del 1 con o sin proveedor: milisegundo (desde la victoria, en pasos de 1 ms) en que Siguiente se habilita, en
        // que empieza a verse y en que se ve entero.
        private static IEnumerator MeasureNext(SlotProbe? probe, int[] times)
        {
            yield return HudSetup.LoadAtIntro(1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            if (probe != null)
            {
                view.RegisterVictoryLineProvider(probe);
            }

            VictorySetup.WinToVictoryScreen(boot);
            times[0] = times[1] = times[2] = -1;
            for (int ms = 0; ms <= 1700; ms++)
            {
                if (times[0] < 0 && view.NextButton.interactable)
                {
                    times[0] = ms;
                }

                if (times[1] < 0 && view.NextAlpha > 0f)
                {
                    times[1] = ms;
                }

                if (times[2] < 0 && view.NextAlpha >= 1f)
                {
                    times[2] = ms;
                }

                PlayModeHelpers.TickExact(boot, 1);
            }

            Assert.That(times[0], Is.GreaterThanOrEqualTo(0));
            Assert.That(times[2], Is.GreaterThanOrEqualTo(0));
            Assert.That(view.VictorySlot.Reserved, Is.EqualTo(probe != null));
        }
    }

    /// <summary>Proveedor de prueba de la ranura: cuenta las llamadas y no dibuja nada.</summary>
    internal sealed class SlotProbe : IVictoryLineProvider
    {
        public bool Active = true;
        public int Layouts;
        public int Shows;
        public int Hides;
        public int LastLevel;
        public float LastWidth;
        public float LastHeight;
        public float LastSince;
        public RectTransform? Content;

        public bool SlotActive => Active;

        public void Layout(RectTransform content, float widthDp, float heightDp)
        {
            Layouts++;
            Content = content;
            LastWidth = widthDp;
            LastHeight = heightDp;
        }

        public bool Show(LevelFlow flow, in VictoryLine line)
        {
            Shows++;
            LastLevel = flow.LevelNumber;
            return true;
        }

        public void Tick(float dtMs, float sinceShownMs)
        {
            LastSince = sinceShownMs;
        }

        public void Hide()
        {
            Hides++;
        }

        public void LayoutIds(List<string> ids, List<RectTransform> rects)
        {
        }
    }
}
