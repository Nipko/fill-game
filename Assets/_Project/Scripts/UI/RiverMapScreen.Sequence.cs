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
    /// <c>map_l10_intro</c> («Yo vivo aquí, donde el río entra al mar.» [P]; nunca una clave <c>sloth_line_*</c>), encima de él
    /// con el pico hacia él (<see cref="SpeechBubble"/>), lo que pide su lectura (<see cref="NoticeTiming.ReadMs(string)"/>). Sin
    /// voz, solo el gesto (<see cref="PresentOnlyMs"/>); sin su arte (release, P-M5), nada.</description></item>
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

        /// <summary>
        /// Lo llama la secuencia al acabar, al saltarla o al cerrar el Mapa a medias (una vez por secuencia): <see cref="GameBootstrap"/>
        /// anota lo visto del Mapa hasta el 10 (<c>MarkMapSeen(10)</c>).
        /// </summary>
        internal Action? L10SequenceEnded { get; set; }

        /// <summary>TiTino tiene voz (<c>ui_sloth_enabled</c> ∧ <c>settings.sloth_messages</c>); sin quien lo diga, sí.</summary>
        internal Func<bool>? L10SlothVoice { get; set; }

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
            FrameL10(); // TiTino, Belém y Santarém a la vista: con la casa dentro, el bocadillo
            bool titino = _houseShown && _houseSloth != null;
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
        }

        private void HideL10Pieces()
        {
            _seqOutMs = -1f;
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

            if (_seqWithBubble && _seqBubble != null && _houseSloth != null)
            {
                // La cabeza de TiTino: medio cuerpo por encima de su punto en la rama (el rig cuelga centrado en él).
                Vector2 at = RootDp(_houseSloth.Root, Vector2.zero);
                float head = _houseSloth.Size * 0.5f;
                _seqBubble.PlaceAbove(at.x, at.y - head - L10BubbleGapDp, left + L10InsetDp, left + _viewW - L10InsetDp, top + L10InsetDp,
                    L10BubbleMaxWidthDp);
            }
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
            Vector2 max = _homeLocal + new Vector2(0f, _homeHeight);
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
