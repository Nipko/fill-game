#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Estallido de la victoria (02 §4.1; 07 §5.4; guía visual v2, 6.9): como máximo <c>anim_confetti_max_particles</c>
    /// partículas (24 plumas roja y azul de guacamayo, 24 hojas y 12 pétalos de heliconia) durante 1,2 s, con gravedad,
    /// giro de 180-540 °/s y aleteo de ±15°. Imágenes reutilizadas sin asignaciones por fotograma, en un Canvas anidado
    /// propio.
    /// </summary>
    /// <remarks>
    /// <para>Revisión visual del 2026-10-07 (<c>06_celebration_l1</c>): salía en dos racimos pegados a las esquinas del
    /// panel y tapaba las cuias. Ahora nace fuera de la pantalla (x = −8 y W + 8 dp, y = panel.top + 40 dp) a 600-850 dp/s,
    /// con 60-75° hacia arriba y hacia el centro (±8°), gravedad de 1100 dp/s² y arrastre de 1,5/s: el pico queda por
    /// encima de panel.top − 40 y las partículas cruzan el ancho antes de caer.</para>
    /// <para>Tras el estallido, como mucho 12 hojas y pétalos caen despacio hasta el CTA de la victoria (reutilizan las
    /// imágenes del estallido: nunca hay más de 60 a la vez).</para>
    /// </remarks>
    public sealed class ConfettiLayer
    {
        /// <summary>Duración del estallido (07 §5.4): 1,2 s.</summary>
        public const float DurationMs = 1200f;

        /// <summary>Hojas y pétalos que caen despacio tras el estallido.</summary>
        public const int DriftCount = 12;

        /// <summary>Gravedad del estallido (dp/s²).</summary>
        public const float Gravity = 1100f;

        /// <summary>Arrastre del aire del estallido (1/s).</summary>
        public const float Drag = 1.5f;

        private const float DriftMaxMs = 6000f;

        private readonly RectTransform _layer;
        private readonly Image[] _items;
        private readonly Vector2[] _pos;
        private readonly Vector2[] _vel;
        private readonly float[] _spin;
        private readonly float[] _phase;
        private readonly float[] _delay;
        private readonly Color[] _baseColor;
        private readonly int[] _driftItems;
        private float _elapsed = -1f;
        private float _drift = -1f;
        private int _active;
        private int _driftActive;
        private float _driftEndY;
        private float _driftWidth;
        private float _driftTopY;
        private uint _rng = 0x51ED270Bu;

        /// <summary>Crea el grupo de <paramref name="maxParticles"/> partículas dentro de <paramref name="layer"/>.</summary>
        public ConfettiLayer(RectTransform layer, ProceduralArt art, int maxParticles)
        {
            _layer = layer;
            if (layer.GetComponent<Canvas>() == null)
            {
                layer.gameObject.AddComponent<Canvas>();
            }

            int n = Mathf.Max(0, maxParticles);
            _items = new Image[n];
            _pos = new Vector2[n];
            _vel = new Vector2[n];
            _spin = new float[n];
            _phase = new float[n];
            _delay = new float[n];
            _baseColor = new Color[n];
            int drift = 0;
            var driftItems = new int[DriftCount];
            for (int i = 0; i < n; i++)
            {
                // Reparto de 60: 24 plumas (alternas roja y azul), 24 hojas y 12 pétalos (guía 6.9).
                int slot = i % 5;
                Sprite sprite;
                Color color;
                Vector2 size;
                if (slot < 2)
                {
                    sprite = art.Feather;
                    color = ((i / 2) % 2 == 0) ? Palette.FeatherRed : Palette.FeatherBlue; // las dos plumas en cada lado
                    size = new Vector2(8f, 22f);
                }
                else if (slot < 4)
                {
                    sprite = art.LeafBit;
                    color = (i % 3 == 0) ? Color.Lerp(Color.white, Palette.LeafShine, 0.3f) : Color.white;
                    size = new Vector2(10f, 17f);
                }
                else
                {
                    sprite = art.Petal;
                    color = Color.white;
                    size = new Vector2(8f, 13f);
                }

                if (slot >= 2 && drift < DriftCount)
                {
                    driftItems[drift++] = i; // hojas y pétalos para la caída lenta
                }

                var go = new GameObject("Burst" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(layer, false);
                Image img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.raycastTarget = false;
                img.color = color;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = size;
                go.SetActive(false);
                _items[i] = img;
                _baseColor[i] = color;
            }

            _driftItems = new int[drift];
            System.Array.Copy(driftItems, _driftItems, drift);
        }

        /// <summary>Hay estallido o caída lenta en pantalla.</summary>
        public bool Running => _elapsed >= 0f || _drift >= 0f;

        /// <summary>Partículas vivas (≤ 60, VA-13).</summary>
        public int ActiveCount
        {
            get
            {
                if (_elapsed >= 0f)
                {
                    return _active;
                }

                return _drift >= 0f ? _driftActive : 0;
            }
        }

        /// <summary>Hay caída lenta de hojas y pétalos tras el estallido.</summary>
        public bool Drifting => _drift >= 0f;

        /// <summary>
        /// Altura (dp desde arriba del lienzo) de la partícula más alta del estallido en curso, para los tests;
        /// <see cref="float.PositiveInfinity"/> si no hay estallido.
        /// </summary>
        public float HighestY
        {
            get
            {
                float best = float.PositiveInfinity;
                if (_elapsed < 0f)
                {
                    return best;
                }

                for (int i = 0; i < _active; i++)
                {
                    best = Mathf.Min(best, -_pos[i].y);
                }

                return best;
            }
        }

        /// <summary>
        /// Lanza <paramref name="count"/> partículas (sin superar el máximo) desde fuera de los dos bordes de un lienzo de
        /// <paramref name="canvasWidth"/> dp, a la altura de <paramref name="panel"/>.top + 40 dp (dp del lienzo, y desde
        /// arriba). Si <paramref name="driftEndY"/> &gt; 0, al terminar caen despacio hasta esa altura (el CTA) como mucho
        /// <see cref="DriftCount"/> hojas y pétalos.
        /// </summary>
        public void Play(int count, Rect panel, float canvasWidth, float driftEndY = -1f)
        {
            _active = Mathf.Min(count, _items.Length);
            _drift = -1f;
            _driftEndY = driftEndY;
            _driftWidth = canvasWidth;
            _driftTopY = panel.yMin;
            for (int i = 0; i < _items.Length; i++)
            {
                bool on = i < _active;
                _items[i].gameObject.SetActive(on);
                if (!on)
                {
                    continue;
                }

                bool left = (i % 2) == 0;
                float y = panel.yMin + 40f;
                float x = left ? -8f : canvasWidth + 8f;
                float a = Mathf.Lerp(60f, 75f, Next()) + ((Next() - 0.5f) * 16f); // ±8°
                float ang = (left ? a : 180f - a) * Mathf.Deg2Rad;
                float speed = Mathf.Lerp(600f, 850f, Next());
                _pos[i] = new Vector2(x, -y);
                _vel[i] = new Vector2(Mathf.Cos(ang) * speed, Mathf.Sin(ang) * speed);
                float spin = Mathf.Lerp(180f, 540f, Next());
                _spin[i] = Next() < 0.5f ? -spin : spin;
                _phase[i] = Next() * Mathf.PI * 2f;
                _items[i].color = _baseColor[i];
                RectTransform rt = _items[i].rectTransform;
                rt.anchoredPosition = _pos[i];
            }

            _elapsed = 0f;
        }

        /// <summary>Retira el estallido y la caída lenta.</summary>
        public void Stop()
        {
            _elapsed = -1f;
            _drift = -1f;
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
            if (_elapsed >= 0f)
            {
                TickBurst(dtMs);
            }
            else if (_drift >= 0f)
            {
                TickDrift(dtMs);
            }
        }

        private void TickBurst(float dtMs)
        {
            _elapsed += dtMs;
            if (_elapsed >= DurationMs)
            {
                _elapsed = -1f;
                for (int i = 0; i < _items.Length; i++)
                {
                    if (_items[i].gameObject.activeSelf)
                    {
                        _items[i].gameObject.SetActive(false);
                    }
                }

                if (_driftEndY > 0f)
                {
                    StartDrift();
                }

                return;
            }

            float dt = dtMs / 1000f;
            float fade = Mathf.Clamp01((DurationMs - _elapsed) / 300f);
            float drag = Mathf.Max(0f, 1f - (Drag * dt));
            for (int i = 0; i < _active; i++)
            {
                _vel[i].y -= Gravity * dt;
                _vel[i] *= drag; // las plumas planean
                float flutter = Mathf.Sin((_elapsed / 140f) + _phase[i]);
                _pos[i] += new Vector2(_vel[i].x + (flutter * 40f), _vel[i].y) * dt;
                RectTransform rt = _items[i].rectTransform;
                rt.anchoredPosition = _pos[i];
                rt.localRotation = Quaternion.Euler(0f, 0f, (_phase[i] * Mathf.Rad2Deg) + (_spin[i] * _elapsed / 1000f) + (flutter * 15f));
                Color c = _baseColor[i];
                c.a = fade;
                _items[i].color = c;
            }
        }

        // Caída lenta: hojas y pétalos desde encima del tablero hasta el CTA, escalonados, a 80-120 dp/s con vaivén.
        private void StartDrift()
        {
            _driftActive = _driftItems.Length;
            for (int d = 0; d < _driftItems.Length; d++)
            {
                int i = _driftItems[d];
                float x = Mathf.Lerp(28f, _driftWidth - 28f, (d + Next()) / _driftItems.Length);
                float y = _driftTopY - 20f - (Next() * 50f);
                _pos[i] = new Vector2(x, -y);
                _vel[i] = new Vector2(0f, -Mathf.Lerp(80f, 120f, Next()));
                _spin[i] = (Next() < 0.5f ? -1f : 1f) * Mathf.Lerp(40f, 90f, Next());
                _phase[i] = Next() * Mathf.PI * 2f;
                _delay[i] = Next() * 1600f;
                Color c = _baseColor[i];
                c.a = 0f;
                _items[i].color = c;
                _items[i].rectTransform.anchoredPosition = _pos[i];
                _items[i].gameObject.SetActive(true);
            }

            _drift = 0f;
        }

        private void TickDrift(float dtMs)
        {
            _drift += dtMs;
            float dt = dtMs / 1000f;
            int alive = 0;
            for (int d = 0; d < _driftItems.Length; d++)
            {
                int i = _driftItems[d];
                if (!_items[i].gameObject.activeSelf)
                {
                    continue;
                }

                float local = _drift - _delay[i];
                if (local < 0f)
                {
                    alive++;
                    continue;
                }

                _pos[i] += _vel[i] * dt;
                float sway = Mathf.Sin((local / 900f) + _phase[i]);
                float y = -_pos[i].y;
                float fadeIn = Mathf.Clamp01(local / 250f);
                float fadeOut = Mathf.Clamp01((_driftEndY - y) / 40f);
                if (fadeOut <= 0f || _drift > DriftMaxMs)
                {
                    _items[i].gameObject.SetActive(false);
                    continue;
                }

                alive++;
                RectTransform rt = _items[i].rectTransform;
                rt.anchoredPosition = new Vector2(_pos[i].x + (sway * 18f), _pos[i].y);
                rt.localRotation = Quaternion.Euler(0f, 0f, (_phase[i] * Mathf.Rad2Deg) + (_spin[i] * local / 1000f) + (sway * 25f));
                Color c = _baseColor[i];
                c.a = fadeIn * fadeOut;
                _items[i].color = c;
            }

            _driftActive = alive;
            if (alive == 0)
            {
                _drift = -1f;
            }
        }

        private float Next()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            return (_rng & 0x00FFFFFF) / 16777216f;
        }
    }
}
