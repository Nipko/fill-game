#nullable enable
using AF.Core.Board;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Aguas turbias en una cuia (07 §4.3; 02 §11 T3): pinta las dosis ocultas de pardo opaco <see cref="Palette.Murky"/>, sin
    /// patrón y nunca con su tinte real, y pone un «?» marfil de <see cref="MarkU"/> u centrado en la banda de cada una.
    /// <c>CuiaView</c> le delega el pintado (<see cref="PaintHidden"/>) y la sincroniza con el estado (<see cref="Show"/>), así
    /// que el aspecto de las turbias cambia aquí sin tocar la vista.
    /// </summary>
    /// <remarks>
    /// <para>Cuelga del cuerpo de la cuia, encima del líquido y debajo del vidrio frontal. Las bandas del líquido no giran con
    /// el vaso (guía 6.4), así que con la cuia en vuelo los «?» se ocultan (<see cref="SetInFlight"/>).</para>
    /// <para>Sin animación en esta versión: <see cref="IsAnimating"/> es false. El revelado animado (MechanicStepKind.Reveal)
    /// y la trama del shader llegan después.</para>
    /// </remarks>
    public sealed class MurkyMarks
    {
        /// <summary>Alto del «?» (07 §4.3: 0,5 u).</summary>
        public const float MarkU = 0.5f;

        private readonly RectTransform _root;
        private readonly Image[] _marks;
        private int _mask;
        private bool _inFlight;
        private float _u = 24f;

        /// <summary>Crea las marcas de una cuia de capacidad <paramref name="capacity"/> dentro de <paramref name="body"/>.</summary>
        public MurkyMarks(RectTransform body, int capacity, ProceduralArt art)
        {
            _root = OverlayParts.NewAnchor("MurkyMarks", body);
            _marks = new Image[Mathf.Clamp(capacity, 0, Cuia.MaxCapacity)];
            for (int k = 0; k < _marks.Length; k++)
            {
                _marks[k] = OverlayParts.NewImage("MurkyMark" + k, _root, art.QuestionMark, Color.white);
            }

            Layout(_u);
        }

        /// <summary>Ancla de las marcas (dentro del cuerpo).</summary>
        public RectTransform Root => _root;

        /// <summary>Hay una animación en curso: mientras, <c>CuiaView.ShowContent</c> no llama a <see cref="Show"/>.</summary>
        public bool IsAnimating => false;

        /// <summary>Dosis con «?» (bit k = dosis k desde el fondo), estén o no a la vista.</summary>
        public int MarkMask => _mask;

        /// <summary>El «?» de la dosis <paramref name="k"/> está a la vista.</summary>
        public bool MarkVisible(int k)
        {
            return (uint)k < (uint)_marks.Length && _marks[k].gameObject.activeSelf;
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

        /// <summary>Coloca cada «?» en el centro de su banda (u en dp).</summary>
        public void Layout(float u)
        {
            _u = u;
            for (int k = 0; k < _marks.Length; k++)
            {
                OverlayParts.Place(_marks[k], 0f, CuiaGeometry.CavityBottomU + k + 0.5f, MarkU, MarkU, _u);
            }
        }

        /// <summary>Con la cuia en vuelo (inclinada) los «?» se ocultan; al volver, reaparecen.</summary>
        public void SetInFlight(bool inFlight)
        {
            _inFlight = inFlight;
            Apply();
        }

        /// <summary>Avanza las animaciones (ninguna en esta versión).</summary>
        public void Tick(float dtMs)
        {
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
