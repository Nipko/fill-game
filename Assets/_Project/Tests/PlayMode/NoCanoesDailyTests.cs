#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// «Mientras esperas: Jugar el Reto del río» de Sin canoas (enganche E7; 04 §7.3; 04 §7.1: el Reto no requiere canoas; 04 §11
    /// y 08 §8.2: <c>af_no_canoes_choice</c> = <c>daily_challenge</c>): la lógica de <c>GameBootstrap.Daily.cs</c>
    /// (<see cref="GameBootstrap.NoCanoesDailyAvailable"/> y <see cref="GameBootstrap.PressNoCanoesDaily"/>), desde el inicio y
    /// desde Nivel fallido, sin gastar canoas; oculta antes del desbloqueo. El enlace dentro del panel (<c>NoCanoesScreen.cs</c>)
    /// es el parche <c>docs/dev/traspaso/spark-enganches/E7.diff</c>, con su propia prueba. Simula E2 con
    /// <see cref="GameBootstrap.SetUpDaily"/> y el pool sintético <see cref="DailyPlayTestPool"/>.
    /// </summary>
    public sealed class NoCanoesDailyTests
    {
        private InMemoryDailyChallengeStore _store = null!;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks(); // reloj 2026-10-08 10:00 en Bogotá, anuncios y compras inmediatos
            LaunchOptions.LocTablesOverride = DailyTestTables.Texts();
            _store = new InMemoryDailyChallengeStore();
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = _store;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator FromTheLobby_OpensTodaysBoard_RecordsTheChoice_AndSpendsNoCanoe()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesProfile(30)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True, "E2");
            boot.DailyRunner!.enabled = false;
            boot.TickDaily(PlayModeHelpers.FrameMs);

            Assert.That(boot.PressNoCanoesDaily(), Is.False, "sin Sin canoas delante, nada");
            boot.Lobby!.CanoesButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            Assert.That(boot.NoCanoesDailyAvailable, Is.True, "nivel 30 ≥ 10 y el Reto de hoy sin jugar");

            Assert.That(boot.PressNoCanoesDaily(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Assert.That(daily.IsOpen, Is.True, "abre el tablero de hoy");
            Assert.That(daily.Flow!.IsDaily, Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "Sin canoas se cerró; AppFlow sigue en el inicio");
            Assert.That(boot.Lobby.Visible, Is.False, "el tablero del Reto sustituye al inicio");
            Assert.That(boot.LastNoCanoesChoice, Is.EqualTo("daily_challenge"), "af_no_canoes_choice (04 §11)");
            Assert.That(boot.Profile!.Wallet.Canoes.Count, Is.EqualTo(0), "el Reto no gasta canoas (04 §7.1)");
            Assert.That(boot.NoCanoesDailyAvailable, Is.False, "con el Reto abierto ya no se ofrece");
        }

        [UnityTest]
        public IEnumerator BeforeTheUnlock_IsNotOffered()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesProfile(9)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True);
            boot.DailyRunner!.enabled = false;
            Assert.That(boot.DailyService!.CardState(9), Is.EqualTo(DailyCardState.Hidden));
            Assert.That(boot.NoCanoesDailyAvailable, Is.False, "live_unlock_level_daily = 10");
            Assert.That(boot.PressNoCanoesDaily(), Is.False);
            Assert.That(boot.LastNoCanoesChoice, Is.Null);
        }

        [UnityTest]
        public IEnumerator WithoutTheRetoInTheBuild_IsNotOffered()
        {
            GameBootstrap.DailyPoolOverride = null; // sin pool (sin E3): D-008
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesProfile(30)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.SetUpDaily();
            Assert.That(boot.DailyInBuild, Is.False);
            boot.Lobby!.CanoesButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            Assert.That(boot.NoCanoesDailyAvailable, Is.False);
            Assert.That(boot.PressNoCanoesDaily(), Is.False);
        }

        [UnityTest]
        public IEnumerator FromLevelFailed_LeavesTheLevel_AndOpensTheReto()
        {
            LaunchOptions.LevelOffsetOverride = 25; // el 31 juega el tablero 6 del tutorial (FunnelSetup.GetStuck)
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0, 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True);
            boot.DailyRunner!.enabled = false;
            boot.TapAnywhere();
            FunnelSetup.GetStuck(boot);
            boot.PressGiveUp();
            boot.ConfirmGiveUp();
            boot.PressRetry();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes), "Reintentar con 0 canoas abre Sin canoas (04 §7.3)");
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(0));
            Assert.That(boot.NoCanoesDailyAvailable, Is.True);

            Assert.That(boot.PressNoCanoesDaily(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "sale del nivel como «Volver al inicio»");
            Assert.That(boot.DailyScreen!.IsOpen, Is.True);
            Assert.That(boot.DailyScreen.Flow!.IsDaily, Is.True);
            Assert.That(boot.LastNoCanoesChoice, Is.EqualTo("daily_challenge"));
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(0), "sin canoa");
        }

        private static Profile NoCanoesProfile(int maxLevel)
        {
            Profile profile = FunnelSetup.Profile(500, 0);
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            return profile;
        }
    }
}
