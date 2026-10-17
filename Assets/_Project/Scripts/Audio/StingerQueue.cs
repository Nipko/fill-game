#nullable enable

namespace AF.Audio
{
    /// <summary>
    /// Cuándo suena un remate (tarea AU3; encargo de sonidos §8): nunca encima de la toada de la victoria. Si se pide mientras
    /// la toada suena (<see cref="AudioMix.ToadaLeftMs"/>), espera a que termine; uno nuevo sustituye al que esperaba. Valor (no
    /// clase): sin asignaciones.
    /// </summary>
    public struct StingerQueue
    {
        private bool _pending;
        private StingerId _id;

        /// <summary>Hay un remate esperando a que termine la toada.</summary>
        public bool Pending => _pending;

        /// <summary>El remate que espera (con <see cref="Pending"/>).</summary>
        public StingerId PendingId => _id;

        /// <summary>
        /// Se pide <paramref name="id"/>: true si puede sonar ya (no suena la toada); si no, queda esperando (en lugar del que
        /// esperaba) y devuelve false.
        /// </summary>
        public bool Request(StingerId id, float toadaLeftMs)
        {
            if (toadaLeftMs <= 0f)
            {
                _pending = false;
                return true;
            }

            _pending = true;
            _id = id;
            return false;
        }

        /// <summary>Cada fotograma: true si el que esperaba ya puede sonar (<paramref name="id"/>) porque la toada terminó.</summary>
        public bool Advance(float toadaLeftMs, out StingerId id)
        {
            id = _id;
            if (!_pending || toadaLeftMs > 0f)
            {
                return false;
            }

            _pending = false;
            return true;
        }

        /// <summary>Olvida el que esperaba (un anuncio, Música apagada).</summary>
        public void Clear()
        {
            _pending = false;
        }
    }
}
