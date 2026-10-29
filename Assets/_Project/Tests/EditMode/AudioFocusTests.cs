#nullable enable
using System;
using System.IO;
using System.Text.RegularExpressions;
using AF.Audio;
using AF.Audio.Unity;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El foco de audio (observación 10 de la jornada 6: con una alarma sonando, la música del juego seguía de fondo; D-026, añadido
    /// del 2026-10-29): el estado del foco → la ganancia y la pausa de <see cref="AudioMix"/>; cuándo se pide, se suelta y se vuelve a
    /// pedir (<see cref="AudioFocusTracker"/>, con el puerto falso); y el plugin <c>AfAudioFocus.androidlib</c>, solo Java con API de
    /// la plataforma, sin permisos ni dependencias, con los mismos textos que el lado de C#.
    /// </summary>
    public sealed class AudioFocusTests
    {
        private static string PluginDir => Path.Combine(Application.dataPath, "_Project", "Plugins", "Android", "AfAudioFocus.androidlib");

        private static string Java => File.ReadAllText(Path.Combine(PluginDir, "src", "main", "java", "com", "amazonfill", "audiofocus", "AfAudioFocus.java"));

        [Test]
        public void States_RoundTrip_AndUnknownTextsSoundAsAlways()
        {
            foreach (AudioFocusState state in (AudioFocusState[])Enum.GetValues(typeof(AudioFocusState)))
            {
                Assert.That(AudioFocusStates.Parse(AudioFocusStates.Name(state)), Is.EqualTo(state), state.ToString());
            }

            Assert.That(AudioFocusStates.Parse("error"), Is.EqualTo(AudioFocusState.None));
            Assert.That(AudioFocusStates.Parse(null), Is.EqualTo(AudioFocusState.None));
            Assert.That(AudioFocusStates.Parse(string.Empty), Is.EqualTo(AudioFocusState.None));
            Assert.That(AudioFocusStates.Pauses(AudioFocusState.LossTransient), Is.True);
            Assert.That(AudioFocusStates.Pauses(AudioFocusState.Loss), Is.True);
            Assert.That(AudioFocusStates.Pauses(AudioFocusState.Duck), Is.False);
            Assert.That(AudioFocusStates.Pauses(AudioFocusState.Denied), Is.False);
        }

        [Test]
        public void Mix_LossPausesEverything_GainFadesBack_DuckLowersOnlyTheMusicTo20()
        {
            var mix = new AudioMix();
            mix.SetFocus(AudioFocusState.Gain);
            Assert.That(mix.MusicGain, Is.EqualTo(1f));
            Assert.That(mix.SfxGain, Is.EqualTo(1f));

            mix.SetFocus(AudioFocusState.Duck);
            Assert.That(mix.MusicGain, Is.EqualTo(AudioMix.FocusDuckGain).Within(1e-6f), "la música, al ~20 %");
            Assert.That(AudioMix.FocusDuckGain, Is.EqualTo(0.2f));
            Assert.That(mix.AmbientGain, Is.EqualTo(AudioMix.FocusDuckGain).Within(1e-6f), "el ambiente va con la música");
            Assert.That(mix.SfxGain, Is.EqualTo(1f), "los efectos siguen");
            mix.ModalDuck = true;
            Assert.That(mix.MusicGain, Is.EqualTo(AudioMix.FocusDuckGain * AudioMix.DuckGain).Within(1e-6f), "con un modal, las dos atenuaciones");
            mix.ModalDuck = false;

            foreach (AudioFocusState loss in new[] { AudioFocusState.LossTransient, AudioFocusState.Loss })
            {
                mix.SetFocus(loss);
                Assert.That(mix.FocusPaused, Is.True, loss.ToString());
                Assert.That(mix.MusicAudible, Is.False);
                Assert.That(mix.SfxAudible, Is.False);
                Assert.That(mix.MusicGain, Is.EqualTo(0f));
                Assert.That(mix.SfxGain, Is.EqualTo(0f));
                Assert.That(mix.HapticsAllowed, Is.True, "vibrar no depende del foco de audio");
                mix.Tick(2000f);
                Assert.That(mix.MusicGain, Is.EqualTo(0f), "sigue callada mientras dura");
                mix.SetAmbient(3, false, 61);
                Assert.That(mix.AdvanceFauna(1e7f, out SfxId _), Is.False, "la fauna, parada");

                mix.SetFocus(AudioFocusState.Gain);
                Assert.That(mix.MusicGain, Is.EqualTo(0f), "al volver, desde 0");
                mix.Tick(AudioMix.AdFadeMs / 2f);
                Assert.That(mix.MusicGain, Is.EqualTo(0.5f).Within(1e-4f), "con el fundido de 0,5 s");
                mix.Tick(AudioMix.AdFadeMs);
                Assert.That(mix.MusicGain, Is.EqualTo(1f));
            }

            mix.Settings = new SoundSettings(true, false, true);
            mix.SetFocus(AudioFocusState.Gain);
            Assert.That(mix.MusicGain, Is.EqualTo(0f), "con la Música apagada, el foco no la enciende");
            Assert.That(mix.SfxGain, Is.EqualTo(1f));

            mix.Settings = SoundSettings.Defaults;
            mix.SetFocus(AudioFocusState.Denied);
            Assert.That(mix.MusicGain, Is.EqualTo(1f), "negado: suena como sin foco");
            mix.SetFocus(AudioFocusState.None);
            Assert.That(mix.MusicGain, Is.EqualTo(1f));
        }

        [Test]
        public void Tracker_RequestsWhenWanted_IgnoresNoticesWithoutIt_AndReacquiresAfterALoss()
        {
            var port = new FakeAudioFocusPort();
            var tracker = new AudioFocusTracker(port);
            Assert.That(tracker.State, Is.EqualTo(AudioFocusState.None));
            port.Push(AudioFocusState.LossTransient);
            Assert.That(tracker.Poll(), Is.False, "sin pedirlo, un aviso no cuenta");

            Assert.That(tracker.Want(true), Is.True);
            Assert.That(tracker.State, Is.EqualTo(AudioFocusState.Gain));
            Assert.That(port.Requests, Is.EqualTo(1));
            Assert.That(tracker.Want(true), Is.False, "ya pedido: no se vuelve a pedir");
            Assert.That(port.Requests, Is.EqualTo(1));
            Assert.That(tracker.Reacquire(), Is.False, "con el foco, no hace falta");

            port.Push(AudioFocusState.Duck);
            port.Push(AudioFocusState.LossTransient);
            Assert.That(tracker.Poll(), Is.True);
            Assert.That(tracker.State, Is.EqualTo(AudioFocusState.LossTransient), "varios avisos antes del fotograma: cuenta el último");
            Assert.That(tracker.Poll(), Is.False);
            Assert.That(tracker.Reacquire(), Is.False, "una pérdida pasajera: Android lo devuelve solo");
            port.Push(AudioFocusState.Gain);
            Assert.That(tracker.Poll(), Is.True);
            Assert.That(tracker.State, Is.EqualTo(AudioFocusState.Gain));

            port.Push(AudioFocusState.Loss);
            Assert.That(tracker.Poll(), Is.True);
            Assert.That(tracker.Reacquire(), Is.True, "sin plazo: se vuelve a pedir");
            Assert.That(port.Requests, Is.EqualTo(2));
            Assert.That(tracker.State, Is.EqualTo(AudioFocusState.Gain));

            Assert.That(tracker.Want(false), Is.True);
            Assert.That(port.Abandons, Is.EqualTo(1));
            Assert.That(port.Held, Is.False);
            Assert.That(tracker.State, Is.EqualTo(AudioFocusState.None));

            port.RequestResult = AudioFocusState.Denied;
            Assert.That(tracker.Want(true), Is.True);
            Assert.That(tracker.State, Is.EqualTo(AudioFocusState.Denied));
            port.RequestResult = AudioFocusState.Gain;
            Assert.That(tracker.Reacquire(), Is.True, "negado: se vuelve a pedir al volver la ventana");
            Assert.That(tracker.State, Is.EqualTo(AudioFocusState.Gain));
        }

        [Test]
        public void EditorPort_IsTheFakeOne()
        {
            Assert.That(AndroidAudioFocus.ForPlatform(), Is.InstanceOf<FakeAudioFocusPort>(), "el Editor no tiene foco de audio de Android");
        }

        [Test]
        public void Plugin_IsJavaOnly_WithPlatformApiOnly_NoPermissionsOrDependencies()
        {
            Assert.That(Directory.GetFiles(PluginDir, "*.kt", SearchOption.AllDirectories), Is.Empty, "sin Kotlin");
            string[] files = Directory.GetFiles(PluginDir, "*.java", SearchOption.AllDirectories);
            Assert.That(files, Has.Length.EqualTo(1), "solo AfAudioFocus");
            string[] banned =
            {
                "java.net", "javax.net", "okhttp", "HttpURLConnection", "Socket", "http:", "https:", "WebView", "android.util.Log", "Log.",
                "System.out", "printStackTrace", "SharedPreferences", "FileOutputStream", "getAdvertisingId", "Settings.Secure", "androidx",
                "com.unity3d", "Class.forName", "getMethod(",
            };
            string text = File.ReadAllText(files[0]);
            foreach (string word in banned)
            {
                Assert.That(text.Contains(word), Is.False, word);
            }

            foreach (Match m in Regex.Matches(text, @"^import\s+([\w\.]+);", RegexOptions.Multiline))
            {
                Assert.That(m.Groups[1].Value.StartsWith("android.", StringComparison.Ordinal), Is.True, "importa " + m.Groups[1].Value);
            }

            string gradle = File.ReadAllText(Path.Combine(PluginDir, "build.gradle"));
            Assert.That(gradle, Does.Contain("namespace 'com.amazonfill.audiofocus'"), "AGP 9: namespace en cada .androidlib");
            Assert.That(gradle, Does.Contain("minSdk 26"), "AudioFocusRequest es de la API 26");
            Assert.That(gradle, Does.Not.Contain("dependencies"), "regla 4: nada de dependencias nuevas");

            string manifestPath = Path.Combine(PluginDir, "src", "main", "AndroidManifest.xml");
            Assert.DoesNotThrow(() => new System.Xml.XmlDocument().Load(manifestPath), "XML válido");
            string manifest = File.ReadAllText(manifestPath);
            Assert.That(manifest, Does.Not.Contain("uses-permission"), "el foco de audio no necesita permisos");
            Assert.That(manifest, Does.Not.Contain("<application"), "sin componentes");

            string proguard = File.ReadAllText(Path.Combine(PluginDir, "proguard-rules.pro"));
            StringAssert.Contains("-keep class com.amazonfill.audiofocus.** { *; }", proguard);
            StringAssert.Contains("-keep interface com.amazonfill.audiofocus.** { *; }", proguard);
        }

        [Test]
        public void Plugin_MatchesTheCSharpSide()
        {
            string java = Java;
            Assert.That(AndroidAudioFocus.BridgeClass, Is.EqualTo("com.amazonfill.audiofocus.AfAudioFocus"));
            StringAssert.Contains("package com.amazonfill.audiofocus;", java);
            StringAssert.Contains("public static final String GAIN = \"" + AudioFocusStates.Gain + "\";", java);
            StringAssert.Contains("public static final String DUCK = \"" + AudioFocusStates.Duck + "\";", java);
            StringAssert.Contains("public static final String LOSS_TRANSIENT = \"" + AudioFocusStates.LossTransient + "\";", java);
            StringAssert.Contains("public static final String LOSS = \"" + AudioFocusStates.Loss + "\";", java);
            StringAssert.Contains("public static final String DENIED = \"" + AudioFocusStates.Denied + "\";", java);
            StringAssert.Contains("public interface Listener", java);
            StringAssert.Contains("void onFocus(String state);", java);
            StringAssert.Contains("public static String request(Context context, Listener target)", java);
            StringAssert.Contains("public static void abandon()", java);

            // Cómo se pide: el juego, con su música, y avisando también de los «duck» (sin el ducking automático de Android 8+).
            StringAssert.Contains("AudioAttributes.USAGE_GAME", java);
            StringAssert.Contains("AudioAttributes.CONTENT_TYPE_MUSIC", java);
            StringAssert.Contains("new AudioFocusRequest.Builder(AudioManager.AUDIOFOCUS_GAIN)", java);
            StringAssert.Contains(".setWillPauseWhenDucked(true)", java);
            StringAssert.Contains("case AudioManager.AUDIOFOCUS_LOSS_TRANSIENT_CAN_DUCK:\n                return DUCK;", java.Replace("\r\n", "\n"));
            StringAssert.Contains("case AudioManager.AUDIOFOCUS_LOSS_TRANSIENT:\n                return LOSS_TRANSIENT;", java.Replace("\r\n", "\n"));
            StringAssert.Contains("case AudioManager.AUDIOFOCUS_LOSS:\n                return LOSS;", java.Replace("\r\n", "\n"));
            StringAssert.Contains("manager.abandonAudioFocusRequest(pending)", java);
        }
    }
}
