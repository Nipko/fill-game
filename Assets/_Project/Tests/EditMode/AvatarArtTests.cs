#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Los retratos de avatar por código (D-056 y D-052; avatares.md §2): los 16, el bloqueado y el disco neutro sin NaN, el disco
    /// opaco y las esquinas transparentes, retratos distintos entre sí, el bloqueado apagado, el tiempo por retrato y el atlas perezoso
    /// por fotogramas sin asignar memoria.
    /// </summary>
    public sealed class AvatarArtTests
    {
        private const int Px = 192; // una celda de 64 dp a 3 px/dp

        private static readonly AvatarArtBackdrop[] Backdrops =
        {
            AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Water, AvatarArtBackdrop.Wood,
            AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Wood, AvatarArtBackdrop.Water,
            AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Water, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Wood,
            AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Water,
        };

        [Test]
        public void EveryPortrait_HasNoNaN_AnOpaqueDisc_AndTransparentCorners()
        {
            for (int i = -1; i < ProceduralArt.AvatarArtCount; i++)
            {
                AvatarArtBackdrop bg = i < 0 ? AvatarArtBackdrop.Leaf : Backdrops[i];
                foreach (bool locked in new[] { false, true })
                {
                    string what = "avatar " + i + (locked ? " bloqueado" : string.Empty);
                    Color32[] px = ProceduralArt.AvatarPixels(i, bg, locked ? AvatarArtBadge.VeryHard : AvatarArtBadge.None, locked, Px);
                    Assert.That(px.Length, Is.EqualTo(Px * Px), what);
                    Assert.That(px[(Px / 2 * Px) + (Px / 2)].a, Is.EqualTo(255), what + ": el centro del disco es opaco");
                    Assert.That(px[(Px / 2 * Px) + 2].a, Is.EqualTo(255), what + ": el aro llega al borde");
                    Assert.That(px[0].a, Is.EqualTo(0), what + ": la esquina, transparente (retrato redondo)");
                    Assert.That(px[(Px * Px) - 1].a, Is.EqualTo(0), what);
                }
            }

            // NaN: con Color32 no se ve; se comprueba en el lienzo de floats.
            for (int i = -1; i < ProceduralArt.AvatarArtCount; i++)
            {
                var pc = new PixelCanvas(96, 96);
                var scratch = new PixelCanvas(96, 96);
                ProceduralArt.PaintAvatar(pc, scratch, i, AvatarArtBackdrop.Water, AvatarArtBadge.Daily, i % 2 == 0);
                for (int y = 0; y < 96; y++)
                {
                    for (int x = 0; x < 96; x++)
                    {
                        Color c = pc.Get(x, y);
                        Assert.That(float.IsNaN(c.r) || float.IsNaN(c.g) || float.IsNaN(c.b) || float.IsNaN(c.a), Is.False, "NaN en el avatar " + i);
                    }
                }

                pc.Release();
                scratch.Release();
            }
        }

        [Test]
        public void Portraits_AreDistinct_AndTheLockedOneIsMuted()
        {
            var all = new Color32[ProceduralArt.AvatarArtCount][];
            for (int i = 0; i < all.Length; i++)
            {
                all[i] = ProceduralArt.AvatarPixels(i, AvatarArtBackdrop.Leaf, AvatarArtBadge.None, false, 64);
            }

            for (int i = 0; i < all.Length; i++)
            {
                for (int j = i + 1; j < all.Length; j++)
                {
                    Assert.That(Difference(all[i], all[j]), Is.GreaterThan(0.02f), "el avatar " + i + " y el " + j + " se distinguen");
                }
            }

            for (int i = 0; i < all.Length; i++)
            {
                Color32[] normal = ProceduralArt.AvatarPixels(i, Backdrops[i], AvatarArtBadge.None, false, 96);
                Color32[] locked = ProceduralArt.AvatarPixels(i, Backdrops[i], AvatarArtBadge.World, true, 96);
                Assert.That(Difference(locked, normal), Is.GreaterThan(0.04f), "bloqueado: silueta y fondo apagado (" + i + ")");
                Color32 ringLocked = locked[((96 - 3) * 96) + 48];
                Color32 ringNormal = normal[((96 - 3) * 96) + 48];
                Color.RGBToHSV(ringLocked, out _, out float sLocked, out _);
                Color.RGBToHSV(ringNormal, out _, out float sNormal, out _);
                Assert.That(sLocked, Is.LessThan(sNormal), "el aro pasa a gris (" + i + ")");
            }
        }

        [Test]
        public void APortrait_TakesLessThan8ms_AtTheCellSize()
        {
            var pc = new PixelCanvas(Px, Px);
            var scratch = new PixelCanvas(Px, Px);
            ProceduralArt.PaintAvatar(pc, scratch, 0, AvatarArtBackdrop.Leaf, AvatarArtBadge.None, false); // calentamiento
            double perPortrait = double.MaxValue;
            for (int round = 0; round < 3; round++)
            {
                // La mejor de tres pasadas: el Editor comparte la CPU con otros procesos (lo que se mide es el dibujo).
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < ProceduralArt.AvatarArtCount; i++)
                {
                    pc.Fill(Color.clear);
                    ProceduralArt.PaintAvatar(pc, scratch, i, Backdrops[i], AvatarArtBadge.None, false);
                }

                watch.Stop();
                perPortrait = Math.Min(perPortrait, watch.Elapsed.TotalMilliseconds / ProceduralArt.AvatarArtCount);
            }

            TestContext.WriteLine("AF_PERF avatar " + perPortrait.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " ms por retrato de " + Px + " px");
            Assert.That(perPortrait, Is.LessThanOrEqualTo(PerfBudget.Ms(8.0)), "≤ 8 ms por retrato de 192 px en el Editor x64 (avatares.md §2.2), " + PerfBudget.Describe(8.0));
            pc.Release();
            scratch.Release();
        }

        [Test]
        public void TheAtlas_PaintsTwoPerFrame_WithoutAllocating()
        {
            using AvatarAtlas atlas = ProceduralArt.CreateAvatarAtlas(64);
            Assert.That(atlas.Texture.width, Is.EqualTo(5 * 64));
            Assert.That(atlas.Texture.height, Is.EqualTo(4 * 64));
            for (int i = 0; i < ProceduralArt.AvatarArtCount; i++)
            {
                atlas.Request(i, null, i, Backdrops[i], i < 6 ? AvatarArtBadge.None : AvatarArtBadge.World, i >= 6);
            }

            atlas.Request(AvatarAtlas.NeutralCell, null, ProceduralArt.AvatarNeutral, AvatarArtBackdrop.Leaf, AvatarArtBadge.None, false);
            Assert.That(atlas.Ready(0), Is.False, "nada se pinta al pedirlo");
            Assert.That(atlas.Tick(), Is.EqualTo(AvatarAtlas.PerTick));
            Assert.That(atlas.Ready(0) && atlas.Ready(1), Is.True);
            Assert.That(atlas.Ready(2), Is.False);
            atlas.Tick(); // los lienzos ya existen

            long before = GC.GetAllocatedBytesForCurrentThread();
            atlas.Tick();
            atlas.Tick();
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0L), "pintar celdas no asigna memoria");
            atlas.PaintAll();
            Assert.That(atlas.Pending, Is.False);
            before = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(atlas.Tick(), Is.EqualTo(0));
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0L), "sin nada pendiente, nada");

            atlas.Request(7, null, 7, AvatarArtBackdrop.Water, AvatarArtBadge.None, false); // se desbloqueó: se repinta la celda
            Assert.That(atlas.Ready(7), Is.False);
            Assert.That(atlas.Tick(), Is.EqualTo(1));
            Sprite cell = atlas.Cell(7);
            Assert.That(cell.rect.width, Is.EqualTo(64f));
            Assert.That(atlas.Cell(7), Is.SameAs(cell));
            Assert.That(ArtSlots.IsKnown("avatar_toucan"), Is.True);
            Assert.That(ArtSlots.IsKnown("avatar_"), Is.False);
            Assert.That(ProceduralArt.AvatarSlot("dart_frog"), Is.EqualTo("avatar_dart_frog"));
        }

        private static float Difference(Color32[] a, Color32[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b);
            }

            return (float)(sum / (a.Length * 3.0 * 255.0));
        }
    }

    /// <summary>
    /// Exporta los retratos de avatar a <c>Logs/art/avatars/</c> para que el dueño los vea (D-050: «el dueño ve capturas antes de
    /// integrar»): los 16 a 336 px, el jaguar y el ocelote bloqueados, el disco neutro y una hoja de contacto. No forma parte de la suite.
    /// <c>unity test &lt;repo&gt; --mode EditMode --filter AvatarArtExportTests</c>.
    /// </summary>
    [Explicit("Exportación manual de los avatares para revisión.")]
    public sealed class AvatarArtExportTests
    {
        private static readonly string[] Ids =
        {
            "macaw", "toucan", "tapir", "turtle", "coati", "hummingbird", "dart_frog", "otter", "jaguar", "kingfisher", "caiman",
            "anteater", "heron", "howler", "ocelot", "armadillo",
        };

        private static readonly AvatarArtBackdrop[] Backdrops =
        {
            AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Water, AvatarArtBackdrop.Wood,
            AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Wood, AvatarArtBackdrop.Water,
            AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Water, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Wood,
            AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Water,
        };

        [Test]
        public void ExportAvatars()
        {
            const int size = 336;
            string dir = Path.Combine(Application.dataPath, "../Logs/art/avatars");
            Directory.CreateDirectory(dir);
            for (int i = 0; i < Ids.Length; i++)
            {
                Save(dir, (i < 10 ? "0" : string.Empty) + i + "_" + Ids[i], ProceduralArt.AvatarPixels(i, Backdrops[i], AvatarArtBadge.None, false, size), size);
            }

            Save(dir, "locked_jaguar", ProceduralArt.AvatarPixels(8, Backdrops[8], AvatarArtBadge.VeryHard, true, size), size);
            Save(dir, "locked_ocelot", ProceduralArt.AvatarPixels(14, Backdrops[14], AvatarArtBadge.League, true, size), size);
            Save(dir, "locked_anteater", ProceduralArt.AvatarPixels(11, Backdrops[11], AvatarArtBadge.World, true, size), size);
            Save(dir, "neutral", ProceduralArt.AvatarPixels(ProceduralArt.AvatarNeutral, AvatarArtBackdrop.Leaf, AvatarArtBadge.None, false, size), size);

            // Hoja de contacto: los 16 a 192 px en 4 × 4 sobre crema, y abajo los seis bloqueados de cada icono y el neutro a 96 px.
            const int cell = 192;
            const int gap = 12;
            int w = (4 * cell) + (5 * gap);
            int h = (4 * cell) + (5 * gap) + 96 + gap;
            var sheet = new Color32[w * h];
            var cream = new Color32(0xFF, 0xF8, 0xE6, 0xFF);
            for (int i = 0; i < sheet.Length; i++)
            {
                sheet[i] = cream;
            }

            for (int i = 0; i < 16; i++)
            {
                int cx = gap + ((i % 4) * (cell + gap));
                int cy = h - ((i / 4) + 1) * (cell + gap);
                Blit(sheet, w, ProceduralArt.AvatarPixels(i, Backdrops[i], AvatarArtBadge.None, false, cell), cell, cx, cy);
            }

            var badges = new[] { AvatarArtBadge.World, AvatarArtBadge.Hard, AvatarArtBadge.VeryHard, AvatarArtBadge.Streak, AvatarArtBadge.Daily, AvatarArtBadge.League };
            var lockedIdx = new[] { 10, 6, 8, 7, 9, 14 };
            for (int b = 0; b < badges.Length; b++)
            {
                Blit(sheet, w, ProceduralArt.AvatarPixels(lockedIdx[b], Backdrops[lockedIdx[b]], badges[b], true, 96), 96, gap + (b * (96 + gap)), gap);
            }

            Blit(sheet, w, ProceduralArt.AvatarPixels(ProceduralArt.AvatarNeutral, AvatarArtBackdrop.Leaf, AvatarArtBadge.None, false, 96), 96, gap + (6 * (96 + gap)), gap);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(sheet);
            File.WriteAllBytes(Path.Combine(dir, "sheet.png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Assert.That(File.Exists(Path.Combine(dir, "sheet.png")), Is.True);
        }

        private static void Blit(Color32[] dst, int dstW, Color32[] src, int size, int ox, int oy)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color32 s = src[(y * size) + x];
                    int i = ((oy + y) * dstW) + ox + x;
                    float a = s.a / 255f;
                    Color32 d = dst[i];
                    dst[i] = new Color32((byte)((s.r * a) + (d.r * (1 - a))), (byte)((s.g * a) + (d.g * (1 - a))), (byte)((s.b * a) + (d.b * (1 - a))), 255);
                }
            }
        }

        private static void Save(string dir, string name, Color32[] px, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
