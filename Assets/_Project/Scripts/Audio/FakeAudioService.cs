#nullable enable
using System.Collections.Generic;

namespace AF.Audio
{
    /// <summary>Qué pidió el juego al servicio de audio (tests).</summary>
    public enum AudioEventKind
    {
        /// <summary>Un efecto (<see cref="AudioEvent.Sfx"/>).</summary>
        Sfx = 0,

        /// <summary>Un háptico (<see cref="AudioEvent.Haptic"/>).</summary>
        Haptic = 1,

        /// <summary>Empieza un chorro.</summary>
        PourBegin = 2,

        /// <summary>Se corta un chorro.</summary>
        PourEnd = 3,
    }

    /// <summary>Un evento de <see cref="FakeAudioService"/>: qué se pidió y si se habría oído (o sentido) con los ajustes de ese momento.</summary>
    public readonly struct AudioEvent
    {
        /// <summary>Crea el evento.</summary>
        public AudioEvent(AudioEventKind kind, SfxId sfx, int tint, HapticKind haptic, bool audible)
        {
            Kind = kind;
            Sfx = sfx;
            Tint = tint;
            Haptic = haptic;
            Audible = audible;
        }

        /// <summary>Tipo.</summary>
        public AudioEventKind Kind { get; }

        /// <summary>El efecto (con <see cref="AudioEventKind.Sfx"/>).</summary>
        public SfxId Sfx { get; }

        /// <summary>Fruta del efecto afinado (0 = ninguna), o la voz del chorro.</summary>
        public int Tint { get; }

        /// <summary>El háptico (con <see cref="AudioEventKind.Haptic"/>).</summary>
        public HapticKind Haptic { get; }

        /// <summary>Se habría oído (efecto o chorro) o sentido (háptico).</summary>
        public bool Audible { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            string what = Kind == AudioEventKind.Haptic ? Haptics.Name(Haptic) : Kind == AudioEventKind.Sfx ? SfxCatalog.Name(Sfx) : Kind.ToString();
            return what + (Tint != 0 ? "#" + Tint : string.Empty) + (Audible ? string.Empty : " (mudo)");
        }
    }

    /// <summary>
    /// Servicio de audio de los tests: no suena; anota cada petición con las mismas reglas de <see cref="AudioMix"/> que el real
    /// (ajustes, modo silencio, anuncio) para comprobar qué se oiría. Asigna al anotar: solo en tests.
    /// </summary>
    public sealed class FakeAudioService : IAudioService
    {
        private readonly List<AudioEvent> _events = new List<AudioEvent>();

        /// <summary>Las reglas de mezcla, con el estado de ahora.</summary>
        public AudioMix Mix { get; } = new AudioMix();

        /// <summary>Lo pedido, en orden.</summary>
        public IReadOnlyList<AudioEvent> Events => _events;

        /// <summary>Mundo y capas del lecho pedidos por última vez.</summary>
        public int MusicWorld { get; private set; }

        /// <summary>Capas del lecho pedidas por última vez.</summary>
        public int MusicLayers { get; private set; }

        /// <summary>Último nivel de cada voz de chorro (0-1).</summary>
        public float LastPourLevel { get; private set; }

        /// <summary>Chorros que suenan ahora.</summary>
        public int ActivePours { get; private set; }

        /// <summary>Olvida lo anotado.</summary>
        public void Clear()
        {
            _events.Clear();
        }

        /// <summary>Se pidió el efecto <paramref name="id"/> (oído o no).</summary>
        public bool Requested(SfxId id)
        {
            return Count(id, false) > 0;
        }

        /// <summary>Se oyó el efecto <paramref name="id"/>.</summary>
        public bool Heard(SfxId id)
        {
            return Count(id, true) > 0;
        }

        /// <summary>Veces que se pidió (o, con <paramref name="audibleOnly"/>, que se oyó) <paramref name="id"/>.</summary>
        public int Count(SfxId id, bool audibleOnly)
        {
            int n = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                AudioEvent e = _events[i];
                if (e.Kind == AudioEventKind.Sfx && e.Sfx == id && (!audibleOnly || e.Audible))
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>Hápticos <paramref name="kind"/> sentidos.</summary>
        public int HapticCount(HapticKind kind)
        {
            int n = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].Kind == AudioEventKind.Haptic && _events[i].Haptic == kind && _events[i].Audible)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>Hápticos sentidos, de cualquier tipo.</summary>
        public int HapticTotal()
        {
            int n = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].Kind == AudioEventKind.Haptic && _events[i].Audible)
                {
                    n++;
                }
            }

            return n;
        }

        /// <inheritdoc />
        public void Play(SfxId id, int tint)
        {
            if (id == SfxId.WinToada && Mix.SfxAudible)
            {
                Mix.ToadaStarted();
            }

            _events.Add(new AudioEvent(AudioEventKind.Sfx, id, tint, default, Mix.SfxAudible));
        }

        /// <inheritdoc />
        public void PourBegin(int voice, float level)
        {
            ActivePours++;
            LastPourLevel = level;
            _events.Add(new AudioEvent(AudioEventKind.PourBegin, SfxId.PourLoop, voice, default, Mix.SfxAudible));
        }

        /// <inheritdoc />
        public void PourLevel(int voice, float level)
        {
            LastPourLevel = level;
        }

        /// <inheritdoc />
        public void PourEnd(int voice)
        {
            ActivePours = ActivePours > 0 ? ActivePours - 1 : 0;
            _events.Add(new AudioEvent(AudioEventKind.PourEnd, SfxId.PourLoop, voice, default, Mix.SfxAudible));
        }

        /// <inheritdoc />
        public void Haptic(HapticKind kind)
        {
            _events.Add(new AudioEvent(AudioEventKind.Haptic, default, 0, kind, Mix.HapticsAllowed));
        }

        /// <inheritdoc />
        public void Configure(SoundSettings settings)
        {
            Mix.Settings = settings;
        }

        /// <inheritdoc />
        public void SetAdActive(bool active)
        {
            Mix.SetAdActive(active);
        }

        /// <inheritdoc />
        public void SetModalDuck(bool on)
        {
            Mix.ModalDuck = on;
        }

        /// <inheritdoc />
        public void SetMusic(int world, int layers)
        {
            MusicWorld = world;
            MusicLayers = layers;
        }

        /// <inheritdoc />
        public void Tick(float dtMs)
        {
            Mix.Tick(dtMs);
        }
    }
}
