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

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Bandeja de comodines en la escena (tarea B2-1; 07 §7.4; 04 §1.1, §2.1 y §2.2): en 1-7 solo Marea; desde el 8, Marea y
    /// Cuia extra con el regalo de Capi; la cuia añadida va a la fila más corta sin cambiar las filas (D-006); desde el 11,
    /// el cupo de Marea en el badge.
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
