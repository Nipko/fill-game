#nullable enable
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// La pista de Morpho en el tablero (tarea MO2; 04 §2.3, VFX: «una morpho azul despega del botón, vuela hasta la cuia de
    /// origen (0,4 s), se posa y luego salta a la de destino dejando una estela de destello azul estructural; ambas cuias pulsan
    /// con un anillo claro»; resalta origen y destino «durante 3 s o hasta que el jugador vierta»; D-002: es una ayuda
    /// explícita, la única que señala un destino). Tres fases y los anillos:
    /// <list type="number">
    /// <item><description><b>Vuelo</b> (<see cref="FlightMs"/> = 400 ms): del botón a la boca del origen, en curva, aleteando;
    /// al posarse, el origen empieza a latir con su anillo.</description></item>
    /// <item><description><b>Posada y salto</b> (<see cref="PerchMs"/> y <see cref="HopMs"/>) [P]: salta en arco a la boca del
    /// destino; al posarse, el destino late también.</description></item>
    /// <item><description><b>Resalte</b> (<see cref="RingMs"/> = 3 s): las dos cuias laten con el anillo claro mientras la
    /// mariposa sigue posada en el destino, aleteando despacio; después todo se apaga en <see cref="FadeMs"/>.</description></item>
    /// </list>
    /// La mariposa se sigue con la vista (revisión de MO2; D-003, luz de juego): despega con la envergadura del glifo de la ranura
    /// (<see cref="TakeoffSpanDp"/>) y baja a <see cref="PerchSpanDp"/> al posarse, nunca por debajo de <see cref="MinSpanDp"/>;
    /// lleva un halo claro de 2 dp (<see cref="ProceduralArt.MorphoHalo"/>) y alas de azul iridiscente
    /// (<see cref="ProceduralArt.MorphoFlightBlue"/>), y deja en el vuelo y en el salto una estela azul de
    /// <see cref="TrailCount"/> puntos de <see cref="TrailDotDp"/> dp, uno cada <see cref="TrailEveryMs"/>, que se apagan de
    /// <see cref="TrailAlpha"/> a 0.
    /// Se apaga antes (<see cref="Clear"/>) en cuanto el flujo apaga la pista (<c>IHintListener.OnHintCleared</c>: un vertido,
    /// Marea, Volver a la orilla, otra cuia o fuera de PLAYING). Con Reducir movimiento (07 §7.8) no vuela: la mariposa aparece
    /// posada en el destino con un fundido corto, los dos anillos se encienden a la vez y no laten (alfa fijo), y no hay estela.
    /// </summary>
    /// <remarks>
    /// Las imágenes (dos anillos, la estela y la mariposa: halo, alas y cuerpo) se crean una vez, en un Canvas anidado propio de
    /// la capa de mecánicas delantera (moverlas no reconstruye el lienzo principal); los sprites de la mariposa se generan en
    /// <see cref="Prewarm"/> (al cargar un nivel con la ranura Pista) o, si no, en el primer <see cref="Play"/>. Cada fotograma
    /// solo coloca y tiñe: sin asignaciones (08 §5.4). Las posiciones y los tamaños van en las coordenadas del contenedor del
    /// tablero, que no se escala (dp; las de <c>CuiaView.FootPosition</c>: origen arriba a la izquierda, y hacia arriba), y
    /// siguen a las cuias si se elevan.
    /// </remarks>
    internal sealed class MorphoFx
    {
        /// <summary>Vuelo del botón a la cuia de origen (ms; 04 §2.3: 0,4 s).</summary>
        public const float FlightMs = 400f;

        /// <summary>Posada en el origen antes de saltar (ms) [P].</summary>
        public const float PerchMs = 220f;

        /// <summary>Salto del origen al destino (ms) [P].</summary>
        public const float HopMs = 340f;

        /// <summary>
        /// Las dos cuias laten con su anillo (ms; 04 §2.3: «durante 3 s o hasta que el jugador vierta»). Los 3 s empiezan cuando
        /// la mariposa se posa en el destino [P] (revisión de MO2): así origen y destino laten juntos los 3 s enteros, y el origen,
        /// que se enciende al posarse la mariposa en él, late además lo que dura la posada y el salto (<see cref="PerchMs"/> +
        /// <see cref="HopMs"/> = 0,56 s).
        /// </summary>
        public const float RingMs = 3000f;

        /// <summary>Fundido de salida de los anillos y de la mariposa (ms) [P].</summary>
        public const float FadeMs = 260f;

        /// <summary>Aparición de la mariposa posada con Reducir movimiento (ms) [P].</summary>
        public const float ReducedFadeMs = 120f;

        /// <summary>Periodo del latido de los anillos (ms) [P], el del pulso de la Luciérnaga.</summary>
        public const float RingPeriodMs = 900f;

        /// <summary>Alfa del anillo en el valle del latido [P].</summary>
        public const float RingAlphaMin = 0.45f;

        /// <summary>Alfa del anillo en la cresta del latido [P].</summary>
        public const float RingAlphaMax = 1f;

        /// <summary>Escala del anillo en la cresta del latido [P].</summary>
        public const float RingScaleMax = 1.04f;

        /// <summary>Separación entre la silueta de la cuia y la línea del anillo (u) [P], como el pulso de la Luciérnaga.</summary>
        public const float RingMarginU = 0.14f;

        /// <summary>Brillo de fuera del anillo, del canto de la línea al de la imagen (u) [P].</summary>
        public const float RingGlowU = 0.30f;

        /// <summary>Altura de la mariposa posada sobre el borde de la boca (u) [P].</summary>
        public const float PerchAboveRimU = 0.34f;

        /// <summary>
        /// Envergadura de la mariposa al despegar del botón (dp) [P] (revisión de MO2): la de la mariposa del glifo de la ranura
        /// Pista, para que se lea como la misma que sale de él.
        /// </summary>
        public const float TakeoffSpanDp = 28f;

        /// <summary>Envergadura de la mariposa posada (dp) [P]: el vuelo baja de <see cref="TakeoffSpanDp"/> a esta.</summary>
        public const float PerchSpanDp = 24f;

        /// <summary>Lo que crece la envergadura en lo alto del salto (dp) [P].</summary>
        public const float HopLiftSpanDp = 2f;

        /// <summary>La mariposa nunca mide menos de esta envergadura (dp) [P].</summary>
        public const float MinSpanDp = 22f;

        /// <summary>Aleteo en vuelo (Hz) [P].</summary>
        public const float WingHz = 7f;

        /// <summary>Aleteo posada (Hz) [P].</summary>
        public const float PerchedWingHz = 1.4f;

        /// <summary>Un punto de la estela cada tantos ms de vuelo o de salto [P] (revisión de MO2).</summary>
        public const float TrailEveryMs = 40f;

        /// <summary>Puntos de la estela [P]: los que caben a la vez (cada uno vive <see cref="TrailLifeMs"/>).</summary>
        public const int TrailCount = 8;

        /// <summary>Vida de cada punto de la estela (ms) [P]: con uno cada 40 ms, se ven 7 u 8 a la vez.</summary>
        public const float TrailLifeMs = 300f;

        /// <summary>Lado de cada punto de la estela (dp) [P].</summary>
        public const float TrailDotDp = 4f;

        /// <summary>Alfa de un punto de la estela al nacer; baja hasta 0 en <see cref="TrailLifeMs"/> [P].</summary>
        public const float TrailAlpha = 0.8f;

        /// <summary>Giro máximo hacia la dirección del vuelo (grados) [P].</summary>
        public const float MaxBankDeg = 40f;

        // Ancho de las alas del sprite sin su margen (px a MechanicPxPerU): la envergadura que se escala a dp.
        private static readonly float WingsInkPx = Mathf.Round(ProceduralArt.MorphoWingsWidthU * ProceduralArt.MechanicPxPerU);

        // Lienzos de los sprites de las alas y del cuerpo, con su margen (px): los halos miden lo mismo que lo que rodean.
        private static readonly Vector2 WingsPx = new Vector2(WingsInkPx, Mathf.Round(ProceduralArt.MorphoWingsHeightU * ProceduralArt.MechanicPxPerU))
            + (2f * ProceduralArt.MorphoSpritePadPx * Vector2.one);

        private static readonly Vector2 BodyPx = new Vector2(Mathf.Round(ProceduralArt.MorphoBodyWidthU * ProceduralArt.MechanicPxPerU), Mathf.Round(ProceduralArt.MorphoBodyHeightU * ProceduralArt.MechanicPxPerU))
            + (2f * ProceduralArt.MorphoSpritePadPx * Vector2.one);

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _butterfly;
        private readonly Image _wingsHalo;
        private readonly Image _bodyHalo;
        private readonly Image _wings;
        private readonly Image _body;
        private readonly Image _sourceRing;
        private readonly Image _targetRing;
        private readonly Image[] _trail = new Image[TrailCount];
        private readonly float[] _trailAge = new float[TrailCount];
        private readonly bool[] _trailOn = new bool[TrailCount];
        private Phase _phase;
        private float _t;
        private float _trailMs;
        private int _trailNext;
        private float _u = 24f;
        private float _span = TakeoffSpanDp;
        private bool _reduceMotion;
        private bool _sourceLit;
        private bool _targetLit;
        private float _ringT;
        private float _waveT;
        private float _fade = 1f;
        private Vector2 _from;
        private Vector2 _sourcePerch;
        private Vector2 _targetPerch;
        private Vector2 _pos;
        private float _angle;
        private float _wingOpen = 1f;

        /// <summary>Crea las imágenes (ocultas) en <paramref name="layer"/>.</summary>
        public MorphoFx(RectTransform layer, ProceduralArt art)
        {
            _art = art;
            var go = new GameObject("MorphoFx", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(layer, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = Vector2.zero;
            _sourceRing = NewRing("MorphoRingSource"); // no «TargetRing»: ese nombre es el resaltado de destino que D-002 prohíbe al seleccionar
            _targetRing = NewRing("MorphoRingTarget");
            for (int i = 0; i < TrailCount; i++)
            {
                Image dot = UiBuilder.Image("Trail", _root, art.Disc, Color.clear);
                RectTransform rt = dot.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(TrailDotDp, TrailDotDp);
                dot.raycastTarget = false; // siempre activo: se esconde con alfa 0 (activarlo la primera vez asigna memoria)
                _trail[i] = dot;
            }

            _butterfly = UiBuilder.Rect("Butterfly", _root);
            _butterfly.anchorMin = new Vector2(0f, 1f);
            _butterfly.anchorMax = new Vector2(0f, 1f);
            _butterfly.pivot = new Vector2(0.5f, 0.5f);
            _butterfly.sizeDelta = Vector2.zero;
            _wingsHalo = UiBuilder.Image("WingsHalo", _butterfly, null, Color.clear);
            _bodyHalo = UiBuilder.Image("BodyHalo", _butterfly, null, Color.clear);
            _wings = UiBuilder.Image("Wings", _butterfly, null, Color.white);
            _body = UiBuilder.Image("Body", _butterfly, null, Color.white);
            CenterOnParent(_wingsHalo.rectTransform);
            CenterOnParent(_bodyHalo.rectTransform);
            CenterOnParent(_wings.rectTransform);
            CenterOnParent(_body.rectTransform);
            go.SetActive(false);
        }

        private enum Phase
        {
            Idle = 0,
            Flight = 1,
            Perched = 2,
            Hop = 3,
            Rings = 4,
            Fade = 5,
        }

        /// <summary>La pista se ve: la mariposa vuela o está posada, o laten los anillos.</summary>
        public bool Active => _phase != Phase.Idle;

        /// <summary>La mariposa vuela del botón al origen.</summary>
        public bool Flying => _phase == Phase.Flight;

        /// <summary>La mariposa está posada en el origen, antes de saltar.</summary>
        public bool PerchedOnSource => _phase == Phase.Perched;

        /// <summary>La mariposa salta del origen al destino.</summary>
        public bool Hopping => _phase == Phase.Hop;

        /// <summary>La mariposa ya se posó en el destino (las dos cuias laten).</summary>
        public bool OnTarget => _phase == Phase.Rings || _phase == Phase.Fade;

        /// <summary>Cuia de origen de la pista en curso (−1 si no hay).</summary>
        public int Source { get; private set; } = -1;

        /// <summary>Cuia de destino de la pista en curso (−1 si no hay).</summary>
        public int Target { get; private set; } = -1;

        /// <summary>La cuia de origen late con su anillo.</summary>
        public bool SourceRinging => Active && _sourceLit;

        /// <summary>La cuia de destino late con su anillo.</summary>
        public bool TargetRinging => Active && _targetLit;

        /// <summary>Ms que les quedan a los anillos antes de apagarse (0 si aún no laten las dos o ya se apagan).</summary>
        public float RingLeftMs => _phase == Phase.Rings ? Mathf.Max(0f, RingMs - _ringT) : 0f;

        /// <summary>La mariposa se ve.</summary>
        public bool ButterflyVisible => Active && _butterfly.gameObject.activeSelf;

        /// <summary>Posición de la mariposa (coordenadas del contenedor del tablero).</summary>
        public Vector2 ButterflyPosition => _pos;

        /// <summary>Envergadura de ahora de la mariposa, con las alas abiertas (dp; tests).</summary>
        public float ButterflySpanDp => _span;

        /// <summary>Raíz del efecto (tests de orden de capas y capturas).</summary>
        public RectTransform Root => _root;

        /// <summary>La mariposa (capturas).</summary>
        public RectTransform Butterfly => _butterfly;

        /// <summary>El halo de las alas (tests).</summary>
        public Image WingsHalo => _wingsHalo;

        /// <summary>Anillo del origen (tests y capturas).</summary>
        public Image SourceRing => _sourceRing;

        /// <summary>Anillo del destino (tests y capturas).</summary>
        public Image TargetRing => _targetRing;

        /// <summary>Puntos de la estela nacidos en la última pista (tests).</summary>
        public int SparklesRequested { get; private set; }

        /// <summary>Puntos de la estela que se ven ahora (tests).</summary>
        public int TrailVisible
        {
            get
            {
                int n = 0;
                for (int i = 0; i < TrailCount; i++)
                {
                    n += _trailOn[i] ? 1 : 0;
                }

                return n;
            }
        }

        /// <summary>Alfa del punto de la estela más nuevo que se ve (0 si no se ve ninguno; tests).</summary>
        public float TrailNewestAlpha
        {
            get
            {
                float best = 0f;
                for (int i = 0; i < TrailCount; i++)
                {
                    if (_trailOn[i])
                    {
                        best = Mathf.Max(best, _trail[i].color.a);
                    }
                }

                return best;
            }
        }

        /// <summary>Los sprites de la mariposa ya están en sus imágenes (tests).</summary>
        public bool SpritesReady => _wings.sprite != null && _body.sprite != null && _wingsHalo.sprite != null && _bodyHalo.sprite != null;

        /// <summary>
        /// Pone ya los sprites de la mariposa (sus texturas se generan una vez y las guarda el arte): al cargar un nivel con la
        /// ranura Pista, para que ni el toque ni el vuelo creen texturas. Idempotente.
        /// </summary>
        public void Prewarm()
        {
            if (_wings.sprite == null)
            {
                _wings.sprite = _art.MorphoWingsSprite;
            }

            if (_body.sprite == null)
            {
                _body.sprite = _art.MorphoBodySprite;
            }

            if (_wingsHalo.sprite == null)
            {
                _wingsHalo.sprite = _art.MorphoWingsHaloSprite;
            }

            if (_bodyHalo.sprite == null)
            {
                _bodyHalo.sprite = _art.MorphoBodyHaloSprite;
            }
        }

        /// <summary>
        /// Arranca la pista: la mariposa sale de <paramref name="from"/> (el botón) hacia la cuia <paramref name="source"/> y
        /// salta a <paramref name="target"/> de <paramref name="views"/>. Una pista en curso se sustituye.
        /// </summary>
        public void Play(Vector2 from, int source, int target, CuiaView[] views, bool reduceMotion)
        {
            if ((uint)source >= (uint)views.Length || (uint)target >= (uint)views.Length)
            {
                Snap();
                return;
            }

            Prewarm();
            Source = source;
            Target = target;
            _reduceMotion = reduceMotion;
            _from = from;
            _t = 0f;
            _ringT = 0f;
            _waveT = 0f;
            _trailMs = 0f;
            _fade = 1f;
            _angle = 0f;
            SparklesRequested = 0;
            HideTrail();
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _butterfly.gameObject.SetActive(true);
            Follow(views);
            _pos = from;
            if (reduceMotion)
            {
                // Sin vuelo: la mariposa ya está en el destino y las dos cuias se resaltan a la vez.
                _phase = Phase.Rings;
                _sourceLit = true;
                _targetLit = true;
                _pos = _targetPerch;
            }
            else
            {
                _phase = Phase.Flight;
                _sourceLit = false;
                _targetLit = false;
            }

            Apply();
        }

        /// <summary>
        /// Avanza <paramref name="dtMs"/> ms: mueve la mariposa, enciende los anillos al posarse, los hace latir y lo apaga todo
        /// al acabar <see cref="RingMs"/>. Sigue a las cuias de <paramref name="views"/> (se elevan al seleccionarlas).
        /// </summary>
        public void Tick(float dtMs, CuiaView[] views)
        {
            if (_phase == Phase.Idle)
            {
                return;
            }

            if ((uint)Source >= (uint)views.Length || (uint)Target >= (uint)views.Length)
            {
                Snap();
                return;
            }

            float dt = Mathf.Max(0f, dtMs);
            _t += dt;
            Follow(views);
            switch (_phase)
            {
                case Phase.Flight:
                    if (_t >= FlightMs)
                    {
                        _t -= FlightMs;
                        _phase = Phase.Perched;
                        _sourceLit = true;
                        _ringT = 0f;
                    }

                    break;

                case Phase.Perched:
                    if (_t >= PerchMs)
                    {
                        _t -= PerchMs;
                        _phase = Phase.Hop;
                    }

                    break;

                case Phase.Hop:
                    if (_t >= HopMs)
                    {
                        _t -= HopMs;
                        _phase = Phase.Rings;
                        _targetLit = true;
                        _ringT = 0f;
                    }

                    break;

                case Phase.Rings:
                    if (_ringT >= RingMs)
                    {
                        _phase = Phase.Fade;
                        _t = 0f;
                    }

                    break;

                case Phase.Fade:
                    _fade = 1f - Mathf.Clamp01(_t / FadeMs);
                    if (_t >= FadeMs)
                    {
                        Snap();
                        return;
                    }

                    break;
            }

            if (_sourceLit || _targetLit)
            {
                _ringT += dt;
                _waveT += dt;
            }

            Apply();
            TickTrail(dt);
        }

        /// <summary>
        /// La pista se apaga (el flujo la apagó: otro vertido, Marea…): los anillos y la mariposa se funden en
        /// <see cref="FadeMs"/>, sin seguir volando. Sin pista, no hace nada.
        /// </summary>
        public void Clear()
        {
            if (_phase == Phase.Idle || _phase == Phase.Fade)
            {
                return;
            }

            _phase = Phase.Fade;
            _t = 0f;
        }

        /// <summary>Lo apaga todo en el acto (nivel nuevo, vistas nuevas).</summary>
        public void Snap()
        {
            _phase = Phase.Idle;
            _t = 0f;
            _sourceLit = false;
            _targetLit = false;
            Source = -1;
            Target = -1;
            HideTrail();
            _root.gameObject.SetActive(false);
        }

        // Vuelve a medir dónde se posa y dónde van los anillos (las cuias se elevan al seleccionarlas).
        private void Follow(CuiaView[] views)
        {
            CuiaView src = views[Source];
            CuiaView dst = views[Target];
            _u = Mathf.Max(1f, dst.Unit);
            _sourcePerch = PerchOf(src);
            _targetPerch = PerchOf(dst);
            PlaceRing(_sourceRing, src);
            PlaceRing(_targetRing, dst);
        }

        // Sobre el centro de la boca, con la cuia elevada también.
        private static Vector2 PerchOf(CuiaView view)
        {
            float u = Mathf.Max(1f, view.Unit);
            return view.FootPosition + view.BodyOffset + new Vector2(0f, (view.HeightU + PerchAboveRimU) * u);
        }

        // El anillo rodea la boca y los lados de la cuia (el aro de la Luciérnaga, abierto por abajo hasta el pie).
        private static void PlaceRing(Image ring, CuiaView view)
        {
            float u = Mathf.Max(1f, view.Unit);
            float margin = (RingMarginU + RingGlowU) * u;
            float w = (CuiaGeometry.WidthU * u) + (2f * margin);
            float h = (view.HeightU * u) + margin;
            Vector2 foot = view.FootPosition + view.BodyOffset;
            RectTransform rt = ring.rectTransform;
            rt.anchoredPosition = new Vector2(foot.x, foot.y + (0.5f * h));
            rt.sizeDelta = new Vector2(w, h);
            ring.pixelsPerUnitMultiplier = ProceduralArt.RevealHaloGlowPx / (RingGlowU * u);
        }

        // Pose del fotograma: posición, giro, tamaño y aleteo de la mariposa; alfa y escala de los anillos.
        private void Apply()
        {
            float alpha = 1f;
            float flapHz = PerchedWingHz;
            float angle = 0f;
            float span = PerchSpanDp;
            switch (_phase)
            {
                case Phase.Flight:
                {
                    float p = Mathf.Clamp01(_t / FlightMs);
                    float e = Ease.InOutSine(p);
                    Vector2 d = _sourcePerch - _from;
                    var normal = new Vector2(-d.y, d.x).normalized;
                    if (normal.y < 0f)
                    {
                        normal = -normal;
                    }

                    Vector2 control = ((_from + _sourcePerch) * 0.5f) + (normal * (0.18f * d.magnitude));
                    Vector2 at = Bezier(_from, control, _sourcePerch, e);
                    Vector2 ahead = Bezier(_from, control, _sourcePerch, Mathf.Min(1f, e + 0.03f));
                    _pos = at;
                    angle = Bank(ahead - at) * (1f - Ease.InOutSine(Mathf.Clamp01((p - 0.7f) / 0.3f)));
                    flapHz = WingHz;
                    alpha = Mathf.Clamp01(_t / 80f); // asoma del botón
                    span = Mathf.Lerp(TakeoffSpanDp, PerchSpanDp, e); // sale del tamaño del glifo y se posa con el suyo
                    break;
                }

                case Phase.Perched:
                    _pos = _sourcePerch;
                    break;

                case Phase.Hop:
                {
                    float p = Mathf.Clamp01(_t / HopMs);
                    float e = Ease.InOutSine(p);
                    Vector2 mid = ((_sourcePerch + _targetPerch) * 0.5f) + new Vector2(0f, (0.9f * _u) + (0.12f * Mathf.Abs(_targetPerch.x - _sourcePerch.x)));
                    Vector2 at = Bezier(_sourcePerch, mid, _targetPerch, e);
                    Vector2 ahead = Bezier(_sourcePerch, mid, _targetPerch, Mathf.Min(1f, e + 0.03f));
                    _pos = at;
                    angle = Bank(ahead - at) * (1f - Ease.InOutSine(Mathf.Clamp01((p - 0.7f) / 0.3f)));
                    flapHz = WingHz;
                    span = PerchSpanDp + (HopLiftSpanDp * Mathf.Sin(Mathf.PI * p));
                    break;
                }

                default:
                    _pos = _targetPerch;
                    if (_reduceMotion && _phase == Phase.Rings)
                    {
                        alpha = Mathf.Clamp01(_ringT / ReducedFadeMs);
                    }

                    break;
            }

            alpha *= _fade;
            _angle = angle;
            _span = Mathf.Max(MinSpanDp, span);
            _wingOpen = _reduceMotion && _phase != Phase.Flight && _phase != Phase.Hop
                ? 1f
                : 0.3f + (0.7f * Mathf.Abs(Mathf.Cos(_t * 0.001f * Mathf.PI * flapHz)));
            _butterfly.anchoredPosition = _pos;
            _butterfly.localRotation = Quaternion.Euler(0f, 0f, _angle);

            // Los sprites llevan MorphoSpritePadPx de margen por lado (el halo cabe dentro): todo a la escala de la envergadura.
            float k = _span / WingsInkPx;
            Vector2 wings = WingsPx * k;
            Vector2 body = BodyPx * k;
            var flap = new Vector3(_wingOpen, 1f, 1f);
            _wings.rectTransform.sizeDelta = wings;
            _wingsHalo.rectTransform.sizeDelta = wings;
            _wings.rectTransform.localScale = flap;
            _wingsHalo.rectTransform.localScale = flap;
            _body.rectTransform.sizeDelta = body;
            _bodyHalo.rectTransform.sizeDelta = body;
            var tone = new Color(1f, 1f, 1f, alpha);
            if (_wings.color != tone)
            {
                _wings.color = tone;
                _body.color = tone;
                Color h = ProceduralArt.MorphoHalo;
                var halo = new Color(h.r, h.g, h.b, ProceduralArt.MorphoHaloAlpha * alpha);
                _wingsHalo.color = halo;
                _bodyHalo.color = halo;
            }

            // Anillos: laten desde que se encienden (con Reducir movimiento, fijos a media altura del latido).
            float wave = _reduceMotion ? 0.5f : 0.5f + (0.5f * Mathf.Cos(_waveT / RingPeriodMs * 2f * Mathf.PI));
            float ringAlpha = Mathf.Lerp(RingAlphaMin, RingAlphaMax, wave) * _fade;
            float scale = _reduceMotion ? 1f : Mathf.Lerp(1f, RingScaleMax, wave);
            ApplyRing(_sourceRing, _sourceLit, ringAlpha, scale);
            ApplyRing(_targetRing, _targetLit, ringAlpha, scale);
        }

        private static void ApplyRing(Image ring, bool lit, float alpha, float scale)
        {
            if (ring.gameObject.activeSelf != lit)
            {
                ring.gameObject.SetActive(lit);
            }

            if (!lit)
            {
                return;
            }

            Color c = ProceduralArt.MorphoRingColor;
            ring.color = new Color(c.r, c.g, c.b, alpha);
            ring.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        // Estela del vuelo y del salto (revisión de MO2): un punto cada TrailEveryMs donde está la mariposa, que se apaga de
        // TrailAlpha a 0 en TrailLifeMs; los puntos se reutilizan en rueda. Sin estela con Reducir movimiento.
        private void TickTrail(float dtMs)
        {
            for (int i = 0; i < TrailCount; i++)
            {
                if (!_trailOn[i])
                {
                    continue;
                }

                _trailAge[i] += dtMs;
                if (_trailAge[i] >= TrailLifeMs)
                {
                    _trailOn[i] = false;
                    _trail[i].color = Color.clear;
                    continue;
                }

                SetTrailAlpha(_trail[i], _trailAge[i]);
            }

            if (_reduceMotion || (_phase != Phase.Flight && _phase != Phase.Hop))
            {
                return;
            }

            _trailMs += dtMs;
            while (_trailMs >= TrailEveryMs)
            {
                _trailMs -= TrailEveryMs;
                Image dot = _trail[_trailNext];
                _trailAge[_trailNext] = 0f;
                _trailOn[_trailNext] = true;
                _trailNext = (_trailNext + 1) % TrailCount;
                dot.rectTransform.anchoredPosition = _pos;
                SetTrailAlpha(dot, 0f);
                SparklesRequested++;
            }
        }

        private void SetTrailAlpha(Image dot, float ageMs)
        {
            Color c = ProceduralArt.MorphoFlightLight;
            dot.color = new Color(c.r, c.g, c.b, TrailAlpha * (1f - Mathf.Clamp01(ageMs / TrailLifeMs)) * _fade);
        }

        private void HideTrail()
        {
            _trailNext = 0;
            for (int i = 0; i < TrailCount; i++)
            {
                _trailAge[i] = 0f;
                if (_trailOn[i])
                {
                    _trailOn[i] = false;
                    _trail[i].color = Color.clear;
                }
            }
        }

        private static float Bank(Vector2 dir)
        {
            return dir.sqrMagnitude > 1e-6f ? Mathf.Clamp(Vector2.SignedAngle(Vector2.up, dir), -MaxBankDeg, MaxBankDeg) : 0f;
        }

        private static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float k = 1f - t;
            return (k * k * a) + (2f * k * t * c) + (t * t * b);
        }

        private Image NewRing(string name)
        {
            Image ring = UiBuilder.Sliced(name, _root, _art.RevealHalo, Color.clear);
            RectTransform rt = ring.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            Material? additive = UiMaterials.Additive;
            if (additive != null)
            {
                ring.material = additive;
            }

            ring.raycastTarget = false;
            ring.gameObject.SetActive(false);
            return ring;
        }

        private static void CenterOnParent(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
