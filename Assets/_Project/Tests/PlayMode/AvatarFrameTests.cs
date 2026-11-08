#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.LiveOps;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los marcos de país del avatar en la escena <c>Game</c> real (Q-M6 de D-033, «se enseña en la 1.1», añadido del 2026-11-08): un
    /// perfil de la 1.0 con marcos lleva el más alto y ve el punto; «Tu avatar» enseña la fila de los marcos solo con alguno ganado y
    /// deja ponerlos y quitarlos; el retrato con marco sale en el inicio y en Ajustes; la casilla 30 de la Bitácora lo pone solo y lo
    /// enseña en la libreta con «Ver tu avatar»; el extra de desarrollo; TalkBack; y todo cabe a 320 dp y al 130 %.
    /// </summary>
    public sealed class AvatarFrameTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime T0 = new DateTime(2026, 11, 8, 15, 0, 0, DateTimeKind.Utc); // 10:00 en Bogotá

        private ManualClock _clock = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(T0, Bogota);
            LaunchOptions.ClockOverride = _clock;
            GameBootstrap.AvatarPickOverride = n => 1; // el tucán
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetAvatarOverrides();
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.LogbookOverride = null;
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        private Profile ProfileAt(int maxLevel, params string[] frames)
        {
            Profile profile = Wallet.NewProfile(_clock);
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            profile.Progress.MapSeenLevel = maxLevel;
            profile.Avatar.Id = "toucan";
            profile.Avatar.Chosen = true;
            profile.Avatar.At = T0;
            profile.Avatar.AddUnlocked("dart_frog"); // el 57 ya pasó el primer Difícil: desbloqueado y visto, sin punto por el avatar
            profile.Avatar.AddSeen("dart_frog");
            if (maxLevel >= 140)
            {
                foreach (string id in new[] { "caiman", "anteater", "jaguar" })
                {
                    profile.Avatar.AddUnlocked(id);
                    profile.Avatar.AddSeen(id);
                }
            }

            profile.Entitlements.Frames.AddRange(frames);
            return profile;
        }

        private static IEnumerator Lobby(Profile profile)
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap.Current!.GetComponent<AvatarRunner>().enabled = false;
        }

        [UnityTest]
        public IEnumerator A10Profile_WithoutFrames_LooksTheSame()
        {
            yield return Lobby(ProfileAt(57));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.FrameService!.Current, Is.Null);
            Assert.That(boot.Lobby!.AvatarDotShown, Is.False);
            Assert.That(boot.Lobby.AvatarA11y, Is.EqualTo("Tu avatar: Tucán. Toca para cambiarlo"));
            Assert.That(boot.OpenAvatarScreen("home"), Is.True);
            Assert.That(boot.AvatarView!.FramesShown, Is.False, "sin marcos ganados, «Tu avatar» como en la 1.0");
            Assert.That(boot.AvatarView.ShownFrameIds, Is.Empty);
        }

        [UnityTest]
        public IEnumerator A10ProfileWithFrames_WearsTheHighest_ShowsTheDot_AndTheScreenPutsAndTakesThemOff()
        {
            yield return Lobby(ProfileAt(57, "frame_w02", "frame_w01"));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(boot.FrameService!.Current!.Id, Is.EqualTo("frame_w02"), "sin decisión: el del mundo más alto [P]");
            Assert.That(lobby.AvatarDotShown, Is.True, "marcos sin ver: el punto del inicio");
            Assert.That(lobby.AvatarA11y, Is.EqualTo("Tu avatar: Tucán con el marco de Tapajós. Hay un marco nuevo. Toca para cambiarlo"));
            Assert.That(lobby.AvatarSprite!.name, Does.Contain("frame_w02"), "el retrato del inicio lleva el marco");

            Assert.That(boot.OpenAvatarScreen("home"), Is.True);
            AvatarScreen screen = boot.AvatarView!;
            Assert.That(screen.FramesShown, Is.True);
            Assert.That(screen.ShownFrameIds, Is.EqualTo(new[] { AvatarScreen.FrameNoneId, "frame_w01", "frame_w02" }), "«Sin marco» y los ganados, por mundo");
            Assert.That(screen.FrameCellChosen("frame_w02"), Is.True);
            Assert.That(screen.FrameCellNew("frame_w01"), Is.True);
            Assert.That(screen.FrameCellOverlay("frame_w01"), Is.Not.Null);
            Assert.That(screen.FrameCellOverlay(AvatarScreen.FrameNoneId), Is.Null);
            Assert.That(screen.FrameCellA11y("frame_w02"), Is.EqualTo("Marco de Tapajós, Brasil (Pará), puesto"));
            Assert.That(screen.FrameCellA11y("frame_w01"), Is.EqualTo("Marco de Estuario, Brasil (Pará), nuevo. Toca para ponerlo"));
            Assert.That(screen.FrameCellA11y(AvatarScreen.FrameNoneId), Is.EqualTo("Sin marco. Toca para quitar el marco"));
            Assert.That(screen.HeaderA11y, Is.EqualTo("Tucán con el marco de Tapajós"));

            screen.FrameCellButton("frame_w01").onClick.Invoke();
            Assert.That(boot.Profile!.Avatar.Frame, Is.EqualTo("frame_w01"));
            Assert.That(boot.Profile.Avatar.FrameAt, Is.EqualTo(T0));
            Assert.That(screen.FrameCellChosen("frame_w01"), Is.True);
            Assert.That(screen.PortraitSprite!.name, Does.Contain("frame_w01"), "la cabecera cambia en el acto");
            Assert.That(screen.Animating, Is.True, "el rebote del retrato");

            Assert.That(boot.ChooseAvatarFrame(null), Is.EqualTo(AvatarFrameSelectResult.Selected));
            Assert.That(boot.Profile.Avatar.Frame, Is.Null);
            Assert.That(boot.Profile.Avatar.FrameAt, Is.Not.Null, "«Sin marco» queda decidido");
            Assert.That(screen.FrameCellChosen(AvatarScreen.FrameNoneId), Is.True);
            Assert.That(screen.FrameCellA11y(AvatarScreen.FrameNoneId), Is.EqualTo("Sin marco, elegido"));
            Assert.That(screen.PortraitSprite!.name, Does.Not.Contain("frame_w"), "sin marco: el aro de madera");
            Assert.That(boot.ChooseAvatarFrame("frame_w07"), Is.EqualTo(AvatarFrameSelectResult.NotOwned));

            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.Profile.Avatar.FramesSeen, Is.EqualTo(new[] { "frame_w01", "frame_w02" }), "cerrar «Tu avatar» los deja vistos");
            Assert.That(lobby.AvatarDotShown, Is.False);
            Assert.That(lobby.AvatarA11y, Is.EqualTo("Tu avatar: Tucán. Toca para cambiarlo"));
        }

        [UnityTest]
        public IEnumerator Day30OfTheLogbook_PutsTheFrameOn_ShowsIt_AndLeadsToYourAvatar()
        {
            GameBootstrap.LogbookOverride = true;
            Profile profile = ProfileAt(57);
            profile.Calendar = new CalendarData { Cycle = 1, NextDay = 30, PlayedDate = _clock.LocalDateKey() };
            var store = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<AvatarRunner>().enabled = false;
            Assert.That(boot.Logbook, Is.Not.Null);
            LogbookScreen screen = boot.LogbookScreen!;
            if (!screen.Visible)
            {
                Assert.That(boot.OpenLogbook(LogbookOpenSource.Card), Is.True);
            }

            Assert.That(screen.PressToday(), Is.True);
            Assert.That(screen.ExtrasTexts, Does.Contain("+ Marco de país"));
            screen.CardButton(LoginCalendarChoice.Seeds).onClick.Invoke();

            Assert.That(boot.Profile!.Entitlements.Frames, Does.Contain("frame_w03"), "el 58 es Manaos: el marco del mundo en que está (06 §4.2, regla 4)");
            Assert.That(boot.Profile.Avatar.Frame, Is.EqualTo("frame_w03"), "se pone solo (como la skin ganada, Q-M8)");
            Profile onDisk = ProfileJson.Deserialize(store.Json!);
            Assert.That(onDisk.Avatar.Frame, Is.EqualTo("frame_w03"), "en la misma escritura que el cobro");
            Assert.That(screen.FrameRevealShown, Is.True);
            Assert.That(screen.FrameRevealText, Is.EqualTo("Marco de Manaos: ya lo lleva tu avatar"));
            Assert.That(screen.FrameRevealPortrait!.name, Does.Contain("frame_w03"));
            Assert.That(boot.Profile.Avatar.FramesSeen, Does.Contain("frame_w03"), "visto en la libreta: sin punto en el inicio");
            Assert.That(boot.Lobby!.AvatarDotShown, Is.False);

            Button view = screen.FrameViewButton!;
            Assert.That(view, Is.Not.Null);
            view.onClick.Invoke();
            Assert.That(screen.Visible, Is.False, "«Ver tu avatar» cierra la libreta");
            Assert.That(boot.AvatarScreenOpen, Is.True);
            Assert.That(boot.AvatarView!.FrameCellChosen("frame_w03"), Is.True);
        }

        [UnityTest]
        public IEnumerator TheDevExtra_GrantsFrames_AndPutsOnTheLastOne()
        {
            GameBootstrap.FrameGrantOverride = "1, 4,99";
            yield return Lobby(ProfileAt(57));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Profile!.Entitlements.Frames, Is.EquivalentTo(new[] { "frame_w01", "frame_w04" }));
            Assert.That(boot.FrameService!.Current!.Id, Is.EqualTo("frame_w04"));
            Assert.That(boot.Lobby!.AvatarDotShown, Is.True, "sin ver, para probar el punto en el teléfono");

            Assert.That(boot.ApplyDevFrameGrant("all"), Is.EqualTo(7), "all: los 9 de la 1.1");
            Assert.That(boot.Profile.Entitlements.Frames.Count, Is.EqualTo(9));
            Assert.That(boot.FrameService.Current!.Id, Is.EqualTo("frame_w09"));
        }

        [Test]
        public void TheDevLogbookDay_LeavesThatPageReadyToday()
        {
            Profile profile = ProfileAt(57);
            Assert.That(GameBootstrap.ApplyDevLogbookDay(profile, _clock, 31), Is.False);
            Assert.That(GameBootstrap.ApplyDevLogbookDay(profile, _clock, 30), Is.True);
            Assert.That(profile.Calendar!.NextDay, Is.EqualTo(30));
            Assert.That(profile.Calendar.LastClaimDate, Is.Null);
            Assert.That(profile.Calendar.PlayedDate, Is.EqualTo(_clock.LocalDateKey()));
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
            yield return Lobby(ProfileAt(145, "frame_w01", "frame_w02", "frame_w03", "frame_w04", "frame_w05", "frame_w06", "frame_w07", "frame_w08", "frame_w09"));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenAvatarScreen("home"), Is.True);
            boot.AvatarAtlasNow?.PaintAll();
            yield return SnapshotKit.Wait(0.2f);
            AvatarScreen screen = boot.AvatarView!;
            Assert.That(screen.ShownFrameIds.Count, Is.EqualTo(10));
            Canvas.ForceUpdateCanvases();
            Assert.That(screen.AnimalsTitleRect.anchoredPosition.y, Is.LessThan(screen.FrameCellRect("frame_w09").anchoredPosition.y - 1f),
                "«Animales» va debajo de la fila de los marcos");
            inventory.Scan("frame_screen");

            var mine = new List<string>();
            foreach (string line in inventory.Lines)
            {
                if (line.Contains("Frame") || line.Contains("frame") || line.Contains("Avatar"))
                {
                    mine.Add(line);
                }
            }

            Assert.That(mine, Is.Empty, "textos de los marcos que se salen o bajan de 12 sp en " + locale + " al 130 %:\n" + string.Join("\n", mine));
        }
    }

    /// <summary>
    /// Capturas de los marcos de país para el dueño (D-050: «el dueño ve capturas antes de integrar»; Q-M6, añadido del 2026-11-08): la
    /// libreta con el marco recién ganado, «Tu avatar» con la fila de los marcos y el inicio con el retrato enmarcado, a 360 dp en
    /// es-419 y al 130 % en pt-BR. <c>unity test &lt;repo&gt; --mode PlayMode --filter AvatarFrameSnapshotTests</c> (las deja en
    /// <c>Logs/snapshots/&lt;idioma&gt;/</c>).
    /// </summary>
    [Explicit("Capturas de los marcos de país (Q-M6); necesitan dispositivo gráfico.")]
    public sealed class AvatarFrameSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetAvatarOverrides();
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.LogbookOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureFrames([Values(LocLocale.SourceLocale, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = SnapshotKit.Prepare(locale);
            float scale = locale == LocLocale.SourceLocale ? 1f : 1.3f;
            LaunchOptions.FontScaleOverride = scale;
            string suffix = locale == LocLocale.SourceLocale ? "_360" : "_360_130";
            var clock = new ManualClock(new DateTime(2026, 11, 8, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.AvatarPickOverride = n => 1;
            GameBootstrap.LogbookOverride = true;
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.MaxLevel = 57;
            profile.Progress.NextLevel = 58;
            profile.Progress.MapSeenLevel = 57;
            profile.Wallet.Seeds = 2450;
            profile.Avatar.Id = "jaguar";
            profile.Avatar.Chosen = true;
            profile.Avatar.AddUnlocked("jaguar");
            profile.Avatar.AddSeen("jaguar");
            profile.Entitlements.Frames.Add("frame_w01");
            profile.Entitlements.Frames.Add("frame_w02");
            profile.Avatar.AddFrameSeen("frame_w01");
            profile.Avatar.AddFrameSeen("frame_w02");
            profile.Calendar = new CalendarData { Cycle = 1, NextDay = 30, PlayedDate = clock.LocalDateKey() };
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            LogbookScreen logbook = boot.LogbookScreen!;
            if (!logbook.Visible)
            {
                Assert.That(boot.OpenLogbook(LogbookOpenSource.Card), Is.True);
            }

            Assert.That(logbook.PressToday(), Is.True);
            SnapshotKit.AttachOverlay(logbook.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "frame_00_logbook_choose" + suffix);
            logbook.CardButton(LoginCalendarChoice.Seeds).onClick.Invoke();
            yield return SnapshotKit.Wait(0.6f);
            SnapshotKit.Save(dir, "frame_01_logbook" + suffix);

            logbook.FrameViewButton!.onClick.Invoke();
            boot.AvatarAtlasNow!.PaintAll();
            SnapshotKit.AttachOverlay(boot.AvatarView!.Canvas);
            yield return SnapshotKit.Wait(0.4f);
            SnapshotKit.Save(dir, "frame_02_screen" + suffix);
            boot.ChooseAvatarFrame("frame_w01");
            yield return SnapshotKit.Wait(0.4f);
            SnapshotKit.Save(dir, "frame_03_chosen" + suffix);
            boot.CloseAvatarScreen();

            SnapshotKit.AttachLobby(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "frame_04_lobby" + suffix);
        }
    }
}
