#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Piraña (M7; 02 §9.1 M7; propuesta de D-007 b.2 M7; D-003 «Selva brillante premium»): la
    /// boca de la cueva, un arco de piedras grises con musgo y el hueco en sombra; la piraña del cardumen, plateada, de cuerpo
    /// alto, con el lomo oscuro, la cola con banda negra y el ojo grande, sin dientes exagerados ni sangre (también dormida,
    /// con el ojo cerrado); y el bocado de fruta que se lleva a la cueva. Lo usa <c>PiranaOverlay</c>.
    /// </summary>
    /// <remarks>
    /// <para>Color (direccion-visual-v2 §2.4, regla 1): las superficies de más de 2 dp quedan a ΔE00 ≥ 10 de los 14 tintes
    /// (piedras de <see cref="StoneLight"/> y <see cref="StoneDark"/>, hueco <see cref="PiranaCaveDark"/> 18,8, musgo
    /// <see cref="PiranaMoss"/> 30,6, piraña <see cref="PiranaFishSilver"/> 13,6, <see cref="PiranaFishBelly"/> 11,8,
    /// <see cref="PiranaFishBack"/> 17,0 y <see cref="PiranaFishFin"/> 16,4). Brillos, ojo, branquia, motas y banda de la cola
    /// miden 2 dp o menos. El bocado lleva el tinte de la fruta mordida a propósito: el sprite es gris y lo tiñe
    /// <c>Image.color</c>. La plata clara se lee sobre el azul petróleo del panel (L* 27 frente a 79), también atenuada.</para>
    /// <para>Perezosos, como los de <c>ProceduralArt.Mechanics.cs</c>: se generan la primera vez que se piden y tras
    /// <see cref="Dispose"/> se vuelven a generar.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del sprite de la cueva en u.</summary>
        public const float PiranaCaveWidthU = 2.1f;

        /// <summary>Alto del sprite de la cueva en u.</summary>
        public const float PiranaCaveHeightU = 1.1f;

        /// <summary>Base del arco sobre el canto inferior del sprite de la cueva (u).</summary>
        public const float PiranaCaveBaseU = 0.1f;

        /// <summary>Ancho del sprite de la piraña en u.</summary>
        public const float PiranaFishWidthU = 0.9f;

        /// <summary>Alto del sprite de la piraña en u.</summary>
        public const float PiranaFishHeightU = 0.5625f;

        /// <summary>Hueco de la cueva, en sombra #0B161A [P] (ΔE00 18,8 de Huito): más oscuro que el panel para que se lea la boca.</summary>
        public static readonly Color PiranaCaveDark = Palette.Hex(0x0B161A);

        /// <summary>Fondo del hueco, algo más claro #132228 [P] (ΔE00 16,4 de Huito).</summary>
        public static readonly Color PiranaCaveFloor = Palette.Hex(0x132228);

        /// <summary>Musgo sobre las piedras #3F5F45 [P] (ΔE00 30,6 de Lulo).</summary>
        public static readonly Color PiranaMoss = Palette.Hex(0x3F5F45);

        /// <summary>Brillo del musgo #5E8A5F [P] (ΔE00 13,7 de Lulo).</summary>
        public static readonly Color PiranaMossLight = Palette.Hex(0x5E8A5F);

        /// <summary>Brillo de las piedras #A9B1BA [P] (ΔE00 18,4 de Agua del Tapajós).</summary>
        public static readonly Color PiranaStoneGlint = Palette.Hex(0xA9B1BA);

        /// <summary>Costado plateado de la piraña #B9C6CA [P] (ΔE00 13,6 de Guanábana).</summary>
        public static readonly Color PiranaFishSilver = Palette.Hex(0xB9C6CA);

        /// <summary>Vientre claro, sin rojo #C4CCD3 [P] (ΔE00 11,8 de Guanábana; b.2 M7: sin sangre).</summary>
        public static readonly Color PiranaFishBelly = Palette.Hex(0xC4CCD3);

        /// <summary>Lomo #515A64 [P] (ΔE00 17,0 de Açaí).</summary>
        public static readonly Color PiranaFishBack = Palette.Hex(0x515A64);

        /// <summary>Aletas #98A6AB [P] (ΔE00 16,4 de Agua del Tapajós).</summary>
        public static readonly Color PiranaFishFin = Palette.Hex(0x98A6AB);

        /// <summary>Contorno, ojo y banda de la cola #1B2B31 [P].</summary>
        public static readonly Color PiranaFishOutline = Palette.Hex(0x1B2B31);

        private Sprite? _piranaCave;
        private Sprite? _piranaFish;
        private Sprite? _piranaFishAsleep;
        private Sprite? _piranaMorsel;

        /// <summary>Boca de la cueva: arco de piedras con musgo y hueco en sombra (2,1 × 1,1 u; colores horneados).</summary>
        internal Sprite PiranaCaveSprite => _piranaCave != null ? _piranaCave : (_piranaCave = BuildPiranaCave());

        /// <summary>Piraña del cardumen mirando a la izquierda (0,9 × 0,5625 u; colores horneados).</summary>
        internal Sprite PiranaFishSprite => _piranaFish != null ? _piranaFish : (_piranaFish = BuildPiranaFish(false));

        /// <summary>La misma piraña dormida: el ojo cerrado (cueva llena, respuesta Q-C).</summary>
        internal Sprite PiranaFishAsleepSprite => _piranaFishAsleep != null ? _piranaFishAsleep : (_piranaFishAsleep = BuildPiranaFish(true));

        /// <summary>Bocado de fruta con la mordida (gris con volumen; lo tiñe <c>Image.color</c> con la fruta).</summary>
        internal Sprite PiranaMorselSprite => _piranaMorsel != null ? _piranaMorsel : (_piranaMorsel = BuildPiranaMorsel());

        private Sprite BuildPiranaCave()
        {
            int w = Mathf.RoundToInt(PiranaCaveWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(PiranaCaveHeightU * MechanicPxPerU);
            const float k = MechanicPxPerU / 96f;
            float cx = w * 0.5f;
            float baseY = PiranaCaveBaseU * MechanicPxPerU;
            var pc = new PixelCanvas(w, h);

            // Hueco en sombra: media elipse con el fondo algo más claro y un halo oscuro hacia el centro.
            float rxIn = w * 0.31f;
            float ryIn = h * 0.64f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    if (py < baseY - 1f)
                    {
                        continue;
                    }

                    float nx = (px - cx) / rxIn;
                    float ny = (py - baseY) / ryIn;
                    float d = (Mathf.Sqrt((nx * nx) + (ny * ny)) - 1f) * Mathf.Min(rxIn, ryIn);
                    float cover = PixelCanvas.Coverage(d) * Mathf.Clamp01(py - baseY + 1f);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    Color c = Color.Lerp(PiranaCaveFloor, PiranaCaveDark, Mathf.Clamp01(ny * 1.4f));
                    pc.Blend(x, y, c, cover);
                }
            }

            // Siete piedras sobre el arco, de las patas a la clave (la clave, encima de todas), con el lado largo a lo largo
            // del arco y un casquete de musgo irregular sobre las tres de arriba.
            float rxArc = w * 0.37f;
            float ryArc = h * 0.64f;
            int[] order = { 0, 6, 1, 5, 2, 4, 3 };
            for (int n = 0; n < order.Length; n++)
            {
                int i = order[n];
                float angle = Mathf.Deg2Rad * Mathf.Lerp(165f, 15f, i / 6f);
                float sx = cx + (Mathf.Cos(angle) * rxArc);
                float sy = baseY + (Mathf.Sin(angle) * ryArc);
                var tangent = new Vector2(-Mathf.Sin(angle) * rxArc, Mathf.Cos(angle) * ryArc).normalized;
                float size = (i == 0 || i == 6 ? 19f : (i == 3 ? 17f : 15.5f)) * k;
                float along = size * (1.18f + (0.2f * ArtNoise.Hash(i, 7, 53)));
                float across = size * (0.8f + (0.14f * ArtNoise.Hash(i, 11, 53)));
                DrawPiranaStone(pc, sx, sy, tangent, along, across, 2.6f * k, i);
                if (i >= 2 && i <= 4)
                {
                    DrawPiranaMoss(pc, sx, sy + (across * 0.72f), along * 0.78f, k, i);
                }
            }

            return MakeSprite(pc, "mech_pirana_cave", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Piedra tallada: superelipse girada (lado largo along a lo largo de tangent, grueso across), contorno pizarra, cara
        // con luz arriba a la izquierda, cantos en sombra, una arista clara y una grieta.
        private static void DrawPiranaStone(PixelCanvas pc, float cx, float cy, Vector2 tangent, float along, float across, float outline, int seed)
        {
            const float power = 2.8f;
            var normal = new Vector2(-tangent.y, tangent.x);
            float reach = Mathf.Max(along, across) + outline + 2f;
            int x0 = Mathf.FloorToInt(cx - reach);
            int x1 = Mathf.CeilToInt(cx + reach);
            int y0 = Mathf.FloorToInt(cy - reach);
            int y1 = Mathf.CeilToInt(cy + reach);
            float r = Mathf.Min(along, across);
            float skew = (ArtNoise.Hash(seed, 3, 61) - 0.5f) * 0.3f;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    float lx = ((dx * tangent.x) + (dy * tangent.y)) / along;
                    float ly = ((dx * normal.x) + (dy * normal.y)) / across;
                    lx += skew * ly; // un poco torcida, como una laja
                    float len = Mathf.Pow(Mathf.Pow(Mathf.Abs(lx), power) + Mathf.Pow(Mathf.Abs(ly), power), 1f / power);
                    float d = (len - 1f) * r;
                    pc.Blend(x, y, Slate, PixelCanvas.Coverage(d - outline));
                    float inside = PixelCanvas.Coverage(d);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float nx = dx / Mathf.Max(along, across);
                    float ny = dy / Mathf.Max(along, across);
                    float lit = Mathf.Clamp01(0.55f + (0.5f * ny) - (0.35f * nx));
                    Color c = Color.Lerp(StoneDark, StoneLight, lit);
                    float edge = Mathf.Clamp01((len - 0.7f) / 0.3f);
                    c = Color.Lerp(c, ny + nx < 0f ? Slate : StoneDark, 0.35f * edge * edge); // cantos en sombra
                    if (len < 0.86f && len > 0.66f && ny > 0.1f && nx < 0.25f)
                    {
                        c = Color.Lerp(c, PiranaStoneGlint, 0.55f); // arista iluminada
                    }

                    pc.Blend(x, y, c, inside);
                }
            }

            // Grieta fina (≤ 2 dp) de un tercio de la piedra.
            float t0 = (ArtNoise.Hash(seed, 5, 61) - 0.5f) * 0.6f;
            Vector2 a = new Vector2(cx, cy) + (tangent * (t0 * along)) + (normal * (0.15f * across));
            Vector2 b = a + (tangent * (0.18f * along)) - (normal * (0.42f * across));
            pc.FillCapsule(a.x, a.y, b.x, b.y, 0.9f, ColorMath.WithAlpha(Slate, 0.7f));
        }

        // Casquete de musgo sobre una piedra: manchas solapadas de borde irregular, con brillos y sombras sueltos (nunca dos
        // puntos iguales que parezcan ojos).
        private static void DrawPiranaMoss(PixelCanvas pc, float cx, float cy, float half, float k, int seed)
        {
            for (int j = 0; j < 6; j++)
            {
                float t = (j / 5f) - 0.5f;
                float mx = cx + (t * 2f * half * 0.85f);
                float my = cy + ((1f - (4f * t * t)) * 2.2f * k) + ((ArtNoise.Hash(seed, j, 67) - 0.5f) * 2f * k);
                float r = (3.4f + (2f * ArtNoise.Hash(seed, j, 71))) * k;
                pc.FillCircle(mx, my, r + (1.2f * k), ColorMath.WithAlpha(Palette.Vein, 0.85f));
                pc.FillCircle(mx, my, r, PiranaMoss);
            }

            for (int j = 0; j < 7; j++)
            {
                float mx = cx + ((ArtNoise.Hash(seed, j, 73) - 0.5f) * 1.6f * half);
                float my = cy + ((ArtNoise.Hash(seed, j, 79) + 0.1f) * 3.2f * k);
                Color c = (j % 3) == 0 ? ColorMath.WithAlpha(Palette.Vein, 0.8f) : PiranaMossLight;
                pc.FillCircle(mx, my, (0.8f + (0.6f * ArtNoise.Hash(seed, j, 83))) * k, c);
            }
        }

        private Sprite BuildPiranaFish(bool asleep)
        {
            int w = Mathf.RoundToInt(PiranaFishWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(PiranaFishHeightU * MechanicPxPerU);
            const float k = MechanicPxPerU / 96f;
            float outline = 3f * k;
            float cx = w * 0.44f;
            float cy = h * 0.5f;
            float rx = w * 0.29f;
            float ry = h * 0.35f;
            var pc = new PixelCanvas(w, h);

            // Aletas y cola, detrás del cuerpo: primero su contorno (triángulos agrandados) y después la aleta.
            float tailBase = cx + (rx * 0.78f);
            var baseTop = new Vector2(tailBase, cy + (ry * 0.36f));
            var baseBottom = new Vector2(tailBase, cy - (ry * 0.36f));
            var tipTop = new Vector2(w - (4f * k), cy + (h * 0.37f));
            var notch = new Vector2(w - (12f * k), cy);
            var tipBottom = new Vector2(w - (4f * k), cy - (h * 0.37f));
            var dorsalA = new Vector2(cx - (rx * 0.18f), cy + (ry * 0.86f));
            var dorsalB = new Vector2(cx + (rx * 0.62f), cy + (ry * 0.62f));
            var dorsalTip = new Vector2(cx + (rx * 0.42f), cy + (ry * 1.32f));
            var analA = new Vector2(cx + (rx * 0.18f), cy - (ry * 0.86f));
            var analB = new Vector2(cx + (rx * 0.7f), cy - (ry * 0.56f));
            var analTip = new Vector2(cx + (rx * 0.58f), cy - (ry * 1.3f));
            for (int pass = 0; pass < 2; pass++)
            {
                float grow = pass == 0 ? outline : 0f;
                Color c = pass == 0 ? PiranaFishOutline : PiranaFishFin;
                FillPiranaTriangle(pc, baseTop, tipTop, notch, grow, c);
                FillPiranaTriangle(pc, baseBottom, notch, tipBottom, grow, c);
                FillPiranaTriangle(pc, baseTop, notch, baseBottom, grow, c);
                FillPiranaTriangle(pc, dorsalA, dorsalB, dorsalTip, grow, c);
                FillPiranaTriangle(pc, analA, analB, analTip, grow, c);
            }

            // Banda oscura en el borde de la cola (rasgo de muchas pirañas).
            for (int y = 0; y < h; y++)
            {
                for (int x = Mathf.FloorToInt(w - (11f * k)); x < w; x++)
                {
                    Color under = pc.Get(x, y);
                    if (under.a > 0.5f && under.r > PiranaFishOutline.r + 0.05f)
                    {
                        pc.Blend(x, y, PiranaFishOutline, 0.7f * Mathf.Clamp01((x + 0.5f - (w - (11f * k))) / (4f * k)));
                    }
                }
            }

            // Cuerpo: contorno y degradado de lomo oscuro, costado plateado y vientre claro; la mandíbula asoma abajo.
            float jawX = cx - (rx * 0.8f);
            float jawY = cy - (ry * 0.32f);
            float jawR = 5.5f * k;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float nx = (px - cx) / rx;
                    float ny = (py - cy) / ry;
                    float body = (Mathf.Sqrt((nx * nx) + (ny * ny)) - 1f) * Mathf.Min(rx, ry);
                    float jdx = px - jawX;
                    float jdy = py - jawY;
                    float jaw = Mathf.Sqrt((jdx * jdx) + (jdy * jdy)) - jawR;
                    float d = Mathf.Min(body, jaw);
                    if (d > outline + 1f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, PiranaFishOutline, PixelCanvas.Coverage(d - outline));
                    float inside = PixelCanvas.Coverage(d);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01((ny + 1f) * 0.5f);
                    Color c = t < 0.38f
                        ? Color.Lerp(PiranaFishBelly, PiranaFishSilver, Mathf.SmoothStep(0f, 1f, t / 0.38f))
                        : (t < 0.66f ? PiranaFishSilver : Color.Lerp(PiranaFishSilver, PiranaFishBack, Mathf.SmoothStep(0f, 1f, (t - 0.66f) / 0.34f)));
                    if (nx > 0.35f)
                    {
                        c = Color.Lerp(c, PiranaFishBack, 0.22f * Mathf.Clamp01((nx - 0.35f) / 0.65f)); // hacia la cola, más oscuro
                    }

                    pc.Blend(x, y, c, inside);
                }
            }

            // Aleta pectoral, motas, branquia y brillo del costado.
            pc.FillEllipseSoft(cx - (rx * 0.12f), cy - (ry * 0.22f), 6f * k, 3.4f * k, 1f, ColorMath.WithAlpha(PiranaFishFin, 0.95f));
            for (int i = 0; i < 6; i++)
            {
                float sx = cx + ((ArtNoise.Hash(i, 3, 71) - 0.25f) * rx * 1.1f);
                float sy = cy + ((0.15f + (0.45f * ArtNoise.Hash(i, 5, 71))) * ry);
                pc.FillCircle(sx, sy, 1.3f * k, ColorMath.WithAlpha(PiranaFishBack, 0.55f));
            }

            pc.StrokeArc(cx - (rx * 0.4f), cy + (ry * 0.02f), ry * 0.62f, 1.7f * k, -62f, 62f, ColorMath.WithAlpha(PiranaFishBack, 0.75f));
            pc.FillCapsule(cx - (rx * 0.2f), cy + (ry * 0.5f), cx + (rx * 0.42f), cy + (ry * 0.56f), 1.5f * k, new Color(1f, 1f, 1f, 0.55f));

            // Boca cerrada, sin dientes (b.2 M7), y ojo grande; dormida, el párpado cerrado.
            pc.FillCapsule(cx - rx - (1.5f * k), cy - (ry * 0.3f), cx - (rx * 0.66f), cy - (ry * 0.4f), 0.9f * k, PiranaFishOutline);
            float ex = cx - (rx * 0.56f);
            float ey = cy + (ry * 0.24f);
            if (asleep)
            {
                pc.StrokeArc(ex, ey + (1.2f * k), 3.6f * k, 1.7f * k, 200f, 340f, PiranaFishOutline);
            }
            else
            {
                pc.FillCircle(ex, ey, 5f * k, ColorMath.WithAlpha(Palette.Cream, 0.95f));
                pc.FillCircle(ex - (0.4f * k), ey, 3.6f * k, PiranaFishOutline);
                pc.FillCircle(ex - (1.4f * k), ey + (1.3f * k), 1.3f * k, Color.white);
            }

            return MakeSprite(pc, asleep ? "mech_pirana_fish_asleep" : "mech_pirana_fish", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Triángulo agrandado grow px desde su centro (contornos de las aletas).
        private static void FillPiranaTriangle(PixelCanvas pc, Vector2 a, Vector2 b, Vector2 c, float grow, Color color)
        {
            if (grow > 0f)
            {
                Vector2 center = (a + b + c) / 3f;
                a += (a - center).normalized * grow * 1.6f;
                b += (b - center).normalized * grow * 1.6f;
                c += (c - center).normalized * grow * 1.6f;
            }

            pc.FillTriangle(a, b, c, color);
        }

        private Sprite BuildPiranaMorsel()
        {
            const int s = 40;
            const float c = s * 0.5f;
            const float r = 15f;
            const float biteX = 30.5f;
            const float biteY = 29.5f;
            const float biteR = 7.5f;
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = px - c;
                    float dy = py - c;
                    float round = Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
                    float bx = px - biteX;
                    float by = py - biteY;
                    float bite = biteR - Mathf.Sqrt((bx * bx) + (by * by));
                    float d = Mathf.Max(round, bite); // el círculo menos la mordida
                    if (d > 1f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, new Color(0.32f, 0.32f, 0.32f, 1f), PixelCanvas.Coverage(d));
                    float inside = PixelCanvas.Coverage(d + 2.2f);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    // Hasta 0,86: el brillo blanco de encima sigue siendo más claro que la fruta al teñirlo.
                    float lit = Mathf.Clamp01(0.55f + (0.35f * (dy / r)) - (0.3f * (dx / r)));
                    float g = Mathf.Lerp(0.62f, 0.86f, lit);
                    pc.Blend(x, y, new Color(g, g, g, 1f), inside);
                }
            }

            pc.FillEllipseSoft(c - 5f, c + 5f, 3.6f, 2.6f, 1f, new Color(1f, 1f, 1f, 0.9f));
            return MakeSprite(pc, "mech_pirana_morsel", new Vector2(0.5f, 0.5f), Vector4.zero);
        }
    }
}
