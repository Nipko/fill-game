#nullable enable
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La escala de texto en el chip de dificultad (02 §12; tarea DC3, revisión): <see cref="DifficultyChip.SetCompact"/> fija el
    /// tamaño de la píldora y de la cinta «×n» por la escala de texto (<see cref="UiBuilder.TextScale"/>), en las dos direcciones.
    /// Antes de DC3 la compacta iba a 12 dp también al 130 % (en 16:9, a 360 × 640 dp, sale en los tableros de 2 filas) y, al
    /// volver a la normal, la píldora se quedaba a 14 dp sin escala.
    /// </summary>
    public sealed class DifficultyChipScaleTests
    {
        private ProceduralArt _art = null!;
        private GameObject? _host;

        [SetUp]
        public void SetUp()
        {
            _art = new ProceduralArt("white");
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.FontScaleOverride = null;
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            _art.Dispose();
        }

        [TestCase(1f)]
        [TestCase(1.3f)]
        public void SetCompact_ScalesThePillAndTheRibbon_BothWays(float scale)
        {
            LaunchOptions.FontScaleOverride = scale;
            Assert.That(UiBuilder.TextScale, Is.EqualTo(scale).Within(1e-4f), "condición del test");
            DifficultyChip chip = NewChip();
            Assert.That(chip.Compact, Is.False);
            Assert.That(chip.TextSize, Is.EqualTo(DifficultyChip.TextDp * scale).Within(1e-3f), "al crearlo (UiBuilder.Label)");
            Assert.That(chip.RibbonTextSize, Is.EqualTo(DifficultyChip.RibbonTextDp * scale).Within(1e-3f), "la cinta al crearlo");

            chip.SetCompact(true);
            Assert.That(chip.Compact, Is.True);
            Assert.That(chip.TextSize, Is.EqualTo(DifficultyChip.CompactTextDp * scale).Within(1e-3f), "compacta: 12 sp × escala, nunca 12 dp al 130 %");
            Assert.That(chip.TextSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * scale) - 1e-3f), "02 §12");
            Assert.That(chip.RibbonTextSize, Is.EqualTo(DifficultyChip.RibbonTextDp * DifficultyChip.CompactRibbonScale * scale).Within(1e-3f),
                "la cinta compacta, a 3/4 × escala");

            chip.SetCompact(false);
            Assert.That(chip.Compact, Is.False);
            Assert.That(chip.TextSize, Is.EqualTo(DifficultyChip.TextDp * scale).Within(1e-3f), "de vuelta a la normal, con la escala");
            Assert.That(chip.RibbonTextSize, Is.EqualTo(DifficultyChip.RibbonTextDp * scale).Within(1e-3f), "la cinta normal, con la escala");
        }

        private DifficultyChip NewChip()
        {
            _host = new GameObject("ChipHost", typeof(RectTransform));
            return DifficultyChip.Create(_host.transform, _art);
        }
    }
}
