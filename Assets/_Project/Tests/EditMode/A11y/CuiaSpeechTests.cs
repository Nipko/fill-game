#nullable enable
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Etiquetas del lector de pantalla y nombres de C9 (bloque B9; 02 §12, fila «Lector de pantalla», con sus ejemplos literales,
    /// y §3.1 C9), con la tabla del proyecto más las filas de <c>B9-strings.csv</c> (<see cref="B9Loc"/>). Los nombres de las frutas
    /// son los de <c>facts.csv</c> (<c>tint.&lt;tinte&gt;.name</c>), con mayúscula («Açaí»).
    /// </summary>
    public sealed class CuiaSpeechTests
    {
        private const int Acai = 8;
        private const int Aguaje = 4;
        private const int CamuCamu = 6;

        [SetUp]
        public void SetUp()
        {
            B9Loc.ConfigureEs419();
        }

        [TearDown]
        public void TearDown()
        {
            LocSetUpFixture.ConfigureEs419();
        }

        [Test]
        public void TheB9Rows_HaveEveryKey()
        {
            Assert.That(B9Loc.HasAllKeys(), Is.True, "B9-strings.csv (o strings.csv tras B9-loc.diff) trae todas las claves de A11yText");
        }

        [Test]
        public void Cuia_ReadsItsDosesBottomToTop_AndItsHoles()
        {
            BoardState state = Board(Cuia.Empty(4), Cuia.Empty(4), Cuia.Create(4, new[] { Acai, Acai, Aguaje }));
            Assert.That(CuiaSpeech.Label(state, 2), Is.EqualTo("cuia 3: Açaí, Açaí, Aguaje, 1 hueco"), "02 §12, el ejemplo literal");
        }

        [Test]
        public void Cuia_FullAndEmpty()
        {
            BoardState state = Board(Cuia.Create(4, new[] { Acai, Acai, Acai, Acai }), Cuia.Empty(4));
            Assert.That(CuiaSpeech.Label(state, 0), Is.EqualTo("cuia 1: Açaí, Açaí, Açaí, Açaí"));
            Assert.That(CuiaSpeech.Label(state, 1), Is.EqualTo("cuia 2: 4 huecos"));
        }

        [Test]
        public void HiddenDoses_AreReadAsMurky_WithoutTheirFruit()
        {
            BoardState state = Board(Cuia.Create(4, new[] { Aguaje, CamuCamu, Acai }, 0b011));
            string label = CuiaSpeech.Label(state, 0);
            Assert.That(label, Is.EqualTo("cuia 1: turbia, turbia, Açaí, 1 hueco"), "02 §12: las dosis ocultas se leen «turbia»");
            Assert.That(label, Does.Not.Contain("Aguaje").And.Not.Contain("Camu"));
        }

        [Test]
        public void Tinaja_TellsOnlyTheMouthAndHowManyDoses()
        {
            Cuia jar = Cuia.Create(5, new[] { Acai, Aguaje, CamuCamu }, 0b011).WithMechanic(MechanicType.Tinaja);
            Cuia cup = Cuia.Create(2, new[] { Acai }).WithMechanic(MechanicType.Cucharon);
            BoardState state = Board(jar, cup);
            string label = CuiaSpeech.Label(state, 0);
            Assert.That(label, Is.EqualTo("tinaja: Camu camu en la boca, 3 dosis dentro"), "02 §12; D-019");
            Assert.That(label, Does.Not.Contain("turbia").And.Not.Contain("hueco").And.Not.Contain("Aguaje"));
            Assert.That(CuiaSpeech.Label(state, 1), Is.EqualTo("su cuia chica: Açaí, 1 hueco"), "02 §12; D-019");
        }

        [Test]
        public void Names_GoTopToBottom_OnePerLine_AndTheTinajaNeverShowsItsHiddenOnes()
        {
            BoardState state = Board(
                Cuia.Create(4, new[] { Aguaje, Acai, CamuCamu }),
                Cuia.Create(4, new[] { Aguaje, Acai }, 0b001),
                Cuia.Create(5, new[] { Acai, Aguaje, CamuCamu }, 0b011).WithMechanic(MechanicType.Tinaja),
                Cuia.Empty(4));
            Assert.That(CuiaSpeech.Names(state, 0), Is.EqualTo("Camu camu\nAçaí\nAguaje"), "C9: los nombres de sus dosis");
            Assert.That(CuiaSpeech.Names(state, 1), Is.EqualTo("Açaí\nturbia"), "una turbia no dice su fruta");
            Assert.That(CuiaSpeech.Names(state, 2), Is.EqualTo("Camu camu"), "C9 en la Tinaja: solo la boca y lo revelado (D-019)");
            Assert.That(CuiaSpeech.Names(state, 3), Is.Empty);
        }

        [Test]
        public void Names_OfATinajaWithARevealedLayer_IncludeIt()
        {
            BoardState state = Board(Cuia.Create(5, new[] { Acai, Aguaje, CamuCamu }, 0b001).WithMechanic(MechanicType.Tinaja));
            Assert.That(CuiaSpeech.Names(state, 0), Is.EqualTo("Camu camu\nAguaje"), "la capa ya revelada se nombra; la oculta no");
        }

        [Test]
        public void Mud_IsReadAsBarro()
        {
            BoardState state = Board(Cuia.Create(4, new[] { Cuia.MudColor, Acai }));
            Assert.That(CuiaSpeech.Label(state, 0), Is.EqualTo("cuia 1: barro, Açaí, 2 huecos"));
        }

        [Test]
        public void Selection_IsAnnouncedWithTheTopFruit()
        {
            BoardState state = Board(Cuia.Create(4, new[] { Aguaje, Acai }));
            Assert.That(A11yText.Selected(CuiaSpeech.TopName(state, 0)), Is.EqualTo("Açaí seleccionado: elige destino"), "02 §12");
            Assert.That(CuiaSpeech.TopName(Board(Cuia.Empty(4)), 0), Is.Empty);
        }

        [Test]
        public void TrayButtons_SayTheirLabelAndWhatIsLeft()
        {
            Assert.That(TraySpeech.Undo("Deshacer", new UndoStatus(false, 2, 0)), Is.EqualTo("Deshacer, 2 restantes"), "02 §12, el ejemplo literal");
            Assert.That(TraySpeech.Undo("Deshacer", new UndoStatus(false, 1, 0)), Is.EqualTo("Deshacer, 1 restante"));
            Assert.That(TraySpeech.Undo("Deshacer", new UndoStatus(false, 1, 2)), Is.EqualTo("Deshacer, 3 restantes"), "cupo + stock");
            Assert.That(TraySpeech.Undo("Deshacer", new UndoStatus(true, 0, 0)), Is.EqualTo("Deshacer"), "ilimitada");
            Assert.That(TraySpeech.Undo("Deshacer", new UndoStatus(false, 0, 0)), Is.EqualTo("Deshacer"), "sin usos: precio o video");
            Assert.That(TraySpeech.Extra("+1 cuia", new ExtraCuiaOffer(ExtraCuiaState.Ready, 2, 100, 0)), Is.EqualTo("+1 cuia, 2 restantes"));
            Assert.That(TraySpeech.Extra("+1 cuia", new ExtraCuiaOffer(ExtraCuiaState.Ready, 0, 100, 0)), Is.EqualTo("+1 cuia"));
            Assert.That(TraySpeech.Hint("Pista", new HintOffer(HintState.Ready, 1, 50, 0, 0)), Is.EqualTo("Pista, 1 restante"));
            Assert.That(TraySpeech.Shuffle("Barajar", new ShuffleOffer(ShuffleState.Ready, 0, 300, 0)), Is.EqualTo("Barajar"));
        }

        [Test]
        public void Lists_UseThePattern_AndSkipEmptyItems()
        {
            Assert.That(A11yText.Append(string.Empty, "a"), Is.EqualTo("a"));
            Assert.That(A11yText.Append("a", "b"), Is.EqualTo("a, b"));
            Assert.That(A11yText.Append("a", string.Empty), Is.EqualTo("a"), "Q-B9-7: una fruta sin nombre aprobado no se dice");
            Assert.That(A11yText.AppendLine("a", "b"), Is.EqualTo("a\nb"));
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }
    }
}
