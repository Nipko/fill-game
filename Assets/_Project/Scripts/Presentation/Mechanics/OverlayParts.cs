#nullable enable
using AF.Core.Board;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Piezas comunes de los overlays y de las marcas de las cuias: anclas, <see cref="Image"/> sin toques colocadas en u
    /// desde el pie, y filas de puntos (<see cref="PipRow"/>). Todo se crea una vez; después solo se cambian valores y solo
    /// cuando cambian (08 §5.4: con la pantalla quieta el lienzo no se reconstruye).
    /// </summary>
    /// <remarks>
    /// Sistema de coordenadas de todas las anclas de <c>CuiaView</c>: origen en el centro del pie de la cuia, y hacia
    /// arriba, en dp. Una pieza en (x, y) u con tamaño (w, h) u se coloca con <see cref="Place"/>.
    /// </remarks>
    public static class OverlayParts
    {
        /// <summary>Gris perla #B8BEC6 [P]: puntos claros, nube de la Lluvia. ΔE00 ≥ 14 frente a los 14 tintes.</summary>
        public static readonly Color Pearl = Palette.Hex(0xB8BEC6);

        /// <summary>Pizarra #2A3B3E [P]: puntos oscuros sobre fondos claros. ΔE00 ≥ 17 frente a los 14 tintes.</summary>
        public static readonly Color Slate = Palette.Hex(0x2A3B3E);

        /// <summary>Crea un ancla vacía (sin tamaño) en el pie de <paramref name="parent"/>.</summary>
        public static RectTransform NewAnchor(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        /// <summary>Crea una imagen sin toques (<c>raycastTarget</c> = false), oculta, centrada en el pie de <paramref name="parent"/>.</summary>
        public static Image NewImage(string name, Transform parent, Sprite? sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            go.SetActive(false);
            return img;
        }

        /// <summary>Centra <paramref name="graphic"/> en (<paramref name="xU"/>, <paramref name="yU"/>) u con tamaño <paramref name="wU"/> × <paramref name="hU"/> u.</summary>
        public static void Place(Graphic graphic, float xU, float yU, float wU, float hU, float u)
        {
            RectTransform rt = graphic.rectTransform;
            var pos = new Vector2(xU * u, yU * u);
            var size = new Vector2(wU * u, hU * u);
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }

            if (rt.sizeDelta != size)
            {
                rt.sizeDelta = size;
            }
        }

        /// <summary>Activa o desactiva <paramref name="component"/> solo si cambia.</summary>
        public static void SetVisible(Component component, bool on)
        {
            if (component.gameObject.activeSelf != on)
            {
                component.gameObject.SetActive(on);
            }
        }

        /// <summary>Fija el color de <paramref name="graphic"/> solo si cambia.</summary>
        public static void SetColor(Graphic graphic, Color color)
        {
            if (graphic.color != color)
            {
                graphic.color = color;
            }
        }

        /// <summary><paramref name="color"/> con su alfa multiplicado por <paramref name="alpha"/>.</summary>
        public static Color Fade(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, color.a * alpha);
        }
    }

    /// <summary>
    /// Fila de puntos centrada (contadores de Lluvia y Piraña, parejas del Remolino, cuenta de la Liana): hasta
    /// <see cref="MaxPips"/> imágenes creadas una vez; los primeros <see cref="Filled"/> llevan el punto lleno y el resto el
    /// aro. <see cref="Set"/> y <see cref="Layout"/> no asignan memoria.
    /// </summary>
    public sealed class PipRow
    {
        /// <summary>Puntos como mucho (3 bits de los campos de mecánica).</summary>
        public const int MaxPips = Cuia.MaxMechanicField;

        private readonly Image[] _pips = new Image[MaxPips];
        private readonly Sprite _full;
        private readonly Sprite _empty;
        private Color _color;
        private float _alpha = 1f;
        private bool _visible = true;
        private float _x;
        private float _y;
        private float _size = 0.16f;
        private float _step = 0.26f;
        private float _u = 24f;

        /// <summary>Crea la fila dentro de <paramref name="parent"/> con los sprites lleno y vacío, teñidos de <paramref name="color"/>.</summary>
        public PipRow(string name, Transform parent, Sprite full, Sprite empty, Color color)
        {
            _full = full;
            _empty = empty;
            _color = color;
            for (int i = 0; i < MaxPips; i++)
            {
                _pips[i] = OverlayParts.NewImage(name + i, parent, empty, color);
            }
        }

        /// <summary>Puntos de la fila.</summary>
        public int Count { get; private set; }

        /// <summary>Puntos llenos (los primeros, de izquierda a derecha).</summary>
        public int Filled { get; private set; }

        /// <summary>La fila se ve (con al menos un punto).</summary>
        public bool Visible => _visible && Count > 0;

        /// <summary>Fija <paramref name="count"/> puntos con <paramref name="filled"/> llenos (se recortan a 0..<see cref="MaxPips"/>).</summary>
        public void Set(int count, int filled)
        {
            count = Mathf.Clamp(count, 0, MaxPips);
            filled = Mathf.Clamp(filled, 0, count);
            bool moved = count != Count;
            Count = count;
            Filled = filled;
            for (int i = 0; i < MaxPips; i++)
            {
                Image pip = _pips[i];
                Sprite sprite = i < filled ? _full : _empty;
                if (pip.sprite != sprite)
                {
                    pip.sprite = sprite;
                }
            }

            if (moved)
            {
                Place();
            }

            ApplyVisibility();
        }

        /// <summary>Centro de la fila (u), lado de cada punto y paso entre centros (u), y u en dp.</summary>
        public void Layout(float xU, float yU, float sizeU, float stepU, float u)
        {
            _x = xU;
            _y = yU;
            _size = sizeU;
            _step = stepU;
            _u = u;
            Place();
        }

        /// <summary>Muestra u oculta la fila entera.</summary>
        public void SetVisible(bool on)
        {
            _visible = on;
            ApplyVisibility();
        }

        /// <summary>Opacidad de la fila (atenuada: Piraña con la cueva llena).</summary>
        public void SetAlpha(float alpha)
        {
            _alpha = alpha;
            Color c = OverlayParts.Fade(_color, alpha);
            for (int i = 0; i < MaxPips; i++)
            {
                OverlayParts.SetColor(_pips[i], c);
            }
        }

        /// <summary>Cambia el color de los puntos.</summary>
        public void SetColor(Color color)
        {
            _color = color;
            SetAlpha(_alpha);
        }

        /// <summary>Imagen del punto <paramref name="i"/> (tests).</summary>
        public Image Pip(int i)
        {
            return _pips[i];
        }

        private void Place()
        {
            float start = _x - (0.5f * _step * (Count - 1));
            for (int i = 0; i < MaxPips; i++)
            {
                OverlayParts.Place(_pips[i], start + (i * _step), _y, _size, _size, _u);
            }
        }

        private void ApplyVisibility()
        {
            for (int i = 0; i < MaxPips; i++)
            {
                OverlayParts.SetVisible(_pips[i], _visible && i < Count);
            }
        }
    }
}
