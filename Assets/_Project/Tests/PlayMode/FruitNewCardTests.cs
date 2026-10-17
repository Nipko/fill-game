#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Tarjeta «Fruta nueva» en la escena real (tarea F5; 06 §4.5; 07 §6.4; guion de la intro §14; 02 §13): en el 2 no sale
    /// durante la intro, entra en la cola al habilitarse el toque y sale cuando la tropa se ha ido, con el nombre «Taperebá», la
    /// frase de su serie (<c>fact.tapereba.tag</c>), el icono y la muestra del tinte, durante su tiempo de lectura (D-011), sin
    /// capturar toques, apartando las etiquetas del HUD y con su sonda <c>AF_STATE fruit_new</c>; sale en el estreno de cada
    /// tinte de 1-300 (2, 4, 7, 11, 21, 41, 61, 81, 111, 141 y 201) con su tinte y como mucho una tarjeta por nivel (ni el
    /// miquito ni el rótulo); no sale en el 1 (tutorial), en el 3, al reintentar ni apagada; ningún texto de las dos tarjetas de
    /// la familia («Fruta nueva» y «¡Miquito a la vista!») baja de 12 sp, al 100 y al 130 %; la frase nunca deja una palabra
    /// sola en su última línea; el icono, sobre un disco con el que llega a 3:1 si alguno de los de la tarjeta llega; en los
    /// tableros de 3 filas (111 y 201) a 780 y 640 dp, al 100 y al 130 %, la placa sale a ≥ 12 sp o, si no cabe, su sonda lo dice
    /// (<c>mode</c> none); no tapa cuias, bandeja ni burbujas; y sin asignaciones por fotograma con la tarjeta a la vista.
    /// </summary>
    public sealed class FruitNewCardTests
    {
        private static readonly int[] FruitNewLevels = { 2, 4, 7, 11, 21, 41, 61, 81, 111, 141, 201 };

        // Frutas de icono claro (guanábana, copoazú y huito): su disco tiene que llegar a 3:1 (D-003, principio 3).
        private static readonly string[] LightFruits = { "guanabana", "cupuacu", "jenipapo" };

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.FruitNewOverride = true;
            GameScreen.StopLabelsOverride = true; // con el rótulo encendido: tampoco compite con la Fruta nueva
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.FruitNewOverride = null;
            GameScreen.StopLabelsOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level2_AfterTheIntro_ShowsTapereba_ForItsReadingTime()
        {
            yield return Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            FruitNewCard card = view.FruitNewView;
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            int t = 0;
            while (flow.Phase == FlowPhase.Intro && t < 6000)
            {
                Assert.That(card.Visible, Is.False, "nunca durante la intro (t = " + t + " ms)");
                Assert.That(view.NoticesQueued, Is.EqualTo(0), "entra en la cola al habilitarse el toque, no antes");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            StepUntilShown(boot);
            Assert.That(card.Name, Is.EqualTo("Taperebá"), "tint.tapereba.name");
            Assert.That(card.Tag, Is.EqualTo("Taperebá: la ciruela de los tapires"), "fact.tapereba.tag (06 §4.5)");
            Assert.That(card.Tint, Is.EqualTo(FactLine.TintOf("tapereba")));
            Assert.That(card.Icon.enabled && card.Icon.sprite != null, Is.True, "el icono de la fruta");
            Assert.That(card.Swatch.enabled && card.Swatch.sprite != null, Is.True, "la muestra del tinte con su patrón");
            Assert.That(card.DurationMs, Is.EqualTo((float)NoticeTiming.ReadMs(card.Name, card.Tag, null)), "D-011");
            Assert.That(card.DurationMs, Is.InRange(3000f, 7000f));

            // No captura toques y aparta las etiquetas del HUD mientras se ve.
            var graphics = new List<Graphic>();
            card.CollectGraphics(graphics);
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, g.name);
            }

            Assert.That(card.Root.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            PlayModeHelpers.Step(boot, 400);
            Assert.That(card.Alpha, Is.EqualTo(1f), "tras la entrada de 200 ms");
            Assert.That(view.HudLabelsAlpha, Is.EqualTo(0f), "las etiquetas del HUD se apartan");
            AssertPlacement(view, "el 2");
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected), "se juega con la tarjeta a la vista");
            boot.TapCuia(0);

            // La sonda.
            string? probe = view.LastFruitNewProbe;
            Assert.That(probe, Is.Not.Null);
            StringAssert.Contains("\"level\":2", probe);
            StringAssert.Contains("\"tint\":\"tapereba\"", probe);
            StringAssert.Contains("\"ms\":" + (int)card.DurationMs, probe);
            StringAssert.Contains("\"mode\":\"" + view.FruitNewMode + "\"", probe);

            // Se va al acabar su tiempo y no vuelve.
            for (int k = 0; k < 8000 && card.Visible; k += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(card.Visible, Is.False);
            PlayModeHelpers.Step(boot, 600);
            Assert.That(view.HudLabelsAlpha, Is.EqualTo(1f), "las etiquetas vuelven");
            PlayModeHelpers.Step(boot, 4000);
            Assert.That(card.Visible, Is.False, "una sola vez");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator EveryTintDebut_ShowsItsCard_AndNoOtherCard()
        {
            foreach (int level in FruitNewLevels)
            {
                yield return Load(level);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                ReachPlaying(boot, level);
                StepUntilShown(boot, level);
                string tint = SightingRule.FruitDebutTints[SightingRule.FruitNewAt(level, true)];
                FruitNewCard card = view.FruitNewView;
                Assert.That(card.Tint, Is.EqualTo(FactLine.TintOf(tint)), level + ": " + tint);
                Assert.That(card.Tag, Is.Not.Empty, "nivel " + level);
                PlayModeHelpers.Step(boot, 250);
                AssertPlacement(view, "el " + level);
                TestContext.WriteLine("Nivel " + level + ": «" + card.Name + "» / «" + card.Tag + "» "
                    + view.FruitNewMode + ", " + card.Height.ToString("0.0") + " dp, "
                    + card.DurationMs.ToString("0") + " ms.");

                // Como mucho una tarjeta en el nivel: ni el miquito ni el rótulo del lugar, ni ahora ni después.
                int shownFrames = 0;
                for (int t = 0; t < 12000; t += PlayModeHelpers.FrameMs)
                {
                    Assert.That(view.Sighting.Visible || view.SightingPending, Is.False, level + ": sin tarjeta del miquito");
                    Assert.That(view.StopLabelView.Visible, Is.False, level + ": sin rótulo del lugar");
                    shownFrames += card.Visible ? 1 : 0;
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                }

                Assert.That(card.Visible, Is.False, level + ": ya se fue");
                Assert.That(view.NoticesQueued, Is.EqualTo(0), "nivel " + level);
                Assert.That(shownFrames * (float)PlayModeHelpers.FrameMs, Is.LessThanOrEqualTo(card.DurationMs), level + ": una sola vez");
            }
        }

        [UnityTest]
        public IEnumerator ThreeRows_GoesAsAPlate_AndHidesWhenARowOneCuiaUnderItLifts()
        {
            // El 111 (11 cuias en 3 filas): a 360 × 780 dp no cabe ni en la franja (−8 dp) ni sobre el muelle (19,5 dp), así que
            // va como placa de una línea clavada al marco [P]; al elevarse una cuia de la fila 1 que queda bajo ella, se va y no
            // vuelve (como el rótulo del lugar, FX4).
            yield return Load(111);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ReachPlaying(boot, 111);
            StepUntilShown(boot, 111);
            FruitNewCard card = view.FruitNewView;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(card.OnPlate, Is.True, "3 filas: como placa");
            Assert.That(view.FruitNewMode, Is.EqualTo("plate"));
            StringAssert.Contains("\"mode\":\"plate\"", view.LastFruitNewProbe);
            Assert.That(card.Tag, Is.EqualTo("Lulo: el naranja peludo de pulpa verde"));
            Assert.That(card.DurationMs, Is.EqualTo((float)NoticeTiming.ReadMs(null, card.Tag, null)), "en la placa se lee la frase");
            PlayModeHelpers.Step(boot, 250);
            AssertPlacement(view, "el 111");

            // Una cuia de la fila 1 bajo la placa.
            BoardLayoutResult l = view.Layout;
            Rect plate = card.Rect;
            int under = -1;
            for (int i = 0; i < view.Views.Length && under < 0; i++)
            {
                if (l.RowOf[i] == 0 && l.CenterX[i] > plate.xMin && l.CenterX[i] < plate.xMax)
                {
                    under = i;
                }
            }

            Assert.That(under, Is.GreaterThanOrEqualTo(0), "condición del test: una cuia de la fila 1 bajo la placa");
            Assert.That(boot.TapCuia(under).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 300);
            Assert.That(view.Views[under].IsLifted, Is.True);
            Assert.That(card.Visible, Is.False, "la cuia elevada llega a la placa: se va");
            boot.TapCuia(under);
            PlayModeHelpers.Step(boot, 6000);
            Assert.That(card.Visible, Is.False, "y no vuelve");
        }

        [UnityTest]
        public IEnumerator NotOnLevel1_NorLevel3_NorWithTheOverrideOff()
        {
            yield return Load(1);
            StepWithoutIt(GameBootstrap.Current!, "en el 1 (el tutorial es el primer contacto; 07 §6.4)");
            yield return Load(3);
            StepWithoutIt(GameBootstrap.Current!, "en el 3 (no estrena tinte)");
            GameScreen.FruitNewOverride = false;
            yield return Load(2);
            StepWithoutIt(GameBootstrap.Current!, "apagada");
        }

        [UnityTest]
        public IEnumerator Restart_SeenOrStillWaiting_DoesNotBringItBack()
        {
            // Volver a la orilla con la tarjeta a la vista: la oculta y el intento nuevo no la repite.
            yield return Load(4);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ReachPlaying(boot, 4);
            StepUntilShown(boot, 4);
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(view.FruitNewView.Visible, Is.False, "otro intento: los avisos del anterior ya no valen");
            StepWithoutIt(boot, "tras Reiniciar con la tarjeta a la vista");

            // Volver a la orilla antes de que salga (aún en la cola, esperando a la tropa): ya entró en esta visita y no sale.
            yield return Load(4);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "la tarjeta espera en la cola a que la tropa se vaya");
            Assert.That(view.FruitNewView.Visible, Is.False);
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(view.NoticesQueued, Is.EqualTo(0), "Reiniciar vacía la cola");
            StepWithoutIt(boot, "tras Reiniciar con la tarjeta en la cola");
        }

        [UnityTest]
        public IEnumerator EveryTint_AtLeast12sp_NoOverflow_ClearOfTheBoard([Values(2340, 1920)] int screenPx, [Values(100, 130)] int percent)
        {
            float scale = Use(screenPx, percent);
            string at = (screenPx == 1920 ? "360 × 640" : "360 × 780") + " al " + percent + " %";
            yield return Load(2, false);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ReachPlaying(boot, 2);
            PlayModeHelpers.Step(boot, 2500);
            view.FruitNewView.Hide();
            var lost = new List<string>();
            for (int fruit = 0; fruit < SightingRule.FruitDebutTints.Length; fruit++)
            {
                string where = SightingRule.FruitDebutTints[fruit] + " " + at;
                FruitNewCard card = view.FruitNewView;
                if (!view.ShowFruitNew(fruit))
                {
                    lost.Add(SightingRule.FruitDebutTints[fruit]);
                    TestContext.WriteLine(where + ": no cabe y no sale (" + card.Height.ToString("0.0") + " dp).");
                    continue;
                }

                PlayModeHelpers.Step(boot, 250);
                Assert.That(card.Height, Is.GreaterThanOrEqualTo((FruitNewCard.HeightDp * scale) - 0.05f), where);
                foreach (TextMeshProUGUI t in card.Texts)
                {
                    MinTextSizeTests.AssertTextFits(t, scale, where);
                }

                AssertPlacement(view, where);
                int lines = AssertNoWidow(card, where);
                AssertIconContrast(card, where);
                TestContext.WriteLine(where + ": " + card.Height.ToString("0.0") + " × " + card.Root.rect.width.ToString("0.0") + " dp, "
                    + view.FruitNewMode + ", frase a " + card.Texts[1].fontSize.ToString("0.0") + " dp en " + lines + " línea(s)"
                    + (card.ShownTag != card.Tag ? " (con espacio duro: «" + card.ShownTag.Replace('\u00A0', '~') + "»)" : string.Empty)
                    + ", icono Y = " + card.IconLuminance.ToString("0.000") + ", disco " + DiscName(card.DiscColor) + " a "
                    + card.IconDiscContrast.ToString("0.00") + ":1.");
                card.Hide();
            }

            if (percent == 100)
            {
                Assert.That(lost, Is.Empty, at + ": al 100 %, sale con todos los tintes");
            }

            // La otra tarjeta de la familia, «¡Miquito a la vista!», tampoco baja de 12 sp (A1; MinTextSizeTests la mira entera).
            SightingCard sighting = view.Sighting;
            foreach (FaunaSpecies species in boot.Fauna!.Species)
            {
                string name = UiText.FaunaName(species.NameKey);
                string fact = UiText.FaunaFact(species.FactKey);
                sighting.Measure(view.CanvasWidth, name, fact);
                sighting.Place(view.CanvasWidth, 120f);
                sighting.Show(view.CardArt.MonkeyArtFor(species).Portrait, name, fact);
                PlayModeHelpers.Step(boot, 50);
                foreach (TextMeshProUGUI t in sighting.Texts)
                {
                    if (!string.IsNullOrEmpty(t.text))
                    {
                        MinTextSizeTests.AssertTextFits(t, scale, "la tarjeta de " + species.Id + " " + at);
                    }
                }

                sighting.Hide();
            }
        }

        [UnityTest]
        public IEnumerator ThreeRowLevels_LargeText_AtLeast12sp_OrLostWithItsProbe([Values(111, 201)] int level,
            [Values(2340, 1920)] int screenPx, [Values(100, 130)] int percent)
        {
            // El 111 y el 201, de 3 filas a 360 × 780 dp, donde la tarjeta solo cabe como placa bajo el HUD (a 360 × 640 dp el
            // tablero va en 2 filas y cabe en la franja, sobre el muelle o como placa): con la escala de texto el mínimo sube a
            // 12 sp × escala y el hueco no cambia. Si sale, a ≥ 12 sp y en su sitio; si no cabe, no sale y la sonda
            // AF_STATE fruit_new lo cuenta con mode "none" (para verlo en el teléfono).
            float scale = Use(screenPx, percent);
            string at = "el " + level + " a " + (screenPx == 1920 ? "360 × 640" : "360 × 780") + " al " + percent + " %";
            yield return Load(level, false);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ReachPlaying(boot, level);
            FruitNewCard card = view.FruitNewView;
            for (int t = 0; t < 8000 && !card.Visible && view.LastFruitNewProbe == null; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            int rows = view.Layout!.Rows;
            if (screenPx == 2340)
            {
                Assert.That(rows, Is.EqualTo(3), at + ": 3 filas");
            }

            string? probe = view.LastFruitNewProbe;
            Assert.That(probe, Is.Not.Null, at + ": la sonda sale tanto si se ve como si se pierde");
            StringAssert.Contains("\"level\":" + level, probe);
            if (card.Visible)
            {
                PlayModeHelpers.Step(boot, 250);
                if (rows >= 3)
                {
                    Assert.That(card.OnPlate, Is.True, at + ": con 3 filas, como placa");
                }

                StringAssert.Contains("\"mode\":\"" + view.FruitNewMode + "\"", probe);
                AssertPlacement(view, at);
                foreach (TextMeshProUGUI t in card.Texts)
                {
                    if (t.gameObject.activeInHierarchy)
                    {
                        MinTextSizeTests.AssertTextFits(t, scale, at);
                        Assert.That(t.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * scale) - 0.01f), at + ": ≥ 12 sp");
                    }
                }

                TestContext.WriteLine(at + ": " + rows + " filas, sale (" + view.FruitNewMode + "), frase a "
                    + card.Texts[1].fontSize.ToString("0.0") + " dp (" + (card.Texts[1].fontSize / scale).ToString("0.0") + " sp), "
                    + card.Rect.width.ToString("0.0") + " × " + card.Height.ToString("0.0") + " dp.");
            }
            else
            {
                StringAssert.Contains("\"mode\":\"none\"", probe);
                StringAssert.Contains("\"ms\":0", probe);
                Assert.That(view.FruitNewMode, Is.EqualTo("none"));
                Assert.That(view.NoticesQueued, Is.EqualTo(0), at + ": no queda en la cola");
                TestContext.WriteLine(at + ": " + rows + " filas, no cabe ni a 12 sp y no sale; sonda " + probe + ".");
            }

            if (percent == 100 && screenPx == 2340)
            {
                Assert.That(card.Visible, Is.True, at + ": al 100 % y a 780 dp sale");
            }
        }

        [UnityTest]
        public IEnumerator NoAllocationsPerFrame_WithTheCardShown()
        {
            yield return Load(7);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ReachPlaying(boot, 7);
            StepUntilShown(boot, 7);
            // Ya en régimen: los ~2,2 s que siguen al salto de la intro asignan memoria también sin ningún aviso (NoticeTests).
            PlayModeHelpers.TickExact(boot, 2300);
            Assert.That(view.FruitNewView.Visible, Is.True, "condición del test: aún a la vista");
            Assert.That(() =>
            {
                for (int f = 0; f < 30; f++)
                {
                    view.Tick(PlayModeHelpers.FrameMs);
                }
            }, Is.Not.AllocatingGCMemory(), "30 fotogramas con la tarjeta «Fruta nueva» a la vista");
        }

        // ---- Utilidades ----

        // La frase de la tarjeta, sin una palabra sola en su última línea (en la placa va en una). Devuelve sus líneas.
        private static int AssertNoWidow(FruitNewCard card, string where)
        {
            TextMeshProUGUI tag = card.Texts[1];
            tag.ForceMeshUpdate(true, true);
            TMP_TextInfo info = tag.textInfo;
            if (info.lineCount < 2)
            {
                return info.lineCount;
            }

            TMP_LineInfo last = info.lineInfo[info.lineCount - 1];
            int words = 0;
            bool inWord = false;
            for (int i = last.firstCharacterIndex; i <= last.lastCharacterIndex; i++)
            {
                char ch = info.characterInfo[i].character;
                bool gap = ch == ' ' || ch == '\u00A0';
                if (!gap && !inWord)
                {
                    words++;
                }

                inWord = !gap;
            }

            Assert.That(words, Is.GreaterThanOrEqualTo(2), where + ": la última línea de «" + card.ShownTag + "» no es una palabra sola");
            Assert.That(card.ShownTag.Replace('\u00A0', ' '), Is.EqualTo(card.Tag), where + ": solo cambia un espacio por uno duro");
            return info.lineCount;
        }

        // El icono sobre su disco: si alguno de los discos de la tarjeta llega a 3:1 con él, el elegido llega; si no, es el de más
        // contraste. Las frutas claras (guanábana, copoazú y huito), siempre a ≥ 3:1. En la placa no hay disco.
        private static void AssertIconContrast(FruitNewCard card, string where)
        {
            if (card.OnPlate)
            {
                return;
            }

            float best = 0f;
            foreach (Color disc in FruitNewCard.DiscColors)
            {
                best = Mathf.Max(best, FruitNewCard.Contrast(card.IconLuminance, ColorMath.RelativeLuminance(disc)));
            }

            Assert.That(card.IconLuminance, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), where + ": luminancia media del icono");
            Assert.That(card.IconDiscContrast, Is.EqualTo(FruitNewCard.Contrast(card.IconLuminance, ColorMath.RelativeLuminance(card.DiscColor))).Within(1e-4f));
            if (best >= FruitNewCard.IconDiscContrastMin)
            {
                Assert.That(card.IconDiscContrast, Is.GreaterThanOrEqualTo(FruitNewCard.IconDiscContrastMin), where + ": icono/disco ≥ 3:1");
            }
            else
            {
                Assert.That(card.IconDiscContrast, Is.EqualTo(best).Within(1e-4f), where + ": el disco de más contraste");
            }

            if (System.Array.IndexOf(LightFruits, ProceduralArt.TintIds[card.Tint]) >= 0)
            {
                Assert.That(card.IconDiscContrast, Is.GreaterThanOrEqualTo(FruitNewCard.IconDiscContrastMin), where + ": fruta clara, ≥ 3:1");
            }
        }

        private static string DiscName(Color disc)
        {
            int i = System.Array.IndexOf(FruitNewCard.DiscColors, disc);
            return i == 0 ? "crema" : i == 1 ? "madera_base" : i == 2 ? "madera_oscura" : disc.ToString();
        }

        // El nivel con 1.240 semillas (sin burbujas «Te faltan {n}»), a 360 × 780 dp salvo que el test fije otra pantalla.
        private static IEnumerator Load(int level, bool screen = true)
        {
            if (screen)
            {
                GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            }

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            PlayModeHelpers.TakeClock(GameBootstrap.Current!);
        }

        // Fija la pantalla (1080 × screenPx px) y la escala de texto (percent %), antes de cargar la escena; devuelve la escala.
        private static float Use(int screenPx, int percent)
        {
            GameScreen.SimulatedScreen = screenPx == 1920 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            float scale = percent / 100f;
            LaunchOptions.FontScaleOverride = scale;
            Assert.That(UiBuilder.TextScale, Is.EqualTo(scale).Within(1e-5f));
            return scale;
        }

        // Hasta el juego: cierra el banner de mundo si lo hay (11, 41, 81, 141 y 201) y salta la intro.
        private static void ReachPlaying(GameBootstrap boot, int level)
        {
            for (int t = 0; t < 15000 && boot.Flow!.Phase != FlowPhase.Playing; t += 100)
            {
                boot.TapAnywhere();
                PlayModeHelpers.Step(boot, 100);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), level + ": en juego");
        }

        // Hasta que la tarjeta esté a la vista: tras la intro y cuando la tropa se ha ido de la rama.
        private static void StepUntilShown(GameBootstrap boot, int level = 2)
        {
            for (int t = 0; t < 8000 && !boot.View!.FruitNewView.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "nivel " + level);
            Assert.That(boot.View!.FruitNewView.Visible, Is.True, level + ": la tarjeta sale tras la intro");
            Assert.That(boot.View.IntroTroopVisible, Is.False, level + ": cuando la tropa se ha ido");
        }

        // Pasa la intro y 8 s de juego sin que la tarjeta salga ni quede en la cola.
        private static void StepWithoutIt(GameBootstrap boot, string when)
        {
            FruitNewCard card = boot.View!.FruitNewView;
            boot.TapAnywhere();
            for (int t = 0; t < 10000 && (boot.Flow!.Phase != FlowPhase.Playing || t < 8000); t += PlayModeHelpers.FrameMs)
            {
                Assert.That(card.Visible, Is.False, "no sale " + when + " (t = " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), when);
            Assert.That(card.Visible, Is.False, when);
            Assert.That(boot.View.NoticesQueued, Is.EqualTo(0), when);
        }

        // La tarjeta a la vista (con su canto): bajo la cabecera, sin tapar las cuias elevadas ni la repisa de la última fila con
        // su sombra, la bandeja ni las burbujas «Te faltan {n}», dentro de la pantalla y de la zona segura (como la del miquito,
        // MinTextSizeTests).
        private static void AssertPlacement(GameScreen view, string where)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            FruitNewCard fruit = view.FruitNewView;
            Rect card = VictorySetup.CanvasRect(fruit.Root, root);
            card.yMax += fruit.OnPlate ? StopLabel.PlateEdgeDp : FruitNewCard.EdgeDp;
            float trayTop = VictorySetup.CanvasRect((RectTransform)view.UndoButton.transform, root).yMin - 4f;
            Assert.That(card.yMax, Is.LessThanOrEqualTo(trayTop + 0.05f), where + ": no tapa la bandeja");
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
            if (fruit.OnPlate)
            {
                // La placa (3 filas o, con la escala de texto, donde la tarjeta no cabe), clavada al marco: sobre lo más alto de la
                // fila 1 en reposo con su aire (como el rótulo, FX4).
                float rowOne = float.MaxValue;
                for (int i = 0; i < view.Views.Length; i++)
                {
                    if (l.RowOf[i] == 0)
                    {
                        rowOne = Mathf.Min(rowOne, l.FootY[i] - (view.Views[i].HeightU * view.Views[i].Unit) - shift);
                    }
                }

                Assert.That(card.yMax, Is.LessThanOrEqualTo(rowOne - StopLabel.PlateCuiaGapDp + 0.05f), where + ": la placa " + card
                    + " no llega a la fila 1 (" + rowOne.ToString("0.0") + " dp)");
            }
            else
            {
                Assert.That(card.yMax <= cuiasTop + 0.05f || card.yMin >= boardBottom - 0.05f, Is.True, where + ": la tarjeta " + card
                    + " no tapa las cuias (de " + cuiasTop.ToString("0.0") + " a " + boardBottom.ToString("0.0") + " dp)");
            }

            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(view.TopRowDp + 48f - 0.05f), where + ": bajo la cabecera");
            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp - 0.05f), where + ": bajo la zona segura de arriba");
            Assert.That(card.xMin >= -0.05f && card.xMax <= view.CanvasWidth + 0.05f, Is.True, where + ": dentro de la pantalla");

            // El icono, la muestra y los textos (su tinta, en la placa: la caja de línea sobresale), dentro de la placa.
            Rect plate = VictorySetup.CanvasRect(fruit.Root, root);
            foreach (RectTransform part in new[] { fruit.Icon.rectTransform, fruit.Swatch.rectTransform, fruit.Texts[0].rectTransform,
                fruit.Texts[1].rectTransform })
            {
                if (!part.gameObject.activeInHierarchy || (fruit.OnPlate && part == fruit.Texts[1].rectTransform))
                {
                    continue;
                }

                Rect r = VictorySetup.CanvasRect(part, root);
                Assert.That(r.xMin >= plate.xMin - 0.05f && r.xMax <= plate.xMax + 0.05f && r.yMin >= plate.yMin - 0.05f
                    && r.yMax <= plate.yMax + 0.05f, Is.True, where + ": " + part.name + " " + r + " dentro de la placa " + plate);
            }

            foreach (TextMeshProUGUI t in fruit.Texts)
            {
                if (t.gameObject.activeInHierarchy)
                {
                    MinTextSizeTests.AssertTextFits(t, UiBuilder.TextScale, where);
                }
            }

            if (fruit.OnPlate)
            {
                TextMeshProUGUI tag = fruit.Texts[1];
                tag.ForceMeshUpdate(true, true);
                Assert.That(tag.textInfo.lineCount, Is.EqualTo(1), where + ": la frase, en una línea");
                Rect tagBox = VictorySetup.CanvasRect(tag.rectTransform, root);
                Assert.That(tagBox.xMax, Is.LessThanOrEqualTo(plate.xMax + 0.05f), where + ": la frase, dentro de la placa a lo ancho");
            }
        }
    }
}
