#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;

namespace AF.RemoteConfig.Binding
{
    /// <summary>
    /// <see cref="A11yConfig"/> leída de Remote Config (bloque B9; 08 §9), como las tablas de <see cref="RcConfigBinder"/>: sin
    /// reflexión, cada clave con su asignación; un valor que no se entiende deja el default de esa propiedad y se anota; si la clase
    /// entera no pasa su <c>Validate()</c> (el factor fuera de 0,3-0,7), se usan sus defaults (Q-B7-7). Lo llama
    /// <see cref="RcConfigBinder.Bind(RcSnapshot, string)"/> (<c>RcConfigs.A11y</c>), que publica sus claves en
    /// <see cref="RcConfigBinder.BoundKeys"/>.
    /// </summary>
    public static class A11yConfigBinder
    {
        /// <summary>Las claves que lee, en orden (los nombres exactos de 08 §9).</summary>
        public static readonly IReadOnlyList<string> Keys = new[]
        {
            "ui_reduce_motion_default",
            "anim_reduce_motion_factor",
        };

        /// <summary><see cref="A11yConfig"/> con los valores <paramref name="rc"/>; los problemas, en <paramref name="errors"/>.</summary>
        public static A11yConfig Bind(RcSnapshot rc, ICollection<string>? errors = null)
        {
            if (rc == null)
            {
                throw new ArgumentNullException(nameof(rc));
            }

            var c = new A11yConfig();
            Try(errors, "ui_reduce_motion_default", () => c.UiReduceMotionDefault = rc.Get<bool>("ui_reduce_motion_default"));
            Try(errors, "anim_reduce_motion_factor", () => c.AnimReduceMotionFactor = rc.Get<double>("anim_reduce_motion_factor"));
            try
            {
                c.Validate();
                return c;
            }
            catch (InvalidOperationException ex)
            {
                errors?.Add(nameof(A11yConfig) + ": " + ex.Message + " Se usan los defaults de 08 §9.");
                return A11yConfig.Defaults;
            }
        }

        private static void Try(ICollection<string>? errors, string key, Action apply)
        {
            try
            {
                apply();
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException || ex is ArgumentException)
            {
                errors?.Add(key + ": " + ex.Message); // se queda el default de esa propiedad
            }
        }
    }
}
