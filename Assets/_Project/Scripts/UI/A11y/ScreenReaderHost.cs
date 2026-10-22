#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;

namespace AF.UI
{
    /// <summary>
    /// Un elemento que el lector de pantalla puede enfocar (bloque B9; 02 §12, fila «Lector de pantalla»): su etiqueta (la
    /// <c>contentDescription</c> de Android: «etiqueta funcional + estado»), su papel, si está desactivado o seleccionado, el
    /// rectángulo que ocupa en pantalla y lo que hace al activarlo (doble toque con TalkBack). Lo arman las fuentes
    /// (<see cref="A11yScanner"/> y la del tablero, <c>GameScreen</c>) y lo publica un <see cref="IScreenReaderHost"/>.
    /// </summary>
    public sealed class A11yItem
    {
        /// <summary>Crea el elemento <paramref name="id"/> (estable mientras sea el mismo objeto en pantalla).</summary>
        public A11yItem(string id, string label, AccessibilityRole role, RectTransform? rect, Func<bool>? invoke)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Label = label ?? string.Empty;
            Role = role;
            Rect = rect;
            Invoke = invoke;
        }

        /// <summary>Identificador estable (el del objeto: <c>cuia_3</c>, <c>btn_12345</c>…).</summary>
        public string Id { get; }

        /// <summary>Etiqueta que se lee («cuia 3: açaí, açaí, aguaje, 1 hueco», «Deshacer, 2 restantes»…); «» si no tiene.</summary>
        public string Label { get; }

        /// <summary>Papel (botón, texto…): el lector lo dice en el idioma del teléfono (no es un texto del juego).</summary>
        public AccessibilityRole Role { get; }

        /// <summary>Desactivado (un botón gris): el lector lo dice en el idioma del teléfono.</summary>
        public bool Disabled { get; set; }

        /// <summary>Seleccionado (la cuia elevada como origen).</summary>
        public bool Selected { get; set; }

        /// <summary>Lo que ocupa en pantalla (null: sin marco; no se puede tocar para enfocarlo).</summary>
        public RectTransform? Rect { get; }

        /// <summary>Lo que hace al activarlo; null si no hace nada (un texto). Devuelve si hizo algo.</summary>
        public Func<bool>? Invoke { get; }

