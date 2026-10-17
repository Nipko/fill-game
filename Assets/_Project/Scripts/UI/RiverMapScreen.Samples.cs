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
    /// El Muestrario del río, la tarjeta de la parada y los miradores del Mapa (tarea M4.1; D-013, punto 10: Q9-A y Q10-A; 06
    /// §4.5; propuesta del Mapa §5.2 y §7), parcial de <see cref="RiverMapScreen"/> sobre sus ganchos <c>Samples*</c> y
    /// <c>MarkersStopTapped</c>. Todo se deduce de <c>max_level</c>, del catálogo de paradas y de la geometría, sin estado nuevo:
    /// <list type="bullet">
    /// <item><description><b>Pestaña del Muestrario</b> (<c>map_samples</c>, «Muestrario · {k}/{n}»): una píldora de madera con su
    /// mini cuia en una fila propia entre la zona del mapa y el pie, a la derecha (en la fila del pie no cabe junto a «Faltan…»).
    /// k son las muestras ganadas (las paradas alcanzadas y los miradores revelados) y n, las que hay en el catálogo.</description></item>
    /// <item><description><b>Muestrario</b> (<see cref="OpenSamples"/>): una hoja que tapa el Mapa con su ←, el título, el contador
    /// <c>map_facts_count</c> («Datos del río · {k}/{n}»: los datos con texto de las paradas alcanzadas y de los miradores
    /// revelados, de todos los que hay [P]) y <b>una fila por mundo</b> del catálogo, con su nombre y sus mini cuias de pie en la
    /// repisa del tablero (<see cref="ProceduralArt.Shelf"/>; «parece un tablero en pequeño», propuesta §7), como mucho
    /// <see cref="SamplesPerShelf"/> por repisa: llenas del agua del sitio las alcanzadas, de vidrio con su canto crema las que
    /// faltan; detrás de las del mundo, la muestra especial de cada mirador (con su «?»), vacía y sin toque hasta que el progreso
    /// lo revela. Cada muestra se toca (≥ 48 dp) y abre su tarjeta. Si no cabe, se desplaza.</description></item>
    /// <item><description><b>Tarjeta de la parada</b> (<see cref="StopCard"/>), al tocar una mini cuia del Mapa o una muestra:
    /// entera si se alcanzó y, si no, solo la silueta y «Nivel n».</description></item>
    /// <item><description><b>Miradores</b> (<c>viewpoints[]</c> de <c>river_map_v1.json</c>): el «?»
    /// (<see cref="ProceduralArt.MapViewpoint"/>) en su punto, fuera de la ruta, en la vista Mundo, desde que se gana su
    /// <c>reveal_on</c> (en 1-200, los tres del estuario desde el 10); al tocarlo, su tarjeta (nombre y dato).</description></item>
    /// </list>
    /// <para>Sin precios, ofertas ni semillas (Q10-A). El Atrás de Android cierra primero la tarjeta y luego el Muestrario
    /// (<see cref="CloseTopSheet"/>). Todo se construye al abrir (y la reserva, al crecer); con el Mapa quieto no hay nada por
    /// fotograma.</para>
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Alto de la fila de la pestaña del Muestrario (dp): un control de 48 dp.</summary>
        public const float SamplesRowDp = MinTouchDp;

        /// <summary>Hueco de la fila de la pestaña con la zona del mapa y con la fila del pie (dp) [P].</summary>
        public const float SamplesRowGapDp = 4f;

        /// <summary>Muestras como mucho en una repisa del Muestrario [P]: 6 × 48 dp caben en 328 dp con su aire.</summary>
        public const int SamplesPerShelf = 6;

        /// <summary>Escala de la mini cuia del Muestrario sobre la del Mapa (20 × 32 dp) [P]: 30 × 48 dp.</summary>
        public const float SampleScale = 1.5f;

        /// <summary>Alto del área táctil de una muestra del Muestrario (dp): la mini cuia de 48 dp y 4 de aire arriba.</summary>
        public const float SampleTouchDp = 52f;

        /// <summary>Alto de la repisa del tablero (dp; guía 4.5).</summary>
        public const float ShelfDp = 26f;

        /// <summary>Lado del «?» de la muestra especial de un mirador en el Muestrario (dp) [P].</summary>
        public const float SampleBadgeDp = 18f;

        /// <summary>Opacidad del «?» de un mirador que aún no se ha revelado, en el Muestrario [P].</summary>
        public const float HiddenBadgeAlpha = 0.45f;

        private const float SamplesTitleSp = 20f;
        private const float SamplesCountSp = 15f;
        private const float WorldLabelSp = 15f;
        private const float SamplesTabSp = 14f;

        private readonly List<SampleSlot> _sampleSlots = new List<SampleSlot>();
        private readonly List<TextMeshProUGUI> _worldLabels = new List<TextMeshProUGUI>();
        private readonly List<Image> _shelves = new List<Image>();
        private readonly List<ViewpointMarker> _viewpoints = new List<ViewpointMarker>();
        private ReliefParts? _samplesTab;
        private TextMeshProUGUI? _samplesTabLabel;
        private Image? _samplesTabIcon;
        private RectTransform? _samplesSheet;
        private Image? _samplesHeader;
        private ReliefParts? _samplesBack;
        private TextMeshProUGUI? _samplesTitle;
        private TextMeshProUGUI? _samplesCount;
        private RectTransform? _samplesViewport;
        private RectTransform? _samplesContent;
        private ScrollRect? _samplesScroll;
        private StopCard? _card;
        private RectTransform? _viewpointLayer;
        private int _samplesEarned;
        private int _samplesTotal;
        private int _factsSeen;
        private int _factsTotal;
        private int _sampleSlotCount;
        private int _worldLabelCount;
        private int _shelfCount;
        private int _viewpointCount;
        private bool _closingMap;

        /// <summary>
        /// Catálogo de miquitos para la fila del miquito del tramo de la tarjeta (lo pone <see cref="GameBootstrap"/>); null = sin
        /// esa fila.
        /// </summary>
        public FaunaCatalog? Fauna { get; set; }

        /// <summary>
        /// <c>water_base</c> del nivel (<c>white</c>, <c>black</c> o <c>clear</c>) para la banda del primer plano del fondo de la
        /// tarjeta (lo pone <see cref="GameBootstrap"/> con el catálogo de niveles). Null o sin respuesta: el agua del sitio de la
        /// gran parada del mundo, que es la del mundo en 1-700 (<see cref="WorldWaterOf"/>).
        /// </summary>
        public Func<int, string?>? LevelWater { get; set; }

        // ============================================================================================ API (tests y sondas)

        /// <summary>La pestaña del Muestrario se ve.</summary>
        public bool SamplesTabShown => _samplesTab != null && _samplesSlot.gameObject.activeSelf;

        /// <summary>«Muestrario · {k}/{n}».</summary>
        public string SamplesTabText => _samplesTabLabel != null ? _samplesTabLabel.text : string.Empty;

        /// <summary>La pestaña del Muestrario.</summary>
        public Button? SamplesTabButton => _samplesTab?.Button;

        /// <summary>El Muestrario está abierto.</summary>
        public bool SamplesSheetOpen => _samplesSheet != null && _samplesSheet.gameObject.activeSelf;

        /// <summary>Hoja del Muestrario (pantalla completa).</summary>
        public RectTransform? SamplesSheet => _samplesSheet;

        /// <summary>← del Muestrario.</summary>
        public Button? SamplesBackButton => _samplesBack?.Button;

        /// <summary>Título del Muestrario.</summary>
        public string SamplesTitle => _samplesTitle != null ? _samplesTitle.text : string.Empty;

        /// <summary>«Datos del río · {k}/{n}».</summary>
        public string SamplesFactsText => _samplesCount != null ? _samplesCount.text : string.Empty;

        /// <summary>Muestras ganadas con el <c>max_level</c> del último <see cref="Open"/>.</summary>
        public int SamplesEarned => _samplesEarned;

        /// <summary>Muestras que hay (paradas del catálogo y miradores).</summary>
        public int SamplesTotal => _samplesTotal;

        /// <summary>Datos del río vistos (con texto, de las paradas alcanzadas y de los miradores revelados).</summary>
        public int FactsSeen => _factsSeen;

        /// <summary>Datos del río que hay (con texto).</summary>
        public int FactsTotal => _factsTotal;

        /// <summary>Muestras del Muestrario abierto, en orden de mundo (las paradas y detrás los miradores).</summary>
        public int SampleCount => _sampleSlotCount;

        /// <summary>Parada (k) de la muestra <paramref name="i"/>, o −1 si es la de un mirador.</summary>
        public int SampleStop(int i)
        {
            return _sampleSlots[i].StopN;
        }

        /// <summary>Mirador de la muestra <paramref name="i"/>, o null.</summary>
        public RiverViewpoint? SampleViewpoint(int i)
        {
            return _sampleSlots[i].Viewpoint;
        }

        /// <summary>Mundo de la fila de la muestra <paramref name="i"/>.</summary>
        public int SampleWorld(int i)
        {
            return _sampleSlots[i].World;
        }

        /// <summary>La muestra <paramref name="i"/> está ganada (llena del agua del sitio).</summary>
        public bool SampleEarned(int i)
        {
            return _sampleSlots[i].Earned;
        }

        /// <summary>Repisa (desde 0, de arriba abajo) de la muestra <paramref name="i"/>.</summary>
        public int SampleShelf(int i)
        {
            return _sampleSlots[i].Shelf;
        }

        /// <summary>Área táctil de la muestra <paramref name="i"/>.</summary>
        public Button SampleButton(int i)
        {
            return _sampleSlots[i].Button;
        }

        /// <summary>Mini cuia de la muestra <paramref name="i"/> (tests: el agua).</summary>
        internal SampleCuia SampleCuiaAt(int i)
        {
            return _sampleSlots[i].Cuia;
        }

        /// <summary>Filas de mundo del Muestrario abierto.</summary>
        public int SampleWorldRows => _worldLabelCount;

        /// <summary>Nombre del mundo de la fila <paramref name="row"/>.</summary>
        public string SampleWorldLabel(int row)
        {
            return _worldLabels[row].text;
        }

        /// <summary>Repisas del Muestrario abierto.</summary>
        public int SampleShelfCount => _shelfCount;

        /// <summary>La tarjeta de la parada (null hasta la primera vez que se abre).</summary>
        public StopCard? Card => _card;

        /// <summary>La tarjeta de la parada está abierta.</summary>
        public bool CardOpen => _card != null && _card.Visible;

        /// <summary>Miradores del Mapa (los del catálogo de la geometría).</summary>
        public int ViewpointCount => _viewpointCount;

        /// <summary>El mirador <paramref name="i"/>.</summary>
        public RiverViewpoint ViewpointAt(int i)
        {
            return _viewpoints[i].Data;
        }

        /// <summary>El «?» del mirador <paramref name="i"/> se ve (revelado y en la vista Mundo).</summary>
        public bool ViewpointShown(int i)
        {
            return _viewpoints[i].Root.gameObject.activeInHierarchy;
        }

        /// <summary>Área táctil del mirador <paramref name="i"/>.</summary>
        public Button ViewpointButton(int i)
        {
            return _viewpoints[i].Button;
        }

        /// <summary>Centro del mirador <paramref name="i"/> en coordenadas locales del contenido (dp, y hacia arriba).</summary>
        public Vector2 ViewpointLocal(int i)
        {
            return _viewpoints[i].Root.anchoredPosition;
        }

        /// <summary>Textos del Muestrario y de la tarjeta que se ven (tests: ≥ 12 sp, sin recortes y sin precios).</summary>
        internal IEnumerable<TextMeshProUGUI> SampleTexts()
        {
            if (SamplesSheetOpen)
            {
                yield return _samplesTitle!;
                yield return _samplesCount!;
                for (int i = 0; i < _worldLabelCount; i++)
                {
                    yield return _worldLabels[i];
                }
            }

            if (CardOpen)
            {
                foreach (TextMeshProUGUI t in _card!.Texts())
                {
                    yield return t;
                }
            }
        }

        // ============================================================================================ reglas puras

        /// <summary>La parada <paramref name="stop"/> se alcanzó con <paramref name="maxLevel"/> niveles ganados (se ganó su <c>10k</c>; Belém, siempre).</summary>
        public static bool StopReached(RiverStop stop, int maxLevel)
        {
            return stop != null && Mathf.Max(0, maxLevel) >= stop.N * RiverStopCatalog.LevelsPerStop;
        }

        /// <summary>El mirador <paramref name="viewpoint"/> está revelado con <paramref name="maxLevel"/> niveles ganados (se ganó su <c>reveal_on</c>).</summary>
        public static bool ViewpointRevealed(RiverViewpoint viewpoint, int maxLevel)
        {
            return viewpoint != null && maxLevel >= viewpoint.RevealOn;
        }

        /// <summary>
        /// Muestras del Muestrario (Q9-A): <paramref name="total"/>, una por parada del catálogo y una especial por mirador;
        /// <paramref name="earned"/>, las de las paradas alcanzadas y los miradores revelados con <paramref name="maxLevel"/>.
        /// </summary>
        public static void CountSamples(RiverStopCatalog stops, RiverMap? map, int maxLevel, out int earned, out int total)
        {
            if (stops == null)
            {
                throw new ArgumentNullException(nameof(stops));
            }

            earned = 0;
            total = 0;
            for (int n = 0; n < stops.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                if (stop == null)
                {
                    continue;
                }

                total++;
                earned += StopReached(stop, maxLevel) ? 1 : 0;
            }

            if (map != null)
            {
                for (int i = 0; i < map.Viewpoints.Count; i++)
                {
                    total++;
                    earned += ViewpointRevealed(map.Viewpoints[i], maxLevel) ? 1 : 0;
                }
            }
        }

        /// <summary>
        /// Datos del río del Muestrario (<c>map_facts_count</c>; 06 §4.5) [P]: <paramref name="total"/>, los datos con texto en el
        /// idioma (<see cref="Loc.Fact(string)"/>) de las paradas del catálogo (el de llegada y los «Otro dato») y de los miradores;
        /// <paramref name="seen"/>, los de las paradas alcanzadas y los miradores revelados con <paramref name="maxLevel"/>. Cada
        /// clave cuenta una vez. Asigna (un conjunto): solo al abrir.
        /// </summary>
        public static void CountFacts(RiverStopCatalog stops, RiverMap? map, int maxLevel, out int seen, out int total)
        {
            if (stops == null)
            {
                throw new ArgumentNullException(nameof(stops));
            }

            seen = 0;
            total = 0;
            var counted = new HashSet<string>(StringComparer.Ordinal);
            var keys = new List<string>();
            for (int n = 0; n < stops.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                if (stop == null)
                {
                    continue;
                }

                bool reached = StopReached(stop, maxLevel);
                StopCard.FactKeysOf(stop, keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    if (counted.Add(keys[i]))
                    {
                        total++;
                        seen += reached ? 1 : 0;
                    }
                }
            }

            if (map != null)
            {
                for (int i = 0; i < map.Viewpoints.Count; i++)
                {
                    RiverViewpoint v = map.Viewpoints[i];
                    if (v.Fact != null && Loc.Fact(v.Fact).Length > 0 && counted.Add(v.Fact))
                    {
                        total++;
                        seen += ViewpointRevealed(v, maxLevel) ? 1 : 0;
                    }
                }
            }
        }

        /// <summary>
        /// Agua del primer plano del mundo <paramref name="world"/> (<c>white</c>, <c>black</c> o <c>clear</c>) sin el catálogo de
        /// niveles: la primera del <c>sample</c> de su gran parada (Belém, blanca; Santarém, clara; el Encuentro, negra; Leticia e
        /// Iquitos, blancas), que es la del mundo (regla de coherencia del agua, propuesta §5.1). Sin esa parada, <c>white</c>.
        /// </summary>
        public static string WorldWaterOf(RiverStopCatalog stops, int world)
        {
            RiverStop? gate = stops?.FirstStopOfWorld(world);
            if (gate == null)
            {
                return "white";
            }

            string sample = gate.Sample;
            int cut = sample.IndexOf('_');
            return cut < 0 ? sample : sample.Substring(0, cut);
        }

        // ============================================================================================ abrir y cerrar

        /// <summary>
        /// Abre el Muestrario encima del Mapa (la pestaña <c>map_samples</c>). El toque completa «ponerse al día» si está en marcha.
        /// </summary>
        public void OpenSamples()
        {
            if (!Visible || _stops == null || _progress == null)
            {
                return;
            }

            CatchUpTouch();
            NoteTouch();
            EnsureSamplesSheet();
            BuildSamples();
            _samplesSheet!.gameObject.SetActive(true);
            _samplesSheet.SetAsLastSibling();
            if (_card != null && _card.Visible)
            {
                _card.Root.SetAsLastSibling();
            }

            LayoutSamplesSheet();
            _samplesScroll!.verticalNormalizedPosition = 1f;
            EmitLayout();
        }

        /// <summary>Cierra el Muestrario (su ← o el Atrás de Android). Sin efecto si no está abierto.</summary>
        public void CloseSamples()
        {
            if (!SamplesSheetOpen)
            {
                return;
            }

            _samplesSheet!.gameObject.SetActive(false);
            EmitLayout();
        }

        /// <summary>
        /// Abre la tarjeta de la parada <paramref name="n"/> (lo que hace tocar su mini cuia o su muestra): entera si se alcanzó, la
        /// silueta y «Nivel n» si no. False sin esa parada.
        /// </summary>
        public bool OpenStopCard(int n)
        {
            RiverStop? stop = _stops?.At(n);
            if (!Visible || stop == null || _progress == null)
            {
                return false;
            }

            StopCard card = EnsureCard();
            card.Layout(_canvasW, _canvasH, _safeTop, _safeBottom);
            card.ShowStop(stop, StopReached(stop, _progress.MaxLevel), StopWater(stop), Fauna);
            EmitLayout();
            return true;
        }

        /// <summary>Abre la tarjeta del mirador <paramref name="i"/> (su «?»), si está revelado. False si no.</summary>
        public bool OpenViewpointCard(int i)
        {
            if (!Visible || _progress == null || i < 0 || i >= _viewpointCount || !ViewpointRevealed(_viewpoints[i].Data, _progress.MaxLevel))
            {
                return false;
            }

            StopCard card = EnsureCard();
            card.Layout(_canvasW, _canvasH, _safeTop, _safeBottom);
            card.ShowViewpoint(_viewpoints[i].Data);
            EmitLayout();
            return true;
        }

        /// <summary>Toca el «?» del mirador <paramref name="i"/> (tests y <c>adb</c>: lo mismo que su área táctil).</summary>
        public bool TapViewpoint(int i)
        {
            if (!Visible || i < 0 || i >= _viewpointCount || !ViewpointShown(i))
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
            return OpenViewpointCard(i);
        }

        /// <summary>
        /// El Atrás de Android con el Mapa abierto: cierra la tarjeta de la parada y, si no la hay, el Muestrario. True si cerró algo
        /// (si no, quien lo llama cierra el Mapa).
        /// </summary>
        public bool CloseTopSheet()
        {
            if (CardOpen)
            {
                _card!.Hide();
                return true;
            }

            if (SamplesSheetOpen)
            {
                CloseSamples();
                return true;
            }

            return false;
        }

        // ============================================================================================ ganchos

        partial void SamplesBuild(RectTransform slot)
        {
            // La pestaña: una píldora de madera con la mini cuia y «Muestrario · {k}/{n}», a la derecha de su fila.
            _samplesTab = UiBuilder.Relief("SamplesTab", slot, _art.CtaWood, OpenSamples);
            ReliefParts tab = _samplesTab.Value;
            _samplesTabIcon = UiBuilder.Image("Icon", tab.Face, _art.MapMiniCuia, Color.white);
            _samplesTabIcon.preserveAspect = true;
            _samplesTabLabel = UiBuilder.Label("Label", tab.Face, TextStyle.PillWood, UiText.MapSamples(0, 0), SamplesTabSp, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(_samplesTabLabel.rectTransform, _art.CtaWood);
            UiBuilder.AutoSize(_samplesTabLabel, UiBuilder.MinTextSp, SamplesTabSp);
            tab.Relief.Bind(tab.Face, tab.Base, tab.FaceImage, _art.CtaWood, null, null, _samplesTabLabel);
        }

        partial void SamplesOpen(IMapProgress progress)
        {
            RiverStopCatalog? stops = _stops;
            _closingMap = false;
            if (_samplesSheet != null)
            {
                _samplesSheet.gameObject.SetActive(false);
            }

            if (_card != null && _card.Visible)
            {
                _card.Root.gameObject.SetActive(false);
            }

            if (stops == null)
            {
                _samplesSlot.gameObject.SetActive(false);
                return;
            }

            int max = Mathf.Clamp(progress.MaxLevel, 0, WorldTable.LastLevel);
            CountSamples(stops, _map, max, out _samplesEarned, out _samplesTotal);
            CountFacts(stops, _map, max, out _factsSeen, out _factsTotal);
            _samplesTabLabel!.text = UiText.MapSamples(_samplesEarned, _samplesTotal);
            _samplesSlot.gameObject.SetActive(_samplesTab != null && _samplesTotal > 0);
            BuildViewpoints(max);
        }

        partial void SamplesSpace(MapSpace space)
        {
            bool world = _view == MapView.World;
            if (_viewpointLayer != null && _viewpointLayer.gameObject.activeSelf != world)
            {
                _viewpointLayer.gameObject.SetActive(world);
            }

            for (int i = 0; i < _viewpointCount; i++)
            {
                ViewpointMarker v = _viewpoints[i];
                v.Root.anchoredPosition = space.ToLocal(new Vector2(v.Data.X, v.Data.Y));
            }
        }

        partial void SamplesLayout(RectTransform slot)
        {
            if (_samplesTab != null)
            {
                // La píldora, a la medida de su texto (con la mini cuia), a la derecha de la fila y a todo su alto.
                ReliefParts tab = _samplesTab.Value;
                float rowW = slot.sizeDelta.x;
                const float iconW = ProceduralArt.MiniCuiaWidthDp * 0.75f;
                const float iconH = ProceduralArt.MiniCuiaHeightDp * 0.75f;
                float labelW = Mathf.Ceil(UiBuilder.MinTextWidth(_samplesTabLabel!));
                float w = Mathf.Min(rowW, Mathf.Max(MinTouchDp * 2f, 14f + iconW + 6f + labelW + 16f));
                UiBuilder.Place((RectTransform)tab.Button.transform, rowW - w, 0f, w, SamplesRowDp);
                float face = _art.CtaWood.FaceHeightDp > 0f ? _art.CtaWood.FaceHeightDp : _art.CtaWood.HeightDp;
                UiBuilder.Place(_samplesTabIcon!.rectTransform, 14f, Mathf.Max(0f, (face - iconH) * 0.5f), iconW, iconH);
                RectTransform label = _samplesTabLabel!.rectTransform;
                label.anchorMin = new Vector2(0f, label.anchorMin.y);
                label.anchorMax = new Vector2(1f, label.anchorMax.y);
                label.offsetMin = new Vector2(14f + iconW + 6f, label.offsetMin.y);
                label.offsetMax = new Vector2(-12f, label.offsetMax.y);
            }

            if (SamplesSheetOpen)
            {
                LayoutSamplesSheet();
            }

            if (_card != null)
            {
                _card.Layout(_canvasW, _canvasH, _safeTop, _safeBottom);
            }
        }

        partial void SamplesLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (SamplesTabShown)
            {
                ids.Add("samples");
                rects.Add((RectTransform)_samplesTab!.Value.Button.transform);
            }

            if (_view == MapView.World)
            {
                for (int i = 0; i < _viewpointCount; i++)
                {
                    ViewpointMarker v = _viewpoints[i];
                    Vector2 p = new Vector2(_offset.x + v.Root.anchoredPosition.x, _offset.y - v.Root.anchoredPosition.y);
                    if (v.Root.gameObject.activeSelf && p.x >= 0f && p.x <= _viewW && p.y >= 0f && p.y <= _viewH)
                    {
                        ids.Add("viewpoint_" + v.Data.Id);
                        rects.Add(v.Root);
                    }
                }
            }

            if (SamplesSheetOpen)
            {
                ids.Add("samples_back");
                rects.Add((RectTransform)_samplesBack!.Value.Button.transform);
                for (int i = 0; i < _sampleSlotCount; i++)
                {
                    SampleSlot s = _sampleSlots[i];
                    if (s.Button.interactable)
                    {
                        ids.Add(s.StopN >= 0 ? "sample_" + DevProbe.StopId(_stops!.At(s.StopN)!) : "sample_" + s.Viewpoint!.Id);
                        rects.Add((RectTransform)s.Button.transform);
                    }
                }
            }

            if (CardOpen)
            {
                ids.Add("card_close");
                rects.Add((RectTransform)_card!.CloseButton.transform);
                if (_card.OtherFactShown)
                {
                    ids.Add("card_other_fact");
                    rects.Add((RectTransform)_card.OtherFactButton.transform);
                }
            }
        }

        partial void SamplesClose()
        {
            _closingMap = true;
            if (_card != null && _card.Visible)
            {
                _card.Hide();
            }

            if (_samplesSheet != null)
            {
                _samplesSheet.gameObject.SetActive(false);
            }
        }

        partial void MarkersStopTapped(int n)
        {
            OpenStopCard(n);
        }

        private partial IEnumerable<TextMeshProUGUI> SamplesTexts()
        {
            if (_samplesTabLabel != null && SamplesTabShown)
            {
                yield return _samplesTabLabel;
            }

            foreach (TextMeshProUGUI t in SampleTexts())
            {
                yield return t;
            }
        }

        // ============================================================================================ construcción

        private StopCard EnsureCard()
        {
            if (_card == null)
            {
                _card = new StopCard(_root, _art, OnCardClosed);
            }

            _card.Root.SetAsLastSibling();
            return _card;
        }

        private void OnCardClosed()
        {
            if (Visible && !_closingMap)
            {
                EmitLayout();
            }
        }

        // El agua del primer plano del fondo de la parada: la del nivel (el catálogo) o, sin él, la de su mundo.
        private string StopWater(RiverStop stop)
        {
            string? water = LevelWater?.Invoke(stop.FirstLevel);
            return !string.IsNullOrEmpty(water) ? water! : WorldWaterOf(_stops!, stop.World);
        }

        private void EnsureSamplesSheet()
        {
            if (_samplesSheet != null)
            {
                return;
            }

            _samplesSheet = UiBuilder.Rect("SamplesSheet", _root);
            Image back = UiBuilder.Image("Background", _samplesSheet, null, BackgroundColor, true); // tapa el Mapa y sus toques
            UiBuilder.Stretch(back.rectTransform);

            _samplesHeader = UiBuilder.Sliced("HeaderPlate", _samplesSheet, _art.WoodPlate, Color.white);
            _samplesBack = UiBuilder.Relief("BackButton", _samplesHeader.transform, _art.DiscWood, CloseSamples);
            ReliefParts samplesBack = _samplesBack.Value;
            RectTransform backVisual = (RectTransform)samplesBack.Face.parent;
            backVisual.anchorMin = new Vector2(0.5f, 1f);
            backVisual.anchorMax = new Vector2(0.5f, 1f);
            backVisual.sizeDelta = new Vector2(_art.DiscWood.Painted ? _art.DiscWood.HeightDp : 40f, _art.DiscWood.HeightDp);
            backVisual.anchoredPosition = new Vector2(0f, -Mathf.Max(0f, (MinTouchDp - _art.DiscWood.HeightDp) * 0.5f));
            GlyphGraphic arrow = GlyphGraphic.Create("Arrow", samplesBack.Face, GlyphKind.BackArrow);
            UiBuilder.Place(arrow.rectTransform, 9f, 9f, 22f, 22f);
            samplesBack.Relief.Bind(samplesBack.Face, samplesBack.Base, samplesBack.FaceImage, _art.DiscWood, null, null, null);
            _samplesTitle = UiBuilder.Label("Title", _samplesHeader.transform, TextStyle.ModalTitle, string.Empty, SamplesTitleSp, Palette.Cream, TextAlignmentOptions.Left);
            UiBuilder.AutoSize(_samplesTitle, UiBuilder.MinTextSp, SamplesTitleSp);

            _samplesCount = UiBuilder.Label("FactsCount", _samplesSheet, TextStyle.PlainBold, string.Empty, SamplesCountSp, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.AutoSize(_samplesCount, UiBuilder.MinTextSp, SamplesCountSp);

            _samplesViewport = UiBuilder.Rect("Viewport", _samplesSheet);
            _samplesViewport.gameObject.AddComponent<RectMask2D>();
            _samplesViewport.gameObject.AddComponent<HitArea>().raycastTarget = true; // el arrastre en los huecos también desplaza
            _samplesContent = UiBuilder.Rect("Content", _samplesViewport);
            _samplesScroll = _samplesViewport.gameObject.AddComponent<ScrollRect>();
            _samplesScroll.viewport = _samplesViewport;
            _samplesScroll.content = _samplesContent;
            _samplesScroll.horizontal = false;
            _samplesScroll.vertical = true;
            _samplesScroll.movementType = ScrollRect.MovementType.Clamped;
            _samplesSheet.gameObject.SetActive(false);
        }

        // Las muestras del catálogo (una por parada, en orden, y detrás las de los miradores de cada mundo) con lo que se sabe con
        // max_level; los textos, aquí (asignan su cadena al abrir).
        private void BuildSamples()
        {
            RiverStopCatalog stops = _stops!;
            int max = Mathf.Clamp(_progress!.MaxLevel, 0, WorldTable.LastLevel);
            _samplesTitle!.text = UiText.MapSamples(_samplesEarned, _samplesTotal);
            _samplesCount!.text = UiText.MapFactsCount(_factsSeen, _factsTotal);
            int count = 0;
            int lastWorld = 0;
            for (int n = 0; n < stops.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                if (stop != null)
                {
                    lastWorld = Mathf.Max(lastWorld, stop.World);
                }
            }

            for (int world = 1; world <= lastWorld; world++)
            {
                for (int n = 0; n < stops.Count; n++)
                {
                    RiverStop? stop = stops.At(n);
                    if (stop == null || stop.World != world)
                    {
                        continue;
                    }

                    SampleSlot s = SlotAt(count++);
                    bool earned = StopReached(stop, max);
                    s.SetStop(stop, world, earned);
                }

                if (_map != null)
                {
                    for (int i = 0; i < _map.Viewpoints.Count; i++)
                    {
                        RiverViewpoint v = _map.Viewpoints[i];
                        if (WorldTable.WorldOf(Mathf.Clamp(v.RevealOn, 1, WorldTable.LastLevel)) != world)
                        {
                            continue;
                        }

                        SampleSlot s = SlotAt(count++);
                        s.SetViewpoint(v, world, ViewpointRevealed(v, max), WorldWaterOf(stops, world));
                    }
                }
            }

            _sampleSlotCount = count;
            for (int i = 0; i < _sampleSlots.Count; i++)
            {
                bool on = i < count;
                if (_sampleSlots[i].Root.gameObject.activeSelf != on)
                {
                    _sampleSlots[i].Root.gameObject.SetActive(on);
                }
            }
        }

        private SampleSlot SlotAt(int i)
        {
            while (_sampleSlots.Count <= i)
            {
                int index = _sampleSlots.Count;
                SampleSlot? created = null;
                Button button = UiBuilder.BareButton("Sample_" + index, _samplesContent!, () =>
                {
                    if (created != null)
                    {
                        TapSampleSlot(created);
                    }
                });
                created = new SampleSlot(button, new SampleCuia("Cuia", button.transform, _art, SampleScale), _art);
                _sampleSlots.Add(created);
            }

            return _sampleSlots[i];
        }

        private void TapSampleSlot(SampleSlot s)
        {
            if (!SamplesSheetOpen)
            {
                return;
            }

            NoteTouch();
            _stopsTapped++;
            if (s.StopN >= 0)
            {
                OpenStopCard(s.StopN);
            }
            else if (s.Viewpoint != null)
            {
                int i = ViewpointIndexOf(s.Viewpoint);
                if (i >= 0)
                {
                    OpenViewpointCard(i);
                }
            }
        }

        /// <summary>Toca la muestra <paramref name="i"/> del Muestrario abierto (tests y <c>adb</c>: lo mismo que su área táctil).</summary>
        public bool TapSample(int i)
        {
            if (!SamplesSheetOpen || i < 0 || i >= _sampleSlotCount || !_sampleSlots[i].Button.interactable)
            {
                return false;
            }

            TapSampleSlot(_sampleSlots[i]);
            return true;
        }

        private int ViewpointIndexOf(RiverViewpoint v)
        {
            for (int i = 0; i < _viewpointCount; i++)
            {
                if (ReferenceEquals(_viewpoints[i].Data, v))
                {
                    return i;
                }
            }

            return -1;
        }

        // Los «?» de los miradores del Mapa: uno por mirador de la geometría, visible desde su reveal_on (en la vista Mundo).
        private void BuildViewpoints(int max)
        {
            RiverMap? map = _map;
            if (map == null)
            {
                _viewpointCount = 0;
                return;
            }

            if (_viewpointLayer == null)
            {
                _viewpointLayer = UiBuilder.Rect("Viewpoints", _dynamicLayer);
                FillTopLeft(_viewpointLayer);
            }

            _viewpointLayer.SetAsLastSibling();
            int count = map.Viewpoints.Count;
            while (_viewpoints.Count < count)
            {
                int index = _viewpoints.Count;
                Button button = UiBuilder.BareButton("Viewpoint_" + index, _viewpointLayer, () => TapViewpoint(index));
                var root = (RectTransform)button.transform;
                root.anchorMin = new Vector2(0f, 1f);
                root.anchorMax = new Vector2(0f, 1f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.sizeDelta = new Vector2(MinTouchDp, MinTouchDp);
                Image glyph = UiBuilder.Image("Glyph", root, _art.MapViewpoint, Color.white);
                glyph.preserveAspect = true;
                RectTransform g = glyph.rectTransform;
                g.anchorMin = new Vector2(0.5f, 0.5f);
                g.anchorMax = new Vector2(0.5f, 0.5f);
                g.pivot = new Vector2(0.5f, 0.5f);
                g.sizeDelta = new Vector2(ProceduralArt.MapViewpointDp, ProceduralArt.MapViewpointDp);
                _viewpoints.Add(new ViewpointMarker(root, button));
            }

            _viewpointCount = count;
            for (int i = 0; i < _viewpoints.Count; i++)
            {
                ViewpointMarker v = _viewpoints[i];
                bool on = i < count && ViewpointRevealed(map.Viewpoints[i], max);
                if (i < count)
                {
                    v.Data = map.Viewpoints[i];
                    v.Root.gameObject.name = "Viewpoint_" + v.Data.Id;
                }

                if (v.Root.gameObject.activeSelf != on)
                {
                    v.Root.gameObject.SetActive(on);
                }
            }
        }

        // ============================================================================================ maquetación

        // La hoja del Muestrario: la cabecera (←, título), el contador y, debajo, las filas de los mundos en su visor.
        private void LayoutSamplesSheet()
        {
            float w = _canvasW;
            float h = _canvasH;
            UiBuilder.Place(_samplesSheet!, 0f, 0f, w, h);
            UiBuilder.Place(_samplesHeader!.rectTransform, -8f, -8f, w + 16f, _safeTop + HeaderDp + 8f);
            float bandTop = _safeTop + 8f;
            UiBuilder.Place((RectTransform)_samplesBack!.Value.Button.transform, 16f, bandTop + ((HeaderDp - MinTouchDp) * 0.5f), MinTouchDp, MinTouchDp);
            float titleX = 16f + MinTouchDp + 8f;
            UiBuilder.Place(_samplesTitle!.rectTransform, titleX, bandTop + 4f, Mathf.Max(48f, w + 16f - titleX - 16f), HeaderDp - 8f);

            float countTop = _safeTop + HeaderDp + 12f;
            float countH = Mathf.Max(24f, Mathf.Ceil(UiBuilder.MinTextHeight(_samplesCount!, w - 32f)) + 4f);
            UiBuilder.Place(_samplesCount!.rectTransform, 16f, countTop, w - 32f, countH);
            float viewTop = countTop + countH + 8f;
            float viewH = Mathf.Max(MinTouchDp, h - _safeBottom - viewTop);
            UiBuilder.Place(_samplesViewport!, 0f, viewTop, w, viewH);

            // Filas: el nombre del mundo y, de SamplesPerShelf en SamplesPerShelf, las muestras de pie en su repisa.
            float contentW = w - 32f;
            float cell = contentW / SamplesPerShelf;
            float y = 4f;
            int labels = 0;
            int shelves = 0;
            int i = 0;
            while (i < _sampleSlotCount)
            {
                int world = _sampleSlots[i].World;
                TextMeshProUGUI label = WorldLabelAt(labels++);
                label.text = UiText.WorldName(world);
                float labelH = Mathf.Max(22f, Mathf.Ceil(UiBuilder.MinTextHeight(label, contentW)) + 2f);
                UiBuilder.Place(label.rectTransform, 16f, y, contentW, labelH);
                y += labelH + 2f;
                int shelf = 0;
                while (i < _sampleSlotCount && _sampleSlots[i].World == world)
                {
                    int col = 0;
                    for (; col < SamplesPerShelf && i < _sampleSlotCount && _sampleSlots[i].World == world; col++, i++)
                    {
                        SampleSlot s = _sampleSlots[i];
                        s.Shelf = shelves;
                        float cx = 16f + (cell * (col + 0.5f));
                        s.Place(cx, y);
                    }

                    Image board = ShelfAt(shelves++);
                    UiBuilder.Place(board.rectTransform, 8f, y + SampleTouchDp - 4f, w - 16f, ShelfDp);
                    board.rectTransform.SetAsFirstSibling(); // las repisas detrás de las muestras
                    y += SampleTouchDp + ShelfDp - 4f + 6f;
                    shelf++;
                }

                y += 8f;
            }

            for (int k = labels; k < _worldLabels.Count; k++)
            {
                _worldLabels[k].gameObject.SetActive(false);
            }

            for (int k = shelves; k < _shelves.Count; k++)
            {
                _shelves[k].gameObject.SetActive(false);
            }

            _worldLabelCount = labels;
            _shelfCount = shelves;
            UiBuilder.Place(_samplesContent!, 0f, 0f, w, Mathf.Max(viewH, y + 8f));
        }

        private TextMeshProUGUI WorldLabelAt(int i)
        {
            while (_worldLabels.Count <= i)
            {
                TextMeshProUGUI label = UiBuilder.Label("World_" + _worldLabels.Count, _samplesContent!, TextStyle.ModalTitle, string.Empty, WorldLabelSp, Palette.Cream, TextAlignmentOptions.Left);
                UiBuilder.AutoSize(label, UiBuilder.MinTextSp, WorldLabelSp);
                _worldLabels.Add(label);
            }

            _worldLabels[i].gameObject.SetActive(true);
            return _worldLabels[i];
        }

        private Image ShelfAt(int i)
        {
            while (_shelves.Count <= i)
            {
                _shelves.Add(UiBuilder.Sliced("Shelf_" + _shelves.Count, _samplesContent!, _art.Shelf, Color.white));
            }

            _shelves[i].gameObject.SetActive(true);
            return _shelves[i];
        }

        // ============================================================================================ piezas

        // Una muestra del Muestrario: su área táctil de 48 × SampleTouchDp dp, la mini cuia de pie al fondo y, en la de un mirador, su
        // «?» arriba a la derecha.
        private sealed class SampleSlot
        {
            private readonly Image _badge;

            public SampleSlot(Button button, SampleCuia cuia, ProceduralArt art)
            {
                Button = button;
                Cuia = cuia;
                Root = (RectTransform)button.transform;
                _badge = UiBuilder.Image("Badge", Root, art.MapViewpoint, Color.white);
                _badge.preserveAspect = true;
            }

            public Button Button { get; }

            public RectTransform Root { get; }

            public SampleCuia Cuia { get; }

            public int StopN { get; private set; } = -1;

            public RiverViewpoint? Viewpoint { get; private set; }

            public int World { get; private set; }

            public bool Earned { get; private set; }

            public int Shelf { get; set; }

            public void SetStop(RiverStop stop, int world, bool earned)
            {
                StopN = stop.N;
                Viewpoint = null;
                World = world;
                Earned = earned;
                Root.gameObject.name = "Sample_" + stop.Id;
                Cuia.Set(stop.Sample, earned, true);
                _badge.gameObject.SetActive(false);
                Button.interactable = true; // las futuras también: su silueta y «Nivel n»
            }

            public void SetViewpoint(RiverViewpoint viewpoint, int world, bool revealed, string water)
            {
                StopN = -1;
                Viewpoint = viewpoint;
                World = world;
                Earned = revealed;
                Root.gameObject.name = "Sample_" + viewpoint.Id;
                Cuia.Set(water, revealed, true);
                _badge.gameObject.SetActive(true);
                _badge.color = revealed ? Color.white : new Color(1f, 1f, 1f, HiddenBadgeAlpha);
                Button.interactable = revealed; // el secreto no se toca hasta que el progreso lo revela
            }

            // Centro de la celda en cx (dp) y su borde de arriba en y (dp desde arriba del contenido).
            public void Place(float cx, float y)
            {
                UiBuilder.Place(Root, cx - (MinTouchDp * 0.5f), y, MinTouchDp, SampleTouchDp);
                Cuia.Place((MinTouchDp - Cuia.WidthDp) * 0.5f, SampleTouchDp - Cuia.HeightDp);
                UiBuilder.Place(_badge.rectTransform, MinTouchDp - SampleBadgeDp, 0f, SampleBadgeDp, SampleBadgeDp);
            }
        }

        // El «?» de un mirador en el Mapa: su área táctil de 48 dp con el dibujo de 28 en medio.
        private sealed class ViewpointMarker
        {
            public ViewpointMarker(RectTransform root, Button button)
            {
                Root = root;
                Button = button;
                Data = null!;
            }

            public RectTransform Root { get; }

            public Button Button { get; }

            public RiverViewpoint Data { get; set; }
        }
    }
}
