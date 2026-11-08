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
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El Mapa del río v2, bloque M2-3 «Capi al remo» (D-069; <c>docs/dev/propuesta-mapa-rio-v2.md</c> §5, §7.2 y §7.3), en la escena
    /// <c>Game</c> real:
    /// <list type="bullet">
    /// <item>la canoa de 60 × 44 dp con el rig, su carga (una cuia por mundo completado) y el atlas que se suelta al cerrar;</item>
    /// <item>al ponerse al día por Manaos → Tefé, Capi saluda a los pueblos (que encienden su luz), sale la emoción del viaje (! en la
    /// boca del Purus) y, al llegar, el remo en alto, amarra la muestra y la cámara se acerca y vuelve;</item>
    /// <item>el viaje de «Ver mapa» al Encuentro (D-053): la pata de visera y el acercamiento en la anticipación, la cámara que sigue a
    /// la canoa en el tercio de abajo, «Jugar» que entra al llegar sin esperar a los gestos y la luz que se funde al mundo 3;</item>
    /// <item>Reducir movimiento: el gesto y la emoción quietos 1,2 s y sin cámara;</item>
    /// <item>el pie del Mapa v2 en una fila con «Faltan N» en el Mapa (desde M2-4, en el rótulo de la parada siguiente: respuesta del
    /// dueño a la pregunta 2), y el respaldo de la 1.0 con <c>ui_map_v2</c> apagada.</item>
    /// </list>
    /// </summary>
    public sealed class MapCapiPlayTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 7, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            RiverMapScreen.ResetCapiSession();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CatchingUp_WavesAtTheTowns_ThenArrives_RaisesTheOar_TiesTheSample_AndTheCameraZooms()
        {
            yield return MapMarkersTests.OpenApp(71, 60); // max 70: de Anavilhanas (60) a Tefé (70), por Manacapuru, el Purus, Codajás y Coari
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.LiveActive, Is.True);
            Assert.That(map.CapiActive, Is.True, "el rig de Capi en la canoa");
            Assert.That(map.CapiArt.Built, Is.True);
            Assert.That(map.Canoe.rectTransform.sizeDelta, Is.EqualTo(new Vector2(60f, 44f)), "propuesta §5.1: 60 × 44 dp en la vista Mundo");
            Assert.That(map.Canoe.enabled, Is.False, "el sprite de la 1.0 deja paso al rig");
            Assert.That(map.CapiRig!.CargoShown, Is.EqualTo(2), "a la salida, los mundos 1 y 2 completados");
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(map.Capi.PlannedEmotion, Is.EqualTo(CapiEmotion.Bang), "la boca del Purus manda sobre ♪ y la muestra");
            Assert.That(CountEvents(map, CapiMoment.TownPass), Is.GreaterThanOrEqualTo(2), "los pueblos del tramo que se dibujan");
            Assert.That(CountEvents(map, CapiMoment.Mouth), Is.EqualTo(1));
            Assert.That(CountEvents(map, CapiMoment.Arrival), Is.EqualTo(1));

            bool waved = false;
            bool glowed = false;
            bool bang = false;
            bool paddled = false;
            for (int ms = 0; ms < 4000 && map.CatchingUp; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                waved |= map.Capi.Current == CapiGesture.Wave;
                paddled |= map.Capi.Current == CapiGesture.Paddle;
                glowed |= map.TownGlowing;
                bang |= map.CapiRig.ShownEmotion == CapiEmotion.Bang;
            }

            Assert.That(map.CatchingUp, Is.False, "llegó");
            Assert.That(paddled, Is.True, "rema en el viaje");
            Assert.That(waved, Is.True, "saluda al pasar un pueblo");
            Assert.That(glowed, Is.True, "el pueblo enciende una luz");
            Assert.That(bang, Is.True, "una emoción, sin texto, en la burbuja");
            Assert.That(map.PlayShown, Is.True, "«Jugar» no espera a los gestos");

            bool cheer = false;
            bool tie = false;
            float zoom = 1f;
            for (int ms = 0; ms < 2500; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                cheer |= map.Capi.Current == CapiGesture.CheerOar;
                tie |= map.Capi.Current == CapiGesture.Tie && map.CapiRig.SampleShown;
                zoom = Mathf.Max(zoom, map.CameraPhaseZoom); // la de §5.5, sin el primer plano del miquito (añadido de D-069 del 2026-11-08)
            }

            Assert.That(cheer, Is.True, "el remo en alto al llegar");
            Assert.That(tie, Is.True, "amarra la muestra");
            Assert.That(zoom, Is.GreaterThan(1.1f).And.LessThanOrEqualTo(RiverMapScreen.CamArrivalZoom + 0.001f), "§5.5: acercamiento de 1,15");
            Assert.That(map.CameraZoom, Is.EqualTo(1f), "y vuelta al encuadre");
            Assert.That(map.ContentRect.localScale, Is.EqualTo(Vector3.one));
            Assert.That(map.Capi.Current, Is.EqualTo(CapiGesture.Rest), "quieto al acabar");
            Assert.That(map.Capi.Emotion, Is.EqualTo(CapiEmotion.None), "una emoción por viaje, 1,2 s");
            Assert.That(OnDemandRendering.renderFrameInterval, Is.GreaterThan(1), "en reposo vuelve a los fps de menús");

            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(map.CapiArt.Built, Is.False, "el atlas se suelta al cerrar (§7.2)");
            Assert.That(map.Canoe.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator VerMapa_ToTheMeeting_LooksOut_ZoomsIn_FollowsTheCanoe_PlayAtArrival_AndTheLightFades()
        {
            yield return VictoryArrivalTests.Win(40);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 900);
            Assert.That(boot.View!.VictoryMapShown, Is.True);
            boot.View.VictoryMapButton.onClick.Invoke();
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.JourneyLocked, Is.True, "D-053");
            Assert.That(map.CapiActive, Is.True);
            Assert.That(map.Capi.Current, Is.EqualTo(CapiGesture.Lookout), "la pata de visera en la anticipación (§5.2)");
            Assert.That(CountEvents(map, CapiMoment.WaterChange), Is.EqualTo(1), "el Encuentro: otra agua");
            Assert.That(CountEvents(map, CapiMoment.NewWorld), Is.EqualTo(1), "y otro mundo");
            Assert.That(map.LightColor, Is.EqualTo(RiverMapScreen.WorldLight(2)), "la luz del mundo de la canoa al salir");

            float lead = 1f;
            for (int ms = 0; ms < RiverMapScreen.JourneyLeadMs; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                lead = Mathf.Max(lead, map.CameraZoom);
            }

            Assert.That(lead, Is.GreaterThan(1.08f).And.LessThanOrEqualTo(RiverMapScreen.CamLeadZoom + 0.001f), "§5.5: de 1 a 1,12 en la anticipación");
            Canvas.ForceUpdateCanvases();
            for (int ms = 0; ms < (int)RiverMapScreen.CamLeadOutMs + 200; ms += PlayModeHelpers.FrameMs)
            {
                AssertNoAllocWhileFollowing(map, "saliendo, a " + ms + " ms");
            }

            Assert.That(map.CameraZoom, Is.EqualTo(1f), "vuelve al salir");
            Assert.That(map.JourneyLocked, Is.True, "aún remando");
            Debug.Log("AF_MAP m23 follow: canoa " + map.CanoeShownInViewport + " vista " + map.ViewportSize + " offset " + map.ContentOffset + " contenido " + map.ContentSize + " mapa " + map.CanoeMapPoint);
            Assert.That(map.CanoeShownInViewport.y, Is.GreaterThan(map.ViewportSize.y * 0.5f), "la cámara deja la canoa en el tercio de abajo");

            bool fading = false;
            float readyAt = -1f;
            float arrivedAt = -1f;
            for (int ms = 0; ms < 6000 && readyAt < 0f; ms += PlayModeHelpers.FrameMs)
            {
                bool before = map.CatchingUp;
                AssertNoAllocWhileFollowing(map, "remando, a " + ms + " ms");
                if (before && !map.CatchingUp)
                {
                    arrivedAt = ms;
                }

                fading |= map.LightFading;
                if (!map.JourneyLocked)
                {
                    readyAt = ms;
                }
            }

            Assert.That(readyAt, Is.GreaterThanOrEqualTo(0f));
            Assert.That(readyAt - arrivedAt, Is.LessThanOrEqualTo(PlayModeHelpers.FrameMs), "«Jugar» entra al llegar: los gestos no lo retrasan (§5.5)");
            bool lean = false;
            for (int ms = 0; ms < 2000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                fading |= map.LightFading;
                lean |= map.Capi.Current == CapiGesture.LeanOver;
            }

            Assert.That(fading, Is.True, "la luz se funde al cambiar de mundo dentro del Mapa");
            Assert.That(map.LightColor, Is.EqualTo(RiverMapScreen.WorldLight(3)), "a la del mundo 3 en 600 ms");
            Assert.That(lean, Is.True, "se asoma por la borda en el Encuentro");
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator ATouchCompletesTheJourney_OnlyTheFinalArrival_AndTheLightReachesThePlayWorld()
        {
            yield return MapMarkersTests.OpenApp(41, 30); // de Flona Tapajós (30) al Encuentro (40), por Óbidos, Parintins e Itacoatiara
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(CountEvents(map, CapiMoment.TownPass), Is.GreaterThan(0));
            Assert.That(map.LightColor, Is.EqualTo(RiverMapScreen.WorldLight(2)));
            Assert.That(map.CapiRig!.CargoShown, Is.EqualTo(1), "a la salida, el mundo 1");
            Assert.That(map.CatchUpTouch(), Is.True, "un toque completa el viaje");
            bool wave = false;
            bool glow = false;
            bool cheer = false;
            bool tie = false;
            for (int ms = 0; ms < 2000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                wave |= map.Capi.Current == CapiGesture.Wave;
                glow |= map.TownGlowing;
                cheer |= map.Capi.Current == CapiGesture.CheerOar;
                tie |= map.Capi.Current == CapiGesture.Tie;
            }

            Assert.That(wave, Is.False, "los pueblos que quedaban, saltados");
            Assert.That(glow, Is.False);
            Assert.That(cheer && tie, Is.True, "lo de la llegada final, sí");
            Assert.That(map.CapiRig.CargoShown, Is.EqualTo(2), "la carga, la de la llegada: los mundos 1 y 2");
            Assert.That(map.LightColor, Is.EqualTo(RiverMapScreen.WorldLight(3)), "la luz acaba en la del mundo de «Jugar»");
            map.Close();

            // Al volver de segundo plano (02 §14), salta al final sin animar: ni gestos ni cámara, y la luz ya en su mundo.
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(41, 40, 30), 41);
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(map.ResumeFromBackground(), Is.True);
            map.Tick(PlayModeHelpers.FrameMs);
            Assert.That(map.Capi.Current, Is.EqualTo(CapiGesture.Rest));
            Assert.That(map.Capi.Emotion, Is.EqualTo(CapiEmotion.None));
            Assert.That(map.CameraZoom, Is.EqualTo(1f));
            Assert.That(map.LightFading, Is.False);
            Assert.That(map.LightColor, Is.EqualTo(RiverMapScreen.WorldLight(3)));
            map.Close();
        }

        [UnityTest]
        public IEnumerator ReduceMotion_GestureAndEmotionStillFor1200ms_NoCamera_TheLightStillFades()
        {
            yield return MapMarkersTests.OpenApp(41, 40);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.ProgressReduceMotion = true;
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(41, 40, 30), 41); // el tramo del Encuentro, sin viaje
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.Capi.Still, Is.True);
            Assert.That(map.Capi.Current, Is.EqualTo(CapiGesture.CheerOar), "la llegada, en su postura clave");
            Assert.That(map.Capi.Phase, Is.EqualTo(CapiDirector.KeyPhase(CapiGesture.CheerOar)));
            Assert.That(map.Capi.Emotion, Is.EqualTo(CapiEmotion.Bang));
            Assert.That(CapiDirector.BubbleScale(map.Capi.EmotionElapsedMs, true), Is.EqualTo(1f), "la burbuja quieta, sin rebote");
            Assert.That(map.LightFading, Is.True, "la luz, con su fundido (no es movimiento)");
            Vector3 paddle = map.CapiRig!.PaddleRect.localEulerAngles;
            map.Tick(600f);
            Assert.That(map.CapiRig.PaddleRect.localEulerAngles, Is.EqualTo(paddle), "quieto");
            Assert.That(map.CameraZoom, Is.EqualTo(1f), "sin cámara");
            map.Tick(500f);
            Assert.That(map.Capi.Current, Is.EqualTo(CapiGesture.CheerOar));
            Assert.That(map.Capi.Emotion, Is.EqualTo(CapiEmotion.Bang));
            map.Tick(150f);
            Assert.That(map.Capi.Current, Is.EqualTo(CapiGesture.Rest), "1,2 s y vuelve");
            Assert.That(map.Capi.Emotion, Is.EqualTo(CapiEmotion.None));
            Assert.That(map.LightColor, Is.EqualTo(RiverMapScreen.WorldLight(3)));
            map.ProgressReduceMotion = false;
            map.Close();
        }

        [UnityTest]
        public IEnumerator OpenWithoutJourney_WavesOncePerSession()
        {
            yield return MapMarkersTests.OpenApp(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.Capi.Current, Is.EqualTo(CapiGesture.Wave), "§5.2: al abrir el Mapa sin viaje, saluda");
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.Capi.Current, Is.EqualTo(CapiGesture.Rest), "una vez por sesión");
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator Footer_OneRow_RemainingInTheNextStopLabel_AndTheMapGrows()
        {
            yield return MapMarkersTests.OpenApp(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Rect viewport = StopCardTests.CanvasRect(map.ViewportRect, map.Canvas);
            Rect capsule = StopCardTests.CanvasRect(map.ViewSwitch.rectTransform, map.Canvas);
            Rect tab = StopCardTests.CanvasRect((RectTransform)map.SamplesTabButton!.transform, map.Canvas);
            Rect play = StopCardTests.CanvasRect((RectTransform)map.PlayButton.transform, map.Canvas);
            Assert.That(map.StopRemainingShown, Is.False, "sin la placa: «Faltan N» va en el rótulo de la parada siguiente (M2-4)");
            Assert.That(map.StopRemainingInLabel, Is.True);
            Rect remaining = StopCardTests.CanvasRect(map.StopLabelOf(map.RemainingStopN())!.rectTransform, map.Canvas);
            Assert.That(tab.yMin, Is.EqualTo(capsule.yMin).Within(0.5f), "el conmutador y el Muestrario, en la misma fila");
            Assert.That(tab.xMin, Is.GreaterThanOrEqualTo(capsule.xMax - 0.5f));
            Assert.That(tab.xMax, Is.LessThanOrEqualTo(map.CanvasWidth + 0.5f));
            Assert.That(Mathf.Min(tab.width, tab.height), Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp - 0.5f));
            Assert.That(capsule.yMax, Is.LessThanOrEqualTo(play.yMin + 0.5f));
            Assert.That(viewport.yMax, Is.LessThanOrEqualTo(capsule.yMin + 0.5f), "el Mapa llega a la fila");
            Assert.That(remaining.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 0.5f), "«Faltan N», en el rótulo, dentro de la zona del mapa");
            Assert.That(remaining.yMax, Is.LessThanOrEqualTo(viewport.yMax + 0.5f));
            Assert.That(remaining.xMax, Is.LessThanOrEqualTo(viewport.xMax + 0.5f));
            float share = viewport.height / map.CanvasHeight;
            Debug.Log("AF_MAP m23 pie: mapa " + viewport.height.ToString("0", CultureInfo.InvariantCulture) + " de "
                + map.CanvasHeight.ToString("0", CultureInfo.InvariantCulture) + " dp (" + (share * 100f).ToString("0.0", CultureInfo.InvariantCulture) + " %)");
            Assert.That(boot.CloseRiverMap(), Is.True);

            // Con ui_map_v2 apagada: el Mapa y el pie de la 1.0 (la pestaña del Muestrario en su fila, «Faltan…» junto al conmutador).
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapV2 = false };
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Rect oldViewport = StopCardTests.CanvasRect(map.ViewportRect, map.Canvas);
            Rect oldCapsule = StopCardTests.CanvasRect(map.ViewSwitch.rectTransform, map.Canvas);
            Rect oldRemaining = StopCardTests.CanvasRect(map.StopRemainingRect, map.Canvas);
            Rect oldTab = StopCardTests.CanvasRect((RectTransform)map.SamplesTabButton!.transform, map.Canvas);
            Assert.That(map.LiveActive, Is.False);
            Assert.That(map.CapiActive, Is.False, "sin rig");
            Assert.That(map.Canoe.enabled, Is.True, "la canoa de la 1.0");
            Assert.That(map.Canoe.rectTransform.sizeDelta, Is.EqualTo(new Vector2(ProceduralArt.MapCanoeWidthDp, ProceduralArt.MapCanoeHeightDp)));
            Assert.That(map.CapiArt.Built, Is.False);
            Assert.That(oldRemaining.yMin, Is.EqualTo(oldCapsule.yMin).Within(0.5f), "«Faltan…» en la fila del pie");
            Assert.That(oldTab.yMax, Is.LessThanOrEqualTo(oldCapsule.yMin + 0.5f), "la pestaña, en su fila de encima");
            Assert.That(viewport.height, Is.GreaterThan(oldViewport.height + 40f), "el Mapa v2 gana la fila del Muestrario");
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator Footer_NoOverlaps_At360x640And360x780_AlsoAt130Percent()
        {
            foreach (float scale in new[] { 1f, 1.3f })
            {
                foreach (Vector4 size in new[] { new Vector4(1080f, 1920f, 96f, 48f), new Vector4(1080f, 2340f, 96f, 48f) })
                {
                    GameScreen.SimulatedScreen = size;
                    LaunchOptions.FontScaleOverride = scale;
                    yield return MapMarkersTests.OpenApp(58);
                    GameBootstrap boot = GameBootstrap.Current!;
                    RiverMapScreen map = boot.RiverMapView!;
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    string where = size.x + " × " + size.y + " al " + Mathf.RoundToInt(scale * 100f) + " %";
                    Assert.That(map.LiveActive, Is.True, where);
                    var named = new System.Collections.Generic.List<(string Name, Rect Box)>
                    {
                        ("←", StopCardTests.CanvasRect((RectTransform)map.BackButton.transform, map.Canvas)),
                        ("título", StopCardTests.CanvasRect(map.TitleLabel.rectTransform, map.Canvas)),
                        ("mundo", StopCardTests.CanvasRect((RectTransform)map.ViewWorldButton.transform, map.Canvas)),
                        ("cuenca", StopCardTests.CanvasRect((RectTransform)map.ViewBasinButton.transform, map.Canvas)),
                        ("muestrario", StopCardTests.CanvasRect((RectTransform)map.SamplesTabButton!.transform, map.Canvas)),
                        ("jugar", StopCardTests.CanvasRect((RectTransform)map.PlayButton.transform, map.Canvas)),
                    };
                    for (int i = 0; i < named.Count; i++)
                    {
                        Rect r = named[i].Box;
                        Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.5f), where + ": " + named[i].Name);
                        Assert.That(r.xMax, Is.LessThanOrEqualTo(map.CanvasWidth + 0.5f), where + ": " + named[i].Name);
                        Assert.That(r.yMax, Is.LessThanOrEqualTo(map.CanvasHeight - map.SafeBottom + 0.5f), where + ": " + named[i].Name);
                        for (int j = i + 1; j < named.Count; j++)
                        {
                            Assert.That(Shrink(r).Overlaps(Shrink(named[j].Box)), Is.False, where + ": " + named[i].Name + " y " + named[j].Name);
                        }
                    }

                    foreach (UnityEngine.UI.Button b in new[] { map.ViewWorldButton, map.ViewBasinButton, map.SamplesTabButton!, map.PlayButton })
                    {
                        Rect r = StopCardTests.CanvasRect((RectTransform)b.transform, map.Canvas);
                        Assert.That(Mathf.Min(r.width, r.height), Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp - 0.5f), where + ": " + b.name + " ≥ 48 dp");
                    }

                    // «Faltan N», en el rótulo de la parada siguiente (M2-4; pregunta 2): sin la placa, con su texto dentro, en dos líneas.
                    Assert.That(map.StopRemainingShown, Is.False, where + ": sin la placa");
                    Assert.That(map.StopRemainingInLabel, Is.True, where);
                    TMPro.TextMeshProUGUI label = map.StopLabelOf(map.RemainingStopN())!;
                    label.ForceMeshUpdate();
                    Assert.That(label.isTextOverflowing, Is.False, where + ": el texto cabe");
                    Assert.That(label.textInfo.lineCount, Is.EqualTo(2), where);
                    boot.CloseRiverMap();
                    LaunchOptions.Reset();
                }
            }
        }

        // Los bordes de dos controles vecinos pueden tocarse (medio dp).
        private static Rect Shrink(Rect r)
        {
            return new Rect(r.x + 0.5f, r.y + 0.5f, Mathf.Max(0f, r.width - 1f), Mathf.Max(0f, r.height - 1f));
        }

        // Un fotograma del viaje: sin asignar memoria salvo cuando cambia el llenado (una boya o la llegada), con la cámara siguiendo.
        private static void AssertNoAllocWhileFollowing(RiverMapScreen map, string at)
        {
            float fill = map.FrontFraction;
            bool before = map.CatchingUp;
            int blocks = AllocBlocks(() => map.Tick(PlayModeHelpers.FrameMs)) + AllocBlocks(Canvas.ForceUpdateCanvases);
            bool changed = map.FrontFraction != fill || before != map.CatchingUp;
            Assert.That(changed || blocks == 0, Is.True, at + ", sin cambio del llenado: " + blocks + " bloques de GC.Alloc");
        }

        private static int AllocBlocks(Action action)
        {
            UnityEngine.Profiling.Recorder recorder = UnityEngine.Profiling.Recorder.Get("GC.Alloc");
            recorder.FilterToCurrentThread();
            recorder.enabled = false;
            recorder.enabled = true;
            action();
            recorder.enabled = false;
            int n = recorder.sampleBlockCount;
            recorder.CollectFromAllThreads();
            return n;
        }

        [UnityTest]
        public IEnumerator Basin_TheCanoeIs40dp_AndTheRigFollows()
        {
            yield return MapMarkersTests.OpenApp(58);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.SetView(MapView.Basin);
            Assert.That(map.Canoe.rectTransform.sizeDelta.x, Is.EqualTo(ProceduralArt.MapCanoeWidthDp).Within(0.01f));
            map.Tick(PlayModeHelpers.FrameMs);
            Assert.That(map.CapiRig!.Root.localScale.x, Is.EqualTo(ProceduralArt.MapCanoeWidthDp / ProceduralArt.CapiCanoeDesignWidthDp).Within(0.001f));
            map.SetView(MapView.World);
            map.Tick(PlayModeHelpers.FrameMs);
            Assert.That(map.CapiRig.Root.localScale.x, Is.EqualTo(1f));
            map.Close();
        }

        private static int CountEvents(RiverMapScreen map, CapiMoment moment)
        {
            int n = 0;
            for (int i = 0; i < map.CapiEventCount; i++)
            {
                n += map.CapiEventAt(i) == moment ? 1 : 0;
            }

            return n;
        }
    }

    /// <summary>
    /// Capturas de M2-3 (D-069): a 360 dp, jugando el 66 (Manaos → Tefé), la canoa en reposo, dos gestos (el remo en alto y la pata
    /// de visera), una emoción (! remando) y el pie nuevo, cada una con un recorte ×1 de la canoa. Los PNG quedan en
    /// <c>Logs/snapshots/es-419/m23_*.png</c>. Explícito: necesita gráficos (sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas del Mapa v2 (M2-3); necesitan dispositivo gráfico.")]
    public sealed class MapCapiSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 7, 15, 0, 0, DateTimeKind.Utc);

        private static readonly (string Name, CapiGesture Gesture, float Phase, CapiEmotion Emotion)[] Poses =
        {
            ("canoa", CapiGesture.Rest, 0f, CapiEmotion.None),
            ("gesto_remo_en_alto", CapiGesture.CheerOar, 0.5f, CapiEmotion.None),
            ("gesto_visera", CapiGesture.Lookout, 1f, CapiEmotion.None),
            ("gesto_amarra", CapiGesture.Tie, 0.45f, CapiEmotion.None),
            ("emocion_bang", CapiGesture.Paddle, 0.25f, CapiEmotion.Bang),
            ("emocion_muestra", CapiGesture.Rest, 0f, CapiEmotion.Sample),
        };

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureTheCanoe_TwoGestures_AnEmotion_AndTheFooter_At360dp()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedPxPerDp = 3f;
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 66;
            profile.Progress.MaxLevel = 65;
            profile.Progress.MapSeenLevel = 65;
            profile.Wallet.Seeds = 1250;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            yield return null;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachOverlay(map.Canvas);
            yield return SnapshotKit.Wait(0.8f); // acaba el saludo de la sesión
            foreach ((string name, CapiGesture gesture, float phase, CapiEmotion emotion) in Poses)
            {
                map.HoldCapiPose(gesture, phase, emotion);
                yield return SnapshotKit.Wait(0.2f);
                SnapshotKit.Save(dir, "m23_" + name + "_360dp");
                SaveCrop(dir, "m23_" + name + "_360dp_zoom", map);
            }

            map.ReleaseCapiPose();
            Debug.Log("AF_MAP m23: canoa " + map.Canoe.rectTransform.sizeDelta + ", carga " + map.CapiRig!.CargoShown + ", arte "
                + ((map.LiveArt.TextureBytes + map.TownArt.TextureBytes + map.CapiArt.TextureBytes) / 1024L) + " KiB (rig "
                + (map.CapiArt.TextureBytes / 1024L) + ")");
            boot.CloseRiverMap();
        }

        // Un recorte de 330 × 270 px alrededor de la canoa (a 3 px/dp: 110 × 90 dp).
        private static void SaveCrop(string dir, string name, RiverMapScreen map)
        {
            var full = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Assert.That(full.LoadImage(SnapshotKit.Capture()), Is.True);
            Camera cam = SnapshotKit.CaptureCamera!;
            Vector3 p = cam.WorldToScreenPoint(map.Canoe.rectTransform.position);
            float sx = full.width / (float)cam.pixelWidth;
            float sy = full.height / (float)cam.pixelHeight;
            const int w = 330;
            const int h = 270;
            int x0 = Mathf.Clamp(Mathf.RoundToInt((p.x * sx) - (w * 0.5f)), 0, full.width - w);
            int y0 = Mathf.Clamp(Mathf.RoundToInt((p.y * sy) - (h * 0.35f)), 0, full.height - h);
            var crop = new Texture2D(w, h, TextureFormat.RGB24, false);
            crop.SetPixels(full.GetPixels(x0, y0, w, h));
            crop.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), crop.EncodeToPNG());
            UnityEngine.Object.Destroy(full);
            UnityEngine.Object.Destroy(crop);
        }
    }
}
