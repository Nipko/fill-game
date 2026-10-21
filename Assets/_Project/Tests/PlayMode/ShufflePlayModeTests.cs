#nullable enable
using System.Collections;
using AF.Ads;
using AF.Boosters;
using AF.Core.Board;
using AF.Economy;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Pororoca · Barajar en la escena (bloque B2; 04 §2.4 y §4; 07 §7.4): la ranura Barajar desde el 30, entre Pista y Revelar;
    /// la tarjeta del primer 30, que se cierra con un toque; la hoja de confirmación con 300 semillas; la ola de 1,2 s que captura
    /// los toques y cambia cada cuia al pasar; el cobro y el tope de 1 por nivel; con el stock, sin hoja.
    /// </summary>
    public sealed class ShufflePlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ShuffleOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.ShuffleOverride = null;
            LaunchOptions.Reset();
        }

        private static IEnumerator Level30(int seeds, int stock)
        {
            Profile profile = FunnelSetup.Profile(seeds);
            profile.Stock.Shuffle = stock;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(30, new MemoryProgressStore(30, 29));
        }

        private static void Click(Button button)
        {
            button.onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator Level30_Card_Confirm_Wave_AndTheLevelCap()
        {
            yield return Level30(1000, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 100);

            // La tarjeta del 30 (04 §4), una vez; un toque la cierra.
            Assert.That(view.ShuffleCard.Visible, Is.True, "tarjeta de la Pororoca en el primer 30");
            Assert.That(boot.Flow!.ShuffleCardDue, Is.False);
            Click(view.ShuffleCard.Root.GetComponentInChildren<Button>());
            Assert.That(view.ShuffleCard.Visible, Is.False);

            // La ranura, cuarta tras Marea, Cuia extra y Pista (07 §7.4).
            Assert.That(view.Tray.ShuffleShown, Is.True);
            Assert.That(view.Tray.SlotCount, Is.EqualTo(4));
            Assert.That(view.Tray.ShufflePriceShown, Is.EqualTo(300));

            BoardState before = boot.Flow.Session.Current;
            Click(view.Tray.ShuffleButton);
            Assert.That(view.ShuffleConfirm.Visible, Is.True, "04 §2.4: hoja de una línea con ≥ 200 semillas");
            Assert.That(view.ShuffleConfirm.Price, Is.EqualTo(300));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(1000), "abrir la hoja no cobra");

            Click(view.ShuffleConfirm.ConfirmButton);
            Assert.That(view.ShuffleConfirm.Visible, Is.False);
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(700));
            BoardState after = boot.Flow.Session.Current;
            Assert.That(after.Equals(before), Is.False);
            Assert.That(view.ShuffleWave.Playing, Is.True, "la ola de 1,2 s");
            Assert.That(view.ShufflePendingMask, Is.Not.EqualTo(0));

            PlayModeHelpers.Step(boot, (int)ShuffleWaveFx.DurationMs + 64);
            Assert.That(view.ShuffleWave.Playing, Is.False);
            Assert.That(view.ShufflePendingMask, Is.EqualTo(0));
            for (int i = 0; i < after.Count; i++)
            {
                Assert.That(view.Views[i].ShownCuia.Packed, Is.EqualTo(after[i].Packed), "cuia " + i + " pintada con la ola");
            }

            Assert.That(boot.Flow.Shuffle.State, Is.EqualTo(ShuffleState.UsedLevel));
            Assert.That(view.Tray.ShuffleLooksEnabled, Is.False, "1 por nivel");
        }

        [UnityTest]
        public IEnumerator WithStock_NoSheet_TheWaveGoesStraightAway()
        {
            yield return Level30(0, 1);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            boot.Flow!.MarkShuffleCardShown();
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.ShuffleCard.Visible, Is.False);
            Assert.That(view.Tray.ShuffleBadgeText, Does.Contain("+1"));

            Click(view.Tray.ShuffleButton);

            Assert.That(view.ShuffleConfirm.Visible, Is.False);
            Assert.That(view.ShuffleWave.Playing, Is.True);
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Shuffle), Is.EqualTo(0));
            PlayModeHelpers.Step(boot, (int)ShuffleWaveFx.DurationMs + 64);
            Assert.That(view.ShuffleWave.Playing, Is.False);
        }

        [UnityTest]
        public IEnumerator Level106_FiveSlots_MissingNotices_DoNotOverlap()
        {
            // 07 §7.4: con las 5 ranuras miden 56 dp. Nivel 106 con 50 semillas y sin video: Cuia extra, Pista, Barajar y Revelar
            // enseñan a la vez «Te faltan {n}», estrechos a su paso de ranura, a 12 sp y sin pisarse.
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(50));
            yield return PlayModeHelpers.LoadGame(106, new MemoryProgressStore(106, 105));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1000);
            BoosterTray tray = boot.View!.Tray;

            Assert.That(tray.SlotCount, Is.EqualTo(5));
            Assert.That(tray.SlotWidth, Is.GreaterThanOrEqualTo(BoosterTray.FiveSlotsDp));
            Assert.That(tray.ShuffleShowsMissing, Is.True);
            Assert.That(tray.ShuffleMissingText, Is.EqualTo("Te faltan 250"));
            Assert.That(tray.RevealShowsMissing, Is.True);
            Assert.That(WorldRect(tray.ShuffleMissingRect).Overlaps(WorldRect(tray.RevealMissingRect)), Is.False, "Barajar y Revelar");
            Assert.That(WorldRect(tray.ShuffleMissingRect).Overlaps(WorldRect(tray.HintMissingRect)), Is.False, "Pista y Barajar");
        }

        private static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator Level29_HasNoShuffleSlot()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1000));
            yield return PlayModeHelpers.LoadGame(29, new MemoryProgressStore(29, 28));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 100);

            Assert.That(boot.View!.Tray.ShuffleShown, Is.False);
            Assert.That(boot.View.ShuffleCard.Visible, Is.False);
        }
    }
}
