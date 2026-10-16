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
    /// arista (y sin nada que no sea rectángulo en la franja que se funde), no bajan de V 0,25 ni usan el turquesa caribeño, y
    /// cumplen las reglas de cada sitio que se pueden medir: el dosel parejo de la Floresta Nacional (sin samaúma), nada bajo el
    /// horizonte en Tefé (sin botos ni orillas secas) y su ciudad de tejas distinta de Nauta, Puerto Nariño sin pueblo, la cocha de
    /// Pacaya-Samiria como lente con el reflejo de los aguajes, lo que el follaje del marco taparía en los ±85 dp centrales y la
    /// Casa de Fierro sin torre (sin el mito de Eiffel). <see cref="StopShapes.Draw"/> no asigna memoria.
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
                if (n >= 20)
                {
                    // 201-420 (M4.5a; Q14-A): solo el kit, con piezas sin forma propia todavía (la genérica del agua).
                    Assert.That(b.Hero, Is.Null, "parada " + n);
                    Assert.That(b.Code, Does.StartWith("kit:"), "parada " + n);
                    Assert.That(StopShapes.ShapeOf(b), Is.EqualTo(StopShapes.GenericWater), "parada " + n);
                    continue;
                }

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

            Assert.That(sites.Count, Is.EqualTo(18), "07 §2.1: 18 sitios en 1-200");
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
                if (shape == StopShapes.Tarapoto)
                {
                    // Lo reconocible de Tarapoto está en el agua (los delfines); sobre el horizonte, solo la orilla baja.
                    Assert.That(r.yMin, Is.LessThanOrEqualTo(-8f), shape + ": los lomos en el agua");
                    Assert.That(r.yMax, Is.GreaterThanOrEqualTo(3f), shape + ": la orilla lejana");
                }
                else
                {
                    // Nauta, la más baja (lejana, revisión de M2.4), llega a 8 dp.
                    Assert.That(r.yMax, Is.GreaterThanOrEqualTo(8f), shape + ": algo sobre el horizonte");
                }
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

                // Solo los rectángulos llegan al borde (y se funden); las playas fundidas se quedan dentro, y copas, techos y
                // cúpulas (lo que no es rectángulo), además, fuera de la franja que se funde: nada opaco flota sobre un fondo que
                // se desvanece (revisión de M2.4).
                float half = StopBackdrop.HeroWidthDp * 0.5f;
                for (int p = 0; p < StopShapes.PrimitiveCount(shape); p++)
                {
                    StopShapes.PrimitiveAt(shape, p, out int kind, out _, out Rect box);
                    if (kind == StopShapes.KindSoftRect)
                    {
                        Assert.That(box.xMin, Is.GreaterThan(-half + 0.01f), shape + ", primitiva " + p);
                        Assert.That(box.xMax, Is.LessThan(half - 0.01f), shape + ", primitiva " + p);
                    }
                    else if (kind != StopShapes.KindRect)
                    {
                        Assert.That(box.xMin, Is.GreaterThanOrEqualTo(-half + StopShapes.EdgeFadeDp - 0.01f),
                            shape + ", primitiva " + p + ": fuera de la franja que se funde");
                        Assert.That(box.xMax, Is.LessThanOrEqualTo(half - StopShapes.EdgeFadeDp + 0.01f),
                            shape + ", primitiva " + p + ": fuera de la franja que se funde");
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
            // Sin samaúma (07 §8.1; propuesta del Mapa §10.3): ninguna copa sobresale del dosel. Revisión de M2.4: el barranco de
            // tierra firme de 6-8 dp y, encima, el dosel continuo de 12-14 dp (un bloque de borde a borde), con el borde de arriba
            // ondulado como mucho 1,5 dp y sin copas sueltas.
            float min = float.MaxValue;
            float max = float.MinValue;
            int crowns = 0;
            bool bank = false;
            bool canopy = false;
            float canopyTop = 0f;
            for (int p = 0; p < StopShapes.PrimitiveCount(StopShapes.FlonaTapajos); p++)
            {
                StopShapes.PrimitiveAt(StopShapes.FlonaTapajos, p, out int kind, out int tone, out Rect box);
                if (kind == StopShapes.KindDome)
                {
                    crowns++;
                    min = Mathf.Min(min, box.yMax);
                    max = Mathf.Max(max, box.yMax);
                    Assert.That(box.height, Is.LessThanOrEqualTo(1.5f), "primitiva " + p + ": ondulación del borde, no una copa suelta");
                    continue;
                }

                bool fullWidth = kind == StopShapes.KindRect && box.xMin <= -120f && box.xMax >= 120f;
                if (fullWidth && tone == StopShapes.ToneFeature)
                {
                    bank = true;
                    Assert.That(box.height, Is.InRange(6f, 8f), "el barranco");
                }

                if (fullWidth && tone == StopShapes.ToneBody)
                {
                    canopy = true;
                    canopyTop = box.yMax;
                    Assert.That(box.height, Is.InRange(12f, 14f), "el dosel continuo");
                }
            }

            Assert.That(bank && canopy, Is.True, "barranco y dosel de borde a borde");
            Assert.That(crowns, Is.GreaterThanOrEqualTo(8));
            Assert.That(max - min, Is.LessThanOrEqualTo(3f), "dosel parejo");
            Assert.That(min - canopyTop, Is.LessThanOrEqualTo(1.5f + 0.01f), "las ondas, sobre el borde del dosel");
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
            // 07 §2.1: asesor obligatorio (aldea identificable); hasta entonces, solo la boca del río: ni casas ni ventanas, ni
            // copas sueltas, y la boca abierta 50-60 dp, sin orilla delante, entre dos orillas en cuña. Lo claro es solo la lengua
            // de agua, en la boca y a ras del horizonte (revisión de M2.4). La boca se mide: del extremo derecho de la orilla y la
            // cuña de la izquierda al extremo izquierdo de las de la derecha.
            float leftBank = float.MinValue;
            float rightBank = float.MaxValue;
            bool straddles = false;
            int tongues = 0;
            int wedges = 0;
            for (int p = 0; p < StopShapes.PrimitiveCount(StopShapes.PuertoNarino); p++)
            {
                StopShapes.PrimitiveAt(StopShapes.PuertoNarino, p, out int kind, out int tone, out Rect box);
                Assert.That(kind, Is.Not.EqualTo(StopShapes.KindDome), "primitiva " + p + ": sin copas sueltas");
                if (tone == StopShapes.ToneLight)
                {
                    tongues++;
                    Assert.That(kind, Is.EqualTo(StopShapes.KindTriangle), "primitiva " + p + ": la lengua de agua, no una ventana");
                    Assert.That(box.yMin, Is.EqualTo(0f), "primitiva " + p);
                    Assert.That(box.yMax, Is.LessThanOrEqualTo(2f), "primitiva " + p + ": 2 dp");
                    Assert.That(box.xMin, Is.GreaterThanOrEqualTo(-30f), "primitiva " + p + ": en la boca");
                    Assert.That(box.xMax, Is.LessThanOrEqualTo(30f), "primitiva " + p + ": en la boca");
                }

                if (tone == StopShapes.ToneShore || tone == StopShapes.ToneBody)
                {
                    if (box.xMax <= 0f)
                    {
                        leftBank = Mathf.Max(leftBank, box.xMax);
                    }
                    else if (box.xMin >= 0f)
                    {
                        rightBank = Mathf.Min(rightBank, box.xMin);
                    }
                    else
                    {
                        straddles = true;
                    }

                    wedges += kind == StopShapes.KindTriangle && tone == StopShapes.ToneBody ? 1 : 0;
                }
            }

            Assert.That(straddles, Is.False, "ninguna orilla cruza el centro de la boca");
            Assert.That(rightBank - leftBank, Is.InRange(50f, 60f), "la boca, abierta 50-60 dp en el centro");
            Assert.That(tongues, Is.EqualTo(1), "la lengua de agua");
            Assert.That(wedges, Is.EqualTo(2), "las dos orillas en cuña");
        }

        [Test]
        public void Tarapoto_TheDolphinsStandOut_OverALowShore()
        {
            // Revisión de M2.4: los dos lomos, grandes (el rosado de 36 × 7 dp; el gris de 26 × 5, con su aleta de 6 dp), cada
            // uno con su trazo de agua clara; la orilla lejana, solo una franja baja, sin copas.
            float pinkWidth = 0f;
            float greyWidth = 0f;
            float finHeight = 0f;
            int outlines = 0;
            for (int p = 0; p < StopShapes.PrimitiveCount(StopShapes.Tarapoto); p++)
            {
                StopShapes.PrimitiveAt(StopShapes.Tarapoto, p, out int kind, out int tone, out Rect box);
                if (tone == StopShapes.ToneShore || tone == StopShapes.ToneAccent || tone == StopShapes.ToneBody)
                {
                    Assert.That(kind, Is.EqualTo(StopShapes.KindRect), "primitiva " + p + ": la orilla, sin copas");
                    Assert.That(box.yMax, Is.LessThanOrEqualTo(3.5f), "primitiva " + p + ": franja baja");
                }

                outlines += tone == StopShapes.ToneLight ? 1 : 0;
                if (kind == StopShapes.KindDome && tone == StopShapes.ToneFeature)
                {
                    pinkWidth = box.width;
                    Assert.That(box.height, Is.GreaterThanOrEqualTo(7f), "lomo rosado");
                }

                if (kind == StopShapes.KindDome && tone == StopShapes.ToneDetail)
                {
                    greyWidth = box.width;
                    Assert.That(box.height, Is.GreaterThanOrEqualTo(5f), "lomo gris");
                }

                if (kind == StopShapes.KindTriangle && tone == StopShapes.ToneDetail)
                {
                    finHeight = box.height;
                }
            }

            Assert.That(pinkWidth, Is.GreaterThanOrEqualTo(36f));
            Assert.That(greyWidth, Is.GreaterThanOrEqualTo(26f));
            Assert.That(finHeight, Is.GreaterThanOrEqualTo(6f), "la aleta");
            Assert.That(outlines, Is.EqualTo(3), "el trazo de agua de los dos lomos y de la aleta");

            // De color propio, que destaca sobre el agua: no pasan a contraluz.
            foreach (int tone in new[] { StopShapes.ToneFeature, StopShapes.ToneDetail, StopShapes.ToneLight })
            {
                Assert.That(StopShapes.KeepsItsTone(StopShapes.Tarapoto, tone), Is.True);
                Color lit = StopShapes.LitColor(StopShapes.Tarapoto, tone, Color.white, StopBackdrop.LutDusk);
                Assert.That(lit, Is.EqualTo(StopShapes.ToneColor(StopShapes.Tarapoto, tone)), "tono " + tone);
            }
        }

        [Test]
        public void Beacon_StandsBetweenLowFloodplainShores_WithoutCrowns()
        {
            // Revisión de M2.4: las orillas de várzea, bajas (≤ 4 dp) y sin copas; solo la baliza sube.
            for (int p = 0; p < StopShapes.PrimitiveCount(StopShapes.Beacon); p++)
            {
                StopShapes.PrimitiveAt(StopShapes.Beacon, p, out int kind, out int tone, out Rect box);
                Assert.That(kind, Is.Not.EqualTo(StopShapes.KindDome), "primitiva " + p + ": sin copas");
                if (tone == StopShapes.ToneShore || tone == StopShapes.ToneBody)
                {
                    Assert.That(box.yMax, Is.LessThanOrEqualTo(4f), "primitiva " + p + ": orilla baja");
                }
            }

            Assert.That(StopShapes.Bounds(StopShapes.Beacon).yMax, Is.GreaterThan(20f), "la baliza");
        }

        [Test]
        public void Rafts_EachHouseFloatsOnItsRaft_WithWaterBetween()
        {
            // Revisión de M2.4: cada casa sobre una balsa de troncos de 3-4 dp con 2 o 3 juntas claras de 0,8 dp, que baja 1 dp bajo
            // el horizonte; entre balsa y balsa, al menos 8 dp de agua.
            var rafts = new List<Rect>();
            var joints = new List<Rect>();
            int houses = 0;
            for (int p = 0; p < StopShapes.PrimitiveCount(StopShapes.Rafts); p++)
            {
                StopShapes.PrimitiveAt(StopShapes.Rafts, p, out int kind, out int tone, out Rect box);
                if (kind != StopShapes.KindRect)
                {
                    continue;
                }

                if (tone == StopShapes.ToneShore)
                {
                    rafts.Add(box);
                }
                else if (tone == StopShapes.ToneLight && box.yMin < 0f)
                {
                    joints.Add(box);
                }
                else if (tone == StopShapes.ToneBody)
                {
                    houses++;
                    Assert.That(box.yMin, Is.GreaterThan(0f), "primitiva " + p + ": la casa, sobre su balsa");
                }
            }

            Assert.That(houses, Is.GreaterThanOrEqualTo(4));
            Assert.That(rafts.Count, Is.EqualTo(houses), "una balsa por casa");
            rafts.Sort((a, b) => a.xMin.CompareTo(b.xMin));
            for (int i = 0; i < rafts.Count; i++)
            {
                Rect r = rafts[i];
                Assert.That(r.height, Is.InRange(3f, 4f), "balsa " + i);
                Assert.That(r.yMin, Is.EqualTo(-1f), "balsa " + i + ": 1 dp bajo el horizonte");
                if (i > 0)
                {
                    Assert.That(r.xMin - rafts[i - 1].xMax, Is.GreaterThanOrEqualTo(8f), "agua entre las balsas " + (i - 1) + " y " + i);
                }

                int inRaft = 0;
                foreach (Rect j in joints)
                {
                    if (j.xMin > r.xMin && j.xMax < r.xMax)
                    {
                        inRaft++;
                        Assert.That(j.width, Is.LessThanOrEqualTo(0.801f), "junta de la balsa " + i);
                    }
                }

                Assert.That(inRaft, Is.InRange(2, 3), "juntas de la balsa " + i);
            }
        }

        [Test]
        public void Anavilhanas_Beaches_FadeAtTheirEnds()
        {
            // Revisión de M2.4: las playas, sin remates verticales: rectángulos fundidos en sus extremos y 1 dp más anchos que su
            // isla a cada lado.
            int beaches = 0;
            for (int p = 0; p < StopShapes.PrimitiveCount(StopShapes.Anavilhanas); p++)
            {
                StopShapes.PrimitiveAt(StopShapes.Anavilhanas, p, out int kind, out int tone, out _);
                if (tone == StopShapes.ToneShore)
                {
                    beaches++;
                    Assert.That(kind, Is.EqualTo(StopShapes.KindSoftRect), "primitiva " + p);
                }
            }

            Assert.That(beaches, Is.EqualTo(3));

            // Pintadas sin reflejo: los vértices de los extremos de cada playa son transparentes.
            StopShapeGraphic g = NewGraphic();
            g.Clear();
            Color sand = StopShapes.ToneColor(StopShapes.Anavilhanas, StopShapes.ToneShore);
            Assert.That(StopShapes.Draw(g, StopShapes.Anavilhanas, FootX, Horizon, 1f, Color.white, null, sand, 0f, 0f), Is.True);
            foreach (float end in new[] { -105f, -39f, -27f, 27f, 39f, 105f })
            {
                int found = 0;
                for (int i = 0; i < g.VertexCount; i++)
                {
                    Color c = g.ColorAt(i);
                    if (Mathf.Abs(g.PositionAt(i).x - (FootX + end)) < 0.01f && Mathf.Abs(c.r - sand.r) < 0.002f
                        && Mathf.Abs(c.g - sand.g) < 0.002f && Mathf.Abs(c.b - sand.b) < 0.002f)
                    {
                        found++;
                        Assert.That(c.a, Is.EqualTo(0f), "extremo " + end);
                    }
                }

                Assert.That(found, Is.GreaterThanOrEqualTo(2), "extremo " + end);
            }
        }

        [Test]
        public void PacayaSamiria_TheCochaIsALensOfBlackWater_WithTheAguajesReflected_EvenAtDawn()
        {
            // Revisión de M2.4 (D-003: luz de juego): la cocha, una lente de 150 × 8 dp que baja del horizonte (media elipse
            // inferior, con los extremos afinados), de agua negra al CochaAlpha; dentro, el reflejo claro de 2-3 aguajes y el brillo
            // de 1 dp del espejo en los 100 dp centrales, fundido en sus extremos. Al alba sigue siendo agua y no una losa a
            // contraluz.
            string s = StopShapes.PacayaSamiria;
            float rx = StopShapes.CochaWidthDp * 0.5f;
            int lenses = 0;
            int shines = 0;
            int trunks = 0;
            int crowns = 0;
            for (int p = 0; p < StopShapes.PrimitiveCount(s); p++)
            {
                StopShapes.PrimitiveAt(s, p, out int kind, out int tone, out Rect box);
                if (box.yMax <= 0f)
                {
                    Assert.That(tone == StopShapes.ToneFeature || tone == StopShapes.ToneLight || tone == StopShapes.ToneShore, Is.True,
                        "primitiva " + p + ": bajo el horizonte, solo el agua y su reflejo");
                    Assert.That(box.yMin, Is.GreaterThanOrEqualTo(-StopShapes.CochaDepthDp), "primitiva " + p + ": dentro de la cocha");
                }

                if (tone == StopShapes.ToneFeature)
                {
                    lenses++;
                    Assert.That(kind, Is.EqualTo(StopShapes.KindDome), "la cocha, una lente y no una franja");
                    Assert.That(box.width, Is.EqualTo(150f), "la cocha");
                    Assert.That(box.yMax, Is.EqualTo(0f), "desde el horizonte");
                    Assert.That(box.yMin, Is.EqualTo(-8f), "8 dp de hondo");
                    Assert.That(box.center.x, Is.EqualTo(0f), "centrada");
                }

                if (tone == StopShapes.ToneLight)
                {
                    shines++;
                    Assert.That(kind, Is.EqualTo(StopShapes.KindSoftRect), "el brillo, fundido en sus extremos");
                    Assert.That(box.height, Is.EqualTo(1f), "el brillo del espejo");
                    Assert.That(box.yMax, Is.EqualTo(0f), "en el borde de arriba de la cocha");
                    Assert.That(box.width, Is.EqualTo(100f), "en los 100 dp centrales");
                }

                if (tone == StopShapes.ToneShore)
                {
                    // Dentro de la lente: en el x de la caja más alejado del centro, la elipse es al menos tan honda como ella.
                    float far = Mathf.Max(Mathf.Abs(box.xMin), Mathf.Abs(box.xMax));
                    float depth = far >= rx ? 0f : StopShapes.CochaDepthDp * Mathf.Sqrt(1f - ((far / rx) * (far / rx)));
                    if (kind == StopShapes.KindRect)
                    {
                        trunks++;
                        Assert.That(box.width, Is.EqualTo(1.6f).Within(0.001f), "primitiva " + p + ": tronco reflejado");
                        Assert.That(box.height, Is.EqualTo(6f), "primitiva " + p + ": tronco reflejado");
                        Assert.That(-box.yMin, Is.LessThanOrEqualTo(depth + 0.01f), "primitiva " + p + ": el tronco, dentro de la lente");
                    }
                    else
                    {
                        crowns++;
                        Assert.That(kind, Is.EqualTo(StopShapes.KindDome), "primitiva " + p);
                        Assert.That(box.width, Is.EqualTo(6f), "primitiva " + p + ": copa reflejada");
                        Assert.That(box.height, Is.EqualTo(3f), "primitiva " + p + ": copa reflejada");
                        float centerDepth = StopShapes.CochaDepthDp * Mathf.Sqrt(1f - ((box.center.x / rx) * (box.center.x / rx)));
                        Assert.That(-box.yMin, Is.LessThanOrEqualTo(centerDepth + 0.01f), "primitiva " + p + ": la copa, dentro de la lente");
                    }
                }
            }

            Assert.That(lenses, Is.EqualTo(1));
            Assert.That(shines, Is.EqualTo(1));
            Assert.That(trunks, Is.InRange(2, 3), "el reflejo de 2-3 aguajes");
            Assert.That(crowns, Is.EqualTo(trunks), "cada tronco reflejado, con su copa");
            Assert.That(StopShapes.ToneAlpha(s, StopShapes.ToneFeature), Is.EqualTo(0.85f));
            Assert.That(StopShapes.ToneAlpha(s, StopShapes.ToneLight), Is.EqualTo(0.8f));
            Assert.That(StopShapes.ToneAlpha(s, StopShapes.ToneShore), Is.EqualTo(0.35f));
            Color water = StopShapes.ToneColor(s, StopShapes.ToneFeature);
            Color.RGBToHSV(water, out _, out _, out float v);
            Assert.That(v, Is.LessThan(0.3f), "agua negra (sin bajar de V 0,25: VA-03)");
            foreach (string light in new[] { StopBackdrop.LutDawn, StopBackdrop.LutDusk })
            {
                foreach (int tone in new[] { StopShapes.ToneFeature, StopShapes.ToneLight, StopShapes.ToneShore })
                {
                    Color lit = StopShapes.LitColor(s, tone, Color.white, light);
                    Assert.That(new Color(lit.r, lit.g, lit.b, 1f), Is.EqualTo(StopShapes.ToneColor(s, tone)), light + ", tono " + tone + ": con su tono");
                    Assert.That(lit.a, Is.EqualTo(StopShapes.ToneAlpha(s, tone)), light + ", tono " + tone);
                }
            }

            // Los aguajes, en cambio, sí van a contraluz al alba.
            Assert.That(StopShapes.LitColor(s, StopShapes.ToneBody, StopBackdrop.DawnLut, StopBackdrop.LutDawn), Is.EqualTo(StopShapes.DawnBody));
            Assert.That(StopShapes.LitColor(s, StopShapes.ToneDetail, StopBackdrop.DawnLut, StopBackdrop.LutDawn), Is.EqualTo(StopShapes.DawnBody));

            // La lente y su reflejo no se vuelven a reflejar: pintada sin el reflejo de la escena, nada baja de los 8 dp de la cocha.
            StopShapeGraphic g = NewGraphic();
            g.Clear();
            Assert.That(StopShapes.Draw(g, s, FootX, Horizon, 1f, Color.white, null, Palette.WaterBase("white"), 0f, 0f), Is.True);
            Assert.That(g.MaxY, Is.LessThanOrEqualTo(Horizon + StopShapes.CochaDepthDp + 0.01f));
        }

        [Test]
        public void LagoTefe_AWarmGabledCity_AndTheLakeShine_UnlikeNauta()
        {
            // Revisión de M2.4: Tefé, la ciudad de paredes cálidas y tejados a dos aguas de teja, de 16-20 dp de alto y unos 90 de
            // ancho (x de −80 a 10), con el brillo del lago de 1 dp al 0,6 a lo largo de la orilla; Nauta, lejana: fría, de azoteas
            // planas, de 6-8 dp y con el velo de la bruma (0,7). Así no se leen como la misma ciudad baja genérica.
            string tefe = StopShapes.LagoTefe;
            float cityMin = float.MaxValue;
            float cityMax = float.MinValue;
            float cityTop = 0f;
            int roofs = 0;
            bool shine = false;
            for (int p = 0; p < StopShapes.PrimitiveCount(tefe); p++)
            {
                StopShapes.PrimitiveAt(tefe, p, out int kind, out int tone, out Rect box);
                if (tone == StopShapes.ToneBody || (tone == StopShapes.ToneFeature && kind == StopShapes.KindTriangle))
                {
                    cityMin = Mathf.Min(cityMin, box.xMin);
                    cityMax = Mathf.Max(cityMax, box.xMax);
                    cityTop = Mathf.Max(cityTop, box.yMax);
                    roofs += kind == StopShapes.KindTriangle ? 1 : 0;
                }

                if (tone == StopShapes.ToneLight)
                {
                    shine = true;
                    Assert.That(kind, Is.EqualTo(StopShapes.KindRect), "el brillo del lago");
                    Assert.That(box.xMin <= -120f && box.xMax >= 120f, Is.True, "a lo largo de la orilla");
                    Assert.That(box.height, Is.EqualTo(1f), "1 dp");
                }
            }

            Assert.That(shine, Is.True);
            Assert.That(roofs, Is.GreaterThanOrEqualTo(4), "tejados a dos aguas");
            Assert.That(cityTop, Is.InRange(16f, 20f), "la ciudad, de 16-20 dp");
            Assert.That(cityMax - cityMin, Is.InRange(86f, 96f), "unos 90 dp de ancho");
            Assert.That(StopShapes.ToneAlpha(tefe, StopShapes.ToneLight), Is.EqualTo(0.6f));
            Assert.That(StopShapes.KeepsItsTone(tefe, StopShapes.ToneLight), Is.True, "el brillo es agua");
            Assert.That(StopShapes.ToneColor(tefe, StopShapes.ToneBody), Is.EqualTo((Color)Palette.Hex(0xC9B79A)), "paredes cálidas");
            Assert.That(StopShapes.ToneColor(tefe, StopShapes.ToneFeature), Is.EqualTo((Color)Palette.Hex(0x9A5A44)), "tejas");

            string nauta = StopShapes.Nauta;
            float nautaTop = 0f;
            for (int p = 0; p < StopShapes.PrimitiveCount(nauta); p++)
            {
                StopShapes.PrimitiveAt(nauta, p, out int kind, out int tone, out Rect box);
                if (tone == StopShapes.ToneBody)
                {
                    Assert.That(kind, Is.EqualTo(StopShapes.KindRect), "primitiva " + p + ": azoteas planas");
                    nautaTop = Mathf.Max(nautaTop, box.yMax);
                }

                Assert.That(kind, Is.Not.EqualTo(StopShapes.KindTriangle), "primitiva " + p + ": sin tejados");
            }

            Assert.That(nautaTop, Is.InRange(6f, 8f), "Nauta, baja y lejana");
            Assert.That(StopShapes.ToneAlpha(nauta, StopShapes.ToneBody), Is.EqualTo(0.7f), "con el velo de la bruma");
            Assert.That(StopShapes.ToneColor(nauta, StopShapes.ToneBody), Is.EqualTo((Color)Palette.Hex(0xA5AEB8)), "fría");
        }

        [Test]
        public void TheFramedFeatures_StayInTheCentral85Dp()
        {
            // Revisión de M2.4: el follaje del marco deja libres en el juego los ±85 dp centrales del héroe; ahí van las agujas de
            // Ver-o-Peso, las cinco casas del Itaya con sus balsas (aleros incluidos: 1 dp más) y el pueblo de Nauta.
            AssertCentral(StopShapes.TwoSpires, StopShapes.KindTriangle, StopShapes.ToneAccent, 3);
            AssertCentral(StopShapes.Rafts, StopShapes.KindRect, StopShapes.ToneBody, 5);
            AssertCentral(StopShapes.Rafts, StopShapes.KindTriangle, StopShapes.ToneAccent, 5);
            AssertCentral(StopShapes.Rafts, StopShapes.KindRect, StopShapes.ToneShore, 5);
            AssertCentral(StopShapes.Nauta, StopShapes.KindRect, StopShapes.ToneBody, 6);
        }

        // Las primitivas de la forma con ese tipo y tono quedan en los ±86 dp centrales (85 y 1 de alero); hay al menos min.
        private static void AssertCentral(string shape, int kind, int tone, int min)
        {
            int found = 0;
            for (int p = 0; p < StopShapes.PrimitiveCount(shape); p++)
            {
                StopShapes.PrimitiveAt(shape, p, out int k, out int t, out Rect box);
                if (t != tone || k != kind)
                {
                    continue;
                }

                found++;
                Assert.That(box.xMin, Is.GreaterThanOrEqualTo(-86f), shape + ", primitiva " + p);
                Assert.That(box.xMax, Is.LessThanOrEqualTo(86f), shape + ", primitiva " + p);
            }

            Assert.That(found, Is.GreaterThanOrEqualTo(min), shape);
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
