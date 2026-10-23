#nullable enable
using AF.Audio;
using AF.Audio.Unity;
using AF.GameFlow;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Audio del arranque y de Ajustes (tareas AU1 y AU3; D-026): parcial de <see cref="GameBootstrap"/> para que sus ganchos sean
    /// líneas sueltas. Instala el servicio de audio (el de Unity, con los clips grabados de la escena y el audio por código de
    /// respaldo, o el de los tests,
    /// <see cref="AudioCues.Override"/>) con los ajustes del perfil; guarda y aplica en el acto Sonido, Música y Vibración; y pone
    /// el sonido de Atrás (<c>sfx_ui_back</c>) y el háptico corto de Atrás bloqueado (la lista, §7: «Atrás no puede cerrar un aviso
    /// obligatorio»).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        // Los clips grabados de la entrega 1 (tarea AU3): los cuelga de la escena GameSceneBuilder, sin Resources.
        [SerializeField]
        private AudioClipRegistry? audioClips;

        /// <summary>Registro de clips grabados asignado (<c>AudioClipRegistry.asset</c>; escena generada por <c>GameSceneBuilder</c>).</summary>
        public AudioClipRegistry? AudioClipsAsset => audioClips;

        /// <summary>
        /// Asigna el registro de clips grabados (tarea AU3; escena generada por <c>GameSceneBuilder</c>). Sin él, el juego suena
        /// con el audio por código de AU1 y AU2.
        /// </summary>
        public void ConfigureAudio(AudioClipRegistry? registry)
        {
            audioClips = registry;
        }

        // El servicio de audio de la sesión, con Sonido, Música y Vibración del perfil (08 §4.3; defaults de 08 §9) y, desde AU3,
        // con los clips grabados de la escena (el audio por código queda de respaldo).
        private void InstallAudio(SettingsData settings)
        {
            IAudioService service;
            if (AudioCues.Override != null)
            {
                service = AudioCues.Override;
            }
            else
            {
                UnityAudioService unity = UnityAudioService.Ensure();
                unity.UseClips(audioClips);
                unity.Probe = _probe;
                service = unity;
            }

            AudioCues.Install(service);
            AudioCues.Configure(settings.Sound, settings.Music, settings.Haptics);
        }

        /// <summary>
        /// Ajustes › Sonido, Música o Vibración (07 §9.4; tarea AU1): guarda <paramref name="on"/> en <c>settings.sound</c>,
        /// <c>music</c> o <c>haptics</c> según <paramref name="key"/> (<see cref="SettingsScreen.SoundId"/>…) y lo aplica al momento
        /// (lo que suena se calla). La sonda escribe <c>settings_change</c> con esa clave (08 §8.2, <c>af_settings_change</c>).
        /// </summary>
        public bool ChooseAudio(string key, bool on)
        {
            Profile? profile = _profile;
            if (profile == null)
            {
                return false;
            }

            SettingsData s = profile.Settings;
            bool changed;
            switch (key)
            {
                case SettingsScreen.SoundId:
                    changed = s.Sound != on;
                    s.Sound = on;
                    break;
                case SettingsScreen.MusicId:
                    changed = s.Music != on;
                    s.Music = on;
                    break;
                case SettingsScreen.HapticsId:
                    changed = s.Haptics != on;
                    s.Haptics = on;
                    break;
                default:
                    return false;
            }

            if (changed)
            {
                SaveSettings(profile);
            }

            AudioCues.Configure(s.Sound, s.Music, s.Haptics);
            _settingsScreen?.MarkAudio(s.Sound, s.Music, s.Haptics);
            _probe?.SettingsChange(key, on ? "true" : "false", null);
            return true;
        }

        // Cambio de pantalla (tarea AU2): fuera del nivel (inicio, «El río sigue creciendo», Sin canoas) suena el tema de menú y no
        // hay ambiente de río; al entrar en el nivel, el lecho y el ambiente los pide la carga del nivel (AudioBoard.LevelLoaded).
        private static void AudioScreenChanged(AppScreen screen)
        {
            if (screen != AppScreen.Level && screen != AppScreen.Boot)
            {
                AudioCues.Home();
            }
        }

        // Volver desde una hoja del inicio: sfx_ui_back si de verdad se volvió.
        private static bool AudioBack(bool done)
        {
            if (done)
            {
                AudioCues.Play(SfxId.UiBack);
            }

            return done;
        }

        // Atrás de Android: si cierra algo, sfx_ui_back (salvo que lo que cerró ya sonara); si no puede cerrar un aviso obligatorio
        // (el embudo del nivel, la compra simulada o el aviso del perfil bloqueado), el háptico corto. Con un anuncio delante
        // Atrás tampoco cierra, pero no vibra: todo se calla durante el anuncio (07 §9.4; AudioMix.HapticsAllowed). Nunca suena
        // ni vibra si Atrás simplemente no hace nada (el nivel en juego, el inicio sin hoja).
        private void AndroidBackWithAudio()
        {
            int cue = AudioCues.Serial;
            if (PressAndroidBack())
            {
                AudioCues.BackIfQuiet(cue);
            }
            else if (BackBlocked())
            {
                AudioCues.Haptic(HapticKind.BackBlocked);
            }
        }

        /// <summary>Atrás no puede cerrar lo que está delante: un modal del embudo del nivel o una hoja que exige una respuesta.</summary>
        internal bool BackBlocked()
        {
            if ((_storeSheet != null && _storeSheet.Visible) || (_adOverlay != null && _adOverlay.Visible) || (_lockedNotice != null && _lockedNotice.Visible))
            {
                return true;
            }

            if (DeleteAccountBusy)
            {
                return true; // «Eliminando…» o la confirmación del borrado: Atrás no lo cierra a medias
            }

            if (_app == null || _app.Screen != AppScreen.Level || _flow == null)
            {
                return false;
            }

            FlowPhase phase = _flow.Phase;
            return phase == FlowPhase.StuckModal || phase == FlowPhase.GiveUpConfirm || phase == FlowPhase.LevelFailed || phase == FlowPhase.NoCanoes;
        }
    }
}
