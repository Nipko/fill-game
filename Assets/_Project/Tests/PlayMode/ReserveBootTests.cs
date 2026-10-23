#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Levels;
using AF.RemoteConfig;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La reserva del 20 % en el arranque real (D-033, añadido «la reserva del 20 %», Q-B11-7 a; D-020, pregunta 31 b): la escena
    /// lleva los paquetes de reserva y el arranque lee <c>mech_enabled_*</c> y <c>level_overrides</c> de Remote Config antes de
    /// crear el catálogo. Con la Hoja apagada y el 43 apuntando a su tablero de reserva, se juega ese tablero, sin mecánicas.
    /// </summary>
    public sealed class ReserveBootTests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.ResetSessionOverrides();
            LaunchOptions.Reset();
            RC.Reset();
        }

        [UnityTest]
        public IEnumerator TheBoot_TakesTheKillSwitchAndTheOverride_FromRemoteConfig_AndServesTheReserveBoard()
        {
            GameBootstrap.RemoteConfigBackendOverride = new FakeRemoteConfigBackend()
                .Set("mech_enabled_hoja", "false")
                .Set("level_overrides", "{\"43\":\"m03_v1_n0043_r1\"}")
                .StoreFetched();
            yield return PlayModeHelpers.LoadGame(43, new MemoryProgressStore(43, 42));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ReservePackAssets.Count, Is.EqualTo(7), "la escena lleva los paquetes de reserva de los mundos 3-9");
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Catalog.Switches.IsEnabled(MechanicSwitch.Hoja), Is.False, "mech_enabled_hoja de Remote Config");
            Assert.That(flow.Catalog.IsOverridden(43), Is.True);
            Assert.That(flow.Level.Id, Is.EqualTo("m03_v1_n0043_r1"), "el tablero de reserva del 43");
            Assert.That(flow.Level.Replaces, Is.EqualTo("m03_v1_n0043"));
            Assert.That(MechanicSwitches.UsedMask(flow.Level), Is.EqualTo(0), "sin la Hoja");
            Assert.That(flow.Catalog.Count, Is.EqualTo(700), "la reserva no añade niveles de campaña");
        }

        [UnityTest]
        public IEnumerator WithEverythingOn_TheOverrideDoesNotChangeTheLevel()
        {
            GameBootstrap.RemoteConfigBackendOverride = new FakeRemoteConfigBackend()
                .Set("level_overrides", "{\"43\":\"m03_v1_n0043_r1\"}")
                .StoreFetched();
            yield return PlayModeHelpers.LoadGame(43, new MemoryProgressStore(43, 42));
            LevelFlow flow = GameBootstrap.Current!.Flow!;
            Assert.That(flow.Catalog.Switches.AnyOff, Is.False);
            Assert.That(flow.Level.Id, Is.EqualTo("m03_v1_n0043"), "con todo encendido ningún nivel cambia (KS1)");
        }
    }
}
