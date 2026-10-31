#nullable enable
using System;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AF.Audio.Unity
{
    /// <summary>
    /// Música, ambiente y remates del servicio de audio (tareas AU2 y AU3; 07 §9.1, §9.3 y §9.4):
    /// <list type="bullet">
    /// <item><description>el lecho del mundo y sus <see cref="MusicSynth.PercussionLayers"/> capas de percusión, en
    /// <see cref="MusicVoices"/> fuentes que arrancan en el mismo instante del reloj de audio (<c>PlayScheduled</c> con el mismo
    /// <c>dspTime</c>); las capas entran y salen con un fundido de <see cref="LayerFadeMs"/> según las cuias selladas, y la
    /// variante nocturna (Muy difícil) sustituye al lecho. Con clips grabados (mundos 1-9), el lecho y sus capas miden lo mismo
    /// muestra a muestra y se cargan en memoria («Compressed In Memory») antes de arrancar, así que se repiten juntos;</description></item>
    /// <item><description>el tema de menú del inicio, el Mercado y la Crecida, en su propia fuente: al pasar del inicio al nivel (o
    /// al revés, o a otro mundo) lo que suena se funde en <see cref="TrackFadeMs"/> y lo nuevo entra igual;</description></item>
    /// <item><description>el ambiente: el agua del mundo en bucle, la lluvia si el nivel la tiene (con su intensidad,
    /// <see cref="AmbientPlan.RainIntensityOf"/>) y un animal grabado del mundo cada 20-45 s (<see cref="AudioMix.AdvanceFauna(float, int, out int)"/>)
    /// en su propia fuente;</description></item>
    /// <item><description>los remates (<see cref="PlayStinger"/>), bajo los efectos y nunca encima de la toada.</description></item>
    /// </list>
    /// Lo que no tiene clip grabado (los mundos 10-13, o un registro incompleto) se genera por código en un hilo de trabajo la
    /// primera vez que se pide con Música encendida (con Música apagada no se genera nada) y se guarda el último de cada clase.
    /// Al ir al inicio, el lecho y el agua se funden y se paran sin soltarse, y al volver a un nivel del mismo mundo (y variante)
    /// suenan otra vez sin cargarse ni generarse; solo se sueltan cuando se pide otro mundo o variante (los grabados se descargan
    /// de memoria; los generados se destruyen). Todo va con la ganancia de <see cref="AudioMix.MusicGain"/> (ajuste Música, modo
    /// silencio, anuncio y atenuación de 6 dB), 3 dB por debajo de sus clips (<see cref="AudioMix.MusicLevelDb"/>). Sin asignaciones por fotograma: solo al cambiar de pieza.
    /// </summary>
    public sealed partial class UnityAudioService
    {
        /// <summary>Fuentes del lecho de un mundo: el lecho y sus capas de percusión.</summary>
        public const int MusicVoices = 1 + MusicSynth.PercussionLayers;

        /// <summary>Fundido de las capas del lecho (ms).</summary>
        public const float LayerFadeMs = 1000f;

        /// <summary>Fundido al cambiar de pieza (inicio, otro mundo o la variante nocturna; ms).</summary>
        public const float TrackFadeMs = 800f;

        /// <summary>Fundido del agua y de la lluvia al entrar o salir (ms).</summary>
        public const float AmbientFadeMs = 1000f;

        /// <summary>Margen con el que se programa el arranque del lecho y sus capas en el reloj de audio (s).</summary>
        public const double ScheduleLeadSeconds = 0.1;

        private readonly AudioSource[] _music = new AudioSource[MusicVoices];
        private readonly AudioClip?[] _musicClips = new AudioClip?[MusicVoices];
        private readonly AudioClip?[] _worldLoad = new AudioClip?[MusicVoices];
        private readonly double[] _musicScheduled = new double[MusicVoices];
        private readonly float[] _layerVolume = new float[MusicVoices];
        private AudioSource _menu = null!;
        private AudioSource _water = null!;
        private AudioSource _rain = null!;
        private AudioSource _fauna = null!;
        private AudioSource _stinger = null!;
        private AudioClip? _menuClip;
        private AudioClip? _waterClip;
        private AudioClip? _rainClip;
        private Task<MusicLoops>? _worldTask;
        private int _worldTaskKey;
        private int _worldLoadKey; // mundo × 2 (+1 de noche) de los clips grabados que se están cargando; 0 = ninguno
        private int _worldKey; // mundo × 2 (+1 de noche) de los clips del lecho; 0 = ninguno
        private bool _worldFromClips; // los clips del lecho son los grabados (se descargan al soltarse, nunca se destruyen)
        private bool _worldPlaying; // las fuentes del lecho están en marcha (paradas en el inicio, con los clips guardados)
        private int _worldRenders;
        private int _worldLoads;
        private Task<MusicLoops>? _menuTask;
        private bool _menuFromClip;
        private Task<float[]>? _waterTask;
        private int _waterTaskWorld;
        private int _waterWorld;
        private bool _waterFromClip;
        private bool _waterPlaying;
        private int _waterRenders;
        private Task<float[]>? _rainTask;
        private int _ambientLevel;
        private float _worldVolume;
        private float _menuVolume;
        private float _waterVolume;
        private float _rainVolume;
        private StingerQueue _stingers;
        private int _stingersStarted;
        private StingerId _lastStinger;

        /// <summary>Mundo y variante de los clips del lecho que hay (mundo × 2, +1 la nocturna; 0 = ninguno; tests).</summary>
        public int WorldBedKey => _worldKey;

        /// <summary>El lecho que hay es el grabado (tests).</summary>
        public bool WorldBedFromClips => _worldKey != 0 && _worldFromClips;

        /// <summary>Mundo del agua que hay (0 = ninguno; tests).</summary>
        public int WaterWorld => _waterWorld;

        /// <summary>El agua que hay es la grabada (tests).</summary>
        public bool WaterFromClip => _waterWorld != 0 && _waterFromClip;

        /// <summary>Lechos generados por código desde que existe el servicio (tests: volver al mismo mundo no genera otro).</summary>
        public int WorldBedRenders => _worldRenders;

        /// <summary>Lechos grabados cargados desde que existe el servicio (tests: volver al mismo mundo no carga otro).</summary>
        public int WorldBedLoads => _worldLoads;

        /// <summary>Aguas generadas por código desde que existe el servicio (tests).</summary>
        public int WaterRenders => _waterRenders;

        /// <summary>Ya está el tema de menú (tests).</summary>
        public bool MenuReady => _menuClip != null;

        /// <summary>Ya hay una lluvia puesta en su fuente (tests).</summary>
        public bool RainReady => _rain.clip != null;

        /// <summary>Remates que empezaron a sonar desde que existe el servicio (tests).</summary>
        public int StingersStarted => _stingersStarted;

        /// <summary>El último remate que empezó a sonar (tests).</summary>
        public StingerId LastStinger => _lastStinger;

        /// <summary>Hay un remate esperando a que termine la toada (tests).</summary>
        public bool StingerPending => _stingers.Pending;

        /// <summary>Suena un remate (tests).</summary>
        public bool StingerPlaying => _stinger.isPlaying;

        /// <summary>Volumen de la fuente <paramref name="voice"/> del lecho (0 = lecho, 1-3 = capas; tests).</summary>
        public float MusicVolume(int voice)
        {
            return _music[voice].volume;
        }

        /// <summary>Clip de la fuente <paramref name="voice"/> del lecho (tests).</summary>
        public AudioClip? MusicClip(int voice)
        {
            return _music[voice].clip;
        }

        /// <summary>Instante del reloj de audio (<c>dspTime</c>) en que se programó la fuente <paramref name="voice"/> del lecho (tests).</summary>
        public double MusicScheduledDsp(int voice)
        {
            return _musicScheduled[voice];
        }

        /// <summary>Volumen del tema de menú (tests).</summary>
        public float MenuVolume => _menu.volume;

        /// <summary>El tema de menú está sonando ahora (en pausa o parado, no; tests del foco de audio).</summary>
        public bool MenuPlaying => _menu.isPlaying;

        /// <summary>Clip del tema de menú (tests).</summary>
        public AudioClip? MenuClip => _menu.clip;

        /// <summary>Volumen del agua (tests).</summary>
        public float WaterVolume => _water.volume;

        /// <summary>Clip del agua (tests).</summary>
        public AudioClip? WaterClip => _water.clip;

        /// <summary>Volumen de la lluvia (tests).</summary>
        public float RainVolume => _rain.volume;

        /// <summary>Clip de la lluvia (tests).</summary>
        public AudioClip? RainClip => _rain.clip;

        /// <summary>Clip del último animal que sonó (tests).</summary>
        public AudioClip? FaunaClip => _fauna.clip;

        /// <inheritdoc />
        public void SetMusic(int world, int layers, bool night)
        {
            _mix.SetMusic(world, layers, night);
            ReportMusic();
        }

        /// <inheritdoc />
        public void SetMenuMusic()
        {
            _mix.SetMenuMusic();
            ReportMusic();
        }

        /// <inheritdoc />
        public void SetAmbient(int world, bool rain, int level)
        {
            _mix.SetAmbient(world, rain, level);
            _ambientLevel = level;
            if (Probe != null)
            {
                int w = _mix.AmbientWorld;
                Probe.Ambient(w, _mix.AmbientRain, level, WaterClipId(w), _mix.AmbientRain ? RainClipId(level) : string.Empty);
            }
        }

        /// <inheritdoc />
        public void PlayStinger(StingerId id)
        {
            if (!_mix.MusicAudible || StingerClip(id) == null)
            {
                return; // Música apagada, teléfono en silencio, un anuncio delante o sin clip grabado: no suena
            }

            if (_stingers.Request(id, _mix.ToadaLeftMs))
            {
                StartStinger(id);
            }
        }

        private AudioClip? StingerClip(StingerId id)
        {
            return _registry != null ? _registry.Get(AudioClipIds.StingerSlot(id)) : null;
        }

        private void StartStinger(StingerId id)
        {
            AudioClip? clip = StingerClip(id);
            if (clip == null)
            {
                return;
            }

            _stinger.Stop();
            _stinger.clip = clip;
            _stinger.volume = _mix.StingerGain;
            _stinger.Play();
            _stingersStarted++;
            _lastStinger = id;
            Probe?.Sfx(AudioClipIds.Id(AudioClipIds.StingerSlot(id)), StingerPeakDb + AudioMix.StingerGainDb, 0, true);
        }

        /// <summary>Pico de los remates de la entrega (dBFS; especificación, «music», remates).</summary>
        public const float StingerPeakDb = -16f;

        private void AwakeBackground()
        {
            for (int i = 0; i < MusicVoices; i++)
            {
                _music[i] = NewSource("Music" + i);
                _music[i].loop = true;
            }

            _menu = NewSource("Menu");
            _menu.loop = true;
            _water = NewSource("Water");
            _water.loop = true;
            _rain = NewSource("Rain");
            _rain.loop = true;
            _fauna = NewSource("Fauna");
            _stinger = NewSource("Stinger");
        }

        // Música apagada (o anuncio): todo el fondo a 0 en el acto; el fotograma siguiente lo mantiene con la ganancia de la mezcla.
        private void SilenceBackground()
        {
            for (int i = 0; i < MusicVoices; i++)
            {
                _music[i].volume = 0f;
            }

            _menu.volume = 0f;
            _water.volume = 0f;
            _rain.volume = 0f;
            _fauna.Stop();
            _stinger.Stop();
            _stingers.Clear();
        }

        private void PauseBackground(bool pause)
        {
            for (int i = 0; i < MusicVoices; i++)
            {
                Pause(_music[i], pause);
            }

            Pause(_menu, pause);
            Pause(_water, pause);
            Pause(_rain, pause);
            if (pause)
            {
                _fauna.Stop();
                _stinger.Stop();
                _stingers.Clear();
            }
        }

        private static void Pause(AudioSource source, bool pause)
        {
            if (pause)
            {
                source.Pause();
            }
            else
            {
                source.volume = 0f;
                source.UnPause();
            }
        }

        // Al cambiar de registro (UseClips): todo el fondo se calla y se suelta; el fotograma siguiente lo vuelve a pedir.
        private void ReleaseBackground()
        {
            if (_worldKey != 0)
            {
                ReleaseWorldBed();
            }

            CancelWorldLoad();
            _worldVolume = 0f;
            _water.Stop();
            _water.clip = null;
            if (_waterClip != null && !_waterFromClip)
            {
                Destroy(_waterClip);
            }

            _waterClip = null;
            _waterFromClip = false;
            _waterWorld = 0;
            _waterPlaying = false;
            _waterVolume = 0f;
            _rain.Stop();
            _rain.clip = null;
            _rainVolume = 0f;
            _menu.Stop();
            _menu.clip = null;
            if (_menuClip != null && !_menuFromClip)
            {
                Destroy(_menuClip);
            }

            _menuClip = null;
            _menuFromClip = false;
            _menuVolume = 0f;
            _fauna.Stop();
            _stinger.Stop();
            _stingers.Clear();
        }

        private void ReportMusic()
        {
            if (Probe != null && _mix.TakeMusicReport(out MusicReport report))
            {
                Probe.Music(report, MusicClipId(report));
            }
        }

        // El id del clip de la música pedida para la sonda: el grabado, «synth» con el respaldo por código, vacío sin música.
        private string MusicClipId(in MusicReport report)
        {
            if (report.Menu)
            {
                return _registry != null && _registry.Has(AudioClipIds.MenuSlot) ? AudioClipIds.Id(AudioClipIds.MenuSlot) : AudioProbeIds.Synth;
            }

            if (report.World <= 0)
            {
                return string.Empty;
            }

            return RecordedBed(report.World, report.Night, null) ? AudioClipIds.Id(AudioClipIds.BedSlot(report.World, report.Night)) : AudioProbeIds.Synth;
        }

        private string WaterClipId(int world)
        {
            if (world <= 0)
            {
                return string.Empty;
            }

            return RecordedWater(world) != null ? AudioClipIds.Id(AudioClipIds.WaterSlot(world)) : AudioProbeIds.Synth;
        }

        private string RainClipId(int level)
        {
            int slot = AudioClipIds.RainSlot(AmbientPlan.RainIntensityOf(level));
            return _registry != null && _registry.Has(slot) ? AudioClipIds.Id(slot) : AudioProbeIds.Synth;
        }

        // El lecho grabado (o su variante nocturna) y sus 3 capas del mundo; true si están todos. Con into, los deja ahí.
        private bool RecordedBed(int world, bool night, AudioClip?[]? into)
        {
            if (_registry == null || !AudioClipIds.HasWorld(world))
            {
                return false;
            }

            for (int l = 0; l < MusicVoices; l++)
            {
                AudioClip? clip = _registry.Get(l == 0 ? AudioClipIds.BedSlot(world, night) : AudioClipIds.PercussionSlot(world, l));
                if (clip == null)
                {
                    return false;
                }

                if (into != null)
                {
                    into[l] = clip;
                }
            }

            return true;
        }

        private AudioClip? RecordedWater(int world)
        {
            return _registry != null && AudioClipIds.HasWorld(world) ? _registry.Get(AudioClipIds.WaterSlot(world)) : null;
        }

        private void UpdateBackground(float dtMs)
        {
            ReportMusic(); // la atenuación de la toada termina sola
            UpdateWorldBed(dtMs);
            UpdateMenu(dtMs);
            UpdateAmbient(dtMs);
            UpdateFauna(dtMs);
            UpdateStinger();
        }

        // El lecho del mundo pedido. En el inicio (0) se funde a 0 y se para, sin soltar los clips: volver al mismo mundo y
        // variante lo hace sonar en el acto. Si se pide otro mundo o variante, se funde a 0, se suelta y se carga (grabado) o se
        // genera (respaldo) el nuevo.
        private void UpdateWorldBed(float dtMs)
        {
            int wanted = _mix.MusicWorld > 0 ? (_mix.MusicWorld * 2) + (_mix.MusicNight ? 1 : 0) : 0;
            float fade = dtMs / TrackFadeMs;
            if (wanted != 0 && _worldKey == wanted)
            {
                if (!_worldPlaying && !_adPaused)
                {
                    PlayWorldBed();
                }

                _worldVolume = Math.Min(1f, _worldVolume + fade);
            }
            else
            {
                _worldVolume = Math.Max(0f, _worldVolume - fade);
                if (_worldVolume <= 0f && _worldKey != 0)
                {
                    if (wanted == 0)
                    {
                        StopWorldBed();
                    }
                    else
                    {
                        ReleaseWorldBed();
                    }
                }

                if (_worldKey == 0 && wanted != 0 && _worldTask == null && _worldLoadKey == 0 && _mix.Settings.Music)
                {
                    int world = _mix.MusicWorld;
                    bool night = _mix.MusicNight;
                    if (RecordedBed(world, night, _worldLoad))
                    {
                        // Grabado: se cargan los 4 clips en memoria y solo se arrancan cuando están todos (misma muestra).
                        _worldLoadKey = wanted;
                        _worldLoads++;
                        for (int l = 0; l < MusicVoices; l++)
                        {
                            _worldLoad[l]!.LoadAudioData();
                        }
                    }
                    else
                    {
                        _worldTaskKey = wanted;
                        _worldRenders++;
                        _worldTask = Task.Run(() => MusicSynth.Render(world, night));
                    }
                }
            }

            UpdateWorldLoad(wanted);
            if (_worldTask != null && _worldTask.IsCompleted)
            {
                Task<MusicLoops> task = _worldTask;
                _worldTask = null;
                if (task.IsFaulted)
                {
                    Debug.LogError("AF.Audio: no se pudo generar el lecho: " + task.Exception?.GetBaseException().Message);
                }
                else if (_worldKey == 0 && (_worldTaskKey == wanted || wanted == 0))
                {
                    // Si entretanto se fue al inicio, se guarda parado para la vuelta.
                    StartWorldBed(_worldTaskKey, task.Result, _worldTaskKey == wanted && !_adPaused);
                }
            }

            float gain = _adPaused ? 0f : _mix.MusicVolume;
            float step = dtMs / LayerFadeMs;
            for (int l = 0; l < MusicVoices; l++)
            {
                float target = l == 0 || l <= _mix.MusicLayers ? 1f : 0f;
                _layerVolume[l] = target > _layerVolume[l] ? Math.Min(target, _layerVolume[l] + step) : Math.Max(target, _layerVolume[l] - step);
                _music[l].volume = _worldKey == 0 ? 0f : _layerVolume[l] * _worldVolume * gain;
            }
        }

        // Los clips grabados del lecho que se cargan: cuando están los 4, se ponen (sonando si es lo pedido; parados si entretanto
        // se fue al inicio). Si se pidió otro mundo, se descargan; si alguno no carga, se avisa y el mundo va con el respaldo.
        private void UpdateWorldLoad(int wanted)
        {
            if (_worldLoadKey == 0)
            {
                return;
            }

            if (wanted != 0 && wanted != _worldLoadKey)
            {
                CancelWorldLoad();
                return;
            }

            bool failed = false;
            for (int l = 0; l < MusicVoices; l++)
            {
                AudioDataLoadState state = _worldLoad[l]!.loadState;
                if (state == AudioDataLoadState.Failed)
                {
                    failed = true;
                }
                else if (state != AudioDataLoadState.Loaded)
                {
                    return; // aún cargando
                }
            }

            int key = _worldLoadKey;
            if (failed)
            {
                Debug.LogError("AF.Audio: no se pudo cargar el lecho grabado del mundo " + (key / 2) + ": suena el de respaldo.");
                CancelWorldLoad();
                _worldTaskKey = key;
                _worldRenders++;
                int world = key / 2;
                bool night = (key & 1) == 1;
                _worldTask = Task.Run(() => MusicSynth.Render(world, night));
                return;
            }

            _worldLoadKey = 0;
            if (_worldKey == 0)
            {
                StartWorldBedClips(key, key == wanted && !_adPaused);
            }

            for (int l = 0; l < MusicVoices; l++)
            {
                _worldLoad[l] = null;
            }
        }

        private void CancelWorldLoad()
        {
            if (_worldLoadKey == 0)
            {
                return;
            }

            for (int l = 0; l < MusicVoices; l++)
            {
                AudioClip? clip = _worldLoad[l];
                if (clip != null && !IsMusicClipInUse(clip))
                {
                    clip.UnloadAudioData();
                }

                _worldLoad[l] = null;
            }

            _worldLoadKey = 0;
        }

        private bool IsMusicClipInUse(AudioClip clip)
        {
            for (int l = 0; l < MusicVoices; l++)
            {
                if (_musicClips[l] == clip)
                {
                    return true;
                }
            }

            return false;
        }

        private void StartWorldBedClips(int key, bool play)
        {
            for (int l = 0; l < MusicVoices; l++)
            {
                AudioClip clip = _worldLoad[l]!;
                _musicClips[l] = clip;
                AudioSource source = _music[l];
                source.clip = clip;
                source.volume = 0f;
            }

            _worldKey = key;
            _worldFromClips = true;
            _worldVolume = 0f;
            if (play)
            {
                PlayWorldBed();
            }
        }

        private void StartWorldBed(int key, MusicLoops loops, bool play)
        {
            for (int l = 0; l < MusicVoices; l++)
            {
                float[] pcm = l == 0 ? loops.Bed : loops.Percussion[l - 1];
                int rate = l == 0 ? loops.BedSampleRate : loops.PercussionSampleRate;
                AudioClip clip = AudioClip.Create("music_" + key + "_" + l, pcm.Length, 1, rate, false);
                clip.SetData(pcm, 0);
                _musicClips[l] = clip;
                AudioSource source = _music[l];
                source.clip = clip;
                source.volume = 0f;
            }

            _worldKey = key;
            _worldFromClips = false;
            _worldVolume = 0f;
            if (play)
            {
                PlayWorldBed();
            }
        }

        // Arranca el lecho y sus capas desde el principio, a la vez: el mismo instante del reloj de audio para las 4 fuentes.
        private void PlayWorldBed()
        {
            double at = AudioSettings.dspTime + ScheduleLeadSeconds;
            for (int l = 0; l < MusicVoices; l++)
            {
                _music[l].volume = 0f;
                _music[l].PlayScheduled(at);
                _musicScheduled[l] = at;
                _layerVolume[l] = l == 0 ? 1f : 0f;
            }

            _worldPlaying = true;
        }

        // En el inicio: el lecho ya callado se para y sus clips se quedan.
        private void StopWorldBed()
        {
            if (!_worldPlaying)
            {
                return;
            }

            for (int l = 0; l < MusicVoices; l++)
            {
                _music[l].Stop();
            }

            _worldPlaying = false;
        }

        // Se suelta el lecho: los grabados se descargan de memoria (son del proyecto: nunca se destruyen); los generados, se destruyen.
        private void ReleaseWorldBed()
        {
            for (int l = 0; l < MusicVoices; l++)
            {
                _music[l].Stop();
                _music[l].clip = null;
            }

            for (int l = 0; l < MusicVoices; l++)
            {
                AudioClip? clip = _musicClips[l];
                _musicClips[l] = null;
                if (clip == null)
                {
                    continue;
                }

                if (_worldFromClips)
                {
                    clip.UnloadAudioData();
                }
                else
                {
                    Destroy(clip);
                }
            }

            _worldKey = 0;
            _worldFromClips = false;
            _worldPlaying = false;
        }

        // El tema de menú: el grabado (en streaming) o, de respaldo, el generado una vez; entra y sale con el mismo fundido.
        private void UpdateMenu(float dtMs)
        {
            bool wanted = _mix.MusicMenu;
            if (wanted && _menuClip == null && _menuTask == null && _mix.Settings.Music)
            {
                AudioClip? recorded = _registry != null ? _registry.Get(AudioClipIds.MenuSlot) : null;
                if (recorded != null)
                {
                    _menuClip = recorded;
                    _menuFromClip = true;
                    _menu.clip = recorded;
                    _menuVolume = 0f;
                }
                else
                {
                    _menuTask = Task.Run(() => MenuThemeSynth.Render());
                }
            }

            if (_menuTask != null && _menuTask.IsCompleted)
            {
                Task<MusicLoops> task = _menuTask;
                _menuTask = null;
                if (task.IsFaulted)
                {
                    Debug.LogError("AF.Audio: no se pudo generar el tema de menú: " + task.Exception?.GetBaseException().Message);
                }
                else if (_menuClip == null)
                {
                    float[] pcm = task.Result.Bed;
                    _menuClip = AudioClip.Create("music_menu", pcm.Length, 1, task.Result.BedSampleRate, false);
                    _menuClip.SetData(pcm, 0);
                    _menuFromClip = false;
                    _menu.clip = _menuClip;
                    _menuVolume = 0f;
                }
            }

            float fade = dtMs / TrackFadeMs;
            bool playable = _menuClip != null && wanted && _worldVolume <= 0f; // entra cuando el lecho ya salió
            _menuVolume = playable ? Math.Min(1f, _menuVolume + fade) : Math.Max(0f, _menuVolume - fade);
            if (_menuClip != null)
            {
                if (_menuVolume > 0f && !_menu.isPlaying && !_adPaused)
                {
                    _menu.Play(); // empieza el tema desde el principio cada vez que se vuelve al inicio
                }
                else if (_menuVolume <= 0f && _menu.isPlaying)
                {
                    _menu.Stop();
                }
            }

            _menu.volume = _menuVolume * (_adPaused ? 0f : _mix.MusicVolume);
        }

        // El agua del mundo y la lluvia del nivel. Como el lecho: en el inicio (0) el agua se funde y se para sin soltarse, y
        // solo se suelta al pedir otro mundo.
        private void UpdateAmbient(float dtMs)
        {
            int wanted = _mix.AmbientWorld;
            float fade = dtMs / AmbientFadeMs;
            if (wanted != 0 && _waterWorld == wanted)
            {
                if (!_waterPlaying && !_adPaused)
                {
                    _water.volume = 0f;
                    _water.Play();
                    _waterPlaying = true;
                }

                _waterVolume = Math.Min(1f, _waterVolume + fade);
            }
            else
            {
                _waterVolume = Math.Max(0f, _waterVolume - fade);
                if (_waterVolume <= 0f && _waterWorld != 0)
                {
                    if (_waterPlaying)
                    {
                        _water.Stop();
                        _waterPlaying = false;
                    }

                    if (wanted != 0)
                    {
                        _water.clip = null;
                        if (_waterClip != null && !_waterFromClip)
                        {
                            Destroy(_waterClip);
                        }

                        _waterClip = null;
                        _waterFromClip = false;
                        _waterWorld = 0;
                    }
                }

                if (_waterWorld == 0 && wanted != 0 && _waterTask == null && _mix.Settings.Music)
                {
                    AudioClip? recorded = RecordedWater(wanted);
                    if (recorded != null)
                    {
                        // Grabada: en streaming, suena en el acto.
                        _waterClip = recorded;
                        _waterFromClip = true;
                        _water.clip = recorded;
                        _water.volume = 0f;
                        _waterWorld = wanted;
                        _waterVolume = 0f;
                        if (!_adPaused)
                        {
                            _water.Play();
                            _waterPlaying = true;
                        }
                    }
                    else
                    {
                        _waterTaskWorld = wanted;
                        _waterRenders++;
                        _waterTask = Task.Run(() => AmbientSynth.RenderWater(wanted));
                    }
                }
            }

            if (_waterTask != null && _waterTask.IsCompleted)
            {
                Task<float[]> task = _waterTask;
                _waterTask = null;
                if (task.IsFaulted)
                {
                    Debug.LogError("AF.Audio: no se pudo generar el agua: " + task.Exception?.GetBaseException().Message);
                }
                else if (_waterWorld == 0 && (_waterTaskWorld == wanted || wanted == 0))
                {
                    // Si entretanto se fue al inicio, se guarda parada para la vuelta.
                    _waterClip = AudioClip.Create("ambient_water_" + _waterTaskWorld, task.Result.Length, 1, AmbientSynth.SampleRate, false);
                    _waterClip.SetData(task.Result, 0);
                    _waterFromClip = false;
                    _water.clip = _waterClip;
                    _water.volume = 0f;
                    _waterWorld = _waterTaskWorld;
                    _waterVolume = 0f;
                    if (_waterTaskWorld == wanted && !_adPaused)
                    {
                        _water.Play();
                        _waterPlaying = true;
                    }
                }
            }

            UpdateRain(wanted, fade);
            float gain = _adPaused ? 0f : _mix.AmbientVolume;
            _water.volume = _waterWorld == 0 ? 0f : _waterVolume * gain;
            _rain.volume = _rainVolume * gain;
        }

        // La lluvia del nivel: la grabada de su intensidad (AmbientPlan.RainIntensityOf) o, de respaldo, la generada una vez. Si el
        // nivel nuevo pide otra intensidad, la que suena se funde a 0 y entra la nueva.
        private void UpdateRain(int wanted, float fade)
        {
            bool rainWanted = _mix.AmbientRain && _waterWorld != 0 && _waterWorld == wanted;
            AudioClip? target = null;
            if (rainWanted)
            {
                int slot = AudioClipIds.RainSlot(AmbientPlan.RainIntensityOf(_ambientLevel));
                target = _registry != null ? _registry.Get(slot) : null;
                if (target == null)
                {
                    if (_rainClip == null && _rainTask == null && _mix.Settings.Music)
                    {
                        _rainTask = Task.Run(() => AmbientSynth.RenderRain());
                    }

                    target = _rainClip;
                }
            }

            if (_rainTask != null && _rainTask.IsCompleted)
            {
                Task<float[]> task = _rainTask;
                _rainTask = null;
                if (task.IsFaulted)
                {
                    Debug.LogError("AF.Audio: no se pudo generar la lluvia: " + task.Exception?.GetBaseException().Message);
                }
                else if (_rainClip == null)
                {
                    _rainClip = AudioClip.Create("ambient_rain", task.Result.Length, 1, AmbientSynth.SampleRate, false);
                    _rainClip.SetData(task.Result, 0);
                }
            }

            bool match = target != null && _rain.clip == target;
            _rainVolume = rainWanted && match ? Math.Min(1f, _rainVolume + fade) : Math.Max(0f, _rainVolume - fade);
            if (_rainVolume <= 0f && target != null && !match)
            {
                _rain.Stop();
                _rain.clip = target; // el fotograma siguiente entra con su fundido
            }

            if (_rain.clip != null)
            {
                if (_rainVolume > 0f && !_rain.isPlaying && !_adPaused)
                {
                    _rain.Play();
                }
                else if (_rainVolume <= 0f && _rain.isPlaying)
                {
                    _rain.Stop();
                }
            }
        }

        // Un animal cada 20-45 s, por debajo del juego, si el ambiente se oye: uno grabado del mundo o, de respaldo, el efecto
        // que encaja (AmbientPlan.FaunaOf; ninguno en los mundos 10-13).
        private void UpdateFauna(float dtMs)
        {
            int world = _mix.AmbientWorld;
            int recorded = _faunaRecorded ? AudioClipIds.FaunaCount(world) : 0;
            AudioClip? clip = null;
            bool fired;
            if (recorded > 0)
            {
                fired = _mix.AdvanceFauna(dtMs, recorded, out int pick);
                if (fired)
                {
                    clip = _registry!.Get(AudioClipIds.FaunaSlot(world, pick));
                }
            }
            else
            {
                fired = _mix.AdvanceFauna(dtMs, out SfxId id);
                if (fired)
                {
                    clip = EffectClip(AudioBank.Index(id, 0), out _);
                }
            }

            if (fired && clip != null && _mix.AmbientAudible && _waterWorld != 0)
            {
                _fauna.Stop();
                _fauna.clip = clip;
                _fauna.volume = _mix.FaunaVolume;
                _fauna.Play();
            }
            else if (_fauna.isPlaying)
            {
                _fauna.volume = _mix.FaunaVolume; // sigue la atenuación y el ajuste Música mientras suena
            }
        }

        // El remate que esperaba a que terminara la toada suena en cuanto termina; el que suena sigue el ajuste Música.
        private void UpdateStinger()
        {
            if (_stingers.Advance(_mix.ToadaLeftMs, out StingerId id) && _mix.MusicAudible && !_adPaused)
            {
                StartStinger(id);
            }

            if (_stinger.isPlaying)
            {
                _stinger.volume = _mix.StingerGain;
            }
        }
    }
}
