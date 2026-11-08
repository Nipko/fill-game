#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los arreglos del Mapa v2 tras la prueba de la 1.1 en el teléfono (añadido de D-069 del 2026-11-08;
    /// <c>docs/dev/traspaso/pruebas-telefono-2026-11-07-1.1.md</c>), en la escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>observación 3: el viaje de un Mapa con viaje dura 4,5 s el tramo (450 ms por boya), un toque lo deja en 1,5 s y la
    /// pestaña sigue poniéndose al día a 150 ms por boya;</item>
    /// <item>observación 2: ningún rótulo de pueblo o de parada ni ninguna viñeta pisa la canoa con Capi, su bandera, el guía y el
    /// tucuxi (como quedan con <c>max_level</c>), en toda la campaña;</item>
    /// <item>observación 4: «Faltan N» va en el rótulo de la parada siguiente también si es una parada en abanico (Leticia, tras
    /// el viaje del 70): sin la placa de arriba a la izquierda; y la píldora del Premio del mundo no pisa «Andes ↑».</item>
    /// </list>
    /// </summary>
    public sealed class MapJourneyPaceTests
    {
        private static readonly System.DateTime T0 = new System.DateTime(2026, 11, 8, 12, 0, 0, System.DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator VerMapa_TheLegTakes4500ms_ATapLeavesPlayAt1500_AndTheTabStaysAt150PerBuoy()
        {
            yield return MapJourneyTests.OpenFromVictory20();
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.CatchUpBuoys, Is.EqualTo(10));
            Assert.That(map.CatchUpDurationMs, Is.EqualTo(4500), "un tramo de 10 niveles, 4,5 s (antes 1,5)");
            Assert.That(map.CatchUpPerBuoyMs, Is.EqualTo(MapConfig.JourneyPerBuoyMs).Within(1e-3f));

            // Sin tocar: a los 3 s la canoa sigue remando (antes ya había llegado a los 1,75 s) y llega a los 4,75 s.
            Level10MapTests.Tick(map, 3000);
            Assert.That(map.CatchingUp, Is.True, "a los 3 s, aún de viaje");
            Assert.That(map.PlayShown, Is.False);
            Level10MapTests.Tick(map, 4500 + RiverMapScreen.JourneyLeadMs - 3000 + PlayModeHelpers.FrameMs);
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.JourneyEndedBy, Is.EqualTo("time"));
            Assert.That(map.JourneyReadyMs, Is.EqualTo(4500f + RiverMapScreen.JourneyLeadMs).Within(2f * PlayModeHelpers.FrameMs));
            Assert.That(map.PlayShown, Is.True);
            map.Close();

            // Con un toque al abrir, «Jugar» llega tan pronto como antes del cambio (1,5 s).
            Assert.That(boot.View!.VictoryMapButton, Is.Not.Null);
            boot.View.VictoryMapButton.onClick.Invoke();
            Assert.That(map.Visible, Is.True);
            Assert.That(map.JourneyLocked, Is.True);
            map.JourneyTouchArea!.onClick.Invoke();
            for (int ms = 0; ms < 3000 && map.JourneyLocked; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(map.JourneyLocked, Is.False);
            Assert.That(map.JourneyReadyMs, Is.LessThanOrEqualTo(1500f + (2f * PlayModeHelpers.FrameMs)), "con un toque, como antes");
            map.Close();

            // La pestaña (ponerse al día, sin viaje de D-053): como siempre, 150 ms por boya.
            OpenLobby(boot, new MemoryProgressStore(21, 20, 14), 21);
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(map.CatchUpPerBuoyMs, Is.EqualTo(MapConfig.CatchUpPerBuoyMs).Within(1e-3f));
            Assert.That(map.CatchUpDurationMs, Is.EqualTo(RiverJourney.CatchUpMs(14, 20)));
            map.Close();
        }

        [UnityTest]
        public IEnumerator NoTownOrStopLabel_NorVignette_TouchesTheCanoeWithItsCrew_AcrossTheCampaign()
        {
            yield return MapMarkersTests.OpenApp(56);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.LiveActive, Is.True);
            int checkedLabels = 0;
            var missing = new System.Collections.Generic.List<string>();
            var unseen = new System.Collections.Generic.List<string>();
            var offView = new System.Collections.Generic.List<string>();
            for (int play = 2; play <= 700; play += 5)
            {
                OpenLobby(boot, new MemoryProgressStore(play, play - 1, play - 1), play);
                Canvas.ForceUpdateCanvases();
                string at = "jugando el " + play;
                Assert.That(map.CrewLabelBoxCount, Is.GreaterThanOrEqualTo(1), at + ": la caja de la canoa");
                for (int n = 0; n <= 70; n++)
                {
                    Rect r = map.StopLabelLocalRect(n);
                    if (r.width > 0f)
                    {
                        checkedLabels++;
                        AssertClear(map, r, at + ", rótulo de la parada " + n + " «" + map.StopLabelText(n).Replace("\n", " / ") + "»");
                    }
                }

                for (int i = 0; i < map.TownMarkerCount; i++)
                {
                    Rect label = map.TownLabelLocalRect(i);
                    if (label.width > 0f)
                    {
                        checkedLabels++;
                        AssertClear(map, label, at + ", rótulo del pueblo «" + map.TownLabelText(i) + "»");
                    }

                    Rect vignette = map.TownVignetteLocalRect(i);
                    if (vignette.width > 0f)
                    {
                        AssertClear(map, vignette, at + ", viñeta del pueblo «" + map.TownLabelText(i) + "»");
                    }
                }

                // «Faltan N» sigue en el rótulo de la parada siguiente (salvo hacia la naciente): esquivar la canoa no lo calla.
                if (play <= 690 && map.StopRemainingInLabel && !map.StopRemainingShown)
                {
                    // Y se ve: el rótulo con «Faltan N», entero dentro del Mapa al abrirlo.
                    Rect view = StopCardTests.CanvasRect(map.ViewportRect, map.Canvas);
                    Rect label = StopCardTests.CanvasRect(map.StopLabelOf(map.RemainingStopN())!.rectTransform, map.Canvas);
                    for (int k = 0; k < map.SilhouetteCount; k++)
                    {
                        UnityEngine.UI.Image plate = map.SilhouettePlate(k);
                        if (plate.enabled && plate.gameObject.activeInHierarchy)
                        {
                            Assert.That(label.Overlaps(StopCardTests.CanvasRect(plate.rectTransform, map.Canvas)), Is.False,
                                at + ": el rótulo con «Faltan N» pisa la placa «" + map.SilhouetteText(k) + "»");
                        }
                    }

                    if (!(view.Contains(label.min) && view.Contains(label.max - new Vector2(0.01f, 0.01f))))
                    {
                        Rect cuia = StopCardTests.CanvasRect(map.StopLabelOf(map.RemainingStopN())!.transform.parent.parent as RectTransform ?? map.ViewportRect, map.Canvas);
                        unseen.Add(play + " (parada " + map.RemainingStopN() + ", rótulo " + label + ", vista " + view + ", mini cuia " + cuia + ")");
                    }
                }

                if (play <= 690 && map.RemainingLabelOffView)
                {
                    Assert.That(map.StopRemainingShown, Is.True, at + ": con la parada siguiente fuera del encuadre, la placa de respaldo");
                    offView.Add(play + " (parada " + map.RemainingStopN() + ")");
                }
                else if (play <= 690 && (!map.StopRemainingInLabel || map.StopRemainingShown))
                {
                    int next = map.RemainingStopN();
                    int idx = map.StopMarkerIndexOf(next);
                    TMPro.TextMeshProUGUI? l = map.StopLabelOf(next);
                    missing.Add(play + " (parada " + next + ", abanico " + (idx >= 0 && map.StopMarkerFanned(idx)) + ", texto «"
                        + (l != null ? l.text.Replace("\n", " / ") : "-") + "»)");
                }
            }

            Debug.Log("AF_MAP d069b: con la parada siguiente fuera del encuadre de apertura (la placa de respaldo), " + offView.Count + " de "
                + ((690 - 2) / 5 + 1) + " aperturas: " + string.Join("; ", offView));
            Assert.That(offView.Count, Is.LessThanOrEqualTo(8), "la placa de respaldo es la excepción: " + string.Join("; ", offView));
            Assert.That(unseen, Is.Empty, "el rótulo con «Faltan N» queda fuera del Mapa al abrirlo jugando el " + string.Join("; ", unseen));
            Assert.That(missing, Is.Empty, "«Faltan N» fuera del rótulo de la parada siguiente (con la placa) jugando el " + string.Join("; ", missing));

            Assert.That(checkedLabels, Is.GreaterThan(200), "se miraron rótulos de verdad");
            map.Close();
        }

        [UnityTest]
        public IEnumerator TheCasesOfThePhone_Coari66_Anavilhanas56_Amacayacu96_AreReadable()
        {
            yield return MapMarkersTests.OpenApp(67); // max 66: «Coa…» bajo la canoa en el teléfono
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            int coari = -1;
            for (int i = 0; i < map.TownMarkerCount; i++)
            {
                if (map.TownLabelText(i) == "Coari")
                {
                    coari = i;
                }
            }

            Assert.That(coari, Is.GreaterThanOrEqualTo(0), "premisa: Coari es un pueblo de este Mapa");
            Rect coariLabel = map.TownLabelLocalRect(coari);
            if (coariLabel.width > 0f)
            {
                AssertClear(map, coariLabel, "Coari con max 66");
            }

            foreach (int play in new[] { 56, 96 })
            {
                OpenLobby(boot, new MemoryProgressStore(play, play - 1, play - 1), play);
                Canvas.ForceUpdateCanvases();
                int next = map.RemainingStopN();
                Rect r = map.StopLabelLocalRect(next);
                Assert.That(r.width, Is.GreaterThan(0f), "jugando el " + play + ": el rótulo de la parada siguiente sale");
                StringAssert.Contains("faltan", map.StopLabelText(next));
                AssertClear(map, r, "jugando el " + play);
            }

            map.Close();
        }

        [UnityTest]
        public IEnumerator After70_FaltanGoesInTheFannedNextStopLabel_NoPlate_AndThePrizePillClearsAndes()
        {
            yield return MapMarkersTests.OpenApp(71);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Canvas.ForceUpdateCanvases();
            int next = map.RemainingStopN();
            Assert.That(next, Is.EqualTo(8));
            Assert.That(map.StopRemainingInLabel, Is.True, "observación 4: en el rótulo de la parada siguiente, aunque vaya en abanico");
            Assert.That(map.StopRemainingShown, Is.False, "sin la placa «Faltan 10 niveles para Leticia»");
            StringAssert.Contains("faltan 10 niveles", map.StopLabelText(next));
            Rect view = StopCardTests.CanvasRect(map.ViewportRect, map.Canvas);
            Rect leticia = StopCardTests.CanvasRect(map.StopLabelOf(next)!.rectTransform, map.Canvas);
            Assert.That(view.Contains(leticia.min) && view.Contains(leticia.max - new Vector2(0.01f, 0.01f)), Is.True,
                "el encuadre enseña la parada siguiente con su rótulo (abre el mundo 4): " + leticia + " en " + view);

            // Sin los pueblos, igual.
            map.Close();
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapTowns = false };
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Canvas.ForceUpdateCanvases();
            Assert.That(map.StopRemainingInLabel, Is.True);
            Assert.That(map.StopRemainingShown, Is.False);
            map.Close();
            GameBootstrap.MapLiveConfigOverride = null;

            // La píldora del Premio del mundo nunca pisa «Andes ↑» (en la captura de M2-4b, con el 56, se leía «PAndes↑l mundo»).
            for (int play = 42; play <= 300; play += 7)
            {
                OpenLobby(boot, new MemoryProgressStore(play, play - 1, play - 1), play);
                map.Tick(PlayModeHelpers.FrameMs); // la píldora se recoloca contra el visor en cada Tick
                Canvas.ForceUpdateCanvases();
                RectTransform? pill = map.PrizePill;
                TMPro.TextMeshProUGUI? andes = map.AndesLabel;
                if (pill == null || andes == null || !andes.gameObject.activeInHierarchy || andes.text.Length == 0)
                {
                    continue;
                }

                Rect p = StopCardTests.CanvasRect(pill, map.Canvas);
                Rect a = StopCardTests.CanvasRect(andes.rectTransform, map.Canvas);
                Assert.That(p.Overlaps(a), Is.False, "jugando el " + play + ": «" + map.PrizeText + "» pisa «Andes ↑»");
            }

            map.Close();
        }

        internal static void OpenLobby(GameBootstrap boot, IMapProgress progress, int playLevel)
        {
            RiverMapScreen map = boot.RiverMapView!;
            map.Close();
            map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, progress, playLevel, true);
            Canvas.ForceUpdateCanvases();
        }

        private static void AssertClear(RiverMapScreen map, Rect r, string what)
        {
            for (int k = 0; k < map.CrewLabelBoxCount; k++)
            {
                Rect box = map.CrewLabelBox(k);
                Assert.That(r.Overlaps(box), Is.False, what + " pisa la canoa con su tripulación (" + (k == 0 ? "canoa" : "guía") + "): " + r + " / " + box);
            }
        }
    }

    /// <summary>
    /// Capturas de los arreglos del añadido de D-069 del 2026-11-08 a 360 dp: Coari con max 66, Anavilhanas jugando el 56,
    /// Amacayacu jugando el 96 y Leticia tras el 70, en <c>Logs/snapshots/es-419/d069b_*.png</c>. Explícito: necesita gráficos.
    /// </summary>
    [Explicit("Capturas del Mapa v2 (añadido de D-069 del 2026-11-08); necesitan dispositivo gráfico.")]
    public sealed class MapJourneyPaceSnapshotTests
    {
        private static readonly System.DateTime T0 = new System.DateTime(2026, 11, 8, 12, 0, 0, System.DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureTheLabelsClearOfTheCanoe_At360dp()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedPxPerDp = 3f;
            foreach (int play in new[] { 56, 67, 71, 96 })
            {
                Profile profile = AF.Economy.Wallet.NewProfile(new ManualClock(T0));
                profile.Progress.NextLevel = play;
                profile.Progress.MaxLevel = play - 1;
                profile.Progress.MapSeenLevel = play - 1;
                profile.Wallet.Seeds = 1250;
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
                GameBootstrap boot = GameBootstrap.Current!;
                SnapshotKit.AttachLobby(boot);
                yield return null;
                Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
                RiverMapScreen map = boot.RiverMapView!;
                SnapshotKit.AttachOverlay(map.Canvas);
                yield return SnapshotKit.Wait(1.0f);
                map.HoldCapiPose(CapiGesture.Rest, 0f, CapiEmotion.None);
                yield return SnapshotKit.Wait(0.2f);
                SnapshotKit.Save(dir, "d069b_rotulos_jugando_" + play.ToString("000") + "_360dp");
                Debug.Log("AF_MAP d069b " + play + ": parada siguiente «" + map.StopLabelText(map.RemainingStopN()).Replace("\n", " / ")
                    + "», placa " + map.StopRemainingShown + ", premio debajo " + map.PrizePillBelow);
                boot.CloseRiverMap();
            }
        }
    }
}
