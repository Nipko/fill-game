#nullable enable
using System;
using AF.Core.Board;
using AF.Economy;

namespace AF.Boosters
{
    /// <summary>Estado del botón «+1 cuia» de la bandeja (04 §2.2).</summary>
    public enum ExtraCuiaState
    {
        /// <summary>Antes de <c>extra_cuia_unlock_level</c> la ranura no existe y la bandeja se centra (07 §7.4).</summary>
        Hidden = 0,

        /// <summary>Se puede usar: del stock o pagando.</summary>
        Ready = 1,

        /// <summary>Ya se usó en este nivel (<c>extra_cuia_max_per_level</c>): gris.</summary>
        Used = 2,

        /// <summary>No queda vertido útil (en el bloqueo se ofrece la Cuia de rescate), o ya hay 16 cuias: gris.</summary>
        Disabled = 3,
    }

    /// <summary>Lo que muestra el botón «+1 cuia»: stock «+N», precio o «Te faltan {n}» (04 §1.1 y §2.2).</summary>
    public readonly struct ExtraCuiaOffer
    {
        /// <summary>Crea la oferta.</summary>
        public ExtraCuiaOffer(ExtraCuiaState state, int stock, int price, int missing)
        {
            State = state;
            Stock = stock;
            Price = price;
            Missing = missing;
        }

        /// <summary>Estado.</summary>
        public ExtraCuiaState State { get; }

        /// <summary>Unidades en el stock (se gastan antes que las semillas).</summary>
        public int Stock { get; }

        /// <summary>Precio en semillas (<c>extra_cuia_seed_price</c>).</summary>
        public int Price { get; }

        /// <summary>Semillas que faltan si no hay stock (0 si alcanza): precio en rojo y «Te faltan {n}».</summary>
        public int Missing { get; }

        /// <summary>Se paga con el stock.</summary>
        public bool FromStock => Stock > 0;

        /// <summary>Se puede tocar (lista, y con stock o saldo).</summary>
        public bool CanUse => State == ExtraCuiaState.Ready && (FromStock || Missing == 0);
    }

    /// <summary>
    /// Cuia extra · +1 cuia (GDD 04 §2.2; 08 §9 <c>extra_cuia_*</c>): comodín planificado que añade una cuia vacía de
    /// capacidad 4 a la fila más corta. Ranura desde el nivel 8 con 1 unidad regalada en ese nivel; 150 semillas; 1 por nivel
    /// por cualquier vía; solo mientras quede algún vertido útil (en el bloqueo, lo que se ofrece es la Cuia de rescate);
    /// persiste al reiniciar. Sin anuncio. Gasta primero el stock. Sin motor.
    /// </summary>
    public static class ExtraCuiaBooster
    {
        /// <summary>La ranura existe en <paramref name="level"/>.</summary>
        public static bool IsUnlocked(int level, BoosterConfig config)
        {
            return level >= config.ExtraCuiaUnlockLevel;
        }

        /// <summary>
        /// Estado del botón en <paramref name="level"/>: <paramref name="usedThisLevel"/> Cuias extra ya aplicadas,
        /// <paramref name="hasUsefulMove"/> (la partida está en juego, no atascada), <paramref name="cuias"/> en pantalla, el
        /// stock y el saldo de <paramref name="wallet"/>.
        /// </summary>
        public static ExtraCuiaOffer Evaluate(int level, int usedThisLevel, bool hasUsefulMove, int cuias, Wallet? wallet, BoosterConfig config)
        {
            int stock = wallet?.Stock.Get(BoosterKind.ExtraCuia) ?? 0;
            int price = wallet?.Price(SeedSink.ExtraCuia) ?? EconomyConfig.Defaults.ExtraCuiaSeedPrice;
            int missing = stock > 0 ? 0 : (wallet == null ? price : wallet.Missing(price));
            if (!IsUnlocked(level, config))
            {
                return new ExtraCuiaOffer(ExtraCuiaState.Hidden, stock, price, missing);
            }

            if (usedThisLevel >= config.ExtraCuiaMaxPerLevel)
            {
                return new ExtraCuiaOffer(ExtraCuiaState.Used, stock, price, missing);
            }

            if (!hasUsefulMove || cuias >= BoardState.MaxCuias)
            {
                return new ExtraCuiaOffer(ExtraCuiaState.Disabled, stock, price, missing);
            }

            return new ExtraCuiaOffer(ExtraCuiaState.Ready, stock, price, missing);
        }

        /// <summary>
        /// Cobra una Cuia extra: una unidad del stock si la hay; si no, <c>extra_cuia_seed_price</c> semillas
        /// (<see cref="Transaction.Buy"/>). Si se rechaza, no cambia nada (<see cref="TransactionResult.Missing"/> dice cuánto falta).
        /// </summary>
        public static TransactionResult Pay(Wallet wallet)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            return wallet.Stock.Has(BoosterKind.ExtraCuia)
                ? wallet.Apply(Transaction.UseStock(BoosterKind.ExtraCuia))
                : wallet.Apply(Transaction.Buy(SeedSink.ExtraCuia));
        }

        /// <summary>
        /// Regalo del tutorial (04 §2.2 y §4): al cargar el nivel de desbloqueo por primera vez (aún no superado:
        /// <paramref name="maxLevel"/> &lt; nivel) y sin Cuia extra en el stock, Capi trae 1. El perfil aún no guarda que el
        /// regalo se hizo: el stock vacío y el nivel sin superar evitan acumular más de una.
        /// </summary>
        public static bool ShouldGift(int level, int maxLevel, Wallet? wallet, BoosterConfig config)
        {
            return wallet != null && level == config.ExtraCuiaUnlockLevel && maxLevel < level
                && !wallet.Stock.Has(BoosterKind.ExtraCuia);
        }
    }
}
