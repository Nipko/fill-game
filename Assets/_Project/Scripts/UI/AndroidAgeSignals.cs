#nullable enable
using System;
using AF.Platform;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Play Age Signals en Android (T-L3; D-057 y su añadido del 2026-10-27): llama por JNI al puente propio
    /// <c>com.amazonfill.agesignals.AfAgeSignals.request(context, listener)</c> (<c>AfAgeSignals.androidlib</c>, sobre
    /// <c>com.google.android.play:age-signals:0.0.4</c>), que traduce la respuesta de la API al formato de
    /// <see cref="AgeSignal.Parse"/> (<c>adult</c>, <c>range:&lt;inferior&gt;[:&lt;superior&gt;]</c>, <c>none</c> o <c>error</c>) y la
    /// entrega en su hilo de fondo <c>AfAgeSignals-callbacks</c>, nunca en el principal de Android (con Unity en pausa, llamar al proxy
    /// desde ahí sería el interbloqueo del ANR de Play Billing; docs/dev/iap.md). Vive en <c>AF.UI</c> porque necesita el motor, como
    /// <see cref="AndroidSimCountry"/>. En el editor y en cualquier otra plataforma, «sin señal»; si el puente no está o falla, <see cref="AgeSignal.Error"/>: en los dos casos manda la
    /// banda autodeclarada. Nada de la respuesta va a los logs ni a la analítica (T-L4).
    /// </summary>
    public sealed class AndroidAgeSignals : IAgeSignals
    {
        /// <summary>La clase Java del puente (<c>Assets/_Project/Plugins/Android/AfAgeSignals.androidlib</c>).</summary>
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
                using (var bridge = new AndroidJavaClass(BridgeClass))
                {
                    // Como AF.IAP.PlayBilling (AfBilling): el contexto de la app; el puente usa getApplicationContext().
                    bridge.CallStatic("request", UnityEngine.Android.AndroidApplication.currentContext, new Listener(this));
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
        // AfAgeSignals.Listener: un solo método, onResult(String), en el hilo AfAgeSignals-callbacks (o en el de Unity si falla al pedir).
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
