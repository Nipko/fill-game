#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// <see cref="IServerDate"/> para el editor y los tests (08 §2.1, principio 6: cada SDK detrás de una interfaz con
    /// implementación falsa): sin red (<see cref="Offline"/>) o con un desfase servidor − dispositivo fijado por el test
    /// (<see cref="Online"/>). Se puede conectar y desconectar en mitad de un test. En la 1.0, sin backend, el juego usa
    /// <see cref="Offline"/>: todo premio sale confirmado y la reconciliación no se ejecuta (nota §7.2).
    /// </summary>
    public sealed class FakeServerDate : IServerDate
    {
        private bool _online;
        private TimeSpan _offset;

        private FakeServerDate(bool online, TimeSpan offset)
        {
            _online = online;
            _offset = offset;
        }

        /// <summary>Sin red: <see cref="TryGetServerOffset"/> devuelve false.</summary>
        public static FakeServerDate Offline()
        {
            return new FakeServerDate(false, TimeSpan.Zero);
        }

        /// <summary>Con red y el desfase <paramref name="serverMinusDevice"/> (servidor − dispositivo; negativo si el dispositivo va adelantado).</summary>
        public static FakeServerDate Online(TimeSpan serverMinusDevice)
        {
            return new FakeServerDate(true, serverMinusDevice);
        }

        /// <summary>Hay red ahora.</summary>
        public bool IsOnline => _online;

        /// <summary>Pierde la red (el último desfase se olvida).</summary>
        public void GoOffline()
        {
            _online = false;
            _offset = TimeSpan.Zero;
        }

        /// <summary>Recupera la red con el desfase <paramref name="serverMinusDevice"/>.</summary>
        public void GoOnline(TimeSpan serverMinusDevice)
        {
            _online = true;
            _offset = serverMinusDevice;
        }

        /// <inheritdoc />
        public bool TryGetServerOffset(out TimeSpan serverMinusDevice)
        {
            serverMinusDevice = _online ? _offset : TimeSpan.Zero;
            return _online;
        }
    }
}
