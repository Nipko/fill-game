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
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Ranura del Dato del río de la victoria (02 §6; Q7-A de D-017; arquitectura de la tanda 4, §2; tarea M1.3b) en la escena:
    /// el alto por idioma y escala (cadenas del peor caso de 90 y 117 caracteres) con su caché; sin proveedores no hay reserva y
    /// la victoria es la de siempre; con uno activo, la ranura va bajo «Inicio» con el alto fijo y «Siguiente» queda a la misma
    /// altura en el 1, el 11 (Cosecha doble) y el 39 (chip del nivel siguiente y Cosecha doble), y con el alto del idioma y la
    /// escala de la escena; «Siguiente» se habilita y se ve en el mismo milisegundo con y sin ranura; la ranura no decide los
    /// tiempos ni los toques de sus proveedores: una línea de fruta de prueba entra con «Siguiente» (fundido de 200 ms, sin él
    /// con Reducir movimiento) y una llegada de prueba se ve y recibe toques desde el primer milisegundo; Siguiente e Inicio
    /// cierran la victoria (el punto común de river_fact_shown); y la caja de TiTino es la franja entre la cabecera y la cima
    /// de las cuias tras ShiftToClear.
    /// </summary>
    public sealed class VictorySlotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = true; // la ranura sola, con proveedores de prueba (la llegada real: VictoryArrivalTests)
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            if (Loc.IsConfigured && Loc.Locale != LocLocale.SpanishLatinAmerica)
            {
                LocSetUpFixture.ConfigureEs419();
            }
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
            Assert.That(VictoryLineSlot.ReserveOf(es), Is.EqualTo(es + VictoryLineSlot.RuleGapDp + VictoryLineSlot.RuleDp + VictoryLineSlot.RuleBelowDp));
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
        public IEnumerator TheReserve_IsTheOneOfTheSceneLocaleAndTextScale()
        {
            // Al 130 %, en es-419 y en en-XA: el alto que reserva la ranura en la victoria es el de Loc.Locale y UiBuilder.TextScale
            // (no el de es-419 al 100 %), y Siguiente sube justo eso frente a la misma victoria sin ranura.
            foreach (string locale in new[] { LocLocale.SpanishLatinAmerica, LocLocale.Pseudo })
            {
                float nextWithout = -1f;
                foreach (bool withProvider in new[] { false, true })
                {
                    LaunchOptions.FontScaleOverride = 1.3f;
                    LaunchOptions.LanguageOverride = locale == LocLocale.SpanishLatinAmerica ? null : locale;
                    if (Loc.IsConfigured && Loc.Locale != locale)
                    {
                        Loc.SetLocale(locale);
                    }

                    yield return HudSetup.LoadAtIntro(1);
                    GameBootstrap boot = GameBootstrap.Current!;
                    GameScreen view = boot.View!;
                    Assert.That(Loc.Locale, Is.EqualTo(locale), "el idioma de la escena");
                    Assert.That(UiBuilder.TextScale, Is.EqualTo(1.3f), "la escala de la escena");
                    if (withProvider)
                    {
                        view.RegisterVictoryLineProvider(new SlotProbe());
                    }

                    VictorySetup.WinToVictoryScreen(boot);
                    PlayModeHelpers.Step(boot, 100);
                    VictoryLineSlot slot = view.VictorySlot;
                    if (!withProvider)
                    {
                        Assert.That(slot.ReserveDp, Is.EqualTo(0f));
                        nextWithout = view.NextTopDp;
                        continue;
                    }

                    float expected = view.VictorySlotContentHeight(locale, 1.3f);
                    float es100 = view.VictorySlotContentHeight(LocLocale.SpanishLatinAmerica, 1f);
                    TestContext.WriteLine(locale + " al 130 %: contenido " + slot.ContentDp + " dp (es-419 al 100 %: " + es100 + "), reserva "
                        + slot.ReserveDp + ", Siguiente " + nextWithout + " → " + view.NextTopDp);
                    Assert.That(slot.ContentDp, Is.EqualTo(expected), "el alto de " + locale + " al 130 %");
                    Assert.That(slot.ContentDp, Is.GreaterThan(es100), "no el de es-419 al 100 %");
                    if (locale == LocLocale.Pseudo)
                    {
                        Assert.That(slot.ContentDp, Is.GreaterThan(view.VictorySlotContentHeight(LocLocale.SpanishLatinAmerica, 1.3f)),
                            "en-XA (117 caracteres) mide más que es-419 a la misma escala");
                    }

                    Assert.That(slot.ReserveDp, Is.EqualTo(expected + VictoryLineSlot.RuleGapDp + VictoryLineSlot.RuleDp + VictoryLineSlot.RuleBelowDp));
                    Assert.That(nextWithout - view.NextTopDp, Is.EqualTo(slot.ReserveDp).Within(0.01f), "Siguiente sube lo que mide la reserva");
                }

                LaunchOptions.Reset();
            }
        }

        [UnityTest]
        public IEnumerator NextEnablesInTheSameMillisecond_AndTheFruitLineEntersWithNext()
        {
            int[] without = new int[5];
            int[] with = new int[5];
            yield return MeasureNext(null, without);
            var fruit = new FruitProbe();
            yield return MeasureNext(fruit, with);
            TestContext.WriteLine("Siguiente (ms desde la victoria): habilitado " + with[0] + ", visible " + with[1] + ", entero " + with[2]
                + "; línea de fruta: visible " + with[3] + ", entera " + with[4]);
            Assert.That(with[0], Is.EqualTo(without[0]), "Siguiente se habilita en el mismo milisegundo");
            Assert.That(with[1], Is.EqualTo(without[1]), "empieza a verse en el mismo milisegundo");
            Assert.That(with[2], Is.EqualTo(without[2]), "entero en el mismo milisegundo");
            Assert.That(with[1], Is.EqualTo((int)GameScreen.VictoryButtonsInMs + 1), "Siguiente entra a los 600 ms de la pantalla (fila 1,40 de 07 §5.4)");

            // La línea de fruta sale con los botones (07 §5.4, fila 1,40; 02 §6), no 1,40 s después de la subida del panel.
            Assert.That(with[3], Is.EqualTo(with[1]), "la línea de fruta empieza a verse con Siguiente");
            Assert.That(with[4] - with[3], Is.EqualTo(VictoryLineSlot.FruitFadeMs).Within(2f), "con un fundido de 200 ms");
            Assert.That(fruit.LastSince, Is.GreaterThanOrEqualTo(1600f), "el proveedor recibe el tiempo de la victoria");
        }

        [UnityTest]
        public IEnumerator TheFruitLineFadesInWithNext_OrCutsWithReducedMotion()
        {
            foreach (bool reduce in new[] { false, true })
            {
                yield return HudSetup.LoadAtIntro(1);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                var fruit = new FruitProbe();
                view.RegisterVictoryLineProvider(fruit);
                boot.Flow!.ReduceMotion = reduce;
                VictorySetup.WinToVictoryScreen(boot);
                VictoryLineSlot slot = view.VictorySlot;
                Assert.That(slot.SinceShownMs, Is.EqualTo(0f));
                PlayModeHelpers.TickExact(boot, (int)VictoryLineSlot.FruitRevealMs);
                Assert.That(fruit.Alpha, Is.EqualTo(0f), "nada antes de Siguiente");
                Assert.That(view.NextAlpha, Is.EqualTo(0f));
                PlayModeHelpers.TickExact(boot, 100);
                Assert.That(view.NextAlpha, Is.GreaterThan(0f));
                if (reduce)
                {
                    Assert.That(fruit.Alpha, Is.EqualTo(1f), "con Reducir movimiento, sin fundido");
                }
                else
                {
                    Assert.That(fruit.Alpha, Is.EqualTo(0.5f).Within(0.01f), "a mitad del fundido de 200 ms");
                    PlayModeHelpers.TickExact(boot, 100);
                    Assert.That(fruit.Alpha, Is.EqualTo(1f));
                }

                Assert.That(slot.DismissedMs, Is.LessThan(0f));
                float since = slot.SinceShownMs;
                view.NextButton.onClick.Invoke();
                Assert.That(slot.DismissedMs, Is.EqualTo(since), "Siguiente cierra la victoria: el punto común de river_fact_shown");
            }
        }

        [UnityTest]
        public IEnumerator AProviderShownFromTheStart_IsSeenAndTouchedFromTheFirstMillisecond()
        {
            // Una llegada de prueba (02 §6, fila Llegada: su franja empieza con la subida del panel y un toque la completa): la
            // ranura no la oculta ni le quita los toques.
            yield return HudSetup.LoadAtIntro(1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            var arrival = new ArrivalProbe();
            view.RegisterVictoryLineProvider(arrival);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.TickExact(boot, 1);

            Button strip = arrival.Strip!;
            Assert.That(strip.gameObject.activeInHierarchy, Is.True);
            float alpha = 1f;
            for (Transform? t = strip.transform; t != null; t = t.parent)
            {
                foreach (CanvasGroup group in t.GetComponents<CanvasGroup>())
                {
                    alpha *= group.alpha;
                    Assert.That(group.blocksRaycasts, Is.True, "ningún CanvasGroup le quita los toques (" + t.name + ")");
                    Assert.That(group.interactable, Is.True, t.name);
                }
            }

            Assert.That(alpha, Is.EqualTo(1f), "se ve entera en el primer milisegundo");
            Assert.That(strip.IsInteractable(), Is.True);
            Assert.That(((Graphic)strip.targetGraphic).raycastTarget, Is.True);
            strip.onClick.Invoke();
            Assert.That(arrival.Taps, Is.EqualTo(1), "un toque la completa");
            Assert.That(view.VictorySlot.Shown, Is.True);
        }

        [UnityTest]
        public IEnumerator HomeClosesTheVictory_AtTheCommonPointOfRiverFactShown()
        {
            yield return HudSetup.LoadAtIntro(1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            view.RegisterVictoryLineProvider(new SlotProbe());
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.TickExact(boot, 900);
            VictoryLineSlot slot = view.VictorySlot;
            Assert.That(slot.DismissedMs, Is.LessThan(0f));
            Assert.That(view.HomeButton.interactable, Is.True);
            float since = slot.SinceShownMs;
            view.HomeButton.onClick.Invoke();
            Assert.That(slot.DismissedMs, Is.EqualTo(since), "Inicio cierra la victoria: dismissed_ms del punto común");
            Assert.That(slot.DismissedMs, Is.GreaterThanOrEqualTo(900f));
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
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

        [Test]
        public void SlothCanSpeak_InAHito_IsTheOneOfItsBox_AndInThe10kPlus1_OnlyIfTheSlothSpokeInTheHito()
        {
            // Arquitectura de la tanda 4, §2: en un hito, voz ∧ caja ≥ 104 dp; T6-A: el dato desplazado sale en el 10k+1 una vez.
            Assert.That(GameScreen.SlothCanSpeakFor(40, true, -1), Is.True, "hito con voz y caja");
            Assert.That(GameScreen.SlothCanSpeakFor(40, false, -1), Is.False, "hito sin voz o con la caja pequeña");
            Assert.That(GameScreen.SlothCanSpeakFor(80, true, 40), Is.True, "otro hito: cuenta su caja, no lo anterior");
            Assert.That(GameScreen.SlothCanSpeakFor(80, false, 79), Is.False);
            Assert.That(GameScreen.SlothCanSpeakFor(41, false, 40), Is.True, "41 tras hablar en el 40: sale el dato desplazado");
            Assert.That(GameScreen.SlothCanSpeakFor(81, false, 80), Is.True);
            Assert.That(GameScreen.SlothCanSpeakFor(42, false, 40), Is.False, "solo en el 10k+1: una vez y solo una");
            Assert.That(GameScreen.SlothCanSpeakFor(41, false, -1), Is.False, "TiTino no habló en el 40");
            Assert.That(GameScreen.SlothCanSpeakFor(41, true, -1), Is.False, "fuera de un hito no cuenta la caja");
            Assert.That(GameScreen.SlothCanSpeakFor(39, true, -1), Is.False, "el 39 no es un hito");
            Assert.That(GameScreen.SlothCanSpeakFor(1, false, 0), Is.False, "spokeAt = 0: nunca habló");
        }

        [UnityTest]
        public IEnumerator SlothCannotSpeak_WithoutVoice_InAHito()
        {
            yield return HudSetup.LoadAtIntro(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            view.SlothVoice = false;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 400);
            TestContext.WriteLine("caja de TiTino en el 40 sin voz: " + view.SlothBoxDp + " dp");
            Assert.That(view.SlothCouldSpeak, Is.False, "sin voz (ui_sloth_enabled ∧ settings.sloth_messages) nunca habla");
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.False);
            Assert.That(view.CurrentVictoryLine.SlothBig, Is.True, "el 40 sigue siendo un hito");
        }

        [UnityTest]
        public IEnumerator SlothCannotSpeak_OutsideAHito_EvenWithVoice()
        {
            yield return HudSetup.LoadAtIntro(39);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            view.SlothVoice = true;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 400);
            TestContext.WriteLine("caja de TiTino en el 39 con voz: " + view.SlothBoxDp + " dp");
            Assert.That(view.SlothCouldSpeak, Is.False, "el 39 no es un hito ni el 10k+1 de uno en que TiTino hablara");
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.False);
            Assert.That(view.CurrentVictoryLine.SlothBig, Is.False);
        }

        // Victoria del 1 con o sin la línea de fruta de prueba: milisegundo (desde la victoria, en pasos de 1 ms) en que Siguiente
        // se habilita, en que empieza a verse y en que se ve entero, y en que la línea empieza a verse y se ve entera.
        private static IEnumerator MeasureNext(FruitProbe? fruit, int[] times)
        {
            yield return HudSetup.LoadAtIntro(1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            if (fruit != null)
            {
                view.RegisterVictoryLineProvider(fruit);
            }

            VictorySetup.WinToVictoryScreen(boot);
            for (int i = 0; i < times.Length; i++)
            {
                times[i] = -1;
            }

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

                if (fruit != null && times[3] < 0 && fruit.Alpha > 0f)
                {
                    times[3] = ms;
                }

                if (fruit != null && times[4] < 0 && fruit.Alpha >= 1f)
                {
                    times[4] = ms;
                }

                PlayModeHelpers.TickExact(boot, 1);
            }

            Assert.That(times[0], Is.GreaterThanOrEqualTo(0));
            Assert.That(times[2], Is.GreaterThanOrEqualTo(0));
            Assert.That(view.VictorySlot.Reserved, Is.EqualTo(fruit != null));
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

    /// <summary>
    /// Línea de fruta de prueba (como la de F3): su propio CanvasGroup con la regla de 02 §6, fila «Dato del río»
    /// (<see cref="VictoryLineSlot.FruitLineAlpha"/>: entra con Siguiente con un fundido de 200 ms; sin él con Reducir movimiento).
    /// </summary>
    internal sealed class FruitProbe : IVictoryLineProvider
    {
        private CanvasGroup? _group;
        private bool _reduceMotion;

        public float LastSince;

        public float Alpha => _group != null ? _group.alpha : 0f;

        public bool SlotActive => true;

        public void Layout(RectTransform content, float widthDp, float heightDp)
        {
            if (_group == null)
            {
                RectTransform line = UiBuilder.Rect("FruitProbe", content);
                UiBuilder.Place(line, 0f, 0f, widthDp, heightDp);
                _group = line.gameObject.AddComponent<CanvasGroup>();
                _group.alpha = 0f;
            }
        }

        public bool Show(LevelFlow flow, in VictoryLine line)
        {
            _reduceMotion = flow.ReduceMotion;
            _group!.alpha = 0f;
            return true;
        }

        public void Tick(float dtMs, float sinceShownMs)
        {
            LastSince = sinceShownMs;
            _group!.alpha = VictoryLineSlot.FruitLineAlpha(sinceShownMs, _reduceMotion);
        }

        public void Hide()
        {
            if (_group != null)
            {
                _group.alpha = 0f;
            }
        }

        public void LayoutIds(List<string> ids, List<RectTransform> rects)
        {
        }
    }

    /// <summary>
    /// Llegada de prueba (como la de M1.6): una franja que se ve desde que sale la victoria y que un toque completa (02 §6, fila
    /// Llegada).
    /// </summary>
    internal sealed class ArrivalProbe : IVictoryLineProvider
    {
        public Button? Strip;
        public int Taps;

        public bool SlotActive => true;

        public void Layout(RectTransform content, float widthDp, float heightDp)
        {
            if (Strip == null)
            {
                Strip = UiBuilder.BareButton("ArrivalProbe", content, () => Taps++);
            }

            UiBuilder.Place((RectTransform)Strip.transform, 0f, 0f, widthDp, heightDp);
        }

        public bool Show(LevelFlow flow, in VictoryLine line)
        {
            Strip!.gameObject.SetActive(true);
            return true;
        }

        public void Tick(float dtMs, float sinceShownMs)
        {
        }

        public void Hide()
        {
            if (Strip != null)
            {
                Strip.gameObject.SetActive(false);
            }
        }

        public void LayoutIds(List<string> ids, List<RectTransform> rects)
        {
            ids.Add("arrival_probe");
            rects.Add((RectTransform)Strip!.transform);
        }
    }
}
