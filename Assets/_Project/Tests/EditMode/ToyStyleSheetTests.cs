#nullable enable
using System.IO;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La lámina de referencia de la guía <c>docs/art/estilo-personajes-3d.md</c> (D-058), generada por el propio código de
    /// <see cref="ToyShading"/>: <c>Logs/snapshots/personajes-3d/p3d_20_referencia_estilo.png</c> (1080 × 1040 px, 3 px/dp).
    /// Tres filas de esferas, una por material (el pelaje gris pardo de TiTino sobre la selva clara, el vinilo rosa de Rosa
    /// sobre el panel azul petróleo y el esmalte azul de la morpho sobre la selva clara), que van sumando capas de izquierda a
    /// derecha: 1) los tres tonos; 2) con el rebote frío y la luz de borde cálida; 3) con la oclusión y la sombra de contacto;
    /// 4) con el barniz (doble brillo); 5) con la felpa. Abajo, el ojo de juguete con el iris de TiTino, de Capi y de Rosa, y
    /// los cuatro tonos (sombra, medio, luz y contorno) de cada personaje. Explícito: escribe un PNG para la guía.
    /// </summary>
    [Explicit("Lámina de referencia de la guía de estilo (D-058); escribe un PNG.")]
    public sealed class ToyStyleSheetTests
    {
        private const int W = 1080;
        private const int H = 1040;

        [Test]
        public void WriteStyleReferenceSheet()
        {
            var pc = new PixelCanvas(W, H);
            Color[] fills = { ProceduralArt.SlothFur, ProceduralArt.RosaPink, ProceduralArt.MorphoFlightBlue };
            Color[] backs = { Palette.JungleLight, Palette.Panel.Top, Palette.JungleLight };
            for (int row = 0; row < 3; row++)
            {
                int top = row * 260;
                Rect(pc, 0, top, W, 260, backs[row]);
                ToyTones t = ToyShading.Tones(fills[row]);
                for (int step = 0; step < 5; step++)
                {
                    Sphere(pc, 108 + (step * 216), top + 120, 82f, t, step);
                }
            }

            Rect(pc, 0, 780, W, 260, Palette.Panel.Top);
            Color[] irises = { ProceduralArt.SlothEyeWarm, ProceduralArt.CapiToy.Iris, ProceduralArt.RosaToy.Iris }; // los de la fuente
            Color[] chars = { ProceduralArt.SlothFur, ProceduralArt.CapiFur, ProceduralArt.RosaPink, ProceduralArt.MorphoFlightBlue };
            for (int i = 0; i < 3; i++)
            {
                float cx = 90f + (i * 150f);
                float cy = H - 880f;
                pc.FillCircle(cx, cy, 64f, Palette.Hex(0xF2E9DA));
                ProceduralArt.DrawToyEye(pc, cx, cy, 52f, irises[i]);
            }

            for (int i = 0; i < chars.Length; i++)
            {
                ToyTones t = ToyShading.Tones(chars[i]);
                Color[] sw = { t.Shadow, t.Mid, t.Light, ColorMath.WithAlpha(t.Outline, 1f) };
                for (int j = 0; j < 4; j++)
                {
                    Rect(pc, 560 + (j * 120), 800 + (i * 58), 112, 50, sw[j]);
                }
            }

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(pc.ToColor32());
            tex.Apply();
            string dir = Path.Combine(Application.dataPath, "../Logs/snapshots/personajes-3d");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "p3d_20_referencia_estilo.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // Rectángulo opaco con la esquina de arriba a la izquierda en (x, yTop) px (y hacia abajo).
        private static void Rect(PixelCanvas pc, int x, int yTop, int w, int h, Color c)
        {
            c.a = 1f;
            for (int y = H - yTop - h; y < H - yTop; y++)
            {
                for (int xx = x; xx < x + w; xx++)
                {
                    pc.Set(xx, y, c);
                }
            }
        }

        // Una esfera de radio r px con la capa step (0..4), centrada en (cx, yTop) px (y hacia abajo), sobre su sombra de contacto.
        private static void Sphere(PixelCanvas pc, int cx, int yTop, float r, in ToyTones t, int step)
        {
            float cy = H - yTop;
            if (step >= 2)
            {
                for (int y = (int)(cy - r - 30f); y < (int)(cy - r + 30f); y++)
                {
                    for (int x = (int)(cx - r - 10f); x < (int)(cx + r + 10f); x++)
                    {
                        float s = ToyShading.ContactShadow(x + 0.5f - cx, y + 0.5f - (cy - r + 4f), r * 0.85f, r * 0.2f);
                        pc.Blend(x, y, new Color(0.08f, 0.06f, 0.04f, 1f), 0.45f * s);
                    }
                }
            }

            for (int y = (int)(cy - r - 3f); y <= (int)(cy + r + 3f); y++)
            {
                for (int x = (int)(cx - r - 3f); x <= (int)(cx + r + 3f); x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
                    pc.Blend(x, y, t.Outline, PixelCanvas.Coverage(d - 2.5f));
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Vector3 shape = ToyShading.Normal(dx / r, dy / r);
                    Vector3 n = shape;
                    float ao = step >= 2 ? 0.7f * ToyShading.ContactShadow(dx, dy + r, r * 0.9f, r * 0.45f) : 0f;
                    if (step >= 4)
                    {
                        float h = ToyShading.PlushClumps(dx, dy, r * 0.32f, 17, out float sx, out float sy);
                        n = ToyShading.Tilt(shape, sx, sy, 0.16f);
                        ao = Mathf.Clamp01(ao + (0.14f * (1f - Mathf.Clamp01(h * 1.6f))));
                    }

                    Color c = step == 0
                        ? ToyShading.ThreeTone(t, ToyShading.Diffuse(n))
                        : ToyShading.Shade(t, n, ao, step >= 3 ? 1f : 0f, shape);
                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }
        }
    }
}
