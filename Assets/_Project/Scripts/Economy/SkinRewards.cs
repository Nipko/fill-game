#nullable enable
using System;
using System.Collections.Generic;
using AF.Save;

namespace AF.Economy
{
    /// <summary>
    /// Una skin de cuia por hito (06 §4.5; 07 §2.2 y §4.6; glosario §4): la de un mundo se gana al completar su último nivel.
    /// </summary>
    public readonly struct SkinMilestone
    {
        /// <summary>Crea la fila.</summary>
        public SkinMilestone(string skinId, int world, int level)
        {
            SkinId = skinId;
            World = world;
            Level = level;
        }

        /// <summary>Id de la skin: el SKU de su pack (<c>af_skin_m02</c>…; 05 §4.1) [P].</summary>
        public string SkinId { get; }

        /// <summary>Mundo de la skin (glosario §8).</summary>
        public int World { get; }

        /// <summary>Último nivel del mundo: al ganarlo se gana la skin (06 §4.5: 40, 80 y 140).</summary>
        public int Level { get; }
    }

    /// <summary>
    /// Reglas de las skins por hito de la 1.0 (06 §4.5: «Cuia de Santarém al 40, Cuia del Encuentro al 80, Totuma al 140»; en 1.1
    /// Huingo al 200, etc.; 07 §4.6: las cuatro primeras van en la 1.0, la base incluida), sin estado ni motor. El id de cada skin es
    /// el SKU de su pack (<c>af_skin_m02</c> … <c>af_skin_m04</c>, 05 §4.1; glosario §11), el mismo que ya anota la compra en
    /// <c>entitlements.skins</c>: el GDD aún no fija otros IDs de skin (pregunta B14-Q1 [P]); la base es <c>cuia_vidrio</c>
    /// (08 §4.3). Las skins no cambian la jugabilidad (glosario §4).
    /// </summary>
    public static class SkinRewards
    {
        /// <summary>La skin base, gratis y siempre del jugador (08 §4.3; 07 §4.6: Cuia de vidrio).</summary>
        public const string BaseSkinId = "cuia_vidrio";

        /// <summary><c>source</c> de <c>af_skin_unlock</c> para una skin ganada por hito (08 §8.2).</summary>
        public const string SourceWorldMilestone = "world_milestone";

        private static readonly SkinMilestone[] Table =
        {
            new SkinMilestone("af_skin_m02", 2, 40),
            new SkinMilestone("af_skin_m03", 3, 80),
            new SkinMilestone("af_skin_m04", 4, 140),
        };

        /// <summary>Las skins por hito de la 1.0, en orden de nivel.</summary>
        public static IReadOnlyList<SkinMilestone> Milestones => Table;

        /// <summary>La skin que se gana al superar <paramref name="level"/>, o null si ese nivel no cierra un mundo con skin.</summary>
        public static string? SkinEarnedAt(int level)
        {
            for (int i = 0; i < Table.Length; i++)
            {
                if (Table[i].Level == level)
                {
                    return Table[i].SkinId;
                }
            }

            return null;
        }

        /// <summary>La fila del mundo <paramref name="world"/>; false si ese mundo no tiene skin por hito en la 1.0.</summary>
        public static bool TryForWorld(int world, out SkinMilestone milestone)
        {
            for (int i = 0; i < Table.Length; i++)
            {
                if (Table[i].World == world)
                {
                    milestone = Table[i];
                    return true;
                }
            }

            milestone = default;
            return false;
        }

