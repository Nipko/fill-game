#nullable enable
using System;
using System.Reflection;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Director de «Llenar y teñir» (guion de la nueva intro, §7.4 y §16: A1, A2, A4, A5, A6 y A12) y tira de agua del marco
    /// (§11, <see cref="WaterStripGraphic"/>). Tableros medidos con <see cref="BoardLayout"/> como en <c>GameScreen</c>: nivel 1
    /// (3 cuias, 1 fila), nivel 7 (7 cuias, 4 + 3) y 16 cuias (6 + 5 + 5), a 360 × 780 y a 360 × 640 dp.
    /// </summary>
    public sealed class IntroChoreographyTests
    {
        private const float Frame = 1000f / 60f;

        private static readonly float[] Durations = { 600f, 800f, 1000f, 1200f, 1400f, 1500f };

        // Nivel 1 del tutorial: açaí × 3 y aguaje; aguaje × 3 y açaí; vacía (guion §7.2).
        private static readonly int[][] Level1 = { new[] { 8, 8, 8, 4 }, new[] { 4, 4, 4, 8 }, Array.Empty<int>() };

        // 7 cuias con 5 llenas de 3 rachas (15 frutas) y 2 vacías.
        private static readonly int[][] Level7 =
        {
            new[] { 8, 4, 4, 3 }, new[] { 4, 8, 8, 1 }, new[] { 3, 3, 1, 8 }, Array.Empty<int>(),
            new[] { 1, 4, 4, 7 }, new[] { 7, 7, 3, 1 }, Array.Empty<int>(),
        };

        [Test]
        public void LastBloom_EndsAtT_WithinOneFrame([Values(3, 7, 16)] int cuias, [Values(780f, 640f)] float height)
        {
            for (int d = 0; d < Durations.Length; d++)
            {
                float t = Durations[d];
                var plan = new IntroChoreography();
                plan.Plan(Input(cuias, height, t, IntroPerch.Canopy));
                Assert.That(plan.FruitCount, Is.GreaterThan(0));
                Assert.That(Mathf.Abs(plan.LastBloomEndMs - t), Is.LessThanOrEqualTo(Frame), "T = " + t);
                for (int r = 0; r < plan.RunCount; r++)
                {
                    Assert.That(plan.RunBloomMs[r] + IntroChoreography.BloomMs, Is.LessThanOrEqualTo(t + 0.01f), "ninguna floración se pasa de T");
                    Assert.That(plan.RunBloomMs[r], Is.GreaterThanOrEqualTo(0f));
                }
            }
        }

        [Test]
        public void LastBloom_EndsAtT_InBranchMode([Values(3, 7)] int cuias)
        {
            for (int d = 0; d < Durations.Length; d++)
            {
                float t = Durations[d];
                var plan = new IntroChoreography();
                plan.Plan(Input(cuias, 780f, t, IntroPerch.Branch));
                Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch));
                Assert.That(Mathf.Abs(plan.LastBloomEndMs - t), Is.LessThanOrEqualTo(Frame), "T = " + t);
            }
        }

        [Test]
        public void Water_StreamOpensWithin30ms_AndFillStartsWithin50ms([Values(3, 7, 16)] int cuias, [Values(800f, 1400f)] float t)
        {
            var plan = new IntroChoreography();
            plan.Plan(Input(cuias, 780f, t, IntroPerch.Canopy));
            for (int i = 0; i < plan.CuiaCount; i++)
            {
                if (plan.PassMs[i] < 0f)
                {
                    continue;
                }

                float open = plan.StreamOpenMs[i] - plan.PassMs[i];
                Assert.That(open, Is.InRange(0f, 30f), "chorrito tras el paso del agua (A2)");
                float fill = plan.FillStartMs[i] - plan.ContactMs[i];
                Assert.That(fill, Is.InRange(0f, 50f), "llenado tras el contacto (A2)");
                Assert.That(plan.ContactMs[i], Is.GreaterThan(plan.StreamOpenMs[i]));
                Assert.That(plan.FillEndMs[i], Is.GreaterThan(plan.FillStartMs[i]));
            }
        }

        [Test]
        public void Fill_EndsBefore700_Long_And400_Short([Values(3, 7, 16)] int cuias, [Values(780f, 640f)] float height)
        {
            var plan = new IntroChoreography();
            plan.Plan(Input(cuias, height, 1400f, IntroPerch.Canopy));
            Assert.That(plan.LastFillEndMs, Is.LessThanOrEqualTo(700f), "larga (A4)");
            plan.Plan(Input(cuias, height, 800f, IntroPerch.Canopy));
            Assert.That(plan.LastFillEndMs, Is.LessThanOrEqualTo(400f), "corta (A4)");
        }

        [Test]
        public void EmptyCuias_StayDry()
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(3, 780f, 1400f, IntroPerch.Canopy);
            plan.Plan(input);
            Assert.That(plan.PassMs[2], Is.EqualTo(-1f));
            Assert.That(plan.StreamOpenMs[2], Is.EqualTo(-1f));
            Assert.That(plan.FillEndMs[2], Is.EqualTo(-1f));
            Assert.That(plan.RunsOf[2], Is.EqualTo(0));
            for (int k = 0; k < plan.FruitCount; k++)
            {
                Assert.That(plan.FruitCuia[k], Is.Not.EqualTo(2));
            }
        }

        [Test]
        public void EveryImpact_ComesAfterItsCuiaIsFull_AndFallsInsideTheAxis([Values(3, 7, 16)] int cuias, [Values(IntroPerch.Canopy, IntroPerch.Branch)] IntroPerch perch)
        {
            for (int d = 0; d < Durations.Length; d++)
            {
                var plan = new IntroChoreography();
                IntroInput input = Input(cuias, 780f, Durations[d], perch);
                plan.Plan(input);
                for (int k = 0; k < plan.FruitCount; k++)
                {
                    int c = plan.FruitCuia[k];
                    Assert.That(plan.FruitImpactMs[k], Is.GreaterThan(plan.FillEndMs[c]), "impacto tras el llenado (A5)");
                    Assert.That(Mathf.Abs(plan.FruitImpactX[k] - input.CenterX[c]), Is.LessThanOrEqualTo(0.1f * input.Unit));
                    float tau = plan.FruitImpactMs[k] - plan.FruitReleaseMs[k];
                    Assert.That(tau, Is.InRange(IntroChoreography.MinFallMs - 0.01f, IntroChoreography.MaxFallMs + 0.01f));
                    Assert.That(plan.FruitY(k, tau), Is.EqualTo(plan.FruitImpactY[k]).Within(0.05f), "la caída llega a la superficie en τ");
                    Assert.That(plan.FruitV0[k], Is.GreaterThanOrEqualTo(0f), "la suelta nunca es hacia arriba");
                }
            }
        }

        [Test]
        public void Fruits_AtMost24_AndOnePerCuiaAbove24Runs()
        {
            var plan = new IntroChoreography();
            plan.Plan(Input(16, 780f, 1400f, IntroPerch.Canopy));
            Assert.That(plan.RunCount, Is.GreaterThan(IntroChoreography.MaxFruits));
            Assert.That(plan.FruitPerCuia, Is.True);
            int nonEmpty = 0;
            for (int i = 0; i < plan.CuiaCount; i++)
            {
                nonEmpty += plan.RunsOf[i] > 0 ? 1 : 0;
            }

            Assert.That(plan.FruitCount, Is.EqualTo(nonEmpty));
            Assert.That(plan.FruitCount, Is.LessThanOrEqualTo(IntroChoreography.MaxFruits));

            // Cascada de abajo arriba, 25 ms entre rachas; la racha de la fruta (la de arriba), la última.
            for (int k = 0; k < plan.FruitCount; k++)
            {
                for (int r = plan.FruitRunFirst[k]; r < plan.FruitRunLast[k]; r++)
                {
                    Assert.That(plan.RunBloomMs[r + 1] - plan.RunBloomMs[r], Is.EqualTo(IntroChoreography.CascadeMs).Within(0.001f));
                }

                Assert.That(plan.FruitTint[k], Is.EqualTo(plan.RunTint[plan.FruitRunLast[k]]));
            }

            plan.Plan(Input(7, 780f, 1400f, IntroPerch.Canopy));
            Assert.That(plan.FruitPerCuia, Is.False);
            Assert.That(plan.FruitCount, Is.EqualTo(15), "una fruta por racha");
        }

        [Test]
        public void ImpactOrder_RowsTopDown_ByPasses_BottomUpInsideTheCuia()
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(7, 780f, 1400f, IntroPerch.Canopy);
            plan.Plan(input);
            for (int k = 1; k < plan.FruitCount; k++)
            {
                Assert.That(plan.FruitImpactMs[k], Is.GreaterThanOrEqualTo(plan.FruitImpactMs[k - 1]));
                int a = plan.FruitCuia[k - 1];
                int b = plan.FruitCuia[k];
                Assert.That(input.Row[b], Is.GreaterThanOrEqualTo(input.Row[a]), "filas de arriba abajo");
                if (a == b)
                {
                    Assert.That(plan.RunFrom[plan.FruitRunFirst[k]], Is.GreaterThan(plan.RunFrom[plan.FruitRunFirst[k - 1]]), "de abajo arriba en la cuia");
                }
            }

            // Primera pasada de la fila 1: la racha de abajo de cada cuia, por x.
            Assert.That(plan.RunFrom[plan.FruitRunFirst[0]], Is.EqualTo(0));
            Assert.That(plan.RunFrom[plan.FruitRunFirst[1]], Is.EqualTo(0));
            Assert.That(input.CenterX[plan.FruitCuia[1]], Is.GreaterThan(input.CenterX[plan.FruitCuia[0]]));
        }

        [Test]
        public void HiddenDoses_GetNoFruit_AndDoNotChangeTheSchedule()
        {
            var a = new IntroChoreography();
            var b = new IntroChoreography();
            IntroInput inA = Input(3, 780f, 1400f, IntroPerch.Branch);
            IntroInput inB = Input(3, 780f, 1400f, IntroPerch.Branch);
            // La dosis 1 de la cuia 0 está oculta; en B tiene otro color real.
            inA.SetCuia(0, inA.CenterX[0], inA.FootY[0], 0, new[] { 8, 8, 8, 4 }, 0b0010);
            inB.SetCuia(0, inB.CenterX[0], inB.FootY[0], 0, new[] { 8, 13, 8, 4 }, 0b0010);
            a.Plan(inA);
            b.Plan(inB);
            Assert.That(a.RunOf(0, 1), Is.EqualTo(-1), "la oculta no forma racha");
            for (int k = 0; k < a.FruitCount; k++)
            {
                for (int r = a.FruitRunFirst[k]; r <= a.FruitRunLast[k]; r++)
                {
                    Assert.That(a.RunCuia[r] == 0 && a.RunFrom[r] <= 1 && a.RunTo[r] >= 1, Is.False, "ninguna fruta para una oculta");
                }
            }

            AssertSamePlan(a, b);
            Assert.That(a.FirstImpactMs, Is.GreaterThan(0f), "el «?» de todas las ocultas aparece en el primer impacto");
        }

        [Test]
        public void SameLevel_SamePlan_ByteForByte([Values(3, 7, 16)] int cuias, [Values(IntroPerch.Canopy, IntroPerch.Branch)] IntroPerch perch)
        {
            var a = new IntroChoreography();
            var b = new IntroChoreography();
            a.Plan(Input(cuias, 780f, 1400f, perch));
            b.Plan(Input(cuias, 780f, 1400f, perch));
            AssertSamePlan(a, b);
            b.Plan(Input(cuias, 780f, 1400f, perch)); // y replanificar sobre los mismos arrays da lo mismo
            AssertSamePlan(a, b);
        }

        [Test]
        public void Branch_Long_TroopEntersWhenRow1IsFull_AndReleasesAfterArriving([Values(3, 7)] int cuias)
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(cuias, 780f, 1400f, IntroPerch.Branch);
            plan.Plan(input);
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch));
            Assert.That(plan.MonkeyCount, Is.InRange(2, plan.ColumnCount));
            Assert.That(plan.MonkeyDp, Is.LessThanOrEqualTo(Mathf.Min(input.MonkeyDp, input.PanelTop - input.HudBottom - 10f)));
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                Assert.That(plan.MonkeyX(m, plan.Row1FullMs), Is.EqualTo(plan.MonkeyX(m, 0f)), "la tropa no se mueve antes de que la fila 1 esté llena (A3)");
                for (float t = 0f; t < plan.TotalMs; t += 5f)
                {
                    Assert.That(Mathf.Abs(plan.MonkeyVelocity(m, t)), Is.LessThanOrEqualTo(IntroChoreography.MonkeySpeedDpPerMs + 0.001f), "≤ 450 dp/s");
                }
            }

            for (int k = 0; k < plan.FruitCount; k++)
            {
                int m = plan.FruitMonkey[k];
                if (m < 0)
                {
                    continue;
                }

                float release = plan.FruitReleaseMs[k];
                float tau = plan.FruitImpactMs[k] - release;
                Assert.That(plan.MonkeyX(m, release), Is.EqualTo(plan.FruitReleaseX[k]).Within(0.01f), "suelta desde la mano");
                Assert.That(Mathf.Abs(plan.FruitImpactX[k] - plan.FruitReleaseX[k]) / tau, Is.LessThanOrEqualTo(IntroChoreography.MaxTossDpPerMs + 0.0001f),
                    "≤ 250 dp/s de velocidad horizontal hasta una columna vecina");
                Assert.That(plan.MonkeyX(m, release - IntroChoreography.ReleaseAfterArrivalMs), Is.EqualTo(plan.FruitReleaseX[k]).Within(0.01f),
                    "la suelta llega ≥ 50 ms después de que el miquito llegue a su columna");
            }
        }

        [Test]
        public void Branch_EveryFruitHasItsMonkey_OnTheTutorialBoards([Values(3, 7)] int cuias, [Values(800f, 1400f)] float t)
        {
            var plan = new IntroChoreography();
            plan.Plan(Input(cuias, 780f, t, IntroPerch.Branch));
            for (int k = 0; k < plan.FruitCount; k++)
            {
                Assert.That(plan.FruitMonkey[k], Is.GreaterThanOrEqualTo(0), "fruta " + k + ": de la mano de un miquito, no del dosel");
            }
        }

        [Test]
        public void Branch_Short_ReleasesNotBefore100ms()
        {
            var plan = new IntroChoreography();
            plan.Plan(Input(7, 780f, 800f, IntroPerch.Branch));
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch));
            for (int k = 0; k < plan.FruitCount; k++)
            {
                if (plan.FruitMonkey[k] >= 0)
                {
                    Assert.That(plan.FruitReleaseMs[k], Is.GreaterThanOrEqualTo(100f));
                }
            }
        }

        [Test]
        public void ThreeRows_OrNarrowBand_FallBackToCanopy()
        {
            var plan = new IntroChoreography();
            plan.Plan(Input(16, 780f, 1400f, IntroPerch.Branch));
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Canopy), "con 3 filas la franja mide < 44 dp");
            Assert.That(plan.MonkeyCount, Is.EqualTo(0));
            for (int k = 0; k < plan.FruitCount; k++)
            {
                Assert.That(plan.FruitMonkey[k], Is.EqualTo(-1));
            }
        }

        [Test]
        public void FruitSize_AtLeast8dp([Values(3, 7, 16)] int cuias, [Values(780f, 640f)] float height)
        {
            var plan = new IntroChoreography();
            plan.Plan(Input(cuias, height, 1400f, IntroPerch.Canopy));
            Assert.That(plan.FruitImpactDp, Is.GreaterThanOrEqualTo(8f));
            Assert.That(plan.FruitFlightDp, Is.GreaterThanOrEqualTo(plan.FruitImpactDp));
        }

        [Test]
        public void Replanning_DoesNotAllocate()
        {
            var plan = new IntroChoreography();
            IntroInput seven = Input(7, 780f, 1400f, IntroPerch.Branch);
            IntroInput sixteen = Input(16, 780f, 800f, IntroPerch.Canopy);
            plan.Plan(seven);
            plan.Plan(sixteen);
            Assert.That(() => plan.Plan(seven), Is.Not.AllocatingGCMemory());
            Assert.That(() => plan.Plan(sixteen), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void WaterStrip_MeshCountsQuads_AndIsEmptyWithZeroLength()
        {
            var go = new GameObject("Strip", typeof(RectTransform), typeof(CanvasRenderer), typeof(WaterStripGraphic));
            try
            {
                WaterStripGraphic strip = go.GetComponent<WaterStripGraphic>();
                strip.Setup();
                Assert.That(strip.raycastTarget, Is.False);
                strip.SetStrip(new Vector2(10f, -50f), 0f, 0f, 5f, 24f, false, false);
                Assert.That(Vertices(strip), Is.EqualTo(0), "con longitud 0 no hay vértices");

                strip.SetStrip(new Vector2(180f, -175f), 120f, 120f, 17.5f, 24f, false, false);
                int v = Vertices(strip);
                Assert.That(v % 4, Is.EqualTo(0));
                Assert.That(v / 4, Is.InRange(2, WaterStripGraphic.MaxQuads));

                strip.SetStrip(new Vector2(20f, -175f), 0f, 400f, 5f, 15f, true, false);
                Assert.That(Vertices(strip) / 4, Is.LessThanOrEqualTo(WaterStripGraphic.MaxQuads), "≤ 64 cuadriláteros aunque la tira sea larga");

                strip.SetStrip(new Vector2(20f, -175f), 0f, 3f, 5f, 24f, true, true);
                Assert.That(Vertices(strip), Is.GreaterThan(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static int Vertices(WaterStripGraphic strip)
        {
            MethodInfo populate = typeof(WaterStripGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;
            using (var vh = new VertexHelper())
            {
                populate.Invoke(strip, new object[] { vh });
                return vh.currentVertCount;
            }
        }

        private static void AssertSamePlan(IntroChoreography a, IntroChoreography b)
        {
            Assert.That(b.FruitCount, Is.EqualTo(a.FruitCount));
            Assert.That(b.RunCount, Is.EqualTo(a.RunCount));
            Assert.That(b.MonkeyCount, Is.EqualTo(a.MonkeyCount));
            Assert.That(b.FruitImpactMs, Is.EqualTo(a.FruitImpactMs));
            Assert.That(b.FruitReleaseMs, Is.EqualTo(a.FruitReleaseMs));
            Assert.That(b.FruitReleaseY, Is.EqualTo(a.FruitReleaseY));
            Assert.That(b.FruitCuia, Is.EqualTo(a.FruitCuia));
            Assert.That(b.FruitSpin, Is.EqualTo(a.FruitSpin));
            Assert.That(b.FruitMonkey, Is.EqualTo(a.FruitMonkey));
            Assert.That(b.RunBloomMs, Is.EqualTo(a.RunBloomMs));
            Assert.That(b.StreamOpenMs, Is.EqualTo(a.StreamOpenMs));
            Assert.That(b.FillEndMs, Is.EqualTo(a.FillEndMs));
            Assert.That(b.MonkeyKeyCount, Is.EqualTo(a.MonkeyKeyCount));
            Assert.That(b.MonkeyKeyT, Is.EqualTo(a.MonkeyKeyT));
            Assert.That(b.MonkeyKeyX, Is.EqualTo(a.MonkeyKeyX));
        }

        // Tablero de 3, 7 o 16 cuias medido con BoardLayout como GameScreen (safeTop 32, safeBottom 24).
        internal static IntroInput Input(int count, float canvasH, float totalMs, IntroPerch perch)
        {
            float topRow = 32f + 8f;
            float boardTop = topRow + 48f;
            float trayTop = canvasH - 24f - 16f - 80f;
            float boardBottom = trayTop - 40f;
            BoardLayoutResult l = BoardLayout.Compute(count, AF.Presentation.CuiaGeometry.HeightU(4), 360f, boardTop, boardBottom);
            float leftEdge = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                leftEdge = Mathf.Min(leftEdge, l.CenterX[i] - (l.CuiaWidth * 0.5f));
            }

            float frame = 13.5f * Mathf.Clamp((leftEdge - l.PanelX - 8f) / 13.5f, 0.45f, 1f);
            var input = new IntroInput
            {
                Count = count,
                Unit = l.CuiaWidth * 0.5f,
                Rows = l.Rows,
                CanvasWidth = 360f,
                PanelX = l.PanelX,
                PanelTop = l.PanelY,
                PanelWidth = l.PanelWidth,
                PanelBottom = l.PanelBottom,
                FrameDp = frame,
                HudBottom = boardTop,
                TotalMs = totalMs,
                Perch = perch,
                Model = TroopModel.Row,
                Troop = 5,
                MonkeyDp = 40f,
                Seed = count,
            };
            for (int r = 0; r < l.Rows; r++)
            {
                input.RowFootY[r] = l.RowFootY[r];
            }

            for (int i = 0; i < count; i++)
            {
                int[] tints = count == 3 ? Level1[i] : (count == 7 ? Level7[i] : Sixteen(i));
                input.SetCuia(i, l.CenterX[i], l.FootY[i], l.RowOf[i], tints);
            }

            return input;
        }

        // 16 cuias: 14 llenas con 4 rachas cada una (56 rachas) y 2 vacías.
        private static int[] Sixteen(int i)
        {
            if (i >= 14)
            {
                return Array.Empty<int>();
            }

            return new[] { (i % 14) + 1, ((i + 3) % 14) + 1, ((i + 6) % 14) + 1, ((i + 9) % 14) + 1 };
        }
    }
}
