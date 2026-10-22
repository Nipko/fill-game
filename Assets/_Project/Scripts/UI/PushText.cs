#nullable enable
using AF.LiveOps;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la tarjeta del pre-prompt de push y de la fila «Avisos» de Ajustes (06 §8.1; nota de B6,
    /// <c>docs/dev/sesiones-notificaciones.md</c> §3 y §6): solo claves, con <see cref="Loc"/>. Las del pre-prompt
    /// (<see cref="PrePromptRequest.BodyKey"/>, <see cref="PrePromptRequest.AcceptKey"/> y <see cref="PrePromptRequest.DeclineKey"/>) y
    /// <see cref="Keys.Row"/> / <see cref="Keys.Blocked"/> llegaron con B6; las demás son de esta tarea, [P] y en <c>borrador</c>. No es
    /// un parcial de <c>UiText</c> (los <c>UiText*.cs</c> están reservados), como <see cref="ConsentText"/> y <see cref="AccountText"/>.
    /// </summary>
    public static class PushText
    {
        /// <summary>Claves.</summary>
        public static class Keys
        {
            /// <summary>«Avisos»: la fila de Ajustes (B6).</summary>
            public const string Row = "settings_notifications";

            /// <summary>Estado bajo la fila con los avisos encendidos [P].</summary>
            public const string StateOn = "settings_notifications_on";

            /// <summary>Estado bajo la fila con los avisos apagados [P].</summary>
            public const string StateOff = "settings_notifications_off";

            /// <summary>Línea bajo la fila con los avisos bloqueados en el teléfono (B6).</summary>
            public const string Blocked = "settings_notifications_blocked";

            /// <summary>Enlace que abre los ajustes de notificaciones de la app en el teléfono [P].</summary>
            public const string OpenPhone = "settings_notifications_open_phone";

            /// <summary>Etiqueta (y anuncio) de TalkBack de la fila con los avisos encendidos [P].</summary>
            public const string A11yOn = "a11y_notifications_on";

            /// <summary>Etiqueta (y anuncio) de TalkBack de la fila con los avisos apagados [P].</summary>
            public const string A11yOff = "a11y_notifications_off";

            /// <summary>Etiqueta (y anuncio) de TalkBack de la fila con los avisos bloqueados en el teléfono [P].</summary>
            public const string A11yBlocked = "a11y_notifications_blocked";
        }

        /// <summary>Las claves que pinta o dice la UI de los avisos (para <c>LocTablesTests</c>), sin las del pre-prompt de B6.</summary>
        public static readonly string[] AllKeys =
        {
            Keys.Row, Keys.StateOn, Keys.StateOff, Keys.Blocked, Keys.OpenPhone, Keys.A11yOn, Keys.A11yOff, Keys.A11yBlocked,
        };

        /// <summary>Texto de TiTino en la tarjeta (06 §8.1).</summary>
        public static string PrePromptBody => Loc.T(PrePromptRequest.BodyKey);

        /// <summary>«Avísame».</summary>
        public static string PrePromptAccept => Loc.T(PrePromptRequest.AcceptKey);

        /// <summary>«Ahora no».</summary>
        public static string PrePromptDecline => Loc.T(PrePromptRequest.DeclineKey);

        /// <summary>«Avisos».</summary>
        public static string Row => Loc.T(Keys.Row);

        /// <summary>«Abrir ajustes del teléfono».</summary>
        public static string OpenPhone => Loc.T(Keys.OpenPhone);

        /// <summary>La línea visible bajo la fila en <paramref name="state"/> (vacía si la fila no se ve).</summary>
        public static string State(PushRowState state)
        {
            switch (state)
            {
                case PushRowState.On: return Loc.T(Keys.StateOn);
                case PushRowState.Off: return Loc.T(Keys.StateOff);
                case PushRowState.Blocked: return Loc.T(Keys.Blocked);
                default: return string.Empty;
            }
        }

        /// <summary>La etiqueta de TalkBack de la fila (y lo que se anuncia al cambiar) en <paramref name="state"/>.</summary>
        public static string A11yLabel(PushRowState state)
        {
            switch (state)
            {
                case PushRowState.On: return Loc.T(Keys.A11yOn);
                case PushRowState.Blocked: return Loc.T(Keys.A11yBlocked);
                case PushRowState.Off: return Loc.T(Keys.A11yOff);
                default: return Loc.T(Keys.Row);
            }
        }
    }
}
