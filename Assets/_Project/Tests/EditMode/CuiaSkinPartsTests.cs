#nullable enable
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Las piezas de las skins (07 §4.6) y su miniatura del Mercado (observación 4 de la jornada 6):
    /// <list type="bullet">
    /// <item><see cref="CuiaSkinParts"/> fija las cotas del tablero (pie, borde, banda entera o bicolor y asa), las que usan
    /// <c>CuiaView</c> y la miniatura;</item>
    /// <item>la miniatura (<see cref="ProceduralArt.PaintSkinThumb"/>) es una ficha del panel con la cuia, y en sus píxeles cada skin
    /// lleva sus colores en su sitio: el pie y el borde, la banda (las dos mitades del Encuentro) y el asa de la Totuma.</item>
    /// </list>
    /// </summary>
    public sealed class CuiaSkinPartsTests
    {
        private const float Eps = 1e-4f;

        private ProceduralArt _art = null!;

        [OneTimeSetUp]
        public void CreateArt()
        {
            _art = new ProceduralArt("white");
        }

        [OneTimeTearDown]
        public void DisposeArt()
        {
            _art.Dispose();
        }

        private static void AssertRect(Rect r, float x, float y, float w, float h, string what)
        {
            Assert.That(r.x, Is.EqualTo(x).Within(Eps), what + ".x (centro desde el eje)");
            Assert.That(r.y, Is.EqualTo(y).Within(Eps), what + ".y (abajo)");
            Assert.That(r.width, Is.EqualTo(w).Within(Eps), what + ".ancho");
            Assert.That(r.height, Is.EqualTo(h).Within(Eps), what + ".alto");
        }

        [Test]
        public void Parts_KeepTheBoardMeasures_ForTheStandardCuia()
        {
            AssertRect(CuiaSkinParts.Foot(), 0f, 0f, 1.60f, 0.30f, "pie");
            AssertRect(CuiaSkinParts.Rim(4), 0f, 5.35f, 1.10f, 0.25f, "borde en el labio");
            AssertRect(CuiaSkinParts.Band(4, 0), 0f, 4.64f, 1.90f, 0.12f, "banda sobre la cavidad");
            AssertRect(CuiaSkinParts.Band(4, -1), -0.475f, 4.64f, 0.95f, 0.12f, "mitad izquierda");
            AssertRect(CuiaSkinParts.Band(4, 1), 0.475f, 4.64f, 0.95f, 0.12f, "mitad derecha");
            AssertRect(CuiaSkinParts.Handle(4), 0.97f, 4.62f, 0.12f, 0.40f, "asa");
            Assert.That(CuiaSkinParts.CornerU, Is.EqualTo(0.06f));
        }

        [Test]
        public void Parts_FollowTheCapacity()
        {
            AssertRect(CuiaSkinParts.Rim(2), 0f, 3.35f, 1.10f, 0.25f, "borde de la de 2");
            AssertRect(CuiaSkinParts.Band(2, 0), 0f, 2.64f, 1.90f, 0.12f, "banda de la de 2");
            AssertRect(CuiaSkinParts.Handle(5), 0.97f, 5.62f, 0.12f, 0.40f, "asa de la de 5");
        }

        // El píxel del punto (xu desde el eje, yu desde el pie) de la cuia en la ficha.
        private static Color At(PixelCanvas pc, float xu, float yu)
        {
            const int ppu = ProceduralArt.SkinThumbPixelsPerU;
            int padX = Mathf.RoundToInt(ProceduralArt.ThumbPadXU * ppu);
            int padY = Mathf.RoundToInt(ProceduralArt.ThumbPadYU * ppu);
            return pc.Get(padX + Mathf.FloorToInt((1f + xu) * ppu), padY + Mathf.FloorToInt(yu * ppu));
        }

        private static void AssertColor(Color actual, Color expected, string what)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.02f), what + " (r)");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.02f), what + " (g)");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.02f), what + " (b)");
            Assert.That(actual.a, Is.EqualTo(1f).Within(0.02f), what + " (opaco)");
        }

        private static bool Near(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.05f && Mathf.Abs(a.g - b.g) < 0.05f && Mathf.Abs(a.b - b.b) < 0.05f;
        }

        [Test]
        public void Thumb_IsAPanelTile_WithEachSkinsColorsInPlace()
        {
            CuiaSkin[] skins = { CuiaSkin.Santarem, CuiaSkin.Encuentro, CuiaSkin.Totuma };
            for (int i = 0; i < skins.Length; i++)
            {
                CuiaSkin skin = skins[i];
                PixelCanvas pc = _art.PaintSkinThumb(skin);
                try
                {
                    AssertColor(At(pc, 0f, 0.15f), skin.Foot, skin.Id + ": el pie");
                    AssertColor(At(pc, 0f, 5.47f), skin.Rim, skin.Id + ": el borde");
                    if (skin.BandBicolor)
                    {
                        AssertColor(At(pc, -0.45f, 4.70f), skin.Band, skin.Id + ": la mitad izquierda de la banda");
                        AssertColor(At(pc, 0.45f, 4.70f), skin.BandB, skin.Id + ": la mitad derecha de la banda");
                    }
                    else
                    {
                        AssertColor(At(pc, 0f, 4.70f), skin.Band, skin.Id + ": la banda");
                    }

                    if (skin.HasHandle)
                    {
                        AssertColor(At(pc, 0.99f, 4.85f), skin.Handle, skin.Id + ": el asa");
                    }
                    else
                    {
                        Assert.That(Near(At(pc, 0.99f, 4.85f), skin.Band), Is.False, skin.Id + ": sin asa");
                    }

                    Color tile = pc.Get(2, pc.Height / 2);
                    Assert.That(tile.a, Is.EqualTo(1f).Within(0.01f), skin.Id + ": la ficha es opaca");
                    Assert.That(tile.b, Is.GreaterThan(tile.r), skin.Id + ": la ficha es del panel petróleo");
                    Assert.That(pc.Get(0, 0).a, Is.LessThan(0.5f), skin.Id + ": con las esquinas redondeadas");
                }
                finally
                {
                    pc.Release();
                }
            }

            PixelCanvas basePc = _art.PaintSkinThumb(CuiaSkin.Base);
            try
            {
                Color foot = At(basePc, 0f, 0.15f);
                for (int i = 0; i < skins.Length; i++)
                {
                    Assert.That(Near(foot, skins[i].Foot), Is.False, "la base, con el pie de fibra y no el de " + skins[i].Id);
                    Assert.That(Near(At(basePc, 0f, 4.70f), skins[i].Band), Is.False, "la base, sin banda de " + skins[i].Id);
                }
            }
            finally
            {
                basePc.Release();
            }
        }
    }
}
