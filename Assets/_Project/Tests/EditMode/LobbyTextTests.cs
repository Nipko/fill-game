#nullable enable
using AF.Economy;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Textos y contraste de la pantalla de inicio (tarea B1-6; 06 §4.7; glosario §10; 07 §1.2 y §7.8): «Jugar · Nivel {n}»,
    /// chips de dificultad, contadores sin asignaciones con separador de miles, cronómetro mm:ss, línea <c>AF_STATE</c> del
    /// inicio y texto ≥ 4,5:1. Con las etiquetas de D-009 (tarea T08; propuesta-mecanicas-dificultad.md §c.1-c.2): subtítulos
    /// con su interruptor, cinta «×n» desde <c>eco_reward_*</c>, «Premio: n» y el contraste del chip, la cinta, el botón
    /// nocturno y las cabeceras de color.
    /// </summary>
    public sealed class LobbyTextTests
    {
        [Test]
        public void PlayButton_SaysJugarNivelN()
        {
            Assert.That(UiText.PlayLevel(1), Is.EqualTo("Jugar · Nivel 1"));
            Assert.That(UiText.PlayLevel(387), Is.EqualTo("Jugar · Nivel 387"));
        }

        [Test]
        public void DifficultyChip_IsDificilOrMuyDificil_AndNothingForNormal()
        {
            Assert.That(UiText.DifficultyChip("hard"), Is.EqualTo("Difícil"));
            Assert.That(UiText.DifficultyChip("very_hard"), Is.EqualTo("Muy difícil"));
            Assert.That(UiText.DifficultyChip("normal"), Is.Null);
        }

        [TestCase(0, false, "0")]
        [TestCase(7, false, "7")]
        [TestCase(1240, false, "1240")]
        [TestCase(0, true, "0")]
        [TestCase(999, true, "999")]
        [TestCase(1000, true, "1.000")]
        [TestCase(36000, true, "36.000")]
        [TestCase(1234567, true, "1.234.567")]
        [TestCase(-5, true, "0")]
        public void WriteNumber_UsesTheThousandsSeparatorOfEs419(int value, bool grouped, string expected)
        {
            var buffer = new char[24];
            buffer[0] = '#';
            int end = UiText.WriteNumber(buffer, 1, value, grouped);
            Assert.That(new string(buffer, 1, end - 1), Is.EqualTo(expected));
            Assert.That(buffer[0], Is.EqualTo('#'), "no escribe antes de la posición pedida");
        }

        [TestCase(0L, "00:00")]
        [TestCase(59L, "00:59")]
        [TestCase(25L * 60L, "25:00")]
        [TestCase((18L * 60L) + 42L, "18:42")]
        [TestCase(3600L, "1:00:00")]
        [TestCase((44L * 60L) + 55L, "44:55")]
        [TestCase(-3L, "00:00")]
        public void WriteClock_IsMmSs_OrHMmSsFromOneHour(long seconds, string expected)
        {
            var buffer = new char[24];
            int end = UiText.WriteClock(buffer, 0, seconds);
            Assert.That(new string(buffer, 0, end), Is.EqualTo(expected));
        }

        [Test]
        public void ContentEnd_UsesTheEtaOfLiveContentEndMessage()
        {
            Assert.That(UiText.ContentEnd(GameConfig.LiveContentEndNextBatchEtaDays), Does.Contain("14 días"));
            Assert.That(UiText.ContentEndTitle, Is.EqualTo("El río sigue creciendo"));
        }

        [Test]
        public void LobbyTexts_HaveAtLeast4_5To1()
        {
            // 07 §1.2 y §7.8 (D-003 no lo cambia): texto con contraste ≥ 4,5:1 sobre su fondo.
            foreach ((string name, UnityEngine.Color text, UnityEngine.Color background) in LobbyScreen.ContrastPairs())
            {
                Assert.That(ColorMath.ContrastRatio(text, background), Is.GreaterThanOrEqualTo(4.5f), name);
            }
        }

        [Test]
        public void LabelChipAndModalHeaders_HaveAtLeast4_5To1()
        {
            // 02 §12 y 07 §7.2: tinta sobre ámbar (5,4:1) y marfil sobre índigo (10,7:1), en el chip, la cinta, el botón nocturno
            // y las cabeceras de ¿Seguro? y Nivel fallido.
            foreach ((string name, UnityEngine.Color text, UnityEngine.Color background) in DifficultyChip.ContrastPairs())
            {
                Assert.That(ColorMath.ContrastRatio(text, background), Is.GreaterThanOrEqualTo(4.5f), name);
            }

            foreach ((string name, UnityEngine.Color text, UnityEngine.Color background) in ModalKit.HeaderContrastPairs())
            {
                Assert.That(ColorMath.ContrastRatio(text, background), Is.GreaterThanOrEqualTo(4.5f), name);
            }

            Assert.That(ColorMath.ContrastRatio(Palette.Ink, Palette.UiAmber), Is.EqualTo(5.4f).Within(0.1f), "07 §7.2: tinta sobre ámbar 5,4:1");
            Assert.That(ColorMath.ContrastRatio(Palette.UiPaper, Palette.UiIndigo), Is.EqualTo(10.7f).Within(0.1f), "07 §7.2: marfil sobre índigo 10,7:1");
            Assert.That(DifficultyChip.TextOn(false), Is.EqualTo((UnityEngine.Color)Palette.Ink), "07 §7.7: texto tinta en Difícil");
            Assert.That(DifficultyChip.TextOn(true), Is.EqualTo((UnityEngine.Color)Palette.UiPaper), "07 §7.7: texto marfil en Muy difícil");
            Assert.That(ModalKit.HeaderTitleColor(Palette.UiAmber), Is.EqualTo((UnityEngine.Color)Palette.Ink));
            Assert.That(ModalKit.HeaderTitleColor(Palette.UiIndigo), Is.EqualTo((UnityEngine.Color)Palette.UiPaper));
        }

        [Test]
        public void HeaderBand_IsAmberForHard_IndigoForVeryHard_AndWoodForNormal()
        {
            Assert.That(ModalKit.HeaderBandFor("hard"), Is.EqualTo((UnityEngine.Color?)Palette.UiAmber));
            Assert.That(ModalKit.HeaderBandFor("very_hard"), Is.EqualTo((UnityEngine.Color?)Palette.UiIndigo));
            Assert.That(ModalKit.HeaderBandFor("normal"), Is.Null);
            Assert.That(ModalKit.HeaderBandFor(null), Is.Null);
        }

        [Test]
        public void DifficultySubtitle_IsAguasBravasOrNocheDeJaguar_AndHidesWithTheSwitch()
        {
            // Glosario §6.1 y 02 §10; 08 §9: ui_show_difficulty_subtitles = true por defecto.
            Assert.That(GameConfig.UiShowDifficultySubtitles, Is.True);
            Assert.That(UiText.DifficultySubtitle("hard"), Is.EqualTo("Aguas bravas"));
            Assert.That(UiText.DifficultySubtitle("very_hard"), Is.EqualTo("Noche de jaguar"));
            Assert.That(UiText.DifficultySubtitle("normal"), Is.Null);
            Assert.That(UiText.DifficultySubtitle(null), Is.Null);

            // Con el interruptor apagado (02 §10: «se apagan si confunden») no sale ninguno.
            Assert.That(UiText.DifficultySubtitle("hard", false), Is.Null);
            Assert.That(UiText.DifficultySubtitle("very_hard", false), Is.Null);
            Assert.That(UiText.DifficultySubtitle("hard", true), Is.EqualTo("Aguas bravas"));
        }

        [Test]
        public void RibbonAndRewardLine_AreTimesNAndPremioN()
        {
            Assert.That(UiText.Ribbon(3), Is.EqualTo("×3"));
            Assert.That(UiText.Ribbon(4), Is.EqualTo("×4"));
            Assert.That(UiText.RewardLine(60), Is.EqualTo("Premio: 60"));
            Assert.That(UiText.RewardLine(80), Is.EqualTo("Premio: 80"));
            Assert.That(UiText.RewardLine(1000), Is.EqualTo("Premio: 1.000"));
        }

        [Test]
        public void RibbonMultiplier_ComesFromEcoReward_NotTheOtherWayRound()
        {
            // Propuesta §c.2 y 08 §9: 60 / 20 = ×3 y 80 / 20 = ×4.
            EconomyConfig config = EconomyConfig.Defaults;
            Assert.That(DifficultyChip.MultiplierFor(config, "hard"), Is.EqualTo(3));
            Assert.That(DifficultyChip.MultiplierFor(config, "very_hard"), Is.EqualTo(4));
            Assert.That(DifficultyChip.MultiplierFor(config, "normal"), Is.EqualTo(1));
            Assert.That(DifficultyChip.MultiplierFor(config, null), Is.EqualTo(1));
            Assert.That(DifficultyChip.MultiplierFor(config, "otra"), Is.EqualTo(1));

            // Remote Config puede igualar dos tramos: la etiqueta sigue siendo la del nivel y la cinta dice lo que toca.
            config.EcoRewardHard = 20;
            config.EcoRewardVeryHard = 100;
            Assert.That(DifficultyChip.MultiplierFor(config, "hard"), Is.EqualTo(1), "sin cinta, pero el chip sigue (lo decide la etiqueta)");
            Assert.That(DifficultyChip.MultiplierFor(config, "very_hard"), Is.EqualTo(5));
            config.EcoRewardNormal = 0;
            Assert.That(DifficultyChip.MultiplierFor(config, "very_hard"), Is.EqualTo(1), "sin premio normal no se divide");
        }

        [Test]
        public void LobbyLayout_PutsThePlayButtonAt60PercentAndControlsAtLeast48dp()
        {
            Assert.That(LobbyScreen.PlayCenterFraction, Is.EqualTo(0.60f), "06 §4.7: «60 % del alto visual»");
            Assert.That(LobbyScreen.MinTouchDp, Is.GreaterThanOrEqualTo(48f));
            Assert.That(LobbyScreen.ReferenceWidth, Is.EqualTo(360f));
        }
    }
}
