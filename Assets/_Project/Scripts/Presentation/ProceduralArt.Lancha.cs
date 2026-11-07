#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Lancha (M11, 1.1; 02 §9.1 M11, regla 5: «un muelle; la lancha se acerca con cada cuia vacía y
    /// atraca»; D-068; arte por código, D-052): el casco de madera de la lancha de carga que lleva la cuia (<see cref="LanchaHull"/>,
    /// con su motorcito fuera borda), el muelle de dos pilotes con su tablón (<see cref="LanchaDock"/>), detrás, y el dibujo del
    /// icono de la mecánica (32 dp y el grande del estreno).
    /// </summary>
    /// <remarks>
    /// Color (direccion-visual-v2 §2.4, regla 1): el casco <see cref="LanchaHullColor"/> (#6E5A4A, ΔE00 14,4 del tinte más
    /// cercano) con la regala <see cref="LanchaTrim"/> (#3E3A36, 16,7); el muelle, con la madera del jirau
    /// (<see cref="JirauWood"/> y <see cref="JirauShade"/>). Perezosos, a <see cref="MechanicPxPerU"/> px por u; los sustituye el
    /// arte del artista.
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del casco (u): algo más que la cuia, con la proa a la derecha.</summary>
        public const float LanchaHullWidthU = 2.8f;

        /// <summary>Alto del casco (u).</summary>
        public const float LanchaHullHeightU = 0.95f;

        /// <summary>Altura del pivote del casco sobre su canto de abajo, en fracción del alto: la línea de agua.</summary>
        public const float LanchaHullPivotY = 0.38f;

        /// <summary>Ancho del muelle (u): los dos pilotes, a los lados de la ranura.</summary>
        public const float LanchaDockWidthU = 3.0f;

        /// <summary>Alto del muelle (u), desde el pie.</summary>
        public const float LanchaDockHeightU = 2.1f;

        /// <summary>Casco de la lancha [P]: #6E5A4A.</summary>
        public static readonly Color LanchaHullColor = Palette.Hex(0x6E5A4A);

        /// <summary>Regala y motor de la lancha [P]: #3E3A36.</summary>
        public static readonly Color LanchaTrim = Palette.Hex(0x3E3A36);

        private Sprite? _lanchaHull;
        private Sprite? _lanchaDock;

        /// <summary>
        /// Casco de la lancha (<see cref="LanchaHullWidthU"/> × <see cref="LanchaHullHeightU"/> u, pivote en la línea de agua del
        /// centro, <see cref="LanchaHullPivotY"/>), coloreado: tablas con su regala, proa alzada a la derecha y el motorcito a popa.
        /// </summary>
        internal Sprite LanchaHull => _lanchaHull != null ? _lanchaHull : (_lanchaHull = BuildLanchaHull());

        /// <summary>
        /// Muelle (<see cref="LanchaDockWidthU"/> × <see cref="LanchaDockHeightU"/> u, pivote en el centro del pie), coloreado: dos
        /// pilotes con su sombrero y el tablón entre ellos, a la altura de la regala.
        /// </summary>
        internal Sprite LanchaDock => _lanchaDock != null ? _lanchaDock : (_lanchaDock = BuildLanchaDock());

        private Sprite BuildLanchaHull()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(LanchaHullWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(LanchaHullHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            DrawLanchaHull(pc, 0f, 0f, w, h, k);
            return MakeSprite(pc, "mech_lancha_hull", new Vector2(0.5f, LanchaHullPivotY), Vector4.zero);
        }

        private Sprite BuildLanchaDock()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(LanchaDockWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(LanchaDockHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            DrawLanchaDock(pc, 0f, 0f, w, h, k);
            return MakeSprite(pc, "mech_lancha_dock", new Vector2(0.5f, 0f), Vector4.zero);
        }

        // Casco en la caja (x0, y0, w, h) px: fondo de tablas, regala oscura, proa alzada a la derecha, motor a la izquierda.
        private static void DrawLanchaHull(PixelCanvas pc, float x0, float y0, float w, float h, float k)
        {
            float bottom = y0 + (h * 0.08f);
            float top = y0 + (h * 0.62f);
            float sternX = x0 + (w * 0.10f);
            float bowX = x0 + (w * 0.97f);

            // Motor fuera borda a popa (detrás del casco).
            pc.FillRoundedRect(x0 + (w * 0.01f), y0 + (h * 0.30f), x0 + (w * 0.11f), y0 + (h * 0.92f), 5f * k, LanchaTrim);
            pc.FillCapsule(x0 + (w * 0.06f), y0 + (h * 0.32f), x0 + (w * 0.06f), y0 + (h * 0.02f), 2.4f * k, LanchaTrim);

            // Casco: el cuerpo y la proa en cuña.
            pc.FillRoundedRect(sternX, bottom, x0 + (w * 0.80f), top, 10f * k, ColorMath.Darken(LanchaHullColor, 0.25f));
            pc.FillTriangle(new Vector2(x0 + (w * 0.78f), bottom), new Vector2(bowX, top + (h * 0.18f)), new Vector2(x0 + (w * 0.78f), top),
                ColorMath.Darken(LanchaHullColor, 0.25f));
            pc.FillRoundedRect(sternX + (2.5f * k), bottom + (2.5f * k), x0 + (w * 0.80f), top - (2.5f * k), 8f * k, LanchaHullColor);
            pc.FillTriangle(new Vector2(x0 + (w * 0.78f), bottom + (2.5f * k)), new Vector2(bowX - (5f * k), top + (h * 0.14f)),
                new Vector2(x0 + (w * 0.78f), top - (2.5f * k)), LanchaHullColor);

            // Tablas (dos juntas claras) y regala.
            for (int i = 1; i <= 2; i++)
            {
                float y = bottom + ((top - bottom) * i / 3f);
                pc.FillCapsule(sternX + (6f * k), y, x0 + (w * 0.84f), y + (i * 2f * k), 1.1f * k, ColorMath.Lighten(LanchaHullColor, 0.18f));
            }

            pc.FillCapsule(sternX, top, bowX - (3f * k), top + (h * 0.18f), 3.4f * k, LanchaTrim);
            pc.FillCapsule(sternX + (4f * k), top + (1.5f * k), x0 + (w * 0.60f), top + (2.5f * k), 1.2f * k, ColorMath.WithAlpha(Palette.Glint, 0.5f));

            // Línea de agua.
            pc.FillEllipseSoft(x0 + (w * 0.5f), bottom, w * 0.46f, 3.5f * k, 2.5f * k, ColorMath.WithAlpha(Palette.WaterLine, 0.55f));
        }

        // Muelle en la caja (x0, y0, w, h) px: dos pilotes con su sombrero y el tablón entre ellos.
        private static void DrawLanchaDock(PixelCanvas pc, float x0, float y0, float w, float h, float k)
        {
            float plankY = y0 + (h * 0.30f);
            for (int side = -1; side <= 1; side += 2)
            {
                float px = x0 + (w * 0.5f) + (side * w * 0.42f);
                pc.FillRoundedRect(px - (9f * k), y0, px + (9f * k), y0 + (h * 0.92f), 5f * k, JirauShade);
                pc.FillRoundedRect(px - (6.5f * k), y0 + (2f * k), px + (6.5f * k), y0 + (h * 0.90f), 4f * k, JirauWood);
                pc.FillEllipseSoft(px, y0 + (h * 0.92f), 11f * k, 4.5f * k, 1.2f * k, JirauShade);
                pc.FillCapsule(px - (2f * k), y0 + (h * 0.15f), px - (2f * k), y0 + (h * 0.80f), 1f * k, ColorMath.WithAlpha(Palette.Glint, 0.35f));
            }

            pc.FillRoundedRect(x0 + (w * 0.05f), plankY - (8f * k), x0 + (w * 0.95f), plankY + (8f * k), 4f * k, JirauShade);
            pc.FillRoundedRect(x0 + (w * 0.05f) + (2f * k), plankY - (5.5f * k), x0 + (w * 0.95f) - (2f * k), plankY + (6f * k), 3f * k, JirauWood);
        }

        // Icono de la mecánica en un lienzo cuadrado de s px (k = s / 96): la lancha con una cuia pequeña a bordo, junto a un
        // pilote del muelle. Sin tintes de fruta (07 §3, regla 6).
        private static void DrawLanchaGlyph(PixelCanvas pc, int s)
        {
            float k = s / 96f;
            pc.FillRoundedRect(76f * k, 6f * k, 90f * k, 80f * k, 4f * k, JirauShade);
            pc.FillRoundedRect(78.5f * k, 8f * k, 87.5f * k, 78f * k, 3f * k, JirauWood);
            pc.FillRoundedRect(36f * k, 34f * k, 58f * k, 66f * k, 6f * k, ColorMath.Darken(Palette.GlassTint, 0.35f));
            pc.FillRoundedRect(38.5f * k, 36f * k, 55.5f * k, 63f * k, 4.5f * k, ColorMath.WithAlpha(Palette.GlassTint, 0.9f));
            DrawLanchaHull(pc, 4f * k, 12f * k, 74f * k, 30f * k, k * 0.7f);
            pc.FillCapsule(72f * k, 34f * k, 80f * k, 58f * k, 1.6f * k, Palette.FiberBase); // el cabo al pilote
        }
    }
}
