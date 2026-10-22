#nullable enable
using System;
using AF.Levels;

namespace AF.UI
{
    /// <summary>
    /// De dónde sale <c>lvl_label_override</c> (08 §9; <see cref="LevelLabelOverrides"/>; tarea B11) al crear el catálogo de niveles:
    /// la de un test (<see cref="JsonOverride"/>); si no, la de Remote Config del arranque (<c>RcConfigs.LevelLabels</c>, leída por
    /// <c>RcGameplayBinder</c> con <c>lvl_labels_from_level</c> y <c>lvl_very_hard_from_level</c>; cierre de B11), que ya anotó lo
    /// que descartó; y, sin Remote Config (una pantalla sola en un test), la fuente local (<see cref="LevelLabelOverrides.LocalSource"/>,
    /// <c>{}</c>) con los defaults de 08 §9 (40 y 60).
    /// </summary>
    internal static class LevelLabelSource
    {
        /// <summary>Solo tests: el JSON de <c>lvl_label_override</c> de la partida (null = Remote Config o la fuente local).</summary>
        internal static string? JsonOverride { get; set; }

        /// <summary>
        /// La tabla de la partida: la del test o la fuente local, con cada entrada descartada escrita con <paramref name="warn"/>; o la
        /// de <paramref name="rc"/> (Remote Config, cuyas entradas descartadas ya salieron en los avisos de Remote Config).
        /// </summary>
        public static LevelLabelOverrides Create(AF.RemoteConfig.Binding.RcConfigs? rc, Action<string>? warn)
        {
            if (JsonOverride == null && rc != null)
            {
                return rc.LevelLabels;
            }

            LevelLabelOverrides table = LevelLabelOverrides.Create(JsonOverride ?? LevelLabelOverrides.LocalSource);
            for (int i = 0; i < table.Problems.Count; i++)
            {
                warn?.Invoke(table.Problems[i]);
            }

            return table;
        }
    }
}
