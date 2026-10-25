#nullable enable
using System;
using System.Collections;
using AF.Backend;
using AF.Backend.Contract;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// F3-5 en la escena <c>Game</c> real (D-035; plan del backend §9; 08 §10.3 y §11), con la tienda verificada y el backend falso del
    /// editor: el Mercado y Aguas estancadas dicen «Pendiente de confirmar» y «No se pudo confirmar la compra» y lo pendiente se acredita
    /// una sola vez al verificarse; sin backend (release sin URL) el Mercado no vende y lo dice; al arrancar, <c>restoreLedger</c>
    /// reacredita lo que falte una vez; y en Ajustes, el código de borrado para la web (en lugar del UID) y el borrado, primero en el
    /// servidor y después en local, sin borrado remoto con la banda &lt; 13.
    /// </summary>
    public sealed class BackendF35Tests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetCloudOverrides(); // también los del backend y la banda de edad
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.A11yHostOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        private static Profile ProfileAt(int nextLevel, int seeds = 0)
        {
            Profile profile = Wallet.NewProfile(FunnelSetup.Clock);
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Wallet.Seeds = seeds;
            return profile;
        }

        private static IEnumerator LoadLobby(MemoryProfileStore store)
        {
            FunnelSetup.Hooks(); // reloj manual y la hoja de compra que confirma al momento
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false; // el test avanza el Mercado a mano
        }

        private static int RowOf(MarketScreen market, string sku)
        {
            for (int i = 0; i < market.Rows.Count; i++)
            {
                if (market.Rows[i].Id == sku)
                {
                    return i;
                }
            }

            Assert.Fail(sku + " no está en la pestaña");
            return -1;
        }

        [UnityTest]
        public IEnumerator Market_APendingPurchase_SaysSo_AndIsCreditedOnceWhenVerified()
        {
            var store = new MemoryProfileStore(ProfileAt(26));
            yield return LoadLobby(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Store!.IsAvailable, Is.True, "editor: el backend falso");
            Assert.That(boot.FakeBackend, Is.Not.Null);
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            MarketScreen market = boot.Market!;
            int row = RowOf(market, "af_coins_2");
            Assert.That(market.StatusText, Is.Null, "con backend, sin la línea de «no disponible»");

            boot.FakeBackend!.FailNext(new BackendError(BackendErrorKind.Network)); // sin red al verificar
            Assert.That(market.PressRow(row), Is.True);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0), "sin verificar no se acredita nada (08 §10.3)");
            Assert.That(market.RowNoteText(row), Is.EqualTo(MarketText.PurchasePending));
            Assert.That(boot.VerifiedStore!.PendingVerifications, Has.Count.EqualTo(1));
            Assert.That(ProfileJson.Deserialize(store.Json!).PurchaseQueue, Is.Not.Null, "la cola, guardada en el perfil");

            FunnelSetup.Clock.Advance(TimeSpan.FromHours(1));
            boot.VerifiedStore.Pump(); // lo que hace TickBackend cada 5 s
            boot.TickMarket(16f);
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(1000), "acreditada al verificarse");
            Assert.That(market.RowNoteText(row), Is.Empty, "sin nota");

            boot.ResumeBackend(true); // vuelta a primer plano: ni Resume ni restoreLedger la acreditan otra vez
            boot.VerifiedStore.Pump();
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(1000), "una sola vez");
            Assert.That(ProfileJson.Deserialize(store.Json!).Wallet.Seeds, Is.EqualTo(1000));
        }

        [UnityTest]
        public IEnumerator Market_ARejectedPurchase_SaysSo_AndNothingIsCredited()
        {
            yield return LoadLobby(new MemoryProfileStore(ProfileAt(26)));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            MarketScreen market = boot.Market!;
            int row = RowOf(market, "af_coins_2");
            boot.FakeBackend!.FailNext(new BackendError(BackendErrorKind.Http, 400, ApiErrorCode.BadRequest, "voided"));
            Assert.That(market.PressRow(row), Is.True);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));
            Assert.That(market.RowNoteText(row), Is.EqualTo(MarketText.PurchaseFailed));
            Assert.That(boot.FakeBilling!.Consumed, Is.Empty, "ni se consume (Play la reembolsa)");
            market.Close();
            Assert.That(boot.Services!.PurchaseNotes.Of("af_coins_2"), Is.EqualTo(PurchaseNoteKind.None), "el aviso es de una vez");
        }

        [UnityTest]
        public IEnumerator Market_WhileTheServerVerifies_SaysConfirming_AndNoPriceAnswers()
        {
            // 08 §10.3: «estado "verifying"; UI muestra "Confirmando…" (≤3 s)». El backend falso guarda la respuesta hasta soltarla,
            // como el servidor real mientras tarda (prueba del dueño del 2026-10-23: las esperas de red, visibles).
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            yield return LoadLobby(new MemoryProfileStore(ProfileAt(26)));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            MarketScreen market = boot.Market!;
            int row = RowOf(market, "af_coins_2");
            int other = RowOf(market, "af_coins_3");
            boot.FakeBackend!.HoldVerifyResponses = true;

            Assert.That(market.PressRow(row), Is.True);
            boot.TickMarket(16f); // la nota cambió: el Mercado se rehace
            Assert.That(boot.Store!.IsPurchasing, Is.True, "Play cobró y se pregunta al servidor");
            Assert.That(market.RowNoteText(row), Is.EqualTo(MarketText.PurchaseVerifying));
            Assert.That(boot.Services!.PurchaseNotes.Of("af_coins_2"), Is.EqualTo(PurchaseNoteKind.Verifying));
            Assert.That(market.RowButton(other).GetComponent<ReliefButton>().Interactable, Is.False, "ningún precio responde mientras");
            Assert.That(market.RowButton(row).GetComponent<ReliefButton>().Interactable, Is.False);
            Assert.That(market.PressRow(other), Is.False, "no se abre otra compra");
            Assert.That(host.Announcements, Does.Contain(MarketText.PurchaseVerifying), "TalkBack lo anuncia");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));

            boot.FakeBackend.ReleaseVerifyResponses();
            boot.TickMarket(16f);
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(1000), "acreditada al verificarse");
            Assert.That(market.RowNoteText(row), Is.Empty, "sin nota");
            Assert.That(market.RowButton(other).GetComponent<ReliefButton>().Interactable, Is.True, "los precios vuelven");
        }

        [UnityTest]
        public IEnumerator WithoutABackend_TheMarketDoesNotSell_AndIsHidden()
        {
            // Release sin URL (BackendEndpoints vacías): UnavailableBackendClient, el camino del ensayo de release (BuildRelease
            // -afRehearsal). La tienda no vende (no podría acreditar nada) y, sin precios de Play, el inicio no enseña el Mercado ni el
            // Cántaro (D-046, añadido del 2026-10-24; prueba del 2026-10-25, jornada 2, observación 4: el ensayo enseñaba el icono).
            GameBootstrap.BackendModeOverride = BackendMode.Unavailable;
            yield return LoadLobby(new MemoryProfileStore(ProfileAt(26)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Backend, Is.InstanceOf<UnavailableBackendClient>());
            Assert.That(boot.PlayBilling, Is.Null, "sin backend, Play ni se conecta");
            Assert.That(boot.Store!.IsAvailable, Is.False);
            Assert.That(boot.StoreDownForBackend, Is.False, "sin precios de la tienda");
            Assert.That(boot.MarketAvailable, Is.False);
            boot.TickMarket(16f);
            Assert.That(boot.MarketIcons!.MarketShown, Is.False, "el inicio sin el icono del Mercado");
            Assert.That(boot.MarketIcons.PiggyShown, Is.False, "ni el Cántaro");
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.False);
            Assert.That(boot.ServerAccountDelete, Is.Null, "sin backend, el borrado es solo local");
            Assert.That(boot.FakeBilling!.Requests, Is.Empty, "Play no abrió ninguna compra");
        }

        [UnityTest]
        public IEnumerator Boot_RestoreLedger_CreditsAPurchaseFromTheServerOnce()
        {
            // Q-B8-1: una compra que el perfil no tiene (la de un teléfono que perdió la fusión) vuelve con restoreLedger al arrancar.
            var backend = new FakeBackendClient(FunnelSetup.Clock);
            backend.VerifyPurchase(new VerifyPurchaseRequest("other.phone.0001", "af_coins_2"), r => Assert.That(r.IsOk, Is.True));
            GameBootstrap.BackendClientOverride = backend;
            var store = new MemoryProfileStore(ProfileAt(26, 50));
            yield return LoadLobby(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.LastLedgerRestore!.Ran, Is.True);
            Assert.That(boot.LastLedgerRestore.Recredited, Is.EqualTo(1));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(1050));
            Profile saved = ProfileJson.Deserialize(store.Json!);
            Assert.That(saved.AppliedPurchases, Is.EqualTo(new[] { PurchaseTokenHash.Of("other.phone.0001") }));
            Assert.That(saved.Wallet.Seeds, Is.EqualTo(1050), "en la misma escritura");

            Assert.That(boot.RestoreLedgerNow(), Is.True);
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(1050), "una sola vez");
        }

        [UnityTest]
        public IEnumerator Stuck_APendingBalsa_SaysSo_AndIsCreditedLaterWithoutItsCuia()
        {
            yield return Fx5Setup.LoadStuck40("es-419", 1f, Fx5Setup.Screen(780));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
            StuckModal stuck = boot.View!.Stuck;
            Assert.That(stuck.BalsaButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(stuck.PurchaseNoteText, Is.Null);

            boot.FakeBackend!.FailNext(new BackendError(BackendErrorKind.Network));
            Assert.That(boot.PressBalsa(), Is.True);
            yield return null; // el modal se rehace con la nota (GameScreen.Tick)
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal), "sin verificar no se aplica la Cuia de rescate");
            Assert.That(stuck.PurchaseNoteText, Is.EqualTo(MarketText.PurchasePending));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));

            FunnelSetup.Clock.Advance(TimeSpan.FromHours(1));
            boot.VerifiedStore!.Pump();
            yield return null;
            int balsaSeeds = boot.Services!.StoreLedger.Catalog.Get(StoreCatalog.Rescue).Content.Seeds;
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(balsaSeeds), "acreditada tarde, una vez: el resto del paquete");
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal), "la Cuia de rescate de una Balsa tardía se pierde (F3-3)");
            Assert.That(stuck.PurchaseNoteText, Is.Null);
        }

        [UnityTest]
        public IEnumerator APendingPiggy_CreditedLater_EmptiesTheJar_AndALateSkinIsPutOn()
        {
            // Q-B5-12: el Cántaro pendiente no se vacía hasta acreditarse, y entonces sí; Q-B5-13: la skin acreditada tarde se pone.
            var offers = new InMemoryOffersStore();
            offers.Save(new OffersRecord { PiggySeeds = 1500 });
            GameBootstrap.OffersStoreOverride = offers;
            yield return LoadLobby(new MemoryProfileStore(ProfileAt(42)));
            GameBootstrap boot = GameBootstrap.Current!;
            PiggyJar piggy = boot.Piggy!;
            Assert.That(piggy.Seeds, Is.EqualTo(1500));

            boot.FakeBackend!.FailNext(new BackendError(BackendErrorKind.Network), 2);
            PurchaseResult? jar = null;
            boot.Store!.Purchase(piggy.Request(PurchaseContext.Piggy, 42), r => jar = r);
            Assert.That(jar!.Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(piggy.OnPurchaseResult(jar), Is.False);
            Assert.That(piggy.Seeds, Is.EqualTo(1500), "pendiente: el Cántaro guarda lo suyo");
            PurchaseResult? skin = null;
            boot.Store.Purchase(new PurchaseRequest("af_skin_m03", PurchaseContext.Shop, 42), r => skin = r);
            Assert.That(skin!.Status, Is.EqualTo(PurchaseStatus.Pending));

            FunnelSetup.Clock.Advance(TimeSpan.FromHours(1));
            boot.VerifiedStore!.Pump();
            boot.VerifiedStore.Pump(); // una por llamada como mucho a la vez
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(1500 + boot.Services!.StoreLedger.Catalog.Get("af_skin_m03").Content.Seeds), "las dos, una vez");
            Assert.That(piggy.Seeds, Is.EqualTo(0), "acreditado tarde: se vacía");
            Assert.That(boot.Profile!.Entitlements.Skins, Does.Contain("af_skin_m03"));
            Assert.That(boot.Skins!.Equipped, Is.EqualTo("af_skin_m03"), "acreditada tarde, se pone");
        }

        [UnityTest]
        public IEnumerator Stuck_WithoutABackend_OffersNeitherHandfulNorBalsa()
        {
            GameBootstrap.BackendModeOverride = BackendMode.Unavailable;
            yield return Fx5Setup.LoadStuck40("es-419", 1f, Fx5Setup.Screen(780));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
            StuckModal stuck = boot.View!.Stuck;
            Assert.That(stuck.HandfulButton.gameObject.activeInHierarchy, Is.False, "la tienda no vende: no se ofrece");
            Assert.That(stuck.BalsaButton.gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator BizBackendEnabledOff_NoSales_NoServerDelete_AndNoCode()
        {
            // B7: biz_backend_enabled de Remote Config, por RcBackendSwitch: con el backend del build pero apagado.
            GameBootstrap.RemoteConfigBackendOverride = new AF.RemoteConfig.FakeRemoteConfigBackend().Set("biz_backend_enabled", "false").StoreFetched();
            GameBootstrap.WipePlayerPrefsForTests = () => { };
            var store = new MemoryProfileStore(ProfileAt(26, 900));
            yield return LoadLobby(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.FakeBackend, Is.Not.Null);
            Assert.That(boot.BackendReady, Is.False);
            Assert.That(boot.Store!.IsAvailable, Is.False);
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            Assert.That(boot.Market!.StatusText, Is.EqualTo(MarketText.StoreUnavailable));
            boot.Market.Close();

            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.SettingsView!.AccountCodeText, Is.EqualTo(AccountText.CodeUnavailable));
            Assert.That(boot.FakeBackend!.Calls.ContainsKey(BackendOperation.GetAccountCode.Name), Is.False);
            Assert.That(boot.OpenDeleteAccount() && boot.ContinueDeleteAccount() && boot.ConfirmDeleteAccount(), Is.True);
            Assert.That(boot.DeleteAccountView!.WarningText, Is.EqualTo(AccountText.Failed), "sin el servidor no se borra nada [P]");
            Assert.That(boot.FakeBackend.Deletions, Is.EqualTo(0));
            Assert.That(store.Json, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Settings_AFailedCodeRequest_SaysSo_AndAsksAgainNextTime()
        {
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            yield return LoadLobby(store);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.FakeBackend!.FailNext(new BackendError(BackendErrorKind.Network));
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.SettingsView!.AccountCodeText, Is.EqualTo(AccountText.CodeUnavailable));
            Assert.That(boot.Profile!.AccountCode, Is.Null, "no se guarda nada");

            boot.CloseSettings();
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.Profile.AccountCode, Is.EqualTo(boot.FakeBackend.CurrentAccountCode));
            Assert.That(boot.SettingsView.AccountCodeText, Is.EqualTo(AccountText.Code(boot.Profile.AccountCode!)));
        }

        [UnityTest]
        public IEnumerator Settings_ShowsTheWebDeleteCode_NotTheUid_AndAsksForItOnce()
        {
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            yield return LoadLobby(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            string code = boot.FakeBackend!.CurrentAccountCode!;
            Assert.That(ContractRules.IsAccountCode(code), Is.True);
            Assert.That(boot.SettingsView!.AccountCodeText, Is.EqualTo(AccountText.Code(code)));
            Assert.That(ProfileJson.Deserialize(store.Json!).AccountCode, Is.EqualTo(code), "account_code [P], guardado");

            boot.CloseSettings();
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.SettingsView.AccountCodeText, Is.EqualTo(AccountText.Code(code)));
            Assert.That(boot.FakeBackend.Calls[BackendOperation.GetAccountCode.Name], Is.EqualTo(1), "rota: solo se pide si se perdió");
        }

        [UnityTest]
        public IEnumerator DeleteAccount_GoesToTheServerFirst_AndAFailureKeepsEverything()
        {
            int wipes = 0;
            GameBootstrap.WipePlayerPrefsForTests = () => wipes++;
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            yield return LoadLobby(store);
            GameBootstrap boot = GameBootstrap.Current!;
            FakeBackendClient backend = boot.FakeBackend!;
            Assert.That(boot.PressSettings(), Is.True);

            backend.FailNext(new BackendError(BackendErrorKind.Network));
            Assert.That(boot.OpenDeleteAccount() && boot.ContinueDeleteAccount(), Is.True);
            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            Assert.That(boot.DeleteAccountView!.WarningText, Is.EqualTo(AccountText.Failed), "sin el servidor no se borra nada");
            Assert.That(backend.Deletions, Is.EqualTo(0));
            Assert.That(store.Json, Is.Not.Null);
            Assert.That(wipes, Is.EqualTo(0));

            Assert.That(boot.ConfirmDeleteAccount(), Is.True, "reintento");
            Assert.That(backend.Deletions, Is.EqualTo(1), "POST /v1/account/delete");
            Assert.That(store.Json, Is.Null, "y después lo local (08 §11)");
            Assert.That(wipes, Is.EqualTo(1));
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != boot && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "arranque en frío tras eliminar la cuenta");
        }

        [UnityTest]
        public IEnumerator Under13_NoWebCode_AndOnlyTheLocalDelete()
        {
            // Plan del backend §7, B12: con la banda < 13 no hay IAP ni borrado remoto (la banda llega con B12; aquí, su gancho).
            GameBootstrap.AgeUnder13Override = true;
            GameBootstrap.WipePlayerPrefsForTests = () => { };
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            yield return LoadLobby(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.RemoteAccountAllowed, Is.False);
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.SettingsView!.AccountCodeText, Is.Null, "sin código para la web");
            Assert.That(boot.FakeBackend!.Calls.ContainsKey(BackendOperation.GetAccountCode.Name), Is.False);

            Assert.That(boot.OpenDeleteAccount() && boot.ContinueDeleteAccount() && boot.ConfirmDeleteAccount(), Is.True);
            Assert.That(boot.FakeBackend.Deletions, Is.EqualTo(0), "sin borrado remoto");
            Assert.That(store.Json, Is.Null, "lo local, sí");
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != boot && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "arranque en frío tras eliminar la cuenta");
        }
    }
}
