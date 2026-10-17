#nullable enable
using System;
using System.Collections;
using System.Globalization;
using AF.Economy;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la pantalla del Mapa del río (tarea M3.3; D-013, puntos 4 y 5; propuesta del Mapa §3.2 y §8.4), abierta desde
    /// la pestaña del inicio con «Jugar · Nivel 58» (<c>max_level</c> 57: la canoa a 7/10 del tramo de Manaos a Anavilhanas):
    /// <c>12_map_mundo_l58</c> (vista Mundo, con la pregunta del mundo 3 en la cabecera, «Faltan 3 niveles para Anavilhanas» y
    /// «Jugar») y <c>13_map_cuenca</c> (la cuenca entera en vidrio a 1×). Es el armazón: el río que se llena, las boyas, los
    /// marcadores y los rótulos llegan con M3.4 y M3.5. Cada captura escribe su línea <c>AF_MAP</c>. Explícito: necesita
    /// gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.MapSnapshotTests</c> sin
    /// <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de la pantalla del Mapa del río (M3.3); necesitan dispositivo gráfico.")]
    public sealed class MapSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
            UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureTheWorldAndBasinViews()
        {
            string dir = SnapshotKit.Prepare();
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 58;
            profile.Progress.MaxLevel = 57;
            profile.Progress.MapSeenLevel = 57; // sin «ponerse al día» (M3.5: sus capturas, MapCatchUpSnapshotTests)
            profile.Wallet.Seeds = 1250;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            yield return null;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachOverlay(map.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(map.View, Is.EqualTo(MapView.World));
            SnapshotKit.Save(dir, "12_map_mundo_l58");
            Log("12_map_mundo_l58", map);

            map.ViewBasinButton.onClick.Invoke();
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(map.View, Is.EqualTo(MapView.Basin));
            SnapshotKit.Save(dir, "13_map_cuenca");
            Log("13_map_cuenca", map);
        }

        private static void Log(string name, RiverMapScreen map)
        {
            Debug.Log("AF_MAP " + name + ": vista " + map.View + ", zoom " + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture)
                + ", contenido " + map.ContentSize + " en " + map.ViewportSize + ", canoa " + map.CanoeInViewport + ", «" + map.Title + "», «"
                + map.StopRemainingText + "»");
        }
    }
}
