#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Ads;
using AF.Boosters;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

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
    /// <item><description>a 360 × 780 dp y a 360 × 640 dp (1080 × 1920 px, que GameScreen maqueta en 438,75 × 780 dp; ver
    /// <c>HudLabelTests</c>), al 100 % y al 130 %, ningún texto de Aguas estancadas (en sus ramas), ¿Seguro?, Nivel fallido,
    /// Sin canoas, el aviso (también el de dos líneas, el regalo de Capi, y el cartel del intento) ni la tarjeta del miquito (de
    /// cada especie) queda por debajo de 12 sp (12 × escala dp) ni se sale de su caja; los modales caben en la zona segura;
    /// </description></item>
    /// <item><description>las cajas que crecen no tapan la bandeja ni las burbujas «Te faltan {n}» (pregunta 41 (4)) ni salen
    /// de la zona segura de abajo; y el aviso con 3 filas del 135 sigue el compromiso ratificado en la pregunta 47 (junto a la
    /// ficha «?×1», sin taparla ni tapar las cuias), también al 130 %.</description></item>
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

            // Las otras ramas del mismo modal (04 §6.2), con el modelo de verdad sobre el contexto de este bloqueo.
            StuckContext ctx = boot.Flow!.BuildStuckContext(false, true, GeoTier.T3);
            foreach ((string name, StuckContext variant) in StuckVariants(ctx))
            {
                view.Stuck.Show(StuckModalModel.Build(variant, boot.Flow.Boosters), boot.Services, view.CanvasWidth);
                view.Stuck.Place(view.CanvasWidth, view.CardCanvasHeight - view.CardSafeBottom - 8f - view.Stuck.Height);
                AssertStuck(view, scale, "Aguas estancadas, " + name + " " + at);
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
                string name = species.NameIn("es");
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
                if (factLabel.textInfo.lineCount > 1)
                {
                    twoLines++;
                }

                card.Hide();
            }

            TestContext.WriteLine(at + ": " + grown + " de " + boot.Fauna.Species.Count + " tarjetas más altas que las de siempre por la escala; "
                + twoLines + " con el dato en dos líneas.");
        }

        // ---- El aviso con 3 filas (preguntas 47 y 48) ----

        [UnityTest]
        public IEnumerator Level135_ThreeRows_TheRescueNotice_BesideTheChip_AtLeast12sp([Values(100, 130)] int percent)
        {
            // El 135 con las 300 semillas justas: Aguas estancadas por un camino de vertidos útiles y el rescate de verdad (12
            // cuias; sin semillas, las dos burbujas). «¡Una cuia más!» va encima de las burbujas sin taparlas y junto a la ficha
            // «?×1», sin taparla ni tapar las cuias (pregunta 47, ratificada: el compromiso de T52 tal como está).
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
            TestContext.WriteLine(where + ": el aviso en " + view.NoticeToast.SeenRect + "; pie de la fila de abajo a " + foot.ToString("0.0") + " dp.");

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
            }

            Assert.That(texts, Is.GreaterThan(2), where + ": condición del test, sus textos");
            Assert.That(issues, Is.Empty, string.Join(NewLine, issues));
            RectTransform panel = modal == view.Stuck.Panel ? modal : (RectTransform)modal.Find("Panel");
            Rect r = VictorySetup.CanvasRect(panel, Root(view));
            Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.05f), where + ": dentro de la pantalla");
            Assert.That(r.xMax, Is.LessThanOrEqualTo(view.CanvasWidth + 0.05f), where);
            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp - 0.05f), where + ": bajo la zona segura de arriba");
            Assert.That(r.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom + 0.05f), where + ": sobre la zona segura de abajo");
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
