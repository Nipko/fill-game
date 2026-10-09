#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// M10 Matapi (D-007, debut 63; propuesta de D-007 b.2 M10, reglas 1-8): nunca es origen; como destino, la regla
    /// normal; nunca fútil como destino; acaba sellado o vacío. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class MatapiTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia Matapi(int capacity, params int[] colorsBottomToTop)
        {
            return C(capacity, colorsBottomToTop).WithMechanic(MechanicType.Matapi);
        }

        [Test]
        public void M10_Regla1_AMatapiAsSource_IsAnIllegalPour_R11()
        {
            // D-007, regla 1: nunca es origen; el toque como origen es un vertido ilegal (R11: sin movimiento ni texto).
            var state = new BoardState(new[] { Matapi(4, 1, 1), C(4, 2, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2) });

            Assert.That(PourRules.CanBeSource(state, 0), Is.False);
            for (int b = 1; b < state.Count; b++)
            {
                Assert.That(PourRules.IsLegal(state, 0, b), Is.False);
                Assert.That(PourRules.Amount(state, 0, b, PourMode.Run), Is.EqualTo(0));
            }
        }

        [Test]
        public void M10_Regla2_AsTarget_FollowsTheNormalRule()
        {
            // Vacío acepta cualquier fruta; con contenido, solo la de su cima; en run entra el tramo que cabe.
            var empty = new BoardState(new[] { Matapi(4), C(4, 2, 1, 1), C(4, 1, 2, 2), Cuia.Empty(4) });
            var started = new BoardState(new[] { Matapi(4, 1, 1), C(4, 2, 1, 1), C(4, 1, 2, 2), C(4, 2) });

            Assert.That(PourRules.IsLegal(empty, 1, 0), Is.True);
            Assert.That(PourRules.IsLegal(empty, 2, 0), Is.True);
            Assert.That(PourRules.Amount(empty, 2, 0, PourMode.Run), Is.EqualTo(2));
            Assert.That(PourRules.IsLegal(started, 1, 0), Is.True);
            Assert.That(PourRules.Amount(started, 1, 0, PourMode.Run), Is.EqualTo(2));
            Assert.That(PourRules.IsLegal(started, 2, 0), Is.False);
        }

        [Test]
        public void M10_Regla5_AsTarget_IsNeverFutile()
        {
            // Anexo B: el Matapi cambia al recibir; verter una monocolor en él compromete la fruta: no es fútil.
            var state = new BoardState(new[] { Matapi(4), C(4, 1, 1), C(4, 2, 2, 1, 1), C(4, 2, 2) });
            var plain = new BoardState(new[] { Cuia.Empty(4), C(4, 1, 1), C(4, 2, 2, 1, 1), C(4, 2, 2) });

            Assert.That(PourRules.IsFutile(state, 1, 0), Is.False);
            Assert.That(PourRules.IsFutile(plain, 1, 0), Is.True);
        }

        [Test]
        public void M10_Regla4_MustEndSealedOrEmpty_R12()
        {
            // R12 sin cambios: un Matapi con dosis que no está sellado impide la victoria; lleno de su fruta, sella.
            var partial = new BoardState(new[] { Matapi(4, 1, 1), C(4, 1, 1), C(4, 2, 2, 2, 2) });
            BoardState sealedAfter = PourRules.Apply(partial, 1, 0, PourMode.Run, out PourEvents events);

            Assert.That(partial.IsWon(), Is.False);
            Assert.That(events.WasSealed(0), Is.True);
            Assert.That(sealedAfter[0].Mechanic, Is.EqualTo(MechanicType.Matapi));
            Assert.That(sealedAfter.IsWon(), Is.True);
        }

        [Test]
        public void M10_WhatGoesInNeverComesOut()
        {
            // Lo que entra no sale: tras recibir sigue sin ser origen (no se abre).
            var state = new BoardState(new[] { Matapi(4), C(4, 2, 1, 1), C(4, 1, 2, 2), Cuia.Empty(4) });

            BoardState after = PourRules.Apply(state, 1, 0, PourMode.Run, out PourEvents events);

            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.Matapi));
            Assert.That(events.OpenedMask, Is.EqualTo(0));
            Assert.That(PourRules.CanBeSource(after, 0), Is.False);
        }

        [Test]
        public void M10_Validation_EmptyOrOneToThreeVisibleDosesOfOneColor_InItsHome()
        {
            // D-007 reglas 3 y 6; [P] derivado de R12: con dosis, cap = U_c de su color.
            int[] caps = { 4, 4, 4, 4, 2 };
            int[][] cuias = { new[] { 1, 1 }, new[] { 2, 1, 2 }, new[] { 1, 2 }, new int[0], new[] { 2 } };
            BoardState Build(int index, int[][]? hidden = null)
            {
                var attrs = new MechanicSpec[5];
                attrs[index] = new MechanicSpec(MechanicType.Matapi);
                BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs, hidden);
                Assert.That(built.IsValid, Is.True, built.Error);
                return built.State!;
            }

            Assert.That(LevelValidation.Validate(Build(0)), Is.Null);
            Assert.That(LevelValidation.Validate(Build(3)), Is.Null);
            Assert.That(LevelValidation.Validate(Build(1)), Does.Contain("un solo color"));
            Assert.That(LevelValidation.Validate(Build(2)), Does.Contain("un solo color"));
            Assert.That(LevelValidation.Validate(Build(0, new[] { new[] { 0, 0 } })), Does.Contain("ocultas"));
            caps[0] = 2;
            caps[4] = 4;
            Assert.That(LevelValidation.Validate(Build(0)), Does.Contain("U_c"));
        }
    }
}
