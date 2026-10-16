#nullable enable
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AF.Audio.Unity
{
    /// <summary>
    /// Audio provisional por código en el juego (tarea AU1; D-026): solo el módulo de audio integrado de Unity
    /// (<c>com.unity.modules.audio</c>), sin paquetes. Al arrancar, un hilo de trabajo genera todo el PCM (<see cref="AudioBank"/>)
    /// y el hilo principal solo crea los clips (<c>AudioClip.Create</c> y <c>SetData</c>; <c>AF_PERF audio_build</c>). Suena con un
    /// pool de <see cref="OneShotVoices"/> fuentes para los efectos (con un tope por efecto: 6 tintes a la vez, 07 §9.2), tres para
    /// el bucle del chorro (su tono sigue al nivel del destino) y tres para el lecho del mundo y sus capas, sincronizadas. Las
    /// reglas de mezcla (ajustes al momento, modo silencio, anuncios con fundido de 0,5 s, atenuación del lecho) son las de
    /// <see cref="AudioMix"/>. Vive en un objeto que sobrevive a las recargas de escena (la reconstrucción por idioma) y pone un
    /// <c>AudioListener</c> si la escena no tiene. Sin asignaciones por efecto.
    /// </summary>
    public sealed class UnityAudioService : MonoBehaviour, IAudioService
    {
        /// <summary>Fuentes para los efectos sueltos.</summary>
        public const int OneShotVoices = 12;

        /// <summary>Fuentes para los chorros simultáneos.</summary>
        public const int PourVoices = 3;

        /// <summary>Tintes a la vez como mucho (07 §9.2: «como mucho 6 voces a la vez»).</summary>
        public const int MaxTintVoices = 6;

        /// <summary>Fundido de entrada y salida del chorro (ms).</summary>
        public const float PourFadeMs = 30f;

        /// <summary>Fundido de las capas del lecho (ms).</summary>
        public const float LayerFadeMs = 1000f;

        /// <summary>Cada cuánto se lee el modo del timbre (s).</summary>
        public const float RingerPollSeconds = 2f;

        private static UnityAudioService? s_instance;

        private readonly AudioMix _mix = new AudioMix();
        private readonly AudioSource[] _voices = new AudioSource[OneShotVoices];
        private readonly int[] _voiceClip = new int[OneShotVoices];
        private readonly float[] _voiceStart = new float[OneShotVoices];
        private readonly AudioSource[] _pours = new AudioSource[PourVoices];
        private readonly int[] _pourKey = new int[PourVoices];
        private readonly float[] _pourVolume = new float[PourVoices];
        private readonly bool[] _pourStopping = new bool[PourVoices];
        private readonly float[] _pourPitch = new float[PourVoices];
        private readonly AudioSource[] _music = new AudioSource[MusicSynth.LayerCount];
        private readonly float[] _layerVolume = new float[MusicSynth.LayerCount];

        private AudioClip?[] _clips = Array.Empty<AudioClip?>();
        private AudioBank? _bank;
        private Task<AudioBank>? _bankTask;
        private Task<float[][]>? _musicTask;
        private int _musicTaskWorld;
        private int _musicWorld;
        private int _musicWanted;
        private int _musicLayers;
        private AudioClip?[] _musicClips = new AudioClip?[MusicSynth.LayerCount];
        private bool _adPaused;
        private float _ringerPoll;
        private IHaptics _haptics = NullHaptics.Instance;
        private AndroidRinger? _ringer;

        /// <summary>Sonda de desarrollo (<c>AF.UI.DevProbe</c>), o null.</summary>
        public IAudioProbe? Probe { get; set; }

        /// <summary>Los clips ya están creados (antes, los efectos se callan).</summary>
        public bool Ready => _bank != null && _clips.Length == AudioBank.ClipCount;

        /// <summary>Lo que costó crear los clips en el hilo principal (ms).</summary>
        public double MainThreadMs { get; private set; }

        /// <summary>Las reglas de mezcla con su estado (tests y sonda).</summary>
        public AudioMix Mix => _mix;

        /// <summary>
        /// El servicio del juego: lo crea la primera vez (y empieza a generar los clips en otro hilo) y después devuelve el mismo,
        /// también tras recargar la escena.
        /// </summary>
        public static UnityAudioService Ensure()
        {
            if (s_instance != null)
            {
                return s_instance;
            }

            var host = new GameObject("AF.Audio");
            DontDestroyOnLoad(host);
            UnityAudioService service = host.AddComponent<UnityAudioService>();
            s_instance = service;
            return service;
        }

        private void Awake()
        {
            for (int i = 0; i < OneShotVoices; i++)
            {
                _voices[i] = NewSource("Voice" + i);
                _voiceClip[i] = -1;
            }

            for (int i = 0; i < PourVoices; i++)
            {
                _pours[i] = NewSource("Pour" + i);
                _pours[i].loop = true;
                _pourKey[i] = -1;
            }

            for (int i = 0; i < MusicSynth.LayerCount; i++)
            {
                _music[i] = NewSource("Music" + i);
                _music[i].loop = true;
            }

            _haptics = AndroidHaptics.ForPlatform();
            _ringer = AndroidRinger.ForPlatform();
            _mix.Ringer = _ringer.Read();
            _bankTask = Task.Run(() => AudioBank.Build());
        }

        private void Start()
        {
            if (Object.FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>(); // la escena del juego no trae: sin él no se oye nada
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus && _ringer != null)
            {
                _mix.Ringer = _ringer.Read(); // el modo silencio pudo cambiar fuera del juego
            }
        }

        private AudioSource NewSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        /// <inheritdoc />
        public void Play(SfxId id, int tint)
        {
            int index = AudioBank.Index(id, tint);
            if (id == SfxId.WinToada && _mix.SfxAudible)
            {
                _mix.ToadaStarted(); // el lecho baja 6 dB mientras suena (07 §9.4)
            }

            PlayIndex(index, id, tint);
        }

        private void PlayIndex(int index, SfxId id, int tint)
        {
            if (!_mix.SfxAudible || !Ready)
            {
                return;
            }

            AudioClip? clip = _clips[index];
            if (clip == null)
            {
                return;
            }

            int voice = PickVoice(index, id);
            AudioSource source = _voices[voice];
            source.Stop();
            source.clip = clip;
            source.pitch = 1f;
            source.volume = _mix.SfxGain;
            source.Play();
            _voiceClip[voice] = index;
            _voiceStart[voice] = Time.unscaledTime;
            Probe?.Sfx(SfxCatalog.Name(id), _bank!.GainDb(index), tint);
        }

        // Una fuente para el clip: si el efecto ya suena en su tope de voces, la más antigua de las suyas; si no, una libre o la
        // más antigua de todas.
        private int PickVoice(int index, SfxId id)
        {
            int limit = id == SfxId.Tint ? MaxTintVoices : id == SfxId.FruitPlip ? 4 : 2;
            int same = 0;
            int oldestSame = -1;
            int free = -1;
            int oldest = 0;
            for (int i = 0; i < OneShotVoices; i++)
            {
                bool playing = _voices[i].isPlaying;
                if (!playing)
                {
                    if (free < 0)
                    {
                        free = i;
                    }

                    continue;
                }

                if (AudioBank.IdOf(_voiceClip[i]) == id)
                {
                    same++;
                    if (oldestSame < 0 || _voiceStart[i] < _voiceStart[oldestSame])
                    {
                        oldestSame = i;
                    }
                }

                if (_voiceStart[i] < _voiceStart[oldest])
                {
                    oldest = i;
                }
            }

            if (same >= limit && oldestSame >= 0)
            {
                return oldestSame;
            }

            return free >= 0 ? free : oldest;
        }

        /// <inheritdoc />
        public void PourBegin(int voice, float level)
        {
            if (!Ready)
            {
                return;
            }

            int slot = PourSlot(voice);
            AudioSource source = _pours[slot];
            AudioClip? clip = _clips[(int)SfxId.PourLoop];
            if (clip == null)
            {
                return;
            }

            if (source.clip != clip)
            {
                source.clip = clip;
            }

            _pourKey[slot] = voice;
            _pourStopping[slot] = false;
            _pourVolume[slot] = 0f;
            _pourPitch[slot] = PourLoopSynth.PitchForLevel(level);
            source.pitch = _pourPitch[slot];
            source.volume = 0f;
            if (!source.isPlaying)
            {
                // Cada chorro empieza en otro punto del bucle: dos a la vez no suenan en fase.
                source.timeSamples = (int)(((uint)(voice + 1) * 7919u) % (uint)Math.Max(1, clip.samples));
                source.Play();
            }
        }

        /// <inheritdoc />
        public void PourLevel(int voice, float level)
        {
            for (int i = 0; i < PourVoices; i++)
            {
                if (_pourKey[i] == voice && !_pourStopping[i])
                {
                    _pourPitch[i] = PourLoopSynth.PitchForLevel(level);
                }
            }
        }

        /// <inheritdoc />
        public void PourEnd(int voice)
        {
            for (int i = 0; i < PourVoices; i++)
            {
                if (_pourKey[i] == voice)
                {
                    _pourStopping[i] = true;
                }
            }
        }

        private int PourSlot(int voice)
        {
            int free = -1;
            for (int i = 0; i < PourVoices; i++)
            {
                if (_pourKey[i] == voice)
                {
                    return i;
                }

                if (free < 0 && (_pourKey[i] < 0 || !_pours[i].isPlaying))
                {
                    free = i;
                }
            }

            return free >= 0 ? free : 0;
        }

        /// <inheritdoc />
        public void Haptic(HapticKind kind)
        {
            if (!_mix.HapticsAllowed)
            {
                return;
            }

            _haptics.Vibrate(kind);
            Probe?.Haptic(Haptics.Name(kind));
        }

        /// <inheritdoc />
        public void Configure(SoundSettings settings)
        {
            _mix.Settings = settings;
            if (!settings.Sound)
            {
                StopEffects(); // Sonido apagado: lo que suena se calla en el acto (07 §9.4)
            }

            if (!settings.Music)
            {
                for (int i = 0; i < _music.Length; i++)
                {
                    _music[i].volume = 0f;
                }
            }
        }

        /// <inheritdoc />
        public void SetAdActive(bool active)
        {
            _mix.SetAdActive(active);
            if (active)
            {
                StopEffects();
                for (int i = 0; i < _music.Length; i++)
                {
                    _music[i].Pause();
                }

                _adPaused = true;
            }
            else if (_adPaused)
            {
                _adPaused = false;
                for (int i = 0; i < _music.Length; i++)
                {
                    _music[i].volume = 0f;
                    _music[i].UnPause();
                }
            }
        }

        /// <inheritdoc />
        public void SetModalDuck(bool on)
        {
            _mix.ModalDuck = on;
        }

        /// <inheritdoc />
        public void SetMusic(int world, int layers)
        {
            _musicWanted = world;
            _musicLayers = layers;
        }

        /// <inheritdoc />
        public void Tick(float dtMs)
        {
        }

        private void StopEffects()
        {
            for (int i = 0; i < OneShotVoices; i++)
            {
                _voices[i].Stop();
            }

            for (int i = 0; i < PourVoices; i++)
            {
                _pours[i].Stop();
                _pourKey[i] = -1;
                _pourStopping[i] = false;
            }
        }

        private void Update()
        {
            float dtMs = Time.unscaledDeltaTime * 1000f;
            _mix.Tick(dtMs);
            CompleteBank();
            PollRinger(dtMs);
            UpdateVoices(dtMs);
            UpdateMusic(dtMs);
        }

        // Los clips se crean en el hilo principal en cuanto el hilo de trabajo termina el PCM (AF_PERF audio_build).
        private void CompleteBank()
        {
            if (_bank != null || _bankTask == null || !_bankTask.IsCompleted)
            {
                return;
            }

            if (_bankTask.IsFaulted)
            {
                Debug.LogError("AF.Audio: no se pudieron generar los efectos: " + _bankTask.Exception?.GetBaseException().Message);
                _bankTask = null;
                return;
            }

            AudioBank bank = _bankTask.Result;
            _bankTask = null;
            var watch = Stopwatch.StartNew();
            var clips = new AudioClip?[AudioBank.ClipCount];
            for (int i = 0; i < AudioBank.ClipCount; i++)
            {
                float[] pcm = bank.Pcm(i);
                AudioClip clip = AudioClip.Create(SfxCatalog.Name(AudioBank.IdOf(i)) + "_" + i, pcm.Length, 1, Dsp.SampleRate, false);
                clip.SetData(pcm, 0);
                clips[i] = clip;
            }

            watch.Stop();
            MainThreadMs = watch.Elapsed.TotalMilliseconds;
            _clips = clips;
            _bank = bank;
            Probe?.Build(bank.BuildMs, MainThreadMs, AudioBank.ClipCount, (bank.Samples * 4L) / 1024L);
        }

        private void PollRinger(float dtMs)
        {
            _ringerPoll -= dtMs / 1000f;
            if (_ringerPoll > 0f || _ringer == null)
            {
                return;
            }

            _ringerPoll = RingerPollSeconds;
            RingerMode mode = _ringer.Read();
            if (mode != _mix.Ringer)
            {
                _mix.Ringer = mode;
                if (mode != RingerMode.Normal)
                {
                    StopEffects();
                }
            }
        }

        private void UpdateVoices(float dtMs)
        {
            float gain = _mix.SfxGain;
            for (int i = 0; i < OneShotVoices; i++)
            {
                if (_voices[i].isPlaying)
                {
                    _voices[i].volume = gain;
                }
            }

            float step = dtMs / PourFadeMs;
            for (int i = 0; i < PourVoices; i++)
            {
                AudioSource source = _pours[i];
                if (_pourKey[i] < 0)
                {
                    continue;
                }

                _pourVolume[i] = _pourStopping[i] ? Math.Max(0f, _pourVolume[i] - step) : Math.Min(1f, _pourVolume[i] + step);
                source.volume = _pourVolume[i] * gain;
                source.pitch = Mathf.MoveTowards(source.pitch, _pourPitch[i], dtMs * 0.004f); // sin saltos de tono
                if (_pourStopping[i] && _pourVolume[i] <= 0f)
                {
                    source.Stop();
                    _pourKey[i] = -1;
                    _pourStopping[i] = false;
                }
            }
        }

        private void UpdateMusic(float dtMs)
        {
            if (_musicWanted != _musicWorld && _musicTask == null && _musicWanted > 0)
            {
                int world = _musicWanted;
                _musicTaskWorld = world;
                _musicTask = Task.Run(() => MusicSynth.RenderLayers(world));
            }
            else if (_musicWanted == 0 && _musicWorld != 0)
            {
                for (int i = 0; i < _music.Length; i++)
                {
                    _music[i].Stop();
                }

                _musicWorld = 0;
            }

            if (_musicTask != null && _musicTask.IsCompleted)
            {
                Task<float[][]> task = _musicTask;
                _musicTask = null;
                if (!task.IsFaulted)
                {
                    StartMusic(_musicTaskWorld, task.Result);
                }
            }

            if (_musicWorld == 0 || _adPaused)
            {
                return;
            }

            float gain = _mix.MusicGain;
            float step = dtMs / LayerFadeMs;
            for (int l = 0; l < MusicSynth.LayerCount; l++)
            {
                float target = l == 0 || l <= _musicLayers ? 1f : 0f;
                _layerVolume[l] = target > _layerVolume[l] ? Math.Min(target, _layerVolume[l] + step) : Math.Max(target, _layerVolume[l] - step);
                _music[l].volume = _layerVolume[l] * gain;
            }
        }

        private void StartMusic(int world, float[][] layers)
        {
            double at = AudioSettings.dspTime + 0.1;
            for (int l = 0; l < MusicSynth.LayerCount; l++)
            {
                AudioSource source = _music[l];
                source.Stop();
                AudioClip? old = _musicClips[l];
                AudioClip clip = AudioClip.Create("music_w" + world + "_" + l, layers[l].Length, 1, MusicSynth.SampleRate, false);
                clip.SetData(layers[l], 0);
                _musicClips[l] = clip;
                source.clip = clip;
                source.volume = 0f;
                _layerVolume[l] = 0f;
                source.PlayScheduled(at); // las tres capas, a la vez y en el mismo punto del bucle
                if (old != null)
                {
                    Destroy(old);
                }
            }

            _musicWorld = world;
        }
    }
}
