#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los mundos 7-9, Madre de Dios (301-420), Madidi (421-560) y Ucayali (561-700), en la escena <c>Game</c> real (tarea C1b;
    /// 03 §11; glosario §8, <c>w07_madre_de_dios</c>, <c>w08_madidi</c> y <c>w09_ucayali</c>; D-012; 06 §9; D-019 Q-Ti3; D-027):
    /// <list type="bullet">
    /// <item>carga: la escena enlaza <c>mundo_07_v1.json</c> a <c>mundo_09_v1.json</c> (<c>pack_schema</c> 3, de GEN79) en la
    /// variante <see cref="LevelCatalog.PublishedVariant"/> tras los 6 paquetes de 1-300; el catálogo cubre 1-700 sin huecos y
    /// los niveles de muestra arrancan con su tablero y llegan a jugarse;</item>
    /// <item>banners: el 301, el 421 y el 561 abren el de su mundo (de <c>strings.csv</c>) antes de la intro, y un toque lo
    /// cierra; con la regla del juego, el viaje de mundo (M3.6) sale si y solo si el catálogo de paradas tiene la gran parada del
    /// mundo;</item>
    /// <item>estrenos: el Brote de dos hojas en el 343 y la Flor en el 403 (VA1b) con su tarjeta y skippable = false; la Tinaja
    /// en el 323 solo en la variante «a» (D-019): con la «b» publicada, el 323 es un nivel normal, y el estreno de la «a» se
    /// prueba con su paquete real cargado por el gancho de desarrollo (sin publicarlo);</item>
    /// <item>tablero: los tableros de 14 cuias de 561-700 (los hitos 580-700, D-027, K = 12 + 1 vacío o K = 13, y el 694) caben en
    /// el tablero de D-006 (≤ 16 ranuras) en el teléfono de pruebas (360 × 780 dp), en 3 filas de como mucho 5 como sus variantes
    /// de alivio de 15 cuias;</item>
    /// <item>fin de contenido: «Siguiente» del 300 abre el 301 y, tras ganar el 700, «El río sigue creciendo» (06 §9): el último
    /// nivel publicado, sin ningún número fijo.</item>
    /// </list>
    /// </summary>
    public sealed class Worlds7to9ContentTests
    {
        private static readonly int[] FirstLevels = { 301, 421, 561 };

        private static readonly string[] Banners =
        {
            "Mundo 7: Madre de Dios · Perú · Tambopata / Madre de Dios",
            "Mundo 8: Madidi · Bolivia · Beni / Mamoré, Iténez",
            "Mundo 9: Ucayali · Perú · Ucayali, Shanay-Timpishka",
        };

        // Los tableros de 14 cuias de 561-700 (los hitos desde el 580, D-027, y el 694 de la variante publicada).
        private static readonly int[] FourteenCuiaLevels = { 580, 600, 620, 640, 660, 680, 694, 700 };

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
        public IEnumerator Load_TheSceneCarriesWorlds7To9_AndTheirLevelsArePlayable()
        {
            yield return PlayModeHelpers.LoadGame(301, new MemoryProgressStore(301, 300));
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            LevelCatalog catalog = flow.Catalog;
            Assert.That(catalog.Packs.Count, Is.EqualTo(9), "tutorial_v0, mundo_02_v1 a mundo_06_v1 y mundo_07_v1 a mundo_09_v1");
            Assert.That(catalog.FirstLevel, Is.EqualTo(1));
            Assert.That(catalog.LastLevel, Is.EqualTo(700));
            Assert.That(catalog.IsContiguous, Is.True, catalog.DescribeGaps());
            string[] water = { "white", "white", "white" };
            for (int w = 0; w < 3; w++)
            {
                LevelPack pack = catalog.PackOf(FirstLevels[w])!;
                Assert.That(pack.PackId, Is.EqualTo("mundo_0" + (7 + w) + "_v1"));
                Assert.That(pack.World, Is.EqualTo(7 + w));
                Assert.That(pack.WaterBase, Is.EqualTo(water[w]), "glosario §8: el agua del mundo " + (7 + w));
                Assert.That(catalog.PackOf(WorldTable.LastLevelOf(7 + w)), Is.SameAs(pack));
            }

            Assert.That(catalog.Find(323)!.HasMechanic(AF.Core.Mechanics.MechanicType.Tinaja), Is.EqualTo(LevelCatalog.TinajaPublished),
                "el 323 lleva la Tinaja si y solo si se publica la variante «a» (D-019 Q-Ti3; hoy, la " + LevelCatalog.PublishedVariant + ")");

            foreach (int level in new[] { 301, 323, 343, 403, 420, 421, 561, 620, 700 })
            {
                flow.Load(level);
                Assert.That(flow.LevelNumber, Is.EqualTo(level));
                Assert.That(flow.Level.World, Is.EqualTo(WorldTable.WorldOf(level)), "glosario §8: mundo del nivel " + level);
                Assert.That(flow.Session.Current, Is.EqualTo(flow.Level.CreateBoard()), "el tablero del paquete, nivel " + level);
                Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Playing), "nivel " + level);
                boot.TapAnywhere();
                yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.Playing, "fase Playing del nivel " + level);
                Assert.That(flow.AcceptsBoardInput, Is.True, "nivel " + level);
            }
        }

        [UnityTest]
        public IEnumerator Banners_301_421_561_AreWorlds7To9_BeforeTheIntro_OneTapClosesThem()
        {
            for (int w = 0; w < 3; w++)
            {
                int level = FirstLevels[w];
                int world = 7 + w;
                GameScreen.SimulatedScreen = HudLabelTests.Screen780;
                GameScreen.BannerTravelOverride = false; // el banner de D-012; el viaje de mundo, en Banners_TravelOnlyWithTheGateOfTheirWorld
                yield return CardSetup.Load(level);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                LevelFlow flow = boot.Flow!;
                WorldBannerView banner = view.WorldBannerView;
                Assert.That(WorldBanners.WorldAt(level), Is.EqualTo(world));
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "nivel " + level);
                Assert.That(flow.WorldBanner, Is.EqualTo(world));
                Assert.That(banner.Visible, Is.True, "nivel " + level);
                Assert.That(banner.Text, Is.EqualTo(UiText.WorldBanner(world)));
                Assert.That(banner.Text, Is.EqualTo(Banners[w]), "strings.csv (glosario §8)");
                Assert.That(banner.Travel, Is.Null, "viaje apagado por el test: el banner de siempre");
                Assert.That(view.LastWorldBannerProbe, Does.Contain("\"level\":" + level).And.Contain("\"world\":" + world));
                yield return WorldBannerSetup.CloseThenTheIntro(boot, level, world);
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
                Assert.That(view.WorldBannersShown, Is.EqualTo(1));

                flow.Load(level + 1);
                PlayModeHelpers.Step(boot, 300);
                Assert.That(flow.WorldBanner, Is.EqualTo(0), "el " + (level + 1) + " no lo lleva");
                Assert.That(banner.Visible, Is.False);
                GameScreen.SimulatedScreen = null;
                GameScreen.BannerTravelOverride = null;
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
            }
        }

        [UnityTest]
        public IEnumerator Banners_TravelOnlyWithTheGateOfTheirWorld()
        {
            for (int w = 0; w < 3; w++)
            {
                int level = FirstLevels[w];
                int world = 7 + w;
                GameScreen.SimulatedScreen = HudLabelTests.Screen780;
                GameScreen.BannerTravelOverride = true; // la regla del juego (MapMoments.TravelInBanner con los datos del Mapa)
                yield return CardSetup.Load(level);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                LevelFlow flow = boot.Flow!;
                WorldBannerView banner = view.WorldBannerView;
                Assert.That(MapMoments.TravelInBanner(level, MapConfig.UiMapAutopen), Is.True, "el " + level + " abre mundo con banner (D-013, punto 7)");
                Assert.That(flow.WorldBanner, Is.EqualTo(world));
                Assert.That(banner.Visible, Is.True);
                RiverStop? gate = boot.RiverStops?.FirstStopOfWorld(world);
                if (gate == null)
                {
                    Assert.That(banner.Travel, Is.Null, "sin la gran parada del mundo " + world + ", el banner de siempre");
                }
                else
                {
                    Assert.That(boot.RiverStops!.StopOf(level), Is.SameAs(gate), "el " + level + " abre la gran parada del mundo " + world + " (" + gate.Id + ")");
                    Assert.That(banner.Travel, Is.Not.Null, "con la gran parada del mundo " + world + " (" + gate.Id + "), el viaje de mundo (M3.6)");
                    Assert.That(banner.TravelMs, Is.LessThanOrEqualTo((float)MapConfig.WorldTravelInBannerMaxMs), "≤ 4 s");
                    for (int elapsed = 0; elapsed < 8000 && !banner.TravelDone; elapsed += PlayModeHelpers.FrameMs)
                    {
                        PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                    }

                    Assert.That(banner.TravelDone, Is.True, "la canoa llega a " + gate.Id);
                }

                PlayModeHelpers.Step(boot, 1500);
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "el banner no se cierra solo (" + level + ")");
                yield return null;
                WorldBannerSetup.Tap(banner);
                Assert.That(flow.WorldBannerClosing && banner.Dismissing, Is.True, "un toque cierra (" + level + ")");
                PlayModeHelpers.Step(boot, 400);
                Assert.That(banner.Visible, Is.False);
                GameScreen.SimulatedScreen = null;
                GameScreen.BannerTravelOverride = null;
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
            }
        }

        [UnityTest]
        public IEnumerator Debuts_BroteTwoLeaves343_AndFlor403_WithTheirCards_NotSkippable()
        {
            foreach ((int level, string intro) in new[] { (343, LevelCardInfo.BroteTwoLeavesIntro), (403, LevelCardInfo.FlorIntro) })
            {
                yield return CardSetup.Load(level);
                AssertDebutCard(level, intro, "mundo_07_v1.json (variante " + LevelCatalog.PublishedVariant + ")");
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
            }
        }

        [UnityTest]
        public IEnumerator Level323_InThePublishedVariant_HasTheTinajaDebutOnlyInVariantA()
        {
            yield return CardSetup.Load(323);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            if (LevelCatalog.TinajaPublished)
            {
                AssertDebutCard(323, "tinaja", "mundo_07_v1.json (variante a)");
                yield break;
            }

            // Salida B de D-019 (Q-Ti3): el 323 de la «b» es un nivel normal (Ma en el calendario), sin la Tinaja ni su estreno.
            Assert.That(flow.Level.IntroMechanic, Is.Null, "variante b: el 323 no estrena nada");
            Assert.That(flow.Level.Skippable, Is.True);
            Assert.That(flow.Level.HasMechanic(AF.Core.Mechanics.MechanicType.Tinaja), Is.False, "variante b: sin la Tinaja");
            Assert.That(boot.View!.LevelCard.Visible && boot.View.LevelCard.Card!.Kind == LevelCardKind.Debut, Is.False, "sin tarjeta de estreno");
        }

        [UnityTest]
        public IEnumerator Level323_VariantA_RealPack_ArrivesWithTheTinajaDebutCard_NotSkippable()
        {
            // El paquete real de la variante «a» (GEN79), que publica C1c si se cumple la condición de D-019: cargado con el gancho
            // de desarrollo (LevelPackJsonOverride), sin tocar la escena.
            string json = File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Data", "Levels", "mundo_07_v1.json"));
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGameWithPack(json, 323, new MemoryProgressStore(323, 322));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(boot.Flow!.Catalog.PackOf(323)!.PackId, Is.EqualTo("mundo_07_v1"));
            Assert.That(boot.Flow.Level.HasMechanic(AF.Core.Mechanics.MechanicType.Tinaja), Is.True, "variante a: el 323 lleva la Tinaja");
            AssertDebutCard(323, "tinaja", "mundo_07_v1.json (variante a)");
        }

        [UnityTest]
        public IEnumerator FourteenCuiaBoards_FitTheBoard_At360x780_InThreeRowsLikeTheirReliefOf15()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp, como BoardRowsTests
            yield return PlayModeHelpers.LoadGame(580, new MemoryProgressStore(580, 579));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            foreach (int level in FourteenCuiaLevels)
            {
                flow.Load(level);
                boot.TapAnywhere();
                PlayModeHelpers.StepUntilIdle(boot, 3000);
                string what = "nivel " + level;
                Assert.That(flow.Level.CuiaCount, Is.EqualTo(14), what + ": 14 cuias");
                Assert.That(view.Views.Length, Is.EqualTo(14), what);
                AssertFits(view.Layout!, 14, what);
                for (int r = 0; r < flow.Level.Relief.Count; r++)
                {
                    ReliefBoard relief = flow.Level.Relief[r];
                    Assert.That(relief.CuiaCount, Is.LessThanOrEqualTo(AF.Core.Board.BoardState.MaxCuias), what + ", relief " + r);
                    if (relief.Type == ReliefBoard.TypePlusEmpty)
                    {
                        // La variante «una cuia vacía más» (03 §13) tiene 15 cuias: 3 filas de 5 (BoardLayout.RowsFor, D-006). Con
                        // el tablero de 14 ya en 3 filas de como mucho 5, la ranura y el alto de fila son los mismos: también cabe.
                        Assert.That(relief.CuiaCount, Is.EqualTo(15), what + ", relief " + r);
                        Assert.That(view.Layout!.Rows, Is.EqualTo(3), what + ": 3 filas, como su alivio de 15");
                        Assert.That(System.Linq.Enumerable.Max(view.Layout.PerRow), Is.LessThanOrEqualTo(5), what + ": filas de ≤ 5, como su alivio de 15");
                    }
                }

                TestContext.WriteLine(what + ": " + view.Layout!.Rows + " filas (" + string.Join("/", view.Layout.PerRow) + "), cuia de "
                    + view.Layout.CuiaWidth.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " dp");
            }

        }

        [UnityTest]
        public IEnumerator Next_From300_Opens301_NotTheContentEnd()
        {
            yield return FactLineTests.Win(300);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Catalog.NextLevelAfter(300), Is.EqualTo(301));
            Assert.That(boot.PressNext(), Is.True);
            for (int elapsed = 0; elapsed < 10000 && boot.Flow!.LevelNumber == 300 && boot.App!.Screen == AppScreen.Level; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "el 300 ya no es el fin de contenido");
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(301));
            Assert.That(boot.Flow.Level.World, Is.EqualTo(7), "el 301, del mundo 7 (el banner va con las tarjetas: Banners_301…)");
        }

        [UnityTest]
        public IEnumerator ContentEnd_AfterWinning700_NextOpensTheRiverKeepsGrowing()
        {
            yield return FactLineTests.Win(700);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Catalog.LastLevel, Is.EqualTo(700), "el último nivel publicado");
            Assert.That(flow.Catalog.NextLevelAfter(700), Is.Null);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(boot.PressNext(), Is.True);
            for (int elapsed = 0; elapsed < 10000 && boot.App.Screen == AppScreen.Level; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.ContentEnd), "06 §9: tras el 700, «El río sigue creciendo»");
            Assert.That(boot.App.OverlayLevel, Is.EqualTo(701));
            yield return null;
            Assert.That(boot.Lobby!.OverlayVisible, Is.True);
            Assert.That(boot.Lobby.OverlayTitle, Is.EqualTo("El río sigue creciendo"));
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(700), "no se carga ningún 701: el 700 sigue siendo el último jugado");
        }

        // La tarjeta del estreno de intro en level, no saltable y dentro del panel.
        private static void AssertDebutCard(int level, string intro, string source)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            LevelCardView card = view.LevelCard;
            Assert.That(flow.Level.IntroMechanic, Is.EqualTo(intro), source + ", nivel " + level);
            Assert.That(flow.Level.Skippable, Is.False, "estreno: no saltable (" + level + ")");
            Assert.That(flow.WorldBanner, Is.EqualTo(0));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "la tarjeta del estreno, " + level);
            Assert.That(card.Visible, Is.True, "nivel " + level);
            Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Debut), "nivel " + level);
            Assert.That(card.Card.IntroMechanic, Is.EqualTo(intro));
            Assert.That(view.LastCardProbe, Does.Contain("\"intro\":\"" + intro + "\"").And.Contain("\"kind\":\"debut\""));
            CardSetup.AssertInsidePanel(view);
        }

        // El tablero cabe: ≤ 16 ranuras (D-006), la cuia no baja del mínimo de 02 §5 y todo el vidrio queda dentro del panel.
        private static void AssertFits(BoardLayoutResult layout, int count, string what)
        {
            Assert.That(count, Is.LessThanOrEqualTo(AF.Core.Board.BoardState.MaxCuias), what);
            Assert.That(layout.Rows, Is.InRange(2, 3), what + ": 2 o 3 filas con 11-14 cuias (D-006)");
            Assert.That(layout.CuiaWidth, Is.GreaterThanOrEqualTo(BoardLayout.MinCuiaWidth), what + ": cuia ≥ 28 dp");
            float left = layout.PanelX;
            float right = layout.PanelX + layout.PanelWidth;
            for (int i = 0; i < count; i++)
            {
                Assert.That(layout.CenterX[i] - (0.5f * layout.CuiaWidth), Is.GreaterThanOrEqualTo(left - 0.01f), what + ", cuia " + i + " por la izquierda");
                Assert.That(layout.CenterX[i] + (0.5f * layout.CuiaWidth), Is.LessThanOrEqualTo(right + 0.01f), what + ", cuia " + i + " por la derecha");
                Assert.That(layout.FootY[i], Is.LessThanOrEqualTo(layout.PanelBottom + 0.01f), what + ", cuia " + i + " por abajo");
                Assert.That(layout.FootY[i] - layout.CuiaHeight, Is.GreaterThanOrEqualTo(layout.PanelY - 0.01f), what + ", cuia " + i + " por arriba");
            }
        }
    }
}
