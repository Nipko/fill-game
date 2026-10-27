#nullable enable
using AF.Backend.Firebase;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Generic;
using Firebase.Analytics;
#endif

namespace AF.Analytics.Firebase
{
    /// <summary>
    /// Sumidero real: Firebase Analytics (F3-6; D-039; 08 §8). Solo existe en el teléfono con la configuración de Firebase
    /// (<see cref="TryCreate"/>); en el editor y en los tests, null.
    /// <para>
    /// <b>Consentimiento (08 §2.3; B12):</b> <see cref="RequiresConsent"/> = true, así que <see cref="AnalyticsService"/> solo le
    /// entrega eventos con la recogida encendida. Además Firebase recoge por su cuenta (sesiones, primera apertura): el manifiesto
    /// (<c>Assets/Plugins/Android/LauncherManifest.xml</c>) arranca con <c>firebase_analytics_collection_enabled = false</c>, sin
    /// almacenamiento de analítica ni de anuncios y sin ID de publicidad, y <see cref="SetCollectionEnabled"/> sigue al consentimiento.
    /// Las señales de anuncios (<c>ad_storage</c>, <c>ad_user_data</c>, <c>ad_personalization</c>) siguen denegadas: llegan con MAX y UMP.
    /// Sin <c>user_id</c>: el UID de Firebase Auth nunca va a la analítica.
    /// </para>
    /// Los booleanos, que en el evento son <c>true</c>/<c>false</c>, llegan a Firebase como 0/1 (D-033, Q-B7-18 (a);
    /// <c>docs/dev/remote-config-analytics.md</c> §6).
    /// </summary>
    public sealed class FirebaseAnalyticsSink : IAnalyticsSink, ICollectionAwareSink
    {
        private FirebaseAnalyticsSink()
        {
        }

        /// <summary>El sumidero real, o null fuera del teléfono o sin la configuración de Firebase.</summary>
        public static IAnalyticsSink? TryCreate()
        {
            return FirebaseBoot.IsConfigured ? new FirebaseAnalyticsSink() : null;
        }

        /// <summary>Valor de un booleano en Firebase: 1 o 0 (Q-B7-18 (a)).</summary>
        public static long BoolValue(bool value) => value ? 1L : 0L;

        /// <inheritdoc />
        public bool RequiresConsent => true;

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <inheritdoc />
        public void Send(AnalyticsEvent e)
        {
            if (e == null)
            {
                return;
            }

            string name = e.Name;
            Parameter[] parameters = ToParameters(e);
            FirebaseBoot.WhenReady(ok =>
            {
                if (ok)
                {
                    FirebaseAnalytics.LogEvent(name, parameters);
                }
            });
        }

        /// <inheritdoc />
        public void SetUserProperty(string name, string value)
        {
            FirebaseBoot.WhenReady(ok =>
            {
                if (ok)
                {
                    FirebaseAnalytics.SetUserProperty(name, value);
                }
            });
        }

        /// <inheritdoc />
        public void SetCollectionEnabled(bool enabled)
        {
            FirebaseBoot.WhenReady(ok =>
            {
                if (!ok)
                {
                    return;
                }

                FirebaseAnalytics.SetConsent(new Dictionary<ConsentType, ConsentStatus>
                {
                    { ConsentType.AnalyticsStorage, enabled ? ConsentStatus.Granted : ConsentStatus.Denied },
                    { ConsentType.AdStorage, ConsentStatus.Denied },
                    { ConsentType.AdUserData, ConsentStatus.Denied },
                    { ConsentType.AdPersonalization, ConsentStatus.Denied },
                });
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(enabled);
            });
        }

        /// <summary>
        /// Borra los datos de analítica del dispositivo y un ID de instancia nuevo (08 §13.3: <c>resetAnalyticsData()</c> al eliminar
        /// la cuenta). Lo llama quien borra la cuenta.
        /// </summary>
        public static void ResetData()
        {
            FirebaseBoot.WhenReady(ok =>
            {
                if (ok)
                {
                    FirebaseAnalytics.ResetAnalyticsData();
                }
            });
        }

        private static Parameter[] ToParameters(AnalyticsEvent e)
        {
            var list = new List<Parameter>(e.Params.Count + e.Common.Count);
            Append(list, e.Params);
            Append(list, e.Common);
            return list.ToArray();
        }

