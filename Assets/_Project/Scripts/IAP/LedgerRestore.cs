#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend;
using AF.Backend.Contract;
using AF.Economy;
using AF.Save;

namespace AF.IAP
{
    /// <summary>Lo que hizo una llamada a <see cref="LedgerRestore.Restore"/>.</summary>
    public sealed class LedgerRestoreReport
    {
        internal LedgerRestoreReport(bool ran, BackendError? error)
        {
            Ran = ran;
            Error = error;
        }

        /// <summary>El servidor respondió y se aplicó su libro mayor (false: no se llamó, o falló).</summary>
        public bool Ran { get; }

        /// <summary>El error de la llamada, si falló (sin tokens ni UID: se puede registrar).</summary>
        public BackendError? Error { get; }

        /// <summary>Compras <c>ok</c> que faltaban en <c>applied_purchases</c> y se acreditaron ahora.</summary>
        public int Recredited { get; internal set; }

        /// <summary>De <see cref="Recredited"/>, las que dan Sin anuncios (<c>af_entitlement_restored</c> con <c>type = no_ads</c>).</summary>
        public int RecreditedNoAds { get; internal set; }

        /// <summary>Compras <c>ok</c> que faltaban pero cuyo producto ya se tenía: solo se anotó su <c>token_hash</c>.</summary>
        public int AlreadyOwned { get; internal set; }

        /// <summary>Compras que se dejaron a la cola de verificación de este dispositivo (la acreditará ella, con su contexto).</summary>
        public int LeftToQueue { get; internal set; }

        /// <summary>Compras de un SKU que esta versión no vende, o que la cartera rechazó: no se acreditaron.</summary>
        public int Skipped { get; internal set; }

        /// <summary>Compras anuladas (<c>voided</c>) que se retiraron ahora.</summary>
        public int Revoked { get; internal set; }

        /// <summary>Semillas que salieron de verdad al retirar (sin bajar de 0).</summary>
        public int SeedsRevoked { get; internal set; }

        /// <summary>Recompensas provisionales que el servidor confirmó (<c>confirmed_nonces</c>).</summary>
        public int Confirmed { get; internal set; }

        /// <summary>Recompensas provisionales revocadas por pasar de <c>sec_ssv_grace_h</c> sin confirmar (cada una suma <c>fraud_flag</c>).</summary>
        public int Expired { get; internal set; }
    }

    /// <summary>
    /// <c>restoreLedger</c> en el cliente (F3-5; 08 §10.2, §10.3 y §11, fila Conflicto; D-033, Q-B8-1; plan del backend §9, F3-4): pide
    /// al servidor el libro mayor del UID con los nonces de las recompensas provisionales y, con su respuesta, en una sola pasada:
    /// <list type="bullet">
    /// <item>acredita <b>una sola vez</b> cada compra <c>ok</c> cuyo <c>token_hash</c> no esté en <c>applied_purchases</c>, con
    /// <see cref="StoreLedger.CreditVerified"/> (el mismo camino que la compra verificada, sin <c>grant</c>: el contenido del catálogo
    /// de 05 §4.1, como con <c>duplicate</c>). Así sale igual que al comprar: derechos (Sin anuncios, skin, Mochila), las Canoas
    /// infinitas de la Balsa solo con <c>biz_feature_lives</c> encendido (D-038, añadido de la Balsa; <see cref="StoreLedger.CreditedCanoes"/>),
    /// sin la Cuia de rescate (se acredita fuera del nivel: D-035, añadido de F3-3) y el Cántaro con <c>eco_piggy_min_buy</c> (sus
    /// semillas solo las sabía el otro teléfono [P]). Las que la cola de este dispositivo aún verifica se dejan a la cola
    /// (<see cref="VerifiedStoreService.IsAwaitingServer"/>), que las acredita con su contexto;</item>
    /// <item>retira las <c>voided</c> ya acreditadas aquí (<see cref="LocalRevocation.ApplyLedger"/>: semillas sin bajar de 0, stock y
    /// Sin anuncios; una vez por token, <c>voided_purchases</c>). Las skins y la marca de la Mochila no se retiran [P];</item>
    /// <item>confirma las recompensas provisionales cuyos nonces devuelve el servidor (<see cref="LocalRevocation.Confirm"/>) y revoca las
    /// que pasaron de <c>sec_ssv_grace_h</c> sin confirmar (<see cref="LocalRevocation.ExpireStale"/>: sin bajar de 0 y
    /// <c>fraud_flag</c>). Solo tras una respuesta del servidor: sin red no se revoca nada [P].</item>
    /// </list>
    /// Las compras se aplican en orden ordinal de <c>token_hash</c> (determinista, sea cual sea el orden del servidor). Quien llama
    /// abre un lote de guardado alrededor de la respuesta si quiere una sola escritura. Con <c>biz_backend_enabled</c> apagado no se
    /// llama. Hilo principal.
    /// </summary>
    public sealed class LedgerRestore
    {
        private readonly StoreLedger _ledger;
        private readonly LocalRevocation _revocation;
        private readonly IBackendClient _backend;
        private readonly IBackendSwitch _switch;
        private readonly VerifiedStoreService? _store;

