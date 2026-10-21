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
    /// (<see cref="HudCounterChip"/>), siempre aparte de ese grupo;</description></item>
    /// <item><description>alrededor del panel, un marco propio de 3 dp ámbar (Difícil) o índigo (Muy difícil), y en Muy difícil
    /// la variante noche: desde B14, la LUT de noche del fondo (<see cref="StopBackdrop.NightLut"/> [P]; y en Difícil, la luz de
    /// tarde, <see cref="StopBackdrop.AfternoonLut"/> [P]), que sustituye al velo del 15 % del paso 1 (02 §10);</description></item>
    /// <item><description>en la victoria, el chip del nivel N + 1 con su cinta «×3»/«×4» colgado de Siguiente (el panel crece
    /// <see cref="NextChipRowDp"/> para hacerle sitio).</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para><b>Banda de las etiquetas</b> (<see cref="HudLabelMode"/>, <see cref="ModeFor"/>): con sitio entre la fila superior
    /// y el canto de arriba del tablero (el del marco si asoma, si no el del panel), chips normales (30 dp) en
    /// <c>TopRowY + 50</c>; si solo caben 20 dp (por encima del canto y de las cuias elevadas de la fila 1, como mucho hasta el
    /// pie de la fila del HUD), la variante compacta en esa línea; si no (tableros de 3 filas, con el panel recortado bajo el
    /// HUD), compacta y «pegada bajo la placa» (sobre el canto inferior de la insignia, bajo el texto, entre x 56 y 192: a los
    /// lados están la pausa, los saldos y Reiniciar) [P]. Si la banda pisa el panel que rodea las cuias (3 filas), el chip va sin sello: los colores de
    /// la rana y del jaguar no llegan a ΔE00 10 de los tintes (direccion-visual-v2 §2.4, regla 1; 07 §3, regla 6); lo que
    /// queda sobre el panel (la píldora, el «?» y el contador) sí llega, salvo el índigo de Muy difícil frente al Huito, que es
    /// el mismo par que el marco de 07 §7.7 (C13, abierta).</para>
    /// <para><b>Contador</b>: nunca en el mismo grupo que las etiquetas de dificultad («Muy difícil 4» repetiría el patrón de la
    /// cinta «×4» y haría creer que el premio baja con cada jugada, 02 §10). En la banda normal va a la derecha (x = W − 8 −
    /// ancho, T09 §5); pegado bajo la placa va a ≥ <see cref="HudCounterGapDp"/> del último chip, con su propio material (cara
    /// clara, cifra en tinta y contorno de madera oscura) y el icono de la mecánica delante de la cifra. A la derecha de esa
    /// banda no cabe: la píldora «Reiniciar» ocupa <c>TopRowY + 32..48</c> en x W − 68..W − 8. Si bajo la placa no caben los
    /// dos (el 160 en 16:9), el contador pasa a la derecha de la insignia, bajo la cabecera compactada y a la izquierda de
    /// Reiniciar (<see cref="HudCounterBesideWallet"/>) [P]. Todo se aparta del cielo de la cuia del contador si está en la
    /// fila 1: su nube y sus puntos van en los 32 dp táctiles sobre la boca (02 §3.3) y suben 16 dp con ella al seleccionarla
    /// (LluviaOverlay: la nube no es más ancha que la cuia).</para>
    /// <para><b>Cuándo se ven</b>: en Playing y en la celebración, con la insignia a la vista (el embudo y la victoria la
    /// ocultan). Durante la intro no: la tropa de miquitos se sienta en la franja entre el HUD y el panel; aparecen con un
    /// fundido de <see cref="HudLabelsFadeMs"/> al terminar, y también esperan al rótulo del intento o «El río bajó» y a la
    /// tarjeta «¡Miquito a la vista!» [P].</para>
    /// <para>El contador se repinta en <c>LabelsLevelLoaded</c> y <c>LabelsBoardChanged</c> (tras cada vertido, Marea,
    /// reanudación, cuia añadida y al final de <c>OnIntroStarted</c>, así que Volver a la orilla lo devuelve a su valor sin
    /// esperar al siguiente vertido) con <see cref="LevelSession.Counter"/>: solo baja con vertidos útiles (Q18 B). Sin
    /// asignaciones por fotograma.</para>
    /// <para><b>Panel bajo la fila del HUD</b> (3 filas: el panel se recorta en <c>BoardTop − 8 = TopRowY + 40</c>, BoardLayout):
    /// la columna de píldoras de la cabecera (Semillas sobre Canoas, de 22 dp con 4 de hueco: <c>TopRowY + 1..49</c>) pisaría
    /// 9 dp del canto. En vez de subirla fuera de la fila, se compacta dentro de ella (<c>LabelsWalletColumn</c>, el gancho de
    /// <c>GameScreen.LayoutWalletBar</c>, que se la pasa a <see cref="WalletBar.Layout(float, float, float, float, float,
    /// float)"/>): píldoras de <see cref="WalletSqueezedPillDp"/> con <see cref="WalletSqueezedGapDp"/> de hueco, de
    /// <c>TopRowY</c> a <c>TopRowY + 38</c>, <see cref="WalletPanelClearDp"/> por encima del canto; el marco de la etiqueta no
    /// asoma por arriba, donde pasaría a trozos bajo la insignia, las píldoras y Reiniciar (<see cref="DifficultyFrameTucked"/>)
    /// [P].</para>
    /// </remarks>
    public sealed partial class GameScreen
    {
        /// <summary>Borde izquierdo de la insignia del nivel (x 56-192, guía 3.5).</summary>
        internal const float HudBadgeLeft = 56f;

        /// <summary>Borde derecho de la insignia del nivel (tests).</summary>
        internal const float HudBadgeRightDp = HudBadgeRight;

        /// <summary>La banda de las etiquetas empieza 50 dp bajo la fila superior (bajo la insignia de 52 dp en TopRowY − 2).</summary>
        internal const float HudLabelsBelowRowDp = 50f;

        /// <summary>
        /// Variante «pegada bajo la placa» (3 filas): la parte alta del chip compacto 36 dp bajo la fila superior, sobre el canto
        /// inferior de la placa, ≈ 3 dp por debajo del contorno del texto «Nivel {n}» y ≥ <see cref="HudLabelsTuckedClearDp"/>
        /// por encima de las cuias elevadas de la fila 1 (que con 3 filas empiezan en TopRowY + 61,5) [P].
        /// </summary>
        internal const float HudLabelsTuckedDp = 36f;

        /// <summary>Holgura mínima de la banda pegada bajo la placa con las cuias elevadas de la fila 1 (dp).</summary>
        internal const float HudLabelsTuckedClearDp = 4f;

        /// <summary>Hueco entre chips de la banda (dp).</summary>
        internal const float HudLabelsGapDp = 4f;

        /// <summary>
        /// Holgura de la banda normal y de la compacta con el canto de arriba visible del tablero y con las cuias elevadas de la
        /// fila 1 (dp).
        /// </summary>
        internal const float HudLabelsClearDp = 2f;

        /// <summary>Margen lateral del contador (x ≈ W − 8 − ancho).</summary>
        internal const float HudCounterRightDp = 8f;

        /// <summary>Distancia mínima entre el contador y el último chip de las etiquetas (dp) [P].</summary>
        internal const float HudCounterGapDp = 16f;

        /// <summary>Fundido de entrada de las etiquetas al terminar la intro [P].</summary>
        internal const float HudLabelsFadeMs = 180f;

        /// <summary>Marco propio alrededor del panel (07 §7.7: «3 dp ámbar/índigo alrededor del tablero»).</summary>
        internal const float DifficultyFrameDp = 3f;

        /// <summary>Velo de Muy difícil: el fondo un 15 % más oscuro (02 §10).</summary>
        internal const float NightVeilAlpha = 0.15f;

        /// <summary>Lo que crece el panel de victoria para el chip del nivel siguiente (dp).</summary>
        internal const float NextChipRowDp = 34f;

        /// <summary>
        /// El chip del nivel siguiente cuelga de Siguiente: su parte alta va 24 dp por encima del canto del botón, así que el chip
        /// de 30 dp monta <see cref="NextChipOverlapDp"/> sobre el botón, como la «x5» de Magic Sort (ficha §3.6) [P].
        /// </summary>
        internal const float NextChipHangDp = 24f;

        /// <summary>Lo que el chip del nivel siguiente monta sobre el canto de arriba de Siguiente (dp).</summary>
        internal const float NextChipOverlapDp = DifficultyChip.HeightDp - NextChipHangDp;

        /// <summary>Aire entre la columna de píldoras de la cabecera (Semillas y Canoas) y el canto del panel o del marco (dp) [P].</summary>
        internal const float WalletPanelClearDp = 2f;

        /// <summary>Alto de las píldoras de la cabecera compactada en la fila (dp; 22 normalmente) [P].</summary>
        internal const float WalletSqueezedPillDp = 18f;

        /// <summary>Hueco entre las píldoras de la cabecera compactada (dp; 4 normalmente) [P].</summary>
        internal const float WalletSqueezedGapDp = 2f;

        /// <summary>Escala de las píldoras de la cabecera compactada: 18 / 22 (las cifras de 15 dp quedan en 12,3 dp).</summary>
        internal const float WalletSqueezeScale = WalletSqueezedPillDp / WalletBar.CompactPillDp;

        // Alto de la fila superior del HUD (07 §7.4).
        private const float HudRowDp = 48f;

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
        private HudLabelMode _hudMode = HudLabelMode.None;
        private string _hudLabel = LevelDefinition.LabelNormal;
        private float _hudLabelsAlpha;
        private int _hudCounterIndex = -1;
        private bool _hudCounterBesideWallet;
        private bool _labelsBuilt;
        private bool _frameTucked;
        private float _frameTopDp;

        /// <summary>Cómo va la banda de las etiquetas bajo la insignia.</summary>
        internal enum HudLabelMode
        {
            /// <summary>No hay ninguna etiqueta a la vista (nivel normal, sin «?» ni contador).</summary>
            None = 0,

            /// <summary>Chips normales (30 dp) en TopRowY + 50, por encima del canto del tablero.</summary>
            Full = 1,

            /// <summary>Chips compactos (20 dp) en TopRowY + 50, por encima del canto del tablero y de las cuias elevadas.</summary>
            CompactBelow = 2,

            /// <summary>Chips compactos pegados bajo la placa de la insignia (tableros de 3 filas), sobre el panel y sin sello.</summary>
            Tucked = 3,
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

        /// <summary>Grupo del chip del nivel siguiente (tests: no recibe toques, así que Siguiente sigue tocándose debajo).</summary>
        internal CanvasGroup NextChipGroup => _nextChipGroup;

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

        /// <summary>La columna de píldoras de la cabecera va compactada en la fila para no pisar el canto del tablero.</summary>
        internal bool WalletSqueezed => _walletBar.Squeezed;

        /// <summary>El contador va a la derecha de la insignia, bajo la cabecera compactada (ver <c>PlaceCounterBesideWallet</c>).</summary>
        internal bool HudCounterBesideWallet => _hudCounterBesideWallet;

        /// <summary>Color del marco de una etiqueta: <c>ui_ambar</c> en Difícil y <c>ui_indigo</c> en Muy difícil (07 §7.7).</summary>
        internal static Color FrameColorFor(bool veryHard)
        {
            return veryHard ? (Color)Palette.UiIndigo : (Color)Palette.UiAmber;
        }

        /// <summary>
        /// Medio ancho (u) del cielo de la cuia del contador, sin la holgura: el de la cuia (1 u) o, con la Piraña, el de su cueva
        /// sobre la boca (<c>max(1, 0,5 × PiranaCaveWidthU)</c> = 1,2 u).
        /// </summary>
        internal static float SkyHalfWidthU(MechanicType counter)
        {
            return counter == MechanicType.Pirana ? Mathf.Max(1f, 0.5f * ProceduralArt.PiranaCaveWidthU) : 1f;
        }

        /// <summary>
        /// Forma de la banda de las etiquetas con la fila superior en <paramref name="topRowY"/>, el canto de arriba visible del
        /// tablero en <paramref name="edge"/> (el del marco si asoma) y las cuias elevadas de la fila 1 desde
        /// <paramref name="liftedTop"/> (dp desde arriba): <see cref="HudLabelMode.Full"/> si los 30 dp caben en
        /// <c>TopRowY + 50</c> con <see cref="HudLabelsClearDp"/> por encima de los dos; <see cref="HudLabelMode.CompactBelow"/>
        /// si los 20 dp caben entre el pie de la fila del HUD (<c>TopRowY + 48</c>) y los dos, también con
        /// <see cref="HudLabelsClearDp"/>; si no, <see cref="HudLabelMode.Tucked"/>. Así la banda nunca pisa el canto del panel ni
        /// el marco de la etiqueta.
        /// </summary>
        internal static HudLabelMode ModeFor(float topRowY, float edge, float liftedTop)
        {
            float limit = Mathf.Min(edge, liftedTop);
            if (limit - (topRowY + HudLabelsBelowRowDp) >= DifficultyChip.HeightDp + HudLabelsClearDp)
            {
                return HudLabelMode.Full;
            }

            return limit - (topRowY + HudRowDp) >= DifficultyChip.CompactHeightDp + HudLabelsClearDp
                ? HudLabelMode.CompactBelow
                : HudLabelMode.Tucked;
        }

        /// <summary>
        /// Parte alta de la banda (dp desde arriba) para <paramref name="mode"/>: <c>TopRowY + 50</c> en la normal; en la compacta,
        /// <see cref="HudLabelsClearDp"/> por encima del canto si cabe, sin subir del pie de la fila del HUD ni bajar de
        /// <c>TopRowY + 50</c>; pegada bajo la placa, <c>TopRowY + 36</c> y ≥ <see cref="HudLabelsTuckedClearDp"/> sobre las cuias
        /// elevadas.
        /// </summary>
        internal static float BandTopFor(HudLabelMode mode, float topRowY, float edge, float liftedTop)
        {
            float below = topRowY + HudLabelsBelowRowDp;
            switch (mode)
            {
                case HudLabelMode.CompactBelow:
                    float y = Mathf.Min(edge, liftedTop) - HudLabelsClearDp - DifficultyChip.CompactHeightDp;
                    return Mathf.Clamp(y, topRowY + HudRowDp, below);
                case HudLabelMode.Tucked:
                    return Mathf.Min(topRowY + HudLabelsTuckedDp, liftedTop - HudLabelsTuckedClearDp - DifficultyChip.CompactHeightDp);
                default:
                    return below;
            }
        }

        /// <summary>
        /// Coloca un tramo de <paramref name="width"/> dp en [<paramref name="lo"/>, <paramref name="hi"/>] lo más cerca posible de
        /// <paramref name="desired"/> y, si <paramref name="blocked"/>, fuera de [<paramref name="skyLeft"/>, <paramref name="skyRight"/>].
        /// False si no cabe.
        /// </summary>
        internal static bool FitSpan(float width, float desired, float lo, float hi, bool blocked, float skyLeft, float skyRight, out float x)
        {
            const float tolerance = 0.01f;
            x = desired;
            if (width <= 0f)
            {
                return true;
            }

            if (!blocked || skyRight <= lo || skyLeft >= hi)
            {
                if (width > hi - lo + tolerance)
                {
                    return false;
                }

                x = Mathf.Clamp(desired, lo, Mathf.Max(lo, hi - width));
                return true;
            }

            bool found = false;
            float leftHi = Mathf.Min(hi, skyLeft);
            if (leftHi - lo >= width - tolerance)
            {
                x = Mathf.Clamp(desired, lo, Mathf.Max(lo, leftHi - width));
                found = true;
            }

            float rightLo = Mathf.Max(lo, skyRight);
            if (hi - rightLo >= width - tolerance)
            {
                float c = Mathf.Clamp(desired, rightLo, Mathf.Max(rightLo, hi - width));
                if (!found || Mathf.Abs(c - desired) < Mathf.Abs(x - desired))
                {
                    x = c;
                }

                found = true;
            }

            return found;
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

            // Chip del nivel siguiente, dentro del panel de victoria y delante de Siguiente, del que cuelga (aparece con él: su
            // opacidad la sigue). No recibe toques: Siguiente se toca también bajo los 6 dp que monta.
            _nextChip = DifficultyChip.Create(_victoryPanel, _art);
            _nextChipGroup = _nextChip.Root.gameObject.AddComponent<CanvasGroup>();
            _nextChipGroup.blocksRaycasts = false;
            _nextChipGroup.interactable = false;
            _nextChipGroup.alpha = 0f;
            _labelsBuilt = true;
        }

        partial void LabelsLayoutHud()
        {
            // LayoutHud ya colocó la cabecera (LayoutWalletBar), pero antes de rehacer el marco y la banda, de los que depende la
            // columna Semillas/Canoas (LabelsWalletColumn): se vuelve a colocar al final.
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
            // B14: la noche de Muy difícil es ya la LUT del fondo (GameScreen.Stop.cs; 07 §7.7 [P]); el velo del 15 % del paso 1
            // solo vuelve con la luz de dificultad apagada (capturas de comparación).
            _nightVeil.gameObject.SetActive(veryHard && !DifficultyLightEnabled);
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

        partial void LabelsVictoryReserve(ref float y)
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

            // El chip cuelga de Siguiente (24 dp por encima de su canto y 6 sobre el botón, como la «x5» de Magic Sort): Siguiente
            // e Inicio bajan NextChipRowDp y el panel crece lo mismo hacia arriba (sigue anclado abajo), así que entre el chip y
            // la Cosecha doble de encima quedan ≈ 12 dp.
            if (_nextChip.Visible)
            {
                y += NextChipRowFor(_victoryCompact);
            }
        }

        partial void LabelsLayoutVictory()
        {
            if (!_labelsBuilt)
            {
                return;
            }

            if (!_nextChip.Visible)
            {
                _nextChipGroup.alpha = 0f;
                return;
            }

            // LayoutVictory ya bajó Siguiente lo que reservó LabelsVictoryReserve: el chip, centrado sobre su canto (en la fila de la
            // victoria compacta, sobre el centro de «Siguiente», D-032).
            _nextChip.PlaceCentered(_victoryNextCenterX, _victoryCtaTopDp - NextChipHangDp); // creado tras Siguiente: va delante
        }

        partial void LabelsTick(float dtMs)
        {
            UpdateHudLabelsAlpha(dtMs);
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
        // terminar la intro (y tras el rótulo del intento o «El río bajó», la tarjeta del miquito o la de «Fruta nueva», el rótulo
        // del lugar y el aviso de TiTino tras Volver a la orilla, TI6b) y se van en el acto. Esas tarjetas y el rótulo del lugar (M2.3: va en su banda, sobre el horizonte o bajo el
        // HUD) solo las apartan mientras se ven: mientras esperan a que la tropa se vaya, siguen a la vista (D-011, punto 3;
        // GameScreen.Notices.cs).
        private void UpdateHudLabelsAlpha(float dtMs)
        {
            LevelFlow? flow = _flow;
            bool show = flow != null && flow.HasLevel && (flow.Phase == FlowPhase.Playing || flow.Phase == FlowPhase.Celebration)
                && _badge.gameObject.activeSelf && !_banner.Visible && !_sighting.Visible && !_fruitNew.Visible && !_stopLabel.Visible
                && !SlothNoticeVisible;
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

        // Canto de arriba visible del tablero (dp desde arriba): el del marco de la etiqueta si asoma por arriba (PanelY − 3);
        // si no, el del panel.
        private float PanelEdgeDp(BoardLayoutResult l)
        {
            bool frame = _difficultyFrame.gameObject.activeSelf && _difficultyFrame.enabled && !_frameTucked;
            return frame ? Mathf.Min(_frameTopDp, l.PanelY) : l.PanelY;
        }

        // NOTAS de la ola 2 («Para T09») y revisión de T09: con el panel recortado bajo el HUD (3 filas: PanelY = BoardTop − 8 =
        // TopRowY + 40), la píldora de Canoas «5/5» de la cabecera (TopRowY + 27..49, WalletBar) pisaba 9 dp del canto de arriba
        // del panel. Subir la columna la sacaba de la fila de 48 dp de 07 §7.4 (hasta el borde de la zona segura, desalineada de
        // la pausa, la insignia y Reiniciar) y aun así acababa en el canto. Ahora se compacta DENTRO de la fila: Semillas y
        // Canoas a escala 18/22 (píldoras de 18 dp, cifras de 12,3 dp e icono de Semillas de 16,4 dp) con 2 dp de hueco,
        // centradas en su hueco horizontal y de TopRowY a TopRowY + 38 como mucho, WalletPanelClearDp por encima del canto [P].
        // Lo decide este gancho de GameScreen.LayoutWalletBar y lo dibuja WalletBar (solo con Canoas, desde el 31: Semillas sola
        // va centrada en la fila y no llega al canto; el saldo grande del embudo y de la victoria va a escala 1). Como
        // LayoutWalletBar lo pregunta cada vez que coloca la cabecera (también SyncChrome al cambiar de fase, desde cualquier
        // evento), nada la deja a medias hasta el siguiente fotograma. Sin asignaciones.
        partial void LabelsWalletColumn(ref float pillDp, ref float gapDp, ref float columnTop)
        {
            BoardLayoutResult? l = _layout;
            if (!_labelsBuilt || l == null || _views.Length == 0)
            {
                return;
            }

            float edge = PanelEdgeDp(l);
            float bottom = columnTop + (2f * pillDp) + gapDp; // Canoas a su tamaño: hasta TopRowY + 49
            if (bottom + WalletPanelClearDp <= edge && !_hudCounterBesideWallet)
            {
                return;
            }

            const float column = (2f * WalletSqueezedPillDp) + WalletSqueezedGapDp;
            float columnBottom = Mathf.Min(TopRowY + ((HudRowDp + column) * 0.5f), edge - WalletPanelClearDp);
            if (_hudCounterBesideWallet)
            {
                columnBottom = Mathf.Min(columnBottom, TopRowY + column); // el contador va debajo (PlaceCounterBesideWallet)
            }

            pillDp = WalletSqueezedPillDp;
            gapDp = WalletSqueezedGapDp;
            columnTop = Mathf.Max(TopRowY, columnBottom - column);
        }

        // Vuelve a colocar la cabecera de partida con la columna de LabelsWalletColumn: tras rehacer el marco (el canto) o la
        // banda (el contador junto a la cabecera). El saldo grande del embudo y de la victoria no depende de ellos.
        private void KeepWalletOffPanel()
        {
            if (_walletBar.Compact)
            {
                LayoutWalletBar(false);
            }
        }

        // Banda de las etiquetas (ver la clase): forma, altura y posición del grupo (chip y «?») y del contador, aparte. Sin
        // asignaciones (también en cada fotograma del relayout de una cuia añadida).
        private void LayoutHudLabels()
        {
            bool chip = _hudChip.Visible;
            bool murky = _hudMurky.Visible;
            bool counter = _hudCounter.Visible;
            if (!chip && !murky && !counter)
            {
                _hudMode = HudLabelMode.None; // la sonda no arrastra la forma del nivel anterior
                if (_hudCounterBesideWallet)
                {
                    _hudCounterBesideWallet = false;
                    KeepWalletOffPanel();
                }

                return;
            }

            float top = TopRowY;
            float edge = float.MaxValue;
            float liftedTop = float.MaxValue;
            float skyTop = float.MaxValue;
            float skyLeft = 0f;
            float skyRight = 0f;
            BoardLayoutResult? l = _layout;
            if (l != null && _views.Length > 0)
            {
                edge = PanelEdgeDp(l);
                float u = l.CuiaWidth * 0.5f;
                int n = Mathf.Min(_views.Length, l.RowOf.Length);
                for (int i = 0; i < n; i++)
                {
                    if (l.RowOf[i] != 0)
                    {
                        continue;
                    }

                    float mouth = l.FootY[i] - (_views[i].HeightU * u);
                    liftedTop = Mathf.Min(liftedTop, mouth - CuiaView.LiftDp);
                    if (i == _hudCounterIndex)
                    {
                        // Cielo de la cuia del contador con la cuia elevada: los 32 dp táctiles sobre la boca (su nube y sus
                        // puntos, o la cueva, 02 §3.3) más los 16 dp que sube al seleccionarla, del ancho de la cuia o, en la
                        // Piraña, del de su cueva, que sobresale 0,2 u por lado (revisión de T09 y T17).
                        float half = (SkyHalfWidthU(_hudCounter.Type) * u) + HudLabelsClearDp;
                        skyTop = mouth - CuiaView.LiftDp - CuiaView.TouchHeadroomDp;
                        skyLeft = l.CenterX[i] - half;
                        skyRight = l.CenterX[i] + half;
                    }
                }
            }

            HudLabelMode mode = ModeFor(top, edge, liftedTop);
            _hudMode = mode;
            bool compact = mode != HudLabelMode.Full;
            bool tucked = mode == HudLabelMode.Tucked;
            float bandH = compact ? DifficultyChip.CompactHeightDp : DifficultyChip.HeightDp;
            float y = BandTopFor(mode, top, edge, liftedTop);
            _hudChip.SetCompact(compact);
            _hudChip.SetSeal(y + bandH <= edge); // sobre el panel que rodea las cuias, sin sello (ver la clase)
            _hudCounter.SetCompact(compact);
            float murkyH = compact ? MurkyChip.CompactHeightDp : MurkyChip.HeightDp;
            float murkyW = MurkyChip.WidthFor(murkyH);
            float centerY = y + (bandH * 0.5f);
            bool blocked = skyTop < y + bandH;
            float gap = HudLabelsGapDp;

            float groupW = 0f;
            if (chip)
            {
                groupW += _hudChip.Width;
            }

            if (murky)
            {
                groupW += (groupW > 0f ? gap : 0f) + murkyW;
            }

            float counterW = counter ? _hudCounter.Width : 0f;
            float counterY = Mathf.Round(centerY - (_hudCounter.Height * 0.5f));
            float center = (HudBadgeLeft + HudBadgeRight) * 0.5f;
            float groupX;
            float counterX;
            bool beside = false;
            if (tucked)
            {
                if (!PlaceTucked(groupW, counterW, center, blocked, skyLeft, skyRight, out groupX, out counterX) && counter && groupW > 0f)
                {
                    beside = PlaceCounterBesideWallet(groupW, counterW, center, edge, liftedTop, skyTop, skyLeft, skyRight, ref groupX, ref counterX, ref counterY);
                }
            }
            else
            {
                // El contador a la derecha de la banda (x ≈ W − 8 − ancho) fuera del cielo; el grupo, centrado bajo la insignia y
                // a ≥ HudCounterGapDp del contador.
                float right = _canvasW - HudCounterRightDp;
                counterX = right - counterW;
                if (counter && FitSpan(counterW, counterX, 8f, right, blocked, skyLeft, skyRight, out float cx))
                {
                    counterX = cx;
                }

                float hi = counter ? counterX - HudCounterGapDp : _canvasW - 8f;
                float desired = center - (groupW * 0.5f);
                groupX = FitSpan(groupW, desired, 8f, hi, blocked, skyLeft, skyRight, out float gx)
                    ? gx
                    : Mathf.Clamp(desired, 8f, Mathf.Max(8f, hi - groupW));
            }

            float x = Mathf.Round(groupX);
            float cx0 = Mathf.Round(counterX);
            if (counter && groupW > 0f && cx0 >= x && cx0 - (x + groupW) < HudCounterGapDp)
            {
                cx0 = Mathf.Ceil(x + groupW + HudCounterGapDp); // el redondeo no se come la separación
            }

            if (chip)
            {
                _hudChip.Place(x, y);
                x += _hudChip.Width + gap;
            }

            if (murky)
            {
                _hudMurky.Place(x, Mathf.Round(centerY - (murkyH * 0.5f)), murkyH);
            }

            if (counter)
            {
                _hudCounter.Place(cx0, counterY);
            }

            if (beside != _hudCounterBesideWallet)
            {
                _hudCounterBesideWallet = beside;
                KeepWalletOffPanel(); // la columna de la cabecera se compacta (o vuelve) para dejarle sitio debajo
            }
        }

        // Pegado bajo la placa, el grupo y el contador no caben juntos (el 160 en 16:9: «Muy difícil» y la Lluvia con sus dos
        // gotitas, y ni la banda normal ni la compacta caben sobre el marco): el grupo, solo bajo la placa, y el contador a la
        // derecha de la insignia, bajo la columna de la cabecera compactada (TopRowY + 38 + 2) y a la izquierda de Reiniciar,
        // por encima del canto del tablero y de las cuias elevadas y a ≥ HudCounterGapDp del grupo [P]. False si tampoco cabe.
        private bool PlaceCounterBesideWallet(float groupW, float counterW, float center, float edge, float liftedTop, float skyTop, float skyLeft, float skyRight, ref float groupX, ref float counterX, ref float counterY)
        {
            float y = TopRowY + (2f * WalletSqueezedPillDp) + WalletSqueezedGapDp + HudLabelsClearDp;
            float h = _hudCounter.Height;
            if (y + h > Mathf.Min(edge - HudLabelsClearDp, liftedTop - HudLabelsTuckedClearDp))
            {
                return false;
            }

            float bandBottom = TopRowY + HudLabelsTuckedDp + DifficultyChip.CompactHeightDp;
            if (!FitSpan(groupW, center - (groupW * 0.5f), HudBadgeLeft, HudBadgeRight, skyTop < bandBottom, skyLeft, skyRight, out float gx))
            {
                return false;
            }

            float lo = Mathf.Max(HudBadgeRight + 4f, Mathf.Round(gx) + groupW + HudCounterGapDp);
            float hi = RestartLimitDp; // a la izquierda de Reiniciar (x = W − 68) y de su píldora si crece (tarea FX5)
            if (!FitSpan(counterW, hi - counterW, lo, hi, skyTop < y + h, skyLeft, skyRight, out float cx))
            {
                return false;
            }

            groupX = gx;
            counterX = cx;
            counterY = y;
            return true;
        }

        // Pegado bajo la placa (x 56-192): el grupo y, a ≥ HudCounterGapDp, el contador, fuera del cielo de la cuia del
        // contador. Primero todo junto y lo más centrado posible bajo la insignia; si no cabe a un lado del cielo, el grupo a un
        // lado y el contador al otro (el cielo, del ancho de una cuia, ≥ 28 dp, ya los separa).
        private static bool PlaceTucked(float groupW, float counterW, float center, bool blocked, float skyLeft, float skyRight, out float groupX, out float counterX)
        {
            const float lo = HudBadgeLeft;
            const float hi = HudBadgeRight;
            float sep = groupW > 0f && counterW > 0f ? HudCounterGapDp : 0f;
            float total = groupW + sep + counterW;
            if (FitSpan(total, center - (total * 0.5f), lo, hi, blocked, skyLeft, skyRight, out float x))
            {
                groupX = x;
                counterX = x + groupW + sep;
                return true;
            }

            if (blocked && groupW > 0f && counterW > 0f)
            {
                float leftHi = Mathf.Min(hi, skyLeft);
                float rightLo = Mathf.Max(lo, skyRight);
                if (groupW <= leftHi - lo && counterW <= hi - rightLo)
                {
                    groupX = Mathf.Clamp(center - (groupW * 0.5f), lo, leftHi - groupW);
                    counterX = Mathf.Clamp(center - (counterW * 0.5f), rightLo, hi - counterW);
                    return true;
                }

                if (counterW <= leftHi - lo && groupW <= hi - rightLo)
                {
                    counterX = Mathf.Clamp(center - (counterW * 0.5f), lo, leftHi - counterW);
                    groupX = Mathf.Clamp(center - (groupW * 0.5f), rightLo, hi - groupW);
                    return true;
                }
            }

            // No cabe en ningún reparto bajo la placa: centrado (quien llama prueba a sacar el contador junto a la cabecera; si
            // tampoco, se queda así, y el barrido de HudLabelTests lo detecta en el catálogo).
            groupX = Mathf.Max(lo, center - (total * 0.5f));
            counterX = groupX + groupW + sep;
            return false;
        }

        // AF_STATE {"event":"hud_label",…} con la etiqueta, el «?», el marco, el velo, la forma de la banda, el sello, el
        // contador y la cabecera.
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
                + ",\"veil\":" + (_nightVeil.gameObject.activeSelf ? NightVeilAlpha.ToString("0.00", CultureInfo.InvariantCulture) : "0")
                + ",\"light\":\"" + (StopBackdrop.DifficultyLightName(DifficultyLightFor(_hudLabel)) ?? "none") + "\""
                + ",\"mode\":\"" + ModeKey(_hudMode) + "\""
                + ",\"seal\":" + (_hudChip.SealShown ? "true" : "false")
                + ",\"counter\":\"" + counter + "\""
                + ",\"remaining\":" + (_hudCounter.Visible ? _hudCounter.Remaining : 0).ToString(CultureInfo.InvariantCulture)
                + ",\"wallet\":\"" + (_walletBar.Squeezed ? "squeezed" : "row") + "\"}";
            _probe.Event("hud_label", json);
        }

        internal static string ModeKey(HudLabelMode mode)
        {
            switch (mode)
            {
                case HudLabelMode.Full: return "full";
                case HudLabelMode.CompactBelow: return "compact";
                case HudLabelMode.Tucked: return "tucked";
                default: return "none";
            }
        }
    }
}
