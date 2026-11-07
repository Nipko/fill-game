#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Vertical solo en los teléfonos (añadido de D-059 del 2026-11-06, Q-BB-1 (b), decidida por el dueño): el manifiesto ya no
    /// restringe la orientación (<c>ProjectSetup.Orientation</c> en autorrotación con las cuatro) y aquí se bloquea en vertical
    /// cuando el lado menor de la pantalla del dispositivo mide menos de <see cref="TabletMinSmallestWidthDp"/> dp. Tabletas,
    /// plegables abiertos y Chromebooks giran, con la interfaz en la columna de <see cref="ScreenFrame"/> (02 §5, «Orientación»).
    /// <para>En Android lo pide primero el plugin <c>AfSplashHold</c> (Java) dentro del <c>onCreate</c> de la actividad, antes de su
    /// primer fotograma; aquí se confirma al arrancar el reproductor (<see cref="RuntimeInitializeLoadType.BeforeSplashScreen"/>, para
    /// que Unity no vuelva a la autorrotación de los Player Settings) y se decide otra vez cada vez que cambia la pantalla (un plegable
    /// que se abre o se cierra). En iOS (solo iPhone en la primera versión, <c>ProjectSetup.Ios</c>) siempre vertical. En el Editor no
    /// hace nada.</para>
    /// </summary>
    public static class OrientationPolicy
    {
        /// <summary>Lado menor (dp) desde el que el dispositivo gira: la frontera teléfono/tableta de Android (la misma en Java).</summary>
        public const int TabletMinSmallestWidthDp = 600;

        /// <summary>True si es un teléfono (lado menor de 1 a 599 dp): ahí el juego va en vertical.</summary>
        public static bool IsPhone(int smallestWidthDp)
        {
            return smallestWidthDp > 0 && smallestWidthDp < TabletMinSmallestWidthDp;
        }

        /// <summary>Lo que se pide: vertical en un teléfono, autorrotación en lo demás; null si no se sabe (no se toca).</summary>
        public static ScreenOrientation? Decide(int smallestWidthDp)
        {
            if (smallestWidthDp <= 0)
            {
                return null;
            }

            return IsPhone(smallestWidthDp) ? ScreenOrientation.Portrait : ScreenOrientation.AutoRotation;
        }

        /// <summary>La pantalla a la que se pide la orientación (la de Unity; una falsa en los tests).</summary>
        public interface ITarget
        {
            /// <summary>La orientación pedida ahora.</summary>
            ScreenOrientation Requested { get; }

            /// <summary>Pide <paramref name="orientation"/>; con autorrotación, las cuatro orientaciones permitidas.</summary>
            void Request(ScreenOrientation orientation);
        }

        /// <summary>
        /// Pide a <paramref name="target"/> lo de <see cref="Decide"/> si cambia algo; devuelve lo pedido (null si no se sabe el
        /// lado menor o ya estaba así).
        /// </summary>
        public static ScreenOrientation? Apply(int smallestWidthDp, ITarget target)
        {
            ScreenOrientation? wanted = Decide(smallestWidthDp);
            if (wanted == null || target.Requested == wanted.Value)
            {
                return null;
            }

            target.Request(wanted.Value);
            return wanted;
        }

        /// <summary>La pantalla de Unity.</summary>
        internal sealed class UnityScreenTarget : ITarget
        {
            public ScreenOrientation Requested => Screen.orientation;

            public void Request(ScreenOrientation orientation)
            {
                if (orientation == ScreenOrientation.AutoRotation)
                {
                    Screen.autorotateToPortrait = true;
                    Screen.autorotateToPortraitUpsideDown = true;
                    Screen.autorotateToLandscapeLeft = true;
                    Screen.autorotateToLandscapeRight = true;
                }

                Screen.orientation = orientation;
            }
        }

        private static readonly UnityScreenTarget s_screen = new UnityScreenTarget();

        /// <summary>
        /// Al arrancar el reproductor, antes de su pantalla de arranque: lo antes que corre C#. Solo en el teléfono (Android o iOS);
        /// en el Editor no hace nada.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        internal static void OnLaunch()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            Apply(ReadDeviceSmallestWidthDp(), s_screen);
#endif
        }

        /// <summary>Tras cargar la primera escena: el vigía que vuelve a decidir si cambia la pantalla (solo en el teléfono).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void StartWatcher()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            var go = new GameObject("AF.OrientationPolicy");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<Watcher>();
#endif
        }

        /// <summary>El lado menor de la pantalla del dispositivo (dp), o 0 si no se puede leer (en el Editor, 0).</summary>
        internal static int ReadDeviceSmallestWidthDp()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var bridge = new AndroidJavaClass(AndroidOsSplash.BridgeClass))
                {
                    return activity == null ? 0 : bridge.CallStatic<int>("deviceSmallestWidthDp", activity);
                }
            }
            catch (System.Exception)
            {
                return 0; // sin lectura no se toca: manda lo que pidió el plugin al crear la actividad
            }
#elif !UNITY_EDITOR && UNITY_IOS
            return 1; // solo iPhone en la primera versión: siempre vertical
#else
            return 0;
#endif
        }

        // El vigía: con la pantalla de otro tamaño (giro, plegable que se abre o se cierra, ventana), vuelve a decidir.
        private sealed class Watcher : MonoBehaviour
        {
            private int _width;
            private int _height;

            private void Update()
            {
                if (Screen.width == _width && Screen.height == _height)
                {
                    return;
                }

                _width = Screen.width;
                _height = Screen.height;
                Apply(ReadDeviceSmallestWidthDp(), s_screen);
            }
        }
    }
}
