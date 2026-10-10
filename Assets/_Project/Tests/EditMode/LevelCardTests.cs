#nullable enable
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Tarjeta previa de 1 toque (tarea T10 del paso 1; 02 §10; propuesta de D-009 §c.1; 04 §2.5): LOADING → LEVEL_CARD →
    /// INTRO en los niveles Difícil y Muy difícil y en los estrenos, con el premio y la cinta de la Cosecha (60/×3 y 80/×4) y la
    /// Luciérnaga regalada del 100 en la tarjeta combinada. Apagada por defecto (<see cref="LevelFlow.LevelCardsEnabled"/>);
    /// no sale al reanudar (02 §14) ni al reintentar [P]. Con el catálogo real (<see cref="TestPacks.Campaign"/>, 1-200).
    /// </summary>
    public sealed class LevelCardTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [Test]
        public void Card_Level40_Hard60x3()
        {
            // 03 §11: el 40 es el primer Difícil. Premio 60 = eco_reward_hard, cinta ×3 sobre los 20 de un nivel normal.
            LevelStateTests.Rig rig = CardRig();
            LevelCardInfo card = LoadCard(rig, 40);
            Assert.That(card.Kind, Is.EqualTo(LevelCardKind.Difficulty));
            Assert.That(card.Level, Is.EqualTo(40));
            Assert.That(card.Label, Is.EqualTo(LevelDefinition.LabelHard));
            Assert.That(card.Tier, Is.EqualTo(RewardTier.Hard));
            Assert.That(card.RewardSeeds, Is.EqualTo(60));
            Assert.That(card.RewardSeeds, Is.EqualTo(rig.Economy.Wallet.Config.WinReward(RewardTier.Hard)), "la misma fuente que la Cosecha");
            Assert.That(card.RibbonMultiplier, Is.EqualTo(3));
            Assert.That(card.IntroMechanic, Is.Null);
            Assert.That(card.IsMurky, Is.False);
            Assert.That(card.RevealGifted, Is.False);

            // Iconos [P] de §c.1: el 40 trae una Cuia chica (2) y una alta (5), sin mecánicas ni Barro.
            Assert.That(card.HasSmallCuia, Is.True);
            Assert.That(card.HasTallCuia, Is.True);
            Assert.That(card.HasMud, Is.False);
            Assert.That(card.MechanicMask, Is.EqualTo(0));
            Assert.That(rig.Listener.Events, Is.EqualTo(new[] { "loaded:40", "card:Difficulty" }), "la intro espera a la tarjeta");
            Assert.That(FlowPhaseNames.Of(FlowPhase.LevelCard), Is.EqualTo("LEVEL_CARD"));
        }

        [Test]
        public void Card_Level60_VeryHard80x4()
        {
            // 03 §12: el 60 es el primer Muy difícil: premio 80 (eco_reward_very_hard) y cinta ×4. Lleva una Hoja y una Cuia chica.
            LevelStateTests.Rig rig = CardRig();
            LevelCardInfo card = LoadCard(rig, 60);
            Assert.That(card.Kind, Is.EqualTo(LevelCardKind.Difficulty));
            Assert.That(card.Label, Is.EqualTo(LevelDefinition.LabelVeryHard));
            Assert.That(card.Tier, Is.EqualTo(RewardTier.VeryHard));
            Assert.That(card.RewardSeeds, Is.EqualTo(80));
            Assert.That(card.RibbonMultiplier, Is.EqualTo(4));
            Assert.That(card.HasMechanic(MechanicType.Hoja), Is.True);
            Assert.That(card.HasMechanic(MechanicType.Liana), Is.False);
            Assert.That(card.HasMechanic(MechanicType.None), Is.False);
            Assert.That(card.HasSmallCuia, Is.True);
            Assert.That(card.HasTallCuia, Is.False);
        }

        [Test]
        public void Card_Level43_DebutHoja()
        {
            // D-007: el 43 estrena la Hoja en un nivel normal; el tutorial hace de tarjeta (§c.1), sin cinta.
            LevelStateTests.Rig rig = CardRig();
            LevelCardInfo card = LoadCard(rig, 43);
            Assert.That(card.Kind, Is.EqualTo(LevelCardKind.Debut));
            Assert.That(card.IntroMechanic, Is.EqualTo("hoja"));
            Assert.That(card.Label, Is.EqualTo(LevelDefinition.LabelNormal));
            Assert.That(card.RewardSeeds, Is.EqualTo(20));
            Assert.That(card.RibbonMultiplier, Is.EqualTo(1), "normal: sin cinta");
            Assert.That(card.HasMechanic(MechanicType.Hoja), Is.True);
            for (int t = (int)MechanicType.Brote; t <= (int)MechanicTypes.Last; t++)
            {
                Assert.That(card.HasMechanic((MechanicType)t), Is.False, ((MechanicType)t).ToString());
            }
        }

        [Test]
        public void Card_Level100_Combined()
        {
            // D-007 y 04 §2.5: el 100 es Muy difícil, estrena Aguas turbias y regala 1 Luciérnaga, todo en una tarjeta
            // (pregunta 1 del plan, recomendación por defecto). El regalo llega antes que la tarjeta.
            LevelStateTests.Rig rig = CardRig();
            LevelCardInfo card = LoadCard(rig, 100);
            Assert.That(card.Kind, Is.EqualTo(LevelCardKind.Combined));
            Assert.That(card.Label, Is.EqualTo(LevelDefinition.LabelVeryHard));
            Assert.That(card.RewardSeeds, Is.EqualTo(80));
            Assert.That(card.RibbonMultiplier, Is.EqualTo(4));
            Assert.That(card.IntroMechanic, Is.EqualTo(LevelFlow.MurkyIntroMechanic));
            Assert.That(card.IsMurky, Is.True, "chip «?» (07 §7.7)");
            Assert.That(card.RevealGifted, Is.True, "«+1 Luciérnaga»");
            Assert.That(rig.Listener.Events, Is.EqualTo(new[] { "loaded:100", "reveal_gift:1", "card:Combined" }));
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1));
        }

        [Test]
        public void Card_Level41_None()
        {
            // Normal y sin estreno: ninguna tarjeta (§c.1), directo a la intro.
            LevelStateTests.Rig rig = CardRig();
            rig.Flow.Load(41);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(rig.Flow.LevelCard, Is.Null);
            Assert.That(rig.Listener.Cards, Is.Empty);
            Assert.That(rig.Listener.Events, Is.EqualTo(new[] { "loaded:41", "intro:" + GameConfig.AnimTintDurationMsLate }));
        }

        [Test]
        public void Card_DisabledByDefault()
        {
            // LevelFlow.LevelCardsEnabled es false por defecto: los tests del flujo (VictoryTests, AppFlowTests…) no ven tarjetas.
            var listener = new RecordingListener();
            var flow = new LevelFlow(LevelCatalog.Create(TestPacks.Campaign), new MemoryProgressStore(), listener);
            Assert.That(flow.LevelCardsEnabled, Is.False);
            Assert.That(flow.LevelCardOnRetry, Is.EqualTo(GameConfig.LevelCardOnRetry));
            flow.Load(40);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro), "el 40 va directo a la intro");
            Assert.That(flow.LevelCard, Is.Null);
            Assert.That(listener.Cards, Is.Empty);
            Assert.That(flow.DismissLevelCard(), Is.False);
            Assert.That(flow.TapAnywhere(), Is.True, "el toque salta la intro, como siempre");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));

            // En el juego, GameBootstrap usa LaunchOptions.LevelCardsOverride ?? GameConfig.LevelCardsEnabledDefault.
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.ResolveLevelCards(), Is.EqualTo(GameConfig.LevelCardsEnabledDefault));
        }

        [Test]
        public void Card_NotOnResume()
        {
            // 02 §14: al reabrir la app, el nivel vuelve sin intro y sin tarjeta.
            LevelStateTests.Rig rig = CardRig();
            LoadCard(rig, 40);
            Assert.That(rig.Store.Load()!.History, Does.StartWith("h2|P|"), "en LEVEL_CARD se guarda como tablero en juego");
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Pour move = Solve(rig.Flow).GetMove(0);
            rig.Flow.TapCuia(move.Source);
            Assert.That(rig.Flow.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
            LevelStateData saved = rig.Store.Load()!;

            LevelStateTests.Rig again = rig.Reopen();
            again.Flow.LevelCardsEnabled = true;
            Assert.That(again.Flow.Resume(saved), Is.True);
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(again.Flow.LevelCard, Is.Null);
            Assert.That(again.Listener.Events, Is.EqualTo(new[] { "loaded:40", "resumed:Playing" }));

            // También con AppFlow, como al abrir la app.
            LevelStateTests.Rig app = rig.Reopen();
            app.Flow.LevelCardsEnabled = true;
            var nav = new AppFlow(app.Flow, app.Progress, app.Economy.Wallet, new NullApp());
            nav.Start(false, saved);
            Assert.That(nav.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(app.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(app.Listener.Cards, Is.Empty);
        }

        [Test]
        public void Card_NotOnRetry()
        {
            // [P] Solo al entrar con Jugar o Siguiente: ni Volver a la orilla ni «Reintentar» de Nivel fallido (D-008) la repiten.
            LevelStateTests.Rig rig = CardRig();
            LoadCard(rig, 40);
            StartPlaying(rig.Flow);
            Assert.That(rig.Flow.PressRestart(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "Volver a la orilla: intro corta, sin tarjeta");
            Assert.That(rig.Listener.Events[rig.Listener.Events.Count - 1], Is.EqualTo("intro:" + GameConfig.AnimTintDurationMsLate + ":short"));
            Assert.That(rig.Flow.TapAnywhere(), Is.True);

            // Desde el 31, «Salir» es fallo duro y lleva a Nivel fallido; «Reintentar» vuelve con la intro corta.
            Assert.That(rig.Flow.ConfirmQuit(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(rig.Flow.PressRetry(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(rig.Listener.Cards.Count, Is.EqualTo(1), "solo la de la entrada");
            Assert.That(rig.Flow.LevelCard, Is.Null);

            // Es un booleano [P]: con LevelCardOnRetry, el reintento también la enseña y la intro corta espera a cerrarla.
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            rig.Flow.LevelCardOnRetry = true;
            Assert.That(rig.Flow.PressRestart(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(rig.Listener.Cards.Count, Is.EqualTo(2));
            Assert.That(rig.Flow.LevelCard!.Kind, Is.EqualTo(LevelCardKind.Difficulty));
            Assert.That(rig.Flow.LevelCard.RevealGifted, Is.False);
            Assert.That(rig.Flow.DismissLevelCard(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(rig.Listener.Events[rig.Listener.Events.Count - 1], Is.EqualTo("intro:" + GameConfig.AnimTintDurationMsLate + ":short"));
        }

        [Test]
        public void Dismiss_StartsIntro()
        {
            // 1 toque: DismissLevelCard, TapAnywhere, TapCuia o el fondo la cierran y empieza la intro, que luego llega a PLAYING.
            LevelStateTests.Rig rig = CardRig();
            LoadCard(rig, 40);
            Assert.That(rig.Flow.AcceptsBoardInput, Is.False, "en la tarjeta el tablero no acepta toques");
            Assert.That(rig.Flow.CanRestart, Is.False);
            rig.Flow.Tick(10000, true);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "la tarjeta no se cierra sola");
            Assert.That(rig.Flow.DismissLevelCard(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(rig.Flow.LevelCard, Is.Null);
            Assert.That(rig.Listener.Events, Is.EqualTo(new[] { "loaded:40", "card:Difficulty", "intro:" + GameConfig.AnimTintDurationMsLate }));
            Assert.That(rig.Flow.DismissLevelCard(), Is.False, "ya no hay tarjeta");
            for (int i = 0; i < 100 && rig.Flow.Phase != FlowPhase.Playing; i++)
            {
                rig.Flow.Tick(16, true);
            }

            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Playing));

            // TapAnywhere: el primer toque cierra la tarjeta, el segundo salta la intro.
            LevelStateTests.Rig any = CardRig();
            LoadCard(any, 60);
            Assert.That(any.Flow.TapAnywhere(), Is.True);
            Assert.That(any.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(any.Flow.TapAnywhere(), Is.True);
            Assert.That(any.Flow.Phase, Is.EqualTo(FlowPhase.Playing));

            // TapCuia: cierra la tarjeta sin seleccionar nada (no llega a la vista como toque).
            LevelStateTests.Rig cuia = CardRig();
            LoadCard(cuia, 43);
            TapResult tap = cuia.Flow.TapCuia(0);
            Assert.That(tap.Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(cuia.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(cuia.Flow.Selected, Is.EqualTo(-1));
            Assert.That(cuia.Listener.Taps, Is.Empty);

            // El fondo, igual.
            LevelStateTests.Rig background = CardRig();
            LoadCard(background, 60);
            Assert.That(background.Flow.TapBackground().Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(background.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(background.Listener.Taps, Is.Empty);
        }

        [Test]
        public void Card_Campaign_KindsFollowLabelsAndDebuts()
        {
            // 02 §10 y §c.1 en 1-200: tarjeta en cada Difícil, Muy difícil y estreno (D-007: 11 estrenos), combinada solo en el 100.
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            EconomyConfig economy = EconomyConfig.Defaults;
            int debuts = 0;
            int combined = 0;
            for (int level = 1; level <= 200; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                bool labeled = def.DifficultyLabel != LevelDefinition.LabelNormal;
                bool debut = def.IntroMechanic != null;
                LevelCardInfo? card = LevelCardInfo.For(def, level, economy, def.CreateBoard().HasHidden, false);
                Assert.That(card != null, Is.EqualTo(labeled || debut), def.Id);
                Assert.That(LevelCardInfo.Shows(def), Is.EqualTo(labeled || debut), def.Id);
                if (card == null)
                {
                    continue;
                }

                Assert.That(card.Kind, Is.EqualTo(labeled && debut ? LevelCardKind.Combined : labeled ? LevelCardKind.Difficulty : LevelCardKind.Debut), def.Id);
                Assert.That(card.RewardSeeds, Is.EqualTo(def.DifficultyLabel == LevelDefinition.LabelVeryHard ? 80 : labeled ? 60 : 20), def.Id);
                Assert.That(card.HasSmallCuia, Is.EqualTo(def.HasSmallCuia), def.Id);
                Assert.That(card.HasTallCuia, Is.EqualTo(def.HasTallCuia), def.Id);
                Assert.That(card.HasMud, Is.EqualTo(def.HasMud), def.Id);
                Assert.That(card.IsMurky, Is.EqualTo(def.IsMurky), def.Id);
                debuts += debut ? 1 : 0;
                combined += card.Kind == LevelCardKind.Combined ? 1 : 0;
                if (card.Kind == LevelCardKind.Combined)
                {
                    Assert.That(level, Is.EqualTo(100));
                }
            }

            Assert.That(debuts, Is.EqualTo(11), "chica 14, alta 27, hoja 43, matapi 63, liana 83, murky 100, remolino 103, barro 123, lluvia 143, brote 163 y pirana 183");
            Assert.That(combined, Is.EqualTo(1));
        }

        [Test]
        public void Ribbon_OnlyWhenTheRewardIsAnExactMultiple()
        {
            // La cinta anuncia lo que se paga (02 §10; §c.2): con premios que no son múltiplos exactos, sin cinta.
            Assert.That(LevelCardInfo.RibbonFor(EconomyConfig.Defaults, RewardTier.Normal), Is.EqualTo(1));
            Assert.That(LevelCardInfo.RibbonFor(EconomyConfig.Defaults, RewardTier.Hard), Is.EqualTo(3));
            Assert.That(LevelCardInfo.RibbonFor(EconomyConfig.Defaults, RewardTier.VeryHard), Is.EqualTo(4));
            var odd = new EconomyConfig { EcoRewardNormal = 25, EcoRewardHard = 60, EcoRewardVeryHard = 100 };
            Assert.That(LevelCardInfo.RibbonFor(odd, RewardTier.Hard), Is.EqualTo(1), "60/25 no es entero");
            Assert.That(LevelCardInfo.RibbonFor(odd, RewardTier.VeryHard), Is.EqualTo(4));
        }

        // Rig del catálogo real con las tarjetas encendidas (y cartera, como en el juego).
        private static LevelStateTests.Rig CardRig()
        {
            var rig = new LevelStateTests.Rig(packs: TestPacks.Campaign);
            rig.Flow.LevelCardsEnabled = true;
            return rig;
        }

        private static LevelCardInfo LoadCard(LevelStateTests.Rig rig, int level)
        {
            int cards = rig.Listener.Cards.Count;
            int events = rig.Listener.Events.Count;
            rig.Flow.Load(level);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "nivel " + level);
            Assert.That(rig.Listener.Cards.Count, Is.EqualTo(cards + 1));
            Assert.That(rig.Flow.LevelCard, Is.SameAs(rig.Listener.Cards[cards]));
            for (int i = events; i < rig.Listener.Events.Count; i++)
            {
                Assert.That(rig.Listener.Events[i], Does.Not.StartWith("intro:"), "sin intro hasta cerrar la tarjeta");
            }

            return rig.Flow.LevelCard!;
        }

        private static void StartPlaying(LevelFlow flow)
        {
            Assert.That(flow.TapAnywhere(), Is.True);
            Assert.That(flow.TapAnywhere(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        private static SolveResult Solve(LevelFlow flow)
        {
            SolveResult result = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(result.IsSolved, Is.True);
            return result;
        }

        private sealed class NullApp : IAppFlowListener
        {
            public void OnAppScreenChanged(AppScreen screen)
            {
            }
        }
    }
}
