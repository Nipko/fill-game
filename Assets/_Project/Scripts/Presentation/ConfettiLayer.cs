#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Confeti de plumas rojas y azules de guacamayo (02 §4.1; 07 §5.4): 1,2 s, como máximo
    /// <c>anim_confetti_max_particles</c> partículas, imágenes reutilizadas sin asignaciones por fotograma.
    /// </summary>
    public sealed class ConfettiLayer
    {
        /// <summary>Duración del confeti (07 §5.4): 1,2 s.</summary>
        public const float DurationMs = 1200f;

        private readonly RectTransform _layer;
        private readonly Image[] _items;
        private readonly Vector2[] _pos;
        private readonly Vector2[] _vel;
        private readonly float[] _spin;
        private readonly float[] _phase;
        private float _elapsed = -1f;
        private int _active;

        /// <summary>Crea el grupo de <paramref name="maxParticles"/> plumas dentro de <paramref name="layer"/>.</summary>
        public ConfettiLayer(RectTransform layer, Sprite feather, int maxParticles)
        {
            _layer = layer;
            int n = Mathf.Max(0, maxParticles);
            _items = new Image[n];
            _pos = new Vector2[n];
            _vel = new Vector2[n];
            _spin = new float[n];
            _phase = new float[n];
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Feather" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(layer, false);
                Image img = go.GetComponent<Image>();
                img.sprite = feather;
                img.raycastTarget = false;
                img.color = i % 2 == 0 ? Palette.FeatherRed : Palette.FeatherBlue;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(6f, 18f);
                go.SetActive(false);
                _items[i] = img;
            }
        }

        /// <summary>Hay confeti en pantalla.</summary>
        public bool Running => _elapsed >= 0f;

        /// <summary>Lanza <paramref name="count"/> plumas desde arriba (sin superar el máximo).</summary>
        public void Play(int count)
        {
            _active = Mathf.Min(count, _items.Length);
            float w = _layer.rect.width;
            for (int i = 0; i < _items.Length; i++)
            {
                bool on = i < _active;
                _items[i].gameObject.SetActive(on);
                if (!on)
                {
                    continue;
                }

                _pos[i] = new Vector2(Random.Range(0f, w), Random.Range(-60f, 30f));
                _vel[i] = new Vector2(Random.Range(-50f, 50f), Random.Range(-560f, -300f));
                _spin[i] = Random.Range(-360f, 360f);
                _phase[i] = Random.Range(0f, Mathf.PI * 2f);
                Color c = _items[i].color;
                c.a = 1f;
                _items[i].color = c;
            }

            _elapsed = 0f;
        }

        /// <summary>Retira el confeti.</summary>
        public void Stop()
        {
            _elapsed = -1f;
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i].gameObject.activeSelf)
                {
                    _items[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>Avanza la animación.</summary>
        public void Tick(float dtMs)
        {
            if (_elapsed < 0f)
            {
                return;
            }

            _elapsed += dtMs;
            if (_elapsed >= DurationMs)
            {
                Stop();
                return;
            }

            float dt = dtMs / 1000f;
            float fade = Mathf.Clamp01((DurationMs - _elapsed) / 300f);
            for (int i = 0; i < _active; i++)
            {
                _vel[i].y -= 220f * dt * 0.3f;
                _pos[i] += new Vector2(_vel[i].x + (Mathf.Sin((_elapsed / 160f) + _phase[i]) * 30f), _vel[i].y) * dt;
                RectTransform rt = _items[i].rectTransform;
                rt.anchoredPosition = _pos[i];
                rt.localRotation = Quaternion.Euler(0f, 0f, _phase[i] * Mathf.Rad2Deg + (_spin[i] * _elapsed / 1000f));
                Color c = _items[i].color;
                c.a = fade;
                _items[i].color = c;
            }
        }
    }
}
