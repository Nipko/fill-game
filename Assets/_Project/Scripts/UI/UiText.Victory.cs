#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Calificativo de la cinta de victoria (tarea DC14; Magic Sort §10.2 #25, recomendada A; D-029), de <c>strings.csv</c>.
    /// Parcial de <see cref="UiText"/>.
    /// </summary>
    public static partial class UiText
    {
        /// <summary>
        /// <c>victory_perfect</c> [P]: «¡Perfecto!», el título de la cinta en lugar de <see cref="LevelComplete"/> cuando el
        /// intento ganado no llevó la Cuia de rescate (<see cref="AF.GameFlow.LevelFlow.IsPerfect"/>).
        /// </summary>
        public static string VictoryPerfect => Loc.T(Keys.VictoryPerfect);

        /// <summary>Título de la cinta de victoria: <see cref="VictoryPerfect"/> si <paramref name="perfect"/>; si no, <see cref="LevelComplete"/>.</summary>
        public static string VictoryTitle(bool perfect)
        {
            return perfect ? VictoryPerfect : LevelComplete;
        }

        /// <summary>Clave del calificativo de victoria (parcial de <see cref="Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>victory_perfect</c> [P] (D-029; tarea DC14).</summary>
            public const string VictoryPerfect = "victory_perfect";
        }
    }
}
