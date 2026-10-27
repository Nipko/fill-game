#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;
using AF.Save;

namespace AF.LiveOps
{
    /// <summary>
    /// Lo que hace falta saber del perfil para los hitos de los avatares (avatares.md §1.3: «todo sale del estado que ya existe»,
    /// sin contadores nuevos). Lo arma quien llama con el perfil de ahora.
    /// </summary>
    public readonly struct AvatarProgress
    {
        public AvatarProgress(int maxLevel, bool hardWon, bool veryHardWon, bool riverFull, int dailyStreakDays, int dailyStamps, int leaguePoints)
        {
            MaxLevel = maxLevel;
            HardWon = hardWon;
            VeryHardWon = veryHardWon;
            RiverFull = riverFull;
            DailyStreakDays = dailyStreakDays;
            DailyStamps = dailyStamps;
            LeaguePoints = leaguePoints;
        }

        /// <summary><c>progress.max_level</c>: el nivel más alto ganado (un nivel saltado con el Salto del Boto cuenta).</summary>
        public int MaxLevel { get; }

        /// <summary>Esta victoria fue de un nivel Difícil.</summary>
        public bool HardWon { get; }

        /// <summary>Esta victoria fue de un nivel Muy difícil.</summary>
        public bool VeryHardWon { get; }

        /// <summary><c>streak.river_full</c>: se pagó el hito 10 de la Crecida.</summary>
        public bool RiverFull { get; }

        /// <summary><c>daily_challenge.streak_days</c>.</summary>
        public int DailyStreakDays { get; }

        /// <summary>Los sellos del Reto de este mes (<c>daily_challenge.stamp_days</c>).</summary>
        public int DailyStamps { get; }

        /// <summary>Los puntos de la Liga de esta semana según la última respuesta del servidor.</summary>
        public int LeaguePoints { get; }
    }

    /// <summary>Un avatar recién desbloqueado (para <c>af_avatar_unlock</c> y <c>AF_STATE avatar</c>).</summary>
    public readonly struct AvatarUnlock
    {
        public AvatarUnlock(AvatarInfo avatar, bool retro)
        {
            Avatar = avatar;
            Retro = retro;
        }

        public AvatarInfo Avatar { get; }

        /// <summary>Desbloqueado al cargar un perfil anterior (<c>af_avatar_unlock.retro</c>).</summary>
        public bool Retro { get; }
    }

    /// <summary>Lo que pasó al tocar un avatar en «Tu avatar».</summary>
    public enum AvatarSelectResult
    {
        /// <summary>Elegido (era otro).</summary>
        Selected = 0,

        /// <summary>Ya era el elegido: nada cambia (ni evento ni guardado).</summary>
        AlreadyChosen = 1,

        /// <summary>Bloqueado: no se elige (sacudida y globo con el hito).</summary>
        Locked = 2,

        /// <summary>No está en el catálogo (o está oculto: el de la Liga con la Liga apagada).</summary>
        Unknown = 3,
    }

    /// <summary>
    /// Las reglas de los avatares (avatares.md §1.3, §3.1 y §3.5), puras y sin motor: cuándo se cumple un hito, qué se ve en el
    /// selector, el punto «nuevo» del inicio y cuántos niveles faltan para un mundo.
    /// </summary>
    public static class AvatarRules
    {
        /// <summary>El avatar sale en el inicio (y en Ajustes) desde que se gana este nivel, con el Mapa y el Mercado (Q-AV-3).</summary>
        public const int HomeUnlockLevel = 10;

        /// <summary>Retroactividad de Difícil: <c>max_level</c> ≥ 41 en un perfil anterior (avatares.md §1.3).</summary>
        public const int HardRetroLevel = 41;

        /// <summary>Retroactividad de Muy difícil: <c>max_level</c> ≥ 61.</summary>
        public const int VeryHardRetroLevel = 61;

        /// <summary>Racha del Reto del martín pescador (la que paga la Pororoca, 06 §4.1).</summary>
        public const int DailyStreakDays = 7;

        /// <summary>Sellos del Reto en un mes de la garza (R-Q7).</summary>
        public const int DailyStamps = 20;

        /// <summary>Puntos de la Liga en una semana del ocelote (06 §6, «Participante»).</summary>
        public const int LeaguePoints = 50;

