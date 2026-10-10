#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Liana (02 §9.1 M3; propuesta de D-007 b.2 M3; ficha de Magic Sort §3.3; D-003;
    /// direccion-visual-v2 §4.8): la vuelta de liana del cuerpo (una hélice: el arco delantero de una elipse que comparte la
    /// proporción de la cara del líquido y baja de izquierda a derecha), su luz de borde y su tramo trasero, la vuelta del
    /// cuello que acaba en el nudo, y la ✓ verde del desbloqueo, que va como sprite porque el atlas TMP no tiene U+2713
    /// (arquitectura del paso 1 §12).
    /// </summary>
    /// <remarks>
    /// <para>La liana va en grises para que <c>Image.color</c> la tiña de verde hoja oscuro (atada) o de pajizo (seca, al
    /// soltarse): contorno <see cref="LianaOutlineShade"/>; hojitas con degradado del tallo a la punta, de
    /// <see cref="LianaLeafBaseShade"/> a <see cref="LianaLeafTipShade"/> (con <c>LianaOverlay.FreshTint</c>, de
    /// <see cref="Palette.LeafDark"/> a <see cref="Palette.LeafMid"/>, 4.8); tallo <see cref="LianaStemShade"/>
    /// (= <see cref="Palette.LeafMid"/>) con el canto de abajo en sombra y el de arriba más claro; y una sombra de contacto
    /// horneada en negro (alfa 0,25, desplazada (+1; −2) dp y con 2 dp de desenfoque: negro por cualquier tinte sigue negro, así
    /// que se conserva al secarse). La luz de borde de 1 dp va en su propio sprite blanco, que se tiñe de
    /// <see cref="Palette.LeafShine"/> (o de fibra clara al secarse): ese verde claro no sale de multiplicar el tinte. Todo,
    /// cortado en el contorno del vidrio.</para>
    /// <para>Perezosos: un nivel sin Liana no crea estas texturas, y tras <see cref="Dispose"/> se vuelven a generar. Sustituyen
    /// a <see cref="LianaWrap"/> y <see cref="LianaKnot"/> de <c>ProceduralArt.Mechanics.cs</c>, ya coloreadas (no se pueden
    /// secar con un tinte), que quedan sin uso y nunca se generan.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Gris del contorno de la liana y de las hojitas (fracción del tinte).</summary>
        public const float LianaOutlineShade = 0.40f;

        /// <summary>Gris de las hojitas junto al tallo (fracción del tinte): con el tinte atado, ≈ <see cref="Palette.LeafDark"/>.</summary>
        public const float LianaLeafBaseShade = 0.60f;

        /// <summary>Gris de las hojitas en la punta (fracción del tinte): con el tinte atado, <see cref="Palette.LeafMid"/>.</summary>
        public const float LianaLeafTipShade = 0.88f;

        /// <summary>Gris del tallo (fracción del tinte); el canto de arriba va a 1 y el de abajo, en sombra.</summary>
        public const float LianaStemShade = 0.88f;

        /// <summary>Gris del tramo trasero (fracción del tinte): con el tinte atado, ≈ <see cref="Palette.LeafDark"/>.</summary>
        public const float LianaBackShade = 0.62f;

        /// <summary>Opacidad horneada del tramo trasero (solo se ve sobre el vidrio vacío).</summary>
        public const float LianaBackAlpha = 0.5f;

        /// <summary>Lado de la ✓ (u).</summary>
        public const float LianaCheckU = 0.62f;

        /// <summary>Ancho del sprite de la vuelta del cuerpo (u): el de la cuia.</summary>
        public const float LianaCoilWidthU = 2.0f;

        /// <summary>Alto del sprite de la vuelta del cuerpo (u).</summary>
        public const float LianaCoilHeightU = 1.0f;

        /// <summary>Lo que baja la vuelta del cuerpo de izquierda a derecha (u): las vueltas, paralelas, forman una hélice.</summary>
        public const float LianaCoilTiltU = 0.35f;

        /// <summary>
        /// Flecha de la vuelta en el eje (u): el arco delantero de una elipse con la proporción de la cara superior del líquido
        /// (<see cref="CuiaGeometry.TopFaceU"/> de alto en el ancho de la cavidad, 0,25 del medio ancho), un poco más plana
        /// porque la vuelta va inclinada: 0,22 u en una cuia de 2 u.
        /// </summary>
        public const float LianaCoilSagU = 0.22f;

        /// <summary>
        /// Altura del arco en el eje sobre el centro de su sprite (u, negativa: debajo). El overlay coloca el sprite para que el
        /// arco cruce el eje sobre una junta de dosis.
        /// </summary>
        public const float LianaCoilAxisYU = LianaCoilLiftU - LianaCoilSagU;

        /// <summary>
        /// Medio ancho de la vuelta del cuerpo hasta el centro de sus extremos (u): con el tallo afinado, la liana acaba dentro
        /// del contorno del vidrio (≥ 0,95 u a la altura de las vueltas) y no sobresale.
        /// </summary>
        public const float LianaCoilHalfChordU = 0.91f;

        /// <summary>Nada de la vuelta (ni la sombra ni las hojitas) pasa de este medio ancho (u): el contorno del vidrio.</summary>
        public const float LianaCoilClipU = 0.945f;

        // El arco sube 0,05 u en su sprite para que las hojitas de arriba y de abajo quepan.
        private const float LianaCoilLiftU = 0.05f;

        /// <summary>Grosor del tallo en el eje (u): 2,7 dp en una cuia de 48 dp.</summary>
        public const float LianaStemU = 0.116f;

        /// <summary>Grosor del tallo en los extremos, donde pasa por detrás de la cuia (u).</summary>
        public const float LianaStemEndU = 0.068f;

        /// <summary>Ancho del sprite de la vuelta del cuello con el nudo (u).</summary>
        public const float LianaCollarWidthU = 1.2f;

        /// <summary>Alto del sprite de la vuelta del cuello con el nudo (u).</summary>
        public const float LianaCollarHeightU = 0.62f;

        /// <summary>Centro del nudo dentro del sprite del cuello, en x (u desde su centro): a la derecha, junto al rabito de la fruta.</summary>
        public const float LianaCollarKnotXU = 0.36f;

        /// <summary>Centro del nudo dentro del sprite del cuello, en y (u desde su centro): arriba, sobre el aro de fibra.</summary>
        public const float LianaCollarKnotYU = 0.10f;

        /// <summary>Gris de los lóbulos del nudo (fracción del tinte).</summary>
        public const float LianaKnotShade = 0.70f;

        /// <summary>Verde de la ✓ #2FBF57 [P] (ficha de Magic Sort §3.3: «marca verde ✓ sobre el icono»).</summary>
        public static readonly Color CheckGreen = Palette.Hex(0x2FBF57);

        // Sombra de contacto horneada [horno]: negro 0,25, (+1; −2) dp y 2 dp de desenfoque, con u ≈ 24 dp (0,042 y 0,083 u).
        private const float LianaShadowAlpha = 0.25f;
        private const float LianaShadowDxU = 0.042f;
        private const float LianaShadowDyU = -0.083f;
        private const float LianaShadowBlurU = 0.083f;

        // Luz de borde: 1 dp (0,042 u) por el canto de arriba del tallo.
        private const float LianaShineU = 0.042f;

        private const int LianaCurveSteps = 48;

        private Sprite? _lianaCoil;
        private Sprite? _lianaCoilShine;
        private Sprite? _lianaCoilBack;
        private Sprite? _lianaCollar;
        private Sprite? _lianaCollarShine;
        private Sprite? _lianaCheck;

        /// <summary>
        /// Vuelta de liana del cuerpo (<see cref="LianaCoilWidthU"/> × <see cref="LianaCoilHeightU"/> u) con dos hojitas y su
        /// sombra de contacto, en grises: la tiñe <c>Image.color</c>. El arco cruza el eje <see cref="LianaCoilAxisYU"/> sobre el
        /// centro del sprite (debajo).
        /// </summary>
        internal Sprite LianaCoil => _lianaCoil != null ? _lianaCoil : (_lianaCoil = BuildLianaCoil(CoilPart.Front, "mech_liana_coil"));

        /// <summary>Luz de borde de 1 dp de la vuelta del cuerpo (blanca, del tamaño de <see cref="LianaCoil"/>).</summary>
        internal Sprite LianaCoilShine => _lianaCoilShine != null ? _lianaCoilShine : (_lianaCoilShine = BuildLianaCoil(CoilPart.Shine, "mech_liana_coil_shine"));

        /// <summary>
        /// Tramo trasero de la vuelta del cuerpo (del tamaño de <see cref="LianaCoil"/>): 0,2 u en cada extremo que suben hacia
        /// dentro, en grises con alfa <see cref="LianaBackAlpha"/>. Va detrás del líquido: solo se ve sobre el vidrio vacío.
        /// </summary>
        internal Sprite LianaCoilBack => _lianaCoilBack != null ? _lianaCoilBack : (_lianaCoilBack = BuildLianaCoil(CoilPart.Back, "mech_liana_coil_back"));

        /// <summary>
        /// Vuelta del cuello (del grosor de las del cuerpo) que acaba en el nudo de tres lóbulos, con su sombra
        /// (<see cref="LianaCollarWidthU"/> × <see cref="LianaCollarHeightU"/> u), en grises: la tiñe <c>Image.color</c>.
        /// </summary>
        internal Sprite LianaCollar => _lianaCollar != null ? _lianaCollar : (_lianaCollar = BuildLianaCollar(false));

        /// <summary>Luz de borde de 1 dp de la vuelta del cuello (blanca, del tamaño de <see cref="LianaCollar"/>).</summary>
        internal Sprite LianaCollarShine => _lianaCollarShine != null ? _lianaCollarShine : (_lianaCollarShine = BuildLianaCollar(true));

        /// <summary>
        /// ✓ verde (<see cref="CheckGreen"/>) con contorno verde oscuro y halo crema, para leerse sobre cualquier fruta
        /// (<see cref="LianaCheckU"/> de lado, ya coloreada).
        /// </summary>
        internal Sprite LianaCheck => _lianaCheck != null ? _lianaCheck : (_lianaCheck = BuildLianaCheck());

        private enum CoilPart
        {
            Front,
            Shine,
            Back,
        }

        /// <summary>
        /// Punto <paramref name="t"/> de la vuelta del cuello (0 en el contorno izquierdo del borde, 1 junto al nudo), en u desde
        /// el centro de su sprite: sube hacia el nudo y cuelga un poco en medio (el arco delantero, en un cuello estrecho).
        /// </summary>
        public static Vector2 LianaCollarPointU(float t)
        {
            var a = new Vector2(-0.50f, -0.18f);
            var b = new Vector2(LianaCollarKnotXU - 0.06f, LianaCollarKnotYU - 0.01f);
            float s = Mathf.Clamp01(t);
            return Vector2.Lerp(a, b, s) + new Vector2(0f, -0.06f * Mathf.Sin(Mathf.PI * s));
        }

        /// <summary>
        /// Punto de la vuelta del cuerpo en <paramref name="xHat"/> (−1 a la izquierda, 1 a la derecha), en u desde el centro de
        /// su sprite: baja <see cref="LianaCoilTiltU"/> de izquierda a derecha y cuelga <see cref="LianaCoilSagU"/> en el eje.
        /// </summary>
        public static Vector2 LianaCoilPointU(float xHat)
        {
            float e = Mathf.Clamp(xHat, -1f, 1f);
            return new Vector2(e * LianaCoilHalfChordU, LianaCoilLiftU - (0.5f * LianaCoilTiltU * e) - (LianaCoilSagU * Mathf.Sqrt(Mathf.Max(0f, 1f - (e * e)))));
        }

        private static Color Gray(float v)
        {
            return new Color(v, v, v, 1f);
        }

        private Sprite BuildLianaCoil(CoilPart part, string name)
        {
            const float px = MechanicPxPerU;
            int w = Mathf.RoundToInt(LianaCoilWidthU * px);
            int h = Mathf.RoundToInt(LianaCoilHeightU * px);
            var pc = new PixelCanvas(w, h);
            var origin = new Vector2(w * 0.5f, h * 0.5f);
            var pts = new Vector2[LianaCurveSteps + 1];
            var radii = new float[LianaCurveSteps + 1];
            for (int i = 0; i <= LianaCurveSteps; i++)
            {
                float xHat = Mathf.Lerp(-1f, 1f, i / (float)LianaCurveSteps);
                pts[i] = origin + (LianaCoilPointU(xHat) * px);
                radii[i] = 0.5f * px * Mathf.Lerp(LianaStemEndU, LianaStemU, Mathf.Sqrt(Mathf.Max(0f, 1f - (xHat * xHat))));
            }

            if (part == CoilPart.Shine)
            {
                DrawLianaShine(pc, pts, radii, 0.55f, 0.88f);
            }
            else if (part == CoilPart.Back)
            {
                // Los extremos siguen por detrás de la cuia: el arco de atrás de la elipse sube desde cada extremo hacia dentro.
                float r = 0.03f * px;
                Color back = ColorMath.WithAlpha(Gray(LianaBackShade), LianaBackAlpha);
                DrawBackTail(pc, pts[0], new Vector2(0.45f, 0.89f), 0.2f * px, r, back);
                DrawBackTail(pc, pts[LianaCurveSteps], new Vector2(-0.45f, 0.89f), 0.2f * px, r, back);
            }
            else
            {
                // Dos hojitas alternas (arriba a la izquierda, abajo a la derecha), lejos del eje: no tapan el centro de ninguna
                // dosis.
                var leafBase = new Vector2[2];
                var leafDir = new Vector2[2];
                for (int j = 0; j < 2; j++)
                {
                    float xHat = j == 0 ? -0.46f : 0.52f;
                    Vector2 p = origin + (LianaCoilPointU(xHat) * px);
                    Vector2 tangent = ((LianaCoilPointU(xHat + 0.02f) - LianaCoilPointU(xHat - 0.02f)) * px).normalized;
                    var normal = new Vector2(-tangent.y, tangent.x);
                    if ((normal.y < 0f) == (j == 0))
                    {
                        normal = -normal; // la primera hacia arriba, la segunda hacia abajo
                    }

                    leafBase[j] = p;
                    leafDir[j] = ((normal * 0.8f) + (tangent * (j == 0 ? -0.55f : 0.55f))).normalized;
                }

                DrawLianaShadow(pc, pts, radii, leafBase, leafDir, 0.24f * px, 0.13f * px);
                for (int j = 0; j < 2; j++)
                {
                    DrawLianaLeaf(pc, leafBase[j], leafDir[j], 0.24f * px, 0.13f * px);
                }

                DrawLianaStem(pc, pts, radii);
            }

            ClipLianaCoil(pc, origin.x, LianaCoilClipU * px);
            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildLianaCollar(bool shine)
        {
            const float px = MechanicPxPerU;
            int w = Mathf.RoundToInt(LianaCollarWidthU * px);
            int h = Mathf.RoundToInt(LianaCollarHeightU * px);
            var pc = new PixelCanvas(w, h);
            var origin = new Vector2(w * 0.5f, h * 0.5f);
            Vector2 knot = origin + (new Vector2(LianaCollarKnotXU, LianaCollarKnotYU) * px);

            // La vuelta del cuello: del contorno izquierdo del borde al nudo, con la flecha del arco delantero (más corta: el
            // cuello es estrecho) y del grosor de las vueltas del cuerpo.
            var pts = new Vector2[LianaCurveSteps + 1];
            var radii = new float[LianaCurveSteps + 1];
            for (int i = 0; i <= LianaCurveSteps; i++)
            {
                float t = i / (float)LianaCurveSteps;
                pts[i] = origin + (LianaCollarPointU(t) * px);
                radii[i] = 0.5f * px * Mathf.Lerp(LianaStemEndU, LianaStemU, Mathf.Clamp01(t * 2.5f));
            }

            if (shine)
            {
                DrawLianaShine(pc, pts, radii, 0.25f, 0.82f);
                return MakeSprite(pc, "mech_liana_collar_shine", new Vector2(0.5f, 0.5f), Vector4.zero);
            }

            DrawLianaShadow(pc, pts, radii, null, null, 0f, 0f);
            DrawKnotShadow(pc, knot, px);
            DrawLianaStem(pc, pts, radii);
            DrawLianaKnot(pc, knot, px);
            return MakeSprite(pc, "mech_liana_collar", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildLianaCheck()
        {
            int s = Mathf.RoundToInt(LianaCheckU * MechanicPxPerU);
            var pc = new PixelCanvas(s, s);
            Vector2 a = new Vector2(0.18f * s, 0.52f * s);
            Vector2 b = new Vector2(0.41f * s, 0.27f * s);
            Vector2 c = new Vector2(0.84f * s, 0.76f * s);
            DrawCheckStroke(pc, a, b, c, 0.135f * s, ColorMath.WithAlpha(Palette.Cream, 0.95f)); // halo crema
            DrawCheckStroke(pc, a, b, c, 0.100f * s, Palette.Vein); // contorno
            DrawCheckStroke(pc, a, b, c, 0.068f * s, CheckGreen);
            Vector2 shine = Vector2.Lerp(b, c, 0.45f);
            pc.FillCapsule(shine.x - (0.05f * s), shine.y - (0.03f * s), shine.x + (0.06f * s), shine.y + (0.09f * s), 0.018f * s, ColorMath.WithAlpha(Palette.Glint, 0.8f));
            return MakeSprite(pc, "mech_liana_check", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Los dos trazos de la ✓ con radio r y extremos redondos.
        private static void DrawCheckStroke(PixelCanvas pc, Vector2 a, Vector2 b, Vector2 c, float r, Color color)
        {
            pc.FillCapsule(a.x, a.y, b.x, b.y, r, color);
            pc.FillCapsule(b.x, b.y, c.x, c.y, r, color);
        }

        // Sombra de contacto del tallo (y de las hojitas, si las hay) en un solo paso: la cobertura más alta de todas las piezas,
        // con el borde suave del desenfoque, desplazada y en negro 0,25. Así las piezas que se solapan no oscurecen dos veces.
        private static void DrawLianaShadow(PixelCanvas pc, Vector2[] pts, float[] radii, Vector2[]? leafBase, Vector2[]? leafDir, float leafLen, float leafWidth)
        {
            const float px = MechanicPxPerU;
            float soft = LianaShadowBlurU * px;
            var offset = new Vector2(LianaShadowDxU * px, LianaShadowDyU * px);
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f) - offset;
                    float d = PolylineDistance(pts, radii, p);
                    float cover = PixelCanvas.SoftCoverage(d, soft);
                    if (leafBase != null && leafDir != null)
                    {
                        for (int j = 0; j < leafBase.Length; j++)
                        {
                            cover = Mathf.Max(cover, PixelCanvas.SoftCoverage(LeafDistance(p, leafBase[j], leafDir[j], leafLen, leafWidth), soft));
                        }
                    }

                    if (cover > 0.002f)
                    {
                        pc.Blend(x, y, new Color(0f, 0f, 0f, LianaShadowAlpha), cover);
                    }
                }
            }
        }

        private static void DrawKnotShadow(PixelCanvas pc, Vector2 knot, float px)
        {
            var c = knot + new Vector2(LianaShadowDxU * px, LianaShadowDyU * px);
            pc.FillEllipseSoft(c.x, c.y, 0.2f * px, 0.16f * px, LianaShadowBlurU * px, new Color(0f, 0f, 0f, LianaShadowAlpha));
        }

        // Tallo en grises sobre la curva: contorno, cuerpo, el canto de abajo en sombra y el de arriba más claro (volumen).
        private static void DrawLianaStem(PixelCanvas pc, Vector2[] pts, float[] radii)
        {
            const float px = MechanicPxPerU;
            float outline = 0.016f * px;
            FillPolyline(pc, pts, radii, outline, 0f, 1f, Gray(LianaOutlineShade));
            FillPolyline(pc, pts, radii, 0f, 0f, 1f, Gray(LianaStemShade));
            FillPolyline(pc, pts, radii, 0f, -0.5f, 0.45f, Gray(0.72f));
            FillPolyline(pc, pts, radii, 0f, 0.42f, 0.30f, ColorMath.WithAlpha(Gray(1f), 0.9f));
        }

        // Hojita: contorno y relleno con degradado del tallo a la punta (de LianaLeafBaseShade a LianaLeafTipShade), y el nervio.
        private static void DrawLianaLeaf(PixelCanvas pc, Vector2 basePx, Vector2 dir, float length, float width)
        {
            const float px = MechanicPxPerU;
            float outline = 0.016f * px;
            int r = Mathf.CeilToInt(length + width + outline + 2f);
            for (int y = Mathf.FloorToInt(basePx.y) - r; y <= Mathf.CeilToInt(basePx.y) + r; y++)
            {
                for (int x = Mathf.FloorToInt(basePx.x) - r; x <= Mathf.CeilToInt(basePx.x) + r; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = LeafDistance(p, basePx, dir, length, width);
                    if (d > outline + 1f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, Gray(LianaOutlineShade), PixelCanvas.Coverage(d - outline));
                    float along = Mathf.Clamp01(Vector2.Dot(p - basePx, dir) / length);
                    pc.Blend(x, y, Gray(Mathf.Lerp(LianaLeafBaseShade, LianaLeafTipShade, along)), PixelCanvas.Coverage(d));
                }
            }

            Vector2 rib = basePx + (dir * length * 0.72f);
            pc.FillCapsule(basePx.x, basePx.y, rib.x, rib.y, 0.008f * px, Gray(LianaOutlineShade));
        }

        // Distancia con signo (px) a una hojita ovada desde basePx hacia dir (como DrawLeafShape: ancho según LeafWidthAt).
        private static float LeafDistance(Vector2 p, Vector2 basePx, Vector2 dir, float length, float width)
        {
            Vector2 n = new Vector2(-dir.y, dir.x);
            Vector2 rel = p - basePx;
            float u = Vector2.Dot(rel, dir) / length;
            float across = Mathf.Abs(Vector2.Dot(rel, n));
            if (u < 0f)
            {
                return Mathf.Sqrt((u * length * u * length) + (across * across));
            }

            if (u > 1f)
            {
                float over = (u - 1f) * length;
                return Mathf.Sqrt((over * over) + (across * across));
            }

            return across - (width * 0.5f * LeafWidthAt(LeafShape.Ovate, u));
        }

        // Luz de borde de 1 dp por el canto de arriba del tallo, entre las fracciones from y to de la curva, que se apaga en los
        // extremos (blanca: la tiñe el overlay).
        private static void DrawLianaShine(PixelCanvas pc, Vector2[] pts, float[] radii, float fadeFrom, float fadeTo)
        {
            const float px = MechanicPxPerU;
            float half = 0.5f * LianaShineU * px;
            int n = pts.Length;
            var line = new Vector2[n];
            var lineR = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 tangent = (pts[Mathf.Min(n - 1, i + 1)] - pts[Mathf.Max(0, i - 1)]).normalized;
                var normal = new Vector2(-tangent.y, tangent.x);
                if (normal.y < 0f)
                {
                    normal = -normal;
                }

                line[i] = pts[i] + (normal * Mathf.Max(0f, radii[i] - half));
                lineR[i] = half;
            }

            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = PolylineDistance(line, lineR, p, out float t);
                    if (d > 1f)
                    {
                        continue;
                    }

                    // Fracción de la curva: 0 y 1 en los extremos; la luz se apaga hacia ellos (simétrica en el eje).
                    float edge = Mathf.Abs((2f * t) - 1f);
                    float k = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeFrom, fadeTo, edge));
                    if (k > 0.001f)
                    {
                        pc.Blend(x, y, Color.white, PixelCanvas.Coverage(d) * k);
                    }
                }
            }
        }

        // Tramo trasero: desde un extremo de la vuelta, una curva corta que sube hacia dentro (el arco de atrás de la elipse).
        private static void DrawBackTail(PixelCanvas pc, Vector2 from, Vector2 dir, float length, float r, Color color)
        {
            const int steps = 10;
            Vector2 prev = from;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 bend = new Vector2(dir.x * 0.35f, 0f) * (t * t) * length;
                Vector2 p = from + (dir * t * length) + bend;
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, r * Mathf.Lerp(1f, 0.6f, t), color);
                prev = p;
            }
        }

        // Nudo de tres lóbulos (dos laterales y el central, más alto) con contorno, volumen y un brillo.
        private static void DrawLianaKnot(PixelCanvas pc, Vector2 c, float px)
        {
            Color outline = Gray(LianaOutlineShade);
            float o = 0.016f * px;
            float lx = 0.105f * px;
            float lrx = 0.10f * px;
            float lry = 0.085f * px;
            pc.FillEllipseSoft(c.x - lx, c.y - (0.01f * px), lrx + o, lry + o, 1.2f, outline);
            pc.FillEllipseSoft(c.x + lx, c.y - (0.01f * px), lrx + o, lry + o, 1.2f, outline);
            pc.FillEllipseSoft(c.x - lx, c.y - (0.01f * px), lrx, lry, 1.2f, Gray(LianaKnotShade));
            pc.FillEllipseSoft(c.x + lx, c.y - (0.01f * px), lrx, lry, 1.2f, Gray(LianaKnotShade - 0.04f));
            pc.FillEllipseSoft(c.x - lx - (0.02f * px), c.y + (0.025f * px), lrx * 0.45f, lry * 0.4f, 1.2f, Gray(0.86f));
            pc.FillEllipseSoft(c.x, c.y, (0.085f * px) + o, (0.125f * px) + o, 1.2f, outline);
            pc.FillEllipseSoft(c.x, c.y, 0.085f * px, 0.125f * px, 1.2f, Gray(LianaStemShade));
            pc.FillEllipseSoft(c.x + (0.015f * px), c.y - (0.04f * px), 0.05f * px, 0.06f * px, 2f, Gray(0.74f));
            pc.FillEllipseSoft(c.x - (0.025f * px), c.y + (0.045f * px), 0.025f * px, 0.04f * px, 1f, Gray(1f)); // brillo
        }

        // Rellena la curva con radio radii[i] × scale + grow (px), desplazada shift × radio a lo largo de la normal de arriba
        // (bandas de volumen), con un solo Blend por píxel (antialias limpio).
        private static void FillPolyline(PixelCanvas pc, Vector2[] pts, float[] radii, float grow, float shift, float scale, Color color)
        {
            int n = pts.Length;
            var line = new Vector2[n];
            var lineR = new float[n];
            float x0 = float.MaxValue;
            float x1 = float.MinValue;
            float y0 = float.MaxValue;
            float y1 = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                Vector2 tangent = (pts[Mathf.Min(n - 1, i + 1)] - pts[Mathf.Max(0, i - 1)]).normalized;
                var normal = new Vector2(-tangent.y, tangent.x);
                if (normal.y < 0f)
                {
                    normal = -normal;
                }

                line[i] = pts[i] + (normal * shift * radii[i]);
                lineR[i] = (radii[i] * scale) + grow;
                x0 = Mathf.Min(x0, line[i].x - lineR[i]);
                x1 = Mathf.Max(x1, line[i].x + lineR[i]);
                y0 = Mathf.Min(y0, line[i].y - lineR[i]);
                y1 = Mathf.Max(y1, line[i].y + lineR[i]);
            }

            for (int y = Mathf.Max(0, Mathf.FloorToInt(y0) - 1); y <= Mathf.Min(pc.Height - 1, Mathf.CeilToInt(y1) + 1); y++)
            {
                for (int x = Mathf.Max(0, Mathf.FloorToInt(x0) - 1); x <= Mathf.Min(pc.Width - 1, Mathf.CeilToInt(x1) + 1); x++)
                {
                    float d = PolylineDistance(line, lineR, new Vector2(x + 0.5f, y + 0.5f));
                    if (d < 1f)
                    {
                        pc.Blend(x, y, color, PixelCanvas.Coverage(d));
                    }
                }
            }
        }

        private static float PolylineDistance(Vector2[] pts, float[] radii, Vector2 p)
        {
            return PolylineDistance(pts, radii, p, out _);
        }

        // Distancia con signo (px) a la curva con radio variable y, en t, la fracción de la curva del punto más cercano.
        private static float PolylineDistance(Vector2[] pts, float[] radii, Vector2 p, out float t)
        {
            float best = float.MaxValue;
            t = 0f;
            int segments = pts.Length - 1;
            for (int i = 0; i < segments; i++)
            {
                Vector2 a = pts[i];
                Vector2 v = pts[i + 1] - a;
                float len2 = Mathf.Max(1e-6f, (v.x * v.x) + (v.y * v.y));
                float s = Mathf.Clamp01((((p.x - a.x) * v.x) + ((p.y - a.y) * v.y)) / len2);
                float dx = p.x - (a.x + (v.x * s));
                float dy = p.y - (a.y + (v.y * s));
                float d = Mathf.Sqrt((dx * dx) + (dy * dy)) - Mathf.Lerp(radii[i], radii[i + 1], s);
                if (d < best)
                {
                    best = d;
                    t = (i + s) / segments;
                }
            }

            return best;
        }

        // Nada de la vuelta pasa del contorno del vidrio: recorta el alfa fuera de |x − cx| ≤ clip (px), con antialias.
        private static void ClipLianaCoil(PixelCanvas pc, float cx, float clip)
        {
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    float d = Mathf.Abs((x + 0.5f) - cx) - clip;
                    if (d > -1f)
                    {
                        pc.MaskAlpha(x, y, PixelCanvas.Coverage(d));
                    }
                }
            }
        }
    }
}
