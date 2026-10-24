#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Audio;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Música y Sonido con un toque en el juego (D-046, punto 2; 07 §9.4; 02 §5), con el servicio de audio falso: en la pausa, junto
    /// a la X, un toque apaga la música en el acto, se guarda en <c>settings.music</c> y Ajustes lo refleja (y al revés); en el
    /// inicio, junto al engranaje, igual con el Sonido; los dos caben sin pisar nada; y TalkBack lee «Música: activada».
    /// </summary>
    public sealed class AudioQuickTogglesTests
    {
        private FakeAudioService _audio = null!;

        [SetUp]
        public void SetUp()
        {
            _audio = new FakeAudioService();
            AudioCues.Override = _audio;
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            GameBootstrap.A11yHostOverride = null;
            AudioCues.Override = null;
            AudioCues.Install(null);
        }

        [UnityTest]
        public IEnumerator Pause_MusicTap_SwitchesItOffAtOnce_AndSettingsShowsIt()
        {
            var profile = SettingsScreenTests.ProfileAt(4);
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return Canvas16x9LayoutTests.LoadPlaying(4);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));

            view.PauseButton.onClick.Invoke();
            PauseMenu pause = view.Pause;
            Assert.That(pause.Visible, Is.True);
            AudioToggles toggles = pause.Audio;
            Assert.That(toggles.Visible, Is.True, "la pausa lleva Música y Sonido (02 §5; D-046)");
            Assert.That(toggles.MusicOn && toggles.SoundOn, Is.True, "activados por defecto (08 §9)");
            Canvas.ForceUpdateCanvases();
            AssertPauseTopRow(pause);

            // Un toque en la nota: se calla, se guarda y la nota sale tachada.
            toggles.MusicButton.onClick.Invoke();
            Assert.That(boot.Profile!.Settings.Music, Is.False, "settings.music (08 §4.3)");
            Assert.That(_audio.Mix.MusicAudible, Is.False, "aplicado en el acto");
            Assert.That(toggles.MusicOn, Is.False);
            Assert.That(boot.Profile.Settings.Sound && boot.Profile.Settings.Haptics, Is.True, "lo demás, intacto");
            Assert.That(A11yScanner.ButtonLabel(toggles.MusicButton, out _), Is.EqualTo("Música: desactivada"), "TalkBack lee el estado nuevo");
            Assert.That(A11yScanner.ButtonLabel(toggles.SoundButton, out _), Is.EqualTo("Sonido: activado"));

            // Ajustes (que solo se abre desde el inicio) enseña lo mismo; y lo que cambia Ajustes llega a la pausa.
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.AudioMarked(SettingsScreen.MusicId), Is.False, "la fila Música de Ajustes lo refleja");
            Assert.That(settings.AudioMarked(SettingsScreen.SoundId), Is.True);
            Assert.That(boot.ChooseAudio(SettingsScreen.SoundId, false), Is.True, "la fila Sonido de Ajustes");
            Assert.That(toggles.SoundOn, Is.False, "y la pausa lo enseña");
            Assert.That(boot.Lobby!.Audio.SoundOn, Is.False, "y el inicio");

            // La Vibración no se toca desde la pausa.
            Assert.That(boot.ChooseAudio(SettingsScreen.HapticsId, false, AudioQuickSettings.SourcePause), Is.False);
            Assert.That(boot.Profile.Settings.Haptics, Is.True);

            pause.ResumeButton.onClick.Invoke();
            Assert.That(pause.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Pause_At130_TheTitleStillFits_NextToMusicSoundAndTheX([Values(false, true)] bool pseudo)
        {
            LaunchOptions.FontScaleOverride = 1.3f;
            if (pseudo)
            {
                LaunchOptions.LanguageOverride = "en-XA";
            }

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return Canvas16x9LayoutTests.LoadPlaying(4);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            view.PauseButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 600);
            Canvas.ForceUpdateCanvases();
            PauseMenu pause = view.Pause;
            AssertPauseTopRow(pause);
            var title = pause.Root.Find("Panel/HeaderPlate/Title").GetComponent<TMPro.TextMeshProUGUI>();
            string where = pseudo ? "en-XA al 130 %" : "es-419 al 130 %";
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), where + ": el título no baja del mínimo");
            Assert.That(UiBuilder.MinTextWidth(title), Is.LessThanOrEqualTo(title.rectTransform.rect.width + 0.5f), where + ": «" + title.text + "» cabe junto a Música, Sonido y la X");
        }

        [UnityTest]
        public IEnumerator Home_SoundAndMusic_NextToTheGear_AreSaved_AndSettingsShowsThem()
        {
            var store = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            Canvas.ForceUpdateCanvases();
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(lobby.Audio.Visible, Is.True, "el inicio lleva Música y Sonido junto al engranaje (D-046)");

            // Junto al engranaje: debajo, alineados con él, sin solaparse.
            var root = (RectTransform)lobby.Canvas.transform;
            Rect gear = CanvasRect((RectTransform)lobby.SettingsButton.transform, root);
            Rect music = CanvasRect(lobby.Audio.MusicRect, root);
            Rect sound = CanvasRect(lobby.Audio.SoundRect, root);
            Assert.That(music.xMin, Is.EqualTo(gear.xMin).Within(0.5f), "bajo el engranaje");
            Assert.That(music.yMin, Is.GreaterThanOrEqualTo(gear.yMax - 0.5f));
            Assert.That(sound.yMin, Is.GreaterThanOrEqualTo(music.yMax - 0.5f));
            Assert.That(music.width >= 47.5f && music.height >= 47.5f && sound.width >= 47.5f && sound.height >= 47.5f, Is.True, "≥ 48 dp (07 §7.8)");

            lobby.Audio.SoundButton.onClick.Invoke();
            Assert.That(boot.Profile!.Settings.Sound, Is.False, "settings.sound");
            Assert.That(store.Load().Profile!.Settings.Sound, Is.False, "guardado");
            Assert.That(_audio.Mix.Settings.Sound, Is.False, "aplicado en el acto");
            _audio.Clear();
            AudioCues.Play(SfxId.Seal);
            Assert.That(_audio.Heard(SfxId.Seal), Is.False, "ya no suena nada");

            lobby.Audio.MusicButton.onClick.Invoke();
            Assert.That(boot.Profile.Settings.Music, Is.False);
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.AudioMarked(SettingsScreen.SoundId) || settings.AudioMarked(SettingsScreen.MusicId), Is.False, "Ajustes lo refleja");
            Assert.That(settings.AudioMarked(SettingsScreen.HapticsId), Is.True);

            // Y al revés: la fila de Ajustes enciende la música y el inicio lo enseña al volver.
            settings.AudioButton(SettingsScreen.MusicId).onClick.Invoke();
            Assert.That(boot.CloseSettings(), Is.True);
            Assert.That(lobby.Audio.MusicOn, Is.True);
            Assert.That(lobby.Audio.MusicSprite, Is.SameAs(boot.Art!.IconMusic));
            Assert.That(lobby.Audio.SoundSprite, Is.SameAs(boot.Art!.IconSoundOff), "el Sonido sigue tachado");
        }

        [UnityTest]
        public IEnumerator Home_FitsWithoutOverlaps_At360x640_And360x800_WithTheCanoes()
        {
            var store = new MemoryProfileStore(SettingsScreenTests.ProfileAt(58));
            foreach (Vector2Int size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2400) })
            {
                GameScreen.SimulatedScreen = new Vector4(size.x, size.y, 96f, 48f);
                yield return PlayModeHelpers.LoadApp(store);
                GameBootstrap boot = GameBootstrap.Current!;
                yield return null;
                Canvas.ForceUpdateCanvases();
                LobbyScreen lobby = boot.Lobby!;
                string where = size.x + " × " + size.y;
                var root = (RectTransform)lobby.Canvas.transform;
                var named = new List<KeyValuePair<string, Rect>>
                {
                    new KeyValuePair<string, Rect>("semillas", CanvasRect(lobby.SeedsRect, root)),
                    new KeyValuePair<string, Rect>("ajustes", CanvasRect((RectTransform)lobby.SettingsButton.transform, root)),
                    new KeyValuePair<string, Rect>("música", CanvasRect(lobby.Audio.MusicRect, root)),
                    new KeyValuePair<string, Rect>("sonido", CanvasRect(lobby.Audio.SoundRect, root)),
                    new KeyValuePair<string, Rect>("jugar", CanvasRect((RectTransform)lobby.PlayButton.transform, root)),
                    new KeyValuePair<string, Rect>("inicio", CanvasRect(lobby.HomeTabRect, root)),
                };
                if (lobby.CanoesShown)
                {
                    named.Add(new KeyValuePair<string, Rect>("canoas", CanvasRect((RectTransform)lobby.CanoesButton.transform, root)));
                }

                if (lobby.HomeWindow.Visible)
                {
                    named.Add(new KeyValuePair<string, Rect>("ventanita", CanvasRect(lobby.HomeWindow.Root, root)));
                }

                if (lobby.HomeWindow.PillShown)
                {
                    named.Add(new KeyValuePair<string, Rect>("píldora de casa", CanvasRect(lobby.HomeWindow.PillRect, root)));
                }

                MarketLobbyIcons? icons = boot.MarketIcons;
                if (icons != null && icons.PiggyShown)
                {
                    named.Add(new KeyValuePair<string, Rect>("cántaro", CanvasRect(icons.PiggyRect, root)));
                }

                if (lobby.StreakWidget.Visible)
                {
                    named.Add(new KeyValuePair<string, Rect>("crecida", CanvasRect(lobby.StreakWidget.Root, root)));
                }

                Debug.Log("AF_AUDIO_QUICK " + where + ": " + string.Join(", ", named.ConvertAll(n => n.Key + " " + n.Value)));
                for (int i = 0; i < named.Count; i++)
                {
                    Rect r = named[i].Value;
                    Assert.That(r.xMin >= -0.5f && r.xMax <= lobby.CanvasWidth + 0.5f, Is.True, where + ": " + named[i].Key + " dentro de la pantalla");
                    for (int j = i + 1; j < named.Count; j++)
                    {
                        Assert.That(Shrunk(named[i].Value).Overlaps(Shrunk(named[j].Value)), Is.False, where + ": " + named[i].Key + " y " + named[j].Key + " se solapan");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator TalkBack_ReadsMusicOn_AndActivatingItSwitchesItOff()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            var service = new A11yService(host);
            service.Refresh();
            A11yItem? music = host.Find("Música: activada");
            Assert.That(music, Is.Not.Null, "TalkBack: «Música: activada» (02 §12; D-046)");
            Assert.That(music!.Role, Is.EqualTo(AccessibilityRole.Button));
            Assert.That(host.Find("Sonido: activado"), Is.Not.Null);
            Assert.That(music.Invoke!(), Is.True, "activar (doble toque) es pulsar el botón");
            Assert.That(boot.Profile!.Settings.Music, Is.False);
            service.Refresh();
            Assert.That(host.Find("Música: desactivada"), Is.Not.Null, "y se lee el estado nuevo");
        }

        // La fila de arriba de la pausa: el título, Música, Sonido y la X, en ese orden y sin pisarse, todos ≥ 48 dp salvo el título.
        private static void AssertPauseTopRow(PauseMenu pause)
        {
            var panel = (RectTransform)pause.Root.Find("Panel");
            Rect header = CanvasRect((RectTransform)panel.Find("HeaderPlate"), panel);
            Rect music = CanvasRect(pause.Audio.MusicRect, panel);
            Rect sound = CanvasRect(pause.Audio.SoundRect, panel);
            Rect close = CanvasRect((RectTransform)panel.Find("CloseButton"), panel);
            Assert.That(header.xMax, Is.LessThanOrEqualTo(music.xMin + 0.5f), "el título, a la izquierda");
            Assert.That(music.xMax, Is.LessThanOrEqualTo(sound.xMin + 0.5f));
            Assert.That(sound.xMax, Is.LessThanOrEqualTo(close.xMin + 0.5f), "junto a la X");
            Assert.That(Mathf.Abs(music.center.y - close.center.y), Is.LessThan(0.5f), "a la altura de la X");
            Assert.That(music.width >= 47.5f && music.height >= 47.5f && sound.width >= 47.5f, Is.True, "≥ 48 dp (07 §7.8)");
        }

        // Rectángulo de rt en dp del espacio root, con origen en su esquina superior izquierda y la y hacia abajo.
        private static Rect CanvasRect(RectTransform rt, RectTransform root)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Rect bounds = root.rect;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = root.InverseTransformPoint(corners[i]);
                float x = local.x - bounds.xMin;
                float y = bounds.yMax - local.y;
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect Shrunk(Rect r)
        {
            return Rect.MinMaxRect(r.xMin + 0.5f, r.yMin + 0.5f, r.xMax - 0.5f, r.yMax - 0.5f);
        }
    }
}
