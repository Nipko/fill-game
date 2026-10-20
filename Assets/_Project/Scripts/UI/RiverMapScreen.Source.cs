#nullable enable
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El hito y el rótulo de la naciente del Mismi (revisión de FX13; D-013, viñeta del 2026-10-19: «que el último tramo
    /// (691-700) apunte a la naciente (el Mismi) como meta, sin parada nueva»), parcial de <see cref="RiverMapScreen"/>:
    /// <list type="bullet">
    /// <item><description><b>Cuándo:</b> con la meta (<see cref="RiverRoutes.Goal"/>) y <c>max_level</c> ≥ el principio de su
    /// tramo (690 con las paradas 0-69): jugando 691-700 y en el fin de contenido del 700, en las dos vistas. Antes, no: el Mapa
    /// de siempre no cambia.</description></item>
    /// <item><description><b>El hito</b> [P]: en el punto real de la naciente (s = 0 del cauce), un disco de
    /// <see cref="SourceMarkScale"/> veces el radio de las boyas, crema con aro azul petróleo, distinto de las boyas (la del 700
    /// queda debajo). Va encima de la canoa, para que la canoa no tape la meta (en la Cuenca, el tramo es un trazo
    /// diminuto), pero se desvanece cuando la canoa llega a él (<see cref="SourceFadeDp"/>).</description></item>
    /// <item><description><b>El rótulo</b> [P]: <c>map_source_name</c> («El nevado Mismi», pendiente del asesor) en crema con
    /// contorno, <see cref="SourceLabelSp"/> sp, con su borde izquierdo junto al hito y a <see cref="SourceLabelGapDp"/> dp por
    /// encima de él (hacia dentro del mapa, que la naciente cae cerca del borde izquierdo); si ahí pisa la canoa con su mástil y
    /// sus banderas o el guía (como quedan con <c>max_level</c>: en el fin de contenido, en la naciente), por debajo o, si
    /// tampoco, más lejos (<see cref="SourceLabelTries"/>). Sin el
    /// visto bueno del asesor (en release), no sale.</description></item>
    /// </list>
    /// Se coloca al cambiar de vista y en cada boya (con las boyas), nunca por fotograma; con «Norte arriba», el rótulo va derecho.
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Radio del hito de la naciente frente al de las boyas del tramo [P] (revisión de FX13).</summary>
        public const float SourceMarkScale = 1.4f;

        /// <summary>Tamaño del rótulo de la naciente (sp) [P], como «Andes ↑» y por encima de los países (12 sp).</summary>
        public const float SourceLabelSp = 14f;

        /// <summary>Hueco entre el punto de la naciente y el borde del rótulo más cercano (dp) [P] (revisión de FX13: 12-16 dp).</summary>
        public const float SourceLabelGapDp = 12f;

        /// <summary>Pasos de <see cref="SourceLabelGapDp"/> que se aleja el rótulo de la naciente buscando sitio libre [P].</summary>
        public const int SourceLabelTries = 4;

        /// <summary>
        /// Distancia (dp) entre la canoa y el hito de la naciente, más allá del radio del hito, en la que el hito pasa de no verse a
        /// verse entero [P] (revisión de FX13): media canoa. Con la canoa en la naciente (el fin de contenido del 700), el hito no
        /// se pinta sobre su casco; la meta la marcan la canoa y el rótulo.
        /// </summary>
        public const float SourceFadeDp = ProceduralArt.MapCanoeWidthDp * 0.5f;

        /// <summary>Aro del hito de la naciente: <c>panel_alto</c>, el azul petróleo del canto de las boyas por ganar.</summary>
        public static readonly Color SourceMarkRim = Palette.Panel.Top;

        /// <summary>Relleno del hito de la naciente: crema.</summary>
        public static readonly Color SourceMarkFill = Palette.Cream;

        private RectTransform? _sourceLayer;
        private SourceMarkGraphic? _sourceMark;
        private TextMeshProUGUI? _sourceLabel;
        private Vector2 _sourceLocal;
        private bool _sourceShown;
        private bool _sourceLabelBelow;

        /// <summary>Se ve el hito de la naciente (jugando el tramo de la meta o en el fin de contenido del 700).</summary>
        public bool SourceMarkShown => _sourceShown;

        /// <summary>Punto del hito de la naciente en coordenadas locales del contenido (dp, y hacia arriba).</summary>
        internal Vector2 SourceMarkLocal => _sourceLocal;

        /// <summary>Radio del hito de la naciente (dp).</summary>
        public float SourceMarkRadius => _sourceMark != null ? _sourceMark.Radius : 0f;

        /// <summary>Opacidad del hito de la naciente (0 con la canoa encima; <see cref="SourceFadeDp"/>).</summary>
        public float SourceMarkAlpha => _sourceMark != null ? _sourceMark.canvasRenderer.GetAlpha() : 0f;

        /// <summary>Se ve el rótulo de la naciente.</summary>
        public bool SourceLabelShown => _sourceShown && _sourceLabel != null && _sourceLabel.gameObject.activeSelf;

        /// <summary>Texto del rótulo de la naciente (<c>map_source_name</c>).</summary>
        public string SourceLabelText => _sourceLabel != null ? _sourceLabel.text : string.Empty;

        /// <summary>El rótulo de la naciente fue por debajo del hito (por encima pisaba la canoa o el guía).</summary>
        public bool SourceLabelBelow => _sourceLabelBelow;

        /// <summary>Rótulo de la naciente.</summary>
        internal TextMeshProUGUI? SourceLabel => _sourceLabel;

        /// <summary>Capa del hito y del rótulo de la naciente (lienzo dinámico, encima de la canoa).</summary>
        internal RectTransform? SourceLayer => _sourceLayer;

        /// <summary>
        /// Se ven el hito y el rótulo de la naciente con <paramref name="maxLevel"/> niveles ganados: hay meta y el tramo que se
        /// juega (o el que acaba de ganarse) es el suyo, desde la llegada a la última parada (690 con las paradas 0-69). Sin
        /// asignaciones.
        /// </summary>
        public static bool SourceMarkDue(RiverRoutes routes, int maxLevel)
        {
            RiverStopRoute? goal = routes.Goal;
            return goal != null && maxLevel >= (goal.N - 1) * RiverStopCatalog.LevelsPerStop;
        }

        // Al montar el Mapa: la capa del hito y del rótulo, encima de la canoa y de Rosa.
        private void SourceBuild(RectTransform dynamicLayer, RectTransform above)
        {
            _sourceLayer = UiBuilder.Rect("Source", dynamicLayer);
            FillTopLeft(_sourceLayer);
            _sourceLayer.SetSiblingIndex(above.GetSiblingIndex() + 1);

            var go = new GameObject("SourceMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(SourceMarkGraphic));
            go.transform.SetParent(_sourceLayer, false);
            FillTopLeft((RectTransform)go.transform);
            _sourceMark = go.GetComponent<SourceMarkGraphic>();
            _sourceMark.raycastTarget = false;
            go.SetActive(false);

            _sourceLabel = UiBuilder.Label("SourceLabel", _sourceLayer, TextStyle.PillWood, string.Empty, SourceLabelSp, Palette.Cream,
                TextAlignmentOptions.Center);
            RectTransform rt = _sourceLabel.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            _sourceLabel.gameObject.SetActive(false);
        }

        // Al abrir: el texto del rótulo (asigna su cadena solo aquí) y su tamaño.
        private void SourceTexts()
        {
            if (_sourceLabel == null)
            {
                return;
            }

            string name = UiText.MapSourceName; // en release, sin el visto bueno del asesor, «»: el rótulo no sale
            _sourceLabel.text = name;
            if (name.Length > 0)
            {
                Vector2 size = _sourceLabel.GetPreferredValues(name, float.PositiveInfinity, float.PositiveInfinity);
                _sourceLabel.rectTransform.sizeDelta = new Vector2(Mathf.Ceil(size.x) + 2f, Mathf.Ceil(size.y) + 2f);
            }
        }

        // El hito en la naciente y el rótulo junto a él, con el radio de las boyas de la vista (con las boyas: al cambiar de vista y
        // en cada boya). Sin asignaciones.
        private void PlaceSource(MapSpace space, float buoyRadius)
        {
            if (_sourceMark == null || _sourceLabel == null)
            {
                return;
            }

            RiverMap? map = _map;
            RiverRoutes? routes = _routes;
            RiverStopRoute? goal = routes?.Goal;
            bool on = map != null && routes != null && goal != null && _progress != null && SourceMarkDue(routes, _progress.MaxLevel);
            _sourceShown = on;
            if (_sourceMark.gameObject.activeSelf != on)
            {
                _sourceMark.gameObject.SetActive(on);
            }

            bool labelOn = on && _sourceLabel.text.Length > 0;
            if (_sourceLabel.gameObject.activeSelf != labelOn)
            {
                _sourceLabel.gameObject.SetActive(labelOn);
            }

            if (!on)
            {
                _sourceLabelBelow = false;
                return;
            }

            Vector2 p = space.ToLocal(PlacePoint(map!, goal!.At));
            float r = SourceMarkScale * Mathf.Max(buoyRadius, BuoyRadiusMinDp);
            _sourceLocal = p;
            _sourceMark.SetMark(p, r);
            if (_hasCanoe)
            {
                FadeSourceUnderCanoe(_canoe.rectTransform.anchoredPosition);
            }
            if (!labelOn)
            {
                return;
            }

            RectTransform rt = _sourceLabel.rectTransform;
            Vector2 size = rt.sizeDelta;
            float contentW = space.DpPerUnit * map!.Width;
            float contentH = space.DpPerUnit * map.Height;
            // Por encima del hito, con el borde izquierdo junto a él (hacia dentro del mapa); si pisa la canoa o el guía, por debajo;
            // y si tampoco, cada vez SourceLabelGapDp más lejos (arriba y abajo, hasta SourceLabelTries pasos) y, por último, con
            // el borde derecho junto al hito. Sin sitio libre, por encima.
            float left = p.x - r + (size.x * 0.5f);
            float right = p.x + r - (size.x * 0.5f);
            Vector2 at = ClampBox(new Vector2(left, p.y + SourceLabelGapDp + (size.y * 0.5f)), size, contentW, contentH);
            _sourceLabelBelow = false;
            bool obstacles = SourceObstacles(space, out Rect gear, out Rect guideBox);
            bool found = false;
            for (int side = 0; side < 2 && !found; side++)
            {
                float cx = side == 0 ? left : right;
                for (int k = 0; k < SourceLabelTries && !found; k++)
                {
                    float gap = SourceLabelGapDp * (k + 1);
                    for (int below = 0; below < 2 && !found; below++)
                    {
                        float cy = below == 0 ? p.y + gap + (size.y * 0.5f) : p.y - gap - (size.y * 0.5f);
                        Vector2 c = ClampBox(new Vector2(cx, cy), size, contentW, contentH);
                        var box = new Rect(c.x - (size.x * 0.5f), c.y - (size.y * 0.5f), size.x, size.y);
                        if (!obstacles || (!box.Overlaps(gear) && !box.Overlaps(guideBox)))
                        {
                            at = c;
                            _sourceLabelBelow = below == 1;
                            found = true;
                        }
                    }
                }
            }

            rt.anchoredPosition = at;
        }

        // El hito de la naciente se desvanece bajo la canoa (revisión de FX13: en la llegada final se veía un círculo blanco
        // pegado al casco): opaco a SourceFadeDp o más del borde del hito, invisible con la canoa en él. Lo llaman la colocación
        // de la canoa (cada fotograma del viaje) y la del hito. Sin asignaciones.
        private void FadeSourceUnderCanoe(Vector2 canoeLocal)
        {
            if (_sourceMark == null || !_sourceShown)
            {
                return;
            }

            float d = Vector2.Distance(canoeLocal, _sourceLocal) - _sourceMark.Radius;
            float a = Mathf.Clamp01(d / SourceFadeDp);
            CanvasRenderer cr = _sourceMark.canvasRenderer;
            if (Mathf.Abs(cr.GetAlpha() - a) > 0.001f)
            {
                cr.SetAlpha(a);
            }
        }

        // La canoa con su mástil y sus banderas y el guía, como quedan con max_level (las cajas, en coordenadas locales). False
        // sin canoa.
        private bool SourceObstacles(MapSpace space, out Rect gear, out Rect guideBox)
        {
            gear = default;
            guideBox = default;
            if (!_hasCanoe || _progress == null || _map == null || _routes == null || _stops == null)
            {
                return false;
            }

            int max = Mathf.Clamp(_progress.MaxLevel, 0, WorldTable.LastLevel);
            float facing = _canoeFacing;
            GearPose(space, max, _canoeMap, FinalFlagCount(max), ref facing, out Vector2 canoe, out Vector2 guide, out _, out _, out _,
                out Vector2 foot, out float mastH);
            gear = GearBox(canoe, foot, mastH, facing);
            guideBox = GuideBox(guide);
            return true;
        }

        // Con «Norte arriba», el rótulo de la naciente derecho y a su tamaño en pantalla, como los países.
        private void UprightSourceLabel(Quaternion upright, Vector3 counter)
        {
            if (_sourceLabel != null)
            {
                RectTransform rt = _sourceLabel.rectTransform;
                rt.localRotation = upright;
                rt.localScale = counter;
            }
        }

        /// <summary>
        /// El hito de la naciente en una malla sin textura (como <see cref="BuoyGraphic"/>): un disco crema con aro azul petróleo.
        /// Solo se reconstruye cuando cambia; sin raycast.
        /// </summary>
        [RequireComponent(typeof(CanvasRenderer))]
        internal sealed class SourceMarkGraphic : MaskableGraphic
        {
            /// <summary>Tramos del disco.</summary>
            public const int Segments = 20;

            /// <summary>Grosor del aro como fracción del radio.</summary>
            public const float RimFraction = 0.3f;

            private Vector2 _point;
            private float _radius;

            /// <summary>Radio actual (dp).</summary>
            public float Radius => _radius;

            /// <summary>Punto actual (local, dp).</summary>
            public Vector2 Point => _point;

            /// <summary>Copia el punto y el radio y vuelve a dibujar solo si cambiaron. Sin asignaciones.</summary>
            public void SetMark(Vector2 point, float radius)
            {
                if (point == _point && radius == _radius)
                {
                    return;
                }

                _point = point;
                _radius = radius;
                SetVerticesDirty();
            }

            /// <inheritdoc />
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                if (_radius <= 0f)
                {
                    return;
                }

                Disc(vh, _point, _radius, SourceMarkRim);
                Disc(vh, _point, _radius * (1f - RimFraction), SourceMarkFill);
            }

            private void Disc(VertexHelper vh, Vector2 c, float r, Color tint)
            {
                Color32 c32 = tint * color;
                int start = vh.currentVertCount;
                vh.AddVert(c, c32, Vector2.zero);
                for (int k = 0; k < Segments; k++)
                {
                    float a = k * (Mathf.PI * 2f / Segments);
                    vh.AddVert(c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r), c32, Vector2.zero);
                }

                for (int k = 0; k < Segments; k++)
                {
                    vh.AddTriangle(start, start + 1 + k, start + 1 + ((k + 1) % Segments));
                }
            }
        }
    }
}
