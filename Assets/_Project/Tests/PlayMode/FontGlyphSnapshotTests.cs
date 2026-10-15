#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de la tarea B1 (glifos de los atlas calculados desde las tablas; D-018, propuesta de idiomas §4.3;
    /// <c>[Explicit]</c>, no forman parte de la suite), en en-XA y en es-419, a 360 × 780 dp:
    /// <list type="bullet">
    /// <item><description><c>b1_01a_glifos_baloo2</c> y <c>b1_01b_glifos_nunito</c>: los estilos de texto (<see cref="TextStyle"/>)
    /// de cada familia sobre fondo negro, cada uno con los caracteres latinos ampliados de su atlas (Latin-1 de ¡ a ÿ en tres
    /// filas fijas y con un espacio fino entre cada carácter, la
    /// puntuación tipográfica, cifras con sus separadores y, en Baloo 2, «+20 → +40» de la tabla) y dos textos de la tabla (en
    /// en-XA, alargados, con acentos y entre corchetes);</description></item>
    /// <item><description><c>b1_02_inicio_l4</c>: el inicio con 1.240 semillas; y</description></item>
    /// <item><description><c>b1_03_victoria_l11</c>: la victoria del 11 con la Cosecha doble «+20 → +40», el único texto de las
    /// tablas que lleva «→» (en Baloo 2).</description></item>
    /// </list>
    /// En cada captura comprueba además que ningún carácter visible de un texto con una fuente del juego (Nunito o Baloo 2) sale
    /// de la fuente de reserva de TMP ni como □ (U+25A1): todos están en el atlas estático de su fuente.
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.FontGlyphSnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/&lt;idioma&gt;/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales (B1); necesitan dispositivo gráfico.")]
    public sealed class FontGlyphSnapshotTests
    {
        private const char MissingGlyph = '□';

        // Puntuación tipográfica fija de los atlas (la de AF.EditorTools.FontSetup.Typography).
        private const string Typography = "–—‘’“”…•";

        private GameObject? _sheet;

        [TearDown]
        public void TearDown()
        {
            if (_sheet != null)
            {
                Object.Destroy(_sheet);
                _sheet = null;
            }

            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Capture_EnXA()
        {
            yield return Walk(LocLocale.Pseudo);
        }

        [UnityTest]
        public IEnumerator Capture_Es419()
        {
            yield return Walk(LocLocale.SourceLocale);
        }

        private IEnumerator Walk(string locale)
        {
            FunnelSetup.Hooks(); // antes de Prepare: limpia los ganchos y fijaría es-419
            string dir = SnapshotKit.Prepare(locale);
            var problems = new List<string>();

            // Hojas de glifos sobre el fondo negro de la cámara, una por familia.
            foreach (bool title in new[] { true, false })
            {
                string name = title ? "b1_01a_glifos_baloo2" : "b1_01b_glifos_nunito";
                _sheet = BuildSheet(locale, title);
                yield return SnapshotKit.Wait(0.2f);
                SnapshotKit.Save(dir, name);
                Scan(name, problems);
                Object.Destroy(_sheet);
                _sheet = null;
                yield return null;
            }

            // Inicio.
            Profile p4 = Wallet.NewProfile(FunnelSetup.Clock);
            p4.Progress.NextLevel = 4;
            p4.Progress.MaxLevel = 3;
            p4.Wallet.Seeds = 1240;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p4));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "b1_02_inicio_l4");
            Scan("b1_02_inicio_l4", problems);

            // Victoria del 11 (tablero 1 con af_level_offset = 10): Cosecha y Cosecha doble «+20 → +40».
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            boot = GameBootstrap.Current!;
            yield return WinRealTime(boot);
            yield return SnapshotKit.Wait(1.2f);
            SnapshotKit.Attach(boot);
            if (boot.AdOverlayView != null)
            {
                SnapshotKit.AttachOverlay(boot.AdOverlayView.Canvas);
            }

            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "b1_03_victoria_l11");
            Assert.That(boot.View!.DoubleHarvestShown, Is.True, "la Cosecha doble en el 11");
            Assert.That(boot.View.DoubleHarvestGainText, Does.Contain("→"), "«+20 → +40»");
            Scan("b1_03_victoria_l11", problems);

            Debug.Log("AF_GLYPHS " + locale + ": " + problems.Count.ToString(CultureInfo.InvariantCulture) + " caracteres fuera del atlas.");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // Hoja: un rótulo por estilo de la familia con los caracteres latinos ampliados de su atlas y textos de la tabla.
        private static GameObject BuildSheet(string locale, bool titleFamily)
        {
            var go = new GameObject("GlyphSheet", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = go.GetComponent<Canvas>();
            SnapshotKit.AttachOverlay(canvas);
            canvas.planeDistance = 1f;
            canvas.scaleFactor = 3f; // 1 dp = 3 px en la captura de 1080 × 2340
            canvas.sortingOrder = 1000;

            string latin = LatinRows();

            string numbers = Loc.Number(1234567) + " · " + Loc.Number(-3.5, 1);
            string sentence = Loc.T(UiText.Keys.DoubleHarvestDelta, LocArgs.Of("n", 20).And("doubled", 40));
            float y = -48f;
            var styles = (TextStyle[])Enum.GetValues(typeof(TextStyle));
            for (int i = 0; i < styles.Length; i++)
            {
                TextStyle style = styles[i];
                bool title = IsTitle(style);
                if (title != titleFamily)
                {
                    continue;
                }

                string sample = style + "  " + locale + "\n" + latin + "\n" + Typography + "  " + numbers
                    + (title ? "  " + sentence : string.Empty) + "\n" + Loc.T(UiText.Keys.Back) + " · " + UiText.Play;
                TextMeshProUGUI label = UiBuilder.Label("Glyphs_" + style, go.transform, style, sample, 13f, Palette.Cream, TextAlignmentOptions.TopLeft);
                label.textWrappingMode = TextWrappingModes.Normal;
                RectTransform rt = label.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(6f, y);
                rt.sizeDelta = new Vector2(348f, 140f); // la fila más ancha (À-ß en Nunito ExtraBold) mide unos 342 dp
                y -= 146f;

                // Las filas son fijas: si alguna no cupiera en el ancho, TMP la partiría y volvería a quedar una letra suelta.
                label.ForceMeshUpdate();
                int lines = sample.Split('\n').Length;
                Assert.That(label.textInfo.lineCount, Is.EqualTo(lines), "hoja de glifos " + style + ": ninguna fila se parte");
            }

            return go;
        }

        // Latin-1 de ¡ a ÿ (sin el guion blando U+00AD), en tres filas fijas de unos 32 caracteres (¡-¿, À-ß y à-ÿ) y con un
        // espacio fino entre cada carácter (<cspace>, sin añadir ningún carácter que tenga que estar en el atlas): así cada
        // letra con diacrítico se ve suelta, también las I estrechas (ÌÍÎÏ, ìíîï), cuyos acentos se juntaban seguidas.
        private static string LatinRows()
        {
            var sb = new StringBuilder("<cspace=0.15em>");
            for (int c = 0xA1; c <= 0xFF; c++)
            {
                if (c == 0xAD)
                {
                    continue;
                }

                if (c == 0xC0 || c == 0xE0)
                {
                    sb.Append('\n');
                }

                sb.Append((char)c);
            }

            return sb.Append("</cspace>").ToString();
        }

        // Estilos que pinta Baloo 2 (UiFonts): Ribbon, Badge, ModalTitle, CtaLeaf y CtaWater.
        private static bool IsTitle(TextStyle style)
        {
            return style == TextStyle.Ribbon || style == TextStyle.Badge || style == TextStyle.ModalTitle
                || style == TextStyle.CtaLeaf || style == TextStyle.CtaWater;
        }

        // Todo carácter visible de un texto activo con una fuente del juego sale de su atlas: ni de la reserva de TMP ni como □.
        private static void Scan(string capture, List<string> problems)
        {
            TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int scanned = 0;
            for (int t = 0; t < texts.Length; t++)
            {
                TMP_Text text = texts[t];
                if (!text.isActiveAndEnabled || text.font == null)
                {
                    continue;
                }

                string fontName = text.font.name;
                if (!fontName.StartsWith("Nunito", StringComparison.Ordinal) && !fontName.StartsWith("Baloo2", StringComparison.Ordinal))
                {
                    continue;
                }

                text.ForceMeshUpdate();
                TMP_TextInfo info = text.textInfo;
                scanned++;
                for (int i = 0; i < info.characterCount; i++)
                {
                    TMP_CharacterInfo ci = info.characterInfo[i];
                    if (ci.elementType != TMP_TextElementType.Character || char.IsControl(ci.character))
                    {
                        continue;
                    }

                    if (ci.character == MissingGlyph || ci.fontAsset != text.font || !text.font.HasCharacter(ci.character, false, false))
                    {
                        problems.Add(capture + ": «" + ci.character + "» (U+" + ((int)ci.character).ToString("X4", CultureInfo.InvariantCulture)
                            + ") en " + text.name + " (" + fontName + ", desde " + (ci.fontAsset != null ? ci.fontAsset.name : "-") + "): «" + text.text + "»");
                    }
                }
            }

            Assert.That(scanned, Is.GreaterThan(0), capture + ": textos mirados");
        }

        // Victoria con la solución del solver, en tiempo real.
        private static IEnumerator WinRealTime(GameBootstrap boot)
        {
            boot.TapAnywhere();
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            for (int i = 0; i < solution.MoveCount; i++)
            {
                boot.TapCuia(solution.GetMove(i).Source);
                boot.TapCuia(solution.GetMove(i).Target);
            }

            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.VictoryScreen, "victoria");
        }
    }
}
