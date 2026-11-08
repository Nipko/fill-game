#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using AF.LiveOps;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Los marcos de país por código (Q-M6 de D-033, añadido del 2026-11-08; D-052): los 13 sin NaN, con el centro transparente y la
    /// banda opaca en el sitio del aro, distintos entre sí y del aro de madera también en el retrato pequeño (24 y 32 dp), con la placa
    /// de la bandera solo donde hay un país, y el retrato con marco a tiempo. Exporta las láminas a <c>Logs/art/frames/</c>.
    /// </summary>
    public sealed class AvatarFrameArtTests
    {
        private const int Px = 192; // una celda de 64 dp a 3 px/dp

        [Test]
        public void EveryFrame_HasNoNaN_ATransparentCentre_AndAnOpaqueBand()
        {
            for (int w = 1; w <= AvatarFrames.Count; w++)
            {
                string? country = AvatarFrames.OfWorld(w)!.Country;
                var pc = new PixelCanvas(96, 96);
                ProceduralArt.PaintAvatarFrame(pc, w, country);
                for (int y = 0; y < 96; y++)
                {
                    for (int x = 0; x < 96; x++)
                    {
                        Color c = pc.Get(x, y);
                        Assert.That(float.IsNaN(c.r) || float.IsNaN(c.g) || float.IsNaN(c.b) || float.IsNaN(c.a), Is.False, "NaN en el marco " + w);
                    }
                }

                pc.Release();
                Color32[] px = ProceduralArt.AvatarFramePixels(w, country, Px);
                string what = "marco " + w;
                Assert.That(px[(Px / 2 * Px) + (Px / 2)].a, Is.EqualTo(0), what + ": el centro, transparente (el retrato se ve)");
                Assert.That(px[(Px / 2 * Px) + 3].a, Is.EqualTo(255), what + ": la banda, opaca a la izquierda");
                Assert.That(px[((Px - 4) * Px) + (Px / 2)].a, Is.EqualTo(255), what + ": y arriba");
                Assert.That(px[0].a, Is.EqualTo(0), what + ": la esquina, transparente");
            }
        }

        [Test]
        public void Frames_AreDistinct_FromEachOther_AndFromTheWoodRing_AtSmallSizes()
        {
            foreach (int size in new[] { 72, 96 }) // 24 y 32 dp a 3 px/dp
            {
                var bands = new Color[AvatarFrames.Count + 1];
                Color32[] wood = ProceduralArt.AvatarPixels(1, AvatarArtBackdrop.Petroleum, AvatarArtBadge.None, false, size);
                bands[0] = BandMean(wood, size);
                for (int w = 1; w <= AvatarFrames.Count; w++)
                {
                    Color32[] px = ProceduralArt.AvatarFramedPixels(1, AvatarArtBackdrop.Petroleum, w, AvatarFrames.OfWorld(w)!.Country, size);
                    bands[w] = BandMean(px, size);
                }

                for (int a = 0; a < bands.Length; a++)
                {
                    for (int b = a + 1; b < bands.Length; b++)
                    {
                        float d = Distance(bands[a], bands[b]);
                        Assert.That(d, Is.GreaterThan(0.06f), "a " + size + " px, el color medio de la banda de " + Name(a) + " y " + Name(b) + " se parecen demasiado (" + d + ")");
                    }
                }
            }
        }

        [Test]
        public void TheFlagPlaque_OnlyWhereThereIsACountry()
        {
            int bottom = (6 * Px) + (Px / 2); // abajo en el centro, en la placa (y = 6 px desde abajo, sobre la banda)
            Color32 brazil = ProceduralArt.AvatarFramePixels(1, "BR", Px)[(int)(Px * 0.07f) * Px + (Px / 2)];
            Color32 without = ProceduralArt.AvatarFramePixels(1, null, Px)[(int)(Px * 0.07f) * Px + (Px / 2)];
            Assert.That(brazil.Equals(without), Is.False, "la placa de Brasil cambia el pie del marco");
            Color32 multi = ProceduralArt.AvatarFramePixels(10, AvatarFrames.OfWorld(10)!.Country, Px)[bottom];
            Assert.That(multi.a, Is.EqualTo(255), "el mundo 10 (varios países) lleva la banda entera, sin placa");
        }

        [Test]
        public void ThePortraitWithAFrame_TakesLessThan16Ms()
        {
            ProceduralArt.AvatarFramedPixels(8, AvatarArtBackdrop.Petroleum, 3, "BR", Px); // calienta las tablas del juguete
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 4; i++)
            {
                ProceduralArt.AvatarFramedPixels(8, AvatarArtBackdrop.Petroleum, 1 + i, AvatarFrames.OfWorld(1 + i)!.Country, Px);
            }

            double ms = watch.Elapsed.TotalMilliseconds / 4.0;
            Assert.That(ms, Is.LessThan(16.0), "un retrato de 192 px con marco: < 16 ms en el Editor (≤ 8 del retrato + ≤ 8 del marco; se pinta solo al cambiar de avatar o de marco) [P]");
        }

        [Test]
        public void ExportsTheSheet()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "art", "frames");
            Directory.CreateDirectory(dir);
            for (int w = 1; w <= 9; w++)
            {
                foreach (int size in new[] { 336, 96, 72 })
                {
                    Color32[] px = ProceduralArt.AvatarFramedPixels(1, AvatarArtBackdrop.Petroleum, w, AvatarFrames.OfWorld(w)!.Country, size);
                    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    tex.SetPixels32(px);
                    tex.Apply();
                    File.WriteAllBytes(Path.Combine(dir, "frame_w" + w.ToString("00") + "_" + size + ".png"), tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);
                }
            }

            Assert.That(File.Exists(Path.Combine(dir, "frame_w03_96.png")), Is.True);
        }

        private static string Name(int i) => i == 0 ? "el aro de madera" : "el mundo " + i;

        // El color medio de la banda (del 82 % al 98 % del radio, fuera de la placa de abajo).
        private static Color BandMean(Color32[] px, int n)
        {
            float half = n * 0.5f;
            double r = 0, g = 0, b = 0;
            int count = 0;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) - half;
                    float dy = (y + 0.5f) - half;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy)) / half;
                    if (d < 0.88f || d > 0.97f || dy < -half * 0.5f)
                    {
                        continue;
                    }

                    Color32 c = px[(y * n) + x];
                    r += c.r;
                    g += c.g;
                    b += c.b;
                    count++;
                }
            }

            return new Color((float)(r / count / 255.0), (float)(g / count / 255.0), (float)(b / count / 255.0), 1f);
        }

        private static float Distance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt((dr * dr) + (dg * dg) + (db * db));
        }
    }
}
