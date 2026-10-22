#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;

namespace AF.RemoteConfig.Binding
{
    /// <summary>
    /// Las cifras de juego de B11 leídas de Remote Config (08 §9; nota <c>docs/dev/tutorial-b11.md</c> §3, «Para el integrador»), sin
    /// una clase de cifras propia: <c>drag_pour_enabled</c> (C10, el arrastre; 02 §3.1) y <c>lvl_label_override</c> (el reetiquetado
    /// en vivo; 03 §15, 02 §10), con <c>lvl_labels_from_level</c> y <c>lvl_very_hard_from_level</c> como primer nivel que puede
    /// mostrar Difícil y Muy difícil. Lo llama <see cref="RcConfigBinder.Bind(RcSnapshot, string)"/>
    /// (<see cref="RcConfigs.DragPourEnabled"/> y <see cref="RcConfigs.LevelLabels"/>), como <see cref="ConsentConfigBinder"/>. Un
    /// valor que no se entiende deja el default de 08 §9 y se anota; las entradas de <c>lvl_label_override</c> que el nivel no puede
    /// mostrar se descartan y se anotan (<see cref="LevelLabelOverrides.Problems"/>), sin romper el arranque.
    /// </summary>
    public static class RcGameplayBinder
    {
        /// <summary>Clave de C10 (08 §9: bool, default true).</summary>
        public const string DragPourKey = "drag_pour_enabled";

        /// <summary>Primer nivel que puede mostrar Difícil (08 §9: int, 40).</summary>
        public const string LabelsFromLevelKey = "lvl_labels_from_level";

        /// <summary>Primer nivel que puede mostrar Muy difícil (08 §9: int, 60).</summary>
        public const string VeryHardFromLevelKey = "lvl_very_hard_from_level";

        /// <summary>Las claves que lee, en orden (los nombres exactos de 08 §9).</summary>
        public static readonly IReadOnlyList<string> Keys = new[]
        {
            DragPourKey,
            LevelLabelOverrides.Key,
            LabelsFromLevelKey,
            VeryHardFromLevelKey,
        };

        /// <summary><c>drag_pour_enabled</c> de <paramref name="rc"/>; si no se entiende, el default de 08 §9 (anotado en <paramref name="errors"/>).</summary>
        public static bool DragPourEnabled(RcSnapshot rc, ICollection<string>? errors = null)
        {
            if (rc == null)
            {
                throw new ArgumentNullException(nameof(rc));
            }

            if (rc.TryGet(DragPourKey, out bool enabled))
            {
                return enabled;
            }

            errors?.Add(DragPourKey + ": no es un bool; se usa el default de 08 §9.");
            return DragPour.EnabledDefault;
        }

        /// <summary>
        /// La tabla <c>lvl_label_override</c> de <paramref name="rc"/>, con el primer nivel de cada etiqueta de la misma
        /// <paramref name="rc"/>. Lo que se descarta va a <paramref name="errors"/>; un JSON roto da la tabla vacía (el default
        /// <c>{}</c>).
        /// </summary>
        public static LevelLabelOverrides LevelLabels(RcSnapshot rc, ICollection<string>? errors = null)
        {
            if (rc == null)
            {
                throw new ArgumentNullException(nameof(rc));
            }

            int hardFrom = Int(rc, LabelsFromLevelKey, LevelLabelOverrides.LabelsFromLevelDefault, errors);
            int veryHardFrom = Int(rc, VeryHardFromLevelKey, LevelLabelOverrides.VeryHardFromLevelDefault, errors);
            if (!rc.TryGet(LevelLabelOverrides.Key, out string json))
            {
                errors?.Add(LevelLabelOverrides.Key + ": no se puede leer; se usa el default de 08 §9 ({}).");
                return LevelLabelOverrides.Empty;
            }

            LevelLabelOverrides table = LevelLabelOverrides.Create(json, hardFrom, veryHardFrom);
            if (errors != null)
            {
                for (int i = 0; i < table.Problems.Count; i++)
                {
                    errors.Add(table.Problems[i]);
                }
            }

            return table;
        }

        // Un nivel de 08 §9 (≥ 1); si no se entiende, su default.
        private static int Int(RcSnapshot rc, string key, int fallback, ICollection<string>? errors)
        {
            if (rc.TryGet(key, out int value) && value >= 1)
            {
                return value;
            }

            errors?.Add(key + ": no es un nivel válido; se usa el default de 08 §9 (" + fallback.ToString(System.Globalization.CultureInfo.InvariantCulture) + ").");
            return fallback;
        }
    }
}
