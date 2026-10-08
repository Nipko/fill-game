#nullable enable
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Líquido de la cuia inclinada al verter (guía visual v2, 6.4; revisión visual del 2026-10-07): las capas siguen
    /// horizontales y su altura sale del volumen, llenando la cavidad girada desde su punto más bajo (sin la cuña vacía
    /// junto al pie). Y el horizonte del panorama según el panel (misma revisión).
    /// </summary>
    public sealed class LiquidVolumeTests
    {
        private const int Capacity = 4;
        private const float DoseArea = 2f * CuiaGeometry.CavityHalfWidthU; // 1,30 u² por dosis

        [Test]
        public void Upright_TheLevelIsTheBottomPlusTheDoses()
        {
            Assert.That(LiquidGraphic.LevelForVolume(2f * DoseArea, 0f, Capacity), Is.EqualTo(CuiaGeometry.CavityBottomU + 2f).Within(1e-4f));
            Assert.That(LiquidGraphic.AreaBelow(CuiaGeometry.CavityBottomU + 3f, 0f, Capacity), Is.EqualTo(3f * DoseArea).Within(0.02f));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void Tilted38_TheSurfaceHoldsTheSameVolume(int doses)
        {
            foreach (float deg in new[] { BoardAnimator.TiltDeg, -BoardAnimator.TiltDeg })
            {
                float slope = -Mathf.Tan(deg * Mathf.Deg2Rad);
                float level = LiquidGraphic.LevelForVolume(doses * DoseArea, slope, Capacity);
                Assert.That(LiquidGraphic.AreaBelow(level, slope, Capacity), Is.EqualTo(doses * DoseArea).Within(0.01f * doses * DoseArea), deg + "°");
            }
        }

        [Test]
        public void Tilted38_OneDose_FillsTheLowCorner_WithoutAnEmptyWedge()
        {
            // Cuia girada 38° en sentido antihorario (vierte a la izquierda): el lado bajo es x − 1 < 0. El líquido cubre la
            // esquina baja del fondo y sube más por la pared baja que por la alta.
            float slope = -Mathf.Tan(BoardAnimator.TiltDeg * Mathf.Deg2Rad);
            float level = LiquidGraphic.LevelForVolume(DoseArea, slope, Capacity);
            float w = CuiaGeometry.CavityHalfWidthU;
            float atLowWall = level + (slope * -w);
            float atHighWall = level + (slope * w);
            Assert.That(atLowWall, Is.GreaterThan(CuiaGeometry.CavityBottomU + 0.5f), "la esquina baja queda llena");
            Assert.That(atLowWall, Is.GreaterThan(atHighWall));
        }

        [Test]
        public void Tilted_LevelsGrowWithTheVolume()
        {
            float slope = -Mathf.Tan(BoardAnimator.TiltDeg * Mathf.Deg2Rad);
            float previous = float.MinValue;
            for (int d = 0; d <= Capacity; d++)
            {
                float level = LiquidGraphic.LevelForVolume(d * DoseArea, slope, Capacity);
                Assert.That(level, Is.GreaterThan(previous));
                previous = level;
            }
        }

        [TestCase(160f, 118f)] // nivel 7 a 360 × 780 (guía 3.3): sin cambios
        [TestCase(206.5f, 142.5f)] // nivel 1 con D-004 (cuias de 64 dp; panel en 206,5 a 360 × 780)
        [TestCase(330f, 200f)] // tope inferior
        [TestCase(108f, 118f)] // tres filas: el horizonte no sube de 118
        public void Horizon_SitsAbovePanelBetween118And200(float panelTop, float horizon)
        {
            Assert.That(GameScreen.HorizonFor(panelTop, 360f), Is.EqualTo(horizon).Within(0.01f));
        }
    }
}
