#nullable enable
using System;
using AF.Localization;
using AF.LiveOps;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La tarjeta de 2 s que presenta el Reto del río tras ganar el nivel 7 (enganche E6; 06 §2.2, fila 7; 02 §13), sola, sin la
    /// escena: cuándo sale (<see cref="DailyTeaserCard.ShouldShow"/>), su texto con <c>live_unlock_level_daily</c>, la espera,
    /// los 2 s a la vista, que se va sola o con la capa de la victoria, que no captura toques y que se coloca entre la cima de
    /// las cuias y la cinta del panel. El gancho en <c>GameScreen.OnVictoryScreen</c> y su prueba con la victoria real del 7 van
    /// en el parche <c>docs/dev/traspaso/spark-enganches/E6.diff</c>. Tabla de textos propia: las del proyecto más
    /// <c>docs/dev/traspaso/spark-strings-reto.csv</c> (como hará E5).
    /// </summary>
    public sealed class DailyTeaserCardTests
    {
        // El arte procedural cuesta unos 3 s por instancia: uno para todo el fixture (T6, carril A); las pantallas solo leen
        // sus sprites.
        private static ProceduralArt? s_art;
        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        private const float CanvasW = 360f;
        private GameObject? _host;
        private RectTransform _layer = null!;
        private DailyTeaserCard _card = null!;

        [SetUp]
        public void SetUp()
        {
            Loc.Configure(DailyTestTables.Catalog(), LocLocale.SourceLocale, true);
            _host = new GameObject("TeaserHost", typeof(RectTransform));
            _layer = UiBuilder.Rect("VictoryOverlay", _host.transform);
            _card = new DailyTeaserCard(_layer, Art);
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
        public void ShouldShow_OnlyInTheVictoryOf7_WithTheRetoInTheBuild_TheSwitchOn_AndBeforeItsUnlock()
        {
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;
            Assert.That(DailyTeaserCard.TeaserLevel, Is.EqualTo(7), "06 §2.2 y 02 §13: tras ganar el 7");
            Assert.That(DailyTeaserCard.ShouldShow(7, true, config), Is.True);
            Assert.That(DailyTeaserCard.ShouldShow(6, true, config), Is.False);
            Assert.That(DailyTeaserCard.ShouldShow(8, true, config), Is.False);
            Assert.That(DailyTeaserCard.ShouldShow(10, true, config), Is.False, "el 10 tiene su propia secuencia (06 §2.3)");
            Assert.That(DailyTeaserCard.ShouldShow(7, false, config), Is.False, "sin el Reto en el build no promete nada (D-008)");

            config.BizFeatureDailyChallenge = false;
            Assert.That(DailyTeaserCard.ShouldShow(7, true, config), Is.False, "biz_feature_daily_challenge apagado");
            config.BizFeatureDailyChallenge = true;
            config.LiveUnlockLevelDaily = 7;
            Assert.That(DailyTeaserCard.ShouldShow(7, true, config), Is.False, "ya desbloqueado: el inicio ya lo enseña [P]");
            Assert.That(DailyTeaserCard.VisibleMs, Is.EqualTo(2000f), "tarjeta de 2 s");
        }

        [Test]
        public void Text_ComesFromItsKey_WithLiveUnlockLevelDaily()
        {
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;
            Assert.That(DailyTeaserCard.TextFor(config), Is.EqualTo("Desde el nivel 10, un tablero nuevo cada día"), "06 §2.2");
            config.LiveUnlockLevelDaily = 12;
            Assert.That(DailyTeaserCard.TextFor(config), Is.EqualTo("Desde el nivel 12, un tablero nuevo cada día"), "el número es la clave de 08 §9");
            Assert.That(Loc.Has(DailyTeaserCard.TextKey), Is.True);
        }

        [Test]
        public void Show_WaitsForTheHarvest_StaysTwoSeconds_AndGoesAlone_WithoutTakingTouches()
        {
            Assert.That(_card.Show(7, true, CanvasW, 96f, 200f, 420f), Is.True);
            Assert.That(_card.Pending, Is.True);
            Assert.That(_card.Visible, Is.False, "espera a que la Cosecha termine de contar");
            Tick(DailyTeaserCard.DelayMs - 16f);
            Assert.That(_card.Visible, Is.False);
            Tick(32f);
            Assert.That(_card.Visible, Is.True);
            Assert.That(_card.Text, Is.EqualTo("Desde el nivel 10, un tablero nuevo cada día"));
            Assert.That(_card.BlocksRaycasts, Is.False, "no captura toques: Siguiente, Inicio y la Cosecha doble siguen respondiendo");
            Assert.That(_card.Root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true), Has.All.Matches<UnityEngine.UI.Graphic>(g => !g.raycastTarget));
            Tick(DailyTeaserCard.VisibleMs - 64f);
            Assert.That(_card.Visible, Is.True, "a la vista 2 s");
            Tick(64f);
            Assert.That(_card.Visible, Is.False, "y se va sola");
            Assert.That(_card.Pending, Is.False);
        }

        [Test]
        public void Show_SitsOverTheCuias_BetweenTheirTopAndTheRibbon_NeverOverThePanel()
        {
            const float minTop = 96f;
            const float cuiasTop = 200f;
            const float ribbonTop = 420f;
            _card.Show(7, true, CanvasW, minTop, cuiasTop, ribbonTop);
            Tick(DailyTeaserCard.DelayMs + 16f);
            Rect seen = _card.SeenRect;
            Assert.That(seen.yMin, Is.GreaterThanOrEqualTo(cuiasTop - 0.5f), "debajo de la caja de TiTino");
            Assert.That(seen.yMax, Is.LessThanOrEqualTo(ribbonTop + 0.5f), "encima de la cinta y del panel");
            Assert.That(seen.center.x, Is.EqualTo(CanvasW * 0.5f).Within(0.5f), "centrada");
            Assert.That((seen.yMin - cuiasTop) - (ribbonTop - seen.yMax), Is.EqualTo(Toast.EdgeDp).Within(1f), "centrada en la franja");

            // Franja más baja que la placa: se apoya en la cinta y sube, sin pasar del pie de la cabecera.
            _card.Show(7, true, CanvasW, minTop, 130f, 170f);
            Tick(DailyTeaserCard.DelayMs + 16f);
            seen = _card.SeenRect;
            Assert.That(seen.yMin, Is.GreaterThanOrEqualTo(minTop - 0.5f));
            Assert.That(seen.yMin, Is.LessThan(130f), "sube por encima de la cima de las cuias");
            Assert.That(seen.yMax - Toast.EdgeDp, Is.EqualTo(170f).Within(0.5f), "apoyada en la cinta");
        }

        [Test]
        public void LeavesWithTheVictoryLayer_AndOtherVictoriesHideIt()
        {
            _card.Show(7, true, CanvasW, 96f, 200f, 420f);
            Tick(DailyTeaserCard.DelayMs + 16f);
            Assert.That(_card.Visible, Is.True);
            _layer.gameObject.SetActive(false); // Siguiente o Inicio cierran la victoria
            Tick(16f);
            Assert.That(_card.Pending, Is.False, "se va con la victoria");
            _layer.gameObject.SetActive(true);
            Tick(16f);
            Assert.That(_card.Visible, Is.False, "y no vuelve en la victoria siguiente");

            _card.Show(7, true, CanvasW, 96f, 200f, 420f);
            Assert.That(_card.Show(8, true, CanvasW, 96f, 200f, 420f), Is.False);
            Tick(DailyTeaserCard.DelayMs + 16f);
            Assert.That(_card.Visible, Is.False, "la victoria del 8 no la lleva");
        }

        [Test]
        public void Tick_DoesNotAllocate()
        {
            _card.Show(7, true, CanvasW, 96f, 200f, 420f);
            Tick(DailyTeaserCard.DelayMs + 16f);
            void Run()
            {
                for (int i = 0; i < 40; i++)
                {
                    _card.Tick(16f);
                }
            }

            Assert.That(Run, Is.Not.AllocatingGCMemory());
        }

        private void Tick(float ms)
        {
            const float frame = 16f;
            float left = ms;
            while (left > 0f)
            {
                float dt = Mathf.Min(frame, left);
                _card.Tick(dt);
                left -= dt;
            }
        }
    }

    /// <summary>
    /// Tablas de textos de los tests del Reto: las del proyecto y, detrás, las filas de <c>spark-strings-reto.csv</c> cuya clave
    /// aún no está en <c>strings.csv</c> (las que E5 ya integró no se repiten; hoy, las de E5b: <c>daily_teaser</c> y
    /// <c>no_canoes_daily</c>). Con E5b aplicado, son las tablas del proyecto tal cual.
    /// </summary>
    internal static class DailyTestTables
    {
        public const string CsvPath = "docs/dev/traspaso/spark-strings-reto.csv";

        public static LocTableTexts Texts()
        {
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile);
            string facts = LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile);
            var keys = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            System.Collections.Generic.IReadOnlyList<LocCsvRow> rows = LocCsv.Parse(strings, LocCatalog.StringsFile);
            for (int i = 1; i < rows.Count; i++)
            {
                keys.Add(rows[i].Fields[0]);
            }

            string daily = LocCsv.Decode(System.IO.File.ReadAllBytes(LocTables.ProjectPath(CsvPath)), "spark-strings-reto.csv");
            var pending = new System.Text.StringBuilder();
            foreach (string line in daily.Substring(daily.IndexOf('\n') + 1).Split('\n'))
            {
                int comma = line.IndexOf(',');
                if (comma > 0 && !keys.Contains(line.Substring(0, comma)))
                {
                    pending.Append(line).Append('\n');
                }
            }

            string body = strings.EndsWith("\n", StringComparison.Ordinal) ? strings : strings + "\n";
            return new LocTableTexts(body + pending, facts);
        }

        public static LocCatalog Catalog()
        {
            LocTableTexts texts = Texts();
            return LocCatalog.Parse(texts.Strings!, texts.Facts);
        }
    }
}
