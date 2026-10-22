#nullable enable
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Etiquetas del lector de pantalla puestas a mano (bloque B9; 02 §12: «todo botón lleva <c>contentDescription</c> con
    /// etiqueta funcional + estado»): para los botones sin texto (la X de cerrar, el engranaje, la pausa) o cuyo texto no basta.
    /// Quien construye el botón registra una función que devuelve la etiqueta de ahora (con su estado) y, si es una opción que se
    /// marca, si está marcada. <see cref="A11yScanner"/> la usa en lugar de los textos del botón. Se guarda con una referencia
    /// débil al objeto: al destruirlo, su entrada se va sola.
    /// </summary>
    public static class A11yLabels
    {
        private static readonly ConditionalWeakTable<GameObject, Entry> Table = new ConditionalWeakTable<GameObject, Entry>();

        /// <summary>
        /// Registra la etiqueta <paramref name="label"/> (y, si es una opción que se marca, <paramref name="selected"/>) para el
        /// objeto de <paramref name="target"/>; sustituye la anterior.
        /// </summary>
        public static void Set(Component target, Func<string> label, Func<bool>? selected = null)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            GameObject go = target.gameObject;
            Table.Remove(go);
            Table.Add(go, new Entry(label ?? throw new ArgumentNullException(nameof(label)), selected));
        }

        /// <summary>Quita la etiqueta registrada para el objeto de <paramref name="target"/>.</summary>
        public static void Remove(Component target)
        {
            if (target != null)
            {
                Table.Remove(target.gameObject);
            }
        }

        /// <summary>La etiqueta registrada para <paramref name="go"/> y si está marcada; false si no hay.</summary>
        public static bool TryGet(GameObject go, out string label, out bool selected)
        {
            label = string.Empty;
            selected = false;
            if (go == null || !Table.TryGetValue(go, out Entry entry))
            {
                return false;
            }

            label = entry.Label() ?? string.Empty;
            selected = entry.Selected != null && entry.Selected();
            return true;
        }

        /// <summary>Si la opción registrada para <paramref name="go"/> está marcada (sin calcular su etiqueta); false si no hay.</summary>
        public static bool SelectedOf(GameObject go)
        {
            return go != null && Table.TryGetValue(go, out Entry entry) && entry.Selected != null && entry.Selected();
        }

        private sealed class Entry
        {
            public Entry(Func<string> label, Func<bool>? selected)
            {
                Label = label;
                Selected = selected;
            }

            public Func<string> Label { get; }

            public Func<bool>? Selected { get; }
        }
    }
}
