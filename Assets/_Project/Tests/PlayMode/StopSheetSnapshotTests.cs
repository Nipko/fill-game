#nullable enable
using System.Collections;
using System.Globalization;
using System.IO;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Hoja de las 20 composiciones del fondo por parada para VA-20 (tarea M2.4; direccion-visual-v2 §9; propuesta del Mapa §10.2:
    /// «la herramienta de capturas genera una hoja con las 20 composiciones»; 07 §2.1): el dueño y el asesor la revisan antes de
    /// encargar los héroes. Cada parada k de 1-200 va con su silueta propia (<see cref="StopShapes"/>; arte provisional, solo en
    /// el Editor y en desarrollo, nunca en la ficha, las capturas de la tienda ni la UA) en el tablero de 2 filas del nivel 7
    /// (7 cuias, 4 + 3) puesto en el nivel <c>10k + 7</c> con el agua base de su mundo, como <see cref="StopSnapshotTests"/>:
    /// <list type="bullet">
    /// <item><c>02f_board_stop_&lt;kk&gt;</c>: las 20 composiciones a pantalla completa (1080 × 2340);</item>
    /// <item><c>02f_board_stops_sheet</c>: la hoja única de 5 × 4, con las paradas en orden (0-4 en la primera fila, 15-19 en la
    /// última), cada una con su número arriba y, de cada captura, la franja de arriba (0-240 dp: el HUD, el cielo, el héroe con su
    /// reflejo, la banda de agua y el borde del panel) a media resolución (1,5 px/dp);</item>
    /// <item><c>02f_board_stop_15_reserva</c>: la parada 15 con la reserva de su héroe (la Casa de Fierro), fuera de la hoja.</item>
    /// </list>
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.StopSheetSnapshotTests</c>
    /// sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Hoja de las 20 composiciones del fondo por parada (M2.4, VA-20); necesita dispositivo gráfico.")]
    public sealed class StopSheetSnapshotTests
    {
        /// <summary>Columnas de la hoja.</summary>
        public const int Columns = 5;

        /// <summary>Filas de la hoja.</summary>
        public const int Rows = 4;

        /// <summary>Alto de la franja de cada captura que entra en la hoja (dp desde arriba).</summary>
        public const int CropDp = 240;

        /// <summary>Reducción de cada captura en la hoja (2 = media resolución).</summary>
        public const int Downscale = 2;

        /// <summary>Separación entre las casillas (px).</summary>
        public const int Gutter = 8;

        /// <summary>Alto de la etiqueta con el número de la parada sobre cada casilla (px).</summary>
        public const int LabelHeight = 28;

        /// <summary>Ancho de una casilla (px).</summary>
        public const int TileWidth = SnapshotKit.Width / Downscale;

        /// <summary>Alto de una casilla (px).</summary>
        public const int TileHeight = CropDp * SnapshotKit.Width / 360 / Downscale;

        /// <summary>Ancho de la hoja (px).</summary>
        public const int SheetWidth = (Columns * TileWidth) + ((Columns + 1) * Gutter);

        /// <summary>Alto de la hoja (px).</summary>
        public const int SheetHeight = (Rows * (LabelHeight + TileHeight)) + ((Rows + 1) * Gutter);

        private const int DigitScale = 4;

        private static readonly Color32 SheetBackground = new Color32(0x22, 0x30, 0x3A, 0xFF);
        private static readonly Color32 LabelInk = new Color32(0xF6, 0xEB, 0xD0, 0xFF);

        private static readonly string[] WorldIds = { string.Empty, "w01_estuario", "w02_tapajos", "w03_manaos", "w04_trapecio", "w05_loreto" };
        private static readonly string[] Waters = { string.Empty, "white", "clear", "black", "white", "white" };

        // Cifras de 3 × 5 (filas de arriba abajo, 3 bits cada una) para el número de la parada.
        private static readonly string[] Digits =
        {
            "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
            "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111",
        };

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureTheTwentyCompositions_AndTheSheet()
        {
            string dir = SnapshotKit.Prepare();
            var sheet = new Color32[SheetWidth * SheetHeight];
            for (int i = 0; i < sheet.Length; i++)
            {
                sheet[i] = SheetBackground;
            }

            var capture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                for (int n = 0; n < Columns * Rows; n++)
                {
                    yield return LoadStop(n);
                    GameScreen view = GameBootstrap.Current!.View!;
                    StopBackdrop b = view.StopBackdrop;
                    Assert.That(b.Stop!.N, Is.EqualTo(n));
                    string? shape = b.Shape;
                    Assert.That(shape == null || !StopShapes.IsGeneric(shape), Is.True, "parada " + n + ": sin forma genérica");
                    string name = "02f_board_stop_" + n.ToString("00", CultureInfo.InvariantCulture);
                    byte[] png = SnapshotKit.Capture();
                    File.WriteAllBytes(Path.Combine(dir, name + ".png"), png);
                    Assert.That(capture.LoadImage(png), Is.True, name);
                    Assert.That(capture.width, Is.EqualTo(SnapshotKit.Width), name);
                    Assert.That(capture.height, Is.EqualTo(SnapshotKit.Height), name);
                    Paste(sheet, capture.GetPixels32(), capture.width, capture.height, n);
                    Label(sheet, n);
                    Debug.Log("AF_STOP " + name + ": héroe " + b.HeroSource + " " + (shape ?? b.Code ?? "-") + ", "
                        + view.LastStopProbe);
                }

                // La reserva de la parada 15: la Casa de Fierro, en el mismo tablero (fuera de la hoja).
                yield return LoadStop(15);
                StopBackdrop backdrop = GameBootstrap.Current!.View!.StopBackdrop;
                backdrop.Apply(WithHero(backdrop.Stop!, "bg_hero_casa_de_fierro"), backdrop.Water, false, 0f);
                Assert.That(backdrop.Shape, Is.EqualTo(StopShapes.CasaDeFierro));
                SnapshotKit.Save(dir, "02f_board_stop_15_reserva");
            }
            finally
            {
                Object.Destroy(capture);
            }

            var output = new Texture2D(SheetWidth, SheetHeight, TextureFormat.RGB24, false);
            output.SetPixels32(sheet);
            output.Apply();
            string sheetPath = Path.Combine(dir, "02f_board_stops_sheet.png");
            File.WriteAllBytes(sheetPath, output.EncodeToPNG());
            Object.Destroy(output);
            Debug.Log("AF_SHEET 02f_board_stops_sheet: " + (Columns * Rows) + " composiciones, " + SheetWidth + " x " + SheetHeight + " px");
            Assert.That(File.Exists(sheetPath), Is.True);
            Assert.That(File.Exists(Path.Combine(dir, "02f_board_stop_19.png")), Is.True);
        }

        // Carga el tablero de 2 filas del nivel 7 en el nivel 10n + 7 (parada n) y espera al fin de la intro.
        private static IEnumerator LoadStop(int n)
        {
            int level = (10 * n) + 7;
            int world = WorldTable.WorldOf(level);
            LaunchOptions.LevelPackJsonOverride = PlayModeHelpers.Level7BoardPack(world, WorldIds[world], Waters[world], level);
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing, "fin de la intro (parada " + n + ")");
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(boot.View!.StopBackdrop.Stop, Is.Not.Null, "parada " + n);
        }

        // La misma parada con otro héroe pintado (la reserva).
        private static RiverStop WithHero(RiverStop s, string hero)
        {
            var facts = new string[s.Facts.Count];
            for (int i = 0; i < facts.Length; i++)
            {
                facts[i] = s.Facts[i];
            }

            return new RiverStop(s.N, s.World, s.Id, s.Kind, s.Country, s.CountryName, s.KmFromSea, s.KmHome, s.Sample, s.FlagRaise,
                s.ArrivalFact, facts, s.WorldQuestion, s.AnswersWorldQuestion,
                new RiverStopBackdrop(s.Backdrop.Kit, hero, null, s.Backdrop.Lut));
        }

        /// <summary>
        /// Esquina superior izquierda (px, desde arriba) de la casilla <paramref name="index"/> de la hoja, por filas: la etiqueta
        /// empieza ahí y la captura, <see cref="LabelHeight"/> px más abajo.
        /// </summary>
        public static Vector2Int TileOrigin(int index)
        {
            int col = index % Columns;
            int row = index / Columns;
            return new Vector2Int(Gutter + (col * (TileWidth + Gutter)), Gutter + (row * (LabelHeight + TileHeight + Gutter)));
        }

        // Pega la franja de arriba de la captura (filas de abajo arriba, como las da Unity) reducida a la mitad, con la media de
        // cada bloque de 2 × 2 px.
        private static void Paste(Color32[] sheet, Color32[] src, int srcW, int srcH, int index)
        {
            Vector2Int o = TileOrigin(index);
            int top = o.y + LabelHeight;
            for (int ty = 0; ty < TileHeight; ty++)
            {
                int sy = ty * Downscale;
                for (int tx = 0; tx < TileWidth; tx++)
                {
                    int sx = tx * Downscale;
                    int r = 0;
                    int g = 0;
                    int b = 0;
                    for (int dy = 0; dy < Downscale; dy++)
                    {
                        int row = (srcH - 1 - (sy + dy)) * srcW;
                        for (int dx = 0; dx < Downscale; dx++)
                        {
                            Color32 c = src[row + sx + dx];
                            r += c.r;
                            g += c.g;
                            b += c.b;
                        }
                    }

                    int count = Downscale * Downscale;
                    SetTop(sheet, o.x + tx, top + ty, new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 0xFF));
                }
            }
        }

        // El número de la parada (dos cifras) en la etiqueta de su casilla.
        private static void Label(Color32[] sheet, int index)
        {
            Vector2Int o = TileOrigin(index);
            int x = o.x + 6;
            int y = o.y + ((LabelHeight - (5 * DigitScale)) / 2);
            Digit(sheet, index / 10, x, y);
            Digit(sheet, index % 10, x + (4 * DigitScale), y);
        }

        private static void Digit(Color32[] sheet, int digit, int x, int y)
        {
            string bits = Digits[digit];
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (bits[(row * 3) + col] != '1')
                    {
                        continue;
                    }

                    for (int py = 0; py < DigitScale; py++)
                    {
                        for (int px = 0; px < DigitScale; px++)
                        {
                            SetTop(sheet, x + (col * DigitScale) + px, y + (row * DigitScale) + py, LabelInk);
                        }
                    }
                }
            }
        }

        // Escribe el píxel (x, y) de la hoja, con la y desde arriba (las filas de la textura van de abajo arriba).
        private static void SetTop(Color32[] sheet, int x, int y, Color32 c)
        {
            sheet[((SheetHeight - 1 - y) * SheetWidth) + x] = c;
        }
    }
}
