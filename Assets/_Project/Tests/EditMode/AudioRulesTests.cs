#nullable enable
using System;
using AF.Audio;
using AF.Audio.Editor;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation;
using AF.Presentation.Mechanics;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Reglas del audio provisional (tarea AU1; 07 §9.4 y 02 §4): ajustes que se aplican al momento, modo silencio del teléfono,
    /// silencio durante los anuncios con el fundido de 0,5 s, atenuación del lecho, nunca háptico en un rechazo, capas de
    /// percusión, ticks de semillas, ganchos de la vista (la nota de la fruta, la Tinaja, los pasos de mecánica y la intro) y el
    /// permiso <c>VIBRATE</c> del manifiesto.
    /// </summary>
    public sealed class AudioRulesTests
    {
        private FakeAudioService _fake = null!;

        [SetUp]
        public void SetUp()
        {
            _fake = new FakeAudioService();
            AudioCues.Install(_fake);
        }

        [TearDown]
        public void TearDown()
        {
            AudioCues.Install(null);
        }

        [Test]
        public void Mix_SoundMusicAndHaptics_SwitchOffSeparately_AndAtOnce()
        {
            var mix = new AudioMix();
            Assert.That(mix.SfxAudible && mix.MusicAudible && mix.HapticsAllowed, Is.True, "07 §9.4: todo encendido por defecto");
            mix.Settings = new SoundSettings(false, true, true);
            Assert.That(mix.SfxAudible, Is.False);
            Assert.That(mix.SfxGain, Is.EqualTo(0f));
            Assert.That(mix.MusicAudible && mix.HapticsAllowed, Is.True, "solo el Sonido");
            mix.Settings = new SoundSettings(true, false, true);
            Assert.That(mix.SfxAudible && !mix.MusicAudible && mix.HapticsAllowed, Is.True, "solo la Música");
            mix.Settings = new SoundSettings(true, true, false);
            Assert.That(mix.SfxAudible && mix.MusicAudible && !mix.HapticsAllowed, Is.True, "solo la Vibración");
        }

        [Test]
        public void Mix_RespectsThePhoneSilentMode()
        {
            var mix = new AudioMix { Ringer = RingerMode.Vibrate };
            Assert.That(mix.SfxAudible || mix.MusicAudible, Is.False, "en vibración no suena");
            Assert.That(mix.HapticsAllowed, Is.True, "pero vibra");
            mix.Ringer = RingerMode.Silent;
            Assert.That(mix.HapticsAllowed, Is.False, "en silencio total tampoco vibra");
            mix.Ringer = RingerMode.Normal;
            Assert.That(mix.SfxAudible, Is.True);
        }

        [Test]
        public void Mix_AdSilencesEverything_AndComesBackWithA05sFade()
        {
            var mix = new AudioMix();
            mix.SetAdActive(true);
            Assert.That(mix.SfxAudible || mix.MusicAudible || mix.HapticsAllowed, Is.False, "07 §9.4: todo se detiene durante el anuncio");
            mix.Tick(1000f);
            Assert.That(mix.SfxGain, Is.EqualTo(0f));
            mix.SetAdActive(false);
            Assert.That(mix.SfxAudible, Is.True);
            Assert.That(mix.SfxGain, Is.EqualTo(0f), "vuelve desde 0");
            mix.Tick(250f);
            Assert.That(mix.SfxGain, Is.EqualTo(0.5f).Within(1e-4f), "a mitad del fundido");
            mix.Tick(250f);
            Assert.That(mix.SfxGain, Is.EqualTo(1f), "0,5 s");
            Assert.That(AudioMix.AdFadeMs, Is.EqualTo(500f));
        }

        [Test]
        public void Mix_DucksTheBed6Db_DuringTheToadaAndTheModals()
        {
            var mix = new AudioMix();
            Assert.That(mix.MusicGain, Is.EqualTo(1f));
            mix.ToadaStarted();
            Assert.That(AudioAnalysis.ToDb(mix.MusicGain), Is.EqualTo(-6f).Within(0.01f), "07 §9.4: −6 dB durante la toada");
            mix.Tick(AudioMix.ToadaDuckMs + 1f);
            Assert.That(mix.MusicGain, Is.EqualTo(1f), "vuelve al acabar la toada");
            mix.ModalDuck = true;
            Assert.That(AudioAnalysis.ToDb(mix.MusicGain), Is.EqualTo(-6f).Within(0.01f), "y con el modal");
            Assert.That(mix.SfxGain, Is.EqualTo(1f), "los efectos no se atenúan");
        }

        [Test]
        public void PercussionLayers_FirstAtTheFirstSeal_ThenOneEvery25Percent_UpTo2()
        {
            Assert.That(AudioMix.PercussionLayers(0, 8, 2), Is.EqualTo(0), "sin sellar, solo el lecho");
            Assert.That(AudioMix.PercussionLayers(1, 8, 2), Is.EqualTo(1), "la primera cuia sellada");
            Assert.That(AudioMix.PercussionLayers(2, 8, 2), Is.EqualTo(2), "25 %");
            Assert.That(AudioMix.PercussionLayers(8, 8, 2), Is.EqualTo(2), "audio_music_percussion_layers = 2");
            Assert.That(AudioMix.PercussionLayers(8, 8, 3), Is.EqualTo(3));
            Assert.That(AudioMix.PercussionLayers(3, 8, 0), Is.EqualTo(0));
            Assert.That(AudioBoard.MusicPercussionLayers, Is.EqualTo(2), "08 §9");
        }

        [Test]
        public void SeedTicker_OneEvery10Seeds_AtMost20()
        {
            var t = new SeedTicker();
            t.Begin(0, 35);
            Assert.That(t.Total, Is.EqualTo(3));
            Assert.That(t.Advance(9), Is.EqualTo(0));
            Assert.That(t.Advance(10), Is.EqualTo(1));
            Assert.That(t.Advance(35), Is.EqualTo(2), "las que faltaban");
            Assert.That(t.Advance(35), Is.EqualTo(0));

            t.Begin(0, 1000);
            Assert.That(t.Total, Is.EqualTo(20), "07 §9.2: máx. 20 ticks");
            int sum = 0;
            for (int v = 0; v <= 1000; v += 7)
            {
                sum += t.Advance(v);
            }

            sum += t.Advance(1000);
            Assert.That(sum, Is.EqualTo(20));

            t.Begin(40, 80);
            Assert.That(t.Advance(80), Is.EqualTo(4), "la Cosecha doble cuenta desde +N");
        }

        [Test]
        public void Haptics_HaveTheirAndroidEffects_AndNoneIsForARejection()
        {
            Assert.That(Enum.GetNames(typeof(HapticKind)), Is.EqualTo(new[] { "Seal", "Win", "Capi", "Boto", "BackBlocked" }), "02 §4: nunca háptico en rechazo");
            Assert.That(Haptics.Name(HapticKind.Seal), Is.EqualTo("haptic_seal"));
            Assert.That(Haptics.Name(HapticKind.BackBlocked), Is.EqualTo("haptic_back_blocked"));
            Assert.That(Haptics.StrengthOf(HapticKind.Seal), Is.EqualTo(HapticStrength.Light));
            Assert.That(Haptics.FallbackMs(HapticStrength.Light), Is.EqualTo(20), "07 §9.2: sellado de 20 ms");
            Assert.That(Haptics.PredefinedEffect(HapticStrength.Light), Is.EqualTo(Haptics.EffectTick));
            foreach (HapticKind k in new[] { HapticKind.Win, HapticKind.Capi, HapticKind.Boto })
            {
                Assert.That(Haptics.StrengthOf(k), Is.EqualTo(HapticStrength.Medium), k.ToString());
                Assert.That(Haptics.PredefinedEffect(Haptics.StrengthOf(k)), Is.EqualTo(Haptics.EffectClick));
            }

            Assert.That(Haptics.StrengthOf(HapticKind.BackBlocked), Is.EqualTo(HapticStrength.Short));
        }

        [Test]
        public void Cues_Seal_PlaysTheMaracaAndTheLightHaptic_AndSettingsMuteAtOnce()
        {
            AudioCues.Seal();
            Assert.That(_fake.Heard(SfxId.Seal), Is.True);
            Assert.That(_fake.HapticCount(HapticKind.Seal), Is.EqualTo(1));

            AudioCues.Configure(false, true, false);
            _fake.Clear();
            AudioCues.Seal();
            Assert.That(_fake.Requested(SfxId.Seal) && !_fake.Heard(SfxId.Seal), Is.True, "Sonido apagado: se pide y no suena");
            Assert.That(_fake.HapticTotal(), Is.EqualTo(0), "Vibración apagada");
        }

        [Test]
        public void Cues_AButtonSoundsOnce_ItsOwnSoundOrTheTap()
        {
            int before = AudioCues.Serial;
            AudioCues.TapIfQuiet(before);
            Assert.That(_fake.Count(SfxId.UiTap, false), Is.EqualTo(1), "sin sonido propio, el toque");

            _fake.Clear();
            before = AudioCues.Serial;
            AudioCues.Play(SfxId.UiBuy);
            AudioCues.TapIfQuiet(before);
            Assert.That(_fake.Count(SfxId.UiTap, false), Is.EqualTo(0), "con el suyo (comprar), sin toque encima");
            Assert.That(_fake.Heard(SfxId.UiBuy), Is.True);

            _fake.Clear();
            before = AudioCues.Serial;
            AudioCues.BackIfQuiet(before);
            Assert.That(_fake.Heard(SfxId.UiBack), Is.True, "Atrás que cierra algo");
        }

        [Test]
        public void Cues_PourBeginAndEnd_SoundTheStartAndTheEnd_AndCutIsSilent()
        {
            AudioCues.PourBegin(2, 0.25f);
            AudioCues.PourLevel(2, 0.75f);
            Assert.That(_fake.ActivePours, Is.EqualTo(1));
            Assert.That(_fake.LastPourLevel, Is.EqualTo(0.75f));
            AudioCues.PourEnd(2);
            Assert.That(_fake.Heard(SfxId.PourStart) && _fake.Heard(SfxId.PourEnd), Is.True);
            Assert.That(_fake.ActivePours, Is.EqualTo(0));

            _fake.Clear();
            AudioCues.PourBegin(1, 0f);
            AudioCues.PourCut(1);
            Assert.That(_fake.Requested(SfxId.PourEnd), Is.False, "un salto calla el chorro sin sfx_pour_end");
            Assert.That(_fake.ActivePours, Is.EqualTo(0));
        }

        [Test]
        public void Select_PlaysTheNoteOfTheTopFruit_TheTinajaToc_AndNothingForAHiddenTop()
        {
            BoardState board = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 9, 4 }),
                Cuia.Empty(4),
                Cuia.Create(4, new[] { 9, 4 }, 0b01),
            });
            AudioBoard.Select(board, 0);
            Assert.That(_fake.Events[0].Sfx, Is.EqualTo(SfxId.Select));
            Assert.That(_fake.Events[0].Tint, Is.EqualTo(4), "la fruta de arriba (aguaje)");
            AudioBoard.Select(board, 1);
            Assert.That(_fake.Events[1].Tint, Is.EqualTo(0), "vacía: la nota del laboratorio");
            AudioBoard.Select(board, 2);
            Assert.That(_fake.Events[2].Tint, Is.EqualTo(4), "con una dosis oculta debajo (Aguas turbias), la de arriba, que se ve (R7)");
            AudioBoard.Deselect(board, 0);
            Assert.That(_fake.Events[3].SelectDown && _fake.Events[3].Tint == 4, Is.True, "bajar: la variante grave");
            AudioBoard.Select(board, 7);
            Assert.That(_fake.Events.Count, Is.EqualTo(4), "fuera del tablero, nada");
        }

        [Test]
        public void MechanicSteps_PlayTheirSoundOfTheList()
        {
            AudioBoard.Step(MechanicStepKind.Open, MechanicType.Hoja);
            AudioBoard.Step(MechanicStepKind.Open, MechanicType.Brote);
            AudioBoard.Step(MechanicStepKind.Open, MechanicType.HojaAtada);
            AudioBoard.Step(MechanicStepKind.Release, MechanicType.Liana);
            AudioBoard.Step(MechanicStepKind.Receive, MechanicType.Remolino);
            AudioBoard.Step(MechanicStepKind.MudWash, MechanicType.None);
            AudioBoard.Step(MechanicStepKind.RainDrop, MechanicType.Lluvia);
            AudioBoard.Step(MechanicStepKind.Bite, MechanicType.Pirana);
            AudioBoard.Step(MechanicStepKind.Seal, MechanicType.Matapi);
            SfxId[] expected =
            {
                SfxId.MechHoja, SfxId.MechBrote, SfxId.MechHojaAtada, SfxId.MechLiana, SfxId.MechRemolino, SfxId.MechBarro,
                SfxId.MechLluvia, SfxId.MechPirana, SfxId.MechMatapi,
            };
            Assert.That(_fake.Events.Count, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(_fake.Events[i].Sfx, Is.EqualTo(expected[i]), "paso " + i);
            }

            _fake.Clear();
            AudioBoard.Step(MechanicStepKind.Seal, MechanicType.None);
            AudioBoard.Step(MechanicStepKind.RainWait, MechanicType.Lluvia);
            AudioBoard.Step(MechanicStepKind.BiteLost, MechanicType.Pirana);
            AudioBoard.Step(MechanicStepKind.Reveal, MechanicType.None);
            Assert.That(_fake.Events, Is.Empty, "el sellado lo pone la tapa; la espera, el mordisco perdido y el revelado por regla, nada");
        }

        [Test]
        public void IntroCues_FollowThePlan_RiverFillPerRowPlipPerFruitAndTintPerRun()
        {
            IntroInput input = IntroChoreographyTests.Input(16, 780f, 1400f, IntroPerch.Branch);
            var plan = new IntroChoreography();
            plan.Plan(input);
            var cues = new IntroAudioCues();
            cues.Begin(plan, input, false);
            for (float t = 0f; t <= 1400f; t += 16f)
            {
                cues.Tick(t);
            }

            cues.Tick(1400f);
            int blooming = 0;
            for (int r = 0; r < plan.RunCount; r++)
            {
                if (plan.RunBloomMs[r] >= 0f)
                {
                    blooming++;
                }
            }

            Assert.That(_fake.Count(SfxId.RiverRise, false), Is.EqualTo(1));
            Assert.That(_fake.Count(SfxId.Fill, false), Is.EqualTo(plan.Rows), "un chorro por fila");
            Assert.That(_fake.Count(SfxId.FruitPlip, false), Is.EqualTo(plan.FruitCount), "un plic por fruta");
            Assert.That(_fake.Count(SfxId.Tint, false), Is.EqualTo(blooming), "una nota por racha");
            Assert.That(_fake.Count(SfxId.MonkeyChirp, false), Is.LessThanOrEqualTo(1), "como mucho un chillido");

            _fake.Clear();
            cues.Begin(plan, input, true);
            cues.Tick(1400f);
            Assert.That(_fake.Events, Is.Empty, "con Reducir movimiento, sin sonidos de la intro");

            cues.Begin(plan, input, false);
            cues.Tick(1f);
            cues.Stop();
            _fake.Clear();
            cues.Tick(1400f);
            Assert.That(_fake.Events, Is.Empty, "saltar la intro calla lo que faltaba");
        }

        [Test]
        public void Probe_WritesSfxAndHapticLines_OnlyOutsideTheEditorUnlessAsked()
        {
            var probe = new AF.UI.DevProbe();
            probe.Sfx("sfx_select", -36f, 4);
            Assert.That(probe.LastSfxLine, Is.Null, "en el Editor no escribe una línea por efecto (sin asignaciones en el bucle)");
            try
            {
                AF.UI.DevProbe.AudioLinesInEditor = true;
                probe.Sfx("sfx_select", -36f, 4);
                probe.Sfx("sfx_seal", -17f, 0);
                probe.Haptic("haptic_seal");
            }
            finally
            {
                AF.UI.DevProbe.AudioLinesInEditor = false;
            }

            StringAssert.StartsWith("AF_STATE {\"event\":\"sfx\",\"t\":", probe.LastSfxLine);
            StringAssert.EndsWith(",\"id\":\"sfx_seal\",\"gain_db\":-17.0}", probe.LastSfxLine, "sin fruta, sin tint");
            StringAssert.StartsWith("AF_STATE {\"event\":\"haptic\",\"t\":", probe.LastHapticLine);
            StringAssert.EndsWith(",\"kind\":\"haptic_seal\"}", probe.LastHapticLine);
        }

        [Test]
        public void VibratePermission_IsAddedOnce_AndNothingElse()
        {
            const string manifest = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\">\n"
                + "  <uses-permission android:name=\"android.permission.INTERNET\" />\n  <application android:label=\"x\">\n  </application>\n</manifest>\n";
            string once = AndroidVibratePermission.WithVibrate(manifest);
            StringAssert.Contains("  <uses-permission android:name=\"android.permission.VIBRATE\" />\n  <application", once);
            Assert.That(Count(once, "<uses-permission"), Is.EqualTo(2), "solo VIBRATE se añade");
            Assert.That(AndroidVibratePermission.WithVibrate(once), Is.EqualTo(once), "si ya está, no cambia");
            Assert.That(Count(AndroidVibratePermission.WithVibrate("<manifest>\n</manifest>"), "android.permission.VIBRATE"), Is.EqualTo(1), "sin <application>, antes del cierre");
            Assert.Throws<FormatException>(() => AndroidVibratePermission.WithVibrate("<x/>"));
        }

        private static int Count(string text, string what)
        {
            int n = 0;
            for (int at = text.IndexOf(what, StringComparison.Ordinal); at >= 0; at = text.IndexOf(what, at + what.Length, StringComparison.Ordinal))
            {
                n++;
            }

            return n;
        }
    }
}
