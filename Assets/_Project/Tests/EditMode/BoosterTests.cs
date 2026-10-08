#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using AF.Boosters;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Bandeja de comodines (tarea B2-1; GDD 04 §1.1, §2.1 y §2.2; 08 §9): cupos de Marea, recarga, Cuia extra (regalo del
    /// nivel 8, precio, tope de 1 por nivel, persistencia al reiniciar, Marea no la quita, no se ofrece en STUCK_MODAL) y la
    /// guarda contra cifras inventadas de <see cref="BoosterConfig"/>.
    /// </summary>
    public sealed class BoosterTests
    {
        private static string BoostersDir => Path.Combine(Application.dataPath, "_Project", "Scripts", "Boosters");

        [Test]
        public void EveryBoosterDefault_IsTheOneOfThe08_9Table()
        {
            Dictionary<string, EconomyConfigTests.RcRow> table = EconomyConfigTests.ReadRemoteConfigTable();
            BoosterConfig config = BoosterConfig.Defaults;
            int checkedKeys = 0;
            foreach (PropertyInfo property in typeof(BoosterConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                if (property.CanWrite)
                {
                    Assert.That(attribute, Is.Not.Null, property.Name + ": toda cifra configurable sale de 08 §9");
                }

                if (attribute == null)
                {
                    continue;
                }

                Assert.That(table.ContainsKey(attribute.Key), Is.True, attribute.Key + " no está en 08 §9");
                EconomyConfigTests.RcRow row = table[attribute.Key];
                object? value = property.GetValue(config);
                switch (value)
                {
                    case int i:
                        Assert.That(row.Type, Is.EqualTo("int"), attribute.Key);
                        Assert.That(i.ToString(CultureInfo.InvariantCulture), Is.EqualTo(row.Default), attribute.Key);
                        break;
                    case bool b:
                        Assert.That(row.Type, Is.EqualTo("bool"), attribute.Key);
                        Assert.That(b ? "true" : "false", Is.EqualTo(row.Default), attribute.Key);
                        break;
                    case double d:
                        Assert.That(row.Type, Is.EqualTo("float"), attribute.Key);
                        Assert.That(d, Is.EqualTo(double.Parse(row.Default!, CultureInfo.InvariantCulture)), attribute.Key);
                        break;
                    case string text:
                        Assert.That(row.Type, Is.EqualTo("string"), attribute.Key);
                        Assert.That(text, Is.EqualTo(row.Default), attribute.Key);
                        break;
                    default:
                        Assert.Fail(attribute.Key + ": tipo no previsto");
                        break;
                }

                checkedKeys++;
            }

            Assert.That(checkedKeys, Is.EqualTo(15));
        }

        [Test]
        public void BoostersAssembly_HasNoEngine_AndNoSystemClock()
        {
            string asmdef = File.ReadAllText(Path.Combine(BoostersDir, "AF.Boosters.asmdef"));
            StringAssert.Contains("\"noEngineReferences\": true", asmdef);
            foreach (string forbidden in new[] { "AF.GameFlow", "AF.UI", "AF.Presentation", "AF.Levels", "Unity" })
            {
                Assert.That(asmdef.Contains(forbidden), Is.False, "AF.Boosters no debe referenciar " + forbidden + " (08 §2.2)");
            }

            string[] banned = { "DateTime" + ".Now", "DateTime" + ".UtcNow", "System" + ".Random", "using " + "Unity", "System.Linq" };
            foreach (string file in Directory.GetFiles(BoostersDir, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                foreach (string word in banned)
                {
                    Assert.That(text.Contains(word), Is.False, Path.GetFileName(file) + ": " + word);
                }
            }
        }

        [Test]
        public void Marea_IsUnlimitedUpTo10_AndThreePerLevelFrom11()
        {
            var quota = new UndoQuota(BoosterConfig.Defaults);
            quota.ResetForLevel(10);
            Assert.That(quota.Unlimited, Is.True);
            Assert.That(quota.Consume(null), Is.EqualTo(UndoSource.Unlimited));

            quota.ResetForLevel(11);
            Assert.That(quota.Unlimited, Is.False);
            Assert.That(quota.FreeLeft, Is.EqualTo(3));
            Assert.That(quota.Consume(null), Is.EqualTo(UndoSource.Free));
            Assert.That(quota.Consume(null), Is.EqualTo(UndoSource.Free));
            Assert.That(quota.Consume(null), Is.EqualTo(UndoSource.Free));
            Assert.That(quota.Consume(null), Is.EqualTo(UndoSource.None), "sin cupo ni stock: toca recargar");
            quota.Restore();
            Assert.That(quota.FreeLeft, Is.EqualTo(3), "04 §2.7: Volver a la orilla restaura el cupo");
        }

        [Test]
        public void Marea_SpendsTheFreeQuotaFirst_ThenTheStock()
        {
            var fx = new FunnelFixture(offset: 10);
            fx.Economy.Profile.Stock.Undo = 2;
            fx.StartPlaying(11);
            UndoStatus start = fx.Flow.UndoStatus;
            Assert.That(start.FreeLeft, Is.EqualTo(3));
            Assert.That(start.Stock, Is.EqualTo(2));
            for (int i = 0; i < 5; i++)
            {
                fx.Flow.TapCuia(0);
                fx.Flow.TapCuia(2);
                Assert.That(fx.Flow.Undo(), Is.True, "Marea " + (i + 1));
            }

            Assert.That(fx.Flow.UndoStatus.FreeLeft, Is.EqualTo(0));
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(0), "las 2 del stock se gastan después del cupo");
            fx.Flow.TapCuia(0);
            fx.Flow.TapCuia(2);
            Assert.That(fx.Flow.CanUndo, Is.False);
            Assert.That(fx.Flow.Undo(), Is.False);
        }

        [Test]
        public void MareaRefill_30Seeds_AddsThreeToTheStock_AndWithoutSeedsChangesNothing()
        {
            var fx = new FunnelFixture(offset: 10, seeds: 40);
            fx.StartPlaying(11);
            TransactionResult? paid = fx.Flow.RefillUndoWithSeeds();
            Assert.That(paid, Is.Not.Null);
            Assert.That(paid!.IsApplied, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(10));
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(3));

            TransactionResult? rejected = fx.Flow.RefillUndoWithSeeds();
            Assert.That(rejected!.IsApplied, Is.False);
            Assert.That(rejected.Missing, Is.EqualTo(20));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(10));
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(3));

            Assert.That(fx.Flow.GrantUndoRefill(), Is.True, "recompensa de rv_undo_refill: +3 al stock");
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(6));
        }

        [Test]
        public void ExtraCuia_SlotExistsFrom8_AndCapiGiftsOneTheFirstTime()
        {
            var fx = new FunnelFixture();
            fx.StartPlaying(7);
            Assert.That(fx.Flow.ExtraCuia.State, Is.EqualTo(ExtraCuiaState.Hidden), "en 1-7 la ranura no existe (07 §7.4)");
            Assert.That(fx.Listener.Saw("gift:1"), Is.False);

            fx.StartPlaying(8);
            Assert.That(fx.Listener.Saw("gift:1"), Is.True, "04 §2.2: Capi regala 1 Cuia extra en el 8");
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(1));
            ExtraCuiaOffer offer = fx.Flow.ExtraCuia;
            Assert.That(offer.State, Is.EqualTo(ExtraCuiaState.Ready));
            Assert.That(offer.FromStock, Is.True);
            Assert.That(offer.CanUse, Is.True);

            fx.Listener.Events.Clear();
            fx.StartPlaying(8);
            Assert.That(fx.Listener.Saw("gift:1"), Is.False, "con la regalada en el stock no se regala otra");

            var won = new FunnelFixture(maxLevel: 8);
            won.StartPlaying(8);
            Assert.That(won.Listener.Saw("gift:1"), Is.False, "ya superado: sin regalo");
        }

        [Test]
        public void ExtraCuia_SpendsTheStockFirst_ThenCosts150_AndIsOnePerLevel()
        {
            var fx = new FunnelFixture(seeds: 200, maxLevel: 8);
            fx.Economy.Profile.Stock.ExtraCuia = 1;
            fx.StartPlaying(8);
            int cuias = fx.Flow.Session.Current.Count;
            TransactionResult? used = fx.Flow.UseExtraCuia();
            Assert.That(used!.IsApplied, Is.True);
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(0));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(200), "con stock no se cobran semillas");
            Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(cuias + 1));
            Assert.That(fx.Flow.Session.Current[cuias].Capacity, Is.EqualTo(4));
            Assert.That(fx.Flow.Session.Current[cuias].IsEmpty, Is.True);
            Assert.That(fx.Listener.Saw("cuia:" + cuias + ":Extra"), Is.True);
            Assert.That(fx.Flow.ExtraCuia.State, Is.EqualTo(ExtraCuiaState.Used), "extra_cuia_max_per_level = 1");
            Assert.That(fx.Flow.UseExtraCuia(), Is.Null);

            var paid = new FunnelFixture(seeds: 200, maxLevel: 8);
            paid.StartPlaying(8);
            Assert.That(paid.Flow.ExtraCuia.Price, Is.EqualTo(150));
            Assert.That(paid.Flow.UseExtraCuia()!.IsApplied, Is.True);
            Assert.That(paid.Wallet.Seeds, Is.EqualTo(50));
        }

        [Test]
        public void ExtraCuia_WithoutSeeds_ShowsWhatIsMissing_AndChangesNothing()
        {
            var fx = new FunnelFixture(seeds: 100, maxLevel: 8);
            fx.StartPlaying(8);
            ExtraCuiaOffer offer = fx.Flow.ExtraCuia;
            Assert.That(offer.State, Is.EqualTo(ExtraCuiaState.Ready));
            Assert.That(offer.Missing, Is.EqualTo(50), "«Te faltan 50» (04 §1.1)");
            Assert.That(offer.CanUse, Is.False);
            int cuias = fx.Flow.Session.Current.Count;
            TransactionResult? rejected = fx.Flow.UseExtraCuia();
            Assert.That(rejected!.IsApplied, Is.False);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(100));
            Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(cuias));
        }

        [Test]
        public void ExtraCuia_PersistsOnRestart_AndMareaDoesNotRemoveIt()
        {
            var fx = new FunnelFixture(maxLevel: 8);
            fx.Economy.Profile.Stock.ExtraCuia = 1;
            fx.StartPlaying(8);
            fx.Flow.TapCuia(0);
            fx.Flow.TapCuia(5); // 7,7 a una vacía generada
            Assert.That(fx.Flow.Session.AppliedPours, Is.EqualTo(1));
            int cuias = fx.Flow.Session.Current.Count;
            Assert.That(fx.Flow.UseExtraCuia()!.IsApplied, Is.True);
            Assert.That(fx.Flow.Session.AppliedPours, Is.EqualTo(1), "el historial de Marea se conserva");
            Assert.That(fx.Flow.Session.Current[5].Count, Is.EqualTo(2));
            Assert.That(fx.Flow.GeneratedCuias, Is.EqualTo(cuias), "las filas las fijan las cuias generadas (D-006)");

            Assert.That(fx.Flow.Undo(), Is.True);
            Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(cuias + 1), "Marea no quita la cuia añadida (02 §14)");
            Assert.That(fx.Flow.Session.AppliedPours, Is.EqualTo(0));

            Assert.That(fx.Flow.Restart(), Is.True);
            Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(cuias + 1), "persiste al reiniciar (04 §2.2)");
            Assert.That(fx.Flow.Session.Initial.Count, Is.EqualTo(cuias + 1));
            Assert.That(fx.Flow.ExtraCuia.State, Is.Not.EqualTo(ExtraCuiaState.Ready), "sigue contando para el tope del nivel");

            fx.Flow.TapAnywhere();
            fx.StartPlaying(8);
            Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(cuias), "desaparece al abandonar el nivel");
        }

        [Test]
        public void ExtraCuia_IsNotOfferedOnceStuck()
        {
            // 04 §2.2: solo mientras quede un vertido útil; en el bloqueo se ofrece la Cuia de rescate. Nivel 36 = tablero 6.
            var fx = new FunnelFixture(offset: 30, maxLevel: 35);
            fx.Economy.Profile.Stock.ExtraCuia = 1;
            fx.StartPlaying(36);
            Assert.That(fx.Flow.ExtraCuia.State, Is.EqualTo(ExtraCuiaState.Ready));
            fx.GetStuck();
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(fx.Flow.ExtraCuia.State, Is.EqualTo(ExtraCuiaState.Disabled));
            Assert.That(fx.Flow.UseExtraCuia(), Is.Null);
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(1));
        }
    }

    /// <summary>Flujo de nivel con cartera para los tests de la bandeja y del embudo (B2-1 a B2-3).</summary>
    internal sealed class FunnelFixture
    {
        public FunnelFixture(int offset = 0, int seeds = 0, int maxLevel = 0, LevelPack? pack = null)
        {
            Economy = new EconomyFixture(seeds);
            Listener = new RecordingListener();
            Progress = new MemoryProgressStore(0, maxLevel);
            LevelCatalog catalog = LevelCatalog.Create(new[] { pack ?? TestPacks.Tutorial }, offset);
            Flow = new LevelFlow(catalog, Progress, Listener, GameConfig.TapSwitchSelection, PourModes.Default, Economy.Wallet);
        }

        public EconomyFixture Economy { get; }

        public Wallet Wallet => Economy.Wallet;

        public RecordingListener Listener { get; }

        public MemoryProgressStore Progress { get; }

        public LevelFlow Flow { get; }

        public void StartPlaying(int level)
        {
            Flow.Load(level);
            Flow.TapAnywhere();
            Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        /// <summary>Tablero 6 del tutorial: 3 → 4 deja Aguas estancadas (TutorialPackTests); abre el modal (R15).</summary>
        public void GetStuck()
        {
            Assert.That(Flow.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(Flow.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(Flow.Session.Status, Is.EqualTo(LevelStatus.Stuck));
            OpenModal();
        }

        /// <summary>Deja pasar la cola y los 400 ms de <c>stuck_modal_delay_ms</c>.</summary>
        public void OpenModal()
        {
            Flow.Tick(16, true);
            Flow.Tick(GameConfig.StuckModalDelayMs, true);
            Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
        }

        public void TickIntro()
        {
            Flow.Tick(Flow.IntroDurationMs, true);
        }

        public void SpendCanoesTo(int canoes)
        {
            while (Wallet.Canoes.Count > canoes)
            {
                Assert.That(Wallet.Apply(Transaction.HardFail(31)).IsApplied, Is.True);
            }
        }

        public void AdvanceClock(TimeSpan delta)
        {
            Economy.Clock.Advance(delta);
        }
    }
}
