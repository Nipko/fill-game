#nullable enable
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// Ajustes › «Enviar informes de fallos» (T-L6 de <c>docs/legal/investigacion-legal-mundial.md</c> §11, riesgo R5; D-057, añadido
    /// del 2026-10-27, que deja sin efecto la «línea sin interruptor» de Q-B12-4). Crashlytics sigue siendo interés legítimo (08 §2.3)
    /// y este interruptor es la objeción fácil (RGPD art. 21):
    /// <list type="bullet">
    /// <item>activado por defecto y guardado con el resto de ajustes, en <c>settings.crash_reports</c> [P];</item>
    /// <item>el arranque lo respeta antes de encender Crashlytics (<see cref="CrashReportsAtBoot"/>, en <c>SetUpTelemetry</c>):
    /// apagado, <c>CrashReporting.Stop</c> (recogida a false y los informes sin enviar, borrados);</item>
    /// <item>al tocarlo, al momento: encendido, <c>CrashReporting.Start</c>; apagado, <c>CrashReporting.Stop</c>; y el perfil sube a la
    /// nube si hay sesión (<see cref="UploadCloud"/>), porque la fusión conserva el apagado de cualquiera de los dos;</item>
    /// <item>con la banda &lt;13 nada cambia: el arranque se para antes (sin SDK) y Crashlytics no se enciende nunca.</item>
    /// </list>
    /// Sin evento de analítica: <c>crash_reports</c> no está en las claves de <c>af_settings_change</c> (08 §8.2); solo la sonda de
    /// desarrollo (<c>AF_STATE settings_change</c>).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>
        /// Lo que pide el perfil para Crashlytics en el arranque (<c>settings.crash_reports</c>; sin perfil, el default, activado). Solo
        /// cuenta si el arranque puede encender servicios de datos (no con &lt;13 guardado).
        /// </summary>
        private bool CrashReportsAtBoot => _profile?.Settings.CrashReports ?? true;

        /// <summary>
        /// Ajustes › «Enviar informes de fallos»: guarda <paramref name="on"/> en <c>settings.crash_reports</c> y enciende o apaga
        /// Crashlytics al momento (apagado: los informes sin enviar se borran). False sin perfil.
        /// </summary>
        public bool ChooseCrashReports(bool on)
        {
            Profile? profile = _profile;
            if (profile == null)
            {
                return false;
            }

            if (profile.Settings.CrashReports != on)
            {
                profile.Settings.CrashReports = on;
                SaveSettings(profile);

                // A la nube en el acto, si hay sesión (D-042, como tras una compra): la fusión deja apagado lo que cualquiera de los dos
                // apagó, así que sin subir, un snapshot viejo apagado volvería a apagar lo que el jugador acaba de encender.
                UploadCloud();
            }

            // Encender solo si este arranque puede tener servicios de datos (con <13 nunca: ni se llega a Ajustes). Apagar, siempre.
            if (!on || (_dataServicesAtBoot && !ConsentBlocked))
            {
                SetCrashReporting(on);
            }

            _settingsScreen?.MarkCrashReports(on);
            _probe?.SettingsChange(SettingsScreen.CrashReportsId, on ? "true" : "false", null);
            return true;
        }
    }
}
