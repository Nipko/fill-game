#nullable enable
using System.Collections.Generic;
using AF.Core.Mechanics;
using AF.Core.Session;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Contador de la Lluvia o de la Piraña en el HUD (02 §9.1 M6-M7; traspaso §6 paso 1.3: «Lluvia (con su contador en el
    /// HUD)»; tarea T09): una píldora clara con contorno de <c>madera_oscura</c>, el icono de la mecánica a la izquierda (la nube
    /// de la Lluvia o la piraña de <see cref="ProceduralArt.MechanicIcon"/>, de ≥ <see cref="MinIconWidthDp"/> de ancho para que
    /// la mecánica se lea antes que la cifra), los vertidos útiles que faltan en tinta (<see cref="LevelCounter.Remaining"/>, que
    /// solo baja con vertidos útiles: Q18 B) y, en la Lluvia, separadas de la cifra por un hueco con un filete, una gotita del
    /// agua del mundo por cada gota pendiente (<see cref="LevelCounter.PendingDrops"/>). Atenuado si
    /// <see cref="LevelCounter.Blocked"/> (la gota espera o el mordisco se perderá). Oculto sin contador.
    /// </summary>
    /// <remarks>
    /// <para><b>Material</b> [P] (revisión de T09): cara clara plana <see cref="FaceColor"/>, cifra en tinta #2B2118 y contorno de
    /// <see cref="OutlineDp"/> en <c>madera_oscura</c> #36261C, distinta del ámbar, del índigo y del gris-pardo de las etiquetas
    /// de dificultad y de Aguas turbias, para que el contador no se lea como parte de ellas («Muy difícil 4», que repite el
    /// patrón de la cinta «×4» y haría creer que el premio baja con cada jugada, 02 §10). La cara no es la crema #FFF8E6 de la
    /// UI: esa queda a ΔE00 6,5 de la Guanábana y el contador va sobre el panel en los tableros de 3 filas
    /// (direccion-visual-v2 §2.4, regla 1). <see cref="FaceColor"/> (#FEFCDC) queda a ΔE00 ≥ 10 de los 14 tintes y es plana:
    /// un degradado la acercaría a la Guanábana o al Copoazú.</para>
    /// <para>Son dos cantidades distintas (los vertidos que faltan y las gotas que quedan por caer): para que «2 ·· » no se
    /// lea como «2 gotas», las gotitas van tras <see cref="DropsGapDp"/> con un filete de 1 dp <c>muelle_sombra</c> en medio,
    /// y la nube del chip es la nube sola (<see cref="CloudSprite"/>), con un contorno pizarra para leerse sobre la cara clara.
    /// Las gotitas llevan el mismo contorno (<see cref="DropSprite"/>).</para>
    /// <para>La vista no decide reglas: pinta el <see cref="LevelCounter"/> del núcleo tal cual. Sin asignaciones al refrescar
    /// (la cifra va con <see cref="TMP_Text.SetCharArray(char[], int, int)"/>); dos alturas, la normal (<see cref="HeightDp"/>)
    /// y la compacta del HUD (<see cref="CompactHeightDp"/>).</para>
    /// </remarks>
    internal sealed class HudCounterChip
    {
        /// <summary>Alto de la píldora normal (dp).</summary>
        public const float HeightDp = 28f;

        /// <summary>Alto de la píldora compacta (dp; la del chip de dificultad compacto).</summary>
        public const float CompactHeightDp = DifficultyChip.CompactHeightDp;

        /// <summary>Gotitas como mucho (la Lluvia del nivel; <c>n</c> ≤ 4 por <c>U_agua</c> = 4, 02 §9.1 M6).</summary>
        public const int MaxDrops = 4;

        /// <summary>Opacidad del chip con el contador bloqueado [P].</summary>
        public const float BlockedAlpha = 0.5f;

        /// <summary>Hueco entre la cifra y las gotitas (dp), con el filete de 1 dp en medio [P].</summary>
        public const float DropsGapDp = 6f;

        /// <summary>Ancho del filete entre la cifra y las gotitas (dp).</summary>
        public const float DividerDp = 1f;

        /// <summary>Contorno de la píldora (dp).</summary>
        public const float OutlineDp = 1.5f;

        /// <summary>Ancho mínimo del icono de la mecánica (dp): se lee antes que la cifra.</summary>
        public const float MinIconWidthDp = 16f;

        /// <summary>Sombra bajo la píldora (dp).</summary>
        public const float ShadowDp = 2f;

        /// <summary>Cara de la píldora: marfil claro #FEFCDC [P], a ΔE00 ≥ 10 de los 14 tintes (ver la clase).</summary>
        public static readonly Color FaceColor = Palette.Hex(0xFEFCDC);

        /// <summary>Contorno de la píldora: <c>madera_oscura</c> #36261C.</summary>
        public static readonly Color OutlineColor = Palette.WoodDark;

        /// <summary>Cifra: tinta #2B2118 (15:1 sobre la cara).</summary>
        public static readonly Color DigitColor = Palette.Ink;

        /// <summary>Contorno de la nube y de las gotitas: pizarra (<see cref="ProceduralArt.Slate"/>).</summary>
        public static readonly Color IconOutline = ProceduralArt.Slate;

        // Ancho del sprite de la placa (dp): los extremos son semicírculos del 9-slice.
        private const float PlateSpriteWidthDp = 40f;

        // Sprite de la nube a 3 px/dp: 76 × 48 px (≈ 25 × 16 dp); contorno de 3,5 px.
        private const int CloudPxW = 76;
        private const int CloudPxH = 48;
        private const float CloudOutlinePx = 3.5f;

        // Sprite de la gotita: 24 × 30 px (8 × 10 dp); contorno de 2,6 px.
        private const int DropPxW = 24;
        private const int DropPxH = 30;

        private static Sprite? s_cloud;
        private static Sprite? s_drop;
        private static Sprite? s_plate;
        private static Sprite? s_plateCompact;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _plate;
        private readonly Image _icon;
        private readonly TextMeshProUGUI _value;
        private readonly Image _divider;
        private readonly Image[] _drops = new Image[MaxDrops];
        private readonly char[] _buf = new char[8];
        private MechanicType _type = MechanicType.None;
        private int _remaining = -1;
        private int _dropCount = -1;
        private int _dropTint = -1;
        private bool _blocked;
        private bool _compact;
        private float _width;

        private HudCounterChip(Transform parent, ProceduralArt art)
        {
            _art = art;
            _root = UiBuilder.Rect("CounterChip", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _plate = UiBuilder.Sliced("Plate", _root, PlateSprite(false), Color.white);
            _icon = UiBuilder.Image("Icon", _root, null, Color.white);
            _icon.preserveAspect = true;
            // Nunito ExtraBold con el contorno de madera de 1,5 dp de las píldoras: con la cara en tinta, la cifra queda gruesa y
            // oscura sobre la cara clara (con Nunito Bold sin contorno se veía gris a 13 dp).
            _value = UiBuilder.Label("Value", _root, TextStyle.PillWood, string.Empty, 17f, DigitColor, TextAlignmentOptions.Center);
            _divider = UiBuilder.Image("Divider", _root, null, Palette.DockShadow);
            _divider.gameObject.SetActive(false);
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i] = UiBuilder.Image("Drop" + i, _root, DropSprite(), Color.white);
                _drops[i].preserveAspect = true;
                _drops[i].gameObject.SetActive(false);
            }

            _root.gameObject.SetActive(false);
        }

        /// <summary>Contenedor del chip.</summary>
        public RectTransform Root => _root;

        /// <summary>Se ve (hay contador).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Mecánica del contador mostrado (<see cref="MechanicType.None"/> si no se ve).</summary>
        public MechanicType Type => Visible ? _type : MechanicType.None;

        /// <summary>Cifra mostrada (−1 si no se ve).</summary>
        public int Remaining => Visible ? _remaining : -1;

        /// <summary>Texto de la cifra.</summary>
        public string Text => _value.text;

        /// <summary>Color de la cifra (tinta).</summary>
        public Color TextColor => _value.color;

        /// <summary>Gotitas a la vista.</summary>
        public int DropsShown => Visible ? _dropCount : 0;

        /// <summary>Atenuado por <see cref="LevelCounter.Blocked"/>.</summary>
        public bool Blocked => Visible && _blocked;

        /// <summary>Opacidad aplicada (1 o <see cref="BlockedAlpha"/>).</summary>
        public float Alpha => _group.alpha;

        /// <summary>Icono de la mecánica (la nube o la piraña).</summary>
        public Sprite? Icon => _icon.sprite;

        /// <summary>Rectángulo del icono (tests).</summary>
        public RectTransform IconRect => _icon.rectTransform;

        /// <summary>Rectángulo de la cifra (tests).</summary>
        public RectTransform ValueRect => _value.rectTransform;

        /// <summary>Placa de la píldora (tests).</summary>
        public Image Plate => _plate;

        /// <summary>Filete entre la cifra y las gotitas (tests).</summary>
        public Image Divider => _divider;

        /// <summary>Gotita <paramref name="i"/> (tests).</summary>
        public Image Drop(int i)
        {
            return _drops[i];
        }

        /// <summary>Color de las gotitas (el agua del mundo).</summary>
        public Color DropColor => _drops[0].color;

        /// <summary>Variante compacta.</summary>
        public bool Compact => _compact;

        /// <summary>Alto de la píldora (dp).</summary>
        public float Height => _compact ? CompactHeightDp : HeightDp;

        /// <summary>Ancho con el contenido actual (dp).</summary>
        public float Width => _width;

        /// <summary>Crea el chip, oculto, bajo <paramref name="parent"/>.</summary>
        public static HudCounterChip Create(Transform parent, ProceduralArt art)
        {
            return new HudCounterChip(parent, art);
        }

        /// <summary>
        /// Colores principales de las superficies de más de 2 dp del chip a la vista (test de ΔE00 frente a los tintes cuando va
        /// sobre el panel; direccion-visual-v2 §2.4, regla 1): la cara y el icono de la mecánica (la nube en gris perla con su
        /// panza y su contorno pizarra, o la piraña con el lomo, el costado y el vientre). El contorno de la píldora (1,5 dp) y el
        /// filete no cuentan, y las gotitas llevan el agua del mundo a propósito, como las de la nube de la cuia
        /// (<c>LluviaOverlay</c>). Solo para tests.
        /// </summary>
        public void CollectSurfaceColors(List<(string Name, Color Color)> into)
        {
            if (!Visible)
            {
                return;
            }

            into.Add(("cara del contador", FaceColor));
            if (_type == MechanicType.Lluvia)
            {
                Color pearl = ProceduralArt.Pearl;
                into.Add(("nube", pearl));
                into.Add(("panza de la nube", new Color(pearl.r * 0.84f, pearl.g * 0.84f, pearl.b * 0.84f, 1f)));
                into.Add(("contorno de la nube", IconOutline));
            }
            else if (_type == MechanicType.Pirana)
            {
                into.Add(("lomo de la piraña", ProceduralArt.FishBack));
                into.Add(("costado de la piraña", ProceduralArt.FishSide));
                into.Add(("vientre de la piraña", ProceduralArt.FishBelly));
                into.Add(("contorno de la piraña", ProceduralArt.Slate));
            }
        }

        /// <summary>
        /// Pinta <paramref name="counter"/> (oculto si no está activo) con las gotitas del tinte <paramref name="dropTint"/> (el
        /// agua del mundo de la Lluvia: <c>Cuia.MechanicKey</c>). Devuelve true si cambió el ancho o la visibilidad (hay que
        /// recolocar). Sin asignaciones.
        /// </summary>
        public bool Show(in LevelCounter counter, int dropTint)
        {
            bool wasVisible = Visible;
            float oldWidth = _width;
            if (!counter.IsActive)
            {
                if (wasVisible)
                {
                    _root.gameObject.SetActive(false);
                }

                return wasVisible;
            }

            if (!wasVisible)
            {
                _root.gameObject.SetActive(true);
            }

            bool typeChanged = counter.Type != _type;
            if (typeChanged)
            {
                _type = counter.Type;
                _icon.sprite = _type == MechanicType.Lluvia ? CloudSprite() : _art.MechanicIcon(_type);
            }

            int remaining = counter.Remaining;
            bool digitsChanged = (remaining >= 10) != (_remaining >= 10);
            if (remaining != _remaining)
            {
                _remaining = remaining;
                int n = UiText.WriteNumber(_buf, 0, remaining, false);
                _value.SetCharArray(_buf, 0, n);
            }

            int drops = counter.Type == MechanicType.Lluvia ? Mathf.Clamp(counter.PendingDrops, 0, MaxDrops) : 0;
            bool dropsChanged = drops != _dropCount;
            if (dropsChanged)
            {
                _dropCount = drops;
                for (int i = 0; i < _drops.Length; i++)
                {
                    _drops[i].gameObject.SetActive(i < drops);
                }
            }

            if (drops > 0 && dropTint != _dropTint)
            {
                _dropTint = dropTint;
                Color water = Palette.Tint(dropTint); // el agua del mundo, como las gotitas de la nube
                for (int i = 0; i < _drops.Length; i++)
                {
                    _drops[i].color = water;
                }
            }

            _blocked = counter.Blocked;
            float alpha = _blocked ? BlockedAlpha : 1f;
            if (!Mathf.Approximately(_group.alpha, alpha))
            {
                _group.alpha = alpha;
            }

            if (dropsChanged || digitsChanged || typeChanged || !wasVisible)
            {
                Layout();
            }

            return !wasVisible || !Mathf.Approximately(oldWidth, _width);
        }

        /// <summary>Pasa a la variante compacta (o vuelve) y recalcula el ancho; quien lo coloca llama después a <see cref="Place"/>.</summary>
        public void SetCompact(bool compact)
        {
            if (compact == _compact)
            {
                return;
            }

            _compact = compact;
            Layout();
        }

        /// <summary>Coloca el chip con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="yTop"/>) del padre.</summary>
        public void Place(float x, float yTop)
        {
            UiBuilder.Place(_root, x, yTop, _width, Height);
        }

        // Placa, icono, cifra y gotitas de izquierda a derecha (dp). La cifra lleva una caja fija por dígito (sin medir el
        // texto) para no asignar. Entre la cifra y las gotitas, DropsGapDp con el filete en medio.
        private void Layout()
        {
            float h = Height;
            float pad = _compact ? 4f : 5f;
            bool cloud = _type == MechanicType.Lluvia;
            float iconW = _compact ? 17f : 23f;
            float iconH = cloud ? Mathf.Round(iconW * CloudPxH / CloudPxW) : iconW;
            if (!cloud)
            {
                iconW = _compact ? 18f : 23f; // la piraña ocupa el 92 % de su caja: ≥ 16 dp de pez
                iconH = iconW;
            }

            float textSize = _compact ? 13f : 17f;
            int digits = _remaining >= 10 ? 2 : 1;
            float digitW = Mathf.Ceil(textSize * 0.62f) * digits;
            float dropW = _compact ? 5f : 7f;
            float dropGap = _compact ? 1.5f : 2f;
            float x = pad;
            UiBuilder.Place(_icon.rectTransform, x, (h - iconH) * 0.5f, iconW, iconH);
            x += iconW + (cloud ? 2f : 1f);
            _value.fontSize = textSize;
            UiBuilder.Place(_value.rectTransform, x, 0f, digitW + 2f, h);
            x += digitW + 2f;
            int drops = Mathf.Max(0, _dropCount);
            _divider.gameObject.SetActive(drops > 0);
            if (drops > 0)
            {
                float side = (DropsGapDp - DividerDp) * 0.5f;
                float lineH = Mathf.Round(h * 0.6f);
                UiBuilder.Place(_divider.rectTransform, x + side, (h - lineH) * 0.5f, DividerDp, lineH);
                x += DropsGapDp;
                float dropH = dropW * DropPxH / DropPxW;
                for (int i = 0; i < drops && i < _drops.Length; i++)
                {
                    UiBuilder.Place(_drops[i].rectTransform, x, (h - dropH) * 0.5f, dropW, dropH);
                    x += dropW + dropGap;
                }

                x -= dropGap;
            }

            x += pad;
            _width = Mathf.Ceil(x);
            _plate.sprite = PlateSprite(_compact);
            UiBuilder.Place(_plate.rectTransform, 0f, 0f, _width, h + ShadowDp);
        }

        /// <summary>
        /// Placa de la píldora (<c>hud_counter_plate</c> o <c>hud_counter_plate_compact</c>) para su alto: cara plana
        /// <see cref="FaceColor"/>, contorno de <see cref="OutlineDp"/> en <see cref="OutlineColor"/>, un filo de luz de 1 dp
        /// arriba y la sombra de <see cref="ShadowDp"/> debajo. 9-slice horizontal (los extremos, semicírculos). Una textura por
        /// alto para toda la app, así el contorno mide 1,5 dp en las dos.
        /// </summary>
        internal static Sprite PlateSprite(bool compact)
        {
            Sprite? cached = compact ? s_plateCompact : s_plate;
            if (cached != null)
            {
                return cached;
            }

            const float k = ChipPaint.PxPerDp;
            float pillH = compact ? CompactHeightDp : HeightDp;
            float hDp = pillH + ShadowDp;
            int w = Mathf.RoundToInt(PlateSpriteWidthDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pixels = new Color[w * h];
            float r = pillH * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    var c = new Color(0f, 0f, 0f, 0f);
                    float sh = ChipPaint.RoundedRectDistance(xd, yd - 1.6f, 0.5f, 0.5f, PlateSpriteWidthDp - 0.5f, pillH - 0.5f, r - 0.5f);
                    c = ChipPaint.Over(c, new Color(0f, 0f, 0f, 0.3f), ChipPaint.SoftCoverage(sh * k, 1.6f * k));
                    float d = ChipPaint.RoundedRectDistance(xd, yd, 0.25f, 0.25f, PlateSpriteWidthDp - 0.25f, pillH - 0.25f, r - 0.25f);
                    float cov = ChipPaint.Coverage(d * k);
                    if (cov > 0f)
                    {
                        c = ChipPaint.Over(c, OutlineColor, cov);
                        float inside = ChipPaint.Coverage((d + OutlineDp) * k);
                        if (inside > 0f)
                        {
                            Color face = FaceColor;
                            float fromEdge = -(d + OutlineDp);
                            if (fromEdge < 1f && yd < pillH * 0.45f)
                            {
                                face = Color.Lerp(face, Color.white, 0.6f * (1f - fromEdge)); // filo de luz (≤ 1 dp)
                            }

                            face.a = 1f;
                            c = ChipPaint.Over(c, face, inside);
                        }
                    }

                    pixels[(y * w) + x] = c;
                }
            }

            float border = r * k;
            Sprite sprite = ChipPaint.ToSprite(pixels, w, h, compact ? "hud_counter_plate_compact" : "hud_counter_plate", new Vector4(border, 0f, border, 0f));
            if (compact)
            {
                s_plateCompact = sprite;
            }
            else
            {
                s_plate = sprite;
            }

            return sprite;
        }

        /// <summary>
        /// Nube de la Lluvia sin gotas para el chip (<c>hud_counter_cloud</c>): la del icono de la mecánica
        /// (<c>ProceduralArt.DrawCloud</c>: base redondeada y tres copos en gris perla <see cref="ProceduralArt.Pearl"/>, con la
        /// panza, el 30 % de abajo, al 84 %) con un contorno pizarra de ≈ 1 dp para leerse sobre la cara clara, dibujada aquí
        /// porque el icono trae tres gotas grises debajo y el sprite de la nube del tablero no es público. Una sola textura legible
        /// para toda la app (se rehace si Unity la destruyó).
        /// </summary>
        internal static Sprite CloudSprite()
        {
            if (s_cloud != null)
            {
                return s_cloud;
            }

            const int w = CloudPxW;
            const int h = CloudPxH;

            // La geometría de DrawCloud en px (y hacia arriba) con la caja de 88 × 50 ajustada al sprite con el contorno.
            const float x0 = -4f;
            const float y0 = 0.5f;
            const float cw = 84f;
            const float ch = 47f;
            var pixels = new Color[w * h];
            Color pearl = ProceduralArt.Pearl;
            var belly = new Color(pearl.r * 0.84f, pearl.g * 0.84f, pearl.b * 0.84f, 1f);
            float bellyTop = y0 + (ch * 0.30f);
            for (int y = 0; y < h; y++)
            {
                float py = y + 0.5f;
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float d = ChipPaint.RoundedRectDistance(px, py, x0 + (cw * 0.08f), y0 + (ch * 0.05f), x0 + (cw * 0.92f), y0 + (ch * 0.50f), ch * 0.22f);
                    d = Mathf.Min(d, ChipPaint.CircleDistance(px, py, x0 + (cw * 0.30f), y0 + (ch * 0.48f), ch * 0.30f));
                    d = Mathf.Min(d, ChipPaint.CircleDistance(px, py, x0 + (cw * 0.53f), y0 + (ch * 0.58f), ch * 0.38f));
                    d = Mathf.Min(d, ChipPaint.CircleDistance(px, py, x0 + (cw * 0.75f), y0 + (ch * 0.45f), ch * 0.27f));
                    float cov = ChipPaint.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c = ChipPaint.Over(new Color(0f, 0f, 0f, 0f), IconOutline, cov);
                    c = ChipPaint.Over(c, py < bellyTop ? belly : pearl, ChipPaint.Coverage(d + CloudOutlinePx));
                    pixels[(y * w) + x] = c;
                }
            }

            s_cloud = ChipPaint.ToSprite(pixels, w, h, "hud_counter_cloud", Vector4.zero);
            return s_cloud;
        }

        /// <summary>
        /// Gotita del chip (<c>hud_counter_drop</c>): gota blanca con la punta arriba y contorno pizarra, que la imagen tiñe con
        /// el agua del mundo (el contorno, multiplicado, queda oscuro y la separa de la cara clara). Una sola textura.
        /// </summary>
        internal static Sprite DropSprite()
        {
            if (s_drop != null)
            {
                return s_drop;
            }

            const int w = DropPxW;
            const int h = DropPxH;
            const float r = 10.5f;
            const float cx = w * 0.5f;
            const float cy = h - r - 1f; // centro del círculo, cerca de abajo (y hacia abajo en la imagen)
            const float tip = cy - 1f;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float py = h - (y + 0.5f); // y hacia abajo
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float d = ChipPaint.DropDistance(px, py, cx, cy, r, tip);
                    float cov = ChipPaint.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c = ChipPaint.Over(new Color(0f, 0f, 0f, 0f), IconOutline, cov);
                    c = ChipPaint.Over(c, Color.white, ChipPaint.Coverage(d + 2.6f));
                    pixels[(y * w) + x] = c;
                }
            }

            s_drop = ChipPaint.ToSprite(pixels, w, h, "hud_counter_drop", Vector4.zero);
            return s_drop;
        }
    }
}
