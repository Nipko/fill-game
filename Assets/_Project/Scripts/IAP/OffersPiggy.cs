#nullable enable
using System;
using AF.Economy;

namespace AF.IAP
{
    /// <summary>Lo que hizo una victoria en el Cántaro (<see cref="PiggyJar.OnCampaignVictory"/>).</summary>
    public readonly struct PiggyFill
    {
        /// <summary>Crea el apunte.</summary>
        public PiggyFill(int added, int seeds, int cap, bool becameFull)
        {
            Added = added;
            Seeds = seeds;
            Cap = cap;
            BecameFull = becameFull;
        }

        /// <summary>Semillas que entraron (0 si ya estaba lleno).</summary>
        public int Added { get; }

        /// <summary>Semillas del Cántaro después.</summary>
        public int Seeds { get; }

        /// <summary>Tope.</summary>
        public int Cap { get; }

        /// <summary>Esta victoria lo llenó («¡Lleno!» a <c>eco_piggy_cap</c>; 05 §4.1).</summary>
        public bool BecameFull { get; }
    }

    /// <summary>
    /// El Cántaro (<c>af_piggy</c>; 05 §4.1 y §5; glosario §8: «Alcancía → Cántaro», determinista y con tope visible):
    /// <list type="bullet">
    /// <item><description>se ve desde <c>eco_piggy_from_level</c> (20) y cada victoria de campaña desde ese nivel le suma
    /// <c>eco_piggy_fill</c> (+15 / +30 / +40 por victoria normal, Difícil y Muy difícil), hasta <c>eco_piggy_cap</c> (3.000,
    /// «¡Lleno!»). El Reto del río no lo llena [P] (Q-B5-5);</description></item>
    /// <item><description>se puede comprar desde <c>eco_piggy_min_buy</c> (1.000): da exactamente lo acumulado
    /// (<see cref="StoreLedger"/> con <see cref="PurchaseRequest.PiggySeeds"/>); al acreditarse se le resta lo acreditado (se vacía si
    /// no entró nada desde la compra) y vuelve a llenarse («1 compra por llenado»).</description></item>
    /// </list>
    /// Las semillas del Cántaro no son de la cartera hasta comprarlo: no pasan por <c>Wallet.Apply</c> al llenarse (06 §4.4 no lo cuenta
    /// como fuente; 05 §2.1 tampoco). Sin asignaciones al consultarlo.
    /// </summary>
    public sealed class PiggyJar
    {
        private readonly OffersState _state;

        /// <summary>Crea el Cántaro sobre <paramref name="state"/>.</summary>
        /// <exception cref="InvalidOperationException">Alguna configuración no es válida.</exception>
        public PiggyJar(OffersState state, OffersConfig? config = null, StoreConfig? store = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            Config = config ?? OffersConfig.Defaults;
            StoreConfig = store ?? StoreConfig.Defaults;
            Config.Validate();
            StoreConfig.Validate();
        }

        /// <summary>Cifras de las ofertas (<c>eco_piggy_from_level</c> y <c>eco_piggy_fill</c>).</summary>
        public OffersConfig Config { get; }

        /// <summary>Cifras de la tienda (<c>eco_piggy_cap</c> y <c>eco_piggy_min_buy</c>).</summary>
        public StoreConfig StoreConfig { get; }

        /// <summary>
        /// Semillas acumuladas, como mucho <see cref="Cap"/>: si Remote Config baja <c>eco_piggy_cap</c> por debajo de lo guardado, el
        /// Cántaro queda lleno con el tope nuevo y se puede comprar (la tienda no vende más que el tope) [P].
        /// </summary>
        public int Seeds => Math.Min(_state.Record.PiggySeeds, Cap);

        /// <summary>Tope en vigor: <c>eco_piggy_cap</c> (se anota en <c>piggy.cap</c> en cada guardado).</summary>
        public int Cap => StoreConfig.EcoPiggyCap;

        /// <summary>Compra mínima (<c>eco_piggy_min_buy</c>).</summary>
        public int MinBuy => StoreConfig.EcoPiggyMinBuy;

        /// <summary>Está lleno («¡Lleno!»).</summary>
        public bool IsFull => Seeds >= Cap;

        /// <summary>Se puede comprar ya (≥ <c>eco_piggy_min_buy</c>).</summary>
        public bool CanBuy => Seeds >= MinBuy;

        /// <summary>
        /// El icono se ve con el nivel en juego <paramref name="currentLevel"/> (el siguiente por jugar, <c>max_level</c> + 1):
        /// desde <c>eco_piggy_from_level</c>, o antes si ya guarda semillas.
        /// </summary>
        public bool VisibleAt(int currentLevel)
        {
            return currentLevel >= Config.EcoPiggyFromLevel || Seeds > 0;
        }

