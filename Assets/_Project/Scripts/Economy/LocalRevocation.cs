#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;
using AF.Save;

namespace AF.Economy
{
    /// <summary>Cifras de la revocación local con el <b>default compilado</b> de la tabla única de 08 §9.</summary>
    public sealed class RevocationConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9.</summary>
        public static RevocationConfig Defaults => new RevocationConfig();

        /// <summary>
        /// <c>sec_ssv_grace_h</c>: horas que una recompensa por anuncio puede seguir provisional sin la confirmación del servidor;
        /// pasadas, se revoca (08 §10.1 y §10.2).
        /// </summary>
        [RemoteConfigKey("sec_ssv_grace_h")]
        public int SecSsvGraceH { get; set; } = 24;

        /// <summary>El plazo como intervalo.</summary>
        public TimeSpan SsvGrace => TimeSpan.FromHours(SecSsvGraceH);

        /// <summary>Comprueba que los valores tienen sentido (08 §9: rango 12-48).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            if (SecSsvGraceH < 1)
            {
                throw new InvalidOperationException("RevocationConfig: sec_ssv_grace_h debe ser >= 1.");
            }
        }
    }

    /// <summary>
    /// Una entrada del libro mayor de compras que devuelve <c>restoreLedger</c> (08 §2.4, §10.3 y §11; 04 §10.2): el token, el SKU,
    /// lo que concede (como lo da el catálogo de la tienda) y si Play lo anuló (<c>voided</c>, por <c>voidedPurchasesJob</c>).
    /// </summary>
    public sealed class LedgerEntry
    {
        /// <summary>Crea la entrada.</summary>
        public LedgerEntry(string token, string sku, int seeds = 0, StockDelta stock = default, TimeSpan infiniteCanoes = default,
            bool grantsNoAds = false, bool voided = false)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Falta el token.", nameof(token));
            }

            if (string.IsNullOrEmpty(sku))
            {
                throw new ArgumentException("Falta el SKU.", nameof(sku));
            }

            if (seeds < 0 || stock.HasNegative || infiniteCanoes < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(seeds), "Una compra solo suma.");
            }

            Token = token;
            Sku = sku;
            Seeds = seeds;
            Stock = stock;
            InfiniteCanoes = infiniteCanoes;
            GrantsNoAds = grantsNoAds;
            Voided = voided;
        }

        /// <summary><c>purchaseToken</c>.</summary>
        public string Token { get; }

        /// <summary>SKU (05 §4.1).</summary>
        public string Sku { get; }

        /// <summary>Semillas que concede.</summary>
        public int Seeds { get; }

        /// <summary>Comodines que concede.</summary>
        public StockDelta Stock { get; }

        /// <summary>Canoas infinitas que concede (no se retiran: ya corrieron) [P].</summary>
        public TimeSpan InfiniteCanoes { get; }

        /// <summary>Concede Sin anuncios.</summary>
        public bool GrantsNoAds { get; }

        /// <summary>Play la anuló (reembolso o contracargo).</summary>
        public bool Voided { get; }
    }

    /// <summary>Lo que hizo <see cref="LocalRevocation.ApplyLedger"/>.</summary>
    public readonly struct LedgerReport
    {
        /// <summary>Crea el informe.</summary>
        public LedgerReport(int recredited, int revoked, int seedsRevoked)
        {
            Recredited = recredited;
            Revoked = revoked;
            SeedsRevoked = seedsRevoked;
        }

        /// <summary>Tokens del libro mayor que faltaban en <c>applied_purchases</c> y se acreditaron.</summary>
        public int Recredited { get; }

        /// <summary>Tokens anulados que se retiraron.</summary>
        public int Revoked { get; }

        /// <summary>Semillas que salieron de verdad al retirar (sin bajar de 0).</summary>
        public int SeedsRevoked { get; }
    }

    /// <summary>
    /// Revocación local (GDD 08 §10.1 y §10.2; 04 §10.2; bloque B8; nota <c>docs/dev/nube-integridad.md</c> §4), sobre el perfil y
    /// la cartera, que ya cuida «sin bajar de 0» (<see cref="Transaction.Revoke"/>):
    /// <list type="bullet">
    /// <item><b>Recompensas por anuncio provisionales</b> (08 §10.2): <see cref="RegisterProvisional"/> apunta lo concedido con su
    /// <c>nonce</c> (en <c>pending_rewards</c> [P], en el mismo lote que la concesión); <see cref="Confirm"/> lo da por bueno
    /// cuando el servidor lo confirma; <see cref="ExpireStale"/> revoca lo que siga sin confirmar tras <c>sec_ssv_grace_h</c> y
    /// suma <c>integrity.fraud_flag</c> (sin castigo automático, 08 §10.1).</item>
    /// <item><b>Libro mayor de compras</b> (08 §10.3 y §11): <see cref="ApplyLedger"/> vuelve a acreditar los tokens que falten en
    /// <c>applied_purchases</c> y retira los anulados (semillas sin bajar de 0, stock y Sin anuncios), apuntándolos en
    /// <c>voided_purchases</c> [P] para no retirarlos dos veces.</item>
    /// </list>
    /// En la 1.0 sin servidor nada llama a <see cref="RegisterProvisional"/> (si no, todo se revocaría a las 24 h): la concesión
    /// sale confirmada, como hoy. La hora sale del reloj que se le da (en el juego, el <c>GuardedClock</c>).
    /// </summary>
    public sealed class LocalRevocation
    {
        private readonly Profile _profile;
        private readonly Wallet _wallet;
        private readonly IProfileStore _store;
        private readonly IClock _clock;
        private readonly RevocationConfig _config;

        /// <summary>Crea la revocación sobre <paramref name="profile"/>, el mismo perfil que usa <paramref name="wallet"/>.</summary>
        public LocalRevocation(Profile profile, Wallet wallet, IProfileStore store, IClock clock, RevocationConfig? config = null)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? RevocationConfig.Defaults;
            _config.Validate();
        }

        /// <summary>Recompensas aún provisionales.</summary>
        public int PendingCount => _profile.PendingRewards.Count;

        /// <summary>
        /// Apunta una recompensa ya concedida como provisional (08 §10.2: <c>Economy.GrantProvisional(reward, nonce)</c>). La
        /// concesión la hace quien llama con la cartera, en el mismo lote. False si el <c>nonce</c> ya estaba.
        /// </summary>
        public bool RegisterProvisional(string nonce, string placement, int seeds, StockDelta stock)
        {
            if (string.IsNullOrEmpty(nonce))
            {
                throw new ArgumentException("Falta el nonce.", nameof(nonce));
            }

            if (seeds < 0 || stock.HasNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(seeds), "Se apunta lo concedido, en positivo.");
            }

            if (IndexOf(nonce) >= 0)
            {
                return false;
            }

            var entry = new ProvisionalRewardData
            {
                Nonce = nonce,
                Placement = placement ?? string.Empty,
                GrantedAt = _clock.UtcNow,
                Seeds = seeds,
            };
            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                var kind = (BoosterKind)k;
                BoosterKinds.Write(entry.Stock, kind, stock.Get(kind));
            }

            _profile.PendingRewards.Add(entry);
            _store.TrySave(_profile);
            return true;
        }

        /// <summary>El servidor confirmó <paramref name="nonce"/> (<c>granted=confirmed</c>): deja de ser provisional. False si no estaba.</summary>
        public bool Confirm(string nonce)
        {
            int i = IndexOf(nonce ?? throw new ArgumentNullException(nameof(nonce)));
            if (i < 0)
            {
                return false;
            }

            _profile.PendingRewards.RemoveAt(i);
            _store.TrySave(_profile);
            return true;
        }

        /// <summary>
        /// Revoca cada recompensa provisional con más de <c>sec_ssv_grace_h</c> horas (08 §10.2: «nonce sin confirmar tras 24 h →
        /// Economy.Revoke(nonce) (no baja de 0); fraud_flag++»). Devuelve cuántas revocó.
        /// </summary>
        public int ExpireStale()
        {
            DateTime now = _clock.UtcNow;
            int revoked = 0;
            for (int i = _profile.PendingRewards.Count - 1; i >= 0; i--)
            {
                ProvisionalRewardData entry = _profile.PendingRewards[i];
                if (now - entry.GrantedAt < _config.SsvGrace)
                {
                    continue;
                }

                StockDelta stock = StockDelta.None;
                for (int k = 0; k < BoosterKinds.Count; k++)
                {
                    var kind = (BoosterKind)k;
                    int units = BoosterKinds.Read(entry.Stock, kind);
                    if (units > 0)
                    {
                        stock = stock.Plus(kind, units);
                    }
                }

                _profile.PendingRewards.RemoveAt(i);
                _profile.Integrity.FraudFlag++;
                TransactionResult result = _wallet.Apply(Transaction.Revoke(entry.Seeds, stock));
                if (!result.Saved)
                {
                    _store.TrySave(_profile); // la cartera no tenía nada que quitar: el apunte se guarda igual
                }

                revoked++;
            }

            return revoked;
        }

        /// <summary>
        /// Aplica el libro mayor de <c>restoreLedger</c> (08 §11: «vuelve a acreditar cualquier token del libro mayor ausente de
        /// <c>applied_purchases</c> (y retira los marcados <c>voided</c>)»). Las entradas se aplican en orden ordinal de token
        /// (determinista, sin importar el orden del servidor).
        /// </summary>
        public LedgerReport ApplyLedger(IReadOnlyList<LedgerEntry> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var ordered = new List<LedgerEntry>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                ordered.Add(entries[i] ?? throw new ArgumentException("El libro mayor no admite entradas null.", nameof(entries)));
            }

            ordered.Sort((a, b) => string.CompareOrdinal(a.Token, b.Token));
            int recredited = 0;
            int revoked = 0;
            int seedsRevoked = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                LedgerEntry entry = ordered[i];
                bool applied = _profile.AppliedPurchases.Contains(entry.Token);
                if (entry.Voided)
                {
                    if (!applied || _profile.VoidedPurchases.Contains(entry.Token))
                    {
                        continue; // nunca se acreditó aquí, o ya se retiró
                    }

                    TransactionResult result = _wallet.Apply(Transaction.Revoke(entry.Seeds, entry.Stock, entry.GrantsNoAds, entry.Token));
                    seedsRevoked += -result.SeedsDelta;
                    revoked++;
                }
                else if (!applied)
                {
                    TransactionResult result = _wallet.Apply(Transaction.Purchase(
                        entry.Sku, entry.Seeds, entry.Stock, entry.InfiniteCanoes, grantsNoAds: entry.GrantsNoAds, purchaseToken: entry.Token));
                    if (result.IsApplied)
                    {
                        recredited++;
                    }
                }
            }

            return new LedgerReport(recredited, revoked, seedsRevoked);
        }

        private int IndexOf(string nonce)
        {
            List<ProvisionalRewardData> list = _profile.PendingRewards;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Nonce, nonce, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
