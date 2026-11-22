#nullable enable
using AF.Levels;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Las 5 viñetas por código de los pueblos de paso del Mapa v2 (D-069, bloque M2-2; propuesta del Mapa v2 §4.2; D-052: el arte
    /// por código es el final; D-003 «Selva brillante premium» con el barniz de juguete de D-058 y la luz arriba a la izquierda):
    /// <list type="bullet">
    /// <item><description><b>Muelle y palafitos</b> (<see cref="RiverTownVignette.Stilts"/>): 3 casas de colores sobre pilotes, un
    /// muelle de tablas y una canoa amarrada;</description></item>
    /// <item><description><b>Iglesia y plaza</b> (<see cref="RiverTownVignette.Church"/>): la iglesia blanca con su campanario y
    /// casas alrededor;</description></item>
    /// <item><description><b>Puerto con barco de línea</b> (<see cref="RiverTownVignette.Port"/>): el barco de dos pisos blanco y
    /// azul con las hamacas colgadas;</description></item>
    /// <item><description><b>Mercado del muelle</b> (<see cref="RiverTownVignette.Market"/>): puestos con toldos y cestas de fruta
    /// en el muelle (nunca «mercado flotante»);</description></item>
    /// <item><description><b>Ciudad con hito</b> (<see cref="RiverTownVignette.Landmark"/>): una cúpula de colores entre torres,
    /// para las paradas que la tienen.</description></item>
    /// </list>
    /// Sin personas (VA-22: la vida, en los barcos, las hamacas y las canoas), sin malocas ni arquitectura indígena (§4.2) y, en
    /// Parintins, sin los bois ni los colores de sus asociaciones (glosario §3.3). Cada una en <see cref="TownVariants"/> juegos de
    /// colores. Las pinta <see cref="MapTownArt"/> en su atlas de 1024 × 512 al abrir el Mapa v2 con <c>ui_map_towns</c>.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Juegos de colores de cada viñeta.</summary>
        public const int TownVariants = 3;

        /// <summary>Ancho de la celda de una viñeta en el atlas (px).</summary>
        public const int TownCellWidthPx = 204;

        /// <summary>Alto de la celda de una viñeta en el atlas (px).</summary>
        public const int TownCellHeightPx = 170;

        // Paredes de las casas por juego [P] (la paleta de la guía visual: rojo achiote, amarillo, azul río; verde, naranja, crema;
        // rosa, turquesa, arena).
        private static readonly Color[][] TownWalls =
        {
            new Color[] { Palette.Hex(0xE8553D), Palette.Hex(0xF2C230), Palette.Hex(0x3FA7D6) },
            new Color[] { Palette.Hex(0x7BC67E), Palette.Hex(0xF28C38), Palette.Hex(0xEFE6CF) },
            new Color[] { Palette.Hex(0xD96BA0), Palette.Hex(0x5BC0BE), Palette.Hex(0xF4E3B2) },
        };

        private static readonly Color TownRoofTin = Palette.Hex(0x9FB3BC);
        private static readonly Color TownRoofRed = Palette.Hex(0xB5462F);
        private static readonly Color TownWood = Palette.Hex(0x8A5A34);
        private static readonly Color TownWoodDark = Palette.Hex(0x5E3B22);
        private static readonly Color TownWhite = Palette.Hex(0xF7F4EA);
        private static readonly Color TownWindow = Palette.Hex(0x2A3A44);
        private static readonly Color TownBlue = Palette.Hex(0x2F6FB2);
        private static readonly Color TownGround = Palette.Hex(0xD9C08A);

        /// <summary>Origen (abajo a la izquierda, px) de la celda de <paramref name="vignette"/> en su juego <paramref name="variant"/>.</summary>
        public static Vector2Int TownCell(RiverTownVignette vignette, int variant)
        {
            int col = Mathf.Clamp((int)vignette - 1, 0, 4);
            int row = Mathf.Clamp(variant, 0, TownVariants - 1);
            return new Vector2Int(col * TownCellWidthPx, row * TownCellHeightPx);
        }

        /// <summary>Pinta las 5 viñetas en sus 3 juegos en <paramref name="pc"/> (1024 × 512, transparente).</summary>
        internal static void PaintTownAtlas(PixelCanvas pc)
        {
            for (int v = 0; v < TownVariants; v++)
            {
                PaintTown(pc, RiverTownVignette.Stilts, v);
                PaintTown(pc, RiverTownVignette.Church, v);
                PaintTown(pc, RiverTownVignette.Port, v);
                PaintTown(pc, RiverTownVignette.Market, v);
                PaintTown(pc, RiverTownVignette.Landmark, v);
            }
        }

        private static void PaintTown(PixelCanvas pc, RiverTownVignette vignette, int variant)
        {
            Vector2Int o = TownCell(vignette, variant);
            Color[] walls = TownWalls[variant];
            // El suelo: una isla de arena con su sombra, para que la viñeta se lea sobre la selva y junto al agua.
            pc.FillEllipseSoft(o.x + 104f, o.y + 30f, 92f, 22f, 10f, new Color(0.02f, 0.10f, 0.05f, 0.35f));
            pc.FillEllipseSoft(o.x + 102f, o.y + 36f, 86f, 18f, 3f, TownGround);
            pc.FillEllipseSoft(o.x + 90f, o.y + 40f, 50f, 8f, 6f, new Color(1f, 0.97f, 0.85f, 0.35f));
            switch (vignette)
            {
                case RiverTownVignette.Stilts:
                    PaintStilts(pc, o, walls);
                    break;
                case RiverTownVignette.Church:
                    PaintChurch(pc, o, walls);
                    break;
                case RiverTownVignette.Port:
                    PaintPort(pc, o, walls);
                    break;
                case RiverTownVignette.Market:
                    PaintMarket(pc, o, walls);
                    break;
                default:
                    PaintLandmark(pc, o, walls);
                    break;
            }
        }

        // Una caja con volumen: la cara con luz arriba a la izquierda, el borde derecho más oscuro y un filo de luz arriba.
        private static void TownBox(PixelCanvas pc, float x0, float y0, float x1, float y1, Color c)
        {
            pc.FillRoundedRect(x0, y0, x1, y1, 2f, TownShade(c, 0.78f));
            pc.FillRoundedRect(x0, y0 + 1f, x1 - 3f, y1, 2f, c);
            pc.FillRoundedRect(x0 + 1f, y1 - 3f, x1 - 4f, y1, 1.5f, Color.Lerp(c, Color.white, 0.35f));
        }

        private static void TownRoof(PixelCanvas pc, float x0, float x1, float y, float h, Color c)
        {
            float over = 4f;
            pc.FillTriangle(new Vector2(x0 - over, y), new Vector2(x1 + over, y), new Vector2((x0 + x1) * 0.5f, y + h), TownShade(c, 0.8f));
            pc.FillTriangle(new Vector2(x0 - over, y + 1.5f), new Vector2((x1 + x0) * 0.5f + 1f, y + 1.5f), new Vector2((x0 + x1) * 0.5f, y + h), c);
        }

        private static void TownWindows(PixelCanvas pc, float x0, float y0, float x1, int count, float size)
        {
            float step = (x1 - x0) / (count + 1);
            for (int i = 1; i <= count; i++)
            {
                float cx = x0 + (step * i);
                pc.FillRoundedRect(cx - (size * 0.5f), y0, cx + (size * 0.5f), y0 + size, 1.2f, TownWindow);
                pc.FillRoundedRect(cx - (size * 0.5f), y0 + size - 1.5f, cx + (size * 0.5f), y0 + size, 0.6f, new Color(1f, 1f, 1f, 0.35f));
            }
        }

        private static Color TownShade(Color c, float k)
        {
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        private static void TownCanoe(PixelCanvas pc, float x, float y)
        {
            pc.FillCapsule(x - 20f, y, x + 20f, y, 5f, TownWoodDark);
            pc.FillCapsule(x - 18f, y + 2f, x + 18f, y + 2f, 3f, TownWood);
        }

        private static void PaintStilts(PixelCanvas pc, Vector2Int o, Color[] walls)
        {
            // El muelle de tablas hacia el agua (a la derecha) y la canoa amarrada.
            pc.FillRoundedRect(o.x + 120f, o.y + 40f, o.x + 196f, o.y + 47f, 2f, TownWoodDark);
            for (int k = 0; k < 7; k++)
            {
                float x = o.x + 124f + (k * 10f);
                pc.FillRoundedRect(x, o.y + 41f, x + 8f, o.y + 47f, 1f, TownWood);
            }

            TownCanoe(pc, o.x + 172f, o.y + 30f);
            for (int h = 0; h < 3; h++)
            {
                float x0 = o.x + 14f + (h * 46f);
                float base0 = o.y + 50f + ((h % 2) * 6f);
                // Pilotes.
                for (int p = 0; p < 4; p++)
                {
                    float px = x0 + 4f + (p * 11f);
                    pc.FillCapsule(px, o.y + 30f, px, base0 + 2f, 1.8f, TownWoodDark);
                }

                TownBox(pc, x0, base0, x0 + 40f, base0 + 34f, walls[h]);
                TownWindows(pc, x0, base0 + 12f, x0 + 37f, 2, 8f);
                pc.FillRoundedRect(x0 + 15f, base0, x0 + 24f, base0 + 14f, 1f, TownShade(walls[h], 0.55f)); // la puerta
                TownRoof(pc, x0, x0 + 40f, base0 + 34f, 20f, h == 1 ? TownRoofRed : TownRoofTin);
            }
        }

        private static void PaintChurch(PixelCanvas pc, Vector2Int o, Color[] walls)
        {
            // Casas a los lados.
            TownBox(pc, o.x + 10f, o.y + 40f, o.x + 46f, o.y + 70f, walls[0]);
            TownWindows(pc, o.x + 10f, o.y + 52f, o.x + 43f, 2, 8f);
            TownRoof(pc, o.x + 10f, o.x + 46f, o.y + 70f, 16f, TownRoofRed);
            TownBox(pc, o.x + 150f, o.y + 40f, o.x + 192f, o.y + 72f, walls[1]);
            TownWindows(pc, o.x + 150f, o.y + 54f, o.x + 189f, 2, 8f);
            TownRoof(pc, o.x + 150f, o.x + 192f, o.y + 72f, 16f, TownRoofTin);
            // La nave, el frontón y el campanario, blancos con su borde azul.
            TownBox(pc, o.x + 58f, o.y + 40f, o.x + 120f, o.y + 92f, TownWhite);
            TownRoof(pc, o.x + 58f, o.x + 120f, o.y + 92f, 24f, TownRoofRed);
            pc.FillRoundedRect(o.x + 82f, o.y + 40f, o.x + 96f, o.y + 66f, 6f, TownBlue); // la puerta en arco
            TownBox(pc, o.x + 120f, o.y + 40f, o.x + 142f, o.y + 128f, TownWhite);
            pc.FillRoundedRect(o.x + 125f, o.y + 104f, o.x + 136f, o.y + 118f, 5f, TownWindow); // el hueco de la campana
            pc.FillCircle(o.x + 130.5f, o.y + 108f, 3.5f, Palette.Hex(0xE2B24A));
            pc.FillTriangle(new Vector2(o.x + 118f, o.y + 128f), new Vector2(o.x + 144f, o.y + 128f), new Vector2(o.x + 131f, o.y + 150f), TownBlue);
            pc.FillCapsule(o.x + 131f, o.y + 150f, o.x + 131f, o.y + 162f, 1.6f, TownWhite);
            pc.FillCapsule(o.x + 126f, o.y + 157f, o.x + 136f, o.y + 157f, 1.6f, TownWhite);
            TownWindows(pc, o.x + 58f, o.y + 72f, o.x + 117f, 2, 9f);
        }

        private static void PaintPort(PixelCanvas pc, Vector2Int o, Color[] walls)
        {
            // El muelle y, detrás, dos casas.
            TownBox(pc, o.x + 12f, o.y + 44f, o.x + 50f, o.y + 76f, walls[2]);
            TownRoof(pc, o.x + 12f, o.x + 50f, o.y + 76f, 16f, TownRoofTin);
            TownBox(pc, o.x + 150f, o.y + 48f, o.x + 192f, o.y + 82f, walls[0]);
            TownRoof(pc, o.x + 150f, o.x + 192f, o.y + 82f, 16f, TownRoofRed);
            pc.FillRoundedRect(o.x + 8f, o.y + 36f, o.x + 196f, o.y + 44f, 2f, TownWoodDark);
            // El barco de línea («recreio»): casco azul, dos pisos blancos con sus ventanas y las hamacas de colores.
            pc.FillCapsule(o.x + 42f, o.y + 30f, o.x + 168f, o.y + 30f, 11f, TownBlue);
            pc.FillTriangle(new Vector2(o.x + 160f, o.y + 41f), new Vector2(o.x + 186f, o.y + 41f), new Vector2(o.x + 168f, o.y + 20f), TownBlue);
            pc.FillRoundedRect(o.x + 40f, o.y + 40f, o.x + 160f, o.y + 66f, 3f, TownWhite);
            pc.FillRoundedRect(o.x + 52f, o.y + 66f, o.x + 148f, o.y + 90f, 3f, TownWhite);
            pc.FillRoundedRect(o.x + 46f, o.y + 64f, o.x + 154f, o.y + 68f, 1.5f, TownBlue);
            pc.FillRoundedRect(o.x + 58f, o.y + 88f, o.x + 142f, o.y + 93f, 1.5f, TownBlue);
            pc.FillRoundedRect(o.x + 118f, o.y + 93f, o.x + 132f, o.y + 108f, 2f, TownWhite); // la cabina
            for (int k = 0; k < 6; k++)
            {
                float x = o.x + 50f + (k * 18f);
                Color hammock = walls[k % 3];
                pc.StrokeArc(x + 6f, o.y + 52f, 6f, 3f, 200f, 340f, hammock); // una hamaca colgada en la cubierta
                pc.FillRoundedRect(x + 2f, o.y + 74f, x + 12f, o.y + 82f, 1.5f, TownWindow);
            }

            pc.FillCapsule(o.x + 42f, o.y + 24f, o.x + 168f, o.y + 24f, 2f, new Color(1f, 1f, 1f, 0.45f)); // el reflejo del casco
        }

        private static void PaintMarket(PixelCanvas pc, Vector2Int o, Color[] walls)
        {
            // El muelle de tablas y tres puestos con toldo de rayas.
            pc.FillRoundedRect(o.x + 10f, o.y + 36f, o.x + 194f, o.y + 46f, 2f, TownWoodDark);
            for (int k = 0; k < 9; k++)
            {
                float x = o.x + 12f + (k * 20f);
                pc.FillRoundedRect(x, o.y + 38f, x + 18f, o.y + 46f, 1f, TownWood);
            }

            Color[] fruits = { (Color)Palette.Hex(0x5B2A6E), Palette.Hex(0xE8553D), Palette.Hex(0xF2C230), Palette.Hex(0x7BC67E) };
            for (int s = 0; s < 3; s++)
            {
                float x0 = o.x + 16f + (s * 60f);
                float x1 = x0 + 52f;
                pc.FillCapsule(x0 + 3f, o.y + 46f, x0 + 3f, o.y + 96f, 2f, TownWoodDark);
                pc.FillCapsule(x1 - 3f, o.y + 46f, x1 - 3f, o.y + 96f, 2f, TownWoodDark);
                TownBox(pc, x0 + 2f, o.y + 46f, x1 - 2f, o.y + 62f, TownWood); // el mostrador
                // Las cestas con fruta (açaí, achiote, cupuaçu, limón): bolitas con barniz.
                for (int b = 0; b < 3; b++)
                {
                    float bx = x0 + 11f + (b * 15f);
                    pc.FillRoundedRect(bx - 6f, o.y + 60f, bx + 6f, o.y + 67f, 2f, Palette.Hex(0xB98A4E));
                    Color f = fruits[(s + b) % fruits.Length];
                    pc.FillCircle(bx - 2.5f, o.y + 69f, 3.2f, f);
                    pc.FillCircle(bx + 2.5f, o.y + 69f, 3.2f, f);
                    pc.FillCircle(bx, o.y + 72f, 3.2f, Color.Lerp(f, Color.white, 0.15f));
                }

                // El toldo: rayas del color del puesto y crema, con su faldón ondulado.
                Color c = walls[s];
                for (int k = 0; k < 6; k++)
                {
                    float sx = x0 + (k * (x1 - x0) / 6f);
                    pc.FillTriangle(new Vector2(sx - 1f, o.y + 92f), new Vector2(sx + ((x1 - x0) / 6f) + 1f, o.y + 92f),
                        new Vector2((x0 + x1) * 0.5f, o.y + 112f), k % 2 == 0 ? c : TownWhite);
                }

                for (int k = 0; k < 6; k++)
                {
                    float cx = x0 + 4.5f + (k * (x1 - x0 - 9f) / 5f);
                    pc.FillCircle(cx, o.y + 92f, 4.5f, k % 2 == 0 ? c : TownWhite);
                }
            }

            TownCanoe(pc, o.x + 160f, o.y + 26f); // un barco de feria amarrado
        }

        private static void PaintLandmark(PixelCanvas pc, Vector2Int o, Color[] walls)
        {
            // Una ciudad con hito: el edificio con la cúpula de colores entre dos torres, y casas delante.
            TownBox(pc, o.x + 50f, o.y + 44f, o.x + 154f, o.y + 96f, TownWhite);
            pc.FillRoundedRect(o.x + 50f, o.y + 92f, o.x + 154f, o.y + 98f, 1.5f, Palette.Hex(0xE2B24A));
            TownWindows(pc, o.x + 50f, o.y + 70f, o.x + 151f, 5, 9f);
            TownWindows(pc, o.x + 50f, o.y + 50f, o.x + 151f, 5, 9f);
            Color dome = walls[1];
            // La cúpula de tejas vidriadas (bandas de color) con su linterna.
            for (int k = 0; k < 5; k++)
            {
                float r = 30f - (k * 5f);
                pc.FillEllipseSoft(o.x + 102f, o.y + 98f + (k * 5f), r, r * 0.9f, 1f, k % 2 == 0 ? dome : Color.Lerp(dome, Palette.Hex(0xF2C230), 0.6f));
            }

            pc.FillEllipseSoft(o.x + 94f, o.y + 120f, 6f, 9f, 2f, new Color(1f, 1f, 1f, 0.45f));
            pc.FillRoundedRect(o.x + 97f, o.y + 126f, o.x + 107f, o.y + 140f, 2f, TownWhite);
            pc.FillTriangle(new Vector2(o.x + 95f, o.y + 140f), new Vector2(o.x + 109f, o.y + 140f), new Vector2(o.x + 102f, o.y + 152f), Palette.Hex(0xE2B24A));
            TownBox(pc, o.x + 36f, o.y + 44f, o.x + 54f, o.y + 120f, walls[0]);
            TownRoof(pc, o.x + 36f, o.x + 54f, o.y + 120f, 14f, TownRoofRed);
            TownBox(pc, o.x + 150f, o.y + 44f, o.x + 168f, o.y + 120f, walls[0]);
            TownRoof(pc, o.x + 150f, o.x + 168f, o.y + 120f, 14f, TownRoofRed);
            TownBox(pc, o.x + 8f, o.y + 36f, o.x + 38f, o.y + 62f, walls[2]);
            TownRoof(pc, o.x + 8f, o.x + 38f, o.y + 62f, 12f, TownRoofTin);
            TownBox(pc, o.x + 168f, o.y + 36f, o.x + 198f, o.y + 60f, walls[2]);
            TownRoof(pc, o.x + 168f, o.x + 198f, o.y + 60f, 12f, TownRoofTin);
        }
    }
}
