#nullable enable
using AF.Economy;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// La Crecida en el inicio como un <b>poste de nivel</b> clavado en el río (D-050, punto 4; 06 §4.3, «HUD»: «en el lobby, widget
    /// lateral silencioso»; 06 §4.7, fila Lobby: «widget de Crecida (25+)»; tarea B1b): una regla de crecida de madera pintada
    /// (<see cref="ProceduralArt.GaugeBoard"/>) con diez rayas, los hitos 3, 6 y 10 marcados (05 §2.4: 300 semillas, Canasta y
    /// Cuia extra + río crecido; el hito alcanzado, encendido), el agua que sube dentro según las victorias seguidas y, arriba, la
    /// cifra en su placa. Al pie, en el agua, la Hoja escudo flota si queda la de hoy. La misma lógica que la barra del HUD:
    /// solo se repinta al cambiar el estado. Silencioso: sin sonido, sin animación y sin toques; TalkBack lee «Crecida: {n} de 10»
    /// (con la Hoja escudo, si está).
    /// </summary>
    internal sealed class StreakWidget
    {
        /// <summary>Ancho del poste (dp).</summary>
        public const float WidthDp = ProceduralArt.GaugeWidthDp;

        /// <summary>Lado de la Hoja escudo que flota al pie (dp).</summary>
        public const float LeafDp = 26f;

        /// <summary>Alto de la placa de la cifra, arriba del poste (dp).</summary>
        public const float TagDp = 24f;

        /// <summary>Alto del poste en el inicio, como mucho (dp): crece con la pantalla hasta aquí.</summary>
        public const float MaxHeightDp = 150f;

        /// <summary>Lo que el poste se hunde en el agua bajo su caja, con el anillo de agua (dp).</summary>
        public const float FootDp = 6f;

        /// <summary>Hitos de la Crecida marcados en la regla (05 §2.4; <c>streak_milestones</c>).</summary>
        public static readonly int[] Milestones = { 3, 6, 10 };

        private readonly RectTransform _root;
        private readonly Image _stake;
        private readonly Image _ring;
        private readonly Image _board;
        private readonly Image _water;
        private readonly Image _surface;
        private readonly Image[] _ticks = new Image[StreakRecord.MaxWins];
        private readonly Image[] _marks = new Image[3];
        private readonly Image _tag;
        private readonly TextMeshProUGUI _count;
        private readonly Image _leaf;
        private int _wins = -1;
        private int _shield = -1;
        private float _height = 120f;
        private float _innerTop;
        private float _innerH;

        public StreakWidget(Transform parent, ProceduralArt art)
        {
            _root = UiBuilder.Rect("StreakWidget", parent);
            _ring = UiBuilder.Image("FootRing", _root, art.RiverRipple, new Color(1f, 1f, 1f, 0.75f));
            _stake = UiBuilder.Image("Stake", _root, art.RoundedRect, Palette.Hex(0x4A3020));
            _board = UiBuilder.Sliced("Board", _root, art.GaugeBoard, Color.white);
            _water = UiBuilder.Sliced("Water", _board.transform, art.RoundedRect, new Color(Palette.FamilyWater.Base.r, Palette.FamilyWater.Base.g, Palette.FamilyWater.Base.b, 0.82f));
            _water.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / 3f;
            _surface = UiBuilder.Image("Surface", _board.transform, art.RiverRipple, new Color(1f, 1f, 1f, 0.9f));
            for (int i = 0; i < _ticks.Length; i++)
            {
                _ticks[i] = UiBuilder.Image("Tick" + (i + 1), _board.transform, null, new Color(Palette.Groove.r, Palette.Groove.g, Palette.Groove.b, 0.55f));
            }

            for (int i = 0; i < _marks.Length; i++)
            {
                _marks[i] = UiBuilder.Image("Mark" + Milestones[i], _board.transform, art.Disc, Color.white);
            }

            _tag = UiBuilder.Sliced("Tag", _root, art.WoodPlate, Color.white);
            _count = UiBuilder.Label("Count", _tag.transform, TextStyle.PillWood, "0", 13f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_count.rectTransform);
            A11yLabels.SetText(_count, () => UiText.HomeCrecidaA11y(Mathf.Max(0, _wins), StreakRecord.MaxWins, _shield == 1));
            _leaf = UiBuilder.Image("Shield", _root, art.LilyPad, Color.white);
            _leaf.preserveAspect = true;
            _root.gameObject.SetActive(false);
        }

        /// <summary>El poste se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Victorias que enseña (0-9; −1 antes de la primera).</summary>
        public int Wins => _wins;

        /// <summary>Enseña la Hoja escudo.</summary>
        public bool ShieldShown => Visible && _shield == 1;

        /// <summary>Caja del poste (tests de solapes): la regla con su placa, sin el pie en el agua.</summary>
        public RectTransform Root => _root;

        /// <summary>La cifra de la placa (tests).</summary>
        public string CountText => _count.text;

        /// <summary>Cuántos hitos están encendidos (tests).</summary>
        public int MarksLit
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Milestones.Length; i++)
                {
                    if (_wins >= Milestones[i])
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Muestra u oculta el poste (solo si cambia).</summary>
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
            _count.text = Loc.Number(wins);
            PaintMarks();
            PlaceWater();
        }

        /// <summary>Coloca el poste en (<paramref name="x"/>, <paramref name="top"/>) con <paramref name="height"/> dp de alto.</summary>
        public void Place(float x, float top, float height)
        {
            _height = Mathf.Max(64f, height);
            UiBuilder.Place(_root, x, top, WidthDp, _height);
            float s = Mathf.Max(1f, UiBuilder.TextScale);
            float tagH = Mathf.Round(TagDp * Mathf.Min(s, 1.15f));
            _count.fontSize = 13f * s;
            UiBuilder.Place(_tag.rectTransform, 0f, 0f, WidthDp, tagH);
            float boardTop = tagH - 4f;
            float boardH = _height - boardTop;
            UiBuilder.Place(_board.rectTransform, 0f, boardTop, WidthDp, boardH);
            UiBuilder.Place(_stake.rectTransform, (WidthDp * 0.5f) - 4f, _height - 6f, 8f, FootDp + 6f);
            UiBuilder.Place(_ring.rectTransform, (WidthDp * 0.5f) - 18f, _height + FootDp - 5f, 36f, 9f);
            UiBuilder.Place(_leaf.rectTransform, -LeafDp + 10f, _height + FootDp - (LeafDp * 0.4f), LeafDp, LeafDp * 0.4f);

            // Dentro de la tabla, entre el remate y el marco: diez rayas, una por victoria, de abajo arriba.
            _innerTop = 8f;
            _innerH = Mathf.Max(20f, boardH - 14f);
            for (int i = 0; i < _ticks.Length; i++)
            {
                int n = i + 1;
                float y = _innerTop + (_innerH * (1f - (n / (float)StreakRecord.MaxWins)));
                bool milestone = n == 3 || n == 6 || n == 10;
                float w = milestone ? 10f : 6f;
                UiBuilder.Place(_ticks[i].rectTransform, 5f, y - 0.6f, w, milestone ? 1.6f : 1.1f);
            }

            for (int i = 0; i < _marks.Length; i++)
            {
                int n = Milestones[i];
                float y = _innerTop + (_innerH * (1f - (n / (float)StreakRecord.MaxWins)));
                UiBuilder.Place(_marks[i].rectTransform, WidthDp - 15f, y - 4.5f, 9f, 9f);
            }

            PaintMarks();
            PlaceWater();
        }

        // Los hitos: apagados (madera clara) o encendidos con su color (semilla, hoja de la Canasta, agua de la Cuia extra).
        private void PaintMarks()
        {
            for (int i = 0; i < _marks.Length; i++)
            {
                bool lit = _wins >= Milestones[i];
                Color on = i == 0 ? ProceduralArt.SeedAmber : (i == 1 ? Palette.FamilyLeaf.Base : Palette.FamilyWater.Light);
                Color c = lit ? on : new Color(Palette.FamilyWood.Light.r, Palette.FamilyWood.Light.g, Palette.FamilyWood.Light.b, 0.55f);
                if (_marks[i].color != c)
                {
                    _marks[i].color = c;
                }
            }
        }

        private void PlaceWater()
        {
            float t = _wins <= 0 ? 0f : _wins / (float)StreakRecord.MaxWins;
            float h = Mathf.Max(0f, _innerH * t);
            bool on = h > 0.5f;
            _water.gameObject.SetActive(on);
            _surface.gameObject.SetActive(on);
            if (!on)
            {
                return;
            }

            float top = _innerTop + _innerH - h;
            UiBuilder.Place(_water.rectTransform, 4f, top, WidthDp - 8f, h + 2f);
            UiBuilder.Place(_surface.rectTransform, 3f, top - 3f, WidthDp - 6f, 5f);
        }
    }
}
