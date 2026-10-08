#nullable enable
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Líquido de la cuia inclinada al verter (guía visual v2, 6.4; revisión visual del 2026-10-07; D-006): las capas siguen
    /// horizontales y su posición sale del volumen, llenando la silueta interior girada desde su punto más bajo (sin la
    /// cuña vacía junto al pie), de 0° a 92° (92° − 9° × dosis que quedan, como Magic Sort), con la línea en forma normal
    /// (<see cref="LiquidGraphic.LineOffset"/>) para que no haya NaN cerca de 90°. Y el horizonte del panorama según el panel
    /// (misma revisión).
    /// </summary>
    public sealed class LiquidVolumeTests
    {
        private const int Capacity = 4;
        private const float DoseArea = 2f * CuiaGeometry.CavityHalfWidthU; // 1,60 u² por dosis (D-006)

        [Test]
        public void DoseArea_Is1_60()
        {
            Assert.That(DoseArea, Is.EqualTo(1.60f).Within(1e-4f));
        }

        [Test]
        public void Upright_TheLevelIsTheBottomPlusTheDoses()
        {
            Assert.That(LiquidGraphic.LevelForVolume(2f * DoseArea, 0f, Capacity), Is.EqualTo(CuiaGeometry.CavityBottomU + 2f).Within(1e-4f));
            Assert.That(LiquidGraphic.AreaBelow(CuiaGeometry.CavityBottomU + 3f, 0f, Capacity), Is.EqualTo(3f * DoseArea).Within(0.02f));
            Assert.That(LiquidGraphic.LevelForVolume(4f * DoseArea, 0f, Capacity), Is.EqualTo(CuiaGeometry.CavityTopU(Capacity)).Within(1e-4f));
        }

        [Test]
        public void Tilted_TheSurfaceHoldsTheSameVolume_WhenItFitsUnderTheLip(
            [Values(0f, 38f, 65f, 74f, 83f)] float angle,
            [Values(1, 2, 3, 4)] int doses)
        {
            // ±1 % del volumen con 1-4 dosis, las que quepan bajo el labio; si no caben, la superficie se recorta en el labio.
            foreach (float deg in new[] { angle, -angle })
            {
                float volume = doses * DoseArea;
                float lipLevel = LiquidGraphic.LipLevel(deg, Capacity);
                float fits = LiquidGraphic.AreaBelow(lipLevel, deg, Capacity);
                float level = LiquidGraphic.LevelForVolume(volume, deg, Capacity);
                Assert.That(float.IsNaN(level), Is.False, deg + "°");
                if (volume <= fits)
                {
                    Assert.That(LiquidGraphic.AreaBelow(level, deg, Capacity), Is.EqualTo(volume).Within(0.01f * volume), deg + "°, " + doses + " dosis");
                }
                else
                {
                    Assert.That(level, Is.EqualTo(lipLevel).Within(1e-4f), deg + "°, " + doses + " dosis: recortada en el labio");
                }
            }
        }

        [TestCase(0, 92f)]
        [TestCase(1, 83f)]
        [TestCase(2, 74f)]
        [TestCase(3, 65f)]
        public void MagicSortPose_TheRemainingDosesFitUnderTheLip(int remaining, float angle)
        {
            // D-006: al terminar cada dosis el origen está a 92° − 9° × dosis que quedan; esas dosis caben bajo el labio.
            Assert.That(92f - (9f * remaining), Is.EqualTo(angle).Within(1e-4f));
            float fits = LiquidGraphic.AreaBelow(LiquidGraphic.LipLevel(angle, Capacity), angle, Capacity);
            Assert.That(fits, Is.GreaterThanOrEqualTo((remaining * DoseArea) - 1e-3f));
            float level = LiquidGraphic.LevelForVolume(remaining * DoseArea, angle, Capacity);
            Assert.That(LiquidGraphic.AreaBelow(level, angle, Capacity), Is.EqualTo(remaining * DoseArea).Within((0.01f * remaining * DoseArea) + 1e-4f));
        }

        [Test]
        public void Tilted38_OneDose_FillsTheLowCorner_WithoutAnEmptyWedge()
        {
            // Cuia girada 38° en sentido antihorario (vierte a la izquierda): el lado bajo es X = x − 1 < 0. El líquido cubre
            // la esquina baja del fondo y sube más por la pared baja que por la alta.
            const float deg = 38f;
            float d = LiquidGraphic.LevelForVolume(DoseArea, deg, Capacity);
            float w = CuiaGeometry.CavityHalfWidthU;
            float sn = Mathf.Sin(deg * Mathf.Deg2Rad);
            float cs = Mathf.Cos(deg * Mathf.Deg2Rad);
            float atLowWall = (d + (w * sn)) / cs; // y donde la superficie toca la pared X = −w
            float atHighWall = (d - (w * sn)) / cs;
            Assert.That(atLowWall, Is.GreaterThan(CuiaGeometry.CavityBottomU + 0.5f), "la esquina baja queda llena");
            Assert.That(atLowWall, Is.GreaterThan(atHighWall));
        }

        [Test]
        public void Tilted_LevelsGrowWithTheVolume()
        {
            foreach (float deg in new[] { 38f, 65f, 74f, 83f })
            {
                float previous = float.MinValue;
                for (int d = 0; d <= Capacity; d++)
                {
                    float level = LiquidGraphic.LevelForVolume(d * DoseArea, deg, Capacity);
                    Assert.That(level, Is.GreaterThanOrEqualTo(previous), deg + "°, " + d + " dosis");
                    if (d * DoseArea <= LiquidGraphic.AreaBelow(LiquidGraphic.LipLevel(deg, Capacity), deg, Capacity))
                    {
                        Assert.That(level, Is.GreaterThan(previous), deg + "°, " + d + " dosis (cabe: sube)");
                    }

                    previous = level;
                }
            }
        }

        [TestCase(90f)]
        [TestCase(92f)]
        [TestCase(-90f)]
        [TestCase(-92f)]
        public void NearlyHorizontal_HasNoNaN_AndTheLevelIsMonotonicInTheVolume(float deg)
        {
            float previous = float.MinValue;
            float lipLevel = LiquidGraphic.LipLevel(deg, Capacity);
            for (int i = 0; i <= 16; i++)
            {
                float volume = i * 0.25f * DoseArea;
                float level = LiquidGraphic.LevelForVolume(volume, deg, Capacity);
                float area = LiquidGraphic.AreaBelow(level, deg, Capacity);
                Assert.That(float.IsNaN(level) || float.IsInfinity(level), Is.False, deg + "°, volumen " + volume);
                Assert.That(float.IsNaN(area), Is.False, deg + "°, área");
                Assert.That(level, Is.GreaterThanOrEqualTo(previous - 1e-5f), deg + "°: monótono, volumen " + volume);
                Assert.That(level, Is.LessThanOrEqualTo(lipLevel + 1e-5f), deg + "°: nunca sobre el labio");
                Assert.That(area, Is.LessThanOrEqualTo(volume + (0.01f * DoseArea) + 1e-4f), deg + "°: lo que se ve no supera el volumen");
                previous = level;
            }
        }

        [TestCase(162.1f, 118f)] // nivel 7 a 360 × 780 (panel en 162,1 con las cuias de 5,60 u, D-006): sin cambios
        [TestCase(226.9f, 162.9f)] // nivel 1 (cuias de 64 × 179,2 dp; panel en 226,9 a 360 × 780; D-004 y D-006)
        [TestCase(330f, 200f)] // tope inferior
        [TestCase(108f, 118f)] // tres filas: el horizonte no sube de 118
        public void Horizon_SitsAbovePanelBetween118And200(float panelTop, float horizon)
        {
            Assert.That(GameScreen.HorizonFor(panelTop, 360f), Is.EqualTo(horizon).Within(0.01f));
        }
    }
}
