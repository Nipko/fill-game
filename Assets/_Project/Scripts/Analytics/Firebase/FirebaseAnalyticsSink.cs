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
    /// </summary>
    public static class CrashReporting
    {
        /// <summary>Lo enciende cuando Firebase está listo. False si este build no tiene Crashlytics (editor o sin configuración).</summary>
        public static bool Start()
        {
            if (!FirebaseBoot.IsConfigured)
            {
                return false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            FirebaseBoot.WhenReady(ok =>
            {
                if (ok)
                {
                    global::Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = true;
                }
            });
#endif
            return true;
        }
    }
}
