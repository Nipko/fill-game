#nullable enable
using System;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>Qué toca del Mapa del río en una victoria (<see cref="MapMoments.ForVictory"/>).</summary>
    public enum VictoryMoment
    {
        /// <summary>Nada del Mapa.</summary>
        None = 0,

        /// <summary>Franja de llegada a la parada en la ranura del Dato del río (02 §6, fila Llegada).</summary>
        Arrival = 1,

        /// <summary>El Mapa del 10 se abre solo con la primera llegada (02 §6; 06 §2.3).</summary>
        L10Map = 2,
    }

    /// <summary>
    /// Qué toca del Mapa del río en cada momento (08 §2.2; D-013, puntos 3, 7 y 8; 02 §6; 06 §2.3): la franja de llegada, el
    /// Mapa automático del 10 o nada, el viaje de mundo dentro del banner (<c>ui_map_autopen</c>, 08 §9) y si la llegada lleva
    /// su dato según la política de los hitos de TiTino (T6-A). Lo que sale en la única línea de la victoria lo decide
    /// <see cref="VictoryLineArbiter"/>, que coincide con estas reglas. Sin motor, sin estado y sin asignaciones.
    /// </summary>
    public static class MapMoments
    {
        /// <summary>Primer nivel con banner de mundo (el 11): su viaje lo cuenta el Mapa del 10 (D-013, punto 7).</summary>
        private const int FirstBannerLevel = 11;

        /// <summary>
        /// Qué toca al ganar el nivel <paramref name="won"/>: en el 10, <see cref="VictoryMoment.L10Map"/> si el Mapa está
        /// disponible y <see cref="VictoryMoment.None"/> si no (02 §6: la primera llegada la cuenta el Mapa); en los <c>10k</c>
        /// con k ≥ 2 y parada en <paramref name="stops"/> (<see cref="RiverStopCatalog.ArrivalOnWin"/>),
        /// <see cref="VictoryMoment.Arrival"/>; en el resto, nada. Los dos valores de <paramref name="autopen"/>
        /// (<c>ui_map_autopen</c>) abren el Mapa en el 10, así que no cambian el resultado: se recibe por el contrato del plan
        /// del Mapa (§3), para que un valor nuevo de la clave no cambie la firma.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El nivel no está en 1..1.000.</exception>
        public static VictoryMoment ForVictory(int won, RiverStopCatalog? stops, bool mapAvailable, string autopen)
        {
            _ = RiverJourney.StopIndexOf(won); // valida el rango 1..1.000
            if (won == RiverStopCatalog.LevelsPerStop)
            {
                return mapAvailable ? VictoryMoment.L10Map : VictoryMoment.None;
            }

            return stops?.ArrivalOnWin(won) != null ? VictoryMoment.Arrival : VictoryMoment.None;
        }

        /// <summary>
        /// El banner de mundo del nivel <paramref name="bannerLevel"/> trae dentro el viaje de mundo (D-013, punto 7; Q4-D; 02
        /// §6, fila Banner de mundo): solo si el nivel lleva banner (<see cref="WorldBanners.ShowsOn"/>), no es el 11 (su viaje
        /// lo cuenta el Mapa del 10) y <paramref name="autopen"/> es <see cref="MapConfig.AutopenL10World"/> (comparación ordinal).
        /// </summary>
        public static bool TravelInBanner(int bannerLevel, string autopen)
        {
            return bannerLevel != FirstBannerLevel && WorldBanners.ShowsOn(bannerLevel)
                && string.Equals(autopen, MapConfig.AutopenL10World, StringComparison.Ordinal);
        }

        /// <summary>
        /// La llegada al ganar <paramref name="won"/> lleva la placa de su dato: con <see cref="HitoLinePolicy.SlothHito"/>, no en
        /// los hitos de TiTino (<see cref="VictoryLineArbiter.IsSlothHito"/>: fines de mundo salvo el 10, el 40 y el 60), porque
        /// habla él y el dato pasa a la victoria siguiente (T6-A); en el resto, sí. Si TiTino no puede hablar, decide
        /// <see cref="VictoryLineArbiter.For"/>, que devuelve el hito a una victoria normal.
        /// </summary>
        public static bool ArrivalShowsFact(int won, HitoLinePolicy policy)
        {
            return policy != HitoLinePolicy.SlothHito || !VictoryLineArbiter.IsSlothHito(won);
        }
    }
}
