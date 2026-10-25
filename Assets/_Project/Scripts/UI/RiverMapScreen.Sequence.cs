#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// La secuencia del Mapa del 10 (tarea M4.2; 06 §2.3, fila 3; D-013, punto 7; P-M2 y P-M4 de D-023), parcial de
    /// <see cref="RiverMapScreen"/> sobre sus ganchos <c>Sequence*</c>. Sale cuando el Mapa se abre desde
    /// <see cref="MapOpenSource.L10"/> (tras «Siguiente» en la victoria del 10, o al entrar al inicio con <c>max_level</c> ≥ 10 y
    /// <c>map_seen_level</c> &lt; 10: <see cref="L10SequenceRule"/>) y dura como mucho <see cref="MapConfig.L10MapMaxMs"/> ms:
    /// <list type="number">
    /// <item><description><b>Vista Cuenca</b>, con el estuario ya lleno (lo pinta el río de M3.4 con <c>max_level</c>).</description></item>
    /// <item><description><b>TiTino se presenta</b> desde su casa del estuario con <c>sloth_present</c> y, si tiene voz
    /// (<c>ui_sloth_enabled</c> ∧ <c>settings.sloth_messages</c>; D-016, punto 4) y su casa se ve, el bocadillo
    /// <c>map_l10_intro</c> («Yo vivo aquí, donde el río entra al mar.» [P]; nunca una clave <c>sloth_line_*</c>), encima de la
    /// copa de su embaúba con el pico hacia él y, como TiTino cuelga bajo la rama, un tallo del pico que baja por delante de la
    /// copa hasta <see cref="L10BubbleStemGapDp"/> dp de su coronilla (<see cref="SpeechBubble"/>; revisión de M4.2: que se vea
    /// quién habla), lo que pide su lectura (<see cref="NoticeTiming.ReadMs(string)"/>). Sin
    /// voz, solo el gesto (<see cref="PresentOnlyMs"/>); sin su arte (solo si un test lo apaga; desde D-052 sale en todos los builds), nada. Si el bocadillo sale, lo avisa
    /// (<see cref="L10BubbleSaid"/>): no gasta ninguna frase de la sesión de TiTino, pero cuenta como pantalla en la que habló y
    /// el saludo del día del inicio que la sigue se aplaza (D-028, pregunta 60 b).</description></item>
    /// <item><description><b>La placa del Dato del río 1</b> (el <c>arrival_fact</c> de la parada 1,
    /// <c>map.stop.estuario.dato_03</c>; 08 §4.5) con el retrato de TiTino, arriba de la zona del mapa, y a la vez <b>la canoa de
    /// Capi sube de Belém a Santarém</b>: «ponerse al día» (M3.5), que espera quieto mientras TiTino habla, enciende las boyas de
    /// 1 a 10 y llena el Xingu al pasar su boca. La placa se queda lo que pide su lectura. Sin el visto bueno del asesor, en
    /// release (<see cref="Loc.Fact(string)"/> da ""), no hay placa.</description></item>
    /// </list>
    /// <para><b>Se puede saltar:</b> mientras dura, un área táctil sobre toda la pantalla del Mapa (<c>map_l10_skip</c> en
    /// <c>AF_LAYOUT</c>) la completa con un toque: la canoa llega en el acto (las llegadas, con <c>skipped</c>), el bocadillo y la
    /// placa se van y quedan «Jugar · Nivel 11» y «←». Cerrar el Mapa a medias (Atrás de Android) también la completa. Al acabar,
    /// saltarla o cerrarla, lo visto del Mapa pasa al 10 (<see cref="L10SequenceEnded"/>: <c>MarkMapSeen(10)</c>), así que no se
    /// repite, y la sonda escribe <c>river_fact_shown</c> con el dato y lo que estuvo a la vista.</para>
    /// <para><b>A tamaño de teléfono</b> (revisión de M4.2): la vista Cuenca no se amplía (la casa mide lo mismo en dp a cualquier
    /// zoom), así que durante la secuencia la figura de la casa (la embaúba y TiTino) crece a <see cref="L10HomeScale"/>× sobre su
    /// pie, con un rebote de <see cref="L10HomeGrowMs"/> ms al entrar, y TiTino mira hacia el estuario al presentarse (la garra de
    /// <c>sloth_present</c> señala el agua); al acabar vuelve a su tamaño con el fundido de salida. Mientras dura, el botón «Norte
    /// arriba» no se ve ni se toca (la capa del salto lo tapa); las placas de los mundos futuros se van mientras se ve la placa del
    /// dato, y los nombres de país y las placas que pisan el bocadillo, la placa del dato, la casa o la canoa con su mástil, sus banderas y
    /// el guía (al salir y al llegar; <see cref="L10DimMarginDp"/> de margen) bajan a <see cref="L10DimAlpha"/> o, si les tapa más
    /// del <see cref="L10DimHideCover"/> (o el bocadillo o la placa, opacos, lo pisan de verdad), no se ven: ningún rótulo a medio
    /// tapar. Salvo la placa del mundo de destino (el del 11, el de «Jugar · Nivel 11» y el viaje de la canoa): se ve entera
    /// mientras sea un mundo futuro (hasta que la canoa llega a Santarém, su gran parada) y, si pisa la canoa en Belém con su
    /// mástil y sus banderas o el bocadillo (con <see cref="L10DimMarginDp"/> de margen), sube hasta quedar
    /// <see cref="L10DestinationClearDp"/> dp por encima de lo alto de lo que pisa, sin salir de la vista (los rótulos que pisa
    /// ahí se atenúan como los de la canoa); al acabar, vuelve a su sitio.</para>
    /// <para><b>Con <c>max_level</c> &gt; 10</b> y el Mapa sin ver (un perfil de antes del Mapa o <c>af_max_level</c>), la canoa
    /// «se pone al día» hasta <c>max_level</c> y sale del encuadre (que es el del 10: la casa, Belém y Santarém); en el juego normal
    /// (ganar el 10 y luego Siguiente, Inicio o cerrar la app) no pasa [P].</para>
    /// <para>Con Reducir movimiento, sin fundidos ni viaje (lo hace «ponerse al día»). Las piezas se crean una vez; los textos y
    /// las medidas, al empezar y al recolocar; por fotograma, solo opacidades y escalas, sin asignaciones.</para>
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Sin voz, TiTino hace <c>sloth_present</c> y la canoa sale a los tantos ms [P]: el gesto (0,6 s) y un respiro.</summary>
        public const int PresentOnlyMs = 1000;

        /// <summary>Fundido de entrada y de salida del bocadillo (ms), el del inicio (<see cref="HomeSloth.BubbleFadeMs"/>).</summary>
        public const float L10BubbleFadeMs = AF.UI.HomeSloth.BubbleFadeMs;

        /// <summary>Fundido de entrada de la placa del dato (ms), el de la franja de llegada.</summary>
        public const float L10PlateFadeMs = ArrivalStrip.PlateFadeMs;

        /// <summary>Al acabar (o saltarla), el bocadillo y la placa se van con este fundido (ms) [P]; con Reducir movimiento, en el acto.</summary>
        public const float L10OutFadeMs = 200f;

        /// <summary>Tras llegar la canoa a Santarém, la secuencia sigue al menos estos ms [P] (sin dato que leer).</summary>
        public const int L10TripTailMs = 400;

        /// <summary>Hueco entre el bocadillo y la cabeza de TiTino (dp) [P].</summary>
        public const float L10BubbleGapDp = 2f;

        /// <summary>Ancho máximo del bocadillo (dp), el del inicio.</summary>
        public const float L10BubbleMaxWidthDp = AF.UI.HomeSloth.BubbleMaxWidthDp;

        /// <summary>Margen de la placa del dato y del bocadillo con el borde de la zona del mapa (dp) [P].</summary>
        public const float L10InsetDp = 8f;

        /// <summary>
        /// Escala de la figura de la casa (embaúba y TiTino) durante la secuencia [P] (revisión de M4.2): en la Cuenca la casa va a
        /// <see cref="HomeBasinScale"/> y TiTino apenas se ve a tamaño de teléfono; así mide ≥ <see cref="L10TiTinoMinDp"/> dp de alto.
        /// </summary>
        public const float L10HomeScale = 2f;

        /// <summary>Alto mínimo de TiTino (sus piezas, con la rama) durante la secuencia (dp) [P].</summary>
        public const float L10TiTinoMinDp = 40f;

        /// <summary>Rebote de entrada de la figura de la casa hasta <see cref="L10HomeScale"/> (ms) [P].</summary>
        public const float L10HomeGrowMs = 250f;

        /// <summary>Opacidad de un nombre de país o una placa de mundo que pisa una pieza de la secuencia [P].</summary>
        public const float L10DimAlpha = 0.35f;

        /// <summary>Margen alrededor del bocadillo, la placa del dato y la canoa al decidir qué rótulo pisan (dp) [P].</summary>
        public const float L10DimMarginDp = 4f;

        /// <summary>Si una pieza de la secuencia tapa más de esta fracción de un rótulo, el rótulo no se ve (alpha 0) [P].</summary>
        public const float L10DimHideCover = 0.2f;

        /// <summary>
        /// Hueco entre la placa del mundo de destino y lo alto de la canoa con su mástil y sus banderas cuando la secuencia la
        /// sube para separarla de ella (dp) [P] (revisión de M4.2).
        /// </summary>
        public const float L10DestinationClearDp = 8f;

        /// <summary>Lo que el tallo del pico del bocadillo se queda por encima de la coronilla de TiTino (dp) [P] (revisión de M4.2).</summary>
        public const float L10BubbleStemGapDp = 6f;

        /// <summary>Grosor del tallo del pico del bocadillo (dp) [P] (revisión de M4.2).</summary>
        public const float L10BubbleStemDp = 2f;

        private RectTransform? _seqLayer;
        private Button? _seqSkip;
        private SpeechBubble? _seqBubble;
        private RectTransform? _seqPlate;
        private CanvasGroup? _seqPlateGroup;
        private Image? _seqPortrait;
        private TextMeshProUGUI? _seqFact;

        private bool _seqOpen;
        private bool _seqActive;
        private bool _seqStarted;
        private bool _seqDone;
        private bool _seqSkipped;
        private bool _seqPlaceDirty;
        private float _seqMs;
        private float _seqOutMs = -1f;
        private int _seqTripFromMs;
        private int _seqEndMs;
        private string _seqBubbleText = string.Empty;
        private string? _seqFactKey;
        private string _seqFactText = string.Empty;
        private bool _seqWithBubble;
        private bool _seqWithPlate;
        private float _seqPlateShownMs;
        private bool _seqFactReported;

        // La figura de la casa a tamaño de teléfono, el atenuado de los rótulos y el botón «Norte arriba» (revisión de M4.2).
        private bool _seqHomeGrown;
        private float _seqHomeK = 1f;
        private float _seqFacing = 1f;
        private float _seqDim;
        private int _seqDimCount;
        private CanvasGroup?[] _seqDimGroups = Array.Empty<CanvasGroup?>();
        private float[] _seqDimTarget = Array.Empty<float>();
        private float[] _seqDimBubble = Array.Empty<float>();
        private float[] _seqDimPlate = Array.Empty<float>();
        private CanvasGroup? _seqNorthUpGroup;
        private readonly Vector3[] _seqCorners = new Vector3[4];
        private readonly Rect[] _seqObstacles = new Rect[11 + MaxCanoeFlags];
        private int _seqObstacleCount;

        // La placa del mundo de destino (el del 11): nunca se atenúa y, si pisa la canoa en Belém, sube (revisión de M4.2).
        private RectTransform? _seqDestPlate;
        private int _seqDestIndex = -1;
        private bool _seqDestMoved;
        private Vector2 _seqDestBase;
        private Vector2 _seqDestShifted;
        private Rect _seqDestBox; // su caja (dp del lienzo de la UI) si la secuencia la subió: los rótulos que pisa se atenúan

        // La cabeza de TiTino en el rig de la casa (para el pico del bocadillo), buscada una vez.
        private RectTransform? _seqSlothHead;
        private SlothRig? _seqSlothHeadOf;

        /// <summary>
        /// Lo llama la secuencia al acabar, al saltarla o al cerrar el Mapa a medias (una vez por secuencia): <see cref="GameBootstrap"/>
        /// anota lo visto del Mapa hasta el 10 (<c>MarkMapSeen(10)</c>).
        /// </summary>
        internal Action? L10SequenceEnded { get; set; }

        /// <summary>TiTino tiene voz (<c>ui_sloth_enabled</c> ∧ <c>settings.sloth_messages</c>); sin quien lo diga, sí.</summary>
        internal Func<bool>? L10SlothVoice { get; set; }

        /// <summary>
        /// Lo llama la secuencia cuando sale el bocadillo <c>map_l10_intro</c> (una vez por secuencia): <see cref="GameBootstrap"/>
        /// lo apunta en la sesión de TiTino (<c>SlothSession.RecordMapIntro</c>; D-028, pregunta 60 b).
        /// </summary>
        internal Action? L10BubbleSaid { get; set; }

        /// <summary>La secuencia del Mapa del 10 está en marcha (el toque la salta).</summary>
        public bool L10SequenceActive => _seqActive;

        /// <summary>Esta apertura del Mapa fue la de la secuencia del Mapa del 10 (aunque ya haya acabado).</summary>
        public bool L10SequenceOpen => _seqOpen;

        /// <summary>La secuencia de esta apertura acabó (sola, con un toque o al cerrar).</summary>
        public bool L10SequenceDone => _seqDone;

        /// <summary>La secuencia de esta apertura se saltó (un toque o el cierre a medias).</summary>
        public bool L10SequenceSkipped => _seqSkipped;

        /// <summary>Milisegundos de secuencia transcurridos.</summary>
        public float L10SequenceElapsedMs => _seqMs;

        /// <summary>Lo que dura la secuencia (ms; ≤ <see cref="MapConfig.L10MapMaxMs"/>), una vez empezada.</summary>
        public int L10SequenceDurationMs => _seqEndMs;

        /// <summary>Cuándo deja TiTino de hablar y sale la canoa con la placa del dato (ms desde el principio).</summary>
        public int L10TripFromMs => _seqTripFromMs;

        /// <summary>El área táctil que salta la secuencia.</summary>
        public Button? L10SkipButton => _seqSkip;

        /// <summary>La secuencia lleva el bocadillo (TiTino con voz y su casa a la vista).</summary>
        public bool L10WithBubble => _seqWithBubble;

        /// <summary>El bocadillo está a la vista.</summary>
        public bool L10BubbleShown => _seqBubble != null && _seqBubble.Visible && _seqBubble.Alpha > 0f;

        /// <summary>Lo que dice el bocadillo ("" si no sale en esta secuencia).</summary>
        public string L10BubbleText => _seqWithBubble ? _seqBubbleText : string.Empty;

        /// <summary>El bocadillo (tests y capturas).</summary>
        internal SpeechBubble? L10Bubble => _seqBubble;

        /// <summary>La clave del dato de la placa (<c>map.stop.estuario.dato_03</c>), o null sin parada 1.</summary>
        public string? L10FactKey => _seqFactKey;

        /// <summary>El texto de la placa del dato ("" si no hay placa).</summary>
        public string L10FactText => _seqWithPlate ? _seqFactText : string.Empty;

        /// <summary>La placa del dato está a la vista.</summary>
        public bool L10FactShown => _seqPlate != null && _seqPlate.gameObject.activeSelf && _seqPlateGroup!.alpha > 0f;

        /// <summary>La placa del dato (tests y capturas).</summary>
        internal RectTransform? L10Plate => _seqPlate;

        /// <summary>El retrato de la placa del dato (null sin su arte).</summary>
        internal Image? L10Portrait => _seqPortrait;

        /// <summary>Escala actual de la figura de la casa (1 fuera de la secuencia; <see cref="L10HomeScale"/> durante ella).</summary>
        internal float L10HomeCurrentScale => _seqHomeK;

        /// <summary>Cuánto se aplica el atenuado de los rótulos y el botón «Norte arriba» oculto (0 fuera de la secuencia; 1 en ella).</summary>
        internal float L10DimAmount => _seqDim;

        /// <summary>
        /// La placa del mundo de destino (el del nivel 11) en esta secuencia, o null si no se ve (también cuando la canoa llega a
        /// Santarém, la gran parada de ese mundo: «ponerse al día» quita su silueta, porque ya no es un mundo futuro).
        /// </summary>
        internal RectTransform? L10DestinationPlate => L10DestinationLive(_seqDestIndex - _countries.Count) ? _seqDestPlate : null;

        /// <summary>La placa del mundo de destino se subió para no pisar la canoa en Belém (y sigue subida).</summary>
        internal bool L10DestinationLifted => _seqDestMoved;

        /// <summary>
        /// Salta la secuencia del Mapa del 10 (el toque en cualquier sitio): la canoa llega en el acto, el bocadillo y la placa se
        /// van y queda el Mapa con «Jugar · Nivel 11» y «←». False si no estaba en marcha.
        /// </summary>
        public bool SkipL10Sequence()
        {
            if (!_seqActive || !Visible)
            {
                return false;
            }

            FinishL10Sequence(true, true);
            return true;
        }

        // ============================================================================================ ganchos

        partial void SequenceBuild(RectTransform root, RectTransform dynamicLayer)
        {
            _seqLayer = UiBuilder.Rect("L10Sequence", root);
            _seqSkip = UiBuilder.BareButton("L10Skip", _seqLayer, () => SkipL10Sequence());
            UiBuilder.Stretch((RectTransform)_seqSkip.transform);

            _seqPlate = UiBuilder.Rect("L10FactPlate", _seqLayer);
            _seqPlateGroup = _seqPlate.gameObject.AddComponent<CanvasGroup>();
            _seqPlateGroup.blocksRaycasts = false;
            _seqPlateGroup.interactable = false;
            Image edge = UiBuilder.Sliced("Edge", _seqPlate, _art.RoundedRect, ArrivalStrip.PlateEdge);
            edge.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / ArrivalStrip.PlateRadiusDp;
            UiBuilder.Stretch(edge.rectTransform);
            Image back = UiBuilder.Sliced("Back", _seqPlate, _art.RoundedRect, Palette.PanelLight);
            back.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / (ArrivalStrip.PlateRadiusDp - 1f);
            UiBuilder.Stretch(back.rectTransform);
            back.rectTransform.offsetMin = new Vector2(1f, 1f);
            back.rectTransform.offsetMax = new Vector2(-1f, -1f);
            _seqPortrait = UiBuilder.Image("Portrait", _seqPlate, null, Color.white);
            _seqPortrait.preserveAspect = true;
            _seqFact = UiBuilder.Label("Fact", _seqPlate, TextStyle.Body, string.Empty, ArrivalStrip.FactSp, Palette.UiInk, TextAlignmentOptions.TopLeft);
            _seqFact.textWrappingMode = TextWrappingModes.Normal;
            _seqFact.enableAutoSizing = false;
            _seqPlate.gameObject.SetActive(false);

            _seqBubble = new SpeechBubble(_seqLayer, _art, "L10Bubble");
            _seqLayer.gameObject.SetActive(false);
        }

        partial void SequenceOpen(MapOpenSource source, ref MapView view)
        {
            ResetL10Sequence();
            if (source != MapOpenSource.L10 || _seqLayer == null)
            {
                return;
            }

            _seqOpen = true;
            _seqActive = true;
            view = MapView.Basin; // 06 §2.3: la cuenca entera, con el estuario lleno

            // El dato de llegada de la parada 1 (08 §4.5: map.stop.estuario.dato_03), «Dato del río 1»; "" en release sin el visto
            // bueno del asesor. El bocadillo, solo con voz (se decide al empezar, con la casa ya colocada).
            RiverStop? first = _stops?.At(1);
            _seqFactKey = first?.ArrivalFact;
            _seqFactText = _seqFactKey != null ? Loc.Fact(_seqFactKey) : string.Empty;
            _seqBubbleText = UiText.MapL10Intro;

            _seqLayer.gameObject.SetActive(true);
            _seqLayer.SetAsLastSibling(); // encima de todo lo del Mapa: el toque salta la secuencia
            _seqSkip!.gameObject.SetActive(true);
        }

        partial void SequenceSpace(MapSpace space)
        {
            _seqPlaceDirty = true;
        }

        partial void SequenceLayout()
        {
            _seqPlaceDirty = true;
        }

        partial void SequenceTick(float dtMs, ref bool animating)
        {
            if (_seqOutMs >= 0f)
            {
                animating = true;
                _seqOutMs += dtMs;
                float a = 1f - Mathf.Clamp01(_seqOutMs / L10OutFadeMs);
                SetL10Alpha(a);
                if (_seqHomeGrown)
                {
                    SetL10HomeScale(Mathf.Lerp(1f, L10HomeScale, a)); // la casa vuelve a su tamaño con el fundido
                }

                _seqDim = Mathf.Min(_seqDim, a);
                ApplyL10Dim();
                if (a <= 0f)
                {
                    HideL10Pieces();
                }
            }

            if (!_seqActive)
            {
                return;
            }

            animating = true;
            if (!_seqStarted)
            {
                StartL10Sequence();
            }
            else
            {
                _seqMs += dtMs;
            }

            if (_seqPlaceDirty)
            {
                PlaceL10Pieces();
            }

            bool reduce = ProgressReduceMotion;

            // La figura de la casa crece sobre su pie con un rebote al entrar (con Reducir movimiento, ya a su tamaño).
            if (_seqHomeGrown)
            {
                float grow = reduce || _seqMs >= L10HomeGrowMs ? 1f : Ease.OutBack(Mathf.Clamp01(_seqMs / L10HomeGrowMs));
                SetL10HomeScale(Mathf.LerpUnclamped(1f, L10HomeScale, grow));
                FaceL10Sloth(_seqFacing); // sin cambios, no toca nada
            }

            // Mientras TiTino se presenta, «ponerse al día» espera quieto en Belém: su reloj vuelve a cero y la canoa a su sitio
            // (en un fotograma no llega a encender una boya: 100 ms como mucho frente a 150 por boya).
            if (_seqMs < _seqTripFromMs && _catchUp)
            {
                _catchMs = 0f;
                MoveCanoe(_catchFrom);
            }

            // El bocadillo: entra, se queda lo que pide su lectura y se va antes de que salga la canoa.
            if (_seqWithBubble && _seqBubble != null)
            {
                float a = 0f;
                if (_seqMs < _seqTripFromMs)
                {
                    float fadeIn = reduce ? 1f : Mathf.Clamp01(_seqMs / L10BubbleFadeMs);
                    float fadeOut = reduce ? 1f : Mathf.Clamp01((_seqTripFromMs - _seqMs) / L10BubbleFadeMs);
                    a = Mathf.Min(fadeIn, fadeOut);
                }

                _seqBubble.SetAlpha(a);
            }

            // La placa del dato, con la canoa: entra con su fundido y el retrato con su rebote (sloth_present provisional).
            if (_seqWithPlate && _seqPlate != null && _seqMs >= _seqTripFromMs)
            {
                float since = _seqMs - _seqTripFromMs;
                _seqPlateShownMs = since;
                if (!_seqPlate.gameObject.activeSelf)
                {
                    _seqPlate.gameObject.SetActive(true);
                }

                _seqPlateGroup!.alpha = reduce ? 1f : Mathf.Clamp01(since / L10PlateFadeMs);
                if (_seqPortrait != null && _seqPortrait.gameObject.activeSelf)
                {
                    float bounce = reduce || since >= ArrivalStrip.PresentMs
                        ? 1f
                        : Mathf.LerpUnclamped(0.8f, 1f, Ease.OutBack(Mathf.Clamp01(since / ArrivalStrip.PresentMs)));
                    _seqPortrait.rectTransform.localScale = new Vector3(bounce, bounce, 1f);
                }
            }

            ApplyL10Dim(); // las placas de mundo se van con la entrada de la placa del dato

            if (_seqMs >= _seqEndMs)
            {
                FinishL10Sequence(false, true);
            }
        }

        partial void SequenceLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (_seqActive && _seqSkip != null)
            {
                ids.Add("map_l10_skip");
                rects.Add((RectTransform)_seqSkip.transform);
            }
        }

        partial void SequenceClose()
        {
            if (_seqActive)
            {
                FinishL10Sequence(true, false); // cerrado a medias: como saltada («ponerse al día» ya se completó al cerrar)
            }

            HideL10Pieces();
        }

        // ============================================================================================ secuencia

        // Primer fotograma de la secuencia, con la vista ya colocada (la casa y la canoa en su sitio): decide el bocadillo, los
        // tiempos y el gesto de TiTino.
        private void StartL10Sequence()
        {
            _seqStarted = true;
            _seqMs = 0f;
            bool titino = _houseShown && _houseSloth != null;
            _seqHomeGrown = titino;
            FrameL10(); // TiTino (a su tamaño de la secuencia), Belém y Santarém a la vista: con la casa dentro, el bocadillo
            SetL10HomeScale(titino && ProgressReduceMotion ? L10HomeScale : 1f); // con movimiento, crece con su rebote
            _seqDim = 1f; // «Norte arriba» fuera y los rótulos que pisa la secuencia, atenuados (se deciden al colocar)
            if (_catchUp)
            {
                MoveCanoe(_catchFrom); // la canoa, en Belém mientras TiTino se presenta (y medida ahí)
            }

            bool voice = L10SlothVoice == null || L10SlothVoice();
            _seqWithBubble = titino && voice && _homeInView && _seqBubbleText.Length > 0;
            _seqWithPlate = _seqFactText.Length > 0;

            // Tiempos [P]: el bocadillo lo que pide su lectura (o el gesto solo); después, la placa lo que pide la suya y, a la vez,
            // el viaje de la canoa, que acaba con un respiro. Todo dentro de L10MapMaxMs (06 §2.3: ≤ 10 s).
            int talk = _seqWithBubble ? NoticeTiming.ReadMs(_seqBubbleText) : (titino ? PresentOnlyMs : 0);
            int read = _seqWithPlate ? NoticeTiming.ReadMs(_seqFactText) : 0;
            int trip = _catchUp ? CatchUpDurationMs + L10TripTailMs : 0;
            _seqEndMs = Mathf.Min(MapConfig.L10MapMaxMs, talk + Mathf.Max(read, Mathf.Max(trip, L10TripTailMs)));
            _seqTripFromMs = Mathf.Min(talk, _seqEndMs);

            if (titino)
            {
                _houseSloth!.Play(SlothPoseId.Present, ProgressReduceMotion); // TiTino se presenta desde su casa
            }

            if (_seqWithBubble)
            {
                _seqBubble!.Show(_seqBubbleText, ProgressReduceMotion ? 1f : 0f);
                L10BubbleSaid?.Invoke(); // fuera del tope de frases, dentro de «dos pantallas seguidas» (D-028, pregunta 60 b)
            }

            if (_seqWithPlate)
            {
                _seqFact!.text = _seqFactText;
                _seqFact.fontSize = ArrivalStrip.FactSp * UiBuilder.TextScale;
                Sprite? portrait = _art.SlothPortrait(false);
                _seqPortrait!.sprite = portrait;
                _seqPortrait.gameObject.SetActive(portrait != null);
                _seqPortrait.rectTransform.localScale = Vector3.one;
                _seqPlateGroup!.alpha = 0f;
            }

            PlaceL10Pieces(); // AF_LAYOUT ya salió al abrir, con el área táctil que salta la secuencia
        }

        // Acaba la secuencia (una vez): la canoa llega (con un toque, en el acto), TiTino vuelve a su reposo, la sonda del dato, lo
        // visto del Mapa hasta el 10 y, si fade, el bocadillo y la placa se van con su fundido.
        private void FinishL10Sequence(bool skipped, bool fade)
        {
            if (!_seqActive)
            {
                return;
            }

            _seqActive = false;
            _seqDone = true;
            _seqSkipped = skipped;
            if (_seqSkip != null)
            {
                _seqSkip.gameObject.SetActive(false);
            }

            if (_catchUp)
            {
                CatchUpTouch(); // la canoa llega a Santarém en el acto (las llegadas que faltaban, con skipped)
            }

            if (_seqStarted && _houseShown && _houseSloth != null && !_asleep)
            {
                _houseSloth.Play(SlothPoseId.Idle, ProgressReduceMotion);
            }

            _seqFacing = 1f;
            FaceL10Sloth(1f); // mira otra vez como siempre

            ReportL10Fact();
            if (fade && !ProgressReduceMotion && (L10BubbleShown || L10FactShown))
            {
                _seqOutMs = 0f;
            }
            else
            {
                HideL10Pieces();
            }

            L10SequenceEnded?.Invoke();
            if (Visible)
            {
                EmitLayout(); // el área táctil que la saltaba ya no está
            }
        }

        // river_fact_shown (af_river_fact_shown, 08 §8.2) con el Dato del río 1, si la placa llegó a verse: dismissed_ms, lo que
        // estuvo a la vista.
        private void ReportL10Fact()
        {
            if (_seqFactReported || !_seqWithPlate || _seqFactKey == null || _seqMs < _seqTripFromMs)
            {
                return;
            }

            _seqFactReported = true;
            _probe.RiverFactShown(L10SequenceRule.Level, _seqFactKey, _seqPlateShownMs);
            UiAnalytics.RiverFactShown(Analytics, _seqFactKey, _seqPlateShownMs); // af_river_fact_shown del 10 (B7)
        }

        // Al abrir el Mapa: ninguna secuencia en marcha ni piezas a la vista.
        private void ResetL10Sequence()
        {
            _seqOpen = false;
            _seqActive = false;
            _seqStarted = false;
            _seqDone = false;
            _seqSkipped = false;
            _seqPlaceDirty = false;
            _seqMs = 0f;
            _seqTripFromMs = 0;
            _seqEndMs = 0;
            _seqWithBubble = false;
            _seqWithPlate = false;
            _seqPlateShownMs = 0f;
            _seqFactReported = false;
            _seqFactKey = null;
            _seqFactText = string.Empty;
            HideL10Pieces();
            _seqDestPlate = null;
            _seqDestIndex = -1;
        }

        // Fuera de la secuencia (o al acabar su fundido): sin bocadillo ni placa, la casa a su tamaño, los rótulos enteros y
        // «Norte arriba» a la vista.
        private void HideL10Pieces()
        {
            _seqOutMs = -1f;
            SetL10HomeScale(1f);
            _seqHomeGrown = false;
            _seqDim = 0f;
            ApplyL10Dim();
            RestoreL10Destination();
            _seqBubble?.Hide();
            if (_seqPlate != null)
            {
                _seqPlate.gameObject.SetActive(false);
            }

            if (_seqLayer != null && !_seqActive)
            {
                _seqLayer.gameObject.SetActive(false);
            }
        }

        // El fundido de salida: el bocadillo (si se veía) y la placa a la vez. Sin asignaciones.
        private void SetL10Alpha(float a)
        {
            if (_seqBubble != null && _seqBubble.Visible)
            {
                _seqBubble.SetAlpha(Mathf.Min(_seqBubble.Alpha, a));
            }

            if (_seqPlateGroup != null && _seqPlate!.gameObject.activeSelf)
            {
                _seqPlateGroup.alpha = Mathf.Min(_seqPlateGroup.alpha, a);
            }
        }

        // Coloca la placa (arriba de la zona del mapa) y el bocadillo (encima de TiTino, con el pico hacia su cabeza, dentro de la
        // zona del mapa). Mide los textos: solo al empezar y al recolocar.
        private void PlaceL10Pieces()
        {
            _seqPlaceDirty = false;
            if (_seqActive)
            {
                FrameL10();
            }

            float left = FrameInsetDp + FramePadDp;
            float top = _safeTop + HeaderDp + 8f + FramePadDp;
            if (_seqWithPlate && _seqPlate != null)
            {
                PlaceL10Plate(left + L10InsetDp, top + L10InsetDp, Mathf.Max(96f, _viewW - (2f * L10InsetDp)));
            }

            if (_seqHomeGrown)
            {
                // TiTino mira (y señala) hacia el estuario. «Norte arriba» recoloca la figura cuando cambia el encuadre (y la deja
                // mirando como siempre): se pone al día antes con el encuadre de la secuencia.
                ApplyNorthUp(false);
                _seqFacing = _homeLocal.x > _homeAt.x ? -1f : 1f;
                FaceL10Sloth(_seqFacing);
            }

            if (_seqWithBubble && _seqBubble != null && _houseSloth != null)
            {
                // El bocadillo, medio cuerpo por encima del punto de TiTino en la rama (sobre la copa), con la figura ya a su tamaño
                // de la secuencia (aunque esté creciendo: el bocadillo no se mueve con el rebote). TiTino cuelga bajo la rama, así
                // que el pico sigue con un tallo hasta L10BubbleStemGapDp de su coronilla (revisión de M4.2).
                float now = _seqHomeK;
                float k = _seqHomeGrown ? L10HomeScale : 1f;
                SetL10HomeScale(k);
                Vector2 at = RootDp(_houseSloth.Root, Vector2.zero);
                float crown = L10SlothCrown(at.y, k);
                SetL10HomeScale(now);
                float tip = at.y - (_houseSloth.Size * k * 0.5f) - L10BubbleGapDp;
                _seqBubble.PlaceAbove(at.x, tip, left + L10InsetDp, left + _viewW - L10InsetDp, top + L10InsetDp, L10BubbleMaxWidthDp,
                    Mathf.Max(tip, crown - L10BubbleStemGapDp), L10BubbleStemDp);
            }

            if (_seqActive)
            {
                DecideL10Dim(top + L10InsetDp);
            }
        }

        // Lo alto de la cabeza de TiTino (dp del lienzo de la UI, y hacia abajo) con la figura a k (ya puesta): la caja de la pieza
        // de la cabeza del rig; sin ella, su sitio en el rig colgado (la cabeza, entre los brazos, bajo el punto de la rama).
        private float L10SlothCrown(float branchY, float k)
        {
            SlothRig rig = _houseSloth!;
            if (!ReferenceEquals(_seqSlothHeadOf, rig))
            {
                _seqSlothHeadOf = rig;
                _seqSlothHead = rig.Root.Find("Body/Head") as RectTransform;
            }

            return _seqSlothHead != null ? RootBox(_seqSlothHead).yMin : branchY + (rig.Size * k * 0.2f);
        }

        // TiTino mira hacia facing (±1) en su rama, sin moverse. Sin asignaciones.
        private void FaceL10Sloth(float facing)
        {
            SlothRig? rig = _houseSloth;
            if (rig == null || !_houseShown)
            {
                return;
            }

            Vector2 p = rig.Root.anchoredPosition;
            rig.Place(p.x, -p.y, facing);
        }

        // La figura de la casa (embaúba y TiTino) a k veces su tamaño sobre su pie: la capa de la casa escala alrededor de él. Sin
        // asignaciones.
        private void SetL10HomeScale(float k)
        {
            _seqHomeK = k;
            RectTransform? layer = _houseLayer;
            if (layer == null)
            {
                return;
            }

            var pivot = new Vector2(0f, 1f); // el de siempre (FillTopLeft): con escala 1, la casa no se mueve
            if (k != 1f)
            {
                Rect r = layer.rect;
                if (r.width > 0f && r.height > 0f)
                {
                    pivot = new Vector2(_homeLocal.x / r.width, 1f + (_homeLocal.y / r.height));
                }
            }

            if (layer.pivot != pivot)
            {
                layer.pivot = pivot; // estirada y sin sizeDelta: la caja no se mueve, solo el punto fijo de la escala
            }

            var scale = new Vector3(k, k, 1f);
            if (layer.localScale != scale)
            {
                layer.localScale = scale;
            }
        }

        // Decide (al empezar y al recolocar) qué rótulos del mapa atenúa la secuencia: un nombre de país o una placa de mundo que
        // pisa la casa crecida o la canoa con su mástil, sus banderas y el guía (donde está y donde llegará), el bocadillo o la
        // placa del dato, con L10DimMarginDp de margen, baja a L10DimAlpha; si le tapan más de L10DimHideCover, a 0. Lo que pisan
        // el bocadillo y la placa se atenúa solo mientras se ven (con su fundido: ApplyL10Dim), y las placas de mundo se van
        // además mientras se ve la placa del dato. La placa del mundo de destino no se atenúa nunca: si pisa la canoa, sube
        // (LiftL10Destination, sin pasar de viewTop). Los CanvasGroup se crean la primera vez; después, sin asignaciones.
        private void DecideL10Dim(float viewTop)
        {
            RestoreL10Destination(); // se decide otra vez desde su sitio de siempre
            _seqObstacleCount = 0;
            if (_seqHomeGrown && _houseTree != null)
            {
                // La casa, a su tamaño de la secuencia (aunque aún esté creciendo).
                float now = _seqHomeK;
                SetL10HomeScale(L10HomeScale);
                AddL10Obstacle(_houseTree.rectTransform);
                SetL10HomeScale(now);
            }

            int canoeFrom = _seqObstacleCount;
            int canoeTo = canoeFrom;
            if (_hasCanoe)
            {
                AddL10Obstacle(_canoe.rectTransform);
                if (_flagCount > 0 && _mast != null)
                {
                    AddL10Obstacle(_mast.rectTransform);
                    for (int i = 0; i < _flagCount && i < _flagImages.Length; i++)
                    {
                        if (_flagImages[i] != null)
                        {
                            AddL10Obstacle(_flagImages[i]!.rectTransform);
                        }
                    }
                }

                canoeTo = _seqObstacleCount; // la canoa con su mástil y sus banderas donde está ahora (en Belém al empezar)
                if (_guide != null && _guide.gameObject.activeInHierarchy)
                {
                    AddL10Obstacle(_guide.rectTransform);
                }

                // Donde llegará: la canoa con su mástil y sus banderas, y el guía, con max_level (coordenadas del contenido).
                var layer = (RectTransform)_canoe.rectTransform.parent;
                AddL10Obstacle(layer, _gearBox);
                AddL10Obstacle(layer, _guideBox);
            }

            int baseCount = _seqObstacleCount;
            if (_seqWithBubble && _seqBubble != null)
            {
                AddL10Obstacle(_seqBubble.Rect, true); // ya colocado, aunque aún no se vea
                AddL10Obstacle(_seqBubble.Tail, true);
                if (_seqBubble.StemShown)
                {
                    AddL10Obstacle(_seqBubble.Stem, true); // el tallo del pico, hasta TiTino
                }
            }

            int bubbleEnd = _seqObstacleCount;
            if (_seqWithPlate && _seqPlate != null)
            {
                AddL10Obstacle(_seqPlate, true); // ya colocada, aunque no salga hasta que parta la canoa
            }

            int plateEnd = _seqObstacleCount;
            LiftL10Destination(canoeFrom, canoeTo, baseCount, bubbleEnd, viewTop);
            _seqDestIndex = -1;
            int countries = _countries.Count;
            int n = countries + _silhouettes.Count;
            if (_seqDimGroups.Length < n)
            {
                Array.Resize(ref _seqDimGroups, n);
                Array.Resize(ref _seqDimTarget, n);
                Array.Resize(ref _seqDimBubble, n);
                Array.Resize(ref _seqDimPlate, n);
            }

            _seqDimCount = n;
            for (int i = 0; i < n; i++)
            {
                RectTransform rt = i < countries ? _countries[i].rectTransform : _silhouettes[i - countries].Root;
                if (_seqDimGroups[i] == null || _seqDimGroups[i]!.transform != rt)
                {
                    CanvasGroup? group = rt.GetComponent<CanvasGroup>();
                    if (group == null)
                    {
                        group = rt.gameObject.AddComponent<CanvasGroup>();
                        group.blocksRaycasts = false;
                        group.interactable = false;
                    }

                    _seqDimGroups[i] = group;
                }

                if (!rt.gameObject.activeInHierarchy || ReferenceEquals(rt, _seqDestPlate))
                {
                    _seqDimTarget[i] = 1f;
                    _seqDimBubble[i] = 1f;
                    _seqDimPlate[i] = 1f;
                    if (rt.gameObject.activeInHierarchy)
                    {
                        _seqDestIndex = i; // la placa del mundo de destino: entera durante toda la secuencia
                    }

                    continue;
                }

                Rect box = RootBox(rt);
                _seqDimTarget[i] = L10DimFor(box, baseCount, baseCount, baseCount);
                _seqDimBubble[i] = L10DimFor(box, baseCount, baseCount, bubbleEnd);
                _seqDimPlate[i] = L10DimFor(box, baseCount, bubbleEnd, plateEnd);
            }

            if (_seqNorthUpGroup == null && _northUpBuilt)
            {
                GameObject button = _northUpButton.Button.gameObject;
                CanvasGroup? existing = button.GetComponent<CanvasGroup>();
                _seqNorthUpGroup = existing != null ? existing : button.AddComponent<CanvasGroup>();
            }

            ApplyL10Dim();
        }

        // La opacidad de un rótulo con la caja box (dp del lienzo de la UI) frente a las piezas de siempre (las baseCount primeras)
        // y las de [from, to): 1 si no pisa ninguna; L10DimAlpha si lo pisan un poco; 0 si le tapan más de L10DimHideCover (suma
        // de lo que tapa cada pieza, con su margen), y también 0 si el bocadillo o la placa, opacos, lo pisan de verdad.
        private float L10DimFor(Rect box, int baseCount, int from, int to)
        {
            float area = box.width * box.height;
            if (area <= 0f)
            {
                return 1f;
            }

            float covered = 0f;
            for (int i = 0; i < to; i++)
            {
                if (i >= baseCount && i < from)
                {
                    continue;
                }

                Rect o = _seqObstacles[i];
                float w = Mathf.Min(box.xMax, o.xMax + L10DimMarginDp) - Mathf.Max(box.xMin, o.xMin - L10DimMarginDp);
                float h = Mathf.Min(box.yMax, o.yMax + L10DimMarginDp) - Mathf.Max(box.yMin, o.yMin - L10DimMarginDp);
                if (w > 0f && h > 0f)
                {
                    covered += w * h;
                    if (i >= baseCount && box.Overlaps(o))
                    {
                        return 0f; // el bocadillo o la placa (opacos, encima) lo tapan de verdad: nunca a medio tapar
                    }
                }
            }

            if (_seqDestMoved)
            {
                // La placa del mundo de destino, subida (LiftL10Destination): como la casa y la canoa.
                Rect o = _seqDestBox;
                float w = Mathf.Min(box.xMax, o.xMax + L10DimMarginDp) - Mathf.Max(box.xMin, o.xMin - L10DimMarginDp);
                float h = Mathf.Min(box.yMax, o.yMax + L10DimMarginDp) - Mathf.Max(box.yMin, o.yMin - L10DimMarginDp);
                if (w > 0f && h > 0f)
                {
                    covered += w * h;
                }
            }

            if (covered <= 0f)
            {
                return 1f;
            }

            return covered / area > L10DimHideCover ? 0f : L10DimAlpha;
        }

        // Aplica el atenuado (_seqDim: 0 nada, 1 entero): los rótulos que pisan la casa y la canoa; los que pisan el bocadillo y
        // la placa del dato, con la opacidad de cada uno; las placas de mundo fuera mientras se ve la placa del dato, y «Norte
        // arriba» oculto y sin toques. Sin asignaciones.
        private void ApplyL10Dim()
        {
            float plate = _seqPlate != null && _seqPlate.gameObject.activeSelf ? _seqPlateGroup!.alpha : 0f;
            float bubble = _seqBubble != null && _seqBubble.Visible ? _seqBubble.Alpha : 0f;
            int countries = _countries.Count;
            for (int i = 0; i < _seqDimCount && i < _seqDimGroups.Length; i++)
            {
                CanvasGroup? group = _seqDimGroups[i];
                if (group == null)
                {
                    continue;
                }

                float rest = _seqDimTarget[i];
                float target = Mathf.Min(Mathf.Lerp(rest, _seqDimBubble[i], bubble), Mathf.Lerp(rest, _seqDimPlate[i], plate));
                if (i >= countries && (i != _seqDestIndex || !L10DestinationLive(i - countries)))
                {
                    target = Mathf.Lerp(target, 0f, plate); // placa de mundo: fuera con la placa del dato (salvo la del destino)
                }

                float alpha = Mathf.Lerp(1f, target, _seqDim);
                if (group.alpha != alpha)
                {
                    group.alpha = alpha;
                }
            }

            if (_seqNorthUpGroup != null)
            {
                float alpha = 1f - _seqDim;
                bool on = _seqDim <= 0f;
                if (_seqNorthUpGroup.alpha != alpha)
                {
                    _seqNorthUpGroup.alpha = alpha;
                }

                if (_seqNorthUpGroup.interactable != on)
                {
                    _seqNorthUpGroup.interactable = on;
                    _seqNorthUpGroup.blocksRaycasts = on;
                }
            }
        }

        // La placa del mundo de destino (el del nivel 11: «Jugar · Nivel 11» y el viaje de la canoa) en esta secuencia. Si su caja
        // corta la de la canoa con su mástil y sus banderas donde están ahora (las piezas [from, to) de _seqObstacles) o el
        // bocadillo de TiTino (las piezas [bubbleFrom, bubbleTo); arreglo de la fusión de la ola 6: desde FX7 la placa del mundo 2
        // se aparta del lleno y quedaba bajo el bocadillo), con L10DimMarginDp de margen, sube hasta quedar L10DestinationClearDp
        // por encima de lo más alto de lo que pisa, sin que su borde de arriba pase de viewTop. Los rótulos que pisa ahí se
        // atenúan como los de la casa y la canoa (L10DimFor, con _seqDestBox). Sin asignaciones.
        private void LiftL10Destination(int from, int to, int bubbleFrom, int bubbleTo, float viewTop)
        {
            _seqDestPlate = null;
            int world = WorldTable.WorldOf(L10SequenceRule.Level + 1);
            for (int i = 0; i < _silhouettes.Count; i++)
            {
                if (_silhouettes[i].World == world && _silhouettes[i].Root.gameObject.activeInHierarchy)
                {
                    _seqDestPlate = _silhouettes[i].Root;
                    break;
                }
            }

            RectTransform? plate = _seqDestPlate;
            if (plate == null || (from >= to && bubbleFrom >= bubbleTo))
            {
                return;
            }

            Rect box = RootBox(plate);
            float localH = plate.rect.height;
            if (localH <= 0f || box.height <= 0f)
            {
                return;
            }

            // Sube por encima de lo que pisa; si ahí pisa otra pieza (la canoa sobre el bocadillo o al revés), otra vez, por encima
            // de ella (como mucho una vez por pieza).
            float gearTop = float.MaxValue;
            for (int i = from; i < to; i++)
            {
                gearTop = Mathf.Min(gearTop, _seqObstacles[i].yMin);
            }

            float top = box.yMin;
            bool moved = false;
            for (int round = 0; round <= (to - from) + (bubbleTo - bubbleFrom); round++)
            {
                var at = new Rect(box.xMin, top, box.width, box.height);
                float hitTop = float.MaxValue;
                if (L10Hits(at, from, to))
                {
                    hitTop = gearTop; // la canoa con todo su equipo, como antes
                }

                for (int i = bubbleFrom; i < bubbleTo; i++)
                {
                    if (L10Hits(at, i, i + 1))
                    {
                        hitTop = Mathf.Min(hitTop, _seqObstacles[i].yMin);
                    }
                }

                if (hitTop == float.MaxValue)
                {
                    break;
                }

                float next = Mathf.Max(viewTop, hitTop - L10DestinationClearDp - box.height);
                if (next >= top - 0.01f)
                {
                    break; // no puede subir más (viewTop)
                }

                top = next;
                moved = true;
            }

            float dy = top - box.yMin; // en dp de la UI, y hacia abajo: < 0 sube
            if (!moved || dy >= -0.01f)
            {
                return;
            }

            _seqDestBase = plate.anchoredPosition;
            _seqDestShifted = _seqDestBase + new Vector2(0f, -dy * localH / box.height); // en el contenido, y hacia arriba
            plate.anchoredPosition = _seqDestShifted;
            _seqDestBox = RootBox(plate);
            _seqDestMoved = true;
        }

        // La caja at corta alguna de las piezas [from, to) de _seqObstacles, con L10DimMarginDp de margen. Sin asignaciones.
        private bool L10Hits(Rect at, int from, int to)
        {
            for (int i = from; i < to; i++)
            {
                Rect o = _seqObstacles[i];
                var grown = Rect.MinMaxRect(o.xMin - L10DimMarginDp, o.yMin - L10DimMarginDp, o.xMax + L10DimMarginDp, o.yMax + L10DimMarginDp);
                if (at.Overlaps(grown))
                {
                    return true;
                }
            }

            return false;
        }

        // La silueta k sigue siendo la del mundo de destino: «ponerse al día» rehace las siluetas en cada boya
        // (ShowProgressAt) y, al llegar la canoa a Santarém, la del mundo 2 deja de serlo (ya no es futuro) y su placa pasa a otro
        // mundo; desde entonces se trata como las demás. Sin asignaciones.
        private bool L10DestinationLive(int k)
        {
            return _seqDestIndex >= 0 && _seqDestPlate != null && k >= 0 && k < _silhouettes.Count
                && ReferenceEquals(_silhouettes[k].Root, _seqDestPlate) && _silhouettes[k].Root.gameObject.activeInHierarchy
                && _silhouettes[k].World == WorldTable.WorldOf(L10SequenceRule.Level + 1);
        }

        // La placa del mundo de destino vuelve a su sitio de siempre, si la subió la secuencia y nadie la recolocó después.
        private void RestoreL10Destination()
        {
            if (!_seqDestMoved)
            {
                return;
            }

            _seqDestMoved = false;
            RectTransform? plate = _seqDestPlate;
            if (plate != null && (plate.anchoredPosition - _seqDestShifted).sqrMagnitude < 1e-6f)
            {
                plate.anchoredPosition = _seqDestBase;
            }
        }

        // La caja de rt como pieza de la secuencia: si está a la vista o, con evenHidden, aunque aún no se vea (ya colocada).
        private void AddL10Obstacle(RectTransform rt, bool evenHidden = false)
        {
            if (_seqObstacleCount < _seqObstacles.Length && (evenHidden || rt.gameObject.activeInHierarchy))
            {
                _seqObstacles[_seqObstacleCount++] = RootBox(rt);
            }
        }

        // Una caja en coordenadas locales de layer (dp, y hacia arriba, desde su esquina de arriba a la izquierda), al lienzo de la UI.
        private void AddL10Obstacle(RectTransform layer, Rect local)
        {
            if (_seqObstacleCount >= _seqObstacles.Length || local.width <= 0f || local.height <= 0f)
            {
                return;
            }

            Rect r = layer.rect;
            float x0 = float.MaxValue;
            float x1 = float.MinValue;
            float y0 = float.MaxValue;
            float y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                float lx = (i & 1) == 0 ? local.xMin : local.xMax;
                float ly = (i & 2) == 0 ? local.yMin : local.yMax;
                Vector3 world = layer.TransformPoint(new Vector3(r.xMin + lx, r.yMax + ly, 0f));
                Vector2 p = WorldToRootDp(world);
                x0 = Mathf.Min(x0, p.x);
                x1 = Mathf.Max(x1, p.x);
                y0 = Mathf.Min(y0, p.y);
                y1 = Mathf.Max(y1, p.y);
            }

            _seqObstacles[_seqObstacleCount++] = Rect.MinMaxRect(x0, y0, x1, y1);
        }

        // La caja de rt en dp del lienzo de la UI (arriba a la izquierda, y hacia abajo). Sin asignaciones.
        private Rect RootBox(RectTransform rt)
        {
            rt.GetWorldCorners(_seqCorners);
            float x0 = float.MaxValue;
            float x1 = float.MinValue;
            float y0 = float.MaxValue;
            float y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = WorldToRootDp(_seqCorners[i]);
                x0 = Mathf.Min(x0, p.x);
                x1 = Mathf.Max(x1, p.x);
                y0 = Mathf.Min(y0, p.y);
                y1 = Mathf.Max(y1, p.y);
            }

            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        private Vector2 WorldToRootDp(Vector3 world)
        {
            Vector3 p = _root.InverseTransformPoint(world);
            Rect r = _root.rect;
            return new Vector2(p.x - r.xMin, r.yMax - p.y);
        }

        // El encuadre de la secuencia en la Cuenca: centrado en la caja de la casa de TiTino (del pie a lo alto del árbol), Belém
        // (donde sale la canoa) y Santarém (donde llega), para que en un teléfono bajo (360 × 640 dp, donde la cuenca a 1× no cabe
        // entera) se vean TiTino, su bocadillo y el viaje. Sin asignaciones; la ventanita de casa se decide otra vez.
        private void FrameL10()
        {
            RiverMap? map = _map;
            RiverRoutes? routes = _routes;
            if (map == null || routes == null || !_markersReady)
            {
                return;
            }

            MapSpace space = _space;
            Vector2 min = _homeLocal;
            Vector2 max = _homeLocal + new Vector2(0f, _homeHeight * (_seqHomeGrown ? L10HomeScale : 1f));
            if (TryCanoePoint(map, routes, _catchFrom, out Vector2 from))
            {
                GrowL10Box(space.ToLocal(from), ref min, ref max);
            }

            if (TryCanoePoint(map, routes, L10SequenceRule.Level, out Vector2 to))
            {
                GrowL10Box(space.ToLocal(to), ref min, ref max);
            }

            Vector2 c = (min + max) * 0.5f;
            SetOffset(new Vector2((_viewW * 0.5f) - c.x, (_viewH * 0.5f) + c.y));
            UpdateHomeWindow();
        }

        private static void GrowL10Box(Vector2 p, ref Vector2 min, ref Vector2 max)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        // La placa del Dato del río (la de la franja de llegada y la del inicio): el retrato a la izquierda y el dato a su derecha,
        // centrado en el alto del retrato si es más bajo; crece hacia abajo con el texto (02 §12).
        private void PlaceL10Plate(float x, float y, float width)
        {
            const float pad = ArrivalStrip.PlatePadDp;
            bool portrait = _seqPortrait != null && _seqPortrait.gameObject.activeSelf;
            float portraitW = portrait ? ArrivalStrip.PortraitDp + ArrivalStrip.PortraitGapDp : 0f;
            float textW = Mathf.Max(24f, width - pad - portraitW - ArrivalStrip.PlateRightPadDp);
            float textH = Mathf.Ceil(_seqFact!.GetPreferredValues(_seqFact.text, textW, float.PositiveInfinity).y);
            float contentH = Mathf.Max(portrait ? ArrivalStrip.PortraitDp : 0f, textH);
            UiBuilder.Place(_seqPlate!, x, y, width, contentH + (2f * pad));
            if (portrait)
            {
                RectTransform rt = _seqPortrait!.rectTransform;
                UiBuilder.Place(rt, pad, pad, ArrivalStrip.PortraitDp, ArrivalStrip.PortraitDp);
                rt.pivot = new Vector2(0.5f, 0.5f); // el rebote, desde su centro
                rt.anchoredPosition = new Vector2(pad + (ArrivalStrip.PortraitDp * 0.5f), -(pad + (ArrivalStrip.PortraitDp * 0.5f)));
            }

            float textTop = pad + Mathf.Floor((contentH - textH) * 0.5f);
            UiBuilder.Place(_seqFact.rectTransform, pad + portraitW, textTop, textW, textH);
        }

        // Un punto de rt (local) en dp del lienzo de la UI del Mapa, arriba a la izquierda y con la y hacia abajo.
        private Vector2 RootDp(RectTransform rt, Vector2 local)
        {
            Vector3 world = rt.TransformPoint(local);
            Vector3 p = _root.InverseTransformPoint(world);
            Rect r = _root.rect;
            return new Vector2(p.x - r.xMin, r.yMax - p.y);
        }
    }
}
