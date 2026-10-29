#nullable enable
using System;
using AF.Ads;
using AF.Boosters;
using AF.GameFlow;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Las ranuras de la bandeja en el orden canónico de 07 §7.4, de izquierda a derecha (02 §5 y 04 §2 lo copian; pregunta 29,
    /// D-020: Revelar va detrás de Morpho y de Pororoca). El valor es la posición relativa: una ranura que no existe no deja
    /// hueco (<see cref="BoosterTray.PositionOf"/>).
    /// </summary>
    internal enum TraySlot
    {
        /// <summary>[Deshacer · Marea].</summary>
        Undo = 0,

        /// <summary>[+1 cuia · Cuia extra].</summary>
        ExtraCuia = 1,

        /// <summary>[Pista · Morpho] (MO2).</summary>
        Hint = 2,

        /// <summary>[Barajar · Pororoca] desde el nivel 30 (bloque B2; 04 §2.4).</summary>
        Shuffle = 3,

        /// <summary>[Revelar · Luciérnaga]: solo en niveles con ocultas, siempre la última.</summary>
        Reveal = 4,
    }

    /// <summary>
    /// Bandeja cumbuca de comodines (tarea B2-1; GDD 07 §7.4, 02 §5, 04 §1.1, §2.1, §2.2 y §2.5) con el orden canónico y los
    /// equivalentes de Magic Sort (ficha §2.3 y §5.2: deshacer, añadir botella; barajar llega con la Pororoca en I6):
    /// <list type="bullet">
    /// <item><description>[Deshacer · Marea]: sin contador en 1-10; desde el 11, badge blanco con el cupo gratis y «+N» ámbar con
    /// el stock; sin cupo ni stock, «+3» con el icono de video (<c>rv_undo_refill</c>) o la etiqueta de precio (30), y tocarlo
    /// recarga; gris sin historial;</description></item>
    /// <item><description>[+1 cuia · Cuia extra] desde el nivel 8: «+N» con stock o el precio (150), en rojo y con «Te faltan
    /// {n}» encima sin saldo; gris si ya se usó en el nivel, si no queda vertido útil o con 16 cuias;</description></item>
    /// <item><description>[Pista · Morpho] (tarea MO2; 04 §2.3) desde <c>hint_unlock_level</c> (12), tras Cuia extra: «+N» con
    /// stock; sin stock, el precio (100, <c>hint_seed_price</c>); sin saldo y con <c>rv_hint</c>, «+1» con el icono de video; sin
    /// nada, el precio en rojo con «Te faltan {n}», que late al tocarla (<see cref="PulseHintMissing"/>). Pulsada mientras
    /// Morpho piensa; gris sin vertido legal, fuera de PLAYING o en el enfriamiento; gris en un nivel sin
    /// <c>hint_supported</c>, y entonces tocarla enseña el globo «La morpho no conoce este tramo todavía»
    /// (<see cref="ShowHintTooltip"/>; 04 §2.3 b). La acción la pone la pantalla (<c>GameScreen.Morpho.cs</c>);</description></item>
    /// <item><description>[Revelar · Luciérnaga] (tarea T14) solo en los niveles de Aguas turbias mientras quede alguna dosis
    /// oculta, siempre la última a la derecha, detrás de Pista (pregunta 29; D-020): «+N» con stock; sin stock y con <c>rv_reveal</c>, «+1» con el icono de
    /// video aunque haya saldo (D-028: video primero, como Marea); sin video, el precio (120, <c>reveal_seed_price</c>) y, si
    /// falta saldo, en rojo con «Te faltan {n}» (<see cref="SetRevealBadge"/>). La visibilidad, la insignia y la acción las pone la pantalla de nivel
    /// (<c>GameScreen.Reveal.cs</c>).</description></item>
    /// </list>
    /// Antes del desbloqueo la ranura no existe y la bandeja se centra (07 §7.4): 16 + n·64 + (n − 1)·8 + 16 dp (240 con las
    /// tres ranuras de un nivel turbio). Las etiquetas de las ranuras van a 12 sp y nunca menos (02 §12; tarea L4): si la más
    /// larga no cabe en 64 dp (otro idioma, en-XA o la fuente al 130 %), todas las ranuras se ensanchan por igual hasta que
    /// quepa, sin pasar de <see cref="SlotMaxDp"/> ni salir de la pantalla; si ni así cabe, esa etiqueta va en dos líneas y su
    /// glifo se encoge para dejarle sitio (<c>Slot.Place</c>). El orden es el canónico de 07 §7.4 (<see cref="TraySlot"/>:
    /// Marea, Cuia extra, Pista, [Pororoca] y Revelar), sin huecos y con la bandeja centrada: con 3 y 4 ranuras miden 64 dp y con
    /// las 5, 56 dp (<see cref="BaseSlotDpFor"/>). Pororoca (30) aún no está en el cliente (va en la tanda siguiente): hoy hay
    /// como mucho 4 ranuras (Marea, Cuia extra, Pista y Revelar en un nivel turbio), y su hueco ya está en el orden
    /// (<see cref="PositionOf"/>). Se refresca cada fotograma solo si algo cambió, sin asignar memoria (08 §5.4): los videos de
    /// Marea y de Pista se preguntan a los anuncios como mucho cada <see cref="UndoVideoPollMs"/> (<see cref="Tick"/>).
    /// </summary>
    internal sealed class BoosterTray
    {
        /// <summary>Lo que da un video de <c>rv_reveal</c> (04 §2.5: «1 revelado»): la cifra de la insignia con el icono de video.</summary>
        public const int RevealVideoUnits = 1;

        /// <summary>
        /// Cada cuánto se vuelve a preguntar a los anuncios si hay video de <c>rv_undo_refill</c> mientras Marea no tiene cupo
        /// [P], el mismo plazo que Revelar (<see cref="GameScreen.RevealVideoPollMs"/>, T14): preguntar lee los contadores del
        /// día con la clave de la fecha local, que crea una cadena cada vez, así que no se hace en cada fotograma (08 §5.4); el
        /// tope diario y la espera entre videos no cambian más deprisa. Tocar Marea vuelve a preguntar en el acto.
        /// </summary>
        public const float UndoVideoPollMs = GameScreen.RevealVideoPollMs;

        /// <summary>
        /// Respuesta de «Te faltan {n}» de Revelar al toque sin stock, sin saldo y sin video (pregunta 48 (b), D-022; tarea A1)
        /// [P]: cada mitad de su único latido (crece y vuelve), en ms. La primera prueba (T52, 2 × 180 ms) duraba un tercio de
        /// segundo y en el teléfono se perdía: este latido es algo más lento y el color sigue después.
        /// </summary>
        public const float MissingPulseBeatMs = 220f;

        /// <summary>
        /// Lo que crece «Te faltan {n}» en lo alto del latido, desde el centro de su pie (crece hacia arriba y a los lados, nunca
        /// hacia el cordón de la bandeja) [P]. Hacia arriba crece siempre esto (<see cref="RevealMissingPulseMaxScale"/>); a los
        /// lados, con una burbuja vecina (las dos estrechas, 70 dp con 2 dp de aire), solo lo que deja
        /// <see cref="MissingPulseClearDp"/> hasta ella (<see cref="RevealMissingPulseMaxScaleX"/>: con 2 dp, nada, así que
        /// late solo hacia arriba; revisión de A1: con 0,5 dp, en lo alto del latido las dos burbujas se tocaban y se leían como un
        /// solo bloque bicolor).
        /// </summary>
        public const float MissingPulseScale = 1.08f;

        /// <summary>Aire que el latido deja siempre hasta la burbuja «Te faltan {n}» vecina (dp) [P].</summary>
        public const float MissingPulseClearDp = 2f;

        /// <summary>Los colores de la etiqueta de precio sin saldo (texto crema sobre el rojo) entran en estos ms [P].</summary>
        public const float MissingColorInMs = 100f;

        /// <summary>Se mantienen hasta estos ms desde el toque (≈ 1,1 s en total con la salida) [P].</summary>
        public const float MissingColorHoldMs = 900f;

        /// <summary>Con Reducir movimiento (sin latido: solo el color), se mantienen hasta estos ms [P].</summary>
        public const float MissingColorHoldReducedMs = 1000f;

        /// <summary>Y vuelven a los de la burbuja en estos ms [P].</summary>
        public const float MissingColorOutMs = 200f;

        /// <summary>
        /// Ancho máximo de una ranura que se ensancha para que quepa su etiqueta (dp) [P] (tarea L4; 02 §12: los contenedores
        /// crecen): con 3 ranuras, la bandeja mide como mucho 16 + 3 · 104 + 2 · 8 + 16 = 360 dp, y además nunca sale de la
        /// pantalla (<see cref="TrayEdgeDp"/> a cada lado).
        /// </summary>
        public const float SlotMaxDp = 104f;

        /// <summary>Aire mínimo entre la bandeja ensanchada y el borde de la pantalla (dp) [P].</summary>
        public const float TrayEdgeDp = 8f;

        /// <summary>
        /// Lo más que se encoge el glifo de una ranura para dejar sitio a su etiqueta en dos líneas (dp) [P] (tarea L4; guía 3.6:
        /// 32 dp).
        /// </summary>
        public const float LabelMinIconDp = 20f;

        /// <summary>
        /// Aire a cada lado de la etiqueta de una ranura ensanchada (dp) [P] (revisión de L4: con 4, al 130 % en en-XA la etiqueta
        /// llegaba casi al contorno redondeado de la ranura): en cuanto la etiqueta no cabe en la ranura de 64 dp de la guía 3.6 (con
        /// <see cref="LabelNarrowInsetDp"/> por lado, como siempre: en es-419 al 100 % nada cambia), la ranura se ensancha hasta su
        /// línea más 2 × este aire y, si no basta, la etiqueta pasa a dos líneas en la caja de la ranura menos 2 × este aire.
        /// </summary>
        public const float LabelSideInsetDp = 6f;

        /// <summary>Aire a cada lado de la etiqueta en la ranura de 64 dp de la guía 3.6 (dp) [P] (tarea L4).</summary>
        public const float LabelNarrowInsetDp = 4f;

        /// <summary>
        /// Lo más que crecen las ranuras y la bandeja hacia abajo para que quepa una etiqueta en dos líneas sobre el glifo más
        /// pequeño (dp) [P] (tarea L4; revisión de L4: al 130 % las dos líneas no caben bajo un glifo de 20 dp en la cara de 64):
        /// la bandeja acaba a 16 dp de la zona segura inferior (02 §5), así que siempre le quedan 4.
        /// </summary>
        public const float MaxGrowDp = 12f;

        /// <summary>Ancho de las ranuras con 1-4 ranuras (dp; 07 §7.4 y guía 3.6).</summary>
        public const float SlotDp = 64f;

        /// <summary>Ancho de las ranuras con las 5 (dp; 07 §7.4: «con los 5 pasan a 56 dp»).</summary>
        public const float FiveSlotsDp = 56f;

        /// <summary>
        /// Lo más ancho del globo «La morpho no conoce este tramo todavía» sobre la ranura Pista (dp) [P]: pasa a dos líneas
        /// antes que salir de ahí, y nunca de la pantalla.
        /// </summary>
        public const float HintTooltipMaxDp = 220f;

        /// <summary>Tamaño del texto del globo de Pista (sp) [P]: ≥ 12 sp (02 §12).</summary>
        public const float HintTooltipSp = 13f;

        /// <summary>
        /// Lo más alto que empieza el glifo de Pista en su ranura (dp desde el canto de arriba) [P] (revisión de MO2): la tinta de
        /// la mariposa (antenas y alas) llega a 1 dp del borde de arriba de su lienzo, y la de los otros glifos deja unos 4. Con
        /// la etiqueta al 130 % los glifos suben hasta 2 dp del canto y la mariposa lo tocaba; así se encoge desde arriba (a unos
        /// 29 dp en la bandeja apretada, sin bajar de <see cref="LabelMinIconDp"/>) y deja ≥ 4 dp de aire, como +1 cuia. Al 100 %
        /// baja 0,5 dp.
        /// </summary>
        public const float HintGlyphMinTopDp = 6.5f;

        /// <summary>
        /// Separación entre ranuras de la bandeja apretada (dp) [P] (tarea MO2, con la 4.ª ranura; L4): cuando la palabra más
        /// larga de una etiqueta no cabe entera en la ranura más ancha que deja la pantalla con la separación de siempre (8 dp),
        /// la bandeja aprieta sus separaciones antes que partir una palabra (02 §12: nada se recorta; D-020, pregunta 36 (a): la
        /// letra no baja de 12 sp × escala). Pasa en es-419 con la fuente al 130 % a 360 dp de ancho: «Deshacer» mide 68,7 dp y
        /// con 4 ranuras quedaban 72 dp por ranura. Es la mitad de los 8 dp entre objetivos táctiles de la guía 3.6: pendiente
        /// del dueño (revisión de MO2), con la opción recomendada aplicada.
        /// </summary>
        public const float CompactGapDp = 4f;

        /// <summary>Margen de la bandeja apretada a cada lado de las ranuras (dp) [P] (<see cref="CompactGapDp"/>).</summary>
        public const float CompactPadDp = 8f;

        /// <summary>
        /// Aire a cada lado de la etiqueta cuando la palabra más larga no cabe ni en la bandeja apretada con el aire de la ranura
        /// de 64 dp (<see cref="LabelNarrowInsetDp"/>) (dp) [P] (revisión de MO2): en en-XA con la fuente al 130 % a 360 dp,
        /// «[Déshácér» mide 74,6 dp y la ranura apretada, 79 (71 por dentro con 4 dp por lado; 75 con este). Antes de partir una
        /// palabra (02 §12) se estrecha el aire, no la letra (D-020, pregunta 36 (a)). Pendiente del dueño junto con
        /// <see cref="CompactGapDp"/>.
        /// </summary>
        public const float LabelTightInsetDp = 2f;

        private const float GapDp = 8f;
        private const float PadDp = 16f;
        private const float TooltipPadXDp = 10f;
        private const float TooltipPadYDp = 5f;
        private const float TooltipGapDp = 6f;

        // «Te faltan {n}» de dos ranuras vecinas a la vez: cada aviso cabe en su paso de ranura (64 + 8, o el ancho de la ranura
        // ensanchada + 8) con 2 dp de aire entre los dos. A 12 sp, «Te faltan» y una cifra de 2 caben en una línea («Te faltan
        // 70» mide 67,8 dp); con 3 (el máximo con los precios de 08 §9; 75 dp) la cifra baja a una segunda línea
        // (Slot.PlaceMissing).
        private const float MissingNeighborGapDp = 2f;

        private readonly ProceduralArt _art;
        private readonly Image _tray;
        private readonly Slot _undo;
        private readonly Slot _extra;
        private readonly Slot _hint;
        private readonly Slot _shuffle;
        private readonly Slot _reveal;
        private readonly Slot[] _order;
        private readonly Slot[] _activeBuf = new Slot[5];
        private readonly MissingRoom _room = new MissingRoom();
        private readonly RectTransform _tip;
        private readonly TextMeshProUGUI _tipText;
        private readonly char[] _buf = new char[24];

        private LevelFlow? _flow;
        private GameServices? _services;
        private Action? _onReveal;
        private Action? _onHint;
        private Action? _onShuffle;
        private Slot? _tipSlot;
        private bool _tipInBand;
        private int _key = int.MinValue;
        private int _revealKey = int.MinValue;
        private int _refreshRepaints;
        private float _canvasW = 360f;
        private float _trayTop;
        private int _slots = 1;
        private float _slotW = SlotDp;
        private float _gap = GapDp;
        private float _pad = PadDp;
        private float _wideInset = LabelSideInsetDp;
        private float _grow;
        private float _trayX;
        private int _slotsVersion;
        private bool _visible = true;
        private bool _revealPressed;
        private bool _undoVideoReady;
        private float _undoVideoPollMs;
        private bool _hintVideoReady;
        private float _hintVideoPollMs;
        private bool _hintPressed;
        private float _tipMs;

        public BoosterTray(Transform hud, ProceduralArt art, Action onUndo, Action onExtraCuia)
        {
            _art = art;
            _tray = UiBuilder.Image("Tray", hud, art.Tray(96f), Color.white);
            _undo = new Slot("UndoButton", hud, art, art.IconUndo, art.IconUndoDisabled, UiText.Undo, onUndo);
            _extra = new Slot("ExtraCuiaButton", hud, art, art.IconCuiaPlus, art.IconCuiaPlusDisabled, UiText.ExtraCuia, onExtraCuia);
            _extra.SetActive(false);
            _hint = new Slot("HintButton", hud, art, art.IconMorpho, art.IconMorphoDisabled, UiText.HintLabel, () => _onHint?.Invoke());
            _hint.GlyphMinTopDp = HintGlyphMinTopDp;
            _hint.SetActive(false);
            _shuffle = new Slot("ShuffleButton", hud, art, art.IconWave, art.IconWaveDisabled, ShuffleText.Label, () => _onShuffle?.Invoke());
            _shuffle.SetActive(false);
            _reveal = new Slot("RevealButton", hud, art, art.IconFirefly, art.IconFireflyDisabled, UiText.RevealLabel, () => _onReveal?.Invoke());
            _reveal.SetActive(false);
            _order = new[] { _undo, _extra, _hint, _shuffle, _reveal }; // 07 §7.4: Marea, Cuia extra, Pista, Pororoca (B2) y Revelar
            for (int i = 0; i < _order.Length; i++)
            {
                _order[i].Room = _room;
            }

            // Globo de Pista sin hint_supported (04 §2.3 b): placa clara con texto de tinta, sobre la ranura; no captura toques.
            _tip = UiBuilder.Rect("HintTooltip", hud);
            Image tipPlate = UiBuilder.Sliced("Plate", _tip, art.RoundedRect, Palette.PanelLight);
            tipPlate.pixelsPerUnitMultiplier = 24f / 9f;
            UiBuilder.Stretch(tipPlate.rectTransform);
            _tipText = UiBuilder.Label("Value", _tip, TextStyle.PlainBold, string.Empty, HintTooltipSp, Palette.Ink, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_tipText.rectTransform);
            _tipText.rectTransform.offsetMin = new Vector2(TooltipPadXDp, TooltipPadYDp);
            _tipText.rectTransform.offsetMax = new Vector2(-TooltipPadXDp, -TooltipPadYDp);
            _tip.gameObject.SetActive(false);
        }

        /// <summary>
        /// Ancho de cada ranura (dp) con <paramref name="slots"/> ranuras a la vista (07 §7.4): 64 hasta 4 y 56 con las 5. Antes de
        /// ensanchar por una etiqueta que no cabe (tarea L4).
        /// </summary>
        public static float BaseSlotDpFor(int slots)
        {
            return slots >= 5 ? FiveSlotsDp : SlotDp;
        }

        /// <summary>
        /// Posición (0 = la de la izquierda) de <paramref name="slot"/> entre las ranuras de <paramref name="activeMask"/> (bit
        /// <c>(int)TraySlot</c> = la ranura existe), en el orden canónico de 07 §7.4 y sin huecos; −1 si no existe. Sin
        /// asignaciones.
        /// </summary>
        public static int PositionOf(TraySlot slot, int activeMask)
        {
            int bit = 1 << (int)slot;
            if ((activeMask & bit) == 0)
            {
                return -1;
            }

            int position = 0;
            for (int below = activeMask & (bit - 1); below != 0; below &= below - 1)
            {
                position++;
            }

            return position;
        }

        /// <summary>
        /// Ancho de la bandeja (dp) con <paramref name="slots"/> ranuras de <see cref="BaseSlotDpFor"/>: 16 + n · ancho + (n − 1) · 8
        /// + 16 (07 §7.4 y guía 3.6; tests).
        /// </summary>
        public static float WidthFor(int slots)
        {
            return PadDp + (slots * BaseSlotDpFor(slots)) + ((slots - 1) * GapDp) + PadDp;
        }

        /// <summary>Ranuras que existen ahora, como máscara de bits de <see cref="TraySlot"/> (tests).</summary>
        public int ActiveMask => (_undo.Active ? 1 << (int)TraySlot.Undo : 0) | (_extra.Active ? 1 << (int)TraySlot.ExtraCuia : 0)
            | (_hint.Active ? 1 << (int)TraySlot.Hint : 0) | (_shuffle.Active ? 1 << (int)TraySlot.Shuffle : 0)
            | (_reveal.Active ? 1 << (int)TraySlot.Reveal : 0);

        /// <summary>Botón de Marea.</summary>
        public Button UndoButton => _undo.Parts.Button;

        /// <summary>Botón de Cuia extra (activo desde el nivel 8).</summary>
        public Button ExtraCuiaButton => _extra.Parts.Button;

        /// <summary>Botón de Revelar · Luciérnaga (solo en los niveles de Aguas turbias con ocultas).</summary>
        public Button RevealButton => _reveal.Parts.Button;

        /// <summary>Botón de Pista · Morpho (desde <c>hint_unlock_level</c>; MO2).</summary>
        public Button HintButton => _hint.Parts.Button;

        /// <summary>La ranura de Pista existe en el nivel.</summary>
        public bool HintShown => _hint.Active;

        /// <summary>Etiqueta de la ranura de Pista (tests de desbordes).</summary>
        public TextMeshProUGUI HintLabel => _hint.Label;

        /// <summary>Glifo de la ranura de Pista (tests y capturas: la mariposa).</summary>
        public Image HintGlyph => _hint.Glyph;

        /// <summary>Texto de la insignia de Pista («+N» del stock o «+1» del video), o vacío.</summary>
        public string HintBadgeText => _hint.BadgeText;

        /// <summary>Precio que muestra Pista (0 si no lo muestra).</summary>
        public int HintPriceShown => _hint.PriceShown;

        /// <summary>Pista muestra «Te faltan {n}».</summary>
        public bool HintShowsMissing => _hint.MissingShown;

        /// <summary>Pista ofrece el video (<c>rv_hint</c>): la insignia lleva el icono de video.</summary>
        public bool HintShowsVideo => _hint.VideoShown;

        /// <summary>Texto del aviso «Te faltan {n}» de Pista (vacío si no se ve).</summary>
        public string HintMissingText => _hint.MissingText;

        /// <summary>Aviso «Te faltan {n}» de Pista (tests de solapes).</summary>
        public RectTransform HintMissingRect => _hint.MissingRect;

        /// <summary>Texto del aviso «Te faltan {n}» de Pista (tests de tamaño y desbordes: 12 sp × escala).</summary>
        public TextMeshProUGUI HintMissingLabel => _hint.MissingLabel;

        /// <summary>Etiqueta de precio de Pista (tests de solapes).</summary>
        public RectTransform HintPriceRect => _hint.PriceRect;

        /// <summary>«Te faltan {n}» de Pista responde al toque (<see cref="PulseHintMissing"/>; tests).</summary>
        public bool HintMissingPulsing => _hint.PulsingNow;

        /// <summary>«Te faltan {n}» de la Cuia extra responde al toque (<see cref="PulseExtraMissing"/>; tests).</summary>
        public bool ExtraMissingPulsing => _extra.PulsingNow;

        /// <summary>Escala de ahora de la burbuja «Te faltan {n}» de la Cuia extra (1 en reposo; tests).</summary>
        public float ExtraMissingScaleNow => _extra.PulseScaleNow;

        /// <summary>Color de ahora de esa burbuja hacia el de la etiqueta de precio sin saldo (0 en reposo, 1 del todo; tests).</summary>
        public float ExtraMissingColorNow => _extra.PulseColorNow;

        /// <summary>
        /// Late la burbuja «Te faltan {n}» de la Cuia extra, como la de Pista (observación 2 de la jornada 6: «+1 cuia» sin saldo
        /// era un toque mudo); con Reducir movimiento, solo el color. False si la burbuja no se ve o lleva otra cifra. Sin
        /// asignaciones.
        /// </summary>
        public bool PulseExtraMissing(int missing, bool reduceMotion)
        {
            return _extra.PulseMissing(missing, reduceMotion);
        }

        /// <summary>La cara de Pista está pulsada: Morpho piensa.</summary>
        public bool HintPressed => _hintPressed;

        /// <summary>Pista tiene el aspecto de un botón que se puede usar (ni gris ni sin <c>hint_supported</c>; tests).</summary>
        public bool HintLooksEnabled => _hint.Parts.Relief.Interactable;

        /// <summary>El globo «La morpho no conoce este tramo todavía» se ve (<see cref="ShowHintTooltip"/>).</summary>
        public bool HintTooltipVisible => _tip.gameObject.activeSelf;

        /// <summary>Texto del globo de Pista (vacío si no se ve).</summary>
        public string HintTooltipText => _tip.gameObject.activeSelf ? _tipText.text : string.Empty;

        /// <summary>Globo de Pista (tests de solapes).</summary>
        public RectTransform HintTooltipRect => _tip;

        /// <summary>El globo de Pista va a la franja de la tropa, sobre el panel (D-046; Q-T6T-3; tests).</summary>
        public bool HintTooltipInBand => _tipInBand && _tip.gameObject.activeSelf;

        /// <summary>Texto del globo de Pista (tests de tamaño: ≥ 12 sp).</summary>
        public TextMeshProUGUI HintTooltipLabel => _tipText;

        /// <summary>Ms que le quedan al globo de Pista (0 si no se ve; tests).</summary>
        public float HintTooltipLeftMs => _tip.gameObject.activeSelf ? _tipMs : 0f;

        /// <summary>La ranura de Cuia extra existe en el nivel.</summary>
        public bool ExtraCuiaShown => _extra.Active;

        /// <summary>La ranura de Revelar existe en el nivel (<see cref="SetRevealVisible"/>).</summary>
        public bool RevealShown => _reveal.Active;

        /// <summary>Botón Barajar · Pororoca (bloque B2).</summary>
        public Button ShuffleButton => _shuffle.Parts.Button;

        /// <summary>La ranura de la Pororoca existe (desde el nivel 30).</summary>
        public bool ShuffleShown => _shuffle.Active;

        /// <summary>Insignias de la Pororoca (el «+N» del stock).</summary>
        public string ShuffleBadgeText => _shuffle.BadgeText;

        /// <summary>Precio que enseña la ranura de la Pororoca (0 sin precio).</summary>
        public int ShufflePriceShown => _shuffle.PriceShown;

        /// <summary>La ranura de la Pororoca enseña «Te faltan {n}».</summary>
        public bool ShuffleShowsMissing => _shuffle.MissingShown;

        /// <summary>Texto de «Te faltan {n}» de la Pororoca (vacío si no se ve).</summary>
        public string ShuffleMissingText => _shuffle.MissingText;

        /// <summary>Placa de «Te faltan {n}» de la Pororoca.</summary>
        public RectTransform ShuffleMissingRect => _shuffle.MissingRect;

        /// <summary>La ranura de la Pororoca se ve habilitada.</summary>
        public bool ShuffleLooksEnabled => _shuffle.Parts.Relief.Interactable;

        /// <summary>Etiqueta de la ranura de la Pororoca.</summary>
        public TextMeshProUGUI ShuffleLabel => _shuffle.Label;

        /// <summary>Ranuras visibles.</summary>
        public int SlotCount => _slots;

        /// <summary>
        /// Sube cada vez que aparece o desaparece una ranura (Cuia extra o Revelar) y la bandeja se vuelve a centrar: las
        /// ranuras que quedan cambian de sitio y AF_LAYOUT tiene que volver a emitirse (<c>GameScreen.Reveal.cs</c>).
        /// </summary>
        public int SlotsVersion => _slotsVersion;

        /// <summary>Ancho de la bandeja (dp).</summary>
        public float Width => _pad + (_slots * _slotW) + ((_slots - 1) * _gap) + _pad;

        /// <summary>
        /// La bandeja va apretada (<see cref="CompactGapDp"/> y <see cref="CompactPadDp"/>): la palabra más larga de una etiqueta
        /// no cabía con la separación de siempre (tests).
        /// </summary>
        public bool Compact => _gap < GapDp;

        /// <summary>Separación de ahora entre ranuras (dp): 8, o <see cref="CompactGapDp"/> apretada (tests).</summary>
        public float GapNowDp => _gap;

        /// <summary>
        /// Aire de ahora a cada lado de la etiqueta de una ranura ensanchada (dp): <see cref="LabelSideInsetDp"/>, o
        /// <see cref="LabelNarrowInsetDp"/>, el de la ranura de 64 dp, si ni apretada cabe la palabra más larga con el de
        /// siempre, o <see cref="LabelTightInsetDp"/> si ni con ese (tests).
        /// </summary>
        public float LabelInsetNowDp => _wideInset;

        /// <summary>Ancho más grande que puede tomar ahora cada ranura en un lienzo de <paramref name="canvasW"/> dp (tests).</summary>
        public float SlotCapFor(float canvasW)
        {
            return CapFor(canvasW, _pad, _gap);
        }

        /// <summary>Ancho de cada ranura (dp): 64, o más si una etiqueta no cabe (tarea L4; <see cref="SlotMaxDp"/>).</summary>
        public float SlotWidth => _slotW;

        /// <summary>
        /// Lo que crecen hacia abajo las ranuras y la bandeja (dp): 0, o hasta <see cref="MaxGrowDp"/> si una etiqueta en dos
        /// líneas no cabe sobre el glifo más pequeño (tarea L4).
        /// </summary>
        public float GrowDp => _grow;

        /// <summary>Etiqueta de la ranura de Marea (tests de desbordes).</summary>
        public TextMeshProUGUI UndoLabel => _undo.Label;

        /// <summary>Etiqueta de la ranura de Cuia extra (tests de desbordes).</summary>
        public TextMeshProUGUI ExtraLabel => _extra.Label;

        /// <summary>Etiqueta de la ranura de Revelar (tests de desbordes).</summary>
        public TextMeshProUGUI RevealLabel => _reveal.Label;

        /// <summary>Glifo de la ranura de Marea (tests de desbordes).</summary>
        public Image UndoGlyph => _undo.Glyph;

        /// <summary>Glifo de la ranura de Cuia extra (tests de desbordes).</summary>
        public Image ExtraGlyph => _extra.Glyph;

        /// <summary>Fondo de la bandeja (tests de desbordes), con el margen de sombra del sprite procedural.</summary>
        public RectTransform TrayRect => _tray.rectTransform;

        /// <summary>
        /// Cuerpo de la bandeja sin su sombra (dp desde la esquina superior izquierda del HUD: x, y, ancho y alto; tests de
        /// desbordes: no sale de la pantalla).
        /// </summary>
        public Rect BodyRect => new Rect(_trayX, _trayTop, Width, 80f + _grow);

        /// <summary>Texto del badge de Marea (diagnóstico y tests).</summary>
        public string UndoBadgeText => _undo.BadgeText;

        /// <summary>Texto del badge de Cuia extra («+N»), o vacío.</summary>
        public string ExtraBadgeText => _extra.BadgeText;

        /// <summary>Precio que muestra la Cuia extra (0 si no lo muestra).</summary>
        public int ExtraPriceShown => _extra.PriceShown;

        /// <summary>La Cuia extra muestra «Te faltan {n}».</summary>
        public bool ExtraShowsMissing => _extra.MissingShown;

        /// <summary>Texto de la insignia de Revelar («+N» del stock o «+1» del video), o vacío.</summary>
        public string RevealBadgeText => _reveal.BadgeText;

        /// <summary>Precio que muestra Revelar (0 si no lo muestra).</summary>
        public int RevealPriceShown => _reveal.PriceShown;

        /// <summary>Revelar muestra «Te faltan {n}».</summary>
        public bool RevealShowsMissing => _reveal.MissingShown;

        /// <summary>Revelar ofrece el video (<c>rv_reveal</c>): la insignia lleva el icono de video.</summary>
        public bool RevealShowsVideo => _reveal.VideoShown;

        /// <summary>Texto del aviso «Te faltan {n}» de Revelar (vacío si no se ve).</summary>
        public string RevealMissingText => _reveal.MissingText;

        /// <summary>«Te faltan {n}» de Revelar responde al toque (<see cref="PulseRevealMissing"/>; tests).</summary>
        public bool RevealMissingPulsing => _reveal.PulsingNow;

        /// <summary>Escala a lo alto de «Te faltan {n}» de Revelar en su latido (1 en reposo; tests y capturas).</summary>
        public float RevealMissingPulseScale => _reveal.PulseScaleNow;

        /// <summary>Escala a lo ancho de «Te faltan {n}» de Revelar en su latido (1 en reposo; tests).</summary>
        public float RevealMissingPulseScaleX => _reveal.PulseScaleXNow;

        /// <summary>Lo más que crece «Te faltan {n}» de Revelar a lo alto en el latido (<see cref="MissingPulseScale"/>; tests).</summary>
        public float RevealMissingPulseMaxScale => _reveal.PulseMaxScale;

        /// <summary>
        /// Lo más que crece ahora «Te faltan {n}» de Revelar a lo ancho en el latido (con su vecina a la vista, lo que deja
        /// <see cref="MissingPulseClearDp"/> hasta ella; tests).
        /// </summary>
        public float RevealMissingPulseMaxScaleX => _reveal.PulseMaxScaleX;

        /// <summary>
        /// Cuánto lleva «Te faltan {n}» de Revelar los colores de la etiqueta de precio sin saldo (0 en reposo: texto rojo sobre
        /// la placa clara; 1: texto crema sobre el rojo; tests y capturas).
        /// </summary>
        public float RevealMissingPulseColor => _reveal.PulseColorNow;

        /// <summary>Texto del aviso «Te faltan {n}» de Cuia extra (vacío si no se ve; tests).</summary>
        public string ExtraMissingText => _extra.MissingText;

        /// <summary>Ancho del aviso «Te faltan {n}» de Revelar (dp; tests de solapes).</summary>
        public float RevealMissingWidth => _reveal.MissingWidth;

        /// <summary>Ancho del aviso «Te faltan {n}» de Cuia extra (dp; tests de solapes).</summary>
        public float ExtraMissingWidth => _extra.MissingWidth;

        /// <summary>Aviso «Te faltan {n}» de Cuia extra (tests de solapes).</summary>
        public RectTransform ExtraMissingRect => _extra.MissingRect;

        /// <summary>Aviso «Te faltan {n}» de Revelar (tests de solapes).</summary>
        public RectTransform RevealMissingRect => _reveal.MissingRect;

        /// <summary>Insignia de arriba a la derecha de Marea (el cupo o «+3»; un disco de 24 dp; tests de solapes).</summary>
        public RectTransform UndoBadgeRect => _undo.BadgeRect;

        /// <summary>Glifo de la ranura de Revelar (tests y capturas: el icono de la luciérnaga).</summary>
        public Image RevealGlyph => _reveal.Glyph;

        /// <summary>La cara de Revelar está pulsada porque se está eligiendo la cuia.</summary>
        public bool RevealPressed => _revealPressed;

        /// <summary>Relieve del botón de Revelar: la cara que baja mientras se elige la cuia (tests).</summary>
        public ReliefButton RevealRelief => _reveal.Parts.Relief;

        /// <summary>Marea ofrece el video (<c>rv_undo_refill</c>): «+3» con el icono de video.</summary>
        public bool UndoShowsVideo => _undo.VideoShown;

        /// <summary>Precio que muestra Marea sin cupo ni video (0 si no lo muestra; tests).</summary>
        public int UndoPriceShown => _undo.PriceShown;

        /// <summary>Texto del aviso «Te faltan {n}» de Marea (vacío si no se ve; tests).</summary>
        public string UndoMissingText => _undo.MissingText;

        /// <summary>
        /// Ms que faltan para volver a preguntar por el video de Marea (0 con cupo, o si la próxima <see cref="Refresh"/> pregunta;
        /// tests).
        /// </summary>
        public float UndoVideoPollLeftMs => _undoVideoPollMs;

        /// <summary>
        /// Veces que <see cref="Refresh"/> repintó la bandeja porque cambió su clave (tests): oculta en la pantalla de victoria,
        /// no sube mientras la Cosecha cuenta (08 §5.4). Allí no se puede medir con <c>Is.Not.AllocatingGCMemory</c>: en el
        /// Editor, <c>TMP_Text.SetCharArray</c> crea una cadena en cada llamada (solo en el Editor), y el saldo de arriba y la
        /// fila de la Cosecha cuentan con ella.
        /// </summary>
        internal int RefreshRepaints => _refreshRepaints;

        /// <summary>Texto del aviso «Te faltan {n}» de Revelar (tests de tamaño y desbordes).</summary>
        public TextMeshProUGUI RevealMissingLabel => _reveal.MissingLabel;

        /// <summary>Texto del aviso «Te faltan {n}» de Cuia extra (tests de tamaño y desbordes).</summary>
        public TextMeshProUGUI ExtraMissingLabel => _extra.MissingLabel;

        /// <summary>Etiqueta de precio de Revelar (tests de solapes).</summary>
        public RectTransform RevealPriceRect => _reveal.PriceRect;

        /// <summary>Etiqueta de precio de Cuia extra (tests de solapes).</summary>
        public RectTransform ExtraPriceRect => _extra.PriceRect;

        /// <summary>
        /// Borde de arriba (dp desde arriba del lienzo) del aviso «Te faltan {n}» más alto a la vista, de cualquier ranura: lo
        /// que el aviso del tablero no debe tapar (GameScreen.Notices.cs, T52). False si no se ve ninguno. Sin asignaciones.
        /// </summary>
        public bool TryGetMissingTop(out float top)
        {
            // Las que van a la franja de la tropa (SetMissingRoom) no están bajo el tablero: no cuentan aquí (TryGetMissingBandTop).
            top = float.PositiveInfinity;
            _undo.MinMissingTop(ref top);
            _extra.MinMissingTop(ref top);
            _hint.MinMissingTop(ref top);
            _shuffle.MinMissingTop(ref top);
            _reveal.MinMissingTop(ref top);
            if (_tip.gameObject.activeInHierarchy)
            {
                top = Mathf.Min(top, -_tip.anchoredPosition.y); // el globo de Pista tampoco se tapa
            }

            return !float.IsPositiveInfinity(top);
        }

        /// <summary>
        /// Borde de arriba (dp desde arriba del lienzo) de la burbuja «Te faltan {n}» más alta de las que van a la franja de la
        /// tropa, sobre el panel (<see cref="SetMissingRoom"/>), se dibujen o no ahora. False si no hay ninguna. Sin asignaciones.
        /// </summary>
        public bool TryGetMissingBandTop(out float top)
        {
            top = float.PositiveInfinity;
            for (int i = 0; i < _order.Length; i++)
            {
                _order[i].MinMissingBandTop(ref top);
            }

            return !float.IsPositiveInfinity(top);
        }

        /// <summary>
        /// Dónde caben las burbujas «Te faltan {n}» (D-046, punto 3; Q-T6T-3, decidida por el dueño el 2026-10-24: «Bajar y mover
        /// avisos»). <paramref name="floorDp"/>: lo más alto que puede llegar una burbuja sobre su ranura sin pisar el panel del
        /// tablero (NaN: sin límite, como siempre). <paramref name="bandBottomDp"/>: con 3 filas y la franja de la tropa, el borde
        /// de arriba del panel; la burbuja que no cabe bajo el panel va a la franja, sobre su ranura a lo ancho y con su borde de
        /// abajo <see cref="MissingPanelClearDp"/> sobre el panel (NaN: sin franja, como siempre). <paramref name="held"/>: mientras
        /// la tropa está en la franja, las de la franja no se dibujan (no la pisan). <paramref name="hiddenInBand"/>: tampoco
        /// mientras el aviso del tablero no cabe en la franja con ellas (GameScreen.PlaceToast). Solo recoloca si cambia algo; sin
        /// asignaciones.
        /// </summary>
        public void SetMissingRoom(float floorDp, float bandBottomDp, bool held, bool hiddenInBand, float bandTopDp = float.NaN)
        {
            if (SameOrNaN(floorDp, _room.Floor) && SameOrNaN(bandBottomDp, _room.BandBottom) && SameOrNaN(bandTopDp, _room.BandTop)
                && held == _room.Held && hiddenInBand == _room.HiddenInBand)
            {
                return;
            }

            _room.Floor = floorDp;
            _room.BandBottom = bandBottomDp;
            _room.BandTop = bandTopDp;
            _room.Held = held;
            _room.HiddenInBand = hiddenInBand;
            LayoutMissing();
        }

        /// <summary>Aire entre una burbuja «Te faltan {n}» y el panel del tablero, debajo o en la franja (dp) [P].</summary>
        public const float MissingPanelClearDp = 2f;

        /// <summary>Alguna burbuja «Te faltan {n}» de la franja de la tropa se dibuja ahora (tests).</summary>
        public bool AnyMissingInBandDrawn => _undo.MissingInBandDrawn || _extra.MissingInBandDrawn || _hint.MissingInBandDrawn || _shuffle.MissingInBandDrawn || _reveal.MissingInBandDrawn;

        /// <summary>Alguna burbuja «Te faltan {n}» va a la franja de la tropa (tests).</summary>
        public bool AnyMissingInBand => _undo.MissingInBand || _extra.MissingInBand || _hint.MissingInBand || _shuffle.MissingInBand || _reveal.MissingInBand;

        private static bool SameOrNaN(float a, float b)
        {
            return float.IsNaN(a) ? float.IsNaN(b) : Mathf.Approximately(a, b);
        }

        public void Bind(LevelFlow flow, GameServices? services)
        {
            _flow = flow;
            _services = services;
            _key = int.MinValue;
            _revealKey = int.MinValue;
            ForgetUndoVideo();
            ForgetHintVideo();
            HideHintTooltip();
        }

        /// <summary>
        /// Avanza <paramref name="dtMs"/> ms el plazo de la próxima pregunta por el video de Marea (<see cref="UndoVideoPollMs"/>):
        /// lo llama la pantalla en cada fotograma, con <see cref="Refresh"/>. Sin asignaciones.
        /// </summary>
        public void Tick(float dtMs)
        {
            if (_undoVideoPollMs > 0f)
            {
                _undoVideoPollMs -= dtMs;
            }

            if (_hintVideoPollMs > 0f)
            {
                _hintVideoPollMs -= dtMs;
            }

            if (_tip.gameObject.activeSelf)
            {
                _tipMs -= dtMs;
                if (_tipMs <= 0f || !(_tipSlot ?? _hint).Parts.Button.gameObject.activeInHierarchy)
                {
                    HideHintTooltip();
                }
            }

            _hint.TickMissingPulse(dtMs);
            _shuffle.TickMissingPulse(dtMs);
            _reveal.TickMissingPulse(dtMs);
            _extra.TickMissingPulse(dtMs);
        }

        /// <summary>
        /// Respuesta al toque de Pista sin stock, sin saldo y sin video (como Revelar: pregunta 48 (b), D-022) [P]: late la
        /// burbuja «Te faltan {n}» de su ranura, con la cifra <paramref name="missing"/>; con <paramref name="reduceMotion"/>,
        /// solo el color. False si la burbuja no se ve o lleva otra cifra. Sin asignaciones.
        /// </summary>
        public bool PulseHintMissing(int missing, bool reduceMotion)
        {
            return _hint.PulseMissing(missing, reduceMotion);
        }

        /// <summary>Acción del botón Pista (la pone <c>GameScreen.Morpho.cs</c>: <c>IGameActions.PressHint</c>).</summary>
        public void SetHintAction(Action onHint)
        {
            _onHint = onHint ?? throw new ArgumentNullException(nameof(onHint));
        }

        /// <summary>
        /// Enseña sobre la ranura Pista el globo «La morpho no conoce este tramo todavía» (04 §2.3 b) durante lo que se tarda en
        /// leerlo (<see cref="NoticeTiming.ReadMs"/>): a <see cref="HintTooltipSp"/> sp, en una línea si cabe en
        /// <see cref="HintTooltipMaxDp"/> y si no en dos, centrado sobre la ranura y dentro de la pantalla. Otro toque lo vuelve a
        /// empezar. False si la ranura no se ve.
        /// </summary>
        public bool ShowHintTooltip()
        {
            if (!_hint.Active || !_hint.Parts.Button.gameObject.activeInHierarchy)
            {
                return false;
            }

            string text = UiText.HintUnsupported;
            _tipSlot = _hint;
            _tipText.text = text;
            _tipMs = NoticeTiming.ReadMs(text.Length);
            _tip.gameObject.SetActive(true);
            _tip.SetAsLastSibling();
            PlaceHintTooltip();
            return true;
        }

        /// <summary>Acción del botón Barajar (la pone <c>GameScreen.Shuffle.cs</c>; bloque B2).</summary>
        public void SetShuffleAction(Action onShuffle)
        {
            _onShuffle = onShuffle ?? throw new ArgumentNullException(nameof(onShuffle));
        }

        /// <summary>
        /// El mismo globo que Pista, sobre la ranura de la Pororoca, con <paramref name="text"/> (04 §2.4 b: «Hay poca agua libre
        /// para la ola»; bloque B2). False si la ranura no se ve.
        /// </summary>
        public bool ShowShuffleTooltip(string text)
        {
            if (!_shuffle.Active || !_shuffle.Parts.Button.gameObject.activeInHierarchy)
            {
                return false;
            }

            _tipSlot = _shuffle;
            _tipText.text = text;
            _tipMs = NoticeTiming.ReadMs(text.Length);
            _tip.gameObject.SetActive(true);
            _tip.SetAsLastSibling();
            PlaceHintTooltip();
            return true;
        }

        /// <summary>Late la burbuja «Te faltan {n}» de la Pororoca, como la de Pista (bloque B2). Sin asignaciones.</summary>
        public bool PulseShuffleMissing(int missing, bool reduceMotion)
        {
            return _shuffle.PulseMissing(missing, reduceMotion);
        }

        /// <summary>Texto del globo a la vista (vacío si no se ve; tests).</summary>
        public string TooltipText => _tip.gameObject.activeSelf ? _tipText.text : string.Empty;

        /// <summary>Oculta el globo de Pista.</summary>
        public void HideHintTooltip()
        {
            _tipMs = 0f;
            if (_tip.gameObject.activeSelf)
            {
                _tip.gameObject.SetActive(false);
            }
        }

        // El globo de Pista sobre su ranura: ancho de su línea (o, si no cabe en HintTooltipMaxDp, dos líneas parejas con
        // BalancedWidth), centrado y dentro de la pantalla.
        // Mide al enseñarlo y al recolocar la bandeja, nunca por fotograma.
        private void PlaceHintTooltip()
        {
            if (!_tip.gameObject.activeSelf)
            {
                return;
            }

            float scale = UiBuilder.TextScale;
            float maxW = Mathf.Min(HintTooltipMaxDp * scale, _canvasW - (2f * TrayEdgeDp));
            float inner = maxW - (2f * TooltipPadXDp);
            _tipText.textWrappingMode = TextWrappingModes.NoWrap;
            float line = Mathf.Ceil(_tipText.GetPreferredValues(_tipText.text, float.PositiveInfinity, float.PositiveInfinity).x);
            bool wrap = line > inner;
            _tipText.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            float textW = wrap ? BalancedWidth(_tipText, inner, line) : line;
            float textH = Mathf.Ceil(_tipText.GetPreferredValues(_tipText.text, textW, float.PositiveInfinity).y);
            float w = textW + (2f * TooltipPadXDp);
            float h = textH + (2f * TooltipPadYDp);
            var slot = (RectTransform)(_tipSlot ?? _hint).Parts.Button.transform;
            float slotX = slot.anchoredPosition.x;
            float slotTop = -slot.anchoredPosition.y;
            float x = Mathf.Clamp(slotX + ((_slotW - w) * 0.5f), TrayEdgeDp, _canvasW - TrayEdgeDp - w);

            // Por encima de las burbujas «Te faltan {n}» de las vecinas, si se ven: el globo es más ancho que su ranura.
            float bottom = slotTop - (TooltipGapDp * scale);
            float bubbles = float.PositiveInfinity;
            _undo.MinMissingTop(ref bubbles);
            _extra.MinMissingTop(ref bubbles);
            _hint.MinMissingTop(ref bubbles);
            _shuffle.MinMissingTop(ref bubbles);
            _reveal.MinMissingTop(ref bubbles);
            if (!float.IsPositiveInfinity(bubbles))
            {
                bottom = Mathf.Min(bottom, bubbles - MissingNeighborGapDp);
            }

            // Con 3 filas y el suelo bajado (D-046, punto 3), si pisaría el panel, a la franja de la tropa, como las burbujas
            // (aplicación de Q-T6T-3, decidida por el dueño el 2026-10-24) [P]: sobre el panel y, si caben, sobre las burbujas de
            // la franja; si con ellas no cabe bajo la fila del HUD, delante de ellas, sobre el panel. Sin franja, como siempre.
            _tipInBand = false;
            if (!float.IsNaN(_room.Floor) && !float.IsNaN(_room.BandBottom) && bottom - h < _room.Floor)
            {
                _tipInBand = true;
                bottom = _room.BandBottom - MissingPanelClearDp;
                if (TryGetMissingBandTop(out float band) && (float.IsNaN(_room.BandTop) || band - MissingNeighborGapDp - h >= _room.BandTop))
                {
                    bottom = Mathf.Min(bottom, band - MissingNeighborGapDp);
                }
            }

            UiBuilder.Place(_tip, x, bottom - h, w, h);
        }

        // El ancho más estrecho (dp, hasta inner) en que el texto ocupa las mismas líneas que en inner (revisión de MO2): las
        // líneas salen parejas en lugar de dejar una palabra sola en la última («La morpho no conoce / este tramo todavía», no
        // «… este tramo / todavía»), sin partir nada que la tabla no parta, en cualquier idioma. Búsqueda binaria sobre la
        // altura medida; solo al enseñar el globo o recolocar la bandeja.
        private static float BalancedWidth(TextMeshProUGUI text, float inner, float line)
        {
            float height = text.GetPreferredValues(text.text, inner, float.PositiveInfinity).y;
            float lo = Mathf.Min(inner, Mathf.Floor(line * 0.5f));
            float hi = inner;
            for (int i = 0; i < 12 && hi - lo > 0.5f; i++)
            {
                float mid = 0.5f * (lo + hi);
                if (text.GetPreferredValues(text.text, mid, float.PositiveInfinity).y <= height + 0.5f)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid;
                }
            }

            // 1 dp de holgura para que el lienzo corte por el mismo sitio que la medida.
            return Mathf.Min(inner, Mathf.Ceil(hi) + 1f);
        }

        /// <summary>
        /// Respuesta al toque de Revelar sin stock, sin saldo y sin video, en lugar de un aviso del tablero (pregunta 48 (b),
        /// decidida por el dueño; D-022; tarea A1) [P]: si su burbuja «Te faltan {n}» se ve con la cifra
        /// <paramref name="missing"/>, late una vez (de 1 a <see cref="MissingPulseScale"/> y vuelta en 2 ×
        /// <see cref="MissingPulseBeatMs"/>, desde el centro de su pie) y toma los colores de la etiqueta de precio sin saldo
        /// (texto crema sobre el rojo del precio): entran en <see cref="MissingColorInMs"/>, se mantienen hasta
        /// <see cref="MissingColorHoldMs"/> y vuelven a los suyos en <see cref="MissingColorOutMs"/>. Con
        /// <paramref name="reduceMotion"/>, solo el color, que se mantiene hasta <see cref="MissingColorHoldReducedMs"/>. Sin texto
        /// nuevo y sin capturar toques; otro toque lo vuelve a empezar. Devuelve false si la burbuja no se ve o lleva otra cifra.
        /// Sin asignaciones.
        /// </summary>
        public bool PulseRevealMissing(int missing, bool reduceMotion)
        {
            return _reveal.PulseMissing(missing, reduceMotion);
        }

        /// <summary>Acción del botón Revelar (la pone <c>GameScreen.Reveal.cs</c>: <c>IGameActions.PressReveal</c>).</summary>
        public void SetRevealAction(Action onReveal)
        {
            _onReveal = onReveal ?? throw new ArgumentNullException(nameof(onReveal));
        }

        /// <summary>
        /// La ranura de Revelar existe (nivel turbio con ocultas) o no: aparece a la derecha de las demás o desaparece, y la
        /// bandeja se vuelve a centrar (07 §7.4). Devuelve si cambió.
        /// </summary>
        public bool SetRevealVisible(bool visible)
        {
            if (visible == _reveal.Active)
            {
                return false;
            }

            _reveal.SetActive(visible);
            _slots = CountSlots();
            _slotsVersion++;
            _revealKey = int.MinValue;
            Layout(_canvasW, _trayTop);
            return true;
        }

        /// <summary>
        /// Insignia y habilitado de Revelar (04 §2.5; 04 §1.1: el stock se gasta antes que las semillas): con
        /// <paramref name="stock"/> &gt; 0, «+N» en ámbar; sin stock y con <paramref name="video"/> (<c>rv_reveal</c> listo),
        /// «+1» con el icono de video aunque haya saldo (D-028, GDD §6.3 pregunta 23 a: video primero, como Marea); sin video y
        /// con <paramref name="seeds"/> ≥ <paramref name="price"/>, el precio; sin nada, el
        /// precio en rojo y «Te faltan {n}» encima, como la Cuia extra. Es el mismo orden en que cobra el botón
        /// (<c>IGameActions.PressReveal</c>). Con <paramref name="bare"/> queda gris y sin insignia, precio ni aviso, nada que
        /// comprar ahora: la pantalla lo pide cuando ya no queda ninguna oculta (la ranura solo espera a que la luciérnaga salga
        /// de ella y se vaya) y, sin stock, cuando la ranura no se puede usar ni se está eligiendo (fuera de PLAYING), como la
        /// Cuia extra (<c>GameScreen.Reveal.cs</c>). Solo repinta si algo cambió; sin asignaciones salvo al cambiar el aviso.
        /// </summary>
        public void SetRevealBadge(int stock, int price, int seeds, bool video, bool enabled, bool selecting, bool bare = false)
        {
            if (_revealPressed && !_reveal.Parts.Relief.Pressed)
            {
                // El botón que soltó el dedo (OnPointerUp u OnPointerExit al levantar el toque) vuelve abajo mientras dure la
                // selección.
                _reveal.Parts.Relief.Press(true);
            }

            int key = Hash(stock, price, seeds, video ? 1 : 0, enabled ? 1 : 0, selecting ? 1 : 0, _reveal.Active ? 1 : 0, _visible ? 1 : 0, bare ? 1 : 0);
            if (key == _revealKey)
            {
                return;
            }

            _revealKey = key;
            enabled &= !bare;
            UiBuilder.SetEnabled(_reveal.Parts.Button, enabled);
            bool pressed = enabled && selecting;
            if (pressed != _revealPressed)
            {
                // Mientras se elige la cuia, la cara del botón se queda abajo (pulsada); otro toque cancela.
                _revealPressed = pressed;
                _reveal.Parts.Relief.Press(pressed);
            }

            if (bare)
            {
                _reveal.ShowBadge(null, false);
                _reveal.ShowStockBadge(0);
                _reveal.HidePrice();
            }
            else if (stock > 0)
            {
                _reveal.ShowBadge(null, false);
                _reveal.ShowStockBadge(stock);
                _reveal.HidePrice();
            }
            else if (video)
            {
                _reveal.ShowStockBadge(0);
                _reveal.ShowBadge(Number(RevealVideoUnits, true), true);
                _reveal.HidePrice();
            }
            else if (seeds >= price)
            {
                _reveal.ShowBadge(null, false);
                _reveal.ShowStockBadge(0);
                _reveal.ShowPrice(price, 0);
            }
            else
            {
                _reveal.ShowBadge(null, false);
                _reveal.ShowStockBadge(0);
                _reveal.ShowPrice(price, Math.Max(0, price - seeds));
            }

            LayoutMissing();
        }

        public void SetVisible(bool visible)
        {
            if (visible && !_visible)
            {
                // Al volver (tras el embudo o la victoria), los avisos «Te faltan {n}» se vuelven a pintar y el video de Marea se
                // pregunta en el acto (en el embudo pudo verse un video: la espera entre videos ya cuenta).
                _key = int.MinValue;
                _revealKey = int.MinValue;
                ForgetUndoVideo();
                ForgetHintVideo();
            }

            if (!visible)
            {
                HideHintTooltip();
            }

            // Cada ranura se enseña si la bandeja se ve y la ranura existe (Slot.SetVisible lo combina con Active): pasarle
            // «visible && Active» la dejaba oculta para siempre si aparecía después (Revelar al cargar un nivel turbio).
            _visible = visible;
            _tray.gameObject.SetActive(visible);
            _undo.SetVisible(visible);
            _extra.SetVisible(visible);
            _hint.SetVisible(visible);
            _shuffle.SetVisible(visible);
            _reveal.SetVisible(visible);
        }

        /// <summary>
        /// Coloca la bandeja: 80 dp de alto con su borde superior en <paramref name="trayTop"/>, centrada, con las ranuras de 64
        /// dp o, si la etiqueta más larga no cabe, más anchas (<see cref="SlotWidth"/>; tarea L4).
        /// </summary>
        public void Layout(float canvasW, float trayTop)
        {
            _canvasW = canvasW;
            _trayTop = trayTop;
            bool shrink = canvasW < GameScreen.ReferenceWidth - 0.5f; // D-061: el último recurso, solo por debajo de 360 dp
            for (int i = 0; i < _order.Length; i++)
            {
                _order[i].ShrinkAllowed = shrink;
            }

            _slotW = SlotWidthFor(canvasW);
            _grow = 0f;
            for (int i = 0; i < _order.Length; i++)
            {
                _order[i].Spacing(_gap, _wideInset);
                if (_order[i].Active)
                {
                    _grow = Mathf.Max(_grow, _order[i].GrowFor(_slotW));
                }
            }

            float width = Width;
            _tray.sprite = _art.Tray(width);
            float trayX = (canvasW - width) / 2f;
            _trayX = trayX;
            if (_art.TrayIsPainted)
            {
                _tray.type = Image.Type.Sliced;
                UiBuilder.Place(_tray.rectTransform, trayX, trayTop, width, 80f + _grow);
            }
            else
            {
                UiBuilder.Place(_tray.rectTransform, trayX - 8f, trayTop - 2f, width + 16f, 94f + _grow);
            }

            // Orden canónico de 07 §7.4 (TraySlot), de izquierda a derecha y sin huecos: Marea, Cuia extra, Pista, [Pororoca] y
            // Revelar. Todas las ranuras miden lo mismo, también a lo alto (_grow).
            float x = trayX + _pad;
            for (int i = 0; i < _order.Length; i++)
            {
                Slot slot = _order[i];
                if (!slot.Active)
                {
                    continue;
                }

                slot.Place(x, trayTop + 4f, _slotW, _grow);
                x += _slotW + _gap;
            }

            LayoutMissing();
        }

        // Ancho de las ranuras (tarea L4; 02 §12: nada se encoge por debajo de 12 sp y los contenedores crecen): 64 dp si todas
        // las etiquetas de las ranuras que se ven caben en una línea; si no, el que necesita la más larga, sin pasar de
        // SlotMaxDp ni de lo que deja la pantalla (la bandeja a TrayEdgeDp de cada borde). Mide al colocar, nunca por fotograma.
        private float SlotWidthFor(float canvasW)
        {
            float text = 0f;
            float word = 0f;
            for (int i = 0; i < _order.Length; i++)
            {
                if (_order[i].Active)
                {
                    text = Mathf.Max(text, _order[i].MeasureLabelTextDp());
                    word = Mathf.Max(word, _order[i].MeasureLabelWordDp());
                }
            }

            // Cabe en la ranura de 64 dp (56 con las 5, 07 §7.4) con su aire de siempre: no se ensancha (guía 3.6). Si no, su
            // línea y LabelSideInsetDp por lado (revisión de L4).
            _gap = GapDp;
            _pad = PadDp;
            _wideInset = LabelSideInsetDp;
            float baseW = BaseSlotDpFor(_slots);
            if (text + (2f * LabelNarrowInsetDp) <= baseW)
            {
                return baseW;
            }

            // Si la palabra más larga no cabe entera ni en la ranura más ancha (la etiqueta iría en dos líneas partiendo esa
            // palabra), la bandeja se aprieta (MO2) y, si ni así, el aire de la etiqueta es el de la ranura de 64 dp y, si ni
            // con ese, LabelTightInsetDp (revisión de MO2).
            float cap = CapFor(canvasW, _pad, _gap);
            if (word + (2f * LabelSideInsetDp) > cap)
            {
                _gap = CompactGapDp;
                _pad = CompactPadDp;
                cap = CapFor(canvasW, _pad, _gap);
                if (word + (2f * LabelSideInsetDp) > cap)
                {
                    _wideInset = word + (2f * LabelNarrowInsetDp) > cap ? LabelTightInsetDp : LabelNarrowInsetDp;
                }
            }

            float need = text + (2f * _wideInset);
            return Mathf.Clamp(Mathf.Ceil(need), baseW, Mathf.Max(baseW, cap));
        }

        // Lo más ancho que puede ir cada ranura con pad y gap: SlotMaxDp, sin salir de la pantalla (TrayEdgeDp a cada lado).
        private float CapFor(float canvasW, float pad, float gap)
        {
            float room = (canvasW - (2f * TrayEdgeDp) - (2f * pad) - ((_slots - 1) * gap)) / _slots;
            return Mathf.Max(BaseSlotDpFor(_slots), Mathf.Min(SlotMaxDp, Mathf.Floor(room)));
        }

        /// <summary>
        /// Refresca badges, precios y habilitado si el estado cambió. Con la victoria pendiente
        /// (<see cref="LevelFlow.IsResolvingWin"/>: termina el último vertido) no repinta nada y se queda como estaba (D-011,
        /// punto 1: nada se da por ganado antes de verse); sus toques no hacen nada (R15: el flujo no acepta input, y
        /// <c>GameScreen.OnTrayUndo</c>, <c>GameBootstrap.PressExtraCuia</c> y <c>GameBootstrap.PressReveal</c> no actúan ni
        /// terminan antes de tiempo el revelado en marcha). Al empezar la celebración, el estado ya no es el último pintado y se
        /// apaga. Los precios y los avisos «Te faltan {n}» se calculan con el saldo que enseña el de arriba
        /// (<see cref="WalletBar.Refresh"/>): las semillas menos <paramref name="seedsPending"/>, la Cosecha que aún no se ve
        /// (durante la celebración, entera; D-011, punto 1). Lo pendiente entra en la clave de repintado: quien llama no debe
        /// pasar un valor que cambie en cada fotograma (la cuenta de la Cosecha en la pantalla de victoria, con la bandeja
        /// oculta), o la bandeja se repintaría en cada uno (08 §5.4; <c>GameScreen.RefreshHud</c> solo lo pasa en la
        /// celebración).
        /// </summary>
        public void Refresh(int seedsPending = 0)
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel || flow.IsResolvingWin)
            {
                return;
            }

            UndoStatus undo = flow.UndoStatus;
            ExtraCuiaOffer extra = flow.ExtraCuia;
            HintOffer hint = flow.Hint;
            ShuffleOffer shuffle = flow.Shuffle;
            bool video = UndoVideo(undo);
            int seeds = Math.Max(0, (_services?.Wallet.Seeds ?? 0) - Math.Max(0, seedsPending));
            bool hintVideo = HintVideo(hint, seeds);
            bool undoEnabled = undo.HasQuota ? flow.CanUndo : flow.AcceptsBoardInput;
            int key = Hash(undo.Unlimited ? 1 : 0, undo.FreeLeft, undo.Stock, video ? 1 : 0, undoEnabled ? 1 : 0, (int)extra.State, extra.Stock, extra.Missing, seeds);

            // Pista (MO2): estado, stock, precio y video; nunca los ms del enfriamiento, que cambian en cada fotograma.
            key = Hash(key, (int)hint.State, hint.Stock, hint.Price, hintVideo ? 1 : 0, (int)shuffle.State, shuffle.Stock, shuffle.Price, 0);
            if (key == _key)
            {
                return;
            }

            _key = key;
            _refreshRepaints++;
            bool showExtra = extra.State != ExtraCuiaState.Hidden;
            bool showHint = hint.State != HintState.Hidden;
            bool showShuffle = shuffle.Shown && GameScreen.ShuffleOverride != false; // sin ella en los tests que no la tratan
            if (showExtra != _extra.Active || showHint != _hint.Active || showShuffle != _shuffle.Active)
            {
                _extra.SetActive(showExtra);
                _hint.SetActive(showHint);
                _shuffle.SetActive(showShuffle);
                if (!showHint || !showShuffle)
                {
                    HideHintTooltip();
                }

                _slots = CountSlots();
                _slotsVersion++;
                _revealKey = int.MinValue;
                Layout(_canvasW, _trayTop);
            }

            // Marea (04 §1.1): ilimitada sin badge; cupo en blanco; stock «+N» en ámbar; sin nada, «+3» con video o el precio.
            UiBuilder.SetEnabled(_undo.Parts.Button, undoEnabled);
            if (undo.Unlimited)
            {
                _undo.ShowBadge(null, false);
                _undo.ShowStockBadge(0);
                _undo.HidePrice();
            }
            else if (undo.HasQuota)
            {
                _undo.ShowBadge(undo.FreeLeft > 0 ? Number(undo.FreeLeft, false) : null, false);
                _undo.ShowStockBadge(undo.Stock);
                _undo.HidePrice();
            }
            else
            {
                int amount = _services?.Wallet.Config.UndoRefillAmount ?? 3;
                int price = _services?.Wallet.Config.UndoRefillSeedPrice ?? 30;
                _undo.ShowStockBadge(0);
                if (video)
                {
                    _undo.ShowBadge(Number(amount, true), true);
                    _undo.HidePrice();
                }
                else
                {
                    _undo.ShowBadge(Number(amount, true), false);
                    _undo.ShowPrice(price, Math.Max(0, price - seeds));
                }
            }

            if (showExtra)
            {
                // Cuia extra (04 §2.2): stock «+N» o precio; gris si ya se usó, sin vertido útil o con 16 cuias.
                UiBuilder.SetEnabled(_extra.Parts.Button, extra.State == ExtraCuiaState.Ready);
                if (extra.FromStock)
                {
                    _extra.ShowBadge(null, false);
                    _extra.ShowStockBadge(extra.Stock);
                    _extra.HidePrice();
                }
                else if (extra.State == ExtraCuiaState.Ready)
                {
                    _extra.ShowBadge(null, false);
                    _extra.ShowStockBadge(0);
                    _extra.ShowPrice(extra.Price, extra.Missing);
                }
                else
                {
                    // Usada en el nivel, sin vertido útil o con 16 cuias: gris y sin precio.
                    _extra.ShowBadge(null, false);
                    _extra.ShowStockBadge(0);
                    _extra.HidePrice();
                }
            }

            if (showHint)
            {
                PaintHint(hint, seeds, hintVideo);
            }

            if (showShuffle)
            {
                PaintShuffle(shuffle, seeds);
            }

            LayoutMissing();
        }

        // Pororoca · Barajar (bloque B2; 04 §2.4; 04 §1.1: el stock se gasta antes que las semillas): lista, «+N» con stock o el
        // precio (300), en rojo con «Te faltan {n}» sin saldo; gris y sin precio si ya se usó en el nivel o hoy, fuera de PLAYING
        // (también en el modal de bloqueo) o con poca agua, con el stock si lo hay. Con poca agua o con Lluvia o Piraña el toque
        // llega (el globo de 04 §2.4 b), como Pista sin hint_supported.
        private void PaintShuffle(ShuffleOffer offer, int seeds)
        {
            bool ready = offer.State == ShuffleState.Ready;
            bool explain = offer.State == ShuffleState.LowWater || offer.State == ShuffleState.Unsupported;
            UiBuilder.SetEnabled(_shuffle.Parts.Button, ready);
            if (explain)
            {
                _shuffle.Parts.Button.interactable = true; // gris, pero el toque enseña el globo
            }

            if (!ready || offer.Stock > 0)
            {
                _shuffle.ShowBadge(null, false);
                _shuffle.ShowStockBadge(offer.Stock);
                _shuffle.HidePrice();
            }
            else
            {
                _shuffle.ShowBadge(null, false);
                _shuffle.ShowStockBadge(0);
                _shuffle.ShowPrice(offer.Price, Math.Max(0, offer.Price - seeds));
            }
        }

        // Pista · Morpho (04 §2.3; 04 §1.1: el stock se gasta antes que las semillas): lista (o pensando), «+N» con stock; si no, el
        // precio; sin saldo y con rv_hint, «+1» con el icono de video; sin nada, el precio en rojo con «Te faltan {n}» (el mismo
        // orden que cobra GameBootstrap.PressHint). Pulsada mientras Morpho piensa. Gris sin vertido legal, fuera de PLAYING y en
        // el enfriamiento, con el stock si lo hay. Sin hint_supported, gris y sin nada, pero el toque llega (el globo de 04 §2.3 b).
        private void PaintHint(HintOffer hint, int seeds, bool video)
        {
            bool usable = hint.State == HintState.Ready || hint.State == HintState.Thinking;
            bool unsupported = hint.State == HintState.Unsupported;
            UiBuilder.SetEnabled(_hint.Parts.Button, usable);
            if (unsupported)
            {
                _hint.Parts.Button.interactable = true; // gris, pero el toque enseña el globo
            }
            else
            {
                HideHintTooltip();
            }

            bool thinking = hint.State == HintState.Thinking;
            if (thinking != _hintPressed)
            {
                _hintPressed = thinking;
                _hint.Parts.Relief.Press(thinking);
            }

            if (!usable)
            {
                _hint.ShowBadge(null, false);
                _hint.ShowStockBadge(unsupported ? 0 : hint.Stock);
                _hint.HidePrice();
            }
            else if (hint.Stock > 0)
            {
                _hint.ShowBadge(null, false);
                _hint.ShowStockBadge(hint.Stock);
                _hint.HidePrice();
            }
            else if (seeds >= hint.Price)
            {
                _hint.ShowBadge(null, false);
                _hint.ShowStockBadge(0);
                _hint.ShowPrice(hint.Price, 0);
            }
            else if (video)
            {
                _hint.ShowStockBadge(0);
                _hint.ShowBadge(Number(1, true), true);
                _hint.HidePrice();
            }
            else
            {
                _hint.ShowBadge(null, false);
                _hint.ShowStockBadge(0);
                _hint.ShowPrice(hint.Price, Math.Max(0, hint.Price - seeds));
            }
        }

        private int CountSlots()
        {
            int n = 0;
            for (int i = 0; i < _order.Length; i++)
            {
                n += _order[i].Active ? 1 : 0;
            }

            return n;
        }

        // ¿«+3» de Marea con el icono de video? Solo sin cupo (B2-1). La respuesta de los anuncios se guarda UndoVideoPollMs
        // (Tick descuenta el plazo): preguntar crea la cadena de la fecha del contador diario (08 §5.4). Con cupo o sin
        // servicios se olvida, así que la primera pregunta sin cupo es en el acto.
        private bool UndoVideo(UndoStatus undo)
        {
            GameServices? services = _services;
            if (services == null || undo.HasQuota)
            {
                ForgetUndoVideo();
                return false;
            }

            if (_undoVideoPollMs <= 0f)
            {
                _undoVideoReady = services.RewardedReady(AdPlacement.UndoRefill);
                _undoVideoPollMs = UndoVideoPollMs;
            }

            return _undoVideoReady;
        }

        private void ForgetUndoVideo()
        {
            _undoVideoReady = false;
            _undoVideoPollMs = 0f;
        }

        // ¿«+1» de Pista con el icono de video (rv_hint)? Solo si se puede pedir ya, sin stock y sin saldo (el orden de
        // PaintHint); la respuesta de los anuncios se guarda UndoVideoPollMs, como la de Marea.
        private bool HintVideo(HintOffer hint, int seeds)
        {
            GameServices? services = _services;
            bool asks = services != null && (hint.State == HintState.Ready || hint.State == HintState.Thinking) && hint.Stock == 0 && seeds < hint.Price;
            if (!asks)
            {
                ForgetHintVideo();
                return false;
            }

            if (_hintVideoPollMs <= 0f)
            {
                _hintVideoReady = services!.RewardedReady(AdPlacement.Hint);
                _hintVideoPollMs = UndoVideoPollMs;
            }

            return _hintVideoReady;
        }

        private void ForgetHintVideo()
        {
            _hintVideoReady = false;
            _hintVideoPollMs = 0f;
        }

        // «Te faltan {n}» encima de cada ranura sin saldo (104 dp, como desde B2-1); si la vecina también lo enseña, los dos
        // estrechan a su paso de ranura para no pisarse. El texto va a 12 sp, el mínimo de 02 §12, y nunca menos: si no cabe en
        // una línea (una cifra de 3 con el aviso estrecho), la cifra baja a una segunda línea y la placa crece hacia arriba
        // (Slot.PlaceMissing), sin salir del hueco entre el tablero y la bandeja.
        private void LayoutMissing()
        {
            // Vecinas en el orden de la bandeja, sin huecos: una ranura que no existe no separa a las de sus lados.
            int n = 0;
            for (int i = 0; i < _order.Length; i++)
            {
                Slot slot = _order[i];
                if (slot.Active)
                {
                    _activeBuf[n++] = slot;
                }
                else
                {
                    slot.PlaceMissing(slot.MissingWideNowDp);
                }
            }

            float narrow = _slotW + _gap - MissingNeighborGapDp;
            for (int i = 0; i < n; i++)
            {
                Slot slot = _activeBuf[i];
                bool shown = slot.MissingShown;
                bool left = i > 0 && _activeBuf[i - 1].MissingShown;
                bool right = i < n - 1 && _activeBuf[i + 1].MissingShown;
                slot.PlaceMissing(shown && (left || right) ? narrow : slot.MissingWideNowDp);
            }

            PlaceHintTooltip(); // el globo de Pista, por encima de las burbujas que se vean
        }

        private string Number(int value, bool plus)
        {
            int n = 0;
            if (plus)
            {
                _buf[n++] = '+';
            }

            n = UiText.WriteNumber(_buf, n, value, false);
            return new string(_buf, 0, n);
        }

        private static int Hash(int a, int b, int c, int d, int e, int f, int g, int h, int i)
        {
            unchecked
            {
                int x = a;
                x = (x * 397) ^ b;
                x = (x * 397) ^ c;
                x = (x * 397) ^ d;
                x = (x * 397) ^ e;
                x = (x * 397) ^ f;
                x = (x * 397) ^ g;
                x = (x * 397) ^ h;
                return (x * 397) ^ i;
            }
        }

        // Una ranura de 64 dp (guía 3.6; más ancha si su etiqueta o la de otra no cabe: tarea L4): botón de la familia Agua con glifo y etiqueta funcional, badge arriba a la derecha
        // (cupo en blanco o «+3» con video), badge ámbar «+N» del stock arriba a la izquierda, etiqueta de precio montada sobre
        // el borde inferior (como Magic Sort, ficha §2.3) y «Te faltan {n}» encima de la ranura sin saldo.
        // Dónde caben las burbujas: lo comparten todas las ranuras (SetMissingRoom).
        private sealed class MissingRoom
        {
            public float Floor = float.NaN;
            public float BandBottom = float.NaN;
            public float BandTop = float.NaN;
            public bool Held;
            public bool HiddenInBand;
        }

        private sealed class Slot
        {
            /// <summary>Ancho del aviso «Te faltan {n}» sin vecinas que también lo enseñen, con la ranura de 64 dp (dp).</summary>
            public const float MissingWideDp = SlotDp + 40f;

            // Lo que el aviso ancho sobresale de la ranura (dp): 20 por cada lado.
            private const float MissingWideExtraDp = MissingWideDp - SlotDp;

            // Etiqueta de la ranura (tarea L4): a 12 sp (12 × escala dp), en una línea de 16 dp (× escala) con su borde de abajo
            // a 57 dp, con LabelInsetFor de aire a cada lado (la cara de la ranura es redondeada); si no cabe ni en la ranura más
            // ancha, en dos líneas con el interlineado apretado (−0,2 em) y el mismo borde de abajo. Lo que la caja crece hacia
            // arriba lo cede el glifo, que sube y, si hace falta, se encoge (hasta LabelMinIconDp); si ni así cabe (al 130 %), la
            // ranura crece hacia abajo lo que falte (GrowFor, hasta MaxGrowDp) y la etiqueta baja con su borde. La etiqueta de precio nunca
            // sube por encima de PriceBelowLabelDp sobre ese borde: al 130 % baja lo justo para no tapar la etiqueta.
            private const float LabelTopDp = 41f;
            private const float LabelHeightDp = 16f;
            private const float PriceBelowLabelDp = 3f;
            private const float LabelTwoLinesSpacing = -20f;

            // Lo menos que encoge una etiqueta cuya palabra más larga no cabe en la ranura (observación 8 de la jornada 6): 12 dp,
            // los 12 sp de 02 §12 sin la escala de texto. Solo con ShrinkAllowed, en un lienzo de menos de 360 dp (D-061: la
            // excepción de 02 §12 por debajo de 360 dp); a 360 dp nunca encoge.
            private const float LabelFloorSp = UiBuilder.MinTextSp;
            private const float IconDp = 32f;
            private const float IconTopDp = 6f;
            private const float LabelIconGapDp = 1f;

            // Lo más alto que puede empezar la etiqueta en dos líneas para que el glifo más pequeño quepa encima (dp): 2 de aire
            // arriba, el glifo de LabelMinIconDp y LabelIconGapDp hasta la etiqueta.
            private const float LabelLowestTopDp = 2f + LabelMinIconDp + LabelIconGapDp;

            // Tamaño del aviso: 12 sp, el mínimo de 02 §12 («Texto ≥ 12 sp»), sin encogerlo nunca (revisión de la limpieza L2b:
            // el autoajuste hasta 11 sp quedaba por debajo); lo que no cabe en una línea va en dos (PlaceMissing).
            private const float MissingSp = 12f;

            // Placa del aviso [P]: con una línea, 18 dp de alto (una línea de Nunito a 12 sp mide 16,4) y su borde de abajo 6 dp
            // sobre el canto de la ranura. Dos líneas solo si la cifra no cabe en una a 12 sp: la placa crece hacia arriba y no
            // a los lados, así que nunca se mete en el aviso de la ranura vecina (02 §12: los contenedores crecen y nada se
            // recorta). El hueco es estrecho: el panel del tablero llega hasta 31,5 dp sobre la bandeja (BoardLayout centra el
            // bloque con ≥ 13,5 dp hasta boardBottom y el panel le suma 22, dentro de los 40 dp entre el tablero y la bandeja) y
            // las ranuras empiezan 4 dp por debajo de ella. Por eso, con dos líneas, el interlineado baja a 1,09 em (−0,28 em:
            // 16,4 + 13 = 29,4 dp, con los glifos a unos 4 dp del borde, como con una) y la placa de 29,5 dp va 4 dp sobre el
            // canto, a ras de la bandeja: queda ≥ 2 dp por debajo del panel y no toca la insignia de la ranura vecina (un disco de
            // 24 dp que asoma 8 dp sobre el canto). L3: con 34 dp a 6 dp del canto, la placa pisaba 4,5 dp el marco del panel en
            // el 106 a 360 × 780.
            private const float MissingGapDp = 6f;
            private const float MissingTwoLinesGapDp = 4f;
            private const float MissingOneLineDp = 18f;
            private const float MissingTwoLinesDp = 29.5f;

            // Interlineado de las dos líneas, en centésimas de em (TextMeshPro): −0,28 em.
            private const float MissingTwoLinesSpacing = -28f;

            // Holgura al decidir si el texto cabe en una línea (dp): la de redondeo de las medidas de TextMeshPro.
            private const float MissingFitSlackDp = 0.5f;

            // Etiqueta de precio: centrada 72 − 3 dp por debajo del canto de arriba de la ranura (montada sobre el borde de
            // abajo) y, como mucho, del ancho de su paso de ranura con 2 dp de aire (70 dp). Un precio de 4 cifras (fuera de los
            // rangos de 08 §9) no cabe: la etiqueta entera se encoge hasta caber, sin que su cifra (16 sp) baje de 12 sp (02 §12).
            private const float PriceCenterDp = 72f - 3f;
            private const float PriceMinScale = 12f / 16f;

            private readonly RectTransform _badge;
            private readonly Image _badgeDisc;
            private readonly TextMeshProUGUI _badgeText;
            private readonly Image _badgeVideo;
            private readonly RectTransform _stock;
            private readonly TextMeshProUGUI _stockText;
            private readonly PriceTag _price;
            private readonly RectTransform _missing;
            private readonly RectTransform _missingBody;
            private readonly Image _missingPlate;
            private readonly TextMeshProUGUI _missingText;
            private readonly char[] _buf = new char[24];
            private readonly float _scale;
            private float _pulseMs = -1f;
            private bool _pulseReduce;
            private float _pulseScale = 1f;
            private float _pulseScaleX = 1f;
            private float _pulseMaxScaleX = MissingPulseScale;
            private float _pulseColor;
            private float _pulseColorFrom;
            private bool _visible = true;
            private float _x;
            private float _y;
            private bool _inBand;
            private float _missingW = MissingWideDp;
            private float _w = SlotDp;
            private float _gap = GapDp;
            private float _wideInset = LabelSideInsetDp;
            private float _grow;
            private bool _labelTwoLines;
            private int _missingShown = -1;
            private float _missingLineWidth;
            private bool _missingTwoLines;

            public Slot(string name, Transform hud, ProceduralArt art, Sprite icon, Sprite iconDisabled, string label, Action onClick)
            {
                _scale = UiBuilder.TextScale;
                Parts = UiBuilder.Relief(name, hud, art.SlotWater, onClick);
                Glyph = UiBuilder.Image("Icon", Parts.Face, icon, Color.white);
                UiBuilder.Place(Glyph.rectTransform, 16f, 6f, 32f, 32f);
                TextMeshProUGUI text = UiBuilder.Label("Label", Parts.Face, TextStyle.SlotWater, label, 12f, Palette.Cream, TextAlignmentOptions.Center);
                UiBuilder.Place(text.rectTransform, 0f, LabelTopDp, SlotDp, LabelHeightDp);
                Label = text;
                _labelSp = text.fontSize;
                Parts.Relief.Bind(Parts.Face, Parts.Base, Parts.FaceImage, art.SlotWater, Glyph, iconDisabled, text);

                _badge = UiBuilder.Rect("Badge", Parts.Button.transform);
                _badgeDisc = UiBuilder.Image("Disc", _badge, art.Disc, Palette.Cream);
                UiBuilder.Stretch(_badgeDisc.rectTransform);
                _badgeText = UiBuilder.Label("Value", _badge, TextStyle.PlainBold, string.Empty, 13f, Palette.Ink, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_badgeText.rectTransform);
                _badgeVideo = UiBuilder.Image("Video", _badge, art.IconVideo, Color.white);
                _badgeVideo.preserveAspect = true;
                _badge.gameObject.SetActive(false);

                _stock = UiBuilder.Rect("Stock", Parts.Button.transform);
                Image stockDisc = UiBuilder.Sliced("Pill", _stock, art.RoundedRect, Palette.UiAmber);
                stockDisc.pixelsPerUnitMultiplier = 24f / 10f;
                UiBuilder.Stretch(stockDisc.rectTransform);
                _stockText = UiBuilder.Label("Value", _stock, TextStyle.PlainBold, string.Empty, 13f, Palette.Ink, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_stockText.rectTransform);
                _stock.gameObject.SetActive(false);

                _price = new PriceTag("Price", Parts.Button.transform, art);
                _price.Root.gameObject.SetActive(false);

                _missing = UiBuilder.Rect("Missing", hud);
                // Estirada, con el pivote en el centro de su pie: el latido de la pregunta 48 (b) crece hacia arriba y a los
                // lados, nunca hacia el cordón de la bandeja.
                _missingBody = UiBuilder.Rect("Body", _missing);
                _missingBody.pivot = new Vector2(0.5f, 0f);
                _missingPlate = UiBuilder.Sliced("Plate", _missingBody, art.RoundedRect, Palette.PanelLight);
                _missingPlate.pixelsPerUnitMultiplier = 24f / 9f;
                UiBuilder.Stretch(_missingPlate.rectTransform);
                _missingText = UiBuilder.Label("Value", _missingBody, TextStyle.PlainBold, string.Empty, MissingSp, Palette.UiRedWarning, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_missingText.rectTransform);
                _missing.gameObject.SetActive(false);
            }

            public ReliefParts Parts { get; }

            public Image Glyph { get; }

            /// <summary>Lo más alto que empieza el glifo (dp desde el canto de arriba; 2 salvo Pista: <see cref="HintGlyphMinTopDp"/>).</summary>
            public float GlyphMinTopDp { get; set; } = 2f;

            /// <summary>Etiqueta funcional de la ranura (12 sp; tarea L4).</summary>
            public TextMeshProUGUI Label { get; }

            // Tamaño de siempre de la etiqueta (12 sp × escala); SetLabelLines lo baja solo como último recurso.
            private readonly float _labelSp;

            /// <summary>Ancho de la etiqueta de la ranura en una línea (dp), sin su aire.</summary>
            /// <summary>
            /// Mide el ancho de la etiqueta en una línea (dp) a su tamaño de siempre. Es un método, no una propiedad, porque la deja a
            /// ese tamaño si la última colocación la había encogido (SetLabelLines; D-061): solo se llama al colocar la bandeja.
            /// </summary>
            public float MeasureLabelTextDp()
            {
                BaseLabelSize();
                return Label.GetPreferredValues(Label.text, float.PositiveInfinity, float.PositiveInfinity).x;
            }

            /// <summary>El último recurso de SetLabelLines está permitido: el lienzo mide menos de 360 dp (D-061; lo pone Layout).</summary>
            public bool ShrinkAllowed { get; set; }

            /// <summary>
            /// Aire a cada lado de la etiqueta en una ranura de <paramref name="w"/> dp: <see cref="LabelNarrowInsetDp"/> en la de
            /// 64 dp de la guía 3.6 y <see cref="LabelSideInsetDp"/> en la ensanchada (revisión de L4).
            /// </summary>
            public float LabelInsetFor(float w)
            {
                return w > SlotDp + 0.01f ? _wideInset : LabelNarrowInsetDp;
            }

            /// <summary>La separación hasta la ranura vecina y el aire de la etiqueta ensanchada de ahora (los pone la bandeja).</summary>
            public void Spacing(float gap, float wideInset)
            {
                _gap = gap;
                _wideInset = wideInset;
            }

            /// <summary>
            /// Mide el ancho de la palabra más larga de la etiqueta (dp): lo menos que necesita para no partirla (MO2). Como
            /// <see cref="MeasureLabelTextDp"/>, la deja a su tamaño de siempre antes de medir: solo se llama al colocar la bandeja.
            /// </summary>
            public float MeasureLabelWordDp()
            {
                BaseLabelSize();
                return UiBuilder.LongestWordWidth(Label);
            }

            // La etiqueta vuelve a su tamaño de siempre (lo que se mide para decidir la ranura es siempre lo mismo).
            private void BaseLabelSize()
            {
                if (Label.fontSize != _labelSp)
                {
                    Label.fontSize = _labelSp;
                }
            }

            /// <summary>Ancho del aviso «Te faltan {n}» sin vecinas con el ancho de ahora de la ranura (dp).</summary>
            public float MissingWideNowDp => _w + MissingWideExtraDp;

            public bool Active { get; private set; } = true;

            public string BadgeText => (_badge.gameObject.activeSelf ? _badgeText.text : string.Empty) + (_stock.gameObject.activeSelf ? _stockText.text : string.Empty);

            public int PriceShown => _price.Root.gameObject.activeSelf ? _price.Value : 0;

            public bool MissingShown => _missing.gameObject.activeSelf;

            public bool VideoShown => _badge.gameObject.activeSelf && _badgeVideo.gameObject.activeSelf;

            public string MissingText => MissingShown ? _missingText.text : string.Empty;

            public float MissingWidth => _missingW;

            public RectTransform MissingRect => _missing;

            public RectTransform BadgeRect => _badge;

            public TextMeshProUGUI MissingLabel => _missingText;

            public RectTransform PriceRect => _price.Root;

            public bool PulsingNow => _pulseMs >= 0f;

            public float PulseScaleNow => _pulseScale;

            public float PulseScaleXNow => _pulseScaleX;

            public float PulseMaxScale => MissingPulseScale;

            public float PulseMaxScaleX => _pulseMaxScaleX;

            public float PulseColorNow => _pulseColor;

            // «Te faltan {n}» responde al toque si se ve con la cifra missing (BoosterTray.PulseRevealMissing).
            public bool PulseMissing(int missing, bool reduceMotion)
            {
                if (!_missing.gameObject.activeInHierarchy || missing <= 0 || missing != _missingShown)
                {
                    return false;
                }

                // Otro toque con la respuesta en marcha la vuelve a empezar desde el color que ya lleva (sin parpadeo).
                _pulseColorFrom = _pulseMs >= 0f ? _pulseColor : 0f;
                _pulseMs = 0f;
                _pulseReduce = reduceMotion;
                ApplyPulse(0f, _pulseColorFrom);
                return true;
            }

            // Un fotograma de la respuesta (ms desde el toque): el latido de 2 × MissingPulseBeatMs y los colores sin saldo que
            // entran, se mantienen y vuelven; si la burbuja deja de verse, todo vuelve a reposo.
            public void TickMissingPulse(float dtMs)
            {
                if (_pulseMs < 0f)
                {
                    return;
                }

                _pulseMs += dtMs;
                float t = _pulseMs;
                float hold = _pulseReduce ? MissingColorHoldReducedMs : MissingColorHoldMs;
                if (t >= hold + MissingColorOutMs || !_missing.gameObject.activeInHierarchy)
                {
                    _pulseMs = -1f;
                    ApplyPulse(0f, 0f);
                    return;
                }

                float beatMs = 2f * MissingPulseBeatMs;
                float beat = _pulseReduce || t >= beatMs ? 0f : Mathf.Sin(Mathf.PI * (t / beatMs));
                float color = t < MissingColorInMs ? Mathf.Lerp(_pulseColorFrom, 1f, t / MissingColorInMs) : (t <= hold ? 1f : 1f - ((t - hold) / MissingColorOutMs));
                ApplyPulse(beat, Mathf.Clamp01(color));
            }

            // beat = 0 en reposo, 1 en lo alto del latido (escala: a lo alto, MissingPulseScale; a lo ancho, lo que deja la
            // vecina; sin Reducir movimiento); color = 0..1 hacia los de la etiqueta de precio sin saldo (texto crema sobre el
            // rojo). Solo escribe lo que cambia.
            private void ApplyPulse(float beat, float color)
            {
                float scale = Mathf.Lerp(1f, MissingPulseScale, beat);
                float scaleX = Mathf.Lerp(1f, _pulseMaxScaleX, beat);
                if (!Mathf.Approximately(scale, _pulseScale) || !Mathf.Approximately(scaleX, _pulseScaleX))
                {
                    _pulseScale = scale;
                    _pulseScaleX = scaleX;
                    _missingBody.localScale = new Vector3(scaleX, scale, 1f);
                }

                if (!Mathf.Approximately(color, _pulseColor))
                {
                    _pulseColor = color;
                    _missingPlate.color = Color.Lerp(Palette.PanelLight, Palette.UiRedWarning, color);
                    _missingText.color = Color.Lerp(Palette.UiRedWarning, Palette.Cream, color);
                }
            }

            // El borde de arriba de su aviso «Te faltan {n}», si se ve bajo el tablero y está más alto que top (dp desde arriba).
            public void MinMissingTop(ref float top)
            {
                if (_missing.gameObject.activeInHierarchy && !_inBand)
                {
                    top = Mathf.Min(top, -_missing.anchoredPosition.y);
                }
            }

            // Lo mismo para el que va a la franja de la tropa, se dibuje o no ahora (la tropa o el aviso del tablero).
            public void MinMissingBandTop(ref float top)
            {
                if (_missing.gameObject.activeInHierarchy && _inBand)
                {
                    top = Mathf.Min(top, -_missing.anchoredPosition.y);
                }
            }

            /// <summary>Lo que comparten las ranuras: dónde caben las burbujas (BoosterTray.SetMissingRoom).</summary>
            public MissingRoom? Room { get; set; }

            /// <summary>Su burbuja, a la vista, va a la franja de la tropa (no cabe bajo el panel).</summary>
            public bool MissingInBand => _inBand && _missing.gameObject.activeInHierarchy;

            /// <summary>Y se dibuja ahora (ni la tropa ni el aviso del tablero ocupan la franja).</summary>
            public bool MissingInBandDrawn => MissingInBand && _missingBody.gameObject.activeSelf;

            public void SetActive(bool active)
            {
                Active = active;
                SetVisible(_visible);
            }

            public void SetVisible(bool visible)
            {
                _visible = visible;
                bool on = visible && Active;
                Parts.Button.gameObject.SetActive(on);
                if (!on)
                {
                    _missing.gameObject.SetActive(false);
                }
            }

            /// <summary>
            /// Lo que tiene que crecer hacia abajo una ranura de <paramref name="w"/> dp para que su etiqueta en dos líneas quepa
            /// bajo el glifo más pequeño (dp; 0 si va en una línea o ya cabe; como mucho <see cref="MaxGrowDp"/>). Mide al
            /// colocar, nunca por fotograma.
            /// </summary>
            public float GrowFor(float w)
            {
                float inner = w - (2f * LabelInsetFor(w));
                if (!SetLabelLines(inner))
                {
                    return 0f;
                }

                float h = Mathf.Ceil(Label.GetPreferredValues(Label.text, inner, float.PositiveInfinity).y);
                float top = LabelTopDp + LabelHeightDp - h;
                return Mathf.Clamp(Mathf.Ceil(LabelLowestTopDp - top), 0f, MaxGrowDp);
            }

            public void Place(float x, float y, float w, float grow)
            {
                _x = x;
                _y = y;
                _w = w;
                _grow = grow;
                UiBuilder.Place((RectTransform)Parts.Button.transform, x, y, w, 72f + grow);
                RectTransform visual = (RectTransform)Parts.Face.parent;
                visual.sizeDelta = new Vector2(visual.sizeDelta.x, Parts.Sprites.HeightDp + grow); // la cara crece hacia abajo
                UiBuilder.Place(_badge, w - 16f, -8f, 24f, 24f);
                UiBuilder.Place(_badgeText.rectTransform, 0f, 0f, 24f, 24f);
                PlaceLabel();
                if (_price.Root.gameObject.activeSelf)
                {
                    PlacePrice();
                }

                PlaceMissing(_missingW); // Layout las vuelve a medir enseguida (LayoutMissing)
            }

            // La etiqueta en una línea si cabe en la ranura; si no, en dos, con el glifo más pequeño encima (tarea L4).
            private void PlaceLabel()
            {
                float inset = LabelInsetFor(_w);
                float inner = _w - (2f * inset);
                bool two = SetLabelLines(inner);

                // El borde de abajo de la etiqueta solo baja lo que crece la ranura (la etiqueta de precio va debajo); con la
                // escala de texto o en dos líneas, la caja crece hacia arriba y el glifo sube y, si hace falta, se encoge.
                float bottom = LabelTopDp + LabelHeightDp + _grow;
                float h = two ? Mathf.Ceil(Label.GetPreferredValues(Label.text, inner, float.PositiveInfinity).y) : LabelHeightDp * _scale;
                float top = bottom - h;
                // El glifo empieza como muy arriba en GlyphMinTopDp (Pista: HintGlyphMinTopDp) y se encoge desde arriba; si ni
                // con LabelMinIconDp cabe, sube hasta 2 dp antes que pisar la etiqueta.
                float icon = Mathf.Clamp(top - LabelIconGapDp - GlyphMinTopDp, LabelMinIconDp, IconDp);
                float iconTop = Mathf.Clamp(top - LabelIconGapDp - icon, 2f, Mathf.Max(IconTopDp, GlyphMinTopDp));
                UiBuilder.Place(Label.rectTransform, inset, top, inner, h);
                UiBuilder.Place(Glyph.rectTransform, (_w - icon) * 0.5f, iconTop, icon, icon);
            }

            // Una línea si la etiqueta cabe en inner dp; si no, dos con el interlineado apretado. Devuelve si van dos. Si ni la
            // palabra más larga cabe en inner (cinco ranuras a 320 dp con la letra al 130 %: «Deshace / r», observación 8 de la
            // jornada 6) y el lienzo mide menos de 360 dp (ShrinkAllowed), la etiqueta encoge lo justo para que quepa, sin bajar de
            // LabelFloorSp (D-061): nunca se parte una palabra.
            private bool SetLabelLines(float inner)
            {
                BaseLabelSize(); // cada colocación empieza con su tamaño de siempre
                float word = UiBuilder.LongestWordWidth(Label);
                if (ShrinkAllowed && word > inner && inner > 0f)
                {
                    float fit = Mathf.Floor(_labelSp * (inner / word) * 10f) / 10f;
                    Label.fontSize = Mathf.Max(LabelFloorSp, Mathf.Min(_labelSp, fit));
                }

                Label.textWrappingMode = TextWrappingModes.NoWrap;
                bool two = Label.GetPreferredValues(Label.text, float.PositiveInfinity, float.PositiveInfinity).x > inner;
                if (two != _labelTwoLines)
                {
                    _labelTwoLines = two;
                    Label.lineSpacing = two ? LabelTwoLinesSpacing : 0f;
                }

                Label.textWrappingMode = two ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                return two;
            }

            public void ShowBadge(string? text, bool video)
            {
                bool on = text != null;
                _badge.gameObject.SetActive(on);
                if (!on)
                {
                    return;
                }

                _badgeText.text = text;
                _badgeVideo.gameObject.SetActive(video);
                float w = video ? 44f : 24f;
                UiBuilder.Place(_badge, _w - w + 8f, -8f, w, 24f);
                _badgeDisc.type = Image.Type.Simple;
                UiBuilder.Place(_badgeText.rectTransform, 0f, 0f, video ? 22f : 24f, 24f);
                if (video)
                {
                    UiBuilder.Place(_badgeVideo.rectTransform, 22f, 3f, 20f, 18f);
                }
            }

            public void ShowStockBadge(int stock)
            {
                bool on = stock > 0;
                _stock.gameObject.SetActive(on);
                if (!on)
                {
                    return;
                }

                int n = 0;
                _buf[n++] = '+';
                n = UiText.WriteNumber(_buf, n, stock, false);
                _stockText.SetCharArray(_buf, 0, n);
                UiBuilder.Place(_stock, -8f, -8f, 14f + (8f * n), 22f);
            }

            public void ShowPrice(int price, int missing)
            {
                _price.Root.gameObject.SetActive(true);
                _price.Set(price, 0, missing > 0);
                PlacePrice();
                _missing.gameObject.SetActive(missing > 0 && Parts.Button.gameObject.activeSelf);
                if (missing > 0 && missing != _missingShown)
                {
                    _missingShown = missing;
                    string text = UiText.TrayMissing(missing);
                    _missingText.text = text;

                    // Ancho en una línea a 12 sp, solo cuando cambia la cifra.
                    _missingLineWidth = _missingText.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
                }

                if (missing > 0)
                {
                    PlaceMissing(_missingW);
                }
            }

            // La etiqueta de precio, centrada en la ranura y como mucho del ancho de su paso de ranura con 2 dp de aire.
            private void PlacePrice()
            {
                float k = Mathf.Clamp((_w + _gap - 2f) / _price.Width, PriceMinScale, 1f);
                _price.Root.localScale = new Vector3(k, k, 1f);
                float top = Mathf.Max(PriceCenterDp + _grow - (_price.Height * 0.5f * k), LabelTopDp + LabelHeightDp + _grow - PriceBelowLabelDp);
                _price.PlaceAt((_w - (_price.Width * k)) * 0.5f, top);
            }

            public void HidePrice()
            {
                _price.Root.gameObject.SetActive(false);
                _missing.gameObject.SetActive(false);
            }

            // «Te faltan {n}» de widthDp centrado sobre la ranura: en una línea si a 12 sp cabe en widthDp, con su borde de abajo
            // 6 dp por encima del canto, y, si no, en dos («Te faltan» y la cifra), con la placa más alta y más cerca del canto.
            // Con la escala de texto, la placa y su aire crecen con ella (02 §12: la placa crece hacia arriba; tarea A1).
            public void PlaceMissing(float widthDp)
            {
                _missingW = widthDp;
                bool twoLines = _missingLineWidth > widthDp - MissingFitSlackDp;
                if (twoLines != _missingTwoLines)
                {
                    _missingTwoLines = twoLines;
                    _missingText.textWrappingMode = twoLines ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                    _missingText.lineSpacing = twoLines ? MissingTwoLinesSpacing : 0f;
                }

                float h = (twoLines ? MissingTwoLinesDp : MissingOneLineDp) * _scale;
                float gap = (twoLines ? MissingTwoLinesGapDp : MissingGapDp) * _scale;
                float top = _y - gap - h;

                // Con 3 filas y el suelo bajado (D-046; Q-T6T-3), si pisaría el panel, a la franja de la tropa, sobre el panel (el
                // latido crece hacia arriba desde su borde de abajo: tampoco lo pisa). Mientras la tropa está ahí o el aviso del
                // tablero ocupa la franja, no se dibuja.
                MissingRoom? room = Room;
                _inBand = room != null && !float.IsNaN(room.Floor) && !float.IsNaN(room.BandBottom) && top < room.Floor;
                bool draw = true;
                if (_inBand)
                {
                    top = room!.BandBottom - MissingPanelClearDp - h;
                    draw = !room.Held && !room.HiddenInBand;
                }

                if (_missingBody.gameObject.activeSelf != draw)
                {
                    _missingBody.gameObject.SetActive(draw);
                }

                UiBuilder.Place(_missing, _x + ((_w - widthDp) * 0.5f), top, widthDp, h);

                // Lo más que crece a lo ancho en el latido: con una vecina (la burbuja estrecha), sin acercarse a ella más de
                // MissingPulseClearDp (con 2 dp de aire entre las dos, nada: solo late hacia arriba).
                _pulseMaxScaleX = widthDp < MissingWideNowDp - 0.01f
                    ? Mathf.Clamp(1f + (2f * (MissingNeighborGapDp - MissingPulseClearDp) / widthDp), 1f, MissingPulseScale)
                    : MissingPulseScale;
            }
        }
    }
}
