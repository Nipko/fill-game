#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// «Datos del río» en Ajustes › «Sobre la Amazonía» (tarea B14; 07 §8.2: «una sección Sobre la Amazonía en Ajustes con la
    /// fuente de cada Dato del río»; 06 §4.4 y §4.5): bajo las fichas de Frutas del río, el subtítulo <c>settings_river_facts</c>
    /// [P] y, por cada parada ya alcanzada con dato de llegada con texto (en el orden del río, sin repetir un dato), el nombre del
    /// lugar, el dato y «Fuente: …» (<c>map_card_source</c>, la fila <c>&lt;clave del dato&gt;.source</c> de <c>facts.csv</c>; D-028).
    /// Las líneas de fruta ya llevan sus fuentes en su ficha («Fuentes», F4). Lo que aún no se ha visto no sale (sin destripar el
    /// viaje), y un dato sin texto en el idioma de la UI (en release, sin aprobar) tampoco (D-018, Q10-A). Solo lectura: sin toques.
    /// Se compone al abrir Ajustes, nunca por fotograma.
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Lugar de un dato del río (sp).</summary>
        public const float RiverFactPlaceSp = 14f;

        /// <summary>Texto de un dato del río (sp).</summary>
        public const float RiverFactSp = 14f;

        /// <summary>Fuente de un dato del río (sp; 12, como la de la tarjeta de la parada).</summary>
        public const float RiverFactSourceSp = 12f;

        /// <summary>Hueco entre dos datos del río (dp).</summary>
        public const float RiverFactGapDp = 10f;

        private readonly List<RiverFactRow> _riverFacts = new List<RiverFactRow>();
        private readonly List<string> _riverFactKeys = new List<string>();
        private TextMeshProUGUI _riverFactsTitle = null!;
        private int _riverFactCount;

        /// <summary>Catálogo de paradas del Mapa del río (lo da <c>GameBootstrap</c>; null = sin «Datos del río»).</summary>
        public RiverStopCatalog? RiverStops { get; set; }

        /// <summary>Datos del río a la vista, en orden.</summary>
        public int RiverFactCount => _riverFactCount;

        /// <summary>Se ve el subtítulo «Datos del río».</summary>
        public bool RiverFactsShown => AboutShown && _riverFactCount > 0;

        /// <summary>Clave del dato <paramref name="i"/>.</summary>
        public string RiverFactKey(int i)
        {
            return RiverFactRowAt(i).Key;
        }

        /// <summary>Lugar del dato <paramref name="i"/>.</summary>
        public string RiverFactPlace(int i)
        {
            return RiverFactRowAt(i).Place.text;
        }

        /// <summary>Texto del dato <paramref name="i"/>.</summary>
        public string RiverFactText(int i)
        {
            return RiverFactRowAt(i).Fact.text;
        }

        /// <summary>«Fuente: …» del dato <paramref name="i"/> («» si el dato no tiene fuente con texto).</summary>
        public string RiverFactSource(int i)
        {
            RiverFactRow row = RiverFactRowAt(i);
            return row.Source.gameObject.activeSelf ? row.Source.text : string.Empty;
        }

        /// <summary>Subtítulo «Datos del río».</summary>
        public string RiverFactsTitle => _riverFactsTitle.text;

        /// <summary>
        /// Claves de los datos de llegada de las paradas alcanzadas con <paramref name="maxLevel"/> niveles ganados que tienen texto
        /// (<see cref="Loc.Fact"/>), en el orden del catálogo y sin repetir, en <paramref name="into"/> (que vacía). Devuelve cuántas.
        /// </summary>
        public static int SeenRiverFacts(RiverStopCatalog stops, int maxLevel, List<string> into)
        {
            if (stops == null || into == null)
            {
                throw new ArgumentNullException(stops == null ? nameof(stops) : nameof(into));
            }

            into.Clear();
            for (int n = 0; n < stops.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                string? key = stop?.ArrivalFact;
                if (stop == null || key == null || !RiverMapScreen.StopReached(stop, maxLevel) || into.Contains(key) || Loc.Fact(key).Length == 0)
                {
                    continue;
                }

                into.Add(key);
            }

            return into.Count;
        }

        /// <summary>Desplaza Ajustes hasta abajo (capturas: el final de «Datos del río»). Sin desplazamiento, nada.</summary>
        internal void ScrollToBottomForTests()
        {
            if (_scroll.vertical)
            {
                _scroll.verticalNormalizedPosition = 0f;
                UpdateFades();
            }
        }

        private void BuildRiverFacts()
        {
            _riverFactsTitle = UiBuilder.Label("RiverFactsTitle", AboutSlot, TextStyle.Body, UiText.SettingsRiverFacts, FruitsTitleSp, FruitSheet.SoftInk, TextAlignmentOptions.MidlineLeft);
            UiBuilder.AutoSize(_riverFactsTitle, MinTextSp, FruitsTitleSp);
        }

        // Los datos con el progreso de ahora (al abrir Ajustes): el lugar de la parada en que se llegó a cada uno.
        private void RefreshRiverFacts()
        {
            _riverFactCount = 0;
            RiverStopCatalog? stops = RiverStops;
            IMapProgress? progress = FruitProgress;
            if (stops != null && progress != null)
            {
                SeenRiverFacts(stops, progress.MaxLevel, _riverFactKeys);
                for (int i = 0; i < _riverFactKeys.Count; i++)
                {
                    string key = _riverFactKeys[i];
                    RiverStop? stop = FirstStopWith(stops, key);
                    if (stop == null)
                    {
                        continue;
                    }

                    if (_riverFacts.Count <= _riverFactCount)
                    {
                        _riverFacts.Add(new RiverFactRow(AboutSlot, _riverFacts.Count));
                    }

                    string source = StopCard.SourceOf(key);
                    _riverFacts[_riverFactCount].Bind(key, UiText.StopName(stop), Loc.Fact(key), source.Length > 0 ? UiText.MapCardSource(source) : string.Empty);
                    _riverFactCount++;
                }
            }

            for (int i = 0; i < _riverFacts.Count; i++)
            {
                _riverFacts[i].SetActive(i < _riverFactCount);
            }

            _riverFactsTitle.gameObject.SetActive(_riverFactCount > 0);
        }

        // Desde y (en la sección), el subtítulo y los datos; devuelve dónde acaban. Cada dato es una «fila» de Ajustes (con su alto)
        // para el corte del visor (CutAtRow).
        private float LayoutRiverFacts(float top, float local, float inner, float s)
        {
            if (_riverFactCount == 0)
            {
                return local;
            }

            float subH = Mathf.Round((FruitsTitleSp * s) + 8f);
            _riverFactsTitle.fontSizeMax = FruitsTitleSp * s;
            _riverFactsTitle.fontSizeMin = MinTextSp;
            UiBuilder.Place(_riverFactsTitle.rectTransform, 16f, local, inner, subH);
            local += subH + RowGapDp;
            for (int i = 0; i < _riverFactCount; i++)
            {
                float h = _riverFacts[i].Layout(16f, local, inner, s);
                _rowTops.Add(top + local);
                _rowHeights.Add(h);
                local += h + RiverFactGapDp;
            }

            return local - RiverFactGapDp;
        }

        private IEnumerable<TextMeshProUGUI> RiverFactTexts()
        {
            if (_riverFactCount == 0)
            {
                yield break;
            }

            yield return _riverFactsTitle;
            for (int i = 0; i < _riverFactCount; i++)
            {
                yield return _riverFacts[i].Place;
                yield return _riverFacts[i].Fact;
                if (_riverFacts[i].Source.gameObject.activeSelf)
                {
                    yield return _riverFacts[i].Source;
                }
            }
        }

        private static RiverStop? FirstStopWith(RiverStopCatalog stops, string key)
        {
            for (int n = 0; n < stops.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                if (stop != null && string.Equals(stop.ArrivalFact, key, StringComparison.Ordinal))
                {
                    return stop;
                }
            }

            return null;
        }

        private RiverFactRow RiverFactRowAt(int i)
        {
            if (i < 0 || i >= _riverFactCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i), i, "Ajustes enseña " + _riverFactCount + " datos del río.");
            }

            return _riverFacts[i];
        }

        // Un dato del río: el lugar en negrita, el dato y su fuente debajo, en la tinta de las opciones; sin placa ni toque.
        private sealed class RiverFactRow
        {
            public RiverFactRow(Transform parent, int index)
            {
                Place = UiBuilder.Label("RiverFactPlace_" + index, parent, TextStyle.PlainBold, string.Empty, RiverFactPlaceSp, Palette.FamilyWood.Deep, TextAlignmentOptions.TopLeft);
                Place.textWrappingMode = TextWrappingModes.Normal;
                Fact = UiBuilder.Label("RiverFact_" + index, parent, TextStyle.Body, string.Empty, RiverFactSp, Palette.FamilyWood.Deep, TextAlignmentOptions.TopLeft);
                Fact.textWrappingMode = TextWrappingModes.Normal;
                Source = UiBuilder.Label("RiverFactSource_" + index, parent, TextStyle.Body, string.Empty, RiverFactSourceSp, FruitSheet.SoftInk, TextAlignmentOptions.TopLeft);
                Source.textWrappingMode = TextWrappingModes.Normal;
            }

            public string Key { get; private set; } = string.Empty;

            public TextMeshProUGUI Place { get; }

            public TextMeshProUGUI Fact { get; }

            public TextMeshProUGUI Source { get; }

            public void Bind(string key, string place, string fact, string source)
            {
                Key = key;
                Place.text = place;
                Fact.text = fact;
                Source.text = source;
            }

            public void SetActive(bool on)
            {
                Place.gameObject.SetActive(on && Place.text.Length > 0);
                Fact.gameObject.SetActive(on);
                Source.gameObject.SetActive(on && Source.text.Length > 0);
            }

            // Coloca el dato desde (x, y) a lo ancho de w, con la escala de texto s; devuelve su alto.
            public float Layout(float x, float y, float w, float s)
            {
                float h = 0f;
                h += PlaceLabel(Place, x, y + h, w, RiverFactPlaceSp * s);
                h += PlaceLabel(Fact, x, y + h, w, RiverFactSp * s);
                h += PlaceLabel(Source, x, y + h, w, RiverFactSourceSp * s);
                return Mathf.Max(1f, h);
            }

            private static float PlaceLabel(TextMeshProUGUI label, float x, float y, float w, float size)
            {
                if (!label.gameObject.activeSelf)
                {
                    return 0f;
                }

                label.enableAutoSizing = false;
                label.fontSize = size;
                float h = Mathf.Ceil(label.GetPreferredValues(label.text, w, float.PositiveInfinity).y) + 2f;
                UiBuilder.Place(label.rectTransform, x, y, w, h);
                return h;
            }
        }
    }
}
