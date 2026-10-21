#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de «Eliminar cuenta y datos» (bloque B8; 08 §11, fila «Eliminación de cuenta»: Ajustes › «Eliminar cuenta y datos»
    /// con confirmación en dos pasos y aviso de que las compras no se reembolsan): solo claves, con <see cref="Loc"/>. Las claves
    /// nuevas aún <b>no</b> están en <c>strings.csv</c>: van en <c>docs/dev/traspaso/spark-enganches/B8-strings.csv</c> y las
    /// copia el integrador. No es un parcial de <c>UiText</c> porque los <c>UiText*.cs</c> están reservados (encargo §3.2), como
    /// <c>SkipText</c>. Clave ausente: «[clave]» en desarrollo y «» en release (<see cref="Loc.T(string)"/>), sin excepciones.
    /// </summary>
    public static class AccountText
    {
        /// <summary>Claves (las nuevas, en <c>B8-strings.csv</c>).</summary>
        public static class Keys
        {
            /// <summary>«Eliminar cuenta y datos»: la fila de Ajustes y el título del primer paso (texto de 08 §11).</summary>
            public const string DeleteAccount = "settings_delete_account";

            /// <summary>Lo que se borra (paso 1) [P].</summary>
            public const string Body = "delete_account_body";

            /// <summary>«Las compras no se reembolsan.» (aviso de 08 §11) [P].</summary>
            public const string NoRefund = "delete_account_no_refund";

            /// <summary>«Conservar mis datos»: el botón principal del paso 1, que cierra sin borrar [P].</summary>
            public const string Keep = "delete_account_keep";

            /// <summary>«Continuar»: pasa al segundo paso [P].</summary>
            public const string Continue = "delete_account_continue";

            /// <summary>«¿Eliminar todo?»: título del segundo paso [P].</summary>
            public const string ConfirmTitle = "delete_account_confirm_title";

            /// <summary>Lo que pasa al confirmar (paso 2) [P].</summary>
            public const string ConfirmBody = "delete_account_confirm_body";

            /// <summary>«Eliminar»: el botón que borra (paso 2) [P].</summary>
            public const string ConfirmCta = "delete_account_confirm_cta";

            /// <summary>«No se pudo eliminar. Inténtalo de nuevo.»: si el borrado local falla [P].</summary>
            public const string Failed = "delete_account_failed";

            /// <summary>
            /// «Código para borrar tu cuenta en la web: {code}» (F3-5; 08 §11; Q-B13-21) [P]: en Ajustes, encima de «Eliminar cuenta y
            /// datos», en lugar del UID. <c>{code}</c> = los 26 caracteres del código (Base32 de Crockford), que no se traducen.
            /// </summary>
            public const string Code = "settings_account_code";

            /// <summary>«Sin conexión: el código para borrar tu cuenta en la web aún no está disponible» (F3-5) [P].</summary>
            public const string CodeUnavailable = "settings_account_code_unavailable";
        }

        /// <summary>«Eliminar cuenta y datos».</summary>
        public static string DeleteAccount => Loc.T(Keys.DeleteAccount);

        /// <summary>Lo que se borra.</summary>
        public static string Body => Loc.T(Keys.Body);

        /// <summary>«Las compras no se reembolsan.»</summary>
        public static string NoRefund => Loc.T(Keys.NoRefund);

        /// <summary>«Conservar mis datos».</summary>
        public static string Keep => Loc.T(Keys.Keep);

        /// <summary>«Continuar».</summary>
        public static string Continue => Loc.T(Keys.Continue);

        /// <summary>«¿Eliminar todo?».</summary>
        public static string ConfirmTitle => Loc.T(Keys.ConfirmTitle);

        /// <summary>Lo que pasa al confirmar.</summary>
        public static string ConfirmBody => Loc.T(Keys.ConfirmBody);

        /// <summary>«Eliminar».</summary>
        public static string ConfirmCta => Loc.T(Keys.ConfirmCta);

        /// <summary>«No se pudo eliminar. Inténtalo de nuevo.»</summary>
        public static string Failed => Loc.T(Keys.Failed);

        /// <summary>«Código para borrar tu cuenta en la web: {code}».</summary>
        public static string Code(string code) => Loc.T(Keys.Code, LocArgs.Of("code", code ?? string.Empty));

        /// <summary>Sin el código (sin red o sin respuesta del servidor).</summary>
        public static string CodeUnavailable => Loc.T(Keys.CodeUnavailable);
    }
}
