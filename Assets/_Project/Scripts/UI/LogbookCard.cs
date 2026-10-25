#nullable enable
using System;
using AF.LiveOps;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Tarjeta de la Bitácora del río en el muelle del inicio (D-050, punto 6; B4; 06 §4.2 «Pantalla»: «El icono del lobby muestra
    /// un punto si hay casilla pendiente»; 06 §4.7: «tarjetas Reto del río y Bitácora»; 06 §2.3, pantalla 4: «Bitácora (sellada
    /// hoy)»), una <see cref="DockCard"/>: la libreta sobre el disco ámbar, «Bitácora» y una línea de estado corta (premio de hoy,
    /// sellada hoy o juega un nivel) con el <b>punto</b> (el de la pestaña del Mapa: disco heliconia con borde crema) cuando la
    /// casilla de hoy se puede sellar. Oculta mientras la Bitácora no existe para el jugador (D-008: antes de
    /// <c>live_unlock_level_daily</c> o con <c>biz_feature_login_calendar</c> apagado). Tocarla abre la libreta. TalkBack la lee
    /// con el nombre entero y el estado largo (<c>home_card_a11y</c>).
    /// </summary>
    public sealed class LogbookCard
    {
        /// <summary>Diámetro del punto (dp), el de la pestaña del Mapa.</summary>
        public const float DotDp = DockCard.DotDp;

        private readonly DockCard _card;
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
            _card = new DockCard(parent, art, "LogbookCard", art.CardIconLogbook, OnTap);
            A11yLabels.Set(_card.Button, () => UiText.HomeCardA11y(LogbookText.Title, LogbookText.Plain(StateKey(_shown))));
        }

        /// <summary>Raíz (para colocarla).</summary>
        public RectTransform Root => _card.Root;

        /// <summary>Área táctil (la tarjeta entera).</summary>
        public Button Button => _card.Button;

        /// <summary>Se ve.</summary>
        public bool Visible => _card.Visible;

        /// <summary>Estado que muestra.</summary>
        public LoginCalendarState State => _shown;

        /// <summary>Se ve el punto (casilla de hoy pendiente).</summary>
        public bool DotShown => _card.DotShown;

        /// <summary>Título que se ve («Bitácora»).</summary>
        public string TitleText => _card.Title.text;

        /// <summary>Línea de estado que se ve.</summary>
        public string StateText => _card.State.text;

        /// <summary>La tarjeta del muelle (tests de desbordes).</summary>
        internal DockCard Card => _card;

        /// <summary>Ancho con el que está colocada (dp).</summary>
        public float Width => _card.Width;

        /// <summary>Alto con el que está colocada (dp).</summary>
        public float Height => _card.Height;

        /// <summary>Alto que pide la tarjeta a <paramref name="width"/> dp con sus textos.</summary>
        public float HeightFor(float width) => _card.HeightFor(width);

        /// <summary>Coloca la tarjeta en (<paramref name="x"/>, <paramref name="y"/>) dp con <paramref name="w"/> × <paramref name="h"/>.</summary>
        public void Place(float x, float y, float w, float h)
        {
            _card.Place(x, y, w, h);
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
                _card.SetShown(false);
                return;
            }

            string text;
            Color color;
            switch (state)
            {
                case LoginCalendarState.Ready:
                    text = UiText.HomeCardLogbookReady;
                    color = Palette.LeafShine;
                    break;
                case LoginCalendarState.Sealed:
                    text = LogbookText.Plain(LogbookText.Keys.CardSealed);
                    color = Palette.Cream;
                    break;
                default:
                    text = UiText.HomeCardLogbookPlay;
                    color = Palette.FiberLight;
                    break;
            }

            _card.SetTexts(UiText.HomeCardLogbook, text, color);
            _card.SetDot(state == LoginCalendarState.Ready);
            _card.SetShown(true);
        }

        /// <summary>Sin la línea de estado (<see cref="DockCard.SetCompact"/>): el punto del icono dice si hay página por sellar.</summary>
        public void SetCompact(bool on) => _card.SetCompact(on);

        /// <summary>Oculta la tarjeta sin cambiar su estado (con algo encima del inicio).</summary>
        public void SetShown(bool shown)
        {
            _card.SetShown(shown && _shown != LoginCalendarState.Hidden);
        }

        /// <summary>Clave de la línea de estado larga de <paramref name="state"/> (la que lee TalkBack).</summary>
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
