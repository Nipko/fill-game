#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del Brote (M2; 02 §9.1 M2; propuesta de D-007 b.2 M2; D-003 «Selva brillante premium»): el
    /// capullo que cierra la boca (<see cref="BroteCork"/>, en grises con contorno oscuro para que lo tiña el color de la cima),
    /// su brillo (<see cref="BroteGloss"/>), el cáliz verde que lo sujeta y tapa la boca entera
    /// (<see cref="BroteSepals"/>) y la hojita verde que asoma por detrás a cada lado y que se abre al germinar
    /// (<see cref="BroteSprout"/>).
    /// </summary>
    /// <remarks>
    /// <para>Tamaño (prueba del dueño del 2026-10-09: el brote se veía pequeño): el cáliz mide <see cref="BroteSepalsWidthU"/>
    /// = 1,32 u, más que el borde de fibra (1,10 u: la boca entera), y el capullo llega a 0,90 u sobre lo alto de la cuia, es
    /// decir, 1,15 u sobre el labio de vertido (<c>CuiaGeometry.LipU</c>). Las hojitas abiertas asoman más allá del cáliz,
    /// así que el verde se ve a los dos lados y por encima de la boca.</para>
    /// <para>Silueta distinta de la tapa de sellado (una cúpula de calabaza baja de 1,10 × 0,35 u) y de la Hoja (una hoja de
    /// palma apaisada de 2,3 × 1 u con la fruta llave en un medallón): un capullo alto y en punta, del color de la cima, en una
    /// copa de sépalos verdes con dos hojitas en V.</para>
    /// <para>Perezosos: se generan la primera vez que se piden (el overlay los pide al unirse a la cuia, nunca en un paso) y
    /// tras <see cref="Dispose"/> se vuelven a generar. A <see cref="MechanicPxPerU"/> px por u.</para>
    /// <para>Color (direccion-visual-v2 §2.4, regla 1): el capullo lleva el tinte de la cima a propósito (es la llave que lo
    /// hace germinar; excepción de la regla). Sépalos y hojitas van en <c>hoja_oscura</c> → <c>hoja_media</c> (ΔE00 ≥ 16 de los
    /// tintes), con contorno <c>vena</c> y luz de borde de 1,5 dp en <c>hoja_brillo</c> (líneas de 2 dp o menos).</para>
    /// <para>Reutiliza <c>DrawLeafShape</c> (silueta, contorno y costura del capullo y de los sépalos) y <c>DrawLeaf</c> (las
    /// hojitas, con su degradado, nervios y luz de borde, como el follaje de la escena).</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del sprite del capullo (u).</summary>
        public const float BroteCorkWidthU = 1.0f;

        /// <summary>Alto del sprite del capullo (u).</summary>
        public const float BroteCorkHeightU = 1.12f;

        /// <summary>Ancho del cáliz (u): tapa el borde de fibra de 1,10 u (la boca) con 0,11 u de margen por lado.</summary>
        public const float BroteSepalsWidthU = 1.32f;

        /// <summary>Alto del sprite del cáliz (u).</summary>
        public const float BroteSepalsHeightU = 0.62f;

        /// <summary>Ancho del sprite de una hojita (u).</summary>
        public const float BroteSproutWidthU = 0.5f;

        /// <summary>Alto del sprite de una hojita (u): su punta está arriba, en el centro.</summary>
        public const float BroteSproutHeightU = 0.88f;

        /// <summary>Hojita y sépalos del Brote, cara en luz: <c>hoja_media</c> #23824A (ΔE00 16,3 de Lulo, el tinte más cercano).</summary>
        public static readonly Color BroteLeafLight = Palette.LeafMid;

        /// <summary>Hojita y sépalos del Brote, cara en sombra: <c>hoja_oscura</c> #145A35.</summary>
        public static readonly Color BroteLeafDark = Palette.LeafDark;

        private Sprite? _broteCork;
        private Sprite? _broteGloss;
        private Sprite? _broteSprout;
        private Sprite? _broteSepals;

        /// <summary>
        /// Capullo (<see cref="BroteCorkWidthU"/> × <see cref="BroteCorkHeightU"/> u, pivote en el pie): dos pétalos plegados
        /// que se cierran en punta, con su costura y un contorno oscuro. En grises con la luz arriba a la izquierda: lo tiñe el
        /// color de la cima (02 §9.1 M2), así que el contorno queda en un tono oscuro del mismo tinte.
        /// </summary>
        internal Sprite BroteCork => _broteCork != null ? _broteCork : (_broteCork = BuildBroteCork());

        /// <summary>Brillo del capullo: un trazo blanco suave (no se tiñe, para que se vea pulido con cualquier tinte).</summary>
        internal Sprite BroteGloss => _broteGloss != null ? _broteGloss : (_broteGloss = BuildBroteGloss());

        /// <summary>Hojita verde (<see cref="BroteSproutWidthU"/> × <see cref="BroteSproutHeightU"/> u, pivote en el pie), ya coloreada.</summary>
        internal Sprite BroteSprout => _broteSprout != null ? _broteSprout : (_broteSprout = BuildBroteSprout());

        /// <summary>
        /// Cáliz (<see cref="BroteSepalsWidthU"/> × <see cref="BroteSepalsHeightU"/> u, pivote en el pie), ya coloreado: un
        /// collarín verde que encaja en la boca y cuatro sépalos en punta que abrazan el pie del capullo, con contorno
        /// <c>vena</c>. Deja ver el capullo por el centro.
        /// </summary>
        internal Sprite BroteSepals => _broteSepals != null ? _broteSepals : (_broteSepals = BuildBroteSepals());

        private Sprite BuildBroteCork()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(BroteCorkWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(BroteCorkHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            var outline = new Color(0.26f, 0.26f, 0.26f, 1f);

            // Capullo: una gota en punta (contorno de 3 px y relleno claro).
            var budBase = new Vector2(cx, 2f * k);
            float budLen = h - (4f * k);
            DrawLeafShape(pc, budBase, Vector2.up, budLen, w - (2f * k), outline, 1f);
            DrawLeafShape(pc, budBase + new Vector2(0f, 3f * k), Vector2.up, budLen - (6.5f * k), w - (9f * k), new Color(0.88f, 0.88f, 0.88f, 1f), 1f);

            // Volumen: luz a 35° desde arriba a la izquierda (1,0) y sombra abajo a la derecha (0,62), solo sobre lo pintado.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color p = pc.Get(x, y);
                    if (p.a <= 0f || p.r < 0.5f)
                    {
                        continue; // fuera, o en el contorno
                    }

                    float nx = ((x + 0.5f) - cx) / (w * 0.42f);
                    float ny = ((y + 0.5f) - (h * 0.5f)) / (h * 0.5f);
                    float lit = Mathf.Clamp01(0.5f + (0.62f * ((-nx * 0.57f) + (ny * 0.35f))));
                    float v = p.r * Mathf.Lerp(0.70f, 1.14f, lit);
                    pc.Set(x, y, new Color(Mathf.Min(1f, v), Mathf.Min(1f, v), Mathf.Min(1f, v), p.a));
                }
            }

            // Costura de los dos pétalos plegados: del pico hacia el pie, curvada a la derecha (la tapan los sépalos abajo).
            Vector2 tip = budBase + new Vector2(0f, budLen);
            Vector2 prev = tip - new Vector2(0f, 6f * k);
            for (int i = 1; i <= 16; i++)
            {
                float t = i / 16f;
                var p = new Vector2(cx + (9f * k * Mathf.Sin(Mathf.PI * 0.85f * t)), Mathf.Lerp(tip.y - (6f * k), h * 0.28f, t));
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, 1.1f * k, new Color(0.40f, 0.40f, 0.40f, 1f));
                prev = p;
            }

            return MakeSprite(pc, "mech_brote_cork", new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildBroteGloss()
        {
            const int w = 24;
            const int h = 48;
            var pc = new PixelCanvas(w, h);
            pc.FillEllipseSoft(w * 0.5f, h * 0.5f, (w * 0.5f) - 5f, (h * 0.5f) - 4f, 4f, new Color(1f, 1f, 1f, 0.75f));
            pc.FillEllipseSoft(w * 0.45f, h * 0.62f, 2.6f, 7f, 1.5f, Color.white);
            return MakeSprite(pc, "mech_brote_gloss", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildBroteSprout()
        {
            int w = Mathf.RoundToInt(BroteSproutWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(BroteSproutHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            const float k = 3.5f; // px por dp con la cuia de 48-56 dp (u de 24-28 dp a 96 px/u): grosor de nervios y luz de borde
            var basePx = new Vector2(w * 0.5f, 2f);
            Vector2 dir = Vector2.up;
            float len = h - 5f;
            DrawLeafShape(pc, basePx - new Vector2(0f, 1.2f), dir, len + 2.4f, w - 2f, Palette.Vein, 1f);
            DrawLeaf(pc, basePx, dir, len, w - 6f, -0.06f, LeafShape.Ovate, BroteLeafDark, BroteLeafLight, k, 163);
            return MakeSprite(pc, "mech_brote_sprout", new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildBroteSepals()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(BroteSepalsWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(BroteSepalsHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            Color vein = Palette.Vein;

            // Sépalos: dos por lado, el de fuera más abierto; nacen del collarín y abrazan el pie del capullo.
            var bases = new Vector2[4];
            var dirs = new Vector2[4];
            var lens = new float[4];
            var widths = new float[4];
            for (int s = 0; s < 4; s++)
            {
                float side = s < 2 ? -1f : 1f;
                bool outer = s == 0 || s == 3;
                bases[s] = new Vector2(cx + (side * (outer ? 30f : 13f) * k), 15f * k);
                dirs[s] = new Vector2(side * (outer ? 0.72f : 0.30f), outer ? 0.69f : 0.95f).normalized;
                lens[s] = (outer ? 36f : 42f) * k;
                widths[s] = (outer ? 24f : 22f) * k;
            }

            // Contornos primero (collarín y sépalos), después los rellenos: los contornos no cruzan los sépalos vecinos.
            float collarL = 6f * k;
            float collarR = w - (6f * k);
            float collarTop = 25f * k;
            pc.FillRoundedRect(collarL - (2.5f * k), 1f * k, collarR + (2.5f * k), collarTop + (2.5f * k), 11f * k, vein);
            for (int s = 0; s < 4; s++)
            {
                DrawLeafShape(pc, bases[s] - (dirs[s] * (2f * k)), dirs[s], lens[s] + (4.5f * k), widths[s] + (5f * k), vein, 1f);
            }

            for (int s = 0; s < 4; s++)
            {
                DrawLeaf(pc, bases[s], dirs[s], lens[s], widths[s], 0f, LeafShape.Ovate, BroteLeafDark, BroteLeafLight, 3.5f, 160 + s);
            }

            // Collarín: banda redondeada que encaja en la boca, de hoja_oscura abajo a hoja_media arriba, con luz en el canto.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = PixelCanvas.RoundedRectDistance(x + 0.5f, y + 0.5f, collarL, 3.5f * k, collarR, collarTop, 9f * k);
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01(((y + 0.5f) - (3.5f * k)) / (collarTop - (3.5f * k)));
                    Color c = Color.Lerp(BroteLeafDark, BroteLeafLight, Mathf.SmoothStep(0f, 1f, t));
                    float side = Mathf.Abs((x + 0.5f) - cx) / (cx - collarL);
                    c = ColorMath.Darken(c, 0.18f * Mathf.Clamp01((side - 0.6f) / 0.4f));
                    if (d > -2f * k && y > collarTop - (5f * k))
                    {
                        c = Color.Lerp(c, Palette.LeafShine, 0.45f); // luz del canto de arriba
                    }

                    pc.Blend(x, y, c, cov);
                }
            }

            return MakeSprite(pc, "mech_brote_sepals", new Vector2(0.5f, 0f), Vector4.zero);
        }
    }
}
