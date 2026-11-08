#nullable enable
using System;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Marcos de país del avatar por código (Q-M6 de D-033, añadido del 2026-11-08; D-052: arte por código en todos los builds, sin
    /// arte nuevo de artista). Cada marco ocupa el sitio del aro de madera (avatares.md §2.1, punto 4: «el retrato no cambia cuando
    /// lleguen los marcos»), algo más ancho (del 80 % al 100 % del radio, frente al 86 % del aro), y lleva:
    /// <list type="bullet">
    /// <item>una banda con los colores del mundo y su canto de tinta, que es lo que se lee en el retrato pequeño (24-40 dp);</item>
    /// <item>un motivo del mundo repetido a lo largo de la banda (12 piezas): açaí en el Estuario, olas en el Tapajós, el Encuentro de
    /// las Aguas en Manaos (media banda negra y media arena, sin mezclarse), hojas en el Trapecio, aguaje en Loreto, cacao en el
    /// Oriente, plumas de guacamayo en Madre de Dios, rombos en el Madidi y puntos de atardecer en el Ucayali; los de los mundos
    /// 10-13 están por si la Bitácora los da al acabar el contenido. Motivos naturales, nunca patrones de un pueblo (el kené
    /// shipibo queda fuera) [P, pendiente del asesor cultural];</item>
    /// <item>abajo, una placa con la bandera simplificada del país (la de la canoa del Mapa, <see cref="FlagColor"/>; uso de los
    /// símbolos nacionales con la misma reserva [NV] que la canoa), salvo en los mundos de varios países (sin placa).</item>
    /// </list>
    /// El retrato elegido (<see cref="AvatarPortrait(string, int, AvatarArtBackdrop, int, int, string)"/>) lo lleva dentro, así que se
    /// ve en todas partes donde sale el avatar del jugador; las celdas de «Tu avatar» y la libreta usan el marco suelto
    /// (<see cref="AvatarFrameOverlay"/>), con el centro transparente, encima del retrato del atlas.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Radio interior de la banda del marco en fracción del radio del retrato (el aro de madera empieza en el 86 %).</summary>
        public const float AvatarFrameInner = 0.80f;

        /// <summary>Ancho de la placa de la bandera en fracción del radio del retrato.</summary>
        public const float AvatarFramePlaqueWidth = 0.58f;

        /// <summary>Piezas del motivo a lo largo de la banda.</summary>
        public const int AvatarFrameMotifs = 12;

        private enum FrameMotif
        {
            Dot = 0,
            Berry = 1,
            Wave = 2,
            Split = 3,
            Leaf = 4,
            Pod = 5,
            Feather = 6,
            Diamond = 7,
        }

        private readonly struct FrameStyle
        {
            public FrameStyle(int outer, int inner, int motif, int motif2, FrameMotif kind)
            {
                Outer = Palette.Hex(outer);
                Inner = Palette.Hex(inner);
                Motif = Palette.Hex(motif);
                Motif2 = Palette.Hex(motif2);
                Kind = kind;
            }

            public Color Outer { get; }

            public Color Inner { get; }

            public Color Motif { get; }

            public Color Motif2 { get; }

            public FrameMotif Kind { get; }
        }

        // Índice 0 = mundo 1 (glosario §8). Colores del entorno de cada mundo [P], nunca un HEX de tinte de líquido.
        private static readonly FrameStyle[] FrameStyles =
        {
            new FrameStyle(0x2F7D32, 0x1F5A24, 0x3A1838, 0x8E5A9A, FrameMotif.Berry),   // 1 Estuario: hoja con açaí
            new FrameStyle(0x1E9AAE, 0x13788F, 0xF2E2B8, 0xF2E2B8, FrameMotif.Wave),    // 2 Tapajós: agua clara y arena
            new FrameStyle(0x2A1B12, 0xC99A5B, 0xF2E2B8, 0xF2E2B8, FrameMotif.Split),   // 3 Manaos: el Encuentro de las Aguas
            new FrameStyle(0xD9A62E, 0xB8841C, 0x2F7D32, 0x1F5A24, FrameMotif.Leaf),    // 4 Trapecio: oro con hojas
            new FrameStyle(0x8A3B22, 0x6B2A18, 0xE07A2E, 0x5A1E10, FrameMotif.Berry),   // 5 Loreto: aguaje
            new FrameStyle(0x5A3220, 0x3F2216, 0xE3A23A, 0xB8761E, FrameMotif.Pod),     // 6 Oriente: cacao
            new FrameStyle(0xB5452F, 0x8E3322, 0x2A6FB8, 0xF2C230, FrameMotif.Feather), // 7 Madre de Dios: collpa y plumas
            new FrameStyle(0x2B4C8C, 0x1E3768, 0xE9C24A, 0xE9C24A, FrameMotif.Diamond), // 8 Madidi: azul de las alturas y rombos de oro
            new FrameStyle(0xD87A2E, 0xB05A1E, 0xFFF1D0, 0xFFF1D0, FrameMotif.Dot),     // 9 Ucayali: atardecer
            new FrameStyle(0x241812, 0x3A2A1E, 0xE9C24A, 0xE9C24A, FrameMotif.Dot),     // 10 Alto Negro: agua negra
            new FrameStyle(0x2E7A6A, 0x1F5C50, 0xF6F1E4, 0xF6F1E4, FrameMotif.Diamond), // 11 Escudo de Guayana
            new FrameStyle(0xC0283A, 0x962030, 0x3E9A3E, 0xF2C230, FrameMotif.Feather), // 12 Invitados del Orinoco: Caño Cristales
            new FrameStyle(0xDDE6EE, 0xAFC3D2, 0x2E4A6B, 0x2E4A6B, FrameMotif.Diamond), // 13 El origen: nieve
        };

        private const int FrameStyleCount = 13;

        private Sprite? _frameOverlay;
        private string? _frameOverlayKey;

        /// <summary>Hay estilo de marco para el mundo <paramref name="world"/> (1-13).</summary>
        public static bool HasAvatarFrame(int world) => world >= 1 && world <= FrameStyleCount;

        /// <summary>
        /// El retrato elegido con el marco de país del mundo <paramref name="frameWorld"/> (0 = sin marco: el aro de madera) y la bandera
        /// de <paramref name="frameCountry"/> (null: sin placa). Con PNG del artista para el avatar, el PNG sin marco [P] (cuando llegue el
        /// arte, el artista entrega también los marcos). Se rehace solo al cambiar de avatar, de marco o de tamaño.
        /// </summary>
        public Sprite AvatarPortrait(string id, int index, AvatarArtBackdrop backdrop, int px, int frameWorld, string? frameCountry)
        {
            Sprite? png = ArtSlots.Find(AvatarSlot(id));
            if (png != null)
            {
                return png;
            }

            if (!HasAvatarFrame(frameWorld))
            {
                return AvatarPortrait(id, index, backdrop, px);
            }

            px = Mathf.Clamp(px, 16, 1024);
            string key = id + "@" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "@" + px.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "@f" + frameWorld.ToString(System.Globalization.CultureInfo.InvariantCulture) + (frameCountry ?? string.Empty);
            if (_avatarPortrait != null && string.Equals(_avatarPortraitKey, key, StringComparison.Ordinal))
            {
                return _avatarPortrait;
            }

            ForgetAvatarPortrait();
            var pc = new PixelCanvas(px, px);
            var scratch = new PixelCanvas(px, px);
            PaintAvatar(pc, scratch, index, backdrop, AvatarArtBadge.None, false);
            scratch.Release();
            float half = px * 0.5f;
            PaintCountryFrame(pc, new AvFrame(half, half, half * AvatarDiscFraction, px), frameWorld, frameCountry);
            Texture2D tex = pc.ToTexture("avatar_" + id, false, false, true);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "avatar_" + id + "_frame_w" + frameWorld.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
            sprite.hideFlags = HideFlags.DontSave;
            _avatarPortrait = sprite;
            _avatarPortraitKey = key;
            return sprite;
        }

        /// <summary>
        /// El marco suelto del mundo <paramref name="world"/> (centro transparente) de <paramref name="px"/> × <paramref name="px"/>, para
        /// ponerlo encima de un retrato del atlas (las celdas de «Tu avatar») o como icono (la libreta). Una sola textura viva a la vez:
        /// la caché guarda el último pedido; quien enseña varios a la vez los pide con <see cref="AvatarFrameSprite"/>.
        /// </summary>
        public Sprite AvatarFrameOverlay(int world, string? country, int px)
        {
            px = Mathf.Clamp(px, 16, 1024);
            string key = world.ToString(System.Globalization.CultureInfo.InvariantCulture) + (country ?? string.Empty) + "@" + px.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (_frameOverlay != null && string.Equals(_frameOverlayKey, key, StringComparison.Ordinal))
            {
                return _frameOverlay;
            }

            ForgetAvatarFrameOverlay();
            _frameOverlay = AvatarFrameSprite(world, country, px);
            _frameOverlayKey = key;
            return _frameOverlay;
        }

        /// <summary>
        /// Un marco suelto nuevo (quien lo pide lo destruye con <see cref="DestroyAvatarFrameSprite"/>): las celdas de los marcos de
        /// «Tu avatar», uno por marco ganado, que se sueltan al cerrar la pantalla.
        /// </summary>
        public static Sprite AvatarFrameSprite(int world, string? country, int px)
        {
            px = Mathf.Clamp(px, 16, 1024);
            var pc = new PixelCanvas(px, px);
            float half = px * 0.5f;
            PaintCountryFrame(pc, new AvFrame(half, half, half * AvatarDiscFraction, px), world, country);
            string name = "avatar_frame_w" + world.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
            Texture2D tex = pc.ToTexture(name, false, false, true);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>Destruye un marco de <see cref="AvatarFrameSprite"/> y su textura.</summary>
        public static void DestroyAvatarFrameSprite(Sprite? sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Texture2D tex = sprite.texture;
            DestroyNow(sprite);
            DestroyNow(tex);
        }

        /// <summary>Destruye el marco suelto de la caché (al cerrar la libreta y la escena).</summary>
        public void ForgetAvatarFrameOverlay()
        {
            DestroyAvatarFrameSprite(_frameOverlay);
            _frameOverlay = null;
            _frameOverlayKey = null;
        }

        /// <summary>Los píxeles del marco suelto (tests y exportación; abajo a la izquierda primero, como <see cref="Texture2D"/>).</summary>
        public static Color32[] AvatarFramePixels(int world, string? country, int px)
        {
            var pc = new PixelCanvas(px, px);
            float half = px * 0.5f;
            PaintCountryFrame(pc, new AvFrame(half, half, half * AvatarDiscFraction, px), world, country);
            return pc.TakeColor32();
        }

        /// <summary>Los píxeles de un retrato con su marco (tests de legibilidad a 24 y 32 dp).</summary>
        public static Color32[] AvatarFramedPixels(int index, AvatarArtBackdrop backdrop, int world, string? country, int px)
        {
            var pc = new PixelCanvas(px, px);
            var scratch = new PixelCanvas(px, px);
            PaintAvatar(pc, scratch, index, backdrop, AvatarArtBadge.None, false);
            scratch.Release();
            float half = px * 0.5f;
            PaintCountryFrame(pc, new AvFrame(half, half, half * AvatarDiscFraction, px), world, country);
            return pc.TakeColor32();
        }

        /// <summary>Pinta el marco del mundo <paramref name="world"/> en <paramref name="pc"/> entero (cuadrado), encima de lo que haya.</summary>
        internal static void PaintAvatarFrame(PixelCanvas pc, int world, string? country)
        {
            float half = pc.Width * 0.5f;
            PaintCountryFrame(pc, new AvFrame(half, half, half * AvatarDiscFraction, pc.Width), world, country);
        }

        // La banda, el motivo y la placa con la bandera, encima de lo que haya (tapa el aro de madera). No asigna memoria.
        private static void PaintCountryFrame(PixelCanvas pc, in AvFrame f, int world, string? country)
        {
            if (!HasAvatarFrame(world))
            {
                return;
            }

            FrameStyle style = FrameStyles[world - 1];
            float rOut = f.N * 0.5f;
            float rIn = rOut * AvatarFrameInner;
            float bw = rOut - rIn;
            float rMid = (rIn + rOut) * 0.5f;
            float hb = bw * 0.5f;
            float ink = Mathf.Max(1f, f.N / 110f);
            float filet = Mathf.Max(1f, f.N * 0.012f);
            bool plaque = country != null && HasSimplifiedFlag(country);
            float plaqueW = rOut * AvatarFramePlaqueWidth;
            float plaqueH = plaqueW * (MapFlagHeightDp / MapFlagWidthDp);
            float plaqueCx = f.Cx;
            // Abajo, sobre la banda, y entera dentro del retrato (y hacia arriba, como el resto del retrato).
            float plaqueCy = Mathf.Max(f.Cy - rMid, (plaqueH * 0.5f) + (2f * ink) + 1.5f);
            float skipAngle = plaque ? Mathf.Atan2((plaqueW * 0.5f) + hb, rMid) : 0f;
            float step = (Mathf.PI * 2f) / AvatarFrameMotifs;
            float offset = (Mathf.PI * -0.5f) + (step * 0.5f); // ninguna pieza justo abajo, donde va la placa
            Color edge = Color.Lerp(style.Inner, AvInk, 0.7f);
            edge.a = 1f;
            float lim = rOut + 1f;
            float inLim = rIn - filet - 1.5f;
            float lim2 = lim * lim;
            float inLim2 = inLim > 0f ? inLim * inLim : 0f;
            Color cream = Palette.Cream;
            Color inner = style.Inner;
            Color outerC = style.Outer;
            float twoPi = Mathf.PI * 2f;
            for (int y = 0; y < f.N; y++)
            {
                float dy = (y + 0.5f) - f.Cy;
                float dy2 = dy * dy;
                float h2 = lim2 - dy2;
                if (h2 <= 0f)
                {
                    continue;
                }

                // Fila a fila, solo el tramo dentro del canto de fuera y sin el hueco de dentro (como el aro).
                float h = Mathf.Sqrt(h2);
                int xa = Mathf.Max(0, Mathf.FloorToInt(f.Cx - h - 1f));
                int xb = Mathf.Min(f.N - 1, Mathf.CeilToInt(f.Cx + h));
                int xs0 = int.MaxValue;
                int xs1 = int.MinValue;
                float hs2 = inLim2 - dy2;
                if (hs2 > 0f)
                {
                    float hs = Mathf.Sqrt(hs2);
                    xs0 = Mathf.CeilToInt(f.Cx - hs + 0.5f);
                    xs1 = Mathf.FloorToInt(f.Cx + hs - 1.5f);
                }

                for (int x = xa; x <= xb; x++)
                {
                    if (x == xs0 && xs0 <= xs1)
                    {
                        x = xs1;
                        continue;
                    }

                    float dx = (x + 0.5f) - f.Cx;
                    float r2 = (dx * dx) + dy2;
                    if (r2 > lim2 || r2 < inLim2)
                    {
                        continue;
                    }

                    float r = Mathf.Sqrt(r2);
                    float outer = PixelCanvas.Coverage(r - rOut + 0.5f);
                    if (outer <= 0f)
                    {
                        continue;
                    }

                    if (r < rIn)
                    {
                        // El filete crema por dentro, como el del aro.
                        float cf = PixelCanvas.Coverage((rIn - filet) - r);
                        if (cf > 0f)
                        {
                            pc.Blend(x, y, cream, cf);
                        }

                        continue;
                    }

                    float u = (r - rIn) / bw; // 0 dentro, 1 fuera
                    if (style.Kind == FrameMotif.Split)
                    {
                        // El Encuentro de las Aguas: la mitad izquierda negra y la derecha arena, con la costura ondulada.
                        float seam = rOut * 0.10f * Mathf.Sin((dy / rOut) * 7f);
                        u = PixelCanvas.Coverage((dx - seam) * 0.8f); // 1 a la izquierda (Outer, el agua negra); 0 a la derecha (la arena)
                    }

                    float cr = inner.r + ((outerC.r - inner.r) * u);
                    float cg = inner.g + ((outerC.g - inner.g) * u);
                    float cb = inner.b + ((outerC.b - inner.b) * u);

                    // Luz de juego arriba a la izquierda y un canto de tinta por dentro y por fuera.
                    float light = ((-dx + dy) / Mathf.Max(1f, r)) * 0.7071f * 0.5f; // −0,5..0,5
                    if (light > 0f)
                    {
                        float k = light * 0.28f;
                        cr += (1f - cr) * k;
                        cg += (1f - cg) * k;
                        cb += (1f - cb) * k;
                    }
                    else
                    {
                        float k = 1f + (light * 0.22f);
                        cr *= k;
                        cg *= k;
                        cb *= k;
                    }

                    float edgeIn = PixelCanvas.Coverage((r - rIn) - ink);
                    float edgeOut = PixelCanvas.Coverage((rOut - r) - ink);
                    float edgeK = edgeIn > edgeOut ? edgeIn : edgeOut;
                    if (edgeK > 0f)
                    {
                        cr += (edge.r - cr) * edgeK;
                        cg += (edge.g - cg) * edgeK;
                        cb += (edge.b - cb) * edgeK;
                    }

                    // El motivo, en la pieza más cercana (coordenadas locales: t a lo largo de la banda, n hacia fuera).
                    if (style.Kind != FrameMotif.Split)
                    {
                        float theta = Mathf.Atan2(dy, dx);
                        float rel = theta - offset;
                        int j = Mathf.RoundToInt(rel / step);
                        float dTheta = rel - (j * step);
                        float a = step * (j + 0.5f); // el ángulo de la pieza desde abajo
                        a -= twoPi * Mathf.Round(a / twoPi);
                        if (!plaque || Mathf.Abs(a) > skipAngle)
                        {
                            float t = dTheta * rMid;
                            float n = r - rMid;
                            float d = MotifSdf(style.Kind, t, n, hb, theta, rMid);
                            float cov = PixelCanvas.Coverage(d);
                            if (cov > 0f)
                            {
                                Color mc = (j & 1) == 0 ? style.Motif : style.Motif2;
                                cr += (mc.r - cr) * cov;
                                cg += (mc.g - cg) * cov;
                                cb += (mc.b - cb) * cov;
                                if (style.Kind == FrameMotif.Berry)
                                {
                                    // El brillo del fruto, arriba a la izquierda.
                                    float tx = t + (hb * 0.18f);
                                    float ny = n - (hb * 0.18f);
                                    float hk = PixelCanvas.Coverage(Mathf.Sqrt((tx * tx) + (ny * ny)) - (hb * 0.16f)) * 0.8f * cov;
                                    cr += (cream.r - cr) * hk;
                                    cg += (cream.g - cg) * hk;
                                    cb += (cream.b - cb) * hk;
                                }
                            }
                        }
                    }

                    pc.Blend(x, y, new Color(cr, cg, cb, 1f), outer);
                }
            }

            if (plaque)
            {
                PaintFlagPlaque(pc, plaqueCx, plaqueCy, plaqueW, plaqueH, country!, ink);
            }
        }

        // Distancia con signo (px; negativa dentro) de una pieza del motivo en coordenadas locales de la banda.
        private static float MotifSdf(FrameMotif kind, float t, float n, float hb, float theta, float rMid)
        {
            switch (kind)
            {
                case FrameMotif.Berry:
                    return Mathf.Sqrt((t * t) + (n * n)) - (hb * 0.52f);
                case FrameMotif.Dot:
                    return Mathf.Sqrt((t * t) + (n * n)) - (hb * 0.34f);
                case FrameMotif.Wave:
                {
                    // Una línea ondulada continua a lo largo de la banda (el ancho no depende de la pieza).
                    float s = theta * rMid;
                    float period = (Mathf.PI * 2f * rMid) / (AvatarFrameMotifs * 1.5f);
                    float n0 = hb * 0.38f * Mathf.Sin((s / period) * Mathf.PI * 2f);
                    return Mathf.Abs(n - n0) - (hb * 0.16f);
                }

                case FrameMotif.Leaf:
                    return Ellipse(t, n, hb * 0.95f, hb * 0.42f);
                case FrameMotif.Pod:
                    return Ellipse(t, n, hb * 0.80f, hb * 0.48f);

                case FrameMotif.Feather:
                    return Ellipse(t, n, hb * 0.34f, hb * 0.80f);
                case FrameMotif.Diamond:
                    return ((Mathf.Abs(t) + Mathf.Abs(n)) - (hb * 0.62f)) * 0.7071f;
                default:
                    return 1e3f;
            }
        }

        // Aproximación de la distancia a una elipse de semiejes a (t) y b (n), en px.
        private static float Ellipse(float t, float n, float a, float b)
        {
            float k = Mathf.Sqrt(((t * t) / (a * a)) + ((n * n) / (b * b)));
            return (k - 1f) * Mathf.Min(a, b);
        }

        // La placa de la bandera: canto de tinta, filete crema y el paño de la bandera simplificada.
        private static void PaintFlagPlaque(PixelCanvas pc, float cx, float cy, float w, float h, string country, float ink)
        {
            float rad = Mathf.Max(1f, h * 0.18f);
            float border = Mathf.Max(1f, ink);
            float hw = (w * 0.5f) + border;
            float hh = (h * 0.5f) + border;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - hw - ink - 1f));
            int x1 = Mathf.Min(pc.Width - 1, Mathf.CeilToInt(cx + hw + ink + 1f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - hh - ink - 1f));
            int y1 = Mathf.Min(pc.Height - 1, Mathf.CeilToInt(cy + hh + ink + 1f));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - cx;
                    float py = (y + 0.5f) - cy;
                    float dOuter = RoundRect(px, py, hw, hh, rad + border);
                    pc.Blend(x, y, AvInk, PixelCanvas.Coverage(dOuter - ink));
                    pc.Blend(x, y, Palette.Cream, PixelCanvas.Coverage(dOuter));
                    float dIn = RoundRect(px, py, w * 0.5f, h * 0.5f, rad);
                    float cov = PixelCanvas.Coverage(dIn);
                    if (cov > 0f)
                    {
                        float xd = ((px / w) + 0.5f) * MapFlagWidthDp;
                        float yd = ((py / h) + 0.5f) * MapFlagHeightDp;
                        pc.Blend(x, y, FlagColor(country, xd, yd), cov);
                    }
                }
            }
        }

        private static float RoundRect(float x, float y, float hw, float hh, float r)
        {
            float qx = Mathf.Abs(x) - hw + r;
            float qy = Mathf.Abs(y) - hh + r;
            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt((ox * ox) + (oy * oy)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }
    }
}
