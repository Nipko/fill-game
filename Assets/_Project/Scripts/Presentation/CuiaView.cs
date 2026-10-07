#nullable enable
using System;
using AF.Core.Board;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Vista de una cuia en uGUI (GDD 07 §4): ranura fija con su área táctil (02 §3.3: ancho de la ranura × alto de la
    /// cuia + 32 dp) y un cuerpo móvil con vidrio trasero, dosis de líquido de 1 u exactas (cavidad recta), menisco,
    /// vidrio frontal, tapa de calabaza y aro de luz. La vista nunca decide reglas: muestra lo que le dice el modelo o la
    /// cola de animación (02 §3.2).
    /// </summary>
    /// <remarks>
    /// <para>Sin el shader <c>Liquid</c> (fuera del prototipo v0): las franjas son rectángulos de color y el líquido gira
    /// con el vidrio al inclinarse. Cada dosis lleva encima el patrón de su tinte al 15 % (02 §12; 07 §3, regla 1).
    /// Unidades en dp del lienzo (360 de ancho de referencia).</para>
    /// <para>Input (02 §3.1): el touch-down solo avisa (la elevación empieza ya, 0 ms de latencia percibida) y la
    /// decisión se toma en el touch-up: soltar dentro del área táctil es un toque; soltar fuera lo cancela.</para>
    /// <para>La pose del cuerpo solo se escribe cuando cambia, para no ensuciar el lienzo cada fotograma con la pantalla
    /// quieta (08 §5.4).</para>
    /// </remarks>
    public sealed class CuiaView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>Elevación de la selección: 16 dp (02 §4.1; 07 §4.3).</summary>
        public const float LiftDp = 16f;

        /// <summary>Altura extra del área táctil sobre la cuia, para alcanzarla elevada (02 §3.3).</summary>
        public const float TouchHeadroomDp = 32f;

        /// <summary>Deselección: 100 ms ease-in (02 §4.1).</summary>
        public const float LowerMs = 100f;

        /// <summary>Rechazo: sacudida horizontal de ±6 dp, 2 ciclos, 120 ms (02 §4.1).</summary>
        public const float ShakeMs = 120f;

        /// <summary>Amplitud de la sacudida (dp).</summary>
        public const float ShakeDp = 6f;

        /// <summary>Sellado completo: 450 ms (02 §4.1; storyboard de 07 §5.3).</summary>
        public const float SealMs = 450f;

        private static readonly Color RingColor = new Color(0xF4 / 255f, 0xEB / 255f, 0xDA / 255f, 0.40f);

        private RectTransform _root = null!;
        private RectTransform _body = null!;
        private RectTransform _liquid = null!;
        private Image _back = null!;
        private Image _front = null!;
        private Image _cap = null!;
        private Image _glow = null!;
        private Image _flash = null!;
        private Image _meniscus = null!;
        private Image[] _doses = Array.Empty<Image>();
        private RawImage[] _patterns = Array.Empty<RawImage>();
        private ProceduralArt _art = null!;
        private Color[] _doseColor = Array.Empty<Color>();
        private float[] _doseFill = Array.Empty<float>();
        private int[] _doseTint = Array.Empty<int>();
        private bool _liquidDirty;
        private Action<int>? _onDown;
        private Action<int, bool>? _onUp;
        private Vector2 _shownOffset;
        private float _shownAngle;

        private float _u = 24f;
        private float _liftMs = 120f;
        private bool _locked;
        private bool _liftTarget;
        private float _liftValue;
        private float _shakeElapsed = -1f;
        private float _sealElapsed = -1f;
        private float _fxDelay = -1f;
        private float _fxElapsed = -1f;
        private bool _sealed;

        /// <summary>Índice de la cuia en el tablero.</summary>
        public int Index { get; private set; }

        /// <summary>Capacidad (dosis).</summary>
        public int Capacity { get; private set; }

        /// <summary>Unidad u en dp (ancho de la cuia / 2, 07 §4.1).</summary>
        public float Unit => _u;

        /// <summary>Alto de la cuia en u.</summary>
        public float HeightU => CuiaGeometry.HeightU(Capacity);

        /// <summary>Ranura (raíz fija; posición del pie en el contenedor del tablero, origen arriba a la izquierda).</summary>
        public RectTransform Root => _root;

        /// <summary>Cuerpo móvil (elevación, sacudida, traslado e inclinación).</summary>
        public RectTransform Body => _body;

        /// <summary>Pie de la cuia en reposo, en coordenadas del contenedor del tablero.</summary>
        public Vector2 FootPosition => _root.anchoredPosition;

        /// <summary>Desplazamiento actual del cuerpo respecto a su ranura.</summary>
        public Vector2 BodyOffset => _body.anchoredPosition;

        /// <summary>Ángulo actual del cuerpo (grados; negativo = inclinada a la derecha).</summary>
        public float BodyAngle => _body.localEulerAngles.z > 180f ? _body.localEulerAngles.z - 360f : _body.localEulerAngles.z;

        /// <summary>Un trabajo de animación controla la pose del cuerpo.</summary>
        public bool Locked => _locked;

        /// <summary>Está sellada en la vista (tapa puesta).</summary>
        public bool Sealed => _sealed;

        /// <summary>Altura actual del líquido en dosis (suma de rellenos), para el chorro.</summary>
        public float LiquidLevel
        {
            get
            {
                float level = 0f;
                for (int k = 0; k < _doseFill.Length; k++)
                {
                    level += _doseFill[k];
                }

                return level;
            }
        }

        /// <summary>
        /// Crea la vista dentro de <paramref name="parent"/> (contenedor del tablero). <paramref name="onDown"/> recibe el
        /// touch-down y <paramref name="onUp"/> el touch-up con "soltado dentro del área táctil" (02 §3.1).
        /// </summary>
        public static CuiaView Create(RectTransform parent, int index, int capacity, ProceduralArt art, Action<int> onDown, Action<int, bool> onUp)
        {
            var go = new GameObject("Cuia" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(HitArea));
            go.transform.SetParent(parent, false);
            CuiaView view = go.AddComponent<CuiaView>();
            view.Build(index, capacity, art, onDown, onUp);
            return view;
        }

        /// <summary>
        /// Coloca la ranura: <paramref name="foot"/> es el centro del pie en el contenedor (origen arriba a la izquierda,
        /// y hacia arriba), <paramref name="cuiaWidth"/> el ancho de la cuia y <paramref name="slotWidth"/> el de la ranura.
        /// </summary>
        public void SetGeometry(Vector2 foot, float cuiaWidth, float slotWidth)
        {
            _u = cuiaWidth / CuiaGeometry.WidthU;
            float h = HeightU * _u;
            _root.anchoredPosition = foot;
            _root.sizeDelta = new Vector2(slotWidth, h + TouchHeadroomDp);
            _body.sizeDelta = new Vector2(cuiaWidth, h);

            _liquid.anchoredPosition = new Vector2(0f, CuiaGeometry.CavityBottomU * _u);
            _liquid.sizeDelta = new Vector2(CuiaGeometry.CavityHalfWidthU * 2f * _u, Capacity * _u);
            _meniscus.rectTransform.sizeDelta = new Vector2(0f, Mathf.Max(1.5f, 0.08f * _u));

            _cap.rectTransform.sizeDelta = new Vector2(1.10f * _u, 0.35f * _u);
            _glow.rectTransform.anchoredPosition = new Vector2(0f, (HeightU - 0.3f) * _u);
            _glow.rectTransform.sizeDelta = new Vector2(2.2f * _u, 2.2f * _u);
            PlaceCapAtRest();
            _liquidDirty = true;
        }

        /// <summary>Duración de la elevación (<c>anim_lift_ms</c> × factor de velocidad).</summary>
        public void SetLiftDuration(float ms)
        {
            _liftMs = Mathf.Max(1f, ms);
        }

        /// <summary>Muestra el contenido final de <paramref name="cuia"/> (sin animación).</summary>
        public void ShowContent(Cuia cuia, bool sealedNow)
        {
            int count = cuia.Count;
            for (int k = 0; k < Capacity; k++)
            {
                if (k < count)
                {
                    int tint = cuia.ColorAt(k);
                    SetDose(k, Palette.Tint(tint), 1f, tint);
                }
                else
                {
                    SetDose(k, Color.clear, 0f, 0);
                }
            }

            SetSealed(sealedNow);
        }

        /// <summary>Fija el color, el relleno (0..1 de la dosis) y el tinte (0 = agua base) de la dosis <paramref name="k"/>.</summary>
        public void SetDose(int k, Color color, float fill, int tint)
        {
            if ((uint)k >= (uint)Capacity)
            {
                return;
            }

            _doseColor[k] = color;
            _doseFill[k] = Mathf.Clamp01(fill);
            _doseTint[k] = tint;
            _liquidDirty = true;
        }

        /// <summary>Color actual de la dosis <paramref name="k"/>.</summary>
        public Color DoseColor(int k)
        {
            return _doseColor[k];
        }

        /// <summary>Pone o quita la tapa sin animación.</summary>
        public void SetSealed(bool sealedNow)
        {
            _sealed = sealedNow;
            _sealElapsed = -1f;
            _cap.gameObject.SetActive(sealedNow);
            _glow.gameObject.SetActive(false);
            _flash.color = Color.clear;
            PlaceCapAtRest();
        }

        /// <summary>
        /// Fotograma <paramref name="ms"/> del sellado (07 §5.3): brillo del líquido 0-100 ms, caída de la tapa
        /// 100-250 ms con mini-rebote, aro de luz 250-450 ms.
        /// </summary>
        public void SealAnimationAt(float ms)
        {
            _sealed = true;
            _sealElapsed = ms;
            float flash = ms < 100f ? Mathf.Sin(Mathf.PI * (ms / 100f)) * 0.15f : 0f;
            _flash.color = new Color(1f, 1f, 1f, flash);
            bool capVisible = ms >= 100f;
            _cap.gameObject.SetActive(capVisible);
            if (capVisible)
            {
                float t = Mathf.Clamp01((ms - 100f) / 150f);
                float drop = 0.6f * (1f - Ease.Out(t));
                float bounce = ms > 250f && ms < 350f ? 0.03f * Mathf.Sin(Mathf.PI * ((ms - 250f) / 100f)) : 0f;
                _cap.rectTransform.anchoredPosition = new Vector2(0f, (HeightU - 0.04f + drop + bounce) * _u);
            }

            bool ring = ms >= 250f && ms < SealMs;
            _glow.gameObject.SetActive(ring);
            if (ring)
            {
                float t = (ms - 250f) / (SealMs - 250f);
                _glow.color = new Color(RingColor.r, RingColor.g, RingColor.b, 0.9f * (1f - t));
                _glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.3f, t);
            }

            if (ms >= SealMs)
            {
                SetSealed(true);
            }
        }

        /// <summary>Elevación de la selección (02 §4.1); se aplica cuando ningún trabajo controla el cuerpo.</summary>
        public void SetLifted(bool lifted)
        {
            _liftTarget = lifted;
        }

        /// <summary>La cuia está elevada o subiendo (selección o touch-down en curso).</summary>
        public bool IsLifted => _liftTarget;

        /// <summary>Rechazo suave: sacudida (02 §4.1, C2, C6).</summary>
        public void Shake()
        {
            _shakeElapsed = 0f;
        }

        /// <summary>Celebración (07 §5.4): tras <paramref name="delayMs"/>, aro de luz si está sellada y salto de 0,15 u.</summary>
        public void PlayCelebration(float delayMs)
        {
            _fxDelay = Mathf.Max(0f, delayMs);
            _fxElapsed = -1f;
        }

        /// <summary>Toma el control de la pose del cuerpo (trabajo de vertido o intro).</summary>
        public void Lock()
        {
            _locked = true;
            _shakeElapsed = -1f;
        }

        /// <summary>Fija la pose del cuerpo mientras está bloqueada.</summary>
        public void SetPose(Vector2 offset, float angleDeg)
        {
            WritePose(offset, angleDeg);
        }

        /// <summary>Devuelve el cuerpo a su ranura y libera el control (la elevación arranca desde 0).</summary>
        public void Unlock()
        {
            _locked = false;
            _liftValue = 0f;
            WritePose(Vector2.zero, 0f);
        }

        /// <summary>Punto del labio (centro del borde superior) en coordenadas del contenedor, con la pose actual.</summary>
        public Vector2 LipInContainer()
        {
            float angle = BodyAngle * Mathf.Deg2Rad;
            float l = HeightU * _u;
            return FootPosition + BodyOffset + new Vector2(-l * Mathf.Sin(angle), l * Mathf.Cos(angle));
        }

        /// <summary>Superficie del líquido en reposo, en coordenadas del contenedor.</summary>
        public Vector2 SurfaceInContainer()
        {
            return FootPosition + new Vector2(0f, (CuiaGeometry.CavityBottomU + LiquidLevel) * _u);
        }

        /// <summary>Avanza las animaciones propias (elevación, sacudida, celebración) y aplica el líquido.</summary>
        public void Tick(float dtMs)
        {
            if (!_locked)
            {
                float target = _liftTarget ? 1f : 0f;
                if (!Mathf.Approximately(_liftValue, target))
                {
                    float duration = _liftTarget ? _liftMs : LowerMs;
                    _liftValue = Mathf.MoveTowards(_liftValue, target, dtMs / duration);
                }

                float x = 0f;
                if (_shakeElapsed >= 0f)
                {
                    _shakeElapsed += dtMs;
                    float p = _shakeElapsed / ShakeMs;
                    if (p >= 1f)
                    {
                        _shakeElapsed = -1f;
                    }
                    else
                    {
                        x = ShakeDp * Mathf.Sin(p * 4f * Mathf.PI);
                    }
                }

                float y = LiftDp * (_liftTarget ? Ease.Out(_liftValue) : Ease.In(_liftValue));
                y += HopOffset(dtMs);
                WritePose(new Vector2(x, y), 0f);
            }
            else
            {
                HopOffset(dtMs);
            }

            if (_liquidDirty)
            {
                ApplyLiquid();
            }
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            _onDown?.Invoke(Index);
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData)
        {
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(_root, eventData.position, eventData.pressEventCamera);
            _onUp?.Invoke(Index, inside);
        }

        // Solo toca el RectTransform si la pose cambió: con la pantalla quieta el lienzo no se reconstruye (08 §5.4).
        private void WritePose(Vector2 offset, float angleDeg)
        {
            if (offset == _shownOffset && angleDeg == _shownAngle)
            {
                return;
            }

            _shownOffset = offset;
            _shownAngle = angleDeg;
            _body.anchoredPosition = offset;
            _body.localRotation = angleDeg == 0f ? Quaternion.identity : Quaternion.Euler(0f, 0f, angleDeg);
        }

        private float HopOffset(float dtMs)
        {
            if (_fxDelay >= 0f)
            {
                _fxDelay -= dtMs;
                if (_fxDelay < 0f)
                {
                    _fxDelay = -1f;
                    _fxElapsed = 0f;
                }

                return 0f;
            }

            if (_fxElapsed < 0f)
            {
                return 0f;
            }

            _fxElapsed += dtMs;
            const float hopMs = 240f;
            const float glowMs = 300f;
            if (_sealed)
            {
                bool on = _fxElapsed < glowMs;
                _glow.gameObject.SetActive(on);
                if (on)
                {
                    float t = _fxElapsed / glowMs;
                    _glow.color = new Color(RingColor.r, RingColor.g, RingColor.b, 0.8f * (1f - t));
                    _glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.25f, t);
                }
            }

            if (_fxElapsed >= Mathf.Max(hopMs, glowMs))
            {
                _fxElapsed = -1f;
                _glow.gameObject.SetActive(false);
                return 0f;
            }

            return _fxElapsed < hopMs ? 0.15f * _u * Mathf.Sin(Mathf.PI * (_fxElapsed / hopMs)) : 0f;
        }

        private void ApplyLiquid()
        {
            _liquidDirty = false;
            int top = -1;
            for (int k = 0; k < Capacity; k++)
            {
                Image img = _doses[k];
                float fill = _doseFill[k];
                bool on = fill > 0.001f && _doseColor[k].a > 0.001f;
                if (img.enabled != on)
                {
                    img.enabled = on;
                }

                // Patrón del tinte (07 §3, regla 1): marcas opacas mezcladas al 15 % con el color actual de la dosis.
                int doseTint = _doseTint[k];
                RawImage pattern = _patterns[k];
                bool patternOn = on && ProceduralArt.HasPattern(doseTint);
                if (pattern.enabled != patternOn)
                {
                    pattern.enabled = patternOn;
                }

                if (!on)
                {
                    continue;
                }

                RectTransform rt = img.rectTransform;
                rt.anchoredPosition = new Vector2(0f, k * _u);
                rt.sizeDelta = new Vector2(0f, fill * _u);
                img.color = _doseColor[k];
                if (patternOn)
                {
                    pattern.color = Palette.PatternMark(_doseColor[k], doseTint);
                    pattern.uvRect = _art.PatternUv(doseTint, CuiaGeometry.CavityHalfWidthU * 2f, fill);
                }

                top = k;
            }

            if (top < 0)
            {
                _meniscus.enabled = false;
                return;
            }

            _meniscus.enabled = true;
            Color c = _doseColor[top];
            int tint = _doseTint[top];
            Color men = ColorMath.Lighten(c, tint > 0 ? Palette.MeniscusLighten(tint) : 0.20f);
            _meniscus.color = men;
            _meniscus.rectTransform.anchoredPosition = new Vector2(0f, (top + _doseFill[top]) * _u);
        }

        private void PlaceCapAtRest()
        {
            _cap.rectTransform.anchoredPosition = new Vector2(0f, (HeightU - 0.04f) * _u);
        }

        private void Build(int index, int capacity, ProceduralArt art, Action<int> onDown, Action<int, bool> onUp)
        {
            Index = index;
            Capacity = capacity;
            _art = art;
            _onDown = onDown;
            _onUp = onUp;
            _root = (RectTransform)transform;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0.5f, 0f);
            GetComponent<HitArea>().raycastTarget = true;

            _body = NewRect("Body", _root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

            _back = NewImage("GlassBack", _body, art.CuiaBack(capacity), Color.white);
            Stretch(_back.rectTransform);

            _liquid = NewRect("Liquid", _body, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            _doses = new Image[capacity];
            _patterns = new RawImage[capacity];
            _doseColor = new Color[capacity];
            _doseFill = new float[capacity];
            _doseTint = new int[capacity];
            for (int k = 0; k < capacity; k++)
            {
                Image dose = NewImage("Dose" + k, _liquid, null, Color.clear);
                SetAnchors(dose.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
                dose.enabled = false;
                _doses[k] = dose;

                var patternGo = new GameObject("Pattern", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                patternGo.transform.SetParent(dose.transform, false);
                RawImage pattern = patternGo.GetComponent<RawImage>();
                pattern.texture = art.PatternAtlas;
                pattern.raycastTarget = false;
                pattern.enabled = false;
                Stretch(pattern.rectTransform);
                _patterns[k] = pattern;
            }

            _meniscus = NewImage("Meniscus", _liquid, null, Color.white);
            SetAnchors(_meniscus.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f));
            _meniscus.enabled = false;

            _flash = NewImage("SealFlash", _liquid, null, Color.clear);
            Stretch(_flash.rectTransform);

            _front = NewImage("GlassFront", _body, art.CuiaFront(capacity), Color.white);
            Stretch(_front.rectTransform);

            _cap = NewImage("SealCap", _body, art.SealCap, Color.white);
            SetAnchors(_cap.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            _cap.gameObject.SetActive(false);

            _glow = NewImage("SealRing", _body, art.Ring, RingColor);
            SetAnchors(_glow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f));
            _glow.gameObject.SetActive(false);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            SetAnchors(rt, anchorMin, anchorMax, pivot);
            return rt;
        }

        private static Image NewImage(string name, Transform parent, Sprite? sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
