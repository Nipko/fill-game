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

            /// <summary>«Reintentar»: el botón de madera tras un fallo, que vuelve a pedir el borrado (idempotente) [P].</summary>
            public const string Retry = "delete_account_retry";

            /// <summary>«Eliminando tu cuenta y tus datos…»: mientras el servidor borra la cuenta (08 §11), con los puntos de espera [P].</summary>
            public const string Deleting = "delete_account_deleting";

            /// <summary>«Tu cuenta y tus datos se eliminaron.»: la confirmación breve antes de volver a la primera ejecución [P].</summary>
            public const string Done = "delete_account_done";

            /// <summary>Paso 2: cuándo se borra la copia de Google Play Juegos (D-042).</summary>
            public const string CloudNote = "delete_account_cloud_note";

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

        /// <summary>«Reintentar».</summary>
        public static string Retry => Loc.T(Keys.Retry);

        /// <summary>«Eliminando tu cuenta y tus datos…».</summary>
        public static string Deleting => Loc.T(Keys.Deleting);

        /// <summary>«Tu cuenta y tus datos se eliminaron.».</summary>
        public static string Done => Loc.T(Keys.Done);

        /// <summary>Paso 2: «La copia en Google Play Juegos…» (D-042; 08 §11).</summary>
        public static string CloudNote => Loc.T(Keys.CloudNote);

        /// <summary>«Código para borrar tu cuenta en la web: {code}».</summary>
        public static string Code(string code) => Loc.T(Keys.Code, LocArgs.Of("code", CodeGroups(code ?? string.Empty)));

        /// <summary>Caracteres por grupo del código en pantalla (<see cref="CodeGroups"/>).</summary>
        public const int CodeGroupSize = 4;

        /// <summary>
        /// El código de borrado como se enseña (D-033, punto 11: al 130 % sus 26 caracteres seguidos no cabían en una línea de
        /// Ajustes): en grupos de como mucho <see cref="CodeGroupSize"/> caracteres, repartidos lo más igual posible, unidos por
        /// guiones (26 = 4-4-4-4-4-3-3). El guion es el separador del Base32 de Crockford, que lo ignora al decodificar, y deja
        /// que la línea se corte entre grupos sin partir ninguno. No cambia ni añade ningún carácter del código: quitando los
        /// guiones queda el mismo. No es texto traducible (el traductor no toca <c>{code}</c>).
        /// </summary>
        public static string CodeGroups(string code)
        {
            int n = code.Length;
            if (n <= CodeGroupSize)
            {
                return code;
            }

            int groups = (n + CodeGroupSize - 1) / CodeGroupSize;
            int size = n / groups;
            int longer = n % groups;
            var sb = new System.Text.StringBuilder(n + groups - 1);
            int at = 0;
            for (int g = 0; g < groups; g++)
            {
                if (g > 0)
                {
                    sb.Append('-');
                }

                int len = size + (g < longer ? 1 : 0);
                sb.Append(code, at, len);
                at += len;
            }

            return sb.ToString();
        }

        /// <summary>Sin el código (sin red o sin respuesta del servidor).</summary>
        public static string CodeUnavailable => Loc.T(Keys.CodeUnavailable);
    }
}
