#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.IAP;
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
    /// Las miniaturas del Mercado y las ofertas en la escena <c>Game</c> real (observación 4 de la jornada 6 y 7 de la jornada 5 de la
    /// prueba completa): en la pestaña Cuias cada skin sale con su cuia (<see cref="ProceduralArt.SkinThumb"/>, la del tablero en
    /// pequeño con su borde, pie, banda, asa y vidrio, y con líquido), no con un círculo de su color; el Cántaro, en su fila y en su
    /// tarjeta, con su tinaja (<see cref="ProceduralArt.DockJar"/>, la del inicio), no con un círculo marrón; y el botón de cada
    /// skin dice su nombre a TalkBack.
    /// </summary>
    public sealed class MarketSkinThumbTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        private static readonly string[] AllSkins = { SkinRewards.BaseSkinId, "af_skin_m02", "af_skin_m03", "af_skin_m04" };

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetSessionOverrides();
            LaunchOptions.Reset();
        }

        private static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        private static IEnumerator LoadLobby(Profile profile)
        {
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
        }

        private static void JarWith(int seeds)
        {
            var store = new InMemoryOffersStore();
            store.Save(new OffersRecord { PiggySeeds = seeds, PiggyCap = 3000 });
            GameBootstrap.OffersStoreOverride = store;
        }

        [UnityTest]
        public IEnumerator CuiasTab_EachSkin_ShowsItsCuia_NotAPlainDisc()
        {
            Profile profile = ProfileAt(141); // las tres skins de los mundos ganadas (el 40, el 80 y el 140)
            profile.Entitlements.Skins.Add("af_skin_m02");
            profile.Entitlements.Skins.Add("af_skin_m03");
            profile.Entitlements.Skins.Add("af_skin_m04");
            profile.Entitlements.SkinEquipped = "af_skin_m03";
            yield return LoadLobby(profile);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenMarket(MarketTab.Cuias), Is.True);
            yield return null;
            MarketScreen market = boot.Market!;
            ProceduralArt art = boot.Art!;
            Assert.That(market.Rows.Count, Is.EqualTo(AllSkins.Length), "la base y las tres de los mundos");
            for (int i = 0; i < market.Rows.Count; i++)
            {
                string id = market.Rows[i].Id;
                Assert.That(id, Is.EqualTo(AllSkins[i]));
                UnityEngine.UI.Image icon = market.RowIcon(i);
                Assert.That(icon.sprite, Is.Not.SameAs(art.Disc), id + ": no el círculo liso");
                Assert.That(ProceduralArt.IsSkinThumb(icon.sprite, id), Is.True, id + ": su cuia (" + icon.sprite!.name + ")");
                Assert.That(icon.sprite, Is.SameAs(art.SkinThumb(CuiaSkin.For(id))), id + ": la miniatura guardada");
                Assert.That(icon.color, Is.EqualTo(Color.white), id + ": lleva sus colores, sin tinte");
                Assert.That(icon.preserveAspect, Is.True);
                Assert.That(icon.rectTransform.rect.height, Is.GreaterThan(36f), id + ": la cuia ocupa el alto de la fila");
            }

            // TalkBack: el botón «Ponerse» de una skin dice cuál.
            int equip = 0;
            Assert.That(market.Rows[equip].Action, Is.EqualTo(MarketAction.Equip));
            Assert.That(A11yLabels.TryGet(market.RowButton(equip).gameObject, out string label, out _), Is.True);
            Assert.That(label, Does.Contain(market.RowNameText(equip)), "el nombre de la skin");
            Assert.That(label, Does.Contain(market.RowButtonText(equip)), "y lo que hace el botón");
        }

        [UnityTest]
        public IEnumerator SkinThumbs_AreOnePerSkin_Cached_AndSmall()
        {
            yield return LoadLobby(ProfileAt(26));
            ProceduralArt art = GameBootstrap.Current!.Art!;
            for (int i = 0; i < AllSkins.Length; i++)
            {
                Sprite thumb = art.SkinThumb(CuiaSkin.For(AllSkins[i]));
                Assert.That(thumb, Is.SameAs(art.SkinThumb(CuiaSkin.For(AllSkins[i]))), "se hace una vez");
                Assert.That(thumb.texture.width, Is.LessThanOrEqualTo(256));
                Assert.That(thumb.texture.height, Is.LessThanOrEqualTo(256), "miniatura de 128-256 px como mucho (memoria, observación 6)");
                Assert.That(thumb.texture.height, Is.GreaterThan(thumb.texture.width), "una cuia de pie, no un círculo");
                for (int j = 0; j < i; j++)
                {
                    Assert.That(thumb, Is.Not.SameAs(art.SkinThumb(CuiaSkin.For(AllSkins[j]))), AllSkins[i] + " y " + AllSkins[j]);
                }
            }

            Assert.That(art.SkinThumb(null), Is.SameAs(art.SkinThumb(CuiaSkin.Base)), "null: la base");
        }

        [UnityTest]
        public IEnumerator Jar_InItsRow_AndInItsCard_IsTheClayJar_NotAPlainDisc()
        {
            JarWith(3000);
            yield return LoadLobby(ProfileAt(40));
            GameBootstrap boot = GameBootstrap.Current!;
            ProceduralArt art = boot.Art!;

            Assert.That(boot.PressPiggy(), Is.True);
            OfferCard card = boot.OfferCardView!;
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Sku, Is.EqualTo(StoreCatalog.Piggy));
            Assert.That(card.Icon.sprite, Is.SameAs(art.DockJar), "la tinaja del Cántaro, la del inicio");
            Assert.That(card.Icon.sprite, Is.Not.SameAs(art.Disc));
            Assert.That(card.Icon.color, Is.EqualTo(Color.white), "con sus colores, no teñida de barro");
            Assert.That(boot.PressMarketBack(), Is.True);

            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            MarketScreen market = boot.Market!;
            Assert.That(market.Rows[0].Id, Is.EqualTo(StoreCatalog.Piggy));
            Assert.That(market.RowIcon(0).sprite, Is.SameAs(art.DockJar), "la fila del Cántaro, con su tinaja");
        }
    }
}
