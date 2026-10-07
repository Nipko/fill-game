#nullable enable
using System;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Catálogo de los PNG pintados presentes en <c>Assets/_Project/Art/Generated/</c>. Lo crea y actualiza el Editor
    /// (<c>AF.EditorTools.GeneratedArt</c>); nunca se edita a mano (CLAUDE.md, regla dura 3).
    /// </summary>
    public sealed class ArtSlotCatalog : ScriptableObject
    {
        [SerializeField]
        private Entry[] entries = Array.Empty<Entry>();

        /// <summary>Entradas (ranura y sprite).</summary>
        public Entry[] Entries => entries;

        /// <summary>Sustituye las entradas (solo el Editor).</summary>
        public void SetEntries(Entry[] value)
        {
            entries = value ?? Array.Empty<Entry>();
        }

        /// <summary>Sprite de la ranura <paramref name="slot"/> o null.</summary>
        public Sprite? Find(string slot)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (string.Equals(entries[i].slot, slot, StringComparison.Ordinal) && entries[i].sprite != null)
                {
                    return entries[i].sprite;
                }
            }

            return null;
        }

        /// <summary>Una ranura con su sprite.</summary>
        [Serializable]
        public struct Entry
        {
            /// <summary>Nombre de la ranura (nombre del PNG sin extensión).</summary>
            public string slot;

            /// <summary>Sprite importado.</summary>
            public Sprite sprite;
        }
    }
}
