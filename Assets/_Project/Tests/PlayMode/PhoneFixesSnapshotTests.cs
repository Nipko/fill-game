#nullable enable
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using AF.Ads;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de la tarea T52 (arreglos de la prueba de la tanda 2 en el teléfono; <c>[Explicit]</c>, no forman
    /// parte de la suite), en el rango 90-99:
    /// <list type="bullet">
    /// <item><description><c>90_banner_l41_130</c>, <c>90b_banner_l41_100</c>, <c>90c_banner_l41_130_16x9</c> y
    /// <c>90d_banner_l141_130</c>: el banner de mundo con la escala de fuente de 02 §12 (pregunta 43);</description></item>
    /// <item><description>observación 1, solo con estados que da el juego (revisión de T52, ronda 3):
    /// <c>92d_l135_revelar_sin_semillas</c> (el 135, 11 cuias, sin semillas ni video: desde la pregunta 48 (b), D-022, tarea A1,
    /// Revelar no saca aviso y responde su burbuja «Te faltan 120», en lo alto de su latido y con los colores del precio sin
    /// saldo; antes de A1 sacaba «Te faltan 120 semillas» por la cola junto a la ficha «?×1»);
    /// <c>92_l135_aviso_burbujas_despues</c> (el rescate comprado de verdad con las 300
    /// semillas justas: 12 cuias, las dos burbujas de dos líneas y «¡Una cuia más! Sigue desde aquí» ajustado a su texto a la
    /// derecha de la ficha «?×1», sin taparla: solo tapa la cara de la repisa de abajo en su tramo, los 3 s que dura; el
    /// compromiso [P]); <c>92f_l115_aviso_tapa_ficha</c> (el 115, igual: su ficha «?×2» queda bajo la tercera cuia de la fila de
    /// abajo, el aviso no cabe a ningún lado y la tapa entera: la otra rama del compromiso [P], que también se da en el juego);
    /// <c>92g_l103_aviso_canal</c> (el 103 tras el rescate de verdad: la cuia 8, de la pareja del Remolino, está en la fila de
    /// abajo, y el aviso ajustado a su texto junto a su ficha tapa en su tramo la bajada y el tramo horizontal del canal, que
    /// no cuentan como fichas, y el canal se ve cortado; el remanso no: parte del compromiso [P]; revisión de T52, ronda 4);
    /// <c>92b_l135_aviso_sin_burbujas</c> (el rescate de verdad con 1.000 semillas: sin burbujas, el aviso encogido a su texto
    /// sobre la bandeja, sin tapar la repisa ni la ficha); <c>93_1fila_aviso_burbuja_despues</c> (el tablero 6 del tutorial como
    /// nivel 46) y <c>93b_l31_aviso_burbuja_despues</c> (2 filas), las dos con el rescate de verdad y «Te faltan 150» de
    /// «+1 cuia». Las de antes del cambio, con el sufijo <c>_antes</c>, se sacaron con el código de <c>f64c19a</c>, con avisos
    /// puestos a mano: sirven para comparar dónde va el aviso, no qué dice;</description></item>
    /// <item><description><c>93z_diag_lienzo_tarde</c> y <c>93z_diag_lienzo_tarde_regenerado</c>: diagnóstico del texto
    /// desenfocado tras el rescate de las capturas 92, 93 y 93b de la ronda 2 (ver <see cref="SaveNow"/>);</description></item>
    /// <item><description><c>94_remolino_l103_chorro</c> y <c>95_remolino_l140_chorro</c> (la cuia tocada se traga el chorro),
    /// <c>94b_remolino_l103_recibe</c> y <c>95b_remolino_l140_recibe</c> (la pareja recibe por el fondo: su espiral, iluminada,
    /// late sobre el remanso aclarado, con burbujas y destellos en el fondo de la cavidad; sobre su líquido y su cavidad no se
    /// pinta nada; revisión de T52, ronda 4) y <c>96_remolino_blanca_l{n}_chorro</c> (el remolino de la
    /// superficie, con su contorno, sobre una dosis blanca de Guanábana; observación 5 y revisión de T52).</description></item>
    /// </list>
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.PhoneFixesSnapshotTests</c>
    /// sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class PhoneFixesSnapshotTests
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
        public IEnumerator CaptureToastAndBubbles()
        {
            string dir = SnapshotKit.Prepare();

            // El 135 (3 filas, 11 cuias) sin semillas ni video: «Te faltan 150» de «+1 cuia» y «Te faltan 120» de Revelar, en dos
            // líneas. Revelar (pregunta 48 (b)): sin aviso; su burbuja late y toma los colores del precio sin saldo (en lo alto
            // del latido).
            yield return LoadWithBubbles(135, 0, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressReveal(), Is.False);
            Assert.That(boot.View!.ToastText, Is.Null);
            Assert.That(boot.View.Tray.RevealMissingPulsing, Is.True);
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(BoosterTray.MissingPulseBeatMs));
            yield return SaveNow(dir, "92d_l135_revelar_sin_semillas", boot);

            // El rescate de verdad en el 135 con las 300 semillas justas (el primero del día): 12 cuias, sin semillas, las dos
            // burbujas y «¡Una cuia más!» a la derecha de la ficha «?×1».
            yield return LoadWithBubbles(135, 0, 300);
            yield return Rescue(dir, "92_l135_aviso_burbujas_despues", null, 12);

            // El 115 (11 cuias; la 10, con 2 ocultas), igual: tras el rescate, la ficha «?×2» de la cuia 10 queda bajo la tercera
            // cuia de la fila de abajo y el aviso la tapa entera.
            yield return LoadWithBubbles(115, 0, 300);
            yield return Rescue(dir, "92f_l115_aviso_tapa_ficha", s => s[10].HasHidden, 12);

            // El 135 con 1.000 semillas: el rescate de verdad deja 700, sin burbujas; el aviso, encogido a su texto sobre la bandeja.
            yield return LoadWithBubbles(135, 0, 1000);
            yield return Rescue(dir, "92b_l135_aviso_sin_burbujas", null, 12);

            // 1 fila: el tablero del 6 (5 cuias) como nivel 46, con las 300 semillas justas: el rescate de verdad (3 → 4) y, sin
            // semillas, «Te faltan 150» de «+1 cuia» con el aviso del rescate.
            yield return LoadWithBubbles(46, 40, 300);
            yield return Rescue(dir, "93_1fila_aviso_burbuja_despues", null, 6);

            // 2 filas: el 31 (8 cuias), igual.
            yield return LoadWithBubbles(31, 0, 300);
            yield return Rescue(dir, "93b_l31_aviso_burbuja_despues", null, 9);

            // Diagnóstico del texto desenfocado de 92, 93 y 93b en la ronda 2: el mismo camino que 93, con el lienzo pasado a la
            // cámara de las capturas solo al final (como entonces) y sin regenerar los textos; después, regenerados.
            yield return LoadWithBubbles(46, 40, 300, false);
            boot = GameBootstrap.Current!;
            PhoneFixesTests.RescueForReal(boot, null, "93z");
            SnapshotKit.Attach(boot);
            yield return null;
            float stored = SdfScale(boot.View!.NoticeToast.Label);
            float bubbleStored = SdfScale(boot.View.Tray.ExtraMissingLabel);
            SnapshotKit.Save(dir, "93z_diag_lienzo_tarde");
            RegenerateTexts(boot);
            float fresh = SdfScale(boot.View.NoticeToast.Label);
            float bubbleFresh = SdfScale(boot.View.Tray.ExtraMissingLabel);
            SnapshotKit.Save(dir, "93z_diag_lienzo_tarde_regenerado");
            Debug.Log("AF_T52 sdf 93z: aviso " + F(stored) + " → " + F(fresh) + " (" + F(fresh / stored) + "×), burbuja " + F(bubbleStored) + " → "
                + F(bubbleFresh) + " (" + F(bubbleFresh / bubbleStored) + "×); escala del lienzo " + F(boot.View.Canvas.scaleFactor));
        }

        [UnityTest]
        public IEnumerator CaptureToastOverRemolinoChannel()
        {
            // Revisión de T52, ronda 4 (compromiso [P]): el 103 (3 filas; la cuia 8, de la pareja del Remolino, en la fila de abajo)
            // con las 300 semillas justas. El rescate de verdad deja 12 cuias y, sin semillas, «Te faltan 150» de «+1 cuia»;
            // «¡Una cuia más!» llega a la repisa de abajo y va ajustado a su texto junto a la ficha de la pareja, sin taparla, pero
            // en su tramo tapa lo que de ese Remolino queda bajo el pie: la bajada y el tramo horizontal del canal (el log dice si
            // también el remanso).
            string dir = SnapshotKit.Prepare();
            yield return LoadWithBubbles(103, 0, 300);
            yield return Rescue(dir, "92g_l103_aviso_canal", null, 12);
            LogRemolinoUnderToast("92g_l103_aviso_canal", GameBootstrap.Current!);
        }

        [UnityTest]
        public IEnumerator CaptureBannerFontScale()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureBanner(dir, 41, 1f, "90b_banner_l41_100");
            yield return CaptureBanner(dir, 41, 1.3f, "90_banner_l41_130");
            yield return CaptureBanner(dir, 141, 1.3f, "90d_banner_l141_130");
            GameScreen.SimulatedScreen = HudLabelTests.Screen640; // 16:9 (1080 × 1920 px): 360 × 640 dp desde DC3
            yield return CaptureBanner(dir, 41, 1.3f, "90c_banner_l41_130_16x9");
        }

        [UnityTest]
        public IEnumerator CaptureRemolinoPours()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureRemolino(dir, 103, "94");
            yield return CaptureRemolino(dir, 140, "95");

            // Revisión de T52: el remolino de la superficie sobre una dosis blanca (Guanábana), el caso con menos contraste. El
            // primer nivel con Remolino cuya solución vierte por él en una cuia con la blanca arriba.
            bool found = false;
            foreach (int level in new[] { 104, 120, 103, 105, 107, 109, 112, 118, 126, 140, 146, 166, 191 })
            {
                yield return MechanicTestKit.LoadPlaying(level);
                Pour[]? path = MechanicTestKit.FindTrigger(GameBootstrap.Current!.Flow!, (ev, b, a) => ev.ThroughRemolino
                    && !b[ev.Target].IsEmpty && ColorMath.RelativeLuminance(Palette.Tint(b[ev.Target].TopColor)) > 0.85f);
                if (path == null)
                {
                    continue;
                }

                found = true;
                yield return CaptureRemolinoPath(dir, level, path, "96_remolino_blanca_l" + level + "_chorro", null);
                break;
            }

            Assert.That(found, Is.True, "algún vertido por el Remolino en una cuia con la blanca arriba");
        }

        [UnityTest]
        public IEnumerator DiagnoseBannerWaitAfterVictory()
        {
            // Diagnóstico de la observación 4 (AF_T52 en el log): lo que cuesta pintar la superficie de madera de 328 × 281 dp en
            // el hilo principal y el wait_ms del banner del 41 al entrar con «Siguiente» tras 1,5 s de victoria del 40. Con el
            // código de f64c19a: 221,9 ms y wait_ms 234 (la superficie, pintada al abrirse el banner); con T52, ≈ 20 ms (veta
            // más rápida, los mismos píxeles) y wait_ms ≈ 0 (la pinta el trabajo del arte, en otro hilo).
            var art = new ProceduralArt("white");
            var watch = Stopwatch.StartNew();
            art.ModalSurfaceFor(328f, 281f);
            watch.Stop();
            Debug.Log("AF_T52 modal_surface_328x281_ms " + watch.Elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture));
            art.Dispose();

            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtTimeoutMsOverride = 60000f;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.View!.CardArt.ReleaseBannerArt(); // el arte es compartido entre escenas: nada del banner de otra captura
            if (boot.Flow!.Phase == FlowPhase.LevelCard)
            {
                boot.TapAnywhere();
            }

            VictorySetup.WinToVictoryScreen(boot);
            Debug.Log("AF_T52 pending_at_victory " + boot.View!.CardArt.BannerArtPending);
            yield return SnapshotKit.Wait(1.5f);
            Debug.Log("AF_T52 pending_after_wait " + boot.View.CardArt.BannerArtPending);
            Assert.That(boot.PressNext(), Is.True);
            for (int ms = 0; ms < 5000 && boot.Flow.LevelNumber != 41; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            for (int ms = 0; ms < 2000 && boot.View.LastWorldBannerArtProbe == null; ms += PlayModeHelpers.FrameMs)
            {
                yield return null;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Debug.Log("AF_T52 banner_art_after_next " + boot.View.LastWorldBannerArtProbe);
            TestContext.WriteLine("AF_T52 banner_art_after_next " + boot.View.LastWorldBannerArtProbe);
        }

        // El banner del nivel level, con la escala de fuente scale, ya quieto (Capi presentando y Rosa fuera).
        private static IEnumerator CaptureBanner(string dir, int level, float scale, string name)
        {
            yield return CardSetup.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.WorldBannerView.Visible, Is.True, "nivel " + level);
            view.BannerFontScale = scale;
            PlayModeHelpers.TickExact(boot, 1100);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            RegenerateTexts(boot);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_T52 captura " + name + ": título «" + view.WorldBannerView.TitleText + "», lugar «" + view.WorldBannerView.PlaceText + "», "
                + view.WorldBannerView.Panel.rect.height.ToString("0", CultureInfo.InvariantCulture) + " dp de alto");
        }

        private static IEnumerator CaptureRemolino(string dir, int level, string prefix)
        {
            yield return MechanicTestKit.LoadPlaying(level);
            Pour[]? path = MechanicTestKit.FindTrigger(GameBootstrap.Current!.Flow!, (ev, b, a) => ev.ThroughRemolino);
            Assert.That(path, Is.Not.Null, "vertido por el Remolino en el " + level);
            yield return CaptureRemolinoPath(dir, level, path!, prefix + "_remolino_l" + level + "_chorro", prefix + "b_remolino_l" + level + "_recibe");
        }

        // Juega el camino path del nivel ya cargado y captura el último vertido (por el Remolino) a mitad del chorro (stream) y,
        // si receive no es null, a mitad de la recepción de la pareja.
        private static IEnumerator CaptureRemolinoPath(string dir, int level, Pour[] path, string stream, string? receive)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            MechanicTestKit.PlayPrefix(boot, path);
            GameScreen view = boot.View!;
            PourTimings timings = view.Timings;
            TapResult poured = MechanicTestKit.Tap(boot, path[path.Length - 1]);
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured), "nivel " + level);
            int partner = poured.Pour.Receiver;
            float mid = (timings.TravelMs * BoardAnimator.StreamStartFraction) + BoardAnimator.StreamGrowMs + (0.5f * timings.DoseMs);
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(mid) + PlayModeHelpers.FrameMs);
            yield return SaveNow(dir, stream, boot);
            if (receive != null)
            {
                MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Receive, partner, 4000);
                MechanicTestKit.StepToFraction(boot, MechanicStepKind.Receive, partner, 0.55f);
                RemolinoOverlay? whirl = view.Views[partner].OverlayOf(MechanicType.Remolino) as RemolinoOverlay;
                Debug.Log("AF_T52 captura " + receive + ": espuma de la espiral " + F(whirl != null ? whirl.SpiralFoamAlpha : -1f) + ", latido "
                    + F(whirl != null ? whirl.SpiralScale : -1f) + ", remanso aclarado " + F(whirl != null ? whirl.PoolLightNow : -1f));
                yield return SaveNow(dir, receive, boot);
            }

            MechanicTestKit.StepToIdle(boot, 4000);
        }

        // Nivel level (con el desplazamiento offset de LaunchOptions.LevelOffsetOverride, si no es 0) con seeds semillas, sin
        // video ni Luciérnagas y en juego con la vista quieta. Con attach, el lienzo pasa a la cámara de las capturas en cuanto
        // carga, como en el teléfono, donde no cambia de modo (ver SaveNow).
        private static IEnumerator LoadWithBubbles(int level, int offset, int seeds, bool attach = true)
        {
            LaunchOptions.Reset();
            FunnelSetup.Hooks();
            if (offset != 0)
            {
                LaunchOptions.LevelOffsetOverride = offset;
            }

            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            if (attach)
            {
                SnapshotKit.Attach(boot);
            }

            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
        }

        // El nivel ya cargado: el rescate de verdad (PhoneFixesTests.RescueForReal; accept, si no es null, fija el tablero
        // bloqueado) y la captura del aviso «¡Una cuia más!» con las cuias que quedan (cuias) y las burbujas de la bandeja.
        private static IEnumerator Rescue(string dir, string name, System.Func<BoardState, bool>? accept, int cuias)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            int pours = PhoneFixesTests.RescueForReal(boot, accept, name);
            GameScreen view = boot.View!;
            Assert.That(view.Views.Length, Is.EqualTo(cuias), name);
            Debug.Log("AF_T52 " + name + ": " + pours + " vertidos hasta Aguas estancadas; " + view.Views.Length + " cuias; burbujas "
                + view.Tray.ExtraShowsMissing + "/" + view.Tray.RevealShowsMissing);
            LogToast(name, boot);
            yield return SaveNow(dir, name, boot);
        }

        // Para cada cuia del Remolino de la fila de abajo: qué tapa el aviso de lo que de su Remolino queda bajo el pie (dp del
        // lienzo; el remanso, de 2,6 × 0,85 u centrado 0,10 u bajo el pie; la bajada, en su eje, y el tramo horizontal del canal,
        // de pie + 17 a pie + 21,5 hacia ChannelSide).
        private static void LogRemolinoUnderToast(string name, GameBootstrap boot)
        {
            GameScreen view = boot.View!;
            BoardLayoutResult l = view.Layout!;
            Rect toast = view.NoticeToast.SeenRect;
            float rowFoot = l.RowFootY[l.Rows - 1];
            for (int i = 0; i < view.Views.Length && i < l.CenterX.Length; i++)
            {
                if (!(view.Views[i].OverlayOf(MechanicType.Remolino) is RemolinoOverlay whirl) || !whirl.PairMarkVisible || Mathf.Abs(l.FootY[i] - rowFoot) > 0.5f)
                {
                    continue;
                }

                float u = view.Views[i].Unit;
                float x = l.CenterX[i];
                float foot = l.FootY[i];
                var pool = Rect.MinMaxRect(x - (0.5f * RemolinoOverlay.PoolWidthU * u), foot, x + (0.5f * RemolinoOverlay.PoolWidthU * u),
                    foot + ((0.5f * RemolinoOverlay.PoolHeightU) - RemolinoOverlay.PoolCenterU) * u);
                var drop = Rect.MinMaxRect(x - (0.5f * RemolinoOverlay.ChannelWidthDp), foot, x + (0.5f * RemolinoOverlay.ChannelWidthDp),
                    foot + RemolinoOverlay.ShelfBottomBelowFootDp + RemolinoOverlay.ChannelWidthDp);
                Debug.Log("AF_T52 " + name + ": cuia " + i + " (pareja " + whirl.PartnerIndex + ", tramo hacia " + F(whirl.ChannelSide) + ") en x = " + F(x)
                    + " dp, pie " + F(foot) + " dp; aviso " + toast + "; tapa el remanso: " + toast.Overlaps(pool) + ", la bajada: " + toast.Overlaps(drop)
                    + ", ficha " + whirl.PairMarkRect);
            }
        }

        private static void LogToast(string name, GameBootstrap boot)
        {
            GameScreen view = boot.View!;
            BoardLayoutResult l = view.Layout!;
            Debug.Log("AF_T52 " + name + ": aviso «" + view.ToastText + "» " + view.NoticeToast.SeenRect + " (ajustado " + F(view.NoticeToast.FitWidthDp(view.CanvasWidth))
                + " dp, de siempre " + F(Toast.FullWidthDp(view.CanvasWidth)) + "); pie de la fila de abajo " + F(l.RowFootY[l.Rows - 1]) + " dp");
        }

        // Captura con el lienzo en la cámara de las capturas y todos los textos regenerados en ese modo. El lienzo de las capturas
        // pasa de superpuesto a cámara (SnapshotKit.Attach); TextMeshPro guarda la escala SDF de cada texto en su malla al
        // generarla y, al cambiar de modo, solo la corrige por la escala del objeto, no por la del lienzo: un texto generado antes
        // del cambio (el aviso y las burbujas del rescate, generados por el Canvas.ForceUpdateCanvases de DevProbe.Layout al
        // recolocar la cuia añadida) queda con la escala SDF dividida por la del lienzo (×1/3) y sale fino y desenfocado
        // (93z_diag_lienzo_tarde). En el teléfono el lienzo no cambia de modo: regenerarlos da lo que se ve allí.
        private static IEnumerator SaveNow(string dir, string name, GameBootstrap boot)
        {
            SnapshotKit.Attach(boot);
            yield return null;
            RegenerateTexts(boot);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_T52 captura " + name);
        }

        // Regenera la malla de todos los textos activos del lienzo de la pantalla de nivel en su modo de ahora.
        private static void RegenerateTexts(GameBootstrap boot)
        {
            foreach (TMP_Text text in boot.View!.Canvas.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate(false, true);
            }
        }

        // Escala SDF guardada en la malla del texto (la del primer vértice; 0 sin malla).
        private static float SdfScale(TMP_Text text)
        {
            TMP_TextInfo info = text.textInfo;
            if (info == null || info.meshInfo == null || info.meshInfo.Length == 0 || info.meshInfo[0].uvs0 == null || info.meshInfo[0].uvs0.Length == 0)
            {
                return 0f;
            }

            return Mathf.Abs(info.meshInfo[0].uvs0[0].w);
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
