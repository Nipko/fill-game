#nullable enable
using System;
using System.Collections;
using AF.Audio;
using AF.Audio.Unity;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El foco de audio con el servicio real y un puerto falso (observación 10 de la jornada 6: con una alarma sonando, la música
    /// del juego seguía de fondo; D-026, añadido del 2026-10-29): con el tema de menú sonando, una alarma
    /// (<see cref="AudioFocusState.LossTransient"/>) lo pone en pausa y calla los efectos; al terminar (<see cref="AudioFocusState.Gain"/>)
    /// vuelve con el fundido; un aviso por encima (<see cref="AudioFocusState.Duck"/>) lo baja al 20 %; con la Música apagada no se
    /// pide el foco; en segundo plano se suelta; tras otra app de música (<see cref="AudioFocusState.Loss"/>), un anuncio que termina
    /// lo vuelve a pedir; y la sonda <c>AF_STATE audio_focus</c> dice cada cambio.
    /// </summary>
    public sealed class AudioFocusPlayTests
    {
        private UnityAudioService? _service;
        private DevProbe _probe = null!;

        [SetUp]
        public void SetUp()
        {
            AudioCues.Override = null;
            _probe = new DevProbe();
        }

        [TearDown]
        public void TearDown()
        {
            if (_service != null)
            {
                UnityEngine.Object.Destroy(_service.gameObject); // el siguiente usa otro, con el foco de la plataforma
                _service = null;
            }
        }

        [UnityTest]
        public IEnumerator Alarm_PausesMusicAndEffects_Gain_BringsThemBack_Duck_LowersTheMusic()
        {
            var port = new FakeAudioFocusPort();
            UnityAudioService s = Take(port);
            Assert.That(port.Requests, Is.EqualTo(1), "con la Música encendida y en primer plano, se pide el foco");
            Assert.That(s.AudioFocus.State, Is.EqualTo(AudioFocusState.Gain));
            s.SetMenuMusic();
            yield return Wait(() => s.MenuReady && s.MenuPlaying && s.MenuVolume > 0.9f, "el tema de menú a todo volumen", 30f);

            // La alarma.
            port.Push(AudioFocusState.LossTransient);
            yield return null;
            yield return null;
            Assert.That(s.Mix.Focus, Is.EqualTo(AudioFocusState.LossTransient));
            Assert.That(s.Mix.FocusPaused, Is.True);
            Assert.That(s.MenuPlaying, Is.False, "la música, en pausa");
            Assert.That(s.Mix.SfxAudible, Is.False, "los efectos, callados");
            Assert.That(s.Mix.MusicGain, Is.EqualTo(0f));
            if (DevProbe.Enabled)
            {
                Assert.That(_probe.LastAudioFocusLine, Does.Contain("\"state\":\"loss_transient\"").And.Contain("\"paused\":true").And.Contain("\"music_gain\":0.00"));
            }

            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(s.MenuPlaying, Is.False, "sigue en pausa mientras suena la alarma");

            // Se apaga la alarma.
            port.Push(AudioFocusState.Gain);
            yield return null;
            yield return null;
            Assert.That(s.Mix.FocusPaused, Is.False);
            Assert.That(s.MenuPlaying, Is.True, "la música vuelve");
            Assert.That(s.Mix.SfxAudible, Is.True, "y los efectos");
            if (DevProbe.Enabled)
            {
                Assert.That(_probe.LastAudioFocusLine, Does.Contain("\"state\":\"gain\"").And.Contain("\"music_gain\":1.00"));
            }

            yield return Wait(() => s.MenuVolume > 0.9f, "con el fundido, hasta arriba", 5f);

            // Un aviso que se puede oír por encima: la música baja al 20 %, los efectos siguen.
            port.Push(AudioFocusState.Duck);
            yield return null;
            yield return null;
            Assert.That(s.Mix.FocusDucked, Is.True);
            Assert.That(s.MenuPlaying, Is.True, "no se pausa");
            Assert.That(s.MenuVolume, Is.EqualTo(AudioMix.FocusDuckGain).Within(0.02f), "al ~20 %");
            Assert.That(s.Mix.SfxAudible, Is.True);
            if (DevProbe.Enabled)
            {
                Assert.That(_probe.LastAudioFocusLine, Does.Contain("\"state\":\"duck\"").And.Contain("\"music_gain\":0.20"));
            }

            port.Push(AudioFocusState.Gain);
            yield return null;
            yield return null;
            Assert.That(s.MenuVolume, Is.EqualTo(1f).Within(0.02f), "vuelve al 100 %");
        }

        [UnityTest]
        public IEnumerator MusicOff_DoesNotHoldTheFocus_Background_ReleasesIt_AndAnAdEnd_TakesItBackAfterALoss()
        {
            var port = new FakeAudioFocusPort();
            UnityAudioService s = Take(port);
            yield return null;
            Assert.That(port.Held, Is.True);

            // Música apagada: se suelta (no corta la música de otra app); al encenderla, se pide otra vez.
            s.Configure(new SoundSettings(true, false, true));
            Assert.That(port.Held, Is.False);
            Assert.That(s.AudioFocus.State, Is.EqualTo(AudioFocusState.None));
            if (DevProbe.Enabled)
            {
                Assert.That(_probe.LastAudioFocusLine, Does.Contain("\"state\":\"none\""));
            }

            port.Push(AudioFocusState.LossTransient);
            yield return null;
            Assert.That(s.Mix.FocusPaused, Is.False, "sin foco pedido, un aviso no cuenta");
            s.Configure(SoundSettings.Defaults);
            Assert.That(port.Held, Is.True);
            Assert.That(s.AudioFocus.State, Is.EqualTo(AudioFocusState.Gain));

            // Segundo plano: se suelta; al volver, se pide.
            int requests = port.Requests;
            s.SetForeground(false);
            Assert.That(port.Held, Is.False);
            s.SetForeground(true);
            Assert.That(port.Requests, Is.EqualTo(requests + 1));

            // Otra app de música lo toma sin plazo; un anuncio empieza y termina: se pide otra vez y todo vuelve.
            port.Push(AudioFocusState.Loss);
            yield return null;
            Assert.That(s.Mix.FocusPaused, Is.True);
            s.SetAdActive(true);
            requests = port.Requests;
            s.SetAdActive(false);
            Assert.That(port.Requests, Is.EqualTo(requests + 1), "al terminar el anuncio, se vuelve a pedir");
            Assert.That(s.Mix.FocusPaused, Is.False);
            Assert.That(s.Mix.MusicAudible, Is.True);

            // Android lo niega (una llamada en curso): suena como sin foco.
            s.SetForeground(false);
            port.RequestResult = AudioFocusState.Denied;
            s.SetForeground(true);
            Assert.That(s.AudioFocus.State, Is.EqualTo(AudioFocusState.Denied));
            Assert.That(s.Mix.FocusPaused, Is.False);
            Assert.That(s.Mix.MusicAudible, Is.True);
        }

        [UnityTest]
        public IEnumerator OtherMusicPlaying_AtForeground_GameMusicStaysOff_EffectsSound_WithoutItTheAlarmPauses()
        {
            // Spotify suena al abrir el juego (decisión del dueño del 2026-10-29): no se pide el foco ni suena la música del juego.
            var port = new FakeAudioFocusPort { OtherMusic = true };
            UnityAudioService s = Take(port);
            Assert.That(port.Requests, Is.EqualTo(0), "no se pide el foco: la otra música sigue");
            Assert.That(s.AudioFocus.State, Is.EqualTo(AudioFocusState.Yielded));
            s.SetMenuMusic();
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }

            Assert.That(s.MenuVolume, Is.EqualTo(0f), "la música del juego, callada");
            Assert.That(s.Mix.SfxAudible, Is.True, "los efectos suenan");
            if (DevProbe.Enabled)
            {
                Assert.That(_probe.LastAudioFocusLine, Does.Contain("\"state\":\"yielded\"").And.Contain("\"music_gain\":0.00").And.Contain("\"paused\":false"));
            }

            // Vuelve de segundo plano y ya no suena nada: pide el foco, la música entra y una alarma la pausa.
            s.SetForeground(false);
            port.OtherMusic = false;
            s.SetForeground(true);
            Assert.That(port.Requests, Is.EqualTo(1));
            Assert.That(s.AudioFocus.State, Is.EqualTo(AudioFocusState.Gain));
            yield return Wait(() => s.MenuReady && s.MenuPlaying && s.MenuVolume > 0.9f, "el tema de menú", 30f);
            port.Push(AudioFocusState.LossTransient);
            yield return null;
            yield return null;
            Assert.That(s.MenuPlaying, Is.False, "la alarma la pausa");
        }

        [UnityTest]
        public IEnumerator Pour_LowersTheMusic5Db_ProbeSaysDucked_AndTheFocusDuckStillApplies()
        {
            // Observación 23 de la jornada 6 (decisión del dueño del 2026-10-30): la música baja un poco mientras cae el chorro, con
            // ataque de 80 ms y suelta de 300 ms tras la espera; el foco de audio sigue igual por encima.
            var port = new FakeAudioFocusPort();
            UnityAudioService s = Take(port);
            DevProbe.AudioLinesInEditor = true;
            try
            {
                s.SetMenuMusic();
                yield return Wait(() => s.SynthReady && s.MenuReady && s.MenuPlaying && s.MenuVolume > 0.99f, "el tema de menú a todo volumen", 30f);
                Assert.That(s.Mix.ActionDucked, Is.False);

                s.PourBegin(0, 0.25f);
                yield return Wait(() => s.Mix.ActionDuckAmount >= 1f, "el ataque de 80 ms", 2f);
                yield return null;
                Assert.That(s.Mix.Pouring, Is.True, "el chorro cae");
                Assert.That(AudioAnalysis.ToDb(s.MenuVolume), Is.EqualTo(AudioMix.ActionDuckDb).Within(0.2f), "la música, −5 dB");
                Assert.That(s.Mix.SfxGain, Is.EqualTo(1f), "los efectos, enteros");
                Assert.That(s.Mix.CurrentMusic.Ducked, Is.True);
                if (DevProbe.Enabled)
                {
                    Assert.That(_probe.LastMusicLine, Does.Contain("\"ducked\":true"), "AF_STATE music con ducked al verter");
                }

                // Un aviso por encima (foco «duck»): las dos atenuaciones, como con un modal.
                port.Push(AudioFocusState.Duck);
                yield return null;
                yield return null;
                Assert.That(s.MenuVolume, Is.EqualTo(AudioMix.FocusDuckGain * s.Mix.ActionDuckGain).Within(0.01f), "el 20 % del foco, sobre el chorro");
                port.Push(AudioFocusState.Gain);
                yield return null;
                yield return null;

                s.PourEnd(0);
                yield return new WaitForSecondsRealtime((AudioMix.ActionDuckHoldMs * 0.5f) / 1000f);
                Assert.That(AudioAnalysis.ToDb(s.MenuVolume), Is.EqualTo(AudioMix.ActionDuckDb).Within(0.2f), "sigue abajo durante la espera");
                yield return Wait(() => !s.Mix.ActionDucked, "la suelta", 3f);
                yield return null;
                Assert.That(s.MenuVolume, Is.EqualTo(1f).Within(0.01f), "vuelve a su nivel");
                if (DevProbe.Enabled)
                {
                    Assert.That(_probe.LastMusicLine, Does.Contain("\"ducked\":false"), "y la sonda lo cuenta");
                }

                // El sellado también la baja mientras suena.
                s.Play(SfxId.Seal, 0);
                yield return Wait(() => s.Mix.ActionDuckAmount >= 1f, "el ataque del sellado", 2f);
                Assert.That(s.Mix.ActionDucked, Is.True);
                yield return Wait(() => !s.Mix.ActionDucked, "el final del sellado", 3f);
            }
            finally
            {
                DevProbe.AudioLinesInEditor = false;
            }
        }

        private UnityAudioService Take(FakeAudioFocusPort port)
        {
            _service = UnityAudioService.Ensure();
            _service.Configure(SoundSettings.Defaults);
            _service.SetAdActive(false);
            _service.SetModalDuck(false);
            _service.Probe = _probe;
            _service.SetForeground(true);
            _service.UseFocusPort(port);
            return _service;
        }

        private static IEnumerator Wait(Func<bool> condition, string what, float seconds)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > seconds)
                {
                    Assert.Fail("Tiempo agotado esperando: " + what);
                }

                yield return null;
            }
        }
    }
}
