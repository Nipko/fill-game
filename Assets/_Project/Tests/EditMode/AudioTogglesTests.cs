#nullable enable
using System.Collections.Generic;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Música y Sonido con un toque (D-046, punto 2; 07 §9.4; 02 §5): los botones de la pausa y del inicio
    /// (<see cref="AudioToggles"/>) guardan en el mismo sitio que las filas de Ajustes (<c>settings.music</c> y
    /// <c>settings.sound</c>; <see cref="AudioQuickSettings"/>) y, con lo guardado, las filas y los botones enseñan lo mismo; la
    /// Vibración solo desde Ajustes; el icono tachado es el apagado; TalkBack lee el ajuste y su estado; y la sonda lleva
    /// <c>source</c> (08 §8.2).
    /// </summary>
    public sealed class AudioTogglesTests
    {
        private ProceduralArt _art = null!;
        private GameObject _host = null!;
        private LocStateSnapshot _loc;

        [SetUp]
        public void SetUp()
        {
            _loc = LocStateSnapshot.Take();
            LocSetUpFixture.ConfigureEs419();
            _art = new ProceduralArt("white");
            _host = new GameObject("AudioTogglesHost", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            _loc.Restore();
            Object.DestroyImmediate(_host);
            _art.Dispose();
        }

        [Test]
        public void Icons_AreDrawnByCode_AndTheOffOnesAreDifferent()
        {
            Sprite[] icons = { _art.IconMusic, _art.IconMusicOff, _art.IconSound, _art.IconSoundOff };
            foreach (Sprite icon in icons)
            {
                Assert.That(icon, Is.Not.Null);
                Assert.That(icon.rect.width, Is.EqualTo(78f), icon.name + ": 26 dp a 3 px/dp, como el engranaje");
            }

            Assert.That(_art.IconMusicOff, Is.Not.SameAs(_art.IconMusic));
            Assert.That(_art.IconSoundOff, Is.Not.SameAs(_art.IconSound));
            Assert.That(_art.IconMusic.name, Is.EqualTo("icon_music"));
            Assert.That(_art.IconSoundOff.name, Is.EqualTo("icon_sound_off"));
        }

        [Test]
        public void Mark_ShowsTheCrossedIcon_WhenOff_AndATapAsksForTheOpposite()
        {
            var toggles = new AudioToggles(_host.transform, _art);
            Assert.That(toggles.MusicOn && toggles.SoundOn, Is.True, "encendidos hasta que se marque lo guardado");
            Assert.That(toggles.MusicSprite, Is.SameAs(_art.IconMusic));
            Assert.That(toggles.SoundSprite, Is.SameAs(_art.IconSound));

            var asked = new List<string>();
            toggles.Toggled = (key, on) => asked.Add(key + "=" + (on ? "on" : "off"));
            toggles.MusicButton.onClick.Invoke();
            Assert.That(asked, Is.EqualTo(new[] { "music=off" }), "pide apagar la música");
            Assert.That(toggles.MusicOn, Is.True, "lo que se ve cambia con lo guardado, no con el toque");

            toggles.Mark(false, true);
            Assert.That(toggles.MusicSprite, Is.SameAs(_art.IconMusicOff), "apagada: la nota tachada");
            Assert.That(toggles.SoundSprite, Is.SameAs(_art.IconSound));
            toggles.MusicButton.onClick.Invoke();
            toggles.SoundButton.onClick.Invoke();
            Assert.That(asked, Is.EqualTo(new[] { "music=off", "music=on", "sound=off" }));

            toggles.PlaceRow(10f, 20f);
            Assert.That(toggles.MusicRect.rect.width, Is.GreaterThanOrEqualTo(48f - 0.01f), "área táctil ≥ 48 dp (07 §7.8)");
            Assert.That(toggles.SoundRect.rect.height, Is.GreaterThanOrEqualTo(48f - 0.01f));
            Assert.That(toggles.SoundRect.anchoredPosition.x - toggles.MusicRect.anchoredPosition.x, Is.EqualTo(AudioToggles.TouchDp + AudioToggles.GapDp).Within(0.01f), "sin solaparse");
        }

        [Test]
        public void TalkBack_ReadsTheSettingAndItsState()
        {
            var toggles = new AudioToggles(_host.transform, _art);
            AssertLabel(toggles.MusicButton.gameObject, "Música: activada");
            AssertLabel(toggles.SoundButton.gameObject, "Sonido: activado");
            toggles.Mark(false, false);
            AssertLabel(toggles.MusicButton.gameObject, "Música: desactivada");
            AssertLabel(toggles.SoundButton.gameObject, "Sonido: desactivado");
        }

        [Test]
        public void PauseAndHome_WriteTheSameSettingsAsTheSettingsRows_AndBothShowTheSame()
        {
            var probe = new DevProbe();
            var settingsScreen = new SettingsScreen(_host.transform, _art, probe, _ => { }, _ => { }, _ => { }, () => { });
            var pause = new AudioToggles(_host.transform, _art);
            var home = new AudioToggles(_host.transform, _art);
            var data = new SettingsData();

            // La pausa apaga la música: el mismo campo que la fila de Ajustes.
            Assert.That(AudioQuickSettings.Apply(data, SettingsScreen.MusicId, false, AudioQuickSettings.SourcePause, out bool changed), Is.True);
            Assert.That(changed, Is.True);
            Assert.That(data.Music, Is.False, "settings.music (08 §4.3)");
            Mark(data, settingsScreen, pause, home);
            Assert.That(settingsScreen.AudioMarked(SettingsScreen.MusicId), Is.False, "Ajustes lo refleja");
            Assert.That(home.MusicOn, Is.False, "y el inicio también");

            // Ajustes apaga el sonido: la pausa y el inicio lo enseñan.
            Assert.That(AudioQuickSettings.Apply(data, SettingsScreen.SoundId, false, AudioQuickSettings.SourceSettings, out changed), Is.True);
            Mark(data, settingsScreen, pause, home);
            Assert.That(pause.SoundOn || home.SoundOn, Is.False);
            Assert.That(pause.SoundSprite, Is.SameAs(_art.IconSoundOff));

            // El inicio vuelve a encender la música; repetir el mismo valor no cambia nada.
            Assert.That(AudioQuickSettings.Apply(data, SettingsScreen.MusicId, true, AudioQuickSettings.SourceHome, out changed), Is.True);
            Assert.That(changed, Is.True);
            Assert.That(AudioQuickSettings.Apply(data, SettingsScreen.MusicId, true, AudioQuickSettings.SourceHome, out changed), Is.True);
            Assert.That(changed, Is.False, "mismo valor: nada que guardar");
            Mark(data, settingsScreen, pause, home);
            Assert.That(settingsScreen.AudioMarked(SettingsScreen.MusicId) && pause.MusicOn && home.MusicOn, Is.True);
            Assert.That(data.Haptics, Is.True, "la Vibración, intacta");
        }

        [Test]
        public void Haptics_OnlyFromSettings_AndUnknownSourcesOrKeysAreRefused()
        {
            var data = new SettingsData();
            Assert.That(AudioQuickSettings.Apply(data, SettingsScreen.HapticsId, false, AudioQuickSettings.SourcePause, out _), Is.False, "la Vibración sigue solo en Ajustes (D-046)");
            Assert.That(AudioQuickSettings.Apply(data, SettingsScreen.HapticsId, false, AudioQuickSettings.SourceHome, out _), Is.False);
            Assert.That(data.Haptics, Is.True);
            Assert.That(AudioQuickSettings.Apply(data, SettingsScreen.HapticsId, false, AudioQuickSettings.SourceSettings, out _), Is.True);
            Assert.That(data.Haptics, Is.False);
            Assert.That(AudioQuickSettings.Apply(data, SettingsScreen.MusicId, false, "hud", out _), Is.False, "nada en la barra de juego");
            Assert.That(AudioQuickSettings.Apply(data, "language", false, AudioQuickSettings.SourcePause, out _), Is.False);
            Assert.That(data.Music, Is.True);
            Assert.That(AudioQuickSettings.Sources, Is.EqualTo(new[] { "settings", "pause", "home" }), "los valores de source de 08 §8.2");
        }

        [Test]
        public void Probe_SettingsChange_CarriesTheSource()
        {
            Assert.That(DevProbe.Enabled, Is.True, "en el Editor, la sonda escribe");
            var probe = new DevProbe();
            Assert.That(probe.FormatSettingsChange("music", "false", null, "pause"), Does.Match("^AF_STATE \\{\"event\":\"settings_change\",\"t\":\\d+,\"key\":\"music\",\"value\":\"false\",\"source\":\"pause\"\\}$"));
            Assert.That(probe.FormatSettingsChange("sound", "true", null, "home"), Does.EndWith(",\"source\":\"home\"}"));
            Assert.That(probe.FormatSettingsChange("language", "es-419", null), Does.Not.Contain("source"), "sin source, la línea de siempre");
        }

        private static void Mark(SettingsData data, SettingsScreen settings, params AudioToggles[] toggles)
        {
            settings.MarkAudio(data.Sound, data.Music, data.Haptics);
            foreach (AudioToggles t in toggles)
            {
                t.Mark(data.Music, data.Sound);
            }
        }

        private static void AssertLabel(GameObject go, string expected)
        {
            Assert.That(A11yLabels.TryGet(go, out string label, out bool selected), Is.True, "lleva su etiqueta (02 §12)");
            Assert.That(label, Is.EqualTo(expected));
            Assert.That(selected, Is.False, "el estado va en la etiqueta, no como opción marcada");
        }
    }
}
