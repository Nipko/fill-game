#nullable enable
using System;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Modo de captura para el marketing (D-066; <c>docs/dev/traspaso/spark-marketing.md</c> §6, enganche E1), solo en builds de
    /// desarrollo (<see cref="CaptureOptions.Resolve"/>: en release, siempre apagado). Con cualquier extra <c>af_capture*</c>:
    /// <list type="bullet">
    /// <item>el perfil se lee y no se guarda (<see cref="CaptureProfileStore"/>), sin nube (la falsa sin sesión), con la Liga de
    /// datos falsos y con la tienda simulada y el backend falso (como <c>af_fake_store</c>): la toma no toca el perfil del teléfono,
    /// ni la nube, ni la clasificación real, ni compra nada. Tampoco migra el progreso de v0 (<see cref="MigratesLegacyProgress"/>)
    /// ni deja usar «Eliminar cuenta y datos»;</item>
    /// <item>inmersivo total, sin barra de estado ni de navegación (<see cref="CaptureImmersive"/>), también al volver de segundo plano;</item>
    /// <item>un paquete <c>af-level/1</c> del extra o de un archivo sustituye a los de la escena (validado con
    /// <see cref="LevelPackReader"/>: <c>BoardBuilder</c> y <c>LevelValidation</c> del núcleo; si no vale, error y los de siempre),
    /// y la app arranca en su primer nivel (o en <c>af_level</c> si está en él);</item>
    /// <item>el HUD del nivel oculto (<see cref="GameScreen.SetCaptureHudHidden"/>), sin tarjeta previa ni banner de mundo;</item>
    /// <item>la solución del solver reproducida con ritmo «humano» (<see cref="CaptureAutoplay"/>);</item>
    /// <item>cámara lenta: el reloj de la presentación y del flujo por <see cref="CaptureOptions.TimeScale"/> (no
    /// <c>Time.timeScale</c>: el juego avanza con el reloj real sin escalar), así que la lógica es la misma, solo más lenta;</item>
    /// <item>la skin de las cuias y el fondo del tablero forzados solo en la vista, sin tocar el perfil;</item>
    /// <item>una pantalla del inicio abierta directamente al llegar a él (Mapa, Liga, Reto, Bitácora o Mercado), sin takeover.</item>
    /// </list>
    /// Cada acción escribe <c>AF_CAPTURE {json}</c> al empezar, al terminar o al fallar (<see cref="CaptureLog"/>).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Lo que se espera en el inicio a que la pantalla pedida se pueda abrir antes de darlo por fallido (ms reales).</summary>
        public const float CaptureScreenTimeoutMs = 15000f;

        private CaptureOptions _capture = CaptureOptions.Inactive;
        private string? _captureBoardJson;
        private CaptureAutoplay? _captureAutoplay;
        private CaptureScreen _captureScreenPending;
        private float _captureScreenWaitMs;
        private CuiaSkin? _captureSkin;

        /// <summary>Opciones del modo de captura de esta partida (<see cref="CaptureOptions.Inactive"/> sin extras y en release).</summary>
        public CaptureOptions Capture => _capture;

        /// <summary>El paquete del modo de captura se validó y sustituye a los de la escena.</summary>
        public bool CaptureBoardLoaded => _captureBoardJson != null;

        /// <summary>La reproducción de la solución en curso o acabada (null sin <c>af_capture_autoplay</c>; tests).</summary>
        internal CaptureAutoplay? CaptureAutoplayRun => _captureAutoplay;

        /// <summary>Pantalla del inicio que aún espera a abrirse (tests).</summary>
        internal CaptureScreen CaptureScreenPending => _captureScreenPending;

        // Al empezar Start, antes de leer el perfil: las opciones, el inmersivo, el paquete y la skin.
        private void SetUpCapture()
        {
            _capture = CaptureOptions.Resolve(Debug.isDebugBuild);
            _captureBoardJson = null;
            _captureAutoplay = null;
            _captureScreenPending = CaptureScreen.None;
            _captureScreenWaitMs = 0f;
            _captureSkin = null;
            if (!_capture.Active)
            {
                return;
            }

            CaptureLog.Write("start", _capture.Describe());
            // Si Android recrea la actividad desde Recientes, el intent vuelve a llegar y la app entra otra vez en el modo: que se vea en logcat.
            Debug.LogWarning("[AF.Capture] modo de captura activo (solo desarrollo): perfil en memoria, sin nube, Liga y tienda simuladas. Para salir, cierra la app y ábrela desde el icono.");
            for (int i = 0; i < _capture.Problems.Count; i++)
            {
                CaptureLog.Error("extra", _capture.Problems[i]);
            }

            bool immersive = CaptureImmersive.Apply();
            CaptureLog.Write("immersive", "\"applied\":" + (immersive ? "true" : "false"));
            if (_capture.HasBoard)
            {
                LoadCaptureBoard();
            }

            if (_capture.Skin != null)
            {
                if (SkinRewards.IsKnown(_capture.Skin))
                {
                    _captureSkin = CuiaSkin.For(_capture.Skin);
                }
                else
                {
                    CaptureLog.Error("skin", "skin desconocida (\"" + _capture.Skin + "\"): se usa la del perfil");
                }
            }

            _captureScreenPending = _capture.Screen;
            if (_capture.Autoplay)
            {
                _captureAutoplay = new CaptureAutoplay(_capture);
            }
        }

        // El paquete pedido, leído y validado con el lector de siempre (BoardBuilder y LevelValidation del núcleo). Si no vale,
        // error y se juega con los de la escena.
        private void LoadCaptureBoard()
        {
            string? json = _capture.ReadBoardJson(Application.persistentDataPath, out string? error);
            if (json == null)
            {
                CaptureLog.Error("board", error ?? "sin paquete");
                return;
            }

            try
            {
                LevelPack pack = LevelPackReader.Read(json);
                if (pack.IsReserve)
                {
                    CaptureLog.Error("board", pack.PackId + " es un paquete de reserva (kind reserve): usa uno de campaña");
                    return;
                }

                _captureBoardJson = json;
                CaptureLog.Write("board_loaded", "\"pack_id\":" + CaptureLog.Quote(pack.PackId) + ",\"levels\":" + pack.Count.ToString(CultureInfo.InvariantCulture)
                    + ",\"first\":" + pack.Levels[0].Level.ToString(CultureInfo.InvariantCulture) + ",\"id\":" + CaptureLog.Quote(pack.Levels[0].Id));
            }
            catch (LevelFormatException e)
            {
                CaptureLog.Error("board", "el paquete no es válido: " + e.Message);
            }
        }

        /// <summary>
        /// Se migra el progreso de v0 (<c>af_v0_level</c> de PlayerPrefs) al leer el perfil: solo con el almacén real del juego y nunca
        /// en una toma (<paramref name="capture"/>), porque la migración borra la clave antigua tras «guardar», y el almacén de la toma
        /// descarta lo guardado: el progreso se perdería.
        /// </summary>
        internal static bool MigratesLegacyProgress(bool storeOverride, bool directoryOverride, bool capture)
        {
            return !storeOverride && !directoryOverride && !capture;
        }

        // «Eliminar cuenta y datos» durante una toma: no se puede (llamaría al servidor y borraría PlayerPrefs, la sesión, la
        // analítica y la nube del teléfono de pruebas). Lo dice AF_CAPTURE error con what = account.
        private bool CaptureBlocksAccountDelete()
        {
            if (!_capture.Active)
            {
                return false;
            }

            CaptureLog.Error("account", "«Eliminar cuenta y datos» no está disponible en el modo de captura: no se borra nada");
            return true;
        }

        // El almacén del arranque en frío: con el modo de captura, solo lectura.
        private IProfileStore CaptureStore(IProfileStore store)
        {
            return _capture.Active && !(store is CaptureProfileStore) ? new CaptureProfileStore(store) : store;
        }

        // El almacén de verdad bajo el de la captura (para reconocer el de la reconstrucción por idioma).
        private static IProfileStore? UnwrapCaptureStore(IProfileStore? store)
        {
            return store is CaptureProfileStore capture ? capture.Inner : store;
        }

        // Con todo creado y antes de AppFlow.Start: el HUD, el fondo y la skin, y sin takeover si hay pantalla que abrir.
        private void ApplyCaptureAfterSetUp()
        {
            if (!_capture.Active || _flow == null || _screen == null)
            {
                return;
            }

            if (_capture.HideHud)
            {
                _flow.LevelCardsEnabled = false; // ni la tarjeta previa ni el banner de mundo tapan el tablero
                _flow.WorldBannersEnabled = false;
                _screen.SetCaptureHudHidden(true);
                CaptureLog.Write("hud", "\"hidden\":true");
            }

            int backdrop = _capture.BackdropLevel;
            if (backdrop > 0)
            {
                _screen.CaptureBackdropLevel = backdrop;
                CaptureLog.Write("backdrop", "\"bg_level\":" + backdrop.ToString(CultureInfo.InvariantCulture) + ",\"world\":"
                    + WorldTable.WorldOf(backdrop).ToString(CultureInfo.InvariantCulture));
            }

            if (_captureSkin != null && _art != null)
            {
                _art.CuiaSkin = _captureSkin; // solo la vista: el perfil (entitlements.skin_equipped) no cambia
                CaptureLog.Write("skin", "\"id\":" + CaptureLog.Quote(_captureSkin.Id));
            }

            if (_captureScreenPending != CaptureScreen.None)
            {
                _skipLobbyTakeover = true; // la primera entrada al inicio abre la pantalla pedida, no la libreta encima
            }
        }

        // Nivel de arranque con el paquete de captura: el pedido si está en él, si no su primer nivel (LevelFlow.ResolveLevel).
        private int CaptureStartLevel(int requested, LevelCatalog catalog)
        {
            if (_captureBoardJson == null || requested > 0)
            {
                return requested;
            }

            return catalog.FirstLevel;
        }

        /// <summary>
        /// Cada fotograma (<c>Update</c>; los tests lo llaman con su reloj): la skin forzada, la pantalla pedida y la reproducción
        /// de la solución, con <paramref name="realMs"/> ms de reloj real (las pausas del ritmo no se estiran con la cámara lenta).
        /// </summary>
        internal void TickCapture(float realMs)
        {
            if (!_capture.Active || _app == null)
            {
                return;
            }

            if (_captureSkin != null && _art != null && !ReferenceEquals(_art.CuiaSkin, _captureSkin))
            {
                _art.CuiaSkin = _captureSkin; // un premio o el Mercado la cambiaron: la toma sigue con la pedida
            }

            TickCaptureScreen(realMs);
            CaptureAutoplay? autoplay = _captureAutoplay;
            if (autoplay == null || autoplay.Done)
            {
                return;
            }

            if (InLevel && _flow != null && _screen != null)
            {
                autoplay.Tick(realMs, _flow, _screen.IsViewIdle, this);
            }
            else
            {
                autoplay.Abort("la app salió del nivel");
            }
        }

        // El paso de la presentación con la cámara lenta (1 sin ella).
        private float CaptureScaled(float dtMs)
        {
            return _capture.TimeScale < 1f ? dtMs * _capture.TimeScale : dtMs;
        }

        private void TickCaptureScreen(float realMs)
        {
            CaptureScreen pending = _captureScreenPending;
            if (pending == CaptureScreen.None || _app == null || _app.Screen != AppScreen.Lobby)
            {
                return;
            }

            _captureScreenWaitMs += realMs;
            if (TryOpenCaptureScreen(pending))
            {
                _captureScreenPending = CaptureScreen.None;
                CaptureLog.Write("screen_open", "\"screen\":\"" + CaptureOptions.ScreenId(pending) + "\",\"waited_ms\":"
                    + ((int)_captureScreenWaitMs).ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (_captureScreenWaitMs >= CaptureScreenTimeoutMs)
            {
                _captureScreenPending = CaptureScreen.None;
                CaptureLog.Error("screen", CaptureOptions.ScreenId(pending) + " no se pudo abrir: ¿está desbloqueada para este perfil? (af_max_level; la Liga con af_league; el Mercado con la tienda o af_fake_store)");
            }
        }

        // Lo mismo que el botón de cada pantalla en el inicio, con el inicio sin nada encima.
        private bool TryOpenCaptureScreen(CaptureScreen screen)
        {
            if (!LobbyClear() || (_mapScreen != null && _mapScreen.Visible) || (_logbookScreen != null && _logbookScreen.Visible))
            {
                return false;
            }

            switch (screen)
            {
                case CaptureScreen.Map: return OpenRiverMap(MapOpenSource.Lobby);
                case CaptureScreen.League: return OpenLeague();
                case CaptureScreen.Daily: return PressPlayDaily();
                case CaptureScreen.Logbook: return OpenLogbook(LogbookOpenSource.Card);
                case CaptureScreen.Market: return OpenMarket(MarketTab.Seeds);
                default: return false;
            }
        }

        // De vuelta de segundo plano: el sistema puede haber vuelto a sacar las barras.
        private void CaptureResumed()
        {
            if (_capture.Active)
            {
                CaptureImmersive.Apply();
            }
        }
    }

    /// <summary>
    /// Inmersivo total para las tomas (modo de captura, D-066 E1): sin barra de estado ni de navegación, que vuelven un momento
    /// con un deslizamiento desde el borde. Solo API de la plataforma por JNI (<c>WindowInsetsController</c> en Android 11+ y
    /// <c>setSystemUiVisibility</c> en 8-10), sin plugin, permisos ni SDK. En el Editor no hace nada.
    /// </summary>
    internal static class CaptureImmersive
    {
        // View.SYSTEM_UI_FLAG_* (Android 8-10): LAYOUT_STABLE | LAYOUT_HIDE_NAVIGATION | LAYOUT_FULLSCREEN | HIDE_NAVIGATION |
        // FULLSCREEN | IMMERSIVE_STICKY.
        private const int LegacyImmersiveFlags = 0x100 | 0x200 | 0x400 | 0x2 | 0x4 | 0x1000;

        // WindowInsetsController.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE.
        private const int ShowTransientBarsBySwipe = 2;

        /// <summary>Pide el inmersivo; true si se pudo pedir (en el teléfono).</summary>
        public static bool Apply()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Screen.fullScreen = true; // solo en el teléfono: en el Editor no toca la ventana del juego
                AndroidJavaObject activity = UnityEngine.Android.AndroidApplication.currentActivity;
                if (activity == null)
                {
                    return false;
                }

                activity.Call("runOnUiThread", new AndroidJavaRunnable(HideBars));
                return true;
            }
            catch (Exception e)
            {
                CaptureLog.Error("immersive", e.Message);
                return false;
            }
#else
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // En el hilo de la UI de Android.
        private static void HideBars()
        {
            try
            {
                AndroidJavaObject activity = UnityEngine.Android.AndroidApplication.currentActivity;
                using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (version.GetStatic<int>("SDK_INT") >= 30)
                    {
                        using (AndroidJavaObject controller = window.Call<AndroidJavaObject>("getInsetsController"))
                        using (var types = new AndroidJavaClass("android.view.WindowInsets$Type"))
                        {
                            controller.Call("hide", types.CallStatic<int>("systemBars"));
                            controller.Call("setSystemBarsBehavior", ShowTransientBarsBySwipe);
                        }
                    }
                    else
                    {
                        using (AndroidJavaObject decor = window.Call<AndroidJavaObject>("getDecorView"))
                        {
                            decor.Call("setSystemUiVisibility", LegacyImmersiveFlags);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AF.Capture] inmersivo: " + e.Message);
            }
        }
#endif
    }
}
