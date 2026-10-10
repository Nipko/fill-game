#nullable enable
using AF.Core.Mechanics;
using AF.Core.Session;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Contador de la Lluvia o de la Piraña en el HUD (02 §9.1 M6-M7; traspaso §6 paso 1.3: «Lluvia (con su contador en el
    /// HUD)»; tarea T09): una píldora de madera oscura como las de Semillas y Canoas de la cabecera (07 §7.4) con el icono de
    /// la mecánica (<see cref="ProceduralArt.MechanicIcon"/>: la nube o la piraña), los vertidos útiles que faltan
    /// (<see cref="LevelCounter.Remaining"/>, que solo baja con vertidos útiles: Q18 B) y, en la Lluvia, una gotita del agua
    /// del mundo por cada gota pendiente (<see cref="LevelCounter.PendingDrops"/>). Atenuado si
    /// <see cref="LevelCounter.Blocked"/> (la gota espera o el mordisco se perderá). Oculto sin contador.
    /// </summary>
    /// <remarks>
    /// La vista no decide reglas: pinta el <see cref="LevelCounter"/> del núcleo tal cual. Sin asignaciones al refrescar
    /// (la cifra va con <see cref="TMP_Text.SetCharArray(char[], int, int)"/>); dos alturas, la normal (<see cref="HeightDp"/>)
    /// y la compacta del HUD (<see cref="CompactHeightDp"/>).
    /// </remarks>
    internal sealed class HudCounterChip
    {
        /// <summary>Alto de la píldora normal (dp).</summary>
        public const float HeightDp = 28f;

        /// <summary>Alto de la píldora compacta (dp; la del chip de dificultad compacto).</summary>
        public const float CompactHeightDp = DifficultyChip.CompactHeightDp;

        /// <summary>Gotitas como mucho (la Lluvia del nivel; <c>n</c> ≤ 4 por <c>U_agua</c> = 4, 02 §9.1 M6).</summary>
        public const int MaxDrops = 4;

        /// <summary>Opacidad del chip con el contador bloqueado [P].</summary>
        public const float BlockedAlpha = 0.5f;

        // La placa de madera mide 48 × 48 dp con 18 dp de 9-slice (como WalletBar): el borde se reduce a la mitad del alto.
        private const float PlateSliceDp = 18f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _plate;
        private readonly Image _icon;
        private readonly TextMeshProUGUI _value;
        private readonly Image[] _drops = new Image[MaxDrops];
        private readonly char[] _buf = new char[8];
        private MechanicType _type = MechanicType.None;
        private int _remaining = -1;
        private int _dropCount = -1;
        private int _dropTint = -1;
        private bool _blocked;
        private bool _compact;
        private float _width;

        private HudCounterChip(Transform parent, ProceduralArt art)
        {
            _art = art;
            _root = UiBuilder.Rect("CounterChip", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _plate = UiBuilder.Sliced("Plate", _root, art.WoodPlate, Color.white);
            _icon = UiBuilder.Image("Icon", _root, null, Color.white);
            _icon.preserveAspect = true;
            _value = UiBuilder.Label("Value", _root, TextStyle.PillWood, string.Empty, 17f, Palette.Cream, TextAlignmentOptions.Center);
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i] = UiBuilder.Image("Drop" + i, _root, art.DropIcon, Color.white);
                _drops[i].preserveAspect = true;
                _drops[i].gameObject.SetActive(false);
            }

            _root.gameObject.SetActive(false);
        }

        /// <summary>Contenedor del chip.</summary>
        public RectTransform Root => _root;

        /// <summary>Se ve (hay contador).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Mecánica del contador mostrado (<see cref="MechanicType.None"/> si no se ve).</summary>
        public MechanicType Type => Visible ? _type : MechanicType.None;

        /// <summary>Cifra mostrada (−1 si no se ve).</summary>
        public int Remaining => Visible ? _remaining : -1;

        /// <summary>Texto de la cifra.</summary>
        public string Text => _value.text;

        /// <summary>Gotitas a la vista.</summary>
        public int DropsShown => Visible ? _dropCount : 0;

        /// <summary>Atenuado por <see cref="LevelCounter.Blocked"/>.</summary>
        public bool Blocked => Visible && _blocked;

        /// <summary>Opacidad aplicada (1 o <see cref="BlockedAlpha"/>).</summary>
        public float Alpha => _group.alpha;

        /// <summary>Icono de la mecánica (la nube o la piraña).</summary>
        public Sprite? Icon => _icon.sprite;

        /// <summary>Color de las gotitas (el agua del mundo).</summary>
        public Color DropColor => _drops[0].color;

        /// <summary>Variante compacta.</summary>
        public bool Compact => _compact;

        /// <summary>Alto de la píldora (dp).</summary>
        public float Height => _compact ? CompactHeightDp : HeightDp;

        /// <summary>Ancho con el contenido actual (dp).</summary>
        public float Width => _width;

        /// <summary>Crea el chip, oculto, bajo <paramref name="parent"/>.</summary>
        public static HudCounterChip Create(Transform parent, ProceduralArt art)
        {
            return new HudCounterChip(parent, art);
        }

        /// <summary>
        /// Pinta <paramref name="counter"/> (oculto si no está activo) con las gotitas del tinte <paramref name="dropTint"/> (el
        /// agua del mundo de la Lluvia: <c>Cuia.MechanicKey</c>). Devuelve true si cambió el ancho o la visibilidad (hay que
        /// recolocar). Sin asignaciones.
        /// </summary>
        public bool Show(in LevelCounter counter, int dropTint)
        {
            bool wasVisible = Visible;
            float oldWidth = _width;
            if (!counter.IsActive)
            {
                if (wasVisible)
                {
                    _root.gameObject.SetActive(false);
                }

                return wasVisible;
            }

            if (!wasVisible)
            {
                _root.gameObject.SetActive(true);
            }

            if (counter.Type != _type)
            {
                _type = counter.Type;
                _icon.sprite = _art.MechanicIcon(_type);
            }

            int remaining = counter.Remaining;
            bool digitsChanged = (remaining >= 10) != (_remaining >= 10);
            if (remaining != _remaining)
            {
                _remaining = remaining;
                int n = UiText.WriteNumber(_buf, 0, remaining, false);
                _value.SetCharArray(_buf, 0, n);
            }

            int drops = counter.Type == MechanicType.Lluvia ? Mathf.Clamp(counter.PendingDrops, 0, MaxDrops) : 0;
            bool dropsChanged = drops != _dropCount;
            if (dropsChanged)
            {
                _dropCount = drops;
                for (int i = 0; i < _drops.Length; i++)
                {
                    _drops[i].gameObject.SetActive(i < drops);
                }
            }

            if (drops > 0 && dropTint != _dropTint)
            {
                _dropTint = dropTint;
                Color water = Palette.Tint(dropTint); // el agua del mundo, como las gotitas de la nube
                for (int i = 0; i < _drops.Length; i++)
                {
                    _drops[i].color = water;
                }
            }

            _blocked = counter.Blocked;
            float alpha = _blocked ? BlockedAlpha : 1f;
            if (!Mathf.Approximately(_group.alpha, alpha))
            {
                _group.alpha = alpha;
            }

            if (dropsChanged || digitsChanged || !wasVisible)
            {
                Layout();
            }

            return !wasVisible || !Mathf.Approximately(oldWidth, _width);
        }

        /// <summary>Pasa a la variante compacta (o vuelve) y recalcula el ancho; quien lo coloca llama después a <see cref="Place"/>.</summary>
        public void SetCompact(bool compact)
        {
            if (compact == _compact)
            {
                return;
            }

            _compact = compact;
            Layout();
        }

        /// <summary>Coloca el chip con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="yTop"/>) del padre.</summary>
        public void Place(float x, float yTop)
        {
            UiBuilder.Place(_root, x, yTop, _width, Height);
        }

        // Placa, icono, cifra y gotitas de izquierda a derecha (dp). La cifra lleva una caja fija por dígito (sin medir el
        // texto) para no asignar.
        private void Layout()
        {
            float h = Height;
            float pad = _compact ? 3f : 4f;
            float icon = h - (_compact ? 3f : 4f);
            float textSize = _compact ? 13f : 17f;
            int digits = _remaining >= 10 ? 2 : 1;
            float digitW = Mathf.Ceil(textSize * 0.62f) * digits;
            float dropW = _compact ? 6f : 8f;
            float dropGap = _compact ? 1.5f : 2f;
            float x = pad;
            UiBuilder.Place(_icon.rectTransform, x, (h - icon) * 0.5f, icon, icon);
            x += icon + 1f;
            _value.fontSize = textSize;
            UiBuilder.Place(_value.rectTransform, x, 0f, digitW + 2f, h);
            x += digitW + 2f;
            int drops = Mathf.Max(0, _dropCount);
            if (drops > 0)
            {
                x += _compact ? 1f : 2f;
                float dropH = dropW * 1.25f;
                for (int i = 0; i < drops && i < _drops.Length; i++)
                {
                    UiBuilder.Place(_drops[i].rectTransform, x, (h - dropH) * 0.5f, dropW, dropH);
                    x += dropW + dropGap;
                }

                x -= dropGap;
            }

            x += pad + 2f;
            _width = Mathf.Ceil(x);
            _plate.pixelsPerUnitMultiplier = PlateSliceDp / (h * 0.5f);
            UiBuilder.Place(_plate.rectTransform, 0f, 0f, _width, h);
        }
    }
}
