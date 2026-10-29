#nullable enable
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La carga con la intro de la marca a la vista (jornada 3 de la prueba completa, 2026-10-26; añadido de D-051):
    /// <list type="bullet">
    /// <item><b>Fotogramas para la intro:</b> entre los bloques de la carga, mientras la intro está a la vista
    /// (<see cref="BootSplash.IntroVisible"/>), se deja pintar un fotograma con la intro al instante de ahora
    /// (<see cref="IntroFrameDue"/>): antes solo llegaban a pintarse 2-3 y la animación no se veía moverse. Desde el añadido de D-051
    /// del 2026-10-26 la intro se ve entera (unos 2,5 s; 3,5 s desde el 2026-10-28) aunque la carga acabe antes, así que repartirla no alarga el arranque. Los
    /// huecos van donde el estado es seguro: antes de crear las pantallas que aún no están enlazadas (Update no hace nada hasta que
    /// existe el flujo).</item>
    /// <item><b>La sonda <c>AF_PERF boot_steps</c></b> (solo en desarrollo): desde que se creó la pantalla, cuándo acabó cada bloque
    /// de la carga, qué costó la escena de la marca y si salió de la caché, cuándo arrancó Unity el Awake y la pantalla, y cuándo se
    /// pintó su primer fotograma. Sirve para separar en el teléfono el arranque del motor (antes del Awake, no depende del juego)
    /// de lo nuestro.</item>
    /// <item><b>Jornada 4 (2026-10-28), el vertido fluido:</b> el movimiento de la intro (del chorro al nombre entero,
    /// <see cref="BrandIntro.SettledMs"/>) empieza en un hueco de la carga, cuando se sabe que su primer fotograma ya está en la
    /// pantalla (<see cref="BrandIntro.OnScreen"/>), y la carga se para mientras se mueve (<see cref="IntroGap"/>): cada fotograma
    /// del vertido sale a su ritmo, sin bloques de la carga entre medias. Lo que queda de la carga va en el último tramo, quieto, con
    /// el emblema y el nombre. Antes, la intro se movía mientras cargaba y solo llegaban a pintarse unos pocos fotogramas del
    /// vertido. La sonda <c>boot_steps</c> suma <c>visible_ms</c>, <c>play_ms</c>, <c>motion_hold_ms</c> (lo que la carga esperó al
    /// movimiento), <c>proc_awake_ms</c> (del arranque del proceso de Android al Awake) y <c>proc_visible_ms</c> (del arranque del
    /// proceso al primer fotograma de la intro ya en la pantalla), para comparar con la ráfaga de capturas.</item>
    /// <item><b>Jornada 6, observación 11 (2026-10-29):</b> <c>os_splash_icon_px</c> ([ancho, alto]) y <c>os_splash_icon_xy</c>
    /// ([izquierda, arriba]) son la caja en px de pantalla del icono que se vio en el velo (<see cref="AndroidOsSplash.ReadIcon"/>:
    /// la vista del icono de la pantalla del sistema o el círculo de <c>af_launch_window</c>); [-1,-1] sin medida. Su centro es
    /// xy + px / 2; se comparan con <c>morph_start_diam_px</c> y <c>morph_start_xy_px</c> de <c>brand_intro</c>.</item>
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
        private const int StepGame = 5;
        private const int StepScreens = 6;
        private const int StepSystems = 7;
        private static readonly string[] BootStepNames = { "profile", "catalog", "services", "sloth", "ads", "game", "screens", "systems" };

        /// <summary>Clave de «Cargando…» bajo el nombre, si la carga dura más que la intro (añadido de D-051 del 2026-10-26).</summary>
        internal const string BootLoadingKey = UiText.Keys.BootLoading;

        private readonly long[] _bootSteps = { -1, -1, -1, -1, -1, -1, -1, -1 };

        // Heap gestionado reservado y usado al acabar cada bloque (bytes; -1 sin marca): AF_PERF mem boot (DevProbe.Memory.cs).
        private readonly long[] _bootStepHeap = { -1, -1, -1, -1, -1, -1, -1, -1 };
        private readonly long[] _bootStepUsed = { -1, -1, -1, -1, -1, -1, -1, -1 };
        private long _awakeMs = -1;
        private long _procAwakeMs = -1;
        private long _introMotionHoldMs;
        private bool _bootInProgress;
        private bool _consentSetUp;
        private bool _introWaitCapped;

        /// <summary>
        /// Tope de seguridad de la espera a la intro tras la carga (ms desde que acaba la carga): <see cref="BrandIntro.AnimMs"/> + 2 s.
        /// Si se llega (fotogramas parados, reloj raro), la intro se quita y la sonda lo anota (<c>wait_capped</c>).
        /// </summary>
        internal const int IntroWaitCapMs = BrandIntro.AnimMs + 2000;

        /// <summary>
        /// Costura de los tests: se llama en cada hueco de la carga en que se deja pintar un fotograma para la intro, con el bloque
        /// que acaba de terminar (<see cref="BootStepCount"/>), justo antes del <c>yield</c>. Null fuera de ellos (y lo vuelven a null).
        /// </summary>
        internal static System.Action<GameBootstrap, int>? IntroFrameHookForTests { get; set; }

        /// <summary>El arranque aún no terminó (hasta <c>ResumeBackend(false)</c>): <c>OnApplicationPause(false)</c> no reanuda nada.</summary>
        internal bool BootInProgress => _bootInProgress;

        /// <summary>Llamadas a <c>restoreLedger</c> antes de <c>SetUpConsent</c> (08 §2.3 y §10.3: deben ser 0; tests).</summary>
        internal int RestoreLedgerBeforeConsent { get; private set; }

        // RestoreLedgerNow lo anota si llega antes de SetUpConsent (no debería: los tests lo comprueban).
        private void NoteRestoreLedger()
        {
            if (!_consentSetUp)
            {
                RestoreLedgerBeforeConsent++;
            }
        }
        private long _splashStartMs = -1;
        private int _introYields;

        /// <summary>Fotogramas que la carga dejó pintar para la intro (tests).</summary>
        internal int IntroYields => _introYields;

        /// <summary>Lo que la carga esperó al movimiento de la intro (ms; <c>motion_hold_ms</c>; tests).</summary>
        internal long IntroMotionHoldMs => _introMotionHoldMs;

        /// <summary>Tope de seguridad de la espera al movimiento en un hueco (ms desde que empezó): fotogramas parados, reloj raro.</summary>
        internal const int IntroMotionCapMs = BrandIntro.SettledMs + 2000;

        /// <summary>Solo para los tests: sustituye a <see cref="IntroMotionCapMs"/> (null = el de siempre). Lo vuelven a null.</summary>
        internal static int? IntroMotionCapOverrideMsForTests { get; set; }

        /// <summary>
        /// Costura de los tests: se llama una vez por hueco en que la carga espera al movimiento de la intro, en su primer fotograma
        /// de espera (el arranque, a medias). Null fuera de ellos (y lo vuelven a null).
        /// </summary>
        internal static System.Action<GameBootstrap>? IntroMotionHookForTests { get; set; }

        // Un hueco de la carga con la intro a la vista (jornada 4, 2026-10-28): deja pintar un fotograma y, si la intro ya puede
        // moverse (su primer fotograma está en la pantalla y no hay Reducir movimiento), la arranca y no vuelve a la carga hasta que
        // el nombre entró entero: el vertido sale a su ritmo, sin bloques de la carga entre sus fotogramas.
        private IEnumerator IntroGap()
        {
            yield return null;
            BootSplash? splash = _splash;
            if (splash == null || !splash.TryPlayIntro())
            {
                yield break;
            }

            double start = Time.realtimeSinceStartupAsDouble;
            double cap = start + ((IntroMotionCapOverrideMsForTests ?? IntroMotionCapMs) / 1000.0);
            bool hooked = false;
            while (_splash != null && splash.IntroMoving && Time.realtimeSinceStartupAsDouble < cap)
            {
                if (!hooked)
                {
                    hooked = true;
                    IntroMotionHookForTests?.Invoke(this);
                }

                yield return null;
            }

            _introMotionHoldMs += (long)((Time.realtimeSinceStartupAsDouble - start) * 1000.0);
        }

        // Del arranque del proceso de Android a ahora (ms; -1 fuera del teléfono o si falla): solo para la sonda boot_steps, en
        // desarrollo. Alinea los tiempos de Unity (realtimeSinceStartup) con la ráfaga de capturas, que cuenta desde el lanzamiento.
        private static long ProcessUptimeMs()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && DEVELOPMENT_BUILD
            try
            {
                using (var clock = new AndroidJavaClass("android.os.SystemClock"))
                using (var process = new AndroidJavaClass("android.os.Process"))
                {
                    return clock.CallStatic<long>("elapsedRealtime") - process.CallStatic<long>("getStartElapsedRealtime");
                }
            }
            catch (System.Exception)
            {
                return -1L;
            }
