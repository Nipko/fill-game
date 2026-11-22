#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Platform;
using AF.Save;

namespace AF.LiveOps
{
    /// <summary>Un marco de país (Q-M6 de D-033, añadido del 2026-11-08): su id, su mundo y el país de su bandera.</summary>
    public sealed class AvatarFrameInfo
    {
        internal AvatarFrameInfo(int world, string? country)
        {
            World = world;
            Id = LoginCalendarRules.FrameId(world);
            Country = country;
        }

        /// <summary><c>frame_wNN</c> (Q-B6), el id de <c>entitlements.frames</c>.</summary>
        public string Id { get; }

        /// <summary>El mundo (1-13, glosario §8).</summary>
        public int World { get; }

        /// <summary>
        /// El país de la bandera de la placa (ISO 3166-1 alfa-2, glosario §8, columna «País»), o null en los mundos de varios países
        /// (10-12), que llevan el marco sin placa [P].
        /// </summary>
        public string? Country { get; }
    }

    /// <summary>Lo que pasó al tocar un marco en «Tu avatar».</summary>
    public enum AvatarFrameSelectResult
    {
        /// <summary>Se puso ese marco (o se quitó, con «Sin marco»).</summary>
        Selected = 0,

        /// <summary>Ya era el que llevaba: nada cambia.</summary>
        AlreadyChosen = 1,

        /// <summary>No es un marco del jugador.</summary>
        NotOwned = 2,
    }

    /// <summary>
    /// Los marcos de país del avatar (Q-M6 de D-033, «se guarda como derecho y se enseña en la 1.1», con su añadido del 2026-11-08),
    /// sin motor. Se ganan en la casilla 30 de la Bitácora (06 §4.2, regla 4; Q-B6: <c>frame_wNN</c>, el del mundo en que está el
    /// jugador) y viven en <c>entitlements.frames</c>; el que lleva el avatar, en <c>avatar.frame</c> [P]:
    /// <list type="bullet">
    /// <item>al ganar uno, se pone solo (como la skin ganada, Q-M8 (a)) y la libreta lo enseña ya puesto;</item>
    /// <item>en «Tu avatar» se elige entre los ganados o «Sin marco», sin confirmar;</item>
    /// <item>sin decisión guardada (un perfil de la 1.0 con marcos), lleva el del mundo más alto ganado;</item>
    /// <item>un marco sin ver (ni en la libreta ni en «Tu avatar») enciende el punto del retrato del inicio.</item>
    /// </list>
    /// Es cosmético: sin compras, sin precios y sin cambios en la economía (05).
    /// </summary>
    public static class AvatarFrames
    {
        /// <summary>Mundos con marco (los 13 de la primera vuelta, glosario §8; la Bitácora da el del mundo de <c>max_level</c> + 1).</summary>
        public const int Count = 13;

        // El país de cada mundo, de la columna «País» del glosario §8 (índice 0 = mundo 1). Null: varios países, sin placa [P].
        private static readonly string?[] Countries = { "BR", "BR", "BR", "CO", "PE", "EC", "PE", "BO", "PE", null, null, null, "PE" };

        private static readonly AvatarFrameInfo[] AllFrames = BuildAll();

        /// <summary>Los 13 marcos, por mundo.</summary>
        public static IReadOnlyList<AvatarFrameInfo> All => AllFrames;

        /// <summary>El marco del mundo <paramref name="world"/> (1-13), o null.</summary>
        public static AvatarFrameInfo? OfWorld(int world) => world >= 1 && world <= Count ? AllFrames[world - 1] : null;

        /// <summary>El marco de id <paramref name="id"/> (<c>frame_wNN</c> con NN de 01 a 13), o null.</summary>
        public static AvatarFrameInfo? Find(string? id) => OfWorld(WorldOf(id));

        /// <summary>El mundo del id <paramref name="id"/> (<c>frame_w01</c> → 1), o 0 si no tiene esa forma.</summary>
        public static int WorldOf(string? id)
        {
            string prefix = LoginCalendarRules.FramePrefix;
            if (id == null || id.Length != prefix.Length + 2 || !id.StartsWith(prefix, StringComparison.Ordinal))
            {
                return 0;
            }

            return int.TryParse(id.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int world) ? world : 0;
        }

        /// <summary>
        /// Los marcos del jugador (<paramref name="owned"/>, <c>entitlements.frames</c>) que el juego conoce, por mundo y sin repetidos.
        /// </summary>
        public static List<AvatarFrameInfo> Owned(IReadOnlyList<string> owned)
        {
            if (owned == null)
            {
                throw new ArgumentNullException(nameof(owned));
            }

            var list = new List<AvatarFrameInfo>(owned.Count);
            for (int w = 1; w <= Count; w++)
            {
                AvatarFrameInfo frame = AllFrames[w - 1];
                if (Contains(owned, frame.Id))
                {
                    list.Add(frame);
                }
            }

            return list;
        }

        /// <summary>
        /// El marco que lleva el avatar: el elegido si es del jugador; «Sin marco» (null) si eligió quitarlo o el elegido ya no es suyo;
        /// sin decisión (<c>frame_at</c> null, un perfil de la 1.0), el del mundo más alto ganado [P].
        /// </summary>
        public static AvatarFrameInfo? Effective(AvatarData data, IReadOnlyList<string> owned)
        {
            if (data == null || owned == null)
            {
                throw new ArgumentNullException(data == null ? nameof(data) : nameof(owned));
            }

            if (data.FrameAt.HasValue)
            {
                return data.Frame != null && Contains(owned, data.Frame) ? Find(data.Frame) : null;
            }

            for (int w = Count; w >= 1; w--)
            {
                if (Contains(owned, AllFrames[w - 1].Id))
                {
                    return AllFrames[w - 1];
                }
            }

            return null;
        }

