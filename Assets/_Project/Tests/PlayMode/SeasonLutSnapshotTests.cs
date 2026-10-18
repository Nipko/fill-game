#nullable enable
using System;
using System.Collections;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la LUT de temporada en los fondos (tarea SE2; pregunta 32 b de docs/GDD.md §6.3, D-020; D-009, punto 6;
    /// 07 §2.1): el mismo tablero sin la LUT de temporada y con ella, en un ciclo A (aguas altas) y en uno B (aguas bajas),
    /// para ver que solo cambia la luz del panorama, del cielo y del héroe, y no la banda de agua (VA-23) ni el brillo del
    /// cielo (VA-03):
    /// <list type="bullet">
    /// <item><description><c>season_a_l41_off</c> y <c>season_a_l41_on</c> (ciclo 41, A), y <c>season_b_l41_forced</c> (la misma
    /// parada con la B forzada, <see cref="GameScreen.SeasonOverride"/>);</description></item>
    /// <item><description><c>season_b_l61_off</c> y <c>season_b_l61_on</c> (ciclo 61, B, aguas negras), y
    /// <c>season_a_l61_forced</c> (la misma parada con la A forzada);</description></item>
    /// <item><description><c>season_a_l91_on</c> (Leticia al atardecer, ciclo 81, A: las dos luces juntas);</description></item>
    /// <item><description><c>season_b_l181_on</c> (ciclo 181, B, el último de 1-200);</description></item>
    /// <item><description><c>season_b_l191_off</c>, <c>season_b_l191_on</c> y <c>season_a_l191_forced</c> (Pacaya-Samiria al
    /// alba, ciclo 181, B: la única parada <c>dawn</c> de 1-200 y con agua negra; la luz más oscura de 21-200, la del
    /// alba por la de aguas bajas);</description></item>
    /// <item><description><c>season_b_lobby_l168_off</c> y <c>season_b_lobby_l168_on</c> (el inicio, con «Jugar» en un ciclo B).</description></item>
    /// </list>
    /// Cada captura escribe su línea <c>AF_SEASON</c> con la sonda del fondo. Criterio de la revisión de SE2 (la LUT es la única
    /// señal de la temporada, sin cartel): en la franja del panorama del tablero (filas 0-380 de 2340 sin la caja del HUD,
    /// x 30-1060, y 110-270), ΔE00 medio ≥ 5 entre <c>_off</c> y <c>_on</c>, y ≥ 8 entre A y B en la misma parada (<c>_on</c>
    /// frente a <c>_forced</c>). VA-03, fuera del panel (en la 191, x 23-1057, y 240-1886), de las piezas del HUD y de la bandeja
    /// (x 285-800, y 1915-2245): media de V ≥ 0,55 y de S ≥ 0,30, y como mucho un 10 % de píxeles con V &lt; 0,25. Las dos
    /// cosas se miden sobre los PNG, fuera del test (ningún test las vigila); en la ronda final de la revisión de SE2:
    /// ΔE00 6,3 y 8,6 en la 41, 6,9 y 9,7 en la 61, y 2,8 y 3,6 en la 191 (la luz del alba tapa casi todo el panorama);
    /// VA-03 en la 191 con la B (la luz más oscura de 21-200): V 0,64, S 0,47 y un 1,3 % por debajo de V 0,25 (sin temporada,
    /// V 0,66 y S 0,48). Explícito: necesita gráficos (<c>unity test
    /// &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.SeasonLutSnapshotTests</c> sin <c>-nographics</c>); los PNG
    /// quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de la LUT de temporada (SE2); necesitan dispositivo gráfico.")]
    public sealed class SeasonLutSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 15, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SeasonLutEnabled = true;
            GameScreen.SeasonOverride = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureTheBoardsWithAndWithoutTheSeason()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureBoard(dir, "season_a_l41_off", 41, false, Season.HighWater);
            yield return CaptureBoard(dir, "season_a_l41_on", 41, true, Season.HighWater);
            yield return CaptureBoard(dir, "season_b_l41_forced", 41, true, Season.HighWater, Season.LowWater);
            yield return CaptureBoard(dir, "season_b_l61_off", 61, false, Season.LowWater);
            yield return CaptureBoard(dir, "season_b_l61_on", 61, true, Season.LowWater);
            yield return CaptureBoard(dir, "season_a_l61_forced", 61, true, Season.LowWater, Season.HighWater);
            yield return CaptureBoard(dir, "season_a_l91_on", 91, true, Season.HighWater);
            yield return CaptureBoard(dir, "season_b_l181_on", 181, true, Season.LowWater);
            yield return CaptureBoard(dir, "season_b_l191_off", 191, false, Season.LowWater);
            yield return CaptureBoard(dir, "season_b_l191_on", 191, true, Season.LowWater);
            yield return CaptureBoard(dir, "season_a_l191_forced", 191, true, Season.LowWater, Season.HighWater);
        }

        [UnityTest]
        public IEnumerator CaptureTheLobbyWithAndWithoutTheSeason()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureLobby(dir, "season_b_lobby_l168_off", 168, false);
            yield return CaptureLobby(dir, "season_b_lobby_l168_on", 168, true);
        }

        private static IEnumerator CaptureBoard(string dir, string name, int level, bool season, Season expected, Season? forced = null)
        {
            GameScreen.SeasonLutEnabled = season;
            GameScreen.SeasonOverride = forced;
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Seasons, Is.Not.Null, "la escena lleva la tabla de temporada (" + name + ")");
            Assert.That(boot.Seasons!.SeasonOf(level), Is.EqualTo(expected), "premisa: la temporada del ciclo (" + name + ")");
            SnapshotKit.Attach(boot);
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing, "fin de la intro (" + name + ")");
            yield return SnapshotKit.Wait(0.3f);
            StopBackdrop b = boot.View!.StopBackdrop;
            Assert.That(b.Season, Is.EqualTo(season ? (forced ?? expected) : Season.None), name);
            Assert.That(boot.View.PanoramaTint, Is.EqualTo(StopBackdrop.LutOf(b.Stop, b.Season)), name);
            Assert.That(b.BandColor, Is.EqualTo(Palette.WaterBase(boot.Flow!.Level.WaterBase)), name + ": la banda sin LUT (VA-23)");
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_SEASON " + name + ": nivel " + level + ", temporada " + (SeasonTable.Letter(b.Season) ?? "-") + ", luz "
                + Rgb(b.LutColor) + ", " + boot.View.LastStopProbe);
        }

        private static IEnumerator CaptureLobby(string dir, string name, int level, bool season)
        {
            GameScreen.SeasonLutEnabled = season;
            GameScreen.SeasonOverride = null;
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = level;
            profile.Progress.MaxLevel = level - 1;
            profile.Wallet.Seeds = 1250;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            SnapshotKit.AttachLobby(boot);
            yield return SnapshotKit.Wait(0.3f);
            StopBackdrop b = lobby.StopBackdrop;
            Assert.That(b.Season, Is.EqualTo(season ? Season.LowWater : Season.None), name);
            Assert.That(lobby.PanoramaTint, Is.EqualTo(StopBackdrop.LutOf(b.Stop, b.Season)), name);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_SEASON " + name + ": nivel " + level + ", temporada " + (SeasonTable.Letter(b.Season) ?? "-") + ", luz "
                + Rgb(b.LutColor) + ", parada " + (b.Stop != null ? b.Stop.Id : "-"));
        }

        private static string Rgb(Color c)
        {
            return c.r.ToString("0.000", CultureInfo.InvariantCulture) + "/" + c.g.ToString("0.000", CultureInfo.InvariantCulture) + "/"
                + c.b.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }
}
