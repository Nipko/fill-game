#nullable enable
using System;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La pantalla de arranque del sistema hasta la intro (añadido de D-051 del 2026-10-28, «si hagamoslo asi»): el plugin propio
    /// <c>Assets/_Project/Plugins/Android/AfSplashHold.androidlib</c> (<c>com.amazonfill.splash.AfSplashHold</c>) vela el arranque
    /// con el icono v2 sobre el petróleo, sin el petróleo liso de en medio, hasta que el juego lo suelta por JNI con
    /// <see cref="Release"/>; como mucho <see cref="CapMs"/>. Desde la observación 1 de la jornada 6 es un velo que no bloquea: una
    /// vista nativa con el fondo de ventana del tema y, en Android 12+, la propia pantalla del sistema, encima de la ventana ya
    /// pintada (no aplaza fotogramas ni toca el foco). Vive en <c>AF.UI</c> porque necesita el motor, como
    /// <see cref="AndroidAgeSignals"/>. En el editor y en otras plataformas no hace nada.
    /// </summary>
    public static class AndroidOsSplash
    {
        /// <summary>La clase Java del puente.</summary>
        public const string BridgeClass = "com.amazonfill.splash.AfSplashHold";

        /// <summary>El tope del velo en Java (<c>AfSplashHold.CAP_MS</c>; un test EditMode los compara) [P].</summary>
        public const int CapMs = 3000;

        /// <summary>Lo soltó la intro: su primer fotograma ya está en la pantalla (<see cref="BrandIntro.OnScreen"/>).</summary>
        public const string ByIntro = "intro";

        /// <summary>Lo soltó la pantalla de arranque sin intro, a su segundo fotograma.</summary>
        public const string BySplash = "splash";

        /// <summary>Sin pantalla de arranque (recarga de la escena: no es un arranque en frío).</summary>
        public const string ByNoSplash = "no_splash";

        /// <summary>Lo soltó el fin de la pantalla de arranque (o de la escena) sin señal antes.</summary>
        public const string ByBootEnd = "boot_end";

        private const string LogPrefix = "[AF.AndroidOsSplash] ";

        /// <summary>
        /// Qué suelta la pantalla del sistema en este fotograma (null = aún nada): con la intro, cuando su primer fotograma ya está en la
        /// pantalla (también quieta, con Reducir movimiento, y con la caché del emblema); sin intro, al segundo fotograma de la pantalla
        /// de arranque; sin pantalla de arranque, ya. <paramref name="frames"/> cuenta los Update desde el Awake (1 el primero).
        /// </summary>
        public static string? SignalFor(bool hasSplash, bool introShown, bool introOnScreen, int frames)
        {
            if (!hasSplash)
            {
                return ByNoSplash;
            }

            if (introShown)
            {
                return introOnScreen ? ByIntro : null;
            }

            return frames >= 2 ? BySplash : null;
        }

        /// <summary>Suelta la pantalla del sistema (<paramref name="by"/>: quién). Java la ignora si no hay velo.</summary>
        public static void Release(string by)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass(BridgeClass))
                {
                    bridge.CallStatic("release", by);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + "no se pudo soltar la pantalla del sistema (" + e.GetType().Name + "); la suelta su tope.");
            }
#endif
        }

        /// <summary>Lo que hizo el velo (solo para la sonda de desarrollo <c>AF_PERF boot_steps</c>; en el editor, sin velo).</summary>
        public static OsSplashInfo Read()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass(BridgeClass))
                {
                    return new OsSplashInfo(
                        bridge.CallStatic<string>("result") ?? "none",
                        bridge.CallStatic<long>("heldMs"),
                        bridge.CallStatic<long>("procReleaseMs"),
                        bridge.CallStatic<long>("focusAfterMs"),
                        bridge.CallStatic<string>("veil") ?? "none");
                }
            }
            catch (Exception)
            {
                return OsSplashInfo.None;
            }
#else
            return OsSplashInfo.None;
#endif
        }
    }

    /// <summary>Lo que hizo el velo de la pantalla del sistema (<see cref="AndroidOsSplash.Read"/>).</summary>
    public readonly struct OsSplashInfo
    {
        /// <summary>Sin velo (editor, otra plataforma o sin el puente).</summary>
        public static readonly OsSplashInfo None = new OsSplashInfo("none", -1L, -1L, -1L, "none");

        /// <summary>Crea el informe (sin decir qué veló: <see cref="Veil"/> = <c>none</c>).</summary>
        public OsSplashInfo(string result, long heldMs, long procReleaseMs, long focusAfterMs)
            : this(result, heldMs, procReleaseMs, focusAfterMs, "none")
        {
        }

        /// <summary>Crea el informe.</summary>
        public OsSplashInfo(string result, long heldMs, long procReleaseMs, long focusAfterMs, string veil)
        {
            Result = result;
            HeldMs = heldMs;
            ProcReleaseMs = procReleaseMs;
            FocusAfterMs = focusAfterMs;
            Veil = veil;
        }

        /// <summary><c>none</c>, <c>pending</c>, <c>signal</c> (la soltó el juego), <c>cap</c> (el tope) o <c>gone</c> (sonda <c>os_splash_release</c>).</summary>
        public string Result { get; }

        /// <summary>Lo que duró el velo, desde que empezó la actividad de Unity (ms; -1 sin velo; <c>os_splash_held_ms</c>).</summary>
        public long HeldMs { get; }

        /// <summary>Del arranque del proceso a soltarla (ms; -1; <c>proc_os_splash_ms</c>).</summary>
        public long ProcReleaseMs { get; }

        /// <summary>
        /// Siempre -1 desde el velo sin bloqueo (jornada 6, observación 1): el velo no toca el foco de la ventana. Con la retención
        /// anterior medía de soltarla al foco. Se queda para no cambiar la sonda <c>os_splash_focus_ms</c>.
        /// </summary>
        public long FocusAfterMs { get; }

        /// <summary>
        /// Qué veló el arranque (<c>os_splash_veil</c>): <c>own</c> (la vista propia con el fondo de ventana del tema), <c>system</c>
        /// (Android 12+ entregó su pantalla de arranque y se quedó encima) o <c>none</c>.
        /// </summary>
        public string Veil { get; }
    }
}
