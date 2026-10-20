#nullable enable
using AF.Economy;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Widget de la Crecida en el inicio (tarea B1b; 06 §4.3, «HUD»: «en el lobby, widget lateral silencioso»; 06 §4.7, fila Lobby:
    /// «widget de Crecida (25+)»): la misma barra vertical del HUD, a la derecha, con la Hoja escudo al pie si queda la de hoy, sin cifra,
    /// como la del HUD [P] (revisión de las capturas de B1b: al 130 % una cuenta «{n}/10» no cabía en la franja libre). Silencioso: sin sonido, sin animación y sin toques. Solo se repinta al cambiar el estado.
    /// </summary>
    internal sealed class StreakWidget
    {
        /// <summary>Ancho del widget (dp).</summary>
        public const float WidthDp = 26f;

        /// <summary>Ancho de su barra (dp; la del HUD, 07 §7.4).</summary>
        public const float BarDp = 8f;

        /// <summary>Lado de la Hoja escudo (dp).</summary>
        public const float LeafDp = 16f;

        private readonly RectTransform _root;
        private readonly Image _edge;
        private readonly Image _track;
        private readonly Image _fill;
        private readonly Image _leaf;
        private int _wins = -1;
        private int _shield = -1;
        private float _barH;

        public StreakWidget(Transform parent, ProceduralArt art)
        {
            _root = UiBuilder.Rect("StreakWidget", parent);
            _edge = UiBuilder.Sliced("Edge", _root, art.RoundedRect, Palette.FamilyWater.Outline);
            _edge.pixelsPerUnitMultiplier = 6f;
            _track = UiBuilder.Sliced("Track", _root, art.RoundedRect, new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.85f));
            _track.pixelsPerUnitMultiplier = 6f;
            _fill = UiBuilder.Sliced("Water", _track.transform, art.RoundedRect, Palette.FamilyWater.Base);
            _fill.pixelsPerUnitMultiplier = 6f;
            _leaf = UiBuilder.Image("Shield", _root, art.LilyPad, Color.white);
            _leaf.preserveAspect = true;
            _root.gameObject.SetActive(false);
        }

        /// <summary>El widget se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Victorias que enseña (0-9; −1 antes de la primera).</summary>
        public int Wins => _wins;

        /// <summary>Enseña la Hoja escudo.</summary>
        public bool ShieldShown => Visible && _shield == 1;

        /// <summary>Caja del widget (tests de solapes).</summary>
        public RectTransform Root => _root;

        /// <summary>Muestra u oculta el widget (solo si cambia).</summary>
        public void SetVisible(bool visible)
        {
            if (_root.gameObject.activeSelf != visible)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        /// <summary>El estado de la Crecida que enseña; solo repinta si cambia.</summary>
        public void Set(int wins, bool shield)
        {
            wins = Mathf.Clamp(wins, 0, StreakRecord.MaxWins);
            int s = shield ? 1 : 0;
            if (wins == _wins && s == _shield)
            {
                return;
            }

            _wins = wins;
            _shield = s;
            _leaf.gameObject.SetActive(shield);
            PlaceFill();
        }

        /// <summary>Coloca el widget en (<paramref name="x"/>, <paramref name="top"/>) con <paramref name="height"/> dp de alto.</summary>
        public void Place(float x, float top, float height)
        {
            UiBuilder.Place(_root, x, top, WidthDp, height);
            _barH = Mathf.Max(24f, height - (LeafDp * 0.5f));
            UiBuilder.Place(_edge.rectTransform, ((WidthDp - BarDp) * 0.5f) - 1f, -1f, BarDp + 2f, _barH + 2f);
            UiBuilder.Place(_track.rectTransform, (WidthDp - BarDp) * 0.5f, 0f, BarDp, _barH);
            UiBuilder.Place(_leaf.rectTransform, (WidthDp - LeafDp) * 0.5f, _barH - (LeafDp * 0.5f), LeafDp, LeafDp);
            PlaceFill();
        }

        private void PlaceFill()
        {
            float t = _wins <= 0 ? 0f : _wins / (float)StreakRecord.MaxWins;
            float h = Mathf.Max(0f, _barH - 2f) * t;
            _fill.gameObject.SetActive(h > 0.5f);
            float fh = Mathf.Max(h, BarDp - 2f);
            UiBuilder.Place(_fill.rectTransform, 1f, _barH - 1f - fh, BarDp - 2f, fh);
        }
    }
}
