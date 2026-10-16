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
    /// «errores de compra») con crema. Su ancho sale de medir las cifras (revisión de A1: antes se estimaba por dígito, la
    /// placa dejaba unos 18-21 dp vacíos a la derecha y, con la escala de texto, le quitaba ese sitio a la etiqueta del CTA,
    /// que se encogía al 130 %), con el mismo aire a los dos lados: la semilla se dibuja a unos 9 dp del borde izquierdo y la
    /// última cifra queda a <see cref="RightPadDp"/> del derecho. Mide solo cuando cambia la cifra; sin asignaciones al
    /// refrescar si no cambia.
    /// </summary>
    internal sealed class PriceTag
    {
        /// <summary>Alto de la etiqueta (dp).</summary>
        public const float HeightDp = 26f;

        /// <summary>Aire a la derecha de la última cifra (dp): el que deja a la izquierda la semilla, que se dibuja más estrecha que su caja.</summary>
        public const float RightPadDp = 9f;

        // La semilla (caja de 22 dp a 5 dp del borde), el hueco hasta la cifra y el que separa la cifra del precio tachado (dp).
        private const float SeedXDp = 5f;
        private const float SeedDp = 22f;
        private const float SeedGapDp = 3f;
        private const float StrikeGapDp = 4f;
        private const float ValueXDp = SeedXDp + SeedDp + SeedGapDp;

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
            Height = HeightDp * UiBuilder.TextScale;
            Root = UiBuilder.Rect(name, parent);
            _plate = UiBuilder.Sliced("Plate", Root, art.RoundedRect, Palette.UiAmber);
            _plate.pixelsPerUnitMultiplier = 24f / (Height * 0.5f);
            UiBuilder.Stretch(_plate.rectTransform);
            _seed = UiBuilder.Image("Seed", Root, art.IconSeed, Color.white);
            _seed.preserveAspect = true;
            _value = UiBuilder.Label("Value", Root, TextStyle.PlainBold, "0", 16f, Palette.Ink, TextAlignmentOptions.MidlineLeft);
            _strike = UiBuilder.Label("Strike", Root, TextStyle.PlainBold, string.Empty, 12f, Palette.Ink, TextAlignmentOptions.MidlineLeft);
            _strike.fontStyle = FontStyles.Strikethrough;
            _strike.gameObject.SetActive(false);
        }

        public RectTransform Root { get; }

        /// <summary>
        /// Alto de la etiqueta (dp): <see cref="HeightDp"/> por la escala de texto con la que se creó (02 §12: crece con su
        /// cifra; <see cref="UiBuilder.TextScale"/>).
        /// </summary>
        public float Height { get; }

        /// <summary>Ancho que ocupa (dp), según la cifra (y la escala de texto).</summary>
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

            // El ancho de las cifras medido con TextMeshPro (crecen con la escala de texto, y su sitio con ellas), solo aquí, al
            // cambiar la cifra: la placa se ajusta a lo que lleva.
            float valueW = Mathf.Ceil(_value.GetPreferredValues().x);
            float strikeW = strike > 0 ? Mathf.Ceil(_strike.GetPreferredValues().x) : 0f;
            float strikeX = ValueXDp + valueW + StrikeGapDp;
            Width = (strike > 0 ? strikeX + strikeW : ValueXDp + valueW) + RightPadDp;
            UiBuilder.Place(_seed.rectTransform, SeedXDp, (Height - SeedDp) * 0.5f, SeedDp, SeedDp);
            UiBuilder.Place(_value.rectTransform, ValueXDp, 0f, valueW + 2f, Height);
            UiBuilder.Place(_strike.rectTransform, strikeX, 1f, strikeW + 2f, Height);
        }

        /// <summary>Coloca la etiqueta con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="y"/>).</summary>
        public void PlaceAt(float x, float y)
        {
            UiBuilder.Place(Root, x, y, Width, Height);
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

        /// <summary>
        /// Primer alto de la escala de superficies compartidas (dp) [P]: con <see cref="SharedSurfaceStep"/>, los altos son
        /// … 115, 144, 180, 225, 281, 352 … (144 · 1,25ⁿ).
        /// </summary>
        public const float SharedSurfaceBaseDp = 144f;

        /// <summary>Razón entre dos altos seguidos de la escala de superficies compartidas [P].</summary>
        public const float SharedSurfaceStep = 1.25f;

        /// <summary>
        /// Coloca la tarjeta y le pone su superficie: al tamaño exacto o, con <paramref name="sharedSurface"/>, la de alto
        /// <see cref="SharedSurfaceHeight"/>, que la imagen estira hasta el alto del cuerpo. La compartida es para los paneles
        /// cuyo alto cambia con el contenido (la tarjeta previa: en 1-200 tiene 5 o 6 altos distintos): cada superficie es una
        /// textura que se guarda toda la sesión (unos 0,5-0,9 MB de GPU) y cuesta generarla (1,6-3,5 MB de basura la primera
        /// vez), así que con la escala se generan como mucho las de 3 altos. El degradado y el lustre de la superficie son
        /// proporcionales al alto, así que estirarla no los cambia; la veta solo se estira o se encoge en lo que separa el alto
        /// del de la escala (como mucho un 12 %; en las tarjetas de 1-200, un 7 %).
        /// </summary>
        public static void Size(RectTransform panel, Image surface, ProceduralArt art, float x, float y, float w, float h, bool sharedSurface = false)
        {
            UiBuilder.Place(panel, x, y, w, h);
            float surfaceH = h - 16f;
            surface.sprite = art.ModalSurfaceFor(w - 16f, sharedSurface ? SharedSurfaceHeight(surfaceH) : surfaceH);
        }

        /// <summary>
        /// Alto de la superficie compartida para un cuerpo de <paramref name="heightDp"/> dp: el de la escala
        /// <see cref="SharedSurfaceBaseDp"/> · <see cref="SharedSurfaceStep"/>ⁿ más cercano en proporción (redondeado al dp),
        /// así la imagen lo estira o lo encoge como mucho √1,25 ≈ 1,12 veces.
        /// </summary>
        public static float SharedSurfaceHeight(float heightDp)
        {
            float steps = Mathf.Log(Mathf.Max(1f, heightDp) / SharedSurfaceBaseDp) / Mathf.Log(SharedSurfaceStep);
            return Mathf.Round(SharedSurfaceBaseDp * Mathf.Pow(SharedSurfaceStep, Mathf.Round(steps)));
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

        /// <summary>
        /// Botón con relieve y su etiqueta centrada (CTA de la guía 5.1), de <paramref name="size"/> sp, que se encoge lo justo
        /// para caber y nunca por debajo de <see cref="UiBuilder.MinTextSp"/> (02 §12; pregunta 36 (a), D-020; antes, hasta 7 sp
        /// menos con un suelo de 11). Con la escala de texto (<see cref="UiBuilder.TextScale"/>), el botón crece en alto con ella
        /// (07 §7.8: «crecen en alto con la fuente hasta el 130 %»; el alto de 48 dp es mínimo, no fijo) [P]: su relieve se
        /// estira a lo alto por la escala, y su cara y su etiqueta con él (<see cref="FaceHeightOf"/>, <see cref="HeightOf"/>). A
        /// la escala 1, el de siempre.
        /// </summary>
        public static ReliefParts Cta(string name, Transform parent, ReliefSprites sprites, TextStyle style, string text, float size, Action onClick, out TextMeshProUGUI label)
        {
            ReliefParts cta = UiBuilder.Relief(name, parent, sprites, onClick);
            StretchForTextScale(cta);
            label = UiBuilder.Label("Label", cta.Face, style, text, size, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(label.rectTransform, FaceHeightOf(cta));
            UiBuilder.AutoSize(label, UiBuilder.MinTextSp, size);
            cta.Relief.Bind(cta.Face, cta.Base, cta.FaceImage, sprites, null, null, label);
            return cta;
        }

        /// <summary>
        /// Estira a lo alto por la escala de texto (<see cref="UiBuilder.TextScale"/>) el relieve de un botón, como hace
        /// <see cref="Cta"/> (07 §7.8: los CTA «crecen en alto con la fuente hasta el 130 %»); para los CTA que se crean con
        /// <see cref="UiBuilder.Relief"/> y colocan su etiqueta con <see cref="FaceHeightOf"/> (Siguiente y Cosecha doble de la
        /// victoria; tarea L4). A la escala 1, nada cambia.
        /// </summary>
        public static void StretchForTextScale(ReliefParts cta)
        {
            if (UiBuilder.TextScale > 1f)
            {
                StretchTo(cta, UiBuilder.TextScale);
            }
        }

        /// <summary>
        /// Coloca la etiqueta de un CTA de <paramref name="buttonW"/> dp de ancho con <paramref name="insetDp"/> dp ocupados a
        /// los lados (12 + 12 de margen y, en el rescate, su etiqueta de precio) y desplazada <paramref name="shiftX"/> dp (tarea
        /// L4; 02 §12: ningún texto baja de 12 sp ni se recorta, los contenedores crecen): en una línea, como
        /// <see cref="UiBuilder.PlaceCtaLabel(RectTransform, float)"/>, si cabe a su tamaño mínimo; si no, en dos, con el
        /// interlineado apretado en <see cref="CtaTwoLinesShare"/> de la cara, y el botón crece a lo alto lo que haga falta (su
        /// relieve se estira, como con la escala de texto). Quien la llama coloca después el botón con <see cref="Place"/>, que
        /// ya mide el alto nuevo (<see cref="HeightOf"/>). Mide al colocar, nunca por fotograma.
        /// </summary>
        public static void FitCtaLabel(ReliefParts cta, TextMeshProUGUI label, float buttonW, float insetDp, float shiftX)
        {
            float width = buttonW - insetDp;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.lineSpacing = 0f;
            bool two = UiBuilder.MinTextWidth(label) > width + 0.5f;
            float k = Mathf.Max(1f, UiBuilder.TextScale);
            if (two)
            {
                label.textWrappingMode = TextWrappingModes.Normal;
                label.lineSpacing = CtaTwoLinesSpacing;
                float need = Mathf.Ceil(UiBuilder.MinTextHeight(label, width)) + 2f;
                k = Mathf.Max(k, need / (CtaTwoLinesShare * cta.Sprites.FaceHeightDp));
            }

            StretchTo(cta, k);
            float face = FaceHeightOf(cta);
            UiBuilder.PlaceCtaLabel(label.rectTransform, face);
            label.rectTransform.sizeDelta = new Vector2(-insetDp, face * (two ? CtaTwoLinesShare : 0.60f));
            label.rectTransform.anchoredPosition = new Vector2(shiftX, -(face * (two ? 0.5f : 0.56f)));
        }

        /// <summary>Parte de la cara de un CTA que ocupa su etiqueta en dos líneas (<see cref="FitCtaLabel"/>) [P].</summary>
        public const float CtaTwoLinesShare = 0.8f;

        // Interlineado de la etiqueta de un CTA en dos líneas, en centésimas de em (TextMeshPro) [P].
        private const float CtaTwoLinesSpacing = -20f;

        // Estira el relieve del botón a k veces el alto de sus sprites (1: el de siempre).
        private static void StretchTo(ReliefParts cta, float k)
        {
            RectTransform visual = (RectTransform)cta.Face.parent;
            visual.sizeDelta = new Vector2(visual.sizeDelta.x, cta.Sprites.HeightDp * Mathf.Max(1f, k));
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

        /// <summary>
        /// Línea de texto sobre la madera clara (madera_base, 5,1:1; nunca alfa en textos), de <paramref name="size"/> sp, que
        /// pasa a otra línea si no cabe y se encoge lo justo, nunca por debajo de <see cref="UiBuilder.MinTextSp"/> (02 §12;
        /// pregunta 36 (a), D-020; antes, hasta 4 sp menos con un suelo de 11). Se coloca con <see cref="PlaceLine"/>: si ni así
        /// cabe, su caja crece.
        /// </summary>
        public static TextMeshProUGUI Line(string name, Transform parent, string text, float size, Color color, TextStyle style = TextStyle.PlainBold)
        {
            TextMeshProUGUI line = UiBuilder.Label(name, parent, style, text, size, color, TextAlignmentOptions.Center);
            line.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(line, UiBuilder.MinTextSp, size);
            return line;
        }

        /// <summary>
        /// Coloca una línea (<see cref="Line"/>, con su texto ya puesto) con su esquina de arriba a la izquierda en
        /// (<paramref name="x"/>, <paramref name="y"/>) y <paramref name="width"/> dp de ancho, y devuelve el alto que ocupa: el
        /// de siempre (<paramref name="height"/>) por la escala de texto o, si su texto no cabe ahí ni a su tamaño mínimo, el que
        /// necesita a ese mínimo (02 §12: los contenedores crecen; ningún texto baja de 12 sp ni se sale de su caja). Con la
        /// escala 1 y un texto que cabe, <paramref name="height"/>: nada cambia. Si el texto va en dos líneas y la segunda se
        /// queda con una sola palabra («Todo para seguir: semillas, una cuia y canoas / infinitas»), su caja se estrecha, centrada,
        /// hasta la mitad de su ancho en una línea más <see cref="WidowSlackDp"/> (o de <see cref="WidowSlackDp"/> en
        /// <see cref="WidowSlackDp"/> más, mientras siga en dos líneas): las dos quedan equilibradas, sin palabra viuda
        /// (revisión de A1; D-003).
        /// </summary>
        public static float PlaceLine(TextMeshProUGUI line, float x, float y, float width, float height)
        {
            float need = Mathf.Ceil(UiBuilder.MinTextHeight(line, width));
            float h = Mathf.Max(height * UiBuilder.TextScale, need);
            float w = width;
            if (UiBuilder.GreedyLines(line, width, out _, out _, out int lastWords) == 2 && lastWords == 1)
            {
                for (float cut = Mathf.Ceil(UiBuilder.MinTextWidth(line) * 0.5f) + WidowSlackDp; cut < width - 0.01f; cut += WidowSlackDp)
                {
                    if (Mathf.Ceil(UiBuilder.MinTextHeight(line, cut)) <= need + 0.01f)
                    {
                        w = cut;
                        break;
                    }
                }
            }

            UiBuilder.Place(line.rectTransform, x + ((width - w) * 0.5f), y, w, h);
            return h;
        }

        /// <summary>
        /// Holgura de la caja de una línea en dos sin palabra viuda sobre la mitad de su ancho en una (dp) [P]
        /// (<see cref="PlaceLine"/>; revisión de A1).
        /// </summary>
        public const float WidowSlackDp = 8f;

        /// <summary>Coloca un botón con relieve (su área táctil) en el panel.</summary>
        public static void Place(ReliefParts parts, float x, float y, float w)
        {
            UiBuilder.Place((RectTransform)parts.Button.transform, x, y, w, HeightOf(parts));
        }

        /// <summary>Alto que ocupa un botón con relieve colocado con <see cref="Place"/> (con el estirado de <see cref="Cta"/>).</summary>
        public static float HeightOf(ReliefParts parts)
        {
            return Mathf.Max(48f, (parts.Sprites.HeightDp * HeightScaleOf(parts)) - 6f);
        }

        /// <summary>
        /// Alto de la cara de un botón con relieve (dp): el de sus sprites (<see cref="ReliefSprites.FaceHeightDp"/>) y, en un CTA
        /// creado con la escala de texto, por esa escala (<see cref="Cta"/>). Para colocar dentro de la cara su etiqueta, sus
        /// iconos y su precio.
        /// </summary>
        public static float FaceHeightOf(ReliefParts parts)
        {
            return parts.Sprites.FaceHeightDp * HeightScaleOf(parts);
        }

        // Lo que el relieve del botón se estiró a lo alto (1 si no; Cta con la escala de texto).
        private static float HeightScaleOf(ReliefParts parts)
        {
            float h = parts.Sprites.HeightDp;
            float visual = ((RectTransform)parts.Face.parent).sizeDelta.y;
            return h > 0f && visual > h ? visual / h : 1f;
        }
    }
}
