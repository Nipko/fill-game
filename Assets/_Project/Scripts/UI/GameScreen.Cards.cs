#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Core.Mechanics;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
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
    /// el rectángulo de la tarjeta (cualquier toque la cierra). Si la tarjeta abierta se recoloca (otra escala u otra pantalla) y
    /// cambia algo de eso, <c>AF_STATE level_card</c> vuelve a salir con lo que se ve; y, si ya terminó de entrar, también su
    /// <c>AF_LAYOUT level_card</c>, con los rectángulos nuevos.</para>
    /// <para><b>Hito p20</b> (M2.5; 02 §10; D-013, punto 8): en el 40, el 60… la tarjeta lleva la fila «Si lo superas, llegas a
    /// {lugar}» con la silueta del sitio (<see cref="LevelCardView.HitoStopFor"/> con <see cref="RiverStops"/>; la ventanita, con
    /// el agua de los niveles de esa parada). <c>AF_STATE level_card</c> la anota con <c>"hito"</c> (el id de la parada o
    /// null), <c>"hito_silhouette"</c> (<c>png</c>, <c>shape</c>, <c>code</c> o <c>none</c>) y <c>"hito_omitted"</c>, y
    /// <c>AF_LAYOUT</c>, con el rectángulo <c>level_card_hito</c>.</para>
    /// <para><b>Instrucción del tutorial con TiTino</b> (tarea TU1a; D-016, T2-A y T5-B; 02 §13; 04 §2.1; 06 §2.2; guion de la
    /// intro §14; propuesta de TiTino d.3 y paso 6e de (i)): en los niveles 1, 2, 3 y 6 (<see cref="TutorialInstruction"/>), al
    /// habilitarse el toque tras la intro de cada intento (el primero y tras Volver a la orilla; nunca al reanudar, que va sin
    /// intro), la instrucción entra la primera en la cola de avisos (tipo Tutorial) y sale cuando la tropa ya se fue y sin el
    /// rótulo del intento: en la tarjeta de TiTino (<see cref="SlothNoticeCard"/>, la del aviso tras Volver a la orilla, que en
    /// estos niveles no sale) con su retrato de 40 dp y <c>sloth_present</c>, sobre la franja del panorama si mide ≥ 64 dp y si
    /// no sobre el muelle, sin capturar toques, sin tapar cuias, bandeja ni HUD, durante su tiempo de lectura (D-011) y sin
    /// quedarse fija. La ocultan lo mismo que al aviso de TiTino (un modal del embudo, otro intento, otro nivel, la celebración o,
    /// sobre el muelle, una burbuja «Te faltan {n}» que sale debajo). En el 3 es el tooltip de Marea, «Marea · Deshacer: toca para
    /// devolver el agua» (04 §2.1); en 1-10 Marea es ilimitada, así que su primer uso no descuenta. Sin TiTino
    /// (<c>ui_sloth_enabled</c> apagado o, en release, sin su arte) o si la tarjeta no cabe, la instrucción sale como aviso de
    /// texto (<c>_toast</c>): el tutorial no se pierde [P]. El ajuste «Mensajes de TiTino» no la quita: es la instrucción del
    /// tutorial, no una frase de su banco [P]. El 8 sigue siendo de Capi (su aviso de la Cuia extra) y el 12, de TU1b. Sonda
    /// <c>AF_STATE tutorial</c> con el nivel, la clave, la duración y el sitio (<c>band</c>, <c>dock</c> o <c>toast</c>).</para>
    /// </remarks>
    public sealed partial class GameScreen : ILevelCardListener
    {
        private LevelCardView _levelCard = null!;
        private bool _cardsBuilt;
        private string? _lastCardProbe;

        // La tarjeta abierta ya publicó su AF_LAYOUT level_card (terminó de entrar): al recolocarla se vuelve a publicar.
        private bool _cardLayoutSent;

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
                    RestoreBoardUnit();
                    RefreshCardProbe(); // la escala y la fila del hito (que puede omitirse) cambian con ella
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
            _cardLayoutSent = false;
            RiverStop? hito = LevelCardView.HitoStopFor(card.Level, RiverStops);
            _levelCard.Show(card, hito, hito != null ? HitoWater(flow, hito) : string.Empty, flow.ReduceMotion, _canvasW, _canvasH,
                _safeTop, _safeBottom);
            RestoreBoardUnit();
            _levelCard.Root.SetAsLastSibling(); // por encima de todo: el toque en cualquier punto es suyo
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.MechCard); // AU1: tarjeta previa
            _lastCardProbe = DevProbe.Enabled ? CardProbeJson(card) : null;
            if (_lastCardProbe != null)
            {
                _probe.Event("level_card", _lastCardProbe);
            }
        }

        // Tras recolocar la tarjeta abierta (otra escala u otra pantalla), AF_STATE level_card vuelve a salir si cambió algo
        // (la escala, hito_omitted…): la sonda refleja siempre la tarjeta que se ve, no la del momento de mostrarla. Si ya terminó
        // de entrar, su AF_LAYOUT level_card sale otra vez siempre (los rectángulos cambian aunque la sonda no cambie), para que
        // quien toca por adb no use los de antes ni un level_card_hito de una fila que ya se omitió.
        private void RefreshCardProbe()
        {
            LevelCardInfo? card = _levelCard.Card;
            if (!DevProbe.Enabled || card == null || !_levelCard.Visible || _levelCard.Dismissing)
            {
                return;
            }

            string probe = CardProbeJson(card);
            if (probe != _lastCardProbe)
            {
                _lastCardProbe = probe;
                _probe.Event("level_card", probe);
            }

            if (_cardLayoutSent)
            {
                EmitCardLayout();
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
                RestoreBoardUnit(); // al cambiar de pantalla, LayoutBoard pone después la unidad nueva
                RefreshCardProbe();
            }

            if (_cardsBuilt)
            {
                BannerLayout();
            }
        }

        // El mordisco de la tarjeta del 183 (PiranaBiteDemo) coloca sus cuias de 44 dp con CuiaView.SetGeometry, que escribe su
        // unidad en el material compartido del líquido (LiquidGraphic.SetUnit: el grosor de 1 dp del borde de la cara de
        // arriba, AF_UI_Liquid.shader). Todas las cuias de un nivel miden lo mismo (02 §5) y la que manda es la del tablero: se
        // vuelve a poner en cuanto la tarjeta coloca el escenario, sin tocar nada más del tablero.
        private void RestoreBoardUnit()
        {
            if (_levelCard.BiteDemo != null && _views.Length > 0)
            {
                CuiaView board = _views[0];
                board.Liquid.SetUnit(board.Unit);
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
                _cardLayoutSent = true;
                EmitCardLayout();
            }

            BannerTick(dtMs);
        }

        partial void CardsLevelLoaded(LevelFlow flow)
        {
            // Otro nivel (o el mismo reanudado): fuera la tarjeta anterior; OnLevelLoaded ya quitó la intro. Si el nivel la tiene,
            // OnLevelCard llega justo después.
            _introHeld = false;
            _cardLayoutSent = false;
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

        // Agua de la ventanita del hito: la de los niveles de la parada a la que se llega (el 41 del Encuentro, en el Negro), o la
        // del nivel si el catálogo no los tiene.
        private static string HitoWater(LevelFlow flow, RiverStop stop)
        {
            LevelDefinition? first = flow.Catalog.Find(stop.FirstLevel);
            return first != null ? first.WaterBase : flow.Level.WaterBase;
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
            RectTransform? hito = _levelCard.HitoRowRect;
            if (hito != null)
            {
                _layoutIds.Add("level_card_hito");
                _layoutRects.Add(hito);
            }

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
            sb.Append(",\"bite_demo\":").Append(bite == null ? "\"none\"" : (bite.Still ? "\"still\"" : "\"animated\""));

            // Hito p20 (M2.5): la parada a la que se llega, el origen de la silueta y si la fila se omitió por no caber.
            RiverStop? stop = view.HitoStop;
            sb.Append(",\"hito\":").Append(stop != null && view.HitoText != null ? "\"" + stop.Id + "\"" : "null");
            sb.Append(",\"hito_silhouette\":\"").Append(view.HitoSilhouette).Append('"');
            sb.Append(",\"hito_omitted\":").Append(view.HitoOmitted ? "true" : "false").Append('}');
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

        // ------------------------------------------------------------------ Instrucción del tutorial con TiTino (TU1a)

        // La tarjeta de TiTino a la vista lleva la instrucción del tutorial (no el aviso tras Volver a la orilla).
        private bool _slothNoticeTutorial;
        private string? _lastTutorialProbe;
        private int _tutorialShown;

        /// <summary>
        /// Solo tests: enciende o apaga la instrucción del tutorial con TiTino (null = encendida, como en el juego). Los tests
        /// PlayMode que no tratan de ella la apagan (<c>PlayModeHelpers</c>), para que sus niveles 1, 2, 3 y 6 sigan con sus
        /// avisos, sus etiquetas del HUD y sus capturas como antes de TU1a; los que la prueban (<c>TutorialSlothTests</c>) la
        /// encienden y la vuelven a null en su TearDown.
        /// </summary>
        internal static bool? TutorialSlothOverride { get; set; }

        /// <summary>La instrucción del tutorial está a la vista en la tarjeta de TiTino (tests).</summary>
        internal bool TutorialSlothVisible => _slothNoticeTutorial && SlothNoticeVisible;

        /// <summary>La instrucción del tutorial va sobre el muelle (no en la franja del panorama; tests).</summary>
        internal bool TutorialSlothOnDock => TutorialSlothVisible && !float.IsNaN(_slothNoticeDockBottom);

        /// <summary>Veces que se enseñó la instrucción del tutorial (en la tarjeta o como aviso de texto) desde que se creó la pantalla (tests).</summary>
        internal int TutorialShownCount => _tutorialShown;

        /// <summary>Campos de la última sonda <c>AF_STATE tutorial</c> (null si no salió ninguna o sin sonda; tests).</summary>
        internal string? LastTutorialProbe => _lastTutorialProbe;

        // Al habilitarse el toque tras la intro (OnIntroFinished), la instrucción del nivel (TutorialInstruction.KeyFor) entra la
        // primera en la cola de avisos; sale cuando la tropa se ha ido y sin el rótulo del intento (PumpNotices).
        private void MaybeShowTutorial(LevelFlow flow)
        {
            if (!flow.HasLevel || !(TutorialSlothOverride ?? true))
            {
                return;
            }

            string? key = TutorialInstruction.KeyFor(flow.LevelNumber);
            if (key == null)
            {
                return;
            }

            string text = UiText.TutorialInstructionText(key);
            if (text.Length == 0)
            {
                return; // sin texto en este idioma no hay instrucción (nunca otro idioma; D-018)
            }

            EnqueueNoticeFirst(NoticeKind.Tutorial, text);
        }

        // La instrucción en la tarjeta de TiTino, en su sitio (PlaceSlothCard: la franja del panorama o el muelle), con
        // sloth_present; sin TiTino (apagado o sin su arte) o sin sitio, como aviso de texto, que nunca tapa las cuias.
        private void ShowTutorialNow(string text)
        {
            BoardLayoutResult? l = _layout;
            _slothNoticeDockBottom = float.NaN;
            _slothNoticeTutorial = false;
            LevelFlow? flow = _flow;
            SlothArt? art = SlothEnabled ? _art.SlothRigArt() : null;
            if (l != null && art != null)
            {
                _slothNotice ??= new SlothNoticeCard(_noticeLayer, _art);
                float height = _slothNotice.Measure(_canvasW, text);
                if (height > 0f && PlaceSlothCard(l, height))
                {
                    _slothNoticeTutorial = true;
                    _slothNotice.Show(art.Head, flow != null && flow.ReduceMotion, SlothPoseId.Present);
                    _tutorialShown++;
                    EmitTutorialProbe((int)_slothNotice.DurationMs, float.IsNaN(_slothNoticeDockBottom) ? "band" : "dock");
                    return;
                }
            }

            _slothNoticeDockBottom = float.NaN;
            ShowToastNow(text);
            _tutorialShown++;
            EmitTutorialProbe(NoticeTiming.ReadMs(text), "toast");
        }

        // AF_STATE tutorial: nivel, clave de la instrucción, duración y sitio (band, dock o toast). Solo con la sonda; al
        // enseñarla, nunca por fotograma.
        private void EmitTutorialProbe(int ms, string mode)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            int level = _flow?.LevelNumber ?? 0;
            var sb = new StringBuilder(128);
            sb.Append("\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "key", TutorialInstruction.KeyFor(level));
            sb.Append(",\"ms\":").Append(ms.ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "mode", mode);
            sb.Append(",\"undo\":").Append(TutorialInstruction.PointsAtUndo(level) ? "true" : "false");
            _lastTutorialProbe = sb.ToString();
            _probe.Event("tutorial", _lastTutorialProbe);
        }
    }
}
