#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Los marcos de país del avatar sin motor (Q-M6 de D-033, añadido del 2026-11-08): el catálogo de 13 con el id de la Bitácora
    /// (Q-B6) y su país, el marco que se ve (el elegido, «Sin marco» o, sin decisión, el del mundo más alto), ponerlo al ganarlo,
    /// elegirlo y quitarlo, lo visto y el extra de desarrollo.
    /// </summary>
    public sealed class AvatarFramesTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

        private static (AvatarFrameService service, AvatarData data, List<string> owned, Func<int> saves) NewService(params string[] owned)
        {
            var data = new AvatarData { Id = "toucan" };
            var list = new List<string>(owned);
            int count = 0;
            var service = new AvatarFrameService(data, list, () => count++, new ManualClock(T0));
            return (service, data, list, () => count);
        }

        [Test]
        public void Catalog_Has13_WithTheLogbookIds_AndTheirCountries()
        {
            Assert.That(AvatarFrames.All.Count, Is.EqualTo(13));
            string[] countries = { "BR", "BR", "BR", "CO", "PE", "EC", "PE", "BO", "PE", null!, null!, null!, "PE" };
            for (int w = 1; w <= 13; w++)
            {
                AvatarFrameInfo f = AvatarFrames.All[w - 1];
                Assert.That(f.World, Is.EqualTo(w));
                Assert.That(f.Id, Is.EqualTo(LoginCalendarRules.FrameId(w)), "el id de la Bitácora (Q-B6)");
                Assert.That(f.Country, Is.EqualTo(countries[w - 1]), "glosario §8, columna «País»");
                Assert.That(AvatarFrames.Find(f.Id), Is.SameAs(f));
                Assert.That(AvatarFrames.WorldOf(f.Id), Is.EqualTo(w));
            }

            Assert.That(AvatarFrames.Find("frame_w14"), Is.Null);
            Assert.That(AvatarFrames.Find("frame_w1"), Is.Null);
            Assert.That(AvatarFrames.Find("af_skin_m02"), Is.Null);
            Assert.That(AvatarFrames.Find(null), Is.Null);
        }

        [Test]
        public void Effective_WithoutADecision_IsTheHighestWorldWon_AndNoneWithoutFrames()
        {
            var data = new AvatarData();
            Assert.That(AvatarFrames.Effective(data, new List<string>()), Is.Null, "sin marcos, el aro de madera");
            Assert.That(AvatarFrames.Effective(data, new List<string> { "frame_w02", "frame_w05", "frame_w03", "raro" })!.World, Is.EqualTo(5),
                "un perfil de la 1.0 con marcos lleva el del mundo más alto [P]");
        }

        [Test]
        public void Effective_FollowsTheDecision_AndANotOwnedChoiceIsNone()
        {
            var owned = new List<string> { "frame_w01", "frame_w04" };
            var data = new AvatarData { Frame = "frame_w01", FrameAt = T0 };
            Assert.That(AvatarFrames.Effective(data, owned)!.Id, Is.EqualTo("frame_w01"));
            data.Frame = null;
            Assert.That(AvatarFrames.Effective(data, owned), Is.Null, "«Sin marco» elegido");
            data.Frame = "frame_w07";
            Assert.That(AvatarFrames.Effective(data, owned), Is.Null, "uno que no es suyo no se lleva");
        }

        [Test]
        public void PutOnEarned_PutsIt_AndSelectChangesOrTakesItOff()
        {
            var (service, data, owned, saves) = NewService("frame_w01");
            int changed = 0;
            service.Changed += () => changed++;
            owned.Add("frame_w03"); // lo apunta la Bitácora
            Assert.That(service.PutOnEarned("frame_w03"), Is.True);
            Assert.That(service.Current!.Id, Is.EqualTo("frame_w03"), "el ganado se pone solo (como la skin, Q-M8)");
            Assert.That(data.FrameAt, Is.EqualTo(T0));
            Assert.That(service.PutOnEarned("frame_w05"), Is.False, "no es suyo");

            Assert.That(service.Select("frame_w01"), Is.EqualTo(AvatarFrameSelectResult.Selected));
            Assert.That(service.Current!.Id, Is.EqualTo("frame_w01"));
            Assert.That(service.Select("frame_w01"), Is.EqualTo(AvatarFrameSelectResult.AlreadyChosen));
            Assert.That(service.Select(null), Is.EqualTo(AvatarFrameSelectResult.Selected));
            Assert.That(service.Current, Is.Null);
            Assert.That(data.FrameAt, Is.Not.Null, "«Sin marco» queda decidido");
            Assert.That(service.Select("frame_w09"), Is.EqualTo(AvatarFrameSelectResult.NotOwned));
            Assert.That(service.Select("cuia_vidrio"), Is.EqualTo(AvatarFrameSelectResult.NotOwned));
            Assert.That(changed, Is.EqualTo(3));
            Assert.That(saves(), Is.EqualTo(3));
        }

        [Test]
        public void ConfirmingTheDeducedOne_SavesTheDecision()
        {
            var (service, data, _, saves) = NewService("frame_w02");
            Assert.That(service.Current!.Id, Is.EqualTo("frame_w02"));
            Assert.That(service.Select("frame_w02"), Is.EqualTo(AvatarFrameSelectResult.AlreadyChosen));
            Assert.That(data.Frame, Is.EqualTo("frame_w02"));
            Assert.That(data.FrameAt, Is.EqualTo(T0));
            Assert.That(saves(), Is.EqualTo(1));
        }

        [Test]
        public void Seen_TurnsOffTheDot()
        {
            var (service, data, _, _) = NewService("frame_w01", "frame_w02");
            Assert.That(service.AnyUnseen, Is.True);
            Assert.That(service.MarkSeen("frame_w01"), Is.True);
            Assert.That(service.MarkSeen("frame_w01"), Is.False);
            Assert.That(service.AnyUnseen, Is.True);
            Assert.That(service.MarkAllSeen(), Is.EqualTo(1));
            Assert.That(service.AnyUnseen, Is.False);
            Assert.That(data.FramesSeen, Is.EqualTo(new[] { "frame_w01", "frame_w02" }));
            Assert.That(service.MarkSeen("nada"), Is.False);
        }

        [Test]
        public void Grant_AddsTheWorldsFrame_Once()
        {
            var (service, _, owned, _) = NewService();
            Assert.That(service.Grant(4), Is.True);
            Assert.That(service.Grant(4), Is.False);
            Assert.That(service.Grant(0), Is.False);
            Assert.That(service.Grant(14), Is.False);
            Assert.That(owned, Is.EqualTo(new[] { "frame_w04" }));
            Assert.That(service.Owned().Count, Is.EqualTo(1));
        }
    }
}
