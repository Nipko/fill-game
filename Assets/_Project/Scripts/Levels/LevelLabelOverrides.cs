#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Mechanics;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>
    /// Tabla <c>lvl_label_override</c> (08 §9: json <c>{nivel: label}</c>, default <c>{}</c>, dueño 03: «Reetiquetado por intentos
    /// medios en vivo (dirección 5)»; 02 §10, fila «Honestidad»: «etiqueta recalibrada en vivo con intentos medios por nivel»;
    /// tarea B11). Sin motor; inmutable.
    /// </summary>
    /// <remarks>
    /// <para>La etiqueta es la que se muestra y la que paga (03 §7: «La banda coloca; la etiqueta se muestra y paga»; 02 §10,
    /// «Recompensa»), así que la aplica el catálogo al servir el nivel (<see cref="Apply"/>): el chip del HUD, la tarjeta previa,
    /// la cinta de «Siguiente» y la Cosecha leen la etiqueta servida, sin saber si vino del paquete o de esta tabla.</para>
    /// <para>Cada entrada pide <c>normal</c>, <c>hard</c> o <c>very_hard</c> (los valores de <c>difficulty.label</c>, 08 §4.1).
    /// Una etiqueta que el nivel no puede mostrar no se aplica (08 §9: <c>lvl_labels_from_level</c> = 40 y
    /// <c>lvl_very_hard_from_level</c> = 60 son el «primer nivel que puede mostrar Difícil / Muy difícil»): <c>hard</c> antes de
    /// <c>lvl_labels_from_level</c> y <c>very_hard</c> antes de <c>lvl_very_hard_from_level</c> se descartan; <c>normal</c> vale en
    /// cualquier nivel (quita la etiqueta). Una entrada inválida (nivel que no es un entero positivo, valor que no es una de las
    /// tres etiquetas, nivel repetido o etiqueta antes de su nivel) no rompe el arranque: se descarta y se anota en
    /// <see cref="Problems"/>, como en <see cref="LevelOverrides"/>.</para>
    /// <para>Fuente: <see cref="LocalSource"/>, vacía en release, como <c>level_overrides</c> hasta que el arranque lea los dos de
    /// Remote Config; los tests pasan la suya.</para>
    /// <para>Kill switch (<c>level_overrides</c>, KS1): la etiqueta se aplica al nivel servido, sea el publicado o su sustituto.</para>
    /// </remarks>
    public sealed class LevelLabelOverrides
    {
        /// <summary>Clave de Remote Config (08 §9).</summary>
        public const string Key = "lvl_label_override";

        /// <summary>Fuente local: el default de 08 §9 (<c>{}</c>), vacía en release.</summary>
        public const string LocalSource = "{}";

        /// <summary>Default de <c>lvl_labels_from_level</c> (08 §9): primer nivel que puede mostrar Difícil.</summary>
        public const int LabelsFromLevelDefault = 40;

        /// <summary>Default de <c>lvl_very_hard_from_level</c> (08 §9): primer nivel que puede mostrar Muy difícil.</summary>
        public const int VeryHardFromLevelDefault = 60;

        private static readonly string[] NoProblems = new string[0];

        private readonly int[] _levels;
        private readonly string[] _labels;
        private readonly string[] _problems;

        private LevelLabelOverrides(int[] levels, string[] labels, string[] problems)
        {
            _levels = levels;
            _labels = labels;
            _problems = problems;
        }

        /// <summary>Tabla vacía (el default <c>{}</c> de 08 §9).</summary>
        public static LevelLabelOverrides Empty { get; } = new LevelLabelOverrides(new int[0], new string[0], NoProblems);

        /// <summary>Número de niveles reetiquetados.</summary>
        public int Count => _levels.Length;

        /// <summary>Niveles reetiquetados, de menor a mayor.</summary>
        public IReadOnlyList<int> Levels => _levels;

        /// <summary>Entradas descartadas al leer la tabla, una por línea de registro (vacío si no hay).</summary>
        public IReadOnlyList<string> Problems => _problems;

        /// <summary>
        /// Lee <paramref name="json"/> (<c>{"57": "hard"}</c>; null o vacío = <c>{}</c>) con los defaults de 08 §9 para el primer
        /// nivel de cada etiqueta.
        /// </summary>
        public static LevelLabelOverrides Create(string? json)
        {
            return Create(json, LabelsFromLevelDefault, VeryHardFromLevelDefault);
        }

        /// <summary>
        /// Lee <paramref name="json"/> con <paramref name="labelsFromLevel"/> (<c>lvl_labels_from_level</c>) y
        /// <paramref name="veryHardFromLevel"/> (<c>lvl_very_hard_from_level</c>). Un JSON roto o que no es un objeto da la tabla
        /// vacía con el problema anotado.
        /// </summary>
        public static LevelLabelOverrides Create(string? json, int labelsFromLevel, int veryHardFromLevel)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Empty;
            }

            JsonValue root;
            try
            {
                root = JsonParser.Parse(json!);
            }
            catch (FormatException e)
            {
                return new LevelLabelOverrides(new int[0], new string[0], new[] { Key + ": no es JSON válido (" + e.Message + "); se ignora." });
            }

            if (root.Kind != JsonKind.Object)
            {
                return new LevelLabelOverrides(new int[0], new string[0], new[] { Key + ": se esperaba un objeto {nivel: label} y hay " + root.Kind + "; se ignora." });
            }

            var levels = new List<int>();
            var labels = new List<string>();
            var problems = new List<string>();
            IReadOnlyList<KeyValuePair<string, JsonValue>> members = root.Members;
            for (int i = 0; i < members.Count; i++)
            {
                string levelText = members[i].Key;
                JsonValue value = members[i].Value;
                if (!TryParseLevel(levelText, out int level))
                {
                    problems.Add(Key + ": el nivel \"" + levelText + "\" no es un número de nivel positivo; se ignora.");
                    continue;
                }

                if (levels.Contains(level))
                {
                    problems.Add(Key + ": el nivel " + levelText + " está repetido; vale el primero.");
                    continue;
                }

                string? label = value.Kind == JsonKind.String ? CanonicalLabel(value.AsString()) : null;
                if (label == null)
                {
                    problems.Add(Key + ": el nivel " + levelText + " no lleva normal, hard ni very_hard; se ignora.");
                    continue;
                }

                if (string.Equals(label, LevelDefinition.LabelHard, StringComparison.Ordinal) && level < labelsFromLevel)
                {
                    problems.Add(Key + ": el nivel " + levelText + " no puede mostrar Difícil (lvl_labels_from_level = "
                        + labelsFromLevel.ToString(CultureInfo.InvariantCulture) + "); se ignora.");
                    continue;
                }

                if (string.Equals(label, LevelDefinition.LabelVeryHard, StringComparison.Ordinal) && level < veryHardFromLevel)
                {
                    problems.Add(Key + ": el nivel " + levelText + " no puede mostrar Muy difícil (lvl_very_hard_from_level = "
                        + veryHardFromLevel.ToString(CultureInfo.InvariantCulture) + "); se ignora.");
                    continue;
                }

                levels.Add(level);
                labels.Add(label);
            }

            int[] levelArray = levels.ToArray();
            string[] labelArray = labels.ToArray();
            Array.Sort(levelArray, labelArray);
            return new LevelLabelOverrides(levelArray, labelArray, problems.Count == 0 ? NoProblems : problems.ToArray());
        }

        /// <summary>Etiqueta pedida para el nivel de campaña <paramref name="level"/>. Sin asignaciones.</summary>
        public bool TryGet(int level, out string label)
        {
            int i = Array.BinarySearch(_levels, level);
            if (i < 0)
            {
                label = LevelDefinition.LabelNormal;
                return false;
            }

            label = _labels[i];
            return true;
        }

        /// <summary>
        /// Los niveles servidos <paramref name="served"/> (con su número de campaña en <paramref name="numbers"/>, en el mismo
        /// orden) con la etiqueta de esta tabla. Si ninguno cambia, devuelve el mismo array; si no, una copia con los niveles
        /// reetiquetados (<see cref="Relabel"/>). Solo al crear el catálogo.
        /// </summary>
        /// <exception cref="ArgumentException">Los dos arrays no tienen la misma longitud.</exception>
        public LevelDefinition[] Apply(LevelDefinition[] served, int[] numbers)
        {
            if (served == null)
            {
                throw new ArgumentNullException(nameof(served));
            }

            if (numbers == null || numbers.Length != served.Length)
            {
                throw new ArgumentException("served y numbers deben tener la misma longitud.", nameof(numbers));
            }

            LevelDefinition[]? result = null;
            for (int i = 0; i < served.Length; i++)
            {
                if (!TryGet(numbers[i], out string label) || string.Equals(served[i].DifficultyLabel, label, StringComparison.Ordinal))
                {
                    continue;
                }

                result ??= (LevelDefinition[])served.Clone();
                result[i] = Relabel(served[i], label);
            }

            return result ?? served;
        }

        /// <summary>
        /// Copia de <paramref name="level"/> con la etiqueta <paramref name="label"/>: el mismo tablero, las mismas variantes de
        /// alivio y los mismos datos del paquete; solo cambia <see cref="LevelDefinition.DifficultyLabel"/>.
        /// </summary>
        public static LevelDefinition Relabel(LevelDefinition level, string label)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            string canonical = CanonicalLabel(label) ?? throw new ArgumentException("Etiqueta desconocida: " + label, nameof(label));
            int count = level.CuiaCount;
            var caps = new int[count];
            var cuias = new int[count][];
            var attrs = new MechanicSpec[count];
            for (int i = 0; i < count; i++)
            {
                caps[i] = level.CapacityOf(i);
                cuias[i] = level.CopyDoses(i);
                attrs[i] = level.Attrs[i];
            }

            var palette = new string[level.Palette.Count];
            for (int i = 0; i < palette.Length; i++)
            {
                palette[i] = level.Palette[i];
            }

            var hidden = new int[level.Hidden.Count][];
            for (int i = 0; i < hidden.Length; i++)
            {
                hidden[i] = (int[])level.Hidden[i].Clone();
            }

            var relief = new ReliefBoard[level.Relief.Count];
            for (int i = 0; i < relief.Length; i++)
            {
                relief[i] = level.Relief[i];
            }

            return new LevelDefinition(
                level.Id,
                level.Level,
                level.World,
                level.WaterBase,
                palette,
                level.Units,
                caps,
                cuias,
                attrs,
                level.HasHiddenDoses,
                hidden.Length == 0 ? null : hidden,
                level.Par,
                canonical,
                relief,
                level.IntroMechanic,
                level.Skippable,
                level.HintSupported,
                level.Band,
                level.ParApprox);
        }

        // Una de las tres etiquetas de difficulty.label (08 §4.1), con la cadena canónica; null si no lo es.
        private static string? CanonicalLabel(string value)
        {
            if (string.Equals(value, LevelDefinition.LabelNormal, StringComparison.Ordinal))
            {
                return LevelDefinition.LabelNormal;
            }

            if (string.Equals(value, LevelDefinition.LabelHard, StringComparison.Ordinal))
            {
                return LevelDefinition.LabelHard;
            }

            return string.Equals(value, LevelDefinition.LabelVeryHard, StringComparison.Ordinal) ? LevelDefinition.LabelVeryHard : null;
        }

        private static bool TryParseLevel(string text, out int level)
        {
            level = 0;
            if (text.Length == 0 || text.Length > 9)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                {
                    return false;
                }
            }

            level = int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);
            return level > 0;
        }
    }
}
