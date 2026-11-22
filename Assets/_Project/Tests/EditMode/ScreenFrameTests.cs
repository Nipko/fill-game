#nullable enable
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Borde a borde y pantallas grandes (añadido de D-059 del 2026-11-06; 02 §5, «Dispositivos» y «Orientación»; 08 §13.1 #4):
    /// <see cref="ScreenFrame"/> con varias pantallas, zonas seguras y proporciones, y <see cref="ScreenColumn"/> con sus fondos
    /// que llegan al borde, también al girar y al redimensionar.
    /// </summary>
    public sealed class ScreenFrameTests
    {
        // Zona segura de una pantalla de w × h px con márgenes (px) arriba, abajo, a la izquierda y a la derecha.
        private static Rect Safe(int w, int h, float top, float bottom, float left = 0f, float right = 0f)
        {
            return new Rect(left, bottom, w - left - right, h - top - bottom);
        }

        // La columna queda dentro del lienzo y de la zona segura de los lados, y nunca más ancha de 10:16 del alto.
        private static void AssertColumn(ScreenFrame f, string where)
        {
            Assert.That(f.ColumnX, Is.GreaterThanOrEqualTo(f.InsetLeft - 0.01f), where + ": la columna empieza tras el margen izquierdo");
            Assert.That(f.ColumnX + f.ColumnWidth, Is.LessThanOrEqualTo(f.CanvasWidth - f.InsetRight + 0.01f), where + ": y acaba antes del derecho");
            Assert.That(f.ColumnWidth, Is.LessThanOrEqualTo((ScreenFrame.ColumnMaxAspect * f.CanvasHeight) + 0.01f), where + ": 10:16 como mucho");
            Assert.That(f.CanvasHeight, Is.GreaterThanOrEqualTo(GameScreen.MinHeight - 0.01f), where + ": 640 dp de alto como mínimo (02 §5)");
            Assert.That(f.ColumnWidth, Is.GreaterThanOrEqualTo(GameScreen.MinWidth - 0.01f), where + ": 320 dp de ancho como mínimo (02 §5)");
        }

        [Test]
        public void Phones_InPortrait_TheColumnIsTheWholeCanvas_AsAlways(
            [Values(1920, 2160, 2340, 2400, 2520)] int heightPx)
        {
            var safe = Safe(1080, heightPx, 96f, 48f);
            ScreenFrame f = ScreenFrame.Compute(1080, heightPx, safe, 0f);
            Assert.That(f.IsFullWidth, Is.True, f.ToString());
            Assert.That(f.Scale, Is.EqualTo(GameScreen.CanvasScale(1080, heightPx)).Within(1e-5f), "la escala de siempre (02 §5)");
            Assert.That(f.ColumnWidth, Is.EqualTo(360f).Within(0.01f));
            Assert.That(f.InsetTop, Is.EqualTo(96f / f.Scale).Within(0.01f));
            Assert.That(f.InsetBottom, Is.EqualTo(48f / f.Scale).Within(0.01f));
            AssertColumn(f, "teléfono 1080 × " + heightPx);
        }

        [Test]
        public void NarrowPhone_With2PxPerDp_KeepsTheQB115Canvas()
        {
            // 640 × 1400 px a 2 px/dp: 320 × 700 dp físicos, el lienzo de 320 dp (Q-B11-5 (a)).
            ScreenFrame f = ScreenFrame.Compute(640, 1400, Safe(640, 1400, 48f, 32f), 2f);
            Assert.That(f.IsFullWidth, Is.True);
            Assert.That(f.ColumnWidth, Is.EqualTo(320f).Within(0.01f));
            Assert.That(f.Scale, Is.EqualTo(GameScreen.CanvasScale(640, 1400, 2f)).Within(1e-5f));
        }

        [Test]
        public void Tablet16x10_InPortrait_IsTheWholeCanvasOf400x640()
        {
            ScreenFrame f = ScreenFrame.Compute(1600, 2560, Safe(1600, 2560, 48f, 96f), 2f);
            Assert.That(f.IsFullWidth, Is.True, f.ToString());
            Assert.That(f.CanvasWidth, Is.EqualTo(400f).Within(0.01f));
            Assert.That(f.CanvasHeight, Is.EqualTo(640f).Within(0.01f));
            AssertColumn(f, "tableta 16:10 en vertical");
        }

        [Test]
        public void Tablet_InLandscape_ACentredColumnOf400Dp()
        {
            ScreenFrame f = ScreenFrame.Compute(2560, 1600, Safe(2560, 1600, 48f, 96f), 2f);
            Assert.That(f.IsFullWidth, Is.False, f.ToString());
            Assert.That(f.CanvasWidth, Is.EqualTo(1024f).Within(0.01f));
            Assert.That(f.CanvasHeight, Is.EqualTo(640f).Within(0.01f));
            Assert.That(f.ColumnWidth, Is.EqualTo(400f).Within(0.01f));
            Assert.That(f.ColumnX, Is.EqualTo(312f).Within(0.01f), "centrada");
            Assert.That(f.RightBand, Is.EqualTo(312f).Within(0.01f));
            AssertColumn(f, "tableta en apaisado");
        }

        [Test]
        public void FoldableOpen_AlmostSquare_ACentredColumn()
        {
            // Plegable abierto (2208 × 1840 px, 2,6 px/dp): lienzo de 768 × 640 dp.
            ScreenFrame f = ScreenFrame.Compute(2208, 1840, Safe(2208, 1840, 110f, 60f), 2.625f);
            Assert.That(f.CanvasWidth, Is.EqualTo(768f).Within(0.01f));
            Assert.That(f.ColumnWidth, Is.EqualTo(400f).Within(0.01f));
            Assert.That(f.ColumnX, Is.EqualTo(184f).Within(0.01f));
            AssertColumn(f, "plegable abierto");
        }

        [Test]
        public void Phone_InLandscape_WithACutoutAndTheNavigationBarOnTheSide_TheColumnAvoidsBoth()
        {
            // 2340 × 1080 px: recorte de 130 px a la izquierda y barra de 3 botones de 144 px a la derecha.
            var safe = Safe(2340, 1080, 0f, 0f, 130f, 144f);
            ScreenFrame f = ScreenFrame.Compute(2340, 1080, safe, 3f);
            Assert.That(f.CanvasHeight, Is.EqualTo(640f).Within(0.01f), "manda el alto");
            Assert.That(f.ColumnWidth, Is.EqualTo(400f).Within(0.01f));
            Assert.That(f.ColumnX + (f.ColumnWidth * 0.5f), Is.EqualTo(f.CanvasWidth * 0.5f).Within(0.01f), "centrada en la pantalla: hay sitio");
            AssertColumn(f, "teléfono en apaisado");
        }

        [Test]
        public void ColumnNextToABigSideInset_IsPushedInsideTheSafeArea()
        {
            // 1600 × 1080 px (lienzo de 948 × 640) con 600 px tapados a la izquierda: el centro de la pantalla no deja sitio.
            var safe = Safe(1600, 1080, 0f, 0f, 600f, 0f);
            ScreenFrame f = ScreenFrame.Compute(1600, 1080, safe, 0f);
            Assert.That(f.ColumnX, Is.EqualTo(f.InsetLeft).Within(0.01f), "pegada al margen: " + f);
            AssertColumn(f, "con un margen grande a la izquierda");
        }

        [Test]
        public void SplitScreen_HalfAPhone_ACentredColumn()
        {
            // La mitad de arriba de un teléfono en pantalla dividida: 1080 × 1150 px.
            ScreenFrame f = ScreenFrame.Compute(1080, 1150, Safe(1080, 1150, 96f, 0f), 2.625f);
            Assert.That(f.CanvasHeight, Is.EqualTo(640f).Within(0.01f));
            Assert.That(f.IsFullWidth, Is.False, f.ToString());
            AssertColumn(f, "pantalla dividida");
        }

        [Test]
        public void Portrait_WithSideInsets_TheCanvasIsMeasuredInsideThem()
        {
            // Bordes curvos que tapan (cascada) 60 px por lado en 1200 × 2600 px: el lienzo de 360 dp va dentro.
            var safe = Safe(1200, 2600, 96f, 48f, 60f, 60f);
            ScreenFrame f = ScreenFrame.Compute(1200, 2600, safe, 0f);
            Assert.That(f.ColumnWidth, Is.EqualTo(360f).Within(0.01f));
            Assert.That(f.ColumnX, Is.EqualTo(f.InsetLeft).Within(0.01f));
            Assert.That(f.Scale, Is.EqualTo(1080f / 360f).Within(1e-4f));
            AssertColumn(f, "cascada");
        }

        [Test]
        public void AnEmptySafeArea_CountsAsTheWholeScreen()
        {
            ScreenFrame f = ScreenFrame.Compute(1080, 2340, new Rect(0f, 0f, 0f, 0f), 0f);
            Assert.That(f.IsFullWidth, Is.True);
            Assert.That(f.InsetTop, Is.EqualTo(0f));
            Assert.That(f.InsetBottom, Is.EqualTo(0f));
        }

        // ------------------------------------------------------------------------------- ScreenColumn

        [Test]
        public void Column_RotatingAndResizing_MovesTheColumnAndItsBleeds()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            try
            {
                ScreenColumn column = ScreenColumn.Create((RectTransform)canvasGo.transform);
                RectTransform group = UiBuilder.Rect("Group", column.Rect);
                var fill = new GameObject("Fill", typeof(RectTransform));
                fill.transform.SetParent(group, false);
                ScreenColumn.Bleed((RectTransform)fill.transform);
                var placed = new GameObject("Placed", typeof(RectTransform));
                placed.transform.SetParent(column.Rect, false);
                UiBuilder.Place((RectTransform)placed.transform, 10f, 10f, 40f, 40f);
                var loose = new GameObject("Loose", typeof(RectTransform));
                loose.transform.SetParent(placed.transform, false);
                ScreenColumn.Bleed((RectTransform)loose.transform); // no está estirado a la columna: solo se estira

                ScreenFrame portrait = ScreenFrame.Compute(1080, 2340, Safe(1080, 2340, 96f, 48f), 0f);
                column.Apply(portrait);
                AssertStretched(column.Rect, "la columna en vertical");
                Assert.That(ScreenColumn.BleedOf((RectTransform)fill.transform), Is.EqualTo(Vector2.zero), "en vertical no se sale");

                ScreenFrame landscape = ScreenFrame.Compute(2340, 1080, Safe(2340, 1080, 0f, 0f, 130f, 144f), 3f);
                column.Apply(landscape);
                Assert.That(column.Rect.anchorMax.x, Is.EqualTo(0f), "en apaisado, anclada a la izquierda");
                Assert.That(column.Rect.offsetMin.x, Is.EqualTo(landscape.ColumnX).Within(0.001f));
                Assert.That(column.Rect.offsetMax.x - column.Rect.offsetMin.x, Is.EqualTo(landscape.ColumnWidth).Within(0.001f));
                Vector2 bleed = ScreenColumn.BleedOf((RectTransform)fill.transform);
                Assert.That(bleed.x, Is.EqualTo(landscape.ColumnX).Within(0.001f), "el fondo llega al borde izquierdo");
                Assert.That(bleed.y, Is.EqualTo(landscape.RightBand).Within(0.001f), "y al derecho");
                Assert.That(ScreenColumn.BleedOf((RectTransform)loose.transform), Is.EqualTo(Vector2.zero), "lo no estirado no se sale");

                column.Apply(portrait);
                AssertStretched(column.Rect, "de vuelta en vertical");
                Assert.That(ScreenColumn.BleedOf((RectTransform)fill.transform), Is.EqualTo(Vector2.zero), "de vuelta en vertical, sin salirse");
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        private static void AssertStretched(RectTransform rt, string where)
        {
            Assert.That(rt.anchorMin, Is.EqualTo(Vector2.zero), where);
            Assert.That(rt.anchorMax, Is.EqualTo(Vector2.one), where);
            Assert.That(rt.offsetMin, Is.EqualTo(Vector2.zero), where);
            Assert.That(rt.offsetMax, Is.EqualTo(Vector2.zero), where);
        }
    }
}
