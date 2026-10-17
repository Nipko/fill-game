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
    /// derecha de la zona del mapa y solo en la vista Cuenca. Un toque gira el contenido del mapa (tierra, vidrio, agua, canoa,
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
    /// se separan de arriba abajo para no pisarse (<see cref="NorthUpLabelGapDp"/>). «Andes ↑» y «Océano Atlántico ↓», fijos en el
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

        private RectTransform? _northUpPivot;
        private ReliefParts _northUpButton;
        private TextMeshProUGUI? _northUpLabel;
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
            ApplyNorthUp(true);
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

            // Las figuras, derechas en pantalla: la canoa con su mástil, sus banderas y el guía, la casa de TiTino y las Rosas de
            // las boyas saltadas (RiverMapScreen.Markers.cs y M3.4), cada una sobre su punto.
            if (_figureAngle != -angle)
            {
                _figureAngle = -angle;
                PlaceCanoeGear(_space);
                PlaceHouseFigure();
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
        // pisarse o salirse por los lados: se calcula, para el giro completo, cuánto se corre cada uno en pantalla para quedar
        // dentro del mapa a lo ancho (LabelEdgeDp) y para no pisar a los de encima (de arriba abajo, bajando, con
        // NorthUpLabelGapDp), y se pasa a un corrimiento en el contenido. El giro lo aplica en proporción. Asigna solo si crece la lista (al tocar el botón o al cambiar de vista,
        // nunca durante el giro).
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
            }

            // Cajas en pantalla con el norte arriba (sin escala: es la misma en todas), en el marco del contenido girado:
            // S(v) = (−v.y, v.x). Los países, centrados en su punto; las placas, a la derecha de su pivote.
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                RectTransform rt = i < countries ? _countries[i].rectTransform : _silhouettes[i - countries].Root;
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
                float x = i < countries ? q.x - (size.x * 0.5f) : q.x;
                _labelOrigin[i] = new Vector2(x, q.y - (size.y * 0.5f));

                // Dentro del mapa a lo ancho (en pantalla, el alto del contenido): con el norte arriba, un rótulo de la orilla oeste
                // o este se saldría.
                x = Mathf.Clamp(x, LabelEdgeDp, Mathf.Max(LabelEdgeDp, _contentH - LabelEdgeDp - size.x));
                _labelBox[i] = new Rect(x, q.y - (size.y * 0.5f), size.x, size.y);

                // De arriba abajo (inserción: pocos rótulos).
                int k = count++;
                while (k > 0 && _labelBox[_labelOrder[k - 1]].yMax < _labelBox[i].yMax)
                {
                    _labelOrder[k] = _labelOrder[k - 1];
                    k--;
                }

                _labelOrder[k] = i;
            }

            for (int a = 1; a < count; a++)
            {
                int i = _labelOrder[a];
                Rect box = _labelBox[i];
                for (int guard = 0; guard < count; guard++)
                {
                    bool moved = false;
                    for (int b = 0; b < a; b++)
                    {
                        Rect other = _labelBox[_labelOrder[b]];
                        if (box.xMin < other.xMax + NorthUpLabelGapDp && box.xMax > other.xMin - NorthUpLabelGapDp
                            && box.yMin < other.yMax + NorthUpLabelGapDp && box.yMax > other.yMin - NorthUpLabelGapDp)
                        {
                            box.y = other.yMin - NorthUpLabelGapDp - box.height; // justo debajo
                            moved = true;
                        }
                    }

                    if (!moved)
                    {
                        break;
                    }
                }

                _labelBox[i] = box;
            }

            // Del corrimiento en pantalla (dx, dy) al del contenido: S(v) = (−v.y, v.x), así que (dy, −dx).
            for (int a = 0; a < count; a++)
            {
                int i = _labelOrder[a];
                Vector2 d = _labelBox[i].position - _labelOrigin[i];
                _labelShift[i] = new Vector2(d.y, -d.x);
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
    }
}
