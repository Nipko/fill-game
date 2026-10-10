#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Liana (02 §9.1 M3; propuesta de D-007 b.2 M3; ficha de Magic Sort §3.3; D-003): la vuelta
    /// de liana con hojitas y el nudo en grises, para que <c>Image.color</c> los tiña de verde hoja oscuro (atada) o de pajizo
    /// (seca, al soltarse), y la ✓ verde del desbloqueo, que va como sprite porque el atlas TMP no tiene U+2713 (arquitectura
    /// del paso 1 §12).
    /// </summary>
    /// <remarks>
    /// <para>Tonos de gris de la liana (fracción del tinte): contorno <see cref="LianaOutlineShade"/>, hojitas
    /// <see cref="LianaLeafShade"/>, tallo <see cref="LianaStemShade"/> y un brillo de 1 en el lado de arriba del tallo. Con el
    /// tinte <c>LianaOverlay.FreshTint</c> el tallo queda en <see cref="Palette.LeafMid"/> y las hojitas cerca de
    /// <see cref="Palette.LeafDark"/>, los verdes del arte de T03, a ΔE00 ≥ 10 de los 14 tintes; con
    /// <see cref="Palette.FiberBase"/>, en pajizo de fibra.</para>
    /// <para>Perezosos: un nivel sin Liana no crea estas texturas, y tras <see cref="Dispose"/> se vuelven a generar.
    /// Sustituyen a <see cref="LianaWrap"/> y <see cref="LianaKnot"/> de <c>ProceduralArt.Mechanics.cs</c>, ya coloreadas (no
    /// se pueden secar con un tinte), que quedan sin uso y nunca se generan.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Gris del contorno de la liana y de las hojitas (fracción del tinte).</summary>
        public const float LianaOutlineShade = 0.40f;

        /// <summary>Gris de las hojitas (fracción del tinte).</summary>
        public const float LianaLeafShade = 0.66f;

        /// <summary>Gris del tallo (fracción del tinte); el brillo del lado de arriba va a 1.</summary>
        public const float LianaStemShade = 0.88f;

        /// <summary>Lado de la ✓ (u).</summary>
        public const float LianaCheckU = 0.62f;

        /// <summary>Verde de la ✓ #2FBF57 [P] (ficha de Magic Sort §3.3: «marca verde ✓ sobre el icono»).</summary>
        public static readonly Color CheckGreen = Palette.Hex(0x2FBF57);

        private Sprite? _lianaVine;
        private Sprite? _lianaVineKnot;
        private Sprite? _lianaCheck;

        /// <summary>Una vuelta de liana en diagonal (2,2 × 1 u) con dos hojitas, en grises: la tiñe <c>Image.color</c>.</summary>
        internal Sprite LianaVine => _lianaVine != null ? _lianaVine : (_lianaVine = BuildLianaVine());

        /// <summary>Nudo de la liana en el cuello (0,5 × 0,42 u), en grises: lo tiñe <c>Image.color</c>.</summary>
        internal Sprite LianaVineKnot => _lianaVineKnot != null ? _lianaVineKnot : (_lianaVineKnot = BuildLianaVineKnot());

        /// <summary>
        /// ✓ verde (<see cref="CheckGreen"/>) con contorno verde oscuro y halo crema, para leerse sobre cualquier fruta
        /// (<see cref="LianaCheckU"/> de lado, ya coloreada).
        /// </summary>
        internal Sprite LianaCheck => _lianaCheck != null ? _lianaCheck : (_lianaCheck = BuildLianaCheck());

        private static Color Gray(float v)
        {
            return new Color(v, v, v, 1f);
        }

        private Sprite BuildLianaVine()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(2.2f * MechanicPxPerU);
            int h = MechanicPxPerU;
            var pc = new PixelCanvas(w, h);
            // La curva baja por el centro (de frente, una vuelta alrededor de un cuerpo redondo se ve como una sonrisa
            // inclinada) y se afina en los extremos, donde la liana pasa por detrás de la cuia.
            DrawLianaVine(pc, new Vector2(5f * k, 18f * k), new Vector2(w * 0.5f, h * 0.10f), new Vector2(w - (5f * k), h - (14f * k)), 8f * k, k);
            return MakeSprite(pc, "mech_liana_vine", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildLianaVineKnot()
        {
            int w = Mathf.RoundToInt(0.5f * MechanicPxPerU);
            int h = Mathf.RoundToInt(0.42f * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            Color outline = Gray(LianaOutlineShade);
            Color lobe = Gray(LianaLeafShade);
            pc.FillEllipseSoft(cx - (w * 0.16f), cy, w * 0.27f, h * 0.31f, 1.2f, outline);
            pc.FillEllipseSoft(cx + (w * 0.16f), cy, w * 0.27f, h * 0.31f, 1.2f, outline);
            pc.FillEllipseSoft(cx - (w * 0.16f), cy, w * 0.21f, h * 0.24f, 1.2f, lobe);
            pc.FillEllipseSoft(cx + (w * 0.16f), cy, w * 0.21f, h * 0.24f, 1.2f, lobe);
            pc.FillEllipseSoft(cx, cy, w * 0.22f, h * 0.38f, 1.2f, outline);
            pc.FillEllipseSoft(cx, cy, w * 0.17f, h * 0.32f, 1.2f, Gray(LianaStemShade));
            pc.FillEllipseSoft(cx - (w * 0.05f), cy + (h * 0.12f), w * 0.06f, h * 0.10f, 1f, Gray(1f)); // brillo
            return MakeSprite(pc, "mech_liana_vine_knot", new Vector2(0.5f, 0.5f), Vector4.zero);
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

        // Liana en grises sobre la curva a → c → b, más fina en los extremos: contorno, tallo con un brillo en el lado de
        // arriba y tres hojitas con su nervio (como DrawVine, que la pinta ya en verde para el icono).
        private static void DrawLianaVine(PixelCanvas pc, Vector2 a, Vector2 c, Vector2 b, float half, float k)
        {
            const int steps = 48;
            Color outline = Gray(LianaOutlineShade);
            for (int pass = 0; pass < 3; pass++)
            {
                Vector2 prev = a;
                for (int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Vector2 p = Quad(a, c, b, t);
                    float r = half * (0.55f + (0.45f * Mathf.Sin(Mathf.PI * t)));
                    if (pass == 0)
                    {
                        pc.FillCapsule(prev.x, prev.y, p.x, p.y, r + (1.4f * k), outline);
                    }
                    else if (pass == 1)
                    {
                        pc.FillCapsule(prev.x, prev.y, p.x, p.y, r, Gray(LianaStemShade));
                    }
                    else
                    {
                        Vector2 tangent = (p - prev).normalized;
                        var normal = new Vector2(-tangent.y, tangent.x);
                        if (normal.y < 0f)
                        {
                            normal = -normal;
                        }

                        Vector2 o = normal * (r * 0.38f);
                        pc.FillCapsule(prev.x + o.x, prev.y + o.y, p.x + o.x, p.y + o.y, r * 0.28f, ColorMath.WithAlpha(Gray(1f), 0.85f));
                    }

                    prev = p;
                }
            }

            for (int j = 0; j < 3; j++)
            {
                float t = j == 0 ? 0.24f : (j == 1 ? 0.52f : 0.8f);
                Vector2 p = Quad(a, c, b, t);
                Vector2 tangent = (Quad(a, c, b, t + 0.02f) - Quad(a, c, b, t - 0.02f)).normalized;
                Vector2 normal = new Vector2(-tangent.y, tangent.x) * ((j % 2) == 0 ? 1f : -1f);
                Vector2 dir = ((normal * 0.8f) + (tangent * 0.6f)).normalized;
                float len = j == 1 ? 20f : 24f;
                DrawLeafShape(pc, p, dir, (len + 2f) * k, 13f * k, outline, 1f);
                DrawLeafShape(pc, p + (dir * 1f * k), dir, len * k, 11f * k, Gray(LianaLeafShade), 1f);
                Vector2 rib = p + (dir * (len - 5f) * k);
                pc.FillCapsule(p.x, p.y, rib.x, rib.y, 0.7f * k, outline);
            }
        }
    }
}
