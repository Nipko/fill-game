#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Analytics;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using UnityEngine.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los avatares del jugador en la escena <c>Game</c> real (D-056 y D-057; <c>docs/dev/avatares.md</c> §3): el retrato del inicio desde
    /// el 10 con el punto «nuevo», la fila de Ajustes, «Tu avatar» (elegir, el bloqueado con su globo, lo visto), el default al azar
    /// entre los libres, los hitos de un perfil anterior, <c>ui_avatars_enabled</c>, la Liga (cada fila con su avatar, el disco neutro,
    /// el del jugador y el envío del índice), la línea de la victoria, TalkBack, y que todo cabe a 320 dp y al 130 % en los tres
    /// idiomas.
    /// </summary>
    public sealed class AvatarTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc);

        private ManualClock _clock = null!;
        private FakeLeagueApi _api = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(T0);
            _api = new FakeLeagueApi(_clock) { StartPoints = 140 };
            GameBootstrap.LeagueApiOverride = _api;
            GameBootstrap.LeagueClockOverride = _clock;
            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = true };
            GameBootstrap.AvatarPickOverride = n => 1; // el tucán, para que las capturas y los textos no dependan del azar
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetAvatarOverrides();
            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
        }

        private static Profile ProfileAt(int maxLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            return profile;
        }

        private static IEnumerator Lobby(Profile profile)
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<LeagueRunner>().enabled = false;
            boot.GetComponent<AvatarRunner>().enabled = false;
            boot.TickLeague(16f);
        }

        private static IEnumerator Lobby(int maxLevel) => Lobby(ProfileAt(maxLevel));

        private static Rect CanvasRect(RectTransform rt, RectTransform root)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 a = root.InverseTransformPoint(corners[0]);
            Vector3 b = root.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        [UnityTest]
        public IEnumerator FirstRun_AssignsAFreeOne_WithoutAsking_AndShowsItFrom10()
        {
            yield return Lobby(9);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Avatars!.Data.Id, Is.EqualTo("toucan"), "uno de los 6 libres, al azar (Q-AV-2)");
            Assert.That(boot.Avatars.Data.Chosen, Is.False);
            Assert.That(boot.Profile!.Avatar.Id, Is.EqualTo("toucan"), "guardado en el perfil");
            Assert.That(boot.Lobby!.AvatarShown, Is.False, "antes del 10, el inicio sigue igual (Q-AV-3)");
            Assert.That(boot.OpenAvatarScreen("home"), Is.False);
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.SettingsView!.AvatarRowShown, Is.False);
            boot.CloseSettings();

            yield return Lobby(10);
            boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(lobby.AvatarShown, Is.True);
            Assert.That(lobby.AvatarDotShown, Is.True, "sin elegir nunca: el punto invita a cambiarlo");
            Assert.That(lobby.AvatarSprite, Is.Not.Null);
            Assert.That(lobby.AvatarRect.rect.width, Is.GreaterThanOrEqualTo(48f), "área táctil (07 §7.8)");
            Assert.That(lobby.AvatarA11y, Is.EqualTo("Tu avatar: Tucán. Hay un avatar nuevo. Toca para cambiarlo"), "con el punto, una frase entera (sin concatenar)");
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.SettingsView!.AvatarRowShown, Is.True, "la fila de Ajustes, arriba del todo");
            Assert.That(boot.SettingsView.AvatarRowA11y, Is.EqualTo("Tu avatar: Tucán. Toca para cambiarlo"));
        }

        [UnityTest]
        public IEnumerator TheTopRow_DoesNotOverlap_At360And320([Values(false, true)] bool narrow)
        {
            if (narrow)
            {
                GameScreen.SimulatedScreen = Canvas320Tests.Screen320;
                GameScreen.SimulatedPxPerDp = Canvas320Tests.Density;
            }
            else
            {
                GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            }

            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            LobbyScreen lobby = boot.Lobby!;
            RectTransform root = (RectTransform)lobby.Canvas.transform;
            Assert.That(lobby.CanvasWidth, Is.EqualTo(narrow ? 320f : 360f).Within(1f));
            Rect avatar = CanvasRect(lobby.AvatarRect, root);
            Rect seeds = CanvasRect(lobby.SeedsRect, root);
            Rect canoes = CanvasRect((RectTransform)lobby.CanoesButton.transform, root);
            Rect settings = CanvasRect((RectTransform)lobby.SettingsButton.transform, root);
            Assert.That(avatar.xMax, Is.LessThanOrEqualTo(seeds.xMin + 0.5f), "el avatar, a la izquierda de Semillas");
            Assert.That(seeds.xMax, Is.LessThanOrEqualTo(canoes.xMin + 0.5f));
            Assert.That(canoes.xMax, Is.LessThanOrEqualTo(settings.xMin + 0.5f));
            Assert.That(avatar.xMin - root.rect.xMin, Is.EqualTo(LobbyScreen.AvatarLeftDp).Within(0.5f), "el retrato en x = 8");
            Assert.That(settings.xMax - root.rect.xMin, Is.LessThanOrEqualTo(lobby.CanvasWidth + 0.5f));
            LobbyScreen.TopRowMetrics(narrow ? 320f : 360f, true, out float sx, out float sw, out float cx, out float cw);
            if (narrow)
            {
                Assert.That(sw, Is.EqualTo(92f).Within(0.5f), "a 320 dp, Semillas de 92 dp (avatares.md §3.1)");
                Assert.That(cw, Is.EqualTo(104f).Within(0.5f), "y Canoas de 104");
            }
            else
            {
                Assert.That(sx, Is.EqualTo(60f));
                Assert.That(sw, Is.EqualTo(108f).Within(0.5f), "a 360 dp, Semillas de 108 dp");
                Assert.That(cx, Is.EqualTo(172f).Within(0.5f));
            }
        }

        [UnityTest]
        public IEnumerator TheScreen_ChoosesAFreeOne_RejectsALockedOne_AndMarksSeen()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            lobby.AvatarButton.onClick.Invoke();
            Assert.That(boot.AvatarScreenOpen, Is.True);
            Assert.That(sink.Last("af_avatar_open")!.Find("source")!.Value.Text, Is.EqualTo("home"));
            AvatarScreen screen = boot.AvatarView!;
            Assert.That(screen.NameText, Is.EqualTo("Tucán"));
            Assert.That(screen.ShownIds.Count, Is.EqualTo(16), "con la Liga encendida, también el ocelote (Q-AV-8)");
            Assert.That(screen.ShownIds[0], Is.EqualTo("macaw"), "primero los libres, en el orden de la tabla");
            Assert.That(screen.CellChosen("toucan"), Is.True);
            Assert.That(screen.CellA11y("toucan"), Is.EqualTo("Tucán, elegido"));
            Assert.That(screen.CellLocked("dart_frog"), Is.False, "el 57 ya pasó el primer Difícil (retroactivo, ≥ 40)");
            Assert.That(screen.CellNew("dart_frog"), Is.True, "desbloqueado sin ver: «Nuevo»");
            Assert.That(screen.CellLocked("anteater"), Is.True);
            Assert.That(screen.CellGoal("anteater"), Is.EqualTo("Mundo 4 · Trapecio"));
            Assert.That(screen.CellA11y("anteater"), Is.EqualTo("Oso hormiguero, bloqueado. Completa Trapecio (te faltan 83 niveles)."));

            Assert.That(boot.ChooseAvatar("anteater"), Is.EqualTo(AvatarSelectResult.Locked));
            Assert.That(screen.BalloonText, Is.EqualTo("Completa Trapecio (te faltan 83 niveles)."), "el globo con la frase entera del hito");
            Assert.That(screen.Animating, Is.True, "la sacudida");
            Canvas.ForceUpdateCanvases();
            Assert.That(A11yScroll.ClippedOut(screen.BalloonRect), Is.False, "el globo se ve (el visor baja hasta él)");
            Assert.That(boot.Avatars!.Data.Id, Is.EqualTo("toucan"), "no se elige");

            Assert.That(boot.ChooseAvatar("tapir"), Is.EqualTo(AvatarSelectResult.Selected));
            Assert.That(boot.Profile!.Avatar.Id, Is.EqualTo("tapir"));
            Assert.That(boot.Profile.Avatar.Chosen, Is.True);
            Assert.That(screen.NameText, Is.EqualTo("Tapir"));
            Assert.That(screen.CellChosen("tapir"), Is.True);
            AnalyticsEvent select = sink.Last("af_avatar_select")!;
            Assert.That(select.Find("avatar")!.Value.Text, Is.EqualTo("tapir"));
            Assert.That(select.Find("first")!.Value.Bool, Is.True);
            Assert.That(select.Find("free")!.Value.Bool, Is.True);
            Assert.That(_api.MyAvatar, Is.EqualTo(2), "el índice del tapir, a la Liga (avatares.md §4.4)");
            Assert.That(boot.League!.Record.AvatarSent, Is.EqualTo(2));

            Assert.That(boot.PressAndroidBack(), Is.True, "Atrás cierra «Tu avatar»");
            Assert.That(boot.AvatarScreenOpen, Is.False);
            Assert.That(boot.Profile.Avatar.Seen, Does.Contain("dart_frog"), "lo nuevo que se vio pasa a visto");
            Assert.That(lobby.AvatarDotShown, Is.False, "elegido y sin nada nuevo: sin punto");
            Assert.That(lobby.AvatarA11y, Is.EqualTo("Tu avatar: Tapir. Toca para cambiarlo"));
        }

        [UnityTest]
        public IEnumerator AnOlderProfile_UnlocksItsMilestones_Retro()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            Profile profile = ProfileAt(145);
            profile.Avatar.Id = "macaw";
            profile.Avatar.Chosen = true;
            yield return Lobby(profile);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Profile!.Avatar.Unlocked, Is.EqualTo(new[] { "anteater", "caiman", "dart_frog", "jaguar" }));
            int retro = 0;
            foreach (AnalyticsEvent e in sink.Events)
            {
                if (e.Name == "af_avatar_unlock")
                {
                    Assert.That(e.Find("retro")!.Value.Bool, Is.True);
                    retro++;
                }
            }

            Assert.That(retro, Is.EqualTo(4));
            Assert.That(boot.Lobby!.AvatarDotShown, Is.True, "cuatro nuevos sin ver");
            Assert.That(sink.Last("af_avatar_unlock")!.Find("milestone")!.Value.Text, Is.EqualTo("world_4"));
        }

        [UnityTest]
        public IEnumerator WithTheSwitchOff_NothingShows_AndTheLeagueGetsNothing()
        {
            GameBootstrap.AvatarConfigOverride = new AvatarConfig { UiAvatarsEnabled = false };
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Lobby!.AvatarShown, Is.False);
            Assert.That(boot.OpenAvatarScreen("home"), Is.False);
            Assert.That(boot.Profile!.Avatar.Id, Is.Not.Null, "el perfil conserva el campo");
            Assert.That(_api.AvatarCalls, Is.EqualTo(0), "la Liga deja de enviarlo");
            Assert.That(boot.OpenLeague(), Is.True);
            Assert.That(boot.LeagueView!.AvatarsShown, Is.False, "las filas, sin avatar");
            boot.CloseLeague();
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.SettingsView!.AvatarRowShown, Is.False);
        }

        [UnityTest]
        public IEnumerator TheLeague_PaintsEachRowsAvatar_TheNeutralDisc_AndMine()
        {
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenLeague(), Is.True);
            LeagueScreen screen = boot.LeagueView!;
            Assert.That(screen.AvatarsShown, Is.True);
            boot.LeagueAvatarAtlasNow!.PaintAll();
            Assert.That(screen.RowAvatarIndex(0), Is.EqualTo(FakeLeagueApi.SampleAvatar(0)));
            Assert.That(screen.RowAvatarIndex(4), Is.Null, "sin avatar: el disco neutro");
            Assert.That(screen.RowAvatar(4), Is.Not.Null);
            Assert.That(screen.RowAvatarIndex(23), Is.Null, "un índice de una versión más nueva: el disco neutro");
            Assert.That(screen.RowAvatar(-1), Is.SameAs(boot.AvatarPortraitNow()), "la fila del jugador, con su avatar del perfil");
            Assert.That(screen.RowA11y(0), Does.StartWith("Puesto 1: "), "el avatar no se lee (es decorativo)");
            screen.MeButton!.onClick.Invoke();
            Assert.That(boot.AvatarScreenOpen, Is.True, "tocar el avatar del jugador abre «Tu avatar»");
            Assert.That(boot.AvatarView!.AliasText, Does.StartWith("En la Liga: "));
            Assert.That(boot.ChooseAvatar("coati"), Is.EqualTo(AvatarSelectResult.Selected));
            Assert.That(screen.RowAvatar(-1), Is.SameAs(boot.AvatarPortraitNow()), "un cambio se ve en el acto");
            Assert.That(boot.CloseAvatarScreen(), Is.True);
            Assert.That(boot.LeagueOpen, Is.True, "debajo sigue la Liga");
        }

        [UnityTest]
        public IEnumerator TheLeague_TheWholeOwnRow_OpensYourAvatar_AndTheOthersOpenNothing()
        {
            // Observación 4 de la jornada 4 (2026-10-27): el guion dice «tocar tu fila»; antes solo el disco del retrato abría «Tu avatar».
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenLeague(), Is.True);
            LeagueScreen screen = boot.LeagueView!;
            boot.LeagueAvatarAtlasNow!.PaintAll();
            yield return null; // un fotograma: el lienzo procesa lo que se acaba de enseñar (la prueba de impacto lo ve)
            Button me = screen.MeButton!;
            Assert.That(me.gameObject.activeInHierarchy, Is.True);
            Rect row = AfLayoutLog.WorldRect(screen.RowRect(-1));
            Rect button = AfLayoutLog.WorldRect((RectTransform)me.transform);
            Assert.That(button.size, Is.EqualTo(row.size), "el blanco es la fila entera");
            Assert.That(((RectTransform)me.transform).rect.height, Is.GreaterThanOrEqualTo(48f), "≥ 48 dp");

            // Un toque en la fila lejos del retrato (en los puntos, a la derecha) da en el botón de la fila.
            TMPro.TextMeshProUGUI points = screen.TextsOf(-1)[3];
            Assert.That(FirstClickHandler(AfLayoutLog.WorldRect(points.rectTransform).center), Is.SameAs(me.gameObject), "los puntos");
            Assert.That(FirstClickHandler(new Vector2(row.xMin + (row.width * 0.6f), row.center.y)), Is.SameAs(me.gameObject), "el centro de la fila");
            Assert.That(FirstClickHandler(new Vector2(row.xMax - 4f, row.yMin + 4f)), Is.SameAs(me.gameObject), "una esquina");

            // Las filas de los demás no abren nada.
            Rect other = AfLayoutLog.WorldRect(screen.RowRect(0));
            GameObject? otherHandler = FirstClickHandler(other.center);
            Assert.That(otherHandler, Is.Not.SameAs(me.gameObject), "la fila de otro no es la tuya");
            if (otherHandler != null)
            {
                UnityEngine.EventSystems.ExecuteEvents.Execute(otherHandler, new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current),
                    UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            }

            Assert.That(boot.AvatarScreenOpen, Is.False, "tocar la fila de otro no abre nada");

            // TalkBack: la fila y lo que hace, en un solo elemento (sin un «Tu avatar» suelto ni la fila repetida).
            Canvas.ForceUpdateCanvases();
            var items = new List<A11yItem>();
            new A11yScanner().Scan(items);
            var labels = new List<string>();
            foreach (A11yItem item in items)
            {
                labels.Add(item.Label);
            }

            string reading = screen.RowA11y(-1);
            Assert.That(reading, Does.StartWith("Tú, ").And.EndWith(AvatarText.Title));
            Assert.That(labels, Does.Contain(reading));
            Assert.That(labels, Has.No.Member(AvatarText.Title), "sin el botón suelto del retrato");
            Assert.That(A11yScanner.ButtonLabel(me, out _), Is.EqualTo(reading));

            var tap = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                eligibleForClick = true,
            };
            UnityEngine.EventSystems.ExecuteEvents.Execute(me.gameObject, tap, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            Assert.That(boot.AvatarScreenOpen, Is.True, "tocar la fila abre «Tu avatar»");
            Assert.That(boot.CloseAvatarScreen(), Is.True);
        }

        private static GameObject? FirstClickHandler(Vector2 screenPoint)
        {
            UnityEngine.EventSystems.EventSystem events = UnityEngine.EventSystems.EventSystem.current;
            var tap = new UnityEngine.EventSystems.PointerEventData(events) { position = screenPoint };
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            events.RaycastAll(tap, hits);
            return hits.Count == 0 ? null : UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject);
        }

        [UnityTest]
        public IEnumerator TheVictory_ShowsTheAvatarOnlyInTheLeagueLine()
        {
            FunnelSetup.Hooks();
            LaunchOptions.LevelOffsetOverride = 40;
            Profile profile = VictorySetup.Profile(100);
            profile.Progress.MaxLevel = 40;
            profile.Progress.NextLevel = 41;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            GameScreen view = boot.View!;
            Assert.That(view.LeagueLineShown, Is.True);
            Assert.That(view.LeagueAvatarShown, Is.True, "«⟨avatar⟩ +{n} puntos de Liga» (Q-AV-4)");
            Rect avatar = view.LeagueAvatarRect!.rect;
            Assert.That(avatar.width, Is.EqualTo(GameScreen.LeagueAvatarDp));
        }

        [UnityTest]
        public IEnumerator TalkBack_ReadsTheButton_EachCell_AndTheHeader()
        {
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenAvatarScreen("home"), Is.True);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var items = new List<A11yItem>();
            new A11yScanner().Scan(items);
            var labels = new List<string>();
            foreach (A11yItem item in items)
            {
                labels.Add(item.Label);
            }

            AvatarScreen screen = boot.AvatarView!;
            Assert.That(labels, Does.Contain(screen.CellA11y("toucan")));
            Assert.That(labels, Does.Contain(screen.CellA11y("armadillo")), "también las de abajo del visor (Q-B9-17)");
            Assert.That(labels, Does.Contain(screen.HeaderA11y), "la cabecera, en un elemento");
            Assert.That(labels, Does.Not.Contain("Armadillo"), "el nombre suelto no se lee aparte");
        }

        [UnityTest]
        public IEnumerator EverythingFits_At130And320_InEsEnAndPt(
            [Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(false, true)] bool narrow)
        {
            LaunchOptions.LanguageOverride = locale;
            LaunchOptions.FontScaleOverride = 1.3f;
            GameScreen.SimulatedScreen = narrow ? Canvas320Tests.Screen320 : HudLabelTests.Screen640;
            GameScreen.SimulatedPxPerDp = narrow ? Canvas320Tests.Density : (float?)null;
            var inventory = new LocInventory(locale, 1.3f);
            yield return Lobby(145);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.League!.RecordCampaignWin(57, RewardTier.Normal); // el seudónimo y el país de la cabecera
            yield return SnapshotKit.Wait(0.2f);
            inventory.Scan("avatar_lobby");
            Assert.That(boot.OpenAvatarScreen("home"), Is.True);
            AvatarScreen screen = boot.AvatarView!;
            Assert.That(screen.Columns, Is.LessThanOrEqualTo(3), "al 130 % (y a 320 dp), 3 columnas, o 2 si un nombre no cabe entero");
            boot.AvatarAtlasNow?.PaintAll();
            boot.ChooseAvatar("armadillo"); // el globo más largo, al final de la rejilla
            yield return SnapshotKit.Wait(0.2f);
            inventory.Scan("avatar_screen");
            boot.CloseAvatarScreen();
            Assert.That(boot.PressSettings(), Is.True);
            yield return SnapshotKit.Wait(0.2f);
            inventory.Scan("avatar_settings");
            boot.CloseSettings();
            Assert.That(boot.OpenLeague(), Is.True);
            yield return SnapshotKit.Wait(0.2f);
            inventory.Scan("avatar_league");

            var mine = new List<string>();
            foreach (string line in inventory.Lines)
            {
                if (line.Contains("Avatar") || line.Contains("avatar_") || line.Contains("League"))
                {
                    mine.Add(line);
                }
            }

            Assert.That(mine, Is.Empty, "textos de los avatares que se salen o bajan de 12 sp en " + locale + " al 130 %:\n" + string.Join("\n", mine));
        }
    }

    /// <summary>
    /// Capturas de los avatares para el dueño (D-050: «el dueño ve capturas antes de integrar»): el inicio con el retrato, «Tu avatar»
    /// con un bloqueado tocado, la fila de Ajustes, la Liga con los avatares y la línea de la victoria, a 360 dp en es-419 y al 130 % en
    /// pt-BR. <c>unity test &lt;repo&gt; --mode PlayMode --filter AvatarSnapshotTests</c> (las deja en <c>Logs/snapshots/&lt;idioma&gt;/</c>).
    /// </summary>
    [Explicit("Capturas de los avatares (D-056); necesitan dispositivo gráfico.")]
    public sealed class AvatarSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetAvatarOverrides();
            GameBootstrap.ResetLeagueOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureAvatars([Values(LocLocale.SourceLocale, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = SnapshotKit.Prepare(locale);
            float scale = locale == LocLocale.SourceLocale ? 1f : 1.3f;
            LaunchOptions.FontScaleOverride = scale;
            string suffix = locale == LocLocale.SourceLocale ? "_100" : "_130";
            var clock = new ManualClock(new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc));
            GameBootstrap.LeagueApiOverride = new FakeLeagueApi(clock) { StartPoints = 140, MyAvatar = 1 };
            GameBootstrap.LeagueClockOverride = clock;
            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = true };
            GameBootstrap.AvatarPickOverride = n => 1;
            LaunchOptions.ClockOverride = clock;
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.MaxLevel = 145;
            profile.Progress.NextLevel = 146;
            profile.Wallet.Seeds = 2450;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<LeagueRunner>().enabled = false;
            boot.League!.RecordCampaignWin(57, RewardTier.Normal);
            SnapshotKit.AttachLobby(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "avatar_01_lobby" + suffix);

            Assert.That(boot.OpenAvatarScreen("home"), Is.True);
            boot.AvatarAtlasNow!.PaintAll();
            SnapshotKit.AttachOverlay(boot.AvatarView!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "avatar_02_screen" + suffix);
            boot.ChooseAvatar("howler");
            yield return SnapshotKit.Wait(0.5f);
            SnapshotKit.Save(dir, "avatar_03_locked" + suffix);
            boot.ChooseAvatar("jaguar");
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "avatar_04_chosen" + suffix);
            boot.CloseAvatarScreen();

            Assert.That(boot.PressSettings(), Is.True);
            SnapshotKit.AttachOverlay(boot.SettingsView!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "avatar_05_settings" + suffix);
            boot.CloseSettings();

            Assert.That(boot.OpenLeague(), Is.True);
            SnapshotKit.AttachOverlay(boot.LeagueView!.Canvas);
            for (int i = 0; i < 20; i++)
            {
                boot.TickAvatars(16f);
            }

            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "avatar_06_league" + suffix);
        }
    }
}
