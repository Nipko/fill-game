#nullable enable
using System.Collections.Generic;
using AF.EditorTools;
using AF.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Memoria (observación 8 de la prueba completa del 2026-10-25; <c>docs/dev/memoria-2026-10-26.md</c>): el pipeline sin HDR y
    /// los atlas de las fuentes sin copia en CPU (los aplica <see cref="ProjectSetup.ApplyAndroidDefaults"/> y los exige
    /// <see cref="ProjectSetup.Validate"/>), y los búferes compartidos de los lienzos del arte por código
    /// (<see cref="PixelBuffers"/>), con las texturas que salen de ellos subidas sin copia en CPU.
    /// </summary>
    public sealed class MemorySettingsTests
    {
        [TearDown]
        public void TearDown()
        {
            PixelBuffers.ResetForTests();
        }

        [Test]
        public void Pipeline_WithoutHdr_InEveryAssetTheProjectUses()
        {
            List<RenderPipelineAsset> assets = ProjectSetup.PipelineAssets();
            Assert.That(assets, Is.Not.Empty, "URP en Graphics y en Quality");
            foreach (RenderPipelineAsset asset in assets)
            {
                Assert.That(ProjectSetup.PipelineHdrOf(asset), Is.False, asset.name + ": ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults");
            }

            Assert.That(ProjectSetup.PipelineHdr, Is.False);
            Assert.That(string.Join("; ", ProjectSetup.Validate()), Does.Not.Contain("pipeline.supportsHDR"));
        }

        [Test]
        public void FontAtlases_NotReadableOnTheCpu()
        {
            Assert.That(FontSetup.ReadableAtlases(), Is.Empty, "ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults");
            foreach (string font in FontSetup.Fonts)
            {
                TMP_FontAsset? asset = Resources.Load<TMP_FontAsset>("AF/Fonts/" + font + " SDF");
                Assert.That(asset, Is.Not.Null, font);
                Assert.That(asset!.atlasTexture.isReadable, Is.False, font);
            }

            Assert.That(string.Join("; ", ProjectSetup.Validate()), Does.Not.Contain("fonts.atlasReadable"));
        }

        [Test]
        public void PixelCanvas_ToTexture_IsNotReadable_AndReleasesItsBuffer()
        {
            var pc = new PixelCanvas(200, 100);
            pc.FillCircle(100f, 50f, 30f, Color.red);
            Texture2D tex = pc.ToTexture("test_canvas");
            try
            {
                Assert.That(tex.isReadable, Is.False, "sin copia en CPU (Apply(false, true))");
                Assert.That(tex.width, Is.EqualTo(200));
                Assert.That(tex.height, Is.EqualTo(100));
                Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(200L * 100L * 16L), "el búfer vuelve para el siguiente lienzo");
                Assert.That(() => pc.Get(0, 0), Throws.TypeOf<System.IndexOutOfRangeException>(), "el lienzo ya no se usa");
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void PixelBuffers_ReuseAReleasedBuffer_Cleared_AndOnlyForCanvasesNotTooSmall()
        {
            PixelBuffers.ResetForTests();
            var big = new PixelCanvas(400, 400);
            big.Fill(Color.white);
            big.Release();
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(400L * 400L * 16L));

            var reuse = new PixelCanvas(300, 300); // 90.000 px ≥ 160.000 / 8: reutiliza
            Assert.That(PixelBuffers.Reused, Is.EqualTo(1));
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0));
            Assert.That(reuse.Get(299, 299), Is.EqualTo(new Color(0f, 0f, 0f, 0f)), "a cero, aunque el búfer venía pintado");
            reuse.Release();
            reuse.Release(); // idempotente: no se guarda dos veces
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(400L * 400L * 16L));

            var small = new PixelCanvas(64, 64); // por debajo de MinPooledPixels: nunca del pool
            Assert.That(PixelBuffers.Reused, Is.EqualTo(1));
            small.Release();
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(400L * 400L * 16L), "los pequeños no se guardan");

            var tiny = new PixelCanvas(130, 130); // 16.900 px < 160.000 / 8: no se le da el grande
            Assert.That(PixelBuffers.Reused, Is.EqualTo(1));
            tiny.Release();

            PixelBuffers.Trim();
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0));
            Assert.That(PixelBuffers.CanvasBytes, Is.EqualTo((160000L + 90000L + 4096L + 16900L) * 16L));
            Assert.That(PixelBuffers.NewBytes, Is.EqualTo((160000L + 4096L + 16900L) * 16L));
        }

        [Test]
        public void PixelBuffers_KeepAtMostMaxFree_DroppingTheSmallest()
        {
            PixelBuffers.ResetForTests();
            var canvases = new List<PixelCanvas>();
            for (int i = 0; i < PixelBuffers.MaxFree + 1; i++)
            {
                canvases.Add(new PixelCanvas(200 + (i * 10), 200));
            }

            foreach (PixelCanvas pc in canvases)
            {
                pc.Release();
            }

            long expected = 0;
            for (int i = 1; i < PixelBuffers.MaxFree + 1; i++)
            {
                expected += (200L + (i * 10)) * 200L * 16L;
            }

            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(expected), "se suelta el más pequeño");
        }
    }
}
