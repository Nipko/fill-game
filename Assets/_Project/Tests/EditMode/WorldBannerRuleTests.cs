#nullable enable
using System;
using System.Collections.Generic;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Banner de entrada a un mundo (07 §2.2, regla 1; 02 §6; glosario §8 y §10, <c>world_banner</c>; decisión del dueño del
    /// 2026-10-11): la regla de los niveles (11, 41, 81 y 141 sí; 1, 12 y 42 no), los textos «Mundo {n}: {nombre} · {país} ·
    /// {río}» y el flujo con el catálogo real (<see cref="TestPacks.Campaign"/>): LEVEL_CARD con el banner antes de la intro, al
    /// entrar con Jugar o con Siguiente y solo en el primer intento (ni al reintentar, ni al volver a entrar, ni al reanudar),
    /// el banner antes que la tarjeta previa y el regalo de la Luciérnaga anotado antes de los dos, y apagado por defecto.
    /// </summary>
    public sealed class WorldBannerRuleTests
    {
        [Test]
        public void Levels_FirstOfEachWorldFrom2_Only()
        {
            foreach (int level in new[] { 11, 41, 81, 141 })
            {
                Assert.That(WorldBanners.ShowsOn(level), Is.True, "nivel " + level);
            }

            foreach (int level in new[] { 1, 12, 42, 10, 40, 80, 140, 200, 0, -1, 1001 })
            {
                Assert.That(WorldBanners.ShowsOn(level), Is.False, "nivel " + level);
            }

            Assert.That(WorldBanners.WorldAt(11), Is.EqualTo(2));
            Assert.That(WorldBanners.WorldAt(41), Is.EqualTo(3));
            Assert.That(WorldBanners.WorldAt(81), Is.EqualTo(4));
            Assert.That(WorldBanners.WorldAt(141), Is.EqualTo(5));
            Assert.That(WorldBanners.WorldAt(201), Is.EqualTo(6), "y los mundos que lleguen después, según WorldTable");
            Assert.That(WorldBanners.WorldAt(1), Is.EqualTo(0), "el nivel 1 no lleva banner (decisión del dueño)");
            int count = 0;
            for (int level = 1; level <= WorldTable.LastLevel; level++)
            {
                if (WorldBanners.ShowsOn(level))
                {
                    count++;
                    Assert.That(level, Is.EqualTo(WorldTable.FirstLevel(WorldBanners.WorldAt(level))));

                    // Ningún banner coincide con la tarjeta «¡Miquito a la vista!» (SightingRule: niveles ocupados).
                    Assert.That(SightingRule.IsBusy(level), Is.True, "nivel " + level + " ocupado para la tarjeta del miquito");
                }
            }

            Assert.That(count, Is.EqualTo(WorldTable.WorldCount - 1), "un banner por mundo desde el 2");
        }

        [Test]
        public void Texts_WorldBanner_FromTheGlossary()
        {
            // El país y el río, tal cual en el glosario §8 (el río sin su tipo de agua), como el ejemplo literal de 06 §2.3.
            Assert.That(UiText.WorldBanner(2), Is.EqualTo("Mundo 2: Tapajós · Brasil (Pará) · Tapajós"), "06 §2.3");
            Assert.That(UiText.WorldBanner(3), Is.EqualTo("Mundo 3: Manaos · Brasil (Amazonas) · Negro + Solimões"));
            Assert.That(UiText.WorldBanner(4), Is.EqualTo("Mundo 4: Trapecio · Colombia · Amazonas / Putumayo"));
            Assert.That(UiText.WorldBanner(5), Is.EqualTo("Mundo 5: Loreto · Perú · Marañón-Ucayali"));
            Assert.That(UiText.WorldBanner(12), Does.Contain("vecinos del Orinoco"), "07 §2.2, regla 3");
            for (int w = 1; w <= WorldTable.WorldCount; w++)
            {
                string text = UiText.WorldBanner(w);
                Assert.That(text, Does.StartWith("Mundo " + w + ": " + UiText.WorldName(w) + " · "), "mundo " + w);
                Assert.That(text, Is.EqualTo(UiText.WorldHeading(w) + UiText.WorldSeparator + UiText.WorldPlace(w)));
                Assert.That(UiText.WorldHeading(w), Is.EqualTo(UiText.WorldTitle(w) + ": " + UiText.WorldName(w)));
                Assert.That(UiText.WorldPlace(w), Is.EqualTo(UiText.WorldCountry(w) + " · " + UiText.WorldRiver(w)));
                Assert.That(UiText.WorldPlaceNamed(w), Is.EqualTo(UiText.WorldName(w) + " · " + UiText.WorldPlace(w)));
                Assert.That(UiText.WorldRiver(w), Does.Not.Contain("(blanca").And.Not.Contain("(clara").And.Not.Contain("(negra"), "sin el tipo de agua");
                Assert.That(text.Split(new[] { " · " }, StringSplitOptions.None).Length, Is.EqualTo(3), "{nombre} · {país} · {río}: " + text);
                Assert.That(UiText.WorldBanner(w), Is.SameAs(text), "se arma una vez");
            }

            Assert.That(() => UiText.WorldBanner(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => UiText.WorldBanner(14), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Flow_Level41_BannerBeforeTheIntro_OneTapStartsIt()
        {
            BannerRig rig = new BannerRig();
            rig.Flow.Load(41);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(rig.Flow.WorldBanner, Is.EqualTo(3));
            Assert.That(rig.Flow.LevelCard, Is.Null, "el 41 es normal: sin tarjeta previa");
            Assert.That(rig.Flow.WorldBannerThenCard, Is.False, "al cerrarlo, la intro (AF_STATE world_banner: \"then\":\"intro\")");
            Assert.That(rig.Events, Is.EqualTo(new[] { "loaded:41", "banner:3:41" }), "sin intro hasta cerrar el banner");
            rig.Flow.Tick(10000, true);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "no se cierra solo");
            Assert.That(rig.Flow.AcceptsBoardInput, Is.False);

            // 1 toque en cualquier punto (TapAnywhere, como DismissLevelCard): la intro larga del primer intento.
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(rig.Flow.WorldBanner, Is.EqualTo(0));
            Assert.That(rig.Events[rig.Events.Count - 1], Is.EqualTo("intro:" + GameConfig.TintDurationMs(41)));
            Assert.That(rig.Flow.DismissWorldBanner(), Is.False, "ya no hay banner");
        }

        [Test]
        public void Flow_TapOnACuiaOrTheBackground_ClosesIt_WithoutSelecting()
        {
            BannerRig rig = new BannerRig();
            rig.Flow.Load(11);
            Assert.That(rig.Flow.WorldBanner, Is.EqualTo(2));
            Assert.That(rig.Flow.TapCuia(0).Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(rig.Flow.Selected, Is.EqualTo(TapController.NoSelection));
            rig.Flow.Load(81);
            Assert.That(rig.Flow.WorldBanner, Is.EqualTo(4));
            Assert.That(rig.Flow.TapBackground().Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [Test]
        public void Flow_NoBannerOn1_12_42()
        {
            BannerRig rig = new BannerRig();
            foreach (int level in new[] { 1, 12, 42 })
            {
                rig.Flow.Load(level);
                Assert.That(rig.Flow.WorldBanner, Is.EqualTo(0), "nivel " + level);
                Assert.That(rig.Banners, Is.EqualTo(0), "nivel " + level);
                Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "nivel " + level + ": normal, directo a la intro");
            }
        }

        [Test]
        public void Flow_OnlyOnTheFirstAttempt_NotOnRetryNorReentry()
        {
            BannerRig rig = new BannerRig();
            rig.Flow.Load(41);
            Assert.That(rig.Banners, Is.EqualTo(1));
            StartPlaying(rig.Flow);

            // Volver a la orilla: intro corta, sin banner.
            Pour move = LevelStateTests.Solve(rig.Flow).GetMove(0);
            rig.Flow.TapCuia(move.Source);
            Assert.That(rig.Flow.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(rig.Flow.PressRestart(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(rig.Banners, Is.EqualTo(1));

            // Desde el 31, «Salir» es fallo duro (D-008) y «Reintentar» vuelve con la intro corta: tampoco.
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.ConfirmQuit(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(rig.Flow.PressRetry(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(rig.Banners, Is.EqualTo(1));

            // Volver a entrar en el mismo nivel (Jugar tras salir al inicio): no es el primer intento.
            rig.Flow.Load(41);
            Assert.That(rig.Flow.WorldBanner, Is.EqualTo(0));
            Assert.That(rig.Banners, Is.EqualTo(1));
        }

        [Test]
        public void Flow_NotOnResume_NorAfterExitingAndReopening()
        {
            var store = new MemoryLevelStateStore();
            var economy = new EconomyFixture();
            BannerRig rig = new BannerRig(store: store, economy: economy);
            rig.Flow.Load(41);
            Assert.That(rig.Banners, Is.EqualTo(1));
            Assert.That(store.Load()!.History, Does.StartWith("h2|P|"), "en el banner se guarda como tablero en juego");
            StartPlaying(rig.Flow);
            Pour move = LevelStateTests.Solve(rig.Flow).GetMove(0);
            rig.Flow.TapCuia(move.Source);
            Assert.That(rig.Flow.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
            LevelStateData saved = store.Load()!;

            // 02 §14: al reabrir la app con el nivel en curso, sin intro ni banner.
            BannerRig again = new BannerRig(store: store, economy: economy);
            Assert.That(again.Flow.Resume(saved), Is.True);
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(again.Flow.WorldBanner, Is.EqualTo(0));
            Assert.That(again.Events, Is.EqualTo(new[] { "loaded:41", "resumed:Playing" }));

            // Se sale al inicio y se reabre la app: el nivel recuerda que ya se entró en él, y Jugar no repite el banner.
            again.Flow.LeaveLevel();
            BannerRig later = new BannerRig(store: store, economy: economy);
            var nav = new AppFlow(later.Flow, later.Progress, economy.Wallet, new NullApp());
            nav.Start(false, store.Load());
            Assert.That(nav.Screen, Is.EqualTo(AppScreen.Lobby));
            later.Progress.SaveWin(40, 41);
            Assert.That(nav.PressPlay(), Is.True);
            Assert.That(later.Flow.LevelNumber, Is.EqualTo(41));
            Assert.That(later.Flow.WorldBanner, Is.EqualTo(0));
            Assert.That(later.Banners, Is.EqualTo(0));
        }

        [Test]
        public void Flow_FromNext_AndFromPlay()
        {
            // Tras «Siguiente» desde la victoria del 40, el 41 abre el banner; desde Jugar (AppFlow) también.
            BannerRig rig = new BannerRig();
            rig.Flow.Load(40);
            Assert.That(rig.Flow.WorldBanner, Is.EqualTo(0));
            Win(rig.Flow);
            Assert.That(rig.Flow.Next(), Is.True);
            Assert.That(rig.Flow.LevelNumber, Is.EqualTo(41));
            Assert.That(rig.Flow.WorldBanner, Is.EqualTo(3));

            var economy = new EconomyFixture();
            BannerRig app = new BannerRig(economy: economy);
            app.Progress.SaveWin(80, 81);
            var nav = new AppFlow(app.Flow, app.Progress, economy.Wallet, new NullApp());
            nav.Start(false);
            Assert.That(nav.PressPlay(), Is.True);
            Assert.That(app.Flow.LevelNumber, Is.EqualTo(81));
            Assert.That(app.Flow.WorldBanner, Is.EqualTo(4));
        }

        [Test]
        public void Flow_BannerFirst_ThenTheLevelCard_ThenTheIntro()
        {
            // Con el catálogo desplazado 1 (af_level_offset), el 41 se juega con el tablero del 40, Difícil: primero el banner,
            // después la tarjeta previa y, al cerrarla, la intro.
            BannerRig rig = new BannerRig(offset: 1);
            rig.Flow.Load(41);
            Assert.That(rig.Flow.Level.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard));
            Assert.That(rig.Flow.WorldBanner, Is.EqualTo(3));
            Assert.That(rig.Flow.LevelCard, Is.Null);
            Assert.That(rig.Flow.WorldBannerThenCard, Is.True, "al cerrarlo, la tarjeta previa (\"then\":\"level_card\")");
            Assert.That(rig.Flow.DismissLevelCard(), Is.True, "DismissLevelCard cierra antes el banner");
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(rig.Flow.WorldBanner, Is.EqualTo(0));
            Assert.That(rig.Flow.WorldBannerThenCard, Is.False, "ya no hay banner");
            Assert.That(rig.Flow.LevelCard!.Kind, Is.EqualTo(LevelCardKind.Difficulty));
            Assert.That(rig.Flow.DismissWorldBanner(), Is.False, "con la tarjeta abierta no hay banner que cerrar");
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(rig.Events, Is.EqualTo(new[] { "loaded:41", "banner:3:41", "card:Difficulty", "intro:" + GameConfig.TintDurationMs(41) }));
        }

        [Test]
        public void Flow_TheGiftIsGrantedOnLoad_BannerThenCombinedCard()
        {
            // Con el catálogo desplazado 41, el 141 (mundo 5) se juega con el tablero del 100: la Luciérnaga se regala al cargar
            // (anotada en level_state), el banner va primero y la tarjeta combinada, con «+1 Luciérnaga», al cerrarlo.
            BannerRig rig = new BannerRig(offset: 41);
            rig.Flow.Load(141);
            Assert.That(rig.Flow.Level.IntroMechanic, Is.EqualTo(LevelFlow.MurkyIntroMechanic));
            Assert.That(rig.Events, Is.EqualTo(new[] { "loaded:141", "reveal_gift:1", "banner:5:141" }));
            Assert.That(rig.Flow.Attempt.RevealGifted, Is.True);
            Assert.That(rig.Flow.WorldBannerThenCard, Is.True, "la tarjeta combinada, con el regalo, va después");
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.LevelCard!.Kind, Is.EqualTo(LevelCardKind.Combined));
            Assert.That(rig.Flow.LevelCard.RevealGifted, Is.True, "el regalo sigue en la tarjeta que va después del banner");
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [Test]
        public void Flow_DisabledByDefault()
        {
            var listener = new BannerListener();
            var flow = new LevelFlow(LevelCatalog.Create(TestPacks.Campaign), new MemoryProgressStore(), listener);
            Assert.That(flow.WorldBannersEnabled, Is.False);
            flow.Load(41);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(flow.WorldBanner, Is.EqualTo(0));
            Assert.That(listener.Banners, Is.EqualTo(0));

            // Sin oyente que lo pinte, el flujo funciona igual (LEVEL_CARD y un toque).
            var plain = new LevelFlow(LevelCatalog.Create(TestPacks.Campaign), new MemoryProgressStore(), new RecordingListener()) { WorldBannersEnabled = true };
            plain.Load(41);
            Assert.That(plain.WorldBanner, Is.EqualTo(3));
            Assert.That(plain.TapAnywhere(), Is.True);
            Assert.That(plain.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        private static void StartPlaying(LevelFlow flow)
        {
            for (int i = 0; i < 3 && flow.Phase != FlowPhase.Playing; i++)
            {
                Assert.That(flow.TapAnywhere(), Is.True);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        // Juega la solución del solver y avanza hasta VICTORY_SCREEN.
        private static void Win(LevelFlow flow)
        {
            StartPlaying(flow);
            SolveResult result = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(result.IsSolved, Is.True);
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour move = result.GetMove(i);
                flow.TapCuia(move.Source);
                flow.TapCuia(move.Target);
            }

            flow.Tick(16, true);
            flow.TapAnywhere();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
        }

        /// <summary>Flujo con el catálogo real, cartera, tarjetas y banner encendidos y el oyente que anota el banner.</summary>
        private sealed class BannerRig
        {
            public BannerRig(int offset = 0, MemoryLevelStateStore? store = null, EconomyFixture? economy = null)
            {
                Listener = new BannerListener();
                Progress = new MemoryProgressStore();
                Economy = economy ?? new EconomyFixture();
                Flow = new LevelFlow(LevelCatalog.Create(TestPacks.Campaign, offset), Progress, Listener, GameConfig.TapSwitchSelection, PourModes.Default, Economy.Wallet)
                {
                    StateStore = store,
                    LevelCardsEnabled = true,
                    WorldBannersEnabled = true,
                };
            }

            public BannerListener Listener { get; }

            public MemoryProgressStore Progress { get; }

            public EconomyFixture Economy { get; }

            public LevelFlow Flow { get; }

            public List<string> Events => Listener.Events;

            public int Banners => Listener.Banners;
        }

        /// <summary>Oyente que anota en orden los eventos que cuentan aquí (carga, regalo, banner, tarjeta, intro y reanudar).</summary>
        private sealed class BannerListener : ILevelFlowListener, ILevelCardListener, IRevealListener, IWorldBannerListener
        {
            public readonly List<string> Events = new List<string>();

            public int Banners { get; private set; }

            public void OnWorldBanner(int world, int level)
            {
                Banners++;
                Events.Add("banner:" + world + ":" + level);
            }

            public void OnLevelCard(LevelCardInfo card)
            {
                Events.Add("card:" + card.Kind);
            }

            public void OnRevealGifted(int units)
            {
                Events.Add("reveal_gift:" + units);
            }

            public void OnRevealSelection(bool selecting)
            {
            }

            public void OnRevealed(int cuia, int layerMask)
            {
            }

            public void OnLevelLoaded(int level)
            {
                Events.Add("loaded:" + level);
            }

            public void OnIntroStarted(int durationMs, bool isShort)
            {
                Events.Add("intro:" + durationMs + (isShort ? ":short" : string.Empty));
            }

            public void OnIntroFinished(bool skipped)
            {
            }

            public void OnTap(TapResult result)
            {
            }

            public void OnUndo(Pour undone, bool fromStuckModal)
            {
            }

            public void OnRestart(bool fromStuckModal)
            {
            }

            public void OnWin()
            {
            }

            public void OnStuck()
            {
            }

            public void OnCelebrationStarted()
            {
            }

            public void OnVictoryScreen()
            {
            }

            public void OnStuckModalOpened()
            {
            }

            public void OnCuiaAdded(int index, AddedCuiaKind kind)
            {
            }

            public void OnExtraCuiaGifted(int units)
            {
                Events.Add("gift:" + units);
            }

            public void OnFunnelPhase(FlowPhase phase)
            {
            }

            public void OnFailResolved(FailOutcome outcome)
            {
            }

            public void OnRiverLowered(string reliefType, int bannerMs)
            {
            }

            public void OnHarvestDoubled(int totalSeeds)
            {
            }

            public void OnAdGate()
            {
            }

            public void OnResumed(FlowPhase phase)
            {
                Events.Add("resumed:" + phase);
            }
        }

        private sealed class NullApp : IAppFlowListener
        {
            public void OnAppScreenChanged(AppScreen screen)
            {
            }
        }
    }
}
