#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Emisores de FX al estilo Magic Sort (T19; ficha de Magic Sort §3; arquitectura del paso 1 §9 y §12): destellos de
    /// estrella, esquirlas y burbujas que emiten y se apagan solos; un pool común con Splash y SealDrops que nunca pasa de
    /// <see cref="FxLayer.MaxParticles"/> (60 de la guía menos 16 de ambiente) ni de <see cref="FxLayer.MaxAdditive"/> brillos
    /// aditivos; nada con Reducir movimiento (02 §4); y 0 B al emitir y en <see cref="FxLayer.Tick"/> (08 §5.4), también tras
    /// reordenar las capas del tablero como hace GameScreen en cada nivel. Una <see cref="FxLayer"/> real en un Canvas propio,
    /// con el reloj en manos del test.
    /// </summary>
    public sealed class FxEmitterTests
    {
        private const float FrameMs = 16f;
        private const float U = 24f; // dp por u: cuia de 48 dp (02 §5)
        private static readonly Vector2 At = new Vector2(180f, -400f);
        private static readonly Color Ice = new Color(0.55f, 0.80f, 0.95f, 1f);
        private static ProceduralArt? s_art;

        private GameObject? _root;
        private FxLayer? _fx;
        private RectTransform? _other;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            _root = null;
            _fx = null;
            _other = null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        [UnityTest]
        public IEnumerator Sparkles_EmitAndExpire()
        {
            yield return StartRig();
            FxLayer fx = _fx!;
            fx.Sparkles(At, Color.white, 4, U);
            Assert.That(fx.ActiveCount, Is.EqualTo(4));
            Assert.That(fx.AdditiveCount, Is.EqualTo(4), "los destellos son brillos aditivos");
            List<Image> sparkles = Shown(fx, Art.StarSparkle);
            Assert.That(sparkles.Count, Is.EqualTo(4));
            if (UiMaterials.Additive != null)
            {
                foreach (Image img in sparkles)
                {
                    Assert.That(img.material, Is.SameAs(UiMaterials.Additive), "aditivos (sin Bloom)");
                }
            }

            var born = new float[4];
            var gone = new float[4];
            var peak = new float[4];
            var lastScale = new float[4];
            var lastAlpha = new float[4];
            for (int k = 0; k < 4; k++)
            {
                born[k] = -1f;
                gone[k] = -1f;
            }

            float t = 0f;
            while (fx.ActiveCount > 0 && t < 2000f)
            {
                fx.Tick(FrameMs);
                t += FrameMs;
                for (int k = 0; k < 4; k++)
                {
                    Image img = sparkles[k];
                    if (img.enabled)
                    {
                        if (born[k] < 0f && img.color.a > 0f)
                        {
                            born[k] = t;
                        }

                        float s = img.rectTransform.localScale.x;
                        peak[k] = Mathf.Max(peak[k], s);
                        lastScale[k] = s;
                        lastAlpha[k] = img.color.a;
                    }
                    else if (gone[k] < 0f)
                    {
                        gone[k] = t;
                    }
                }

                yield return null;
            }

            Assert.That(fx.ActiveCount, Is.EqualTo(0), "se apagan solos");
            Assert.That(t, Is.LessThanOrEqualTo(FxLayer.SparkleDelayMaxMs + FxLayer.SparkleLifeMaxMs + FrameMs));
            for (int k = 0; k < 4; k++)
            {
                Assert.That(born[k], Is.GreaterThan(0f), "destello " + k + " visible");
                Assert.That(born[k], Is.LessThanOrEqualTo(FxLayer.SparkleDelayMaxMs + FrameMs), "destello " + k + ": retardo corto");
                Assert.That(gone[k] - born[k], Is.InRange(FxLayer.SparkleLifeMinMs - FrameMs, FxLayer.SparkleLifeMaxMs + FrameMs), "destello " + k + ": crece y se apaga en 300-400 ms");
                Assert.That(peak[k], Is.GreaterThanOrEqualTo(0.95f), "destello " + k + " crece hasta su tamaño");
                Assert.That(lastScale[k], Is.LessThan(peak[k] * 0.6f), "destello " + k + " mengua al final");
                Assert.That(lastAlpha[k], Is.LessThan(0.3f), "destello " + k + " se apaga");
            }

            Assert.That(fx.AdditiveCount, Is.EqualTo(0));
            Assert.That(Shown(fx, null).Count, Is.EqualTo(0), "ninguna imagen a la vista");
        }

        [UnityTest]
        public IEnumerator Shards_EmitAndExpire()
        {
            yield return StartRig();
            FxLayer fx = _fx!;
            fx.Shards(At, Ice, 6, U);
            Assert.That(fx.ActiveCount, Is.EqualTo(6));
            Assert.That(fx.AdditiveCount, Is.EqualTo(0));
            List<Image> shards = Shown(fx, Art.Shard);
            Assert.That(shards.Count, Is.EqualTo(6));
            var startY = new float[6];
            var startAngle = new float[6];
            var maxY = new float[6];
            var lastY = new float[6];
            var maxTurn = new float[6];
            for (int k = 0; k < 6; k++)
            {
                Color c = shards[k].color;
                Assert.That(new Color(c.r, c.g, c.b, 1f), Is.EqualTo(Ice), "esquirla " + k + " del color pedido");
                Assert.That(c.a, Is.EqualTo(1f).Within(1e-4f), "esquirla " + k + " a la vista desde el primer fotograma");
                startY[k] = shards[k].rectTransform.anchoredPosition.y;
                startAngle[k] = shards[k].rectTransform.localEulerAngles.z;
                maxY[k] = startY[k];
            }

            float t = 0f;
            while (fx.ActiveCount > 0 && t < 2000f)
            {
                fx.Tick(FrameMs);
                t += FrameMs;
                if (t < FxLayer.ShardLifeMinMs - FrameMs)
                {
                    Assert.That(fx.ActiveCount, Is.EqualTo(6), "ninguna se va antes de tiempo (t = " + t + ")");
                }

                for (int k = 0; k < 6; k++)
                {
                    if (shards[k].enabled)
                    {
                        float y = shards[k].rectTransform.anchoredPosition.y;
                        maxY[k] = Mathf.Max(maxY[k], y);
                        lastY[k] = y;
                        float turn = Mathf.Abs(Mathf.DeltaAngle(startAngle[k], shards[k].rectTransform.localEulerAngles.z));
                        maxTurn[k] = Mathf.Max(maxTurn[k], turn);
                    }
                }

                yield return null;
            }

            Assert.That(fx.ActiveCount, Is.EqualTo(0), "se apagan solas");
            Assert.That(t, Is.LessThanOrEqualTo(FxLayer.ShardLifeMaxMs + FrameMs));
            float highest = 0f;
            for (int k = 0; k < 6; k++)
            {
                Assert.That(maxY[k], Is.GreaterThan(startY[k]), "esquirla " + k + " salta");
                Assert.That(lastY[k], Is.LessThan(maxY[k]), "esquirla " + k + " cae");
                Assert.That(maxTurn[k], Is.GreaterThan(30f), "esquirla " + k + " gira");
                highest = Mathf.Max(highest, maxY[k] - startY[k]);
            }

            Assert.That(highest, Is.GreaterThan(0.25f * U), "alguna sube más de un cuarto de u");
            Assert.That(Shown(fx, null).Count, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Bubbles_EmitAndExpire()
        {
            yield return StartRig();
            FxLayer fx = _fx!;
            fx.Bubbles(At, 3, U);
            Assert.That(fx.ActiveCount, Is.EqualTo(3));
            Assert.That(fx.AdditiveCount, Is.EqualTo(0));
            List<Image> bubbles = Shown(fx, Art.Bubble);
            Assert.That(bubbles.Count, Is.EqualTo(3));
            var startY = new float[3];
            var lastY = new float[3];
            var maxScale = new float[3];
            var lastAlpha = new float[3];
            for (int k = 0; k < 3; k++)
            {
                Color c = bubbles[k].color;
                Assert.That(new Color(c.r, c.g, c.b, 1f), Is.EqualTo(Color.white), "burbuja " + k + " blanca");
                startY[k] = bubbles[k].rectTransform.anchoredPosition.y;
            }

            float t = 0f;
            while (fx.ActiveCount > 0 && t < 2000f)
            {
                fx.Tick(FrameMs);
                t += FrameMs;
                for (int k = 0; k < 3; k++)
                {
                    if (bubbles[k].enabled)
                    {
                        lastY[k] = bubbles[k].rectTransform.anchoredPosition.y;
                        maxScale[k] = Mathf.Max(maxScale[k], bubbles[k].rectTransform.localScale.x);
                        lastAlpha[k] = bubbles[k].color.a;
                    }
                }

                yield return null;
            }

            Assert.That(fx.ActiveCount, Is.EqualTo(0), "estallan y se van solas");
            Assert.That(t, Is.LessThanOrEqualTo(FxLayer.BubbleDelayMaxMs + FxLayer.BubbleLifeMaxMs + FrameMs));
            for (int k = 0; k < 3; k++)
            {
                Assert.That(lastY[k] - startY[k], Is.GreaterThan(0.5f * U), "burbuja " + k + " sube");
                Assert.That(maxScale[k], Is.GreaterThan(1.25f), "burbuja " + k + " estalla (crece al final)");
                Assert.That(lastAlpha[k], Is.LessThan(0.3f), "burbuja " + k + " se apaga al estallar");
            }

            Assert.That(Shown(fx, null).Count, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator GlobalCap_44()
        {
            yield return StartRig();
            FxLayer fx = _fx!;
            Assert.That(FxLayer.MaxParticles, Is.EqualTo(44), "60 de la guía menos 16 de ambiente");

            // 10 llamadas seguidas de 8: caben 44 y el resto se descarta.
            for (int call = 0; call < 10; call++)
            {
                fx.Shards(At, Ice, 8, U);
                Assert.That(fx.ActiveCount, Is.LessThanOrEqualTo(FxLayer.MaxParticles), "llamada " + call);
            }

            Assert.That(fx.ActiveCount, Is.EqualTo(FxLayer.MaxParticles), "el tope se alcanza");
            Assert.That(fx.DiscardedCount, Is.EqualTo((10 * 8) - FxLayer.MaxParticles));
            Assert.That(Shown(fx, null).Count, Is.EqualTo(FxLayer.MaxParticles));

            // Splash y SealDrops comparten el pool: con el tope lleno no emiten.
            int discarded = fx.DiscardedCount;
            fx.Splash(At, Ice, U);
            fx.SealDrops(At, Ice, U);
            Assert.That(fx.ActiveCount, Is.EqualTo(FxLayer.MaxParticles));
            Assert.That(fx.DiscardedCount, Is.GreaterThanOrEqualTo(discarded + 5 + 6), "4-6 gotas y el aro, y 6-8 gotas");
            Assert.That(Shown(fx, Art.Drop).Count, Is.EqualTo(0));
            yield return RunUntilIdle(fx, 2000f, null);
            Assert.That(fx.ActiveCount, Is.EqualTo(0));

            // Todos los emisores mezclados, 10 rondas de 8 con el reloj en marcha: nunca más de 44 ni de 8 aditivos, y las
            // imágenes a la vista son las que cuenta la capa.
            for (int round = 0; round < 10; round++)
            {
                fx.Splash(At, Ice, U);
                fx.SealDrops(At, Ice, U);
                fx.Sparkles(At, Color.white, 8, U);
                fx.Shards(At, Ice, 8, U);
                fx.Bubbles(At, 8, U);
                AssertWithinCaps(fx, "ronda " + round);
                fx.Tick(FrameMs);
                AssertWithinCaps(fx, "ronda " + round + " tras Tick");
                yield return null;
            }

            yield return RunUntilIdle(fx, 2000f, f => AssertWithinCaps(f, "apagándose"));
            Assert.That(fx.ActiveCount, Is.EqualTo(0));
            Assert.That(fx.AdditiveCount, Is.EqualTo(0));
            Assert.That(Shown(fx, null).Count, Is.EqualTo(0));

            // Tope de brillos aditivos (guía 8): de 10 × 8 destellos quedan 8.
            for (int call = 0; call < 10; call++)
            {
                fx.Sparkles(At, Color.white, 8, U);
            }

            Assert.That(fx.AdditiveCount, Is.EqualTo(FxLayer.MaxAdditive));
            Assert.That(fx.ActiveCount, Is.EqualTo(FxLayer.MaxAdditive));
        }

        [UnityTest]
        public IEnumerator ReduceMotion_NoParticles()
        {
            yield return StartRig();
            FxLayer fx = _fx!;
            fx.ReduceMotion = true;
            fx.Splash(At, Ice, U);
            fx.SealDrops(At, Ice, U);
            fx.Sparkles(At, Color.white, 8, U);
            fx.Shards(At, Ice, 8, U);
            fx.Bubbles(At, 8, U);
            Assert.That(fx.ActiveCount, Is.EqualTo(0), "ningún emisor emite (02 §4)");
            Assert.That(fx.DiscardedCount, Is.EqualTo(0), "no se intenta: no hay descartes");
            for (int f = 0; f < 10; f++)
            {
                fx.Tick(FrameMs);
                Assert.That(fx.ActiveCount, Is.EqualTo(0));
                yield return null;
            }

            Assert.That(Shown(fx, null).Count, Is.EqualTo(0), "ninguna imagen a la vista");

            // Sin Reducir movimiento, los mismos emisores sí emiten (el camino existe).
            fx.ReduceMotion = false;
            fx.Sparkles(At, Color.white, 3, U);
            fx.Bubbles(At, 2, U);
            Assert.That(fx.ActiveCount, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator NoAllocOnEmitAndTick()
        {
            yield return StartRig();
            FxLayer fx = _fx!;
            RectTransform other = _other!;
            Vector2 at = At;
            Color tint = Ice;
            TestDelegate emit = () =>
            {
                fx.Splash(at, tint, U);
                fx.SealDrops(at, tint, U);
                fx.Sparkles(at, Color.white, 4, U);
                fx.Shards(at, tint, 8, U);
                fx.Bubbles(at, 3, U);
            };
            TestDelegate flood = () =>
            {
                for (int k = 0; k < 10; k++)
                {
                    fx.Shards(at, tint, 8, U);
                }
            };
            TestDelegate tick = () => fx.Tick(FrameMs);

            // Calentamiento: cada plaza pasa por cada tipo de partícula (y por el descarte), con fotogramas reales para que
            // UGUI reconstruya el Canvas.
            for (int pass = 0; pass < 2; pass++)
            {
                fx.Shards(at, tint, FxLayer.MaxParticles, U);
                yield return RunTicks(tick, 50);
                fx.Bubbles(at, FxLayer.MaxParticles, U);
                yield return RunTicks(tick, 60);
                for (int k = 0; k < 8; k++)
                {
                    fx.Splash(at, tint, U);
                    fx.Sparkles(at, Color.white, 2, U);
                }

                yield return RunTicks(tick, 40);
                emit();
                flood();
                yield return RunTicks(tick, 60);
            }

            Assert.That(fx.ActiveCount, Is.EqualTo(0), "calentamiento terminado");

            // Las capas del tablero se reordenan en cada nivel (GameScreen.OrderBoardLayers): emitir después tampoco asigna.
            fx.Root.SetAsFirstSibling();
            yield return null;
            fx.Root.SetAsLastSibling();
            Assert.That(other.GetSiblingIndex(), Is.LessThan(fx.Root.GetSiblingIndex()));
            yield return null;

            Assert.That(emit, Is.Not.AllocatingGCMemory(), "emitir");
            Assert.That(fx.ActiveCount, Is.GreaterThan(0));
            for (int f = 0; f < 60; f++)
            {
                yield return null; // UGUI reconstruye el Canvas entre fotogramas (no es código de la capa)
                if (f == 20)
                {
                    Assert.That(emit, Is.Not.AllocatingGCMemory(), "emitir con el pool a medias");
                }
                else if (f == 21)
                {
                    Assert.That(flood, Is.Not.AllocatingGCMemory(), "descartar lo que excede el tope");
                    Assert.That(fx.ActiveCount, Is.EqualTo(FxLayer.MaxParticles));
                }

                Assert.That(tick, Is.Not.AllocatingGCMemory(), "Tick, fotograma " + f);
            }

            yield return RunTicks(tick, 60);
            Assert.That(fx.ActiveCount, Is.EqualTo(0), "todo se apaga solo");
        }

        // Canvas con el contenedor del tablero, una capa hermana (para reordenar) y la capa de efectos, como GameScreen.
        private IEnumerator StartRig()
        {
            _root = new GameObject("FxEmitterRig", typeof(RectTransform), typeof(Canvas));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform board = NewLayer("Board", (RectTransform)_root.transform);
            _other = NewLayer("Cuias", board);
            _fx = new FxLayer(board, Art);
            yield return null; // un fotograma para que el Canvas se construya
        }

        private static IEnumerator RunTicks(TestDelegate tick, int frames)
        {
            for (int f = 0; f < frames; f++)
            {
                tick();
                yield return null;
            }
        }

        private static IEnumerator RunUntilIdle(FxLayer fx, float maxMs, System.Action<FxLayer>? each)
        {
            float t = 0f;
            while (fx.ActiveCount > 0 && t < maxMs)
            {
                fx.Tick(FrameMs);
                t += FrameMs;
                each?.Invoke(fx);
                yield return null;
            }
        }

        private static void AssertWithinCaps(FxLayer fx, string where)
        {
            Assert.That(fx.ActiveCount, Is.LessThanOrEqualTo(FxLayer.MaxParticles), "≤ 44: " + where);
            Assert.That(fx.AdditiveCount, Is.LessThanOrEqualTo(FxLayer.MaxAdditive), "≤ 8 aditivos: " + where);
            Assert.That(Shown(fx, null).Count, Is.EqualTo(fx.ActiveCount), "imágenes a la vista = vivas: " + where);
        }

        // Partículas a la vista (enabled; las que esperan su retardo cuentan) con el sprite pedido, o todas si es null.
        private static List<Image> Shown(FxLayer fx, Sprite? sprite)
        {
            var result = new List<Image>();
            foreach (Image img in fx.Root.GetComponentsInChildren<Image>(true))
            {
                if (img.enabled && img.name.StartsWith("Particle", System.StringComparison.Ordinal) && (sprite == null || img.sprite == sprite))
                {
                    result.Add(img);
                }
            }

            return result;
        }

        private static RectTransform NewLayer(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }
    }
}
