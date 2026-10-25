#nullable enable
using System;
using AF.GameFlow;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// La travesía del inicio (D-050, punto 3; 06 §4.7, fila Lobby): la canoa de Capi con Rosa delante (las piezas del Mapa del
    /// río: <see cref="ProceduralArt.MapCanoe"/> y <see cref="ProceduralArt.Rosa"/>, o el bufeo en los mundos 7 y 8, Q18-A) que
    /// avanza por una ruta punteada hacia la <b>próxima parada</b>, un poste de madera clavado en el agua con su letrero: el nombre
    /// del sitio y, debajo, la píldora «Faltan {n} niveles» (<c>home_voyage_levels</c>). La canoa va en la fracción del tramo
    /// que ya se jugó (<see cref="ProgressOf"/>, como el Mapa: (n − 1) mod 10 / 10) y crece un poco al acercarse (perspectiva).
    /// <para>Tocar la canoa o la parada abre el Mapa del río (<c>af_map_open</c> con <c>source = lobby</c>, 08 §8.2), solo cuando
    /// el Mapa existe para el jugador (la pestaña del Mapa, desde <c>max_level</c> ≥ 10; D-008): antes, la travesía se ve pero no
    /// se toca. Las dos áreas táctiles miden ≥ 48 dp (07 §7.8) y TalkBack las lee con la línea entera de la parada
    /// (<c>home_voyage_a11y</c>).</para>
    /// <para>Sin parada siguiente (fin del catálogo o un nombre sin el visto bueno del asesor en release), sin letrero ni ruta: la
    /// canoa espera junto a la salida. Con Reducir movimiento, todo quieto. <see cref="Tick"/> no asigna memoria.</para>
    /// </summary>
    internal sealed class HomeVoyage
    {
        /// <summary>Puntos de la ruta.</summary>
        public const int RouteDots = 16;

        /// <summary>Escala de la canoa del Mapa en el inicio (40 × 32 dp × 1,9) junto a la parada; más lejos, más pequeña.</summary>
        public const float CanoeScale = 1.9f;

        /// <summary>Ancho de Rosa delante de la canoa (dp).</summary>
        public const float GuideDp = 40f;

        /// <summary>Cuerpo del nombre de la parada (sp), en Nunito Bold.</summary>
        public const float NameSp = 14f;

        /// <summary>Cuerpo de «Faltan {n} niveles» (sp), en Nunito Bold.</summary>
        public const float LevelsSp = 12f;

        /// <summary>Lado mínimo de un área táctil (07 §7.8).</summary>
        public const float MinTouchDp = 48f;

        private const float SignPadX = 10f;
        private const float SignPadY = 5f;
        private const float PillPadX = 8f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly Image[] _dots = new Image[RouteDots];
        private readonly Image _wakeA;
        private readonly Image _wakeB;
        private readonly Image _canoe;
        private readonly Image _guide;
        private readonly Image _postRing;
        private readonly Image _post;
        private readonly RectTransform _sign;
        private readonly TextMeshProUGUI _name;
        private readonly RectTransform _pill;
        private readonly TextMeshProUGUI _levels;
        private readonly Button _canoeTap;
        private readonly Button _stopTap;
        private readonly Vector2[] _dotPos = new Vector2[RouteDots];

        private Vector2 _start;
        private Vector2 _foot;
        private Vector2 _control;
        private float _k = 1f;
        private float _progress;
        private bool _hasNext;
        private bool _interactive;
        private bool _reduceMotion;
        private float _clockMs;
        private Vector2 _canoePos;
        private Vector2 _guidePos;
        private float _canoeW;
        private float _canoeH;
        private string _guideId = RiverJourney.GuideRosa;
        private string _sentence = string.Empty;
        private Rect _signRect;
        private bool _laidOut;

        /// <summary>Crea la travesía (oculta) bajo <paramref name="parent"/>; los dos toques llaman a <paramref name="onTap"/>.</summary>
        public HomeVoyage(Transform parent, ProceduralArt art, Action onTap)
        {
            _art = art;
            _root = UiBuilder.Rect("HomeVoyage", parent);
            UiBuilder.Stretch(_root);
            // Canvas anidado propio (como el río): el vaivén de la canoa y de Rosa no rehace el resto del inicio; con su raycaster,
            // para que la canoa y la parada se toquen.
            Canvas nested = _root.gameObject.AddComponent<Canvas>();
            nested.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            _root.gameObject.AddComponent<GraphicRaycaster>();
            for (int i = 0; i < RouteDots; i++)
            {
                _dots[i] = Piece("RouteDot" + i, art.Disc, new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.9f));
            }

            _postRing = Piece("PostRing", art.RiverRipple, new Color(1f, 1f, 1f, 0.7f));
            _post = Piece("StopPost", art.StopPost, Color.white);
            _post.rectTransform.pivot = new Vector2(0.5f, 0f);
            _wakeA = Piece("WakeA", art.RiverRipple, new Color(1f, 1f, 1f, 0.55f));
            _wakeB = Piece("WakeB", art.RiverRipple, new Color(1f, 1f, 1f, 0.35f));
            _canoe = Piece("Canoe", art.MapCanoe, Color.white);
            _canoe.preserveAspect = true;
            _guide = Piece("Guide", null, Color.white); // delante de la canoa, junto a la proa; Rosa o el bufeo, al enseñarla (Apply)
            _guide.preserveAspect = true;

            _sign = UiBuilder.Rect("StopSign", _root);
            Image board = UiBuilder.Sliced("Board", _sign, art.WoodPlate, Color.white);
            UiBuilder.Stretch(board.rectTransform);
            _name = UiBuilder.Label("Name", _sign, TextStyle.PlainBold, string.Empty, NameSp, Palette.Cream, TextAlignmentOptions.Center);
            _name.textWrappingMode = TextWrappingModes.Normal;
            _pill = UiBuilder.Rect("LevelsPill", _sign);
            Image pillEdge = UiBuilder.Sliced("Edge", _pill, art.RoundedRect, Palette.FamilyWood.Edge);
            pillEdge.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / 9f;
            UiBuilder.Stretch(pillEdge.rectTransform);
            Image pillFace = UiBuilder.Sliced("Face", _pill, art.RoundedRect, Palette.PanelLight);
            pillFace.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / 8f;
            UiBuilder.Stretch(pillFace.rectTransform);
            pillFace.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            pillFace.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            _levels = UiBuilder.Label("Levels", _pill, TextStyle.PlainBold, string.Empty, LevelsSp, Palette.UiInk, TextAlignmentOptions.Center);
            _levels.textWrappingMode = TextWrappingModes.Normal;

            _canoeTap = UiBuilder.BareButton("VoyageCanoe", _root, onTap);
            _stopTap = UiBuilder.BareButton("VoyageStop", _root, onTap);
            A11yLabels.Set(_canoeTap, () => UiText.HomeVoyageA11y(_sentence));
            A11yLabels.Set(_stopTap, () => UiText.HomeVoyageA11y(_sentence));
            _canoeTap.gameObject.SetActive(false);
            _stopTap.gameObject.SetActive(false);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Raíz (tests).</summary>
        public RectTransform Root => _root;

        /// <summary>La travesía se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>El letrero de la parada (nombre y «Faltan {n} niveles») se ve.</summary>
        public bool SignShown => _sign.gameObject.activeInHierarchy;

        /// <summary>Caja del letrero.</summary>
        public RectTransform SignRect => _sign;

        /// <summary>Caja de la canoa (con Rosa).</summary>
        public RectTransform CanoeRect => _canoe.rectTransform;

        /// <summary>Caja de Rosa (o del bufeo).</summary>
        public RectTransform GuideRect => _guide.rectTransform;

        /// <summary>Caja del poste.</summary>
        public RectTransform PostRect => _post.rectTransform;

        /// <summary>Nombre de la parada.</summary>
        public TextMeshProUGUI NameLabel => _name;

        /// <summary>«Faltan {n} niveles».</summary>
        public TextMeshProUGUI LevelsLabel => _levels;

        /// <summary>Caja de la píldora «Faltan {n} niveles».</summary>
        public RectTransform LevelsPill => _pill;

        /// <summary>Toque en la canoa (abre el Mapa).</summary>
        public Button CanoeButton => _canoeTap;

        /// <summary>Toque en la parada (abre el Mapa).</summary>
        public Button StopButton => _stopTap;

        /// <summary>Las dos áreas táctiles están activas (el Mapa existe para el jugador).</summary>
        public bool Interactive => _interactive && Visible;

        /// <summary>Fracción del tramo en la que va la canoa (0..1).</summary>
        public float Progress => _progress;

        /// <summary>Quién guía delante de la canoa.</summary>
        public string GuideId => _guideId;

        /// <summary>Lo que lee TalkBack (y la línea entera de la parada: «Faltan {n} niveles para {lugar}»).</summary>
        public string Sentence => _sentence;

        /// <summary>Con Reducir movimiento: todo quieto.</summary>
        public bool ReduceMotion => _reduceMotion;

        /// <summary>Posición del centro de la canoa en el lienzo (dp, y hacia abajo; tests de quietud).</summary>
        public Vector2 CanoeCenter => new Vector2(_canoe.rectTransform.anchoredPosition.x, -_canoe.rectTransform.anchoredPosition.y);

        /// <summary>
        /// Fracción del tramo hacia la parada siguiente con <paramref name="playLevel"/> en «Jugar»: (n − 1) mod 10 / 10 (en el 58,
        /// 0,7; en el 51, 0: acaba de salir), la misma del Mapa (<see cref="RiverJourney.LevelsLeft"/>).
        /// </summary>
        public static float ProgressOf(int playLevel)
        {
            int left = RiverJourney.LevelsLeft(Mathf.Clamp(playLevel, 1, AF.Levels.WorldTable.LastLevel));
            return Mathf.Clamp01(1f - (left / (float)AF.Levels.RiverStopCatalog.LevelsPerStop));
        }

        /// <summary>
        /// Lo que enseña: el nombre de la parada siguiente (<paramref name="place"/>; vacío = sin letrero ni ruta), los niveles que
        /// faltan, la línea entera para TalkBack (<paramref name="sentence"/>), la fracción del tramo y quién guía. Asigna textos:
        /// solo al cambiar de nivel o de idioma.
        /// </summary>
        public void Apply(string place, int levelsLeft, string sentence, float progress, string guideId)
        {
            _hasNext = place.Length > 0;
            _sentence = sentence;
            _progress = _hasNext ? Mathf.Clamp01(progress) : 0f;
            _guideId = guideId;
            _name.text = place;
            _levels.text = _hasNext ? UiText.HomeVoyageLevels(levelsLeft) : string.Empty;
            Sprite guide = string.Equals(guideId, RiverJourney.GuideBufeo, StringComparison.Ordinal) ? _art.MapBufeo : _art.Rosa;
            if (_guide.sprite != guide)
            {
                _guide.sprite = guide;
            }

            if (_laidOut)
            {
                Relayout();
            }
        }

        /// <summary>Activa o quita los toques (el Mapa existe para el jugador: <c>max_level</c> ≥ 10 y la escena lo trae).</summary>
        public void SetInteractive(bool on)
        {
            _interactive = on;
            bool visible = on && Visible;
            bool canoe = visible && !_canoeCovered;
            if (_canoeTap.gameObject.activeSelf != canoe)
            {
                _canoeTap.gameObject.SetActive(canoe);
            }

            bool stop = visible && _hasNext && !_signCovered;
            if (_stopTap.gameObject.activeSelf != stop)
            {
                _stopTap.gameObject.SetActive(stop);
            }
        }

        /// <summary>
        /// El bocadillo de TiTino tapa el letrero (<paramref name="sign"/>) o la canoa (<paramref name="canoe"/>): lo tapado se aparta
        /// mientras dure (la revisión de D-046 hacía lo mismo con la tarjeta del Reto), sin rehacer nada.
        /// </summary>
        public void SetCovered(bool sign, bool canoe)
        {
            _signCovered = sign;
            _canoeCovered = canoe;
            bool signOn = _hasNext && !sign;
            if (_sign.gameObject.activeSelf != signOn)
            {
                _sign.gameObject.SetActive(signOn);
            }

            bool canoeOn = !canoe;
            if (_canoe.gameObject.activeSelf != canoeOn)
            {
                _canoe.gameObject.SetActive(canoeOn);
                _guide.gameObject.SetActive(canoeOn);
                _wakeA.gameObject.SetActive(canoeOn);
                _wakeB.gameObject.SetActive(canoeOn);
            }

            SetInteractive(_interactive);
        }

        private bool _signCovered;
        private bool _canoeCovered;

        /// <summary>Muestra u oculta la travesía.</summary>
        public void SetVisible(bool on)
        {
            on = on && _laidOut;
            if (_root.gameObject.activeSelf != on)
            {
                _root.gameObject.SetActive(on);
            }

            SetInteractive(_interactive);
        }

        /// <summary>Reducir movimiento: la canoa y Rosa, quietas.</summary>
        public void SetReduceMotion(bool on)
        {
            if (on == _reduceMotion)
            {
                return;
            }

            _reduceMotion = on;
            if (_laidOut)
            {
                ApplyMotion();
            }
        }

        /// <summary>
        /// Coloca la ruta de <paramref name="start"/> (la salida, a la izquierda y más lejos) al pie del poste (<paramref name="foot"/>,
        /// delante, en el agua), con el letrero en lo alto del poste, centrado sobre él y entre <paramref name="minX"/> y
        /// <paramref name="maxX"/>; <paramref name="k"/> es el ancho del lienzo / 360. Solo al maquetar.
        /// </summary>
        public void Layout(Vector2 start, Vector2 foot, float minX, float maxX, float k)
        {
            _start = start;
            _foot = foot;
            _k = k;
            _laidOut = true;
            _minX = minX;
            _maxX = maxX;
            Relayout();
        }

        private float _minX;
        private float _maxX = 360f;

        /// <summary>Ancho del letrero, como mucho (dp a 360 de ancho).</summary>
        public const float SignMaxWidthDp = 180f;

        // El letrero a lo ancho de su texto, como mucho maxW: el nombre en una línea si cabe (si no, en las que pida) y la píldora
        // debajo.
        private void SignSize(float maxW, out float w, out float h, out float nameH, out float pillW, out float pillH)
        {
            float nameW = Mathf.Ceil(UiBuilder.MinTextWidth(_name)) + 2f;
            float levelsW = Mathf.Ceil(UiBuilder.MinTextWidth(_levels)) + 2f;
            float innerMax = Mathf.Max(40f, maxW - (2f * SignPadX));
            float inner = Mathf.Min(innerMax, Mathf.Max(nameW, levelsW + (2f * PillPadX)));
            nameH = Mathf.Ceil(UiBuilder.MinTextHeight(_name, inner));
            float pillInner = Mathf.Min(inner, levelsW + (2f * PillPadX)) - (2f * PillPadX);
            pillW = pillInner + (2f * PillPadX);
            pillH = _levels.text.Length == 0 ? 0f : Mathf.Max(20f * Mathf.Max(1f, UiBuilder.TextScale), Mathf.Ceil(UiBuilder.MinTextHeight(_levels, pillInner)) + 4f);
            w = inner + (2f * SignPadX);
            h = SignPadY + nameH + (pillH > 0f ? 3f + pillH : 0f) + SignPadY + 2f;
        }

        /// <summary>
        /// Alto del poste (dp): lo bastante para que la canoa llegue a su pie por debajo del letrero de lo alto (el alto de la canoa
        /// junto a la parada y 14 dp de aire).
        /// </summary>
        public float PostHeight => Mathf.Max(ProceduralArt.StopPostHeightDp, (ProceduralArt.MapCanoeHeightDp * CanoeScale) + 26f) * _k;

        private void Relayout()
        {
            float k = _k;
            // La ruta baja un poco por el medio (más cerca), por debajo del letrero de lo alto del poste.
            _control = new Vector2((_start.x + _foot.x) * 0.5f, ((_start.y + _foot.y) * 0.5f) + (6f * k));

            // El poste, más alto que la canoa, con su anillo de agua; el letrero en lo alto, centrado sobre él (lo que dejen minX y
            // maxX), con su borde de abajo 4 dp por debajo de la punta.
            float postW = ProceduralArt.StopPostWidthDp * k;
            float postH = PostHeight;
            _post.gameObject.SetActive(_hasNext);
            _postRing.gameObject.SetActive(_hasNext);
            _sign.gameObject.SetActive(_hasNext && !_signCovered);
            if (_hasNext)
            {
                float half = Mathf.Min(_foot.x - _minX, _maxX - _foot.x);
                float maxW = Mathf.Min(SignMaxWidthDp * k, Mathf.Max(2f * half, _maxX - _minX));
                SignSize(maxW, out float w, out float h, out float nameH, out float pillW, out float pillH);
                float signBottom = _foot.y - postH + 4f;
                float x = Mathf.Clamp(_foot.x - (w * 0.5f), _minX, Mathf.Max(_minX, _maxX - w));
                _post.rectTransform.pivot = new Vector2(0.5f, 0f);
                Place(_post.rectTransform, _foot.x, _foot.y, postW, postH);
                Center(_postRing.rectTransform, _foot.x, _foot.y - (1f * k), 28f * k, 7f * k);
                _signRect = new Rect(x, signBottom - h, w, h);
                UiBuilder.Place(_sign, _signRect.x, _signRect.y, w, h);
                UiBuilder.Place(_name.rectTransform, SignPadX, SignPadY, w - (2f * SignPadX), nameH);
                _pill.gameObject.SetActive(pillH > 0f);
                UiBuilder.Place(_pill, (w - pillW) * 0.5f, SignPadY + nameH + 3f, pillW, pillH);
                UiBuilder.Place(_levels.rectTransform, PillPadX, 0f, pillW - (2f * PillPadX), pillH);

                // El toque de la parada: el letrero y el poste, con 48 dp como mínimo.
                float tapW = Mathf.Max(MinTouchDp, w);
                float tapTop = _signRect.y;
                float tapH = Mathf.Max(MinTouchDp, _foot.y - tapTop);
                UiBuilder.Place((RectTransform)_stopTap.transform, _signRect.center.x - (tapW * 0.5f), tapTop, tapW, tapH);
            }
            // Ruta: los puntos de delante de la canoa, hasta el poste.
            for (int i = 0; i < RouteDots; i++)
            {
                float t = (i + 0.5f) / RouteDots;
                _dotPos[i] = Bezier(t);
                bool ahead = _hasNext && t > _progress + 0.09f && t < 0.95f;
                _dots[i].gameObject.SetActive(ahead);
                if (ahead)
                {
                    float s = Mathf.Lerp(3.4f, 5f, t) * k;
                    Center(_dots[i].rectTransform, _dotPos[i].x, _dotPos[i].y, s, s);
                }
            }

            // La canoa en su fracción (más pequeña lejos, en la salida: perspectiva) y Rosa delante, hacia la parada.
            float tc = _hasNext ? Mathf.Min(_progress, 0.82f) : 0f;
            float scale = Mathf.Lerp(0.62f, 1f, tc / 0.82f) * k;
            _canoeW = ProceduralArt.MapCanoeWidthDp * CanoeScale * scale;
            _canoeH = ProceduralArt.MapCanoeHeightDp * CanoeScale * scale;
            _canoePos = Bezier(tc);
            Size(_canoe.rectTransform, _canoeW, _canoeH);
            float gw = GuideDp * scale;

            // La canoa llega hasta el poste sin taparlo: deja sitio para Rosa delante.
            if (_hasNext)
            {
                float maxX = _foot.x - (ProceduralArt.StopPostWidthDp * k * 0.5f) - (gw * 0.85f) - (_canoeW * 0.5f);
                if (_canoePos.x > maxX)
                {
                    _canoePos = new Vector2(maxX, _canoePos.y);
                }
            }

            // Rosa asoma junto a la proa, un poco más cerca (más abajo), sin llegar al poste.
            float gx = _canoePos.x + (_canoeW * 0.5f) + (gw * 0.1f);
            if (_hasNext)
            {
                gx = Mathf.Min(gx, _foot.x - (ProceduralArt.StopPostWidthDp * k * 0.5f) - (gw * 0.5f) - 2f);
            }

            _guidePos = new Vector2(gx, _canoePos.y + (_canoeH * 0.22f));
            Size(_guide.rectTransform, gw, gw * ProceduralArt.RosaHeightDp / ProceduralArt.RosaWidthDp);
            _guide.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // Rosa y el bufeo miran a la izquierda en su dibujo
            Size(_wakeA.rectTransform, 30f * scale, 7f * scale);
            Size(_wakeB.rectTransform, 40f * scale, 9f * scale);
            ApplyMotion();

            // El toque de la canoa: la canoa y Rosa, con 48 dp como mínimo.
            float left = Mathf.Min(_canoePos.x - (_canoeW * 0.5f), _guidePos.x - (gw * 0.5f));
            float right = Mathf.Max(_canoePos.x + (_canoeW * 0.5f), _guidePos.x + (gw * 0.5f));
            float top = Mathf.Min(_canoePos.y - (_canoeH * 0.6f), _guidePos.y - (gw * 0.4f));
            float bot = _canoePos.y + (_canoeH * 0.45f);
            float tw = Mathf.Max(MinTouchDp, right - left);
            float th = Mathf.Max(MinTouchDp, bot - top);
            UiBuilder.Place((RectTransform)_canoeTap.transform, ((left + right) * 0.5f) - (tw * 0.5f), ((top + bot) * 0.5f) - (th * 0.5f), tw, th);
        }

        /// <summary>Caja de la canoa con Rosa en el lienzo (dp, y hacia abajo), tal como se colocó.</summary>
        public Rect CanoeBounds
        {
            get
            {
                float gw = _guide.rectTransform.sizeDelta.x;
                float left = Mathf.Min(_canoePos.x - (_canoeW * 0.5f), _guidePos.x - (gw * 0.5f));
                float right = Mathf.Max(_canoePos.x + (_canoeW * 0.5f), _guidePos.x + (gw * 0.5f));
                float top = Mathf.Min(_canoePos.y - (_canoeH * 0.5f), _guidePos.y - (gw * 0.35f));
                float bot = _canoePos.y + (_canoeH * 0.5f);
                return Rect.MinMaxRect(left, top, right, bot);
            }
        }

        /// <summary>Avanza el vaivén de la canoa y de Rosa (sin asignaciones). Con Reducir movimiento no hace nada.</summary>
        public void Tick(float dtMs)
        {
            if (_reduceMotion || !_laidOut || !_root.gameObject.activeInHierarchy)
            {
                return;
            }

            _clockMs += Mathf.Clamp(dtMs, 0f, 100f);
            if (_clockMs > 600000f)
            {
                _clockMs -= 600000f;
            }

            ApplyMotion();
        }

        /// <summary>Lleva el vaivén al instante <paramref name="ms"/> (capturas).</summary>
        public void Seek(float ms)
        {
            _clockMs = Mathf.Max(0f, ms);
            if (_laidOut)
            {
                ApplyMotion();
            }
        }

        private void ApplyMotion()
        {
            float t = _reduceMotion ? 0f : _clockMs * 0.001f;
            float k = _k;
            float bob = _reduceMotion ? 0f : Mathf.Sin(t * 1.7f) * 1.3f * k;
            float roll = _reduceMotion ? 0f : Mathf.Sin(t * 1.1f) * 2.2f;
            Center(_canoe.rectTransform, _canoePos.x, _canoePos.y + bob, _canoeW, _canoeH);
            _canoe.rectTransform.localRotation = Quaternion.Euler(0f, 0f, roll);
            float gBob = _reduceMotion ? 0f : Mathf.Sin((t * 2.1f) + 1f) * 1.8f * k;
            Center(_guide.rectTransform, _guidePos.x, _guidePos.y + gBob, _guide.rectTransform.sizeDelta.x, _guide.rectTransform.sizeDelta.y);
            float wake = _reduceMotion ? 0.5f : 0.5f + (0.5f * Mathf.Sin(t * 1.4f));
            Center(_wakeA.rectTransform, _canoePos.x - (_canoeW * 0.42f), _canoePos.y + (_canoeH * 0.3f), _wakeA.rectTransform.sizeDelta.x, _wakeA.rectTransform.sizeDelta.y);
            Center(_wakeB.rectTransform, _canoePos.x - (_canoeW * 0.62f), _canoePos.y + (_canoeH * 0.36f), _wakeB.rectTransform.sizeDelta.x, _wakeB.rectTransform.sizeDelta.y);
            _wakeA.canvasRenderer.SetAlpha(0.35f + (0.4f * wake));
            _wakeB.canvasRenderer.SetAlpha(0.2f + (0.3f * (1f - wake)));
        }

        private Vector2 Bezier(float t)
        {
            float u = 1f - t;
            return (u * u * _start) + (2f * u * t * _control) + (t * t * _foot);
        }

        private Image Piece(string name, Sprite? sprite, Color color)
        {
            Image img = UiBuilder.Image(name, _root, sprite, color);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        private static void Size(RectTransform rt, float w, float h)
        {
            var size = new Vector2(w, h);
            if (rt.sizeDelta != size)
            {
                rt.sizeDelta = size;
            }
        }

        // El centro en (x, y desde arriba), solo si cambia.
        private static void Center(RectTransform rt, float x, float y, float w, float h)
        {
            Size(rt, w, h);
            var pos = new Vector2(x, -y);
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }
        }

        // Con el pivote del rectángulo en (x, y desde arriba).
        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }
    }
}
