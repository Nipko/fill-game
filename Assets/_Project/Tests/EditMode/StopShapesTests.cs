#nullable enable
using System.Collections.Generic;
using AF.Levels;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Siluetas propias de los sitios (tarea M2.4; 07 §2.1 y §8.1; propuesta del Mapa §10.2, §10.3 y §10.6; direccion-visual-v2
    /// §9, VA-20 y VA-22): ninguna parada pintada de 1-200 usa una forma genérica, cada héroe pintado y la reserva (la Casa de
    /// Fierro) tienen la suya, las dos paradas solo con el kit también y las dos líneas entre aguas no llevan silueta; todas caben
    /// en la caja de 240 × 80 dp (≤ 48 dp sobre el horizonte, lo reconocible en 30, ≤ 32 de orilla), entran por los bordes sin
    /// arista, no bajan de V 0,25 ni usan el turquesa caribeño, y cumplen las reglas de cada sitio que se pueden medir: el dosel
    /// parejo de la Floresta Nacional (sin samaúma), nada bajo el horizonte en Tefé (sin botos ni orillas secas), Puerto Nariño
    /// sin pueblo y la Casa de Fierro sin torre (sin el mito de Eiffel). <see cref="StopShapes.Draw"/> no asigna memoria.
    /// </summary>
    public sealed class StopShapesTests
    {
        private const float FootX = 180f;
        private const float Horizon = 118f;

        private GameObject? _go;

        private static RiverStopCatalog Stops => MapTestCatalogs.Real;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        private StopShapeGraphic NewGraphic()
        {
            _go = new GameObject("StopShapesTestGraphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(StopShapeGraphic));
            StopShapeGraphic g = _go.GetComponent<StopShapeGraphic>();
            g.Setup();
            return g;
        }

        private static void DrawPlain(StopShapeGraphic g, string shape, float k)
        {
            g.Clear();
            Assert.That(StopShapes.Draw(g, shape, FootX * k, Horizon * k, k, Color.white, null, Palette.WaterBase("white"),
                StopBackdrop.ReflectionAlpha, StopBackdrop.ReflectionDp), Is.True, shape);
        }

        [Test]
        public void NoPaintedStop_UsesAGenericShape_AndEachHeroHasItsOwn()
        {
            var shapeOfHero = new Dictionary<string, string>();
            var heroOfShape = new Dictionary<string, string>();
            for (int n = 0; n < Stops.Count; n++)
            {
                RiverStopBackdrop b = Stops.At(n)!.Backdrop;
                if (b.Hero == null)
                {
                    continue;
                }

                string? shape = StopShapes.ShapeOf(b);
                Assert.That(shape, Is.Not.Null, "parada " + n);
                Assert.That(StopShapes.IsKnown(shape), Is.True, "parada " + n);
                Assert.That(StopShapes.IsGeneric(shape), Is.False, "parada " + n + " (" + b.Hero + "): sin forma genérica");
                if (shapeOfHero.TryGetValue(b.Hero, out string? same))
                {
                    Assert.That(shape, Is.EqualTo(same), "el mismo héroe, la misma forma (parada " + n + ")");
                    continue;
                }

                Assert.That(heroOfShape.ContainsKey(shape!), Is.False, "parada " + n + ": " + shape + " ya es de otro héroe");
                shapeOfHero[b.Hero] = shape!;
                heroOfShape[shape!] = b.Hero;
            }

            Assert.That(shapeOfHero.Count, Is.EqualTo(14), "07 §2.1: 14 héroes pintados en 1-200");

            // La reserva de la parada 15 (Casa de Fierro), con su forma propia y sin coincidir con ninguna otra.
            string? reserve = StopShapes.ShapeOf(new RiverStopBackdrop("bg_kit_w05", "bg_hero_casa_de_fierro", null, null));
            Assert.That(reserve, Is.EqualTo(StopShapes.CasaDeFierro));
            Assert.That(heroOfShape.ContainsKey(reserve!), Is.False);
        }

        [Test]
        public void TheEighteenSites_KitStopsHaveTheirOwnShape_CodeStopsNone()
        {
            var sites = new HashSet<string>();
            int kit = 0;
            int code = 0;
            for (int n = 0; n < Stops.Count; n++)
            {
                RiverStopBackdrop b = Stops.At(n)!.Backdrop;
                sites.Add(b.Hero ?? b.Code!);
                if (b.Code == null)
                {
                    continue;
                }

                string? shape = StopShapes.ShapeOf(b);
                if (b.Code.StartsWith("kit:", System.StringComparison.Ordinal))
                {
                    kit++;
                    Assert.That(StopShapes.IsKnown(shape) && !StopShapes.IsGeneric(shape), Is.True, "parada " + n + ": forma propia del kit");
                }
                else
                {
                    code++;
                    Assert.That(shape, Is.Null, "parada " + n + ": la línea entre aguas ya es la pieza definitiva");
                }
            }

            Assert.That(sites.Count, Is.EqualTo(18), "07 §2.1: 18 sitios");
            Assert.That(kit, Is.EqualTo(2), "el río frontera y Nauta");
            Assert.That(code, Is.EqualTo(2), "Santarém y el Encuentro");
            Assert.That(StopShapes.ShapeOf(new RiverStopBackdrop("bg_kit_w13", null, "kit:rio_ancho_con_baliza", null)), Is.EqualTo(StopShapes.Beacon));
            Assert.That(StopShapes.ShapeOf(new RiverStopBackdrop("bg_kit_w05", null, "kit:orilla_de_nauta", null)), Is.EqualTo(StopShapes.Nauta));
        }

        [Test]
        public void TheSourceTable_GivesDistinctOwnShapes_AndAllListsThem()
        {
            Assert.That(StopShapes.SourceCount, Is.EqualTo(17), "14 héroes, la reserva y 2 piezas del kit");
            var seen = new HashSet<string>();
            for (int i = 0; i < StopShapes.SourceCount; i++)
            {
                string shape = StopShapes.SourceShapeAt(i);
                Assert.That(StopShapes.IsKnown(shape), Is.True, StopShapes.SourceAt(i));
                Assert.That(StopShapes.IsGeneric(shape), Is.False, StopShapes.SourceAt(i));
                Assert.That(seen.Add(shape), Is.True, StopShapes.SourceAt(i) + ": forma repetida");
                Assert.That(System.Array.IndexOf(StopShapes.All, shape), Is.GreaterThanOrEqualTo(0), shape);
            }

            Assert.That(StopShapes.All.Length, Is.EqualTo(StopShapes.SourceCount + StopShapes.Generic.Length));
            foreach (string generic in StopShapes.Generic)
            {
                Assert.That(StopShapes.IsGeneric(generic), Is.True);
                Assert.That(seen.Contains(generic), Is.False);
            }

            Assert.That(new HashSet<string>(StopShapes.All).Count, Is.EqualTo(StopShapes.All.Length), "ids distintos");
        }

        [Test]
        public void EveryShape_FitsThe240By80Box()
        {
            foreach (string shape in StopShapes.All)
            {
                Rect r = StopShapes.Bounds(shape);
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-StopBackdrop.HeroWidthDp * 0.5f), shape);
                Assert.That(r.xMax, Is.LessThanOrEqualTo(StopBackdrop.HeroWidthDp * 0.5f), shape);
                Assert.That(r.yMax, Is.LessThanOrEqualTo(StopBackdrop.HeroAboveDp), shape + ": ≤ 48 dp sobre el horizonte");
                Assert.That(r.yMax, Is.LessThanOrEqualTo(StopShapes.RecognizableDp), shape + ": entera en los 30 dp que se ven con 2 filas");
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(-StopBackdrop.HeroBelowDp), shape + ": ≤ 32 dp de orilla");
                Assert.That(r.height, Is.LessThanOrEqualTo(StopBackdrop.HeroHeightDp), shape);
                Assert.That(r.yMax, Is.GreaterThan(8f), shape + ": algo sobre el horizonte");
            }

            // Pintadas, a dos anchos: dentro de la caja del héroe, con el reflejo como mucho a 44 dp.
            StopShapeGraphic g = NewGraphic();
            foreach (float k in new[] { 1f, 1.2f })
            {
                foreach (string shape in StopShapes.All)
                {
                    DrawPlain(g, shape, k);
                    float half = StopBackdrop.HeroWidthDp * 0.5f * k;
                    Assert.That(g.MinX, Is.GreaterThanOrEqualTo((FootX * k) - half - 0.01f), shape);
                    Assert.That(g.MaxX, Is.LessThanOrEqualTo((FootX * k) + half + 0.01f), shape);
                    Assert.That(g.MinY, Is.GreaterThanOrEqualTo((Horizon - StopBackdrop.HeroAboveDp) * k - 0.01f), shape);
                    Assert.That(g.MaxY, Is.LessThanOrEqualTo((Horizon + StopBackdrop.ReflectionDp) * k + 0.01f), shape);
                }
            }
        }

        [Test]
        public void EveryShape_EntersAtTheBoxEdges_WithoutAHardEdge()
        {
            StopShapeGraphic g = NewGraphic();
            foreach (string shape in StopShapes.All)
            {
                DrawPlain(g, shape, 1f);
                float left = FootX - (StopBackdrop.HeroWidthDp * 0.5f);
                float right = FootX + (StopBackdrop.HeroWidthDp * 0.5f);
                for (int i = 0; i < g.VertexCount; i++)
                {
                    float x = g.PositionAt(i).x;
                    if (Mathf.Abs(x - left) < 0.01f || Mathf.Abs(x - right) < 0.01f)
                    {
                        Assert.That(g.ColorAt(i).a, Is.EqualTo(0f), shape + ", vértice " + i + ": sin arista en el borde de la caja");
                    }
                }

                // Solo los rectángulos llegan al borde (y se funden); copas, techos y cúpulas se quedan dentro.
                for (int p = 0; p < StopShapes.PrimitiveCount(shape); p++)
                {
                    StopShapes.PrimitiveAt(shape, p, out int kind, out _, out Rect box);
                    if (kind != StopShapes.KindRect)
                    {
                        Assert.That(box.xMin, Is.GreaterThan(-StopBackdrop.HeroWidthDp * 0.5f + 0.01f), shape + ", primitiva " + p);
                        Assert.That(box.xMax, Is.LessThan(StopBackdrop.HeroWidthDp * 0.5f - 0.01f), shape + ", primitiva " + p);
                    }
                }
            }
        }

        [Test]
        public void EveryTone_IsBrightEnough_AndNotCaribbeanTurquoise()
        {
            foreach (string shape in StopShapes.All)
            {
                for (int tone = 0; tone < StopShapes.ToneCount; tone++)
                {
                    Color c = StopShapes.ToneColor(shape, tone);
                    Color.RGBToHSV(c, out float h, out float s, out float v);
                    Assert.That(v, Is.GreaterThanOrEqualTo(0.25f), shape + ", tono " + tone + ": VA-03, nada por debajo de V 0,25");
                    bool turquoise = h * 360f >= 160f && h * 360f <= 200f && s >= 0.4f && v >= 0.6f;
                    Assert.That(turquoise, Is.False, shape + ", tono " + tone + ": sin turquesa caribeño (guía §7)");
                }
            }
        }

        [Test]
        public void FlonaTapajos_HasAnEvenCanopy_WithNoEmergentTree()
        {
            // Sin samaúma (07 §8.1; propuesta del Mapa §10.3): ninguna copa sobresale del dosel.
            float min = float.MaxValue;
            float max = float.MinValue;
            int crowns = 0;
            for (int p = 0; p < StopShapes.PrimitiveCount(StopShapes.FlonaTapajos); p++)
            {
                StopShapes.PrimitiveAt(StopShapes.FlonaTapajos, p, out int kind, out _, out Rect box);
                if (kind == StopShapes.KindDome)
                {
                    crowns++;
                    min = Mathf.Min(min, box.yMax);
                    max = Mathf.Max(max, box.yMax);
                }
            }

            Assert.That(crowns, Is.GreaterThanOrEqualTo(8));
            Assert.That(max - min, Is.LessThanOrEqualTo(3f), "dosel parejo");
            Assert.That(StopShapes.Bounds(StopShapes.FlonaTapajos).yMax, Is.EqualTo(max), "nada por encima del dosel");
        }

        [Test]
        public void LagoTefe_HasNothingBelowTheHorizon_NoDolphinsNorDryShores()
        {
            // 07 §2.1 y §8.1: sin botos y sin orillas secas (la mortandad de 2023).
            Assert.That(StopShapes.Bounds(StopShapes.LagoTefe).yMin, Is.GreaterThanOrEqualTo(0f));

            // En cambio, Tarapoto sí lleva sus lomos de delfín en el agua.
            Assert.That(StopShapes.Bounds(StopShapes.Tarapoto).yMin, Is.LessThan(-4f));
        }

        [Test]
        public void PuertoNarino_IsOnlyTheRiverMouth_WithoutTheTown()
        {
            // 07 §2.1: asesor obligatorio (aldea identificable); hasta entonces, solo la boca del río: ni casas ni ventanas, y el
            // hueco de la boca sin orilla delante.
            bool mouthOpen = true;
            for (int p = 0; p < StopShapes.PrimitiveCount(StopShapes.PuertoNarino); p++)
            {
                StopShapes.PrimitiveAt(StopShapes.PuertoNarino, p, out _, out int tone, out Rect box);
                Assert.That(tone, Is.Not.EqualTo(StopShapes.ToneLight), "primitiva " + p + ": sin ventanas");
                if (tone == StopShapes.ToneShore || tone == StopShapes.ToneBody)
                {
                    mouthOpen &= box.xMax <= -4f || box.xMin >= 4f;
                }
            }

            Assert.That(mouthOpen, Is.True, "la boca, abierta en el centro");
        }

        [Test]
        public void CasaDeFierro_HasNoTower()
        {
            // 07 §2.1 y §8.1: la reserva, solo como arquitectura y sin el mito de Eiffel: nada alto y estrecho.
            for (int p = 0; p < StopShapes.PrimitiveCount(StopShapes.CasaDeFierro); p++)
            {
                StopShapes.PrimitiveAt(StopShapes.CasaDeFierro, p, out _, out _, out Rect box);
                if (box.yMax > 20f)
                {
                    Assert.That(box.width, Is.GreaterThanOrEqualTo(box.height), "primitiva " + p + ": más ancha que alta");
                }
            }

            Assert.That(StopShapes.Bounds(StopShapes.CasaDeFierro).yMax, Is.LessThanOrEqualTo(28f), "dos pisos y un tejado bajo");
        }

        [Test]
        public void Draw_DoesNotAllocate_OnceTheBuffersHaveGrown()
        {
            StopShapeGraphic g = NewGraphic();
            Color water = Palette.WaterBase("white");
            foreach (string shape in StopShapes.All)
            {
                // Los búferes crecen la primera vez, y cada camino (también el del atardecer) ya se ejecutó una vez.
                g.Clear();
                StopShapes.Draw(g, shape, FootX, Horizon, 1f, StopBackdrop.DuskLut, StopBackdrop.LutDusk, water,
                    StopBackdrop.ReflectionAlpha, StopBackdrop.ReflectionDp);
            }

            foreach (string shape in StopShapes.All)
            {
                g.Clear();
                TestDelegate draw = () => StopShapes.Draw(g, shape, FootX, Horizon, 1f, StopBackdrop.DuskLut, StopBackdrop.LutDusk,
                    water, StopBackdrop.ReflectionAlpha, StopBackdrop.ReflectionDp);
                draw(); // el delegado, ya compilado
                g.Clear();
                Assert.That(draw, Is.Not.AllocatingGCMemory(), shape);
            }
        }
    }
}
