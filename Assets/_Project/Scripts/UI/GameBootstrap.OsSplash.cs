#nullable enable

namespace AF.UI
{
    /// <summary>
    /// La pantalla de arranque del sistema hasta la intro (añadido de D-051 del 2026-10-28, «si hagamoslo asi»): el plugin
    /// <c>AfSplashHold.androidlib</c> vela el arranque con el icono v2 sobre el petróleo, sin bloquear el pintado de la ventana, y
    /// este parcial lo suelta (<see cref="AndroidOsSplash"/>) en cuanto el primer fotograma de la intro ya está en la pantalla, para
    /// que se vea icono → escena sin el petróleo liso de en medio. Si no llega la señal, la sueltan el fin de la pantalla de
    /// arranque o el tope de Java (3 s). La intro no cambia.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private string? _osSplashBy;
        private int _osSplashFrames;

        /// <summary>Quién soltó la pantalla del sistema (<c>intro</c>, <c>splash</c>, <c>no_splash</c> o <c>boot_end</c>); null si aún no (tests).</summary>
        internal string? OsSplashBy => _osSplashBy;

        // Cada Update, tras el Tick de la pantalla de arranque (la intro ya miró si su primer fotograma está en la pantalla).
        private void TickOsSplash()
        {
            if (_osSplashBy != null)
            {
                return;
            }

            _osSplashFrames++;
            BrandIntro? intro = _splash?.Intro;
            bool introShown = intro != null && intro.Visible;
            string? by = AndroidOsSplash.SignalFor(_splash != null, introShown, introShown && intro!.OnScreen, _osSplashFrames);
            if (by != null)
            {
                ReleaseOsSplash(by);
            }
        }

        // Una sola vez por escena (Java, una por proceso).
        private void ReleaseOsSplash(string by)
        {
            if (_osSplashBy != null)
            {
                return;
            }

            _osSplashBy = by;
            AndroidOsSplash.Release(by);
        }
    }
}
