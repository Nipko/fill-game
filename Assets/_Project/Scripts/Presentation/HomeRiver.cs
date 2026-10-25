#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// El río vivo del inicio (D-050, punto 2; 06 §4.7, fila Lobby): encima de la banda de agua del mundo
    /// (<see cref="StopBackdrop"/>, con el <c>water_base</c> de la parada: blanca, clara o negra) y delante del panorama, en un
    /// Canvas anidado propio (como el fondo de la parada: lo que se mueve aquí no rehace el resto del inicio), sin toques. De atrás
    /// adelante:
    /// <list type="number">
    /// <item>el reflejo del cielo junto al horizonte y la profundidad hacia el muelle (dos degradados teñidos), y la sombra del
    /// muelle sobre el agua;</item>
    /// <item>la columna de destellos del sol del panorama, que titilan;</item>
    /// <item>ondas claras que derivan despacio, más pequeñas cuanto más lejos (perspectiva);</item>
    /// <item>nenúfares (la hoja de la garza del tablero) que se mecen;</item>
    /// <item>un pez genérico que salta cada <see cref="FishPeriodMs"/> ms con su salpicadura, y un ave lejana que cruza el cielo
    /// sobre el horizonte cada <see cref="BirdPeriodMs"/> ms.</item>
    /// </list>
    /// Con Reducir movimiento (07 §7.8) todo queda quieto: ondas fijas, sin pez ni ave. <see cref="Tick"/> no asigna memoria: solo
    /// mueve rectángulos guardados y cambia la opacidad por el <see cref="CanvasRenderer"/> (sin rehacer mallas).
    /// </summary>
    public sealed class HomeRiver
    {
        /// <summary>Ondas del río.</summary>
        public const int RippleCount = 14;

        /// <summary>Destellos de la columna del sol.</summary>
        public const int GlintCount = 7;

        /// <summary>Nenúfares.</summary>
        public const int LilyCount = 4;

        /// <summary>Cada cuánto salta el pez (ms).</summary>
        public const float FishPeriodMs = 7000f;

        /// <summary>Lo que dura el salto del pez, fuera del agua (ms).</summary>
        public const float FishJumpMs = 900f;

        /// <summary>Lo que dura la salpicadura tras el salto (ms).</summary>
        public const float SplashMs = 800f;

        /// <summary>Cada cuánto cruza el ave (ms).</summary>
        public const float BirdPeriodMs = 24000f;

        /// <summary>Lo que tarda el ave en cruzar la pantalla (ms).</summary>
        public const float BirdCrossMs = 15000f;

        // Sitio de cada onda en el río: u (0..1 del ancho) y v (0 junto al horizonte, 1 junto al muelle), con su fase.
        private static readonly Vector3[] RippleSpots =
        {
            new Vector3(0.14f, 0.12f, 0.1f), new Vector3(0.42f, 0.08f, 1.7f), new Vector3(0.80f, 0.15f, 3.1f),
            new Vector3(0.28f, 0.27f, 0.6f), new Vector3(0.63f, 0.30f, 2.2f), new Vector3(0.90f, 0.40f, 4.0f),
            new Vector3(0.08f, 0.46f, 5.1f), new Vector3(0.46f, 0.50f, 2.8f), new Vector3(0.74f, 0.58f, 0.3f),
            new Vector3(0.22f, 0.68f, 3.7f), new Vector3(0.56f, 0.74f, 1.2f), new Vector3(0.94f, 0.80f, 4.6f),
            new Vector3(0.36f, 0.90f, 2.5f), new Vector3(0.70f, 0.94f, 5.6f),
        };

        // Nenúfares: u, v y ancho (dp a 360 de ancho).
        private static readonly Vector3[] LilySpots =
        {
            new Vector3(0.07f, 0.34f, 44f), new Vector3(0.19f, 0.44f, 30f), new Vector3(0.72f, 0.30f, 36f), new Vector3(0.55f, 0.13f, 24f),
        };

        // Sitios del salto del pez: u y v.
        private static readonly Vector2[] FishSpots =
        {
            new Vector2(0.50f, 0.36f), new Vector2(0.22f, 0.22f), new Vector2(0.70f, 0.48f),
        };

        private readonly RectTransform _root;
        private readonly Image _sheen;
        private readonly Image _deep;
        private readonly Image _dockShade;
        private readonly Image _streaks;
        private readonly Image[] _glints = new Image[GlintCount];
        private readonly Image[] _ripples = new Image[RippleCount];
        private readonly Image[] _lilies = new Image[LilyCount];
        private readonly Image _fish;
        private readonly Image _splash;
        private readonly Image _bird;
        private readonly Vector2[] _rippleBase = new Vector2[RippleCount];
        private readonly float[] _rippleSize = new float[RippleCount];
        private readonly Vector2[] _lilyBase = new Vector2[LilyCount];
        private readonly Vector2[] _glintBase = new Vector2[GlintCount];
        private float _w = 360f;
        private float _top;
        private float _bottom = 1f;
        private float _clockMs;
        private bool _reduceMotion;
        private bool _laidOut;
        private Vector2 _streaksBase;

        /// <summary>Crea el río (oculto hasta <see cref="Layout"/>) bajo <paramref name="parent"/>.</summary>
        public HomeRiver(RectTransform parent, ProceduralArt art)
        {
            _root = Rect("HomeRiver", parent);
            Canvas nested = _root.gameObject.AddComponent<Canvas>(); // lo que se mueve no rehace el resto del inicio
            nested.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            _sheen = Img("SkySheen", art.VerticalFade, new Color(Palette.SkyHorizon.r, Palette.SkyHorizon.g, Palette.SkyHorizon.b, 0.62f));
            _deep = Img("Depth", art.VerticalFade, new Color(0f, 0f, 0f, 0.32f));
            _deep.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            _dockShade = Img("DockShade", art.VerticalFade, new Color(0f, 0f, 0f, 0.28f));
            _dockShade.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            _streaks = Img("Streaks", art.RiverStreaks, new Color(Palette.Glint.r, Palette.Glint.g, Palette.Glint.b, 0.85f));
            for (int i = 0; i < GlintCount; i++)
            {
                _glints[i] = Img("Glint" + i, art.RiverRipple, Palette.Glint);
            }

            for (int i = 0; i < RippleCount; i++)
            {
                _ripples[i] = Img("Ripple" + i, art.RiverRipple, Color.white);
            }

            for (int i = 0; i < LilyCount; i++)
            {
                _lilies[i] = Img("Lily" + i, i % 2 == 0 ? art.RiverLilyFlower : art.RiverLily, Color.white);
                _lilies[i].preserveAspect = true;
            }

            _splash = Img("Splash", art.RiverRipple, Color.white);
            _fish = Img("Fish", art.RiverFish, Color.white);
            _bird = Img("Bird", art.RiverBird, Color.white);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Raíz del río (tests).</summary>
        public RectTransform Root => _root;

        /// <summary>El río se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Reloj del río (ms; tests y capturas).</summary>
        public float ClockMs => _clockMs;

        /// <summary>Con Reducir movimiento: todo quieto.</summary>
        public bool ReduceMotion => _reduceMotion;

        /// <summary>El pez está fuera del agua ahora (tests y capturas).</summary>
        public bool FishShown => _fish.gameObject.activeSelf;

        /// <summary>El ave cruza ahora (tests y capturas).</summary>
        public bool BirdShown => _bird.gameObject.activeSelf;

        /// <summary>Posición de la primera onda (tests: se mueve o se queda quieta).</summary>
        public Vector2 FirstRipplePosition => _ripples[0].rectTransform.anchoredPosition;

        /// <summary>Muestra u oculta el río.</summary>
        public void SetVisible(bool on)
        {
            on = on && _laidOut;
            if (_root.gameObject.activeSelf != on)
            {
                _root.gameObject.SetActive(on);
            }
        }

        /// <summary>
        /// Coloca el río en un lienzo de <paramref name="w"/> dp: del horizonte (<paramref name="top"/>) a la viga del muelle
        /// (<paramref name="bottom"/>), con la columna del sol en <paramref name="sunX"/>. Solo al maquetar, nunca por fotograma.
        /// </summary>
        public void Layout(float w, float top, float bottom, float sunX)
        {
            _w = w;
            _top = top;
            _bottom = Mathf.Max(top + 24f, bottom);
            float depth = _bottom - _top;
            float k = w / 360f;
            Box(_root, 0f, 0f, w, _bottom + 4f);
            Box(_sheen.rectTransform, 0f, _top, w, depth * 0.6f);
            Size(_streaks.rectTransform, w + (16f * k), depth - 6f);
            _streaksBase = new Vector2(w * 0.5f, _top + 3f + ((depth - 6f) * 0.5f));
            Flipped(_deep.rectTransform, 0f, _top + (depth * 0.35f), w, depth * 0.65f);
            Flipped(_dockShade.rectTransform, 0f, _bottom - 14f, w, 14f);
            for (int i = 0; i < GlintCount; i++)
            {
                float v = (i + 0.6f) / GlintCount;
                float y = _top + (depth * 0.9f * v * v) + 3f;
                float width = Mathf.Lerp(10f, 26f, v) * k;
                _glintBase[i] = new Vector2(sunX + (((i % 3) - 1) * 4f * k), y);
                Size(_glints[i].rectTransform, width, width * 0.25f);
            }

            for (int i = 0; i < RippleCount; i++)
            {
                Vector3 s = RippleSpots[i];
                float scale = Mathf.Lerp(0.45f, 1.15f, s.y);
                _rippleBase[i] = new Vector2(s.x * w, _top + 6f + (s.y * (depth - 12f)));
                _rippleSize[i] = ProceduralArt.RiverRippleWidthDp * scale * k;
                Size(_ripples[i].rectTransform, _rippleSize[i], ProceduralArt.RiverRippleHeightDp * scale * k);
            }

            for (int i = 0; i < LilyCount; i++)
            {
                Vector3 s = LilySpots[i];
                float width = s.z * Mathf.Lerp(0.7f, 1.1f, s.y) * k;
                _lilyBase[i] = new Vector2(s.x * w, _top + (s.y * depth));
                Size(_lilies[i].rectTransform, width, width * ProceduralArt.RiverLilyHeightDp / ProceduralArt.RiverLilyWidthDp);
            }

            Size(_fish.rectTransform, ProceduralArt.RiverFishWidthDp * k, ProceduralArt.RiverFishHeightDp * k);
            Size(_bird.rectTransform, ProceduralArt.RiverBirdWidthDp * k, ProceduralArt.RiverBirdHeightDp * k);
            _laidOut = true;
            Apply();
        }

        /// <summary>El agua del mundo (<paramref name="water"/>): tiñe la profundidad y la sombra del muelle.</summary>
        public void SetWater(Color water)
        {
            Color deep = ColorMath.Darken(water, 0.55f);
            deep.a = 0.42f;
            if (_deep.color != deep)
            {
                _deep.color = deep;
            }

            Color shade = ColorMath.Darken(water, 0.75f);
            shade.a = 0.38f;
            if (_dockShade.color != shade)
            {
                _dockShade.color = shade;
            }
        }

        /// <summary>Reducir movimiento: todo quieto desde ya (las ondas, en su sitio; sin pez ni ave).</summary>
        public void SetReduceMotion(bool on)
        {
            if (_reduceMotion == on)
            {
                return;
            }

            _reduceMotion = on;
            Apply();
        }

        /// <summary>Avanza el río <paramref name="dtMs"/> ms (sin asignaciones). Con Reducir movimiento no hace nada.</summary>
        public void Tick(float dtMs)
        {
            if (_reduceMotion || !_laidOut || !_root.gameObject.activeInHierarchy)
            {
                return;
            }

            _clockMs += Mathf.Clamp(dtMs, 0f, 100f);
            if (_clockMs > 1200000f)
            {
                _clockMs -= 1200000f; // múltiplo de los dos periodos: sin saltos
            }

            Apply();
        }

        /// <summary>Lleva el río al instante <paramref name="ms"/> (capturas deterministas).</summary>
        public void Seek(float ms)
        {
            _clockMs = Mathf.Max(0f, ms);
            Apply();
        }

        private void Apply()
        {
            if (!_laidOut)
            {
                return;
            }

            float t = _reduceMotion ? 0f : _clockMs * 0.001f;
            float k = _w / 360f;
            float drift = _reduceMotion ? 0f : Mathf.Sin(t * 0.35f) * 6f * k;
            Move(_streaks.rectTransform, _streaksBase.x + drift, _streaksBase.y);
            _streaks.canvasRenderer.SetAlpha(_reduceMotion ? 1f : 0.82f + (0.18f * Mathf.Sin(t * 0.9f)));
            for (int i = 0; i < GlintCount; i++)
            {
                float a = _reduceMotion ? 0.55f : 0.3f + (0.45f * (0.5f + (0.5f * Mathf.Sin((t * 2.3f) + (i * 1.9f)))));
                Move(_glints[i].rectTransform, _glintBase[i].x, _glintBase[i].y);
                _glints[i].canvasRenderer.SetAlpha(a);
            }

            for (int i = 0; i < RippleCount; i++)
            {
                float phase = RippleSpots[i].z;
                float dx = _reduceMotion ? 0f : Mathf.Sin((t * 0.45f) + phase) * 5f * k;
                float a = _reduceMotion ? 0.2f : 0.08f + (0.18f * (0.5f + (0.5f * Mathf.Sin((t * 0.8f) + (phase * 1.3f)))));
                Move(_ripples[i].rectTransform, _rippleBase[i].x + dx, _rippleBase[i].y);
                _ripples[i].canvasRenderer.SetAlpha(a);
            }

            for (int i = 0; i < LilyCount; i++)
            {
                float dy = _reduceMotion ? 0f : Mathf.Sin((t * 0.9f) + (i * 2.1f)) * 0.8f;
                Move(_lilies[i].rectTransform, _lilyBase[i].x, _lilyBase[i].y + dy);
            }

            ApplyFish();
            ApplyBird();
        }

        private void ApplyFish()
        {
            float depth = _bottom - _top;
            float cycle = _reduceMotion ? -1f : Mathf.Repeat(_clockMs, FishPeriodMs) - (FishPeriodMs - FishJumpMs - SplashMs);
            int spot = (int)(Mathf.Floor(_clockMs / FishPeriodMs) % FishSpots.Length);
            Vector2 s = FishSpots[spot < 0 ? 0 : spot];
            float x = s.x * _w;
            float y = _top + (s.y * depth);
            float k = _w / 360f;
            bool jumping = cycle >= 0f && cycle < FishJumpMs;
            if (_fish.gameObject.activeSelf != jumping)
            {
                _fish.gameObject.SetActive(jumping);
            }

            if (jumping)
            {
                float u = cycle / FishJumpMs; // 0..1
                float arc = 4f * u * (1f - u);
                float px = x - (14f * k) + (28f * k * u);
                float py = y - (arc * 22f * k);
                Move(_fish.rectTransform, px, py);
                _fish.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(38f, -38f, u));
            }

            float splashT = cycle - FishJumpMs;
            bool splash = cycle >= 0f && splashT >= -120f && splashT < SplashMs;
            if (_splash.gameObject.activeSelf != splash)
            {
                _splash.gameObject.SetActive(splash);
            }

            if (splash)
            {
                float u = Mathf.Clamp01((splashT + 120f) / (SplashMs + 120f));
                float width = Mathf.Lerp(10f, 34f, u) * k;
                Size(_splash.rectTransform, width, width * 0.28f);
                Move(_splash.rectTransform, x + (14f * k), y);
                _splash.canvasRenderer.SetAlpha(0.85f * (1f - u));
            }
        }

        private void ApplyBird()
        {
            float cycle = _reduceMotion ? -1f : Mathf.Repeat(_clockMs + 6000f, BirdPeriodMs);
            bool flying = cycle >= 0f && cycle < BirdCrossMs;
            if (_bird.gameObject.activeSelf != flying)
            {
                _bird.gameObject.SetActive(flying);
            }

            if (!flying)
            {
                return;
            }

            float u = cycle / BirdCrossMs;
            float k = _w / 360f;
            float x = Mathf.Lerp(-20f * k, _w + (20f * k), u);
            float y = _top - (36f * k) - (Mathf.Sin(u * Mathf.PI) * 18f * k);
            Move(_bird.rectTransform, x, y);
            float flap = 0.55f + (0.45f * Mathf.Abs(Mathf.Sin(cycle * 0.006f)));
            _bird.rectTransform.localScale = new Vector3(1f, flap, 1f);
        }

        private Image Img(string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_root, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            return rt;
        }

        // Caja con la esquina de arriba a la izquierda en (x, y desde arriba).
        private static void Box(RectTransform rt, float x, float yTop, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -yTop);
            rt.sizeDelta = new Vector2(w, h);
        }

        // Caja volteada en vertical (opaca abajo): con el pivote al centro, para que el volteo no la saque de su sitio.
        private static void Flipped(RectTransform rt, float x, float yTop, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + (w * 0.5f), -(yTop + (h * 0.5f)));
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void Size(RectTransform rt, float w, float h)
        {
            var size = new Vector2(w, h);
            if (rt.sizeDelta != size)
            {
                rt.sizeDelta = size;
            }
        }

        // El centro en (x, y desde arriba) del río; solo si cambia.
        private static void Move(RectTransform rt, float x, float y)
        {
            var pos = new Vector2(x, -y);
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }
        }
    }
}
