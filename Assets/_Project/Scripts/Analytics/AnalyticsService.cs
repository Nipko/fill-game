#nullable enable
using System;
using System.Collections.Generic;
using AF.Events;

namespace AF.Analytics
{
    /// <summary>
    /// <c>AF.Analytics</c> (08 §2.2): suscriptor del bus (<see cref="EventBus"/>; D-033, Q-T1 (a)) que traduce los eventos de
    /// dominio a la taxonomía única de 08 §8.2, añade los parámetros comunes de 08 §8.1 y los entrega a sus sumideros,
    /// respetando el consentimiento. Sin motor.
    /// <para>
    /// Cada evento pasa por <see cref="AnalyticsCatalog"/>: un nombre que no está dado de alta no se envía, y un parámetro que
    /// el evento no admite se quita; las dos cosas quedan en <see cref="Violations"/> (los tests exigen que esté vacío) [P].
    /// Los sumideros con <see cref="IAnalyticsSink.RequiresConsent"/> solo reciben con <see cref="CollectionEnabled"/>, que
    /// empieza apagado (08 §2.3: «Analytics apagado hasta consentimiento»); el de logcat y el falso reciben siempre.
    /// </para>
    /// Solo desde el hilo principal.
    /// </summary>
    public sealed class AnalyticsService : IDisposable
    {
        private readonly List<IAnalyticsSink> _sinks = new List<IAnalyticsSink>();
        private readonly List<string> _violations = new List<string>();
        private readonly CommonParams _common = new CommonParams();
        private readonly EventBus? _bus;
        private readonly Action<LevelStarted> _onStarted;
        private readonly Action<LevelWon> _onWon;
        private readonly Action<LevelFailed> _onFailed;
        private readonly Action<LevelAbandoned> _onAbandoned;
        private readonly Action<SeedsEarned> _onEarned;
        private readonly Action<SeedsSpent> _onSpent;

        /// <summary>Crea el servicio; con <paramref name="bus"/>, se suscribe a sus eventos de dominio.</summary>
        public AnalyticsService(EventBus? bus = null, ICommonParamsSource? common = null)
        {
            CommonSource = common;
            _onStarted = OnLevelStarted;
            _onWon = OnLevelWon;
            _onFailed = OnLevelFailed;
            _onAbandoned = OnLevelAbandoned;
            _onEarned = OnSeedsEarned;
            _onSpent = OnSeedsSpent;
            _bus = bus;
            if (bus != null)
            {
                bus.Subscribe(_onStarted);
                bus.Subscribe(_onWon);
                bus.Subscribe(_onFailed);
                bus.Subscribe(_onAbandoned);
                bus.Subscribe(_onEarned);
                bus.Subscribe(_onSpent);
            }
        }

        /// <summary>Hay consentimiento de analítica (08 §2.3). Apagado al crear el servicio.</summary>
        public bool CollectionEnabled { get; private set; }

        /// <summary>De dónde salen los comunes en cada envío; null = sin comunes.</summary>
        public ICommonParamsSource? CommonSource { get; set; }

        /// <summary>Tope de <see cref="Violations"/> (las siguientes se cuentan en <see cref="ViolationCount"/>, no se guardan).</summary>
        public const int MaxViolations = 64;

        /// <summary>Rechazos del catálogo desde el arranque, también los que ya no caben en <see cref="Violations"/>.</summary>
        public int ViolationCount { get; private set; }

        /// <summary>Eventos o parámetros rechazados por el catálogo, en orden (<c>evento</c> o <c>evento.parámetro</c>), hasta <see cref="MaxViolations"/>.</summary>
        public IReadOnlyList<string> Violations => _violations;

        /// <summary>Eventos enviados (a algún sumidero).</summary>
        public int Sent { get; private set; }

        /// <summary>Añade un sumidero.</summary>
        public void AddSink(IAnalyticsSink sink)
        {
            _sinks.Add(sink ?? throw new ArgumentNullException(nameof(sink)));
        }

        /// <summary>
        /// Enciende o apaga la recogida (08 §2.3: <c>Analytics.SetEnabled(profile.consent.analytics)</c>, siempre antes de
        /// <c>GameFlow.Go</c>). Con <paramref name="enabled"/> = false, los sumideros reales no reciben nada desde ahora.
        /// </summary>
        public void SetCollectionEnabled(bool enabled)
        {
            CollectionEnabled = enabled;
        }

