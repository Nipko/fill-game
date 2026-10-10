#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M7 Piraña: la cueva y su cardumen (02 §9.1 M7; propuesta de D-007 b.2 M7; respuesta Q-C; Q18 B) [P]. Sobre la boca de
    /// la cueva, un arco de piedras grises con musgo y el hueco en sombra, con la fila de <c>k</c> puntos (llenos los vertidos
    /// útiles ya contados; los mismos campos que el contador del HUD, <c>LevelCounter.Of</c>). Encima, un cardumen de
    /// <see cref="SchoolSize"/> pirañas pequeñas, plateadas y sin dientes exagerados ni sangre, que ronda la cueva. Con la
    /// cueva llena (el próximo mordisco se perdería, Q-C) el cardumen duerme atenuado y el arco se atenúa. En vuelo se ocultan
    /// el arco y los puntos (cuelgan del cuerpo); el cardumen se queda.
    /// </summary>
    /// <remarks>
    /// <para>Cardumen (arquitectura del paso 1 §6): se crea una vez en <see cref="OnAttach"/> bajo
    /// <c>Host.BoardFrontLayer</c>, en un Canvas anidado propio (se mueve cada fotograma sin reconstruir el lienzo del tablero,
    /// guía 8), y se recoloca en <see cref="OnBoardLayout"/> con <c>Host.Board.BodyCenter(Index)</c>: ronda por encima del
    /// arco, dentro del ancho de su ranura, así que no tapa ninguna cuia, y no captura toques (<c>raycastTarget</c> = false,
    /// sin <c>GraphicRaycaster</c>). Las pirañas se anclan arriba a la izquierda del tablero, como las cuias y los efectos:
    /// las posiciones de la capa (centradas) se pasan a ese espacio en el mismo instante en que se leen, así que un cambio
    /// tardío del tamaño del tablero (la escala del lienzo se resuelve al pintar) las mueve igual que a las cuias. Con
    /// Reducir movimiento (<c>IBoardFx.ReduceMotion</c>) se queda quieto. Muere con la vista de la cueva (un enlace en su
    /// ranura lo destruye).</para>
    /// <para>Pasos de la etapa (e), despachados a la cueva (<c>Index</c> = la cueva, <c>OtherIndex</c> = la víctima):</para>
    /// <list type="bullet">
    /// <item><description><see cref="MechanicStepKind.Bite"/> (<see cref="BiteStepMs"/> × <c>ctx.SpeedFactor</c>, [P] b.2): el
    /// cardumen sale de la cueva y nada en arco hasta la boca de la víctima (<c>ctx.OtherPos</c>), la que va delante muerde la
    /// dosis de arriba (que se ve al empezar: el vertido acabó en <c>ctx.OtherBefore</c>), unas burbujas, y se lleva el bocado
    /// a la cueva, donde forma su dosis de arriba. Las dos cuias se pintan con <c>ctx.Board.SetDose</c> y, al terminar, con
    /// <c>ShowContent</c> de <c>ctx.After</c> y <c>ctx.OtherAfter</c>. A mitad del paso la víctima aún tiene la dosis.</description></item>
    /// <item><description><see cref="MechanicStepKind.BiteLost"/> (<see cref="BiteLostStepMs"/> [P]; víctima sellada, Matapi,
    /// la propia cueva o cueva llena): un amago hacia la víctima y vuelta, sin cambiar nada; con la cueva llena, el cardumen
    /// dormido solo se revuelve.</description></item>
    /// </list>
    /// <para>Los puntos se llenan todos al empezar (el vertido que completa el ritmo) y vuelven a los de <c>After</c> con el
    /// mordisco o el amago. Sin asignaciones en los pasos ni en <see cref="Tick"/>. La Piraña queda pendiente de revisión del
    /// asesor cultural.</para>
    /// </remarks>
    public sealed class PiranaOverlay : CuiaOverlay
    {
        /// <summary>Opacidad con la cueva llena (<c>LevelCounter.Blocked</c>): el cardumen duerme atenuado.</summary>
        public const float DimAlpha = 0.5f;

        /// <summary>Mordisco: 500 ms [P, b.2 M7].</summary>
        public const float BiteStepMs = 500f;

        /// <summary>Mordisco perdido (amago): 400 ms [P].</summary>
        public const float BiteLostStepMs = 400f;

        /// <summary>Pirañas del cardumen (b.2 M7).</summary>
        public const int SchoolSize = 3;

        /// <summary>Vuelta del cardumen alrededor de su sitio (ms) [P].</summary>
        public const float IdlePeriodMs = 3600f;

        /// <summary>Centro del arco de piedras sobre el canto de la boca (u).</summary>
        public const float CaveCenterAboveRimU = 0.39f;

        /// <summary>Hueco entre lo alto del arco y el centro del cardumen (u).</summary>
        public const float SchoolAboveCaveU = 0.42f;

        // La piraña que va delante muerde.
        private const int Lead = 1;

        // Ciclo común de la vuelta y del balanceo (ms): múltiplo de 3.600 y de 2.800, así el reloj se recorta sin saltos.
        private const float ClockWrapMs = 50400f;
        private const float BobPeriodMs = 2800f;

        // Fases del mordisco (fracción del paso).
        private const float JoinEnd = 0.12f;
        private const float ReachEnd = 0.40f;
        private const float BiteAt = 0.53f;
        private const float DrainEnd = 0.66f;
        private const float CarryStart = 0.62f;
        private const float CarryEnd = 0.86f;
        private const float FillStart = 0.88f;
        private const float MorselGone = 0.96f;

        // Fases del amago.
        private const float FeintEnd = 0.45f;
        private const float TurnEnd = 0.55f;
        private const float FeintReach = 0.38f;
        private const float LostPipsAt = 0.5f;

        // Sitio de cada piraña quieta (dormida o con Reducir movimiento), en u desde el centro del cardumen, y hacia dónde mira
        // (+1 a la izquierda).
        private static readonly float[] RestX = { -0.5f, 0.02f, 0.52f };
        private static readonly float[] RestY = { -0.06f, 0.08f, -0.03f };
        private static readonly float[] RestFacing = { 1f, -1f, 1f };

        private readonly Image[] _fish = new Image[SchoolSize];
        private readonly Vector2[] _fishPos = new Vector2[SchoolSize];
        private readonly Vector2[] _start = new Vector2[SchoolSize];
        private Image _arch = null!;
        private Image _morsel = null!;
        private Image _glint = null!;
        private PipRow _pips = null!;
        private RectTransform _school = null!;
        private bool _on;
        private bool _inFlight;
        private bool _dimmed;
        private int _pipK = -1;
        private float _u = 24f;
        private float _heightU = CuiaGeometry.HeightU(2);
        private Vector2 _home;
        private float _clock;

        // Paso en curso.
        private bool _animating;
        private bool _fresh;
        private MechanicStepKind _kind;
        private float _t;
        private float _dur;
        private ICuiaBoard? _board;
        private int _index;
        private int _victim;
        private Cuia _after;
        private bool _afterSealed;
        private Cuia _otherAfter;
        private bool _otherAfterSealed;
        private int _afterK;
        private int _afterCounter;
        private int _victimDose = -1;
        private int _caveDose = -1;
        private int _tint;
        private Color _color;
        private Vector2 _target;
        private Vector2 _cave;
        private Vector2 _caveSurface;
        private Vector2 _victimMouth;
        private bool _sleepy;
        private bool _bitten;
        private bool _drained;
        private bool _splashed;
        private bool _pipsReset;
        private IBoardFx? _fx;
        private bool _reduceMotion;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Pirana;

        /// <inheritdoc />
        public override bool IsAnimating => _animating;

        /// <summary>Puntos del contador: el ritmo <c>k</c>.</summary>
        public int PipCount => _on ? _pips.Count : 0;

        /// <summary>Puntos llenos: vertidos útiles contados desde el último mordisco.</summary>
        public int FilledPips => _on ? _pips.Filled : 0;

        /// <summary>Atenuada: la cueva está llena y el cardumen duerme (respuesta Q-C).</summary>
        public bool Dimmed => _on && _dimmed;

        /// <summary>Opacidad actual del arco de piedras.</summary>
        public float StonesAlpha => _arch.color.a;

        /// <summary>Raíz del cardumen (en la capa de tablero delantera, con su Canvas anidado).</summary>
        public RectTransform School => _school;

        /// <summary>Sitio del cardumen, en el espacio de las capas de tablero: por encima del arco de la cueva.</summary>
        public Vector2 SchoolHome => ToLayer(_home);

        /// <summary>Centro actual del cardumen (media de las pirañas), en el espacio de las capas de tablero.</summary>
        public Vector2 SchoolCenter
        {
            get
            {
                Vector2 sum = Vector2.zero;
                for (int i = 0; i < SchoolSize; i++)
                {
                    sum += _fishPos[i];
                }

                return ToLayer(sum / SchoolSize);
            }
        }

        /// <summary>El cardumen está a la vista.</summary>
        public bool SchoolVisible => _on && _school.gameObject.activeSelf;

        /// <summary>Las pirañas del cardumen tienen los ojos cerrados (duermen).</summary>
        public bool SchoolAsleep
        {
            get
            {
                Sprite asleep = Host.Art.PiranaFishAsleepSprite;
                for (int i = 0; i < SchoolSize; i++)
                {
                    if (_fish[i].sprite != asleep)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>El bocado que se lleva el cardumen está a la vista.</summary>
        public bool MorselVisible => _morsel.color.a > 0.01f;

        /// <summary>Imagen de la piraña <paramref name="i"/> (tests).</summary>
        public Image Fish(int i)
        {
            return _fish[i];
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            _on = cuia.Mechanic == MechanicType.Pirana;
            Visible = _on;
            if (_on)
            {
                int k = cuia.MechanicK;
                if (k != _pipK)
                {
                    _pipK = k;
                    LayoutPips();
                }

                _pips.Set(k, cuia.MechanicCounter);
                bool dim = cuia.IsFull;
                if (dim != _dimmed)
                {
                    _dimmed = dim;
                    float alpha = dim ? DimAlpha : 1f;
                    OverlayParts.SetColor(_arch, new Color(1f, 1f, 1f, alpha));
                    _pips.SetAlpha(alpha);
                }
            }

            Apply();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            OverlayParts.Place(_arch, 0f, heightU + CaveCenterAboveRimU, ProceduralArt.PiranaCaveWidthU, ProceduralArt.PiranaCaveHeightU, u);
            LayoutPips();
            var fishSize = new Vector2(ProceduralArt.PiranaFishWidthU * u, ProceduralArt.PiranaFishHeightU * u);
            for (int i = 0; i < SchoolSize; i++)
            {
                RectTransform rt = _fish[i].rectTransform;
                if (rt.sizeDelta != fishSize)
                {
                    rt.sizeDelta = fishSize;
                }
            }

            RecomputeHome();
            if (!_animating)
            {
                PlaceIdle();
            }
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            RecomputeHome();
            if (!_animating)
            {
                PlaceIdle();
            }
        }

        /// <inheritdoc />
        public override void SetInFlight(bool inFlight)
        {
            _inFlight = inFlight;
            Apply();
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (!_on)
            {
                return;
            }

            if (!ReduceMotion)
            {
                _clock += dtMs;
                if (_clock >= ClockWrapMs)
                {
                    _clock -= ClockWrapMs;
                }
            }

            if (!_animating)
            {
                PlaceIdle();
                return;
            }

            // El primer fotograma del paso no avanza: la animación nunca va por delante del paso de la cola.
            if (_fresh)
            {
                _fresh = false;
            }
            else
            {
                _t += dtMs;
            }

            Pose(_dur > 0f ? Mathf.Clamp01(_t / _dur) : 1f);
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            switch (kind)
            {
                case MechanicStepKind.Bite:
                    return BiteStepMs;
                case MechanicStepKind.BiteLost:
                    return BiteLostStepMs;
                default:
                    return 0f;
            }
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            float nominal = StepDurationMs(ctx.Kind);
            if (nominal <= 0f)
            {
                return 0f;
            }

            if (_animating)
            {
                FinishStep(false);
            }

            _kind = ctx.Kind;
            _board = ctx.Board;
            _index = ctx.Index;
            _victim = ctx.OtherIndex;
            _after = ctx.After;
            _afterSealed = ctx.AfterSealed;
            _otherAfter = ctx.OtherAfter;
            _otherAfterSealed = ctx.OtherAfterSealed;
            _afterK = ctx.After.MechanicK;
            _afterCounter = ctx.After.MechanicCounter;
            _fx = ctx.Fx;
            _reduceMotion = ctx.ReduceMotion;
            _dur = nominal * Mathf.Max(0.01f, ctx.SpeedFactor);
            _t = 0f;
            _fresh = true;
            _animating = true;
            _bitten = false;
            _drained = false;
            _splashed = false;
            _pipsReset = false;
            _sleepy = ctx.Before.IsFull;
            int k = ctx.Before.MechanicK;
            _pips.Set(k, k); // el vertido que completa el ritmo llena todos los puntos
            for (int i = 0; i < SchoolSize; i++)
            {
                _start[i] = _fishPos[i];
            }

            // Las posiciones del contexto (capa, centradas) pasan al espacio del cardumen en este mismo instante.
            bool hasVictim = _victim >= 0 && _victim != _index;
            float victimHeight = CuiaGeometry.HeightU(ctx.OtherBefore.Capacity > 0 ? ctx.OtherBefore.Capacity : 4);
            Vector2 self = ToSchool(ctx.SelfPos);
            Vector2 other = ToSchool(ctx.OtherPos);
            _target = hasVictim ? other + new Vector2(0f, (victimHeight + 0.55f) * _u) : _home + new Vector2(0f, 0.45f * _u);
            _victimMouth = other + new Vector2(0f, (victimHeight - 0.15f) * _u);
            _cave = self + new Vector2(0f, (_heightU + 0.42f) * _u);
            _victimDose = -1;
            _caveDose = -1;
            if (_kind == MechanicStepKind.Bite && hasVictim)
            {
                _tint = ctx.Tint;
                _color = Palette.Tint(_tint);
                _victimDose = ctx.OtherBefore.Count - 1;
                _caveDose = ctx.Before.Count;
                _caveSurface = self + new Vector2(0f, (CuiaGeometry.CavityBottomU + _caveDose) * _u);
                OverlayParts.SetColor(_morsel, new Color(_color.r, _color.g, _color.b, 0f));
                if (_board != null)
                {
                    PaintVictim(1f);
                    PaintCave(0f);
                }
            }

            Pose(0f);
            return _dur;
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            FinishStep(true);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.StoneLight); // piedras
            into.Add(ProceduralArt.StoneDark);
            into.Add(ProceduralArt.PiranaStoneGlint);
            into.Add(ProceduralArt.PiranaCaveDark); // hueco de la cueva
            into.Add(ProceduralArt.PiranaCaveFloor);
            into.Add(ProceduralArt.PiranaMoss);
            into.Add(ProceduralArt.PiranaMossLight);
            into.Add(ProceduralArt.PiranaFishSilver); // pirañas
            into.Add(ProceduralArt.PiranaFishBelly);
            into.Add(ProceduralArt.PiranaFishBack);
            into.Add(ProceduralArt.PiranaFishFin);
            into.Add(ProceduralArt.Slate); // contorno de las piedras
            into.Add(ProceduralArt.Pearl); // puntos del contador (el bocado lleva la fruta a propósito)
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            ProceduralArt art = Host.Art;
            _arch = OverlayParts.NewImage("PiranaCave", Host.BodyFront, art.PiranaCaveSprite, Color.white);
            _pips = new PipRow("PiranaPip", Host.BodyFront, art.CounterPip, art.CounterPipEmpty, ProceduralArt.Pearl);

            // El cardumen, en la capa delantera del tablero con su Canvas anidado (sin GraphicRaycaster: no recibe toques).
            RectTransform layer = Host.BoardFrontLayer ?? Host.SlotFront;
            var go = new GameObject("PiranaSchool" + Host.Index, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(layer, false);
            _school = (RectTransform)go.transform;
            _school.anchorMin = Vector2.zero;
            _school.anchorMax = Vector2.one;
            _school.pivot = new Vector2(0.5f, 0.5f);
            _school.offsetMin = Vector2.zero;
            _school.offsetMax = Vector2.zero;
            for (int i = 0; i < SchoolSize; i++)
            {
                _fish[i] = SchoolImage("PiranaFish" + i, art.PiranaFishSprite, Color.white);
            }

            _morsel = SchoolImage("PiranaMorsel", art.PiranaMorselSprite, new Color(1f, 1f, 1f, 0f));
            _glint = SchoolImage("PiranaMorselGlint", art.CounterPip, new Color(1f, 1f, 1f, 0f));
            go.SetActive(false);

            // El cardumen vive en una capa que no es de la cuia: un enlace en su ranura lo destruye con la vista.
            var link = new GameObject("PiranaSchoolLink");
            link.transform.SetParent(Host.SlotBack, false);
            link.AddComponent<SchoolLink>().School = go;
        }

        private Image SchoolImage(string name, Sprite sprite, Color color)
        {
            Image img = OverlayParts.NewImage(name, _school, sprite, color);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); // arriba a la izquierda, como las cuias y los efectos
            rt.anchorMax = new Vector2(0f, 1f);
            img.canvasRenderer.cullTransparentMesh = true;
            img.gameObject.SetActive(true);
            return img;
        }

        private bool ReduceMotion
        {
            get
            {
                IBoardFx? fx = Host.Fx;
                return fx != null && fx.ReduceMotion;
            }
        }

        private void LayoutPips()
        {
            float step = _pipK >= 5 ? 0.22f : 0.27f; // con k = 5 la fila cabe en el hueco del arco
            _pips.Layout(0f, _heightU + 0.27f, 0.2f, step, _u);
        }

        private void RecomputeHome()
        {
            ICuiaBoard? board = Host.Board;
            if (board == null || (uint)Host.Index >= (uint)board.Count)
            {
                return;
            }

            float archTop = CaveCenterAboveRimU + (0.5f * ProceduralArt.PiranaCaveHeightU);
            _home = ToSchool(board.BodyCenter(Host.Index)) + new Vector2(0f, ((0.5f * _heightU) + archTop + SchoolAboveCaveU) * _u);
        }

        // Del espacio de las capas de tablero (origen en su centro) al del cardumen (arriba a la izquierda del tablero), con el
        // tamaño que tiene el tablero ahora mismo.
        private Vector2 ToSchool(Vector2 layer)
        {
            Rect r = _school.rect;
            return new Vector2(layer.x - r.xMin, layer.y - r.yMax);
        }

        private Vector2 ToLayer(Vector2 school)
        {
            Rect r = _school.rect;
            return new Vector2(school.x + r.xMin, school.y + r.yMax);
        }

        private void Apply()
        {
            bool shown = _on && !_inFlight;
            OverlayParts.SetVisible(_arch, shown);
            _pips.SetVisible(shown);
            if (_school.gameObject.activeSelf != _on)
            {
                _school.gameObject.SetActive(_on);
            }

            if (!_animating)
            {
                PlaceIdle();
            }
        }

        // Cardumen en reposo: rondando (despierto), quieto y atenuado (dormido) o quieto (Reducir movimiento).
        private void PlaceIdle()
        {
            for (int i = 0; i < SchoolSize; i++)
            {
                IdlePose(i, out Vector2 pos, out float facing, out float scale, out float alpha);
                SetFish(i, pos, facing, scale, alpha, 0f);
            }

            OverlayParts.SetColor(_morsel, new Color(_color.r, _color.g, _color.b, 0f));
            OverlayParts.SetColor(_glint, new Color(1f, 1f, 1f, 0f));
        }

        private void IdlePose(int i, out Vector2 pos, out float facing, out float scale, out float alpha)
        {
            float bob = Mathf.Sin((_clock * 2f * Mathf.PI / BobPeriodMs) + (i * 1.3f));
            if (_dimmed || ReduceMotion)
            {
                float lift = _dimmed && !ReduceMotion ? 0.025f * bob : 0f;
                pos = _home + new Vector2(RestX[i] * _u, (RestY[i] + lift) * _u);
                facing = RestFacing[i];
                scale = _dimmed ? 0.92f : 1f;
                alpha = _dimmed ? DimAlpha : 1f;
                return;
            }

            // Ronda: cada piraña da la vuelta a una elipse plana alrededor del sitio, a un tercio de vuelta de la anterior;
            // la de atrás (arriba) se ve algo más pequeña y en los extremos gira (se estrecha).
            float angle = (_clock * 2f * Mathf.PI / IdlePeriodMs) + (i * 2f * Mathf.PI / SchoolSize);
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            pos = _home + new Vector2(cos * 0.55f * _u, ((sin * 0.13f) + (0.03f * bob)) * _u);
            float turn = Mathf.Clamp(sin * 3f, -1f, 1f);
            facing = Mathf.Abs(turn) < 0.3f ? (sin >= 0f ? 0.3f : -0.3f) : turn;
            scale = Mathf.Lerp(1f, 0.84f, (sin + 1f) * 0.5f);
            alpha = 1f;
        }

        // Fotograma del paso en la fracción p (sin asignaciones).
        private void Pose(float p)
        {
            if (_kind == MechanicStepKind.Bite && _victimDose >= 0)
            {
                PoseBite(p);
            }
            else
            {
                PoseLost(p);
            }
        }

        private void PoseBite(float p)
        {
            Vector2 center;
            float facing = FacingFrom(_home, _target);
            if (p < ReachEnd)
            {
                center = Arc(_home, _target, Ease.InOut(p / ReachEnd));
            }
            else if (p < CarryStart)
            {
                center = _target;
            }
            else if (p < CarryEnd)
            {
                facing = FacingFrom(_target, _cave);
                center = Arc(_target, _cave, Ease.InOut((p - CarryStart) / (CarryEnd - CarryStart)));
            }
            else
            {
                facing = FacingFrom(_target, _cave);
                center = Vector2.Lerp(_cave, _home, Ease.Out((p - CarryEnd) / (1f - CarryEnd)));
            }

            // La que va delante baja de morros a la boca de la víctima y muerde.
            float dip = p >= ReachEnd && p < CarryStart ? Mathf.Sin(Mathf.PI * (p - ReachEnd) / (CarryStart - ReachEnd)) : 0f;
            Vector2 leadPos = Vector2.zero;
            float leadTilt = 0f;
            for (int i = 0; i < SchoolSize; i++)
            {
                Vector2 pos = Formation(i, center, facing);
                float tilt = 0f;
                float fishFacing = facing;
                if (i == Lead && dip > 0f)
                {
                    pos = Vector2.Lerp(pos, center + new Vector2(facing * 0.38f * _u, -0.5f * _u), dip);
                    tilt = 28f * dip;
                }
                else if (dip > 0f)
                {
                    // Las otras dos se abren a los lados de la boca, por encima, mirando a la que muerde.
                    float side = i == 0 ? -1f : 1f;
                    pos = Vector2.Lerp(pos, center + new Vector2(side * 0.6f * _u, (0.26f + (0.04f * side)) * _u), dip);
                    if (dip > 0.5f)
                    {
                        fishFacing = side;
                    }
                }

                pos = JoinAndLeave(i, p, pos, fishFacing, out float sx, out float scale, out float alpha);
                SetFish(i, pos, sx, scale, alpha, tilt * facing);
                if (i == Lead)
                {
                    leadPos = pos;
                    leadTilt = tilt;
                }
            }

            // El mordisco: burbujas, la víctima pierde la dosis de arriba y el contador vuelve a empezar.
            if (!_bitten && p >= BiteAt)
            {
                _bitten = true;
                Emit(true, 0f);
                ResetPips();
            }

            if (p >= BiteAt && !_drained)
            {
                float q = Mathf.Clamp01((p - BiteAt) / (DrainEnd - BiteAt));
                PaintVictim(1f - Ease.In(q));
                _drained = q >= 1f;
            }

            // El bocado va en la boca de la que muerde y cae dentro de la cueva, que forma su dosis de arriba.
            float morselAlpha = 0f;
            float morselScale = 1f;
            Vector2 morselPos = Mouth(leadPos, facing, leadTilt);
            if (p >= BiteAt && p < CarryEnd)
            {
                morselAlpha = 1f;
                morselScale = 0.6f + (0.4f * Ease.Out(Mathf.Clamp01((p - BiteAt) / 0.06f)));
            }
            else if (p >= CarryEnd && p < MorselGone)
            {
                float q = (p - CarryEnd) / (MorselGone - CarryEnd);
                Vector2 from = Mouth(Formation(Lead, _cave, facing), facing, 0f);
                Vector2 into = _caveSurface + new Vector2(0f, 0.3f * _u);
                morselPos = Vector2.Lerp(from, into, Ease.In(q));
                morselScale = Mathf.Lerp(1f, 0.6f, q);
                morselAlpha = q < 0.7f ? 1f : 1f - ((q - 0.7f) / 0.3f);
            }

            PlaceMorsel(morselPos, morselScale, morselAlpha);
            if (p >= FillStart)
            {
                float fill = Ease.Out((p - FillStart) / (1f - FillStart));
                PaintCave(fill);
                if (!_splashed && fill >= 0.7f)
                {
                    _splashed = true;
                    Emit(false, fill); // en la superficie que sube, no dentro de la dosis ya formada
                }
            }
        }

        private void PoseLost(float p)
        {
            if (!_pipsReset && p >= LostPipsAt)
            {
                ResetPips();
            }

            if (_sleepy)
            {
                // Cueva llena: el cardumen dormido se revuelve y sigue durmiendo.
                float wiggle = 12f * Mathf.Sin(5f * Mathf.PI * p) * (1f - p);
                for (int i = 0; i < SchoolSize; i++)
                {
                    IdlePose(i, out Vector2 pos, out float facing, out float scale, out float alpha);
                    SetFish(i, pos, facing, scale, alpha, (i % 2 == 0 ? 1f : -1f) * wiggle);
                }

                PlaceMorsel(_home, 1f, 0f);
                return;
            }

            // Amago: hacia la víctima un tramo, un lance de la que va delante y vuelta a su sitio.
            Vector2 feint = Vector2.Lerp(_home, _target, FeintReach);
            float toward = FacingFrom(_home, _target);
            Vector2 center;
            float facingNow = toward;
            float lunge = 0f;
            if (p < FeintEnd)
            {
                center = Vector2.Lerp(_home, feint, Ease.Out(p / FeintEnd));
            }
            else if (p < TurnEnd)
            {
                center = feint;
                lunge = Mathf.Sin(Mathf.PI * (p - FeintEnd) / (TurnEnd - FeintEnd));
            }
            else
            {
                facingNow = -toward;
                center = Vector2.Lerp(feint, _home, Ease.InOut((p - TurnEnd) / (1f - TurnEnd)));
            }

            for (int i = 0; i < SchoolSize; i++)
            {
                Vector2 pos = Formation(i, center, facingNow);
                if (i == Lead)
                {
                    pos += new Vector2(-toward * 0.15f * lunge * _u, 0f);
                }

                pos = JoinAndLeave(i, p, pos, facingNow, out float sx, out float scale, out float alpha);
                SetFish(i, pos, sx, scale, alpha, 0f);
            }

            PlaceMorsel(_home, 1f, 0f);
        }

        // Entra desde donde estaba cada piraña y, al final, vuelve a su ronda.
        private Vector2 JoinAndLeave(int i, float p, Vector2 pos, float facing, out float sx, out float scale, out float alpha)
        {
            sx = facing;
            scale = 1f;
            alpha = 1f;
            if (p < JoinEnd)
            {
                pos = Vector2.Lerp(_start[i], pos, Ease.Out(p / JoinEnd));
            }

            if (p > CarryEnd)
            {
                IdlePose(i, out Vector2 idle, out float idleFacing, out float idleScale, out float idleAlpha);
                float b = Ease.InOut((p - CarryEnd) / (1f - CarryEnd));
                pos = Vector2.Lerp(pos, idle, b);
                sx = b > 0.5f ? idleFacing : facing;
                scale = Mathf.Lerp(1f, idleScale, b);
                alpha = Mathf.Lerp(1f, idleAlpha, b);
            }

            return pos;
        }

        // Formación en marcha: la que muerde va delante y las otras dos detrás, una arriba y otra abajo.
        private Vector2 Formation(int i, Vector2 center, float facing)
        {
            float forward = -facing * _u; // mirando a la izquierda (+1), delante es −x
            switch (i)
            {
                case Lead:
                    return center + new Vector2(forward * 0.28f, 0f);
                case 0:
                    return center + new Vector2(-forward * 0.22f, 0.26f * _u);
                default:
                    return center + new Vector2(-forward * 0.30f, -0.22f * _u);
            }
        }

        // Boca de una piraña en pos que mira a facing con el morro tilt grados hacia abajo.
        private Vector2 Mouth(Vector2 pos, float facing, float tilt)
        {
            float a = tilt * Mathf.Deg2Rad;
            return pos + new Vector2(-facing * Mathf.Cos(a) * 0.42f * _u, ((-Mathf.Sin(a) * 0.42f) - 0.06f) * _u);
        }

        // Arco de a a b, más alto cuanto más lejos (salta por encima del tablero).
        private Vector2 Arc(Vector2 a, Vector2 b, float q)
        {
            Vector2 mid = (0.5f * (a + b)) + new Vector2(0f, (0.6f * _u) + (0.15f * (b - a).magnitude));
            float r = 1f - q;
            return (r * r * a) + (2f * r * q * mid) + (q * q * b);
        }

        private float FacingFrom(Vector2 from, Vector2 to)
        {
            float dx = to.x - from.x;
            if (dx < -0.5f)
            {
                return 1f;
            }

            return dx > 0.5f ? -1f : 1f;
        }

        private void SetFish(int i, Vector2 pos, float facing, float scale, float alpha, float tiltDeg)
        {
            _fishPos[i] = pos;
            Image fish = _fish[i];
            RectTransform rt = fish.rectTransform;
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }

            var s = new Vector3(facing * scale, scale, 1f);
            if (rt.localScale != s)
            {
                rt.localScale = s;
            }

            Quaternion rot = tiltDeg == 0f ? Quaternion.identity : Quaternion.Euler(0f, 0f, tiltDeg);
            if (rt.localRotation != rot)
            {
                rt.localRotation = rot;
            }

            Sprite sprite = _dimmed && !_animating ? Host.Art.PiranaFishAsleepSprite : Host.Art.PiranaFishSprite;
            if (_sleepy && _animating)
            {
                sprite = Host.Art.PiranaFishAsleepSprite;
            }

            if (fish.sprite != sprite)
            {
                fish.sprite = sprite;
            }

            OverlayParts.SetColor(fish, new Color(1f, 1f, 1f, alpha));
        }

        private void PlaceMorsel(Vector2 pos, float scale, float alpha)
        {
            float side = 0.36f * scale * _u;
            RectTransform rt = _morsel.rectTransform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(side, side);
            OverlayParts.SetColor(_morsel, new Color(_color.r, _color.g, _color.b, alpha));
            RectTransform g = _glint.rectTransform;
            g.anchoredPosition = pos + new Vector2(-0.25f * side, 0.25f * side);
            g.sizeDelta = new Vector2(0.3f * side, 0.3f * side);
            OverlayParts.SetColor(_glint, new Color(1f, 1f, 1f, 0.85f * alpha));
        }

        private void ResetPips()
        {
            _pipsReset = true;
            _pips.Set(_afterK, _afterCounter);
        }

        private void PaintVictim(float fill)
        {
            if (_board != null && _victimDose >= 0)
            {
                _board.SetDose(_victim, _victimDose, _color, fill, _tint);
            }
        }

        private void PaintCave(float fill)
        {
            if (_board != null && _caveDose >= 0 && _caveDose < Host.Capacity)
            {
                _board.SetDose(_index, _caveDose, _color, fill, _tint);
            }
        }

        // Burbujas en la boca de la víctima al morder, o salpicadura de la fruta al caer en la cueva. Con Reducir movimiento
        // el pool no emite.
        // Los efectos usan el mismo espacio que el cardumen (arriba a la izquierda del tablero).
        private void Emit(bool bite, float fill)
        {
            if (_fx == null || _reduceMotion)
            {
                return;
            }

            if (bite)
            {
                _fx.Bubbles(_victimMouth, 3, _u);
            }
            else
            {
                _fx.Splash(_caveSurface + new Vector2(0f, fill * _u), _color, _u);
            }
        }

        // Termina el paso: las dos cuias pintadas como después (con ShowContent si cierra), el bocado oculto y el cardumen
        // de vuelta a su ronda.
        private void FinishStep(bool paint)
        {
            if (!_animating)
            {
                return;
            }

            _animating = false;
            PaintVictim(0f);
            PaintCave(1f);
            ICuiaBoard? board = _board;
            _board = null;
            _fx = null;
            _victimDose = -1;
            _caveDose = -1;
            _sleepy = false;
            PlaceIdle();
            if (paint && board != null)
            {
                board.ShowContent(_index, _after, _afterSealed);
                if (_victim >= 0 && _victim != _index && _victim < board.Count)
                {
                    board.ShowContent(_victim, _otherAfter, _otherAfterSealed);
                }
            }
        }

        /// <summary>Destruye el cardumen (que vive en la capa del tablero) cuando se destruye la vista de la cueva.</summary>
        private sealed class SchoolLink : MonoBehaviour
        {
            public GameObject? School;

            private void OnDestroy()
            {
                if (School == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Destroy(School);
                }
                else
                {
                    DestroyImmediate(School);
                }
            }
        }
    }
}
