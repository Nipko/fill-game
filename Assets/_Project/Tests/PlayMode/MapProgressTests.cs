#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El río de vidrio que se llena (tarea M3.4; D-013, puntos 4 y 5; 06 §4.5; propuesta del Mapa §3.3-§3.5; 04 §2.6), en la
    /// escena <c>Game</c> real con <c>river_map_v1.json</c> y <c>river_stops_v1.json</c>:
    /// <list type="bullet">
    /// <item>con <c>max_level</c> 58, el lleno llega a Manaos y el frente va al 80 % del tramo a Anavilhanas, con 8 de sus 10
    /// boyas encendidas; el frente, las boyas y la canoa en el mismo sitio; los estados con 0, 10, 58, 80, 140 y 200;</item>
    /// <item>el agua real: el Tapajós claro y el Negro negro, las dos aguas de Santarém y del Encuentro, el Xingu entero al pasar
    /// su boca, el Tocantins lleno desde el primer Mapa, y Madeira y Napo (mundos futuros) solo en su boca;</item>
    /// <item>las siluetas de los mundos futuros con <c>map_next_world</c> en azul petróleo («Mundo 6 · Oriente · Nivel 201»);</item>
    /// <item>la boya de un nivel saltado lleva a Rosa sin cortar el agua; con <c>ui_map_mode</c> = <c>worlds</c>, sin boyas;</item>
    /// <item>cada victoria del 1 al 200 se ve en el agua (la boca de un afluente que remonta una ruta no se traga el frente: los
    /// tramos 2, 5, 6, 16 y 18); las boyas por ganar, encima de la canoa; en un tramo de longitud 0 (el 85, el 145, el 185), la
    /// fila corta de boyas bajo la parada; el lleno con su orla de vidrio (≥ 3:1 con la selva) sobre el vidrio fantasma;</item>
    /// <item>nunca retrocede (nivel a nivel y tras un fallo) y solo se recalcula si cambia <c>max_level</c>; el fundido de 300 ms
    /// de los afluentes de paso; «Andes ↑» y «Océano Atlántico ↓» en Baloo 2 y los países en su región; cero asignaciones
    /// con el Mapa quieto.</item>
    /// </list>
    /// </summary>
    public sealed class MapProgressTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator Max58_TheFillReachesManaos_AndTheFrontGoesTo80Percent_With8Buoys()
        {
            yield return OpenApp(59); // max_level 58
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;
            Assert.That(map.ProgressLevel, Is.EqualTo(58));
            Assert.That(map.FrontStop, Is.EqualTo(6), "el tramo de Manaos (5) a Anavilhanas (6)");
            Assert.That(map.FrontFraction, Is.EqualTo(0.8f).Within(1e-6f), "8 niveles de 10 (RiverJourney.LegFill)");

            // Lleno hasta Manaos: el Negro desde Manaos (y la boca, fill_on_pass) hasta su boca; Anavilhanas y Tefé, en vidrio.
            int negro = data.LineIndexOf("negro");
            int amazonas = data.LineIndexOf("amazonas");
            float manaos = routes.At(5)!.At.S;
            float anavilhanas = routes.At(6)!.At.S;
            Assert.That(map.FillFrom(negro), Is.LessThanOrEqualTo(manaos), "Manaos, lleno");
            Assert.That(map.FillFrom(negro), Is.GreaterThan(anavilhanas), "Anavilhanas, todavía no");
            // La parada del Encuentro está en la boca del Negro (geometry.confluence_s [P], revisión de M5.1).
            Assert.That(map.FillFrom(amazonas), Is.EqualTo(routes.At(4)!.At.S).Within(1e-6f), "el cauce, lleno hasta el Encuentro");
            Assert.That(map.FillFrom(amazonas), Is.GreaterThan(routes.At(7)!.At.S), "Tefé, todavía no");
            Assert.That(map.FillRibbon(negro).gameObject.activeSelf, Is.True);
            Assert.That(map.FillRibbon(negro).Look, Is.EqualTo(RibbonLook.Water));
            Assert.That(map.FillRibbon(negro).FillStart, Is.EqualTo(map.FillFrom(negro)).Within(1e-6f));
            Assert.That(map.FillRibbon(negro).FillEnd, Is.EqualTo(1f), "hasta la boca");

            // El frente: una cinta de agua sobre el Negro, de Manaos a 8/10 del tramo.
            Assert.That(map.FrontPieceCount, Is.EqualTo(1));
            RiverRibbonGraphic front = map.FrontRibbon(0);
            Assert.That(front.Line!.Id, Is.EqualTo("negro"));
            Assert.That(front.Look, Is.EqualTo(RibbonLook.Water));
            Assert.That(front.FillEnd, Is.EqualTo(manaos).Within(1e-5f));
            Assert.That(front.FillStart, Is.EqualTo(manaos - (0.8f * (manaos - anavilhanas))).Within(1e-5f));

            // 10 boyas, 8 encendidas (51-58) y 2 por ganar; la del 58 está con la canoa.
            Assert.That(map.BuoyCount, Is.EqualTo(RiverMapScreen.BuoysPerLeg));
            int lit = 0;
            for (int i = 0; i < map.BuoyCount; i++)
            {
                Assert.That(map.BuoyLevel(i), Is.EqualTo(51 + i));
                Assert.That(map.BuoyStateAt(i), Is.EqualTo(i < 8 ? RiverMapScreen.BuoyState.Lit : RiverMapScreen.BuoyState.Unlit), "boya del " + (51 + i));
                Vector2 expected = RiverMapScreen.RoutePoint(data, routes.At(6)!, (i + 1) / 10f);
                Assert.That(Vector2.Distance(map.BuoyMapPoint(i), expected), Is.LessThan(1e-5f), "por la longitud real del tramo");
                lit += map.BuoyStateAt(i) == RiverMapScreen.BuoyState.Lit ? 1 : 0;
            }

            Assert.That(lit, Is.EqualTo(8));
            Assert.That(Vector2.Distance(map.BuoyMapPoint(7), map.CanoeMapPoint), Is.LessThan(1e-5f), "la canoa va en la punta del agua");
            Canvas.ForceUpdateCanvases();
            Assert.That(map.BuoysDrawn, Is.EqualTo(10));
            Assert.That(map.Buoys!.DrawnCount, Is.EqualTo(8), "las encendidas, detrás de la canoa");
            Assert.That(map.BuoysAhead!.DrawnCount, Is.EqualTo(2), "las del 59 y el 60, encima");
            // El frente de una sola pieza (el Negro) cabe en la cifra de referencia de §11.4 (FrontMaxVertices), aunque desde la
            // revisión de FX11 el Mapa no la aplica (sin saltar puntos); el tramo del 300 la supera y lo acota Fx11Tests.
            Assert.That(front.LastVertexCount, Is.InRange(1, RiverRibbonGraphic.FrontMaxVertices), "el frente de una pieza, dentro de la cifra de §11.4");
            Assert.That(map.FrontCasingRibbon(0).LastVertexCount, Is.InRange(1, RiverRibbonGraphic.FrontMaxVertices), "su orla, dentro de la cifra de §11.4");
        }

        [UnityTest]
        public IEnumerator Max58_World_TheBuoysToWin_AreSeenOverTheCanoe()
        {
            yield return OpenApp(59); // max_level 58, vista Mundo
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.View, Is.EqualTo(MapView.World));
            Canvas.ForceUpdateCanvases();
            Rect canoe = map.CanoeBoxLocal;
            Assert.That(canoe.width, Is.GreaterThan(0f));
            RectTransform ahead = map.BuoysAheadLayer!;
            Assert.That(ahead.parent, Is.SameAs(map.Canoe.transform.parent));
            Assert.That(ahead.GetSiblingIndex(), Is.GreaterThan(map.Canoe.transform.GetSiblingIndex()), "las por ganar, encima de la canoa");
            Assert.That(map.BuoysAhead!.transform.parent, Is.SameAs(ahead));
            int under = 0;
            for (int i = 0; i < map.BuoyCount; i++)
            {
                if (map.BuoyStateAt(i) != RiverMapScreen.BuoyState.Unlit)
                {
                    continue;
                }

                // Propuesta §3.4 y §4.1: las que faltan son el «solo me falta uno»; se ven, aunque caigan bajo la canoa.
                Vector2 p = map.BuoyLocalPoint(i);
                bool clear = !canoe.Contains(p);
                under += clear ? 0 : 1;
                Assert.That(clear || ahead.GetSiblingIndex() > map.Canoe.transform.GetSiblingIndex(), Is.True, "boya del " + map.BuoyLevel(i));
            }

            Debug.Log("AF_MAP boyas por ganar bajo la caja de la canoa con max 58 en Mundo: " + under);
            Assert.That(map.BuoysAhead.DrawnCount, Is.EqualTo(2), "el 59 y el 60, dibujadas por la malla de encima");
            yield return null;
        }

        [UnityTest]
        public IEnumerator FrontAndBuoys_At0_10_58_80_85_140_145_185_And200()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;

            // 85 y 185: los tramos de longitud 0 de «misma vista, otra luz» (Leticia al atardecer y Pacaya al alba); 145: el de
            // Itaya, de unos metros.
            var cases = new (int Max, int Front, float Fraction, int Lit)[]
            {
                (0, 1, 0f, 0),
                (10, 1, 1f, 10),
                (58, 6, 0.8f, 8),
                (80, 8, 1f, 10),
                (85, 9, 0.5f, 5),
                (140, 14, 1f, 10),
                (145, 15, 0.5f, 5),
                (185, 19, 0.5f, 5),
                (200, 20, 1f, 10),
            };

            foreach (var c in cases)
            {
                Reopen(boot, new MemoryProgressStore(c.Max + 1, c.Max, c.Max), c.Max + 1);
                string where = "max_level " + c.Max;
                Assert.That(map.ProgressLevel, Is.EqualTo(c.Max), where);
                Assert.That(RiverMapScreen.FrontStopOf(c.Max), Is.EqualTo(c.Front), where);
                if (routes.At(c.Front) == null)
                {
                    // Sin la ruta del tramo (con las paradas de 1-200, el 200 llega a la 20, que no está): sin frente ni boyas.
                    Assert.That(map.FrontStop, Is.EqualTo(-1), where);
                    Assert.That(map.FrontPieceCount, Is.EqualTo(0), where);
                    Assert.That(map.BuoyCount, Is.EqualTo(0), where);
                    continue;
                }

                Assert.That(map.FrontStop, Is.EqualTo(c.Front), where);
                Assert.That(map.FrontFraction, Is.EqualTo(c.Fraction).Within(1e-6f), where);
                bool hasLength = LegLength(data, routes.At(c.Front)!) > 0f;
                Assert.That(map.FrontPieceCount, c.Fraction > 0f && hasLength ? Is.GreaterThan(0) : Is.EqualTo(0), where);
                Assert.That(map.BuoyCount, Is.EqualTo(10), where);
                int lit = 0;
                for (int i = 0; i < 10; i++)
                {
                    lit += map.BuoyStateAt(i) == RiverMapScreen.BuoyState.Lit ? 1 : 0;
                }

                Assert.That(lit, Is.EqualTo(c.Lit), where);

                // Un tramo sin largo a la escala de la vista: las 10 boyas en la fila corta bajo la parada, con el radio de la
                // vista, separadas y fuera de la caja de la canoa (que va en la parada).
                bool shortLeg = c.Max == 85 || c.Max == 145 || c.Max == 185;
                foreach (MapView v in new[] { MapView.World, MapView.Basin })
                {
                    map.SetView(v);
                    Canvas.ForceUpdateCanvases();
                    string at = where + " en " + v;
                    if (shortLeg)
                    {
                        Assert.That(map.BuoysCompact, Is.True, at);
                    }

                    if (!map.BuoysCompact)
                    {
                        continue;
                    }

                    float radius = v == MapView.World ? RiverMapScreen.BuoyRadiusWorldDp : RiverMapScreen.BuoyRadiusBasinDp;
                    Assert.That(map.Buoys!.Radius, Is.EqualTo(radius), at + ": el radio de la vista, no el mínimo");
                    Rect canoe = map.CanoeBoxLocal;
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 p = map.BuoyLocalPoint(i);
                        Assert.That(canoe.Contains(p), Is.False, at + ": la canoa no tapa la boya del " + map.BuoyLevel(i));
                        if (i > 0)
                        {
                            Assert.That(Vector2.Distance(p, map.BuoyLocalPoint(i - 1)), Is.GreaterThanOrEqualTo((2f * radius) - 1e-3f),
                                at + ": boyas separadas");
                            Assert.That(p.x, Is.GreaterThan(map.BuoyLocalPoint(i - 1).x), at + ": en orden, de izquierda a derecha");
                        }
                    }

                    Assert.That(map.BuoysDrawn, Is.EqualTo(10), at);
                }

                map.SetView(MapView.World);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryWin_AdvancesTheVisibleWater_InTheFrontLeg_From1To200()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;
            var from = new float[data.Lines.Count];

            // D-013, punto 4: cada victoria llena una décima del tramo; la boca de un afluente que remonta una ruta no se traga el
            // frente (el Tapajós en los tramos 2 y 3, el Negro en el 5 y el 6, el Nanay en el 16 y el Marañón en el 18).
            var required = new HashSet<int> { 2, 5, 6, 16, 18 };
            var checkedLegs = new HashSet<int>();
            var advancing = new Dictionary<int, int>();
            int last = Mathf.Min(200, routes.Count * RiverStopCatalog.LevelsPerStop);
            for (int max = 1; max <= last; max++)
            {
                int leg = RiverMapScreen.FrontStopOf(max);
                RiverStopRoute? route = routes.At(leg);
                if (route == null)
                {
                    continue;
                }

                float now = VisibleFront(data, routes, max, from, out bool tipUpstream);
                float before = RiverMapScreen.FrontStopOf(max - 1) == leg ? VisibleFront(data, routes, max - 1, from, out _) : 0f;
                string where = "max_level " + max + " (tramo " + leg + ")";

                // Mientras la punta remonta (un tramo de vuelta, río abajo, va por agua ya llena: ahí avanzan la canoa y las
                // boyas), cada victoria se ve en el agua: el frente pasa del lleno.
                if (tipUpstream)
                {
                    Assert.That(now, Is.GreaterThan(before), where + ": la victoria se ve en el agua (el frente pasa del lleno)");
                    if (required.Contains(leg))
                    {
                        advancing[leg] = (advancing.TryGetValue(leg, out int n) ? n : 0) + 1;
                    }
                }

                // Con el tramo entero (el 10k), todo lo que remonta por el afluente se ve: su boca llena no se lo tragó.
                if (max % RiverStopCatalog.LevelsPerStop == 0 && required.Contains(leg))
                {
                    float upstream = UpstreamLength(data, route);
                    Assert.That(upstream, Is.GreaterThan(0f), where);
                    Assert.That(now, Is.GreaterThanOrEqualTo(upstream * 0.9999f), where + ": lo que remonta el tramo, entero a la vista");
                    checkedLegs.Add(leg);
                }
            }

            Assert.That(checkedLegs, Is.EquivalentTo(required));
            foreach (int leg in required)
            {
                int n = advancing.TryGetValue(leg, out int k) ? k : 0;
                Debug.Log("AF_MAP tramo " + leg + ": " + n + " de 10 victorias con el agua avanzando a la vista");
                Assert.That(n, Is.GreaterThanOrEqualTo(8), "tramo " + leg + ": las victorias se ven en el agua");
            }

            // Entre el 11 y el 20, el Tapajós no se llena por la boca: lo remonta el tramo 2.
            RiverMapScreen.ComputeFill(data, routes, 15, from);
            Assert.That(from[data.LineIndexOf("tapajos")], Is.EqualTo(RiverMapScreen.NoFill));
            RiverMapScreen.ComputeFill(data, routes, 45, from);
            Assert.That(from[data.LineIndexOf("negro")], Is.EqualTo(RiverMapScreen.NoFill), "el Negro, entre el 41 y el 50");
            RiverMapScreen.ComputeFill(data, routes, 155, from);
            Assert.That(from[data.LineIndexOf("nanay")], Is.EqualTo(RiverMapScreen.NoFill), "el Nanay, hasta llegar a Allpahuayo");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Max200_RealWaters_PassbyWhole_AndFutureWorldsOnlyAtTheirMouth()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;
            Reopen(boot, new MemoryProgressStore(201, 200, 200), 200);
            Assert.That(map.ProgressLevel, Is.EqualTo(200));

            // El Tapajós claro y el Negro negro, llenos hasta sus paradas.
            int tapajos = data.LineIndexOf("tapajos");
            int negro = data.LineIndexOf("negro");
            Assert.That(data.Lines[tapajos].WaterAt(0.9f), Is.EqualTo(RiverWater.Clear));
            Assert.That(data.Lines[negro].WaterAt(0.95f), Is.EqualTo(RiverWater.Black));
            Assert.That(map.FillFrom(tapajos), Is.LessThanOrEqualTo(routes.At(3)!.At.S + 1e-6f), "hasta la Flona do Tapajós");
            Assert.That(map.FillFrom(negro), Is.LessThanOrEqualTo(routes.At(6)!.At.S + 1e-6f), "hasta Anavilhanas");
            Assert.That(map.FillRibbon(tapajos).Look, Is.EqualTo(RibbonLook.Water));
            Assert.That(map.FillRibbon(negro).Look, Is.EqualTo(RibbonLook.Water));
            Assert.That(RiverRibbonGraphic.WaterColor(RiverWater.Clear), Is.EqualTo(Palette.WaterClear));
            Assert.That(RiverRibbonGraphic.WaterColor(RiverWater.Black), Is.EqualTo(Palette.WaterBlack));

            // Los de paso, enteros; el Tocantins también.
            foreach (string id in new[] { "xingu", "trombetas", "purus", "jurua", "japura", "putumayo_ica", "tocantins" })
            {
                int line = data.LineIndexOf(id);
                Assert.That(data.Lines[line].Kind, Is.EqualTo(RiverLineKind.Passby), id);
                Assert.That(map.FillFrom(line), Is.EqualTo(0f), id + ": entero al pasar su boca");
                Assert.That(map.FillRibbon(line).gameObject.activeSelf, Is.True, id);
            }

            // Madeira (mundos 7 y 8) y Napo (mundo 6): solo la boca (fill_on_pass, con al menos 14 dp en la Cuenca), mientras
            // ninguna ruta del catálogo los remonte; si alguna lo hace, se llenan por la ruta y la boca no se traga su frente.
            foreach (string id in new[] { "madeira", "napo" })
            {
                int line = data.LineIndexOf(id);
                RiverLine river = data.Lines[line];
                Assert.That(river.FillOnPass, Is.InRange(0.01f, 0.5f), id);
                if (RiverMapScreen.RouteClimbs(routes, line))
                {
                    Assert.That(map.FillFrom(line), Is.EqualTo(RiverMapScreen.NoFill), id + ": lo remonta una ruta, sin boca");
                    continue;
                }

                float mouth = RiverMapScreen.MouthFill(data, river);
                Assert.That(mouth, Is.GreaterThanOrEqualTo(river.FillOnPass), id);
                Assert.That(map.FillFrom(line), Is.EqualTo(1f - mouth).Within(1e-6f), id + ": solo la boca");

                // Lo que asoma fuera del cauce anfitrión (su medio ancho y su orla), en la Cuenca.
                float host = RiverRibbonGraphic.WidthForClass(data.Lines[river.Mouth!.Value.Line].Class) * 0.5f;
                float outside = (mouth * river.Length * MapSpace.BasinWidthDp) - host - RiverRibbonGraphic.CasingDp;
                Debug.Log("AF_MAP boca de " + id + ": " + outside.ToString("0.0", CultureInfo.InvariantCulture) + " dp fuera del cauce en la Cuenca");
                Assert.That(outside, Is.GreaterThanOrEqualTo(RiverMapScreen.MouthMinBasinDp - 1e-3f), id + ": la boca, de al menos 14 dp en la Cuenca (no una grieta)");
            }

            // Las dos aguas de Santarém y del Encuentro van dentro del lleno del cauce (la cinta las parte a lo largo).
            RiverLine main = data.Lines[data.LineIndexOf("amazonas")];
            Assert.That(main.Splits.Count, Is.EqualTo(2));
            for (int i = 0; i < main.Splits.Count; i++)
            {
                Assert.That(map.FillFrom(main.Index), Is.LessThanOrEqualTo(main.Splits[i].S0), main.Splits[i].Id);
                Assert.That(main.TryGetSplitAt((main.Splits[i].S0 + main.Splits[i].S1) * 0.5f, out _), Is.True);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheTocantins_IsFullFromTheFirstMap_AndTheXingu_WhenTheFrontPassesItsMouth()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;
            var from = new float[data.Lines.Count];
            int tocantins = data.LineIndexOf("tocantins");
            int xingu = data.LineIndexOf("xingu");
            int tapajos = data.LineIndexOf("tapajos");
            RiverMapScreen.ComputeFill(data, routes, 0, from);
            Assert.That(from[tocantins], Is.EqualTo(0f), "el Tocantins va lleno desde el primer Mapa (propuesta §3.3)");
            Assert.That(from[xingu], Is.EqualTo(RiverMapScreen.NoFill), "con 0 niveles, el Xingu en vidrio");

            // La boca del Xingu cae dentro del tramo de Belém a Santarém: se llena entero en cuanto el frente la pasa.
            float mouth = RiverMapScreen.MouthFraction(data, routes.At(1)!, xingu);
            Assert.That(mouth, Is.InRange(0.01f, 0.99f), "la boca del Xingu, a mitad del tramo 1");
            bool passed = false;
            for (int max = 1; max <= 10; max++)
            {
                RiverMapScreen.ComputeFill(data, routes, max, from);
                bool now = RiverJourney.LegFill(max) >= mouth;
                Assert.That(from[xingu], Is.EqualTo(now ? 0f : RiverMapScreen.NoFill), "max_level " + max);
                passed |= now;
            }

            Assert.That(passed, Is.True);

            // Con el tramo de Santarém ya detrás (el 11 ganado; en el 10, ese tramo aún es el frente, lleno), el estuario y el
            // cauce hasta Santarém, en el lleno, y la boca del Tapajós.
            RiverMapScreen.ComputeFill(data, routes, 11, from);
            Assert.That(from[data.LineIndexOf("para")], Is.EqualTo(0f), "el ramal del Pará, entero");
            Assert.That(from[data.LineIndexOf("amazonas")], Is.EqualTo(routes.At(1)!.At.S).Within(1e-6f), "el cauce hasta Santarém");
            Assert.That(RiverMapScreen.RouteClimbs(routes, tapajos), Is.True);
            Assert.That(from[tapajos], Is.EqualTo(RiverMapScreen.NoFill), "el Tapajós no se llena por la boca: lo remonta el tramo 2, que es el frente");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheFill_NeverRecedes_LevelByLevel()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;
            var previous = new float[data.Lines.Count];
            var now = new float[data.Lines.Count];
            RiverMapScreen.ComputeFill(data, routes, 0, previous);
            int last = Mathf.Min(WorldTable.LastLevel, (routes.Count * RiverStopCatalog.LevelsPerStop) + 20);
            for (int max = 1; max <= last; max++)
            {
                RiverMapScreen.ComputeFill(data, routes, max, now);
                for (int i = 0; i < now.Length; i++)
                {
                    Assert.That(now[i], Is.LessThanOrEqualTo(previous[i]), data.Lines[i].Id + " con max_level " + max + ": el agua nunca retrocede");
                    previous[i] = now[i];
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator AfterAFailure_TheRiverDoesNotRecede_AndIsNotRebuilt()
        {
            LaunchOptions.LevelCardsOverride = false;
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            float[] before = Snapshot(map, data);
            int builds = map.ProgressBuilds;
            float fraction = map.FrontFraction;

            // Jugar el 59 y perderlo desde la pausa (cuesta una canoa: es un fallo; 04 §5.2).
            map.PlayButton.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow != null && boot.Flow.LevelNumber == 59, "el nivel 59");
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            view.PauseButton.onClick.Invoke();
            view.Pause.QuitButton.onClick.Invoke();
            view.Confirm.ConfirmButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            view.Failed.CloseButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.Profile!.Progress.MaxLevel, Is.EqualTo(58), "un fallo no cambia max_level");

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.ProgressLevel, Is.EqualTo(58));
            Assert.That(map.ProgressBuilds, Is.EqualTo(builds), "el mismo max_level: no se recalcula");
            Assert.That(map.FrontFraction, Is.EqualTo(fraction));
            float[] after = Snapshot(map, data);
            for (int i = 0; i < after.Length; i++)
            {
                Assert.That(after[i], Is.EqualTo(before[i]), data.Lines[i].Id + ": el agua no retrocede tras un fallo");
            }

            boot.enabled = true;
        }

        [UnityTest]
        public IEnumerator TheFill_IsRebuiltOnlyWhenMaxLevelChanges()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            int builds = map.ProgressBuilds;
            Assert.That(builds, Is.GreaterThan(0));
            Reopen(boot, new MemoryProgressStore(59, 58, 58), 59);
            Assert.That(map.ProgressBuilds, Is.EqualTo(builds), "mismo max_level");
            map.SetView(MapView.Basin);
            map.SetView(MapView.World);
            Assert.That(map.ProgressBuilds, Is.EqualTo(builds), "cambiar de vista solo reescala");
            Reopen(boot, new MemoryProgressStore(61, 60, 60), 61);
            Assert.That(map.ProgressBuilds, Is.EqualTo(builds + 1), "otro max_level");
            Assert.That(map.FrontFraction, Is.EqualTo(1f), "el tramo recién completado");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ASkippedLevel_ItsBuoyCarriesRosa_AndTheWaterIsNotCut()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            var progress = new MemoryProgressStore(59, 58, 58);
            progress.MarkSkipped(55);
            Reopen(boot, progress, 59);
            Assert.That(map.BuoyLevel(4), Is.EqualTo(55));
            Assert.That(map.BuoyStateAt(4), Is.EqualTo(RiverMapScreen.BuoyState.Skipped), "04 §2.6: la boya del nivel saltado lleva a Rosa");
            Image rosa = map.BuoyRosa(4);
            Assert.That(rosa.gameObject.activeInHierarchy, Is.True);
            Assert.That(rosa.sprite, Is.Not.Null, "el icono procedural de Rosa");
            Assert.That(rosa.rectTransform.anchoredPosition, Is.EqualTo(map.Space.ToLocal(map.BuoyMapPoint(4))));
            for (int i = 0; i < 10; i++)
            {
                if (i != 4)
                {
                    Assert.That(map.BuoyRosa(i).gameObject.activeSelf, Is.False, "boya del " + map.BuoyLevel(i));
                }
            }

            Assert.That(map.FrontFraction, Is.EqualTo(0.8f).Within(1e-6f), "el agua no se corta");
            Canvas.ForceUpdateCanvases();
            Assert.That(map.BuoysDrawn, Is.EqualTo(9), "la saltada no lleva disco: lleva a Rosa");
            Assert.That(map.PastRosaCount, Is.EqualTo(0), "el 55 está en el frente");

            // Rosa se reconoce: entre 12 y 18 dp, como mucho 1,6 veces la distancia entre boyas (si no, tapa la vecina), con el halo
            // de vidrio_borde y encima de la canoa.
            foreach (MapView v in new[] { MapView.World, MapView.Basin })
            {
                map.SetView(v);
                float spacing = float.MaxValue;
                for (int i = 1; i < 10; i++)
                {
                    spacing = Mathf.Min(spacing, Vector2.Distance(map.BuoyMapPoint(i), map.BuoyMapPoint(i - 1)) * map.Space.DpPerUnit);
                }

                float w = rosa.rectTransform.sizeDelta.x;
                float cap = v == MapView.World ? RiverMapScreen.SkippedRosaWorldDp : RiverMapScreen.SkippedRosaBasinDp;
                Assert.That(w, Is.InRange(RiverMapScreen.SkippedRosaMinDp - 1e-4f, cap + 1e-4f), v + ": ancho de Rosa");
                Assert.That(w, Is.LessThanOrEqualTo(Mathf.Max(RiverMapScreen.SkippedRosaMinDp, RiverMapScreen.SkippedRosaSpacingFactor * spacing) + 1e-4f), v + ": no descoloca la fila de boyas");
                Assert.That(rosa.rectTransform.anchoredPosition, Is.EqualTo(map.BuoyLocalPoint(4)), v + ": en su boya");
                if (!map.BuoysCompact)
                {
                    Assert.That(map.BuoyLocalPoint(4), Is.EqualTo(map.Space.ToLocal(map.BuoyMapPoint(4))), v + ": por la longitud real del tramo");
                }
            }

            Outline halo = rosa.GetComponent<Outline>();
            Assert.That(halo, Is.Not.Null, "halo de Rosa");
            Assert.That(halo.effectColor, Is.EqualTo(Palette.GlassRim), "vidrio_borde, #EAF7F3");
            Assert.That(Mathf.Abs(halo.effectDistance.x), Is.EqualTo(RiverMapScreen.SkippedRosaHaloDp));
            Assert.That(rosa.transform.parent, Is.SameAs(map.RosaLayer));
            Assert.That(map.RosaLayer!.parent, Is.SameAs(map.Canoe.transform.parent));
            Assert.That(map.RosaLayer.GetSiblingIndex(), Is.GreaterThan(map.Canoe.transform.GetSiblingIndex()), "Rosa, encima de la canoa");

            // Con el nivel saltado ya en un tramo recorrido (max 63: el frente va del 61 al 70), Rosa sigue en su boya del tramo
            // de Manaos a Anavilhanas (04 §2.6: queda marcado en el Mapa) y ninguna de las boyas 61-70 la lleva.
            var later = new MemoryProgressStore(64, 63, 63);
            later.MarkSkipped(55);
            Reopen(boot, later, 64);
            Assert.That(map.FrontStop, Is.EqualTo(7));
            for (int i = 0; i < 10; i++)
            {
                Assert.That(map.BuoyLevel(i), Is.EqualTo(61 + i));
                Assert.That(map.BuoyRosa(i).gameObject.activeSelf, Is.False, "boya del " + (61 + i));
                Assert.That(map.BuoyStateAt(i), Is.Not.EqualTo(RiverMapScreen.BuoyState.Skipped), "boya del " + (61 + i));
            }

            Assert.That(map.PastRosaCount, Is.EqualTo(1));
            Assert.That(map.PastRosaLevel(0), Is.EqualTo(55));
            Vector2 expected = RiverMapScreen.RoutePoint(boot.RiverMapData!, boot.RiverRoutes!.At(6)!, 0.5f);
            Assert.That(Vector2.Distance(map.PastRosaMapPoint(0), expected), Is.LessThan(1e-5f), "en la boya del 55, en el tramo 5 → 6");
            Image past = map.PastRosa(0);
            Assert.That(past.gameObject.activeInHierarchy, Is.True);
            Assert.That(past.sprite, Is.Not.Null);
            Assert.That(past.rectTransform.anchoredPosition, Is.EqualTo(map.Space.ToLocal(expected)));
            Assert.That(past.rectTransform.sizeDelta.x, Is.GreaterThanOrEqualTo(RiverMapScreen.SkippedRosaMinDp - 1e-4f));

            // Sin el nivel saltado, ninguna Rosa.
            Reopen(boot, new MemoryProgressStore(64, 63, 63), 64);
            Assert.That(map.PastRosaCount, Is.EqualTo(0));
            Assert.That(past.gameObject.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WithUiMapModeWorlds_ThereAreNoBuoys()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            try
            {
                map.ProgressMapMode = MapConfig.MapModeWorlds;
                Reopen(boot, new MemoryProgressStore(59, 58, 58), 59);
                Assert.That(map.BuoyCount, Is.EqualTo(0), "ui_map_mode = worlds (08 §9): sin boyas");
                Canvas.ForceUpdateCanvases();
                Assert.That(map.BuoysDrawn, Is.EqualTo(0));
                Assert.That(map.FrontFraction, Is.EqualTo(0.8f).Within(1e-6f), "el río sí");
                var later = new MemoryProgressStore(64, 63, 63);
                later.MarkSkipped(55);
                Reopen(boot, later, 64);
                Assert.That(map.PastRosaCount, Is.EqualTo(0), "sin boyas, tampoco Rosa");
            }
            finally
            {
                map.ProgressMapMode = MapConfig.UiMapMode;
            }

            Reopen(boot, new MemoryProgressStore(59, 58, 58), 59);
            Assert.That(map.BuoyCount, Is.EqualTo(10));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FutureWorlds_HaveTheirSilhouette_WithMapNextWorld()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverStopCatalog stops = boot.RiverStops!;
            RiverRoutes routes = boot.RiverRoutes!;

            // Con max_level 58 (mundo 3), los mundos 4 y siguientes del catálogo son futuros.
            var expected = new List<string>();
            for (int world = 2; world <= WorldTable.WorldCount; world++)
            {
                RiverStop? gate = stops.FirstStopOfWorld(world);
                if (gate != null && routes.At(gate.N) != null && gate.N * 10 > 58)
                {
                    expected.Add(UiText.MapNextWorld(world, gate.FirstLevel));
                }
            }

            Assert.That(expected, Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(expected[0], Is.EqualTo("Mundo 4 · Trapecio · Nivel 81"), "map_next_world (glosario §10)");
            Assert.That(map.SilhouetteCount, Is.EqualTo(expected.Count));
            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                Assert.That(map.SilhouetteText(i), Is.EqualTo(expected[i]));
                Color plate = map.SilhouettePlate(i).color;
                Color petroleo = Palette.Hex(0x234452);
                Assert.That(Mathf.Abs(plate.r - petroleo.r) + Mathf.Abs(plate.g - petroleo.g) + Mathf.Abs(plate.b - petroleo.b), Is.LessThan(0.01f),
                    "azul petróleo, panel_alto (propuesta §3.5)");
                Assert.That(plate.a, Is.GreaterThanOrEqualTo(RiverMapScreen.SilhouettePlateAlpha - 1e-4f), "casi opaca: no se transparentan las líneas");
                Assert.That(map.SilhouetteLabel(i).fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f));
                RiverStop gate = stops.FirstStopOfWorld(map.SilhouetteWorld(i))!;
                Vector2 at = RiverMapScreen.PlacePoint(boot.RiverMapData!, routes.At(gate.N)!.At);
                Assert.That(Vector2.Distance(map.SilhouetteMapPoint(i), at), Is.LessThan(1e-6f), "en su gran parada");
                RectTransform root = (RectTransform)map.SilhouettePlate(i).transform.parent;
                float x0 = root.anchoredPosition.x - (root.pivot.x * root.sizeDelta.x);
                Assert.That(x0, Is.GreaterThanOrEqualTo(0f), map.SilhouetteText(i) + ": dentro del mapa");
                Assert.That(x0 + root.sizeDelta.x, Is.LessThanOrEqualTo(map.ContentSize.x + 0.01f), map.SilhouetteText(i) + ": dentro del mapa");
            }

            // El mundo de la siguiente gran parada deja de ser futuro al llegar a ella (el 80: Leticia).
            Reopen(boot, new MemoryProgressStore(81, 80, 80), 81);
            Assert.That(map.SilhouetteCount, Is.EqualTo(expected.Count - 1));
            Assert.That(map.SilhouetteWorld(0), Is.EqualTo(5));
            yield return null;
        }

        [UnityTest]
        public IEnumerator World6_Silhouette_AtTheNapo_WhoseMouthDoesNotSwallowTheFront()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            BuildWithWorld6(boot, out RiverStopCatalog stops, out RiverRoutes routes);
            int napo = data.LineIndexOf("napo");

            map.Close();
            map.Open(MapOpenSource.Lobby, data, routes, stops, new MemoryProgressStore(141, 140, 140), 141, true);
            Assert.That(map.SilhouetteCount, Is.EqualTo(1));
            Assert.That(map.SilhouetteWorld(0), Is.EqualTo(6));
            Assert.That(map.SilhouetteText(0), Is.EqualTo("Mundo 6 · Oriente · Nivel 201"));
            Assert.That(RiverMapScreen.RouteClimbs(routes, napo), Is.True, "con la parada 20, una ruta remonta el Napo");
            Assert.That(map.FillFrom(napo), Is.EqualTo(RiverMapScreen.NoFill), "el Napo, sin boca: su boca se tragaría el tramo 20");
            Assert.That(map.SilhouetteMapPoint(0).x, Is.EqualTo(RiverMapScreen.PlacePoint(data, routes.At(20)!.At).x).Within(1e-6f));

            // La gran parada de Mazán cae junto a la canoa (en Iquitos, con max 140): la placa la deja libre, con 6 dp de hueco, y
            // se lee entera, en las dos vistas.
            Assert.That(map.HasCanoe, Is.True);
            foreach (MapView v in new[] { MapView.Basin, MapView.World })
            {
                map.SetView(v);
                Canvas.ForceUpdateCanvases();
                Rect plate = FrameRect(map.SilhouettePlate(0).rectTransform);
                Rect canoe = FrameRect(map.Canoe.rectTransform);
                Assert.That(plate.Overlaps(canoe), Is.False, v + ": la canoa no tapa «" + map.SilhouetteText(0) + "»");
                float m = RiverMapScreen.SilhouetteCanoeClearanceDp - 0.5f;
                Rect wide = Rect.MinMaxRect(canoe.xMin - m, canoe.yMin - m, canoe.xMax + m, canoe.yMax + m);
                Assert.That(plate.Overlaps(wide), Is.False, v + ": al menos 6 dp de hueco con la canoa");
                TextMeshProUGUI label = map.SilhouetteLabel(0);
                Assert.That(label.preferredWidth, Is.LessThanOrEqualTo(plate.width + 0.5f), v + ": el texto cabe en la placa");
                RectTransform root = (RectTransform)map.SilhouettePlate(0).transform.parent;
                float x0 = root.anchoredPosition.x - (root.pivot.x * root.sizeDelta.x);
                Assert.That(x0, Is.GreaterThanOrEqualTo(0f), v + ": dentro del mapa");
                Assert.That(x0 + root.sizeDelta.x, Is.LessThanOrEqualTo(map.ContentSize.x + 0.01f), v + ": dentro del mapa");
            }

            map.SetView(MapView.World);

            // Al llegar a Mazán (el 200) el mundo 6 ya no es futuro; el frente remonta el Napo.
            map.Close();
            map.Open(MapOpenSource.Lobby, data, routes, stops, new MemoryProgressStore(201, 200, 200), 201, true);
            Assert.That(map.SilhouetteCount, Is.EqualTo(0));
            Assert.That(map.FrontStop, Is.EqualTo(20));
            Assert.That(map.FrontRibbon(map.FrontPieceCount - 1).Line!.Id, Is.EqualTo("napo"));
            Assert.That(map.FillFrom(napo), Is.EqualTo(RiverMapScreen.NoFill), "en el lleno, todavía nada: el Napo va en el frente");
            RiverRibbonGraphic napoFront = map.FrontRibbon(map.FrontPieceCount - 1);
            Assert.That(napoFront.FillStart, Is.EqualTo(0.946947f).Within(1e-5f), "el frente remonta el Napo hasta Mazán");
            Assert.That(napoFront.FillEnd, Is.EqualTo(1f));
            map.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PassbyTributaries_FadeIn300ms_TheFirstTimeTheyAreSeen()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            int xingu = data.LineIndexOf("xingu");
            Reopen(boot, new MemoryProgressStore(59, 58, 0), 59); // map_seen_level 0: el Xingu llega nuevo
            Assert.That(map.FillFading, Is.True);
            Assert.That(map.FillAlpha(xingu), Is.EqualTo(0f));
            Assert.That(map.FillAlpha(data.LineIndexOf("negro")), Is.EqualTo(1f), "un afluente de un mundo no se funde");
            map.Tick(150f);
            Assert.That(map.FillAlpha(xingu), Is.EqualTo(0.5f).Within(0.02f), "a mitad de PassbyFillFadeMs");
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1), "60 fps mientras se funde");
            map.Tick(MapConfig.PassbyFillFadeMs);
            Assert.That(map.FillAlpha(xingu), Is.EqualTo(1f));
            Assert.That(map.FillFading, Is.False);

            Reopen(boot, new MemoryProgressStore(59, 58, 58), 59);
            Assert.That(map.FillFading, Is.False, "ya visto: sin fundido");
            Assert.That(map.FillAlpha(xingu), Is.EqualTo(1f));

            // Reducir movimiento (07 §7.8): un fundido de 150 ms (propuesta §4.2).
            try
            {
                map.ProgressReduceMotion = true;
                Reopen(boot, new MemoryProgressStore(59, 58, 0), 59);
                Assert.That(map.FillFading, Is.True);
                Assert.That(map.FillAlpha(xingu), Is.EqualTo(0f));
                map.Tick(MapConfig.ReducedMotionFadeMs * 0.5f);
                Assert.That(map.FillAlpha(xingu), Is.EqualTo(0.5f).Within(0.02f), "a los 75 ms, a mitad");
                map.Tick(MapConfig.ReducedMotionFadeMs * 0.5f);
                Assert.That(map.FillAlpha(xingu), Is.EqualTo(1f), "a los 150 ms, terminado");
                Assert.That(map.FillFading, Is.False);
            }
            finally
            {
                map.ProgressReduceMotion = false;
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator OrientationLabels_InBaloo2_AndCountries_InTheirRegion()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            TextMeshProUGUI andes = map.AndesLabel!;
            TextMeshProUGUI atlantic = map.AtlanticLabel!;
            Assert.That(andes.text, Is.EqualTo("Andes ↑"), "map_label_andes (P-M2)");
            Assert.That(atlantic.text, Is.EqualTo("Océano Atlántico ↓"), "map_label_atlantic (P-M2)");
            foreach (MapView v in new[] { MapView.World, MapView.Basin })
            {
                map.SetView(v);
                foreach (TextMeshProUGUI label in new[] { andes, atlantic })
                {
                    Assert.That(label.font, Is.SameAs(UiFonts.Font(TextStyle.ModalTitle)), label.name + ": Baloo 2, que tiene las flechas");
                    Assert.That(label.font.HasCharacters(label.text, out List<char> missing), Is.True, label.name + ": sin " + string.Join(",", missing ?? new List<char>()));
                    Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f));
                    Assert.That(label.transform.parent, Is.SameAs(map.ViewportRect.parent), "fijo en el marco: no se arrastra");
                    Rect r = FrameRect(label.rectTransform);
                    Rect viewport = FrameRect(map.ViewportRect);
                    Assert.That(viewport.Contains(r.min) && viewport.Contains(r.max), Is.True, label.name + " dentro de la zona del mapa");
                    Assert.That(label.preferredWidth, Is.LessThanOrEqualTo(r.width + 0.5f), label.name + ": cabe");
                }

                Rect button = FrameRect((RectTransform)map.BackToCanoeButton.transform);
                Assert.That(Shrunk(FrameRect(atlantic.rectTransform)).Overlaps(button), Is.False, "no pisa «Volver a la canoa»");
                Assert.That(FrameRect(andes.rectTransform).yMax, Is.LessThan(FrameRect(atlantic.rectTransform).yMin), "Andes arriba, Atlántico abajo");
            }

            // Los países en su región, por Loc.Fact.
            Assert.That(data.Labels.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(map.CountryLabelCount, Is.EqualTo(data.Labels.Count));
            var names = new List<string>();
            for (int i = 0; i < data.Labels.Count; i++)
            {
                TextMeshProUGUI label = map.CountryLabel(i);
                Assert.That(label.text, Is.EqualTo(Loc.Fact(data.Labels[i].NameKey)), data.Labels[i].Country);
                Assert.That(label.text.Length, Is.GreaterThan(0));
                Vector2 at = map.Space.ToLocal(data.Labels[i].X, data.Labels[i].Y);
                Vector2 placed = label.rectTransform.anchoredPosition;
                float half = label.rectTransform.sizeDelta.x * 0.5f;
                Assert.That(Vector2.Distance(placed, at), Is.LessThanOrEqualTo(RiverMapScreen.CountryMaxShiftDp + half + RiverMapScreen.LabelEdgeDp),
                    data.Labels[i].Country + ": en su región (apartado del río o corrido hacia dentro si hace falta)");
                Assert.That(placed.x - half, Is.GreaterThanOrEqualTo(0f), data.Labels[i].Country + ": dentro del mapa");
                Assert.That(placed.x + half, Is.LessThanOrEqualTo(map.ContentSize.x + 0.01f), data.Labels[i].Country + ": dentro del mapa");
                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f));
                Assert.That(label.fontSize, Is.LessThan(andes.fontSize), data.Labels[i].Country + ": un escalón por debajo de «Andes ↑»");
                Assert.That(label.color.a, Is.EqualTo(RiverMapScreen.CountryLabelAlpha).Within(1e-4f), "crema al 75 %");
                Assert.That(label.fontSharedMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth),
                    Is.LessThan(UiFonts.Material(TextStyle.PillWood)!.GetFloat(ShaderUtilities.ID_OutlineWidth)), "contorno de 1 dp, más fino que la píldora");
                names.Add(label.text);
            }

            Assert.That(names, Does.Contain("Brasil").And.Contain("Perú").And.Contain("Colombia"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CountryLabels_DoNotCoverTheRiver_NorEachOther()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            foreach (int max in new[] { 58, 200 })
            {
                Reopen(boot, new MemoryProgressStore(max + 1, max, max), max + 1);
                foreach (MapView v in new[] { MapView.Basin, MapView.World })
                {
                    map.SetView(v);
                    var boxes = new List<Rect>();
                    for (int i = 0; i < data.Labels.Count; i++)
                    {
                        if (map.CountryBasinHidden(i))
                        {
                            // En la Cuenca no sale: no cabe sin quedar bajo un control del Mapa ni pisar el río (revisión de FX7).
                            Assert.That(v, Is.EqualTo(MapView.Basin), data.Labels[i].Country + " con max " + max + ": solo se oculta en la Cuenca");
                            Assert.That(map.CountryLabel(i).canvasRenderer.GetAlpha(), Is.EqualTo(0f), data.Labels[i].Country + " con max " + max);
                            continue;
                        }

                        RectTransform rt = map.CountryLabel(i).rectTransform;
                        Vector2 c = rt.anchoredPosition;
                        Vector2 size = rt.sizeDelta;
                        var box = new Rect(c.x - (size.x * 0.5f), c.y - (size.y * 0.5f), size.x, size.y);
                        string where = data.Labels[i].Country + " en " + v + " con max " + max;
                        Assert.That(size.x, Is.GreaterThan(0f), where);

                        // Comprobación independiente: muestras cada 0,5 dp de cada línea de clase 1-2, con su medio ancho.
                        float gap = SampledClearance(data, map.Space, v, box);
                        Assert.That(gap, Is.GreaterThanOrEqualTo(-0.05f), where + ": no pisa el río (hueco " + gap.ToString("0.0", CultureInfo.InvariantCulture) + " dp)");
                        if (v == MapView.Basin)
                        {
                            Debug.Log("AF_MAP país " + where + ": hueco con el río " + gap.ToString("0.0", CultureInfo.InvariantCulture) + " dp");
                        }

                        for (int k = 0; k < boxes.Count; k++)
                        {
                            Assert.That(box.Overlaps(boxes[k]), Is.False, where + ": no pisa otro país");
                        }

                        boxes.Add(box);
                    }
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Layers_FillOverGlass_FrontBehindTheCanoe_AndNoAllocationAtRest()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Reopen(boot, new MemoryProgressStore(59, 58, 58), 59);
            RectTransform fill = map.FillLayer!;
            Assert.That(fill.parent, Is.SameAs(map.StaticCanvas.transform), "el lleno, en el lienzo estático");
            Assert.That(fill.GetSiblingIndex(), Is.GreaterThan(map.Glass(0).transform.parent.GetSiblingIndex()), "encima del vidrio");
            RectTransform front = map.FrontLayer!;
            Assert.That(front.parent, Is.SameAs(map.DynamicCanvas.transform), "el frente, en el lienzo dinámico");
            Assert.That(front.GetSiblingIndex(), Is.LessThan(map.Canoe.transform.GetSiblingIndex()), "detrás de la canoa");
            Assert.That(map.BuoysAheadLayer!.GetSiblingIndex(), Is.GreaterThan(map.Canoe.transform.GetSiblingIndex()), "las boyas por ganar, encima de la canoa");
            Assert.That(map.RosaLayer!.GetSiblingIndex(), Is.GreaterThan(map.BuoysAheadLayer.GetSiblingIndex()), "Rosa, encima de todo");
            Assert.That(map.Canvas.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(3), "sin lienzos nuevos");

            // Las orlas (del lleno y del frente) en el lienzo estático, debajo del agua de todas las líneas: en una confluencia, el
            // afluente entra en el cauce sin costura.
            RectTransform casing = map.FillCasingLayer!;
            Assert.That(casing.parent, Is.SameAs(fill));
            Assert.That(casing.GetSiblingIndex(), Is.LessThan(map.FillRibbon(0).transform.parent.GetSiblingIndex()), "las orlas, debajo del agua");
            Assert.That(map.FrontCasingRibbon(0).transform.parent, Is.SameAs(casing), "la orla del frente, con las del lleno");
            Assert.That(map.FrontCasingRibbon(0).Part, Is.EqualTo(RibbonPart.Casing));
            Assert.That(map.FrontRibbon(0).Part, Is.EqualTo(RibbonPart.Core));

            yield return SnapshotKit.Wait(0.5f);
            map.Tick(16f);
            Assert.That(() => map.Tick(16f), Is.Not.AllocatingGCMemory(), "el Mapa quieto, con el río lleno");
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(RiverMapScreen.RestRenderInterval), "en reposo");
        }

        [UnityTest]
        public IEnumerator TheFill_ReadsOverTheGhostGlass_WithItsGlassEdge()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            Reopen(boot, new MemoryProgressStore(201, 200, 200), 200);
            map.SetView(MapView.Basin);
            Canvas.ForceUpdateCanvases();

            // El vidrio vacío, fantasma (0,40 en el Mapa; el color base de 07 §2.1 y de la cinta no cambia).
            for (int i = 0; i < data.Lines.Count; i++)
            {
                Assert.That(map.Glass(i).GlassBodyAlpha, Is.EqualTo(RiverMapScreen.MapGlassBodyAlpha), data.Lines[i].Id);
            }

            Assert.That(RiverRibbonGraphic.GlassBody.a, Is.EqualTo(0.85f).Within(1e-6f));

            // El lleno: el agua encima y su orla debajo, con el mismo intervalo.
            foreach (string id in new[] { "tapajos", "negro", "amazonas", "xingu" })
            {
                int line = data.LineIndexOf(id);
                RiverRibbonGraphic core = map.FillRibbon(line);
                RiverRibbonGraphic casing = map.FillCasingRibbon(line);
                Assert.That(core.Part, Is.EqualTo(RibbonPart.Core), id);
                Assert.That(casing.Part, Is.EqualTo(RibbonPart.Casing), id);
                Assert.That(casing.gameObject.activeSelf, Is.EqualTo(core.gameObject.activeSelf), id);
                Assert.That(casing.FillStart, Is.EqualTo(core.FillStart), id);
                Assert.That(casing.FillEnd, Is.EqualTo(core.FillEnd), id);
                Assert.That(casing.WidthDp, Is.EqualTo(core.WidthDp), id + ": la orla, por fuera del mismo ancho del agua (el del cauce)");
                Assert.That(casing.LastVertexCount, Is.GreaterThan(0), id);
            }

            // El canto de vidrio separa el lleno de la selva con al menos 3:1 (el agua clara sola no llega a 1,5:1), y es de 1,5 dp
            // (4,5 px a 3×): se ve.
            foreach (Color ground in new[] { RiverMapScreen.GroundColor, LandGraphic.CanopyColor, LandGraphic.VarzeaColor })
            {
                float rim = ColorMath.ContrastRatio(Palette.GlassRim, ground);
                Debug.Log("AF_MAP contraste del canto del lleno con " + ColorUtility.ToHtmlStringRGB(ground) + ": "
                    + rim.ToString("0.00", CultureInfo.InvariantCulture) + ":1; el agua clara sola: "
                    + ColorMath.ContrastRatio(Palette.WaterClear, ground).ToString("0.00", CultureInfo.InvariantCulture) + ":1");
                Assert.That(rim, Is.GreaterThanOrEqualTo(3f), ColorUtility.ToHtmlStringRGB(ground));
            }

            Assert.That(RiverRibbonGraphic.CasingRimDp, Is.GreaterThanOrEqualTo(1.5f));
            Assert.That(RiverRibbonGraphic.CasingFilletDp, Is.EqualTo(1f));
            Assert.That(RiverRibbonGraphic.FilletColor.a, Is.EqualTo(0.45f).Within(1e-6f));
            yield return null;
        }

        // ============================================================================================ ayudas

        // Largo real de la ruta de un tramo, en unidades del marco.
        private static float LegLength(RiverMap data, RiverStopRoute route)
        {
            float total = 0f;
            for (int i = 0; i < route.Path.Count; i++)
            {
                total += Mathf.Abs(route.Path[i].S1 - route.Path[i].S0) * data.Lines[route.Path[i].Line].Length;
            }

            return total;
        }

        // Largo de los tramos de una ruta que remontan (s baja), en unidades del marco.
        private static float UpstreamLength(RiverMap data, RiverStopRoute route)
        {
            float total = 0f;
            for (int i = 0; i < route.Path.Count; i++)
            {
                RiverPathPiece piece = route.Path[i];
                if (piece.S1 < piece.S0)
                {
                    total += (piece.S0 - piece.S1) * data.Lines[piece.Line].Length;
                }
            }

            return total;
        }

        // Arco del frente con max_level «max» (de 0 a LegFill de su ruta) que no queda dentro del lleno, en unidades del marco: el
        // agua de la victoria que se ve. «tipUpstream»: la punta del agua va por un tramo con largo que remonta (s baja).
        private static float VisibleFront(RiverMap data, RiverRoutes routes, int max, float[] from, out bool tipUpstream)
        {
            tipUpstream = false;
            RiverMapScreen.ComputeFill(data, routes, max, from);
            RiverStopRoute? route = routes.At(RiverMapScreen.FrontStopOf(max));
            if (route == null)
            {
                return 0f;
            }

            float left = RiverJourney.LegFill(max) * LegLength(data, route);
            float visible = 0f;
            for (int i = 0; i < route.Path.Count && left > 0f; i++)
            {
                RiverPathPiece piece = route.Path[i];
                float lineLength = data.Lines[piece.Line].Length;
                float length = Mathf.Abs(piece.S1 - piece.S0) * lineLength;
                if (length <= 0f)
                {
                    continue;
                }

                float t = Mathf.Clamp01(left / length);
                left -= length;
                float end = piece.S0 + ((piece.S1 - piece.S0) * t);
                float lo = Mathf.Min(piece.S0, end);
                float hi = Mathf.Max(piece.S0, end);
                visible += Mathf.Max(0f, Mathf.Min(hi, from[piece.Line]) - lo) * lineLength; // el lleno va de from a la boca
                tipUpstream = piece.S1 < piece.S0;
            }

            return visible;
        }

        // Abre la app en el inicio con «Jugar · Nivel playLevel» (max_level = playLevel − 1, todo visto) y el Mapa desde la pestaña.
        private static IEnumerator OpenApp(int playLevel)
        {
            if (!GameScreen.SimulatedScreen.HasValue)
            {
                GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // un teléfono de 360 × 780 dp
            }

            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = playLevel;
            profile.Progress.MaxLevel = playLevel - 1;
            profile.Progress.MapSeenLevel = playLevel - 1;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True, "el Mapa se abre desde el inicio");
            yield return null;
        }

        // Vuelve a abrir el Mapa de la escena con otro progreso.
        private static void Reopen(GameBootstrap boot, IMapProgress progress, int playLevel)
        {
            RiverMapScreen map = boot.RiverMapView!;
            map.Close();
            map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, progress, playLevel, playLevel <= 200);
        }

        private static float[] Snapshot(RiverMapScreen map, RiverMap data)
        {
            var values = new float[data.Lines.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = map.FillFrom(i);
            }

            return values;
        }

        // Las paradas 0-19 del catálogo y una gran parada del mundo 6 en el Napo (Mazán, k = 20, como la de M3.7a), con su ruta
        // desde Pacaya-Samiria: Marañón abajo, el cauce y el Napo arriba.
        private static void BuildWithWorld6(GameBootstrap boot, out RiverStopCatalog stops, out RiverRoutes routes)
        {
            RiverStopCatalog real = boot.RiverStops!;
            RiverRoutes realRoutes = boot.RiverRoutes!;
            RiverMap data = boot.RiverMapData!;
            var list = new List<RiverStop>();
            var routeList = new List<RiverStopRoute>();
            for (int n = 0; n < 20; n++)
            {
                list.Add(real.At(n)!);
                routeList.Add(realRoutes.At(n)!);
            }

            list.Add(new RiverStop(20, 6, "mazan", StopKind.Gate, "PE", null, null, 0, "white", null, null, new string[0], null, false,
                new RiverStopBackdrop("bg_kit_w06", null, null, null)));
            int maranon = data.LineIndexOf("maranon");
            int amazonas = data.LineIndexOf("amazonas");
            int napo = data.LineIndexOf("napo");
            routeList.Add(new RiverStopRoute(20, "mazan", new RiverMapPlace(napo, 0.946947f),
                new[]
                {
                    new RiverPathPiece(maranon, realRoutes.At(19)!.At.S, 1f),
                    new RiverPathPiece(amazonas, data.Lines[maranon].Mouth!.Value.S, data.Lines[napo].Mouth!.Value.S),
                    new RiverPathPiece(napo, 1f, 0.946947f),
                },
                new string[0], new int[0]));
            stops = new RiverStopCatalog("test", list.ToArray());
            routes = new RiverRoutes(routeList.ToArray());
        }

        // Hueco (dp) entre una caja local del contenido y el borde de la cinta de clase 1-2 más cercana, muestreando las líneas.
        private static float SampledClearance(RiverMap data, MapSpace space, MapView view, Rect box)
        {
            float best = float.MaxValue;
            for (int l = 0; l < data.Lines.Count; l++)
            {
                RiverLine line = data.Lines[l];
                if (line.Class > 2)
                {
                    continue;
                }

                float half = (RiverMapScreen.RibbonWidthDp(view, line.Class) * 0.5f) + RiverRibbonGraphic.CasingDp; // con la orla del lleno
                for (int i = 1; i < line.PointCount; i++)
                {
                    Vector2 a = space.ToLocal(line.X(i - 1), line.Y(i - 1));
                    Vector2 b = space.ToLocal(line.X(i), line.Y(i));
                    int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.5f));
                    for (int k = 0; k <= n; k++)
                    {
                        Vector2 p = Vector2.Lerp(a, b, k / (float)n);
                        float dx = Mathf.Max(0f, Mathf.Max(box.xMin - p.x, p.x - box.xMax));
                        float dy = Mathf.Max(0f, Mathf.Max(box.yMin - p.y, p.y - box.yMax));
                        best = Mathf.Min(best, Mathf.Sqrt((dx * dx) + (dy * dy)) - half);
                    }
                }
            }

            return best;
        }

        // Caja de rt en dp del marco del Mapa (padre común de la zona del mapa y de los rótulos fijos), y hacia abajo.
        private static Rect FrameRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            RectTransform frame = (RectTransform)GameBootstrap.Current!.RiverMapView!.ViewportRect.parent;
            Vector2 min = frame.InverseTransformPoint(corners[0]);
            Vector2 max = frame.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, -max.y, max.x, -min.y);
        }

        private static Rect Shrunk(Rect r)
        {
            return Rect.MinMaxRect(r.xMin + 0.5f, r.yMin + 0.5f, r.xMax - 0.5f, r.yMax - 0.5f);
        }
    }
}
