#nullable enable
using System;
using System.Collections;
using AF.Economy;
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
    /// Capturas del inicio con el Mapa del río (tarea M2.1; D-013, punto 6, Q17-A; 06 §4.7): el fondo de la parada de «Jugar»,
    /// «Faltan {n} niveles para {lugar}» bajo «Jugar» y la ventanita de casa de TiTino (provisional: solo en el Editor y en
    /// desarrollo; nunca en la ficha, las capturas de la tienda ni la UA). Una por parada, a 3 niveles de la siguiente:
    /// <c>lobby_stop_k0</c> (8, Belém: la ventanita sin píldora), <c>lobby_stop_k2</c> (28, Alter do Chão), <c>lobby_stop_k5</c>
    /// (58, Manaos: «a unos 1.480 km»), <c>lobby_stop_k8</c> (88, Leticia) y <c>lobby_stop_k14</c> (148, Iquitos). Cada captura
    /// escribe su línea <c>AF_LOBBY_STOP</c>. Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.LobbyStopSnapshotTests</c> sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas del inicio con el sitio, «Faltan N» y la ventanita (M2.1); necesitan dispositivo gráfico.")]
    public sealed class LobbyStopSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 15, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureTheLobbyAtFiveStops()
        {
            string dir = SnapshotKit.Prepare();
            yield return Capture(dir, 0, 8);
            yield return Capture(dir, 2, 28);
            yield return Capture(dir, 5, 58);
            yield return Capture(dir, 8, 88);
            yield return Capture(dir, 14, 148);
        }

        private static IEnumerator Capture(string dir, int stop, int level)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = level;
            profile.Progress.MaxLevel = level - 1;
            profile.Wallet.Seeds = 1250;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            SnapshotKit.AttachLobby(boot);
            yield return SnapshotKit.Wait(0.3f);
            string name = "lobby_stop_k" + stop;
            Assert.That(lobby.StopBackdrop.Stop, Is.Not.Null, name);
            Assert.That(lobby.StopBackdrop.Stop!.N, Is.EqualTo(stop), name);
            Assert.That(lobby.HomeWindow.Visible, Is.True, name);
            Assert.That(lobby.StopRemainingShown, Is.True, name);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_LOBBY_STOP " + name + ": nivel " + level + ", parada " + lobby.StopBackdrop.Stop.Id + ", héroe "
                + lobby.StopBackdrop.HeroSource + " " + lobby.StopBackdrop.HeroRect + ", «" + lobby.StopRemainingText + "», «"
                + lobby.HomeWindow.DistanceText + "»");
        }
    }
}
