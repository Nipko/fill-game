#nullable enable
using System;
using System.Collections.Generic;
using AF.Levels;

namespace AF.LiveOps
{
    /// <summary>
    /// El hito que desbloquea un avatar (avatares.md §1.2), con su id de analítica (<c>af_avatar_unlock.milestone</c>, 08 §8.2).
    /// <see cref="None"/> = libre desde el principio.
    /// </summary>
    public enum AvatarMilestone
    {
        /// <summary>Libre.</summary>
        None = 0,

        /// <summary><c>hard_win</c>: la primera victoria de un nivel Difícil (06 §3; D-009).</summary>
        HardWin = 1,

        /// <summary><c>very_hard_win</c>: la primera victoria de un nivel Muy difícil.</summary>
        VeryHardWin = 2,

        /// <summary><c>world_3</c>: gana el último nivel del mundo 3 (Manaos, el 80).</summary>
        World3 = 3,

        /// <summary><c>world_4</c>: el mundo 4 (Trapecio, el 140).</summary>
        World4 = 4,

        /// <summary><c>world_5</c>: el mundo 5 (Loreto, el 200).</summary>
        World5 = 5,

        /// <summary><c>world_6</c>: el mundo 6 (Oriente, el 300).</summary>
        World6 = 6,

        /// <summary><c>streak_10</c>: el hito 10 de la Crecida, el río crecido (06 §4.3; <c>streak.river_full</c>).</summary>
        Streak10 = 7,

        /// <summary><c>daily_streak_7</c>: racha de 7 días del Reto del río (06 §4.1).</summary>
        DailyStreak7 = 8,

        /// <summary><c>daily_stamps_20</c>: 20 sellos del Reto en un mes (06 §4.1, R-Q7).</summary>
        DailyStamps20 = 9,

        /// <summary><c>league_50</c>: 50 puntos de la Liga en una semana (06 §6, «Participante»).</summary>
        League50 = 10,
    }

    /// <summary>El fondo del retrato (avatares.md §1.2 y §2.1): una de las cuatro familias de la UI, nunca un tinte.</summary>
    public enum AvatarBackdrop
    {
        /// <summary>Hoja: #236A29 a #2F7D32.</summary>
        Leaf = 0,

        /// <summary>Agua: #0F6275 a #13788F.</summary>
        Water = 1,

        /// <summary>Madera: #46302A a #7E5638.</summary>
        Wood = 2,

        /// <summary>El petróleo de la marca (D-045).</summary>
        Petroleum = 3,
    }

    /// <summary>Un avatar del catálogo: su índice (lo que viaja al servidor), su id (lo que guarda el perfil), su nombre, su hito y su fondo.</summary>
    public sealed class AvatarInfo
    {
        internal AvatarInfo(int index, string id, AvatarMilestone milestone, AvatarBackdrop backdrop)
        {
            Index = index;
            Id = id;
            Milestone = milestone;
            Backdrop = backdrop;
            NameKey = "league_name_" + id;
        }

        /// <summary>El índice fijo (0-63; la lista solo crece por el final: nunca cambia de animal ni se reutiliza).</summary>
        public int Index { get; }

        /// <summary>El id (<c>avatar.id</c> del perfil y <c>af_avatar_*.avatar</c>).</summary>
        public string Id { get; }

        /// <summary>La clave del nombre: el del seudónimo de la Liga (<c>league_name_&lt;id&gt;</c>, avatares.md §1.1).</summary>
        public string NameKey { get; }

        /// <summary>El hito que lo desbloquea (<see cref="AvatarMilestone.None"/> si es libre).</summary>
        public AvatarMilestone Milestone { get; }

        /// <summary>Libre desde el principio.</summary>
        public bool Free => Milestone == AvatarMilestone.None;

        /// <summary>El fondo de su retrato.</summary>
        public AvatarBackdrop Backdrop { get; }
    }

    /// <summary>
    /// El catálogo de los avatares del jugador de la 1.0 (D-056; avatares.md §1.2, Q-AV-1 y Q-AV-7 con la recomendada, D-057): 16
    /// animales de la lista de la Liga, 6 libres y 10 por hito, en el orden de la tabla (primero los libres y después los de hito: la
    /// fila de los bloqueados se lee como un camino). Los hitos van en el código y no en Remote Config (§4.3: la condición de
    /// desbloqueo es visible y no cambia por servidor).
    /// </summary>
    public static class AvatarCatalog
    {
        /// <summary>El índice más alto que admite el contrato de la Liga (<c>avatar</c> 0-63, avatares.md §4.4).</summary>
        public const int MaxIndex = 63;

        /// <summary>Los libres desde el principio.</summary>
        public const int FreeCount = 6;

