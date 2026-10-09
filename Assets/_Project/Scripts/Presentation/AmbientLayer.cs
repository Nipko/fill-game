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
    /// En «Llenar y teñir» (guion de la intro, §6.2 y §11) el río crece: el río está horneado en el panorama, así que no se
    /// puede subir; una capa de crecida clara (degradado con suelo de 0,35 y alfa máximo de 0,65) aclara la franja de río
    /// visible unos +12 L* y los destellos duplican su alfa y se deslizan 10 dp hacia el panel (<see cref="SetRiverBoost"/>).
    /// En el modo dosel el follaje de arriba se sacude ±6° (<see cref="SetCanopyShake"/>). Mientras dura la intro el polen se
    /// pausa (<see cref="SetPollenPaused"/>: en los primeros 100 ms solo se mueve el río). Sin ninguna cresta que avance por el
    /// río (sería la Pororoca).
    /// </summary>
    public sealed class AmbientLayer
    {
        /// <summary>Motas de polen (guía 6.12: 8).</summary>
        public const int PollenCount = 8;

        /// <summary>Destellos del río (guía 6.12: 6).</summary>
        public const int GlintCount = 6;

        /// <summary>
        /// Opacidad máxima de la capa de crecida: con el suelo del degradado en 0,35 (a = 0,35 + 0,65 · smoothstep) y el color
        /// claro del destello, la franja de río sube unos +12 L* a los 150 ms (guion §6.2; con 0,35 sobre un degradado de 0 a 1
        /// en el beige del río solo subía +2 L*).
        /// </summary>
        public const float RiverSwellAlpha = 0.65f;

        /// <summary>Suelo del degradado de la capa de crecida (arriba, junto al horizonte).</summary>
        public const float RiverSwellFloor = 0.35f;

        /// <summary>
        /// Alto de la capa de crecida (del horizonte al panel, dp) para el que vale <see cref="RiverSwellAlpha"/> (el nivel 7: 44
        /// dp). En franjas más altas (nivel 1: 64 dp) el alfa baja con √(44 / alto), para que la franja suba de media unos +12 L*
        /// y no +15 (medido en las capturas 01_intro_l7 y 01c_intro_l1 a 0 y 150 ms).
        /// </summary>
        public const float RiverSwellRefDp = 44f;

        /// <summary>Los destellos se deslizan hacia el panel con la crecida (dp).</summary>
        public const float RiverGlintSlideDp = 10f;

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
        private readonly Vector2[] _glintPos = new Vector2[GlintCount];
        private readonly Image _swell;
        private readonly RectTransform?[] _foliage = new RectTransform?[4];
        private readonly float[] _foliagePhase = { 0f, 1.7f, 3.1f, 4.4f };
        private readonly float[] _foliagePeriod = { 4.6f, 5.4f, 5.0f, 4.2f };
        private Rect _panel;
        private Rect _river;
        private Vector2 _size = new Vector2(360f, 780f);
        private uint _rng = 0x2545F491u;
        private float _time;
        private bool _suspended;
        private bool _pollenPaused;
        private float _boost;
        private float _shake;

        /// <summary>Crea las dos capas: <paramref name="farParent"/> (sobre el panorama) y <paramref name="nearParent"/> (sobre el HUD).</summary>
        public AmbientLayer(RectTransform farParent, RectTransform nearParent, ProceduralArt art)
        {
            _far = NewCanvas("AmbientFar", farParent);
            _near = NewCanvas("AmbientNear", nearParent);
            Material? additive = UiMaterials.Additive;
            _swell = NewImage(_far, "RiverSwell", art.RiverSwell, null);
            _swell.rectTransform.pivot = new Vector2(0f, 1f);
            _swell.enabled = false;
            _swell.gameObject.SetActive(true);
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

        /// <summary>Crecida del río de la intro (0..1).</summary>
        public float RiverBoost => _boost;

        /// <summary>Capa de crecida sobre la franja de río (tests).</summary>
        public Image RiverSwell => _swell;

        /// <summary>
        /// Crecida del río en «Llenar y teñir» (0..1): la capa sobre la franja de río visible pasa de alfa 0 a 0,65 (corregido
        /// por el alto de la franja, <see cref="RiverSwellRefDp"/>) en un color claro y cálido, y los 6 destellos duplican su alfa
        /// y se deslizan 10 dp hacia el panel. Se aplica en el acto, también con el ambiente congelado (capturas).
        /// </summary>
        public void SetRiverBoost(float boost)
        {
            boost = Mathf.Clamp01(boost);
            if (Mathf.Abs(boost - _boost) < 0.0005f)
            {
                return;
            }

            _boost = boost;
            bool on = boost > 0.001f && _river.height > 4f;
            if (_swell.enabled != on)
            {
                _swell.enabled = on; // el componente, no el GameObject: sin asignaciones en mitad de la intro
            }

            if (on)
            {
                Color c = Palette.Glint; // claro y cálido: el beige del río apenas lo aclaraba
                c.a = RiverSwellAlpha * SwellBandScale() * boost;
                _swell.color = c;
            }

            for (int i = 0; i < GlintCount; i++)
            {
                if (_glintAge[i] >= 0f)
                {
                    ApplyGlint(i);
                }
            }
        }

        // √(44 / alto de la capa), como mucho 1: las franjas altas no se aclaran más que la del nivel 7.
        private float SwellBandScale()
        {
            return Mathf.Sqrt(Mathf.Min(1f, RiverSwellRefDp / Mathf.Max(1f, _river.height + 8f)));
        }

        /// <summary>
        /// Sacudida del follaje de las dos esquinas de arriba (grados, ±6° en el modo dosel de la intro). Se suma al balanceo y se
        /// aplica en el acto.
        /// </summary>
        public void SetCanopyShake(float degrees)
        {
            if (Mathf.Abs(degrees - _shake) < 0.001f)
            {
                return;
            }

            _shake = degrees;
            ApplyFoliage();
        }

        private static bool s_frozen;

        /// <summary>
        /// Congela el ambiente con semilla y tiempo fijos (capturas de prueba; guía 6, "Reducir movimiento"), y el reloj de los
        /// shaders del líquido y del chorro (<see cref="ShaderClock"/>: la intro lo lleva a su propio tiempo).
        /// </summary>
        public static bool Frozen
        {
            get => s_frozen;
            set
            {
                s_frozen = value;
                if (value)
                {
                    ShaderClock.Freeze(0f);
                }
                else
                {
                    ShaderClock.Release();
                }
            }
        }

        /// <summary>El polen está en pausa (la intro).</summary>
        public bool PollenPaused => _pollenPaused;

        /// <summary>Posición de la mota de polen <paramref name="i"/> (dp, y desde arriba; tests).</summary>
        public Vector2 PollenPosition(int i)
        {
            return _pollenPos[i];
        }

        /// <summary>
        /// Pausa el polen (las motas se quedan quietas y no nacen nuevas): mientras dura la intro, en los primeros 100 ms solo
        /// se mueven el río y sus destellos (guion §7.2, A3). Vuelve con el input.
        /// </summary>
        public void SetPollenPaused(bool paused)
        {
            _pollenPaused = paused;
        }

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
            _swell.rectTransform.anchoredPosition = new Vector2(0f, -river.yMin + 4f);
            _swell.rectTransform.sizeDelta = new Vector2(canvasSize.x, Mathf.Max(0f, river.height + 8f));
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
            ApplyFoliage();

            if (_suspended)
            {
                return;
            }

            float dt = dtMs / 1000f;
            for (int i = 0; i < PollenCount && !_pollenPaused; i++)
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

        // Balanceo de ±1,5° (guía 6.12) más la sacudida del dosel en las dos esquinas de arriba (en espejo).
        private void ApplyFoliage()
        {
            for (int i = 0; i < 4; i++)
            {
                RectTransform? f = _foliage[i];
                if (f != null)
                {
                    float a = Frozen ? 0f : 1.5f * Mathf.Sin(((_time / _foliagePeriod[i]) * Mathf.PI * 2f) + _foliagePhase[i]);
                    if (i == 0)
                    {
                        a += _shake;
                    }
                    else if (i == 1)
                    {
                        a -= _shake;
                    }

                    f.localRotation = Quaternion.Euler(0f, 0f, a);
                }
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
            _glintPos[i] = p;
            SetActive(_glints[i], true);
            ApplyGlint(i);
        }

        private void ApplyGlint(int i)
        {
            float t = _glintAge[i] / _glintLife[i];
            Color c = Palette.Glint;
            c.a = Mathf.Min(1f, 0.7f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) * (1f + _boost));
            _glints[i].color = c;
            Vector2 p = _glintPos[i];
            float slide = RiverGlintSlideDp * _boost; // hacia el panel, sin salir de la franja
            float y = Mathf.Min(p.y + slide, Mathf.Max(p.y, _panel.yMin - 8f));
            _glints[i].rectTransform.anchoredPosition = new Vector2(p.x, -y);
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
