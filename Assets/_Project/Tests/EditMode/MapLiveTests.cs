#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El Mapa del río v2, bloque M2-1 «El río vivo» (D-069; <c>docs/dev/propuesta-mapa-rio-v2.md</c> §3.1, §7.2, §7.3 y §9), sin
    /// escena:
    /// <list type="bullet">
    /// <item>el lector de <c>af-river-map/2</c> y <c>af-river-stops/2</c> con los archivos reales (zonas, várzea, <c>shade</c>,
    /// orillas y vegetación) y los fallos de los campos nuevos; <c>/1</c> ya no se lee (P7 (a));</item>
    /// <item>la geometría de las cintas nuevas: las orillas por fuera del agua en su lado con su ancho y su rampa, la várzea y el agua
    /// con el canal profundo;</item>
    /// <item>las zonas de la tierra con su borde difuminado y el respaldo de la 1.0 (solo el dosel);</item>
    /// <item>la selva: densidad por zoom, fuera del agua, determinista, con tope y limitada a la región;</item>
    /// <item>el arte del Mapa v2, dentro del tope de memoria de §7.2 y soltado al cerrar;</item>
    /// <item>Reducir movimiento (§7.3): la corriente, el reloj del ambiente y el parallax quietos.</item>
    /// </list>
    /// </summary>
    public sealed class MapLiveTests
    {
        private static readonly MethodInfo RibbonPopulate = typeof(RiverRibbonGraphic).GetMethod("OnPopulateMesh",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;

        private static readonly MethodInfo LandPopulate = typeof(LandGraphic).GetMethod("OnPopulateMesh",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;

        private static readonly MethodInfo CanopyPopulate = typeof(CanopyGraphic).GetMethod("OnPopulateMesh",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;

        private readonly List<GameObject> _objects = new List<GameObject>();
        private RiverMap _map = null!;

        [SetUp]
        public void SetUp()
        {
            _map = RiverMapReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Map/river_map_v1.json")));
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

        // ---------------------------------------------------------------------------------------------- datos /2

        [Test]
        public void RealMap_IsV2_WithZonesBanksAndVegetation()
        {
            Assert.That(RiverMap.Format, Is.EqualTo("af-river-map/2"), "contratos 29 y 30 en /2 (D-069, P7 (a))");
            Assert.That(RiverStopCatalog.Format, Is.EqualTo("af-river-stops/2"));
            RiverLand land = _map.Land;
            Assert.That(land.Zones[0].Id, Is.EqualTo("canopy"), "la tierra de la 1.0 va primero");
            Assert.That(land.Zone("savanna"), Is.Not.Null, "los Llanos de Moxos (mundo 8)");
            Assert.That(land.Zone("rock"), Is.Not.Null, "los Andes");
            Assert.That(land.Zone("snow"), Is.Not.Null);
            RiverLandZone varzea = land.Zone("varzea")!;
            Assert.That(varzea.IsBand, Is.True, "la várzea es una banda junto al río, sin triángulos");
            Assert.That(varzea.BandDp, Is.EqualTo(40f), "propuesta §9.1: 40 dp [P]");
            Assert.That(land.Shade.Length, Is.EqualTo(land.VertexCount), "un peso por vértice");
            RiverLandZone canopy = land.Zones[0];
            for (int t = canopy.First * 3; t < (canopy.First + canopy.Count) * 3; t++)
            {
                Assert.That(land.ShadeOf(land.Triangles[t]), Is.EqualTo(1f), "el dosel, sin difuminar");
            }

            Assert.That(_map.Banks.Count, Is.InRange(150, 400), "los meandros con playa y barranco");
            int beaches = 0;
            for (int i = 0; i < _map.Banks.Count; i++)
            {
                RiverBank bank = _map.Banks.At(i);
                Assert.That(bank.S1, Is.GreaterThan(bank.S0));
                Assert.That(bank.WidthDp, Is.InRange(bank.Kind == RiverBankKind.Beach ? 6f : 4f, bank.Kind == RiverBankKind.Beach ? 16f : 8f),
                    "playa ≤ 16 dp y barranco ≤ 8 dp (propuesta §3.1, capa 2)");
                beaches += bank.Kind == RiverBankKind.Beach ? 1 : 0;
            }

            Assert.That(beaches * 2, Is.EqualTo(_map.Banks.Count), "cada curva, su playa y su barranco");
            RiverVegetation veg = _map.Vegetation;
            Assert.That(veg.Count, Is.GreaterThan(5000));
            Assert.That(veg.CellKm, Is.EqualTo(30f));
            Assert.That(veg.KmPerUnit, Is.EqualTo(2779.9f).Within(1f), "25° de latitud del marco");
            int lod0 = 0;
            for (int i = 0; i < veg.Count; i++)
            {
                lod0 += veg.Lod(i) == 0 ? 1 : 0;
                Assert.That(veg.Size(i), Is.InRange(0, 2));
                Assert.That(veg.Xy[2 * i], Is.InRange(0f, 1f));
            }

            Assert.That(lod0 / (float)veg.Count, Is.InRange(0.15f, 0.35f), "la capa gruesa es una celda de cada cuatro");
        }

        [Test]
        public void Reader_RejectsTheOldFormat_AndBrokenNewFields()
        {
            Assert.That(() => RiverMapReader.Read(RiverMapReaderTests.Doc(format: "af-river-map/1")),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("format"), "P7 (a): leídos solo en /2");
            RiverMap ok = RiverMapReader.Read(RiverMapReaderTests.Doc());
            Assert.That(ok.Banks.Count, Is.EqualTo(2));
            Assert.That(ok.Banks.At(0).Left, Is.True);
            Assert.That(ok.Banks.At(1).Kind, Is.EqualTo(RiverBankKind.Bluff));
            Assert.That(ok.Vegetation.Count, Is.EqualTo(2));
            Assert.That(ok.Vegetation.Kind(1), Is.EqualTo(RiverVegKind.Palm));
            Assert.That(ok.Vegetation.Lod(1), Is.EqualTo(1));
            Assert.That(ok.Vegetation.Tone(1), Is.EqualTo(3));
            Assert.That(ok.Vegetation.DistanceKm(1), Is.EqualTo(10f));
            Assert.That(ok.Vegetation.Size(0), Is.EqualTo(2));

            string bankL = "{\"line\": \"amazonas\", \"s0\": 0.1, \"s1\": 0.2, \"side\": \"left\", \"kind\": \"beach\", \"w\": 10}";
            Expect(RiverMapReaderTests.Doc(banks: RiverMapReaderTests.Banks(bankL.Replace("left", "up"))), "banks[0].side");
            Expect(RiverMapReaderTests.Doc(banks: RiverMapReaderTests.Banks(bankL.Replace("beach", "reef"))), "banks[0].kind");
            Expect(RiverMapReaderTests.Doc(banks: RiverMapReaderTests.Banks(bankL.Replace("\"s1\": 0.2", "\"s1\": 0.05"))), "banks[0]");
            Expect(RiverMapReaderTests.Doc(banks: RiverMapReaderTests.Banks(bankL.Replace("amazonas", "orinoco"))), "banks[0].line");
            Expect(RiverMapReaderTests.Doc(banks: RiverMapReaderTests.Banks(bankL.Replace("\"w\": 10", "\"w\": 90"))), "banks[0].w");
            Expect(RiverMapReaderTests.Doc(banks: RiverMapReaderTests.Banks(bankL + ", " + bankL.Replace("0.1", "0.05"))), "banks[1]");
            Expect(RiverMapReaderTests.Doc(veg: RiverMapReaderTests.Veg(info: Convert.ToBase64String(new byte[] { 0, 1 }))), "veg.info");
            Expect(RiverMapReaderTests.Doc(veg: RiverMapReaderTests.Veg(info: Convert.ToBase64String(new byte[] { 3, 1, 0, 1 }))), "veg.info");
            Expect(RiverMapReaderTests.Doc(veg: RiverMapReaderTests.Veg(cell: "0")), "veg.cell");
            Expect(RiverMapReaderTests.Doc(land: RiverMapReaderTests.Land(shade: Convert.ToBase64String(new byte[] { 255 }))), "land.shade");
            Expect(RiverMapReaderTests.Doc(land: RiverMapReaderTests.Land(zones:
                "[{\"id\": \"canopy\", \"tile\": \"canopy\", \"first\": 0, \"count\": 2}, {\"id\": \"varzea\", \"tile\": \"varzea\", \"band\": 40, \"first\": 0}]")),
                "land.zones[1]");
            Expect(RiverMapReaderTests.Doc(land: RiverMapReaderTests.Land(zones:
                "[{\"id\": \"canopy\", \"tile\": \"canopy\", \"first\": 0, \"count\": 2}, {\"id\": \"varzea\", \"tile\": \"varzea\", \"band\": 0}]")),
                "land.zones[1].band");
        }

        private static void Expect(string doc, string where)
        {
            Assert.That(() => RiverMapReader.Read(doc), Throws.TypeOf<LevelFormatException>().With.Message.Contains(where + ":"), where);
        }

        // ---------------------------------------------------------------------------------------------- cintas

        [Test]
        public void Banks_GoOutsideTheWater_OnTheirSide_WithTheirWidthAndRamp()
        {
            RiverMap map = Straight("{\"line\": \"amazonas\", \"s0\": 0.2, \"s1\": 0.8, \"side\": \"left\", \"kind\": \"beach\", \"w\": 12}");
            RiverRibbonGraphic ribbon = Ribbon(map, RibbonPart.Banks, 20f);
            List<UIVertex> mesh = Mesh(ribbon);
            Assert.That(mesh.Count % 4, Is.EqualTo(0), "dos bandas de dos vértices por sección");
            float half = 10f;
            float center = ribbon.Space.ToLocal(0f, map.Lines[0].Y(0)).y;
            float maxLeft = 0f;
            float maxRight = 0f;
            bool sand = false;
            foreach (UIVertex v in mesh)
            {
                // La línea va de izquierda a derecha en pantalla: la orilla izquierda mirando río abajo queda arriba (y local > 0).
                float off = v.position.y - center;
                if (off > 0f)
                {
                    maxLeft = Mathf.Max(maxLeft, off);
                }
                else
                {
                    maxRight = Mathf.Max(maxRight, -off);
                }

                sand |= Close(v.color, RiverRibbonGraphic.BeachInner);
                Assert.That(Mathf.Abs(off), Is.GreaterThanOrEqualTo(half - 0.01f), "por fuera del agua");
            }

            Assert.That(maxLeft, Is.EqualTo(half + 12f).Within(0.6f), "la playa de 12 dp en la orilla izquierda, a su ancho en el centro del tramo");
            Assert.That(maxRight, Is.EqualTo(half + RiverRibbonGraphic.GrassLineDp).Within(0.05f), "en la otra, la línea de hierba");
            Assert.That(sand, Is.True, "la arena de la playa");

            // Fuera del tramo, solo hierba a los dos lados.
            ribbon.SetFill(0f, 0.15f);
            foreach (UIVertex v in Mesh(ribbon))
            {
                float off = Mathf.Abs(v.position.y - center);
                Assert.That(off, Is.LessThanOrEqualTo(half + RiverRibbonGraphic.GrassLineDp + 0.01f));
            }
        }

        [Test]
        public void Varzea_IsABandThatFadesOutwards_AndLiveWater_HasADeepChannel()
        {
            RiverMap map = Straight(null);
            RiverRibbonGraphic varzea = Ribbon(map, RibbonPart.Varzea, 20f);
            varzea.SetBand(40f);
            float center = varzea.Space.ToLocal(0f, map.Lines[0].Y(0)).y;
            float reach = 0f;
            foreach (UIVertex v in Mesh(varzea))
            {
                float off = Mathf.Abs(v.position.y - center);
                reach = Mathf.Max(reach, off);
                if (off > 10f + 39f)
                {
                    Assert.That(v.color.a, Is.EqualTo(0), "se apaga hacia fuera");
                }
            }

            Assert.That(reach, Is.EqualTo(50f).Within(0.01f), "40 dp por fuera de los 10 del medio ancho");

            RiverRibbonGraphic core = Ribbon(map, RibbonPart.Core, 20f);
            core.SetLook(RibbonLook.Water);
            int legacy = Mesh(core).Count;
            core.SetLive(true);
            List<UIVertex> live = Mesh(core);
            Assert.That(live.Count, Is.GreaterThan(legacy), "dos mitades y el reflejo");
            Color white = Palette.WaterWhite;
            bool deep = false;
            bool edge = false;
            foreach (UIVertex v in live)
            {
                float off = Mathf.Abs(v.position.y - center);
                if (off < 0.01f)
                {
                    deep |= Close(v.color, new Color(white.r * RiverRibbonGraphic.DeepChannelFactor, white.g * RiverRibbonGraphic.DeepChannelFactor,
                        white.b * RiverRibbonGraphic.DeepChannelFactor, white.a));
                }
                else if (off > 9.99f)
                {
                    edge |= Close(v.color, white);
                }
            }

            Assert.That(deep, Is.True, "el canal del centro × 0,75 (propuesta §3.1, capa 3)");
            Assert.That(edge, Is.True, "las orillas del agua, con su color");
            Assert.That(RiverRibbonGraphic.SectionVertices(map.Lines[0], 0f, 1f, RibbonPart.Core, true),
                Is.EqualTo(6), "el presupuesto del frente cuenta las tres bandas");
        }

        [Test]
        public void ShapeWidth_KeepsTheCurveOfThe10_WhileTheRiverIsDrawnWider()
        {
            RiverLine line = _map.Lines[0];
            var space = new MapSpace(MapSpace.BasinWidthDp * 8f, Vector2.zero);
            RiverRibbonGraphic old = Make<RiverRibbonGraphic>();
            old.Setup();
            old.SetLine(line);
            old.SetSpace(space);
            old.SetSmooth(true);
            old.SetCurve(true);
            old.SetWidth(RiverMapScreen.WorldWidthClass1Dp);
            Mesh(old);
            int oldSections = old.LastSectionPointCount;
            RiverRibbonGraphic wide = Make<RiverRibbonGraphic>();
            wide.Setup();
            wide.SetLine(line);
            wide.SetSpace(space);
            wide.SetSmooth(true);
            wide.SetCurve(true);
            wide.SetWidth(RiverMapScreen.LiveWidthClass1Dp);
            wide.SetShapeWidth(RiverMapScreen.WorldWidthClass1Dp);
            Mesh(wide);
            Assert.That(wide.LastSectionPointCount, Is.GreaterThan(10));
            Assert.That(oldSections, Is.GreaterThan(10));
            // Cada sección de la cinta ancha cae sobre el trazo de la de 8 dp (la misma curva, muestreada quizá más fina).
            float worst = 0f;
            for (int i = 0; i < wide.LastSectionPointCount; i += 3)
            {
                Vector2 p = wide.LastSectionPoint(i);
                float best = float.MaxValue;
                for (int k = 1; k < oldSections; k++)
                {
                    best = Mathf.Min(best, SegmentDistance(p, old.LastSectionPoint(k - 1), old.LastSectionPoint(k)));
                }

                worst = Mathf.Max(worst, best * space.DpPerUnit);
            }

            Assert.That(worst, Is.LessThan(1f), "a menos de 1 dp del trazo de la 1.0: la canoa y las paradas no se mueven");

            Assert.That(RiverMapScreen.RibbonWidthDp(MapView.World, 1, true), Is.EqualTo(26f));
            Assert.That(RiverMapScreen.RibbonWidthDp(MapView.World, 2, true), Is.EqualTo(14f));
            Assert.That(RiverMapScreen.RibbonWidthDp(MapView.World, 3, true), Is.EqualTo(6f));
            Assert.That(RiverMapScreen.RibbonWidthDp(MapView.World, 1, false), Is.EqualTo(8f), "con ui_map_v2 apagada, la 1.0");
            Assert.That(RiverMapScreen.RibbonWidthDp(MapView.Basin, 1, true), Is.EqualTo(RiverMapScreen.RibbonWidthDp(MapView.Basin, 1)));
        }

        // ---------------------------------------------------------------------------------------------- tierra

        [Test]
        public void Land_Zones_FadeAtTheirEdge_AndTheFallbackDrawsOnlyTheCanopy()
        {
            LandGraphic land = Make<LandGraphic>();
            land.Setup();
            land.SetLand(_map.Land, new MapSpace(MapSpace.BasinWidthDp, Vector2.zero));
            land.SetLive(true);
            List<UIVertex> live = LandMesh(land);
            bool faded = false;
            bool savanna = false;
            foreach (UIVertex v in live)
            {
                faded |= v.color.a == 0;
                savanna |= Close(new Color32(v.color.r, v.color.g, v.color.b, 255), LandGraphic.SavannaLive);
            }

            Assert.That(faded, Is.True, "el borde de las manchas se difumina (land.shade)");
            Assert.That(savanna, Is.True, "la sabana amarilla de los Llanos de Moxos");

            land.SetLive(false);
            land.SetCanopyOnly(true);
            List<UIVertex> legacy = LandMesh(land);
            RiverLandZone canopy = _map.Land.Zones[0];
            Assert.That(legacy.Count, Is.LessThanOrEqualTo(canopy.Count * 3));
            foreach (UIVertex v in legacy)
            {
                Assert.That(Close(v.color, LandGraphic.CanopyColor), Is.True, "el respaldo: la tierra de la 1.0, de un color");
            }
        }

        // ---------------------------------------------------------------------------------------------- selva

        [Test]
        public void Canopy_Density_ByZoom()
        {
            CanopyGraphic.Density(0.30f, 30f, out int lod, out int cluster, out float spacing);
            Assert.That(lod, Is.EqualTo(0), "con 9 dp por celda, solo la capa gruesa");
            Assert.That(spacing, Is.EqualTo(18f).Within(0.1f));
            Assert.That(cluster, Is.EqualTo(1));
            CanopyGraphic.Density(0.47f, 30f, out lod, out cluster, out spacing);
            Assert.That(lod, Is.EqualTo(1), "a 4× (14 dp por celda), las dos capas");
            Assert.That(spacing, Is.EqualTo(14.1f).Within(0.1f));
            CanopyGraphic.Density(1.77f, 30f, out lod, out cluster, out spacing);
            Assert.That(lod, Is.EqualTo(1), "a 15×, las dos capas");
            Assert.That(cluster, Is.EqualTo(CanopyGraphic.MaxCluster));
        }

        [Test]
        public void Canopy_StaysOutOfTheWater_IsDeterministic_AndCapped()
        {
            var space = new MapSpace(MapSpace.BasinWidthDp * 6f, Vector2.zero);
            CanopyGraphic canopy = Make<CanopyGraphic>();
            canopy.Setup();
            canopy.SetData(_map.Vegetation, space, null);
            canopy.SetRiverClearance(29f);

            // Una vista de 360 × 640 dp alrededor de Manaos, con su margen.
            RiverLine amazonas = _map.Lines[0];
            amazonas.PointAt(0.62f, out float mx, out float my);
            float w = 360f / space.DpPerUnit;
            float h = 640f / space.DpPerUnit;
            canopy.SetRegion(new Rect(mx - w, my - h, 2f * w, 2f * h));
            List<UIVertex> a = CanopyMesh(canopy);
            List<UIVertex> b = CanopyMesh(canopy);
            Assert.That(canopy.LastQuadCount, Is.GreaterThan(50), "hay selva alrededor del río");
            Assert.That(canopy.LastQuadCount, Is.LessThanOrEqualTo(CanopyGraphic.MaxQuads));
            Assert.That(b.Count, Is.EqualTo(a.Count));
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(b[i].position, Is.EqualTo(a[i].position), "la misma selva cada vez (los puntos de la herramienta)");
            }

            // Ninguna copa con su centro sobre el cauce: lo que la cinta y su orilla tapan queda libre.
            float dpPerKm = space.DpPerUnit / _map.Vegetation.KmPerUnit;
            RiverVegetation veg = _map.Vegetation;
            for (int i = 0; i < veg.Count; i++)
            {
                Vector2 p = new Vector2(veg.Xy[2 * i], veg.Xy[(2 * i) + 1]);
                if (!canopy.Region.Contains(p))
                {
                    continue;
                }

                float distDp = veg.DistanceKm(i) * dpPerKm;
                Assert.That(distDp, Is.GreaterThanOrEqualTo(0f));
            }

            Vector2 river = space.ToLocal(mx, my);
            float clear = 13f + 2f;
            int onWater = 0;
            for (int q = 0; q < a.Count; q += 4)
            {
                Vector2 c = (Vector2)(a[q].position + a[q + 2].position) * 0.5f;
                onWater += Vector2.Distance(c, river) < clear ? 1 : 0;
            }

            Assert.That(onWater, Is.EqualTo(0), "ninguna copa sobre el punto del río");

            // Una región enorme no pasa del tope.
            canopy.SetRegion(new Rect(0f, 0f, 1f, _map.Height));
            CanopyMesh(canopy);
            Assert.That(canopy.LastQuadCount, Is.LessThanOrEqualTo(CanopyGraphic.MaxQuads));
            Assert.That(canopy.LastQuadCount, Is.GreaterThan(CanopyGraphic.MaxQuads / 2), "la cuenca entera, repartida hasta el tope (no cortada por abajo)");
        }

        // ---------------------------------------------------------------------------------------------- arte

        [Test]
        public void LiveArt_StaysWithinTheMemoryBudget_AndIsReleased()
        {
            var art = new MapLiveArt();
            art.Build();
            try
            {
                Assert.That(art.Built, Is.True);
                Assert.That(art.Atlas!.width, Is.EqualTo(MapLiveArt.AtlasPx));
                Assert.That(art.Atlas.mipmapCount, Is.GreaterThan(1), "las copas se pintan de 9 a 48 dp: con mipmaps");
                Assert.That(art.TextureBytes, Is.LessThanOrEqualTo(6L * 1024L * 1024L), "propuesta §7.2: +6 MiB como mucho");
                Assert.That(art.Piece(MapLivePiece.Buoy), Is.Not.Null);
                Assert.That(art.ForegroundLeft, Is.Not.Null);
                Assert.That(art.Andes, Is.Not.Null);
                Assert.That(MapLiveArt.Uv((int)MapLivePiece.Buoy).Overlaps(MapLiveArt.Uv((int)MapLivePiece.Glint)), Is.False);
            }
            finally
            {
                Texture2D? atlas = art.Atlas;
                art.Release();
                Assert.That(art.Built, Is.False);
                Assert.That(art.TextureBytes, Is.EqualTo(0));
                Assert.That(atlas == null, Is.True, "la textura se destruye (el == de Unity)");
            }
        }

        // ---------------------------------------------------------------------------------------------- Reducir movimiento

        [Test]
        public void ReduceMotion_KeepsTheFlowCloudsAndParallaxStill()
        {
            Assert.That(RiverMapScreen.AdvanceFlow(17f, 500f, true), Is.EqualTo(17f), "la corriente quieta (propuesta §7.3)");
            Assert.That(RiverMapScreen.AdvanceClock(1234f, 500f, true), Is.EqualTo(1234f), "el reloj de las nubes y los destellos, quieto");
            Assert.That(RiverMapScreen.ParallaxDrift(new Vector2(100f, -50f), true), Is.EqualTo(Vector2.zero), "sin parallax");

            Assert.That(RiverMapScreen.AdvanceFlow(17f, 500f, false), Is.EqualTo(17f + (RiverMapScreen.FlowSpeedDp * 0.5f)).Within(1e-4f));
            Assert.That(RiverMapScreen.AdvanceFlow(58f, 500f, false), Is.LessThan(RiverMapScreen.FlowStepDp), "da la vuelta cada chevron");
            Assert.That(RiverMapScreen.AdvanceClock(1234f, 500f, false), Is.EqualTo(1734f));
            Vector2 drift = RiverMapScreen.ParallaxDrift(new Vector2(100f, -1000f), false);
            Assert.That(drift.x, Is.EqualTo(15f).Within(1e-4f), "×1,15: el primer plano se mueve un 15 % más");
            Assert.That(drift.y, Is.EqualTo(-RiverMapScreen.ParallaxMaxDp), "con tope");
        }

        [Test]
        public void MapLiveConfig_DefaultsAreThoseOf08_9()
        {
            MapLiveConfig c = MapLiveConfig.Defaults;
            Assert.That(c.UiMapV2, Is.True, "ui_map_v2 encendida por defecto (D-069, P8 (a))");
            Assert.That(c.UiMapCrew, Is.True);
            Assert.That(c.UiMapTowns, Is.True);
        }

        [Test]
        public void WorldLight_HasOneLightPerWorld()
        {
            var seen = new HashSet<Color>();
            for (int w = 1; w <= 10; w++)
            {
                Color c = RiverMapScreen.WorldLight(w);
                Assert.That(c.a, Is.InRange(0.1f, 0.3f), "un velo, no un filtro que tape el río");
                seen.Add(c);
            }

            Assert.That(seen.Count, Is.EqualTo(10), "propuesta §3.3: cada mundo con su luz");
        }

        [Test]
        public void MapFramesProbe_Format()
        {
            string line = DevProbe.FormatMapFrames(3, "world", new[] { 10f, 16f, 17f, 33f }, 120, 640, 2100);
            Assert.That(line, Does.StartWith("AF_PERF {\"event\":\"map_frames\",\"world\":3,\"view\":\"world\",\"frames\":4,\"rest_frames\":120,"));
            Assert.That(line, Does.Contain("\"avg_ms\":19.00"));
            Assert.That(line, Does.Contain("\"p95_ms\":33.00"));
            Assert.That(line, Does.Contain("\"fps\":52.63"));
            Assert.That(line, Does.EndWith("\"veg_quads\":640,\"live_tex_kib\":2100}"));
        }

        // ---------------------------------------------------------------------------------------------- utilidades

        // Un Mapa de prueba con el cauce recto de izquierda a derecha a media altura (y las orillas que se den).
        private static RiverMap Straight(string? bank)
        {
            string pts = RiverMapReaderTests.EncodePoints(new ushort[] { 0, 20000, 16384, 20000, 32768, 20000, 49152, 20000, 65535, 20000 });
            string main = RiverMapReaderTests.Main(pts: pts, water: RiverMapReaderTests.Water("0.0", "1.0", "white"), splits: string.Empty);
            string doc = RiverMapReaderTests.Doc(lines: RiverMapReaderTests.Lines(main, RiverMapReaderTests.Trib()),
                banks: RiverMapReaderTests.Banks(bank ?? string.Empty), home: "{\"on\": \"amazonas\", \"s\": 0.9}",
                passing: "[{\"id\": \"gurupa\", \"line\": \"amazonas\", \"s\": 0.8, \"fact\": null}]");
            return RiverMapReader.Read(doc);
        }

        private RiverRibbonGraphic Ribbon(RiverMap map, RibbonPart part, float width)
        {
            RiverRibbonGraphic ribbon = Make<RiverRibbonGraphic>();
            ribbon.Setup();
            ribbon.SetLine(map.Lines[0]);
            ribbon.SetSpace(new MapSpace(1000f, Vector2.zero));
            ribbon.SetPart(part);
            ribbon.SetLook(RibbonLook.Glass);
            ribbon.SetWidth(width);
            ribbon.SetBanks(map.Banks);
            ribbon.SetFill(0f, 1f);
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
            return Populate(RibbonPopulate, ribbon);
        }

        private static List<UIVertex> LandMesh(LandGraphic land)
        {
            return Populate(LandPopulate, land);
        }

        private static List<UIVertex> CanopyMesh(CanopyGraphic canopy)
        {
            return Populate(CanopyPopulate, canopy);
        }

        private static List<UIVertex> Populate(MethodInfo populate, Graphic graphic)
        {
            var list = new List<UIVertex>();
            using (var vh = new VertexHelper())
            {
                populate.Invoke(graphic, new object[] { vh });
                var v = new UIVertex();
                for (int i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(ref v, i);
                    list.Add(v);
                }
            }

            return list;
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(p, a + (ab * t));
        }

        private static bool Close(Color32 a, Color b)
        {
            Color32 c = b;
            return Mathf.Abs(a.r - c.r) <= 2 && Mathf.Abs(a.g - c.g) <= 2 && Mathf.Abs(a.b - c.b) <= 2;
        }
    }
}
