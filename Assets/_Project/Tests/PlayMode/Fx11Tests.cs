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
    /// visible; también en la Cuenca y otra vez en el Mundo. Revisión: cada cinta del frente y su orla van sobre el vidrio de su
    /// línea (su espacio, su ancho y sus secciones a menos de medio ancho del eje del vidrio); con el tope del frente repartido
    /// entre las 5 piezas del tramo del 300, en Mundo saltaban puntos y se veían dos cauces. Las capturas son las de
    /// <see cref="Fx10SnapshotTests"/> y
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

        // Revisión de FX11: el frente ya no lleva el tope de 400 vértices de la propuesta del Mapa §11.4 (desviación anotada allí,
        // pendiente del dueño). Aquí se mide lo que pide en el tramo del 300 (5 piezas, el caso de la revisión) y se acota con
        // FrontMeasuredMaxVertices, y se mide la memoria del viaje: los fotogramas sin cambio del llenado no asignan (ni el Tick
        // ni el lienzo) y solo asignan los ≤ 10 en que cambia (uno por boya). Esos asignan lo mismo con el tope de 400 repartido
        // (medido: 27-38 bloques de GC.Alloc por boya con los dos topes), así que no los causa el tope; quedan anotados en §11.4.
        [UnityTest]
        public IEnumerator ContentEndMap_After300_TheFrontVertices_AndTheJourneyOnlyAllocatesAtTheBuoys()
        {
            GameBootstrap.ContentEndMapOverride = true;
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            yield return Fx10Tests.OpenContentEnd();
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.CatchingUp, Is.True, "el viaje del último tramo");
            map.Tick(16f);
            Canvas.ForceUpdateCanvases();
            int frames = 0;
            int changes = 0;
            int allocating = 0;
            int blocks = 0;
            float fill = map.FrontFraction;
            for (int t = 0; t < 30000 && (map.CatchingUp || map.CatchUpFading); t += 16)
            {
                int tickBlocks = AllocBlocks(() => map.Tick(16f));
                int canvasBlocks = AllocBlocks(Canvas.ForceUpdateCanvases);
                frames++;
                bool changed = map.FrontFraction != fill;
                fill = map.FrontFraction;
                changes += changed ? 1 : 0;
                if (tickBlocks + canvasBlocks > 0)
                {
                    allocating++;
                    blocks += tickBlocks + canvasBlocks;
                }

                Assert.That(changed || tickBlocks + canvasBlocks == 0, Is.True,
                    "a " + t + " ms, sin cambio del llenado: " + tickBlocks + " bloques de GC.Alloc en el Tick y " + canvasBlocks + " en el lienzo");
            }

            Debug.Log("FX11 viaje del 300: " + frames + " fotogramas, " + changes + " cambios del llenado, " + allocating
                + " fotogramas con asignaciones (" + blocks + " bloques de GC.Alloc)");
            Assert.That(map.CatchingUp, Is.False, "el viaje terminó");
            Assert.That(frames, Is.GreaterThan(RiverMapScreen.BuoysPerLeg), "fotogramas del viaje");
            Assert.That(changes, Is.InRange(1, RiverMapScreen.BuoysPerLeg), "el llenado cambia una vez por boya como mucho");
            map.Tick(16f);
            CheckFrontVertices(map, MapView.World, "Mundo");
            map.SetView(MapView.Basin);
            map.Tick(16f);
            CheckFrontVertices(map, MapView.Basin, "Cuenca");
        }

        // Bloques de GC.Alloc del hilo principal durante «action» (como Is.Not.AllocatingGCMemory, pero contándolos).
        private static int AllocBlocks(System.Action action)
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

        // Lo que pide el frente entero (agua y orla) del tramo del 300, a la vista: se registra la cifra y se acota por
        // FrontMeasuredMaxVertices (la cifra medida con margen, anotada en la propuesta §11.4).
        private static void CheckFrontVertices(RiverMapScreen map, MapView view, string at)
        {
            Assert.That(map.View, Is.EqualTo(view));
            Canvas.ForceUpdateCanvases();
            int n = map.FrontPieceCount;
            Assert.That(n, Is.EqualTo(5), at + ": el tramo del 300, 5 piezas");
            int water = 0;
            int casing = 0;
            int most = 0;
            for (int i = 0; i < n; i++)
            {
                water += map.FrontRibbon(i).LastVertexCount;
                casing += map.FrontCasingRibbon(i).LastVertexCount;
                most = Mathf.Max(most, Mathf.Max(map.FrontRibbon(i).LastVertexCount, map.FrontCasingRibbon(i).LastVertexCount));
            }

            Debug.Log("FX11 frente del 300 en " + at + ": " + water + " vértices de agua y " + casing + " de orla (" + (water + casing)
                + "); la pieza más cara, " + most);
            Assert.That(water + casing, Is.InRange(1, FrontMeasuredMaxVertices),
                at + ": el frente del 300, " + water + " de agua y " + casing + " de orla");
        }

        // El frente del 300 (agua y orla) en Mundo, medido el 2026-10-19: 1.028 vértices de agua y 1.506 de orla (2.534; la pieza
        // más cara, la orla del Amazonas, 504); en Cuenca, 724. Tope con margen; anotado en la propuesta del Mapa §11.4. Con la primera
        // versión de FX12 (trazo curvo en Mundo, codos de hasta 25° y 0,75 dp), 2.886. Desde la revisión de FX12 (codos de ≤ 10° y
        // flecha de ≤ 0,3 dp, que es lo que deja de verse como esquina), 5.764 en Mundo (2.326 de agua y 3.438 de orla; la pieza más
        // cara, 1.314): no cabe en 3.000 con ningún reparto medido (el más barato con esa calidad, 4.088, aparta la curva de la
        // polilínea hasta 6 dp en la canoa). Tope provisional [P] de 6.500, pendiente del dueño (anotado en la propuesta §11.4):
        // el frente solo se rehace una vez por boya (≤ 10 por apertura).
        private const int FrontMeasuredMaxVertices = 6500;

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
                        AssertOnTheGlass(map, ribbon, what);
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

        // Revisión de FX11: el agua y su orla van sobre el vidrio de su línea, con su espacio y su ancho, y siguen su trazo: cada
        // sección cae cerca del eje del vidrio y cada punto de la línea dentro del intervalo queda a la misma distancia (más medio
        // ancho como mucho) de la cinta que del vidrio. El fin de contenido del 300 en Mundo, con el tope del frente repartido
        // entre las 5 piezas del tramo, saltaba puntos: las secciones seguían en la línea, pero las cuerdas entre ellas se
        // apartaban del vidrio de 5 a 10 dp (dos cauces a la vista).
        private static void AssertOnTheGlass(RiverMapScreen map, RiverRibbonGraphic ribbon, string what)
        {
            RiverRibbonGraphic glass = map.Glass(ribbon.Line!.Index);
            Assert.That(glass.gameObject.activeSelf && glass.Line == ribbon.Line, Is.True, what + ": su vidrio");
            Assert.That(glass.Space, Is.EqualTo(ribbon.Space), what + ": el espacio del vidrio");
            Assert.That(ribbon.WidthDp, Is.EqualTo(glass.WidthDp).Within(1e-4f), what + ": el ancho del vidrio");
            Assert.That(glass.LastSectionPointCount, Is.GreaterThan(1), what + ": el vidrio dibujado");
            Assert.That(ribbon.LastSectionPointCount, Is.GreaterThan(1), what + ": la cinta dibujada");
            float dp = ribbon.Space.DpPerUnit;

            // Las secciones de la cinta, sobre el vidrio (los extremos y los cambios de agua caen en la línea, no en una cuerda
            // del vidrio suavizado: hasta un ancho).
            float sections = 0f;
            for (int i = 0; i < ribbon.LastSectionPointCount; i++)
            {
                sections = Mathf.Max(sections, DistanceToSections(glass, ribbon.LastSectionPoint(i)) * dp);
            }

            Assert.That(sections, Is.LessThanOrEqualTo(glass.WidthDp),
                what + ": sus secciones, a " + sections + " dp del eje del vidrio como mucho (ancho " + glass.WidthDp + " dp)");

            // Los puntos de la línea dentro del intervalo: la cinta no se aparta de ellos más que el vidrio (que también suaviza).
            PolylineCursor cursor = ribbon.Cursor!;
            float a0 = ribbon.FillStart * cursor.Length;
            float a1 = ribbon.FillEnd * cursor.Length;
            float apart = 0f;
            int checkedPoints = 0;
            for (int v = 0; v < cursor.PointCount; v++)
            {
                float arc = cursor.ArcOfPoint(v);
                if (arc <= a0 || arc >= a1)
                {
                    continue;
                }

                Vector2 p = cursor.Point(v);
                apart = Mathf.Max(apart, (DistanceToSections(ribbon, p) - DistanceToSections(glass, p)) * dp);
                checkedPoints++;
            }

            Assert.That(apart, Is.LessThanOrEqualTo(glass.WidthDp * 0.5f),
                what + ": se aparta del vidrio " + apart + " dp como mucho en " + checkedPoints + " puntos de la línea (ancho " + glass.WidthDp + " dp)");
        }

        // Distancia (unidades del Mapa) de «p» a la polilínea de las secciones de la última malla de la cinta.
        private static float DistanceToSections(RiverRibbonGraphic ribbon, Vector2 p)
        {
            float best = float.MaxValue;
            int n = ribbon.LastSectionPointCount;
            for (int k = 0; k + 1 < n; k++)
            {
                Vector2 a = ribbon.LastSectionPoint(k);
                Vector2 ab = ribbon.LastSectionPoint(k + 1) - a;
                float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                best = Mathf.Min(best, (a + (ab * t) - p).magnitude);
            }

            return best;
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
