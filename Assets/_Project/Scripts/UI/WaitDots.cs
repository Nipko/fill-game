#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Indicador de espera de red: tres puntos en fila que se encienden uno tras otro mientras se espera al servidor (el borrado de
    /// la cuenta de 08 §11; <c>docs/dev/accesibilidad.md</c>). Con «Reducir movimiento» (02 §12, fila «Movimiento») se quedan quietos
    /// y encendidos: la espera se sigue viendo, sin animación. Es decorativo (sin texto ni raycast): lo que dice la espera lo dice
    /// la línea de texto de al lado, que TalkBack lee. Sin asignaciones por fotograma: <see cref="Tick"/> solo cambia el alfa.
    /// </summary>
    internal sealed class WaitDots
    {
        /// <summary>Puntos de la fila.</summary>
        public const int Count = 3;

        /// <summary>Lado de cada punto (dp) [P].</summary>
        public const float DotDp = 10f;

        /// <summary>Hueco entre puntos (dp) [P].</summary>
        public const float GapDp = 8f;

        /// <summary>Una vuelta de los tres puntos (ms) [P].</summary>
        public const float PeriodMs = 1200f;

        /// <summary>Alfa de un punto apagado [P].</summary>
        public const float DimAlpha = 0.3f;

        private readonly RectTransform _root;
        private readonly Image[] _dots = new Image[Count];
        private readonly Color _color;
        private float _ms;
        private bool _still;

        public WaitDots(string name, Transform parent, Sprite disc, Color color)
        {
            _root = UiBuilder.Rect(name, parent);
            _color = color;
            for (int i = 0; i < Count; i++)
            {
                Image dot = UiBuilder.Image("Dot" + i, _root, disc, color);
                UiBuilder.Place(dot.rectTransform, i * (DotDp + GapDp), 0f, DotDp, DotDp);
                _dots[i] = dot;
            }

            _root.gameObject.SetActive(false);
        }

        /// <summary>Ancho de la fila (dp).</summary>
        public static float WidthDp => (Count * DotDp) + ((Count - 1) * GapDp);

        /// <summary>Raíz (para colocarla).</summary>
        public RectTransform Root => _root;

        /// <summary>Se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Se ve y se mueve (false con «Reducir movimiento»).</summary>
        public bool Animating => Visible && !_still;

        /// <summary>Enseña la fila en (<paramref name="x"/>, <paramref name="y"/>) desde arriba; quieta si <paramref name="still"/>.</summary>
        public void Show(float x, float y, bool still)
        {
            UiBuilder.Place(_root, x, y, WidthDp, DotDp);
            _root.gameObject.SetActive(true);
            _still = still;
            _ms = 0f;
            Apply();
        }

        /// <summary>La esconde.</summary>
        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Avanza <paramref name="dtMs"/> ms (no hace nada escondida o quieta).</summary>
        public void Tick(float dtMs)
        {
            if (!Visible || _still || dtMs <= 0f)
            {
                return;
            }

            _ms = (_ms + dtMs) % PeriodMs;
            Apply();
        }

        /// <summary>Alfa del punto <paramref name="i"/> ahora (tests).</summary>
        public float AlphaOf(int i) => _dots[i].color.a;

        // Cada punto se enciende en su tercio de la vuelta (subida y bajada en triángulo) y se queda en DimAlpha el resto; quietos,
        // todos encendidos.
        private void Apply()
        {
            float t = _ms / PeriodMs;
            for (int i = 0; i < Count; i++)
            {
                float a = 1f;
                if (!_still)
                {
                    float local = t - ((float)i / Count);
                    if (local < 0f)
                    {
                        local += 1f;
                    }

                    float span = 2f / Count; // cada punto tarda dos tercios en subir y bajar: se solapan con el siguiente
                    float k = local < span ? 1f - Mathf.Abs((local / span * 2f) - 1f) : 0f;
                    a = Mathf.Lerp(DimAlpha, 1f, k);
                }

                Color c = _color;
                c.a = _color.a * a;
                _dots[i].color = c;
            }
        }
    }
}
