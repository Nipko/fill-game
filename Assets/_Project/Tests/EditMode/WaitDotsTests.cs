#nullable enable
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Los puntos de espera de red (<see cref="WaitDots"/>; «Eliminando tu cuenta y tus datos…», 08 §11): se encienden uno tras
    /// otro con el tiempo y, con «Reducir movimiento» (02 §12, fila «Movimiento»), se quedan quietos y encendidos.
    /// </summary>
    public sealed class WaitDotsTests
    {
        private GameObject? _host;
        private Texture2D? _texture;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            if (_texture != null)
            {
                Object.DestroyImmediate(_texture);
            }
        }

        private WaitDots NewDots()
        {
            _host = new GameObject("WaitDotsHost", typeof(RectTransform));
            _texture = new Texture2D(4, 4);
            Sprite disc = Sprite.Create(_texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            return new WaitDots("Dots", _host.transform, disc, Color.white);
        }

        [Test]
        public void TheDots_TakeTurns_AndLoop()
        {
            WaitDots dots = NewDots();
            Assert.That(dots.Visible, Is.False, "nace escondida");
            dots.Show(0f, 0f, false);
            Assert.That(dots.Visible && dots.Animating, Is.True);
            Assert.That(WaitDots.WidthDp, Is.EqualTo((3 * WaitDots.DotDp) + (2 * WaitDots.GapDp)));

            int brightest = -1;
            var seen = new bool[WaitDots.Count];
            for (int step = 0; step < 12; step++)
            {
                dots.Tick(WaitDots.PeriodMs / 12f);
                float best = -1f;
                for (int i = 0; i < WaitDots.Count; i++)
                {
                    float a = dots.AlphaOf(i);
                    Assert.That(a, Is.InRange(WaitDots.DimAlpha - 0.001f, 1.001f));
                    if (a > best)
                    {
                        best = a;
                        brightest = i;
                    }
                }

                seen[brightest] = true;
            }

            Assert.That(seen, Is.All.True, "cada punto tiene su turno en una vuelta");
            float[] start = Snapshot(dots);
            dots.Tick(WaitDots.PeriodMs); // una vuelta entera: igual que antes
            float[] again = Snapshot(dots);
            for (int i = 0; i < WaitDots.Count; i++)
            {
                Assert.That(again[i], Is.EqualTo(start[i]).Within(0.01f));
            }
        }

        [Test]
        public void WithReduceMotion_TheyStayStillAndLit()
        {
            WaitDots dots = NewDots();
            dots.Show(0f, 0f, true);
            Assert.That(dots.Visible, Is.True, "la espera se sigue viendo");
            Assert.That(dots.Animating, Is.False);
            dots.Tick(300f);
            dots.Tick(450f);
            Assert.That(Snapshot(dots), Is.All.EqualTo(1f), "quietos y encendidos");
            dots.Hide();
            Assert.That(dots.Visible || dots.Animating, Is.False);
        }

        private static float[] Snapshot(WaitDots dots)
        {
            var a = new float[WaitDots.Count];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = dots.AlphaOf(i);
            }

            return a;
        }
    }
}
