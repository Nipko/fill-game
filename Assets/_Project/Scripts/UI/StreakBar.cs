#nullable enable
using AF.Economy;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Barra vertical de la Crecida en el borde derecho del tablero (tarea B1b; 02 §5, fila «Barra de la Crecida»; 07 §7.4: x =
    /// 348-356, y = 120-560 a 360 × 780; 06 §4.3, «HUD»): 8 dp de ancho, el agua sube de abajo arriba un escalón por victoria (10
    /// marcas, muescas en los hitos 3, 6 y 10), y la Hoja escudo, si queda la de hoy, como hoja de vitória-régia al
    /// pie. Visible desde <c>streak_unlock_level</c> (<see cref="StreakSnapshot.Active"/>) y, en cada nivel, desde T, al habilitarse
    /// el toque: durante la intro no está y la tropa lateral puede pasar por su franja (D-030, pregunta 4 (c); D-021). Sin raycast
    /// y sin asignaciones (solo cambia al cambiar el estado).
    /// </summary>
    internal sealed class StreakBar
    {
        /// <summary>Ancho de la barra (dp; 07 §7.4).</summary>
        public const float WidthDp = 8f;

        /// <summary>De su canto izquierdo al borde derecho del tablero (dp): 348 a 360 dp de ancho (07 §7.4).</summary>
        public const float FromRightDp = 12f;

        /// <summary>Su cima bajo el pie de la cabecera del HUD (dp): la y = 120 de 07 §7.4 con la barra de evento colapsada (88 + 32).</summary>
        public const float BelowHeaderDp = 32f;

        /// <summary>Su pie sobre el fin del tablero (dp): la y = 560 de 07 §7.4 con el tablero hasta 620.</summary>
        public const float AboveBoardBottomDp = 60f;

        /// <summary>Lado de la Hoja escudo al pie (dp).</summary>
        public const float LeafDp = 20f;

        /// <summary>Ancho máximo del tablero en tableta (02 §5: «el tablero se centra a 480 dp»).</summary>
        public const float MaxBoardWidthDp = 480f;

        private readonly RectTransform _root;
        private readonly Image _edge;
        private readonly Image _track;
        private readonly Image _fill;
        private readonly Image[] _notches = new Image[3];
        private readonly Image _leaf;
        private int _wins = -1;
        private int _shield = -1;
        private int _river = -1;
        private float _height;

        public StreakBar(Transform parent, ProceduralArt art)
        {
            _root = UiBuilder.Rect("StreakBar", parent);
            // Vidrio claro con el canto oscuro [P] (revisión de las capturas de B1b: sobre el marco del panel, la pista oscura no se leía).
            _edge = UiBuilder.Sliced("Edge", _root, art.RoundedRect, Palette.FamilyWater.Outline);
            _edge.pixelsPerUnitMultiplier = 6f;
            _track = UiBuilder.Sliced("Track", _root, art.RoundedRect, new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.85f));
            _track.pixelsPerUnitMultiplier = 6f;
            _fill = UiBuilder.Sliced("Water", _track.transform, art.RoundedRect, Palette.FamilyWater.Base);
            _fill.pixelsPerUnitMultiplier = 6f;
            for (int i = 0; i < _notches.Length; i++)
            {
                _notches[i] = UiBuilder.Image("Notch" + (i + 1), _track.transform, null, Palette.FamilyWater.Outline);
            }

            _leaf = UiBuilder.Image("Shield", _root, art.LilyPad, Color.white);
            _leaf.preserveAspect = true;
            _root.gameObject.SetActive(false);
        }

        /// <summary>La barra se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Victorias que enseña (0-9; −1 antes de la primera).</summary>
        public int Wins => _wins;

        /// <summary>Enseña la Hoja escudo al pie.</summary>
        public bool ShieldShown => _shield == 1 && Visible;

        /// <summary>Lo lleno del agua (0-1).</summary>
        public float Fill01 => _wins <= 0 ? 0f : _wins / (float)StreakRecord.MaxWins;

        /// <summary>Contenedor (tests de posición).</summary>
        public RectTransform Root => _root;

        /// <summary>La pista (tests de posición: x = 348-356 a 360 dp).</summary>
        public RectTransform TrackRect => _track.rectTransform;

        /// <summary>La Hoja escudo (tests).</summary>
        public RectTransform LeafRect => _leaf.rectTransform;

        /// <summary>Muestra u oculta la barra (solo si cambia).</summary>
        public void SetVisible(bool visible)
        {
            if (_root.gameObject.activeSelf != visible)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        /// <summary>El estado que enseña (la barra, la hoja de hoy y el río crecido); solo repinta si cambia.</summary>
        public void Set(int wins, bool shield, bool riverFull)
        {
            wins = Mathf.Clamp(wins, 0, StreakRecord.MaxWins);
            int s = shield ? 1 : 0;
            int r = riverFull ? 1 : 0;
            if (wins == _wins && s == _shield && r == _river)
            {
                return;
            }

            _wins = wins;
            _shield = s;
            _river = r;
            _leaf.gameObject.SetActive(shield);
            // El río crecido [P]: el agua más clara mientras dura la Cosecha doble sin anuncio (02 §5 no fija su dibujo).
            _fill.color = riverFull ? (Color)Palette.FamilyWater.Light : (Color)Palette.FamilyWater.Base;
            PlaceFill();
        }

        /// <summary>
        /// Coloca la barra junto al borde derecho del tablero en un lienzo de <paramref name="canvasW"/> dp, de
        /// <paramref name="top"/> a <paramref name="bottom"/> (dp desde arriba): la pista de 8 dp y la hoja al pie.
        /// </summary>
        public void Layout(float canvasW, float top, float bottom)
        {
            float boardRight = (canvasW + Mathf.Min(canvasW, MaxBoardWidthDp)) * 0.5f;
            float x = boardRight - FromRightDp;
            _height = Mathf.Max(40f, bottom - top);
            UiBuilder.Place(_root, 0f, 0f, canvasW, bottom + LeafDp);
            UiBuilder.Place(_edge.rectTransform, x - 1f, top - 1f, WidthDp + 2f, _height + 2f);
            UiBuilder.Place(_track.rectTransform, x, top, WidthDp, _height);
            for (int i = 0; i < _notches.Length; i++)
            {
                // Muescas en los hitos 3, 6 y 10 (02 §5), contadas desde el pie.
                int m = i == 0 ? StreakRecord.HitSeeds : i == 1 ? StreakRecord.HitBasket : StreakRecord.HitRiver;
                float y = _height - (_height * m / StreakRecord.MaxWins);
                UiBuilder.Place(_notches[i].rectTransform, 1f, Mathf.Clamp(y - 1f, 1f, _height - 3f), WidthDp - 2f, 2f);
            }

            float leafX = Mathf.Min(x + (WidthDp * 0.5f) - (LeafDp * 0.5f), canvasW - LeafDp); // nunca fuera del lienzo
            UiBuilder.Place(_leaf.rectTransform, leafX, bottom - (LeafDp * 0.5f), LeafDp, LeafDp);
            PlaceFill();
        }

        private void PlaceFill()
        {
            float h = Mathf.Max(0f, _height - 2f) * Fill01;
            _fill.gameObject.SetActive(h > 0.5f);
            UiBuilder.Place(_fill.rectTransform, 1f, _height - 1f - Mathf.Max(h, WidthDp - 2f), WidthDp - 2f, Mathf.Max(h, WidthDp - 2f));
        }
    }
}
