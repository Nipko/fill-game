#nullable enable
using System;
using AF.Platform;

namespace AF.Economy
{
    /// <summary>
    /// <see cref="IStreakStore"/> en memoria, para tests y el editor. <see cref="Load"/> da el estado vivo, pasado al día local del
    /// reloj (<see cref="StreakRecord.RollOver"/>); cada guardado que «llega al disco» deja una copia independiente en
    /// <see cref="Persisted"/>, como haría el perfil. Con <see cref="FailWrites"/> o <see cref="FailuresLeft"/> simula un disco que
    /// no escribe: el cambio sigue en memoria y <see cref="PendingSave"/> se queda en true hasta un guardado o un <see cref="Flush"/>
    /// que sí escriba.
    /// </summary>
    public sealed class InMemoryStreakStore : IStreakStore
    {
        private readonly IClock _clock;
        private StreakRecord _live;

        /// <summary>Crea el almacén vacío o con <paramref name="initial"/> ya guardado (se copia).</summary>
        public InMemoryStreakStore(IClock clock, StreakRecord? initial = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _live = initial == null ? new StreakRecord() : initial.Clone();
            Persisted = _live.Clone();
        }

        /// <summary>Copia de lo último que llegó al «disco».</summary>
        public StreakRecord Persisted { get; private set; }

        /// <summary>Todas las escrituras fallan mientras sea true.</summary>
        public bool FailWrites { get; set; }

        /// <summary>Las próximas <c>n</c> escrituras fallan.</summary>
        public int FailuresLeft { get; set; }

        /// <summary>Escrituras que llegaron al «disco».</summary>
        public int WriteCount { get; private set; }

        /// <summary>Escrituras que fallaron.</summary>
        public int FailedWriteCount { get; private set; }

        /// <inheritdoc />
        public bool PendingSave { get; private set; }

        /// <inheritdoc />
        public StreakRecord Load()
        {
            _live.RollOver(_clock.LocalDateKey());
            return _live;
        }

        /// <inheritdoc />
        public void Save(StreakRecord record)
        {
            _live = record ?? throw new ArgumentNullException(nameof(record));
            Write();
        }

        /// <inheritdoc />
        public bool Flush()
        {
            if (PendingSave)
            {
                Write();
            }

            return !PendingSave;
        }

        /// <summary>
        /// Simula reabrir la app: el estado vivo pasa a ser una copia de <see cref="Persisted"/> (se pierde lo que no se escribió).
        /// </summary>
        public void Reopen()
        {
            _live = Persisted.Clone();
            PendingSave = false;
        }

        private void Write()
        {
            if (FailWrites || FailuresLeft > 0)
            {
                if (FailuresLeft > 0)
                {
                    FailuresLeft--;
                }

                FailedWriteCount++;
                PendingSave = true;
                return;
            }

            Persisted = _live.Clone();
            WriteCount++;
            PendingSave = false;
        }
    }
}
