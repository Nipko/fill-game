#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Ads;
using AF.Boosters;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
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
    /// Tarea A1: ningún texto por debajo de 12 sp (pregunta 36 (a) de <c>docs/GDD.md</c> §6.3, D-020; 02 §12 y 07 §6.4) y la escala
    /// de texto global para pruebas (<see cref="UiBuilder.TextScale"/>, <see cref="LaunchOptions.FontScaleOverride"/>; 02 §12:
    /// hasta el 130 % sin desbordes a 360 dp, los contenedores crecen):
    /// <list type="bullet">
    /// <item><description>la escala, 1 por defecto, sin efecto con 1, llevada a 1..1,3 y aplicada al tamaño de
    /// <see cref="UiBuilder.Label"/> y al mínimo y al máximo de <see cref="UiBuilder.AutoSize"/>;</description></item>
    /// <item><description>los mínimos de las piezas: líneas y botones de los modales (<c>ModalKit.Line</c> y <c>ModalKit.Cta</c>),
    /// el aviso del tablero (<see cref="Toast"/>) y la tarjeta «¡Miquito a la vista!» (<see cref="SightingCard"/>) ya no bajan
    /// de 12 sp;</description></item>
    /// <item><description>a 360 × 780 dp y a 360 × 640 dp (1080 × 1920 px, que GameScreen maqueta en 360 × 640 dp desde DC3; ver
    /// <c>HudLabelTests</c>), al 100 % y al 130 %, ningún texto de Aguas estancadas (en sus ramas), ¿Seguro?, Nivel fallido,
    /// Sin canoas, el aviso (también el de dos líneas, el regalo de Capi, y el cartel del intento) ni la tarjeta del miquito (de
    /// cada especie) queda por debajo de 12 sp (12 × escala dp) ni se sale de su caja; los modales caben en la zona segura;
    /// </description></item>
    /// <item><description>las cajas que crecen no tapan la bandeja ni las burbujas «Te faltan {n}» (pregunta 41 (4)) ni salen
    /// de la zona segura de abajo: el aviso y la tarjeta del miquito, esta por su camino de verdad en sus niveles de 1-200 y
    /// sobre el muelle con una burbuja a la vista (si no cabe, no sale); y el aviso con 3 filas del 135 sigue el compromiso
    /// ratificado en la pregunta 47, (1): nunca tapa las cuias ni deja a medias la ficha «?×1» (al 100 %, junto a ella; al
    /// 130 %, la tapa entera);</description></item>
    /// <item><description>la etiqueta de los CTA con precio no se encoge con la escala (la etiqueta de precio se mide) y
    /// Aguas estancadas deja el tablero entero sobre el panel a 360 × 780 al 100 % (al 130 %, el tablero sube hasta su tope y
    /// lo que el panel tapa se registra: conflicto de 02 §7 con 02 §12, pendiente del dueño); en 16:9, al 100 % y al 130 %, en
    /// todas las ramas, el panel no tapa ninguna cuia del 31: va compacto si hace falta y, si ni así, el tablero se reduce
    /// mientras el modal esté abierto (tarea FX16a; <see cref="StuckCompactTests"/>).</description></item>
    /// </list>
    /// La pregunta 48 (b) (Revelar sin medios: sin aviso, responde la burbuja) se prueba en <c>PhoneFixesTests</c> y
    /// <c>LuciernagaTests</c>.
    /// </summary>
    public sealed class MinTextSizeTests
    {
        private const string NewLine = "\n";

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        // ---- La escala de texto ----

        [Test]
        public void TextScale_OneByDefault_ClampedTo1And130_ResetClearsIt()
        {
            Assert.That(LaunchOptions.FontScaleOverride, Is.Null);
            Assert.That(UiBuilder.TextScale, Is.EqualTo(1f), "sin escala forzada (ni extra af_font_scale en el Editor), 1");
            LaunchOptions.FontScaleOverride = 1.15f;
            Assert.That(UiBuilder.TextScale, Is.EqualTo(1.15f));
            LaunchOptions.FontScaleOverride = 1.3f;
            Assert.That(UiBuilder.TextScale, Is.EqualTo(UiBuilder.MaxTextScale));
            LaunchOptions.FontScaleOverride = 2f;
            Assert.That(UiBuilder.TextScale, Is.EqualTo(UiBuilder.MaxTextScale), "por encima del 130 %, el tope (02 §12)");
            LaunchOptions.FontScaleOverride = 0.85f;
            Assert.That(UiBuilder.TextScale, Is.EqualTo(1f), "por debajo de 1, 1: los textos no bajan de 12 sp");
            LaunchOptions.FontScaleOverride = float.NaN;
            Assert.That(UiBuilder.TextScale, Is.EqualTo(1f));
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.FontScaleOverride, Is.Null, "Reset la quita");
            Assert.That(UiBuilder.TextScale, Is.EqualTo(1f));
        }

        [Test]
        public void TextScale_AppliesToLabelAndAutoSize_AndHasNoEffectAtOne()
        {
            var go = new GameObject("TextScaleTest", typeof(RectTransform));
            try
            {
                TextMeshProUGUI one = UiBuilder.Label("One", go.transform, TextStyle.PlainBold, "Texto", 15f, Color.white, TextAlignmentOptions.Center);
                UiBuilder.AutoSize(one, 12f, 15f);
                Assert.That(one.fontSize, Is.EqualTo(15f), "con 1, el tamaño de siempre");
                Assert.That(one.fontSizeMin, Is.EqualTo(12f));
                Assert.That(one.fontSizeMax, Is.EqualTo(15f));

                LaunchOptions.FontScaleOverride = 1.3f;
                TextMeshProUGUI big = UiBuilder.Label("Big", go.transform, TextStyle.PlainBold, "Texto", 15f, Color.white, TextAlignmentOptions.Center);
                UiBuilder.AutoSize(big, 12f, 15f);
                Assert.That(big.fontSize, Is.EqualTo(19.5f).Within(1e-4f), "15 sp al 130 %");
                Assert.That(big.fontSizeMin, Is.EqualTo(15.6f).Within(1e-4f), "12 sp al 130 %");
                Assert.That(big.fontSizeMax, Is.EqualTo(19.5f).Within(1e-4f));
                Assert.That(one.fontSize, Is.EqualTo(15f), "los textos ya creados no cambian");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [UnityTest]
        public IEnumerator Pieces_NeverShrinkBelow12sp()
        {
            // Hoy bajaban de 12: ModalKit (Line y Cta, hasta 11), Toast (11) y SightingCard (nombre 11 y dato 10). Ahora se
            // encogen lo justo para caber, con 12 sp como mínimo («fontSizeMin = 12», tarea A1).
            yield return Load31(1000, 5);
            ProceduralArt art = GameBootstrap.Current!.View!.CardArt;
            var go = new GameObject("MinimumsTest", typeof(RectTransform));
            try
            {
                foreach (float size in new[] { 12f, 13f, 14f, 15f, 18f })
                {
                    TextMeshProUGUI line = ModalKit.Line("Line", go.transform, "Texto", size, Color.white);
                    Assert.That(line.fontSizeMin, Is.EqualTo(12f), "ModalKit.Line de " + size + " sp");
                    Assert.That(line.fontSizeMax, Is.EqualTo(size), "ModalKit.Line de " + size + " sp: su tamaño");
                    ModalKit.Cta("Cta", go.transform, art.CtaWood, TextStyle.PillWood, "Texto", size, () => { }, out TextMeshProUGUI cta);
                    Assert.That(cta.fontSizeMin, Is.EqualTo(12f), "ModalKit.Cta de " + size + " sp");
                    Assert.That(cta.fontSizeMax, Is.EqualTo(size), "ModalKit.Cta de " + size + " sp: su tamaño");
                }

                var toast = new Toast("Toast", go.transform, art, 15f);
                Assert.That(toast.Label.fontSizeMin, Is.EqualTo(12f), "el aviso");
                var card = new SightingCard(go.transform, art);
                foreach (TextMeshProUGUI t in card.Texts)
                {
                    Assert.That(t.fontSizeMin, Is.EqualTo(12f), "la tarjeta del miquito: " + t.name);
                }
            }
            finally
            {
                Object.Destroy(go);
            }
        }

        // ---- Aguas estancadas, ¿Seguro?, Nivel fallido y Sin canoas ----

        [UnityTest]
        public IEnumerator FunnelModals_AtLeast12sp_NoOverflow_InsideTheSafeArea([Values(2340, 1920)] int screenPx, [Values(100, 130)] int percent)
        {
            float scale = Use(screenPx, percent);
            string at = AtText(screenPx, percent);
            yield return Load31(0, 1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);

            // Aguas estancadas tal como lo abre el juego en el 31 sin semillas y con la última canoa: rama B (el rescate en gris
            // con «Te faltan 300 semillas», Puñado y Balsa) y «Perderás tu última canoa» (rama G).
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Disabled));
            Assert.That(view.Stuck.BalsaButton.gameObject.activeSelf, Is.True);
            AssertStuck(view, scale, "Aguas estancadas, rama B " + at);
            AssertBoardOverThePanel(view, scale, "Aguas estancadas, rama B " + at);

            // Las otras ramas del mismo modal (04 §6.2), con el modelo de verdad sobre el contexto de este bloqueo.
            StuckContext ctx = boot.Flow!.BuildStuckContext(false, true, GeoTier.T3);
            foreach ((string name, StuckContext variant) in StuckVariants(ctx))
            {
                // Como al abrirlo: el panel en su sitio y el tablero ajustado a él (GameScreen.FitBoardToStuck; FX16a).
                view.ShowStuckVariant(StuckModalModel.Build(variant, boot.Flow.Boosters));
                AssertStuck(view, scale, "Aguas estancadas, " + name + " " + at);
                if (view.CardCanvasHeight <= GameScreen.MinHeight + 0.5f)
                {
                    // En 16:9, al 100 % y al 130 %, ninguna variante tapa las cuias (02 §7; FX16a).
                    float covered = StuckCompactTests.CoveredDp(view, "Aguas estancadas, " + name + " " + at);
                    Assert.That(covered, Is.LessThanOrEqualTo(0.05f), "Aguas estancadas, " + name + " " + at + ": ninguna cuia tapada (02 §7; FX16a)");
                }
            }

            // ¿Seguro? con la última canoa y el rescate en gris; después, con el rescate a la venta (−50 %) y la cabecera de Muy
            // difícil (el sello de la etiqueta estrecha el título).
            Assert.That(boot.PressGiveUp(), Is.True);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            AssertModal(view, view.GiveUp.Root, scale, "¿Seguro? " + at);
            StuckContext rich = ctx;
            rich.Seeds = 1000;
            rich.RescuePrice = 300;
            rich.RescueBasePrice = 600;
            view.GiveUp.SetLabel(LevelDefinition.LabelVeryHard);
            view.GiveUp.Show(StuckModalModel.BuildGiveUpConfirm(rich), view.CanvasWidth, view.CardCanvasHeight);
            AssertModal(view, view.GiveUp.Root, scale, "¿Seguro?, rescate a la venta y Muy difícil " + at);
            view.GiveUp.SetLabel(LevelDefinition.LabelNormal);

            // Nivel fallido; también con la cabecera de Muy difícil y un número de nivel de 3 cifras.
            Assert.That(boot.ConfirmGiveUp(), Is.True);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            AssertModal(view, view.Failed.Root, scale, "Nivel fallido " + at);
            view.Failed.SetLabel(LevelDefinition.LabelVeryHard);
            view.Failed.Show(200, 80, view.CanvasWidth, view.CardCanvasHeight);
            AssertModal(view, view.Failed.Root, scale, "Nivel fallido del 200, Muy difícil " + at);
            view.Failed.SetLabel(LevelDefinition.LabelNormal);
            view.Failed.Show(31, 20, view.CanvasWidth, view.CardCanvasHeight);

            // Sin canoas (0/5): el cronómetro, el video, «Llenar canoas» en gris con «Te faltan {n} semillas» y Canoas infinitas.
            Assert.That(boot.PressRetry(), Is.True);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            PlayModeHelpers.Step(boot, 20);
            Assert.That(view.NoCanoes.VideoShown, Is.True);
            AssertModal(view, view.NoCanoes.Root, scale, "Sin canoas " + at);
        }

        /// <summary>
        /// En 16:9 (360 × 640 dp) al 130 %, el panel de Aguas estancadas de la rama B cabe entre las zonas seguras y no tapa
        /// ninguna cuia del 31 (tarea FX16a, decisión del dueño del 2026-10-20: compactarlo; antes
        /// <c>Stuck_130_16x9_InsideTheSafeArea_KnownIssue</c>, explícito: el panel medía 597 dp, más que los 576 entre las zonas
        /// seguras, y subía 21 dp por encima de la de arriba).
        /// </summary>
        [UnityTest]
        public IEnumerator Stuck_130_16x9_InsideTheSafeArea()
        {
            float scale = Use(1920, 130);
            yield return Load31(0, 1);
            GameBootstrap boot = GameBootstrap.Current!;
            FunnelSetup.GetStuck(boot);
            Assert.That(boot.View!.Stuck.Compact, Is.True, "la disposición compacta");
            AssertStuck(boot.View, scale, "Aguas estancadas, rama B " + AtText(1920, 130));
            AssertBoardOverThePanel(boot.View, scale, "Aguas estancadas, rama B " + AtText(1920, 130));
        }

        [UnityTest]
        public IEnumerator FunnelModals_Level6_FreeRestartBranch([Values(2340, 1920)] int screenPx, [Values(100, 130)] int percent)
        {
            // Rama C (1-30): Marea con su cupo y «Volver a la orilla (gratis)» como botón.
            float scale = Use(screenPx, percent);
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore(6, 5));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            FunnelSetup.GetStuck(boot);
            Assert.That(boot.View!.Stuck.RestartButton.gameObject.activeSelf, Is.True);
            AssertStuck(boot.View, scale, "Aguas estancadas, rama C " + AtText(screenPx, percent));
            AssertBoardOverThePanel(boot.View, scale, "Aguas estancadas, rama C " + AtText(screenPx, percent));
        }

        [UnityTest]
        public IEnumerator PricedCtas_TheLabelDoesNotShrinkAt130_ThePriceTagFitsItsDigits([Values(2340, 1920)] int screenPx)
        {
            // Revisión de A1: la etiqueta de precio se estimaba por dígito (con 18-21 dp vacíos a la derecha) y crecía con la
            // escala, así que al 130 % le quitaba sitio a «Continuar: +1 cuia», que quedaba más pequeña que al 100 %. Ahora se
            // mide: la etiqueta de los tres CTA con precio del embudo (Aguas estancadas, ¿Seguro? y Sin canoas) es al 130 % al
            // menos tan grande como al 100 %, y la placa deja el mismo aire a los dos lados.
            var sizes = new float[2, 3];
            string[] names = { "«Continuar: +1 cuia» de Aguas estancadas", "«Continuar: +1 cuia» de ¿Seguro?", "«Llenar canoas» de Sin canoas" };
            int[] percents = { 100, 130 };
            for (int p = 0; p < percents.Length; p++)
            {
                float scale = Use(screenPx, percents[p]);
                string at = AtText(screenPx, percents[p]);
                yield return Load31(0, 1);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                FunnelSetup.GetStuck(boot);
                sizes[p, 0] = PricedCtaLabelSize(view, view.Stuck.RescueButton, scale, names[0] + " " + at);
                Assert.That(boot.PressGiveUp(), Is.True);
                sizes[p, 1] = PricedCtaLabelSize(view, view.GiveUp.RescueButton, scale, names[1] + " " + at);
                Assert.That(boot.ConfirmGiveUp(), Is.True);
                Assert.That(boot.PressRetry(), Is.True);
                Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
                PlayModeHelpers.Step(boot, 20);
                sizes[p, 2] = PricedCtaLabelSize(view, view.NoCanoes.RefillButton, scale, names[2] + " " + at);
            }

            for (int i = 0; i < names.Length; i++)
            {
                TestContext.WriteLine(names[i] + " a " + (screenPx == 1920 ? "360 × 640" : "360 × 780") + ": " + sizes[0, i].ToString("0.00") + " dp al 100 %, "
                    + sizes[1, i].ToString("0.00") + " dp al 130 %.");
                Assert.That(sizes[1, i], Is.GreaterThanOrEqualTo(sizes[0, i] - 0.01f), names[i] + ": al 130 %, al menos su tamaño al 100 %");
            }
        }

        // ---- El aviso del tablero ----

        [UnityTest]
        public IEnumerator Toast_AtLeast12sp_NoOverflow_ClearOfTheTrayAndTheBubbles([Values(2340, 1920)] int screenPx, [Values(100, 130)] int percent)
        {
            float scale = Use(screenPx, percent);
            string at = AtText(screenPx, percent);

            // El tablero 6 del tutorial (1 fila) como nivel 46, sin semillas ni video: «Te faltan 150» de «+1 cuia».
            LaunchOptions.LevelOffsetOverride = 40;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(46, new MemoryProgressStore(46, 45));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            GameScreen view = boot.View!;
            Assert.That(view.Tray.ExtraShowsMissing, Is.True, "condición del test: la burbuja de «+1 cuia»");
            AssertTextFits(view.Tray.ExtraMissingLabel, scale, "la burbuja «Te faltan 150» " + at);

            // El regalo de Capi (el aviso más largo, de 2 o 3 líneas), «¡Una cuia más!» y «+1 Luciérnaga».
            foreach (string text in new[] { UiText.ExtraCuiaGift, UiText.RescueApplied, UiText.RevealGiftToast })
            {
                view.NoticeToast.Hide();
                view.ShowNotice(text);
                PlayModeHelpers.Step(boot, 200);
                Assert.That(view.ToastText, Is.EqualTo(text));
                string where = "«" + text + "» " + at;
                AssertTextFits(view.NoticeToast.Label, scale, where);
                AssertToastPlacement(view, where);
            }

            // Sin burbujas (con semillas), sobre la bandeja: la placa de siempre crece con la escala (y con el texto, si no cabe ni
            // a 12 sp); si el tablero no le deja sitio, se encoge hasta su texto, nunca por debajo.
            yield return Load31(1000, 5);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            foreach (string text in new[] { UiText.ExtraCuiaGift, UiText.RescueApplied })
            {
                view.NoticeToast.Hide();
                view.ShowNotice(text);
                PlayModeHelpers.Step(boot, 200);
                string where = "«" + text + "» sin burbujas " + at;
                AssertTextFits(view.NoticeToast.Label, scale, where);
                AssertToastPlacement(view, where);
                float preferred = view.NoticeToast.PreferredHeightDp(view.CanvasWidth);
                Assert.That(preferred, Is.GreaterThanOrEqualTo((Toast.DefaultHeightDp * scale) - 0.05f), where + ": la placa crece con la escala");
                Assert.That(view.NoticeToast.Height, Is.InRange(view.NoticeToast.MinHeightDp(view.CanvasWidth) - 0.05f, preferred + 0.05f), where);
                TestContext.WriteLine(where + ": placa de " + view.NoticeToast.Height.ToString("0.0") + " dp (la suya, " + preferred.ToString("0.0") + ").");
            }

            Assert.That(view.NoticeToast.PreferredHeightDp(view.CanvasWidth), Is.EqualTo(Toast.DefaultHeightDp * scale).Within(0.05f), "«¡Una cuia más!»: del alto de siempre por la escala");
        }

        [UnityTest]
        public IEnumerator AttemptBanner_AtLeast12sp_NoOverflow([Values(100, 130)] int percent)
        {
            // El cartel del intento («Nivel 31 · intento 2»), la otra placa de la familia del aviso: al reintentar tras Nivel fallido.
            float scale = Use(2340, percent);
            yield return Load31(1000, 5);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(boot.PressGiveUp(), Is.True);
            Assert.That(boot.ConfirmGiveUp(), Is.True);
            Assert.That(boot.PressRetry(), Is.True);
            Assert.That(view.BannerText, Is.EqualTo(UiText.LevelAttempt(31, 2)));
            TextMeshProUGUI? label = null;
            foreach (TextMeshProUGUI t in view.Canvas.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                if (t.transform.parent != null && t.transform.parent.name == "RiverLowered")
                {
                    label = t;
                }
            }

            Assert.That(label, Is.Not.Null, "condición del test: el texto del cartel");
            AssertTextFits(label!, scale, "el cartel del intento al " + percent + " %");
        }

        // ---- La tarjeta «¡Miquito a la vista!» ----

        [UnityTest]
        public IEnumerator SightingCard_EverySpecies_AtLeast12sp_NoOverflow([Values(2340, 1920)] int screenPx, [Values(100, 130)] int percent)
        {
            float scale = Use(screenPx, percent);
            string at = AtText(screenPx, percent);
            yield return Load31(1000, 5);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Fauna, Is.Not.Null);
            SightingCard card = view.Sighting;
            int twoLines = 0;
            int grown = 0;
            foreach (FaunaSpecies species in boot.Fauna!.Species)
            {
                string name = UiText.FaunaName(species.NameKey);
                string fact = UiText.FaunaFact(species.FactKey);
                float height = card.Measure(view.CanvasWidth, name, fact);
                Assert.That(height, Is.GreaterThanOrEqualTo(SightingCard.HeightDp * scale - 0.05f), species.Id);
                card.Place(view.CanvasWidth, 120f);
                card.Show(view.CardArt.MonkeyArtFor(species).Portrait, name, fact);
                PlayModeHelpers.Step(boot, 50);
                string where = "la tarjeta de " + species.Id + " " + at;
                foreach (TextMeshProUGUI t in card.Texts)
                {
                    if (!string.IsNullOrEmpty(t.text))
                    {
                        AssertTextFits(t, scale, where);
                    }
                }

                // Las filas, dentro de la tarjeta.
                Canvas.ForceUpdateCanvases();
                Rect root = VictorySetup.CanvasRect(card.Root, Root(view));
                foreach (TextMeshProUGUI t in card.Texts)
                {
                    Rect r = VictorySetup.CanvasRect(t.rectTransform, Root(view));
                    Assert.That(r.yMax, Is.LessThanOrEqualTo(root.yMax + 0.05f), where + ": «" + t.text + "» dentro de la tarjeta");
                }

                if (height > (SightingCard.HeightDp * scale) + 0.5f)
                {
                    grown++;
                }

                TextMeshProUGUI factLabel = card.Texts[2];
                factLabel.ForceMeshUpdate(true, true);
                if (percent == 100 && factLabel.textInfo.lineCount == 1)
                {
                    // Con el dato en una línea a la escala 1, la franja del panorama le basta con los 64 dp de 07 §6.4.
                    Assert.That(BandNeedDp(height), Is.EqualTo(GameScreen.SightingBandDp), where + ": el umbral de la franja, el de 07 §6.4");
                }
                if (factLabel.textInfo.lineCount > 1)
                {
                    // En dos líneas cortadas con el ancho entero de la columna y, solo si así la segunda quedaba corta (una
                    // palabra suelta), equilibradas: ni una palabra viuda ni un bloque cojo con un gran hueco a la derecha
                    // (revisiones de A1; D-003).
                    twoLines++;
                    Assert.That(factLabel.textInfo.lineCount, Is.EqualTo(2), where + ": el dato, en dos líneas como mucho");
                    float first = LineWidth(factLabel, 0);
                    float second = LineWidth(factLabel, 1);
                    Assert.That(second, Is.GreaterThanOrEqualTo((first * SightingCard.ShortLastLineRatio) - 0.5f),
                        where + ": sin palabra viuda (" + first.ToString("0") + " y " + second.ToString("0") + " dp)");
                    Assert.That(factLabel.rectTransform.rect.width, Is.EqualTo(card.FactWidth).Within(0.05f), where + ": la caja medida");
                    Assert.That(Mathf.Max(first, second), Is.GreaterThanOrEqualTo(card.FactWidth * 0.5f),
                        where + ": la línea más larga llena al menos media caja");
                    TestContext.WriteLine(where + ": «" + factLabel.text + "» en " + first.ToString("0") + " + " + second.ToString("0") + " dp (caja de "
                        + card.FactWidth.ToString("0") + " dp; tarjeta de " + card.Root.rect.width.ToString("0") + " dp).");
                }

                card.Hide();
            }

            TestContext.WriteLine(at + ": " + grown + " de " + boot.Fauna.Species.Count + " tarjetas más altas que las de siempre por la escala; "
                + twoLines + " con el dato en dos líneas.");
        }

        [UnityTest]
        public IEnumerator SightingCard_RealPath_CardLevels_ClearOfTheTrayBubblesCuiasAndSafeArea([Values(2340, 1920)] int screenPx, [Values(100, 130)] int percent)
        {
            // Revisión de A1: la tarjeta por su camino de verdad (MaybeShowSighting al habilitarse el toque, la cola de avisos y
            // ShowSightingNow) en los niveles de tarjeta de 1-200, sin semillas ni video (82 y 142, con las burbujas «Te faltan
            // {n}»). Si sale, no tapa la bandeja, sus burbujas, las cuias ni la zona segura; si no cabe, no sale. Al 100 % sale en
            // todos; lo que se pierde al 130 % queda en el registro (pendiente del dueño).
            var lost = new List<int>();
            foreach (int level in SightingCardLevels)
            {
                Use(screenPx, percent);
                LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
                yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                PlayModeHelpers.TakeClock(boot);
                string where = "nivel " + level + " " + AtText(screenPx, percent);
                Assert.That(boot.TapAnywhere(), Is.True, where + ": salta la intro");
                Assert.That(view.SightingPending || view.Sighting.Visible, Is.True, where + ": la regla elige este nivel (MaybeShowSighting)");
                for (int t = 0; t < 12000 && !view.Sighting.Visible && (view.SightingPending || view.NoticesQueued > 0 || view.ToastText != null); t += PlayModeHelpers.FrameMs)
                {
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                }

                string room = SightingRoomText(view);
                if (view.Sighting.Visible)
                {
                    PlayModeHelpers.Step(boot, 250); // entra con su fundido
                    Assert.That(view.Sighting.Visible, Is.True, where);
                    AssertSightingPlacement(view, where);
                    TestContext.WriteLine(where + ": sale " + (view.SightingOnDock ? "sobre el muelle" : "en la franja del panorama") + " (" + room + ").");
                }
                else
                {
                    Assert.That(view.SightingPending, Is.False, where + ": no espera más");
                    BoardLayoutResult l = view.Layout!;
                    float h = view.Sighting.Height;
                    Assert.That(l.PanelY - (view.TopRowDp + 48f) < BandNeedDp(h) && l.PanelBottom + 4f + h > DockLimitDp(view) + 0.05f, Is.True,
                        where + ": solo deja de salir si no cabe ni en la franja ni sobre el muelle (" + room + ")");
                    lost.Add(level);
                    TestContext.WriteLine(where + ": no cabe y no sale (" + room + ").");
                }
            }

            // En 16:9, desde DC3 (lienzo de 360 × 640 dp; D-028), los tableros de 2 filas de estos niveles dejan 4 dp de franja y
            // 32 dp de muelle para una tarjeta de 61 dp: no cabe y no sale (la regla de siempre); se registra (pendiente del dueño;
            // open issue de DC3).
            if (percent == 100 && screenPx == 2340)
            {
                Assert.That(lost, Is.Empty, AtText(screenPx, percent) + ": al 100 %, la tarjeta sale en todos sus niveles");
            }
        }

        [UnityTest]
        public IEnumerator SightingCard_OnTheDockWithAMissingBubble_DoesNotCoverIt_OrDoesNotShow([Values(100, 130)] int percent)
        {
            // Revisión de A1 (criterio 3): la tarjeta que va al muelle con una burbuja «Te faltan {n}» a la vista. El tablero 5 del
            // tutorial (2 filas, el de a1_06) como nivel 46, sin semillas ni video: «Te faltan 150» de «+1 cuia» y la de Revelar,
            // en dos líneas. La tarjeta, por su colocación de verdad (ShowSightingNow), con el dato más largo del catálogo:
            // - al 100 % (61 dp) cabe en la franja del panorama (74 dp) y no baja al muelle: no llega a las burbujas;
            // - al 130 % (100 dp, el dato en dos líneas) no cabe en la franja y va al muelle: hasta la bandeja cabría (102 dp,
            //   como en el 5 sin burbujas, a1_06_130), pero hasta la burbuja más alta no (76 dp), así que no sale. Antes de la
            //   revisión salía y tapaba las burbujas.
            // En los niveles de tarjeta de 1-200 no se da el caso de una tarjeta que salga sobre el muelle con una burbuja a la
            // vista (SightingCard_RealPath_*): o cabe en la franja o, con burbujas, no cabe en el muelle.
            Use(2340, percent);
            string at = AtText(2340, percent);
            yield return LoadDockBoard(0);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Tray.TryGetMissingTop(out _), Is.True, "condición del test: alguna burbuja «Te faltan {n}»");
            FaunaSpecies longest = LongestFact(boot);
            bool shown = view.ShowSighting(longest);
            string room = SightingRoomText(view);
            TestContext.WriteLine("Tablero 5 como nivel 46 con burbujas " + at + ": la tarjeta de " + longest.Id + " "
                + (!shown ? "no sale" : view.SightingOnDock ? "sale sobre el muelle" : "sale en la franja del panorama") + " (" + room + ").");
            if (shown)
            {
                PlayModeHelpers.Step(boot, 250);
                AssertSightingPlacement(view, "la tarjeta con burbujas " + at);
            }

            BoardLayoutResult l = view.Layout!;
            float height = view.Sighting.Height;
            if (percent == 100)
            {
                Assert.That(shown && !view.SightingOnDock, Is.True, at + ": en la franja del panorama");
            }
            else
            {
                Assert.That(l.PanelY - (view.TopRowDp + 48f), Is.LessThan(BandNeedDp(height)), at + ": condición del test, no cabe en la franja");
                Assert.That(l.PanelBottom + 4f + height, Is.LessThanOrEqualTo(TrayTopDp(view) - 8f + 0.05f), at + ": condición del test, hasta la bandeja cabría");
                Assert.That(shown, Is.False, at + ": no tapa la burbuja: no sale");
            }
        }

        [UnityTest]
        public IEnumerator SightingCard_OnTheDock_HidesWhenAMissingBubbleAppearsUnderIt([Values(100, 130)] int percent)
        {
            // Una burbuja que sale después bajo la tarjeta del muelle (tras gastar las semillas que quedaban) se taparía: la
            // tarjeta no puede subir sin tapar las cuias y se oculta en el acto. En la franja del panorama, sigue.
            Use(2340, percent);
            string at = AtText(2340, percent);
            yield return LoadDockBoard(150);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Tray.TryGetMissingTop(out _), Is.False, "condición del test: con 150 semillas, ninguna burbuja");
            FaunaSpecies species = boot.Fauna!.ForLevel(5)!;
            Assert.That(view.ShowSighting(species), Is.True, at + ": sin burbujas, la tarjeta sale");
            PlayModeHelpers.Step(boot, 250);
            bool onDock = view.SightingOnDock;
            AssertSightingPlacement(view, "la tarjeta sin burbujas " + at);
            Assert.That(boot.Wallet!.Apply(Transaction.Buy(SeedSink.ExtraCuia)).IsApplied, Is.True, "gasta las 150 semillas");
            PlayModeHelpers.Step(boot, 2 * PlayModeHelpers.FrameMs);
            Assert.That(view.Tray.TryGetMissingTop(out _), Is.True, "salen las burbujas «Te faltan {n}»");
            if (view.Sighting.Visible)
            {
                AssertSightingPlacement(view, "la tarjeta con las burbujas nuevas " + at);
            }

            TestContext.WriteLine(at + ": la tarjeta " + (onDock ? "sobre el muelle" : "en la franja del panorama") + "; con las burbujas nuevas, "
                + (view.Sighting.Visible ? "sigue" : "se oculta") + ".");
            if (percent == 130)
            {
                Assert.That(onDock, Is.True, at + ": condición del test, la tarjeta sobre el muelle");
                Assert.That(view.Sighting.Visible, Is.False, at + ": se oculta antes que tapar la burbuja");
            }
            else
            {
                Assert.That(view.Sighting.Visible, Is.True, at + ": la burbuja no la alcanza, sigue");
            }
        }

        // ---- El aviso con 3 filas (preguntas 47 y 48) ----

        [UnityTest]
        public IEnumerator Level135_ThreeRows_TheRescueNotice_BesideTheChipAt100_CoversItWholeAt130_AtLeast12sp([Values(100, 130)] int percent)
        {
            // El 135 con las 300 semillas justas: Aguas estancadas por un camino de vertidos útiles y el rescate de verdad (12
            // cuias; sin semillas, las dos burbujas). «¡Una cuia más!» va encima de las burbujas sin taparlas y nunca tapa las
            // cuias ni deja a medias la ficha «?×1» (pregunta 47, (1), ratificada: «junto a ella o la tapa entera»; el
            // compromiso de T52 tal como está):
            // - al 100 %, ajustado a su texto (≈ 253 dp) a la derecha de la ficha, con NoticeChipClearDp de aire: no la tapa;
            // - al 130 %, su texto (≈ 19,5 sp) ya no cabe junto a ella: va de su ancho de siempre, con su borde de arriba en el
            //   pie de la fila de abajo, y tapa la ficha entera los 3 s que dura (la otra rama del compromiso, como el 115 tras
            //   el rescate al 100 %); la placa se queda en el hueco y su texto baja hacia 12 sp × 1,3.
            float scale = Use(2340, percent);
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(300));
            yield return PlayModeHelpers.LoadGame(135, new MemoryProgressStore(135, 134));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            PhoneFixesTests.RescueForReal(boot, null, "135");
            GameScreen view = boot.View!;
            Assert.That(view.Views.Length, Is.EqualTo(12));
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied));
            string where = "135 tras el rescate al " + percent + " %";
            AssertTextFits(view.NoticeToast.Label, scale, where);
            Assert.That(view.Tray.ExtraShowsMissing && view.Tray.RevealShowsMissing, Is.True, "las dos burbujas, estrechas y en dos líneas");
            AssertTextFits(view.Tray.ExtraMissingLabel, scale, where + ", la burbuja de «+1 cuia»");
            AssertTextFits(view.Tray.RevealMissingLabel, scale, where + ", la burbuja de Revelar");
            AssertToastPlacement(view, where);
            float foot = view.Layout.RowFootY[view.Layout.Rows - 1];
            Assert.That(view.NoticeToast.Top, Is.GreaterThanOrEqualTo(foot - 0.05f), where + ": no tapa las cuias de la fila de abajo");
            int covered = PhoneFixesTests.AssertChipsWholeOrClear(view, where);
            if (percent == 100)
            {
                Assert.That(covered, Is.EqualTo(0), where + ": junto a la ficha «?×1», sin taparla");
                Assert.That(view.NoticeToast.Width, Is.EqualTo(view.NoticeToast.FitWidthDp(view.CanvasWidth)).Within(0.05f), where + ": ajustado a su texto");
            }
            else
            {
                Assert.That(covered, Is.GreaterThan(0), where + ": tapa entera la ficha «?×1», sin dejar una tira suelta");
                Assert.That(view.NoticeToast.Width, Is.EqualTo(Toast.FullWidthDp(view.CanvasWidth)).Within(0.05f), where + ": no cabe junto a la ficha: de su ancho de siempre");
                Assert.That(view.NoticeToast.Top, Is.EqualTo(foot).Within(0.05f), where + ": su borde de arriba, en el pie de la fila de abajo");
            }

            TestContext.WriteLine(where + ": el aviso en " + view.NoticeToast.SeenRect + "; pie de la fila de abajo a " + foot.ToString("0.0")
                + " dp; tapa " + covered + " ficha(s) enteras.");

            // Revelar sin medios no añade ningún aviso (pregunta 48 (b)): responde la burbuja.
            Assert.That(boot.PressReveal(), Is.False);
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
            Assert.That(view.Tray.RevealMissingPulsing, Is.True);
        }

        // ---- Utilidades ----

        // Fija la pantalla (1080 × screenPx px) y la escala de texto (percent %), antes de cargar la escena; devuelve la escala.
        private static float Use(int screenPx, int percent)
        {
            GameScreen.SimulatedScreen = screenPx == 1920 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            float scale = percent / 100f;
            LaunchOptions.FontScaleOverride = scale;
            Assert.That(UiBuilder.TextScale, Is.EqualTo(scale).Within(1e-5f));
            return scale;
        }

        private static string AtText(int screenPx, int percent)
        {
            return "a " + (screenPx == 1920 ? "360 × 640" : "360 × 780") + " al " + percent + " %";
        }

        // El 31 (el tablero 6 del tutorial con el desplazamiento de 25) con seeds semillas y canoes canoas, en juego.
        private static IEnumerator Load31(int seeds, int canoes)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds, canoes));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        // Niveles de la tarjeta del miquito en 1-200 (nivel_tarjeta del catálogo; todos libres: SightingRule.FirstFreeLevel).
        private static readonly int[] SightingCardLevels = { 5, 13, 42, 82, 142 };

        // El tablero 5 del tutorial (2 filas; la tarjeta, en la franja al 100 % y sobre el muelle al 130 %: a1_06) como nivel 46,
        // sin video ni Luciérnagas y con seeds semillas (con 0, las burbujas «Te faltan {n}» de «+1 cuia» y Revelar), en juego
        // con la vista quieta.
        private static IEnumerator LoadDockBoard(int seeds)
        {
            LaunchOptions.LevelOffsetOverride = 41;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds));
            yield return PlayModeHelpers.LoadGame(46, new MemoryProgressStore(46, 45));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.View!.Sighting.Visible || boot.View.SightingPending, Is.False, "condición del test: el 46 no lleva tarjeta propia");
        }

        // La especie del dato más largo del catálogo (la tarjeta más alta).
        private static FaunaSpecies LongestFact(GameBootstrap boot)
        {
            FaunaSpecies? longest = null;
            foreach (FaunaSpecies s in boot.Fauna!.Species)
            {
                if (longest == null || UiText.FaunaFact(s.FactKey).Length > UiText.FaunaFact(longest.FactKey).Length)
                {
                    longest = s;
                }
            }

            return longest!;
        }

        // Borde de arriba de la bandeja (dp desde arriba): 4 dp sobre el área táctil de sus ranuras.
        private static float TrayTopDp(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            return VictorySetup.CanvasRect((RectTransform)view.UndoButton.transform, Root(view)).yMin - 4f;
        }

        // Lo que tiene que medir la franja del panorama para la tarjeta de height dp: 64 dp (07 §6.4) o, si la tarjeta crece, su
        // alto + SightingBandAirDp (GameScreen.ShowSightingNow).
        private static float BandNeedDp(float height)
        {
            return Mathf.Max(GameScreen.SightingBandDp, height + GameScreen.SightingBandAirDp);
        }

        // El sitio que tiene la tarjeta (registro): su alto, la franja del panorama y el muelle hasta la bandeja o la burbuja.
        private static string SightingRoomText(GameScreen view)
        {
            BoardLayoutResult l = view.Layout!;
            float band = l.PanelY - (view.TopRowDp + 48f);
            float dock = DockLimitDp(view) - (l.PanelBottom + 4f);
            return "tarjeta de " + view.Sighting.Height.ToString("0.0") + " dp; franja de " + band.ToString("0.0") + " dp (le hacen falta "
                + BandNeedDp(view.Sighting.Height).ToString("0.0") + "); muelle de " + dock.ToString("0.0") + " dp hasta "
                + (view.Tray.TryGetMissingTop(out _) ? "la burbuja más alta" : "la bandeja");
        }

        // Lo más bajo que puede llegar la placa de la tarjeta sobre el muelle (dp desde arriba): 8 dp sobre la bandeja y, con
        // burbujas «Te faltan {n}», con su canto NoticeBubbleGapDp por encima de la más alta.
        private static float DockLimitDp(GameScreen view)
        {
            float limit = TrayTopDp(view) - 8f;
            if (view.Tray.TryGetMissingTop(out float bubbleTop))
            {
                limit = Mathf.Min(limit, bubbleTop - GameScreen.NoticeBubbleGapDp - SightingCard.EdgeDp);
            }

            return limit;
        }

        // La tarjeta a la vista (con su canto): bajo la cabecera, sin tapar las cuias elevadas ni la repisa de la última fila con
        // su sombra, la bandeja ni las burbujas «Te faltan {n}», dentro de la pantalla y de la zona segura.
        private static void AssertSightingPlacement(GameScreen view, string where)
        {
            Canvas.ForceUpdateCanvases();
            Rect card = VictorySetup.CanvasRect(view.Sighting.Root, Root(view));
            card.yMax += SightingCard.EdgeDp;
            Assert.That(card.yMax, Is.LessThanOrEqualTo(TrayTopDp(view) + 0.05f), where + ": no tapa la bandeja");
            if (view.Tray.TryGetMissingTop(out float bubbleTop))
            {
                Assert.That(card.yMax, Is.LessThanOrEqualTo(bubbleTop - GameScreen.NoticeBubbleGapDp + 0.05f), where + ": ni las burbujas «Te faltan {n}»");
            }

            BoardLayoutResult l = view.Layout!;
            float shift = view.BoardContainer.anchoredPosition.y;
            float cuiasTop = float.MaxValue;
            for (int i = 0; i < view.Views.Length; i++)
            {
                cuiasTop = Mathf.Min(cuiasTop, l.FootY[i] - (view.Views[i].HeightU * view.Views[i].Unit) - CuiaView.LiftDp - shift);
            }

            float boardBottom = l.RowFootY[l.Rows - 1] + GameScreen.ShelfShadowBelowFootDp - shift;
            Assert.That(card.yMax <= cuiasTop + 0.05f || card.yMin >= boardBottom - 0.05f, Is.True, where + ": la tarjeta " + card
                + " no tapa las cuias (de " + cuiasTop.ToString("0.0") + " a " + boardBottom.ToString("0.0") + " dp)");
            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(view.TopRowDp + 48f - 0.05f), where + ": bajo la cabecera");
            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp - 0.05f), where + ": bajo la zona segura de arriba");
            Assert.That(card.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom + 0.05f), where + ": sobre la zona segura de abajo");
            Assert.That(card.xMin >= -0.05f && card.xMax <= view.CanvasWidth + 0.05f, Is.True, where + ": dentro de la pantalla");
            foreach (TextMeshProUGUI t in view.Sighting.Texts)
            {
                if (!string.IsNullOrEmpty(t.text))
                {
                    AssertTextFits(t, UiBuilder.TextScale, where);
                }
            }
        }

        // Aguas estancadas y el tablero (02 §7 y 04 §6.6: «el tablero entero a la vista y atenuado; sube lo justo para que el
        // panel no lo tape»; 07 §5.5), con la geometría de verdad del tablero (su desplazamiento y, en 16:9, su escala:
        // StuckCompactTests.BoardToCanvasY). Al 100 %, en estos tableros de una fila, el tablero queda entero sobre el panel (con
        // los 12 dp de aire de ShiftToClear). A cualquier escala, o queda entero o ha subido hasta su tope (las cuias elevadas,
        // bajo la cabecera): es la regla de hoy, la que ya se da al 100 % con 2 y 3 filas. A 360 × 780 al 130 % el panel crece
        // con la escala (02 §12) y tapa la parte de abajo de las cuias: se registra cuánto (conflicto de 02 §7 con 02 §12,
        // pendiente del dueño; revisión de A1).
        private static void AssertBoardOverThePanel(GameScreen view, float scale, string where)
        {
            Canvas.ForceUpdateCanvases();
            BoardLayoutResult l = view.Layout!;
            float shift = view.BoardContainer.anchoredPosition.y;
            Rect panel = VictorySetup.CanvasRect(view.Stuck.Panel, Root(view));
            float boardBottom = StuckCompactTests.BoardToCanvasY(view, l.PanelBottom);
            float foot = StuckCompactTests.BoardToCanvasY(view, l.RowFootY[l.Rows - 1]);
            float highest = float.MaxValue;
            for (int i = 0; i < view.Views.Length; i++)
            {
                highest = Mathf.Min(highest, l.FootY[i] - (view.Views[i].HeightU * view.Views[i].Unit) - CuiaView.LiftDp);
            }

            float room = Mathf.Max(0f, highest - (view.TopRowDp + 48f));
            float top = StuckCompactTests.BoardToCanvasY(view, highest);
            float mouth = StuckCompactTests.BoardToCanvasY(view, highest + CuiaView.LiftDp);
            float covered = Mathf.Clamp(foot - panel.yMin, 0f, foot - mouth);
            bool whole = panel.yMin >= boardBottom + 12f - 0.05f;
            TestContext.WriteLine(where + ": panel de " + panel.height.ToString("0.0") + " dp desde " + panel.yMin.ToString("0.0")
                + " dp; tablero subido " + shift.ToString("0.0") + " de " + room.ToString("0.0") + " dp; cuias de " + mouth.ToString("0.0") + " a "
                + foot.ToString("0.0") + " dp: " + (whole ? "el tablero, entero sobre el panel" : "el panel tapa " + covered.ToString("0.0") + " dp de las cuias ("
                + (100f * covered / Mathf.Max(1f, foot - mouth)).ToString("0") + " %)") + ".");
            // En 16:9 (360 × 640 dp desde DC3; D-028), el panel de la rama B (413 dp al 100 % y 597 al 130 % en la disposición de
            // siempre) tapaba la parte de abajo de las cuias del 31 aunque el tablero subiera hasta su tope; desde FX16a, al 100 %
            // y al 130 %, no tapa ninguna cuia (02 §7): va compacto y, si ni así cabe bajo las cuias (la rama B al 130 %), el
            // tablero se reduce mientras el modal esté abierto. A 360 × 780, al 100 %, el tablero entero sobre el panel; al 130 %,
            // se registra lo que tapa (conflicto de 02 §7 con 02 §12, pendiente del dueño desde A1).
            if (view.CardCanvasHeight <= GameScreen.MinHeight + 0.5f)
            {
                Assert.That(covered, Is.LessThanOrEqualTo(0.05f), where + ": ninguna cuia tapada (02 §7; FX16a)");
            }
            else if (scale <= 1f)
            {
                Assert.That(whole, Is.True, where + ": el tablero entero sobre el panel (02 §7)");
            }

            Assert.That(whole || top <= view.TopRowDp + 48f + 0.05f, Is.True, where + ": o entero sobre el panel o subido hasta su tope");
        }

        // Tamaño (dp) de la etiqueta de un CTA con precio, ya ajustada por TextMeshPro: entera, sin desbordes, ≥ 12 sp por la
        // escala y sin pisar la etiqueta de precio, que deja a la derecha de su última cifra el aire que deja la semilla a la
        // izquierda (PriceTag.RightPadDp; revisión de A1: antes, 18-21 dp).
        private static float PricedCtaLabelSize(GameScreen view, Button button, float scale, string where)
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(button.gameObject.activeInHierarchy, Is.True, where + ": condición del test, el botón a la vista");
            Transform face = button.transform.Find("Visual/Face");
            Assert.That(face, Is.Not.Null, where + ": condición del test, la cara del botón");
            TextMeshProUGUI label = face.Find("Label").GetComponent<TextMeshProUGUI>();
            AssertTextFits(label, scale, where);
            RectTransform price = (RectTransform)face.Find("Price");
            Assert.That(price.gameObject.activeInHierarchy, Is.True, where + ": con su precio");
            TextMeshProUGUI value = price.Find("Value").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI strike = price.Find("Strike").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI last = strike.gameObject.activeSelf ? strike : value;
            last.ForceMeshUpdate(true, true);
            RectTransform root = Root(view);
            Rect plate = VictorySetup.CanvasRect(price, root);
            float right = CanvasX(last.transform, last.textBounds.max.x, root);
            float air = plate.xMax - right;
            Assert.That(air, Is.InRange(PriceTag.RightPadDp - 2f, PriceTag.RightPadDp + 3f), where + ": aire a la derecha de «" + last.text + "» en su placa (" + air.ToString("0.0") + " dp)");
            Assert.That(CanvasX(label.transform, label.textBounds.max.x, root), Is.LessThanOrEqualTo(plate.xMin + 0.5f), where + ": la etiqueta no pisa la placa del precio");
            TestContext.WriteLine(where + ": etiqueta a " + label.fontSize.ToString("0.00") + " dp; placa del precio de " + plate.width.ToString("0.0") + " dp, aire a la derecha de "
                + air.ToString("0.0") + " dp.");
            return label.fontSize;
        }

        // Ancho de la línea line del texto t ya generado (dp, de su primera a su última letra).
        private static float LineWidth(TextMeshProUGUI t, int line)
        {
            Extents e = t.textInfo.lineInfo[line].lineExtents;
            return e.max.x - e.min.x;
        }

        // Coordenada x en el lienzo (dp desde la izquierda) de un punto x local de t.
        private static float CanvasX(Transform t, float localX, RectTransform root)
        {
            return root.InverseTransformPoint(t.TransformPoint(new Vector3(localX, 0f, 0f))).x - root.rect.xMin;
        }

        // Ramas de Aguas estancadas desde el 31 que el bloqueo del test no da por sí solo (04 §6.2 y §6.6).
        private static IEnumerable<(string, StuckContext)> StuckVariants(StuckContext ctx)
        {
            StuckContext a = ctx;
            a.Seeds = 1000;
            a.RescuePrice = 300;
            a.RescueBasePrice = 600;
            a.Canoes = 5;
            yield return ("rama A, rescate con el −50 %", a);

            StuckContext video = ctx;
            video.UndoUnlimited = false;
            video.UndoFree = 0;
            video.UndoStock = 0;
            video.UndoAdReady = true;
            yield return ("recarga de Marea con video, sin semillas", video);

            StuckContext seeds = video;
            seeds.UndoAdReady = false;
            seeds.Seeds = 1000;
            seeds.RescuePrice = 300;
            seeds.RescueBasePrice = 300;
            yield return ("recarga de Marea con semillas, sin video", seeds);

            StuckContext f = video;
            f.UndoAdReady = false;
            f.RescueCheck = RescueCheck.Unsolvable;
            f.UndoGiftUsed = false;
            yield return ("rama F, sin salida con una cuia más", f);

            StuckContext h = ctx;
            h.InfiniteCanoes = true;
            yield return ("rama H, Canoas infinitas", h);
        }

        private static void AssertStuck(GameScreen view, float scale, string where)
        {
            Assert.That(view.StuckVisible, Is.True, where);
            AssertModal(view, view.Stuck.Panel, scale, where);
        }

        // Todos los textos a la vista del modal: ≥ 12 sp por la escala y sin salirse de su caja; el modal, dentro de la pantalla
        // y de su zona segura.
        private static void AssertModal(GameScreen view, RectTransform modal, float scale, string where)
        {
            Canvas.ForceUpdateCanvases();
            int texts = 0;
            var issues = new List<string>();
            foreach (TextMeshProUGUI t in modal.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                if (string.IsNullOrEmpty(t.text))
                {
                    continue;
                }

                texts++;
                CollectTextIssues(t, scale, where, issues);

                // Las notas y líneas de ModalKit.PlaceLine en dos líneas, sin palabra viuda (revisión de A1; D-003).
                bool placedLine = t.name.EndsWith("Note", System.StringComparison.Ordinal) || t.name == "CanoeLine" || t.name == "Subtitle" || t.name == "Unsolvable";
                if (placedLine && t.textInfo.lineCount == 2 && t.textInfo.lineInfo[1].wordCount < 2)
                {
                    issues.Add(where + ": «" + t.text + "» (" + t.name + ") deja una palabra viuda en su segunda línea");
                }
            }

            Assert.That(texts, Is.GreaterThan(2), where + ": condición del test, sus textos");
            Assert.That(issues, Is.Empty, string.Join(NewLine, issues));
            RectTransform panel = modal == view.Stuck.Panel ? modal : (RectTransform)modal.Find("Panel");
            Rect r = VictorySetup.CanvasRect(panel, Root(view));
            Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.05f), where + ": dentro de la pantalla");
            Assert.That(r.xMax, Is.LessThanOrEqualTo(view.CanvasWidth + 0.05f), where);
            Assert.That(r.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom + 0.05f), where + ": sobre la zona segura de abajo");
            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp - 0.05f), where + ": bajo la zona segura de arriba");
        }

        // El texto t: ≥ 12 sp por la escala, sin desbordes (TextMeshPro), entero y con su geometría dentro de su rectángulo.
        // Junta todo lo que no se cumple (para verlo de una vez) y falla al final con la lista.
        internal static void AssertTextFits(TextMeshProUGUI t, float scale, string where)
        {
            var issues = new List<string>();
            CollectTextIssues(t, scale, where, issues);
            Assert.That(issues, Is.Empty, string.Join(NewLine, issues));
        }

        internal static void CollectTextIssues(TextMeshProUGUI t, float scale, string where, List<string> issues)
        {
            t.ForceMeshUpdate(true, true);
            string what = where + ": «" + t.text.Replace(NewLine, " / ") + "» (" + t.name + ", " + t.fontSize.ToString("0.0") + " dp en "
                + t.rectTransform.rect.width.ToString("0.0") + " × " + t.rectTransform.rect.height.ToString("0.0") + ")";
            if (t.fontSize < (UiBuilder.MinTextSp * scale) - 0.01f)
            {
                issues.Add(what + " < 12 sp");
            }

            if (t.overflowMode != TextOverflowModes.Overflow)
            {
                issues.Add(what + " se trunca");
            }

            if (t.isTextOverflowing)
            {
                issues.Add(what + " desborda (TextMeshPro)");
            }

            if (t.textInfo.characterCount != t.GetParsedText().Length)
            {
                issues.Add(what + " no está entero");
            }

            Rect r = t.rectTransform.rect;
            Bounds b = t.textBounds;
            const float slack = 0.5f;
            if (b.min.x < r.xMin - slack || b.max.x > r.xMax + slack)
            {
                issues.Add(what + " se sale a lo ancho (" + b.min.x.ToString("0.0") + ".." + b.max.x.ToString("0.0") + ")");
            }

            if (b.min.y < r.yMin - slack || b.max.y > r.yMax + slack)
            {
                issues.Add(what + " se sale a lo alto (" + b.min.y.ToString("0.0") + ".." + b.max.y.ToString("0.0") + " en "
                    + r.yMin.ToString("0.0") + ".." + r.yMax.ToString("0.0") + ")");
            }
        }

        // El aviso: sobre la bandeja, sin tapar las burbujas «Te faltan {n}», dentro de la pantalla y de la zona segura.
        private static void AssertToastPlacement(GameScreen view, string where)
        {
            Canvas.ForceUpdateCanvases();
            Toast toast = view.NoticeToast;
            Rect seen = toast.SeenRect;
            Rect undo = VictorySetup.CanvasRect((RectTransform)view.UndoButton.transform, Root(view));
            float trayTop = undo.yMin - 4f;
            Assert.That(seen.yMax, Is.LessThanOrEqualTo(trayTop + 0.05f), where + ": no tapa la bandeja");
            Assert.That(seen.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom + 0.05f), where + ": sobre la zona segura de abajo");
            Assert.That(seen.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp - 0.05f), where + ": bajo la zona segura de arriba");
            Assert.That(seen.xMin, Is.GreaterThanOrEqualTo(-0.05f), where);
            Assert.That(seen.xMax, Is.LessThanOrEqualTo(view.CanvasWidth + 0.05f), where);
            if (view.Tray.TryGetMissingTop(out float bubbleTop))
            {
                Assert.That(seen.yMax, Is.LessThanOrEqualTo(bubbleTop - GameScreen.NoticeBubbleGapDp + 0.05f), where + ": encima de las burbujas «Te faltan {n}», sin taparlas");
            }

            Rect plate = VictorySetup.CanvasRect(toast.Root, Root(view));
            Rect label = VictorySetup.CanvasRect(toast.Label.rectTransform, Root(view));
            Assert.That(label.yMin >= plate.yMin - 0.05f && label.yMax <= plate.yMax + 0.05f, Is.True, where + ": el texto, dentro de su placa");
        }

        private static RectTransform Root(GameScreen view)
        {
            return (RectTransform)view.Canvas.transform;
        }
    }
}
