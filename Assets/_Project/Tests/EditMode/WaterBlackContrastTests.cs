#nullable enable
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Agua negra en ámbar-té translúcido (D-029; guion de la intro §18, Q14-A; tarea DC13): <see cref="Palette.WaterBlack"/>
    /// mantiene los 29 puntos de L* frente al tinte 14 (Agua del Río Negro; 07 §3, fila 14) opaca y compuesta en lineal sobre
    /// el fondo real de la cuia (la cavidad opaca, <see cref="PanelColors.Cavity"/>, al <see cref="IntroAnimation.WaterAlpha"/>
    /// de «Llenar y teñir»; D-003, comprobaciones compuestas de P9), se lee ámbar y no casi negra (más croma que #4A2C14 en la
    /// cavidad, tono naranja-pardo) y se sigue distinguiendo del Barro, de la blanca y de la clara (el Encuentro) y del cauce de la
    /// franja de llegada. Cálculo en CIELAB D65, como <see cref="VisualContrastTests"/>.
    /// </summary>
    public sealed class WaterBlackContrastTests
    {
        // El agua negra de antes (#4A2C14 al 70 %), solo como referencia del croma que se gana.
        private static readonly Color Previous = Palette.Hex(0x4A2C14);

        private static Color OnCavity(Color water)
        {
            return ColorMath.CompositeLinear(water, IntroAnimation.WaterAlpha, Palette.Panel.Cavity);
        }

        private static float Chroma(Color c)
        {
            Vector3 lab = ColorMath.ToLab(c);
            return Mathf.Sqrt((lab.y * lab.y) + (lab.z * lab.z));
        }

        [Test]
        public void KeepsTwentyNineLStar_FromTint14_Opaque()
        {
            float tint = ColorMath.LabLightness(Palette.Tint(14));
            Assert.That(tint - ColorMath.LabLightness(Palette.WaterBlack), Is.GreaterThanOrEqualTo(29f));
        }

        [Test]
        public void KeepsTwentyNineLStar_FromTint14_OnTheCavity()
        {
            // El agua dentro de la cuia, al 70 % sobre la cavidad opaca, frente a una dosis del tinte 14 (opaca).
            float tint = ColorMath.LabLightness(Palette.Tint(14));
            float seen = ColorMath.LabLightness(OnCavity(Palette.WaterBlack));
            Assert.That(tint - seen, Is.GreaterThanOrEqualTo(29f));
            Assert.That(IntroAnimation.WaterAlpha, Is.EqualTo(0.70f), "sin cambiar la opacidad (07 §5.1)");
        }

        [Test]
        public void ReadsAsAmberTea_NotAlmostBlack()
        {
            // Más croma que #4A2C14 en la cavidad (≥ 1,4 veces y C* ≥ 22) y tono ámbar: h entre 45° y 70° (naranja-pardo).
            Color now = OnCavity(Palette.WaterBlack);
            Assert.That(Chroma(now), Is.GreaterThanOrEqualTo(22f));
            Assert.That(Chroma(now), Is.GreaterThanOrEqualTo(1.4f * Chroma(OnCavity(Previous))));
            Vector3 lab = ColorMath.ToLab(Palette.WaterBlack);
            float hue = Mathf.Atan2(lab.z, lab.y) * Mathf.Rad2Deg;
            Assert.That(hue, Is.InRange(45f, 70f));
            Assert.That(ColorMath.DeltaE00(now, Palette.Panel.Cavity), Is.GreaterThanOrEqualTo(15f), "se ve en la cavidad vacía");
        }

        [Test]
        public void StaysApartFromMud_AndFromTheOtherWaters()
        {
            Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.WaterBlack), Is.GreaterThanOrEqualTo(10f), "Barro");
            Assert.That(ColorMath.DeltaE00(Palette.WaterBlack, Palette.WaterWhite), Is.GreaterThanOrEqualTo(20f), "blanca (Encuentro)");
            Assert.That(ColorMath.DeltaE00(Palette.WaterBlack, Palette.WaterClear), Is.GreaterThanOrEqualTo(20f), "clara");
            Assert.That(ColorMath.DeltaE00(Palette.WaterBlack, ArrivalStrip.StripWhiteWater), Is.GreaterThanOrEqualTo(20f),
                "blanca de la franja del Encuentro");
            Assert.That(ColorMath.DeltaE00(OnCavity(Palette.Mud), OnCavity(Palette.WaterBlack)), Is.GreaterThanOrEqualTo(10f),
                "Barro y agua negra, también en la cavidad");
        }

        [Test]
        public void TheBlackOfTheArrivalStrip_IsThePaletteOne()
        {
            Assert.That(ArrivalStrip.StripWater("black"), Is.EqualTo(Palette.WaterBlack));
            Color first = ArrivalStrip.SampleWater("black_white", out Color? second);
            Assert.That(first, Is.EqualTo(Palette.WaterBlack));
            Assert.That(second.HasValue, Is.True);
            Assert.That(ColorMath.DeltaE00(first, second!.Value), Is.GreaterThanOrEqualTo(20f), "las dos aguas del Encuentro");
        }
    }
}
