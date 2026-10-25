#nullable enable
using System;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Tarjeta «Reto del río» del muelle del inicio (D-050, punto 6; tarea R9; 06 §4.1 «Pantalla»; 06 §4.7; nota §8), una
    /// <see cref="DockCard"/>: el disco de agua clara con 1-3 olas (la banda del tablero de hoy), «Reto del río · {fecha corta}» (06 §4.1; si no cabe en una línea, en dos), el estado según
    /// <see cref="DailyChallengeService.CardState"/> (Jugar, Seguir, Sellado o Pendiente, la forma corta de «Pendiente de validar»)
    /// y la racha «{n}/7» en la píldora de la esquina (<see cref="DailyChallengeRules.StreakShown"/>); con el día sellado, el sello
    /// verde hoja sobre el disco (<see cref="DailyStampCalendar.StampColor"/>; nunca rojo, guía v2 §7). Oculta (D-008) no se ve
    /// ni recibe toques. Un toque llama a quien la creó con el estado que tenía (jugar o seguir abre la partida; sellado o
    /// pendiente, el calendario del mes [P]). TalkBack la lee entera, con la fecha: «Reto del río · {fecha}: Jugar. Racha: 0 de 7
    /// días» (<c>home_card_daily_a11y</c>). <see cref="Refresh"/> asigna textos (no por fotograma).
    /// </summary>
    public sealed class DailyChallengeCard
    {
        /// <summary>Olas del icono de la banda (una por banda, 1-3).</summary>
        public const int MaxWaves = 3;

        private static Sprite? s_wave;

        private readonly Action<DailyCardState> _onTap;
        private readonly DockCard _card;
        private readonly Image[] _waves = new Image[MaxWaves];
        private DailyCardState _shown = DailyCardState.Hidden;
        private int _band;
        private string _titleFull = string.Empty;
        private string _streak = string.Empty;
        private int _streakShown;
        private bool _dated = true;

        /// <summary>Construye la tarjeta (oculta) bajo <paramref name="parent"/>.</summary>
        public DailyChallengeCard(Transform parent, ProceduralArt art, Action<DailyCardState> onTap)
        {
            if (art == null)
            {
                throw new ArgumentNullException(nameof(art));
            }

            _onTap = onTap ?? throw new ArgumentNullException(nameof(onTap));
            _card = new DockCard(parent, art, "DailyChallengeCard", art.CardIconDaily, OnTap);
            for (int i = 0; i < MaxWaves; i++)
            {
                _waves[i] = UiBuilder.Image("Wave" + (i + 1), _card.IconExtras, WaveSprite(), Palette.Cream);
            }

            A11yLabels.Set(_card.Button, () => UiText.HomeCardDailyA11y(_titleFull, DailyText.State(_shown), _streakShown));
        }

        /// <summary>Raíz de la tarjeta (para colocarla).</summary>
        public RectTransform Root => _card.Root;

        /// <summary>Área táctil (la tarjeta entera).</summary>
        public Button Button => _card.Button;

        /// <summary>Se ve (estado distinto de Oculta).</summary>
        public bool Visible => _card.Visible;

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
        public bool StampShown => _card.StampShown;

        /// <summary>Título, con la fecha («Reto del río · {fecha corta}»; 06 §4.1): el que se ve, en hasta dos líneas, y lee TalkBack.</summary>
        public string TitleText => _card.Title.text;

        /// <summary>Texto del estado.</summary>
        public string StateText => _card.State.text;

        /// <summary>Racha «{n}/7».</summary>
        public string StreakText => _streak;

        /// <summary>La tarjeta del muelle (tests de desbordes).</summary>
        internal DockCard Card => _card;

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

        /// <summary>Ancho con el que está colocada (dp).</summary>
        public float Width => _card.Width;

        /// <summary>Alto con el que está colocada (dp).</summary>
        public float Height => _card.Height;

        /// <summary>Alto que pide la tarjeta a <paramref name="width"/> dp con sus textos (02 §12: los contenedores crecen).</summary>
        public float HeightFor(float width) => _card.HeightFor(width);

        /// <summary>Coloca la tarjeta en (<paramref name="x"/>, <paramref name="y"/>) dp con <paramref name="w"/> × <paramref name="h"/>.</summary>
        public void Place(float x, float y, float w, float h)
        {
            _card.Place(x, y, w, h);
            PlaceWaves(_band);
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
                _card.SetShown(false);
                return state;
            }

            DailyPlayInProgress? resumable = service.Resumable;
            _band = resumable != null ? resumable.Band : service.TodaySlot(maxLevel).Band;
            _band = Mathf.Clamp(_band, 1, MaxWaves);
            PlaceWaves(_band);
            _titleFull = DailyText.CardTitle(clock.LocalDate());
            _streakShown = service.Rules.StreakShown();
            _streak = DailyText.Streak(_streakShown, DailyChallengeRules.StreakLength);
            ShowState(state);
            _card.SetBadge(_streak);
            bool stamped = state == DailyCardState.Sealed || state == DailyCardState.PendingValidation;
            _card.SetStamp(stamped, state == DailyCardState.PendingValidation ? 0.45f : 1f);
            _card.SetShown(true);
            return state;
        }

        // El título visible y el estado corto con su color: Jugar y Seguir en verde claro, Sellado en crema, Pendiente en fibra.
        private void ShowState(DailyCardState state)
        {
            string text = state == DailyCardState.PendingValidation ? UiText.HomeCardDailyPending : DailyText.State(state);
            Color color = state == DailyCardState.Sealed ? Palette.Cream
                : (state == DailyCardState.PendingValidation ? Palette.FiberLight : Palette.LeafShine);
            _card.SetTexts(_dated ? _titleFull : DailyText.Title, text, color);
        }

        /// <summary>
        /// Con la fecha en el título («Reto del río · {fecha corta}», 06 §4.1) o, si la fila de tarjetas no cabe, sin ella («Reto del
        /// río»; la fecha sigue en lo que lee TalkBack). Solo asigna texto si cambia.
        /// </summary>
        public void SetDated(bool dated)
        {
            if (dated == _dated)
            {
                return;
            }

            _dated = dated;
            if (_shown != DailyCardState.Hidden)
            {
                ShowState(_shown);
            }
        }

        /// <summary>El título lleva la fecha (<see cref="SetDated"/>).</summary>
        public bool Dated => _dated;

        /// <summary>Sin la línea de estado (<see cref="DockCard.SetCompact"/>): el sello del icono la dice.</summary>
        public void SetCompact(bool on) => _card.SetCompact(on);

        /// <summary>Solo tests: pone el texto de <paramref name="state"/> en la línea de estado, como <see cref="Refresh"/>.</summary>
        internal void ShowStateTextForTests(DailyCardState state)
        {
            ShowState(state);
        }

        /// <summary>Oculta la tarjeta sin cambiar su estado (p. ej. con una partida del Reto abierta o un modal encima).</summary>
        public void SetShown(bool shown)
        {
            _card.SetShown(shown && _shown != DailyCardState.Hidden);
        }

        // Olas apiladas y centradas en el disco (de 34 dp: 22 × 7, separadas 7 dp): 1 → centro; 2 y 3 → apiladas.
        private void PlaceWaves(int count)
        {
            const float waveW = 22f;
            const float waveH = 7f;
            const float gap = 6.5f;
            float d = DockCard.IconDp;
            float left = (d - waveW) * 0.5f;
            float top = (d - (waveH + (gap * (Mathf.Max(1, count) - 1)))) * 0.5f;
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
