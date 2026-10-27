#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Piraña (M7; 02 §9.1 M7; propuesta de D-007 b.2 M7; D-003 «Selva brillante premium»): la
    /// boca de la cueva, un arco de piedras grises con musgo y el hueco en sombra; la piraña del cardumen, plateada, de cuerpo
    /// alto, con el lomo oscuro, la cola con banda negra y el ojo grande, sin dientes exagerados ni sangre (también dormida,
    /// con el ojo cerrado); y el bocado de fruta que se lleva a la cueva, una cápsula del tinte con su contorno de vidrio. Lo
    /// usa <c>PiranaOverlay</c>.
    /// </summary>
    /// <remarks>
    /// <para>Color (direccion-visual-v2 §2.4, regla 1): las superficies de más de 2 dp quedan a ΔE00 ≥ 10 de los 14 tintes
    /// (piedras de <see cref="StoneLight"/> y <see cref="StoneDark"/>, hueco <see cref="PiranaCaveDark"/> 18,8, musgo
    /// <see cref="PiranaMoss"/> 30,6, piraña <see cref="PiranaFishSilver"/> 13,6, <see cref="PiranaFishBelly"/> 11,8,
    /// <see cref="PiranaFishBack"/> 17,0 y <see cref="PiranaFishFin"/> 16,4). Brillos, ojo, branquia, motas y banda de la cola
    /// miden 2 dp o menos. El bocado lleva el tinte de la fruta mordida a propósito: el sprite es claro y lo tiñe
    /// <c>Image.color</c>; su contorno (<see cref="PiranaMorselRimSprite"/>, ≈ 1,5 dp de <c>vidrio_borde</c>) lo separa del
    /// panel con cualquier tinte, también los oscuros (Cacao, Huito, Açaí). La plata clara se lee sobre el azul petróleo del
    /// panel (L* 27 frente a 79), también atenuada.</para>
    /// <para>Perezosos, como los de <c>ProceduralArt.Mechanics.cs</c>: se generan la primera vez que se piden y tras
    /// <see cref="Dispose"/> se vuelven a generar.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Ancho del sprite de la cueva en u: el hueco del arco mide ≥ 1,3 u a la altura de los puntos, así que caben 4 (o 5,
        /// algo más juntos) de 0,28 u (revisión de T17).
        /// </summary>
        public const float PiranaCaveWidthU = 2.4f;

        /// <summary>Alto del sprite de la cueva en u.</summary>
        public const float PiranaCaveHeightU = 1.15f;

        /// <summary>Base del arco sobre el canto inferior del sprite de la cueva (u).</summary>
        public const float PiranaCaveBaseU = 0.1f;

        /// <summary>Ancho de la cápsula del bocado (u; ≈ 18 dp con la cuia de 48 dp).</summary>
        public const float PiranaMorselCapsuleWidthU = 0.8f;

        /// <summary>Alto de la cápsula del bocado (u; ≈ 12 dp).</summary>
        public const float PiranaMorselCapsuleHeightU = 0.55f;

        /// <summary>Contorno del bocado (u; 1,5 dp con u = 24).</summary>
        public const float PiranaMorselRimU = 0.0625f;

        /// <summary>Ancho de los sprites del bocado (u): la cápsula y su contorno.</summary>
        public const float PiranaMorselWidthU = PiranaMorselCapsuleWidthU + (2f * PiranaMorselRimU);

        /// <summary>Alto de los sprites del bocado (u): la cápsula y su contorno.</summary>
        public const float PiranaMorselHeightU = PiranaMorselCapsuleHeightU + (2f * PiranaMorselRimU);

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
        private Sprite? _piranaMorselRim;

        /// <summary>Boca de la cueva: arco de piedras con musgo y hueco en sombra (2,4 × 1,15 u; colores horneados).</summary>
        internal Sprite PiranaCaveSprite => _piranaCave != null ? _piranaCave : (_piranaCave = BuildPiranaCave());

        /// <summary>Piraña del cardumen mirando a la izquierda (0,9 × 0,5625 u; colores horneados).</summary>
        internal Sprite PiranaFishSprite => _piranaFish != null ? _piranaFish : (_piranaFish = BuildPiranaFish(false));

        /// <summary>La misma piraña dormida: el ojo cerrado (cueva llena, respuesta Q-C).</summary>
        internal Sprite PiranaFishAsleepSprite => _piranaFishAsleep != null ? _piranaFishAsleep : (_piranaFishAsleep = BuildPiranaFish(true));

        /// <summary>
        /// Bocado: cápsula de <see cref="PiranaMorselCapsuleWidthU"/> × <see cref="PiranaMorselCapsuleHeightU"/> u, clara con
        /// volumen (canto de abajo en sombra), dentro de un sprite de <see cref="PiranaMorselWidthU"/> ×
        /// <see cref="PiranaMorselHeightU"/> u que deja sitio al contorno; lo tiñe <c>Image.color</c> con la fruta.
        /// </summary>
        internal Sprite PiranaMorselSprite => _piranaMorsel != null ? _piranaMorsel : (_piranaMorsel = BuildPiranaMorsel(false));

        /// <summary>
        /// Contorno del bocado: la misma cápsula agrandada <see cref="PiranaMorselRimU"/> por todos lados, blanca, del mismo
        /// tamaño de sprite (se dibuja detrás, con <c>vidrio_borde</c>).
        /// </summary>
        internal Sprite PiranaMorselRimSprite => _piranaMorselRim != null ? _piranaMorselRim : (_piranaMorselRim = BuildPiranaMorsel(true));

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
            // del arco y un casquete de musgo irregular sobre las tres de arriba. Lajas algo largas y delgadas: el arco es
            // ancho y el hueco queda libre para la fila de puntos.
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
                float along = size * (1.3f + (0.2f * ArtNoise.Hash(i, 7, 53)));
                float across = size * (0.74f + (0.12f * ArtNoise.Hash(i, 11, 53)));
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
            float outline = PiranaToyEdge * k; // el borde fino de juguete (D-058), antes 3 px de tinta
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
                Color c = pass == 0 ? PiranaToy.Fin.Outline : PiranaToy.Fin.Mid;
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

            // Cuerpo de juguete (D-058): lomo oscuro, costado plateado y vientre claro mezclados en los tonos, con el volumen de
            // una cúpula alargada, el barniz y el borde fino; la mandíbula asoma abajo.
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

                    float t = Mathf.Clamp01((ny + 1f) * 0.5f);
                    ToyTones tones = t < 0.38f
                        ? ToyTones.Lerp(PiranaToy.Belly, PiranaToy.Silver, Mathf.SmoothStep(0f, 1f, t / 0.38f))
                        : (t < 0.66f ? PiranaToy.Silver : ToyTones.Lerp(PiranaToy.Silver, PiranaToy.Back, Mathf.SmoothStep(0f, 1f, (t - 0.66f) / 0.34f)));
                    if (nx > 0.35f)
                    {
                        tones = ToyTones.Lerp(tones, PiranaToy.Back, 0.22f * Mathf.Clamp01((nx - 0.35f) / 0.65f)); // hacia la cola, más oscuro
                    }

                    pc.Blend(x, y, PiranaToy.Back.Outline, PixelCanvas.Coverage(d - outline));
                    float inside = PixelCanvas.Coverage(d);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    Vector3 n = body < jaw ? ToyShading.Normal(nx / 1.06f, ny / 1.06f) : ToyShading.Normal(jdx / (jawR * 1.2f), jdy / (jawR * 1.2f));
                    float ao = 0.6f * ToyShading.CreaseAO(body, jaw, 3f * k);
                    pc.Blend(x, y, ToyShading.Shade(tones, n, ao, PiranaGloss), inside);
                }
            }

            // Aleta pectoral, motas y branquia (el brillo del costado ya lo pone el barniz).
            ToyEllipseOn(pc, cx - (rx * 0.12f), cy - (ry * 0.22f), 6f * k, 3.4f * k, PiranaToy.Fin, 0.5f, 0.8f * k, 0f, cx - (rx * 0.12f), cy - (ry * 0.1f), 12f * k, 8f * k, 0f);
            for (int i = 0; i < 6; i++)
            {
                float sx = cx + ((ArtNoise.Hash(i, 3, 71) - 0.25f) * rx * 1.1f);
                float sy = cy + ((0.15f + (0.45f * ArtNoise.Hash(i, 5, 71))) * ry);
                pc.FillCircle(sx, sy, 1.3f * k, ColorMath.WithAlpha(PiranaFishBack, 0.55f));
            }

            pc.StrokeArc(cx - (rx * 0.4f), cy + (ry * 0.02f), ry * 0.62f, 1.4f * k, -62f, 62f, ColorMath.WithAlpha(PiranaToy.Silver.Shadow, 0.8f));

            // Boca cerrada, sin dientes (b.2 M7), y ojo grande de juguete (iris en degradado y dos reflejos); dormida, el
            // párpado cerrado.
            pc.FillCapsule(cx - rx - (1.5f * k), cy - (ry * 0.3f), cx - (rx * 0.66f), cy - (ry * 0.4f), 0.8f * k, PiranaToy.Back.Outline);
            float ex = cx - (rx * 0.56f);
            float ey = cy + (ry * 0.24f);
            if (asleep)
            {
                pc.StrokeArc(ex, ey + (1.2f * k), 3.6f * k, 1.5f * k, 200f, 340f, PiranaToy.Back.Outline);
            }
            else
            {
                pc.FillCircle(ex, ey, 5f * k, ColorMath.WithAlpha(Palette.Cream, 0.95f));
                DrawToyEye(pc, ex - (0.4f * k), ey, 3.7f * k, PiranaEyeIris);
            }

            return MakeSprite(pc, asleep ? "mech_pirana_fish_asleep" : "mech_pirana_fish", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Piraña de juguete (D-058): los tonos, una vez (en su propia clase: se inicializan al pintarla).
        private static class PiranaToy
        {
            public static readonly ToyTones Back = ToyShading.Tones(PiranaFishBack);
            public static readonly ToyTones Silver = ToyShading.Tones(PiranaFishSilver, 0.8f);
            public static readonly ToyTones Belly = ToyShading.Tones(PiranaFishBelly, 0.6f);
            public static readonly ToyTones Fin = ToyShading.Tones(PiranaFishFin, 0.7f);
        }

        // Escamas lacadas (el pez es liso y mojado) y el borde fino (px de 96 por u).
        private const float PiranaGloss = 0.8f;
        private const float PiranaToyEdge = 1.8f;
        private static readonly Color PiranaEyeIris = Palette.Hex(0x3B3A2A);

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

        // Bocado (rim = false): la cápsula, casi blanca para que el tinte se vea tal cual, con el canto de abajo y los extremos
        // algo en sombra (volumen, como la dosis). Contorno (rim = true): la misma cápsula agrandada PiranaMorselRimU, blanca.
        // Los dos sprites miden lo mismo, así que las dos imágenes se colocan igual.
        private Sprite BuildPiranaMorsel(bool rim)
        {
            int w = Mathf.RoundToInt(PiranaMorselWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(PiranaMorselHeightU * MechanicPxPerU);
            float r = 0.5f * PiranaMorselCapsuleHeightU * MechanicPxPerU;
            float half = (0.5f * PiranaMorselCapsuleWidthU * MechanicPxPerU) - r; // medio tramo recto entre los dos extremos
            float rimPx = PiranaMorselRimU * MechanicPxPerU;
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = Mathf.Max(Mathf.Abs(px - cx) - half, 0f);
                    float dy = py - cy;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy)) - r; // distancia con signo a la cápsula (negativa dentro)
                    if (rim)
                    {
                        float cover = PixelCanvas.Coverage(d - rimPx);
                        if (cover > 0f)
                        {
                            pc.Blend(x, y, Color.white, cover);
                        }

                        continue;
                    }

                    float inside = PixelCanvas.Coverage(d);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    // De abajo (0,80) a arriba (1) y un poco más oscuro junto al canto: el tinte se lee entero en el centro.
                    float t = Mathf.Clamp01((dy + r) / (2f * r));
                    float g = Mathf.Lerp(0.8f, 1f, Mathf.SmoothStep(0f, 1f, t));
                    float edge = Mathf.Clamp01(1f + (d / (0.22f * r)));
                    g *= Mathf.Lerp(1f, 0.9f, edge * edge);
                    pc.Blend(x, y, new Color(g, g, g, 1f), inside);
                }
            }

            return MakeSprite(pc, rim ? "mech_pirana_morsel_rim" : "mech_pirana_morsel", new Vector2(0.5f, 0.5f), Vector4.zero);
        }
    }
}
