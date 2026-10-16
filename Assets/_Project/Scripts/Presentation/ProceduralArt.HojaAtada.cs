#nullable enable
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Hoja atada (variante de la Hoja de la 1.0, estreno en el 283; 02 §9.1 «Variantes» y §1.2;
    /// propuesta de D-007 b.3: «hoja pequeña atada al cuello con fibra, con icono grande; nunca cae»; tarea VA1a). En el tablero
    /// reutiliza la hoja de palma de la Hoja (<see cref="HojaPalm"/>), más pequeña y colgada del cuello; su medallón
    /// (<see cref="HojaBadge"/>); y la vuelta del cuello con su nudo de la Liana (<see cref="LianaCollar"/>), teñida de fibra
    /// (<see cref="HojaCordTint"/>) en lugar de verde, para que se lea como un cordel y no como una liana que bloquea. Lo propio
    /// es la llave del medallón: una muestra de la dosis (<see cref="HojaAtadaSwatch"/>), un disco del tinte con el mismo patrón y
    /// el mismo icono de accesibilidad que las capas, porque el medallón es el único sitio que dice qué fruta acepta (revisión de
    /// VA1a: con la fruta en vuelo, de otro color que su líquido, el aguaje se confundía con el urucum o el camu-camu).
    /// Aquí van también el icono de la mecánica (<see cref="MechanicIcon"/> de 32 dp y el grande del estreno,
    /// <see cref="CardHeroIcon"/>), con los tres elementos del tablero: el aro de cordel, el medallón en el nudo y la hoja
    /// pequeña colgando.
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
        /// Tinte de la vuelta del cuello (<see cref="LianaCollar"/>, en grises) para el cordel de fibra de la Hoja atada [P]:
        /// <see cref="Palette.FiberBase"/>, así el tallo (×0,88) queda en fibra media y el nudo (×0,70), más oscuro; más oscuro
        /// que el borde de fibra trenzado de la cuia, para no confundirse con él.
        /// </summary>
        public static readonly Color HojaCordTint = Palette.FiberBase;

        /// <summary>Luz de borde del cordel de la Hoja atada (sobre <see cref="LianaCollarShine"/>) [P].</summary>
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

        // Icono de la Hoja atada en un lienzo cuadrado de s px (k = s / 96; y hacia arriba), con los tres elementos del tablero
        // (revisión de VA1a, para que la tarjeta y el tablero rimen): el aro de cordel del cuello arriba a la izquierda, con su
        // nudo; en el nudo, el medallón (placa menta redondeada, ≈ 38 % del círculo de la tarjeta de 76 dp) con una fruta
        // genérica en tinta (sin tintes de fruta en la UI, 07 §3, regla 6: la fruta llave la dice el medallón del tablero); y la
        // hoja de palma pequeña colgando por debajo de la placa (≈ 20 % del círculo).
        private static void DrawHojaAtadaGlyph(PixelCanvas pc, int s)
        {
            float k = s / 96f;

            // La hoja cuelga de debajo de la placa, de (60, 34) a (78, 4), con la panza hacia la derecha.
            DrawPalmLeaf(pc, new Vector2(60f * k, 34f * k), new Vector2(80f * k, 24f * k), new Vector2(78f * k, 4f * k), 8.5f * k,
                Palette.JungleMid, Palette.LeafDark, Palette.Vein, 0.8f * k);

            // El aro del cuello (detrás, la mitad de atrás en sombra) y el nudo con sus cabos hasta la placa.
            StrokeEllipse(pc, 26f * k, 82f * k, 20f * k, 8f * k, 5f * k, Palette.FiberShadow, false);
            StrokeEllipse(pc, 26f * k, 82f * k, 20f * k, 8f * k, 5f * k, Palette.FiberBase, true);
            pc.FillCapsule(44f * k, 78f * k, 40f * k, 66f * k, 2.4f * k, Palette.FiberShadow);
            pc.FillCapsule(44f * k, 78f * k, 50f * k, 66f * k, 2.4f * k, Palette.FiberBase);

            // La placa del medallón: sombra, canto, cara menta y brillo.
            const float px0 = 28f;
            const float py0 = 28f;
            const float px1 = 84f;
            const float py1 = 72f;
            pc.FillRoundedRectSoft((px0 + 1f) * k, (py0 - 2.5f) * k, (px1 + 1f) * k, (py1 - 2.5f) * k, 10f * k, 2f * k, new Color(0f, 0f, 0f, 0.35f));
            pc.FillRoundedRect(px0 * k, py0 * k, px1 * k, py1 * k, 10f * k, ColorMath.Darken(HojaBadgeColor, 0.45f));
            pc.FillRoundedRect((px0 + 2.2f) * k, (py0 + 2.2f) * k, (px1 - 2.2f) * k, (py1 - 2.2f) * k, 8f * k, HojaBadgeColor);
            pc.FillEllipseSoft(40f * k, 64f * k, 7f * k, 2.6f * k, 1.2f * k, new Color(1f, 1f, 1f, 0.75f));

            // El nudo, encima de la placa (el medallón cuelga de él).
            pc.FillCircle(44f * k, 76f * k, 6.5f * k, Palette.FiberShadow);
            pc.FillCircle(44f * k, 77f * k, 4.8f * k, Palette.FiberBase);
            pc.FillCircle(42.6f * k, 78.4f * k, 1.8f * k, Palette.FiberLight);

            // La fruta genérica, en tinta, como los iconos de accesibilidad de las capas (sin aro alrededor, que con la placa se
            // leía como una cámara): un fruto redondo con su brillo, el rabito y una hoja verde.
            const float cx = 55f;
            const float cy = 47f;
            pc.FillCircle(cx * k, cy * k, 12.5f * k, Palette.UiInk);
            pc.FillCircle((cx - 2f) * k, (cy + 12f) * k, 3.2f * k, HojaBadgeColor); // la muesca del rabito
            pc.FillCapsule(cx * k, (cy + 9f) * k, (cx + 2.5f) * k, (cy + 17f) * k, 1.6f * k, Palette.UiInk);
            DrawLeafShape(pc, new Vector2((cx + 2.5f) * k, (cy + 15f) * k), new Vector2(0.88f, 0.47f), 13f * k, 8f * k, Palette.LeafDark, 1f);
            DrawLeafShape(pc, new Vector2((cx + 3.2f) * k, (cy + 15.3f) * k), new Vector2(0.88f, 0.47f), 11f * k, 5.6f * k, Palette.JungleMid, 1f);
            pc.FillEllipseSoft((cx - 5f) * k, (cy + 4f) * k, 2.4f * k, 3.6f * k, 0.8f * k, ColorMath.WithAlpha(Palette.UiPaper, 0.75f));
        }
    }
}
