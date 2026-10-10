#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del Brote (M2; 02 §9.1 M2; propuesta de D-007 b.2 M2; D-003 «Selva brillante premium»): el
    /// tapón-brote que cierra la boca (<see cref="BroteCork"/>, en grises para que lo tiña el color de la cima), su brillo
    /// (<see cref="BroteGloss"/>) y la hojita verde plegada que asoma por detrás a cada lado y que se abre al germinar
    /// (<see cref="BroteSprout"/>).
    /// </summary>
    /// <remarks>
    /// <para>Perezosos: se generan la primera vez que se piden (el overlay los pide al unirse a la cuia, nunca en un paso) y
    /// tras <see cref="Dispose"/> se vuelven a generar. A <see cref="MechanicPxPerU"/> px por u.</para>
    /// <para>Color (direccion-visual-v2 §2.4, regla 1): el tapón lleva el tinte de la cima a propósito (es la llave que lo hace
    /// germinar; excepción de la regla). Las hojitas van en <c>hoja_oscura</c> → <c>hoja_media</c> (ΔE00 ≥ 16 de los tintes),
    /// con nervadura y luz de borde de 1,5 dp en <c>hoja_brillo</c> (líneas de 2 dp o menos).</para>
    /// <para>Reutiliza <c>DrawLeafShape</c> (silueta y costura del tapón, contorno de las hojitas) y <c>DrawLeaf</c> (las
    /// hojitas, con su degradado, nervios y luz de borde, como el follaje de la escena).</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del sprite del tapón-brote (u).</summary>
        public const float BroteCorkWidthU = 0.92f;

        /// <summary>Alto del sprite del tapón-brote (u).</summary>
        public const float BroteCorkHeightU = 1.0f;

        /// <summary>Ancho del sprite de una hojita (u).</summary>
        public const float BroteSproutWidthU = 0.42f;

        /// <summary>Alto del sprite de una hojita (u).</summary>
        public const float BroteSproutHeightU = 0.74f;

        /// <summary>Hojita del Brote, cara en luz: <c>hoja_media</c> #23824A (ΔE00 16,3 de Lulo, el tinte más cercano).</summary>
        public static readonly Color BroteLeafLight = Palette.LeafMid;

        /// <summary>Hojita del Brote, cara en sombra: <c>hoja_oscura</c> #145A35.</summary>
        public static readonly Color BroteLeafDark = Palette.LeafDark;

        private Sprite? _broteCork;
        private Sprite? _broteGloss;
        private Sprite? _broteSprout;

        /// <summary>
        /// Tapón-brote (<see cref="BroteCorkWidthU"/> × <see cref="BroteCorkHeightU"/> u, pivote en el pie): un collarín
        /// encajado en la boca y encima un capullo puntiagudo de dos hojitas plegadas, con su costura. En grises con la luz
        /// arriba a la izquierda y contorno oscuro: lo tiñe el color de la cima (02 §9.1 M2).
        /// </summary>
        internal Sprite BroteCork => _broteCork != null ? _broteCork : (_broteCork = BuildBroteCork());

        /// <summary>Brillo del tapón: un trazo blanco suave (no se tiñe, para que el capullo se vea pulido con cualquier tinte).</summary>
        internal Sprite BroteGloss => _broteGloss != null ? _broteGloss : (_broteGloss = BuildBroteGloss());

        /// <summary>Hojita verde (<see cref="BroteSproutWidthU"/> × <see cref="BroteSproutHeightU"/> u, pivote en el pie), ya coloreada.</summary>
        internal Sprite BroteSprout => _broteSprout != null ? _broteSprout : (_broteSprout = BuildBroteSprout());

        private Sprite BuildBroteCork()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(BroteCorkWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(BroteCorkHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            var outline = new Color(0.30f, 0.30f, 0.30f, 1f);

            // Collarín encajado en la boca (abajo) y capullo (dos hojitas plegadas que se cierran en punta).
            float collarTop = 24f * k;
            float collarHalf = 40f * k;
            pc.FillRoundedRect(cx - collarHalf - (1.5f * k), 2f * k, cx + collarHalf + (1.5f * k), collarTop + (1.5f * k), 9f * k, outline);
            Vector2 budBase = new Vector2(cx, collarTop - (6f * k));
            float budLen = h - budBase.y - (4f * k);
            DrawLeafShape(pc, budBase - new Vector2(0f, 1.6f * k), Vector2.up, budLen + (3f * k), 76f * k, outline, 1f);
            pc.FillRoundedRect(cx - collarHalf, 3.5f * k, cx + collarHalf, collarTop, 8f * k, new Color(0.80f, 0.80f, 0.80f, 1f));
            DrawLeafShape(pc, budBase, Vector2.up, budLen, 72f * k, new Color(0.86f, 0.86f, 0.86f, 1f), 1f);

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
                    float ny = ((y + 0.5f) - (h * 0.55f)) / (h * 0.5f);
                    float lit = Mathf.Clamp01(0.5f + (0.62f * ((-nx * 0.57f) + (ny * 0.35f))));
                    float v = p.r * Mathf.Lerp(0.72f, 1.16f, lit);
                    pc.Set(x, y, new Color(Mathf.Min(1f, v), Mathf.Min(1f, v), Mathf.Min(1f, v), p.a));
                }
            }

            // Costura de las dos hojitas plegadas: del pico al collarín, curvada a la derecha; y el canto del collarín.
            Vector2 tip = budBase + new Vector2(0f, budLen);
            Vector2 prev = tip - new Vector2(0f, 3f * k);
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;
                var p = new Vector2(cx + (5f * k * Mathf.Sin(Mathf.PI * t)), Mathf.Lerp(tip.y - (3f * k), collarTop + (1f * k), t));
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, 0.9f * k, new Color(0.42f, 0.42f, 0.42f, 1f));
                prev = p;
            }

            pc.FillCapsule(cx - collarHalf + (4f * k), collarTop - (2.5f * k), cx + collarHalf - (4f * k), collarTop - (2.5f * k), 0.9f * k, new Color(0.55f, 0.55f, 0.55f, 1f));
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
    }
}
