#nullable enable
using AF.Presentation;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Silueta de la cuia por dentro (D-006, punto 3, opción (a) de la propuesta §3.4, como Magic Sort: ficha §2.2; 07 §4.1):
    /// 1 dosis = 1 u; cavidad recta de 1,60 u de ancho (≥ 0,80 del exterior); alto exterior 1,60 + capacidad (5,60 u con 4
    /// dosis, que ocupan ≈ 0,72 del alto); se mantienen el pie, el hombro, el cuello corto y el borde de fibra de la
    /// cuia-garrafa, sin labio de botella (trade dress de 07 §1.3).
    /// </summary>
    public sealed class CuiaGeometryTests
    {
        private const float Eps = 1e-3f;

        [Test]
        public void StandardCuia_Is5_60uTall_AndItsFourDosesFill0_72OfIt()
        {
            Assert.That(CuiaGeometry.HeightU(4), Is.EqualTo(5.60f).Within(Eps));
            Assert.That(4f / CuiaGeometry.HeightU(4), Is.InRange(0.71f, 0.73f), "4 dosis / alto exterior (Magic Sort: 0,74)");
            Assert.That(CuiaGeometry.CavityTopU(4), Is.EqualTo(4.60f).Within(Eps));
            Assert.That(CuiaGeometry.LipU(4), Is.EqualTo(5.35f).Within(Eps));
        }

        [Test]
        public void Cavity_IsAtLeast0_80OfTheOuterWidth()
        {
            Assert.That(2f * CuiaGeometry.CavityHalfWidthU / CuiaGeometry.WidthU, Is.GreaterThanOrEqualTo(0.80f - 1e-5f));
            Assert.That(2f * CuiaGeometry.CavityHalfWidthU, Is.EqualTo(1.60f).Within(Eps), "1 dosis = 1,60 u²");
        }

        [Test]
        public void DoseZone_IsAStraightCavity_InsideAGlassWallOfAtLeast0_15u()
        {
            // De 0,60 a 4,60 u (sin incluir el techo) el interior mide 0,80 u en todas partes, y el vidrio, ≥ 0,15 u.
            for (int i = 0; i < 400; i++)
            {
                float y = CuiaGeometry.CavityBottomU + (i * 0.01f);
                Assert.That(CuiaGeometry.InnerHalfWidth(y, 4), Is.EqualTo(0.80f).Within(Eps), "interior en y = " + y);
                Assert.That(CuiaGeometry.OuterHalfWidth(y, 4) - CuiaGeometry.InnerHalfWidth(y, 4), Is.GreaterThanOrEqualTo(0.15f - Eps), "pared en y = " + y);
            }

            Assert.That(CuiaGeometry.InnerHalfWidth(CuiaGeometry.CavityBottomU - 0.01f, 4), Is.EqualTo(0f), "sin cavidad en la base");
        }

        [Test]
        public void Silhouette_KeepsTheGourdShape_WithoutABottleLip()
        {
            // Pie, hombro, cuello corto y borde de fibra (07 §1.3: nada de labio de botella).
            Assert.That(CuiaGeometry.FootTopU, Is.GreaterThan(0f));
            Assert.That(CuiaGeometry.ShoulderU, Is.GreaterThan(0f));
            Assert.That(CuiaGeometry.NeckU, Is.GreaterThan(0f));
            Assert.That(CuiaGeometry.RimU, Is.GreaterThan(0f));
            Assert.That(CuiaGeometry.NeckU, Is.LessThanOrEqualTo(CuiaGeometry.ShoulderU), "cuello corto");
            Assert.That(CuiaGeometry.ShoulderU + CuiaGeometry.NeckU + CuiaGeometry.RimU, Is.EqualTo(CuiaGeometry.AboveCavityU).Within(Eps));

            // Reparto [P] de D-006: pie 0,30, base 0,30, hombro 0,40, cuello 0,35 y borde 0,25 u.
            Assert.That(CuiaGeometry.FootTopU, Is.EqualTo(0.30f).Within(Eps));
            Assert.That(CuiaGeometry.CavityBottomU - CuiaGeometry.FootTopU, Is.EqualTo(0.30f).Within(Eps));
            Assert.That(CuiaGeometry.ShoulderU, Is.EqualTo(0.40f).Within(Eps));
            Assert.That(CuiaGeometry.NeckU, Is.EqualTo(0.35f).Within(Eps));
            Assert.That(CuiaGeometry.RimU, Is.EqualTo(0.25f).Within(Eps));

            // Hombro: interior 0,80 → 0,40 y exterior 0,99 → 0,50 en 0,40 u; cuello 0,50 / 0,40; borde de fibra de 0,55.
            float top = CuiaGeometry.CavityTopU(4);
            Assert.That(CuiaGeometry.InnerHalfWidth(top, 4), Is.EqualTo(0.80f).Within(Eps));
            Assert.That(CuiaGeometry.OuterHalfWidth(top, 4), Is.EqualTo(0.99f).Within(Eps));
            float neck = top + CuiaGeometry.ShoulderU + 0.01f;
            Assert.That(CuiaGeometry.InnerHalfWidth(neck, 4), Is.EqualTo(0.40f).Within(Eps));
            Assert.That(CuiaGeometry.OuterHalfWidth(neck, 4), Is.EqualTo(0.50f).Within(Eps));
            Assert.That(CuiaGeometry.OuterHalfWidth(CuiaGeometry.LipU(4) + 0.01f, 4), Is.EqualTo(CuiaGeometry.RimHalfWidthU).Within(Eps));
            Assert.That(CuiaGeometry.RimHalfWidthU, Is.EqualTo(0.55f).Within(Eps));
            Assert.That(CuiaGeometry.OuterHalfWidth(CuiaGeometry.HeightU(4) + 0.01f, 4), Is.EqualTo(0f));

            // El hombro estrecha de forma monótona y la pared nunca se cruza.
            float previous = float.MaxValue;
            for (int i = 0; i <= 40; i++)
            {
                float y = top + (i * CuiaGeometry.ShoulderU / 40f) - 1e-4f;
                float inner = CuiaGeometry.InnerHalfWidth(y, 4);
                Assert.That(inner, Is.LessThanOrEqualTo(previous + Eps), "hombro en y = " + y);
                Assert.That(CuiaGeometry.OuterHalfWidth(y, 4), Is.GreaterThan(inner), "pared del hombro en y = " + y);
                previous = inner;
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void OtherCapacities_Measure1_60PlusCapacity(int capacity)
        {
            Assert.That(CuiaGeometry.HeightU(capacity), Is.EqualTo(1.60f + capacity).Within(Eps));
        }

        [Test]
        public void TopFace_IsAnEllipse0_40uTall()
        {
            // Cara superior del líquido: elipse de ≈ 0,4 de dosis (Magic Sort: 24 px con dosis de 62,75 px).
            Assert.That(CuiaGeometry.TopFaceU, Is.EqualTo(0.40f).Within(Eps));
            Assert.That(LiquidGraphic.MaxTiltDeg, Is.GreaterThanOrEqualTo(92f), "el líquido se dibuja hasta los 92° del origen vacío");
        }
    }
}
