#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using AF.Levels;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Tiempos del arte de los personajes de juguete (D-058): el presupuesto propio del atlas de TiTino (P3D-4 a, el dueño el
    /// 2026-10-27) y las mediciones de antes y después de la fase 2 (explícita, solo escribe líneas <c>AF_PERF</c> en el log).
    /// Cada cifra es el mínimo de 3 generaciones en instancias nuevas, tras el JIT.
    /// </summary>
    public sealed class Personajes3dPerfTests
    {
        /// <summary>
        /// Presupuesto del atlas de piezas de TiTino (1024 × 512, una vez por sesión) en el Editor: el piloto lo midió en 80-102 ms
        /// (antes de D-058, 60); 150 ms deja el margen del Editor compartido sin dejarlo crecer sin control. En el teléfono, si se
        /// mide un tirón, se pinta fuera del hilo principal (P3D-4 b).
        /// </summary>
        public const double TiTinoAtlasBudgetMs = 150.0;

        [Test]
        public void TheTiTinoAtlas_StaysWithinItsOwnBudget()
        {
            ArtSlots.ForceProcedural = true;
            try
            {
                double ms = Min(art => { art.SlothRigArtProcedural(); return art.SlothArtMs; });
                Log("titino_atlas", ms);
                Assert.That(ms, Is.GreaterThan(0d), "el atlas se pinta por código");
                Assert.That(ms, Is.LessThanOrEqualTo(PerfBudget.Ms(TiTinoAtlasBudgetMs)), "atlas de TiTino (mínimo de 3), " + PerfBudget.Describe(TiTinoAtlasBudgetMs));
            }
            finally
            {
                ArtSlots.ForceProcedural = false;
            }
        }

        [Test]
        [Timeout(900000)]
        [Explicit("Mediciones de la fase 2 de personajes 3D (D-058): solo escribe AF_PERF.")]
        public void MeasureCharacterArt()
        {
            ArtSlots.ForceProcedural = true;
            try
            {
                Measure();
            }
            finally
            {
                ArtSlots.ForceProcedural = false;
            }
        }

        private static void Measure()
        {
            MicroBench();
            Log("titino_atlas", Min(art => { art.SlothRigArtProcedural(); return art.SlothArtMs; }));
            Log("titino_retrato", Min(art => Time(() => art.SlothPortrait(false))));
            Log("rosa", Min(art => Time(() => _ = art.Rosa)));
            Log("capi_canoa_mapa", Min(art => Time(() => _ = art.MapCanoe)));
            Log("bufeo_mapa", Min(art => Time(() => _ = art.MapBufeo)));
            Log("pirana_pez", Min(art => Time(() => _ = art.PiranaFishSprite)));
            Log("pirana_pez_dormida", Min(art => Time(() => _ = art.PiranaFishAsleepSprite)));
            Log("luciernaga_cuerpo_alas", Min(art => Time(() =>
            {
                _ = art.FireflyBodySprite;
                _ = art.FireflyWingsSprite;
            })));

            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            double worst = 0;
            foreach (FaunaSpecies sp in catalog.Species)
            {
                double ms = Min(art => { art.MonkeyArtFor(sp); return art.FaunaArtMs; });
                worst = Math.Max(worst, ms);
                Log("miquito_" + sp.Id, ms);
            }

            Log("miquito_peor", worst);
            foreach (int px in new[] { 192, 96, 72 })
            {
                var pc = new PixelCanvas(px, px);
                var scratch = new PixelCanvas(px, px);
                ProceduralArt.PaintAvatar(pc, scratch, 0, AvatarArtBackdrop.Leaf, AvatarArtBadge.None, false);
                foreach (bool locked in new[] { false, true })
                {
                    double best = double.MaxValue;
                    for (int round = 0; round < 3; round++)
                    {
                        var watch = Stopwatch.StartNew();
                        for (int i = 0; i < ProceduralArt.AvatarArtCount; i++)
                        {
                            pc.Fill(Color.clear);
                            ProceduralArt.PaintAvatar(pc, scratch, i, AvatarArtBackdrop.Water, locked ? AvatarArtBadge.World : AvatarArtBadge.None, locked);
                        }

                        watch.Stop();
                        best = Math.Min(best, watch.Elapsed.TotalMilliseconds / ProceduralArt.AvatarArtCount);
                    }

                    Log("avatar_" + px + (locked ? "_bloqueado" : string.Empty), best);
                }

                pc.Release();
                scratch.Release();
            }
        }

        // Coste por llamada de las piezas del sombreado (ns), para saber dónde se va el tiempo en el Editor.
        private static void MicroBench()
        {
            const int n = 200000;
            ToyTones t = ToyShading.Tones(ProceduralArt.CapiFur);
            var pc = new PixelCanvas(64, 64);
            float acc = 0f;
            var w = Stopwatch.StartNew();
            for (int i = 0; i < n; i++)
            {
                ToyShading.ShadeCore(t, 0.1f, 0.2f, 0.97f, 0f, 0.5f, 0.1f, 0.2f, 0.97f, out float r, out float g, out float b);
                acc += r;
            }

            Log("ns_shadecore", w.Elapsed.TotalMilliseconds * 1e6 / n);
            w.Restart();
            for (int i = 0; i < n; i++)
            {
                acc += ToyShading.Normal(0.1f, (i & 63) / 80f).z;
            }

            Log("ns_normal", w.Elapsed.TotalMilliseconds * 1e6 / n);
            w.Restart();
            var c = new Color(0.3f, 0.4f, 0.5f, 1f);
            for (int i = 0; i < n; i++)
            {
                pc.Blend(i & 63, (i >> 6) & 63, c, 0.5f);
            }

            Log("ns_blend", w.Elapsed.TotalMilliseconds * 1e6 / n);
            w.Restart();
            for (int i = 0; i < n; i++)
            {
                pc.Set(i & 63, (i >> 6) & 63, c);
            }

            Log("ns_set", w.Elapsed.TotalMilliseconds * 1e6 / n);
            w.Restart();
            for (int i = 0; i < n; i++)
            {
                acc += Mathf.Sqrt(i) + PixelCanvas.Coverage(acc * 1e-9f);
            }

            Log("ns_sqrt_coverage", w.Elapsed.TotalMilliseconds * 1e6 / n);
            pc.Release();
            UnityEngine.Debug.Log("AF_PERF acc " + acc.ToString(CultureInfo.InvariantCulture));
            UnityEngine.Debug.Log("AF_PERF optimizacion " + UnityEditor.Compilation.CompilationPipeline.codeOptimization);
        }

        internal static void Log(string what, double ms)
        {
            UnityEngine.Debug.Log("AF_PERF p3d " + what + " " + ms.ToString("F2", CultureInfo.InvariantCulture) + " ms");
            TestContext.WriteLine("AF_PERF p3d " + what + " " + ms.ToString("F2", CultureInfo.InvariantCulture) + " ms");
        }

        // Mínimo de 3 generaciones en instancias nuevas (la primera, aparte, para el JIT).
        internal static double Min(Func<ProceduralArt, double> generate)
        {
            var warm = new ProceduralArt("white");
            try
            {
                generate(warm);
            }
            finally
            {
                warm.Dispose();
            }

            double best = double.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                var art = new ProceduralArt("white");
                try
                {
                    best = Math.Min(best, generate(art));
                }
                finally
                {
                    art.Dispose();
                }
            }

            return best;
        }

        private static double Time(Action action)
        {
            var watch = Stopwatch.StartNew();
            action();
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds;
        }
    }
}
