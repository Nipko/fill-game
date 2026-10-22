#nullable enable
using System;
using AF.LiveOps;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Tarjeta de la Bitácora del río en el inicio (B4; 06 §4.2 «Pantalla»: «El icono del lobby muestra un punto si hay casilla
    /// pendiente»; 06 §4.7: «tarjetas Reto del río y Bitácora»; 06 §2.3, pantalla 4: «Bitácora (sellada hoy)»). Título, una línea
    /// de estado y el <b>punto</b> (el mismo de la pestaña del Mapa: disco heliconia con borde crema) cuando la casilla de hoy se
    /// puede sellar. Oculta mientras la Bitácora no existe para el jugador (D-008: antes de <c>live_unlock_level_daily</c> o con
    /// <c>biz_feature_login_calendar</c> apagado). Tocarla abre la libreta. Gráficos provisionales por código; los textos, de
    /// <see cref="LogbookText"/>.
    /// </summary>
    public sealed class LogbookCard
    {
        /// <summary>Ancho (dp), el de la tarjeta del Reto.</summary>
        public const float WidthDp = DailyChallengeCard.WidthDp;

        /// <summary>Alto (dp) [P].</summary>
        public const float HeightDp = 60f;

        /// <summary>Diámetro del punto (dp), el de la pestaña del Mapa.</summary>
        public const float DotDp = 14f;

        private readonly RectTransform _root;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _state;
        private readonly RectTransform _dot;
        private readonly Button _button;
        private readonly Action _onTap;
        private LoginCalendarState _shown = LoginCalendarState.Hidden;

        /// <summary>Construye la tarjeta (oculta) bajo <paramref name="parent"/>.</summary>
        public LogbookCard(Transform parent, ProceduralArt art, Action onTap)
        {
            if (art == null)
            {
                throw new ArgumentNullException(nameof(art));
            }

            _onTap = onTap ?? throw new ArgumentNullException(nameof(onTap));
            _root = ModalKit.Card("LogbookCard", parent, art, out Image surface);
            ModalKit.Size(_root, surface, art, 0f, 0f, WidthDp, HeightDp);

            Image book = UiBuilder.Image("Book", _root, art.RoundedRect, Palette.UiAmber);
            UiBuilder.Place(book.rectTransform, 14f, 10f, 40f, 40f);
            Image page = UiBuilder.Image("Page", book.transform, art.RoundedRect, Palette.UiPaper);
            UiBuilder.Place(page.rectTransform, 6f, 6f, 28f, 28f);

            _title = UiBuilder.Label("Title", _root, TextStyle.PlainBold, string.Empty, 16f, Palette.UiInk, TextAlignmentOptions.MidlineLeft);
            UiBuilder.Place(_title.rectTransform, 66f, 6f, WidthDp - 66f - 36f, 26f);
            UiBuilder.AutoSize(_title, UiBuilder.MinTextSp, 16f);
            _state = UiBuilder.Label("State", _root, TextStyle.PlainBold, string.Empty, 14f, Palette.UiGreenButton, TextAlignmentOptions.MidlineLeft);
            UiBuilder.Place(_state.rectTransform, 66f, 30f, WidthDp - 66f - 36f, 24f);
            UiBuilder.AutoSize(_state, UiBuilder.MinTextSp, 14f);

            _dot = UiBuilder.Rect("ReadyDot", _root);
            Image rim = UiBuilder.Image("Rim", _dot, art.Disc, Palette.Cream);
            UiBuilder.Stretch(rim.rectTransform);
            Image dot = UiBuilder.Image("Dot", _dot, art.Disc, Palette.Heliconia);
            UiBuilder.Place(dot.rectTransform, 2f, 2f, DotDp - 4f, DotDp - 4f);
            UiBuilder.Place(_dot, WidthDp - DotDp - 12f, 10f, DotDp, DotDp);
            _dot.gameObject.SetActive(false);

            _button = UiBuilder.BareButton("Tap", _root, OnTap);
            UiBuilder.Stretch((RectTransform)_button.transform);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Raíz (para colocarla).</summary>
        public RectTransform Root => _root;

        /// <summary>Área táctil (la tarjeta entera).</summary>
        public Button Button => _button;

        /// <summary>Se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Estado que muestra.</summary>
        public LoginCalendarState State => _shown;

        /// <summary>Se ve el punto (casilla de hoy pendiente).</summary>
        public bool DotShown => _dot.gameObject.activeSelf;

        /// <summary>Título.</summary>
        public string TitleText => _title.text;

        /// <summary>Línea de estado.</summary>
        public string StateText => _state.text;

        /// <summary>Coloca la tarjeta con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="y"/>) dp.</summary>
        public void Place(float x, float y)
        {
            UiBuilder.Place(_root, x, y, WidthDp, HeightDp);
        }

        /// <summary>
        /// Pinta <paramref name="state"/>: oculta con <see cref="LoginCalendarState.Hidden"/>; si no, el título, la línea de estado y
        /// el punto con <see cref="LoginCalendarState.Ready"/>. Asigna cadenas: al mostrar el inicio o al cambiar el estado.
        /// </summary>
        public void Refresh(LoginCalendarState state)
        {
            _shown = state;
            if (state == LoginCalendarState.Hidden)
            {
                _root.gameObject.SetActive(false);
                return;
            }

            _title.text = LogbookText.Title;
            _state.text = LogbookText.Plain(StateKey(state));
            _dot.gameObject.SetActive(state == LoginCalendarState.Ready);
            _root.gameObject.SetActive(true);
        }

        /// <summary>Oculta la tarjeta sin cambiar su estado (con algo encima del inicio).</summary>
        public void SetShown(bool shown)
        {
            bool visible = shown && _shown != LoginCalendarState.Hidden;
            if (_root.gameObject.activeSelf != visible)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        /// <summary>Clave de la línea de estado de <paramref name="state"/>.</summary>
        public static string StateKey(LoginCalendarState state)
        {
            switch (state)
            {
                case LoginCalendarState.Ready: return LogbookText.Keys.CardReady;
                case LoginCalendarState.Sealed: return LogbookText.Keys.CardSealed;
                default: return LogbookText.Keys.CardPlay;
            }
        }

        private void OnTap()
        {
            if (Visible)
            {
                _onTap();
            }
        }
    }
}
