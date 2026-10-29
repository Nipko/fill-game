#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de las miniaturas del Mercado y las ofertas (observación 4 de la jornada 6 y 7 de la jornada 5), a 360 × 780 dp y en
    /// español: <c>skins_01_market_cuias_todas</c> (pestaña Cuias con las cuatro skins, la del Encuentro puesta),
    /// <c>skins_02_market_cuias_faltan</c> (en el 46: la base y Santarém, y las que faltan con «faltan N»),
    /// <c>skins_03_offer_skin</c> (la tarjeta de la Cuia del Encuentro en el inicio, con la venta de skins encendida),
    /// <c>skins_04_offer_cantaro</c> (la tarjeta del Cántaro lleno), <c>skins_05_market_semillas</c> (la fila del Cántaro en
    /// Semillas), <c>skins_06_miniaturas_x3</c> (las cuatro fichas de <see cref="ProceduralArt.SkinThumb"/> lado a lado, cada píxel
    /// ×3, sobre el papel del Mercado) y <c>skins_07_mapa_premio</c> (la tarjeta de Flona Tapajós en el Mapa, con el Premio del
    /// mundo: la Cuia de Santarém). Explícito: necesita gráficos; los PNG quedan en <c>Logs/snapshots/es-419/</c> (se copian a
    /// <c>docs/art/mercado-skins/</c>).
    /// </summary>
    [Explicit("Capturas de las miniaturas de skins y del Cántaro; necesitan dispositivo gráfico.")]
    public sealed class MarketSkinSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetSessionOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        private static Profile ProfileAt(int nextLevel, int seeds)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Wallet.Seeds = seeds;
            return profile;
        }

        private static void JarWith(int seeds)
        {
            var store = new InMemoryOffersStore();
            store.Save(new OffersRecord { PiggySeeds = seeds, PiggyCap = 3000 });
            GameBootstrap.OffersStoreOverride = store;
        }

        private static IEnumerator Lobby(Profile profile)
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
        }

        [UnityTest]
        public IEnumerator CaptureCuiasTab()
        {
            string dir = SnapshotKit.Prepare();
            Profile profile = ProfileAt(141, 2450);
            profile.Entitlements.Skins.Add("af_skin_m02");
            profile.Entitlements.Skins.Add("af_skin_m03");
            profile.Entitlements.Skins.Add("af_skin_m04");
            profile.Entitlements.SkinEquipped = "af_skin_m03";
            yield return Lobby(profile);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.OpenMarket(MarketTab.Cuias), Is.True);
            SnapshotKit.AttachOverlay(boot.Market!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_01_market_cuias_todas");

            JarWith(1230);
            Profile early = ProfileAt(46, 800);
            early.Entitlements.Skins.Add("af_skin_m02");
            early.Entitlements.SkinEquipped = "af_skin_m02";
            yield return Lobby(early);
            boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.OpenMarket(MarketTab.Cuias), Is.True);
            SnapshotKit.AttachOverlay(boot.Market!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_02_market_cuias_faltan");

            boot.Market.SelectTab(MarketTab.Seeds);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_05_market_semillas");
        }

        [UnityTest]
        public IEnumerator CaptureSkinOffer()
        {
            string dir = SnapshotKit.Prepare();
            GameBootstrap.SkinSalesOverride = true; // Q-B5-1: la venta de skins, encendida para la captura
            GameBootstrap.SessionStoreOverride = new InMemorySessionStore(new SessionRecord { Sessions = 1 });
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(ProfileAt(41, 340));
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            Assert.That(boot.PressHome(), Is.True);
            boot.TickMarket(16f);
            Assert.That(boot.OfferCardView!.Visible, Is.True);
            Assert.That(boot.OfferCardView.Sku, Is.EqualTo("af_skin_m03"));
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(boot.OfferCardView.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_03_offer_skin");
        }

        [UnityTest]
        public IEnumerator CaptureJarCard()
        {
            string dir = SnapshotKit.Prepare();
            JarWith(3000);
            yield return Lobby(ProfileAt(60, 800));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.PressPiggy(), Is.True);
            SnapshotKit.AttachOverlay(boot.OfferCardView!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_04_offer_cantaro");
        }

        [UnityTest]
        public IEnumerator CaptureThumbsX3()
        {
            string dir = SnapshotKit.Prepare();
            yield return Lobby(ProfileAt(26, 0));
            ProceduralArt art = GameBootstrap.Current!.Art!;
            CuiaSkin[] skins = { CuiaSkin.Base, CuiaSkin.Santarem, CuiaSkin.Encuentro, CuiaSkin.Totuma };
            const int scale = 3;
            const int gap = 48;
            Texture2D first = art.SkinThumb(skins[0]).texture;
            int tw = first.width * scale;
            int th = first.height * scale;
            int w = (skins.Length * tw) + ((skins.Length + 1) * gap);
            int h = th + (2 * gap);
            var sheet = new Texture2D(w, h, TextureFormat.RGB24, false);
            var fill = new Color32[w * h];
            Color32 paper = Palette.UiPaper;
            for (int i = 0; i < fill.Length; i++)
            {
                fill[i] = paper;
            }

            for (int k = 0; k < skins.Length; k++)
            {
                Color32[] px = ReadTexture(art.SkinThumb(skins[k]).texture, out int sw, out int sh);
                int ox = gap + (k * (tw + gap));
                for (int y = 0; y < sh * scale; y++)
                {
                    for (int x = 0; x < sw * scale; x++)
                    {
                        Color32 c = px[((y / scale) * sw) + (x / scale)];
                        int at = ((gap + y) * w) + ox + x;
                        fill[at] = Color32.Lerp(fill[at], new Color32(c.r, c.g, c.b, 255), c.a / 255f);
                    }
                }
            }

            sheet.SetPixels32(fill);
            sheet.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "skins_06_miniaturas_x3.png"), sheet.EncodeToPNG());
            UnityEngine.Object.Destroy(sheet);
        }

        // Los píxeles de una textura que ya no se puede leer en la CPU (las de ProceduralArt): por una RenderTexture.
        private static Color32[] ReadTexture(Texture2D tex, out int w, out int h)
        {
            w = tex.width;
            h = tex.height;
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Color32[] px = copy.GetPixels32();
            UnityEngine.Object.Destroy(copy);
            return px;
        }

        [UnityTest]
        public IEnumerator CaptureMapPrize()
        {
            string dir = SnapshotKit.Prepare();
            yield return MapMarkersTests.OpenApp(26); // el Premio de Flona Tapajós (el 40): la Cuia de Santarém
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            Assert.That(map.OpenStopCard(3), Is.True, "la tarjeta de Flona Tapajós");
            map.Tick(16f);
            Assert.That(ProceduralArt.IsSkinThumb(map.Card!.PrizeCuiaImage.sprite, "af_skin_m02"), Is.True);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_07_mapa_premio");
        }
    }
}
