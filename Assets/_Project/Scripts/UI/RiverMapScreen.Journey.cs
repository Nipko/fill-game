#nullable enable
using AF.Levels;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El Mapa que se abre con viaje (D-053, petición del dueño del 2026-10-26; 06 §2.3 y §4.5): «cuando veo el mapa, luego de
    /// pasar los 10 niveles, pareciera que no pasa nada, pero está el botón para seguir de nivel y no deja ver la animación del
    /// Capi subiendo el río». Parcial de <see cref="RiverMapScreen"/> sobre sus ganchos <c>Journey*</c>. Vale para los Mapas que
    /// se abren con el viaje de la canoa por el tramo recién ganado (<see cref="JourneySource"/>): el del 10
    /// (<see cref="MapOpenSource.L10"/>, RiverMapScreen.Sequence.cs), «Ver mapa» en la victoria del <c>10k</c>
    /// (<see cref="MapOpenSource.Victory"/>, que repite el tramo aunque la franja ya anotara la llegada, RiverMapScreen.Modes.cs)
    /// y el fin de contenido (<see cref="MapOpenSource.ContentEnd"/>). El Mapa abierto desde la pestaña, la pausa o el banner
    /// sigue como siempre: allí un toque completa «ponerse al día».
    /// <list type="bullet">
    /// <item><description><b>Sin tiempo muerto:</b> desde el primer fotograma la vista ya tiene la canoa de Capi (con Rosa o el
    /// bufeo delante) y el tramo (en la victoria, si el principio del tramo queda fuera, se corre lo justo para que se vean los
    /// dos extremos), Capi rema (<c>sfx_map_paddle</c>), la canoa se balancea (<see cref="JourneySwayDeg"/>°) y la mini cuia de
    /// la parada a la que va late (<see cref="JourneyPulseScale"/>); tras <see cref="JourneyLeadMs"/> ms de anticipación, la
    /// canoa sale (antes, en el Mapa del 10, esperaba a que TiTino acabara su bocadillo: unos 3,2 s quieta).</description></item>
    /// <item><description><b>«Jugar» (o «Volver») y «←» no están hasta que la canoa llega</b> a la parada del tramo: ocultos
    /// y sin responder durante el viaje (ni en <c>AF_LAYOUT</c>), y al llegar entran con un pequeño rebote
    /// (<see cref="JourneyButtonInMs"/> ms), suena la muestra que se llena (<c>sfx_map_sample</c>), TalkBack anuncia la llegada y
    /// la sonda escribe <c>AF_STATE map_play_ready</c>.</description></item>
    /// <item><description><b>El toque no salta el viaje:</b> un área sobre toda la pantalla (<c>map_journey</c> en
    /// <c>AF_LAYOUT</c>) recibe los toques mientras dura; como mucho lo acelera (<see cref="JourneyTapSpeed"/>×; en la
    /// anticipación, la canoa sale ya). Tampoco se arrastra el mapa ni se tocan sus controles: la cámara se queda en la
    /// canoa.</description></item>
    /// <item><description><b>Atrás de Android</b> durante el viaje cierra el Mapa como siempre (las llegadas que faltaban, como
    /// saltadas, y lo visto anotado): Atrás nunca se bloquea ni obliga a esperar, y nadie lo pulsa sin querer. <b>Reducir
    /// movimiento:</b> sin viaje (el fundido de «ponerse al día»), así que los botones están desde el principio. <b>Segundo
    /// plano:</b> al volver, el viaje salta al final (02 §14) y los botones entran.</description></item>
    /// </list>
    /// Las piezas se crean una vez; por fotograma, solo una rotación, una escala y una opacidad, sin asignaciones.
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Anticipación antes de que la canoa salga (ms) [P]: el viaje empieza a moverse en ≤ 300 ms (D-053).</summary>
        public const int JourneyLeadMs = 250;

        /// <summary>Un toque durante el viaje lo acelera a este ritmo (×) [P]; nunca lo salta (D-053).</summary>
        public const float JourneyTapSpeed = 2f;

        /// <summary>Entrada de «Jugar» y «←» al llegar la canoa (ms) [P].</summary>
        public const float JourneyButtonInMs = 250f;

        /// <summary>Escala de la que parten «Jugar» y «←» en su entrada [P].</summary>
        public const float JourneyButtonFromScale = 0.85f;

        /// <summary>Balanceo de la canoa durante el viaje (grados, a cada lado) [P].</summary>
        public const float JourneySwayDeg = 4f;

        /// <summary>Periodo del balanceo de la canoa (ms) [P].</summary>
        public const float JourneySwayPeriodMs = 700f;

        /// <summary>Lo que crece la mini cuia de la parada de destino en su latido (fracción) [P].</summary>
        public const float JourneyPulseScale = 0.18f;

        /// <summary>Periodo del latido de la mini cuia de destino (ms) [P].</summary>
        public const float JourneyPulsePeriodMs = 600f;

        /// <summary>Margen de la vista para decidir si los dos extremos del tramo se ven al abrir (dp) [P].</summary>
        public const float JourneyFrameMarginDp = 24f;

        private Button? _journeyTouch;
        private CanvasGroup? _playGroup;
        private CanvasGroup? _backGroup;
        private bool _journeyLock;
        private bool _journeyOpen;
        private float _journeyMs;
        private float _journeyInMs = -1f;
        private float _journeyFirstMoveMs = -1f;
        private float _journeyReadyMs = -1f;
        private int _journeyTaps;
        private int _journeyPulse = -1;
        private string _journeyBy = string.Empty;
        private int _playOrder = -1;
        private int _headerOrder = -1;

        /// <summary>El lector de pantalla (TalkBack) para anunciar la llegada (D-053); lo pone <see cref="GameBootstrap"/>.</summary>
        internal A11yService? A11y { get; set; }

        /// <summary>
        /// El viaje está en marcha y bloquea «Jugar» y «←» (D-053): los toques solo lo aceleran. False sin viaje o ya llegado.
        /// </summary>
        public bool JourneyLocked => _journeyLock;

        /// <summary>Esta apertura del Mapa fue con viaje (aunque ya haya llegado).</summary>
        public bool JourneyOpened => _journeyOpen;

        /// <summary>Cuándo empezó a moverse la canoa (ms desde que se abrió el Mapa); −1 si aún no (o no hubo viaje).</summary>
        public float JourneyFirstMoveMs => _journeyFirstMoveMs;

        /// <summary>Cuándo se habilitaron «Jugar» y «←» (ms desde que se abrió el Mapa); −1 si aún no (o no hubo viaje).</summary>
        public float JourneyReadyMs => _journeyReadyMs;

        /// <summary>Toques que recibió el viaje (lo aceleraron).</summary>
        public int JourneyTaps => _journeyTaps;

        /// <summary>Cómo acabó el viaje: <c>time</c>, <c>touch</c>, <c>close</c> o <c>resume</c> ("" sin viaje o en marcha).</summary>
        public string JourneyEndedBy => _journeyBy;

        /// <summary>El área que recibe los toques durante el viaje (<c>map_journey</c>).</summary>
        public Button? JourneyTouchArea => _journeyTouch;

        /// <summary>«Jugar» (o «Volver») se ve y responde.</summary>
        public bool PlayShown => _play.Button.gameObject.activeSelf && !_journeyLock;

        /// <summary>«←» se ve y responde.</summary>
        public bool BackShown => _back.Button.gameObject.activeSelf && !_journeyLock;

        /// <summary>Opacidad de «Jugar» (su entrada al llegar la canoa).</summary>
        public float PlayAlpha => _play.Button.gameObject.activeSelf ? (_playGroup != null ? _playGroup.alpha : 1f) : 0f;

        /// <summary>
        /// La entrada <paramref name="source"/> abre el Mapa con el viaje del tramo, que bloquea sus botones hasta que la canoa
        /// llega (D-053): el Mapa del 10, «Ver mapa» de la victoria y el fin de contenido.
        /// </summary>
        public static bool JourneySource(MapOpenSource source)
        {
            return source == MapOpenSource.L10 || source == MapOpenSource.Victory || source == MapOpenSource.ContentEnd;
        }

        /// <summary>
        /// Un toque durante el viaje (el área <c>map_journey</c>): no lo salta; en la anticipación la canoa sale ya y, remando,
        /// acelera a <see cref="JourneyTapSpeed"/>×. False sin viaje en marcha.
        /// </summary>
        public bool TapJourney()
        {
            if (!_journeyLock || !Visible)
            {
                return false;
            }

            _journeyTaps++;
            _catchHoldMs = 0f;
            _catchSpeed = JourneyTapSpeed;
            return true;
        }

        /// <summary>
        /// La app vuelve de segundo plano (02 §14): con el viaje (o «ponerse al día») en marcha, salta al final y, si el Mapa
        /// tenía viaje, entran sus botones. True si había algo que completar.
        /// </summary>
        public bool ResumeFromBackground()
        {
            return Visible && CatchUpResume();
        }

        // ============================================================================================ ganchos

        partial void JourneyBuild(RectTransform root)
        {
            _journeyTouch = UiBuilder.BareButton("JourneyTouch", root, () => TapJourney());
            UiBuilder.Stretch((RectTransform)_journeyTouch.transform);
            A11yLabels.Set(_journeyTouch, () => A11yText.MapJourney); // TalkBack: qué pasa y qué hace el toque (D-053)
            _journeyTouch.gameObject.SetActive(false);
            _playGroup = Group(_play.Button.gameObject);
            _backGroup = Group(_back.Button.gameObject);
        }

        partial void JourneyOpen(MapOpenSource source)
        {
            _journeyLock = false;
            _journeyOpen = false;
            _journeyMs = 0f;
            _journeyInMs = -1f;
            _journeyFirstMoveMs = -1f;
            _journeyReadyMs = -1f;
            _journeyTaps = 0;
            _journeyPulse = -1;
            _journeyBy = string.Empty;
            RestoreJourneyOrder();
            _play.Button.gameObject.SetActive(true); // defensa: un viaje anterior que no acabó bien no deja los botones ocultos
            _back.Button.gameObject.SetActive(true);
            ShowJourneyButtons(1f);
            if (_journeyTouch != null)
            {
                _journeyTouch.gameObject.SetActive(false);
            }

            if (!JourneySource(source) || !_catchUp)
            {
                return; // sin viaje (o con Reducir movimiento: el fundido): los botones, como siempre
            }

            _journeyOpen = true;
            _journeyLock = true;
            _catchHoldMs = JourneyLeadMs;
            _catchSpeed = 1f;
            _play.Button.gameObject.SetActive(false);
            _back.Button.gameObject.SetActive(false);
            if (_journeyTouch != null)
            {
                _journeyTouch.gameObject.SetActive(true);
                _journeyTouch.transform.SetAsLastSibling(); // encima de todo, también del área que salta el Mapa del 10
            }

            if (source == MapOpenSource.Victory)
            {
                FrameJourney();
            }

            int n = JourneyDestinationStop();
            _journeyPulse = n > 0 ? StopMarkerIndexOf(n) : -1;
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.MapPaddle); // Capi rema: algo va a pasar
        }

        partial void JourneyTick(float dtMs, ref bool animating)
        {
            if (_journeyLock)
            {
                animating = true;
                _journeyMs += dtMs;
                if (_journeyFirstMoveMs < 0f && _catchMs > 0f)
                {
                    _journeyFirstMoveMs = _journeyMs;
                }

                // La canoa se balancea sobre su punto (con «Norte arriba», sobre su vertical en pantalla) y la mini cuia de destino
                // late. Sin asignaciones.
                float sway = Mathf.Sin(_journeyMs * (2f * Mathf.PI / JourneySwayPeriodMs)) * JourneySwayDeg;
                _canoe.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _figureAngle + sway);
                SetJourneyPulse(1f + (JourneyPulseScale * 0.5f * (1f - Mathf.Cos(_journeyMs * (2f * Mathf.PI / JourneyPulsePeriodMs)))));
                return;
            }

            if (_journeyInMs >= 0f)
            {
                animating = true;
                _journeyInMs += dtMs;
                float t = Mathf.Clamp01(_journeyInMs / JourneyButtonInMs);
                ShowJourneyButtons(t);
                if (t >= 1f)
                {
                    _journeyInMs = -1f;
                }
            }
        }

        partial void JourneyClose()
        {
            _journeyLock = false;
            _journeyInMs = -1f;
            SetJourneyPulse(1f);
            _journeyPulse = -1;
            _play.Button.gameObject.SetActive(true);
            _back.Button.gameObject.SetActive(true);
            ShowJourneyButtons(1f);
            RestoreJourneyOrder();
            if (_journeyTouch != null)
            {
                _journeyTouch.gameObject.SetActive(false);
            }
        }

        // ============================================================================================ llegada

        // La canoa llegó (FinishCatchUp: por tiempo, con un toque del código, al cerrar o al volver de segundo plano): fuera el
        // bloqueo, la canoa derecha, la mini cuia a su tamaño y, salvo al cerrar, «Jugar» y «←» entran, suena la muestra, TalkBack
        // lo anuncia y la sonda escribe map_play_ready.
        private void JourneyArrived(string by)
        {
            if (!_journeyLock)
            {
                return;
            }

            _journeyLock = false;
            _journeyBy = by;
            _journeyReadyMs = _journeyMs;
            _canoe.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _figureAngle);
            SetJourneyPulse(1f);
            _journeyPulse = -1;
            if (_journeyTouch != null)
            {
                _journeyTouch.gameObject.SetActive(false);
            }

            _play.Button.gameObject.SetActive(true);
            _back.Button.gameObject.SetActive(true);

            // En el Mapa del 10 la canoa llega con el bocadillo aún a la vista y la placa del mundo 2 se va (deja de ser futuro):
            // los rótulos que pisa el bocadillo se vuelven a decidir con las placas en su sitio nuevo.
            if (_seqActive)
            {
                _seqPlaceDirty = true;
            }

            if (by == JourneyByClose)
            {
                ShowJourneyButtons(1f);
                return;
            }

            // Encima del área que salta lo que queda del Mapa del 10 (la placa del dato): «Jugar · Nivel 11» y «←» responden ya.
            // Solo el tiempo que dura esa secuencia: al cerrar (o al volver a abrir) vuelven a su sitio (RestoreJourneyOrder).
            if (_seqActive && _playOrder < 0)
            {
                _playOrder = _play.Button.transform.GetSiblingIndex();
                _headerOrder = _headerPlate.transform.GetSiblingIndex();
                _play.Button.transform.SetAsLastSibling();
                _headerPlate.transform.SetAsLastSibling();
            }

            bool reduce = ProgressReduceMotion;
            ShowJourneyButtons(reduce ? 1f : 0f);
            _journeyInMs = reduce ? -1f : 0f;
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.MapSample); // se llena la muestra del sitio: ya se puede seguir
            AnnounceJourneyArrival();
            int firstMove = _journeyFirstMoveMs >= 0f ? Mathf.RoundToInt(_journeyFirstMoveMs) : -1;
            _probe.MapPlayReady(_source, _catchTo, firstMove, Mathf.RoundToInt(_journeyReadyMs), _journeyTaps, by);
            if (Visible)
            {
                EmitLayout(); // «Jugar» y «←» vuelven a AF_LAYOUT; map_journey se va
            }
        }

        // TalkBack: la llegada («Santarém · Brasil · río Amazonas», si la parada tiene nombres) y el botón que ya se puede tocar.
        private void AnnounceJourneyArrival()
        {
            A11yService? a11y = A11y;
            if (a11y == null)
            {
                return;
            }

            a11y.Invalidate(); // «Jugar» y «←» vuelven al recorrido del lector
            if (!a11y.On)
            {
                return;
            }

            string text = _playLabel.text;
            RiverStop? stop = _catchTo % RiverStopCatalog.LevelsPerStop == 0 ? _stops?.At(_catchTo / RiverStopCatalog.LevelsPerStop) : null;
            if (stop != null)
            {
                string name = UiText.StopName(stop);
                string country = UiText.StopCountry(stop);
                string river = UiText.StopRiver(stop);
                if (name.Length > 0 && country.Length > 0 && river.Length > 0)
                {
                    text = A11yText.Append(UiText.MapArrived(name, country, river), text);
                }
            }

            a11y.Announce(text);
        }

        // ============================================================================================ piezas

        // La parada hacia la que va la canoa: la del 10k en que acaba el tramo o, si acaba entre dos, la siguiente.
        private int JourneyDestinationStop()
        {
            int to = _catchTo;
            if (to <= 0)
            {
                return -1;
            }

            return (to + RiverStopCatalog.LevelsPerStop - 1) / RiverStopCatalog.LevelsPerStop;
        }

        // «Ver mapa» de la victoria: la vista Mundo encuadra el mundo de «Jugar» y la canoa en la llegada; si el principio del
        // tramo (la parada anterior, a veces del mundo de antes) queda fuera, se corre lo justo para centrar los dos extremos.
        private void FrameJourney()
        {
            if (!_hasCanoe || _map == null || _routes == null)
            {
                return;
            }

            Vector2 a = _space.ToLocal(CanoeAt(_map, _routes, _catchFrom));
            Vector2 b = _space.ToLocal(_canoeMap);
            if (InView(a) && InView(b))
            {
                return;
            }

            SetOffset(new Vector2((_viewW * 0.5f) - ((a.x + b.x) * 0.5f), (_viewH * 0.5f) + ((a.y + b.y) * 0.5f)));
            PlatesDragged(); // otro encuadre: las placas que no cabían vuelven (FX10)
        }

        // Un punto del contenido (dp locales) cae en la vista con el margen JourneyFrameMarginDp.
        private bool InView(Vector2 local)
        {
            float x = _offset.x + local.x;
            float y = _offset.y - local.y;
            float m = JourneyFrameMarginDp;
            return x >= m && x <= _viewW - m && y >= m && y <= _viewH - m;
        }

        private void SetJourneyPulse(float k)
        {
            if (_journeyPulse < 0 || _journeyPulse >= _markerCount)
            {
                return;
            }

            _markers[_journeyPulse].Root.localScale = new Vector3(k, k, 1f);
        }

        // «Jugar» y «←» a t (0-1) de su entrada: opacidad y un pequeño rebote de la cara.
        private void ShowJourneyButtons(float t)
        {
            float alpha = Mathf.Clamp01(t);
            float k = t >= 1f ? 1f : Mathf.LerpUnclamped(JourneyButtonFromScale, 1f, Ease.OutBack(alpha));
            var scale = new Vector3(k, k, 1f);
            if (_playGroup != null)
            {
                _playGroup.alpha = alpha;
            }

            if (_backGroup != null)
            {
                _backGroup.alpha = alpha;
            }

            _play.Face.parent.localScale = scale;
            _back.Face.parent.localScale = scale;
        }

        // «Jugar» y la cabecera vuelven a su orden de construcción, si la llegada en el Mapa del 10 los subió (primero el que estaba
        // antes, para que los índices guardados sigan valiendo).
        private void RestoreJourneyOrder()
        {
            if (_playOrder < 0 && _headerOrder < 0)
            {
                return;
            }

            if (_headerOrder >= 0 && (_playOrder < 0 || _headerOrder < _playOrder))
            {
                _headerPlate.transform.SetSiblingIndex(_headerOrder);
                if (_playOrder >= 0)
                {
                    _play.Button.transform.SetSiblingIndex(_playOrder);
                }
            }
            else
            {
                _play.Button.transform.SetSiblingIndex(_playOrder);
                if (_headerOrder >= 0)
                {
                    _headerPlate.transform.SetSiblingIndex(_headerOrder);
                }
            }

            _playOrder = -1;
            _headerOrder = -1;
        }

        private static CanvasGroup Group(GameObject go)
        {
            CanvasGroup g = go.GetComponent<CanvasGroup>();
            return g != null ? g : go.AddComponent<CanvasGroup>();
        }
    }
}
