#nullable enable
using System.Collections.Generic;
using System.IO;
using AF.GameFlow;
using AF.Levels;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// En qué niveles sale el rótulo del lugar (<see cref="StopLabelRule"/>; D-013, punto 8; propuesta del Mapa §10.5, punto 5;
    /// tarea M2.3): los 12 niveles de 1-200 con las paradas y los miquitos reales (P-M6 (a): el 31 incluido); 41 (banner), 61 y
    /// 111 (Fruta nueva), 1 y 11 (tutorial), los estrenos y la tarjeta del miquito no lo llevan; sin catálogo, en un reintento o
    /// con <c>ui_stop_label</c> apagada, tampoco; los textos de los 12 con los nombres de facts.csv y sin «Belén»; el contraste
    /// del rótulo; y cero asignaciones.
    /// </summary>
    public sealed class StopLabelRuleTests
    {
        private static readonly int[] Expected = { 31, 51, 71, 91, 101, 121, 131, 151, 161, 171, 181, 191 };

        private static FaunaCatalog? s_fauna;
        private static int s_sink;

        private static FaunaCatalog Fauna =>
            s_fauna ??= FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));

        [Test]
        public void In1To200_ItShowsOnTheTwelveLevelsOfTheProposal()
        {
            RiverStopCatalog stops = MapTestCatalogs.Real;
            var shown = new List<int>();
            for (int level = 1; level <= 200; level++)
            {
                if (StopLabelRule.ShowsOn(level, stops, true, StopLabelRule.MonkeyCardOn(level, Fauna)))
                {
                    shown.Add(level);
                }
            }

            Assert.That(shown, NUnit.Framework.Is.EqualTo(Expected), "31, 51, 71, 91, 101, 121, 131, 151, 161, 171, 181 y 191");
        }

        [Test]
        public void LevelsWithTheirOwnPresentation_DoNotShowIt()
        {
            RiverStopCatalog stops = MapTestCatalogs.Real;
            Assert.That(StopLabelRule.ShowsOn(41, stops, true, false), NUnit.Framework.Is.False, "41: banner de mundo");
            Assert.That(StopLabelRule.ShowsOn(81, stops, true, false), NUnit.Framework.Is.False, "81: banner de mundo");
            Assert.That(StopLabelRule.ShowsOn(141, stops, true, false), NUnit.Framework.Is.False, "141: banner de mundo y Fruta nueva");
            Assert.That(StopLabelRule.ShowsOn(61, stops, true, false), NUnit.Framework.Is.False, "61: Fruta nueva");
            Assert.That(StopLabelRule.ShowsOn(111, stops, true, false), NUnit.Framework.Is.False, "111: Fruta nueva");
            Assert.That(StopLabelRule.ShowsOn(21, stops, true, false), NUnit.Framework.Is.False, "21: Fruta nueva");
            Assert.That(StopLabelRule.ShowsOn(11, stops, true, false), NUnit.Framework.Is.False, "11: tutorial, banner y Fruta nueva");
            Assert.That(StopLabelRule.ShowsOn(1, stops, true, false), NUnit.Framework.Is.False, "1: tutorial (y no es 10n + 1 con n ≥ 1)");
            Assert.That(StopLabelRule.IsTaken(1) && StopLabelRule.IsTaken(12), NUnit.Framework.Is.True, "el tutorial es 1-12 (02 §13)");
            Assert.That(StopLabelRule.IsTaken(13), NUnit.Framework.Is.False);
            foreach (int debut in SightingRule.MechanicDebutLevels)
            {
                Assert.That(StopLabelRule.IsTaken(debut), NUnit.Framework.Is.True, "estreno en el " + debut);
            }

            foreach (int level in Expected)
            {
                Assert.That(StopLabelRule.ShowsOn(level, stops, true, true), NUnit.Framework.Is.False, level + ": con la tarjeta del miquito, no");
            }
        }

        [Test]
        public void OnlyTheFirstLevelOfAStop_FromStopOne()
        {
            Assert.That(StopLabelRule.IsStopFirstLevel(1), NUnit.Framework.Is.False, "n ≥ 1");
            Assert.That(StopLabelRule.IsStopFirstLevel(11), NUnit.Framework.Is.True);
            Assert.That(StopLabelRule.IsStopFirstLevel(31), NUnit.Framework.Is.True);
            Assert.That(StopLabelRule.IsStopFirstLevel(30), NUnit.Framework.Is.False);
            Assert.That(StopLabelRule.IsStopFirstLevel(32), NUnit.Framework.Is.False);
            Assert.That(StopLabelRule.IsStopFirstLevel(991), NUnit.Framework.Is.True);
            Assert.That(StopLabelRule.ShowsOn(32, MapTestCatalogs.Real, true, false), NUnit.Framework.Is.False);
            Assert.That(StopLabelRule.ShowsOn(40, MapTestCatalogs.Real, true, false), NUnit.Framework.Is.False);
        }

        [Test]
        public void WithoutCatalog_OnARetry_OrWithTheKeyOff_ItDoesNotShow()
        {
            RiverStopCatalog stops = MapTestCatalogs.Real;
            foreach (int level in Expected)
            {
                Assert.That(StopLabelRule.ShowsOn(level, null, true, false), NUnit.Framework.Is.False, level + ": sin catálogo");
                Assert.That(StopLabelRule.ShowsOn(level, stops, false, false), NUnit.Framework.Is.False, level + ": reintento");
                Assert.That(StopLabelRule.ShowsOn(level, stops, true, false, false), NUnit.Framework.Is.False, level + ": ui_stop_label apagada");
                Assert.That(StopLabelRule.ShowsOn(level, stops, true, false, true), NUnit.Framework.Is.True, level.ToString());
            }

            Assert.That(MapConfig.UiStopLabel, NUnit.Framework.Is.True, "ui_stop_label = true (08 §9)");
        }

        [Test]
        public void AfterTheCatalog_ItNeedsAStop()
        {
            Assert.That(StopLabelRule.ShowsOn(211, MapTestCatalogs.Real, true, false), NUnit.Framework.Is.False, "el 211 no tiene parada en 1-200");
            Assert.That(StopLabelRule.ShowsOn(211, MapTestCatalogs.Full, true, false), NUnit.Framework.Is.True, "con 100 paradas, sí");
            Assert.That(StopLabelRule.ShowsOn(201, MapTestCatalogs.Full, true, false), NUnit.Framework.Is.False, "201: banner y Fruta nueva");
            Assert.That(StopLabelRule.ShowsOn(1001, MapTestCatalogs.Full, true, false), NUnit.Framework.Is.False, "fuera de 1-1.000");
        }

        [Test]
        public void MonkeyCard_FollowsTheSightingRule_WithoutArt()
        {
            FaunaCatalog fauna = Fauna;
            Assert.That(StopLabelRule.MonkeyCardOn(5, fauna), NUnit.Framework.Is.True, "saimiri_collinsi en el 5");
            Assert.That(StopLabelRule.MonkeyCardOn(13, fauna), NUnit.Framework.Is.True, "mico_argentatus en el 13");
            Assert.That(StopLabelRule.MonkeyCardOn(42, fauna), NUnit.Framework.Is.True);
            Assert.That(StopLabelRule.MonkeyCardOn(82, fauna), NUnit.Framework.Is.True);
            Assert.That(StopLabelRule.MonkeyCardOn(142, fauna), NUnit.Framework.Is.True);
            foreach (int level in Expected)
            {
                Assert.That(StopLabelRule.MonkeyCardOn(level, fauna), NUnit.Framework.Is.False, level.ToString());
            }

            Assert.That(StopLabelRule.MonkeyCardOn(13, null), NUnit.Framework.Is.False, "sin catálogo");
            Assert.That(StopLabelRule.MonkeyCardOn(0, fauna), NUnit.Framework.Is.False);
            Assert.That(StopLabelRule.MonkeyCardOn(1001, fauna), NUnit.Framework.Is.False);
        }

        [Test]
        public void Texts_UseTheNamesOfFactsCsv_AndNeverBelen()
        {
            RiverStopCatalog stops = MapTestCatalogs.Real;
            foreach (int level in Expected)
            {
                string text = UiText.StopLabel(stops.StopOf(level)!);
                Assert.That(text, NUnit.Framework.Does.Contain(" · "), level.ToString());
                Assert.That(text, NUnit.Framework.Does.Not.Contain("Belén").And.Not.Contain("Belen"), level + ": «Belén» nunca (07 §8.1)");
                Assert.That(text.Length, NUnit.Framework.Is.LessThanOrEqualTo(48), level + ": max_chars de stop_label");
            }

            Assert.That(UiText.StopLabel(stops.StopOf(31)!), NUnit.Framework.Is.EqualTo("Floresta Nacional do Tapajós · Brasil"));
            Assert.That(UiText.StopLabel(stops.StopOf(131)!), NUnit.Framework.Is.EqualTo("El río frontera · Colombia y Perú"), "country_name");
            Assert.That(UiText.StopLabel(stops.StopOf(151)!), NUnit.Framework.Is.EqualTo("Río Itaya (Iquitos) · Perú"));
        }

        [Test]
        public void ContrastPairs_AreAtLeast45To1()
        {
            foreach ((string name, Color text, Color background) in StopLabel.ContrastPairs())
            {
                Assert.That(AF.Presentation.ColorMath.ContrastRatio(text, background), NUnit.Framework.Is.GreaterThanOrEqualTo(4.5f), name);
            }
        }

        [Test]
        public void ShowsOn_ForEveryLevel_DoesNotAllocate()
        {
            RiverStopCatalog real = MapTestCatalogs.Real;
            RiverStopCatalog full = MapTestCatalogs.Full;
            FaunaCatalog fauna = Fauna;
            TestDelegate call = () =>
            {
                int acc = 0;
                for (int level = 1; level <= WorldTable.LastLevel; level++)
                {
                    bool monkey = StopLabelRule.MonkeyCardOn(level, fauna);
                    acc += StopLabelRule.ShowsOn(level, real, true, monkey) ? 1 : 0;
                    acc += StopLabelRule.ShowsOn(level, full, true, monkey) ? 1 : 0;
                    acc += StopLabelRule.ShowsOn(level, full, false, monkey) ? 1000 : 0;
                    acc += StopLabelRule.ShowsOn(level, null, true, false) ? 1 : 0;
                }

                s_sink = acc;
            };
            call(); // calentamiento
            Assert.That(call, Is.Not.AllocatingGCMemory());
            Assert.That(s_sink, NUnit.Framework.Is.GreaterThan(12).And.LessThan(1000), "ningún reintento lo saca");
        }
    }
}
