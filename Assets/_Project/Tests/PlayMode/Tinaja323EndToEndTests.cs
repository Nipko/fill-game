#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El 323 real con la Tinaja, de punta a punta (tarea C1c; D-019 Q-Ti3; 02 §4.1, §9.1 M13, R7 y R10 f; 04 §2.3 y §2.5;
    /// propuesta de la Tinaja, b.2 y c.6): el paquete real de la variante «a» de GEN79 (<see cref="VariantAPackJson"/>; se
    /// prueba aunque la escena publique la «b», la salida B de D-019, mientras la compuerta de PF1 no conste en verde), con la
    /// tarjeta del estreno (no saltable: <c>skippable</c> = false), la intro, Morpho y la Luciérnaga como dice TJ4 y la solución
    /// del A* del núcleo jugada con los toques del jugador: cada sacada con el gesto de sacar de TJ3 (se mueve su cuia, la tinaja
    /// no se inclina y vuelven las gotas), la suelta de su cuia al vaciarla, las ocultas de la tinaja nunca pintadas como agua
    /// turbia y, al final, la victoria sin excepciones ni errores en el registro.
    /// </summary>
    public sealed class Tinaja323EndToEndTests
    {
        private const int Level = TinajaTutorial.DebutLevel;

        private readonly List<string> _states = new List<string>();
        private readonly List<string> _errors = new List<string>();

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            _states.Clear();
            _errors.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        /// <summary>
        /// El paquete <c>mundo_07_v1.json</c> de la variante «a» (con la Tinaja, estreno en el 323): el publicado si la escena
        /// publica la «a» (<see cref="LevelCatalog.TinajaPublished"/>) y, si no, el que el generador escribe con
        /// <c>--variant a</c> en <c>tools/out/salida_a/</c> (fuera de <c>Assets/</c>; GEN79 y C1b).
        /// </summary>
        internal static string VariantAPackJson()
        {
            return File.ReadAllText(LevelCatalog.TinajaPublished
                ? Path.Combine(Application.dataPath, "_Project", "Data", "Levels", "mundo_07_v1.json")
                : Path.Combine(Application.dataPath, "..", "tools", "out", "salida_a", "mundo_07_v1.json"));
        }

        /// <summary>
        /// Carga el 323 real de la «a» en la escena <c>Game</c> con las tarjetas encendidas y el reloj del test: se queda en la
        /// tarjeta del estreno (LEVEL_CARD).
        /// </summary>
        internal static IEnumerator LoadReal323(Profile profile)
        {
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGameWithPack(VariantAPackJson(), Level, new MemoryProgressStore(Level, Level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
        }

        /// <summary>La solución óptima del A* del núcleo desde el tablero de la sesión, sin poda de fútiles (nivel con tinaja).</summary>
        internal static SolveResult Solve(LevelFlow flow)
        {
            BoardState start = flow.Session.Current;
            SolveResult solution = new AStarSolver().Solve(start, SolverOptions.ForPipeline(flow.Session.PourMode, start));
            Assert.That(solution.IsSolved, Is.True, "el 323 real se resuelve");
            return solution;
        }

        [UnityTest]
        public IEnumerator Real323_DebutCard_Morpho_Firefly_AndTheAStarSolution_WithTheScoopGesture_ToVictory()
        {
            Profile profile = FunnelSetup.Profile(500);
            profile.Stock.Hint = 3;
            profile.Stock.Reveal = 1;
            yield return LoadReal323(profile);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;

            // El nivel real: el 323 del paquete de la «a», con la Tinaja y su cuia, sin Aguas turbias.
            Assert.That(flow.Catalog.PackOf(Level)!.PackId, Is.EqualTo("mundo_07_v1"));
            LevelDefinition def = flow.Level;
            Assert.That(def.HasTinaja, Is.True, "variante a: el 323 lleva la Tinaja");
            Assert.That(def.IsMurky, Is.False, "un nivel con tinaja no es de Aguas turbias");
            int tinaja = MechanicTestKit.IndexOf(flow, MechanicType.Tinaja);
            int scoop = MechanicTestKit.IndexOf(flow, MechanicType.Cucharon);
            Assert.That(flow.Session.Current[tinaja].HiddenMask, Is.Not.EqualTo(0), "la tinaja esconde las capas de debajo");

            // La tarjeta del estreno sale, no se cierra sola y el nivel no se puede saltar (skippable = false, 04 §2.6).
            Assert.That(def.IntroMechanic, Is.EqualTo(TinajaTutorial.IntroMechanic));
            Assert.That(def.Skippable, Is.False, "estreno: no saltable");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "la tarjeta del estreno");
            LevelCardView card = view.LevelCard;
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Debut));
            Assert.That(card.Card.IntroMechanic, Is.EqualTo(TinajaTutorial.IntroMechanic));
            Assert.That(card.Card.IsMurky, Is.False, "sin chip «?»");
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "la tarjeta espera al toque");
            Assert.That(card.TutorialText, Is.EqualTo(UiText.TutorialTinaja));
            Assert.That(card.ScoopText, Is.EqualTo(UiText.TutorialTinajaScoop));
            Assert.That(view.LastCardProbe, Does.Contain("\"intro\":\"tinaja\"").And.Contain("\"kind\":\"debut\"").And.Contain("\"murky\":false"));
            CardSetup.AssertInsidePanel(view);

            // Un toque la cierra; la intro corre entera y la tinaja nunca se pinta como agua turbia.
            Assert.That(boot.TapAnywhere(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            CuiaView tv = view.Views[tinaja];
            for (int t = 0; t < 8000 && (flow.Phase != FlowPhase.Playing || !view.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                AssertNoMurkyLook(tv, "intro a los " + t + " ms");
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.HudMurkyChip.Visible, Is.False, "sin el chip «?» de Aguas turbias en la cabecera");
            AssertJarClosed(view, tinaja, flow.Session.Current[tinaja], "al empezar");
            Assert.That(HasState("\"event\":\"load\"", "\"murky\":false", "\"tinaja\":{\"cuia\":" + tinaja + ",\"partner\":" + scoop), Is.True, "AF_STATE load con la tinaja");

            // Morpho (04 §2.3; TJ4): un vertido seguro en todas las realidades, sobre ellas (SafeMoves), cobrado del stock.
            yield return TinajaBoostersTests.PressAndWait(boot);
            HintResult hint = flow.LastHint!.Value;
            Assert.That(hint.Tinaja, Is.True, "tablero con tinaja");
            Assert.That(hint.Answer, Is.EqualTo(HintAnswer.Pour), "el 323 tiene ruta segura");
            Assert.That(hint.Solver, Is.EqualTo(HintSolver.SafeMoves));
            Assert.That(hint.Realities, Is.GreaterThanOrEqualTo(1));
            Assert.That(PourRules.IsLegal(flow.Session.Current, hint.Move.Source, hint.Move.Target), Is.True);
            Assert.That(hint.Charged, Is.True);
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Hint), Is.EqualTo(2));
            Assert.That(view.HintAnswerCard.Visible, Is.False, "un vertido no saca tarjeta");

            // La Luciérnaga (04 §2.5; TJ4): su ranura sale, solo late la tinaja y su cuia (sin ocultas) cancela gratis.
            BoosterTray tray = view.Tray;
            Assert.That(tray.RevealShown, Is.True, "la ranura de la Luciérnaga, también con la tinaja");
            Assert.That(boot.PressReveal(), Is.True);
            for (int i = 0; i < flow.Session.Current.Count; i++)
            {
                Assert.That(view.RevealPulse.IsPulsing(i), Is.EqualTo(i == tinaja), "cuia " + i + ": solo late la tinaja");
            }

            BoardState before = flow.Session.Current;
            Assert.That(boot.TapCuia(scoop).Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(flow.RevealSelecting, Is.False, "cancela");
            Assert.That(flow.Session.Current, Is.SameAs(before), "nada cambia");
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1), "nunca se cobra sin efecto");

            // La solución del A*, con los toques del jugador; cada sacada con el gesto de sacar.
            int scoops = PlaySolution(boot, tinaja, scoop);
            Assert.That(scoops, Is.EqualTo(3), "las 3 dosis de la tinaja salen con su cuia");
            Assert.That(flow.Session.HiddenCount, Is.EqualTo(0), "sacar revela lo de debajo (R7)");
            Assert.That(tray.RevealShown, Is.False, "sin ocultas, la ranura de la Luciérnaga se va");
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1), "la Luciérnaga no se cobró");
            AssertWonAndClean(boot);
        }

        [UnityTest]
        public IEnumerator Real323_TheFireflyLightsTheTinaja_ThenTheAStarSolution_ToVictory()
        {
            Profile profile = FunnelSetup.Profile(500);
            profile.Stock.Reveal = 1;
            yield return LoadReal323(profile);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            int tinaja = MechanicTestKit.IndexOf(flow, MechanicType.Tinaja);
            int scoop = MechanicTestKit.IndexOf(flow, MechanicType.Cucharon);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            boot.TapAnywhere();
            boot.TapAnywhere(); // salta Sumergir y teñir
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));

            // 04 §2.5: revela todas las capas de la tinaja como bandas de luz, nunca como agua turbia, y se cobra (tuvo efecto).
            int hidden = flow.Session.Current[tinaja].HiddenMask;
            Assert.That(hidden, Is.Not.EqualTo(0));
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(boot.TapCuia(tinaja).Kind, Is.EqualTo(TapKind.Ignored), "el toque elige la tinaja");
            Assert.That(flow.Session.Current[tinaja].HiddenMask, Is.EqualTo(0), "todas sus capas");
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0), "cobrada");
            CuiaView tv = view.Views[tinaja];
            for (int t = 0; view.RevealAnimating; t += 4)
            {
                Assert.That(t, Is.LessThan(5000), "el revelado termina");
                Assert.That(tv.Murky.IsAnimating, Is.False, "no hay agua turbia que aclarar");
                PlayModeHelpers.TickExact(boot, 4);
            }

            PlayModeHelpers.StepUntilIdle(boot, 4000);
            TinajaOverlay jar = TinajaBoostersTests.JarOf(tv);
            Assert.That(jar.BandMask, Is.EqualTo(hidden), "las capas, como bandas con su icono");
            TinajaVisualTests.AssertNotMurky(tv, flow.Session.Current[tinaja], "tras la Luciérnaga");
            Assert.That(view.Tray.RevealShown, Is.False, "sin ocultas, la ranura se va");

            // Morpho tras la Luciérnaga: una sola realidad, nunca «No veo un vertido seguro» (b.2).
            var morpho = new MorphoBooster(BoosterConfig.Defaults, new AF.Platform.ManualClock(FunnelSetup.T0));
            HintResult lit = morpho.Compute(flow.Session.Current, null, flow.Session.PourMode, true);
            Assert.That(lit.Realities, Is.EqualTo(1));
            Assert.That(lit.Answer, Is.Not.EqualTo(HintAnswer.NoSafePour));
            Assert.That(lit.Tinaja, Is.True);

            int scoops = PlaySolution(boot, tinaja, scoop);
            Assert.That(scoops, Is.EqualTo(3));
            AssertWonAndClean(boot);
        }

        // Juega la solución del A* con TapCuia y deja la vista quieta tras cada vertido. Cada sacada (tinaja → su cuia colgada)
        // con el gesto de TJ3: la tinaja no se inclina ni se mueve, sin pose de vertido en ella, su cuia pasa a la capa de vertido y
        // vuelve con sus gotas; la tinaja nunca se pinta como agua turbia. Devuelve cuántas sacadas hubo.
        private int PlaySolution(GameBootstrap boot, int tinaja, int scoop)
        {
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            SolveResult solution = Solve(flow);
            Assert.That(solution.MoveCount, Is.EqualTo(flow.Level.Par), "el par del paquete");
            CuiaView tv = view.Views[tinaja];
            CuiaView cv = view.Views[scoop];
            TinajaOverlay jar = TinajaBoostersTests.JarOf(tv);
            int scoops = 0;
            bool released = false;
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                BoardState before = flow.Session.Current;
                bool isScoop = before[move.Source].Mechanic == MechanicType.Tinaja;
                if (isScoop)
                {
                    Assert.That(move.Target, Is.EqualTo(scoop), "la tinaja solo vierte en su cuia (M13)");
                    Assert.That(before[scoop].Mechanic, Is.EqualTo(MechanicType.Cucharon), "su cuia, colgada");
                }

                Assert.That(before[move.Target].Mechanic, Is.Not.EqualTo(MechanicType.Tinaja), "la tinaja nunca recibe (M13)");
                int drips = jar.DripsStarted;
                int stateMark = _states.Count;
                TapResult poured = MechanicTestKit.Tap(boot, move);
                Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured), "vertido " + i + " (" + move.Source + " → " + move.Target + ")");
                bool layered = false;
                int t = 0;
                while (!view.IsViewIdle)
                {
                    Assert.That(t, Is.LessThan(8000), "la vista se queda quieta tras el vertido " + i);
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                    t += PlayModeHelpers.FrameMs;
                    if (isScoop)
                    {
                        Assert.That(tv.BodyAngle, Is.EqualTo(0f).Within(0.01f), "sacada " + scoops + ": la tinaja no se inclina (" + t + " ms)");
                        Assert.That(view.Animator.TryGetPourPose(tinaja, out _, out _), Is.False, "sin pose de vertido en la tinaja");
                        layered |= cv.Body.parent != cv.Root;
                    }

                    if (!released)
                    {
                        AssertNoMurkyLook(tv, "vertido " + i + " a los " + t + " ms");
                    }
                }

                BoardState after = flow.Session.Current;
                if (isScoop)
                {
                    scoops++;
                    Assert.That(layered, Is.True, "sacada " + scoops + ": vuela su cuia, no la tinaja");
                    Assert.That(jar.DripsStarted, Is.EqualTo(drips + BoardAnimator.ScoopDripCount), "sacada " + scoops + ": vuelve con sus gotas");
                    // Solo las líneas AF_STATE de este vertido, con el objeto tinaja completo hasta sus dosis.
                    string jarState = "\"tinaja\":{\"cuia\":" + tinaja + ",\"partner\":" + scoop + ",\"doses\":" + after[tinaja].Count + ",";
                    Assert.That(HasStateSince(stateMark, "\"event\":\"pour\"", jarState), Is.True, "AF_STATE tinaja tras la sacada " + scoops);
                    if (after[tinaja].IsEmpty)
                    {
                        // R10 f: la última sacada suelta su cuia, que queda como una Cuia chica normal.
                        Assert.That(after[scoop].Mechanic, Is.EqualTo(MechanicType.None), "su cuia baja del jirau");
                        Assert.That(HasStateSince(stateMark, "\"event\":\"pour\"", jarState, "\"hung\":false"), Is.True, "AF_STATE: suelta");
                        released = true;
                    }
                }

                if (!released)
                {
                    AssertJarClosed(view, tinaja, after[tinaja], "tras el vertido " + i);
                }

                if (after.IsWon())
                {
                    Assert.That(i, Is.EqualTo(solution.MoveCount - 1), "gana con el último vertido");
                }
            }

            Assert.That(released, Is.True, "la tinaja se vacía y su cuia se suelta");
            MechanicTestKit.AssertViewMatchesModel(boot);
            return scoops;
        }

        // La tinaja con sus ocultas cerradas: del barro, mates y sin el «?» ni el pardo; la sonda dice que no hay agua turbia.
        private static void AssertJarClosed(GameScreen view, int tinaja, Cuia model, string when)
        {
            CuiaView tv = view.Views[tinaja];
            TinajaVisualTests.AssertNotMurky(tv, model, when);
            Assert.That(tv.ShownCuia, Is.EqualTo(model), when + ": la vista sigue al modelo");
            Assert.That(DevProbe.HasMurkyWater(GameBootstrap.Current!.Flow!.Session.Current), Is.False, when + ": sin agua turbia en la sonda");
        }

        // Fotograma a fotograma (como TinajaVisualTests con la cola real): ni la insignia «?×n», ni el aspecto turbio, ni el «?», ni
        // el pardo en ninguna capa de la tinaja; el barro de las ocultas se comprueba con la vista quieta (AssertJarClosed).
        private static void AssertNoMurkyLook(CuiaView tv, string when)
        {
            Assert.That(tv.Murky.BadgeVisible, Is.False, when + ": sin «?×n»");
            for (int k = 0; k < tv.Capacity; k++)
            {
                Assert.That(tv.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.None), when + ": capa " + k + " sin aspecto turbio");
                Assert.That(tv.Liquid.MurkyMarkDrawn(k), Is.False, when + ": capa " + k + " sin «?»");
                Assert.That(tv.DoseColor(k), Is.Not.EqualTo(Palette.Murky), when + ": capa " + k + " sin el pardo");
            }
        }

        private void AssertWonAndClean(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            for (int elapsed = 0; elapsed < 15000 && flow.Phase != FlowPhase.VictoryScreen; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "la victoria del 323");
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(flow.Level.Par));
            Assert.That(boot.View!.Animator.LagSnapCount, Is.EqualTo(0), "sin saltos de la cola visual");
            Assert.That(_errors, Is.Empty, "sin excepciones ni errores en el registro: " + string.Join(" | ", _errors));
        }

        private bool HasState(params string[] parts)
        {
            return HasStateSince(0, parts);
        }

        // Como HasState, pero solo en las líneas AF_STATE llegadas desde el índice from (las de un vertido concreto).
        private bool HasStateSince(int from, params string[] parts)
        {
            for (int i = from; i < _states.Count; i++)
            {
                string line = _states[i];
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
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                _errors.Add(type + ": " + condition);
            }
            else if (condition.StartsWith("AF_STATE", StringComparison.Ordinal))
            {
                _states.Add(condition);
            }
        }
    }

    /// <summary>
    /// Capturas del 323 real con la Tinaja, de punta a punta (tarea C1c; <c>[Explicit]</c>, no forman parte de la suite), con el
    /// paquete de la variante «a» y el arte provisional: <c>c1c_01_card_l323</c> (al empezar: la tarjeta del estreno),
    /// <c>c1c_02_start_l323</c> (el tablero tras la intro: la tinaja con la fruta de arriba y sus ocultas del barro),
    /// <c>c1c_03_scoop_l323</c> (al sacar: su cuia en la boca de la tinaja), <c>c1c_04_scooped_l323</c> (tras sacar: la nueva
    /// boca a la vista), <c>c1c_05_released_l323</c> (la tinaja vacía, boca abajo, y su cuia suelta) y
    /// <c>c1c_06_victory_l323</c> (al ganar). Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.Tinaja323EndToEndSnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class Tinaja323EndToEndSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureReal323_StartScoopAndVictory()
        {
            string dir = SnapshotKit.Prepare();
            yield return Tinaja323EndToEndTests.LoadReal323(FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            int tinaja = MechanicTestKit.IndexOf(flow, MechanicType.Tinaja);
            int scoop = MechanicTestKit.IndexOf(flow, MechanicType.Cucharon);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            PlayModeHelpers.Step(boot, 800);
            Save(dir, "c1c_01_card_l323", boot);

            boot.TapAnywhere();
            for (int t = 0; t < 8000 && (flow.Phase != FlowPhase.Playing || !view.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "la intro termina antes de la captura de inicio");
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "c1c_02_start_l323", boot);

            SolveResult solution = Tinaja323EndToEndTests.Solve(flow);
            CuiaView tv = view.Views[tinaja];
            CuiaView cv = view.Views[scoop];
            bool scoopShot = false;
            bool scoopedShot = false;
            bool releasedShot = false;
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                bool isScoop = flow.Session.Current[move.Source].Mechanic == MechanicType.Tinaja;
                Assert.That(MechanicTestKit.Tap(boot, move).Kind, Is.EqualTo(TapKind.Poured));
                if (isScoop && !scoopShot)
                {
                    // Al sacar: su cuia en la boca de la tinaja, con el pie en el eje de la vasija.
                    Vector2 dip = BoardAnimator.ScoopFootFor(tv);
                    for (int t = 0; t < 3000; t += 4)
                    {
                        Vector2 foot = cv.FootPosition + cv.BodyOffset;
                        if (cv.Body.parent != cv.Root && Mathf.Abs(foot.x - dip.x) < 0.05f * tv.Unit && Mathf.Abs(foot.y - dip.y) < 0.3f * tv.Unit)
                        {
                            break;
                        }

                        PlayModeHelpers.TickExact(boot, 4);
                    }

                    PlayModeHelpers.TickExact(boot, 60);
                    Save(dir, "c1c_03_scoop_l323", boot);
                    scoopShot = true;
                }

                PlayModeHelpers.StepUntilIdle(boot, 8000);
                if (isScoop && !scoopedShot)
                {
                    PlayModeHelpers.Step(boot, 300);
                    Save(dir, "c1c_04_scooped_l323", boot);
                    scoopedShot = true;
                }

                if (isScoop && flow.Session.Current[tinaja].IsEmpty && !releasedShot)
                {
                    PlayModeHelpers.Step(boot, 300);
                    Save(dir, "c1c_05_released_l323", boot);
                    releasedShot = true;
                }
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            for (int elapsed = 0; elapsed < 15000 && flow.Phase != FlowPhase.VictoryScreen; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            PlayModeHelpers.Step(boot, 1600);
            Save(dir, "c1c_06_victory_l323", boot);
            Assert.That(scoopShot && scoopedShot && releasedShot, Is.True);
        }

        private static void Save(string dir, string name, GameBootstrap boot)
        {
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, boot);
            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }
    }
}
