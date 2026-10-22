#nullable enable
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using AF.Presentation;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
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
        private static readonly List<Graphic> GraphicsBuffer = new List<Graphic>();
        private static readonly List<CanvasGroup> GroupsBuffer = new List<CanvasGroup>();
        private static readonly List<TextMeshProUGUI> TextsBuffer = new List<TextMeshProUGUI>();
        private static readonly List<Canvas> CanvasBuffer = new List<Canvas>();

        private readonly List<IA11ySource> _sources = new List<IA11ySource>();
        private readonly List<Canvas> _canvases = new List<Canvas>();
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<A11yItem> _canvasItems = new List<A11yItem>();
        private readonly List<List<A11yItem>> _layers = new List<List<A11yItem>>();
        private readonly List<RectMask2D> _masks = new List<RectMask2D>();

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
            bool groupBlocks = false;
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
                groupBlocks |= blocks;
                bool groupEnds = c + 1 >= _canvases.Count || _canvases[c + 1].sortingOrder != canvas.sortingOrder;
                if (groupEnds)
                {
                    if (groupBlocks)
                    {
                        break;
                    }

                    groupBlocks = false;
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
            List<Graphic> graphics = GraphicsBuffer;
            canvas.GetComponentsInChildren(false, graphics);
            bool blocked = false;
            for (int i = 0; i < graphics.Count && !blocked; i++)
            {
                blocked = IsBlocker(graphics[i], canvasRect);
            }

            graphics.Clear();
            return blocked;
        }

        /// <summary>
        /// En <paramref name="canvas"/>, algo dibujado después de <paramref name="marker"/> (y fuera de él) lo tapa: un velo que recibe
        /// toques y cubre casi todo el lienzo (un modal en el mismo lienzo: la pausa, una confirmación). Lo de debajo de
        /// <paramref name="marker"/> (el fondo que recoge los toques del tablero) no cuenta.
        /// </summary>
        public static bool BlockedAbove(Canvas canvas, Transform marker)
        {
            Rect canvasRect = ((RectTransform)canvas.transform).rect;
            List<Graphic> graphics = GraphicsBuffer; // en orden de dibujo (profundidad primero); sin asignar
            canvas.GetComponentsInChildren(false, graphics);
            bool past = false;
            bool blocked = false;
            for (int i = 0; i < graphics.Count && !blocked; i++)
            {
                if (graphics[i].transform.IsChildOf(marker))
                {
                    past = true;
                    continue;
                }

                blocked = past && IsBlocker(graphics[i], canvasRect);
            }

            graphics.Clear();
            return blocked;
        }

        /// <summary>
        /// Lo de <paramref name="marker"/> en <paramref name="canvas"/> está tapado: por un velo dibujado después en el mismo lienzo
        /// (<see cref="BlockedAbove"/>: la pausa, una confirmación) o por otro lienzo de encima o del mismo orden con un velo (el
        /// Mercado, un anuncio, la tarjeta del Reto). Busca los lienzos (asigna): solo al activar un nodo del lector, nunca por
        /// fotograma.
        /// </summary>
        public static bool Covered(Canvas canvas, Transform marker)
        {
            CanvasesAbove(canvas, CanvasBuffer);
            bool covered = CoveredBy(canvas, marker, CanvasBuffer);
            CanvasBuffer.Clear();
            return covered;
        }

        /// <summary>
        /// Deja en <paramref name="into"/> los lienzos raíz que pueden ir encima de <paramref name="canvas"/>: los de
        /// <see cref="Canvas.sortingOrder"/> mayor o igual (con el mismo orden, Unity no fija cuál va encima: se cuentan los dos, por
        /// prudencia). Asigna (busca los lienzos): una vez, no por fotograma.
        /// </summary>
        public static void CanvasesAbove(Canvas canvas, List<Canvas> into)
        {
            into.Clear();
            Canvas[] all = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                Canvas c = all[i];
                if (c != canvas && c.isRootCanvas && c.renderMode != RenderMode.WorldSpace && c.sortingOrder >= canvas.sortingOrder)
                {
                    into.Add(c);
                }
            }
        }

        /// <summary>Como <see cref="Covered"/>, con los lienzos de encima ya buscados (<see cref="CanvasesAbove"/>), sin asignar.</summary>
        public static bool CoveredBy(Canvas canvas, Transform marker, List<Canvas> above)
        {
            if (BlockedAbove(canvas, marker))
            {
                return true;
            }

            for (int i = 0; i < above.Count; i++)
            {
                Canvas c = above[i];
                if (c != null && c.isActiveAndEnabled && HasBlocker(c))
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
            bool selected;
            string label;
            if (labelOverride != null)
            {
                label = labelOverride;
                selected = A11yLabels.SelectedOf(button.gameObject);
            }
            else
            {
                label = ButtonLabel(button, out selected);
            }

            return new A11yItem("btn_" + button.GetInstanceID(), label, AccessibilityRole.Button, (RectTransform)button.transform, () => Press(button))
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

        // De arriba abajo por sortingOrder; con el mismo orden, el de la jerarquía (Unity no lo fija: Scan los trata como un grupo).
        private static int CompareTop(Canvas a, Canvas b)
        {
            return b.sortingOrder.CompareTo(a.sortingOrder);
        }

        // Los lienzos raíz activos, de arriba abajo por sortingOrder (los de un mismo orden van juntos: Scan los trata como grupo).
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

            into.Sort(CompareTop);
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

            // Lo que se toca sin ser un botón (un velo «toca para seguir» de la tarjeta previa o del banner, un marcador): un botón
            // que hace el mismo toque.
            if (graphic != null && graphic.raycastTarget && IsTapTarget(t) && BlocksRaycasts(graphic))
            {
                string? label = TapLabel(t, graphic, canvasRect);
                if (label != null)
                {
                    GameObject target = t.gameObject;
                    Add(new A11yItem("tap_" + target.GetInstanceID(), label, AccessibilityRole.Button, (RectTransform)t, () => Tap(target)), (RectTransform)t);
                }
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

        /// <summary>
        /// Toca <paramref name="target"/> como un dedo en su centro (touch-down, touch-up y clic), para lo que no es un
        /// <see cref="Button"/> (los relés de toque). False si ya no está activo.
        /// </summary>
        public static bool Tap(GameObject target)
        {
            if (target == null || !target.activeInHierarchy || EventSystem.current == null)
            {
                return false;
            }

            Graphic? graphic = target.GetComponent<Graphic>();
            if (graphic != null && !BlocksRaycasts(graphic))
            {
                return false; // un velo que se está yendo: un dedo ya no lo alcanza, el lector tampoco
            }

            RectTransform rt = (RectTransform)target.transform;
            Rect frame = UnityScreenReaderHost.ScreenFrame(rt);
            var data = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(frame.center.x, Screen.height - frame.center.y),
                button = PointerEventData.InputButton.Left,
            };
            data.pointerPress = target;
            data.rawPointerPress = target;
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
            return true;
        }

        // Un relé de toque del juego (PointerTapRelay o PointerDownRelay de AF.Presentation).
        private static bool IsTapTarget(Transform t)
        {
            return t.GetComponent<PointerTapRelay>() != null || t.GetComponent<PointerDownRelay>() != null;
        }

        // Etiqueta de un relé: la registrada (null si es «»: se calla, como el fondo del tablero, C7), sus textos o, si cubre casi
        // todo el lienzo (un velo «toca para seguir»), «Continuar» [P]; si no, «» (sin etiqueta: lo dice la sonda).
        private static string? TapLabel(Transform t, Graphic graphic, Rect canvasRect)
        {
            if (A11yLabels.TryGet(t.gameObject, out string label, out _))
            {
                return label.Length == 0 ? null : label;
            }

            string text = string.Empty;
            t.GetComponentsInChildren(false, TextsBuffer);
            for (int i = 0; i < TextsBuffer.Count; i++)
            {
                if (Shown(TextsBuffer[i]))
                {
                    text = A11yText.Append(text, TextsBuffer[i].text.Trim());
                }
            }

            TextsBuffer.Clear();

            if (text.Length > 0)
            {
                return text;
            }

            return Covers(graphic, canvasRect) ? A11yText.TapToContinue : string.Empty;
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

            // Recortado del todo por un visor (RectMask2D; la lista de Ajustes que se desplaza): no se ve, no se publica.
            rect.GetComponentsInParent(false, _masks);
            for (int i = 0; i < _masks.Count; i++)
            {
                if (_masks[i].isActiveAndEnabled && !UnityScreenReaderHost.ScreenFrame(_masks[i].rectTransform).Overlaps(frame))
                {
                    return;
                }
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
            if (!graphic.isActiveAndEnabled || !graphic.raycastTarget || graphic is TextMeshProUGUI || !BlocksRaycasts(graphic))
            {
                return false;
            }

            return Covers(graphic, canvasRect);
        }

        // Un CanvasGroup de encima con blocksRaycasts apagado (un velo que se está yendo) deja pasar los toques: no tapa.
        private static bool BlocksRaycasts(Graphic graphic)
        {
            graphic.GetComponentsInParent(false, GroupsBuffer);
            bool blocks = true;
            for (int i = 0; i < GroupsBuffer.Count; i++)
            {
                CanvasGroup group = GroupsBuffer[i];
                if (!group.blocksRaycasts)
                {
                    blocks = false;
                    break;
                }

                if (group.ignoreParentGroups)
                {
                    break;
                }
            }

            GroupsBuffer.Clear();
            return blocks;
        }

        private static bool Covers(Graphic graphic, Rect canvasRect)
        {
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
