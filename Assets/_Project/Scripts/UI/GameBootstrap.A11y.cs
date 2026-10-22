#nullable enable
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Accesibilidad en el arranque (bloque B9; parche <c>B9-ui.diff</c>; 02 §12; 07 §3, regla 5, y §7.8; 08 §4.3 y §9):
    /// <list type="bullet">
    /// <item><b>Reducir movimiento</b>: <c>settings.reduce_motion</c> [P] (<c>auto</c>, <c>on</c> u <c>off</c>); en <c>auto</c>,
    /// <c>ui_reduce_motion_default</c> o [P] el teléfono con «Quitar animaciones» (<see cref="A11yRules.ReduceMotion"/>; Q-B9-3).
    /// Llega a <c>LevelFlow.ReduceMotion</c> (de campaña y del Reto, este con <c>DailyChallengeScreen.Prepare</c> antes de construir
    /// su tablero), al Mapa del río y al factor de los vertidos y de los pasos de mecánica (<c>anim_reduce_motion_factor</c>).</item>
    /// <item><b>Alto contraste</b>: <c>settings.high_contrast</c> [P] → el patrón al 35 % en el tablero.</item>
    /// <item><b>Lector de pantalla</b>: <see cref="A11yService"/> con el módulo de accesibilidad de Unity
    /// (<see cref="UnityScreenReaderHost"/>; los tests ponen el falso con <see cref="A11yHostOverride"/>), con la pantalla del nivel
    /// (y la del Reto) como fuente propia, avanzado en cada fotograma; con TalkBack apagado no cuesta nada.</item>
    /// <item>La escala de fuente del sistema la lee <see cref="LaunchOptions.ResolveTextScale"/> al construir los textos.</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private A11yService? _a11y;
        private bool _reduceMotionNow;
        private float _animatorScale = float.NaN;
        private bool _animatorScaleRead;

        /// <summary>Anfitrión del lector de pantalla de un test (null: el de Unity). Se lee en el arranque.</summary>
        internal static IScreenReaderHost? A11yHostOverride { get; set; }

        /// <summary>El lector de pantalla del juego (tests).</summary>
        internal A11yService? A11y => _a11y;

        /// <summary>«Reducir movimiento» en vigor (lo resuelto: la elección del jugador, Remote Config o el teléfono).</summary>
        public bool ReduceMotionNow => _reduceMotionNow;

        /// <summary>
        /// Ajustes › Reducir movimiento o Alto contraste (bloque B9; <paramref name="id"/> = <see cref="SettingsScreen.ReduceMotionId"/>
        /// o <see cref="SettingsScreen.HighContrastId"/>): guarda <paramref name="on"/> en <c>settings.reduce_motion</c> (<c>on</c> u
        /// <c>off</c>: desde ahí manda la elección) o <c>settings.high_contrast</c> en el acto. Alto contraste se ve al momento;
        /// Reducir movimiento llega al flujo al momento y a los tiempos del tablero con el siguiente que se construya (Ajustes solo
        /// se abre desde el inicio: el siguiente tablero es el próximo nivel). La sonda escribe <c>settings_change</c> con esa clave.
        /// </summary>
        public bool ChooseA11y(string id, bool on)
        {
            Profile? profile = _profile;
            if (profile == null)
            {
                return false;
            }

            if (id == SettingsScreen.ReduceMotionId)
            {
                ReduceMotionSetting value = on ? ReduceMotionSetting.On : ReduceMotionSetting.Off;
                if (profile.Settings.ReduceMotion != value)
                {
                    profile.Settings.ReduceMotion = value;
                    SaveSettings(profile);
                }
            }
            else if (id == SettingsScreen.HighContrastId)
            {
                if (profile.Settings.HighContrast != on)
                {
                    profile.Settings.HighContrast = on;
                    SaveSettings(profile);
                }
            }
            else
            {
                return false;
            }

            ApplyA11y();
            _probe?.SettingsChange(id, on ? "true" : "false", null);
            return true;
        }

        // En el arranque, con el flujo, las pantallas y Remote Config listos.
        private void SetUpA11y()
        {
            _a11y = new A11yService(A11yHostOverride ?? new UnityScreenReaderHost());
            if (_screen != null)
            {
                _screen.A11y = _a11y;
            }

            if (_settingsScreen != null)
            {
                _settingsScreen.A11yToggled = (id, on) => ChooseA11y(id, on);
            }

            if (_lobby != null)
            {
                A11yLabels.Set(_lobby.SettingsButton, () => UiText.Settings); // TalkBack: el engranaje es un botón de solo icono (02 §12)
            }

            ApplyA11y();
        }

        /// <summary>
        /// «Reducir movimiento» en vigor con <paramref name="profile"/>: la elección del jugador o, en <c>auto</c>,
        /// <c>ui_reduce_motion_default</c> o [P] el teléfono sin animaciones. Lo usa también la pantalla de arranque, antes de
        /// <see cref="SetUpA11y"/>.
        /// </summary>
        private bool ResolveReduceMotion(Profile profile)
        {
            A11yConfig config = _rcConfigs?.A11y ?? A11yConfig.Defaults;
            bool? choice = profile.Settings.ReduceMotion == ReduceMotionSetting.Auto ? (bool?)null : profile.Settings.ReduceMotion == ReduceMotionSetting.On;
            if (!_animatorScaleRead)
            {
                // «Quitar animaciones» del teléfono se lee una vez por arranque (JNI), como la escala de fuente (Q-B9-1).
                _animatorScale = LaunchOptions.SystemScale.AnimatorDurationScale;
                _animatorScaleRead = true;
            }

            return A11yRules.ReduceMotion(choice, config.UiReduceMotionDefault, _animatorScale);
        }

        // El Reto crea su flujo y su vista al abrirse (DailyChallengeScreen.Prepare, que pone SetUpDaily): se preparan antes de
        // construir su tablero, sin un fotograma tarde.
        private void PrepareDaily(LevelFlow flow, GameScreen view)
        {
            Profile? profile = _profile;
            if (profile == null)
            {
                return;
            }

            // Lo resuelto en el arranque o en Ajustes, el mismo que la campaña (no se vuelve a leer el teléfono a medias).
            flow.ReduceMotion = _reduceMotionNow;
            ApplyA11y(view, _rcConfigs?.A11y ?? A11yConfig.Defaults, profile);
        }

        // Al destruir la escena: quita lo publicado para TalkBack, cuyas acciones apuntan a esta escena.
        private void ClearA11y()
        {
            if (_screen != null)
            {
                _screen.A11y = null;
            }

            if (_dailyScreen?.View != null)
            {
                _dailyScreen.View.A11y = null;
            }

            _a11y?.Host.Clear();
            _a11y = null;
        }

        // Lo que está en vigor, a todo lo que lo usa: el flujo y la pantalla del nivel, los del Reto, el Mapa y Ajustes.
        private void ApplyA11y()
        {
            Profile? profile = _profile;
            if (profile == null)
            {
                return;
            }

            A11yConfig config = _rcConfigs?.A11y ?? A11yConfig.Defaults;
            _reduceMotionNow = ResolveReduceMotion(profile);
            if (_flow != null)
            {
                _flow.ReduceMotion = _reduceMotionNow;
            }

            ApplyA11y(_screen, config, profile);
            if (_dailyScreen?.Flow != null)
            {
                // Solo con el Reto abierto: una vista cerrada conserva su nivel y pisaría la opacidad global del patrón del tablero
                // que se ve (el de campaña). Al abrirse, la prepara PrepareDaily.
                _dailyScreen.Flow.ReduceMotion = _reduceMotionNow;
                ApplyA11y(_dailyScreen.View, config, profile);
            }

            if (_mapScreen != null)
            {
                _mapScreen.ProgressReduceMotion = _reduceMotionNow;
            }

            _settingsScreen?.MarkA11y(_reduceMotionNow, profile.Settings.HighContrast);
        }

        private void ApplyA11y(GameScreen? screen, A11yConfig config, Profile profile)
        {
            if (screen == null)
            {
                return;
            }

            screen.ReduceMotionFactor = config.AnimReduceMotionFactor;
            screen.HighContrast = profile.Settings.HighContrast;
            screen.A11y = _a11y;
        }

        // Cada fotograma: el lector de pantalla (solo trabaja con TalkBack encendido).
        private void TickA11y(float dtMs)
        {
            _a11y?.Tick(dtMs);
        }
    }
}
