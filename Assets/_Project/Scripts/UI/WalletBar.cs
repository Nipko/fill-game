#nullable enable
using System;
using AF.Economy;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Saldos del jugador en la fila superior, con dos formas:
    /// <list type="bullet">
    /// <item><description><b>Embudo y victoria</b> (D-008 paso 1: «arriba, semillas y canoas»; 04 §6.6: «cabecera: semillas,
    /// canoas»), como el saldo de Magic Sort sobre sus pantallas de fallo (ficha §4.4 y §4.8: «el saldo sale siempre arriba a
    /// la izquierda»): placas grandes con Semillas y, desde el 31, Canoas «n/5» con el cronómetro de la próxima (∞ y el tiempo
    /// que queda con Canoas infinitas).</description></item>
    /// <item><description><b>Cabecera de partida</b> (<see cref="Compact"/>; 07 §7.4, dueño del HUD: «Centro-derecha: pill
    /// Semillas (icono 20 + cifra). Derecha: pill Canoas (visible desde el 31; antes, oculto)»; 02 §5: «Canoas "4/5" desde el
    /// nivel 31»): píldoras de 22 dp entre la insignia del nivel y Reiniciar. A 360 dp solo quedan ≈ 92 dp entre las dos, así
    /// que desde el 31 la de Semillas va encima de la de Canoas (las dos dentro de la fila de 48 dp) y la de Canoas lleva
    /// «n/5» sin cronómetro (el cronómetro está en el inicio y en Sin canoas). En 1-30, solo Semillas, centrada en la
    /// fila. La columna Semillas/Canoas se puede compactar dentro de la fila (<see cref="Layout(float, float, float, float,
    /// float, float)"/>: píldoras más bajas, a escala, cuando el tablero pasa bajo el HUD).</description></item>
    /// </list>
    /// Mismo material que la pantalla de inicio (placa de madera oscura). Sin asignaciones al refrescar ni al colocar.
    /// </summary>
    internal sealed class WalletBar
    {
        /// <summary>Alto de las píldoras de la cabecera de partida (dp).</summary>
        public const float CompactPillDp = 22f;

        /// <summary>Hueco entre la píldora de Semillas y la de Canoas en la cabecera de partida (dp).</summary>
        public const float CompactGapDp = 4f;

        // La placa de madera mide 48 × 48 dp con 18 dp de 9-slice: en las píldoras de 22 dp el borde se reduce a 11 dp
        // (radio de píldora) en los dos ejes, sin esquinas elípticas.
        private const float CompactSliceMultiplier = 18f / 11f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _seeds;
        private readonly Image _seedsPlate;
        private readonly Image _seedsIcon;
        private readonly TextMeshProUGUI _seedsText;
        private readonly RectTransform _canoes;
        private readonly Image _canoesPlate;
        private readonly Image _canoesIcon;
        private readonly Image _infinite;
        private readonly TextMeshProUGUI _canoesCount;
        private readonly TextMeshProUGUI _timer;
        private readonly char[] _buf = new char[24];
        private int _seedsShown = -1;
        private int _countShown = -1;
        private int _modeShown = -1;
        private long _timerShown = long.MinValue;
        private bool _compact;
        private float _left = 64f;
        private float _top = 40f;
        private float _width = 92f;
        private float _compactMinWidth = -1f;
        private float _columnPill = CompactPillDp;
        private float _columnGap = CompactGapDp;
        private float _columnTop = 41f;

        public WalletBar(Transform parent, ProceduralArt art)
        {
            _art = art;
            _root = UiBuilder.Rect("WalletBar", parent);
            _seeds = UiBuilder.Rect("Seeds", _root);
            _seedsPlate = UiBuilder.Sliced("Plate", _seeds, art.WoodPlate, Color.white);
            UiBuilder.Stretch(_seedsPlate.rectTransform);
            _seedsIcon = UiBuilder.Image("Icon", _seeds, art.IconSeed, Color.white);
            _seedsIcon.preserveAspect = true;
            _seedsText = UiBuilder.Label("Value", _seeds, TextStyle.PillWood, "0", 20f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.AutoSize(_seedsText, 14f, 20f);

            _canoes = UiBuilder.Rect("Canoes", _root);
            _canoesPlate = UiBuilder.Sliced("Plate", _canoes, art.WoodPlate, Color.white);
            _canoesIcon = UiBuilder.Image("Icon", _canoes, art.IconCanoe, Color.white);
            _canoesIcon.preserveAspect = true;
            _infinite = UiBuilder.Image("Infinite", _canoes, art.IconInfinite, Color.white);
            _infinite.preserveAspect = true;
            _canoesCount = UiBuilder.Label("Count", _canoes, TextStyle.PillWood, "5/5", 18f, Palette.Cream, TextAlignmentOptions.Center);
            _timer = UiBuilder.Label("Timer", _canoes, TextStyle.PillWood, string.Empty, 12f, Palette.Cream, TextAlignmentOptions.Center);
            ArrangeChildren();
            _root.gameObject.SetActive(false);
        }

        /// <summary>Raíz de la barra (la pantalla la pone en la capa del HUD o en la superior según la forma).</summary>
        public RectTransform Root => _root;

        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Forma de cabecera de partida (píldoras de 07 §7.4) en lugar de las placas del embudo.</summary>
        public bool Compact => _compact;

        /// <summary>Semillas mostradas.</summary>
        public int SeedsShown => _seedsShown;

        /// <summary>Canoas mostradas (−1 con Canoas infinitas o sin contador).</summary>
        public int CanoesShown => _canoes.gameObject.activeSelf && _modeShown != 2 ? _countShown : -1;

        /// <summary>Se ve el cronómetro de la próxima canoa (nunca en la cabecera de partida).</summary>
        public bool TimerShown => _canoes.gameObject.activeSelf && _timer.gameObject.activeSelf;

        /// <summary>
        /// Lo más estrecha que puede ir la columna de la cabecera de partida (dp) [P]: la cifra de Semillas de cinco dígitos con
        /// su separador de miles («99.999», con el de la región: <see cref="UiText.WriteNumber"/>) cabe junto a su icono a 12 sp
        /// (× la escala de texto), el mínimo de 02 §12, en su caja (tras el icono y con 6 dp de aire a la derecha). Hasta aquí
        /// la puede estrechar la píldora «Reiniciar» cuando una palabra de su etiqueta no cabe en su área
        /// (GameScreen.LayoutRestartPill; tarea FX5). Mide una vez, al colocar: la escala de texto no cambia en la vida de la
        /// barra.
        /// <para>
        /// Supuesto pendiente del dueño (revisión de FX5): el saldo de la cabecera tiene cinco cifras como mucho. Ni el GDD ni el
        /// código ponen tope al saldo (05 vende paquetes de hasta 36.000 semillas); con seis cifras («100.000» o más) y la
        /// fuente al 130 % a 360 dp de ancho, la cifra no cabe a 12 sp × escala en la columna que deja «Reiniciar» (02 §12), y
        /// medir aquí con seis cifras dejaría a la píldora sin sitio para su palabra, que se volvería a partir. Al 100 %, seis
        /// cifras caben (Fx5Tests); desde DC3 el 16:9 también mide 360 dp de ancho y comparte el pendiente del 130 %.
        /// </para>
        /// </summary>
        public float CompactMinWidthDp
        {
            get
            {
                if (_compactMinWidth < 0f)
                {
                    int n = UiText.WriteNumber(_buf, 0, 99999, true);
                    string sample = new string(_buf, 0, n);
                    float min = UiBuilder.MinTextSp * UiBuilder.TextScale;
                    float at = _seedsText.enableAutoSizing ? _seedsText.fontSizeMin : _seedsText.fontSize;
                    float width = UiBuilder.MinTextWidth(_seedsText, sample) * (at > 0f ? min / at : 1f);
                    _compactMinWidth = Mathf.Ceil(CompactSeedsTextInsetDp + width);
                    _seedsShown = -1; // _buf se reutiliza: la cifra se vuelve a escribir en el próximo Refresh
                }

                return _compactMinWidth;
            }
        }

        // Lo que la píldora de Semillas de la cabecera deja a los lados de su cifra: 22 dp del icono y 6 a la derecha
        // (ArrangeChildren).
        private const float CompactSeedsTextInsetDp = 22f + 6f;

        /// <summary>Rectángulo de la píldora o placa de Semillas (tests de layout).</summary>
        public RectTransform SeedsRect => _seeds;

        /// <summary>Rectángulo de la píldora o placa de Canoas (tests de layout).</summary>
        public RectTransform CanoesRect => _canoes;

        /// <summary>
        /// La columna Semillas/Canoas de la cabecera de partida va a la vista y compactada: píldoras de menos de
        /// <see cref="CompactPillDp"/>, a escala (<see cref="Layout(float, float, float, float, float, float)"/>).
        /// </summary>
        public bool Squeezed => _root.gameObject.activeSelf && _compact && _canoes.gameObject.activeSelf && _columnPill < CompactPillDp;

        public void SetVisible(bool visible)
        {
            if (_root.gameObject.activeSelf != visible)
            {
                _root.gameObject.SetActive(visible);
                _seedsShown = -1;
                _modeShown = -1;
                _timerShown = long.MinValue;
            }
        }

        /// <summary>Pasa de la forma del embudo a la de la cabecera de partida (o al revés) y recoloca las piezas.</summary>
        public void SetCompact(bool compact)
        {
            if (_compact == compact)
            {
                return;
            }

            _compact = compact;
            _seedsShown = -1;
            _modeShown = -1;
            _timerShown = long.MinValue;
            ArrangeChildren();
            Arrange();
        }

        /// <summary>
        /// Coloca la barra en la fila superior (<paramref name="top"/>), empezando en <paramref name="left"/>. En la cabecera de
        /// partida, <paramref name="width"/> es el ancho de las píldoras (el hueco entre la insignia y Reiniciar) y, desde el 31,
        /// la columna Semillas/Canoas va a su tamaño: píldoras de <see cref="CompactPillDp"/> con <see cref="CompactGapDp"/> de
        /// hueco desde <c>top + 1</c> (hasta <c>top + 49</c>).
        /// </summary>
        public void Layout(float left, float top, float width = 92f)
        {
            Layout(left, top, width, CompactPillDp, CompactGapDp, top + 1f);
        }

        /// <summary>
        /// Como <see cref="Layout(float, float, float)"/>, con la columna Semillas/Canoas de la cabecera de partida (desde el 31)
        /// en píldoras de <paramref name="pillDp"/> de alto (como mucho <see cref="CompactPillDp"/>) con <paramref name="gapDp"/>
        /// de hueco, desde <paramref name="columnTop"/> (dp desde arriba). Las píldoras más bajas se dibujan a escala
        /// <c>pillDp / 22</c>, cifras e iconos incluidos, centradas en el ancho (la pantalla de nivel la compacta así dentro de la
        /// fila cuando el tablero pasa bajo el HUD: <c>GameScreen.LabelsWalletColumn</c>). Sin Canoas, Semillas va centrada en
        /// la fila a su tamaño; en el saldo grande del embudo y de la victoria, la columna no cuenta y todo va a escala 1.
        /// </summary>
        public void Layout(float left, float top, float width, float pillDp, float gapDp, float columnTop)
        {
            _left = left;
            _top = top;
            _width = Mathf.Max(64f, width);
            _columnPill = Mathf.Clamp(pillDp, 1f, CompactPillDp);
            _columnGap = Mathf.Max(0f, gapDp);
            _columnTop = columnTop;
            Arrange();
        }

        /// <summary>
        /// Refresca las cifras de <paramref name="wallet"/>; las canoas solo si <paramref name="showCanoes"/>. Con
        /// <paramref name="seedsPending"/> &gt; 0 (la Cosecha de la victoria mientras cuenta hacia arriba, 02 §6), el saldo
        /// enseña lo ya contado.
        /// </summary>
        public void Refresh(Wallet? wallet, bool showCanoes, int seedsPending = 0)
        {
            if (!_root.gameObject.activeSelf || wallet == null)
            {
                return;
            }

            int seeds = Math.Max(0, wallet.Seeds - Math.Max(0, seedsPending));
            if (seeds != _seedsShown)
            {
                _seedsShown = seeds;
                int n = UiText.WriteNumber(_buf, 0, seeds, true);
                _seedsText.SetCharArray(_buf, 0, n);
            }

            if (_canoes.gameObject.activeSelf != showCanoes)
            {
                _canoes.gameObject.SetActive(showCanoes);
                _modeShown = -1;
                if (_compact)
                {
                    Arrange(); // en la cabecera, Semillas se centra en la fila o sube encima de Canoas
                }
            }

            if (!showCanoes)
            {
                return;
            }

            CanoesStatus canoes = wallet.Canoes;
            int mode = canoes.IsInfinite ? 2 : (canoes.Count <= 0 ? 0 : 1);
            if (mode != _modeShown || canoes.Count != _countShown)
            {
                _modeShown = mode;
                _countShown = canoes.Count;
                _canoesIcon.sprite = mode == 0 ? _art.IconCanoeEmpty : _art.IconCanoe;
                _infinite.gameObject.SetActive(mode == 2);
                _canoesCount.gameObject.SetActive(mode != 2);
                if (mode != 2)
                {
                    int n = UiText.WriteNumber(_buf, 0, canoes.Count, false);
                    _buf[n++] = '/';
                    n = UiText.WriteNumber(_buf, n, canoes.Max, false);
                    _canoesCount.SetCharArray(_buf, 0, n);
                }

                _timerShown = long.MinValue;
            }

            // En la cabecera de partida, «n/5» sin cronómetro (02 §5: Canoas "4/5").
            long seconds = _compact ? -1L : canoes.IsInfinite ? Ceil(canoes.InfiniteRemaining) : (canoes.IsFull ? -1L : Ceil(canoes.TimeToNext));
            if (seconds != _timerShown)
            {
                bool layout = (seconds < 0) != (_timerShown < 0) || _timerShown == long.MinValue;
                _timerShown = seconds;
                _timer.gameObject.SetActive(seconds >= 0);
                if (seconds >= 0)
                {
                    int n = UiText.WriteClock(_buf, 0, seconds);
                    _timer.SetCharArray(_buf, 0, n);
                }

                if (layout)
                {
                    ArrangeCanoeText(seconds >= 0);
                }
            }
        }

        private static long Ceil(TimeSpan span)
        {
            return span <= TimeSpan.Zero ? 0L : (span.Ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond;
        }

        private static void SetScale(RectTransform rt, float scale)
        {
            if (rt.localScale.x != scale || rt.localScale.y != scale)
            {
                rt.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // Placas en la fila: en el embudo, 112 + 12 + 128 dp en línea; en la cabecera de partida, píldoras de 22 dp en columna
        // (Semillas encima de Canoas desde el 31, con la columna de Layout: a escala si va compactada, centradas en el ancho) o
        // Semillas sola centrada en la fila de 48 dp.
        private void Arrange()
        {
            if (!_compact)
            {
                SetScale(_seeds, 1f);
                SetScale(_canoes, 1f);
                UiBuilder.Place(_seeds, _left, _top + 4f, 112f, 40f);
                UiBuilder.Place(_canoes, _left + 124f, _top + 4f, 128f, 40f);
                return;
            }

            bool canoes = _canoes.gameObject.activeSelf;
            float scale = canoes ? _columnPill / CompactPillDp : 1f;
            float x = _left + (_width * (1f - scale) * 0.5f);
            float seedsTop = canoes ? _columnTop : _top + ((48f - CompactPillDp) * 0.5f);
            SetScale(_seeds, scale);
            SetScale(_canoes, scale);
            UiBuilder.Place(_seeds, x, seedsTop, _width, CompactPillDp);
            UiBuilder.Place(_canoes, x, _columnTop + _columnPill + _columnGap, _width, CompactPillDp);
            UiBuilder.Place(_canoesPlate.rectTransform, 0f, 0f, _width, CompactPillDp);
            ArrangeCanoeText(false);
        }

        // Piezas de cada placa según la forma (icono, cifra, contorno de la placa).
        private void ArrangeChildren()
        {
            if (_compact)
            {
                _seedsPlate.pixelsPerUnitMultiplier = CompactSliceMultiplier;
                _canoesPlate.pixelsPerUnitMultiplier = CompactSliceMultiplier;
                UiBuilder.Place(_seedsIcon.rectTransform, 1f, 1f, 20f, 20f); // 07 §7.4: icono de 20 dp
                _seedsText.rectTransform.anchorMin = new Vector2(0f, 0f);
                _seedsText.rectTransform.anchorMax = new Vector2(1f, 1f);
                _seedsText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _seedsText.rectTransform.offsetMin = new Vector2(22f, 0f);
                _seedsText.rectTransform.offsetMax = new Vector2(-6f, 0f);
                UiBuilder.AutoSize(_seedsText, 12f, 15f);
                UiBuilder.Place(_canoesIcon.rectTransform, 1f, 3f, 28f, 16f);
                UiBuilder.AutoSize(_canoesCount, 12f, 15f);
                return;
            }

            _seedsPlate.pixelsPerUnitMultiplier = 1f;
            _canoesPlate.pixelsPerUnitMultiplier = 1f;
            UiBuilder.Place(_seedsIcon.rectTransform, -8f, 1f, 38f, 38f);
            UiBuilder.Place(_seedsText.rectTransform, 30f, 0f, 74f, 40f);
            UiBuilder.AutoSize(_seedsText, 14f, 20f);
            UiBuilder.Place(_canoesPlate.rectTransform, 0f, 0f, 128f, 40f);
            UiBuilder.Place(_canoesIcon.rectTransform, -8f, 7f, 48f, 28f);
            UiBuilder.AutoSize(_canoesCount, 14f, 18f);
        }

        // Cifra, cronómetro e ∞ de la placa de Canoas.
        private void ArrangeCanoeText(bool timer)
        {
            if (_compact)
            {
                float textX = 30f;
                float textW = Mathf.Max(20f, _width - textX - 6f);
                UiBuilder.Place(_canoesCount.rectTransform, textX, 0f, textW, CompactPillDp);
                UiBuilder.Place(_infinite.rectTransform, textX + ((textW - 22f) * 0.5f), 5f, 22f, 12f);
                UiBuilder.Place(_timer.rectTransform, textX, 0f, textW, CompactPillDp);
                return;
            }

            UiBuilder.Place(_canoesCount.rectTransform, 40f, timer ? 2f : 0f, 82f, timer ? 21f : 40f);
            UiBuilder.Place(_timer.rectTransform, 40f, 22f, 82f, 16f);
            UiBuilder.Place(_infinite.rectTransform, 40f + ((82f - 26f) * 0.5f), timer ? 6f : 13f, 26f, 14f);
        }
    }
}