        /// <param name="ledger">Registro de la tienda (el mismo perfil y la misma cartera que <paramref name="revocation"/>).</param>
        /// <param name="revocation">Revocación local de B8.</param>
        /// <param name="backend">Cliente del backend.</param>
        /// <param name="backendSwitch"><c>biz_backend_enabled</c>; null: el default.</param>
        /// <param name="store">La tienda verificada de este dispositivo, para no pisar su cola; null: sin cola.</param>
        public LedgerRestore(StoreLedger ledger, LocalRevocation revocation, IBackendClient backend, IBackendSwitch? backendSwitch = null,
            VerifiedStoreService? store = null)
        {
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _revocation = revocation ?? throw new ArgumentNullException(nameof(revocation));
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _switch = backendSwitch ?? new BackendSwitch();
            _store = store;
        }

        /// <summary>Hay una llamada en vuelo.</summary>
        public bool InFlight { get; private set; }

        /// <summary>
        /// Llama a <c>restoreLedger</c> y aplica su respuesta. False, sin llamar a <paramref name="done"/>, si ya hay una en vuelo o el
        /// backend está apagado (<c>biz_backend_enabled</c>).
        /// </summary>
        public bool Restore(Action<LedgerRestoreReport>? done)
        {
            if (InFlight || !_switch.BizBackendEnabled)
            {
                return false;
            }

            RestoreLedgerRequest request = new RestoreLedgerRequest(PendingNonces(_ledger.Profile));
            InFlight = true;
            _backend.RestoreLedger(request, result =>
            {
                InFlight = false;
                LedgerRestoreReport report;
                if (!result.IsOk)
                {
                    report = new LedgerRestoreReport(false, result.Error);
                }
                else
                {
                    report = new LedgerRestoreReport(true, null);
                    Apply(result.Value!, report);
                }

                done?.Invoke(report);
            });
            return true;
        }

        /// <summary>Los nonces de <c>pending_rewards</c> que cumplen el contrato (UUID v4), sin repetidos y como mucho 200.</summary>
        public static List<string> PendingNonces(Profile profile)
        {
            var nonces = new List<string>();
            List<ProvisionalRewardData> pending = profile.PendingRewards;
            for (int i = 0; i < pending.Count && nonces.Count < ContractRules.MaxNonces; i++)
            {
                string nonce = pending[i].Nonce;
                if (ContractRules.IsNonce(nonce) && !nonces.Contains(nonce))
                {
                    nonces.Add(nonce);
                }
            }

            return nonces;
        }

        private void Apply(RestoreLedgerResponse response, LedgerRestoreReport report)
        {
            var purchases = new List<LedgerPurchase>(response.Purchases);
            purchases.Sort((a, b) => string.CompareOrdinal(a.TokenHash, b.TokenHash));
            var voided = new List<LedgerEntry>();
            Profile profile = _ledger.Profile;
            for (int i = 0; i < purchases.Count; i++)
            {
                LedgerPurchase purchase = purchases[i];
                StoreProduct? product = _ledger.Catalog.Find(purchase.Sku);
                if (product == null)
                {
                    report.Skipped++; // un SKU que esta versión no conoce: ni se acredita ni se retira
                    continue;
                }

                ProductContent content = product.Content;
                if (purchase.State == LedgerPurchaseState.Voided)
                {
                    int seeds = content.PiggySeeds ? _ledger.Config.EcoPiggyMinBuy : content.Seeds;
                    voided.Add(new LedgerEntry(purchase.TokenHash, purchase.Sku, seeds, content.Stock, TimeSpan.Zero, content.GrantsNoAds, voided: true));
                    continue;
                }

                if (profile.HasAppliedPurchase(purchase.TokenHash))
                {
                    continue; // ya acreditada aquí: nunca dos veces
                }

                if (_store != null && _store.IsAwaitingServer(purchase.TokenHash))
                {
                    report.LeftToQueue++;
                    continue;
                }

                VerifiedCredit credit = _ledger.CreditVerified(product, purchase.TokenHash, null, null, null, appliesRescueNow: false);
                switch (credit.Kind)
                {
                    case VerifiedCreditKind.Credited:
                        report.Recredited++;
                        if (content.GrantsNoAds)
                        {
                            report.RecreditedNoAds++;
                        }

                        break;
                    case VerifiedCreditKind.AlreadyOwned:
                        report.AlreadyOwned++;
                        break;
                    case VerifiedCreditKind.Rejected:
                        report.Skipped++;
                        break;
                }
            }

            if (voided.Count > 0)
            {
                LedgerReport revoked = _revocation.ApplyLedger(voided);
                report.Revoked = revoked.Revoked;
                report.SeedsRevoked = revoked.SeedsRevoked;
            }

            for (int i = 0; i < response.ConfirmedNonces.Count; i++)
            {
                if (_revocation.Confirm(response.ConfirmedNonces[i]))
                {
                    report.Confirmed++;
                }
            }

            report.Expired = _revocation.ExpireStale();
        }
    }
}