        /// <summary>Victorias del hito de la Crecida de la nutria (el río crecido).</summary>
        public const int StreakWins = 10;

        /// <summary>¿Cumple <paramref name="p"/> el hito <paramref name="m"/>? (Libre: siempre.)</summary>
        public static bool IsMet(AvatarMilestone m, in AvatarProgress p)
        {
            switch (m)
            {
                case AvatarMilestone.None: return true;
                case AvatarMilestone.HardWin: return p.HardWon || p.VeryHardWon || p.MaxLevel >= HardRetroLevel;
                case AvatarMilestone.VeryHardWin: return p.VeryHardWon || p.MaxLevel >= VeryHardRetroLevel;
                case AvatarMilestone.World3:
                case AvatarMilestone.World4:
                case AvatarMilestone.World5:
                case AvatarMilestone.World6:
                    return p.MaxLevel >= AvatarCatalog.WorldLastLevel(m);
                case AvatarMilestone.Streak10: return p.RiverFull;
                case AvatarMilestone.DailyStreak7: return p.DailyStreakDays >= DailyStreakDays;
                case AvatarMilestone.DailyStamps20: return p.DailyStamps >= DailyStamps;
                case AvatarMilestone.League50: return p.LeaguePoints >= LeaguePoints;
                default: return false;
            }
        }

        /// <summary>Se puede elegir: libre o ya desbloqueado (un desbloqueo no se pierde nunca).</summary>
        public static bool IsAvailable(AvatarInfo avatar, AvatarData data)
        {
            if (avatar == null || data == null)
            {
                throw new ArgumentNullException(avatar == null ? nameof(avatar) : nameof(data));
            }

            return avatar.Free || data.IsUnlocked(avatar.Id);
        }

        /// <summary>
        /// Sale en el selector: todos, salvo el de la Liga con la Liga apagada (Q-AV-8: no se enseña una meta imposible; si ya lo
        /// desbloqueó, sí sale).
        /// </summary>
        public static bool VisibleInPicker(AvatarInfo avatar, AvatarData data, bool leagueEnabled)
        {
            return avatar.Milestone != AvatarMilestone.League50 || leagueEnabled || data.IsUnlocked(avatar.Id);
        }

        /// <summary>Lleva la chapa «Nuevo»: de hito, desbloqueado y sin ver en el selector.</summary>
        public static bool IsNew(AvatarInfo avatar, AvatarData data)
        {
            return !avatar.Free && data.IsUnlocked(avatar.Id) && !data.IsSeen(avatar.Id);
        }

        /// <summary>
        /// El punto «nuevo» sobre el retrato del inicio (avatares.md §3.1): desde el 10, hasta la primera elección (<c>chosen</c>) y
        /// cada vez que hay un desbloqueado sin ver.
        /// </summary>
        public static bool DotShown(AvatarData data, int maxLevel)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (maxLevel < HomeUnlockLevel)
            {
                return false;
            }

            if (!data.Chosen)
            {
                return true;
            }

