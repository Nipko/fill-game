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

        /// <summary>
        /// Solo para los tests: la caja del icono que «midió» Java (null = lo de siempre: el puente en el teléfono, nada en el
        /// editor). Los tests que la cambian la vuelven a null en su TearDown.
        /// </summary>
        public static SplashIconBox? IconOverrideForTests { get; set; }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaClass? s_bridge;
        private static int s_iconVersion = -1;
        private static SplashIconBox s_icon;
#endif

        /// <summary>
        /// La caja del icono de la pantalla del sistema en la pantalla, en px (jornada 6, observación 11): la vista del icono que
        /// entregó Android 12+ (<c>SplashScreenView.getIconView()</c>, el círculo visible) o el círculo de <c>af_launch_window</c>
        /// con el velo propio, y el marco de la DecorView. <see cref="SplashIconBox.None"/> si no hay medida (el editor, sin velo, o
        /// aún sin layout): entonces la intro usa la caja de la guía de Android (240/160 dp centrados).
        /// </summary>
        public static SplashIconBox ReadIcon()
        {
            SplashIconBox? fake = IconOverrideForTests;
            if (fake.HasValue)
            {
                return fake.Value;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // El puente se guarda y la caja solo se copia (un int[] de JNI) cuando Java dice que cambió.
                s_bridge ??= new AndroidJavaClass(BridgeClass);
                int version = s_bridge.CallStatic<int>("iconBoxVersion");
                if (version != s_iconVersion)
                {
                    s_icon = SplashIconBox.FromArray(s_bridge.CallStatic<int[]>("iconBox"));
                    s_iconVersion = version;
                }

                return s_icon;
            }
            catch (Exception)
            {
                return SplashIconBox.None;
            }
#else
            return SplashIconBox.None;
#endif
        }

        /// <summary>
        /// Lo que costó en Java medir la máscara de iconos del sistema (<c>AfSplashHold.maskMicros()</c>, en ms; -1 si no se midió:
        /// el editor, Android 8-11 o el velo propio). Solo para la sonda <c>os_splash_mask_ms</c> de <c>AF_PERF boot_steps</c>
        /// (jornada 6, observación 12).
        /// </summary>
        public static float ReadMaskMs()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass(BridgeClass))
                {
                    long us = bridge.CallStatic<long>("maskMicros");
                    return us < 0L ? -1f : us / 1000f;
                }
            }
            catch (Exception)
            {
                return -1f;
            }
