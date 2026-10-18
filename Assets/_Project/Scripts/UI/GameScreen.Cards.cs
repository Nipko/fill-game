#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Solver.Search;
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
    /// sobre el muelle, una burbuja «Te faltan {n}» que sale debajo). Sobre el muelle va a ≥ 8 dp del marco del tablero y a ≥ 12
    /// dp del marco de Marea, y si tapa el poste, el poste se funde con ella. En el 3 es el tooltip de Marea, «Marea · Deshacer:
    /// toca para devolver el agua» (04 §2.1), que no sale tras la intro sino tras el primer vertido que no acerca a la solución
    /// (02 §13 y 06 §2.2, «la primera vez que un vertido aleja de la solución»; 04 §2.1, «tras ejecutarlo»), medido con el solver,
    /// y solo la primera vez en la sesión de juego (ni en los reintentos) [P]. Con TiTino señalando el botón: sobre el muelle,
    /// pegada a la bandeja si cabe (aunque la franja mida ≥ 64 dp: prioridad de 04 §2.1 [P], pregunta para el dueño), con el
    /// retrato hacia el botón y una cola cuya punta queda en el eje del botón de Marea, a <see cref="TutorialTailUndoAirDp"/> dp
    /// de su marco; en 1-10 Marea es ilimitada, así que su primer uso no descuenta. El respaldo de 02 §13 («o al inicio del
    /// nivel 5 si no ocurrió») y «la primera vez» entre sesiones necesitan guardar en el perfil que ya salió (migración y
    /// aprobación del dueño, como los avistamientos de <see cref="SightingRule"/>): aún no están, pregunta para el dueño.
    /// Sin TiTino (<c>ui_sloth_enabled</c> apagado o, en release,
    /// sin su arte) o si la tarjeta no cabe, la instrucción sale como aviso de texto (<c>_toast</c>): el tutorial no se pierde [P]. El ajuste «Mensajes de TiTino» no la quita: es la instrucción del
    /// tutorial, no una frase de su banco [P]. El 8 sigue siendo de Capi (su aviso de la Cuia extra). En el 12 (tarea TU1b; 04
    /// §2.3; 06 §2.2) TiTino presenta Morpho tras la intro, «¿Atascado? La morpho conoce el río», en el sitio de las demás
    /// (02 §13 y D-016, T5-B: sobre la franja del panorama si mide ≥ 64 dp y, si no, sobre el muelle; sin la excepción del 3, porque
    /// 04 §2.3 dice que TiTino «presenta» el botón, no que lo señale); sobre el muelle va pegada a la bandeja con la cola en el eje
    /// del botón Pista si cabe con ella [P]; solo si la ranura Pista está en la bandeja (si Remote Config moviera <c>hint_unlock_level</c>, el 12 sin ranura no la
    /// enseña). Los 3 Morphos los acredita el flujo al cargar el nivel por primera vez (<see cref="OnHintGifted"/>; la insignia de
    /// la ranura pasa a «+3»). Sonda <c>AF_STATE tutorial</c> con el nivel, la clave, la duración, el sitio (<c>band</c>,
    /// <c>dock</c> o <c>toast</c>), si es el tooltip de Marea (<c>undo</c>) o la presentación de Morpho (<c>hint</c>) y si la cola
    /// señala su botón, a ≤ <see cref="TutorialPointToleranceDp"/> dp de su eje (<c>over_undo</c> y <c>over_hint</c>).</para>
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
            UpdateDebutClearance(card);
            _levelCard.Show(card, hito, hito != null ? HitoWater(flow, hito) : string.Empty, flow.ReduceMotion, _canvasW, _canvasH,
                _safeTop, _safeBottom);
            RestoreBoardUnit();
            _levelCard.Root.SetAsLastSibling(); // por encima de todo: el toque en cualquier punto es suyo
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.MechCard); // AU1: tarjeta previa
            if (card.Kind != LevelCardKind.Difficulty)
            {
                AF.Audio.AudioCues.Stinger(AF.Audio.StingerId.MechCard); // AU3: su remate, solo con un estreno (Debut o Combined)
            }

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
                if (_levelCard.Card != null)
                {
                    UpdateDebutClearance(_levelCard.Card); // el tablero ya está colocado en la pantalla nueva
                }

                _levelCard.Layout(_canvasW, _canvasH, _safeTop, _safeBottom);
                RestoreBoardUnit(); // al cambiar de pantalla, LayoutBoard pone después la unidad nueva
                RefreshCardProbe();
            }

            if (_cardsBuilt)
            {
                BannerLayout();
            }
        }

        // La tarjeta de estreno no tapa la cuia que estrena (VA1b): le pasa lo más alto que se ve de esa cuia (su cuerpo y las
        // piezas a la vista de su overlay; con varias, la más baja en la pantalla) y lo alto de la fila 1, en dp desde arriba del
        // lienzo. Solo al mostrar la tarjeta y al cambiar de pantalla (no por fotograma).
        private void UpdateDebutClearance(LevelCardInfo card)
        {
            MechanicType type = card.Kind == LevelCardKind.Debut ? LevelCardInfo.DebutMechanicType(card.IntroMechanic) : MechanicType.None;
            float debutTop = float.NaN;
            float rowOneTop = float.NaN;
            for (int i = 0; i < _views.Length; i++)
            {
                CuiaView view = _views[i];
                CanvasRectDp(view.Body, out _, out float bodyTop, out _, out _);
                rowOneTop = float.IsNaN(rowOneTop) ? bodyTop : Mathf.Min(rowOneTop, bodyTop);
                if (type == MechanicType.None || view.ShownCuia.Mechanic != type)
                {
                    continue;
                }

                float top = bodyTop;
                var host = view.OverlayHost;
                top = Mathf.Min(top, ShownChildrenTopDp(host.SlotBack));
                top = Mathf.Min(top, ShownChildrenTopDp(host.BodyBack));
                top = Mathf.Min(top, ShownChildrenTopDp(host.BodyFront));
                top = Mathf.Min(top, ShownChildrenTopDp(host.SlotFront));
                debutTop = float.IsNaN(debutTop) ? top : Mathf.Max(debutTop, top);
            }

            _levelCard.SetDebutClearance(debutTop, rowOneTop);
        }

        // Lo más alto (dp desde arriba del lienzo) de los hijos a la vista de anchor; +∞ si no hay ninguno.
        private float ShownChildrenTopDp(RectTransform anchor)
        {
            float top = float.PositiveInfinity;
            for (int c = 0; c < anchor.childCount; c++)
            {
                var child = anchor.GetChild(c) as RectTransform; // algún overlay cuelga piezas sin RectTransform
                if (child == null || !child.gameObject.activeSelf || child.rect.width <= 0f || child.rect.height <= 0f)
                {
                    continue;
                }

                CanvasRectDp(child, out _, out float childTop, out _, out _);
                top = Mathf.Min(top, childTop);
            }

            return top;
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

            TickTutorialAfterPour(); // el tooltip de Marea del 3, tras el primer vertido que no acerca (TU1a)

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
            _tutorialAfterPour = null; // el tooltip de Marea que esperaba un vertido era del nivel anterior (TU1a)
            _undoTutorialDue = false;
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

        /// <summary>
        /// Aire mínimo (dp) entre el marco del tablero y la instrucción del tutorial cuando va sobre el muelle (revisión de TU1a: la
        /// tarjeta del miquito y el aviso de TI6b van a 4 dp).
        /// </summary>
        internal const float TutorialDockBoardAirDp = 8f;

        /// <summary>
        /// Aire mínimo (dp) entre el canto de la instrucción del tutorial sobre el muelle y el marco del botón de Marea, por debajo
        /// (revisión de TU1a). Se suma a lo que ya pide la bandeja con sus burbujas (SightingDockLimit).
        /// </summary>
        internal const float TutorialDockUndoAirDp = 12f;

        /// <summary>
        /// Aire mínimo (dp) entre la punta de la cola del tooltip de Marea, con su canto, y el marco del botón de Marea (revisión de
        /// TU1a) [P]: la ranura empieza 4 dp por debajo del borde de la bandeja y el cuenco pintado por código asoma 2 dp por encima
        /// (BoosterTray.Layout), así que la cola queda fuera de la bandeja.
        /// </summary>
        internal const float TutorialTailUndoAirDp = 8f;

        /// <summary>
        /// La cola señala el botón de Marea si su punta queda a ≤ este desvío (dp) del eje del botón (revisión de TU1a; sonda
        /// <c>over_undo</c>) [P].
        /// </summary>
        internal const float TutorialPointToleranceDp = 8f;

        // La tarjeta de TiTino a la vista lleva la instrucción del tutorial (no el aviso tras Volver a la orilla).
        private bool _slothNoticeTutorial;
        private string? _lastTutorialProbe;
        private int _tutorialShown;

        // El tooltip de Marea del 3 espera al primer vertido que no acerca a la solución (02 §13; 04 §2.1: «tras ejecutarlo»):
        // su texto, o null.
        private string? _tutorialAfterPour;

        // Vigilancia del tooltip de Marea: los vertidos aplicados y la distancia del solver a la victoria (UndoTutorialUnknown si
        // no se sabe; UndoTutorialDead sin salida) con los que se miró por última vez, y si ya hay un vertido que no acerca y el
        // tooltip espera a que la vista termine.
        private int _undoWatchPours;
        private int _undoWatchDistance = UndoTutorialUnknown;
        private bool _undoTutorialDue;

        // El tooltip de Marea ya salió en esta sesión de juego: «la primera vez» (02 §13), nunca en los reintentos [P].
        private bool _undoTutorialDone;

        // Solver de la distancia del tooltip de Marea (perezoso: solo en el 3, al cambiar el número de vertidos).
        private AStarSolver? _undoSolver;

        private const int UndoTutorialUnknown = -1;
        private const int UndoTutorialDead = int.MaxValue;

        // La última instrucción se colocó con la cola sobre el botón que señala (tests y sonda): el de Marea en el 3, el de Pista
        // en el 12 (_tutorialTarget).
        private bool _tutorialOverTarget;
        private TutorialTarget _tutorialTarget;

        // El botón de la bandeja que señala la instrucción del nivel con la cola de la tarjeta de TiTino.
        private enum TutorialTarget
        {
            None = 0,
            Undo = 1,
            Hint = 2,
        }

        /// <summary>
        /// Solo tests y capturas: el alto (dp) que la instrucción del tutorial toma como franja del panorama (null = el de verdad,
        /// <c>PanelY − BoardTop</c>). Por debajo de <see cref="SightingBandDp"/> fuerza el sitio sobre el muelle, que en los
        /// tableros de 1, 2, 3 y 6 no sale solo (sus franjas miden más de 64 dp a 640 y 780 dp, también al 130 %).
        /// </summary>
        internal static float? TutorialBandOverrideDp { get; set; }

        /// <summary>El tooltip de Marea del 3 espera aún al primer vertido que no acerca a la solución (tests).</summary>
        internal bool TutorialWaitsForPour => _tutorialAfterPour != null;

        /// <summary>El tooltip de Marea ya salió en esta sesión de juego y no vuelve (tests).</summary>
        internal bool UndoTutorialDone => _undoTutorialDone;

        /// <summary>
        /// La instrucción a la vista señala el botón de Marea con la cola de la tarjeta de TiTino, a ≤
        /// <see cref="TutorialPointToleranceDp"/> dp de su eje (el 3; 04 §2.1; tests).
        /// </summary>
        internal bool TutorialPointsAtUndo => TutorialSlothVisible && _tutorialOverTarget && _tutorialTarget == TutorialTarget.Undo;

        /// <summary>
        /// La presentación de Morpho a la vista señala el botón Pista con la cola de la tarjeta de TiTino, a ≤
        /// <see cref="TutorialPointToleranceDp"/> dp de su eje (el 12; 04 §2.3: «TiTino presenta el botón»; TU1b; tests).
        /// </summary>
        internal bool TutorialPointsAtHint => TutorialSlothVisible && _tutorialOverTarget && _tutorialTarget == TutorialTarget.Hint;

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
        // En el 3, el tooltip de Marea no entra aún: espera al primer vertido que no acerca a la solución (TickTutorialAfterPour),
        // y solo hasta que salga una vez en esta sesión de juego.
        private void MaybeShowTutorial(LevelFlow flow)
        {
            _tutorialAfterPour = null;
            _undoTutorialDue = false;
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

            if (TutorialInstruction.PointsAtHint(flow.LevelNumber) && flow.Hint.State == HintState.Hidden)
            {
                return; // la presentación de Morpho, solo con su ranura en la bandeja (hint_unlock_level movido por Remote Config) [P]
            }

            if (TutorialInstruction.PointsAtUndo(flow.LevelNumber))
            {
                if (_undoTutorialDone)
                {
                    return; // «la primera vez» (02 §13): ni en los reintentos ni al volver a jugar el 3 [P]
                }

                // 02 §13 y 04 §2.1: tras el vertido que empeora la posición, medido con el solver desde el tablero del intento.
                _tutorialAfterPour = text;
                LevelSession session = flow.Session;
                _undoWatchPours = session.AppliedPours;
                _undoWatchDistance = UndoTutorialDistance(session);
                return;
            }

            EnqueueNoticeFirst(NoticeKind.Tutorial, text);
        }

        // El tooltip de Marea del 3 (02 §13; 04 §2.1). Cada vez que cambia el número de vertidos del intento, la distancia del solver
        // a la victoria (A* óptimo con el modo de vertido y la regla de fútiles de la partida): un vertido nuevo que no la baja (o
        // que deja el tablero sin salida) es el que «aleja de la solución». Lectura débil de «distancia del solver +2» [P]: en el
        // tablero del 3 ningún vertido útil la sube (lo mide tools/out/tutorial_v0_report.md: «el mayor cambio … es 0»), así que
        // vale «no acerca», la misma que usa la comprobación del nivel 3 (decisiones.md §2.1.4; 4 de los 6 vertidos iniciales).
        // Un vertido que acerca no lo dispara; deshacer antes de que salga lo cancela. En cuanto la vista termina de animarlo, en
        // juego y sin bloqueo ni victoria pendientes, entra el primero en la cola (sale cuando la tropa se ha ido; PumpNotices).
        // Cada fotograma sin asignaciones; el solver, solo al cambiar el número de vertidos y solo en el 3.
        private void TickTutorialAfterPour()
        {
            string? text = _tutorialAfterPour;
            LevelFlow? flow = _flow;
            if (text == null || flow == null || !flow.HasLevel)
            {
                return;
            }

            LevelSession session = flow.Session;
            int pours = session.AppliedPours;
            if (pours != _undoWatchPours)
            {
                int distance = UndoTutorialDistance(session);
                if (pours < _undoWatchPours)
                {
                    _undoTutorialDue = false; // deshizo (u otro intento): espera al siguiente vertido
                }
                else if (_undoWatchDistance != UndoTutorialUnknown && distance != UndoTutorialUnknown
                    && (distance == UndoTutorialDead || distance >= _undoWatchDistance))
                {
                    _undoTutorialDue = true;
                }

                _undoWatchPours = pours;
                _undoWatchDistance = distance;
            }

            if (!_undoTutorialDue || NoticesHeld(flow) || !IsViewIdle)
            {
                return;
            }

            _tutorialAfterPour = null;
            _undoTutorialDue = false;
            EnqueueNoticeFirst(NoticeKind.Tutorial, text);
            PumpNotices();
        }

        // Vertidos que faltan para ganar desde el tablero del intento (0 si ya está ganado), UndoTutorialDead si no tiene salida o
        // UndoTutorialUnknown si el solver no decide dentro de su presupuesto (el del pipeline, 03 §5: el tablero del 3 es pequeño).
        private int UndoTutorialDistance(LevelSession session)
        {
            BoardState state = session.Current;
            _undoSolver ??= new AStarSolver();
            SolverOptions options = SolverOptions.ForPipeline(session.PourMode, state).WithFutileRule(session.FutileRuleEnabled);
            SolveResult result = _undoSolver.Solve(state, options);
            switch (result.Status)
            {
                case SolveStatus.Solved: return result.MoveCount;
                case SolveStatus.Unsolvable: return UndoTutorialDead;
                default: return UndoTutorialUnknown;
            }
        }

        // La instrucción en la tarjeta de TiTino, en su sitio (PlaceTutorialCard: la franja del panorama o el muelle; el tooltip
        // de Marea y la presentación de Morpho, junto a su botón y señalándolo con la cola), con sloth_present; sin TiTino (apagado
        // o sin su arte) o sin sitio, como aviso de texto, que nunca tapa las cuias. El tooltip de Marea, una vez enseñado, no
        // vuelve en esta sesión.
        private void ShowTutorialNow(string text)
        {
            BoardLayoutResult? l = _layout;
            _slothNoticeDockBottom = float.NaN;
            _slothNoticeTutorial = false;
            _tutorialOverTarget = false;
            LevelFlow? flow = _flow;
            bool hasLevel = flow != null && flow.HasLevel;
            TutorialTarget target = !hasLevel ? TutorialTarget.None
                : TutorialInstruction.PointsAtUndo(flow!.LevelNumber) ? TutorialTarget.Undo
                : TutorialInstruction.PointsAtHint(flow!.LevelNumber) ? TutorialTarget.Hint
                : TutorialTarget.None;
            _tutorialTarget = target;
            if (target == TutorialTarget.Undo)
            {
                _undoTutorialDone = true;
            }

            SlothArt? art = SlothEnabled ? _art.SlothRigArt() : null;
            if (l != null && art != null)
            {
                _slothNotice ??= new SlothNoticeCard(_noticeLayer, _art);
                float height = _slothNotice.Measure(_canvasW, text);
                if (height > 0f && PlaceTutorialCard(l, height, target))
                {
                    _slothNoticeTutorial = true;
                    _slothNotice.Show(art.Head, flow != null && flow.ReduceMotion, SlothPoseId.Present);
                    _tutorialShown++;
                    EmitTutorialProbe((int)_slothNotice.DurationMs, float.IsNaN(_slothNoticeDockBottom) ? "band" : "dock");
                    return;
                }
            }

            _slothNoticeDockBottom = float.NaN;
            _slothCoversPost = false;
            _tutorialOverTarget = false;
            ShowToastNow(text);
            _tutorialShown++;
            EmitTutorialProbe(NoticeTiming.ReadMs(text), "toast");
        }

        // La instrucción ya medida (height) en su sitio, como la tarjeta del miquito: sobre la franja del panorama si mide al menos
        // SightingBandDp (y su alto + SightingBandAirDp); si no, sobre el muelle, con TutorialDockBoardAirDp bajo el marco del
        // tablero y TutorialDockUndoAirDp sobre el marco de Marea (y por encima de la bandeja y de sus burbujas). Sobre el muelle,
        // si tapa el poste, el poste se funde con ella (como con «Fruta nueva»). False si no cabe en ningún sitio.
        // El tooltip de Marea del 3 (pointsAtUndo; 04 §2.1: TiTino señala el botón) va sobre el muelle, pegado a la bandeja, si
        // cabe con su cola (la punta, a TutorialTailUndoAirDp sobre el marco de Marea y en su eje), aunque la franja mida ≥ 64 dp:
        // 02 §13 y D-016 (T5-B) piden la franja si mide ≥ 64 dp y 04 §2.1 que señale el botón, y desde la franja no se puede
        // señalar la bandeja. Prioridad de 04 §2.1 [P], pregunta para el dueño (entrega de TU1a). Si no cabe con su cola, sale
        // como las demás instrucciones, sin cola. La presentación de Morpho del 12 (TU1b) no tiene esa excepción (04 §2.3 dice
        // «TiTino presenta el botón», no que lo señale): sigue 02 §13 y D-016 (T5-B), la franja si mide ≥ 64 dp y, si no, el
        // muelle; solo entonces, pegada a la bandeja con la cola en el eje del botón Pista si cabe con ella (si no, sin cola) [P].
        private bool PlaceTutorialCard(BoardLayoutResult l, float height, TutorialTarget target)
        {
            _slothNoticeDockBottom = float.NaN;
            _slothCoversPost = false;
            float realBand = l.PanelY - BoardTop;
            float band = TutorialBandOverrideDp ?? realBand;
            bool bandFits = band >= Mathf.Max(SightingBandDp, height + SightingBandAirDp);
            float dockTop = l.PanelBottom + TutorialDockBoardAirDp;
            float dockBottom = TutorialDockLimit(TutorialTarget.None, out _);
            bool dockFits = dockTop + height <= dockBottom;
            float targetX = float.NaN;
            float tailBottom = target != TutorialTarget.None ? TutorialDockLimit(target, out targetX) : float.NaN;
            bool tailFits = target != TutorialTarget.None && !float.IsNaN(targetX) && dockTop + height <= tailBottom;
            // Solo el tooltip de Marea del 3 antepone el muelle con cola a la franja (04 §2.1: «TiTino señala el botón» [P]).
            bool tailFirst = target == TutorialTarget.Undo;
            float top;
            bool dock;
            bool tail;
            if (tailFits && tailFirst)
            {
                top = tailBottom - height;
                dock = true;
                tail = true;
            }
            else if (bandFits)
            {
                top = BoardTop + ((realBand - height) * 0.5f);
                dock = false;
                tail = false;
            }
            else if (tailFits)
            {
                top = tailBottom - height;
                dock = true;
                tail = true;
            }
            else if (dockFits)
            {
                top = dockTop;
                dock = true;
                tail = false;
            }
            else
            {
                return false;
            }

            _slothNotice!.Place(_canvasW, top, tail ? targetX : float.NaN);

            // La cola señala el botón si su punta queda en su eje (la limita el aire de TailInsetDp con los lados de la tarjeta).
            _tutorialOverTarget = tail && Mathf.Abs(_slothNotice.TailCenterXDp - targetX) <= TutorialPointToleranceDp;
            if (dock)
            {
                // Con cola, lo que baja la tarjeta sobre el muelle llega a su punta (sin su canto, como la placa): así la aparta
                // también una burbuja «Te faltan {n}» que saliera debajo (KeepSightingClearOfBubbles).
                _slothNoticeDockBottom = top + height + (tail ? SlothNoticeCard.TailHeightDp : 0f);
                _slothCoversPost = CardOverPost(_slothNotice.Root, SlothNoticeCard.EdgeDp)
                    || (tail && CardOverPost(_slothNotice.Tail, SlothNoticeCard.EdgeDp));
            }

            return true;
        }

        // Lo más bajo que puede llegar la placa de la instrucción sobre el muelle (dp desde arriba): lo de la tarjeta del miquito
        // (SightingDockLimit: la bandeja y sus burbujas) y, con su canto, TutorialDockUndoAirDp sobre el marco del botón de Marea.
        // Con cola (target: el botón que señala, Marea o Pista), además, su punta con su canto a TutorialTailUndoAirDp sobre el
        // marco de ese botón, y sin su canto por encima de lo que deja SightingDockLimit. targetX: el eje de ese botón (NaN sin
        // botón o si no se ve; entonces no hay cola).
        private float TutorialDockLimit(TutorialTarget target, out float targetX)
        {
            float limit = SightingDockLimit();
            targetX = float.NaN;
            bool withTail = target != TutorialTarget.None;
            if (withTail)
            {
                limit -= SlothNoticeCard.TailHeightDp;
            }

            RectTransform undo = (RectTransform)_tray.UndoButton.transform;
            if (undo.gameObject.activeInHierarchy)
            {
                CanvasRectDp(undo, out _, out float top, out _, out _);
                limit = Mathf.Min(limit, top - TutorialDockUndoAirDp - SlothNoticeCard.EdgeDp);
            }

            if (withTail)
            {
                RectTransform button = (RectTransform)(target == TutorialTarget.Hint ? _tray.HintButton.transform : _tray.UndoButton.transform);
                if (button.gameObject.activeInHierarchy)
                {
                    CanvasRectDp(button, out float left, out float top, out float right, out _);
                    targetX = (left + right) * 0.5f;
                    limit = Mathf.Min(limit, top - TutorialTailUndoAirDp - SlothNoticeCard.TailReachDp);
                }
            }

            return limit;
        }

        // AF_STATE tutorial: nivel, clave de la instrucción, duración, sitio (band, dock o toast) y el botón que señala con la cola
        // (undo/over_undo en el 3, hint/over_hint en el 12). Solo con la sonda; al enseñarla, nunca por fotograma.
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
            bool over = mode != "toast" && _tutorialOverTarget;
            sb.Append(",\"undo\":").Append(TutorialInstruction.PointsAtUndo(level) ? "true" : "false");
            sb.Append(",\"over_undo\":").Append(over && _tutorialTarget == TutorialTarget.Undo ? "true" : "false");
            sb.Append(",\"hint\":").Append(TutorialInstruction.PointsAtHint(level) ? "true" : "false");
            sb.Append(",\"over_hint\":").Append(over && _tutorialTarget == TutorialTarget.Hint ? "true" : "false");
            _lastTutorialProbe = sb.ToString();
            _probe.Event("tutorial", _lastTutorialProbe);
        }
    }
}