#else
            return -1L;
#endif
        }

        // Del arranque del proceso al instante splashMs (ms desde la creación de la pantalla); -1 si falta algo.
        private long ProcMsAt(long splashMs)
        {
            if (_procAwakeMs < 0 || _awakeMs < 0 || _splashStartMs < 0 || splashMs < 0)
            {
                return -1L;
            }

            return _procAwakeMs + (_splashStartMs - _awakeMs) + splashMs;
        }

        /// <summary>Cuándo acabó cada bloque de la carga (ms desde que se creó la pantalla; -1 si no llegó; tests).</summary>
        internal long BootStepMs(int step) => _bootSteps[step];

        /// <summary>Heap gestionado usado al acabar el bloque <paramref name="step"/> (bytes; -1 sin marca; AF_PERF mem boot, tests).</summary>
        internal long BootStepUsed(int step) => _bootStepUsed[step];

        /// <summary>Heap gestionado reservado al acabar el bloque <paramref name="step"/> (bytes; -1 sin marca).</summary>
        internal long BootStepHeap(int step) => _bootStepHeap[step];

        /// <summary>Número de bloques de la carga con marca.</summary>
        internal static int BootStepCount => BootStepNames.Length;

        // Anota el fin de un bloque de la carga (con la pantalla de arranque).
        private void MarkBootStep(int step)
        {
            if (_splash != null)
            {
                _bootSteps[step] = _splash.AgeMs;
            }

            if (DevProbe.Enabled)
            {
                _bootStepHeap[step] = UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong();
                _bootStepUsed[step] = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            }
        }

        // ¿Toca dejar pintar un fotograma para la intro? Mientras está a la vista (añadido de D-051 del 2026-10-26: se ve entera,
        // así que repartir la carga no alarga nada y la animación va fluida; después, los puntos de «Cargando…»); entonces la lleva
        // al instante de ahora (su Tick de este fotograma fue antes del bloque) y cuenta el fotograma.
        private bool IntroFrameDue(int step)
        {
            if (_splash == null || !_splash.IntroVisible)
            {
                return false;
            }

            _splash.RefreshIntro();
            _introYields++;
            IntroFrameHookForTests?.Invoke(this, step);
            return true;
        }

        // Completa el informe de la intro con lo del arranque (start_ms, yields) antes de su sonda.
        private void StampIntroReport(BrandIntroReport report)
        {
            report.StartMs = _splashStartMs;
            report.Yields = _introYields;
            report.WaitCapped = _introWaitCapped;
        }

        // AF_PERF boot_steps (solo en desarrollo), al cortar la pantalla de arranque.
        private void WriteBootSteps(BootReport boot)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            Debug.Log(FormatBootSteps(boot));
            DevProbe.Mem("boot", 0, DevProbe.FormatBootStepHeaps(_bootStepHeap, _bootStepUsed) + DevProbe.FormatCanvasStats());
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
            sb.Append(",\"visible_ms\":").Append((intro?.VisibleMs ?? -1L).ToString(inv));
            sb.Append(",\"play_ms\":").Append((intro?.PlayMs ?? -1L).ToString(inv));
            sb.Append(",\"motion_hold_ms\":").Append(_introMotionHoldMs.ToString(inv));
            sb.Append(",\"proc_awake_ms\":").Append(_procAwakeMs.ToString(inv));
            sb.Append(",\"proc_visible_ms\":").Append(ProcMsAt(intro?.VisibleMs ?? -1L).ToString(inv));
            OsSplashInfo os = AndroidOsSplash.Read(); // añadido de D-051 del 2026-10-28 (GameBootstrap.OsSplash.cs)
            sb.Append(",\"os_splash_held_ms\":").Append(os.HeldMs.ToString(inv));
            sb.Append(",\"os_splash_release\":\"").Append(os.Result).Append('"');
            sb.Append(",\"os_splash_by\":\"").Append(_osSplashBy ?? string.Empty).Append('"');
            sb.Append(",\"proc_os_splash_ms\":").Append(os.ProcReleaseMs.ToString(inv));
            sb.Append(",\"os_splash_focus_ms\":").Append(os.FocusAfterMs.ToString(inv));
            sb.Append(",\"os_splash_veil\":\"").Append(os.Veil).Append('"');
            SplashIconBox icon = AndroidOsSplash.ReadIcon(); // jornada 6, observación 11: la caja del icono que se vio (px)
            bool measured = icon.Measured;
            sb.Append(",\"os_splash_icon_px\":[").Append((measured ? icon.Width : -1).ToString(inv)).Append(',')
                .Append((measured ? icon.Height : -1).ToString(inv)).Append(']');
            sb.Append(",\"os_splash_icon_xy\":[").Append((measured ? icon.Left : -1).ToString(inv)).Append(',')
                .Append((measured ? icon.Top : -1).ToString(inv)).Append(']');
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

    /// <summary>La clave de «Cargando…» de la pantalla de arranque (parcial de <see cref="UiText"/>).</summary>
    public static partial class UiText
    {
        /// <summary>Parcial de <see cref="Keys"/>.</summary>
        public static partial class Keys
        {
            /// <summary>
            /// <c>boot_loading</c>: «Cargando…» bajo el nombre de la pantalla de arranque, si la carga dura más que la intro de la marca
            /// (añadido de D-051 del 2026-10-26; <see cref="BootSplash.SetLoadingText"/>).
            /// </summary>
            public const string BootLoading = "boot_loading";
        }
    }
}
