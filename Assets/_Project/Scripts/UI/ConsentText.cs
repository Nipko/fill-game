#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la edad, el consentimiento y la doble confirmación de compras (bloque B12; 08 §2.3 y §7; 05 §7; 06 §2.1, regla 3;
    /// Q-T4 (a)): solo claves, con <see cref="Loc"/>. Las claves nuevas entran en <c>strings.csv</c> con el parche
    /// <c>B12-loc.diff</c> (las registra también en <c>LocTablesTests</c>). Todas son [P] y quedan en <c>borrador</c> con la nota
    /// «pendiente del abogado» (el contrato 32 no tiene un estado para eso: pregunta Q-B12-5); la única literal del GDD es
    /// <see cref="Keys.Blocked"/> («Pide a un adulto que instale el juego», 08 §7 y 05 §7). No es un parcial de <c>UiText</c>
    /// porque los <c>UiText*.cs</c> están reservados (encargo §3.2), como <c>AccountText</c> de B8.
    /// </summary>
    public static class ConsentText
    {
        /// <summary>Claves.</summary>
        public static class Keys
        {
            /// <summary>Título de la pantalla de edad neutral.</summary>
            public const string AgeTitle = "consent_age_title";

            /// <summary>Banda &lt;13.</summary>
            public const string AgeUnder13 = "consent_age_under_13";

            /// <summary>Banda 13-17.</summary>
            public const string AgeTeen = "consent_age_13_17";

            /// <summary>Banda 18+.</summary>
            public const string AgeAdult = "consent_age_18_plus";

            /// <summary>Título de la pantalla de consentimiento.</summary>
            public const string Title = "consent_title";

            /// <summary>Línea bajo el título.</summary>
            public const string Body = "consent_body";

            /// <summary>Finalidad «Anuncios personalizados».</summary>
            public const string PurposeAds = "consent_purpose_ads";

            /// <summary>Finalidad «Analítica».</summary>
            public const string PurposeAnalytics = "consent_purpose_analytics";

            /// <summary>
            /// Línea del diagnóstico (Crashlytics como interés legítimo, 08 §2.3): se puede desactivar en Ajustes (T-L6; D-057, añadido
            /// del 2026-10-27). Sustituye a <c>consent_diagnostics</c> («siempre activos», la línea sin interruptor de Q-B12-4), que sale
            /// de la tabla: una clave no se reutiliza para otro sentido.
            /// </summary>
            public const string Diagnostics = "consent_diagnostics_optional";

            /// <summary>Línea de los anuncios en el EEE, el Reino Unido y Suiza: los decide el aviso de UMP.</summary>
            public const string AdsByUmp = "consent_ads_ump";

            /// <summary>Línea de los anuncios con 13-17: no personalizados.</summary>
            public const string AdsTeen = "consent_ads_teen";

            /// <summary>Línea de los anuncios sin país conocido: no personalizados (Q-T3 (a), el tratamiento más estricto).</summary>
            public const string AdsStrict = "consent_ads_strict";

            /// <summary>Enlace de Ajustes con el perfil <c>ump</c>: vuelve a abrir el mensaje de UMP.</summary>
            public const string AdsUmpOptions = "consent_ads_ump_options";

            /// <summary>«Aceptar todo».</summary>
            public const string AcceptAll = "consent_accept_all";

            /// <summary>«Rechazar todo», al mismo nivel que «Aceptar todo» (05 §7).</summary>
            public const string RejectAll = "consent_reject_all";

            /// <summary>«Guardar mi elección».</summary>
            public const string Save = "consent_save";

            /// <summary>«Pide a un adulto que instale el juego» (08 §7; 05 §7).</summary>
            public const string Blocked = "consent_blocked";

            /// <summary>Fila de Ajustes que abre la pantalla de consentimiento.</summary>
            public const string SettingsPrivacy = "settings_privacy";

            /// <summary>Fila de Ajustes que abre la pantalla de edad (una vez).</summary>
            public const string SettingsAge = "settings_age_correct";

            /// <summary>Fila de Ajustes «Enviar informes de fallos» (T-L6; D-057): interruptor de <c>settings.crash_reports</c> [P].</summary>
            public const string SettingsCrashReports = "settings_crash_reports";

            /// <summary>Título de la segunda confirmación de una compra (13-17).</summary>
            public const string ConfirmTitle = "purchase_confirm_title";

            /// <summary>Línea de la segunda confirmación: <c>{product}</c> y <c>{price}</c>.</summary>
            public const string ConfirmBody = "purchase_confirm_body";

            /// <summary>Botón que compra.</summary>
            public const string ConfirmBuy = "purchase_confirm_buy";

            /// <summary>Botón que no compra.</summary>
            public const string ConfirmCancel = "purchase_confirm_cancel";

            /// <summary>
            /// Título de la confirmación de «Corregir mi edad» (añadido de D-033 del 2026-10-28): neutral e igual para las tres bandas.
            /// </summary>
            public const string AgeConfirmTitle = "consent_age_confirm_title";

            /// <summary>Línea de la confirmación de la edad: <c>{band}</c>, la banda elegida tal como sale en su botón.</summary>
            public const string AgeConfirmBody = "consent_age_confirm_body";

            /// <summary>Botón que guarda la banda elegida (gasta la corrección).</summary>
            public const string AgeConfirmOk = "consent_age_confirm_ok";

            /// <summary>Botón que vuelve a la pantalla de edad sin gastar la corrección.</summary>
            public const string AgeConfirmBack = "consent_age_confirm_back";
        }

        /// <summary>Todas las claves (para <c>LocTablesTests</c>, con el parche <c>B12-loc.diff</c>).</summary>
        public static readonly string[] AllKeys =
        {
            Keys.AgeTitle, Keys.AgeUnder13, Keys.AgeTeen, Keys.AgeAdult, Keys.Title, Keys.Body, Keys.PurposeAds, Keys.PurposeAnalytics,
            Keys.Diagnostics, Keys.AdsByUmp, Keys.AdsTeen, Keys.AdsStrict, Keys.AdsUmpOptions, Keys.AcceptAll, Keys.RejectAll, Keys.Save, Keys.Blocked, Keys.SettingsPrivacy,
            Keys.SettingsAge, Keys.SettingsCrashReports, Keys.ConfirmTitle, Keys.ConfirmBody, Keys.ConfirmBuy, Keys.ConfirmCancel,
            Keys.AgeConfirmTitle, Keys.AgeConfirmBody, Keys.AgeConfirmOk, Keys.AgeConfirmBack,
        };

        /// <summary>Texto de <paramref name="key"/>.</summary>
        public static string T(string key) => Loc.T(key);

        /// <summary>La línea de la segunda confirmación con el nombre del producto y su precio (ya escrito por la tienda).</summary>
        public static string ConfirmBody(string productName, string price)
        {
            return Loc.T(Keys.ConfirmBody, LocArgs.Of("product", productName).And("price", price));
        }

        /// <summary>La etiqueta del botón de <paramref name="band"/> en la pantalla de edad.</summary>
        public static string AgeLabel(AF.Platform.AgeBand band)
        {
            switch (band)
            {
                case AF.Platform.AgeBand.Under13: return Loc.T(Keys.AgeUnder13);
                case AF.Platform.AgeBand.Teen: return Loc.T(Keys.AgeTeen);
                case AF.Platform.AgeBand.Adult: return Loc.T(Keys.AgeAdult);
                default: throw new System.ArgumentOutOfRangeException(nameof(band));
            }
        }

        /// <summary>
        /// La línea de la confirmación de la edad con <paramref name="band"/> tal como sale en la pantalla de edad: la misma frase para
        /// las tres bandas (neutral; añadido de D-033 del 2026-10-28).
        /// </summary>
        public static string AgeConfirmBody(AF.Platform.AgeBand band)
        {
            return Loc.T(Keys.AgeConfirmBody, LocArgs.Of("band", AgeLabel(band)));
        }
    }
}
