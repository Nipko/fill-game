#nullable enable
using System;
using System.Collections.Generic;
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Gráficos provisionales generados por código al arrancar (sin assets binarios; el arte final es del artista,
    /// CLAUDE.md "Humanos"): cuia-garrafa de vidrio en dos capas (detrás y delante del líquido, 07 §4.1-§4.2), tapa de
    /// calabaza (07 §4.4), aro de luz, iconos de Marea y Volver a la orilla (07 §7.6), fondo selvático sobrio (07 §1.1-
    /// §1.2: S ≤ 35 %, V ≤ 55 %), banda de río del agua base, plumas del confeti, un rectángulo redondeado 9-slice para
    /// los botones "piedra de río" y los paneles de papel (07 §7.3) y el atlas de patrones por tinte (07 §3; 02 §12).
    /// </summary>
    public sealed class ProceduralArt : IDisposable
    {
        /// <summary>Píxeles por u de las texturas de la cuia (u = ancho de la cuia / 2, 07 §4.1).</summary>
        public const int CuiaPixelsPerU = 64;

        /// <summary>Radio en píxeles del sprite 9-slice <see cref="RoundedRect"/>.</summary>
        public const float RoundedRectRadiusPx = 20f;

        /// <summary>Lado de una tesela del patrón por tinte, en píxeles; cubre <see cref="PatternTileU"/> u.</summary>
        public const int PatternTilePx = 64;

        /// <summary>Lado de la tesela del patrón en u (dos teselas por dosis de 1 u).</summary>
        public const float PatternTileU = 0.5f;

        /// <summary>Alto de una fila del atlas de patrones: una dosis (1 u, dos teselas).</summary>
        public const int PatternRowPx = 2 * PatternTilePx;

        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        private readonly Dictionary<int, Sprite> _cuiaBack = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, Sprite> _cuiaFront = new Dictionary<int, Sprite>();
        private readonly Color _water;

        /// <summary>Genera los gráficos comunes para el agua base <paramref name="waterBase"/>.</summary>
        public ProceduralArt(string waterBase)
        {
            _water = Palette.WaterBase(waterBase);
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SealCap = BuildSealCap();
            Ring = BuildRing();
            Disc = BuildDisc();
            RoundedRect = BuildRoundedRect();
            Feather = BuildFeather();
            IconUndo = BuildIconUndo();
            IconRestart = BuildIconRestart();
            Background = BuildBackground();
            RiverBand = BuildRiverBand(_water);
            PatternAtlas = BuildPatternAtlas();
        }

        /// <summary>Fuente de UI (<c>LegacyRuntime.ttf</c>, sin importar TextMeshPro; docs/dev/prototipo-v0.md c).</summary>
        public Font Font { get; }

        /// <summary>Tapa de calabaza (07 §4.4): 1,10 u × 0,35 u.</summary>
        public Sprite SealCap { get; }

        /// <summary>Aro de luz blanco (sellado, destino válido, celebración).</summary>
        public Sprite Ring { get; }

        /// <summary>Disco blanco suave.</summary>
        public Sprite Disc { get; }

        /// <summary>Rectángulo redondeado blanco 9-slice (radio <see cref="RoundedRectRadiusPx"/> px).</summary>
        public Sprite RoundedRect { get; }

        /// <summary>Pluma blanca para el confeti de guacamayo.</summary>
        public Sprite Feather { get; }

        /// <summary>Icono de Marea · Deshacer (07 §7.6).</summary>
        public Sprite IconUndo { get; }

        /// <summary>Icono de Volver a la orilla · Reiniciar (07 §7.6).</summary>
        public Sprite IconRestart { get; }

        /// <summary>Fondo selvático del Estuario: degradado sobrio con línea de selva y palmeras de açaí en silueta.</summary>
        public Sprite Background { get; }

        /// <summary>Banda de río con el agua base del mundo (07 §7.4: 60 dp en reposo).</summary>
        public Sprite RiverBand { get; }

        /// <summary>Agua base del mundo (07 §2.1).</summary>
        public Color Water => _water;

        /// <summary>
        /// Atlas de patrones por tinte (07 §3, columna "Patrón"): una fila de <see cref="PatternRowPx"/> px (una dosis)
        /// por índice de motor 0-14, marcas blancas sobre transparente, repetible en horizontal. Una sola textura para que
        /// los patrones de todas las cuias vayan en el mismo lote de dibujo.
        /// </summary>
        public Texture2D PatternAtlas { get; }

        /// <summary>El tinte tiene patrón (Guanábana es "liso", 07 §3).</summary>
        public static bool HasPattern(int tint)
        {
            return tint >= 2 && tint <= Cuia.MaxTintColor;
        }

        /// <summary>
        /// UV de una dosis del tinte <paramref name="tint"/> en <see cref="PatternAtlas"/>: <paramref name="widthU"/> u de
        /// ancho (se repite en horizontal) y <paramref name="fill"/> de dosis de alto, anclado al fondo de la dosis.
        /// </summary>
        public Rect PatternUv(int tint, float widthU, float fill)
        {
            float rowH = PatternRowPx / (float)PatternAtlas.height;
            return new Rect(0f, tint * rowH, widthU / PatternTileU, Mathf.Clamp01(fill) * rowH);
        }

        /// <summary>Vidrio trasero y fondo de la cavidad de una cuia de capacidad <paramref name="capacity"/>.</summary>
        public Sprite CuiaBack(int capacity)
        {
            if (!_cuiaBack.TryGetValue(capacity, out Sprite? sprite))
            {
                sprite = BuildCuiaBack(capacity);
                _cuiaBack[capacity] = sprite;
            }

            return sprite;
        }

        /// <summary>Vidrio frontal: contorno claro, brillo, gradiente lateral, marcas de dosis, pie y borde de fibra.</summary>
        public Sprite CuiaFront(int capacity)
        {
            if (!_cuiaFront.TryGetValue(capacity, out Sprite? sprite))
            {
                sprite = BuildCuiaFront(capacity);
                _cuiaFront[capacity] = sprite;
            }

            return sprite;
        }

        /// <summary>Destruye texturas y sprites generados.</summary>
        public void Dispose()
        {
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                {
                    UnityEngine.Object.Destroy(_owned[i]);
                }
            }

            _owned.Clear();
            _cuiaBack.Clear();
            _cuiaFront.Clear();
        }

        private Sprite MakeSprite(PixelCanvas canvas, string name, Vector2 pivot, Vector4 border, bool repeatX = false)
        {
            Texture2D tex = canvas.ToTexture(name, repeatX);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, canvas.Width, canvas.Height), pivot, 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            _owned.Add(tex);
            _owned.Add(sprite);
            return sprite;
        }

        private Texture2D BuildPatternAtlas()
        {
            const int t = PatternTilePx;
            int rows = Cuia.MaxTintColor + 1;
            var atlas = new PixelCanvas(t, PatternRowPx * rows);
            for (int tint = 2; tint <= Cuia.MaxTintColor; tint++)
            {
                var row = new PixelCanvas(t, PatternRowPx);
                for (int ox = -t; ox <= t; ox += t)
                {
                    for (int oy = -t; oy <= PatternRowPx; oy += t)
                    {
                        DrawPatternTile(row, tint, ox, oy); // copias desplazadas: la tesela se repite sin costuras
                    }
                }

                for (int y = 0; y < PatternRowPx; y++)
                {
                    for (int x = 0; x < t; x++)
                    {
                        atlas.Set(x, (tint * PatternRowPx) + y, row.Get(x, y));
                    }
                }
            }

            Texture2D tex = atlas.ToTexture("pattern_atlas", true, false);
            _owned.Add(tex);
            return tex;
        }

        // Motivos de 07 §3 en una tesela de 64 × 64 px con origen (ox, oy); blanco opaco (el color lo pone la dosis).
        private static void DrawPatternTile(PixelCanvas pc, int tint, float ox, float oy)
        {
            Color w = Color.white;
            switch (tint)
            {
                case 2: // Copoazú: puntos finos
                    for (int j = 0; j < 4; j++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            pc.FillCircle(ox + (i * 16f) + 8f + ((j % 2) * 8f), oy + (j * 16f) + 8f, 2.6f, w);
                        }
                    }

                    break;
                case 3: // Taperebá: puntos grandes
                    pc.FillCircle(ox + 16f, oy + 16f, 9f, w);
                    pc.FillCircle(ox + 48f, oy + 48f, 9f, w);
                    break;
                case 4: // Aguaje: escamas
                    for (int r = 0; r < 4; r++)
                    {
                        for (int i = -1; i <= 2; i++)
                        {
                            pc.StrokeArc(ox + (i * 32f) + ((r % 2) * 16f) + 16f, oy + (r * 16f) + 16f, 15f, 3f, 180f, 360f, w);
                        }
                    }

                    break;
                case 5: // Achiote: triángulos
                    pc.FillTriangle(new Vector2(ox + 16f, oy + 26f), new Vector2(ox + 6f, oy + 8f), new Vector2(ox + 26f, oy + 8f), w);
                    pc.FillTriangle(new Vector2(ox + 48f, oy + 58f), new Vector2(ox + 38f, oy + 40f), new Vector2(ox + 58f, oy + 40f), w);
                    break;
                case 6: // Camu camu: rayas verticales
                    pc.FillRoundedRect(ox + 9f, oy - 1f, ox + 19f, oy + 65f, 0f, w);
                    pc.FillRoundedRect(ox + 41f, oy - 1f, ox + 51f, oy + 65f, 0f, w);
                    break;
                case 7: // Bacaba: ondas
                    DrawWave(pc, ox - 2f, ox + 66f, oy + 16f, 6f, 64f, 2.2f, w);
                    DrawWave(pc, ox - 2f, ox + 66f, oy + 48f, 6f, 64f, 2.2f, w);
                    break;
                case 8: // Açaí: rombos
                    FillDiamond(pc, ox + 16f, oy + 16f, 11f, w);
                    FillDiamond(pc, ox + 48f, oy + 48f, 11f, w);
                    break;
                case 9: // Huito: cuadrícula
                    pc.FillRoundedRect(ox - 1.5f, oy - 1f, ox + 1.5f, oy + 65f, 0f, w);
                    pc.FillRoundedRect(ox + 30.5f, oy - 1f, ox + 33.5f, oy + 65f, 0f, w);
                    pc.FillRoundedRect(ox - 1f, oy - 1.5f, ox + 65f, oy + 1.5f, 0f, w);
                    pc.FillRoundedRect(ox - 1f, oy + 30.5f, ox + 65f, oy + 33.5f, 0f, w);
                    break;
                case 10: // Agua del Tapajós: líneas onduladas finas
                    for (int k = 0; k < 4; k++)
                    {
                        DrawWave(pc, ox - 2f, ox + 66f, oy + 8f + (k * 16f), 3f, 32f, 1.3f, w);
                    }

                    break;
                case 11: // Lulo: chevrons
                    for (int k = 0; k < 2; k++)
                    {
                        float y0 = oy + 10f + (k * 32f);
                        for (int i = -1; i < 2; i++)
                        {
                            float x0 = ox + (i * 32f);
                            pc.FillCapsule(x0, y0 + 12f, x0 + 16f, y0, 2.2f, w);
                            pc.FillCapsule(x0 + 16f, y0, x0 + 32f, y0 + 12f, 2.2f, w);
                        }
                    }

                    break;
                case 12: // Arazá: medias lunas
                    FillCrescent(pc, ox + 16f, oy + 16f, 9f, w);
                    FillCrescent(pc, ox + 48f, oy + 48f, 9f, w);
                    break;
                case 13: // Cacao: cruzado
                    for (int k = -2; k <= 2; k++)
                    {
                        float c = k * 32f;
                        pc.FillCapsule(ox - 8f, oy - 8f + c, ox + 72f, oy + 72f + c, 1.6f, w);
                        pc.FillCapsule(ox - 8f, oy + 72f + c, ox + 72f, oy - 8f + c, 1.6f, w);
                    }

                    break;
                case 14: // Agua del Río Negro: moteado
                    pc.FillCircle(ox + 10f, oy + 12f, 5f, w);
                    pc.FillCircle(ox + 30f, oy + 6f, 3f, w);
                    pc.FillCircle(ox + 50f, oy + 18f, 6f, w);
                    pc.FillCircle(ox + 20f, oy + 38f, 4f, w);
                    pc.FillCircle(ox + 42f, oy + 44f, 5f, w);
                    pc.FillCircle(ox + 58f, oy + 58f, 3f, w);
                    pc.FillCircle(ox + 6f, oy + 54f, 3.5f, w);
                    pc.FillCircle(ox + 34f, oy + 26f, 2.5f, w);
                    break;
            }
        }

        private static void FillDiamond(PixelCanvas pc, float cx, float cy, float r, Color c)
        {
            pc.FillTriangle(new Vector2(cx - r, cy), new Vector2(cx, cy - r), new Vector2(cx + r, cy), c);
            pc.FillTriangle(new Vector2(cx - r, cy), new Vector2(cx + r, cy), new Vector2(cx, cy + r), c);
        }

        private static void FillCrescent(PixelCanvas pc, float cx, float cy, float r, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - r - 1f);
            int x1 = Mathf.CeilToInt(cx + r + 1f);
            int y0 = Mathf.FloorToInt(cy - r - 1f);
            int y1 = Mathf.CeilToInt(cy + r + 1f);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = Vector2.Distance(p, new Vector2(cx, cy)) - r;
                    float d2 = Vector2.Distance(p, new Vector2(cx + (0.45f * r), cy + (0.35f * r))) - (0.85f * r);
                    pc.Blend(x, y, c, PixelCanvas.Coverage(d1) * (1f - PixelCanvas.Coverage(d2)));
                }
            }
        }

        private Sprite BuildCuiaBack(int capacity)
        {
            const int ppu = CuiaPixelsPerU;
            int w = Mathf.RoundToInt(CuiaGeometry.WidthU * ppu);
            int h = Mathf.RoundToInt(CuiaGeometry.HeightU(capacity) * ppu);
            var pc = new PixelCanvas(w, h);
            float lip = CuiaGeometry.LipU(capacity);
            var glass = new Color(0.96f, 0.93f, 0.86f, 0.10f);
            var cavity = new Color(0.09f, 0.13f, 0.11f, 0.62f);
            for (int y = 0; y < h; y++)
            {
                float yu = (y + 0.5f) / ppu;
                float outer = CuiaGeometry.OuterHalfWidth(yu, capacity);
                float inner = CuiaGeometry.InnerHalfWidth(yu, capacity);
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(((x + 0.5f) / ppu) - 1f);
                    if (yu >= CuiaGeometry.FootTopU && yu < lip)
                    {
                        pc.Blend(x, y, glass, PixelCanvas.Coverage((dx - outer) * ppu));
                    }

                    if (inner > 0f && yu < lip)
                    {
                        float cov = PixelCanvas.Coverage((dx - inner) * ppu) * Mathf.Clamp01(((yu - CuiaGeometry.CavityBottomU) * ppu) + 0.5f);
                        pc.Blend(x, y, cavity, cov);
                        float edge = inner - dx;
                        if (edge < 0.15f)
                        {
                            pc.Blend(x, y, new Color(0f, 0f, 0f, 0.30f * (1f - Mathf.Clamp01(edge / 0.15f))), cov); // viñeta lateral (07 §4.2)
                        }
                    }
                }
            }

            return MakeSprite(pc, "cuia_back_" + capacity, new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildCuiaFront(int capacity)
        {
            const int ppu = CuiaPixelsPerU;
            int w = Mathf.RoundToInt(CuiaGeometry.WidthU * ppu);
            int h = Mathf.RoundToInt(CuiaGeometry.HeightU(capacity) * ppu);
            var pc = new PixelCanvas(w, h);
            float height = CuiaGeometry.HeightU(capacity);
            float lip = CuiaGeometry.LipU(capacity);
            float cavityTop = CuiaGeometry.CavityTopU(capacity);
            var ivory = (Color)Palette.UiPaper;
            Color fiberDark = Palette.Fiber * 0.72f;
            fiberDark.a = 1f;

            // Brillo diagonal fijo (07 §4.2): de (0,25 u, alto − 1 u) a (0,55 u, 1 u), 0,18 u de ancho, marfil al 18 %.
            var hlA = new Vector2(0.25f, height - 1.0f);
            var hlB = new Vector2(0.55f, 1.0f);
            var spot = new Vector2(1.55f, cavityTop + 0.22f);

            for (int y = 0; y < h; y++)
            {
                float yu = (y + 0.5f) / ppu;
                float outer = CuiaGeometry.OuterHalfWidth(yu, capacity);
                float inner = CuiaGeometry.InnerHalfWidth(yu, capacity);
                for (int x = 0; x < w; x++)
                {
                    float xu = (x + 0.5f) / ppu;
                    float dx = Mathf.Abs(xu - 1f);
                    float covOuter = PixelCanvas.Coverage((dx - outer) * ppu);
                    bool fiber = yu < CuiaGeometry.FootTopU || yu >= lip;
                    if (fiber)
                    {
                        // Pie y borde de fibra de arumã trenzada (07 §4.6, Cuia de vidrio): trenzado en diagonal.
                        float band = Mathf.Repeat((xu + (yu * (xu < 1f ? 1f : -1f))) * 5.5f, 1f);
                        Color c = band < 0.5f ? Palette.Fiber : Color.Lerp(Palette.Fiber, fiberDark, 0.6f);
                        float vy = yu < CuiaGeometry.FootTopU
                            ? Mathf.Clamp01((yu * ppu) + 0.5f)
                            : Mathf.Clamp01(((height - yu) * ppu) + 0.5f);
                        pc.Blend(x, y, c, covOuter * vy);
                        float edgeY = yu < CuiaGeometry.FootTopU ? Mathf.Min(yu, CuiaGeometry.FootTopU - yu) : Mathf.Min(yu - lip, height - yu);
                        if (edgeY < 0.04f)
                        {
                            pc.Blend(x, y, fiberDark, covOuter * vy * 0.6f);
                        }

                        continue;
                    }

                    // Base de vidrio grueso: gradiente oscuro de "fondo de botella".
                    if (yu < CuiaGeometry.CavityBottomU)
                    {
                        float k = (CuiaGeometry.CavityBottomU - yu) / (CuiaGeometry.CavityBottomU - CuiaGeometry.FootTopU);
                        pc.Blend(x, y, new Color(0f, 0f, 0f, 0.18f + (0.12f * k)), covOuter);
                    }

                    // Gradiente lateral sobre el líquido: 0,20 u por borde, negro al 20 % (07 §4.2).
                    if (yu >= CuiaGeometry.CavityBottomU && yu < cavityTop)
                    {
                        float edge = CuiaGeometry.CavityHalfWidthU - dx;
                        if (edge > 0f && edge < 0.20f)
                        {
                            pc.Blend(x, y, new Color(0f, 0f, 0f, 0.20f * (1f - (edge / 0.20f))), 1f);
                        }
                    }

                    // Marcas de dosis grabadas en y = 1,60 / 2,60 / 3,60 / 4,60 (07 §4.1): 1 px, marfil al 25 %.
                    if (inner > 0f && dx < inner)
                    {
                        for (int k = 1; k <= capacity; k++)
                        {
                            float d = Mathf.Abs(yu - (CuiaGeometry.CavityBottomU + k)) * ppu;
                            if (d < 1.0f)
                            {
                                pc.Blend(x, y, new Color(ivory.r, ivory.g, ivory.b, 0.25f), Mathf.Clamp01(1.0f - d));
                            }
                        }

                        float bottom = Mathf.Abs(yu - CuiaGeometry.CavityBottomU) * ppu;
                        if (bottom < 1.0f)
                        {
                            pc.Blend(x, y, new Color(ivory.r, ivory.g, ivory.b, 0.22f), Mathf.Clamp01(1.0f - bottom));
                        }
                    }

                    // Pared interior de la cavidad (grosor del vidrio).
                    if (inner > 0f)
                    {
                        float wall = Mathf.Abs(dx - inner) * ppu;
                        if (wall < 1.0f && yu >= CuiaGeometry.CavityBottomU)
                        {
                            pc.Blend(x, y, new Color(ivory.r, ivory.g, ivory.b, 0.22f), Mathf.Clamp01(1.0f - wall));
                        }
                    }

                    // Brillos (recortados a la silueta).
                    float hl = DistanceToSegment(new Vector2(xu, yu), hlA, hlB) - 0.09f;
                    pc.Blend(x, y, new Color(ivory.r, ivory.g, ivory.b, 0.18f), PixelCanvas.Coverage(hl * ppu) * covOuter);
                    float sp = Vector2.Distance(new Vector2(xu, yu), spot) - 0.10f;
                    pc.Blend(x, y, new Color(ivory.r, ivory.g, ivory.b, 0.10f), PixelCanvas.Coverage(sp * ppu) * covOuter);

                    // Contorno claro de la silueta (cuia-garrafa reconocible).
                    float ring = covOuter - PixelCanvas.Coverage((dx - (outer - 0.05f)) * ppu);
                    pc.Blend(x, y, new Color(ivory.r, ivory.g, ivory.b, 0.85f), ring);
                }
            }

            return MakeSprite(pc, "cuia_front_" + capacity, new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildSealCap()
        {
            const int w = 72;
            const int h = 24;
            var pc = new PixelCanvas(w, h);
            Color wood = Palette.CapWood;
            Color dark = wood * 0.72f;
            dark.a = 1f;
            Color light = Color.Lerp(wood, Color.white, 0.45f);
            const float cx = 36f;
            const float cy = 3f;
            const float rx = 34f;
            const float ry = 19f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = (x + 0.5f - cx) / rx;
                    float py = (y + 0.5f - cy) / ry;
                    float e = Mathf.Sqrt((px * px) + (py * py));
                    float d = (e - 1f) * ry;
                    if (y + 0.5f < cy - 1f)
                    {
                        d = Mathf.Max(d, (cy - 1f) - (y + 0.5f));
                    }

                    float cov = PixelCanvas.Coverage(d);
                    pc.Blend(x, y, wood, cov);
                    if (y < 5)
                    {
                        pc.Blend(x, y, dark, cov * 0.8f); // asiento sobre el borde
                    }

                    float veta = Mathf.Abs(e - 0.62f) * ry; // una veta
                    if (veta < 1.0f && y > 5)
                    {
                        pc.Blend(x, y, dark, cov * (1f - veta) * 0.55f);
                    }
                }
            }

            pc.FillCircle(25f, 15f, 4.5f, new Color(light.r, light.g, light.b, 0.6f)); // brillo
            return MakeSprite(pc, "seal_cap", new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildRing()
        {
            const int s = 64;
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f));
                    float core = PixelCanvas.Coverage(Mathf.Abs(d - 26f) - 2.5f);
                    float glow = Mathf.Clamp01(1f - (Mathf.Abs(d - 26f) / 6f)) * 0.45f;
                    pc.Blend(x, y, Color.white, Mathf.Max(core, glow));
                }
            }

            return MakeSprite(pc, "ring", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildDisc()
        {
            var pc = new PixelCanvas(32, 32);
            pc.FillCircle(16f, 16f, 15f, Color.white);
            return MakeSprite(pc, "disc", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildRoundedRect()
        {
            var pc = new PixelCanvas(64, 64);
            pc.FillRoundedRect(0f, 0f, 64f, 64f, RoundedRectRadiusPx, Color.white);
            return MakeSprite(pc, "rounded_rect", new Vector2(0.5f, 0.5f), new Vector4(24f, 24f, 24f, 24f));
        }

        private Sprite BuildFeather()
        {
            const int w = 12;
            const int h = 36;
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = (x + 0.5f - 6f) / 5.5f;
                    float py = (y + 0.5f - 18f) / 17.5f;
                    float d = (Mathf.Sqrt((px * px) + (py * py)) - 1f) * 5.5f;
                    pc.Blend(x, y, Color.white, PixelCanvas.Coverage(d));
                }
            }

            pc.FillCapsule(6f, 2f, 6f, 33f, 0.6f, new Color(0f, 0f, 0f, 0.25f)); // raquis
            return MakeSprite(pc, "feather", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildIconUndo()
        {
            // Ola que retrocede con flecha curva a la izquierda sobre la cresta (07 §7.6).
            var pc = new PixelCanvas(96, 96);
            Color wave = Palette.IconTurquoise;
            DrawWave(pc, 14f, 82f, 28f, 5f, 34f, 4f, wave);
            DrawWave(pc, 20f, 76f, 15f, 3.5f, 34f, 2.6f, new Color(wave.r, wave.g, wave.b, 0.5f));
            // Flecha en tinta: el icono va sobre la piedra de río de papel de la bandeja (07 §7.3, §7.6: tinta, color
            // principal y marfil como únicos tonos).
            DrawArrowArc(pc, new Vector2(48f, 46f), 22f, 7f, 15f, 165f, Palette.UiInk);
            return MakeSprite(pc, "icon_undo", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildIconRestart()
        {
            // Flecha circular con una orilla y una ola (07 §7.6).
            var pc = new PixelCanvas(96, 96);
            Color ivory = Palette.UiPaper;
            DrawArrowArc(pc, new Vector2(48f, 54f), 25f, 7f, 60f, 380f, ivory);
            DrawWave(pc, 22f, 74f, 14f, 3f, 26f, 3f, Palette.IconTurquoise);
            return MakeSprite(pc, "icon_restart", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private static void DrawWave(PixelCanvas pc, float x0, float x1, float yMid, float amplitude, float wavelength, float radius, Color c)
        {
            float prevX = x0;
            float prevY = yMid + (amplitude * Mathf.Sin(0f));
            for (float x = x0 + 2f; x <= x1; x += 2f)
            {
                float y = yMid + (amplitude * Mathf.Sin((x - x0) * 2f * Mathf.PI / wavelength));
                pc.FillCapsule(prevX, prevY, x, y, radius, c);
                prevX = x;
                prevY = y;
            }
        }

        private static void DrawArrowArc(PixelCanvas pc, Vector2 center, float radius, float thickness, float fromDeg, float toDeg, Color c)
        {
            pc.StrokeArc(center.x, center.y, radius, thickness, fromDeg, toDeg, c);
            float a = toDeg * Mathf.Deg2Rad;
            var p = new Vector2(center.x + (Mathf.Cos(a) * radius), center.y + (Mathf.Sin(a) * radius));
            var dir = new Vector2(-Mathf.Sin(a), Mathf.Cos(a)); // tangente en sentido antihorario
            var perp = new Vector2(dir.y, -dir.x);
            Vector2 tip = p + (dir * (thickness * 1.6f));
            Vector2 b0 = p - (dir * 1.5f) + (perp * (thickness * 1.35f));
            Vector2 b1 = p - (dir * 1.5f) - (perp * (thickness * 1.35f));
            pc.FillTriangle(tip, b0, b1, c);
        }

        private Sprite BuildBackground()
        {
            // Estuario, alba con bruma baja (07 §2.1): fondo plano desaturado y oscuro (S ≤ 35 %, V ≤ 55 %, 07 §1.2)
            // con línea de selva y palmeras de açaí en silueta; la zona del tablero queda lisa.
            const int w = 240;
            const int h = 520;
            var pc = new PixelCanvas(w, h);
            Color c0 = Palette.Hex(0x5C5D4A);
            Color c1 = Palette.Hex(0x404C3D);
            Color c2 = Palette.Hex(0x2C3729);
            Color c3 = Palette.Hex(0x1F271F);
            Color far = Palette.Hex(0x37453A);
            Color near = Palette.Hex(0x2A3529);
            Color mist = Palette.UiPaper;
            for (int y = 0; y < h; y++)
            {
                float f = 1f - ((y + 0.5f) / h); // 0 arriba, 1 abajo
                Color g = f < 0.35f ? Color.Lerp(c0, c1, f / 0.35f)
                    : f < 0.70f ? Color.Lerp(c1, c2, (f - 0.35f) / 0.35f)
                    : Color.Lerp(c2, c3, (f - 0.70f) / 0.30f);
                for (int x = 0; x < w; x++)
                {
                    pc.Set(x, y, g);
                    float fx = (x + 0.5f) / w;
                    float farEdge = 0.27f + (0.020f * Mathf.Sin(fx * 9.1f)) + (0.012f * Mathf.Sin((fx * 23.7f) + 1.3f)) + (0.006f * Mathf.Sin((fx * 61f) + 2f));
                    if (f > farEdge)
                    {
                        float fade = Mathf.Clamp01(1f - ((f - farEdge) / 0.22f));
                        pc.Blend(x, y, far, PixelCanvas.Coverage((farEdge - f) * h) * 0.85f * fade);
                    }

                    float nearEdge = 0.36f + (0.025f * Mathf.Sin((fx * 6.3f) + 0.7f)) + (0.015f * Mathf.Sin((fx * 17f) + 2.1f)) + (0.006f * Mathf.Sin((fx * 47f) + 0.4f));
                    if (f > nearEdge)
                    {
                        float fade = Mathf.Clamp01(1f - ((f - nearEdge) / 0.26f));
                        pc.Blend(x, y, near, PixelCanvas.Coverage((nearEdge - f) * h) * 0.9f * fade);
                    }

                    float m = Mathf.Exp(-Mathf.Pow((f - 0.31f) / 0.045f, 2f)) * 0.06f; // bruma baja
                    pc.Blend(x, y, new Color(mist.r, mist.g, mist.b, 1f), m);
                }
            }

            DrawPalm(pc, 0.11f * w, (1f - 0.47f) * h, (1f - 0.18f) * h, 2.2f, near);
            DrawPalm(pc, 0.89f * w, (1f - 0.50f) * h, (1f - 0.22f) * h, 2.0f, near);
            DrawPalm(pc, 0.77f * w, (1f - 0.44f) * h, (1f - 0.29f) * h, 1.5f, far);
            return MakeSprite(pc, "background_w01", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private static void DrawPalm(PixelCanvas pc, float x, float yBase, float yTop, float trunk, Color c)
        {
            float lean = (yTop - yBase) * 0.06f;
            pc.FillCapsule(x, yBase, x + lean, yTop, trunk, c);
            var crown = new Vector2(x + lean, yTop);
            float len = (yTop - yBase) * 0.32f;
            for (int i = 0; i < 9; i++)
            {
                float a = (12f + (i * 19.5f)) * Mathf.Deg2Rad;
                var tip = new Vector2(crown.x + (Mathf.Cos(a) * len), crown.y + (Mathf.Sin(a) * len * 0.55f) - (len * 0.25f * Mathf.Abs(Mathf.Cos(a))));
                var mid = Vector2.Lerp(crown, tip, 0.5f) + new Vector2(0f, len * 0.08f);
                pc.FillCapsule(crown.x, crown.y, mid.x, mid.y, trunk * 0.8f, c);
                pc.FillCapsule(mid.x, mid.y, tip.x, tip.y, trunk * 0.5f, c);
            }
        }

        private Sprite BuildRiverBand(Color water)
        {
            // Agua base translúcida (capa media, 07 §1.1) con cresta ondulada y reflejos: 12 ondas a lo ancho.
            const int w = 512;
            const int h = 64;
            var pc = new PixelCanvas(w, h);
            Color ivory = Palette.UiPaper;
            for (int x = 0; x < w; x++)
            {
                float phase = (x + 0.5f) * 2f * Mathf.PI * 12f / w;
                float crest = h - 6f + (2.5f * Mathf.Sin(phase));
                for (int y = 0; y < h; y++)
                {
                    float fy = y + 0.5f;
                    float inside = PixelCanvas.Coverage(fy - crest);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float t = fy / h; // 0 abajo, 1 arriba
                    Color c = water;
                    c.a = Mathf.Lerp(0.22f, 0.42f, t);
                    pc.Blend(x, y, c, inside);
                    float edge = crest - fy;
                    if (edge < 3f)
                    {
                        pc.Blend(x, y, new Color(ivory.r, ivory.g, ivory.b, 0.30f * (1f - (edge / 3f))), inside);
                    }

                    for (int k = 1; k <= 3; k++)
                    {
                        float ry = crest - (k * 13f) + (1.5f * Mathf.Sin((phase * (0.5f + (0.25f * k))) + k));
                        float d = Mathf.Abs(fy - ry);
                        if (d < 1.2f)
                        {
                            pc.Blend(x, y, new Color(ivory.r, ivory.g, ivory.b, 0.10f * (1f - (d / 1.2f))), inside);
                        }
                    }
                }
            }

            return MakeSprite(pc, "river_band", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + (ab * t));
        }
    }
}
