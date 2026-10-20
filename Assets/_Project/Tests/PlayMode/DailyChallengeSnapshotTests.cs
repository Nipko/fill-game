#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.Text;
using AF.Core.Rules;
using AF.GameFlow;
using AF.LiveOps;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de las pantallas del Reto del río (tarea R11; 06 §4.1 «Pantalla»; nota <c>docs/dev/reto-del-rio.md</c>
    /// §8; <c>[Explicit]</c>, no forman parte de la suite), con la pantalla simulada de <see cref="SnapshotKit"/> a 360 × 780 dp
    /// (1080 × 2340 px) y en 16:9 (360 × 640 dp, 1080 × 1920 px, sufijo <c>_16x9</c>), en es-419 (<c>Logs/snapshots/es-419/</c>)
    /// y en en-XA (pseudolocalización, <c>Logs/snapshots/en-XA/</c>):
    /// <list type="bullet">
    /// <item><c>r11_01_card_play</c>: el inicio con la tarjeta «Jugar» (banda 1, racha 0/7);</item>
    /// <item><c>r11_02_header</c>: la partida del Reto, con la cabecera «Reto del río» (sin «Nivel N» ni barra de Crecida);</item>
    /// <item><c>r11_03_card_continue</c>: la tarjeta «Seguir» tras salir sin ganar (R-Q16 a);</item>
    /// <item><c>r11_04_calendar_falling</c>: el calendario del mes con sellos y el del día cayendo (a media caída);</item>
    /// <item><c>r11_05_calendar_reward</c>: el sello posado y el «+60»;</item>
    /// <item><c>r11_06_card_sealed</c>: la tarjeta «Sellado» con el sello y la racha 1/7;</item>
    /// <item><c>r11_07_calendar_month</c>: el calendario abierto desde la tarjeta sellada (sin premio);</item>
    /// <item><c>r11_08_calendar_pending</c> y <c>r11_09_card_pending</c>: el premio pendiente de validar (reloj adelantado 48 h
    /// con red; 08 §10.1);</item>
    /// <item><c>r11_10_calendar_pororoca_locked</c>: la racha de 7 antes del nivel 30, «+1 Pororoca · se desbloquea en el nivel
    /// 30» (R-Q14 a).</item>
    /// </list>
    /// Textos: la tabla propia de <see cref="DailyChallengeTests"/> (las tablas del proyecto más
    /// <c>docs/dev/traspaso/spark-strings-reto.csv</c>, como hará E5), así que es-419 muestra el texto real. Cada captura deja en el
    /// log una línea <c>AF_FIT</c> por texto visible que no cabe en su caja. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode
    /// PlayMode --architecture arm64 --filter AF.Tests.PlayMode.DailyChallengeSnapshotTests</c> sin <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class DailyChallengeSnapshotTests
    {
        private const int MaxLevel = 25;
        private const int Frame = PlayModeHelpers.FrameMs;
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Monday = new DateTime(2026, 10, 19, 10, 0, 0); // hora local de Bogotá

        private ManualClock _clock = null!;
        private string _dir = string.Empty;
        private string _suffix = string.Empty;
        private int _heightPx = SnapshotKit.Height;
        private int _overflows;

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            SnapshotKit.Release();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator CaptureDaily([Values("es-419", "en-XA")] string locale, [Values(780, 640)] int screen)
        {
            _dir = SnapshotKit.Prepare(locale);
            if (screen == 640)
            {
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            }

            _heightPx = screen == 640 ? 1920 : SnapshotKit.Height;
            _suffix = screen == 640 ? "_16x9" : string.Empty;
            _overflows = 0;
            _clock = new ManualClock(Utc(Monday), Bogota);
            LaunchOptions.ClockOverride = _clock;
            LaunchOptions.LocTablesOverride = TestTables();
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;

            // 1) Jugar, la cabecera, Seguir, el calendario con el sello que cae y el +60, Sellado y el calendario del mes.
            var record = new DailyChallengeRecord { LastClaimedDate = "2026-10-16", StreakDays = 2, StampsMonth = "2026-10" };
            int[] earlier = { 2, 3, 5, 9, 15, 16 };
            for (int i = 0; i < earlier.Length; i++)
            {
                record.StampDays.Add(earlier[i]);
            }

            var store = new InMemoryDailyChallengeStore(record);
            GameBootstrap.DailyStoreOverride = store;
            yield return Boot(MaxLevel);
            GameBootstrap boot = GameBootstrap.Current!;
            DailyChallengeCard card = boot.DailyCard!;
            Assert.That(card.State, Is.EqualTo(DailyCardState.Play));
            yield return Shot("r11_01_card_play", card.Root);

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            SnapshotKit.AttachOverlay(daily.View!.Canvas);
            daily.View.Canvas.planeDistance = 10f;
            ToPlaying(boot, daily);
            Step(boot, 600);
            Assert.That(daily.View.LevelTitle.text, Is.EqualTo(DailyText.Title));
            yield return Shot("r11_02_header", daily.View.LevelTitle.transform.parent);

            PourFirstMove(boot, daily);
            Assert.That(daily.Actions.PressQuit(), Is.True, "pausa › Salir");
            Step(boot, Frame);
            Assert.That(card.State, Is.EqualTo(DailyCardState.InProgress));
            yield return Shot("r11_03_card_continue", card.Root);

            Assert.That(boot.PressDailyCard(), Is.True, "Seguir");
            daily = boot.DailyScreen!;
            SnapshotKit.AttachOverlay(daily.View!.Canvas);
            daily.View.Canvas.planeDistance = 10f;
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            DailyStampCalendar calendar = daily.Calendar;
            Step(boot, (int)(DailyStampCalendar.StampDropMs * 0.5f));
            Assert.That(calendar.StampLanded, Is.False, "a media caída");
            yield return Shot("r11_04_calendar_falling", calendar.Canvas.transform);
            calendar.Settle();
            Assert.That(calendar.RewardShown, Is.True);
            yield return Shot("r11_05_calendar_reward", calendar.Canvas.transform);

            calendar.HomeButton.onClick.Invoke();
            Step(boot, Frame);
            Assert.That(card.State, Is.EqualTo(DailyCardState.Sealed));
            yield return Shot("r11_06_card_sealed", card.Root);

            Assert.That(boot.PressDailyCard(), Is.True, "tarjeta sellada: el calendario del mes");
            Step(boot, Frame);
            yield return Shot("r11_07_calendar_month", calendar.Canvas.transform);

            // 2) Premio pendiente de validar: la hora del servidor va 48 h por delante del teléfono (08 §10.1).
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            GameBootstrap.DailyServerOverride = FakeServerDate.Online(TimeSpan.FromHours(48));
            yield return Boot(MaxLevel);
            boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            daily = boot.DailyScreen!;
            SnapshotKit.AttachOverlay(daily.View!.Canvas);
            daily.View.Canvas.planeDistance = 10f;
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Pending));
            yield return Shot("r11_08_calendar_pending", daily.Calendar.Canvas.transform);
            daily.Calendar.HomeButton.onClick.Invoke();
            Step(boot, Frame);
            Assert.That(boot.DailyCard!.State, Is.EqualTo(DailyCardState.PendingValidation));
            yield return Shot("r11_09_card_pending", boot.DailyCard.Root);
            GameBootstrap.DailyServerOverride = null;

            // 3) Racha de 7 antes del nivel 30: la Pororoca entra en el stock y espera (R-Q14 a).
            var streak = new DailyChallengeRecord { LastClaimedDate = "2026-10-18", StreakDays = 6, StampsMonth = "2026-10" };
            for (int d = 12; d <= 18; d++)
            {
                streak.StampDays.Add(d);
            }

            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore(streak);
            yield return Boot(MaxLevel);
            boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            daily = boot.DailyScreen!;
            SnapshotKit.AttachOverlay(daily.View!.Canvas);
            daily.View.Canvas.planeDistance = 10f;
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            Assert.That(daily.LastClaim!.Value.ShufflePaid, Is.EqualTo(1));
            yield return Shot("r11_10_calendar_pororoca_locked", daily.Calendar.Canvas.transform);

            Debug.Log("AF_FIT " + locale + _suffix + ": " + _overflows + " textos fuera de su caja");
        }

        // ---- Ayudas ----

        // Arranque con el perfil del test y el enganche E2 simulado (como DailyChallengeTests), con los lienzos en la cámara de las
        // capturas: el inicio (10), la tarjeta (5) y el calendario (5; nunca a la vez que la tarjeta).
        private IEnumerator Boot(int maxLevel)
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(maxLevel)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True, "E2");
            boot.DailyRunner!.enabled = false;
            Step(boot, Frame);
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(boot.DailyCard!.Root.GetComponentInParent<Canvas>().rootCanvas);
            SnapshotKit.AttachOverlay(boot.DailyScreen!.Calendar.Canvas);
        }

        // Captura y registra los textos visibles de la pieza que se revisa que no caben en su caja (AF_FIT).
        private IEnumerator Shot(string name, Transform focus)
        {
            yield return SnapshotKit.Wait(0.2f);
            Canvas.ForceUpdateCanvases();
            LogFit(name, focus);
            SnapshotKit.SaveTop(_dir, name + _suffix, _heightPx);
        }

        private void LogFit(string name, Transform focus)
        {
            var line = new StringBuilder();
            foreach (TextMeshProUGUI t in focus.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                if (!t.isActiveAndEnabled || t.text.Length == 0)
                {
                    continue;
                }

                t.ForceMeshUpdate();
                Rect r = t.rectTransform.rect;
                bool over = t.isTextOverflowing || t.textBounds.size.x > r.width + 0.5f || t.textBounds.size.y > r.height + 0.5f;
                line.Append(" | ").Append(t.name).Append(" «").Append(t.text).Append("» ")
                    .Append(t.fontSize.ToString("0.#", CultureInfo.InvariantCulture)).Append(" sp, ")
                    .Append(t.textBounds.size.x.ToString("0", CultureInfo.InvariantCulture)).Append('×')
                    .Append(t.textBounds.size.y.ToString("0", CultureInfo.InvariantCulture)).Append(" en ")
                    .Append(r.width.ToString("0", CultureInfo.InvariantCulture)).Append('×')
                    .Append(r.height.ToString("0", CultureInfo.InvariantCulture)).Append(" dp");
                if (over)
                {
                    line.Append(" NO CABE");
                    _overflows++;
                }
            }

            Debug.Log("AF_FIT " + SnapshotKit.Locale + " " + name + _suffix + ":" + line);
        }

        private static void Step(GameBootstrap boot, int ms)
        {
            for (int t = 0; t < ms; t += Frame)
            {
                boot.TickDaily(Frame);
            }
        }

        private static void StepUntil(GameBootstrap boot, Func<bool> done, int maxMs, string what)
        {
            for (int t = 0; t < maxMs; t += Frame)
            {
                if (done())
                {
                    return;
                }

                boot.TickDaily(Frame);
            }

            Assert.That(done(), Is.True, "tiempo agotado: " + what);
        }

        private static void ToPlaying(GameBootstrap boot, DailyChallengeScreen daily)
        {
            LevelFlow flow = daily.Flow!;
            daily.Actions.TapAnywhere(); // salta la intro
            StepUntil(boot, () => flow.Phase == FlowPhase.Playing && flow.AcceptsBoardInput, 10000, "fin de la intro del Reto");
        }

        private static void PourFirstMove(GameBootstrap boot, DailyChallengeScreen daily)
        {
            LevelFlow flow = daily.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved && solution.MoveCount >= 2, Is.True);
            Pour move = solution.GetMove(0);
            daily.Actions.TapCuia(move.Source);
            daily.Actions.TapCuia(move.Target);
            Step(boot, 1000);
        }

        private static void Win(GameBootstrap boot, DailyChallengeScreen daily)
        {
            ToPlaying(boot, daily);
            LevelFlow flow = daily.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                StepUntil(boot, () => flow.AcceptsBoardInput, 5000, "tablero listo para el vertido " + i);
                daily.Actions.TapCuia(move.Source);
                daily.Actions.TapCuia(move.Target);
            }

            StepUntil(boot, () => daily.Won, 10000, "victoria del Reto");
        }

        private static Profile ProfileAt(int maxLevel)
        {
            Profile profile = AF.Economy.Wallet.NewProfile(new ManualClock(Utc(Monday), Bogota));
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            return profile;
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }

        // Tabla propia: strings.csv y facts.csv del proyecto más las filas del Reto (sin su cabecera), como hará E5.
        private static LocTableTexts TestTables()
        {
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile);
            string facts = LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile);
            string daily = LocCsv.Decode(
                System.IO.File.ReadAllBytes(LocTables.ProjectPath("docs/dev/traspaso/spark-strings-reto.csv")), "spark-strings-reto.csv");
            string rows = daily.Substring(daily.IndexOf('\n') + 1);
            return new LocTableTexts(strings.EndsWith("\n", StringComparison.Ordinal) ? strings + rows : strings + "\n" + rows, facts);
        }
    }
}
