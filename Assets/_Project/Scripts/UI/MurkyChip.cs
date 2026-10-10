#nullable enable
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Chip de Aguas turbias (07 §7.7: «Chip adicional gris-pardo con "?"», en el HUD y en la tarjeta previa; arquitectura del
    /// paso 1, §9: lo crea T09 y T13 lo reutiliza en la tarjeta sin editarlo): una píldora gris-pardo
    /// (<see cref="Palette.Murky"/>, #8A7B6A, el agua de las dosis ocultas) con el «?» marfil de
    /// <see cref="ProceduralArt.QuestionMark"/> en sprite, sin texto. La píldora lleva la misma pintura que la del chip de
    /// dificultad (contorno más oscuro, luz en la parte alta, canto abajo y sombra) para leerse como su pareja [P].
    /// </summary>
    /// <remarks>
    /// <para>Contraste del «?» (marfil <see cref="Palette.UiPaper"/>) sobre el gris-pardo: ≥ 3:1 (<see cref="GlyphContrast"/>,
    /// ≈ 3,4:1), y además lleva el halo de tinta del sprite (07 §7.8: no solo color).</para>
    /// <para>Geometría (dp, desde la esquina superior izquierda de <see cref="Root"/>): la píldora mide el alto pedido
    /// (<see cref="HeightDp"/> o <see cref="CompactHeightDp"/>) con los extremos en semicírculo y la sombra asoma por debajo
    /// (<see cref="ShadowFraction"/> del alto); el ancho es <see cref="AspectRatio"/> veces el alto. El sprite de la píldora
    /// (9-slice horizontal) se genera una sola vez para toda la app. Sin asignaciones salvo al crearlo.</para>
    /// </remarks>
    internal sealed class MurkyChip
    {
        /// <summary>Alto de la píldora (dp): el de la píldora del chip de dificultad (07 §7.7).</summary>
        public const float HeightDp = ProceduralArt.LabelPillDp;

        /// <summary>Alto de la píldora en la variante compacta del HUD (dp; la del chip de dificultad compacto).</summary>
        public const float CompactHeightDp = DifficultyChip.CompactPillDp;

        /// <summary>Ancho / alto de la píldora [P].</summary>
        public const float AspectRatio = 1.6f;

        /// <summary>Sombra bajo la píldora respecto a su alto (2 dp con 26, como la del chip de dificultad).</summary>
        public const float ShadowFraction = ProceduralArt.LabelPillShadowDp / ProceduralArt.LabelPillDp;

        /// <summary>Lado del sprite del «?» respecto al alto de la píldora: el glifo ocupa ≈ 89 % del sprite (≈ 0,69 del alto).</summary>
        public const float GlyphFraction = 0.78f;

        // Sprite de la píldora a 3 px/dp: 40 × (26 + 2) dp con 13 dp de 9-slice a cada lado (el radio).
        private const float PxPerDp = ProceduralArt.UiPxPerDp;
        private const float SpriteWidthDp = 40f;

        private static Sprite? s_pill;

        private readonly RectTransform _root;
        private readonly Image _pill;
        private readonly Image _glyph;
        private float _height = HeightDp;

        private MurkyChip(Transform parent, ProceduralArt art)
        {
            _root = UiBuilder.Rect("MurkyChip", parent);
            _pill = UiBuilder.Sliced("Pill", _root, PillSprite(), Color.white);
            _glyph = UiBuilder.Image("Glyph", _root, art.QuestionMark, Color.white);
            _glyph.preserveAspect = true;
            _root.gameObject.SetActive(false);
            Layout(HeightDp);
        }

        /// <summary>Gris-pardo de la cara: el de las dosis ocultas (<see cref="Palette.Murky"/>, 07 §4.3 y §7.7).</summary>
        public static Color PillColor => Palette.Murky;

        /// <summary>Marfil del «?» (<see cref="Palette.UiPaper"/>), como el de las dosis ocultas (07 §7.7).</summary>
        public static Color GlyphColor => Palette.UiPaper;

        /// <summary>Contenedor del chip (abarca la píldora y su sombra).</summary>
        public RectTransform Root => _root;

        /// <summary>El chip se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Alto de la píldora colocada (dp, sin la sombra).</summary>
        public float Height => _height;

        /// <summary>Ancho de la píldora colocada (dp).</summary>
        public float Width => WidthFor(_height);

        /// <summary>Sprite del «?» (<c>mech_question</c>).</summary>
        public Sprite? GlyphSprite => _glyph.sprite;

        /// <summary>Rectángulo del «?» (tests).</summary>
        public RectTransform GlyphRect => _glyph.rectTransform;

        /// <summary>Píldora (tests).</summary>
        public Image Pill => _pill;

        /// <summary>Crea el chip, oculto, bajo <paramref name="parent"/>.</summary>
        public static MurkyChip Create(Transform parent, ProceduralArt art)
        {
            return new MurkyChip(parent, art);
        }

        /// <summary>Ancho de la píldora para un alto <paramref name="height"/> (dp).</summary>
        public static float WidthFor(float height)
        {
            return Mathf.Round(height * AspectRatio);
        }

        /// <summary>Contraste del «?» marfil sobre el gris-pardo de la cara (≥ 3:1 para un gráfico; 07 §7.8).</summary>
        public static float GlyphContrast()
        {
            return ColorMath.ContrastRatio(GlyphColor, PillColor);
        }

        /// <summary>Muestra u oculta el chip.</summary>
        public void SetVisible(bool visible)
        {
            if (_root.gameObject.activeSelf != visible)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Coloca la píldora con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="yTop"/>) del padre y
        /// con <paramref name="height"/> dp de alto (<see cref="HeightDp"/> o <see cref="CompactHeightDp"/>); la sombra asoma por
        /// debajo.
        /// </summary>
        public void Place(float x, float yTop, float height = HeightDp)
        {
            if (!Mathf.Approximately(height, _height))
            {
                Layout(height);
            }

            UiBuilder.Place(_root, x, yTop, WidthFor(height), height * (1f + ShadowFraction));
        }

        private void Layout(float height)
        {
            _height = height;
            float w = WidthFor(height);
            _pill.pixelsPerUnitMultiplier = HeightDp / height; // el 9-slice (el radio) sigue a la mitad del alto
            UiBuilder.Place(_pill.rectTransform, 0f, 0f, w, height * (1f + ShadowFraction));
            float glyph = height * GlyphFraction;
            UiBuilder.Place(_glyph.rectTransform, (w - glyph) * 0.5f, (height - glyph) * 0.5f, glyph, glyph);
        }

        // Píldora gris-pardo con la pintura de ProceduralArt.DifficultyPill (contorno oscurecido 0,42, luz en el 42 % de arriba,
        // canto oscurecido 0,16 abajo, filo de luz arriba y sombra de 2 dp), ya coloreada. Una sola textura para toda la app (se
        // rehace si Unity la destruyó).
        private static Sprite PillSprite()
        {
            if (s_pill != null)
            {
                return s_pill;
            }

            const float pillH = HeightDp;
            const float hDp = HeightDp * (1f + ShadowFraction);
            int w = Mathf.RoundToInt(SpriteWidthDp * PxPerDp);
            int h = Mathf.RoundToInt(hDp * PxPerDp);
            var pixels = new Color32[w * h];
            Color color = PillColor;
            Color outline = ColorMath.Darken(color, 0.42f);
            Color light = ColorMath.Lighten(color, 0.24f);
            Color low = ColorMath.Darken(color, 0.16f);
            float r = (pillH * 0.5f) - 0.5f;
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / PxPerDp); // dp desde arriba
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / PxPerDp;
                    Color c = new Color(0f, 0f, 0f, 0f);
                    float sh = Stadium(xd, yd - 1.6f, 0.8f, 0.5f, SpriteWidthDp - 0.8f, pillH - 0.5f, r);
                    c = Over(c, new Color(0f, 0f, 0f, 0.35f), Mathf.Clamp01(0.5f - (sh * PxPerDp / 2f)));
                    float d = Stadium(xd, yd, 0.5f, 0.5f, SpriteWidthDp - 0.5f, pillH - 0.5f, r);
                    float cov = Mathf.Clamp01(0.5f - (d * PxPerDp));
                    if (cov > 0f)
                    {
                        c = Over(c, outline, cov);
                        float inside = Mathf.Clamp01(0.5f - ((d + 1.4f) * PxPerDp));
                        if (inside > 0f)
                        {
                            float v = yd / pillH;
                            Color face = color;
                            if (v < 0.42f)
                            {
                                face = Color.Lerp(light, color, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.04f, 0.42f, v)));
                            }
                            else if (v > 0.8f)
                            {
                                face = Color.Lerp(color, low, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 0.98f, v)));
                            }

                            float fromEdge = -(d + 1.4f);
                            if (fromEdge < 1.2f && v < 0.5f)
                            {
                                face = Color.Lerp(face, Color.white, 0.35f * (1f - (fromEdge / 1.2f)));
                            }

                            face.a = 1f;
                            c = Over(c, face, inside);
                        }
                    }

                    pixels[(y * w) + x] = c;
                }
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "label_pill_murky",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, false); // legible: 120 × 84 px, y los tests miran su color
            float border = (pillH * 0.5f) * PxPerDp;
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f * PxPerDp, 0, SpriteMeshType.FullRect, new Vector4(border, 0f, border, 0f));
            sprite.name = "label_pill_murky";
            sprite.hideFlags = HideFlags.DontSave;
            s_pill = sprite;
            return sprite;
        }

        // Distancia con signo (dp) a una píldora horizontal de [x0, x1] × [y0, y1] con extremos de radio r.
        private static float Stadium(float px, float py, float x0, float y0, float x1, float y1, float r)
        {
            float cy = (y0 + y1) * 0.5f;
            float cx = Mathf.Clamp(px, x0 + r, x1 - r);
            float dx = px - cx;
            float dy = py - cy;
            return Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
        }

        // «Encima» con cobertura: c sobre dst con alfa c.a × coverage.
        private static Color Over(Color dst, Color c, float coverage)
        {
            float a = c.a * coverage;
            if (a <= 0f)
            {
                return dst;
            }

            float outA = a + (dst.a * (1f - a));
            if (outA <= 0f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            float k = dst.a * (1f - a);
            return new Color(((c.r * a) + (dst.r * k)) / outA, ((c.g * a) + (dst.g * k)) / outA, ((c.b * a) + (dst.b * k)) / outA, outA);
        }
    }
}
