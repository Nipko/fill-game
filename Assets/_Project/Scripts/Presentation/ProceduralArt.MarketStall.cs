#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Piezas del Mercado más visible (observación 24 de la jornada 6 de la prueba completa, decisión del dueño del 2026-10-30), por
    /// código y sin texturas nuevas del artista (D-052), en la familia de D-003 (madera, fibra y luz de juego):
    /// <list type="bullet">
    /// <item><see cref="IconStall"/>: el puesto del Mercado para la entrada de la barra de abajo del inicio (un toldo a rayas de
    /// heliconia y crema sobre un mostrador de madera con fruta), para que se entienda que ahí se compra; antes, solo una semilla.</item>
    /// <item><see cref="SkinPreview"/>: la cuia de una skin en grande para la vista previa del Mercado (la miniatura de
    /// <see cref="SkinThumb"/> pintada a <see cref="SkinPreviewPixelsPerU"/> px por u, la misma ficha y el mismo dibujo). Solo una a
    /// la vez: la anterior se suelta al pedir otra y al cerrar la vista (<see cref="ReleaseSkinPreview"/>), por la memoria
    /// (observación 6 de la jornada 6).</item>
    /// <item><see cref="IconZoom"/>: la lupa pequeña en la esquina de la ficha de cada skin del Mercado, la pista de que la tarjeta se
    /// toca para ver la cuia en grande (revisión de la observación 24: sin texto nuevo).</item>
    /// </list>
    /// Perezosas: nada se crea al construir <see cref="ProceduralArt"/>.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del icono del puesto (dp).</summary>
        public const float StallWidthDp = 36f;

        /// <summary>Alto del icono del puesto (dp).</summary>
        public const float StallHeightDp = 32f;

        /// <summary>
        /// Densidad de la cuia de la vista previa (px por u): la ficha mide unos 260 × 604 px, casi 1:1 a 200 dp de alto y 3 px por dp
        /// (unos 630 KB de textura, solo mientras la vista está abierta).
        /// </summary>
        public const int SkinPreviewPixelsPerU = 100;

        /// <summary>Prefijo del nombre de los sprites de la vista previa (tests).</summary>
        public const string SkinPreviewPrefix = "skin_preview_";

        /// <summary>Lado de la lupa de la ficha de una skin (dp).</summary>
        public const float ZoomDp = 18f;

        private Sprite? _iconStall;
        private Sprite? _iconZoom;
        private Sprite? _skinPreview;
        private Texture2D? _skinPreviewTexture;

        /// <summary>El puesto del Mercado (toldo a rayas, mostrador de madera y fruta), de <see cref="StallWidthDp"/> × <see cref="StallHeightDp"/> dp.</summary>
        public Sprite IconStall => _iconStall != null ? _iconStall : (_iconStall = BuildIconStall());

        /// <summary>La lupa de la ficha de una skin: disco crema con canto de madera, el aro de la lente y el mango, de <see cref="ZoomDp"/> dp.</summary>
        public Sprite IconZoom => _iconZoom != null ? _iconZoom : (_iconZoom = BuildIconZoom());

        /// <summary>La cuia en grande de la vista previa que está a la vista, o null (tests).</summary>
        public Sprite? CurrentSkinPreview => _skinPreview;

        /// <summary>
        /// La cuia de <paramref name="skin"/> (null: la base) en grande, con la ficha del tablero; pivote en el centro, sin tinte. Suelta
        /// la de otra skin si la había: solo vive una a la vez.
        /// </summary>
        public Sprite SkinPreview(CuiaSkin? skin)
        {
            CuiaSkin s = skin ?? CuiaSkin.Base;
            if (_skinPreview != null && string.Equals(_skinPreview.name, SkinPreviewPrefix + s.Id, System.StringComparison.Ordinal))
            {
                return _skinPreview;
            }

            ReleaseSkinPreview();
            PixelCanvas pc = PaintSkinThumb(s, SkinPreviewPixelsPerU);
            Texture2D tex = pc.ToTexture(SkinPreviewPrefix + s.Id);
            pc.Release();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f * UiPxPerDp, 0, SpriteMeshType.FullRect, Vector4.zero);
            sprite.name = SkinPreviewPrefix + s.Id;
            sprite.hideFlags = HideFlags.DontSave;
            _skinPreview = sprite;
            _skinPreviewTexture = tex;
            return sprite;
        }

        /// <summary>Suelta la cuia en grande de la vista previa (al cerrarla y en <see cref="Dispose"/>).</summary>
        public void ReleaseSkinPreview()
        {
            DestroyPreviewObject(_skinPreview);
            DestroyPreviewObject(_skinPreviewTexture);
            _skinPreview = null;
            _skinPreviewTexture = null;
        }

        private static void DestroyPreviewObject(Object? o)
        {
            if (o == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(o);
            }
            else
            {
                Object.DestroyImmediate(o);
            }
        }

        // El puesto en dp (origen abajo a la izquierda, y hacia arriba): dos postes, el mostrador de madera con tres frutas encima
        // (ámbar, heliconia y hoja) y el toldo a rayas de heliconia y crema con el borde en ondas; contorno oscuro de 1,2 dp y la
        // luz arriba a la izquierda (07 §1.1).
        private Sprite BuildIconStall()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(StallWidthDp * k);
            int h = Mathf.RoundToInt(StallHeightDp * k);
            var pc = new PixelCanvas(w, h);
            Color ink = Palette.Groove;
            const float o = 1.2f * k; // contorno (px)

            // Postes.
            pc.FillRoundedRect(6f * k - o, 10f * k, 8.6f * k + o, 23f * k, 1f * k, ink);
            pc.FillRoundedRect(27.4f * k - o, 10f * k, 30f * k + o, 23f * k, 1f * k, ink);
            pc.FillRoundedRect(6f * k, 10f * k, 8.6f * k, 23f * k, 0.8f * k, Palette.DockShadow);
            pc.FillRoundedRect(27.4f * k, 10f * k, 30f * k, 23f * k, 0.8f * k, Palette.DockShadow);

            // Frutas sobre el mostrador.
            DrawStallFruit(pc, 12.5f, 15.2f, 3.1f, ProceduralArt.SeedAmber, o);
            DrawStallFruit(pc, 18.4f, 15.6f, 3.4f, Palette.Heliconia, o);
            DrawStallFruit(pc, 24.2f, 15.2f, 3.0f, Palette.FamilyLeaf.Light, o);

            // Mostrador de madera: canto claro arriba y una veta.
            pc.FillRoundedRect(3f * k - o, 1.5f * k - o, 33f * k + o, 12.5f * k + o, 2f * k, ink);
            pc.FillRoundedRect(3f * k, 1.5f * k, 33f * k, 12.5f * k, 1.6f * k, Palette.DockLight);
            pc.FillRoundedRect(3f * k, 9.6f * k, 33f * k, 12.5f * k, 1.2f * k, Palette.FamilyWood.Light);
            pc.FillRoundedRect(6f * k, 5.4f * k, 30f * k, 6.2f * k, 0.4f * k, ColorMath.Darken(Palette.DockLight, 0.22f));

            // Toldo: trapecio de 2..34 dp abajo (y = 23) a 6..30 arriba (y = 30,5), rayas de 4 dp y el borde de abajo en ondas.
            Color cream = Palette.Cream;
            Color red = Palette.Heliconia;
            Color redDark = ColorMath.Darken(red, 0.18f);
            for (int y = 0; y < h; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float d = AwningDistance(xd, yd); // dp, negativa dentro
                    float cov = PixelCanvas.Coverage((d - 1.2f) * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, ink, cov);
                    float inside = PixelCanvas.Coverage(d * k);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    int stripe = Mathf.FloorToInt((xd - 2f) / 4f);
                    Color c = (stripe & 1) == 0 ? red : cream;
                    float shade = Mathf.Clamp01((30.5f - yd) / 9f); // más oscuro hacia el borde de abajo
                    c = Color.Lerp(c, (stripe & 1) == 0 ? redDark : ColorMath.Darken(cream, 0.12f), 0.5f * shade);
                    if (yd > 28.6f)
                    {
                        c = Color.Lerp(c, Color.white, 0.25f); // brillo del canto de arriba
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }

            return MakeSprite(pc, "icon_stall", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        // La lupa en dp (y hacia arriba): el disco crema con canto de madera (se lee sobre el petróleo de la ficha), la lente con su
        // brillo y el mango en diagonal hacia abajo a la derecha, en tinta.
        private Sprite BuildIconZoom()
        {
            const float k = UiPxPerDp;
            int n = Mathf.RoundToInt(ZoomDp * k);
            var pc = new PixelCanvas(n, n);
            float c = ZoomDp * 0.5f * k;
            pc.FillCircle(c, c, (ZoomDp * 0.5f * k) - 0.5f, Palette.FamilyWood.Base);
            pc.FillCircle(c, c, (ZoomDp * 0.5f * k) - (1.6f * k), Palette.Cream);
            Color ink = Palette.Groove;
            float lx = c - (1.4f * k);
            float ly = c + (1.4f * k);
            pc.FillCapsule(lx + (2.6f * k), ly - (2.6f * k), c + (4.6f * k), c - (4.6f * k), 1.1f * k, ink); // el mango
            pc.FillCircle(lx, ly, 3.9f * k, ink);
            pc.FillCircle(lx, ly, 2.6f * k, Color.Lerp(Palette.Cream, new Color(0.55f, 0.85f, 0.95f, 1f), 0.45f)); // la lente
            pc.FillCircle(lx - (0.9f * k), ly + (0.9f * k), 0.8f * k, Color.white); // su brillo
            return MakeSprite(pc, "icon_zoom", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        private static void DrawStallFruit(PixelCanvas pc, float cxDp, float cyDp, float rDp, Color color, float outlinePx)
        {
            const float k = UiPxPerDp;
            pc.FillCircle(cxDp * k, cyDp * k, (rDp * k) + outlinePx, Palette.Groove);
            Color body = color;
            body.a = 1f;
            pc.FillCircle(cxDp * k, cyDp * k, rDp * k, body);
            Color shine = Color.Lerp(body, Color.white, 0.6f);
            pc.FillCircle((cxDp - (rDp * 0.35f)) * k, (cyDp + (rDp * 0.35f)) * k, rDp * 0.32f * k, shine);
        }

        // Distancia con signo (dp) al toldo: el trapecio menos las ondas del borde de abajo (medio círculo de 2 dp por raya).
        private static float AwningDistance(float x, float y)
        {
            const float bottom = 23f;
            const float top = 30.5f;
            float t = Mathf.Clamp01((y - bottom) / (top - bottom));
            float left = Mathf.Lerp(2f, 6f, t);
            float right = Mathf.Lerp(34f, 30f, t);
            float d = Mathf.Max(Mathf.Max(left - x, x - right), y - top);

            // Borde de abajo: ondas (semicírculos de radio 2 centrados en y = bottom, uno por raya).
            float cx = 2f + (Mathf.Floor((x - 2f) / 4f) * 4f) + 2f;
            float dx = x - cx;
            float dy = y - bottom;
            float scallop = dy >= 0f ? -dy : Mathf.Sqrt((dx * dx) + (dy * dy)) - 2f;
            return Mathf.Max(d, scallop);
        }
    }
}