        /// <summary>Hay un marco del jugador sin ver (el punto del inicio y la chapa «Nuevo» de su celda).</summary>
        public static bool AnyUnseen(AvatarData data, IReadOnlyList<string> owned)
        {
            if (data == null || owned == null)
            {
                throw new ArgumentNullException(data == null ? nameof(data) : nameof(owned));
            }

            for (int i = 0; i < owned.Count; i++)
            {
                if (Find(owned[i]) != null && !data.IsFrameSeen(owned[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(IReadOnlyList<string> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static AvatarFrameInfo[] BuildAll()
        {
            var all = new AvatarFrameInfo[Count];
            for (int w = 1; w <= Count; w++)
            {
                all[w - 1] = new AvatarFrameInfo(w, Countries[w - 1]);
            }

            return all;
        }
    }

    /// <summary>
    /// El marco del avatar en el cliente (<see cref="AvatarFrames"/>), sin motor: sobre <c>avatar</c> (<see cref="AvatarData"/>) y
    /// <c>entitlements.frames</c> del mismo perfil. Guarda con <c>save</c> cada vez que algo cambia (el lote del perfil en el juego).
    /// </summary>
    public sealed class AvatarFrameService
    {
        private readonly AvatarData _data;
        private readonly List<string> _owned;
        private readonly Action _save;
        private readonly IClock _clock;

        /// <param name="data">El bloque <c>avatar</c>.</param>
        /// <param name="owned"><c>entitlements.frames</c> (la lista viva del perfil).</param>
        /// <param name="save">Guarda el perfil.</param>
        /// <param name="clock">Reloj (<c>frame_at</c>).</param>
        public AvatarFrameService(AvatarData data, List<string> owned, Action save, IClock clock)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _owned = owned ?? throw new ArgumentNullException(nameof(owned));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>El bloque <c>avatar</c>.</summary>
        public AvatarData Data => _data;

        /// <summary><c>entitlements.frames</c>.</summary>
        public IReadOnlyList<string> OwnedIds => _owned;

        /// <summary>El marco que lleva el avatar, o null.</summary>
        public AvatarFrameInfo? Current => AvatarFrames.Effective(_data, _owned);

        /// <summary>Los marcos del jugador, por mundo.</summary>
        public List<AvatarFrameInfo> Owned() => AvatarFrames.Owned(_owned);

        /// <summary>Hay alguno sin ver.</summary>
        public bool AnyUnseen => AvatarFrames.AnyUnseen(_data, _owned);

        /// <summary>Cambió el marco puesto o lo visto (la UI vuelve a pintar).</summary>
        public event Action? Changed;

        /// <summary>
        /// Pone <paramref name="frameId"/> (null: «Sin marco») si es del jugador. Sin confirmar, como el avatar (avatares.md §3.2).
        /// </summary>
        public AvatarFrameSelectResult Select(string? frameId)
        {
            if (frameId != null && (AvatarFrames.Find(frameId) == null || !_owned.Contains(frameId)))
            {
                return AvatarFrameSelectResult.NotOwned;
            }

            AvatarFrameInfo? current = Current;
            if (string.Equals(current?.Id, frameId, StringComparison.Ordinal))
            {
                if (!_data.FrameAt.HasValue)
                {
                    // Confirmar el que se deducía también es una decisión: queda guardada.
                    _data.Frame = frameId;
                    _data.FrameAt = _clock.UtcNow;
                    _save();
                }

                return AvatarFrameSelectResult.AlreadyChosen;
            }

            _data.Frame = frameId;
            _data.FrameAt = _clock.UtcNow;
            _save();
            Changed?.Invoke();
            return AvatarFrameSelectResult.Selected;
        }

        /// <summary>
        /// Recién ganado <paramref name="frameId"/> (ya en <c>entitlements.frames</c>, lo apunta la Bitácora): se pone solo, como la
        /// skin ganada (Q-M8 (a)) [P]. False si no es un marco del jugador.
        /// </summary>
        public bool PutOnEarned(string frameId)
        {
            if (AvatarFrames.Find(frameId) == null || !_owned.Contains(frameId))
            {
                return false;
            }

            _data.Frame = frameId;
            _data.FrameAt = _clock.UtcNow;
            _save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>El jugador vio <paramref name="frameId"/> (en la libreta o en «Tu avatar»). True si es la primera vez.</summary>
        public bool MarkSeen(string frameId)
        {
            if (AvatarFrames.Find(frameId) == null || !_data.AddFrameSeen(frameId))
            {
                return false;
            }

            _save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Al cerrar «Tu avatar»: todos los del jugador pasan a vistos. Cuántos.</summary>
        public int MarkAllSeen()
        {
            int added = 0;
            for (int i = 0; i < _owned.Count; i++)
            {
                if (AvatarFrames.Find(_owned[i]) != null && _data.AddFrameSeen(_owned[i]))
                {
                    added++;
                }
            }

            if (added > 0)
            {
                _save();
                Changed?.Invoke();
            }

            return added;
        }

        /// <summary>
        /// Da el marco del mundo <paramref name="world"/> (solo el extra de desarrollo <c>af_frame_grant</c> y los tests): lo apunta en
        /// <c>entitlements.frames</c> si no estaba. True si lo añadió.
        /// </summary>
        public bool Grant(int world)
        {
            AvatarFrameInfo? frame = AvatarFrames.OfWorld(world);
            if (frame == null || _owned.Contains(frame.Id))
            {
                return false;
            }

            _owned.Add(frame.Id);
            _save();
            Changed?.Invoke();
            return true;
        }
    }
}
