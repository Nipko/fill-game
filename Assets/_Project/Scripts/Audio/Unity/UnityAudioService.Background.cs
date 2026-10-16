#nullable enable
using System;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AF.Audio.Unity
{
    /// <summary>
    /// Música y ambiente del servicio de audio (tarea AU2; 07 §9.1, §9.3 y §9.4):
    /// <list type="bullet">
    /// <item><description>el lecho del mundo y sus <see cref="MusicSynth.PercussionLayers"/> capas de percusión, en
    /// <see cref="MusicVoices"/> fuentes que arrancan en el mismo instante (<c>PlayScheduled</c>); las capas entran y salen con
    /// un fundido de <see cref="LayerFadeMs"/> según las cuias selladas, y la variante nocturna es otro par de bucles;</description></item>
    /// <item><description>el tema de menú del inicio, en su propia fuente: al pasar del inicio al nivel (o al revés, o a otro
    /// mundo) lo que suena se funde en <see cref="TrackFadeMs"/> y lo nuevo entra igual;</description></item>
    /// <item><description>el ambiente: el agua del mundo, la lluvia si el nivel la tiene y un animal cada 20-45 s
    /// (<see cref="AudioMix.AdvanceFauna"/>) en su propia fuente.</description></item>
    /// </list>
    /// Cada bucle se genera en un hilo de trabajo la primera vez que se pide (y se guarda el último de cada clase: el tema de
    /// menú, el lecho de un mundo y una variante, el agua de un mundo y la lluvia); el hilo principal solo crea sus clips. Todo
    /// va con la ganancia de <see cref="AudioMix.MusicGain"/> (ajuste Música, modo silencio, anuncio y atenuación de 6 dB).
    /// Sin asignaciones por fotograma: solo al cambiar de pieza.
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

        private readonly AudioSource[] _music = new AudioSource[MusicVoices];
        private readonly AudioClip?[] _musicClips = new AudioClip?[MusicVoices];
        private readonly float[] _layerVolume = new float[MusicVoices];
        private AudioSource _menu = null!;
        private AudioSource _water = null!;
        private AudioSource _rain = null!;
        private AudioSource _fauna = null!;
        private AudioClip? _menuClip;
        private AudioClip? _waterClip;
        private AudioClip? _rainClip;
        private Task<MusicLoops>? _worldTask;
        private int _worldTaskKey;
        private int _worldKey; // mundo × 2 (+1 de noche) de los clips del lecho; 0 = ninguno
        private Task<MusicLoops>? _menuTask;
        private Task<float[]>? _waterTask;
        private int _waterTaskWorld;
        private int _waterWorld;
        private Task<float[]>? _rainTask;
        private float _worldVolume;
        private float _menuVolume;
        private float _waterVolume;
        private float _rainVolume;

        /// <summary>Mundo y variante de los clips del lecho que hay (mundo × 2, +1 la nocturna; 0 = ninguno; tests).</summary>
        public int WorldBedKey => _worldKey;

        /// <summary>Mundo del agua que hay (0 = ninguno; tests).</summary>
        public int WaterWorld => _waterWorld;

        /// <summary>Ya está el tema de menú (tests).</summary>
        public bool MenuReady => _menuClip != null;

        /// <summary>Ya está la lluvia (tests).</summary>
        public bool RainReady => _rainClip != null;

        /// <summary>Volumen de la fuente <paramref name="voice"/> del lecho (0 = lecho, 1-3 = capas; tests).</summary>
        public float MusicVolume(int voice)
        {
            return _music[voice].volume;
        }

        /// <summary>Volumen del tema de menú (tests).</summary>
        public float MenuVolume => _menu.volume;

        /// <summary>Volumen del agua (tests).</summary>
        public float WaterVolume => _water.volume;

        /// <summary>Volumen de la lluvia (tests).</summary>
        public float RainVolume => _rain.volume;

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
            Probe?.Ambient(_mix.AmbientWorld, _mix.AmbientRain, level);
        }

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

        private void ReportMusic()
        {
            if (Probe != null && _mix.TakeMusicReport(out MusicReport report))
            {
                Probe.Music(report);
            }
        }

        private void UpdateBackground(float dtMs)
        {
            ReportMusic(); // la atenuación de la toada termina sola
            UpdateWorldBed(dtMs);
            UpdateMenu(dtMs);
            UpdateAmbient(dtMs);
            UpdateFauna(dtMs);
        }

        // El lecho del mundo pedido: si los clips son de otro mundo o variante, se funden a 0, se sueltan y se genera el nuevo.
        private void UpdateWorldBed(float dtMs)
        {
            int wanted = _mix.MusicWorld > 0 ? (_mix.MusicWorld * 2) + (_mix.MusicNight ? 1 : 0) : 0;
            float fade = dtMs / TrackFadeMs;
            if (_worldKey != wanted)
            {
                _worldVolume = Math.Max(0f, _worldVolume - fade);
                if (_worldVolume <= 0f && _worldKey != 0)
                {
                    ReleaseWorldBed();
                }

                if (_worldKey == 0 && wanted != 0 && _worldTask == null)
                {
                    int world = _mix.MusicWorld;
                    bool night = _mix.MusicNight;
                    _worldTaskKey = wanted;
                    _worldTask = Task.Run(() => MusicSynth.Render(world, night));
                }
            }
            else if (wanted != 0)
            {
                _worldVolume = Math.Min(1f, _worldVolume + fade);
            }

            if (_worldTask != null && _worldTask.IsCompleted)
            {
                Task<MusicLoops> task = _worldTask;
                _worldTask = null;
                if (task.IsFaulted)
                {
                    Debug.LogError("AF.Audio: no se pudo generar el lecho: " + task.Exception?.GetBaseException().Message);
                }
                else if (_worldKey == 0 && _worldTaskKey == wanted)
                {
                    StartWorldBed(_worldTaskKey, task.Result);
                }
            }

            float gain = _adPaused ? 0f : _mix.MusicGain;
            float step = dtMs / LayerFadeMs;
            for (int l = 0; l < MusicVoices; l++)
            {
                float target = l == 0 || l <= _mix.MusicLayers ? 1f : 0f;
                _layerVolume[l] = target > _layerVolume[l] ? Math.Min(target, _layerVolume[l] + step) : Math.Max(target, _layerVolume[l] - step);
                _music[l].volume = _worldKey == 0 ? 0f : _layerVolume[l] * _worldVolume * gain;
            }
        }

        private void StartWorldBed(int key, MusicLoops loops)
        {
            double at = AudioSettings.dspTime + 0.1;
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
                source.PlayScheduled(at); // el lecho y sus capas, a la vez y en el mismo punto del bucle
                _layerVolume[l] = l == 0 ? 1f : 0f;
            }

            _worldKey = key;
            _worldVolume = 0f;
        }

        private void ReleaseWorldBed()
        {
            for (int l = 0; l < MusicVoices; l++)
            {
                _music[l].Stop();
                _music[l].clip = null;
                if (_musicClips[l] != null)
                {
                    Destroy(_musicClips[l]);
                    _musicClips[l] = null;
                }
            }

            _worldKey = 0;
        }

        // El tema de menú: se genera una vez y se queda; entra y sale con el mismo fundido.
        private void UpdateMenu(float dtMs)
        {
            bool wanted = _mix.MusicMenu;
            if (wanted && _menuClip == null && _menuTask == null)
            {
                _menuTask = Task.Run(() => MenuThemeSynth.Render());
            }

            if (_menuTask != null && _menuTask.IsCompleted)
            {
                Task<MusicLoops> task = _menuTask;
                _menuTask = null;
                if (task.IsFaulted)
                {
                    Debug.LogError("AF.Audio: no se pudo generar el tema de menú: " + task.Exception?.GetBaseException().Message);
                }
                else
                {
                    float[] pcm = task.Result.Bed;
                    _menuClip = AudioClip.Create("music_menu", pcm.Length, 1, task.Result.BedSampleRate, false);
                    _menuClip.SetData(pcm, 0);
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

            _menu.volume = _menuVolume * (_adPaused ? 0f : _mix.MusicGain);
        }

        // El agua del mundo y la lluvia del nivel.
        private void UpdateAmbient(float dtMs)
        {
            int wanted = _mix.AmbientWorld;
            float fade = dtMs / AmbientFadeMs;
            if (_waterWorld != wanted)
            {
                _waterVolume = Math.Max(0f, _waterVolume - fade);
                if (_waterVolume <= 0f && _waterWorld != 0)
                {
                    _water.Stop();
                    _water.clip = null;
                    if (_waterClip != null)
                    {
                        Destroy(_waterClip);
                        _waterClip = null;
                    }

                    _waterWorld = 0;
                }

                if (_waterWorld == 0 && wanted != 0 && _waterTask == null)
                {
                    _waterTaskWorld = wanted;
                    _waterTask = Task.Run(() => AmbientSynth.RenderWater(wanted));
                }
            }
            else if (wanted != 0)
            {
                _waterVolume = Math.Min(1f, _waterVolume + fade);
            }

            if (_waterTask != null && _waterTask.IsCompleted)
            {
                Task<float[]> task = _waterTask;
                _waterTask = null;
                if (task.IsFaulted)
                {
                    Debug.LogError("AF.Audio: no se pudo generar el agua: " + task.Exception?.GetBaseException().Message);
                }
                else if (_waterWorld == 0 && _waterTaskWorld == wanted)
                {
                    _waterClip = AudioClip.Create("ambient_water_" + wanted, task.Result.Length, 1, AmbientSynth.SampleRate, false);
                    _waterClip.SetData(task.Result, 0);
                    _water.clip = _waterClip;
                    _water.volume = 0f;
                    _water.Play();
                    _waterWorld = wanted;
                    _waterVolume = 0f;
                }
            }

            bool rainWanted = _mix.AmbientRain && _waterWorld != 0 && _waterWorld == wanted;
            if (rainWanted && _rainClip == null && _rainTask == null)
            {
                _rainTask = Task.Run(() => AmbientSynth.RenderRain());
            }

            if (_rainTask != null && _rainTask.IsCompleted)
            {
                Task<float[]> task = _rainTask;
                _rainTask = null;
                if (!task.IsFaulted)
                {
                    _rainClip = AudioClip.Create("ambient_rain", task.Result.Length, 1, AmbientSynth.SampleRate, false);
                    _rainClip.SetData(task.Result, 0);
                    _rain.clip = _rainClip;
                    _rain.volume = 0f;
                }
            }

            _rainVolume = rainWanted && _rainClip != null ? Math.Min(1f, _rainVolume + fade) : Math.Max(0f, _rainVolume - fade);
            if (_rainClip != null)
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

            float gain = _adPaused ? 0f : _mix.AmbientGain;
            _water.volume = _waterWorld == 0 ? 0f : _waterVolume * gain;
            _rain.volume = _rainVolume * gain;
        }

        // Un animal cada 20-45 s, por debajo del juego, si el ambiente se oye.
        private void UpdateFauna(float dtMs)
        {
            if (_mix.AdvanceFauna(dtMs, out SfxId id) && _mix.AmbientAudible && Ready && _waterWorld != 0)
            {
                AudioClip? clip = _clips[AudioBank.Index(id, 0)];
                if (clip != null)
                {
                    _fauna.Stop();
                    _fauna.clip = clip;
                    _fauna.volume = _mix.FaunaGain;
                    _fauna.Play();
                }
            }
            else if (_fauna.isPlaying)
            {
                _fauna.volume = _mix.FaunaGain; // sigue la atenuación y el ajuste Música mientras suena
            }
        }
    }
}
