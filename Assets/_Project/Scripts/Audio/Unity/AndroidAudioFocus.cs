#nullable enable
using System;
using UnityEngine;

namespace AF.Audio.Unity
{
    /// <summary>
    /// El foco de audio en Android (observación 10 de la jornada 6; D-026, añadido del 2026-10-29): llama por JNI al plugin propio
    /// <c>com.amazonfill.audiofocus.AfAudioFocus</c> (<c>Assets/_Project/Plugins/Android/AfAudioFocus.androidlib</c>, solo API de la
    /// plataforma, sin permisos), que pide el foco con <c>AudioFocusRequest</c> y <c>USAGE_GAME</c> y avisa de sus cambios por un
    /// <see cref="AndroidJavaProxy"/> en el hilo principal de Android. Aquí solo se guarda el último aviso (con un candado, sin
    /// asignar al tomarlo): <see cref="UnityAudioService"/> lo toma en su fotograma. En el Editor y en otras plataformas,
    /// <see cref="FakeAudioFocusPort"/>, que da el foco y no avisa nunca.
    /// </summary>
    public sealed class AndroidAudioFocus : IAudioFocusPort
    {
        /// <summary>La clase Java del plugin.</summary>
        public const string BridgeClass = "com.amazonfill.audiofocus.AfAudioFocus";

        private const string LogPrefix = "[AF.AndroidAudioFocus] ";
        private readonly object _gate = new object();
        private AudioFocusState _pending;
        private bool _arrived;
#if UNITY_ANDROID && !UNITY_EDITOR
        private Listener? _listener;
#endif

        private AndroidAudioFocus()
        {
        }

        /// <summary>El de esta plataforma: el de Android en un teléfono y el falso en el resto (el Editor).</summary>
        public static IAudioFocusPort ForPlatform()
        {
            return Application.platform == RuntimePlatform.Android ? new AndroidAudioFocus() : new FakeAudioFocusPort();
        }

        /// <inheritdoc />
        public AudioFocusState Request()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                _listener ??= new Listener(this);
                using (var bridge = new AndroidJavaClass(BridgeClass))
                {
                    string? result = bridge.CallStatic<string>("request", UnityEngine.Android.AndroidApplication.currentContext, _listener);
                    return AudioFocusStates.Parse(result);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + "no se pudo pedir el foco de audio (" + e.GetType().Name + "); suena sin él.");
                return AudioFocusState.None;
            }
#else
            return AudioFocusState.None;
#endif
        }

        /// <inheritdoc />
        public void Abandon()
        {
            lock (_gate)
            {
                _arrived = false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass(BridgeClass))
                {
                    bridge.CallStatic("abandon");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + "no se pudo soltar el foco de audio (" + e.GetType().Name + ").");
            }
#endif
        }

        /// <inheritdoc />
        public bool OtherMusicAtForeground()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass(BridgeClass))
                {
                    return bridge.CallStatic<bool>("otherMusicAtForeground");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + "no se pudo mirar si suena otra música (" + e.GetType().Name + ").");
                return false;
            }
#else
            return false;
#endif
        }

        /// <inheritdoc />
        public bool TryTake(out AudioFocusState state)
        {
            lock (_gate)
            {
                state = _pending;
                if (!_arrived)
                {
                    return false;
                }

                _arrived = false;
                return true;
            }
        }

        // Cualquier hilo (el principal de Android): si llegan varios antes del fotograma, cuenta el último.
        private void Deliver(string? wire)
        {
            AudioFocusState state = AudioFocusStates.Parse(wire);
            if (state == AudioFocusState.None)
            {
                return;
            }

            lock (_gate)
            {
                _pending = state;
                _arrived = true;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // AfAudioFocus.Listener: un solo método, onFocus(String), en el hilo principal de Android.
        private sealed class Listener : AndroidJavaProxy
        {
            private readonly AndroidAudioFocus _owner;

            public Listener(AndroidAudioFocus owner)
                : base(BridgeClass + "$Listener")
            {
                _owner = owner;
            }

            public override AndroidJavaObject? Invoke(string methodName, object[] args)
            {
                if (methodName == "onFocus")
                {
                    _owner.Deliver(args != null && args.Length > 0 ? args[0] as string : null);
                    return null;
                }

                return base.Invoke(methodName, args);
            }

            // Por si la búsqueda por reflexión no pasa por Invoke (con Preserve para el stripping High, como AndroidAgeSignals).
            [UnityEngine.Scripting.Preserve]
            public void onFocus(string? state)
            {
                _owner.Deliver(state);
            }
        }
#endif
    }
}
