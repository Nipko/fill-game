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
        /// <summary>Ancho de la tarjeta (dp).</summary>
        public const float WidthDp = 296f;

        /// <summary>Alto de la tarjeta (dp; el área táctil es la tarjeta entera, ≥ 48 dp, 07 §7.8).</summary>
        public const float HeightDp = 84f;

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
        private DailyCardState _shown = DailyCardState.Hidden;
        private int _band;

        /// <summary>Construye la tarjeta (oculta) bajo <paramref name="parent"/>.</summary>
        public DailyChallengeCard(Transform parent, ProceduralArt art, Action<DailyCardState> onTap)
        {
            if (art == null)
            {
                throw new ArgumentNullException(nameof(art));
            }

            _onTap = onTap ?? throw new ArgumentNullException(nameof(onTap));
            _root = ModalKit.Card("DailyChallengeCard", parent, art, out Image surface);
            ModalKit.Size(_root, surface, art, 0f, 0f, WidthDp, HeightDp);

            _bandPlate = UiBuilder.Image("BandPlate", _root, art.RoundedRect, Palette.WaterClear);
            UiBuilder.Place(_bandPlate.rectTransform, 14f, 14f, 56f, 56f);
            for (int i = 0; i < MaxWaves; i++)
            {
                _waves[i] = UiBuilder.Image("Wave" + (i + 1), _bandPlate.transform, WaveSprite(), Palette.Cream);
            }

            _title = UiBuilder.Label("Title", _root, TextStyle.PlainBold, string.Empty, 17f, Palette.UiInk, TextAlignmentOptions.MidlineLeft);
            // El título no pasa por debajo del sello (esquina superior derecha, StampSlotDp): revisión de R11 (en-XA).
            UiBuilder.Place(_title.rectTransform, 80f, 12f, WidthDp - StampSlotDp - 80f, 28f);
            UiBuilder.AutoSize(_title, UiBuilder.MinTextSp, 17f);
            _state = UiBuilder.Label("State", _root, TextStyle.PlainBold, string.Empty, 15f, Palette.UiGreenButton, TextAlignmentOptions.MidlineLeft);
            // Hasta dos líneas: un estado largo (Pendiente de validar en en-XA) baja de línea en vez de pisar la racha (R11).
            UiBuilder.Place(_state.rectTransform, 80f, 41f, 132f, 34f);
            _state.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_state, UiBuilder.MinTextSp, 15f);
            _streak = UiBuilder.Label("Streak", _root, TextStyle.PlainBold, string.Empty, 15f, Palette.UiWood, TextAlignmentOptions.MidlineRight);
            UiBuilder.Place(_streak.rectTransform, 212f, 44f, 52f, 26f);
            UiBuilder.AutoSize(_streak, UiBuilder.MinTextSp, 15f);

            _stamp = UiBuilder.Image("Stamp", _root, art.Disc, DailyStampCalendar.StampColor);
            UiBuilder.Place(_stamp.rectTransform, WidthDp - 40f, 8f, 28f, 28f);
            Image ring = UiBuilder.Image("Ring", _stamp.transform, art.Ring, Palette.Cream);
            UiBuilder.Stretch(ring.rectTransform);

            _button = UiBuilder.BareButton("Tap", _root, OnTap);
            UiBuilder.Stretch((RectTransform)_button.transform);
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

        /// <summary>Coloca la tarjeta con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="y"/>) dp.</summary>
        public void Place(float x, float y)
        {
            UiBuilder.Place(_root, x, y, WidthDp, HeightDp);
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
            Color stampColor = DailyStampCalendar.StampColor;
            _stamp.color = state == DailyCardState.PendingValidation ? new Color(stampColor.r, stampColor.g, stampColor.b, 0.45f) : stampColor;
            _root.gameObject.SetActive(true);
            return state;
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

        // Olas apiladas y centradas en la placa de 56 dp: 1 → centro; 2 y 3 → separadas 12 dp.
        private void PlaceWaves(int count)
        {
            const float waveW = 40f;
            const float waveH = 13f;
            const float gap = 12f;
            float top = (56f - (waveH + (gap * (count - 1)))) * 0.5f;
            for (int i = 0; i < _waves.Length; i++)
            {
                bool on = i < count;
                _waves[i].gameObject.SetActive(on);
                if (on)
                {
                    UiBuilder.Place(_waves[i].rectTransform, 8f, top + (gap * i), waveW, waveH);
                }
            }
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
