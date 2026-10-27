#nullable enable
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El delfín de río de la fase 2 de D-058 (Rosa y el bufeo, con la forma nueva que pidió el dueño): en el banner en reposo solo
    /// la espuma tapa el corte del cuerpo con el agua (la onda y el soplo están a alfa 0), así que el corte cabe bajo
    /// <see cref="ProceduralArt.RosaFoamWidthDp"/> con margen para las puntas afinadas de la espuma, en las constantes y en los
    /// píxeles reales de la fila del agua; y la silueta deja un margen transparente en los bordes del sprite (el lóbulo de abajo de
    /// la cola no se recorta).
    /// </summary>
    public sealed class RosaWaterCutTests
    {
        // Lo que se exige al corte frente a la espuma: el 85 % de su ancho (la espuma es una lente que se afina hacia las puntas).
        private const float FoamShare = 0.85f;

        [Test]
        public void TheCut_FitsUnderTheFoam_WithMargin()
        {
            float cutDp = ProceduralArt.RosaCutWidth * ProceduralArt.RosaWidthDp;
            Assert.That(cutDp, Is.LessThanOrEqualTo(FoamShare * ProceduralArt.RosaFoamWidthDp),
                "el corte (" + cutDp + " dp) cabe bajo la espuma (" + ProceduralArt.RosaFoamWidthDp + " dp) con margen");
            Assert.That(ProceduralArt.RosaCutCenter - (ProceduralArt.RosaCutWidth * 0.5f), Is.GreaterThan(0f));
            Assert.That(ProceduralArt.RosaCutCenter + (ProceduralArt.RosaCutWidth * 0.5f), Is.LessThan(1f));
        }

        [Test]
        public void TheRealPixels_OnTheWaterRow_AreInsideTheDeclaredCut()
        {
            // La fila del agua del banner: asoma RosaAbove del alto, así que la línea está a (1 − RosaAbove) del alto desde abajo.
            Color32[] px = ProceduralArt.RosaPixels(out int w, out int h);
            int row = Mathf.RoundToInt((1f - WorldBannerView.RosaAbove) * h);
            float pxPerDp = w / ProceduralArt.RosaWidthDp;
            int first = -1;
            int last = -1;
            for (int dy = -1; dy <= 1; dy++)
            {
                int y = row + dy;
                for (int x = 0; x < w; x++)
                {
                    if (px[(y * w) + x].a > 8)
                    {
                        first = first < 0 ? x : Mathf.Min(first, x);
                        last = Mathf.Max(last, x);
                    }
                }
            }

            Assert.That(first, Is.GreaterThanOrEqualTo(0), "el cuerpo cruza la línea del agua");
            float fromDp = first / pxPerDp;
            float toDp = (last + 1) / pxPerDp;
            float cutFrom = (ProceduralArt.RosaCutCenter - (ProceduralArt.RosaCutWidth * 0.5f)) * ProceduralArt.RosaWidthDp;
            float cutTo = (ProceduralArt.RosaCutCenter + (ProceduralArt.RosaCutWidth * 0.5f)) * ProceduralArt.RosaWidthDp;
            string what = "corte real de " + fromDp.ToString("0.0") + " a " + toDp.ToString("0.0") + " dp; declarado de " + cutFrom.ToString("0.0") + " a " + cutTo.ToString("0.0");
            TestContext.WriteLine(what);
            Assert.That(fromDp, Is.GreaterThanOrEqualTo(cutFrom - 1f), what);
            Assert.That(toDp, Is.LessThanOrEqualTo(cutTo + 1f), what);
            Assert.That(toDp - fromDp, Is.LessThanOrEqualTo(FoamShare * ProceduralArt.RosaFoamWidthDp), what + ": cabe bajo la espuma");
        }

        [Test]
        public void RosaAndTheBufeo_LeaveATransparentMargin()
        {
            // Rosa (312 × 198 px), 2 px; el bufeo del Mapa (96 px de ancho, con el borde de 1,2 px como mínimo), 1 px: el canto del
            // sprite queda transparente, así que nada se recorta.
            AssertMargin(ProceduralArt.RosaPixels(out int w, out int h), w, h, 2, "Rosa");
            AssertMargin(ProceduralArt.MapBufeoPixels(out int bw, out int bh), bw, bh, 1, "el bufeo");
        }

        // Las margin filas de abajo y de arriba y columnas de los lados, transparentes: la silueta no toca el borde del sprite.
        private static void AssertMargin(Color32[] px, int w, int h, int margin, string who)
        {
            int touching = 0;
            string where = string.Empty;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool edge = y < margin || y >= h - margin || x < margin || x >= w - margin;
                    if (edge && px[(y * w) + x].a > 8)
                    {
                        touching++;
                        where += touching <= 4 ? " (" + x + ", " + y + ")" : string.Empty;
                    }
                }
            }

            Assert.That(touching, Is.EqualTo(0), who + " (" + w + " × " + h + " px): la silueta no toca el borde del sprite, en" + where);
        }
    }
}
