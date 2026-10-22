#nullable enable
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Filas «Reducir movimiento» y «Alto contraste» de Ajustes (bloque B9; 07 §7.8: viven en Ajustes «junto a Patrones de fruta,
    /// Alto contraste, Reducir movimiento y Vibración»; 07 §3, regla 5; 02 §12; D-033, Q-N4 (a): un solo ajuste «Alto contraste»,
    /// con el patrón al 35 %), como la de «Pista por inactividad»: cada una se marca o se desmarca y quien abre la pantalla
    /// (<c>GameBootstrap.ChooseReduceMotion</c> y <c>ChooseHighContrast</c>, parche <c>B9-ui.diff</c>) lo guarda en
    /// <c>settings.reduce_motion</c> y <c>settings.high_contrast</c> [P] (08 §4.3; parche <c>B9-profile.diff</c>) y lo aplica en el
    /// acto. Van tras «Pista por inactividad», en su propio hueco (<see cref="A11ySlot"/>). Parcial nuevo de B9: el parche lo llama
    /// desde el constructor, <c>Layout</c>, <c>EmitLayout</c> y la lista de textos.
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Id de la fila «Reducir movimiento» (sonda) y clave de <c>af_settings_change</c> [P]: <c>reduce_motion</c>.</summary>
        public const string ReduceMotionId = "reduce_motion";

        /// <summary>Id de la fila «Alto contraste» (sonda) y clave de <c>af_settings_change</c> [P]: <c>high_contrast</c>.</summary>
        public const string HighContrastId = "high_contrast";

        /// <summary>Filas de B9 en el hueco (y en la sonda).</summary>
        public const int A11yRowCount = 2;

        private readonly OptionRow?[] _a11yRows = new OptionRow?[A11yRowCount];
        private readonly bool[] _a11yOn = new bool[A11yRowCount];

        /// <summary>Al tocar una fila de B9: su id (<see cref="ReduceMotionId"/> o <see cref="HighContrastId"/>) y el valor nuevo.</summary>
        public Action<string, bool>? A11yToggled { get; set; }

        /// <summary>Hueco de las dos filas.</summary>
        public RectTransform A11ySlot { get; private set; } = null!;

        /// <summary>Botón de la fila <paramref name="id"/>.</summary>
        public Button A11yButton(string id)
        {
            return Row(id).Button;
        }

        /// <summary>Texto de la fila <paramref name="id"/>.</summary>
        public string A11yLabel(string id)
        {
            return Row(id).Label.text;
        }

        /// <summary>Si la fila <paramref name="id"/> lleva la marca (activado).</summary>
        public bool A11yMarked(string id)
        {
            return Row(id).Selected;
        }

        /// <summary>Marca lo que está en vigor (al abrir y tras tocar una fila).</summary>
        public void MarkA11y(bool reduceMotion, bool highContrast)
        {
            _a11yOn[0] = reduceMotion;
            _a11yOn[1] = highContrast;
            for (int i = 0; i < A11yRowCount; i++)
            {
                _a11yRows[i]?.SetSelected(_a11yOn[i]);
            }
        }

        private void BuildA11yRows()
        {
            A11ySlot = UiBuilder.Rect("A11ySlot", _content);
            _a11yRows[0] = Option(A11ySlot, _art, "ReduceMotion", ReduceMotionId, A11yText.SettingsReduceMotion, () => ToggleA11y(0));
            _a11yRows[1] = Option(A11ySlot, _art, "HighContrast", HighContrastId, A11yText.SettingsHighContrast, () => ToggleA11y(1));
            MarkA11y(false, false);
        }

        private void ToggleA11y(int index)
        {
            OptionRow? row = _a11yRows[index];
            if (row != null)
            {
                A11yToggled?.Invoke(row.Id, !_a11yOn[index]);
            }
        }

        // Las dos filas en el contenido desde y (cada una en la lista de filas del corte del visor); devuelve la y de lo que sigue.
        private float LayoutA11y(float y, float inner, float rowH, float s)
        {
            float slotH = (A11yRowCount * (rowH + RowGapDp)) - RowGapDp;
            UiBuilder.Place(A11ySlot, 16f, y, inner, slotH);
            for (int i = 0; i < A11yRowCount; i++)
            {
                float top = i * (rowH + RowGapDp);
                _a11yRows[i]!.Place(0f, top, inner, rowH, s);
                _rowTops.Add(y + top);
                _rowHeights.Add(rowH);
            }

            return y + slotH + RowGapDp;
        }

        private IEnumerable<TextMeshProUGUI> A11yTexts()
        {
            for (int i = 0; i < A11yRowCount; i++)
            {
                yield return _a11yRows[i]!.Label;
            }
        }

        private void A11yLayoutIds(string[] ids, RectTransform[] rects, int k)
        {
            for (int i = 0; i < A11yRowCount; i++, k++)
            {
                ids[k] = _a11yRows[i]!.Id;
                rects[k] = _a11yRows[i]!.Root;
            }
        }

        private OptionRow Row(string id)
        {
            switch (id)
            {
                case ReduceMotionId: return _a11yRows[0]!;
                case HighContrastId: return _a11yRows[1]!;
                default: throw new ArgumentOutOfRangeException(nameof(id), "Fila de accesibilidad desconocida: " + id);
            }
        }
    }
}
