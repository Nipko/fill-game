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
    /// (58, Manaos: «a unos 1.480 km»), <c>lobby_stop_k8</c> (88, Leticia) y <c>lobby_stop_k14</c> (148, Iquitos); con DC20, <c>lobby_stop_k9</c> (98, Leticia al atardecer) y
    /// <c>lobby_stop_k19</c> (198, Pacaya-Samiria al alba), con la línea <c>AF_SKY_DUSK_DAWN</c> del cielo de las dos. Cada captura
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

        // DC20 (D-030, pregunta 18 b): el inicio con el atardecer de Leticia (98) y el alba de Pacaya-Samiria (198), con el cielo
        // a alfa ≤ 0,5 también en el tercio de abajo. Es la medida del cielo que vale para el dueño (AF_SKY_DUSK_DAWN): la misma
        // composición en las dos y el cielo a la vista hasta el horizonte, solo en sus píxeles (SkyMeasure).
        [UnityTest]
        public IEnumerator CaptureTheLobbyAtDuskAndDawn()
        {
            string dir = SnapshotKit.Prepare();
            SkyMeasure.Shot? dusk = null;
            yield return Capture(dir, 9, 98, s => dusk = s);
            SkyMeasure.Shot? dawn = null;
            yield return Capture(dir, 19, 198, s => dawn = s);
            Assert.That(dusk, Is.Not.Null);
            Assert.That(dawn, Is.Not.Null);
            SkyMeasure.Log("inicio lobby_stop_k9 / lobby_stop_k19", dusk!, dawn!);
        }

        private static IEnumerator Capture(string dir, int stop, int level)
        {
            return Capture(dir, stop, level, null);
        }

        private static IEnumerator Capture(string dir, int stop, int level, Action<SkyMeasure.Shot>? shot)
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
            if (shot != null)
            {
                shot(SkyMeasure.Take(dir, name, lobby.StopBackdrop));
            }
            else
            {
                SnapshotKit.Save(dir, name);
            }

            Debug.Log("AF_LOBBY_STOP " + name + ": nivel " + level + ", parada " + lobby.StopBackdrop.Stop.Id + ", héroe "
                + lobby.StopBackdrop.HeroSource + " " + lobby.StopBackdrop.HeroRect + ", «" + lobby.StopRemainingText + "», «"
                + lobby.HomeWindow.DistanceText + "»");
        }
    }
}
