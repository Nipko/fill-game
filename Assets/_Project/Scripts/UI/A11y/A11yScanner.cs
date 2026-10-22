#nullable enable
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Una fuente propia de elementos del lector para un lienzo (bloque B9): la pantalla del nivel da sus cuias por orden de ranura
    /// y después la barra de comodines (02 §12). Si no reclama el lienzo, <see cref="A11yScanner"/> lo recorre de forma genérica.
    /// </summary>
    public interface IA11ySource
    {
        /// <summary>
        /// Si el lienzo <paramref name="canvas"/> es suyo y ahora lo describe ella, añade a <paramref name="into"/> sus elementos en el
        /// orden del foco y devuelve true; si no, false (lo recorre el escáner).
        /// </summary>
        bool Collect(Canvas canvas, A11yScanner scanner, List<A11yItem> into);
    }

    /// <summary>
    /// Recorrido genérico de la interfaz para el lector de pantalla (bloque B9; 02 §12: «todo botón lleva <c>contentDescription</c>
    /// con etiqueta funcional + estado»), para que TalkBack llegue a todas las pantallas sin que cada una tenga que describirse:
    /// <list type="number">
    /// <item>Toma los lienzos raíz activos de arriba abajo (<see cref="Canvas.sortingOrder"/>) y se para en el primero que tapa lo de
    /// debajo: el que tiene un velo, una imagen que recibe toques y cubre casi todo el lienzo (<see cref="BlockerCover"/>). Lo que
    /// queda tapado no se puede tocar y no se publica.</item>
    /// <item>En cada lienzo, en el orden de dibujo, lo que va detrás de su último velo tampoco cuenta (un modal en el mismo
    /// lienzo).</item>
    /// <item>Cada <see cref="Button"/> activo es un botón: su etiqueta es la registrada en <see cref="A11yLabels"/> o, si no hay,
    /// sus textos a la vista en orden, con el patrón de lista (<see cref="A11yText.Append"/>); gris, desactivado; activarlo pulsa el
    /// botón. Cada texto suelto a la vista es un texto.</item>
    /// <item>El orden es el de lectura: de arriba abajo por filas (dos elementos van en la misma fila si sus centros están a menos de
    /// media altura, la menor) y, dentro de una fila, de izquierda a derecha; los lienzos de debajo antes que los de encima (un aviso que flota sobre el tablero se lee después).</item>
    /// </list>
    /// Solo corre con el lector encendido (<see cref="A11yService"/>): asigna listas y cadenas.
    /// </summary>
    public sealed class A11yScanner
    {
        /// <summary>Fracción del lienzo que tiene que cubrir una imagen que recibe toques para tapar lo de debajo [P].</summary>
        public const float BlockerCover = 0.9f;

        private static readonly Vector3[] Corners = new Vector3[4];

        private readonly List<IA11ySource> _sources = new List<IA11ySource>();
        private readonly List<Canvas> _canvases = new List<Canvas>();
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<A11yItem> _canvasItems = new List<A11yItem>();
        private readonly List<List<A11yItem>> _layers = new List<List<A11yItem>>();

        /// <summary>Elementos sin etiqueta encontrados en el último recorrido (botones de solo icono sin registrar; tests y sonda).</summary>
        public int Unlabeled { get; private set; }

        /// <summary>Añade una fuente propia (la pantalla del nivel).</summary>
        public void AddSource(IA11ySource source)
        {
            if (source != null && !_sources.Contains(source))
            {
                _sources.Add(source);
            }
        }

        /// <summary>Quita una fuente propia.</summary>
        public void RemoveSource(IA11ySource source)
        {
            _sources.Remove(source);
        }

        /// <summary>Recorre la interfaz y deja en <paramref name="into"/> lo que el lector puede enfocar, en el orden del foco.</summary>
        public void Scan(List<A11yItem> into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            into.Clear();
            Unlabeled = 0;
            RootCanvases(_canvases);
            int layers = 0;
            for (int c = 0; c < _canvases.Count; c++)
            {
                Canvas canvas = _canvases[c];
                if (_layers.Count <= layers)
                {
                    _layers.Add(new List<A11yItem>());
                }

                List<A11yItem> layer = _layers[layers++];
                layer.Clear();
                bool claimed = false;
                for (int s = 0; s < _sources.Count && !claimed; s++)
                {
                    claimed = _sources[s].Collect(canvas, this, layer);
                }

                bool blocks = claimed ? HasBlocker(canvas) : CollectGeneric(canvas, null, layer);
                if (blocks)
                {
                    break;
                }
            }

            // De abajo arriba: la pantalla de debajo primero y lo que flota encima después.
            for (int l = layers - 1; l >= 0; l--)
            {
                into.AddRange(_layers[l]);
            }
        }

        /// <summary>
        /// Recorrido genérico de <paramref name="canvas"/> (ver la clase), sin entrar en <paramref name="skip"/> (lo que ya describió
        /// una fuente propia); añade a <paramref name="into"/> en el orden de lectura. Devuelve si el lienzo tapa lo de debajo.
        /// </summary>
        public bool CollectGeneric(Canvas canvas, ICollection<Transform>? skip, List<A11yItem> into)
        {
            _entries.Clear();
            Rect canvasRect = ((RectTransform)canvas.transform).rect;
            bool blocks = false;
            Walk(canvas.transform, skip, canvasRect, ref blocks, 1f);
            SortReadingOrder();
            for (int i = 0; i < _entries.Count; i++)
            {
                into.Add(_entries[i].Item);
            }

            _entries.Clear();
            return blocks;
        }

        /// <summary>El lienzo <paramref name="canvas"/> tiene un velo que tapa lo de debajo.</summary>
        public static bool HasBlocker(Canvas canvas)
        {
            Rect canvasRect = ((RectTransform)canvas.transform).rect;
            Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(false);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (IsBlocker(graphics[i], canvasRect))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Etiqueta de <paramref name="button"/>: la registrada o sus textos a la vista, en orden («» si no tiene).</summary>
        public static string ButtonLabel(Button button, out bool selected)
        {
            if (A11yLabels.TryGet(button.gameObject, out string label, out selected))
            {
                return label;
            }

            string text = string.Empty;
            TextMeshProUGUI[] texts = button.GetComponentsInChildren<TextMeshProUGUI>(false);
            for (int i = 0; i < texts.Length; i++)
            {
                if (Shown(texts[i]))
                {
                    text = A11yText.Append(text, texts[i].text.Trim());
                }
            }

            return text;
        }

        /// <summary>Elemento de botón para <paramref name="button"/> (etiqueta, desactivado y la pulsación).</summary>
        public static A11yItem ButtonItem(Button button, string? labelOverride = null)
        {
            string label = ButtonLabel(button, out bool selected);
            return new A11yItem("btn_" + button.GetInstanceID(), labelOverride ?? label, AccessibilityRole.Button, (RectTransform)button.transform, () => Press(button))
            {
                Disabled = !button.IsInteractable(),
                Selected = selected,
            };
        }

        /// <summary>Pulsa <paramref name="button"/> como un toque (si está activo y se puede pulsar).</summary>
        public static bool Press(Button button)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable())
            {
                return false;
            }

            button.onClick.Invoke();
            return true;
        }

        // Los lienzos raíz activos, de arriba abajo (sortingOrder mayor primero; empate, el último creado encima).
        private static void RootCanvases(List<Canvas> into)
        {
            into.Clear();
            Canvas[] all = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            for (int i = 0; i < all.Length; i++)
            {
                Canvas c = all[i];
                if (c.isRootCanvas && c.isActiveAndEnabled && c.renderMode != RenderMode.WorldSpace)
                {
                    into.Add(c);
                }
            }

            into.Sort((a, b) => a.sortingOrder != b.sortingOrder ? b.sortingOrder.CompareTo(a.sortingOrder) : b.GetInstanceID().CompareTo(a.GetInstanceID()));
        }

        // Recorrido en orden de dibujo: un velo descarta lo recogido hasta él en este lienzo.
        private void Walk(Transform t, ICollection<Transform>? skip, Rect canvasRect, ref bool blocks, float alpha)
        {
            if (!t.gameObject.activeInHierarchy || (skip != null && skip.Contains(t)))
            {
                return;
            }

            CanvasGroup? group = t.GetComponent<CanvasGroup>();
            if (group != null)
            {
                alpha *= group.alpha;
                if (group.ignoreParentGroups)
                {
                    alpha = group.alpha;
                }
            }

            if (alpha <= 0.01f)
            {
                return;
            }

            Graphic? graphic = t.GetComponent<Graphic>();
            if (graphic != null && IsBlocker(graphic, canvasRect))
            {
                _entries.Clear(); // lo de detrás del velo no se puede tocar
                blocks = true;
            }

            Button? button = t.GetComponent<Button>();
            if (button != null && button.enabled)
            {
                Add(ButtonItem(button), (RectTransform)t);
                return; // sus textos son su etiqueta
            }

            if (graphic is TextMeshProUGUI text && Shown(text) && !Silenced(text))
            {
                Add(new A11yItem("txt_" + text.GetInstanceID(), text.text.Trim(), AccessibilityRole.StaticText, text.rectTransform, null), text.rectTransform);
            }

            for (int i = 0; i < t.childCount; i++)
            {
                Walk(t.GetChild(i), skip, canvasRect, ref blocks, alpha);
            }
        }

        // Un texto con etiqueta registrada a «» se calla (decorativo).
        private static bool Silenced(TextMeshProUGUI text)
        {
            return A11yLabels.TryGet(text.gameObject, out string label, out _) && label.Length == 0;
        }

        private void Add(A11yItem item, RectTransform rect)
        {
            Rect frame = UnityScreenReaderHost.ScreenFrame(rect);
            if (frame.width <= 0f || frame.height <= 0f || frame.xMax <= 0f || frame.yMax <= 0f || frame.xMin >= Screen.width || frame.yMin >= Screen.height)
            {
                return; // fuera de la pantalla
            }

            if (item.Label.Length == 0)
            {
                Unlabeled++;
            }

            _entries.Add(new Entry(item, frame.center.y, frame.height, frame.xMin, _entries.Count));
        }

        private static bool Shown(TextMeshProUGUI text)
        {
            return text != null && text.isActiveAndEnabled && text.color.a > 0.01f && !string.IsNullOrWhiteSpace(text.text);
        }

        private static bool IsBlocker(Graphic graphic, Rect canvasRect)
        {
            if (!graphic.isActiveAndEnabled || !graphic.raycastTarget || graphic is TextMeshProUGUI)
            {
                return false;
            }

            RectTransform rt = graphic.rectTransform;
            rt.GetWorldCorners(Corners);
            Transform canvas = graphic.canvas != null ? graphic.canvas.rootCanvas.transform : rt.root;
            Vector3 a = canvas.InverseTransformPoint(Corners[0]);
            Vector3 b = canvas.InverseTransformPoint(Corners[2]);
            float w = Mathf.Abs(b.x - a.x);
            float h = Mathf.Abs(b.y - a.y);
            return canvasRect.width > 0f && canvasRect.height > 0f && w * h >= BlockerCover * canvasRect.width * canvasRect.height;
        }

        // Orden de lectura: primero de arriba abajo por el centro; después, filas por solape (un elemento entra en la fila si su
        // centro queda a menos de media altura, la menor de las dos, del centro del primero de la fila); dentro de cada fila, de
        // izquierda a derecha. Determinista (el empate final es el orden de dibujo).
        private void SortReadingOrder()
        {
            _entries.Sort((a, b) => a.CenterY != b.CenterY ? a.CenterY.CompareTo(b.CenterY) : a.Order.CompareTo(b.Order));
            int row = 0;
            int anchor = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                Entry first = _entries[anchor];
                if (i > anchor && e.CenterY - first.CenterY >= Mathf.Max(1f, Mathf.Min(e.Height, first.Height) * 0.5f))
                {
                    row++;
                    anchor = i;
                }

                _entries[i] = e.WithRow(row);
            }

            _entries.Sort(CompareEntries);
        }

        private static int CompareEntries(Entry a, Entry b)
        {
            if (a.Row != b.Row)
            {
                return a.Row.CompareTo(b.Row);
            }

            if (a.X != b.X)
            {
                return a.X.CompareTo(b.X);
            }

            return a.Order.CompareTo(b.Order);
        }

        private readonly struct Entry
        {
            public Entry(A11yItem item, float centerY, float height, float x, int order, int row = 0)
            {
                Item = item;
                CenterY = centerY;
                Height = height;
                X = x;
                Order = order;
                Row = row;
            }

            public A11yItem Item { get; }

            public float CenterY { get; }

            public float Height { get; }

            public float X { get; }

            public int Order { get; }

            public int Row { get; }

            public Entry WithRow(int row)
            {
                return new Entry(Item, CenterY, Height, X, Order, row);
            }
        }
    }
}
