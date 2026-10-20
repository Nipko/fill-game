#nullable enable
using AF.Economy;
using AF.GameFlow;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Calificativo «¡Perfecto!» de la victoria (tarea DC14; Magic Sort §10.2 #25, recomendada A; D-029):
    /// <see cref="LevelFlow.IsPerfect"/> es true solo si el intento ganado no llevó la Cuia de rescate (ni la de «Continuar:
    /// +1 cuia» ni la de la Balsa de rescate); la Cuia extra y Marea no lo quitan [P]. Se evalúa por intento: tras un rescate,
    /// Volver a la orilla sigue con la cuia (no es perfecto), y otro nivel o volver a entrar en el mismo empiezan sin ella. En el
    /// nivel 36 (tablero 6 del tutorial con <c>af_level_offset</c> = 30), como <see cref="StuckModalTests"/>.
    /// </summary>
    public sealed class VictoryPerfectTests
    {
        [Test]
        public void WithoutTheRescue_TheVictoryIsPerfect()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 1000, maxLevel: 35);
            fx.StartPlaying(36);
            Assert.That(fx.Flow.IsPerfect, Is.False, "fuera de una victoria");
            LevelStateTests.Win(fx.Flow);
            Assert.That(fx.Flow.IsPerfect, Is.True, "desde el instante lógico de la victoria");
            ToVictoryScreen(fx.Flow);
            Assert.That(fx.Flow.IsPerfect, Is.True);
            Assert.That(UiText.VictoryTitle(fx.Flow.IsPerfect), Is.EqualTo("¡Perfecto!"));
        }

        [Test]
        public void WithTheRescue_TheVictoryKeepsTheUsualTitle()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 1000, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            Assert.That(fx.Flow.BuyRescue()!.IsApplied, Is.True);
            LevelStateTests.Win(fx.Flow);
            Assert.That(fx.Flow.IsPerfect, Is.False, "el intento ganado llevó la Cuia de rescate");
            ToVictoryScreen(fx.Flow);
            Assert.That(fx.Flow.IsPerfect, Is.False);
            Assert.That(UiText.VictoryTitle(fx.Flow.IsPerfect), Is.EqualTo("¡Nivel completado!"));
        }

        [Test]
        public void TheBalsaRescue_AlsoRemovesThePerfect()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 100, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            Assert.That(fx.Flow.ApplyPaidRescue(), Is.True, "Balsa de rescate: la Cuia de rescate sin semillas");
            LevelStateTests.Win(fx.Flow);
            Assert.That(fx.Flow.IsPerfect, Is.False);
        }

        [Test]
        public void TheExtraCuiaAndMarea_DoNotRemoveThePerfect()
        {
            // Marea desde Aguas estancadas y Cuia extra: la pregunta solo nombra la Cuia de rescate [P].
            var fx = new FunnelFixture(offset: 30, seeds: 1000, maxLevel: 35);
            fx.Economy.Profile.Stock.ExtraCuia = 1;
            fx.StartPlaying(36);
            fx.GetStuck();
            Assert.That(fx.Flow.Undo(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            TransactionResult? extra = fx.Flow.UseExtraCuia();
            Assert.That(extra!.IsApplied, Is.True);
            Assert.That(fx.Flow.Attempt.ExtraCuias, Is.EqualTo(1));
            LevelStateTests.Win(fx.Flow);
            Assert.That(fx.Flow.IsPerfect, Is.True);
        }

        [Test]
        public void ItIsDecidedPerAttempt()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 2000, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            Assert.That(fx.Flow.BuyRescue()!.IsApplied, Is.True);

            // Volver a la orilla: la Cuia de rescate persiste en el tablero del intento nuevo, así que no es perfecto.
            Assert.That(fx.Flow.Restart(), Is.True);
            Assert.That(fx.Flow.Attempt.Rescues, Is.EqualTo(1));
            Assert.That(fx.Flow.TapAnywhere(), Is.True, "la INTRO corta del reintento");
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            LevelStateTests.Win(fx.Flow);
            Assert.That(fx.Flow.IsPerfect, Is.False, "el tablero ganado llevaba la Cuia de rescate");

            // El nivel siguiente empieza sin ella: perfecto otra vez.
            fx.StartPlaying(37);
            Assert.That(fx.Flow.IsPerfect, Is.False, "al cargar, fuera de una victoria");
            LevelStateTests.Win(fx.Flow);
            Assert.That(fx.Flow.IsPerfect, Is.True);

            // Rescate en el 36 sin ganarlo y volver a entrar en él: las cuias añadidas se olvidan al salir del nivel.
            fx.StartPlaying(36);
            fx.GetStuck();
            Assert.That(fx.Flow.BuyRescue()!.IsApplied, Is.True);
            fx.StartPlaying(36);
            Assert.That(fx.Flow.Attempt.Rescues, Is.EqualTo(0));
            LevelStateTests.Win(fx.Flow);
            Assert.That(fx.Flow.IsPerfect, Is.True);
        }

        // De la victoria lógica a VICTORY_SCREEN: la vista está libre, celebración y toque.
        private static void ToVictoryScreen(LevelFlow flow)
        {
            flow.Tick(16, true);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Celebration));
            flow.TapAnywhere();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
        }
    }
}
