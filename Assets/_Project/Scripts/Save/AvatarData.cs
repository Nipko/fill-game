#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Save
{
    /// <summary>
    /// <c>avatar</c> [P] (D-056; <c>docs/dev/avatares.md</c> §4.1; 08 §4.3): el avatar del jugador,
    /// <c>{id, chosen, at, unlocked[], seen[]}</c>. Entra sin migración, como <c>map_seen_level</c> (el esquema aún no se ha
    /// publicado): sin <see cref="Id"/> el bloque no se escribe y un perfil anterior se lee con el default. AF.Save no conoce el
    /// catálogo (<c>AF.LiveOps.AvatarCatalog</c>): solo comprueba la forma de los id y guarda; las reglas (el default, los hitos, la
    /// elección) son de <c>AF.LiveOps.AvatarService</c>. Las listas van en orden ordinal y sin repetidos (la firma de B8 no depende
    /// del orden) y nunca encogen.
    /// </summary>
    public sealed class AvatarData
    {
        /// <summary>Largo máximo de un id (forma <c>[a-z0-9_]{1,32}</c>).</summary>
        public const int MaxIdLength = 32;

        private readonly List<string> _unlocked = new List<string>();
        private readonly List<string> _seen = new List<string>();

        /// <summary><c>id</c>: el avatar elegido, o null si aún no hay ninguno (perfil anterior a D-056 o recién creado).</summary>
        public string? Id { get; set; }

        /// <summary><c>chosen</c>: el jugador eligió alguna vez (false con el asignado al azar del primer arranque, Q-AV-2).</summary>
        public bool Chosen { get; set; }

        /// <summary><c>at</c>: el instante (UTC) de la última elección, para la fusión con la nube; null si nunca eligió.</summary>
        public DateTime? At { get; set; }

        /// <summary><c>unlocked</c>: los avatares de hito ya desbloqueados (los libres no se apuntan), en orden ordinal.</summary>
        public IReadOnlyList<string> Unlocked => _unlocked;

        /// <summary><c>seen</c>: los desbloqueados que el jugador ya vio en «Tu avatar», en orden ordinal.</summary>
        public IReadOnlyList<string> Seen => _seen;

        /// <summary>Sin nada que guardar: el bloque no se escribe.</summary>
        public bool IsEmpty => Id == null && !Chosen && At == null && _unlocked.Count == 0 && _seen.Count == 0;

        /// <summary><paramref name="id"/> tiene la forma de un id de avatar (<c>[a-z0-9_]{1,32}</c>).</summary>
        public static bool IsId(string? id)
        {
            if (string.IsNullOrEmpty(id) || id!.Length > MaxIdLength)
            {
                return false;
            }

            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>¿Está <paramref name="id"/> en <see cref="Unlocked"/>?</summary>
        public bool IsUnlocked(string id) => _unlocked.BinarySearch(id, StringComparer.Ordinal) >= 0;

        /// <summary>¿Está <paramref name="id"/> en <see cref="Seen"/>?</summary>
        public bool IsSeen(string id) => _seen.BinarySearch(id, StringComparer.Ordinal) >= 0;

        /// <summary>Apunta <paramref name="id"/> como desbloqueado: true la primera vez.</summary>
        public bool AddUnlocked(string id) => Insert(_unlocked, id);

        /// <summary>Apunta <paramref name="id"/> como visto: true la primera vez.</summary>
        public bool AddSeen(string id) => Insert(_seen, id);

        /// <summary>Vacía el bloque (lectura de un archivo y borrado de cuenta).</summary>
        public void Clear()
        {
            Id = null;
            Chosen = false;
            At = null;
            _unlocked.Clear();
            _seen.Clear();
        }

        /// <summary>
        /// Fusión de 08 §11 con D-056 (avatares.md §4.1) sobre este bloque (la copia del ganador): <c>unlocked</c> y <c>seen</c> por
        /// unión, <c>chosen</c> por «o» y el par <c>(id, at)</c> del de <c>at</c> más reciente (la elección es una preferencia: gana
        /// la última, no la del blob con más nivel); sin <c>at</c> en ninguno, el del ganador (este), y si este no tiene id, el otro.
        /// Reevaluar los hitos y validar el id contra el catálogo lo hace el cliente después (<c>AF.LiveOps.AvatarService</c>).
        /// </summary>
        /// <param name="other">El bloque del otro perfil (el perdedor).</param>
        public void Merge(AvatarData other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            for (int i = 0; i < other._unlocked.Count; i++)
            {
                AddUnlocked(other._unlocked[i]);
            }

            for (int i = 0; i < other._seen.Count; i++)
            {
                AddSeen(other._seen[i]);
            }

            Chosen = Chosen || other.Chosen;
            bool otherNewer = other.At.HasValue && (!At.HasValue || other.At.Value > At.Value);
            if (otherNewer || (Id == null && other.Id != null && !At.HasValue))
            {
                Id = other.Id;
                At = other.At;
            }
        }

        private static bool Insert(List<string> list, string id)
        {
            if (!IsId(id))
            {
                throw new ArgumentException("Id de avatar con otra forma ([a-z0-9_]{1,32}).", nameof(id));
            }

            int i = list.BinarySearch(id, StringComparer.Ordinal);
            if (i >= 0)
            {
                return false;
            }

            list.Insert(~i, id);
            return true;
        }
    }
}
