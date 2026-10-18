#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// «Norte arriba» (tarea M4.4; D-013, punto 5, Q15-A: «en la vista Cuenca, el botón «Norte arriba» enseña la forma con el
    /// norte arriba, reducida a ≈ 0,76»; 06 §4.5; propuesta del Mapa §3.1), parcial de <see cref="RiverMapScreen"/> sobre sus
    /// ganchos <c>NorthUp*</c>:
    /// <list type="bullet">
    /// <item><description><b>El botón</b> <c>map_north_up</c> («Norte arriba»), de madera como «Volver a la canoa», arriba a la
    /// derecha de la zona del mapa y solo en la vista Cuenca; activo, con la cara crema, el texto en la madera oscura del «Cuenca»
    /// elegido y el relieve hundido, para que se vea que el mapa está girado y que otro toque lo devuelve. Un toque gira el contenido del mapa (tierra, vidrio, agua, canoa,
    /// casa…) con el norte arriba en <see cref="MapConfig.NorthUpRotateMs"/> ms, con la escala
    /// <see cref="MapConfig.NorthUpScale"/> (328 / 431: la cuenca con el norte arriba mide 431 dp de ancho y solo hay 328) o, en
    /// una zona más pequeña, la mayor que deja entera la cuenca girada (<see cref="NorthUpTargetScale"/>, revisión de M4.4), y
    /// centrado en la zona; otro toque lo devuelve. Salir de la Cuenca, abrir o cerrar el Mapa lo dejan sin giro, en el acto.</description></item>
    /// <item><description><b>El giro</b>, el «−90°» de la propuesta en sus coordenadas con y hacia abajo (X = y; Y = 1 − x: el
    /// norte, a la derecha en la vista de siempre, queda arriba y el oeste a la izquierda), es en Unity (y hacia arriba) un giro
    /// antihorario de <see cref="NorthUpAngleDeg"/>° de un contenedor entre la zona visible y el contenido
    /// (<see cref="NorthUpPivot"/>), con su pivote en el centro de la zona: el contenido no cambia, así que el arrastre, el
    /// encuadre y los marcadores siguen con sus cuentas de siempre. Es barato: una rotación del contenedor, sin rehacer ninguna
    /// malla.</description></item>
    /// <item><description><b>Los rótulos no giran ni encogen:</b> los nombres de los países y las placas de los mundos futuros
    /// giran al revés lo mismo que el contenido y se escalan al revés de él (se leen derechos y a su tamaño, ≥ 12 sp, durante
    /// todo el giro); como derechos ocupan otra caja, con el norte arriba se recolocan para no pisarse
    /// (<see cref="NorthUpLabelGapDp"/>) ni pisar la canoa con su mástil y sus banderas, el guía o la fila corta de boyas: cada
    /// placa, lo más cerca de su punto, a un lado u otro, encima o debajo, a ≤ <see cref="NorthUpPlateMaxDistDp"/> de él en
    /// pantalla (si no hay sitio para todas, hasta <see cref="NorthUpPlateFarDistDp"/>), con un punto crema en su sitio y una
    /// línea guía si se apartó más de <see cref="NorthUpPinLineFromDp"/>, sin que esa línea cruce ni roce otra placa ni pase por
    /// otro punto (con dos puntos casi juntos, la placa del más al norte encima y la otra debajo). Solo salen las placas de los
    /// <see cref="MapConfig.NorthUpFutureWorldPlates"/> mundos futuros siguientes (decisión del dueño del 2026-10-17, opción A):
    /// las de los más lejanos, sin punto ni sitio, se funden con el giro y vuelven con la vista de siempre
    /// (<see cref="NorthUpPlateHidden"/>). Cada país, en su región (D-013, punto 5: «cada país lleva su nombre en su región»; el mapa
    /// enseña la geografía real): a ≤ <see cref="NorthUpCountryReachDp"/> de su punto de <c>labels</c> en pantalla y, si ahí no
    /// cabe, no sale mientras el norte está arriba [P]. «Andes ↑» y «Océano Atlántico ↓», fijos en el
    /// marco, tampoco giran, pero con el norte arriba sus flechas mentirían (los Andes quedan a la izquierda y el Atlántico a la
    /// derecha): se funden con el giro (un <see cref="CanvasGroup"/> en cada uno, que se crea al primer toque) y no se ven
    /// mientras el norte está arriba [P]. Siguen activos: el giro no activa ni desactiva nada.</description></item>
    /// <item><description><b>Las figuras tampoco se tumban</b> [P]: la canoa con su mástil, sus banderas y el guía, la casa de
    /// TiTino y las Rosas de las boyas saltadas giran al revés sobre su punto, y la proa mira hacia el guía en pantalla. La fila
    /// corta de boyas sigue en horizontal bajo la canoa, como en la vista de siempre, y la casa, con el pie a un lado de Guajará
    /// en pantalla (a la izquierda o a la derecha, en tierra).</description></item>
    /// <item><description>Con el norte arriba la cuenca entera cabe en la zona: no se arrastra, ni salen «Volver a la canoa» ni la
    /// ventanita de casa (la casa se ve).</description></item>
    /// <item><description><b>Reducir movimiento</b> (07 §7.8): corte, sin giro animado.</description></item>
    /// <item><description><b>Sonda:</b> <c>north_up</c> de <c>map_close</c> (<c>af_map_close</c>, 08 §8.2) = se usó en esa
    /// apertura; el botón sale en <c>AF_LAYOUT</c> como <c>map_north_up</c>.</description></item>
    /// </list>
    /// <para>Durante el giro, <see cref="Tick"/> solo cambia transformaciones y opacidades: cero asignaciones (08 §5.4). Con el
    /// norte arriba y quieto, no toca nada salvo que cambie el encuadre o el tamaño de la zona.</para>
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>
        /// Giro de «Norte arriba» en grados de Unity (eje z, y hacia arriba: antihorario): el «−90°» de la propuesta del Mapa §3.1,
        /// que va en sus coordenadas con y hacia abajo (X = y; Y = 1 − x).
        /// </summary>
        public const float NorthUpAngleDeg = 90f;

        /// <summary>Hueco entre dos rótulos del contenido con el norte arriba (dp) [P].</summary>
        public const float NorthUpLabelGapDp = 2f;

        /// <summary>Margen del botón «Norte arriba» con el borde de la zona del mapa (dp), el de los rótulos fijos.</summary>
        public const float NorthUpInsetDp = OrientationInsetDp;

        /// <summary>
        /// Con el norte arriba, una placa de mundo futuro queda, si hay sitio, como mucho a este tanto de su punto en pantalla (dp,
        /// del punto al borde más cercano de la placa) [P]; si no, hasta <see cref="NorthUpPlateFarDistDp"/> antes que pisar otra
        /// placa o el sitio de un país.
        /// </summary>
        public const float NorthUpPlateMaxDistDp = 24f;

        /// <summary>
        /// Si no hay sitio para todas las placas a ≤ <see cref="NorthUpPlateMaxDistDp"/> de su punto sin pisar a otra ni el sitio
        /// de un país (varias con el punto casi en el mismo sitio y la canoa al lado), una placa puede quedar hasta a este tanto
        /// (dp en pantalla) [P], con su línea guía.
        /// </summary>
        public const float NorthUpPlateFarDistDp = 40f;

        /// <summary>Diámetro del punto que marca, con el norte arriba, el sitio de cada placa de mundo futuro (dp en pantalla).</summary>
        public const float NorthUpPinDotDp = 6f;

        /// <summary>Borde del punto de la placa (dp en pantalla), en <see cref="NorthUpPinRim"/>.</summary>
        public const float NorthUpPinRimDp = 1.5f;

        /// <summary>Hueco de una línea guía con cualquier otra placa (dp en pantalla) [P]: ni la cruza ni la roza.</summary>
        public const float NorthUpLineClearDp = 4f;

        /// <summary>Grosor de la línea guía del punto a su placa (dp en pantalla).</summary>
        public const float NorthUpPinLineDp = 1.5f;

        /// <summary>La línea guía sale si la placa queda a más de este tanto de su punto (dp en pantalla).</summary>
        public const float NorthUpPinLineFromDp = 8f;

        /// <summary>Borde del punto y de la línea guía de las placas: verde selva oscuro #1F4A3A.</summary>
        public static readonly Color NorthUpPinRim = Palette.Hex(0x1F4A3A);

        /// <summary>
        /// Con el norte arriba, el centro del nombre de un país queda como mucho a este tanto de su punto de <c>labels</c> en
        /// pantalla (dp) [P], para que siga en su región (D-013, punto 5); si ahí no cabe, no sale mientras el norte está arriba.
        /// </summary>
        public const float NorthUpCountryReachDp = 12f;

        // Pasos de la búsqueda de sitio con el norte arriba (dp del contenido).
        private const float NorthUpPlateStepDp = 4f;
        private const float NorthUpCountryStepDp = 1.5f;

        // Peso de lo que no se puede pisar (otro rótulo, un punto, una línea guía, la canoa, el guía, la fila de boyas) frente a
        // lo que una placa prefiere no pisar (el sitio de un país): una placa solo pisa el sitio de un país si no tiene otro.
        private const int NorthUpHardHit = 64;

        // Rondas de reparación de las placas que pisan algo (NorthUpLabelShifts).
        private const int NorthUpRepairRounds = 4;

        private RectTransform? _northUpPivot;
        private ReliefParts _northUpButton;
        private TextMeshProUGUI? _northUpLabel;
        private Image? _northUpLatch;
        private Material? _northUpLabelMaterial;
        private PlatePinsGraphic? _pins;
        private Vector2[] _labelPin = Array.Empty<Vector2>();
        private DragArea? _dragArea;
        private CanvasGroup? _andesFade;
        private CanvasGroup? _atlanticFade;
        private float _figureAngle;
        private Vector2[] _labelBase = Array.Empty<Vector2>();
        private Vector2[] _labelWritten = Array.Empty<Vector2>();
        private Vector2[] _labelShift = Array.Empty<Vector2>();
        private Vector2[] _labelOrigin = Array.Empty<Vector2>();
        private int[] _labelOrder = Array.Empty<int>();
        private Rect[] _labelBox = Array.Empty<Rect>();
        private Rect[] _labelIdeal = Array.Empty<Rect>();
        private Vector2[] _labelLineEnd = Array.Empty<Vector2>();
        private bool[] _labelLine = Array.Empty<bool>();
        private bool[] _labelHidden = Array.Empty<bool>();
        private int[] _plateSide = Array.Empty<int>();
        private readonly Rect[] _northUpObstacles = new Rect[4];
        private int _northUpObstacleCount;
        private readonly Rect[] _basinControls = new Rect[4];
        private int _basinControlCount;
        private float _northUpShiftScale = float.NaN;
        private float _northUpLineScale = 1f;
        private float _rowShift;
        private bool _northUpBuilt;
        private bool _northUpOn;
        private float _northUpT;
        private float _northUpAppliedT = float.NaN;
        private Vector2 _northUpAppliedOffset;
        private Vector2 _northUpAppliedContent;
        private Vector2 _northUpAppliedView;

        // ============================================================================================ API (tests y sondas)

        /// <summary>«Norte arriba».</summary>
        public Button NorthUpButton => _northUpButton.Button;

        /// <summary>El botón «Norte arriba» se ve (solo en la vista Cuenca).</summary>
        public bool NorthUpShown => _northUpBuilt && _northUpButton.Button.gameObject.activeSelf;

        /// <summary>Texto del botón.</summary>
        public string NorthUpLabel => _northUpLabel != null ? _northUpLabel.text : string.Empty;

        /// <summary>Texto del botón (tests: tamaño y caja).</summary>
        internal TextMeshProUGUI? NorthUpLabelText => _northUpLabel;

        /// <summary>El norte está arriba o va hacia arriba (el último toque lo pidió).</summary>
        public bool NorthUpOn => _northUpOn;

        /// <summary>Avance del giro, de 0 (la vista de siempre) a 1 (norte arriba), sin suavizar.</summary>
        public float NorthUpProgress => _northUpT;

        /// <summary>El giro está en marcha (pinta a 60 fps).</summary>
        public bool NorthUpTurning => _northUpT != (_northUpOn ? 1f : 0f);

        /// <summary>Giro actual del contenido (grados de Unity, antihorario): de 0 a <see cref="NorthUpAngleDeg"/>.</summary>
        public float NorthUpAngle => NorthUpAngleDeg * NorthUpEase(_northUpT);

        /// <summary>Escala actual del contenido: de 1 a <see cref="NorthUpTargetScale"/>.</summary>
        public float NorthUpCurrentScale => Mathf.Lerp(1f, NorthUpTargetScale(), NorthUpEase(_northUpT));

        /// <summary>
        /// Escala del contenido con el norte arriba: <see cref="MapConfig.NorthUpScale"/> (≈ 0,76; D-013, punto 5) o, si la cuenca
        /// girada no cabría entera en la zona del mapa (una zona más estrecha o más baja), la mayor que la deja entera (revisión de
        /// M4.4: con el norte arriba no se arrastra).
        /// </summary>
        public float NorthUpTargetScale()
        {
            float scale = MapConfig.NorthUpScale;
            if (_contentH > 0f)
            {
                scale = Mathf.Min(scale, _viewW / _contentH); // girada, el alto del contenido va a lo ancho
            }

            if (_contentW > 0f)
            {
                scale = Mathf.Min(scale, _viewH / _contentW);
            }

            return Mathf.Max(0.01f, scale);
        }

        /// <summary>Se puede arrastrar el mapa (no con el norte arriba).</summary>
        public bool DragEnabled => _dragArea == null || _dragArea.enabled;

        /// <summary>Opacidad de «Andes ↑» y «Océano Atlántico ↓»: 1 en la vista de siempre, 0 con el norte arriba.</summary>
        internal float OrientationLabelsAlpha => _andesFade != null ? _andesFade.alpha : 1f;

        /// <summary>Contenedor que gira entre la zona visible y el contenido (pivote en el centro de la zona).</summary>
        internal RectTransform? NorthUpPivot => _northUpPivot;

        /// <summary>
        /// El botón muestra que está activo: cara crema, texto en la madera oscura del «Cuenca» elegido y relieve hundido (con el
        /// norte arriba); apagado, la madera de siempre.
        /// </summary>
        internal bool NorthUpLatched => _northUpLatch != null && _northUpLatch.enabled && _northUpButton.Relief.Pressed;

        /// <summary>Puntos de las placas que se pintan con el norte arriba (uno por placa a la vista).</summary>
        internal int NorthUpPinCount => _pins != null ? _pins.DotCount : 0;

        /// <summary>Líneas guía de los puntos a sus placas (las que quedan a más de <see cref="NorthUpPinLineFromDp"/>).</summary>
        internal int NorthUpPinLineCount => _pins != null ? _pins.LineCount : 0;

        /// <summary>Opacidad de los puntos de las placas: 0 en la vista de siempre, 1 con el norte arriba.</summary>
        internal float NorthUpPinAlpha => _pins != null ? _pins.canvasRenderer.GetAlpha() : 0f;

        /// <summary>El punto de la silueta <paramref name="i"/> a la vista (su gran parada), en coordenadas del mundo.</summary>
        internal Vector3 SilhouettePivotWorld(int i)
        {
            Silhouette s = ActiveSilhouette(i);
            Vector2 local = _space.ToLocal(s.MapPoint);
            var layer = (RectTransform)s.Root.parent;
            Rect r = layer.rect;
            return layer.TransformPoint(new Vector3(r.xMin + local.x, r.yMax + local.y, 0f));
        }

        /// <summary>
        /// Rótulos de país que se ven (tests: que se leen derechos), sin los que no caben en su región con el norte arriba
        /// (<see cref="NorthUpCountryHidden"/>).
        /// </summary>
        internal IEnumerable<TextMeshProUGUI> CountryLabels()
        {
            for (int i = 0; i < _countries.Count; i++)
            {
                if (_countries[i].gameObject.activeSelf && !NorthUpCountryHidden(i))
                {
                    yield return _countries[i];
                }
            }
        }

        /// <summary>
        /// El país <paramref name="i"/> (en el orden de <c>labels</c>) no sale con el norte arriba: no cabe a ≤
        /// <see cref="NorthUpCountryReachDp"/> de su punto sin pisar otro rótulo, una línea guía o una figura.
        /// </summary>
        internal bool NorthUpCountryHidden(int i)
        {
            return i < _countries.Count && NorthUpLabelHidden(i);
        }

        /// <summary>
        /// La línea guía de la placa <paramref name="k"/> de las que tienen punto (en el orden en que se pintan), en coordenadas
        /// locales del contenido (dp, y hacia arriba): de su punto (<paramref name="from"/>) al borde más cercano de la placa
        /// (<paramref name="to"/>). False si esa placa no lleva línea.
        /// </summary>
        internal bool NorthUpPinLine(int k, out Vector2 from, out Vector2 to)
        {
            if (_pins == null || k >= _pins.DotCount)
            {
                from = default;
                to = default;
                return false;
            }

            return _pins.Line(k, out from, out to);
        }

        /// <summary>Capa de los rótulos del contenido (países, placas y sus puntos).</summary>
        internal RectTransform? LabelLayer => _labelLayer;

        /// <summary>
        /// Placas de los mundos futuros que se ven (tests: que se leen derechas), sin las que no salen con el norte arriba
        /// (<see cref="NorthUpPlateHidden"/>).
        /// </summary>
        internal IEnumerable<RectTransform> SilhouettePlates()
        {
            int countries = _countries.Count;
            for (int i = 0; i < _silhouettes.Count; i++)
            {
                if (_silhouettes[i].Root.gameObject.activeSelf && !NorthUpLabelHidden(countries + i))
                {
                    yield return _silhouettes[i].Root;
                }
            }
        }

        /// <summary>
        /// La placa de la silueta <paramref name="i"/> a la vista (en el orden de <see cref="SilhouetteCount"/>) no sale con el
        /// norte arriba: su mundo no es uno de los <see cref="MapConfig.NorthUpFutureWorldPlates"/> mundos futuros siguientes
        /// (decisión del dueño del 2026-10-17, opción A). Se funde con el giro y vuelve con la vista de siempre.
        /// </summary>
        internal bool NorthUpPlateHidden(int i)
        {
            int seen = 0;
            for (int k = 0; k < _silhouettes.Count; k++)
            {
                if (_silhouettes[k].Root.gameObject.activeSelf && seen++ == i)
                {
                    return NorthUpLabelHidden(_countries.Count + k);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(i), i, "No hay tantas siluetas a la vista.");
        }

        /// <summary>
        /// Placas de mundos futuros que salen: con el norte arriba (o mientras gira), las de los
        /// <see cref="MapConfig.NorthUpFutureWorldPlates"/> mundos futuros siguientes, que son las primeras de
        /// <see cref="SilhouetteCount"/> (van en orden de mundo); en la vista de siempre, todas.
        /// </summary>
        internal int NorthUpPlateCount
        {
            get
            {
                int n = 0;
                int countries = _countries.Count;
                for (int i = 0; i < _silhouettes.Count; i++)
                {
                    if (_silhouettes[i].Root.gameObject.activeSelf && !NorthUpLabelHidden(countries + i))
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        // El rótulo i (países y luego placas, como _labelHidden) no sale mientras el norte está arriba o gira.
        private bool NorthUpLabelHidden(int i)
        {
            return _northUpT > 0f && i < _labelHidden.Length && _labelHidden[i];
        }

        /// <summary>
        /// El toque de «Norte arriba» (también para <c>adb</c> y los tests): en la vista Cuenca, gira el contenido con el norte
        /// arriba o lo devuelve. Como los demás controles del Mapa, completa «ponerse al día» si está en marcha.
        /// </summary>
        public void ToggleNorthUp()
        {
            if (!Visible || _view != MapView.Basin || !_northUpBuilt)
            {
                return;
            }

            CatchUpTouch(); // un toque en un control del Mapa completa «ponerse al día» (M3.5)
            EnsureOrientationFades();
            _northUpOn = !_northUpOn;
            if (_northUpOn)
            {
                _northUp = true; // map_close.north_up: se usó en esta apertura
            }

            if (ProgressReduceMotion)
            {
                _northUpT = _northUpOn ? 1f : 0f; // Reducir movimiento: corte
            }

            SetDragEnabled(!_northUpOn);
            ApplyNorthUpLatch();
            _activeMs = ActiveLingerMs;
            if (_northUpOn)
            {
                NorthUpLabelShifts();
            }

            ApplyNorthUp(true);
            if (_northUpOn)
            {
                HideBackToCanoeForNorthUp();
            }
            else
            {
                SetOffset(_offset); // «Volver a la canoa» vuelve a decidirse con el encuadre de siempre
            }

            UpdateHomeWindow(); // con el norte arriba la casa se ve: sin ventanita
            EmitLayout();
        }

        // ============================================================================================ ganchos

        partial void NorthUpBuild(RectTransform frame)
        {
            // El contenedor que gira: estirado a la zona visible, con el pivote en su centro y el contenido dentro (sin giro, el
            // contenido queda donde estaba: arriba a la izquierda de la zona).
            RectTransform pivot = UiBuilder.Rect("NorthUpPivot", _viewport);
            pivot.SetSiblingIndex(_content.GetSiblingIndex());
            _content.SetParent(pivot, false);
            _northUpPivot = pivot;
            _dragArea = _viewport.GetComponent<DragArea>();

            _northUpButton = UiBuilder.Relief("NorthUpButton", frame, _art.CtaWood, ToggleNorthUp);
            _northUpLabel = UiBuilder.Label("Label", _northUpButton.Face, TextStyle.PillWood, UiText.MapNorthUp, RowSp, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(_northUpLabel.rectTransform, _art.CtaWood);
            UiBuilder.AutoSize(_northUpLabel, UiBuilder.MinTextSp, RowSp);
            _northUpButton.Relief.Bind(_northUpButton.Face, _northUpButton.Base, _northUpButton.FaceImage, _art.CtaWood, null, null, _northUpLabel);
            _northUpLabelMaterial = _northUpLabel.fontSharedMaterial;

            // Activo (con el norte arriba): la cara en crema, como el «Cuenca» elegido del conmutador, encima de la madera y debajo
            // del texto; apagada, no se pinta.
            // La cara visible es la franja de arriba (FaceHeightDp) del relieve; esquinas de unos 14 dp, como las de la madera.
            _northUpLatch = UiBuilder.Sliced("Latch", _northUpButton.Face, _art.RoundedRect, SegmentSelectedPlate);
            _northUpLatch.pixelsPerUnitMultiplier = 1.5f;
            RectTransform latch = _northUpLatch.rectTransform;
            latch.anchorMin = new Vector2(0f, 1f);
            latch.anchorMax = new Vector2(1f, 1f);
            latch.pivot = new Vector2(0.5f, 1f);
            latch.sizeDelta = new Vector2(-4f, _art.CtaWood.FaceHeightDp - 4f);
            latch.anchoredPosition = new Vector2(0f, -2f);
            _northUpLatch.raycastTarget = false;
            _northUpLatch.rectTransform.SetSiblingIndex(_northUpButton.FaceImage.rectTransform.GetSiblingIndex() + 1);
            _northUpLatch.enabled = false;
            _northUpButton.Button.gameObject.SetActive(false);
            _northUpBuilt = true;
        }

        partial void NorthUpOpen()
        {
            if (!_northUpBuilt)
            {
                return;
            }

            _northUpLabel!.text = UiText.MapNorthUp; // por si cambió el idioma desde la última apertura
            ResetNorthUp();
        }

        partial void NorthUpView(MapView view)
        {
            if (!_northUpBuilt)
            {
                return;
            }

            bool basin = view == MapView.Basin;
            if (_northUpButton.Button.gameObject.activeSelf != basin)
            {
                _northUpButton.Button.gameObject.SetActive(basin);
            }

            if (!basin && (_northUpOn || _northUpT > 0f))
            {
                ResetNorthUp(); // fuera de la Cuenca, sin giro
            }

            _northUpAppliedT = float.NaN; // la vista se rehace: se vuelve a aplicar con su encuadre
        }

        partial void NorthUpSpace(MapSpace space)
        {
            PlaceNorthUpButton(); // los rótulos fijos ya están colocados (M3.4): el botón no pisa «Andes ↑»
            if (_northUpOn)
            {
                NorthUpLabelShifts(); // M3.4 acaba de colocar los rótulos a la escala nueva
            }

            _northUpAppliedT = float.NaN;
        }

        partial void NorthUpTick(float dtMs, ref bool animating)
        {
            if (!_northUpBuilt)
            {
                return;
            }

            float target = _northUpOn ? 1f : 0f;
            if (_northUpT != target)
            {
                _northUpT = Mathf.MoveTowards(_northUpT, target, dtMs / Mathf.Max(1f, MapConfig.NorthUpRotateMs));
                animating = true;
            }

            ApplyNorthUp(false);
            if (_northUpOn && !_northUpButton.Relief.Pressed)
            {
                _northUpButton.Relief.Press(true); // activo, hundido (un dedo que se fue del botón lo habría soltado)
            }

            if (_northUpOn && _backToCanoe.Button.gameObject.activeSelf)
            {
                HideBackToCanoeForNorthUp(); // un cambio de métricas recolocó la vista con el norte arriba
                EmitLayout();
            }
        }

        partial void NorthUpLayout()
        {
            PlaceNorthUpButton();
            _northUpAppliedT = float.NaN;
        }

        partial void NorthUpLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (NorthUpShown)
            {
                ids.Add("map_north_up");
                rects.Add((RectTransform)_northUpButton.Button.transform);
            }
        }

        partial void NorthUpClose()
        {
            EndRosaJump(); // el salto de Rosa en Tarapoto no sigue con el Mapa cerrado (M4.4, RiverMapScreen.Markers.cs)
            if (_northUpBuilt)
            {
                ResetNorthUp();
            }
        }

        // ============================================================================================ giro

        // Sin giro, en el acto: el contenido, los rótulos y el arrastre como siempre.
        private void ResetNorthUp()
        {
            _northUpOn = false;
            _northUpT = 0f;
            SetDragEnabled(true);
            ApplyNorthUpLatch();
            ApplyNorthUp(true);
        }

        // El estado del botón (revisión de M4.4): activo, la cara crema con el texto en la madera oscura del «Cuenca» elegido
        // (SegmentSelectedText, sin el contorno de la madera) y el relieve hundido; apagado, lo inverso. Sin asignaciones.
        private void ApplyNorthUpLatch()
        {
            if (!_northUpBuilt || _northUpLatch == null)
            {
                return;
            }

            bool on = _northUpOn;
            if (_northUpLatch.enabled != on)
            {
                _northUpLatch.enabled = on;
            }

            TextMeshProUGUI label = _northUpLabel!;
            Color text = on ? SegmentSelectedText : Palette.Cream;
            if (label.color != text)
            {
                label.color = text;
            }

            Material? plain = UiFonts.Font(TextStyle.Disabled) == label.font ? UiFonts.Material(TextStyle.Disabled) : null;
            Material? material = on && plain != null ? plain : _northUpLabelMaterial;
            if (material != null && label.fontSharedMaterial != material)
            {
                label.fontSharedMaterial = material;
            }

            _northUpButton.Relief.Press(on);
        }

        // Aplica el giro de _northUpT (suavizado): el contenedor gira NorthUpAngleDeg × u y escala de 1 a NorthUpScale, y se
        // traslada para que el centro del contenido vaya del sitio en que está (u = 0) al centro de la zona (u = 1); los rótulos
        // del contenido giran al revés y los fijos del marco se funden. Solo si cambió el giro, el encuadre o el tamaño (o con
        // force). Sin asignaciones.
        private void ApplyNorthUp(bool force)
        {
            RectTransform? pivot = _northUpPivot;
            if (pivot == null)
            {
                return;
            }

            var content = new Vector2(_contentW, _contentH);
            var view = new Vector2(_viewW, _viewH);
            if (!force && _northUpT == _northUpAppliedT && _offset == _northUpAppliedOffset && content == _northUpAppliedContent
                && view == _northUpAppliedView)
            {
                return;
            }

            _northUpAppliedT = _northUpT;
            _northUpAppliedOffset = _offset;
            _northUpAppliedContent = content;
            _northUpAppliedView = view;

            float target = NorthUpTargetScale();
            if (_northUpOn && target != _northUpShiftScale)
            {
                NorthUpLabelShifts(); // otra zona (otra escala): los rótulos se recolocan a su tamaño en pantalla
            }

            float u = NorthUpEase(_northUpT);
            float angle = NorthUpAngleDeg * u;
            float scale = Mathf.Lerp(1f, target, u);

            // Centro del contenido en coordenadas del contenedor (origen en el centro de la zona, y hacia arriba).
            var d = new Vector2((-_viewW * 0.5f) + _offset.x + (_contentW * 0.5f), (_viewH * 0.5f) - _offset.y - (_contentH * 0.5f));
            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            var turned = new Vector2((d.x * cos) - (d.y * sin), (d.x * sin) + (d.y * cos)) * scale;

            pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            pivot.localScale = new Vector3(scale, scale, 1f);
            pivot.anchoredPosition = -u * turned;

            // Los rótulos del contenido, derechos, a su tamaño en pantalla (la escala, al revés: 12 sp siguen siendo 12 sp) y, con
            // el norte arriba, sin pisarse (NorthUpLabelShifts); un país que no cabe en su región se funde con el giro.
            Quaternion upright = Quaternion.Euler(0f, 0f, -angle);
            float inv = 1f / scale;
            var counter = new Vector3(inv, inv, 1f);
            int countries = _countries.Count;
            for (int i = 0; i < countries + _silhouettes.Count; i++)
            {
                RectTransform rt = i < countries ? _countries[i].rectTransform : _silhouettes[i - countries].Root;
                rt.localRotation = upright;
                rt.localScale = counter;
                float alpha = i < _labelHidden.Length && _labelHidden[i] ? 1f - u : 1f;
                if (i < countries)
                {
                    SetLabelAlpha(_countries[i].canvasRenderer, alpha);
                }
                else
                {
                    // Una placa de un mundo futuro más lejano que los NorthUpFutureWorldPlates siguientes (decisión del dueño del
                    // 2026-10-17, opción A): se funde con el giro, su placa y su texto.
                    Silhouette s = _silhouettes[i - countries];
                    SetLabelAlpha(s.Plate.canvasRenderer, alpha);
                    SetLabelAlpha(s.Label.canvasRenderer, alpha);
                }

                if (i >= _labelBase.Length)
                {
                    continue; // un rótulo nuevo, aún sin sitio de partida: sin corrimiento
                }

                Vector2 now = rt.anchoredPosition;
                if (now != _labelWritten[i])
                {
                    _labelBase[i] = now; // M3.4 lo recolocó: ese es su sitio de partida
                }

                Vector2 at = _labelBase[i] + (u * _labelShift[i]);
                rt.anchoredPosition = at;
                _labelWritten[i] = at;
            }

            // El punto de cada placa (y su línea guía), con el giro: solo se ven con el norte arriba.
            if (_pins != null && _pins.canvasRenderer.GetAlpha() != u)
            {
                _pins.canvasRenderer.SetAlpha(u);
            }

            // Las figuras, derechas en pantalla: la canoa con su mástil, sus banderas y el guía, la casa de TiTino y las Rosas de
            // las boyas saltadas (RiverMapScreen.Markers.cs y M3.4), cada una sobre su punto.
            if (_figureAngle != -angle)
            {
                _figureAngle = -angle;
                PlaceCanoeGear(_space);
                PlaceHouseFigure();
                if (_rosaJumpPlaying)
                {
                    ApplyRosaJump(); // el salto de Rosa en Tarapoto, también derecho (RiverMapScreen.Markers.cs)
                }

                for (int i = 0; i < _rosa.Length; i++)
                {
                    if (_rosa[i] != null)
                    {
                        _rosa[i].rectTransform.localRotation = upright;
                    }
                }

                for (int i = 0; i < _pastRosa.Count; i++)
                {
                    _pastRosa[i].rectTransform.localRotation = upright;
                }
            }

            UprightCompactRow(u, -angle);

            // «Andes ↑» y «Océano Atlántico ↓»: se funden con el giro (con el norte arriba, sus flechas no dirían la verdad).
            FadeOrientationLabel(_andesFade, 1f - u);
            FadeOrientationLabel(_atlanticFade, 1f - u);
        }

        // Con el norte arriba, los rótulos derechos ocupan otra caja que en la vista de siempre (donde M3.4 los separa) y pueden
        // pisarse o salirse por los lados. Se calcula, para el giro completo y en pantalla, dónde queda cada uno, y se pasa a un
        // corrimiento en el contenido que el giro aplica en proporción. Todo va en unidades del contenido (dp de la vista de
        // siempre) en el marco girado; los rótulos, escalados al revés que el contenido (ApplyNorthUp), miden ahí su tamaño entre
        // la escala, y los topes en pantalla también van entre la escala (revisión de M4.4):
        // - las placas de los mundos futuros, que señalan un sitio: de los sitios a su alrededor (a un lado o al otro de su punto,
        //   encima o debajo) que no se salen del mapa a lo ancho y quedan a ≤ NorthUpPlateMaxDistDp de su punto en pantalla (o, si
        //   no hay sitio, a ≤ NorthUpPlateFarDistDp), el más cercano a su punto sin pisar otra placa, el punto o la línea guía de
        //   otra, la canoa con su mástil y sus banderas, el guía ni la fila corta de boyas (NorthUpObstacles), sin que su propia
        //   línea guía cruce o roce otra placa o pase por otro punto y, si puede, sin pisar el sitio de un país; con dos puntos a
        //   menos de 2 × NorthUpPinDotDp, la placa del más al norte va encima de él y la otra debajo del suyo; si ningún sitio
        //   queda libre, el que menos pisa;
        // - los países, que no salen de su región (D-013, punto 5): el sitio libre más cercano a su punto de labels, con el centro
        //   a ≤ NorthUpCountryReachDp de él en pantalla, sin pisar nada de lo anterior ni los otros países; si no hay ninguno, el
        //   país no sale mientras el norte está arriba (_labelHidden) [P].
        // Prepara además el punto de cada placa y su línea guía (PlatePinsGraphic). Asigna solo si crece la lista o al crear los
        // puntos (al tocar el botón, al cambiar de vista o de zona; nunca durante el giro).
        private void NorthUpLabelShifts()
        {
            int countries = _countries.Count;
            int n = countries + _silhouettes.Count;
            if (_labelBase.Length < n)
            {
                Vector2[] bases = new Vector2[n];
                Vector2[] written = new Vector2[n];
                Array.Copy(_labelBase, bases, _labelBase.Length);
                Array.Copy(_labelWritten, written, _labelWritten.Length);
                _labelBase = bases;
                _labelWritten = written;
                _labelShift = new Vector2[n];
                _labelOrigin = new Vector2[n];
                _labelOrder = new int[n];
                _labelBox = new Rect[n];
                _labelPin = new Vector2[n];
                _labelIdeal = new Rect[n];
                _labelLineEnd = new Vector2[n];
                _labelLine = new bool[n];
                _labelHidden = new bool[n];
                _plateSide = new int[n];
            }

            float scale = NorthUpTargetScale();
            _northUpShiftScale = scale;
            _northUpLineScale = scale;
            float inv = 1f / scale;
            IReadOnlyList<RiverMapLabel>? data = _map?.Labels;

            // Cajas en pantalla con el norte arriba, en el marco del contenido girado: S(v) = (−v.y, v.x). Los países, centrados en
            // su punto; las placas, a la derecha de su pivote (como en la vista de siempre). Primero las placas, y solo las de los
            // MapConfig.NorthUpFutureWorldPlates mundos futuros siguientes (decisión del dueño del 2026-10-17, opción A): las
            // siluetas a la vista son las de los mundos futuros con ruta, en orden de mundo (BuildSilhouettes), así que son las
            // primeras; las demás no salen con el norte arriba (_labelHidden), sin punto ni sitio.
            int count = 0;
            int plates = 0;
            int future = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    bool plate = i >= countries;
                    if (plate != (pass == 0))
                    {
                        continue;
                    }

                    RectTransform rt = plate ? _silhouettes[i - countries].Root : _countries[i].rectTransform;
                    Vector2 now = rt.anchoredPosition;
                    if (now != _labelWritten[i])
                    {
                        _labelBase[i] = now;
                    }

                    _labelShift[i] = Vector2.zero;
                    _labelHidden[i] = false;
                    _labelLine[i] = false;
                    _plateSide[i] = 0;
                    if (!rt.gameObject.activeSelf)
                    {
                        continue;
                    }

                    if (plate && future++ >= MapConfig.NorthUpFutureWorldPlates)
                    {
                        _labelHidden[i] = true; // un mundo futuro más lejano: su placa no sale con el norte arriba
                        continue;
                    }

                    Vector2 q = NorthUpScreen(_labelBase[i]);
                    Vector2 size = rt.sizeDelta * inv;
                    _labelOrigin[i] = new Vector2(plate ? q.x : q.x - (size.x * 0.5f), q.y - (size.y * 0.5f));
                    if (plate)
                    {
                        _labelPin[i] = NorthUpScreen(_space.ToLocal(_silhouettes[i - countries].MapPoint));
                        plates++;
                    }
                    else
                    {
                        // Su punto de labels (su región), y la caja centrada en él dentro del mapa a lo ancho: el sitio que una
                        // placa prefiere no pisar.
                        _labelPin[i] = data != null && i < data.Count ? NorthUpScreen(_space.ToLocal(data[i].X, data[i].Y)) : q;
                        float maxX = Mathf.Max(LabelEdgeDp, _contentH - LabelEdgeDp - size.x);
                        _labelIdeal[i] = new Rect(Mathf.Clamp(_labelPin[i].x - (size.x * 0.5f), LabelEdgeDp, maxX), _labelPin[i].y - (size.y * 0.5f), size.x, size.y);
                    }

                    _labelOrder[count++] = i;
                }
            }

            NorthUpObstacles(scale);

            // Dos puntos casi juntos (menos de 2 × NorthUpPinDotDp en pantalla): la placa del más al norte (arriba en pantalla),
            // encima de su punto, y la otra, debajo del suyo; así cada una queda junto al suyo y ninguna línea cruza la otra.
            float close = 2f * NorthUpPinDotDp * inv;
            for (int a = 0; a < plates; a++)
            {
                for (int b = a + 1; b < plates; b++)
                {
                    int i = _labelOrder[a];
                    int k = _labelOrder[b];
                    if ((_labelPin[i] - _labelPin[k]).sqrMagnitude < close * close)
                    {
                        bool iNorth = _labelPin[i].y >= _labelPin[k].y;
                        _plateSide[i] = iNorth ? 1 : -1;
                        _plateSide[k] = iNorth ? -1 : 1;
                    }
                }
            }

            float reach = NorthUpCountryReachDp * inv;
            float pinReach = ((NorthUpPinDotDp * 0.5f) + NorthUpPinRimDp + 1f) * inv; // nada pisa el punto de una placa
            float cap = NorthUpPlateFarDistDp * inv;

            // Las placas y los países se colocan juntos, uno tras otro (primero las placas, luego los países en el orden de labels),
            // cada uno frente a los ya colocados; si alguno pisa algo, se reparan por rondas (hasta NorthUpRepairRounds): cada uno
            // que pisa otra cosa busca el sitio que menos pisa frente a todos los demás (con varias placas con el punto casi en el
            // mismo sitio, la primera que se coloca puede quitarle a otra el único sitio que le queda; un país se aparta dentro de
            // su región y una placa, hasta NorthUpPlateFarDistDp). Si así no caben todos, se vuelve a probar solo con las placas, y
            // los países se colocan después, cada uno en el sitio libre que le quede en su región, o no salen con el norte arriba
            // (D-013, punto 5: antes que fuera de su región) [P]. Como mucho dos intentos: unos milisegundos, solo al tocar.
            int hard = 0;
            int joint = count;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                joint = attempt == 0 ? count : plates;
                hard = 0;
                for (int a = 0; a < joint; a++)
                {
                    hard += PlaceNorthUpLabel(a, a, plates, count, scale, cap, reach, pinReach) / NorthUpHardHit;
                }

                for (int round = 0; hard > 0 && round < NorthUpRepairRounds; round++)
                {
                    for (int a = 0; a < joint; a++)
                    {
                        int i = _labelOrder[a];
                        if (NorthUpLabelHits(_labelBox[i], joint, plates, i, pinReach, a < plates, _labelPin[i], count) >= NorthUpHardHit)
                        {
                            PlaceNorthUpLabel(a, joint, plates, count, scale, cap, reach, pinReach);
                        }
                    }

                    hard = 0;
                    for (int a = 0; a < joint; a++)
                    {
                        int i = _labelOrder[a];
                        hard += NorthUpLabelHits(_labelBox[i], joint, plates, i, pinReach, a < plates, _labelPin[i], count) / NorthUpHardHit;
                    }
                }

                if (hard == 0)
                {
                    break;
                }
            }

            // Los países que no entraron en la colocación conjunta, cada uno en el sitio libre que le quede en su región; el que
            // no cabe no sale con el norte arriba.
            for (int a = joint; a < count; a++)
            {
                int hits = PlaceNorthUpLabel(a, a, plates, count, scale, cap, reach, pinReach);
                _labelHidden[_labelOrder[a]] = hits >= NorthUpHardHit;
            }

            // Del corrimiento en pantalla (dx, dy) al del contenido: S(v) = (−v.y, v.x), así que (dy, −dx).
            for (int a = 0; a < count; a++)
            {
                int i = _labelOrder[a];
                Vector2 d = _labelBox[i].position - _labelOrigin[i];
                _labelShift[i] = new Vector2(d.y, -d.x);
            }

            NorthUpPins(plates, scale);
        }

        // Coloca el rótulo a de _labelOrder (una placa si a < plates; si no, un país) en el sitio que menos pisa frente a los
        // upto primeros de _labelOrder (NorthUpLabelHits) y, a igualdad, el más cercano: una placa, a ≤ cap de su punto y sin
        // taparlo, cerca de su sitio de siempre; un país, con el centro a ≤ reach de su punto de labels. Devuelve lo que pisa (sin
        // ningún sitio posible, 16 × NorthUpHardHit: entonces queda en su sitio de siempre o en su punto). Sin asignaciones.
        private int PlaceNorthUpLabel(int a, int upto, int plates, int count, float scale, float cap, float reach, float pinReach)
        {
            int countries = _countries.Count;
            int i = _labelOrder[a];
            RectTransform rt = i >= countries ? _silhouettes[i - countries].Root : _countries[i].rectTransform;
            Vector2 size = rt.sizeDelta / scale;
            Vector2 origin = _labelOrigin[i] + (size * 0.5f);
            bool plate = a < plates;
            Vector2 p = _labelPin[i];
            float x0 = plate ? p.x - cap - size.x : p.x - reach - (size.x * 0.5f);
            float x1 = plate ? p.x + cap : p.x + reach - (size.x * 0.5f);
            float y0 = plate ? p.y - cap - size.y : p.y - reach - (size.y * 0.5f);
            float y1 = plate ? p.y + cap : p.y + reach - (size.y * 0.5f);
            float step = plate ? NorthUpPlateStepDp : NorthUpCountryStepDp;
            int nx = Mathf.CeilToInt((x1 - x0) / step);
            int ny = Mathf.CeilToInt((y1 - y0) / step);
            float maxX = Mathf.Max(LabelEdgeDp, _contentH - LabelEdgeDp - size.x); // a lo ancho en pantalla: el alto del contenido
            float maxY = Mathf.Max(LabelEdgeDp, _contentW - LabelEdgeDp - size.y);

            // Sin ningún sitio que cumpla: el de siempre (una placa) o su punto (un país, que entonces no sale), dentro del mapa a
            // lo ancho.
            Vector2 from = plate ? origin : p;
            Rect best = new Rect(Mathf.Clamp(from.x - (size.x * 0.5f), LabelEdgeDp, maxX), from.y - (size.y * 0.5f), size.x, size.y);
            int bestHits = int.MaxValue;
            float bestCost = float.PositiveInfinity;
            for (int iy = 0; iy <= ny; iy++)
            {
                float y = y0 + (iy * step);
                if (y < LabelEdgeDp || y > maxY)
                {
                    continue;
                }

                for (int ix = 0; ix <= nx; ix++)
                {
                    float x = x0 + (ix * step);
                    if (x < LabelEdgeDp || x > maxX)
                    {
                        continue;
                    }

                    var box = new Rect(x, y, size.x, size.y);
                    float c;
                    if (plate)
                    {
                        float dist = DistanceToBox(box, p);
                        if (dist > cap || dist < pinReach)
                        {
                            continue; // lejos de su punto, o encima de él
                        }

                        // Junto a su punto (y, a igualdad, cerca de su sitio de siempre); más allá de NorthUpPlateMaxDistDp, solo si
                        // pisa menos.
                        float near = NorthUpPlateMaxDistDp / scale;
                        c = (dist * dist) + (0.01f * (box.center - origin).sqrMagnitude) + (dist > near ? 1e6f : 0f);
                    }
                    else
                    {
                        c = (box.center - p).sqrMagnitude;
                        if (c > reach * reach)
                        {
                            continue; // fuera de su región
                        }
                    }

                    if (bestHits == 0 && c >= bestCost)
                    {
                        continue;
                    }

                    int hits = NorthUpLabelHits(box, upto, plates, i, pinReach, plate, p, count);
                    if (hits < bestHits || (hits == bestHits && c < bestCost))
                    {
                        bestHits = hits;
                        bestCost = c;
                        best = box;
                    }
                }
            }

            _labelBox[i] = best;
            if (plate)
            {
                Vector2 near = new Vector2(Mathf.Clamp(p.x, best.xMin, best.xMax), Mathf.Clamp(p.y, best.yMin, best.yMax));
                _labelLineEnd[i] = near;
                _labelLine[i] = Vector2.Distance(near, p) * scale > NorthUpPinLineFromDp;
            }

            return bestHits == int.MaxValue ? NorthUpHardHit * 16 : bestHits;
        }

        // Lo que ningún rótulo pisa con el norte arriba, en pantalla (el marco girado, unidades del contenido) y con
        // MarkerObstacleGapDp de hueco en pantalla: la canoa con su mástil y sus banderas (con la proa a un lado o al otro, que
        // depende del guía), el guía y la fila corta de boyas, que con el norte arriba sigue en horizontal bajo la canoa
        // (UprightCompactRow; aquí se fija también lo que se corre a lo ancho para no salirse). Las figuras van derechas en
        // pantalla, así que su caja es la de la vista de siempre alrededor de su punto girado. Sin asignaciones.
        private void NorthUpObstacles(float scale)
        {
            _northUpObstacleCount = 0;
            float g = MarkerObstacleGapDp / scale;
            if (_markersReady && _hasCanoe && _map != null && _routes != null && _stops != null)
            {
                float facing = _canoeFacing;
                GearPose(_space, _canoeX, _canoeShown, _flagCount, ref facing, out Vector2 canoe, out Vector2 guide, out _, out _, out _,
                    out _, out float mastH);
                Rect gear = default;
                for (int s = 0; s < 2; s++)
                {
                    float f = s == 0 ? -1f : 1f;
                    var foot = canoe + new Vector2(-f * ProceduralArt.MapCanoeWidthDp * SternFraction, 2f);
                    Rect box = GearBox(canoe, foot, mastH, f);
                    gear = s == 0 ? box : Rect.MinMaxRect(Mathf.Min(gear.xMin, box.xMin), Mathf.Min(gear.yMin, box.yMin),
                        Mathf.Max(gear.xMax, box.xMax), Mathf.Max(gear.yMax, box.yMax));
                }

                _northUpObstacles[_northUpObstacleCount++] = Grow(UprightBox(gear, canoe), g);
                _northUpObstacles[_northUpObstacleCount++] = Grow(UprightBox(GuideBox(guide), guide), g);
            }

            _rowShift = 0f;
            if (_buoysCompact && _buoyCount > 0)
            {
                Vector2 stop = _space.ToLocal(_frontStopMap);
                float r = BuoyRadiusBasinDp;
                float x0 = Mathf.Min(_buoyLocal[0].x, _buoyLocal[BuoysPerLeg - 1].x) - r;
                float x1 = Mathf.Max(_buoyLocal[0].x, _buoyLocal[BuoysPerLeg - 1].x) + r;
                Rect row = UprightBox(Rect.MinMaxRect(x0, _buoyLocal[0].y - r, x1, _buoyLocal[0].y + r), stop);
                float lo = LabelEdgeDp;
                float hi = _contentH - LabelEdgeDp;
                _rowShift = row.xMin < lo ? lo - row.xMin : (row.xMax > hi ? Mathf.Max(lo - row.xMin, hi - row.xMax) : 0f);
                row.x += _rowShift;
                _northUpObstacles[_northUpObstacleCount++] = Grow(row, g);
            }

            // «Norte arriba», que sigue en el marco con el norte arriba (tarea FX7): su caja en la zona visible, llevada al marco
            // girado. Con el giro entero, un punto v del contenido queda en pantalla, desde el centro de la zona y con y hacia
            // arriba, en scale × (S(v) − S(centro del contenido)) (ApplyNorthUp); así que un punto s de la pantalla es, en el marco
            // girado, s / scale + S(centro) = s / scale + (alto, ancho) / 2 del contenido.
            if (_northUpBuilt)
            {
                Rect b = NorthUpButtonFrameRect();
                float x0 = b.xMin - FramePadDp - (_viewW * 0.5f);
                float x1 = b.xMax - FramePadDp - (_viewW * 0.5f);
                float y0 = (_viewH * 0.5f) - (b.yMax - FramePadDp);
                float y1 = (_viewH * 0.5f) - (b.yMin - FramePadDp);
                float cx = _contentH * 0.5f;
                float cy = _contentW * 0.5f;
                Rect button = Rect.MinMaxRect((x0 / scale) + cx, (y0 / scale) + cy, (x1 / scale) + cx, (y1 / scale) + cy);
                _northUpObstacles[_northUpObstacleCount++] = Grow(button, g);
            }
        }

        // La caja box de una figura derecha en pantalla con su punto en at (coordenadas del contenido), en el marco girado: la
        // misma caja alrededor del punto girado.
        private static Rect UprightBox(Rect box, Vector2 at)
        {
            Vector2 s = NorthUpScreen(at);
            return new Rect(s.x + (box.xMin - at.x), s.y + (box.yMin - at.y), box.width, box.height);
        }

        // Del contenido (dp, y hacia arriba) al marco girado con el norte arriba (sin la escala): S(v) = (−v.y, v.x).
        private static Vector2 NorthUpScreen(Vector2 v)
        {
            return new Vector2(-v.y, v.x);
        }

        // Cuánto pisa la caja del rótulo self en pantalla frente a los upto primeros de _labelOrder (los ya colocados), con
        // NorthUpLabelGapDp de hueco. Pisar algo que no se puede pisar cuenta NorthUpHardHit: otro rótulo colocado (no un país que
        // no sale), el punto de otra placa, la línea guía de otra placa colocada, la canoa, el guía o la fila corta de boyas
        // (NorthUpObstacles) y, en una placa, que su propia línea guía cruce otra placa colocada o que quede del lado que no le
        // toca de su punto (_plateSide). Una placa suma además, por cada sitio de un país aún sin colocar (_labelIdeal) que pisa,
        // más cuanto antes va ese país en labels (Brasil el primero). Sin asignaciones.
        private int NorthUpLabelHits(Rect box, int upto, int plates, int self, float pinReach, bool plate, Vector2 pin, int count)
        {
            float g = NorthUpLabelGapDp;
            Rect grown = Rect.MinMaxRect(box.xMin - g, box.yMin - g, box.xMax + g, box.yMax + g);
            float lineGap = NorthUpLineClearDp / _northUpLineScale;
            Rect lineClear = Grow(box, lineGap);
            int hard = 0;
            for (int b = 0; b < upto; b++)
            {
                int k = _labelOrder[b];
                if (k != self && !_labelHidden[k] && grown.Overlaps(_labelBox[k]))
                {
                    hard++;
                }
            }

            for (int b = 0; b < plates; b++)
            {
                int k = _labelOrder[b];
                if (k != self && DistanceToBox(box, _labelPin[k]) < pinReach)
                {
                    hard++;
                }

                if (b < upto && k != self && _labelLine[k] && SegmentHitsBox(_labelPin[k], _labelLineEnd[k], lineClear))
                {
                    hard++; // pisaría (o rozaría) la línea guía de otra placa
                }
            }

            for (int o = 0; o < _northUpObstacleCount; o++)
            {
                if (grown.Overlaps(_northUpObstacles[o]))
                {
                    hard++;
                }
            }

            if (!plate)
            {
                return hard * NorthUpHardHit;
            }

            int side = _plateSide[self];
            if ((side > 0 && box.yMin < pin.y) || (side < 0 && box.yMax > pin.y))
            {
                hard++; // con otro punto casi junto: la del norte encima y la otra debajo
            }

            var near = new Vector2(Mathf.Clamp(pin.x, box.xMin, box.xMax), Mathf.Clamp(pin.y, box.yMin, box.yMax));
            if (Vector2.Distance(near, pin) * _northUpLineScale > NorthUpPinLineFromDp)
            {
                // Su línea guía no cruza ni roza otra placa colocada ni pasa por el punto de otra (salvo el que está casi junto al
                // suyo, que comparte sitio): así se sabe de qué punto sale cada placa.
                float close = 2f * NorthUpPinDotDp / _northUpLineScale;
                for (int b = 0; b < plates; b++)
                {
                    int k = _labelOrder[b];
                    if (k == self)
                    {
                        continue;
                    }

                    if (b < upto && SegmentHitsBox(pin, near, Grow(_labelBox[k], lineGap)))
                    {
                        hard++;
                    }

                    if ((_labelPin[k] - pin).sqrMagnitude >= close * close && PointSegmentDistance(_labelPin[k], pin, near) < pinReach)
                    {
                        hard++;
                    }
                }
            }

            int soft = 0;
            for (int b = Mathf.Max(upto, plates); b < count; b++)
            {
                if (grown.Overlaps(_labelIdeal[_labelOrder[b]]))
                {
                    soft += count - b; // el sitio de un país aún sin colocar: los primeros de labels (Brasil, Perú…) pesan más
                }
            }

            return (hard * NorthUpHardHit) + Mathf.Min(soft, NorthUpHardHit - 1);
        }

        // Con el norte arriba, la fila corta de boyas (un tramo sin largo a esta escala: el «me faltan 3», M3.4) sigue en
        // horizontal bajo la canoa en pantalla, como en la vista de siempre: sus dos mallas (y las Rosas de sus boyas saltadas)
        // giran al revés que el contenido alrededor del punto de la parada, corridas _rowShift a lo ancho si se saldrían
        // (NorthUpObstacles). Sin la fila corta, o sin giro, como siempre. Sin asignaciones.
        private void UprightCompactRow(float u, float figureAngle)
        {
            bool turn = figureAngle != 0f && _buoysCompact && _buoyCount > 0;
            Vector2 stop = turn ? _space.ToLocal(_frontStopMap) : Vector2.zero;
            var d = new Vector2(u * _rowShift, 0f);
            Vector2 shift = turn ? stop + Turn(d - stop, figureAngle) : Vector2.zero;
            Quaternion rotation = Quaternion.Euler(0f, 0f, turn ? figureAngle : 0f);
            PlaceRowMesh(_buoys, rotation, shift);
            PlaceRowMesh(_buoysAhead, rotation, shift);
            for (int i = 0; i < _rosa.Length; i++)
            {
                Image rosa = _rosa[i];
                if (rosa != null && rosa.gameObject.activeSelf)
                {
                    rosa.rectTransform.anchoredPosition = turn ? stop + Turn(_buoyLocal[i] - stop + d, figureAngle) : _buoyLocal[i];
                }
            }
        }

        private static void PlaceRowMesh(Graphic? mesh, Quaternion rotation, Vector2 shift)
        {
            if (mesh == null)
            {
                return;
            }

            RectTransform rt = mesh.rectTransform;
            rt.localRotation = rotation;
            if (rt.anchoredPosition != shift)
            {
                rt.anchoredPosition = shift;
            }
        }

        // Distancia de p a la caja (0 si está dentro).
        private static float DistanceToBox(Rect box, Vector2 p)
        {
            float dx = Mathf.Max(0f, Mathf.Max(box.xMin - p.x, p.x - box.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(box.yMin - p.y, p.y - box.yMax));
            return Mathf.Sqrt((dx * dx) + (dy * dy));
        }

        // El punto de cada placa (las plates primeras de _labelOrder) y, si la placa queda a más de NorthUpPinLineFromDp en
        // pantalla, una línea guía del punto al borde más cercano de la placa; en el contenido (de pantalla al contenido,
        // (s.y, −s.x)), con los tamaños de pantalla divididos por la escala. Debajo de los rótulos; se ve con el giro
        // (ApplyNorthUp).
        private void NorthUpPins(int plates, float scale)
        {
            if (_pins == null)
            {
                if (_labelLayer == null)
                {
                    return;
                }

                var go = new GameObject("NorthUpPins", typeof(RectTransform), typeof(CanvasRenderer), typeof(PlatePinsGraphic));
                go.transform.SetParent(_labelLayer, false);
                go.transform.SetAsFirstSibling();
                FillTopLeft((RectTransform)go.transform);
                _pins = go.GetComponent<PlatePinsGraphic>();
                _pins.raycastTarget = false;
                _pins.canvasRenderer.SetAlpha(0f);
            }

            _pins.Reserve(Mathf.Max(1, plates));
            _pins.Clear(NorthUpPinDotDp * 0.5f / scale, NorthUpPinRimDp / scale, NorthUpPinLineDp * 0.5f / scale);
            for (int a = 0; a < plates; a++)
            {
                int i = _labelOrder[a];
                Vector2 p = _labelPin[i];
                Rect box = _labelBox[i];
                var near = new Vector2(Mathf.Clamp(p.x, box.xMin, box.xMax), Mathf.Clamp(p.y, box.yMin, box.yMax));
                bool line = Vector2.Distance(near, p) * scale > NorthUpPinLineFromDp;
                _pins.Add(new Vector2(p.y, -p.x), new Vector2(near.y, -near.x), line);
            }

            _pins.Commit();
        }

        // Opacidad de un rótulo del contenido (un país o una placa con su texto), solo si cambia. Sin asignaciones.
        private static void SetLabelAlpha(CanvasRenderer cr, float alpha)
        {
            if (cr.GetAlpha() != alpha)
            {
                cr.SetAlpha(alpha);
            }
        }

        private static void FadeOrientationLabel(CanvasGroup? fade, float alpha)
        {
            if (fade != null && fade.alpha != alpha)
            {
                fade.alpha = alpha;
            }
        }

        // Un CanvasGroup en «Andes ↑» y otro en «Océano Atlántico ↓» (los crea M3.4 al abrir el Mapa por primera vez), para
        // fundirlos con el giro (también su glifo de otra fuente, si lo hubiera). Se crean al primer toque, nunca durante el giro.
        private void EnsureOrientationFades()
        {
            if (_andesFade == null && AndesLabel != null)
            {
                _andesFade = AndesLabel.gameObject.AddComponent<CanvasGroup>();
                _andesFade.interactable = false;
                _andesFade.blocksRaycasts = false;
            }

            if (_atlanticFade == null && AtlanticLabel != null)
            {
                _atlanticFade = AtlanticLabel.gameObject.AddComponent<CanvasGroup>();
                _atlanticFade.interactable = false;
                _atlanticFade.blocksRaycasts = false;
            }
        }

        // Suavizado del giro (entrada y salida suaves).
        private static float NorthUpEase(float t)
        {
            float x = Mathf.Clamp01(t);
            return x * x * (3f - (2f * x));
        }

        private void SetDragEnabled(bool on)
        {
            if (_dragArea != null && _dragArea.enabled != on)
            {
                _dragArea.enabled = on;
            }

            if (!on)
            {
                _dragging = false; // un arrastre a medias no recibe su final con el arrastre apagado
            }
        }

        private void HideBackToCanoeForNorthUp()
        {
            if (_backToCanoe.Button.gameObject.activeSelf)
            {
                _backToCanoe.Button.gameObject.SetActive(false); // la canoa se ve: la cuenca entera cabe
            }
        }

        // El botón, arriba a la derecha de la zona del mapa, a la medida de su texto (como «Volver a la canoa»); si pisaría
        // «Andes ↑» (texto grande o un idioma largo), justo debajo de él.
        private void PlaceNorthUpButton()
        {
            if (!_northUpBuilt)
            {
                return;
            }

            Rect r = NorthUpButtonFrameRect();
            UiBuilder.Place((RectTransform)_northUpButton.Button.transform, r.x, r.y, r.width, r.height);
        }

        // Dónde va «Norte arriba» en el marco (dp, y hacia abajo): arriba a la derecha de la zona del mapa, a la medida de su
        // texto y, si pisaría «Andes ↑» (ya colocado), justo debajo. Lo usan su colocación y la de las placas de la Cuenca
        // (BasinControlObstacles y NorthUpObstacles), que se colocan antes que él.
        private Rect NorthUpButtonFrameRect()
        {
            float labelW = Mathf.Ceil(UiBuilder.MinTextWidth(_northUpLabel!));
            float w = Mathf.Min(_viewW - 16f, Mathf.Max(MinTouchDp * 2f, labelW + 32f));
            float x = FramePadDp + _viewW - NorthUpInsetDp - w;
            float y = FramePadDp + NorthUpInsetDp;
            TextMeshProUGUI? andes = AndesLabel;
            if (andes != null && !string.IsNullOrEmpty(andes.text))
            {
                Rect a = FixedViewRect(andes.rectTransform); // en la zona visible; en el marco, + FramePadDp
                var label = new Rect(a.x + FramePadDp, a.y + FramePadDp, a.width, a.height);
                if (label.Overlaps(new Rect(x, y, w, MinTouchDp)))
                {
                    y = label.yMax + 4f;
                }
            }

            return new Rect(x, y, w, MinTouchDp);
        }

        // Lo que ninguna placa de mundo futuro pisa en la vista Cuenca sin girar (tarea FX7, observación 5 de la prueba de la ola
        // 5: con max 58 la placa «Mundo 5 · Loreto · Nivel 141» rozaba «Norte arriba»): los controles que flotan encima, «Norte
        // arriba» y, si el encuadre de la Cuenca lo enseña (la canoa queda fuera en una zona que no la deja entera), «Volver a la
        // canoa», y la canoa con su mástil, sus banderas y el guía, como con el norte arriba (NorthUpObstacles). Cajas en coordenadas locales del
        // contenido (y hacia arriba), con el encuadre con que abre la Cuenca (centrada en el mapa, ApplyView) y con
        // MarkerObstacleGapDp de hueco: ninguna placa de mundo futuro se coloca encima (PlaceSilhouettes). Fuera de la Cuenca,
        // ninguna. Solo al cambiar de vista o de zona; sin asignaciones.
        private void BasinControlObstacles()
        {
            _basinControlCount = 0;
            RiverMap? map = _map;
            if (_view != MapView.Basin || map == null)
            {
                return;
            }

            Vector2 c = _space.ToLocal(new Vector2(map.Width * 0.5f, map.Height * 0.5f));
            var o = new Vector2(ClampAxis((_viewW * 0.5f) - c.x, _contentW, _viewW), ClampAxis((_viewH * 0.5f) + c.y, _contentH, _viewH));
            if (_northUpBuilt)
            {
                Rect b = NorthUpButtonFrameRect();
                _basinControls[_basinControlCount++] = Grow(FromView(new Rect(b.x - FramePadDp, b.y - FramePadDp, b.width, b.height), o), MarkerObstacleGapDp);
            }

            // La canoa con su mástil y sus banderas (con la proa a un lado o al otro, que depende del guía) y el guía: la caja de
            // la canoa sola (PlaceSilhouettes) dejaba que una placa quedara debajo de las banderas (capturas de FX7 con max 140 y 200).
            if (_markersReady && _hasCanoe && _progress != null && _routes != null)
            {
                int max = Mathf.Clamp(_progress.MaxLevel, 0, WorldTable.LastLevel);
                float facing = _canoeFacing;
                GearPose(_space, max, _canoeMap, FinalFlagCount(max), ref facing, out Vector2 canoe, out Vector2 guide, out _, out _, out _,
                    out _, out float mastH);
                Rect gear = default;
                for (int side = 0; side < 2; side++)
                {
                    float f = side == 0 ? -1f : 1f;
                    var foot = canoe + new Vector2(-f * ProceduralArt.MapCanoeWidthDp * SternFraction, 2f);
                    Rect box = GearBox(canoe, foot, mastH, f);
                    gear = side == 0 ? box : Rect.MinMaxRect(Mathf.Min(gear.xMin, box.xMin), Mathf.Min(gear.yMin, box.yMin),
                        Mathf.Max(gear.xMax, box.xMax), Mathf.Max(gear.yMax, box.yMax));
                }

                _basinControls[_basinControlCount++] = Grow(gear, MarkerObstacleGapDp);
                _basinControls[_basinControlCount++] = Grow(GuideBox(guide), MarkerObstacleGapDp);
            }

            if (_hasCanoe)
            {
                Vector2 local = _space.ToLocal(_canoeMap);
                var p = new Vector2(o.x + local.x, o.y - local.y);
                bool inView = p.x >= CanoeMarginDp && p.x <= _viewW - CanoeMarginDp && p.y >= CanoeMarginDp && p.y <= _viewH - CanoeMarginDp;
                var target = new Vector2(ClampAxis((_viewW * 0.5f) - local.x, _contentW, _viewW), ClampAxis((_viewH * 0.5f) + local.y, _contentH, _viewH));
                if (!inView && (Mathf.Abs(target.x - o.x) > 0.5f || Mathf.Abs(target.y - o.y) > 0.5f))
                {
                    Rect b = FixedViewRect((RectTransform)_backToCanoe.Button.transform);
                    _basinControls[_basinControlCount++] = Grow(FromView(b, o), MarkerObstacleGapDp);
                }
            }
        }

        // La caja r de la zona visible (dp, y hacia abajo) en coordenadas locales del contenido (y hacia arriba) con el contenido
        // en o: lo inverso de InView.
        private static Rect FromView(Rect r, Vector2 o)
        {
            return Rect.MinMaxRect(r.xMin - o.x, o.y - r.yMax, r.xMax - o.x, o.y - r.yMin);
        }

        // La caja (coordenadas locales del contenido) pisa un control de la Cuenca sin girar (BasinControlObstacles).
        private bool HitsBasinControl(Rect box)
        {
            for (int k = 0; k < _basinControlCount; k++)
            {
                if (box.Overlaps(_basinControls[k]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Con el norte arriba, el punto de cada placa de mundo futuro (crema con borde <see cref="NorthUpPinRim"/>) y, si la placa
        /// quedó a más de <see cref="NorthUpPinLineFromDp"/>, una línea guía hasta ella, en una sola malla sin textura en el
        /// contenido (gira con él; los puntos son círculos). Solo se reconstruye al recolocar; sin raycast.
        /// </summary>
        [RequireComponent(typeof(CanvasRenderer))]
        internal sealed class PlatePinsGraphic : MaskableGraphic
        {
            private Vector2[] _dot = Array.Empty<Vector2>();
            private Vector2[] _end = Array.Empty<Vector2>();
            private bool[] _line = Array.Empty<bool>();
            private int _count;
            private float _radius;
            private float _rim;
            private float _half;

            /// <summary>Puntos dibujados.</summary>
            public int DotCount => _count;

            /// <summary>Líneas guía dibujadas.</summary>
            public int LineCount
            {
                get
                {
                    int n = 0;
                    for (int i = 0; i < _count; i++)
                    {
                        n += _line[i] ? 1 : 0;
                    }

                    return n;
                }
            }

            /// <summary>Prepara sitio para <paramref name="capacity"/> placas (asigna solo si crece).</summary>
            public void Reserve(int capacity)
            {
                if (_dot.Length < capacity)
                {
                    Array.Resize(ref _dot, capacity);
                    Array.Resize(ref _end, capacity);
                    Array.Resize(ref _line, capacity);
                }
            }

            /// <summary>Vacía los puntos y fija sus medidas (en unidades del contenido).</summary>
            public void Clear(float radius, float rim, float halfLine)
            {
                _count = 0;
                _radius = radius;
                _rim = rim;
                _half = halfLine;
            }

            /// <summary>Añade el punto <paramref name="dot"/> con, si <paramref name="line"/>, su línea hasta <paramref name="end"/>.</summary>
            public void Add(Vector2 dot, Vector2 end, bool line)
            {
                if (_count < _dot.Length)
                {
                    _dot[_count] = dot;
                    _end[_count] = end;
                    _line[_count] = line;
                    _count++;
                }
            }

            /// <summary>El punto <paramref name="i"/> y el final de su línea guía; false si no la lleva.</summary>
            public bool Line(int i, out Vector2 dot, out Vector2 end)
            {
                dot = _dot[i];
                end = _end[i];
                return _line[i];
            }

            /// <summary>Vuelve a dibujar.</summary>
            public void Commit()
            {
                SetVerticesDirty();
            }

            /// <inheritdoc />
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                for (int i = 0; i < _count; i++)
                {
                    if (_line[i])
                    {
                        Segment(vh, _dot[i], _end[i], _half + (_rim * 0.5f), NorthUpPinRim);
                    }
                }

                for (int i = 0; i < _count; i++)
                {
                    if (_line[i])
                    {
                        Segment(vh, _dot[i], _end[i], _half, Palette.Cream);
                    }

                    Disc(vh, _dot[i], _radius, NorthUpPinRim);
                    Disc(vh, _dot[i], Mathf.Max(0f, _radius - _rim), Palette.Cream);
                }
            }

            private void Segment(VertexHelper vh, Vector2 a, Vector2 b, float half, Color tint)
            {
                Vector2 dir = b - a;
                float length = dir.magnitude;
                if (length <= 0f)
                {
                    return;
                }

                dir /= length;
                Vector2 n = new Vector2(-dir.y, dir.x) * half;
                Color32 c = tint * color;
                int i = vh.currentVertCount;
                vh.AddVert(a - n, c, Vector2.zero);
                vh.AddVert(a + n, c, Vector2.zero);
                vh.AddVert(b + n, c, Vector2.zero);
                vh.AddVert(b - n, c, Vector2.zero);
                vh.AddTriangle(i, i + 1, i + 2);
                vh.AddTriangle(i, i + 2, i + 3);
            }

            private void Disc(VertexHelper vh, Vector2 center, float r, Color tint)
            {
                const int segments = 16;
                Color32 c32 = tint * color;
                int start = vh.currentVertCount;
                vh.AddVert(center, c32, Vector2.zero);
                for (int k = 0; k < segments; k++)
                {
                    float a = k * (Mathf.PI * 2f / segments);
                    vh.AddVert(center + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r), c32, Vector2.zero);
                }

                for (int k = 0; k < segments; k++)
                {
                    vh.AddTriangle(start, start + 1 + k, start + 1 + ((k + 1) % segments));
                }
            }
        }
    }
}
