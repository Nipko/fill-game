#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M3 Liana atada (02 §9.1 M3; propuesta de D-007 b.2 M3; ficha de Magic Sort §3.3 y §2.7) [P]: 2-3 vueltas en diagonal
    /// de liana verde hoja oscuro con hojitas (el tallo en <see cref="Palette.LeafMid"/>, a ΔE00 16,3 del Lulo; nunca
    /// <c>LeafLight</c> ni <c>JungleLight</c>, a 7,8 y 6,4) que dejan ver el contenido (Q17 A), una vuelta más en el cuello
    /// con el nudo a la derecha y, con fruta (<c>key</c> &gt; 0), la fruta llave grande colgando del nudo sobre el hombro
    /// derecho, sin tapar la cara de la dosis de arriba; de cuenta (<c>n</c> &gt; 0, variante del 243), <c>n</c> puntos en el
    /// nudo. Se oculta en cuanto la Liana se suelta.
    /// </summary>
    /// <remarks>
    /// <para><b>Desbloqueo</b> (paso Release, etapa (d) o (d′), después del Seal de la cuia de la llave; <c>ctx.OtherIndex</c>
    /// = la cuia recién sellada): dura <see cref="ReleaseMs"/> × factor de velocidad. Como la tela de Magic Sort (t = 56,
    /// 362 y 398): una ✓ verde sobre la fruta (sprite: el atlas TMP no tiene U+2713); un hilo de luz que baja desde la tapa
    /// de la cuia sellada hasta la fruta (<see cref="BezierStripGraphic"/> aditivo en la capa delantera del tablero, corto y
    /// con alfa porque cruza otras cuias), con un brillo y destellos al llegar; y la liana se seca (de verde a pajizo de
    /// fibra) y cae. Fases [P] en fracción del paso: ✓ 0-0,14 (y se apaga 0,64-0,74); hilo 0,08-0,46 (se apaga 0,56-0,72);
    /// brillo 0,44-0,68; se seca 0,48-0,70; cae 0,66-1. La Liana de cuenta hace lo mismo sin la ✓ y el hilo llega al nudo.</para>
    /// <para>El hilo vive fuera de la cuia (en la capa delantera del tablero): un componente en el overlay lo destruye cuando
    /// se destruye la cuia. Todo se crea en <see cref="OnAttach"/>; <see cref="Tick"/>, <see cref="Show"/> y los pasos no
    /// asignan memoria y solo escriben lo que cambia (08 §5.4). Sin paso, nada se mueve (D-002).</para>
    /// </remarks>
    public sealed class LianaOverlay : CuiaOverlay
    {
        /// <summary>Vueltas como mucho.</summary>
        public const int MaxWraps = 3;

        /// <summary>Desbloqueo de la Liana (ms, sin factor de velocidad) [P b.2 M3: 600-900 ms].</summary>
        public const float ReleaseMs = 750f;

        /// <summary>
        /// Lado de la celda de la fruta llave (u; <see cref="ProceduralArt.FruitFlight"/>, que deja margen y el rabito): la
        /// fruta se ve de unos 0,7 u, grande para la cuia.
        /// </summary>
        public const float FruitCellU = 1.2f;

        /// <summary>Centro de la fruta a la vista: sobre el hombro derecho (u desde el eje).</summary>
        public const float FruitCenterXU = 0.70f;

        /// <summary>Centro de la fruta a la vista sobre el techo de la cavidad (u).</summary>
        public const float FruitCenterAboveCavityU = 0.20f;

        /// <summary>Centro del nudo: en el canto derecho del cuello (u desde el eje).</summary>
        public const float KnotXU = 0.52f;

        /// <summary>Centro del nudo sobre el techo de la cavidad (u): en el cuello.</summary>
        public const float KnotAboveCavityU = 0.64f;

        /// <summary>Lo que cae la liana al soltarse (u) [P].</summary>
        public const float FallU = 1.4f;

        /// <summary>Destellos al llegar el hilo de luz.</summary>
        public const int ReleaseSparkles = 5;

        /// <summary>Segmentos del hilo de luz.</summary>
        public const int BeamSegments = 24;

        /// <summary>
        /// Tinte de la liana atada [P] #289454: con el gris del tallo (<see cref="ProceduralArt.LianaStemShade"/>) da
        /// <see cref="Palette.LeafMid"/>.
        /// </summary>
        public static readonly Color FreshTint = Palette.Hex(0x289454);

        /// <summary>Tinte de la liana seca: pajizo de fibra (<see cref="Palette.FiberBase"/>; excepción <c>fibra_*</c>).</summary>
        public static readonly Color DryTint = Palette.FiberBase;

        // Fases del desbloqueo (fracción del paso) [P].
        private const float CheckInEnd = 0.14f;
        private const float CheckFadeStart = 0.64f;
        private const float CheckFadeEnd = 0.74f;
        private const float BeamStart = 0.08f;
        private const float BeamArrive = 0.46f;
        private const float BeamFadeStart = 0.56f;
        private const float BeamFadeEnd = 0.72f;
        private const float GlowStart = 0.44f;
        private const float GlowEnd = 0.68f;
        private const float DryStart = 0.48f;
        private const float DryEnd = 0.70f;
        private const float FallStart = 0.66f;

        // Medidas (u). La celda de la fruta va 0,04 u a la derecha y 0,07 u por encima del centro de la fruta: el rabito
        // arriba, en el nudo.
        private const float FruitCellDxU = 0.04f;
        private const float FruitCellDyU = 0.07f;
        private const float CollarWidthU = 1.2f;
        private const float CollarHeightU = 0.42f;
        private const float CollarAboveCavityU = 0.58f;
        private const float KnotWidthU = 0.5f;
        private const float KnotHeightU = 0.42f;
        private const float GlowU = 1.7f;
        private const float CapTopAboveMouthU = 0.31f;
        private const float BeamBaseU = 0.10f;
        private const float BeamHeadU = 0.17f;
        private const float BeamHaloU = 0.10f;
        private const float MaxBeamLiftU = 2.6f;

        // Luz del hilo (aditiva): oro cálido con la punta casi blanca y un halo suave.
        private static readonly Color BeamBody = new Color(1f, 0.86f, 0.52f, 0.9f);
        private static readonly Color BeamTip = new Color(1f, 0.99f, 0.92f, 1f);
        private static readonly Color BeamHalo = new Color(1f, 0.78f, 0.36f, 0.35f);

        // Brillo cálido y destellos: cielo_horizonte #FFE4A8.
        private static readonly Color WarmLight = new Color(1f, 0xE4 / 255f, 0xA8 / 255f, 1f);

        private readonly Image[] _wraps = new Image[MaxWraps];
        private RectTransform _vine = null!;
        private Image _collar = null!;
        private Image _glow = null!;
        private Image _fruit = null!;
        private Image _knot = null!;
        private Image _check = null!;
        private PipRow _dots = null!;
        private BezierStripGraphic _beam = null!;
        private int _key;
        private int _n;
        private float _u = 24f;
        private bool _active;
        private float _elapsed;
        private float _duration = 1f;
        private float _progress;
        private bool _withCheck;
        private bool _hasBeam;
        private bool _sparkled;
        private int _otherIndex = -1;
        private Vector2 _selfPos;
        private IBoardFx? _fx;
        private Vector2 _p0;
        private Vector2 _p1;
        private Vector2 _p2;
        private Vector2 _p3;
        private Color _tint;
        private float _dry;
        private float _fall;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Liana;

        /// <inheritdoc />
        public override bool IsAnimating => _active;

        /// <summary>Vueltas de liana que se dibujan: 2 en las cuias de 1-2 dosis y 3 en las demás.</summary>
        public int WrapCount => Host.Capacity <= 2 ? 2 : MaxWraps;

        /// <summary>Fruta llave que cuelga del nudo (0 en la Liana de cuenta).</summary>
        public int ShownKey => _key;

        /// <summary>Puntos del nudo a la vista (Liana de cuenta: <c>n</c>).</summary>
        public int CountDots => _dots.Visible ? _dots.Count : 0;

        /// <summary>Fracción del desbloqueo en curso (0..1; 0 sin paso).</summary>
        public float ReleaseProgress => _active ? _progress : 0f;

        /// <summary>La ✓ verde está a la vista sobre la fruta.</summary>
        public bool CheckVisible => _check.gameObject.activeSelf && _check.color.a > 0.001f;

        /// <summary>El hilo de luz está a la vista.</summary>
        public bool BeamVisible => _beam != null && _beam.gameObject.activeSelf && _beam.color.a > 0.001f;

        /// <summary>El hilo de luz (en la capa delantera del tablero).</summary>
        public BezierStripGraphic Beam => _beam;

        /// <summary>Origen del hilo (la tapa de la cuia sellada) en el espacio de las capas de tablero.</summary>
        public Vector2 BeamFrom => _p0;

        /// <summary>Destino del hilo (la fruta, o el nudo en la Liana de cuenta) en el espacio de las capas de tablero.</summary>
        public Vector2 BeamTo => _p3;

        /// <summary>Tinte actual de la liana (<see cref="FreshTint"/> atada; hacia <see cref="DryTint"/> al secarse).</summary>
        public Color VineTint => _tint;

        /// <summary>Lo seca que está la liana (0 verde, 1 pajiza).</summary>
        public float DryAmount => _dry;

        /// <summary>Lo que ha caído la liana (u).</summary>
        public float FallAmount => _fall;

        /// <summary>Rectángulo de la celda de la fruta en u (x desde el eje, y desde el techo de la cavidad).</summary>
        public static Rect FruitCellRectU => new Rect(
            FruitCenterXU + FruitCellDxU - (0.5f * FruitCellU),
            FruitCenterAboveCavityU + FruitCellDyU - (0.5f * FruitCellU),
            FruitCellU,
            FruitCellU);

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Liana;
            Visible = on;
            if (on)
            {
                int key = cuia.MechanicKey;
                if (key != _key)
                {
                    _key = key;
                    _fruit.sprite = key > 0 ? Host.Art.FruitFlight(key) : null;
                }

                _n = cuia.MechanicN;
            }

            int wraps = WrapCount;
            for (int i = 0; i < MaxWraps; i++)
            {
                OverlayParts.SetVisible(_wraps[i], on && i < wraps);
            }

            OverlayParts.SetVisible(_collar, on);
            OverlayParts.SetVisible(_knot, on);
            OverlayParts.SetVisible(_fruit, on && _key > 0);
            int dots = on && _key == 0 ? _n : 0;
            _dots.Set(dots, dots);
            HideStepParts();
            ApplyRest();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            int cap = Host.Capacity;
            float cavityTop = CuiaGeometry.CavityTopU(cap);
            int wraps = WrapCount;
            float band = cap / (float)wraps;
            for (int i = 0; i < MaxWraps; i++)
            {
                float y = CuiaGeometry.CavityBottomU + (band * (i + 0.5f));
                OverlayParts.Place(_wraps[i], 0f, y, 2.1f, Mathf.Min(1f, band + 0.2f), u);
            }

            float knotY = cavityTop + KnotAboveCavityU;
            float fruitY = cavityTop + FruitCenterAboveCavityU;
            OverlayParts.Place(_collar, 0f, cavityTop + CollarAboveCavityU, CollarWidthU, CollarHeightU, u);
            OverlayParts.Place(_knot, KnotXU, knotY, KnotWidthU, KnotHeightU, u);
            OverlayParts.Place(_fruit, FruitCenterXU + FruitCellDxU, fruitY + FruitCellDyU, FruitCellU, FruitCellU, u);
            OverlayParts.Place(_glow, FruitCenterXU, fruitY, GlowU, GlowU, u);
            OverlayParts.Place(_check, FruitCenterXU, fruitY, ProceduralArt.LianaCheckU, ProceduralArt.LianaCheckU, u);
            _dots.Layout(KnotXU, knotY, 0.13f, 0.18f, u);
            if (_active)
            {
                RefreshBeamEnds();
                Render(_progress);
            }
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            if (_active)
            {
                RefreshBeamEnds();
                Render(_progress);
            }
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (_active)
            {
                _elapsed = Mathf.Min(_duration, _elapsed + dtMs);
                Render(_elapsed / _duration);
            }
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Release ? ReleaseMs : 0f;
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            if (ctx.Kind != MechanicStepKind.Release || ctx.Before.Mechanic != MechanicType.Liana)
            {
                return 0f;
            }

            float ms = ReleaseMs * ctx.SpeedFactor;
            if (ms <= 0f)
            {
                return 0f;
            }

            Show(ctx.Before, false); // la liana atada de antes de la etapa, en reposo
            _active = true;
            _duration = ms;
            _elapsed = 0f;
            _progress = 0f;
            _sparkled = false;
            _fx = ctx.Fx ?? Host.Fx;
            _selfPos = ctx.SelfPos;
            _otherIndex = ctx.OtherIndex;
            _withCheck = _key > 0;
            RefreshBeamEnds();
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
            HideStepParts();
            ApplyRest();
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(Shade(FreshTint, ProceduralArt.LianaStemShade)); // tallo de la liana (= LeafMid)
            into.Add(Shade(FreshTint, ProceduralArt.LianaLeafShade)); // hojitas y lóbulos del nudo
            into.Add(ProceduralArt.Pearl); // puntos de la Liana de cuenta
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            ProceduralArt art = Host.Art;
            _vine = OverlayParts.NewAnchor("LianaVine", Host.BodyFront);
            for (int i = 0; i < MaxWraps; i++)
            {
                _wraps[i] = OverlayParts.NewImage("LianaWrap" + i, _vine, art.LianaVine, FreshTint);
                if ((i % 2) == 1)
                {
                    _wraps[i].rectTransform.localScale = new Vector3(-1f, 1f, 1f); // vueltas alternas, en zigzag
                }
            }

            _collar = OverlayParts.NewImage("LianaCollar", _vine, art.LianaVine, FreshTint);
            _glow = OverlayParts.NewImage("LianaGlow", _vine, art.Glow, OverlayParts.Fade(WarmLight, 0f));
            Material? additive = UiMaterials.Additive;
            if (additive != null)
            {
                _glow.material = additive;
            }

            _fruit = OverlayParts.NewImage("LianaFruit", _vine, null, Color.white);
            _knot = OverlayParts.NewImage("LianaKnot", _vine, art.LianaVineKnot, FreshTint);
            _dots = new PipRow("LianaDot", _vine, art.CounterPip, art.CounterPipEmpty, ProceduralArt.Pearl);
            _check = OverlayParts.NewImage("LianaCheck", _vine, art.LianaCheck, Color.white);
            _tint = FreshTint;

            // Hilo de luz: en la capa delantera del tablero (delante de las cuias que cruza), con el espacio de la capa.
            RectTransform layer = Host.BoardFrontLayer!;
            var go = new GameObject("LianaBeam" + Host.Index, typeof(RectTransform), typeof(CanvasRenderer), typeof(BezierStripGraphic));
            go.transform.SetParent(layer, false);
            _beam = go.GetComponent<BezierStripGraphic>();
            _beam.Setup(BeamSegments);
            RectTransform rt = _beam.rectTransform;
            rt.anchorMin = layer.pivot;
            rt.anchorMax = layer.pivot;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            if (additive != null)
            {
                _beam.material = additive;
            }

            go.SetActive(false);
            _vine.gameObject.AddComponent<BeamOwner>().Beam = go;
        }

        // Fotograma p (0..1) del desbloqueo.
        private void Render(float p)
        {
            _progress = p;

            // ✓ sobre la fruta.
            if (_withCheck)
            {
                float s = Ease.OutBack(Mathf.Clamp01(p / CheckInEnd));
                float a = 1f - Mathf.Clamp01((p - CheckFadeStart) / (CheckFadeEnd - CheckFadeStart));
                bool on = s > 0.001f && a > 0.001f;
                OverlayParts.SetVisible(_check, on);
                if (on)
                {
                    SetScale(_check.rectTransform, s, s);
                    OverlayParts.SetColor(_check, new Color(1f, 1f, 1f, a));
                }
            }

            // Hilo de luz desde la tapa de la cuia sellada.
            if (_hasBeam)
            {
                float head = Ease.InOut(Mathf.Clamp01((p - BeamStart) / (BeamArrive - BeamStart)));
                float a = 1f - Mathf.Clamp01((p - BeamFadeStart) / (BeamFadeEnd - BeamFadeStart));
                bool on = head > 0.001f && a > 0.001f;
                OverlayParts.SetVisible(_beam, on);
                if (on)
                {
                    SetBeamHead(head);
                    OverlayParts.SetColor(_beam, new Color(1f, 1f, 1f, a));
                }
            }

            // Brillo y destellos al llegar.
            bool glowOn = p >= GlowStart && p < GlowEnd;
            OverlayParts.SetVisible(_glow, glowOn && _key > 0);
            if (glowOn && _key > 0)
            {
                float g = (p - GlowStart) / (GlowEnd - GlowStart);
                OverlayParts.SetColor(_glow, OverlayParts.Fade(WarmLight, 0.8f * Mathf.Sin(Mathf.PI * g)));
                float scale = Mathf.Lerp(0.8f, 1.2f, Ease.Out(g));
                SetScale(_glow.rectTransform, scale, scale);
            }

            if (!_sparkled && p >= BeamArrive)
            {
                _sparkled = true;
                _fx?.Sparkles(ToFx(_selfPos + (EndLocalU() * _u)), WarmLight, ReleaseSparkles, _u);
            }

            // Se seca y cae.
            _dry = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((p - DryStart) / (DryEnd - DryStart)));
            float q = Mathf.Clamp01((p - FallStart) / (1f - FallStart));
            _fall = FallU * q * q;
            float alpha = 1f - Ease.In(Mathf.Clamp01((q - 0.25f) / 0.75f));
            ApplyVine(Color.Lerp(FreshTint, DryTint, _dry), alpha, _fall, 12f * q);
        }

        // Liana en reposo: atada, verde, sin caer.
        private void ApplyRest()
        {
            _dry = 0f;
            _fall = 0f;
            ApplyVine(FreshTint, 1f, 0f, 0f);
        }

        // Tinte, opacidad, caída (u) y giro (grados, alterno por vuelta) de la liana, el nudo, la fruta y los puntos.
        private void ApplyVine(Color tint, float alpha, float fallU, float tiltDeg)
        {
            _tint = tint;
            Color c = OverlayParts.Fade(tint, alpha);
            for (int i = 0; i < MaxWraps; i++)
            {
                OverlayParts.SetColor(_wraps[i], c);
                Quaternion r = tiltDeg == 0f ? Quaternion.identity : Quaternion.Euler(0f, 0f, (i % 2) == 0 ? tiltDeg : -tiltDeg);
                if (_wraps[i].rectTransform.localRotation != r)
                {
                    _wraps[i].rectTransform.localRotation = r;
                }
            }

            OverlayParts.SetColor(_collar, c);
            OverlayParts.SetColor(_knot, c);
            OverlayParts.SetColor(_fruit, new Color(1f, 1f, 1f, alpha));
            _dots.SetAlpha(alpha);
            var pos = new Vector2(0f, -fallU * _u);
            if (_vine.anchoredPosition != pos)
            {
                _vine.anchoredPosition = pos;
            }
        }

        private void HideStepParts()
        {
            OverlayParts.SetVisible(_check, false);
            OverlayParts.SetVisible(_glow, false);
            if (_beam != null)
            {
                OverlayParts.SetVisible(_beam, false);
            }
        }

        // Extremos del hilo: de la tapa de la cuia sellada (pie + dos veces el centro del cuerpo) a la fruta o al nudo,
        // en un arco que sube por encima de las cuias que cruza.
        private void RefreshBeamEnds()
        {
            ICuiaBoard? board = Host.Board;
            _hasBeam = false;
            if (board == null || (uint)_otherIndex >= (uint)board.Count || (uint)Host.Index >= (uint)board.Count)
            {
                return;
            }

            _selfPos = board.Foot(Host.Index);
            Vector2 foot = board.Foot(_otherIndex);
            Vector2 center = board.BodyCenter(_otherIndex);
            Vector2 top = foot + (2f * (center - foot));
            _p0 = top + new Vector2(0f, CapTopAboveMouthU * _u);
            _p3 = _selfPos + (EndLocalU() * _u);
            Vector2 d = _p3 - _p0;
            float lift = Mathf.Min((1.0f * _u) + (0.3f * d.magnitude), MaxBeamLiftU * _u); // entre filas, sin salirse del panel
            _p1 = _p0 + (d * 0.2f) + new Vector2(0f, lift);
            _p2 = _p0 + (d * 0.8f) + new Vector2(0f, lift);
            float outline = BeamHaloU * _u;
            _beam.SetColors(BeamBody, BeamTip, 0.75f, BeamHalo, outline);
            _hasBeam = true;
        }

        // El hilo hasta la fracción head de su curva (de Casteljau: los puntos de control del tramo [0, head]).
        private void SetBeamHead(float head)
        {
            Vector2 a = Vector2.LerpUnclamped(_p0, _p1, head);
            Vector2 b = Vector2.LerpUnclamped(_p1, _p2, head);
            Vector2 c = Vector2.LerpUnclamped(_p2, _p3, head);
            Vector2 ab = Vector2.LerpUnclamped(a, b, head);
            Vector2 bc = Vector2.LerpUnclamped(b, c, head);
            Vector2 abc = Vector2.LerpUnclamped(ab, bc, head);
            _beam.SetCurve(_p0, a, ab, abc, BeamBaseU * _u, BeamHeadU * _u);
        }

        // Adónde llega el hilo, en u desde el pie: la fruta, o el nudo en la Liana de cuenta.
        private Vector2 EndLocalU()
        {
            float cavityTop = CuiaGeometry.CavityTopU(Host.Capacity);
            return _key > 0 ? new Vector2(FruitCenterXU, cavityTop + FruitCenterAboveCavityU) : new Vector2(KnotXU, cavityTop + KnotAboveCavityU);
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

        private static Color Shade(Color c, float k)
        {
            return new Color(c.r * k, c.g * k, c.b * k, 1f);
        }

        private static void SetScale(RectTransform rt, float x, float y)
        {
            var s = new Vector3(x, y, 1f);
            if (rt.localScale != s)
            {
                rt.localScale = s;
            }
        }

        /// <summary>
        /// Destruye el hilo de luz, que vive en la capa delantera del tablero, cuando se destruye la cuia (cambio de nivel):
        /// así no queda huérfano en esa capa.
        /// </summary>
        private sealed class BeamOwner : MonoBehaviour
        {
            public GameObject? Beam;

            private void OnDestroy()
            {
                if (Beam == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Destroy(Beam);
                }
                else
                {
                    DestroyImmediate(Beam);
                }
            }
        }
    }
}