#else
            return -1f;
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

    /// <summary>
    /// La caja del icono de la pantalla del sistema en la pantalla (px de pantalla, desde arriba a la izquierda;
    /// <see cref="AndroidOsSplash.ReadIcon"/>, <c>AfSplashHold.iconBox()</c>): la del círculo visible y la de la DecorView, el marco
    /// con que se pasa al lienzo. Sin medida, todo 0 (<see cref="Measured"/> false). Con la pantalla del sistema, también la forma de
    /// su máscara (jornada 6, observación 12): los radios a 0, 45 y 22,5 grados en diezmilésimas de medio lado
    /// (<see cref="MaskAxis"/>, <see cref="MaskDiag"/>, <see cref="MaskMid"/>; 0 sin máscara: el círculo).
    /// </summary>
    public readonly struct SplashIconBox
    {
        /// <summary>Sin medida.</summary>
        public static readonly SplashIconBox None = default;

        /// <summary>Cuántos enteros da Java (<c>AfSplashHold.BOX_LENGTH</c>): la caja, el marco y los tres radios de la máscara.</summary>
        public const int ArrayLength = 11;

        /// <summary>Crea la caja, sin la forma de la máscara (el círculo).</summary>
        public SplashIconBox(int left, int top, int width, int height, int refLeft, int refTop, int refWidth, int refHeight)
            : this(left, top, width, height, refLeft, refTop, refWidth, refHeight, 0, 0, 0)
        {
        }

        /// <summary>Crea la caja con la forma de la máscara (radios a 0, 45 y 22,5 grados en diezmilésimas de medio lado).</summary>
        public SplashIconBox(int left, int top, int width, int height, int refLeft, int refTop, int refWidth, int refHeight, int maskAxis, int maskDiag, int maskMid)
        {
            MaskAxis = maskAxis;
            MaskDiag = maskDiag;
            MaskMid = maskMid;
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            RefLeft = refLeft;
            RefTop = refTop;
            RefWidth = refWidth;
            RefHeight = refHeight;
        }

        /// <summary>Izquierda de la vista del icono (px).</summary>
        public int Left { get; }

        /// <summary>Arriba de la vista del icono (px).</summary>
        public int Top { get; }

        /// <summary>Ancho de la vista del icono (px).</summary>
        public int Width { get; }

        /// <summary>Alto de la vista del icono (px).</summary>
        public int Height { get; }

        /// <summary>Izquierda de la DecorView (px).</summary>
        public int RefLeft { get; }

        /// <summary>Arriba de la DecorView (px).</summary>
        public int RefTop { get; }

        /// <summary>Ancho de la DecorView (px): el ancho del lienzo.</summary>
        public int RefWidth { get; }

        /// <summary>Alto de la DecorView (px): el alto del lienzo.</summary>
        public int RefHeight { get; }

        /// <summary>Radio de la máscara en los ejes (diezmilésimas de medio lado; 0 sin máscara).</summary>
        public int MaskAxis { get; }

        /// <summary>Radio de la máscara en las diagonales, a 45 grados (diezmilésimas de medio lado; 0 sin máscara).</summary>
        public int MaskDiag { get; }

        /// <summary>Radio de la máscara a 22,5 grados (diezmilésimas de medio lado; 0 sin máscara): solo para la sonda del ajuste.</summary>
        public int MaskMid { get; }

        /// <summary>Hay medida: las dos cajas tienen tamaño.</summary>
        public bool Measured => Width > 0 && Height > 0 && RefWidth > 0 && RefHeight > 0;

        /// <summary>Diámetro del círculo visible (px): el lado menor de la vista del icono.</summary>
        public float Diameter => Mathf.Min(Width, Height);

        /// <summary>Centro del círculo (px de pantalla, desde arriba).</summary>
        public Vector2 Center => new Vector2(Left + (Width * 0.5f), Top + (Height * 0.5f));

        /// <summary>
        /// Las dos cajas difieren en más de <paramref name="tolerancePx"/> px en algún borde (la del icono o la del marco), o una
        /// tiene medida y la otra no.
        /// </summary>
        public bool DiffersFrom(SplashIconBox other, int tolerancePx)
        {
            if (Measured != other.Measured)
            {
                return true;
            }

            if (!Measured)
            {
                return false;
            }

            return Far(Left, other.Left, tolerancePx) || Far(Top, other.Top, tolerancePx) || Far(Width, other.Width, tolerancePx)
                || Far(Height, other.Height, tolerancePx) || Far(RefLeft, other.RefLeft, tolerancePx) || Far(RefTop, other.RefTop, tolerancePx)
                || Far(RefWidth, other.RefWidth, tolerancePx) || Far(RefHeight, other.RefHeight, tolerancePx)
                || MaskAxis != other.MaskAxis || MaskDiag != other.MaskDiag || MaskMid != other.MaskMid;
        }

        private static bool Far(int a, int b, int tolerance)
        {
            return Math.Abs(a - b) > tolerance;
        }

        /// <summary>
        /// La caja desde el arreglo de Java (<see cref="ArrayLength"/> enteros; con 8, sin la forma de la máscara); sin medida si no
        /// cuadra.
        /// </summary>
        public static SplashIconBox FromArray(int[]? box)
        {
            if (box == null || box.Length < 8)
            {
                return None;
            }

            if (box.Length < ArrayLength)
            {
                return new SplashIconBox(box[0], box[1], box[2], box[3], box[4], box[5], box[6], box[7]);
            }

            return new SplashIconBox(box[0], box[1], box[2], box[3], box[4], box[5], box[6], box[7], box[8], box[9], box[10]);
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
