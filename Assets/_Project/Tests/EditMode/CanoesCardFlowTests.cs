#nullable enable
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La tarjeta de las Canoas del 31 en el flujo (tarea B11, parche B11-flow; 02 §13: «31 Canoas + Cuia de rescate (tarjeta de
    /// 1 pantalla antes del nivel)»; 04 §7.1, «Debut»; D-033, Q-E4): LOADING → LEVEL_CARD con <c>lives</c> en el primer nivel con
    /// canoas, solo si las canoas cuentan (D-038: con <c>biz_feature_lives</c> apagado, no) y solo al entrar.
    /// </summary>
    public sealed class CanoesCardFlowTests
    {
        [Test]
        public void ForCanoes_IsAnUnlabeledDebut_WithTheLivesId()
        {
            LevelCardInfo card = LevelCardInfo.ForCanoes(31);
            Assert.That(LevelCardInfo.CanoesIntro, Is.EqualTo("lives"), "glosario §6.3: ID del sistema de las Canoas");
            Assert.That(card.Level, Is.EqualTo(31));
            Assert.That(card.IntroMechanic, Is.EqualTo(LevelCardInfo.CanoesIntro));
            Assert.That(card.Kind, Is.EqualTo(LevelCardKind.Debut), "sin etiqueta, como la de la Crecida del 25");
            Assert.That(card.RewardSeeds, Is.EqualTo(0));
            Assert.That(card.MechanicMask, Is.EqualTo(0));
        }

        [Test]
        public void Level31_OpensTheCanoesCard()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 30); // el 31 juega el tablero del 1
            fx.Flow.LevelCardsEnabled = true;
            fx.Flow.Load(31);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            LevelCardInfo card = fx.Flow.LevelCard!;
            Assert.That(card.IntroMechanic, Is.EqualTo(LevelCardInfo.CanoesIntro));
            Assert.That(card.Kind, Is.EqualTo(LevelCardKind.Debut));
            Assert.That(fx.Flow.DismissLevelCard(), Is.True, "1 toque");
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [Test]
        public void WithoutLives_NoCard_D038()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 30);
            fx.Wallet.LivesEnabled = false; // biz_feature_lives = false (08 §9; D-038): no hay canoas en ninguna parte
            fx.Flow.LevelCardsEnabled = true;
            fx.Flow.Load(31);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(fx.Flow.LevelCard, Is.Null);
        }

        [Test]
        public void OtherLevels_NoCard([Values(30, 32)] int level)
        {
            var fx = new FunnelFixture(offset: 29, maxLevel: level - 1); // el 30 juega el tablero del 1, el 32 el del 3
            fx.Flow.LevelCardsEnabled = true;
            fx.Flow.Load(level);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "solo en el primer nivel con canoas (rescue_min_level)");
        }

        [Test]
        public void FollowsRescueMinLevel()
        {
            var fx = new FunnelFixture(offset: 34, maxLevel: 34);
            fx.Wallet.Config.RescueMinLevel = 35;
            fx.Flow.LevelCardsEnabled = true;
            fx.Flow.Load(35);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "rescue_min_level = 35: el primer nivel con canoas");
            Assert.That(fx.Flow.LevelCard!.IntroMechanic, Is.EqualTo(LevelCardInfo.CanoesIntro));
        }

        [Test]
        public void Retry_NoCard()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 30);
            fx.Flow.LevelCardsEnabled = true;
            fx.Flow.Load(31);
            fx.Flow.DismissLevelCard();
            fx.TickIntro();
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(fx.Flow.PressRestart(), Is.True);
            Assert.That(fx.Flow.Phase, Is.Not.EqualTo(FlowPhase.LevelCard), "no en el reintento, como las demás tarjetas");
        }

        [Test]
        public void ALabeledLevelKeepsItsOwnCard()
        {
            // Si el primer nivel con canoas trae su propia tarjeta (una etiqueta o un estreno), esa gana. En la campaña el 31 es
            // normal y sin estreno; aquí, con rescue_min_level en el 40 (el primer Difícil, 03 §11).
            var fx = new FunnelFixture(pack: null, maxLevel: 39);
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            Assert.That(catalog.Find(40)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard), "condición del test");
            EconomyConfig config = fx.Wallet.Config;
            config.RescueMinLevel = 40;
            var flow = new LevelFlow(catalog, fx.Progress, fx.Listener, GameConfig.TapSwitchSelection, AF.Core.Rules.PourModes.Default, fx.Wallet)
            {
                LevelCardsEnabled = true,
            };
            flow.Load(40);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.LevelCard!.Kind, Is.EqualTo(LevelCardKind.Difficulty), "la del Difícil");
        }
    }
}
