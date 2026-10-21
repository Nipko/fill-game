#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Localization;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La libreta y la tarjeta de la Bitácora del río (B4; 06 §4.2 «Pantalla»; 06 §2.3, pantalla 2; D-033, añadido del
    /// 2026-10-20, puntos 4-6 y 11), solas, sin la escena: 30 casillas en 5 × 6 con los hitos más grandes, la de hoy pulsando, las
    /// tres tarjetas en orden fijo con lo fijo aparte, un toque sella sin confirmación, el sello que cae, el pie, la variante del
    /// nivel 10 y el punto de la tarjeta. Tabla de textos propia: las del proyecto más
    /// <c>docs/dev/traspaso/spark-enganches/B4-strings.csv</c>.
    /// </summary>
    public sealed class LogbookScreenTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);

        private GameObject? _host;
        private LogbookScreen _screen = null!;
        private LoginCalendarService _service = null!;
        private ManualClock _clock = null!;
        private Wallet _wallet = null!;
        private readonly List<LoginCalendarChoice> _chosen = new List<LoginCalendarChoice>();
        private int _closed;

        [SetUp]
        public void SetUp()
        {
            Loc.Configure(LogbookTestTables.Catalog(), LocLocale.SourceLocale, true);
            _host = new GameObject("LogbookHost", typeof(RectTransform));
            _clock = new ManualClock(DateTime.SpecifyKind(new DateTime(2026, 10, 20, 10, 0, 0) - Bogota, DateTimeKind.Utc), Bogota);
            _wallet = new Wallet(Wallet.NewProfile(_clock), new MemoryProfileStore(), _clock);
            _service = new LoginCalendarService(new InMemoryLoginCalendarStore(), _wallet, _clock, 10);
            _chosen.Clear();
            _closed = 0;
            _screen = new LogbookScreen(_host.transform, new ProceduralArt("white"), c => _chosen.Add(c), () => _closed++);
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                UnityEngine.Object.Destroy(_host);
            }

            LocSetUpFixture.ConfigureEs419();
        }

        [Test]
        public void TheNotebook_Has30PagesIn5RowsOf6_WithBiggerMilestones()
        {
            _service.MarkPlayed();
            _screen.Show(Peek(), LogbookOpenSource.Card);
            Assert.That(_screen.Visible, Is.True);
            Assert.That(LogbookScreen.Rows * LogbookScreen.Columns, Is.EqualTo(30), "06 §4.2: 5 filas × 6");
            Assert.That(_screen.TitleText, Is.EqualTo("Bitácora del río"));
            Assert.That(_screen.FooterText, Is.EqualTo("Página 1 de 30 · Ciclo 1"));
            Assert.That(_screen.StatusText, Is.EqualTo("Toca la página de hoy"));
            Assert.That(_screen.ButtonText, Is.EqualTo("Cerrar"));
            Assert.That(_screen.IntroShown, Is.False);

            float plain = _screen.CellSize(1);
            Assert.That(plain, Is.GreaterThan(30f), "casillas tocables");
            foreach (int day in new[] { 7, 14, 21, 30 })
            {
                Assert.That(_screen.CellSize(day), Is.EqualTo(plain + (2f * LogbookScreen.MilestoneGrowDp)).Within(0.01f), "hito " + day + " más grande");
            }

            // Fila y columna: la 6 al final de la primera fila, la 7 al principio de la segunda.
            Vector2 p1 = _screen.CellRect(1).anchoredPosition;
            Vector2 p6 = _screen.CellRect(6).anchoredPosition;
            Vector2 p7 = _screen.CellRect(7).anchoredPosition;
            Vector2 p30 = _screen.CellRect(30).anchoredPosition;
            Assert.That(p6.y, Is.EqualTo(p1.y).Within(0.01f));
            Assert.That(p6.x, Is.GreaterThan(p1.x));
            Assert.That(p7.x, Is.EqualTo(p1.x).Within(0.01f));
            Assert.That(p7.y, Is.LessThan(p1.y));
            Assert.That(p30.x, Is.EqualTo(p6.x).Within(0.01f));
            Assert.That(_screen.StampedDays, Is.Empty);
        }

        [Test]
        public void TodayPulses_AndATapShowsTheThreeCardsInTheirFixedOrder_WithTheFixedExtrasApart()
        {
            for (int day = 1; day <= 6; day++)
            {
                _service.MarkPlayed();
                _service.Claim(LoginCalendarChoice.Seeds, 25, LoginCalendarUnlocks.All, 1);
                _clock.Advance(TimeSpan.FromDays(1));
            }

            _service.MarkPlayed();
            _screen.Show(Peek(), LogbookOpenSource.Card);
            Assert.That(_screen.TodayDay, Is.EqualTo(7));
            Assert.That(_screen.StampedDays, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }), "las pasadas, solo con el sello (punto 6)");
            Assert.That(_screen.TodayPulsing, Is.True);
            _screen.Tick(LogbookScreen.PulsePeriodMs / 4f);
            Assert.That(_screen.CellRect(7).localScale.x, Is.GreaterThan(1.01f), "la de hoy pulsa");

            Assert.That(_screen.PressChoice(LoginCalendarChoice.Seeds), Is.False, "sin las tarjetas a la vista no se elige");
            _screen.CellButton(6).onClick.Invoke();
            Assert.That(_screen.Choosing, Is.False, "una casilla pasada no hace nada");
            _screen.CellButton(7).onClick.Invoke();
            Assert.That(_screen.Choosing, Is.True);
            Assert.That(_screen.StatusText, Is.EqualTo("Elige tu premio"));
            Assert.That(_screen.CardTexts(LoginCalendarChoice.Seeds), Is.EqualTo(new[] { "+250" }));
            Assert.That(_screen.CardTexts(LoginCalendarChoice.Booster), Is.EqualTo(new[] { "+50", "Morpho ×2" }));
            Assert.That(_screen.CardTexts(LoginCalendarChoice.Mix), Is.EqualTo(new[] { "+100", "Marea ×5" }));
            Assert.That(_screen.ExtrasTexts, Is.EqualTo(new[] { "+ Canasta" }), "la Canasta va fija en las tres (punto 2)");
            Assert.That(_chosen, Is.Empty, "nunca se elige sola");

            _screen.CardButton(LoginCalendarChoice.Mix).onClick.Invoke();
            Assert.That(_chosen, Is.EqualTo(new[] { LoginCalendarChoice.Mix }), "un toque sella, sin confirmación (punto 4)");
        }

        [Test]
        public void AfterSealing_TheStampFallsOnThePage_AndWhatWasReceivedShowsBelow()
        {
            _service.MarkPlayed();
            _screen.Show(Peek(), LogbookOpenSource.Card);
            Assert.That(_screen.PressToday(), Is.True);
            LoginCalendarClaim claim = _service.Claim(LoginCalendarChoice.Mix, 25, LoginCalendarUnlocks.All, 1)!;
            _screen.ShowClaimed(claim, Peek());
            Assert.That(_screen.Choosing, Is.False);
            Assert.That(_screen.FallingDay, Is.EqualTo(1));
            Assert.That(_screen.StampLanded, Is.False);
            Assert.That(_screen.StampedDays, Is.EqualTo(new[] { 1 }));
            Assert.That(_screen.FooterText, Is.EqualTo("Página 1 de 30 · Ciclo 1"), "el pie, con la página recién sellada");
            Assert.That(_screen.StatusText, Is.EqualTo("Página sellada. La siguiente, mañana"));
            Assert.That(_screen.ReceivedTexts, Is.EqualTo(new[] { "+30", "Marea ×1" }));
            Assert.That(_screen.TodayPulsing, Is.False);
            _screen.Tick(LogbookScreen.StampDropMs + 16f);
            Assert.That(_screen.StampLanded, Is.True);
            Assert.That(_screen.PressToday(), Is.False, "una página por día");

            _screen.CloseButton.onClick.Invoke();
            Assert.That(_closed, Is.EqualTo(1));
        }

        [Test]
        public void TheDayThe30IsSealed_TheNotebookShowsTheWholeCycleThatClosed()
        {
            var store = new InMemoryLoginCalendarStore(new LoginCalendarRecord { NextDay = 30, Cycle = 1, LastClaimDate = "2026-10-19" });
            _service = new LoginCalendarService(store, _wallet, _clock, 10);
            _service.MarkPlayed();
            Assert.That(_service.Claim(LoginCalendarChoice.Seeds, 25, LoginCalendarUnlocks.All, 1)!.Day, Is.EqualTo(30));

            // Reabierta el mismo día (tarjeta «Sellada hoy»): el ciclo 1 entero, no una libreta vacía del ciclo 2.
            _screen.Show(Peek(), LogbookOpenSource.Card);
            Assert.That(_screen.StampedDays.Count, Is.EqualTo(30));
            Assert.That(_screen.FooterText, Is.EqualTo("Página 30 de 30 · Ciclo 1"));
            Assert.That(_screen.StatusText, Is.EqualTo("Página sellada. La siguiente, mañana"));

            // Al día siguiente, el ciclo 2 empieza en la página 1.
            _clock.Advance(TimeSpan.FromDays(1));
            _screen.Show(Peek(), LogbookOpenSource.Card);
            Assert.That(_screen.StampedDays, Is.Empty);
            Assert.That(_screen.FooterText, Is.EqualTo("Página 1 de 30 · Ciclo 2"));
        }

        [Test]
        public void ClosingWithoutChoosing_LeavesThePagePending()
        {
            _service.MarkPlayed();
            _screen.Show(Peek(), LogbookOpenSource.Takeover);
            _screen.PressToday();
            _screen.Hide();
            Assert.That(_screen.Visible, Is.False);
            Assert.That(_chosen, Is.Empty);
            Assert.That(_service.StateFor(25), Is.EqualTo(LoginCalendarState.Ready), "cerrar sin elegir la deja pendiente (punto 4)");
            _screen.Show(Peek(), LogbookOpenSource.Card);
            Assert.That(_screen.Choosing, Is.False, "vuelve a la libreta, con la de hoy pulsando");
            Assert.That(_screen.TodayPulsing, Is.True);
        }

        [Test]
        public void Waiting_SaysToFinishALevel_AndTheTodayPageDoesNotOpen()
        {
            _screen.Show(Peek(), LogbookOpenSource.Card);
            Assert.That(_screen.StatusText, Is.EqualTo("Termina un nivel hoy para sellar la página"));
            Assert.That(_screen.TodayPulsing, Is.False);
            Assert.That(_screen.PressToday(), Is.False);
        }

        [Test]
        public void TheLevel10Sequence_ShowsTheIntroAndASingleContinue()
        {
            _service.MarkPlayed();
            _screen.Show(Peek(), LogbookOpenSource.L10);
            Assert.That(_screen.IntroShown, Is.True);
            Assert.That(_screen.ButtonText, Is.EqualTo("Seguir"), "06 §2.3: un solo botón «Seguir»");
            Canvas.ForceUpdateCanvases();
            Assert.That(_screen.Canvas.sortingOrder, Is.EqualTo(LogbookScreen.SortingOrder));
        }

        [Test]
        public void TheCard_ShowsTheDotOnlyWithAPagePending()
        {
            var card = new LogbookCard(_host!.transform, new ProceduralArt("white"), () => { });
            card.Refresh(LoginCalendarState.Hidden);
            Assert.That(card.Visible, Is.False, "D-008: oculta mientras no existe");
            card.Refresh(LoginCalendarState.Waiting);
            Assert.That(card.Visible, Is.True);
            Assert.That(card.DotShown, Is.False);
            Assert.That(card.TitleText, Is.EqualTo("Bitácora del río"));
            Assert.That(card.StateText, Is.EqualTo("Juega un nivel para sellar"));
            card.Refresh(LoginCalendarState.Ready);
            Assert.That(card.DotShown, Is.True, "06 §4.2: un punto si hay casilla pendiente");
            Assert.That(card.StateText, Is.EqualTo("Sella la página de hoy"));
            card.Refresh(LoginCalendarState.Sealed);
            Assert.That(card.DotShown, Is.False);
            Assert.That(card.StateText, Is.EqualTo("Sellada hoy"), "06 §2.3, pantalla 4");
            card.SetShown(false);
            Assert.That(card.Visible, Is.False);
            card.SetShown(true);
            Assert.That(card.Visible, Is.True);
        }

        [Test]
        public void EveryKeyOfTheLogbook_IsInItsTable()
        {
            foreach (string key in LogbookText.NewKeys)
            {
                Assert.That(Loc.Has(key), Is.True, key);
            }
        }

        private LoginCalendarSnapshot Peek()
        {
            return _service.Peek(25, LoginCalendarUnlocks.All, 1);
        }
    }

    /// <summary>Tablas de textos para las pruebas de la Bitácora: las del proyecto más <c>B4-strings.csv</c> (como hará B4-strings).</summary>
    internal static class LogbookTestTables
    {
        public const string CsvPath = "docs/dev/traspaso/spark-enganches/B4-strings.csv";

        public static LocCatalog Catalog()
        {
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile);
            string facts = LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<LocCsvRow> rows = LocCsv.Parse(strings, LocCatalog.StringsFile);
            for (int i = 1; i < rows.Count; i++)
            {
                keys.Add(rows[i].Fields[0]);
            }

            string extra = LocCsv.Decode(System.IO.File.ReadAllBytes(LocTables.ProjectPath(CsvPath)), "B4-strings.csv");
            var pending = new System.Text.StringBuilder();
            IReadOnlyList<LocCsvRow> extraRows = LocCsv.Parse(extra, "B4-strings.csv");
            foreach (string line in extra.Substring(extra.IndexOf('\n') + 1).Split('\n'))
            {
                int comma = line.IndexOf(',');
                if (comma > 0 && !keys.Contains(line.Substring(0, comma)))
                {
                    pending.Append(line).Append('\n');
                }
            }

            Assert.That(extraRows.Count, Is.GreaterThan(LogbookText.NewKeys.Length), "B4-strings.csv se leyó entero");
            string body = strings.EndsWith("\n", StringComparison.Ordinal) ? strings : strings + "\n";
            return LocCatalog.Parse(body + pending, facts);
        }
    }
}
