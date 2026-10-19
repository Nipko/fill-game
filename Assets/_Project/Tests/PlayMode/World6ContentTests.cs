#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El mundo 6, Oriente (201-300), en la escena <c>Game</c> real (tarea C1a; 03 §11; glosario §8, <c>w06_oriente</c>; D-012;
    /// 06 §9; D-017; F1c), sin paquetes de prueba:
    /// <list type="bullet">
    /// <item>carga: la escena enlaza <c>mundo_06_v1.json</c> (<c>pack_schema</c> 3, de P7a) tras los 5 paquetes de 1-200 (y,
    /// desde C1b, los mundos 7-9 detrás); el catálogo cubre 1-700 sin huecos y los niveles de muestra del mundo 6 arrancan con su tablero y llegan a jugarse, con el
    /// fondo que dan los datos del Mapa (la parada del nivel si el catálogo de paradas llega al mundo 6, M4.5a; si no, el de
    /// hoy);</item>
    /// <item>banner: el 201 abre el del mundo 6 («Mundo 6: Oriente · Ecuador · Napo + lagunas negras de Cuyabeno», de
    /// <c>strings.csv</c>) antes de la intro, y un toque lo cierra; con la regla del juego, el viaje de mundo (M3.6) sale si y
    /// solo si el catálogo de paradas tiene la gran parada del mundo 6;</item>
    /// <item>estrenos: la tarjeta de la Liana de cuenta en el 243 y la de la Hoja atada en el 283 (VA1a), con el catálogo real;</item>
    /// <item>«Siguiente» del 200 abre el 201 (el fin de contenido, tras el 700 desde C1b, y el paso del 300 al 301 están en
    /// <c>Worlds7to9ContentTests</c>);</item>
    /// <item>líneas: cada victoria de 261-300 lleva su línea de río (<c>rio_napo</c> 261-280 y <c>rio_amazonas</c> 281-300; propuesta
    /// de Frutas §5.16) salvo donde el árbitro dice otra cosa, y todas caben, en es-419 al 100 % y en en-XA al 130 % a
    /// 360 × 640.</item>
    /// </list>
    /// </summary>
    public sealed class World6ContentTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.BannerTravelOverride = null;
            LaunchOptions.Reset();
            if (!Loc.IsConfigured || Loc.Locale != LocLocale.SpanishLatinAmerica || !Loc.ShowUnapproved
                || !ReferenceEquals(Loc.Catalog, LocSetUpFixture.Catalog))
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        [UnityTest]
        public IEnumerator Load_TheSceneCarriesWorld6_AndItsLevelsArePlayable()
        {
            yield return PlayModeHelpers.LoadGame(201, new MemoryProgressStore(201, 200));
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            LevelCatalog catalog = flow.Catalog;
            Assert.That(catalog.Packs.Count, Is.EqualTo(9), "tutorial_v0, mundo_02_v1 a mundo_05_v1, mundo_06_v1 y mundo_07_v1 a mundo_09_v1 (C1b)");
            Assert.That(catalog.FirstLevel, Is.EqualTo(1));
            Assert.That(catalog.LastLevel, Is.EqualTo(700));
            Assert.That(catalog.IsContiguous, Is.True, catalog.DescribeGaps());
            LevelPack world6 = catalog.PackOf(201)!;
            Assert.That(world6.PackId, Is.EqualTo("mundo_06_v1"));
            Assert.That(world6.WaterBase, Is.EqualTo("white"), "el Napo, de agua blanca (glosario §8)");
            Assert.That(catalog.PackOf(300), Is.SameAs(world6));
            Assert.That(catalog.PackOf(200)!.PackId, Is.EqualTo("mundo_05_v1"));

            foreach (int level in new[] { 201, 243, 261, 283, 300 })
            {
                flow.Load(level);
                Assert.That(flow.LevelNumber, Is.EqualTo(level));
                Assert.That(flow.Level.World, Is.EqualTo(6), "glosario §8: el Oriente es 201-300");
                Assert.That(flow.Session.Current, Is.EqualTo(flow.Level.CreateBoard()), "el tablero del paquete, nivel " + level);
                Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Playing), "nivel " + level);
                RiverStop? expected = boot.RiverStops?.StopOf(level);
                if (expected != null && !MapConfig.UiBgPerStop)
                {
                    expected = boot.RiverStops!.FirstStopOfWorld(expected.World) ?? expected;
                }

                Assert.That(GameScreen.StopForLevel(boot.RiverStops, level, MapConfig.UiBgPerStop), Is.SameAs(expected),
                    "la parada de los datos del Mapa, o ninguna si el catálogo no llega al nivel " + level);
                Assert.That(view.StopBackdrop.Stop, Is.SameAs(expected), "el fondo del nivel " + level + ": el de su parada, o el de hoy sin ella");
                if (expected != null)
                {
                    Assert.That(expected.World, Is.EqualTo(6), "la parada del nivel " + level + ", del mundo 6");
                }

                boot.TapAnywhere();
                yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.Playing, "fase Playing del nivel " + level);
                Assert.That(flow.AcceptsBoardInput, Is.True, "nivel " + level);
            }
        }

        [UnityTest]
        public IEnumerator Banner_Level201_IsWorld6_BeforeTheIntro_OneTapClosesIt()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            GameScreen.BannerTravelOverride = false; // el banner de D-012; el viaje de mundo, en Banner_Level201_TravelsOnlyWithTheGateOfWorld6
            yield return CardSetup.Load(201);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(WorldBanners.WorldAt(201), Is.EqualTo(6));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.WorldBanner, Is.EqualTo(6));
            Assert.That(banner.Visible, Is.True);
            Assert.That(banner.Text, Is.EqualTo(UiText.WorldBanner(6)));
            Assert.That(banner.Text, Is.EqualTo("Mundo 6: Oriente · Ecuador · Napo + lagunas negras de Cuyabeno"), "strings.csv (glosario §8)");
            Assert.That(banner.Travel, Is.Null, "viaje apagado por el test: el banner de siempre");
            Assert.That(view.LastWorldBannerProbe, Does.Contain("\"level\":201").And.Contain("\"world\":6"));
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 201, 6);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.WorldBannersShown, Is.EqualTo(1));

            flow.Load(202);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(flow.WorldBanner, Is.EqualTo(0), "el 202 no lo lleva");
            Assert.That(banner.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Banner_Level201_TravelsOnlyWithTheGateOfWorld6()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            GameScreen.BannerTravelOverride = true; // la regla del juego (MapMoments.TravelInBanner con los datos del Mapa)
            yield return CardSetup.Load(201);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(MapMoments.TravelInBanner(201, MapConfig.UiMapAutopen), Is.True, "el 201 abre mundo con banner (D-013, punto 7)");
            Assert.That(flow.WorldBanner, Is.EqualTo(6));
            Assert.That(banner.Visible, Is.True);
            RiverStop? gate = boot.RiverStops?.FirstStopOfWorld(6);
            if (gate == null)
            {
                Assert.That(banner.Travel, Is.Null, "sin la gran parada del mundo 6 en el catálogo de paradas, el banner de siempre");
                yield return WorldBannerSetup.CloseThenTheIntro(boot, 201, 6);
                yield break;
            }

            Assert.That(boot.RiverStops!.StopOf(201), Is.SameAs(gate), "el 201 abre la gran parada del mundo 6 (" + gate.Id + ")");
            Assert.That(banner.Travel, Is.Not.Null, "con la gran parada del mundo 6 (" + gate.Id + "), el viaje de mundo (M3.6)");
            Assert.That(banner.TravelMs, Is.LessThanOrEqualTo((float)MapConfig.WorldTravelInBannerMaxMs), "≤ 4 s");
            for (int elapsed = 0; elapsed < 8000 && !banner.TravelDone; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(banner.TravelDone, Is.True, "la canoa llega a " + gate.Id);
            PlayModeHelpers.Step(boot, 1000);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "el banner no se cierra solo");
            yield return null;
            WorldBannerSetup.Tap(banner);
            Assert.That(flow.WorldBannerClosing && banner.Dismissing, Is.True, "con el viaje terminado, un toque cierra");
            PlayModeHelpers.Step(boot, 400);
            Assert.That(banner.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Debuts_CountLiana243_AndTiedLeaf283_WithTheirCards()
        {
            foreach ((int level, string intro) in new[] { (243, LevelCardInfo.LianaCountIntro), (283, LevelCardInfo.HojaAtadaIntro) })
            {
                yield return CardSetup.Load(level);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                LevelFlow flow = boot.Flow!;
                LevelCardView card = view.LevelCard;
                Assert.That(flow.Level.IntroMechanic, Is.EqualTo(intro), "mundo_06_v1.json, nivel " + level);
                Assert.That(flow.Level.Skippable, Is.False, "estreno: no saltable (" + level + ")");
                Assert.That(flow.WorldBanner, Is.EqualTo(0));
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "la tarjeta del estreno, " + level);
                Assert.That(card.Visible, Is.True, "nivel " + level);
                Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Debut), "nivel " + level);
                Assert.That(card.Card.IntroMechanic, Is.EqualTo(intro));
                Assert.That(view.LastCardProbe, Does.Contain("\"intro\":\"" + intro + "\"").And.Contain("\"kind\":\"debut\""));
                CardSetup.AssertInsidePanel(view);
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
            }
        }

        [UnityTest]
        public IEnumerator Next_From200_Opens201_NotTheContentEnd()
        {
            yield return FactLineTests.Win(200);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressNext(), Is.True);
            for (int elapsed = 0; elapsed < 10000 && boot.Flow!.LevelNumber == 200 && boot.App!.Screen == AppScreen.Level; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "el 200 ya no es el fin de contenido");
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(201));
            Assert.That(boot.Flow.Level.World, Is.EqualTo(6), "el 201, del mundo 6 (el banner va con las tarjetas: Banner_Level201…)");
        }

        [UnityTest]
        public IEnumerator Levels261To300_EachVictoryCarriesItsRiverLine_UnlessTheArbiterSaysOtherwise_AndAllFit()
        {
            foreach (string locale in new[] { LocLocale.SpanishLatinAmerica, LocLocale.Pseudo })
            {
                bool pseudo = locale == LocLocale.Pseudo;
                yield return FactLineTests.Win(261, null, locale, pseudo ? 1.3f : 1f, pseudo);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                FactLine line = view.FruitLine!;
                FactSchedule schedule = boot.FruitFacts!;
                RiverStopCatalog stops = boot.RiverStops!;
                int shown = 0;
                var without = new List<int>();
                for (int level = 261; level <= 300; level++)
                {
                    string what = level + " en " + locale;
                    FactPosition p = schedule.Progress(level);
                    string subject = level <= 280 ? "rio_napo" : "rio_amazonas";
                    Assert.That(p.IsValid, Is.True, what);
                    Assert.That(p.Series!.Subject, Is.EqualTo(subject), what + ": propuesta de Frutas §5.16");
                    Assert.That(p.Series.Kind, Is.EqualTo(FactSeriesKind.River), what);
                    Assert.That(p.K, Is.EqualTo(((level - 261) % 20) + 1), what);
                    Assert.That(Loc.Fact(p.Key!), Is.Not.Empty, what + ": la línea tiene texto");
                    VictoryLine vl = VictoryLineArbiter.For(VictoryLineArbiter.ContextFor(level, false, stops, view.FruitKeyOf!(level), false));
                    bool expected = vl.FruitKey != null;
                    Assert.That(line.ShowFor(level, false, in vl), Is.EqualTo(expected), what);
                    if (!expected)
                    {
                        without.Add(level);
                        continue;
                    }

                    shown++;
                    Assert.That(vl.FruitKey, Is.EqualTo(p.Key), what);
                    Assert.That(line.ShownFactId, Is.EqualTo(subject + "." + p.K.ToString("00", System.Globalization.CultureInfo.InvariantCulture)), what);
                    Assert.That(line.BodyText, Is.EqualTo(Loc.Fact(p.Key!)), what);
                    if (!pseudo)
                    {
                        string name = Loc.Fact(p.Series.NameKey);
                        Assert.That(line.ProgressText, p.IsLast ? (NUnit.Framework.Constraints.IResolveConstraint)Is.EqualTo(name) : Does.StartWith(name + " · "),
                            what + ": «{name} · k/20» con el nombre del río (en la línea 20, solo el nombre con el sello)");
                    }

                    Assert.That(line.Icon == null || !line.Icon.gameObject.activeSelf, Is.True, what + ": sin icono de fruta (no es un tinte)");
                    FactLineTests.AssertFits(view, what);
                }

                line.Hide();
                TestContext.WriteLine(locale + ": " + shown + " líneas de río; sin línea: " + string.Join(", ", without));
                Assert.That(shown + without.Count, Is.EqualTo(40));
                Assert.That(shown, Is.GreaterThanOrEqualTo(36),
                    "casi todas: como mucho las desplazan las llegadas de 270, 280, 290 y 300 (si hay paradas del mundo 6) y los hitos de TiTino");

                GameScreen.SimulatedScreen = null;
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
            }
        }

        [UnityTest]
        public IEnumerator Victory261_ShowsTheNapoLine_WithItsHeader()
        {
            yield return FactLineTests.Win(261);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            VictoryLine vl = view.CurrentVictoryLine;
            Assert.That(vl.FruitKey, Is.EqualTo("fact.rio_napo.01"));
            FactLine line = view.FruitLine!;
            Assert.That(line.Shown, Is.True);
            Assert.That(line.ShownFactId, Is.EqualTo("rio_napo.01"));
            Assert.That(line.ProgressText, Is.EqualTo("Napo y Cuyabeno · 1/20"));
            Assert.That(line.BodyText, Is.EqualTo("El Napo baja de los Andes de Ecuador cargado de barro fino: es un río de agua blanca."));
            FactLineTests.AssertFits(view, "261");
            view.NextButton.onClick.Invoke();
            Assert.That(view.LastRiverFactId, Is.EqualTo("rio_napo.01"), "fact_id de af_river_fact_shown (08 §8.2)");
        }
    }
}
