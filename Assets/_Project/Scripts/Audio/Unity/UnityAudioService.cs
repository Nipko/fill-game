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
    /// Audio del juego (tareas AU1-AU3; D-026): solo el módulo de audio integrado de Unity (<c>com.unity.modules.audio</c>), sin
    /// paquetes. Desde AU3 suena con los clips grabados de la entrega 1 (<see cref="AudioClipRegistry"/>, que la escena da con
    /// <see cref="UseClips"/>) y el audio por código de AU1 y AU2 queda solo como respaldo de un id sin clip (y en los tests sin
    /// escena): si falta algún efecto grabado, un hilo de trabajo genera todo el PCM (<see cref="AudioBank"/>) y el hilo principal
    /// solo crea los clips (<c>AudioClip.Create</c> y <c>SetData</c>; <c>AF_PERF audio_build</c>). Suena con un pool de
    /// <see cref="OneShotVoices"/> fuentes para los efectos (con un tope por efecto: 6 tintes a la vez, 07 §9.2) y, para el chorro,
    /// los 4 bucles grabados fundidos por el nivel del destino (<see cref="PourBlend"/>, 4 fuentes por chorro) o, de respaldo, el
    /// bucle por código, que sube de tono con el nivel. La música, el ambiente y los remates (tareas AU2 y AU3) van en
    /// <c>UnityAudioService.Background.cs</c>. Las reglas de mezcla (ajustes al momento, modo silencio, anuncios con fundido de
    /// 0,5 s, atenuación del lecho) son las de <see cref="AudioMix"/>. Vive en un objeto que sobrevive a las recargas de escena (la
    /// reconstrucción por idioma) y pone un <c>AudioListener</c> si la escena no tiene. Sin asignaciones por efecto ni por
    /// fotograma.
    /// </summary>
    public sealed partial class UnityAudioService : MonoBehaviour, IAudioService
    {
        /// <summary>Fuentes para los efectos sueltos.</summary>
        public const int OneShotVoices = 12;

        /// <summary>Fuentes para los chorros simultáneos.</summary>
        public const int PourVoices = 3;

        /// <summary>Tintes a la vez como mucho (07 §9.2: «como mucho 6 voces a la vez»).</summary>
        public const int MaxTintVoices = 6;

        /// <summary>Fundido de entrada y salida del chorro (ms).</summary>
        public const float PourFadeMs = 30f;

        /// <summary>Cada cuánto se lee el modo del timbre (s).</summary>
        public const float RingerPollSeconds = 2f;

        /// <summary>Lo que avanza por milisegundo la posición sobre los 4 bucles del chorro (sin saltos al subir el nivel).</summary>
        public const float PourBlendPerMs = 0.006f;

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
        private readonly AudioSource[] _pourLayers = new AudioSource[PourVoices * PourBlend.Loops];
        private readonly bool[] _pourLayerOn = new bool[PourVoices * PourBlend.Loops];
        private readonly bool[] _pourRecorded = new bool[PourVoices];
        private readonly float[] _pourPos = new float[PourVoices];
        private readonly float[] _pourTargetPos = new float[PourVoices];

        private AudioClip?[] _clips = Array.Empty<AudioClip?>();
        private AudioBank? _bank;
        private Task<AudioBank>? _bankTask;
        private bool _adPaused;
        private float _ringerPoll;
        private IHaptics _haptics = NullHaptics.Instance;
        private AndroidRinger? _ringer;
        private AudioClipRegistry? _registry;
        private bool _sfxRecorded;
        private bool _pourLoopsRecorded;
        private bool _faunaRecorded;
        private bool _started;

        /// <summary>Sonda de desarrollo (<c>AF.UI.DevProbe</c>), o null.</summary>
        public IAudioProbe? Probe { get; set; }

        /// <summary>
        /// Los efectos ya pueden sonar: todos tienen clip grabado o el audio por código de respaldo ya creó los suyos (antes, los
        /// efectos sin clip se callan).
        /// </summary>
        public bool Ready => _sfxRecorded || SynthReady;

        /// <summary>El audio por código de respaldo ya creó sus clips.</summary>
        public bool SynthReady => _bank != null && _clips.Length == AudioBank.ClipCount;

        /// <summary>Se está generando o ya se generó el audio por código de respaldo (tests: con el registro completo, nunca).</summary>
        public bool SynthRequested => _bankTask != null || _bank != null;

        /// <summary>El registro de clips grabados en uso (null: solo el audio por código).</summary>
        public AudioClipRegistry? Clips => _registry;

        /// <summary>Los 61 efectos y las 28 notas de fruta tienen clip grabado.</summary>
        public bool EffectsRecorded => _sfxRecorded;

        /// <summary>Lo que costó crear los clips del respaldo en el hilo principal (ms).</summary>
        public double MainThreadMs { get; private set; }

        /// <summary>Las reglas de mezcla con su estado (tests y sonda).</summary>
        public AudioMix Mix => _mix;

        /// <summary>
        /// El servicio del juego: lo crea la primera vez y después devuelve el mismo, también tras recargar la escena. El respaldo
        /// por código se empieza a generar en otro hilo en su primer fotograma si hace falta (<see cref="UseClips"/>).
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
                for (int k = 0; k < PourBlend.Loops; k++)
                {
                    AudioSource layer = NewSource("Pour" + i + "Loop" + (k + 1));
                    layer.loop = true;
                    _pourLayers[(i * PourBlend.Loops) + k] = layer;
                }
            }

            AwakeBackground();
            _haptics = AndroidHaptics.ForPlatform();
            _ringer = AndroidRinger.ForPlatform();
            _mix.Ringer = _ringer.Read();
        }

        private void Start()
        {
            if (Object.FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>(); // la escena del juego no trae: sin él no se oye nada
            }

            // El arranque da el registro justo después de crear el servicio (UseClips): aquí ya se sabe si hace falta el respaldo.
            _started = true;
            StartSynthIfNeeded();
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

        /// <summary>
        /// Usa los clips grabados de <paramref name="registry"/> (tarea AU3; null: solo el audio por código). Lo llama el arranque
        /// con el registro de la escena, también tras recargarla (con el mismo registro no cambia nada). Al cambiarlo, lo que suena
        /// se calla y se vuelve a pedir con los clips nuevos; si falta algún efecto, bucle del chorro o animal grabado, se genera
        /// el respaldo por código.
        /// </summary>
        public void UseClips(AudioClipRegistry? registry)
        {
            if (ReferenceEquals(registry, _registry))
            {
                return;
            }

            StopEffects();
            ReleaseBackground();
            _registry = registry;
            _sfxRecorded = registry != null && AllRecorded(registry, 0, AudioBank.ClipCount);
            _pourLoopsRecorded = registry != null && AllRecorded(registry, AudioClipIds.PourLoopBase, AudioClipIds.PourLoops);
            _faunaRecorded = registry != null && AllRecorded(registry, AudioClipIds.FaunaBase, AudioClipIds.FaunaTotal);
            if (_started)
            {
                StartSynthIfNeeded();
            }
        }

        private static bool AllRecorded(AudioClipRegistry registry, int first, int count)
        {
            for (int i = first; i < first + count; i++)
            {
                if (!registry.Has(i))
                {
                    return false;
                }
            }

            return true;
        }

        // El respaldo por código (AU1) solo se genera si algún efecto, bucle del chorro o animal no tiene clip grabado.
        private void StartSynthIfNeeded()
        {
            if (_sfxRecorded && _pourLoopsRecorded && _faunaRecorded)
            {
                return;
            }

            if (_bank == null && _bankTask == null)
            {
                _bankTask = Task.Run(() => AudioBank.Build());
            }
        }

        // El clip del efecto de índice index (AudioBank.Index, que es su ranura en el registro): el grabado si lo hay; si no, el
        // del respaldo por código (null mientras se genera).
        private AudioClip? EffectClip(int index, out bool recorded)
        {
            AudioClip? clip = _registry != null ? _registry.Get(index) : null;
            recorded = clip != null;
            if (clip != null)
            {
                return clip;
            }

            return SynthReady && index < _clips.Length ? _clips[index] : null;
        }

        /// <inheritdoc />
        public void Play(SfxId id, int tint)
        {
            int index = AudioBank.Index(id, tint);
            if (id == SfxId.WinToada && _mix.SfxAudible)
            {
                // El lecho baja 6 dB mientras suena (07 §9.4) y los remates esperan a que termine: lo que dura el clip que va a
                // sonar, el WAV o el del respaldo por código (tarea FX7); sin clip todavía, el largo fijo.
                AudioClip? toada = EffectClip(index, out _);
                _mix.ToadaStarted(toada != null ? toada.length * 1000f : AudioMix.ToadaDuckMs);
            }

            PlayIndex(index, id, tint);
        }

        private void PlayIndex(int index, SfxId id, int tint)
        {
            if (!_mix.SfxAudible)
            {
                return;
            }

            AudioClip? clip = EffectClip(index, out bool recorded);
            if (clip == null)
            {
                return;
            }

            int voice = PickVoice(index, id);
            AudioSource source = _voices[voice];
            source.Stop();
            source.clip = clip;
            source.pitch = 1f; // las notas de fruta grabadas ya van afinadas: sin cambio de tono por código
            source.volume = _mix.SfxGain;
            source.Play();
            _voiceClip[voice] = index;
            _voiceStart[voice] = Time.unscaledTime;

            // Grabado: el pico de su ficha (el de la especificación y del catálogo); por código: el medido al generarlo.
            Probe?.Sfx(SfxCatalog.Name(id), recorded ? SfxCatalog.Get(id).TargetPeakDb : _bank!.GainDb(index), tint, recorded);
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
            if (_pourLoopsRecorded)
            {
                // Los 4 bucles grabados, fundidos por el nivel del destino (AU3): empiezan en la posición del nivel.
                int recordedSlot = PourSlot(voice);
                _pours[recordedSlot].Stop();
                _pourKey[recordedSlot] = voice;
                _pourStopping[recordedSlot] = false;
                _pourVolume[recordedSlot] = 0f;
                _pourRecorded[recordedSlot] = true;
                _pourTargetPos[recordedSlot] = PourBlend.Position(level);
                _pourPos[recordedSlot] = _pourTargetPos[recordedSlot];
                ApplyPourLayers(recordedSlot, 0f);
                return;
            }

            AudioClip? clip = SynthReady ? _clips[(int)SfxId.PourLoop] : null;
            if (clip == null)
            {
                return;
            }

            int slot = PourSlot(voice);
            StopPourLayers(slot);
            _pourRecorded[slot] = false;
            AudioSource source = _pours[slot];
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
                    _pourTargetPos[i] = PourBlend.Position(level);
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

                if (free < 0 && (_pourKey[i] < 0 || !PourSounding(i)))
                {
                    free = i;
                }
            }

            return free >= 0 ? free : 0;
        }

        private bool PourSounding(int slot)
        {
            if (_pours[slot].isPlaying)
            {
                return true;
            }

            for (int k = 0; k < PourBlend.Loops; k++)
            {
                if (_pourLayerOn[(slot * PourBlend.Loops) + k])
                {
                    return true;
                }
            }

            return false;
        }

        // Los bucles grabados del chorro slot a volume: suenan los que pesan en la posición de ahora (como mucho dos vecinos),
        // cada uno afinado ±2 semitonos como mucho (PourBlend); los demás se paran en silencio, sin clic.
        private void ApplyPourLayers(int slot, float volume)
        {
            AudioClipRegistry? registry = _registry;
            if (registry == null)
            {
                return;
            }

            float pos = _pourPos[slot];
            for (int k = 0; k < PourBlend.Loops; k++)
            {
                int layer = (slot * PourBlend.Loops) + k;
                AudioSource source = _pourLayers[layer];
                float weight = PourBlend.Weight(pos, k);
                if (weight <= 0f)
                {
                    if (_pourLayerOn[layer])
                    {
                        source.Stop();
                        _pourLayerOn[layer] = false;
                    }

                    continue;
                }

                AudioClip? clip = registry.Get(AudioClipIds.PourLoopSlot(k));
                if (clip == null)
                {
                    continue;
                }

                if (source.clip != clip)
                {
                    source.clip = clip;
                }

                source.pitch = PourBlend.Pitch(PourBlend.Semitones(pos, k));
                source.volume = volume * weight;
                if (!_pourLayerOn[layer])
                {
                    // Cada chorro y cada bucle empiezan en otro punto: dos chorros a la vez no suenan en fase.
                    uint offset = ((uint)(_pourKey[slot] + 1) * 7919u) + ((uint)k * 104729u);
                    source.timeSamples = (int)(offset % (uint)Math.Max(1, clip.samples));
                    source.Play();
                    _pourLayerOn[layer] = true;
                }
            }
        }

        private void StopPourLayers(int slot)
        {
            for (int k = 0; k < PourBlend.Loops; k++)
            {
                _pourLayers[(slot * PourBlend.Loops) + k].Stop();
                _pourLayerOn[(slot * PourBlend.Loops) + k] = false;
            }
        }

        /// <summary>El chorro <paramref name="slot"/> (0-2) suena con los 4 bucles grabados (tests).</summary>
        public bool PourUsesClips(int slot)
        {
            return _pourKey[slot] >= 0 && _pourRecorded[slot];
        }

        /// <summary>Volumen del bucle grabado <paramref name="loop"/> (0-3) del chorro <paramref name="slot"/>; 0 si calla (tests).</summary>
        public float PourLayerVolume(int slot, int loop)
        {
            int layer = (slot * PourBlend.Loops) + loop;
            return _pourLayerOn[layer] ? _pourLayers[layer].volume : 0f;
        }

        /// <summary>Velocidad de reproducción del bucle grabado <paramref name="loop"/> del chorro <paramref name="slot"/> (tests).</summary>
        public float PourLayerPitch(int slot, int loop)
        {
            return _pourLayers[(slot * PourBlend.Loops) + loop].pitch;
        }

        /// <summary>Posición 0-3 del chorro <paramref name="slot"/> sobre los 4 bucles (tests).</summary>
        public float PourPosition(int slot)
        {
            return _pourPos[slot];
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
                SilenceBackground(); // Música apagada: el lecho, sus capas, el ambiente y los remates se callan en el acto
            }
        }

        /// <inheritdoc />
        public void SetAdActive(bool active)
        {
            _mix.SetAdActive(active);
            if (active)
            {
                StopEffects();
                PauseBackground(true);
                _adPaused = true;
            }
            else if (_adPaused)
            {
                _adPaused = false;
                PauseBackground(false); // vuelve desde 0 con el fundido de 0,5 s (AudioMix.MusicGain)
            }
        }

        /// <inheritdoc />
        public void SetModalDuck(bool on)
        {
            _mix.ModalDuck = on;
        }

        /// <summary>
        /// Lo de cada fotograma (lo llama <c>Update</c> con el tiempo real; los tests pueden llamarlo para medir que no asigna):
        /// fundidos, clips que terminan de generarse, modo silencio, chorros, música, ambiente, fauna y remates.
        /// </summary>
        public void Tick(float dtMs)
        {
            _mix.Tick(dtMs);
            CompleteBank();
            PollRinger(dtMs);
            UpdateVoices(dtMs);
            UpdateBackground(dtMs);
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
                StopPourLayers(i);
                _pourKey[i] = -1;
                _pourStopping[i] = false;
            }
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime * 1000f);
        }

        // Los clips del respaldo se crean en el hilo principal en cuanto el hilo de trabajo termina el PCM (AF_PERF audio_build).
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
                if (_pourRecorded[i])
                {
                    _pourPos[i] = Mathf.MoveTowards(_pourPos[i], _pourTargetPos[i], dtMs * PourBlendPerMs); // sin saltos
                    ApplyPourLayers(i, _pourVolume[i] * gain);
                }
                else
                {
                    source.volume = _pourVolume[i] * gain;
                    source.pitch = Mathf.MoveTowards(source.pitch, _pourPitch[i], dtMs * 0.004f); // sin saltos de tono
                }

                if (_pourStopping[i] && _pourVolume[i] <= 0f)
                {
                    source.Stop();
                    StopPourLayers(i);
                    _pourKey[i] = -1;
                    _pourStopping[i] = false;
                }
            }
        }
    }
}
