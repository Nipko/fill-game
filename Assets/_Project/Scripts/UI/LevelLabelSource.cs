#nullable enable
using System;
using AF.Levels;

namespace AF.UI
{
    /// <summary>
    /// De dónde sale <c>lvl_label_override</c> (08 §9; <see cref="LevelLabelOverrides"/>; tarea B11) al crear el catálogo de niveles:
    /// la fuente local (<see cref="LevelLabelOverrides.LocalSource"/>, <c>{}</c>, vacía en release), como <c>level_overrides</c>
    /// (<c>LaunchOptions.ResolveLevelOverridesJson</c>), hasta que el arranque lea los dos de Remote Config; o la de un test
    /// (<see cref="JsonOverride"/>). Los defaults de <c>lvl_labels_from_level</c> (40) y <c>lvl_very_hard_from_level</c> (60) son
    /// los de 08 §9.
    /// </summary>
    internal static class LevelLabelSource
    {
        /// <summary>Solo tests: el JSON de <c>lvl_label_override</c> de la partida (null = la fuente local).</summary>
        internal static string? JsonOverride { get; set; }

        /// <summary>El JSON de la partida: el del test o la fuente local.</summary>
        public static string ResolveJson()
        {
            return JsonOverride ?? LevelLabelOverrides.LocalSource;
        }

        /// <summary>La tabla de la partida; cada entrada descartada se escribe con <paramref name="warn"/>.</summary>
        public static LevelLabelOverrides Create(Action<string>? warn)
        {
            LevelLabelOverrides table = LevelLabelOverrides.Create(ResolveJson());
            for (int i = 0; i < table.Problems.Count; i++)
            {
                warn?.Invoke(table.Problems[i]);
            }

            return table;
        }
    }
}