        private static void Append(List<Parameter> list, IReadOnlyList<AnalyticsParam> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                AnalyticsParam p = source[i];
                switch (p.Kind)
                {
                    case ParamKind.String:
                        if (p.Text != null)
                        {
                            list.Add(new Parameter(p.Name, p.Text));
                        }

                        break;
                    case ParamKind.Long:
                        list.Add(new Parameter(p.Name, p.Long));
                        break;
                    case ParamKind.Double:
                        list.Add(new Parameter(p.Name, p.Double));
                        break;
                    case ParamKind.Bool:
                        list.Add(new Parameter(p.Name, BoolValue(p.Bool)));
                        break;
                }
            }
        }
#else
        /// <inheritdoc />
        public void Send(AnalyticsEvent e)
        {
        }

        /// <inheritdoc />
        public void SetUserProperty(string name, string value)
        {
        }

        /// <inheritdoc />
        public void SetCollectionEnabled(bool enabled)
        {
        }

        /// <summary>Sin Firebase en el editor: no hace nada.</summary>
        public static void ResetData()
        {
        }
#endif
    }

    /// <summary>
    /// Crashlytics (F3-6; 08 §2.3: <c>Crashlytics.Enable()</c>, diagnóstico; pregunta abierta 6 de 08). El manifiesto lo deja
    /// apagado (<c>firebase_crashlytics_collection_enabled = false</c>) para que lo encienda el arranque, en el código, y se pueda
    /// cortar por banda de edad o consentimiento sin otro build. Sin <c>SetUserId</c>: los informes no llevan el UID.
    /// <para>
    /// Ajustes › «Enviar informes de fallos» (T-L6; D-057, añadido del 2026-10-27; sustituye Q-B12-4) llama a <see cref="Start"/> o
    /// <see cref="Stop"/>, también en el arranque: apagado, la recogida a false y los informes sin enviar, borrados.
    /// </para>
    /// <para>
    /// Lo que se aplica al estar Firebase listo es siempre la <b>última</b> petición (<see cref="Start"/> o <see cref="Stop"/>), no
    /// cada una en su orden: un <see cref="Start"/> que espera a Firebase no puede encenderlo después de un <see cref="Stop"/>.
    /// </para>
    /// </summary>
    public static class CrashReporting
    {
        private static readonly CrashReportingIntent s_intent = new CrashReportingIntent();

        /// <summary>Lo enciende cuando Firebase está listo. False si este build no tiene Crashlytics (editor o sin configuración).</summary>
        public static bool Start()
        {
            if (!FirebaseBoot.IsConfigured)
            {
                return false;
            }

            s_intent.Request(true);
#if UNITY_ANDROID && !UNITY_EDITOR
            FirebaseBoot.WhenReady(ApplyWanted);
#endif
            return true;
        }

        /// <summary>
        /// Lo apaga de forma explícita (B12, banda &lt; 13; 08 §7): <c>setCrashlyticsCollectionEnabled(false)</c>, que persiste entre
        /// arranques, así que un <see cref="Start"/> anterior queda anulado. Si el arranque de Firebase en C# no se ha pedido (el arranque
        /// con la banda &lt; 13 guardada), no lo arranca (ni App Check): lo apaga en el SDK de Android, que ya inició el proceso
        /// (<c>FirebaseInitProvider</c>), y borra los informes sin enviar; lo mismo si Firebase no llega a estar listo
        /// (<see cref="FirebaseBootState.Unavailable"/>). False si este build no tiene Crashlytics.
        /// </summary>
        public static bool Stop()
        {
            if (!FirebaseBoot.IsConfigured)
            {
                return false;
            }

            s_intent.Request(false);
#if UNITY_ANDROID && !UNITY_EDITOR
            if (s_intent.OnStop(FirebaseBoot.State != FirebaseBootState.NotStarted) == CrashReportingAction.DisableNative)
            {
                StopNative();
            }
            else
            {
                FirebaseBoot.WhenReady(ApplyWanted); // Unavailable responde en el acto con false: StopNative
            }
#endif
            return true;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void ApplyWanted(bool ok)
        {
            switch (s_intent.WhenBooted(ok))
            {
                case CrashReportingAction.EnableInSdk:
                    global::Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = true;
                    break;
                case CrashReportingAction.DisableInSdk:
                    global::Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = false;
                    DeleteUnsentNative(); // T-L6: lo que quedara en el disco sin enviar no sale nunca (el SDK de C# no lo expone)
                    break;
                case CrashReportingAction.DisableNative:
                    StopNative(); // el Firebase de C# no está disponible: apagado igualmente en el SDK de Android
                    break;
            }
        }

        // FirebaseCrashlytics de Android sin pasar por el Firebase de C#: la recogida apagada (persiste) y los informes que hubiera
        // en el disco, borrados para que nunca salgan. Sin FirebaseApp (no hay FirebaseInitProvider) Crashlytics no corre: nada que hacer.
        private static void StopNative()
        {
            try
            {
                using (var crashlytics = new UnityEngine.AndroidJavaClass("com.google.firebase.crashlytics.FirebaseCrashlytics"))
                using (UnityEngine.AndroidJavaObject instance = crashlytics.CallStatic<UnityEngine.AndroidJavaObject>("getInstance"))
                {
                    instance.Call("setCrashlyticsCollectionEnabled", false);
                    instance.Call("deleteUnsentReports");
                }
            }
            catch (System.Exception)
            {
                // Sin FirebaseApp o sin la clase: Crashlytics no está corriendo en este proceso.
            }
        }

        // T-L6 (D-057): los informes sin enviar, borrados con el SDK de Android (deleteUnsentReports), tras apagar la recogida con el
        // de C#. Con la recogida apagada Crashlytics guarda en el disco lo que capture hasta que alguien lo envíe o lo borre.
        private static void DeleteUnsentNative()
        {
            try
            {
                using (var crashlytics = new UnityEngine.AndroidJavaClass("com.google.firebase.crashlytics.FirebaseCrashlytics"))
                using (UnityEngine.AndroidJavaObject instance = crashlytics.CallStatic<UnityEngine.AndroidJavaObject>("getInstance"))
                {
                    instance.Call("deleteUnsentReports");
                }
            }
            catch (System.Exception)
            {
                // Sin FirebaseApp o sin la clase: nada que borrar.
            }
        }
#endif
    }

    /// <summary>Qué hacer con Crashlytics (<see cref="CrashReportingIntent"/>).</summary>
    public enum CrashReportingAction
    {
        /// <summary>Nada (o esperar a que Firebase responda).</summary>
        None = 0,

        /// <summary>Encenderlo con el SDK de C# (Firebase listo).</summary>
        EnableInSdk = 1,

        /// <summary>Apagarlo con el SDK de C# (Firebase listo) y borrar lo no enviado (T-L6).</summary>
        DisableInSdk = 2,

        /// <summary>Apagarlo en el SDK de Android, sin el Firebase de C# (no pedido o no disponible), y borrar lo no enviado.</summary>
        DisableNative = 3,
    }

    /// <summary>
    /// La decisión de <see cref="CrashReporting"/>, sin SDK: <b>la última petición manda</b>. Lo que se aplica cuando Firebase responde
    /// es la última de <see cref="Request"/>, no cada una en su orden: un encendido que esperaba a Firebase no gana a un apagado
    /// posterior, y un apagado no se pierde aunque Firebase falle.
    /// </summary>
    public sealed class CrashReportingIntent
    {
        /// <summary>La última petición: true encendido, false apagado (al empezar, apagado, como el manifiesto).</summary>
        public bool Wanted { get; private set; }

        /// <summary>Anota una petición; manda sobre las anteriores.</summary>
        public void Request(bool on)
        {
            Wanted = on;
        }

        /// <summary>
        /// Al pedir el apagado: sin el arranque de Firebase pedido (<paramref name="firebaseBootRequested"/> = false), apagarlo ya en el
        /// SDK de Android, sin arrancar Firebase ni App Check; si no, esperar a que Firebase responda (<see cref="WhenBooted"/>).
        /// </summary>
        public CrashReportingAction OnStop(bool firebaseBootRequested)
        {
            return firebaseBootRequested ? CrashReportingAction.None : CrashReportingAction.DisableNative;
        }

        /// <summary>
        /// Cuando Firebase responde: listo, la última petición con el SDK de C#; sin Firebase, solo el apagado (en el SDK de Android),
        /// porque encender no es posible.
        /// </summary>
        public CrashReportingAction WhenBooted(bool firebaseReady)
        {
            if (firebaseReady)
            {
                return Wanted ? CrashReportingAction.EnableInSdk : CrashReportingAction.DisableInSdk;
            }

            return Wanted ? CrashReportingAction.None : CrashReportingAction.DisableNative;
        }
    }
}
