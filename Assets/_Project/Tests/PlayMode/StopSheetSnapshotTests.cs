#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Las 20 composiciones del fondo por parada y su hoja para VA-20 (tareas M1.4 y M2.4; direccion-visual-v2 §9; propuesta del
    /// Mapa §10.2: «la herramienta de capturas genera una hoja con las 20 composiciones»; 07 §2.1): el dueño y el asesor la
    /// revisan antes de encargar los héroes. Cada parada k de 1-200 va con su silueta propia (<see cref="StopShapes"/>; arte
    /// provisional, solo en el Editor y en desarrollo, nunca en la ficha, las capturas de la tienda ni la UA) en el tablero de 2
    /// filas del nivel 7 (7 cuias, 4 + 3) puesto en el nivel <c>10k + 7</c> con el agua base de su mundo
    /// (<see cref="PlayModeHelpers.LoadStopBoard"/>). Es el único fixture que escribe las 20 composiciones (revisión de M2.4),
    /// y la hoja las reutiliza:
    /// <list type="bullet">
    /// <item><c>02f_board_stop_&lt;kk&gt;</c>: las 20 composiciones a pantalla completa (1080 × 2340), con el HUD y el tablero;</item>
    /// <item><c>02f_board_stops_sheet</c>: la hoja única de 5 × 4, con las paradas en orden (0-4 en la primera fila, 15-19 en la
    /// última) y, de cada composición, solo la banda del héroe (<see cref="CropWidthDp"/> × <see cref="CropHeightDp"/> dp: de
    /// 48 dp sobre el horizonte a 32 bajo él, con el reflejo) a 1,5 px/dp, con el número de la parada arriba y un borde ámbar
    /// en las que exigen asesor (<see cref="AdvisorStops"/>: Tefé, Puerto Nariño y el Itaya). La banda sale de una segunda
    /// captura de la misma composición con la cabecera (pausa, nivel, monedas, Canoas y Reiniciar) y el follaje del marco
    /// ocultos (<see cref="HideChrome"/>; revisión de M2.4), así que la hoja no lleva texto ni UI del juego y deja ver la silueta
    /// entera. La leyenda (número → id del sitio → forma) sale en el log en las líneas <c>AF_SHEET</c> y en
    /// <c>docs/dev/prototipo-v0.md</c>; la parada 11 lleva la alternativa de 07 §2.1 (solo la boca, sin el pueblo), y la
    /// composición canónica, con el pueblo lejano, la decide el asesor;</item>
    /// <item><c>02f_board_stop_15_reserva</c>: la parada 15 con la reserva de su héroe (la Casa de Fierro), fuera de la hoja.</item>
    /// </list>
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.StopSheetSnapshotTests</c>
    /// sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Las 20 composiciones del fondo por parada y su hoja (M1.4, M2.4, VA-20); necesita dispositivo gráfico.")]
    public sealed class StopSheetSnapshotTests
    {
        /// <summary>Columnas de la hoja.</summary>
        public const int Columns = 5;

        /// <summary>Filas de la hoja.</summary>
        public const int Rows = 4;

        /// <summary>Ancho de la banda del héroe que entra en la hoja (dp, centrada en el pie del héroe: x de 40 a 320).</summary>
        public const int CropWidthDp = 280;

        /// <summary>Alto de la banda del héroe (dp): de <see cref="StopBackdrop.HeroAboveDp"/> sobre el horizonte a 32 bajo él.</summary>
        public const int CropHeightDp = 80;

        /// <summary>Reducción de cada captura en la hoja (2: de 3 px/dp a 1,5 px/dp).</summary>
        public const int Downscale = 2;

        /// <summary>Separación entre las casillas (px).</summary>
        public const int Gutter = 8;

        /// <summary>Alto de la etiqueta con el número de la parada sobre cada casilla (px).</summary>
        public const int LabelHeight = 28;

        /// <summary>Parada que va con la alternativa de 07 §2.1 (Puerto Nariño: solo la boca del río, sin el pueblo).</summary>
        public const int AlternativeStop = 11;

        /// <summary>Grosor del borde de las casillas de asesor obligatorio (px).</summary>
        public const int AdvisorBorder = 4;

        /// <summary>Píxeles de la captura por dp (1080 / 360).</summary>
        public const int CapturePxPerDp = SnapshotKit.Width / 360;

        /// <summary>Ancho de una casilla (px): 420.</summary>
        public const int TileWidth = CropWidthDp * CapturePxPerDp / Downscale;

        /// <summary>Alto de una casilla (px): 120.</summary>
        public const int TileHeight = CropHeightDp * CapturePxPerDp / Downscale;

        /// <summary>Ancho de la hoja (px).</summary>
        public const int SheetWidth = (Columns * TileWidth) + ((Columns + 1) * Gutter);

        /// <summary>Alto de la hoja (px).</summary>
        public const int SheetHeight = (Rows * (LabelHeight + TileHeight)) + ((Rows + 1) * Gutter);

        /// <summary>
        /// Paradas con asesor obligatorio: 11 (Puerto Nariño) y 15 (Itaya), que marca 07 §2.1, y 7 (Tefé), que marca la propuesta
        /// del Mapa §10.3 (fila 7; 07 §2.1 no la marca: discrepancia que confirma el dueño).
        /// </summary>
        public static readonly int[] AdvisorStops = { 7, 11, 15 };

        private const int DigitScale = 4;

        private static readonly Color32 SheetBackground = new Color32(0x22, 0x30, 0x3A, 0xFF);
        private static readonly Color32 LabelInk = new Color32(0xF6, 0xEB, 0xD0, 0xFF);
        private static readonly Color32 AdvisorInk = new Color32(0xE0, 0xA0, 0x30, 0xFF);

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

            var legend = new StringBuilder();
            var capture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                for (int n = 0; n < Columns * Rows; n++)
                {
                    yield return LoadStop(n);
                    GameScreen view = GameBootstrap.Current!.View!;
                    StopBackdrop b = view.StopBackdrop;
                    RiverStop stop = b.Stop!;
                    Assert.That(stop.N, Is.EqualTo(n));
                    string? shape = b.Shape;
                    AssertOwnShape(n, stop.Backdrop, b, shape);
                    string name = "02f_board_stop_" + n.ToString("00", CultureInfo.InvariantCulture);
                    File.WriteAllBytes(Path.Combine(dir, name + ".png"), SnapshotKit.Capture());

                    // Para la hoja, la misma composición sin la cabecera ni el follaje del marco (revisión de M2.4).
                    byte[] png;
                    float[] previous = HideChrome(view);
                    try
                    {
                        png = SnapshotKit.Capture();
                    }
                    finally
                    {
                        RestoreChrome(view, previous);
                    }

                    Assert.That(capture.LoadImage(png), Is.True, name);
                    Assert.That(capture.width, Is.EqualTo(SnapshotKit.Width), name);
                    Assert.That(capture.height, Is.EqualTo(SnapshotKit.Height), name);

                    // La banda del héroe: de 48 dp sobre el horizonte a 32 bajo él, y x de 40 a 320 dp.
                    int top = Mathf.RoundToInt((view.HorizonY - StopBackdrop.HeroAboveDp) * CapturePxPerDp);
                    int left = ((360 - CropWidthDp) / 2) * CapturePxPerDp;
                    Assert.That(top, Is.GreaterThanOrEqualTo(0), name);
                    Assert.That(top + (CropHeightDp * CapturePxPerDp), Is.LessThanOrEqualTo(SnapshotKit.Height), name);
                    Paste(sheet, capture.GetPixels32(), capture.width, capture.height, n, left, top);
                    Label(sheet, n);
                    if (Array.IndexOf(AdvisorStops, n) >= 0)
                    {
                        Border(sheet, n);
                    }

                    string entry = n.ToString("00", CultureInfo.InvariantCulture) + " " + stop.Id + " " + (shape ?? b.Code ?? "-")
                        + (stop.Backdrop.Lut != null ? " (" + stop.Backdrop.Lut + ")" : string.Empty)
                        + (Array.IndexOf(AdvisorStops, n) >= 0 ? " [asesor]" : string.Empty)
                        + (n == AlternativeStop
                            ? " [alternativa de 07 §2.1: solo la boca, sin el pueblo; la canónica, con el pueblo lejano, la decide el asesor]"
                            : string.Empty);
                    legend.Append(entry).Append('\n');
                    Debug.Log("AF_STOP " + name + ": héroe " + b.HeroSource + " " + (shape ?? b.Code ?? "-") + ", " + view.LastStopProbe);
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
            Debug.Log("AF_SHEET 02f_board_stops_sheet: " + (Columns * Rows) + " composiciones, " + SheetWidth + " x " + SheetHeight
                + " px; casilla " + TileWidth + " x " + TileHeight + " px (banda del héroe a 1,5 px/dp, sin la cabecera ni el follaje"
                + " del marco); borde ámbar = asesor obligatorio");
            foreach (string line in legend.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Debug.Log("AF_SHEET " + line);
            }

            Assert.That(File.Exists(sheetPath), Is.True);
            Assert.That(File.Exists(Path.Combine(dir, "02f_board_stop_19.png")), Is.True);
        }

        // Toda parada con héroe pintado o con pieza del kit lleva su silueta propia (no genérica): «ninguna parada pintada usa una
        // forma genérica» (M2.4). Solo las dos líneas entre aguas (codigo:) van sin silueta.
        private static void AssertOwnShape(int n, RiverStopBackdrop backdrop, StopBackdrop b, string? shape)
        {
            bool kit = backdrop.Code != null && backdrop.Code.StartsWith("kit:", StringComparison.Ordinal);
            if (backdrop.Hero != null || kit)
            {
                Assert.That(b.HeroSource, Is.EqualTo(StopBackdrop.HeroShape), "parada " + n + ": con su silueta");
                Assert.That(shape, Is.Not.Null, "parada " + n);
                Assert.That(StopShapes.IsGeneric(shape), Is.False, "parada " + n + ": sin forma genérica");
                return;
            }

            bool code = backdrop.Code != null && backdrop.Code.StartsWith("codigo:", StringComparison.Ordinal);
            Assert.That(code, Is.True, "parada " + n + ": solo las líneas entre aguas van sin silueta");
            Assert.That(shape, Is.Null, "parada " + n);
        }

        // Lo que el juego pinta delante de la banda del héroe y no se juzga en la hoja: la cabecera (la capa del HUD, con la placa
        // del nivel y Reiniciar, y la capa superior, con la pausa, las monedas y las Canoas) y el follaje de las cuatro esquinas.
        private static readonly string[] ChromePaths = { "TopLayer", "Scene/FoliageTL", "Scene/FoliageTR", "Scene/FoliageBL", "Scene/FoliageBR" };

        // Oculta (CanvasGroup al alfa 0) la cabecera y el follaje del marco para la captura de la hoja; devuelve el alfa que tenía
        // cada uno (el HUD primero, luego ChromePaths), para RestoreChrome.
        private static float[] HideChrome(GameScreen view)
        {
            var previous = new float[ChromePaths.Length + 1];
            for (int i = 0; i < previous.Length; i++)
            {
                CanvasGroup group = ChromeGroup(view, i);
                previous[i] = group.alpha;
                group.alpha = 0f;
            }

            return previous;
        }

        // Deshace HideChrome: cada grupo, con su alfa de antes.
        private static void RestoreChrome(GameScreen view, float[] previous)
        {
            for (int i = 0; i < previous.Length; i++)
            {
                ChromeGroup(view, i).alpha = previous[i];
            }
        }

        // El CanvasGroup del elemento i de la cabecera o el follaje (0 = el HUD); lo crea si no lo tiene.
        private static CanvasGroup ChromeGroup(GameScreen view, int i)
        {
            Transform? t = i == 0 ? view.Hud : view.Canvas.transform.Find(ChromePaths[i - 1]);
            Assert.That(t, Is.Not.Null, i == 0 ? "Hud" : ChromePaths[i - 1]);
            return t!.TryGetComponent(out CanvasGroup group) ? group : t.gameObject.AddComponent<CanvasGroup>();
        }

        // La parada n en su tablero de 2 filas, con la intro terminada.
        private static IEnumerator LoadStop(int n)
        {
            yield return PlayModeHelpers.LoadStopBoard(n);
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
        /// empieza ahí y la banda del héroe, <see cref="LabelHeight"/> px más abajo.
        /// </summary>
        public static Vector2Int TileOrigin(int index)
        {
            int col = index % Columns;
            int row = index / Columns;
            return new Vector2Int(Gutter + (col * (TileWidth + Gutter)), Gutter + (row * (LabelHeight + TileHeight + Gutter)));
        }

        // Pega la banda de la captura que empieza en (left, top) px desde arriba (las filas de la captura van de abajo arriba,
        // como las da Unity) reducida a la mitad, con la media de cada bloque de 2 × 2 px.
        private static void Paste(Color32[] sheet, Color32[] src, int srcW, int srcH, int index, int left, int top)
        {
            Vector2Int o = TileOrigin(index);
            int tileTop = o.y + LabelHeight;
            for (int ty = 0; ty < TileHeight; ty++)
            {
                int sy = top + (ty * Downscale);
                for (int tx = 0; tx < TileWidth; tx++)
                {
                    int sx = left + (tx * Downscale);
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
                    SetTop(sheet, o.x + tx, tileTop + ty, new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 0xFF));
                }
            }
        }

        // Borde ámbar de AdvisorBorder px alrededor de la casilla (etiqueta y banda), dentro del margen entre casillas.
        private static void Border(Color32[] sheet, int index)
        {
            Vector2Int o = TileOrigin(index);
            int x0 = o.x - AdvisorBorder;
            int y0 = o.y - AdvisorBorder;
            int x1 = o.x + TileWidth + AdvisorBorder;
            int y1 = o.y + LabelHeight + TileHeight + AdvisorBorder;
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    bool inside = x >= o.x && x < o.x + TileWidth && y >= o.y && y < o.y + LabelHeight + TileHeight;
                    if (!inside)
                    {
                        SetTop(sheet, x, y, AdvisorInk);
                    }
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
