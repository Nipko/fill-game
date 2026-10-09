#nullable enable
using System;
using AF.Economy;

namespace AF.Boosters
{
    /// <summary>De dónde salió una Marea (04 §1.1: primero el cupo gratis, luego el stock).</summary>
    public enum UndoSource
    {
        /// <summary>No había Marea disponible (sin cupo ni stock): toca recargar.</summary>
        None = 0,

        /// <summary>Niveles 1-10: ilimitada y sin contador (<c>undo_unlimited_until_level</c>).</summary>
        Unlimited = 1,

        /// <summary>Del cupo gratis del nivel (<c>undo_free_per_level</c>).</summary>
        Free = 2,

        /// <summary>Del stock comprado o regalado (<c>stock.undo</c>).</summary>
        Stock = 3,
    }

    /// <summary>Lo que muestra el botón de Marea (bandeja y modal; 04 §1.1 y §2.1).</summary>
    public readonly struct UndoStatus
    {
        /// <summary>Crea el estado.</summary>
        public UndoStatus(bool unlimited, int freeLeft, int stock)
        {
            Unlimited = unlimited;
            FreeLeft = freeLeft;
            Stock = stock;
        }

        /// <summary>Ilimitada y sin contador (niveles 1-10).</summary>
        public bool Unlimited { get; }

        /// <summary>Cupo gratis que queda en el nivel (badge blanco).</summary>
        public int FreeLeft { get; }

        /// <summary>Stock (badge «+N» en ámbar semilla).</summary>
        public int Stock { get; }

        /// <summary>Queda alguna Marea (ilimitada, cupo o stock); si no, el botón ofrece la recarga.</summary>
        public bool HasQuota => Unlimited || FreeLeft > 0 || Stock > 0;
    }

    /// <summary>
    /// Cupo de Marea por nivel (GDD 04 §2.1 y §1.1; 08 §9 <c>undo_free_per_level</c> y <c>undo_unlimited_until_level</c>):
    /// ilimitada y sin contador en 1-10; desde el 11, 3 gratis por nivel, restaurados al reiniciar o al cargar el nivel. Sin
    /// cupo se gasta el stock (<c>stock.undo</c>, con <see cref="Transaction.UseStock"/>); sin stock, el botón ofrece la
    /// recarga «+3 · Ver video» (<c>rv_undo_refill</c>) o «+3 · 30 semillas» (<see cref="SeedSink.UndoRefill"/>), que entran al
    /// stock. Que haya historial que deshacer lo decide la partida. Sin motor; un solo hilo.
    /// </summary>
    public sealed class UndoQuota
    {
        private readonly BoosterConfig _config;

        /// <summary>Crea el cupo (sin nivel: ilimitado hasta <see cref="ResetForLevel"/>).</summary>
        public UndoQuota(BoosterConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Nivel del cupo (0 si aún no hay).</summary>
        public int Level { get; private set; }

        /// <summary>Marea ilimitada en el nivel.</summary>
        public bool Unlimited => Level == 0 || _config.UndoUnlimited(Level);

        /// <summary>Cupo gratis que queda.</summary>
        public int FreeLeft { get; private set; }

        /// <summary>Al cargar el nivel <paramref name="level"/>: cupo entero (04 §1.1).</summary>
        public void ResetForLevel(int level)
        {
            Level = level;
            Restore();
        }

        /// <summary>Volver a la orilla: el cupo vuelve a <c>undo_free_per_level</c> (04 §2.7).</summary>
        public void Restore()
        {
            FreeLeft = Unlimited ? 0 : _config.UndoFreePerLevel;
        }

        /// <summary>
        /// Al reabrir el nivel guardado (<c>level_state.undo_free</c>; 02 §14; tarea B2-5): el cupo gratis que quedaba, entre 0 y
        /// <c>undo_free_per_level</c>. En 1-10 no hay cupo que restaurar.
        /// </summary>
        public void RestoreFreeLeft(int freeLeft)
        {
            FreeLeft = Unlimited ? 0 : Math.Max(0, Math.Min(freeLeft, _config.UndoFreePerLevel));
        }

        /// <summary>Estado del botón con <paramref name="stock"/> Mareas en el stock.</summary>
        public UndoStatus Status(int stock)
        {
            return new UndoStatus(Unlimited, FreeLeft, Math.Max(0, stock));
        }

        /// <summary>
        /// Gasta una Marea: nada en 1-10, una del cupo o una del stock de <paramref name="wallet"/> (rechazada si no hay). No
        /// deshace nada: eso lo hace la partida después, si esto no devuelve <see cref="UndoSource.None"/>.
        /// </summary>
        public UndoSource Consume(Wallet? wallet)
        {
            if (Unlimited)
            {
                return UndoSource.Unlimited;
            }

            if (FreeLeft > 0)
            {
                FreeLeft--;
                return UndoSource.Free;
            }

            if (wallet != null && wallet.Stock.Has(BoosterKind.Undo)
                && wallet.Apply(Transaction.UseStock(BoosterKind.Undo)).IsApplied)
            {
                return UndoSource.Stock;
            }

            return UndoSource.None;
        }

        /// <summary>Devuelve la Marea gastada si el deshacer no llegó a aplicarse (nunca se cobra sin entregar).</summary>
        public void Refund(UndoSource source, Wallet? wallet)
        {
            if (source == UndoSource.Free)
            {
                FreeLeft++;
            }
            else if (source == UndoSource.Stock && wallet != null)
            {
                wallet.Apply(Transaction.GrantStock(BoosterKind.Undo, 1));
            }
        }
    }
}
