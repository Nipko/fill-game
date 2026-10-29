#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

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

        /// <summary>
        /// La lista que se desplaza en la que va (Ajustes, el Mercado; Q-B9-17), o null: el lector le da la acción de desplazar y,
        /// al enfocarlo, desplaza el visor para enseñarlo (<see cref="A11yScroll"/>). Se publica aunque el visor lo recorte.
        /// </summary>
        public ScrollRect? Scroll { get; set; }

        /// <summary>
        /// El marco que da ahora en pantalla (píxeles, origen arriba a la izquierda): en una lista que se desplaza, lo que se ve de él
        /// en el visor (<see cref="A11yScroll.Frame"/>); vacío sin <see cref="Rect"/> o si ya no existe. Es el que se publica en el
        /// nodo y el que <see cref="A11yService"/> vigila para saber si hay que volver a leer los marcos (observación 7 de la jornada
        /// 6: tras desplazar la Liga, TalkBack seguía con los marcos de antes).
        /// </summary>
        public UnityEngine.Rect CurrentFrame()
        {
            if (Rect == null)
            {
                return UnityEngine.Rect.zero;
            }

            return Scroll != null ? A11yScroll.Frame(Scroll!, Rect!) : UnityScreenReaderHost.ScreenFrame(Rect!);
        }

        /// <summary>Misma identidad y mismo contenido que <paramref name="other"/> (si no cambia nada, no se vuelve a publicar).</summary>
        public bool SameAs(A11yItem other)
        {
            return other != null && string.Equals(Id, other.Id, StringComparison.Ordinal) && string.Equals(Label, other.Label, StringComparison.Ordinal)
                && Role == other.Role && Disabled == other.Disabled && Selected == other.Selected && ReferenceEquals(Rect, other.Rect)
                && ReferenceEquals(Scroll, other.Scroll);
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

        /// <summary>
        /// Lo publicado sigue igual, pero se movió en la pantalla (se desplazó una lista con el dedo, se arrastró el Mapa): vuelve a
        /// leer los marcos de todos los nodos para que el lector toque y lea lo que está ahora en cada sitio (observación 7 de la
        /// jornada 6). No mueve el foco, salvo que el elemento enfocado sea una fila que el desplazamiento sacó del visor: entonces
        /// pasa a la primera fila de su lista que se ve, para que el orden del foco siga lo que se ve.
        /// </summary>
        void RefreshFrames();
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

        /// <summary>Veces que se pidió volver a leer los marcos (<see cref="RefreshFrames"/>).</summary>
        public int FrameRefreshes { get; private set; }

        /// <inheritdoc />
        public void RefreshFrames()
        {
            FrameRefreshes++;
        }

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
    /// Los nodos de una lista que se desplaza (<see cref="A11yItem.Scroll"/>; Q-B9-17) llevan además la acción de desplazar
    /// (<see cref="AccessibilityNode.scrolled"/>: una página, y el foco pasa a la primera fila que se ve entera) y, al recibir el
    /// foco (<see cref="AccessibilityNode.focusChanged"/>), desplazan el visor para enseñarse; su marco es lo que se ve de ellos
    /// en el visor (<see cref="A11yScroll.Frame"/>). Unity guarda el marco de cada nodo y solo lo relee con
    /// <see cref="AccessibilityHierarchy.RefreshNodeFrames"/>: se relee al desplazar con el lector y cuando
    /// <see cref="A11yService"/> ve que lo publicado se movió (<see cref="RefreshFrames"/>; observación 7 de la jornada 6).
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
            if (_hierarchy != null && SameShape(items))
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
                var nodeRef = new NodeRef(this, hierarchy, node, item);
                _nodes.Add(node);
                _current.Add(item);
                _refs.Add(nodeRef);
                Apply(node, item);
                node.frameGetter = nodeRef.Frame;
                node.invoked += nodeRef.Invoke;
                if (item.Scroll != null)
                {
                    // Q-B9-17: la fila de una lista que se desplaza se desplaza con el lector y se enseña al enfocarla.
                    node.scrolled += nodeRef.Scroll;
                    node.focusChanged += nodeRef.FocusChanged;
                }
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
            private readonly UnityScreenReaderHost _host;
            private readonly AccessibilityHierarchy _hierarchy;

            public NodeRef(UnityScreenReaderHost host, AccessibilityHierarchy hierarchy, AccessibilityNode node, A11yItem item)
            {
                _host = host;
                _hierarchy = hierarchy;
                Node = node;
                Item = item;
            }

            public AccessibilityNode Node { get; }

            public A11yItem Item { get; set; }

            public bool Live => ReferenceEquals(AssistiveSupport.activeHierarchy, _hierarchy);

            // El marco del elemento de ahora (el de una publicación en sitio, no el de cuando se creó el nodo); en una lista que se
            // desplaza, lo que se ve de él en el visor.
            public Rect Frame()
            {
                if (Item.Rect == null)
                {
                    return Rect.zero;
                }

                return Item.Scroll != null ? A11yScroll.Frame(Item.Scroll!, Item.Rect!) : ScreenFrame(Item.Rect!);
            }

            // Activar: solo si su jerarquía es la activa (un nodo de una jerarquía vieja no hace nada).
            public bool Invoke()
            {
                return Live && Item.Invoke != null && Item.Invoke!();
            }

            // La acción de desplazar del lector (Q-B9-17): una página de su lista; si se movió, el foco pasa a la primera fila que se
            // ve entera y el lector vuelve a leer los marcos.
            public bool Scroll(AccessibilityScrollDirection direction)
            {
                if (!Live || Item.Scroll == null || !A11yScroll.Page(Item.Scroll!, direction))
                {
                    return false;
                }

                _hierarchy.RefreshNodeFrames(); // observación 7 de la jornada 6: los marcos de después de desplazar
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(_host.FirstShownIn(Item.Scroll!));
                return true;
            }

            // El foco del lector llegó a la fila: si el visor la recorta, se desplaza para enseñarla y el lector relee su marco.
            public void FocusChanged(AccessibilityNode node, bool focused)
            {
                if (focused && Reveal())
                {
                    _hierarchy.RefreshNodeFrames(); // la lista se movió: todos los marcos de la jerarquía, no solo el suyo
                    AssistiveSupport.notificationDispatcher.SendLayoutChanged(node);
                }
            }

            public bool Reveal()
            {
                return Live && Item.Scroll != null && Item.Rect != null && A11yScroll.Reveal(Item.Scroll!, Item.Rect!);
            }
        }

        // El nodo de la primera fila de list que se ve entera (null si ninguna).
        private AccessibilityNode? FirstShownIn(ScrollRect list)
        {
            int index = FirstShownIndexIn(list, true);
            return index >= 0 ? _refs[index].Node : null;
        }

        // El índice de la primera fila de list que se ve entera (whole) o, sin whole, aunque sea en parte; −1 si ninguna.
        private int FirstShownIndexIn(ScrollRect list, bool whole)
        {
            for (int i = 0; i < _refs.Count; i++)
            {
                A11yItem item = _refs[i].Item;
                if (ReferenceEquals(item.Scroll, list) && item.Rect != null
                    && (whole ? A11yScroll.FullyShown(list, item.Rect!) : A11yScroll.PartlyShown(list, item.Rect!)))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <inheritdoc />
        public void RefreshFrames()
        {
            if (_hierarchy == null || !ReferenceEquals(AssistiveSupport.activeHierarchy, _hierarchy))
            {
                return;
            }

            // Observación 7 de la jornada 6: Unity guarda el marco de cada nodo al crearlo (AccessibilityNode.frame) y solo lo
            // vuelve a pedir a frameGetter con RefreshNodeFrames; sin esto, tras desplazar la Liga con el dedo, TalkBack tocaba y
            // leía la fila que había antes en ese sitio. RefreshNodeFrames ya avisa al lector (SendLayoutChanged sin mover el foco).
            _hierarchy.RefreshNodeFrames();
            int focused = -1;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].isFocused)
                {
                    focused = i;
                    break;
                }
            }

            int target = RefocusAfterMove(focused);
            if (target >= 0 && target != focused)
            {
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(_nodes[target]);
            }
        }

        /// <summary>
        /// A dónde va el foco tras moverse la interfaz con el nodo <paramref name="focused"/> enfocado (−1: ninguno): se queda donde
        /// está, salvo que sea una fila de una lista que se desplaza y ya no se vea nada de ella; entonces, a la primera fila de esa
        /// lista que se ve entera (o, si no hay, en parte), para que el siguiente gesto siga el orden que se ve. −1 si ninguno.
        /// </summary>
        internal int RefocusAfterMove(int focused)
        {
            if (focused < 0 || focused >= _refs.Count)
            {
                return -1;
            }

            A11yItem item = _refs[focused].Item;
            if (item.Scroll == null || item.Rect == null || A11yScroll.PartlyShown(item.Scroll!, item.Rect!))
            {
                return focused;
            }

            int first = FirstShownIndexIn(item.Scroll!, true);
            if (first < 0)
            {
                first = FirstShownIndexIn(item.Scroll!, false);
            }

            return first >= 0 ? first : focused;
        }

        /// <summary>El marco que tiene guardado el nodo <paramref name="index"/>: el que usa el lector para tocar y dibujar el foco (tests).</summary>
        internal Rect NodeStoredFrame(int index)
        {
            return _nodes[index].frame;
        }

        /// <summary>Lo que lee el nodo <paramref name="index"/> (tests).</summary>
        internal string NodeLabel(int index)
        {
            return _nodes[index].label;
        }

        /// <summary>Nodos publicados (tests).</summary>
        internal int NodeCount => _nodes.Count;

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

        /// <summary>La acción de desplazar del nodo <paramref name="index"/> (tests de Q-B9-17).</summary>
        internal Func<AccessibilityScrollDirection, bool> NodeScroller(int index)
        {
            return _refs[index].Scroll;
        }

        /// <summary>Lo que hace el nodo <paramref name="index"/> al recibir el foco: si su lista lo recorta, se enseña (tests de Q-B9-17).</summary>
        internal Func<bool> NodeReveal(int index)
        {
            return _refs[index].Reveal;
        }

        private static void Apply(AccessibilityNode node, A11yItem item)
        {
            node.label = item.Label;
            node.role = item.Role;
            node.state = item.Disabled ? AccessibilityState.Disabled : item.Selected ? AccessibilityState.Selected : AccessibilityState.None;
        }

        // Mismos elementos en el mismo orden y, cada uno, dentro o fuera de una lista que se desplaza como antes (las acciones de
        // desplazar de un nodo se ponen al crearlo: si una lista empieza o deja de desplazarse, se rehace la jerarquía).
        private bool SameShape(IReadOnlyList<A11yItem> items)
        {
            if (items.Count != _current.Count)
            {
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (!string.Equals(items[i].Id, _current[i].Id, StringComparison.Ordinal) || (items[i].Scroll == null) != (_current[i].Scroll == null))
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
