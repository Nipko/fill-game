#nullable enable
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Textos y contraste de la pantalla de inicio (tarea B1-6; 06 §4.7; glosario §10; 07 §1.2 y §7.8): «Jugar · Nivel {n}»,
    /// chips de dificultad, contadores sin asignaciones con separador de miles, cronómetro mm:ss, línea <c>AF_STATE</c> del
    /// inicio y texto ≥ 4,5:1.
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
        public void LobbyLayout_PutsThePlayButtonAt60PercentAndControlsAtLeast48dp()
        {
            Assert.That(LobbyScreen.PlayCenterFraction, Is.EqualTo(0.60f), "06 §4.7: «60 % del alto visual»");
            Assert.That(LobbyScreen.MinTouchDp, Is.GreaterThanOrEqualTo(48f));
            Assert.That(LobbyScreen.ReferenceWidth, Is.EqualTo(360f));
        }
    }
}