        private static readonly AvatarInfo[] Entries =
        {
            new AvatarInfo(0, "macaw", AvatarMilestone.None, AvatarBackdrop.Leaf),
            new AvatarInfo(1, "toucan", AvatarMilestone.None, AvatarBackdrop.Petroleum),
            new AvatarInfo(2, "tapir", AvatarMilestone.None, AvatarBackdrop.Water),
            new AvatarInfo(3, "turtle", AvatarMilestone.None, AvatarBackdrop.Wood),
            new AvatarInfo(4, "coati", AvatarMilestone.None, AvatarBackdrop.Leaf),
            new AvatarInfo(5, "hummingbird", AvatarMilestone.None, AvatarBackdrop.Petroleum),
            new AvatarInfo(6, "dart_frog", AvatarMilestone.HardWin, AvatarBackdrop.Wood),
            new AvatarInfo(7, "otter", AvatarMilestone.Streak10, AvatarBackdrop.Water),
            new AvatarInfo(8, "jaguar", AvatarMilestone.VeryHardWin, AvatarBackdrop.Petroleum),
            new AvatarInfo(9, "kingfisher", AvatarMilestone.DailyStreak7, AvatarBackdrop.Water),
            new AvatarInfo(10, "caiman", AvatarMilestone.World3, AvatarBackdrop.Petroleum),
            new AvatarInfo(11, "anteater", AvatarMilestone.World4, AvatarBackdrop.Wood),
            new AvatarInfo(12, "heron", AvatarMilestone.DailyStamps20, AvatarBackdrop.Leaf),
            new AvatarInfo(13, "howler", AvatarMilestone.World5, AvatarBackdrop.Leaf),
            new AvatarInfo(14, "ocelot", AvatarMilestone.League50, AvatarBackdrop.Petroleum),
            new AvatarInfo(15, "armadillo", AvatarMilestone.World6, AvatarBackdrop.Water),
        };

        /// <summary>Los 16, en el orden de la tabla (que es el de su índice).</summary>
        public static IReadOnlyList<AvatarInfo> All => Entries;

        /// <summary>Cuántos hay.</summary>
        public static int Count => Entries.Length;

        /// <summary>El avatar de id <paramref name="id"/>, o null si no está en el catálogo.</summary>
        public static AvatarInfo? Find(string? id)
        {
            if (id == null)
            {
                return null;
            }

            for (int i = 0; i < Entries.Length; i++)
            {
                if (string.Equals(Entries[i].Id, id, StringComparison.Ordinal))
                {
                    return Entries[i];
                }
            }

            return null;
        }

        /// <summary>El avatar del índice <paramref name="index"/>, o null (un índice de una versión más nueva: el disco neutro).</summary>
        public static AvatarInfo? AtIndex(int? index)
        {
            if (!index.HasValue || index.Value < 0 || index.Value >= Entries.Length)
            {
                return null;
            }

            return Entries[index.Value];
        }

        /// <summary>El libre número <paramref name="k"/> (0 a <see cref="FreeCount"/> − 1).</summary>
        public static AvatarInfo Free(int k)
        {
            if (k < 0 || k >= FreeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(k));
            }

            return Entries[k];
        }

        /// <summary>El id de analítica del hito (<c>af_avatar_unlock.milestone</c>, 08 §8.2), o null si es libre.</summary>
        public static string? MilestoneId(AvatarMilestone m)
        {
            switch (m)
            {
                case AvatarMilestone.HardWin: return "hard_win";
                case AvatarMilestone.VeryHardWin: return "very_hard_win";
                case AvatarMilestone.World3: return "world_3";
                case AvatarMilestone.World4: return "world_4";
                case AvatarMilestone.World5: return "world_5";
                case AvatarMilestone.World6: return "world_6";
                case AvatarMilestone.Streak10: return "streak_10";
                case AvatarMilestone.DailyStreak7: return "daily_streak_7";
                case AvatarMilestone.DailyStamps20: return "daily_stamps_20";
                case AvatarMilestone.League50: return "league_50";
                default: return null;
            }
        }

        /// <summary>El mundo de un hito de mundo (3-6), o 0.</summary>
        public static int WorldOf(AvatarMilestone m)
        {
            switch (m)
            {
                case AvatarMilestone.World3: return 3;
                case AvatarMilestone.World4: return 4;
                case AvatarMilestone.World5: return 5;
                case AvatarMilestone.World6: return 6;
                default: return 0;
            }
        }

        /// <summary>El último nivel del mundo de un hito de mundo (el que hay que ganar: <see cref="WorldTable.LastLevelOf"/>), o 0.</summary>
        public static int WorldLastLevel(AvatarMilestone m)
        {
            int world = WorldOf(m);
            return world > 0 ? WorldTable.LastLevelOf(world) : 0;
        }
    }
}
