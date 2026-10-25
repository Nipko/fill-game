#nullable enable
using System;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// La tarjeta «Liga» del muelle del inicio (D-050, punto 6; D-046; liga.md §1: «un botón "Liga" en el inicio, desde el 41, con su
    /// icono»), una <see cref="DockCard"/> en un lienzo propio encima del inicio (como <see cref="MarketLobbyIcons"/>): el podio
    /// sobre el disco índigo, «Liga» y, si el jugador está en el top 100 de la semana, «Puesto {n}» (<c>home_card_league_rank</c>).
    /// Solo con la Liga activa; va en la fila de las tarjetas del día, tras el Reto y la Bitácora (la coloca
    /// <c>GameBootstrap.PlaceDayCards</c>). Sin asignaciones mientras nada cambia.
    /// </summary>
    internal sealed class LeagueLobbyButton
    {
        /// <summary>Orden del lienzo: el de las entradas del Mercado (6).</summary>
        public const int SortingOrder = 6;

        private readonly OverlayCanvas _canvas;
        private readonly DockCard _card;
        private int _rank = -2;

        public LeagueLobbyButton(Transform host, ProceduralArt art, Action onPress)
        {
            _canvas = new OverlayCanvas("LeagueButtonCanvas", host, SortingOrder);
            _card = new DockCard(_canvas.Root, art, "LeagueTab", art.CardIconLeague, onPress);
            SetRank(null);
            A11yLabels.Set(_card.Button, () => UiText.HomeCardA11y(LeagueText.Button, _card.State.text));
        }

        public Canvas Canvas => _canvas.Canvas;

        /// <summary>Lienzo propio (dp de la pantalla, como el del inicio).</summary>
        internal OverlayCanvas Overlay => _canvas;

        public Button Button => _card.Button;

        public RectTransform Rect => _card.Root;

        public bool Shown => _card.Visible;

        /// <summary>Título («Liga»).</summary>
        public string Label => _card.Title.text;

        /// <summary>Segunda línea («Puesto {n}» o vacía).</summary>
        public string RankText => _card.State.text;

        /// <summary>La tarjeta del muelle (tests de desbordes).</summary>
        internal DockCard Card => _card;

        /// <summary>
        /// El puesto del jugador en la semana (null fuera del top 100 o sin clasificación): «Puesto {n}» bajo «Liga». Solo asigna
        /// texto si cambia.
        /// </summary>
        public bool SetRank(int? rank)
        {
            int r = rank.HasValue && rank.Value > 0 ? rank.Value : -1;
            if (r == _rank)
            {
                return false;
            }

            _rank = r;
            _card.SetTexts(LeagueText.Button, r > 0 ? UiText.HomeCardLeagueRank(r) : string.Empty, Palette.LeafShine);
            return true;
        }

        /// <summary>Alto que pide la tarjeta a <paramref name="width"/> dp con sus textos.</summary>
        public float HeightFor(float width) => _card.HeightFor(width);

        /// <summary>Coloca la tarjeta en (<paramref name="x"/>, <paramref name="y"/>) dp con <paramref name="w"/> × <paramref name="h"/>.</summary>
        public void Place(float x, float y, float w, float h)
        {
            _card.Place(x, y, w, h);
        }

        /// <summary>Sin la línea del puesto (<see cref="DockCard.SetCompact"/>).</summary>
        public void SetCompact(bool on) => _card.SetCompact(on);

        /// <summary>Enseña la tarjeta.</summary>
        public void Show()
        {
            _canvas.Measure();
            _card.SetShown(true);
        }

        /// <summary>Vuelve a medir la pantalla; true si cambió.</summary>
        public bool Remeasure() => _canvas.Measure();

        public void Hide()
        {
            _card.SetShown(false);
        }
    }
}
