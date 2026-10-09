#nullable enable
using System;
using AF.Core.Board;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Vista de una cuia en uGUI (GDD 07 §4; guía visual v2, 4.1-4.3 y 6): ranura fija con su área táctil (02 §3.3: ancho
    /// de la ranura × alto de la cuia + 32 dp) y sombra de contacto sobre la repisa, y un cuerpo móvil con vidrio trasero y
    /// cavidad opaca, líquido (<see cref="LiquidGraphic"/>, shader <c>AF/UI/Liquid</c>), vidrio frontal, tapa de calabaza,
    /// aro de luz y destello. La vista nunca decide reglas: muestra lo que le dice el modelo o la cola de animación (02 §3.2).
    /// </summary>
    /// <remarks>
    /// <para>Input (02 §3.1): el touch-down solo avisa (la elevación empieza ya, 0 ms de latencia percibida) y la
    /// decisión se toma en el touch-up: soltar dentro del área táctil es un toque; soltar fuera lo cancela.</para>
    /// <para>La pose del cuerpo y la sombra solo se escriben cuando cambian, para no ensuciar el lienzo cada fotograma con
    /// la pantalla quieta (08 §5.4). Nada cambia en una cuia por la selección de otra (D-002).</para>
    /// <para>Mientras vierte, el cuerpo visual (no la ranura con su área táctil) pasa a la capa de vertido, delante de las
    /// vecinas, del marco del panel y del HUD (D-006, como Magic Sort; <see cref="MoveBodyTo"/>), y vuelve a su ranura al
    /// terminar (<see cref="ReturnBody"/>), en la misma posición de pantalla.</para>
    /// </remarks>
    public sealed class CuiaView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>Elevación de la selección: 16 dp (02 §4.1; 07 §4.3).</summary>
        public const float LiftDp = 16f;

        /// <summary>Altura extra del área táctil sobre la cuia, para alcanzarla elevada (02 §3.3).</summary>
        public const float TouchHeadroomDp = 32f;

        /// <summary>Deselección: 100 ms ease-in (02 §4.1).</summary>
        public const float LowerMs = 100f;

        /// <summary>Rechazo: sacudida horizontal de ±6 dp, 2 ciclos, 120 ms (02 §4.1).</summary>
        public const float ShakeMs = 120f;

        /// <summary>Amplitud de la sacudida (dp).</summary>
        public const float ShakeDp = 6f;

        /// <summary>Sellado completo: 450 ms (02 §4.1; storyboard de 07 §5.3).</summary>
        public const float SealMs = 450f;

        // Aro de sellado y celebración teñido de cielo_horizonte #FFE4A8 (revisión visual del 2026-10-07: el blanco aditivo
        // con poco alfa sobre el panel oscuro se leía gris).
        private static readonly Color RingColor = new Color(1f, 0xE4 / 255f, 0xA8 / 255f, 1f);

        // Brillo cálido detrás de cada cuia sellada durante la victoria (revisión visual del 2026-10-07): #FFE4A8 al 25 %.
        private static readonly Color AuraColor = new Color(1f, 0xE4 / 255f, 0xA8 / 255f, 1f);

        /// <summary>Alfa máximo del brillo de victoria detrás de una cuia sellada.</summary>
        public const float VictoryAuraAlpha = 0.25f;

        private RectTransform _root = null!;
        private RectTransform _body = null!;
        private Image _shadow = null!;
        private Image _aura = null!;
        private Image _back = null!;
        private LiquidGraphic _liquid = null!;
        private Image _front = null!;
        private Image _marks = null!;
        private Image _cap = null!;
        private Image _glow = null!;
        private Image _star = null!;
        private ProceduralArt _art = null!;
        private IBoardFx? _fx;
        private Color[] _doseColor = Array.Empty<Color>();
        private float[] _doseFill = Array.Empty<float>();
        private int[] _doseTint = Array.Empty<int>();
        private Action<int>? _onDown;
        private Action<int, bool>? _onUp;
        private Vector2 _shownOffset = new Vector2(float.NaN, float.NaN);
        private float _shownAngle;
        private float _shownShadow = -1f;
        private float _shownMarks = 1f;
        private float _shownAura = -1f;
        private int _glassPpu;
        private bool _glassBoost;
        private bool _bodyInFront;
        private int _bodySibling;

        private float _u = 24f;
        private float _liftMs = 120f;
        private bool _locked;
        private bool _liftTarget;
        private float _liftValue;
        private float _shakeElapsed = -1f;
        private float _sealElapsed = -1f;
        private bool _sealDropsDone;
        private float _fxDelay = -1f;
        private float _fxElapsed = -1f;
        private bool _sealed;

        /// <summary>Índice de la cuia en el tablero.</summary>
        public int Index { get; private set; }

        /// <summary>Capacidad (dosis).</summary>
        public int Capacity { get; private set; }

        /// <summary>Unidad u en dp (ancho de la cuia / 2, 07 §4.1).</summary>
        public float Unit => _u;

        /// <summary>Alto de la cuia en u.</summary>
        public float HeightU => CuiaGeometry.HeightU(Capacity);

        /// <summary>Ranura (raíz fija; posición del pie en el contenedor del tablero, origen arriba a la izquierda).</summary>
        public RectTransform Root => _root;

        /// <summary>Cuerpo móvil (elevación, sacudida, traslado e inclinación).</summary>
        public RectTransform Body => _body;

        /// <summary>Líquido de la cuia (shader <c>AF/UI/Liquid</c>).</summary>
        public LiquidGraphic Liquid => _liquid;

        /// <summary>Pie de la cuia en reposo, en coordenadas del contenedor del tablero.</summary>
        public Vector2 FootPosition => _root.anchoredPosition;

        /// <summary>Desplazamiento actual del cuerpo respecto a su ranura (esté en la ranura o en la capa de vertido).</summary>
        public Vector2 BodyOffset => float.IsNaN(_shownOffset.x) ? _body.anchoredPosition : _shownOffset;

        /// <summary>El cuerpo está en la capa de vertido (<see cref="MoveBodyTo"/>), delante del resto de la pantalla.</summary>
        public bool BodyInFront => _bodyInFront;

        /// <summary>Ángulo actual del cuerpo (grados; negativo = inclinada a la derecha).</summary>
        public float BodyAngle => _body.localEulerAngles.z > 180f ? _body.localEulerAngles.z - 360f : _body.localEulerAngles.z;

        /// <summary>Un trabajo de animación controla la pose del cuerpo.</summary>
        public bool Locked => _locked;

        /// <summary>Está sellada en la vista (tapa puesta).</summary>
        public bool Sealed => _sealed;

        /// <summary>El vidrio lleva el +10 % de brillo de la selección (solo el origen, D-002).</summary>
        public bool GlassBoosted => _glassBoost;

        /// <summary>Altura actual del líquido en dosis (suma de rellenos), para el chorro.</summary>
        public float LiquidLevel
        {
            get
            {
                float level = 0f;
                for (int k = 0; k < _doseFill.Length; k++)
                {
                    level += _doseFill[k];
                }

                return level;
            }
        }

        /// <summary>
        /// Crea la vista dentro de <paramref name="parent"/> (contenedor del tablero). <paramref name="onDown"/> recibe el
        /// touch-down y <paramref name="onUp"/> el touch-up con "soltado dentro del área táctil" (02 §3.1).
        /// </summary>
        public static CuiaView Create(RectTransform parent, int index, int capacity, ProceduralArt art, Action<int> onDown, Action<int, bool> onUp, IBoardFx? fx = null)
        {
            var go = new GameObject("Cuia" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(HitArea));
            go.transform.SetParent(parent, false);
            CuiaView view = go.AddComponent<CuiaView>();
            view.Build(index, capacity, art, onDown, onUp, fx);
            return view;
        }

        /// <summary>
        /// Coloca la ranura: <paramref name="foot"/> es el centro del pie en el contenedor (origen arriba a la izquierda,
        /// y hacia arriba), <paramref name="cuiaWidth"/> el ancho de la cuia y <paramref name="slotWidth"/> el de la ranura.
        /// </summary>
        public void SetGeometry(Vector2 foot, float cuiaWidth, float slotWidth)
        {
            _u = cuiaWidth / CuiaGeometry.WidthU;
            float h = HeightU * _u;
            _root.anchoredPosition = foot;
            _root.sizeDelta = new Vector2(slotWidth, h + TouchHeadroomDp);
            _body.sizeDelta = new Vector2(cuiaWidth, h);
            _liquid.SetUnit(_u);

            // Vidrio horneado a la densidad real de la pantalla (guía 4.1: ceil(u_dp × escala) px/u).
            int ppu = _art.CuiaPixelsPerUFor(_u);
            if (ppu != _glassPpu)
            {
                _glassPpu = ppu;
                _back.sprite = _art.CuiaBack(Capacity, ppu);
                _front.sprite = _art.CuiaFront(Capacity, ppu);
                _marks.sprite = _art.CuiaMarks(Capacity, ppu);
            }

            _aura.rectTransform.anchoredPosition = new Vector2(0f, HeightU * 0.5f * _u);
            _aura.rectTransform.sizeDelta = new Vector2(4.2f * _u, (HeightU + 2.2f) * _u);
            _cap.rectTransform.sizeDelta = new Vector2(1.10f * _u, 0.35f * _u);
            _glow.rectTransform.anchoredPosition = new Vector2(0f, (HeightU - 0.3f) * _u);
            _glow.rectTransform.sizeDelta = new Vector2(2.4f * _u, 2.4f * _u);
            _star.rectTransform.anchoredPosition = new Vector2(0f, (HeightU + 0.1f) * _u);
            _star.rectTransform.sizeDelta = new Vector2(40f, 40f);
            PlaceCapAtRest();
            _shownShadow = -1f;
            UpdateShadow();
        }

        /// <summary>Duración de la elevación (<c>anim_lift_ms</c> × factor de velocidad).</summary>
        public void SetLiftDuration(float ms)
        {
            _liftMs = Mathf.Max(1f, ms);
        }

        /// <summary>Muestra el contenido final de <paramref name="cuia"/> (sin animación).</summary>
        public void ShowContent(Cuia cuia, bool sealedNow)
        {
            int count = cuia.Count;
            for (int k = 0; k < Capacity; k++)
            {
                if (k < count)
                {
                    int tint = cuia.ColorAt(k);
                    SetDose(k, Palette.Tint(tint), 1f, tint);
                }
                else
                {
                    SetDose(k, Color.clear, 0f, 0);
                }
            }

            SetSealed(sealedNow);
        }

        /// <summary>Fija el color, el relleno (0..1 de la dosis) y el tinte (0 = agua base) de la dosis <paramref name="k"/>.</summary>
        public void SetDose(int k, Color color, float fill, int tint)
        {
            if ((uint)k >= (uint)Capacity)
            {
                return;
            }

            _doseColor[k] = color;
            _doseFill[k] = Mathf.Clamp01(fill);
            _doseTint[k] = tint;
            _liquid.SetDose(k, color, _doseFill[k], tint);
        }

        /// <summary>Color actual de la dosis <paramref name="k"/>.</summary>
        public Color DoseColor(int k)
        {
            return _doseColor[k];
        }

        /// <summary>Arranca la onda de la superficie (al caer una dosis; 08 §5.2).</summary>
        public void Ripple()
        {
            _liquid.StartRipple();
        }

        /// <summary>
        /// Onda de la superficie en función de un reloj de fuera (la intro, A14): edad en ms (&lt; 0 sin onda) y fase fija; no
        /// acumula estado y no avanza con <see cref="Tick"/> hasta <see cref="ReleaseRipple"/>.
        /// </summary>
        public void SetRippleAt(float ageMs, float phase)
        {
            _liquid.SetRippleAt(ageMs, phase);
        }

        /// <summary>Devuelve la onda al reloj del juego.</summary>
        public void ReleaseRipple()
        {
            _liquid.ReleaseRipple();
        }

        /// <summary>La onda la lleva un reloj de fuera (la intro).</summary>
        public bool RippleDriven => _liquid.RippleDriven;

        /// <summary>Onda en curso (tests).</summary>
        public bool Rippling => _liquid.Rippling;

        /// <summary>Edad (ms; −1 sin onda) y fase (radianes) de la onda (tests de determinismo, A14).</summary>
        public void RippleState(out float ageMs, out float phase)
        {
            ageMs = _liquid.RippleAgeMs;
            phase = _liquid.RipplePhase;
        }

        /// <summary>
        /// Lengua de líquido por la pared inferior del cuello mientras sale el chorro (0..1; revisión visual del
        /// 2026-10-07): une la superficie inclinada con el labio del que sale el chorro.
        /// </summary>
        public void SetTongue(float amount, Color color, int tint)
        {
            _liquid.SetTongue(amount, color, tint);
        }

        /// <summary>Las marcas de dosis se ven (se ocultan con la cuia inclinada).</summary>
        public bool MarksVisible => _shownMarks > 0.001f;

        /// <summary>Pone o quita la tapa sin animación.</summary>
        public void SetSealed(bool sealedNow)
        {
            _sealed = sealedNow;
            _sealElapsed = -1f;
            _cap.gameObject.SetActive(sealedNow);
            _glow.gameObject.SetActive(false);
            _star.gameObject.SetActive(false);
            _liquid.SetFlash(0f);
            PlaceCapAtRest();
        }

        /// <summary>
        /// Fotograma <paramref name="ms"/> del sellado (07 §5.3; guía 6.8): líquido +15 % 0-100 ms, caída de la tapa
        /// 100-250 ms con mini-rebote, y a los 250 ms aro de luz aditivo, destello en estrella y gotas.
        /// </summary>
        public void SealAnimationAt(float ms)
        {
            if (_sealElapsed < 0f || ms < _sealElapsed)
            {
                _sealDropsDone = false;
            }

            _sealed = true;
            _sealElapsed = ms;
            _liquid.SetFlash(ms < 100f ? Mathf.Sin(Mathf.PI * (ms / 100f)) * 0.15f : 0f);
            bool capVisible = ms >= 100f;
            _cap.gameObject.SetActive(capVisible);
            if (capVisible)
            {
                float t = Mathf.Clamp01((ms - 100f) / 150f);
                float drop = 0.6f * (1f - Ease.Out(t));
                float bounce = ms > 250f && ms < 350f ? 0.03f * Mathf.Sin(Mathf.PI * ((ms - 250f) / 100f)) : 0f;
                _cap.rectTransform.anchoredPosition = new Vector2(0f, (HeightU - 0.04f + drop + bounce) * _u);
            }

            bool ring = ms >= 250f && ms < SealMs;
            _glow.gameObject.SetActive(ring);
            _star.gameObject.SetActive(ring);
            if (ring)
            {
                float t = (ms - 250f) / (SealMs - 250f);
                _glow.color = new Color(RingColor.r, RingColor.g, RingColor.b, 0.9f * (1f - (t * t)));
                _glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.25f, Ease.Out(t));
                float star = t < 0.4f ? Mathf.Lerp(0f, 1.2f, t / 0.4f) : Mathf.Lerp(1.2f, 0f, (t - 0.4f) / 0.6f);
                _star.rectTransform.localScale = Vector3.one * star;
                _star.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f * t);
                _star.color = new Color(1f, 0.98f, 0.9f, Mathf.Clamp01(star));
                if (!_sealDropsDone && _fx != null)
                {
                    _sealDropsDone = true;
                    Color tint = _doseColor.Length > 0 ? _doseColor[Capacity - 1] : Color.white;
                    _fx.SealDrops(FootPosition + BodyOffset + new Vector2(0f, HeightU * _u), tint, _u);
                }
            }

            if (ms >= SealMs)
            {
                SetSealed(true);
            }
        }

        /// <summary>Elevación de la selección (02 §4.1); se aplica cuando ningún trabajo controla el cuerpo.</summary>
        public void SetLifted(bool lifted)
        {
            _liftTarget = lifted;
            SetGlassBoost(lifted && !_locked);
        }

        /// <summary>La cuia está elevada o subiendo (selección o touch-down en curso).</summary>
        public bool IsLifted => _liftTarget;

        /// <summary>Rechazo suave: sacudida (02 §4.1, C2, C6). Sin rojo, sin texto y sin cambiar de color (guía 6.3).</summary>
        public void Shake()
        {
            _shakeElapsed = 0f;
        }

        /// <summary>Celebración (07 §5.4): tras <paramref name="delayMs"/>, aro de luz si está sellada y salto de 0,15 u.</summary>
        public void PlayCelebration(float delayMs)
        {
            _fxDelay = Mathf.Max(0f, delayMs);
            _fxElapsed = -1f;
        }

        /// <summary>Toma el control de la pose del cuerpo (trabajo de vertido o intro).</summary>
        public void Lock()
        {
            _locked = true;
            _shakeElapsed = -1f;
            SetGlassBoost(false);
        }

        /// <summary>Fija la pose del cuerpo mientras está bloqueada.</summary>
        public void SetPose(Vector2 offset, float angleDeg)
        {
            WritePose(offset, angleDeg);
        }

        /// <summary>
        /// Lleva el cuerpo visual a <paramref name="layer"/> (la capa de vertido, alineada con el contenedor del tablero y
        /// delante de todo, D-006) conservando su posición en pantalla. La ranura, con su área táctil y su sombra, no se
        /// mueve. Idempotente; sin asignaciones.
        /// </summary>
        public void MoveBodyTo(RectTransform layer)
        {
            if (_bodyInFront)
            {
                return;
            }

            _bodyInFront = true;
            _body.SetParent(layer, false);
            _body.anchorMin = new Vector2(0f, 1f);
            _body.anchorMax = new Vector2(0f, 1f);
            _body.SetAsLastSibling();
            _body.anchoredPosition = _root.anchoredPosition + BodyOffsetOrZero();
        }

        /// <summary>Devuelve el cuerpo visual a su ranura (mismo índice de hermano), en la misma posición de pantalla.</summary>
        public void ReturnBody()
        {
            if (!_bodyInFront)
            {
                return;
            }

            _bodyInFront = false;
            _body.SetParent(_root, false);
            _body.SetSiblingIndex(_bodySibling);
            _body.anchorMin = new Vector2(0.5f, 0f);
            _body.anchorMax = new Vector2(0.5f, 0f);
            _body.anchoredPosition = BodyOffsetOrZero();
        }

        /// <summary>Devuelve el cuerpo a su ranura y libera el control (la elevación arranca desde 0).</summary>
        public void Unlock()
        {
            _locked = false;
            _liftValue = 0f;
            WritePose(Vector2.zero, 0f);
            SetGlassBoost(_liftTarget);
        }

        /// <summary>
        /// Canto interior de la boca por el que sale el chorro, medido desde el eje (u): 0,34 u, dentro del cuello interior
        /// de <see cref="CuiaGeometry.NeckInnerHalfWidthU"/> (0,40 u).
        /// </summary>
        public const float PourEdgeU = 0.34f;

        /// <summary>
        /// Centro de la boca (centro de lo alto del borde de fibra, en el eje a <see cref="HeightU"/>) en coordenadas del
        /// contenedor, con la pose actual. Durante el caudal queda sobre el eje del destino (D-006).
        /// </summary>
        public Vector2 MouthCenterInContainer()
        {
            return FootPosition + BodyOffset + Rotate(new Vector2(0f, HeightU * _u), BodyAngle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Punto más bajo de la boca con la pose actual: el canto de lo alto del borde de fibra
        /// (<see cref="CuiaGeometry.RimHalfWidthU"/>) del lado bajo, en coordenadas del contenedor. Sin inclinación, el
        /// centro de la boca. D-006: queda a 0,20-0,60 u sobre el borde del destino mientras vierte.
        /// </summary>
        public Vector2 MouthLowestInContainer()
        {
            float deg = BodyAngle;
            if (Mathf.Abs(deg) < 0.5f)
            {
                return MouthCenterInContainer();
            }

            float side = deg > 0f ? -1f : 1f; // el lado bajo: a la izquierda si la cuia gira en sentido antihorario
            return FootPosition + BodyOffset + Rotate(new Vector2(side * CuiaGeometry.RimHalfWidthU * _u, HeightU * _u), deg * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Punto de la boca por el que sale el chorro: el canto interior del lado bajo (<see cref="PourEdgeU"/>, donde acaba
        /// la lengua de líquido), a 0,21 u del punto más bajo de la boca (<see cref="MouthLowestInContainer"/>), en
        /// coordenadas del contenedor, con la pose actual. Sin inclinación, el centro de la boca.
        /// </summary>
        public Vector2 PourPointInContainer()
        {
            float deg = BodyAngle;
            if (Mathf.Abs(deg) < 0.5f)
            {
                return MouthCenterInContainer();
            }

            float angle = deg * Mathf.Deg2Rad;
            float side = deg > 0f ? -1f : 1f;
            return FootPosition + BodyOffset + Rotate(new Vector2(side * PourEdgeU * _u, HeightU * _u), angle);
        }

        /// <summary>
        /// Dirección (unitaria) hacia fuera del labio por el lado bajo, con la pose actual: la del chorro al salir
        /// (revisión visual del 2026-10-07). Sin inclinación, hacia abajo.
        /// </summary>
        public Vector2 PourOutward()
        {
            float deg = BodyAngle;
            if (Mathf.Abs(deg) < 0.5f)
            {
                return Vector2.down;
            }

            float side = deg > 0f ? -1f : 1f;
            return Rotate(new Vector2(side, 0f), deg * Mathf.Deg2Rad);
        }

        /// <summary>Eje de la cuia (unitario, del pie al labio) con la pose actual.</summary>
        public Vector2 AxisUp()
        {
            return Rotate(Vector2.up, BodyAngle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Brillo cálido aditivo detrás de la cuia (0..1 de <see cref="VictoryAuraAlpha"/>), solo en la victoria y solo si
        /// está sellada: es posterior al juego y no depende de la selección ni de la legalidad (D-002).
        /// </summary>
        public void SetVictoryAura(float amount)
        {
            float a = _sealed ? Mathf.Clamp01(amount) : 0f;
            if (Mathf.Abs(a - _shownAura) < 0.002f)
            {
                return;
            }

            _shownAura = a;
            bool on = a > 0.002f;
            if (_aura.gameObject.activeSelf != on)
            {
                _aura.gameObject.SetActive(on);
            }

            _aura.color = new Color(AuraColor.r, AuraColor.g, AuraColor.b, VictoryAuraAlpha * a);
        }

        /// <summary>El brillo de victoria se ve detrás de la cuia (tests).</summary>
        public bool VictoryAuraVisible => _aura.gameObject.activeSelf;

        /// <summary>Gira <paramref name="v"/> <paramref name="radians"/> en sentido antihorario.</summary>
        public static Vector2 Rotate(Vector2 v, float radians)
        {
            float c = Mathf.Cos(radians);
            float s = Mathf.Sin(radians);
            return new Vector2((v.x * c) - (v.y * s), (v.x * s) + (v.y * c));
        }

        /// <summary>Superficie del líquido en reposo, en coordenadas del contenedor.</summary>
        public Vector2 SurfaceInContainer()
        {
            return FootPosition + new Vector2(0f, (CuiaGeometry.CavityBottomU + LiquidLevel) * _u);
        }

        /// <summary>Avanza las animaciones propias (elevación, sacudida, celebración, onda).</summary>
        public void Tick(float dtMs)
        {
            if (!_locked)
            {
                float target = _liftTarget ? 1f : 0f;
                if (!Mathf.Approximately(_liftValue, target))
                {
                    float duration = _liftTarget ? _liftMs : LowerMs;
                    _liftValue = Mathf.MoveTowards(_liftValue, target, dtMs / duration);
                }

                float x = 0f;
                if (_shakeElapsed >= 0f)
                {
                    _shakeElapsed += dtMs;
                    float p = _shakeElapsed / ShakeMs;
                    if (p >= 1f)
                    {
                        _shakeElapsed = -1f;
                    }
                    else
                    {
                        x = ShakeDp * Mathf.Sin(p * 4f * Mathf.PI);
                    }
                }

                // Subida con rebote (easeOutBack: pico de ≈ 17,6 dp hacia t ≈ 0,58 y asiento en 16; guía 6.1) y bajada
                // ease-in (6.2).
                float y = LiftDp * (_liftTarget ? Ease.OutBack(_liftValue) : Ease.In(_liftValue));
                y += HopOffset(dtMs);
                WritePose(new Vector2(x, y), 0f);
            }
            else
            {
                HopOffset(dtMs);
            }

            _liquid.TickRipple(dtMs);
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            _onDown?.Invoke(Index);
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData)
        {
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(_root, eventData.position, eventData.pressEventCamera);
            _onUp?.Invoke(Index, inside);
        }

        // Solo toca el RectTransform si la pose cambió: con la pantalla quieta el lienzo no se reconstruye (08 §5.4).
        private void WritePose(Vector2 offset, float angleDeg)
        {
            if (offset == _shownOffset && angleDeg == _shownAngle)
            {
                return;
            }

            _shownOffset = offset;
            _shownAngle = angleDeg;
            _body.anchoredPosition = _bodyInFront ? _root.anchoredPosition + offset : offset;
            _body.localRotation = angleDeg == 0f ? Quaternion.identity : Quaternion.Euler(0f, 0f, angleDeg);
            _liquid.SetTilt(angleDeg); // el líquido no rota (guía 6.4)
            UpdateMarks(angleDeg);
            UpdateShadow();
        }

        // Las marcas de dosis giran con el vidrio: con la cuia inclinada cruzarían en diagonal las capas horizontales del
        // líquido, así que se apagan en los primeros 6° (revisión visual del 2026-10-07).
        private void UpdateMarks(float angleDeg)
        {
            float a = 1f - Mathf.Clamp01(Mathf.Abs(angleDeg) / 6f);
            if (Mathf.Abs(a - _shownMarks) < 0.001f)
            {
                return;
            }

            _shownMarks = a;
            _marks.color = new Color(1f, 1f, 1f, a);
        }

        // Sombra de contacto (guía 4.1): en reposo 1,90 × 0,28 u con alfa 0,55 (ΔL* ≥ 15 sobre la repisa); con la cuia
        // elevada se queda en la repisa y se encoge a 1,50 × 0,22 u con alfa 0,25.
        private void UpdateShadow()
        {
            float away = float.IsNaN(_shownOffset.x) ? 0f : Mathf.Clamp01(_shownOffset.magnitude / LiftDp);
            if (Mathf.Abs(away - _shownShadow) < 0.001f)
            {
                return;
            }

            _shownShadow = away;
            float w = Mathf.Lerp(1.90f, 1.50f, away) * _u;
            float h = Mathf.Lerp(0.28f, 0.22f, away) * _u * 2.6f; // el sprite deja borde suave alrededor del núcleo
            _shadow.rectTransform.sizeDelta = new Vector2(w * 1.35f, h);
            _shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.55f, 0.25f, away));
        }

        private void SetGlassBoost(bool on)
        {
            if (_glassBoost == on)
            {
                return;
            }

            _glassBoost = on;
            Material? mat = on ? UiMaterials.GlassSelected : UiMaterials.Glass;
            if (mat != null)
            {
                _front.material = mat;
            }
        }

        private float HopOffset(float dtMs)
        {
            if (_fxDelay >= 0f)
            {
                _fxDelay -= dtMs;
                if (_fxDelay < 0f)
                {
                    _fxDelay = -1f;
                    _fxElapsed = 0f;
                }

                return 0f;
            }

            if (_fxElapsed < 0f)
            {
                return 0f;
            }

            _fxElapsed += dtMs;
            const float hopMs = 240f;
            const float glowMs = 300f;
            if (_sealed)
            {
                // Aro cálido (#FFE4A8) y destello en estrella de 40 dp como en el sellado (guía 6.8; revisión visual del
                // 2026-10-07: el aro blanco con poco alfa se leía gris). El aro se apaga con 1 − t² para que se vea entero.
                bool on = _fxElapsed < glowMs;
                _glow.gameObject.SetActive(on);
                _star.gameObject.SetActive(on);
                if (on)
                {
                    float t = _fxElapsed / glowMs;
                    _glow.color = new Color(RingColor.r, RingColor.g, RingColor.b, 0.9f * (1f - (t * t)));
                    _glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.25f, Ease.Out(t));
                    float star = t < 0.4f ? Mathf.Lerp(0f, 1.2f, t / 0.4f) : Mathf.Lerp(1.2f, 0f, (t - 0.4f) / 0.6f);
                    _star.rectTransform.localScale = Vector3.one * star;
                    _star.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f * t);
                    _star.color = new Color(1f, 0.98f, 0.9f, Mathf.Clamp01(star));
                }
            }

            if (_fxElapsed >= Mathf.Max(hopMs, glowMs))
            {
                _fxElapsed = -1f;
                _glow.gameObject.SetActive(false);
                _star.gameObject.SetActive(false);
                return 0f;
            }

            return _fxElapsed < hopMs ? 0.15f * _u * Mathf.Sin(Mathf.PI * (_fxElapsed / hopMs)) : 0f;
        }

        private Vector2 BodyOffsetOrZero()
        {
            return float.IsNaN(_shownOffset.x) ? Vector2.zero : _shownOffset;
        }

        // Si la vista se destruye con el cuerpo en la capa de vertido (no debería: la cola lo devuelve antes), el cuerpo
        // no queda huérfano en esa capa.
        private void OnDestroy()
        {
            if (_bodyInFront && _body != null)
            {
                Destroy(_body.gameObject);
            }
        }

        private void PlaceCapAtRest()
        {
            _cap.rectTransform.anchoredPosition = new Vector2(0f, (HeightU - 0.04f) * _u);
        }

        private void Build(int index, int capacity, ProceduralArt art, Action<int> onDown, Action<int, bool> onUp, IBoardFx? fx)
        {
            Index = index;
            Capacity = capacity;
            _art = art;
            _fx = fx;
            _onDown = onDown;
            _onUp = onUp;
            _root = (RectTransform)transform;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0.5f, 0f);
            GetComponent<HitArea>().raycastTarget = true;

            // Sombra de contacto: en la ranura (no en el cuerpo), así se queda en la repisa al elevarse.
            _shadow = NewImage("ContactShadow", _root, art.ContactShadow, new Color(0f, 0f, 0f, 0.55f));
            SetAnchors(_shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f));
            _shadow.rectTransform.anchoredPosition = new Vector2(0f, -0.5f);

            // Brillo de victoria detrás del cuerpo (apagado salvo en la victoria con la cuia sellada).
            _aura = NewImage("VictoryAura", _root, art.Glow, AuraColor);
            SetAnchors(_aura.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f));
            _aura.gameObject.SetActive(false);

            _body = NewRect("Body", _root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            _bodySibling = _body.GetSiblingIndex();

            _glassPpu = ProceduralArt.CuiaPixelsPerU;
            _back = NewImage("GlassBack", _body, art.CuiaBack(capacity, _glassPpu), Color.white);
            Stretch(_back.rectTransform);

            var liquidGo = new GameObject("Liquid", typeof(RectTransform), typeof(CanvasRenderer), typeof(LiquidGraphic));
            liquidGo.transform.SetParent(_body, false);
            _liquid = liquidGo.GetComponent<LiquidGraphic>();
            RectTransform lrt = _liquid.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            _liquid.Setup(capacity, art.PatternAtlas);
            _doseColor = new Color[capacity];
            _doseFill = new float[capacity];
            _doseTint = new int[capacity];

            _front = NewImage("GlassFront", _body, art.CuiaFront(capacity, _glassPpu), Color.white);
            Stretch(_front.rectTransform);
            Material? glass = UiMaterials.Glass;
            if (glass != null)
            {
                _front.material = glass;
            }

            _marks = NewImage("DoseMarks", _body, art.CuiaMarks(capacity, _glassPpu), Color.white);
            Stretch(_marks.rectTransform);

            _cap = NewImage("SealCap", _body, art.SealCap, Color.white);
            SetAnchors(_cap.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            _cap.gameObject.SetActive(false);

            _glow = NewImage("SealRing", _body, art.Ring, RingColor);
            SetAnchors(_glow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f));
            _glow.gameObject.SetActive(false);
            _star = NewImage("SealStar", _body, art.Star, Color.white);
            SetAnchors(_star.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f));
            _star.gameObject.SetActive(false);
            Material? additive = UiMaterials.Additive;
            if (additive != null)
            {
                _glow.material = additive;
                _star.material = additive;
                _aura.material = additive;
            }
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            SetAnchors(rt, anchorMin, anchorMax, pivot);
            return rt;
        }

        private static Image NewImage(string name, Transform parent, Sprite? sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
