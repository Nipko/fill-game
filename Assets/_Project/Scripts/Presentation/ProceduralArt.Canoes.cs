#nullable enable

namespace AF.Presentation
{
    /// <summary>
    /// El icono grande de la tarjeta de las Canoas del 31 (tarea B11; 04 §7.1, «Debut»; D-033, Q-E4): por ahora, la misma canoa de
    /// la cabecera y del inicio (<see cref="IconCanoe"/>; glosario §2.2: «canoa de madera vista de lado, proa a la derecha, con
    /// línea de agua»), que <see cref="CardHeroIcon"/> devuelve con la clave <see cref="CanoesHeroIconKey"/> (parche B11-flow).
    /// Provisional por código; lo sustituye el arte del artista.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Clave del icono grande de la tarjeta de las Canoas (<c>lives</c>, el ID del sistema del glosario §6.3): la misma que
        /// <c>AF.GameFlow.LevelCardInfo.CanoesIntro</c>.
        /// </summary>
        public const string CanoesHeroIconKey = "lives";
    }
}
