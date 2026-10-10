#nullable enable
using System;
using AF.Boosters;
using AF.Core.Session;
using AF.Economy;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>
    /// Luciérnaga · Revelar (tarea T10 del paso 1; 04 §2.5; 02 §11 T6; 07 §7.6; 08 §9 <c>reveal_seed_price</c> y
    /// <c>rv_reveal</c>): el regalo del estreno de Aguas turbias (nivel 100), el modo de selección («toca el botón, las cuias
    /// turbias pulsan, toca la cuia; tocar fuera cancela sin costo»), el revelado con <see cref="LevelSession.Reveal"/> y el
    /// cobro con el stock o con semillas, en el orden de 04 §2.5 d: aplicar → guardar → cobrar, para no cobrar nunca sin
    /// entregar. Sin tope por nivel. La bandeja, el anuncio y la animación son de la vista (T14).
    /// </summary>
    public sealed partial class LevelFlow
    {
        /// <summary><c>intro_mechanic</c> del estreno de Aguas turbias (D-007: nivel 100), que trae la Luciérnaga regalada.</summary>
        public const string MurkyIntroMechanic = "murky";

        private bool _revealSelecting;
        private RevealPayment _revealPayment;

        /// <summary>
        /// La Luciérnaga se puede usar ahora: PLAYING sin victoria ni bloqueo pendiente (R15) y con dosis ocultas en el tablero
        /// (04 §2.5: no está en el modal de bloqueo y «se oculta si ya no quedan unidades ocultas»).
        /// </summary>
        public bool RevealAvailable => AcceptsBoardInput && _session!.HiddenCount > 0;

        /// <summary>El modo de selección de la Luciérnaga está abierto: el siguiente toque en una cuia elige cuál revelar.</summary>
        public bool RevealSelecting => _revealSelecting;

        /// <summary>Con qué se pagará la Luciérnaga del modo de selección abierto.</summary>
        public RevealPayment RevealSelectionPayment => _revealPayment;

        /// <summary>Luciérnagas en el stock (0 sin cartera).</summary>
        public int RevealStock => _wallet?.Stock.Get(BoosterKind.Reveal) ?? 0;

        /// <summary>Precio en semillas (<c>reveal_seed_price</c>, 120).</summary>
        public int RevealPrice => RevealBooster.Price(_wallet);

        /// <summary>
        /// Botón Revelar (04 §2.5): abre el modo de selección pagando con <paramref name="payment"/> (el stock o
        /// <c>reveal_seed_price</c> semillas), sin cobrar todavía. C8: primero baja la cuia seleccionada. False si la Luciérnaga
        /// no se puede usar ahora (<see cref="RevealAvailable"/>), si no hay cartera, si ya se estaba eligiendo o si el pago no
        /// alcanza (sin stock o «Te faltan {n}»: la vista lo avisa).
        /// </summary>
        public bool BeginRevealSelection(RevealPayment payment)
        {
            Wallet? wallet = _wallet;
            if (_revealSelecting || !RevealAvailable || wallet == null || !RevealBooster.CanPay(wallet, payment))
            {
                return false;
            }

            ClearSelection();
            _revealSelecting = true;
            _revealPayment = payment;
            (_listener as IRevealListener)?.OnRevealSelection(true);
            return true;
        }

        /// <summary>Cierra el modo de selección sin coste (04 §2.5: «tocar fuera cancela sin costo»). False si no estaba abierto.</summary>
        public bool CancelRevealSelection()
        {
            if (!_revealSelecting)
            {
                return false;
            }

            EndRevealSelection();
            return true;
        }

        /// <summary>
        /// Recompensa del video <c>rv_reveal</c> (04 §2.5: «1 revelado»; la vista ya vio el anuncio entero): +1 Luciérnaga al
        /// stock, que después se gasta con <see cref="BeginRevealSelection"/> y <see cref="RevealPayment.Stock"/>. False sin cartera.
        /// </summary>
        public bool GrantRevealVideoUnit()
        {
            return _wallet != null && _wallet.Apply(Transaction.GrantStock(BoosterKind.Reveal, 1)).IsApplied;
        }

        // Regalo del estreno de Aguas turbias al cargar el nivel (04 §2.5; D-007): 1 Luciérnaga al stock, una sola vez por nivel
        // (marca «l» de level_state, también al salir al inicio). Devuelve si se regaló ahora.
        private bool GiftRevealOnLoad(LevelDefinition def, int number)
        {
            bool murkyDebut = string.Equals(def.IntroMechanic, MurkyIntroMechanic, StringComparison.Ordinal);
            if (!RevealBooster.ShouldGift(number, murkyDebut, _progress.LoadMaxLevel(), _attempt.RevealGifted, _wallet, _boosters)
                || !_wallet!.Apply(Transaction.GrantStock(BoosterKind.Reveal, BoosterConfig.RevealGiftUnits)).IsApplied)
            {
                return false;
            }

            _attempt.RevealGifted = true;
            (_listener as IRevealListener)?.OnRevealGifted(BoosterConfig.RevealGiftUnits);
            return true;
        }

        // Toque en la cuia index con el modo de selección abierto. En una cuia con ocultas: aplicar (LevelSession.Reveal) →
        // guardar (level_state con la capa en la sección R) → cobrar (04 §2.5 d), en un solo lote de guardado del perfil; si
        // el estado cambia, el mismo camino que tras un vertido. En otra cuia (o si el pago ya no alcanza) cancela sin coste.
        // El toque no llega a la vista como OnTap: la vista recibe OnRevealSelection y OnRevealed.
        private TapResult TapRevealTarget(int index)
        {
            LevelSession session = _session!;
            if ((uint)index >= (uint)session.Current.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice de cuia fuera del tablero.");
            }

            int mask = session.Current[index].HiddenMask;
            Wallet? wallet = _wallet;
            RevealPayment payment = _revealPayment;
            if (mask == 0 || wallet == null || !RevealBooster.CanPay(wallet, payment))
            {
                EndRevealSelection();
                return TapResult.Ignored(index, _taps.Selected);
            }

            using (Saves())
            {
                session.Reveal(index); // 1. aplicar
                _revealSelecting = false;
                PersistState(); // 2. guardar: si la app se cierra ahora, lo revelado ya está y no se cobró
                RevealBooster.Pay(wallet, payment); // 3. cobrar (no puede faltar: se comprobó justo antes)
                if (_listener is IRevealListener reveal)
                {
                    reveal.OnRevealSelection(false);
                    reveal.OnRevealed(index, mask);
                }

                EvaluateStatus(); // como tras un vertido (R10 g-h): victoria o bloqueo
            }

            return TapResult.Ignored(index, _taps.Selected);
        }

        // Cierra el modo de selección, si estaba abierto, y lo avisa (sin coste). Lo llaman cancelar y cualquier otra acción.
        private void EndRevealSelection()
        {
            if (!_revealSelecting)
            {
                return;
            }

            _revealSelecting = false;
            (_listener as IRevealListener)?.OnRevealSelection(false);
        }
    }
}
