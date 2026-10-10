#nullable enable
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Las capas de Aguas turbias ya reveladas no vuelven a ocultarse (tarea T06 del paso 1; 02 R7, R8, §11 T6 y §14; 04 §2.1,
    /// §2.2, §2.5 d y §10.1). Con el catálogo real (<see cref="TestPacks.CampaignPaths"/>), reanudar el nivel, añadir la Cuia
    /// extra y la Cuia de rescate (comprada o con la Balsa) conservan la capa revelada del nivel 100 aunque el historial
    /// rehecho no la revele (Marea deshizo el vertido que la reveló); si el historial no se puede rehacer igual con la cuia
    /// añadida, no se cobra ni se añade nada y queda <see cref="LevelFlow.LastFunnelError"/>; y una sección R que no es de este
    /// tablero descarta el estado sin excepciones. Las capas se restauran al final del rehacer, que es exacto en 1-200 porque
    /// ningún nivel lleva Barro en una cuia turbia (<see cref="Campaign_NoMudInMurkyCuias_SoRestoringAtTheEndIsExact"/>).
    /// </summary>
    public sealed class MurkyPersistenceTests
    {
        // Nivel 100 (m04_v1_n0100, estreno de Aguas turbias): cuia 0 = [10*, 8*, 6] y cuia 1 = [6] (02 §11: 1 cuia, 2 ocultas).
        private const int Level100 = 100;
        private const int MurkyCuia = 0;

        [Test]
        public void Resume_KeepsPartialReveal_Level100()
        {
            var rig = new LevelStateTests.Rig(packs: TestPacks.Campaign);
            rig.Start(Level100);
            RevealLayer1ThenUndo(rig.Flow);
            BoardState board = rig.Flow.Session.Current;
            LevelStateData saved = rig.Store.Load()!;
            Assert.That(saved.History, Is.EqualTo("h2|P||||002"), "R: la capa 1 (0b10) de la cuia 0; V vacía tras Marea");
            Assert.That(saved.RevealedCuias, Is.EqualTo(new[] { MurkyCuia }), "revealed_cuias sigue igual, por compatibilidad");

            LevelStateTests.Rig again = rig.Reopen();
            Assert.That(again.Flow.CheckResume(saved), Is.EqualTo(ResumeCheck.Resumable));
            Assert.That(again.Flow.Resume(saved), Is.True);
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "sin intro (02 §14)");
            Assert.That(again.Flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0b10), "02 T6: lo revelado sigue revelado");
            Assert.That(again.Flow.Session.Current, Is.EqualTo(board), "el mismo Current: la capa 1 a la vista y la 0 oculta");
            Assert.That(again.Flow.Session.HiddenCount, Is.EqualTo(1));

            // Lo conserva la máscara guardada: sin la sección R (lo que escribía h1), la capa volvía a ocultarse.
            LevelStateData legacy = LevelStateSnapshot.Copy(saved);
            legacy.History = "h1|P|||";
            LevelStateTests.Rig old = rig.Reopen();
            Assert.That(old.Flow.Resume(legacy), Is.True);
            Assert.That(old.Flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0));
            Assert.That(old.Flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0b11));

            // Volver a la orilla tampoco la oculta (04 §2.7, I5), y se vuelve a guardar.
            Assert.That(again.Flow.Restart(), Is.True);
            Assert.That(again.Flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0b01));
            Assert.That(again.Store.Load()!.History, Is.EqualTo("h2|P||||002"));
        }

        [Test]
        public void ExtraCuia_KeepsReveal()
        {
            var rig = new LevelStateTests.Rig(seeds: 150, packs: TestPacks.Campaign);
            rig.Start(Level100);
            RevealLayer1ThenUndo(rig.Flow);
            int cuias = rig.Flow.Session.Current.Count;
            Assert.That(rig.Flow.ExtraCuia.State, Is.EqualTo(ExtraCuiaState.Ready));

            TransactionResult? paid = rig.Flow.UseExtraCuia();
            Assert.That(paid!.IsApplied, Is.True);
            Assert.That(rig.Flow.LastFunnelError, Is.Null);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(0), "150 semillas");
            Assert.That(rig.Flow.Session.Current.Count, Is.EqualTo(cuias + 1));
            Assert.That(rig.Flow.Session.Current[cuias].IsEmpty, Is.True);
            Assert.That(rig.Listener.Saw("cuia:" + cuias + ":Extra"), Is.True);
            Assert.That(rig.Flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0b10), "02 T6: rehacer la sesión no oculta la capa");
            Assert.That(rig.Flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0b01));
            Assert.That(rig.Store.Load()!.History, Is.EqualTo("h2|P|E|||002"));

            // Al reabrir la app siguen la cuia y la capa.
            LevelStateTests.Rig again = rig.Reopen();
            Assert.That(again.Flow.Resume(rig.Store.Load()!), Is.True);
            Assert.That(again.Flow.Session.Current, Is.EqualTo(rig.Flow.Session.Current));
            Assert.That(again.Flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0b10));
        }

        [Test]
        public void Rescue_KeepsReveal()
        {
            var rig = new LevelStateTests.Rig(seeds: 1000, packs: TestPacks.Campaign);
            rig.Start(Level100);
            RevealLayer1ThenUndo(rig.Flow);
            GetStuck(rig.Flow);
            Assert.That(rig.Flow.RescueOffered, Is.True, "04 §6.4: el tablero + 1 cuia se resuelve");
            int cuias = rig.Flow.Session.Current.Count;
            int price = rig.Flow.FrozenRescueQuote!.Value.Price;

            TransactionResult? paid = rig.Flow.BuyRescue();
            Assert.That(paid!.IsApplied, Is.True);
            Assert.That(rig.Flow.LastFunnelError, Is.Null);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(1000 - price));
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(rig.Flow.Session.Current.Count, Is.EqualTo(cuias + 1));
            Assert.That(rig.Flow.Session.AppliedPours, Is.EqualTo(3), "el historial se conserva (04 §2.1 caso d)");
            Assert.That(rig.Flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0b10), "02 T6: la Cuia de rescate no oculta la capa");
            Assert.That(rig.Flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0b01));
            Assert.That(rig.Store.Load()!.History, Is.EqualTo("h2|P|R||102132|002"));

            LevelStateTests.Rig again = rig.Reopen();
            Assert.That(again.Flow.Resume(rig.Store.Load()!), Is.True);
            Assert.That(again.Flow.Session.Current, Is.EqualTo(rig.Flow.Session.Current));
            Assert.That(again.Flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0b10));

            // Con la Balsa de rescate comprada (sin semillas, 04 §6.2 BALSA_499), igual.
            var balsa = new LevelStateTests.Rig(seeds: 0, packs: TestPacks.Campaign);
            balsa.Start(Level100);
            RevealLayer1ThenUndo(balsa.Flow);
            GetStuck(balsa.Flow);
            Assert.That(balsa.Flow.ApplyPaidRescue(), Is.True);
            Assert.That(balsa.Flow.LastFunnelError, Is.Null);
            Assert.That(balsa.Flow.Session.Current.Count, Is.EqualTo(cuias + 1));
            Assert.That(balsa.Flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0b10));
            Assert.That(balsa.Flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0b01));
        }

        [Test]
        public void ReplayFailure_DoesNotCharge_SetsLastFunnelError()
        {
            // El único caso en que rehacer el historial con la cuia añadida no da lo mismo: Barro sobre una oculta, lavado con
            // su mismo color y revelado a mitad del historial con la Luciérnaga (LevelSession.Reveal; el caso teórico de
            // RestoreRevealed en el núcleo). Restaurada al final, la racha (R6) del vertido siguiente sale de 1 dosis y no de 2.
            // Ningún nivel de 1-200 lo combina, así que va con un paquete sintético: 31 (tablero A) y 32 (tablero B).
            var rig = new LevelStateTests.Rig(offset: 30, seeds: 1000, packs: new[] { MudOverMurkyPack() });

            // A, en juego: Cuia extra, con semillas y con stock (04 §2.5 d: nunca se cobra sin entregar).
            rig.Start(31);
            Play(rig.Flow, 0, 1, 1); // el 2 sella la cuia 1 y deja el Barro arriba
            Play(rig.Flow, 2, 0, 1); // el 3 lava el Barro: [3*, 3]
            Assert.That(rig.Flow.Session.Reveal(0), Is.EqualTo(1), "la Luciérnaga [P] revela la oculta");
            Play(rig.Flow, 0, 3, 2); // con la oculta a la vista, la racha es de 2
            Assert.That(rig.Flow.ExtraCuia.CanUse, Is.True);
            AssertNothingChanges(rig, () => rig.Flow.UseExtraCuia() == null);
            Assert.That(rig.Flow.ExtraCuia.State, Is.EqualTo(ExtraCuiaState.Ready), "la cuia no cuenta como usada");

            rig.Economy.Profile.Stock.ExtraCuia = 1;
            AssertNothingChanges(rig, () => rig.Flow.UseExtraCuia() == null);
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(1), "el stock tampoco se gasta");

            // Otra acción del embudo borra el error.
            Assert.That(rig.Flow.GrantUndoRefill(), Is.True);
            Assert.That(rig.Flow.LastFunnelError, Is.Null);

            // B, en Aguas estancadas: Cuia de rescate comprada y Balsa.
            rig.Start(32);
            Play(rig.Flow, 1, 0, 1); // el 3 lava el Barro: [4, 3*, 3]
            Assert.That(rig.Flow.Session.Reveal(0), Is.EqualTo(1));
            Play(rig.Flow, 0, 2, 2); // sella el 3 y deja Aguas estancadas
            OpenStuckModal(rig.Flow);
            Assert.That(rig.Flow.RescueOffered, Is.True);
            AssertNothingChanges(rig, () => rig.Flow.BuyRescue() == null);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal), "el modal sigue abierto");
            Assert.That(rig.Flow.Attempt.Rescues, Is.EqualTo(0));
            Assert.That(rig.Flow.RescueOffered, Is.True);
            AssertNothingChanges(rig, () => !rig.Flow.ApplyPaidRescue());
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));

            Assert.That(rig.Flow.PressGiveUp(), Is.True);
            Assert.That(rig.Flow.LastFunnelError, Is.Null);
        }

        [Test]
        public void InvalidMask_Discards()
        {
            var rig = new LevelStateTests.Rig(packs: TestPacks.Campaign);
            rig.Start(Level100);
            RevealLayer1ThenUndo(rig.Flow);
            LevelStateData saved = rig.Store.Load()!;

            // El formato vale, pero no son capas ocultas de este tablero: la cima (capa 2), una capa sin dosis (3), una cuia sin
            // ocultas (1) o una que no existe (10 y 15; el nivel 100 tiene 10 cuias).
            string[] wrong = { "h2|P||||004", "h2|P||||00c", "h2|P||||102", "h2|P||||a01", "h2|P||||f01" };
            for (int i = 0; i < wrong.Length; i++)
            {
                LevelStateData bad = LevelStateSnapshot.Copy(saved);
                bad.History = wrong[i];
                LevelStateTests.Rig again = rig.Reopen();
                Assert.That(again.Flow.CheckResume(bad), Is.EqualTo(ResumeCheck.Resumable), "el formato vale: " + wrong[i]);
                bool resumed = true;
                Assert.That(() => { resumed = again.Flow.Resume(bad); }, Throws.Nothing, wrong[i]);
                Assert.That(resumed, Is.False, "no se reabre con otra máscara: " + wrong[i]);
            }

            // Con AppFlow, como un historial roto: el nivel se carga de nuevo (04 §10.2), con sus dos capas ocultas.
            LevelStateData broken = LevelStateSnapshot.Copy(saved);
            broken.History = wrong[0];
            LevelStateTests.Rig last = rig.Reopen();
            var app = new AppFlow(last.Flow, last.Progress, last.Economy.Wallet, new NullApp());
            app.Start(false, broken);
            Assert.That(app.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(last.Flow.LevelNumber, Is.EqualTo(Level100));
            Assert.That(last.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "desde el principio");
            Assert.That(last.Flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0));
            Assert.That(last.Flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0b11));
            Assert.That(last.Store.Load()!.History, Is.EqualTo("h2|P||||"), "el estado guardado ya es el del nivel recargado");
        }

        [Test]
        public void Campaign_NoMudInMurkyCuias_SoRestoringAtTheEndIsExact()
        {
            // Las capas se restauran al final del rehacer (h2 guarda la máscara, no el punto del historial). Con T4 (bajo una
            // oculta nunca va el color de la de encima) eso es exacto salvo si un lavado de Barro deja sobre la oculta su mismo
            // color: si un paquete futuro pusiera Barro en una cuia turbia, este test avisa de que hay que guardar el punto del
            // revelado (o prohibir la combinación en LevelValidation). Los tableros de alivio no tienen ocultas.
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            int murkyLevels = 0;
            for (int level = 1; level <= 200; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                BoardState board = def.CreateBoard();
                for (int i = 0; i < board.Count; i++)
                {
                    Cuia cuia = board[i];
                    if (!cuia.HasHidden)
                    {
                        continue;
                    }

                    for (int k = 0; k < cuia.Count; k++)
                    {
                        Assert.That(cuia.ColorAt(k), Is.Not.EqualTo(Cuia.MudColor), def.Id + ": Barro en la cuia turbia " + i);
                    }
                }

                if (board.HasHidden)
                {
                    murkyLevels++;
                }

                for (int r = 0; r < def.Relief.Count; r++)
                {
                    Assert.That(def.Relief[r].CreateBoard().HasHidden, Is.False, def.Id + ".relief[" + r + "]");
                }
            }

            Assert.That(murkyLevels, Is.EqualTo(7), "100, 106, 115, 135, 155, 175 y 195");
        }

        // 0 → 1 revela la capa 1 de la cuia 0 (R7) y Marea lo deshace: la capa sigue revelada (I5), pero ningún vertido del
        // historial la revela al rehacerlo, así que solo la conserva la máscara.
        private static void RevealLayer1ThenUndo(LevelFlow flow)
        {
            Assert.That(flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0b11));
            Assert.That(flow.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            TapResult poured = flow.TapCuia(1);
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(poured.Pour.RevealedMask, Is.EqualTo(1 << MurkyCuia), "R7: la capa 1 queda arriba y se revela");
            Assert.That(flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0b10));
            Assert.That(flow.Undo(), Is.True);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0));
            Assert.That(flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0b01), "I5: Marea no la vuelve a ocultar");
        }

        // Desde ahí, 1 → 0, 2 → 1 y 3 → 2 dejan Aguas estancadas sin usar la cuia 0 como origen (búsqueda hecha con el núcleo);
        // con una cuia vacía más se resuelve. Abre el modal (R15).
        private static void GetStuck(LevelFlow flow)
        {
            Play(flow, 1, 0, 1);
            Play(flow, 2, 1, 1);
            Play(flow, 3, 2, 1);
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0b10));
            OpenStuckModal(flow);
        }

        private static void OpenStuckModal(LevelFlow flow)
        {
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Stuck));
            for (int i = 0; i < 100 && flow.Phase != FlowPhase.StuckModal; i++)
            {
                flow.Tick(16, true);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
        }

        private static void Play(LevelFlow flow, int source, int target, int amount)
        {
            Assert.That(flow.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected), source + " → " + target);
            TapResult result = flow.TapCuia(target);
            Assert.That(result.Kind, Is.EqualTo(TapKind.Poured), source + " → " + target);
            Assert.That(result.Pour.Pour.Amount, Is.EqualTo(amount), source + " → " + target);
        }

        // La acción devuelve su «no aplicado» y no cambia nada: ni la cartera ni el stock, ni la sesión, ni el intento, ni el
        // nivel guardado, ni avisa a la vista de una cuia; LastFunnelError queda fijado.
        private static void AssertNothingChanges(LevelStateTests.Rig rig, System.Func<bool> notApplied)
        {
            LevelFlow flow = rig.Flow;
            LevelSession session = flow.Session;
            BoardState board = session.Current;
            int seeds = rig.Economy.Wallet.Seeds;
            int stock = rig.Economy.Wallet.Stock.Get(BoosterKind.ExtraCuia);
            int added = flow.Attempt.Added.Count;
            FlowPhase phase = flow.Phase;
            string? history = rig.Store.Load()!.History;
            int events = rig.Listener.Events.Count;

            Assert.That(notApplied(), Is.True, "devuelve null (o false): no aplicado");
            Assert.That(flow.LastFunnelError, Is.EqualTo(LevelFlow.AddCuiaReplayError));
            Assert.That(flow.LastFunnelError, Is.EqualTo("add_cuia_replay"));
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(seeds), "no se cobra");
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(stock));
            Assert.That(flow.Session, Is.SameAs(session), "la sesión no cambia");
            Assert.That(flow.Session.Current, Is.SameAs(board));
            Assert.That(flow.Attempt.Added.Count, Is.EqualTo(added), "no se añade la cuia");
            Assert.That(flow.Phase, Is.EqualTo(phase));
            Assert.That(rig.Store.Load()!.History, Is.EqualTo(history), "ni el nivel guardado");
            Assert.That(rig.Listener.Events.Count, Is.EqualTo(events), "ni se avisa a la vista");
        }

        // Paquete pack_schema 2 con Barro sobre una oculta (válido: 02 §9.1 M5 y §11 T2-T4). Nivel 1 (A): [3*, B, 2], [2, 2, 2],
        // [3], [3, 3], [4, 4, 5, 5], [5, 5, 4, 4] y vacía. Nivel 2 (B): [4, 3*, B], [5, 3], [3, 3], [4, 4, 4, 6], [5, 5, 5, 7],
        // [8, 6, 6, 6], [8, 7, 7, 7] y [8, 8], que tras lavar, revelar y verter el 3 queda en Aguas estancadas.
        private static LevelPack MudOverMurkyPack()
        {
            string a = TestPacks.MinimalLevel(
                cuias: "[[3, 15, 2], [2, 2, 2], [3], [3, 3], [4, 4, 5, 5], [5, 5, 4, 4], []]",
                caps: "[4, 4, 4, 4, 4, 4, 4]",
                attrs: "[null, null, null, null, null, null, null]",
                murky: "true",
                hidden: "[[0, 0]]");
            string b = TestPacks.MinimalLevel(
                cuias: "[[4, 3, 15], [5, 3], [3, 3], [4, 4, 4, 6], [5, 5, 5, 7], [8, 6, 6, 6], [8, 7, 7, 7], [8, 8]]",
                caps: "[4, 4, 4, 4, 4, 4, 4, 4]",
                attrs: "[null, null, null, null, null, null, null, null]",
                murky: "true",
                hidden: "[[0, 1]]")
                .Replace("\"id\": \"m01_v1_n0001\", \"level\": 1,", "\"id\": \"m01_v1_n0002\", \"level\": 2,");
            return LevelPackReader.Read(TestPacks.MinimalPack(a + ", " + b, "\"pack_schema\": 2"));
        }

        private sealed class NullApp : IAppFlowListener
        {
            public void OnAppScreenChanged(AppScreen screen)
            {
            }
        }
    }
}