        /// <summary>Semillas que suma una victoria de <paramref name="tier"/> (<c>eco_piggy_fill</c>).</summary>
        public int FillFor(RewardTier tier)
        {
            switch (tier)
            {
                case RewardTier.Hard: return Config.EcoPiggyFill[1];
                case RewardTier.VeryHard: return Config.EcoPiggyFill[2];
                default: return Config.EcoPiggyFill[0];
            }
        }

        /// <summary>
        /// Victoria de campaña en <paramref name="level"/> con la etiqueta <paramref name="tier"/>: desde <c>eco_piggy_from_level</c>,
        /// suma <see cref="FillFor"/> hasta el tope y guarda. La misma victoria no suma dos veces (un nivel ≤ el último que llenó no
        /// cuenta: tras reabrir la app en la pantalla de victoria). Devuelve lo que hizo, o null si no cuenta.
        /// </summary>
        public PiggyFill? OnCampaignVictory(int level, RewardTier tier)
        {
            OffersRecord r = _state.Record;
            if (level < Config.EcoPiggyFromLevel || level <= r.PiggyLastLevel)
            {
                return null;
            }

            int cap = Cap;
            int before = Seeds;
            bool wasFull = before >= cap;
            int after = Math.Min(cap, before + FillFor(tier));
            r.PiggyLastLevel = level;
            if (wasFull && r.PiggySeeds == after && r.PiggyCap == cap)
            {
                // Ya lleno: no suma nada y no hace falta otra escritura en la victoria (el nivel sale con la siguiente).
                return new PiggyFill(0, after, cap, false);
            }

            r.PiggySeeds = after;
            r.PiggyCap = cap;
            _state.Persist();
            return new PiggyFill(after - before, after, cap, !wasFull && after >= cap);
        }

        /// <summary>La petición de compra con lo acumulado (contexto <c>piggy</c> o <c>shop</c>).</summary>
        public PurchaseRequest Request(PurchaseContext context, int level)
        {
            return new PurchaseRequest(StoreCatalog.Piggy, context, Math.Max(0, level), Seeds); // nunca más que el tope en vigor
        }

        /// <summary>
        /// Terminó una compra del Cántaro: solo con <see cref="PurchaseStatus.Purchased"/> (acreditada: <c>Wallet.Apply</c> ya sumó
        /// <see cref="PurchaseRequest.PiggySeeds"/>) se le restan esas semillas, así que se vacía y vuelve a llenarse desde 0 (y, si algo
        /// entró mientras la compra seguía abierta, lo conserva, como <see cref="OnCreditedLater"/>). Con
        /// <see cref="PurchaseStatus.Pending"/> (sin verificar, o el pago en efectivo de Play aún sin pagar) no cambia: el Cántaro guarda
        /// lo suyo hasta que la compra se acredite [P] (Q-B5-12). Con cualquier otro resultado no cambia nada. Devuelve si se acreditó.
        /// </summary>
        public bool OnPurchaseResult(PurchaseResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!string.Equals(result.Product.Sku, StoreCatalog.Piggy, StringComparison.Ordinal)
                || result.Status != PurchaseStatus.Purchased)
            {
                return false;
            }

            Withdraw(result.Request.PiggySeeds);
            return true;
        }

        /// <summary>
        /// Una compra del Cántaro que quedó pendiente (Q-B5-12) se acreditó después, desde la cola de verificación (F3-5: al volver a
        /// primer plano, en el siguiente arranque o con el <c>Pump</c> periódico): se le restan solo las <paramref name="creditedSeeds"/>
        /// que entraron en la cartera (<see cref="PurchaseVerificationEvent.CreditedPiggySeeds"/>) y se conserva lo que entró en el
        /// Cántaro después de la compra (D-035, añadido de F3-5: sustituye vaciarlo entero). Con otro <paramref name="sku"/> no hace
        /// nada. Devuelve si cambió el Cántaro.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="creditedSeeds"/> es negativo.</exception>
        public bool OnCreditedLater(string sku, int creditedSeeds)
        {
            if (creditedSeeds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(creditedSeeds));
            }

            if (!string.Equals(sku, StoreCatalog.Piggy, StringComparison.Ordinal))
            {
                return false;
            }

            Withdraw(creditedSeeds);
            return true;
        }

        // Resta lo acreditado de lo que hay (con el tope en vigor; nunca por debajo de 0). La tarjeta «¡Lleno!» vuelve a salir cuando
        // se llene otra vez.
        private void Withdraw(int creditedSeeds)
        {
            OffersRecord r = _state.Record;
            int cap = StoreConfig.EcoPiggyCap;
            int left = Math.Max(0, Seeds - Math.Max(0, creditedSeeds));
            r.PiggySeeds = left;
            r.PiggyCap = cap;
            if (left < cap)
            {
                r.PiggyFullCardShown = false;
            }

            _state.Persist();
        }
    }
}
