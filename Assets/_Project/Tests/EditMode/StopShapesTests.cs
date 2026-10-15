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
                if (shape == StopShapes.Tarapoto)
                {
                    // Lo reconocible de Tarapoto está en el agua (los delfines); sobre el horizonte, solo la orilla baja.
                    Assert.That(r.yMin, Is.LessThanOrEqualTo(-8f), shape + ": los lomos en el agua");
                    Assert.That(r.yMax, Is.GreaterThanOrEqualTo(3f), shape + ": la orilla lejana");
                }
                else
                {
                    Assert.That(r.yMax, Is.GreaterThan(8f), shape + ": algo sobre el horizonte");
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

                // Solo los rectángulos llegan al borde (y se funden); copas, techos, cúpulas y playas fundidas se quedan dentro.
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
            // de agua, en la boca y a ras del horizonte (revisión de M2.4).
            bool mouthOpen = true;
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
                    mouthOpen &= box.xMax <= -25f || box.xMin >= 25f;
                    wedges += kind == StopShapes.KindTriangle && tone == StopShapes.ToneBody ? 1 : 0;
                }
            }

            Assert.That(mouthOpen, Is.True, "la boca, abierta 50-60 dp en el centro");
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
            foreach (float end in new[] { -111f, -41f, -27f, 27f, 41f, 111f })
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
        public void PacayaSamiria_TheCochaIsATranslucentBlackMirror_EvenAtDawn()
        {
            // Revisión de M2.4 (D-003: luz de juego): la cocha, 4 dp de agua negra al CochaAlpha con su brillo de 1 dp arriba, y
            // los bordes fundidos en 28 dp; al alba sigue siendo agua y no una losa a contraluz.
            string s = StopShapes.PacayaSamiria;
            bool cocha = false;
            bool shine = false;
            for (int p = 0; p < StopShapes.PrimitiveCount(s); p++)
            {
                StopShapes.PrimitiveAt(s, p, out int kind, out int tone, out Rect box);
                if (box.yMax <= 0f)
                {
                    Assert.That(tone == StopShapes.ToneFeature || tone == StopShapes.ToneLight, Is.True,
                        "primitiva " + p + ": bajo el horizonte, solo el agua");
                }

                if (kind == StopShapes.KindRect && tone == StopShapes.ToneFeature)
                {
                    cocha = true;
                    Assert.That(box.height, Is.EqualTo(4f), "la cocha");
                }

                if (kind == StopShapes.KindRect && tone == StopShapes.ToneLight)
                {
                    shine = true;
                    Assert.That(box.height, Is.EqualTo(1f), "el brillo del espejo");
                    Assert.That(box.yMax, Is.EqualTo(0f), "en el borde de arriba de la cocha");
                }
            }

            Assert.That(cocha && shine, Is.True);
            Assert.That(StopShapes.EdgeFadeOf(s), Is.EqualTo(28f));
            Assert.That(StopShapes.EdgeFadeOf(StopShapes.Tarapoto), Is.EqualTo(StopShapes.EdgeFadeDp));
            Assert.That(StopShapes.ToneAlpha(s, StopShapes.ToneFeature), Is.InRange(0.55f, 0.65f));
            Color water = StopShapes.ToneColor(s, StopShapes.ToneFeature);
            Color.RGBToHSV(water, out _, out _, out float v);
            Assert.That(v, Is.LessThan(0.3f), "agua negra");
            foreach (string light in new[] { StopBackdrop.LutDawn, StopBackdrop.LutDusk })
            {
                Color lit = StopShapes.LitColor(s, StopShapes.ToneFeature, Color.white, light);
                Assert.That(new Color(lit.r, lit.g, lit.b, 1f), Is.EqualTo(water), light + ": la cocha, con su tono");
                Assert.That(lit.a, Is.EqualTo(StopShapes.CochaAlpha), light);
            }

            // Los aguajes, en cambio, sí van a contraluz al alba.
            Assert.That(StopShapes.LitColor(s, StopShapes.ToneBody, StopBackdrop.DawnLut, StopBackdrop.LutDawn), Is.EqualTo(StopShapes.DawnBody));
            Assert.That(StopShapes.LitColor(s, StopShapes.ToneDetail, StopBackdrop.DawnLut, StopBackdrop.LutDawn), Is.EqualTo(StopShapes.DawnBody));
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
