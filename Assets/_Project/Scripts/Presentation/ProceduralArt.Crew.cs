#nullable enable
using AF.Levels;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>Piezas de la tripulación del Mapa v2 (D-069, bloque M2-4; propuesta del Mapa v2 §6.2).</summary>
    public enum CrewPiece
    {
        /// <summary>El tucán posado, de perfil hacia la proa (sin el pico).</summary>
        ToucanBody = 0,

        /// <summary>La mandíbula de arriba del pico, que gira sobre su base (abre el pico en las llegadas).</summary>
        ToucanBeakUpper = 1,

        /// <summary>La mandíbula de abajo.</summary>
        ToucanBeakLower = 2,

        /// <summary>El ala, que gira sobre el hombro (aletea al llegar).</summary>
        ToucanWing = 3,

        /// <summary>El tucuxi: la silueta de Rosa con su paleta gris (§6.2: «el rig de Rosa con otra paleta»).</summary>
        Tucuxi = 4,

        /// <summary>El árbol de la orilla del cameo de los miquitos, con su rama.</summary>
        Tree = 5,

        /// <summary>El cuerpo del miquito del cameo, con la cola colgando (con la paleta de la especie del tramo).</summary>
        MonkeyBody = 6,

        /// <summary>La cabeza del miquito, con la cara.</summary>
        MonkeyHead = 7,

        /// <summary>El brazo que saluda, que gira sobre el hombro.</summary>
        MonkeyArm = 8,
    }

    /// <summary>
    /// La tripulación del Mapa v2 por código (D-069, M2-4; D-052: el arte por código es el final; D-058: «juguete brillante»): el tucán
    /// en piezas (cuerpo, las dos mandíbulas y el ala), el tucuxi (la silueta de Rosa con una paleta gris de dorso oscuro y vientre rosado, distinta de la gris azulada del bufeo), el árbol de la orilla y el miquito
    /// del cameo con la paleta de su especie (07 §6.4). Todo en un atlas de <see cref="CrewAtlasWidthPx"/> ×
    /// <see cref="CrewAtlasHeightPx"/> a <see cref="UiPxPerDp"/> px por dp sin mipmaps, que pinta <c>MapCrewArt</c> al abrir el Mapa.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del atlas de la tripulación (px).</summary>
        public const int CrewAtlasWidthPx = 256;

        /// <summary>Alto del atlas de la tripulación (px).</summary>
        public const int CrewAtlasHeightPx = 128;

        /// <summary>Piezas del atlas.</summary>
        public const int CrewPieceCount = 9;

        /// <summary>Ancho de la pieza del tucán sin el pico (dp de diseño; el tucán entero, con el pico, mide unos 18 dp de largo).</summary>
        public const float ToucanBodyDp = 13f;

        /// <summary>Donde nace el pico en la pieza del cuerpo del tucán (dp desde abajo a la izquierda).</summary>
        public static readonly Vector2 ToucanBeakHinge = new Vector2(11.2f, 9.6f);

        /// <summary>Donde va el hombro del ala en la pieza del cuerpo del tucán (dp).</summary>
        public static readonly Vector2 ToucanShoulder = new Vector2(6.6f, 7.4f);

        /// <summary>Ancho del tucuxi en el atlas (dp; se pinta a su tamaño de la vista, más pequeño).</summary>
        public const float CrewTucuxiAtlasDp = 26f;

        /// <summary>Donde se apoya el miquito en la rama del árbol (dp desde abajo a la izquierda de la pieza del árbol).</summary>
        public static readonly Vector2 TreePerch = new Vector2(22.5f, 15.6f);

        /// <summary>Donde va el cuello en la pieza del cuerpo del miquito (dp).</summary>
        public static readonly Vector2 MonkeyNeck = new Vector2(5f, 10.4f);

        /// <summary>Donde va el hombro del brazo que saluda en la pieza del cuerpo del miquito (dp).</summary>
        public static readonly Vector2 MonkeyShoulder = new Vector2(6.4f, 8.6f);

        // Celdas (px; x, y desde abajo a la izquierda, ancho y alto), en el orden de CrewPiece, con 2 px de aire.
        private static readonly RectInt[] CrewCells =
        {
            new RectInt(2, 2, 39, 42), // ToucanBody: 13 × 14 dp
            new RectInt(44, 2, 27, 15), // ToucanBeakUpper: 9 × 5 dp
            new RectInt(44, 20, 24, 9), // ToucanBeakLower: 8 × 3 dp
            new RectInt(74, 2, 24, 15), // ToucanWing: 8 × 5 dp
            new RectInt(2, 50, 80, 50), // Tucuxi: 26,7 × 16,7 dp
            new RectInt(160, 2, 90, 102), // Tree: 30 × 34 dp
            new RectInt(86, 50, 30, 36), // MonkeyBody: 10 × 12 dp
            new RectInt(120, 50, 30, 30), // MonkeyHead: 10 × 10 dp
            new RectInt(86, 92, 27, 12), // MonkeyArm: 9 × 4 dp
        };

        // El tucuxi de juguete: gris de dorso oscuro y vientre rosado [P] (la especie: gris arriba y claro, a veces rosado, abajo).
        private static readonly Color TucuxiBody = Palette.Hex(0x9A9EA4);
        private static readonly Color TucuxiBack = Palette.Hex(0x5E646C);
        private static readonly Color TucuxiBelly = Palette.Hex(0xEAD3D3);

        // El tucán de juguete (el tucano-toco de la región [E]): negro con el babero blanco cremoso, el anillo azul del ojo, el pico
        // naranja con la punta negra y la rabadilla roja.
        private static readonly Color ToucanBlack = Palette.Hex(0x23222A);
        private static readonly Color ToucanBib = Palette.Hex(0xFFF1CF);
        private static readonly Color ToucanRing = Palette.Hex(0x3F86D8);
        private static readonly Color ToucanBeak = Palette.Hex(0xF2892A);
        private static readonly Color ToucanBeakLight = Palette.Hex(0xF8C64A);
        private static readonly Color ToucanRump = Palette.Hex(0xD23A2C);
        private static readonly Color ToucanFeet = Palette.Hex(0x6E86A6);

        /// <summary>Celda de la pieza <paramref name="piece"/> en el atlas (px).</summary>
        public static RectInt CrewCell(CrewPiece piece)
        {
            return CrewCells[(int)piece];
        }

        /// <summary>Pivote de la pieza (fracción de su celda: x desde la izquierda, y desde abajo) con el que la vista la coloca y la gira.</summary>
        public static Vector2 CrewPivot(CrewPiece piece)
        {
            switch (piece)
            {
                case CrewPiece.ToucanBody:
                    return new Vector2(0.5f, 0f); // las patas sobre la borda
                case CrewPiece.ToucanBeakUpper:
                    return new Vector2(0.5f / 9f, 1.2f / 5f); // la bisagra, abajo en la base
                case CrewPiece.ToucanBeakLower:
                    return new Vector2(0.5f / 8f, 2.6f / 3f); // la bisagra, arriba en la base
                case CrewPiece.ToucanWing:
                    return new Vector2(1.2f / 8f, 3.6f / 5f); // el hombro
                case CrewPiece.Tucuxi:
                    return new Vector2(0.5f, 0.3f); // como el guía
                case CrewPiece.Tree:
                    return new Vector2(0.5f, 0f); // el pie del tronco
                case CrewPiece.MonkeyBody:
                    return new Vector2(0.5f, 6f / 12f); // donde se sienta en la rama
                case CrewPiece.MonkeyArm:
                    return new Vector2(1f / 9f, 0.5f); // el hombro
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }

        /// <summary>
        /// Pinta las piezas en <paramref name="pc"/> (de <see cref="CrewAtlasWidthPx"/> × <see cref="CrewAtlasHeightPx"/>, transparente):
        /// el tucán, el tucuxi y el árbol siempre; el miquito, con la paleta de <paramref name="monkey"/> (sin ella, en blanco). Asigna:
        /// solo al abrir el Mapa v2 con la tripulación.
        /// </summary>
        internal static void PaintCrewAtlas(PixelCanvas pc, FaunaPalette? monkey)
        {
            const float k = UiPxPerDp;
            PaintToucanBody(pc, CrewCells[(int)CrewPiece.ToucanBody], k);
            PaintToucanBeak(pc, CrewCells[(int)CrewPiece.ToucanBeakUpper], k, true);
            PaintToucanBeak(pc, CrewCells[(int)CrewPiece.ToucanBeakLower], k, false);
            PaintToucanWing(pc, CrewCells[(int)CrewPiece.ToucanWing], k);
            PaintTucuxi(pc, CrewCells[(int)CrewPiece.Tucuxi]);
            PaintCameoTree(pc, CrewCells[(int)CrewPiece.Tree], k);
            if (monkey != null)
            {
                var tones = new FaunaTones(monkey);
                PaintCameoMonkeyBody(pc, CrewCells[(int)CrewPiece.MonkeyBody], k, tones);
                PaintCameoMonkeyHead(pc, CrewCells[(int)CrewPiece.MonkeyHead], k, tones);
                PaintCameoMonkeyArm(pc, CrewCells[(int)CrewPiece.MonkeyArm], k, tones);
            }
        }

        // El tucán (13 × 14 dp, mirando a la derecha): la cola hacia abajo a la izquierda con la rabadilla roja, el cuerpo negro de
        // juguete, el babero crema, la cabeza con el anillo azul y el ojo, y las patas sobre la borda.
        private static void PaintToucanBody(PixelCanvas pc, RectInt cell, float k)
        {
            float x0 = cell.x;
            float y0 = cell.y;
            ToyTones black = ToyShading.Tones(ToucanBlack);
            const float edge = 1.1f;
            ToyCapsule(pc, x0 + (4.4f * k), y0 + (5.4f * k), x0 + (1.6f * k), y0 + (1.4f * k), 1.3f * k, black, 0.5f, edge);
            pc.FillEllipseSoft(x0 + (4.6f * k), y0 + (4.6f * k), 1.3f * k, 1f * k, 1f, ToucanRump);
            ToyEllipse(pc, x0 + (6.2f * k), y0 + (6.6f * k), 3.8f * k, 4.4f * k, black, 0.55f, edge);
            ToyEllipseOn(pc, x0 + (8.6f * k), y0 + (7.6f * k), 2.2f * k, 2.4f * k, ToyShading.Tones(ToucanBib, 0.6f), 0.6f, 0f, 0f,
                x0 + (6.2f * k), y0 + (6.6f * k), 3.8f * k, 4.4f * k, 1f);
            ToyEllipse(pc, x0 + (9.2f * k), y0 + (10.2f * k), 2.6f * k, 2.5f * k, black, 0.6f, edge, 1.5f);
            pc.FillEllipseSoft(x0 + (9.8f * k), y0 + (8.6f * k), 1.5f * k, 0.9f * k, 1f, ToucanBib); // la garganta
            pc.FillCircle(x0 + (9.9f * k), y0 + (10.6f * k), 1.05f * k, ToucanRing);
            DrawToyEye(pc, x0 + (9.95f * k), y0 + (10.6f * k), 0.6f * k, Palette.Hex(0x1A1410));
            pc.FillCapsule(x0 + (5.6f * k), y0 + (2.6f * k), x0 + (5.2f * k), y0 + (0.6f * k), 0.45f * k, ToucanFeet);
            pc.FillCapsule(x0 + (7.2f * k), y0 + (2.6f * k), x0 + (7.6f * k), y0 + (0.6f * k), 0.45f * k, ToucanFeet);
        }

        // Una mandíbula del pico (upper: 9 × 5 dp, la grande y curva; si no, 8 × 3 dp), de la base a la izquierda a la punta negra.
        private static void PaintToucanBeak(PixelCanvas pc, RectInt cell, float k, bool upper)
        {
            float x0 = cell.x;
            float y0 = cell.y;
            Color ink = ColorMath.WithAlpha(ToyShading.Tones(ToucanBeak).Outline, 0.9f);
            if (upper)
            {
                var a = new Vector2(x0 + (0.5f * k), y0 + (1.2f * k));
                var b = new Vector2(x0 + (0.5f * k), y0 + (4.4f * k));
                var tip = new Vector2(x0 + (8.6f * k), y0 + (0.8f * k));
                pc.FillTriangle(a - new Vector2(0f, 0.4f * k), b + new Vector2(0f, 0.4f * k), tip + new Vector2(0.4f * k, 0f), ink);
                pc.FillTriangle(a, b, tip, ToucanBeak);
                pc.FillCapsule(a.x + (0.6f * k), a.y + (2.6f * k), tip.x - (2f * k), a.y + (1.2f * k), 0.9f * k, ToucanBeakLight);
                pc.FillCapsule(a.x + (1f * k), b.y - (0.6f * k), tip.x - (3f * k), a.y + (2f * k), 0.35f * k, new Color(1f, 1f, 1f, 0.55f));
                pc.FillEllipseSoft(tip.x - (0.8f * k), tip.y + (0.35f * k), 1.0f * k, 0.6f * k, 1f, ToucanBlack);
                return;
            }

            var la = new Vector2(x0 + (0.5f * k), y0 + (0.9f * k));
            var lb = new Vector2(x0 + (0.5f * k), y0 + (2.6f * k));
            var ltip = new Vector2(x0 + (7.4f * k), y0 + (1.6f * k));
            pc.FillTriangle(la - new Vector2(0f, 0.35f * k), lb + new Vector2(0f, 0.35f * k), ltip + new Vector2(0.4f * k, 0f), ink);
            pc.FillTriangle(la, lb, ltip, ColorMath.Darken(ToucanBeak, 0.08f));
            pc.FillEllipseSoft(ltip.x - (0.7f * k), ltip.y, 0.9f * k, 0.5f * k, 1f, ToucanBlack);
        }

        // El ala (8 × 5 dp): negra de juguete, del hombro a la punta, con un brillo arriba.
        private static void PaintToucanWing(PixelCanvas pc, RectInt cell, float k)
        {
            float x0 = cell.x;
            float y0 = cell.y;
            ToyTones black = ToyShading.Tones(Color.Lerp(ToucanBlack, Color.white, 0.1f));
            ToyCapsule(pc, x0 + (1.6f * k), y0 + (3.4f * k), x0 + (6.6f * k), y0 + (1.6f * k), 1.25f * k, black, 0.7f, 1.0f);
        }

        // El tucuxi (26 dp de ancho): la silueta de Rosa con su paleta, como el bufeo del Mapa (DrawMapBufeo), y el ojo de juguete.
        private static void PaintTucuxi(PixelCanvas pc, RectInt cell)
        {
            int h = cell.height;
            float inv = 1f / h;
            ToyTones body = ToyShading.Tones(TucuxiBody);
            ToyTones back = ToyShading.Tones(TucuxiBack);
            ToyTones belly = ToyShading.Tones(TucuxiBelly, 0.7f);
            float outline = Mathf.Max(1.2f, CapiEdgeDp * h / RosaHeightDp);
            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < cell.width; px++)
                {
                    float x = (px + 0.5f) * inv;
                    float y = (py + 0.5f) * inv;
                    float d = RosaSdf(x, y, out float side, out float b, out float head, out Vector2 spine) * h;
                    if (d > outline + 1f)
                    {
                        continue;
                    }

                    pc.Blend(cell.x + px, cell.y + py, body.Outline, PixelCanvas.Coverage(d - outline));
                    float cov = PixelCanvas.Coverage(d);
                    if (cov > 0f)
                    {
                        pc.Blend(cell.x + px, cell.y + py, RosaColorAt(x, y, side, b, head, spine, body, back, belly), cov);
                    }
                }
            }

            pc.FillCapsule(cell.x + (RosaMouthFrom.x * h), cell.y + (RosaMouthFrom.y * h), cell.x + (RosaMouthTo.x * h), cell.y + (RosaMouthTo.y * h),
                Mathf.Max(0.6f, 0.012f * h), ColorMath.WithAlpha(body.Outline, 0.55f));
            DrawToyEye(pc, cell.x + (RosaEyeAt.x * h), cell.y + (RosaEyeAt.y * h), Mathf.Max(1.1f, 0.0275f * h), BufeoIris);
        }

        // El árbol de la orilla (30 × 34 dp): el tronco, la rama a la derecha donde se sienta el miquito y la copa de hojas en tres
        // bolas de juguete, con su brillo.
        private static void PaintCameoTree(PixelCanvas pc, RectInt cell, float k)
        {
            float x0 = cell.x;
            float y0 = cell.y;
            ToyTones bark = ToyShading.Tones(Palette.Hex(0x7A5636));
            ToyCapsule(pc, x0 + (15f * k), y0 + (1.2f * k), x0 + (15f * k), y0 + (19f * k), 1.6f * k, bark, 0.35f, 1.0f);
            ToyCapsule(pc, x0 + (15f * k), y0 + (14.4f * k), x0 + (27.5f * k), y0 + (14.8f * k), 0.9f * k, bark, 0.35f, 1.0f);
            ToyTones leaf = ToyShading.Tones(Palette.LeafMid);
            ToyTones leafDark = ToyShading.Tones(Palette.LeafDark);
            ToyEllipse(pc, x0 + (9.5f * k), y0 + (22.5f * k), 7.2f * k, 6.2f * k, leafDark, 0.45f, 1.0f);
            ToyEllipse(pc, x0 + (20f * k), y0 + (22f * k), 7.4f * k, 6.4f * k, leafDark, 0.45f, 1.0f);
            ToyEllipse(pc, x0 + (14.6f * k), y0 + (27f * k), 8.6f * k, 6.6f * k, leaf, 0.55f, 1.0f, 1.5f);
        }

        // El cuerpo del miquito (10 × 12 dp): la cola que cuelga de la rama, el torso de juguete con su color y la pata de apoyo.
        private static void PaintCameoMonkeyBody(PixelCanvas pc, RectInt cell, float k, in FaunaTones t)
        {
            float x0 = cell.x;
            float y0 = cell.y;
            pc.FillCapsule(x0 + (3.2f * k), y0 + (6.4f * k), x0 + (2.2f * k), y0 + (1.0f * k), 0.75f * k, t.Body.Outline);
            pc.FillCapsule(x0 + (3.2f * k), y0 + (6.4f * k), x0 + (2.2f * k), y0 + (1.0f * k), 0.5f * k, t.Body.Mid);
            ToyEllipse(pc, x0 + (5f * k), y0 + (8f * k), 3.2f * k, 3.4f * k, t.Body, FaunaFurGloss, 1.0f);
            ToyCapsule(pc, x0 + (4f * k), y0 + (6.4f * k), x0 + (4.6f * k), y0 + (5.2f * k), 0.9f * k, t.Limbs, FaunaFurGloss, 1.0f);
        }

        // La cabeza del miquito (10 × 10 dp, mirando al jugador): el pelo de la gorra, la cara de su color con los dos ojos y el hocico.
        private static void PaintCameoMonkeyHead(PixelCanvas pc, RectInt cell, float k, in FaunaTones t)
        {
            float cx = cell.x + (5f * k);
            float cy = cell.y + (5f * k);
            ToyEllipse(pc, cx, cy, 3.6f * k, 3.5f * k, t.Cap, FaunaFurGloss, 1.0f);
            ToyEllipseOn(pc, cx, cy - (0.4f * k), 2.6f * k, 2.4f * k, t.Face, FaunaFaceGloss, 0f, 0f, cx, cy, 3.6f * k, 3.5f * k, 1f);
            DrawToyEye(pc, cx - (1.1f * k), cy + (0.2f * k), 0.55f * k, FaunaDarkIris);
            DrawToyEye(pc, cx + (1.1f * k), cy + (0.2f * k), 0.55f * k, FaunaDarkIris);
            pc.FillEllipseSoft(cx, cy - (1.4f * k), 1.1f * k, 0.7f * k, 1f, t.Muzzle.Mid);
        }

        // El brazo que saluda (9 × 4 dp): del hombro a la izquierda a la mano, más oscura.
        private static void PaintCameoMonkeyArm(PixelCanvas pc, RectInt cell, float k, in FaunaTones t)
        {
            float y = cell.y + (2f * k);
            ToyCapsule(pc, cell.x + (1f * k), y, cell.x + (6.6f * k), y, 0.95f * k, t.Limbs, FaunaFurGloss, 1.0f);
            ToyEllipse(pc, cell.x + (7.4f * k), y, 1.1f * k, 1.1f * k, ToyShading.Tones(ColorMath.Darken(t.Limbs.Mid, 0.22f)), FaunaFurGloss, 1.0f);
        }
    }
}
