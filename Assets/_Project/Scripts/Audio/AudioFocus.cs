#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Audio
{
    /// <summary>
    /// El foco de audio del teléfono (observación 10 de la jornada 6: con una alarma sonando, la música del juego seguía de fondo;
    /// D-026, añadido del 2026-10-29). Lo da Android (<c>AudioManager.requestAudioFocus</c>, plugin <c>AfAudioFocus.androidlib</c>)
    /// y lo aplica <see cref="AudioMix"/>.
    /// </summary>
    public enum AudioFocusState
    {
        /// <summary>No se tiene pedido (Editor, segundo plano, Música apagada): todo suena como siempre.</summary>
        None = 0,

        /// <summary>El juego tiene el foco: todo suena.</summary>
        Gain = 1,

        /// <summary>
        /// Otro suena un momento por encima y deja oír lo de debajo (<c>AUDIOFOCUS_LOSS_TRANSIENT_CAN_DUCK</c>: un aviso, la voz del
        /// navegador): la música y el ambiente bajan a <see cref="AudioMix.FocusDuckGain"/>; los efectos siguen.
        /// </summary>
        Duck = 2,

        /// <summary>
        /// Otro toma el foco un momento (<c>AUDIOFOCUS_LOSS_TRANSIENT</c>: una alarma, el timbre de una llamada): la música, el
        /// ambiente y los efectos se pausan hasta <see cref="Gain"/>.
        /// </summary>
        LossTransient = 3,

        /// <summary>
        /// Otro toma el foco sin plazo (<c>AUDIOFOCUS_LOSS</c>: otra app de música): se pausa todo; Android no lo devuelve solo y el
        /// juego lo vuelve a pedir al recuperar la ventana o al terminar un anuncio.
        /// </summary>
        Loss = 4,

        /// <summary>Android no dio el foco al pedirlo (p. ej., durante una llamada): suena como sin foco.</summary>
        Denied = 5,
    }

    /// <summary>Textos del foco de audio (los del plugin Java y los de la sonda <c>AF_STATE audio_focus</c>), sin asignar.</summary>
    public static class AudioFocusStates
    {
        /// <summary>Texto de <see cref="AudioFocusState.None"/>.</summary>
        public const string None = "none";

        /// <summary>Texto de <see cref="AudioFocusState.Gain"/> (<c>AfAudioFocus.GAIN</c>).</summary>
        public const string Gain = "gain";

        /// <summary>Texto de <see cref="AudioFocusState.Duck"/> (<c>AfAudioFocus.DUCK</c>).</summary>
        public const string Duck = "duck";

        /// <summary>Texto de <see cref="AudioFocusState.LossTransient"/> (<c>AfAudioFocus.LOSS_TRANSIENT</c>).</summary>
        public const string LossTransient = "loss_transient";

        /// <summary>Texto de <see cref="AudioFocusState.Loss"/> (<c>AfAudioFocus.LOSS</c>).</summary>
        public const string Loss = "loss";

        /// <summary>Texto de <see cref="AudioFocusState.Denied"/> (<c>AfAudioFocus.DENIED</c>).</summary>
        public const string Denied = "denied";

        /// <summary>El texto de <paramref name="state"/>.</summary>
        public static string Name(AudioFocusState state)
        {
            switch (state)
            {
                case AudioFocusState.Gain:
                    return Gain;
                case AudioFocusState.Duck:
                    return Duck;
                case AudioFocusState.LossTransient:
                    return LossTransient;
                case AudioFocusState.Loss:
                    return Loss;
                case AudioFocusState.Denied:
                    return Denied;
                default:
                    return None;
            }
        }

        /// <summary>
        /// El estado de un texto del plugin: <c>gain</c>, <c>duck</c>, <c>loss_transient</c>, <c>loss</c> o <c>denied</c>; cualquier
        /// otro (<c>error</c>, vacío, null) es <see cref="AudioFocusState.None"/>: suena como siempre.
        /// </summary>
        public static AudioFocusState Parse(string? text)
        {
            switch (text)
            {
                case Gain:
                    return AudioFocusState.Gain;
                case Duck:
                    return AudioFocusState.Duck;
                case LossTransient:
                    return AudioFocusState.LossTransient;
                case Loss:
                    return AudioFocusState.Loss;
                case Denied:
                    return AudioFocusState.Denied;
                default:
                    return AudioFocusState.None;
            }
        }

        /// <summary>Con <paramref name="state"/> se pausan la música, el ambiente y los efectos.</summary>
        public static bool Pauses(AudioFocusState state)
        {
            return state == AudioFocusState.LossTransient || state == AudioFocusState.Loss;
        }
    }

    /// <summary>
    /// El foco de audio de la plataforma: en Android, <c>AF.Audio.Unity.AndroidAudioFocus</c> (plugin <c>AfAudioFocus.androidlib</c>);
    /// en el Editor y en los tests, <see cref="FakeAudioFocusPort"/>. Los avisos de Android llegan en otro hilo: el puerto los guarda y
    /// el juego los toma en su fotograma (<see cref="TryTake"/>).
    /// </summary>
    public interface IAudioFocusPort
    {
        /// <summary>Pide el foco: <see cref="AudioFocusState.Gain"/> si se da, <see cref="AudioFocusState.Denied"/> si no y <see cref="AudioFocusState.None"/> si no se pudo pedir.</summary>
        AudioFocusState Request();

        /// <summary>Suelta el foco (ya no llegan avisos).</summary>
        void Abandon();

        /// <summary>El último aviso de Android desde la vez anterior, si llegó alguno (sin asignar).</summary>
        bool TryTake(out AudioFocusState state);
    }

    /// <summary>
    /// <see cref="IAudioFocusPort"/> del Editor y de los tests: da el foco al pedirlo (o lo que diga <see cref="RequestResult"/>) y
    /// deja simular los avisos de Android con <see cref="Push"/>.
    /// </summary>
    public sealed class FakeAudioFocusPort : IAudioFocusPort
    {
        private readonly Queue<AudioFocusState> _pushed = new Queue<AudioFocusState>();

        /// <summary>Lo que devuelve <see cref="Request"/>.</summary>
        public AudioFocusState RequestResult { get; set; } = AudioFocusState.Gain;

        /// <summary>Veces que se pidió.</summary>
        public int Requests { get; private set; }

        /// <summary>Veces que se soltó.</summary>
        public int Abandons { get; private set; }

        /// <summary>Se tiene pedido (tras un <see cref="Request"/> concedido y sin <see cref="Abandon"/>).</summary>
        public bool Held { get; private set; }

        /// <inheritdoc />
        public AudioFocusState Request()
        {
            Requests++;
            Held = RequestResult == AudioFocusState.Gain;
            return RequestResult;
        }

        /// <inheritdoc />
        public void Abandon()
        {
            Abandons++;
            Held = false;
            _pushed.Clear();
        }

        /// <summary>Simula un aviso de Android (una alarma: <see cref="AudioFocusState.LossTransient"/>; su fin: <see cref="AudioFocusState.Gain"/>).</summary>
        public void Push(AudioFocusState state)
        {
            _pushed.Enqueue(state);
        }

        /// <inheritdoc />
        public bool TryTake(out AudioFocusState state)
        {
            if (_pushed.Count == 0)
            {
                state = AudioFocusState.None;
                return false;
            }

            // Como el puerto de Android: si llegaron varios, cuenta el último.
            do
            {
                state = _pushed.Dequeue();
            }
            while (_pushed.Count > 0);
            return true;
        }
    }

    /// <summary>
    /// Cuándo se pide el foco de audio y en qué estado está (D-026, añadido del 2026-10-29), sin motor y sin asignar:
    /// <list type="bullet">
    /// <item>Se tiene pedido mientras el juego está en primer plano y con la Música encendida (<see cref="Want"/>): con la Música
    /// apagada no se pide, para no cortar la música de otra app del jugador [P].</item>
    /// <item>Los avisos de Android (<see cref="Poll"/>) cambian el estado solo con el foco pedido.</item>
    /// <item>Tras un <see cref="AudioFocusState.Loss"/> o un <see cref="AudioFocusState.Denied"/>, Android no lo devuelve solo:
    /// <see cref="Reacquire"/> lo vuelve a pedir al recuperar la ventana o al terminar un anuncio.</item>
    /// </list>
    /// </summary>
    public sealed class AudioFocusTracker
    {
        private readonly IAudioFocusPort _port;

        /// <summary>Crea el seguimiento sobre <paramref name="port"/>.</summary>
        public AudioFocusTracker(IAudioFocusPort port)
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
        }

        /// <summary>El puerto.</summary>
        public IAudioFocusPort Port => _port;

        /// <summary>El estado de ahora (<see cref="AudioFocusState.None"/> sin pedir).</summary>
        public AudioFocusState State { get; private set; }

        /// <summary>Se tiene pedido.</summary>
        public bool Wanted { get; private set; }

        /// <summary>Lo pide (<paramref name="want"/>) o lo suelta si cambia. Devuelve si cambió <see cref="State"/>.</summary>
        public bool Want(bool want)
        {
            if (want == Wanted)
            {
                return false;
            }

            Wanted = want;
            AudioFocusState before = State;
            if (want)
            {
                State = _port.Request();
            }
            else
            {
                _port.Abandon();
                State = AudioFocusState.None;
            }

            return State != before;
        }

        /// <summary>
        /// Con el foco pedido y perdido sin plazo (<see cref="AudioFocusState.Loss"/>) o negado, lo vuelve a pedir. Devuelve si cambió
        /// <see cref="State"/>.
        /// </summary>
        public bool Reacquire()
        {
            if (!Wanted || (State != AudioFocusState.Loss && State != AudioFocusState.Denied))
            {
                return false;
            }

            AudioFocusState before = State;
            State = _port.Request();
            return State != before;
        }

        /// <summary>Toma el último aviso de Android: devuelve si cambió <see cref="State"/>. Sin foco pedido, se descarta.</summary>
        public bool Poll()
        {
            if (!_port.TryTake(out AudioFocusState state) || !Wanted || state == State)
            {
                return false;
            }

            State = state;
            return true;
        }
    }

    /// <summary>
    /// Sonda del foco de audio (la implementa <c>AF.UI.DevProbe</c>, junto a <see cref="IAudioProbe"/>):
    /// <c>AF_STATE {"event":"audio_focus","state":"loss_transient","music_gain":0.00,"paused":true}</c> en cada cambio.
    /// </summary>
    public interface IAudioFocusProbe
    {
        /// <summary>
        /// Cambió el foco de audio a <paramref name="state"/> (<see cref="AudioFocusStates"/>): la ganancia de la música de ahora
        /// (sin el fundido de vuelta) y si la música y los efectos quedan en pausa.
        /// </summary>
        void AudioFocus(string state, float musicGain, bool paused);
    }
}
