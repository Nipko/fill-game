#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Medidas y capturas de la ranura del Dato del río antes de fijar nada (tarea M1.3b, punto 2; 02 §6, filas «Dato del río» y
    /// Llegada; pregunta 11 del plan): con las cadenas del peor caso (90 caracteres en es-419 y 117 en en, pt-BR y en-XA) y una
    /// llegada maqueta con los elementos de 02 §6 (fila de 28 dp con el agua, la canoa y la mini cuia; bienvenida; placa
    /// «{lugar} · {país} · río {río}»; placa del dato con el retrato de 40 dp), a 360 × 780 y 360 × 640 dp (esta, 1080 × 1920
    /// px, que GameScreen maqueta en 438,75 × 780 dp; <see cref="HudLabelTests.Screen640"/>), al 100 % y al 130 %, en la
    /// victoria del 39 (chip del 40 y Cosecha doble). Mide el alto de la línea de fruta (H), el de la llegada completa y el
    /// de la llegada compacta de la opción (a) de la pregunta 11 (la fila del agua y una sola placa con el retrato, el lugar a
    /// 12 sp y el dato a 14, o a 12 como mínimo), el de la llegada ceñida a la forma de la línea de fruta (el lugar en la fila
    /// del agua y el dato a todo el ancho; la (a) más apretada [P]), y dónde quedan Siguiente, el panel y la cinta frente a la
    /// cabecera.
    /// <para>Escribe la tabla en <c>Logs/snapshots/slot_measures.txt</c> (y en el log, <c>AF_SLOT</c>) y las capturas
    /// <c>slot_{fruit|arrival|compact|tight}_{780|640}_{100|130}.png</c> en <c>Logs/snapshots/&lt;idioma&gt;/</c> (es-419 y en-XA).
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.VictorySlotMeasureSnapshotTests</c> sin <c>-nographics</c>). No falla por lo que mide: las cifras
    /// van a la pregunta 11.</para>
    /// </summary>
    [Explicit("Medidas y capturas de la ranura (M1.3b); necesitan dispositivo gráfico.")]
    public sealed class VictorySlotMeasureSnapshotTests
    {
        private static readonly string[] Locales =
        {
            LocLocale.SpanishLatinAmerica, LocLocale.English, LocLocale.PortugueseBrazil, LocLocale.Pseudo,
        };

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator MeasureFruitAndArrival_AndCapture()
        {
            var table = new List<string>
            {
                "pantalla | escala | idioma | H fruta | reserva | llegada completa | compacta 14 sp | compacta 12 sp | ceñida 14 sp | ceñida 12 sp"
                    + " | cabe completa / compacta 14 / 12 / ceñida 14 / 12"
                    + " | Siguiente (dp) | panel arriba | cinta arriba | cabecera | cinta bajo la cabecera | Cosecha doble | chip",
            };

            foreach (string captureLocale in new[] { LocLocale.SpanishLatinAmerica, LocLocale.Pseudo })
            {
                foreach (bool small in new[] { false, true })
                {
                    foreach (float scale in new[] { 1f, 1.3f })
                    {
                        FunnelSetup.Hooks();
                        string dir = SnapshotKit.Prepare(captureLocale);
                        if (small)
                        {
                            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
                        }

                        LaunchOptions.FontScaleOverride = scale;
                        LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
                        yield return PlayModeHelpers.LoadGame(39, new MemoryProgressStore(39, 38));
                        GameBootstrap boot = GameBootstrap.Current!;
                        PlayModeHelpers.TakeClock(boot);
                        GameScreen view = boot.View!;
                        var maqueta = new SlotMaqueta(captureLocale, scale);
                        view.RegisterVictoryLineProvider(maqueta);
                        SnapshotKit.Attach(boot);
                        VictorySetup.WinToVictoryScreen(boot);
                        PlayModeHelpers.Step(boot, 1800);

                        string screen = small ? "640" : "780";
                        string pct = scale > 1.01f ? "130" : "100";
                        float width = view.VictorySlotWidthDp;
                        float headerBottom = view.SlothBandTopDp;
                        float ribbonTop = view.VictoryPanelTopDp - 38f;
                        if (captureLocale == LocLocale.SpanishLatinAmerica)
                        {
                            // La tabla de los cuatro idiomas una vez por pantalla y escala (la geometría no depende del idioma de la
                            // captura; el alto que reserva sí: es el del idioma de la escena).
                            foreach (string locale in Locales)
                            {
                                float fruit = view.VictorySlotContentHeight(locale, scale);
                                float reserve = fruit + VictoryLineSlot.RuleGapDp + VictoryLineSlot.RuleDp + VictoryLineSlot.RuleBelowDp;
                                float full = maqueta.MeasureArrival(locale, width, false, VictoryLineSlot.TextSp);
                                float compact = maqueta.MeasureArrival(locale, width, true, VictoryLineSlot.TextSp);
                                float compact12 = maqueta.MeasureArrival(locale, width, true, UiBuilder.MinTextSp);
                                float tight = maqueta.MeasureTight(locale, width, VictoryLineSlot.TextSp);
                                float tight12 = maqueta.MeasureTight(locale, width, UiBuilder.MinTextSp);
                                float panelTop = view.VictoryPanelTopDp + view.VictorySlot.ReserveDp - reserve;
                                float next = view.NextTopDp + view.VictorySlot.ReserveDp - reserve;
                                table.Add(screen + " | " + pct + " | " + locale + " | " + F(fruit) + " | " + F(reserve) + " | " + F(full) + " | "
                                    + F(compact) + " | " + F(compact12) + " | " + F(tight) + " | " + F(tight12) + " | " + Yes(full <= fruit) + " / "
                                    + Yes(compact <= fruit) + " / " + Yes(compact12 <= fruit) + " / " + Yes(tight <= fruit) + " / " + Yes(tight12 <= fruit)
                                    + " | " + F(next) + " | " + F(panelTop) + " | " + F(panelTop - 38f) + " | "
                                    + F(headerBottom) + " | " + Yes(panelTop - 38f >= headerBottom) + " | " + Yes(view.DoubleHarvestShown)
                                    + " | " + Yes(view.NextChip.Visible));
                            }
                        }

                        Debug.Log("AF_SLOT " + captureLocale + " " + screen + " " + pct + ": H " + F(view.VictorySlot.ContentDp) + ", Siguiente "
                            + F(view.NextTopDp) + ", cinta " + F(ribbonTop) + ", cabecera " + F(headerBottom));
                        Assert.That(view.VictorySlot.Reserved, Is.True);

                        foreach (SlotMaqueta.Mode mode in new[] { SlotMaqueta.Mode.Fruit, SlotMaqueta.Mode.Arrival, SlotMaqueta.Mode.Compact, SlotMaqueta.Mode.Tight })
                        {
                            maqueta.Draw(mode);
                            yield return SnapshotKit.Wait(0.2f);
                            SnapshotKit.Save(dir, "slot_" + mode.ToString().ToLowerInvariant() + "_" + screen + "_" + pct);
                        }

                        SnapshotKit.Release();
                        LaunchOptions.Reset();
                    }
                }
            }

            string path = Path.Combine(Application.dataPath, "../Logs/snapshots", "slot_measures.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Join("\n", table) + "\n", new UTF8Encoding(false));
            foreach (string line in table)
            {
                Debug.Log("AF_SLOT " + line);
            }
        }

        private static string F(float v)
        {
            return v.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string Yes(bool v)
        {
            return v ? "sí" : "no";
        }
    }

    /// <summary>
    /// Maqueta de la ranura para medir y capturar (solo en los tests; sus textos nunca van al juego): la línea de fruta con el
    /// texto del peor caso, la llegada completa de 02 §6 y la compacta de la opción (a) de la pregunta 11.
    /// </summary>
    internal sealed class SlotMaqueta : IVictoryLineProvider
    {
        /// <summary>Qué dibuja.</summary>
        public enum Mode
        {
            /// <summary>Línea de fruta: icono de 32 dp, cabecera a 12 sp y el texto del peor caso a 14 sp.</summary>
            Fruit,

            /// <summary>Llegada completa (02 §6): fila del agua, bienvenida, placa del lugar y placa del dato con el retrato.</summary>
            Arrival,

            /// <summary>Llegada compacta (pregunta 11, a): fila del agua y una placa con el retrato, el lugar (12 sp) y el dato.</summary>
            Compact,

            /// <summary>
            /// Llegada ceñida a la forma de la línea de fruta (la (a) más apretada [P]): fila de 32 dp como la cabecera, con el
            /// agua de fondo, el retrato de 32 dp y el lugar a 12 sp (crece si parte en dos líneas), y el dato a todo el ancho.
            /// </summary>
            Tight,
        }

        private const float RowDp = 28f;
        private const float PortraitDp = 40f;
        private const float GapDp = 4f;

        // Placa del lugar del peor caso: nombre de 32 caracteres (RiverStopCatalog.NameMaxChars), país doble y río; ≈ 65 en es-419 y
        // ≈ 85 (+30 %) en los demás. Bienvenida de las paradas 80 y 140 (P-M7 a).
        private const string PlaceEs = "Encuentro de las Aguas de Manaos · Colombia y Perú · río Solimões";
        private const string PlaceEn = "Meeting of the Waters near the city of Manaus · Colombia and Peru · the Solimões River";
        private const string PlacePt = "Encontro das Águas perto da grande cidade de Manaus · Colômbia e Peru · rio Solimões";
        private const string PlaceXa = "[Éñcúéñtró dé lás Ágúás dé Máñáós · Cólómbíá ý Pérú · río Sólímõés ·················]";
        private const string WelcomeEs = "¡Bienvenidos a Colombia y Perú!";
        private const string WelcomeOther = "Welcome to Colombia and Peru, friends!";
        private const string HeaderEs = "Dato del río · Copoazú 20/20";
        private const string HeaderOther = "[Ðátó dél ríó · Cópóázú 20/20 ·····]";

        private readonly string _locale;
        private readonly float _scale;
        private RectTransform? _content;
        private float _width;
        private float _height;
        private TextMeshProUGUI? _bold;
        private TextMeshProUGUI? _body;
        private readonly List<GameObject> _drawn = new List<GameObject>();

        public SlotMaqueta(string locale, float scale)
        {
            _locale = locale;
            _scale = scale;
        }

        public bool SlotActive => true;

        public void Layout(RectTransform content, float widthDp, float heightDp)
        {
            _content = content;
            _width = widthDp;
            _height = heightDp;
            if (_bold == null)
            {
                _bold = UiBuilder.Label("MeasureBold", content, TextStyle.PlainBold, string.Empty, 14f, new Color(0f, 0f, 0f, 0f), TextAlignmentOptions.TopLeft);
                _bold.textWrappingMode = TextWrappingModes.Normal;
                _body = UiBuilder.Label("MeasureBody", content, TextStyle.Body, string.Empty, 14f, new Color(0f, 0f, 0f, 0f), TextAlignmentOptions.TopLeft);
                _body.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        public bool Show(LevelFlow flow, in VictoryLine line)
        {
            Draw(Mode.Fruit);
            return true;
        }

        public void Tick(float dtMs, float sinceShownMs)
        {
        }

        public void Hide()
        {
            Clear();
        }

        public void LayoutIds(List<string> ids, List<RectTransform> rects)
        {
        }

        /// <summary>
        /// Alto de la llegada maqueta de <paramref name="locale"/> en <paramref name="width"/> dp (a la escala de la maqueta):
        /// completa o compacta, con el dato a <paramref name="factSp"/> sp.
        /// </summary>
        public float MeasureArrival(string locale, float width, bool compact, float factSp)
        {
            string fact = VictoryLineSlot.WorstCaseText(locale);
            string place = Place(locale);
            float textW = width - PortraitDp - 8f;
            if (compact)
            {
                float placeH = Measure(_bold!, place, 12f, textW);
                float factH = Measure(_body!, fact, factSp, textW);
                return Mathf.Ceil(RowDp + GapDp + Mathf.Max(PortraitDp, placeH + 2f + factH) + VictoryLineSlot.BottomDp);
            }

            float welcome = Measure(_bold!, locale == LocLocale.SpanishLatinAmerica ? WelcomeEs : WelcomeOther, 14f, width);
            float placeFull = Measure(_bold!, place, 14f, width);
            float factFull = Measure(_body!, fact, factSp, textW);
            return Mathf.Ceil(RowDp + GapDp + welcome + GapDp + placeFull + 6f + Mathf.Max(PortraitDp, factFull) + VictoryLineSlot.BottomDp);
        }

        /// <summary>
        /// Alto de la llegada ceñida (<see cref="Mode.Tight"/>) de <paramref name="locale"/> en <paramref name="width"/> dp, con el
        /// dato a <paramref name="factSp"/> sp.
        /// </summary>
        public float MeasureTight(string locale, float width, float factSp)
        {
            float row = TightRow(Measure(_bold!, Place(locale), 12f, width - 40f));
            float factH = Measure(_body!, VictoryLineSlot.WorstCaseText(locale), factSp, width);
            return Mathf.Ceil(row + factH + VictoryLineSlot.BottomDp);
        }

        /// <summary>Dibuja <paramref name="mode"/> en la ranura (con una raya magenta en el pie del alto fijo H).</summary>
        public void Draw(Mode mode)
        {
            Clear();
            RectTransform content = _content!;
            float w = _width;
            string fact = VictoryLineSlot.WorstCaseText(_locale);
            bool es = _locale == LocLocale.SpanishLatinAmerica;
            if (mode == Mode.Tight)
            {
                float row = TightRow(Measure(_bold!, Place(_locale), 12f, w - 40f));
                Box(content, 0f, 0f, w, row, new Color(0.18f, 0.55f, 0.55f, 1f));
                Box(content, 0f, 0f, 32f, 32f, new Color(0.55f, 0.45f, 0.30f, 1f)); // retrato
                Text(content, TextStyle.PlainBold, Place(_locale), 12f, 40f, 3f, w - 40f, 0f, Palette.Cream, TextAlignmentOptions.TopLeft);
                Text(content, TextStyle.Body, fact, 14f, 0f, row, w, 0f, Palette.UiInk, TextAlignmentOptions.TopLeft);
            }
            else if (mode == Mode.Fruit)
            {
                Box(content, 0f, 0f, 32f, 32f, new Color(0.55f, 0.25f, 0.45f, 1f));
                Text(content, TextStyle.PlainBold, es ? HeaderEs : HeaderOther, 12f, 40f, 0f, w - 40f, 32f, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
                Text(content, TextStyle.Body, fact, 14f, 0f, VictoryLineSlot.HeaderDp, w, 0f, Palette.UiInk, TextAlignmentOptions.TopLeft);
            }
            else
            {
                Box(content, 0f, 0f, w, RowDp, new Color(0.18f, 0.55f, 0.55f, 1f));
                Box(content, w * 0.45f, 4f, 30f, 20f, new Color(0.45f, 0.30f, 0.15f, 1f)); // canoa
                Box(content, w - 26f, 4f, 20f, 20f, new Color(0.90f, 0.85f, 0.70f, 1f)); // mini cuia
                float y = RowDp + GapDp;
                float textW = w - PortraitDp - 8f;
                if (mode == Mode.Arrival)
                {
                    string welcome = es ? WelcomeEs : WelcomeOther;
                    y += Text(content, TextStyle.PlainBold, welcome, 14f, 0f, y, w, 0f, Palette.FamilyWood.Deep, TextAlignmentOptions.TopLeft) + GapDp;
                    y += Text(content, TextStyle.PlainBold, Place(_locale), 14f, 0f, y, w, 0f, Palette.FamilyWood.Deep, TextAlignmentOptions.TopLeft) + 6f;
                    Box(content, 0f, y, PortraitDp, PortraitDp, new Color(0.55f, 0.45f, 0.30f, 1f));
                    Text(content, TextStyle.Body, fact, 14f, PortraitDp + 8f, y, textW, 0f, Palette.UiInk, TextAlignmentOptions.TopLeft);
                }
                else
                {
                    Box(content, 0f, y, PortraitDp, PortraitDp, new Color(0.55f, 0.45f, 0.30f, 1f));
                    float placeH = Text(content, TextStyle.PlainBold, Place(_locale), 12f, PortraitDp + 8f, y, textW, 0f, Palette.FamilyWood.Deep, TextAlignmentOptions.TopLeft);
                    Text(content, TextStyle.Body, fact, 14f, PortraitDp + 8f, y + placeH + 2f, textW, 0f, Palette.UiInk, TextAlignmentOptions.TopLeft);
                }
            }

            Box(content, -8f, _height, w + 16f, 1.5f, new Color(1f, 0f, 1f, 0.9f)); // pie del alto fijo H
        }

        // Fila de la llegada ceñida: 32 dp como la cabecera de la fruta, o lo que pida el lugar con 3 dp arriba y abajo.
        private static float TightRow(float placeH)
        {
            return Mathf.Max(VictoryLineSlot.HeaderDp, placeH + 6f);
        }

        private static string Place(string locale)
        {
            switch (locale)
            {
                case LocLocale.SpanishLatinAmerica:
                    return PlaceEs;
                case LocLocale.PortugueseBrazil:
                    return PlacePt;
                case LocLocale.Pseudo:
                    return PlaceXa;
                default:
                    return PlaceEn;
            }
        }

        private float Measure(TextMeshProUGUI label, string text, float sp, float width)
        {
            label.fontSize = sp * _scale;
            float h = label.GetPreferredValues(text, width, float.PositiveInfinity).y;
            label.text = string.Empty;
            return h;
        }

        private void Box(RectTransform parent, float x, float y, float w, float h, Color color)
        {
            Image img = UiBuilder.Image("MaquetaBox", parent, null, color);
            UiBuilder.Place(img.rectTransform, x, y, w, h);
            _drawn.Add(img.gameObject);
        }

        // Etiqueta a sp × escala de la maqueta, de alto fixedH (0 = el que mide); devuelve su alto medido.
        private float Text(RectTransform parent, TextStyle style, string value, float sp, float x, float y, float w, float fixedH, Color color, TextAlignmentOptions align)
        {
            TextMeshProUGUI t = UiBuilder.Label("MaquetaText", parent, style, value, sp, color, align);
            t.fontSize = sp * _scale;
            t.textWrappingMode = TextWrappingModes.Normal;
            float measured = t.GetPreferredValues(value, w, float.PositiveInfinity).y;
            UiBuilder.Place(t.rectTransform, x, y, w, fixedH > 0f ? fixedH : Mathf.Max(measured, 1f));
            _drawn.Add(t.gameObject);
            return measured;
        }

        private void Clear()
        {
            for (int i = 0; i < _drawn.Count; i++)
            {
                if (_drawn[i] != null)
                {
                    Object.Destroy(_drawn[i]);
                }
            }

            _drawn.Clear();
        }
    }
}
