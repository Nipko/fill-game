#nullable enable
using TMPro;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Respuesta de una línea «Te faltan {n} semillas» de un modal al toque en su botón en gris (observación 6 de la jornada 5 en
    /// el teléfono, 2026-10-28: «Continuar: +1 cuia» sin saldo no hacía nada). Es la de la burbuja de un comodín sin saldo
    /// (<see cref="BoosterTray.PulseHintMissing"/>; pregunta 48 (b), D-022): un latido de 2 × <see cref="BoosterTray.MissingPulseBeatMs"/>
    /// hasta <see cref="BoosterTray.MissingPulseScale"/>, centrado en la línea. Con Reducir movimiento (02 §12), sin latido: la línea
    /// se atenúa hasta <see cref="ReducedAlpha"/> y vuelve en el mismo tiempo [P]. Otro toque la vuelve a empezar. Sin asignaciones.
    /// </summary>
    internal sealed class NotePulse
    {
        /// <summary>Opacidad más baja de la línea con Reducir movimiento [P].</summary>
        public const float ReducedAlpha = 0.4f;

        private readonly TextMeshProUGUI _text;
        private float _ms = -1f;
        private bool _reduce;
        private Vector2 _base;

        public NotePulse(TextMeshProUGUI text)
        {
            _text = text;
        }

        /// <summary>Duración de la respuesta (ms).</summary>
        public static float DurationMs => 2f * BoosterTray.MissingPulseBeatMs;

        /// <summary>La respuesta está en marcha.</summary>
        public bool Running => _ms >= 0f;

        /// <summary>Veces que empezó (tests).</summary>
        public int Starts { get; private set; }

        /// <summary>Escala de ahora de la línea (1 en reposo).</summary>
        public float ScaleNow => _text.rectTransform.localScale.y;

        /// <summary>Opacidad de ahora de la línea (1 en reposo).</summary>
        public float AlphaNow => _text.alpha;

        /// <summary>Lo más que crece la línea en el latido.</summary>
        public static float MaxScale => BoosterTray.MissingPulseScale;

        /// <summary>Empieza la respuesta. False si la línea no se ve o no tiene texto.</summary>
        public bool Start(bool reduceMotion)
        {
            if (!_text.gameObject.activeInHierarchy || _text.text.Length == 0)
            {
                return false;
            }

            Stop();
            _base = _text.rectTransform.anchoredPosition;
            _ms = 0f;
            _reduce = reduceMotion;
            Starts++;
            return true;
        }

        /// <summary>Un fotograma de la respuesta.</summary>
        public void Tick(float dtMs)
        {
            if (_ms < 0f)
            {
                return;
            }

            _ms += dtMs;
            if (_ms >= DurationMs || !_text.gameObject.activeInHierarchy)
            {
                Stop();
                return;
            }

            float k = Mathf.Sin(Mathf.PI * (_ms / DurationMs));
            if (_reduce)
            {
                _text.alpha = Mathf.Lerp(1f, ReducedAlpha, k);
                return;
            }

            Apply(Mathf.Lerp(1f, BoosterTray.MissingPulseScale, k));
        }

        /// <summary>Deja la línea en reposo (escala 1, opaca y en su sitio). Sin respuesta en marcha, no hace nada.</summary>
        public void Stop()
        {
            if (_ms < 0f)
            {
                return;
            }

            _ms = -1f;
            Apply(1f);
            _text.alpha = 1f;
        }

        // La línea se coloca con el pivote arriba a la izquierda (UiBuilder.Place): para crecer desde su centro, se desplaza la mitad
        // de lo que crece hacia la izquierda y hacia arriba.
        private void Apply(float scale)
        {
            RectTransform rt = _text.rectTransform;
            rt.localScale = new Vector3(scale, scale, 1f);
            Vector2 size = rt.rect.size;
            rt.anchoredPosition = new Vector2(_base.x - ((scale - 1f) * size.x * 0.5f), _base.y + ((scale - 1f) * size.y * 0.5f));
        }
    }
}
