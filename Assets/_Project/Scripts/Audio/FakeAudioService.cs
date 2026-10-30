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

        /// <summary>Un animal del ambiente (<see cref="AudioEvent.Sfx"/>; tarea AU2).</summary>
        Fauna = 4,

        /// <summary>Un remate musical (<see cref="AudioEvent.Stinger"/>; tarea AU3).</summary>
        Stinger = 5,
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
            Stinger = default;
        }

        /// <summary>Crea el evento de un remate (tarea AU3).</summary>
        public AudioEvent(StingerId stinger, bool audible)
        {
            Kind = AudioEventKind.Stinger;
            Sfx = default;
            Tint = 0;
            Haptic = default;
            Audible = audible;
            Stinger = stinger;
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

        /// <summary>El remate (con <see cref="AudioEventKind.Stinger"/>).</summary>
        public StingerId Stinger { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            string what = Kind == AudioEventKind.Haptic ? Haptics.Name(Haptic)
                : Kind == AudioEventKind.Sfx ? SfxCatalog.Name(Sfx)
                : Kind == AudioEventKind.Fauna ? "fauna:" + SfxCatalog.Name(Sfx)
                : Kind == AudioEventKind.Stinger ? "sting:" + Stinger : Kind.ToString();
            return what + (Tint != 0 ? "#" + Tint : string.Empty) + (Audible ? string.Empty : " (mudo)");
        }
    }

    /// <summary>
    /// Servicio de audio de los tests: no suena; anota cada petición con las mismas reglas de <see cref="AudioMix"/> que el real
    /// (ajustes, modo silencio, anuncio) para comprobar qué se oiría. Desde AU2 lleva también la música y el ambiente pedidos,
    /// la fauna (al avanzar con <see cref="Tick"/>, que en los tests llama el propio test) y la sonda de la música y del
    /// ambiente (<see cref="Probe"/>), como el real. Asigna al anotar: solo en tests.
    /// </summary>
    public sealed class FakeAudioService : IAudioService
    {
        private readonly List<AudioEvent> _events = new List<AudioEvent>();

        /// <summary>Las reglas de mezcla, con el estado de ahora.</summary>
        public AudioMix Mix { get; } = new AudioMix();

        /// <summary>Lo pedido, en orden.</summary>
        public IReadOnlyList<AudioEvent> Events => _events;

        /// <summary>Sonda de la música y del ambiente (o null), como la del servicio real.</summary>
        public IAudioProbe? Probe { get; set; }

        /// <summary>Mundo del lecho pedido por última vez (0 = ninguno).</summary>
        public int MusicWorld => Mix.MusicWorld;

        /// <summary>Capas del lecho pedidas por última vez.</summary>
        public int MusicLayers => Mix.MusicLayers;

        /// <summary>Se pidió la variante nocturna.</summary>
        public bool MusicNight => Mix.MusicNight;

        /// <summary>Suena el tema de menú.</summary>
        public bool MusicMenu => Mix.MusicMenu;

        /// <summary>Mundo del ambiente pedido por última vez (0 = ninguno).</summary>
        public int AmbientWorld => Mix.AmbientWorld;

        /// <summary>El ambiente pedido por última vez lleva lluvia.</summary>
        public bool AmbientRain => Mix.AmbientRain;

        /// <summary>Nivel con el que se pidió el ambiente por última vez.</summary>
        public int AmbientLevel { get; private set; }

        /// <summary>Veces que se pidió un ambiente.</summary>
        public int AmbientRequests { get; private set; }

        /// <summary>Animales que sonaron (o, con <paramref name="audibleOnly"/>, que se oyeron).</summary>
        public int FaunaCount(bool audibleOnly)
        {
            int n = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].Kind == AudioEventKind.Fauna && (!audibleOnly || _events[i].Audible))
                {
                    n++;
                }
            }

            return n;
        }

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

        /// <summary>Veces que se pidió (o, con <paramref name="audibleOnly"/>, que se habría oído) el remate <paramref name="id"/>.</summary>
        public int StingerCount(StingerId id, bool audibleOnly)
        {
            int n = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                AudioEvent e = _events[i];
                if (e.Kind == AudioEventKind.Stinger && e.Stinger == id && (!audibleOnly || e.Audible))
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
            else if (id == SfxId.Seal && Mix.SfxAudible)
            {
                Mix.SealStarted();
            }

            _events.Add(new AudioEvent(AudioEventKind.Sfx, id, tint, default, Mix.SfxAudible));
            ReportMusic();
        }

        /// <inheritdoc />
        public void PourBegin(int voice, float level)
        {
            ActivePours++;
            LastPourLevel = level;
            Mix.SetPouring(true);
            _events.Add(new AudioEvent(AudioEventKind.PourBegin, SfxId.PourLoop, voice, default, Mix.SfxAudible));
            ReportMusic();
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
            Mix.SetPouring(ActivePours > 0);
            _events.Add(new AudioEvent(AudioEventKind.PourEnd, SfxId.PourLoop, voice, default, Mix.SfxAudible));
            ReportMusic();
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
            ReportMusic();
        }

        /// <inheritdoc />
        public void SetMusic(int world, int layers, bool night)
        {
            Mix.SetMusic(world, layers, night);
            ReportMusic();
        }

        /// <inheritdoc />
        public void SetMenuMusic()
        {
            Mix.SetMenuMusic();
            ReportMusic();
        }

        /// <inheritdoc />
        public void SetAmbient(int world, bool rain, int level)
        {
            Mix.SetAmbient(world, rain, level);
            AmbientLevel = level;
            AmbientRequests++;
            Probe?.Ambient(Mix.AmbientWorld, Mix.AmbientRain, level, string.Empty, string.Empty);
        }

        /// <inheritdoc />
        public void PlayStinger(StingerId id)
        {
            _events.Add(new AudioEvent(id, Mix.MusicAudible));
        }

        /// <inheritdoc />
        public void Tick(float dtMs)
        {
            Mix.Tick(dtMs);
            if (Mix.AdvanceFauna(dtMs, out SfxId fauna))
            {
                _events.Add(new AudioEvent(AudioEventKind.Fauna, fauna, 0, default, Mix.AmbientAudible));
            }

            ReportMusic();
        }

        private void ReportMusic()
        {
            if (Probe != null && Mix.TakeMusicReport(out MusicReport report))
            {
                Probe.Music(report, string.Empty);
            }
        }
    }
}
