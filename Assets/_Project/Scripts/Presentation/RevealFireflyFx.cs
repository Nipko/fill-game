#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>Lo que pasó en un fotograma de <see cref="RevealFireflyFx.Tick"/>.</summary>
    public enum RevealFireflyEvent
    {
        /// <summary>Nada nuevo.</summary>
        None = 0,

        /// <summary>La luciérnaga acaba de posarse en el borde de la cuia: empieza el revelado (su luz baja por el vidrio).</summary>
        Landed = 1,

        /// <summary>La luciérnaga ya se fue: el efecto terminó.</summary>
        Done = 2,
    }

    /// <summary>
    /// La luciérnaga de Revelar (tarea T14 del paso 1; 04 §2.5: «una luciérnaga sale del botón, se posa en el borde de la cuia
    /// elegida y su luz baja por el vidrio»; D-003, luz de juego): sale del botón, vuela en curva con un leve vaivén hasta el
    /// borde de la boca de la cuia (<see cref="FlightMs"/>), se posa con el farol encendido mientras la cuia se aclara de arriba
    /// abajo (la duración del revelado, que pinta <c>CuiaView.PlayReveal</c> con <c>RevealStyle.Firefly</c>) y se va hacia
    /// arriba apagándose (<see cref="LeaveMs"/>). Deja una estela de destellos del pool común de <see cref="IBoardFx"/> (tope de
    /// 44 partículas y de 8 brillos aditivos; T19). Va en la capa de mecánicas delantera del tablero (arquitectura del paso 1,
    /// §6), con las posiciones en las coordenadas del contenedor del tablero (las de <c>CuiaView.FootPosition</c> y de
    /// <see cref="FxLayer"/>: origen arriba a la izquierda, y hacia arriba).
    /// </summary>
    /// <remarks>
    /// Tres imágenes creadas una vez (halo aditivo, alas y cuerpo) bajo una raíz que se mueve y gira; sin asignaciones al
    /// volar (08 §5.4). Con Reducir movimiento (07 §7.8) no vuela: aparece en el borde con un fundido corto y se apaga sin
    /// subir, y el pool no emite destellos. Las duraciones son [P] y ya vienen multiplicadas por el factor de velocidad.
    /// </remarks>
    public sealed class RevealFireflyFx
    {
        /// <summary>Vuelo del botón a la boca de la cuia (ms, × factor de velocidad) [P].</summary>
        public const float FlightMs = 520f;

        /// <summary>Despegue y fundido al terminar el revelado (ms, × factor de velocidad) [P].</summary>
        public const float LeaveMs = 300f;

        /// <summary>Aparición en el borde con Reducir movimiento (ms) [P].</summary>
        public const float ReducedFadeMs = 120f;

        /// <summary>
        /// Un destello de la estela cada tantos ms de vuelo [P]: con su vida (≤ 490 ms) y los de <see cref="LandingSparkles"/>, a
        /// lo sumo 8 a la vez, el tope de brillos aditivos del pool (<see cref="FxLayer.MaxAdditive"/>).
        /// </summary>
        public const float SparkleEveryMs = 90f;

        /// <summary>Destellos al posarse [P].</summary>
        public const int LandingSparkles = 3;

        /// <summary>Halo del farol en vuelo (diámetro en u) [P].</summary>
        public const float GlowFlightU = 1.25f;

        /// <summary>Halo del farol posada, mientras su luz baja por el vidrio (diámetro en u) [P].</summary>
        public const float GlowLandedU = 1.9f;

        /// <summary>Alfa del halo aditivo [P].</summary>
        public const float GlowAlpha = 0.85f;

        /// <summary>Aleteo de las alas en vuelo (Hz) [P].</summary>
        public const float WingHz = 18f;

        /// <summary>Altura de la luciérnaga posada sobre el borde (u): el farol queda justo encima del labio [P].</summary>
        public const float PerchAboveRimU = 0.30f;

        /// <summary>Giro máximo hacia la dirección del vuelo (grados) [P].</summary>
        public const float MaxBankDeg = 50f;

        private readonly RectTransform _root;
        private readonly Image _glow;
        private readonly Image _wings;
        private readonly Image _body;
        private readonly IBoardFx? _fx;
        private Phase _phase;
        private float _t;
        private float _flightMs;
        private float _glowMs;
        private float _leaveMs;
        private float _sparkleMs;
        private float _u = 24f;
        private float _angle;
        private bool _reduceMotion;
        private bool _landedReported;
        private Vector2 _from;
        private Vector2 _to;
        private Vector2 _control;
        private Vector2 _pos;

        /// <summary>Crea las imágenes (ocultas) en <paramref name="layer"/>; los destellos van a <paramref name="fx"/> (null = sin estela).</summary>
        public RevealFireflyFx(RectTransform layer, ProceduralArt art, IBoardFx? fx)
        {
            if (layer == null)
            {
                throw new System.ArgumentNullException(nameof(layer));
            }

            _fx = fx;

            // Canvas anidado propio: moverla cada fotograma no reconstruye el lienzo principal (guía 8).
            var go = new GameObject("RevealFirefly", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(layer, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = Vector2.zero;
            _glow = NewImage("Glow", art.Glow, new Color(ProceduralArt.RevealHaloColor.r, ProceduralArt.RevealHaloColor.g, ProceduralArt.RevealHaloColor.b, 0f));
            Material? additive = UiMaterials.Additive;
            if (additive != null)
            {
                _glow.material = additive;
            }

            _wings = NewImage("Wings", art.FireflyWingsSprite, Color.white);
            _body = NewImage("Body", art.FireflyBodySprite, Color.white);
            go.SetActive(false);
        }

        private enum Phase
        {
            Idle = 0,
            Flight = 1,
            Perched = 2,
            Leave = 3,
        }

        /// <summary>El efecto está en marcha (vuela, está posada o se va).</summary>
        public bool Active => _phase != Phase.Idle;

        /// <summary>Ya se posó (posada o yéndose): el revelado empezó.</summary>
        public bool HasLanded => _phase == Phase.Perched || _phase == Phase.Leave;

        /// <summary>Vuela hacia la cuia.</summary>
        public bool Flying => _phase == Phase.Flight;

        /// <summary>Raíz de las imágenes (tests y capturas).</summary>
        public RectTransform Root => _root;

        /// <summary>Halo aditivo del farol (tests y capturas).</summary>
        public Image Glow => _glow;

        /// <summary>Posición actual del centro de la luciérnaga, en coordenadas del contenedor del tablero.</summary>
        public Vector2 Position => _pos;

        /// <summary>Punto de salida del último vuelo (el botón).</summary>
        public Vector2 From => _from;

        /// <summary>Punto donde se posa (sobre el borde de la boca).</summary>
        public Vector2 Perch => _to;

        /// <summary>Duración del vuelo en curso (ms, ya con el factor de velocidad; 0 con Reducir movimiento).</summary>
        public float FlightDurationMs => _flightMs;

        /// <summary>Destellos pedidos al pool en el último efecto (diagnóstico y tests).</summary>
        public int SparklesRequested { get; private set; }

        /// <summary>Punto donde se posa la luciérnaga para la boca en <paramref name="rimCenter"/> (u = <paramref name="unitDp"/>).</summary>
        public static Vector2 PerchFor(Vector2 rimCenter, float unitDp, float side)
        {
            return rimCenter + new Vector2(Mathf.Sign(side == 0f ? 1f : side) * 0.32f * unitDp, PerchAboveRimU * unitDp);
        }

        /// <summary>
        /// Arranca el efecto: vuela de <paramref name="from"/> (el botón) a <paramref name="perch"/> en
        /// <see cref="FlightMs"/> × <paramref name="speedFactor"/>, se queda posada <paramref name="glowMs"/> ms (la duración del
        /// revelado, ya con el factor) y se va en <see cref="LeaveMs"/> × <paramref name="speedFactor"/>. Un efecto en curso se
        /// sustituye. Sin asignaciones.
        /// </summary>
        public void Play(Vector2 from, Vector2 perch, float unitDp, float speedFactor, float glowMs, bool reduceMotion)
        {
            float speed = Mathf.Max(0.01f, speedFactor);
            _u = Mathf.Max(1f, unitDp);
            _from = from;
            _to = perch;
            _reduceMotion = reduceMotion;
            _flightMs = reduceMotion ? 0f : FlightMs * speed;
            _glowMs = Mathf.Max(0f, glowMs);
            _leaveMs = (reduceMotion ? ReducedFadeMs : LeaveMs) * speed;
            _t = 0f;
            _sparkleMs = 0f;
            _angle = 0f;
            _landedReported = false;
            SparklesRequested = 0;

            // Curva hacia el lado contrario al centro del vuelo, un poco más alta que la recta (vuelo de insecto, no de proyectil).
            Vector2 d = perch - from;
            var normal = new Vector2(-d.y, d.x).normalized;
            if (normal.x * d.x < 0f)
            {
                normal = -normal;
            }

            _control = ((from + perch) * 0.5f) + (normal * (0.22f * d.magnitude)) + new Vector2(0f, 0.08f * d.magnitude);
            _wings.rectTransform.sizeDelta = new Vector2(ProceduralArt.FireflyWingsWidthU * _u, ProceduralArt.FireflyWingsHeightU * _u);
            _wings.rectTransform.anchoredPosition = new Vector2(0f, ProceduralArt.FireflyWingsAboveU * _u);
            _body.rectTransform.sizeDelta = new Vector2(ProceduralArt.FireflyBodyWidthU * _u, ProceduralArt.FireflyBodyHeightU * _u);
            _glow.rectTransform.anchoredPosition = new Vector2(0f, -ProceduralArt.FireflyLanternBelowU * _u);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _phase = _flightMs > 0f ? Phase.Flight : Phase.Perched;
            Apply(0f);
        }

        /// <summary>
        /// Sigue a la boca si la cuia se mueve (se eleva al seleccionarla) mientras está posada o llegando: el nuevo punto donde
        /// se posa.
        /// </summary>
        public void SetPerch(Vector2 perch)
        {
            _to = perch;
        }

        /// <summary>
        /// Avanza <paramref name="dtMs"/> milisegundos. Devuelve <see cref="RevealFireflyEvent.Landed"/> en el fotograma en que se
        /// posa (con Reducir movimiento, en el primero) y <see cref="RevealFireflyEvent.Done"/> en el que termina.
        /// </summary>
        public RevealFireflyEvent Tick(float dtMs)
        {
            if (_phase == Phase.Idle)
            {
                return RevealFireflyEvent.None;
            }

            RevealFireflyEvent result = RevealFireflyEvent.None;
            if (_phase == Phase.Perched && !_landedReported)
            {
                _landedReported = true;
                result = RevealFireflyEvent.Landed; // Reducir movimiento: se posa en el primer fotograma
            }

            _t += Mathf.Max(0f, dtMs);
            switch (_phase)
            {
                case Phase.Flight:
                    TickSparkles(dtMs);
                    if (_t >= _flightMs)
                    {
                        _t -= _flightMs;
                        _phase = Phase.Perched;
                        _landedReported = true;
                        Burst();
                        result = RevealFireflyEvent.Landed;
                    }

                    break;

                case Phase.Perched:
                    if (_t >= _glowMs)
                    {
                        _t -= _glowMs;
                        _phase = Phase.Leave;
                    }

                    break;
            }

            if (_phase == Phase.Leave && _t >= _leaveMs)
            {
                Hide();
                return RevealFireflyEvent.Done;
            }

            Apply(dtMs);
            return result;
        }

        /// <summary>Termina en el acto: oculta la luciérnaga (los destellos ya emitidos siguen su vida en el pool).</summary>
        public void Snap()
        {
            if (_phase != Phase.Idle)
            {
                Hide();
            }
        }

        private void Hide()
        {
            _phase = Phase.Idle;
            _t = 0f;
            _root.gameObject.SetActive(false);
        }

        // Pose del fotograma (posición, giro, alas y halo) según la fase.
        private void Apply(float dtMs)
        {
            float angle = 0f;
            float wings = 0.5f;
            float glowSize = GlowFlightU;
            float alpha = 1f;
            float flicker = 1f + (0.08f * Mathf.Sin(_t * 0.001f * 2f * Mathf.PI * 7f));
            switch (_phase)
            {
                case Phase.Flight:
                {
                    float p = Mathf.Clamp01(_t / Mathf.Max(1f, _flightMs));
                    float e = Ease.InOutSine(p);
                    Vector2 at = Bezier(e);
                    Vector2 ahead = Bezier(Mathf.Min(1f, e + 0.02f));
                    Vector2 dir = ahead - at;
                    var normal = new Vector2(-dir.y, dir.x).normalized;
                    at += normal * (Mathf.Sin(p * Mathf.PI * 4f) * 0.14f * _u * (1f - p)); // vaivén que se calma al llegar
                    _pos = at;
                    if (dir.sqrMagnitude > 1e-6f)
                    {
                        angle = Mathf.Clamp(Vector2.SignedAngle(Vector2.up, dir), -MaxBankDeg, MaxBankDeg);
                    }

                    angle *= 1f - Ease.InOutSine(Mathf.Clamp01((p - 0.75f) / 0.25f)); // se endereza para posarse
                    wings = 0.55f + (0.45f * Mathf.Abs(Mathf.Sin(_t * 0.001f * Mathf.PI * WingHz)));
                    alpha = Mathf.Clamp01(_t / 90f); // asoma por el borde del botón
                    break;
                }

                case Phase.Perched:
                {
                    // Posada: el farol crece y late mientras su luz baja por el vidrio; un leve balanceo.
                    float p = _glowMs > 0f ? Mathf.Clamp01(_t / _glowMs) : 1f;
                    _pos = _to + new Vector2(0f, Mathf.Sin(_t * 0.001f * 2f * Mathf.PI * 1.6f) * 0.03f * _u);
                    angle = Mathf.Sin(_t * 0.001f * 2f * Mathf.PI * 1.1f) * 4f;
                    wings = 0.42f;
                    glowSize = Mathf.Lerp(GlowFlightU, GlowLandedU, Ease.Out(Mathf.Clamp01(p / 0.25f)));
                    alpha = _reduceMotion ? Mathf.Clamp01(_t / ReducedFadeMs) : 1f;
                    break;
                }

                case Phase.Leave:
                {
                    float p = Mathf.Clamp01(_t / Mathf.Max(1f, _leaveMs));
                    float rise = _reduceMotion ? 0f : Ease.In(p) * 1.4f * _u;
                    _pos = _to + new Vector2(0.35f * _u * Ease.In(p) * Mathf.Sign(_to.x - _from.x), rise);
                    angle = _reduceMotion ? 0f : 18f * Ease.In(p) * -Mathf.Sign(_to.x - _from.x);
                    wings = _reduceMotion ? 0.42f : 0.55f + (0.45f * Mathf.Abs(Mathf.Sin(_t * 0.001f * Mathf.PI * WingHz)));
                    glowSize = Mathf.Lerp(GlowLandedU, GlowFlightU * 0.6f, p);
                    alpha = 1f - Ease.In(p);
                    break;
                }
            }

            _angle = angle;
            _root.anchoredPosition = _pos;
            _root.localRotation = Quaternion.Euler(0f, 0f, _angle);
            _wings.rectTransform.localScale = new Vector3(1f, wings, 1f);
            float g = glowSize * _u * flicker;
            _glow.rectTransform.sizeDelta = new Vector2(g, g);
            Color halo = ProceduralArt.RevealHaloColor;
            _glow.color = new Color(halo.r, halo.g, halo.b, GlowAlpha * alpha);
            var tone = new Color(1f, 1f, 1f, alpha);
            if (_body.color != tone)
            {
                _body.color = tone;
                _wings.color = tone;
            }
        }

        private Vector2 Bezier(float t)
        {
            float a = 1f - t;
            return (a * a * _from) + (2f * a * t * _control) + (t * t * _to);
        }

        // Estela: un destello cada SparkleEveryMs en el farol (el pool descarta lo que no cabe).
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
                if (_t > 60f && _t < _flightMs - 60f)
                {
                    _fx.Sparkles(LanternPosition(), ProceduralArt.RevealHaloColor, 1, _u * 0.7f);
                    SparklesRequested++;
                }
            }
        }

        // Destellos al posarse, sobre el borde.
        private void Burst()
        {
            if (_fx == null || _reduceMotion)
            {
                return;
            }

            _fx.Sparkles(_to + new Vector2(0f, -0.15f * _u), ProceduralArt.RevealHaloColor, LandingSparkles, _u);
            SparklesRequested += LandingSparkles;
        }

        // Farol: bajo el centro del cuerpo, girado con él.
        private Vector2 LanternPosition()
        {
            float rad = _angle * Mathf.Deg2Rad;
            var offset = new Vector2(0f, -ProceduralArt.FireflyLanternBelowU * _u);
            return _pos + new Vector2((offset.x * Mathf.Cos(rad)) - (offset.y * Mathf.Sin(rad)), (offset.x * Mathf.Sin(rad)) + (offset.y * Mathf.Cos(rad)));
        }

        private Image NewImage(string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_root, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            return img;
        }
    }
}
