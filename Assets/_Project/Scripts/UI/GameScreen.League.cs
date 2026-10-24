#nullable enable
using AF.Presentation;
using TMPro;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La línea «+{n} puntos de Liga» de la victoria (D-046; liga.md §1: «una línea pequeña desde el 41, cuando el nivel suma
    /// puntos» [P]), bajo la Cosecha. Con la Liga activa la fila se reserva en todas las victorias de campaña (así «Siguiente» no
    /// salta de una victoria a otra, como la ranura de Q7-A) y el texto sale solo cuando esta victoria sumó; sin Liga (apagada,
    /// antes del 41 o en el Reto), ni fila ni texto. Los puntos los pone <c>GameBootstrap</c> al ganar (<see cref="SetLeagueVictory"/>),
    /// antes de que se vea la victoria; son los de la tabla de 06 §6, porque los del servidor llegan después.
    /// </summary>
    public sealed partial class GameScreen
    {
        /// <summary>Tamaño del texto de la línea al 100 % (sp) [P].</summary>
        public const float LeagueLineSp = 14f;

        private TextMeshProUGUI? _leagueLine;
        private int _leaguePoints;
        private int _leaguePointsLevel = -1;

        /// <summary>La Liga está activa en esta sesión: la victoria de campaña reserva su línea (lo fija <c>GameBootstrap</c>).</summary>
        internal bool LeagueLineReserved { get; set; }

        /// <summary>La línea se ve en la victoria de ahora.</summary>
        internal bool LeagueLineShown => _leagueLine != null && _leagueLine.gameObject.activeSelf && _leagueLine.text.Length > 0;

        /// <summary>Lo que dice la línea (vacío si no se ve).</summary>
        internal string LeagueLineText => LeagueLineShown ? _leagueLine!.text : string.Empty;

        internal RectTransform? LeagueLineRect => _leagueLine?.rectTransform;

        /// <summary>La victoria del nivel <paramref name="level"/> sumó <paramref name="points"/> puntos de Liga (0 = no sumó).</summary>
        internal void SetLeagueVictory(int level, int points)
        {
            _leaguePointsLevel = level;
            _leaguePoints = points;
        }

        // Alto de la línea con la escala de texto (y la línea en sí, si se reserva).
        private float LeagueLineDp(int compact)
        {
            if (!LeagueLineActiveNow())
            {
                return 0f;
            }

            float s = Mathf.Max(1f, UiBuilder.TextScale);
            float sp = compact >= VictoryCompactText ? UiBuilder.MinTextSp : LeagueLineSp;
            return Mathf.Ceil((sp * s) + 6f);
        }

        // La reserva vale para la victoria de campaña con la Liga activa (en el Reto, nunca).
        private bool LeagueLineActiveNow()
        {
            return LeagueLineReserved && (_flow == null || !_flow.IsDaily);
        }

        // En LayoutVictoryPass, tras la Cosecha: coloca la línea y devuelve lo que ocupa.
        private float LayoutLeagueLine(float y, float panelW, int compact)
        {
            float h = LeagueLineDp(compact);
            if (h <= 0f)
            {
                if (_leagueLine != null)
                {
                    _leagueLine.gameObject.SetActive(false);
                }

                return 0f;
            }

            if (_leagueLine == null)
            {
                _leagueLine = UiBuilder.Label("LeaguePoints", _victoryPanel, TextStyle.PlainBold, string.Empty, LeagueLineSp, Palette.FamilyWood.Base, TextAlignmentOptions.Center);
                _leagueLine.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            }

            bool earned = _flow != null && _leaguePoints > 0 && _leaguePointsLevel == _flow.LevelNumber;
            float s = Mathf.Max(1f, UiBuilder.TextScale);
            _leagueLine.enableAutoSizing = true;
            _leagueLine.fontSizeMax = (compact >= VictoryCompactText ? UiBuilder.MinTextSp : LeagueLineSp) * s;
            _leagueLine.fontSizeMin = Mathf.Min(_leagueLine.fontSizeMax, UiBuilder.MinTextSp * s);
            _leagueLine.text = earned ? LeagueText.VictoryPoints(_leaguePoints) : string.Empty;
            _leagueLine.gameObject.SetActive(earned);
            UiBuilder.Place(_leagueLine.rectTransform, 16f, y, panelW - 32f, h);
            return h;
        }
    }
}
