#nullable enable
using System;
using AF.Platform;

namespace AF.Economy
{
    /// <summary>
    /// <see cref="IStreakStore"/> en memoria, para tests y el editor, con las mismas reglas que el del perfil: guarda su propio
    /// estado (el que tendría el perfil), <see cref="Load"/> lo pasa al día local del reloj (<see cref="StreakRecord.RollOver"/>),
    /// lo recorta (<see cref="StreakRecord.Normalize"/>) y lo copia en el registro vivo; <see cref="Save"/> lo lleva de vuelta con
    /// <see cref="StreakRecord.MergeDay"/> (la regla conservadora para un registro de un día anterior). Cada guardado que «llega al
    /// disco» deja una copia independiente en <see cref="Persisted"/>. Con <see cref="FailWrites"/> o <see cref="FailuresLeft"/>
    /// simula un disco que no escribe: el cambio sigue en memoria y <see cref="PendingSave"/> se queda en true hasta un guardado o
    /// un <see cref="Flush"/> que sí escriba. <see cref="SimulateDayChangeByAnotherModule"/> hace lo que la cartera o los anuncios
    /// hacen con <c>daily</c> a medianoche.
    /// </summary>
    public sealed class InMemoryStreakStore : IStreakStore
    {
        private readonly IClock _clock;
        private StreakRecord _state;
        private StreakRecord _live = new StreakRecord();
        private StreakRecord _loaded = new StreakRecord();

        /// <summary>Crea el almacén vacío o con <paramref name="initial"/> ya guardado (se copia).</summary>
        public InMemoryStreakStore(IClock clock, StreakRecord? initial = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _state = initial == null ? new StreakRecord() : initial.Clone();
            Persisted = _state.Clone();
        }

        /// <summary>Copia de lo último que llegó al «disco».</summary>
        public StreakRecord Persisted { get; private set; }

        /// <summary>Copia del estado actual del almacén (lo que tendría el perfil en memoria).</summary>
        public StreakRecord Current => _state.Clone();

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
            _state.RollOver(_clock.LocalDateKey());
            _state.Normalize();
            Copy(_state, _live);
            _loaded = _state.Clone();
            return _live;
        }

        /// <inheritdoc />
        public void Save(StreakRecord record)
        {
            _live = record ?? throw new ArgumentNullException(nameof(record));
            _state.Wins = record.Wins;
            _state.RiverFull = record.RiverFull;
            StreakRecord.MergeDay(_state, _loaded, record);
            _loaded = record.Clone();
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
        /// Simula que otro módulo pasa el día local al del reloj sin pasar por <see cref="Load"/> (como hacen la cartera o los
        /// anuncios con <c>daily</c>): el registro vivo que ya se entregó no cambia.
        /// </summary>
        public void SimulateDayChangeByAnotherModule()
        {
            _state.RollOver(_clock.LocalDateKey());
        }

        /// <summary>
        /// Simula reabrir la app: el estado pasa a ser una copia de <see cref="Persisted"/> (se pierde lo que no se escribió) y el
        /// siguiente <see cref="Load"/> entrega un registro nuevo.
        /// </summary>
        public void Reopen()
        {
            _state = Persisted.Clone();
            _live = new StreakRecord();
            _loaded = new StreakRecord();
            PendingSave = false;
        }

        private static void Copy(StreakRecord from, StreakRecord to)
        {
            to.Wins = from.Wins;
            to.RiverFull = from.RiverFull;
            to.DateLocal = from.DateLocal;
            to.ShieldAvailable = from.ShieldAvailable;
            to.SeedsToday = from.SeedsToday;
            to.HitsToday.Clear();
            to.HitsToday.AddRange(from.HitsToday);
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

            Persisted = _state.Clone();
            WriteCount++;
            PendingSave = false;
        }
    }
}
