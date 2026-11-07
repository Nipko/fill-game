#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El Mapa del río v2, bloque M2-1 «El río vivo» (D-069; <c>docs/dev/propuesta-mapa-rio-v2.md</c> §3, §7.1-§7.3 y §9.3), parcial
    /// de <see cref="RiverMapScreen"/>. Con <c>ui_map_v2</c> encendida (<see cref="LiveRequested"/>) y unos datos <c>/2</c> con
    /// vegetación, el Mapa ocupa la pantalla entre la cabecera y la barra de abajo, sin placa ni marco, y llega al borde como fondo
    /// (la columna de D-059 deja los controles dentro: la máscara se abre a los lados con <see cref="RectMask2D.padding"/>). Las capas
    /// de la propuesta §3.1, de atrás adelante:
    /// <list type="number">
    /// <item><description>(1) la tierra por zonas (<see cref="LandGraphic.SetLive"/>: dosel, sabana, roca y nieve con su borde
    /// difuminado) y la <b>várzea</b> clara junto a cada río (<see cref="RibbonPart.Varzea"/>);</description></item>
    /// <item><description>(5) la <b>selva</b> (<see cref="CanopyGraphic"/>, limitada a la vista con un margen);</description></item>
    /// <item><description>(2) las <b>orillas</b> (<see cref="RibbonPart.Banks"/>: playas, barrancos y hierba);</description></item>
    /// <item><description>(3) los <b>ríos</b> de <see cref="LiveWidthClass1Dp"/>/<see cref="LiveWidthClass2Dp"/>/<see cref="LiveWidthClass3Dp"/>
    /// dp con color por vértice y la franja del Encuentro (<see cref="RiverRibbonGraphic.SetLive"/>), sobre la forma de la 1.0
    /// (<see cref="RiverRibbonGraphic.SetShapeWidth"/>, <see cref="ShapeWidthDp"/>): las paradas quedan en el eje dibujado y la canoa
    /// y las boyas sobre el agua;</description></item>
    /// <item><description>(0 y 8) la <b>luz del mundo</b> (<see cref="WorldLight"/>), las <b>sombras de dos nubes</b> que derivan y la
    /// <b>niebla</b> del cuarto de arriba con los Andes lejanos, en un lienzo entre el estático y el dinámico (debajo de la canoa y de
    /// los marcadores);</description></item>
    /// <item><description>(4) la <b>corriente</b>: chevrons que bajan con el río y destellos que titilan, en su propio lienzo dentro del
    /// dinámico; solo mueven rectángulos guardados y cambian la opacidad del <see cref="CanvasRenderer"/> (como
    /// <see cref="HomeRiver"/>): sin asignar memoria ni rehacer mallas;</description></item>
    /// <item><description>(9) el <b>primer plano</b>: dos racimos de hojas con una heliconia en las esquinas, con parallax ×
    /// <see cref="ParallaxFactor"/> al arrastrar.</description></item>
    /// </list>
    /// Las paradas futuras pasan de la mini cuia gris a una <b>boya de vidrio</b> de <see cref="LiveBuoyDp"/> dp; la mini cuia queda
    /// en las alcanzadas y en la siguiente, con un brillo que late. <b>Reducir movimiento</b> (§7.3): la corriente, las nubes, el
    /// brillo y el parallax quedan quietos. El arte (<see cref="MapLiveArt"/>, ≈ 2 MiB) se pinta al abrir y se suelta al cerrar
    /// (§7.2: +6 MiB como mucho, solo con el Mapa abierto). Con <c>ui_map_v2</c> apagada, el Mapa de la 1.0 con los mismos datos.
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Lado de la boya de vidrio de una parada futura (dp; propuesta §3.1).</summary>
        public const float LiveBuoyDp = 14f;

        /// <summary>Lado del brillo de la parada siguiente (dp) [P].</summary>
        public const float NextGlowDp = 40f;

        /// <summary>Periodo del brillo de la parada siguiente (ms) [P].</summary>
        public const float NextGlowPeriodMs = 1600f;

        /// <summary>Un chevron cada tantos dp de río (propuesta §3.1, capa 4).</summary>
        public const float FlowStepDp = 60f;

        /// <summary>Velocidad de la corriente (dp/s; propuesta §3.1, capa 4).</summary>
        public const float FlowSpeedDp = 12f;

        /// <summary>Chevrons a la vez como mucho.</summary>
        public const int MaxChevrons = 40;

        /// <summary>Destellos a la vez como mucho.</summary>
        public const int MaxGlints = 12;

        /// <summary>Periodo del titileo de un destello (ms) [P].</summary>
        public const float GlintPeriodMs = 1800f;

        /// <summary>Opacidad máxima de un chevron [P].</summary>
        public const float ChevronAlpha = 0.5f;

        /// <summary>La niebla ocupa esta fracción de arriba de la vista (propuesta §3.1, capa 8: el 25 %).</summary>
        public const float FogFraction = 0.25f;

        /// <summary>Opacidad de la niebla en el borde de arriba [P].</summary>
        public const float FogAlpha = 0.72f;

        /// <summary>Velocidad de las sombras de las nubes (dp/s) [P].</summary>
        public const float CloudSpeedDp = 5f;

        /// <summary>Opacidad de las sombras de las nubes [P].</summary>
        public const float CloudAlpha = 0.10f;

        /// <summary>El primer plano se mueve este tanto más que el mapa al arrastrar (propuesta §3.1, capa 9: ×1,15).</summary>
        public const float ParallaxFactor = 0.15f;

        /// <summary>Lo más que se aparta el primer plano de su esquina (dp) [P].</summary>
        public const float ParallaxMaxDp = 36f;

        /// <summary>Lado de cada racimo del primer plano (dp) [P].</summary>
        public const float ForegroundDp = 132f;

        /// <summary>Margen de la región de la selva y de la corriente alrededor de la vista, en fracción de la vista [P].</summary>
        public const float RegionMargin = 0.3f;

        /// <summary>Banda de la várzea por clase de línea, en fracción del <c>band</c> de los datos [P].</summary>
        public static readonly float[] VarzeaClassScale = { 1f, 0.6f, 0.35f };

        private readonly MapLiveArt _liveArt = new MapLiveArt();
        private readonly List<RiverRibbonGraphic> _varzea = new List<RiverRibbonGraphic>();
        private readonly List<RiverRibbonGraphic> _bankRibbons = new List<RiverRibbonGraphic>();
        private readonly Image?[] _chevrons = new Image?[MaxChevrons];
        private readonly Image?[] _glints = new Image?[MaxGlints];
        private readonly int[] _slotLine = new int[MaxChevrons];
        private readonly float[] _slotArc = new float[MaxChevrons];
        private readonly float[] _frameMs = new float[MapFrameSamples];
        private bool _live;
        private bool _liveBuilt;
        private RectTransform? _varzeaLayer;
        private RectTransform? _bankLayer;
        private CanopyGraphic? _canopy;
        private Canvas? _atmoCanvas;
        private RectTransform? _atmoRoot;
        private Image? _light;
        private Image? _fog;
        private Image? _andes;
        private Image? _cloudA;
        private Image? _cloudB;
        private RectTransform? _flowRoot;
        private RectTransform? _foreground;
        private Image? _fgLeft;
        private Image? _fgRight;
        private Image? _footerPanel;
        private int _slotCount;
        private int _glintCount;
        private float _liveClockMs;
        private float _flowPhaseDp;
        private Vector2 _offsetAtOpen;
        private bool _parallaxReset;
        private Rect _liveRegion;
        private int _nextMarker = -1;
        private float _bleedLeft;
        private float _bleedRight;
        private int _frameCount;
        private int _frameNext;
        private int _restFrames;

        /// <summary>Frames activos (a 60 fps) que guarda la sonda <c>AF_PERF map_frames</c>.</summary>
        public const int MapFrameSamples = 600;

        /// <summary>
        /// <c>ui_map_v2</c> (08 §9; D-069, P8 (a)): lo pone <see cref="GameBootstrap"/> antes de abrir. Apagada (o sin vegetación en los
        /// datos), el Mapa de la 1.0.
        /// </summary>
        public bool LiveRequested { get; set; }

        /// <summary>Esta apertura dibuja el Mapa v2.</summary>
        public bool LiveActive => _live;

        /// <summary>El arte del Mapa v2 (tests y sonda).</summary>
        internal MapLiveArt LiveArt => _liveArt;

        /// <summary>La selva (null hasta la primera apertura con el Mapa v2).</summary>
        internal CanopyGraphic? Canopy => _canopy;

        /// <summary>Cintas de la várzea (una por línea).</summary>
        internal IReadOnlyList<RiverRibbonGraphic> VarzeaRibbons => _varzea;

        /// <summary>Cintas de las orillas (una por línea).</summary>
        internal IReadOnlyList<RiverRibbonGraphic> BankRibbons => _bankRibbons;

        /// <summary>Chevrons de la corriente activos.</summary>
        internal int FlowSlotCount => _slotCount;

        /// <summary>Chevron <paramref name="i"/> (tests de Reducir movimiento).</summary>
        internal Image? Chevron(int i)
        {
            return _chevrons[i];
        }

        /// <summary>Sombra de nube <paramref name="i"/> (0 o 1).</summary>
        internal Image? Cloud(int i)
        {
            return i == 0 ? _cloudA : _cloudB;
        }

        /// <summary>Racimo del primer plano (abajo a la izquierda con false).</summary>
        internal Image? Foreground(bool topRight)
        {
            return topRight ? _fgRight : _fgLeft;
        }

        /// <summary>La luz del mundo.</summary>
        internal Image? LightLayer => _light;

        /// <summary>La niebla de arriba.</summary>
        internal Image? FogLayer => _fog;

        /// <summary>La barra de madera de abajo del Mapa v2.</summary>
        internal Image? FooterPanel => _footerPanel;

        /// <summary>Lienzo de la luz, las nubes y la niebla.</summary>
        internal Canvas? AtmosphereCanvas => _atmoCanvas;

        /// <summary>
        /// Luz de cada mundo (propuesta del Mapa v2 §3.3) [P]: el color del degradado que entra arriba a la izquierda, con su opacidad
        /// en el alfa. Del 10 en adelante, el crepúsculo azul.
        /// </summary>
        public static Color WorldLight(int world)
        {
            switch (world)
            {
                case 1:
                    return new Color(1f, 0.82f, 0.48f, 0.20f); // Estuario: mañana dorada
                case 2:
                    return new Color(1f, 0.97f, 0.85f, 0.16f); // Tapajós: mediodía claro
                case 3:
                    return new Color(1f, 0.70f, 0.35f, 0.20f); // Manaos: tarde ámbar
                case 4:
                    return new Color(1f, 0.55f, 0.42f, 0.22f); // Trapecio: atardecer
                case 5:
                    return new Color(0.80f, 0.89f, 1f, 0.22f); // Loreto: alba con neblina
                case 6:
                    return new Color(0.74f, 0.82f, 0.80f, 0.20f); // Oriente: nublado tropical
                case 7:
                    return new Color(1f, 0.76f, 0.48f, 0.18f); // Madre de Dios: mañana con guacamayos
                case 8:
                    return new Color(1f, 0.81f, 0.42f, 0.22f); // Madidi: tarde dorada
                case 9:
                    return new Color(1f, 0.60f, 0.30f, 0.22f); // Ucayali: atardecer anaranjado
                default:
                    return new Color(0.44f, 0.53f, 0.80f, 0.22f); // Alto Negro: crepúsculo azul
            }
        }

        /// <summary>Niebla de arriba (propuesta §3.1, capa 8): blanco azulado [P].</summary>
        public static readonly Color FogColor = new Color(0.90f, 0.95f, 0.96f, FogAlpha);

        // ============================================================================================ apertura y cierre

        // Al abrir, tras EnsureGeometry: decide si esta apertura es la v2 y prepara las capas.
        partial void LiveOpen(RiverMap map)
        {
            _live = LiveRequested && map.Vegetation.Count > 0;
            _land.SetLive(_live);
            _land.SetCanopyOnly(!_live); // sin el Mapa v2, la tierra de la 1.0 (D-069, P8 (a): el respaldo)
            _frame.enabled = !_live;
            _frameCorners.gameObject.SetActive(!_live);
            for (int i = 0; i < _glass.Count; i++)
            {
                _glass[i].SetLive(_live);
            }

            if (!_live)
            {
                SetLiveObjectsActive(false);
                _liveArt.Release();
                return;
            }

            _liveArt.Build();
            EnsureLiveObjects();
            IReadOnlyList<RiverLine> lines = map.Lines;
            RiverLandZone? varzea = map.Land.Zone("varzea");
            float band = varzea != null && varzea.IsBand ? varzea.BandDp : 0f;
            for (int i = 0; i < lines.Count; i++)
            {
                RiverRibbonGraphic v = LiveRibbonAt(_varzea, i, _varzeaLayer!, RibbonPart.Varzea, "Varzea_");
                v.gameObject.SetActive(band > 0f);
                v.SetLine(lines[i]);
                v.SetBand(band * VarzeaClassScale[Mathf.Clamp(lines[i].Class, 1, 3) - 1]);
                v.SetAnchors(LineAnchors(lines[i]) ?? RiverRibbonGraphic.MouthAnchorsOf(map, lines[i]));
                RiverRibbonGraphic b = LiveRibbonAt(_bankRibbons, i, _bankLayer!, RibbonPart.Banks, "Banks_");
                b.gameObject.SetActive(true);
                b.SetLine(lines[i]);
                b.SetBanks(map.Banks);
                b.SetAnchors(LineAnchors(lines[i]) ?? RiverRibbonGraphic.MouthAnchorsOf(map, lines[i]));
            }

            for (int i = lines.Count; i < _varzea.Count; i++)
            {
                _varzea[i].gameObject.SetActive(false);
                _bankRibbons[i].gameObject.SetActive(false);
            }

            _canopy!.SetData(map.Vegetation, _space, _liveArt.Atlas);
            _light!.sprite = _liveArt.Light;
            _light.color = WorldLight(_world);
            _fog!.sprite = _liveArt.Fog;
            _andes!.sprite = _liveArt.Andes;
            Sprite? cloud = _liveArt.Piece(MapLivePiece.Cloud);
            _cloudA!.sprite = cloud;
            _cloudB!.sprite = cloud;
            _fgLeft!.sprite = _liveArt.ForegroundLeft;
            _fgRight!.sprite = _liveArt.ForegroundRight;
            Sprite? chevron = _liveArt.Piece(MapLivePiece.Chevron);
            Sprite? glint = _liveArt.Piece(MapLivePiece.Glint);
            for (int i = 0; i < MaxChevrons; i++)
            {
                _chevrons[i]!.sprite = chevron;
            }

            for (int i = 0; i < MaxGlints; i++)
            {
                _glints[i]!.sprite = glint;
            }

            _liveClockMs = 0f;
            _flowPhaseDp = 0f;
            _liveRegion = Rect.zero;
            _nextMarker = -1;
            _frameCount = 0;
            _frameNext = 0;
            _restFrames = 0;
            SetLiveObjectsActive(true);
        }

        // Al cerrar: suelta el arte (≈ 2 MiB) y escribe la sonda de fotogramas.
        partial void LiveClose()
        {
            if (_live)
            {
                EmitMapFrames();
            }

            if (_liveBuilt)
            {
                _canopy!.SetData(null, _space, null);
                ClearLiveSprites();
            }

            _liveArt.Release();
        }

        // Tras ApplyView: las capas de la vista (la várzea y las orillas solo en la vista Mundo).
        partial void LiveSpace(MapSpace space)
        {
            if (!_live || !_liveBuilt)
            {
                return;
            }

            bool world = _view == MapView.World;
            _varzeaLayer!.gameObject.SetActive(world);
            _bankLayer!.gameObject.SetActive(world);
            _flowRoot!.gameObject.SetActive(world);
            for (int i = 0; i < _varzea.Count; i++)
            {
                FitLiveRibbon(_varzea[i], space);
                FitLiveRibbon(_bankRibbons[i], space);
            }

            _canopy!.SetData(_map!.Vegetation, space, _liveArt.Atlas);
            _canopy.SetRiverClearance((MapRibbonWidthDp(_view, 1) * 0.5f) + (world ? 16f : 2f));
            _parallaxReset = true; // el primer plano vuelve a su esquina con el encuadre nuevo (LiveOffset)
            _liveRegion = Rect.zero; // la región se rehace con el encuadre nuevo (LiveOffset)
        }

        private void FitLiveRibbon(RiverRibbonGraphic ribbon, MapSpace space)
        {
            if (!ribbon.gameObject.activeSelf || ribbon.Line == null)
            {
                return;
            }

            ribbon.SetSpace(space);
            ribbon.SetWidth(MapRibbonWidthDp(_view, ribbon.Line.Class));
            ribbon.SetShapeWidth(MapShapeWidthDp(_view, ribbon.Line.Class));
            ribbon.SetAnchorWidth(MapAnchorWidthDp(_view, ribbon.Line.Class));
            ribbon.SetCurve(_view == MapView.World);
            ribbon.SetAnchors(LineAnchors(ribbon.Line) ?? RiverRibbonGraphic.MouthAnchorsOf(_map!, ribbon.Line));
        }

        /// <summary>
        /// Suelta el arte del Mapa v2 sin tocar la pantalla (que puede estar ya destruida): lo llama <see cref="GameBootstrap"/> si la
        /// escena se va con el Mapa abierto, porque sus texturas (<c>DontSave</c>) no se descargan con ella.
        /// </summary>
        internal void ReleaseLiveArt()
        {
            _liveArt.Release();
        }

        // Tras SetOffset: la atmósfera sigue a la vista, el primer plano hace parallax y la región de la selva se rehace si la vista
        // se sale de la que hay. Sin asignaciones salvo al rehacer la malla de la selva.
        partial void LiveOffset()
        {
            if (!_live || !_liveBuilt)
            {
                return;
            }

            float w = _viewW + _bleedLeft + _bleedRight;
            RectTransform atmo = _atmoRoot!;
            atmo.anchoredPosition = new Vector2(-_offset.x - _bleedLeft, _offset.y);
            atmo.sizeDelta = new Vector2(w, _viewH);
            bool upright = !_northUpOn && !NorthUpTurning;
            if (atmo.gameObject.activeSelf != upright)
            {
                atmo.gameObject.SetActive(upright); // con el norte arriba, la niebla y la luz no tienen sentido
            }

            if (_parallaxReset)
            {
                _parallaxReset = false;
                _offsetAtOpen = _offset;
            }

            Vector2 drift = ParallaxDrift(_offset - _offsetAtOpen, ProgressReduceMotion);

            _fgLeft!.rectTransform.anchoredPosition = new Vector2(-_bleedLeft - 18f + drift.x, -18f - drift.y);
            _fgRight!.rectTransform.anchoredPosition = new Vector2(_bleedRight + 18f + drift.x, 18f - drift.y);

            Rect visible = VisibleMapRect();
            if (!Contains(_liveRegion, visible))
            {
                RebuildLiveRegion(visible);
            }
        }

        // ============================================================================================ cada fotograma

        partial void LiveTick(float dtMs)
        {
            if (!_live || !_liveBuilt)
            {
                return;
            }

            SampleFrame(dtMs);
            bool still = ProgressReduceMotion;
            _liveClockMs = AdvanceClock(_liveClockMs, dtMs, still);
            _flowPhaseDp = AdvanceFlow(_flowPhaseDp, dtMs, still);

            PlaceFlow(still);
            PlaceClouds(still);
            PulseNext(still);
        }

        /// <summary>
        /// Reloj del ambiente del Mapa v2 tras <paramref name="dtMs"/> ms: con Reducir movimiento (<paramref name="still"/>; propuesta
        /// del Mapa v2 §7.3), quieto.
        /// </summary>
        internal static float AdvanceClock(float clockMs, float dtMs, bool still)
        {
            return still ? clockMs : clockMs + Mathf.Max(0f, dtMs);
        }

        /// <summary>
        /// Fase de la corriente (dp, de 0 a <see cref="FlowStepDp"/>) tras <paramref name="dtMs"/> ms a <see cref="FlowSpeedDp"/> dp/s;
        /// con Reducir movimiento, quieta.
        /// </summary>
        internal static float AdvanceFlow(float phaseDp, float dtMs, bool still)
        {
            return still ? phaseDp : (phaseDp + (FlowSpeedDp * Mathf.Max(0f, dtMs) / 1000f)) % FlowStepDp;
        }

        /// <summary>Desplazamiento del primer plano al arrastrar <paramref name="moved"/> dp: ×<see cref="ParallaxFactor"/>, con tope; quieto con Reducir movimiento.</summary>
        internal static Vector2 ParallaxDrift(Vector2 moved, bool still)
        {
            if (still)
            {
                return Vector2.zero;
            }

            Vector2 drift = moved * ParallaxFactor;
            return new Vector2(Mathf.Clamp(drift.x, -ParallaxMaxDp, ParallaxMaxDp), Mathf.Clamp(drift.y, -ParallaxMaxDp, ParallaxMaxDp));
        }

        private void PlaceFlow(bool still)
        {
            RiverMap? map = _map;
            if (map == null || _view != MapView.World || _space.DpPerUnit <= 0f)
            {
                return;
            }

            float phaseUnits = _flowPhaseDp / _space.DpPerUnit;
            float fade = still ? 0.6f : Mathf.Sin(Mathf.PI * (_flowPhaseDp / FlowStepDp));
            for (int i = 0; i < _slotCount; i++)
            {
                Image chevron = _chevrons[i]!;
                RiverRibbonGraphic glass = _glass[_slotLine[i]];
                PolylineCursor? cursor = glass.Cursor;
                if (cursor == null)
                {
                    continue;
                }

                float arc = Mathf.Min(cursor.Length, _slotArc[i] + phaseUnits);
                Vector2 p = _space.ToLocal(cursor.PointAtArc(arc));
                Vector2 d = MapSpace.DirectionToLocal(cursor.TangentAtArc(arc));
                RectTransform rt = chevron.rectTransform;
                rt.anchoredPosition = p;
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                chevron.canvasRenderer.SetAlpha(ChevronAlpha * fade);
            }

            for (int i = 0; i < _glintCount; i++)
            {
                float phase = (_liveClockMs / GlintPeriodMs) + (i * 0.37f);
                float a = still ? 0.6f : 0.5f + (0.5f * Mathf.Sin(phase * 2f * Mathf.PI));
                _glints[i]!.canvasRenderer.SetAlpha(a * a * 0.8f);
            }
        }

        private void PlaceClouds(bool still)
        {
            float w = _viewW + _bleedLeft + _bleedRight + 240f;
            float t = still ? 0f : _liveClockMs / 1000f * CloudSpeedDp;
            _cloudA!.rectTransform.anchoredPosition = new Vector2(Wrap((0.15f * w) + t, w) - 120f, -(0.42f * _viewH));
            _cloudB!.rectTransform.anchoredPosition = new Vector2(Wrap((0.70f * w) + (0.7f * t), w) - 120f, -(0.74f * _viewH));
        }

        private static float Wrap(float x, float w)
        {
            return w > 0f ? x - (Mathf.Floor(x / w) * w) : x;
        }

        private void PulseNext(bool still)
        {
            if (_nextMarker < 0 || _nextMarker >= _markerCount)
            {
                return;
            }

            Image? glow = _markers[_nextMarker].Glow;
            if (glow == null)
            {
                return;
            }

            float a = still ? 0.45f : 0.30f + (0.25f * Mathf.Sin(_liveClockMs / NextGlowPeriodMs * 2f * Mathf.PI));
            glow.canvasRenderer.SetAlpha(a);
        }

        // ============================================================================================ región visible

        /// <summary>La vista (con lo que se sale por los lados de la columna) en unidades del marco.</summary>
        internal Rect VisibleMapRect()
        {
            float dpu = Mathf.Max(1e-4f, _space.DpPerUnit);
            if (_northUpOn || NorthUpTurning)
            {
                return new Rect(0f, 0f, _map != null ? _map.Width : 1f, _map != null ? _map.Height : 1f); // girada, se ve entera
            }

            float x0 = (-_offset.x - _bleedLeft) / dpu;
            float y0 = -_offset.y / dpu;
            return new Rect(x0, y0, (_viewW + _bleedLeft + _bleedRight) / dpu, _viewH / dpu);
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            return outer.width > 0f && outer.xMin <= inner.xMin && outer.yMin <= inner.yMin && outer.xMax >= inner.xMax && outer.yMax >= inner.yMax;
        }

        // La región con margen: la selva y los chevrons de la corriente. Asigna lo que asigne la malla de la selva (VertexHelper).
        private void RebuildLiveRegion(Rect visible)
        {
            float mx = visible.width * RegionMargin;
            float my = visible.height * RegionMargin;
            _liveRegion = new Rect(visible.xMin - mx, visible.yMin - my, visible.width + (2f * mx), visible.height + (2f * my));
            _canopy!.SetRegion(_liveRegion);
            ChooseFlowSlots(_liveRegion);
        }

        // Los sitios de los chevrons: cada FlowStepDp de agua de las líneas de clase 1-2 dentro de la región, hasta MaxChevrons; y un
        // destello de cada tres chevrons, algo apartado del eje.
        private void ChooseFlowSlots(Rect region)
        {
            _slotCount = 0;
            _glintCount = 0;
            RiverMap? map = _map;
            if (map != null && _view == MapView.World && _space.DpPerUnit > 0f)
            {
                float step = FlowStepDp / _space.DpPerUnit;
                for (int l = 0; l < _glass.Count && l < map.Lines.Count && _slotCount < MaxChevrons; l++)
                {
                    RiverRibbonGraphic glass = _glass[l];
                    PolylineCursor? cursor = glass.Cursor;
                    if (!glass.gameObject.activeSelf || cursor == null || map.Lines[l].Class > 2)
                    {
                        continue;
                    }

                    for (float a = step * 0.5f; a < cursor.Length - step && _slotCount < MaxChevrons; a += step)
                    {
                        if (!region.Contains(cursor.PointAtArc(a)))
                        {
                            continue;
                        }

                        _slotLine[_slotCount] = l;
                        _slotArc[_slotCount] = a;
                        float size = map.Lines[l].Class == 1 ? 10f : 7f;
                        _chevrons[_slotCount]!.rectTransform.sizeDelta = new Vector2(size, size);
                        if (_slotCount % 3 == 1 && _glintCount < MaxGlints)
                        {
                            float side = (_glintCount % 2 == 0 ? 1f : -1f) * MapRibbonWidthDp(_view, map.Lines[l].Class) * 0.22f;
                            Vector2 p = _space.ToLocal(cursor.PointAtArc(a + (step * 0.5f)));
                            Vector2 n = MapSpace.DirectionToLocal(cursor.TangentAtArc(a));
                            Vector2 normal = new Vector2(-n.y, n.x);
                            RectTransform g = _glints[_glintCount]!.rectTransform;
                            g.anchoredPosition = p + (normal * side);
                            float gs = map.Lines[l].Class == 1 ? 9f : 6f;
                            g.sizeDelta = new Vector2(gs, gs);
                            _glintCount++;
                        }

                        _slotCount++;
                    }
                }
            }

            for (int i = 0; i < MaxChevrons; i++)
            {
                bool on = i < _slotCount;
                if (_chevrons[i]!.gameObject.activeSelf != on)
                {
                    _chevrons[i]!.gameObject.SetActive(on);
                }
            }

            for (int i = 0; i < MaxGlints; i++)
            {
                bool on = i < _glintCount;
                if (_glints[i]!.gameObject.activeSelf != on)
                {
                    _glints[i]!.gameObject.SetActive(on);
                }
            }

            PlaceFlow(ProgressReduceMotion);
        }

        // ============================================================================================ maquetación

        // Tras Layout: la barra de abajo, la máscara abierta a los lados, la atmósfera y el primer plano.
        partial void LiveLayout(float mapTop, float mapBottom)
        {
            ScreenFrame frame = _canvas.Frame;
            _bleedLeft = _live ? Mathf.Max(0f, frame.ColumnX) : 0f;
            _bleedRight = _live ? frame.RightBand : 0f;
            RectMask2D? mask = _viewport.GetComponent<RectMask2D>();
            if (mask != null)
            {
                mask.padding = new Vector4(-_bleedLeft, 0f, -_bleedRight, 0f); // el Mapa llega al borde; los controles, en la columna
            }

            RectTransform ground = _ground.rectTransform;
            ground.offsetMin = new Vector2(-_bleedLeft, 0f);
            ground.offsetMax = new Vector2(_bleedRight, 0f);
            if (!_live || !_liveBuilt)
            {
                return;
            }

            RectTransform footer = _footerPanel!.rectTransform;
            footer.offsetMin = new Vector2(footer.offsetMin.x, -8f);
            footer.offsetMax = new Vector2(footer.offsetMax.x, -mapBottom);

            float fogH = _viewH * FogFraction;
            StretchX(_fog!.rectTransform, fogH);
            float andesH = Mathf.Min(70f, fogH * 0.55f);
            StretchX(_andes!.rectTransform, andesH);
            _andes.color = new Color(1f, 1f, 1f, 0.5f);
            _light!.rectTransform.anchorMin = Vector2.zero;
            _light.rectTransform.anchorMax = Vector2.one;
            _light.rectTransform.offsetMin = Vector2.zero;
            _light.rectTransform.offsetMax = Vector2.zero;
            RectTransform fg = _foreground!;
            fg.offsetMin = new Vector2(-_bleedLeft, 0f);
            fg.offsetMax = new Vector2(_bleedRight, 0f);
            LiveOffset();
        }

        // Ancho de su padre, arriba, de alto h.
        private static void StretchX(RectTransform rt, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -h);
            rt.offsetMax = Vector2.zero;
        }

        // ============================================================================================ boyas

        // Tras ShowMarkersAt: las paradas futuras como boyas; la siguiente, con su mini cuia y el brillo que late.
        partial void LiveMarkers(int max)
        {
            int next = (max / RiverStopCatalog.LevelsPerStop) + 1;
            _nextMarker = -1;
            for (int i = 0; i < _markerCount; i++)
            {
                StopMarker m = _markers[i];
                bool buoy = _live && !m.Reached && m.N > next;
                bool isNext = _live && !m.Reached && m.N == next;
                if (isNext)
                {
                    _nextMarker = i;
                }

                if (buoy && m.Buoy == null)
                {
                    m.Buoy = MarkerPiece("Buoy", m.Root, null!);
                    m.Buoy.raycastTarget = false;
                    m.Buoy.rectTransform.sizeDelta = new Vector2(LiveBuoyDp, LiveBuoyDp);
                }

                if (isNext && m.Glow == null)
                {
                    m.Glow = MarkerPiece("NextGlow", m.Root, null!);
                    m.Glow.raycastTarget = false;
                    m.Glow.rectTransform.SetAsFirstSibling();
                    m.Glow.rectTransform.sizeDelta = new Vector2(NextGlowDp, NextGlowDp);
                    m.Glow.color = new Color(1f, 0.93f, 0.70f, 1f);
                }

                if (m.Buoy != null)
                {
                    m.Buoy.sprite = _liveArt.Piece(MapLivePiece.Buoy);
                    m.Buoy.enabled = buoy && m.Buoy.sprite != null;
                }

                if (m.Glow != null)
                {
                    m.Glow.sprite = _liveArt.Piece(MapLivePiece.Cloud);
                    m.Glow.enabled = isNext && m.Glow.sprite != null;
                }

                bool cuia = !buoy;
                m.Glass.enabled = cuia;
                m.WaterA.enabled = cuia;
                m.WaterB.enabled = cuia;
                m.Rim.enabled = cuia;
            }
        }

        /// <summary>La parada <paramref name="n"/> se pinta como boya (tests).</summary>
        internal bool StopMarkerIsBuoy(int n)
        {
            int i = StopMarkerIndexOf(n);
            return i >= 0 && _markers[i].Buoy != null && _markers[i].Buoy!.enabled;
        }

        /// <summary>La parada <paramref name="n"/> lleva el brillo de la siguiente (tests).</summary>
        internal bool StopMarkerIsNext(int n)
        {
            int i = StopMarkerIndexOf(n);
            return i >= 0 && _markers[i].Glow != null && _markers[i].Glow!.enabled;
        }

        // ============================================================================================ construcción

        private void EnsureLiveObjects()
        {
            if (_liveBuilt)
            {
                return;
            }

            _liveBuilt = true;
            // Lienzo estático: la várzea y la selva justo encima de la tierra, las orillas encima de la selva; el vidrio, después.
            int landIndex = _land.transform.GetSiblingIndex();
            _varzeaLayer = UiBuilder.Rect("Varzea", _staticLayer);
            FillTopLeft(_varzeaLayer);
            _varzeaLayer.SetSiblingIndex(landIndex + 1);
            var canopyGo = new GameObject("Canopy", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanopyGraphic));
            canopyGo.transform.SetParent(_staticLayer, false);
            FillTopLeft((RectTransform)canopyGo.transform);
            canopyGo.transform.SetSiblingIndex(landIndex + 2);
            _canopy = canopyGo.GetComponent<CanopyGraphic>();
            _canopy.Setup();
            _bankLayer = UiBuilder.Rect("Banks", _staticLayer);
            FillTopLeft(_bankLayer);
            _bankLayer.SetSiblingIndex(landIndex + 3);

            // Atmósfera: entre el lienzo estático y el dinámico, en su propio lienzo (las nubes se mueven sin rehacer el resto).
            var atmoGo = new GameObject("Atmosphere", typeof(RectTransform), typeof(Canvas));
            atmoGo.transform.SetParent(_content, false);
            _atmoRoot = (RectTransform)atmoGo.transform;
            _atmoRoot.anchorMin = new Vector2(0f, 1f);
            _atmoRoot.anchorMax = new Vector2(0f, 1f);
            _atmoRoot.pivot = new Vector2(0f, 1f);
            _atmoRoot.SetSiblingIndex(_dynamicCanvas.transform.GetSiblingIndex());
            _atmoCanvas = atmoGo.GetComponent<Canvas>();
            _atmoCanvas.overrideSorting = false;
            _light = LiveImage("WorldLight", _atmoRoot);
            _cloudA = LiveImage("CloudShadowA", _atmoRoot);
            _cloudB = LiveImage("CloudShadowB", _atmoRoot);
            foreach (Image cloud in new[] { _cloudA, _cloudB })
            {
                RectTransform rt = cloud.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(240f, 130f);
                cloud.color = new Color(0.02f, 0.12f, 0.08f, CloudAlpha);
            }

            _fog = LiveImage("Fog", _atmoRoot);
            _fog.color = FogColor;
            _andes = LiveImage("Andes", _atmoRoot);

            // La corriente: dentro del lienzo dinámico, debajo de la canoa y los marcadores, en su propio lienzo.
            var flowGo = new GameObject("Flow", typeof(RectTransform), typeof(Canvas));
            flowGo.transform.SetParent(_dynamicLayer, false);
            _flowRoot = (RectTransform)flowGo.transform;
            FillTopLeft(_flowRoot);
            _flowRoot.SetAsFirstSibling();
            flowGo.GetComponent<Canvas>().overrideSorting = false;
            for (int i = 0; i < MaxGlints; i++)
            {
                _glints[i] = FlowImage("Glint" + i, new Color(1f, 1f, 0.92f, 1f));
            }

            for (int i = 0; i < MaxChevrons; i++)
            {
                _chevrons[i] = FlowImage("Chevron" + i, new Color(0.96f, 0.99f, 1f, 1f));
            }

            // Primer plano: encima de todo lo del mapa, sin toques, en su propio lienzo.
            var fgGo = new GameObject("Foreground", typeof(RectTransform), typeof(Canvas));
            fgGo.transform.SetParent(_viewport, false);
            _foreground = (RectTransform)fgGo.transform;
            UiBuilder.Stretch(_foreground);
            fgGo.GetComponent<Canvas>().overrideSorting = false;
            _fgLeft = LiveImage("LeavesLeft", _foreground);
            Corner(_fgLeft.rectTransform, false);
            _fgRight = LiveImage("LeavesRight", _foreground);
            Corner(_fgRight.rectTransform, true);

            // La barra de madera de abajo, detrás del pie, de borde a borde.
            _footerPanel = UiBuilder.Sliced("FooterPlate", _root, _art.WoodPlate, Color.white);
            _footerPanel.raycastTarget = false;
            ScreenColumn.Bleed(_footerPanel.rectTransform);
            _footerPanel.rectTransform.SetSiblingIndex(_frame.transform.GetSiblingIndex() + 1);
        }

        private static void Corner(RectTransform rt, bool topRight)
        {
            Vector2 a = topRight ? new Vector2(1f, 1f) : new Vector2(0f, 0f);
            rt.anchorMin = a;
            rt.anchorMax = a;
            rt.pivot = a;
            rt.sizeDelta = new Vector2(ForegroundDp, ForegroundDp);
        }

        private static Image LiveImage(string name, RectTransform parent)
        {
            Image image = UiBuilder.Image(name, parent, null, Color.white);
            image.raycastTarget = false;
            return image;
        }

        private Image FlowImage(string name, Color color)
        {
            Image image = UiBuilder.Image(name, _flowRoot!, null, color);
            image.raycastTarget = false;
            RectTransform rt = image.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            image.gameObject.SetActive(false);
            return image;
        }

        private static RiverRibbonGraphic LiveRibbonAt(List<RiverRibbonGraphic> list, int i, RectTransform parent, RibbonPart part, string prefix)
        {
            while (list.Count <= i)
            {
                var go = new GameObject(prefix + list.Count, typeof(RectTransform), typeof(CanvasRenderer), typeof(RiverRibbonGraphic));
                go.transform.SetParent(parent, false);
                FillTopLeft((RectTransform)go.transform);
                RiverRibbonGraphic ribbon = go.GetComponent<RiverRibbonGraphic>();
                ribbon.Setup();
                ribbon.SetPart(part);
                ribbon.SetLook(RibbonLook.Glass);
                ribbon.SetSmooth(true);
                ribbon.SetFill(0f, 1f);
                list.Add(ribbon);
            }

            return list[i];
        }

        private void SetLiveObjectsActive(bool on)
        {
            if (!_liveBuilt)
            {
                return;
            }

            _varzeaLayer!.gameObject.SetActive(on);
            _bankLayer!.gameObject.SetActive(on);
            _canopy!.gameObject.SetActive(on);
            _atmoRoot!.gameObject.SetActive(on);
            _flowRoot!.gameObject.SetActive(on);
            _foreground!.gameObject.SetActive(on);
            _footerPanel!.gameObject.SetActive(on);
        }

        // Quita los sprites del arte que se va a soltar (ninguna Image se queda con una textura destruida).
        private void ClearLiveSprites()
        {
            _light!.sprite = null;
            _fog!.sprite = null;
            _andes!.sprite = null;
            _cloudA!.sprite = null;
            _cloudB!.sprite = null;
            _fgLeft!.sprite = null;
            _fgRight!.sprite = null;
            for (int i = 0; i < MaxChevrons; i++)
            {
                _chevrons[i]!.sprite = null;
            }

            for (int i = 0; i < MaxGlints; i++)
            {
                _glints[i]!.sprite = null;
            }

            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i].Buoy != null)
                {
                    _markers[i].Buoy!.sprite = null;
                    _markers[i].Buoy!.enabled = false;
                }

                if (_markers[i].Glow != null)
                {
                    _markers[i].Glow!.sprite = null;
                    _markers[i].Glow!.enabled = false;
                }
            }
        }

        // ============================================================================================ sonda AF_PERF map_frames

        // Un fotograma: con el Mapa pintando a 60 fps (arrastre, viaje o animación), su tiempo; en reposo solo se cuenta.
        private void SampleFrame(float dtMs)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            if (OnDemandRendering.renderFrameInterval > 1)
            {
                _restFrames++;
                return;
            }

            _frameMs[_frameNext] = dtMs;
            _frameNext = (_frameNext + 1) % MapFrameSamples;
            _frameCount = Mathf.Min(MapFrameSamples, _frameCount + 1);
        }

        // AF_PERF {"event":"map_frames","v2":1,"world":…,"view":…,"frames":…,"rest_frames":…,"avg_ms":…,"p95_ms":…,"max_ms":…,
        // "fps":…,"veg_quads":…,"live_tex_kib":…}: al cerrar el Mapa v2 en desarrollo (D-069: medir 60 fps en el teléfono de pruebas
        // y ≥ 30 en gama baja antes de cerrar M2-1).
        private void EmitMapFrames()
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            var sorted = new float[_frameCount];
            Array.Copy(_frameMs, sorted, _frameCount);
            Array.Sort(sorted);
            DevProbe.Write(DevProbe.FormatMapFrames(_world, _view == MapView.World ? "world" : "basin", sorted, _restFrames,
                _canopy != null ? _canopy.LastQuadCount : 0, _liveArt.TextureBytes / 1024L));
        }

        /// <summary>Miembros extra de <c>AF_PERF mem</c> al abrir el Mapa (D-069): si es el v2, sus texturas y los puntos de la selva.</summary>
        internal string LiveMemJson()
        {
            return "\"map_v2\":" + (_live ? "1" : "0") + ",\"live_tex_kib\":" + (_liveArt.TextureBytes / 1024L).ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ",\"veg_points\":" + (_live && _map != null ? _map.Vegetation.Count : 0).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
