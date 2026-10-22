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
    /// ya en el manifiesto: sin paquete nuevo). Cada publicación rehace un <see cref="AccessibilityHierarchy"/> plano, con un nodo
    /// por elemento en el orden del foco, y lo activa (<see cref="AssistiveSupport.activeHierarchy"/>); el marco de cada nodo se
    /// calcula de su <see cref="RectTransform"/> cuando el lector lo pide (<see cref="AccessibilityNode.frameGetter"/>); activarlo
    /// (doble toque) llama a <see cref="A11yItem.Invoke"/>; los anuncios van por
    /// <see cref="AssistiveSupport.notificationDispatcher"/>. El origen de coordenadas del marco es la esquina de arriba a la
    /// izquierda de la pantalla [NV]: hay que comprobarlo en el teléfono con TalkBack (el recuadro del foco sobre cada cuia).
    /// </summary>
    public sealed class UnityScreenReaderHost : IScreenReaderHost
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        private AccessibilityHierarchy? _hierarchy;

        /// <summary>Lector forzado por un test o un build de desarrollo (null: el del sistema).</summary>
        public static bool? ScreenReaderOverride { get; set; }

        /// <inheritdoc />
        public bool ScreenReaderOn => ScreenReaderOverride ?? AssistiveSupport.isScreenReaderEnabled;

        /// <inheritdoc />
        public void Publish(IReadOnlyList<A11yItem> items)
        {
            var hierarchy = new AccessibilityHierarchy();
            for (int i = 0; i < items.Count; i++)
            {
                A11yItem item = items[i];
                AccessibilityNode node = hierarchy.AddNode(item.Label, null);
                node.role = item.Role;
                node.state = item.Disabled ? AccessibilityState.Disabled : item.Selected ? AccessibilityState.Selected : AccessibilityState.None;
                RectTransform? rect = item.Rect;
                if (rect != null)
                {
                    node.frameGetter = () => ScreenFrame(rect);
                }

                Func<bool>? invoke = item.Invoke;
                if (invoke != null)
                {
                    node.invoked += () => invoke();
                }
            }

            _hierarchy = hierarchy;
            AssistiveSupport.activeHierarchy = hierarchy;
            AssistiveSupport.notificationDispatcher.SendLayoutChanged(null);
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_hierarchy != null && ReferenceEquals(AssistiveSupport.activeHierarchy, _hierarchy))
            {
                AssistiveSupport.activeHierarchy = null;
            }

            _hierarchy = null;
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
