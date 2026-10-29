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
