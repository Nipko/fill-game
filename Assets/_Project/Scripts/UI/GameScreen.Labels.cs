#nullable enable
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Parcial de las etiquetas de D-009 en el HUD y en Siguiente (arquitectura del paso 1, §7; tarea T09; 02 §5 y §10; 07 §7.4
    /// y §7.7; propuesta-mecanicas-dificultad.md §c.1 y §c.5), sin editar GameScreen.cs:
    /// <list type="bullet">
    /// <item><description>bajo la insignia «Nivel {n}», el chip de dificultad (Difícil o Muy difícil, sin cinta) y, en los
    /// niveles de Aguas turbias, el chip gris-pardo «?» (<see cref="MurkyChip"/>); con un contador de Lluvia o Piraña, su chip
    /// (<see cref="HudCounterChip"/>) a la derecha de la misma banda;</description></item>
    /// <item><description>alrededor del panel, un marco propio de 3 dp ámbar (Difícil) o índigo (Muy difícil), y en Muy difícil
    /// un velo propio que oscurece el fondo un 15 %, detrás del panel (la variante noche con LUT queda [P]);</description></item>
    /// <item><description>en la victoria, sobre Siguiente, el chip del nivel N + 1 con su cinta «×3»/«×4» (el panel crece
    /// <see cref="NextChipRowDp"/> para hacerle sitio).</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para><b>Banda de las etiquetas</b> (<see cref="HudLabelMode"/>): con sitio entre la fila superior y el canto del panel,
    /// chips normales (30 dp) en <c>TopRowY + 50</c>; si no, la variante compacta de 20 dp: en la misma línea si queda por encima
    /// de las cuias elevadas de la fila 1, o «pegada bajo la placa» (sobre el canto inferior de la insignia, bajo el texto) en los
    /// tableros de 3 filas, con el contador en el mismo grupo (a los lados están la pausa, los saldos y Reiniciar) [P]. El grupo
    /// se centra bajo la insignia y se aparta del cielo de la cuia del contador (su nube y sus puntos, en los 32 dp táctiles de
    /// encima, 02 §3.3) si está en la fila 1.</para>
    /// <para><b>Cuándo se ven</b>: en Playing y en la celebración, con la insignia a la vista (el embudo y la victoria la
    /// ocultan). Durante la intro no: la tropa de miquitos se sienta en la franja entre el HUD y el panel; aparecen con un
    /// fundido de <see cref="HudLabelsFadeMs"/> al terminar, y también esperan al rótulo del intento o «El río bajó» y a la
    /// tarjeta «¡Miquito a la vista!» [P].</para>
    /// <para>El contador se repinta en <c>LabelsLevelLoaded</c> y <c>LabelsBoardChanged</c> (tras cada vertido, Marea,
    /// reanudación, cuia añadida y al final de <c>OnIntroStarted</c>, así que Volver a la orilla lo devuelve a su valor sin
    /// esperar al siguiente vertido) con <see cref="LevelSession.Counter"/>: solo baja con vertidos útiles (Q18 B). Sin
    /// asignaciones por fotograma.</para>
    /// <para><b>Panel bajo la fila del HUD</b> (3 filas: el panel se recorta en <c>BoardTop − 8 = TopRowY + 40</c>, BoardLayout):
    /// la columna de píldoras de la cabecera (Semillas y Canoas, <c>TopRowY + 1..49</c>) sube lo justo para no pisar el canto
    /// del panel, sin salir de la zona segura (<see cref="KeepWalletOffPanel"/>), y el marco de la etiqueta no asoma por
    /// arriba, donde pasaría a trozos bajo la insignia, las píldoras y Reiniciar (<see cref="DifficultyFrameTucked"/>) [P].</para>
    /// </remarks>
    public sealed partial class GameScreen
    {
        /// <summary>Borde izquierdo de la insignia del nivel (x 56-192, guía 3.5).</summary>
        internal const float HudBadgeLeft = 56f;

        /// <summary>La banda de las etiquetas empieza 50 dp bajo la fila superior (bajo la insignia de 52 dp en TopRowY − 2).</summary>
        internal const float HudLabelsBelowRowDp = 50f;

        /// <summary>
        /// Variante «pegada bajo la placa» (3 filas): la parte alta del chip compacto 36 dp bajo la fila superior, sobre el canto
        /// inferior de la placa, ≈ 3 dp por debajo del contorno del texto «Nivel {n}» y ≥ 5 dp por encima de las cuias elevadas
        /// de la fila 1 (que con 3 filas empiezan en TopRowY + 61,5) [P].
        /// </summary>
        internal const float HudLabelsTuckedDp = 36f;

        /// <summary>Hueco entre chips de la banda (dp).</summary>
        internal const float HudLabelsGapDp = 4f;

        /// <summary>Holgura con el canto del panel y con las cuias elevadas de la fila 1 (dp).</summary>
        internal const float HudLabelsClearDp = 2f;

        /// <summary>Margen lateral del contador (x ≈ W − 8 − ancho).</summary>
        internal const float HudCounterRightDp = 8f;

        /// <summary>Fundido de entrada de las etiquetas al terminar la intro [P].</summary>
        internal const float HudLabelsFadeMs = 180f;

        /// <summary>Marco propio alrededor del panel (07 §7.7: «3 dp ámbar/índigo alrededor del tablero»).</summary>
        internal const float DifficultyFrameDp = 3f;

        /// <summary>Velo de Muy difícil: el fondo un 15 % más oscuro (02 §10).</summary>
        internal const float NightVeilAlpha = 0.15f;

        /// <summary>Lo que crece el panel de victoria para el chip del nivel siguiente (chip de 30 dp y 4 de aire).</summary>
        internal const float NextChipRowDp = 34f;

        /// <summary>De la píldora del chip al borde superior de Siguiente (como en Jugar del inicio: 6 dp).</summary>
        internal const float NextChipGapDp = 6f;

        /// <summary>
        /// Aire que se busca entre la columna de píldoras de la cabecera (Semillas y Canoas) y el canto del panel o del marco
        /// (dp) [P]. Con el panel recortado bajo el HUD (3 filas) no cabe: la columna sube hasta la zona segura y Canoas acaba
        /// justo en el canto (holgura 0).
        /// </summary>
        internal const float WalletPanelClearDp = 2f;

        // Radio exterior del marco tallado del panel (radio 24 más el contorno de 1,5 dp; ProceduralArt.BuildPanelFrame), que
        // se escala con el 9-slice cuando el marco adelgaza (PanelFrameDp).
        private const float PanelOuterRadiusDp = 25.5f;

        private RectTransform _labelsHud = null!;
        private CanvasGroup _labelsHudGroup = null!;
        private DifficultyChip _hudChip = null!;
        private MurkyChip _hudMurky = null!;
        private HudCounterChip _hudCounter = null!;
        private Image _difficultyFrame = null!;
        private Image _nightVeil = null!;
        private DifficultyChip _nextChip = null!;
        private CanvasGroup _nextChipGroup = null!;
        private HudLabelMode _hudMode = HudLabelMode.Full;
        private string _hudLabel = LevelDefinition.LabelNormal;
        private float _hudLabelsAlpha;
        private int _hudCounterIndex = -1;
        private bool _labelsBuilt;
        private bool _frameTucked;
        private float _frameTopDp;

        // Columna de píldoras de la cabecera tal como la dejó KeepWalletOffPanel (para ver en LabelsTick si GameScreen la ha
        // vuelto a colocar: SyncChrome la pone en TopRowY al cambiar de fase).
        private float _walletLift;
        private float _walletSeedsY = float.NaN;
        private bool _walletVisible;
        private bool _walletCompact;
        private bool _walletCanoes;

        /// <summary>Cómo va la banda de las etiquetas bajo la insignia.</summary>
        internal enum HudLabelMode
        {
            /// <summary>Chips normales (30 dp) en TopRowY + 50, por encima del canto del panel.</summary>
            Full = 0,

            /// <summary>Chips compactos (20 dp) en TopRowY + 50, por encima de las cuias elevadas de la fila 1.</summary>
            CompactBelow = 1,

            /// <summary>Chips compactos pegados bajo la placa de la insignia (tableros de 3 filas), con el contador en el grupo.</summary>
            Tucked = 2,
        }

        /// <summary>Chip de dificultad del HUD (tests).</summary>
        internal DifficultyChip HudDifficultyChip => _hudChip;

        /// <summary>Chip «?» de Aguas turbias del HUD (tests).</summary>
        internal MurkyChip HudMurkyChip => _hudMurky;

        /// <summary>Contador de Lluvia o Piraña del HUD (tests).</summary>
        internal HudCounterChip HudCounter => _hudCounter;

        /// <summary>Marco de 3 dp alrededor del panel (tests).</summary>
        internal Image DifficultyFrame => _difficultyFrame;

        /// <summary>Velo de Muy difícil detrás del panel (tests).</summary>
        internal Image NightVeil => _nightVeil;

        /// <summary>Chip del nivel siguiente sobre Siguiente (tests).</summary>
        internal DifficultyChip NextChip => _nextChip;

        /// <summary>Forma de la banda de las etiquetas (tests).</summary>
        internal HudLabelMode HudLabelsMode => _hudMode;

        /// <summary>Opacidad de la banda de las etiquetas (0 durante la intro; tests).</summary>
        internal float HudLabelsAlpha => _labelsHudGroup.alpha;

        /// <summary>Opacidad del chip del nivel siguiente (sigue a la de Siguiente; tests).</summary>
        internal float NextChipAlpha => _nextChipGroup.alpha;

        /// <summary>Insignia «Nivel {n}» (tests de solape).</summary>
        internal RectTransform LevelBadgeRect => _badge.rectTransform;

        /// <summary>Texto «Nivel {n}» de la insignia (tests de solape).</summary>
        internal TextMeshProUGUI LevelTitle => _levelText;

        /// <summary>Panel de victoria (tests de solape).</summary>
        internal RectTransform VictoryPanelRect => _victoryPanel;

        /// <summary>Fila superior del HUD (dp desde arriba).</summary>
        internal float TopRowDp => TopRowY;

        /// <summary>Borde de la zona segura superior (dp desde arriba; la fila del HUD va 8 dp más abajo).</summary>
        internal float SafeTopDp => _safeTop;

        /// <summary>
        /// El marco de la etiqueta empieza en el canto del panel y no asoma por arriba (el panel pasa bajo la fila del HUD): solo
        /// se ve por los lados y por abajo.
        /// </summary>
        internal bool DifficultyFrameTucked => _frameTucked;

        /// <summary>Borde de arriba del marco de la etiqueta (dp desde arriba): <c>PanelY − 3</c>, o <c>PanelY</c> si va recogido.</summary>
        internal float DifficultyFrameTopDp => _frameTopDp;

        /// <summary>Lo que ha subido la columna de píldoras de la cabecera para no pisar el panel (dp; 0 si no hizo falta).</summary>
        internal float WalletLiftDp => _walletLift;

        /// <summary>Color del marco de una etiqueta: <c>ui_ambar</c> en Difícil y <c>ui_indigo</c> en Muy difícil (07 §7.7).</summary>
        internal static Color FrameColorFor(bool veryHard)
        {
            return veryHard ? (Color)Palette.UiIndigo : (Color)Palette.UiAmber;
        }

        partial void LabelsBuild()
        {
            // Banda del HUD, justo encima de la insignia (el chip «pegado bajo la placa» se pinta sobre su canto inferior).
            _labelsHud = UiBuilder.Rect("HudLabels", _hud);
            _labelsHud.SetSiblingIndex(_badge.transform.GetSiblingIndex() + 1);
            _labelsHudGroup = _labelsHud.gameObject.AddComponent<CanvasGroup>();
            _labelsHudGroup.blocksRaycasts = false;
            _labelsHudGroup.interactable = false;
            _labelsHudGroup.alpha = 0f;
            _hudChip = DifficultyChip.Create(_labelsHud, _art);
            _hudMurky = MurkyChip.Create(_labelsHud, _art);
            _hudCounter = HudCounterChip.Create(_labelsHud, _art);

            // Marco propio (no _panelFrame): un rectángulo redondeado detrás del panel, sobre su sombra, que asoma 3 dp alrededor.
            _difficultyFrame = UiBuilder.Sliced("DifficultyFrame", _board, _art.RoundedRect, FrameColorFor(false));
            _difficultyFrame.transform.SetSiblingIndex(_panelShadow.transform.GetSiblingIndex() + 1);
            _difficultyFrame.gameObject.SetActive(false);

            // Velo propio (no _sceneVeil): en la escena, delante del follaje y detrás de las luces de victoria y del tablero.
            Transform scene = _sceneVeil.transform.parent;
            _nightVeil = UiBuilder.Image("NightVeil", scene, null, new Color(0f, 0f, 0f, NightVeilAlpha));
            UiBuilder.Stretch(_nightVeil.rectTransform);
            _nightVeil.transform.SetSiblingIndex(_sceneVeil.transform.GetSiblingIndex() + 1);
            _nightVeil.gameObject.SetActive(false);

            // Chip del nivel siguiente, dentro del panel de victoria (aparece con Siguiente: su opacidad la sigue).
            _nextChip = DifficultyChip.Create(_victoryPanel, _art);
            _nextChipGroup = _nextChip.Root.gameObject.AddComponent<CanvasGroup>();
            _nextChipGroup.blocksRaycasts = false;
            _nextChipGroup.interactable = false;
            _nextChipGroup.alpha = 0f;
            _labelsBuilt = true;
        }

        partial void LabelsLayoutHud()
        {
            // LayoutHud acaba de poner la cabecera en la fila superior (LayoutWalletBar): con el panel bajo el HUD, se aparta.
            if (_labelsBuilt)
            {
                LayoutDifficultyFrame();
                LayoutHudLabels();
                KeepWalletOffPanel();
            }
        }

        partial void LabelsLayoutBoard()
        {
            if (_labelsBuilt)
            {
                LayoutDifficultyFrame();
                LayoutHudLabels();
                KeepWalletOffPanel();
            }
        }

        partial void LabelsLevelLoaded(LevelFlow flow)
        {
            // 02 §10 y 07 §7.7: la etiqueta sale de difficulty.label del nivel, nunca del premio (Remote Config puede igualar
            // tramos). En el HUD el chip va sin cinta.
            LevelDefinition level = flow.Level;
            string label = level.DifficultyLabel;
            _hudLabel = label;
            _hudChip.Set(label, 1);
            _hudMurky.SetVisible(level.IsMurky);
            bool veryHard = label == LevelDefinition.LabelVeryHard;
            bool labeled = veryHard || label == LevelDefinition.LabelHard;
            _difficultyFrame.color = FrameColorFor(veryHard);
            _difficultyFrame.gameObject.SetActive(labeled);
            _nightVeil.gameObject.SetActive(veryHard);
            _hudCounterIndex = -1;
            _hudCounter.Show(LevelCounter.None, 0);
            RefreshHudCounter(flow);
            _hudLabelsAlpha = 0f;
            _labelsHudGroup.alpha = 0f;
            LayoutDifficultyFrame();
            LayoutHudLabels();
            KeepWalletOffPanel();
            EmitHudLabelProbe(flow);
        }

        partial void LabelsBoardChanged(LevelFlow flow)
        {
            RefreshHudCounter(flow);
            UpdateHudLabelsAlpha(0f); // al reiniciar (OnIntroStarted) se ocultan en el acto, sin esperar al Tick
            if (WalletMoved())
            {
                KeepWalletOffPanel(); // OnResumed y OnCuiaAdded pasan por SyncChrome justo antes
            }
        }

        partial void LabelsVictoryShown(LevelFlow flow)
        {
            // El chip ya está colocado (LabelsLayoutVictory); entra con Siguiente, que acaba de volver a 0 (ApplyVictoryAnim).
            _nextChipGroup.alpha = _nextChip.Visible ? _nextGroup.alpha : 0f;
            if (!DevProbe.Enabled)
            {
                return;
            }

            int next = flow.LevelNumber + 1;
            string json = "{\"level\":" + flow.LevelNumber.ToString(CultureInfo.InvariantCulture)
                + ",\"next\":" + next.ToString(CultureInfo.InvariantCulture)
                + ",\"next_label\":\"" + (_nextChip.Visible ? _nextChip.Label : LevelDefinition.LabelNormal) + "\""
                + ",\"ribbon\":" + (_nextChip.RibbonShown ? _nextChip.Multiplier : 0).ToString(CultureInfo.InvariantCulture) + "}";
            _probe.Event("hud_label", json);
        }

        partial void LabelsLayoutVictory()
        {
            if (!_labelsBuilt)
            {
                return;
            }

            // Propuesta §c.1: «Siguiente» lleva el chip y la cinta del nivel N + 1 (con D-008 se pasa directo a él sin ver el
            // inicio). La etiqueta, de difficulty.label del nivel siguiente; la cinta, de eco_reward_* (§c.2).
            LevelFlow? flow = _flow;
            string? next = null;
            if (flow != null && flow.HasLevel && (flow.Phase == FlowPhase.VictoryScreen || flow.Phase == FlowPhase.AdGate))
            {
                next = flow.Catalog.Find(flow.LevelNumber + 1)?.DifficultyLabel;
            }

            int multiplier = next == null ? 1 : DifficultyChip.MultiplierFor(_services?.Wallet.Config ?? EconomyConfig.Defaults, next);
            _nextChip.Set(next, multiplier);
            if (!_nextChip.Visible)
            {
                _nextChipGroup.alpha = 0f;
                return;
            }

            // El chip va 6 dp por encima de Siguiente, como en Jugar: Siguiente e Inicio bajan NextChipRowDp y el panel crece lo
            // mismo hacia arriba (sigue anclado abajo). LayoutVictory acaba de colocarlo todo, así que esto se rehace entero en
            // cada llamada.
            // OJO, COPIA: desde aquí hasta los rayos, el brillo, el velo y ModalSurfaceFor se repite el final de
            // GameScreen.LayoutVictory (GameScreen.cs, desde «_victoryCtaTopDp = y» hasta el final del método) con el panel más
            // alto. Si cambia allí, hay que cambiarlo aquí. Pendiente de la sesión principal: un gancho
            // «partial void LabelsVictoryReserve(ref float y)» justo antes de «_victoryCtaTopDp = y» para reservar
            // NextChipRowDp sin recolocar el panel (y sin pedir a ModalSurfaceFor una superficie que no se usa).
            float w = _canvasW;
            float panelW = Mathf.Min(328f, w - 32f);
            MoveDown((RectTransform)_next.Button.transform, NextChipRowDp);
            MoveDown((RectTransform)_home.Button.transform, NextChipRowDp);
            _victoryCtaTopDp += NextChipRowDp;
            _victoryHomeTopDp += NextChipRowDp;
            _victoryPanelDp += NextChipRowDp;
            _victoryPanelY = _canvasH - _safeBottom - 8f - _victoryPanelDp;
            UiBuilder.Place(_victoryPanel, (w - panelW) / 2f, _victoryPanelY, panelW, _victoryPanelDp);
            _victorySurface.sprite = _art.ModalSurfaceFor(panelW - 16f, _victoryPanelDp - 16f);
            float ribbonY = _victoryPanelY - RibbonRiseDp + (RibbonHeightDp * 0.5f);
            PlaceAt(_victoryRays.rectTransform, w * 0.5f, ribbonY, 2f * VictoryRaysRadiusDp, 2f * VictoryRaysRadiusDp, new Vector2(0.5f, 0.5f));
            PlaceAt(_victoryGlow.rectTransform, w * 0.5f, ribbonY, 2f * VictoryGlowRadiusDp, 2f * VictoryGlowRadiusDp, new Vector2(0.5f, 0.5f));
            PlaceAt(_victoryVeil.rectTransform, w * 0.5f, ribbonY, 2f * ProceduralArt.VeilHoleHalfDp, 2f * ProceduralArt.VeilHoleHalfDp, new Vector2(0.5f, 0.5f));
            _nextChip.PlaceCentered(panelW * 0.5f, _victoryCtaTopDp - NextChipGapDp - _nextChip.PillBottom);
        }

        partial void LabelsTick(float dtMs)
        {
            UpdateHudLabelsAlpha(dtMs);
            if (WalletMoved())
            {
                KeepWalletOffPanel(); // SyncChrome (cambio de fase) o Refresh (aparece Canoas) la volvieron a colocar
            }

            if (_nextChip.Visible)
            {
                float a = _nextGroup.alpha;
                if (_nextChipGroup.alpha != a)
                {
                    _nextChipGroup.alpha = a;
                }
            }
        }

        // Las etiquetas del HUD se ven en Playing y en la celebración con la insignia a la vista; entran con un fundido al
        // terminar la intro (y tras el rótulo del intento o «El río bajó», y la tarjeta del miquito) y se van en el acto.
        private void UpdateHudLabelsAlpha(float dtMs)
        {
            LevelFlow? flow = _flow;
            bool show = flow != null && flow.HasLevel && (flow.Phase == FlowPhase.Playing || flow.Phase == FlowPhase.Celebration)
                && _badge.gameObject.activeSelf && !_banner.Visible && !_sighting.Visible;
            float target = show ? 1f : 0f;
            if (_hudLabelsAlpha != target)
            {
                _hudLabelsAlpha = show ? Mathf.Min(1f, _hudLabelsAlpha + (dtMs / HudLabelsFadeMs)) : 0f;
                _labelsHudGroup.alpha = _hudLabelsAlpha;
            }
        }

        // Contador del núcleo (Q18 B: solo baja con vertidos útiles) con las gotitas del agua de la Lluvia.
        private void RefreshHudCounter(LevelFlow flow)
        {
            if (!flow.HasLevel)
            {
                return;
            }

            LevelCounter counter = flow.Session.Counter;
            int tint = 0;
            if (counter.IsActive && counter.Type == MechanicType.Lluvia)
            {
                BoardState board = flow.Session.Current;
                tint = board[counter.CuiaIndex].MechanicKey;
            }

            int index = counter.IsActive ? counter.CuiaIndex : -1;
            bool moved = index != _hudCounterIndex;
            _hudCounterIndex = index;
            if (_hudCounter.Show(counter, tint) || moved)
            {
                LayoutHudLabels();
            }
        }

        // Marco de 3 dp: un rectángulo redondeado (radio exterior del marco tallado + 3) detrás del panel, que solo asoma
        // alrededor. Sigue al marco cuando este adelgaza (PanelFrameDp) y al panel recortado bajo el HUD (3 filas).
        // Con el canto de arriba bajo la fila del HUD (la insignia acaba en TopRowY + 50), la línea pasaría bajo la insignia,
        // las píldoras y Reiniciar y asomaría a trozos entre ellos: ahí el marco empieza en el canto del panel, su lado de
        // arriba queda detrás del panel y solo se ve por los lados y por abajo, saliendo de debajo del HUD por las esquinas
        // redondeadas (el panel tallado también pasa bajo el HUD) [P].
        private void LayoutDifficultyFrame()
        {
            BoardLayoutResult? l = _layout;
            if (l == null || _views.Length == 0)
            {
                _difficultyFrame.enabled = false;
                _frameTucked = false;
                return;
            }

            _difficultyFrame.enabled = true;
            float scale = _art.PanelFrameDp > 0f ? Mathf.Clamp01(PanelFrameDp / _art.PanelFrameDp) : 1f;
            float radius = (PanelOuterRadiusDp * scale) + DifficultyFrameDp;
            _difficultyFrame.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / radius;
            float d = DifficultyFrameDp;
            _frameTucked = l.PanelY - d < TopRowY + HudLabelsBelowRowDp;
            _frameTopDp = _frameTucked ? l.PanelY : l.PanelY - d;
            float bottom = l.PanelY + l.PanelHeight + d;
            UiBuilder.Place(_difficultyFrame.rectTransform, l.PanelX - d, _frameTopDp, l.PanelWidth + (2f * d), bottom - _frameTopDp);
        }

        // NOTAS de la ola 2 («Para T09»): con el panel recortado bajo el HUD (3 filas: PanelY = BoardTop − 8 = TopRowY + 40),
        // la píldora de Canoas «5/5» de la cabecera (TopRowY + 27..49, WalletBar) pisaba 9 dp del canto de arriba del panel
        // (y 12 del marco). La columna de Semillas y Canoas sube lo que haga falta para acabar WalletPanelClearDp por encima del
        // canto (el del marco si asoma por arriba; si no, el del panel), pero nunca por encima de la zona segura: la píldora de
        // Semillas, como mucho, en _safeTop = TopRowY − 8. En 3 filas eso da 9 dp, justo lo que pisaba, así que Canoas acaba
        // en el canto del panel, sin aire [P]. Solo la cabecera de partida (Compact): el saldo grande del embudo y de la
        // victoria no es del HUD. Parte de donde la pone GameScreen (LayoutWalletBar) para medir sus píldoras sin repetir su
        // maquetación. Sin asignaciones.
        private void KeepWalletOffPanel()
        {
            RectTransform seeds = _walletBar.SeedsRect;
            RectTransform canoes = _walletBar.CanoesRect;
            _walletVisible = _walletBar.Visible;
            _walletCompact = _walletBar.Compact;
            _walletCanoes = canoes.gameObject.activeSelf;
            float lift = 0f;
            BoardLayoutResult? l = _layout;
            if (_walletVisible && _walletCompact)
            {
                LayoutWalletBar(false); // cabecera de partida: en la fila superior (TopRowY)
            }

            if (_walletVisible && _walletCompact && l != null && _views.Length > 0)
            {
                float seedsTop = -seeds.anchoredPosition.y;
                float bottom = _walletCanoes ? -canoes.anchoredPosition.y + canoes.sizeDelta.y : seedsTop + seeds.sizeDelta.y;
                float edge = _difficultyFrame.gameObject.activeSelf && _difficultyFrame.enabled ? Mathf.Min(_frameTopDp, l.PanelY) : l.PanelY;
                float room = Mathf.Max(0f, seedsTop - _safeTop);
                lift = Mathf.Clamp(bottom + WalletPanelClearDp - edge, 0f, room);
                if (lift > 0f)
                {
                    _walletBar.Layout(seeds.anchoredPosition.x, TopRowY - lift, seeds.sizeDelta.x);
                }
            }

            _walletLift = lift;
            _walletSeedsY = seeds.anchoredPosition.y;
        }

        // La cabecera ya no está donde la dejó KeepWalletOffPanel (otra forma, otra fila, Canoas que aparece o desaparece).
        private bool WalletMoved()
        {
            return _walletBar.Visible != _walletVisible || _walletBar.Compact != _walletCompact
                || _walletBar.CanoesRect.gameObject.activeSelf != _walletCanoes
                || _walletBar.SeedsRect.anchoredPosition.y != _walletSeedsY;
        }

        // Banda de las etiquetas (ver la clase): forma, altura y posición del grupo (chip, «?» y, en 3 filas, el contador) y del
        // contador a la derecha. Sin asignaciones (también en cada fotograma del relayout de una cuia añadida).
        private void LayoutHudLabels()
        {
            bool chip = _hudChip.Visible;
            bool murky = _hudMurky.Visible;
            bool counter = _hudCounter.Visible;
            if (!chip && !murky && !counter)
            {
                return;
            }

            float top = TopRowY;
            float below = top + HudLabelsBelowRowDp;
            float panelTop = float.MaxValue;
            float liftedTop = float.MaxValue;
            float headTop = float.MaxValue;
            float headLeft = 0f;
            float headRight = 0f;
            BoardLayoutResult? l = _layout;
            if (l != null)
            {
                panelTop = l.PanelY;
                float u = l.CuiaWidth * 0.5f;
                int n = Mathf.Min(_views.Length, l.RowOf.Length);
                for (int i = 0; i < n; i++)
                {
                    if (l.RowOf[i] != 0)
                    {
                        continue;
                    }

                    float lifted = l.FootY[i] - (_views[i].HeightU * u) - CuiaView.LiftDp;
                    liftedTop = Mathf.Min(liftedTop, lifted);
                    if (i == _hudCounterIndex)
                    {
                        // Cielo de la cuia del contador: su nube y sus puntos van en los 32 dp táctiles sobre la boca (02 §3.3).
                        headTop = l.FootY[i] - (_views[i].HeightU * u) - CuiaView.TouchHeadroomDp;
                        headLeft = l.CenterX[i] - u - HudLabelsClearDp;
                        headRight = l.CenterX[i] + u + HudLabelsClearDp;
                    }
                }
            }

            HudLabelMode mode;
            float y;
            if (panelTop - below >= DifficultyChip.HeightDp + HudLabelsClearDp)
            {
                mode = HudLabelMode.Full;
                y = below;
            }
            else if (liftedTop - below >= DifficultyChip.CompactHeightDp + HudLabelsClearDp)
            {
                mode = HudLabelMode.CompactBelow;
                y = below;
            }
            else
            {
                mode = HudLabelMode.Tucked;
                y = Mathf.Min(top + HudLabelsTuckedDp, liftedTop - HudLabelsClearDp - DifficultyChip.CompactHeightDp);
            }

            _hudMode = mode;
            bool compact = mode != HudLabelMode.Full;
            _hudChip.SetCompact(compact);
            _hudCounter.SetCompact(compact);
            float chipH = compact ? DifficultyChip.CompactHeightDp : DifficultyChip.HeightDp;
            float murkyH = compact ? MurkyChip.CompactHeightDp : MurkyChip.HeightDp;
            float murkyW = MurkyChip.WidthFor(murkyH);
            float centerY = y + (chipH * 0.5f);
            bool overlapsHead = headTop < y + chipH;
            float gap = HudLabelsGapDp;

            // El contador, a la derecha de la banda (x ≈ W − 8 − ancho), salvo pegado bajo la placa: ahí, en el grupo.
            bool counterInGroup = counter && mode == HudLabelMode.Tucked;
            float counterX = _canvasW - HudCounterRightDp - _hudCounter.Width;
            if (counter && !counterInGroup && overlapsHead && counterX < headRight && counterX + _hudCounter.Width > headLeft)
            {
                counterX = headLeft - gap - _hudCounter.Width;
            }

            float groupW = 0f;
            if (chip)
            {
                groupW += _hudChip.Width;
            }

            if (murky)
            {
                groupW += (groupW > 0f ? gap : 0f) + murkyW;
            }

            if (counterInGroup)
            {
                groupW += (groupW > 0f ? gap : 0f) + _hudCounter.Width;
            }

            // Grupo centrado bajo la insignia. Pegado bajo la placa no sale de ella (a los lados, la pausa y los saldos); si no,
            // va de 8 dp al contador.
            float minX = mode == HudLabelMode.Tucked ? HudBadgeLeft : 8f;
            float maxRight = mode == HudLabelMode.Tucked ? HudBadgeRight : (counter && !counterInGroup ? counterX - gap : _canvasW - 8f);
            float x = Mathf.Clamp(HudBadgeLeft + ((HudBadgeRight - HudBadgeLeft - groupW) * 0.5f), minX, Mathf.Max(minX, maxRight - groupW));
            if (overlapsHead && groupW > 0f && x < headRight && x + groupW > headLeft)
            {
                // Fuera del cielo de la cuia del contador: a su derecha si cabe; si no, a su izquierda.
                if (headRight + groupW <= maxRight)
                {
                    x = headRight;
                }
                else if (headLeft - groupW >= minX)
                {
                    x = headLeft - groupW;
                }
            }

            x = Mathf.Round(x);
            if (chip)
            {
                _hudChip.Place(x, y);
                x += _hudChip.Width + gap;
            }

            if (murky)
            {
                _hudMurky.Place(x, Mathf.Round(centerY - (murkyH * 0.5f)), murkyH);
                x += murkyW + gap;
            }

            if (counter)
            {
                float cy = Mathf.Round(centerY - (_hudCounter.Height * 0.5f));
                _hudCounter.Place(counterInGroup ? x : Mathf.Round(counterX), cy);
            }
        }

        // AF_STATE {"event":"hud_label",…} con la etiqueta, el «?», el marco, el velo, la forma de la banda y el contador.
        private void EmitHudLabelProbe(LevelFlow flow)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            bool veryHard = _hudLabel == LevelDefinition.LabelVeryHard;
            bool labeled = veryHard || _hudLabel == LevelDefinition.LabelHard;
            string counter = _hudCounter.Visible ? MechanicTypes.ToKey(_hudCounter.Type) : "none";
            string json = "{\"level\":" + flow.LevelNumber.ToString(CultureInfo.InvariantCulture)
                + ",\"label\":\"" + _hudLabel + "\""
                + ",\"murky\":" + (_hudMurky.Visible ? "true" : "false")
                + ",\"frame\":\"" + (labeled ? (veryHard ? "indigo" : "amber") : "none") + "\""
                + ",\"veil\":" + (veryHard ? NightVeilAlpha.ToString("0.00", CultureInfo.InvariantCulture) : "0")
                + ",\"mode\":\"" + ModeKey(_hudMode) + "\""
                + ",\"counter\":\"" + counter + "\""
                + ",\"remaining\":" + (_hudCounter.Visible ? _hudCounter.Remaining : 0).ToString(CultureInfo.InvariantCulture) + "}";
            _probe.Event("hud_label", json);
        }

        private static string ModeKey(HudLabelMode mode)
        {
            switch (mode)
            {
                case HudLabelMode.CompactBelow: return "compact";
                case HudLabelMode.Tucked: return "tucked";
                default: return "full";
            }
        }

        private static void MoveDown(RectTransform rt, float dy)
        {
            Vector2 p = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(p.x, p.y - dy);
        }
    }
}
