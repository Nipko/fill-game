#nullable enable
using System;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// La ola de la Pororoca (bloque B2; 04 §2.4 VFX: «Una ola grande cruza la pantalla de izquierda a derecha (1,2 s)… No
    /// saltable (es el feedback de una compra)»): la ola de <see cref="ProceduralArt.ShuffleWaveSprite"/> entra por la izquierda
    /// y sale por la derecha en <see cref="DurationMs"/>, por encima del tablero, con un velo transparente que captura los toques
    /// mientras dura (no se salta ni se juega a medias). Quien la usa (<c>GameScreen.Shuffle.cs</c>) lee <see cref="FrontX"/> en
    /// cada fotograma para cambiar cada cuia cuando el frente la cruza. Con Reducir movimiento (07 §7.8) no hay ola que cruce:
    /// un fundido de la misma duración [P], y el frente salta al final a la mitad. Sin asignaciones por fotograma.
    /// </summary>
    internal sealed class ShuffleWaveFx
    {
        /// <summary>Duración de la ola (04 §2.4: 1,2 s).</summary>
        public const float DurationMs = 1200f;

        /// <summary>Alto de la ola respecto al alto de la zona que cruza.</summary>
        public const float HeightFraction = 1.1f;

        private readonly RectTransform _root;
        private readonly HitArea _blocker;
        private readonly Image _wave;
        private float _elapsedMs = -1f;
        private float _left;
        private float _right;
        private float _top;
        private float _height;
        private bool _reduce;

        public ShuffleWaveFx(Transform parent, ProceduralArt art)
        {
            if (art == null)
            {
                throw new ArgumentNullException(nameof(art));
            }

            _root = UiBuilder.Rect("ShuffleWave", parent);
            UiBuilder.Stretch(_root);
            // Área táctil invisible (HitArea, como las de las cuias): captura los toques mientras la ola cruza.
            RectTransform blocker = UiBuilder.Rect("Blocker", _root);
            UiBuilder.Stretch(blocker);
            _blocker = blocker.gameObject.AddComponent<HitArea>();
            _blocker.raycastTarget = true;
            _wave = UiBuilder.Image("Wave", _root, art.ShuffleWaveSprite, Color.white);
            _root.gameObject.SetActive(false);
        }

        /// <summary>La ola está cruzando.</summary>
        public bool Playing => _elapsedMs >= 0f;

        /// <summary>Ms desde que empezó (−1 si no hay ola).</summary>
        public float ElapsedMs => _elapsedMs;

        /// <summary>
        /// Posición x del frente de la ola (dp desde la izquierda del lienzo): de <c>left</c> a <c>right</c> más su ancho a lo
        /// largo de la ola (sale entera por la derecha); con Reducir movimiento, en la mitad de la duración salta al final. −∞ si
        /// no hay ola.
        /// </summary>
        public float FrontX
        {
            get
            {
                if (!Playing)
                {
                    return float.NegativeInfinity;
                }

                float t = Mathf.Clamp01(_elapsedMs / DurationMs);
                if (_reduce)
                {
                    return t < 0.5f ? _left : _right;
                }

                return Mathf.Lerp(_left, _right + WaveWidth, EaseInOut(t));
            }
        }

        /// <summary>Raíz (tests y orden de capas).</summary>
        public RectTransform Root => _root;

        /// <summary>
        /// Lanza la ola sobre la franja de <paramref name="top"/> a <paramref name="bottom"/> (dp desde arriba) y de
        /// <paramref name="left"/> a <paramref name="right"/> (dp); la ola entra por fuera de la izquierda y sale por fuera de la
        /// derecha. Otra ola en marcha empieza de nuevo.
        /// </summary>
        public void Play(float left, float right, float top, float bottom, bool reduceMotion)
        {
            _left = left;
            _right = right;
            _top = top;
            _height = Mathf.Max(1f, bottom - top);
            _reduce = reduceMotion;
            _elapsedMs = 0f;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Place();
        }

        /// <summary>Avanza <paramref name="dtMs"/>; devuelve true en el fotograma en que la ola termina.</summary>
        public bool Tick(float dtMs)
        {
            if (!Playing)
            {
                return false;
            }

            _elapsedMs += Mathf.Max(0f, dtMs);
            if (_elapsedMs >= DurationMs)
            {
                Stop();
                return true;
            }

            Place();
            return false;
        }

        /// <summary>Quita la ola en el acto (otro nivel, el modal…).</summary>
        public void Stop()
        {
            _elapsedMs = -1f;
            _root.gameObject.SetActive(false);
        }

        // Ancho de la ola (dp): 0,6 de su alto, como el sprite (96 × 160).
        private float WaveWidth => _height * HeightFraction * 0.6f;

        private static float EaseInOut(float t)
        {
            return t * t * (3f - (2f * t));
        }

        private void Place()
        {
            float h = _height * HeightFraction;
            float w = WaveWidth;
            float t = Mathf.Clamp01(_elapsedMs / DurationMs);
            if (_reduce)
            {
                // Fundido: la ola quieta en el centro aparece y se va.
                float alpha = Mathf.Sin(t * Mathf.PI);
                _wave.color = new Color(1f, 1f, 1f, alpha);
                UiBuilder.Place(_wave.rectTransform, ((_left + _right) * 0.5f) - (w * 0.5f), _top - ((h - _height) * 0.5f), w, h);
                return;
            }

            _wave.color = Color.white;
            float front = Mathf.Lerp(_left, _right + w, EaseInOut(t));
            UiBuilder.Place(_wave.rectTransform, front - w, _top - ((h - _height) * 0.5f), w, h);
        }
    }
}
