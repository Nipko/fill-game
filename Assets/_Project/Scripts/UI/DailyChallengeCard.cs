#nullable enable
using System;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Tarjeta «Reto del río» del inicio (tarea R9; 06 §4.1 «Pantalla»; 06 §4.7; nota §8): «Reto del río · {fecha corta}» (fecha
    /// con el formato del idioma, <see cref="DailyText.DateShort"/>), el icono de la banda con 1-3 olas, el estado según
    /// <see cref="DailyChallengeService.CardState"/> (Oculta, En curso, Pendiente de validar, Sellado, Jugar) y la racha
    /// «{n}/7» (<see cref="DailyChallengeRules.StreakShown"/>). Oculta (D-008) no se ve ni recibe toques. Un toque llama a quien la
    /// creó con el estado que tenía (jugar o seguir abre la partida; sellado o pendiente, el calendario del mes [P]).
    /// <para>Gráficos provisionales por código (D-003, «Selva brillante premium»: madera y luz; <c>docs/art/direccion-visual-v2.md</c>):
    /// la superficie de madera de los modales, una placa de agua con las olas dibujadas por código (<see cref="WaveSprite"/>) y un
    /// sello verde hoja (el de los CTA, <see cref="DailyStampCalendar.StampColor"/>; nunca rojo, guía v2 §7) cuando el día está sellado. Sin arte nuevo ni paquetes.</para>
    /// <para>Se construye bajo un padre cualquiera: hoy, el lienzo propio que crea <c>GameBootstrap.SetUpDaily</c> encima del
    /// inicio; con el enganche E1, dentro de <c>LobbyScreen</c>. <see cref="Refresh"/> asigna textos (no por fotograma).</para>
    /// </summary>
    public sealed class DailyChallengeCard
    {
        /// <summary>Ancho de la tarjeta (dp) en un lienzo de 360 dp o más.</summary>
        public const float WidthDp = 296f;

        /// <summary>
        /// Franja de la derecha que la tarjeta deja libre (dp) [P]: la del widget de la Crecida del inicio (<c>LobbyScreen</c>, de
        /// w − 30 a w − 4) y 2 dp de aire. A 360 dp la tarjeta, centrada, ya llega justo a w − 32.
        /// </summary>
        public const float RightClearDp = 32f;

        /// <summary>Margen izquierdo mínimo de la tarjeta (dp) [P]: el de la ventanita de casa y los saldos del inicio.</summary>
        public const float LeftGutterDp = 16f;

        /// <summary>Ancho mínimo de la tarjeta (dp): el de un lienzo de 320 dp, el más estrecho (Q-B11-5 (a)).</summary>
        public const float MinWidthDp = 320f - LeftGutterDp - RightClearDp;

        /// <summary>
        /// Alto de la tarjeta (dp; el área táctil es la tarjeta entera, ≥ 48 dp, 07 §7.8): 100 para que el título y el estado
        /// quepan en dos líneas cada uno (en-XA al 130 %, revisión de R12).
        /// </summary>
        public const float HeightDp = 100f;

        /// <summary>Olas del icono de la banda (una por banda, 1-3).</summary>
        public const int MaxWaves = 3;

        // Hueco del sello a la derecha del título (dp): el sello de 28 dp a 12 del borde, más 4 de aire.
        private const float StampSlotDp = 44f;

        private static Sprite? s_wave;

        private readonly Action<DailyCardState> _onTap;
        private readonly RectTransform _root;
        private readonly Button _button;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _state;
        private readonly TextMeshProUGUI _streak;
        private readonly Image _bandPlate;
        private readonly Image[] _waves = new Image[MaxWaves];
        private readonly Image _stamp;
        private readonly Image _surface;
        private readonly ProceduralArt _art;
        private DailyCardState _shown = DailyCardState.Hidden;
        private int _band;
        private float _width = WidthDp;
        private float _plateDp = 56f;
        private float _height = HeightDp;
        private float _sizedW = WidthDp;
        private float _sizedH = HeightDp;
        private float _x;
        private float _y;

        // Lado de la placa de la banda en una tarjeta estrecha (dp) [P].
        private const float NarrowPlateDp = 44f;

        /// <summary>Construye la tarjeta (oculta) bajo <paramref name="parent"/>.</summary>
        public DailyChallengeCard(Transform parent, ProceduralArt art, Action<DailyCardState> onTap)
        {
            if (art == null)
            {
                throw new ArgumentNullException(nameof(art));
            }

            _onTap = onTap ?? throw new ArgumentNullException(nameof(onTap));
            _art = art;
            _root = ModalKit.Card("DailyChallengeCard", parent, art, out _surface);
            ModalKit.Size(_root, _surface, art, 0f, 0f, WidthDp, HeightDp);

            _bandPlate = UiBuilder.Image("BandPlate", _root, art.RoundedRect, Palette.WaterClear);
            for (int i = 0; i < MaxWaves; i++)
            {
                _waves[i] = UiBuilder.Image("Wave" + (i + 1), _bandPlate.transform, WaveSprite(), Palette.Cream);
            }

            _title = UiBuilder.Label("Title", _root, TextStyle.PlainBold, string.Empty, 17f, Palette.UiInk, TextAlignmentOptions.MidlineLeft);
            // El título no pasa por debajo del sello (esquina superior derecha, StampSlotDp): revisión de R11 (en-XA). Hasta dos
            // líneas: «Reto del río · {fecha}» largo (en-XA al 130 %, 360 × 640) baja de línea en vez de salirse (revisión de R12).
            _title.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_title, UiBuilder.MinTextSp, 17f);
            _state = UiBuilder.Label("State", _root, TextStyle.PlainBold, string.Empty, 15f, Palette.UiGreenButton, TextAlignmentOptions.MidlineLeft);
            // Hasta dos líneas: un estado largo (Pendiente de validar en en-XA) baja de línea en vez de pisar la racha (R11).
            _state.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_state, UiBuilder.MinTextSp, 15f);
            _streak = UiBuilder.Label("Streak", _root, TextStyle.PlainBold, string.Empty, 15f, Palette.UiWood, TextAlignmentOptions.MidlineRight);
            UiBuilder.AutoSize(_streak, UiBuilder.MinTextSp, 15f);

            _stamp = UiBuilder.Image("Stamp", _root, art.Disc, DailyStampCalendar.StampColor);
            Image ring = UiBuilder.Image("Ring", _stamp.transform, art.Ring, Palette.Cream);
            UiBuilder.Stretch(ring.rectTransform);

            _button = UiBuilder.BareButton("Tap", _root, OnTap);
            UiBuilder.Stretch((RectTransform)_button.transform);
            LayoutInner();
            _root.gameObject.SetActive(false);
        }

        /// <summary>Raíz de la tarjeta (para colocarla).</summary>
        public RectTransform Root => _root;

        /// <summary>Área táctil (la tarjeta entera).</summary>
        public Button Button => _button;

        /// <summary>Se ve (estado distinto de Oculta).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Estado que muestra.</summary>
        public DailyCardState State => _shown;

        /// <summary>Banda del icono (1-3), 0 oculta.</summary>
        public int Band => _band;

        /// <summary>Olas que se ven en el icono.</summary>
        public int WavesShown
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _waves.Length; i++)
                {
                    if (_waves[i].gameObject.activeSelf)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Se ve el sello (Sellado o Pendiente de validar).</summary>
        public bool StampShown => _stamp.gameObject.activeSelf;

        /// <summary>Título («Reto del río · {fecha corta}»).</summary>
        public string TitleText => _title.text;

        /// <summary>Texto del estado.</summary>
        public string StateText => _state.text;

        /// <summary>Racha «{n}/7».</summary>
        public string StreakText => _streak.text;

        /// <summary>
        /// Última línea <c>AF_STATE daily_card</c> que escribió <see cref="Refresh"/> (tests y <c>adb</c>; R10), o null fuera de
        /// desarrollo o antes del primer recálculo.
        /// </summary>
        public string? LastProbe { get; private set; }

        /// <summary>
        /// Ola de las bandas, dibujada por código (una vez por sesión): un trazo sinusoidal blanco con bordes suaves de 48 × 16 px,
        /// que se tiñe con el color de la imagen.
        /// </summary>
        public static Sprite WaveSprite()
        {
            if (s_wave != null)
            {
                return s_wave;
            }

            const int w = 48;
            const int h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "DailyWave", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Dos crestas en el ancho; trazo de ~2,6 px con 1 px de borde suave.
                    float cy = (h - 1) * 0.5f + (Mathf.Sin(x / (float)(w - 1) * Mathf.PI * 4f) * 4.5f);
                    float d = Mathf.Abs(y - cy);
                    float a = Mathf.Clamp01(2.3f - d);
                    pixels[(y * w) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            s_wave = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
            s_wave.name = "DailyWave";
            return s_wave;
        }

        /// <summary>Ancho con el que está colocada (dp; <see cref="WidthFor"/>).</summary>
        public float Width => _width;

        /// <summary>
        /// Ancho de la tarjeta en un lienzo de <paramref name="canvasW"/> dp: <see cref="WidthDp"/> centrada mientras deja libre la
        /// franja de la Crecida (<see cref="RightClearDp"/>); en lienzos más estrechos (320 dp: 272), lo que queda entre
        /// <see cref="LeftGutterDp"/> y esa franja. A 320 dp, centrada a 296 dp (x 12-308), pisaba el widget de la Crecida
        /// (x 290-316) [P].
        /// </summary>
        public static float WidthFor(float canvasW)
        {
            return Mathf.Clamp(canvasW - LeftGutterDp - RightClearDp, MinWidthDp, WidthDp);
        }

        /// <summary>Borde izquierdo de la tarjeta (dp) en un lienzo de <paramref name="canvasW"/> dp (ver <see cref="WidthFor"/>).</summary>
        public static float LeftFor(float canvasW)
        {
            float width = WidthFor(canvasW);
            return Mathf.Min((canvasW - width) * 0.5f, canvasW - RightClearDp - width);
        }

        /// <summary>Coloca la tarjeta con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="y"/>) dp, a <see cref="WidthDp"/>.</summary>
        public void Place(float x, float y)
        {
            Place(x, y, WidthDp);
        }

        /// <summary>
        /// Alto de la tarjeta (dp): <see cref="HeightDp"/> o, si el título y el estado no caben en dos líneas cada uno a su mínimo
        /// (en-XA al 130 % con «Pendiente de validar»), lo que necesiten (02 §12: los contenedores crecen) [P]. Quien la coloca
        /// deja fijo su borde de abajo y la hace crecer hacia arriba.
        /// </summary>
        public float Height => _height;

        /// <summary>Coloca la tarjeta con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="y"/>) dp y <paramref name="width"/> dp de ancho.</summary>
        public void Place(float x, float y, float width)
        {
            if (!Mathf.Approximately(width, _width))
            {
                _width = width;
                LayoutInner();
            }

            _x = x;
            _y = y;
            if (!Mathf.Approximately(_width, _sizedW) || !Mathf.Approximately(_height, _sizedH))
            {
                _sizedW = _width;
                _sizedH = _height;
                ModalKit.Size(_root, _surface, _art, x, y, _width, _height);
                return;
            }

            UiBuilder.Place(_root, x, y, _width, _height);
        }

        // Lo de dentro, a lo ancho de la tarjeta: la placa de la banda a la izquierda, el título hasta el sello (StampSlotDp), la racha
        // pegada a la derecha, a 32 dp del borde, y el estado entre la placa y la racha. A 296 dp, placa de 56, texto desde 80 y
        // racha de 52 (estado de 132 dp). Más estrecha (320 dp: 272), la placa baja a 44 dp, el texto empieza en 64 y la racha
        // mide 40, así que el estado conserva sus 132 dp y «Pendiente de validar» cabe al 130 % como a 360 dp [P].
        private void LayoutInner()
        {
            float w = _width;
            bool narrow = w < WidthDp - 0.5f;
            float plate = narrow ? NarrowPlateDp : 56f;
            float plateX = narrow ? 12f : 14f;
            float textX = narrow ? 64f : 80f;
            float streakW = narrow ? 40f : 52f;
            // El título (44 dp) y el estado (36 dp) crecen lo que necesite su texto a su mínimo (02 §12: los contenedores crecen;
            // «Pendiente de validar» en dos líneas al 130 % pide ~42 dp) y la tarjeta con ellos, desde los 100 dp de siempre.
            float titleW = w - StampSlotDp - textX;
            float stateW = w - 32f - streakW - textX;
            float titleH = Mathf.Max(44f, Mathf.Ceil(UiBuilder.MinTextHeight(_title, titleW)));
            float stateH = Mathf.Max(36f, Mathf.Ceil(UiBuilder.MinTextHeight(_state, stateW)));
            _height = Mathf.Max(HeightDp, 8f + titleH + 4f + stateH + 4f);
            float h = _height;
            UiBuilder.Place(_bandPlate.rectTransform, plateX, (h - plate) * 0.5f, plate, plate);
            _plateDp = plate;
            if (_band > 0)
            {
                PlaceWaves(_band);
            }

            UiBuilder.Place(_title.rectTransform, textX, 8f, titleW, titleH);
            UiBuilder.Place(_state.rectTransform, textX, 8f + titleH + 4f, stateW, stateH);
            UiBuilder.Place(_streak.rectTransform, w - 32f - streakW, h - 39f, streakW, 26f);
            UiBuilder.Place(_stamp.rectTransform, w - 40f, 8f, 28f, 28f);
        }

        /// <summary>
        /// Recalcula el estado (nota §8) y los textos para un jugador de nivel máximo <paramref name="maxLevel"/>. Oculta: se oculta
        /// entera. Asigna cadenas: llamarla al mostrar el inicio o al volver de una partida, no por fotograma.
        /// </summary>
        public DailyCardState Refresh(DailyChallengeService service, IClock clock, int maxLevel)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            DailyCardState state = service.CardState(maxLevel);
            _shown = state;
            Probe(state, service, clock, maxLevel);
            if (state == DailyCardState.Hidden)
            {
                _band = 0;
                _root.gameObject.SetActive(false);
                return state;
            }

            DailyPlayInProgress? resumable = service.Resumable;
            _band = resumable != null ? resumable.Band : service.TodaySlot(maxLevel).Band;
            if (_band < 1)
            {
                _band = 1;
            }
            else if (_band > MaxWaves)
            {
                _band = MaxWaves;
            }

            PlaceWaves(_band);
            _title.text = DailyText.CardTitle(clock.LocalDate());
            _state.text = DailyText.State(state);
            _streak.text = DailyText.Streak(service.Rules.StreakShown(), DailyChallengeRules.StreakLength);
            bool stamped = state == DailyCardState.Sealed || state == DailyCardState.PendingValidation;
            _stamp.gameObject.SetActive(stamped);
            LayoutInner(); // el alto del estado depende de su texto
            Color stampColor = DailyStampCalendar.StampColor;
            _stamp.color = state == DailyCardState.PendingValidation ? new Color(stampColor.r, stampColor.g, stampColor.b, 0.45f) : stampColor;
            _root.gameObject.SetActive(true);
            return state;
        }

        /// <summary>Solo tests: pone el texto de <paramref name="state"/> en la línea de estado y recoloca, como <see cref="Refresh"/>.</summary>
        internal void ShowStateTextForTests(DailyCardState state)
        {
            _state.text = DailyText.State(state);
            float bottom = _y + _height;
            LayoutInner();
            Place(_x, bottom - _height, _width);
        }

        /// <summary>Oculta la tarjeta sin cambiar su estado (p. ej. con una partida del Reto abierta o un modal encima).</summary>
        public void SetShown(bool shown)
        {
            bool visible = shown && _shown != DailyCardState.Hidden;
            if (_root.gameObject.activeSelf != visible)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        // Olas apiladas y centradas en la placa (de 56 dp: 40 × 13, separadas 12 dp; en la de 44, a escala): 1 → centro; 2 y 3 → apiladas.
        private void PlaceWaves(int count)
        {
            float k = _plateDp / 56f;
            float waveW = 40f * k;
            float waveH = 13f * k;
            float gap = 12f * k;
            float left = (_plateDp - waveW) * 0.5f;
            float top = (_plateDp - (waveH + (gap * (count - 1)))) * 0.5f;
            for (int i = 0; i < _waves.Length; i++)
            {
                bool on = i < count;
                _waves[i].gameObject.SetActive(on);
                if (on)
                {
                    UiBuilder.Place(_waves[i].rectTransform, left, top + (gap * i), waveW, waveH);
                }
            }
        }

        // R10: AF_STATE daily_card en cada recálculo: el estado, el slot de hoy (banda y día; con la partida de hoy, su banda
        // congelada, R-Q3), el tablero que se jugaría o se reanuda, la racha mostrada y la sustitución pedida o ignorada
        // (daily_overrides). Oculta: sin slot ni tablero.
        private void Probe(DailyCardState state, DailyChallengeService service, IClock clock, int maxLevel)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            DailySlot? slot = null;
            string? board = null;
            AF.Levels.DailyPool? pool = service.Pool;
            if (state != DailyCardState.Hidden && pool != null)
            {
                DailySlot today = service.TodaySlot(maxLevel);
                DailyPlayInProgress? resumable = service.Resumable;
                if (resumable != null)
                {
                    slot = new DailySlot(resumable.Band, today.Day, today.OverrideId, today.RequestedOverrideId);
                    board = resumable.BoardId;
                }
                else
                {
                    slot = today;
                    board = today.Board(pool)?.Id;
                }
            }

            LastProbe = DevProbe.DailyCard(state, clock.LocalDateKey(), slot, board, service.Rules.StreakShown());
        }

        private void OnTap()
        {
            if (_shown != DailyCardState.Hidden)
            {
                _onTap(_shown);
            }
        }
    }
}
