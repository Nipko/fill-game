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
        public void LabelSeals_EveryColorAndDiscGradientIsAtLeast5DeltaE00FromEveryTint_SaveTheThinOrTranslucentJaguarDetails()
        {
            // 07 §3, regla 6, y direccion-visual-v2 §2.4, regla 1, y §7 («HEX de tintes en la UI»): los colores de la UI quedan a
            // ΔE00 ≥ 5 de los 14 tintes. Se miden TODOS los colores de los dos sellos (ProceduralArt.LabelSealColors): el
            // degradado de cada disco de DrawMedallionBase cada 5 %, su brillo, el aro con su luz, su filete y su sombra, la rana,
            // el jaguar y los brillos. En el HUD (T09), junto al tablero, un sello con un color de tinte se leería como una dosis.
            // Antes fallaban el borde del disco de la rana (1,8 del Copoazú), su brillo (4,3), el hocico del jaguar (4,5 del
            // Copoazú) y la mitad del degradado de la noche (2,6 del Huito).
            //
            // Excepciones de §2.4, regla 1 («líneas de ≤ 2 dp»), con su motivo. Nada más puede quedar por debajo de 5.
            var exceptions = new System.Collections.Generic.Dictionary<string, string>
            {
                ["JaguarEye"] = "el iris ámbar (4,6 del Taperebá) mide 2,2 × 1,4 dp con la pupila en medio: lo visible son franjas de ≤ 1,4 dp",
                ["JaguarShade"] = "(5,0 del Agua del Río Negro) solo se pinta en líneas de ≤ 2 dp (borde de la oreja de 0,6 dp y centro de las rosetas de ≤ 1,9 dp) y en la sombra de la nuca con alfa 0,55",
            };
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach ((string name, UnityEngine.Color color) in ProceduralArt.LabelSealColors())
            {
                Assert.That(seen.Add(name), Is.True, "color repetido en la lista: " + name);
                Assert.That(color.a, Is.EqualTo(1f), name + ": se mide el color opaco");
                if (exceptions.ContainsKey(name))
                {
                    continue;
                }

                for (int tint = 1; tint <= 14; tint++)
                {
                    Assert.That(ColorMath.DeltaE00(color, Palette.Tint(tint)), Is.GreaterThanOrEqualTo(5f), name + " frente al tinte " + tint);
                }
            }

            foreach (string name in exceptions.Keys)
            {
                Assert.That(seen.Contains(name), Is.True, "la excepción " + name + " tiene que seguir en la lista: " + exceptions[name]);
            }

            // Se miden los dos discos enteros, del centro al borde.
            Assert.That(seen.Contains("RanaDisc@0.00") && seen.Contains("RanaDisc@0.50") && seen.Contains("RanaDisc@1.00"), Is.True);
            Assert.That(seen.Contains("JaguarDisc@0.00") && seen.Contains("JaguarDisc@0.50") && seen.Contains("JaguarDisc@1.00"), Is.True);
            Assert.That(seen.Contains("JaguarCream") && seen.Contains("RanaRing") && seen.Contains("RanaDiscGlow"), Is.True);

            // La rana sigue lejos del Aguaje y del Achiote, los dos tintes naranjas.
            Assert.That(ColorMath.DeltaE00(ProceduralArt.FrogOrange, Palette.Tint(4)), Is.EqualTo(11.7f).Within(0.2f), "frente al Aguaje #EF7D1A");
            Assert.That(ColorMath.DeltaE00(ProceduralArt.FrogOrange, Palette.Tint(5)), Is.EqualTo(9.8f).Within(0.2f), "frente al Achiote #CF3E1C");
        }

        [Test]
        public void LabelSeals_FrogAndJaguarGlyphsHaveAtLeast3To1OverTheirWholeDisc()
        {
            // direccion-visual-v2 §2.4, regla 2: iconos y glifos ≥ 3:1. El naranja de la rana (#E85D12) se quedaba en 2,5:1 sobre
            // el borde del disco crema; ahora es #D05802, ≥ 3,1:1 en todo el degradado.
            float frogMin = float.MaxValue;
            foreach ((string name, UnityEngine.Color glyph, UnityEngine.Color background) in ProceduralArt.LabelSealContrastPairs())
            {
                float ratio = ColorMath.ContrastRatio(glyph, background);
                Assert.That(ratio, Is.GreaterThanOrEqualTo(3f), name);
                if (glyph == ProceduralArt.FrogOrange)
                {
                    frogMin = UnityEngine.Mathf.Min(frogMin, ratio);
                }
            }

            Assert.That(frogMin, Is.EqualTo(3.15f).Within(0.05f), "el naranja de la rana sobre el borde del disco");
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
