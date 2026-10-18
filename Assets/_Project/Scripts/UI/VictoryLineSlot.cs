#nullable enable
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
    /// Quien pone contenido en la ranura del Dato del río de la victoria (<see cref="VictoryLineSlot"/>): la franja de llegada
    /// (M1.6) y la línea de fruta (F3). Se registra con <see cref="GameScreen.RegisterVictoryLineProvider"/> desde
    /// GameScreen.Victory.cs. Nada de lo que hace por fotograma asigna memoria.
    /// </summary>
    internal interface IVictoryLineProvider
    {
        /// <summary>
        /// El proveedor usa la ranura en esta partida (sale de su configuración y de sus datos, no de la victoria concreta):
        /// con alguno activo, la ranura existe en todas las victorias con su alto fijo y Siguiente queda siempre en el mismo
        /// sitio (Q7-A de D-017), aunque en una victoria no salga nada (el 10 con el Mapa).
        /// </summary>
        bool SlotActive { get; }

        /// <summary>
        /// Coloca su contenido en <paramref name="content"/> (la ranura bajo el filete; padre de lo que cree) de
        /// <paramref name="widthDp"/> × <paramref name="heightDp"/> dp. Al recolocar la victoria (también antes de la primera).
        /// </summary>
        void Layout(RectTransform content, float widthDp, float heightDp);

        /// <summary>
        /// La victoria de <paramref name="flow"/> se ve con la línea <paramref name="line"/> del árbitro
        /// (<see cref="VictoryLineArbiter"/>): enseña su parte y devuelve true, u oculta lo suyo y devuelve false.
        /// </summary>
        bool Show(LevelFlow flow, in VictoryLine line);

        /// <summary>
        /// Avanza <paramref name="dtMs"/>; <paramref name="sinceShownMs"/> cuenta desde que se vio la victoria (OnVictoryScreen:
        /// el panel empieza a subir, fila 0,80 de 07 §5.4; «Siguiente» entra a los <see cref="GameScreen.VictoryButtonsInMs"/>
        /// ms, fila 1,40). Con eso cada proveedor decide cuándo y cómo aparece lo suyo y cuándo recibe toques: la ranura no oculta,
        /// no retrasa ni bloquea nada.
        /// </summary>
        void Tick(float dtMs, float sinceShownMs);

        /// <summary>Se acabó la victoria sin enseñar nada suyo (o se dejó de usar la ranura): oculta lo que tenga.</summary>
        void Hide();

        /// <summary>Sus botones visibles para la línea <c>AF_LAYOUT victory</c> (la línea de fruta abre la ficha desde F4).</summary>
        void LayoutIds(List<string> ids, List<RectTransform> rects);
    }

    /// <summary>
    /// Ranura del Dato del río de la victoria (02 §6, filas «Dato del río» y Llegada; Q7-A de D-017; D-013 punto 8; arquitectura
    /// de la tanda 4, §2; tarea M1.3b): un hueco de alto fijo bajo «Inicio», separado por un filete, donde cabe la única línea
    /// de texto de la victoria (T6-A): la llegada a la parada del Mapa (M1.6) o la línea de fruta (F3).
    /// <list type="bullet">
    /// <item>Su alto de contenido es fijo por idioma, escala y ancho, y lo decide GameScreen.SlotContentDp (GameScreen.Victory.cs;
    /// pregunta 11, opción (a) [P], tarea M1.6): máx(línea de fruta más larga, llegada más alta del catálogo). La línea de fruta
    /// (<see cref="ContentHeightFor"/>) es la cabecera de <see cref="HeaderDp"/> dp más el texto del peor caso
    /// (<see cref="WorstCaseText"/>: <see cref="SourceWorstChars"/> caracteres en es-419 y <see cref="TranslatedWorstChars"/> en
    /// en, pt-BR y en-XA) a <see cref="TextSp"/> sp, medido con TextMeshPro y guardado en caché; la llegada, ArrivalStrip.NeedDp.
    /// Lo que reserva bajo «Inicio» es <see cref="ReserveOf"/> de ese alto. Así «Siguiente» sube una sola vez y queda en el
    /// mismo sitio en todos los niveles.</item>
    /// <item>Solo existe con algún proveedor activo: sin ninguno, la victoria es la de siempre.</item>
    /// <item>No retrasa nada: «Siguiente» entra en el mismo milisegundo que sin ranura (a los
    /// <see cref="GameScreen.VictoryButtonsInMs"/> ms de la pantalla de victoria, la fila 1,40 de 07 §5.4).</item>
    /// <item>No decide cuándo aparece ni si recibe toques lo que ponen los proveedores: el contenido es visible y recibe toques
    /// desde que se ve la victoria, y cada proveedor gobierna lo suyo con el <c>sinceShownMs</c> de
    /// <see cref="IVictoryLineProvider.Tick"/>. La línea de fruta (F3) sale con «Siguiente» y un fundido de
    /// <see cref="FruitFadeMs"/> ms (<see cref="FruitLineAlpha"/>; 02 §6, fila «Dato del río»); la llegada (M1.6) tiene su franja
    /// de 1,2 s desde la subida del panel, un toque que la completa y «Ver mapa» (02 §6, fila Llegada).</item>
    /// </list>
    /// Las cadenas del peor caso solo se miden: nunca se enseñan (los textos visibles son los de las tablas, D-018).
    /// <para>Medidas de M1.3b (VictorySlotMeasureSnapshotTests, 360 × 780 y 360 × 640, victoria del 39 con chip y Cosecha
    /// doble): H = 94 dp en los cuatro idiomas al 100 %; al 130 %, 111 en es-419 y 136 en en, pt-BR y en-XA (la ranura añade
    /// 11 dp más con el hueco y el filete). La llegada de 02 §6 no cabe en H: completa, 161-199 dp al 100 % y 245-295 al
    /// 130 %. La opción (a) de la pregunta 11 (compactar la llegada: la fila del agua y una placa con retrato, lugar y dato) se
    /// aplica [P] (NOTAS de la tanda 4, respuesta 11), pero no basta: 129-164 dp al 100 % (120-137 con el dato a 12 sp) y
    /// 166-226 al 130 %. Solo la forma ceñida (el lugar en la fila del agua y el dato a todo el ancho) con el dato a 12 sp cabe,
    /// y solo al 100 % en es-419, pt-BR y en-XA (76-92 dp; en, 109); al 130 % no cabe ninguna. Queda como pregunta nueva para el
    /// dueño (H = máx(línea de fruta, llegada compacta), con Siguiente más arriba; dato de llegada a 12 sp con la forma ceñida;
    /// o un tope de caracteres menor para el dato de llegada). M1.6 aplica la primera [P] (política de preguntas de la ola 2):
    /// la ranura reserva máx(línea de fruta, llegada más alta del catálogo), así que la llegada cabe siempre y «Siguiente»
    /// sube lo que haga falta, una sola vez.</para>
    /// </summary>
    internal sealed class VictoryLineSlot
    {
        /// <summary>Del pie del área táctil de «Inicio» (48 dp) al filete [P].</summary>
        public const float RuleGapDp = 6f;

        /// <summary>Filete que separa la ranura de «Inicio» (02 §6; propuesta de Frutas §2.2, regla 1) [P].</summary>
        public const float RuleDp = 1f;

        /// <summary>Del filete al contenido [P].</summary>
        public const float RuleBelowDp = 4f;

        /// <summary>Cabecera de la línea de fruta: icono de 32 dp y «Dato del río · Açaí 3/20» (02 §6; F3).</summary>
        public const float HeaderDp = 32f;

        /// <summary>Texto de la línea: Nunito Regular de 14 sp, nunca menos de 12 (02 §6 y §12).</summary>
        public const float TextSp = 14f;

        /// <summary>Margen bajo el texto [P].</summary>
        public const float BottomDp = 4f;

        /// <summary>Margen a cada lado, dentro del panel de victoria [P].</summary>
        public const float SideDp = 16f;

        /// <summary>Toda la línea es un botón de 48 dp como mínimo (02 §6; 07 §7.8).</summary>
        public const float MinContentDp = 48f;

        /// <summary>
        /// La línea de fruta empieza a verse con «Siguiente», en la fila 1,40 de 07 §5.4: a los
        /// <see cref="GameScreen.VictoryButtonsInMs"/> ms de la pantalla de victoria (02 §6, fila «Dato del río»).
        /// </summary>
        public const float FruitRevealMs = GameScreen.VictoryButtonsInMs;

        /// <summary>Fundido de entrada de la línea de fruta (02 §6; propuesta de Frutas §2.2, regla 2).</summary>
        public const float FruitFadeMs = 200f;

        /// <summary>Peor caso en es-419: el tope de un dato (<see cref="RiverStopCatalog.FactMaxChars"/>, 90; 02 §6).</summary>
        public const int SourceWorstChars = RiverStopCatalog.FactMaxChars;

        /// <summary>Peor caso en en, pt-BR y en-XA: 90 + 30 % (propuesta de Frutas §2.5; W3C).</summary>
        public const int TranslatedWorstChars = 117;

        // Cadenas del peor caso, solo para medir (nunca se enseñan): frases de palabras largas con la longitud exacta del tope,
        // que parten peor que el texto real. en-XA con el aspecto de LocPseudo (acentos de Latin-1, relleno y corchetes).
        private const string WorstEs = "Los bufeos rosados persiguen cardúmenes entre las raíces sumergidas del igapó del Tapajós.";
        private const string WorstEn = "Pink river dolphins chase schools of fish between the submerged roots of the flooded forest during the long wet rains";
        private const string WorstPt = "Os botos-cor-de-rosa perseguem cardumes entre as raízes submersas da floresta alagada durante a longa estação chuvosa";
        private const string WorstXa = "[Lós búféós rósádós pérsígúéñ cárdúméñés éñtré lás ráícés súmérgídás dél ígápó ámázóñícó, cérca dé lá órílla ·······]";

        private readonly List<IVictoryLineProvider> _providers = new List<IVictoryLineProvider>(4);
        private readonly List<CacheEntry> _cache = new List<CacheEntry>(4);
        private RectTransform? _root;
        private Image? _rule;
        private RectTransform? _content;
        private TextMeshProUGUI? _measure;
        private float _reserveDp;
        private float _contentDp;
        private float _sinceShownMs = -1f;
        private float _dismissedMs = -1f;
        private float _sheetMs;
        private bool _shown;

        /// <summary>Hay algún proveedor activo: la ranura se reserva (<see cref="IVictoryLineProvider.SlotActive"/>).</summary>
        public bool Reserved
        {
            get
            {
                for (int i = 0; i < _providers.Count; i++)
                {
                    if (_providers[i].SlotActive)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>La ranura ya creó sus objetos (la primera vez que hubo reserva o medida).</summary>
        public bool Built => _root != null;

        /// <summary>Lo que la ranura añade bajo «Inicio» en la última maquetación (dp; 0 sin reserva).</summary>
        public float ReserveDp => _reserveDp;

        /// <summary>Alto del contenido, bajo el filete, en la última maquetación (dp; 0 sin reserva).</summary>
        public float ContentDp => _contentDp;

        /// <summary>Medidas hechas con TextMeshPro (las demás salen de la caché; tests).</summary>
        public int MeasureCount { get; private set; }

        /// <summary>La ranura entera (filete y contenido), hija del panel de victoria; null hasta que hace falta.</summary>
        public RectTransform? Root => _root;

        /// <summary>El contenido bajo el filete, padre de lo que crean los proveedores; null hasta que hace falta.</summary>
        public RectTransform? Content => _content;

        /// <summary>Algún proveedor enseña algo en la victoria de ahora.</summary>
        public bool Shown => _shown;

        /// <summary>Milisegundos desde que se vio la victoria (−1 fuera de la victoria).</summary>
        public float SinceShownMs => _sinceShownMs;

        /// <summary>
        /// Milisegundos desde que se vio la victoria hasta que se cerró con Siguiente (AD_GATE) o Inicio (−1 si sigue abierta):
        /// el <c>dismissed_ms</c> del punto común de <c>river_fact_shown</c> (M1.6 y F3). Incluye el tiempo con la ficha de la fruta
        /// abierta encima (<see cref="SheetMs"/>; revisión de F4): es tiempo real hasta el toque en «Siguiente» (08 §8.2), aunque la
        /// vista de la victoria (<see cref="SinceShownMs"/>) no avance mientras tanto.
        /// </summary>
        public float DismissedMs => _dismissedMs;

        /// <summary>
        /// Milisegundos que la ficha de una fruta (F4) estuvo abierta encima de esta victoria, con la vista congelada: se suman a
        /// <see cref="DismissedMs"/> y no a <see cref="SinceShownMs"/>.
        /// </summary>
        public float SheetMs => _sheetMs;

        /// <summary>Proveedores registrados (en orden de registro).</summary>
        public int ProviderCount => _providers.Count;

        /// <summary>Número de caracteres del peor caso de <paramref name="locale"/>: 90 en es-419 y 117 en los demás.</summary>
        public static int WorstCaseChars(string locale)
        {
            return locale == LocLocale.SpanishLatinAmerica ? SourceWorstChars : TranslatedWorstChars;
        }

        /// <summary>
        /// Cadena del peor caso de <paramref name="locale"/> (solo para medir; nunca se enseña): es-419, en, pt-BR o en-XA; otro
        /// idioma usa la de en.
        /// </summary>
        public static string WorstCaseText(string locale)
        {
            switch (locale)
            {
                case LocLocale.SpanishLatinAmerica:
                    return WorstEs;
                case LocLocale.PortugueseBrazil:
                    return WorstPt;
                case LocLocale.Pseudo:
                    return WorstXa;
                default:
                    return WorstEn;
            }
        }

        /// <summary>
        /// Opacidad de la línea de fruta a los <paramref name="sinceShownMs"/> ms de la pantalla de victoria (02 §6, fila «Dato del
        /// río»; 07 §5.4, fila 1,40): 0 hasta que entra «Siguiente» (<see cref="FruitRevealMs"/>) y después un fundido de
        /// <see cref="FruitFadeMs"/> ms; con <paramref name="reduceMotion"/>, sin fundido. Para el proveedor de la línea de fruta
        /// (F3); la llegada lleva sus propios tiempos.
        /// </summary>
        public static float FruitLineAlpha(float sinceShownMs, bool reduceMotion)
        {
            float t = sinceShownMs - FruitRevealMs;
            if (t <= 0f)
            {
                return 0f;
            }

            return reduceMotion ? 1f : Mathf.Clamp01(t / FruitFadeMs);
        }

        /// <summary>Registra <paramref name="provider"/> (una vez).</summary>
        public void Register(IVictoryLineProvider provider)
        {
            if (!_providers.Contains(provider))
            {
                _providers.Add(provider);
            }
        }

        /// <summary>Quita <paramref name="provider"/> y oculta lo suyo.</summary>
        public void Unregister(IVictoryLineProvider provider)
        {
            if (_providers.Remove(provider))
            {
                provider.Hide();
            }
        }

        /// <summary>
        /// Crea la ranura bajo el panel de victoria (una vez): filete, contenido y la etiqueta de medir. El contenido no lleva
        /// CanvasGroup: lo que pongan los proveedores se ve y recibe toques según decida cada uno.
        /// </summary>
        public void Build(RectTransform panel)
        {
            if (_root != null)
            {
                return;
            }

            _root = UiBuilder.Rect("DatoDelRioSlot", panel);
            Image rule = UiBuilder.Image("Rule", _root, null, WithAlpha(Palette.FamilyWood.Base, 0.45f));
            UiBuilder.Place(rule.rectTransform, 0f, RuleGapDp, 0f, RuleDp);
            rule.rectTransform.anchorMax = new Vector2(1f, 1f); // a todo el ancho de la ranura
            rule.rectTransform.sizeDelta = new Vector2(0f, RuleDp);
            rule.gameObject.SetActive(false);
            _rule = rule;
            _content = UiBuilder.Rect("Content", _root);

            // Medidas con la fuente de verdad (Nunito Regular, como el texto de la línea); vacía e invisible fuera de la medida.
            _measure = UiBuilder.Label("Measure", _root, TextStyle.Body, string.Empty, TextSp, new Color(0f, 0f, 0f, 0f), TextAlignmentOptions.TopLeft);
            _measure.textWrappingMode = TextWrappingModes.Normal;
            _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Alto del contenido (bajo el filete) para <paramref name="locale"/>, la escala de texto <paramref name="textScale"/>
        /// y <paramref name="widthDp"/> dp de ancho: cabecera, texto del peor caso y margen, con 48 dp como mínimo. Se mide una
        /// vez por combinación (caché). Necesita <see cref="Build"/>.
        /// </summary>
        public float ContentHeightFor(string locale, float textScale, float widthDp)
        {
            for (int i = 0; i < _cache.Count; i++)
            {
                CacheEntry e = _cache[i];
                if (e.Locale == locale && Mathf.Approximately(e.Scale, textScale) && Mathf.Approximately(e.Width, widthDp))
                {
                    return e.Height;
                }
            }

            float text = MeasureText(WorstCaseText(locale), TextSp * textScale, widthDp);
            float height = Mathf.Max(MinContentDp, Mathf.Ceil(HeaderDp + text + BottomDp));
            _cache.Add(new CacheEntry(locale, textScale, widthDp, height));
            return height;
        }

        /// <summary>Lo que la ranura añade bajo «Inicio» con <paramref name="contentDp"/> dp de contenido: el hueco, el filete y el contenido.</summary>
        public static float ReserveOf(float contentDp)
        {
            return RuleGapDp + RuleDp + RuleBelowDp + contentDp;
        }

        /// <summary>
        /// Alto de <paramref name="text"/> a <paramref name="sizeDp"/> dp en <paramref name="widthDp"/> dp de ancho, con la fuente
        /// de la línea (Nunito Regular); al medir no se ve. Necesita <see cref="Build"/>.
        /// </summary>
        public float MeasureText(string text, float sizeDp, float widthDp)
        {
            TextMeshProUGUI m = _measure!;
            MeasureCount++;
            m.fontSize = sizeDp;
            float height = m.GetPreferredValues(text, widthDp, float.PositiveInfinity).y;
            m.text = string.Empty;
            return height;
        }

        /// <summary>
        /// Coloca la ranura en el panel: (<paramref name="x"/>, <paramref name="top"/>) desde el canto de arriba a la izquierda,
        /// <paramref name="widthDp"/> de ancho y <paramref name="contentDp"/> de contenido; y avisa a los proveedores.
        /// </summary>
        public void Layout(float x, float top, float widthDp, float contentDp)
        {
            RectTransform root = _root!;
            _contentDp = contentDp;
            _reserveDp = ReserveOf(contentDp);
            root.gameObject.SetActive(true);
            UiBuilder.Place(root, x, top, widthDp, _reserveDp);
            RectTransform content = _content!;
            UiBuilder.Place(content, 0f, RuleGapDp + RuleDp + RuleBelowDp, widthDp, contentDp);
            for (int i = 0; i < _providers.Count; i++)
            {
                if (_providers[i].SlotActive)
                {
                    _providers[i].Layout(content, widthDp, contentDp);
                }
            }
        }

        /// <summary>Sin reserva: la ranura no se ve y no ocupa nada.</summary>
        public void Collapse()
        {
            _reserveDp = 0f;
            _contentDp = 0f;
            if (_root != null && _root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// La victoria de <paramref name="flow"/> se ve con la línea <paramref name="line"/>: cada proveedor activo enseña lo suyo
        /// (con sus propios tiempos, <see cref="IVictoryLineProvider.Tick"/>) o lo oculta, y empieza a contar
        /// <see cref="SinceShownMs"/>.
        /// </summary>
        public void Show(LevelFlow flow, in VictoryLine line)
        {
            _sinceShownMs = 0f;
            _dismissedMs = -1f;
            _sheetMs = 0f;
            _shown = false;
            for (int i = 0; i < _providers.Count; i++)
            {
                IVictoryLineProvider p = _providers[i];
                if (!p.SlotActive || _reserveDp <= 0f)
                {
                    p.Hide();
                    continue;
                }

                _shown |= p.Show(flow, in line);
            }

            if (_rule != null)
            {
                _rule.gameObject.SetActive(_shown); // el filete, solo con algo debajo
            }
        }

        /// <summary>Avanza la ranura y sus proveedores <paramref name="dtMs"/> ms. Sin asignaciones.</summary>
        public void Tick(float dtMs)
        {
            if (_sinceShownMs < 0f)
            {
                return;
            }

            _sinceShownMs += dtMs;
            for (int i = 0; i < _providers.Count; i++)
            {
                if (_providers[i].SlotActive)
                {
                    _providers[i].Tick(dtMs, _sinceShownMs);
                }
            }
        }

        /// <summary>La victoria se cierra (Siguiente → AD_GATE, o Inicio): anota <see cref="DismissedMs"/> una vez.</summary>
        public void Dismiss()
        {
            if (_sinceShownMs >= 0f && _dismissedMs < 0f)
            {
                _dismissedMs = _sinceShownMs + _sheetMs;
            }
        }

        /// <summary>
        /// La ficha de una fruta siguió abierta encima de la victoria <paramref name="dtMs"/> ms más (F4; revisión de F4): la vista
        /// no avanza, pero el reloj de <see cref="DismissedMs"/> sí. Fuera de la victoria o ya cerrada, nada. Sin asignaciones.
        /// </summary>
        public void AddSheetMs(float dtMs)
        {
            if (_sinceShownMs >= 0f && _dismissedMs < 0f && dtMs > 0f)
            {
                _sheetMs += dtMs;
            }
        }

        /// <summary>Fuera de la victoria: deja de contar y de avisar a los proveedores.</summary>
        public void Stop()
        {
            _sinceShownMs = -1f;
        }

        /// <summary>Los botones de los proveedores para <c>AF_LAYOUT victory</c>.</summary>
        public void LayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (_reserveDp <= 0f)
            {
                return;
            }

            for (int i = 0; i < _providers.Count; i++)
            {
                if (_providers[i].SlotActive)
                {
                    _providers[i].LayoutIds(ids, rects);
                }
            }
        }

        private static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        private readonly struct CacheEntry
        {
            public CacheEntry(string locale, float scale, float width, float height)
            {
                Locale = locale;
                Scale = scale;
                Width = width;
                Height = height;
            }

            public string Locale { get; }

            public float Scale { get; }

            public float Width { get; }

            public float Height { get; }
        }
    }
}
