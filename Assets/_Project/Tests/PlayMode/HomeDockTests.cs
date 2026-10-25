#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Audio;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El inicio «el muelle del río» (D-050), en la escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>tocar la canoa o la parada abre el Mapa del río con <c>af_map_open</c> <c>source = lobby</c> (08 §8.2), solo desde
    /// <c>max_level</c> ≥ 10 (antes, la travesía se ve pero no se toca: D-008);</item>
    /// <item>con Reducir movimiento, el río, la canoa y Rosa quedan quietos (07 §7.8), y sin él se mueven;</item>
    /// <item>sin asignaciones por fotograma (08 §5.4), con el pez, el ave, la canoa y la comprobación del bocadillo;</item>
    /// <item>TalkBack: la canoa y la parada («… Toca para abrir el Mapa del río»), las tarjetas del día y el poste de la Crecida;</item>
    /// <item>la sonda <c>AF_LAYOUT lobby_cards</c> con las tarjetas;</item>
    /// <item>sin solapes ni textos cortados a 360 × 780, 360 × 640 y 320 dp, al 100 % y al 130 %, en es-419, en, pt-BR y en-XA,
    /// con todo a la vista (Canoas, el Cántaro, la Crecida, la Liga, el Reto y la travesía); y antes del 10, la caja de cuias.</item>
    /// </list>
    /// </summary>
    public sealed class HomeDockTests
    {
        private static readonly int[] Screens = { 780, 640, 320 };

        [SetUp]
        public void SetUp()
        {
            AudioCues.Override = new FakeAudioService();
        }

        [TearDown]
        public void TearDown()
        {
            HomeSloth.ManualTickForTests = false;
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.LogbookOverride = null; // la deja apagada para los demás tests (PlayModeHelpers.NoLogbookUnlessSet)
            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.A11yHostOverride = null;
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            AudioCues.Override = null;
            AudioCues.Install(null);
        }

        [UnityTest]
        public IEnumerator TheCanoeAndTheStop_OpenTheMap_WithSourceLobby_From10Only()
        {
            var lines = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_open\"", StringComparison.Ordinal))
                {
                    lines.Add(message);
                }
            }

            Application.logMessageReceived += OnLog;
            try
            {
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(58)));
                GameBootstrap boot = GameBootstrap.Current!;
                LobbyScreen lobby = boot.Lobby!;
                yield return null;
                HomeVoyage voyage = lobby.Voyage;
                Assert.That(voyage.Visible, Is.True, "la travesía hacia Anavilhanas");
                Assert.That(voyage.Interactive, Is.True, "con el Mapa (max_level 57)");
                Assert.That(((RectTransform)voyage.CanoeButton.transform).rect.width, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.01f), "07 §7.8");
                Assert.That(((RectTransform)voyage.StopButton.transform).rect.height, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.01f));

                voyage.CanoeButton.onClick.Invoke();
                Assert.That(boot.RiverMapView!.Visible, Is.True, "la canoa abre el Mapa");
                Assert.That(lines.Count, Is.EqualTo(1), string.Join("\n", lines));
                StringAssert.Contains("\"source\":\"lobby\"", lines[0], "08 §8.2: el mismo source que la pestaña del Mapa");
                yield return null;
                Assert.That(boot.CloseRiverMap(), Is.True);
                yield return null;

                voyage.StopButton.onClick.Invoke();
                Assert.That(boot.RiverMapView.Visible, Is.True, "la parada abre el Mapa");
                Assert.That(lines.Count, Is.EqualTo(2));
                StringAssert.Contains("\"source\":\"lobby\"", lines[1]);
                yield return null;
                Assert.That(boot.CloseRiverMap(), Is.True);

                // Antes del 10: la travesía se ve (hacia Santarém), pero no se toca.
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
                lobby = GameBootstrap.Current!.Lobby!;
                yield return null;
                Assert.That(lobby.Voyage.Visible, Is.True);
                Assert.That(lobby.StopRemainingText, Is.EqualTo("Faltan 7 niveles para Santarém"));
                Assert.That(lobby.Voyage.Interactive, Is.False, "D-008: el Mapa, desde el 10");
                Assert.That(lobby.Voyage.CanoeButton.gameObject.activeInHierarchy, Is.False);
                Assert.That(lobby.Voyage.StopButton.gameObject.activeInHierarchy, Is.False);
                Assert.That(lobby.DayCardCount, Is.EqualTo(0), "antes del 10, sin tarjetas del día");
                Assert.That(lobby.Crate.gameObject.activeInHierarchy, Is.True, "la caja de cuias en el muelle");
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }
        }

        [UnityTest]
        public IEnumerator ReduceMotion_KeepsTheRiverTheCanoeAndRosaStill_AndWithoutIt_TheyMove()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            Assert.That(lobby.ReduceMotion, Is.False, "premisa: sin Reducir movimiento");
            Vector2 ripple = lobby.River.FirstRipplePosition;
            Vector2 canoe = lobby.Voyage.CanoeCenter;
            Step(lobby, 1500);
            Assert.That(lobby.River.FirstRipplePosition, Is.Not.EqualTo(ripple), "las ondas derivan");
            Assert.That(lobby.Voyage.CanoeCenter, Is.Not.EqualTo(canoe), "la canoa se mece");

            Assert.That(boot.ChooseA11y(SettingsScreen.ReduceMotionId, true), Is.True);
            Assert.That(lobby.ReduceMotion, Is.True, "llega al inicio al momento");
            ripple = lobby.River.FirstRipplePosition;
            canoe = lobby.Voyage.CanoeCenter;
            float clock = lobby.River.ClockMs;
            for (int i = 0; i < 300; i++)
            {
                lobby.Tick(100f); // 30 s: varios saltos del pez y un vuelo del ave, si se movieran
                Assert.That(lobby.River.FishShown, Is.False, "sin pez con Reducir movimiento");
                Assert.That(lobby.River.BirdShown, Is.False, "sin ave con Reducir movimiento");
            }

            Assert.That(lobby.River.FirstRipplePosition, Is.EqualTo(ripple), "todo quieto");
            Assert.That(lobby.Voyage.CanoeCenter, Is.EqualTo(canoe));
            Assert.That(lobby.River.ClockMs, Is.EqualTo(clock), "el reloj del río no avanza");
            Assert.That(boot.ChooseA11y(SettingsScreen.ReduceMotionId, false), Is.True);
        }

        [UnityTest]
        public IEnumerator Tick_DoesNotAllocate_WithTheRiverTheVoyageAndTheBubbleCheck()
        {
            HomeSloth.ManualTickForTests = true;
            yield return AudioQuickTogglesTests.LoadRichLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            HomeSloth sloth = lobby.HomeWindow.Sloth!;
            sloth.Tick(16f);
            AudioQuickTogglesTests.Tick(sloth, 1400); // un mensaje a la vista: la comprobación del bocadillo contra la travesía
            Assert.That(sloth.BubbleShown, Is.True, "premisa: un mensaje de TiTino");

            // Un ciclo entero antes de medir (el salto del pez, su salpicadura y el vuelo del ave): lo que pase la primera vez por
            // cada camino no cuenta.
            for (int i = 0; i < 260; i++)
            {
                lobby.Tick(100f);
            }

            void Run240()
            {
                for (int i = 0; i < 240; i++)
                {
                    lobby.Tick(100f); // 24 s: un salto del pez y medio vuelo del ave
                    boot.PlaceDayCards();
                }
            }

            Assert.That(Run240, UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory(), "08 §5.4: el inicio, sin asignaciones por fotograma");
        }

        [UnityTest]
        public IEnumerator TalkBack_ReadsTheCanoeTheStopTheCardsAndTheCrecida()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            HomeSloth.ManualTickForTests = true;
            UseLogbook();
            yield return AudioQuickTogglesTests.LoadRichLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TickDaily(16f);
            boot.TickLogbook(16f);
            yield return null;
            var service = new A11yService(host);
            service.Refresh();
            string line = boot.Lobby!.StopRemainingText;
            Assert.That(line, Is.EqualTo("Faltan 3 niveles para Anavilhanas"));
            A11yItem? canoe = host.Find(line + ". Toca para abrir el Mapa del río");
            Assert.That(canoe, Is.Not.Null, "la canoa y la parada, con la línea entera de la parada y lo que hacen");
            Assert.That(canoe!.Role, Is.EqualTo(UnityEngine.Accessibility.AccessibilityRole.Button));
            DailyChallengeCard daily = boot.DailyCard!;
            Assert.That(host.Find(daily.TitleText + ": Jugar. Racha: 0 de 7 días"), Is.Not.Null, "la tarjeta del Reto, con la fecha y la racha");
            Assert.That(host.Find("Bitácora del río: Juega un nivel para sellar"), Is.Not.Null, "la tarjeta de la Bitácora, con el nombre y el estado largos");
            Assert.That(boot.Lobby.StreakWidget.Visible, Is.True, "condición del test, la Crecida (58)");
            bool crecida = false;
            foreach (A11yItem item in host.Items)
            {
                crecida |= item.Label.StartsWith("Crecida: ", StringComparison.Ordinal) && item.Label.Contains(" de 10");
            }

            Assert.That(crecida, Is.True, "el poste de la Crecida: «Crecida: {n} de 10»");
        }

        [UnityTest]
        public IEnumerator AfLayout_LobbyCards_PublishesTheDayCards()
        {
            using var layout = new AfLayoutLog();
            yield return AudioQuickTogglesTests.LoadRichLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TickDaily(16f);
            boot.PlaceDayCardsForTests();
            string? line = layout.LastOf("lobby_cards", 0);
            Assert.That(line, Is.Not.Null, "AF_LAYOUT lobby_cards");
            string[] ids = AfLayoutLog.ButtonIds(line!);
            Assert.That(ids, Does.Contain("daily_card"));
            Assert.That(AfLayoutLog.Button(line!, "daily_card")!.Value, Is.EqualTo(AfLayoutLog.Published(boot.DailyCard!.Root)));
        }

        // D-050, condiciones: cabe sin solapes a 360 × 780, 360 × 640 y 320 dp, al 100 % y al 130 %, en es-419, en, pt-BR y en-XA,
        // con todo a la vista; los textos del letrero y de las tarjetas, enteros y a 12 sp × escala o más.
        [UnityTest]
        public IEnumerator Fits_WithoutOverlaps_AtEverySizeScaleAndLanguage(
            [ValueSource(nameof(Screens))] int screen, [Values(1f, 1.3f)] float scale, [Values("es-419", "en", "pt-BR", "en-XA")] string language)
        {
            string where = Use(screen) + " · " + language + " al " + Mathf.RoundToInt(scale * 100f) + " %";
            LaunchOptions.FontScaleOverride = scale;
            LaunchOptions.LanguageOverride = language == "es-419" ? null : language;
            HomeSloth.ManualTickForTests = true; // sin bocadillo: lo que se mide es el muelle (el bocadillo tiene su prueba)
            var clock = new ManualClock(new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc));
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.LeagueApiOverride = new AF.LiveOps.FakeLeagueApi(clock);
            GameBootstrap.LeagueClockOverride = clock;
            GameBootstrap.LeagueConfigOverride = new AF.LiveOps.LeagueConfig { LiveLeagueEnabled = true };
            UseLogbook(); // las tres tarjetas del día: el caso más estrecho de D-050
            yield return AudioQuickTogglesTests.LoadRichLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TickLeague(16f);
            boot.TickDaily(16f);
            boot.TickLogbook(16f);
            LobbyScreen lobby = boot.Lobby!;
            lobby.Tick(16f);
            Canvas.ForceUpdateCanvases();
            Assert.That(boot.DailyCard!.Visible && boot.LeagueButton!.Shown && boot.LogbookCard!.Visible, Is.True, where + ": condición del test, las tres tarjetas del día");
            Assert.That(lobby.DayCardCount, Is.EqualTo(3), where);
            Assert.That(lobby.StopRemainingShown && lobby.CanoesShown && lobby.StreakWidget.Visible && lobby.HomeWindow.Visible, Is.True,
                where + ": condición del test, la travesía, Canoas, la Crecida y TiTino a la vista");
            Assert.That(boot.MarketIcons!.PiggyShown, Is.True, where + ": condición del test, el Cántaro en el muelle");
            if (screen == 780 && scale < 1.01f)
            {
                // El plan B de la fila (sin fecha, sin estado) solo cuando no cabe: en el teléfono de referencia al 100 %, nunca.
                Assert.That(boot.DailyCard.Dated && !boot.DailyCard.Card.Compact && !boot.LogbookCard.Card.Compact && !boot.LeagueButton.Card.Compact,
                    Is.True, where + ": el Reto con su fecha y las tres con su línea de estado");
                Assert.That(boot.DailyCard.TitleText, Does.Contain("16"), where + ": la fecha corta en el título del Reto (06 §4.1)");
            }


            var root = (RectTransform)lobby.Canvas.transform;
            List<KeyValuePair<string, Rect>> named = Named(boot, root);
            Debug.Log("AF_DOCK_FIT " + where + ": " + string.Join(", ", named.ConvertAll(n => n.Key + " " + n.Value)));
            float w = lobby.CanvasWidth;
            float h = lobby.CanvasHeight;
            for (int i = 0; i < named.Count; i++)
            {
                Rect r = named[i].Value;
                Assert.That(r.xMin >= -0.5f && r.xMax <= w + 0.5f, Is.True, where + ": " + named[i].Key + " " + r + " dentro de la pantalla");
                Assert.That(r.yMin >= lobby.SafeTop - 0.5f && r.yMax <= h - lobby.SafeBottom + 0.5f, Is.True, where + ": " + named[i].Key + " " + r + " en la zona segura");
                for (int j = i + 1; j < named.Count; j++)
                {
                    Assert.That(Shrunk(named[i].Value).Overlaps(Shrunk(named[j].Value)), Is.False, where + ": " + named[i].Key + " " + named[i].Value + " y " + named[j].Key + " " + named[j].Value + " se solapan");
                }
            }

            var inventory = new LocInventory(language, scale);
            var texts = new List<TMPro.TextMeshProUGUI>();
            texts.AddRange(lobby.StopRemainingRect.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false));
            texts.AddRange(boot.DailyCard.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false));
            texts.AddRange(boot.LeagueButton.Rect.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false));
            texts.AddRange(boot.LogbookCard.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false));
            texts.AddRange(lobby.StreakWidget.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false));
            foreach (TMPro.TextMeshProUGUI t in texts)
            {
                if (t.text.Length == 0)
                {
                    continue;
                }

                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * scale) - 0.01f), where + ": " + t.name + " «" + t.text + "» no baja de 12 sp × escala");
                Assert.That(inventory.ProblemsOf(t), Is.Empty, where + ": " + t.name + " «" + t.text + "» cabe");
            }
        }

        // La Bitácora del río encendida (los tests la apagan por defecto: PlayModeHelpers) y sin jugar hoy: su tarjeta dice «Juega un
        // nivel» y no hay takeover que abra la libreta encima.
        internal static void UseLogbook()
        {
            GameBootstrap.LogbookOverride = true;
            GameBootstrap.LogbookStoreOverride = new AF.LiveOps.InMemoryLoginCalendarStore(new AF.LiveOps.LoginCalendarRecord { PlayedDate = null });
        }

        // Lo que se ve en el inicio y no puede pisarse.
        private static List<KeyValuePair<string, Rect>> Named(GameBootstrap boot, RectTransform root)
        {
            LobbyScreen lobby = boot.Lobby!;
            var named = new List<KeyValuePair<string, Rect>>
            {
                new KeyValuePair<string, Rect>("semillas", CanvasRect(lobby.SeedsRect, root)),
                new KeyValuePair<string, Rect>("canoas", CanvasRect((RectTransform)lobby.CanoesButton.transform, root)),
                new KeyValuePair<string, Rect>("ajustes", CanvasRect((RectTransform)lobby.SettingsButton.transform, root)),
                new KeyValuePair<string, Rect>("música", CanvasRect(lobby.Audio.MusicRect, root)),
                new KeyValuePair<string, Rect>("sonido", CanvasRect(lobby.Audio.SoundRect, root)),
                new KeyValuePair<string, Rect>("jugar", CanvasRect((RectTransform)lobby.PlayButton.transform, root)),
                new KeyValuePair<string, Rect>("inicio", CanvasRect(lobby.HomeTabRect, root)),
                new KeyValuePair<string, Rect>("letrero", CanvasRect(lobby.StopRemainingRect, root)),
                new KeyValuePair<string, Rect>("canoa", lobby.Voyage.CanoeBounds),
                new KeyValuePair<string, Rect>("tarjeta del Reto", CanvasRect(boot.DailyCard!.Root, root)),
                new KeyValuePair<string, Rect>("tarjeta de la Liga", CanvasRect(boot.LeagueButton!.Rect, root)),
            };
            if (lobby.ChipShown)
            {
                named.Add(new KeyValuePair<string, Rect>("chip", CanvasRect(lobby.ChipRect, root)));
            }

            if (lobby.MapTabShown)
            {
                named.Add(new KeyValuePair<string, Rect>("mapa", CanvasRect(lobby.MapTabRect, root)));
            }

            if (lobby.HomeWindow.Visible)
            {
                named.Add(new KeyValuePair<string, Rect>("TiTino", CanvasRect(lobby.HomeWindow.Root, root)));
            }

            if (lobby.StreakWidget.Visible)
            {
                named.Add(new KeyValuePair<string, Rect>("crecida", CanvasRect(lobby.StreakWidget.Root, root)));
            }

            MarketLobbyIcons? icons = boot.MarketIcons;
            if (icons != null && icons.PiggyShown)
            {
                named.Add(new KeyValuePair<string, Rect>("cántaro", CanvasRect(icons.PiggyRect, root)));
            }

            if (icons != null && icons.MarketShown)
            {
                named.Add(new KeyValuePair<string, Rect>("mercado", CanvasRect(icons.MarketRect, root)));
            }

            if (boot.LogbookCard != null && boot.LogbookCard.Visible)
            {
                named.Add(new KeyValuePair<string, Rect>("tarjeta de la Bitácora", CanvasRect(boot.LogbookCard.Root, root)));
            }

            return named;
        }

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

        private static void Step(LobbyScreen lobby, int ms)
        {
            for (int t = 0; t < ms; t += 16)
            {
                lobby.Tick(16f);
            }
        }

        // Medio dp menos por lado: dos cajas que solo se tocan no se solapan.
        private static Rect Shrunk(Rect r)
        {
            return Rect.MinMaxRect(r.xMin + 0.5f, r.yMin + 0.5f, r.xMax - 0.5f, r.yMax - 0.5f);
        }

        // Rectángulo de rt en dp del lienzo del inicio, con origen arriba a la izquierda y la y hacia abajo.
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
    }
}
