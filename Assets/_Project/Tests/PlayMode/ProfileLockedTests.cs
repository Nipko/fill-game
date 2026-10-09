#nullable enable
using System;
using System.Collections;
using AF.Boosters;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Perfil en solo lectura (revisión de la pista B, punto 5; 08 §4.4: con un perfil de un esquema más nuevo o una
    /// migración fallida «se conserva el perfil anterior»). El almacén no deja guardar, así que la sesión se juega con un
    /// perfil en blanco que nunca se guarda: al arrancar se avisa con «Actualiza Tintalú» y no se ofrece ninguna compra (ni
    /// Puñado ni Balsa en Aguas estancadas, ni Canoas infinitas en Sin canoas), porque lo comprado se perdería. Es la medida
    /// mínima [P]; la pantalla de bloqueo (como <c>biz_min_app_version</c> de 08 §9) la decide el dueño.
    /// </summary>
    public sealed class ProfileLockedTests
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
        public IEnumerator TooNewProfile_WarnsToUpdate_PlaysWithoutSaving_AndSellsNothing()
        {
            var store = new ReadOnlyProfileStore(ProfileLoadStatus.TooNew);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.ProfileLocked, Is.True);
            Assert.That(boot.LockedNotice, Is.Not.Null);
            Assert.That(boot.LockedNotice!.Visible, Is.True, "aviso «Actualiza Tintalú» al arrancar");
            Assert.That(boot.LockedNotice.BodyText, Is.EqualTo(UiText.ProfileLockedTooNew));
            Assert.That(boot.Services!.PurchasesBlocked, Is.True);
            boot.LockedNotice.PlayButton.onClick.Invoke();
            Assert.That(boot.LockedNotice.Visible, Is.False, "se puede seguir jugando sin guardar");

            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot); // perfil en blanco: 0 semillas (rama B)
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Disabled));
            Assert.That(view.Stuck.HandfulButton.gameObject.activeSelf, Is.False, "sin Puñado");
            Assert.That(view.Stuck.BalsaButton.gameObject.activeSelf, Is.False, "sin Balsa");
            Assert.That(boot.Services.StoreLedger.BalsaShownToday, Is.EqualTo(0), "ni cuenta su impresión");
            Assert.That(boot.PressHandful(), Is.False, "la compra no se lanza");
            Assert.That(boot.PressBalsa(), Is.False);
            Assert.That(FunnelSetup.Purchases.Presented, Is.EqualTo(0), "ninguna «Compra simulada»");

            // Sin canoas: sin «Canoas infinitas 1 h».
            boot.PressGiveUp();
            boot.ConfirmGiveUp();
            while (boot.Wallet!.Canoes.Count > 0)
            {
                boot.Wallet.Apply(AF.Economy.Transaction.HardFail(31));
            }

            boot.PressRetry();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            PlayModeHelpers.Step(boot, 20);
            Assert.That(view.NoCanoes.InfiniteButton.gameObject.activeSelf, Is.False, "sin Canoas infinitas");
            Assert.That(view.NoCanoes.RefillButton.gameObject.activeSelf, Is.True, "Llenar canoas (semillas) sigue");
            Assert.That(boot.PressInfiniteCanoes(), Is.False);
            Assert.That(FunnelSetup.Purchases.Presented, Is.EqualTo(0));
            Assert.That(store.SaveAttempts, Is.GreaterThan(0), "se intentó guardar…");
            Assert.That(boot.SaveBatch!.PendingSave, Is.True, "…pero el almacén protege el perfil: lo jugado solo vive en memoria");
        }

        [UnityTest]
        public IEnumerator MigrationFailed_ShowsItsOwnText()
        {
            LaunchOptions.ProfileStoreOverride = new ReadOnlyProfileStore(ProfileLoadStatus.MigrationFailed);
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ProfileLocked, Is.True);
            Assert.That(boot.LockedNotice!.Visible, Is.True);
            Assert.That(boot.LockedNotice.BodyText, Is.EqualTo(UiText.ProfileLockedMigration));
        }

        [UnityTest]
        public IEnumerator AHealthyProfile_HasNoNotice_AndSells()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ProfileLocked, Is.False);
            Assert.That(boot.LockedNotice, Is.Null);
            Assert.That(boot.Services!.PurchasesBlocked, Is.False);
        }
    }

    /// <summary>Almacén que protege un perfil que esta build no puede leer: no hay perfil y no deja guardar (como <c>LocalProfileStore</c>).</summary>
    internal sealed class ReadOnlyProfileStore : IProfileStore
    {
        private readonly ProfileLoadStatus _status;

        public ReadOnlyProfileStore(ProfileLoadStatus status)
        {
            _status = status;
        }

        public int SaveAttempts { get; private set; }

        public ProfileLoadResult Load()
        {
            return new ProfileLoadResult(_status, null, false, "perfil de prueba en solo lectura");
        }

        public void Save(Profile profile)
        {
            SaveAttempts++;
            throw new InvalidOperationException("Perfil protegido: " + _status);
        }

        public void Delete()
        {
        }
    }
}
