#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Luciérnaga y Morpho con la Tinaja, la tarjeta del estreno, el consejo del 324, la sonda y los consumidores de las ocultas
    /// (tarea TJ4; M13, D-019; 04 §2.3 y §2.5; 02 §9.1 M13 «Tutorial»; propuesta de la Tinaja, b.2 y c.6):
    /// <list type="bullet">
    /// <item><description><b>Luciérnaga</b>: su ranura sale en los niveles con tinaja, solo late la tinaja, revela todas sus capas
    /// (la barriga se enciende y las capas salen como bandas, nunca como agua turbia), y nunca se cobra sin efecto (su cuia, sin
    /// ocultas, cancela gratis; sin ocultas, la ranura se va).</description></item>
    /// <item><description><b>Morpho</b>: las 4 respuestas en tableros con tinaja, con su frase «… la tinaja» y sin cobrar salvo el
    /// vertido; nunca «No veo un vertido seguro» con una sola realidad.</description></item>
    /// <item><description><b>Tarjeta del estreno</b> (323) con sus dos líneas, ninguna de más de 2 líneas a 360 dp, y el
    /// <b>consejo</b> del 324 (solo con tinaja).</description></item>
    /// <item><description><b>AF_STATE tinaja</b> y los consumidores de <c>hidden</c>/<c>murky</c> (chip «?» del HUD y de la
    /// tarjeta, sonda <c>murky</c>, iconos), y <c>ui_tinaja_show_contents</c> = false.</description></item>
    /// </list>
    /// Tableros: el 323 del paquete de muestra (<see cref="TinajaVisualTests.Pack323"/>: su cuia en la 1 y la tinaja en la 2 con
    /// [jenipapo*, lulo*, jenipapo]) y el S0 de b.2 (<see cref="S0Pack"/>), que tiene ruta segura y pierde con un vertido.
    /// </summary>
    public sealed class TinajaBoostersTests
    {
        private const int Cucharon = TinajaVisualTests.CucharonIndex;
        private const int Tinaja = TinajaVisualTests.TinajaIndex;
        private const int Level = TinajaTutorial.DebutLevel;

        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // Luciérnaga (04 §2.5)
        // ---------------------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Reveal_SlotInTinajaLevels_OnlyTheTinajaPulses_AndItsCuiaCancelsForFree()
        {
            Profile profile = FunnelSetup.Profile(500);
            profile.Stock.Reveal = 1;
            yield return LoadPlaying(TinajaVisualTests.Pack323(Cucharon, Tinaja), Level, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;

            // 04 §2.5 y 07 §7.4: la ranura sale «en niveles con ocultos: Aguas turbias y, desde D-019, la Tinaja», la última.
            Assert.That(flow.Level.HasTinaja && !flow.Level.IsMurky, Is.True, "un nivel con tinaja no es de Aguas turbias");
            Assert.That(tray.RevealShown, Is.True, "la ranura de la Luciérnaga, también con la tinaja");
            Assert.That(BoosterTray.PositionOf(TraySlot.Reveal, tray.ActiveMask), Is.EqualTo(tray.SlotCount - 1), "Revelar, la última");
            Assert.That(tray.RevealBadgeText, Is.EqualTo("+1"));
            Assert.That(view.HudMurkyChip.Visible, Is.False, "sin el chip «?» de Aguas turbias en la cabecera");

            // El modo de selección: solo laten las cuias con ocultas, aquí solo la tinaja (no su cuia, D-002).
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(flow.RevealSelecting, Is.True);
            for (int i = 0; i < flow.Session.Current.Count; i++)
            {
                Assert.That(view.RevealPulse.IsPulsing(i), Is.EqualTo(i == Tinaja), "cuia " + i + ": solo late la tinaja");
            }

            // Nunca se cobra sin efecto: su cuia (sin ocultas) cancela la selección sin coste y sin tocar el tablero.
            BoardState before = flow.Session.Current;
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(flow.RevealSelecting, Is.False, "cancela");
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1), "sin cobrar");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(500));
            Assert.That(flow.Session.Current, Is.SameAs(before), "nada cambia");
            Assert.That(view.RevealAnimating, Is.False);
        }

        [UnityTest]
        public IEnumerator Reveal_TheTinaja_AllItsLayers_BellyLightsUp_BandsTopToBottom_NeverMurky()
        {
            Profile profile = FunnelSetup.Profile(500);
            profile.Stock.Reveal = 1;
            yield return LoadPlaying(TinajaVisualTests.Pack323(Cucharon, Tinaja), Level, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            CuiaView tv = view.Views[Tinaja];
            TinajaOverlay jar = JarOf(tv);
            Cuia start = flow.Session.Current[Tinaja];
            Assert.That(start.HiddenMask, Is.EqualTo(0b11), "las dos capas de debajo, ocultas");

            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Ignored), "el toque elige la tinaja");

            // 04 §2.5: revela todas las capas ocultas de la tinaja; aplicar → guardar → cobrar.
            Assert.That(flow.Session.Current[Tinaja].HiddenMask, Is.EqualTo(0), "todas sus capas");
            Assert.That(flow.Session.HiddenCount, Is.EqualTo(0));
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0), "cobrada: tuvo efecto");
            Assert.That(view.RevealAnimating, Is.True);
            Assert.That(tv.ShownCuia.HiddenMask, Is.EqualTo(0b11), "mientras vuela, la tinaja sigue cerrada");

            int flight = 0;
            while (!view.RevealMarksStarted)
            {
                Assert.That(flight, Is.LessThan(2000), "la luciérnaga se posa");
                PlayModeHelpers.TickExact(boot, 4);
                flight += 4;
            }

            // Entra por la boca (b.2): se posa sobre el labio de la vasija, más alto que el vidrio.
            float u = tv.Unit;
            Vector2 mouth = tv.FootPosition + tv.BodyOffset + new Vector2(0f, ProceduralArt.TinajaTopU * u);
            Assert.That(Vector2.Distance(view.RevealFirefly.Position, mouth), Is.LessThan(0.8f * u), "posada en la boca de la tinaja");
            Assert.That(jar.FireflyAnimating, Is.True, "la barriga se enciende por dentro");
            Assert.That(jar.FireflyMask, Is.EqualTo(0b11));
            Assert.That(tv.MarksAnimating, Is.True);
            Assert.That(tv.Murky.IsAnimating, Is.False, "no hay agua turbia que aclarar (contrato 27)");

            // La luz baja de la boca: la banda de arriba (capa 1) se enciende antes que la de abajo (capa 0).
            float speed = view.Animator.StepSpeedBase;
            float maxGlow = 0f;
            int firstBand1 = -1;
            int firstBand0 = -1;
            int glowMs = 0;
            while (jar.FireflyAnimating)
            {
                Assert.That(glowMs, Is.LessThan(2000));
                TinajaVisualTests.AssertNotMurky(tv, tv.ShownCuia, "Luciérnaga a los " + glowMs + " ms");
                maxGlow = Mathf.Max(maxGlow, jar.GlowAlpha);
                if (firstBand1 < 0 && jar.Band(1).gameObject.activeSelf)
                {
                    firstBand1 = glowMs;
                }

                if (firstBand0 < 0 && jar.Band(0).gameObject.activeSelf)
                {
                    firstBand0 = glowMs;
                }

                PlayModeHelpers.TickExact(boot, 4);
                glowMs += 4;
            }

            Assert.That(glowMs, Is.EqualTo(TinajaOverlay.FireflyMs * speed).Within(8f), "0,6 s × factor (04 §2.5)");
            Assert.That(maxGlow, Is.EqualTo(TinajaOverlay.FireflyGlowAlpha).Within(0.02f), "la barriga encendida");
            Assert.That(firstBand1, Is.GreaterThanOrEqualTo(0));
            Assert.That(firstBand0, Is.GreaterThan(firstBand1), "de arriba abajo");
            Assert.That(jar.GlowAlpha, Is.EqualTo(0f), "la luz se apaga");

            StepUntilRevealDone(boot);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Cuia now = flow.Session.Current[Tinaja];
            Assert.That(jar.BandMask, Is.EqualTo(0b11), "las dos capas, como bandas de luz con su icono");
            Assert.That(jar.Band(0).color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(jar.BandGlyph(1).color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(jar.ChipCount, Is.EqualTo(3), "«×3»: la Luciérnaga no saca nada");
            TinajaVisualTests.AssertNotMurky(tv, now, "tras la Luciérnaga");
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(view.Tray.RevealShown, Is.False, "sin ocultas, la ranura se va");
            Assert.That(boot.PressReveal(), Is.False, "y no se puede volver a pagar");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(500));

            // Lo revelado sigue revelado tras sacar y Marea (R7, I5).
            Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            Assert.That(HasEvent("\"event\":\"pour\"", "\"tinaja\":{\"cuia\":2,\"partner\":1,\"doses\":2,\"hidden\":0,\"revealed\":1"), Is.True, "AF_STATE tinaja tras revelar y sacar");
            Assert.That(flow.Undo(), Is.True);
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            Assert.That(flow.Session.Current[Tinaja].HiddenMask, Is.EqualTo(0), "Marea conserva lo revelado");
            Assert.That(jar.BandMask, Is.EqualTo(0b11));
        }

        [UnityTest]
        public IEnumerator Reveal_ReduceMotion_BandsFadeTogether_WithoutTheGlow()
        {
            Profile profile = FunnelSetup.Profile(500);
            profile.Stock.Reveal = 1;
            yield return LoadPlaying(TinajaVisualTests.Pack323(Cucharon, Tinaja), Level, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.Flow!.ReduceMotion = true; // Reducir movimiento (02 §12)
            TinajaOverlay jar = JarOf(view.Views[Tinaja]);
            Assert.That(boot.PressReveal(), Is.True);
            boot.TapCuia(Tinaja);
            for (int t = 0; t < 3000 && !view.RevealMarksStarted; t += 4)
            {
                PlayModeHelpers.TickExact(boot, 4);
            }

            Assert.That(jar.FireflyAnimating, Is.True);
            int ms = 0;
            while (jar.FireflyAnimating)
            {
                Assert.That(jar.GlowAlpha, Is.EqualTo(0f), "sin la luz de dentro");
                Assert.That(jar.Band(0).gameObject.activeSelf, Is.EqualTo(jar.Band(1).gameObject.activeSelf), "a la vez");
                PlayModeHelpers.TickExact(boot, 4);
                ms += 4;
            }

            Assert.That(ms, Is.EqualTo(TinajaOverlay.ReducedFadeMs * view.Animator.StepSpeedBase).Within(8f), "fundido de 0,25 s");
            StepUntilRevealDone(boot);
            Assert.That(jar.BandMask, Is.EqualTo(0b11));
        }

        [UnityTest]
        public IEnumerator Reveal_SlotLeaves_WhenScoopingShowsEveryLayer_NothingToPayFor()
        {
            Profile profile = FunnelSetup.Profile(500);
            yield return LoadPlaying(TinajaVisualTests.Pack323(Cucharon, Tinaja), Level, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(view.Tray.RevealShown, Is.True);
            Assert.That(view.Tray.RevealPriceShown, Is.EqualTo(120), "sin stock, el precio");

            // Sacar dos veces enseña las dos capas de debajo (R7): ya no queda nada oculto.
            for (int i = 0; i < 2; i++)
            {
                Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Poured));
                PlayModeHelpers.StepUntilIdle(boot, 6000);
            }

            Assert.That(flow.Session.HiddenCount, Is.EqualTo(0));
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Tray.RevealShown, Is.False, "04 §2.5: «se oculta si ya no quedan unidades ocultas»");
            Assert.That(flow.RevealAvailable, Is.False);
            Assert.That(boot.PressReveal(), Is.False);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(500), "nada que cobrar");
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // Morpho (04 §2.3; 02 T5; D-019)
        // ---------------------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Morpho_FourAnswers_OnTinajaBoards_OnlyThePourCharges()
        {
            Profile profile = FunnelSetup.Profile(500);
            profile.Stock.Hint = 3;
            yield return LoadPlaying(S0Pack(), Level, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(flow.Level.HasTinaja, Is.True);
            Assert.That(view.Tray.HintShown, Is.True);

            // 1. Un vertido seguro en todas las realidades: se resalta y se cobra (del stock).
            yield return PressAndWait(boot);
            HintResult pour = flow.LastHint!.Value;
            Assert.That(pour.Answer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(pour.Tinaja, Is.True, "tablero con tinaja");
            Assert.That(pour.Solver, Is.EqualTo(HintSolver.SafeMoves), "sobre las realidades, sin poda (C1-8)");
            Assert.That(pour.Charged, Is.True);
            Assert.That(PourRules.IsLegal(flow.Session.Current, pour.Move.Source, pour.Move.Target), Is.True);
            Assert.That(flow.HasActiveHint, Is.True);
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Hint), Is.EqualTo(2));
            Assert.That(view.HintAnswerCard.Visible, Is.False, "un vertido no saca tarjeta");
            Assert.That(HasMorphoLine("\"answer\":\"pour\"", "\"tinaja\":true"), Is.True, "AF_PERF morpho");

            // 4. Se agota hint_timeout_ms antes de decidir: «No encuentro un camino desde aquí», sin cobrar.
            yield return NotFound(boot);
            Assert.That(flow.LastHint!.Value.Tinaja, Is.True);
            Assert.That(flow.LastHint.Value.Charged, Is.False);
            Assert.That(view.HintAnswerCard.Visible, Is.True);
            Assert.That(view.HintAnswerCard.Text, Is.EqualTo(UiText.HintAnswerText(HintAnswer.NotFound, true)));
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(2), "no cobra");
            view.HintAnswerCard.CloseButton.onClick.Invoke();

            // 2. Ninguna realidad tiene solución: «Desde aquí no hay salida: la Marea o la orilla», sin cobrar, también con R = 1 y
            // nunca «No veo un vertido seguro» (b.2). El tablero perdido de b.2 (el S0 tras el desvío sacar, N1 → N2, su cuia → N1),
            // con el Morpho del cliente, entregado a la vista: en la partida, ese estado ya abre Aguas estancadas (su cierre se
            // agota sin victoria; R13 con D-014), donde Morpho no está (abajo).
            var morpho = new MorphoBooster(BoosterConfig.Defaults, new ManualClock(FunnelSetup.T0));
            HintResult noExit = morpho.Compute(LostBoard(), null, PourMode.Run, true);
            Assert.That(noExit.Answer, Is.EqualTo(HintAnswer.NoExit));
            Assert.That(noExit.Realities, Is.EqualTo(1), "R = 1: «no hay salida», nunca «no veo un vertido seguro»");
            Assert.That(noExit.Tinaja, Is.True);
            view.OnHint(noExit);
            Assert.That(view.HintAnswerCard.Answer, Is.EqualTo(HintAnswer.NoExit));
            Assert.That(view.HintAnswerCard.Text, Is.EqualTo(UiText.HintAnswerText(HintAnswer.NoExit, true)));
            view.HintAnswerCard.CloseButton.onClick.Invoke();

            // 3. Alguna realidad con solución y ningún vertido seguro en todas: «No veo un vertido seguro: la luciérnaga ilumina la
            // tinaja». En un tablero de campaña con tinaja es rarísimo (b.2: 0 de ~6.400 estados; tampoco sale en 10.000 estados
            // de paseos al azar por el 323), así que se calcula con el Morpho del cliente sobre el tablero de prueba de
            // MorphoBoosterTests (R = 2, con una tinaja inerte) y se entrega a la vista.
            HintResult noSafe = morpho.Compute(NoSafePourBoard(), null, PourMode.Run, true);
            Assert.That(noSafe.Answer, Is.EqualTo(HintAnswer.NoSafePour));
            Assert.That(noSafe.Tinaja, Is.True);
            Assert.That(noSafe.Realities, Is.GreaterThanOrEqualTo(2), "nunca con R = 1");
            view.OnHint(noSafe);
            Assert.That(view.HintAnswerCard.Answer, Is.EqualTo(HintAnswer.NoSafePour));
            Assert.That(view.HintAnswerCard.Text, Is.EqualTo(UiText.HintAnswerText(HintAnswer.NoSafePour, true)));
            Assert.That(view.HintAnswerCard.Text, Does.EndWith("la tinaja"), "«… la tinaja» (04 §2.3 a)");
            view.HintAnswerCard.CloseButton.onClick.Invoke();
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(2), "no cobra");

            // En la partida: el vertido que pierde (N1 → N2) abre Aguas estancadas; Morpho ya no se puede pedir ni cobra.
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Poured));
            for (int t = 0; t < 8000 && flow.Phase == FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.HasActiveHint, Is.False, "la pista se apaga al verter otra cosa");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal), "R13 con D-014: el cierre se agota sin victoria");
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(BoosterConfig.HintCooldownMs + 1));
            Assert.That(flow.Hint.CanUse, Is.False, "en Aguas estancadas Morpho no está (04 §2.3)");
            Assert.That(boot.PressHint(), Is.False);
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(2));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(500));
        }

        [Test]
        public void Morpho_AfterTheFireflyRevealsTheTinaja_OneReality_NeverNoSafePour()
        {
            // Tras la Luciérnaga la tinaja ya no esconde nada: una sola realidad, así que Morpho nunca manda a por otra luciérnaga.
            BoardState s0 = TinajaVisualTests.Board323();
            var cuias = new Cuia[s0.Count];
            for (int i = 0; i < s0.Count; i++)
            {
                cuias[i] = i == Tinaja ? s0[i].WithRevealed(s0[i].HiddenMask) : s0[i];
            }

            var lit = new BoardState(cuias);
            Assert.That(lit.HiddenCount, Is.EqualTo(0));
            var morpho = new MorphoBooster(BoosterConfig.Defaults, new ManualClock(FunnelSetup.T0));
            HintResult r = morpho.Compute(lit, s0, PourMode.Run, true);
            Assert.That(r.Realities, Is.EqualTo(1));
            Assert.That(r.Answer, Is.Not.EqualTo(HintAnswer.NoSafePour));
            Assert.That(r.Tinaja, Is.True);
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // Tarjeta del estreno y consejo (02 §9.1 M13 «Tutorial»)
        // ---------------------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator DebutCard_323_TwoLines_EachAtMostTwoLinesAt360dp_WithTheJarAndNoMurkyChip()
        {
            LaunchOptions.LevelCardsOverride = true;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGameWithPack(DebutPack(), Level, new MemoryProgressStore(Level, Level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "la tarjeta del estreno");
            LevelCardInfo card = flow.LevelCard!;
            Assert.That(card.IntroMechanic, Is.EqualTo(TinajaTutorial.IntroMechanic));
            Assert.That(card.IsMurky, Is.False, "la tinaja no es Aguas turbias: sin chip «?»");
            Assert.That(LevelCardInfo.DebutMechanicType(card.IntroMechanic), Is.EqualTo(MechanicType.Tinaja));
            PlayModeHelpers.Step(boot, 600);

            LevelCardView v = view.LevelCard;
            Assert.That(v.TutorialText, Is.EqualTo(UiText.TutorialTinaja), "línea 1 (02 §9.1 M13)");
            Assert.That(v.ScoopText, Is.EqualTo(UiText.TutorialTinajaScoop), "línea 2, en su renglón");
            Assert.That(v.FireflyText, Is.Null, "sin la línea de la Luciérnaga ni regalo");
            Assert.That(v.GiftText, Is.Null);
            Assert.That(UiText.TutorialTinaja, Is.EqualTo("La tinaja no deja ver: solo ves la fruta de arriba, y la de debajo nunca es igual."));
            Assert.That(UiText.TutorialTinajaScoop, Is.EqualTo("Saca el agua con su cuia chica: le caben dos dosis."));
            Assert.That(v.TutorialLines, Is.InRange(1, LevelCardView.MaxTutorialLines), "la línea 1, ≤ 2 líneas a 360 dp");
            Assert.That(v.Tutorial.fontSize, Is.GreaterThanOrEqualTo(12f - 0.01f));
            TextMeshProUGUI second = v.SecondLine!;
            second.ForceMeshUpdate(true, true);
            Assert.That(second.textInfo.lineCount, Is.InRange(1, 2), "la línea 2, ≤ 2 líneas");
            Assert.That(second.fontSize, Is.GreaterThanOrEqualTo(12f - 0.01f));
            Assert.That(v.HeroSprite, Is.Not.Null, "el medallón con la vasija");
            Assert.That(v.HeroSprite, Is.SameAs(view.CardArt.CardHeroIcon(TinajaTutorial.IntroMechanic)));
            Assert.That(v.HeroSprite!.name, Is.EqualTo("card_hero_tinaja"), "la vasija (ProceduralArt.DrawTinajaGlyph; antes, transparente)");
            Assert.That(view.LastCardProbe, Does.Contain("\"intro\":\"tinaja\"").And.Contain("\"murky\":false"));

            // Un toque la cierra y sigue la intro.
            Assert.That(boot.TapAnywhere(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [UnityTest]
        public IEnumerator Tip_324_OneLine_OnlyWithTheTinaja()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGameWithPack(PackAt(TinajaVisualTests.Pack323(Cucharon, Tinaja), TinajaTutorial.TipLevel), TinajaTutorial.TipLevel, new MemoryProgressStore(TinajaTutorial.TipLevel, TinajaTutorial.TipLevel - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            int shown = view.TinajaTipShownCount;
            Assert.That(view.ToastText, Is.Null, "nada durante la intro (D-011, punto 2)");
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.ToastText, Is.EqualTo(UiText.TutorialTinajaTip), "«Si sacas dos veces, la primera dosis queda debajo.»");
            Assert.That(UiText.TutorialTinajaTip, Is.EqualTo("Si sacas dos veces, la primera dosis queda debajo."));
            Assert.That(view.TinajaTipShownCount, Is.EqualTo(shown + 1));
            Assert.That(HasEvent("\"event\":\"tinaja_tip\""), Is.True);

            // El 325, con el mismo tablero: sin consejo.
            yield return PlayModeHelpers.LoadGameWithPack(PackAt(TinajaVisualTests.Pack323(Cucharon, Tinaja), TinajaTutorial.TipLevel + 1), TinajaTutorial.TipLevel + 1, new MemoryProgressStore(TinajaTutorial.TipLevel + 1, TinajaTutorial.TipLevel));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 100);
            Assert.That(boot.View!.ToastText, Is.Null);
        }

        [Test]
        public void Tip_Rule_OnlyOnTheSecondDebutBoard_WithTheTinaja()
        {
            Assert.That(TinajaTutorial.TipKeyFor(324, true), Is.EqualTo("tutorial_tinaja_tip"));
            Assert.That(TinajaTutorial.TipKeyFor(324, false), Is.Null, "salida B: el 324 sin tinaja, sin consejo");
            Assert.That(TinajaTutorial.TipKeyFor(323, true), Is.Null, "el 323 lleva la tarjeta");
            Assert.That(TinajaTutorial.TipKeyFor(325, true), Is.Null);
            Assert.That(UiText.Tutorial(TinajaTutorial.IntroMechanic), Is.EqualTo(UiText.TutorialTinaja));
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // AF_STATE tinaja y consumidores de hidden / murky
        // ---------------------------------------------------------------------------------------------------------------------

        [Test]
        public void Probe_TinajaFields_DosesRevealedAndHung_AndNoMurkyWater()
        {
            BoardState s0 = TinajaVisualTests.Board323();
            var sb = new StringBuilder();
            DevProbe.AppendBoardFields(sb, s0, s0);
            string fields = sb.ToString();
            Assert.That(fields, Does.StartWith(",\"murky\":false"), "la tinaja no es agua turbia");
            Assert.That(fields, Does.EndWith(",\"tinaja\":{\"cuia\":2,\"partner\":1,\"doses\":3,\"hidden\":2,\"revealed\":0,\"mouth\":9,\"hung\":true,\"show_contents\":false}"));

            // Sacar 2, verter su cuia y sacar la última: su cuia baja del jirau (R10 f) y la tinaja queda vacía.
            BoardState s = PourRules.Apply(s0, Tinaja, Cucharon, PourMode.Run);
            sb.Clear();
            DevProbe.AppendBoardFields(sb, s0, s);
            Assert.That(sb.ToString(), Does.Contain("\"doses\":2,\"hidden\":1,\"revealed\":0,\"mouth\":11,\"hung\":true"));
            s = PourRules.Apply(s, Tinaja, Cucharon, PourMode.Run);
            s = PourRules.Apply(s, Cucharon, 7, PourMode.Run);
            s = PourRules.Apply(s, Cucharon, 9, PourMode.Run);
            s = PourRules.Apply(s, Tinaja, Cucharon, PourMode.Run);
            Assert.That(s[Tinaja].IsEmpty, Is.True);
            sb.Clear();
            DevProbe.AppendBoardFields(sb, s0, s);
            Assert.That(sb.ToString(), Does.Contain("\"tinaja\":{\"cuia\":2,\"partner\":1,\"doses\":0,\"hidden\":0,\"revealed\":0,\"mouth\":0,\"hung\":false"));

            // Un tablero de Aguas turbias sigue con murky = true y sin campo tinaja.
            var murky = new BoardState(new[] { Cuia.Create(4, new[] { 5, 6, 11 }, 0b011), Cuia.Empty(4) });
            sb.Clear();
            DevProbe.AppendBoardFields(sb, murky, murky);
            Assert.That(sb.ToString(), Does.StartWith(",\"murky\":true").And.Not.Contain("\"tinaja\""));
        }

        [UnityTest]
        public IEnumerator Probe_LoadLine_CarriesTheTinaja_AndHudHasNoMurkyChip()
        {
            yield return LoadPlaying(TinajaVisualTests.Pack323(Cucharon, Tinaja), Level, FunnelSetup.Profile(0));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(HasEvent("\"event\":\"load\"", "\"murky\":false", "\"tinaja\":{\"cuia\":2,\"partner\":1,\"doses\":3,\"hidden\":2"), Is.True, "AF_STATE load");
            Assert.That(boot.View!.HudMurkyChip.Visible, Is.False);

            // Sacar: la línea del vertido lleva la tinaja con una dosis menos y la capa revelada (R7).
            boot.TapCuia(Tinaja);
            boot.TapCuia(Cucharon);
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            Assert.That(HasEvent("\"event\":\"pour\"", "\"doses\":2,\"hidden\":1"), Is.True, "AF_STATE pour");
        }

        [Test]
        public void Consumers_Icons_LevelDefinition_AndShowContentsOff()
        {
            using (var art = new ProceduralArt("white"))
            {
                Sprite? jar = art.MechanicIcon(MechanicType.Tinaja);
                Assert.That(jar, Is.Not.Null);
                Assert.That(jar!.name, Is.EqualTo("mech_icon_tinaja"));
                Assert.That(art.MechanicIcon(MechanicType.Cucharon), Is.Null, "su cuia sin icono: la pareja cuenta como una mecánica (02 §9.2)");
            }

            // El dibujo de la vasija (el del icono de 32 dp y el del medallón): una silueta opaca, más alta que ancha, sin tintes.
            Vector3Int glyph = ProceduralArt.MeasureTinajaGlyph(96);
            int opaque = glyph.x;
            Assert.That(opaque, Is.GreaterThan(1500), "la vasija se ve (antes, transparente)");
            Assert.That(glyph.z, Is.GreaterThan(1.8f * glyph.y), "más alta que ancha (b.2 «Aspecto»)");

            Assert.That(GameConfig.UiTinajaShowContents, Is.False, "ui_tinaja_show_contents = false (08 §9; Q-Ti5-A)");
            Assert.That(TinajaTutorial.ShowsContents, Is.False);
            Assert.That(DevProbe.HasMurkyWater(TinajaVisualTests.Board323()), Is.False);
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// S0 de la propuesta de la Tinaja, b.2 («Por qué hace falta el orden»), en el 323: N[c,c,a] N[a,b,a] N[b,b,b]
        /// T[c*,a*,c] y su cuia vacía (a = urucum, b = camu camu, c = jenipapo). Tiene ruta segura; el vertido N1 → N2 lo pierde
        /// con una sola realidad.
        /// </summary>
        internal static string S0Pack()
        {
            return "{ \"pack_schema\": 3, \"pack_id\": \"test_m07_v1_s0\", \"kind\": \"campaign\", \"world\": 7, \"world_id\": \"w07_madre_de_dios\", "
                + "\"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"m07_v1_n0323\", \"level\": 323, \"world\": 7, \"lap\": 1, "
                + "\"water_base\": \"white\", \"palette\": [\"urucum\", \"camu_camu\", \"jenipapo\"], \"units_per_color\": 4, "
                + "\"caps\": [4, 4, 4, 5, 2], \"cuias\": [[9, 9, 5], [5, 6, 5], [6, 6, 6], [9, 5, 9], []], "
                + "\"attrs\": [null, null, null, { \"type\": \"tinaja\" }, { \"type\": \"cucharon\" }], "
                + "\"murky\": true, \"hidden\": [[3, 0], [3, 1]], \"difficulty\": { \"par\": 8 }, \"relief\": [] } ] }";
        }

        /// <summary>El 323 de muestra con <c>intro_mechanic</c> = <c>tinaja</c>: la tarjeta del estreno.</summary>
        internal static string DebutPack()
        {
            string json = TinajaVisualTests.Pack323(Cucharon, Tinaja);
            string debut = json.Replace("\"relief\": []", "\"relief\": [], \"intro_mechanic\": \"tinaja\"");
            Assert.That(debut, Is.Not.EqualTo(json), "el paquete de prueba cambió de forma");
            return debut;
        }

        /// <summary>El paquete <paramref name="json"/> del 323 movido al nivel <paramref name="level"/>.</summary>
        internal static string PackAt(string json, int level)
        {
            string l = level.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string moved = json.Replace("\"level\": 323", "\"level\": " + l).Replace("n0323", "n0" + l);
            Assert.That(moved, Is.Not.EqualTo(json));
            return moved;
        }

        // El tablero perdido de b.2 con los tintes del S0: N[c,c,c] N[a,b,a,a] N[b,b,b] T[c*,a] y su cuia vacía.
        private static BoardState LostBoard()
        {
            return new BoardState(new[]
            {
                Cuia.Create(4, new[] { 9, 9, 9 }), Cuia.Create(4, new[] { 5, 6, 5, 5 }), Cuia.Create(4, new[] { 6, 6, 6 }),
                Cuia.Create(LevelValidation.TinajaCapacity, new[] { 9, 5 }, 0x1).WithMechanic(MechanicType.Tinaja),
                Cuia.Empty(LevelValidation.CucharonCapacity).WithMechanic(MechanicType.Cucharon),
            });
        }

        // El tablero de prueba de la respuesta 3 de MorphoBoosterTests: Aguas turbias con R = 2, alguna realidad con solución y
        // ningún vertido seguro en las dos, con una tinaja vacía e inerte al lado (no es de campaña).
        private static BoardState NoSafePourBoard()
        {
            return new BoardState(new[]
            {
                Cuia.Create(3, new[] { 3, 3, 1 }, 0x2), Cuia.Create(3, new[] { 2, 1 }, 0x1), Cuia.Create(3, new[] { 3, 2, 2 }), Cuia.Create(3, new[] { 1 }),
                Cuia.Empty(LevelValidation.TinajaCapacity).WithMechanic(MechanicType.Tinaja),
            });
        }

        internal static TinajaOverlay JarOf(CuiaView view)
        {
            return (TinajaOverlay)view.OverlayOf(MechanicType.Tinaja)!;
        }

        internal static IEnumerator LoadPlaying(string pack, int level, Profile profile)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGameWithPack(pack, level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 6000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "nivel " + level + " en juego");
            PlayModeHelpers.Step(boot, 300);
        }

        internal static void StepUntilRevealDone(GameBootstrap boot)
        {
            for (int t = 0; boot.View!.RevealAnimating; t += 4)
            {
                Assert.That(t, Is.LessThan(5000), "el revelado termina");
                PlayModeHelpers.TickExact(boot, 4);
            }
        }

        internal static IEnumerator PressAndWait(GameBootstrap boot)
        {
            Assert.That(boot.PressHint(), Is.True);
            float start = Time.realtimeSinceStartup;
            while (boot.Flow!.HintThinking)
            {
                Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "Morpho no respondió");
                PlayModeHelpers.TickExact(boot, 4);
                yield return null;
            }
        }

        // «No encuentro un camino desde aquí»: el hilo de Morpho, retenido, pasa el corte de hint_timeout_ms (como MorphoUiTests).
        internal static IEnumerator NotFound(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(BoosterConfig.HintCooldownMs + 1));
            using (var gate = new ManualResetEventSlim(false))
            {
                flow.Morpho = new MorphoBooster(flow.Boosters, flow.Clock, () => gate.Wait(20000));
                Assert.That(boot.PressHint(), Is.True);
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(flow.Boosters.HintTimeoutMs));
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                gate.Set();
                float start = Time.realtimeSinceStartup;
                while (flow.HintThinking)
                {
                    Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "Morpho no respondió");
                    PlayModeHelpers.TickExact(boot, 4);
                    yield return null;
                }
            }

            Assert.That(flow.LastHint!.Value.Answer, Is.EqualTo(HintAnswer.NotFound));
        }

        private bool HasEvent(params string[] parts)
        {
            foreach (string line in _lines)
            {
                if (!line.StartsWith("AF_STATE", StringComparison.Ordinal))
                {
                    continue;
                }

                bool all = true;
                foreach (string p in parts)
                {
                    all &= line.Contains(p);
                }

                if (all)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasMorphoLine(params string[] parts)
        {
            foreach (string line in _lines)
            {
                if (!line.StartsWith("AF_PERF {\"event\":\"morpho\"", StringComparison.Ordinal))
                {
                    continue;
                }

                bool all = true;
                foreach (string p in parts)
                {
                    all &= line.Contains(p);
                }

                if (all)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            _lines.Add(condition);
        }
    }
}
