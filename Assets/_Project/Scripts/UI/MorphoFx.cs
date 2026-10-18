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
    /// destino con una estela de destellos azules (<see cref="ProceduralArt.MorphoLight"/>) del pool común de
    /// <see cref="IBoardFx"/>; al posarse, el destino late también.</description></item>
    /// <item><description><b>Resalte</b> (<see cref="RingMs"/> = 3 s): las dos cuias laten con el anillo claro mientras la
    /// mariposa sigue posada en el destino, aleteando despacio; después todo se apaga en <see cref="FadeMs"/>.</description></item>
    /// </list>
    /// Se apaga antes (<see cref="Clear"/>) en cuanto el flujo apaga la pista (<c>IHintListener.OnHintCleared</c>: un vertido,
    /// Marea, Volver a la orilla, otra cuia o fuera de PLAYING). Con Reducir movimiento (07 §7.8) no vuela: la mariposa aparece
    /// posada en el destino con un fundido corto, los dos anillos se encienden a la vez y no laten (alfa fijo), y no hay estela.
    /// </summary>
    /// <remarks>
    /// Las imágenes (dos anillos y la mariposa: alas y cuerpo) se crean una vez, en un Canvas anidado propio de la capa de
    /// mecánicas delantera (moverlas no reconstruye el lienzo principal); los sprites de la mariposa se generan en
    /// <see cref="Prewarm"/> (al cargar un nivel con la ranura Pista) o, si no, en el primer <see cref="Play"/>. Cada fotograma
    /// solo coloca y tiñe: sin asignaciones (08 §5.4). Las posiciones van en las coordenadas del contenedor del tablero (las de
    /// <c>CuiaView.FootPosition</c>: origen arriba a la izquierda, y hacia arriba) y siguen a las cuias si se elevan.
    /// </remarks>
    internal sealed class MorphoFx
    {
        /// <summary>Vuelo del botón a la cuia de origen (ms; 04 §2.3: 0,4 s).</summary>
        public const float FlightMs = 400f;

        /// <summary>Posada en el origen antes de saltar (ms) [P].</summary>
        public const float PerchMs = 220f;

        /// <summary>Salto del origen al destino (ms) [P].</summary>
        public const float HopMs = 340f;

        /// <summary>Las dos cuias laten con su anillo (ms; 04 §2.3: 3 s), desde que la mariposa se posa en el destino.</summary>
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

        /// <summary>Aleteo en vuelo (Hz) [P].</summary>
        public const float WingHz = 7f;

        /// <summary>Aleteo posada (Hz) [P].</summary>
        public const float PerchedWingHz = 1.4f;

        /// <summary>Un destello de la estela cada tantos ms de salto [P] (el pool descarta lo que no cabe).</summary>
        public const float SparkleEveryMs = 60f;

        /// <summary>Giro máximo hacia la dirección del vuelo (grados) [P].</summary>
        public const float MaxBankDeg = 40f;

        private readonly ProceduralArt _art;
        private readonly IBoardFx? _fx;
        private readonly RectTransform _root;
        private readonly RectTransform _butterfly;
        private readonly Image _wings;
        private readonly Image _body;
        private readonly Image _sourceRing;
        private readonly Image _targetRing;
        private Phase _phase;
        private float _t;
        private float _sparkleMs;
        private float _u = 24f;
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

        /// <summary>Crea las imágenes (ocultas) en <paramref name="layer"/>; la estela va a <paramref name="fx"/> (null = sin estela).</summary>
        public MorphoFx(RectTransform layer, ProceduralArt art, IBoardFx? fx)
        {
            _art = art;
            _fx = fx;
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
            _butterfly = UiBuilder.Rect("Butterfly", _root);
            _butterfly.anchorMin = new Vector2(0f, 1f);
            _butterfly.anchorMax = new Vector2(0f, 1f);
            _butterfly.pivot = new Vector2(0.5f, 0.5f);
            _butterfly.sizeDelta = Vector2.zero;
            _wings = UiBuilder.Image("Wings", _butterfly, null, Color.white);
            _body = UiBuilder.Image("Body", _butterfly, null, Color.white);
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

        /// <summary>Raíz del efecto (tests de orden de capas y capturas).</summary>
        public RectTransform Root => _root;

        /// <summary>La mariposa (capturas).</summary>
        public RectTransform Butterfly => _butterfly;

        /// <summary>Anillo del origen (tests y capturas).</summary>
        public Image SourceRing => _sourceRing;

        /// <summary>Anillo del destino (tests y capturas).</summary>
        public Image TargetRing => _targetRing;

        /// <summary>Destellos pedidos al pool en la última pista (tests).</summary>
        public int SparklesRequested { get; private set; }

        /// <summary>Los sprites de la mariposa ya están en sus imágenes (tests).</summary>
        public bool SpritesReady => _wings.sprite != null && _body.sprite != null;

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
            _sparkleMs = 0f;
            _fade = 1f;
            _angle = 0f;
            SparklesRequested = 0;
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
                    TickSparkles(dt);
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

        // Pose del fotograma: posición, giro y aleteo de la mariposa; alfa y escala de los anillos.
        private void Apply()
        {
            float alpha = 1f;
            float flapHz = PerchedWingHz;
            float angle = 0f;
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
            _wingOpen = _reduceMotion && _phase != Phase.Flight && _phase != Phase.Hop
                ? 1f
                : 0.3f + (0.7f * Mathf.Abs(Mathf.Cos(_t * 0.001f * Mathf.PI * flapHz)));
            _butterfly.anchoredPosition = _pos;
            _butterfly.localRotation = Quaternion.Euler(0f, 0f, _angle);
            _wings.rectTransform.sizeDelta = new Vector2(ProceduralArt.MorphoWingsWidthU * _u, ProceduralArt.MorphoWingsHeightU * _u);
            _wings.rectTransform.localScale = new Vector3(_wingOpen, 1f, 1f);
            _body.rectTransform.sizeDelta = new Vector2(ProceduralArt.MorphoBodyWidthU * _u, ProceduralArt.MorphoBodyHeightU * _u);
            var tone = new Color(1f, 1f, 1f, alpha);
            if (_wings.color != tone)
            {
                _wings.color = tone;
                _body.color = tone;
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

        // Estela del salto: un destello azul cada SparkleEveryMs, en la mariposa (el pool descarta lo que no cabe).
        private void TickSparkles(float dtMs)
        {
            if (_fx == null || _reduceMotion)
            {
                return;
            }

            _sparkleMs += dtMs;
            while (_sparkleMs >= SparkleEveryMs)
            {
                _sparkleMs -= SparkleEveryMs;
                _fx.Sparkles(_pos, ProceduralArt.MorphoLight, 1, _u * 0.6f);
                SparklesRequested++;
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
