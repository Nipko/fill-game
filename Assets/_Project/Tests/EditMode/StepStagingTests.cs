#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation;
using AF.Presentation.Mechanics;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Estados intermedios de un vertido (arquitectura del paso 1, §3 y §4): <see cref="StepStaging"/> reconstruye el aspecto
    /// de cada cuia antes de cada etapa desde <c>Before</c>, <c>After</c> y <c>PourEvents</c>. Es interpolación para pintar:
    /// el trabajo del vertido deja el origen y la tocada en <see cref="StepStaging.JobEnd"/>, cada paso empieza donde acabó el
    /// anterior de esa cuia y el último deja <c>After[i]</c>.
    /// </summary>
    public sealed class StepStagingTests
    {
        private readonly StepEntry[] _plan = new StepEntry[MechanicStepPlan.MaxSteps];

        [Test]
        public void JobEnd_Bite_VictimKeepsDose() // a mitad del mordisco la víctima aún tiene la dosis
        {
            PourVisual v = StepBoards.Bite(out int cave, out int victim);
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(StepStaging.JobEnd(in v, _plan, n, victim, out Cuia end, out bool sealedNow), Is.True, "la víctima tiene un paso pendiente");
            Assert.That(sealedNow, Is.False);
            Assert.That(end.Count, Is.EqualTo(v.After[victim].Count + 1), "el trabajo la deja con la dosis mordida");
            Assert.That(end.TopColor, Is.EqualTo(v.After[cave].TopColor), "la fruta que se lleva la cueva");
            Assert.That(StepStaging.Pre(in v, cave, StepStage.E), Is.EqualTo(v.Before[cave]), "la cueva, como estaba (contador incluido)");
            Assert.That(StepStaging.JobEnd(in v, _plan, n, v.Source, out Cuia source, out _), Is.False, "el origen no tiene pasos");
            Assert.That(source, Is.EqualTo(v.After[v.Source]));
        }

        [Test]
        public void Pre_Open_KeepsClosedMechanic() // la hoja se ve hasta el paso Open
        {
            PourVisual hoja = StepBoards.HojaOpen(out int leaf);
            Cuia pre = StepStaging.Pre(in hoja, leaf, StepStage.B);
            Assert.That(pre.Mechanic, Is.EqualTo(MechanicType.Hoja));
            Assert.That(pre.MechanicFlags, Is.EqualTo(Cuia.FlagClosed), "puesta");
            Assert.That(pre.MechanicKey, Is.EqualTo(hoja.Before[leaf].MechanicKey));
            Assert.That(pre.Count, Is.EqualTo(hoja.After[leaf].Count), "con lo vertido dentro");
            Assert.That(StepStaging.Pre(in hoja, leaf, StepStage.C), Is.EqualTo(hoja.After[leaf]), "tras (b), abierta");

            PourVisual brote = StepBoards.BroteOpen(out int bud);
            Assert.That(StepStaging.Pre(in brote, bud, StepStage.B).Mechanic, Is.EqualTo(MechanicType.Brote));
            Assert.That(StepStaging.Pre(in brote, bud, StepStage.B).TopColor, Is.EqualTo(brote.After[bud].TopColor));

            PourVisual liana = StepBoards.LianaSeal(out _, out int tied);
            Assert.That(StepStaging.Pre(in liana, tied, StepStage.D), Is.EqualTo(liana.Before[tied]), "la Liana, atada hasta (d)");
        }

        [Test]
        public void Pre_RainDrop_RemovesTopDose()
        {
            PourVisual v = StepBoards.LluviaLastDrop(out int rain);
            Cuia pre = StepStaging.Pre(in v, rain, StepStage.E);
            Assert.That(pre.Count, Is.EqualTo(v.After[rain].Count - 1), "sin la gota");
            Assert.That(pre, Is.EqualTo(v.Before[rain]), "con la nube, las gotas y el contador de Before");
            Assert.That(StepStaging.PreSealed(in v, rain, StepStage.E), Is.False);
            Assert.That(StepStaging.PreSealed(in v, rain, StepStage.CPrime), Is.False, "la tapa cae en (c′)");
            Assert.That(StepStaging.PreSealed(in v, rain, StepStage.DPrime), Is.True);
            Assert.That(StepStaging.Pre(in v, rain, StepStage.CPrime), Is.EqualTo(v.After[rain]), "tras la gota, el agua completa");
        }

        [Test]
        public void Pre_Receive_PartnerBeforeTheBottomInsert()
        {
            PourVisual v = StepBoards.Remolino(out int touched, out int partner);
            Assert.That(StepStaging.Pre(in v, partner, StepStage.A), Is.EqualTo(v.Before[partner]), "las dosis aún no han entrado por el fondo");
            Assert.That(StepStaging.Pre(in v, touched, StepStage.A), Is.EqualTo(v.After[touched]), "la tocada no cambia");
        }

        [Test]
        public void LastStep_EqualsAfter([Values("liana", "matapi", "lluvia", "drop_seal_release", "bite", "bite_lost", "reveal", "mud", "remolino", "hoja", "brote")] string scenario)
        {
            PourVisual v = Scenario(scenario);
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(n, Is.GreaterThan(0), scenario + ": con pasos");
            for (int c = 0; c < v.After.Count; c++)
            {
                int first = StepStaging.FirstStage(_plan, n, c);
                if (first < 0)
                {
                    continue;
                }

                // Cada paso de la cuia empieza donde acabó el anterior (el primero, donde la deja el trabajo) y el último
                // deja After[c] con su tapa.
                StepStaging.JobEnd(in v, _plan, n, c, out Cuia shown, out _);
                bool shownSealed = StepStaging.PreSealed(in v, c, (StepStage)first);
                for (int s = first; s >= 0; s = StepStaging.NextStage(_plan, n, c, s))
                {
                    StepStaging.StepStates(in v, _plan, n, c, (StepStage)s, out Cuia before, out Cuia after, out bool afterSealed);
                    Assert.That(before, Is.EqualTo(shown), scenario + ", cuia " + c + ": el paso de " + (StepStage)s + " empieza donde acabó el anterior");
                    shown = after;
                    shownSealed = afterSealed;
                }

                Assert.That(shown, Is.EqualTo(v.After[c]), scenario + ", cuia " + c + ": el último paso deja After");
                Assert.That(shownSealed, Is.EqualTo(v.After.IsSealed(c)), scenario + ", cuia " + c + ": y su tapa");
            }
        }

        [Test]
        public void Pre_DoesNotAllocate()
        {
            PourVisual v = StepBoards.DropSealsAndReleases(out int rain, out int liana);
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(() =>
            {
                StepStaging.Pre(in v, rain, StepStage.E);
                StepStaging.Pre(in v, liana, StepStage.DPrime);
                StepStaging.JobEnd(in v, _plan, n, v.Receiver, out _, out _);
                StepStaging.StepStates(in v, _plan, n, rain, StepStage.E, out _, out _, out _);
            }, Is.Not.AllocatingGCMemory());
        }

        private static PourVisual Scenario(string name)
        {
            switch (name)
            {
                case "liana":
                    return StepBoards.LianaSeal(out _, out _);
                case "matapi":
                    return StepBoards.MatapiSeal(out _);
                case "lluvia":
                    return StepBoards.LluviaLastDrop(out _);
                case "drop_seal_release":
                    return StepBoards.DropSealsAndReleases(out _, out _);
                case "bite":
                    return StepBoards.Bite(out _, out _);
                case "bite_lost":
                    return StepBoards.BiteLostOnSeal(out _, out _);
                case "reveal":
                    return StepBoards.Reveal(out _);
                case "mud":
                    return StepBoards.MudWash(out _);
                case "remolino":
                    return StepBoards.Remolino(out _, out _);
                case "hoja":
                    return StepBoards.HojaOpen(out _);
                default:
                    return StepBoards.BroteOpen(out _);
            }
        }
    }
}
