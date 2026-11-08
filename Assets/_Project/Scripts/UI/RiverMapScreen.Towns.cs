#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El Mapa del río v2, bloque M2-2 «Pueblos de paso» (D-069; <c>docs/dev/propuesta-mapa-rio-v2.md</c> §4, §7.2, §7.3, §9.3 y
    /// §9.5), parcial de <see cref="RiverMapScreen"/>. Con el Mapa v2 (<see cref="LiveActive"/>) y <c>ui_map_towns</c>
    /// (<see cref="TownsRequested"/>):
    /// <list type="bullet">
    /// <item><description>los <b>pueblos de paso</b> de los tramos de los mundos abiertos se dibujan con su viñeta por código
    /// (<see cref="MapTownArt"/>) en la orilla, junto a su punto del río, en la vista Mundo; un pueblo se <b>pasa</b> al ganar el nivel
    /// <c>10(n − 1) + ⌈10 f⌉</c> (<see cref="RiverTowns.PassLevel"/>, deducido de <c>max_level</c>, sin estado nuevo): los pasados, en
    /// color; los que faltan, apagados;</description></item>
    /// <item><description><b>nivel de detalle</b> (§9.5, para evitar ruido): la viñeta crece con el zoom (de
    /// <see cref="TownMinDp"/> a <see cref="TownMaxDp"/> dp) y, si dos se pisan (o pisan una parada), sale la más importante (la
    /// pasada, la ciudad, la más cerca de la canoa); solo llevan <b>rótulo</b> los pueblos a un tramo o menos de la canoa
    /// (<see cref="RiverTowns.TownLabelShown"/>), la última parada alcanzada y la siguiente (<see cref="RiverTowns.StopLabelShown"/>);
    /// </description></item>
    /// <item><description>al <b>tocar</b> un pueblo pasado (zona táctil de <see cref="RiverMapScreen.MinTouchDp"/> dp) se abre su
    /// ficha (<see cref="StopCard.ShowTown"/>: nombre, país, río y, si lo hay, su dato) y TalkBack lo lee como «Coari · pueblo de paso
    /// · Brasil» (<see cref="TownText.A11yTown"/>);</description></item>
    /// <item><description>la <b>excursión local</b> de los 25 tramos de 0 km (P5 (a); §4.4): las rutas pasan a
    /// <see cref="RiverRoutes.WithExcursions"/>, y la canoa y las 10 boyas del tramo van por el lazo de la excursión
    /// (<see cref="RiverTowns.ExcursionPoint"/>, en <see cref="RoutePoint"/>); con Reducir movimiento, como hoy, la canoa salta.</description></item>
    /// </list>
    /// Las bocas y los rápidos (los <see cref="RiverTownTier.Mark"/>) siguen con el punto de la 1.0 (<c>BuildPassing</c>). Un pueblo
    /// sin nombre aprobado (en release, hasta la revisión de Gemini, D-054) no se dibuja. Con <c>ui_map_towns</c> apagada, el Mapa
    /// de M2-1. Todo es estático: nada se mueve por fotograma (Reducir movimiento no cambia nada aquí) y el Mapa quieto no asigna
    /// memoria. El atlas (2 MiB y sus mipmaps) se pinta al abrir y se suelta al cerrar (§7.2).
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Ancho de la viñeta al zoom mínimo de la vista Mundo (dp) [P].</summary>
        public const float TownMinDp = 36f;

        /// <summary>Ancho de la viñeta al zoom máximo (dp; propuesta §4.2: unos 80 × 60).</summary>
        public const float TownMaxDp = 80f;

        /// <summary>Texto del rótulo de un pueblo o una parada (sp; 12, el mínimo legible).</summary>
        public const float TownLabelSp = 12f;

        /// <summary>Opacidad de la viñeta de un pueblo que aún no se pasó [P].</summary>
        public const float TownAheadAlpha = 0.55f;

        /// <summary>Hueco entre el borde del río y la viñeta (dp) [P].</summary>
        public const float TownBankGapDp = 4f;

        /// <summary>
        /// Hueco entre un rótulo o una viñeta y la caja de la canoa con su tripulación (dp) [P] (añadido de D-069 del 2026-11-08,
        /// observación 2 de la prueba de la 1.1: la canoa con su bandera tapaba «Coari» y «Amacayacu · faltan 5»).
        /// </summary>
        public const float CrewLabelGapDp = 3f;

        // La caja de la canoa (con Capi, el mástil y las banderas) y la del guía con el tucuxi, como quedan con max_level, que ningún
        // rótulo ni viñeta pisa (añadido de D-069 del 2026-11-08). Se rehacen en cada TownsSpace; sin asignaciones.
        private readonly Rect[] _crewLabelBoxes = new Rect[2];
        private int _crewLabelBoxCount;
        private readonly List<Rect> _stopLabelPlaced = new List<Rect>();

        // Sitios que prueba el rótulo de una parada: los cuatro lados de su mini cuia, pegados y dos pasos más allá [P].
        private const int StopLabelSpots = 12;

        private readonly MapTownArt _townArt = new MapTownArt();
        private readonly List<TownMarker> _townMarkers = new List<TownMarker>();
        private readonly List<Rect> _townPlaced = new List<Rect>();
        private readonly List<int> _townOrder = new List<int>();
        private readonly List<StopLabel> _stopLabels = new List<StopLabel>();
        private RectTransform? _townLayer;
        private RiverRoutes? _townRoutesFrom;
        private RiverRoutes? _townRoutes;
        private bool _towns;
        private int _townCount;
        private int _townMax = -1;

        /// <summary>
        /// <c>ui_map_towns</c> (08 §9; D-069, P8 (a)): lo pone <see cref="GameBootstrap"/> antes de abrir. Solo cuenta con el Mapa v2.
        /// </summary>
        public bool TownsRequested { get; set; } = true;

        /// <summary>Esta apertura dibuja los pueblos de paso (y la excursión local).</summary>
        public bool TownsActive => _towns;

        /// <summary>El atlas de los pueblos (tests y sonda).</summary>
        internal MapTownArt TownArt => _townArt;

        /// <summary>Pueblos dibujados (los de los tramos de los mundos abiertos, con nombre).</summary>
        public int TownCount => _towns ? _townCount : 0;

        /// <summary>El pueblo <paramref name="i"/>.</summary>
        public RiverPassingPoint TownAt(int i)
        {
            return _townMarkers[i].Data;
        }

        /// <summary>El pueblo <paramref name="i"/> ya se pasó con el <c>max_level</c> de esta apertura.</summary>
        public bool TownPassed(int i)
        {
            return _townMarkers[i].Passed;
        }

        /// <summary>El pueblo <paramref name="i"/> se ve (vista Mundo, sin que otro más importante lo pise).</summary>
        public bool TownShown(int i)
        {
            return i >= 0 && i < TownCount && _townMarkers[i].Root.gameObject.activeInHierarchy;
        }

        /// <summary>El pueblo <paramref name="i"/> se ve y su centro cae dentro del visor (los que publica la sonda AF_LAYOUT).</summary>
        public bool TownInView(int i)
        {
            if (!TownShown(i) || _view != MapView.World)
            {
                return false;
            }

            Vector2 p = _townMarkers[i].Root.anchoredPosition;
            Vector2 at = new Vector2(_offset.x + p.x, _offset.y - p.y);
            return at.x >= 0f && at.x <= _viewW && at.y >= 0f && at.y <= _viewH;
        }

        /// <summary>El pueblo <paramref name="i"/> lleva rótulo.</summary>
        public bool TownLabelVisible(int i)
        {
            return TownShown(i) && _townMarkers[i].Pill.gameObject.activeSelf;
        }

        /// <summary>Texto del rótulo del pueblo <paramref name="i"/>.</summary>
        public string TownLabelText(int i)
        {
            return _townMarkers[i].Label.text;
        }

        /// <summary>Ancho de la viñeta del pueblo <paramref name="i"/> (dp).</summary>
        public float TownVignetteDp(int i)
        {
            return _townMarkers[i].Vignette.rectTransform.sizeDelta.x;
        }

        /// <summary>Centro del área táctil del pueblo <paramref name="i"/> (dp, local del contenido).</summary>
        public Vector2 TownLocal(int i)
        {
            return _townMarkers[i].Root.anchoredPosition;
        }

        /// <summary>Área táctil del pueblo <paramref name="i"/> (tests: ≥ 48 dp).</summary>
        internal RectTransform TownTouch(int i)
        {
            return _townMarkers[i].Root;
        }

        /// <summary>Índice del pueblo <paramref name="id"/> entre los dibujados, o −1.</summary>
        public int TownIndexOf(string id)
        {
            for (int i = 0; i < TownCount; i++)
            {
                if (string.Equals(_townMarkers[i].Data.Id, id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>La parada <paramref name="n"/> lleva su rótulo (la última alcanzada o la siguiente; §9.5).</summary>
        public bool StopLabelVisible(int n)
        {
            int i = StopMarkerIndexOf(n);
            return i >= 0 && i < _stopLabels.Count && _stopLabels[i].Pill.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Toca el pueblo <paramref name="i"/> (tests y <c>adb</c>: lo mismo que su área táctil): si ya se pasó, abre su ficha.
        /// </summary>
        public bool TapTown(int i)
        {
            if (!Visible || !TownShown(i) || !_townMarkers[i].Passed)
            {
                return false;
            }

            if (CatchUpTouch())
            {
                NoteTouch();
                return true; // el primer toque solo completa «ponerse al día», como en las mini cuias
            }

            NoteTouch();
            _stopsTapped++;
            StopCard card = EnsureCard();
            card.Layout(_canvasW, _canvasH, _safeTop, _safeBottom, CardClearBottom());
            card.ShowTown(_townMarkers[i].Data);
            EmitLayout();
            return true;
        }

        // ============================================================================================ apertura y cierre

        partial void TownsRoutes(ref RiverRoutes routes)
        {
            _towns = _live && TownsRequested;
            _townMax = -1;
            if (!_towns)
            {
                _townArt.Release();
                if (_townLayer != null)
                {
                    _townLayer.gameObject.SetActive(false);
                }

                HideStopLabels();
                return;
            }

            _townArt.Build();
            if (!ReferenceEquals(_townRoutesFrom, routes) || _townRoutes == null)
            {
                _townRoutesFrom = routes;
                _townRoutes = routes.WithExcursions(); // una vez por tabla de rutas: la excursión local (P5 (a))
            }

            routes = _townRoutes;
        }

        partial void TownsClose()
        {
            for (int i = 0; i < _townMarkers.Count; i++)
            {
                _townMarkers[i].Vignette.sprite = null;
            }

            _townArt.Release();
        }

        /// <summary>Suelta el atlas sin tocar la pantalla (como <see cref="ReleaseLiveArt"/>).</summary>
        internal void ReleaseTownArt()
        {
            _townArt.Release();
        }

        /// <summary>Miembros extra de <c>AF_PERF mem</c> (D-069, M2-2): los pueblos y su atlas.</summary>
        internal string TownsMemJson()
        {
            return ",\"towns\":" + TownCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"towns_tex_kib\":"
                + (_townArt.TextureBytes / 1024L).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // ============================================================================================ pueblos

        // Tras BuildPassing (SamplesOpen): los pueblos de los tramos de los mundos abiertos, cada uno una vez (en el tramo más cerca
        // de la canoa), pasados o no con max.
        private void TownsBuild(int max)
        {
            _townCount = 0;
            _townMax = max;
            RiverMap? map = _map;
            RiverRoutes? routes = _routes;
            RiverStopCatalog? stops = _stops;
            if (!_towns || map == null || routes == null || stops == null || _markerLayer == null)
            {
                if (_townLayer != null)
                {
                    _townLayer.gameObject.SetActive(false);
                }

                HideStopLabels();
                if (_live && _markerLayer != null)
                {
                    BuildStopLabels(max); // D-069, M2-4: sin los pueblos, solo el rótulo de la parada siguiente con «Faltan N»
                }

                SyncRemainingPlate();
                return;
            }

            if (_townLayer == null)
            {
                _townLayer = UiBuilder.Rect("Towns", _dynamicLayer);
                FillTopLeft(_townLayer);
            }

            _townLayer.gameObject.SetActive(true);
            _townLayer.SetSiblingIndex(_markerLayer.GetSiblingIndex()); // debajo de las mini cuias: ellas ganan el toque
            int canoeLeg = RiverTowns.CanoeLeg(max);
            for (int n = 1; n < routes.Count && n < stops.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                RiverStopRoute? route = routes.At(n);
                if (stop == null || route == null || WorldTable.WorldOf(RiverStopCatalog.LevelsPerStop * n) > _world)
                {
                    continue; // el tramo se juega en un mundo aún cerrado (su último nivel, 10n)
                }

                for (int v = 0; v < route.Via.Count; v++)
                {
                    RiverPassingPoint? point = map.PassingPoint(route.Via[v]);
                    if (point == null || !point.IsTown || Loc.Fact(point.NameKey).Length == 0)
                    {
                        continue; // una boca o unos rápidos (su punto de la 1.0), o un nombre sin aprobar (en release)
                    }

                    bool passed = RiverTowns.Passed(n, route.ViaFraction(v), max);
                    int at = TownSlotOf(point);
                    if (at < 0)
                    {
                        TownMarker m = TownSlot(_townCount++);
                        m.Data = point;
                        m.Leg = n;
                        m.Passed = passed;
                        m.MapPoint = PlacePoint(map, point.At);
                        m.Root.gameObject.name = "Town_" + point.Id;
                        continue;
                    }

                    TownMarker same = _townMarkers[at];
                    same.Passed |= passed;
                    if (Math.Abs(n - canoeLeg) < Math.Abs(same.Leg - canoeLeg))
                    {
                        same.Leg = n;
                    }
                }
            }

            for (int i = 0; i < _townCount; i++)
            {
                TownMarker m = _townMarkers[i];
                m.Vignette.sprite = _townArt.Piece(m.Data.Vignette, VariantOf(m.Data.Id));
                m.Vignette.color = m.Passed ? Color.white : new Color(0.80f, 0.86f, 0.84f, TownAheadAlpha);
                m.Button.interactable = m.Passed;
                m.Label.text = Loc.Fact(m.Data.NameKey);
                m.WantsLabel = RiverTowns.TownLabelShown(m.Leg, max);
            }

            for (int i = _townCount; i < _townMarkers.Count; i++)
            {
                _townMarkers[i].Root.gameObject.SetActive(false);
            }

            BuildStopLabels(max);
        }

        private int TownSlotOf(RiverPassingPoint point)
        {
            for (int i = 0; i < _townCount; i++)
            {
                if (ReferenceEquals(_townMarkers[i].Data, point))
                {
                    return i;
                }
            }

            return -1;
        }

        // Uno de los 3 juegos de colores, fijo por pueblo (sin string.GetHashCode: el mismo en todas las máquinas).
        internal static int VariantOf(string id)
        {
            int h = 7;
            for (int i = 0; i < id.Length; i++)
            {
                h = (h * 31) + id[i];
            }

            return (h & 0x7fffffff) % ProceduralArt.TownVariants;
        }

        private TownMarker TownSlot(int i)
        {
            while (_townMarkers.Count <= i)
            {
                int index = _townMarkers.Count;
                Button button = UiBuilder.BareButton("Town_" + index, _townLayer!, () => TapTown(index));
                var root = (RectTransform)button.transform;
                root.anchorMin = new Vector2(0f, 1f);
                root.anchorMax = new Vector2(0f, 1f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.sizeDelta = new Vector2(MinTouchDp, MinTouchDp);
                Image vignette = UiBuilder.Image("Vignette", root, null, Color.white);
                vignette.raycastTarget = false;
                vignette.preserveAspect = true;
                RectTransform vr = vignette.rectTransform;
                vr.anchorMin = new Vector2(0.5f, 0.5f);
                vr.anchorMax = new Vector2(0.5f, 0.5f);
                vr.pivot = new Vector2(0.5f, 0.5f);
                Image pill = UiBuilder.Sliced("Label", root, _art.RoundedRect, new Color(Palette.WoodDark.r, Palette.WoodDark.g, Palette.WoodDark.b, 0.88f));
                pill.raycastTarget = false;
                TextMeshProUGUI label = UiBuilder.Label("Text", pill.transform, TextStyle.PlainBold, string.Empty, TownLabelSp, Palette.Cream, TextAlignmentOptions.Center);
                label.raycastTarget = false;
                var marker = new TownMarker(root, button, vignette, pill.rectTransform, label);
                A11yLabels.Set(button, () => TownText.A11yTown(marker.Data)); // TalkBack: «Coari · pueblo de paso · Brasil»
                _townMarkers.Add(marker);
            }

            _townMarkers[i].Root.gameObject.SetActive(true);
            return _townMarkers[i];
        }

        // Tras PlacePassing (SamplesSpace): en la vista Mundo, cada viñeta en su orilla y a la medida del zoom, sin pisarse.
        private void TownsSpace(MapSpace space)
        {
            CollectCrewLabelBoxes();
            _stopLabelPlaced.Clear();
            TownsSpaceCore(space);
            if (!_towns)
            {
                RemainingLabelAlone(); // sin los pueblos, el rótulo de la parada siguiente, fuera de la canoa (y en abanico, si cabe)
            }

            SyncRemainingPlate();
        }

        private void TownsSpaceCore(MapSpace space)
        {
            if (_townLayer == null)
            {
                return;
            }

            bool world = _towns && _view == MapView.World;
            if (_townLayer.gameObject.activeSelf != world)
            {
                _townLayer.gameObject.SetActive(world);
            }

            if (!world || _map == null)
            {
                return;
            }

            float w = TownWidthDp(_zoom);
            float h = w * (ProceduralArt.TownCellHeightPx / (float)ProceduralArt.TownCellWidthPx);
            _townOrder.Clear();
            for (int i = 0; i < _townCount; i++)
            {
                _townOrder.Add(i);
            }

            int canoeLeg = RiverTowns.CanoeLeg(_townMax);
            _townOrder.Sort((a, b) => TownRank(a, canoeLeg).CompareTo(TownRank(b, canoeLeg)));
            _townPlaced.Clear();
            for (int i = 0; i < _markerCount; i++)
            {
                Vector2 c = _markers[i].Root.anchoredPosition;
                _townPlaced.Add(new Rect(c.x - 12f, c.y - 18f, 24f, 36f)); // la mini cuia (o la boya) de cada parada
                if (i < _stopLabels.Count)
                {
                    LayoutStopLabel(i); // fuera de la canoa; en abanico, solo el de la parada siguiente y si cabe
                }

                if (i < _stopLabels.Count && _stopLabels[i].Pill.gameObject.activeSelf)
                {
                    RectTransform p = _stopLabels[i].Pill;
                    Vector2 at = c + p.anchoredPosition;
                    _townPlaced.Add(new Rect(at.x - (p.sizeDelta.x * 0.5f), at.y - (p.sizeDelta.y * 0.5f), p.sizeDelta.x, p.sizeDelta.y));
                }
            }

            for (int k = 0; k < _townOrder.Count; k++)
            {
                TownMarker m = _townMarkers[_townOrder[k]];
                Vector2 axis = space.ToLocal(m.MapPoint);
                float lineW = MapRibbonWidthDp(_view, _map.Lines[m.Data.At.Line].Class) * 0.5f;
                float bank = VariantOf(m.Data.Id) == 1 ? -1f : 1f;
                Vector2 center = default;
                Rect rect = default;
                bool free = false;
                for (int side = 0; side < 2 && !free; side++)
                {
                    // Su orilla y, solo si allí la echa la canoa con su tripulación, la otra (añadido de D-069 del 2026-11-08).
                    Vector2 normal = BankNormal(_map, m.Data, side == 0 ? bank : -bank);
                    center = axis + (normal * (lineW + TownBankGapDp + (h * 0.5f)));
                    rect = new Rect(center.x - (w * 0.5f), center.y - (h * 0.5f), w, h);
                    bool crew = HitsCrew(rect);
                    free = !crew;
                    for (int j = 0; j < _townPlaced.Count && free; j++)
                    {
                        free = !_townPlaced[j].Overlaps(rect);
                    }

                    if (side == 0 && !free && !crew)
                    {
                        break; // lo que la echa de su orilla no es la canoa: como antes, sin viñeta
                    }
                }

                SetTownActive(m, free);
                if (!free)
                {
                    continue;
                }

                _townPlaced.Add(rect);
                m.Root.anchoredPosition = center;
                m.Vignette.rectTransform.sizeDelta = new Vector2(w, h);
                m.Vignette.rectTransform.anchoredPosition = Vector2.zero;
                m.Root.sizeDelta = new Vector2(Mathf.Max(MinTouchDp, w), Mathf.Max(MinTouchDp, h));
                LayoutTownLabel(m, h, center);
            }
        }

        /// <summary>Ancho de la viñeta (dp) al zoom <paramref name="zoom"/> de la vista Mundo (nivel de detalle, §9.5).</summary>
        public static float TownWidthDp(float zoom)
        {
            float t = Mathf.InverseLerp(WorldZoomMin, WorldZoomMax, zoom);
            return Mathf.Round(Mathf.Lerp(TownMinDp, TownMaxDp, t));
        }

        // Menos es antes: los pasados, las ciudades y los más cerca de la canoa ganan el sitio.
        private int TownRank(int i, int canoeLeg)
        {
            TownMarker m = _townMarkers[i];
            int rank = Math.Abs(m.Leg - canoeLeg) * 4;
            rank += m.Passed ? 0 : 200;
            rank += m.Data.Tier == RiverTownTier.City ? 0 : 1;
            return (rank * 64) + i;
        }

        // Normal del río en el punto del pueblo (local, dp), hacia el lado side (±1).
        private Vector2 BankNormal(RiverMap map, RiverPassingPoint point, float side)
        {
            RiverLine line = map.Lines[point.At.Line];
            float eps = Mathf.Max(1e-4f, 0.002f);
            line.PointAt(Mathf.Clamp01(point.At.S + eps), out float ax, out float ay);
            line.PointAt(Mathf.Clamp01(point.At.S - eps), out float bx, out float by);
            Vector2 d = MapSpace.DirectionToLocal(new Vector2(ax - bx, ay - by));
            if (d.sqrMagnitude < 1e-12f)
            {
                return new Vector2(0f, side);
            }

            d.Normalize();
            return new Vector2(-d.y, d.x) * side;
        }

        // El rótulo bajo la viñeta, si toca (§9.5) y no pisa otra viñeta, otro rótulo, una parada ni la canoa con su tripulación;
        // si debajo no cabe, encima de la viñeta (añadido de D-069 del 2026-11-08, observación 2); si tampoco, sin rótulo.
        private void LayoutTownLabel(TownMarker m, float h, Vector2 center)
        {
            bool on = m.WantsLabel && m.Label.text.Length > 0;
            if (on)
            {
                SizePill(m.Pill, m.Label);
                float dy = (h * 0.5f) + (m.Pill.sizeDelta.y * 0.5f) + 1f;
                on = false;
                for (int side = 0; side < 2 && !on; side++)
                {
                    m.Pill.anchoredPosition = new Vector2(0f, side == 0 ? -dy : dy);
                    Vector2 at = center + m.Pill.anchoredPosition;
                    var rect = new Rect(at.x - (m.Pill.sizeDelta.x * 0.5f), at.y - (m.Pill.sizeDelta.y * 0.5f), m.Pill.sizeDelta.x, m.Pill.sizeDelta.y);
                    on = !HitsCrew(rect);
                    for (int j = 0; j < _townPlaced.Count - 1 && on; j++)
                    {
                        on = !_townPlaced[j].Overlaps(rect); // la última es su propia viñeta
                    }

                    if (on)
                    {
                        _townPlaced.Add(rect);
                    }
                }
            }

            if (m.Pill.gameObject.activeSelf != on)
            {
                m.Pill.gameObject.SetActive(on);
            }
        }

        // La píldora a la medida de su texto (12 sp por la escala).
        private static void SizePill(RectTransform pill, TextMeshProUGUI label)
        {
            label.enableAutoSizing = false;
            label.fontSize = TownLabelSp * UiBuilder.TextScale;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            float textW = Mathf.Ceil(label.GetPreferredValues(label.text).x) + 2f; // la línea entera, sin partir ni cortar
            float textH = Mathf.Ceil(label.GetPreferredValues(label.text.IndexOf('\n') >= 0 ? label.text : "Ág").y); // M2-4: el rótulo con «Faltan N», en dos líneas
            pill.anchorMin = new Vector2(0.5f, 0.5f);
            pill.anchorMax = new Vector2(0.5f, 0.5f);
            pill.pivot = new Vector2(0.5f, 0.5f);
            pill.sizeDelta = new Vector2(textW + 12f, textH + 4f);
            UiBuilder.Place(label.rectTransform, 6f, 0f, textW + 1f, textH + 4f);
        }

        private static void SetTownActive(TownMarker m, bool on)
        {
            if (m.Root.gameObject.activeSelf != on)
            {
                m.Root.gameObject.SetActive(on);
            }
        }

        private void TownsLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (!_towns || _view != MapView.World)
            {
                return;
            }

            for (int i = 0; i < _townCount; i++)
            {
                TownMarker m = _townMarkers[i];
                if (m.Passed && TownInView(i))
                {
                    ids.Add("town_" + m.Data.Id);
                    rects.Add(m.Root);
                }
            }
        }

        // ============================================================================================ la canoa y los rótulos

        // Añadido de D-069 del 2026-11-08 (observación 2 de la prueba de la 1.1): las cajas que ningún rótulo ni viñeta pisa, con el
        // Mapa v2 en la vista Mundo: la de la canoa con Capi, el mástil y las banderas (_gearBox) y la del guía con el tucuxi a su
        // lado, si va a bordo con max_level (las de CollectObstacles: como queda la canoa al acabar el viaje).
        private void CollectCrewLabelBoxes()
        {
            _crewLabelBoxCount = 0;
            if (!_live || _view != MapView.World || !_hasCanoe)
            {
                return;
            }

            if (_gearBox.width > 0f)
            {
                _crewLabelBoxes[_crewLabelBoxCount++] = Grow(_gearBox, CrewLabelGapDp);
            }

            if (_guideBox.width > 0f)
            {
                Rect g = _guideBox;
                int max = _progress != null ? Mathf.Clamp(_progress.MaxLevel, 0, WorldTable.LastLevel) : 0;
                if (CrewRequested && RiverCrew.IsAboardCapped(CrewKind.Tucuxi, max, true))
                {
                    // El tucuxi nada TucuxiBesideRosa por detrás y por debajo de Rosa (su pivote, a 0,3 del alto de su caja).
                    var rosa = new Vector2(g.center.x, g.yMin + (0.3f * g.height));
                    Vector2 at = rosa + new Vector2(TucuxiBesideRosa.x * _gearFacing, TucuxiBesideRosa.y);
                    float tw = RiverCrew.TucuxiDp;
                    g = Union(g, new Rect(at.x - (tw * 0.5f), at.y - (tw * 0.5f), tw, tw));
                }

                _crewLabelBoxes[_crewLabelBoxCount++] = Grow(g, CrewLabelGapDp);
            }
        }

        // La caja r (local del contenido, dp, y hacia arriba) pisa la canoa con su tripulación.
        private bool HitsCrew(Rect r)
        {
            for (int k = 0; k < _crewLabelBoxCount; k++)
            {
                if (r.Overlaps(_crewLabelBoxes[k]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Cajas de la canoa con su tripulación que esquivan rótulos y viñetas (añadido de D-069 del 2026-11-08; tests).</summary>
        internal int CrewLabelBoxCount => _crewLabelBoxCount;

        /// <summary>La caja <paramref name="k"/> de <see cref="CrewLabelBoxCount"/>, con su hueco, en coordenadas locales del contenido.</summary>
        internal Rect CrewLabelBox(int k)
        {
            return _crewLabelBoxes[k];
        }

        /// <summary>Número de pueblos de paso de este Mapa (tests).</summary>
        internal int TownMarkerCount => _townCount;

        /// <summary>Caja del rótulo de la parada <paramref name="n"/> en coordenadas locales del contenido; vacía si no sale (tests).</summary>
        internal Rect StopLabelLocalRect(int n)
        {
            int i = StopMarkerIndexOf(n);
            if (i < 0 || i >= _stopLabels.Count || !_stopLabels[i].Pill.gameObject.activeSelf || !_markers[i].Root.gameObject.activeSelf)
            {
                return default;
            }

            return PillRect(_markers[i].Root.anchoredPosition, _stopLabels[i].Pill);
        }

        /// <summary>Caja del rótulo del pueblo <paramref name="i"/> en coordenadas locales del contenido; vacía si no sale (tests).</summary>
        internal Rect TownLabelLocalRect(int i)
        {
            TownMarker m = _townMarkers[i];
            if (!m.Root.gameObject.activeSelf || !m.Pill.gameObject.activeSelf)
            {
                return default;
            }

            return PillRect(m.Root.anchoredPosition, m.Pill);
        }

        /// <summary>Caja de la viñeta del pueblo <paramref name="i"/> en coordenadas locales del contenido; vacía si no sale (tests).</summary>
        internal Rect TownVignetteLocalRect(int i)
        {
            TownMarker m = _townMarkers[i];
            if (!m.Root.gameObject.activeSelf)
            {
                return default;
            }

            Vector2 s = m.Vignette.rectTransform.sizeDelta;
            Vector2 c = m.Root.anchoredPosition;
            return new Rect(c.x - (s.x * 0.5f), c.y - (s.y * 0.5f), s.x, s.y);
        }

        private static Rect PillRect(Vector2 parent, RectTransform pill)
        {
            Vector2 s = pill.sizeDelta;
            Vector2 at = parent + pill.anchoredPosition;
            return new Rect(at.x - (pill.pivot.x * s.x), at.y - (pill.pivot.y * s.y), s.x, s.y);
        }

        // El rótulo de la parada i: debajo de su mini cuia, como siempre; si ahí pisa la canoa con su tripulación, otra mini cuia u
        // otro rótulo de parada, encima, a la derecha o a la izquierda, el primero libre (añadido de D-069 del 2026-11-08,
        // observaciones 2 y 4). Una parada en abanico va sin rótulo (se pisarían), salvo la siguiente, que lleva «Faltan N» y sale
        // si le queda un sitio libre (así no hace falta la placa de arriba a la izquierda). Si ninguno está libre, debajo, como antes
        // (y la de un abanico, sin rótulo). True si sale.
        private bool LayoutStopLabel(int i)
        {
            StopLabel s = _stopLabels[i];
            StopMarker sm = _markers[i];
            bool on = s.Label.text.Length > 0 && (!sm.Fanned || sm.N == RemainingStopN());
            if (on && !PlaceStopLabel(i) && sm.Fanned)
            {
                on = false;
            }

            if (s.Pill.gameObject.activeSelf != on)
            {
                s.Pill.gameObject.SetActive(on);
            }

            if (on)
            {
                _stopLabelPlaced.Add(PillRect(sm.Root.anchoredPosition, s.Pill));
            }

            return on;
        }

        private bool _labelFitView;
        private Rect _labelView;
        private Rect _labelOld;

        // Añadido de D-069 del 2026-11-08 (observación 4): con el encuadre ya decidido (ApplyView), el rótulo de la parada siguiente,
        // que lleva «Faltan N», se cambia al primero de sus sitios que quede entero a la vista si el suyo se sale (una parada junto al
        // borde del Mapa). Si ninguno cabe, se queda donde estaba. No se mueve al arrastrar: solo al abrir y al cambiar de vista.
        private void RemainingLabelFitView()
        {
            int n = RemainingStopN();
            int i = n >= 0 ? StopMarkerIndexOf(n) : -1;
            bool valid = i >= 0 && i < _stopLabels.Count && i < _markerCount;
            if (!_live || _view != MapView.World || !valid || !_stopLabels[i].Pill.gameObject.activeSelf)
            {
                if (ClearRemainingOffView(valid ? i : -1))
                {
                    SyncRemainingPlate();
                }

                return;
            }

            RectTransform pill = _stopLabels[i].Pill;
            Vector2 c = _markers[i].Root.anchoredPosition;
            Rect old = PillRect(c, pill);
            Rect placedRect = old; // su caja en _stopLabelPlaced y _townPlaced, tal como la dejó TownsSpace
            _labelView = new Rect(2f, 2f, _viewW - 4f, _viewH - 4f);
            if (ClearRemainingOffView(i))
            {
                old = PillRect(c, pill); // venía de la placa (otro encuadre de esta apertura): con «Faltan N» otra vez, ¿cabe?
            }

            if (Inside(InView(old, _offset), _labelView) && !HitsPlate(old)) // las placas se encajan tras el encuadre (PlatesFitView)
            {
                SyncRemainingPlate();
                return;
            }

            Vector2 before = pill.anchoredPosition;
            _labelOld = placedRect;
            int own = _stopLabelPlaced.IndexOf(placedRect);
            if (own >= 0)
            {
                _stopLabelPlaced.RemoveAt(own);
            }

            _labelFitView = true;
            bool placed = PlaceStopLabel(i);
            _labelFitView = false;
            if (!placed)
            {
                pill.anchoredPosition = before;
            }

            _remainingOffView = !placed; // ni así se ve entero (la parada queda fuera del encuadre): la placa de respaldo
            if (_remainingOffView)
            {
                // «Faltan N» va entonces en la placa: el rótulo, solo con el nombre, para no decirlo dos veces.
                TextMeshProUGUI label = _stopLabels[i].Label;
                label.text = UiText.StopName(_markers[i].Stop);
                SizePill(pill, label);
            }

            _stopLabelPlaced.Add(PillRect(c, pill));
            SyncRemainingPlate();
        }

        /// <summary>Margen del encuadre de la parada siguiente con su rótulo y la canoa (dp) [P] (añadido de D-069 del 2026-11-08).</summary>
        public const float RemainingFrameMarginDp = 8f;

        // Añadido de D-069 del 2026-11-08 (observación 4): con el Mapa v2, el encuadre de apertura de la vista Mundo se corre lo justo
        // para que se vean enteras la mini cuia de la parada siguiente con su rótulo («Faltan N») y la canoa con su equipo, debajo de
        // «Andes ↑»; si las dos no caben a la vez, no se corre (y «Faltan N» sale en la placa de respaldo). Tras MarkersFrame.
        private void FrameRemaining(ref Vector2 offset)
        {
            int n = RemainingStopN();
            int i = n >= 0 ? StopMarkerIndexOf(n) : -1;
            if (!_live || _view != MapView.World || i < 0 || i >= _markerCount || i >= _stopLabels.Count)
            {
                return;
            }

            Vector2 c = _markers[i].Root.anchoredPosition;
            Rect want = CuiaBox(c);
            if (_stopLabels[i].Pill.gameObject.activeSelf)
            {
                want = Union(want, PillRect(c, _stopLabels[i].Pill));
            }

            if (_gearBox.width > 0f)
            {
                want = Union(want, _gearBox);
            }

            float m = RemainingFrameMarginDp;
            float top = m;
            Rect andes = FixedViewRect(AndesLabel);
            if (andes.width > 0f)
            {
                top = Mathf.Max(top, andes.yMax + 4f);
            }

            Rect v = InView(want, offset);
            if (v.width > _viewW - (2f * m) || v.height > _viewH - top - m)
            {
                return;
            }

            float dx = v.xMin < m ? m - v.xMin : (v.xMax > _viewW - m ? _viewW - m - v.xMax : 0f);
            float dy = v.yMin < top ? top - v.yMin : (v.yMax > _viewH - m ? _viewH - m - v.yMax : 0f);
            offset += new Vector2(dx, dy);
        }

        private float _closeUpLabelAlpha = 1f;

        /// <summary>Opacidad de los rótulos del Mapa por el primer plano de la tripulación (1 sin él; tests).</summary>
        internal float CloseUpLabelAlpha => _closeUpLabelAlpha;

        // Añadido de D-069 del 2026-11-08 (observación 8): en el primer plano de la tripulación, los rótulos de los pueblos y de las
        // paradas y la píldora del Premio del mundo se apagan (a ×2 taparían la escena) y vuelven con él. Solo escribe si cambia; sin
        // asignaciones.
        private void CloseUpLabels()
        {
            float a = 1f - Mathf.Clamp01((_closeZoom - 1f) / (CamCloseUpZoom - 1f));
            if (Mathf.Abs(a - _closeUpLabelAlpha) < 0.01f && (a < 1f || _closeUpLabelAlpha == 1f))
            {
                return;
            }

            _closeUpLabelAlpha = a;
            for (int i = 0; i < _stopLabels.Count; i++)
            {
                SetPillAlpha(_stopLabels[i].Pill, a);
            }

            for (int i = 0; i < _townMarkers.Count; i++)
            {
                SetPillAlpha(_townMarkers[i].Pill, a);
            }

            if (_prizePill != null)
            {
                SetPillAlpha(_prizePill, a);
            }

            if (_buoysAhead != null)
            {
                _buoysAhead.canvasRenderer.SetAlpha(a); // las boyas por ganar van encima de Rosa y del tucuxi
            }
        }

        // La píldora y lo que lleva dentro (su texto y, en la del premio, la mini cuia), a la opacidad a.
        private static void SetPillAlpha(RectTransform pill, float a)
        {
            CanvasRenderer? own = pill.GetComponent<CanvasRenderer>();
            if (own != null)
            {
                own.SetAlpha(a);
            }

            for (int c = 0; c < pill.childCount; c++)
            {
                CanvasRenderer? r = pill.GetChild(c).GetComponent<CanvasRenderer>();
                if (r != null)
                {
                    r.SetAlpha(a);
                }
            }
        }

        // Baja la placa de respaldo de RemainingLabelFitView: el rótulo de la parada i (si i ≥ 0 y lleva texto) vuelve a llevar
        // «Faltan N». True si estaba puesta.
        private bool ClearRemainingOffView(int i)
        {
            if (!_remainingOffView)
            {
                return false;
            }

            _remainingOffView = false;
            if (i >= 0 && _stopLabels[i].Label.text.Length > 0)
            {
                TextMeshProUGUI full = _stopLabels[i].Label;
                full.text = UiText.MapStopLabelRemaining(UiText.StopName(_markers[i].Stop), RiverJourney.LevelsLeft(_playLevel));
                SizePill(_stopLabels[i].Pill, full);
            }

            return true;
        }

        // El rótulo de la parada siguiente no cabe entero a la vista con el encuadre de apertura (RemainingLabelFitView).
        private bool _remainingOffView;

        /// <summary>
        /// El rótulo de la parada siguiente no cabe entero a la vista con el encuadre de apertura y «Faltan N» sale en la placa de
        /// respaldo (añadido de D-069 del 2026-11-08; tests).
        /// </summary>
        internal bool RemainingLabelOffView => _remainingOffView;

        // Sin los pueblos, solo el rótulo de la parada siguiente, con las mismas reglas que con ellos.
        private void RemainingLabelAlone()
        {
            int n = RemainingStopN();
            int i = n >= 0 ? StopMarkerIndexOf(n) : -1;
            if (i < 0 || i >= _stopLabels.Count || i >= _markerCount || _view != MapView.World)
            {
                return;
            }

            LayoutStopLabel(i);
        }

        // Prueba los sitios del rótulo de la parada i en orden y lo deja en el primero libre (true) o debajo (false).
        private bool PlaceStopLabel(int i)
        {
            StopLabel s = _stopLabels[i];
            Vector2 c = _markers[i].Root.anchoredPosition;
            Vector2 size = s.Pill.sizeDelta;
            float cw = ProceduralArt.MiniCuiaWidthDp;
            float ch = ProceduralArt.MiniCuiaHeightDp;
            bool prizeHere = _prizeMarker == i && _prizePill != null && _prizePill.gameObject.activeSelf;
            var below = new Vector2(0f, -(ch * 0.5f) - (size.y * 0.5f) - 2f);
            float side = (cw * 0.5f) + BasketBadgeDp + (size.x * 0.5f) + 2f; // más allá de la cestita de la esquina
            for (int k = 0; k < StopLabelSpots; k++)
            {
                // De cerca a lejos: debajo, encima, a la derecha y a la izquierda; y después lo mismo un paso más allá (en un abanico,
                // las vecinas ocupan los cuatro sitios pegados).
                int dir = k % 4;
                int ring = k / 4;
                float stepY = ring * (size.y + 4f);
                float stepX = ring * (cw + 4f);
                Vector2 off;
                if (dir == 0)
                {
                    off = below - new Vector2(0f, stepY);
                }
                else if (dir == 1)
                {
                    if (prizeHere)
                    {
                        continue; // encima va la píldora del Premio del mundo
                    }

                    off = new Vector2(0f, (ch * 0.5f) + (BasketBadgeDp * 0.5f) + (size.y * 0.5f) + 2f + stepY); // por encima de la cestita
                }
                else
                {
                    off = new Vector2(dir == 2 ? side + stepX : -(side + stepX), 0f);
                }

                if (_labelFitView)
                {
                    off = ShiftIntoView(c, off, size, dir < 2, dir < 2 ? (size.x + cw) * 0.5f - 4f : (size.y + ch) * 0.5f - 4f);
                }

                var rect = new Rect(c.x + off.x - (size.x * 0.5f), c.y + off.y - (size.y * 0.5f), size.x, size.y);
                if (StopLabelFree(i, rect))
                {
                    s.Pill.anchoredPosition = off;
                    return true;
                }
            }

            s.Pill.anchoredPosition = below;
            return false;
        }

        // Con el encuadre decidido, corre el rótulo a lo largo de su mini cuia (en x si va encima o debajo; en y si va a un lado), como
        // mucho reach dp para que siga junto a ella, hasta que quede dentro de _labelView. Sin asignaciones.
        private Vector2 ShiftIntoView(Vector2 c, Vector2 off, Vector2 size, bool alongX, float reach)
        {
            if (alongX)
            {
                float left = _offset.x + c.x + off.x - (size.x * 0.5f);
                float d = 0f;
                if (left < _labelView.xMin)
                {
                    d = _labelView.xMin - left;
                }
                else if (left + size.x > _labelView.xMax)
                {
                    d = _labelView.xMax - (left + size.x);
                }

                off.x += Mathf.Clamp(d, -reach, reach);
            }
            else
            {
                float top = _offset.y - (c.y + off.y + (size.y * 0.5f)); // en la vista, y hacia abajo
                float d = 0f;
                if (top < _labelView.yMin)
                {
                    d = top - _labelView.yMin; // hay que bajarlo: y local hacia abajo
                }
                else if (top + size.y > _labelView.yMax)
                {
                    d = top + size.y - _labelView.yMax;
                }

                off.y += Mathf.Clamp(d, -reach, reach);
            }

            return off;
        }

        private bool StopLabelFree(int self, Rect rect)
        {
            if (HitsCrew(rect))
            {
                return false;
            }

            if (_labelFitView)
            {
                // Con el encuadre ya decidido: entero a la vista, sin pisar los pueblos ni sus rótulos (salvo su propio sitio de antes).
                if (!Inside(InView(rect, _offset), _labelView))
                {
                    return false;
                }

                for (int j = 0; j < _townPlaced.Count; j++)
                {
                    if (_townPlaced[j] != _labelOld && rect.Overlaps(_townPlaced[j]))
                    {
                        return false;
                    }
                }
            }

            for (int j = 0; j < _markerCount; j++)
            {
                if (j != self && _markers[j].Root.gameObject.activeSelf && rect.Overlaps(CuiaBox(_markers[j].Root.anchoredPosition)))
                {
                    return false;
                }
            }

            for (int j = 0; j < _stopLabelPlaced.Count; j++)
            {
                if (rect.Overlaps(_stopLabelPlaced[j]))
                {
                    return false;
                }
            }

            return !HitsPlate(rect);
        }

        // La caja r pisa la placa de un mundo futuro que sale («Mundo 4 · Trapecio · Nivel 81», junto a la parada que lo abre).
        private bool HitsPlate(Rect r)
        {
            for (int k = 0; k < _silhouettes.Count; k++)
            {
                Silhouette sil = _silhouettes[k];
                if (sil.Root.gameObject.activeSelf && sil.Plate.enabled && r.Overlaps(PlateBox(sil.Root)))
                {
                    return true;
                }
            }

            return false;
        }

        // Añadido de D-069 del 2026-11-08: la caja r (local) de la píldora del Premio del mundo pisa una viñeta o un rótulo de pueblo o
        // de parada, la canoa con su tripulación o una placa de mundo. Sin asignaciones.
        private bool PrizeSpotTaken(Rect r)
        {
            if (HitsCrew(r) || HitsPlate(r))
            {
                return true;
            }

            if (_towns && _view == MapView.World)
            {
                for (int j = 0; j < _townPlaced.Count; j++)
                {
                    if (r.Overlaps(_townPlaced[j]))
                    {
                        return true;
                    }
                }
            }

            for (int j = 0; j < _stopLabelPlaced.Count; j++)
            {
                if (r.Overlaps(_stopLabelPlaced[j]))
                {
                    return true;
                }
            }

            return false;
        }

        // ============================================================================================ rótulos de las paradas

        // La última parada alcanzada y la siguiente llevan su nombre bajo la mini cuia (§9.5): una píldora hija de su marcador.
        // Con el Mapa v2 (respuesta del dueño a la pregunta 2 de M2-3, en M2-4), el rótulo de la parada siguiente lleva «Faltan N»
        // dentro (map_stop_label_remaining), como en la maqueta; sin los pueblos (ui_map_towns apagada), solo ese rótulo.
        private void BuildStopLabels(int max)
        {
            int remain = RemainingStopN();
            bool nextOnly = !_towns;
            for (int i = 0; i < _markerCount; i++)
            {
                StopMarker m = _markers[i];
                while (_stopLabels.Count <= i)
                {
                    Image pill = UiBuilder.Sliced("StopLabel", m.Root, _art.RoundedRect, new Color(Palette.WoodDark.r, Palette.WoodDark.g, Palette.WoodDark.b, 0.88f));
                    pill.raycastTarget = false;
                    TextMeshProUGUI label = UiBuilder.Label("Text", pill.transform, TextStyle.PlainBold, string.Empty, TownLabelSp, Palette.Cream, TextAlignmentOptions.Center);
                    label.raycastTarget = false;
                    _stopLabels.Add(new StopLabel(pill.rectTransform, label));
                }

                StopLabel s = _stopLabels[i];
                if (s.Pill.parent != m.Root)
                {
                    s.Pill.SetParent(m.Root, false);
                }

                bool shown = nextOnly ? m.N == remain : RiverTowns.StopLabelShown(m.N, max);
                string name = shown ? UiText.StopName(m.Stop) : string.Empty;
                if (name.Length > 0 && m.N == remain)
                {
                    name = UiText.MapStopLabelRemaining(name, RiverJourney.LevelsLeft(_playLevel));
                }

                s.Label.text = name;
                bool on = name.Length > 0;
                if (s.Pill.gameObject.activeSelf != on)
                {
                    s.Pill.gameObject.SetActive(on);
                }

                if (on)
                {
                    SizePill(s.Pill, s.Label);
                    s.Pill.anchoredPosition = new Vector2(0f, -(ProceduralArt.MiniCuiaHeightDp * 0.5f) - (s.Pill.sizeDelta.y * 0.5f) - 2f);
                }
            }

            for (int i = _markerCount; i < _stopLabels.Count; i++)
            {
                _stopLabels[i].Pill.gameObject.SetActive(false);
            }

            SyncRemainingPlate();
        }

        private void HideStopLabels()
        {
            for (int i = 0; i < _stopLabels.Count; i++)
            {
                if (_stopLabels[i].Pill.gameObject.activeSelf)
                {
                    _stopLabels[i].Pill.gameObject.SetActive(false);
                }
            }
        }

        private sealed class TownMarker
        {
            public TownMarker(RectTransform root, Button button, Image vignette, RectTransform pill, TextMeshProUGUI label)
            {
                Root = root;
                Button = button;
                Vignette = vignette;
                Pill = pill;
                Label = label;
                Data = null!;
            }

            public RectTransform Root { get; }

            public Button Button { get; }

            public Image Vignette { get; }

            public RectTransform Pill { get; }

            public TextMeshProUGUI Label { get; }

            public RiverPassingPoint Data { get; set; }

            public int Leg { get; set; }

            public bool Passed { get; set; }

            public bool WantsLabel { get; set; }

            public Vector2 MapPoint { get; set; }
        }

        private sealed class StopLabel
        {
            public StopLabel(RectTransform pill, TextMeshProUGUI label)
            {
                Pill = pill;
                Label = label;
            }

            public RectTransform Pill { get; }

            public TextMeshProUGUI Label { get; }
        }
    }
}
