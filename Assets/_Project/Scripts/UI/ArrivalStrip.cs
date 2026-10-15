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
    /// Llegada a la parada del Mapa del río en la ranura del Dato del río de la victoria (02 §6, filas Llegada y «Dato del río»;
    /// D-013 punto 8; T6-A; propuesta del Mapa §4.2, §5.3, §6.3 y §8.2; tarea M1.6). Es un proveedor de
    /// <see cref="VictoryLineSlot"/> y enseña lo que diga el árbitro (<see cref="VictoryLineArbiter"/>):
    /// <list type="bullet">
    /// <item><see cref="ArrivalKind.Arrival"/>: la llegada completa (política Canon): la fila del agua de
    /// <see cref="RowDp"/> dp con el tramo que termina de llenarse, la canoa de Capi que llega y la mini cuia del sitio que se
    /// llena, y debajo una sola placa con el retrato de TiTino (<see cref="PortraitDp"/> dp), la bienvenida del país en los
    /// cambios de país (80 y 140; P-M7 a), «{lugar} · {país} · río {río}» (<c>map_arrived</c>, con los nombres por
    /// <see cref="Loc.Fact(string)"/>) y el dato a <see cref="FactSp"/> sp.</item>
    /// <item><see cref="ArrivalKind.Place"/>: solo la placa del lugar (política SingleKey, o un hito en el que habla TiTino).</item>
    /// <item><see cref="ArrivalKind.Deferred"/>: en el <c>10k+1</c> tras un hito en el que habló TiTino, solo la placa del dato
    /// desplazado con el retrato, que entra con «Siguiente» como la línea de fruta (<see cref="VictoryLineSlot.FruitLineAlpha"/>).</item>
    /// </list>
    /// <para><b>Pregunta 11, opción (a) [P]:</b> la llegada va compactada (la fila del agua y una sola placa con el retrato, el
    /// lugar a <see cref="PlaceSp"/> sp y el dato a <see cref="FactSp"/>; la bienvenida, dentro de la misma placa). Las medidas de
    /// M1.3b dicen que ni compactada cabe siempre en el alto de la línea de fruta, así que la ranura reserva
    /// máx(línea de fruta, <see cref="NeedDp"/>), con <see cref="NeedDp"/> = la llegada más alta del catálogo en el idioma y la
    /// escala de ahora: nada se recorta y «Siguiente» sigue en el mismo sitio en todas las victorias (Q7-A).</para>
    /// <para><b>Tiempos:</b> la franja dura <see cref="MapConfig.ArrivalStripMs"/> ms desde que termina de subir el panel
    /// (<see cref="GameScreen.VictoryPanelUpMs"/>); un toque la completa y el texto se queda hasta que se cierra la victoria
    /// (D-011). Nunca toca «Siguiente», «Inicio» ni la Cosecha doble. Con Reducir movimiento, el estado final con un fundido de
    /// <see cref="MapConfig.ReducedMotionFadeMs"/> ms. Nada de lo que hace por fotograma asigna memoria: los textos y las medidas
    /// se hacen al enseñarla.</para>
    /// </summary>
    internal sealed class ArrivalStrip : IVictoryLineProvider
    {
        /// <summary>Fila del agua, la canoa y la mini cuia (02 §6; tarea M1.6).</summary>
        public const float RowDp = 28f;

        /// <summary>De la fila a la placa [P].</summary>
        public const float GapDp = 4f;

        /// <summary>Retrato de TiTino de la placa del dato (02 §6; <see cref="ProceduralArt.SlothPortraitMinDp"/>).</summary>
        public const float PortraitDp = ProceduralArt.SlothPortraitMinDp;

        /// <summary>Del retrato al texto [P].</summary>
        public const float PortraitGapDp = 8f;

        /// <summary>Bienvenida y lugar: Nunito Bold de 12 sp, el mínimo (pregunta 11, a; maqueta de M1.3b) [P].</summary>
        public const float PlaceSp = UiBuilder.MinTextSp;

        /// <summary>Dato: Nunito Regular de 14 sp, nunca menos de 12 (02 §6).</summary>
        public const float FactSp = VictoryLineSlot.TextSp;

        /// <summary>Entre las líneas de la placa [P].</summary>
        public const float LineGapDp = 2f;

        /// <summary>Margen bajo la placa (el de la ranura).</summary>
        public const float BottomDp = VictoryLineSlot.BottomDp;

        /// <summary>El tramo llega lleno a 9/10 (el 10k: nueve niveles del tramo ganados; <see cref="RiverJourney.LegFill"/>).</summary>
        public const float LegFromFill = 0.9f;

        /// <summary>El agua termina de llenar el tramo en los primeros 400 ms de la franja [P].</summary>
        public const float WaterFillMs = 400f;

        /// <summary>La canoa sale a los 100 ms de la franja [P].</summary>
        public const float CanoeFromMs = 100f;

        /// <summary>La canoa tarda 600 ms en llegar a la parada [P].</summary>
        public const float CanoeMs = 600f;

        /// <summary>La placa del lugar entra (fundido de <see cref="PlateFadeMs"/>) a los 600 ms de la franja [P].</summary>
        public const float PlaceInMs = 600f;

        /// <summary>La línea del dato entra a los 800 ms de la franja, con el gesto <c>sloth_present</c> del retrato [P].</summary>
        public const float FactInMs = 800f;

        /// <summary>Fundido de entrada de las líneas de la placa [P].</summary>
        public const float PlateFadeMs = 200f;

        /// <summary>Gesto <c>sloth_present</c> provisional (TI4 hará el rig): el retrato entra con un rebote de 300 ms [P].</summary>
        public const float PresentMs = 300f;

        /// <summary>Parpadeo del retrato al presentar el dato: de 1.000 a 1.120 ms de la franja [P].</summary>
        public const float BlinkFromMs = 1000f;

        /// <summary>Duración del parpadeo [P].</summary>
        public const float BlinkMs = 120f;

        /// <summary>Opacidad del fondo de la placa (madera del panel, apenas marcada) [P].</summary>
        public const float PlateAlpha = 0.10f;

        private const float MiniCuiaW = RowDp * ProceduralArt.MiniCuiaWidthDp / ProceduralArt.MiniCuiaHeightDp;
        private const float CanoeW = RowDp * ProceduralArt.MapCanoeWidthDp / ProceduralArt.MapCanoeHeightDp;

        private readonly ProceduralArt _art;
        private readonly Func<RiverStopCatalog?> _stops;
        private readonly Action<ArrivalStrip>? _started;
        private readonly Action<ArrivalStrip>? _completed;
        private readonly List<NeedEntry> _needs = new List<NeedEntry>(4);
        private readonly TextMeshProUGUI _measureBold;
        private readonly TextMeshProUGUI _measureBody;

        private RectTransform? _content;
        private Button? _root;
        private CanvasGroup? _group;
        private Image? _plateBack;
        private Image? _track;
        private Image? _waterA;
        private Image? _waterB;
        private Image? _waterLine;
        private Image? _canoe;
        private Image? _miniWaterA;
        private Image? _miniWaterB;
        private Image? _miniGlass;
        private Image? _portrait;
        private TextMeshProUGUI? _welcomeLabel;
        private TextMeshProUGUI? _placeLabel;
        private TextMeshProUGUI? _factLabel;
        private Sprite? _portraitOpen;
        private Sprite? _portraitClosed;
        private float _width;
        private float _height;
        private float _trackW;
        private float _canoeFrom;
        private float _canoeTo;
        private float _since = -1f;
        private float _elapsed = -1f;
        private bool _reduceMotion;
        private bool _completedFired;
        private bool _startedFired;
        private bool _mixed;

        /// <summary>
        /// Crea la franja. <paramref name="measureParent"/> aloja las etiquetas de medir (ocultas); <paramref name="stops"/> da el
        /// catálogo de paradas de ahora; <paramref name="started"/> se llama cuando empieza la franja (tras la subida del panel) y
        /// <paramref name="completed"/> cuando termina, sola o por un toque (las sondas de GameScreen.Arrival.cs).
        /// </summary>
        public ArrivalStrip(ProceduralArt art, RectTransform measureParent, Func<RiverStopCatalog?> stops, Action<ArrivalStrip>? started,
            Action<ArrivalStrip>? completed)
        {
            _art = art;
            _stops = stops;
            _started = started;
            _completed = completed;
            RectTransform measure = UiBuilder.Rect("ArrivalMeasure", measureParent);
            _measureBold = UiBuilder.Label("MeasureBold", measure, TextStyle.PlainBold, string.Empty, PlaceSp, new Color(0f, 0f, 0f, 0f), TextAlignmentOptions.TopLeft);
            _measureBold.textWrappingMode = TextWrappingModes.Normal;
            _measureBody = UiBuilder.Label("MeasureBody", measure, TextStyle.Body, string.Empty, FactSp, new Color(0f, 0f, 0f, 0f), TextAlignmentOptions.TopLeft);
            _measureBody.textWrappingMode = TextWrappingModes.Normal;
            measure.gameObject.SetActive(false);
        }

        /// <summary>La llegada existe en esta partida (false: tests de la ranura sola). Cuenta desde la siguiente maquetación.</summary>
        public bool Enabled { get; set; } = true;

        /// <inheritdoc />
        public bool SlotActive => Enabled && _stops() != null;

        /// <summary>Qué enseña en la victoria de ahora.</summary>
        public ArrivalKind Kind { get; private set; }

        /// <summary>La parada a la que se llega en la victoria de ahora (null en <see cref="ArrivalKind.Deferred"/> y sin llegada).</summary>
        public RiverStop? Stop { get; private set; }

        /// <summary>Nivel de la victoria que se enseña (0 sin ninguna).</summary>
        public int Level { get; private set; }

        /// <summary>
        /// Clave del dato que se ve (el de la llegada o el desplazado), o null si no sale ninguno o su texto está vacío (sin
        /// traducir o sin aprobar en release): el <c>fact_id</c> de <c>river_fact_shown</c>.
        /// </summary>
        public string? ShownFactKey { get; private set; }

        /// <summary>Lleva la bienvenida del país (paradas que izan bandera: 80 y 140; P-M7 a).</summary>
        public bool Welcome { get; private set; }

        /// <summary>Texto de la bienvenida (vacío sin ella).</summary>
        public string WelcomeText { get; private set; } = string.Empty;

        /// <summary>Texto de la placa del lugar, «{lugar} · {país} · río {río}» (vacío si falta algún nombre).</summary>
        public string PlaceText { get; private set; } = string.Empty;

        /// <summary>Texto del dato (vacío sin él).</summary>
        public string FactText { get; private set; } = string.Empty;

        /// <summary>Alto que ocupa lo que se enseña ahora (dp; 0 sin nada).</summary>
        public float ShownHeightDp { get; private set; }

        /// <summary>Alto del contenido de la ranura en la última maquetación (dp).</summary>
        public float SlotHeightDp => _height;

        /// <summary>La franja ya empezó (tras la subida del panel).</summary>
        public bool Started => _startedFired;

        /// <summary>La franja terminó (sola, por un toque o al cerrarse la victoria).</summary>
        public bool Completed => _completedFired;

        /// <summary>La franja no se vio entera: un toque la completó o la victoria se cerró antes (<c>skipped</c> de <c>af_stop_arrive</c>).</summary>
        public bool Skipped { get; private set; }

        /// <summary>Cómo terminó: <c>time</c>, <c>tap</c> o <c>close</c> (vacío mientras no termina).</summary>
        public string CompletedBy { get; private set; } = string.Empty;

        /// <summary>Con Reducir movimiento en la victoria de ahora.</summary>
        public bool ReduceMotion => _reduceMotion;

        /// <summary>Milisegundos desde que empezó la franja (−1 antes).</summary>
        public float ElapsedMs => _elapsed;

        /// <summary>Opacidad de toda la franja (fundido de Reducir movimiento y del dato desplazado).</summary>
        public float Alpha => _group != null ? _group.alpha : 0f;

        /// <summary>Llenado del tramo (de <see cref="LegFromFill"/> a 1).</summary>
        public float WaterFill { get; private set; }

        /// <summary>Llenado de la mini cuia (0-1).</summary>
        public float SampleFill { get; private set; }

        /// <summary>Avance de la canoa hasta la parada (0-1).</summary>
        public float CanoeProgress { get; private set; }

        /// <summary>Opacidad de la línea del lugar (y de la bienvenida).</summary>
        public float PlaceAlpha => _placeLabel != null ? _placeLabel.alpha : 0f;

        /// <summary>Opacidad de la línea del dato (y del retrato).</summary>
        public float FactAlpha => _factLabel != null ? _factLabel.alpha : 0f;

        /// <summary>El área táctil de la franja (un toque la completa).</summary>
        public Button? TapArea => _root;

        /// <summary>Fila del agua: el tramo (tests y capturas).</summary>
        public RectTransform? Track => _track != null ? _track.rectTransform : null;

        /// <summary>La canoa de Capi.</summary>
        public RectTransform? Canoe => _canoe != null ? _canoe.rectTransform : null;

        /// <summary>El vidrio de la mini cuia.</summary>
        public RectTransform? MiniCuia => _miniGlass != null ? _miniGlass.rectTransform : null;

        /// <summary>El retrato de TiTino (null en release sin su PNG).</summary>
        public Image? Portrait => _portrait;

        /// <summary>Etiquetas de la placa (tests de recortes).</summary>
        public TextMeshProUGUI? WelcomeLabel => _welcomeLabel;

        /// <summary>Etiqueta del lugar.</summary>
        public TextMeshProUGUI? PlaceLabel => _placeLabel;

        /// <summary>Etiqueta del dato.</summary>
        public TextMeshProUGUI? FactLabel => _factLabel;

        /// <summary>Color del agua de la mini cuia (el del sitio).</summary>
        public Color SampleColor => _miniWaterA != null ? _miniWaterA.color : Color.clear;

        /// <summary>
        /// Color del agua de <paramref name="sample"/> (<c>white</c>, <c>black</c>, <c>clear</c>, <c>clear_white</c> o
        /// <c>black_white</c>; 08 §4.5): la primera, la del sitio, y en las mezclas la segunda en <paramref name="second"/>.
        /// </summary>
        public static Color SampleWater(string sample, out Color? second)
        {
            int cut = sample.IndexOf('_');
            if (cut < 0)
            {
                second = null;
                return Palette.WaterBase(sample);
            }

            second = Palette.WaterBase(sample.Substring(cut + 1));
            return Palette.WaterBase(sample.Substring(0, cut));
        }

        /// <summary>
        /// Lo que tiene que caber en la ranura: la llegada más alta de las paradas del catálogo (k ≥ 2, las que se ven en una
        /// victoria) en el idioma de <see cref="Loc"/>, a la escala <paramref name="textScale"/> y en <paramref name="widthDp"/> dp;
        /// 0 sin catálogo. Se mide una vez por combinación (caché).
        /// </summary>
        public float NeedDp(float textScale, float widthDp)
        {
            RiverStopCatalog? stops = _stops();
            if (stops == null)
            {
                return 0f;
            }

            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            for (int i = 0; i < _needs.Count; i++)
            {
                NeedEntry e = _needs[i];
                if (ReferenceEquals(e.Stops, stops) && e.Locale == locale && Mathf.Approximately(e.Scale, textScale) && Mathf.Approximately(e.Width, widthDp))
                {
                    return e.Height;
                }
            }

            float need = 0f;
            for (int n = 2; n < stops.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                if (stop != null)
                {
                    need = Mathf.Max(need, ArrivalHeightOf(stop, textScale, widthDp));
                }
            }

            _needs.Add(new NeedEntry(stops, locale, textScale, widthDp, need));
            return need;
        }

        /// <summary>
        /// Alto (dp) de la llegada completa a <paramref name="stop"/> (con su dato, si tiene texto, y la bienvenida si iza bandera)
        /// en el idioma de <see cref="Loc"/>, a la escala <paramref name="textScale"/> y en <paramref name="widthDp"/> dp.
        /// </summary>
        public float ArrivalHeightOf(RiverStop stop, float textScale, float widthDp)
        {
            string fact = stop.ArrivalFact != null ? Loc.Fact(stop.ArrivalFact) : string.Empty;
            ArrivalKind kind = fact.Length > 0 ? ArrivalKind.Arrival : ArrivalKind.Place;
            return HeightOf(kind, WelcomeFor(stop), PlaceFor(stop), fact, textScale, widthDp);
        }

        /// <summary>
        /// Alto (dp) de lo que se enseña con <paramref name="kind"/> y estos textos (vacío = sin esa línea), a la escala
        /// <paramref name="textScale"/> y en <paramref name="widthDp"/> dp: la fila del agua, el hueco y la placa (con el retrato
        /// si hay dato) más el margen de abajo. Mide con TextMeshPro: solo al enseñar o al medir, nunca por fotograma.
        /// </summary>
        public float HeightOf(ArrivalKind kind, string welcome, string place, string fact, float textScale, float widthDp)
        {
            float textW = widthDp - PortraitDp - PortraitGapDp;
            switch (kind)
            {
                case ArrivalKind.Arrival:
                    {
                        float block = Lines(welcome, place, textW, textScale) + Measure(_measureBody, fact, FactSp * textScale, textW);
                        return Mathf.Ceil(RowDp + GapDp + Mathf.Max(PortraitDp, block) + BottomDp);
                    }

                case ArrivalKind.Place:
                    {
                        float block = Lines(welcome, place, widthDp, textScale);
                        return Mathf.Ceil(RowDp + (block > 0f ? GapDp + block - LineGapDp : 0f) + BottomDp);
                    }

                case ArrivalKind.Deferred:
                    return Mathf.Ceil(Mathf.Max(PortraitDp, Measure(_measureBody, fact, FactSp * textScale, textW)) + BottomDp);

                default:
                    return 0f;
            }
        }

        /// <inheritdoc />
        public void Layout(RectTransform content, float widthDp, float heightDp)
        {
            _content = content;
            _width = widthDp;
            _height = heightDp;
            Build(content);
            UiBuilder.Place((RectTransform)_root!.transform, 0f, 0f, widthDp, heightDp);
            if (Kind != ArrivalKind.None)
            {
                Arrange();
                Apply();
            }
        }

        /// <inheritdoc />
        public bool Show(LevelFlow flow, in VictoryLine line)
        {
            ResetState();
            Level = flow.LevelNumber;
            _reduceMotion = flow.ReduceMotion;
            RiverStopCatalog? stops = _stops();
            if (line.Arrival && stops != null)
            {
                Stop = stops.ArrivalOnWin(Level);
            }

            if (Stop != null)
            {
                FactText = line.ArrivalFact && line.ArrivalFactKey != null ? Loc.Fact(line.ArrivalFactKey) : string.Empty;
                Kind = FactText.Length > 0 ? ArrivalKind.Arrival : ArrivalKind.Place;
                WelcomeText = WelcomeFor(Stop);
                Welcome = WelcomeText.Length > 0;
                PlaceText = PlaceFor(Stop);
            }
            else if (line.DeferredFact && line.ArrivalFactKey != null)
            {
                FactText = Loc.Fact(line.ArrivalFactKey);
                Kind = FactText.Length > 0 ? ArrivalKind.Deferred : ArrivalKind.None;
            }

            if (Kind == ArrivalKind.None || _root == null)
            {
                Kind = ArrivalKind.None;
                Hide();
                return false;
            }

            ShownFactKey = FactText.Length > 0 ? line.ArrivalFactKey : null;
            _since = 0f;
            _root.gameObject.SetActive(true);
            Arrange();
            Apply();
            return true;
        }

        /// <inheritdoc />
        public void Tick(float dtMs, float sinceShownMs)
        {
            if (Kind == ArrivalKind.None || _since < 0f)
            {
                return;
            }

            _since = sinceShownMs;
            if (!_startedFired && sinceShownMs >= GameScreen.VictoryPanelUpMs)
            {
                _startedFired = true;
                _elapsed = 0f;
                _started?.Invoke(this);
            }

            if (_startedFired && !_completedFired)
            {
                _elapsed = sinceShownMs - GameScreen.VictoryPanelUpMs;
                if (_elapsed >= MapConfig.ArrivalStripMs)
                {
                    Finish("time", false);
                }
            }

            Apply();
        }

        /// <summary>
        /// Un toque completa la franja (02 §6, fila Llegada): todo salta a su estado final y el texto se queda. Antes de que
        /// empiece, también la da por empezada.
        /// </summary>
        public void Complete()
        {
            if (Kind == ArrivalKind.None || _completedFired)
            {
                return;
            }

            if (!_startedFired)
            {
                _startedFired = true;
                _started?.Invoke(this);
            }

            Finish("tap", true);
            Apply();
        }

        /// <summary>La victoria se cierra antes de que termine la franja: la da por saltada (sin tocar lo que se ve).</summary>
        public void Close()
        {
            if (Kind != ArrivalKind.None && !_completedFired)
            {
                Finish("close", true);
            }
        }

        /// <inheritdoc />
        public void Hide()
        {
            if (_root != null && _root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }

            _since = -1f;
        }

        /// <inheritdoc />
        public void LayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (Kind == ArrivalKind.None || _root == null || !_root.gameObject.activeInHierarchy)
            {
                return;
            }

            ids.Add("arrival");
            rects.Add((RectTransform)_root.transform);
        }

        /// <summary>«{país} te da la bienvenida» en las paradas que izan bandera (P-M7 a), o vacío.</summary>
        private static string WelcomeFor(RiverStop stop)
        {
            if (stop.FlagRaise == null)
            {
                return string.Empty;
            }

            string country = UiText.StopCountry(stop);
            return country.Length > 0 ? UiText.MapCountryWelcome(country) : string.Empty;
        }

        /// <summary>«{lugar} · {país} · río {río}», o vacío si falta algún nombre (sin traducir o sin aprobar en release).</summary>
        private static string PlaceFor(RiverStop stop)
        {
            string name = UiText.StopName(stop);
            string country = UiText.StopCountry(stop);
            string river = UiText.StopRiver(stop);
            return name.Length > 0 && country.Length > 0 && river.Length > 0 ? UiText.MapArrived(name, country, river) : string.Empty;
        }

        // Alto de la bienvenida y del lugar (cada una con su hueco debajo) a PlaceSp en width dp.
        private float Lines(string welcome, string place, float width, float textScale)
        {
            float h = 0f;
            if (welcome.Length > 0)
            {
                h += Measure(_measureBold, welcome, PlaceSp * textScale, width) + LineGapDp;
            }

            if (place.Length > 0)
            {
                h += Measure(_measureBold, place, PlaceSp * textScale, width) + LineGapDp;
            }

            return h;
        }

        private static float Measure(TextMeshProUGUI label, string text, float sizeDp, float width)
        {
            if (text.Length == 0)
            {
                return 0f;
            }

            label.fontSize = sizeDp;
            float h = label.GetPreferredValues(text, width, float.PositiveInfinity).y;
            label.text = string.Empty;
            return h;
        }

        private void ResetState()
        {
            Kind = ArrivalKind.None;
            Stop = null;
            Level = 0;
            ShownFactKey = null;
            Welcome = false;
            WelcomeText = string.Empty;
            PlaceText = string.Empty;
            FactText = string.Empty;
            ShownHeightDp = 0f;
            Skipped = false;
            CompletedBy = string.Empty;
            WaterFill = LegFromFill;
            SampleFill = 0f;
            CanoeProgress = 0f;
            _since = -1f;
            _elapsed = -1f;
            _startedFired = false;
            _completedFired = false;
        }

        private void Finish(string by, bool skipped)
        {
            _completedFired = true;
            Skipped = skipped;
            CompletedBy = by;
            if (_elapsed < MapConfig.ArrivalStripMs)
            {
                _elapsed = MapConfig.ArrivalStripMs;
            }

            if (by != "close")
            {
                _completed?.Invoke(this);
            }
        }

        // Crea la franja bajo el contenido de la ranura (una vez): el área táctil con su CanvasGroup, el fondo de la placa, la fila
        // del agua (tramo, agua, línea de agua, canoa y mini cuia con su agua) y la placa (retrato, bienvenida, lugar y dato).
        private void Build(RectTransform content)
        {
            if (_root != null)
            {
                if (_root.transform.parent != content)
                {
                    _root.transform.SetParent(content, false);
                }

                return;
            }

            Button root = UiBuilder.BareButton("ArrivalStrip", content, Complete);
            _root = root;
            RectTransform rt = (RectTransform)root.transform;
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _plateBack = UiBuilder.Image("PlateBack", rt, null, WithAlpha(Palette.FamilyWood.Base, PlateAlpha));
            _track = UiBuilder.Image("Leg", rt, null, Color.white);
            _waterA = UiBuilder.Image("Water", rt, null, Color.white);
            _waterB = UiBuilder.Image("WaterMeet", rt, null, Color.white);
            _waterLine = UiBuilder.Image("WaterLine", rt, null, WithAlpha(Palette.WaterLine, 0.85f));
            _canoe = UiBuilder.Image("Canoe", rt, _art.MapCanoe, Color.white);
            _canoe.preserveAspect = true;
            _miniWaterA = FilledWater("MiniCuiaWater", rt);
            _miniWaterB = FilledWater("MiniCuiaWaterMeet", rt);
            _miniGlass = UiBuilder.Image("MiniCuia", rt, _art.MapMiniCuia, Color.white);
            _miniGlass.preserveAspect = true;
            _portrait = UiBuilder.Image("Portrait", rt, null, Color.white);
            _portrait.preserveAspect = true;
            _welcomeLabel = PlateLabel("Welcome", rt, TextStyle.PlainBold, PlaceSp, Palette.FamilyWood.Deep);
            _placeLabel = PlateLabel("Place", rt, TextStyle.PlainBold, PlaceSp, Palette.FamilyWood.Deep);
            _factLabel = PlateLabel("Fact", rt, TextStyle.Body, FactSp, Palette.UiInk);
            root.gameObject.SetActive(false);
        }

        private Image FilledWater(string name, RectTransform parent)
        {
            Image img = UiBuilder.Image(name, parent, _art.MapMiniCuiaWater, Color.white);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Vertical;
            img.fillOrigin = (int)Image.OriginVertical.Bottom;
            img.preserveAspect = true;
            return img;
        }

        private static TextMeshProUGUI PlateLabel(string name, RectTransform parent, TextStyle style, float sp, Color color)
        {
            TextMeshProUGUI label = UiBuilder.Label(name, parent, style, string.Empty, sp, color, TextAlignmentOptions.TopLeft);
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        // Coloca todo para lo que se enseña ahora (al enseñar y al recolocar la victoria; nunca por fotograma).
        private void Arrange()
        {
            float w = _width;
            float scale = UiBuilder.TextScale;
            bool row = Kind == ArrivalKind.Arrival || Kind == ArrivalKind.Place;
            bool fact = Kind == ArrivalKind.Arrival || Kind == ArrivalKind.Deferred;
            ShownHeightDp = HeightOf(Kind, WelcomeText, PlaceText, FactText, scale, w);

            _mixed = false;
            SetActive(_track, row);
            SetActive(_waterA, row);
            SetActive(_waterB, false);
            SetActive(_waterLine, row);
            SetActive(_canoe, row);
            SetActive(_miniGlass, row);
            SetActive(_miniWaterA, row);
            if (row)
            {
                Color a = SampleWater(Stop!.Sample, out Color? b);
                _mixed = b.HasValue;
                SetActive(_waterB, _mixed);
                SetActive(_miniWaterB, _mixed);
                _trackW = w - MiniCuiaW - 6f;
                _canoeFrom = (_trackW * 0.5f) - CanoeW;
                _canoeTo = _trackW - CanoeW - 2f;
                _track!.color = WithAlpha(a, 0.28f);
                UiBuilder.Place(_track.rectTransform, 0f, RowDp * 0.5f, _trackW, RowDp * 0.5f);
                _waterA!.color = a;
                _waterB!.color = b ?? a;
                _miniWaterA!.color = a;
                _miniWaterB!.color = b ?? a;
                float miniX = w - MiniCuiaW;
                UiBuilder.Place(_miniGlass!.rectTransform, miniX, 0f, MiniCuiaW, RowDp);
                UiBuilder.Place(_miniWaterA.rectTransform, miniX, 0f, MiniCuiaW, RowDp);
                UiBuilder.Place(_miniWaterB.rectTransform, miniX, 0f, MiniCuiaW, RowDp);
            }
            else
            {
                SetActive(_miniWaterB, false);
            }

            Sprite? open = fact ? _art.SlothPortrait(false) : null;
            _portraitOpen = open;
            _portraitClosed = open != null ? _art.SlothPortrait(true) : null;
            SetActive(_portrait, open != null);
            float top = row ? RowDp + GapDp : 0f;
            float textX = open != null ? PortraitDp + PortraitGapDp : 0f;
            float textW = (fact ? w - PortraitDp - PortraitGapDp : w);
            if (open != null)
            {
                _portrait!.sprite = open;
                UiBuilder.Place(_portrait.rectTransform, 0f, top, PortraitDp, PortraitDp);
            }

            float plateH = Mathf.Max(0f, ShownHeightDp - top - BottomDp);
            SetActive(_plateBack, plateH > 0f && (fact || WelcomeText.Length > 0 || PlaceText.Length > 0));
            UiBuilder.Place(_plateBack!.rectTransform, -4f, top - 2f, w + 8f, plateH + 4f);

            float y = top;
            if (!fact)
            {
                textX = 0f;
                textW = w;
            }

            y = PlaceLine(_welcomeLabel!, row ? WelcomeText : string.Empty, textX, y, textW, scale);
            y = PlaceLine(_placeLabel!, row ? PlaceText : string.Empty, textX, y, textW, scale);
            SetLine(_factLabel!, fact ? FactText : string.Empty, FactSp * scale, textX, y, textW);
        }

        private static float PlaceLine(TextMeshProUGUI label, string text, float x, float y, float w, float scale)
        {
            float h = SetLine(label, text, PlaceSp * scale, x, y, w);
            return h > 0f ? y + h + LineGapDp : y;
        }

        private static float SetLine(TextMeshProUGUI label, string text, float sizeDp, float x, float y, float w)
        {
            bool on = text.Length > 0;
            if (label.gameObject.activeSelf != on)
            {
                label.gameObject.SetActive(on);
            }

            if (!on)
            {
                return 0f;
            }

            label.fontSize = sizeDp;
            label.text = text;
            float h = label.GetPreferredValues(text, w, float.PositiveInfinity).y;
            UiBuilder.Place(label.rectTransform, x, y, w, Mathf.Max(h, 1f));
            return h;
        }

        // Estado de la franja en el instante de ahora (sin asignaciones): agua, canoa, mini cuia, placa, retrato y opacidad.
        private void Apply()
        {
            if (_group == null || Kind == ArrivalKind.None)
            {
                return;
            }

            if (Kind == ArrivalKind.Deferred)
            {
                // El dato desplazado ocupa el sitio de la línea de fruta: entra con «Siguiente» como ella (02 §6).
                float a = _completedFired ? 1f : VictoryLineSlot.FruitLineAlpha(Mathf.Max(0f, _since), _reduceMotion);
                _group.alpha = a;
                SetAlpha(_factLabel, 1f);
                SetAlpha(_portrait, 1f);
                SetAlpha(_plateBack, PlateAlpha);
                ApplyPortrait(_completedFired || _reduceMotion ? float.MaxValue : _since - VictoryLineSlot.FruitRevealMs);
                return;
            }

            float t = _completedFired ? float.MaxValue : _elapsed;
            if (_reduceMotion)
            {
                // Sin viaje ni llenado: el estado final con un fundido de 150 ms desde la subida del panel.
                _group.alpha = _completedFired ? 1f : (t < 0f ? 0f : Mathf.Clamp01(t / MapConfig.ReducedMotionFadeMs));
                t = float.MaxValue;
            }
            else
            {
                _group.alpha = 1f;
            }

            WaterFill = Mathf.Lerp(LegFromFill, 1f, Fraction(t, 0f, WaterFillMs));
            CanoeProgress = Ease.OutCubic(Fraction(t, CanoeFromMs, CanoeMs));
            float sampleFrom = MapConfig.ArrivalStripMs - MapConfig.ArrivalSampleFillMs;
            SampleFill = Fraction(t, sampleFrom, MapConfig.ArrivalSampleFillMs);
            float placeA = Fraction(t, PlaceInMs, PlateFadeMs);
            float factA = Fraction(t, FactInMs, PlateFadeMs);

            float waterW = _trackW * WaterFill;
            float half = RowDp * 0.5f;
            UiBuilder.Place(_waterA!.rectTransform, 0f, half, waterW, _mixed ? half * 0.5f : half);
            UiBuilder.Place(_waterB!.rectTransform, 0f, half * 1.5f, waterW, half * 0.5f);
            UiBuilder.Place(_waterLine!.rectTransform, 0f, half, waterW, 1.5f);
            UiBuilder.Place(_canoe!.rectTransform, Mathf.Lerp(_canoeFrom, _canoeTo, CanoeProgress), 0f, CanoeW, RowDp);
            _miniWaterA!.fillAmount = SampleFill;
            _miniWaterB!.fillAmount = Mathf.Min(SampleFill, 0.5f);
            SetAlpha(_welcomeLabel, placeA);
            SetAlpha(_placeLabel, placeA);
            SetAlpha(_plateBack, PlateAlpha * Mathf.Max(placeA, factA));
            SetAlpha(_factLabel, factA);
            SetAlpha(_portrait, factA);
            ApplyPortrait(t - FactInMs);
        }

        // Gesto sloth_present provisional: el retrato entra con un rebote y parpadea una vez (TI4 hará el rig).
        private void ApplyPortrait(float sinceFactMs)
        {
            if (_portrait == null || _portraitOpen == null)
            {
                return;
            }

            float bounce = sinceFactMs >= PresentMs ? 1f : Mathf.LerpUnclamped(0.8f, 1f, Ease.OutBack(Mathf.Clamp01(sinceFactMs / PresentMs)));
            _portrait.rectTransform.localScale = new Vector3(bounce, bounce, 1f);
            float blink = sinceFactMs - (BlinkFromMs - FactInMs);
            Sprite? sprite = blink >= 0f && blink < BlinkMs && _portraitClosed != null ? _portraitClosed : _portraitOpen;
            if (_portrait.sprite != sprite)
            {
                _portrait.sprite = sprite;
            }
        }

        private static float Fraction(float t, float from, float length)
        {
            return t <= from ? 0f : (length <= 0f ? 1f : Mathf.Clamp01((t - from) / length));
        }

        private static void SetAlpha(Graphic? g, float a)
        {
            if (g == null)
            {
                return;
            }

            Color c = g.color;
            if (!Mathf.Approximately(c.a, a))
            {
                c.a = a;
                g.color = c;
            }
        }

        private static void SetActive(Graphic? g, bool on)
        {
            if (g != null && g.gameObject.activeSelf != on)
            {
                g.gameObject.SetActive(on);
            }
        }

        private static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        private readonly struct NeedEntry
        {
            public NeedEntry(RiverStopCatalog stops, string locale, float scale, float width, float height)
            {
                Stops = stops;
                Locale = locale;
                Scale = scale;
                Width = width;
                Height = height;
            }

            public RiverStopCatalog Stops { get; }

            public string Locale { get; }

            public float Scale { get; }

            public float Width { get; }

            public float Height { get; }
        }
    }

    /// <summary>Qué enseña la franja de llegada en una victoria (<see cref="ArrivalStrip"/>).</summary>
    internal enum ArrivalKind
    {
        /// <summary>Nada.</summary>
        None = 0,

        /// <summary>La llegada completa: fila del agua y placa con el retrato, el lugar y el dato.</summary>
        Arrival = 1,

        /// <summary>Solo la placa del lugar (SingleKey, o un hito en el que habla TiTino).</summary>
        Place = 2,

        /// <summary>Solo la placa del dato desplazado con el retrato (el <c>10k+1</c> tras un hito de TiTino).</summary>
        Deferred = 3,
    }
}
