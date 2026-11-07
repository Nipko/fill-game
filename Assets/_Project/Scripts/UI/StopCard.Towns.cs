#nullable enable
using System;
using AF.Levels;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// La ficha de un pueblo de paso del Mapa v2 (D-069, M2-2; propuesta del Mapa v2 §4.1, regla 4), parcial de
    /// <see cref="StopCard"/>: como la de un punto de paso (<see cref="StopCardMode.Passing"/>, sin fondo, Canasta ni muestra), con
    /// la placa del lugar de las paradas, «{lugar} · {país} · río {río}» (<c>map_arrived</c>, con sus cortes), y su dato si tiene
    /// texto aprobado. Sin país o sin río aprobados (en release, hasta la revisión de Gemini, D-054), solo el nombre.
    /// </summary>
    public sealed partial class StopCard
    {
        /// <summary>Abre la ficha del pueblo <paramref name="point"/> (ya pasado).</summary>
        public void ShowTown(RiverPassingPoint point)
        {
            ShowPassing(point ?? throw new ArgumentNullException(nameof(point)));
            string place = Loc.Fact(point.NameKey);
            string country = point.CountryNameKey.Length > 0 ? Loc.Fact(point.CountryNameKey) : string.Empty;
            string river = Loc.Fact(point.RiverKey);
            _title.text = TownTitle(place, country, river);
            ApplyContent();
        }

        // La placa del lugar con el primer reparto de {sep} con el que la línea no se parte sola (como ArrivedText).
        private string TownTitle(string place, string country, string river)
        {
            if (place.Length == 0 || country.Length == 0 || river.Length == 0)
            {
                return place;
            }

            int seps = UiText.MapArrivedSeparators();
            float width = TitleWidth(_canvasW);
            float size = UiText.StopNameSizeSp * UiBuilder.TextScale;
            int[] tries = { 0, 2, 1, 3 };
            for (int i = 0; i < tries.Length; i++)
            {
                int breaks = tries[i];
                if (breaks >= (1 << seps))
                {
                    continue;
                }

                string text = UiText.MapArrived(place, country, river, breaks);
                int lines = 1 + ((breaks & 1) != 0 ? 1 : 0) + ((breaks & 2) != 0 ? 1 : 0);
                int measured = UiText.LineCount(text, TextStyle.ModalTitle, size, width);
                if (measured >= 0 && measured <= lines)
                {
                    return text;
                }
            }

            return UiText.MapArrived(place, country, river, (1 << seps) - 1);
        }
    }
}
