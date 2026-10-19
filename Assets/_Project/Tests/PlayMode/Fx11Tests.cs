#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Tarea FX11: el agua llena del último tramo no se veía tras «ponerse al día». <c>BuildFront</c> montaba la cinta nueva del
    /// frente (y su orla) sin el espacio ni el ancho de la vista: solo los ajustaba <c>ProgressSpace</c>, al cambiar de vista, y
    /// solo para las cintas que ya había. Si «ponerse al día» llegaba a un tramo con más piezas que el de partida (en el 41, el de
    /// la parada 4: Tapajós y Amazonas), la cinta nueva se quedaba con el espacio por defecto de la Cuenca (328 dp por unidad) y se
    /// pintaba fuera de la vista; hasta la canoa solo se veía el vidrio. Aquí, desde la pausa del 41 (max 40, visto 30) y en el
    /// Mapa del fin de contenido del 300, al abrir, en cada fotograma del viaje y al acabar: cada cinta del frente y su orla llevan
    /// el espacio y el ancho de la vista, su malla no está vacía y el final del agua (donde está la canoa) cae dentro de la zona
    /// visible; también en la Cuenca y otra vez en el Mundo. Las capturas son las de <see cref="Fx10SnapshotTests"/> y
    /// <c>MapProgressSnapshotTests</c>.
    /// </summary>
    public sealed class Fx11Tests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ContentEndMapOverride = null;
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator PauseMap_At41_AfterCatchingUp_EveryFrontRibbonHasTheViewSpace_AndTheWaterReachesTheCanoe()
        {
            MapArtRule.ForceProvisional = true;
            yield return Fx10Tests.OpenFromPause41(40, 30);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.CatchingUp, Is.True, "«ponerse al día» desde el 30");
            (int open, int most) = CatchUpAndCheck(map, "pausa del 41");
            Assert.That(most, Is.GreaterThan(open),
                "pausa del 41: «ponerse al día» llega a un tramo con más piezas que el de partida (el caso del fallo)");
            Assert.That(map.FrontPieceCount, Is.GreaterThanOrEqualTo(2), "pausa del 41: el tramo de la parada 4, Tapajós y Amazonas");
            CheckTheViews(map, "pausa del 41");
        }

        [UnityTest]
        public IEnumerator ContentEndMap_After300_EveryFrontRibbonHasTheViewSpace_AndTheWaterReachesTheCanoe()
        {
            GameBootstrap.ContentEndMapOverride = true;
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            yield return Fx10Tests.OpenContentEnd();
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.ContentEnd));
            Assert.That(map.CatchingUp, Is.True, "el viaje del último tramo");
            CatchUpAndCheck(map, "fin de contenido");
            CheckTheViews(map, "fin de contenido");
        }

        // Al abrir, en cada fotograma y al acabar. Devuelve las piezas del frente al abrir y las más que tuvo.
        private static (int Open, int Most) CatchUpAndCheck(RiverMapScreen map, string at)
        {
            map.Tick(16f);
            AssertFront(map, at + ", al abrir", false);
            int open = map.FrontPieceCount;
            int most = open;
            for (int t = 0; t < 30000 && (map.CatchingUp || map.CatchUpFading); t += 16)
            {
                map.Tick(16f);
                AssertFront(map, at + ", a " + t + " ms", false);
                most = Mathf.Max(most, map.FrontPieceCount);
            }

            Assert.That(map.CatchingUp, Is.False, at + ": «ponerse al día» terminó");
            map.Tick(16f);
            AssertFront(map, at + ", al acabar", true);
            Debug.Log("FX11 " + at + ": " + open + " piezas del frente al abrir, " + most + " como mucho, " + map.FrontPieceCount + " al acabar");
            return (open, most);
        }

        private static void CheckTheViews(RiverMapScreen map, string at)
        {
            map.SetView(MapView.Basin);
            map.Tick(16f);
            AssertFront(map, at + ", Cuenca", true);
            map.SetView(MapView.World);
            map.Tick(16f);
            AssertFront(map, at + ", Mundo otra vez", true);
        }

        // Cada cinta activa del frente y su orla: el espacio y el ancho de la vista. Con «drawn», además, su malla no está vacía y
        // el final del agua de la última cae dentro de la zona visible (allí está la canoa).
        private static void AssertFront(RiverMapScreen map, string at, bool drawn)
        {
            int n = map.FrontPieceCount;
            if (drawn)
            {
                Assert.That(n, Is.GreaterThan(0), at + ": hay frente");
                Canvas.ForceUpdateCanvases();
            }

            for (int i = 0; i < n; i++)
            {
                RiverRibbonGraphic[] pair = { map.FrontRibbon(i), map.FrontCasingRibbon(i) };
                foreach (RiverRibbonGraphic ribbon in pair)
                {
                    string what = at + ": " + ribbon.name;
                    Assert.That(ribbon.gameObject.activeSelf, Is.True, what + " activa");
                    Assert.That(ribbon.Line, Is.Not.Null, what + " con línea");
                    Assert.That(ribbon.Space, Is.EqualTo(map.Space),
                        what + " con el espacio de la vista (" + ribbon.Space.DpPerUnit + " frente a " + map.Space.DpPerUnit + " dp por unidad)");
                    Assert.That(ribbon.WidthDp, Is.EqualTo(RiverMapScreen.RibbonWidthDp(map.View, ribbon.Line!.Class)).Within(1e-4f),
                        what + " con el ancho de la vista");
                    if (drawn)
                    {
                        Assert.That(ribbon.LastVertexCount, Is.GreaterThan(0), what + ": malla no vacía");
                    }
                }
            }

            if (!drawn)
            {
                return;
            }

            // SetFill guarda el intervalo ordenado: en un tramo que remonta, la canoa está en el s menor. El final del agua es el
            // extremo de la última cinta más cerca de la canoa.
            RiverRibbonGraphic last = map.FrontRibbon(n - 1);
            Vector2 a = InViewport(map, last, last.FillStart);
            Vector2 b = InViewport(map, last, last.FillEnd);
            Vector2 canoe = map.CanoeInViewport;
            Vector2 end = (a - canoe).sqrMagnitude <= (b - canoe).sqrMagnitude ? a : b;
            Vector2 view = map.ViewportSize;
            Assert.That(end.x >= -1f && end.y >= -1f && end.x <= view.x + 1f && end.y <= view.y + 1f, Is.True,
                at + ": el final del agua (" + last.name + ", s " + last.FillStart + "-" + last.FillEnd + ": " + a + " y " + b + ") en " + end
                + ", dentro de la zona visible " + view + " (canoa en " + canoe + ")");
        }

        // El punto en s de la cinta, en dp dentro de la zona visible (y hacia abajo), con el espacio de la propia cinta.
        private static Vector2 InViewport(RiverMapScreen map, RiverRibbonGraphic ribbon, float s)
        {
            PolylineCursor cursor = ribbon.Cursor!;
            int i = cursor.Locate(cursor.ArcOfS(s), out float t);
            Vector2 p = cursor.PointCount < 2 ? cursor.Point(0) : Vector2.Lerp(cursor.Point(i), cursor.Point(i + 1), t);
            Vector3 world = ribbon.rectTransform.TransformPoint(ribbon.Space.ToLocal(p));
            Vector3 inView = map.ViewportRect.InverseTransformPoint(world);
            Rect rect = map.ViewportRect.rect;
            return new Vector2(inView.x - rect.xMin, rect.yMax - inView.y);
        }
    }
}