        /// <summary>Valida <paramref name="e"/>, le añade los comunes y lo entrega. False si el catálogo no lo admite.</summary>
        public bool Log(AnalyticsEvent e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            CatalogEntry? entry = AnalyticsCatalog.Find(e.Name);
            if (entry == null)
            {
                Violate(e.Name);
                return false;
            }

            for (int i = e.Params.Count - 1; i >= 0; i--)
            {
                if (!entry.Allows(e.Params[i].Name))
                {
                    Violate(e.Name + "." + e.Params[i].Name);
                    e.RemoveParamAt(i);
                }
            }

            e.ClearCommon();
            _common.Clear();
            CommonSource?.Fill(_common);
            _common.AppendTo(e);

            bool any = false;
            for (int i = 0; i < _sinks.Count; i++)
            {
                IAnalyticsSink sink = _sinks[i];
                if (sink.RequiresConsent && !CollectionEnabled)
                {
                    continue;
                }

                sink.Send(e);
                any = true;
            }

            if (any)
            {
                Sent++;
            }

            return true;
        }

        /// <summary>Fija una propiedad de usuario de 08 §8.1 (las demás se rechazan y van a <see cref="Violations"/>).</summary>
        public bool SetUserProperty(string name, string value)
        {
            bool known = false;
            for (int i = 0; i < AnalyticsCatalog.UserProperties.Count; i++)
            {
                if (string.Equals(AnalyticsCatalog.UserProperties[i], name, StringComparison.Ordinal))
                {
                    known = true;
                    break;
                }
            }

            if (!known || value == null)
            {
                Violate("user_property." + name);
                return false;
            }

            for (int i = 0; i < _sinks.Count; i++)
            {
                if (!_sinks[i].RequiresConsent || CollectionEnabled)
                {
                    _sinks[i].SetUserProperty(name, value);
                }
            }

            return true;
        }

        private void Violate(string what)
        {
            ViolationCount++;
            if (_violations.Count < MaxViolations)
            {
                _violations.Add(what);
            }
        }

        /// <summary>Se da de baja del bus.</summary>
        public void Dispose()
        {
            if (_bus == null)
            {
                return;
            }

            _bus.Unsubscribe(_onStarted);
            _bus.Unsubscribe(_onWon);
            _bus.Unsubscribe(_onFailed);
            _bus.Unsubscribe(_onAbandoned);
            _bus.Unsubscribe(_onEarned);
            _bus.Unsubscribe(_onSpent);
        }

        // Traducción de los eventos de dominio a 08 §8.2 (los nombres exactos y sus parámetros, en el orden de la tabla).

        private void OnLevelStarted(LevelStarted e)
        {
            AnalyticsEvent start = AnalyticsEvent.Create("level_start").Add("level_name", e.LevelName).Add("variant", e.Variant);
            if (e.Band.Length > 0)
            {
                start.Add("band", e.Band); // Q-B7-23 (a) [P]
            }

            Log(start);
        }

        private void OnLevelWon(LevelWon e)
        {
            Log(AnalyticsEvent.Create("af_level_win")
                .Add("pours", e.Pours)
                .Add("par", e.Par)
                .Add("pours_over_par", e.Par > 0 ? e.Pours - e.Par : 0)
                .Add("undo_used", e.UndoUsed)
                .Add("boosters_json", e.BoostersJson)
                .Add("t_ms", e.TimeMs)
                .Add("reward_seeds", e.RewardSeeds)
                .Add("double_harvest", e.DoubleHarvest)
                .Add("rescued", e.Rescued));
            Log(AnalyticsEvent.Create("level_end").Add("success", true).Add("level_name", e.LevelName));
        }

        private void OnLevelFailed(LevelFailed e)
        {
            Log(AnalyticsEvent.Create("af_level_fail")
                .Add("kind", e.Hard ? "hard" : "soft")
                .Add("canoe_lost", e.CanoeLost)
                .Add("streak_broken", e.StreakBroken)
                .Add("shield_used", e.ShieldUsed)
                .Add("relief_served", e.ReliefServed)
                .Add("pours", e.Pours)
                .Add("t_ms", e.TimeMs));
            if (e.Hard)
            {
                // 08 §8.2: level_end en «Victoria, fallo duro o salto»; el fallo blando no termina el nivel.
                Log(AnalyticsEvent.Create("level_end").Add("success", false).Add("level_name", e.LevelName));
            }
        }

        private void OnLevelAbandoned(LevelAbandoned e)
        {
            Log(AnalyticsEvent.Create("af_level_abandon").Add("pours", e.Pours).Add("t_ms", e.TimeMs).Add("reason", e.Reason));
        }

        private void OnSeedsEarned(SeedsEarned e)
        {
            Log(AnalyticsEvent.Create("earn_virtual_currency").Add("virtual_currency_name", "seeds").Add("source", e.Source).Add("value", e.Value));
        }

        private void OnSeedsSpent(SeedsSpent e)
        {
            Log(AnalyticsEvent.Create("spend_virtual_currency").Add("virtual_currency_name", "seeds").Add("item_name", e.ItemName).Add("value", e.Value));
        }
    }
}
