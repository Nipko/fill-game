#nullable enable
using System;
using System.Collections;
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
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El Mapa del río v2, bloque M2-1 «El río vivo» (D-069; <c>docs/dev/propuesta-mapa-rio-v2.md</c> §3, §7.2, §7.3 y §9.3), en la
    /// escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>abre a pantalla completa (sin placa ni marco, de la cabecera a la barra de madera y de borde a borde), con las capas del
    /// río vivo, y se cierra soltando su arte (§7.2: la memoria vuelve);</item>
    /// <item>las paradas se tocan como en la 1.0: las futuras son boyas, la siguiente lleva su mini cuia con el brillo, las alcanzadas
    /// su mini cuia llena; «Jugar» y el ← como siempre;</item>
    /// <item>con <c>ui_map_v2</c> apagada, el Mapa de la 1.0 con los mismos datos <c>/2</c> (P8 (a): el respaldo);</item>
    /// <item>Reducir movimiento (§7.3): la corriente, las nubes y el primer plano quietos; y el Mapa quieto no asigna memoria.</item>
    /// </list>
    /// </summary>
    public sealed class MapLivePlayTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 7, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator OpensFullScreen_WithTheLiveRiver_AndClosesReleasingItsArt()
        {
            int before = LiveTextures(); // las de pruebas anteriores que aún no se han recogido no cuentan
            yield return OpenAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.LiveActive, Is.True, "ui_map_v2 encendida por defecto (08 §9)");
            Assert.That(map.View, Is.EqualTo(MapView.World));
            Assert.That(map.ViewportSize.x, Is.EqualTo(map.CanvasWidth).Within(0.01f), "de borde a borde, sin la placa ni el marco (P2 (a))");
            Assert.That(map.ViewportCorners.gameObject.activeSelf, Is.False, "sin los rincones del marco");
            Rect header = CanvasRect(map.HeaderRect, map.Canvas);
            Rect viewport = CanvasRect(map.ViewportRect, map.Canvas);
            Assert.That(viewport.yMin, Is.EqualTo(header.yMax).Within(0.5f), "el Mapa empieza bajo la cabecera");
            Image footer = map.FooterPanel!;
            Assert.That(footer.gameObject.activeSelf, Is.True, "la barra de madera de abajo");
            Rect footerRect = CanvasRect(footer.rectTransform, map.Canvas);
            Assert.That(footerRect.yMin, Is.EqualTo(viewport.yMax).Within(0.5f), "el Mapa acaba en la barra de abajo");
            Rect play = CanvasRect((RectTransform)map.PlayButton.transform, map.Canvas);
            Assert.That(play.yMin, Is.GreaterThanOrEqualTo(viewport.yMax), "«Jugar», en la barra, fuera del Mapa");
            Assert.That(viewport.height / map.CanvasHeight, Is.GreaterThan(0.57f), "más Mapa a la vista que en la 1.0 (la zona de dentro del marco)");

            yield return null;
            Assert.That(map.Canopy!.gameObject.activeInHierarchy, Is.True);
            Assert.That(map.Canopy.LastQuadCount, Is.GreaterThan(30), "la selva alrededor del río");
            Assert.That(map.Canopy.LastQuadCount, Is.LessThanOrEqualTo(CanopyGraphic.MaxQuads));
            Assert.That(map.FlowSlotCount, Is.GreaterThan(0), "la corriente en el cauce");
            Assert.That(map.Glass(0).WidthDp, Is.EqualTo(RiverMapScreen.LiveWidthClass1Dp), "el cauce a 26 dp");
            Assert.That(map.Glass(0).ShapeWidthDp, Is.EqualTo(RiverMapScreen.ShapeWidthDp(MapView.World, 1, true)), "el trazo, con su forma [P]");
            Assert.That(map.VarzeaRibbons.Count, Is.EqualTo(boot.RiverMapData!.Lines.Count));
            Assert.That(map.BankRibbons[0].Part, Is.EqualTo(RibbonPart.Banks));
            Assert.That(map.Land.Live, Is.True);
            Assert.That(map.LightLayer!.color, Is.EqualTo(RiverMapScreen.WorldLight(3)), "la luz de la tarde de Manaos");
            Assert.That(map.LiveArt.Built, Is.True);
            Assert.That(map.LiveArt.TextureBytes, Is.LessThanOrEqualTo(6L * 1024L * 1024L), "propuesta §7.2: +6 MiB solo con el Mapa abierto");
            Assert.That(LiveTextures(), Is.GreaterThan(before));

            // La vista Cuenca: sin orillas, várzea ni corriente (a 1× no caben); la selva, sí.
            map.ViewBasinButton.onClick.Invoke();
            yield return null;
            Assert.That(map.BankRibbons[0].gameObject.activeInHierarchy, Is.False);
            Assert.That(map.Glass(0).WidthDp, Is.EqualTo(RiverMapScreen.RibbonWidthDp(MapView.Basin, 1)));

            Assert.That(boot.CloseRiverMap(), Is.True);
            yield return null;
            yield return null;
            Assert.That(map.LiveArt.Built, Is.False, "el arte se suelta al cerrar");
            Assert.That(LiveTextures(), Is.LessThanOrEqualTo(before), "ninguna textura del Mapa v2 queda viva (memoria.md: el Mapa deja sus iconos)");

            // Abrir otra vez lo vuelve a pintar.
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
            Assert.That(map.LiveArt.Built, Is.True);
            Assert.That(boot.CloseRiverMap(), Is.True);
        }

        [UnityTest]
        public IEnumerator Stops_AreTappedAsIn10_FutureOnesAreBuoys_TheNextGlows()
        {
            yield return OpenAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            yield return null;
            // max_level 57: alcanzadas 0-5; la siguiente, la 6 (Anavilhanas); la 7 (Tefé), futura.
            Assert.That(map.StopMarkerIsBuoy(5), Is.False, "las alcanzadas, con su mini cuia llena");
            Assert.That(map.StopMarkerIsBuoy(6), Is.False, "la siguiente, con su mini cuia");
            Assert.That(map.StopMarkerIsNext(6), Is.True, "y el brillo que late");
            Assert.That(map.StopMarkerIsBuoy(7), Is.True, "las futuras, boyas de vidrio (propuesta §3.1)");
            Assert.That(map.StopMarkerIsNext(7), Is.False);

            Assert.That(map.TapStop(7), Is.True, "una boya se toca como la mini cuia");
            yield return null;
            Assert.That(map.CardOpen, Is.True, "abre la tarjeta de la parada");
            Assert.That(map.CloseTopSheet(), Is.True);
            yield return null;
            Assert.That(map.TapStop(5), Is.True);
            yield return null;
            Assert.That(map.CardOpen, Is.True);
            Assert.That(map.CloseTopSheet(), Is.True);
            yield return null;

            map.PlayButton.onClick.Invoke();
            yield return null;
            Assert.That(map.Visible, Is.False, "«Jugar» abre el nivel, como en la 1.0");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
        }

        [UnityTest]
        public IEnumerator UiMapV2Off_FallsBackToTheMapOf10_WithTheSameData()
        {
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapV2 = false };
            yield return OpenAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            yield return null;
            Assert.That(boot.RiverMapData!.Banks.Count, Is.GreaterThan(0), "los datos son /2");
            Assert.That(map.LiveActive, Is.False, "ui_map_v2 apagada: el Mapa de la 1.0");
            Assert.That(map.ViewportCorners.gameObject.activeSelf, Is.True, "con su marco");
            Assert.That(map.ViewportSize.x, Is.EqualTo(map.CanvasWidth - (2f * (8f + RiverMapScreen.FramePadDp))).Within(0.01f));
            Assert.That(map.Glass(0).WidthDp, Is.EqualTo(RiverMapScreen.WorldWidthClass1Dp), "el cauce a 8 dp");
            Assert.That(map.Land.CanopyOnly, Is.True, "la tierra de un color");
            Assert.That(map.Land.Live, Is.False);
            Assert.That(map.LiveArt.Built, Is.False, "sin el arte del Mapa v2");
            Assert.That(map.FooterPanel == null || !map.FooterPanel.gameObject.activeSelf, Is.True);
            Assert.That(map.StopMarkerIsBuoy(7), Is.False, "las paradas futuras, con su mini cuia gris de la 1.0");
            Assert.That(map.TapStop(7), Is.True);
            yield return null;
            Assert.That(map.CardOpen, Is.True);
            map.CloseTopSheet();
            Assert.That(boot.CloseRiverMap(), Is.True);

            // Y vuelve al v2 al encenderla (otra apertura).
            GameBootstrap.MapLiveConfigOverride = null;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
            Assert.That(map.LiveActive, Is.True);
            Assert.That(map.ViewportCorners.gameObject.activeSelf, Is.False);
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator ReduceMotion_KeepsEverythingStill_AndTheStillMapDoesNotAllocate()
        {
            yield return OpenAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.ProgressReduceMotion = true;
            yield return null;
            Assert.That(map.FlowSlotCount, Is.GreaterThan(0));
            Vector2 chevron = map.Chevron(0)!.rectTransform.anchoredPosition;
            Vector2 cloud = map.Cloud(0)!.rectTransform.anchoredPosition;
            Vector2 leaves = map.Foreground(false)!.rectTransform.anchoredPosition;
            for (int i = 0; i < 30; i++)
            {
                map.Tick(16f);
            }

            Assert.That(map.Chevron(0)!.rectTransform.anchoredPosition, Is.EqualTo(chevron), "la corriente quieta (propuesta §7.3)");
            Assert.That(map.Cloud(0)!.rectTransform.anchoredPosition, Is.EqualTo(cloud), "las nubes quietas");
            map.Drag(new Vector2(-40f, 30f));
            Assert.That(map.Foreground(false)!.rectTransform.anchoredPosition, Is.EqualTo(leaves), "sin parallax");

            map.ProgressReduceMotion = false;
            for (int i = 0; i < 30; i++)
            {
                map.Tick(16f);
            }

            Assert.That(map.Cloud(0)!.rectTransform.anchoredPosition, Is.Not.EqualTo(cloud), "sin Reducir movimiento, las nubes derivan");
            map.Drag(new Vector2(-40f, 30f));
            Assert.That(map.Foreground(false)!.rectTransform.anchoredPosition, Is.Not.EqualTo(leaves), "y el primer plano hace parallax");

            map.Tick(16f);
            map.Tick(400f);
            Assert.That(() => map.Tick(16f), Is.Not.AllocatingGCMemory(), "el Mapa v2 quieto: la corriente y las nubes sin asignar memoria");
            boot.CloseRiverMap();
        }

        private static int LiveTextures()
        {
            int n = 0;
            foreach (Texture2D tex in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                n += tex != null && tex.name.StartsWith("map_live_", StringComparison.Ordinal) ? 1 : 0;
            }

            return n;
        }

        private static IEnumerator OpenAt(int playLevel)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = playLevel;
            profile.Progress.MaxLevel = playLevel - 1;
            profile.Progress.MapSeenLevel = playLevel - 1; // sin «ponerse al día»
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
        }

        // Caja de rt en dp del lienzo, con el origen arriba a la izquierda.
        private static Rect CanvasRect(RectTransform rt, Canvas canvas)
        {
            var root = (RectTransform)canvas.transform;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 a = root.InverseTransformPoint(corners[0]);
            Vector3 b = root.InverseTransformPoint(corners[2]);
            float x0 = a.x - root.rect.xMin;
            float x1 = b.x - root.rect.xMin;
            float y0 = root.rect.yMax - b.y;
            float y1 = root.rect.yMax - a.y;
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
    }
}
