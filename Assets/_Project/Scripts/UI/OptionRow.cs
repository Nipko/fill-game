#nullable enable
using System;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Una opción que se marca o se desmarca, común a Ajustes (<see cref="SettingsScreen"/>: idiomas, patrones, audio, avisos) y a
    /// la pantalla de consentimiento (<see cref="ConsentScreen"/>: las finalidades), extraída al integrar B12 (Q-B12-14 (a); D-033,
    /// añadido de 2026-10-21): placa crema con borde de madera, o de hoja si está elegida, con su marca de radio a la izquierda
    /// (anillo, hueco y punto: el estado no depende solo del color, 07 §7.8) y el texto. Toda la fila es el área táctil (≥ 48 dp).
    /// </summary>
    internal sealed class OptionRow
    {
        // Placa de una opción no elegida: crema (más clara que la tarjeta, la levanta; la madera oscura encima, 11,5:1) con un
        // borde opaco de 2 dp de la madera base #7E5638 (5,1:1 sobre la madera clara de la tarjeta, ≥ 3:1 de WCAG 1.4.11 para
        // el límite de un componente), para que se lea como botón y no como texto suelto (revisión de L3: el papel solo se
        // separaba de la tarjeta un 3 % de luminancia y el primer borde, la madera oscura al 25 %, daba 1,56:1). Opaco y no con
        // alfa: el proyecto está en Linear y la GPU mezcla en lineal (ColorMath.CompositeLinear), así que la madera oscura al
        // 60 % quedaría en 2,2:1 en pantalla aunque en sRGB dé 3,3:1.
        public static readonly Color Plate = Palette.Cream;
        public static readonly Color Border = Palette.FamilyWood.Base;
        public const float BorderDp = 2f;
        public const float RadiusDp = 12f;
        public const float RadioDp = 20f;

        private readonly Image _border;
        private readonly Image _plate;
        private readonly Image _ring;
        private readonly Image _hole;
        private readonly Image _dot;
        private readonly float _labelSp;
        private readonly float _labelMinSp;
        private Action? _onClick;

        /// <param name="labelSp">Cuerpo del texto a escala 1 (Ajustes: <see cref="SettingsScreen.OptionSp"/>).</param>
        /// <param name="labelMinSp">Cuerpo mínimo del autoajuste.</param>
        /// <param name="wrap">El texto parte en líneas (las finalidades del consentimiento son frases largas).</param>
        public OptionRow(Transform parent, ProceduralArt art, string name, string id, string text, Action? onClick, float labelSp, float labelMinSp,
            bool wrap = false)
        {
            Id = id;
            _onClick = onClick;
            _labelSp = labelSp;
            _labelMinSp = labelMinSp;
            Button = UiBuilder.BareButton(name, parent, () => _onClick?.Invoke());
            Root = (RectTransform)Button.transform;
            _border = UiBuilder.Sliced("Border", Root, art.RoundedRect, Border);
            UiBuilder.Stretch(_border.rectTransform);
            _plate = UiBuilder.Sliced("Plate", Root, art.RoundedRect, Plate);
            UiBuilder.Stretch(_plate.rectTransform);
            _border.pixelsPerUnitMultiplier = 24f / RadiusDp;
            _ring = Circle("Ring", art, RadioDp);
            _hole = Circle("Hole", art, RadioDp - 6f);
            _dot = Circle("Dot", art, 10f);
            Label = UiBuilder.Label("Label", Root, TextStyle.PlainBold, text, labelSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            if (wrap)
            {
                Label.textWrappingMode = TextWrappingModes.Normal;
            }

            UiBuilder.AutoSize(Label, labelMinSp, labelSp);
            SetSelected(false);
            A11yLabels.Set(Button, () => Label.text, () => Selected); // TalkBack: su texto y si está marcada (B9; 02 §12)
        }

        public string Id { get; private set; }

        public Button Button { get; }

        public RectTransform Root { get; }

        public TextMeshProUGUI Label { get; }

        public bool Selected { get; private set; }

        public void Rebind(string id, string text, Action onClick)
        {
            Id = id;
            Label.text = text;
            _onClick = onClick;
        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            Color plate = selected ? Palette.FamilyLeaf.Deep : Plate;
            Color ink = selected ? (Color)Palette.Cream : Palette.FamilyWood.Deep;
            _plate.color = plate;
            Label.color = ink;
            _ring.color = ink;
            _hole.color = plate;
            _dot.color = ink;
            _dot.gameObject.SetActive(selected);

            // La elegida, verde llena hasta el borde y sin él; las demás, la placa dentro del borde.
            float inset = selected ? 0f : BorderDp;
            _border.gameObject.SetActive(!selected);
            _plate.rectTransform.offsetMin = new Vector2(inset, inset);
            _plate.rectTransform.offsetMax = new Vector2(-inset, -inset);
            _plate.pixelsPerUnitMultiplier = 24f / (RadiusDp - inset);
        }

        /// <summary>Coloca la fila en (<paramref name="x"/>, <paramref name="y"/>) con el texto a la escala <paramref name="s"/>.</summary>
        public void Place(float x, float y, float w, float h, float s)
        {
            UiBuilder.Place(Root, x, y, w, h);
            float r = h * 0.5f;
            Center(_ring.rectTransform, 14f + (RadioDp * 0.5f), r, RadioDp);
            Center(_hole.rectTransform, 14f + (RadioDp * 0.5f), r, RadioDp - 6f);
            Center(_dot.rectTransform, 14f + (RadioDp * 0.5f), r, 10f);
            Label.fontSizeMax = _labelSp * s;
            Label.fontSizeMin = _labelMinSp;
            UiBuilder.Place(Label.rectTransform, 14f + RadioDp + 12f, 0f, w - (14f + RadioDp + 12f) - 12f, h);
        }

        private Image Circle(string name, ProceduralArt art, float d)
        {
            Image img = UiBuilder.Sliced(name, Root, art.RoundedRect, Color.white);
            img.pixelsPerUnitMultiplier = 24f / (d * 0.5f);
            return img;
        }

        private static void Center(RectTransform rt, float cx, float cy, float d)
        {
            UiBuilder.Place(rt, cx - (d * 0.5f), cy - (d * 0.5f), d, d);
        }
    }
}
