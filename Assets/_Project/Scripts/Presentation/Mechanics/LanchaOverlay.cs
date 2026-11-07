#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M11 Lancha (02 §9.1 M11; D-068, fase F4; arte por código, D-052) [P]: «un muelle; la lancha se acerca con cada cuia vacía y
    /// atraca en unos 1,4 s» (regla 5). La cuia de carga va a bordo de una lancha de madera (<see cref="ProceduralArt.LanchaHull"/>,
    /// en el cuerpo, bajo las dosis) y, detrás, en la ranura, está el muelle de dos pilotes (<see cref="ProceduralArt.LanchaDock"/>)
    /// y, en la cara de la repisa, una fila de <c>k</c> puntos (<see cref="PipRow"/>): uno lleno por cada cuia vacía del tablero (las que cuentan para
    /// <c>k</c>: toda cuia sin dosis salvo la lancha, como el núcleo, <see cref="CurveMechanics.LanchaEmpties"/>). La lancha está
    /// más baja (más lejos del muelle) cuanto menos vacías hay (<see cref="ApproachDropU"/>) y sube hacia él con cada una, con un
    /// leve cabeceo. No es origen ni destino: al tocarla solo se sacude la cuia (D-002).
    /// </summary>
    /// <remarks>
    /// <para><b>El atraque</b> (paso <see cref="MechanicStepKind.Dock"/>, etapa (f) de R10, <see cref="DockMs"/> ×
    /// <c>ctx.SpeedFactor</c>): la lancha llega al muelle (la cuia sube a su ranura), se tiende el cabo al pilote, los puntos se
    /// encienden y después el casco, el cabo y el muelle se funden: la cuia queda normal. Si atraca sin un vertido (la Cuia extra o
    /// la de rescate; <c>PourRules.Settle</c>), el mismo atraque lo arranca el propio overlay al ver la cuia sin la Lancha. Con
    /// Reducir movimiento, sin cabeceo ni subida: solo el fundido.</para>
    /// <para><b>Marea</b> (regla 2): si deshace el vertido que la hizo atracar, el <see cref="Show"/> con la Lancha devuelta la vuelve
    /// a poner en el agua. La subida usa <see cref="CuiaOverlayHost.BodyNudgeDp"/>, que vuelve a 0 al atracar o al ocultarse. Sin
    /// asignaciones en <see cref="Tick"/>, <see cref="Show"/> ni en el paso (08 §5.4).</para>
    /// </remarks>
    public sealed class LanchaOverlay : CuiaOverlay
    {
        /// <summary>Duración nominal del atraque (ms) [P]: «en unos 1,4 s» (02 §9.1 M11, regla 5).</summary>
        public const float DockMs = 1400f;

        /// <summary>Lo que baja la lancha sin ninguna vacía (u) [P]: con k vacías llega a 0, la altura del muelle.</summary>
        public const float ApproachDropU = 0.9f;

        /// <summary>Lo que tarda la lancha en acercarse un tramo (ms) [P].</summary>
        public const float ApproachMs = 450f;

        /// <summary>Cabeceo de la lancha en el agua (u) [P].</summary>
        public const float BobU = 0.05f;

        /// <summary>Periodo del cabeceo (ms) [P].</summary>
        public const float BobPeriodMs = 1800f;

        /// <summary>Altura del casco: su línea de agua, sobre el pie de la cuia (u).</summary>
        public const float HullWaterlineU = -0.05f;

        /// <summary>Altura de la fila de puntos (u desde el pie): en la cara de la repisa, bajo la cuia, donde no la tapa nada.</summary>
        public const float PipsY = -0.36f;

        private Image _dock = null!;
        private Image _hull = null!;
        private Image _rope = null!;
        private PipRow _pips = null!;
        private float _u = 24f;
        private int _k;
        private int _empties;
        private float _approach; // 0 lejos, 1 en el muelle
        private float _bobT;
        private bool _stepping;
        private bool _selfDock;
        private float _stepT;
        private float _stepMs = DockMs;
        private bool _stepReduce;
        private bool _lastShownLancha;
        private float _startNudge;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Lancha;

        /// <inheritdoc />
        public override bool IsAnimating => _stepping;

        /// <summary>El muelle está a la vista.</summary>
        public bool DockVisible => _dock.gameObject.activeSelf;

        /// <summary>El casco está a la vista.</summary>
        public bool HullVisible => _hull.gameObject.activeSelf;

        /// <summary>El cabo al pilote está a la vista (solo en el atraque).</summary>
        public bool RopeVisible => _rope.gameObject.activeSelf;

        /// <summary><c>k</c> de la lancha mostrada (las vacías con que atraca), o 0.</summary>
        public int K => _k;

        /// <summary>Cuias vacías que cuentan ahora para <c>k</c> (los puntos llenos del muelle).</summary>
        public int Empties => _empties;

        /// <summary>Cuánto se ha acercado la lancha al muelle (0..1; 1 = atracable).</summary>
        public float Approach => _approach;

        /// <summary>Puntos del muelle (tests).</summary>
        public PipRow Pips => _pips;

        /// <summary>Hay un atraque en curso, también el que arranca el propio overlay (<c>PourRules.Settle</c>).</summary>
        public bool Docking => _stepping;

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Dock ? DockMs : 0f;
        }

        /// <summary>El atraque (<see cref="MechanicStepKind.Dock"/>). Sin la lancha a la vista, 0 (salto). Sin asignaciones.</summary>
        public override float PlayStep(in MechanicStepContext ctx)
        {
            if (ctx.Kind != MechanicStepKind.Dock || !Visible)
            {
                return 0f;
            }

            BeginDock(DockMs * Mathf.Max(0.01f, ctx.SpeedFactor), ctx.ReduceMotion, false);
            return _stepMs;
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (!_stepping)
            {
                return;
            }

            EndDock();
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (_stepping)
            {
                _stepT += dtMs;
                ApplyDock();
                if (_selfDock && _stepT >= _stepMs)
                {
                    EndDock();
                }

                return;
            }

            if (!Visible)
            {
                return;
            }

            CountEmpties();
            float target = _k > 0 ? Mathf.Clamp01((float)_empties / _k) : 0f;
            if (Host.ReduceMotion)
            {
                _approach = target;
            }
            else if (_approach != target)
            {
                float step = dtMs / ApproachMs;
                _approach = target > _approach ? Mathf.Min(target, _approach + step) : Mathf.Max(target, _approach - step);
            }

            _bobT = Host.ReduceMotion ? 0f : (_bobT + dtMs) % BobPeriodMs;
            ApplyRest();
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Lancha;
            if (!on && _lastShownLancha && Visible && !_stepping && Host.Board != null)
            {
                // Atraca sin un vertido (la Cuia extra o la de rescate: PourRules.Settle) [P]: el mismo atraque, desde el overlay.
                _lastShownLancha = false;
                BeginDock(DockMs * (Host.ReduceMotion ? 0.5f : 1f), Host.ReduceMotion, true);
                return;
            }

            _lastShownLancha = on;
            Visible = on;
            OverlayParts.SetVisible(_dock, on);
            OverlayParts.SetVisible(_hull, on);
            OverlayParts.SetVisible(_rope, false);
            _pips.SetVisible(on);
            if (!on)
            {
                _k = 0;
                Host.BodyNudgeDp = 0f;
                return;
            }

            _k = cuia.MechanicK;
            SetAlpha(1f);
            CountEmpties();
            _approach = _k > 0 ? Mathf.Clamp01((float)_empties / _k) : 0f; // al pintar un estado (Marea, carga), sin transición
            ApplyRest();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            Place();
        }

        /// <inheritdoc />
        public override void SetInFlight(bool inFlight)
        {
            // La lancha nunca es origen (regla 2): nada que hacer.
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.LanchaHullColor);
            into.Add(ProceduralArt.LanchaTrim);
            into.Add(ProceduralArt.JirauWood); // el muelle
            into.Add(ProceduralArt.JirauShade);
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            ProceduralArt art = Host.Art;
            _dock = OverlayParts.NewImage("LanchaDock", Host.SlotBack, art.LanchaDock, Color.white);
            _dock.rectTransform.pivot = new Vector2(0.5f, 0f);
            _rope = OverlayParts.NewImage("LanchaRope", Host.SlotFront, art.MatapiRod, Palette.FiberBase);
            _rope.rectTransform.pivot = new Vector2(0f, 0.5f);
            _hull = OverlayParts.NewImage("LanchaHull", Host.BodyFront, art.LanchaHull, Color.white);
            _hull.rectTransform.pivot = new Vector2(0.5f, ProceduralArt.LanchaHullPivotY);
            _pips = new PipRow("LanchaPip", Host.SlotFront, art.CounterPip, art.CounterPipEmpty, ProceduralArt.Pearl); // en la cara de la repisa
        }

        // Las cuias vacías que cuentan para k en lo que pinta la cola: toda cuia sin dosis salvo la lancha (núcleo, fase F2 [P]).
        private void CountEmpties()
        {
            ICuiaBoard? board = Host.Board;
            int empties = 0;
            if (board != null)
            {
                for (int j = 0; j < board.Count; j++)
                {
                    if (j == Host.Index)
                    {
                        continue;
                    }

                    Cuia c = board.ShownCuia(j);
                    if (c.IsEmpty && c.Mechanic != MechanicType.Lancha)
                    {
                        empties++;
                    }
                }
            }

            if (empties != _empties || _pips.Count != _k)
            {
                _empties = empties;
                _pips.Set(_k, Mathf.Min(empties, _k));
            }
        }

        private void BeginDock(float ms, bool reduce, bool self)
        {
            _stepping = true;
            _selfDock = self;
            _stepT = 0f;
            _stepMs = ms;
            _stepReduce = reduce;
            _startNudge = Host.BodyNudgeDp;
            OverlayParts.SetVisible(_rope, !reduce);
            if (_k > 0)
            {
                _pips.Set(_k, _k);
            }

            ApplyDock();
        }

        private void EndDock()
        {
            _stepping = false;
            _selfDock = false;
            _lastShownLancha = false;
            Host.BodyNudgeDp = 0f;
            Visible = false;
            OverlayParts.SetVisible(_dock, false);
            OverlayParts.SetVisible(_hull, false);
            OverlayParts.SetVisible(_rope, false);
            _pips.SetVisible(false);
            SetAlpha(1f);
        }

        // En el agua: la lancha baja ApproachDropU × (1 − acercamiento), con su cabeceo.
        private void ApplyRest()
        {
            float bob = Host.ReduceMotion ? 0f : BobU * Mathf.Sin(2f * Mathf.PI * _bobT / BobPeriodMs);
            Host.BodyNudgeDp = (-(ApproachDropU * (1f - _approach)) + bob) * _u;
        }

        // Fotograma del atraque: hasta el 55 % la lancha sube al muelle (ease-out); del 35 % al 70 % se tiende el cabo; desde el
        // 70 % el casco, el cabo, el muelle y los puntos se funden. Con Reducir movimiento, la cuia ya en su sitio y solo el fundido.
        private void ApplyDock()
        {
            float q = Mathf.Clamp01(_stepT / Mathf.Max(1f, _stepMs));
            Host.BodyNudgeDp = _stepReduce ? 0f : Mathf.Lerp(_startNudge, 0f, Ease.Out(Mathf.Clamp01(q / 0.55f)));
            float fade = 1f - Mathf.Clamp01((q - 0.7f) / 0.3f);
            SetAlpha(fade);
            if (!_stepReduce)
            {
                float rope = Ease.Out(Mathf.Clamp01((q - 0.35f) / 0.35f));
                float half = 0.5f * ProceduralArt.LanchaDockWidthU * 0.84f;
                float len = Mathf.Max(0.01f, half - 0.75f) * rope;
                RectTransform rt = _rope.rectTransform;
                OverlayParts.Place(_rope, half - len, HullWaterlineU + 0.35f, len, 0.07f, _u);
                rt.localEulerAngles = new Vector3(0f, 0f, 12f);
                OverlayParts.SetColor(_rope, OverlayParts.Fade(Palette.FiberBase, fade));
            }
        }

        private void SetAlpha(float a)
        {
            var c = new Color(1f, 1f, 1f, a);
            OverlayParts.SetColor(_dock, c);
            OverlayParts.SetColor(_hull, c);
            _pips.SetAlpha(a);
        }

        private void Place()
        {
            OverlayParts.Place(_dock, 0f, -0.25f, ProceduralArt.LanchaDockWidthU, ProceduralArt.LanchaDockHeightU, _u);
            OverlayParts.Place(_hull, 0.15f, HullWaterlineU, ProceduralArt.LanchaHullWidthU, ProceduralArt.LanchaHullHeightU, _u);
            _pips.Layout(0f, PipsY, 0.22f, 0.30f, _u);
        }
    }
}
