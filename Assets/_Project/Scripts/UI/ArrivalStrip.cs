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
    /// <item><see cref="ArrivalKind.Place"/>: solo la placa del lugar (política SingleKey, un hito en el que habla TiTino o una
    /// llegada sin dato). Si el árbitro da además la línea de fruta (<see cref="VictoryLine.FruitKey"/>; arquitectura de la
    /// tanda 4, §2: «con SingleKey, la llegada solo con la placa del lugar, más la línea de fruta») y la línea de fruta se
    /// enseña (<see cref="StackFruit"/>: tiene sus textos en el idioma de la UI), la placa va arriba de la ranura
    /// (<see cref="Stacked"/>) y la línea de fruta, debajo (<see cref="FactLine"/>; tarea F3); si no, la placa va centrada.</item>
    /// <item><see cref="ArrivalKind.Deferred"/>: en el <c>10k+1</c> tras un hito en el que habló TiTino, solo la placa del dato
    /// desplazado con el retrato, que entra con «Siguiente» como la línea de fruta (<see cref="VictoryLineSlot.FruitLineAlpha"/>).</item>
    /// </list>
    /// <para><b>Pregunta 11, opción (a) [P]:</b> la llegada va compactada (la fila del agua y una sola placa con el retrato, el
    /// lugar a <see cref="PlaceSp"/> sp y el dato a <see cref="FactSp"/>; la bienvenida, dentro de la misma placa). Las medidas de
    /// M1.3b dicen que ni compactada cabe siempre en el alto de la línea de fruta, así que la ranura reserva
    /// máx(línea de fruta, <see cref="NeedDp"/>), con <see cref="NeedDp"/> = la llegada más alta del catálogo en el idioma y la
    /// escala de ahora: nada se recorta y «Siguiente» sigue en el mismo sitio en todas las victorias (Q7-A).</para>
    /// <para><b>Aspecto (revisiones de M1.6; D-003, vidrio, madera y luz):</b> la fila del agua es un cauce de extremos
    /// redondeados (<see cref="BedRadiusDp"/> dp) con borde de madera oscura <see cref="BedEdge"/> de <see cref="BedEdgeDp"/> dp
    /// y un canto de vidrio <see cref="BedColor"/> de <see cref="BedRingDp"/> dp entre el borde y el agua, que es una banda de
    /// <see cref="WaterDp"/> dp con un brillo de superficie de <see cref="WaterGlintDp"/> dp y una línea de ondas
    /// (<see cref="ProceduralArt.ArrivalWave"/>) <see cref="WaveBelowDp"/> dp más abajo. El agua blanca va más oscura en la franja
    /// (<see cref="StripWhiteWater"/>, ≥ 3:1 frente al panel), y el agua, el cauce y su borde contrastan entre sí con los tres
    /// <c>water_base</c> (<c>ArrivalStripColorTests</c>). La canoa (<see cref="CanoeDp"/> dp) flota con la quilla
    /// <see cref="CanoeDraftDp"/> dp bajo la superficie. La mini cuia se pinta con el mismo color que la barra (su agua va encima
    /// del vidrio provisional, cuyo interior es opaco, y encima de todo un brillo del vidrio al 25 %). La placa es una tarjeta de
    /// esquinas de <see cref="PlateRadiusDp"/> dp con borde de 1 dp #E3D3B0 [P], alineada con la fila del agua, con
    /// <see cref="PlatePadDp"/> dp de relleno y <see cref="PlateRightPadDp"/> a la derecha del texto; la bienvenida va en verde
    /// hoja (<see cref="WelcomeInk"/>) para separarse de la línea del lugar. La línea del lugar se parte por sus <c>{sep}</c>
    /// (<see cref="UiText.MapArrived(string, string, string, int)"/>) y nunca deja un « · » colgando. La ranura tiene un alto fijo
    /// (la llegada más alta del catálogo): lo que se enseña va centrado en vertical dentro de ella, con el aire repartido arriba
    /// y abajo.</para>
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

        /// <summary>
        /// Bienvenida y lugar: Nunito Bold de 12 sp, el mínimo (pregunta 11, a; maqueta de M1.3b) [P]. La revisión de M1.6 pidió
        /// 13 sp si el alto de la ranura lo permitía, pero las medidas de M1.3b (<c>slot_measures.txt</c>) dicen que ni la llegada
        /// compactada a 12 sp cabe en el alto de la línea de fruta: se queda en 12 sp y la bienvenida se distingue por el color
        /// (<see cref="WelcomeInk"/>), sin coste de alto.
        /// </summary>
        public const float PlaceSp = UiBuilder.MinTextSp;

        /// <summary>Dato: Nunito Regular de 14 sp, nunca menos de 12 (02 §6).</summary>
        public const float FactSp = VictoryLineSlot.TextSp;

        /// <summary>Entre las líneas de la placa [P].</summary>
        public const float LineGapDp = 2f;

        /// <summary>Margen bajo la fila del agua cuando no hay placa (el de la ranura); con placa, su relleno hace de margen.</summary>
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

        /// <summary>Relleno de la placa a la izquierda, arriba y abajo [P].</summary>
        public const float PlatePadDp = 6f;

        /// <summary>Relleno de la placa a la derecha del texto (revisión de M1.6) [P].</summary>
        public const float PlateRightPadDp = 12f;

        /// <summary>Radio de las esquinas de la placa [P].</summary>
        public const float PlateRadiusDp = 10f;

        /// <summary>Brillo del vidrio de la mini cuia, encima del agua (revisión de M1.6: ≤ 0,25) [P].</summary>
        public const float MiniGlintAlpha = 0.25f;

        /// <summary>
        /// Banda de agua de la fila (dp) [P]. La revisión de M1.6 pidió de 14 a 18 dp (0,65 × <see cref="RowDp"/>); con 18 la
        /// canoa, con la quilla <see cref="CanoeDraftDp"/> dp bajo la superficie, no pasaría de 14 dp dentro de la fila de 28: 16
        /// deja la canoa en <see cref="CanoeDp"/> dp.
        /// </summary>
        public const float WaterDp = 16f;

        /// <summary>Borde de madera del cauce (dp) [P].</summary>
        public const float BedEdgeDp = 1.5f;

        /// <summary>Canto de vidrio entre el borde del cauce y el agua (dp) [P].</summary>
        public const float BedRingDp = 1f;

        /// <summary>Radio de los extremos redondeados del cauce (dp; el agua, con el radio menos el borde y el canto) [P].</summary>
        public const float BedRadiusDp = 7f;

        /// <summary>Brillo de la superficie del agua de la fila: <see cref="WaterGlintDp"/> dp blanco al 45 % [P].</summary>
        public const float WaterGlintAlpha = 0.45f;

        /// <summary>Alto del brillo de la superficie del agua (dp) [P].</summary>
        public const float WaterGlintDp = 2f;

        /// <summary>Línea de ondas del agua (<see cref="ProceduralArt.ArrivalWave"/>): blanca al 35 % [P].</summary>
        public const float WaveAlpha = 0.35f;

        /// <summary>El eje de la línea de ondas va 5 dp por debajo de la superficie del agua [P].</summary>
        public const float WaveBelowDp = 5f;

        /// <summary>Alto de la canoa de Capi en la fila (dp) [P].</summary>
        public const float CanoeDp = 17f;

        /// <summary>La quilla de la canoa va 3 dp por debajo de la superficie: flota dentro del agua, no sobre la barra [P].</summary>
        public const float CanoeDraftDp = 3f;

        /// <summary>Borde de la placa (#E3D3B0) [P].</summary>
        public static readonly Color PlateEdge = Palette.Hex(0xE3D3B0);

        /// <summary>
        /// Canto de vidrio del cauce, entre el borde y el agua (<see cref="Palette.GlassRim"/>, #EAF7F3): ≥ 3,6:1 frente al agua
        /// de la franja con los tres <c>water_base</c> (<c>ArrivalStripColorTests</c>) [P].
        /// </summary>
        public static readonly Color BedColor = Palette.GlassRim;

        /// <summary>
        /// Borde de madera oscura del cauce (#7A5A32): 5,6:1 frente al panel #FBF0D9 y 5,7:1 frente al canto de vidrio; nunca del
        /// tono del agua (ΔE00 ≥ 10 con los tres <c>water_base</c>) [P].
        /// </summary>
        public static readonly Color BedEdge = Palette.Hex(0x7A5A32);

        /// <summary>
        /// Agua blanca en la franja (#9C7646, solo aquí; <see cref="Palette.WaterWhite"/> #B08A5A da 2,8:1 frente al panel): 3,6:1
        /// frente al panel, el mínimo de 3:1 de un elemento gráfico [P].
        /// </summary>
        public static readonly Color StripWhiteWater = Palette.Hex(0x9C7646);

        /// <summary>Tinta de la bienvenida: el verde hoja de la cinta «¡Nivel completado!» (<see cref="ButtonFamily.Deep"/> de Hoja), 5,9:1 [P].</summary>
        public static readonly Color WelcomeInk = Palette.FamilyLeaf.Deep;

        /// <summary>Tinta de la línea del lugar (la de antes).</summary>
        public static readonly Color PlaceInk = Palette.FamilyWood.Deep;

        private const float MiniCuiaW = RowDp * ProceduralArt.MiniCuiaWidthDp / ProceduralArt.MiniCuiaHeightDp;
        private const float CanoeW = CanoeDp * ProceduralArt.MapCanoeWidthDp / ProceduralArt.MapCanoeHeightDp;

        // Quilla de la canoa provisional: a 5 dp del pie de su sprite de 32 dp (ProceduralArt.BuildMapCanoe).
        private const float CanoeKeelDp = CanoeDp * 5f / ProceduralArt.MapCanoeHeightDp;

        // Del borde exterior del cauce al agua: el borde de madera y el canto de vidrio.
        private const float BedInsetDp = BedEdgeDp + BedRingDp;

        // Arriba del cauce, dentro de la fila (el cauce va al pie de la fila).
        private const float BedTopDp = RowDp - WaterDp - (2f * BedInsetDp);

        // Superficie del agua, dentro de la fila.
        private const float SurfaceDp = BedTopDp + BedInsetDp;

        // Radio de las esquinas del agua.
        private const float WaterRadiusDp = BedRadiusDp - BedInsetDp;

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
        private Image? _plateEdge;
        private Image? _plateBack;
        private Image? _bedEdge;
        private Image? _bed;
        private Image? _track;
        private Image? _waterA;
        private Image? _waterB;
        private Image? _waterLine;
        private Image? _waterGlint;
        private Image? _canoe;
        private Image? _miniWaterA;
        private Image? _miniWaterB;
        private Image? _miniGlass;
        private Image? _miniGlint;
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
        private float _offY;
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

        /// <summary>
        /// Texto de la placa del lugar, «{lugar} · {país} · río {río}», con saltos de línea en sus cortes si no cabe en una
        /// (<see cref="PlaceBreaks"/>; vacío si falta algún nombre).
        /// </summary>
        public string PlaceText { get; private set; } = string.Empty;

        /// <summary>Texto del dato (vacío sin él).</summary>
        public string FactText { get; private set; } = string.Empty;

        /// <summary>Alto que ocupa lo que se enseña ahora (dp; 0 sin nada).</summary>
        public float ShownHeightDp { get; private set; }

        /// <summary>
        /// La línea de fruta se enseña en la victoria de ahora (lo pone GameScreen.Victory.cs antes de cada victoria, con
        /// <see cref="FactLine.CanShow"/>: el árbitro la da y tiene sus textos en el idioma de la UI): una placa del lugar con línea
        /// de fruta (<see cref="VictoryLine.FruitKey"/>) va entonces arriba, con la línea debajo (<see cref="Stacked"/>); si no, centrada.
        /// </summary>
        public bool StackFruit { get; set; }

        /// <summary>
        /// En la victoria de ahora la placa del lugar va arriba de la ranura, sin centrar, y la línea de fruta debajo (tarea F3;
        /// arquitectura de la tanda 4, §2).
        /// </summary>
        public bool Stacked { get; private set; }

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

        /// <summary>
        /// El área táctil recibe toques: solo mientras se ve algo de la franja (alfa &gt; 0). Con el dato desplazado o con Reducir
        /// movimiento, antes del fundido de entrada un toque en el hueco invisible no la completa.
        /// </summary>
        public bool TapEnabled => _group != null && _group.blocksRaycasts;

        /// <summary>Cortes de la línea del lugar: el bit i, el <c>{sep}</c> número i de <c>map_arrived</c> (0 = en una línea).</summary>
        public int PlaceBreaks { get; private set; }

        /// <summary>Fila del agua: el cauce, de 0 al canto de la mini cuia menos 6 dp (tests y capturas).</summary>
        public RectTransform? Track => _bedEdge != null ? _bedEdge.rectTransform : null;

        /// <summary>El agua de la fila (la primera, en las mezclas).</summary>
        public RectTransform? Water => _waterA != null ? _waterA.rectTransform : null;

        /// <summary>La canoa de Capi.</summary>
        public RectTransform? Canoe => _canoe != null ? _canoe.rectTransform : null;

        /// <summary>El vidrio de la mini cuia.</summary>
        public RectTransform? MiniCuia => _miniGlass != null ? _miniGlass.rectTransform : null;

        /// <summary>El agua de la mini cuia (encima del vidrio).</summary>
        public RectTransform? MiniWater => _miniWaterA != null ? _miniWaterA.rectTransform : null;

        /// <summary>La placa (su borde: el rectángulo entero).</summary>
        public RectTransform? Plate => _plateEdge != null ? _plateEdge.rectTransform : null;

        /// <summary>Desplazamiento vertical de lo que se enseña dentro de la ranura (dp; el aire se reparte arriba y abajo).</summary>
        public float OffsetDp => _offY;

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
                return StripWater(sample);
            }

            second = StripWater(sample.Substring(cut + 1));
            return StripWater(sample.Substring(0, cut));
        }

        /// <summary>
        /// Agua de <paramref name="waterBase"/> (<c>black</c>, <c>white</c> o <c>clear</c>; glosario §11) en la franja: la de
        /// <see cref="Palette.WaterBase"/>, salvo la blanca, que va en <see cref="StripWhiteWater"/>.
        /// </summary>
        public static Color StripWater(string waterBase)
        {
            switch (waterBase)
            {
                case "black":
                case "clear":
                    return Palette.WaterBase(waterBase);
                default:
                    return StripWhiteWater;
            }
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
            float placeW = kind == ArrivalKind.Arrival ? FactTextWidth(widthDp) : PlaceTextWidth(widthDp);
            return HeightOf(kind, WelcomeFor(stop), PlaceFor(stop, placeW, textScale, out _), fact, textScale, widthDp);
        }

        /// <summary>
        /// Alto (dp) de la placa del lugar de <paramref name="stop"/> sola (<see cref="ArrivalKind.Place"/>: la fila del agua, la
        /// bienvenida si iza bandera y el lugar) en el idioma de <see cref="Loc"/>, a la escala <paramref name="textScale"/> y en
        /// <paramref name="widthDp"/> dp: lo que ocupa encima de la línea de fruta cuando van juntas (<see cref="Stacked"/>).
        /// </summary>
        public float PlaceHeightOf(RiverStop stop, float textScale, float widthDp)
        {
            return HeightOf(ArrivalKind.Place, WelcomeFor(stop), PlaceFor(stop, PlaceTextWidth(widthDp), textScale, out _), string.Empty, textScale, widthDp);
        }

        /// <summary>
        /// Alto (dp) de lo que se enseña con <paramref name="kind"/> y estos textos (vacío = sin esa línea), a la escala
        /// <paramref name="textScale"/> y en <paramref name="widthDp"/> dp: la fila del agua, el hueco y la placa (con el retrato
        /// si hay dato) más el margen de abajo. Mide con TextMeshPro: solo al enseñar o al medir, nunca por fotograma.
        /// </summary>
        public float HeightOf(ArrivalKind kind, string welcome, string place, string fact, float textScale, float widthDp)
        {
            float textW = FactTextWidth(widthDp);
            const float pads = 2f * PlatePadDp;
            switch (kind)
            {
                case ArrivalKind.Arrival:
                    {
                        float block = Lines(welcome, place, textW, textScale) + Measure(_measureBody, fact, FactSp * textScale, textW);
                        return Mathf.Ceil(RowDp + GapDp + pads + Mathf.Max(PortraitDp, block));
                    }

                case ArrivalKind.Place:
                    {
                        float block = Lines(welcome, place, PlaceTextWidth(widthDp), textScale);
                        return Mathf.Ceil(RowDp + (block > 0f ? GapDp + pads + block - LineGapDp : BottomDp));
                    }

                case ArrivalKind.Deferred:
                    return Mathf.Ceil(pads + Mathf.Max(PortraitDp, Measure(_measureBody, fact, FactSp * textScale, textW)));

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

            Stacked = StackFruit && Kind == ArrivalKind.Place && line.FruitKey != null;

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

        /// <summary>
        /// «{lugar} · {país} · río {río}» en <paramref name="width"/> dp a la escala <paramref name="textScale"/>, o vacío si falta
        /// algún nombre (sin traducir o sin aprobar en release). Si no cabe en una línea, se parte por el último <c>{sep}</c> de
        /// <c>map_arrived</c> (en ES, «Leticia · Colombia» / «río Amazonas») y, si aún no cabe, por todos: nunca deja un « · »
        /// colgando al final de una línea (revisión de M1.6). <paramref name="breaks"/>: los cortes elegidos.
        /// </summary>
        private string PlaceFor(RiverStop stop, float width, float textScale, out int breaks)
        {
            breaks = 0;
            string name = UiText.StopName(stop);
            string country = UiText.StopCountry(stop);
            string river = UiText.StopRiver(stop);
            if (name.Length == 0 || country.Length == 0 || river.Length == 0)
            {
                return string.Empty;
            }

            string line = UiText.MapArrived(name, country, river, 0);
            int seps = UiText.MapArrivedSeparators();
            if (seps == 0 || FitsUnbroken(line, width, textScale))
            {
                return line;
            }

            breaks = 1 << (seps - 1);
            string two = UiText.MapArrived(name, country, river, breaks);
            if (seps == 1 || FitsUnbroken(two, width, textScale))
            {
                return two;
            }

            breaks = (1 << seps) - 1;
            return UiText.MapArrived(name, country, river, breaks);
        }

        // Cada línea de text (las de sus saltos) cabe en width dp a PlaceSp sin que TextMeshPro la parta, con medio dp de holgura.
        private bool FitsUnbroken(string text, float width, float textScale)
        {
            _measureBold.fontSize = PlaceSp * textScale;
            float w = _measureBold.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
            _measureBold.text = string.Empty;
            return w <= width - 0.5f;
        }

        // Ancho del texto de la placa con el retrato (el dato): relleno, retrato, hueco y relleno derecho.
        private static float FactTextWidth(float widthDp)
        {
            return widthDp - PlatePadDp - PortraitDp - PortraitGapDp - PlateRightPadDp;
        }

        // Ancho del texto de la placa sin retrato (solo el lugar).
        private static float PlaceTextWidth(float widthDp)
        {
            return widthDp - PlatePadDp - PlateRightPadDp;
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
            PlaceBreaks = 0;
            FactText = string.Empty;
            ShownHeightDp = 0f;
            Stacked = false;
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
            _plateEdge = UiBuilder.Sliced("PlateEdge", rt, _art.RoundedRect, PlateEdge);
            _plateEdge.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / PlateRadiusDp;
            _plateBack = UiBuilder.Sliced("PlateBack", rt, _art.RoundedRect, Palette.PanelLight);
            _plateBack.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / (PlateRadiusDp - 1f);
            // El cauce: borde de madera, canto de vidrio y el agua, de extremos redondeados (el RoundedRect de la placa); encima
            // del agua, el brillo de la superficie y la línea de ondas; delante de todo, la canoa.
            _bedEdge = Rounded("BedEdge", rt, BedEdge, BedRadiusDp);
            _bed = Rounded("Bed", rt, BedColor, BedRadiusDp - BedEdgeDp);
            _track = Rounded("Leg", rt, Color.white, WaterRadiusDp);
            _waterA = Rounded("Water", rt, Color.white, WaterRadiusDp);
            _waterB = UiBuilder.Image("WaterMeet", rt, null, Color.white);
            _waterLine = UiBuilder.Image("WaterGlint", rt, null, new Color(1f, 1f, 1f, WaterGlintAlpha));
            _waterGlint = UiBuilder.Image("WaterWave", rt, _art.ArrivalWave, new Color(1f, 1f, 1f, WaveAlpha));
            _waterGlint.type = Image.Type.Tiled;
            _canoe = UiBuilder.Image("Canoe", rt, _art.MapCanoe, Color.white);
            _canoe.preserveAspect = true;

            // El vidrio provisional (ProceduralArt.MapIcons) tiene el interior casi opaco: con el agua detrás, la muestra se veía
            // oscura (revisión de M1.6). El agua va encima del vidrio, con el mismo color que la barra, y encima de todo un brillo
            // del vidrio al 25 %.
            _miniGlass = UiBuilder.Image("MiniCuia", rt, _art.MapMiniCuia, Color.white);
            _miniGlass.preserveAspect = true;
            _miniWaterA = FilledWater("MiniCuiaWater", rt);
            _miniWaterB = FilledWater("MiniCuiaWaterMeet", rt);
            _miniGlint = UiBuilder.Image("MiniCuiaGlint", rt, null, new Color(1f, 1f, 1f, MiniGlintAlpha));
            _portrait = UiBuilder.Image("Portrait", rt, null, Color.white);
            _portrait.preserveAspect = true;
            _welcomeLabel = PlateLabel("Welcome", rt, TextStyle.PlainBold, PlaceSp, WelcomeInk);
            _placeLabel = PlateLabel("Place", rt, TextStyle.PlainBold, PlaceSp, PlaceInk);
            _factLabel = PlateLabel("Fact", rt, TextStyle.Body, FactSp, Palette.UiInk);
            root.gameObject.SetActive(false);
        }

        // Rectángulo redondeado 9-slice con esquinas de radiusDp dp.
        private Image Rounded(string name, RectTransform parent, Color color, float radiusDp)
        {
            Image img = UiBuilder.Sliced(name, parent, _art.RoundedRect, color);
            img.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / radiusDp;
            return img;
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
            float textW = fact ? FactTextWidth(w) : PlaceTextWidth(w);
            if (Stop != null)
            {
                // La línea del lugar depende del ancho y de la escala: se rehace en cada colocación (al enseñar y al recolocar).
                PlaceText = PlaceFor(Stop, textW, scale, out int breaks);
                PlaceBreaks = breaks;
            }

            ShownHeightDp = HeightOf(Kind, WelcomeText, PlaceText, FactText, scale, w);

            // La ranura tiene el alto de la llegada más alta del catálogo: lo de ahora va centrado, con el aire repartido. Con la
            // línea de fruta debajo (Stacked), la placa va arriba: la ranura reserva la placa más alta y la línea (F3).
            _offY = Stacked ? 0f : Mathf.Max(0f, Mathf.Floor((_height - ShownHeightDp) * 0.5f));
            float off = _offY;
            _mixed = false;
            SetActive(_bedEdge, row);
            SetActive(_bed, row);
            SetActive(_track, row);
            SetActive(_waterA, row);
            SetActive(_waterB, false);
            SetActive(_waterLine, row);
            SetActive(_waterGlint, row);
            SetActive(_canoe, row);
            SetActive(_miniGlass, row);
            SetActive(_miniWaterA, row);
            SetActive(_miniGlint, row);
            if (row)
            {
                Color a = SampleWater(Stop!.Sample, out Color? b);
                _mixed = b.HasValue;
                SetActive(_waterB, _mixed);
                SetActive(_miniWaterB, _mixed);
                // El cauce va al pie de la fila, de 0 a _trackW; el agua, dentro del borde y del canto (de _trackW − 2 × inset).
                _trackW = w - MiniCuiaW - 6f;
                float waterMax = _trackW - (2f * BedInsetDp);
                _canoeFrom = BedInsetDp + (waterMax * 0.5f) - CanoeW;
                _canoeTo = BedInsetDp + waterMax - CanoeW - WaterRadiusDp; // la pala del remo, lejos del extremo redondeado
                UiBuilder.Place(_bedEdge!.rectTransform, 0f, off + BedTopDp, _trackW, RowDp - BedTopDp);
                UiBuilder.Place(_bed!.rectTransform, BedEdgeDp, off + BedTopDp + BedEdgeDp, _trackW - (2f * BedEdgeDp), RowDp - BedTopDp - (2f * BedEdgeDp));
                _track!.color = WithAlpha(a, 0.28f);
                UiBuilder.Place(_track.rectTransform, BedInsetDp, off + SurfaceDp, waterMax, WaterDp);
                _waterA!.color = a;
                _waterB!.color = b ?? a;
                _miniWaterA!.color = a;
                _miniWaterB!.color = b ?? a;
                float miniX = w - MiniCuiaW;
                UiBuilder.Place(_miniGlass!.rectTransform, miniX, off, MiniCuiaW, RowDp);
                UiBuilder.Place(_miniWaterA.rectTransform, miniX, off, MiniCuiaW, RowDp);
                UiBuilder.Place(_miniWaterB.rectTransform, miniX, off, MiniCuiaW, RowDp);
                PlaceMiniGlint(miniX, off);
            }
            else
            {
                SetActive(_miniWaterB, false);
            }

            Sprite? open = fact ? _art.SlothPortrait(false) : null;
            _portraitOpen = open;
            _portraitClosed = open != null ? _art.SlothPortrait(true) : null;
            SetActive(_portrait, open != null);
            float plateTop = off + (row ? RowDp + GapDp : 0f);
            float top = plateTop + PlatePadDp;
            float textX = PlatePadDp + (open != null ? PortraitDp + PortraitGapDp : 0f);
            if (open != null)
            {
                _portrait!.sprite = open;
                UiBuilder.Place(_portrait.rectTransform, PlatePadDp, top, PortraitDp, PortraitDp);
            }

            // La placa: alineada con la fila del agua (de 0 a w), del hueco bajo la fila al pie de lo que se enseña.
            bool plate = fact || (row && (WelcomeText.Length > 0 || PlaceText.Length > 0));
            float plateH = Mathf.Max(0f, off + ShownHeightDp - plateTop);
            plate &= plateH > 0f;
            SetActive(_plateEdge, plate);
            SetActive(_plateBack, plate);
            UiBuilder.Place(_plateEdge!.rectTransform, 0f, plateTop, w, plateH);
            UiBuilder.Place(_plateBack!.rectTransform, 1f, plateTop + 1f, w - 2f, Mathf.Max(0f, plateH - 2f));

            float y = top;
            if (!fact)
            {
                textX = PlatePadDp;
            }

            y = PlaceLine(_welcomeLabel!, row ? WelcomeText : string.Empty, textX, y, textW, scale);
            y = PlaceLine(_placeLabel!, row ? PlaceText : string.Empty, textX, y, textW, scale);
            SetLine(_factLabel!, fact ? FactText : string.Empty, FactSp * scale, textX, y, textW);
        }

        // Brillo vertical del vidrio de la mini cuia (a la izquierda de la cavidad, como el del vidrio provisional), encima del
        // agua: la cavidad de capacidad ProceduralArt.MiniCuiaCapacity en la caja de RowDp de alto.
        private void PlaceMiniGlint(float miniX, float off)
        {
            const float s = RowDp / ProceduralArt.MiniCuiaHeightDp;
            const int cap = ProceduralArt.MiniCuiaCapacity;
            float u = (ProceduralArt.MiniCuiaHeightDp - 1f) / CuiaGeometry.HeightU(cap);
            float bottom = 0.5f + (CuiaGeometry.CavityBottomU * u);
            float topU = 0.5f + (CuiaGeometry.CavityTopU(cap) * u);
            float cx = (ProceduralArt.MiniCuiaWidthDp * 0.5f) - (0.55f * u);
            float gw = Mathf.Max(1f, 0.26f * u * s);
            UiBuilder.Place(_miniGlint!.rectTransform, miniX + (cx * s) - (gw * 0.5f), off + RowDp - (topU * s), gw, (topU - bottom) * s);
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
                SetGroupAlpha(a);
                SetAlpha(_factLabel, 1f);
                SetAlpha(_portrait, 1f);
                SetAlpha(_plateEdge, 1f);
                SetAlpha(_plateBack, 1f);
                ApplyPortrait(_completedFired || _reduceMotion ? float.MaxValue : _since - VictoryLineSlot.FruitRevealMs);
                return;
            }

            float t = _completedFired ? float.MaxValue : _elapsed;
            if (_reduceMotion)
            {
                // Sin viaje ni llenado: el estado final con un fundido de 150 ms desde la subida del panel.
                SetGroupAlpha(_completedFired ? 1f : (t < 0f ? 0f : Mathf.Clamp01(t / MapConfig.ReducedMotionFadeMs)));
                t = float.MaxValue;
            }
            else
            {
                SetGroupAlpha(1f);
            }

            WaterFill = Mathf.Lerp(LegFromFill, 1f, Fraction(t, 0f, WaterFillMs));
            CanoeProgress = Ease.OutCubic(Fraction(t, CanoeFromMs, CanoeMs));
            float sampleFrom = MapConfig.ArrivalStripMs - MapConfig.ArrivalSampleFillMs;
            SampleFill = Fraction(t, sampleFrom, MapConfig.ArrivalSampleFillMs);
            float placeA = Fraction(t, PlaceInMs, PlateFadeMs);
            float factA = Fraction(t, FactInMs, PlateFadeMs);

            // El agua llena el tramo de izquierda a derecha; en las mezclas, la segunda agua corre por la mitad de abajo, entre los
            // extremos redondeados. El brillo y las ondas, sin pisar los extremos; la canoa, con la quilla bajo la superficie.
            float waterW = (_trackW - (2f * BedInsetDp)) * WaterFill;
            float inner = Mathf.Max(0f, waterW - (2f * WaterRadiusDp));
            float off = _offY;
            float surface = off + SurfaceDp;
            UiBuilder.Place(_waterA!.rectTransform, BedInsetDp, surface, waterW, WaterDp);
            UiBuilder.Place(_waterB!.rectTransform, BedInsetDp + WaterRadiusDp, surface + (WaterDp * 0.5f), inner, WaterDp * 0.5f);
            UiBuilder.Place(_waterLine!.rectTransform, BedInsetDp + WaterRadiusDp, surface, inner, WaterGlintDp);
            UiBuilder.Place(_waterGlint!.rectTransform, BedInsetDp + WaterRadiusDp, surface + WaveBelowDp - (ProceduralArt.ArrivalWaveHeightDp * 0.5f), inner,
                ProceduralArt.ArrivalWaveHeightDp);
            float canoeTop = surface + CanoeDraftDp + CanoeKeelDp - CanoeDp;
            UiBuilder.Place(_canoe!.rectTransform, Mathf.Lerp(_canoeFrom, _canoeTo, CanoeProgress), canoeTop, CanoeW, CanoeDp);
            _miniWaterA!.fillAmount = SampleFill;
            _miniWaterB!.fillAmount = Mathf.Min(SampleFill, 0.5f);
            SetAlpha(_welcomeLabel, placeA);
            SetAlpha(_placeLabel, placeA);
            float plateA = Mathf.Max(placeA, factA);
            SetAlpha(_plateEdge, plateA);
            SetAlpha(_plateBack, plateA);
            SetAlpha(_factLabel, factA);
            SetAlpha(_portrait, factA);
            ApplyPortrait(t - FactInMs);
        }

        // Opacidad de toda la franja; el área táctil solo recibe toques mientras se ve algo (revisión de M1.6: con el dato
        // desplazado o con Reducir movimiento, un toque en el hueco aún invisible adelantaba la entrada y escribía las sondas).
        private void SetGroupAlpha(float a)
        {
            _group!.alpha = a;
            bool taps = a > 0f;
            if (_group.blocksRaycasts != taps)
            {
                _group.blocksRaycasts = taps;
            }
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
