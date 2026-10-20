#nullable enable
using System.Collections;
using AF.Ads;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de la tarea A1 (12 sp como mínimo, pregunta 36 (a), D-020; escala de texto de 02 §12; el aviso con 3
    /// filas, preguntas 47 y 48; <c>[Explicit]</c>, no forman parte de la suite), al 100 % y al 130 % (<c>_130</c>) a 360 × 780 dp
    /// y, con <c>_16x9</c>, a 1080 × 1920 px (360 × 640 dp desde DC3):
    /// <list type="bullet">
    /// <item><description><c>a1_01_stuck_l31</c>: Aguas estancadas en el 31 sin semillas y con la última canoa (rama B: rescate
    /// en gris con «Te faltan 300 semillas», Puñado y Balsa);</description></item>
    /// <item><description><c>a1_02_seguro_l31</c>: ¿Seguro? con la última canoa; <c>a1_03_fallido_l31</c>: Nivel fallido;
    /// <c>a1_04_sin_canoas_l31</c>: Sin canoas (0/5);</description></item>
    /// <item><description><c>a1_05_aviso_2_lineas_l8</c>: el regalo de Capi del 8, el aviso más largo;</description></item>
    /// <item><description><c>a1_06_miquito_l5</c>: la tarjeta «¡Miquito a la vista!» del 5 (mono ardilla) cuando la tropa se
    /// va, y <c>a1_06b_miquito_dato_largo</c>: la del dato más largo del catálogo, en el mismo sitio, con el dato en dos líneas
    /// equilibradas al 130 % (sin 16:9: ahí, al 130 %, no cabe y no sale; pendiente del dueño);</description></item>
    /// <item><description><c>a1_07_l135_3_filas</c>: el 135 tras el rescate de verdad (12 cuias, 3 filas, las dos burbujas
    /// «Te faltan {n}»): «¡Una cuia más!» junto a la ficha «?×1» al 100 % y, al 130 %, tapándola entera (las dos ramas del
    /// compromiso ratificado en la pregunta 47, (1)); y
    /// <c>a1_07b_l135_revelar_sin_medios</c>: Revelar sin semillas ni video, sin aviso: la burbuja en lo alto de su latido y
    /// con los colores del precio sin saldo (pregunta 48 (b)); a 360 × 780 dp.</description></item>
    /// </list>
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.MinTextSnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class MinTextSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureFunnelModals()
        {
            string dir = SnapshotKit.Prepare();
            foreach ((float scale, bool sixteenNine, string suffix) in Variants())
            {
                yield return Load31(scale, sixteenNine, 0, 1);
                GameBootstrap boot = GameBootstrap.Current!;
                FunnelSetup.GetStuck(boot);
                yield return SaveNow(dir, "a1_01_stuck_l31" + suffix, boot);

                Assert.That(boot.PressGiveUp(), Is.True);
                PlayModeHelpers.Step(boot, 400);
                yield return SaveNow(dir, "a1_02_seguro_l31" + suffix, boot);

                Assert.That(boot.ConfirmGiveUp(), Is.True);
                PlayModeHelpers.Step(boot, 400);
                yield return SaveNow(dir, "a1_03_fallido_l31" + suffix, boot);

                Assert.That(boot.PressRetry(), Is.True);
                Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
                PlayModeHelpers.Step(boot, 400);
                yield return SaveNow(dir, "a1_04_sin_canoas_l31" + suffix, boot);
            }
        }

        [UnityTest]
        public IEnumerator CaptureNoticesAndCards()
        {
            string dir = SnapshotKit.Prepare();
            foreach ((float scale, bool sixteenNine, string suffix) in Variants())
            {
                // El regalo de Capi en el 8, al habilitarse el toque.
                Use(scale, sixteenNine);
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
                yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
                GameBootstrap boot = GameBootstrap.Current!;
                SnapshotKit.Attach(boot);
                PlayModeHelpers.TakeClock(boot);
                Assert.That(boot.TapAnywhere(), Is.True);
                PlayModeHelpers.Step(boot, 800);
                Assert.That(boot.View!.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
                yield return SaveNow(dir, "a1_05_aviso_2_lineas_l8" + suffix, boot);

                // La tarjeta del miquito en el 5, cuando la tropa se ha ido. Al 130 % en 16:9 la tarjeta (≈ 100 dp, con el dato en
                // dos líneas) no cabe ni en la franja del panorama (71 dp) ni sobre el muelle (99 dp) y no sale (la regla de
                // siempre: si no cabe sin tapar cuias, botones ni burbujas, no sale; MinTextSizeTests prueba sus textos a esa
                // escala y su camino de verdad; la pérdida, pendiente del dueño).
                if (sixteenNine)
                {
                    continue;
                }

                Use(scale, sixteenNine);
                yield return PlayModeHelpers.LoadGame(5, new MemoryProgressStore(5, 4));
                boot = GameBootstrap.Current!;
                SnapshotKit.Attach(boot);
                PlayModeHelpers.TakeClock(boot);
                Assert.That(boot.TapAnywhere(), Is.True);
                for (int t = 0; t < 3000 && !boot.View!.Sighting.Visible; t += PlayModeHelpers.FrameMs)
                {
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                }

                Assert.That(boot.View!.Sighting.Visible, Is.True, "la tarjeta del 5");
                PlayModeHelpers.Step(boot, 400);
                yield return SaveNow(dir, "a1_06_miquito_l5" + suffix, boot);

                // La del dato más largo del catálogo, en el mismo sitio (el alto crece si el dato va en dos líneas).
                FaunaSpecies? longest = null;
                foreach (FaunaSpecies s in boot.Fauna!.Species)
                {
                    if (longest == null || UiText.FaunaFact(s.FactKey).Length > UiText.FaunaFact(longest.FactKey).Length)
                    {
                        longest = s;
                    }
                }

                GameScreen view = boot.View;
                SightingCard card = view.Sighting;
                Rect was = VictorySetup.CanvasRect(card.Root, (RectTransform)view.Canvas.transform);
                string name = UiText.FaunaName(longest!.NameKey);
                string fact = UiText.FaunaFact(longest.FactKey);
                card.Hide();
                card.Measure(view.CanvasWidth, name, fact);
                card.Place(view.CanvasWidth, was.yMin);
                card.Show(view.CardArt.MonkeyArtFor(longest).Portrait, name, fact);
                PlayModeHelpers.Step(boot, 400);
                Debug.Log("AF_A1 captura a1_06b_miquito_dato_largo" + suffix + ": «" + fact + "», tarjeta de " + card.Height.ToString("0.0") + " dp");
                yield return SaveNow(dir, "a1_06b_miquito_dato_largo" + suffix, boot);
            }
        }

        [UnityTest]
        public IEnumerator CaptureThreeRowsNotice()
        {
            string dir = SnapshotKit.Prepare();
            foreach ((float scale, bool sixteenNine, string suffix) in Variants())
            {
                if (sixteenNine)
                {
                    continue; // 3 filas en 16:9: basta 360 × 780 (las de 16:9, en Canvas16x9SnapshotTests desde DC3)
                }

                // El 135 con las 300 semillas justas: el rescate de verdad y «¡Una cuia más!» junto a la ficha «?×1».
                Use(scale, false);
                LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(300));
                yield return PlayModeHelpers.LoadGame(135, new MemoryProgressStore(135, 134));
                GameBootstrap boot = GameBootstrap.Current!;
                SnapshotKit.Attach(boot);
                PlayModeHelpers.TakeClock(boot);
                boot.TapAnywhere();
                PlayModeHelpers.Step(boot, 1500);
                PlayModeHelpers.StepUntilIdle(boot, 4000);
                PhoneFixesTests.RescueForReal(boot, null, "a1_07");
                Assert.That(boot.View!.ToastText, Is.EqualTo(UiText.RescueApplied));
                Debug.Log("AF_A1 captura a1_07_l135_3_filas" + suffix + ": aviso en " + boot.View.NoticeToast.SeenRect);
                yield return SaveNow(dir, "a1_07_l135_3_filas" + suffix, boot);

                // Revelar sin medios: sin aviso nuevo; la burbuja «Te faltan 120» en lo alto de su latido.
                boot.View.NoticeToast.Hide();
                Assert.That(boot.PressReveal(), Is.False);
                Assert.That(boot.View.ToastText, Is.Null);
                PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(BoosterTray.MissingPulseBeatMs));
                yield return SaveNow(dir, "a1_07b_l135_revelar_sin_medios" + suffix, boot);
            }
        }

        // 100 % y 130 % a 360 × 780 dp, y 130 % a 1080 × 1920 px (360 × 640 dp desde DC3).
        private static (float Scale, bool SixteenNine, string Suffix)[] Variants()
        {
            return new[] { (1f, false, string.Empty), (1.3f, false, "_130"), (1.3f, true, "_130_16x9") };
        }

        private static void Use(float scale, bool sixteenNine)
        {
            LaunchOptions.Reset();
            FunnelSetup.Hooks();
            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = sixteenNine ? HudLabelTests.Screen640 : new Vector4(SnapshotKit.Width, SnapshotKit.Height, 96f, 48f);
        }

        private static IEnumerator Load31(float scale, bool sixteenNine, int seeds, int canoes)
        {
            Use(scale, sixteenNine);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds, canoes));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.TapAnywhere(), Is.True);
            PlayModeHelpers.Step(boot, 50);
        }

        // Con el lienzo en la cámara de las capturas, regenera los textos (PhoneFixesSnapshotTests: TextMeshPro no corrige la
        // escala SDF de los ya generados al cambiar de modo el lienzo) y guarda la captura.
        private static IEnumerator SaveNow(string dir, string name, GameBootstrap boot)
        {
            SnapshotKit.Attach(boot);
            yield return null;
            foreach (TMP_Text text in boot.View!.Canvas.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate(false, true);
            }

            SnapshotKit.Save(dir, name);
            Debug.Log("AF_A1 captura " + name);
        }
    }
}