        /// <summary>Misma identidad y mismo contenido que <paramref name="other"/> (si no cambia nada, no se vuelve a publicar).</summary>
        public bool SameAs(A11yItem other)
        {
            return other != null && string.Equals(Id, other.Id, StringComparison.Ordinal) && string.Equals(Label, other.Label, StringComparison.Ordinal)
                && Role == other.Role && Disabled == other.Disabled && Selected == other.Selected && ReferenceEquals(Rect, other.Rect);
        }
    }

    /// <summary>
    /// Donde se publican los elementos del lector de pantalla (bloque B9): en el juego, el módulo de accesibilidad de Unity
    /// (<see cref="UnityScreenReaderHost"/>: <see cref="AssistiveSupport"/>, que llega a TalkBack en Android); en los tests,
    /// <see cref="FakeScreenReaderHost"/>, que solo los guarda.
    /// </summary>
    public interface IScreenReaderHost
    {
        /// <summary>El lector de pantalla del teléfono está encendido (sin él no se publica nada: coste cero).</summary>
        bool ScreenReaderOn { get; }

        /// <summary>Sustituye lo publicado por <paramref name="items"/>, en el orden del foco.</summary>
        void Publish(IReadOnlyList<A11yItem> items);

        /// <summary>Quita todo lo publicado.</summary>
        void Clear();

        /// <summary>Dice <paramref name="text"/> sin mover el foco (el anuncio de la selección, 02 §12).</summary>
        void Announce(string text);
    }

    /// <summary>
    /// <see cref="IScreenReaderHost"/> de los tests y del Editor sin lector: guarda lo publicado y lo anunciado, y deja elegir si
    /// el lector está encendido.
    /// </summary>
    public sealed class FakeScreenReaderHost : IScreenReaderHost
    {
        private readonly List<A11yItem> _items = new List<A11yItem>();
        private readonly List<string> _announcements = new List<string>();

        /// <summary>Crea el anfitrión falso, con el lector <paramref name="on"/>.</summary>
        public FakeScreenReaderHost(bool on = true)
        {
            ScreenReaderOn = on;
        }

        /// <inheritdoc />
        public bool ScreenReaderOn { get; set; }

        /// <summary>Lo publicado ahora, en el orden del foco.</summary>
        public IReadOnlyList<A11yItem> Items => _items;

        /// <summary>Lo anunciado, en orden.</summary>
        public IReadOnlyList<string> Announcements => _announcements;

        /// <summary>Veces que se publicó (los tests comprueban que no se republica sin cambios).</summary>
        public int Publishes { get; private set; }

        /// <inheritdoc />
        public void Publish(IReadOnlyList<A11yItem> items)
        {
            _items.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                _items.Add(items[i]);
            }

            Publishes++;
        }

        /// <inheritdoc />
        public void Clear()
        {
            _items.Clear();
        }

        /// <inheritdoc />
        public void Announce(string text)
        {
            _announcements.Add(text ?? string.Empty);
        }

        /// <summary>El primer elemento publicado cuya etiqueta es <paramref name="label"/>, o null.</summary>
        public A11yItem? Find(string label)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].Label, label, StringComparison.Ordinal))
                {
                    return _items[i];
                }
            }

            return null;
        }
    }

    /// <summary>
    /// <see cref="IScreenReaderHost"/> real (bloque B9): el módulo de accesibilidad de Unity (<c>com.unity.modules.accessibility</c>,
    /// ya en el manifiesto: sin paquete nuevo). Un <see cref="AccessibilityHierarchy"/> plano, con un nodo por elemento en el orden
    /// del foco (se rehace solo si cambian los elementos; si no, se actualizan sus nodos sin mover el foco), que se activa (<see cref="AssistiveSupport.activeHierarchy"/>); el marco de cada nodo se
    /// calcula de su <see cref="RectTransform"/> cuando el lector lo pide (<see cref="AccessibilityNode.frameGetter"/>); activarlo
    /// (doble toque) llama a <see cref="A11yItem.Invoke"/>; los anuncios van por
    /// <see cref="AssistiveSupport.notificationDispatcher"/>. El origen de coordenadas del marco es la esquina de arriba a la
    /// izquierda de la pantalla [NV]: hay que comprobarlo en el teléfono con TalkBack (el recuadro del foco sobre cada cuia).
    /// </summary>
    public sealed class UnityScreenReaderHost : IScreenReaderHost
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        private readonly List<AccessibilityNode> _nodes = new List<AccessibilityNode>();
        private readonly List<A11yItem> _current = new List<A11yItem>();
        private readonly List<NodeRef> _refs = new List<NodeRef>();
        private AccessibilityHierarchy? _hierarchy;

        /// <summary>Lector forzado por un test o un build de desarrollo (null: el del sistema).</summary>
        public static bool? ScreenReaderOverride { get; set; }

        /// <inheritdoc />
        public bool ScreenReaderOn => ScreenReaderOverride ?? AssistiveSupport.isScreenReaderEnabled;

        /// <summary>
        /// Publica <paramref name="items"/>. Si son los mismos elementos en el mismo orden (mismos <see cref="A11yItem.Id"/>), solo
        /// cambia la etiqueta y el estado de cada nodo: el foco de TalkBack no se mueve (tras seleccionar una cuia o verter, el
        /// jugador sigue donde estaba). Si cambian, rehace la jerarquía y devuelve el foco al elemento que lo tenía, si sigue.
        /// </summary>
        public void Publish(IReadOnlyList<A11yItem> items)
        {
            if (_hierarchy != null && SameIds(items))
            {
                AccessibilityNode? focusedChanged = null;
                for (int i = 0; i < items.Count; i++)
                {
                    bool changed = !items[i].SameAs(_current[i]);
                    _current[i] = items[i];
                    _refs[i].Item = items[i];
                    Apply(_nodes[i], items[i]);
                    if (changed && _nodes[i].isFocused)
                    {
                        focusedChanged = _nodes[i];
                    }
                }

                if (focusedChanged != null)
                {
                    // El elemento enfocado cambió (tras un vertido, «1 hueco» → «2 huecos»): se avisa con el foco en él mismo, para
                    // que TalkBack no se quede con la descripción vieja [NV] y sin mover el foco.
                    AssistiveSupport.notificationDispatcher.SendLayoutChanged(focusedChanged);
                }

                return;
            }

            string? focused = null;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].isFocused)
                {
                    focused = _current[i].Id;
                }
            }

            var hierarchy = new AccessibilityHierarchy();
            _nodes.Clear();
            _current.Clear();
            _refs.Clear();
            AccessibilityNode? refocus = null;
            for (int i = 0; i < items.Count; i++)
            {
                A11yItem item = items[i];
                AccessibilityNode node = hierarchy.AddNode(item.Label, null);
                var nodeRef = new NodeRef(hierarchy, item);
                _nodes.Add(node);
                _current.Add(item);
                _refs.Add(nodeRef);
                Apply(node, item);
                node.frameGetter = nodeRef.Frame;
                node.invoked += nodeRef.Invoke;
                if (focused != null && string.Equals(item.Id, focused, StringComparison.Ordinal))
                {
                    refocus = node;
                }
            }

            _hierarchy = hierarchy;
            AssistiveSupport.activeHierarchy = hierarchy;
            AssistiveSupport.notificationDispatcher.SendLayoutChanged(refocus);
        }

        // Lo que hace un nodo: su elemento de ahora (se actualiza en sitio) y su jerarquía; uno de una jerarquía que ya no está
        // activa no hace nada.
        private sealed class NodeRef
        {
            private readonly AccessibilityHierarchy _hierarchy;

            public NodeRef(AccessibilityHierarchy hierarchy, A11yItem item)
            {
                _hierarchy = hierarchy;
                Item = item;
            }

            public A11yItem Item { get; set; }

            public bool Live => ReferenceEquals(AssistiveSupport.activeHierarchy, _hierarchy);

            // El marco del elemento de ahora (el de una publicación en sitio, no el de cuando se creó el nodo).
            public Rect Frame()
            {
                return Item.Rect != null ? ScreenFrame(Item.Rect!) : Rect.zero;
            }

            // Activar: solo si su jerarquía es la activa (un nodo de una jerarquía vieja no hace nada).
            public bool Invoke()
            {
                return Live && Item.Invoke != null && Item.Invoke!();
            }
        }

        /// <summary>Lo que hace el nodo <paramref name="index"/> al activarlo (tests: un nodo viejo, tras republicar, no hace nada).</summary>
        internal Func<bool> NodeInvoker(int index)
        {
            return _refs[index].Invoke;
        }

        /// <summary>El marco que da el nodo <paramref name="index"/> (tests: tras una publicación en sitio, el del elemento nuevo).</summary>
        internal Func<Rect> NodeFrame(int index)
        {
            return _refs[index].Frame;
        }

        private static void Apply(AccessibilityNode node, A11yItem item)
        {
            node.label = item.Label;
            node.role = item.Role;
            node.state = item.Disabled ? AccessibilityState.Disabled : item.Selected ? AccessibilityState.Selected : AccessibilityState.None;
        }

        private bool SameIds(IReadOnlyList<A11yItem> items)
        {
            if (items.Count != _current.Count)
            {
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (!string.Equals(items[i].Id, _current[i].Id, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_hierarchy != null && ReferenceEquals(AssistiveSupport.activeHierarchy, _hierarchy))
            {
                AssistiveSupport.activeHierarchy = null;
            }

            _hierarchy = null;
            _nodes.Clear();
            _current.Clear();
            _refs.Clear();
        }

        /// <inheritdoc />
        public void Announce(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                AssistiveSupport.notificationDispatcher.SendAnnouncement(text);
            }
        }

        /// <summary>
        /// Rectángulo de <paramref name="rect"/> en píxeles de pantalla con el origen arriba a la izquierda [NV] (vacío si el objeto
        /// ya no existe o no está activo).
        /// </summary>
        public static Rect ScreenFrame(RectTransform rect)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy)
            {
                return Rect.zero;
            }

            Canvas? canvas = rect.GetComponentInParent<Canvas>();
            Camera? camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            rect.GetWorldCorners(Corners);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(camera, Corners[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
        }
    }
}
