#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Core.Mechanics;
using AF.GameFlow;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial de la tarjeta previa de 1 toque y del tutorial de una pantalla de cada estreno (arquitectura del paso 1, §7 y §8;
    /// tarea T13; 02 §10, §12 y §13; propuesta-mecanicas-dificultad.md §c.1), sin editar GameScreen.cs: implementa
    /// <see cref="ILevelCardListener"/> (T10) y los ganchos <c>Cards*</c> (T04) y pinta la tarjeta con <see cref="LevelCardView"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo</b>: LOADING → LEVEL_CARD → INTRO (02 §2; LevelFlow.Cards.cs). <see cref="OnLevelCard"/> llega tras
    /// <c>OnLevelLoaded</c> y los regalos del nivel, en lugar de <c>OnIntroStarted</c>; 1 toque en cualquier punto llama
    /// directamente a <c>LevelFlow.DismissLevelCard</c>, que arranca la intro (y su <c>OnIntroStarted</c> de siempre). La tarjeta
    /// también se va si el flujo sale de LEVEL_CARD por otro camino (<c>TapAnywhere</c>, <c>TapCuia</c>, el fondo o cargar otro
    /// nivel). No sale al reanudar ni al reintentar: eso lo decide el flujo (02 §14; <c>GameConfig.LevelCardOnRetry</c> [P]).</para>
    /// <para><b>El tablero detrás</b> [P]: la tarjeta va antes de «Llenar y teñir», así que detrás se ve el tablero como en el
    /// fotograma 0 de la intro (las cuias secas y vacías en su sitio, con sus mecánicas), no el tablero lleno que la intro vaciaría
    /// de golpe al cerrar la tarjeta. Para eso la intro arranca con la tarjeta, se lleva a t = 0 (<c>IntroAnimation.Seek</c>) y se
    /// retiene (<c>_introHeld</c>: GameScreen.Tick no la avanza) hasta que el flujo la arranca de verdad (StartIntro la suelta);
    /// si sale de LEVEL_CARD sin arrancarla (el cartel «El río bajó» del reintento con alivio, con <c>LevelCardOnRetry</c>), se
    /// suelta y el tablero queda como en ese caso sin tarjeta.</para>
    /// <para>El banner de entrada a un mundo (GameScreen.WorldBanner.cs) usa los mismos ganchos y la misma retención de la intro
    /// (es LEVEL_CARD con <c>LevelFlow.WorldBanner</c>); si el nivel tiene además tarjeta, sale al cerrar el banner. En el
    /// estreno de la Piraña (183), la tarjeta lleva el mordisco animado (<see cref="PiranaBiteDemo"/>): <c>AF_STATE
    /// level_card</c> lo anota con <c>"bite_demo"</c>.</para>
    /// <para>En desarrollo, <c>AF_STATE {"event":"level_card",…}</c> con el tipo, el nivel, la etiqueta, el estreno, las líneas y
    /// el tamaño del tutorial, los iconos y la escala; y, al terminar la entrada, <c>AF_LAYOUT {"screen":"level_card",…}</c> con
    /// el rectángulo de la tarjeta (cualquier toque la cierra).</para>
    /// </remarks>
    public sealed partial class GameScreen : ILevelCardListener
    {
        private LevelCardView _levelCard = null!;
        private bool _cardsBuilt;
        private string? _lastCardProbe;

        /// <summary>Tarjeta previa (tests y capturas).</summary>
        internal LevelCardView LevelCard => _levelCard;

        /// <summary>
        /// Escala de la fuente del sistema para la tarjeta, de 1 a 1,3 (02 §12; tests y capturas: la escala de Android aún no llega
        /// al juego [P]). Con la tarjeta abierta, la vuelve a colocar en el acto.
        /// </summary>
        internal float CardFontScale
        {
            get => _levelCard.FontScale;
            set
            {
                _levelCard.FontScale = value;
                if (_levelCard.Visible)
                {
                    _levelCard.Relayout();
                }
            }
        }

        /// <summary>Campos de la última línea <c>AF_STATE level_card</c> (tests; null si aún no hubo tarjeta o sin sonda).</summary>
        internal string? LastCardProbe => _lastCardProbe;

        /// <summary>La intro está retenida en t = 0 detrás de la tarjeta (tests).</summary>
        internal bool CardIntroHeld => _introHeld;

        /// <summary>Arte procedural con el que se pinta la tarjeta (tests: comparar sprites).</summary>
        internal AF.Presentation.ProceduralArt CardArt => _art;

        /// <summary>Alto del lienzo en dp con el que se maqueta (el de la pantalla simulada en los tests).</summary>
        internal float CardCanvasHeight => _canvasH;

        /// <summary>Zona segura inferior con la que se maqueta (dp).</summary>
        internal float CardSafeBottom => _safeBottom;

        /// <inheritdoc />
        public void OnLevelCard(LevelCardInfo card)
        {
            LevelFlow flow = RequireFlow();
            HoldIntroForCard(flow);
            _levelCard.Show(card, flow.ReduceMotion, _canvasW, _canvasH, _safeTop, _safeBottom);
            _levelCard.Root.SetAsLastSibling(); // por encima de todo: el toque en cualquier punto es suyo
            _lastCardProbe = DevProbe.Enabled ? CardProbeJson(card) : null;
            if (_lastCardProbe != null)
            {
                _probe.Event("level_card", _lastCardProbe);
            }
        }

        partial void CardsBuild()
        {
            _levelCard = new LevelCardView(_root, _art, OnLevelCardTapped);
            BannerBuild(); // el banner de mundo (GameScreen.WorldBanner.cs) va con los mismos ganchos
            _cardsBuilt = true;
        }

        partial void CardsLayout()
        {
            if (_cardsBuilt && _levelCard.Visible)
            {
                _levelCard.Layout(_canvasW, _canvasH, _safeTop, _safeBottom);
            }

            if (_cardsBuilt)
            {
                BannerLayout();
            }
        }

        partial void CardsTick(float dtMs)
        {
            if (!_cardsBuilt)
            {
                return;
            }

            LevelFlow? flow = _flow;
            bool inCard = flow != null && flow.HasLevel && flow.Phase == FlowPhase.LevelCard;
            if (_levelCard.Visible && !_levelCard.Dismissing && !inCard)
            {
                _levelCard.Dismiss(); // el flujo salió de LEVEL_CARD por otro camino (TapAnywhere, TapCuia, el fondo…)
            }

            // Retenida, la intro no avanza (GameScreen.Tick): se queda en el fotograma 0 sin volver a buscarlo en cada fotograma.
            if (_introHeld)
            {
                if (inCard)
                {
                    if (!_intro.Running)
                    {
                        HoldIntroForCard(flow!); // la soltó SnapToModel (vuelta de segundo plano): otra vez en t = 0
                    }
                }
                else
                {
                    // Salió de LEVEL_CARD sin arrancar la intro (cartel «El río bajó» antes; si la arrancó, StartIntro ya la
                    // soltó): se suelta.
                    _introHeld = false;
                    if (!_introAnimStarted)
                    {
                        _intro.Clear();
                    }
                }
            }

            _levelCard.Tick(dtMs);
            if (_levelCard.ConsumeEntered())
            {
                EmitCardLayout();
            }

            BannerTick(dtMs);
        }

        partial void CardsLevelLoaded(LevelFlow flow)
        {
            // Otro nivel (o el mismo reanudado): fuera la tarjeta anterior; OnLevelLoaded ya quitó la intro. Si el nivel la tiene,
            // OnLevelCard llega justo después.
            _introHeld = false;
            _levelCard.HideNow();
            BannerLevelLoaded();
        }

        partial void CardsLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (_cardsBuilt && _levelCard.Visible && !_levelCard.Dismissing)
            {
                ids.Add("level_card");
                rects.Add(_levelCard.Panel);
            }

            if (_cardsBuilt)
            {
                BannerLayoutIds(ids, rects);
            }
        }

        // 1 toque en cualquier punto (T13): directo al flujo, que arranca la intro; la tarjeta sale sin bloquear el siguiente
        // toque, que ya salta la intro (02 §4.2).
        private void OnLevelCardTapped()
        {
            if (!_levelCard.Visible || _levelCard.Dismissing)
            {
                return;
            }

            RequireFlow().DismissLevelCard();
            _levelCard.Dismiss();
        }

        // La intro del nivel arranca con la tarjeta y se queda en t = 0 (ver la clase): mismas vistas, geometría y especie que la
        // de verdad, que la vuelve a arrancar al cerrar la tarjeta (OnIntroStarted).
        private void HoldIntroForCard(LevelFlow flow)
        {
            if (_views.Length == 0)
            {
                return;
            }

            int ms = flow.ReduceMotion ? GameConfig.ReducedMotionIntroMs : GameConfig.TintDurationMs(flow.LevelNumber);
            StartIntro(flow, ms, false);
            _intro.Seek(0f);
            _introHeld = true; // después de StartIntro, que la suelta
        }

        private void EmitCardLayout()
        {
            LevelFlow? flow = _flow;
            if (!DevProbe.Enabled || flow == null || !flow.HasLevel)
            {
                return;
            }

            _layoutIds.Clear();
            _layoutRects.Clear();
            _layoutIds.Add("level_card");
            _layoutRects.Add(_levelCard.Panel);
            _probe.Layout("level_card", flow.LevelNumber, null, _layoutIds.ToArray(), _layoutRects.ToArray());
        }

        // AF_STATE level_card: tipo, nivel, etiqueta, estreno, líneas y tamaño del tutorial, iconos, «?», Luciérnaga y escala.
        private string CardProbeJson(LevelCardInfo card)
        {
            LevelCardView view = _levelCard;
            var sb = new StringBuilder(256);
            sb.Append("{\"kind\":\"").Append(KindKey(card.Kind)).Append('"');
            sb.Append(",\"level\":").Append(card.Level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"label\":\"").Append(card.Label).Append('"');
            sb.Append(",\"intro\":\"").Append(card.IntroMechanic ?? string.Empty).Append('"');
            sb.Append(",\"lines\":").Append(view.TutorialLines.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"sp\":").Append(view.TutorialBaseSp.ToString("0", CultureInfo.InvariantCulture));
            sb.Append(",\"reward\":").Append(card.RewardSeeds.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"ribbon\":").Append((card.RibbonMultiplier > 1 ? card.RibbonMultiplier : 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"murky\":").Append(card.IsMurky ? "true" : "false");
            sb.Append(",\"firefly\":").Append(card.RevealGifted ? "true" : "false");
            sb.Append(",\"icons\":[");
            bool first = true;
            for (int t = 1; t <= (int)MechanicTypes.Last; t++)
            {
                if (card.Kind != LevelCardKind.Debut && card.HasMechanic((MechanicType)t))
                {
                    AppendIcon(sb, MechanicTypes.ToKey((MechanicType)t), ref first);
                }
            }

            if (card.Kind != LevelCardKind.Debut)
            {
                if (card.HasMud)
                {
                    AppendIcon(sb, "barro", ref first);
                }

                if (card.HasSmallCuia)
                {
                    AppendIcon(sb, "chica", ref first);
                }

                if (card.HasTallCuia)
                {
                    AppendIcon(sb, "alta", ref first);
                }
            }

            sb.Append(']');
            sb.Append(",\"scale\":").Append(view.FontScale.ToString("0.00", CultureInfo.InvariantCulture));
            PiranaBiteDemo? bite = view.BiteDemo;
            sb.Append(",\"bite_demo\":").Append(bite == null ? "\"none\"" : (bite.Still ? "\"still\"" : "\"animated\"")).Append('}');
            return sb.ToString();
        }

        private static void AppendIcon(StringBuilder sb, string key, ref bool first)
        {
            sb.Append(first ? "\"" : ",\"").Append(key).Append('"');
            first = false;
        }

        internal static string KindKey(LevelCardKind kind)
        {
            switch (kind)
            {
                case LevelCardKind.Debut: return "debut";
                case LevelCardKind.Combined: return "combined";
                default: return "difficulty";
            }
        }
    }
}
