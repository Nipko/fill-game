#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Ads;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Arreglos de la prueba de la tanda 2 en el teléfono (T52; hoja <c>docs/dev/traspaso/pruebas-telefono-2026-10-13.md</c> y
    /// decisiones del dueño del 2026-10-13 sobre las preguntas 41-43 de <c>docs/GDD.md</c> §6.3):
    /// <list type="bullet">
    /// <item><description>pregunta 42 (b): el aviso a la vista se oculta en el fotograma en que se abre Aguas estancadas, no
    /// vuelve al cerrarlo, y el que esperaba en la cola sale después, como antes;</description></item>
    /// <item><description>observación 1 / pregunta 41 (4): el aviso no tapa las burbujas «Te faltan {n}» de la bandeja, con 1 y
    /// con 3 filas (el 135); sin burbujas tampoco tapa la repisa de abajo ni la ficha «?×n» del 135; con ellas no deja a medias
    /// ninguna ficha: o va junto a ella, ajustado a su texto (el 135 tras el rescate de verdad, con 12 cuias), o la tapa
    /// entera (el 115 tras el rescate de verdad; y un estado forzado del 135 con 11 cuias): el compromiso [P], ratificado por el
    /// dueño (pregunta 47, D-022); el aire del cartel no depende del texto anterior. Revelar sin semillas ni video ya no saca
    /// aviso: responde su burbuja, con un latido y el color del precio sin saldo, y con Reducir movimiento solo el color
    /// (pregunta 48 (b), D-022; tarea A1);</description></item>
    /// <item><description>pregunta 43: el banner de mundo admite la escala de fuente de 02 §12 hasta el 130 %, con las mismas
    /// comprobaciones que la tarjeta previa;</description></item>
    /// <item><description>observación 4: tras ver la victoria del 40, el banner del 41 entra con «Siguiente» sin esperar
    /// (<c>wait_ms</c> ≤ 16 ms y sin respaldo): la superficie de madera ya se pintó en otro hilo con su arte (el test la saca
    /// antes de la caché del arte compartido, así que no depende de los tests que corrieron antes);</description></item>
    /// <item><description>observación 5: por el Remolino, la cuia tocada se traga el chorro (remolino con contorno en su
    /// superficie y espiral que gira, se ilumina y late, con el remanso que se aclara) y la pareja se ilumina y late al recibir
    /// desde su remolino (su espiral, con el remanso aclarado), con burbujas y destellos en el fondo y nada pintado sobre su
    /// líquido ni su cavidad (revisión de T52, ronda 4; guía v2 §2.5, §4.1 y §7; D-003 y VA-06); sin brillos aditivos fuera de
    /// FxLayer (guía v2 §8), dentro del presupuesto de 2.800 ms, sin
    /// asignaciones por fotograma y, con Reducir movimiento, sin giros, latidos ni partículas (ni siquiera pedidas): solo
    /// fundidos.</description></item>
    /// </list>
    /// </summary>
    public sealed class PhoneFixesTests
    {
        private static ProceduralArt? s_art;
        private MechanicTestKit.Rig? _rig;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        // ---- 1 · Pregunta 42 (b) ----

        [UnityTest]
        public IEnumerator Notice_VisibleWhenAguasEstancadasOpens_HidesInThatFrame_NeverComesBack_TheQueuedOneComesAfter()
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1000));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            PlayModeHelpers.Step(boot, 50);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));

            // Un aviso a la vista (3 s) y otro esperando su turno (avisos reales que siguen tras la pregunta 48 (b)).
            string shown = UiText.RevealGiftToast;
            string waiting = UiText.ExtraCuiaGift;
            view.ShowNotice(shown);
            view.ShowNotice(waiting);
            Assert.That(view.ToastText, Is.EqualTo(shown));
            Assert.That(view.NoticesQueued, Is.EqualTo(1));

            // 3 → 4 bloquea el tablero 6 del tutorial: el bloqueo queda pendiente mientras se anima el vertido y después se abre
            // Aguas estancadas. En el fotograma en que se abre, el aviso ya no se ve.
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            string? lastPlaying = null;
            int t = 0;
            while (flow.Phase == FlowPhase.Playing && t < 5000)
            {
                lastPlaying = view.ToastText;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(lastPlaying, Is.EqualTo(shown), "condición del test: el aviso se veía en el último fotograma en juego (" + t + " ms)");
            Assert.That(view.ToastText, Is.Null, "se oculta en el fotograma en que se abre el modal (pregunta 42 b)");
            Assert.That(view.NoticeToast.Visible, Is.False);
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el que esperaba sigue en la cola");

            // Con el modal abierto no vuelve ni sale nada.
            for (int ms = 0; ms < 4000; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.ToastText, Is.Null, "con Aguas estancadas abierto (" + ms + " ms)");
            }

            // Al volver a jugar (el rescate): sale el que esperaba, después «¡Una cuia más!», y el que se ocultó no vuelve.
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.ToastText, Is.EqualTo(waiting), "el que esperaba sale al volver a jugar");
            var seen = new List<string>();
            for (int ms = 0; ms < 12000; ms += PlayModeHelpers.FrameMs)
            {
                string? now = view.ToastText;
                if (now != null && (seen.Count == 0 || seen[seen.Count - 1] != now))
                {
                    seen.Add(now);
                }

                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(seen, Is.EqualTo(new[] { waiting, UiText.RescueApplied }), "el aviso ocultado se descartó: no vuelve");
        }

        // ---- 3 · Observación 1 / pregunta 41 (4): el aviso y las burbujas «Te faltan {n}» ----

        [UnityTest]
        public IEnumerator Toast_DoesNotCoverTheMissingBubble_OneRow()
        {
            // El tablero del 6 (5 cuias en 1 fila) como nivel 46, sin semillas ni video: «Te faltan 150» sobre «+1 cuia».
            yield return LoadWithBubbles(46, 40, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(1));
            Assert.That(view.Tray.ExtraShowsMissing, Is.True, "la burbuja de «+1 cuia»");
            view.ShowNotice(UiText.RescueApplied);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied));
            AssertToastClearOfBubbles(view, "1 fila");
            Assert.That(view.NoticeToast.Height, Is.EqualTo(Toast.DefaultHeightDp), "con sitio, del alto de siempre");
            Assert.That(view.NoticeToast.Top, Is.GreaterThanOrEqualTo(BoardFloor(view)), "y sin tapar el tablero");
            AssertToastTextFits(view);

            // Sin burbujas, en su sitio de siempre: 8 dp sobre la bandeja, 56 dp de alto.
            yield return LoadWithBubbles(46, 40, 1000);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            Assert.That(view.Tray.ExtraShowsMissing, Is.False);
            view.ShowNotice(UiText.RescueApplied);
            PlayModeHelpers.Step(boot, 200);
            Rect undo = VictorySetup.CanvasRect((RectTransform)view.UndoButton.transform, Root(view));
            float trayTop = undo.yMin - 4f;
            Assert.That(view.NoticeToast.Top, Is.EqualTo(trayTop - 64f).Within(0.05f), "como antes: 64 dp sobre la bandeja");
            Assert.That(view.NoticeToast.Height, Is.EqualTo(Toast.DefaultHeightDp));
        }

        [UnityTest]
        public IEnumerator Toast_DoesNotCoverTheMissingBubbles_ThreeRows_Level135()
        {
            // El 135 (3 filas) sin semillas ni video: «Te faltan 150» y «Te faltan 120», en dos líneas (las dos ranuras vecinas),
            // como en el teléfono.
            yield return LoadWithBubbles(135, 0, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3), "el 135, de 3 filas");
            Assert.That(view.Tray.ExtraShowsMissing && view.Tray.RevealShowsMissing, Is.True, "las dos burbujas");
            Assert.That(VisibleChips(view).Count, Is.GreaterThan(0), "condición del test: la ficha «?×1» de la fila de abajo");
            BoardLayoutResult l = view.Layout;
            float foot = l.RowFootY[l.Rows - 1];

            // Estado FORZADO (el juego no lo da en el 135: tras el rescate de verdad hay 12 cuias y el aviso cabe junto a la ficha,
            // Toast_AfterTheRealRescue_ThreeRows): el aviso del rescate con 11 cuias, sin rescate. La ficha «?×1» queda bajo la
            // primera cuia de una fila de 3 y a su derecha no cabe el aviso ajustado a su texto (≈ 253 dp): va del ancho de siempre
            // y tapa enteras las fichas que alcanza, nunca las cuias ni las burbujas, como en el 115 tras el rescate de verdad
            // (captura 92f): tapa los 23 dp bajo el pie.
            view.ShowNotice(UiText.RescueApplied);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied));
            AssertToastClearOfBubbles(view, "3 filas, el rescate");
            AssertToastTextFits(view);
            Assert.That(view.NoticeToast.Width, Is.EqualTo(Toast.FullWidthDp(view.CanvasWidth)).Within(0.05f), "no cabe junto a la ficha: del ancho de siempre");
            Assert.That(view.NoticeToast.Top, Is.GreaterThanOrEqualTo(foot - 0.05f), "no tapa las cuias de la fila de abajo");
            int covered = AssertChipsWholeOrClear(view, "3 filas, el rescate");
            Assert.That(covered, Is.GreaterThan(0), "tapa la ficha entera, sin dejar una tira suelta");
            float coveredDp = Mathf.Max(0f, BoardFloor(view) - view.NoticeToast.Top);
            Assert.That(coveredDp, Is.LessThanOrEqualTo(GameScreen.ShelfShadowBelowFootDp + 0.05f), "como mucho, lo que hay bajo el pie");
            TestContext.WriteLine("135 con burbujas y 11 cuias (forzado), el rescate: tapa " + coveredDp.ToString("0.0") + " dp bajo la fila de abajo y " + covered + " ficha(s) enteras.");

            // «+1 Luciérnaga» (corto; forzado: el regalo solo sale en el 100): ajustado a su texto en el tramo sin fichas, lo más
            // centrado posible; no tapa ninguna ficha.
            view.NoticeToast.Hide();
            view.ShowNotice(UiText.RevealGiftToast);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(view.ToastText, Is.EqualTo(UiText.RevealGiftToast));
            AssertToastClearOfBubbles(view, "3 filas, «+1 Luciérnaga»");
            AssertToastTextFits(view);
            Assert.That(view.NoticeToast.Width, Is.LessThan(Toast.FullWidthDp(view.CanvasWidth) - 1f), "ajustado a su texto");
            Assert.That(view.NoticeToast.Width, Is.EqualTo(view.NoticeToast.FitWidthDp(view.CanvasWidth)).Within(0.05f));
            Assert.That(view.NoticeToast.Top, Is.GreaterThanOrEqualTo(foot - 0.05f), "no tapa las cuias de la fila de abajo");
            Assert.That(AssertChipsWholeOrClear(view, "3 filas, «+1 Luciérnaga»"), Is.EqualTo(0), "no tapa ninguna ficha");
            TestContext.WriteLine("135 con burbujas, «+1 Luciérnaga»: " + view.NoticeToast.SeenRect);

            // Sin burbujas, encogido a su texto entre la repisa (con su sombra y la ficha «?×n») y la bandeja: no la tapa (el texto
            // es forzado, con semillas no sale; la colocación es la de cualquier aviso sin burbujas, como el del rescate de 92b).
            yield return LoadWithBubbles(135, 0, 1000);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            Assert.That(view.Tray.ExtraShowsMissing || view.Tray.RevealShowsMissing, Is.False);
            view.ShowNotice(UiText.RescueApplied);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(view.NoticeToast.Top, Is.GreaterThanOrEqualTo(BoardFloor(view) + GameScreen.NoticeBoardGapDp - 0.05f), "sin tapar la repisa ni la ficha «?×n»");
            Assert.That(view.NoticeToast.Height, Is.LessThan(Toast.DefaultHeightDp).And.GreaterThanOrEqualTo(view.NoticeToast.MinHeightDp(view.CanvasWidth) - 0.05f));
            Assert.That(view.NoticeToast.Width, Is.EqualTo(Toast.FullWidthDp(view.CanvasWidth)).Within(0.05f));
            Assert.That(AssertChipsWholeOrClear(view, "3 filas sin burbujas"), Is.EqualTo(0));
            AssertToastTextFits(view);
        }

        [UnityTest]
        public IEnumerator RevealWithoutSeedsNorVideo_NoNotice_TheBubbleBeatsAndTakesThePriceColors_Level135()
        {
            // Pregunta 48 (b) (D-022; tarea A1): Revelar sin stock, saldo ni video en el 135 (3 filas, las dos burbujas en dos
            // líneas) no saca ningún aviso del tablero (antes, «Te faltan 120 semillas» por la cola, que repetía la burbuja y
            // tapaba la repisa de abajo). Responde la burbuja «Te faltan 120» de la ranura: un latido, sin pisar la burbuja
            // vecina, y los colores de la etiqueta de precio sin saldo, que vuelven después. Sin cobrar y sin asignaciones.
            yield return LoadWithBubbles(135, 0, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(tray.ExtraShowsMissing && tray.RevealShowsMissing, Is.True, "las dos burbujas");
            Assert.That(tray.RevealMissingText, Is.EqualTo("Te faltan 120"));
            Assert.That(tray.RevealMissingPulsing, Is.False);
            Rect bubble = VictorySetup.CanvasRect(tray.RevealMissingRect, Root(view));
            Rect neighbor = VictorySetup.CanvasRect(tray.ExtraMissingRect, Root(view));
            Assert.That(boot.PressReveal(), Is.False, "sin stock, saldo ni video: no hace nada más");
            Assert.That(view.ToastText, Is.Null, "ningún aviso del tablero");
            Assert.That(view.NoticesQueued, Is.EqualTo(0), "ni en la cola");
            Assert.That(boot.Flow!.RevealSelecting, Is.False);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0), "ningún cobro");
            Assert.That(tray.RevealMissingPulsing, Is.True, "responde la burbuja");
            Assert.That(tray.RevealMissingText, Is.EqualTo("Te faltan 120"), "sin texto nuevo");

            // El latido: crece hasta el tope (con la vecina a la vista, el que no la pisa) a mitad y vuelve a 1; el color entra
            // en 100 ms y se mantiene.
            float maxScale = tray.RevealMissingPulseMaxScale;
            Assert.That(maxScale, Is.GreaterThan(1f).And.LessThanOrEqualTo(BoosterTray.MissingPulseScale));
            float grown = bubble.width * (maxScale - 1f) * 0.5f;
            Assert.That(neighbor.xMax <= bubble.xMin - grown + 0.05f || neighbor.xMin >= bubble.xMax + grown - 0.05f, Is.True, "el latido no pisa la burbuja vecina");
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(BoosterTray.MissingPulseBeatMs));
            Assert.That(tray.RevealMissingPulseScale, Is.EqualTo(maxScale).Within(0.002f), "en lo alto del latido");
            Assert.That(tray.RevealMissingPulseColor, Is.EqualTo(1f), "con los colores del precio sin saldo");
            Assert.That(tray.RevealMissingLabel.color, Is.EqualTo((Color)Palette.Cream), "texto crema sobre el rojo");
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(BoosterTray.MissingPulseBeatMs) + 1);
            Assert.That(tray.RevealMissingPulseScale, Is.EqualTo(1f), "un solo latido");
            Assert.That(tray.RevealMissingPulseColor, Is.EqualTo(1f), "el color sigue");

            // Otro toque lo vuelve a empezar, sin aviso; sin asignaciones por fotograma.
            Assert.That(boot.PressReveal(), Is.False);
            Assert.That(view.ToastText, Is.Null);
            Assert.That(() =>
            {
                for (int f = 0; f < 20; f++)
                {
                    view.Tick(PlayModeHelpers.FrameMs);
                }
            }, Is.Not.AllocatingGCMemory(), "el latido y el color, sin asignaciones");
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(BoosterTray.MissingColorHoldMs + BoosterTray.MissingColorOutMs));
            Assert.That(tray.RevealMissingPulsing, Is.False, "≈ 1,1 s después vuelve a reposo");
            Assert.That(tray.RevealMissingPulseScale, Is.EqualTo(1f));
            Assert.That(tray.RevealMissingPulseColor, Is.EqualTo(0f));
            Assert.That(tray.RevealMissingLabel.color, Is.EqualTo((Color)Palette.UiRedWarning), "con sus colores de siempre");

            // Con Reducir movimiento, solo el color: sin latido.
            boot.Flow.ReduceMotion = true;
            Assert.That(boot.PressReveal(), Is.False);
            Assert.That(view.ToastText, Is.Null);
            Assert.That(tray.RevealMissingPulsing, Is.True);
            int held = 0;
            while (held + PlayModeHelpers.FrameMs <= BoosterTray.MissingColorHoldReducedMs)
            {
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                held += PlayModeHelpers.FrameMs;
                Assert.That(tray.RevealMissingPulseScale, Is.EqualTo(1f), "sin latido con Reducir movimiento (" + held + " ms)");
            }

            Assert.That(tray.RevealMissingPulseColor, Is.EqualTo(1f), "pero sí el color, hasta " + BoosterTray.MissingColorHoldReducedMs + " ms");
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(BoosterTray.MissingColorHoldReducedMs + BoosterTray.MissingColorOutMs) - held + 1);
            Assert.That(tray.RevealMissingPulsing, Is.False);
            Assert.That(tray.RevealMissingPulseColor, Is.EqualTo(0f));
            boot.Flow.ReduceMotion = false;
        }

        [UnityTest]
        public IEnumerator Toast_AfterTheRealRescue_ThreeRows_BesideTheChipIn135_CoversTheWholeChipIn115()
        {
            // El 135 con las 300 semillas justas: Aguas estancadas por un camino de vertidos útiles y el rescate comprado de verdad
            // (12 cuias, 4/4/4; sin semillas, las dos burbujas). La ficha «?×1» queda bajo la primera cuia de la fila de abajo y
            // «¡Una cuia más! Sigue desde aquí», ajustado a su texto, cabe a su derecha: no tapa ninguna ficha ni las cuias, solo la
            // cara de la repisa de abajo en su tramo, los 3 s que dura (el compromiso [P], captura 92).
            yield return LoadWithBubbles(135, 0, 300);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            RescueForReal(boot, null, "135");
            Assert.That(view.Views.Length, Is.EqualTo(12));
            Assert.That(view.Tray.ExtraShowsMissing && view.Tray.RevealShowsMissing, Is.True, "las dos burbujas, sin semillas");
            BoardLayoutResult l = view.Layout!;
            float foot = l.RowFootY[l.Rows - 1];
            AssertToastClearOfBubbles(view, "135 tras el rescate");
            AssertToastTextFits(view);
            Assert.That(view.NoticeToast.Top, Is.GreaterThanOrEqualTo(foot - 0.05f), "no tapa las cuias de la fila de abajo");
            Assert.That(view.NoticeToast.Top, Is.LessThan(BoardFloor(view) - 1f), "tapa la cara de la repisa de abajo en su tramo");
            Assert.That(view.NoticeToast.Width, Is.EqualTo(view.NoticeToast.FitWidthDp(view.CanvasWidth)).Within(0.05f), "ajustado a su texto");
            Assert.That(view.NoticeToast.Width, Is.LessThan(Toast.FullWidthDp(view.CanvasWidth) - 1f));
            Assert.That(AssertChipsWholeOrClear(view, "135 tras el rescate"), Is.EqualTo(0), "junto a la ficha «?×1», sin taparla");
            TestContext.WriteLine("135 tras el rescate (12 cuias): «¡Una cuia más!» en " + view.NoticeToast.SeenRect + "; pie de la fila de abajo a " + foot.ToString("0.0") + " dp.");

            // Revelar sin semillas con el aviso del rescate a la vista: no se encola ningún aviso (pregunta 48 (b)); responde la
            // burbuja y el aviso del rescate sigue en su sitio.
            Rect before = view.NoticeToast.SeenRect;
            Assert.That(boot.PressReveal(), Is.False);
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied), "el del rescate sigue");
            Assert.That(view.NoticesQueued, Is.EqualTo(0), "nada espera su turno");
            Assert.That(view.Tray.RevealMissingPulsing, Is.True, "responde la burbuja");
            PlayModeHelpers.Step(boot, 200);
            Assert.That(view.NoticeToast.SeenRect, Is.EqualTo(before), "el aviso no se mueve");
            AssertToastClearOfBubbles(view, "135 tras el rescate, Revelar sin semillas");
            Assert.That(AssertChipsWholeOrClear(view, "135 tras el rescate, Revelar sin semillas"), Is.EqualTo(0));

            // El 115, igual: tras el rescate (12 cuias), la cuia 10, con 2 ocultas, es la tercera de la fila de abajo y su ficha
            // «?×2» parte el tramo de la repisa. El aviso del rescate no cabe ajustado a su texto a ningún lado: va del ancho de
            // siempre y tapa entera la ficha (sin dejar una tira suelta), nunca las cuias ni las burbujas. Es la otra rama del
            // compromiso [P], que se da en el juego (captura 92f): tapa la repisa de abajo y su ficha los 3 s que dura.
            yield return LoadWithBubbles(115, 0, 300);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            RescueForReal(boot, s => s[10].HasHidden, "115");
            Assert.That(view.Views.Length, Is.EqualTo(12));
            Assert.That(view.Tray.ExtraShowsMissing && view.Tray.RevealShowsMissing, Is.True, "las dos burbujas, sin semillas");
            l = view.Layout!;
            foot = l.RowFootY[l.Rows - 1];
            Assert.That(l.Rows, Is.EqualTo(3));
            AssertToastClearOfBubbles(view, "115 tras el rescate");
            AssertToastTextFits(view);
            Assert.That(view.NoticeToast.Width, Is.EqualTo(Toast.FullWidthDp(view.CanvasWidth)).Within(0.05f), "no cabe junto a la ficha: del ancho de siempre");
            Assert.That(view.NoticeToast.Top, Is.GreaterThanOrEqualTo(foot - 0.05f), "no tapa las cuias de la fila de abajo");
            int covered = AssertChipsWholeOrClear(view, "115 tras el rescate");
            Assert.That(covered, Is.GreaterThan(0), "tapa la ficha «?×2» entera");
            TestContext.WriteLine("115 tras el rescate (12 cuias): «¡Una cuia más!» en " + view.NoticeToast.SeenRect + ", tapa " + covered + " ficha(s); pie de la fila de abajo a " + foot.ToString("0.0") + " dp.");
        }

        [UnityTest]
        public IEnumerator NoAllocationsPerFrame_NoticeBesideTheChips_Level135()
        {
            // 08 §5.4: el aviso que busca sitio entre las fichas de la repisa (cada fotograma) no asigna.
            yield return LoadWithBubbles(135, 0, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TickExact(boot, 1200); // en régimen: los ~2,2 s que siguen al salto de la intro asignan (NoticeTests)
            foreach (string text in new[] { UiText.RescueApplied, UiText.RevealGiftToast, UiText.ExtraCuiaGift })
            {
                view.NoticeToast.Hide();
                view.ShowNotice(text);
                view.Tick(PlayModeHelpers.FrameMs); // la primera colocación mide el texto (una vez por texto)
                Assert.That(view.ToastText, Is.EqualTo(text));
                Assert.That(() =>
                {
                    for (int f = 0; f < 30; f++)
                    {
                        view.Tick(PlayModeHelpers.FrameMs);
                    }
                }, Is.Not.AllocatingGCMemory(), "30 fotogramas con «" + text + "» junto a las fichas");
            }
        }

        [UnityTest]
        public IEnumerator Toast_PadFollowsItsOwnText_NotTheOneShownBefore()
        {
            // Revisión de T52, ronda 2: el cartel («El río bajó», el rótulo del intento) se coloca antes de mostrar su texto y
            // lleva siempre 6 dp de aire arriba y abajo, como antes de T52, sea cual sea el texto que se mostró antes; el aviso
            // colocado con PlaceAt reajusta su aire al texto de cada Show.
            yield return null;
            var go = new GameObject("ToastPadTest", typeof(RectTransform), typeof(Canvas));
            var banner = new Toast("Banner", go.transform, Art, 18f);
            banner.Place(360f, 100f);
            banner.Show(UiText.RiverLoweredColor, 2000f);
            Assert.That(banner.TextPadDp, Is.EqualTo(6f), "«" + UiText.RiverLoweredColor + "»");
            banner.Hide();
            banner.Place(360f, 100f); // como GameScreen: se coloca con el texto anterior aún en la etiqueta
            banner.Show(UiText.LevelAttempt(31, 2), 2000f);
            Assert.That(banner.TextPadDp, Is.EqualTo(6f), "el rótulo del intento tras «El río bajó»: el aire de siempre");

            var toast = new Toast("Toast", go.transform, Art, 15f);
            toast.Show(UiText.RiverLoweredColor, 3000f);
            toast.PlaceAt(20f, 100f, 160f, Toast.DefaultHeightDp); // estrecha: el texto largo va en varias líneas
            float padLong = toast.TextPadDp;
            toast.Show(UiText.RevealGiftToast, 3000f); // sin volver a colocarla
            float padShort = toast.TextPadDp;
            toast.PlaceAt(20f, 100f, 160f, Toast.DefaultHeightDp);
            Assert.That(padShort, Is.EqualTo(toast.TextPadDp), "Show reajusta el aire al texto nuevo, como una colocación nueva");
            Assert.That(padShort, Is.GreaterThan(padLong), "condición del test: el texto corto deja más aire que el largo");
            Object.Destroy(go);
        }

        // ---- 2 · Pregunta 43: el banner de mundo admite la escala de fuente de 02 §12 ----

        [UnityTest]
        public IEnumerator Banner_FontScale100And130_NoOverflowNorOverlap()
        {
            // 02 §12: hasta el 130 % sin desbordes ni recortes; los contenedores crecen. El 41 a 100 % y a 130 % a 360 × 780 dp y
            // en 16:9 (438,75 × 780 dp), y el 11, el 81 y el 141 a 130 %.
            foreach (Vector4 screen in new[] { HudLabelTests.Screen780, HudLabelTests.Screen640 })
            {
                GameScreen.SimulatedScreen = screen;
                yield return CardSetup.Load(41);
                GameScreen view = GameBootstrap.Current!.View!;
                string where = "41 a " + screen.x + " × " + screen.y;
                Assert.That(view.WorldBannerView.Visible, Is.True, where);
                view.BannerFontScale = 1f;
                PlayModeHelpers.Step(GameBootstrap.Current!, 50);
                AssertBannerFits(view, 1f, where + " al 100 %");
                float baseHeight = view.WorldBannerView.Panel.rect.height;
                view.BannerFontScale = 1.3f;
                PlayModeHelpers.Step(GameBootstrap.Current!, 50);
                Assert.That(view.BannerFontScale, Is.EqualTo(1.3f));
                AssertBannerFits(view, 1.3f, where + " al 130 %");
                Assert.That(view.WorldBannerView.Panel.rect.height, Is.GreaterThan(baseHeight), "la tarjeta crece: " + where);
                view.BannerFontScale = 2f;
                Assert.That(view.BannerFontScale, Is.EqualTo(LevelCardView.MaxFontScale), "por encima del 130 %, el tope (02 §12)");
            }

            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            foreach (int level in new[] { 11, 81, 141 })
            {
                yield return CardSetup.Load(level);
                GameScreen view = GameBootstrap.Current!.View!;
                Assert.That(view.WorldBannerView.Visible, Is.True, "nivel " + level);
                view.BannerFontScale = 1.3f;
                PlayModeHelpers.Step(GameBootstrap.Current!, 50);
                AssertBannerFits(view, 1.3f, "nivel " + level + " al 130 %");
            }
        }

        // ---- 4 · Observación 4: wait_ms del banner al entrar con «Siguiente» ----

        [UnityTest]
        public IEnumerator Banner_AfterSeeingTheVictoryOf40_Next_EntersWithoutWaiting()
        {
            // La hoja de la tanda 2 esperaba wait_ms ≈ 0 entrando con «Siguiente» y el teléfono midió 66-111 ms: el arte se pedía
            // en la victoria, pero la superficie de madera de la tarjeta se pintaba en el hilo principal al abrir el banner (unos
            // 220 ms en el Editor) y wait_ms lo contaba. Ahora la pinta el mismo trabajo del arte, en otro hilo.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtTimeoutMsOverride = 60000f;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            ProceduralArt art = view.CardArt;
            art.ReleaseBannerArt(); // el arte es compartido entre escenas: nada de un banner de otro test
            if (flow.Phase == FlowPhase.LevelCard)
            {
                Assert.That(boot.TapAnywhere(), Is.True, "la tarjeta previa del 40, si la hay");
            }

            // Condición de partida fija: la superficie de 328 × 281 dp, fuera de la caché del arte compartido de la sesión (las
            // tarjetas del 100 y del 183 y el banner del 41 de otros tests ya la habían generado: sin esto el test pasaría aunque
            // el banner la pintara en el hilo principal al abrirse, como en f64c19a).
            var surface = new Vector2(LevelCardView.MaxWidthDp - 16f, 281f);
            art.ForgetModalSurface(surface.x, surface.y);
            Assert.That(art.HasModalSurface(surface.x, surface.y), Is.False, "condición del test: la superficie, sin generar");
            Assert.That(art.ModalSurfaceOriginOf(surface.x, surface.y), Is.EqualTo(ModalSurfaceOrigin.None));

            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(art.BannerArtPending, Is.True, "en la victoria del 40, el arte del banner del 41, pedido");
            Assert.That(art.WaitForBannerArt(30000), Is.True, "pintado en otro hilo mientras se ve la victoria");

            Assert.That(boot.PressNext(), Is.True);
            for (int ms = 0; ms < 5000 && flow.LevelNumber != 41; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            WorldBannerView banner = view.WorldBannerView;
            Assert.That(flow.LevelNumber, Is.EqualTo(41));
            Assert.That(flow.WorldBanner, Is.EqualTo(3));
            Assert.That(banner.AsyncArt, Is.True);
            Assert.That(banner.CardShown && banner.ArtReady, Is.True, "entra en el acto, con su arte");
            Assert.That(banner.SurfaceSizeDp, Is.EqualTo(surface), "la superficie compartida de 281 dp");
            Assert.That(art.HasModalSurface(surface.x, surface.y), Is.True, "la superficie llegó con el arte");
            Assert.That(art.ModalSurfaceOriginOf(surface.x, surface.y), Is.EqualTo(ModalSurfaceOrigin.OtherThread), "pintada en el trabajo del arte, no en el hilo principal al abrir el banner");
            Assert.That(banner.ArtWaitMs, Is.LessThanOrEqualTo(16f), "wait_ms ≈ 0 (≤ 16 ms en el Editor)");
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"level\":41").And.Contain("\"fallback\":\"none\""));
            TestContext.WriteLine("AF_STATE world_banner_art tras «Siguiente»: " + view.LastWorldBannerArtProbe);
            PlayModeHelpers.Step(boot, 1500);
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 41, 3);
        }

        // ---- 5 · Observación 5: el Remolino se lee ----

        [UnityTest]
        public IEnumerator Remolino103_TheTouchedCuiaSwallowsTheStream_ThePartnerLightsUp_WithinTheBudget()
        {
            yield return MechanicTestKit.LoadPlaying(103);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            BoardAnimator animator = view.Animator;
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.ThroughRemolino);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            Pour pour = path![path.Length - 1];
            BoardState before = boot.Flow!.Session.Current;
            float tapAt = animator.Clock;
            PourVisual visual = MechanicTestKit.TapPour(boot, pour, out TapResult poured);
            int touched = pour.Target;
            int partner = poured.Pour.Receiver;
            Assert.That(partner, Is.Not.EqualTo(touched));
            RemolinoOverlay intake = Whirl(view.Views[touched]);
            RemolinoOverlay receive = Whirl(view.Views[partner]);

            // La cabeza del chorro llega a la cuia tocada: se lo traga.
            int t = 0;
            while (!intake.IntakeActive && t < 2000)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
            }

            float arrive = (view.Timings.TravelMs * BoardAnimator.StreamStartFraction) + BoardAnimator.StreamGrowMs;
            Assert.That(intake.IntakeActive, Is.True, "la cuia tocada se traga el chorro");
            Assert.That(t, Is.InRange(Mathf.FloorToInt(arrive), Mathf.CeilToInt(arrive) + PlayModeHelpers.FrameMs), "cuando la cabeza del chorro llega");
            Assert.That(intake.IntakeSwirlVisible, Is.True, "el remolino de su superficie");
            Assert.That(intake.IntakeSwirlCenterU.x, Is.EqualTo(0f).Within(1e-3f), "en su eje, donde acaba el chorro");
            Assert.That(intake.IntakeSwirlCenterU.y, Is.EqualTo(CuiaGeometry.CavityBottomU + before[touched].Count).Within(0.01f), "sobre su líquido");
            Assert.That(intake.IntakeSwirlSizeU.x, Is.LessThanOrEqualTo(2f * CuiaGeometry.CavityHalfWidthU), "dentro de la cavidad");
            float angle = intake.SpiralAngle;
            PlayModeHelpers.Step(boot, 160);
            Assert.That(intake.IntakeActive, Is.True);
            Assert.That(intake.SpiralFoamAlpha, Is.GreaterThan(RemolinoOverlay.FoamAlpha + 0.2f), "la espiral de la base se ilumina");
            Assert.That(intake.SpiralScale, Is.GreaterThan(1.08f), "y late (hasta 1,15)");
            Assert.That(intake.PoolLightNow, Is.GreaterThan(0.9f * RemolinoOverlay.PoolLightMix), "y el remanso entero se aclara");
            AssertPoolLit(intake, "la cuia tocada");
            Assert.That(intake.AdditiveImageCount, Is.EqualTo(0), "sin brillos aditivos fuera de FxLayer (guía v2 §8)");
            Assert.That(intake.SpiralAngle, Is.Not.EqualTo(angle), "y gira deprisa");
            Assert.That(intake.IntakeSwirlAlpha, Is.GreaterThan(0.5f));
            Assert.That(intake.IsAnimating, Is.False, "acompaña al vertido: no es un paso");
            Assert.That(animator.ShownOf(touched), Is.EqualTo(before[touched]), "la cuia tocada no cambia");

            // La pareja recibe por el fondo: se ilumina y late; la cuia tocada ya se apagó.
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Receive, partner, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Receive, partner, 0.6f);
            Assert.That(receive.SpiralFoamAlpha, Is.GreaterThan(RemolinoOverlay.FoamAlpha + 0.2f), "la pareja se ilumina al recibir");
            Assert.That(receive.SpiralScale, Is.GreaterThan(1.08f), "y late");
            Assert.That(receive.PoolLightNow, Is.GreaterThan(0.5f * RemolinoOverlay.PoolLightMix), "con el remanso aclarado");
            AssertNothingOnTheBody(view.Views[partner], "la pareja, al 60 % de la recepción");
            Assert.That(receive.AdditiveImageCount, Is.EqualTo(0), "sin brillos aditivos fuera de FxLayer (guía v2 §8)");
            Assert.That(intake.IntakeActive || intake.IntakeSwirlVisible, Is.False, "la cuia tocada ya se apagó");
            Assert.That(intake.PoolLightNow, Is.EqualTo(0f), "y su remanso también");
            Assert.That(intake.PoolColorNow, Is.EqualTo(ProceduralArt.WhirlPoolColor), "con su color de siempre");
            Assert.That(intake.SpiralFoamAlpha, Is.EqualTo(RemolinoOverlay.FoamAlpha).Within(1e-3f));
            Assert.That(intake.SpiralScale, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(intake.SpiralAngle, Is.EqualTo(0f).Within(0.01f));

            MechanicTestKit.StepToIdle(boot, 6000);
            float elapsed = animator.Clock - tapAt;
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta (2.800 ms)");
            MechanicStepRecord record = MechanicTestKit.Record(animator, MechanicStepKind.Receive, partner);
            Assert.That(record.EndMs - record.StartMs, Is.EqualTo(RemolinoOverlay.ReceiveMs * record.SpeedFactor).Within(1f), "la recepción dura lo mismo");
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(receive.SpiralFoamAlpha, Is.EqualTo(RemolinoOverlay.FoamAlpha).Within(1e-3f), "en reposo, la espuma de siempre");
            Assert.That(receive.SpiralScale, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(receive.PoolLightNow, Is.EqualTo(0f), "y el remanso de siempre");
            Assert.That(receive.PoolColorNow, Is.EqualTo(ProceduralArt.WhirlPoolColor));
            AssertNothingOnTheBody(view.Views[partner], "la pareja, en reposo");
            TestContext.WriteLine("Nivel 103: " + elapsed + " ms del toque a la vista quieta.");
        }

        [UnityTest]
        public IEnumerator Remolino140_FromTheTapToIdle_WithinTheBudget()
        {
            yield return MechanicTestKit.LoadPlaying(140);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.ThroughRemolino);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            Pour pour = path![path.Length - 1];
            float tapAt = view.Animator.Clock;
            PourVisual visual = MechanicTestKit.TapPour(boot, pour, out _);
            bool swallowed = false;
            for (int t = 0; t < 6000 && !view.IsViewIdle; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                swallowed |= Whirl(view.Views[pour.Target]).IntakeActive;
            }

            float elapsed = view.Animator.Clock - tapAt;
            Assert.That(swallowed, Is.True, "la cuia tocada se tragó el chorro");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta (2.800 ms)");
            MechanicTestKit.AssertLogFollowsPlan(view.Animator, in visual);
            MechanicTestKit.AssertViewMatchesModel(boot);
            TestContext.WriteLine("Nivel 140: " + elapsed + " ms del toque a la vista quieta.");
        }

        [UnityTest]
        public IEnumerator Remolino_SparklesAtTheBottom_AndReduceMotion_WithoutSpinsNorParticles()
        {
            yield return null;
            BoardState board = PairBoard();
            for (int pass = 0; pass < 2; pass++)
            {
                bool reduce = pass == 1;
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, board);
                var fx = new CountingFx { ReduceMotion = reduce };
                _rig.Animator.Fx = fx;
                _rig.Animator.ReduceMotion = reduce;
                RemolinoOverlay intake = Whirl(_rig.Views[0]);
                RemolinoOverlay partner = Whirl(_rig.Views[2]);
                BoardState after = _rig.Pour(board, 1, 0, out PourVisual v);
                Assert.That(v.Receiver, Is.EqualTo(2));
                bool lit = false;
                bool spun = false;
                bool pulsed = false;
                bool swirl = false;
                bool poolLit = false;
                bool partnerPoolLit = false;
                bool partnerLit = false;
                int partnerLitFrames = 0;
                for (int t = 0; t < 4000 && !_rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
                {
                    _rig.Tick(PlayModeHelpers.FrameMs);
                    lit |= intake.SpiralFoamAlpha > RemolinoOverlay.FoamAlpha + 0.1f;
                    spun |= Mathf.Abs(intake.SpiralAngle) > 0.01f;
                    pulsed |= intake.SpiralScale > 1.001f;
                    swirl |= intake.IntakeSwirlVisible && intake.IntakeSwirlAlpha > 0.3f;
                    poolLit |= intake.PoolLightNow > 0.5f * RemolinoOverlay.PoolLightMix;
                    partnerPoolLit |= partner.PoolLightNow > 0.5f * RemolinoOverlay.PoolLightMix;
                    partnerLit |= partner.SpiralFoamAlpha > RemolinoOverlay.FoamAlpha + 0.2f;
                    partnerLitFrames += partner.SpiralFoamAlpha > RemolinoOverlay.FoamAlpha + 0.01f ? 1 : 0;
                    Assert.That(intake.AdditiveImageCount + partner.AdditiveImageCount, Is.EqualTo(0), "sin brillos aditivos fuera de FxLayer");
                    AssertNothingOnTheBody(_rig.Views[2], "la pareja a los " + t + " ms");
                }

                string what = reduce ? "con Reducir movimiento" : "normal";
                Assert.That(_rig.Animator.IsIdle, Is.True, what);
                Assert.That(_rig.Animator.ShownOf(2), Is.EqualTo(after[2]), what);
                Assert.That(lit && swirl, Is.True, what + ": la espuma se ilumina y el remolino de la superficie se funde");
                Assert.That(poolLit && partnerPoolLit && partnerLit, Is.True, what + ": los remansos se aclaran y la espiral de la pareja se ilumina (fundidos, también con Reducir movimiento)");
                Assert.That(partnerLitFrames, Is.GreaterThan(3), what + ": la espiral de la pareja se ilumina varios fotogramas");
                Assert.That(intake.PoolLightNow + partner.PoolLightNow, Is.EqualTo(0f), what + ": al terminar, en reposo");
                Assert.That(partner.SpiralFoamAlpha, Is.EqualTo(RemolinoOverlay.FoamAlpha).Within(1e-3f), what + ": la espuma de siempre");
                Assert.That(partner.PoolColorNow, Is.EqualTo(ProceduralArt.WhirlPoolColor), what);
                Assert.That(spun, Is.EqualTo(!reduce), what + ": la espiral gira solo sin Reducir movimiento");
                Assert.That(pulsed, Is.EqualTo(!reduce), what + ": y late");
                Assert.That(intake.IntakeActive || intake.IntakeSwirlVisible, Is.False, what + ": al terminar, en reposo");
                if (reduce)
                {
                    Assert.That(fx.Sparkles + fx.Bubbles, Is.EqualTo(0), "con Reducir movimiento, el Remolino no pide partículas");
                }
                else
                {
                    Assert.That(fx.Sparkles, Is.EqualTo(RemolinoOverlay.SparklesBottom), "destellos en el fondo de la pareja");
                    CuiaView rv = _rig.Views[2];
                    Vector2 bottom = rv.FootPosition + new Vector2(0f, CuiaGeometry.CavityBottomU * rv.Unit);
                    Assert.That(Vector2.Distance(fx.FirstSparkleAt, bottom), Is.LessThan(0.5f * rv.Unit), "en el fondo de la cavidad");
                    Assert.That(fx.Sparkles + fx.Bubbles, Is.LessThanOrEqualTo(FxLayer.MaxParticles), "dentro del tope");
                    Assert.That(fx.Splashes, Is.EqualTo(0), "el chorro no salpica en la cuia tocada: se lo traga");
                }
            }
        }

        [UnityTest]
        public IEnumerator Remolino_NoAllocationsPerFrame_WhileSwallowingAndReceiving()
        {
            yield return null;
            BoardState board = PairBoard();

            // Calentamiento en un banco (JIT y sprites) y medida en otro nuevo, como RemolinoBarroTests.NoAllocDuringSteps.
            for (int pass = 0; pass < 2; pass++)
            {
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, board);
                MechanicTestKit.Rig rig = _rig;
                rig.Pour(board, 1, 0, out _);
                if (pass == 0)
                {
                    rig.RunUntilIdle(4000);
                    continue;
                }

                Assert.That(() => { rig.Run(60 * PlayModeHelpers.FrameMs); }, Is.Not.AllocatingGCMemory(), "60 fotogramas desde el toque (el chorro y la recepción)");
                Assert.That(() => { rig.RunUntilIdle(4000); }, Is.Not.AllocatingGCMemory(), "hasta la vista quieta");
                Assert.That(rig.Animator.IsIdle, Is.True);
            }
        }

        // ---- Ayudas ----

        /// <summary>
        /// El rescate de verdad (también lo usan las capturas 92, 92b, 92f, 93 y 93b): desde el nivel ya cargado, con 300
        /// semillas o más, juega un camino de vertidos útiles hasta Aguas estancadas con el rescate a la venta (y, si
        /// <paramref name="accept"/> no es null, con el tablero bloqueado que cumple <paramref name="accept"/>), compra
        /// «Continuar: +1 cuia» (300, el primero del día) y deja la vista con el aviso «¡Una cuia más!» ya entero y la cuia añadida
        /// en su sitio. Devuelve los vertidos del camino.
        /// </summary>
        internal static int RescueForReal(GameBootstrap boot, System.Func<BoardState, bool>? accept, string what)
        {
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Pour[]? path = FindRescuableStuck(flow, accept);
            Assert.That(path, Is.Not.Null, what + ": un camino de vertidos útiles hasta Aguas estancadas con el rescate a la venta");
            MechanicTestKit.PlayPrefix(boot, path!, false);
            for (int ms = 0; ms < 5000 && flow.Phase != FlowPhase.StuckModal; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal), what);
            PlayModeHelpers.Step(boot, 300); // el panel termina de subir
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Available), what);
            Assert.That(view.Stuck.RescuePriceShown, Is.EqualTo(300), what + ": el primer rescate del día");
            int seeds = boot.Wallet!.Seeds;
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), what);
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(seeds - 300), what + ": cobra los 300");
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied), what);
            PlayModeHelpers.Step(boot, 700); // la recolocación de «+1 cuia» (600 ms) y el aviso ya entero
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied), what);
            return path!.Length;
        }

        // Un camino de vertidos útiles desde el tablero de ahora hasta Aguas estancadas con el rescate a la venta (04 §6.4:
        // resoluble con una cuia más) y, si accept no es null, con el tablero bloqueado que lo cumple; en profundidad, sin
        // repetir tableros ni ganar; null si no lo encuentra. Cada vertido se simula con LevelSession, la misma semántica que la
        // partida: R13 y su ampliación por bucle (D-014, el cierre acotado sin victoria), así que el camino se corta en el primer
        // Aguas estancadas, tras el que la partida ignora los toques (R15). Antes buscaba solo con R13 de reglas
        // (PourRules.IsStuck) y, con D-014, la partida se bloqueaba antes del final del camino.
        private static Pour[]? FindRescuableStuck(LevelFlow flow, System.Func<BoardState, bool>? accept)
        {
            var search = new StuckSearch(flow.Session.PourMode, flow.Session.FutileRuleEnabled, new RescueBooster(flow.Boosters), accept);
            return search.Run(flow.Session.Current);
        }

        // Búsqueda en profundidad de FindRescuableStuck (solo tests: asigna sin cuidado; una LevelSession por vertido, para no
        // arrastrar entre ramas lo revelado, que Marea no vuelve a ocultar: I5).
        private sealed class StuckSearch
        {
            private const int MaxDepth = 60;
            private const int MaxStates = 50000;

            private readonly PourMode _mode;
            private readonly bool _futile;
            private readonly RescueBooster _rescue;
            private readonly System.Func<BoardState, bool>? _accept;
            private readonly HashSet<BoardState> _seen = new HashSet<BoardState>();
            private readonly List<Pour> _path = new List<Pour>();

            public StuckSearch(PourMode mode, bool futile, RescueBooster rescue, System.Func<BoardState, bool>? accept)
            {
                _mode = mode;
                _futile = futile;
                _rescue = rescue;
                _accept = accept;
            }

            public Pour[]? Run(BoardState start)
            {
                return Visit(start, 0) ? _path.ToArray() : null;
            }

            private bool Visit(BoardState state, int depth)
            {
                if (depth >= MaxDepth || _seen.Count >= MaxStates || !_seen.Add(state))
                {
                    return false;
                }

                var moves = new Pour[PourRules.MaxPours];
                int n = PourRules.CollectUsefulMoves(state, _futile, _mode, moves);
                for (int i = 0; i < n; i++)
                {
                    // El vertido como en la partida: LevelSession desde este tablero (en juego) evalúa R10 (g) y (h) con D-014.
                    var game = new LevelSession(state, _futile, _mode);
                    Assert.That(game.Status, Is.EqualTo(LevelStatus.Playing), "el buscador solo sigue por tableros en juego");
                    Assert.That(game.TryPour(moves[i].Source, moves[i].Target, out PourResult poured), Is.True, "un vertido útil es legal");
                    BoardState next = game.Current;
                    if (poured.Status == LevelStatus.Won)
                    {
                        continue;
                    }

                    _path.Add(moves[i]);
                    if (poured.Status == LevelStatus.Stuck)
                    {
                        // El primer Aguas estancadas del camino: aquí se corta, con rescate o sin él.
                        if (RescueBooster.IsOffered(_rescue.Check(next, _mode, _futile)) && (_accept == null || _accept(next)))
                        {
                            return true;
                        }
                    }
                    else if (Visit(next, depth + 1))
                    {
                        return true;
                    }

                    _path.RemoveAt(_path.Count - 1);
                }

                return false;
            }
        }

        // Nivel level (con LaunchOptions.LevelOffsetOverride = offset si no es 0) con seeds semillas, sin video ni Luciérnagas,
        // en juego con la vista quieta.
        private static IEnumerator LoadWithBubbles(int level, int offset, int seeds)
        {
            LaunchOptions.Reset();
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            if (offset != 0)
            {
                LaunchOptions.LevelOffsetOverride = offset;
            }

            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        private static RectTransform Root(GameScreen view)
        {
            return (RectTransform)view.Canvas.transform;
        }

        // Lo más bajo del tablero (dp desde arriba): la sombra de la repisa de la última fila.
        private static float BoardFloor(GameScreen view)
        {
            BoardLayoutResult l = view.Layout!;
            return l.RowFootY[l.Rows - 1] + GameScreen.ShelfShadowBelowFootDp;
        }

        // El aviso, con su canto de abajo, no se solapa con ninguna burbuja «Te faltan {n}» a la vista, ni va detrás de ellas.
        private static void AssertToastClearOfBubbles(GameScreen view, string what)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = Root(view);
            Rect toast = VictorySetup.CanvasRect(view.NoticeToast.Root, root);
            toast.yMax += Toast.EdgeDp;
            BoosterTray tray = view.Tray;
            int bubbles = 0;
            foreach (RectTransform bubble in new[] { tray.ExtraMissingRect, tray.RevealMissingRect })
            {
                if (!bubble.gameObject.activeInHierarchy)
                {
                    continue;
                }

                bubbles++;
                Rect r = VictorySetup.CanvasRect(bubble, root);
                Assert.That(toast.Overlaps(r), Is.False, what + ": el aviso " + toast + " no tapa la burbuja " + r);
                Assert.That(toast.yMax, Is.LessThanOrEqualTo(r.yMin - GameScreen.NoticeBubbleGapDp + 0.05f), what + ": queda por encima, con aire");
            }

            Assert.That(bubbles, Is.GreaterThan(0), what + ": alguna burbuja a la vista");
            Assert.That(tray.TryGetMissingTop(out float top), Is.True);
            Assert.That(view.NoticeToast.Top + view.NoticeToast.Height + Toast.EdgeDp, Is.LessThanOrEqualTo(top - GameScreen.NoticeBubbleGapDp + 0.05f));
        }

        // Las fichas de la repisa a la vista (la «?×n» de las cuias con ocultas y la de la pareja del Remolino), en el lienzo.
        private static List<Rect> VisibleChips(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = Root(view);
            var chips = new List<Rect>();
            foreach (CuiaView cuia in view.Views)
            {
                if (cuia.Murky.BadgeVisible)
                {
                    chips.Add(VictorySetup.CanvasRect(cuia.Murky.Badge.rectTransform, root));
                }

                if (cuia.OverlayOf(MechanicType.Remolino) is RemolinoOverlay whirl && whirl.PairMarkVisible)
                {
                    chips.Add(VictorySetup.CanvasRect((RectTransform)cuia.transform.Find("SlotFront/RemolinoPairTag")!, root));
                }
            }

            return chips;
        }

        // El aviso no deja a medias ninguna ficha de la repisa: o no la toca (con NoticeChipClearDp de aire a lo ancho) o la tapa
        // entera, con su borde de arriba sobre ella (pregunta 47, (1)). Devuelve las que tapa. También lo usa MinTextSizeTests
        // (el 135 al 100 % y al 130 %, A1).
        internal static int AssertChipsWholeOrClear(GameScreen view, string what)
        {
            Canvas.ForceUpdateCanvases();
            Rect toast = VictorySetup.CanvasRect(view.NoticeToast.Root, Root(view));
            toast.yMax += Toast.EdgeDp;
            int covered = 0;
            foreach (Rect chip in VisibleChips(view))
            {
                if (!toast.Overlaps(chip))
                {
                    if (chip.yMax > toast.yMin && chip.yMin < toast.yMax)
                    {
                        float gap = chip.xMin >= toast.xMax ? chip.xMin - toast.xMax : toast.xMin - chip.xMax;
                        Assert.That(gap, Is.GreaterThanOrEqualTo(GameScreen.NoticeChipClearDp - 0.6f), what + ": aire entre el aviso " + toast + " y la ficha " + chip);
                    }

                    continue;
                }

                covered++;
                Assert.That(chip.xMin >= toast.xMin && chip.xMax <= toast.xMax && chip.yMax <= toast.yMax, Is.True, what + ": el aviso " + toast + " tapa entera la ficha " + chip);
                Assert.That(chip.yMin, Is.GreaterThanOrEqualTo(toast.yMin + 0.5f), what + ": sin una tira suelta de la ficha " + chip + " sobre el aviso " + toast);
            }

            return covered;
        }

        // El texto del aviso cabe entero, a su tamaño de siempre (15 sp), sin desbordes.
        private static void AssertToastTextFits(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            TextMeshProUGUI t = view.NoticeToast.Label;
            t.ForceMeshUpdate(true, true);
            Rect r = t.rectTransform.rect;
            string what = "«" + t.text + "»";
            Assert.That(t.fontSize, Is.EqualTo(15f).Within(0.01f), what + " a 15 sp: no se encoge");
            Assert.That(t.textInfo.characterCount, Is.EqualTo(t.GetParsedText().Length), what + " entero");
            Assert.That(t.preferredHeight, Is.LessThanOrEqualTo(r.height + 0.5f), what + " cabe en alto");
            Bounds b = t.textBounds;
            Assert.That(b.min.y, Is.GreaterThanOrEqualTo(r.yMin - 0.5f), what + " por abajo");
            Assert.That(b.max.y, Is.LessThanOrEqualTo(r.yMax + 0.5f), what + " por arriba");
        }

        // El banner a la escala scale: dentro de la pantalla y de su marco, la cabecera sobre la viñeta y la viñeta sobre el lugar,
        // Capi y Rosa en la viñeta (no tocan los textos), y los textos a su tamaño por la escala, enteros y sin desbordes (las
        // comprobaciones de la tarjeta previa: CardSetup.AssertNoOverflow, AssertInsidePanel y AssertPanelOnScreen).
        private static void AssertBannerFits(GameScreen view, float scale, string where)
        {
            Canvas.ForceUpdateCanvases();
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(banner.FontScale, Is.EqualTo(scale).Within(1e-4f), where);
            RectTransform root = Root(view);
            Rect panel = VictorySetup.CanvasRect(banner.Panel, root);
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(LevelCardView.SideMarginDp - 0.5f), where);
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(view.CanvasWidth - LevelCardView.SideMarginDp + 0.5f), where);
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp), where + ": bajo la zona segura de arriba");
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom), where + ": sobre la zona segura de abajo");
            Rect title = VictorySetup.CanvasRect(banner.TitleLabel.rectTransform, root);
            Rect scene = VictorySetup.CanvasRect(banner.Scene, root);
            Rect place = VictorySetup.CanvasRect(banner.PlaceLabel.rectTransform, root);
            foreach ((string name, Rect r) in new[] { ("cabecera", title), ("viñeta", scene), ("lugar", place) })
            {
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(panel.xMin + 7.5f), where + ": " + name + " dentro del marco");
                Assert.That(r.xMax, Is.LessThanOrEqualTo(panel.xMax - 7.5f), where + ": " + name + " dentro del marco");
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(panel.yMin + 7.5f), where + ": " + name + " dentro del marco");
                Assert.That(r.yMax, Is.LessThanOrEqualTo(panel.yMax - 7.5f), where + ": " + name + " dentro del marco");
            }

            Assert.That(title.yMax, Is.LessThanOrEqualTo(scene.yMin + 0.5f), where + ": la cabecera, encima de la viñeta");
            Assert.That(scene.yMax, Is.LessThanOrEqualTo(place.yMin + 0.5f), where + ": la viñeta, encima del lugar");
            Rect capi = VictorySetup.CanvasRect(banner.CapiRect, root);
            Assert.That(capi.xMin >= scene.xMin - 0.5f && capi.xMax <= scene.xMax + 0.5f && capi.yMin >= scene.yMin - 0.5f && capi.yMax <= scene.yMax + 0.5f, Is.True, where + ": Capi, en la viñeta");
            Rect rosa = VictorySetup.CanvasRect(banner.RosaClip, root);
            Assert.That(rosa.xMin >= scene.xMin - 0.5f && rosa.xMax <= scene.xMax + 0.5f && rosa.yMin >= scene.yMin - 0.5f && rosa.yMax <= scene.yMax + 0.5f, Is.True, where + ": Rosa, en la viñeta");
            Assert.That(capi.Overlaps(title) || capi.Overlaps(place) || rosa.Overlaps(title) || rosa.Overlaps(place), Is.False, where + ": Capi y Rosa no tocan los textos");

            float titleSp = WorldBannerView.TitleSp * scale;
            Assert.That(banner.TitleLabel.fontSize, Is.EqualTo(titleSp).Within(0.01f), where + ": la cabecera crece, no se encoge");
            Assert.That(banner.PlaceLabel.fontSize, Is.InRange((WorldBannerView.PlaceMinSp * scale) - 0.01f, (WorldBannerView.PlaceSp * scale) + 0.01f), where + ": el lugar, de 18 a 15 sp por la escala");
            foreach (TextMeshProUGUI t in new[] { banner.TitleLabel, banner.PlaceLabel })
            {
                t.ForceMeshUpdate(true, true);
                Rect r = t.rectTransform.rect;
                string what = where + ": «" + t.text + "»";
                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(12f), what + " ≥ 12 sp");
                Assert.That(t.overflowMode, Is.EqualTo(TextOverflowModes.Overflow), what + " nunca se trunca");
                Assert.That(t.textInfo.characterCount, Is.EqualTo(t.GetParsedText().Length), what + " entero");
                Assert.That(t.preferredHeight, Is.LessThanOrEqualTo(r.height + 0.5f), what + " cabe en alto");
                Bounds b = t.textBounds;
                Assert.That(b.min.x, Is.GreaterThanOrEqualTo(r.xMin - 0.5f), what + " por la izquierda");
                Assert.That(b.max.x, Is.LessThanOrEqualTo(r.xMax + 0.5f), what + " por la derecha");
                Assert.That(b.min.y, Is.GreaterThanOrEqualTo(r.yMin - 0.5f), what + " por abajo");
                Assert.That(b.max.y, Is.LessThanOrEqualTo(r.yMax + 0.5f), what + " por arriba");
            }

            Assert.That(banner.Text, Is.EqualTo(UiText.WorldBanner(banner.World)), where + ": la clave entera");
        }

        // Banco de 4 cuias: la 0 y la 2, pareja de Remolino; verter la 1 en la 0 entra por el fondo de la 2.
        private static BoardState PairBoard()
        {
            int[][] cuias = { new[] { 1, 1 }, new[] { 3, 3, 3 }, new[] { 2 }, new int[0] };
            MechanicSpec[] attrs =
            {
                new MechanicSpec(MechanicType.Remolino, partner: 2), MechanicSpec.None, new MechanicSpec(MechanicType.Remolino, partner: 0),
                MechanicSpec.None,
            };
            BoardBuildResult result = BoardBuilder.Build(new[] { 4, 4, 4, 4 }, cuias, attrs);
            Assert.That(result.Error, Is.Null);
            return result.State!;
        }

        // El remanso, aclarado hacia el agua del canal (revisión de T52, ronda 2): ≥ 40 niveles RGB más claro en cada canal que
        // en reposo (el criterio de la captura 94 en el centro del remanso).
        private static void AssertPoolLit(RemolinoOverlay whirl, string what)
        {
            Color now = whirl.PoolColorNow;
            Color rest = ProceduralArt.WhirlPoolColor;
            Assert.That(now.a, Is.EqualTo(1f), what + ": el remanso, opaco");
            Assert.That(255f * Mathf.Min(now.r - rest.r, Mathf.Min(now.g - rest.g, now.b - rest.b)), Is.GreaterThanOrEqualTo(40f), what + ": el remanso se aclara ≥ 40 niveles RGB (" + now + ")");
        }

        // La pareja que recibe no lleva nada del Remolino en su cuerpo (revisión de T52, ronda 4): ni delante del líquido
        // (BodyFront: brillos blancos con alfa sobre el líquido, guía v2 §2.5, regla 2; §4.1 y §7) ni detrás (BodyBack: la zona
        // vacía de la cavidad no se tiñe; D-003 y VA-06). Su señal es su remolino, en la ranura: la espiral que se ilumina y
        // late, el remanso aclarado, las burbujas y los destellos del fondo (FxLayer).
        private static void AssertNothingOnTheBody(CuiaView view, string what)
        {
            CuiaOverlayHost host = view.OverlayHost;
            foreach (UnityEngine.UI.Graphic g in host.BodyFront.GetComponentsInChildren<UnityEngine.UI.Graphic>(false))
            {
                Assert.That(g.color.a, Is.EqualTo(0f), what + ": nada sobre el líquido de la pareja (" + g.name + ")");
            }

            foreach (UnityEngine.UI.Graphic g in host.BodyBack.GetComponentsInChildren<UnityEngine.UI.Graphic>(false))
            {
                Assert.That(g.name.StartsWith("Remolino"), Is.False, what + ": nada del Remolino tiñe su cavidad (" + g.name + ")");
            }
        }

        private static RemolinoOverlay Whirl(CuiaView view)
        {
            CuiaOverlay? overlay = view.OverlayOf(MechanicType.Remolino);
            Assert.That(overlay, Is.InstanceOf<RemolinoOverlay>(), "la cuia " + view.name + " tiene Remolino");
            return (RemolinoOverlay)overlay!;
        }

        // Efectos de prueba: cuenta todo lo que se pide, también con Reducir movimiento (FxLayer lo descartaría, pero aquí se
        // comprueba que quien anima ni siquiera lo pide).
        private sealed class CountingFx : IBoardFx
        {
            public int Sparkles;
            public int Bubbles;
            public int Splashes;
            public Vector2 FirstSparkleAt;

            public bool ReduceMotion { get; set; }

            public void Splash(Vector2 surface, Color tint, float unitDp)
            {
                Splashes++;
            }

            public void SealDrops(Vector2 rim, Color tint, float unitDp)
            {
            }

            void IBoardFx.Sparkles(Vector2 pos, Color color, int count, float unitDp)
            {
                if (Sparkles == 0)
                {
                    FirstSparkleAt = pos;
                }

                Sparkles += count;
            }

            public void Shards(Vector2 pos, Color color, int count, float unitDp)
            {
            }

            void IBoardFx.Bubbles(Vector2 pos, int count, float unitDp)
            {
                Bubbles += count;
            }
        }
    }
}
