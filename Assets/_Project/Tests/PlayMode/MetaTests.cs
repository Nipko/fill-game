#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La meta de B14 (tanda 5) en la escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>ganar el 40 da la Cuia de Santarém, se pone sola con el aviso «Cuia de Santarém · puesta» (Q-M8 (a)) y las cuias del
    /// nivel siguiente la llevan (borde, pie, banda y vidrio ámbar; la cavidad y las marcas, iguales: 07 §4.6);</item>
    /// <item><c>album.fruits</c> se llena con los tintes del primer nivel y escribe una sonda por tinte (08 §4.3 y §8.2);</item>
    /// <item>el Mapa: la cestita de la Canasta en las paradas que faltan, el ✓ en las cobradas, «Premio del mundo · faltan N» bajo
    /// el cierre del mundo y, en la tarjeta de una parada futura, el contenido de su Canasta (06 §4.4-§4.5);</item>
    /// <item>los puntos de paso ya recorridos se tocan y cuentan su nombre y su dato (Teotônio);</item>
    /// <item>Ajustes › «Sobre la Amazonía» › «Datos del río», con el lugar y la fuente de cada dato visto (07 §8.2);</item>
    /// <item>la noche de Muy difícil y la tarde de Difícil son la luz del fondo (07 §7.7 [P]), sin el velo del paso 1.</item>
    /// </list>
    /// </summary>
    public sealed class MetaTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.DifficultyLightEnabled = true;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator Winning40_PutsOnTheSantaremCuia_WithItsNotice_AndTheNextBoardWearsIt()
        {
            LaunchOptions.LevelOffsetOverride = 30;
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.Skins!.Equipped, Is.EqualTo(SkinRewards.BaseSkinId));
            Assert.That(boot.View!.Views[0].Skin, Is.SameAs(CuiaSkin.Base), "antes, la Cuia de vidrio");
            Assert.That(boot.View.Views[0].SkinRim.gameObject.activeSelf, Is.False);

            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMeta(16f);
            Assert.That(boot.Flow!.LastSkinUnlock!.SkinId, Is.EqualTo("af_skin_m02"));
            Assert.That(boot.Skins.Equipped, Is.EqualTo("af_skin_m02"), "Q-M8 (a): se pone sola");
            Assert.That(boot.Profile!.Entitlements.Skins, Does.Contain("af_skin_m02"));
            Assert.That(boot.Profile.Entitlements.SkinEquipped, Is.EqualTo("af_skin_m02"));
            Assert.That(boot.SkinUnlockCount, Is.EqualTo(1));
            Assert.That(boot.SkinToast == null || !boot.SkinToast.Visible, Is.True, "sobre la victoria no: taparía el bocadillo de TiTino");

            boot.Flow.Load(40);
            boot.TickMeta(16f);
            Toast toast = boot.SkinToast!;
            Assert.That(toast.Visible, Is.True, "con aviso, en la pantalla siguiente");
            Assert.That(toast.Text, Is.EqualTo("Cuia de Santarém · puesta"));
            Assert.That(toast.BlocksRaycasts, Is.False, "no tapa los toques del tablero");
            Assert.That(boot.SkinUnlockCount, Is.EqualTo(1), "una sola sonda");
            CuiaView cuia = boot.View.Views[0];
            Assert.That(cuia.Skin, Is.SameAs(CuiaSkin.Santarem), "las cuias nuevas la llevan");
            Assert.That(cuia.SkinRim.gameObject.activeSelf && cuia.SkinFoot.gameObject.activeSelf && cuia.SkinBand.gameObject.activeSelf, Is.True);
            Assert.That(cuia.SkinHandle.gameObject.activeSelf, Is.False, "el asa es de la Totuma");
            Assert.That(cuia.SkinRim.color, Is.EqualTo(CuiaSkin.Santarem.Rim));
            Color glass = cuia.GlassBackImage.color;
            Assert.That(glass.r, Is.GreaterThan(glass.b), "vidrio ámbar");
            Assert.That(1f - glass.b, Is.LessThanOrEqualTo(CuiaSkin.MaxGlassTint + 0.001f), "≤ 8 % (07 §4.6)");
            Assert.That(CuiaSkin.Santarem.GlassTint.a, Is.EqualTo(0.06f).Within(0.0001f));
            // La banda va en el hombro, por encima de la cavidad: nunca la cubre (07 §4.6; D-006).
            float bandBottom = cuia.SkinBand.rectTransform.anchoredPosition.y;
            Assert.That(bandBottom, Is.GreaterThanOrEqualTo(CuiaGeometry.CavityTopU(cuia.Capacity) * cuia.Unit - 0.01f));
        }

        [UnityTest]
        public IEnumerator Winning40_ThenNext_TheNoticeWaitsForTheWorldBannerOf41()
        {
            // El camino real: tras el 40, «Siguiente» abre el 41 con el banner del mundo 3 (D-012); el aviso no sale encima de él.
            FunnelSetup.Hooks();
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true; // el arte del banner en el acto, como CardSetup
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(Wallet.NewProfile(new ManualClock(T0)));
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            CloseCards(boot); // la tarjeta previa del 40 (Difícil)
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMeta(16f);
            Assert.That(boot.SkinUnlockCount, Is.EqualTo(1));

            boot.PressNext();
            for (int i = 0; i < 200 && boot.Flow!.LevelNumber != 41; i++)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(41));
            Assert.That(boot.Flow.WorldBanner, Is.Not.EqualTo(0), "el banner del mundo 3");
            boot.TickMeta(16f);
            Assert.That(boot.SkinToast == null || !boot.SkinToast.Visible, Is.True, "no encima del banner");
            CloseCards(boot);
            boot.TickMeta(16f);
            Assert.That(boot.SkinToast!.Visible, Is.True, "al cerrarse el banner");
            Assert.That(boot.SkinToast.Text, Is.EqualTo("Cuia de Santarém · puesta"));
            Assert.That(boot.View!.Views[0].Skin, Is.SameAs(CuiaSkin.Santarem), "el 41 ya la lleva");
            Assert.That(boot.SkinUnlockCount, Is.EqualTo(1));
        }

        // Cierra el banner de mundo y la tarjeta previa (con toques y su tiempo), hasta que el nivel entra.
        internal static void CloseCards(GameBootstrap boot)
        {
            for (int i = 0; i < 400 && (boot.Flow!.Phase == FlowPhase.LevelCard || boot.Flow.WorldBanner != 0); i++)
            {
                if (boot.Flow.WorldBanner != 0)
                {
                    boot.Flow.DismissWorldBanner();
                }
                else
                {
                    boot.Flow.DismissLevelCard();
                }

                PlayModeHelpers.Step(boot, 50);
            }

            Assert.That(boot.Flow!.Phase == FlowPhase.LevelCard || boot.Flow.WorldBanner != 0, Is.False, "el nivel entra");
        }

        [UnityTest]
        public IEnumerator Level1_FillsTheAlbumWithItsTints_AndProbesEachOne()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore(1, 0));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Profile!.Album.Fruits, Is.EqualTo(new[] { 4, 8 }), "aguaje y açaí (glosario §9)");
            boot.TickMeta(16f);
            Assert.That(boot.FruitDiscoveredCount, Is.EqualTo(2), "una línea af_fruit_discovered por tinte");
            string? line = new DevProbe().FormatFruitDiscovered("acai", 8, 1, boot.Profile.Album.Fruits);
            Assert.That(line, Does.Contain("\"tint_id\":\"acai\"").And.Contain("\"album\":[4,8]"));
        }

        [UnityTest]
        public IEnumerator Map_BasketMarks_ThePrizeOfTheWorld_AndTheFutureCardContents()
        {
            yield return MapMarkersTests.OpenApp(26); // max 25: las paradas 0-3 de los mundos 1-2 y el Premio de Flona Tapajós (el 40)
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            int santarem = map.StopMarkerIndexOf(1);
            int alter = map.StopMarkerIndexOf(2);
            int flona = map.StopMarkerIndexOf(3);
            Assert.That(map.StopMarkerBasketShown(map.StopMarkerIndexOf(0)) || map.StopMarkerClaimedShown(map.StopMarkerIndexOf(0)), Is.False, "Belém no tiene Canasta");
            Assert.That(map.StopMarkerClaimedShown(santarem) && map.StopMarkerClaimedShown(alter), Is.True, "las del 10 y el 20, cobradas");
            Assert.That(map.StopMarkerBasketShown(santarem), Is.False);
            Assert.That(map.StopMarkerBasketShown(flona), Is.True, "la del 30 falta");
            Assert.That(map.PrizeMarker, Is.EqualTo(flona));
            Assert.That(map.PrizeText, Is.EqualTo("Premio del mundo · faltan 15"));
            Assert.That(RiverMapScreen.PrizeLevelsLeft(GameBootstrap.Current!.RiverStops!.At(3)!, 39), Is.EqualTo(1));
            Assert.That(RiverMapScreen.PrizeLevelsLeft(GameBootstrap.Current!.RiverStops!.At(3)!, 40), Is.EqualTo(-1), "ganado el 40, ya no hay premio");

            Assert.That(map.OpenStopCard(3), Is.True);
            StopCard card = map.Card!;
            Assert.That(card.Mode, Is.EqualTo(StopCardMode.Future));
            Assert.That(card.BasketShown, Is.True, "06 §4.5: la parada futura enseña su Canasta");
            BasketContents basket = card.Basket;
            Assert.That(basket.Level, Is.EqualTo(30));
            Assert.That(basket.Seeds, Is.EqualTo(50));
            Assert.That(basket.Stock.Get(BoosterKind.Hint), Is.EqualTo(1), "el 30: eco_basket_cycle[1] = hint");
            Assert.That(card.BasketItemCount, Is.EqualTo(2));
            Assert.That(card.BasketItemText(0), Is.EqualTo("50"));
            Assert.That(card.BasketItemText(1), Is.EqualTo("1"));
            Assert.That(card.PrizeText, Is.EqualTo("Premio del mundo · faltan 15"));
            Assert.That(card.PrizeSkin, Is.SameAs(CuiaSkin.Santarem));
            Assert.That(ProceduralArt.IsSkinThumb(card.PrizeCuiaImage.sprite, "af_skin_m02"), Is.True, "el premio, con la cuia de su skin");
            foreach (TMPro.TextMeshProUGUI t in card.Texts())
            {
                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(12f), t.name + " ≥ 12 sp");
            }

            card.Hide();
            Assert.That(map.OpenStopCard(2), Is.True);
            Assert.That(map.Card!.BasketShown, Is.False, "una parada alcanzada no repite la Canasta: su marca es el ✓");
            Assert.That(map.Card.PrizeText, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Map_PassedWaypoints_TellTheirNameAndFact()
        {
            yield return MapMarkersTests.OpenApp(21); // max 20: los tramos a Santarém y a Alter do Chão
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.PassingCount, Is.GreaterThanOrEqualTo(4));
            int breves = IndexOfPassing(map, "breves");
            Assert.That(breves, Is.GreaterThanOrEqualTo(0), "Breves, en el tramo a Santarém");
            Assert.That(IndexOfPassing(map, "obidos"), Is.LessThan(0), "Óbidos queda río arriba de Santarém: aún no se pasó");
            Assert.That(map.TapPassing(breves), Is.True);
            StopCard card = map.Card!;
            Assert.That(card.Mode, Is.EqualTo(StopCardMode.Passing));
            Assert.That(card.TitleText, Is.EqualTo("Breves"));
            Assert.That(card.FactText, Is.Empty, "sin dato en el catálogo: solo el nombre");
            Assert.That(card.SceneShown, Is.False);
            card.Hide();

            // Teotônio, en el tramo a Puerto Heath (el 300), sí tiene dato.
            yield return MapMarkersTests.OpenApp(301);
            map = GameBootstrap.Current!.RiverMapView!;
            int teotonio = IndexOfPassing(map, "teotonio");
            Assert.That(teotonio, Is.GreaterThanOrEqualTo(0));
            Assert.That(map.TapPassing(teotonio), Is.True);
            Assert.That(map.Card!.TitleText, Is.EqualTo("Rápidos de Teotônio"));
            Assert.That(map.Card.FactText, Does.StartWith("Río arriba de Teotônio"));
            Assert.That(map.Card.FactKey, Is.EqualTo("map.stop.teotonio.dato_01"));
        }

        [UnityTest]
        public IEnumerator Settings_AboutTheAmazon_ListsTheRiverFactsSeen_WithTheirSource()
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 26;
            profile.Progress.MaxLevel = 25;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.AboutShown && settings.RiverFactsShown, Is.True);
            Assert.That(settings.RiverFactsTitle, Is.EqualTo("Datos del río"));
            Assert.That(settings.RiverFactCount, Is.EqualTo(2), "los datos de llegada del 10 y del 20");
            Assert.That(settings.RiverFactKey(0), Is.EqualTo("map.stop.estuario.dato_03"));
            Assert.That(settings.RiverFactPlace(0), Is.EqualTo("Santarém"));
            Assert.That(settings.RiverFactSource(0), Is.EqualTo("Fuente: Agência FAPESP"));
            Assert.That(settings.RiverFactKey(1), Is.EqualTo("map.stop.alter_do_chao.dato_01"));
            Assert.That(settings.RiverFactText(1), Is.Not.Empty);

            var keys = new System.Collections.Generic.List<string>();
            Assert.That(SettingsScreen.SeenRiverFacts(boot.RiverStops!, 9, keys), Is.EqualTo(0), "antes del 10, ninguno");
            Assert.That(SettingsScreen.SeenRiverFacts(boot.RiverStops!, 700, keys), Is.GreaterThan(30), "con todo el río, uno por dato sin repetir");
            Assert.That(keys.Count, Is.EqualTo(new System.Collections.Generic.HashSet<string>(keys).Count));
        }

        [UnityTest]
        public IEnumerator VeryHardLevels_GetTheNightLut_AndHardOnes_TheAfternoonLight()
        {
            yield return HudSetup.LoadPlaying(60);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.StopBackdrop.Light, Is.EqualTo(StopBackdrop.NightLut), "el 60 es Muy difícil");
            Assert.That(view.NightVeil.gameObject.activeSelf, Is.False, "la LUT sustituye al velo del paso 1");
            Color tint = view.PanoramaTint;
            Assert.That(tint.r, Is.LessThan(0.8f), "el panorama, de noche: " + tint);
            Assert.That(tint.b, Is.GreaterThan(tint.r), "luz de luna, fría");
            StringAssert.Contains("\"light\":\"night\"", view.LastStopProbe);

            yield return HudSetup.LoadPlaying(40);
            view = GameBootstrap.Current!.View!;
            Assert.That(view.StopBackdrop.Light, Is.EqualTo(StopBackdrop.AfternoonLut), "el 40 es Difícil: luz de tarde");
            Assert.That(view.PanoramaTint.r, Is.GreaterThan(view.PanoramaTint.b), "cálida");

            yield return HudSetup.LoadPlaying(41);
            view = GameBootstrap.Current!.View!;
            Assert.That(view.StopBackdrop.Light, Is.EqualTo(Color.white), "normal: sin luz de dificultad");
            Assert.That(StopBackdrop.DifficultyLightOf(LevelDefinition.LabelNormal), Is.EqualTo(Color.white));
        }

        private static int IndexOfPassing(RiverMapScreen map, string id)
        {
            for (int i = 0; i < map.PassingCount; i++)
            {
                if (map.PassingAt(i).Id == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
