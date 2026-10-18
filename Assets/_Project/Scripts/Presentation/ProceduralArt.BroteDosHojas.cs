#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del Brote de dos hojas (variante del Brote de la 1.0, estreno en el 343; 02 §9.1 «Variantes» y R10 b;
    /// propuesta de D-007 b.3: «Brote con dos hojas cerradas; cada dosis abre una»; tarea VA1b). En el tablero reutiliza el
    /// capullo y el cáliz del Brote (<see cref="BroteCork"/> y <see cref="BroteSepals"/>) y suma su hoja grande
    /// (<see cref="BroteWrapLeaf"/>), que <c>BroteOverlay</c> pone a cada lado del capullo, por delante. Aquí va también el icono
    /// grande del estreno (<see cref="CardHeroIcon"/> con <see cref="BroteTwoLeavesIconKey"/>). El nombre «Brote de dos hojas»
    /// está pendiente del asesor cultural (glosario §6.5).
    /// </summary>
    /// <remarks>
    /// <para>Color (revisión de VA1b: a tamaño de teléfono las hojas grandes en <c>hoja_oscura</c> se leían como dos rayas
    /// oscuras sobre el capullo): verde medio <see cref="BroteWrapFill"/> con luz <see cref="BroteWrapVein"/> en la nervadura y
    /// un filo de 1 dp <see cref="BroteWrapEdge"/>. La revisión pedía #4E9A3C y #7CC55A, que quedan a ΔE00 8,7 y 8,9 del Lulo
    /// (#4EB265); van los más cercanos que cumplen ΔE00 ≥ 10 con los 14 tintes (direccion-visual-v2 §2.4, regla 1): #4C9236
    /// (11,1) y #86C452 (10,7).</para>
    /// <para>Icono del estreno: simétrico, como el del Brote (163): el capullo centrado sobre su cáliz, en un melocotón neutro
    /// (no verde y sin tinte de fruta, 07 §3, regla 6: #E8B080 y #D49A68, ΔE00 ≥ 11 de los tintes; la revisión proponía la crema
    /// #E9D7A8, a ΔE00 1,0 del Copoazú), y dos hojas iguales a ±35° de la base, de 0,45 del alto del capullo, en los colores de
    /// la hoja grande del tablero. Ocupa en el medallón lo mismo que el del Brote.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Clave del icono grande del Brote de dos hojas (<c>intro_mechanic</c> <c>brote_n2</c> [P] del 343; VA1b): no es un tipo
        /// de mecánica propio (es el Brote con <c>n</c> = 2), así que <see cref="CardHeroIcon"/> lo dibuja aparte.
        /// </summary>
        public const string BroteTwoLeavesIconKey = "brote_n2";

        /// <summary>Ancho del sprite de la hoja grande del Brote de dos hojas (u).</summary>
        public const float BroteWrapWidthU = 0.46f;

        /// <summary>Alto del sprite de la hoja grande del Brote de dos hojas (u): su punta está arriba, en el centro.</summary>
        public const float BroteWrapHeightU = 0.88f;

        /// <summary>Hoja grande del Brote de dos hojas: verde medio #4C9236 (ΔE00 11,1 del Lulo, el tinte más cercano).</summary>
        public static readonly Color BroteWrapFill = new Color32(0x4C, 0x92, 0x36, 0xFF);

        /// <summary>Luz de la nervadura de la hoja grande: #86C452 (ΔE00 10,7 del Lulo).</summary>
        public static readonly Color BroteWrapVein = new Color32(0x86, 0xC4, 0x52, 0xFF);

        /// <summary>Filo de 1 dp de la hoja grande: #1F4D2A.</summary>
        public static readonly Color BroteWrapEdge = new Color32(0x1F, 0x4D, 0x2A, 0xFF);

        // Capullo del icono, en melocotón neutro (luz y sombra) con contorno pardo: ni verde ni tinte de fruta.
        private static readonly Color IconBudLight = new Color32(0xE8, 0xB0, 0x80, 0xFF);
        private static readonly Color IconBudShade = new Color32(0xD4, 0x9A, 0x68, 0xFF);
        private static readonly Color IconBudOutline = new Color32(0x7A, 0x5A, 0x3A, 0xFF);

        private Sprite? _broteWrap;

        /// <summary>
        /// Hoja grande del Brote de dos hojas (<see cref="BroteWrapWidthU"/> × <see cref="BroteWrapHeightU"/> u, pivote en el pie),
        /// ya coloreada: verde medio con la nervadura en luz y un filo oscuro de 1 dp.
        /// </summary>
        internal Sprite BroteWrapLeaf => _broteWrap != null ? _broteWrap : (_broteWrap = BuildBroteWrapLeaf());

        private Sprite BuildBroteWrapLeaf()
        {
            int w = Mathf.RoundToInt(BroteWrapWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(BroteWrapHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            const float dp = 3.5f; // px por dp con la cuia de 48-56 dp (u de 24-28 dp a 96 px/u)
            float cx = w * 0.5f;
            var basePx = new Vector2(cx, 1f);
            float len = h - 2f;

            // Filo de 1 dp y relleno.
            DrawLeafShape(pc, basePx, Vector2.up, len, w - 2f, BroteWrapEdge, 1f);
            DrawLeafShape(pc, basePx + new Vector2(0f, dp), Vector2.up, len - (2.4f * dp), w - 2f - (2f * dp), BroteWrapFill, 1f);

            // Nervadura en luz, más fina hacia la punta, y dos pares de nervios laterales cortos y tenues.
            pc.FillCapsule(cx, 3f * dp, cx, len * 0.55f, 0.75f * dp, BroteWrapVein);
            pc.FillCapsule(cx, len * 0.55f, cx, len * 0.82f, 0.45f * dp, BroteWrapVein);
            for (int i = 0; i < 2; i++)
            {
                float y = len * (0.30f + (0.22f * i));
                float reach = (w * 0.5f) - (2.6f * dp) - (i * dp);
                Color side = ColorMath.WithAlpha(BroteWrapVein, 0.55f);
                pc.FillCapsule(cx, y, cx - reach, y + (2.2f * dp), 0.3f * dp, side);
                pc.FillCapsule(cx, y, cx + reach, y + (2.2f * dp), 0.3f * dp, side);
            }

            return MakeSprite(pc, "mech_brote_wrap", new Vector2(0.5f, 0f), Vector4.zero);
        }

        // Brote de dos hojas en un lienzo cuadrado de s px (k = s / 96; y hacia arriba): el capullo y el cáliz del icono del Brote
        // en el mismo sitio y tamaño, el capullo en melocotón neutro, y delante dos hojas iguales que nacen de la base a ±35° de
        // la vertical, de 0,45 del alto del capullo, con el verde, la nervadura y el filo de la hoja grande del tablero.
        private static void DrawBroteTwoLeavesGlyph(PixelCanvas pc, int s)
        {
            float k = s / 96f;
            const float budLen = 70f;
            DrawBud(pc, 48f * k, 18f * k, budLen * k, 24f * k, IconBudLight, IconBudShade, IconBudOutline);
            DrawCalyx(pc, 48f * k, 8f * k, 24f * k, 20f * k, Palette.LeafDark);

            float leafLen = 0.45f * budLen * k;
            float leafW = 15f * k;
            for (int side = -1; side <= 1; side += 2)
            {
                float rad = 35f * Mathf.Deg2Rad;
                var dir = new Vector2(side * Mathf.Sin(rad), Mathf.Cos(rad));
                var foot = new Vector2((48f + (side * 4f)) * k, 13f * k);
                DrawLeafShape(pc, foot - (dir * (1.5f * k)), dir, leafLen + (3f * k), leafW + (3f * k), BroteWrapEdge, 1f);
                DrawLeafShape(pc, foot, dir, leafLen, leafW, BroteWrapFill, 1f);
                Vector2 rib = foot + (dir * (0.78f * leafLen));
                pc.FillCapsule(foot.x + (dir.x * 2f * k), foot.y + (dir.y * 2f * k), rib.x, rib.y, 1.1f * k, BroteWrapVein);
            }
        }
    }
}
