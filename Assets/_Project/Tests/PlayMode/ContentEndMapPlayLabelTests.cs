#nullable enable
using System.Collections;
using AF.LiveOps;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Observación 18 de la jornada 6 en el teléfono (2026-10-29): con el contenido acabado (<c>max_level</c> 700), el inicio decía
    /// bien «Jugar · Reto del río», pero el botón «Jugar» del Mapa del río, abierto desde el inicio, decía «Jugar · Nivel 701».
    /// 06 (D-032, punto 6) pide que nunca diga «Jugar · Nivel 701». El Mapa usa ahora la misma regla que el inicio
    /// (<see cref="LobbyScreen.PlayModeFor"/> y <see cref="LobbyScreen.PlayLabelFor"/>): con el Reto desbloqueado, «Jugar · Reto
    /// del río»; sin él, «Jugar». En es-419, en y pt-BR, el Mapa dice lo mismo que el inicio, y nunca el nivel 701.
    /// </summary>
    public sealed class ContentEndMapPlayLabelTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
            if (!Loc.IsConfigured || Loc.Locale != LocLocale.SpanishLatinAmerica)
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        [UnityTest]
        public IEnumerator After700_TheMapSaysWhatTheLobbySays_NeverLevel701(
            [Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(true, false)] bool reto)
        {
            LaunchOptions.LocTablesOverride = locale == LocLocale.SourceLocale ? null : TranslatedTables.Completed(locale, out _, out _);
            LaunchOptions.LanguageOverride = locale;
            if (reto)
            {
                GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
                GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            }
            else
            {
                GameBootstrap.DailyPoolDisabledOverride = true; // sin el Reto en el build (D-008)
            }

            yield return Fx16bTests.LoadAfter700();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(Loc.Locale, Is.EqualTo(locale));
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(boot.App!.PlayLevelAvailable, Is.False, "el 701 no está publicado");
            Assert.That(boot.DailyPlayUnlocked, Is.EqualTo(reto));
            PlayButtonMode expected = reto ? PlayButtonMode.Daily : PlayButtonMode.ContentEnd;
            string expectedLabel = reto ? UiText.PlayDaily : UiText.Play;
            string level701 = UiText.PlayLevel(701);
            Assert.That(lobby.PlayMode, Is.EqualTo(expected));
            Assert.That(lobby.PlayLabel, Is.EqualTo(expectedLabel));

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True, "el Mapa se abre desde el inicio");
            yield return null;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.PlayMode, Is.EqualTo(expected), locale + ": la misma regla que el inicio");
            Assert.That(map.PlayLabel, Is.EqualTo(lobby.PlayLabel), locale + ": el Mapa dice lo mismo que el inicio");
            Assert.That(map.PlayLabel, Is.Not.EqualTo(level701), locale + ": nunca «" + level701 + "» (D-032, punto 6)");
            Assert.That(map.PlayLabel, Does.Not.Contain("701"), locale + ": ningún 701 en «Jugar»");
            TestContext.WriteLine("AF_MAP_701 " + locale + (reto ? " con Reto" : " sin Reto") + ": inicio «" + lobby.PlayLabel + "», Mapa «" + map.PlayLabel + "»");
        }
    }
}
