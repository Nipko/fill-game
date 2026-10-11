#nullable enable
using AF.Core.Board;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Pulso del modo de selección de la Luciérnaga (tarea T14 del paso 1; 04 §2.5: «toca el botón, las cuias turbias pulsan,
    /// toca la cuia»): un aro de luz aditivo (<see cref="ProceduralArt.RevealHalo"/> en <see cref="ProceduralArt.RevealHaloColor"/>)
    /// que rodea la boca y los lados de cada cuia con dosis ocultas y se apaga al llegar al pie, y que late mientras dura la
    /// selección. Es la única excepción a D-002 (al
    /// seleccionar no se resalta ningún destino): solo existe en este modo y solo en las cuias turbias. Va en la capa de
    /// mecánicas delantera del tablero (arquitectura del paso 1, §6), con las posiciones del contenedor del tablero.
    /// </summary>
    /// <remarks>
    /// Las imágenes se crean la primera vez que hacen falta (una por cuia, como mucho <see cref="BoardState.MaxCuias"/>) y
    /// después solo se encienden, se colocan y se tiñen: sin asignaciones por fotograma (08 §5.4). Con Reducir movimiento
    /// (07 §7.8) el aro no late: queda fijo con el alfa medio. Valores [P].
    /// </remarks>
    internal sealed class RevealPulse
    {
        /// <summary>Periodo del latido (ms) [P].</summary>
        public const float PeriodMs = 900f;

        /// <summary>Alfa del aro en el valle del latido [P].</summary>
        public const float AlphaMin = 0.35f;

        /// <summary>Alfa del aro en la cresta del latido [P].</summary>
        public const float AlphaMax = 0.95f;

        /// <summary>Escala del aro en la cresta del latido [P].</summary>
        public const float ScaleMax = 1.035f;

        /// <summary>Separación entre la silueta de la cuia y la línea del aro (u) [P].</summary>
        public const float RingMarginU = 0.14f;

        /// <summary>Brillo de fuera del aro, del canto de la línea al de la imagen (u) [P].</summary>
        public const float GlowU = 0.30f;

        private readonly RectTransform _root;
        private readonly ProceduralArt _art;
        private readonly Image?[] _halos = new Image?[BoardState.MaxCuias];
        private readonly Material? _additive;
        private int _mask;
        private float _t;
        private bool _reduceMotion;
        private float _alpha;

        /// <summary>
        /// Crea el pulso (sin imágenes todavía) en la capa <paramref name="layer"/>, en un Canvas anidado propio: el latido no
        /// reconstruye el lienzo principal (guía 8, «Reconstrucción de Canvas»).
        /// </summary>
        public RevealPulse(RectTransform layer, ProceduralArt art)
        {
            _art = art;
            _additive = UiMaterials.Additive;
            _root = UiBuilder.Rect("RevealPulse", layer);
            _root.gameObject.AddComponent<Canvas>();
        }

        /// <summary>Contenedor de los aros (tests de orden de capas).</summary>
        public RectTransform Root => _root;

        /// <summary>Hay cuias latiendo (el modo de selección está abierto).</summary>
        public bool Active => _mask != 0;

        /// <summary>Cuias que laten (bit i = cuia i).</summary>
        public int Mask => _mask;

        /// <summary>Alfa actual del aro (tests).</summary>
        public float Alpha => _alpha;

        /// <summary>La cuia <paramref name="cuia"/> late (su aro está a la vista).</summary>
        public bool IsPulsing(int cuia)
        {
            if ((uint)cuia >= (uint)_halos.Length || ((_mask >> cuia) & 1) == 0)
            {
                return false;
            }

            Image? halo = _halos[cuia];
            return halo != null && halo.gameObject.activeSelf;
        }

        /// <summary>Aro de la cuia <paramref name="cuia"/> (null si nunca latió; tests y capturas).</summary>
        public Image? HaloOf(int cuia)
        {
            return (uint)cuia < (uint)_halos.Length ? _halos[cuia] : null;
        }

        /// <summary>
        /// Enciende el aro de las cuias de <paramref name="mask"/> (las turbias) en <paramref name="views"/> y apaga los demás. El
        /// latido empieza en la cresta.
        /// </summary>
        public void Show(int mask, CuiaView[] views, bool reduceMotion)
        {
            _reduceMotion = reduceMotion;
            _mask = 0;
            for (int i = 0; i < _halos.Length; i++)
            {
                bool on = i < views.Length && ((mask >> i) & 1) != 0;
                if (on)
                {
                    _mask |= 1 << i;
                    Image halo = Ensure(i);
                    if (!halo.gameObject.activeSelf)
                    {
                        halo.gameObject.SetActive(true);
                    }
                }
                else if (_halos[i] != null && _halos[i]!.gameObject.activeSelf)
                {
                    _halos[i]!.gameObject.SetActive(false);
                }
            }

            _t = 0f;
            Layout(views);
            Apply();
        }

        /// <summary>Apaga todos los aros.</summary>
        public void Hide()
        {
            _mask = 0;
            for (int i = 0; i < _halos.Length; i++)
            {
                if (_halos[i] != null && _halos[i]!.gameObject.activeSelf)
                {
                    _halos[i]!.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Coloca los aros encendidos alrededor de su cuia (tras cada recolocación del tablero): por encima de la boca y a los
        /// lados, a <see cref="RingMarginU"/> de la silueta, y abiertos por abajo hasta el pie, donde se apagan (el aro no cruza
        /// la repisa ni la insignia «?×n» de debajo).
        /// </summary>
        public void Layout(CuiaView[] views)
        {
            for (int i = 0; i < _halos.Length && i < views.Length; i++)
            {
                Image? halo = _halos[i];
                if (halo == null || ((_mask >> i) & 1) == 0)
                {
                    continue;
                }

                CuiaView view = views[i];
                float u = Mathf.Max(1f, view.Unit);
                float margin = (RingMarginU + GlowU) * u;
                float w = (CuiaGeometry.WidthU * u) + (2f * margin);
                float h = (view.HeightU * u) + margin;
                Vector2 foot = view.FootPosition;
                RectTransform rt = halo.rectTransform;
                rt.anchoredPosition = new Vector2(foot.x, foot.y + (0.5f * h));
                rt.sizeDelta = new Vector2(w, h);

                // La línea del aro queda a GlowU del canto de la imagen: el 9-slice escala su borde con u.
                halo.pixelsPerUnitMultiplier = ProceduralArt.RevealHaloGlowPx / (GlowU * u);
            }
        }

        /// <summary>Avanza el latido.</summary>
        public void Tick(float dtMs)
        {
            if (_mask == 0)
            {
                return;
            }

            _t += dtMs;
            Apply();
        }

        private void Apply()
        {
            float wave = _reduceMotion ? 0.5f : 0.5f + (0.5f * Mathf.Cos(_t / PeriodMs * 2f * Mathf.PI)); // 1 en la cresta
            _alpha = Mathf.Lerp(AlphaMin, AlphaMax, wave);
            float scale = _reduceMotion ? 1f : Mathf.Lerp(1f, ScaleMax, wave);
            Color c = ProceduralArt.RevealHaloColor;
            var color = new Color(c.r, c.g, c.b, _alpha);
            var s = new Vector3(scale, scale, 1f);
            for (int i = 0; i < _halos.Length; i++)
            {
                Image? halo = _halos[i];
                if (halo == null || ((_mask >> i) & 1) == 0)
                {
                    continue;
                }

                halo.color = color;
                halo.rectTransform.localScale = s;
            }
        }

        private Image Ensure(int i)
        {
            Image? halo = _halos[i];
            if (halo != null)
            {
                return halo;
            }

            halo = UiBuilder.Sliced("Halo" + i, _root, _art.RevealHalo, Color.clear);
            RectTransform rt = halo.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            if (_additive != null)
            {
                halo.material = _additive;
            }

            halo.gameObject.SetActive(false);
            _halos[i] = halo;
            return halo;
        }
    }
}
