#nullable enable
using AF.Core.Board;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Barro en una cuia (02 §9.1 M5; color 15 del motor): pinta cada dosis de Barro de <see cref="Palette.Mud"/>, opaca y sin
    /// patrón, con grumos más oscuros encima (<see cref="ProceduralArt.MudLumps"/>). <c>CuiaView</c> le delega el pintado
    /// (<see cref="PaintMud"/>) y la sincroniza con el estado (<see cref="Show"/>), así que el aspecto del Barro cambia aquí sin
    /// tocar la vista.
    /// </summary>
    /// <remarks>
    /// Cuelga del cuerpo de la cuia, encima del líquido. Las bandas no giran con el vaso, así que en vuelo los grumos se
    /// ocultan. Sin animación en esta versión (<see cref="IsAnimating"/> = false): el lavado (MechanicStepKind.MudWash)
    /// llega después.
    /// </remarks>
    public sealed class MudMarks
    {
        private readonly RectTransform _root;
        private readonly Image[] _lumps;
        private int _mask;
        private bool _inFlight;
        private float _u = 24f;

        /// <summary>Crea los grumos de una cuia de capacidad <paramref name="capacity"/> dentro de <paramref name="body"/>.</summary>
        public MudMarks(RectTransform body, int capacity, ProceduralArt art)
        {
            _root = OverlayParts.NewAnchor("MudMarks", body);
            _lumps = new Image[Mathf.Clamp(capacity, 0, Cuia.MaxCapacity)];
            for (int k = 0; k < _lumps.Length; k++)
            {
                _lumps[k] = OverlayParts.NewImage("MudLumps" + k, _root, art.MudLumps, Color.white);
            }

            Layout(_u);
        }

        /// <summary>Ancla de los grumos (dentro del cuerpo).</summary>
        public RectTransform Root => _root;

        /// <summary>Hay una animación en curso: mientras, <c>CuiaView.ShowContent</c> no llama a <see cref="Show"/>.</summary>
        public bool IsAnimating => false;

        /// <summary>Dosis con grumos (bit k = dosis k desde el fondo), estén o no a la vista.</summary>
        public int LumpMask => _mask;

        /// <summary>Los grumos de la dosis <paramref name="k"/> están a la vista.</summary>
        public bool LumpsVisible(int k)
        {
            return (uint)k < (uint)_lumps.Length && _lumps[k].gameObject.activeSelf;
        }

        /// <summary>
        /// Pinta la dosis de Barro <paramref name="k"/>: <see cref="Palette.Mud"/> opaco, lleno y sin patrón (tinte 0), con sus
        /// grumos. Devuelve el color pintado, para que la vista lleve la cuenta.
        /// </summary>
        public Color PaintMud(LiquidGraphic liquid, int k)
        {
            liquid.SetDose(k, Palette.Mud, 1f, 0);
            if (!IsAnimating && (uint)k < (uint)_lumps.Length)
            {
                SetMask(_mask | (1 << k));
            }

            return Palette.Mud;
        }

        /// <summary>Deja grumos en cada dosis visible de Barro de <paramref name="cuia"/> y en ninguna otra. Idempotente.</summary>
        public void Show(Cuia cuia)
        {
            int mask = 0;
            int count = Mathf.Min(cuia.Count, _lumps.Length);
            for (int k = 0; k < count; k++)
            {
                if (!cuia.IsHiddenAt(k) && cuia.ColorAt(k) == Cuia.MudColor)
                {
                    mask |= 1 << k;
                }
            }

            SetMask(mask);
        }

        /// <summary>Quita los grumos de la dosis <paramref name="k"/> (la intro, antes de que el agua se enturbie).</summary>
        public void HideMark(int k)
        {
            if ((uint)k < (uint)_lumps.Length)
            {
                SetMask(_mask & ~(1 << k));
            }
        }

        /// <summary>Quita todos los grumos.</summary>
        public void Clear()
        {
            SetMask(0);
        }

        /// <summary>Coloca los grumos de cada banda (u en dp): el ancho de la cavidad y la banda entera.</summary>
        public void Layout(float u)
        {
            _u = u;
            for (int k = 0; k < _lumps.Length; k++)
            {
                OverlayParts.Place(_lumps[k], 0f, CuiaGeometry.CavityBottomU + k + 0.5f, 2f * CuiaGeometry.CavityHalfWidthU, 1f, _u);
            }
        }

        /// <summary>Con la cuia en vuelo (inclinada) los grumos se ocultan; al volver, reaparecen.</summary>
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
            for (int k = 0; k < _lumps.Length; k++)
            {
                OverlayParts.SetVisible(_lumps[k], !_inFlight && ((_mask >> k) & 1) != 0);
            }
        }
    }
}
