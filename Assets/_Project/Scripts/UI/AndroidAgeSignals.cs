#nullable enable
using System;
using AF.Platform;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Play Age Signals en Android (T-L3; D-057 y su añadido del 2026-10-27): llama por JNI al puente propio
    /// <c>com.amazonfill.agesignals.AfAgeSignals.request(activity, listener)</c>, que traduce la respuesta de la API al formato de
    /// <see cref="AgeSignal.Parse"/> (<c>adult</c>, <c>range:&lt;inferior&gt;[:&lt;superior&gt;]</c>, <c>none</c> o <c>error</c>) y la
    /// entrega en cualquier hilo. Vive en <c>AF.UI</c> porque necesita el motor, como <see cref="AndroidSimCountry"/>. En el editor y
    /// en cualquier otra plataforma, «sin señal»; si el puente no está o falla, <see cref="AgeSignal.Error"/>: en los dos casos manda la
    /// banda autodeclarada. Nada de la respuesta va a los logs ni a la analítica (T-L4).
    /// </summary>
    public sealed class AndroidAgeSignals : IAgeSignals
    {
        /// <summary>La clase Java del puente (<c>AfAgeSignals.androidlib</c>, pendiente de la coordenada Maven de la API).</summary>
        public const string BridgeClass = "com.amazonfill.agesignals.AfAgeSignals";

        private const string LogPrefix = "[AF.AndroidAgeSignals] ";
        private readonly object _gate = new object();
        private string? _wire;
        private bool _arrived;

        /// <summary>La de esta plataforma: la de Android en un teléfono y «sin señal» en el resto (el editor).</summary>
        public static IAgeSignals ForPlatform()
        {
            return Application.platform == RuntimePlatform.Android ? new AndroidAgeSignals() : new FakeAgeSignals(AgeSignal.None);
        }

        /// <inheritdoc />
        public void Request()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject? activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var bridge = new AndroidJavaClass(BridgeClass))
                {
                    bridge.CallStatic("request", activity, new Listener(this));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + "no se pudo pedir la señal de edad (" + e.GetType().Name + "); se usa la banda autodeclarada.");
                Deliver("error");
            }
#else
            Deliver("none");
#endif
        }

        /// <inheritdoc />
        public bool TryTake(out AgeSignal signal)
        {
            string? wire;
            lock (_gate)
            {
                if (!_arrived)
                {
                    signal = AgeSignal.None;
                    return false;
                }

                _arrived = false;
                wire = _wire;
                _wire = null;
            }

            signal = AgeSignal.Parse(wire);
            return true;
        }

        private void Deliver(string? wire)
        {
            lock (_gate)
            {
                _wire = wire;
                _arrived = true;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // AfAgeSignals.Listener: un solo método, onResult(String), en cualquier hilo.
        private sealed class Listener : AndroidJavaProxy
        {
            private readonly AndroidAgeSignals _owner;

            public Listener(AndroidAgeSignals owner)
                : base(BridgeClass + "$Listener")
            {
                _owner = owner;
            }

            public override AndroidJavaObject? Invoke(string methodName, object[] args)
            {
                if (methodName == "onResult")
                {
                    _owner.Deliver(args != null && args.Length > 0 ? args[0] as string : null);
                    return null;
                }

                return base.Invoke(methodName, args);
            }

            // Por si la búsqueda por reflexión no pasa por Invoke (con Preserve para el stripping High, como BillingListenerProxy).
            [UnityEngine.Scripting.Preserve]
            public void onResult(string? wire)
            {
                _owner.Deliver(wire);
            }
        }
#endif
    }
}
