#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Flor de vitória-régia (variante de la Liana con fruta de la 1.0, estreno en el 403; 02 §9.1
    /// «Variantes» y R10 d; propuesta de D-007 b.3: «Capullo blanco → rosa → abierto. Solo la flor: la hoja redonda es la Hoja
    /// escudo»; tarea VA1b): el pétalo (<see cref="FlorPetal"/>, en grises claros para que lo tiña el blanco o el rosa de la flor:
    /// el mismo sprite hace el capullo cerrado, la flor entreabierta y la abierta) y el cáliz espinoso que tapa la boca
    /// (<see cref="FlorSepals"/>, ya coloreado, del vino de los sépalos de la vitória-régia, para que no se lea como el cáliz
    /// verde del Brote). Aquí va también el dibujo del icono de la mecánica (<see cref="MechanicIcon"/> de 32 dp y el grande del
    /// estreno, <see cref="CardHeroIcon"/>): la Flor cerrada del tablero, blanca (<see cref="FlorWhite"/>) y sin rosa, sobre su
    /// cáliz de vino, con dos discos neutros a los lados (<see cref="FlorIconKeyFill"/> con filo de 1 dp en
    /// <see cref="FlorIconKeyEdge"/>) por sus dos llaves en lógica Y (b.3); la fruta concreta la pone el nivel (revisión de VA1b).
    /// </summary>
    /// <remarks>
    /// <para>Color (direccion-visual-v2 §2.4, regla 1): el blanco de la flor no es el blanco puro, que se confundiría con la
    /// guanábana (#FAFAF7), sino <see cref="FlorWhite"/>, un blanco verdoso a ΔE00 10,7 de ella; el rosa de la flor abierta,
    /// <see cref="FlorPink"/>, queda a ΔE00 20,5 de la bacaba y a 19,7 del camu camu; el cáliz, <see cref="FlorSepal"/>, a
    /// ΔE00 19,8 del cacao. Sin tintes de fruta en el icono (07 §3, regla 6). Las frutas llave las pinta el overlay con la muestra de
    /// su dosis (<see cref="HojaAtadaSwatch"/>), que lleva su tinte a propósito.</para>
    /// <para>Perezosos: se generan la primera vez que se piden (el overlay los pide al unirse a la cuia) y tras
    /// <see cref="Dispose"/> se vuelven a generar. A <see cref="MechanicPxPerU"/> px por u. Lo sustituye el arte del artista; el
    /// nombre «Flor de vitória-régia» está pendiente del asesor cultural (glosario §6.5).</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del sprite de un pétalo (u).</summary>
        public const float FlorPetalWidthU = 0.46f;

        /// <summary>Alto del sprite de un pétalo (u), con la punta arriba y el pivote en el pie.</summary>
        public const float FlorPetalHeightU = 1.0f;

        /// <summary>Ancho del cáliz (u): tapa el borde de fibra de 1,10 u (la boca), como el del Brote.</summary>
        public const float FlorSepalsWidthU = 1.32f;

        /// <summary>Alto del sprite del cáliz (u).</summary>
        public const float FlorSepalsHeightU = 0.56f;

        /// <summary>Blanco de la flor cerrada y entreabierta (b.3: «blanca el primer día») [P]: #D8E2CE, a ΔE00 10,7 de la guanábana.</summary>
        public static readonly Color FlorWhite = Palette.Hex(0xD8E2CE);

        /// <summary>Rosa de la flor abierta (b.3: «rosa rojiza el segundo») [P]: #C9407A, a ΔE00 ≥ 19 de los 14 tintes.</summary>
        public static readonly Color FlorPink = Palette.Hex(0xC9407A);

        /// <summary>Cáliz espinoso de la flor [P]: vino oscuro #5A2E3E, a ΔE00 19,8 del cacao (el tinte más cercano).</summary>
        public static readonly Color FlorSepal = Palette.Hex(0x5A2E3E);

        /// <summary>Relleno de los dos discos neutros de las llaves en el icono [P]: #EADBC0, sin tinte de fruta (07 §3, regla 6).</summary>
        public static readonly Color FlorIconKeyFill = Palette.Hex(0xEADBC0);

        /// <summary>Filo de 1 dp de los discos de las llaves en el icono [P]: #3B2A20, que los separa del crema del medallón.</summary>
        public static readonly Color FlorIconKeyEdge = Palette.Hex(0x3B2A20);

        /// <summary>Diámetro de cada disco de llave en el icono, en fracción del lado del icono [P] (revisión de VA1b: ≈ 28 %).</summary>
        public const float FlorIconKeyDiameter = 0.28f;

        private Sprite? _florPetal;
        private Sprite? _florSepals;

        /// <summary>
        /// Pétalo (<see cref="FlorPetalWidthU"/> × <see cref="FlorPetalHeightU"/> u, pivote en el pie): lanceolado, con contorno
        /// gris, luz arriba a la izquierda y un nervio central suave. En grises claros: lo tiñe <c>Image.color</c> con
        /// <see cref="FlorWhite"/> o <see cref="FlorPink"/>.
        /// </summary>
        internal Sprite FlorPetal => _florPetal != null ? _florPetal : (_florPetal = BuildFlorPetal());

        /// <summary>
        /// Cáliz (<see cref="FlorSepalsWidthU"/> × <see cref="FlorSepalsHeightU"/> u, pivote en el pie), ya coloreado: un collarín
        /// que encaja en la boca y cuatro sépalos cortos que abrazan el pie de los pétalos, del vino <see cref="FlorSepal"/>, con
        /// espinas pequeñas en el canto y luz de borde.
        /// </summary>
        internal Sprite FlorSepals => _florSepals != null ? _florSepals : (_florSepals = BuildFlorSepals());

        private Sprite BuildFlorPetal()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(FlorPetalWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(FlorPetalHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            var basePx = new Vector2(cx, 1.5f * k);
            float len = h - (3f * k);
            DrawLeafShape(pc, basePx, Vector2.up, len, w - (2f * k), new Color(0.40f, 0.40f, 0.42f, 1f), 1f);
            DrawLeafShape(pc, basePx + new Vector2(0f, 2.6f * k), Vector2.up, len - (5.2f * k), w - (7.5f * k), Color.white, 1f);

            // Volumen: luz arriba a la izquierda y algo de sombra en el pie (lo tapa el cáliz), solo en el relleno.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color p = pc.Get(x, y);
                    if (p.a <= 0f || p.r < 0.6f)
                    {
                        continue;
                    }

                    float nx = ((x + 0.5f) - cx) / (w * 0.45f);
                    float ny = (y + 0.5f) / h;
                    float lit = Mathf.Clamp01(0.55f - (0.35f * nx) + (0.25f * ny));
                    float v = p.r * Mathf.Lerp(0.84f, 1f, lit);
                    pc.Set(x, y, new Color(v, v, v, p.a));
                }
            }

            // Nervio central, más claro que el contorno: del pie al 78 % del pétalo.
            pc.FillCapsule(cx, 4f * k, cx, h * 0.78f, 0.9f * k, new Color(0.80f, 0.80f, 0.82f, 1f));
            return MakeSprite(pc, "mech_flor_petal", new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildFlorSepals()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(FlorSepalsWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(FlorSepalsHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            Color edge = ColorMath.Darken(FlorSepal, 0.45f);
            Color light = ColorMath.Lighten(FlorSepal, 0.30f);

            // Cuatro sépalos cortos, dos por lado, que abrazan el pie de los pétalos; contornos primero y después los rellenos.
            var bases = new Vector2[4];
            var dirs = new Vector2[4];
            for (int s = 0; s < 4; s++)
            {
                float side = s < 2 ? -1f : 1f;
                bool outer = s == 0 || s == 3;
                bases[s] = new Vector2(cx + (side * (outer ? 34f : 14f) * k), 16f * k);
                dirs[s] = new Vector2(side * (outer ? 0.62f : 0.22f), outer ? 0.78f : 0.97f).normalized;
            }

            float collarL = 5f * k;
            float collarR = w - (5f * k);
            float collarTop = 24f * k;
            pc.FillRoundedRect(collarL - (2.5f * k), 1f * k, collarR + (2.5f * k), collarTop + (2.5f * k), 11f * k, edge);
            for (int s = 0; s < 4; s++)
            {
                bool outer = s == 0 || s == 3;
                DrawLeafShape(pc, bases[s] - (dirs[s] * (2f * k)), dirs[s], (outer ? 30f : 34f) * k + (4.5f * k), 22f * k + (5f * k), edge, 1f);
            }

            for (int s = 0; s < 4; s++)
            {
                bool outer = s == 0 || s == 3;
                DrawLeafShape(pc, bases[s], dirs[s], (outer ? 30f : 34f) * k, 22f * k, FlorSepal, 1f);
                Vector2 tip = bases[s] + (dirs[s] * ((outer ? 30f : 34f) * k * 0.7f));
                pc.FillCapsule(bases[s].x, bases[s].y, tip.x, tip.y, 0.9f * k, light);
            }

            // Collarín: banda redondeada que encaja en la boca, del vino del cáliz con luz en el canto de arriba.
            pc.FillRoundedRect(collarL, 3.5f * k, collarR, collarTop, 9f * k, FlorSepal);
            pc.FillCapsule(collarL + (8f * k), collarTop - (2.5f * k), collarR - (8f * k), collarTop - (2.5f * k), 1.1f * k, light);

            // Espinas: triángulos pequeños en el canto de abajo y en los costados del collarín (la vitória-régia es espinosa).
            for (int i = 0; i < 9; i++)
            {
                float x = Mathf.Lerp(collarL + (6f * k), collarR - (6f * k), i / 8f);
                pc.FillTriangle(new Vector2(x - (2.2f * k), 4f * k), new Vector2(x + (2.2f * k), 4f * k), new Vector2(x, 0.5f * k), edge);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side < 0 ? collarL : collarR;
                for (int i = 0; i < 2; i++)
                {
                    float y = (9f + (i * 9f)) * k;
                    pc.FillTriangle(new Vector2(x, y - (2.2f * k)), new Vector2(x, y + (2.2f * k)), new Vector2(x + (side * 3.6f * k), y), edge);
                }
            }

            return MakeSprite(pc, "mech_flor_sepals", new Vector2(0.5f, 0f), Vector4.zero);
        }

        // Icono de la Flor de vitória-régia en un lienzo cuadrado de s px (k = s / 96; y hacia arriba): la Flor cerrada del tablero
        // (FlorOverlay sin llaves cumplidas), con sus cinco pétalos blancos (FlorWhite, sin rosa: el rosa solo sale al abrirse) en
        // capullo, con el contorno gris del pétalo del tablero, sobre su cáliz espinoso de vino; y a los lados, abajo, dos discos
        // neutros de FlorIconKeyDiameter del lado (FlorIconKeyFill con filo de 1 dp en FlorIconKeyEdge), las dos llaves en lógica Y
        // (b.3), sin icono de fruta: la fruta concreta la pone el nivel (revisión de VA1b). Solo la flor: sin la hoja redonda, que
        // es la Hoja escudo (b.3).
        private static void DrawFlorGlyph(PixelCanvas pc, int s)
        {
            float k = s / 96f;
            Color petalEdge = new Color(FlorWhite.r * 0.40f, FlorWhite.g * 0.40f, FlorWhite.b * 0.42f, 1f); // el gris del pétalo, teñido
            Color sepalEdge = ColorMath.Darken(FlorSepal, 0.45f);
            Color sepalLight = ColorMath.Lighten(FlorSepal, 0.30f);
            var foot = new Vector2(48f * k, 24f * k);

            // Pétalos de atrás adelante, cerrados como en el tablero (giros de 15°, 7° y 0°): los de fuera, algo más oscuros.
            float[] deg = { 15f, 7f };
            float[] footX = { 4.5f, 2.5f };
            for (int ring = 0; ring < 2; ring++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float a = deg[ring] * Mathf.Deg2Rad;
                    var dir = new Vector2(side * Mathf.Sin(a), Mathf.Cos(a));
                    Vector2 b = foot + new Vector2(side * footX[ring] * k, 0f);
                    DrawLeafShape(pc, b - (dir * (1.5f * k)), dir, 67f * k, 27f * k, petalEdge, 1f);
                    DrawLeafShape(pc, b, dir, 64f * k, 22f * k, ring == 0 ? ColorMath.Darken(FlorWhite, 0.06f) : FlorWhite, 1f);
                }
            }

            DrawLeafShape(pc, foot - new Vector2(0f, 1.5f * k), Vector2.up, 67f * k, 27f * k, petalEdge, 1f);
            DrawLeafShape(pc, foot, Vector2.up, 64f * k, 22f * k, FlorWhite, 1f);
            pc.FillCapsule(foot.x, foot.y + (8f * k), foot.x, foot.y + (48f * k), 1.1f * k, ColorMath.Lighten(FlorWhite, 0.5f));

            // El cáliz: dos sépalos cortos que abrazan el pie de los pétalos y el collarín vino con espinas, delante.
            for (int side = -1; side <= 1; side += 2)
            {
                var dir = new Vector2(side * 0.62f, 0.78f).normalized;
                var b = new Vector2((48f + (side * 12f)) * k, 24f * k);
                DrawLeafShape(pc, b - (dir * (2f * k)), dir, 24f * k, 18f * k, sepalEdge, 1f);
                DrawLeafShape(pc, b, dir, 20f * k, 13f * k, FlorSepal, 1f);
            }

            // El collarín, algo más ancho que el capullo, como en el tablero (cáliz de 1,32 u).
            pc.FillRoundedRect(25.5f * k, 9.5f * k, 70.5f * k, 30.5f * k, 9f * k, sepalEdge);
            pc.FillRoundedRect(28f * k, 12f * k, 68f * k, 28f * k, 7f * k, FlorSepal);
            pc.FillCapsule(35f * k, 25.5f * k, 61f * k, 25.5f * k, 1.1f * k, sepalLight);
            for (int i = 0; i < 6; i++)
            {
                float x = Mathf.Lerp(32f * k, 64f * k, i / 5f);
                pc.FillTriangle(new Vector2(x - (2.4f * k), 10.5f * k), new Vector2(x + (2.4f * k), 10.5f * k), new Vector2(x, 5.5f * k), sepalEdge);
            }

            // Las dos llaves: discos neutros a los lados del cáliz, con el filo de 1 dp (UiPxPerDp px a cualquier tamaño).
            float r = FlorIconKeyDiameter * 0.5f * s;
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = (48f + (side * 34f)) * k;
                float cy = 18f * k;
                pc.FillCircle(cx, cy, r, FlorIconKeyEdge);
                pc.FillCircle(cx, cy, r - UiPxPerDp, FlorIconKeyFill);
            }
        }
    }
}
