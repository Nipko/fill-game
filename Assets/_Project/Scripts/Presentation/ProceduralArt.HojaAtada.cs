#nullable enable
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Hoja atada (variante de la Hoja de la 1.0, estreno en el 283; 02 §9.1 «Variantes» y §1.2;
    /// propuesta de D-007 b.3: «hoja pequeña atada al cuello con fibra, con icono grande; nunca cae»; tarea VA1a). En el tablero
    /// reutiliza la hoja de palma de la Hoja (<see cref="HojaPalm"/>), más pequeña y colgada del cuello, y su medallón
    /// (<see cref="HojaBadge"/>). Lo propio es el cordel (<see cref="HojaAtadaCord"/>: el nudo de la Liana con un tramo delantero
    /// que ciñe el cuello justo bajo el borde de fibra, teñido de fibra, <see cref="HojaCordTint"/>, para que se lea como un
    /// cordel y no como una liana que bloquea, y sin colgar sobre la boca) y la llave del medallón:
    /// una muestra de la dosis (<see cref="HojaAtadaSwatch"/>), un disco del tinte con el mismo patrón y
    /// el mismo icono de accesibilidad que las capas, porque el medallón es el único sitio que dice qué fruta acepta (revisión de
    /// VA1a: con la fruta en vuelo, de otro color que su líquido, el aguaje se confundía con el urucum o el camu-camu).
    /// Aquí van también el icono de la mecánica (<see cref="MechanicIcon"/> de 32 dp y el grande del estreno,
    /// <see cref="CardHeroIcon"/>), con lo que se ve en el tablero: el cuello de la cuia, el cordel trenzado que le da la vuelta,
    /// el medallón colgado del nudo en el hombro derecho y la hoja pequeña colgando.
    /// </summary>
    /// <remarks>
    /// Color (direccion-visual-v2 §2.4, regla 1): la hoja usa los colores de la Hoja (a ΔE00 ≥ 10 de los 14 tintes); el cordel,
    /// tonos <c>fibra_*</c>, que la regla deja fuera; y la muestra lleva su tinte a propósito, como la fruta llave. En el icono
    /// (UI) no hay tintes (07 §3, regla 6): el medallón lleva una fruta genérica en tinta. Lo sustituye el arte del
    /// artista; el nombre «Hoja atada» está pendiente del asesor cultural (glosario §6.5).
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Tramo delantero del cordel de la Hoja atada (<see cref="HojaAtadaCord"/>), en y desde el centro de su sprite (u) [P]:
        /// con el sprite en <c>HojaOverlay.TiedCordAboveCavityU</c>, a 0,02 u bajo el borde de fibra. Del canto izquierdo del
        /// cuello (<see cref="CuiaGeometry.NeckOuterHalfWidthU"/>) al nudo, que es el de la Liana
        /// (<see cref="LianaCollarKnotXU"/>, <see cref="LianaCollarKnotYU"/>), sin colgar: la boca queda libre entre las dos
        /// paredes de vidrio (revisión de VA1a, captura 54e: el arco que colgaba sobre la boca se leía como una barra).
        /// </summary>
        public const float HojaCordFrontYU = 0.07f;

        /// <summary>
        /// Icono de la Hoja atada: margen de la caja del grupo por cada lado (fracción del lado del lienzo) [P]. En la tarjeta, el
        /// icono de 48 dp va en el disco del medallón (≈ 61 dp por dentro): el grupo, de ≈ 45 dp, queda centrado en el disco con
        /// ≈ 13 % de margen (revisión de VA1a, captura 62k).
        /// </summary>
        public const float HojaGlyphMargin = 0.03f;

        // Icono de la Hoja atada (DrawHojaAtadaGlyph), en u de la cuia desde el eje y desde el techo de la cavidad: el centro del
        // cordel (bajo el borde de fibra, con un hilo de vidrio entre los dos para que no se fundan), su grosor (≈ 3 dp en el
        // icono grande), el nudo (el de la Liana), la placa del medallón (≈ 1,1 veces la del tablero), lo que asoma el hombro
        // izquierdo en la caja del grupo y el tramo en que el hombro se desvanece hacia abajo.
        private const float HojaGlyphCordYU = 0.60f;
        private const float HojaGlyphCordU = 0.19f;
        private const float HojaGlyphKnotXU = LianaCollarKnotXU;
        private const float HojaGlyphPlateWU = 1.15f;
        private const float HojaGlyphPlateHU = 0.92f;
        private const float HojaGlyphShoulderXU = CuiaGeometry.ShoulderOuterHalfWidthU;
        private const float HojaGlyphFadeFromU = -0.12f;
        private const float HojaGlyphFadeToU = 0.12f;

        private Sprite? _hojaAtadaCord;
        private Sprite? _hojaAtadaCordShine;

        /// <summary>
        /// Cordel de la Hoja atada (<see cref="LianaCollarWidthU"/> × <see cref="LianaCollarHeightU"/> u, como la vuelta del
        /// cuello de la Liana y con el mismo nudo), en grises: lo tiñe <see cref="HojaCordTint"/>. Su tramo delantero va recto,
        /// en <see cref="HojaCordFrontYU"/>, del canto izquierdo del cuello al nudo (<see cref="HojaAtadaCordPointU"/>).
        /// </summary>
        internal Sprite HojaAtadaCord => _hojaAtadaCord != null ? _hojaAtadaCord : (_hojaAtadaCord = BuildHojaAtadaCord(false));

        /// <summary>Luz de borde de 1 dp del cordel de la Hoja atada (blanca, del tamaño de <see cref="HojaAtadaCord"/>).</summary>
        internal Sprite HojaAtadaCordShine => _hojaAtadaCordShine != null ? _hojaAtadaCordShine : (_hojaAtadaCordShine = BuildHojaAtadaCord(true));

        /// <summary>
        /// Punto <paramref name="t"/> del tramo delantero del cordel de la Hoja atada (0 en el canto izquierdo del cuello, 1 junto
        /// al nudo), en u desde el centro de su sprite: casi recto, de <see cref="HojaCordFrontYU"/> a la entrada del nudo.
        /// </summary>
        public static Vector2 HojaAtadaCordPointU(float t)
        {
            var a = new Vector2(-CuiaGeometry.NeckOuterHalfWidthU, HojaCordFrontYU);
            var b = new Vector2(LianaCollarKnotXU - 0.06f, LianaCollarKnotYU - 0.01f);
            return Vector2.Lerp(a, b, Mathf.Clamp01(t));
        }

        /// <summary>
        /// Tinte del cordel de fibra de la Hoja atada (<see cref="HojaAtadaCord"/>, en grises) [P]:
        /// <see cref="Palette.FiberBase"/>, así el tallo (×0,88) queda en fibra media y el nudo (×0,70), más oscuro; más oscuro
        /// que el borde de fibra trenzado de la cuia, para no confundirse con él.
        /// </summary>
        public static readonly Color HojaCordTint = Palette.FiberBase;

        /// <summary>Luz de borde del cordel de la Hoja atada (sobre <see cref="HojaAtadaCordShine"/>) [P].</summary>
        public static readonly Color HojaCordShine = ColorMath.WithAlpha(Palette.FiberLight, 0.85f);

        /// <summary>
        /// Píxeles por u de la muestra de la Hoja atada: los del atlas de patrones (una tesela de <see cref="PatternTilePx"/> px
        /// mide <see cref="PatternTileU"/> u), así el patrón y el icono de accesibilidad (celda de 0,5 u =
        /// <see cref="FruitIconPx"/> px) van a la misma escala que en las capas, sin remuestrear.
        /// </summary>
        public const float HojaAtadaSwatchPxPerU = PatternTilePx / PatternTileU;

        /// <summary>
        /// Diámetro del disco de la muestra (u) [P]: unos 16 dp con las cuias de 47-48 dp de los tableros de 3 filas (D-006),
        /// dentro de la placa del medallón (0,8 u de alto a escala 1,05 en la Hoja atada).
        /// </summary>
        public const float HojaAtadaSwatchU = SwatchDiscPx / HojaAtadaSwatchPxPerU;

        /// <summary>Lado del sprite de la muestra (u): el disco más su margen de antialias.</summary>
        public const float HojaAtadaSwatchSpriteU = SwatchSpritePx / HojaAtadaSwatchPxPerU;

        // Disco de la muestra y su sprite (px a HojaAtadaSwatchPxPerU) y el canto oscuro del disco (≈ 0,8 dp a 23,5 dp por u).
        private const int SwatchDiscPx = 88;
        private const int SwatchSpritePx = SwatchDiscPx + 4;
        private const float SwatchEdgePx = 4.5f;

        private readonly Sprite?[] _hojaAtadaSwatches = new Sprite?[Cuia.MaxTintColor + 1];

        /// <summary>
        /// Muestra de la dosis del tinte <paramref name="tint"/> (1-14) para el medallón de la Hoja atada [P]: un disco de
        /// <see cref="HojaAtadaSwatchU"/> u del tinte, con su patrón (<see cref="Palette.PatternMark(Color, int, float)"/> a la
        /// opacidad de los niveles con patrones, <see cref="Palette.PatternOpacityOn"/>: la Hoja atada llega en el 283) y su icono
        /// de accesibilidad derecho (<see cref="FruitIconMask"/> en <see cref="Palette.PatternBase"/> al
        /// <see cref="LiquidGraphic.FruitIconOpacity"/>, con su contorno si lo lleva), como una dosis vista de frente; canto del
        /// tinte oscurecido y un brillo arriba a la izquierda. Perezosa: la primera vez que el overlay enseña esa llave (al
        /// cargar el nivel), nunca en un paso.
        /// </summary>
        internal Sprite HojaAtadaSwatch(int tint)
        {
            int t = Mathf.Clamp(tint, Cuia.MinColor, Cuia.MaxTintColor);
            Sprite? cached = _hojaAtadaSwatches[t];
            if (cached != null)
            {
                return cached;
            }

            Sprite sprite = BuildHojaAtadaSwatch(t);
            _hojaAtadaSwatches[t] = sprite;
            return sprite;
        }

        // El cordel de la Hoja atada: como la vuelta del cuello de la Liana (BuildLianaCollar, el mismo grosor, sombra, luz y
        // nudo), con el tramo delantero de HojaAtadaCordPointU.
        private Sprite BuildHojaAtadaCord(bool shine)
        {
            const float px = MechanicPxPerU;
            int w = Mathf.RoundToInt(LianaCollarWidthU * px);
            int h = Mathf.RoundToInt(LianaCollarHeightU * px);
            var pc = new PixelCanvas(w, h);
            var origin = new Vector2(w * 0.5f, h * 0.5f);
            Vector2 knot = origin + (new Vector2(LianaCollarKnotXU, LianaCollarKnotYU) * px);
            var pts = new Vector2[LianaCurveSteps + 1];
            var radii = new float[LianaCurveSteps + 1];
            for (int i = 0; i <= LianaCurveSteps; i++)
            {
                float t = i / (float)LianaCurveSteps;
                pts[i] = origin + (HojaAtadaCordPointU(t) * px);
                radii[i] = 0.5f * px * Mathf.Lerp(LianaStemEndU, LianaStemU, Mathf.Clamp01(t * 2.5f));
            }

            if (shine)
            {
                DrawLianaShine(pc, pts, radii, 0.25f, 0.82f);
                return MakeSprite(pc, "mech_hoja_atada_cord_shine", new Vector2(0.5f, 0.5f), Vector4.zero);
            }

            DrawLianaShadow(pc, pts, radii, null, null, 0f, 0f);
            DrawKnotShadow(pc, knot, px);
            DrawLianaStem(pc, pts, radii);
            DrawLianaKnot(pc, knot, px);
            return MakeSprite(pc, "mech_hoja_atada_cord", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildHojaAtadaSwatch(int tint)
        {
            const int n = SwatchSpritePx;
            const int cell = FruitIconPx;
            float c = n * 0.5f;
            float r = SwatchDiscPx * 0.5f;

            // El patrón de las capas, en blanco con su cobertura en alfa, a la misma escala (teselas repetidas sin costuras).
            var pattern = new PixelCanvas(n, n);
            if (HasPattern(tint))
            {
                for (int ox = -PatternTilePx; ox <= n; ox += PatternTilePx)
                {
                    for (int oy = -PatternTilePx; oy <= n; oy += PatternTilePx)
                    {
                        DrawPatternTile(pattern, tint, ox, oy);
                    }
                }
            }

            float[] mask = FruitIconMask(tint);
            float[] rim = FruitIconRimMask(tint);
            Color dose = Palette.Tint(tint);
            Color mark = Palette.PatternMark(dose, tint, Palette.PatternOpacityOn);
            Color glyph = Color.Lerp(dose, Palette.PatternBase(tint), LiquidGraphic.FruitIconOpacity);
            Color rimColor = ColorMath.Darken(dose, LiquidGraphic.FruitIconRimDarken);
            Color edge = ColorMath.Darken(dose, 0.38f);
            int offset = (n - cell) / 2; // la celda del icono (0,5 u), centrada en el disco
            var pc = new PixelCanvas(n, n);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) - c;
                    float dy = (y + 0.5f) - c;
                    float dist = Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
                    float cover = PixelCanvas.Coverage(dist);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    Color col = Color.Lerp(dose, mark, pattern.Get(x, y).a);
                    int gx = x - offset;
                    int gy = y - offset;
                    if (gx >= 0 && gy >= 0 && gx < cell && gy < cell)
                    {
                        int i = (gy * cell) + gx;
                        col = Color.Lerp(col, rimColor, rim[i]);
                        col = Color.Lerp(col, glyph, mask[i]);
                    }

                    col = Color.Lerp(col, edge, Mathf.Clamp01(dist + SwatchEdgePx + 0.5f));
                    col.a = 1f;
                    pc.Blend(x, y, col, cover);
                }
            }

            pc.FillEllipseSoft(c - (r * 0.42f), c + (r * 0.52f), r * 0.30f, r * 0.13f, 1.5f, new Color(1f, 1f, 1f, 0.42f));
            return MakeSprite(pc, "mech_hoja_atada_swatch_" + TintIds[tint], new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Icono de la Hoja atada en un lienzo cuadrado de s px (k = s / 96; y hacia arriba), con los elementos del tablero y en su
        // sitio (revisión de VA1a, para que la tarjeta y el tablero rimen y se vea a qué va atada): el cuello de una cuia de vidrio
        // (el perfil de CuiaGeometry, con el borde de fibra trenzada y el hombro que se desvanece hacia abajo), el cordel que le
        // da la vuelta bajo el borde, trenzado en chevrones de dos tonos como el borde (≈ 3 dp en el icono grande), con su nudo en
        // el lado derecho; del nudo cuelga el medallón en el hombro derecho (placa menta con una fruta genérica en tinta: sin
        // tintes de fruta en la UI, 07 §3, regla 6; la fruta llave la dice el medallón del tablero) y, de la placa, la hoja de
        // palma pequeña. Todo en u de la cuia, con la caja del grupo centrada en el lienzo y un margen de HojaGlyphMargin por lado.
        private static void DrawHojaAtadaGlyph(PixelCanvas pc, int s)
        {
            float k = s / 96f;

            // La caja del grupo en u (x desde el eje de la cuia, y desde el techo de la cavidad): del hombro izquierdo al canto
            // derecho de la placa y de la punta de la hoja a lo alto del borde.
            float knotX = HojaGlyphKnotXU;
            float knotY = HojaGlyphCordYU + 0.02f;
            float plateX0 = knotX - 0.12f;
            float plateX1 = plateX0 + HojaGlyphPlateWU;
            float plateY1 = knotY - 0.04f;
            float plateY0 = plateY1 - HojaGlyphPlateHU;
            var leafBase = new Vector2(plateX1 - 0.30f, plateY0 + 0.06f);
            var leafTip = new Vector2(leafBase.x + 0.22f, leafBase.y - 0.60f);
            float minX = -HojaGlyphShoulderXU;
            float maxX = plateX1;
            float minY = leafTip.y - 0.04f;
            float maxY = CuiaGeometry.AboveCavityU;
            float u = s * (1f - (2f * HojaGlyphMargin)) / Mathf.Max(maxX - minX, maxY - minY);
            float ox = (0.5f * s) - (0.5f * (minX + maxX) * u);
            float oy = (0.5f * s) - (0.5f * (minY + maxY) * u);

            DrawGlyphNeck(pc, ox, oy, u);

            // El cordel: del canto izquierdo del cuello (un poco por fuera, donde da la vuelta) al nudo, trenzado.
            DrawBraidedCord(pc, ox - ((CuiaGeometry.NeckOuterHalfWidthU + 0.03f) * u), oy + (HojaGlyphCordYU * u), ox + (knotX * u), oy + (knotY * u), 0.5f * HojaGlyphCordU * u);

            // La hoja cuelga de debajo de la placa, con la panza hacia la derecha.
            var origin = new Vector2(ox, oy);
            DrawPalmLeaf(pc, origin + (leafBase * u), origin + (new Vector2(leafBase.x + 0.36f, leafBase.y - 0.22f) * u), origin + (leafTip * u), 0.17f * u,
                Palette.JungleMid, Palette.LeafDark, Palette.Vein, 0.8f * k);

            // La placa del medallón: sombra, canto, cara menta y brillo (f: la placa del dibujo de referencia medía 44 de alto).
            float px0 = ox + (plateX0 * u);
            float px1 = ox + (plateX1 * u);
            float py0 = oy + (plateY0 * u);
            float py1 = oy + (plateY1 * u);
            float f = (py1 - py0) / 44f;
            pc.FillRoundedRectSoft(px0 + (1f * f), py0 - (2.5f * f), px1 + (1f * f), py1 - (2.5f * f), 10f * f, 2f * f, new Color(0f, 0f, 0f, 0.35f));
            pc.FillRoundedRect(px0, py0, px1, py1, 10f * f, ColorMath.Darken(HojaBadgeColor, 0.45f));
            pc.FillRoundedRect(px0 + (2.2f * f), py0 + (2.2f * f), px1 - (2.2f * f), py1 - (2.2f * f), 8f * f, HojaBadgeColor);
            pc.FillEllipseSoft(px0 + (12f * f), py1 - (8f * f), 7f * f, 2.6f * f, 1.2f * f, new Color(1f, 1f, 1f, 0.75f));

            // El nudo, encima de la esquina de la placa (el medallón cuelga de él).
            float kx = ox + (knotX * u);
            float ky = oy + (knotY * u);
            float kr = 0.72f * HojaGlyphCordU * u;
            pc.FillCircle(kx, ky, kr, Palette.FiberShadow);
            pc.FillCircle(kx, ky + (0.15f * kr), 0.74f * kr, Palette.FiberBase);
            pc.FillCircle(kx - (0.22f * kr), ky + (0.37f * kr), 0.28f * kr, Palette.FiberLight);

            // La fruta genérica, en tinta, como los iconos de accesibilidad de las capas (sin aro alrededor, que con la placa se
            // leía como una cámara): un fruto redondo con su brillo, el rabito y una hoja verde.
            float cx = (0.5f * (px0 + px1)) - (1f * f);
            float cy = (0.5f * (py0 + py1)) - (3f * f);
            pc.FillCircle(cx, cy, 12.5f * f, Palette.UiInk);
            pc.FillCircle(cx - (2f * f), cy + (12f * f), 3.2f * f, HojaBadgeColor); // la muesca del rabito
            pc.FillCapsule(cx, cy + (9f * f), cx + (2.5f * f), cy + (17f * f), 1.6f * f, Palette.UiInk);
            DrawLeafShape(pc, new Vector2(cx + (2.5f * f), cy + (15f * f)), new Vector2(0.88f, 0.47f), 13f * f, 8f * f, Palette.LeafDark, 1f);
            DrawLeafShape(pc, new Vector2(cx + (3.2f * f), cy + (15.3f * f)), new Vector2(0.88f, 0.47f), 11f * f, 5.6f * f, Palette.JungleMid, 1f);
            pc.FillEllipseSoft(cx - (5f * f), cy + (4f * f), 2.4f * f, 3.6f * f, 0.8f * f, ColorMath.WithAlpha(Palette.UiPaper, 0.75f));
        }

        // Cuello de cuia del icono de la Hoja atada, con el eje en x = ox, el techo de la cavidad en y = oy y u px por u: el perfil
        // de CuiaGeometry (cuia de 4) desde el hombro hasta lo alto del borde, como la cuia de la tarjeta (DrawMiniCuia): canto
        // oscuro, vidrio claro, interior oscuro del panel (D-003) y el borde de fibra trenzada en chevrones. El hombro se desvanece
        // hacia abajo (de HojaGlyphFadeFromU a HojaGlyphFadeToU): se lee como el cuello de una cuia y no como un frasco entero.
        private static void DrawGlyphNeck(PixelCanvas pc, float ox, float oy, float u)
        {
            const int cap = 4;
            float top = CuiaGeometry.CavityTopU(cap);
            float lip = CuiaGeometry.LipU(cap);
            float height = CuiaGeometry.HeightU(cap);
            float line = Mathf.Max(1.4f, 0.09f * u);
            Color fiber = Palette.Fiber;
            Color fiberDark = ColorMath.Darken(Palette.Fiber, 0.32f);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(oy + (HojaGlyphFadeFromU * u)));
            int y1 = Mathf.Min(pc.Height - 1, Mathf.CeilToInt(oy + (CuiaGeometry.AboveCavityU * u) + 1f));
            int x0 = Mathf.Max(0, Mathf.FloorToInt(ox - (CuiaGeometry.ShoulderOuterHalfWidthU * u) - 2f));
            int x1 = Mathf.Min(pc.Width - 1, Mathf.CeilToInt(ox + (CuiaGeometry.ShoulderOuterHalfWidthU * u) + 2f));
            for (int y = y0; y <= y1; y++)
            {
                float yr = ((y + 0.5f) - oy) / u; // desde el techo de la cavidad
                float yu = top + yr;
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((yr - HojaGlyphFadeFromU) / (HojaGlyphFadeToU - HojaGlyphFadeFromU)));
                float outer = CuiaGeometry.OuterHalfWidth(yu, cap);
                float vy = fade * Mathf.Clamp01(((height - yu) * u) + 0.5f);
                if (vy <= 0f || outer <= 0f)
                {
                    continue;
                }

                float inner = CuiaGeometry.InnerHalfWidth(yu, cap);
                bool fiberBand = yu >= lip;
                for (int x = x0; x <= x1; x++)
                {
                    float xu = ((x + 0.5f) - ox) / u;
                    float dx = Mathf.Abs(xu);
                    float d = (dx - outer) * u; // px, negativa dentro
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, CardGlassEdge, cov * vy);
                    float inside = PixelCanvas.Coverage(d + line);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    Color fill;
                    if (fiberBand)
                    {
                        // Fibra trenzada en chevrones, como el borde de la cuia del tablero.
                        float chev = Mathf.Repeat((xu + Mathf.Abs(Mathf.Repeat(yu, 0.24f) - 0.12f)) / 0.12f, 1f);
                        fill = chev < 0.5f ? fiber : fiberDark;
                    }
                    else
                    {
                        fill = dx < inner ? Palette.Panel.Cavity : CardGlass;
                    }

                    pc.Blend(x, y, fill, inside * vy);
                }
            }
        }

        // Cordel trenzado de (ax, ay) a (bx, by) con radio r (px): canto oscuro y chevrones de dos tonos de fibra a lo largo, con
        // la punta del chevron en el centro del cordel, como la fibra del borde de la cuia (dos tonos cercanos: con más contraste
        // se leían como flechas).
        private static void DrawBraidedCord(PixelCanvas pc, float ax, float ay, float bx, float by, float r)
        {
            float lx = bx - ax;
            float ly = by - ay;
            float len = Mathf.Max(1e-3f, Mathf.Sqrt((lx * lx) + (ly * ly)));
            float tx = lx / len;
            float ty = ly / len;
            float period = Mathf.Max(3f, 1.6f * r);
            float edge = Mathf.Max(1f, 0.28f * r);
            Color strand = Palette.FiberBase;
            Color strandDark = ColorMath.Darken(Palette.FiberBase, 0.18f);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(ax, bx) - r - 2f));
            int x1 = Mathf.Min(pc.Width - 1, Mathf.CeilToInt(Mathf.Max(ax, bx) + r + 2f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(ay, by) - r - 2f));
            int y1 = Mathf.Min(pc.Height - 1, Mathf.CeilToInt(Mathf.Max(ay, by) + r + 2f));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - ax;
                    float py = (y + 0.5f) - ay;
                    float along = (px * tx) + (py * ty);
                    float across = (py * tx) - (px * ty);
                    float t = Mathf.Clamp(along, 0f, len);
                    float ex = px - (t * tx);
                    float ey = py - (t * ty);
                    float d = Mathf.Sqrt((ex * ex) + (ey * ey)) - r;
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, Palette.FiberShadow, cov);
                    float inside = PixelCanvas.Coverage(d + edge);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float chev = Mathf.Repeat((along + Mathf.Abs(across)) / period, 1f);
                    pc.Blend(x, y, chev < 0.5f ? strand : strandDark, inside);
                }
            }
        }
    }
}
