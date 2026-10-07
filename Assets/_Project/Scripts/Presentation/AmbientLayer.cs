#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Ambiente de la escena (guía visual v2, 6.12): polen y motas de luz (8, aditivas, que se alejan de la luz),
    /// destellos del río (6, titilan) y balanceo del follaje (±1,5°, 4-6 s, desfasado). Siempre fuera del rectángulo del
    /// panel; dos Canvas anidados propios (lejano y cercano) para no reconstruir el lienzo principal. Nada depende de la
    /// selección ni de la legalidad (D-002): solo de la fase (se suspende en la celebración, la victoria y el bloqueo).
    /// </summary>
    public sealed class AmbientLayer
    {
        /// <summary>Motas de polen (guía 6.12: 8).</summary>
        public const int PollenCount = 8;

        /// <summary>Destellos del río (guía 6.12: 6).</summary>
        public const int GlintCount = 6;

        private readonly RectTransform _far;
        private readonly RectTransform _near;
        private readonly Image[] _pollen = new Image[PollenCount];
        private readonly Vector2[] _pollenPos = new Vector2[PollenCount];
        private readonly Vector2[] _pollenVel = new Vector2[PollenCount];
        private readonly float[] _pollenAge = new float[PollenCount];
        private readonly float[] _pollenLife = new float[PollenCount];
        private readonly float[] _pollenAlpha = new float[PollenCount];
        private readonly Image[] _glints = new Image[GlintCount];
        private readonly float[] _glintAge = new float[GlintCount];
        private readonly float[] _glintLife = new float[GlintCount];
        private readonly RectTransform?[] _foliage = new RectTransform?[4];
        private readonly float[] _foliagePhase = { 0f, 1.7f, 3.1f, 4.4f };
        private readonly float[] _foliagePeriod = { 4.6f, 5.4f, 5.0f, 4.2f };
        private Rect _panel;
        private Rect _river;
        private Vector2 _size = new Vector2(360f, 780f);
        private uint _rng = 0x2545F491u;
        private float _time;
        private bool _suspended;

        /// <summary>Crea las dos capas: <paramref name="farParent"/> (sobre el panorama) y <paramref name="nearParent"/> (sobre el HUD).</summary>
        public AmbientLayer(RectTransform farParent, RectTransform nearParent, ProceduralArt art)
        {
            _far = NewCanvas("AmbientFar", farParent);
            _near = NewCanvas("AmbientNear", nearParent);
            Material? additive = UiMaterials.Additive;
            for (int i = 0; i < GlintCount; i++)
            {
                _glints[i] = NewImage(_far, "RiverGlint" + i, art.GlintStreak, additive);
                _glints[i].rectTransform.sizeDelta = new Vector2(6f, 2f);
                _glintAge[i] = -1f;
            }

            for (int i = 0; i < PollenCount; i++)
            {
                _pollen[i] = NewImage(_near, "Pollen" + i, art.Glow, additive);
                _pollenAge[i] = -1f;
            }
        }

        /// <summary>Congela el ambiente con semilla y tiempo fijos (capturas de prueba; guía 6, "Reducir movimiento").</summary>
        public static bool Frozen { get; set; }

        /// <summary>Raíz de la capa lejana.</summary>
        public RectTransform Far => _far;

        /// <summary>Raíz de la capa cercana.</summary>
        public RectTransform Near => _near;

        /// <summary>Partículas de ambiente vivas (≤ 16).</summary>
        public int ActiveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < PollenCount; i++)
                {
                    n += _pollen[i].gameObject.activeSelf ? 1 : 0;
                }

                for (int i = 0; i < GlintCount; i++)
                {
                    n += _glints[i].gameObject.activeSelf ? 1 : 0;
                }

                return n;
            }
        }

        /// <summary>Asocia el follaje que se balancea (pivote en su esquina).</summary>
        public void SetFoliage(RectTransform tl, RectTransform tr, RectTransform bl, RectTransform br)
        {
            _foliage[0] = tl;
            _foliage[1] = tr;
            _foliage[2] = bl;
            _foliage[3] = br;
        }

        /// <summary>
        /// Zonas en dp del lienzo (y desde arriba): el lienzo, el panel (exclusión) y la franja de río visible donde
        /// titilan los destellos.
        /// </summary>
        public void SetLayout(Vector2 canvasSize, Rect panel, Rect river)
        {
            _size = canvasSize;
            _panel = panel;
            _river = river;
            if (Frozen)
            {
                Seed();
            }
        }

        /// <summary>Suspende el ambiente (celebración, victoria, bloqueo: el estallido dispone de las 60 partículas).</summary>
        public void SetSuspended(bool suspended)
        {
            if (_suspended == suspended)
            {
                return;
            }

            _suspended = suspended;
            if (suspended)
            {
                for (int i = 0; i < PollenCount; i++)
                {
                    _pollenAge[i] = -1f;
                    SetActive(_pollen[i], false);
                }

                for (int i = 0; i < GlintCount; i++)
                {
                    _glintAge[i] = -1f;
                    SetActive(_glints[i], false);
                }
            }
        }

        /// <summary>Avanza el ambiente.</summary>
        public void Tick(float dtMs)
        {
            if (Frozen)
            {
                return;
            }

            _time += dtMs / 1000f;
            for (int i = 0; i < 4; i++)
            {
                RectTransform? f = _foliage[i];
                if (f != null)
                {
                    float a = 1.5f * Mathf.Sin(((_time / _foliagePeriod[i]) * Mathf.PI * 2f) + _foliagePhase[i]);
                    f.localRotation = Quaternion.Euler(0f, 0f, a);
                }
            }

            if (_suspended)
            {
                return;
            }

            float dt = dtMs / 1000f;
            for (int i = 0; i < PollenCount; i++)
            {
                if (_pollenAge[i] < 0f)
                {
                    if (Next() < dt * 0.6f)
                    {
                        SpawnPollen(i, false);
                    }

                    continue;
                }

                _pollenAge[i] += dt;
                if (_pollenAge[i] >= _pollenLife[i])
                {
                    _pollenAge[i] = -1f;
                    SetActive(_pollen[i], false);
                    continue;
                }

                _pollenPos[i] += _pollenVel[i] * dt;
                if (InsidePanel(_pollenPos[i], 6f))
                {
                    _pollenAge[i] = -1f; // nunca dentro del panel
                    SetActive(_pollen[i], false);
                    continue;
                }

                ApplyPollen(i);
            }

            for (int i = 0; i < GlintCount; i++)
            {
                if (_glintAge[i] < 0f)
                {
                    if (_river.height > 4f && Next() < dt * 1.2f)
                    {
                        SpawnGlint(i, 0f);
                    }

                    continue;
                }

                _glintAge[i] += dt;
                if (_glintAge[i] >= _glintLife[i])
                {
                    _glintAge[i] = -1f;
                    SetActive(_glints[i], false);
                    continue;
                }

                ApplyGlint(i);
            }
        }

        private void Seed()
        {
            _rng = 0x2545F491u;
            for (int i = 0; i < PollenCount; i++)
            {
                SpawnPollen(i, true);
            }

            for (int i = 0; i < GlintCount; i++)
            {
                if (_river.height > 4f && !_suspended)
                {
                    SpawnGlint(i, 0.5f);
                }
            }
        }

        private void SpawnPollen(int i, bool midLife)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var p = new Vector2(Next() * _size.x, Next() * _size.y);
                if (InsidePanel(p, 12f))
                {
                    continue;
                }

                _pollenPos[i] = p;
                // Se alejan de la luz (arriba a la izquierda): derivan a la derecha y hacia abajo a 6-12 dp/s.
                float speed = Mathf.Lerp(6f, 12f, Next());
                var dir = new Vector2(0.75f + (Next() * 0.3f), 0.45f + (Next() * 0.3f)).normalized;
                _pollenVel[i] = dir * speed;
                _pollenLife[i] = Mathf.Lerp(6f, 10f, Next());
                _pollenAge[i] = midLife ? _pollenLife[i] * 0.5f : 0f;
                _pollenAlpha[i] = Mathf.Lerp(0.3f, 0.6f, Next());
                float s = Mathf.Lerp(2f, 4f, Next()) * 2.5f; // el halo es mayor que la mota (2-4 dp de núcleo)
                _pollen[i].rectTransform.sizeDelta = new Vector2(s, s);
                SetActive(_pollen[i], true);
                ApplyPollen(i);
                return;
            }
        }

        private void ApplyPollen(int i)
        {
            float t = _pollenAge[i] / _pollenLife[i];
            float fade = Mathf.Clamp01(t / 0.15f) * Mathf.Clamp01((1f - t) / 0.2f);
            Color c = Palette.Glint;
            c.a = _pollenAlpha[i] * fade;
            _pollen[i].color = c;
            _pollen[i].rectTransform.anchoredPosition = new Vector2(_pollenPos[i].x, -_pollenPos[i].y);
        }

        private void SpawnGlint(int i, float startT)
        {
            var p = new Vector2(Mathf.Lerp(_river.xMin, _river.xMax, Next()), Mathf.Lerp(_river.yMin, _river.yMax, Next()));
            if (InsidePanel(p, 8f))
            {
                return;
            }

            _glintLife[i] = Mathf.Lerp(0.8f, 1.4f, Next());
            _glintAge[i] = _glintLife[i] * startT;
            _glints[i].rectTransform.anchoredPosition = new Vector2(p.x, -p.y);
            SetActive(_glints[i], true);
            ApplyGlint(i);
        }

        private void ApplyGlint(int i)
        {
            float t = _glintAge[i] / _glintLife[i];
            Color c = Palette.Glint;
            c.a = 0.7f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
            _glints[i].color = c;
        }

        private bool InsidePanel(Vector2 p, float margin)
        {
            return p.x > _panel.xMin - margin && p.x < _panel.xMax + margin && p.y > _panel.yMin - margin && p.y < _panel.yMax + margin;
        }

        private static RectTransform NewCanvas(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Image NewImage(RectTransform parent, string name, Sprite sprite, Material? material)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (material != null)
            {
                img.material = material;
            }

            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            go.SetActive(false);
            return img;
        }

        private static void SetActive(Image img, bool on)
        {
            if (img.gameObject.activeSelf != on)
            {
                img.gameObject.SetActive(on);
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
