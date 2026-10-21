#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Analytics
{
    /// <summary>
    /// Destino de los eventos (08 §2.1, principio 6): Firebase Analytics en el teléfono (<c>AF.Analytics.Firebase.FirebaseAnalyticsSink</c>,
    /// F3-6), el sumidero de logcat de desarrollo (<c>AF.Analytics.Unity.LogcatAnalyticsSink</c>) y el falso de los tests
    /// (<see cref="FakeAnalyticsSink"/>).
    /// </summary>
    public interface IAnalyticsSink
    {
        /// <summary>
        /// El sumidero envía datos fuera del dispositivo, así que solo recibe eventos con el consentimiento de analítica
        /// (08 §2.3: <c>Analytics.SetEnabled(profile.consent.analytics)</c>). Los de diagnóstico local (logcat, falso), no.
        /// </summary>
        bool RequiresConsent { get; }

        /// <summary>Envía un evento (ya validado y con sus parámetros comunes).</summary>
        void Send(AnalyticsEvent e);

        /// <summary>Fija una propiedad de usuario de 08 §8.1.</summary>
        void SetUserProperty(string name, string value);
    }

    /// <summary>
    /// Sumidero que además recoge datos por su cuenta (Firebase Analytics: sesiones, primera apertura) y por eso tiene que saber si
    /// hay consentimiento aunque no reciba eventos (08 §2.3: <c>Analytics.SetEnabled(profile.consent.analytics)</c>).
    /// <see cref="AnalyticsService"/> se lo dice al añadirlo y en cada cambio.
    /// </summary>
    public interface ICollectionAwareSink
    {
        /// <summary>Enciende o apaga la recogida propia del sumidero.</summary>
        void SetCollectionEnabled(bool enabled);
    }

    /// <summary>Sumidero falso: guarda lo que recibe, en orden.</summary>
    public sealed class FakeAnalyticsSink : IAnalyticsSink
    {
        private readonly List<AnalyticsEvent> _events = new List<AnalyticsEvent>();
        private readonly Dictionary<string, string> _properties = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Crea el sumidero; con <paramref name="requiresConsent"/> se comporta como uno real (Firebase).</summary>
        public FakeAnalyticsSink(bool requiresConsent = false)
        {
            RequiresConsent = requiresConsent;
        }

        /// <inheritdoc />
        public bool RequiresConsent { get; }

        /// <summary>Los eventos recibidos.</summary>
        public IReadOnlyList<AnalyticsEvent> Events => _events;

        /// <summary>Las propiedades de usuario recibidas.</summary>
        public IReadOnlyDictionary<string, string> Properties => _properties;

        /// <summary>Los nombres de los eventos recibidos, en orden.</summary>
        public IReadOnlyList<string> Names
        {
            get
            {
                var names = new string[_events.Count];
                for (int i = 0; i < names.Length; i++)
                {
                    names[i] = _events[i].Name;
                }

                return names;
            }
        }

        /// <summary>El último evento llamado <paramref name="name"/>, o null.</summary>
        public AnalyticsEvent? Last(string name)
        {
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_events[i].Name, name, StringComparison.Ordinal))
                {
                    return _events[i];
                }
            }

            return null;
        }

        /// <summary>Vacía lo recibido.</summary>
        public void Clear()
        {
            _events.Clear();
            _properties.Clear();
        }

        /// <inheritdoc />
        public void Send(AnalyticsEvent e) => _events.Add(e);

        /// <inheritdoc />
        public void SetUserProperty(string name, string value) => _properties[name] = value;
    }
}
