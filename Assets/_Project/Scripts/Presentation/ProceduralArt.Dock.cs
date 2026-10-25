#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Piezas por código del inicio «el muelle del río» (D-050; 06 §4.7, fila Lobby; dirección «Selva brillante premium», D-003:
    /// madera, vidrio y luz de juego, sin papel ni acuarela). Son el arte final del inicio (D-050: el dueño hace de artista y todo
    /// va por código), no provisionales de P-M5: salen también en release.
    /// <list type="bullet">
    /// <item><see cref="HomeBranch"/>: la rama de la que cuelga TiTino, que entra desde la vegetación de arriba a la izquierda,
    /// con musgo, hojas genéricas (sin especie reconocible: nada que revisar con el asesor) y un bejuco.</item>
    /// <item>El río vivo: <see cref="RiverRipple"/> (onda), <see cref="RiverFish"/> (un pez genérico que salta),
    /// <see cref="RiverBird"/> (un ave lejana) y <see cref="VerticalFade"/> (degradado blanco para la profundidad y el
    /// reflejo del cielo, que se tiñe).</item>
    /// <item><see cref="StopPost"/>: el poste de madera de la próxima parada, clavado en el agua.</item>
    /// <item><see cref="GaugeBoard"/>: la regla de crecida (poste de nivel) de la Crecida, en 9 cortes.</item>
    /// <item><see cref="DockJar"/>: el Cántaro como tinaja de barro, con su cuerda y la semilla pintada.</item>
    /// <item><see cref="DockCrate"/>: la caja de cuias de Capi con su cuerda, para el muelle antes del nivel 10.</item>
    /// <item>Los iconos de las tarjetas del día: <see cref="CardIconDaily"/>, <see cref="CardIconLogbook"/> y
    /// <see cref="CardIconLeague"/>.</item>
    /// </list>
    /// Perezosas: nada se crea al construir <see cref="ProceduralArt"/>, solo la primera vez que se piden; tras
    /// <see cref="Dispose"/> se vuelven a generar (el == de Unity).
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho de la rama de TiTino (dp), desde el borde izquierdo de la pantalla.</summary>
        public const float HomeBranchWidthDp = 210f;

        /// <summary>Alto de la caja de la rama de TiTino (dp).</summary>
        public const float HomeBranchHeightDp = 70f;

        /// <summary>Línea media de la rama (dp desde arriba de su caja) en x = 0.</summary>
        public const float HomeBranchLineStartDp = 22f;

        /// <summary>Lo que baja la línea media de la rama hasta su punta (dp).</summary>
        public const float HomeBranchDropDp = 10f;

        /// <summary>Radio de la rama junto al borde de la pantalla (dp).</summary>
        public const float HomeBranchRadiusDp = 7.5f;

        /// <summary>Ancho de la onda del río (dp).</summary>
        public const float RiverRippleWidthDp = 36f;

        /// <summary>Alto de la onda del río (dp).</summary>
        public const float RiverRippleHeightDp = 9f;

        /// <summary>Ancho del pez que salta (dp).</summary>
        public const float RiverFishWidthDp = 26f;

        /// <summary>Alto del pez que salta (dp).</summary>
        public const float RiverFishHeightDp = 12f;

        /// <summary>Ancho del ave lejana (dp).</summary>
        public const float RiverBirdWidthDp = 20f;

        /// <summary>Alto del ave lejana (dp).</summary>
        public const float RiverBirdHeightDp = 9f;

        /// <summary>Ancho del poste de la parada (dp).</summary>
        public const float StopPostWidthDp = 16f;

        /// <summary>Alto del poste de la parada (dp), con el pie mojado.</summary>
        public const float StopPostHeightDp = 46f;

        /// <summary>Ancho de la regla de crecida (dp).</summary>
        public const float GaugeWidthDp = 30f;

        /// <summary>Borde de 9 cortes de la regla de crecida (dp): el remate de arriba y el marco.</summary>
        public const float GaugeBorderDp = 10f;

        /// <summary>Ancho del Cántaro de barro (dp).</summary>
        public const float DockJarWidthDp = 52f;

        /// <summary>Alto del Cántaro de barro (dp).</summary>
        public const float DockJarHeightDp = 58f;

        /// <summary>Ancho de la caja de cuias (dp).</summary>
        public const float DockCrateWidthDp = 132f;

        /// <summary>Alto de la caja de cuias (dp).</summary>
        public const float DockCrateHeightDp = 74f;

        /// <summary>Lado de los iconos de las tarjetas del día (dp).</summary>
        public const float CardIconDp = 34f;

        private Sprite? _homeBranch;
        private Sprite? _riverRipple;
        private Sprite? _riverFish;
        private Sprite? _riverBird;
        private Sprite? _verticalFade;
        private Sprite? _stopPost;
        private Sprite? _gaugeBoard;
        private Sprite? _dockJar;
        private Sprite? _dockCrate;
        private Sprite? _cardDaily;
        private Sprite? _cardLogbook;
        private Sprite? _cardLeague;

        /// <summary>La rama de TiTino: <see cref="HomeBranchWidthDp"/> × <see cref="HomeBranchHeightDp"/> dp, pivote arriba a la izquierda.</summary>
        public Sprite HomeBranch => _homeBranch != null ? _homeBranch : (_homeBranch = BuildHomeBranch());

        /// <summary>Onda clara del río (blanca; se tiñe), pivote al centro.</summary>
        public Sprite RiverRipple => _riverRipple != null ? _riverRipple : (_riverRipple = BuildRiverRipple());

        /// <summary>Pez genérico de lado, mirando a la derecha, pivote al centro.</summary>
        public Sprite RiverFish => _riverFish != null ? _riverFish : (_riverFish = BuildRiverFish());

        /// <summary>Ave lejana con las alas abiertas, pivote al centro.</summary>
        public Sprite RiverBird => _riverBird != null ? _riverBird : (_riverBird = BuildRiverBird());

        /// <summary>Degradado vertical blanco, opaco arriba y transparente abajo (se tiñe y se voltea).</summary>
        public Sprite VerticalFade => _verticalFade != null ? _verticalFade : (_verticalFade = BuildVerticalFade());

        /// <summary>Poste de madera de la parada, pivote al pie.</summary>
        public Sprite StopPost => _stopPost != null ? _stopPost : (_stopPost = BuildStopPost());

        /// <summary>Regla de crecida en 9 cortes (<see cref="GaugeBorderDp"/>), pivote al centro.</summary>
        public Sprite GaugeBoard => _gaugeBoard != null ? _gaugeBoard : (_gaugeBoard = BuildGaugeBoard());

        /// <summary>El Cántaro de barro, pivote al pie.</summary>
        public Sprite DockJar => _dockJar != null ? _dockJar : (_dockJar = BuildDockJar());

        /// <summary>Caja de cuias con su cuerda, pivote al pie.</summary>
        public Sprite DockCrate => _dockCrate != null ? _dockCrate : (_dockCrate = BuildDockCrate());

        /// <summary>Icono de la tarjeta del Reto del río: el disco de agua clara (las olas de la banda las pone la tarjeta).</summary>
        public Sprite CardIconDaily => _cardDaily != null ? _cardDaily : (_cardDaily = BuildCardIcon(0));

        /// <summary>Icono de la tarjeta de la Bitácora: la libreta abierta sobre el disco ámbar.</summary>
        public Sprite CardIconLogbook => _cardLogbook != null ? _cardLogbook : (_cardLogbook = BuildCardIcon(1));

        /// <summary>Icono de la tarjeta de la Liga: el podio sobre el disco índigo.</summary>
        public Sprite CardIconLeague => _cardLeague != null ? _cardLeague : (_cardLeague = BuildCardIcon(2));

        /// <summary>Línea media de la rama de TiTino a <paramref name="xDp"/> dp del borde (dp desde arriba de su caja).</summary>
        public static float HomeBranchLineAt(float xDp)
        {
            float t = Mathf.Clamp01(xDp / HomeBranchWidthDp);
            return HomeBranchLineStartDp + (HomeBranchDropDp * t * t) - (3f * Mathf.Sin(t * Mathf.PI));
        }

        /// <summary>Radio de la rama de TiTino a <paramref name="xDp"/> dp del borde (dp).</summary>
        public static float HomeBranchRadiusAt(float xDp)
        {
            float t = Mathf.Clamp01(xDp / HomeBranchWidthDp);
            return Mathf.Lerp(HomeBranchRadiusDp, 2.6f, t);
        }

        // La rama: corteza de madera cálida con veta, luz arriba, sombra abajo, musgo en la parte de arriba, un bejuco que cuelga,
        // hojas genéricas a lo largo y un ramillete en la punta. Coordenadas en px con el origen abajo a la izquierda.
        private Sprite BuildHomeBranch()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(HomeBranchWidthDp * k);
            int h = Mathf.RoundToInt(HomeBranchHeightDp * k);
            var pc = new PixelCanvas(w, h);
            Color barkDark = Palette.Hex(0x4A3020);
            Color bark = Palette.Hex(0x7A5234);
            Color barkLight = Palette.Hex(0xB98A5C);
            Color moss = Palette.Hex(0x5E9A3A);
            Color mossLight = Palette.Hex(0x9BCB55);

            // Hojas de detrás (más oscuras), antes que la rama.
            DrawBranchLeaf(pc, 46f, 0.9f, 15f, 6.5f, -0.25f, Palette.LeafDark, Palette.LeafMid, k, 811);
            DrawBranchLeaf(pc, 128f, 1.15f, 13f, 5.5f, 0.2f, Palette.LeafDark, Palette.LeafMid, k, 812);
            DrawBranchLeaf(pc, 186f, 0.7f, 12f, 5f, -0.15f, Palette.LeafDark, Palette.LeafMid, k, 813);

            // Sombra suave bajo la rama.
            for (float x = 0f; x <= HomeBranchWidthDp - 4f; x += 0.5f)
            {
                float r = HomeBranchRadiusAt(x);
                float yc = HomeBranchHeightDp - HomeBranchLineAt(x) - 2.2f;
                pc.FillCircle(x * k, yc * k, r * k, new Color(0f, 0f, 0f, 0.05f));
            }

            // La rama por columnas: un tubo con luz arriba.
            for (int px = 0; px < w; px++)
            {
                float xd = (px + 0.5f) / k;
                if (xd > HomeBranchWidthDp - 6f)
                {
                    break;
                }

                float r = HomeBranchRadiusAt(xd);
                float yc = HomeBranchHeightDp - HomeBranchLineAt(xd);
                int y0 = Mathf.FloorToInt((yc - r - 1f) * k);
                int y1 = Mathf.CeilToInt((yc + r + 1f) * k);
                for (int py = Mathf.Max(0, y0); py <= Mathf.Min(h - 1, y1); py++)
                {
                    float yd = (py + 0.5f) / k;
                    float v = (yd - yc) / r; // -1 abajo, 1 arriba
                    float cov = PixelCanvas.Coverage((Mathf.Abs(yd - yc) - r) * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float grain = ArtNoise.Value2(xd / 7f, yd * 0.9f, 431);
                    float stripe = Mathf.Abs(Mathf.Sin((xd * 0.55f) + (v * 2.2f) + (grain * 3f)));
                    Color c = Color.Lerp(barkDark, bark, Mathf.SmoothStep(-1f, 0.2f, v));
                    c = Color.Lerp(c, barkLight, Mathf.Clamp01((v - 0.35f) * 1.6f));
                    if (stripe < 0.12f)
                    {
                        c = ColorMath.Darken(c, 0.16f);
                    }

                    // Musgo encima, a manchas.
                    float mossNoise = ArtNoise.Value2(xd / 11f, 3.1f, 977);
                    if (v > 0.45f && mossNoise > 0.48f && xd < HomeBranchWidthDp - 30f)
                    {
                        c = Color.Lerp(moss, mossLight, Mathf.Clamp01((v - 0.45f) * 2f));
                    }

                    c.a = 1f;
                    pc.Blend(px, py, c, cov);
                }
            }

            // Ramita que sube hacia la punta y un nudo.
            float tx = 150f;
            float ty = HomeBranchHeightDp - HomeBranchLineAt(tx);
            pc.FillCapsule(tx * k, ty * k, (tx + 22f) * k, (ty + 12f) * k, 1.6f * k, bark);
            pc.FillCapsule(tx * k, (ty + 0.6f) * k, (tx + 22f) * k, (ty + 12.6f) * k, 0.6f * k, barkLight);
            float kx = 92f;
            float ky = HomeBranchHeightDp - HomeBranchLineAt(kx);
            pc.FillCircle(kx * k, (ky - 1f) * k, 2.4f * k, barkDark);
            pc.FillCircle((kx - 0.4f) * k, (ky - 0.6f) * k, 1.4f * k, bark);

            // Bejuco que cuelga a la izquierda, con hojitas.
            float vx0 = 20f;
            float vy0 = HomeBranchHeightDp - HomeBranchLineAt(vx0) - 4f;
            for (float t = 0f; t < 1f; t += 0.01f)
            {
                float x = vx0 + (4f * Mathf.Sin(t * 3.2f));
                float y = vy0 - (t * 34f);
                pc.FillCircle(x * k, y * k, 0.9f * k, Palette.Hex(0x3C6E2A));
            }

            for (int i = 1; i <= 3; i++)
            {
                float t = i / 3.6f;
                float x = vx0 + (4f * Mathf.Sin(t * 3.2f));
                float y = vy0 - (t * 34f);
                var dir = new Vector2(i % 2 == 0 ? 0.8f : -0.8f, -0.6f).normalized;
                DrawLeaf(pc, new Vector2(x * k, y * k), dir, 7f * k, 3.4f * k, 0.1f, LeafShape.Ovate, Palette.LeafMid, Palette.LeafLight, k, 820 + i);
            }

            // Hojas de delante y el ramillete de la punta.
            DrawBranchLeaf(pc, 72f, -0.55f, 14f, 6f, 0.25f, Palette.LeafMid, Palette.LeafLight, k, 831);
            DrawBranchLeaf(pc, 108f, 2.6f, 12f, 5.2f, -0.2f, Palette.LeafMid, Palette.LeafLight, k, 832);
            DrawBranchLeaf(pc, 166f, -0.9f, 13f, 5.5f, 0.15f, Palette.LeafMid, Palette.LeafLight, k, 833);
            float ex = tx + 22f;
            float ey = ty + 12f;
            DrawLeaf(pc, new Vector2(ex * k, ey * k), new Vector2(0.9f, 0.45f).normalized, 15f * k, 6.5f * k, 0.15f, LeafShape.Ovate, Palette.LeafMid, Palette.LeafLight, k, 841);
            DrawLeaf(pc, new Vector2(ex * k, ey * k), new Vector2(0.35f, 0.94f).normalized, 13f * k, 5.8f * k, -0.2f, LeafShape.Ovate, Palette.LeafMid, Palette.LeafLight, k, 842);
            DrawLeaf(pc, new Vector2(ex * k, ey * k), new Vector2(0.98f, -0.2f).normalized, 12f * k, 5.4f * k, 0.1f, LeafShape.Ovate, Palette.LeafDark, Palette.LeafMid, k, 843);
            float endX = HomeBranchWidthDp - 8f;
            float endY = HomeBranchHeightDp - HomeBranchLineAt(endX);
            DrawLeaf(pc, new Vector2(endX * k, endY * k), new Vector2(0.95f, -0.3f).normalized, 11f * k, 5f * k, 0.12f, LeafShape.Ovate, Palette.LeafMid, Palette.LeafLight, k, 851);
            DrawLeaf(pc, new Vector2(endX * k, endY * k), new Vector2(0.7f, -0.7f).normalized, 10f * k, 4.6f * k, -0.1f, LeafShape.Ovate, Palette.LeafDark, Palette.LeafMid, k, 852);

            return MakeSprite(pc, "home_branch", new Vector2(0f, 1f), Vector4.zero, k);
        }

        // Una hoja que nace de la rama a x dp, con el ángulo angle (radianes; 0 = hacia la derecha, positivo hacia arriba).
        private static void DrawBranchLeaf(PixelCanvas pc, float x, float angle, float length, float width, float bend, Color dark, Color light, float k, int seed)
        {
            float y = HomeBranchHeightDp - HomeBranchLineAt(x);
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            DrawLeaf(pc, new Vector2(x * k, y * k), dir, length * k, width * k, bend, LeafShape.Ovate, dark, light, k, seed);
        }

        // Onda: el arco de arriba de una elipse fina, más gruesa en el centro y que se apaga en las puntas.
        private Sprite BuildRiverRipple()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(RiverRippleWidthDp * k);
            int h = Mathf.RoundToInt(RiverRippleHeightDp * k);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float rx = (RiverRippleWidthDp * 0.5f - 1.5f) * k;
            float ry = (RiverRippleHeightDp - 2.5f) * k;
            float cy = 1.2f * k;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = ((x + 0.5f) - cx) / rx;
                    float dy = ((y + 0.5f) - cy) / ry;
                    if (dy < 0f)
                    {
                        continue;
                    }

                    float q = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float thick = (0.6f + (0.9f * Mathf.Clamp01(dy))) * k;
                    float d = Mathf.Abs(q - 1f) * Mathf.Min(rx, ry) - (thick * 0.5f);
                    float cov = PixelCanvas.Coverage(d);
                    float ends = Mathf.Clamp01(1.4f - Mathf.Abs(dx) * 1.3f);
                    if (cov > 0f)
                    {
                        pc.Blend(x, y, new Color(1f, 1f, 1f, ends), cov);
                    }
                }
            }

            return MakeSprite(pc, "river_ripple", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        /// <summary>Ancho del nenúfar del río (dp), visto en perspectiva.</summary>
        public const float RiverLilyWidthDp = 40f;

        /// <summary>Alto del nenúfar del río (dp), con la flor.</summary>
        public const float RiverLilyHeightDp = 22f;

        private Sprite? _riverLily;
        private Sprite? _riverLilyFlower;

        /// <summary>Nenúfar genérico en perspectiva, con su muesca y sus nervios; pivote al centro.</summary>
        public Sprite RiverLily => _riverLily != null ? _riverLily : (_riverLily = BuildRiverLily(false));

        /// <summary>El nenúfar con una flor rosa abierta encima; pivote al centro.</summary>
        public Sprite RiverLilyFlower => _riverLilyFlower != null ? _riverLilyFlower : (_riverLilyFlower = BuildRiverLily(true));

        // La hoja: una elipse aplastada (perspectiva) con la muesca hacia delante a la derecha, nervios que salen del centro, el canto
        // claro y una sombra en el agua; con flower, una flor de pétalos rosas y centro amarillo encima, sin especie reconocible.
        private Sprite BuildRiverLily(bool flower)
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(RiverLilyWidthDp * k);
            int h = Mathf.RoundToInt(RiverLilyHeightDp * k);
            var pc = new PixelCanvas(w, h);
            float cx = RiverLilyWidthDp * 0.5f;
            const float cy = 7.5f;
            const float rx = 18f;
            const float ry = 6.6f;
            pc.FillEllipseSoft(cx * k, (cy - 1.2f) * k, (rx + 1f) * k, (ry + 0.8f) * k, 1.5f * k, new Color(0f, 0f, 0f, 0.22f));
            Color dark = Palette.LeafDark;
            Color mid = Palette.LeafMid;
            Color light = Palette.LeafLight;
            for (int y = 0; y < h; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float u = (xd - cx) / rx;
                    float v = (yd - cy) / ry;
                    float q = Mathf.Sqrt((u * u) + (v * v));
                    float d = (q - 1f) * ry;
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    // La muesca: una cuña hacia delante a la derecha.
                    float ang = Mathf.Atan2(-v, u) * Mathf.Rad2Deg;
                    if (ang > 18f && ang < 38f && q > 0.08f)
                    {
                        continue;
                    }

                    Color c = Color.Lerp(mid, light, Mathf.Clamp01(0.55f + (v * 0.5f) - (u * 0.2f)));
                    float vein = Mathf.Abs(Mathf.Sin(Mathf.Atan2(v, u) * 7f));
                    if (vein < 0.08f && q > 0.15f && q < 0.92f)
                    {
                        c = Color.Lerp(c, dark, 0.35f);
                    }

                    if (q > 0.86f)
                    {
                        c = Color.Lerp(c, Palette.Hex(0xB5D86A), 0.55f); // el canto que sube
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            if (flower)
            {
                Color petal = Palette.Hex(0xF2A0BE);
                Color petalDeep = Palette.Hex(0xD9658F);
                float fx = cx - 4f;
                float fy = cy + 5f;
                for (int i = 0; i < 8; i++)
                {
                    float a = (i / 8f) * Mathf.PI * 2f;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.55f + 0.35f);
                    Vector2 tip = new Vector2(fx, fy) + (dir * 5.6f);
                    pc.FillCapsule(fx * k, fy * k, tip.x * k, tip.y * k, 2.1f * k, i % 2 == 0 ? petalDeep : petal);
                }

                for (int i = 0; i < 5; i++)
                {
                    float a = ((i + 0.5f) / 5f) * Mathf.PI;
                    var dir = new Vector2(Mathf.Cos(a) * 0.8f, Mathf.Sin(a));
                    Vector2 tip = new Vector2(fx, fy) + (dir * 5f);
                    pc.FillCapsule(fx * k, (fy + 0.6f) * k, tip.x * k, tip.y * k, 1.8f * k, Palette.Hex(0xFBD3E1));
                }

                pc.FillCircle(fx * k, (fy + 1.4f) * k, 1.7f * k, Palette.Hex(0xF4C842));
            }

            return MakeSprite(pc, flower ? "river_lily_flower" : "river_lily", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        /// <summary>Ancho de la pieza de los destellos del río (dp): se estira al ancho del lienzo.</summary>
        public const float RiverStreaksWidthDp = 360f;

        /// <summary>Alto de la pieza de los destellos del río (dp): se estira al alto del río.</summary>
        public const float RiverStreaksHeightDp = 200f;

        private Sprite? _riverStreaks;

        /// <summary>
        /// Los destellos del río: trazos horizontales blancos, finos y cortos junto al horizonte y largos y gruesos cerca del muelle
        /// (perspectiva), con las puntas suaves, como el río del panorama; se tiñen con la luz del cielo.
        /// </summary>
        public Sprite RiverStreaks => _riverStreaks != null ? _riverStreaks : (_riverStreaks = BuildRiverStreaks());

        // Trazos deterministas (ArtNoise): más juntos arriba (lejos). Cada trazo, tres cápsulas superpuestas de largo creciente y
        // alfa bajo, para que las puntas se apaguen.
        private Sprite BuildRiverStreaks()
        {
            const float k = 2f;
            int w = Mathf.RoundToInt(RiverStreaksWidthDp * k);
            int h = Mathf.RoundToInt(RiverStreaksHeightDp * k);
            var pc = new PixelCanvas(w, h);
            const int count = 86;
            for (int i = 0; i < count; i++)
            {
                float hr = ArtNoise.Hash(i, 3, 515);
                float hx = ArtNoise.Hash(i, 7, 516);
                float hl = ArtNoise.Hash(i, 11, 517);
                float t = Mathf.Clamp01((i + hr) / count);
                float yDown = RiverStreaksHeightDp * Mathf.Pow(t, 1.7f);
                float y = RiverStreaksHeightDp - yDown - 1f;
                float len = Mathf.Lerp(6f, 54f, t) * (0.45f + hl);
                float thick = Mathf.Lerp(0.45f, 1.3f, t);
                float x = hx * (RiverStreaksWidthDp + len) - (len * 0.5f);
                float alpha = Mathf.Lerp(0.75f, 0.42f, t);
                for (int layer = 0; layer < 3; layer++)
                {
                    float l = len * (0.45f + (layer * 0.275f));
                    float a = alpha * (layer == 0 ? 0.6f : 0.25f);
                    float cx = x + (len * 0.5f);
                    pc.FillCapsule((cx - (l * 0.5f)) * k, y * k, (cx + (l * 0.5f)) * k, y * k, thick * (layer == 0 ? 1f : 0.8f) * k, new Color(1f, 1f, 1f, a));
                }
            }

            return MakeSprite(pc, "river_streaks", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        // Pez genérico: cuerpo ahusado plateado con el lomo oliva, aleta dorsal, cola en horquilla, ojo y brillo.
        private Sprite BuildRiverFish()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(RiverFishWidthDp * k);
            int h = Mathf.RoundToInt(RiverFishHeightDp * k);
            var pc = new PixelCanvas(w, h);
            Color back = Palette.Hex(0x5E7A5A);
            Color belly = Palette.Hex(0xE8EEE4);
            Color outline = Palette.Hex(0x2A3A2C);
            float cy = RiverFishHeightDp * 0.5f;

            // Cola (triángulos) y aleta.
            pc.FillTriangle(new Vector2(6f * k, cy * k), new Vector2(0.6f * k, (cy + 4.6f) * k), new Vector2(2.4f * k, cy * k), outline);
            pc.FillTriangle(new Vector2(6f * k, cy * k), new Vector2(0.6f * k, (cy - 4.6f) * k), new Vector2(2.4f * k, cy * k), outline);
            pc.FillTriangle(new Vector2(6.4f * k, cy * k), new Vector2(1.6f * k, (cy + 3.6f) * k), new Vector2(3f * k, cy * k), back);
            pc.FillTriangle(new Vector2(6.4f * k, cy * k), new Vector2(1.6f * k, (cy - 3.6f) * k), new Vector2(3f * k, cy * k), back);
            pc.FillTriangle(new Vector2(11f * k, (cy + 2.6f) * k), new Vector2(15.5f * k, (cy + 5.4f) * k), new Vector2(17f * k, (cy + 2.4f) * k), back);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float yd = (y + 0.5f) / k;
                    float u = (xd - 15f) / 10f;
                    float v = (yd - cy) / 3.6f;
                    float shape = (u * u) + (v * v * (u > 0f ? 1f + (u * 1.2f) : 1f));
                    float d = (Mathf.Sqrt(shape) - 1f) * 3.6f;
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, outline, cov);
                    float inside = PixelCanvas.Coverage((d + 0.7f) * k);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    Color c = Color.Lerp(belly, back, Mathf.SmoothStep(-0.2f, 0.7f, v));
                    if (v > 0.1f && v < 0.3f && u > -0.7f && u < 0.6f)
                    {
                        c = Color.Lerp(c, Color.white, 0.35f); // la línea brillante del costado
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }

            pc.FillCircle(21.4f * k, (cy + 0.8f) * k, 1.15f * k, outline);
            pc.FillCircle(21.7f * k, (cy + 1.1f) * k, 0.4f * k, Color.white);
            return MakeSprite(pc, "river_fish", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        // Ave lejana: dos arcos gruesos con las puntas hacia arriba, del verde oscuro de la selva.
        private Sprite BuildRiverBird()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(RiverBirdWidthDp * k);
            int h = Mathf.RoundToInt(RiverBirdHeightDp * k);
            var pc = new PixelCanvas(w, h);
            Color ink = Palette.Hex(0x22302A);
            float mid = RiverBirdWidthDp * 0.5f;
            for (float t = 0f; t <= 1f; t += 0.02f)
            {
                float xl = mid - (t * (mid - 1.5f));
                float yl = 2.6f + (Mathf.Sin(t * Mathf.PI * 0.9f) * 4.2f) + (t * 1.2f);
                float r = Mathf.Lerp(1.15f, 0.45f, t);
                pc.FillCircle(xl * k, yl * k, r * k, ink);
                pc.FillCircle((RiverBirdWidthDp - xl) * k, yl * k, r * k, ink);
            }

            pc.FillCircle(mid * k, 2.6f * k, 1.3f * k, ink);
            return MakeSprite(pc, "river_bird", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        // 4 × 64 px: blanco con alfa 1 arriba que baja a 0 abajo, en curva suave.
        private Sprite BuildVerticalFade()
        {
            const int w = 4;
            const int h = 64;
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                float t = (y + 0.5f) / h; // 0 abajo, 1 arriba
                float a = Mathf.SmoothStep(0f, 1f, t);
                for (int x = 0; x < w; x++)
                {
                    pc.Set(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            return MakeSprite(pc, "vertical_fade", new Vector2(0.5f, 0.5f), Vector4.zero, 1f);
        }

        // Poste de la parada: tronco redondo con la punta en tejado, dos amarres de cuerda y el pie oscuro y mojado.
        private Sprite BuildStopPost()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(StopPostWidthDp * k);
            int h = Mathf.RoundToInt(StopPostHeightDp * k);
            var pc = new PixelCanvas(w, h);
            float cx = StopPostWidthDp * 0.5f;
            const float r = 4.6f;
            Color dark = Palette.Hex(0x4A3020);
            Color baseC = Palette.Hex(0x8A5E3A);
            Color light = Palette.Hex(0xC79A68);
            for (int y = 0; y < h; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float top = StopPostHeightDp - 2f - Mathf.Abs(xd - cx) * 0.8f;
                    float d = Mathf.Max(Mathf.Abs(xd - cx) - r, yd - top);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float u = (xd - cx) / r; // -1 izquierda (luz) … 1
                    Color c = Color.Lerp(light, baseC, Mathf.SmoothStep(-0.9f, 0.1f, u));
                    c = Color.Lerp(c, dark, Mathf.Clamp01((u - 0.35f) * 1.4f));
                    float grain = Mathf.Abs(Mathf.Sin((yd * 0.8f) + (u * 3f) + (ArtNoise.Value2(xd, yd / 6f, 61) * 3f)));
                    if (grain < 0.1f)
                    {
                        c = ColorMath.Darken(c, 0.15f);
                    }

                    if (yd < 9f)
                    {
                        c = Color.Lerp(c, Palette.Hex(0x2E2218), 0.55f * (1f - (yd / 9f))); // el pie mojado
                    }

                    if (yd > top - 1.6f && u < 0.3f)
                    {
                        c = Color.Lerp(c, Palette.Hex(0xE3BE8A), 0.6f); // el canto del tejado
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            foreach (float band in new[] { 30f, 36f })
            {
                for (int i = 0; i < 3; i++)
                {
                    float y = band + (i * 1.3f);
                    pc.FillCapsule((cx - r - 0.4f) * k, y * k, (cx + r + 0.4f) * k, (y - 0.5f) * k, 0.7f * k, i % 2 == 0 ? Palette.FiberBase : Palette.FiberLight);
                }
            }

            return MakeSprite(pc, "stop_post", new Vector2(0.5f, 0f), Vector4.zero, k);
        }

        // Regla de crecida: tabla clara pintada con marco de madera, remate en punta arriba; 9 cortes de GaugeBorderDp. Las marcas y
        // el agua las pone el código encima (la altura cambia con la pantalla).
        private Sprite BuildGaugeBoard()
        {
            const float k = UiPxPerDp;
            const float wDp = GaugeWidthDp;
            const float hDp = 40f;
            const float border = GaugeBorderDp;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            Color frame = Palette.Hex(0x6E4A2E);
            Color frameLight = Palette.Hex(0xB0805A);
            Color board = Palette.Hex(0xF3E7CC);
            Color boardShade = Palette.Hex(0xDCCBA6);
            for (int y = 0; y < h; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float outer = PixelCanvas.RoundedRectDistance(xd, yd, 0.5f, 0.5f, wDp - 0.5f, hDp - 0.5f, 5f);
                    float cov = PixelCanvas.Coverage(outer * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float inner = PixelCanvas.RoundedRectDistance(xd, yd, 4f, 4f, wDp - 4f, hDp - 4f, 2.5f);
                    Color c;
                    if (inner > 0f)
                    {
                        c = Color.Lerp(frame, frameLight, Mathf.Clamp01((wDp * 0.5f - xd) / (wDp * 0.5f)) * 0.7f);
                        if (outer > -1.2f)
                        {
                            c = ColorMath.Darken(c, 0.25f);
                        }
                    }
                    else
                    {
                        c = Color.Lerp(board, boardShade, Mathf.Clamp01((xd - 4f) / (wDp - 8f)) * 0.6f);
                        if (inner > -1f)
                        {
                            c = Color.Lerp(c, Palette.Groove, 0.35f);
                        }
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            float s = border * k;
            return MakeSprite(pc, "gauge_board", new Vector2(0.5f, 0.5f), new Vector4(s, s, s, s), k);
        }

        // El Cántaro: tinaja de barro panzuda con cuello, boca con tapa de madera y ranura, cuerda en el cuello, la semilla pintada
        // en la panza y la luz arriba a la izquierda (07 §1.1). Pivote al pie, con la sombra de contacto incluida.
        private Sprite BuildDockJar()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(DockJarWidthDp * k);
            int h = Mathf.RoundToInt(DockJarHeightDp * k);
            var pc = new PixelCanvas(w, h);
            float cx = DockJarWidthDp * 0.5f;
            Color clay = Palette.Hex(0xC2754A);
            Color clayLight = Palette.Hex(0xE6A274);
            Color clayDark = Palette.Hex(0x7E3F22);
            Color outline = Palette.Hex(0x3E2416);
            pc.FillEllipseSoft(cx * k, 2.6f * k, 20f * k, 2.6f * k, 2f * k, new Color(0f, 0f, 0f, 0.35f));
            for (int y = 0; y < h; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float half = JarHalfWidth(yd);
                    if (half <= 0f)
                    {
                        continue;
                    }

                    float d = Mathf.Abs(xd - cx) - half;
                    d = Mathf.Max(d, 2f - yd);
                    d = Mathf.Max(d, yd - 50f);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, outline, cov);
                    float inside = PixelCanvas.Coverage((d + 1.3f) * k);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float u = (xd - cx) / Mathf.Max(1f, half);
                    float vy = (yd - 22f) / 22f;
                    Color c = Color.Lerp(clayLight, clay, Mathf.SmoothStep(-0.8f, 0.2f, u));
                    c = Color.Lerp(c, clayDark, Mathf.Clamp01((u - 0.3f) * 1.3f) + Mathf.Clamp01(-vy - 0.4f) * 0.5f);
                    float hu = (u + 0.45f) / 0.22f;
                    float hv = (yd - 30f) / 7f;
                    float hl = (hu * hu) + (hv * hv);
                    if (hl < 1f)
                    {
                        c = Color.Lerp(c, Color.white, 0.4f * (1f - hl));
                    }

                    // Una banda pintada en crema con puntos, bajo el cuello.
                    if (yd > 34f && yd < 36.6f)
                    {
                        c = Color.Lerp(c, Palette.Hex(0xF3DDB8), 0.85f);
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }

            // Cuerda del cuello.
            for (int i = 0; i < 4; i++)
            {
                float y = 41.5f + (i * 1.25f);
                pc.FillCapsule((cx - 9.6f) * k, y * k, (cx + 9.6f) * k, (y - 0.4f) * k, 0.75f * k, i % 2 == 0 ? Palette.FiberBase : Palette.FiberLight);
            }

            pc.FillCapsule((cx + 8.5f) * k, 42f * k, (cx + 11f) * k, 35f * k, 0.8f * k, Palette.FiberBase);

            // Tapa de madera con la ranura de las semillas.
            pc.FillRoundedRect((cx - 9.8f) * k, 49f * k, (cx + 9.8f) * k, 54.6f * k, 2.4f * k, outline);
            pc.FillRoundedRect((cx - 8.8f) * k, 49.9f * k, (cx + 8.8f) * k, 53.8f * k, 1.8f * k, Palette.CapWood);
            pc.FillRoundedRect((cx - 8.8f) * k, 52.4f * k, (cx + 8.8f) * k, 53.8f * k, 0.8f * k, ColorMath.Lighten(Palette.CapWood, 0.25f));
            pc.FillRoundedRect((cx - 4.5f) * k, 51.2f * k, (cx + 4.5f) * k, 52.4f * k, 0.6f * k, Palette.Groove);

            // La semilla pintada en la panza (la del contador, 07 §7.6).
            DrawSeedMark(pc, cx * k, 20f * k, 7.2f * k);
            return MakeSprite(pc, "dock_jar", new Vector2(0.5f, 0f), Vector4.zero, k);
        }

        // Medio ancho de la tinaja a la altura yd (dp desde el pie): pie estrecho, panza ancha, hombro y cuello.
        private static float JarHalfWidth(float yd)
        {
            if (yd < 2f || yd > 50f)
            {
                return 0f;
            }

            if (yd < 6f)
            {
                return Mathf.Lerp(11f, 14.5f, (yd - 2f) / 4f);
            }

            if (yd < 36f)
            {
                float t = (yd - 21f) / 15f;
                return 24f * Mathf.Sqrt(Mathf.Max(0f, 1f - (t * t * 0.86f))) - (yd < 21f ? (21f - yd) * 0.12f : 0f);
            }

            if (yd < 42f)
            {
                return Mathf.Lerp(12.5f, 9.4f, (yd - 36f) / 6f);
            }

            if (yd < 47f)
            {
                return 9.4f;
            }

            return Mathf.Lerp(9.4f, 11f, (yd - 47f) / 3f);
        }

        // Semilla ámbar en óvalo inclinado con su veta (como el icono de semillas), en px.
        private static void DrawSeedMark(PixelCanvas pc, float cx, float cy, float size)
        {
            float rx = size * 0.62f;
            float ry = size;
            float angle = -24f * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            int x0 = Mathf.FloorToInt(cx - size - 2f);
            int x1 = Mathf.CeilToInt(cx + size + 2f);
            int y0 = Mathf.FloorToInt(cy - size - 2f);
            int y1 = Mathf.CeilToInt(cy + size + 2f);
            Color light = ColorMath.Lighten(SeedAmber, 0.3f);
            Color dark = ColorMath.Darken(SeedAmber, 0.1f);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    float u = (dx * cos) + (dy * sin);
                    float v = (-dx * sin) + (dy * cos);
                    float q = Mathf.Sqrt(((u * u) / (rx * rx)) + ((v * v) / (ry * ry)));
                    float d = (q - 1f) * Mathf.Min(rx, ry);
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, Palette.Hex(0x5A3418), cov);
                    float inside = PixelCanvas.Coverage(d + (size * 0.14f));
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    Color c = Color.Lerp(dark, light, Mathf.Clamp01(0.5f - (0.4f * u / rx) + (0.4f * v / ry)));
                    if (Mathf.Abs(u) < size * 0.07f && Mathf.Abs(v) < ry * 0.6f)
                    {
                        c = ColorMath.Darken(SeedAmber, 0.4f);
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }
        }

        // Caja de cuias de Capi: listones de madera con clavos, tres cuias de vidrio con agua de colores asomando y un rollo de
        // cuerda apoyado a la derecha. Pivote al pie.
        private Sprite BuildDockCrate()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(DockCrateWidthDp * k);
            int h = Mathf.RoundToInt(DockCrateHeightDp * k);
            var pc = new PixelCanvas(w, h);
            pc.FillEllipseSoft(52f * k, 3f * k, 50f * k, 3.4f * k, 3f * k, new Color(0f, 0f, 0f, 0.3f));
            pc.FillEllipseSoft(108f * k, 3f * k, 20f * k, 3f * k, 2.5f * k, new Color(0f, 0f, 0f, 0.3f));

            // Las cuias que asoman, detrás de la tabla de delante.
            DrawCrateCuia(pc, 24f, 30f, 11f, 36f, Palette.Hex(0x7B2D6E), k);
            DrawCrateCuia(pc, 50f, 30f, 12f, 40f, Palette.Hex(0xE8A21E), k);
            DrawCrateCuia(pc, 77f, 30f, 11f, 34f, Palette.Hex(0x2E8B8B), k);

            // La caja: dos tablas con veta, canto claro arriba, clavos y las esquinas oscuras.
            Color plank = Palette.DockBase;
            for (int i = 0; i < 2; i++)
            {
                float y0 = 4f + (i * 14f);
                float y1 = y0 + 13f;
                pc.FillRoundedRect(4f * k, y0 * k, 100f * k, y1 * k, 1.6f * k, Palette.Groove);
                pc.FillRoundedRect(5f * k, (y0 + 0.9f) * k, 99f * k, (y1 - 0.9f) * k, 1.2f * k, i == 0 ? ColorMath.Darken(plank, 0.06f) : plank);
                pc.FillRoundedRect(5f * k, (y1 - 2.6f) * k, 99f * k, (y1 - 0.9f) * k, 0.8f * k, Palette.DockLight);
                for (float x = 12f; x < 96f; x += 21f)
                {
                    pc.FillCapsule(x * k, (y0 + 4f + ((x % 3f) * 0.6f)) * k, (x + 13f) * k, (y0 + 4.4f + ((x % 3f) * 0.6f)) * k, 0.4f * k, ColorMath.Darken(plank, 0.2f));
                }

                pc.FillCircle(9f * k, (y0 + 6.5f) * k, 1.1f * k, Palette.Groove);
                pc.FillCircle(95f * k, (y0 + 6.5f) * k, 1.1f * k, Palette.Groove);
            }

            pc.FillRoundedRect(4f * k, 4f * k, 10f * k, 31f * k, 1.4f * k, Palette.DockShadow);
            pc.FillRoundedRect(94f * k, 4f * k, 100f * k, 31f * k, 1.4f * k, Palette.DockShadow);

            // Rollo de cuerda.
            for (int ring = 0; ring < 4; ring++)
            {
                float rr = 13f - (ring * 3f);
                pc.StrokeArc(110f * k, 12f * k, rr * k, 2.6f * k, 0f, 360f, ring % 2 == 0 ? Palette.FiberBase : Palette.FiberShadow);
                pc.StrokeArc(110f * k, 12.6f * k, rr * k, 1f * k, 30f, 150f, Palette.FiberLight);
            }

            pc.FillCapsule(97f * k, 4f * k, 110f * k, 2.4f * k, 1.3f * k, Palette.FiberBase);
            return MakeSprite(pc, "dock_crate", new Vector2(0.5f, 0f), Vector4.zero, k);
        }

        // Una cuia de vidrio pequeña (cuello corto con su tapón de fibra) con agua de color hasta media altura, en dp.
        private static void DrawCrateCuia(PixelCanvas pc, float cx, float yBase, float halfW, float height, Color water, float k)
        {
            float top = yBase + height;
            float neckY = top - 8f;
            for (int y = Mathf.FloorToInt(yBase * k); y < Mathf.CeilToInt(top * k) && y < pc.Height; y++)
            {
                float yd = (y + 0.5f) / k;
                float half = yd > neckY ? halfW * 0.42f : halfW * (yd > neckY - 5f ? Mathf.Lerp(1f, 0.42f, (yd - (neckY - 5f)) / 5f) : 1f);
                for (int x = Mathf.FloorToInt((cx - halfW - 1f) * k); x < Mathf.CeilToInt((cx + halfW + 1f) * k); x++)
                {
                    float xd = (x + 0.5f) / k;
                    float d = Mathf.Abs(xd - cx) - half;
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, new Color(Palette.Groove.r, Palette.Groove.g, Palette.Groove.b, 0.8f), cov);
                    float inside = PixelCanvas.Coverage((d + 0.8f) * k);
                    Color c = yd < yBase + (height * 0.55f) ? water : new Color(Palette.GlassTint.r, Palette.GlassTint.g, Palette.GlassTint.b, 0.85f);
                    float u = (xd - cx) / Mathf.Max(0.5f, half);
                    if (u < -0.35f && u > -0.75f)
                    {
                        c = Color.Lerp(c, Color.white, 0.55f);
                    }

                    pc.Blend(x, y, c, inside);
                }
            }

            pc.FillRoundedRect((cx - (halfW * 0.5f)) * k, (top - 1f) * k, (cx + (halfW * 0.5f)) * k, (top + 2.4f) * k, 1f * k, Palette.FiberBase);
        }

        // Iconos de las tarjetas: un disco con canto oscuro y brillo arriba y, encima, el glifo crema.
        private Sprite BuildCardIcon(int kind)
        {
            const float k = UiPxPerDp;
            const float size = CardIconDp;
            int s = Mathf.RoundToInt(size * k);
            var pc = new PixelCanvas(s, s);
            float c = size * 0.5f;
            Color disc = kind == 0 ? Palette.WaterClear : (kind == 1 ? Palette.UiAmber : Palette.UiIndigo);
            Color rim = ColorMath.Darken(disc, 0.45f);
            pc.FillCircle(c * k, (c - 0.8f) * k, (c - 1f) * k, rim);
            pc.FillCircle(c * k, c * k, (c - 2.4f) * k, disc);
            pc.FillEllipseSoft(c * k, (c + 6f) * k, 9f * k, 5f * k, 3f * k, new Color(1f, 1f, 1f, 0.22f));
            Color glyph = Palette.Cream;
            switch (kind)
            {
                case 0:
                    break; // las olas (1-3, la banda de hoy) las pone la tarjeta encima (DailyChallengeCard)
                case 1:
                    // Libreta abierta: dos páginas con el lomo y unas líneas.
                    pc.FillTriangle(new Vector2(c * k, 9f * k), new Vector2(7.5f * k, 11.5f * k), new Vector2(7.5f * k, 25f * k), glyph);
                    pc.FillRoundedRect(7.5f * k, 11.5f * k, (c - 0.6f) * k, 25f * k, 1f * k, glyph);
                    pc.FillRoundedRect((c + 0.6f) * k, 11.5f * k, (size - 7.5f) * k, 25f * k, 1f * k, glyph);
                    pc.FillTriangle(new Vector2(c * k, 9f * k), new Vector2((size - 7.5f) * k, 11.5f * k), new Vector2((size - 7.5f) * k, 25f * k), glyph);
                    pc.FillCapsule(c * k, 9.5f * k, c * k, 24.5f * k, 0.6f * k, rim);
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 21.5f - (i * 3.2f);
                        pc.FillCapsule(10f * k, y * k, (c - 3f) * k, y * k, 0.5f * k, ColorMath.Darken(disc, 0.1f));
                        pc.FillCapsule((c + 3f) * k, y * k, (size - 10f) * k, y * k, 0.5f * k, ColorMath.Darken(disc, 0.1f));
                    }

                    break;
                default:
                    // Podio de tres escalones con una estrella encima del más alto.
                    pc.FillRoundedRect(7f * k, 9f * k, 14f * k, 17f * k, 0.8f * k, glyph);
                    pc.FillRoundedRect(13.5f * k, 9f * k, 20.5f * k, 22f * k, 0.8f * k, glyph);
                    pc.FillRoundedRect(20f * k, 9f * k, 27f * k, 14.5f * k, 0.8f * k, glyph);
                    pc.FillCircle(17f * k, 26f * k, 2.3f * k, Palette.Hex(0xF2C24B));
                    break;
            }

            return MakeSprite(pc, kind == 0 ? "card_icon_daily" : (kind == 1 ? "card_icon_logbook" : "card_icon_league"), new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }
    }
}
