#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AF.Levels;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Primitivas del Mapa del río (D-013; 08 §2.2; propuesta del Mapa §3.4, §11.1 y §11.4; tarea M3.2): <see cref="PolylineCursor"/>
    /// en los extremos, a ≤ 0,5 dp de la polilínea exacta y monotónico, con las bocas del archivo real sobre su río y sin
    /// asignaciones; <see cref="RiverRibbonGraphic"/> con el ancho por clase, el vidrio y el agua (colores por tramo, las dos
    /// aguas a cada lado y el borde de vidrio), la trama de Patrones de fruta (con su forma en la andina), el frente a ≤ 400
    /// vértices, la trama con el aspecto que dibuja la cinta cuando el tope omite cambios de agua, la sección final que llega
    /// por su tramo, <see cref="RiverRibbonGraphic.SetFill"/> que no reconstruye ni asigna sin cambios, la malla sin
    /// asignaciones y el inglete limitado en un codo de 170°; y
    /// <see cref="LandGraphic"/> con un color por zona.
    /// </summary>
    public sealed class RibbonCursorTests
    {
        private const float Basin = MapSpace.BasinWidthDp;

        private static readonly MethodInfo RibbonPopulate = typeof(RiverRibbonGraphic).GetMethod("OnPopulateMesh",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;

        private static readonly MethodInfo LandPopulate = typeof(LandGraphic).GetMethod("OnPopulateMesh",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;

        private static float s_sink;
        private static bool s_flag;

        private readonly List<GameObject> _objects = new List<GameObject>();
        private string _json = string.Empty;
        private RiverMap _map = null!;

        [SetUp]
        public void SetUp()
        {
            _json = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Map/river_map_v1.json"));
            _map = RiverMapReader.Read(_json);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            _objects.Clear();
        }

        // ---------------------------------------------------------------------------------------------- cursor

        [Test]
        public void Cursor_Extremes_AndSinglePointLines()
        {
            RiverLine amazonas = _map.Lines[0];
            var cursor = new PolylineCursor(amazonas);
            int last = cursor.PointCount - 1;
            Assert.That(cursor.Length, Is.EqualTo(amazonas.Length));
            Assert.That(Vector2.Distance(cursor.PointAtS(0f), cursor.Point(0)), Is.LessThan(1e-6f));
            Assert.That(Vector2.Distance(cursor.PointAtS(1f), cursor.Point(last)), Is.LessThan(1e-6f));
            Assert.That(Vector2.Distance(cursor.PointAtS(-0.5f), cursor.Point(0)), Is.LessThan(1e-6f), "s se recorta a [0, 1]");
            Assert.That(Vector2.Distance(cursor.PointAtS(1.5f), cursor.Point(last)), Is.LessThan(1e-6f));
            Assert.That(Vector2.Distance(cursor.PointAtS(amazonas.SOfPoint(3)), cursor.Point(3)), Is.LessThan(1e-5f),
                "la s de un punto cae en el punto");
            Assert.That(cursor.Locate(0f, out float t0), Is.EqualTo(0));
            Assert.That(t0, Is.EqualTo(0f));
            Assert.That(cursor.Locate(cursor.Length, out float t1), Is.EqualTo(last - 1));
            Assert.That(t1, Is.EqualTo(1f));

            Vector2 first = cursor.Point(1) - cursor.Point(0);
            Assert.That(Vector2.Distance(cursor.TangentAtS(0f), first.normalized), Is.LessThan(1e-5f), "tangente unitaria, río abajo");
            Assert.That(cursor.TangentAtS(1f).magnitude, Is.EqualTo(1f).Within(1e-5f));

            // Una línea de un solo punto (solo la boca). Desde M5.1 el archivo real ya no tiene ninguna (el Atacuari sale de
            // HydroRIVERS): un documento mínimo con una.
            RiverMap tiny = RiverMapReader.Read(RiverMapReaderTests.Doc(lines: RiverMapReaderTests.Lines(RiverMapReaderTests.Main(),
                RiverMapReaderTests.Trib(pts: RiverMapReaderTests.EncodePoints(new ushort[] { 20000, 40000 })))));
            RiverLine mouthOnly = tiny.Line("negro")!;
            var single = new PolylineCursor(mouthOnly);
            Assert.That(single.Length, Is.EqualTo(0f));
            Assert.That(single.PointAtS(0.5f), Is.EqualTo(new Vector2(mouthOnly.X(0), mouthOnly.Y(0))), "solo la boca");
            Assert.That(single.TangentAtS(0.5f), Is.EqualTo(new Vector2(0f, 1f)), "sin tramos: río abajo");
        }

        [Test]
        public void Cursor_WithinHalfDp_AndMonotonic_OnEveryRealLine([Values(1f, 15f)] float zoom)
        {
            float dpPerUnit = Basin * zoom; // la Cuenca a 1× y la vista Mundo más cercana (15×; propuesta §3.2)
            double aspect = _map.Frame.Aspect;
            for (int k = 0; k < _map.Lines.Count; k++)
            {
                RiverLine line = _map.Lines[k];
                if (line.PointCount < 2)
                {
                    continue;
                }

                var cursor = new PolylineCursor(line);
                Reference reference = Reference.Of(RiverMapReaderTests.RawPoints(_json, line.Id), aspect);
                int lastSegment = -1;
                float lastT = -1f;
                float lastArc = -1f;
                double worst = 0.0;
                float worstAt = 0f;
                const int Steps = 2000;
                for (int i = 0; i <= Steps; i++)
                {
                    float s = i / (float)Steps;
                    Vector2 p = cursor.PointAtS(s);
                    reference.At(s, out double rx, out double ry);
                    double error = Math.Sqrt(((p.x - rx) * (p.x - rx)) + ((p.y - ry) * (p.y - ry))) * dpPerUnit;
                    if (error > worst)
                    {
                        worst = error;
                        worstAt = s;
                    }

                    int segment = cursor.Locate(cursor.ArcOfS(s), out float t);
                    float arc = cursor.ArcOfPoint(segment) + (t * (cursor.ArcOfPoint(segment + 1) - cursor.ArcOfPoint(segment)));
                    if (segment < lastSegment || (segment == lastSegment && t < lastT) || arc < lastArc)
                    {
                        Assert.Fail(line.Id + ": el cursor retrocede en s=" + s + " (tramo " + segment + ", t=" + t + ")");
                    }

                    lastSegment = segment;
                    lastT = t;
                    lastArc = arc;
                }

                Assert.That(worst, Is.LessThanOrEqualTo(0.5), line.Id + ": error máximo en s=" + worstAt + " a " + zoom + "×");
            }
        }

        [Test]
        public void Cursor_RealMouths_LandOnTheirHost()
        {
            for (int k = 0; k < _map.Lines.Count; k++)
            {
                RiverLine line = _map.Lines[k];
                if (line.Mouth == null)
                {
                    continue;
                }

                RiverMapPlace mouth = line.Mouth.Value;
                Vector2 host = new PolylineCursor(_map.Lines[mouth.Line]).PointAtS(mouth.S);
                Vector2 end = new PolylineCursor(line).Point(line.PointCount - 1);
                float gapDp = Vector2.Distance(host, end) * Basin;

                // ≤ 15 km = 2 dp en la Cuenca (geometry.py, TOLERANCE_KM); el Pacaya es la excepción pendiente del dueño (23,8 km
                // con el cauce de HydroRIVERS, M5.1; PENDING_MOUTH_KM: su boca está en el canal de Puinahua, que el cauce no dibuja).
                float limit = line.Id == "pacaya" ? 4.5f : 2f;
                Assert.That(gapDp, Is.LessThanOrEqualTo(limit), line.Id + " desemboca en " + _map.Lines[mouth.Line].Id + " s=" + mouth.S);
            }
        }

        [Test]
        public void Cursor_DoesNotAllocate()
        {
            var cursor = new PolylineCursor(_map.Lines[0]);
            var single = new PolylineCursor(_map.Line("atacuari")!);
            TestDelegate call = () =>
            {
                float acc = 0f;
                for (int i = 0; i <= 200; i++)
                {
                    float s = i / 200f;
                    acc += cursor.PointAtS(s).x + cursor.TangentAtS(s).y;
                    cursor.Sample(s, out Vector2 p, out Vector2 d);
                    acc += p.y + d.x + cursor.Locate(cursor.ArcOfS(s), out float t) + t;
                    cursor.DirectionsAt(i % cursor.PointCount, out Vector2 a, out Vector2 b);
                    acc += a.x + b.y + single.PointAtS(s).x;
                }

                s_sink = acc;
            };
            call(); // calentamiento
            Assert.That(call, Is.Not.AllocatingGCMemory());
            Assert.That(float.IsNaN(s_sink), Is.False);
        }

        // ---------------------------------------------------------------------------------------------- cinta

        [Test]
        public void Ribbon_WidthByClass_AndGlassWholeLine()
        {
            Assert.That(RiverRibbonGraphic.WidthForClass(1), Is.EqualTo(6f));
            Assert.That(RiverRibbonGraphic.WidthForClass(2), Is.EqualTo(3.5f));
            Assert.That(RiverRibbonGraphic.WidthForClass(3), Is.EqualTo(2f));

            RiverLine amazonas = _map.Lines[0];
            RiverRibbonGraphic glass = Ribbon(amazonas, RibbonLook.Glass);
            List<UIVertex> verts = Mesh(glass);

            // Una sección por punto (los extremos de [0, 1] son el primero y el último), tres bandas (borde, cuerpo y borde) de
            // dos vértices cada una.
            Assert.That(verts.Count, Is.EqualTo(6 * amazonas.PointCount));
            Assert.That(glass.LastVertexCount, Is.EqualTo(verts.Count));
            Color32 body = RiverRibbonGraphic.GlassBody;
            Color32 rim = Palette.GlassRim;
            foreach (UIVertex v in verts)
            {
                Assert.That(Same(v.color, body) || Same(v.color, rim), Is.True, "el vidrio no lleva tipos de agua");
            }

            // Primera sección (normal del primer tramo): de orilla a orilla, el ancho de la clase.
            Assert.That(Vector2.Distance(verts[0].position, verts[5].position), Is.EqualTo(6f).Within(1e-3f));
            Vector2 start = new MapSpace(Basin, Vector2.zero).ToLocal(amazonas.X(0), amazonas.Y(0));
            Assert.That(Vector2.Distance((verts[0].position + verts[5].position) * 0.5f, start), Is.LessThan(1e-3f));

            RiverRibbonGraphic tapajos = Ribbon(_map.Line("tapajos")!, RibbonLook.Glass);
            List<UIVertex> tv = Mesh(tapajos);
            Assert.That(Vector2.Distance(tv[0].position, tv[5].position), Is.EqualTo(3.5f).Within(1e-3f));
            Assert.That(tapajos.WidthDp, Is.EqualTo(3.5f));
        }

        [Test]
        public void Ribbon_Water_ColorsBySpan_AndTwoWatersSideBySide()
        {
            RiverLine amazonas = _map.Lines[0];
            List<UIVertex> verts = Mesh(Ribbon(amazonas, RibbonLook.Water));
            bool andean = false;
            bool white = false;
            bool black = false;
            bool clear = false;
            int rims = 0;
            foreach (UIVertex v in verts)
            {
                andean |= Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.Andean));
                white |= Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.White));
                black |= Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.Black));
                clear |= Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.Clear));
                Assert.That(Same(v.color, RiverRibbonGraphic.GlassBody), Is.False, "el cauce tiene el agua comprobada entera");
                bool isRim = Same(v.color, Palette.GlassRim);
                rims += isRim ? 1 : 0;
                Assert.That(isRim || Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.Andean))
                    || Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.White))
                    || Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.Black))
                    || Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.Clear)), Is.True,
                    "cada vértice es agua o el borde de vidrio: sin orilla oscura");
            }

            // El agua llena lleva el borde vidrio_borde (#EAF7F3; propuesta del Mapa §3.5), el mismo que el vidrio: las dos
            // bandas de fuera de cada sección (4 de sus 6 u 8 vértices).
            Assert.That(rims, Is.GreaterThanOrEqualTo(verts.Count / 2), "el borde de vidrio en cada sección");
            Assert.That(andean && white, Is.True, "cabecera andina y el resto blanca");
            Assert.That(black, Is.True, "el Encuentro: el Negro a un lado");
            Assert.That(clear, Is.True, "Santarém: el Tapajós a un lado");

            // En el Encuentro (left black, right white, mirando río abajo), el agua negra va a la izquierda de la cinta.
            RiverSplit encontro = amazonas.Splits[0];
            RiverRibbonGraphic split = Ribbon(amazonas, RibbonLook.Water);
            split.SetFill(encontro.S0 + 0.001f, encontro.S1 - 0.001f);
            var cursor = new PolylineCursor(amazonas);
            var space = new MapSpace(Basin, Vector2.zero);
            float mid = (encontro.S0 + encontro.S1) * 0.5f;
            Vector2 center = space.ToLocal(cursor.PointAtS(mid));
            Vector2 dir = MapSpace.DirectionToLocal(cursor.TangentAtS(mid));
            float blackSide = 0f;
            float whiteSide = 0f;
            foreach (UIVertex v in Mesh(split))
            {
                Vector2 p = (Vector2)v.position - center;
                float side = (dir.x * p.y) - (dir.y * p.x); // > 0: a la izquierda mirando río abajo
                if (Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.Black)))
                {
                    blackSide += side;
                }
                else if (Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.White)))
                {
                    whiteSide += side;
                }
            }

            Assert.That(blackSide, Is.GreaterThan(0f), "el Negro, a la izquierda");
            Assert.That(whiteSide, Is.LessThan(0f), "el Solimões, a la derecha");

            // Un río con el agua sin comprobar ([NV]) se dibuja en vidrio también en el aspecto de agua.
            foreach (UIVertex v in Mesh(Ribbon(_map.Line("pacaya")!, RibbonLook.Water)))
            {
                Assert.That(Same(v.color, RiverRibbonGraphic.GlassBody) || Same(v.color, Palette.GlassRim), Is.True);
            }
        }

        [Test]
        public void MapFill_CasingOutsideTheWater_AndCoreWithAGlint_AndGhostGlass()
        {
            // El agua llena del Mapa en dos partes (M3.4; D-003, vidrio y luz): la orla por fuera del ancho del agua (canto de
            // vidrio de 1,5 dp opaco y filete de tinta de 1 dp al 45 %) y el agua sin borde con su reflejo de destello.
            RiverLine tapajos = _map.Line("tapajos")!;
            RiverRibbonGraphic casing = Ribbon(tapajos, RibbonLook.Water);
            casing.SetPart(RibbonPart.Casing);
            List<UIVertex> cv = Mesh(casing);
            Assert.That(cv.Count, Is.EqualTo(6 * tapajos.PointCount), "tres bandas por sección, sin cambios de agua");
            Assert.That(Vector2.Distance(cv[0].position, cv[5].position), Is.EqualTo(3.5f + (2f * RiverRibbonGraphic.CasingDp)).Within(1e-3f),
                "la orla, 2,5 dp por fuera del agua a cada lado");
            Assert.That(Vector2.Distance(cv[2].position, cv[3].position), Is.EqualTo(3.5f + (2f * RiverRibbonGraphic.CasingRimDp)).Within(1e-3f),
                "el canto, 1,5 dp por cada lado");
            foreach (UIVertex v in cv)
            {
                Assert.That(Same(v.color, Palette.GlassRim) || Same(v.color, RiverRibbonGraphic.FilletColor), Is.True, "solo canto y filete");
            }

            Assert.That(RiverRibbonGraphic.FilletColor.a, Is.EqualTo(0.45f).Within(1e-6f));
            Assert.That(casing.LastMarkCount, Is.EqualTo(0), "la orla no lleva trama");

            RiverRibbonGraphic core = Ribbon(tapajos, RibbonLook.Water);
            core.SetPart(RibbonPart.Core);
            List<UIVertex> wv = Mesh(core);
            Assert.That(Vector2.Distance(wv[0].position, wv[1].position), Is.EqualTo(3.5f).Within(1e-3f), "el agua, el ancho del cauce");
            bool glint = false;
            foreach (UIVertex v in wv)
            {
                Assert.That(Same(v.color, Palette.GlassRim), Is.False, "el agua no lleva el borde: lo pone la orla");
                glint |= Same(v.color, RiverRibbonGraphic.ReflectionColor(RiverWater.Clear));
            }

            Assert.That(glint, Is.True, "el reflejo de destello sobre el agua clara");

            // El reflejo, de 1 dp (como mucho el 30 % del agua), desplazado hacia el borde de arriba.
            float width = Vector2.Distance(wv[2].position, wv[3].position);
            Assert.That(width, Is.EqualTo(Mathf.Min(RiverRibbonGraphic.ReflectionDp, 0.3f * 3.5f)).Within(1e-3f));
            Vector2 mid = (wv[0].position + wv[1].position) * 0.5f;
            Vector2 glintMid = (wv[2].position + wv[3].position) * 0.5f;
            Assert.That(glintMid.y, Is.GreaterThanOrEqualTo(mid.y - 1e-3f), "hacia arriba (o por el eje, si el río va en vertical)");

            // El vidrio fantasma: solo el cuerpo baja; el borde, no.
            RiverRibbonGraphic glass = Ribbon(tapajos, RibbonLook.Glass);
            glass.SetGlassBodyAlpha(0.4f);
            bool body = false;
            foreach (UIVertex v in Mesh(glass))
            {
                Color32 ghost = RiverRibbonGraphic.GlassBody;
                ghost.a = (byte)Mathf.RoundToInt(0.4f * 255f);
                body |= Same(v.color, ghost);
                Assert.That(Same(v.color, ghost) || Same(v.color, Palette.GlassRim), Is.True);
            }

            Assert.That(body, Is.True);
            Assert.That(RiverRibbonGraphic.GlassBody.a, Is.EqualTo(0.85f).Within(1e-6f), "el color base no cambia");
        }

        [Test]
        public void Ribbon_Patterns_AddATramaOnlyOnWater()
        {
            RiverLine amazonas = _map.Lines[0];
            RiverRibbonGraphic plain = Ribbon(amazonas, RibbonLook.Water);
            int without = Mesh(plain).Count;
            Assert.That(plain.LastMarkCount, Is.EqualTo(0));

            RiverRibbonGraphic patterned = Ribbon(amazonas, RibbonLook.Water);
            patterned.SetPatterns(true);
            List<UIVertex> verts = Mesh(patterned);
            Assert.That(patterned.LastMarkCount, Is.GreaterThan(100), "una marca cada 4 dp en ≈ 565 dp, dos en las dos aguas");
            Assert.That(verts.Count, Is.EqualTo(without + (4 * patterned.LastMarkCount)));
            bool dots = false;
            bool stripes = false;
            bool waves = false;
            bool dashes = false;
            foreach (UIVertex v in verts)
            {
                dots |= Same(v.color, RiverRibbonGraphic.MarkColor(RiverWater.White));
                stripes |= Same(v.color, RiverRibbonGraphic.MarkColor(RiverWater.Black));
                waves |= Same(v.color, RiverRibbonGraphic.MarkColor(RiverWater.Clear));
                dashes |= Same(v.color, RiverRibbonGraphic.MarkColor(RiverWater.Andean));
            }

            Assert.That(dots && stripes && waves && dashes, Is.True,
                "puntos en la blanca, rayas en la negra, ondas en la clara y guiones en la andina");

            // La andina tiene su propia trama [P], no los puntos de la blanca (07 §Accesibilidad: el paso de la andina a la
            // blanca no depende solo del color): guiones a lo largo del río frente a rombos con los cuatro lados iguales.
            float headwaters = amazonas.NextBoundaryAfter(0f);
            Assert.That(amazonas.WaterAt(0f), Is.EqualTo(RiverWater.Andean));
            Assert.That(amazonas.WaterAt(headwaters), Is.EqualTo(RiverWater.White), "el primer cambio: de la andina a la blanca");
            RiverRibbonGraphic andeanSpan = Ribbon(amazonas, RibbonLook.Water);
            andeanSpan.SetPatterns(true);
            andeanSpan.SetFill(0f, headwaters);
            AssertMarkShapes(andeanSpan, amazonas, RiverWater.Andean, true);
            RiverRibbonGraphic whiteSpan = Ribbon(amazonas, RibbonLook.Water);
            whiteSpan.SetPatterns(true);
            whiteSpan.SetFill(headwaters, headwaters + 0.05f);
            AssertMarkShapes(whiteSpan, amazonas, RiverWater.White, false);

            // La opacidad canónica del patrón (07 §3, regla 1): ui_pattern_opacity = 0,20 por defecto y 0,35 solo en Alto
            // contraste, que pasa quien configura la cinta.
            Assert.That(patterned.PatternOpacity, Is.EqualTo(Palette.PatternOpacityOn));
            Assert.That(RiverRibbonGraphic.PatternOpacityHighContrast, Is.EqualTo(0.35f));
            Assert.That(Same(RiverRibbonGraphic.MarkColor(RiverWater.White), RiverRibbonGraphic.MarkColor(RiverWater.White, 0.20f)), Is.True);
            Color32 subtle = RiverRibbonGraphic.MarkColor(RiverWater.White);
            Color32 strong = RiverRibbonGraphic.MarkColor(RiverWater.White, RiverRibbonGraphic.PatternOpacityHighContrast);
            Assert.That(Same(subtle, strong), Is.False);
            patterned.SetPatterns(true, RiverRibbonGraphic.PatternOpacityHighContrast);
            bool strongSeen = false;
            bool subtleSeen = false;
            foreach (UIVertex v in Mesh(patterned))
            {
                strongSeen |= Same(v.color, strong);
                subtleSeen |= Same(v.color, subtle);
            }

            Assert.That(strongSeen && !subtleSeen, Is.True, "en Alto contraste, la trama al 35 %");

            RiverRibbonGraphic glass = Ribbon(amazonas, RibbonLook.Glass);
            glass.SetPatterns(true);
            Mesh(glass);
            Assert.That(glass.LastMarkCount, Is.EqualTo(0), "el vidrio no lleva trama: se distingue por el borde");
        }

        [Test]
        public void Front_NeverPassesItsBudget_AndKeepsBothEnds()
        {
            RiverLine amazonas = _map.Lines[0];
            RiverRibbonGraphic front = Ribbon(amazonas, RibbonLook.Water);
            front.SetPatterns(true);
            front.SetVertexBudget(RiverRibbonGraphic.FrontMaxVertices);
            var cursor = new PolylineCursor(amazonas);
            var space = new MapSpace(Basin, Vector2.zero);
            float[][] intervals =
            {
                new[] { 0f, 1f }, new[] { 0.931756f, 0.855387f }, new[] { 0.70f, 0.73f }, new[] { 0.2f, 0.9f }, new[] { 0.5f, 0.55f },
            };

            foreach (float[] fill in intervals)
            {
                front.SetFill(fill[0], fill[1]);
                List<UIVertex> verts = Mesh(front);
                Assert.That(verts.Count, Is.LessThanOrEqualTo(RiverRibbonGraphic.FrontMaxVertices), "frente " + fill[0] + "-" + fill[1]);
                Assert.That(verts.Count, Is.GreaterThan(0));

                // Las dos puntas del intervalo están dibujadas aunque haya que saltar puntos intermedios.
                Vector2 a = space.ToLocal(cursor.PointAtS(Mathf.Min(fill[0], fill[1])));
                Vector2 b = space.ToLocal(cursor.PointAtS(Mathf.Max(fill[0], fill[1])));
                Assert.That(Nearest(verts, a), Is.LessThanOrEqualTo(3.01f), "punta de arriba");
                Assert.That(Nearest(verts, b), Is.LessThanOrEqualTo(3.01f), "punta de abajo");
            }

            // Un tramo de una décima, el frente de verdad, cabe con holgura y lleva su trama.
            front.SetFill(0.80f, 0.81f);
            Assert.That(Mesh(front).Count, Is.LessThan(RiverRibbonGraphic.FrontMaxVertices / 2));
            Assert.That(front.LastMarkCount, Is.GreaterThan(0));
        }

        [Test]
        public void SmallBudget_WithManyWaterChanges_KeepsBothEnds([Values(16, 30, 48, 60, 84, 100)] int budget)
        {
            // El cauce entero en agua: cinco cambios (andina a blanca y las dos franjas de dos aguas) son 12 secciones fijas, que
            // con secciones de hasta 8 vértices no caben en 96. Ningún punto intermedio cabe y, si tampoco caben todos los
            // cambios, la sección final se queda con su sitio.
            RiverLine amazonas = _map.Lines[0];
            RiverRibbonGraphic ribbon = Ribbon(amazonas, RibbonLook.Water);
            ribbon.SetVertexBudget(budget);
            List<UIVertex> verts = Mesh(ribbon);
            Assert.That(verts.Count, Is.LessThanOrEqualTo(budget));
            Assert.That(verts.Count, Is.GreaterThan(0));
            Assert.That(ribbon.LastWasDecimated, Is.True);
            var cursor = new PolylineCursor(amazonas);
            var space = new MapSpace(Basin, Vector2.zero);
            Assert.That(Nearest(verts, space.ToLocal(cursor.PointAtS(0f))), Is.LessThanOrEqualTo(3.01f), "punta de arriba");
            Assert.That(Nearest(verts, space.ToLocal(cursor.PointAtS(1f))), Is.LessThanOrEqualTo(3.01f), "punta de abajo");
        }

        [Test]
        public void Budget_TooSmallForTwoSections_LeavesTheMeshEmpty()
        {
            RiverRibbonGraphic ribbon = Ribbon(_map.Lines[0], RibbonLook.Water);
            ribbon.SetVertexBudget(8);
            Assert.That(Mesh(ribbon).Count, Is.EqualTo(0), "una sección sola no es una cinta");
        }

        [Test]
        public void Decimated_RibbonAndTrama_FollowTheChords()
        {
            // Con puntos saltados, la cinta une los que quedan con cuerdas: cada marca de la trama cae dentro de un triángulo
            // de la cinta (antes seguían la polilínea original y algunas quedaban fuera).
            RiverLine amazonas = _map.Lines[0];
            RiverRibbonGraphic ribbon = Ribbon(amazonas, RibbonLook.Water);
            ribbon.SetPatterns(true);
            ribbon.SetVertexBudget(700);
            var stream = new List<UIVertex>();
            using (var vh = new VertexHelper())
            {
                RibbonPopulate.Invoke(ribbon, new object[] { vh });
                vh.GetUIVertexStream(stream);
            }

            Assert.That(ribbon.LastWasDecimated, Is.True);
            int marks = ribbon.LastMarkCount;
            Assert.That(marks, Is.GreaterThan(20));
            int ribbonCount = stream.Count - (6 * marks); // dos triángulos por marca, al final
            float worst = 0f;
            for (int m = 0; m < marks; m++)
            {
                // Los cuatro vértices de la marca (b, b + 1, b + 2 y b + 3 en (b, b+1, b+2) y (b+2, b+3, b)) promedian su centro.
                int at = ribbonCount + (6 * m);
                Vector2 c = ((Vector2)stream[at].position + (Vector2)stream[at + 1].position + (Vector2)stream[at + 2].position
                    + (Vector2)stream[at + 4].position) * 0.25f;
                float best = float.MaxValue;
                for (int t = 0; t + 2 < ribbonCount && best > 0f; t += 3)
                {
                    best = Mathf.Min(best, TriangleDistance(c, stream[t].position, stream[t + 1].position, stream[t + 2].position));
                }

                worst = Mathf.Max(worst, best);
            }

            Assert.That(worst, Is.LessThanOrEqualTo(0.05f), "la marca más alejada de la cinta, en dp");
        }

        [Test]
        public void SetFill_OnlyRebuildsOnChange_AndDoesNotAllocate()
        {
            RiverRibbonGraphic ribbon = Ribbon(_map.Lines[0], RibbonLook.Water);
            Assert.That(ribbon.SetFill(0.2f, 0.3f), Is.True);
            Assert.That(ribbon.SetFill(0.2f, 0.3f), Is.False, "sin cambio, no reconstruye");
            Assert.That(ribbon.SetFill(0.3f, 0.2f), Is.False, "remontar es el mismo intervalo");
            Assert.That(ribbon.FillStart, Is.EqualTo(0.2f));
            Assert.That(ribbon.FillEnd, Is.EqualTo(0.3f));
            Assert.That(ribbon.SetFill(-1f, 0.3f), Is.True);
            Assert.That(ribbon.FillStart, Is.EqualTo(0f), "se recorta a [0, 1]");

            TestDelegate same = () => s_flag = ribbon.SetFill(0f, 0.3f);
            same();
            Assert.That(same, Is.Not.AllocatingGCMemory());
            Assert.That(s_flag, Is.False, "el mismo intervalo: no reconstruye");
        }

        [Test]
        public void Ribbon_Mesh_DoesNotAllocate_AfterTheFirstBuild()
        {
            RiverRibbonGraphic front = Ribbon(_map.Lines[0], RibbonLook.Water);
            front.SetPatterns(true);
            front.SetVertexBudget(RiverRibbonGraphic.FrontMaxVertices);
            var populate = (Action<VertexHelper>)Delegate.CreateDelegate(typeof(Action<VertexHelper>), front, RibbonPopulate);
            using (var vh = new VertexHelper())
            {
                float s = 0.70f;
                TestDelegate frame = () =>
                {
                    s += 0.0001f; // el frente avanza un poco en cada fotograma
                    front.SetFill(0.70f, s);
                    populate(vh);
                };
                front.SetFill(0f, 1f);
                populate(vh); // calentamiento: la malla más grande, para que las listas ya tengan su capacidad
                frame();
                Assert.That(frame, Is.Not.AllocatingGCMemory());
                Assert.That(vh.currentVertCount, Is.GreaterThan(0));
            }
        }

        [Test]
        public void Truncated_WaterChanges_TramaFollowsTheDrawnLook()
        {
            // Documento mínimo: el cauce tiene tres puntos y el segundo cae en s ≈ 0,436, así que [0,499, 0,9] no tiene puntos
            // interiores y cruza la franja de dos aguas (0,5-0,6). Con un tope de 20 vértices, el cambio a la franja no cabe: la
            // cinta va blanca hasta la punta, y la trama tiene que ir igual (antes tomaba el agua del archivo y pintaba la
            // franja, con la raya de la negra, sobre una cinta blanca).
            RiverLine main = RiverMapReader.Read(RiverMapReaderTests.Doc()).Lines[0];
            Assert.That(main.SOfPoint(1), Is.LessThan(0.499f));
            RiverRibbonGraphic ribbon = Ribbon(main, RibbonLook.Water);
            ribbon.SetPatterns(true);
            ribbon.SetVertexBudget(20);
            ribbon.SetFill(0.499f, 0.9f);
            List<UIVertex> verts = Mesh(ribbon);
            Assert.That(verts.Count, Is.LessThanOrEqualTo(20));
            Assert.That(ribbon.LastWasDecimated, Is.False, "sin puntos interiores no se salta ninguno");
            int marks = ribbon.LastMarkCount;
            Assert.That(marks, Is.GreaterThan(0));
            Color32 whiteMark = RiverRibbonGraphic.MarkColor(RiverWater.White);
            for (int i = 0; i < verts.Count; i++)
            {
                UIVertex v = verts[i];
                Assert.That(Same(v.color, RiverRibbonGraphic.WaterColor(RiverWater.Black)), Is.False, "la cinta no llega a la franja");
                if (i >= verts.Count - (4 * marks))
                {
                    Assert.That(Same(v.color, whiteMark), Is.True, "la trama, con el agua que dibuja la cinta (vértice " + i + ")");
                }
            }
        }

        [Test]
        public void Ribbon_EndOnALinePoint_FollowsTheArrivingSegment()
        {
            // Si la punta del intervalo cae justo en un punto de la línea (SetFill(x, SOfPoint(k))), la sección final es
            // perpendicular al tramo por el que llega la cinta (k − 1), no al que sale de ese punto; la inicial, en el mismo
            // punto, al tramo por el que sale (k). Se busca, desde el punto 10, uno en el que s · largo dé justo su longitud
            // acumulada (la igualdad exacta es el caso límite) y en el que la línea gire.
            RiverLine amazonas = _map.Lines[0];
            var cursor = new PolylineCursor(amazonas);
            int k = -1;
            for (int i = 10; i < amazonas.PointCount - 1 && k < 0; i++)
            {
                bool exact = cursor.ArcOfS(amazonas.SOfPoint(i)) == cursor.ArcOfPoint(i);
                bool segments = cursor.ArcOfPoint(i - 1) < cursor.ArcOfPoint(i) && cursor.ArcOfPoint(i) < cursor.ArcOfPoint(i + 1);
                bool turns = Vector2.Angle(cursor.SegmentDirection(i - 1), cursor.SegmentDirection(i)) > 5f;
                if (exact && segments && turns)
                {
                    k = i;
                }
            }

            Assert.That(k, Is.GreaterThanOrEqualTo(10), "un punto del cauce con la s exacta y un giro de más de 5°");
            Vector2 arriving = MapSpace.DirectionToLocal(cursor.SegmentDirection(k - 1));
            Vector2 leaving = MapSpace.DirectionToLocal(cursor.SegmentDirection(k));

            RiverRibbonGraphic upTo = Ribbon(amazonas, RibbonLook.Glass);
            upTo.SetFill(0f, amazonas.SOfPoint(k));
            List<UIVertex> verts = Mesh(upTo);
            Assert.That(verts.Count, Is.EqualTo(6 * (k + 1)), "una sección por punto, la última en el punto " + k);
            Vector2 last = verts[verts.Count - 1].position - verts[verts.Count - 6].position;
            Assert.That(last.magnitude, Is.EqualTo(RiverRibbonGraphic.WidthClass1Dp).Within(1e-3f));
            Assert.That(Vector2.Dot(last.normalized, arriving), Is.EqualTo(0f).Within(1e-3f), "perpendicular al tramo " + (k - 1));

            RiverRibbonGraphic fromK = Ribbon(amazonas, RibbonLook.Glass);
            fromK.SetFill(amazonas.SOfPoint(k), 1f);
            List<UIVertex> fv = Mesh(fromK);
            Vector2 first = fv[5].position - fv[0].position;
            Assert.That(first.magnitude, Is.EqualTo(RiverRibbonGraphic.WidthClass1Dp).Within(1e-3f));
            Assert.That(Vector2.Dot(first.normalized, leaving), Is.EqualTo(0f).Within(1e-3f), "perpendicular al tramo " + k);
        }

        [Test]
        public void Ribbon_Elbow170_MiterIsLimited()
        {
            // Un codo de 170° en unidades del marco (la misma escala en los dos ejes): A → B y de vuelta casi hacia A.
            double aspect = 33.0 * Math.Cos(5.0 * Math.PI / 180.0) / 25.0;
            double angle = 170.0 * Math.PI / 180.0;
            var bx = 0.5;
            var by = 0.5;
            ushort[] raw =
            {
                Quant(bx - 0.3), QuantY(by, aspect), Quant(bx), QuantY(by, aspect),
                Quant(bx + (0.3 * Math.Cos(angle))), QuantY(by + (0.3 * Math.Sin(angle)), aspect),
            };
            string doc = RiverMapReaderTests.Doc(lines: RiverMapReaderTests.Lines(
                RiverMapReaderTests.Main(pts: RiverMapReaderTests.EncodePoints(raw), splits: string.Empty), RiverMapReaderTests.Trib()));
            RiverLine elbow = RiverMapReader.Read(doc).Lines[0];
            var cursor = new PolylineCursor(elbow);
            Vector2 d0 = cursor.SegmentDirection(0);
            Vector2 d1 = cursor.SegmentDirection(1);
            Assert.That(Vector2.Angle(d0, d1), Is.EqualTo(170f).Within(0.5f), "el giro del codo");

            RiverRibbonGraphic ribbon = Ribbon(elbow, RibbonLook.Glass);
            List<UIVertex> verts = Mesh(ribbon);
            Assert.That(verts.Count, Is.EqualTo(18), "tres secciones (inicio, codo y fin) de seis vértices");
            var space = new MapSpace(Basin, Vector2.zero);
            Vector2 b = space.ToLocal(cursor.Point(1));
            float half = RiverRibbonGraphic.WidthClass1Dp * 0.5f;
            float farthest = 0f;
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 p = verts[i].position;
                Assert.That(float.IsNaN(p.x) || float.IsNaN(p.y), Is.False);
                if (i >= 6 && i < 12)
                {
                    farthest = Mathf.Max(farthest, Vector2.Distance(p, b));
                }
            }

            // Sin límite, el inglete de un codo de 170° mediría 1 / cos(85°) ≈ 11,5 medios anchos (≈ 34 dp).
            Assert.That(farthest, Is.EqualTo(RiverRibbonGraphic.MiterLimit * half).Within(0.01f), "inglete recortado al límite");
        }

        // ---------------------------------------------------------------------------------------------- tierra

        [Test]
        public void Land_RealFile_OneColorPerZone_AndFlatGlass()
        {
            var space = new MapSpace(Basin, new Vector2(16f, -40f));
            LandGraphic land = Make<LandGraphic>();
            land.Setup();
            land.SetLand(_map.Land, space);
            var populate = (Action<VertexHelper>)Delegate.CreateDelegate(typeof(Action<VertexHelper>), land, LandPopulate);
            using (var vh = new VertexHelper())
            {
                populate(vh);
                Assert.That(vh.currentVertCount, Is.EqualTo(_map.Land.VertexCount), "una zona: los vértices se comparten");
                Assert.That(vh.currentIndexCount, Is.EqualTo(3 * _map.Land.TriangleCount));
                var v = new UIVertex();
                vh.PopulateUIVertex(ref v, 0);
                Assert.That(Same(v.color, LandGraphic.CanopyColor), Is.True, "dosel");
                int firstIndex = _map.Land.Triangles[0]; // la malla añade los vértices en el orden en que los citan los triángulos
                Assert.That(Vector2.Distance(v.position, space.ToLocal(_map.Land.Xy[2 * firstIndex], _map.Land.Xy[(2 * firstIndex) + 1])),
                    Is.LessThan(1e-3f));

                land.SetFlatColor(RiverRibbonGraphic.GlassBody);
                populate(vh);
                vh.PopulateUIVertex(ref v, 5);
                Assert.That(Same(v.color, RiverRibbonGraphic.GlassBody), Is.True, "tierra en vidrio");

                TestDelegate again = () => populate(vh);
                again();
                Assert.That(again, Is.Not.AllocatingGCMemory());
            }

            // Dos zonas: cada una con su color y sus propios vértices.
            string doc = RiverMapReaderTests.Doc(land: RiverMapReaderTests.Land(zones:
                "[{\"id\": \"a\", \"tile\": \"canopy\", \"first\": 0, \"count\": 1}, {\"id\": \"b\", \"tile\": \"snow\", \"first\": 1, \"count\": 1}]"));
            LandGraphic two = Make<LandGraphic>();
            two.SetLand(RiverMapReader.Read(doc).Land, space);
            var populateTwo = (Action<VertexHelper>)Delegate.CreateDelegate(typeof(Action<VertexHelper>), two, LandPopulate);
            using (var vh = new VertexHelper())
            {
                populateTwo(vh);
                Assert.That(vh.currentVertCount, Is.EqualTo(6), "tres y tres: el borde compartido se duplica");
                var v = new UIVertex();
                vh.PopulateUIVertex(ref v, 0);
                Assert.That(Same(v.color, LandGraphic.CanopyColor), Is.True);
                vh.PopulateUIVertex(ref v, 5);
                Assert.That(Same(v.color, LandGraphic.SnowColor), Is.True);
            }

            Assert.That(LandGraphic.TileColor("rock"), Is.EqualTo(LandGraphic.RockColor));
            Assert.That(LandGraphic.TileColor("desconocido"), Is.EqualTo(LandGraphic.CanopyColor));
        }

        // ---------------------------------------------------------------------------------------------- utilidades

        private RiverRibbonGraphic Ribbon(RiverLine line, RibbonLook look)
        {
            RiverRibbonGraphic ribbon = Make<RiverRibbonGraphic>();
            ribbon.Setup();
            ribbon.SetLine(line);
            ribbon.SetSpace(new MapSpace(Basin, Vector2.zero));
            ribbon.SetLook(look);
            return ribbon;
        }

        private T Make<T>()
            where T : Component
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(CanvasRenderer));
            _objects.Add(go);
            return go.AddComponent<T>();
        }

        private static List<UIVertex> Mesh(RiverRibbonGraphic ribbon)
        {
            var list = new List<UIVertex>();
            using (var vh = new VertexHelper())
            {
                RibbonPopulate.Invoke(ribbon, new object[] { vh });
                var v = new UIVertex();
                for (int i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(ref v, i);
                    list.Add(v);
                }
            }

            return list;
        }

        // Forma de las marcas de la trama de «ribbon» (sin puntos saltados), las últimas 4 · LastMarkCount de la malla, todas del
        // agua «water»: con «elongated», guiones a lo largo del río (un lado al menos el triple del otro y el largo paralelo a la
        // tangente); si no, rombos con los cuatro lados iguales.
        private static void AssertMarkShapes(RiverRibbonGraphic ribbon, RiverLine line, RiverWater water, bool elongated)
        {
            List<UIVertex> verts = Mesh(ribbon);
            int marks = ribbon.LastMarkCount;
            Assert.That(ribbon.LastWasDecimated, Is.False);
            Assert.That(marks, Is.GreaterThan(5), water.ToString());
            var cursor = new PolylineCursor(line);
            float step = RiverRibbonGraphic.PatternStepDp / Basin;
            float aStart = ribbon.FillStart * cursor.Length;
            int at = verts.Count - (4 * marks);
            Color32 color = RiverRibbonGraphic.MarkColor(water);
            for (int m = 0; m < marks; m++)
            {
                Vector2 p0 = verts[at + (4 * m)].position;
                Vector2 p1 = verts[at + (4 * m) + 1].position;
                Vector2 p2 = verts[at + (4 * m) + 2].position;
                for (int q = 0; q < 4; q++)
                {
                    Assert.That(Same(verts[at + (4 * m) + q].color, color), Is.True, water + ", marca " + m);
                }

                float side01 = Vector2.Distance(p0, p1);
                float side12 = Vector2.Distance(p1, p2);
                if (elongated)
                {
                    Vector2 tangent = MapSpace.DirectionToLocal(cursor.TangentAtArc(aStart + (step * (m + 0.5f))));
                    Assert.That(side01, Is.GreaterThanOrEqualTo(3f * side12), water + ", marca " + m + ": un guion");
                    Assert.That(Mathf.Abs(Vector2.Dot((p1 - p0).normalized, tangent)), Is.GreaterThan(0.999f), "a lo largo del río");
                }
                else
                {
                    Assert.That(side01, Is.EqualTo(side12).Within(1e-3f), water + ", marca " + m + ": un rombo");
                }
            }
        }

        // Distancia de p al triángulo (a, b, c): 0 dentro.
        private static float TriangleDistance(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(b - a, p - a);
            float d2 = Cross(c - b, p - b);
            float d3 = Cross(a - c, p - c);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            if (!(hasNeg && hasPos))
            {
                return 0f;
            }

            return Mathf.Min(SegmentDistance(p, a, b), Mathf.Min(SegmentDistance(p, b, c), SegmentDistance(p, c, a)));
        }

        private static float Cross(Vector2 u, Vector2 v)
        {
            return (u.x * v.y) - (u.y * v.x);
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            float t = lengthSq > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq) : 0f;
            return Vector2.Distance(p, a + (ab * t));
        }

        private static float Nearest(List<UIVertex> verts, Vector2 p)
        {
            float best = float.MaxValue;
            foreach (UIVertex v in verts)
            {
                best = Mathf.Min(best, Vector2.Distance(v.position, p));
            }

            return best;
        }

        private static bool Same(Color32 a, Color32 b)
        {
            return Math.Abs(a.r - b.r) <= 1 && Math.Abs(a.g - b.g) <= 1 && Math.Abs(a.b - b.b) <= 1 && Math.Abs(a.a - b.a) <= 1;
        }

        private static bool Same(Color32 a, Color b)
        {
            return Same(a, (Color32)b);
        }

        private static bool Same(Color a, Color b)
        {
            return Same((Color32)a, (Color32)b);
        }

        private static ushort Quant(double x)
        {
            return (ushort)Math.Round(x * 65535.0);
        }

        private static ushort QuantY(double y, double aspect)
        {
            return (ushort)Math.Round(y / aspect * 65535.0);
        }

        // La polilínea exacta en doble precisión, con las unidades y la s de la herramienta.
        private sealed class Reference
        {
            private readonly double[] _x;
            private readonly double[] _y;
            private readonly double[] _cum;

            private Reference(double[] x, double[] y, double[] cum)
            {
                _x = x;
                _y = y;
                _cum = cum;
            }

            public static Reference Of(ushort[] raw, double aspect)
            {
                int n = raw.Length / 2;
                var x = new double[n];
                var y = new double[n];
                var cum = new double[n];
                for (int i = 0; i < n; i++)
                {
                    x[i] = raw[2 * i] / 65535.0;
                    y[i] = raw[(2 * i) + 1] / 65535.0 * aspect;
                    if (i > 0)
                    {
                        cum[i] = cum[i - 1] + Math.Sqrt(((x[i] - x[i - 1]) * (x[i] - x[i - 1])) + ((y[i] - y[i - 1]) * (y[i] - y[i - 1])));
                    }
                }

                return new Reference(x, y, cum);
            }

            public void At(double s, out double x, out double y)
            {
                double target = s * _cum[_cum.Length - 1];
                for (int i = 0; i < _cum.Length - 1; i++)
                {
                    if (_cum[i + 1] >= target)
                    {
                        double d = _cum[i + 1] - _cum[i];
                        double t = d > 0.0 ? (target - _cum[i]) / d : 0.0;
                        x = _x[i] + (t * (_x[i + 1] - _x[i]));
                        y = _y[i] + (t * (_y[i + 1] - _y[i]));
                        return;
                    }
                }

                x = _x[_x.Length - 1];
                y = _y[_y.Length - 1];
            }
        }
    }
}
