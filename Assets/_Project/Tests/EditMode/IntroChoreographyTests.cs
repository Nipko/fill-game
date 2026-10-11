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
        public void Barro_IsNotARun() // 02 §9.1 M5 y §4.2 aplicada al Barro [P]
        {
            var a = new IntroChoreography();
            var b = new IntroChoreography();
            IntroInput inA = Input(3, 780f, 1400f, IntroPerch.Branch);
            IntroInput inB = Input(3, 780f, 1400f, IntroPerch.Branch);
            // En A la dosis 1 de la cuia 0 es Barro; en B está oculta (con otro color real): se planifican igual.
            inA.SetCuia(0, inA.CenterX[0], inA.FootY[0], 0, new[] { 8, AF.Core.Board.Cuia.MudColor, 8, 4 });
            inB.SetCuia(0, inB.CenterX[0], inB.FootY[0], 0, new[] { 8, 13, 8, 4 }, 0b0010);
            a.Plan(inA);
            b.Plan(inB);
            Assert.That(inA.IsInert(0, 1), Is.True);
            Assert.That(a.RunOf(0, 1), Is.EqualTo(-1), "el Barro no forma racha");
            Assert.That(a.RunOf(0, 0), Is.Not.EqualTo(a.RunOf(0, 2)), "el Barro corta la racha de açaí");
            for (int r = 0; r < a.RunCount; r++)
            {
                Assert.That(a.RunTint[r], Is.Not.EqualTo(AF.Core.Board.Cuia.MudColor), "ninguna racha de Barro");
            }

            for (int k = 0; k < a.FruitCount; k++)
            {
                Assert.That(a.FruitTint[k], Is.Not.EqualTo(AF.Core.Board.Cuia.MudColor), "ninguna fruta para el Barro");
                for (int r = a.FruitRunFirst[k]; r <= a.FruitRunLast[k]; r++)
                {
                    Assert.That(a.RunCuia[r] == 0 && a.RunFrom[r] <= 1 && a.RunTo[r] >= 1, Is.False, "ninguna fruta tiñe el Barro");
                }
            }

            AssertSamePlan(a, b);
            Assert.That(a.FirstImpactMs, Is.GreaterThan(0f), "el Barro aparece en el primer impacto, como las ocultas");
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

        /// <summary>
        /// Entrada de la larga. Antes (A3 del guion), la tropa no se movía hasta que la fila 1 estaba llena y entraba a 450 dp/s.
        /// Con D-011, punto 3 (decisión 2 de la sesión principal), entra trepando mientras el río llena: la llegada a su primera
        /// columna es la de siempre (salir cuando la fila 1 está llena a 450 dp/s), así que el calendario no cambia, y la
        /// entrada empieza ≈ 500 ms antes, más despacio. De A3 queda «primero el río»: nadie se mueve antes de que desborde.
        /// </summary>
        [Test]
        public void Branch_Long_TroopEntersWhileTheRiverFills_ArrivesAsBefore_AndReleasesAfterArriving([Values(3, 7)] int cuias)
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(cuias, 780f, 1400f, IntroPerch.Branch);
            plan.Plan(input);
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch));
            Assert.That(plan.MonkeyCount, Is.InRange(2, plan.ColumnCount));
            Assert.That(plan.MonkeyDp, Is.LessThanOrEqualTo(Mathf.Min(input.MonkeyDp, input.PanelTop - input.HudBottom - 10f)));
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                Assert.That(plan.MonkeyX(m, plan.SheetStartMs), Is.EqualTo(plan.MonkeyX(m, 0f)), "nadie se mueve antes de que el río desborde (A3)");
                float start = plan.MonkeyEnterStartMs[m];
                float end = plan.MonkeyEnterEndMs[m];
                Assert.That(start, Is.InRange(plan.SheetStartMs - 0.01f, plan.TroopEnterMs + 0.01f), "sale mientras el río llena");
                Assert.That(end - start, Is.GreaterThanOrEqualTo(Mathf.Min(IntroChoreography.EnterMs, end - plan.SheetStartMs) - 0.01f), "≈ 500 ms de entrada");
                float x0 = plan.MonkeyX(m, 0f);
                Assert.That(x0 < -0.5f * plan.BodyDp || x0 > input.CanvasWidth + (0.5f * plan.BodyDp), Is.True, "en el follaje de su esquina");
                float seat = plan.MonkeySeatX(m);
                Assert.That(end, Is.EqualTo(plan.TroopEnterMs + (Mathf.Abs(seat - x0) / IntroChoreography.MonkeySpeedDpPerMs)).Within(0.05f),
                    "llega cuando llegaba (salir con la fila 1 llena a 450 dp/s): el calendario no cambia");
                for (float t = 0f; t < plan.ExitStartMs + IntroChoreography.ExitMs; t += 5f)
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

        /// <summary>
        /// D-011, punto 3 (decisión 1 de la sesión principal): en la corta, que antes estaba posada desde t = 0, la tropa entra
        /// trepando desde el follaje de su esquina más cercana; la entrada es solo visual: cada miquito llega a su primer sitio
        /// ≥ 50 ms antes de su primera suelta (dura como mucho min(500 ms, primera suelta − 50 ms)) y desde ahí su trayectoria
        /// es la de antes (sus sueltas salen de su mano). Los de una esquina no se adelantan ni se acercan a menos de 1,1 S, y nadie
        /// entra a más de 450 dp/s (R2): quien no llega a tiempo sin correr más (con T corta, el que se sienta más lejos de su
        /// esquina, tras la cola de los de delante) sigue posado desde t = 0, como antes, y nadie entra pasando por encima de él.
        /// </summary>
        [Test]
        public void Branch_Short_TroopClimbsInFromTheNearestCorner_AndSitsBeforeItsFirstRelease([Values(3, 7)] int cuias, [Values(600f, 800f, 1000f)] float t,
            [Values(TroopModel.Row, TroopModel.Hang, TroopModel.Shake)] TroopModel model)
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(cuias, 780f, t, IntroPerch.Branch);
            input.Version = IntroVersion.Short;
            input.Model = model;
            input.MonkeyDp = model == TroopModel.Hang ? 34f : 40f;
            plan.Plan(input);
            Assert.That(plan.IsLong, Is.False);
            Assert.That(plan.MonkeyCount, Is.GreaterThanOrEqualTo(2));
            float w = input.CanvasWidth;
            int entering = 0;
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                float start = plan.MonkeyEnterStartMs[m];
                float end = plan.MonkeyEnterEndMs[m];
                float seat = plan.MonkeySeatX(m);
                float x0 = plan.MonkeyX(m, 0f);
                string who = "miquito " + m;
                if (end - start <= 0f)
                {
                    // Posado desde t = 0, como antes de D-011: los de su esquina que se sientan más lejos tampoco entran.
                    Assert.That(start, Is.EqualTo(0f), who);
                    Assert.That(x0, Is.EqualTo(seat), who + ": posado en su sitio desde t = 0");
                    for (int o = 0; o < plan.MonkeyCount; o++)
                    {
                        bool sameSide = plan.MonkeyFromRight[o] == plan.MonkeyFromRight[m];
                        float dm = plan.MonkeyFromRight[m] ? w - seat : seat;
                        float d = plan.MonkeyFromRight[m] ? w - plan.MonkeySeatX(o) : plan.MonkeySeatX(o);
                        if (o != m && sameSide && plan.MonkeyEnterEndMs[o] > 0f)
                        {
                            Assert.That(d, Is.LessThan(dm), who + ": el miquito " + o + " entra sin pasar por encima de él");
                        }
                    }

                    continue;
                }

                entering++;
                Assert.That(end - start, Is.GreaterThanOrEqualTo(IntroChoreography.MinShortEnterMs - 0.01f), who + ": entra");
                Assert.That(end - start, Is.LessThanOrEqualTo(IntroChoreography.EnterMs + 0.01f), who + ": ≤ 500 ms");
                Assert.That(start, Is.GreaterThanOrEqualTo(0f));
                bool fromLeft = x0 < -0.5f * plan.BodyDp;
                bool fromRight = x0 > w + (0.5f * plan.BodyDp);
                bool middle = Mathf.Abs(seat - (w * 0.5f)) <= 0.5f; // en el centro, el lado de la variación
                Assert.That(middle ? fromLeft || fromRight : (seat < w * 0.5f ? fromLeft : fromRight), Is.True, who + ": desde el follaje de su esquina más cercana");
                Assert.That(plan.MonkeyX(m, start), Is.EqualTo(x0), who + ": quieto en el follaje hasta salir");
                for (float now = start; now < end; now += 2f)
                {
                    Assert.That(Mathf.Abs(plan.MonkeyVelocity(m, now)), Is.LessThanOrEqualTo(IntroChoreography.MonkeySpeedDpPerMs + 0.001f), who + ": ≤ 450 dp/s (R2)");
                }

                for (int k = 0; k < plan.FruitCount; k++)
                {
                    if (plan.FruitMonkey[k] == m)
                    {
                        Assert.That(end, Is.LessThanOrEqualTo(plan.FruitReleaseMs[k] - IntroChoreography.ReleaseAfterArrivalMs + 0.01f), who + ": llega ≥ 50 ms antes de soltar");
                        Assert.That(plan.MonkeyHandX(m, plan.FruitReleaseMs[k]), Is.EqualTo(plan.FruitReleaseX[k]).Within(0.01f), who + ": suelta desde la mano");
                    }
                }
            }

            Assert.That(entering, Is.GreaterThanOrEqualTo(1), "la tropa entra (al menos los que se sientan cerca de su esquina)");
            if (t >= 1000f)
            {
                Assert.That(entering * 2, Is.GreaterThan(plan.MonkeyCount), "con T = 1.000 entra la mayoría");
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
                    bool resting = Mathf.Abs(plan.MonkeyVelocity(m, now)) < 0.0001f && now < plan.ExitStartMs && now >= plan.MonkeyEnterEndMs[m];
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
            float at = plan.ExitStartMs - 1f; // sentados en su asiento (desde D-011 también la corta entra por la rama)
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

        /// <summary>
        /// Salida (D-011, punto 3): antes salían en T (<c>ExitStartMs == T</c>) en 300 ms; ahora reposan ≈ 1.500 ms tras la última
        /// suelta, ya en juego, y salen todos a la vez en 500 ms hacia la esquina más cercana, trepando: los de cada esquina a la
        /// misma velocidad (≤ 450 dp/s), la que lleva al más lejano en 500 ms.
        /// </summary>
        [Test]
        public void Exit_AfterTheRest_HeadsToTheNearestCorner_AllAtOnce([Values(800f, 1400f)] float t)
        {
            var plan = new IntroChoreography();
            plan.Plan(Input(7, 780f, t, IntroPerch.Branch));
            float last = 0f;
            for (int k = 0; k < plan.FruitCount; k++)
            {
                last = Mathf.Max(last, plan.FruitReleaseMs[k]);
            }

            Assert.That(plan.LastReleaseMs, Is.EqualTo(last));
            Assert.That(plan.ExitStartMs, Is.EqualTo(Mathf.Max(t, last + IntroChoreography.RestMs)).Within(0.01f), "salen tras el reposo de 1.500 ms");
            float farthest = 0f;
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                float x = plan.MonkeyX(m, plan.ExitStartMs);
                int dir = x < 180f ? -1 : 1;
                Assert.That(plan.MonkeyExitDir[m], Is.EqualTo(dir));
                Assert.That(plan.MonkeyFacing(m, plan.ExitStartMs + 1f), Is.EqualTo(dir), "de espaldas al tablero, hacia su esquina");
                Assert.That((plan.MonkeyX(m, plan.ExitStartMs + IntroChoreography.ExitMs) - x) * dir, Is.GreaterThan(0f));
                for (float now = plan.ExitStartMs; now < plan.ExitStartMs + IntroChoreography.ExitMs; now += 5f)
                {
                    Assert.That(Mathf.Abs(plan.MonkeyVelocity(m, now)), Is.LessThanOrEqualTo(IntroChoreography.MonkeySpeedDpPerMs + 0.001f), "≤ 450 dp/s");
                }

                float corner = dir < 0 ? -(0.6f * plan.BodyDp) - IntroChoreography.EdgeMarginDp : 360f + (0.6f * plan.BodyDp) + IntroChoreography.EdgeMarginDp;
                farthest = Mathf.Max(farthest, Mathf.Abs(corner - x));
            }

            if (farthest <= IntroChoreography.MonkeySpeedDpPerMs * IntroChoreography.ExitMs)
            {
                bool someoneStillClimbs = false;
                for (int m = 0; m < plan.MonkeyCount; m++)
                {
                    someoneStillClimbs |= Mathf.Abs(plan.MonkeyVelocity(m, plan.ExitStartMs + IntroChoreography.ExitMs - 5f)) > 0.0001f;
                }

                Assert.That(someoneStillClimbs, Is.True, "el más lejano trepa durante los 500 ms de la salida");
            }
        }

        /// <summary>
        /// Reposo en juego (D-011, punto 3): tras su última suelta, cada miquito se queda quieto en su sitio hasta la salida, y la
        /// tropa se ve ≥ 1.200 ms después de T (opaca hasta los últimos 80 ms de la salida) y se ha ido hacia T + 2,2 s; T, el
        /// calendario y el toque no cambian. Con 3, 7 y 13 cuias, larga y corta, y en los tres modelos.
        /// </summary>
        [Test]
        public void Troop_RestsInPlay_VisibleAtLeast1200msAfterT_AndGoneBy2200([Values(3, 7)] int cuias, [Values(800f, 1400f)] float t,
            [Values(TroopModel.Row, TroopModel.Hang, TroopModel.Shake)] TroopModel model)
        {
            var plan = new IntroChoreography();
            IntroInput input = Input(cuias, 780f, t, IntroPerch.Branch);
            input.Model = model;
            input.MonkeyDp = model == TroopModel.Hang ? 34f : 40f;
            plan.Plan(input);
            Assert.That(plan.MonkeyCount, Is.GreaterThanOrEqualTo(2));
            float opaqueUntil = plan.ExitStartMs + IntroChoreography.ExitMs - IntroChoreography.ExitFadeMs;
            Assert.That(opaqueUntil - t, Is.GreaterThanOrEqualTo(1200f), "a la vista ≥ 1.200 ms después de T");
            Assert.That(plan.ExitStartMs + IntroChoreography.ExitMs - t, Is.LessThanOrEqualTo(2200f), "se ha ido hacia T + 2,2 s");
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                float last = -1f;
                for (int k = 0; k < plan.FruitCount; k++)
                {
                    last = plan.FruitMonkey[k] == m ? Mathf.Max(last, plan.FruitReleaseMs[k]) : last;
                }

                float seat = plan.MonkeyX(m, plan.ExitStartMs);
                for (float now = Mathf.Max(t, last + IntroChoreography.GestureTailMs); now < plan.ExitStartMs; now += 10f)
                {
                    Assert.That(plan.MonkeyX(m, now), Is.EqualTo(seat), "miquito " + m + " quieto en su sitio a " + now + " ms");
                }
            }
        }

        // ---- Calendario dorado (D-011, punto 3: T, las fases a-d, la fruta y el toque no cambian) ----

        // Nivel 2 del tutorial (5 cuias, 1 fila; mono ardilla: fila, tropa de 5, 40 dp) y nivel 21 del mundo 2 (8 cuias, 4 + 4;
        // sagui plateado: cuelga, tropa de 3, 34 dp), con sus tintes de motor y la semilla de su número de nivel.
        private static readonly int[][] Level2Board = { new[] { 4, 8, 4, 4 }, new[] { 3, 3, 3, 4 }, new[] { 3, 8, 8, 8 }, Array.Empty<int>(), Array.Empty<int>() };

        private static readonly int[][] Level21Board =
        {
            new[] { 3, 3, 3, 1 }, Array.Empty<int>(), new[] { 8, 4, 10, 8 }, new[] { 1, 1, 1, 10 },
            new[] { 6, 6, 6, 8 }, new[] { 6, 3, 10, 4 }, Array.Empty<int>(), new[] { 4, 4, 8, 10 },
        };

        // Capturado con el planificador de 96f6766 (antes de D-011), con la misma entrada y la misma semilla.
        private static readonly float[] GoldenL2Release = { 829.68097f, 889.5513f, 996.76794f, 1004.68097f, 1064.5514f, 1123.4014f, 1179.6809f };
        private static readonly float[] GoldenL2Impact = { 950f, 1008.3333f, 1115.0332f, 1125f, 1183.3334f, 1241.6666f, 1300f };
        private static readonly float[] GoldenL2ReleaseX = { 45.6f, 113.2f, 180f, 45.6f, 113.2f, 180f, 45.6f };
        private static readonly float[] GoldenL2ReleaseY = { 225.30402f, 227.50902f, 228.24402f, 225.30402f, 227.50902f, 228.24402f, 225.30402f };
        private static readonly int[] GoldenL2Monkey = { 0, 1, 2, 0, 1, 2, 0 };
        private static readonly int[] GoldenL2Cuia = { 0, 1, 2, 0, 1, 2, 0 };
        private static readonly float[] GoldenL2FillEnd = { 493.3846f, 441.69232f, 390f, -1f, -1f };
        private static readonly float[] GoldenL2Bloom = { 950f, 1125f, 1300f, 1008.3333f, 1183.3334f, 1115.0332f, 1241.6666f };
        private static readonly float[] GoldenL2Arrival = { 772.4957f, 798.2735f, 946.71796f };
        private static readonly float[] GoldenL2Seat = { 33.6f, 101.2f, 192f };

        private static readonly float[] GoldenL21Release =
        {
            440.4555f, 449.8305f, 459.2055f, 468.5805f, 477.9555f, 487.3305f, 496.7055f, 536.7055f, 497.3509f, 476.8814f, 497.3509f,
            497.3509f, 476.8814f, 497.3509f, 476.8814f, 497.3509f, 476.8814f,
        };

        private static readonly float[] GoldenL21Impact = { 550f, 559.375f, 568.75f, 578.125f, 587.5f, 596.875f, 606.25f, 646.25f, 700f, 700f, 700f, 700f, 700f, 700f, 700f, 700f, 700f };
        private static readonly float[] GoldenL21ReleaseX = { 54f, 222f, 306f, 54f, 222f, 306f, 222f, 222f, 54f, 138f, 306f, 54f, 138f, 306f, 138f, 306f, 138f };

        private static readonly float[] GoldenL21ReleaseY =
        {
            152.1f, 152.1f, 152.1f, 152.1f, 152.1f, 152.1f, 152.1f, 152.1f, 152.1f, 99.80859f, 152.1f, 152.1f, 99.80859f, 152.1f, 99.80859f, 152.1f, 99.80859f,
        };

        private static readonly int[] GoldenL21Monkey = { 0, 1, 2, 0, 1, 2, 1, 1, 0, -1, 2, 0, -1, 2, -1, 2, -1 };
        private static readonly int[] GoldenL21Cuia = { 0, 2, 3, 0, 2, 3, 2, 2, 4, 5, 7, 4, 5, 7, 5, 7, 5 };
        private static readonly float[] GoldenL21FillEnd = { 263.46155f, -1f, 231.15384f, 263.46155f, 333.0351f, 359.2851f, -1f, 333.0351f };
        private static readonly float[] GoldenL21Bloom = { 550f, 578.125f, 559.375f, 587.5f, 606.25f, 646.25f, 568.75f, 596.875f, 700f, 700f, 700f, 700f, 700f, 700f, 700f, 700f, 700f };
        private static readonly float[] GoldenL21Seat = { 49.239998f, 226.76f, 310.76f };

        /// <summary>
        /// P3 no toca el calendario (D-011, punto 3): con la misma entrada y la misma semilla, el plan de los niveles 2 (larga) y
        /// 21 (corta) da las mismas sueltas, impactos, manos (x e y de la suelta), miquitos, llenados, floraciones, la última
        /// floración en T y T, que el planificador de antes de D-011 (96f6766). La tropa llega a su primer sitio cuando llegaba
        /// (larga) o ≥ 50 ms antes de su primera suelta (corta), y desde ahí hasta T sigue donde estaba. Tolerancia de 0,01 ms
        /// y 0,01 dp (la aritmética de float del motor).
        /// </summary>
        [Test]
        public void Calendar_IsTheSameAsBeforeD011_Level2LongAnd21Short()
        {
            var plan = new IntroChoreography();
            plan.Plan(BoardInput(Level2Board, 1400f, IntroVersion.Long, TroopModel.Row, 5, 40f, 2));
            Assert.That(plan.IsLong, Is.True);
            Assert.That(plan.TotalMs, Is.EqualTo(1400f));
            Assert.That(plan.LastBloomEndMs, Is.EqualTo(1400f).Within(0.01f));
            Assert.That(plan.TroopEnterMs, Is.EqualTo(493.3846f).Within(0.01f), "la fila 1 se llena cuando se llenaba");
            AssertGolden(plan, "nivel 2", GoldenL2Release, GoldenL2Impact, GoldenL2ReleaseX, GoldenL2ReleaseY, GoldenL2Monkey, GoldenL2Cuia, GoldenL2FillEnd, GoldenL2Bloom, GoldenL2Seat);
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                Assert.That(plan.MonkeyEnterEndMs[m], Is.EqualTo(GoldenL2Arrival[m]).Within(0.01f), "nivel 2: el miquito " + m + " llega cuando llegaba");
            }

            plan.Plan(BoardInput(Level21Board, 800f, IntroVersion.Short, TroopModel.Hang, 3, 34f, 21));
            Assert.That(plan.IsLong, Is.False);
            Assert.That(plan.TotalMs, Is.EqualTo(800f));
            Assert.That(plan.LastBloomEndMs, Is.EqualTo(800f).Within(0.01f));
            AssertGolden(plan, "nivel 21", GoldenL21Release, GoldenL21Impact, GoldenL21ReleaseX, GoldenL21ReleaseY, GoldenL21Monkey, GoldenL21Cuia, GoldenL21FillEnd, GoldenL21Bloom, GoldenL21Seat);
        }

        private static void AssertGolden(IntroChoreography plan, string level, float[] release, float[] impact, float[] releaseX, float[] releaseY, int[] monkey,
            int[] cuia, float[] fillEnd, float[] bloom, float[] seat)
        {
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch), level);
            Assert.That(plan.FruitCount, Is.EqualTo(release.Length), level + ": frutas");
            Assert.That(plan.RunCount, Is.EqualTo(bloom.Length), level + ": rachas");
            Assert.That(plan.MonkeyCount, Is.EqualTo(seat.Length), level + ": miquitos");
            for (int k = 0; k < release.Length; k++)
            {
                string what = level + ", fruta " + k + " (actual: suelta " + plan.FruitReleaseMs[k].ToString("R") + ", impacto " + plan.FruitImpactMs[k].ToString("R") + ")";
                Assert.That(plan.FruitReleaseMs[k], Is.EqualTo(release[k]).Within(0.01f), what);
                Assert.That(plan.FruitImpactMs[k], Is.EqualTo(impact[k]).Within(0.01f), what);
                Assert.That(plan.FruitReleaseX[k], Is.EqualTo(releaseX[k]).Within(0.01f), what);
                Assert.That(plan.FruitReleaseY[k], Is.EqualTo(releaseY[k]).Within(0.01f), what);
                Assert.That(plan.FruitMonkey[k], Is.EqualTo(monkey[k]), what);
                Assert.That(plan.FruitCuia[k], Is.EqualTo(cuia[k]), what);
            }

            for (int i = 0; i < fillEnd.Length; i++)
            {
                Assert.That(plan.FillEndMs[i], Is.EqualTo(fillEnd[i]).Within(0.01f), level + ", llenado de la cuia " + i);
            }

            for (int r = 0; r < bloom.Length; r++)
            {
                Assert.That(plan.RunBloomMs[r], Is.EqualTo(bloom[r]).Within(0.01f), level + ", floración " + r);
            }

            for (int m = 0; m < seat.Length; m++)
            {
                Assert.That(plan.MonkeySeatX(m), Is.EqualTo(seat[m]).Within(0.01f), level + ": primer sitio del miquito " + m);
                for (int k = 0; k < release.Length; k++)
                {
                    if (monkey[k] == m)
                    {
                        Assert.That(plan.MonkeyEnterEndMs[m], Is.LessThanOrEqualTo(release[k] - IntroChoreography.ReleaseAfterArrivalMs + 0.01f), level + ": llega antes de soltar");
                    }
                }

                for (float t = plan.MonkeyEnterEndMs[m]; t <= plan.TotalMs; t += 5f)
                {
                    Assert.That(plan.MonkeyX(m, t), Is.EqualTo(seat[m]).Within(0.01f), level + ": el miquito " + m + " sigue en su sitio a " + t + " ms");
                }
            }
        }

        // Tablero real medido con BoardLayout como en Input (360 × 780), con su especie y su semilla.
        private static IntroInput BoardInput(int[][] board, float totalMs, IntroVersion version, TroopModel model, int troop, float size, int seed)
        {
            int count = board.Length;
            float topRow = 32f + 8f;
            float boardTop = topRow + 48f;
            float trayTop = 780f - 24f - 16f - 80f;
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
                Perch = IntroPerch.Branch,
                Model = model,
                Troop = troop,
                MonkeyDp = size,
                Seed = seed,
                Version = version,
            };
            for (int r = 0; r < l.Rows; r++)
            {
                input.RowFootY[r] = l.RowFootY[r];
            }

            for (int i = 0; i < count; i++)
            {
                input.SetCuia(i, l.CenterX[i], l.FootY[i], l.RowOf[i], board[i]);
            }

            return input;
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
