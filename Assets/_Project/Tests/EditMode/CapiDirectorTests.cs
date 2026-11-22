#nullable enable
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La elección de gestos y emociones de Capi al remo (D-069, M2-3; propuesta del Mapa v2 §5.2-§5.4 y §7.3): qué gesto trae cada
    /// momento, una emoción por viaje con su prioridad, la cola, el bucle de remar y Reducir movimiento (quieto 1,2 s).
    /// </summary>
    public sealed class CapiDirectorTests
    {
        [Test]
        public void EachMoment_HasTheGestureOfTheProposal()
        {
            var g = new CapiGesture[3];
            Assert.That(CapiDirector.GesturesFor(CapiMoment.TownPass, g), Is.EqualTo(1));
            Assert.That(g[0], Is.EqualTo(CapiGesture.Wave), "pasar un pueblo: saluda");
            Assert.That(CapiDirector.GesturesFor(CapiMoment.Lead, g), Is.EqualTo(1));
            Assert.That(g[0], Is.EqualTo(CapiGesture.Lookout), "la anticipación de D-053: la pata de visera");
            Assert.That(CapiDirector.GesturesFor(CapiMoment.Mouth, g), Is.EqualTo(1));
            Assert.That(g[0], Is.EqualTo(CapiGesture.Lookout), "una boca: mira el agua nueva");
            Assert.That(CapiDirector.GesturesFor(CapiMoment.Rapids, g), Is.EqualTo(1));
            Assert.That(g[0], Is.EqualTo(CapiGesture.PaddleHard), "los rápidos: rema fuerte");
            Assert.That(CapiDirector.GesturesFor(CapiMoment.Arrival, g), Is.EqualTo(2));
            Assert.That(g[0], Is.EqualTo(CapiGesture.CheerOar), "la llegada: el remo en alto");
            Assert.That(g[1], Is.EqualTo(CapiGesture.Tie), "y amarra la muestra");
            Assert.That(CapiDirector.GesturesFor(CapiMoment.WaterChange, g), Is.EqualTo(1));
            Assert.That(g[0], Is.EqualTo(CapiGesture.LeanOver), "el Encuentro: se asoma por la borda");
            Assert.That(CapiDirector.GesturesFor(CapiMoment.CountryChange, g), Is.EqualTo(1));
            Assert.That(g[0], Is.EqualTo(CapiGesture.Wave), "la bandera: la saluda");
            Assert.That(CapiDirector.GesturesFor(CapiMoment.Depart, g), Is.EqualTo(0));
            Assert.That(CapiDirector.GesturesFor(CapiMoment.NewWorld, g), Is.EqualTo(0));
        }

        [Test]
        public void Durations_AreThoseOfTheProposal()
        {
            Assert.That(CapiDirector.DurationMs(CapiGesture.Lookout), Is.EqualTo(250f));
            Assert.That(CapiDirector.DurationMs(CapiGesture.Wave), Is.EqualTo(600f));
            Assert.That(CapiDirector.DurationMs(CapiGesture.CheerOar), Is.EqualTo(500f));
            Assert.That(CapiDirector.DurationMs(CapiGesture.Tie), Is.EqualTo(400f));
            Assert.That(CapiDirector.DurationMs(CapiGesture.Paddle), Is.EqualTo(0f), "el remo es un bucle");
            Assert.That(CapiDirector.IsLoop(CapiGesture.Paddle), Is.True);
        }

        [Test]
        public void OneEmotionPerJourney_TheRareOneWins()
        {
            int departArrival = CapiDirector.Bit(CapiMoment.Depart) | CapiDirector.Bit(CapiMoment.Arrival);
            Assert.That(CapiDirector.PickEmotion(CapiDirector.Bit(CapiMoment.Depart)), Is.EqualTo(CapiEmotion.Note), "♪ remando");
            Assert.That(CapiDirector.PickEmotion(departArrival), Is.EqualTo(CapiEmotion.Sample), "la muestra que se llena");
            Assert.That(CapiDirector.PickEmotion(departArrival | CapiDirector.Bit(CapiMoment.WaterChange)), Is.EqualTo(CapiEmotion.Bang), "! al cambiar el agua");
            Assert.That(CapiDirector.PickEmotion(departArrival | CapiDirector.Bit(CapiMoment.Join)), Is.EqualTo(CapiEmotion.Heart), "♥ al unirse alguien");
            Assert.That(CapiDirector.PickEmotion(departArrival | CapiDirector.Bit(CapiMoment.Join) | CapiDirector.Bit(CapiMoment.ContentEnd)), Is.EqualTo(CapiEmotion.Question));
            Assert.That(CapiDirector.PickEmotion(CapiDirector.Bit(CapiMoment.TownPass)), Is.EqualTo(CapiEmotion.None));
        }

        [Test]
        public void TheEmotion_ShowsOnceAtItsMoment_For1200ms()
        {
            var d = new CapiDirector();
            d.Begin(false, CapiEmotion.Bang);
            d.Traveling = true;
            d.React(CapiMoment.Depart);
            Assert.That(d.Emotion, Is.EqualTo(CapiEmotion.None), "♪ no es la del viaje");
            d.React(CapiMoment.Mouth);
            Assert.That(d.Emotion, Is.EqualTo(CapiEmotion.Bang));
            d.Tick(CapiDirector.EmotionMs - 1f);
            Assert.That(d.Emotion, Is.EqualTo(CapiEmotion.Bang));
            d.Tick(2f);
            Assert.That(d.Emotion, Is.EqualTo(CapiEmotion.None), "1,2 s");
            d.React(CapiMoment.WaterChange);
            Assert.That(d.Emotion, Is.EqualTo(CapiEmotion.None), "como mucho una por viaje");
        }

        [Test]
        public void Gestures_Queue_AndThePaddleLoopIsUnderneath()
        {
            var d = new CapiDirector();
            d.Begin(false, CapiEmotion.None);
            d.Traveling = true;
            d.Tick(16f);
            Assert.That(d.Current, Is.EqualTo(CapiGesture.Paddle), "viajando, rema");
            d.React(CapiMoment.TownPass);
            Assert.That(d.Current, Is.EqualTo(CapiGesture.Wave));
            d.React(CapiMoment.Arrival);
            Assert.That(d.Queued, Is.EqualTo(2), "el remo en alto y amarrar esperan al saludo");
            d.Tick(300f);
            Assert.That(d.Phase, Is.EqualTo(0.5f).Within(0.01f));
            d.Tick(300f);
            Assert.That(d.Current, Is.EqualTo(CapiGesture.CheerOar));
            d.Traveling = false;
            d.Tick(500f);
            Assert.That(d.Current, Is.EqualTo(CapiGesture.Tie));
            d.Tick(400f);
            Assert.That(d.Current, Is.EqualTo(CapiGesture.Rest), "quieto al acabar, sin viaje");
            Assert.That(d.Animating, Is.False);
        }

        [Test]
        public void ReduceMotion_TheKeyPoseStillFor1200ms_OnlyTheFirstGesture_AndTheEmotionStill()
        {
            var d = new CapiDirector();
            d.Begin(true, CapiEmotion.Sample);
            d.React(CapiMoment.Arrival);
            Assert.That(d.Still, Is.True);
            Assert.That(d.Current, Is.EqualTo(CapiGesture.CheerOar));
            Assert.That(d.Queued, Is.EqualTo(0), "solo el primer gesto de la reacción");
            Assert.That(d.Emotion, Is.EqualTo(CapiEmotion.Sample));
            float phase = d.Phase;
            CapiPose before = CapiDirector.Pose(d.Current, d.Phase);
            d.Tick(700f);
            Assert.That(d.Phase, Is.EqualTo(phase), "quieto");
            Assert.That(CapiDirector.Pose(d.Current, d.Phase).Arm, Is.EqualTo(before.Arm));
            Assert.That(CapiDirector.BubbleScale(d.EmotionElapsedMs, true), Is.EqualTo(1f), "la burbuja, sin rebote");
            d.Tick(CapiDirector.StillMs - 700f - 1f);
            Assert.That(d.Current, Is.EqualTo(CapiGesture.CheerOar));
            Assert.That(d.Emotion, Is.EqualTo(CapiEmotion.Sample));
            d.Tick(2f);
            Assert.That(d.Current, Is.EqualTo(CapiGesture.Rest), "1,2 s");
            Assert.That(d.Emotion, Is.EqualTo(CapiEmotion.None));

            d.Traveling = true;
            d.Tick(16f);
            Assert.That(d.Current, Is.EqualTo(CapiGesture.Rest), "con Reducir movimiento no hay bucle de remar");
        }

        [Test]
        public void ShowPlanned_ShowsTheEmotionAtOnce_OnlyOnce()
        {
            var d = new CapiDirector();
            d.Begin(true, CapiEmotion.Bang);
            d.ShowPlanned();
            Assert.That(d.Emotion, Is.EqualTo(CapiEmotion.Bang));
            d.Tick(CapiDirector.StillMs + 1f);
            d.ShowPlanned();
            Assert.That(d.Emotion, Is.EqualTo(CapiEmotion.None));
        }

        [Test]
        public void Poses_CheerRaisesTheOar_LookoutPutsThePawUp_TieCarriesTheSample()
        {
            CapiPose cheer = CapiDirector.Pose(CapiGesture.CheerOar, 0.5f);
            Assert.That(cheer.PaddleHeld, Is.True);
            Assert.That(cheer.Paddle, Is.GreaterThan(150f), "el remo en alto");
            CapiPose look = CapiDirector.Pose(CapiGesture.Lookout, 1f);
            Assert.That(look.Arm, Is.InRange(50f, 75f), "la pata de visera, a la frente (sobre el ojo)");
            Assert.That(look.PaddleHeld, Is.False);
            CapiPose tie = CapiDirector.Pose(CapiGesture.Tie, 0.5f);
            Assert.That(tie.Sample, Is.EqualTo(0.5f));
            Assert.That(CapiDirector.Pose(CapiGesture.Paddle, 0.3f).Sample, Is.LessThan(0f));
            Assert.That(CapiDirector.Pose(CapiGesture.PaddleHard, 0.1f).Splash, Is.GreaterThan(0f), "salpicaduras");
            Assert.That(CapiDirector.Pose(CapiGesture.Paddle, 0.1f).Splash, Is.EqualTo(0f));
        }

        [Test]
        public void TheBubble_PopsIn_AndShrinksOut()
        {
            Assert.That(CapiDirector.BubbleScale(-1f, false), Is.EqualTo(0f));
            Assert.That(CapiDirector.BubbleScale(0f, false), Is.EqualTo(0f).Within(0.01f));
            Assert.That(CapiDirector.BubbleScale(CapiDirector.BubbleInMs + 10f, false), Is.EqualTo(1f));
            Assert.That(CapiDirector.BubbleScale(CapiDirector.EmotionMs - 100f, false), Is.EqualTo(0.5f).Within(0.01f));
        }

        [Test]
        public void Rapids_AreTeotonio_TheOtherMarksAreMouths()
        {
            Assert.That(RiverMapScreen.IsRapids("teotonio"), Is.True);
            Assert.That(RiverMapScreen.IsRapids("boca_del_purus"), Is.False);
        }
    }
}
