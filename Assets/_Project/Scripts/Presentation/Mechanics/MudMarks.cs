#nullable enable
using AF.Core.Board;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Barro en una cuia (02 §9.1 M5; color 15 del motor): pinta cada dosis de Barro de <see cref="Palette.Mud"/>, opaca y sin
    /// patrón (tinte 0), con grumos más oscuros encima (<see cref="ProceduralArt.MudLumps"/>). <c>CuiaView</c> le delega el
    /// pintado (<see cref="PaintMud"/>) y la sincroniza con el estado (<see cref="Show"/>), así que el aspecto del Barro
    /// cambia aquí sin tocar la vista.
    /// </summary>
    /// <remarks>
    /// <para><b>Pendiente: el brillo.</b> T16 pide el Barro «opaco, con grumos y sin brillo». <c>LiquidGraphic</c> pinta el
    /// tinte 0 como agua base, con sus franjas especulares (+10 L*) y, si el barro queda en la cima, la cara superior aclarada
    /// un 20 % y la línea clara de su borde; los grumos no las tapan. Quitarlas pide una vía mate en <c>LiquidGraphic</c> y su
    /// shader (dueño en la ola 3: T15), que <see cref="PaintMud"/> usará; hasta entonces el Barro lleva el brillo del agua
    /// base.</para>
    /// <para>Cuelga del cuerpo de la cuia, encima del líquido. Las bandas no giran con el vaso, así que en vuelo los grumos se
    /// ocultan.</para>
    /// <para><b>Lavado</b> (paso <see cref="MechanicStepKind.MudWash"/> de la cola, etapa (a), en el receptor; propuesta de
    /// D-007 b.2 M5: «el barro se enturbia y se disuelve en la fruta nueva (~400 ms), con un chapoteo»): <c>CuiaView.PlayMudWash</c>
    /// lo delega en <see cref="PlayWash"/>, que dura <see cref="WashMs"/> × factor de velocidad. Al empezar, el trabajo del
    /// vertido ya llenó la dosis con la fruta nueva (el chorro la pintó sobre el barro, que se veía con sus grumos): la dosis
    /// se enturbia hasta <see cref="PeakTurbidity"/> de barro en el primer 30 % con una nube parda que gira
    /// (<see cref="ProceduralArt.BarroCloud"/>), los grumos se deshacen (crecen y se apagan) y el agua se aclara hasta la fruta,
    /// con una salpicadura parda al empezar (<see cref="IBoardFx.Splash"/>; con Reducir movimiento la capa de efectos no
    /// emite). Mientras dura, <see cref="IsAnimating"/> es true y <c>CuiaView.ShowContent</c> no llama a <see cref="Show"/>;
    /// al terminar, la cola llama a <see cref="Snap"/> y a <c>ShowContent(After)</c>.</para>
    /// <para>Sin asignaciones tras construirse ni dentro de un paso: la nube y los grumos se crean una vez, y el sprite de la
    /// nube (<see cref="ProceduralArt.BarroCloud"/>, una textura que se genera la primera vez) se pide la primera vez que la
    /// cuia tiene Barro, a la vista o bajo una oculta: al cargar el nivel (<see cref="Show"/> desde <c>ShowContent</c>) o al
    /// pintarlo (<see cref="PaintMud"/>), nunca a mitad del lavado. Un nivel sin Barro no la crea.</para>
    /// </remarks>
    public sealed class MudMarks
    {
        /// <summary>Lavado del Barro: 400 ms [P] (propuesta de D-007 b.2 M5).</summary>
        public const float WashMs = 400f;

        /// <summary>Barro que llega a tener la dosis lavada en el punto más turbio (mezcla con la fruta) [P].</summary>
        public const float PeakTurbidity = 0.7f;

        /// <summary>Fracción del lavado en la que el agua está más turbia [P].</summary>
        public const float TurbidPeakAt = 0.3f;

        /// <summary>Fracción del lavado en la que los grumos ya se han deshecho [P].</summary>
        public const float LumpsGoneAt = 0.6f;

        private const float CloudAlpha = 0.9f;

        private readonly RectTransform _root;
        private readonly Image _cloud;
        private readonly Image[] _lumps;
        private readonly ProceduralArt _art;
        private readonly CuiaView? _view;
        private int _mask;
        private bool _inFlight;
        private float _u = 24f;
        private LiquidGraphic? _washLiquid;
        private int _washLayer = -1;
        private int _washTint;
        private float _washMs;
        private float _washElapsed;
        private bool _washing;
        private bool _washFinal;
        private Color _washColor;
        private float _turbidity;
        private float _lumpAlpha;

        /// <summary>Crea los grumos de una cuia de capacidad <paramref name="capacity"/> dentro de <paramref name="body"/>.</summary>
        public MudMarks(RectTransform body, int capacity, ProceduralArt art)
        {
            _art = art;
            _view = body.parent != null ? body.parent.GetComponent<CuiaView>() : null; // la ranura de la cuia (efectos y pie)
            _root = OverlayParts.NewAnchor("MudMarks", body);
            _cloud = OverlayParts.NewImage("MudCloud", _root, null, Color.white);
            PreRegister(_cloud); // se ve a mitad de un paso: registrada desde ya en su Canvas
            _lumps = new Image[Mathf.Clamp(capacity, 0, Cuia.MaxCapacity)];
            for (int k = 0; k < _lumps.Length; k++)
            {
                _lumps[k] = OverlayParts.NewImage("MudLumps" + k, _root, art.MudLumps, Color.white);
            }

            Layout(_u);
        }

        /// <summary>Ancla de los grumos (dentro del cuerpo).</summary>
        public RectTransform Root => _root;

        /// <summary>Hay un lavado en curso: mientras, <c>CuiaView.ShowContent</c> no llama a <see cref="Show"/>.</summary>
        public bool IsAnimating => _washing;

        /// <summary>Duración nominal del lavado (ms, sin factor de velocidad): <see cref="WashMs"/>.</summary>
        public float WashDurationMs => WashMs;

        /// <summary>Dosis con grumos (bit k = dosis k desde el fondo), estén o no a la vista.</summary>
        public int LumpMask => _mask;

        /// <summary>Dosis que se está lavando (−1 sin lavado).</summary>
        public int WashLayer => _washing ? _washLayer : -1;

        /// <summary>Avance del lavado en curso (0..1; 0 sin lavado).</summary>
        public float WashProgress => _washing && _washMs > 0f ? Mathf.Clamp01(_washElapsed / _washMs) : 0f;

        /// <summary>Barro en la mezcla de la dosis lavada (0 = fruta limpia; como mucho <see cref="PeakTurbidity"/>).</summary>
        public float Turbidity => _washing ? _turbidity : 0f;

        /// <summary>Color que pinta ahora el lavado en su dosis (la fruta mezclada con el barro).</summary>
        public Color WashColor => _washColor;

        /// <summary>Opacidad de los grumos que se deshacen (0 sin lavado).</summary>
        public float WashLumpAlpha => _washing ? _lumpAlpha : 0f;

        /// <summary>La nube de barro del lavado está a la vista.</summary>
        public bool CloudVisible => _cloud.gameObject.activeSelf;

        /// <summary>El sprite de la nube ya está pedido (la cuia tuvo Barro): el lavado no tiene que generarlo.</summary>
        public bool CloudReady => _cloud.sprite != null;

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
            EnsureCloud();
            liquid.SetDose(k, Palette.Mud, 1f, 0);
            if (!IsAnimating && (uint)k < (uint)_lumps.Length)
            {
                SetMask(_mask | (1 << k));
            }

            return Palette.Mud;
        }

        /// <summary>
        /// Deja grumos en cada dosis visible de Barro de <paramref name="cuia"/> y en ninguna otra. Idempotente. Si la cuia
        /// tiene Barro (también bajo una oculta, que se lavará cuando se revele), pide ya el sprite de la nube: así el lavado
        /// no genera la textura a mitad del paso.
        /// </summary>
        public void Show(Cuia cuia)
        {
            int mask = 0;
            bool anyMud = false;
            int count = Mathf.Min(cuia.Count, _lumps.Length);
            for (int k = 0; k < count; k++)
            {
                if (cuia.ColorAt(k) != Cuia.MudColor)
                {
                    continue;
                }

                anyMud = true;
                if (!cuia.IsHiddenAt(k))
                {
                    mask |= 1 << k;
                }
            }

            if (anyMud)
            {
                EnsureCloud();
            }

            SetMask(mask);
        }

        /// <summary>
        /// Arranca el lavado del barro de la capa más baja de <paramref name="layerMask"/> (la dosis de barro que el vertido
        /// sustituyó; con una máscara negativa, la cima de lo que muestra la cuia) y devuelve su duración (ms):
        /// <see cref="WashMs"/> × <paramref name="speedFactor"/>, o 0 si la capa no existe. El <c>ShowContent</c> del final del
        /// paso deja los grumos del estado.
        /// </summary>
        public float PlayWash(LiquidGraphic liquid, int layerMask, float speedFactor)
        {
            Cuia shown = _view != null ? _view.ShownCuia : default;
            int k = layerMask < 0 ? shown.Count - 1 : LowestBit(layerMask);
            float ms = WashMs * speedFactor;
            if (k < 0 || k >= _lumps.Length || ms <= 0f)
            {
                return 0f;
            }

            if (_washing)
            {
                Snap();
            }

            int tint = k < shown.Count && !shown.IsHiddenAt(k) ? shown.ColorAt(k) : 0;
            _washLiquid = liquid;
            _washLayer = k;
            _washTint = tint == Cuia.MudColor ? 0 : tint;
            _washMs = ms;
            _washElapsed = 0f;
            _washing = true;
            _washFinal = false;
            EnsureCloud(); // ya pedido al mostrar el Barro (Show o PaintMud); solo un respaldo, que no asigna si ya está

            OverlayParts.Place(_cloud, 0f, CuiaGeometry.CavityBottomU + k + 0.5f, 2f * CuiaGeometry.CavityHalfWidthU, 1f, _u);
            Splash(k);
            PaintWash(0f);
            return ms;
        }

        /// <summary>Termina en el acto el lavado en curso, si lo hay: la dosis con su fruta y los grumos como estaban.</summary>
        public void Snap()
        {
            if (!_washing)
            {
                return;
            }

            if (_washLiquid != null && _washTint > 0)
            {
                _washLiquid.SetDose(_washLayer, Palette.Tint(_washTint), 1f, _washTint);
            }

            int k = _washLayer;
            _washing = false;
            _washFinal = false;
            _washLiquid = null;
            _washLayer = -1;
            _turbidity = 0f;
            _lumpAlpha = 0f;
            if ((uint)k < (uint)_lumps.Length)
            {
                Image lump = _lumps[k];
                OverlayParts.SetColor(lump, Color.white);
                lump.rectTransform.localScale = Vector3.one;
                OverlayParts.Place(lump, 0f, CuiaGeometry.CavityBottomU + k + 0.5f, 2f * CuiaGeometry.CavityHalfWidthU, 1f, _u);
            }

            Apply();
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

            if (_washing)
            {
                OverlayParts.Place(_cloud, 0f, CuiaGeometry.CavityBottomU + _washLayer + 0.5f, 2f * CuiaGeometry.CavityHalfWidthU, 1f, _u);
            }
        }

        /// <summary>Con la cuia en vuelo (inclinada) los grumos se ocultan; al volver, reaparecen.</summary>
        public void SetInFlight(bool inFlight)
        {
            _inFlight = inFlight;
            Apply();
        }

        /// <summary>Avanza el lavado en curso (sin asignaciones).</summary>
        public void Tick(float dtMs)
        {
            if (!_washing || _washFinal)
            {
                return;
            }

            _washElapsed += dtMs;
            float t = Mathf.Clamp01(_washElapsed / _washMs);
            PaintWash(t);
            if (t >= 1f)
            {
                _washFinal = true; // la cola cierra con Snap y ShowContent(After)
            }
        }

        // Turbidez del lavado en t (0..1): sube hasta PeakTurbidity en TurbidPeakAt y se aclara hasta 0.
        private static float TurbidityAt(float t)
        {
            if (t <= TurbidPeakAt)
            {
                return PeakTurbidity * Ease.Out(t / TurbidPeakAt);
            }

            return PeakTurbidity * (1f - Ease.InOut((t - TurbidPeakAt) / (1f - TurbidPeakAt)));
        }

        // Un fotograma del lavado: la dosis mezclada con el barro, la nube que gira y se apaga y los grumos que se deshacen.
        private void PaintWash(float t)
        {
            int k = _washLayer;
            _turbidity = TurbidityAt(t);
            if (_washLiquid != null && _washTint > 0)
            {
                _washColor = Color.Lerp(Palette.Tint(_washTint), Palette.Mud, _turbidity);
                _washLiquid.SetDose(k, _washColor, 1f, _washTint);
            }
            else
            {
                _washColor = Palette.Mud;
            }

            float cloud = _turbidity / PeakTurbidity;
            OverlayParts.SetColor(_cloud, new Color(1f, 1f, 1f, CloudAlpha * cloud));
            float spread = 0.82f + (0.18f * Ease.Out(t));
            var cloudScale = new Vector3(spread, 1f, 1f);
            RectTransform crt = _cloud.rectTransform;
            if (crt.localScale != cloudScale)
            {
                crt.localScale = cloudScale;
            }

            Quaternion swirl = Quaternion.Euler(0f, 0f, 10f * Mathf.Sin(t * Mathf.PI * 2f)); // la nube gira y vuelve
            if (crt.localRotation != swirl)
            {
                crt.localRotation = swirl;
            }

            _lumpAlpha = t < LumpsGoneAt ? 1f - Ease.In(t / LumpsGoneAt) : 0f;
            Image lump = _lumps[k];
            OverlayParts.SetColor(lump, new Color(1f, 1f, 1f, _lumpAlpha));
            float grow = 1f + (0.35f * Mathf.Clamp01(t / LumpsGoneAt));
            var lumpScale = new Vector3(grow, grow, 1f);
            if (lump.rectTransform.localScale != lumpScale)
            {
                lump.rectTransform.localScale = lumpScale;
            }

            float drift = 0.06f * Mathf.Sin(t * Mathf.PI * 3f) * _u; // los grumos se mecen al deshacerse
            var at = new Vector2(drift, (CuiaGeometry.CavityBottomU + k + 0.5f + (0.08f * t)) * _u);
            if (lump.rectTransform.anchoredPosition != at)
            {
                lump.rectTransform.anchoredPosition = at;
            }

            Apply();
        }

        // Chapoteo pardo en la superficie de la dosis lavada (espacio del contenedor del tablero, el de la capa de efectos).
        private void Splash(int k)
        {
            if (_view == null)
            {
                return;
            }

            IBoardFx? fx = _view.OverlayHost != null ? _view.OverlayHost.Fx : null;
            if (fx == null || fx.ReduceMotion)
            {
                return;
            }

            Color fruit = _washTint > 0 ? Palette.Tint(_washTint) : Palette.Mud;
            Vector2 surface = _view.FootPosition + new Vector2(0f, (CuiaGeometry.CavityBottomU + k + 1f) * _u);
            fx.Splash(surface, Color.Lerp(fruit, Palette.Mud, 0.6f), _u);
        }

        // Pide el sprite de la nube (se genera una vez por sesión y agua base) la primera vez que esta cuia tiene Barro.
        private void EnsureCloud()
        {
            if (_cloud.sprite == null)
            {
                _cloud.sprite = _art.BarroCloud;
            }
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
                bool washing = _washing && k == _washLayer && _lumpAlpha > 0.01f;
                OverlayParts.SetVisible(_lumps[k], !_inFlight && (((_mask >> k) & 1) != 0 || washing));
            }

            OverlayParts.SetVisible(_cloud, !_inFlight && _washing && _turbidity > 0.01f);
        }

        // Registra en su Canvas, oculta, una pieza que solo se ve durante el lavado: así mostrarla a mitad del paso no hace crecer
        // el registro de UGUI (sin asignaciones; ver GraphicPool). Sin efecto fuera de una jerarquía activa.
        private static void PreRegister(Component piece)
        {
            GameObject go = piece.gameObject;
            Transform? parent = go.transform.parent;
            if (go.activeSelf || parent == null || !parent.gameObject.activeInHierarchy)
            {
                return;
            }

            go.SetActive(true);
            go.SetActive(false);
        }

        private static int LowestBit(int mask)
        {
            for (int i = 0; i < 31; i++)
            {
                if (((mask >> i) & 1) != 0)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
