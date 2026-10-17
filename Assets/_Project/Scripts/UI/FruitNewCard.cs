#nullable enable
using System.Collections.Generic;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Tarjeta «Fruta nueva» (06 §4.5; propuesta de Frutas §5.1 y paso F5 de §10; tarea F5): en el estreno de cada tinte
    /// (<see cref="SightingRule.FruitNewAt"/>: 2, 4, 7, 11, 21, 41, 61, 81, 111, 141 y 201 en 1-300), después de la intro y cuando la
    /// tropa se ha ido de la rama, lleva lo que pide 06 §4.5: el icono de la fruta (la fruta en vuelo del tinte,
    /// <see cref="ProceduralArt.FruitFlight"/>, con su PNG <c>fruta_&lt;tinte&gt;</c> si existe) sobre un disco, el nombre en el
    /// idioma del jugador (<c>tint.&lt;tinte&gt;.name</c>), la muestra de color con el patrón y el icono de accesibilidad
    /// (<see cref="ProceduralArt.TintSwatch"/>) y una línea, la frase de su serie de Frutas del río (<c>fact.&lt;tinte&gt;.tag</c>,
    /// por <see cref="Loc.Fact(string)"/>; D-017). Es de la familia de la tarjeta «¡Miquito a la vista!» (07 §6.4; la de
    /// <see cref="SightingCard"/>): la misma placa de madera clara con su canto, el mismo sitio (sobre la franja del panorama si
    /// cabe; si no, sobre el muelle, por encima de la bandeja y de sus burbujas) y la misma cola de avisos (GameScreen.Notices.cs:
    /// tipo FruitNew, con la prioridad tutorial &gt; Fruta nueva &gt; miquito &gt; rótulo, como mucho una tarjeta por nivel). En los
    /// tableros de 3 filas, donde no cabe en ninguno de los dos (el 111 y el 201 a 360 × 780 dp; con la escala de texto, también
    /// alguno de 2 filas), va como placa de una línea clavada al marco bajo el HUD, como el rótulo del lugar
    /// (<see cref="PlaceAsPlate"/>) [P]; si ni así cabe a 12 sp (el 111 y el 201 a 360 × 780 dp al 130 %), no sale y la sonda
    /// <c>AF_STATE fruit_new</c> lo cuenta con <c>mode</c> none [P].
    /// <list type="bullet">
    /// <item><b>Duración:</b> la de lectura de D-011 (<see cref="NoticeTiming.ReadMs(string, string, string)"/> con el nombre y la
    /// frase, 3-7 s) [P], en lugar de los 2 s de 06 §4.5, como los demás avisos del tablero; fundido de entrada y salida.</item>
    /// <item><b>Texto:</b> ninguno baja de 12 sp (02 §12; pregunta 36 (a), D-020; A1): el nombre a 15 sp (se encoge hasta 12 si no
    /// cabe con su muestra) y la frase a 13 sp, que pasa a una segunda línea si no cabe, y la tarjeta crece (<see cref="Measure"/>);
    /// con la escala de texto (<see cref="UiBuilder.TextScale"/>) crecen las filas.</item>
    /// <item><b>Icono:</b> sobre un disco propio a la resolución de la UI (<see cref="ProceduralArt.FruitNewDisc"/>) del color que
    /// le da contraste (<see cref="DiscFor"/>: la crema de <see cref="FactLine.IconBack"/> para los iconos oscuros, como el açaí o
    /// la hoja del Río Negro, y la madera de <see cref="Palette.FamilyWood"/> para los claros, como la guanábana, el copoazú o el
    /// huito; con los iconos procedurales, todos a ≥ 3:1), con un contorno de 1,5 dp de tinta
    /// #2B2118 a alfa 0,45 (D-003, principio 3: contraste local) [P].</item>
    /// <item><b>Frase:</b> sin una palabra sola en la última línea: si queda viuda, el último espacio se muestra como espacio
    /// duro (U+00A0) y la palabra baja con su compañera (<see cref="ShownTag"/>; la tabla de textos no cambia) [P].</item>
    /// <item><b>Toques:</b> ninguno (<c>raycastTarget</c> = false y <see cref="CanvasGroup.blocksRaycasts"/> = false); no tapa cuias
    /// ni botones.</item>
    /// <item><b>Textos:</b> si falta el nombre o la frase en el idioma de la UI (o, en release, sin aprobar del asesor), no sale:
    /// nunca mezcla idiomas (D-018, Q10-A; <see cref="TryTexts"/>).</item>
    /// </list>
    /// Sin confeti. Los nombres y las frases quedan pendientes del asesor cultural (07 §8.2).
    /// </summary>
    internal sealed class FruitNewCard
    {
        /// <summary>Alto mínimo de la tarjeta a la escala de texto 1 (dp), como la del miquito (<see cref="SightingCard.HeightDp"/>).</summary>
        public const float HeightDp = SightingCard.HeightDp;

        /// <summary>Lado del icono de la fruta (dp), como el retrato de la tarjeta del miquito.</summary>
        public const float IconDp = SightingCard.PortraitDp;

        /// <summary>El canto de madera asoma este alto bajo la placa (dp), como en la tarjeta del miquito.</summary>
        public const float EdgeDp = SightingCard.EdgeDp;

        /// <summary>Lado de la muestra del tinte junto al nombre (dp, por la escala de texto) [P].</summary>
        public const float SwatchDp = 18f;

        /// <summary>Del nombre a la muestra (dp) [P].</summary>
        public const float SwatchGapDp = 6f;

        /// <summary>Nombre del tinte: Nunito Bold de 15 sp, como el título de la tarjeta del miquito.</summary>
        public const float NameSp = 15f;

        /// <summary>Frase de «Fruta nueva»: Nunito Regular de 13 sp [P] (≥ 12 sp; 02 §12).</summary>
        public const float TagSp = 13f;

        /// <summary>Con la frase en una línea, la tarjeta se encoge al ancho de su texto más esto a su derecha (dp) [P].</summary>
        public const float ShrunkRightPadDp = SightingCard.ShrunkRightPadDp;

        /// <summary>Frase en la placa de una línea (<see cref="PlaceAsPlate"/>): 14 sp, como la del rótulo del lugar [P].</summary>
        public const float PlateTagSp = StopLabel.PlateTextSp;

        /// <summary>Entre el icono, la frase y la muestra en la placa (dp) [P].</summary>
        public const float PlateGapDp = 6f;

        /// <summary>Contraste mínimo del icono con su disco (iconos ≥ 3:1; direccion-visual-v2 §2.4) [P].</summary>
        public const float IconDiscContrastMin = 3f;

        /// <summary>Contorno del icono de la fruta (dp) [P].</summary>
        public const float IconOutlineDp = 1.5f;

        /// <summary>Tinta del contorno del icono: #2B2118 a alfa 0,45 [P].</summary>
        public static readonly Color IconOutline = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.45f);

        /// <summary>
        /// Colores del disco bajo el icono, por orden de preferencia (<see cref="DiscFor"/>): la crema oscurecida de la línea de
        /// fruta de la victoria (<see cref="FactLine.IconBack"/>), <c>madera_base</c> y <c>madera_oscura</c> de la familia Madera.
        /// </summary>
        public static readonly Color[] DiscColors = { FactLine.IconBack, Palette.FamilyWood.Base, Palette.FamilyWood.Deep };

        // Espacio duro con el que la última palabra de la frase baja acompañada.
        private const char NoBreakSpace = '\u00A0';

        private const float FadeMs = 200f;

        // Fila del nombre, aire entre filas y aire de arriba y de abajo del bloque de texto (dp, por la escala).
        private const float NameRowDp = 22f;
        private const float RowGapDp = 2f;
        private const float PadDp = 8f;

        // Columna del texto, a la derecha del icono (dp desde el borde izquierdo de la tarjeta), y su aire a la derecha.
        private const float IconX = 11f;
        private const float TextX = IconX + IconDp + 10f;
        private const float TextRightPadDp = 12f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _plate;
        private readonly Image _edge;
        private readonly Image _iconBack;
        private readonly Image _icon;
        private readonly Image _swatch;
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _tag;
        private readonly ProceduralArt _art;
        private readonly float _scale;
        private string _tagSource = string.Empty; // la frase de la tabla (la que se muestra puede llevar un espacio duro)
        private float _leftMs = -1f;
        private float _totalMs;
        private float _height = HeightDp;
        private float _tagHeight = 17f;
        private float _nameWidth;
        private float _cardWidth = -1f; // con Measure, si se encoge al texto; < 0, el de siempre

        public FruitNewCard(Transform parent, ProceduralArt art)
        {
            _scale = UiBuilder.TextScale;
            _art = art;
            _root = UiBuilder.Rect("FruitNewCard", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _edge = UiBuilder.Sliced("Edge", _root, art.RoundedRect, Palette.FamilyWood.Base);
            _edge.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_edge.rectTransform);
            _edge.rectTransform.offsetMin = new Vector2(0f, -EdgeDp);
            _edge.rectTransform.offsetMax = new Vector2(0f, -EdgeDp);
            _plate = UiBuilder.Sliced("Plate", _root, art.RoundedRect, Palette.PanelLight);
            _plate.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_plate.rectTransform);
            // El disco bajo la fruta, a la resolución de la UI, del color que le da contraste al icono (DiscFor, en Show), y el
            // contorno del icono, que lo separa también de la placa cuando va sin disco.
            _iconBack = UiBuilder.Image("FruitBack", _root, art.FruitNewDisc, FactLine.IconBack);
            _icon = UiBuilder.Image("Fruit", _root, null, Color.white);
            _icon.preserveAspect = true;
            Outline outline = _icon.gameObject.AddComponent<Outline>();
            outline.effectColor = IconOutline;
            outline.effectDistance = new Vector2(IconOutlineDp, -IconOutlineDp);
            outline.useGraphicAlpha = true;
            _swatch = UiBuilder.Image("Swatch", _root, null, Color.white);
            _swatch.preserveAspect = true;
            _name = UiBuilder.Label("Name", _root, TextStyle.PlainBold, string.Empty, NameSp, Palette.FamilyWood.Base, TextAlignmentOptions.Left);
            _tag = UiBuilder.Label("Tag", _root, TextStyle.Body, string.Empty, TagSp, Palette.FamilyWood.Deep, TextAlignmentOptions.Left);
            _tag.textWrappingMode = TextWrappingModes.Normal; // si no cabe a 13 sp, en dos líneas: la tarjeta crece
            UiBuilder.AutoSize(_name, UiBuilder.MinTextSp, NameSp);
            _root.gameObject.SetActive(false);
        }

        /// <summary>La tarjeta está a la vista.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Nombre del tinte que muestra.</summary>
        public string Name => _name.text;

        /// <summary>Frase de «Fruta nueva» que muestra, como está en la tabla de textos.</summary>
        public string Tag => _tagSource;

        /// <summary>La frase tal como se ve: con un espacio duro (U+00A0) si evitó una palabra sola en la última línea.</summary>
        public string ShownTag => _tag.text;

        /// <summary>Color del disco bajo el icono (<see cref="DiscFor"/>; tests).</summary>
        public Color DiscColor => _iconBack.color;

        /// <summary>Contraste del icono que muestra con su disco (luminancia media del icono; tests y capturas).</summary>
        public float IconDiscContrast { get; private set; }

        /// <summary>Luminancia relativa media del icono que muestra (<see cref="ProceduralArt.FruitIconLuminance"/>; tests).</summary>
        public float IconLuminance { get; private set; }

        /// <summary>Índice de motor del tinte que muestra (glosario §9; 0 si ninguno).</summary>
        public int Tint { get; private set; }

        /// <summary>Va como placa de una línea bajo el HUD (<see cref="PlaceAsPlate"/>), no como tarjeta.</summary>
        public bool OnPlate { get; private set; }

        /// <summary>Caja de la tarjeta o de la placa en el lienzo (dp desde arriba, sin el canto).</summary>
        public Rect Rect => new Rect(_root.anchoredPosition.x, -_root.anchoredPosition.y, _root.rect.width, _root.rect.height);

        /// <summary>Raíz (tests de exclusión y de colocación).</summary>
        public RectTransform Root => _root;

        /// <summary>Opacidad actual del fundido.</summary>
        public float Alpha => _group.alpha;

        /// <summary>Duración de la tarjeta a la vista, con sus fundidos (ms; tests).</summary>
        public float DurationMs => _totalMs;

        /// <summary>Alto de la tarjeta (dp) con el último <see cref="Measure"/>.</summary>
        public float Height => _height;

        /// <summary>El icono de la fruta (tests).</summary>
        public Image Icon => _icon;

        /// <summary>La muestra del tinte (tests).</summary>
        public Image Swatch => _swatch;

        /// <summary>Nombre y frase (tests de tamaño y desbordes).</summary>
        public TextMeshProUGUI[] Texts => new[] { _name, _tag };

        /// <summary>
        /// El nombre visible y la frase de «Fruta nueva» del tinte <paramref name="fruit"/> (índice en
        /// <see cref="SightingRule.FruitDebutTints"/>) en el idioma de la UI, o false si falta alguno: <see cref="Loc.Fact(string)"/>
        /// da «» si la clave falta en ese idioma o, en release, si no está aprobada (D-018, Q10-A). Asigna sus cadenas: solo al
        /// enseñar la tarjeta.
        /// </summary>
        public static bool TryTexts(int fruit, out string name, out string tag)
        {
            name = Loc.Fact(SightingRule.FruitNameKey(fruit));
            tag = Loc.Fact(SightingRule.FruitTagKey(fruit));
            return name.Length > 0 && tag.Length > 0;
        }

        /// <summary>
        /// Pone el nombre y la frase que va a mostrar y devuelve el alto de la tarjeta en un lienzo de <paramref name="canvasW"/>
        /// dp (<see cref="Height"/>): la fila del nombre y la de la frase por la escala de texto, la de la frase con las líneas que
        /// necesite a 13 sp, y su aire; al menos <see cref="HeightDp"/> por la escala. Si la frase cabe en una línea, la tarjeta se
        /// encoge al ancho de su texto (o del nombre con su muestra) más <see cref="ShrunkRightPadDp"/> y va centrada, como la del
        /// miquito con el dato equilibrado (A1). Se llama antes de decidir dónde va (GameScreen.Notices.cs) y de
        /// <see cref="Place"/>; mide una vez por tarjeta, nunca por fotograma.
        /// </summary>
        public float Measure(float canvasW, string name, string tag)
        {
            _name.text = name;
            _tagSource = tag;
            _tag.text = tag;
            CardMode();
            float s = _scale;
            float column = TextWidth(canvasW);
            float swatch = (SwatchDp + SwatchGapDp) * s;
            _nameWidth = Mathf.Min(PreferredWidth(_name), Mathf.Max(0f, column - swatch));
            Vector2 oneLine = _tag.GetPreferredValues(tag, float.PositiveInfinity, float.PositiveInfinity);
            float tagHeight = _tag.GetPreferredValues(tag, column, float.PositiveInfinity).y;
            if (oneLine.x > column)
            {
                string shown = WithoutWidow(tag, column, tagHeight);
                if (!ReferenceEquals(shown, tag))
                {
                    _tag.text = shown;
                    tagHeight = _tag.GetPreferredValues(shown, column, float.PositiveInfinity).y;
                }
            }

            _cardWidth = -1f;
            if (oneLine.x <= column)
            {
                float content = Mathf.Max(oneLine.x, _nameWidth + swatch);
                _cardWidth = Mathf.Ceil(TextX + content + ShrunkRightPadDp);
            }

            _tagHeight = Mathf.Max(17f * s, Mathf.Ceil(tagHeight));
            _height = Mathf.Max(HeightDp * s, Block() + (2f * PadDp * s));
            return _height;
        }

        /// <summary>
        /// Coloca la tarjeta centrada con su borde superior en <paramref name="top"/> (dp desde arriba), del alto y, si se encogió
        /// a su texto, del ancho del último <see cref="Measure"/>: el icono centrado a lo alto a la izquierda y, a su derecha, el
        /// bloque del nombre con su muestra y la frase, centrado a lo alto.
        /// </summary>
        public void Place(float canvasW, float top)
        {
            float w = Mathf.Min(320f, canvasW - 32f);
            if (_cardWidth > 0f)
            {
                w = Mathf.Min(w, _cardWidth);
            }

            float s = _scale;
            UiBuilder.Place(_root, (canvasW - w) * 0.5f, top, w, _height);
            float iconTop = Mathf.Round((_height - IconDp) * 0.5f);
            UiBuilder.Place(_iconBack.rectTransform, IconX - 2f, iconTop - 2f, IconDp + 4f, IconDp + 4f);
            UiBuilder.Place(_icon.rectTransform, IconX, iconTop, IconDp, IconDp);
            float textW = w - TextX - TextRightPadDp;
            float y = Mathf.Round((_height - Block()) * 0.5f);
            float swatch = SwatchDp * s;
            float nameW = Mathf.Min(_nameWidth, Mathf.Max(0f, textW - swatch - (SwatchGapDp * s)));
            UiBuilder.Place(_name.rectTransform, TextX, y, nameW, NameRowDp * s);
            UiBuilder.Place(_swatch.rectTransform, TextX + nameW + (SwatchGapDp * s), y + (((NameRowDp * s) - swatch) * 0.5f), swatch, swatch);
            UiBuilder.Place(_tag.rectTransform, TextX, y + ((NameRowDp + RowGapDp) * s), textW, _tagHeight);
        }

        /// <summary>
        /// La tarjeta como placa de una línea (<see cref="OnPlate"/>) [P], para los tableros en que no cabe ni en la franja del
        /// panorama ni sobre el muelle (los de 3 filas, como el 111 y el 201: a 360 × 780 dp la franja mide −8 dp y el muelle 19,5):
        /// clavada al marco bajo el HUD, como la placa del rótulo del lugar (<see cref="StopLabel"/>; FX4), con el icono de la
        /// fruta, la frase (que ya empieza por el nombre del tinte, 06 §4.5; el nombre suelto no va) y la muestra del tinte en una
        /// línea, de <see cref="PlateTagSp"/> sp que se encogen hasta 12 sp (por la escala) para caber entre
        /// <paramref name="top"/> y <paramref name="maxBottom"/> (dp desde arriba, sin contar el canto) y entre los márgenes del
        /// lienzo (<see cref="StopLabel.SideDp"/>). Devuelve false si ni a 12 sp cabe: entonces no sale. Mide al colocar, nunca
        /// por fotograma.
        /// </summary>
        public bool PlaceAsPlate(float canvasW, string name, string tag, float top, float maxBottom)
        {
            _name.text = name;
            _tagSource = tag;
            _tag.text = tag;
            OnPlate = true;
            _name.gameObject.SetActive(false);
            _iconBack.gameObject.SetActive(false);
            _edge.rectTransform.offsetMin = new Vector2(0f, -StopLabel.PlateEdgeDp);
            _edge.rectTransform.offsetMax = new Vector2(0f, -StopLabel.PlateEdgeDp);
            _tag.textWrappingMode = TextWrappingModes.NoWrap;
            _tag.alignment = TextAlignmentOptions.MidlineLeft;
            float s = _scale;
            float min = UiBuilder.MinTextSp * s;
            float size = Mathf.Min(PlateTagSp * s, Mathf.Floor(maxBottom - StopLabel.PlateEdgeDp - top) / StopLabel.PlateHeightEm);
            if (size < min - 0.01f)
            {
                return false;
            }

            float maxWidth = canvasW - (2f * StopLabel.SideDp);
            float width = PlateWidth(size, out float line, out float h);
            if (width > maxWidth)
            {
                size = Mathf.Max(min, size * (maxWidth - (width - line) - 1f) / line);
                width = PlateWidth(size, out line, out h);
                if (width > maxWidth + 0.01f)
                {
                    return false;
                }
            }

            _height = h;
            float left = Mathf.Round((canvasW - width) * 0.5f);
            UiBuilder.Place(_root, left, top, width, h);
            float x = StopLabel.PlatePadXDp;
            UiBuilder.Place(_icon.rectTransform, x, 0f, h, h);
            x += h + PlateGapDp;
            UiBuilder.Place(_tag.rectTransform, x, -size, line, h + (2f * size)); // la caja de línea sobresale; la tinta, centrada
            x += line + PlateGapDp;
            float swatch = h - 4f;
            UiBuilder.Place(_swatch.rectTransform, x, 2f, swatch, swatch);
            return true;
        }

        /// <summary>
        /// Muestra la tarjeta del tinte <paramref name="tint"/> (índice de motor) el tiempo de leerla:
        /// <see cref="NoticeTiming.ReadMs(string, string, string)"/> con el nombre y la frase (D-011, punto 2; en la placa, solo
        /// la frase, que es lo que se lee). Sin el icono o la muestra (null), su sitio queda vacío. El disco toma el color que da
        /// más contraste al icono del tinte (<see cref="DiscFor"/> con <see cref="ProceduralArt.FruitIconLuminance"/>).
        /// </summary>
        public void Show(int tint, Sprite? icon, Sprite? swatch)
        {
            int ms = NoticeTiming.ReadMs(OnPlate ? null : _name.text, _tagSource, null);
            Tint = tint;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _iconBack.enabled = icon != null && !OnPlate;
            IconDiscContrast = 0f;
            IconLuminance = 0f;
            if (icon != null && tint > 0)
            {
                float lum = _art.FruitIconLuminance(tint);
                IconLuminance = lum;
                Color disc = DiscFor(lum);
                _iconBack.color = disc;
                IconDiscContrast = Contrast(lum, ColorMath.RelativeLuminance(disc));
            }

            _swatch.sprite = swatch;
            _swatch.enabled = swatch != null;
            _leftMs = ms;
            _totalMs = Mathf.Max(1f, ms);
            _group.alpha = 0f;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling(); // dentro de la capa de avisos: sigue detrás de la capa de vertido
        }

        public void Hide()
        {
            _leftMs = -1f;
            _root.gameObject.SetActive(false);
        }

        public void Tick(float dtMs)
        {
            if (_leftMs < 0f)
            {
                return;
            }

            _leftMs -= dtMs;
            float shown = _totalMs - _leftMs;
            _group.alpha = Mathf.Clamp01(Mathf.Min(shown, _leftMs) / FadeMs);
            if (_leftMs <= 0f)
            {
                Hide();
            }
        }

        /// <summary>
        /// Color del disco para un icono de luminancia relativa media <paramref name="iconLuminance"/>: el primero de
        /// <see cref="DiscColors"/> que llega a <see cref="IconDiscContrastMin"/> con él (la crema, salvo las frutas claras) o, si
        /// ninguno llega (iconos de tono medio, que se separan con su contorno), el de más contraste.
        /// </summary>
        public static Color DiscFor(float iconLuminance)
        {
            Color best = DiscColors[0];
            float bestContrast = 0f;
            for (int i = 0; i < DiscColors.Length; i++)
            {
                float c = Contrast(iconLuminance, ColorMath.RelativeLuminance(DiscColors[i]));
                if (c >= IconDiscContrastMin)
                {
                    return DiscColors[i];
                }

                if (c > bestContrast)
                {
                    bestContrast = c;
                    best = DiscColors[i];
                }
            }

            return best;
        }

        /// <summary>Contraste WCAG entre dos luminancias relativas (1 a 21).</summary>
        public static float Contrast(float a, float b)
        {
            return (Mathf.Max(a, b) + 0.05f) / (Mathf.Min(a, b) + 0.05f);
        }

        /// <summary>Añade los gráficos de la tarjeta (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.Add(_plate);
            graphics.Add(_edge);
            graphics.Add(_iconBack);
            graphics.Add(_icon);
            graphics.Add(_swatch);
            graphics.Add(_name);
            graphics.Add(_tag);
        }

        // La frase sin una palabra sola en su última línea (con el ancho column y el alto height que ya midió): si sin su última
        // palabra ocupa una línea menos, esa palabra iba sola; entonces el último espacio pasa a espacio duro y baja con la
        // anterior, siempre que el alto no crezca. Con menos de tres palabras, o si no mejora, la deja como está (la misma cadena).
        private string WithoutWidow(string tag, float column, float height)
        {
            int last = tag.LastIndexOf(' ');
            if (last <= 0 || last >= tag.Length - 1 || tag.LastIndexOf(' ', last - 1) <= 0)
            {
                return tag;
            }

            string head = tag.Substring(0, last);
            if (_tag.GetPreferredValues(head, column, float.PositiveInfinity).y >= height - 0.5f)
            {
                return tag; // la última palabra no va sola
            }

            string kept = head + NoBreakSpace + tag.Substring(last + 1);
            return _tag.GetPreferredValues(kept, column, float.PositiveInfinity).y <= height + 0.5f ? kept : tag;
        }

        // Alto del bloque de texto: la fila del nombre, el aire y la frase.
        private float Block()
        {
            return ((NameRowDp + RowGapDp) * _scale) + _tagHeight;
        }

        // Vuelve a la tarjeta de siempre (tras una placa): el nombre y el disco a la vista, la frase en su tamaño, cortada en
        // líneas y arriba a la izquierda, y el canto de la tarjeta.
        private void CardMode()
        {
            OnPlate = false;
            _name.gameObject.SetActive(true);
            _iconBack.gameObject.SetActive(true);
            _edge.rectTransform.offsetMin = new Vector2(0f, -EdgeDp);
            _edge.rectTransform.offsetMax = new Vector2(0f, -EdgeDp);
            _tag.fontSize = TagSp * _scale;
            _tag.textWrappingMode = TextWrappingModes.Normal;
            _tag.alignment = TextAlignmentOptions.Left;
        }

        // Ancho de la placa con la frase a size dp: el aire, el icono (del alto de la placa), la frase en una línea, la muestra y
        // el aire; line, el ancho de la frase, y h, el alto de la placa.
        private float PlateWidth(float size, out float line, out float h)
        {
            _tag.fontSize = size;
            line = Mathf.Ceil(_tag.GetPreferredValues(_tag.text, float.PositiveInfinity, float.PositiveInfinity).x) + 2f; // holgura del redondeo
            h = Mathf.Ceil((size * StopLabel.PlateHeightEm) - 0.01f);
            return (2f * StopLabel.PlatePadXDp) + h + PlateGapDp + line + PlateGapDp + (h - 4f);
        }

        // Ancho de su texto de ahora en una línea, al tamaño al que mide TextMeshPro (con autoajuste, su máximo).
        private static float PreferredWidth(TextMeshProUGUI text)
        {
            return string.IsNullOrEmpty(text.text) ? 0f : Mathf.Ceil(text.GetPreferredValues(text.text, float.PositiveInfinity, float.PositiveInfinity).x);
        }

        private static float TextWidth(float canvasW)
        {
            return Mathf.Min(320f, canvasW - 32f) - TextX - TextRightPadDp;
        }
    }
}
