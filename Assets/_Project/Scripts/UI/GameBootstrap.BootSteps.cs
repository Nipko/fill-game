#nullable enable
using System.Globalization;
using System.Text;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La carga con la intro de la marca a la vista (jornada 3 de la prueba completa, 2026-10-26; añadido de D-051):
    /// <list type="bullet">
    /// <item><b>Fotogramas para la intro:</b> entre los bloques de la carga, mientras la intro anima
    /// (<see cref="BootSplash.IntroAnimating"/>), se deja pintar un fotograma con la intro al instante de ahora
    /// (<see cref="IntroFrameDue"/>): antes solo llegaban a pintarse 2-3 y la animación no se veía moverse. Con Reducir movimiento,
    /// con la animación ya en su último fotograma o sin intro, ninguno: no alarga nada que no se vea.</item>
    /// <item><b>La sonda <c>AF_PERF boot_steps</c></b> (solo en desarrollo): desde que se creó la pantalla, cuándo acabó cada bloque
    /// de la carga, qué costó la escena de la marca y si salió de la caché, cuándo arrancó Unity el Awake y la pantalla, y cuándo se
    /// pintó su primer fotograma. Sirve para separar en el teléfono el arranque del motor (antes del Awake, no depende del juego)
    /// de lo nuestro.</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        // Bloques de la carga, en orden (marcas de _bootSteps).
        private const int StepProfile = 0;
        private const int StepCatalog = 1;
        private const int StepServices = 2;
        private const int StepSloth = 3;
        private const int StepAds = 4;
        private const int StepScreens = 5;
        private const int StepSystems = 6;
        private static readonly string[] BootStepNames = { "profile", "catalog", "services", "sloth", "ads", "screens", "systems" };

        private readonly long[] _bootSteps = { -1, -1, -1, -1, -1, -1, -1 };
        private long _awakeMs = -1;
        private long _splashStartMs = -1;
        private int _introYields;

        /// <summary>Fotogramas que la carga dejó pintar para la intro (tests).</summary>
        internal int IntroYields => _introYields;

        /// <summary>Cuándo acabó cada bloque de la carga (ms desde que se creó la pantalla; -1 si no llegó; tests).</summary>
        internal long BootStepMs(int step) => _bootSteps[step];

        /// <summary>Número de bloques de la carga con marca.</summary>
        internal static int BootStepCount => BootStepNames.Length;

        // Anota el fin de un bloque de la carga (con la pantalla de arranque).
        private void MarkBootStep(int step)
        {
            if (_splash != null)
            {
                _bootSteps[step] = _splash.AgeMs;
            }
        }

        // ¿Toca dejar pintar un fotograma para la intro? Solo mientras anima; entonces la lleva al instante de ahora (su Tick de
        // este fotograma fue antes del bloque) y cuenta el fotograma.
        private bool IntroFrameDue()
        {
            if (_splash == null || !_splash.IntroAnimating)
            {
                return false;
            }

            _splash.RefreshIntro();
            _introYields++;
            return true;
        }

        // Completa el informe de la intro con lo del arranque (start_ms, yields) antes de su sonda.
        private void StampIntroReport(BrandIntroReport report)
        {
            report.StartMs = _splashStartMs;
            report.Yields = _introYields;
        }

        // AF_PERF boot_steps (solo en desarrollo), al cortar la pantalla de arranque.
        private void WriteBootSteps(BootReport boot)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            Debug.Log(FormatBootSteps(boot));
        }

        /// <summary>La línea <c>AF_PERF boot_steps</c> del último arranque con pantalla (tests).</summary>
        internal string FormatBootSteps(BootReport boot)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(320);
            sb.Append("AF_PERF {\"event\":\"boot_steps\",\"first_open\":").Append(boot.FirstOpen ? "true" : "false");
            sb.Append(",\"awake_ms\":").Append(_awakeMs.ToString(inv));
            sb.Append(",\"start_ms\":").Append(_splashStartMs.ToString(inv));
            BrandIntroReport? intro = _introReport;
            sb.Append(",\"emblem_ms\":").Append((intro?.EmblemMs ?? -1L).ToString(inv));
            sb.Append(",\"emblem_cache\":\"").Append(CacheId(intro?.EmblemCache ?? BrandEmblemCacheResult.Off)).Append('"');
            sb.Append(",\"first_frame_ms\":").Append((intro?.FirstFrameMs ?? -1L).ToString(inv));
            for (int i = 0; i < BootStepNames.Length; i++)
            {
                sb.Append(",\"").Append(BootStepNames[i]).Append("\":").Append(_bootSteps[i].ToString(inv));
            }

            sb.Append(",\"load_ms\":").Append(boot.LoadMs.ToString(inv));
            sb.Append(",\"yields\":").Append(_introYields.ToString(inv));
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>El nombre de la sonda de <paramref name="cache"/> (<c>off</c>, <c>miss</c> o <c>hit</c>).</summary>
        internal static string CacheId(BrandEmblemCacheResult cache)
        {
            return cache == BrandEmblemCacheResult.Hit ? "hit" : cache == BrandEmblemCacheResult.Miss ? "miss" : "off";
        }
    }
}
