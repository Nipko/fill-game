#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>Piezas del rig de Capi al remo (D-069, bloque M2-3; propuesta del Mapa v2 §5.1-§5.3).</summary>
    public enum CapiRigPiece
    {
        /// <summary>El casco de la canoa de 60 dp, con su borda y la línea de agua.</summary>
        Hull = 0,

        /// <summary>Capi sentado de perfil (cabeza y cuerpo, el mismo dibujo de juguete de D-058 que el banner).</summary>
        Body = 1,

        /// <summary>El remo: la pala abajo y el mango arriba.</summary>
        Paddle = 2,

        /// <summary>La pata delantera (del hombro a la mano), que gira sobre el hombro.</summary>
        Paw = 3,

        /// <summary>El vidrio de una cuia de la carga atada a popa.</summary>
        CargoGlass = 4,

        /// <summary>La máscara blanca del agua de una cuia de la carga (se tiñe con el agua de su mundo).</summary>
        CargoWater = 5,

        /// <summary>La burbuja de las emociones, sin texto (P3 (a)).</summary>
        Bubble = 6,

        /// <summary>♪ remando.</summary>
        Note = 7,

        /// <summary>♥ al unirse alguien (M2-4).</summary>
        Heart = 8,

        /// <summary>! en el Encuentro, al cambiar el agua o en una boca.</summary>
        Bang = 9,

        /// <summary>? ante la niebla del fin de contenido.</summary>
        Question = 10,

        /// <summary>Salpicaduras de los rápidos (el remo fuerte).</summary>
        Splash = 11,
    }

    /// <summary>
    /// El rig de Capi al remo por código (D-069, M2-3; D-052: el arte por código es el final; D-058: «juguete brillante»), pintado en
    /// un atlas de <see cref="CapiRigAtlasWidthPx"/> × <see cref="CapiRigAtlasHeightPx"/> a <see cref="UiPxPerDp"/> px por dp, sin
    /// mipmaps (la canoa se pinta a 40-60 dp: ×0,67-1). Las medidas de las piezas van en dp de diseño de la canoa de
    /// <see cref="CapiCanoeDesignWidthDp"/> × <see cref="CapiCanoeDesignHeightDp"/> (propuesta §5.1: 60 × 44); la vista
    /// (<c>CapiRigView</c>) las coloca y las anima. Capi es el mismo dibujo que el del banner (<see cref="DrawCapi"/>), con la cabeza
    /// 1,4 veces la de la canoa de la 1.0 (cuadrado de <see cref="CapiRigBodyDp"/> dp frente a 20).
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del atlas del rig (px).</summary>
        public const int CapiRigAtlasWidthPx = 512;

        /// <summary>Alto del atlas del rig (px).</summary>
        public const int CapiRigAtlasHeightPx = 256;

        /// <summary>Ancho de la canoa de Capi del Mapa v2 en la vista Mundo (dp; propuesta §5.1).</summary>
        public const float CapiCanoeDesignWidthDp = 60f;

        /// <summary>Alto de la canoa de Capi del Mapa v2 en la vista Mundo (dp; propuesta §5.1).</summary>
        public const float CapiCanoeDesignHeightDp = 44f;

        /// <summary>Lado del cuadrado de Capi en la canoa de 60 dp (dp): 28 frente a los 20 de la 1.0 (la cabeza, 1,4 veces).</summary>
        public const float CapiRigBodyDp = 28f;

        // Celdas del atlas (px; x, y desde abajo a la izquierda, ancho y alto), en el orden de CapiRigPiece, con 2 px de aire.
        private static readonly RectInt[] CapiRigCells =
        {
            new RectInt(2, 2, 180, 60), // Hull: 60 × 20 dp
            new RectInt(186, 2, 84, 84), // Body: 28 × 28 dp
            new RectInt(274, 2, 18, 96), // Paddle: 6 × 32 dp
            new RectInt(296, 2, 42, 24), // Paw: 14 × 8 dp
            new RectInt(342, 2, 18, 30), // CargoGlass: 6 × 10 dp
            new RectInt(364, 2, 18, 30), // CargoWater
            new RectInt(386, 2, 72, 66), // Bubble: 24 × 22 dp
            new RectInt(462, 2, 36, 36), // Note: 12 × 12 dp
            new RectInt(462, 42, 36, 36), // Heart
            new RectInt(462, 82, 36, 36), // Bang
            new RectInt(462, 122, 36, 36), // Question
            new RectInt(296, 30, 36, 24), // Splash: 12 × 8 dp
        };

        /// <summary>Piezas del rig.</summary>
        public const int CapiRigPieceCount = 12;

        /// <summary>Celda de la pieza <paramref name="piece"/> en el atlas (px).</summary>
        public static RectInt CapiRigCell(CapiRigPiece piece)
        {
            return CapiRigCells[(int)piece];
        }

        /// <summary>Pivote de la pieza (fracción de su celda: x desde la izquierda, y desde abajo) con la que la vista la gira.</summary>
        public static Vector2 CapiRigPivot(CapiRigPiece piece)
        {
            switch (piece)
            {
                case CapiRigPiece.Body:
                    return new Vector2(0.36f, 0.12f); // la grupa sentada: se inclina sobre ella
                case CapiRigPiece.Paddle:
                    return new Vector2(0.5f, 0.72f); // donde lo agarra la mano
                case CapiRigPiece.Paw:
                    return new Vector2(4f / 14f, 0.5f); // el hombro
                case CapiRigPiece.Bubble:
                    return new Vector2(0.3f, 0f); // la punta del rabito
                case CapiRigPiece.CargoGlass:
                case CapiRigPiece.CargoWater:
                case CapiRigPiece.Hull:
                case CapiRigPiece.Splash:
                    return new Vector2(0.5f, 0f);
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }

        /// <summary>
        /// Pinta todas las piezas del rig en <paramref name="pc"/> (de <see cref="CapiRigAtlasWidthPx"/> ×
        /// <see cref="CapiRigAtlasHeightPx"/>, transparente). Asigna: solo al abrir el Mapa v2.
        /// </summary>
        internal static void PaintCapiRigAtlas(PixelCanvas pc)
        {
            const float k = UiPxPerDp;
            Color ink = new Color(Palette.Groove.r, Palette.Groove.g, Palette.Groove.b, 0.85f);
            PaintRigHull(pc, CapiRigCells[(int)CapiRigPiece.Hull], k, ink);

            RectInt body = CapiRigCells[(int)CapiRigPiece.Body];
            DrawCapi(pc, body.x, body.y, body.width);

            PaintRigPaddle(pc, CapiRigCells[(int)CapiRigPiece.Paddle], k, ink);
            PaintRigPaw(pc, CapiRigCells[(int)CapiRigPiece.Paw], k);
            PaintRigCargo(pc, CapiRigCells[(int)CapiRigPiece.CargoGlass], k, ink, false);
            PaintRigCargo(pc, CapiRigCells[(int)CapiRigPiece.CargoWater], k, ink, true);
            PaintRigBubble(pc, CapiRigCells[(int)CapiRigPiece.Bubble], k, ink);
            PaintRigNote(pc, CapiRigCells[(int)CapiRigPiece.Note], k);
            PaintRigHeart(pc, CapiRigCells[(int)CapiRigPiece.Heart], k);
            PaintRigBang(pc, CapiRigCells[(int)CapiRigPiece.Bang], k);
            PaintRigQuestion(pc, CapiRigCells[(int)CapiRigPiece.Question], k);
            PaintRigSplash(pc, CapiRigCells[(int)CapiRigPiece.Splash], k);
        }

        // El casco (60 × 20 dp, proa a la derecha): 56 dp de eslora, los extremos levantados y la proa más alta, la borda clara,
        // una línea de tablón, el barniz de juguete arriba y la línea de agua de vidrio por delante. Como el de la canoa de la 1.0
        // (BuildMapCanoe), a 1,47 veces.
        private static void PaintRigHull(PixelCanvas pc, RectInt cell, float k, Color ink)
        {
            const float half = 28f;
            const float mid = 30f;
            const float keel = 9.5f;
            for (int y = 0; y < cell.height; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < cell.width; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float xn = (xd - mid) / half;
                    float ax = Mathf.Min(1f, Mathf.Abs(xn));
                    float rise = (5.5f * Mathf.Pow(ax, 5f)) + (xn > 0f ? 3f * ax * ax * ax : 0f);
                    float top = keel + 3.5f + rise;
                    float bottom = keel - (7f * Mathf.Pow(Mathf.Max(0f, 1f - (xn * xn)), 0.7f));
                    float d = Mathf.Max(Mathf.Max(bottom - yd, yd - top), (Mathf.Abs(xn) - 1f) * half);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov > 0f)
                    {
                        pc.Blend(cell.x + x, cell.y + y, ink, cov);
                        float inside = PixelCanvas.Coverage((d + 1.2f) * k);
                        if (inside > 0f)
                        {
                            float t = Mathf.Clamp01((top - yd) / Mathf.Max(0.01f, top - bottom));
                            Color c = Color.Lerp(Palette.DockLight, Palette.DockShadow, t);
                            if (top - yd < 2.2f)
                            {
                                c = Palette.ShelfEdge;
                            }
                            else if (Mathf.Abs(yd - (bottom + ((top - bottom) * 0.55f))) < 0.45f)
                            {
                                c = ColorMath.Darken(c, 0.25f);
                            }
                            else if (top - yd < 4f)
                            {
                                c = Color.Lerp(c, Color.white, 0.12f * (1f - Mathf.Abs(xn))); // barniz de juguete (D-058)
                            }

                            c.a = 1f;
                            pc.Blend(cell.x + x, cell.y + y, c, inside);
                        }
                    }

                    if (xd > 1f && xd < (cell.width / k) - 1f)
                    {
                        float yw = 3.4f + (0.8f * Mathf.Sin((xd * 0.7f) + 0.6f));
                        float dw = Mathf.Abs(yd - yw) - 1.1f;
                        pc.Blend(cell.x + x, cell.y + y, new Color(Palette.GlassRim.r, Palette.GlassRim.g, Palette.GlassRim.b, 0.9f), PixelCanvas.Coverage(dw * k));
                    }
                }
            }
        }

        // El remo (6 × 32 dp): el mango de madera clara y la pala abajo, con su canto.
        private static void PaintRigPaddle(PixelCanvas pc, RectInt cell, float k, Color ink)
        {
            float cx = cell.x + (3f * k);
            float y0 = cell.y;
            pc.FillCapsule(cx, y0 + (9f * k), cx, y0 + (30.5f * k), 1.1f * k, ink);
            pc.FillCapsule(cx, y0 + (9f * k), cx, y0 + (30.5f * k), 0.6f * k, Palette.FamilyWood.Light);
            pc.FillEllipseSoft(cx, y0 + (5.6f * k), 2.4f * k, 5f * k, 1f, ink);
            pc.FillEllipseSoft(cx, y0 + (5.6f * k), 1.8f * k, 4.4f * k, 1f, Palette.FamilyWood.Edge);
            pc.FillEllipseSoft(cx - (0.6f * k), y0 + (6.4f * k), 0.5f * k, 2.6f * k, 1f, new Color(1f, 1f, 1f, 0.35f));
        }

        // La pata (14 × 8 dp): el hombro redondo a la izquierda, el brazo y la mano clara a la derecha, con el pelo de Capi.
        private static void PaintRigPaw(PixelCanvas pc, RectInt cell, float k)
        {
            float y = cell.y + (4f * k);
            ToyTones fur = CapiToy.Fur;
            pc.FillCapsule(cell.x + (4f * k), y, cell.x + (11f * k), y, 3.1f * k, fur.Outline);
            pc.FillCapsule(cell.x + (4f * k), y, cell.x + (11f * k), y, 2.4f * k, fur.Mid);
            pc.FillCapsule(cell.x + (4f * k), y + (0.9f * k), cell.x + (10f * k), y + (0.9f * k), 0.9f * k, fur.Light);
            pc.FillEllipseSoft(cell.x + (11.3f * k), y, 2.2f * k, 2.4f * k, 1f, CapiToy.Palm.Outline);
            pc.FillEllipseSoft(cell.x + (11.3f * k), y, 1.7f * k, 1.9f * k, 1f, CapiToy.Palm.Mid);
        }

        // Una cuia de la carga (6 × 10 dp): el vidrio con su borde de fibra o, con water, la máscara blanca de su agua.
        private static void PaintRigCargo(PixelCanvas pc, RectInt cell, float k, Color ink, bool water)
        {
            float cx = cell.x + (3f * k);
            float y0 = cell.y;
            if (water)
            {
                pc.FillRoundedRect(cx - (1.9f * k), y0 + (1.1f * k), cx + (1.9f * k), y0 + (6.2f * k), 1.4f * k, Color.white);
                return;
            }

            pc.FillRoundedRect(cx - (2.7f * k), y0 + (0.3f * k), cx + (2.7f * k), y0 + (7.8f * k), 2f * k, ink);
            pc.FillRoundedRect(cx - (2.1f * k), y0 + (0.8f * k), cx + (2.1f * k), y0 + (7.3f * k), 1.6f * k,
                new Color(Palette.GlassRim.r, Palette.GlassRim.g, Palette.GlassRim.b, 0.35f));
            pc.FillRoundedRect(cx - (1.5f * k), y0 + (7.1f * k), cx + (1.5f * k), y0 + (9.6f * k), 0.6f * k, ink);
            pc.FillRoundedRect(cx - (1.0f * k), y0 + (7.4f * k), cx + (1.0f * k), y0 + (9.2f * k), 0.4f * k, Palette.FiberBase);
            pc.FillCapsule(cx - (1.2f * k), y0 + (1.8f * k), cx - (1.2f * k), y0 + (5.8f * k), 0.4f * k, new Color(1f, 1f, 1f, 0.75f));
        }

        // La burbuja (24 × 22 dp): crema con el contorno de tinta y el rabito abajo a la izquierda, hacia la cabeza de Capi.
        private static void PaintRigBubble(PixelCanvas pc, RectInt cell, float k, Color ink)
        {
            float x0 = cell.x;
            float y0 = cell.y;
            pc.FillTriangle(new Vector2(x0 + (5.5f * k), y0 + (8f * k)), new Vector2(x0 + (12f * k), y0 + (8f * k)), new Vector2(x0 + (7.2f * k), y0 + (0.6f * k)), ink);
            pc.FillRoundedRect(x0 + (0.8f * k), y0 + (5.2f * k), x0 + (23.2f * k), y0 + (21.2f * k), 6.5f * k, ink);
            pc.FillTriangle(new Vector2(x0 + (6.4f * k), y0 + (7.5f * k)), new Vector2(x0 + (11.2f * k), y0 + (7.5f * k)), new Vector2(x0 + (7.4f * k), y0 + (2f * k)), Palette.Cream);
            pc.FillRoundedRect(x0 + (1.8f * k), y0 + (6.2f * k), x0 + (22.2f * k), y0 + (20.2f * k), 5.6f * k, Palette.Cream);
            pc.FillRoundedRect(x0 + (4f * k), y0 + (16.6f * k), x0 + (14f * k), y0 + (18.6f * k), 1f * k, new Color(1f, 1f, 1f, 0.6f));
        }

        // ♪ (12 × 12 dp), en la tinta de la madera.
        private static void PaintRigNote(PixelCanvas pc, RectInt cell, float k)
        {
            Color c = Palette.WoodDark;
            float x0 = cell.x;
            float y0 = cell.y;
            pc.FillEllipseSoft(x0 + (4.2f * k), y0 + (3.2f * k), 2.6f * k, 2f * k, 1f, c);
            pc.FillCapsule(x0 + (6.3f * k), y0 + (3.4f * k), x0 + (6.3f * k), y0 + (10.6f * k), 0.75f * k, c);
            pc.FillCapsule(x0 + (6.3f * k), y0 + (10.6f * k), x0 + (9.8f * k), y0 + (8.6f * k), 0.8f * k, c);
        }

        // ♥ (12 × 12 dp), rojo de pluma.
        private static void PaintRigHeart(PixelCanvas pc, RectInt cell, float k)
        {
            Color c = Palette.FeatherRed;
            float x0 = cell.x;
            float y0 = cell.y;
            pc.FillCircle(x0 + (4.2f * k), y0 + (7.4f * k), 2.9f * k, c);
            pc.FillCircle(x0 + (7.8f * k), y0 + (7.4f * k), 2.9f * k, c);
            pc.FillTriangle(new Vector2(x0 + (1.5f * k), y0 + (6.6f * k)), new Vector2(x0 + (10.5f * k), y0 + (6.6f * k)), new Vector2(x0 + (6f * k), y0 + (1.2f * k)), c);
            pc.FillCircle(x0 + (3.6f * k), y0 + (8.4f * k), 0.8f * k, new Color(1f, 1f, 1f, 0.6f));
        }

        // ! (12 × 12 dp), ámbar con contorno: se lee por la forma, no solo por el color (§7.3).
        private static void PaintRigBang(PixelCanvas pc, RectInt cell, float k)
        {
            float x = cell.x + (6f * k);
            float y0 = cell.y;
            pc.FillCapsule(x, y0 + (5.2f * k), x, y0 + (10.4f * k), 1.6f * k, Palette.WoodDark);
            pc.FillCapsule(x, y0 + (5.2f * k), x, y0 + (10.4f * k), 1.0f * k, Palette.UiAmber);
            pc.FillCircle(x, y0 + (2f * k), 1.6f * k, Palette.WoodDark);
            pc.FillCircle(x, y0 + (2f * k), 1.0f * k, Palette.UiAmber);
        }

        // ? (12 × 12 dp), azul de pluma.
        private static void PaintRigQuestion(PixelCanvas pc, RectInt cell, float k)
        {
            float x0 = cell.x;
            float y0 = cell.y;
            Color c = Palette.FeatherBlue;
            pc.StrokeArc(x0 + (6f * k), y0 + (8f * k), 2.6f * k, 1.7f * k, -60f, 180f, c);
            pc.FillCapsule(x0 + (7.3f * k), y0 + (5.8f * k), x0 + (6f * k), y0 + (4.4f * k), 0.85f * k, c);
            pc.FillCircle(x0 + (6f * k), y0 + (1.6f * k), 1.0f * k, c);
        }

        // Salpicaduras (12 × 8 dp): tres gotas de vidrio en abanico.
        private static void PaintRigSplash(PixelCanvas pc, RectInt cell, float k)
        {
            Color c = new Color(0.94f, 0.99f, 1f, 0.95f);
            float x0 = cell.x;
            float y0 = cell.y;
            pc.FillEllipseSoft(x0 + (3f * k), y0 + (4.5f * k), 1.3f * k, 1.8f * k, 1f, c);
            pc.FillEllipseSoft(x0 + (6.5f * k), y0 + (5.8f * k), 1.2f * k, 1.6f * k, 1f, c);
            pc.FillEllipseSoft(x0 + (9.6f * k), y0 + (3.8f * k), 1.1f * k, 1.5f * k, 1f, c);
            pc.FillEllipseSoft(x0 + (6f * k), y0 + (1.6f * k), 4.6f * k, 1.0f * k, 1f, new Color(1f, 1f, 1f, 0.6f));
        }
    }
}
