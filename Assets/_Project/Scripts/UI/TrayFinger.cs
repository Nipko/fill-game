#nullable enable
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Dedo guía sobre un botón de la bandeja (tarea B11): el del primer uso de la Cuia extra en el nivel 8 (02 §13, fila 8; 06
    /// §2.2: «dedo guía en su primer uso»; 04 §2.2). La misma mano y la misma onda que el dedo guía del tablero
    /// (<see cref="TutorialFinger"/>, D-028 con P5), pero sin viaje: aparece sobre el botón, lo toca (se hunde un poco y deja la
    /// onda), se queda, se funde y espera, en bucle (<see cref="CycleMs"/>). Con Reducir movimiento (07 §7.8), la mano fija sobre
    /// el botón, sin onda ni fundidos. No captura toques (ningún gráfico es <c>raycastTarget</c>) y sigue al botón si se mueve.
    /// Las imágenes se crean una vez en un Canvas anidado propio (moverlas no reconstruye el lienzo principal); cada fotograma solo
    /// coloca y tiñe, sin asignaciones (08 §5.4). Las duraciones son las del dedo del tablero [P].
    /// </summary>
    internal sealed class TrayFinger
    {
        /// <summary>Entrada de la mano (ms), la de <see cref="TutorialFinger.AppearMs"/>.</summary>
        public const float AppearMs = TutorialFinger.AppearMs;

        /// <summary>Toque (ms), el de <see cref="TutorialFinger.PressMs"/>.</summary>
        public const float PressMs = TutorialFinger.PressMs;

        /// <summary>La mano se queda tras tocar (ms), la de <see cref="TutorialFinger.HoldMs"/>.</summary>
        public const float HoldMs = TutorialFinger.HoldMs;

        /// <summary>Fundido de salida (ms), el de <see cref="TutorialFinger.FadeMs"/>.</summary>
        public const float FadeMs = TutorialFinger.FadeMs;

        /// <summary>Pausa sin mano (ms), la de <see cref="TutorialFinger.GapMs"/>.</summary>
        public const float GapMs = TutorialFinger.GapMs;

        /// <summary>Una vuelta entera del bucle (ms).</summary>
        public const float CycleMs = AppearMs + PressMs + HoldMs + FadeMs + GapMs;

        /// <summary>
        /// Altura del punto que toca la yema dentro del botón, desde abajo (fracción de su alto) [P]: el icono, que va arriba y
        /// deja la etiqueta y el saldo a la vista.
        /// </summary>
        public const float TapHeightFraction = 0.62f;

        private readonly RectTransform _root;
        private readonly Image _ripple;
        private readonly Image _rippleOutline;
        private readonly Image _hand;
        private readonly ProceduralArt _art;
        private RectTransform? _target;
        private bool _active;
        private bool _reduceMotion;
        private float _t;
        private int _rippleStep = -1;

        /// <summary>Crea la mano (oculta) en <paramref name="layer"/>, por encima de lo que ya tenga.</summary>
        public TrayFinger(RectTransform layer, ProceduralArt art)
        {
            _art = art;
            var go = new GameObject("TrayFinger", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(layer, false);
            _root = (RectTransform)go.transform;
            UiBuilder.Stretch(_root);
            _rippleOutline = NewImage("RippleOutline", new Vector2(0.5f, 0.5f));
            _ripple = NewImage("Ripple", new Vector2(0.5f, 0.5f));
            _hand = NewImage("Hand", ProceduralArt.FingerTipPivot);
            _hand.sprite = art.FingerHand;
            _hand.rectTransform.sizeDelta = new Vector2(ProceduralArt.FingerHandWidthDp, ProceduralArt.FingerHandHeightDp);
            _hand.rectTransform.localRotation = Quaternion.Euler(0f, 0f, TutorialFinger.HandTiltDeg);
            go.SetActive(false);
        }

        /// <summary>La mano está a la vista.</summary>
        public bool Shown => _active;

        /// <summary>El botón que señala (null si ninguno).</summary>
        public RectTransform? Target => _target;

        /// <summary>Va sin animación (Reducir movimiento).</summary>
        public bool ReduceMotion => _reduceMotion;

        /// <summary>La mano (AF_LAYOUT y tests).</summary>
        public Image Hand => _hand;

        /// <summary>Alfa de la mano ahora (tests).</summary>
        public float HandAlpha => _hand.color.a;

        /// <summary>Milisegundos dentro de la vuelta del bucle (tests).</summary>
        public float CycleTimeMs => _t;

        /// <summary>Señala <paramref name="button"/> desde el principio del bucle (o fijo, con <paramref name="reduceMotion"/>).</summary>
        public void Show(RectTransform button, bool reduceMotion)
        {
            _target = button;
            _reduceMotion = reduceMotion;
            _t = 0f;
            _active = true;
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            Tick(0f);
        }

        /// <summary>Retira la mano.</summary>
        public void Hide()
        {
            _active = false;
            _target = null;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Avanza el bucle y coloca la mano sobre el botón. Sin asignaciones.</summary>
        public void Tick(float dtMs)
        {
            RectTransform? target = _target;
            if (!_active || target == null)
            {
                return;
            }

            Vector3 tip = TapPoint(target);
            float press = ProceduralArt.FingerHandHeightDp * TutorialFinger.PressDepth;
            if (_reduceMotion)
            {
                SetHand(tip, 1f, 0f);
                _ripple.enabled = false;
                _rippleOutline.enabled = false;
                return;
            }

            _t = (_t + dtMs) % CycleMs;
            float t = _t;
            float alpha;
            float sink = 0f;
            float ripple = -1f;
            if (t < AppearMs)
            {
                alpha = t / AppearMs;
            }
            else if ((t -= AppearMs) < PressMs)
            {
                alpha = 1f;
                float k = t / PressMs;
                sink = Mathf.Sin(k * Mathf.PI);
                ripple = k;
            }
            else if ((t -= PressMs) < HoldMs)
            {
                alpha = 1f;
            }
            else if ((t -= HoldMs) < FadeMs)
            {
                alpha = 1f - (t / FadeMs);
            }
            else
            {
                alpha = 0f;
            }

            SetHand(tip, alpha, sink * press);
            SetRipple(tip, ripple);
        }

        // La yema en el punto que toca del botón (en coordenadas de mundo), bajada «down» dp al tocar.
        private void SetHand(Vector3 tip, float alpha, float downDp)
        {
            RectTransform hand = _hand.rectTransform;
            hand.position = tip;
            if (downDp != 0f)
            {
                hand.anchoredPosition -= new Vector2(0f, downDp);
            }

            Color c = Color.white;
            c.a = alpha;
            _hand.color = c;
        }

        // La onda del toque: crece de FingerRippleStartDp a FingerRippleDp y se apaga (k en 0-1; < 0 sin onda).
        private void SetRipple(Vector3 tip, float k)
        {
            bool on = k >= 0f;
            _ripple.enabled = on;
            _rippleOutline.enabled = on;
            if (!on)
            {
                return;
            }

            int step = Mathf.Clamp(Mathf.FloorToInt(k * ProceduralArt.FingerRippleSteps), 0, ProceduralArt.FingerRippleSteps - 1);
            if (step != _rippleStep)
            {
                _rippleStep = step;
                _ripple.sprite = _art.FingerRippleAt(step);
                _rippleOutline.sprite = _art.FingerRippleOutlineAt(step);
            }

            float size = Mathf.Lerp(TutorialFinger.RippleStartDp, TutorialFinger.RippleEndDp, k);
            Vector2 sizeV = new Vector2(size, size);
            _ripple.rectTransform.sizeDelta = sizeV;
            _rippleOutline.rectTransform.sizeDelta = sizeV;
            _ripple.rectTransform.position = tip;
            _rippleOutline.rectTransform.position = tip;
            float fade = 1f - k;
            _ripple.color = new Color(1f, 1f, 1f, TutorialFinger.RippleAlpha * fade);
            _rippleOutline.color = new Color(1f, 1f, 1f, TutorialFinger.RippleOutlineAlpha * fade);
        }

        // El punto del botón que toca la yema, en coordenadas de mundo: centrado y a TapHeightFraction de su alto desde abajo.
        private static Vector3 TapPoint(RectTransform button)
        {
            Rect r = button.rect;
            var local = new Vector3(r.xMin + (r.width * 0.5f), r.yMin + (r.height * TapHeightFraction), 0f);
            return button.TransformPoint(local);
        }

        private Image NewImage(string name, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            RectTransform rt = image.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            return image;
        }
    }
}
