#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using AF.Levels;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Frutas en vuelo de «Llenar y teñir» (guion §5 y §11): 14 sprites de 64 px en un solo atlas de 512 × 128 (una sola
    /// textura, un lote de dibujo), generados de forma perezosa en la primera intro, con la forma de la familia del icono de
    /// accesibilidad de cada tinte (07 §3), contorno de tinta y luz arriba a la izquierda. Ninguna es la semilla ámbar de la
    /// moneda (glosario §2.1): el taperebá lleva rabito y hoja, el cacao es un trozo de mazorca y nunca un grano, el urucum
    /// suelta semillitas rojas y angulosas, el Río Negro es una hoja parda rizada y el Tapajós, gotas. Si existe el atlas
    /// reducido que genera el Editor a partir de los PNG <c>fruta_&lt;tinte&gt;</c> (<c>AF.EditorTools.GeneratedArt</c>), se usa ese.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado de cada fruta en el atlas (px).</summary>
        public const int FruitFlightPx = 64;

        /// <summary>Ancho del atlas de frutas en vuelo (8 columnas).</summary>
        public const int FruitAtlasWidth = 512;

        /// <summary>Alto del atlas de frutas en vuelo (2 filas).</summary>
        public const int FruitAtlasHeight = 128;

        /// <summary>Atlas reducido de los PNG <c>fruta_*</c> dentro de <c>Resources</c> (lo escribe el Editor).</summary>
        public const string FruitFlightAtlasResource = "AF/fruit_flight_atlas";

        /// <summary>ID de cada tinte por índice de motor (glosario §9), para las ranuras <c>fruta_&lt;id&gt;</c>.</summary>
        public static readonly string[] TintIds =
        {
            string.Empty, "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo",
            "agua_tapajos", "lulo", "araza", "cacao", "agua_negro",
        };

        private static readonly Color Ink = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.8f);

        private Sprite[]? _fruitFlight;
        private Texture2D? _fruitAtlas;

        private Sprite? _riverSwell;

        /// <summary>
        /// Degradado vertical blanco de la capa de crecida del río (guion §11): alfa 0 arriba (el horizonte) → 1 abajo (junto
        /// al panel); el color y el alfa máximo (0,35) los pone <c>Image.color</c>.
        /// </summary>
        public Sprite RiverSwell
        {
            get
            {
                if (_riverSwell == null)
                {
                    var pc = new PixelCanvas(4, 64);
                    for (int y = 0; y < 64; y++)
                    {
                        float a = 1f - ((y + 0.5f) / 64f);
                        for (int x = 0; x < 4; x++)
                        {
                            pc.Set(x, y, new Color(1f, 1f, 1f, a * a * (3f - (2f * a))));
                        }
                    }

                    _riverSwell = MakeSprite(pc, "river_swell", new Vector2(0.5f, 0.5f), Vector4.zero);
                }

                return _riverSwell;
            }
        }

        /// <summary>Tiempo de generación del atlas de frutas (≤ 10 ms en el Editor; 0 si aún no se generó).</summary>
        public double FruitAtlasMs { get; private set; }

        /// <summary>Textura del atlas de frutas (null si aún no se generó).</summary>
        public Texture2D? FruitAtlas => _fruitAtlas;

        /// <summary>Sprite de la fruta en vuelo del tinte <paramref name="tint"/> (1-14). Genera el atlas la primera vez.</summary>
        public Sprite FruitFlight(int tint)
        {
            Sprite[] sprites = EnsureFruitAtlas();
            return sprites[Mathf.Clamp(tint, 1, 14)];
        }

        /// <summary>Rectángulo en píxeles de la fruta del tinte <paramref name="tint"/> en el atlas de 512 × 128.</summary>
        public static RectInt FruitCell(int tint)
        {
            int index = Mathf.Clamp(tint, 1, 14) - 1;
            int col = index % 8;
            int rowFromTop = index / 8;
            return new RectInt(col * FruitFlightPx, FruitAtlasHeight - ((rowFromTop + 1) * FruitFlightPx), FruitFlightPx, FruitFlightPx);
        }

        /// <summary>
        /// Píxeles del atlas procedural de 512 × 128 (de abajo arriba, como <see cref="Texture2D"/>). Lo usa el Editor para
        /// componer el atlas con los PNG que haya y el respaldo procedural del resto.
        /// </summary>
        public static Color32[] FruitFlightPixels()
        {
            return BuildFruitAtlasCanvas().ToColor32();
        }

        /// <summary>
        /// Herramienta de revisión: escribe en <paramref name="dir"/> el atlas de frutas en vuelo y las piezas procedurales de
        /// cada especie de <paramref name="species"/> para mirarlos sin abrir el juego. No se usa en el juego.
        /// </summary>
        public static void ExportIntroArt(string dir, IReadOnlyList<FaunaSpecies> species)
        {
            System.IO.Directory.CreateDirectory(dir);
            Save(dir, "fruit_flight_atlas", BuildFruitAtlasCanvas());
            for (int i = 0; i < species.Count; i++)
            {
                var art = new MonkeyArt(species[i].Id, species[i].Skeleton);
                Save(dir, "fauna_" + species[i].Id, BuildFaunaCanvas(species[i], art));
            }
        }

        // Tras Dispose, las texturas y los sprites ya no existen: se vuelven a generar si hace falta.
        private void ForgetIntroArt()
        {
            _fruitFlight = null;
            _fruitAtlas = null;
            _riverSwell = null;
            _monkeys.Clear();
        }

        private Sprite[] EnsureFruitAtlas()
        {
            if (_fruitFlight != null)
            {
                return _fruitFlight;
            }

            Stopwatch watch = Stopwatch.StartNew();
            Texture2D? tex = ArtSlots.ForceProcedural ? null : Resources.Load<Texture2D>(FruitFlightAtlasResource);
            if (tex == null || tex.width != FruitAtlasWidth || tex.height != FruitAtlasHeight)
            {
                tex = BuildFruitAtlasCanvas().ToTexture("fruit_flight_atlas");
                _owned.Add(tex);
            }

            _fruitAtlas = tex;
            var sprites = new Sprite[15];
            for (int tint = 1; tint <= 14; tint++)
            {
                RectInt cell = FruitCell(tint);
                Sprite sprite = Sprite.Create(tex, new Rect(cell.x, cell.y, cell.width, cell.height), new Vector2(0.5f, 0.5f), 100f * UiPxPerDp, 0, SpriteMeshType.FullRect);
                sprite.name = "fruta_vuelo_" + TintIds[tint];
                sprite.hideFlags = HideFlags.DontSave;
                _owned.Add(sprite);
                sprites[tint] = sprite;
            }

            sprites[0] = sprites[1];
            _fruitFlight = sprites;
            watch.Stop();
            FruitAtlasMs = watch.Elapsed.TotalMilliseconds;
            return sprites;
        }

        private static PixelCanvas BuildFruitAtlasCanvas()
        {
            var pc = new PixelCanvas(FruitAtlasWidth, FruitAtlasHeight);
            for (int tint = 1; tint <= 14; tint++)
            {
                RectInt cell = FruitCell(tint);
                DrawFruit(pc, tint, cell.x, cell.y);
            }

            return pc;
        }

        // Cada fruta en su celda de 64 × 64 px con origen (ox, oy), abajo a la izquierda; contorno de tinta de 2 px.
        private static void DrawFruit(PixelCanvas pc, int tint, float ox, float oy)
        {
            switch (tint)
            {
                case 1: // Guanábana: gajo de pulpa blanca con 1-2 pepitas negras y la cáscara verde en el lomo.
                    FillSector(pc, ox + 14f, oy + 12f, 44f, 22f, 72f, Ink, 2f);
                    FillSector(pc, ox + 14f, oy + 12f, 44f, 22f, 72f, Hex(0x5E8A3A), 0f);
                    FillSector(pc, ox + 14f, oy + 12f, 39f, 25f, 69f, Hex(0xF7F4EA), 0f);
                    pc.FillEllipseSoft(ox + 34f, oy + 38f, 9f, 6f, 4f, new Color(1f, 1f, 1f, 0.55f));
                    Seed(pc, ox + 30f, oy + 26f, 3.6f, 2.2f, Hex(0x1E1A16));
                    Seed(pc, ox + 40f, oy + 36f, 3.4f, 2.1f, Hex(0x1E1A16));
                    break;

                case 2: // Copoazú: trozo de pulpa crema con el borde de cáscara parda y vellosa.
                    Outlined(pc, ox + 32f, oy + 30f, 24f, 19f, Hex(0x7A5634));
                    pc.FillEllipseSoft(ox + 35f, oy + 33f, 19f, 14.5f, 1f, Hex(0xEFE2BE));
                    pc.FillEllipseSoft(ox + 30f, oy + 38f, 9f, 5f, 4f, new Color(1f, 1f, 1f, 0.5f));
                    for (int i = 0; i < 9; i++)
                    {
                        float a = (200f + (i * 16f)) * Mathf.Deg2Rad;
                        pc.FillCircle(ox + 32f + (Mathf.Cos(a) * 22f), oy + 30f + (Mathf.Sin(a) * 17f), 1.4f, Hex(0xA07A50)); // vello
                    }

                    break;

                case 3: // Taperebá: ciruela amarilla con rabito y una hoja verde (nunca la semilla-moneda).
                    pc.FillCapsule(ox + 31f, oy + 44f, ox + 34f, oy + 55f, 3.2f, Ink);
                    pc.FillCapsule(ox + 31f, oy + 44f, ox + 34f, oy + 55f, 1.6f, Hex(0x6B4A2B));
                    DrawLeaf(pc, new Vector2(ox + 34f, oy + 51f), new Vector2(0.94f, 0.34f), 17f, 10f, 0.08f, LeafShape.Lanceolate, Palette.LeafMid, Palette.LeafLight, 0.5f, 3);
                    Outlined(pc, ox + 30f, oy + 28f, 18f, 20f, Hex(0xEFB21C));
                    pc.FillEllipseSoft(ox + 34f, oy + 24f, 13f, 14f, 5f, new Color(0.85f, 0.55f, 0.05f, 0.45f)); // sombra cálida
                    pc.FillEllipseSoft(ox + 24f, oy + 35f, 6f, 8f, 4f, new Color(1f, 0.96f, 0.7f, 0.85f)); // brillo
                    break;

                case 4: // Aguaje: fruto escamoso vinotinto, mordido, con la pulpa naranja a la vista.
                    Outlined(pc, ox + 32f, oy + 30f, 19f, 23f, Hex(0x6A1E1C));
                    for (int r = 0; r < 6; r++)
                    {
                        for (int c = -2; c <= 2; c++)
                        {
                            float sx = ox + 32f + (c * 8f) + ((r % 2) * 4f);
                            float sy = oy + 12f + (r * 7f);
                            if (Inside(sx - ox - 32f, sy - oy - 30f, 17f, 21f))
                            {
                                pc.StrokeArc(sx, sy, 3.6f, 1.2f, 200f, 340f, new Color(0.62f, 0.27f, 0.20f, 0.9f));
                            }
                        }
                    }

                    pc.FillEllipseSoft(ox + 40f, oy + 42f, 10f, 9f, 1f, Hex(0xEF8A24)); // mordisco con la pulpa
                    pc.FillEllipseSoft(ox + 41f, oy + 43f, 6f, 5f, 3f, Hex(0xFFB24E));
                    break;

                case 5: // Urucum: cápsula roja erizada abierta con semillitas rojas y angulosas (nunca ámbar).
                    for (int i = 0; i < 11; i++)
                    {
                        float a = (i / 11f) * Mathf.PI * 2f;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Vector2 b = new Vector2(ox + 32f, oy + 30f) + (d * 18f);
                        pc.FillTriangle(b + (d * 7f), b + (new Vector2(-d.y, d.x) * 3f), b - (new Vector2(-d.y, d.x) * 3f), Hex(0xB2301C)); // púas blandas
                    }

                    Outlined(pc, ox + 32f, oy + 30f, 21f, 19f, Hex(0xC23A22));
                    pc.FillEllipseSoft(ox + 32f, oy + 31f, 14f, 12f, 1f, Hex(0xF0D2A4)); // interior claro de las valvas
                    for (int i = 0; i < 7; i++)
                    {
                        float a = (i / 7f) * Mathf.PI * 2f;
                        float sx = ox + 32f + (Mathf.Cos(a) * 7.5f);
                        float sy = oy + 31f + (Mathf.Sin(a) * 6.5f);
                        Angular(pc, sx, sy, 3.6f, a, Hex(0xB51E14));
                    }

                    Angular(pc, ox + 32f, oy + 31f, 3.4f, 0.4f, Hex(0xA8180F));
                    break;

                case 6: // Camu camu: 2-3 bayas granate brillantes juntas.
                    pc.FillCapsule(ox + 32f, oy + 50f, ox + 26f, oy + 38f, 1.3f, Hex(0x5A4024));
                    pc.FillCapsule(ox + 32f, oy + 50f, ox + 40f, oy + 34f, 1.3f, Hex(0x5A4024));
                    Berry(pc, ox + 22f, oy + 26f, 11f, Hex(0x8E1A22));
                    Berry(pc, ox + 41f, oy + 24f, 11.5f, Hex(0x9A1E26));
                    Berry(pc, ox + 31f, oy + 38f, 10f, Hex(0x82161E));
                    break;

                case 7: // Bacaba: racimito rojizo sin pruina (para no confundirlo con el açaí).
                    pc.FillCapsule(ox + 32f, oy + 58f, ox + 32f, oy + 20f, 1.4f, Hex(0x6B4A2B));
                    for (int i = 0; i < 7; i++)
                    {
                        float sx = ox + 32f + ((i % 2 == 0 ? -1f : 1f) * (6f + ((i % 3) * 2f)));
                        float sy = oy + 46f - (i * 5.2f);
                        Berry(pc, sx, sy, 6.6f, Hex(0xB04A60));
                    }

                    break;

                case 8: // Açaí: ramita de bolitas violeta-negras con pruina gris.
                    pc.FillCapsule(ox + 10f, oy + 52f, ox + 50f, oy + 22f, 2.6f, Ink);
                    pc.FillCapsule(ox + 10f, oy + 52f, ox + 50f, oy + 22f, 1.4f, Hex(0x7A5A34));
                    for (int i = 0; i < 6; i++)
                    {
                        float t = 0.18f + (i * 0.14f);
                        float sx = ox + 10f + (40f * t) + ((i % 2 == 0) ? -5f : 5f);
                        float sy = oy + 52f - (30f * t) + ((i % 2 == 0) ? -4f : 3f);
                        Berry(pc, sx, sy, 6.4f, Hex(0x3A1846));
                        pc.FillEllipseSoft(sx + 1f, sy - 1f, 4f, 3f, 3f, new Color(0.62f, 0.60f, 0.68f, 0.45f)); // pruina
                    }

                    break;

                case 9: // Huito: fruto gris pardo partido, con la pulpa clara.
                    Outlined(pc, ox + 24f, oy + 30f, 14f, 19f, Hex(0x7A6C58));
                    Outlined(pc, ox + 41f, oy + 29f, 14f, 19f, Hex(0x7A6C58));
                    pc.FillEllipseSoft(ox + 27f, oy + 30f, 9f, 15f, 1f, Hex(0xD9CBAE));
                    pc.FillEllipseSoft(ox + 38f, oy + 29f, 9f, 15f, 1f, Hex(0xD9CBAE));
                    for (int i = 0; i < 4; i++)
                    {
                        pc.FillCircle(ox + 28f, oy + 22f + (i * 5f), 1.3f, Hex(0x4A3C2C));
                        pc.FillCircle(ox + 37f, oy + 23f + (i * 5f), 1.3f, Hex(0x4A3C2C));
                    }

                    break;

                case 10: // Agua del Tapajós: tres gotas turquesa (icono gota), sin fruta.
                    FruitDrop(pc, ox + 20f, oy + 34f, 8f, Hex(0x3BB8D9));
                    FruitDrop(pc, ox + 42f, oy + 40f, 7f, Hex(0x45C2E0));
                    FruitDrop(pc, ox + 33f, oy + 16f, 7.5f, Hex(0x34AECE));
                    break;

                case 11: // Lulo: bola naranja peluda, mordida, con la pulpa verde a la vista.
                    Outlined(pc, ox + 32f, oy + 30f, 21f, 21f, Hex(0xE58A2A));
                    for (int i = 0; i < 26; i++)
                    {
                        float a = i * 2.39996f;
                        float r = 6f + ((i * 37 % 13) * 1.1f);
                        pc.FillCircle(ox + 32f + (Mathf.Cos(a) * r), oy + 30f + (Mathf.Sin(a) * r), 0.9f, new Color(1f, 0.86f, 0.62f, 0.8f)); // vello
                    }

                    pc.FillEllipseSoft(ox + 40f, oy + 38f, 11f, 10f, 1f, Hex(0x5FB94E));
                    pc.FillEllipseSoft(ox + 40f, oy + 38f, 6f, 5f, 3f, Hex(0x9BDC6A));
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 1.2566f;
                        pc.FillCircle(ox + 40f + (Mathf.Cos(a) * 5f), oy + 38f + (Mathf.Sin(a) * 4.5f), 1.1f, Hex(0xF4EFD8));
                    }

                    break;

                case 12: // Arazá: fruto aterciopelado verde-amarillo.
                    pc.FillCapsule(ox + 33f, oy + 50f, ox + 35f, oy + 56f, 1.6f, Hex(0x6B4A2B));
                    Outlined(pc, ox + 32f, oy + 29f, 21f, 19f, Hex(0xB5C43C));
                    pc.FillEllipseSoft(ox + 36f, oy + 26f, 16f, 13f, 6f, new Color(0.55f, 0.62f, 0.12f, 0.45f));
                    pc.FillEllipseSoft(ox + 24f, oy + 36f, 8f, 6f, 5f, new Color(1f, 1f, 0.82f, 0.75f));
                    break;

                case 13: // Cacao: trozo de mazorca amarillo-naranja con la pulpa blanca. Nunca un grano suelto.
                    FillSector(pc, ox + 10f, oy + 8f, 50f, 20f, 70f, Ink, 2f);
                    FillSector(pc, ox + 10f, oy + 8f, 50f, 20f, 70f, Hex(0xD99A2E), 0f);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = (28f + (i * 12f)) * Mathf.Deg2Rad;
                        pc.FillCapsule(ox + 10f + (Mathf.Cos(a) * 26f), oy + 8f + (Mathf.Sin(a) * 26f), ox + 10f + (Mathf.Cos(a) * 48f), oy + 8f + (Mathf.Sin(a) * 48f), 1.2f, Hex(0xB37422)); // costillas
                    }

                    FillSector(pc, ox + 10f, oy + 8f, 30f, 22f, 68f, Hex(0xF4EEE0), 0f); // pulpa blanca en el corte
                    for (int i = 0; i < 3; i++)
                    {
                        float a = (34f + (i * 14f)) * Mathf.Deg2Rad;
                        pc.FillEllipseSoft(ox + 10f + (Mathf.Cos(a) * 20f), oy + 8f + (Mathf.Sin(a) * 20f), 4f, 3f, 2f, new Color(0.86f, 0.82f, 0.74f, 0.9f));
                    }

                    break;

                default: // 14 · Agua del Río Negro: hoja seca, estrecha, parda y rizada (se hunde; nunca verde ni posada).
                    DrawLeaf(pc, new Vector2(ox + 14f, oy + 10f), new Vector2(0.62f, 0.78f), 48f, 21f, -0.28f, LeafShape.Lanceolate, Hex(0x6E4520), Hex(0xB07A40), 0.6f, 14);
                    pc.FillCapsule(ox + 12f, oy + 10f, ox + 9f, oy + 5f, 1.3f, Hex(0x5A3A1A));
                    break;
            }
        }

        // Elipse con contorno de tinta de 2 px en una sola pasada: el interior cubierto del todo se escribe sin mezclar.
        private static void Outlined(PixelCanvas pc, float cx, float cy, float rx, float ry, Color fill)
        {
            float ox = rx + 2f;
            float oy = ry + 2f;
            int x0 = Mathf.FloorToInt(cx - ox - 2f);
            int x1 = Mathf.CeilToInt(cx + ox + 2f);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - oy - 2f));
            int y1 = Mathf.Min(pc.Height - 1, Mathf.CeilToInt(cy + oy + 2f));
            x0 = Mathf.Max(0, x0);
            x1 = Mathf.Min(pc.Width - 1, x1);
            float rin = Mathf.Min(rx, ry);
            float rout = Mathf.Min(ox, oy);
            for (int y = y0; y <= y1; y++)
            {
                float py = (y + 0.5f) - cy;
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - cx;
                    float din = (Mathf.Sqrt(((px / rx) * (px / rx)) + ((py / ry) * (py / ry))) - 1f) * rin;
                    float fillCov = PixelCanvas.SoftCoverage(din, 1f);
                    if (fillCov >= 1f && fill.a >= 1f)
                    {
                        pc.Set(x, y, fill);
                        continue;
                    }

                    float dout = (Mathf.Sqrt(((px / ox) * (px / ox)) + ((py / oy) * (py / oy))) - 1f) * rout;
                    float inkCov = PixelCanvas.SoftCoverage(dout, 1f);
                    if (inkCov > 0f)
                    {
                        pc.Blend(x, y, Ink, inkCov);
                    }

                    if (fillCov > 0f)
                    {
                        pc.Blend(x, y, fill, fillCov);
                    }
                }
            }
        }

        // Baya brillante con contorno, en una sola pasada.
        private static void Berry(PixelCanvas pc, float cx, float cy, float r, Color fill)
        {
            float ro = r + 1.8f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - ro - 1f));
            int x1 = Mathf.Min(pc.Width - 1, Mathf.CeilToInt(cx + ro + 1f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ro - 1f));
            int y1 = Mathf.Min(pc.Height - 1, Mathf.CeilToInt(cy + ro + 1f));
            for (int y = y0; y <= y1; y++)
            {
                float py = (y + 0.5f) - cy;
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - cx;
                    float d = Mathf.Sqrt((px * px) + (py * py));
                    float fillCov = PixelCanvas.Coverage(d - r);
                    if (fillCov >= 1f)
                    {
                        pc.Set(x, y, fill);
                        continue;
                    }

                    float inkCov = PixelCanvas.Coverage(d - ro);
                    if (inkCov > 0f)
                    {
                        pc.Blend(x, y, Ink, inkCov);
                    }

                    if (fillCov > 0f)
                    {
                        pc.Blend(x, y, fill, fillCov);
                    }
                }
            }

            pc.FillEllipseSoft(cx - (r * 0.35f), cy + (r * 0.35f), r * 0.35f, r * 0.28f, 2f, new Color(1f, 1f, 1f, 0.75f));
        }

        private static void Seed(PixelCanvas pc, float cx, float cy, float rx, float ry, Color fill)
        {
            pc.FillEllipseSoft(cx, cy, rx, ry, 1f, fill);
            pc.FillCircle(cx - (rx * 0.3f), cy + (ry * 0.3f), 0.8f, new Color(1f, 1f, 1f, 0.6f));
        }

        // Semillita angulosa (rombo irregular) con brillo.
        private static void Angular(PixelCanvas pc, float cx, float cy, float r, float angle, Color fill)
        {
            var a = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var b = new Vector2(-a.y, a.x);
            var c = new Vector2(cx, cy);
            pc.FillTriangle(c + (a * r), c + (b * r * 0.7f), c - (a * r * 0.8f), fill);
            pc.FillTriangle(c + (a * r), c - (b * r * 0.75f), c - (a * r * 0.8f), fill);
            pc.FillCircle(cx + (a.x * r * 0.3f), cy + (a.y * r * 0.3f), 0.8f, new Color(1f, 0.8f, 0.7f, 0.7f));
        }

        private static void FruitDrop(PixelCanvas pc, float cx, float cy, float r, Color fill)
        {
            pc.FillCircle(cx, cy, r + 1.8f, Ink);
            pc.FillTriangle(new Vector2(cx, cy + (r * 2.3f) + 2f), new Vector2(cx - r - 1.6f, cy + 1f), new Vector2(cx + r + 1.6f, cy + 1f), Ink);
            pc.FillCircle(cx, cy, r, fill);
            pc.FillTriangle(new Vector2(cx, cy + (r * 2.2f)), new Vector2(cx - (r * 0.95f), cy + 1.5f), new Vector2(cx + (r * 0.95f), cy + 1.5f), fill);
            pc.FillEllipseSoft(cx - (r * 0.35f), cy + (r * 0.2f), r * 0.28f, r * 0.45f, 2f, new Color(1f, 1f, 1f, 0.8f));
        }

        private static bool Inside(float dx, float dy, float rx, float ry)
        {
            return ((dx * dx) / (rx * rx)) + ((dy * dy) / (ry * ry)) < 1f;
        }

        // Sector circular de centro (cx, cy) y radio r entre fromDeg y toDeg (antihorario), con antialias en los tres bordes;
        // grow > 0 lo agranda (contorno).
        private static void FillSector(PixelCanvas pc, float cx, float cy, float r, float fromDeg, float toDeg, Color c, float grow)
        {
            float a0 = fromDeg * Mathf.Deg2Rad;
            float a1 = toDeg * Mathf.Deg2Rad;
            var e0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
            var e1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            // Caja del sector: el centro, los dos extremos del arco y los ejes que caen dentro del ángulo.
            float bx0 = Mathf.Min(0f, Mathf.Min(e0.x, e1.x) * r);
            float bx1 = Mathf.Max(0f, Mathf.Max(e0.x, e1.x) * r);
            float by0 = Mathf.Min(0f, Mathf.Min(e0.y, e1.y) * r);
            float by1 = Mathf.Max(0f, Mathf.Max(e0.y, e1.y) * r);
            for (int q = 0; q < 4; q++)
            {
                float a = q * Mathf.PI * 0.5f;
                if ((a >= a0 && a <= a1) || (a + (Mathf.PI * 2f) >= a0 && a + (Mathf.PI * 2f) <= a1))
                {
                    bx0 = Mathf.Min(bx0, Mathf.Cos(a) * r);
                    bx1 = Mathf.Max(bx1, Mathf.Cos(a) * r);
                    by0 = Mathf.Min(by0, Mathf.Sin(a) * r);
                    by1 = Mathf.Max(by1, Mathf.Sin(a) * r);
                }
            }

            int x0 = Mathf.FloorToInt(cx + bx0 - grow - 2f);
            int x1 = Mathf.CeilToInt(cx + bx1 + grow + 2f);
            int y0 = Mathf.FloorToInt(cy + by0 - grow - 2f);
            int y1 = Mathf.CeilToInt(cy + by1 + grow + 2f);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - cx;
                    float py = (y + 0.5f) - cy;
                    float d = Mathf.Sqrt((px * px) + (py * py)) - r - grow;
                    float s0 = -((e0.x * py) - (e0.y * px)) - grow; // lado interior del borde de inicio
                    float s1 = ((e1.x * py) - (e1.y * px)) - grow;
                    float dist = Mathf.Max(d, Mathf.Max(s0, s1));
                    pc.Blend(x, y, c, PixelCanvas.Coverage(dist));
                }
            }
        }
    }
}
