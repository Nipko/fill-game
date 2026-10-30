#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Economy;
using AF.IAP;
using AF.Localization;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El Mercado más visible (observación 24 de la jornada 6 de la prueba completa, decisión del dueño del 2026-10-30), en la escena
    /// <c>Game</c> real:
    /// <list type="bullet">
    /// <item>las pestañas son botones: cada una con su pastilla (borde y relleno), la elegida con el acento y la letra crema, las
    /// demás en tono suave, sin subrayado, con un área táctil de 48 dp o más y TalkBack diciendo cuál está elegida;</item>
    /// <item>en la pestaña Cuias, tocar la tarjeta de una skin abre la vista previa con la cuia en grande, su nombre y el botón de la
    /// fila; se cierra con la X, tocando fuera y con Atrás, y suelta la textura grande al cerrarse;</item>
    /// <item>la entrada del Mercado en la barra de abajo del inicio lleva el puesto (<see cref="ProceduralArt.IconStall"/>) y el rótulo
    /// «Mercado» / «Market» / «Mercado», en una línea y sin cortarse a 320 dp con la letra al 100 % y al 130 %.</item>
    /// </list>
    /// </summary>
    public sealed class MarketVisibleTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedPxPerDp = null;
            GameScreen.SimulatedScreen = null;
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetSessionOverrides();
            Fx130Walk.TearDown();
            LaunchOptions.Reset();
        }

        private static Profile SkinsProfile()
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 141; // las tres skins de los mundos ganadas (el 40, el 80 y el 140)
            profile.Progress.MaxLevel = 140;
            profile.Wallet.Seeds = 2450;
            profile.Entitlements.Skins.Add("af_skin_m02");
            profile.Entitlements.Skins.Add("af_skin_m03");
            profile.Entitlements.Skins.Add("af_skin_m04");
            profile.Entitlements.SkinEquipped = "af_skin_m03";
            return profile;
        }

        private static IEnumerator LoadLobby(Profile profile)
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
        public IEnumerator Tabs_AreChips_WithAccentFill_AndA48dpTouchArea([Values(360, 320)] int width, [Values(100, 130)] int percent)
        {
            Fx130Walk.SetUp();
            if (width == 320)
            {
                GameScreen.SimulatedPxPerDp = Canvas320Tests.Density;
            }

            yield return Fx130Walk.Load(LocLocale.SourceLocale, percent / 100f, width == 320 ? Canvas320Tests.Screen320 : (Vector4?)null);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            MarketScreen market = boot.Market!;
            ProceduralArt art = boot.Art!;
            string tag = width.ToString(CultureInfo.InvariantCulture) + " dp al " + percent.ToString(CultureInfo.InvariantCulture) + " %";
            foreach (MarketTab selected in new[] { MarketTab.Seeds, MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds })
            {
                market.SelectTab(selected);
                yield return null;
                Canvas.ForceUpdateCanvases();
                int shown = 0;
                foreach (MarketTab tab in new[] { MarketTab.Seeds, MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds })
                {
                    if (!market.TabShown(tab))
                    {
                        continue;
                    }

                    shown++;
                    string what = tag + ", " + tab + " con " + market.Tab + " elegida";
                    RectTransform button = (RectTransform)market.TabButton(tab).transform;
                    Assert.That(button.rect.height, Is.GreaterThanOrEqualTo(MarketScreen.TabDp - 0.01f), what + ": área táctil de 48 dp de alto (07 §7.8)");
                    Assert.That(button.rect.width, Is.GreaterThanOrEqualTo(MarketScreen.TabDp - 0.01f), what + ": y de ancho");
                    Image chip = market.TabChip(tab);
                    Image edge = market.TabChipEdge(tab);
                    Assert.That(chip.gameObject.activeInHierarchy && edge.gameObject.activeInHierarchy, Is.True, what + ": la pastilla se ve");
                    Assert.That(chip.sprite, Is.SameAs(art.RoundedRect), what + ": pastilla redondeada");
                    Assert.That(chip.raycastTarget, Is.False, what + ": el toque es del botón");
                    bool isSelected = tab == market.Tab;
                    Assert.That(chip.color, Is.EqualTo(isSelected ? MarketScreen.TabSelectedFill : MarketScreen.TabIdleFill), what + ": relleno");
                    Assert.That(edge.color, Is.Not.EqualTo(chip.color), what + ": con borde");
                    TextMeshProUGUI label = market.TabLabel(tab);
                    Assert.That(label.color, Is.EqualTo(isSelected ? MarketScreen.TabSelectedText : MarketScreen.TabIdleText), what + ": letra");
                    Assert.That((label.fontStyle & FontStyles.Underline) == 0, Is.True, what + ": sin subrayado");
                    Assert.That(ColorMath.ContrastRatio(label.color, chip.color), Is.GreaterThanOrEqualTo(4.5f), what + ": contraste ≥ 4,5:1 (02 §12)");

                    // La pastilla, dentro de su área táctil, y el texto dibujado dentro de la pastilla.
                    Rect chipRect = Local(edge.rectTransform, button);
                    Rect buttonRect = button.rect;
                    Assert.That(chipRect.xMin >= buttonRect.xMin - 0.5f && chipRect.xMax <= buttonRect.xMax + 0.5f
                        && chipRect.yMin >= buttonRect.yMin - 0.5f && chipRect.yMax <= buttonRect.yMax + 0.5f, Is.True, what + ": la pastilla cabe en su botón");
                    label.ForceMeshUpdate();
                    TMP_TextInfo info = label.textInfo;
                    for (int c = 0; c < info.characterCount; c++)
                    {
                        if (!info.characterInfo[c].isVisible)
                        {
                            continue;
                        }

                        float a = button.InverseTransformPoint(label.rectTransform.TransformPoint(info.characterInfo[c].bottomLeft)).x;
                        float b = button.InverseTransformPoint(label.rectTransform.TransformPoint(info.characterInfo[c].topRight)).x;
                        Assert.That(Mathf.Min(a, b) >= chipRect.xMin - 0.5f && Mathf.Max(a, b) <= chipRect.xMax + 0.5f, Is.True, what + ": la letra dentro de la pastilla");
                    }

                    Assert.That(A11yLabels.TryGet(market.TabButton(tab).gameObject, out string spoken, out bool spokenSelected), Is.True, what + ": TalkBack");
                    Assert.That(spoken, Is.EqualTo(label.text), what + ": TalkBack dice la pestaña");
                    Assert.That(spokenSelected, Is.EqualTo(isSelected), what + ": y si está elegida");
                }

                Assert.That(shown, Is.GreaterThanOrEqualTo(2), tag + ": al menos dos pestañas");
            }
        }

        [UnityTest]
        public IEnumerator SkinCard_OpensTheBigPreview_AndXOutsideAndBackCloseIt()
        {
            yield return LoadLobby(SkinsProfile());
            GameBootstrap boot = GameBootstrap.Current!;
            ProceduralArt art = boot.Art!;
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            MarketScreen market = boot.Market!;
            yield return null;
            Assert.That(market.OpenPreview(0), Is.False, "Semillas: no hay vista previa");
            Assert.That(market.RowCardButton(0).gameObject.activeSelf, Is.False, "ni área de tarjeta fuera de Cuias");

            market.SelectTab(MarketTab.Cuias);
            yield return null;
            string? announced = null;
            market.PreviewOpened += name => announced = name;
            int row = 1; // af_skin_m02: «Poner»
            Assert.That(market.Rows[row].Id, Is.EqualTo("af_skin_m02"));
            Assert.That(market.RowCardButton(row).gameObject.activeInHierarchy, Is.True, "la tarjeta de la skin se toca");
            Assert.That(A11yLabels.TryGet(market.RowCardButton(row).gameObject, out string cardSpoken, out _), Is.True);
            Assert.That(cardSpoken, Is.EqualTo(market.RowNameText(row)), "TalkBack: el nombre de la skin");
            float thumbH = market.RowIcon(row).rectTransform.rect.height;

            // Abrir: la cuia en grande, su nombre y el botón de la fila; TalkBack anuncia el nombre.
            market.RowCardButton(row).onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(market.PreviewOpen, Is.True);
            Assert.That(market.PreviewSkinId, Is.EqualTo("af_skin_m02"));
            Assert.That(market.PreviewTitle, Is.EqualTo(market.RowNameText(row)), "el nombre de la skin");
            Assert.That(announced, Is.EqualTo(market.RowNameText(row)), "TalkBack anuncia el nombre");
            Sprite? big = market.PreviewCuia.sprite;
            Assert.That(big, Is.Not.Null);
            Assert.That(big!.name, Is.EqualTo(ProceduralArt.SkinPreviewPrefix + "af_skin_m02"));
            Assert.That(big, Is.SameAs(art.CurrentSkinPreview));
            Assert.That(big, Is.Not.SameAs(art.SkinThumb(CuiaSkin.For("af_skin_m02"))), "no la miniatura estirada");
            Assert.That(big.texture.height, Is.GreaterThan(art.SkinThumb(CuiaSkin.For("af_skin_m02")).texture.height * 2), "pintada en grande");
            float bigH = market.PreviewCuia.rectTransform.rect.height;
            Assert.That(bigH, Is.GreaterThanOrEqualTo(MarketScreen.PreviewCuiaMinDp), "a buen tamaño");
            Assert.That(bigH, Is.GreaterThan(thumbH * 1.8f), "mucho más grande que la miniatura (" + bigH + " frente a " + thumbH + " dp)");
            Assert.That(market.PreviewButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(market.PreviewButtonText, Is.EqualTo(market.RowButtonText(row)), "el mismo botón que la fila");
            Assert.That(A11yLabels.TryGet(market.PreviewButton.gameObject, out string ctaSpoken, out _), Is.True);
            Assert.That(ctaSpoken, Does.Contain(market.PreviewTitle).And.Contain(market.PreviewButtonText), "TalkBack: «Cuia …, Poner»");
            AssertInsideSafeArea(market);

            // Con la vista previa delante, la sonda solo da lo suyo.
            var ids = new List<string>();
            var rects = new List<RectTransform>();
            market.VisibleButtons(ids, rects);
            Assert.That(ids, Does.Contain("market_preview_close").And.Contain("market_preview_cta"));
            Assert.That(ids, Does.Not.Contain("market_close"));

            // La X.
            market.PreviewCloseButton.onClick.Invoke();
            yield return null;
            Assert.That(market.PreviewOpen, Is.False, "la X la cierra");
            Assert.That(market.Visible, Is.True, "el Mercado sigue abierto");
            Assert.That(art.CurrentSkinPreview, Is.Null, "suelta la cuia en grande (memoria)");

            // Tocar fuera.
            Assert.That(market.OpenPreview(row), Is.True);
            market.PreviewOutsideButton.onClick.Invoke();
            yield return null;
            Assert.That(market.PreviewOpen, Is.False, "tocar fuera la cierra");

            // Atrás: primero la vista previa, después el Mercado.
            Assert.That(market.OpenPreview(row), Is.True);
            Assert.That(boot.PressMarketBack(), Is.True);
            Assert.That(market.PreviewOpen, Is.False, "Atrás cierra la vista previa");
            Assert.That(market.Visible, Is.True, "y no el Mercado");
            Assert.That(boot.PressMarketBack(), Is.True);
            Assert.That(market.Visible, Is.False, "el segundo Atrás cierra el Mercado");
            Assert.That(boot.PressMarketBack(), Is.False);
        }

        [UnityTest]
        public IEnumerator PreviewButton_EquipsTheSkin_AndSaysEquipped()
        {
            yield return LoadLobby(SkinsProfile());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenMarket(MarketTab.Cuias), Is.True);
            MarketScreen market = boot.Market!;
            yield return null;
            int row = 1;
            Assert.That(market.Rows[row].Action, Is.EqualTo(MarketAction.Equip));
            Assert.That(market.OpenPreview(row), Is.True);
            market.PreviewButton.onClick.Invoke();
            yield return null;
            Assert.That(market.PreviewOpen, Is.True, "sigue abierta tras ponerla");
            Assert.That(market.Rows[row].Action, Is.EqualTo(MarketAction.Equipped), "la skin ya está puesta");
            Assert.That(market.PreviewButtonText, Is.EqualTo(MarketText.SkinEquipped), "«Puesta»");
            Assert.That(market.PreviewButton.interactable, Is.False, "en gris");

            // Cerrar el Mercado con la vista previa abierta la cierra también.
            market.CloseButton.onClick.Invoke();
            yield return null;
            Assert.That(market.PreviewOpen, Is.False);
            Assert.That(boot.Art!.CurrentSkinPreview, Is.Null);
        }

        [UnityTest]
        public IEnumerator LobbyEntry_HasTheStall_AndItsLabel_WithoutCutting(
            [Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent, [Values(360, 320)] int width)
        {
            Fx130Walk.SetUp();
            if (width == 320)
            {
                GameScreen.SimulatedPxPerDp = Canvas320Tests.Density;
            }

            float scale = percent / 100f;
            yield return Fx130Walk.Load(locale, scale, width == 320 ? Canvas320Tests.Screen320 : (Vector4?)null);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(Loc.Locale, Is.EqualTo(locale));
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            yield return null;
            boot.TickMarket(16f);
            Canvas.ForceUpdateCanvases();
            MarketLobbyIcons icons = boot.MarketIcons!;
            string tag = locale + " al " + percent.ToString(CultureInfo.InvariantCulture) + " % a " + width.ToString(CultureInfo.InvariantCulture) + " dp";
            Assert.That(icons.MarketShown, Is.True, tag + ": la entrada del Mercado");
            Assert.That(icons.MarketGlyph.sprite, Is.SameAs(boot.Art!.IconStall), tag + ": el puesto, no la semilla");
            string expected = locale == LocLocale.English ? "Market" : "Mercado";
            TextMeshProUGUI label = icons.MarketLabel;
            Assert.That(label.text, Is.EqualTo(expected), tag + ": el rótulo");
            Assert.That(A11yLabels.TryGet(icons.MarketButton.gameObject, out string spoken, out _), Is.True);
            Assert.That(spoken, Is.EqualTo(expected), tag + ": TalkBack");

            label.ForceMeshUpdate();
            Assert.That(label.textInfo.lineCount, Is.EqualTo(1), tag + ": en una línea");
            Assert.That(label.isTextOverflowing, Is.False, tag + ": sin cortarse");
            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * UiBuilder.TextScale) - 0.01f), tag + ": nunca por debajo de 12 sp × escala (02 §12)");
            Assert.That(new LocInventory(locale, scale).ProblemsOf(label), Is.Empty, tag + ": sin problemas de texto");

            // Los glifos, dentro de la pantalla y a la izquierda de «Inicio»; la caja del rótulo, dentro del área táctil.
            RectTransform root = (RectTransform)icons.Canvas.transform;
            Rect screen = root.rect;
            Rect home = Local(boot.Lobby!.HomeTabRect, root);
            TMP_TextInfo info = label.textInfo;
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            for (int c = 0; c < info.characterCount; c++)
            {
                if (!info.characterInfo[c].isVisible)
                {
                    continue;
                }

                minX = Mathf.Min(minX, root.InverseTransformPoint(label.rectTransform.TransformPoint(info.characterInfo[c].bottomLeft)).x);
                maxX = Mathf.Max(maxX, root.InverseTransformPoint(label.rectTransform.TransformPoint(info.characterInfo[c].topRight)).x);
            }

            Assert.That(maxX, Is.GreaterThan(minX), tag + ": hay texto dibujado");
            Assert.That(minX, Is.GreaterThanOrEqualTo(screen.xMin), tag + ": no se sale por la izquierda");
            Assert.That(maxX, Is.LessThanOrEqualTo(home.xMin), tag + ": no llega a «Inicio»");
            Rect labelRect = Local(label.rectTransform, root);
            Rect button = Local(icons.MarketRect, root);
            Assert.That(button.height, Is.GreaterThanOrEqualTo(48f), tag + ": área táctil (07 §7.8)");
            Assert.That(labelRect.yMin, Is.GreaterThanOrEqualTo(button.yMin - 0.5f), tag + ": el rótulo dentro del área táctil");
            Assert.That(labelRect.yMax, Is.LessThanOrEqualTo(button.yMax + 0.5f), tag + ": el rótulo dentro del área táctil");
            // La barra de pestañas empieza 12 dp bajo lo alto de «Inicio», mide 64 dp (LobbyScreen) y acaba en la zona segura de abajo: el
            // rótulo, sobre la barra (y hacia arriba en estas coordenadas; la pantalla simulada no cambia el lienzo real de los tests).
            float barBottom = home.yMax - 12f - 64f;
            Assert.That(labelRect.yMin, Is.GreaterThanOrEqualTo(barBottom - 0.5f), tag + ": sobre la barra, fuera de la zona de gestos");
            TestContext.WriteLine("AF_MARKET_NAV " + tag + ": «" + label.text + "» a " + label.fontSize.ToString("0.#", CultureInfo.InvariantCulture)
                + " dp en " + labelRect.width.ToString("0.#", CultureInfo.InvariantCulture) + " dp (glifos " + (maxX - minX).ToString("0.#", CultureInfo.InvariantCulture) + " dp)");
        }

        private static void AssertInsideSafeArea(MarketScreen market)
        {
            RectTransform panel = market.PreviewPanel;
            float top = -panel.anchoredPosition.y;
            float bottom = top + panel.sizeDelta.y;
            float left = panel.anchoredPosition.x;
            Assert.That(top, Is.GreaterThanOrEqualTo(market.SafeTopDp - 0.5f), "la vista previa bajo la zona segura de arriba");
            Assert.That(bottom, Is.LessThanOrEqualTo(market.AreaHeight - market.SafeBottomDp + 0.5f), "y sobre la de abajo");
            Assert.That(left, Is.GreaterThanOrEqualTo(-0.5f));
            Assert.That(left + panel.sizeDelta.x, Is.LessThanOrEqualTo(market.AreaWidth + 0.5f));
        }

        // Rectángulo de rt en las coordenadas locales de root.
        private static Rect Local(RectTransform rt, RectTransform root)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 a = root.InverseTransformPoint(corners[0]);
            Vector3 b = root.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
    }

    /// <summary>Capturas del Mercado más visible (observación 24 de la jornada 6) en <c>Logs/snapshots/es-419/</c>.</summary>
    [Explicit("Capturas del Mercado más visible; necesitan dispositivo gráfico.")]
    public sealed class MarketVisibleSnapshotTests
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

        [UnityTest]
        public IEnumerator CaptureLobbyMarketAndPreview()
        {
            string dir = SnapshotKit.Prepare();
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 141;
            profile.Progress.MaxLevel = 140;
            profile.Wallet.Seeds = 2450;
            profile.Entitlements.Skins.Add("af_skin_m02");
            profile.Entitlements.Skins.Add("af_skin_m03");
            profile.Entitlements.Skins.Add("af_skin_m04");
            profile.Entitlements.SkinEquipped = "af_skin_m03";
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(boot.MarketIcons!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            boot.TickMarket(16f);
            yield return SnapshotKit.Wait(0.1f);
            SnapshotKit.Save(dir, "market_visible_01_lobby");

            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            SnapshotKit.AttachOverlay(boot.Market!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "market_visible_02_seeds");

            boot.Market.SelectTab(MarketTab.Cuias);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "market_visible_03_cuias");

            Assert.That(boot.Market.OpenPreview(1), Is.True);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "market_visible_04_preview");
        }
    }
}
