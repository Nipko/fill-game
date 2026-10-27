#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Ads;
using AF.Analytics;
using AF.Analytics.Firebase;
using AF.IAP;
using AF.Platform;
using AF.RemoteConfig.Binding;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Edad y consentimiento en el arranque y en Ajustes (bloque B12; 08 §2.3 y §7; 05 §7; 06 §2.1, regla 3; Q-T3 (a) y Q-T4 (a)
    /// de D-033; nota <c>docs/dev/consentimiento.md</c>). Parcial nuevo; el parche <c>B12-ui.diff</c> añade sus llamadas en
    /// <c>GameBootstrap.cs</c>:
    /// <list type="number">
    /// <item><see cref="BlockedAtBoot"/>, con el arte listo y antes de crear la cartera de juego, los anuncios y la tienda: con la
    /// banda &lt;13 guardada, corta la pantalla de arranque, enseña «Pide a un adulto que instale el juego» y el arranque se para
    /// (no se crea sesión de juego, 08 §7).</item>
    /// <item><see cref="WrapConsentAds"/> y <see cref="WrapConsentStore"/> envuelven el proveedor de anuncios y la tienda: sin
    /// decisión, MAX sin inicializar y sin compras; con 13-17, la doble confirmación.</item>
    /// <item><see cref="SetUpConsent"/>, con Remote Config enlazado: las cifras (<see cref="ConsentConfigBinder"/>), el país
    /// (SIM → región → ninguno) y su perfil legal, el flujo y la pantalla.</item>
    /// <item><see cref="StartConsentAtBoot"/> (y, si hay pantalla, <see cref="WaitConsentAtBoot"/>), tras cortar la pantalla de arranque y antes de <c>AppFlow.Start</c>: la edad y el
    /// consentimiento si hacen falta, y los flags a la analítica (<c>Analytics.SetEnabled</c>, «siempre ANTES de GameFlow.Go»), a
    /// los anuncios y a la tienda. Con &lt;13, <see cref="ConsentBlocked"/> y la app no arranca.</item>
    /// <item>En Ajustes (<see cref="RefreshConsentRows"/>): «Privacidad y datos» y «Corregir mi edad» (una vez).</item>
    /// </list>
    /// Con <see cref="ConsentEnabled"/> apagado (los tests de siempre, <c>PlayModeHelpers</c>), nada de esto actúa y todo queda
    /// como antes de B12: la analítica sin recoger y los anuncios y la tienda tal cual.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private ConsentConfig? _consentConfig;
        private ConsentFlow? _consentFlow;
        private ConsentScreen? _consentScreen;
        private ConsentAdService? _consentAds;
        private ConsentRecord? _appliedConsent;
        private bool _consentApplied;
        private ConsentStoreService? _consentStore;
        private string? _consentCountry;
        private CountrySource _consentCountrySource;

        /// <summary>Interruptor de los tests (null = encendido). <see cref="ResetConsentOverrides"/>.</summary>
        internal static bool? ConsentEnabledOverride { get; set; }

        /// <summary>Almacén del consentimiento para los tests (por defecto, <c>profile.consent</c>).</summary>
        internal static IConsentStore? ConsentStoreOverride { get; set; }

        /// <summary>SIM para los tests (por defecto, la del teléfono; en el Editor, ninguna).</summary>
        internal static ISimCountry? SimCountryOverride { get; set; }

        /// <summary>La edad y el consentimiento actúan (<see cref="ConsentEnabledOverride"/> o, sin él, siempre).</summary>
        public static bool ConsentEnabled => ConsentEnabledOverride ?? true;

        /// <summary>El flujo del consentimiento (null sin él).</summary>
        public ConsentFlow? Consent => _consentFlow;

        /// <summary>La pantalla de la edad y el consentimiento (null hasta crearla).</summary>
        internal ConsentScreen? ConsentView => _consentScreen;

        /// <summary>El proveedor de anuncios con los flags (null sin el consentimiento).</summary>
        public ConsentAdService? ConsentAds => _consentAds;

        /// <summary>La tienda con los flags (null sin el consentimiento).</summary>
        public ConsentStoreService? ConsentStore => _consentStore;

        /// <summary>La banda &lt;13 bloquea el juego: «Pide a un adulto que instale el juego» y nada más.</summary>
        public bool ConsentBlocked { get; private set; }

        /// <summary>El país del perfil legal (null si no se supo) y de dónde salió.</summary>
        public string? ConsentCountryCode => _consentCountry;

        /// <summary>Limpia los ganchos de test del consentimiento.</summary>
        internal static void ResetConsentOverrides()
        {
            ConsentEnabledOverride = null;
            ConsentStoreOverride = null;
            CrashReportingOverride = null;
            AnalyticsResetOverride = null;
            SimCountryOverride = null;
            AdProviderOverride = null; // D-042: el proveedor de anuncios de los tests
        }

        // false con la banda <13 guardada (StartsDataServicesAtBoot): el arranque no enciende Crashlytics, no descarga Remote Config,
        // no crea el sumidero de Firebase Analytics ni fusiona con la nube. Se decide con el perfil leído, antes de iniciar ningún SDK.
        private bool _dataServicesAtBoot = true;

        /// <summary>
        /// El arranque puede encender lo que recoge o descarga (Crashlytics, la descarga de Remote Config, Firebase Analytics y la
        /// nube): sin banda guardada, 13-17 y 18+, sí; con &lt;13 guardado, no (B12; 08 §7). Sin el consentimiento en marcha (los tests de
        /// siempre), sí. Lee <see cref="ConsentStoreOverride"/> si lo hay, como <see cref="BlockedAtBoot"/>.
        /// </summary>
        private static bool DataServicesAllowedAtBoot(Profile profile)
        {
            if (!ConsentEnabled)
            {
                return true;
            }

            ConsentRecord? record = ConsentStoreOverride != null ? ConsentStoreOverride.Load() : profile.Consent;
            return ConsentPolicy.StartsDataServicesAtBoot(record);
        }

        /// <summary>
        /// Con la banda &lt;13 guardada en <paramref name="store"/>, corta la pantalla de arranque y enseña el bloqueo; devuelve true
        /// y quien arranca se para. Necesita el arte (<c>_art</c>) y los textos.
        /// </summary>
        private bool BlockedAtBoot(IConsentStore store)
        {
            if (!ConsentEnabled || _art == null)
            {
                return false;
            }

            IConsentStore actual = ConsentStoreOverride ?? store;
            ConsentRecord? record = actual.Load();
            if (!ConsentPolicy.IsBlocked(record))
            {
                return false;
            }

            ConsentBlocked = true;
            Under13Firebase(); // F3-6: Crashlytics apagado y la analítica sin recoger, de forma explícita (persisten entre arranques)
            CancelNotificationsForBlock(); // Q-M5: con <13, ningún aviso local pendiente ni en la bandeja (08 §7)
            CutSplash();
            _consentScreen ??= new ConsentScreen(transform, _art);
            _consentScreen.ShowBlocked();
            DevProbe.ConsentState("blocked", ConsentPolicy.Flags(record, ConsentConfig.Defaults), record!.Profile, null, CountrySource.None);
            return true;
        }

        /// <summary>
        /// Tests de los enganches con Firebase (F3-6): recibe true al encender Crashlytics y false al apagarlo, en lugar de
        /// <see cref="CrashReporting"/>.
        /// </summary>
        internal static Action<bool>? CrashReportingOverride { get; set; }

        /// <summary>Tests: sustituye a <see cref="FirebaseAnalyticsSink.ResetData"/> al eliminar la cuenta (F3-6).</summary>
        internal static Action? AnalyticsResetOverride { get; set; }

        /// <summary>Crashlytics encendido o apagado (F3-6; en el editor, nada salvo el gancho de los tests).</summary>
        private static void SetCrashReporting(bool on)
        {
            if (CrashReportingOverride != null)
            {
                CrashReportingOverride(on);
            }
            else if (on)
            {
                CrashReporting.Start();
            }
            else
            {
                CrashReporting.Stop();
            }
        }

        /// <summary>Los datos de analítica del dispositivo, borrados (08 §13.3; F3-6), al eliminar la cuenta.</summary>
        private static void ResetAnalyticsData()
        {
            if (AnalyticsResetOverride != null)
            {
                AnalyticsResetOverride();
            }
            else
            {
                FirebaseAnalyticsSink.ResetData();
            }
        }

        // Banda < 13 (08 §7): la analítica nunca se enciende (la recogida a false también en Firebase, que la recuerda entre
        // arranques) y Crashlytics se apaga de forma explícita, porque IsCrashlyticsCollectionEnabled también persiste. En el arranque
        // con <13 guardado no hay sumidero de Firebase (no se toca Analytics) y CrashReporting.Stop lo apaga sin arrancar el Firebase
        // de C# ni App Check (lo nunca encendido sigue apagado y lo que dejara encendido un arranque anterior se corrige).
        private void Under13Firebase()
        {
            _analytics?.SetCollectionEnabled(false);
            SetCrashReporting(false);
        }

        /// <summary>Envuelve los anuncios con los flags del consentimiento (sin él, tal cual).</summary>
        private IAdService WrapConsentAds(IAdService inner)
        {
            if (!ConsentEnabled)
            {
                return inner;
            }

            _consentAds = new ConsentAdService(inner);
            return _consentAds;
        }

        /// <summary>Envuelve la tienda con los flags del consentimiento (sin él, tal cual).</summary>
        private IStoreService WrapConsentStore(IStoreService inner)
        {
            if (!ConsentEnabled)
            {
                return inner;
            }

            _consentStore = new ConsentStoreService(inner);
            return _consentStore;
        }

        /// <summary>
        /// Las cifras, el país y su perfil legal, el flujo y la pantalla (con Remote Config ya activado y el arte listo). El registro
        /// se guarda en <paramref name="store"/> (<c>profile.consent</c>).
        /// </summary>
        private void SetUpConsent(IClock clock, IConsentStore store)
        {
            if (!ConsentEnabled || _art == null)
            {
                return;
            }

            // Las cifras ya enlazadas con el resto (RcConfigBinder; sus errores ya salieron en SetUpTelemetry): una sola fuente.
            _consentConfig = _rcConfigs?.Consent ?? ConsentConfig.Defaults;

            _consentCountry = ConsentCountry.Resolve(SimCountryOverride ?? AndroidSimCountry.ForPlatform(), _deviceLocale, out _consentCountrySource);
            string profile = _consentConfig.ProfileFor(_consentCountry);
            _consentFlow = new ConsentFlow(_consentConfig, ConsentStoreOverride ?? store, clock, profile, countryKnown: _consentCountry != null, country: _consentCountry)
            {
                Reported = ReportConsent,
            };
            _consentScreen ??= new ConsentScreen(transform, _art);
            if (_consentStore != null)
            {
                _consentStore.Confirmation = _consentScreen; // Q-T4 (a): la segunda confirmación con 13-17
                _consentStore.Precheck = (product, request) =>
                    BillingPrecheck(product) ?? _storeLedger?.CheckPurchasable(product, request); // nunca confirmar lo imposible (ni sin precio de Play, D-040)
            }
        }

        /// <summary>
        /// La edad y el consentimiento del arranque (antes de <c>AppFlow.Start</c>): con una decisión vigente (o con el bloqueo)
        /// aplica los flags en el acto y devuelve false; si hace falta preguntar, enseña la pantalla y devuelve true, y quien arranca
        /// espera con <see cref="WaitConsentAtBoot"/>. Así, sin pantalla, el arranque no gasta ningún fotograma de más. Sin el
        /// consentimiento en marcha, false sin hacer nada.
        /// </summary>
        private bool StartConsentAtBoot()
        {
            ConsentFlow? flow = _consentFlow;
            if (!ConsentEnabled || flow == null)
            {
                return false;
            }

            ConsentStep step = flow.StartBoot();
            if (step == ConsentStep.Age || step == ConsentStep.Choices)
            {
                ShowConsentStep();
                ProbeConsent();
                return true;
            }

            FinishConsentAtBoot();
            return false;
        }

        /// <summary>Espera la decisión de la pantalla abierta por <see cref="StartConsentAtBoot"/> y aplica los flags.</summary>
        private IEnumerator WaitConsentAtBoot()
        {
            ConsentFlow? flow = _consentFlow;
            while (flow != null && (flow.Step == ConsentStep.Age || flow.Step == ConsentStep.Choices))
            {
                yield return null;
            }

            FinishConsentAtBoot();
        }

        // Los flags (siempre antes de AppFlow.Start) y, con <13, el bloqueo: ConsentBlocked y la app no arranca.
        private void FinishConsentAtBoot()
        {
            ConsentFlow? flow = _consentFlow;
            if (flow == null)
            {
                return;
            }

            ApplyConsentFlags();
            if (flow.Step == ConsentStep.Blocked)
            {
                BlockSession();
                return;
            }

            _consentScreen?.Hide();
            ProbeConsent();
            StartPlayBilling(false); // D-040: con la edad ya decidida (y no < 13), Play puede conectarse
            StartCloudAfterConsent(flow.Flags.PlayGamesAllowed); // D-042: primer arranque, Play Games tras declarar la edad (no < 13)
        }

        // <13 (en el arranque o al corregir la edad en Ajustes): el bloqueo a la vista y la escena se recarga, como al cambiar de idioma
        // (RebuildUi). El arranque siguiente lee la banda guardada y se para en BlockedAtBoot antes de crear el juego: sin sesión, sin
        // audio, sin avisos locales al pasar a segundo plano (08 §7: «no se crea sesión de juego»).
        private void BlockSession()
        {
            ConsentBlocked = true;
            StopCloudForBlock(); // D-042: sin Play Games con < 13
            Under13Firebase(); // F3-6 (ApplyConsentFlags ya apagó la analítica; Crashlytics, aquí)
            CancelNotificationsForBlock(); // Q-M5: los avisos locales programados en otra salida, fuera (08 §7)
            _consentScreen?.ShowBlocked();
            ProbeConsent();
            RebuildUi(); // el perfil en memoria viaja al arranque siguiente (también si no se pudo guardar: perfil en solo lectura)
        }

        /// <summary>Atrás de Android con la pantalla abierta: la consume (y la cierra si se puede). False si no estaba abierta.</summary>
        private bool ConsentBack()
        {
            if (_consentScreen == null || !_consentScreen.Visible)
            {
                return false;
            }

            _consentScreen.PressBack();
            return true;
        }

        /// <summary>Las filas de «Privacidad» en Ajustes, al abrirla: con una decisión vigente; la edad, si aún se puede corregir.</summary>
        private void RefreshConsentRows()
        {
            SettingsScreen? settings = _settingsScreen;
            if (settings == null)
            {
                return;
            }

            ConsentFlow? flow = _consentFlow;
            bool privacy = ConsentEnabled && flow != null && flow.Flags.Decided;
            settings.PrivacyPressed = () => OpenPrivacy();
            settings.AgePressed = () => OpenAgeCorrection();
            settings.ShowConsentRows(privacy, privacy && flow!.CanCorrectAge);
        }

        /// <summary>Ajustes › «Privacidad y datos»: las finalidades con lo guardado marcado.</summary>
        internal bool OpenPrivacy()
        {
            ConsentFlow? flow = _consentFlow;
            if (flow == null || _consentScreen == null || _consentScreen.Visible || !flow.Flags.Decided)
            {
                return false;
            }

            flow.BeginChange();
            ShowConsentStep();
            ProbeConsent();
            return true;
        }

        /// <summary>Ajustes › «Corregir mi edad» (una vez).</summary>
        internal bool OpenAgeCorrection()
        {
            ConsentFlow? flow = _consentFlow;
            if (flow == null || _consentScreen == null || _consentScreen.Visible || !flow.CanCorrectAge)
            {
                return false;
            }

            flow.BeginAgeCorrection();
            ShowConsentStep();
            ProbeConsent();
            return true;
        }

        // Pinta el paso del flujo. En el arranque, sin X; desde Ajustes, con ella.
        private void ShowConsentStep()
        {
            ConsentFlow? flow = _consentFlow;
            ConsentScreen? screen = _consentScreen;
            if (flow == null || screen == null)
            {
                return;
            }

            bool fromSettings = flow.Mode != ConsentFlowMode.Boot;
            switch (flow.Step)
            {
                case ConsentStep.Age:
                    screen.ShowAge(OnAgeChosen, fromSettings ? CloseConsentFromSettings : (System.Action?)null);
                    break;
                case ConsentStep.Choices:
                    screen.ShowChoices(AdsLineFor(flow), flow.InitialAnalytics, flow.InitialAdsPersonalized, OnConsentAnswer,
                        fromSettings ? CloseConsentFromSettings : (System.Action?)null, fromSettings ? OpenAdsPrivacyOptions : (System.Action?)null,
                        flow.OffersAnalyticsChoice); // D-057: sin «Analítica» con 13-17 donde la edad digital del país pasa de 13
                    break;
                case ConsentStep.Blocked:
                    screen.ShowBlocked();
                    break;
                default:
                    screen.Hide();
                    break;
            }

            EmitConsentLayout();
            _skipNextDelta = true;
        }

        // La línea dice lo mismo que los flags: el interruptor si se ofrece; UMP si lo decide UMP (ConsentFlow.AdsByUmp, también con
        // 13-17 sin biz_under18_no_personalization); sin país, la estricta; con 13-17, la de la edad.
        private static ConsentAdsLine AdsLineFor(ConsentFlow flow)
        {
            if (flow.OffersAdsChoice)
            {
                return ConsentAdsLine.Toggle;
            }

            if (flow.AdsByUmp)
            {
                return ConsentAdsLine.Ump;
            }

            if (!flow.CountryKnown)
            {
                return ConsentAdsLine.Strict;
            }

            return flow.Band == AgeBand.Teen ? ConsentAdsLine.Teen : ConsentAdsLine.Strict;
        }

        /// <summary>
        /// La edad o el consentimiento están pendientes (sin decisión vigente) o el juego está bloqueado: la sesión no cuenta y no se programan avisos locales
        /// al pasar a segundo plano (parche <c>B12-ui.diff</c> en <c>GameBootstrap.Sessions.cs</c>).
        /// </summary>
        internal bool ConsentHoldsSession
        {
            get
            {
                if (ConsentBlocked)
                {
                    return true;
                }

                // Sugerencia 1 de la revisión de B12 (nota §6), al integrar: sin decisión vigente, también durante el segundo de la
                // pantalla de arranque de quien debe volver a decidir (textos nuevos u otro perfil legal), con el flujo aún en None.
                ConsentFlow? flow = _consentFlow;
                return ConsentEnabled && flow != null && !flow.Flags.Decided;
            }
        }

        // Ajustes con el perfil ump: vuelve a abrir el mensaje de UMP (MAX) para cambiar o retirar el consentimiento de los anuncios.
        private void OpenAdsPrivacyOptions()
        {
            if (_consentAds != null && _consentAds.ShowPrivacyOptions())
            {
                _probe?.SettingsChange(SettingsScreen.PrivacyId, "ump_options", null);
            }
        }

        private void OnAgeChosen(AgeBand band)
        {
            ConsentFlow? flow = _consentFlow;
            if (flow == null || flow.Step != ConsentStep.Age)
            {
                return;
            }

            ConsentFlowMode mode = flow.Mode;
            AgeBand before = flow.Record?.AgeBand ?? AgeBand.Unknown;
            ConsentStep step = flow.ChooseAge(band);
            if (mode != ConsentFlowMode.Boot)
            {
                AfterSettingsChange(step, band != before ? "age" : null);
                return;
            }

            ShowConsentStep();
            ProbeConsent();
        }

        private void OnConsentAnswer(ConsentAnswer answer, bool analytics, bool ads)
        {
            ConsentFlow? flow = _consentFlow;
            if (flow == null || flow.Step != ConsentStep.Choices)
            {
                return;
            }

            ConsentFlowMode mode = flow.Mode;
            ConsentStep step = flow.Choose(answer, analytics, ads);
            if (mode != ConsentFlowMode.Boot)
            {
                AfterSettingsChange(step, "choices");
                return;
            }

            ShowConsentStep();
            ProbeConsent();
        }

        private void CloseConsentFromSettings()
        {
            ConsentFlow? flow = _consentFlow;
            if (flow != null && flow.Mode != ConsentFlowMode.Boot && flow.Step != ConsentStep.Blocked)
            {
                flow.Cancel();
            }

            _consentScreen?.Hide();
            ProbeConsent();
            _skipNextDelta = true;
        }

        // Tras decidir desde Ajustes: los flags nuevos, af_settings_change (key = consent) y las filas; con <13, el bloqueo y la escena
        // se recarga sin sesión de juego (BlockSession).
        private void AfterSettingsChange(ConsentStep step, string? changed)
        {
            ApplyConsentFlags();
            if (changed != null)
            {
                _analytics?.Log(AnalyticsEvent.Create("af_settings_change").Add("key", SettingsScreen.PrivacyId).Add("value", changed));
                _probe?.SettingsChange(SettingsScreen.PrivacyId, changed, null);
            }

            if (step == ConsentStep.Blocked)
            {
                BlockSession();
                return;
            }

            _consentScreen?.Hide();
            RefreshConsentRows();
            ProbeConsent();
            _skipNextDelta = true;
        }

        /// <summary>Los flags vigentes a la analítica (siempre antes de <c>AppFlow.Start</c>), a los anuncios y a la tienda.</summary>
        private void ApplyConsentFlags()
        {
            ConsentFlow? flow = _consentFlow;
            if (flow == null)
            {
                return;
            }

            // Una vez por decisión: ReportConsent ya los aplica antes del evento y el cierre del paso no los repite.
            if (_consentApplied && ReferenceEquals(_appliedConsent, flow.Record))
            {
                return;
            }

            _consentApplied = true;
            _appliedConsent = flow.Record;
            ConsentFlags flags = flow.Flags;
            _analytics?.SetCollectionEnabled(flags.AnalyticsEnabled);
            _consentAds?.Apply(flags);
            _consentStore?.Apply(flags);
            if (flags.Decided && flags.Profile != null)
            {
                _analytics?.SetUserProperty("consent_profile", flags.Profile); // 08 §8.1
            }
        }

        // af_consent (08 §8.2). Los flags se aplican antes: «accepted» ya llega a los sumideros que piden consentimiento; «rejected» y
        // «shown» del arranque, solo a los locales.
        private void ReportConsent(ConsentReport report)
        {
            if (report.Step != ConsentReport.Shown)
            {
                ApplyConsentFlags();
            }

            AnalyticsService? analytics = _analytics;
            if (analytics == null)
            {
                return;
            }

            AnalyticsEvent e = AnalyticsEvent.Create("af_consent")
                .Add("profile", report.Profile)
                .Add("text_version", report.TextVersion)
                .Add("analytics", report.Analytics)
                .Add("ads_personalized", report.AdsPersonalized)
                .Add("step", report.Step);
            string? band = AgeBands.Id(report.Band);
            if (band != null)
            {
                e.Add("age_band", band);
            }

            analytics.Log(e);
        }

        private void ProbeConsent()
        {
            ConsentFlow? flow = _consentFlow;
            if (flow == null)
            {
                return;
            }

            DevProbe.ConsentState(StepKey(flow.Step), flow.Flags, flow.Profile, _consentCountry, _consentCountrySource);
        }

        private void EmitConsentLayout()
        {
            ConsentScreen? screen = _consentScreen;
            if (!DevProbe.Enabled || _probe == null || screen == null || !screen.Visible)
            {
                return;
            }

            var ids = new List<string>();
            var rects = new List<RectTransform>();
            screen.VisibleButtons(ids, rects);
            _probe.Layout("consent", 0, null, ids.ToArray(), rects.ToArray());
        }

        private static string StepKey(ConsentStep step)
        {
            switch (step)
            {
                case ConsentStep.Age: return "age";
                case ConsentStep.Choices: return "choices";
                case ConsentStep.Blocked: return "blocked";
                case ConsentStep.Done: return "done";
                default: return "none";
            }
        }
    }
}
