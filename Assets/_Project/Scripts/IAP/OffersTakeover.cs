#nullable enable
using System;

namespace AF.IAP
{
    /// <summary>
    /// El cupo de pantallas que salen solas (takeovers) en la sesión: <c>ui_max_takeovers_per_session</c> = 1 (08 §9; 05 §5,
    /// <c>offer_scheduler</c>: «máximo 1 takeover por sesión»; 06 §4.7). Lo comparten la Bitácora pendiente, los eventos, las ofertas
    /// y la liga (orden de 06 §4.7). Con B4 integrado, el arranque lo implementa sobre su <c>TakeoverQueue</c>
    /// (<c>QuotaLeft</c> y <c>MarkShown(TakeoverKind.Offer)</c>; nota <c>docs/dev/mercado.md</c> §6).
    /// </summary>
    public interface ITakeoverBudget
    {
        /// <summary>Queda cupo en la sesión.</summary>
        bool QuotaLeft { get; }

        /// <summary>Una oferta salió sola: gasta el cupo.</summary>
        void Spend();
    }

    /// <summary>
    /// Cupo por sesión sin B4: <paramref name="max"/> takeovers por sesión, con la sesión que da <c>sessionNumber</c> (el
    /// <c>SessionTracker</c> de B6: arranque en frío o vuelta tras ≥ 30 min fuera, Q-M4 (a)). Al cambiar el número, el cupo vuelve
    /// entero. Sin asignaciones al consultarlo.
    /// </summary>
    public sealed class SessionTakeoverBudget : ITakeoverBudget
    {
        private readonly Func<int> _sessionNumber;
        private readonly int _max;
        private int _session = int.MinValue;
        private int _spent;

        /// <summary>Crea el cupo.</summary>
        /// <param name="sessionNumber">Número de la sesión en curso.</param>
        /// <param name="max"><c>ui_max_takeovers_per_session</c> (08 §9: 1, rango 1).</param>
        public SessionTakeoverBudget(Func<int> sessionNumber, int max = 1)
        {
            _sessionNumber = sessionNumber ?? throw new ArgumentNullException(nameof(sessionNumber));
            if (max < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max));
            }

            _max = max;
        }

        /// <summary>Takeovers gastados en la sesión en curso.</summary>
        public int Spent
        {
            get
            {
                Sync();
                return _spent;
            }
        }

        /// <inheritdoc />
        public bool QuotaLeft
        {
            get
            {
                Sync();
                return _spent < _max;
            }
        }

        /// <inheritdoc />
        public void Spend()
        {
            Sync();
            _spent++;
        }

        private void Sync()
        {
            int session = _sessionNumber();
            if (session != _session)
            {
                _session = session;
                _spent = 0;
            }
        }
    }
}
