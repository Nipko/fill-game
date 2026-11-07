#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>Piezas del atlas del Mapa v2 (<see cref="MapLiveArt"/>): una celda de 128 px cada una.</summary>
    public enum MapLivePiece
    {
        /// <summary>Copas en racimo, 6 tonos (0-5).</summary>
        Canopy0 = 0,

        /// <summary>Palmera de açaí o aguaje.</summary>
        Palm = 6,

        /// <summary>Palmera de la sabana (burití).</summary>
        PalmSavanna = 7,

        /// <summary>Ipê amarillo.</summary>
        FlowerYellow = 8,

        /// <summary>Ipê rosa.</summary>
        FlowerPink = 9,

        /// <summary>Chevron de la corriente (mira a +x).</summary>
        Chevron = 10,

        /// <summary>Destello del agua.</summary>
        Glint = 11,

        /// <summary>Boya de vidrio de una parada futura.</summary>
        Buoy = 12,

        /// <summary>Mancha suave (la sombra de una nube).</summary>
        Cloud = 13,
    }

    /// <summary>
    /// Arte por código del Mapa v2 (D-069, M2-1; D-052: el arte por código es el final; propuesta del Mapa v2 §3.1 y §7.2), con la
    /// luz de la guía visual arriba a la izquierda y el barniz de juguete de D-058:
    /// <list type="bullet">
    /// <item><description>un <b>atlas</b> de <see cref="AtlasPx"/>² px con mipmaps (las copas se pintan de ~10 a ~48 dp): las copas en
    /// racimo en 6 tonos, las palmeras, los ipê en flor, el chevron y el destello de la corriente, la boya de vidrio y la sombra de
    /// una nube (<see cref="MapLivePiece"/>): una sola llamada de dibujo por capa;</description></item>
    /// <item><description>el <b>primer plano</b>: dos racimos de hojas con una heliconia (<see cref="ForegroundPx"/>²);</description></item>
    /// <item><description>los <b>Andes</b> lejanos en azul, para la niebla de arriba, y el <b>degradado de la luz</b> del mundo.</description></item>
    /// </list>
    /// Solo existe con el Mapa v2 abierto: <see cref="Build"/> lo pinta al abrir y <see cref="Release"/> lo suelta al cerrar
    /// (≈ 2 MiB de texturas; tope de la propuesta §7.2: +6 MiB solo con el Mapa abierto). No toca <see cref="ProceduralArt"/>.
    /// </summary>
    public sealed class MapLiveArt
    {
        /// <summary>Lado del atlas (px).</summary>
        public const int AtlasPx = 512;

        /// <summary>Lado de una celda del atlas (px).</summary>
        public const int CellPx = 128;

        /// <summary>Lado de cada racimo del primer plano (px).</summary>
        public const int ForegroundPx = 256;

        /// <summary>Ancho de la tira de los Andes (px).</summary>
        public const int AndesWidthPx = 512;

        /// <summary>Alto de la tira de los Andes (px).</summary>
        public const int AndesHeightPx = 96;

        /// <summary>Tonos de copa del atlas.</summary>
        public const int CanopyTones = 6;

        private static readonly Color[] CanopyGreens =
        {
            Palette.Hex(0x2E8B47), Palette.Hex(0x3C9C4C), Palette.Hex(0x2A7C44), Palette.Hex(0x4CA851), Palette.Hex(0x34823D),
            Palette.Hex(0x5BB25A),
        };

        private readonly List<Object> _owned = new List<Object>();
        private readonly Sprite?[] _pieces = new Sprite?[16];

        /// <summary>Atlas (null antes de <see cref="Build"/> o tras <see cref="Release"/>).</summary>
        public Texture2D? Atlas { get; private set; }

        /// <summary>Racimo del primer plano de abajo a la izquierda.</summary>
        public Sprite? ForegroundLeft { get; private set; }

        /// <summary>Racimo del primer plano de arriba a la derecha.</summary>
        public Sprite? ForegroundRight { get; private set; }

        /// <summary>Los Andes lejanos.</summary>
        public Sprite? Andes { get; private set; }

        /// <summary>Degradado de la luz (opaco arriba a la izquierda, transparente abajo a la derecha).</summary>
        public Sprite? Light { get; private set; }

        /// <summary>Degradado vertical (opaco arriba), para la niebla.</summary>
        public Sprite? Fog { get; private set; }

        /// <summary>Está pintado.</summary>
        public bool Built => Atlas != null;

        /// <summary>Bytes de las texturas en la GPU (con los mipmaps del atlas), para la sonda <c>AF_PERF</c> y los tests.</summary>
        public long TextureBytes { get; private set; }

        /// <summary>Rectángulo UV de la pieza <paramref name="piece"/> en el atlas (con medio píxel de margen).</summary>
        public static Rect Uv(int piece)
        {
            int perRow = AtlasPx / CellPx;
            int col = piece % perRow;
            int row = piece / perRow;
            const float pad = 0.5f / AtlasPx;
            float size = CellPx / (float)AtlasPx;
            return new Rect((col * size) + pad, (row * size) + pad, size - (2f * pad), size - (2f * pad));
        }

        /// <summary>Sprite de una pieza del atlas (para las <c>Image</c> de la corriente, las boyas y las nubes).</summary>
        public Sprite? Piece(MapLivePiece piece)
        {
            return Atlas != null ? _pieces[(int)piece] : null;
        }

        /// <summary>Pinta todo (una vez por apertura; si ya está, nada). Asigna: solo aquí.</summary>
        public void Build()
        {
            if (Built)
            {
                return;
            }

            var atlas = new PixelCanvas(AtlasPx, AtlasPx);
            for (int i = 0; i < CanopyTones; i++)
            {
                DrawCanopy(atlas, i, CanopyGreens[i]);
            }

            DrawPalm(atlas, (int)MapLivePiece.Palm, Palette.Hex(0x2F7D3A), 9);
            DrawPalm(atlas, (int)MapLivePiece.PalmSavanna, Palette.Hex(0x5E8F3A), 7);
            DrawFlower(atlas, (int)MapLivePiece.FlowerYellow, Palette.Hex(0xF2C230));
            DrawFlower(atlas, (int)MapLivePiece.FlowerPink, Palette.Hex(0xEE7FB0));
            DrawChevron(atlas, (int)MapLivePiece.Chevron);
            DrawGlint(atlas, (int)MapLivePiece.Glint);
            DrawBuoy(atlas, (int)MapLivePiece.Buoy);
            DrawCloud(atlas, (int)MapLivePiece.Cloud);
            Texture2D tex = Own(atlas.ToTexture("map_live_atlas", false, false, true));
            Atlas = tex;
            TextureBytes = (long)AtlasPx * AtlasPx * 4L * 4L / 3L;
            for (int i = 0; i < _pieces.Length; i++)
            {
                int perRow = AtlasPx / CellPx;
                var rect = new Rect((i % perRow) * CellPx, (i / perRow) * CellPx, CellPx, CellPx);
                Sprite sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                sprite.name = "map_live_" + i;
                sprite.hideFlags = HideFlags.DontSave;
                _pieces[i] = Own(sprite);
            }

            ForegroundLeft = MakeSprite(DrawForeground(false), "map_live_fg_left");
            ForegroundRight = MakeSprite(DrawForeground(true), "map_live_fg_right");
            Andes = MakeSprite(DrawAndes(), "map_live_andes");
            Light = MakeSprite(DrawLight(), "map_live_light");
            Fog = MakeSprite(DrawFog(), "map_live_fog");
            TextureBytes += (2L * ForegroundPx * ForegroundPx * 4L) + ((long)AndesWidthPx * AndesHeightPx * 4L) + (64L * 64L * 4L) + (4L * 64L * 4L);
        }

        /// <summary>Suelta todas las texturas y sprites (al cerrar el Mapa). Idempotente.</summary>
        public void Release()
        {
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(_owned[i]);
                    }
                    else
                    {
                        Object.DestroyImmediate(_owned[i]);
                    }
                }
            }

            _owned.Clear();
            for (int i = 0; i < _pieces.Length; i++)
            {
                _pieces[i] = null;
            }

            Atlas = null;
            ForegroundLeft = null;
            ForegroundRight = null;
            Andes = null;
            Light = null;
            Fog = null;
            TextureBytes = 0;
        }

        private T Own<T>(T obj)
            where T : Object
        {
            _owned.Add(obj);
            return obj;
        }

        private Sprite MakeSprite(PixelCanvas canvas, string name)
        {
            Texture2D tex = Own(canvas.ToTexture(name));
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return Own(sprite);
        }

        private static Vector2 CellOrigin(int piece)
        {
            int perRow = AtlasPx / CellPx;
            return new Vector2((piece % perRow) * CellPx, (piece / perRow) * CellPx);
        }

        // ------------------------------------------------------------------------------------------- dibujo

        // Una bola con volumen: luz arriba a la izquierda, borde algo más oscuro y el brillo de barniz de D-058.
        private static void Ball(PixelCanvas pc, float cx, float cy, float r, Color baseColor, float alpha = 1f)
        {
            int x0 = Mathf.FloorToInt(cx - r - 1f);
            int x1 = Mathf.CeilToInt(cx + r + 1f);
            int y0 = Mathf.FloorToInt(cy - r - 1f);
            int y1 = Mathf.CeilToInt(cy + r + 1f);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float cov = PixelCanvas.Coverage(d - r);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float nx = dx / r;
                    float ny = dy / r;
                    float rr = Mathf.Min(1f, (nx * nx) + (ny * ny));
                    float nz = Mathf.Sqrt(1f - rr);
                    float lambert = Mathf.Clamp01((-0.45f * nx) + (0.55f * ny) + (0.70f * nz));
                    float shade = 0.58f + (0.55f * lambert);
                    float rim = Mathf.Lerp(1f, 0.82f, Mathf.SmoothStep(0.72f, 1f, Mathf.Sqrt(rr)));
                    float hx = nx + 0.38f;
                    float hy = ny - 0.42f;
                    float spot = Mathf.Clamp01(1f - (Mathf.Sqrt((hx * hx) + (hy * hy)) / 0.32f));
                    var c = new Color(baseColor.r * shade * rim, baseColor.g * shade * rim, baseColor.b * shade * rim, alpha);
                    c = Color.Lerp(c, new Color(1f, 1f, 0.94f, alpha), spot * spot * 0.5f);
                    pc.Blend(x, y, c, cov);
                }
            }
        }

        private static void Shadow(PixelCanvas pc, float cx, float cy, float rx, float ry, float alpha)
        {
            pc.FillEllipseSoft(cx, cy, rx, ry, 8f, new Color(0.02f, 0.10f, 0.05f, alpha));
        }

        private static void DrawCanopy(PixelCanvas pc, int piece, Color green)
        {
            Vector2 o = CellOrigin(piece) + new Vector2(64f, 64f);
            Shadow(pc, o.x + 5f, o.y - 26f, 50f, 20f, 0.30f);
            bool alt = piece % 2 == 1;
            Color back = new Color(green.r * 0.86f, green.g * 0.86f, green.b * 0.86f, 1f);
            Ball(pc, o.x + (alt ? 20f : -20f), o.y - 6f, alt ? 26f : 29f, back);
            Ball(pc, o.x + (alt ? -18f : 18f), o.y - 8f, alt ? 29f : 26f, back);
            Ball(pc, o.x, o.y + 14f, 31f, green);
            Ball(pc, o.x + (alt ? -6f : 6f), o.y - 14f, 22f, Color.Lerp(green, Color.white, 0.08f));
        }

        private static void DrawPalm(PixelCanvas pc, int piece, Color green, int fronds)
        {
            Vector2 o = CellOrigin(piece) + new Vector2(64f, 62f);
            Shadow(pc, o.x + 6f, o.y - 22f, 40f, 14f, 0.28f);
            Color dark = new Color(green.r * 0.75f, green.g * 0.75f, green.b * 0.75f, 1f);
            for (int i = 0; i < fronds; i++)
            {
                float a = ((i / (float)fronds) * Mathf.PI * 2f) + 0.3f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 tip = o + (dir * 52f);
                pc.FillCapsule(o.x, o.y, tip.x, tip.y, 3.2f, dark);
                var n = new Vector2(-dir.y, dir.x);
                for (int k = 1; k <= 6; k++)
                {
                    Vector2 p = o + (dir * (8f + (k * 7f)));
                    float len = 13f - k;
                    Color leaf = (k % 2 == 0) ? green : Color.Lerp(green, Color.white, 0.12f);
                    pc.FillCapsule(p.x, p.y, p.x + (n.x * len) + (dir.x * 4f), p.y + (n.y * len) + (dir.y * 4f), 2.6f, leaf);
                    pc.FillCapsule(p.x, p.y, p.x - (n.x * len) + (dir.x * 4f), p.y - (n.y * len) + (dir.y * 4f), 2.6f, leaf);
                }
            }

            Ball(pc, o.x, o.y, 7f, Palette.Hex(0x6B4A2E));
        }

        private static void DrawFlower(PixelCanvas pc, int piece, Color bloom)
        {
            Vector2 o = CellOrigin(piece) + new Vector2(64f, 64f);
            Shadow(pc, o.x + 5f, o.y - 26f, 46f, 18f, 0.28f);
            Color back = new Color(bloom.r * 0.85f, bloom.g * 0.85f, bloom.b * 0.85f, 1f);
            Ball(pc, o.x - 18f, o.y - 6f, 26f, back);
            Ball(pc, o.x + 18f, o.y - 8f, 25f, back);
            Ball(pc, o.x, o.y + 12f, 29f, bloom);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.39996f;
                float r = 6f + ((i % 4) * 7f);
                Ball(pc, o.x + (Mathf.Cos(a) * r), o.y + 8f + (Mathf.Sin(a) * r * 0.8f), 5f, Color.Lerp(bloom, Color.white, 0.35f));
            }
        }

        private static void DrawChevron(PixelCanvas pc, int piece)
        {
            Vector2 o = CellOrigin(piece);
            pc.FillCapsule(o.x + 38f, o.y + 30f, o.x + 84f, o.y + 64f, 9f, Color.white);
            pc.FillCapsule(o.x + 38f, o.y + 98f, o.x + 84f, o.y + 64f, 9f, Color.white);
        }

        private static void DrawGlint(PixelCanvas pc, int piece)
        {
            Vector2 o = CellOrigin(piece) + new Vector2(64f, 64f);
            pc.FillEllipseSoft(o.x, o.y, 54f, 5f, 4f, Color.white);
            pc.FillEllipseSoft(o.x, o.y, 5f, 54f, 4f, Color.white);
            pc.FillEllipseSoft(o.x, o.y, 14f, 14f, 10f, Color.white);
        }

        private static void DrawBuoy(PixelCanvas pc, int piece)
        {
            Vector2 o = CellOrigin(piece) + new Vector2(64f, 66f);
            Shadow(pc, o.x + 6f, o.y - 44f, 40f, 12f, 0.35f);
            pc.FillCircle(o.x, o.y, 50f, Palette.Hex(0x2F6F7A));
            Ball(pc, o.x, o.y, 45f, Palette.Hex(0xBFE6EE), 0.96f);
            pc.FillEllipseSoft(o.x - 16f, o.y + 20f, 13f, 8f, 4f, new Color(1f, 1f, 1f, 0.9f));
            pc.FillCapsule(o.x - 30f, o.y - 8f, o.x + 30f, o.y - 8f, 5f, new Color(1f, 1f, 1f, 0.35f));
        }

        private static void DrawCloud(PixelCanvas pc, int piece)
        {
            Vector2 o = CellOrigin(piece) + new Vector2(64f, 64f);
            pc.FillEllipseSoft(o.x, o.y, 36f, 22f, 24f, Color.white);
        }

        // Elipse girada con borde suave (las hojas del primer plano).
        private static void Leaf(PixelCanvas pc, Vector2 c, float rx, float ry, float angle, float soft, Color color)
        {
            float cs = Mathf.Cos(angle);
            float sn = Mathf.Sin(angle);
            float reach = Mathf.Max(rx, ry) + soft + 2f;
            float r = Mathf.Min(rx, ry);
            for (int y = Mathf.FloorToInt(c.y - reach); y <= Mathf.CeilToInt(c.y + reach); y++)
            {
                for (int x = Mathf.FloorToInt(c.x - reach); x <= Mathf.CeilToInt(c.x + reach); x++)
                {
                    float dx = (x + 0.5f) - c.x;
                    float dy = (y + 0.5f) - c.y;
                    float lx = ((dx * cs) + (dy * sn)) / rx;
                    float ly = ((-dx * sn) + (dy * cs)) / ry;
                    float d = (Mathf.Sqrt((lx * lx) + (ly * ly)) - 1f) * r;
                    pc.Blend(x, y, color, PixelCanvas.SoftCoverage(d, soft));
                }
            }
        }

        private static PixelCanvas DrawForeground(bool topRight)
        {
            const int s = ForegroundPx;
            var pc = new PixelCanvas(s, s);
            pc.Fill(new Color(0f, 0f, 0f, 0f));
            // Abajo a la izquierda las hojas salen de la esquina (0, 0); arriba a la derecha, de (s, s).
            Vector2 corner = topRight ? new Vector2(s, s) : Vector2.zero;
            float sign = topRight ? -1f : 1f;
            Color deep = Palette.Hex(0x174F2A);
            Color mid = Palette.Hex(0x22693A);
            for (int i = 0; i < 5; i++)
            {
                float a = (topRight ? Mathf.PI : 0f) + 0.15f + (i * 0.30f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float len = 110f + ((i % 3) * 18f);
                Vector2 c = corner + (dir * len * 0.62f);
                Leaf(pc, c, len * 0.6f, 26f + ((i % 2) * 6f), a, 5f, i % 2 == 0 ? deep : mid);
                Vector2 tip = corner + (dir * len * 1.15f);
                pc.FillCapsule(corner.x + (dir.x * 12f), corner.y + (dir.y * 12f), tip.x, tip.y, 1.6f, new Color(0.55f, 0.75f, 0.45f, 0.5f));
            }

            // La heliconia: un tallo con brácteas rojas alternas, como barcas, con el borde amarillo.
            Vector2 stem = corner + new Vector2(sign * 64f, sign * 40f);
            Vector2 top = stem + new Vector2(sign * 44f, sign * 86f);
            Color red = Palette.Hex(0xD9342B);
            Color yellow = Palette.Hex(0xF2C230);
            pc.FillCapsule(stem.x, stem.y, top.x, top.y, 2.2f, Palette.Hex(0x2F6B2E));
            for (int k = 0; k < 4; k++)
            {
                Vector2 b = Vector2.Lerp(stem, top, (k + 0.5f) / 4f);
                float side = k % 2 == 0 ? 1f : -1f;
                float angle = (topRight ? Mathf.PI : 0f) + 1.05f + (side * 0.75f);
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 c = b + (dir * 12f);
                Leaf(pc, c, 14f, 5.5f, angle, 1.5f, red);
                Leaf(pc, c + (dir * 2f) + new Vector2(0f, 2f), 11f, 1.6f, angle, 1f, yellow);
            }

            return pc;
        }

        private static PixelCanvas DrawAndes()
        {
            var pc = new PixelCanvas(AndesWidthPx, AndesHeightPx);
            pc.Fill(new Color(0f, 0f, 0f, 0f));
            Color far = Palette.Hex(0x7F9CC0);
            Color near = Palette.Hex(0x6787AE);
            for (int x = 0; x < AndesWidthPx; x++)
            {
                float u = x / (float)AndesWidthPx;
                float farH = 40f + (18f * Mathf.Sin((u * 19f) + 1.3f)) + (10f * Mathf.Sin((u * 47f) + 0.4f)) + (5f * Mathf.Sin(u * 113f));
                float nearH = 26f + (14f * Mathf.Sin((u * 13f) + 2.1f)) + (8f * Mathf.Sin((u * 37f) + 0.9f));
                for (int y = 0; y < AndesHeightPx; y++)
                {
                    float fy = y + 0.5f;
                    if (fy < farH)
                    {
                        bool snow = farH > 52f && fy > farH - 7f;
                        pc.Blend(x, y, snow ? new Color(0.94f, 0.96f, 0.98f, 1f) : far, Mathf.Clamp01(farH - fy));
                    }

                    if (fy < nearH)
                    {
                        pc.Blend(x, y, near, Mathf.Clamp01(nearH - fy));
                    }

                    // El pie se funde con la niebla: sin borde de abajo.
                    Color c = pc.Get(x, y);
                    c.a *= Mathf.SmoothStep(0f, 1f, fy / (AndesHeightPx * 0.45f));
                    pc.Set(x, y, c);
                }
            }

            return pc;
        }

        private static PixelCanvas DrawLight()
        {
            const int s = 64;
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float u = x / (s - 1f);
                    float v = 1f - (y / (s - 1f)); // 0 arriba
                    float a = Mathf.Clamp01(1f - ((u + v) / 1.7f));
                    pc.Set(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }

            return pc;
        }

        private static PixelCanvas DrawFog()
        {
            var pc = new PixelCanvas(4, 64);
            for (int y = 0; y < 64; y++)
            {
                float v = y / 63f; // 1 arriba
                float a = Mathf.Pow(v, 1.6f);
                for (int x = 0; x < 4; x++)
                {
                    pc.Set(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            return pc;
        }
    }
}
