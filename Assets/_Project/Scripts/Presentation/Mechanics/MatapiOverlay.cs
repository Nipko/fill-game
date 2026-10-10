#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M10 Matapi (D-007; propuesta b.2 M10) [P]: un cilindro de 6 varillas de fibra semitransparentes (tonos
    /// <c>fibra_*</c>) visto de frente, con las varillas juntas hacia los bordes y el centro libre (ninguna a menos de
    /// <see cref="RodClearHalfU"/> del eje: ni rayas verticales sobre la fruta, que son el patrón del Camu camu, ni rejilla
    /// con los aros, que es la del Huito, ni barrotes sobre el parche central de VA-05 o el icono de fruta), atadas con tres
    /// aros de cuerda sobre las juntas de dosis; en la boca, un embudo de fibra ancho hacia dentro, con el canto interior
    /// claro y el hueco oscuro: la forma ya dice «entra». El contenido se ve entre las varillas. Nunca se abre: al sellarse,
    /// el embudo queda cerrado sobre el borde, bajo la tapa, y el cuello de vidrio despejado como en las demás cuias selladas.
    /// </summary>
    /// <remarks>
    /// <para><b>Rechazo</b> (R11, sin texto ni color de error; D-002): si se toca como origen, la cuia se sacude y el embudo
    /// late con la sacudida (escala 1 → <see cref="FunnelPulseScale"/> → 1 en <see cref="PulseMs"/>).</para>
    /// <para><b>Sellado</b> (b.2 M10: «el embudo se cierra con un tirón de cuerda y la trampa sale del agua entre gotas y un
    /// brillo»; solo como receptor, así que la cola lo despacha como paso Seal de esta cuia): dura <see cref="SealMs"/> ×
    /// factor de velocidad. Fases [P] en fracción del paso: 0-0,08 asoma la cuerda, floja (inclinada y con catenaria);
    /// 0,10-0,30 el tirón la tensa y el aro ciñe el embudo, que se cierra (con rebote) y sube sobre el borde; 0,18-0,62 la
    /// trampa sube <see cref="TrapLiftU"/> y vuelve, con gotas de agua (<c>Fx.Splash</c>) al salir y destellos dorados
    /// (<c>Fx.Sparkles</c>) en la boca; 0,55-0,86 la tapa de calabaza cae con un mini-rebote, como la del sellado normal
    /// (07 §5.3), y el aro de cuerda se apaga bajo ella; 0,70-1 aparece un brillo cálido. Al terminar, la cola pinta
    /// <c>ShowContent(After, true)</c> y la tapa de la cuia queda donde cayó la de este paso. No se muestra ninguna
    /// captura.</para>
    /// <para>Todo se crea en <see cref="OnAttach"/>; <see cref="Tick"/>, <see cref="Show"/> y los pasos no asignan memoria y
    /// solo escriben lo que cambia (08 §5.4). Sin pasos ni sacudida, nada se mueve (D-002).</para>
    /// </remarks>
    public sealed class MatapiOverlay : CuiaOverlay
    {
        /// <summary>Varillas del cilindro: tres a cada lado del eje.</summary>
        public const int RodCount = 6;

        /// <summary>Opacidad de las varillas de los lados (dejan ver el contenido).</summary>
        public const float RodAlpha = 0.6f;

        /// <summary>Opacidad del par de varillas más cercano al eje, sobre la fruta.</summary>
        public const float InnerRodAlpha = 0.45f;

        /// <summary>Medio ancho del centro libre de varillas (u desde el eje).</summary>
        public const float RodClearHalfU = 0.45f;

        /// <summary>Distancia del eje a una varilla del todo de lado (u): x = esto × sen(ángulo).</summary>
        public const float RodRadiusU = 0.86f;

        /// <summary>Sellado del Matapi (ms, sin factor de velocidad) [P b.2 M10: ~800 ms].</summary>
        public const float SealMs = 800f;

        /// <summary>Escala del embudo en el pico del latido del rechazo [P].</summary>
        public const float FunnelPulseScale = 1.15f;

        /// <summary>El latido dura lo que la sacudida (02 §4.1: 120 ms).</summary>
        public const float PulseMs = CuiaView.ShakeMs;

        /// <summary>Ancho del embudo cerrado, en fracción del abierto [P]: 1,02 u, el del borde de la cuia (1,10 u) bajo la tapa.</summary>
        public const float ClosedFunnelScale = 0.6f;

        /// <summary>Alto del embudo cerrado, en fracción del abierto [P]: cabe entero sobre el borde y bajo la tapa.</summary>
        public const float ClosedFunnelScaleY = 0.45f;

        /// <summary>Ancho del aro de cuerda con el embudo cerrado, en fracción del suyo [P].</summary>
        public const float ClosedTieScale = 0.55f;

        /// <summary>Ancho del aro de cuerda al empezar el tirón (embudo abierto), en fracción del suyo [P].</summary>
        public const float OpenTieScale = 1.0f;

        /// <summary>Aros de cuerda del cilindro: abajo, en medio y arriba.</summary>
        public const int BandCount = 3;

        /// <summary>Lo que sube la trampa en el sellado (u) [P].</summary>
        public const float TrapLiftU = 0.18f;

        /// <summary>Altura desde la que cae la tapa (u sobre su sitio), como en el sellado normal (07 §5.3).</summary>
        public const float CapDropU = 0.6f;

        /// <summary>Destellos de la boca al salir del agua.</summary>
        public const int SealSparkles = 6;

        /// <summary>Inclinación de la cuerda floja, antes del tirón (grados) [P].</summary>
        public const float CordSlackTiltDeg = 22f;

        /// <summary>Inclinación de la cuerda tensa, tras el tirón (grados) [P].</summary>
        public const float CordTautTiltDeg = 18f;

        // Medidas (u) de la boca, del aro y de la tapa. La tapa es la de CuiaView (1,10 × 0,35 u, base a HeightU − 0,04) y
        // el borde de la cuia mide 0,25 u. Abierta, la boca se centra 0,12 u bajo lo alto: el aro queda sobre el borde y el
        // cono entra por el cuello. Cerrada (0,6 × 0,45), se centra 0,03 u bajo lo alto: de HeightU − 0,24 a HeightU + 0,18,
        // entera sobre el borde y bajo la tapa, sin colgar en el cuello. El aro de cuerda sube con ella.
        private const float FunnelBelowTopU = 0.12f;
        private const float ClosedFunnelBelowTopU = 0.03f;
        private const float TieBelowTopU = 0.36f;
        private const float ClosedTieBelowTopU = 0.17f;
        private const float ClosedTieScaleY = 0.7f;
        private const float CordStretch = 0.06f;
        private const float CapWidthU = 1.10f;
        private const float CapHeightU = 0.35f;
        private const float CapSinkU = 0.04f;
        private const float BottomHoopU = 0.42f;

        // Fases del sellado (fracción del paso) [P].
        private const float CordInEnd = 0.08f;
        private const float TugStart = 0.10f;
        private const float TugEnd = 0.30f;
        private const float CordFadeStart = 0.40f;
        private const float CordFadeEnd = 0.55f;
        private const float LiftStart = 0.18f;
        private const float LiftEnd = 0.62f;
        private const float SplashAt = 0.22f;
        private const float SparkleAt = 0.45f;
        private const float CapStart = 0.55f;
        private const float CapLand = 0.74f;
        private const float CapBounceEnd = 0.86f;
        private const float GlowStart = 0.70f;
        private const float TieFadeStart = CapLand;
        private const float TieFadeEnd = CapBounceEnd;

        // Brillo cálido del sellado: cielo_horizonte #FFE4A8, como el aro de CuiaView.
        private static readonly Color WarmLight = new Color(1f, 0xE4 / 255f, 0xA8 / 255f, 1f);

        // Gotas de agua clara al salir del agua (vidrio teñido #CFEFE6).
        private static readonly Color WaterDrop = Palette.GlassTint;

        private readonly Image[] _rods = new Image[RodCount];
        private readonly Image[] _hoops = new Image[BandCount];
        private RectTransform _trap = null!;
        private Image _funnel = null!;
        private Image _tie = null!;
        private Image _cord = null!;
        private Sprite _cordSlack = null!;
        private Sprite _cordTaut = null!;
        private Image _cap = null!;
        private Image _glow = null!;
        private Image _star = null!;
        private float _u = 24f;
        private float _heightU = CuiaGeometry.HeightU(4);
        private bool _closedLook;
        private float _close;
        private float _pulse = 1f;
        private float _pulseElapsed = -1f;
        private bool _active;
        private float _elapsed;
        private float _duration = 1f;
        private float _progress;
        private bool _splashed;
        private bool _sparkled;
        private bool _sealTarget;
        private Vector2 _selfPos;
        private IBoardFx? _fx;
        private float _lift;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Matapi;

        /// <inheritdoc />
        public override bool IsAnimating => _active;

        /// <summary>Escala actual del latido del embudo (1 en reposo).</summary>
        public float FunnelPulse => _pulse;

        /// <summary>El embudo se ve cerrado (Matapi sellado).</summary>
        public bool FunnelClosed => _closedLook;

        /// <summary>Cierre actual del embudo (0 abierto, 1 cerrado; el tirón rebota algo por encima de 1).</summary>
        public float FunnelClose => _close;

        /// <summary>Lo que la trampa está subida ahora mismo (u).</summary>
        public float TrapLift => _lift;

        /// <summary>Fracción del paso de sellado en curso (0..1; 0 sin paso).</summary>
        public float SealProgress => _active ? _progress : 0f;

        /// <summary>La tapa de este paso está a la vista (cae antes de que la cola ponga la de la cuia).</summary>
        public bool StepCapVisible => _cap.gameObject.activeSelf;

        /// <summary>El brillo del sellado está a la vista.</summary>
        public bool GlowVisible => _glow.gameObject.activeSelf;

        /// <summary>La cuerda del tirón está a la vista.</summary>
        public bool CordVisible => _cord.gameObject.activeSelf;

        /// <summary>La cuerda del tirón está floja (con catenaria, antes del tirón).</summary>
        public bool CordSlack => _cord.sprite == _cordSlack;

        /// <summary>Inclinación actual de la cuerda del tirón (grados desde la vertical, hacia la derecha).</summary>
        public float CordTiltDeg => -Mathf.DeltaAngle(0f, _cord.rectTransform.localEulerAngles.z);

        /// <summary>El aro de cuerda del embudo está a la vista (solo durante el sellado).</summary>
        public bool TieVisible => _tie.gameObject.activeSelf && _tie.color.a > 0.001f;

        /// <summary>Centro del embudo (u desde el pie, sin la subida de la trampa).</summary>
        public float FunnelCenterU => _funnel.rectTransform.anchoredPosition.y / _u;

        /// <summary>Alto actual del embudo (u, con su escala).</summary>
        public float FunnelHeightU => ProceduralArt.MatapiMouthHeightU * _funnel.rectTransform.localScale.y;

        /// <summary>Posición en x (u desde el eje) de la varilla <paramref name="i"/>: tres a cada lado, juntas hacia los bordes.</summary>
        public static float RodXU(int i)
        {
            return RodRadiusU * RodSide(i);
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Matapi;
            Visible = on;
            for (int i = 0; i < RodCount; i++)
            {
                OverlayParts.SetVisible(_rods[i], on);
            }

            for (int i = 0; i < BandCount; i++)
            {
                OverlayParts.SetVisible(_hoops[i], on);
            }

            OverlayParts.SetVisible(_funnel, on);
            _closedLook = on && sealedNow;
            _close = _closedLook ? 1f : 0f;
            HideStepParts();
            SetLift(0f);
            ApplyFunnel();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            float bottom = CuiaGeometry.FootTopU;
            float top = CuiaGeometry.CavityBottomU + Host.Capacity + 0.1f;
            for (int i = 0; i < RodCount; i++)
            {
                float side = RodSide(i);
                OverlayParts.Place(_rods[i], RodRadiusU * side, (top + bottom) * 0.5f, Mathf.Lerp(0.10f, 0.07f, Mathf.Abs(side)), top - bottom, u);
            }

            float bandH = ProceduralArt.MatapiBandHeightU;
            OverlayParts.Place(_hoops[0], 0f, BottomHoopU, ProceduralArt.MatapiBandWidthU, bandH, u);
            OverlayParts.Place(_hoops[1], 0f, (BottomHoopU + top) * 0.5f, ProceduralArt.MatapiBandWidthU, bandH, u);
            OverlayParts.Place(_hoops[2], 0f, top - 0.14f, ProceduralArt.MatapiBandWidthU, bandH, u);
            OverlayParts.Place(_funnel, 0f, heightU - FunnelBelowTopU, ProceduralArt.MatapiMouthWidthU, ProceduralArt.MatapiMouthHeightU, u);
            OverlayParts.Place(_tie, 0f, heightU - TieBelowTopU, ProceduralArt.MatapiTieWidthU, ProceduralArt.MatapiTieHeightU, u);
            RectTransform cord = _cord.rectTransform; // pivote en el pie: crece hacia arriba desde el aro (ApplyFunnel lo sigue)
            var cordSize = new Vector2(ProceduralArt.MatapiCordSpriteWidthU * u, ProceduralArt.MatapiCordLengthU * u);
            if (cord.sizeDelta != cordSize)
            {
                cord.sizeDelta = cordSize;
            }

            OverlayParts.Place(_glow, 0f, heightU, 2.6f, 2.6f, u);
            OverlayParts.Place(_star, 0f, heightU + 0.1f, 1.6f, 1.6f, u);
            PlaceCap(0f);
            SetLift(_lift, true);
            ApplyFunnel();
            if (_active)
            {
                Render(_progress);
            }
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            if (_active && Host.Board != null && (uint)Host.Index < (uint)Host.Board.Count)
            {
                _selfPos = Host.Board.Foot(Host.Index);
            }
        }

        /// <inheritdoc />
        public override void OnRejected()
        {
            if (Visible)
            {
                _pulseElapsed = 0f;
            }
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (_pulseElapsed >= 0f)
            {
                _pulseElapsed += dtMs;
                if (_pulseElapsed >= PulseMs)
                {
                    _pulseElapsed = -1f;
                    _pulse = 1f;
                }
                else
                {
                    _pulse = 1f + ((FunnelPulseScale - 1f) * Mathf.Sin(Mathf.PI * (_pulseElapsed / PulseMs)));
                }

                ApplyFunnel();
            }

            if (_active)
            {
                _elapsed = Mathf.Min(_duration, _elapsed + dtMs);
                Render(_elapsed / _duration);
            }
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Seal ? SealMs : 0f;
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            if (ctx.Kind != MechanicStepKind.Seal || ctx.Before.Mechanic != MechanicType.Matapi)
            {
                return 0f;
            }

            float ms = SealMs * ctx.SpeedFactor;
            if (ms <= 0f)
            {
                return 0f;
            }

            _active = true;
            _duration = ms;
            _elapsed = 0f;
            _progress = 0f;
            _splashed = false;
            _sparkled = false;
            _sealTarget = ctx.AfterSealed;
            _selfPos = ctx.SelfPos;
            _fx = ctx.Fx ?? Host.Fx;
            _closedLook = false;
            OverlayParts.SetColor(_tie, Color.white);
            Render(0f);
            return ms;
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _progress = 0f;
            _closedLook = Visible && _sealTarget;
            _close = _closedLook ? 1f : 0f;
            HideStepParts();
            SetLift(0f);
            ApplyFunnel();
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            // Varillas, aros, embudo, aro de cuerda y cuerda son de fibra (fibra_*): excepción de la regla 1 de
            // direccion-visual-v2 §2.4. El hueco oscuro de la boca (unos 1,5 × 0,3 u sobre la boca de la cuia) no es de fibra
            // y cuenta. La tapa y el brillo solo se ven durante el paso de sellado.
            into.Add(ProceduralArt.MatapiHole);
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            ProceduralArt art = Host.Art;
            _trap = OverlayParts.NewAnchor("MatapiTrap", Host.BodyFront);
            for (int i = 0; i < RodCount; i++)
            {
                // El par de dentro, claro y más transparente (pasa sobre la fruta); hacia los lados, en sombra (la luz viene
                // de delante).
                float side = Mathf.Abs(RodSide(i));
                bool inner = side < 0.7f;
                Color fiber = inner ? Palette.FiberLight : (side < 0.9f ? Palette.FiberBase : Palette.FiberShadow);
                _rods[i] = OverlayParts.NewImage("MatapiRod" + i, _trap, art.MatapiRod, OverlayParts.Fade(fiber, inner ? InnerRodAlpha : RodAlpha));
            }

            for (int i = 0; i < BandCount; i++)
            {
                _hoops[i] = OverlayParts.NewImage("MatapiBand" + i, _trap, art.MatapiBand, Color.white);
            }

            _funnel = OverlayParts.NewImage("MatapiMouth", _trap, art.MatapiMouth, Color.white);
            _tie = OverlayParts.NewImage("MatapiTie", _trap, art.MatapiTieRing, Color.white);
            _cordSlack = art.MatapiPullCordSlack;
            _cordTaut = art.MatapiPullCord;
            _cord = OverlayParts.NewImage("MatapiCord", _trap, _cordSlack, Color.white);
            _cord.rectTransform.pivot = new Vector2(0.5f, 0f);

            // Lo que no sube con la trampa: la tapa del paso y el brillo (aditivo, sin Bloom).
            _cap = OverlayParts.NewImage("MatapiStepCap", Host.BodyFront, art.SealCap, Color.white);
            _glow = OverlayParts.NewImage("MatapiGlow", Host.BodyFront, art.Glow, OverlayParts.Fade(WarmLight, 0f));
            _star = OverlayParts.NewImage("MatapiStar", Host.BodyFront, art.Star, new Color(1f, 0.98f, 0.9f, 0f));
            Material? additive = UiMaterials.Additive;
            if (additive != null)
            {
                _glow.material = additive;
                _star.material = additive;
            }
        }

        // Fotograma p (0..1) del sellado.
        private void Render(float p)
        {
            _progress = p;

            // Tirón: la cuerda asoma floja, el tirón la tensa y el aro ciñe el embudo, que se cierra con rebote y sube sobre
            // el borde; el aro se apaga bajo la tapa.
            float pull = Mathf.Clamp01((p - TugStart) / (TugEnd - TugStart));
            _close = Ease.OutBack(pull, 2.2f);
            bool tieOn = p < TieFadeEnd;
            OverlayParts.SetVisible(_tie, tieOn);
            if (tieOn)
            {
                OverlayParts.SetColor(_tie, new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((p - TieFadeStart) / (TieFadeEnd - TieFadeStart))));
            }

            ApplyFunnel();

            bool cordOn = p < CordFadeEnd;
            OverlayParts.SetVisible(_cord, cordOn);
            if (cordOn)
            {
                Sprite rope = p < TugStart ? _cordSlack : _cordTaut;
                if (_cord.sprite != rope)
                {
                    _cord.sprite = rope;
                }

                float grow = Ease.Out(Mathf.Clamp01(p / CordInEnd));
                float stretch = 1f + (CordStretch * Mathf.Sin(Mathf.PI * pull)); // el tirón la estira un momento
                SetScale(_cord.rectTransform, 1f, Mathf.Max(0.001f, grow * stretch));
                float tilt = Mathf.Lerp(CordSlackTiltDeg, CordTautTiltDeg, Ease.Out(pull));
                Quaternion lean = Quaternion.Euler(0f, 0f, -tilt);
                if (_cord.rectTransform.localRotation != lean)
                {
                    _cord.rectTransform.localRotation = lean;
                }

                OverlayParts.SetColor(_cord, new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((p - CordFadeStart) / (CordFadeEnd - CordFadeStart))));
            }

            // La trampa sale del agua y vuelve a su sitio.
            float lq = Mathf.Clamp01((p - LiftStart) / (LiftEnd - LiftStart));
            SetLift(TrapLiftU * Mathf.Sin(Mathf.PI * lq));
            IBoardFx? fx = _fx;
            if (!_splashed && p >= SplashAt)
            {
                _splashed = true;
                if (fx != null)
                {
                    float y = (BottomHoopU + _lift) * _u;
                    fx.Splash(ToFx(_selfPos + new Vector2(-0.62f * _u, y)), WaterDrop, _u);
                    fx.Splash(ToFx(_selfPos + new Vector2(0.62f * _u, y)), WaterDrop, _u);
                }
            }

            if (!_sparkled && p >= SparkleAt)
            {
                _sparkled = true;
                fx?.Sparkles(ToFx(_selfPos + new Vector2(0f, (_heightU + _lift) * _u)), WarmLight, SealSparkles, _u);
            }

            // La tapa cae con un mini-rebote (07 §5.3).
            bool capOn = p >= CapStart;
            OverlayParts.SetVisible(_cap, capOn);
            if (capOn)
            {
                float drop = CapDropU * (1f - Ease.Out(Mathf.Clamp01((p - CapStart) / (CapLand - CapStart))));
                float bounce = p > CapLand && p < CapBounceEnd ? 0.03f * Mathf.Sin(Mathf.PI * ((p - CapLand) / (CapBounceEnd - CapLand))) : 0f;
                PlaceCap(drop + bounce);
            }

            // Brillo cálido y destello al cerrarse.
            bool glowOn = p >= GlowStart && p < 1f;
            OverlayParts.SetVisible(_glow, glowOn);
            OverlayParts.SetVisible(_star, glowOn);
            if (glowOn)
            {
                float g = (p - GlowStart) / (1f - GlowStart);
                float alpha = 0.85f * (1f - (g * g)) * Mathf.Clamp01(g / 0.15f);
                OverlayParts.SetColor(_glow, OverlayParts.Fade(WarmLight, alpha));
                float scale = Mathf.Lerp(0.8f, 1.3f, Ease.Out(g));
                SetScale(_glow.rectTransform, scale, scale);
                float star = g < 0.4f ? Mathf.Lerp(0f, 1.1f, g / 0.4f) : Mathf.Lerp(1.1f, 0f, (g - 0.4f) / 0.6f);
                SetScale(_star.rectTransform, star, star);
                Quaternion spin = Quaternion.Euler(0f, 0f, 45f * g);
                if (_star.rectTransform.localRotation != spin)
                {
                    _star.rectTransform.localRotation = spin;
                }

                OverlayParts.SetColor(_star, new Color(1f, 0.98f, 0.9f, Mathf.Clamp01(star)));
            }
        }

        // Escala y sitio del embudo, del aro y del pie de la cuerda: el cierre (0 abierto, 1 cerrado) por el latido del
        // rechazo. Al cerrarse, el embudo sube sobre el borde de la cuia y el aro con él.
        private void ApplyFunnel()
        {
            float funnelX = Mathf.LerpUnclamped(1f, ClosedFunnelScale, _close) * _pulse;
            float funnelY = Mathf.LerpUnclamped(1f, ClosedFunnelScaleY, _close) * _pulse;
            SetScale(_funnel.rectTransform, funnelX, funnelY);
            SetY(_funnel.rectTransform, _heightU - Mathf.LerpUnclamped(FunnelBelowTopU, ClosedFunnelBelowTopU, _close));
            float tieX = Mathf.LerpUnclamped(OpenTieScale, ClosedTieScale, _close) * _pulse;
            float tieY = Mathf.LerpUnclamped(1f, ClosedTieScaleY, _close) * _pulse;
            SetScale(_tie.rectTransform, tieX, tieY);
            float tieAt = _heightU - Mathf.LerpUnclamped(TieBelowTopU, ClosedTieBelowTopU, _close);
            SetY(_tie.rectTransform, tieAt);
            SetY(_cord.rectTransform, tieAt);
        }

        // Fracción del radio de la varilla i (seno de su ángulo en el cilindro): ±40°, ±59° y ±78°, más juntas hacia los
        // bordes y con el centro libre (|x| = 0,55, 0,74 y 0,84 u).
        private static float RodSide(int i)
        {
            float deg;
            switch (i)
            {
                case 0:
                    deg = -78f;
                    break;
                case 1:
                    deg = -59f;
                    break;
                case 2:
                    deg = -40f;
                    break;
                case 3:
                    deg = 40f;
                    break;
                case 4:
                    deg = 59f;
                    break;
                default:
                    deg = 78f;
                    break;
            }

            return Mathf.Sin(deg * Mathf.Deg2Rad);
        }

        private void SetY(RectTransform rt, float yU)
        {
            var pos = new Vector2(0f, yU * _u);
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }
        }

        private void HideStepParts()
        {
            OverlayParts.SetVisible(_tie, false);
            OverlayParts.SetVisible(_cord, false);
            OverlayParts.SetVisible(_cap, false);
            OverlayParts.SetVisible(_glow, false);
            OverlayParts.SetVisible(_star, false);
        }

        private void SetLift(float liftU, bool force = false)
        {
            if (!force && Mathf.Approximately(liftU, _lift))
            {
                return;
            }

            _lift = liftU;
            var pos = new Vector2(0f, liftU * _u);
            if (_trap.anchoredPosition != pos)
            {
                _trap.anchoredPosition = pos;
            }
        }

        // La tapa del paso, dropU por encima de su sitio (el de la tapa de CuiaView).
        private void PlaceCap(float dropU)
        {
            OverlayParts.Place(_cap, 0f, _heightU - CapSinkU + (CapHeightU * 0.5f) + dropU, CapWidthU, CapHeightU, _u);
        }

        // Punto de las capas de tablero → coordenadas de los efectos (FxLayer: anclas arriba a la izquierda del tablero, que
        // comparte rectángulo con las capas).
        private Vector2 ToFx(Vector2 layerPos)
        {
            RectTransform? layer = Host.BoardFrontLayer;
            if (layer == null)
            {
                return layerPos;
            }

            Rect r = layer.rect;
            return new Vector2(layerPos.x - r.xMin, layerPos.y - r.yMax);
        }

        private static void SetScale(RectTransform rt, float x, float y)
        {
            var s = new Vector3(x, y, 1f);
            if (rt.localScale != s)
            {
                rt.localScale = s;
            }
        }
    }
}
