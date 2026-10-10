#nullable enable
using System;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Etiqueta de precio en semillas (07 §7.3: «pill ámbar con icono semilla 20 dp y cifra»; tinta sobre ámbar 5,4:1): la
    /// cifra, opcionalmente el precio sin descuento tachado (04 §6.6: «300 ~600~») y, sin saldo, en rojo aviso (07 §7.2:
    /// «errores de compra») con crema. Sin asignaciones al refrescar si la cifra no cambia.
    /// </summary>
    internal sealed class PriceTag
    {
        private const float HeightDp = 26f;

        private readonly Image _plate;
        private readonly Image _seed;
        private readonly TextMeshProUGUI _value;
        private readonly TextMeshProUGUI _strike;
        private readonly char[] _buf = new char[16];
        private int _shown = int.MinValue;
        private int _strikeShown = int.MinValue;
        private int _missingShown = -1;

        public PriceTag(string name, Transform parent, ProceduralArt art)
        {
            Root = UiBuilder.Rect(name, parent);
            _plate = UiBuilder.Sliced("Plate", Root, art.RoundedRect, Palette.UiAmber);
            _plate.pixelsPerUnitMultiplier = 24f / (HeightDp * 0.5f);
            UiBuilder.Stretch(_plate.rectTransform);
            _seed = UiBuilder.Image("Seed", Root, art.IconSeed, Color.white);
            _seed.preserveAspect = true;
            _value = UiBuilder.Label("Value", Root, TextStyle.PlainBold, "0", 16f, Palette.Ink, TextAlignmentOptions.MidlineLeft);
            _strike = UiBuilder.Label("Strike", Root, TextStyle.PlainBold, string.Empty, 12f, Palette.Ink, TextAlignmentOptions.MidlineLeft);
            _strike.fontStyle = FontStyles.Strikethrough;
            _strike.gameObject.SetActive(false);
        }

        public RectTransform Root { get; }

        /// <summary>Ancho que ocupa (dp), según la cifra.</summary>
        public float Width { get; private set; } = 64f;

        /// <summary>Cifra mostrada.</summary>
        public int Value => _shown;

        /// <summary>Se muestra en rojo por falta de saldo.</summary>
        public bool ShowsMissing => _missingShown == 1;

        /// <summary>Pinta <paramref name="price"/>; con <paramref name="strike"/> &gt; 0, el precio sin descuento tachado.</summary>
        public void Set(int price, int strike, bool missing)
        {
            int missingFlag = missing ? 1 : 0;
            if (price == _shown && strike == _strikeShown && missingFlag == _missingShown)
            {
                return;
            }

            _shown = price;
            _strikeShown = strike;
            _missingShown = missingFlag;
            int n = UiText.WriteNumber(_buf, 0, price, true);
            _value.SetCharArray(_buf, 0, n);
            _plate.color = missing ? (Color)Palette.UiRedWarning : (Color)Palette.UiAmber;
            _value.color = missing ? (Color)Palette.Cream : (Color)Palette.Ink;
            _strike.color = _value.color;
            _strike.gameObject.SetActive(strike > 0);
            if (strike > 0)
            {
                int m = UiText.WriteNumber(_buf, 0, strike, true);
                _strike.SetCharArray(_buf, 0, m);
            }

            float valueW = 9f * n + 4f;
            float strikeW = strike > 0 ? 8f * UiText.WriteNumber(_buf, 0, strike, true) + 6f : 0f;
            Width = 6f + 22f + 2f + valueW + strikeW + 8f;
            UiBuilder.Place(_seed.rectTransform, 5f, 2f, 22f, 22f);
            UiBuilder.Place(_value.rectTransform, 29f, 0f, valueW + 2f, HeightDp);
            UiBuilder.Place(_strike.rectTransform, 29f + valueW, 1f, strikeW, HeightDp);
        }

        /// <summary>Coloca la etiqueta con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="y"/>).</summary>
        public void PlaceAt(float x, float y)
        {
            UiBuilder.Place(Root, x, y, Width, HeightDp);
        }
    }

    /// <summary>
    /// Piezas comunes de los modales del embudo de D-008 y de la pausa (guía visual v2, 5.3; D-003): tarjeta de madera clara
    /// con marco, placa de cabecera, cierre de madera, botones con relieve, enlaces y líneas de texto. Sin oro biselado, sin
    /// corazones rotos ni calaveras (07 §1.3).
    /// </summary>
    internal static class ModalKit
    {
        /// <summary>Lado del cierre (área táctil de 48 dp, 07 §7.8).</summary>
        public const float CloseDp = 48f;

        /// <summary>Velo a pantalla completa que recibe los toques (el tablero detrás no se toca).</summary>
        public static Image Dim(string name, Transform parent, float alpha)
        {
            Image dim = UiBuilder.Image(name, parent, null, new Color(0.04f, 0.06f, 0.05f, alpha), true);
            UiBuilder.Stretch(dim.rectTransform);
            return dim;
        }

        /// <summary>Tarjeta (cuerpo de modal) bajo <paramref name="parent"/>; <paramref name="surface"/> se ajusta al tamaño con <see cref="Size"/>.</summary>
        public static RectTransform Card(string name, Transform parent, ProceduralArt art, out Image surface)
        {
            RectTransform panel = UiBuilder.Rect(name, parent);
            surface = UiBuilder.ModalBody(panel, art);
            return panel;
        }

        /// <summary>Coloca la tarjeta y genera su superficie al tamaño exacto.</summary>
        public static void Size(RectTransform panel, Image surface, ProceduralArt art, float x, float y, float w, float h)
        {
            UiBuilder.Place(panel, x, y, w, h);
            surface.sprite = art.ModalSurfaceFor(w - 16f, h - 16f);
        }

        /// <summary>Lado del sello de la etiqueta en una cabecera con banda (dp).</summary>
        public const float HeaderBadgeDp = 30f;

        /// <summary>
        /// Placa de cabecera con el título (Baloo 2 de 22 dp, mínimo 16): la placa de madera oscura de siempre o, con
        /// <paramref name="band"/>, la banda de ese color (propuesta-mecanicas-dificultad.md §c.1: «cabecera ámbar» o
        /// «índigo» en Difícil y Muy difícil) con el título en tinta o marfil según contraste (<see cref="HeaderTitleColor"/>).
        /// </summary>
        public static TextMeshProUGUI Header(RectTransform panel, ProceduralArt art, string text, out Image plate, Color? band = null)
        {
            plate = UiBuilder.Image("HeaderPlate", panel, band.HasValue ? art.HeaderBand(band.Value) : art.HeaderPlate, Color.white);
            TextMeshProUGUI title = UiBuilder.Label("Title", plate.transform, TextStyle.ModalTitle, text, 22f, HeaderTitleColor(band), TextAlignmentOptions.Center);
            UiBuilder.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(8f, 0f);
            title.rectTransform.offsetMax = new Vector2(-8f, 0f);
            UiBuilder.AutoSize(title, 16f, 22f);
            SetTitleColor(title, HeaderTitleColor(band));
            return title;
        }

        /// <summary>
        /// Banda de la cabecera para <c>difficulty.label</c> (propuesta §c.1; 07 §7.2 y §7.7): <c>ui_ambar</c> en Difícil,
        /// <c>ui_indigo</c> en Muy difícil y null (la madera de siempre) en <c>normal</c>.
        /// </summary>
        public static Color? HeaderBandFor(string? label)
        {
            switch (label)
            {
                case LevelDefinition.LabelHard: return Palette.UiAmber;
                case LevelDefinition.LabelVeryHard: return Palette.UiIndigo;
                default: return null;
            }
        }

        /// <summary>
        /// Color del título sobre la cabecera: crema sobre la madera; sobre una banda, tinta o marfil, el de más contraste
        /// (tinta sobre ámbar 5,4:1; marfil sobre índigo 10,7:1; 07 §7.2).
        /// </summary>
        public static Color HeaderTitleColor(Color? band)
        {
            if (!band.HasValue)
            {
                return Palette.Cream;
            }

            Color ink = Palette.Ink;
            Color ivory = Palette.UiPaper;
            return ColorMath.ContrastRatio(ink, band.Value) >= ColorMath.ContrastRatio(ivory, band.Value) ? ink : ivory;
        }

        /// <summary>Sello de la etiqueta (rana o jaguar) a la izquierda de la cabecera, oculto hasta <see cref="ApplyHeaderLabel"/>.</summary>
        public static Image HeaderBadge(Image plate)
        {
            Image badge = UiBuilder.Image("Badge", plate.transform, null, Color.white); // el sello, perezoso: ApplyHeaderLabel
            badge.preserveAspect = true;
            UiBuilder.Place(badge.rectTransform, 5f, 5f, HeaderBadgeDp, HeaderBadgeDp);
            badge.gameObject.SetActive(false);
            return badge;
        }

        /// <summary>
        /// Viste la cabecera para <c>difficulty.label</c> (propuesta §c.1: «¿Seguro?» y «Nivel fallido» con cabecera ámbar en
        /// Difícil e índigo en Muy difícil; en normal, igual que siempre): la banda, el color del título y el sello de la rana
        /// o del jaguar, para que la etiqueta no dependa solo del color (07 §7.8). Devuelve la banda aplicada (null = madera).
        /// </summary>
        public static Color? ApplyHeaderLabel(Image plate, TextMeshProUGUI title, Image badge, ProceduralArt art, string? label)
        {
            Color? band = HeaderBandFor(label);
            Sprite sprite = band.HasValue ? art.HeaderBand(band.Value) : art.HeaderPlate;
            if (plate.sprite != sprite)
            {
                plate.sprite = sprite;
            }

            SetTitleColor(title, HeaderTitleColor(band));
            bool badged = band.HasValue;
            badge.gameObject.SetActive(badged);
            float inset = badged ? HeaderBadgeDp + 10f : 8f;
            title.rectTransform.offsetMin = new Vector2(inset, 0f);
            title.rectTransform.offsetMax = new Vector2(-inset, 0f);
            if (badged)
            {
                badge.sprite = art.DifficultyIcon(label == LevelDefinition.LabelVeryHard);
            }

            return band;
        }

        // Color del título y su material: la tinta (sobre ámbar) va sin el contorno oscuro del título, que empastaba las letras
        // (revisión de la captura 42); el texto claro, con el contorno y la sombra de siempre (TextStyle.ModalTitle).
        private static void SetTitleColor(TextMeshProUGUI title, Color color)
        {
            title.color = color;
            bool darkText = ColorMath.RelativeLuminance(color) < 0.2f;
            Material? material = darkText ? (title.font != null ? title.font.material : null) : UiFonts.Material(TextStyle.ModalTitle);
            if (material != null && title.fontSharedMaterial != material)
            {
                title.fontSharedMaterial = material;
            }
        }

        /// <summary>Pares texto / fondo de las cabeceras con banda, para la comprobación de contraste ≥ 4,5:1 (02 §12).</summary>
        public static (string Name, Color Text, Color Background)[] HeaderContrastPairs()
        {
            Color amber = Palette.UiAmber;
            Color indigo = Palette.UiIndigo;
            return new[]
            {
                ("cabecera Difícil", HeaderTitleColor(amber), amber),
                ("cabecera Muy difícil", HeaderTitleColor(indigo), indigo),
                ("cabecera normal", HeaderTitleColor(null), (Color)Palette.WoodDark),
            };
        }

        /// <summary>Cierre: disco de madera de 40 dp con la X en crema, en un área de 48 dp.</summary>
        public static ReliefParts Close(string name, Transform parent, ProceduralArt art, Action onClick)
        {
            ReliefParts close = UiBuilder.Relief(name, parent, art.DiscWood, onClick);
            RectTransform visual = (RectTransform)close.Face.parent;
            visual.anchorMin = new Vector2(0.5f, 1f);
            visual.anchorMax = new Vector2(0.5f, 1f);
            visual.sizeDelta = new Vector2(art.DiscWood.Painted ? art.DiscWood.HeightDp : 40f, art.DiscWood.HeightDp);
            visual.anchoredPosition = new Vector2(0f, -4f);
            Image glyph = UiBuilder.Image("Icon", close.Face, art.IconClose, Color.white);
            UiBuilder.Place(glyph.rectTransform, 9f, 9f, 22f, 22f);
            close.Relief.Bind(close.Face, close.Base, close.FaceImage, art.DiscWood, glyph, null, null);
            return close;
        }

        /// <summary>Botón con relieve y su etiqueta centrada (CTA de la guía 5.1).</summary>
        public static ReliefParts Cta(string name, Transform parent, ReliefSprites sprites, TextStyle style, string text, float size, Action onClick, out TextMeshProUGUI label)
        {
            ReliefParts cta = UiBuilder.Relief(name, parent, sprites, onClick);
            label = UiBuilder.Label("Label", cta.Face, style, text, size, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(label.rectTransform, sprites);
            UiBuilder.AutoSize(label, Mathf.Max(11f, size - 7f), size);
            cta.Relief.Bind(cta.Face, cta.Base, cta.FaceImage, sprites, null, null, label);
            return cta;
        }

        /// <summary>Enlace de texto subrayado (pie de modal, 07 §7.5) con un área táctil de 48 dp de alto.</summary>
        public static Button Link(string name, Transform parent, string text, Action onClick, out TextMeshProUGUI label)
        {
            Button link = UiBuilder.BareButton(name, parent, onClick);
            label = UiBuilder.Label("Label", link.transform, TextStyle.PlainBold, text, 16f, Palette.FamilyWood.Base, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Underline;
            UiBuilder.Stretch(label.rectTransform);
            return link;
        }

        /// <summary>Línea de texto sobre la madera clara (madera_base, 5,1:1; nunca alfa en textos).</summary>
        public static TextMeshProUGUI Line(string name, Transform parent, string text, float size, Color color, TextStyle style = TextStyle.PlainBold)
        {
            TextMeshProUGUI line = UiBuilder.Label(name, parent, style, text, size, color, TextAlignmentOptions.Center);
            line.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(line, Mathf.Max(11f, size - 4f), size);
            return line;
        }

        /// <summary>Coloca un botón con relieve (su área táctil) en el panel.</summary>
        public static void Place(ReliefParts parts, float x, float y, float w)
        {
            UiBuilder.Place((RectTransform)parts.Button.transform, x, y, w, Mathf.Max(48f, parts.Sprites.HeightDp - 6f));
        }

        /// <summary>Alto que ocupa un botón con relieve colocado con <see cref="Place"/>.</summary>
        public static float HeightOf(ReliefParts parts)
        {
            return Mathf.Max(48f, parts.Sprites.HeightDp - 6f);
        }
    }
}
