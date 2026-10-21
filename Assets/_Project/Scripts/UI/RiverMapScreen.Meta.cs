#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Lo que B14 añade al Mapa del río (06 §4.5; tarea B14 de la tanda 5), parcial de <see cref="RiverMapScreen"/>. Todo sale de
    /// <c>max_level</c>, del catálogo de paradas, de la geometría y de las reglas de la Canasta y de las skins, sin estado nuevo:
    /// <list type="bullet">
    /// <item><description><b>La Canasta de cada parada</b> («cada parada futura enseña el contenido de su Canasta, y las cobradas llevan
    /// una marca»): en la mini cuia de una parada que falta, una cestita (<see cref="ProceduralArt.EmptyBasket"/>, provisional) en la
    /// esquina; tocarla abre su tarjeta con el contenido (<see cref="StopCard.Basket"/>). La parada <c>k</c> se alcanza al ganar el
    /// <c>10k</c>, así que su Canasta es la de ese nivel (<see cref="BasketFor"/>); en las alcanzadas cuya Canasta ya se cobró, el ✓
    /// en el mismo sitio. Sin Canasta (la parada 0, o sin reglas), nada.</description></item>
    /// <item><description><b>«Premio del mundo · faltan N»</b> (cierre de mundo: «con baliza y la skin del mundo como Premio del mundo
    /// · faltan N, sin precio»): bajo la mini cuia del cierre de un mundo con skin (<see cref="SkinRewards"/>: 2, 3 y 4) mientras no se
    /// ha ganado su último nivel, una píldora con la mini cuia teñida por la skin y <c>map_world_prize</c>; N son los niveles que
    /// faltan (último nivel del mundo − <c>max_level</c>). También en su tarjeta.</description></item>
    /// <item><description><b>Los puntos de paso</b> («puntos de paso con nombre (Óbidos, Parintins, Tabatinga…), que al tocarlos
    /// cuentan su dato»): los de los tramos ya recorridos (<c>leg.via</c> de las paradas alcanzadas), con un punto crema en su sitio
    /// del río en la vista Mundo y un área táctil de 48 dp debajo de las mini cuias; tocar uno abre su tarjeta con su nombre y, si
    /// tiene texto, su dato y su fuente (<see cref="StopCard.ShowPassing"/>).</description></item>
    /// </list>
    /// Las piezas se crean una vez (al crecer la reserva al abrir) y al cambiar de vista solo se recolocan: sin asignaciones con el
    /// Mapa quieto.
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Lado de la marca de la Canasta (la cestita o el ✓) en la esquina de una mini cuia (dp) [P].</summary>
        public const float BasketBadgeDp = 16f;

        /// <summary>Lado del punto de un punto de paso (dp) [P].</summary>
        public const float PassingDotDp = 9f;

        /// <summary>Texto de la píldora del Premio del mundo (sp; 12, el mínimo legible).</summary>
        public const float PrizePillSp = 12f;

        /// <summary>
        /// La píldora del Premio del mundo empieza este tanto por encima del centro de la mini cuia (dp) [P]: encima, en tierra, y no
        /// debajo, donde la tapaba la canoa que navega por el tramo (revisión de las capturas de B14).
        /// </summary>
        public const float PrizePillAboveDp = (ProceduralArt.MiniCuiaHeightDp * 0.5f) + 8f;

        /// <summary>Crema del punto de un punto de paso.</summary>
        public static readonly Color PassingDotColor = new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.95f);

        private readonly List<Image> _basketBadges = new List<Image>();
        private readonly List<GlyphGraphic> _claimedBadges = new List<GlyphGraphic>();
        private readonly List<PassingMarker> _passingMarkers = new List<PassingMarker>();
        private RectTransform? _prizePill;
        private Image? _prizePillCuia;
        private TextMeshProUGUI? _prizePillLabel;
        private RectTransform? _passingLayer;
        private int _prizeMarker = -1;
        private int _prizeLeftShown = -1;
        private int _passingCount;

        /// <summary>
        /// Contenido de la Canasta del nivel <c>10k</c> (lo da <c>GameBootstrap</c> con las reglas de B1a: <see cref="BasketRules.For"/>
        /// con <c>live_unlock_level_daily</c> y <c>eco_basket_*</c>). Null = sin Canastas en el Mapa.
        /// </summary>
        public Func<int, BasketContents>? BasketFor { get; set; }

        /// <summary>Puntos de paso a la vista (los de los tramos ya recorridos).</summary>
        public int PassingCount => _passingCount;

        /// <summary>El punto de paso <paramref name="i"/>.</summary>
        public RiverPassingPoint PassingAt(int i)
        {
            return _passingMarkers[i].Data;
        }

        /// <summary>El punto de paso <paramref name="i"/> se ve (en la vista Mundo).</summary>
        public bool PassingShown(int i)
        {
            return i >= 0 && i < _passingCount && _passingMarkers[i].Root.gameObject.activeInHierarchy;
        }

        /// <summary>Centro del punto de paso <paramref name="i"/> en coordenadas locales del contenido (dp, y hacia arriba).</summary>
        public Vector2 PassingLocal(int i)
        {
            return _passingMarkers[i].Root.anchoredPosition;
        }

        /// <summary>La mini cuia <paramref name="i"/> lleva la cestita de su Canasta (parada que falta).</summary>
        public bool StopMarkerBasketShown(int i)
        {
            return i < _basketBadges.Count && _basketBadges[i].gameObject.activeSelf;
        }

        /// <summary>La mini cuia <paramref name="i"/> lleva el ✓ de su Canasta cobrada.</summary>
        public bool StopMarkerClaimedShown(int i)
        {
            return i < _claimedBadges.Count && _claimedBadges[i].gameObject.activeSelf;
        }

        /// <summary>Mini cuia (índice) que lleva el Premio del mundo, o −1.</summary>
        public int PrizeMarker => _prizePill != null && _prizePill.gameObject.activeSelf ? _prizeMarker : -1;

        /// <summary>Texto de la píldora del Premio del mundo («» sin ella).</summary>
        public string PrizeText => PrizeMarker >= 0 ? _prizePillLabel!.text : string.Empty;

        /// <summary>Caja de la píldora del Premio del mundo (tests; null sin ella).</summary>
        internal RectTransform? PrizePill => PrizeMarker >= 0 ? _prizePill : null;

        /// <summary>
        /// La Canasta con que se llega a la parada <paramref name="stop"/>: la del nivel <c>10k</c> (<see cref="BasketFor"/>), o
        /// <see cref="BasketContents.None"/> en la parada 0 o sin reglas.
        /// </summary>
        public BasketContents BasketOf(RiverStop stop)
        {
            int level = stop.N * RiverStopCatalog.LevelsPerStop;
            Func<int, BasketContents>? basket = BasketFor;
            return basket == null || level < 1 ? BasketContents.None : basket(level);
        }

        /// <summary>
        /// Niveles que faltan para el Premio del mundo de la parada <paramref name="stop"/> con <paramref name="maxLevel"/> niveles
        /// ganados, o −1 si no lo lleva (no es un cierre de mundo con skin, o ya se ganó).
        /// </summary>
        public static int PrizeLevelsLeft(RiverStop stop, int maxLevel)
        {
            if (stop == null || stop.Kind != StopKind.Close || !SkinRewards.TryForWorld(stop.World, out SkinMilestone skin) || maxLevel >= skin.Level)
            {
                return -1;
            }

            return skin.Level - Mathf.Max(0, maxLevel);
        }

        /// <summary>Toca el punto de paso <paramref name="i"/> (tests y <c>adb</c>: lo mismo que su área táctil).</summary>
        public bool TapPassing(int i)
        {
            if (!Visible || !PassingShown(i))
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
            card.ShowPassing(_passingMarkers[i].Data);
            EmitLayout();
            return true;
        }

        // ============================================================================================ marcas de las mini cuias

        // Tras pintar las mini cuias con max (ShowMarkersAt): la cestita en las que faltan, el ✓ en las alcanzadas con su Canasta
        // cobrada, y el Premio del mundo bajo el cierre del mundo con skin que aún no se ganó.
        private void MetaShowMarkers(int max)
        {
            _prizeMarker = -1;
            int prizeLeft = -1;
            for (int i = 0; i < _markerCount; i++)
            {
                StopMarker m = _markers[i];
                EnsureBadges(i, m.Root);
                bool hasBasket = !BasketOf(m.Stop).IsNone;
                bool reached = max >= m.N * RiverStopCatalog.LevelsPerStop;
                SetActive(_basketBadges[i], hasBasket && !reached);
                SetActive(_claimedBadges[i], hasBasket && reached);
                int left = PrizeLevelsLeft(m.Stop, max);
                if (left >= 0 && _prizeMarker < 0)
                {
                    _prizeMarker = i;
                    prizeLeft = left;
                }
            }

            for (int i = _markerCount; i < _basketBadges.Count; i++)
            {
                SetActive(_basketBadges[i], false);
                SetActive(_claimedBadges[i], false);
            }

            if (_prizeMarker < 0)
            {
                if (_prizePill != null && _prizePill.gameObject.activeSelf)
                {
                    _prizePill.gameObject.SetActive(false);
                }

                return;
            }

            EnsurePrizePill();
            StopMarker pm = _markers[_prizeMarker];
            SkinRewards.TryForWorld(pm.Stop.World, out SkinMilestone milestone);
            CuiaSkin skin = CuiaSkin.For(milestone.SkinId);
            _prizePillCuia!.color = skin.PreviewTint;
            if (prizeLeft != _prizeLeftShown || _prizePill!.parent != pm.Root)
            {
                // Solo al cambiar N o de parada: «ponerse al día» repinta las mini cuias en cada boya sin asignar memoria.
                _prizeLeftShown = prizeLeft;
                _prizePillLabel!.text = UiText.MapWorldPrize(prizeLeft);
                _prizePill!.SetParent(pm.Root, false);
                LayoutPrizePill();
            }

            SetActive(_prizePill!, _prizePillLabel!.text.Length > 0);
        }

        // Tras colocar las mini cuias (PlaceStopMarkers): la marca de la Canasta en la esquina de arriba del lado contrario al de la
        // baliza, y la píldora del premio centrada bajo su mini cuia.
        private void MetaPlaceMarkers()
        {
            for (int i = 0; i < _markerCount && i < _basketBadges.Count; i++)
            {
                StopMarker m = _markers[i];
                float side = m.Beacon.gameObject.activeSelf && !m.BeaconLeft ? -1f : 1f;
                var at = new Vector2(side * ((ProceduralArt.MiniCuiaWidthDp * 0.5f) + 2f), (ProceduralArt.MiniCuiaHeightDp * 0.5f) - 2f);
                _basketBadges[i].rectTransform.anchoredPosition = at;
                _claimedBadges[i].rectTransform.anchoredPosition = at;
            }

            PlacePrizePill();
        }

        // La píldora del premio, sobre su mini cuia y hacia el centro de la vista (revisión de las capturas de B14: centrada, en una
        // mini cuia junto al borde, salía cortada por el marco): con la mini cuia en la mitad izquierda de la vista, la píldora
        // empieza en su borde izquierdo y crece a la derecha; en la derecha, al revés.
        private void PlacePrizePill()
        {
            if (_prizePill == null || !_prizePill.gameObject.activeSelf || _prizeMarker < 0 || _prizeMarker >= _markerCount)
            {
                return;
            }

            StopMarker m = _markers[_prizeMarker];
            // Contra el visor, en mundo: el encuadre (MarkersFrame) corre la vista después de colocar las mini cuias, así que se
            // vuelve a mirar en cada Tick (sin asignar memoria; solo escribe si cambia de lado).
            bool left = _viewport.InverseTransformPoint(m.Root.position).x < _viewport.rect.center.x;
            float edge = ProceduralArt.MiniCuiaWidthDp * 0.5f;
            var pivot = new Vector2(left ? 0f : 1f, 0f);
            if (_prizePill.pivot != pivot)
            {
                _prizePill.pivot = pivot;
                _prizePill.anchoredPosition = new Vector2(left ? -edge : edge, PrizePillAboveDp);
            }
            else if (_prizePill.anchoredPosition.y != PrizePillAboveDp)
            {
                _prizePill.anchoredPosition = new Vector2(left ? -edge : edge, PrizePillAboveDp);
            }
        }

        private void EnsureBadges(int i, RectTransform root)
        {
            while (_basketBadges.Count <= i)
            {
                RectTransform parent = _markers[_basketBadges.Count].Root;
                Image basket = UiBuilder.Image("BasketBadge", parent, _art.EmptyBasket, Color.white);
                basket.preserveAspect = true;
                basket.raycastTarget = false;
                CenterPiece(basket.rectTransform, BasketBadgeDp);
                basket.gameObject.SetActive(false);
                GlyphGraphic check = GlyphGraphic.Create("ClaimedBadge", parent, GlyphKind.Check);
                check.raycastTarget = false;
                CenterPiece(check.rectTransform, BasketBadgeDp);
                check.gameObject.SetActive(false);
                _basketBadges.Add(basket);
                _claimedBadges.Add(check);
            }

            // La reserva sigue a la mini cuia aunque se reutilice para otra parada (BuildStopMarkers reasigna las raíces en orden).
            if (_basketBadges[i].transform.parent != root)
            {
                _basketBadges[i].transform.SetParent(root, false);
                _claimedBadges[i].transform.SetParent(root, false);
            }
        }

        private void EnsurePrizePill()
        {
            if (_prizePill != null)
            {
                return;
            }

            Image pill = UiBuilder.Sliced("PrizePill", _markerLayer!, _art.RoundedRect, new Color(Palette.WoodDark.r, Palette.WoodDark.g, Palette.WoodDark.b, 0.88f));
            pill.raycastTarget = false;
            _prizePill = pill.rectTransform;
            _prizePill.anchorMin = new Vector2(0.5f, 0.5f);
            _prizePill.anchorMax = new Vector2(0.5f, 0.5f);
            _prizePill.pivot = new Vector2(0.5f, 1f);
            _prizePillCuia = UiBuilder.Image("Cuia", _prizePill, _art.MapMiniCuia, Color.white);
            _prizePillCuia.preserveAspect = true;
            _prizePillCuia.raycastTarget = false;
            _prizePillLabel = UiBuilder.Label("Label", _prizePill, TextStyle.PlainBold, string.Empty, PrizePillSp, Palette.Cream, TextAlignmentOptions.MidlineLeft);
            _prizePillLabel.raycastTarget = false;
            _prizePill.gameObject.SetActive(false);
        }

        // La píldora a la medida del texto: la mini cuia (10 × 16 dp) y la línea a 12 sp por la escala.
        private void LayoutPrizePill()
        {
            TextMeshProUGUI label = _prizePillLabel!;
            label.enableAutoSizing = false;
            label.fontSize = PrizePillSp * UiBuilder.TextScale;
            float textW = Mathf.Ceil(UiBuilder.MinTextWidth(label));
            float textH = Mathf.Ceil(label.GetPreferredValues("Ág").y);
            const float cuiaW = ProceduralArt.MiniCuiaWidthDp * 0.5f;
            const float cuiaH = ProceduralArt.MiniCuiaHeightDp * 0.5f;
            float h = Mathf.Max(cuiaH, textH) + 6f;
            float w = 6f + cuiaW + 4f + textW + 8f;
            _prizePill!.sizeDelta = new Vector2(w, h);
            PlacePrizePill();
            UiBuilder.Place(_prizePillCuia!.rectTransform, 6f, (h - cuiaH) * 0.5f, cuiaW, cuiaH);
            UiBuilder.Place(label.rectTransform, 6f + cuiaW + 4f, 0f, textW + 2f, h);
        }

        private static void CenterPiece(RectTransform rt, float size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
        }

        private static void SetActive(Component c, bool on)
        {
            if (c.gameObject.activeSelf != on)
            {
                c.gameObject.SetActive(on);
            }
        }

        // ============================================================================================ puntos de paso

        // Los puntos de paso de los tramos recorridos con max (la vía de cada parada alcanzada, de los mundos abiertos), sin repetir.
        private void BuildPassing(int max)
        {
            RiverMap? map = _map;
            RiverRoutes? routes = _routes;
            RiverStopCatalog? stops = _stops;
            _passingCount = 0;
            if (map == null || routes == null || stops == null || _markerLayer == null)
            {
                return;
            }

            if (_passingLayer == null)
            {
                _passingLayer = UiBuilder.Rect("Passing", _dynamicLayer);
                FillTopLeft(_passingLayer);
            }

            // Debajo de las mini cuias: ellas ganan el toque donde se tocan las dos áreas.
            _passingLayer.SetSiblingIndex(_markerLayer.GetSiblingIndex());
            for (int n = 1; n < routes.Count && n < stops.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                RiverStopRoute? route = routes.At(n);
                if (stop == null || route == null || stop.World > _world || !StopReached(stop, max))
                {
                    continue;
                }

                for (int v = 0; v < route.Via.Count; v++)
                {
                    RiverPassingPoint? point = map.PassingPoint(route.Via[v]);
                    if (point == null || HasPassing(point))
                    {
                        continue;
                    }

                    PassingMarker marker = PassingSlot(_passingCount++);
                    marker.Data = point;
                    marker.MapPoint = PlacePoint(map, point.At);
                    marker.Root.gameObject.name = "Passing_" + point.Id;
                }
            }

            for (int i = 0; i < _passingMarkers.Count; i++)
            {
                SetActive(_passingMarkers[i].Root, i < _passingCount);
            }
        }

        private bool HasPassing(RiverPassingPoint point)
        {
            for (int i = 0; i < _passingCount; i++)
            {
                if (ReferenceEquals(_passingMarkers[i].Data, point))
                {
                    return true;
                }
            }

            return false;
        }

        private PassingMarker PassingSlot(int i)
        {
            while (_passingMarkers.Count <= i)
            {
                int index = _passingMarkers.Count;
                Button button = UiBuilder.BareButton("Passing_" + index, _passingLayer!, () => TapPassing(index));
                var root = (RectTransform)button.transform;
                root.anchorMin = new Vector2(0f, 1f);
                root.anchorMax = new Vector2(0f, 1f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.sizeDelta = new Vector2(MinTouchDp, MinTouchDp);
                Image rim = UiBuilder.Image("Rim", root, _art.Disc, LeaderRim);
                rim.raycastTarget = false;
                CenterPiece(rim.rectTransform, PassingDotDp + 2f);
                Image dot = UiBuilder.Image("Dot", root, _art.Disc, PassingDotColor);
                dot.raycastTarget = false;
                CenterPiece(dot.rectTransform, PassingDotDp);
                _passingMarkers.Add(new PassingMarker(root));
            }

            return _passingMarkers[i];
        }

        // Al cambiar de vista: solo en la vista Mundo, cada uno en su sitio.
        private void PlacePassing(MapSpace space)
        {
            if (_passingLayer == null)
            {
                return;
            }

            SetActive(_passingLayer, _view == MapView.World);
            for (int i = 0; i < _passingCount; i++)
            {
                PassingMarker p = _passingMarkers[i];
                p.Root.anchoredPosition = space.ToLocal(p.MapPoint);
            }
        }

        private void PassingLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (_view != MapView.World)
            {
                return;
            }

            for (int i = 0; i < _passingCount; i++)
            {
                PassingMarker p = _passingMarkers[i];
                Vector2 at = new Vector2(_offset.x + p.Root.anchoredPosition.x, _offset.y - p.Root.anchoredPosition.y);
                if (p.Root.gameObject.activeInHierarchy && at.x >= 0f && at.x <= _viewW && at.y >= 0f && at.y <= _viewH)
                {
                    ids.Add("passing_" + p.Data.Id);
                    rects.Add(p.Root);
                }
            }
        }

        private sealed class PassingMarker
        {
            public PassingMarker(RectTransform root)
            {
                Root = root;
                Data = null!;
            }

            public RectTransform Root { get; }

            public RiverPassingPoint Data { get; set; }

            public Vector2 MapPoint { get; set; }
        }
    }
}
