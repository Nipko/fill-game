#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Audio;
using AF.GameFlow;
using AF.IAP;
using AF.Platform;
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
            GameScreen.SimulatedPxPerDp = null;
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
        public IEnumerator Pause_At130_TheTitleStillFits_NextToMusicSoundAndTheX([Values(false, true)] bool pseudo, [Values(360, 320)] int width)
        {
            if (width == 320)
            {
                GameScreen.SimulatedScreen = Canvas320Tests.Screen320;
                GameScreen.SimulatedPxPerDp = Canvas320Tests.Density;
            }
            else
            {
                GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            }

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
            string where = (pseudo ? "en-XA al 130 %" : "es-419 al 130 %") + " a " + width + " dp";
            Assert.That(view.CanvasWidth, Is.EqualTo((float)width).Within(0.5f), where);
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

        // Las tres pantallas de la revisión: 360 × 640 dp (16:9) y 360 × 800 dp (20:9) a 3 px/dp, y 320 × 675 dp a 2,25 px/dp.
        private static readonly int[] Screens = { 640, 800, 320 };

        [UnityTest]
        public IEnumerator Home_FitsWithoutOverlaps_WithTheDailyCardThePiggyAndTheCanoes([ValueSource(nameof(Screens))] int screen)
        {
            string where = Use(screen);
            yield return LoadRichLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            // El saludo de TiTino, si sale, cruza a 360 × 640 la tarjeta del Reto y la aparta mientras se ve: se espera a que acabe.
            HomeSloth sloth = lobby.HomeWindow.Sloth!;
            Tick(sloth, 400);
            Tick(sloth, (int)sloth.BubbleDurationMs + 300);
            boot.TickDaily(16f);
            Canvas.ForceUpdateCanvases();
            Assert.That(boot.MarketIcons!.PiggyShown, Is.True, where + ": condición del test, el Cántaro a la vista (≥ 20)");
            Assert.That(boot.DailyCard != null && boot.DailyCard.Visible, Is.True, where + ": condición del test, la tarjeta del Reto (≥ 10)");
            Assert.That(lobby.CanoesShown && lobby.StopRemainingShown, Is.True, where + ": condición del test, Canoas y el letrero de la parada");

            List<KeyValuePair<string, Rect>> named = Named(boot);
            Debug.Log("AF_AUDIO_QUICK " + where + ": " + string.Join(", ", named.ConvertAll(n => n.Key + " " + n.Value)));
            AssertNoOverlaps(named, lobby.CanvasWidth, where);
        }

        // Integración con la Liga (D-046, punto 1): el botón «Liga», abajo a la izquierda, tampoco pisa nada, al 100 % y al 130 %.
        [UnityTest]
        public IEnumerator Home_WithTheLeagueOn_TheLeagueButtonFitsWithoutOverlaps([ValueSource(nameof(Screens))] int screen, [Values(1f, 1.3f)] float fontScale)
        {
            string where = Use(screen) + " · Liga al " + Mathf.RoundToInt(fontScale * 100f) + " %";
            LaunchOptions.FontScaleOverride = fontScale;
            var clock = new ManualClock(new System.DateTime(2026, 10, 16, 15, 0, 0, System.DateTimeKind.Utc));
            GameBootstrap.LeagueApiOverride = new AF.LiveOps.FakeLeagueApi(clock);
            GameBootstrap.LeagueClockOverride = clock;
            GameBootstrap.LeagueConfigOverride = new AF.LiveOps.LeagueConfig { LiveLeagueEnabled = true };
            try
            {
                yield return LoadRichLobby();
                GameBootstrap boot = GameBootstrap.Current!;
                LobbyScreen lobby = boot.Lobby!;
                HomeSloth sloth = lobby.HomeWindow.Sloth!;
                Tick(sloth, 400);
                Tick(sloth, (int)sloth.BubbleDurationMs + 300);
                boot.TickLeague(16f);
                boot.TickDaily(16f);
                Canvas.ForceUpdateCanvases();
                Assert.That(boot.LeagueButton != null && boot.LeagueButton.Shown, Is.True, where + ": condición del test, el botón «Liga» (≥ 41)");

                List<KeyValuePair<string, Rect>> named = Named(boot);
                Debug.Log("AF_AUDIO_QUICK_LEAGUE " + where + ": " + string.Join(", ", named.ConvertAll(n => n.Key + " " + n.Value)));
                AssertNoOverlaps(named, lobby.CanvasWidth, where);
            }
            finally
            {
                GameBootstrap.ResetLeagueOverrides();
            }
        }

        [UnityTest]
        public IEnumerator Home_At130_TiTinosBubble_AndTheVoyageStayClear([ValueSource(nameof(Screens))] int screen, [Values("es-419", "pt-BR")] string language)
        {
            string where = Use(screen) + " · " + language + " al 130 %";
            LaunchOptions.FontScaleOverride = 1.3f;
            LaunchOptions.LanguageOverride = language;
            LaunchOptions.ClockOverride = new ManualClock(new System.DateTime(2026, 10, 16, 15, 0, 0, System.DateTimeKind.Utc));
            yield return LoadRichLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            var root = (RectTransform)lobby.Canvas.transform;
            AssertNoOverlaps(Named(boot), lobby.CanvasWidth, where);

            // El letrero de la parada: su texto, entero dentro de la placa y nunca por debajo de 12 sp.
            Assert.That(lobby.StopRemainingShown, Is.True, where);
            var inventory = new LocInventory(language, 1.3f);
            foreach (TMPro.TextMeshProUGUI t in lobby.StopRemainingRect.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false))
            {
                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * 1.3f) - 0.01f), where + ": " + t.name + " no baja de 12 sp × 1,3");
                Assert.That(inventory.ProblemsOf(t), Is.Empty, where + ": " + t.name + " «" + t.text + "» cabe en el letrero");
            }

            // Un mensaje de TiTino (un toque): ni «Jugar», ni Música, Sonido, el Cántaro o las tarjetas; si llega a la travesía, la
            // travesía se aparta mientras dure.
            HomeSloth sloth = lobby.HomeWindow.Sloth!;
            Tick(sloth, 400);
            Assert.That(sloth.Tap(), Is.True, where);
            Tick(sloth, 400);
            lobby.Tick(16f);
            Canvas.ForceUpdateCanvases();
            Assert.That(sloth.BubbleShown, Is.True, where + ": condición del test, un mensaje a la vista");
            Rect bubble = CanvasRect(sloth.BubbleRect, root);
            var clear = new List<KeyValuePair<string, Rect>>
            {
                new KeyValuePair<string, Rect>("jugar", CanvasRect((RectTransform)lobby.PlayButton.transform, root)),
                new KeyValuePair<string, Rect>("música", CanvasRect(lobby.Audio.MusicRect, root)),
                new KeyValuePair<string, Rect>("sonido", CanvasRect(lobby.Audio.SoundRect, root)),
                new KeyValuePair<string, Rect>("cántaro", CanvasRect(boot.MarketIcons!.PiggyRect, root)),
            };
            if (lobby.StopRemainingShown)
            {
                clear.Add(new KeyValuePair<string, Rect>("letrero", CanvasRect(lobby.StopRemainingRect, root)));
            }

            if (boot.DailyCard != null && boot.DailyCard.Visible)
            {
                clear.Add(new KeyValuePair<string, Rect>("tarjeta del Reto", CanvasRect(boot.DailyCard.Root, root)));
            }

            Debug.Log("AF_AUDIO_QUICK_NOTE " + where + ": " + sloth.Note + " " + bubble + ", " + string.Join(", ", clear.ConvertAll(n => n.Key + " " + n.Value)));
            foreach (KeyValuePair<string, Rect> other in clear)
            {
                Assert.That(Shrunk(bubble).Overlaps(Shrunk(other.Value)), Is.False, where + ": el bocadillo " + bubble + " tapa " + other.Key + " " + other.Value);
            }
        }

        // D-050, punto 6: las tarjetas del día del muelle, a 320 dp, en es-419 y en-XA, al 100 % y al 130 %: en su fila, sin pisar
        // nada, y sus textos caben a 12 sp (× escala) o más, también con los estados más largos del Reto.
        [UnityTest]
        public IEnumerator Home_At320_TheDayCardsFit_AndTheirTextsToo([Values("es-419", "en-XA")] string language, [Values(1f, 1.3f)] float fontScale)
        {
            string where = Use(320) + " · " + language + " al " + Mathf.RoundToInt(fontScale * 100f) + " %";
            LaunchOptions.FontScaleOverride = fontScale;
            LaunchOptions.LanguageOverride = language;
            yield return LoadRichLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            boot.TickDaily(16f);
            Canvas.ForceUpdateCanvases();
            DailyChallengeCard card = boot.DailyCard!;
            Assert.That(card.Visible, Is.True, where + ": condición del test, la tarjeta del Reto");
            var root = (RectTransform)lobby.Canvas.transform;
            Assert.That(lobby.CanvasWidth, Is.EqualTo(320f).Within(0.5f), where);
            Rect play = CanvasRect((RectTransform)lobby.PlayButton.transform, root);
            Rect rect = CanvasRect(card.Root, root);
            Assert.That(rect.yMin - DockCard.IconOverhangDp, Is.GreaterThanOrEqualTo(play.yMax - 0.5f), where + ": la fila, bajo «Jugar»");
            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(LobbyScreen.CardGutterDp - 0.5f), where);

            // Los textos de la tarjeta, con el estado de hoy y con los dos más largos, puestos a mano.
            var inventory = new LocInventory(language, fontScale);
            foreach (AF.LiveOps.DailyCardState shown in new[] { card.State, AF.LiveOps.DailyCardState.PendingValidation, AF.LiveOps.DailyCardState.InProgress })
            {
                card.ShowStateTextForTests(shown);
                boot.PlaceDayCardsForTests();
                Canvas.ForceUpdateCanvases();
                Debug.Log("AF_DAY_CARDS_320 " + where + " · " + shown + ": " + CanvasRect(card.Root, root) + " (alto " + card.Height + ")");
                AssertNoOverlaps(Named(boot), lobby.CanvasWidth, where + " · " + shown);
                foreach (TMPro.TextMeshProUGUI t in card.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false))
                {
                    if (t.text.Length == 0)
                    {
                        continue;
                    }

                    string problems = inventory.ProblemsOf(t);
                    Debug.Log("AF_DAY_CARDS_320_FIT " + where + ": " + t.name + " «" + t.text + "» " + t.fontSize.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " sp en " + t.rectTransform.rect.width.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " dp");
                    Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), where + ": " + t.name + " no baja de 12 sp");
                    Assert.That(problems, Is.Empty, where + ": " + t.name + " «" + t.text + "» cabe en la tarjeta");
                }
            }
        }

        // Con dos tarjetas (Reto y Bitácora), centradas y del mismo alto (D-050, punto 6).
        [UnityTest]
        public IEnumerator Home_At360_TheDayCards_AreCentered_AndAsTallAsEachOther()
        {
            Use(800);
            yield return LoadRichLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TickDaily(16f);
            boot.TickLogbook(16f);
            Canvas.ForceUpdateCanvases();
            var root = (RectTransform)boot.Lobby!.Canvas.transform;
            var cards = new List<Rect>();
            if (boot.DailyCard != null && boot.DailyCard.Visible)
            {
                cards.Add(CanvasRect(boot.DailyCard.Root, root));
            }

            if (boot.LogbookCard != null && boot.LogbookCard.Visible)
            {
                cards.Add(CanvasRect(boot.LogbookCard.Root, root));
            }

            Assert.That(cards.Count, Is.GreaterThanOrEqualTo(1), "condición del test, al menos la tarjeta del Reto");
            float left = float.MaxValue;
            float right = float.MinValue;
            foreach (Rect r in cards)
            {
                left = Mathf.Min(left, r.xMin);
                right = Mathf.Max(right, r.xMax);
                Assert.That(r.height, Is.EqualTo(cards[0].height).Within(0.5f), "todas del mismo alto");
                Assert.That(r.width, Is.LessThanOrEqualTo(LobbyScreen.CardMaxWidthDp + 0.5f));
            }

            Assert.That((left + right) * 0.5f, Is.EqualTo(180f).Within(1f), "la fila, centrada");
        }

        // Pone la pantalla simulada de screen (640, 800 o 320) y dice cuál es.
        private static string Use(int screen)
        {
            if (screen == 320)
            {
                GameScreen.SimulatedScreen = Canvas320Tests.Screen320;
                GameScreen.SimulatedPxPerDp = Canvas320Tests.Density;
                return "320 dp";
            }

            GameScreen.SimulatedScreen = new Vector4(1080f, screen * 3f, 96f, 48f);
            return "360 × " + screen;
        }

        // El inicio del 58 con canoas en marcha, semillas y la tienda lista: Canoas, el Cántaro, la tarjeta del Reto y la píldora.
        internal static IEnumerator LoadRichLobby()
        {
            Profile profile = SettingsScreenTests.ProfileAt(58);
            profile.Wallet.Canoes.Count = 3;
            PlayModeHelpers.NoRealClockUnlessSet(); // el cronómetro, a 10 min del reloj del juego, no del sistema (T6, carril A)
            profile.Wallet.Canoes.NextRegenAt = LaunchOptions.ClockOverride!.UtcNow + System.TimeSpan.FromMinutes(10);
            profile.Wallet.Seeds = 12345;
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            yield return null;
            boot.TickMarket(16f);
            Canvas.ForceUpdateCanvases();
        }

        internal static void Tick(HomeSloth sloth, int ms)
        {
            for (int t = 0; t < ms; t += 16)
            {
                sloth.Tick(16f);
            }
        }

        // Lo que se ve en el inicio y no puede pisarse (columna de audio, Cántaro, tarjeta del Reto, Crecida, ventanita, Canoas…).
        private static List<KeyValuePair<string, Rect>> Named(GameBootstrap boot)
        {
            LobbyScreen lobby = boot.Lobby!;
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
                named.Add(new KeyValuePair<string, Rect>("TiTino", CanvasRect(lobby.HomeWindow.Root, root)));
            }

            if (lobby.StopRemainingShown)
            {
                named.Add(new KeyValuePair<string, Rect>("letrero de la parada", CanvasRect(lobby.StopRemainingRect, root)));
            }

            if (lobby.Voyage.Visible)
            {
                named.Add(new KeyValuePair<string, Rect>("canoa", lobby.Voyage.CanoeBounds));
            }

            MarketLobbyIcons? icons = boot.MarketIcons;
            if (icons != null && icons.PiggyShown)
            {
                named.Add(new KeyValuePair<string, Rect>("cántaro", CanvasRect(icons.PiggyRect, root)));
            }

            LeagueLobbyButton? league = boot.LeagueButton;
            if (league != null && league.Shown)
            {
                named.Add(new KeyValuePair<string, Rect>("liga", CanvasRect(league.Rect, root)));
            }

            if (boot.DailyCard != null && boot.DailyCard.Visible)
            {
                named.Add(new KeyValuePair<string, Rect>("tarjeta del Reto", CanvasRect(boot.DailyCard.Root, root)));
            }

            if (boot.LogbookCard != null && boot.LogbookCard.Visible)
            {
                named.Add(new KeyValuePair<string, Rect>("tarjeta de la Bitácora", CanvasRect(boot.LogbookCard.Root, root)));
            }

            if (lobby.StreakWidget.Visible)
            {
                named.Add(new KeyValuePair<string, Rect>("crecida", CanvasRect(lobby.StreakWidget.Root, root)));
            }

            return named;
        }

        private static void AssertNoOverlaps(List<KeyValuePair<string, Rect>> named, float width, string where)
        {
            for (int i = 0; i < named.Count; i++)
            {
                Rect r = named[i].Value;
                Assert.That(r.xMin >= -0.5f && r.xMax <= width + 0.5f, Is.True, where + ": " + named[i].Key + " dentro de la pantalla");
                for (int j = i + 1; j < named.Count; j++)
                {
                    Assert.That(Shrunk(named[i].Value).Overlaps(Shrunk(named[j].Value)), Is.False, where + ": " + named[i].Key + " " + named[i].Value + " y " + named[j].Key + " " + named[j].Value + " se solapan");
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
