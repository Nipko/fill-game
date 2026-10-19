#nullable enable
using System.Collections.Generic;
using AF.Levels;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Las placas de los mundos futuros (M3.4), enteras en la zona visible del Mapa con el encuadre con que se abre cada vista
    /// (tarea FX10, observación 3 de la prueba de la ola 6 en el teléfono: desde la pausa del 41, «Mundo 4 · Trapecio…» se salía
    /// por la derecha; en el Mapa del fin de contenido, «Mundo 8 · Madidi · Nivel 421» se salía por la izquierda y quedaba bajo la
    /// ventanita de casa):
    /// <list type="bullet">
    /// <item><description>Al abrir una vista (<see cref="ApplyView"/>), cada placa que asoma a la zona visible y no queda entera
    /// dentro (con <see cref="LabelEdgeDp"/> de margen), o que pisa la ventanita de casa y, en la vista Mundo, «Volver a la
    /// canoa» o un rótulo de orientación (con los huecos de <c>FixedHit</c>) o, en la Cuenca, uno de sus controles (los de FX7,
    /// <c>BasinControlObstacles</c>, con su hueco), se coloca en otro sitio junto al punto de su mundo: a su derecha, a su
    /// izquierda, encima o debajo, subiendo o bajando de <see cref="LabelSearchStepDp"/> en <see cref="LabelSearchStepDp"/> hasta
    /// <see cref="SilhouetteMaxShiftDp"/> (como M3.4 y <c>ResolvePlates</c>); si ninguno vale, esos mismos sitios desplazados lo
    /// justo hacia dentro de la zona visible; y si tampoco, el sitio más cercano a su punto en toda la zona visible (una rejilla
    /// de <see cref="PlateFitGridDp"/>; en el Mapa del fin de contenido, la placa de Madre de Dios, de 215 dp en una zona de 328,
    /// tiene encima la canoa con su mástil y sus banderas, y a la izquierda y debajo la ventanita). Vale el sitio que queda
    /// entero a la vista, dentro del mapa, a lo sumo a <see cref="SilhouetteFarMaxDistDp"/> más medio ancho de su punto (como en
    /// FX7) y sin pisar nada de lo anterior, la canoa con su equipo, el guía, una mini cuia, un país ni otra placa; en los dos primeros pasos, el primero que no tapa ninguna línea del lleno o, si todos tapan alguna, el que
    /// tapa menos; en la rejilla, el más cercano que no tapa el lleno o, si todos lo tapan, el más cercano. Una placa que ya se
    /// veía bien, o que queda entera fuera de la zona visible, no se mueve.</description></item>
    /// <item><description>Si una placa no cabe de ningún modo, en la vista Mundo no sale con ese encuadre [P]
    /// (<see cref="PlateFitHidden"/>) ni cuenta como obstáculo para las demás (<c>PlateBlocked</c>); arrastrar el mapa la vuelve
    /// a enseñar en su sitio. En la Cuenca se queda donde la puso FX7
    /// (no se oculta: el norte arriba, opción A, decide sus placas aparte).</description></item>
    /// <item><description>«Ponerse al día» rehace las placas en cada boya (<see cref="ShowProgressAt"/>): cuando cambian los mundos
    /// futuros (un mundo deja de serlo), las placas se vuelven a colocar con el encuadre de ese momento (antes, la del mundo
    /// siguiente heredaba el sitio de la que se iba, que podía estar cortada o bajo la ventanita).</description></item>
    /// </list>
    /// Con el norte arriba (o mientras gira) y mientras la secuencia del 10 está abierta (coloca su placa de destino y atenúa los
    /// rótulos bajo el bocadillo aparte; FX7 y 20cf633), no se toca nada. Solo al abrir una vista o al cambiar las placas; sin asignaciones
    /// (salvo la tabla de las ocultas si crece el número de placas).
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        // Orden de los sitios que se prueban en cada paso vertical (los de ResolvePlates): a la derecha (1) y a la izquierda (2)
        // subiendo, encima (0), y lo mismo bajando (4, 5 y 3).
        private static readonly int[] PlateFitSides = { 1, 2, 0, 4, 5, 3 };

        /// <summary>Paso de la rejilla con que se busca sitio en toda la zona visible para una placa que no cabe junto a su punto (dp).</summary>
        public const float PlateFitGridDp = 8f;

        /// <summary>
        /// Para los tests: una zona (coordenadas locales del contenido, dp) que la placa del mundo
        /// <see cref="PlateTestObstacleWorld"/> no pisa, como un control, en las dos vistas: ni al colocarla
        /// (<c>PlaceSilhouettes</c>, con su búsqueda en la rejilla de la Cuenca) ni al encajarla en la vista (FX10). Las demás placas
        /// no la ven. Prueba la placa sin sitio (FX10) y la que no tiene sitio junto a su parada (M5.1c) sin depender de la geometría
        /// del Mapa (con la de producción, todas caben en los casos medidos). Null en el juego.
        /// </summary>
        internal static Rect? PlateTestObstacle { get; set; }

        /// <summary>El mundo de la placa que no pisa <see cref="PlateTestObstacle"/> (tests).</summary>
        internal static int PlateTestObstacleWorld { get; set; }

        /// <summary>
        /// Para los tests: la placa de este mundo no encuentra ningún sitio junto a su parada (<c>PlaceSilhouettes</c>), como si los
        /// países y los ríos más detallados llenaran todos los huecos entre meandros (M5.1c); 0, ninguna. Prueba el respaldo de la
        /// rejilla de la Cuenca sin depender de la geometría del Mapa. 0 en el juego.
        /// </summary>
        internal static int PlateTestNoNearSpotWorld { get; set; }

        private bool[] _plateFitHidden = new bool[0];
        private long _plateFitWorlds = -1;
        private int _plateFitMoves;

        /// <summary>
        /// La placa de la silueta <paramref name="i"/> (en el orden de <see cref="SilhouetteCount"/>) no sale con este encuadre:
        /// no cabía entera a la vista sin pisar nada (FX10) [P].
        /// </summary>
        internal bool PlateFitHidden(int i)
        {
            int seen = 0;
            for (int k = 0; k < _silhouettes.Count; k++)
            {
                if (_silhouettes[k].Root.gameObject.activeSelf && seen++ == i)
                {
                    return k < _plateFitHidden.Length && _plateFitHidden[k];
                }
            }

            return false;
        }

        /// <summary>Placas que la última colocación (FX10) tuvo que mover para que quedaran enteras a la vista.</summary>
        internal int PlateFitMoves => _plateFitMoves;

        /// <summary>
        /// Caja de la placa de la silueta <paramref name="i"/> (en el orden de <see cref="SilhouetteCount"/>) en coordenadas de la
        /// zona visible del mapa (dp, y hacia abajo), con el encuadre de ahora.
        /// </summary>
        internal Rect SilhouetteViewBox(int i)
        {
            return InView(SilhouetteLocalBox(i), _offset);
        }

        /// <summary>
        /// Una placa (la silueta <paramref name="i"/>, en el orden de <see cref="SilhouetteCount"/>; −1, una que no es ninguna de
        /// ellas) en <paramref name="localBox"/> (coordenadas locales del contenido) pisaría la canoa con su equipo, el guía, una
        /// mini cuia u otra placa que sale (<c>PlateBlocked</c>; una oculta por FX10 no cuenta). Para los tests.
        /// </summary>
        internal bool SilhouetteBlockedAt(int i, Rect localBox)
        {
            int self = -1;
            int seen = 0;
            for (int k = 0; i >= 0 && k < _silhouettes.Count; k++)
            {
                if (_silhouettes[k].Root.gameObject.activeSelf && seen++ == i)
                {
                    self = k;
                    break;
                }
            }

            return PlateBlocked(self, localBox);
        }

        // Tras abrir una vista (ApplyView, con el encuadre ya puesto): las placas, enteras a la vista.
        private void PlatesFitView()
        {
            PlatesFit();
            _plateFitWorlds = PlateWorlds();
        }

        // Tras una boya de «ponerse al día» (CatchUp.cs): si cambiaron los mundos futuros, las placas se vuelven a colocar (M3.4,
        // con los países, que comparten la tabla de lo ya colocado) y a encajar en la vista.
        private void PlatesFollowProgress()
        {
            if (!Visible || _map == null || _fillLayer == null || _seqOpen || _northUpOn || _northUpT > 0f)
            {
                return;
            }

            long worlds = PlateWorlds();
            if (worlds == _plateFitWorlds)
            {
                return;
            }

            _plateFitWorlds = worlds;
            _placed.Clear();
            PlaceCountries(_space);
            PlaceSilhouettes(_space);
            PlatesFit();
        }

        // Arrastrar el mapa: las placas que no salían con el encuadre de apertura vuelven, en su sitio.
        private void PlatesDragged()
        {
            for (int k = 0; k < _plateFitHidden.Length && k < _silhouettes.Count; k++)
            {
                if (_plateFitHidden[k])
                {
                    ShowPlate(k, true);
                }
            }
        }

        private long PlateWorlds()
        {
            long worlds = 0;
            for (int k = 0; k < _silhouettes.Count; k++)
            {
                if (_silhouettes[k].Root.gameObject.activeSelf)
                {
                    worlds |= 1L << (_silhouettes[k].World & 63);
                }
            }

            return worlds;
        }

        private void ShowPlate(int k, bool show)
        {
            Silhouette s = _silhouettes[k];
            if (s.Plate.enabled != show)
            {
                s.Plate.enabled = show;
            }

            if (s.Label.enabled != show)
            {
                s.Label.enabled = show;
            }

            _plateFitHidden[k] = !show;
        }

        private void PlatesFit()
        {
            _plateFitMoves = 0;
            if (_plateFitHidden.Length < _silhouettes.Count)
            {
                System.Array.Resize(ref _plateFitHidden, _silhouettes.Count);
            }

            for (int k = 0; k < _silhouettes.Count; k++)
            {
                if (_plateFitHidden[k])
                {
                    ShowPlate(k, true);
                }
            }

            RiverMap? map = _map;
            if (map == null || _northUpOn || _northUpT > 0f || _seqOpen || (_view != MapView.World && _view != MapView.Basin))
            {
                return;
            }

            Vector2 o = _offset;
            bool basin = _view == MapView.Basin;

            // Lo fijo del marco, con este encuadre (los mismos campos que usa MarkersFrame, que ya terminó).
            _frameWindow = _homeWindow != null && _homeWindow.Ready;
            _frameWin = _frameWindow ? HomeWindowInView() : default;
            _frameAndes = FixedViewRect(AndesLabel);
            _frameAtlantic = FixedViewRect(AtlanticLabel);
            _frameButton = FixedViewRect((RectTransform)_backToCanoe.Button.transform);
            bool window = _frameWindow && !HomeInViewAt(o);
            bool button = _backToCanoe.Button.gameObject.activeSelf;
            var full = new Rect(0f, 0f, _viewW, _viewH);
            float e = LabelEdgeDp;
            var inner = new Rect(e, e, _viewW - (2f * e), _viewH - (2f * e));
            for (int k = 0; k < _silhouettes.Count; k++)
            {
                RectTransform root = _silhouettes[k].Root;
                if (!root.gameObject.activeSelf)
                {
                    continue;
                }

                Rect local = PlateBox(root);
                Rect seen = InView(local, o);
                if (!seen.Overlaps(full) || (Inside(seen, inner) && !PlateFixedHit(_silhouettes[k].World, local, seen, window, button, basin)))
                {
                    continue; // entera fuera de la vista (no se ve) o entera dentro y libre
                }

                if (PlateFitSpot(map, k, o, inner, window, button, basin, out Rect spot))
                {
                    root.anchoredPosition = new Vector2(spot.xMin, spot.center.y);
                    _silhouettes[k].FillHits = FillLinesUnder(map, _space, _view, spot);
                    _plateFitMoves++;
                }
                else if (!basin)
                {
                    ShowPlate(k, false); // no cabe entera con este encuadre [P]
                }
            }
        }

        // El sitio de la placa k con el contenido en o (ver el resumen de la clase). False si no hay ninguno.
        private bool PlateFitSpot(RiverMap map, int k, Vector2 o, Rect inner, bool window, bool button, bool basin, out Rect spot)
        {
            spot = default;
            RectTransform root = _silhouettes[k].Root;
            Vector2 size = root.sizeDelta;
            Vector2 p = _space.ToLocal(_silhouettes[k].MapPoint);
            float reach = SilhouetteFarMaxDistDp + (size.x * 0.5f);
            int steps = Mathf.RoundToInt(SilhouetteMaxShiftDp / LabelSearchStepDp);

            // 1 y 2: junto al punto, como M3.4; y esos sitios, desplazados lo justo hacia dentro de la zona visible.
            for (int pass = 0; pass < 2; pass++)
            {
                bool any = false;
                int bestFill = int.MaxValue;
                for (int step = 0; step <= steps && bestFill > 0; step++)
                {
                    float dy = step * LabelSearchStepDp;
                    for (int j = 0; j < PlateFitSides.Length && bestFill > 0; j++)
                    {
                        int c = PlateFitSides[j];
                        float up = c < 3 ? dy : -dy;
                        float x = (c % 3) == 0 ? p.x - (size.x * 0.5f) : (c % 3) == 1 ? p.x + SilhouetteGapDp : p.x - SilhouetteGapDp - size.x;
                        float y = (c % 3) != 0 ? p.y + up - (size.y * 0.5f) : c == 0 ? p.y + FixedOverlayGapDp + dy : p.y - FixedOverlayGapDp - dy - size.y;
                        if (pass == 1)
                        {
                            // En coordenadas locales: la x de la zona visible es x + o.x y su y (hacia abajo), o.y − y.
                            x = Mathf.Clamp(x, inner.xMin - o.x, Mathf.Max(inner.xMin - o.x, inner.xMax - o.x - size.x));
                            y = Mathf.Clamp(y, o.y - inner.yMax, Mathf.Max(o.y - inner.yMax, o.y - inner.yMin - size.y));
                        }

                        var box = new Rect(x, y, size.x, size.y);
                        if (PlateFits(map, k, box, p, reach, o, inner, window, button, basin, out int fill) && (!any || fill < bestFill))
                        {
                            any = true;
                            bestFill = fill;
                            spot = box;
                        }
                    }
                }

                if (any)
                {
                    return true;
                }
            }

            // 3: en toda la zona visible, a su alcance (una rejilla de PlateFitGridDp, pegada también a los bordes derecho e
            // inferior), el sitio libre más cercano al punto que no tapa el lleno o, si todos lo tapan, el más cercano.
            int cols = Mathf.FloorToInt((inner.width - size.x) / PlateFitGridDp);
            int rows = Mathf.FloorToInt((inner.height - size.y) / PlateFitGridDp);
            bool found = false;
            bool clean = false;
            float best = float.PositiveInfinity;
            for (int r = 0; r <= rows + 1; r++)
            {
                float vy = r <= rows ? inner.yMin + (r * PlateFitGridDp) : inner.yMax - size.y;
                for (int c = 0; c <= cols + 1; c++)
                {
                    float vx = c <= cols ? inner.xMin + (c * PlateFitGridDp) : inner.xMax - size.x;
                    var box = new Rect(vx - o.x, o.y - vy - size.y, size.x, size.y);
                    float distance = PointBoxDistance(p, box);
                    if ((clean && distance >= best) || !PlateFits(map, k, box, p, reach, o, inner, window, button, basin, out int fill))
                    {
                        continue;
                    }

                    bool free = fill == 0;
                    if (!found || (free && !clean) || (free == clean && distance < best))
                    {
                        found = true;
                        clean = free;
                        best = distance;
                        spot = box;
                    }
                }
            }

            return found;
        }

        // Una placa de la silueta k en box (coordenadas locales del contenido) vale con el contenido en o: dentro del mapa, a su
        // alcance del punto p, entera en la zona visible y sin pisar lo fijo del marco, la canoa con su equipo, el guía, una mini
        // cuia, otra placa, un país ni, en la Cuenca, sus controles; fill, las líneas del lleno que tapa.
        private bool PlateFits(RiverMap map, int k, Rect box, Vector2 p, float reach, Vector2 o, Rect inner, bool window, bool button, bool basin,
            out int fill)
        {
            fill = 0;
            float e = LabelEdgeDp;
            if (box.xMin < e || box.xMax > _contentW - e || box.yMax > -e || box.yMin < -_contentH + e
                || (box.center - p).sqrMagnitude > reach * reach)
            {
                return false;
            }

            Rect seen = InView(box, o);
            if (!Inside(seen, inner) || PlateFixedHit(_silhouettes[k].World, box, seen, window, button, basin) || PlateBlocked(k, box) || CountryUnder(box))
            {
                return false;
            }

            fill = FillLinesUnder(map, _space, _view, box);
            return true;
        }

        // Lo que flota encima del mapa y la placa no pisa (local: la caja en coordenadas del contenido; seen, en la zona visible):
        // en la vista Mundo, la ventanita, «Volver a la canoa» y los rótulos de orientación con los huecos de FixedHit (como
        // ResolvePlates); en la Cuenca, la ventanita y los controles de FX7 (BasinControlObstacles, con su hueco), que ya cuentan
        // «Norte arriba», «Volver a la canoa» y los rótulos.
        // world: el de la placa (para el obstáculo de los tests, PlateTestObstacle).
        private bool PlateFixedHit(int world, Rect local, Rect seen, bool window, bool button, bool basin)
        {
            if (TestObstacleHit(world, local))
            {
                return true;
            }

            if (!basin)
            {
                return FixedHit(seen, window, button);
            }

            return (window && seen.Overlaps(Grow(_frameWin, HomeWindowCuiaGapDp))) || HitsBasinControl(local);
        }

        // La placa del mundo world en local (coordenadas locales del contenido) pisa el obstáculo de los tests (PlateTestObstacle).
        private static bool TestObstacleHit(int world, Rect local)
        {
            return PlateTestObstacle.HasValue && world == PlateTestObstacleWorld && local.Overlaps(PlateTestObstacle.Value);
        }

        // La caja (coordenadas locales del contenido) pisa un país que se ve.
        private bool CountryUnder(Rect box)
        {
            List<TMPro.TextMeshProUGUI> countries = _countries;
            for (int i = 0; i < countries.Count; i++)
            {
                TMPro.TextMeshProUGUI label = countries[i];
                if (!label.gameObject.activeSelf || CountryBasinHidden(i))
                {
                    continue;
                }

                RectTransform rt = label.rectTransform;
                Vector2 at = rt.anchoredPosition;
                Vector2 size = rt.sizeDelta;
                if (box.Overlaps(new Rect(at.x - (size.x * 0.5f), at.y - (size.y * 0.5f), size.x, size.y)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
