#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Save
{
    /// <summary>
    /// <c>avatar</c> [P] (D-056; <c>docs/dev/avatares.md</c> §4.1; 08 §4.3): el avatar del jugador,
    /// <c>{id, chosen, at, unlocked[], seen[]}</c> y, desde la 1.1 (Q-M6, añadido del 2026-11-08) [P], el marco de país que lleva,
    /// <c>{frame, frame_at, frames_seen[]}</c>, que se omiten en su default (un perfil de la 1.0 se lee igual). Entra sin migración, como <c>map_seen_level</c> (el esquema aún no se ha
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
        private readonly List<string> _framesSeen = new List<string>();

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

        /// <summary>
        /// <c>frame</c> [P] (Q-M6 de D-033, añadido del 2026-11-08): el marco de país que lleva el avatar (<c>frame_wNN</c>, uno de
        /// <c>entitlements.frames</c>), o null sin marco. Solo cuenta con <see cref="FrameAt"/>: sin él, el jugador nunca decidió y
        /// el marco que se ve lo deduce <c>AF.LiveOps.AvatarFrameService</c> (el del mundo más alto ganado).
        /// </summary>
        public string? Frame { get; set; }

        /// <summary>
        /// <c>frame_at</c> [P]: el instante (UTC) de la última decisión sobre el marco (ponerlo al ganarlo, elegir otro o quitarlo), para
        /// la fusión con la nube; null si nunca se decidió (un perfil de la 1.0).
        /// </summary>
        public DateTime? FrameAt { get; set; }

        /// <summary><c>frames_seen</c> [P]: los marcos de país que el jugador ya vio (en la libreta o en «Tu avatar»), en orden ordinal.</summary>
        public IReadOnlyList<string> FramesSeen => _framesSeen;

        /// <summary>Sin nada que guardar: el bloque no se escribe.</summary>
        public bool IsEmpty => Id == null && !Chosen && At == null && _unlocked.Count == 0 && _seen.Count == 0
            && Frame == null && FrameAt == null && _framesSeen.Count == 0;

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

        /// <summary>¿Está el marco <paramref name="id"/> en <see cref="FramesSeen"/>?</summary>
        public bool IsFrameSeen(string id) => _framesSeen.BinarySearch(id, StringComparer.Ordinal) >= 0;

        /// <summary>Apunta el marco <paramref name="id"/> como visto: true la primera vez.</summary>
        public bool AddFrameSeen(string id) => Insert(_framesSeen, id);

        /// <summary>Vacía el bloque (lectura de un archivo y borrado de cuenta).</summary>
        public void Clear()
        {
            Id = null;
            Chosen = false;
            At = null;
            _unlocked.Clear();
            _seen.Clear();
            Frame = null;
            FrameAt = null;
            _framesSeen.Clear();
        }

        /// <summary>
        /// Fusión de 08 §11 con D-056 (avatares.md §4.1) sobre este bloque (la copia del ganador): <c>unlocked</c> y <c>seen</c> por
        /// unión, <c>chosen</c> por «o» y el par <c>(id, at)</c> del de <c>at</c> más reciente (la elección es una preferencia: gana
        /// la última, no la del blob con más nivel); sin <c>at</c> en ninguno, el del ganador (este), y si este no tiene id, el otro.
        /// Reevaluar los hitos y validar el id contra el catálogo lo hace el cliente después (<c>AF.LiveOps.AvatarService</c>).
        /// El marco (Q-M6, añadido del 2026-11-08) [P], igual: <c>frames_seen</c> por unión y el par <c>(frame, frame_at)</c> del de
        /// <c>frame_at</c> más reciente (sin <c>frame_at</c> en ninguno, nada que elegir); que el marco sea del jugador lo comprueba el
        /// cliente con <c>entitlements.frames</c> (ya unidos).
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

            for (int i = 0; i < other._framesSeen.Count; i++)
            {
                AddFrameSeen(other._framesSeen[i]);
            }

            if (other.FrameAt.HasValue && (!FrameAt.HasValue || other.FrameAt.Value > FrameAt.Value))
            {
                Frame = other.Frame;
                FrameAt = other.FrameAt;
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
