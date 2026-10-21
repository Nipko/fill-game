#nullable enable
using System;
using System.Collections;
using AF.Boosters;
using AF.Economy;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El Salto del Boto en la escena (bloque B3; 04 §2.6 y §6.6 wireframe D; nota <c>docs/dev/salto-del-boto.md</c>): en el 31
    /// (tablero 6 del tutorial con <c>af_level_offset</c> = 25), al quinto fallo Aguas estancadas ofrece «Salto del Boto · 300»,
    /// la pausa y el banner también; la confirmación avisa si se pierde una Cuia extra y lleva al 32 con 300 semillas menos, sin
    /// canoa, y el Mapa pone a Rosa en el 31. Con progreso en memoria (<see cref="MemorySkipStore"/>) el salto no sube su
    /// <c>max_level</c>: aquí no importa (el 32 no es un 10k ni elegible). Necesita <c>B3-model.diff</c> y <c>B3-ui.diff</c>.
    /// </summary>
    public sealed class SkipPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameBootstrap.ResetSkipOverrides();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetSkipOverrides();
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator FifthFail_OffersTheSkip_InTheModalThePauseAndTheBanner_AndItLeadsTo32()
        {
            Profile profile = FunnelSetup.Profile(1000);
            profile.Wallet.Canoes.InfiniteUntil = FunnelSetup.T0 + TimeSpan.FromHours(2); // «Rendirse» sin ¿Seguro?
            var progress = new MemoryProgressStore(31, 30);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(31, progress);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            Assert.That(boot.SkipStore, Is.InstanceOf<MemorySkipStore>(), "el progreso de prueba es en memoria");

            for (int fail = 0; fail < 5; fail++)
            {
                FunnelSetup.GetStuck(boot);
                Assert.That(view.Stuck.SkipButton.gameObject.activeSelf, Is.False, "con " + fail + " fallos no hay rama D");
                Assert.That(boot.Flow!.PressGiveUp(), Is.True);
                Assert.That(boot.Flow.PressRetry(), Is.True);
                boot.TickSkipBanner();
                Assert.That(boot.SkipBannerView!.Visible, Is.EqualTo(fail == 4), "el banner, solo con el salto elegible");
                boot.TapAnywhere();
                PlayModeHelpers.Step(boot, 50);
            }

            // Banner: con el tablero sin vertidos; un toque abre la confirmación.
            boot.TickSkipBanner();
            Assert.That(boot.SkipBannerView!.Visible, Is.True);
            Assert.That(boot.SkipBannerView.Text, Is.EqualTo(SkipText.Banner(300)));
            Assert.That(boot.SkipBannerView.RosaSprite, Is.Not.Null, "Rosa, pintada al enseñar el banner");
            Assert.That(boot.SkipBannerView.Interactable, Is.True);
            Assert.That(boot.SkipBannerView.MissingText, Is.Null);

            // Sin saldo: el banner sigue, en gris y con «Te faltan {n} semillas» (04 §1); el toque no abre nada.
            while (boot.Wallet!.Seeds >= 300)
            {
                Assert.That(boot.Wallet.Apply(Transaction.Buy(SeedSink.ExtraCuia)).IsApplied, Is.True);
            }

            int poorSeeds = boot.Wallet.Seeds;
            boot.TickSkipBanner();
            Assert.That(boot.SkipBannerView.Visible, Is.True, "elegible sin saldo: en gris");
            Assert.That(boot.SkipBannerView.Interactable, Is.False);
            Assert.That(boot.SkipBannerView.MissingText, Is.EqualTo(UiText.MissingSeeds(300 - poorSeeds)));
            Assert.That(boot.PressSkip(), Is.False);
            Assert.That(boot.SkipConfirmView!.Visible, Is.False);
            Assert.That(boot.Wallet.Apply(Transaction.Earn(SeedSource.Calendar, 1000 - poorSeeds)).IsApplied, Is.True);
            boot.TickSkipBanner();
            Assert.That(boot.SkipBannerView.Interactable, Is.True, "con saldo, otra vez lista");
            Assert.That(boot.SkipBannerView.MissingText, Is.Null);

            // Pausa: «Salto del Boto» con su precio; abre la confirmación y cierra la pausa; la X vuelve al juego.
            view.PauseButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.True);
            Assert.That(view.Pause.SkipButton.gameObject.activeSelf, Is.True, "04 §2.6: Rosa también en la pausa");
            Assert.That(view.Pause.SkipRosaSprite, Is.Not.Null, "Rosa, pintada al enseñar la pausa con el Salto");
            view.Pause.SkipButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.False);
            Assert.That(boot.SkipConfirmView.Visible, Is.True);
            Assert.That(boot.SkipConfirmView.RosaSprite, Is.Not.Null, "Rosa, pintada al abrir la confirmación");
            boot.TickSkipBanner();
            Assert.That(boot.SkipBannerView.Visible, Is.False, "con la confirmación abierta, sin banner");
            boot.SkipConfirmView.CloseButton.onClick.Invoke();
            Assert.That(boot.SkipConfirmView.Visible, Is.False);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(31), "la X no salta");

            // Rama D en Aguas estancadas.
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.SkipButton.gameObject.activeSelf, Is.True, "rama D");
            Assert.That(view.Stuck.SkipPriceShown, Is.EqualTo(300));
            Assert.That(view.Stuck.SkipNoteText, Is.EqualTo(SkipText.Tagline));
            view.Stuck.SkipButton.onClick.Invoke();
            Assert.That(boot.SkipConfirmView.Visible, Is.True);
            Assert.That(boot.SkipConfirmView.PriceShown, Is.EqualTo(300));
            Assert.That(boot.SkipConfirmView.WarnsExtraCuia, Is.False, "sin Cuia extra aplicada, sin aviso");
            boot.SkipConfirmView.CloseButton.onClick.Invoke();
            Assert.That(view.StuckVisible, Is.True, "la X vuelve al modal");

            // Con Cuia extra aplicada (Marea para salir del bloqueo, «+1 cuia»), la confirmación avisa de que se pierde (04 §2.6 c).
            Assert.That(boot.PressUndo(), Is.True);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.PressExtraCuia(), Is.True);
            Assert.That(boot.Flow.SkipLosesExtraCuia, Is.True);
            view.PauseButton.onClick.Invoke();
            view.Pause.SkipButton.onClick.Invoke();
            Assert.That(boot.SkipConfirmView.Visible, Is.True);
            Assert.That(boot.SkipConfirmView.WarnsExtraCuia, Is.True, "«Perderás la Cuia extra de este nivel»");
            int seeds = boot.Wallet!.Seeds;
            int canoes = boot.Wallet.Canoes.Count;
            boot.SkipConfirmView.ConfirmButton.onClick.Invoke();

            Assert.That(boot.SkipConfirmView.Visible, Is.False);
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(32), "Rosa lleva al siguiente nivel");
            Assert.That(view.StuckVisible, Is.False);
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(seeds - 300));
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(canoes), "sin canoa");
            Assert.That(progress.IsSkipped(31), Is.True, "el Mapa pone a Rosa en la boya del 31");
            Assert.That(progress.NextLevel, Is.EqualTo(32));
            Assert.That(boot.Flow.Attempt.FailsLevel, Is.Zero);
            boot.TickSkipBanner();
            Assert.That(boot.SkipBannerView.Visible, Is.False, "en el 32 no es elegible");
        }
    }
}
