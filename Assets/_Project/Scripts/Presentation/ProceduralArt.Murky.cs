#nullable enable
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de las Aguas turbias (07 §4.3; 02 §11 T3; propuesta de D-007 b.2; D-003): la fila turbia del atlas
    /// de patrones que lee el shader del líquido (<see cref="DrawMurkyRow"/>: trama diagonal y «?» marfil de 0,5 u con su halo),
    /// la insignia «?×n» de la cuia turbia (<see cref="MurkyBadgePill"/> y los glifos <see cref="MurkyBadgeQuestion"/>,
    /// <see cref="MurkyBadgeTimes"/> y <see cref="MurkyBadgeDigit"/>, en sprite porque AF.Presentation no tiene TMP) y el disco
    /// marfil sobre el que la Luciérnaga muestra el icono de cada fruta (<see cref="MurkyFruitDisc"/>).
    /// </summary>
    /// <remarks>
    /// La insignia se genera de forma perezosa la primera vez que se pide (un nivel sin turbias no crea sus texturas): una
    /// píldora 9-slice y una tira con los 11 glifos en una sola textura (cada glifo es un <see cref="Sprite"/> de esa tira),
    /// así que son dos texturas para toda la familia; el disco, la primera vez que se usa la Luciérnaga (una más).
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Fila turbia del atlas de patrones (<see cref="PatternAtlas"/>), tras las de los 14 tintes: es la <c>TEXCOORD3.a</c>
        /// de las dosis turbias en el shader del líquido. [P] Sustituye a la máscara <c>_HiddenMask</c> de 08 §5.2 (que no puede
        /// ir como propiedad: uGUI no admite <c>MaterialPropertyBlock</c> por cuia) y, con el «?» que lleva en R y G, al
        /// «sprite "?"» de 08 §5.2; se alinea con el GDD en el paso 2. Nunca es la fila de una fruta (1-14), así que no delata
        /// la fruta oculta.
        /// </summary>
        public const int MurkyPatternRow = Cuia.MaxTintColor + 1;

        /// <summary>Paso de la trama diagonal en x y en y (px de la fila turbia; 0,25 u): dos rayas por tesela de 0,5 u.</summary>
        public const float MurkyHatchPeriodPx = 32f;

        /// <summary>Medio grosor de cada raya de la trama, en perpendicular (px de la fila turbia; ≈ 0,05 u de grosor) [P].</summary>
        public const float MurkyHatchHalfWidthPx = 3f;

        /// <summary>Alto de la insignia «?×n» (dp) [P]: cabe en la cara de la repisa bajo el pie (guía 4.5).</summary>
        public const float MurkyBadgeDp = 14f;

        /// <summary>Ancho de la insignia «?×n» (dp) [P].</summary>
        public const float MurkyBadgeWidthDp = 30f;

        /// <summary>Ancho de una celda de glifo de la insignia (dp): el glifo mide 6 × 9,3 dp en su centro.</summary>
        public const float MurkyBadgeCellDp = 10f;

        /// <summary>
        /// Relleno de la insignia, gris pardo oscuro #3A332B [P] (07 §7.7, «gris-pardo»): ΔE00 ≥ 15 frente a los 14 tintes (el
        /// más cercano, Cacao) y 26 frente al pardo de las turbias (direccion-visual-v2 §2.4, regla 1).
        /// </summary>
        public static readonly Color MurkyBadgeFill = Palette.Hex(0x3A332B);

        /// <summary>
        /// Filo del disco de la fruta de la Luciérnaga: <c>madera_contorno</c> (<see cref="Palette.FamilyWood"/>) [P]. Con el
        /// relleno marfil <see cref="Palette.UiPaper"/>, el disco queda a ≥ 3:1 de cualquier tinte (direccion-visual-v2 §2.4,
        /// regla 2): el marfil frente a los oscuros y el filo frente a los claros.
        /// </summary>
        public static readonly Color MurkyFruitDiscRim = Palette.FamilyWood.Outline;

        // Celdas de la tira de glifos de la insignia (px a 3 px/dp): 30 × 42 px (10 × 14 dp), con el glifo de 18 × 28 px.
        private const int BadgeCellPx = 30;
        private const int BadgeHeightPx = 42;
        private const int BadgeGlyphCount = 11; // «?», «×» y los dígitos 1-9
        private const float BadgeStrokePx = 2.3f;

        // Disco de la fruta de la Luciérnaga: 0,80 u ≈ 19-26 dp (u de 24 a 32 dp); 72 px ≈ 24 dp a 3 px/dp, con un filo de
        // 1,5 dp (4,5 px) y una sombra de 1 dp (3 px) hacia abajo [P].
        private const int FruitDiscPx = 72;
        private const float FruitDiscRimPx = 4.5f;
        private const float FruitDiscShadowPx = 3f;

        private Sprite? _murkyBadgePill;
        private Sprite[]? _murkyBadgeGlyphs;
        private Sprite? _murkyFruitDisc;

        /// <summary>
        /// Píldora de la insignia «?×n» (9-slice en horizontal, <see cref="MurkyBadgeDp"/> de alto a 3 px/dp): relleno
        /// <see cref="MurkyBadgeFill"/>, filo marfil de 1 dp y un brillo suave arriba (la luz de juego de D-003).
        /// </summary>
        public Sprite MurkyBadgePill => _murkyBadgePill != null ? _murkyBadgePill : (_murkyBadgePill = BuildMurkyBadgePill());

        /// <summary>
        /// Disco bajo el icono de la fruta que muestra la Luciérnaga (04 §2.5; la regla de iconos ≥ 3:1 de direccion-visual-v2
        /// §2.4): relleno marfil <see cref="Palette.UiPaper"/>, filo de 1,5 dp <see cref="MurkyFruitDiscRim"/>, sombra de 1 dp y
        /// un brillo suave arriba (D-003). Se genera la primera vez que se pide.
        /// </summary>
        public Sprite MurkyFruitDisc => _murkyFruitDisc != null ? _murkyFruitDisc : (_murkyFruitDisc = BuildMurkyFruitDisc());

        /// <summary>Glifo «?» marfil de la insignia (celda de 10 × 14 dp).</summary>
        public Sprite MurkyBadgeQuestion => MurkyBadgeGlyphs()[0];

        /// <summary>Glifo «×» marfil de la insignia (celda de 10 × 14 dp).</summary>
        public Sprite MurkyBadgeTimes => MurkyBadgeGlyphs()[1];

        /// <summary>Dígito marfil <paramref name="n"/> de la insignia (1-9; fuera de rango se recorta).</summary>
        public Sprite MurkyBadgeDigit(int n)
        {
            return MurkyBadgeGlyphs()[1 + Mathf.Clamp(n, 1, 9)];
        }

        /// <summary>
        /// Fila turbia del atlas (<see cref="PatternTilePx"/> × <see cref="PatternRowPx"/> px = 0,5 × 1 u, de abajo arriba):
        /// <list type="bullet">
        /// <item><description>A: la trama diagonal «/» (rayas cada <see cref="MurkyHatchPeriodPx"/> px y
        /// 2 · <see cref="MurkyHatchHalfWidthPx"/> de grosor), repetible en los dos ejes; el shader la pone al 25 % hacia la
        /// tinta (07 §4.3).</description></item>
        /// <item><description>R: el «?» marfil de 0,5 u (64 px), centrado en la fila; G: su halo de tinta, 1,6 · k px más
        /// grueso (como el «?» de los iconos). Una sola vez: el shader lo lee en una ventana de 0,5 u centrada en la
        /// banda.</description></item>
        /// </list>
        /// Aparte del atlas para que los tests de EditMode lo midan (la textura no es legible).
        /// </summary>
        internal static PixelCanvas DrawMurkyRow()
        {
            const int w = PatternTilePx;
            const int h = PatternRowPx;
            var glyph = new PixelCanvas(w, h);
            var halo = new PixelCanvas(w, h);
            const float glyphPx = QuestionGlyphU * h; // 0,5 u en una fila de 1 u
            const float k = glyphPx / QuestionGlyphHeight;
            float y0 = ((h - glyphPx) * 0.5f) - (QuestionGlyphBottom * k);
            DrawQuestionGlyph(halo, w * 0.5f, y0, k, Color.white, 1.6f * k);
            DrawQuestionGlyph(glyph, w * 0.5f, y0, k, Color.white, 0f);

            var row = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = Mathf.Repeat(x - y, MurkyHatchPeriodPx);
                    float across = Mathf.Min(t, MurkyHatchPeriodPx - t) * 0.70710678f; // distancia en perpendicular a la raya
                    float hatch = PixelCanvas.Coverage(across - MurkyHatchHalfWidthPx);
                    row.Set(x, y, new Color(glyph.Get(x, y).a, halo.Get(x, y).a, 0f, hatch));
                }
            }

            return row;
        }

        private Sprite[] MurkyBadgeGlyphs()
        {
            if (_murkyBadgeGlyphs != null && _murkyBadgeGlyphs[0] != null)
            {
                return _murkyBadgeGlyphs;
            }

            const int cell = BadgeCellPx;
            var pc = new PixelCanvas(cell * BadgeGlyphCount, BadgeHeightPx);
            Color ink = Palette.UiPaper;
            for (int i = 0; i < BadgeGlyphCount; i++)
            {
                DrawBadgeGlyph(pc, i * cell, i, ink);
            }

            Texture2D tex = pc.ToTexture("mech_murky_badge_glyphs");
            _owned.Add(tex);
            var sprites = new Sprite[BadgeGlyphCount];
            for (int i = 0; i < BadgeGlyphCount; i++)
            {
                Sprite s = Sprite.Create(tex, new Rect(i * cell, 0, cell, BadgeHeightPx), new Vector2(0.5f, 0.5f), 100f * UiPxPerDp, 0, SpriteMeshType.FullRect);
                s.name = i == 0 ? "mech_murky_badge_q" : (i == 1 ? "mech_murky_badge_x" : "mech_murky_badge_" + (i - 1));
                s.hideFlags = HideFlags.DontSave;
                _owned.Add(s);
                sprites[i] = s;
            }

            _murkyBadgeGlyphs = sprites;
            return sprites;
        }

        private Sprite BuildMurkyFruitDisc()
        {
            const int s = FruitDiscPx;
            const float c = s * 0.5f;
            const float r = c - FruitDiscShadowPx - 0.5f;
            var pc = new PixelCanvas(s, s);
            pc.FillCircle(c, c - FruitDiscShadowPx, r, ColorMath.WithAlpha(Color.black, 0.35f)); // sombra de 1 dp hacia abajo
            pc.FillCircle(c, c, r, MurkyFruitDiscRim);
            pc.FillCircle(c, c, r - FruitDiscRimPx, Palette.UiPaper);
            pc.FillEllipseSoft(c, c + (r * 0.42f), r * 0.55f, r * 0.26f, 3f, ColorMath.WithAlpha(Color.white, 0.35f)); // luz de arriba
            return MakeSprite(pc, "mech_murky_fruit_disc", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildMurkyBadgePill()
        {
            const int w = 64;
            const int h = BadgeHeightPx;
            const float r = h * 0.5f;
            var pc = new PixelCanvas(w, h);
            pc.FillRoundedRect(0f, 0f, w, h, r, ColorMath.WithAlpha(Palette.UiPaper, 0.85f));
            pc.FillRoundedRect(3f, 3f, w - 3f, h - 3f, r - 3f, MurkyBadgeFill);
            pc.FillRoundedRectSoft(7f, h * 0.56f, w - 7f, h - 6f, (h * 0.22f) - 1f, 3f, ColorMath.WithAlpha(Color.white, 0.10f)); // brillo de arriba
            return MakeSprite(pc, "mech_murky_badge_pill", new Vector2(0.5f, 0.5f), new Vector4(r + 1f, 0f, r + 1f, 0f), UiPxPerDp);
        }

        // Glifo i de la insignia en la celda que empieza en ox: 0 «?», 1 «×», 2..10 los dígitos 1-9, en un recuadro de 18 × 28
        // px centrado en la celda (origen abajo a la izquierda), con trazos de 2 · BadgeStrokePx px (≈ 1,5 dp).
        private static void DrawBadgeGlyph(PixelCanvas pc, float ox, int i, Color c)
        {
            float bx = ox + ((BadgeCellPx - 18f) * 0.5f);
            const float by = (BadgeHeightPx - 28f) * 0.5f;
            const float s = BadgeStrokePx;
            switch (i)
            {
                case 0:
                {
                    const float k = 28f / QuestionGlyphHeight;
                    DrawQuestionGlyph(pc, ox + (BadgeCellPx * 0.5f), by - (QuestionGlyphBottom * k), k, c, 0.25f);
                    break;
                }

                case 1: // «×», a la altura de media cifra
                {
                    float cx = ox + (BadgeCellPx * 0.5f);
                    float cy = by + 12f;
                    pc.FillCapsule(cx - 4.6f, cy - 4.6f, cx + 4.6f, cy + 4.6f, s * 0.85f, c);
                    pc.FillCapsule(cx - 4.6f, cy + 4.6f, cx + 4.6f, cy - 4.6f, s * 0.85f, c);
                    break;
                }

                default:
                    DrawBadgeDigit(pc, bx, by, i - 1, s, c);
                    break;
            }
        }

        // Cifra n (1-9) de trazo redondeado en el recuadro de 18 × 28 px con origen (bx, by).
        private static void DrawBadgeDigit(PixelCanvas pc, float bx, float by, int n, float s, Color c)
        {
            switch (n)
            {
                case 1:
                    pc.FillCapsule(bx + 10.5f, by + 2.3f, bx + 10.5f, by + 25.7f, s, c);
                    pc.FillCapsule(bx + 10.5f, by + 25.7f, bx + 4.5f, by + 20.5f, s, c);
                    break;
                case 2:
                    pc.StrokeArc(bx + 9f, by + 19.2f, 6.4f, 2f * s, -38f, 168f, c);
                    pc.FillCapsule(bx + 9f + (6.4f * 0.788f), by + 19.2f - (6.4f * 0.616f), bx + 2.6f, by + 2.3f, s, c);
                    pc.FillCapsule(bx + 2.6f, by + 2.3f, bx + 15.6f, by + 2.3f, s, c);
                    break;
                case 3:
                    pc.StrokeArc(bx + 9f, by + 20f, 5.6f, 2f * s, -90f, 155f, c);
                    pc.StrokeArc(bx + 9f, by + 8.4f, 6.1f, 2f * s, -155f, 90f, c);
                    break;
                case 4:
                    pc.FillCapsule(bx + 12.6f, by + 25.7f, bx + 2.4f, by + 8.6f, s, c);
                    pc.FillCapsule(bx + 2.4f, by + 8.6f, bx + 16f, by + 8.6f, s, c);
                    pc.FillCapsule(bx + 12.6f, by + 25.7f, bx + 12.6f, by + 2.3f, s, c);
                    break;
                case 5:
                    pc.FillCapsule(bx + 15f, by + 25.7f, bx + 4.2f, by + 25.7f, s, c);
                    pc.FillCapsule(bx + 4.2f, by + 25.7f, bx + 3.6f, by + 15.2f, s, c);
                    pc.StrokeArc(bx + 9f, by + 9.2f, 6.6f, 2f * s, -150f, 128f, c);
                    break;
                case 6:
                    pc.StrokeArc(bx + 9f, by + 8.6f, 6.2f, 2f * s, 0f, 360f, c);
                    DrawBadgeCurve(pc, new Vector2(bx + 14f, by + 25.7f), new Vector2(bx + 3.2f, by + 22f), new Vector2(bx + 2.8f, by + 9f), s, c);
                    break;
                case 7:
                    pc.FillCapsule(bx + 2.4f, by + 25.7f, bx + 15.6f, by + 25.7f, s, c);
                    pc.FillCapsule(bx + 15.6f, by + 25.7f, bx + 6.2f, by + 2.3f, s, c);
                    break;
                case 8:
                    pc.StrokeArc(bx + 9f, by + 20.1f, 5.3f, 2f * s, 0f, 360f, c);
                    pc.StrokeArc(bx + 9f, by + 8.2f, 6.1f, 2f * s, 0f, 360f, c);
                    break;
                default: // 9
                    pc.StrokeArc(bx + 9f, by + 19.4f, 6.2f, 2f * s, 0f, 360f, c);
                    DrawBadgeCurve(pc, new Vector2(bx + 15.2f, by + 19f), new Vector2(bx + 15f, by + 6f), new Vector2(bx + 4f, by + 2.3f), s, c);
                    break;
            }
        }

        // Trazo curvo (cuadrática a → c → b) de radio s.
        private static void DrawBadgeCurve(PixelCanvas pc, Vector2 a, Vector2 c, Vector2 b, float s, Color color)
        {
            Vector2 prev = a;
            for (int i = 1; i <= 16; i++)
            {
                Vector2 p = Quad(a, c, b, i / 16f);
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, s, color);
                prev = p;
            }
        }
    }
}
