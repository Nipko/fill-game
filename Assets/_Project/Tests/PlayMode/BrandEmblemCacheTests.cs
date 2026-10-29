#nullable enable
using System;
using System.Collections;
using System.IO;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Caché de disco de la escena de la marca (<see cref="BrandEmblemCache"/>; jornada 3 de D-051, observaciones 1 y 2): en el
    /// Editor apagada; con carpeta, la primera vez dibuja y guarda en segundo plano y la segunda lee lo mismo; la clave cambia con
    /// la pantalla, la densidad, los datos y el build; un archivo de otra clave, corto, largo o con basura se descarta y se dibuja.
    /// </summary>
    public sealed class BrandEmblemCacheTests
    {
        private string _dir = string.Empty;
        private BrandEmblemArt? _a;
        private BrandEmblemArt? _b;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Application.temporaryCachePath, "af-brand-cache-test-" + Guid.NewGuid().ToString("N"));
            BrandEmblemCache.BuildOverride = "build-a";
        }

        [TearDown]
        public void TearDown()
        {
            BrandEmblemCache.PendingWrite?.Wait(20000);
            _a?.Dispose();
            _b?.Dispose();
            _a = null;
            _b = null;
            BrandEmblemCache.DirectoryOverride = null;
            BrandEmblemCache.BuildOverride = null;
            BrandEmblemCache.KeepReadableForTests = false;
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        [Test]
        public void InTheEditor_TheCacheIsOff_AndTheSceneIsDrawn()
        {
            Assert.That(BrandEmblemCache.Active, Is.False, "en el Editor se itera el arte");
            Assert.That(BrandEmblemCache.FilePath, Is.Null);
            _a = BrandEmblemArt.Create(360f, 780f, 3f);
            Assert.That(_a.Cache, Is.EqualTo(BrandEmblemCacheResult.Off));
        }

        [UnityTest]
        public IEnumerator FirstOpen_DrawsAndSaves_SecondOpen_ReadsTheSamePieces()
        {
            BrandEmblemCache.DirectoryOverride = _dir;
            _a = BrandEmblemArt.Create(360f, 780f, 3f);
            Assert.That(_a.Cache, Is.EqualTo(BrandEmblemCacheResult.Miss), "primera apertura: sin archivo");
            yield return WaitWrite();
            Assert.That(File.Exists(BrandEmblemCache.FilePath), Is.True, "guardada en segundo plano");
            Assert.That(File.Exists(BrandEmblemCache.FilePath + ".tmp"), Is.False, "el temporal pasó a su sitio");

            _b = BrandEmblemArt.Create(360f, 780f, 3f);
            Assert.That(_b.Cache, Is.EqualTo(BrandEmblemCacheResult.Hit), "segunda apertura: de la caché");
            for (int i = 0; i < BrandEmblemArt.Count; i++)
            {
                Assert.That(_b.DpRect(i), Is.EqualTo(_a.DpRect(i)), "pieza " + i + ": en el mismo sitio");
                Assert.That(_b.Sprite(i)!.texture.width, Is.EqualTo(_a.Sprite(i)!.texture.width), "pieza " + i);
                Assert.That(_b.Sprite(i)!.texture.height, Is.EqualTo(_a.Sprite(i)!.texture.height), "pieza " + i);
            }

            Debug.Log("AF_SPLASH caché de la escena (360 × 780 dp a 3 px/dp): dibujar " + _a.BuildMs + " ms, leer " + _b.BuildMs + " ms; archivo de "
                + (new FileInfo(BrandEmblemCache.FilePath!).Length / 1024) + " KB");
            Assert.That(_b.BuildMs, Is.LessThan(_a.BuildMs), "leer cuesta menos que dibujar");
        }

        [UnityTest]
        public IEnumerator TheReadPixels_AreTheWrittenOnes()
        {
            BrandEmblemCache.DirectoryOverride = _dir;
            BrandEmblemCache.KeepReadableForTests = true;
            var px = new Color32[2][];
            px[0] = new Color32[3 * 2];
            px[1] = new Color32[1 * 4];
            for (int i = 0; i < px[0].Length; i++)
            {
                px[0][i] = new Color32((byte)(10 * i), (byte)(255 - i), 7, (byte)(i * 40));
            }

            for (int i = 0; i < px[1].Length; i++)
            {
                px[1][i] = new Color32(1, 2, (byte)i, 255);
            }

            var sizes = new[] { new Vector2Int(3, 2), new Vector2Int(1, 4) };
            var rects = new[] { new Rect(1.5f, 2f, 1f, 0.66f), new Rect(-3f, 4.25f, 0.33f, 1.33f) };
            BrandEmblemCache.Write(BrandEmblemCache.FilePath!, "clave", px, sizes, rects);
            yield return null;

            var textures = new Texture2D?[2];
            var read = new Rect[2];
            Assert.That(BrandEmblemCache.TryLoad("clave", textures, read), Is.True);
            try
            {
                for (int k = 0; k < 2; k++)
                {
                    Assert.That(read[k], Is.EqualTo(rects[k]));
                    Assert.That(textures[k]!.width, Is.EqualTo(sizes[k].x));
                    Assert.That(textures[k]!.height, Is.EqualTo(sizes[k].y));
                    Assert.That(textures[k]!.GetPixels32(), Is.EqualTo(px[k]), "los mismos bytes RGBA32");
                }
            }
            finally
            {
                foreach (Texture2D? t in textures)
                {
                    UnityEngine.Object.Destroy(t);
                }
            }
        }

        [Test]
        public void TheKey_ChangesWithTheScreen_TheDensity_TheData_AndTheBuild()
        {
            string key = BrandEmblemCache.KeyFor(360f, 780f, 3f);
            Assert.That(BrandEmblemCache.KeyFor(360f, 780f, 3f), Is.EqualTo(key), "estable");
            Assert.That(BrandEmblemCache.KeyFor(361f, 780f, 3f), Is.Not.EqualTo(key), "otro ancho");
            Assert.That(BrandEmblemCache.KeyFor(360f, 640f, 3f), Is.Not.EqualTo(key), "otro alto (16:9)");
            Assert.That(BrandEmblemCache.KeyFor(360f, 780f, 2.625f), Is.Not.EqualTo(key), "otra densidad");
            StringAssert.Contains("d" + BrandRiverScene.DataHash.ToString("x16"), key, "la huella de los datos generados y del dibujo");
            Assert.That(BrandRiverScene.DataHash, Is.Not.EqualTo(0UL));
            BrandEmblemCache.BuildOverride = "build-b";
            Assert.That(BrandEmblemCache.KeyFor(360f, 780f, 3f), Is.Not.EqualTo(key), "otro build");
        }

        [UnityTest]
        public IEnumerator AFileOfAnotherKey_IsDiscarded_AndTheSceneIsDrawnAgain()
        {
            BrandEmblemCache.DirectoryOverride = _dir;
            _a = BrandEmblemArt.Create(360f, 780f, 3f);
            yield return WaitWrite();

            BrandEmblemCache.BuildOverride = "build-b"; // otro build (o, igual, otros datos u otra pantalla)
            _b = BrandEmblemArt.Create(360f, 780f, 3f);
            Assert.That(_b.Cache, Is.EqualTo(BrandEmblemCacheResult.Miss), "no sirve: se dibuja");
            yield return WaitWrite();
            _b.Dispose();
            _b = BrandEmblemArt.Create(360f, 780f, 3f);
            Assert.That(_b.Cache, Is.EqualTo(BrandEmblemCacheResult.Hit), "y se guardó con la clave nueva");
        }

        [UnityTest]
        public IEnumerator ACorruptFile_IsDiscarded_NeverServedHalfway([Values("corto", "largo", "basura", "vacio")] string damage)
        {
            BrandEmblemCache.DirectoryOverride = _dir;
            _a = BrandEmblemArt.Create(360f, 780f, 3f);
            yield return WaitWrite();
            _a.Dispose();
            _a = null;
            string path = BrandEmblemCache.FilePath!;
            byte[] good = File.ReadAllBytes(path);
            byte[] bad = damage switch
            {
                "corto" => good.AsSpan(0, good.Length - 9).ToArray(),
                "largo" => Concat(good, new byte[] { 1, 2, 3 }),
                "basura" => new byte[] { 0x41, 0x46, 0x42, 0x45, 0xFF, 0xFF, 0xFF, 0x7F, 9, 9, 9, 9, 9, 9 },
                _ => Array.Empty<byte>(),
            };
            File.WriteAllBytes(path, bad);

            _b = BrandEmblemArt.Create(360f, 780f, 3f);
            Assert.That(_b.Cache, Is.EqualTo(BrandEmblemCacheResult.Miss), damage + ": se dibuja");
            Assert.That(_b.Sprite(BrandEmblemArt.Cuia), Is.Not.Null);
            yield return WaitWrite();
            Assert.That(File.ReadAllBytes(path).Length, Is.EqualTo(good.Length), "rehecha entera");
        }

        [Test]
        public void ADiskFailure_OnlyWarns()
        {
            string file = Path.Combine(Application.temporaryCachePath, "af-brand-cache-blocker-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(file, "x"); // un archivo donde debería ir la carpeta
            try
            {
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[AF\.BrandEmblemCache\] no se pudo guardar"));
                Assert.DoesNotThrow(() => BrandEmblemCache.Write(Path.Combine(file, "sub", BrandEmblemCache.FileName), "k", new Color32[]?[] { new Color32[1] }, new[] { Vector2Int.one }, new[] { new Rect(0, 0, 1, 1) }));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [UnityTest]
        public IEnumerator TheBootSplash_ReadsTheSceneFromTheCache_FromTheSecondOpen()
        {
            BrandEmblemCache.DirectoryOverride = _dir;
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            var host = new GameObject("BrandEmblemCacheHost");
            try
            {
                var first = new BootSplash(host.transform, withSloth: false, withIntro: true);
                Assert.That(first.EmblemCache, Is.EqualTo(BrandEmblemCacheResult.Miss));
                first.Dispose();
                yield return WaitWrite();
                var second = new BootSplash(host.transform, withSloth: false, withIntro: true);
                Assert.That(second.EmblemCache, Is.EqualTo(BrandEmblemCacheResult.Hit));
                Assert.That(second.Intro!.Piece("EmblemCuia")!.GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null, "la cuia, desde el primer fotograma");
                Debug.Log("AF_SPLASH pantalla de arranque: escena dibujada " + first.EmblemMs + " ms, leída de la caché " + second.EmblemMs + " ms");
                BrandIntroReport report = second.EndIntro()!;
                Assert.That(report.EmblemCache, Is.EqualTo(BrandEmblemCacheResult.Hit), "emblem_cache de la sonda");
                Assert.That(report.EmblemMs, Is.EqualTo(second.EmblemMs));
                second.Dispose();
            }
            finally
            {
                GameScreen.SimulatedScreen = null;
                UnityEngine.Object.Destroy(host);
            }
        }

        private static IEnumerator WaitWrite()
        {
            double until = Time.realtimeSinceStartupAsDouble + 20.0;
            while (BrandEmblemCache.PendingWrite != null && Time.realtimeSinceStartupAsDouble < until)
            {
                yield return null;
            }

            Assert.That(BrandEmblemCache.PendingWrite, Is.Null, "la escritura en segundo plano terminó");
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            var r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }
    }
}
