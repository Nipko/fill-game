#nullable enable
using AF.Core.Board;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Contenido de la cuia con Aguas turbias y Barro (07 §4.3; 02 §11 T3; 02 §9.1 M5; 02 §4.2): una dosis oculta se pinta de
    /// pardo opaco #8A7B6A con su «?» y nunca con su tinte real; tras revelarse, con su tinte; el Barro tiene color propio a
    /// ΔE00 ≥ 10 de los 14 tintes, de las turbias y de las 3 aguas base (direccion-visual-v2 §2.4, regla 1), con sus grumos.
    /// <see cref="CuiaView"/> en un Canvas de prueba; el arte procedural se crea una vez (≈ 300 ms en EditMode).
    /// </summary>
    public sealed class MurkyMudContentTests
    {
        private ProceduralArt _art = null!;
        private GameObject _canvas = null!;

        [OneTimeSetUp]
        public void CreateArt()
        {
            _art = new ProceduralArt("white");
            _canvas = new GameObject("MurkyMudCanvas", typeof(RectTransform), typeof(Canvas));
        }

        [OneTimeTearDown]
        public void DisposeArt()
        {
            Object.DestroyImmediate(_canvas);
            _art.Dispose();
        }

        [TearDown]
        public void DestroyViews()
        {
            Transform root = _canvas.transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        [Test]
        public void Hidden_PaintedMurkyWithMark() // 07 §4.3; 02 §11 T3
        {
            CuiaView view = NewView();
            Cuia cuia = Cuia.Create(4, new[] { 8, 4, 4, 3 }, 0b0011);
            view.ShowContent(cuia, false);
            for (int k = 0; k < 2; k++)
            {
                Assert.That(Same(view.DoseColor(k), Palette.Murky), Is.True, "dosis oculta " + k + " en pardo opaco");
                Assert.That(Same(view.DoseColor(k), Palette.Tint(cuia.ColorAt(k))), Is.False, "nunca su tinte real");
                Assert.That(view.Murky.MarkVisible(k), Is.True, "«?» en la dosis oculta " + k);
            }

            for (int k = 2; k < 4; k++)
            {
                Assert.That(Same(view.DoseColor(k), Palette.Tint(cuia.ColorAt(k))), Is.True, "dosis visible " + k);
                Assert.That(view.Murky.MarkVisible(k), Is.False, "sin «?» en la dosis visible " + k);
            }

            Assert.That(view.LiquidLevel, Is.EqualTo(4f).Within(0.001f), "las ocultas cuentan para la altura");
            Assert.That(view.ShownCuia, Is.EqualTo(cuia));
        }

        [Test]
        public void Hidden_MarksHideInFlight_AndComeBack()
        {
            CuiaView view = NewView();
            view.ShowContent(Cuia.Create(4, new[] { 8, 4, 4 }, 0b0001), false);
            var layer = new GameObject("PourLayer", typeof(RectTransform));
            layer.transform.SetParent(_canvas.transform, false);
            view.MoveBodyTo((RectTransform)layer.transform);
            Assert.That(view.InFlight, Is.True);
            Assert.That(view.Murky.MarkVisible(0), Is.False, "en vuelo las bandas no giran: el «?» se oculta");
            view.ReturnBody();
            Assert.That(view.Murky.MarkVisible(0), Is.True, "al volver, el «?» reaparece");
        }

        [Test]
        public void Revealed_PaintedWithTint() // 02 §11 T3; 04 §2.5
        {
            CuiaView view = NewView();
            Cuia cuia = Cuia.Create(4, new[] { 8, 4, 4, 3 }, 0b0011);
            view.ShowContent(cuia, false);
            Cuia revealed = cuia.WithRevealed(0b0010);
            view.ShowContent(revealed, false);
            Assert.That(Same(view.DoseColor(1), Palette.Tint(4)), Is.True, "revelada: su tinte");
            Assert.That(view.Murky.MarkVisible(1), Is.False, "sin «?» tras revelarse");
            Assert.That(Same(view.DoseColor(0), Palette.Murky), Is.True, "la otra sigue oculta");
            Assert.That(view.Murky.MarkVisible(0), Is.True);

            view.ShowContent(cuia.WithRevealed(0b0011), false);
            Assert.That(Same(view.DoseColor(0), Palette.Tint(8)), Is.True);
            Assert.That(view.Murky.MarkMask, Is.EqualTo(0), "ningún «?» con todo revelado");
        }

        [Test]
        public void Mud_HasOwnColor_DeltaE00() // 02 §9.1 M5 y §4.2; direccion-visual-v2 §2.4, regla 1
        {
            for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
            {
                Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.Tint(tint)), Is.GreaterThanOrEqualTo(10f), "Barro frente al tinte " + tint);
            }

            Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.Murky), Is.GreaterThanOrEqualTo(10f), "Barro frente a las turbias");
            Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.WaterBlack), Is.GreaterThanOrEqualTo(10f), "Barro frente al agua negra");
            Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.WaterWhite), Is.GreaterThanOrEqualTo(10f), "Barro frente al agua blanca");
            Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.WaterClear), Is.GreaterThanOrEqualTo(10f), "Barro frente al agua clara");

            CuiaView view = NewView();
            view.ShowContent(Cuia.Create(4, new[] { Cuia.MudColor, 8, 8 }), false);
            Assert.That(Same(view.DoseColor(0), Palette.Mud), Is.True, "la dosis de Barro, con su color");
            Assert.That(view.Mud.LumpsVisible(0), Is.True, "con sus grumos");
            Assert.That(view.Mud.LumpsVisible(1), Is.False);
            Assert.That(view.Murky.MarkMask, Is.EqualTo(0), "el Barro no es una dosis oculta");

            view.ShowContent(Cuia.Create(4, new[] { 8, 8, 8 }), false);
            Assert.That(view.Mud.LumpMask, Is.EqualTo(0), "sin Barro, sin grumos");
        }

        [Test]
        public void Tint15_IsMud()
        {
            Assert.That(Palette.Tint(Cuia.MudColor), Is.EqualTo(Palette.Mud));
            Assert.That(Same(Palette.Tint(Cuia.MudColor), Color.magenta), Is.False, "el Barro ya no sale magenta");
        }

        private CuiaView NewView(int capacity = 4)
        {
            return CuiaView.Create((RectTransform)_canvas.transform, 0, capacity, _art, _ => { }, (_, __) => { });
        }

        private static bool Same(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.005f;
        }
    }
}
