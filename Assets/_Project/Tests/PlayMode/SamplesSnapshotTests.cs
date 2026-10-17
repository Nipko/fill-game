#nullable enable
using System;
using System.Collections;
using System.Globalization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del Muestrario, de la tarjeta de la parada y de los miradores del Mapa del río (tarea M4.1), con el arte
    /// provisional: <c>12i_map_muestrario_tab_l58</c> (el Mapa con max 58 y la pestaña «Muestrario · 9/n» en su fila),
    /// <c>12j_muestrario_l58</c> (una fila por mundo con sus mini cuias en la repisa: de Belém a Manaos llenas, los tres miradores
    /// con su «?», las demás vacías), <c>12k_card_manaos</c> (la tarjeta de Manaos: agua negra, «Manaos · Brasil · río Negro», el
    /// fondo de la parada, el dato de llegada, «Otro dato» y el miquito), <c>12l_card_manaos_otro_dato</c> (tras «Otro dato»),
    /// <c>12m_card_tefe_futura</c> (el lago de Tefé, futuro: la silueta y «Nivel 71»), <c>12n_card_belem</c> (Belém con sus cinco
    /// frutas), <c>12o_map_miradores_l10</c> (max 10, la vista en el estuario: los tres «?») y <c>12p_card_mirador</c> (la tarjeta de
    /// la isla de Marajó). Cada captura escribe su línea <c>AF_MAP</c>.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.SamplesSnapshotTests</c>
    /// sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas del Muestrario, la tarjeta de la parada y los miradores del Mapa del río (M4.1); necesitan dispositivo gráfico.")]
    public sealed class SamplesSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureSamplesCardsAndViewpoints()
        {
            string dir = SnapshotKit.Prepare();

            yield return Shot(dir, 59, "12i_map_muestrario_tab_l58", null);
            yield return Shot(dir, 59, "12j_muestrario_l58", m => m.OpenSamples());
            yield return Shot(dir, 59, "12k_card_manaos", m => m.OpenStopCard(5));
            yield return Shot(dir, 59, "12l_card_manaos_otro_dato", m =>
            {
                m.OpenStopCard(5);
                m.Card!.OtherFact();
            });
            yield return Shot(dir, 59, "12m_card_tefe_futura", m => m.OpenStopCard(7));
            yield return Shot(dir, 59, "12n_card_belem", m => m.OpenStopCard(0));
            yield return Shot(dir, 11, "12o_map_miradores_l10", CenterOnViewpoints);
            yield return Shot(dir, 11, "12p_card_mirador", m =>
            {
                for (int i = 0; i < m.ViewpointCount; i++)
                {
                    if (m.ViewpointAt(i).Id == "isla_de_marajo")
                    {
                        m.OpenViewpointCard(i);
                    }
                }
            });
        }

        // Lleva la vista al centro de los miradores (los tres del estuario).
        private static void CenterOnViewpoints(RiverMapScreen map)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < map.ViewpointCount; i++)
            {
                sum += map.ViewpointLocal(i);
            }

            Vector2 c = sum / Mathf.Max(1, map.ViewpointCount);
            Vector2 p = new Vector2(map.ContentOffset.x + c.x, map.ContentOffset.y - c.y);
            map.Drag(new Vector2((map.ViewportSize.x * 0.5f) - p.x, p.y - (map.ViewportSize.y * 0.5f)));
        }

        private static IEnumerator Shot(string dir, int playLevel, string name, Action<RiverMapScreen>? before)
        {
            yield return MapMarkersTests.OpenApp(playLevel);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            before?.Invoke(map);
            map.Tick(16f);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
            StopCard? card = map.Card;
            Debug.Log("AF_MAP " + name + ": muestras " + map.SamplesEarned.ToString(CultureInfo.InvariantCulture) + "/"
                + map.SamplesTotal.ToString(CultureInfo.InvariantCulture) + ", datos " + map.FactsSeen.ToString(CultureInfo.InvariantCulture) + "/"
                + map.FactsTotal.ToString(CultureInfo.InvariantCulture) + ", tarjeta " + (card != null && card.Visible ? card.Mode + " «" + card.TitleText.Replace('\n', ' ') + "» "
                + (card.FactKey ?? "-") + ", frutas " + card.FruitCount.ToString(CultureInfo.InvariantCulture) + ", miquito «" + card.FaunaName + "»" : "no"));
        }
    }
}
