#nullable enable
using System;
using AF.Economy;

namespace AF.Boosters
{
    /// <summary>Con qué se paga una Luciérnaga al elegir la cuia (04 §1.1: el stock se gasta antes que las semillas).</summary>
    public enum RevealPayment
    {
        /// <summary>Una unidad del stock (<c>stock.reveal</c>: regalada en el 100, comprada o del video <c>rv_reveal</c>).</summary>
        Stock = 0,

        /// <summary><c>reveal_seed_price</c> semillas (120; 08 §9).</summary>
        Seeds = 1,
    }

    /// <summary>
    /// Luciérnaga · Revelar (GDD 04 §2.5; 02 §11 T6; 08 §9 <c>reveal_seed_price</c> y <c>rv_reveal</c>): revela todas las dosis
    /// ocultas de una cuia turbia elegida por el jugador. Sin tope por nivel; 1 regalada en el estreno de Aguas turbias (nivel
    /// 100, D-007); 120 semillas o 1 video. El revelado lo hace el núcleo (<c>LevelSession.Reveal</c>) y el modo de selección
    /// <c>AF.GameFlow</c>; aquí, el regalo y el cobro. Sin motor y sin <c>AF.Levels</c> (08 §2.2): lo que necesita del nivel
    /// entra como primitivos, como en <see cref="ExtraCuiaBooster.ShouldGift"/>.
    /// </summary>
    public static class RevealBooster
    {
        /// <summary>
        /// Regalo del estreno (04 §2.5: «regala 1 Luciérnaga al stock con la tarjeta»): al cargar el nivel que estrena Aguas
        /// turbias (<paramref name="murkyDebut"/>: su <c>intro_mechanic</c> es <c>murky</c>) aún no superado
        /// (<paramref name="maxLevel"/> &lt; <paramref name="level"/>), si no se regaló ya en este nivel
        /// (<paramref name="alreadyGifted"/>, que <c>AF.GameFlow</c> guarda en <c>level_state</c> también al salir al inicio) y sin
        /// Luciérnagas en el stock, el mismo patrón que la Cuia extra del nivel 8. Así salir y volver a entrar no lo repite.
        /// </summary>
        public static bool ShouldGift(int level, bool murkyDebut, int maxLevel, bool alreadyGifted, Wallet? wallet, BoosterConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return wallet != null && murkyDebut && !alreadyGifted && level >= 1 && maxLevel < level && !wallet.Stock.Has(BoosterKind.Reveal);
        }

        /// <summary>Precio en semillas (<c>reveal_seed_price</c>; sin cartera, el default de 08 §9).</summary>
        public static int Price(Wallet? wallet)
        {
            return wallet?.Price(SeedSink.Reveal) ?? EconomyConfig.Defaults.RevealSeedPrice;
        }

        /// <summary><paramref name="payment"/> se puede cobrar ahora: hay stock, o el saldo llega al precio.</summary>
        public static bool CanPay(Wallet wallet, RevealPayment payment)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            switch (payment)
            {
                case RevealPayment.Stock:
                    return wallet.Stock.Has(BoosterKind.Reveal);
                case RevealPayment.Seeds:
                    return wallet.Missing(wallet.Price(SeedSink.Reveal)) == 0;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Cobra una Luciérnaga: una unidad del stock (<see cref="Transaction.UseStock"/>) o <c>reveal_seed_price</c> semillas
        /// (<see cref="Transaction.Buy"/> de <see cref="SeedSink.Reveal"/>). Quien llama ya aplicó y guardó el revelado (04 §2.5 d:
        /// aplicar → guardar → cobrar). Si se rechaza, no cambia nada.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="payment"/> no es un valor definido.</exception>
        public static TransactionResult Pay(Wallet wallet, RevealPayment payment)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            switch (payment)
            {
                case RevealPayment.Stock:
                    return wallet.Apply(Transaction.UseStock(BoosterKind.Reveal));
                case RevealPayment.Seeds:
                    return wallet.Apply(Transaction.Buy(SeedSink.Reveal));
                default:
                    throw new ArgumentOutOfRangeException(nameof(payment), payment, "Pago de la Luciérnaga desconocido: Stock o Seeds.");
            }
        }
    }
}