            for (int i = 0; i < data.Unlocked.Count; i++)
            {
                if (!data.IsSeen(data.Unlocked[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Niveles que faltan para completar el mundo de un hito de mundo (ganar su último nivel), o 0.</summary>
        public static int LevelsLeft(AvatarMilestone m, int maxLevel)
        {
            int last = AvatarCatalog.WorldLastLevel(m);
            return last > 0 ? Math.Max(0, last - Math.Max(0, maxLevel)) : 0;
        }
    }

    /// <summary>
    /// Los avatares del jugador en el cliente (D-056; avatares.md §1.3, §3.2 y §3.5), sin motor: el bloque <c>avatar</c> del perfil
    /// (<see cref="AvatarData"/>), el default al azar entre los libres, los desbloqueos por hito, la elección y lo visto. Guarda con
    /// <c>save</c> cada vez que algo cambia (el lote del perfil en el juego).
    /// </summary>
    public sealed class AvatarService
    {
        private readonly AvatarData _data;
        private readonly Action _save;
        private readonly IClock _clock;

        public AvatarService(AvatarData data, Action save, IClock clock)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>El bloque del perfil.</summary>
        public AvatarData Data => _data;

        /// <summary>El avatar elegido (tras <see cref="EnsureValid"/>, siempre del catálogo; antes, el primero libre).</summary>
        public AvatarInfo Current => AvatarCatalog.Find(_data.Id) ?? AvatarCatalog.Free(0);

        /// <summary>Se desbloqueó un avatar.</summary>
        public event Action<AvatarUnlock>? Unlocked;

        /// <summary>Se eligió otro avatar: el nuevo, si es libre y si es la primera elección del jugador.</summary>
        public event Action<AvatarInfo, bool, bool>? Selected;

        /// <summary>Cambió el elegido o lo desbloqueado (la UI vuelve a pintar; la Liga encola el índice).</summary>
        public event Action? Changed;

        /// <summary>
        /// El id del perfil vale: si falta, no está en el catálogo o ya no se puede elegir, se asigna un libre con
        /// <paramref name="pickFree"/> (0 a <see cref="AvatarCatalog.FreeCount"/> − 1; Q-AV-2: al azar, sin preguntar) y se guarda.
        /// True si cambió.
        /// </summary>
        public bool EnsureValid(Func<int, int> pickFree)
        {
            if (pickFree == null)
            {
                throw new ArgumentNullException(nameof(pickFree));
            }

            AvatarInfo? current = AvatarCatalog.Find(_data.Id);
            if (current != null && AvatarRules.IsAvailable(current, _data))
            {
                return false;
            }

            int k = pickFree(AvatarCatalog.FreeCount);
            _data.Id = AvatarCatalog.Free(Math.Max(0, Math.Min(AvatarCatalog.FreeCount - 1, k))).Id;
            _save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Apunta los hitos que <paramref name="p"/> cumple y aún no estaban (nunca quita ninguno), guarda si hubo alguno y avisa de
        /// cada uno con <see cref="Unlocked"/>. <paramref name="retro"/>: al cargar el perfil (los de un perfil anterior).
        /// </summary>
        public int Evaluate(in AvatarProgress p, bool retro)
        {
            List<AvatarInfo>? added = null;
            IReadOnlyList<AvatarInfo> all = AvatarCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                AvatarInfo a = all[i];
                if (!a.Free && !_data.IsUnlocked(a.Id) && AvatarRules.IsMet(a.Milestone, p))
                {
                    _data.AddUnlocked(a.Id);
                    (added ??= new List<AvatarInfo>()).Add(a);
                }
            }

            if (added == null)
            {
                return 0;
            }

            _save();
            for (int i = 0; i < added.Count; i++)
            {
                Unlocked?.Invoke(new AvatarUnlock(added[i], retro));
            }

            Changed?.Invoke();
            return added.Count;
        }

        /// <summary>
        /// Elige <paramref name="id"/> si se puede (libre o desbloqueado y visible): sin botón de confirmar (avatares.md §3.2, punto 4).
        /// </summary>
        public AvatarSelectResult Select(string id, bool leagueEnabled)
        {
            AvatarInfo? avatar = AvatarCatalog.Find(id);
            if (avatar == null || !AvatarRules.VisibleInPicker(avatar, _data, leagueEnabled))
            {
                return AvatarSelectResult.Unknown;
            }

            if (!AvatarRules.IsAvailable(avatar, _data))
            {
                return AvatarSelectResult.Locked;
            }

            if (string.Equals(_data.Id, avatar.Id, StringComparison.Ordinal))
            {
                if (!_data.Chosen)
                {
                    // Elegir el asignado al azar también cuenta como la primera elección (se apaga el punto).
                    _data.Chosen = true;
                    _data.At = _clock.UtcNow;
                    _save();
                    Changed?.Invoke();
                }

                return AvatarSelectResult.AlreadyChosen;
            }

            bool first = !_data.Chosen;
            _data.Id = avatar.Id;
            _data.Chosen = true;
            _data.At = _clock.UtcNow;
            _save();
            Selected?.Invoke(avatar, avatar.Free, first);
            Changed?.Invoke();
            return AvatarSelectResult.Selected;
        }

        /// <summary>Al cerrar el selector: los desbloqueados pasan a vistos (se apagan las chapas «Nuevo» y el punto). Cuántos.</summary>
        public int MarkSeen()
        {
            int added = 0;
            for (int i = 0; i < _data.Unlocked.Count; i++)
            {
                if (_data.AddSeen(_data.Unlocked[i]))
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
    }
}
