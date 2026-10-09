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
                Assert.That(plan.MonkeyHandX(m, release), Is.EqualTo(plan.FruitReleaseX[k]).Within(0.01f), "suelta desde la mano");
                Assert.That(Mathf.Abs(plan.FruitImpactX[k] - plan.FruitReleaseX[k]) / tau, Is.LessThanOrEqualTo(IntroChoreography.MaxTossDpPerMs + 0.0001f),
                    "≤ 250 dp/s de velocidad horizontal hasta una columna vecina");
                Assert.That(plan.MonkeyHandX(m, release - IntroChoreography.ReleaseAfterArrivalMs), Is.EqualTo(plan.FruitReleaseX[k]).Within(0.01f),
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
        public void FruitSize_AtLeast8dpVisible_AndTheSpriteCompensatesTheArtPadding([Values(3, 7, 16)] int cuias, [Values(780f, 640f)] float height)
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(cuias, height, 1400f, IntroPerch.Canopy);
            plan.Plan(input);
            Assert.That(plan.FruitImpactVisibleDp, Is.GreaterThanOrEqualTo(8f));
            Assert.That(plan.FruitFlightVisibleDp, Is.GreaterThanOrEqualTo(plan.FruitImpactVisibleDp));
            Assert.That(plan.FruitFlightVisibleDp, Is.EqualTo(Mathf.Max(8f, 0.6f * input.Unit)).Within(0.001f), "0,6 u en vuelo (guion §5)");
            Assert.That(plan.FruitImpactVisibleDp, Is.EqualTo(Mathf.Max(8f, 0.45f * input.Unit)).Within(0.001f), "0,45 u al impacto");
            Assert.That(plan.FruitFlightDp, Is.EqualTo(plan.FruitFlightVisibleDp / IntroChoreography.FruitArtFill).Within(0.001f), "el sprite compensa el relleno de la celda");
            Assert.That(plan.FruitImpactDp, Is.EqualTo(plan.FruitImpactVisibleDp / IntroChoreography.FruitArtFill).Within(0.001f));
            if (0.6f * input.Unit > 8f)
            {
                Assert.That(plan.FruitFlightVisibleDp, Is.LessThan(0.68f * input.Unit), "menor que la boca (0,68 u)");
            }
        }

        /// <summary>
        /// Tamaño visible del aguaje (tinte del tutorial) medido en su celda del atlas (revisión: 9 dp al impacto en el nivel 1 y
        /// 8 en vuelo en el nivel 7): en el nivel 7, ≥ 14 dp en vuelo y ≥ 10,5 dp al impacto; con 16 cuias, ≥ 8 dp al impacto.
        /// </summary>
        [Test]
        public void Aguaje_VisibleSize_MeasuredOnTheArt([Values(780f, 640f)] float height)
        {
            Color32[] px = ProceduralArt.FruitFlightPixels();
            RectInt cell = ProceduralArt.FruitCell(4);
            int x0 = int.MaxValue;
            int x1 = int.MinValue;
            int y0 = int.MaxValue;
            int y1 = int.MinValue;
            for (int y = cell.y; y < cell.yMax; y++)
            {
                for (int x = cell.x; x < cell.xMax; x++)
                {
                    if (px[(y * ProceduralArt.FruitAtlasWidth) + x].a > 128)
                    {
                        x0 = Mathf.Min(x0, x);
                        x1 = Mathf.Max(x1, x);
                        y0 = Mathf.Min(y0, y);
                        y1 = Mathf.Max(y1, y);
                    }
                }
            }

            float fill = Mathf.Min(x1 - x0 + 1, y1 - y0 + 1) / (float)ProceduralArt.FruitFlightPx; // el lado más corto
            var plan = new IntroChoreography();
            if (height > 700f)
            {
                plan.Plan(Input(7, height, 1400f, IntroPerch.Branch));
                Assert.That(plan.FruitFlightDp * fill, Is.GreaterThanOrEqualTo(14f), "nivel 7: aguaje en vuelo");
                Assert.That(plan.FruitImpactDp * fill, Is.GreaterThanOrEqualTo(10.5f), "nivel 7: aguaje al impacto");
            }

            plan.Plan(Input(16, height, 1400f, IntroPerch.Canopy));
            Assert.That(plan.FruitImpactDp * fill, Is.GreaterThanOrEqualTo(8f), "16 cuias: ≥ 8 dp visibles (A5)");
        }

        [Test]
        public void Version_IsDecidedByTheKey_NotByT()
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(7, 780f, 1000f, IntroPerch.Canopy);
            input.Version = IntroVersion.Short; // anim_tint_duration_ms_late = 1.000 (rango 600-1.000 de 08 §9)
            plan.Plan(input);
            Assert.That(plan.IsLong, Is.False);
            Assert.That(plan.Scale, Is.EqualTo(1000f / 800f).Within(0.0001f));
            Assert.That(Mathf.Abs(plan.LastBloomEndMs - 1000f), Is.LessThanOrEqualTo(Frame));
            input.Version = IntroVersion.Long;
            input.TotalMs = 1200f;
            plan.Plan(input);
            Assert.That(plan.IsLong, Is.True);
            Assert.That(plan.Scale, Is.EqualTo(1200f / 1400f).Within(0.0001f));
        }

        [Test]
        public void Hang_StaysHanging_WithABandOf64OrMore_AndReleasesAtLeast8dpAboveThePanel([Values(800f, 1400f)] float t)
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(7, 780f, t, IntroPerch.Branch);
            input.Model = TroopModel.Hang;
            input.MonkeyDp = 34f;
            input.Troop = 3;
            Assert.That(input.PanelTop - input.HudBottom, Is.GreaterThanOrEqualTo(IntroChoreography.HangMinBandDp));
            plan.Plan(input);
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch));
            Assert.That(plan.Model, Is.EqualTo(TroopModel.Hang));
            Assert.That(plan.BranchY, Is.EqualTo(input.HudBottom + IntroChoreography.HighBranchBelowHudDp).Within(0.001f), "rama alta en hudBottom + 6");
            Assert.That(plan.HangGripY, Is.GreaterThan(plan.BranchY), "cuelga por debajo de la rama alta");
            Assert.That(plan.HangGripY + (IntroChoreography.HangDepthS * plan.BodyDp), Is.LessThanOrEqualTo(input.PanelTop - 8f + 0.01f), "el cuerpo colgado acaba ≥ 8 dp sobre el panel");
            for (int k = 0; k < plan.FruitCount; k++)
            {
                if (plan.FruitMonkey[k] >= 0)
                {
                    Assert.That(plan.FruitReleaseY[k], Is.LessThanOrEqualTo(input.PanelTop - 8f + 0.01f), "la suelta de cuelga, ≥ 8 dp sobre el panel");
                    Assert.That(plan.MonkeyHandX(plan.FruitMonkey[k], plan.FruitReleaseMs[k]), Is.EqualTo(plan.FruitReleaseX[k]).Within(0.01f));
                }
            }
        }

        [Test]
        public void Hang_SitsOnTheLowBranchAsRow_WithABandUnder64()
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(7, 780f, 1400f, IntroPerch.Branch);
            input.Model = TroopModel.Hang;
            input.HudBottom = input.PanelTop - 56f; // franja de 56 dp: ≥ 44 (rama) pero < 64
            plan.Plan(input);
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch));
            Assert.That(plan.Model, Is.EqualTo(TroopModel.Row), "con menos de 64 dp, cuelga se sienta y suelta como fila (guion §4.4)");
            Assert.That(plan.BranchY, Is.EqualTo(input.PanelTop - IntroChoreography.LowBranchAbovePanelDp).Within(0.001f), "rama baja");
        }

        [Test]
        public void Shake_FruitFallsFromTheBranchOverItsColumn_AndTheMonkeysStayInTheirSeats([Values(800f, 1400f)] float t)
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(7, 780f, t, IntroPerch.Branch);
            input.Model = TroopModel.Shake;
            input.Troop = 4;
            input.MonkeyDp = 42f;
            plan.Plan(input);
            Assert.That(plan.Model, Is.EqualTo(TroopModel.Shake));
            Assert.That(plan.HandOffsetDp, Is.EqualTo(0f));
            int fromBranch = 0;
            for (int k = 0; k < plan.FruitCount; k++)
            {
                int m = plan.FruitMonkey[k];
                if (m < 0)
                {
                    continue;
                }

                fromBranch++;
                Assert.That(plan.FruitReleaseX[k], Is.EqualTo(plan.FruitImpactX[k]).Within(0.001f), "cae de la rama recta sobre su columna");
                Assert.That(plan.FruitReleaseY[k], Is.EqualTo(plan.BranchCurveY(plan.FruitImpactX[k]) - 2f).Within(0.01f), "desde la rama baja sacudida");
                Assert.That(plan.FruitV0[k], Is.GreaterThanOrEqualTo(0f));
                float seat = plan.MonkeyX(m, plan.FruitReleaseMs[k]);
                Assert.That(plan.MonkeyVelocity(m, plan.FruitReleaseMs[k]), Is.EqualTo(0f), "sacude quieto en su asiento");
                Assert.That(seat, Is.EqualTo(plan.MonkeyX(m, plan.ExitStartMs)).Within(0.01f), "no se mueve de su asiento hasta salir");
            }

            Assert.That(fromBranch, Is.GreaterThan(plan.FruitCount / 2), "casi todas desde la rama sacudida");
        }

        [Test]
        public void TooSmallMonkey_FallsBackToTheCanopy()
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(7, 780f, 1400f, IntroPerch.Branch);
            input.MonkeyDp = 24f; // tam_dp < 28: no cabe con su silueta (guion §4.4)
            plan.Plan(input);
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Canopy));
            Assert.That(plan.MonkeyCount, Is.EqualTo(0));
            input.MonkeyDp = 40f;
            input.HudBottom = input.PanelTop - 37f; // franja de 37 dp: < 44 y tamaño 27
            plan.Plan(input);
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Canopy));
        }

        /// <summary>
        /// Revisión: los miquitos soltaban 30 dp antes de su columna y salían cortados por el borde o solapados. Ahora: la mano
        /// sobre el eje (o a un lado con ≤ 250 dp/s), el centro en [0,5·S + 6, 360 − 0,5·S − 6] mientras posa y ≥ 1,1 S entre
        /// los centros de dos vecinos en todo momento.
        /// </summary>
        [Test]
        public void Troop_KeepsItsMargins_AndNeighborsNeverOverlap([Values(3, 7)] int cuias, [Values(800f, 1400f)] float t,
            [Values(TroopModel.Row, TroopModel.Hang, TroopModel.Shake)] TroopModel model)
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(cuias, 780f, t, IntroPerch.Branch);
            input.Model = model;
            input.MonkeyDp = model == TroopModel.Hang ? 34f : 40f;
            plan.Plan(input);
            Assert.That(plan.MonkeyCount, Is.GreaterThanOrEqualTo(2));
            float s = plan.BodyDp;
            float lo = (0.5f * s) + IntroChoreography.EdgeMarginDp;
            float hi = 360f - (0.5f * s) - IntroChoreography.EdgeMarginDp;
            for (float now = 0f; now <= plan.ExitStartMs + IntroChoreography.ExitMs; now += 2f)
            {
                for (int m = 0; m < plan.MonkeyCount; m++)
                {
                    float x = plan.MonkeyX(m, now);
                    bool resting = Mathf.Abs(plan.MonkeyVelocity(m, now)) < 0.0001f && now < plan.ExitStartMs && (!plan.IsLong || now > plan.TroopEnterMs);
                    if (resting)
                    {
                        Assert.That(x, Is.InRange(lo - 0.01f, hi + 0.01f), "centro dentro de los márgenes: miquito " + m + " a " + now + " ms");
                    }

                    if (m + 1 < plan.MonkeyCount)
                    {
                        float x2 = plan.MonkeyX(m + 1, now);
                        bool both = x > -0.5f * s && x < 360f + (0.5f * s) && x2 > -0.5f * s && x2 < 360f + (0.5f * s);
                        if (both)
                        {
                            Assert.That(Mathf.Abs(x2 - x), Is.GreaterThanOrEqualTo((IntroChoreography.NeighborGapS * s) - 0.05f), "vecinos " + m + "-" + (m + 1) + " a " + now + " ms");
                        }
                    }
                }
            }

            for (int k = 0; k < plan.FruitCount; k++)
            {
                if (plan.FruitMonkey[k] >= 0 && model != TroopModel.Shake)
                {
                    float tau = plan.FruitImpactMs[k] - plan.FruitReleaseMs[k];
                    Assert.That(Mathf.Abs(plan.FruitReleaseX[k] - plan.FruitImpactX[k]), Is.LessThanOrEqualTo((IntroChoreography.MaxTossDpPerMs * tau) + 0.01f));
                }
            }
        }

        [Test]
        public void Level1_HandsOverTheAxis_TheFruitFallsStraight([Values(800f, 1400f)] float t)
        {
            var plan = new IntroChoreography();
            plan.Plan(Input(3, 780f, t, IntroPerch.Branch));
            for (int k = 0; k < plan.FruitCount; k++)
            {
                Assert.That(plan.FruitMonkey[k], Is.GreaterThanOrEqualTo(0));
                Assert.That(plan.FruitReleaseX[k], Is.EqualTo(plan.FruitImpactX[k]).Within(0.01f), "la mano sobre el eje de la cuia (guion §7.2)");
            }
        }

        /// <summary>
        /// La pareja de <c>colas_entrelazadas</c> (revisión del 2026-10-09): lado a lado, mirando al mismo sitio, con las colas
        /// colgando bajo la rama; la rama sube lo que cuelgan, la punta acaba ≥ 6 dp sobre el panel y la pareja sentada no pasa
        /// sobre el HUD. Sin sitio en la franja, se sientan como los demás.
        /// </summary>
        [Test]
        public void Pair_SitsSideBySide_FacingTheSameWay_WithTheTailsHangingUnderTheBranch([Values(800f, 1400f)] float t, [Values(1, 2)] int seed)
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(7, 780f, t, IntroPerch.Branch);
            input.Model = TroopModel.Shake;
            input.Troop = 2;
            input.MonkeyDp = 40f;
            input.Pair = true;
            input.Seed = seed;
            plan.Plan(input);
            Assert.That(plan.Pair, Is.True);
            Assert.That(plan.MonkeyCount, Is.EqualTo(2));
            float at = plan.IsLong ? plan.ExitStartMs - 1f : 0f;
            float gap = plan.MonkeyX(1, at) - plan.MonkeyX(0, at);
            Assert.That(gap, Is.EqualTo(IntroChoreography.PairGapS * plan.BodyDp).Within(0.01f), "centros a 0,9 S");
            Assert.That(plan.MonkeyFacing(0, at), Is.EqualTo(plan.MonkeyFacing(1, at)), "mirando al mismo sitio");
            Assert.That(plan.PairTailHangDp, Is.EqualTo(IntroChoreography.PairTailHangS * plan.BodyDp).Within(0.01f));
            Assert.That(plan.BranchY + plan.PairTailHangDp, Is.LessThanOrEqualTo(input.PanelTop - IntroChoreography.PairTailClearDp + 0.01f), "la trenza acaba ≥ 6 dp sobre el panel");
            Assert.That(plan.BranchY - (IntroChoreography.SitTopS * plan.BodyDp), Is.GreaterThanOrEqualTo(input.HudBottom), "la pareja sentada no pasa sobre el HUD");
            Assert.That(plan.BodyDp, Is.GreaterThanOrEqualTo(IntroChoreography.MinMonkeyDp));

            // Con una franja de 56 dp no caben las colas colgando con ≥ 28 dp: sin pareja, en la rama baja.
            input.HudBottom = input.PanelTop - 56f;
            plan.Plan(input);
            Assert.That(plan.Pair, Is.False);
            Assert.That(plan.PairTailHangDp, Is.EqualTo(0f));
            Assert.That(plan.BranchY, Is.EqualTo(input.PanelTop - IntroChoreography.LowBranchAbovePanelDp).Within(0.001f));
        }

        /// <summary>
        /// Revisión del 2026-10-09: la rama baja tiene su punto más bajo en panel.top − 8 (con 6 dp ocupa de panel.top − 11 a
        /// panel.top − 5, sobre la lámina del desborde, que sube hasta panel.top − 4) y sube hacia el follaje de las esquinas.
        /// </summary>
        [Test]
        public void LowBranch_LowestPointIsPanelTopMinus8_AndRisesToTheCorners([Values(3, 7)] int cuias)
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(cuias, 780f, 1400f, IntroPerch.Branch);
            plan.Plan(input);
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch));
            float low = float.MinValue;
            for (float x = -IntroChoreography.BranchOverhangDp; x <= 360f + IntroChoreography.BranchOverhangDp; x += 1f)
            {
                low = Mathf.Max(low, plan.BranchCurveY(x));
            }

            Assert.That(low, Is.EqualTo(input.PanelTop - IntroChoreography.LowBranchAbovePanelDp).Within(0.05f), "punto más bajo");
            Assert.That(low + 3f, Is.LessThanOrEqualTo(input.PanelTop - IntroChoreography.SheetAbovePanelDp - 1f + 0.05f), "por encima de la lámina");
            Assert.That(plan.BranchCurveY(-IntroChoreography.BranchOverhangDp), Is.LessThan(low - 5f), "sube hacia las esquinas");
        }

        [Test]
        public void Exit_HeadsToTheNearestCorner_AllAtOnce([Values(800f, 1400f)] float t)
        {
            var plan = new IntroChoreography();
            plan.Plan(Input(7, 780f, t, IntroPerch.Branch));
            Assert.That(plan.ExitStartMs, Is.EqualTo(t).Within(0.01f), "salen en T");
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                float x = plan.MonkeyX(m, plan.ExitStartMs);
                int dir = x < 180f ? -1 : 1;
                Assert.That(plan.MonkeyExitDir[m], Is.EqualTo(dir));
                Assert.That(plan.MonkeyFacing(m, plan.ExitStartMs + 1f), Is.EqualTo(dir), "de espaldas al tablero, hacia su esquina");
                Assert.That((plan.MonkeyX(m, plan.ExitStartMs + IntroChoreography.ExitMs) - x) * dir, Is.GreaterThan(0f));
            }
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

        /// <summary>
        /// Frente en lengua (revisión: la lámina y el labio que crecían se leían como una caja translúcida): con
        /// <see cref="WaterStripGraphic.SetTaper"/>, la cresta baja hasta el 15 % en cada punta y llega a la altura completa a
        /// los dp del afinado; sin afinado, la punta tiene la altura completa. Sigue en ≤ 64 cuadriláteros.
        /// </summary>
        [Test]
        public void WaterStrip_TaperedFront_LowersTheCrestAtTheTips()
        {
            var go = new GameObject("Strip", typeof(RectTransform), typeof(CanvasRenderer), typeof(WaterStripGraphic));
            try
            {
                WaterStripGraphic strip = go.GetComponent<WaterStripGraphic>();
                strip.Setup();
                var origin = new Vector2(180f, -200f);
                strip.SetStrip(origin, 60f, 60f, 16f, 24f, false, false);
                Assert.That(TipCrest(strip, origin.y, 120f), Is.GreaterThan(12f), "sin afinado, la punta recta");

                strip.SetTaper(14f, 14f);
                Assert.That(TipCrest(strip, origin.y, 120f), Is.LessThan(16f * 0.2f), "con afinado, la punta baja al 15 %");
                Assert.That(CrestAt(strip, origin.y, origin.x - 60f + 14f), Is.GreaterThan(12f), "a 14 dp de la punta, altura completa");
                Assert.That(Vertices(strip) / 4, Is.LessThanOrEqualTo(WaterStripGraphic.MaxQuads));

                strip.SetStrip(origin, 3f, 3f, 16f, 24f, false, false);
                Assert.That(Vertices(strip), Is.GreaterThan(0), "una lengua corta sigue dibujándose");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // Altura de la cresta (dp sobre el origen) en la punta de atrás de una tira horizontal de largo «length».
        private static float TipCrest(WaterStripGraphic strip, float originY, float length)
        {
            return CrestAt(strip, originY, 180f - (length * 0.5f));
        }

        // Altura máxima de los vértices del cuerpo en x (dp sobre el origen).
        private static float CrestAt(WaterStripGraphic strip, float originY, float x)
        {
            MethodInfo populate = typeof(WaterStripGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;
            using (var vh = new VertexHelper())
            {
                populate.Invoke(strip, new object[] { vh });
                float best = 0f;
                var v = new UIVertex();
                for (int i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(ref v, i);
                    if (Mathf.Abs(v.position.x - x) < 0.01f)
                    {
                        best = Mathf.Max(best, v.position.y - originY);
                    }
                }

                return best;
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

        /// <summary>
        /// Tropas pequeñas con muchas columnas (churuco: 2 colgados de 60 dp; mono nocturno: 2 en fila): la fruta que ningún
        /// miquito alcanza a tiempo cae de la rama de la tropa, recta sobre su columna, y nunca del cielo por detrás del HUD con
        /// la tropa a la vista (revisión de las hojas 21_sheet_l13 y l82). Con la tropa del mono ardilla, ninguna.
        /// </summary>
        [Test]
        public void Branch_FruitNoMonkeyReaches_FallsFromTheTroopBranch([Values(800f, 1400f)] float t)
        {
            var cases = new (TroopModel Model, int Troop, float Size)[]
            {
                (TroopModel.Hang, 2, 60f), (TroopModel.Row, 2, 38f), (TroopModel.Hang, 3, 34f), (TroopModel.Row, 5, 40f),
            };
            foreach ((TroopModel model, int troop, float size) in cases)
            {
                var plan = new IntroChoreography();
                IntroInput input = Input(7, 780f, t, IntroPerch.Branch);
                input.Model = model;
                input.Troop = troop;
                input.MonkeyDp = size;
                plan.Plan(input);
                Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch));
                int fromBranch = 0;
                for (int k = 0; k < plan.FruitCount; k++)
                {
                    if (plan.FruitMonkey[k] >= 0)
                    {
                        continue;
                    }

                    fromBranch++;
                    string what = model + " × " + troop + ", fruta " + k;
                    Assert.That(plan.FruitReleaseY[k], Is.EqualTo(plan.BranchDropY(plan.FruitImpactX[k])).Within(0.01f), "de la rama de la tropa: " + what);
                    Assert.That(plan.FruitReleaseY[k], Is.GreaterThan(input.HudBottom), "nunca por detrás del HUD: " + what);
                    Assert.That(plan.FruitReleaseX[k], Is.EqualTo(plan.FruitImpactX[k]).Within(0.01f), "recta sobre su columna: " + what);
                    Assert.That(plan.FruitImpactMs[k] - plan.FruitReleaseMs[k], Is.InRange(IntroChoreography.MinFallMs - 0.01f, IntroChoreography.MaxFallMs + 0.01f));
                }

                if (troop == 5)
                {
                    Assert.That(fromBranch, Is.EqualTo(0), "la tropa del mono ardilla suelta todas las frutas");
                }

                Assert.That(fromBranch, Is.LessThanOrEqualTo(plan.FruitCount / 2), "la mayoría, de la mano: " + model + " × " + troop);
            }
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
