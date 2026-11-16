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

        /// <summary>
        /// Android Blit Type en <c>Auto</c> (memoria, 2026-10-29; <c>docs/dev/memoria.md</c>): sin el búfer de pantalla completa
        /// fuera de la pantalla de <c>Always</c>, que el juego no necesita (sin MSAA, HDR, posproceso ni escala de render).
        /// </summary>
        [Test]
        public void AndroidBlitType_Auto_AndNoIntermediateTargetsInThePipeline()
        {
            Assert.That(ProjectSetup.BlitType, Is.EqualTo(UnityEditor.AndroidBlitType.Auto));
            Assert.That(UnityEditor.PlayerSettings.Android.blitType, Is.EqualTo(UnityEditor.AndroidBlitType.Auto), "ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults");
            Assert.That(string.Join("; ", ProjectSetup.Validate()), Does.Not.Contain("android.blitType"));
            foreach (RenderPipelineAsset asset in ProjectSetup.PipelineAssets())
            {
                // El asset de URP por sus propiedades serializadas (este ensamblado no referencia URP).
                var so = new UnityEditor.SerializedObject(asset);
                Assert.That(so.FindProperty("m_MSAA")!.intValue, Is.EqualTo(1), asset.name + ": sin MSAA (el 2D no lo necesita)");
                Assert.That(so.FindProperty("m_RenderScale")!.floatValue, Is.EqualTo(1f), asset.name + ": escala 1 (otra cambiaría el aspecto y pediría un búfer intermedio)");
                Assert.That(so.FindProperty("m_RequireDepthTexture")!.boolValue, Is.False, asset.name);
                Assert.That(so.FindProperty("m_RequireOpaqueTexture")!.boolValue, Is.False, asset.name);
            }
        }

        /// <summary>
        /// El 2D Renderer sin datos de posproceso (observación 14 de la jornada 6; <c>docs/dev/memoria.md</c> §7): el juego no pinta
        /// posproceso y así el reproductor no carga las texturas de grano, las de SMAA ni los shaders de posproceso.
        /// </summary>
        [Test]
        public void Renderer2D_WithoutPostProcessData_InEveryRendererTheProjectUses()
        {
            Assert.That(ProjectSetup.RendererPostProcessData, Is.False);
            List<ScriptableObject> renderers = ProjectSetup.RendererDatas();
            Assert.That(renderers, Is.Not.Empty, "el asset de URP tiene su 2D Renderer");
            foreach (ScriptableObject renderer in renderers)
            {
                Assert.That(ProjectSetup.RendererHasPostProcessData(renderer), Is.False, renderer.name + ": ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults");
            }

            Assert.That(string.Join("; ", ProjectSetup.Validate()), Does.Not.Contain("renderer.postProcessData"));
        }

        /// <summary>
        /// Añadido de D-060 del 2026-10-29 (observaciones 14 y 20 de la jornada 6; <c>docs/dev/memoria.md</c> §7.8): en Android, OpenGL
        /// ES 3 primero y Vulkan de respaldo, no la lista automática de Unity.
        /// </summary>
        [Test]
        public void AndroidGraphicsApis_OpenGles3First_VulkanAsFallback()
        {
            Assert.That(ProjectSetup.UseDefaultGraphicsApis, Is.False);
            Assert.That(ProjectSetup.AndroidGraphicsApis, Is.EqualTo(new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan }));
            Assert.That(UnityEditor.PlayerSettings.GetUseDefaultGraphicsAPIs(UnityEditor.BuildTarget.Android), Is.False, "ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults");
            Assert.That(UnityEditor.PlayerSettings.GetGraphicsAPIs(UnityEditor.BuildTarget.Android), Is.EqualTo(ProjectSetup.AndroidGraphicsApis));
            string problems = string.Join("; ", ProjectSetup.Validate());
            Assert.That(problems, Does.Not.Contain("android.useDefaultGraphicsAPIs").And.Not.Contain("android.graphicsAPIs"));
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
        public void PixelCanvas_OnAReusedPaintedBuffer_PaintsTheSameAsOnANewOne()
        {
            PixelBuffers.ResetForTests();
            var old = new PixelCanvas(400, 400);
            old.Fill(new Color(0.9f, 0.1f, 0.3f, 1f));
            old.Release();

            var reused = new PixelCanvas(300, 300);
            Assert.That(PixelBuffers.Reused, Is.EqualTo(1), "sale del búfer pintado de 400 × 400");
            Paint(reused);
            Color32[] got = reused.ToColor32();

            PixelBuffers.EndPooling(); // sin búferes libres: el siguiente es nuevo
            var fresh = new PixelCanvas(300, 300);
            Assert.That(PixelBuffers.Reused, Is.EqualTo(1));
            Paint(fresh);
            Assert.That(got, Is.EqualTo(fresh.ToColor32()), "los mismos píxeles: nada del lienzo anterior se cuela");

            Texture2D tex = reused.ToTexture("test_reused");
            try
            {
                Assert.That(tex.width, Is.EqualTo(300));
                Assert.That(tex.height, Is.EqualTo(300));
                Assert.That(tex.isReadable, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void PixelBuffers_AfterEndPooling_KeepNothing()
        {
            PixelBuffers.ResetForTests();
            PixelBuffers.EndPooling();
            Assert.That(PixelBuffers.Pooling, Is.False);
            new PixelCanvas(400, 400).Release();
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0), "después del arranque ningún búfer queda vivo");
            PixelBuffers.BeginPooling();
            new PixelCanvas(400, 400).Release();
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(400L * 400L * 16L));
        }

        // ---- La reserva del Mapa (docs/dev/memoria.md §10; observación 2 de la prueba r2b de la 1.1) ----

        private const long Px400 = 400L * 400L * 16L;
        private const long Px600 = 600L * 600L * 16L;

        // Una apertura del Mapa con un lienzo de side × side px: BeginPooling, el lienzo y EndPooling(true).
        private static void MapOpening(int side)
        {
            PixelBuffers.BeginPooling();
            new PixelCanvas(side, side).Release();
            PixelBuffers.EndPooling(true);
        }

        [Test]
        public void Reserve_EndPoolingKeepLargest_KeepsTheLargestFreeBuffer_AndNothingElse()
        {
            PixelBuffers.ResetForTests();
            PixelBuffers.EndPooling();
            PixelBuffers.BeginPooling();
            var a = new PixelCanvas(400, 400);
            var b = new PixelCanvas(300, 300); // a la vez: búferes distintos
            var c = new PixelCanvas(200, 200);
            a.Release();
            b.Release();
            c.Release();
            PixelBuffers.EndPooling(true);
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(Px400), "el más grande se queda de reserva");
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0L), "los demás se sueltan");
            Assert.That(PixelBuffers.Pooling, Is.False);
        }

        [Test]
        public void Reserve_BeginPooling_LendsIt_AndARentThatFitsReusesIt_Cleared()
        {
            PixelBuffers.ResetForTests();
            PixelBuffers.EndPooling();
            PixelBuffers.BeginPooling();
            var first = new PixelCanvas(400, 400);
            first.Fill(Color.white);
            first.Release();
            PixelBuffers.EndPooling(true);
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(Px400));

            int reused = PixelBuffers.Reused;
            long fresh = PixelBuffers.NewBytes;
            PixelBuffers.BeginPooling();
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(0L), "prestada");
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(Px400), "entre los libres");
            var next = new PixelCanvas(300, 300);
            Assert.That(PixelBuffers.Reused, Is.EqualTo(reused + 1), "la reutiliza");
            Assert.That(PixelBuffers.NewBytes, Is.EqualTo(fresh), "sin búfer nuevo");
            Assert.That(next.Get(0, 0), Is.EqualTo(new Color(0f, 0f, 0f, 0f)));
            Assert.That(next.Get(299, 299), Is.EqualTo(new Color(0f, 0f, 0f, 0f)), "a cero en los count primeros, aunque venía pintada");
            next.Release();
            PixelBuffers.EndPooling(true);
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(Px400), "vuelve");
        }

        [Test]
        public void Reserve_LentToAnotherPooling_ComesBackWithoutGrowing()
        {
            // El camino de RestoreHomeScene (LobbyScreen): BeginPooling + EndPooling() con la reserva guardada.
            PixelBuffers.ResetForTests();
            PixelBuffers.EndPooling();
            MapOpening(400);
            PixelBuffers.BeginPooling();
            var small = new PixelCanvas(300, 300); // usa la reserva
            var bigger = new PixelCanvas(600, 600); // nuevo, mayor que la reserva
            small.Release();
            bigger.Release();
            PixelBuffers.EndPooling();
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(Px400), "vuelve la de su medida, no el búfer mayor");
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0L));
        }

        [Test]
        public void Reserve_ANextCanvasBiggerThanIt_GetsANewBuffer_AndTheReserveGrows()
        {
            PixelBuffers.ResetForTests();
            PixelBuffers.EndPooling();
            MapOpening(400);
            long fresh = PixelBuffers.NewBytes;
            MapOpening(600);
            Assert.That(PixelBuffers.NewBytes - fresh, Is.EqualTo(Px600), "no cabe en la reserva: búfer nuevo");
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(Px600), "EndPooling(true) guarda el mayor");
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0L));
        }

        [Test]
        public void Reserve_ReleasedWhileLent_ThenEndPooling_LeavesNothing()
        {
            PixelBuffers.ResetForTests();
            PixelBuffers.EndPooling();
            MapOpening(400);
            PixelBuffers.BeginPooling();
            PixelBuffers.ReleaseReserve();
            PixelBuffers.EndPooling();
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(0L), "no queda reserva");
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0L));
        }

        [Test]
        public void Reserve_TwoBeginPoolingInARow_DoNotAddItTwice()
        {
            PixelBuffers.ResetForTests();
            PixelBuffers.EndPooling();
            MapOpening(400);
            PixelBuffers.BeginPooling();
            PixelBuffers.BeginPooling();
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(Px400), "una sola vez entre los libres");
            var a = new PixelCanvas(400, 400);
            int reused = PixelBuffers.Reused;
            var b = new PixelCanvas(400, 400);
            Assert.That(PixelBuffers.Reused, Is.EqualTo(reused), "el segundo lienzo no recibe el mismo búfer");
            a.Release();
            b.Release();
            PixelBuffers.EndPooling();
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(Px400));
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0L));
        }

        [Test]
        public void Reserve_AfterLowMemory_TheNextOpeningDoesNotKeepIt_TheOneAfterDoes()
        {
            PixelBuffers.ResetForTests();
            PixelBuffers.EndPooling();
            MapOpening(400);
            PixelBuffers.ReleaseReserveForLowMemory();
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(0L));
            Assert.That(PixelBuffers.ReserveDroppedForLowMemory, Is.True);

            MapOpening(400);
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(0L), "la apertura que sigue al aviso no la guarda");
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0L));
            Assert.That(PixelBuffers.ReserveDroppedForLowMemory, Is.False);

            MapOpening(400);
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(Px400), "la siguiente apertura normal, sí");
        }

        [Test]
        public void Reserve_LowMemoryWhileLent_TheOpeningInCourseDoesNotKeepIt()
        {
            PixelBuffers.ResetForTests();
            PixelBuffers.EndPooling();
            MapOpening(400);
            PixelBuffers.BeginPooling();
            new PixelCanvas(400, 400).Release();
            PixelBuffers.ReleaseReserveForLowMemory();
            PixelBuffers.EndPooling(true);
            Assert.That(PixelBuffers.ReserveBytes, Is.EqualTo(0L));
            Assert.That(PixelBuffers.FreeBytes, Is.EqualTo(0L));
        }

        private static void Paint(PixelCanvas pc)
        {
            pc.FillRoundedRect(10f, 20f, 280f, 250f, 18f, new Color(0.2f, 0.6f, 0.4f, 0.7f));
            pc.FillCircle(150f, 150f, 60.5f, new Color(1f, 0.8f, 0.1f, 0.5f));
            pc.FillEllipseSoft(90f, 200f, 40f, 25f, 6f, new Color(0f, 0f, 0f, 0.4f));
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
