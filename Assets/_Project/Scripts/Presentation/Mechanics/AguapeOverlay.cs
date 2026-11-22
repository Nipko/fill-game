#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M8 Aguapé (02 §9.1 M8; D-068, fase F4; arte por código, D-052) [P]: una mata de aguapé abraza la base de cada cuia del
    /// grupo hasta media altura (regla 9). Detrás del vidrio, la mata ancha (<see cref="ProceduralArt.AguapeMat"/>) sale por los
    /// dos lados y, entre dos cuias contiguas del mismo grupo y de la misma fila, se une con un puente de hojas
    /// (<see cref="BridgeVisible"/>): el grupo se lee como una sola mata. Delante, dos matas pequeñas en los cantos del vidrio
    /// (<see cref="ProceduralArt.AguapeTuft"/>), sin tapar el centro: el contenido se ve (regla 1). Es un estado: nada cambia al
    /// seleccionar (D-002) y al tocarla como origen solo se sacude la cuia.
    /// </summary>
    /// <remarks>
    /// <para><b>Aguapé espeso</b> (<c>n</c> = 2; P13 A): una segunda capa de hojas más oscura encima de la mata
    /// (<see cref="ThickLayerAlpha"/>) y dos matas más delante. Con el primer sello del grupo «se aclara» (regla 9): la segunda
    /// capa se funde en <see cref="ThinMs"/>. El overlay mira el grupo en el tablero que pinta la cola (<see cref="ICuiaBoard"/>):
    /// cuenta las cuias del grupo selladas (llenas de una sola fruta) cada fotograma, sin asignar.</para>
    /// <para><b>La suelta</b> (paso <see cref="MechanicStepKind.Loosen"/>, etapa (d) de R10; regla 3; <see cref="LoosenMs"/> ×
    /// <c>ctx.SpeedFactor</c>, unos 700 ms): la mata se parte en <see cref="BitCount"/> trozos que se alejan flotando hacia los
    /// lados y arriba, girando, mientras la mata y las de delante se funden. Con Reducir movimiento, solo el fundido. Lo ejecuta
    /// cada cuia del grupo a la vez; suena una vez por grupo (<see cref="MechanicStepPlan.LoosenLeadTint"/>).</para>
    /// <para><b>Marea</b> (regla 7): si deshace el sello que la soltó, el <see cref="Show"/> con el Aguapé devuelto vuelve a pintar la
    /// mata. Sin asignaciones en <see cref="Tick"/>, <see cref="Show"/> ni en los pasos (08 §5.4).</para>
    /// </remarks>
    public sealed class AguapeOverlay : CuiaOverlay
    {
        /// <summary>Duración nominal de la suelta (ms) [P]: «en unos 700 ms» (02 §9.1 M8, regla 9).</summary>
        public const float LoosenMs = 700f;

        /// <summary>Lo que tarda el espeso en aclararse con el primer sello del grupo (ms) [P].</summary>
        public const float ThinMs = 300f;

        /// <summary>Opacidad de la segunda capa del espeso [P].</summary>
        public const float ThickLayerAlpha = 0.95f;

        /// <summary>Trozos de la mata en la suelta.</summary>
        public const int BitCount = 5;

        /// <summary>Lo que la mata de delante se mete en el vidrio desde su canto (u): no llega al centro de la cuia.</summary>
        public const float TuftInsetU = 0.08f;

        // Dirección (x, y en u) y giro final (grados) de cada trozo en la suelta.
        private static readonly Vector2[] BitStart = { new Vector2(-1.05f, 1.6f), new Vector2(-0.55f, 2.3f), new Vector2(0.15f, 1.9f), new Vector2(0.7f, 2.4f), new Vector2(1.1f, 1.5f) };
        private static readonly Vector2[] BitDrift = { new Vector2(-1.3f, 0.6f), new Vector2(-0.7f, 1.2f), new Vector2(0.1f, 1.4f), new Vector2(0.8f, 1.1f), new Vector2(1.4f, 0.5f) };
        private static readonly float[] BitSpin = { -70f, -35f, 20f, 45f, 80f };

        private Image _mat = null!;
        private Image _thick = null!;
        private Image _bridge = null!;
        private Image _tuftL = null!;
        private Image _tuftR = null!;
        private Image _tuftL2 = null!;
        private Image _tuftR2 = null!;
        private readonly Image[] _bits = new Image[BitCount];
        private float _u = 24f;
        private float _heightU = 5.6f;
        private bool _thickGroup;
        private int _group = -1;
        private float _thin; // 0 tupido, 1 aclarado (espeso con un sello)
        private bool _stepping;
        private float _stepT;
        private float _stepMs = LoosenMs;
        private bool _stepReduce;
        private int _layoutVersion = -1;
        private float _bridgeHalfU;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Aguape;

        /// <inheritdoc />
        public override bool IsAnimating => _stepping;

        /// <summary>La mata de detrás está a la vista.</summary>
        public bool MatVisible => _mat.gameObject.activeSelf;

        /// <summary>El puente de hojas hacia la cuia siguiente del grupo está a la vista.</summary>
        public bool BridgeVisible => _bridge.gameObject.activeSelf;

        /// <summary>La capa del espeso está a la vista (aunque se esté fundiendo).</summary>
        public bool ThickVisible => _thick.gameObject.activeSelf;

        /// <summary>Opacidad de la capa del espeso (0 aclarado).</summary>
        public float ThickAlpha => _thick.color.a;

        /// <summary>Opacidad de la mata de detrás (1 en reposo; baja en la suelta).</summary>
        public float MatAlpha => _mat.color.a;

        /// <summary>Grupo de la cuia mostrada (el de <see cref="Cuia.MechanicGroup"/>), o −1 sin la mata.</summary>
        public int Group => _group;

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Loosen ? LoosenMs : 0f;
        }

        /// <summary>
        /// La suelta (<see cref="MechanicStepKind.Loosen"/>): los trozos se alejan flotando y la mata se funde. Si la cuia de antes
        /// del paso no lleva la mata, 0 (salto). Sin asignaciones.
        /// </summary>
        public override float PlayStep(in MechanicStepContext ctx)
        {
            if (ctx.Kind != MechanicStepKind.Loosen || ctx.Before.Mechanic != MechanicType.Aguape)
            {
                return 0f;
            }

            if (!Visible)
            {
                Show(ctx.Before, false); // la mata del estado de antes de la suelta
            }

            _stepping = true;
            _stepT = 0f;
            _stepMs = LoosenMs * Mathf.Max(0.01f, ctx.SpeedFactor);
            _stepReduce = ctx.ReduceMotion;
            for (int i = 0; i < BitCount; i++)
            {
                OverlayParts.SetVisible(_bits[i], !_stepReduce);
            }

            ApplyLoosen();
            return _stepMs;
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (!_stepping)
            {
                return;
            }

            _stepping = false;
            for (int i = 0; i < BitCount; i++)
            {
                OverlayParts.SetVisible(_bits[i], false);
            }

            SetAlpha(1f);
            Place();
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (_stepping)
            {
                _stepT += dtMs;
                ApplyLoosen();
                return;
            }

            if (!Visible)
            {
                return;
            }

            // El espeso se aclara con el primer sello del grupo (regla 9).
            if (_thickGroup)
            {
                float target = GroupSeals() >= 1 ? 1f : 0f;
                if (_thin != target)
                {
                    float step = ThinMs > 0f ? dtMs / ThinMs : 1f;
                    _thin = target > _thin ? Mathf.Min(target, _thin + step) : Mathf.Max(target, _thin - step);
                    ApplyThin();
                }
            }

            ICuiaBoard? board = Host.Board;
            if (board != null && board.LayoutVersion != _layoutVersion)
            {
                UpdateBridge();
            }
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Aguape;
            Visible = on;
            _group = on ? cuia.MechanicGroup : -1;
            _thickGroup = on && cuia.MechanicN >= CurveMechanics.AguapeThickSeals;
            OverlayParts.SetVisible(_mat, on);
            OverlayParts.SetVisible(_tuftL, on);
            OverlayParts.SetVisible(_tuftR, on);
            OverlayParts.SetVisible(_tuftL2, on && _thickGroup);
            OverlayParts.SetVisible(_tuftR2, on && _thickGroup);
            OverlayParts.SetVisible(_thick, on && _thickGroup);
            if (!on)
            {
                OverlayParts.SetVisible(_bridge, false);
                return;
            }

            SetAlpha(1f);
            if (_thickGroup)
            {
                _thin = GroupSeals() >= 1 ? 1f : 0f; // al pintar un estado (Marea, carga), sin fundido
                ApplyThin();
            }

            _layoutVersion = -1; // el puente se recalcula en el siguiente Tick (el vecino puede haber cambiado)
            UpdateBridge();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            Place();
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            _layoutVersion = -1;
            if (Visible)
            {
                UpdateBridge();
            }
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.AguapeLeaf);
            into.Add(ProceduralArt.AguapeLeafLight);
            into.Add(ProceduralArt.AguapeLeafDark);
            into.Add(ProceduralArt.AguapeFlower);
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            ProceduralArt art = Host.Art;
            _mat = NewBottomImage("AguapeMat", Host.SlotBack, art.AguapeMat);
            _bridge = NewBottomImage("AguapeBridge", Host.SlotBack, art.AguapeMat);
            _bridge.transform.SetAsFirstSibling(); // detrás de la mata propia
            _thick = NewBottomImage("AguapeThick", Host.SlotBack, art.AguapeMat);
            _thick.color = ThickColor(1f);
            _tuftL = NewBottomImage("AguapeTuftL", Host.SlotFront, art.AguapeTuft);
            _tuftR = NewBottomImage("AguapeTuftR", Host.SlotFront, art.AguapeTuft);
            _tuftL2 = NewBottomImage("AguapeTuftL2", Host.SlotFront, art.AguapeTuft);
            _tuftR2 = NewBottomImage("AguapeTuftR2", Host.SlotFront, art.AguapeTuft);
            for (int i = 0; i < BitCount; i++)
            {
                _bits[i] = OverlayParts.NewImage("AguapeBit" + i, Host.SlotFront, art.AguapeBit, Color.white);
            }
        }

        private static Image NewBottomImage(string name, Transform parent, Sprite sprite)
        {
            Image img = OverlayParts.NewImage(name, parent, sprite, Color.white);
            img.rectTransform.pivot = new Vector2(0.5f, 0f);
            return img;
        }

        // Color de la capa del espeso con opacidad alpha: la mata, más oscura.
        private static Color ThickColor(float alpha)
        {
            return new Color(0.72f, 0.80f, 0.76f, ThickLayerAlpha * alpha);
        }

        // Las cuias del grupo selladas en lo que pinta la cola (llenas de una sola fruta, sin ocultas: R4). Sin asignaciones.
        private int GroupSeals()
        {
            ICuiaBoard? board = Host.Board;
            if (board == null || _group < 0)
            {
                return 0;
            }

            int seals = 0;
            for (int j = 0; j < board.Count; j++)
            {
                Cuia c = board.ShownCuia(j);
                if (c.Mechanic == MechanicType.Aguape && c.MechanicGroup == _group && c.IsFull && c.IsMonocolor && !c.HasHidden)
                {
                    seals++;
                }
            }

            return seals;
        }

        // El puente hacia la siguiente cuia del grupo, si es contigua, de la misma fila y del mismo grupo (02 §5: un grupo nunca
        // se parte entre filas). Sin asignaciones.
        private void UpdateBridge()
        {
            ICuiaBoard? board = Host.Board;
            bool on = false;
            if (board != null && Visible && !_stepping)
            {
                _layoutVersion = board.LayoutVersion;
                int i = Host.Index;
                if (i + 1 < board.Count)
                {
                    Cuia next = board.ShownCuia(i + 1);
                    Vector2 a = board.Foot(i);
                    Vector2 b = board.Foot(i + 1);
                    if (next.Mechanic == MechanicType.Aguape && next.MechanicGroup == _group && Mathf.Abs(a.y - b.y) < 1f && b.x > a.x)
                    {
                        on = true;
                        _bridgeHalfU = (b.x - a.x) / Mathf.Max(1f, _u) * 0.5f;
                    }
                }
            }

            OverlayParts.SetVisible(_bridge, on);
            if (on)
            {
                // A medio camino entre las dos cuias, del ancho del hueco más un poco y algo más baja que la mata.
                float w = Mathf.Max(1f, (2f * _bridgeHalfU) - 0.6f);
                OverlayParts.Place(_bridge, _bridgeHalfU, 0f, w, ProceduralArt.AguapeMatHeightU * 0.82f, _u);
            }
        }

        private void Place()
        {
            float half = 0.5f * CuiaGeometry.WidthU;
            float h = Mathf.Min(ProceduralArt.AguapeMatHeightU, 0.5f * _heightU);
            OverlayParts.Place(_mat, 0f, 0f, ProceduralArt.AguapeMatWidthU, h, _u);
            OverlayParts.Place(_thick, 0.08f, 0.35f, ProceduralArt.AguapeMatWidthU * 0.92f, h * 0.88f, _u);
            float tw = ProceduralArt.AguapeTuftWidthU;
            float th = Mathf.Min(ProceduralArt.AguapeTuftHeightU, 0.45f * _heightU);
            float tx = half - TuftInsetU;
            OverlayParts.Place(_tuftL, -tx, 0f, tw, th, _u);
            OverlayParts.Place(_tuftR, tx, 0f, tw, th, _u);
            OverlayParts.Place(_tuftL2, -tx - 0.12f, 0.15f, tw * 0.9f, th * 0.85f, _u);
            OverlayParts.Place(_tuftR2, tx + 0.12f, 0.15f, tw * 0.9f, th * 0.85f, _u);
            _tuftR.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // la de la derecha, reflejada
            _tuftR2.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            if (Visible)
            {
                UpdateBridge();
            }
        }

        private void ApplyThin()
        {
            OverlayParts.SetColor(_thick, ThickColor(1f - _thin));
            Color tuft2 = new Color(1f, 1f, 1f, 1f - _thin);
            OverlayParts.SetColor(_tuftL2, tuft2);
            OverlayParts.SetColor(_tuftR2, tuft2);
        }

        private void SetAlpha(float a)
        {
            var c = new Color(1f, 1f, 1f, a);
            OverlayParts.SetColor(_mat, c);
            OverlayParts.SetColor(_bridge, c);
            OverlayParts.SetColor(_tuftL, c);
            OverlayParts.SetColor(_tuftR, c);
            float thin = _thickGroup ? 1f - _thin : 1f;
            OverlayParts.SetColor(_thick, ThickColor(a * thin));
            var c2 = new Color(1f, 1f, 1f, a * thin);
            OverlayParts.SetColor(_tuftL2, c2);
            OverlayParts.SetColor(_tuftR2, c2);
        }

        // Fotograma de la suelta: la mata se funde en el primer 60 % y los trozos se alejan flotando (ease-out), girando, y se
        // apagan en la segunda mitad. Con Reducir movimiento, solo el fundido.
        private void ApplyLoosen()
        {
            float q = Mathf.Clamp01(_stepT / Mathf.Max(1f, _stepMs));
            SetAlpha(1f - Ease.Out(Mathf.Clamp01(q / 0.6f)));
            if (_stepReduce)
            {
                return;
            }

            float travel = Ease.OutCubic(q);
            float fade = 1f - Mathf.Clamp01((q - 0.45f) / 0.55f);
            float s = ProceduralArt.AguapeBitU;
            for (int i = 0; i < BitCount; i++)
            {
                Vector2 p = BitStart[i] + (BitDrift[i] * travel);
                OverlayParts.Place(_bits[i], p.x, p.y, s, s, _u);
                _bits[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, BitSpin[i] * travel);
                OverlayParts.SetColor(_bits[i], new Color(1f, 1f, 1f, fade));
            }
        }
    }
}
