#nullable enable
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Las dos llamadas de B14 en el flujo del nivel (LevelFlow.Meta.cs): la skin del mundo ganada y puesta en la victoria de su
    /// último nivel (06 §4.5; Q-M8 (a)), dentro del lote de la victoria, y <c>album.fruits</c> con los tintes nuevos de cada carga
    /// de campaña (08 §4.3; <c>af_fruit_discovered</c>, 08 §8.2). Y que la tabla de ids de tinte del álbum es la del glosario §9.
    /// </summary>
    public sealed class MetaFlowHookTests
    {
        [Test]
        public void WinningThe40_GivesAndPutsOnTheSantaremCuia_AndTheNextLoadClearsIt()
        {
            var rig = new StreakFlowRig(offset: 30);
            Profile profile = Wallet.NewProfile(new ManualClock(EconomyFixture.T0));
            var store = new MemoryProfileStore();
            var skins = new SkinService(profile, store);
            rig.Flow.UseSkins(skins);

            rig.Start(39);
            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Flow.LastSkinUnlock, Is.Null, "el 39 no cierra el mundo");

            rig.Start(40);
            LevelStateTests.Win(rig.Flow);
            SkinUnlock? unlock = rig.Flow.LastSkinUnlock;
            Assert.That(unlock, Is.Not.Null);
            Assert.That(unlock!.SkinId, Is.EqualTo("af_skin_m02"));
            Assert.That(unlock.Level, Is.EqualTo(40));
            Assert.That(skins.Equipped, Is.EqualTo("af_skin_m02"), "Q-M8 (a): se pone sola");
            Assert.That(CuiaSkin.For(skins.Equipped), Is.SameAs(CuiaSkin.Santarem));

            rig.Start(40);
            Assert.That(rig.Flow.LastSkinUnlock, Is.Null, "la carga siguiente limpia el resultado");
            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Flow.LastSkinUnlock, Is.Null, "ya la tiene: no se gana dos veces");
        }

        [Test]
        public void WithoutTheService_NothingHappens()
        {
            var rig = new StreakFlowRig(offset: 30);
            rig.Start(40);
            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Flow.LastSkinUnlock, Is.Null);
            Assert.That(rig.Flow.LastFruitsDiscovered, Is.Empty);
        }

        [Test]
        public void EachLoad_AddsTheNewTintsOfItsBoard_ToTheAlbum()
        {
            var rig = new StreakFlowRig(offset: 0);
            Profile profile = Wallet.NewProfile(new ManualClock(EconomyFixture.T0));
            var store = new MemoryProfileStore();
            var album = new FruitAlbum(profile, store);
            rig.Flow.UseAlbum(album);

            rig.Flow.Load(1);
            Assert.That(rig.Flow.LastFruitsDiscovered, Is.EqualTo(new[] { 4, 8 }), "el 1 estrena el aguaje y el açaí (glosario §9)");
            Assert.That(profile.Album.Fruits, Is.EqualTo(new[] { 4, 8 }));
            Assert.That(rig.Flow.FruitDiscoveryCount, Is.EqualTo(1));
            Assert.That(store.Load().Profile!.Album.Fruits, Is.EqualTo(new[] { 4, 8 }), "guardado con la carga");

            rig.Flow.Load(2);
            Assert.That(rig.Flow.LastFruitsDiscovered, Is.EqualTo(new[] { 3 }), "el 2, el taperebá");
            rig.Flow.Load(1);
            Assert.That(rig.Flow.LastFruitsDiscovered, Is.Empty, "repetir un nivel no descubre nada");
            Assert.That(rig.Flow.FruitDiscoveryCount, Is.EqualTo(2));
            Assert.That(FruitAlbum.TintId(8), Is.EqualTo("acai"));
            Assert.That(FruitAlbum.TintId(0), Is.EqualTo(string.Empty));
        }

        [Test]
        public void TheAlbumTintIds_AreTheGlossaryOnes()
        {
            Assert.That(FruitAlbum.TintIds, Is.EqualTo(ProceduralArt.TintIds), "glosario §9: el número de motor de cada tinte");
            var debut = new HashSet<string>(SightingRule.FruitDebutTints);
            for (int i = AlbumData.MinTint; i <= AlbumData.MaxTint; i++)
            {
                Assert.That(debut.Contains(FruitAlbum.TintIds[i]), Is.True, FruitAlbum.TintIds[i]);
            }
        }
    }
}
