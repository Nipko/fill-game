#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El Mapa del río v2, bloque M2-2 «Pueblos de paso» (D-069; <c>docs/dev/propuesta-mapa-rio-v2.md</c> §4, §7.2 y §9.5), sin
    /// escena:
    /// <list type="bullet">
    /// <item>los datos reales: los 30 puntos de paso con su viñeta, su país y su tier, los 12 pueblos nuevos que entran (P4 (a)),
    /// ninguno vetado (Belén y Boca Colorado), la vía con su fracción y la excursión de los 25 tramos de 0 km (P5 (a));</item>
    /// <item>el anclaje: cada pueblo está sobre su línea (su punto del río) y su tramo lo cruza;</item>
    /// <item>la cadencia (§4.1, regla 2): cada tramo largo pasa algún pueblo, salvo las excepciones anotadas en
    /// <c>tools/river-map/overrides.json</c>;</item>
    /// <item>cuándo se pasa un pueblo (§4.1, regla 3) y el nivel de detalle de los rótulos y de las viñetas (§9.5);</item>
    /// <item>el lazo de la excursión, que sale y vuelve a la parada; los fallos del lector; y el atlas de §7.2, que se suelta.</item>
    /// </list>
    /// </summary>
    public sealed class MapTownsTests
    {
        // Los tramos sin el pueblo que pide la cadencia, anotados en towns.cadence de overrides.json (con su motivo).
        private static readonly int[] CadenceExceptions = { 18, 21, 22, 25, 27, 38, 42, 45, 51, 53, 59, 61, 64 };

        private static readonly string[] NewTowns =
        {
            "almeirim", "prainha", "manacapuru", "codajas", "amatura", "sao_paulo_de_olivenca", "indiana", "orellana", "humaita",
            "manicore", "cachuela_esperanza", "guajara_mirim",
        };

        private RiverMap _map = null!;
        private RiverRoutes _routes = null!;

        [SetUp]
        public void SetUp()
        {
            _map = RiverMapReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Map/river_map_v1.json")));
            _routes = RiverMapReader.ReadRoutes(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Map/river_stops_v1.json")), _map);
        }

        [Test]
        public void RealData_ThirtyPassingPoints_WithVignetteCountryAndTier()
        {
            Assert.That(_map.Passing.Count, Is.EqualTo(30), "los 18 de la 1.0 y los 12 nuevos (P4 (a))");
            int towns = 0;
            foreach (RiverPassingPoint p in _map.Passing)
            {
                Assert.That(p.Country.Length, Is.EqualTo(2), p.Id);
                Assert.That(p.IsTown, Is.EqualTo(p.Tier != RiverTownTier.Mark), p.Id);
                towns += p.IsTown ? 1 : 0;
            }

            Assert.That(towns, Is.EqualTo(25));
            foreach (string id in NewTowns)
            {
                RiverPassingPoint? p = _map.PassingPoint(id);
                Assert.That(p, Is.Not.Null, id);
                Assert.That(p!.IsTown, Is.True, id);
                Assert.That(p.Fact, Is.Null, id + ": sin dato hasta que sea [V] y lo revise Gemini (D-054)");
            }

            Assert.That(_map.PassingPoint("parintins")!.Vignette, Is.EqualTo(RiverTownVignette.Port), "Parintins, el puerto, sin bois");
            Assert.That(_map.PassingPoint("parintins")!.Tier, Is.EqualTo(RiverTownTier.City));
            Assert.That(_map.PassingPoint("teotonio")!.Vignette, Is.EqualTo(RiverTownVignette.None), "los rápidos: el punto de la 1.0");
            Assert.That(_map.PassingPoint("cachuela_esperanza")!.CountryNameKey, Is.EqualTo("map.country.bo.name"));
            Assert.That(_map.PassingPoint("coari")!.RiverKey, Is.EqualTo("map.stop.coari.river"));
        }

        [Test]
        public void RealData_Vetoes_NoBelenNoBocaColoradoNoExcluded()
        {
            string[] never = { "belen", "boca_colorado", "porto_velho", "borba", "costa_marques", "masisea", "laberinto", "sao_gabriel_da_cachoeira" };
            foreach (RiverPassingPoint p in _map.Passing)
            {
                Assert.That(p.Id, Does.Not.Contain("belen"));
                Assert.That(p.Id, Does.Not.Contain("colorado"));
                Assert.That(Array.IndexOf(never, p.Id), Is.EqualTo(-1), p.Id);
            }
        }

        [Test]
        public void Anchoring_EachTownSitsOnItsLine_AndItsLegsCrossIt()
        {
            foreach (string id in NewTowns)
            {
                RiverPassingPoint p = _map.PassingPoint(id)!;
                bool crossed = false;
                for (int n = 1; n < _routes.Count && !crossed; n++)
                {
                    RiverStopRoute r = _routes.At(n)!;
                    for (int k = 0; k < r.Path.Count && !crossed; k++)
                    {
                        RiverPathPiece piece = r.Path[k];
                        float lo = Mathf.Min(piece.S0, piece.S1) - 2e-6f;
                        float hi = Mathf.Max(piece.S0, piece.S1) + 2e-6f;
                        if (piece.Line == p.At.Line && p.At.S >= lo && p.At.S <= hi)
                        {
                            crossed = Array.IndexOf(ToArray(r.Via), id) >= 0;
                        }
                    }
                }

                Assert.That(crossed, Is.True, id + ": un tramo pasa por su punto del río y lo lleva en su vía");
            }
        }

        [Test]
        public void Cadence_EveryLongLegPassesATown_SaveTheAnnotatedExceptions()
        {
            double kmPerUnit = (_map.Frame.LatMax - _map.Frame.LatMin) * Math.PI / 180.0 * RiverMapReader.EarthKm;
            var missing = new List<int>();
            for (int n = 1; n < _routes.Count; n++)
            {
                RiverStopRoute r = _routes.At(n)!;
                double km = 0.0;
                for (int k = 0; k < r.Path.Count; k++)
                {
                    km += Math.Abs(r.Path[k].S1 - r.Path[k].S0) * _map.Lines[r.Path[k].Line].Length * kmPerUnit;
                }

                int towns = 0;
                for (int v = 0; v < r.Via.Count; v++)
                {
                    towns += _map.PassingPoint(r.Via[v])!.IsTown ? 1 : 0;
                }

                // La herramienta mide en km por la línea; aquí, en el plano del marco: un 8 % de margen para los tramos de ~150 km.
                if (km >= 162.0 && towns == 0 && Array.IndexOf(CadenceExceptions, n) < 0)
                {
                    missing.Add(n);
                }
            }

            Assert.That(missing, Is.Empty, "tramos de 150 km o más sin pueblo ni excepción (propuesta §4.1, regla 2)");
        }

        [Test]
        public void Via_CarriesItsFraction_InOrder()
        {
            RiverStopRoute santarem = _routes.At(1)!;
            Assert.That(ToArray(santarem.Via), Is.EqualTo(new[] { "breves", "gurupa", "boca_del_xingu", "almeirim", "prainha", "monte_alegre" }));
            for (int n = 1; n < _routes.Count; n++)
            {
                RiverStopRoute r = _routes.At(n)!;
                float last = 0f;
                for (int v = 0; v < r.Via.Count; v++)
                {
                    Assert.That(r.ViaFraction(v), Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
                    Assert.That(r.ViaFraction(v), Is.GreaterThanOrEqualTo(last));
                    last = r.ViaFraction(v);
                }
            }
        }

        [Test]
        public void PassLevel_FollowsTheFractionOfTheLeg()
        {
            Assert.That(RiverTowns.PassLevel(1, 0.2978f), Is.EqualTo(3), "Breves, en el 3");
            Assert.That(RiverTowns.PassLevel(1, 0.6133f), Is.EqualTo(7), "Almeirim, en el 7");
            Assert.That(RiverTowns.PassLevel(7, 0.3176f), Is.EqualTo(64), "Manacapuru, en el 64");
            Assert.That(RiverTowns.PassLevel(8, 0.9942f), Is.EqualTo(80), "Tabatinga, con la llegada a Leticia");
            Assert.That(RiverTowns.PassLevel(3, 0.0001f), Is.EqualTo(21), "al menos el primero del tramo");
            Assert.That(RiverTowns.Passed(7, 0.3176f, 63), Is.False);
            Assert.That(RiverTowns.Passed(7, 0.3176f, 64), Is.True);
        }

        [Test]
        public void LabelLod_OnlyTownsWithinOneLegOfTheCanoe_AndTheLastAndNextStops()
        {
            // max 57: la canoa va por el tramo de la parada 6 (51-60).
            Assert.That(RiverTowns.CanoeLeg(57), Is.EqualTo(6));
            Assert.That(RiverTowns.CanoeLeg(60), Is.EqualTo(6), "justo al llegar, el tramo recién hecho");
            Assert.That(RiverTowns.CanoeLeg(0), Is.EqualTo(0));
            Assert.That(RiverTowns.TownLabelShown(5, 57), Is.True);
            Assert.That(RiverTowns.TownLabelShown(6, 57), Is.True);
            Assert.That(RiverTowns.TownLabelShown(7, 57), Is.True, "Tefé: Manacapuru y Coari, el próximo pueblo");
            Assert.That(RiverTowns.TownLabelShown(4, 57), Is.False, "el Encuentro: solo su viñeta");
            Assert.That(RiverTowns.TownLabelShown(8, 57), Is.False);
            Assert.That(RiverTowns.StopLabelShown(5, 57), Is.True, "la última alcanzada");
            Assert.That(RiverTowns.StopLabelShown(6, 57), Is.True, "la siguiente");
            Assert.That(RiverTowns.StopLabelShown(4, 57), Is.False);
            Assert.That(RiverTowns.StopLabelShown(7, 57), Is.False);
        }

        [Test]
        public void VignetteLod_GrowsWithTheZoom()
        {
            Assert.That(RiverMapScreen.TownWidthDp(RiverMapScreen.WorldZoomMin), Is.EqualTo(RiverMapScreen.TownMinDp));
            Assert.That(RiverMapScreen.TownWidthDp(RiverMapScreen.WorldZoomMax), Is.EqualTo(RiverMapScreen.TownMaxDp));
            Assert.That(RiverMapScreen.TownWidthDp(1f), Is.EqualTo(RiverMapScreen.TownMinDp));
            Assert.That(RiverMapScreen.TownWidthDp(9.5f), Is.InRange(RiverMapScreen.TownMinDp + 1f, RiverMapScreen.TownMaxDp - 1f));
        }

        [Test]
        public void Excursion_TheTwentyFiveZeroLegs_LoopOutAndBackToTheStop()
        {
            int count = 0;
            for (int n = 1; n < _routes.Count; n++)
            {
                RiverStopRoute r = _routes.At(n)!;
                bool zero = true;
                for (int k = 0; k < r.Path.Count; k++)
                {
                    zero &= r.Path[k].S0 == r.Path[k].S1;
                }

                Assert.That(r.ExcursionData != null, Is.EqualTo(zero), r.Id);
                Assert.That(r.Excursion, Is.Null, "sin ui_map_towns, la ruta de M2-1");
                count += zero ? 1 : 0;
            }

            Assert.That(count, Is.EqualTo(25), "P5 (a): los 25 tramos de 0 km");
            RiverRoutes towns = _routes.WithExcursions();
            RiverStopRoute leticia = towns.At(9)!;
            Assert.That(leticia.Excursion, Is.Not.Null);
            Vector2 stop = RiverMapScreen.PlacePoint(_map, leticia.At);
            Assert.That(Vector2.Distance(RiverMapScreen.RoutePoint(_map, leticia, 0f), stop), Is.LessThan(1e-5f), "sale de la parada");
            Assert.That(Vector2.Distance(RiverMapScreen.RoutePoint(_map, leticia, 1f), stop), Is.LessThan(1e-5f), "y vuelve a ella");
            Vector2 a = RiverMapScreen.RoutePoint(_map, leticia, 0.25f);
            Vector2 b = RiverMapScreen.RoutePoint(_map, leticia, 0.75f);
            Assert.That(Vector2.Distance(a, b), Is.GreaterThan(1e-5f), "la ida y la vuelta por orillas distintas: un lazo, sin boyas encima");
            Assert.That(RiverMapScreen.RoutePoint(_map, _routes.At(9)!, 0.5f), Is.EqualTo(stop), "sin la excursión, la parada (M2-1)");
            Assert.That(towns.At(10), Is.SameAs(_routes.At(10)), "un tramo con largo no cambia");
        }

        [Test]
        public void Reader_RejectsInvalidTownsAndExcursions()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Map/river_map_v1.json"));
            Assert.Throws<LevelFormatException>(() => RiverMapReader.Read(json.Replace("\"tier\": \"mark\"", "\"tier\": \"aldea\"")));
            Assert.Throws<LevelFormatException>(() => RiverMapReader.Read(json.Replace("\"vignette\": null, \"country\": \"BR\", \"tier\": \"mark\"",
                "\"vignette\": \"church\", \"country\": \"BR\", \"tier\": \"mark\"")));
            Assert.Throws<LevelFormatException>(() => RiverMapReader.Read(json.Replace("\"vignette\": \"market\"", "\"vignette\": \"maloca\"")));
            string stops = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Map/river_stops_v1.json"));
            Assert.Throws<LevelFormatException>(() => RiverMapReader.ReadRoutes(stops.Replace("{\"id\": \"breves\", \"f\": 0.2978}", "\"breves\""), _map));
            Assert.Throws<LevelFormatException>(() => RiverMapReader.ReadRoutes(stops.Replace("{\"id\": \"breves\", \"f\": 0.2978}", "{\"id\": \"breves\", \"f\": 0}"), _map));
            Assert.Throws<LevelFormatException>(() => RiverMapReader.ReadRoutes(stops.Replace("\"excursion\": {\"line\": \"amazonas\", \"s0\": 0.485554",
                "\"excursion\": {\"line\": \"amazonas\", \"s0\": 0.4"), _map));
        }

        [Test]
        public void TownArt_IsOneAtlasOf1024x512_AndIsReleased()
        {
            var art = new MapTownArt();
            try
            {
                art.Build();
                Assert.That(art.Atlas!.width, Is.EqualTo(1024));
                Assert.That(art.Atlas.height, Is.EqualTo(512));
                Assert.That(art.TextureBytes, Is.LessThanOrEqualTo(3L * 1024L * 1024L), "§7.2: 2 MiB y sus mipmaps");
                foreach (RiverTownVignette v in new[] { RiverTownVignette.Stilts, RiverTownVignette.Church, RiverTownVignette.Port, RiverTownVignette.Market, RiverTownVignette.Landmark })
                {
                    for (int variant = 0; variant < ProceduralArt.TownVariants; variant++)
                    {
                        Assert.That(art.Piece(v, variant), Is.Not.Null, v + " " + variant);
                    }
                }

                Assert.That(art.Piece(RiverTownVignette.None, 0), Is.Null);
            }
            finally
            {
                art.Release();
            }

            Assert.That(art.Built, Is.False);
            Assert.That(art.TextureBytes, Is.EqualTo(0L));
        }

        [Test]
        public void VariantOf_IsStableAcrossMachines()
        {
            Assert.That(RiverMapScreen.VariantOf("coari"), Is.EqualTo(RiverMapScreen.VariantOf("coari")));
            var seen = new HashSet<int>();
            foreach (string id in NewTowns)
            {
                seen.Add(RiverMapScreen.VariantOf(id));
            }

            Assert.That(seen.Count, Is.EqualTo(ProceduralArt.TownVariants), "los tres juegos de colores salen");
        }

        private static string[] ToArray(IReadOnlyList<string> list)
        {
            var a = new string[list.Count];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = list[i];
            }

            return a;
        }
    }
}
