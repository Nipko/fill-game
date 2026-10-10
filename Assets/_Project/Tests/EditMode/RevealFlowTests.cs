#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Luciérnaga · Revelar en el flujo (tarea T10 del paso 1; 04 §2.5; 02 §11 T6 y §14; 07 §7.6; 08 §9 <c>reveal_seed_price</c>
    /// = 120): el regalo del estreno de Aguas turbias en el 100, una sola vez y guardado (marca <c>l</c>); el modo de selección,
    /// que no cobra nada al abrirlo ni al cancelarlo; el revelado con el stock o con 120 semillas en el orden aplicar → guardar
    /// → cobrar (04 §2.5 d); y lo revelado, que sobrevive a Marea, a Volver a la orilla y a reabrir la app. Con el catálogo real
    /// (<see cref="TestPacks.Campaign"/>): nivel 100, cuia 0 = [10*, 8*, 6] y cuia 1 = [6]; nivel 106, cuia 6 = [14*, 1].
    /// </summary>
    public sealed class RevealFlowTests
    {
        private const int Level100 = 100;
        private const int MurkyCuia = 0;

        [Test]
        public void ShouldGift_PrimitivesOnly()
        {
            // 08 §2.2: AF.Boosters no referencia AF.Levels; lo que necesita del nivel entra como primitivos, como en
            // ExtraCuiaBooster.ShouldGift.
            MethodInfo method = typeof(RevealBooster).GetMethod(nameof(RevealBooster.ShouldGift))!;
            ParameterInfo[] parameters = method.GetParameters();
            Assert.That(
                Array.ConvertAll(parameters, p => p.ParameterType),
                Is.EqualTo(new[] { typeof(int), typeof(bool), typeof(int), typeof(bool), typeof(Wallet), typeof(BoosterConfig) }));
            for (int i = 0; i < parameters.Length; i++)
            {
                Assert.That(parameters[i].ParameterType.Assembly.GetName().Name, Is.Not.EqualTo("AF.Levels"), parameters[i].Name);
            }

            AssemblyName[] references = typeof(RevealBooster).Assembly.GetReferencedAssemblies();
            for (int i = 0; i < references.Length; i++)
            {
                Assert.That(references[i].Name, Is.Not.EqualTo("AF.Levels"));
            }

            string asmdef = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/Boosters/AF.Boosters.asmdef"));
            Assert.That(asmdef, Does.Not.Contain("AF.Levels"));

            // Solo en el estreno (murkyDebut), aún no superado, una vez y sin Luciérnagas en el stock (el patrón de la Cuia extra).
            var economy = new EconomyFixture();
            BoosterConfig config = BoosterConfig.Defaults;
            Assert.That(RevealBooster.ShouldGift(100, true, 99, false, economy.Wallet, config), Is.True);
            Assert.That(RevealBooster.ShouldGift(100, true, 0, false, economy.Wallet, config), Is.True);
            Assert.That(RevealBooster.ShouldGift(100, false, 99, false, economy.Wallet, config), Is.False, "otro nivel turbio (106) no lo regala");
            Assert.That(RevealBooster.ShouldGift(100, true, 100, false, economy.Wallet, config), Is.False, "ya superado");
            Assert.That(RevealBooster.ShouldGift(100, true, 99, true, economy.Wallet, config), Is.False, "ya regalada en este nivel");
            Assert.That(RevealBooster.ShouldGift(100, true, 99, false, null, config), Is.False, "sin cartera");
            economy.Profile.Stock.Reveal = 1;
            Assert.That(RevealBooster.ShouldGift(100, true, 99, false, economy.Wallet, config), Is.False, "ya tiene una");
            Assert.That(BoosterConfig.RevealGiftUnits, Is.EqualTo(1), "04 §2.5: 1 Luciérnaga");
        }

        [Test]
        public void Gift_Level100_Once()
        {
            LevelStateTests.Rig rig = Rig();
            rig.Start(Level100);
            Assert.That(rig.Listener.Events, Is.EqualTo(new[] { "loaded:100", "reveal_gift:1", "intro:" + GameConfig.AnimTintDurationMsLate, "ready:skipped" }));
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1));
            Assert.That(rig.Flow.Attempt.RevealGifted, Is.True);
            Assert.That(rig.Flow.RevealStock, Is.EqualTo(1));

            // Se gasta, se sale al inicio y se vuelve a jugar el 100: no se repite (ni con el stock a 0).
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Stock), Is.True);
            rig.Flow.TapCuia(MurkyCuia);
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0));
            rig.Flow.LeaveLevel();
            rig.Listener.Events.Clear();
            rig.Start(Level100);
            Assert.That(rig.Listener.Saw("reveal_gift:1"), Is.False);
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0));

            // Otros niveles turbios (106) y los vecinos no regalan nada; sin cartera, tampoco.
            LevelStateTests.Rig other = Rig();
            other.Start(106);
            Assert.That(other.Flow.Level.IsMurky, Is.True);
            other.Start(99);
            other.Start(101);
            Assert.That(other.Listener.Saw("reveal_gift:1"), Is.False);
            Assert.That(other.Economy.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0));

            var listener = new RecordingListener();
            var noWallet = new LevelFlow(LevelCatalog.Create(TestPacks.Campaign), new MemoryProgressStore(), listener);
            noWallet.Load(Level100);
            Assert.That(listener.Saw("reveal_gift:1"), Is.False);
            Assert.That(noWallet.Attempt.RevealGifted, Is.False);
        }

        [Test]
        public void Gift_Persisted()
        {
            LevelStateTests.Rig rig = Rig();
            rig.Start(Level100);
            Assert.That(rig.Store.Load()!.History, Is.EqualTo("h2|P||l||"), "marca l en el nivel en curso");

            // Cerrar y abrir la app a mitad del nivel: la marca vuelve con el resto del estado.
            LevelStateTests.Rig again = rig.Reopen();
            Assert.That(again.Flow.Resume(rig.Store.Load()!), Is.True);
            Assert.That(again.Flow.Attempt.RevealGifted, Is.True);
            Assert.That(again.Listener.Saw("reveal_gift:1"), Is.False, "reanudar no regala");

            // Se gasta (así el stock no tapa un regalo repetido) y se sale al inicio: la marca va en el estado de salida.
            Assert.That(again.Flow.BeginRevealSelection(RevealPayment.Stock), Is.True);
            again.Flow.TapCuia(MurkyCuia);
            Assert.That(again.Economy.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0));
            again.Flow.LeaveLevel();
            LevelStateData exited = again.Store.Load()!;
            Assert.That(exited.History, Is.EqualTo("h2|X||l||"));

            // Otra sesión: la app abre el inicio y Jugar vuelve a cargar el 100 sin regalo.
            LevelStateTests.Rig later = again.Reopen();
            var app = new AppFlow(later.Flow, later.Progress, later.Economy.Wallet, new NullApp());
            app.Start(false, exited);
            Assert.That(app.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(later.Flow.Attempt.RevealGifted, Is.True);
            later.Flow.Load(Level100);
            Assert.That(later.Listener.Saw("reveal_gift:1"), Is.False, "04 §2.5: una sola vez");
            Assert.That(later.Economy.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0));
            Assert.That(later.Store.Load()!.History, Is.EqualTo("h2|P||l||"));
        }

        [Test]
        public void Select_Cancel_NoCost()
        {
            LevelStateTests.Rig rig = Rig(seeds: 500);
            rig.Start(Level100);
            LevelStateData before = rig.Store.Load()!;
            Assert.That(rig.Flow.RevealAvailable, Is.True);
            Assert.That(rig.Flow.RevealSelecting, Is.False);
            Assert.That(rig.Flow.RevealPrice, Is.EqualTo(120), "reveal_seed_price (08 §9)");

            // C8: con una cuia elevada, abrir la selección primero la baja.
            Assert.That(rig.Flow.TapCuia(1).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Stock), Is.True);
            Assert.That(rig.Flow.RevealSelecting, Is.True);
            Assert.That(rig.Flow.RevealSelectionPayment, Is.EqualTo(RevealPayment.Stock));
            Assert.That(rig.Flow.Selected, Is.EqualTo(-1));
            Assert.That(Tail(rig, 2), Is.EqualTo(new[] { "tap:Deselected", "reveal_select:on" }));
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Seeds), Is.False, "ya está eligiendo");

            Assert.That(rig.Flow.CancelRevealSelection(), Is.True);
            Assert.That(rig.Flow.RevealSelecting, Is.False);
            Assert.That(Tail(rig, 1), Is.EqualTo(new[] { "reveal_select:off" }));
            Assert.That(rig.Flow.CancelRevealSelection(), Is.False);

            // Tocar el fondo también cancela (04 §2.5: «tocar fuera cancela sin costo»), sin llegar a la vista como toque.
            int taps = rig.Listener.Taps.Count;
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Seeds), Is.True);
            Assert.That(rig.Flow.TapBackground().Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(rig.Flow.RevealSelecting, Is.False);
            Assert.That(Tail(rig, 1), Is.EqualTo(new[] { "reveal_select:off" }));
            Assert.That(rig.Listener.Taps.Count, Is.EqualTo(taps));

            // Y cualquier otra acción: Marea, la pausa (ClearSelection) o Volver a la orilla.
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Stock), Is.True);
            rig.Flow.ClearSelection();
            Assert.That(rig.Flow.RevealSelecting, Is.False);
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Stock), Is.True);
            Assert.That(rig.Flow.PressRestart(), Is.True);
            Assert.That(rig.Flow.RevealSelecting, Is.False);
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Stock), Is.False, "en la intro no se puede");
            Assert.That(rig.Flow.TapAnywhere(), Is.True);

            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1), "nada se cobró");
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(500));
            Assert.That(rig.Flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0b11), "nada se reveló");
            Assert.That(rig.Store.Load()!.History, Is.EqualTo(before.History));
            Assert.That(rig.Listener.Saw("revealed:0:3"), Is.False);
        }

        [Test]
        public void Reveal_Stock()
        {
            LevelStateTests.Rig rig = Rig();
            rig.Start(Level100);
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Stock), Is.True);
            int taps = rig.Listener.Taps.Count;
            TapResult tap = rig.Flow.TapCuia(MurkyCuia);
            Assert.That(tap.Kind, Is.EqualTo(TapKind.Ignored), "el toque elige la cuia; no selecciona");
            Assert.That(rig.Listener.Taps.Count, Is.EqualTo(taps), "no llega a la vista como toque");
            Assert.That(rig.Flow.Selected, Is.EqualTo(-1));
            Assert.That(rig.Flow.RevealSelecting, Is.False);
            Assert.That(Tail(rig, 2), Is.EqualTo(new[] { "reveal_select:off", "revealed:0:3" }), "las dos capas ocultas (bits 0 y 1)");

            // 02 T6: todas las ocultas de la cuia, a la vista y sin volver a ocultarse; sin coste en semillas.
            Assert.That(rig.Flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0));
            Assert.That(rig.Flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0b11));
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0), "se gastó la regalada");
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(rig.Store.Load()!.History, Is.EqualTo("h2|P||l||003"), "R: las capas 0 y 1 de la cuia 0");
            Assert.That(rig.Flow.Session.AppliedPours, Is.EqualTo(0), "no entra en el historial de Marea (04 §2.1)");
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Playing));

            // Sin ocultas ya no hay Luciérnaga que usar (04 §2.5: «se oculta si ya no quedan unidades ocultas»).
            Assert.That(rig.Flow.RevealAvailable, Is.False);
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Stock), Is.False);

            // El siguiente toque vuelve a ser tap-tap.
            Assert.That(rig.Flow.TapCuia(MurkyCuia).Kind, Is.EqualTo(TapKind.Selected));
        }

        [Test]
        public void Reveal_Seeds_ApplySaveCharge()
        {
            // 04 §2.5 d: aplicar → guardar → cobrar. El nivel 106 es turbio sin regalo: se paga con 120 semillas.
            var economy = new EconomyFixture(seeds: 119);
            var log = new List<string>();
            var store = new LoggingStore(log, economy.Wallet);
            economy.Wallet.Changed += r => log.Add("charge:" + r.SeedsDelta.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var listener = new RecordingListener();
            var flow = new LevelFlow(LevelCatalog.Create(TestPacks.Campaign), new MemoryProgressStore(), listener, GameConfig.TapSwitchSelection, PourModes.Default, economy.Wallet)
            {
                StateStore = store,
            };
            flow.Load(106);
            Assert.That(flow.TapAnywhere(), Is.True);
            int murky = FirstMurkyCuia(flow.Session.Current);
            int mask = flow.Session.Current[murky].HiddenMask;
            Assert.That(murky, Is.EqualTo(6));
            Assert.That(mask, Is.EqualTo(0b01));

            // Sin stock ni saldo (119 < 120) no se abre: la vista avisa «Te faltan 1».
            Assert.That(flow.RevealAvailable, Is.True);
            Assert.That(flow.BeginRevealSelection(RevealPayment.Stock), Is.False);
            Assert.That(flow.BeginRevealSelection(RevealPayment.Seeds), Is.False);
            Assert.That(flow.RevealSelecting, Is.False);

            economy.Profile.Wallet.Seeds = 120;
            Assert.That(flow.BeginRevealSelection(RevealPayment.Seeds), Is.True, "abrir la selección no cobra");
            Assert.That(economy.Wallet.Seeds, Is.EqualTo(120));
            log.Clear();
            Assert.That(flow.TapCuia(murky).Kind, Is.EqualTo(TapKind.Ignored));

            string revealed = "6" + mask.ToString("x2", System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(log, Is.EqualTo(new[] { "save:120:" + revealed, "charge:-120" }), "primero se guarda lo revelado, con el saldo intacto; después se cobra");
            Assert.That(economy.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(flow.Session.Current[murky].HiddenMask, Is.EqualTo(0));
            Assert.That(flow.Session.RevealedLayers(murky), Is.EqualTo(mask));
            Assert.That(listener.Saw("revealed:" + murky + ":" + mask), Is.True);
            Assert.That(store.Load()!.History, Does.EndWith("|" + revealed));
        }

        [Test]
        public void Reveal_NonMurkyCuia_Cancels()
        {
            LevelStateTests.Rig rig = Rig();
            rig.Start(Level100);
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Stock), Is.True);
            int taps = rig.Listener.Taps.Count;
            TapResult tap = rig.Flow.TapCuia(1); // [6], sin ocultas
            Assert.That(tap.Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(rig.Flow.RevealSelecting, Is.False);
            Assert.That(rig.Flow.Selected, Is.EqualTo(-1), "cancela; no la selecciona");
            Assert.That(rig.Listener.Taps.Count, Is.EqualTo(taps));
            Assert.That(Tail(rig, 1), Is.EqualTo(new[] { "reveal_select:off" }));
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1), "sin coste");
            Assert.That(rig.Flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0b11));
            Assert.That(rig.Listener.Saw("revealed:1:0"), Is.False);

            // El siguiente toque ya es tap-tap.
            Assert.That(rig.Flow.TapCuia(1).Kind, Is.EqualTo(TapKind.Selected));
        }

        [Test]
        public void Reveal_SurvivesUndoRestartResume()
        {
            // 02 T6 e I5: lo que revela la Luciérnaga sigue revelado tras Marea, tras Volver a la orilla y al reabrir la app.
            LevelStateTests.Rig rig = Rig();
            rig.Start(Level100);
            Assert.That(rig.Flow.BeginRevealSelection(RevealPayment.Stock), Is.True);
            rig.Flow.TapCuia(MurkyCuia);
            BoardState revealed = rig.Flow.Session.Current;
            Assert.That(revealed[MurkyCuia].HiddenMask, Is.EqualTo(0));

            Assert.That(rig.Flow.TapCuia(MurkyCuia).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(rig.Flow.TapCuia(1).Kind, Is.EqualTo(TapKind.Poured), "el 6 sobre el 6");
            Assert.That(rig.Flow.PressUndo(), Is.True);
            Assert.That(rig.Flow.Session.Current, Is.EqualTo(revealed), "Marea deshace el vertido, no el revelado");
            Assert.That(rig.Flow.Session.Current[MurkyCuia].HiddenMask, Is.EqualTo(0));

            Assert.That(rig.Flow.PressRestart(), Is.True);
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.Session.Current, Is.EqualTo(revealed), "Volver a la orilla tampoco lo oculta (04 §2.7)");
            LevelStateData saved = rig.Store.Load()!;
            Assert.That(saved.History, Is.EqualTo("h2|P||l||003"));

            LevelStateTests.Rig again = rig.Reopen();
            Assert.That(again.Flow.Resume(saved), Is.True);
            Assert.That(again.Flow.Session.RevealedLayers(MurkyCuia), Is.EqualTo(0b11));
            Assert.That(again.Flow.Session.Current, Is.EqualTo(revealed), "02 §14: se reabre con lo revelado");
            Assert.That(again.Flow.RevealAvailable, Is.False, "no quedan ocultas");
        }

        private static LevelStateTests.Rig Rig(int seeds = 0)
        {
            return new LevelStateTests.Rig(seeds: seeds, packs: TestPacks.Campaign);
        }

        // Los últimos n eventos del oyente.
        private static string[] Tail(LevelStateTests.Rig rig, int n)
        {
            List<string> events = rig.Listener.Events;
            return events.GetRange(events.Count - n, n).ToArray();
        }

        private static int FirstMurkyCuia(BoardState board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].HasHidden)
                {
                    return i;
                }
            }

            return -1;
        }

        // Nivel en curso en memoria que anota cada guardado con el saldo de ese instante y la sección R de history.
        private sealed class LoggingStore : ILevelStateStore
        {
            private readonly MemoryLevelStateStore _inner = new MemoryLevelStateStore();
            private readonly List<string> _log;
            private readonly Wallet _wallet;

            public LoggingStore(List<string> log, Wallet wallet)
            {
                _log = log;
                _wallet = wallet;
            }

            public LevelStateData? Load()
            {
                return _inner.Load();
            }

            public void Save(LevelStateData state)
            {
                string history = state.History ?? string.Empty;
                _log.Add("save:" + _wallet.Seeds.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + history.Substring(history.LastIndexOf('|') + 1));
                _inner.Save(state);
            }

            public void Clear()
            {
                _log.Add("clear");
                _inner.Clear();
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
