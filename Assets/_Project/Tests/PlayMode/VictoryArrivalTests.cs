#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Llegada a la parada del Mapa del río en la ranura del Dato del río (02 §6, filas Llegada y «Dato del río»; D-013 punto 8;
    /// T6-A; P-M7 a; tarea M1.6) en la escena, con el catálogo real de paradas y los textos de <c>facts.csv</c>:
    /// <list type="bullet">
    /// <item>el 20: agua clara, canoa, mini cuia, «Alter do Chão · Brasil · río Tapajós» y el dato con el retrato; el 80, con la
    /// bienvenida de Colombia; el 10 y el 21, sin llegada;</item>
    /// <item>las políticas del árbitro: Canon (llegada con dato), SingleKey (solo la placa del lugar y la línea de fruta) y, con
    /// SlothHito activado en el test, el 40 sin dato y el 41 con el dato desplazado;</item>
    /// <item>tiempos: 1.200 ms tras la subida del panel, un toque la completa, Reducir movimiento con un fundido de 150 ms, la
    /// Cosecha doble no la reinicia ni espera por ella, y «Siguiente» se habilita en el mismo milisegundo y queda a la misma
    /// altura que sin llegada;</item>
    /// <item>sondas: <c>stop_arrive</c> una vez por llegada (saltada si se toca o se cierra antes), <c>AF_LAYOUT arrival</c> al
    /// empezar y <c>river_fact_shown</c> desde el punto común de la ranura al cerrar la victoria con Siguiente o Inicio;</item>
    /// <item>sin recortes: lo que se ve cabe en la ranura y cada línea en su caja, a 360 × 780 y 360 × 640, al 100 % y al 130 %.</item>
    /// </list>
    /// </summary>
    public sealed class VictoryArrivalTests
    {
        private const string AlterFact = "map.stop.alter_do_chao.dato_01";
        private const string EncuentroFact = "map.stop.manaos.dato_01";

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level20_ClearWater_Canoe_MiniCuia_ThePlaceAndTheFactWithThePortrait()
        {
            yield return Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ArrivalStrip strip = view.Arrival!;
            VictoryLine line = view.CurrentVictoryLine;
            Assert.That(line.Arrival && line.ArrivalFact, Is.True, "Canon: llegada con dato");
            Assert.That(line.ArrivalFactKey, Is.EqualTo(AlterFact));
            Assert.That(line.FruitKey, Is.Null, "la línea de fruta del 20 queda en la ficha");
            Assert.That(strip.Kind, Is.EqualTo(ArrivalKind.Arrival));
            Assert.That(strip.Stop!.Id, Is.EqualTo("alter_do_chao"));
            Assert.That(strip.Stop.N, Is.EqualTo(2));
            Assert.That(view.VictorySlot.Shown, Is.True);
            Assert.That(strip.Started, Is.False, "aún sube el panel");

            PlayModeHelpers.Step(boot, 1600);
            Assert.That(strip.Started && strip.Completed, Is.True);
            Assert.That(strip.CompletedBy, Is.EqualTo("time"));
            Assert.That(strip.Skipped, Is.False);
            Assert.That(strip.PlaceText, Is.EqualTo("Alter do Chão · Brasil · río Tapajós"));
            Assert.That(strip.FactText, Is.EqualTo(Loc.Fact(AlterFact)));
            Assert.That(strip.FactText, Does.StartWith("Cuando el río baja"));
            Assert.That(strip.Welcome, Is.False, "el 20 no cambia de país");
            Assert.That(strip.WelcomeLabel!.gameObject.activeSelf, Is.False);
            Assert.That(strip.ShownFactKey, Is.EqualTo(AlterFact));
            Assert.That(strip.SampleColor, Is.EqualTo(Palette.WaterClear), "agua clara del Tapajós (sample clear)");
            Assert.That(strip.WaterFill, Is.EqualTo(1f), "el tramo, lleno");
            Assert.That(strip.SampleFill, Is.EqualTo(1f), "la mini cuia, llena");
            Assert.That(strip.CanoeProgress, Is.EqualTo(1f), "la canoa, en la parada");
            Assert.That(strip.Canoe!.gameObject.activeInHierarchy, Is.True);
            Assert.That(strip.MiniCuia!.gameObject.activeInHierarchy, Is.True);
            Assert.That(strip.Portrait!.gameObject.activeInHierarchy && strip.Portrait.sprite != null, Is.True, "el retrato de TiTino");
            Assert.That(strip.Portrait.rectTransform.rect.width, Is.EqualTo(ArrivalStrip.PortraitDp));
            Assert.That(strip.PlaceAlpha, Is.EqualTo(1f));
            Assert.That(strip.FactAlpha, Is.EqualTo(1f));
            Assert.That(strip.Alpha, Is.EqualTo(1f));
            Assert.That(strip.FactLabel!.fontSize, Is.EqualTo(ArrivalStrip.FactSp * UiBuilder.TextScale), "dato a 14 sp");
            Assert.That(view.StopArriveCount, Is.EqualTo(1));
            Assert.That(view.LastStopArriveSkipped, Is.False, "vista entera");
            Assert.That(view.ArrivalLayoutCount, Is.EqualTo(1), "AF_LAYOUT arrival al empezar");
            AssertFits(view, "20");
        }

        [UnityTest]
        public IEnumerator Levels10And21_HaveNoArrival_ButTheSlotKeepsItsHeight()
        {
            foreach (int level in new[] { 10, 21 })
            {
                yield return Win(level);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                PlayModeHelpers.Step(boot, 1600);
                Assert.That(view.Arrival!.Kind, Is.EqualTo(ArrivalKind.None), "nivel " + level);
                Assert.That(view.CurrentVictoryLine.Arrival, Is.False, "nivel " + level);
                Assert.That(view.VictorySlot.Reserved, Is.True, "la ranura existe en todas las victorias (Q7-A)");
                Assert.That(view.VictorySlot.Shown, Is.False);
                Assert.That(view.StopArriveCount, Is.EqualTo(0));
                Assert.That(view.ArrivalLayoutCount, Is.EqualTo(0));
                view.NextButton.onClick.Invoke();
                Assert.That(view.RiverFactShownCount, Is.EqualTo(0), "sin dato, sin river_fact_shown");
            }
        }

        [UnityTest]
        public IEnumerator Level80_WelcomesToColombia()
        {
            yield return Win(80);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            ArrivalStrip strip = view.Arrival!;
            Assert.That(strip.Kind, Is.EqualTo(ArrivalKind.Arrival));
            Assert.That(strip.Stop!.Id, Is.EqualTo("leticia"));
            Assert.That(strip.Welcome, Is.True, "cambio de país (P-M7 a)");
            Assert.That(strip.WelcomeText, Is.EqualTo("Colombia te da la bienvenida"));
            Assert.That(strip.WelcomeLabel!.gameObject.activeSelf, Is.True);
            Assert.That(strip.PlaceText, Is.EqualTo("Leticia · Colombia · río Amazonas"));
            Assert.That(strip.ShownFactKey, Is.EqualTo("map.country.co.dato_01"));
            Assert.That(strip.SampleColor, Is.EqualTo(Palette.WaterWhite));
            AssertFits(view, "80");
        }

        [UnityTest]
        public IEnumerator SlothHito_40WithoutFact_41WithTheDeferredFact()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.HitoPolicyOverride = HitoLinePolicy.SlothHito;
            view.SlothBoxCanSpeakOverride = true;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1600);

            VictoryLine line = view.CurrentVictoryLine;
            Assert.That(line.SlothSpeaks, Is.True, "el 40 es un hito: habla TiTino");
            Assert.That(line.Arrival, Is.True);
            Assert.That(line.ArrivalFact, Is.False, "la llegada va sin dato");
            ArrivalStrip strip = view.Arrival!;
            Assert.That(strip.Kind, Is.EqualTo(ArrivalKind.Place), "solo la placa del lugar");
            Assert.That(strip.PlaceText, Is.EqualTo("Encuentro de las Aguas · Brasil · río Negro y Solimões"));
            Assert.That(strip.ShownFactKey, Is.Null);
            Assert.That(strip.FactLabel!.gameObject.activeSelf, Is.False);
            Assert.That(strip.Portrait!.gameObject.activeSelf, Is.False, "el retrato va con el dato");
            Assert.That(view.StopArriveCount, Is.EqualTo(1));

            view.NextButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(0), "en el 40 no salió ningún dato");
            for (int t = 0; t < 5000 && !(boot.Flow!.LevelNumber == 41 && VictorySetup.InNewLevel(boot.Flow.Phase)); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(41));
            VictorySetup.WinToVictoryScreen(boot);
            line = view.CurrentVictoryLine;
            Assert.That(line.DeferredFact, Is.True, "el 41 lleva el dato desplazado (T6-A)");
            Assert.That(line.ArrivalFactKey, Is.EqualTo(EncuentroFact));
            Assert.That(line.FruitKey, Is.Null);
            Assert.That(strip.Kind, Is.EqualTo(ArrivalKind.Deferred), "solo la placa del dato con el retrato");
            Assert.That(strip.Stop, Is.Null);
            Assert.That(strip.FactText, Is.EqualTo(Loc.Fact(EncuentroFact)));
            Assert.That(strip.Canoe!.gameObject.activeSelf, Is.False, "sin la fila del agua");
            Assert.That(strip.Alpha, Is.EqualTo(0f), "entra con Siguiente, como la línea de fruta");
            PlayModeHelpers.TickExact(boot, (int)VictoryLineSlot.FruitRevealMs + (int)VictoryLineSlot.FruitFadeMs + 10);
            Assert.That(strip.Alpha, Is.EqualTo(1f));
            Assert.That(strip.Portrait.gameObject.activeSelf, Is.True);
            AssertFits(view, "41");

            float since = view.VictorySlot.SinceShownMs;
            view.NextButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(1));
            Assert.That(view.LastRiverFactId, Is.EqualTo(EncuentroFact));
            Assert.That(view.LastRiverFactDismissedMs, Is.EqualTo(since));
            Assert.That(view.StopArriveCount, Is.EqualTo(1), "el dato desplazado no es una llegada");
        }

        [UnityTest]
        public IEnumerator Policies_CanonAndArrivalEverywhere_GiveTheFact_SingleKeyOnlyThePlace()
        {
            // Canon y ArrivalEverywhere (los defaults): el 40 es una victoria normal con su dato.
            yield return Win(40);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.False);
            Assert.That(view.Arrival!.Kind, Is.EqualTo(ArrivalKind.Arrival));
            Assert.That(view.Arrival.ShownFactKey, Is.EqualTo(EncuentroFact));

            // SlothHito sin sitio (o sin voz) para TiTino: el hito vuelve a ser una victoria normal.
            yield return Win(40, v =>
            {
                v.HitoPolicyOverride = HitoLinePolicy.SlothHito;
                v.SlothBoxCanSpeakOverride = false;
            });
            view = GameBootstrap.Current!.View!;
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.False);
            Assert.That(view.Arrival!.Kind, Is.EqualTo(ArrivalKind.Arrival));

            // SingleKey: la llegada solo con la placa del lugar y la línea es la de fruta.
            yield return Win(20, v =>
            {
                v.RiverPolicyOverride = RiverFactPolicy.SingleKey;
                v.FruitKeyOf = level => "fruit.test.line_" + level;
            });
            view = GameBootstrap.Current!.View!;
            Assert.That(view.CurrentVictoryLine.FruitKey, Is.EqualTo("fruit.test.line_20"));
            Assert.That(view.Arrival!.Kind, Is.EqualTo(ArrivalKind.Place));
            Assert.That(view.Arrival.PlaceText, Is.EqualTo("Alter do Chão · Brasil · río Tapajós"));
            Assert.That(view.Arrival.ShownFactKey, Is.Null);
        }

        [UnityTest]
        public IEnumerator Timing_StartsWhenThePanelIsUp_LastsItsTime_AndATapCompletesIt()
        {
            yield return Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ArrivalStrip strip = view.Arrival!;
            PlayModeHelpers.TickExact(boot, (int)GameScreen.VictoryPanelUpMs - 1);
            Assert.That(strip.Started, Is.False);
            Assert.That(strip.CanoeProgress, Is.EqualTo(0f));
            Assert.That(strip.WaterFill, Is.EqualTo(ArrivalStrip.LegFromFill), "el tramo llega lleno a 9/10");
            PlayModeHelpers.TickExact(boot, 2);
            Assert.That(strip.Started, Is.True, "empieza al terminar de subir el panel");
            PlayModeHelpers.TickExact(boot, 300);
            Assert.That(strip.WaterFill, Is.GreaterThan(ArrivalStrip.LegFromFill).And.LessThan(1f), "el agua termina de llenar el tramo");
            Assert.That(strip.CanoeProgress, Is.GreaterThan(0f).And.LessThan(1f), "la canoa va llegando");
            Assert.That(strip.SampleFill, Is.EqualTo(0f));
            Assert.That(strip.FactAlpha, Is.EqualTo(0f));
            Assert.That(strip.Completed, Is.False);

            // Un toque la completa (02 §6, fila Llegada): todo en su sitio, el texto se queda, y la llegada cuenta como saltada.
            strip.TapArea!.onClick.Invoke();
            Assert.That(strip.Completed, Is.True);
            Assert.That(strip.CompletedBy, Is.EqualTo("tap"));
            Assert.That(strip.CanoeProgress, Is.EqualTo(1f));
            Assert.That(strip.SampleFill, Is.EqualTo(1f));
            Assert.That(strip.FactAlpha, Is.EqualTo(1f));
            Assert.That(view.StopArriveCount, Is.EqualTo(1));
            Assert.That(view.LastStopArriveSkipped, Is.True);
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(strip.FactAlpha, Is.EqualTo(1f), "el texto se queda hasta cerrar la victoria (D-011)");
            Assert.That(view.StopArriveCount, Is.EqualTo(1), "una vez por llegada");

            // Sin tocar: termina a los 1.200 ms de la franja.
            yield return Win(20);
            boot = GameBootstrap.Current!;
            strip = boot.View!.Arrival!;
            PlayModeHelpers.TickExact(boot, (int)GameScreen.VictoryPanelUpMs + MapConfig.ArrivalStripMs - 5);
            Assert.That(strip.Completed, Is.False);
            Assert.That(strip.SampleFill, Is.GreaterThan(0.9f).And.LessThan(1f), "la mini cuia se llena en los últimos 600 ms");
            PlayModeHelpers.TickExact(boot, 10);
            Assert.That(strip.Completed, Is.True);
            Assert.That(strip.CompletedBy, Is.EqualTo("time"));
        }

        [UnityTest]
        public IEnumerator ReducedMotion_TheFinalStateWithA150msFade()
        {
            yield return Win(20, v => GameBootstrap.Current!.Flow!.ReduceMotion = true);
            GameBootstrap boot = GameBootstrap.Current!;
            ArrivalStrip strip = boot.View!.Arrival!;
            Assert.That(strip.ReduceMotion, Is.True);
            PlayModeHelpers.TickExact(boot, (int)GameScreen.VictoryPanelUpMs - 1);
            Assert.That(strip.Alpha, Is.EqualTo(0f));
            PlayModeHelpers.TickExact(boot, 1 + (MapConfig.ReducedMotionFadeMs / 2));
            Assert.That(strip.Alpha, Is.EqualTo(0.5f).Within(0.02f), "a mitad del fundido de 150 ms");
            Assert.That(strip.CanoeProgress, Is.EqualTo(1f), "sin viaje: la canoa ya está en la parada");
            Assert.That(strip.SampleFill, Is.EqualTo(1f), "sin llenado animado");
            Assert.That(strip.FactAlpha, Is.EqualTo(1f));
            PlayModeHelpers.TickExact(boot, MapConfig.ReducedMotionFadeMs / 2);
            Assert.That(strip.Alpha, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Next_EnablesInTheSameMillisecond_AndStaysAtTheSameHeight_With19_20And21()
        {
            int[] without = new int[3];
            int[] with = new int[3];
            GameScreen.ArrivalDisabledForTests = true;
            yield return MeasureNext(20, without);
            GameScreen.ArrivalDisabledForTests = false;
            yield return MeasureNext(20, with);
            TestContext.WriteLine("Siguiente (ms desde la victoria) sin llegada: " + without[0] + "/" + without[1] + "/" + without[2] + "; con: "
                + with[0] + "/" + with[1] + "/" + with[2]);
            Assert.That(with[0], Is.EqualTo(without[0]), "se habilita en el mismo milisegundo");
            Assert.That(with[1], Is.EqualTo(without[1]), "empieza a verse en el mismo milisegundo");
            Assert.That(with[2], Is.EqualTo(without[2]), "entero en el mismo milisegundo");

            var nexts = new List<float>();
            var panels = new List<float>();
            foreach (int level in new[] { 19, 20, 21 })
            {
                yield return Win(level);
                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.Step(boot, 1600);
                nexts.Add(boot.View!.NextTopDp);
                panels.Add(boot.View.VictoryPanelBottomDp - boot.View.HomeBottomDp);
                Assert.That(boot.View.VictorySlot.ContentDp, Is.EqualTo(boot.View.VictorySlotHeight(UiBuilder.TextScale)));
            }

            Assert.That(nexts[1], Is.EqualTo(nexts[0]).Within(0.01f), "Siguiente, igual en el 19 y en el 20");
            Assert.That(nexts[2], Is.EqualTo(nexts[0]).Within(0.01f), "e igual en el 21");
            Assert.That(panels[1], Is.EqualTo(panels[0]).Within(0.01f), "la misma ranura bajo Inicio");
        }

        [UnityTest]
        public IEnumerator DoubleHarvest_NeitherRestartsNorWaitsForTheArrival()
        {
            // Llegada vista entera y después la Cosecha doble: la franja sigue completa tras recolocar la victoria y el avance solo
            // llega cuando siempre (D-011, punto 4), con river_fact_shown en su AD_GATE.
            yield return Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ArrivalStrip strip = view.Arrival!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(view.DoubleHarvestShown, Is.True, "desde el 11, con video");
            Assert.That(strip.Completed, Is.True);
            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(view.AutoNextPending, Is.True);
            Assert.That(strip.Kind, Is.EqualTo(ArrivalKind.Arrival), "la recolocación no la reinicia");
            Assert.That(strip.Completed && strip.FactAlpha >= 1f && strip.CanoeProgress >= 1f, Is.True);
            AssertFits(view, "20 tras la Cosecha doble");
            PlayModeHelpers.Step(boot, 1116);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(21), "N + 1 a su hora, sin esperar a la llegada");
            Assert.That(view.RiverFactShownCount, Is.EqualTo(1));
            Assert.That(view.LastRiverFactId, Is.EqualTo(AlterFact));
            Assert.That(view.StopArriveCount, Is.EqualTo(1));

            // Cosecha doble a mitad de la franja: el avance llega igual y la llegada queda saltada al cerrarse la victoria.
            yield return Win(20);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            strip = view.Arrival!;
            PlayModeHelpers.TickExact(boot, 500);
            Assert.That(strip.Started && !strip.Completed, Is.True);
            view.DoubleHarvestButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 1116);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(21), "la franja no retrasa la Cosecha doble");
            Assert.That(view.StopArriveCount, Is.EqualTo(1), "una llegada, terminada sola o al cerrarse la victoria");
        }

        [UnityTest]
        public IEnumerator RiverFactShown_FromTheCommonPoint_WithNextOrHome_OncePerVictory()
        {
            yield return Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TickExact(boot, 900);
            Assert.That(view.RiverFactShownCount, Is.EqualTo(0), "solo al cerrar la victoria");
            float since = view.VictorySlot.SinceShownMs;
            view.HomeButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(1));
            Assert.That(view.LastRiverFactId, Is.EqualTo(AlterFact), "fact_id = la clave del dato");
            Assert.That(view.LastRiverFactDismissedMs, Is.EqualTo(since));
            Assert.That(view.StopArriveCount, Is.EqualTo(1), "cerrada a mitad: stop_arrive saltado");
            Assert.That(view.LastStopArriveSkipped, Is.True);

            yield return Win(20);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            view.NextButton.onClick.Invoke();
            view.NextButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(1), "una vez por victoria");
            Assert.That(view.LastRiverFactDismissedMs, Is.GreaterThanOrEqualTo(1600f));

            // Las líneas de la sonda (solo claves e identificadores).
            var probe = new DevProbe();
            string? fact = probe.FormatRiverFactShown(20, AlterFact, 1234.4f);
            Assert.That(fact, Does.StartWith("AF_STATE {\"event\":\"river_fact_shown\"").And.Contain("\"level\":20")
                .And.Contain("\"fact_id\":\"" + AlterFact + "\"").And.EndWith("\"dismissed_ms\":1234}"));
            RiverStop stop = boot.RiverStops!.At(2)!;
            Assert.That(probe.FormatStopArrive(20, stop, false), Does.Contain("\"event\":\"stop_arrive\"").And.Contain("\"stop_id\":\"alter_do_chao\"")
                .And.Contain("\"stop\":2").And.Contain("\"world\":2").And.Contain("\"level\":20").And.Contain("\"view\":\"victory\"")
                .And.EndWith("\"skipped\":false}"));
            string? arrival = probe.FormatArrival("show", view.Arrival!, view.VictorySlot.ContentDp);
            Assert.That(arrival, Does.StartWith("AF_STATE {\"event\":\"arrival\"").And.Contain("\"phase\":\"show\"").And.Contain("\"kind\":\"arrival\"")
                .And.Contain("\"stop_id\":\"alter_do_chao\"").And.Contain("\"fact\":\"" + AlterFact + "\""));
        }

        [UnityTest]
        public IEnumerator NoClipping_At360x640_AndAt130Percent()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    foreach (int level in new[] { 20, 80 })
                    {
                        if (small)
                        {
                            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
                        }

                        LaunchOptions.FontScaleOverride = scale;
                        yield return Win(level);
                        GameBootstrap boot = GameBootstrap.Current!;
                        GameScreen view = boot.View!;
                        PlayModeHelpers.Step(boot, 1600);
                        string what = level + " a " + (small ? "640" : "780") + " al " + Mathf.RoundToInt(scale * 100f) + " %";
                        Assert.That(UiBuilder.TextScale, Is.EqualTo(scale), what);
                        float fruit = view.VictorySlotContentHeight(Loc.Locale, scale);
                        float need = view.Arrival!.NeedDp(scale, view.VictorySlotWidthDp);
                        TestContext.WriteLine(what + ": línea de fruta " + fruit + " dp, llegada más alta " + need + " dp, ranura "
                            + view.VictorySlot.ContentDp + " dp, esta llegada " + view.Arrival.ShownHeightDp + " dp, Siguiente " + view.NextTopDp
                            + ", cinta " + (view.VictoryPanelTopDp - 38f) + ", cabecera " + view.SlothBandTopDp);
                        var heights = new System.Text.StringBuilder();
                        RiverStopCatalog stops = boot.RiverStops!;
                        for (int n = 2; n < stops.Count; n++)
                        {
                            float h = view.Arrival.ArrivalHeightOf(stops.At(n)!, scale, view.VictorySlotWidthDp);
                            Assert.That(h, Is.LessThanOrEqualTo(view.VictorySlot.ContentDp), what + ": la llegada a la parada " + n + " cabe");
                            heights.Append(' ').Append(stops.At(n)!.Id).Append('=').Append(h);
                        }

                        TestContext.WriteLine(what + ": llegadas del catálogo (dp):" + heights);
                        AssertFits(view, what);
                        Assert.That(view.VictoryPanelTopDp - 38f, Is.GreaterThanOrEqualTo(view.SlothBandTopDp), what + ": la cinta, bajo la cabecera");
                        GameScreen.SimulatedScreen = null;
                        LaunchOptions.Reset();
                        FunnelSetup.Hooks();
                    }
                }
            }
        }

        // Lo que enseña la llegada cabe en la ranura, y cada línea de la placa cabe en su caja y dentro de la ranura (sin recortes).
        internal static void AssertFits(GameScreen view, string what)
        {
            ArrivalStrip strip = view.Arrival!;
            float slot = view.VictorySlot.ContentDp;
            Assert.That(strip.ShownHeightDp, Is.LessThanOrEqualTo(slot + 0.01f), what + ": la llegada cabe en la ranura");
            Assert.That(strip.SlotHeightDp, Is.EqualTo(slot), what);
            foreach (TextMeshProUGUI? label in new[] { strip.WelcomeLabel, strip.PlaceLabel, strip.FactLabel })
            {
                if (label == null || !label.gameObject.activeSelf)
                {
                    continue;
                }

                RectTransform rt = label.rectTransform;
                float preferred = label.GetPreferredValues(label.text, rt.rect.width, float.PositiveInfinity).y;
                Assert.That(preferred, Is.LessThanOrEqualTo(rt.rect.height + 0.5f), what + ": " + label.name + " cabe en su caja");
                float bottom = -rt.anchoredPosition.y + rt.rect.height;
                Assert.That(bottom, Is.LessThanOrEqualTo(slot + 0.5f), what + ": " + label.name + " dentro de la ranura");
                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * UiBuilder.TextScale) - 0.01f), what + ": ≥ 12 sp");
            }
        }

        // Carga el nivel con 5 canoas y 1.240 semillas, quita el reloj, aplica before y lo gana hasta VICTORY_SCREEN.
        internal static IEnumerator Win(int level, System.Action<GameScreen>? before = null)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            before?.Invoke(boot.View!);
            VictorySetup.WinToVictoryScreen(boot);
        }

        // Milisegundo (desde la victoria) en que Siguiente se habilita, empieza a verse y se ve entero.
        private static IEnumerator MeasureNext(int level, int[] times)
        {
            yield return Win(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
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

                PlayModeHelpers.TickExact(boot, 1);
            }

            Assert.That(times[2], Is.GreaterThanOrEqualTo(0));
        }
    }
}