        /// <summary>true si <paramref name="skinId"/> es la base o una de <see cref="Milestones"/>.</summary>
        public static bool IsKnown(string? skinId)
        {
            if (skinId == null)
            {
                return false;
            }

            if (string.Equals(skinId, BaseSkinId, StringComparison.Ordinal))
            {
                return true;
            }

            for (int i = 0; i < Table.Length; i++)
            {
                if (string.Equals(Table[i].SkinId, skinId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Una skin ganada por hito en una victoria (<c>af_skin_unlock</c>, 08 §8.2).</summary>
    public sealed class SkinUnlock
    {
        /// <summary>Crea el resultado.</summary>
        public SkinUnlock(string skinId, int level, bool equipped)
        {
            SkinId = skinId;
            Level = level;
            Equipped = equipped;
        }

        /// <summary><c>skin_id</c>.</summary>
        public string SkinId { get; }

        /// <summary>Nivel cuya victoria la dio.</summary>
        public int Level { get; }

        /// <summary><c>source</c>: siempre <see cref="SkinRewards.SourceWorldMilestone"/>.</summary>
        public string Source => SkinRewards.SourceWorldMilestone;

        /// <summary>Quedó puesta (Q-M8 (a): sí, siempre que se gana).</summary>
        public bool Equipped { get; }
    }

    /// <summary>
    /// Skins del jugador sobre el <see cref="Profile"/> (06 §4.5; D-033, Q-M8 (a): «la skin ganada se pone sola, con aviso, y se
    /// cambia en el Mercado»): la que llevan las cuias (<see cref="Equipped"/>), ganarlas al cerrar un mundo
    /// (<see cref="ApplyLevelPassed"/>), recuperar las de los mundos ya cerrados (<see cref="Backfill"/>) y la elección
    /// (<see cref="Select"/>), que usará la pestaña Cuias del Mercado (B5; 06 §4.7). Guarda con el <see cref="IProfileStore"/> que
    /// recibe (en el juego, la vía síncrona del lote, como la cartera: el derecho y la victoria salen en la misma escritura). Como
    /// <see cref="ProfileStreakStore"/>, un fallo de escritura no lanza: el cambio queda en el perfil en memoria
    /// (<see cref="PendingSave"/>).
    /// </summary>
    public sealed class SkinService
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;

        /// <summary>Crea el servicio sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public SkinService(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>El último guardado falló: el cambio está en el perfil en memoria y sale con el siguiente.</summary>
        public bool PendingSave { get; private set; }

        /// <summary>
        /// La skin que llevan las cuias: <c>entitlements.skin_equipped</c> si el jugador la tiene y es conocida; si no, la base.
        /// </summary>
        public string Equipped
        {
            get
            {
                string? id = _profile.Entitlements.SkinEquipped;
                return id != null && SkinRewards.IsKnown(id) && Owns(id) ? id : SkinRewards.BaseSkinId;
            }
        }

        /// <summary>true si el jugador tiene <paramref name="skinId"/> (la base, siempre).</summary>
        public bool Owns(string skinId)
        {
            if (skinId == null)
            {
                throw new ArgumentNullException(nameof(skinId));
            }

            return string.Equals(skinId, SkinRewards.BaseSkinId, StringComparison.Ordinal) || _profile.Entitlements.Skins.Contains(skinId);
        }

        /// <summary>
        /// Superado el nivel de campaña <paramref name="level"/>: si cierra un mundo con skin (<see cref="SkinRewards.SkinEarnedAt"/>)
        /// y el jugador aún no la tiene, la gana y se la pone (Q-M8 (a)), y se guarda. Devuelve lo ganado, o null (el Reto del río no
        /// da skins; ya la tenía, comprada o ganada: nada cambia).
        /// </summary>
        public SkinUnlock? ApplyLevelPassed(int level, bool isCampaign)
        {
            string? skin = isCampaign ? SkinRewards.SkinEarnedAt(level) : null;
            if (skin == null || Owns(skin))
            {
                return null;
            }

            _profile.Entitlements.Skins.Add(skin);
            _profile.Entitlements.SkinEquipped = skin;
            Persist();
            return new SkinUnlock(skin, level, true);
        }

        /// <summary>
        /// Da las skins de los mundos ya cerrados con <paramref name="maxLevel"/> que falten (un perfil anterior a B14), sin
        /// ponerlas ni avisar [P]: el aviso es de la victoria. Guarda si cambió algo y devuelve cuántas dio.
        /// </summary>
        public int Backfill(int maxLevel)
        {
            int added = 0;
            IReadOnlyList<SkinMilestone> table = SkinRewards.Milestones;
            for (int i = 0; i < table.Count; i++)
            {
                if (maxLevel >= table[i].Level && !Owns(table[i].SkinId))
                {
                    _profile.Entitlements.Skins.Add(table[i].SkinId);
                    added++;
                }
            }

            if (added > 0)
            {
                Persist();
            }

            return added;
        }

        /// <summary>
        /// Pone <paramref name="skinId"/> (la elección del Mercado, B5): true si el jugador la tiene y es conocida (la base deja
        /// <c>skin_equipped</c> en null); false si no, sin cambiar nada. Guarda si cambió.
        /// </summary>
        public bool Select(string skinId)
        {
            if (skinId == null || !SkinRewards.IsKnown(skinId) || !Owns(skinId))
            {
                return false;
            }

            string? value = string.Equals(skinId, SkinRewards.BaseSkinId, StringComparison.Ordinal) ? null : skinId;
            if (!string.Equals(_profile.Entitlements.SkinEquipped, value, StringComparison.Ordinal))
            {
                _profile.Entitlements.SkinEquipped = value;
                Persist();
            }

            return true;
        }

        private void Persist()
        {
            PendingSave = !_store.TrySave(_profile);
        }
    }
}
