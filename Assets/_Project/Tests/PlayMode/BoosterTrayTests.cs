#nullable enable
using System;
using System.Collections;
using AF.Ads;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Bandeja de comodines en la escena (tarea B2-1; 07 §7.4; 04 §1.1, §2.1 y §2.2): en 1-7 solo Marea; desde el 8, Marea y
    /// Cuia extra con el regalo de Capi; la cuia añadida va a la fila más corta sin cambiar las filas (D-006); desde el 11,
    /// el cupo de Marea en el badge. Con la Luciérnaga (T14; 04 §2.5): 1, 2 y 3 ranuras de 64 dp con Revelar la última,
    /// recentradas, y los avisos «Te faltan {n}» de dos ranuras vecinas sin solaparse.
    /// </summary>
    public sealed class BoosterTrayTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Levels1To7_OnlyMarea_From8_MareaAndTheGiftedExtraCuia()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.View!.Tray.SlotCount, Is.EqualTo(1), "en 1-7 la ranura de Cuia extra no existe y la bandeja se centra");
            Assert.That(boot.View.Tray.ExtraCuiaShown, Is.False);
            Assert.That(boot.View.Tray.UndoBadgeText, Is.Empty, "Marea sin contador en 1-10");

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift), "04 §2.2: Capi trae una cuia en el 8");
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.Tray.SlotCount, Is.EqualTo(2));
            Assert.That(view.Tray.ExtraCuiaShown, Is.True);
            Assert.That(view.Tray.ExtraBadgeText, Is.EqualTo("+1"), "stock regalado");
            Assert.That(((RectTransform)view.ExtraCuiaButton.transform).rect.width, Is.GreaterThanOrEqualTo(48f));

            int rows = view.Layout!.Rows;
            int cuias = view.Views.Length;
            Assert.That(boot.PressExtraCuia(), Is.True);
            Assert.That(view.Views.Length, Is.EqualTo(cuias + 1));
            Assert.That(view.Relayouting, Is.True, "re-layout de 300 ms y entrada de 600 ms (02 §4.1)");
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.Relayouting, Is.False);
            Assert.That(view.Layout!.Rows, Is.EqualTo(rows), "las filas no cambian a mitad de nivel (D-006)");
            Assert.That(view.Views[cuias].Root.localScale.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(view.ExtraCuiaButton.interactable, Is.False, "1 por nivel");
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator From11_MareaShowsItsFreeQuota_AndWithoutQuotaOffersTheRefill()
        {
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            for (int i = 3; i >= 1; i--)
            {
                boot.TapCuia(0);
                boot.TapCuia(2);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
                PlayModeHelpers.Step(boot, 20);
                Assert.That(view.Tray.UndoBadgeText, Is.EqualTo(i.ToString(System.Globalization.CultureInfo.InvariantCulture)), "cupo gratis en el badge");
                Assert.That(boot.PressUndo(), Is.True);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
            }

            PlayModeHelpers.Step(boot, 20);
            Assert.That(view.Tray.UndoBadgeText, Is.EqualTo("+3"), "sin cupo: «+3» con el precio (sin fill no hay video)");
            Assert.That(boot.PressUndoRefillSeeds(), Is.False, "0 semillas: nada cambia");
        }

        [UnityTest]
        public IEnumerator RevealSlot_OneTwoAndThreeSlots_RecentersAndKeepsTheCanonicalOrder()
        {
            // T14; 07 §7.4: Revelar va siempre la última a la derecha y, sin ella, la bandeja se recentra conservando el orden.
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;
            AssertSlots(view, 1, tray.UndoButton);
            Assert.That(tray.RevealShown, Is.False, "nivel 7: sin Aguas turbias");

            // Marea y Revelar (solo la API de la bandeja: en la campaña no hay niveles turbios antes del 8).
            Assert.That(tray.SetRevealVisible(true), Is.True);
            AssertSlots(view, 2, tray.UndoButton, tray.RevealButton);
            Assert.That(tray.SetRevealVisible(true), Is.False, "sin cambio");
            Assert.That(tray.SetRevealVisible(false), Is.True);
            AssertSlots(view, 1, tray.UndoButton);
            Assert.That(tray.RevealButton.gameObject.activeInHierarchy, Is.False);

            // Nivel 100: Marea, Cuia extra y Revelar, de 64 dp; al irse Revelar quedan Marea y Cuia extra, recentradas.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            view = boot.View!;
            tray = view.Tray;
            AssertSlots(view, 3, tray.UndoButton, tray.ExtraCuiaButton, tray.RevealButton);
            Assert.That(tray.SetRevealVisible(false), Is.True);
            AssertSlots(view, 2, tray.UndoButton, tray.ExtraCuiaButton);
            Assert.That(tray.SetRevealVisible(true), Is.True);
            AssertSlots(view, 3, tray.UndoButton, tray.ExtraCuiaButton, tray.RevealButton);
        }

        [UnityTest]
        public IEnumerator MissingNotices_OfNeighbourSlots_DoNotOverlap()
        {
            // Nivel 106 con 50 semillas y sin video: Cuia extra (150) y Revelar (120) enseñan su precio en rojo y «Te faltan {n}»
            // a la vez; los dos avisos se estrechan a su paso de ranura y no se pisan.
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(50));
            yield return PlayModeHelpers.LoadGame(106, new MemoryProgressStore(106, 105));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1000);
            BoosterTray tray = boot.View!.Tray;
            Assert.That(tray.ExtraShowsMissing, Is.True);
            Assert.That(tray.RevealShowsMissing, Is.True);
            Assert.That(tray.RevealMissingText, Is.EqualTo("Te faltan 70"));
            Assert.That(WorldRect(tray.ExtraMissingRect).Overlaps(WorldRect(tray.RevealMissingRect)), Is.False, "sin solapes");
            Assert.That(tray.ExtraMissingWidth, Is.LessThan(72f));
            Assert.That(tray.RevealMissingWidth, Is.LessThan(72f));
        }

        // n ranuras de 64 dp en el orden dado, con 8 dp entre ellas, y la bandeja (16 + n·64 + (n − 1)·8 + 16) centrada.
        private static void AssertSlots(GameScreen view, int n, params Button[] slots)
        {
            BoosterTray tray = view.Tray;
            float width = 32f + (n * 64f) + ((n - 1) * 8f);
            Assert.That(tray.SlotCount, Is.EqualTo(n));
            Assert.That(tray.Width, Is.EqualTo(width).Within(0.01f));
            float x = ((view.CanvasWidth - width) * 0.5f) + 16f;
            for (int i = 0; i < slots.Length; i++)
            {
                var rt = (RectTransform)slots[i].transform;
                Assert.That(slots[i].gameObject.activeInHierarchy, Is.True, rt.name);
                Assert.That(rt.rect.width, Is.EqualTo(64f).Within(0.01f), rt.name + ": 64 dp");
                Assert.That(rt.anchoredPosition.x, Is.EqualTo(x).Within(0.01f), rt.name + ": en su sitio, centrada y sin huecos");
                x += 72f;
            }
        }

        private static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }

    /// <summary>Ganchos comunes de los tests del embudo: anuncios y compras inmediatos y un reloj fijo.</summary>
    internal static class FunnelSetup
    {
        /// <summary>2026-10-08 15:00 UTC = 10:00 en Bogotá.</summary>
        public static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        public static ManualClock Clock { get; private set; } = new ManualClock(T0, TimeSpan.FromHours(-5));

        public static ImmediatePurchasePresenter Purchases { get; private set; } = new ImmediatePurchasePresenter(true);

        public static ImmediateAdPresenter AdsPresenter { get; private set; } = new ImmediateAdPresenter();

        public static void Hooks()
        {
            LaunchOptions.Reset();
            Clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            Purchases = new ImmediatePurchasePresenter(true);
            AdsPresenter = new ImmediateAdPresenter();
            LaunchOptions.ClockOverride = Clock;
            LaunchOptions.AdPresenterOverride = AdsPresenter;
            LaunchOptions.PurchasePresenterOverride = Purchases;
        }

        /// <summary>Perfil con <paramref name="seeds"/> semillas, <paramref name="canoes"/> canoas y el progreso hasta el 30.</summary>
        public static Profile Profile(int seeds, int canoes = 5)
        {
            Profile profile = Wallet.NewProfile(Clock);
            profile.Wallet.Seeds = seeds;
            profile.Wallet.Canoes.Count = canoes;
            if (canoes < 5)
            {
                profile.Wallet.Canoes.NextRegenAt = T0 + TimeSpan.FromMinutes(18) + TimeSpan.FromSeconds(42);
            }

            return profile;
        }

        /// <summary>Juega 3 → 4 en el tablero 6 del tutorial (Aguas estancadas) y espera al modal (R15).</summary>
        public static void GetStuck(GameBootstrap boot)
        {
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            for (int elapsed = 0; elapsed < 5000 && boot.Flow!.Phase != FlowPhase.StuckModal; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
            PlayModeHelpers.Step(boot, 300); // el panel termina de subir (250 ms)
        }
    }
}
