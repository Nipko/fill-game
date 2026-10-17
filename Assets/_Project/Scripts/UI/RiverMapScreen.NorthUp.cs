#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
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
    /// <see cref="MapConfig.NorthUpScale"/> (328 / 431: la cuenca con el norte arriba mide 431 dp de ancho y solo hay 328) y
    /// centrado en la zona; otro toque lo devuelve. Salir de la Cuenca, abrir o cerrar el Mapa lo dejan sin giro, en el acto.</description></item>
    /// <item><description><b>El giro</b>, el «−90°» de la propuesta en sus coordenadas con y hacia abajo (X = y; Y = 1 − x: el
    /// norte, a la derecha en la vista de siempre, queda arriba y el oeste a la izquierda), es en Unity (y hacia arriba) un giro
    /// antihorario de <see cref="NorthUpAngleDeg"/>° de un contenedor entre la zona visible y el contenido
    /// (<see cref="NorthUpPivot"/>), con su pivote en el centro de la zona: el contenido no cambia, así que el arrastre, el
    /// encuadre y los marcadores siguen con sus cuentas de siempre. Es barato: una rotación del contenedor, sin rehacer ninguna
    /// malla.</description></item>
    /// <item><description><b>Los rótulos no giran:</b> los nombres de los países y las placas de los mundos futuros giran al
    /// revés lo mismo que el contenido y se leen derechos durante todo el giro; como derechos ocupan otra caja, con el norte arriba
    /// se recolocan para no pisarse (<see cref="NorthUpLabelGapDp"/>): cada placa, en el sitio más cercano al suyo a un lado u otro
    /// de su punto, encima o debajo, a ≤ <see cref="NorthUpPlateMaxDistDp"/> de él en pantalla, con un punto crema en su sitio y una
    /// línea guía si se apartó más de <see cref="NorthUpPinLineFromDp"/> (D-013, punto 5: el mapa enseña la geografía real); los
    /// países se apartan antes que una placa. «Andes ↑» y «Océano Atlántico ↓», fijos en el
    /// marco, tampoco giran, pero con el norte arriba sus flechas mentirían (los Andes quedan a la izquierda y el Atlántico a la
    /// derecha): se funden con el giro (un <see cref="CanvasGroup"/> en cada uno, que se crea al primer toque) y no se ven
    /// mientras el norte está arriba [P]. Siguen activos: el giro no activa ni desactiva nada.</description></item>
    /// <item><description><b>Las figuras tampoco se tumban</b> [P]: la canoa con su mástil, sus banderas y el guía, la casa de
    /// TiTino y las Rosas de las boyas saltadas giran al revés sobre su punto, y la proa mira hacia el guía en pantalla.</description></item>
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
        /// Con el norte arriba, una placa de mundo futuro queda como mucho a este tanto de su punto en pantalla (dp, del punto al
        /// borde más cercano de la placa) [P]: si no cabe en ningún sitio tan cerca, se apartan antes los países.
        /// </summary>
        public const float NorthUpPlateMaxDistDp = 24f;

        /// <summary>Diámetro del punto que marca, con el norte arriba, el sitio de cada placa de mundo futuro (dp en pantalla).</summary>
        public const float NorthUpPinDotDp = 6f;

        /// <summary>Borde del punto de la placa (dp en pantalla), en <see cref="NorthUpPinRim"/>.</summary>
        public const float NorthUpPinRimDp = 1.5f;

        /// <summary>Grosor de la línea guía del punto a su placa (dp en pantalla).</summary>
        public const float NorthUpPinLineDp = 1.5f;

        /// <summary>La línea guía sale si la placa queda a más de este tanto de su punto (dp en pantalla).</summary>
        public const float NorthUpPinLineFromDp = 8f;

        /// <summary>Borde del punto y de la línea guía de las placas: verde selva oscuro #1F4A3A.</summary>
        public static readonly Color NorthUpPinRim = Palette.Hex(0x1F4A3A);

        // Pasos de la búsqueda de sitio con el norte arriba (dp del contenido) y hasta dónde se aparta un país de su sitio.
        private const float NorthUpPlateStepDp = 2f;
        private const float NorthUpCountryStepDp = 3f;
        private const float NorthUpCountryReachDp = 96f;

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

        /// <summary>Escala actual del contenido: de 1 a <see cref="MapConfig.NorthUpScale"/>.</summary>
        public float NorthUpCurrentScale => Mathf.Lerp(1f, MapConfig.NorthUpScale, NorthUpEase(_northUpT));

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

        /// <summary>Rótulos de país que se ven (tests: que se leen derechos).</summary>
        internal IEnumerable<TextMeshProUGUI> CountryLabels()
        {
            for (int i = 0; i < _countries.Count; i++)
            {
                if (_countries[i].gameObject.activeSelf)
                {
                    yield return _countries[i];
                }
            }
        }

        /// <summary>Placas de los mundos futuros que se ven (tests: que se leen derechas).</summary>
        internal IEnumerable<RectTransform> SilhouettePlates()
        {
            for (int i = 0; i < _silhouettes.Count; i++)
            {
                if (_silhouettes[i].Root.gameObject.activeSelf)
                {
                    yield return _silhouettes[i].Root;
                }
            }
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

            float u = NorthUpEase(_northUpT);
            float angle = NorthUpAngleDeg * u;
            float scale = Mathf.Lerp(1f, MapConfig.NorthUpScale, u);

            // Centro del contenido en coordenadas del contenedor (origen en el centro de la zona, y hacia arriba).
            var d = new Vector2((-_viewW * 0.5f) + _offset.x + (_contentW * 0.5f), (_viewH * 0.5f) - _offset.y - (_contentH * 0.5f));
            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            var turned = new Vector2((d.x * cos) - (d.y * sin), (d.x * sin) + (d.y * cos)) * scale;

            pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            pivot.localScale = new Vector3(scale, scale, 1f);
            pivot.anchoredPosition = -u * turned;

            // Los rótulos del contenido, derechos y, con el norte arriba, sin pisarse (NorthUpLabelShifts).
            Quaternion upright = Quaternion.Euler(0f, 0f, -angle);
            int countries = _countries.Count;
            for (int i = 0; i < countries + _silhouettes.Count; i++)
            {
                RectTransform rt = i < countries ? _countries[i].rectTransform : _silhouettes[i - countries].Root;
                rt.localRotation = upright;
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

            // «Andes ↑» y «Océano Atlántico ↓»: se funden con el giro (con el norte arriba, sus flechas no dirían la verdad).
            FadeOrientationLabel(_andesFade, 1f - u);
            FadeOrientationLabel(_atlanticFade, 1f - u);
        }

        // Con el norte arriba, los rótulos derechos ocupan otra caja que en la vista de siempre (donde M3.4 los separa) y pueden
        // pisarse o salirse por los lados. Se calcula, para el giro completo y en pantalla (sin la escala, que es la misma en
        // todos), dónde queda cada uno, y se pasa a un corrimiento en el contenido que el giro aplica en proporción (revisión de
        // M4.4: antes se bajaban en cascada y una placa podía acabar en otro país):
        // - primero las placas de los mundos futuros, que señalan un sitio: de los sitios a su alrededor (a un lado o al otro de
        //   su punto, encima o debajo) que no se salen del mapa a lo ancho y quedan a menos de NorthUpPlateMaxDistDp de su punto en
        //   pantalla, el que menos la mueve de su sitio de siempre sin pisar otra placa ni el punto de otra; si ninguno queda
        //   libre, el que menos pisa;
        // - después los países, que se apartan antes que una placa: el sitio libre más cercano al suyo (hasta
        //   NorthUpCountryReachDp), sin pisar las placas, sus puntos ni los países ya colocados.
        // Prepara además el punto de cada placa y su línea guía (PlatePinsGraphic). Asigna solo si crece la lista o al crear los
        // puntos (al tocar el botón o al cambiar de vista, nunca durante el giro).
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
            }

            // Cajas en pantalla con el norte arriba, en el marco del contenido girado: S(v) = (−v.y, v.x). Los países, centrados en
            // su punto; las placas, a la derecha de su pivote (como en la vista de siempre). Primero las placas.
            int count = 0;
            int plates = 0;
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
                    if (!rt.gameObject.activeSelf)
                    {
                        continue;
                    }

                    Vector2 q = new Vector2(-_labelBase[i].y, _labelBase[i].x);
                    Vector2 size = rt.sizeDelta;
                    _labelOrigin[i] = new Vector2(plate ? q.x : q.x - (size.x * 0.5f), q.y - (size.y * 0.5f));
                    if (plate)
                    {
                        Vector2 point = _space.ToLocal(_silhouettes[i - countries].MapPoint);
                        _labelPin[i] = new Vector2(-point.y, point.x);
                        plates++;
                    }

                    _labelOrder[count++] = i;
                }
            }

            float scale = Mathf.Max(0.01f, MapConfig.NorthUpScale);
            float cap = NorthUpPlateMaxDistDp / scale;
            float pinReach = ((NorthUpPinDotDp * 0.5f) + NorthUpPinRimDp + 1f) / scale; // nada pisa el punto de una placa
            for (int a = 0; a < count; a++)
            {
                int i = _labelOrder[a];
                RectTransform rt = i >= countries ? _silhouettes[i - countries].Root : _countries[i].rectTransform;
                Vector2 size = rt.sizeDelta;
                Vector2 origin = _labelOrigin[i] + (size * 0.5f);
                bool plate = a < plates;
                Vector2 p = _labelPin[i];
                float x0 = plate ? p.x - cap - size.x : origin.x - (size.x * 0.5f) - NorthUpCountryReachDp;
                float x1 = plate ? p.x + cap : origin.x - (size.x * 0.5f) + NorthUpCountryReachDp;
                float y0 = plate ? p.y - cap - size.y : origin.y - (size.y * 0.5f) - NorthUpCountryReachDp;
                float y1 = plate ? p.y + cap : origin.y - (size.y * 0.5f) + NorthUpCountryReachDp;
                float step = plate ? NorthUpPlateStepDp : NorthUpCountryStepDp;
                int nx = Mathf.CeilToInt((x1 - x0) / step);
                int ny = Mathf.CeilToInt((y1 - y0) / step);
                float maxX = Mathf.Max(LabelEdgeDp, _contentH - LabelEdgeDp - size.x); // a lo ancho en pantalla: el alto del contenido
                float maxY = Mathf.Max(LabelEdgeDp, _contentW - LabelEdgeDp - size.y);

                // Sin ningún sitio que cumpla: el de siempre, dentro del mapa a lo ancho.
                Rect best = new Rect(Mathf.Clamp(origin.x - (size.x * 0.5f), LabelEdgeDp, maxX), origin.y - (size.y * 0.5f), size.x, size.y);
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
                        if (plate)
                        {
                            float dist = DistanceToBox(box, p);
                            if (dist > cap || dist < pinReach)
                            {
                                continue; // lejos de su punto, o encima de él
                            }
                        }

                        float cost = (box.center - origin).sqrMagnitude;
                        if (bestHits == 0 && cost >= bestCost)
                        {
                            continue;
                        }

                        int hits = NorthUpLabelHits(box, a, plates, i, pinReach);
                        if (hits < bestHits || (hits == bestHits && cost < bestCost))
                        {
                            bestHits = hits;
                            bestCost = cost;
                            best = box;
                        }
                    }
                }

                _labelBox[i] = best;
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

        // Cuántas cosas pisa la caja del rótulo a (en pantalla), con NorthUpLabelGapDp de hueco: los rótulos ya colocados (las
        // placas antes que los países) y los puntos de todas las placas menos el suyo. Sin asignaciones.
        private int NorthUpLabelHits(Rect box, int a, int plates, int self, float pinReach)
        {
            float g = NorthUpLabelGapDp;
            Rect grown = Rect.MinMaxRect(box.xMin - g, box.yMin - g, box.xMax + g, box.yMax + g);
            int hits = 0;
            for (int b = 0; b < a; b++)
            {
                if (grown.Overlaps(_labelBox[_labelOrder[b]]))
                {
                    hits++;
                }
            }

            for (int b = 0; b < plates; b++)
            {
                int k = _labelOrder[b];
                if (k != self && DistanceToBox(box, _labelPin[k]) < pinReach)
                {
                    hits++;
                }
            }

            return hits;
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

            UiBuilder.Place((RectTransform)_northUpButton.Button.transform, x, y, w, MinTouchDp);
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
