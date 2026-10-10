#nullable enable
using AF.Core.Board;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Aguas turbias en una cuia (07 §4.3; 02 §11 T3): pinta las dosis ocultas de pardo opaco <see cref="Palette.Murky"/>, sin
    /// patrón y nunca con su tinte real, y pone un «?» marfil de <see cref="MarkU"/> u centrado en la banda de cada una.
    /// <c>CuiaView</c> le delega el pintado (<see cref="PaintHidden"/>), la sincroniza con el estado (<see cref="Show"/>) y le
    /// delega el revelado animado (<see cref="PlayReveal"/>, <c>CuiaView.PlayReveal</c>), así que el aspecto de las turbias
    /// cambia aquí sin tocar la vista.
    /// </summary>
    /// <remarks>
    /// <para>Cuelga del cuerpo de la cuia, encima del líquido y debajo del vidrio frontal. Las bandas del líquido no giran con
    /// el vaso (guía 6.4), así que con la cuia en vuelo los «?» se ocultan (<see cref="SetInFlight"/>).</para>
    /// <para>Revelado por defecto (T07, hasta que T15 aplique b.2): con <see cref="RevealStyle.Rule"/> (R7, la nueva cima del
    /// origen) cada capa funde del pardo a su tinte y el «?» se apaga en <see cref="RevealRuleMs"/> (02 §4.1; 07 §4.3); con
    /// <see cref="RevealStyle.Firefly"/> las capas se aclaran de arriba abajo en <see cref="RevealFireflyMs"/> (04 §2.5). El
    /// patrón del tinte aparece a mitad del fundido. Mientras anima (<see cref="IsAnimating"/>), <c>ShowContent</c> no llama a
    /// <see cref="Show"/>; la cola termina el paso con <see cref="Snap"/> y el <c>ShowContent</c> final. Sin asignaciones.</para>
    /// </remarks>
    public sealed class MurkyMarks
    {
        /// <summary>Alto del «?» marfil, sin el halo (07 §4.3: 0,5 u).</summary>
        public const float MarkU = ProceduralArt.QuestionGlyphU;

        /// <summary>Revelado por R7: fundido del pardo al tinte en 300 ms (02 §4.1; 07 §4.3) [P hasta b.2, T15].</summary>
        public const float RevealRuleMs = 300f;

        /// <summary>Revelado de la Luciérnaga: las capas se aclaran de arriba abajo en 600 ms (04 §2.5).</summary>
        public const float RevealFireflyMs = 600f;

        private readonly RectTransform _root;
        private readonly Image[] _marks;
        private readonly Color[] _target;
        private readonly int[] _targetTint;
        private int _mask;
        private bool _inFlight;
        private float _u = 24f;
        private LiquidGraphic? _liquid;
        private float _animMs = -1f;
        private float _animDur = 1f;
        private int _animMask;
        private RevealStyle _style;

        /// <summary>Crea las marcas de una cuia de capacidad <paramref name="capacity"/> dentro de <paramref name="body"/>.</summary>
        public MurkyMarks(RectTransform body, int capacity, ProceduralArt art)
        {
            _root = OverlayParts.NewAnchor("MurkyMarks", body);
            _marks = new Image[Mathf.Clamp(capacity, 0, Cuia.MaxCapacity)];
            _target = new Color[_marks.Length];
            _targetTint = new int[_marks.Length];
            for (int k = 0; k < _marks.Length; k++)
            {
                _marks[k] = OverlayParts.NewImage("MurkyMark" + k, _root, art.QuestionMark, Color.white);
            }

            Layout(_u);
        }

        /// <summary>Ancla de las marcas (dentro del cuerpo).</summary>
        public RectTransform Root => _root;

        /// <summary>Hay una animación en curso: mientras, <c>CuiaView.ShowContent</c> no llama a <see cref="Show"/>.</summary>
        public bool IsAnimating => _animMs >= 0f;

        /// <summary>Capas que se están revelando (bit k = dosis k desde el fondo); 0 sin animación.</summary>
        public int RevealingMask => IsAnimating ? _animMask : 0;

        /// <summary>Avance del revelado en curso (0..1); 1 sin animación.</summary>
        public float RevealProgress => IsAnimating ? Mathf.Clamp01(_animMs / _animDur) : 1f;

        /// <summary>Dosis con «?» (bit k = dosis k desde el fondo), estén o no a la vista.</summary>
        public int MarkMask => _mask;

        /// <summary>El «?» de la dosis <paramref name="k"/> está a la vista.</summary>
        public bool MarkVisible(int k)
        {
            return (uint)k < (uint)_marks.Length && _marks[k].gameObject.activeSelf;
        }

        /// <summary>Duración nominal (ms, sin factor de velocidad) del revelado <paramref name="style"/>.</summary>
        public float StepDurationMs(RevealStyle style)
        {
            return style == RevealStyle.Firefly ? RevealFireflyMs : RevealRuleMs;
        }

        /// <summary>
        /// Pinta la dosis oculta <paramref name="k"/>: pardo opaco <see cref="Palette.Murky"/>, llena, sin patrón (tinte 0), y su
        /// «?». Devuelve el color pintado, para que la vista lleve la cuenta (color y nivel del líquido).
        /// </summary>
        public Color PaintHidden(LiquidGraphic liquid, int k)
        {
            liquid.SetDose(k, Palette.Murky, 1f, 0);
            if (!IsAnimating && (uint)k < (uint)_marks.Length)
            {
                SetMask(_mask | (1 << k));
            }

            return Palette.Murky;
        }

        /// <summary>Deja un «?» en cada dosis oculta de <paramref name="cuia"/> y ninguno en las demás. Idempotente.</summary>
        public void Show(Cuia cuia)
        {
            SetMask(cuia.Count > 0 ? cuia.HiddenMask : 0);
        }

        /// <summary>
        /// Arranca el revelado de las capas <paramref name="layerMask"/> de <paramref name="shown"/> (el estado que muestra la
        /// cuia, con el tinte real de cada capa): las pinta turbias con su «?» en este mismo fotograma y las funde a su tinte
        /// (ver la clase). Devuelve la duración (ms) ya multiplicada por <paramref name="speedFactor"/>, o 0 si no hay capas que
        /// revelar. Sin asignaciones.
        /// </summary>
        public float PlayReveal(LiquidGraphic liquid, int layerMask, RevealStyle style, Cuia shown, float speedFactor)
        {
            int layers = layerMask & ((1 << Mathf.Min(_marks.Length, shown.Count)) - 1);
            if (liquid == null || layers == 0)
            {
                return 0f;
            }

            Snap();
            _liquid = liquid;
            _animMask = layers;
            _style = style;
            _animDur = Mathf.Max(1f, StepDurationMs(style) * Mathf.Max(0.01f, speedFactor));
            _animMs = 0f;
            for (int k = 0; k < _marks.Length; k++)
            {
                if (((layers >> k) & 1) == 0)
                {
                    continue;
                }

                int tint = shown.ColorAt(k);
                bool mud = tint == Cuia.MudColor;
                _target[k] = mud ? Palette.Mud : Palette.Tint(tint);
                _targetTint[k] = mud ? 0 : tint;
            }

            SetMask(_mask | layers);
            ApplyReveal(0f);
            return _animDur;
        }

        /// <summary>Termina en el acto el revelado en curso (las capas con su tinte y sin «?»).</summary>
        public void Snap()
        {
            if (!IsAnimating)
            {
                return;
            }

            ApplyReveal(1f);
            Finish();
        }

        /// <summary>Quita el «?» de la dosis <paramref name="k"/> (la intro, antes de que el agua se enturbie).</summary>
        public void HideMark(int k)
        {
            if ((uint)k < (uint)_marks.Length)
            {
                SetMask(_mask & ~(1 << k));
            }
        }

        /// <summary>Quita todos los «?».</summary>
        public void Clear()
        {
            SetMask(0);
        }

        /// <summary>
        /// Coloca cada «?» en el centro de su banda (u en dp). La caja es el sprite entero
        /// (<see cref="ProceduralArt.QuestionMarkU"/>, con el margen del halo), así que el glifo mide <see cref="MarkU"/>.
        /// </summary>
        public void Layout(float u)
        {
            _u = u;
            const float box = ProceduralArt.QuestionMarkU;
            for (int k = 0; k < _marks.Length; k++)
            {
                OverlayParts.Place(_marks[k], 0f, CuiaGeometry.CavityBottomU + k + 0.5f, box, box, _u);
            }
        }

        /// <summary>Con la cuia en vuelo (inclinada) los «?» se ocultan; al volver, reaparecen.</summary>
        public void SetInFlight(bool inFlight)
        {
            _inFlight = inFlight;
            Apply();
        }

        /// <summary>Avanza el revelado en curso (sin asignaciones).</summary>
        public void Tick(float dtMs)
        {
            if (!IsAnimating)
            {
                return;
            }

            _animMs += dtMs;
            float t = Mathf.Clamp01(_animMs / _animDur);
            ApplyReveal(t);
            if (t >= 1f)
            {
                Finish();
            }
        }

        // Fotograma t (0..1) del revelado: cada capa funde del pardo a su tinte y su «?» se apaga; con la Luciérnaga, de
        // arriba abajo (cada capa ocupa su parte del tiempo).
        private void ApplyReveal(float t)
        {
            if (_liquid == null)
            {
                return;
            }

            int layers = 0;
            for (int k = 0; k < _marks.Length; k++)
            {
                layers += (_animMask >> k) & 1;
            }

            int above = 0;
            for (int k = _marks.Length - 1; k >= 0; k--)
            {
                if (((_animMask >> k) & 1) == 0)
                {
                    continue;
                }

                float local = _style == RevealStyle.Firefly && layers > 1 ? Mathf.Clamp01((t * layers) - above) : t;
                above++;
                float e = Ease.InOut(local);
                _liquid.SetDose(k, Color.Lerp(Palette.Murky, _target[k], e), 1f, local >= 0.5f ? _targetTint[k] : 0);
                OverlayParts.SetColor(_marks[k], new Color(1f, 1f, 1f, 1f - e));
            }
        }

        private void Finish()
        {
            int done = _animMask;
            _animMs = -1f;
            _animMask = 0;
            for (int k = 0; k < _marks.Length; k++)
            {
                if (((done >> k) & 1) != 0)
                {
                    OverlayParts.SetColor(_marks[k], Color.white);
                }
            }

            SetMask(_mask & ~done);
        }

        private void SetMask(int mask)
        {
            _mask = mask;
            Apply();
        }

        private void Apply()
        {
            for (int k = 0; k < _marks.Length; k++)
            {
                OverlayParts.SetVisible(_marks[k], !_inFlight && ((_mask >> k) & 1) != 0);
            }
        }
    }
}
