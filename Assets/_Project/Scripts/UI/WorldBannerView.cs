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
    /// Banner de entrada a un mundo (07 §2.2, regla transversal 1; 02 §6; clave <c>world_banner</c> del glosario §10; decisión
    /// del dueño del 2026-10-11): una tarjeta simple de 1 toque que enseña la cadena de la clave entera, «Mundo {n}: {nombre} ·
    /// {país} · {río}» (<see cref="Text"/>), partida en dos: la cabecera «Mundo {n}: {nombre}» y, bajo una viñeta del río del
    /// mundo (el agua de su tipo, 07 §2.1) en la que Capi presenta con la pata (<c>capi_present</c>) y Rosa asoma al abrir
    /// (<c>rosa_surface</c>, con su soplo) y se sumerge al cerrar (<c>rosa_dive</c>), la línea «{país} · {río}». Si el nombre no
    /// cabe en la cabecera a 24 sp, la cabecera dice «Mundo {n}» y el nombre abre la línea, en negrita. Material de D-003: la
    /// tarjeta de madera clara con su marco de las tarjetas previas (<see cref="ModalKit"/>), la placa de madera oscura y la
    /// viñeta con el canto de madera; arte procedural provisional [P] (<c>ProceduralArt.Mascots</c>). En el mundo 3 la viñeta
    /// enseña las dos aguas del texto, «Negro + Solimões», con una costura suave (<see cref="MeetingWater"/>) [P].
    /// </summary>
    /// <remarks>
    /// <para>Se cierra con 1 toque en cualquier punto (el velo cubre la pantalla; ningún botón de debajo recibe el toque). Sale
    /// antes de que empiece nada: Rosa se sumerge con el pico por delante y después la tarjeta se funde, solo en los últimos
    /// <see cref="FadeTailMs"/> y encogiéndose a <see cref="ExitScale"/> (<see cref="ExitMs"/>); el flujo sigue en LEVEL_CARD
    /// hasta que termina (<c>LevelFlow.WorldBannerExitMs</c>) y solo entonces abre la tarjeta previa o arranca «Llenar y teñir»
    /// (07 §5.1: «se muestra antes de esta animación, no encima»). Mientras sale sigue recibiendo el toque: un segundo toque
    /// termina la salida en el acto. No se cierra sola, como la tarjeta previa [P].</para>
    /// <para>La línea del lugar va en 1 línea o, si no cabe, en 2 con el corte en uno de los <c>{sep}</c> de su plantilla
    /// (<see cref="UiText.WorldPlaceLine"/>; « · » en es-419; nunca dentro de un nombre: «ríos» o «Negro +» no se quedan solos al
    /// final de la línea) a 18 sp o, si hace falta, más pequeña hasta 15. El banner elige qué corte da el salto de línea; el
    /// texto, la negrita y el orden de los trozos son los de la plantilla (D-018, tarea L1).</para>
    /// <para><b>Escala de la fuente</b> (<see cref="FontScale"/>; 02 §12: hasta el 130 % sin desbordes ni recortes; pregunta 43,
    /// decidida por el dueño el 2026-10-13; T52), con el mismo mecanismo que la tarjeta previa (<see cref="LevelCardView"/>):
    /// los textos se multiplican por ella (la cabecera de 24 sp y la línea del lugar de 18 a 15 sp) y la tarjeta crece en alto:
    /// la placa de la cabecera hasta el alto de línea de su título y la línea del lugar en las líneas que necesite (a 130 %
    /// puede pasar de 2, como el tutorial de la tarjeta previa). La viñeta, Capi y Rosa no cambian de tamaño. Si el nombre no
    /// cabe en la cabecera a la escala de ahora, abre la línea del lugar. La escala del sistema aún no llega al juego: hasta
    /// entonces vale 1 y los tests la fijan a 1,3 [P].</para>
    /// <para>Con «Reducir movimiento» (07 §7.8): solo el fundido, con Capi ya presentando y Rosa ya fuera, sin soplo ni ondas.
    /// Sin asignaciones por fotograma, y quieto del todo al terminar la entrada (<see cref="Settled"/>): Tick ya no toca ninguna
    /// transformada ni ningún color mientras sigue abierto, así que no obliga a rehacer los lotes del lienzo principal ni el
    /// recorte de las dos RectMask2D (guía 8, docs/art/direccion-visual-v2.md: el ambiente que se mueve va en Canvas propios).
    /// El arte (las mascotas y la viñeta con Capi) se pide al abrirlo (o antes, con <see cref="Prefetch"/>) y se libera al cerrarlo
    /// (<c>ProceduralArt.ReleaseBannerArt</c>): construir la pantalla no genera ninguna textura. El cuerpo de Capi va dibujado
    /// en la viñeta y no en una imagen aparte: el lienzo funde cada gráfico por separado, y a medio fundir el agua se veía a
    /// través de él (captura 69e); solo la pata, que se mueve, va aparte.</para>
    /// <para>El arte se pinta fuera del hilo principal (<see cref="AsyncArt"/>; <c>ProceduralArt.RequestBannerArt</c>): unos
    /// 230 ms de CPU en el Editor que, en el fotograma de la carga del nivel, lo habrían parado (08 §5.4). Mientras se pinta, el
    /// velo sale en el acto (el flujo en LEVEL_CARD, detrás el tablero en el fotograma 0 de la intro) pero la tarjeta aún no
    /// (<see cref="Waiting"/>) y los toques se ignoran: el banner no se puede perder sin verse. La tarjeta entra desde t = 0
    /// cuando llega el arte (<see cref="ArtReady"/>) o, si el trabajo falla o no se puede encolar, o si pasan
    /// <see cref="ArtTimeoutMs"/> de reloj real desde que se pidió, sin él (<see cref="Fallback"/>): la tarjeta, la placa y el
    /// texto de D-012, con la viñeta del color plano del agua y sin Rosa ni Capi; el arte que llegue tarde se descarta. En los
    /// dos casos <see cref="CardShown"/>, y desde entonces un toque lo cierra como siempre. Si el arte ya estaba (pedido antes con
    /// <see cref="Prefetch"/>, o en el acto), entra como antes, con el velo y la tarjeta fundiéndose juntos.</para>
    /// </remarks>
    internal sealed class WorldBannerView
    {
        /// <summary>Opacidad del velo (la de la tarjeta previa).</summary>
        public const float DimAlpha = LevelCardView.DimAlpha;

        /// <summary>
        /// Tiempo límite del arte (ms de reloj real, <see cref="System.Diagnostics.Stopwatch"/>, desde que el banner lo pide al
        /// abrirse) [P]: si para entonces no ha llegado, el banner entra sin él (<see cref="FallbackTimeout"/>) y lo que llegue
        /// después se descarta. Los tests lo cambian con <see cref="ArtTimeoutLimitMs"/>.
        /// </summary>
        public const float ArtTimeoutMs = 800f;

        /// <summary>Respaldo (<see cref="Fallback"/>; <c>fallback</c> de <c>AF_STATE world_banner_art</c>): ninguno, con su arte.</summary>
        public const string FallbackNone = "none";

        /// <summary>Respaldo: el trabajo del arte falló en su hilo.</summary>
        public const string FallbackError = "error";

        /// <summary>Respaldo: el grupo de hilos no aceptó el trabajo del arte.</summary>
        public const string FallbackQueue = "queue";

        /// <summary>Respaldo: el arte no llegó en <see cref="ArtTimeoutMs"/>.</summary>
        public const string FallbackTimeout = "timeout";

        /// <summary>Entrada de la tarjeta: fundido y rebote (ms) [P].</summary>
        public const float FadeInMs = 220f;

        /// <summary>Rosa asoma: empieza (ms desde que se abre) [P].</summary>
        public const float RosaSurfaceStartMs = 120f;

        /// <summary>Rosa asoma: duración (ms) [P].</summary>
        public const float RosaSurfaceMs = 520f;

        /// <summary>Soplo de la exhalación: empieza (ms desde que se abre) y dura <see cref="SprayMs"/> [P].</summary>
        public const float SprayStartMs = 560f;

        /// <summary>Soplo de la exhalación: una bruma breve (ms) [P, pendiente del asesor cultural].</summary>
        public const float SprayMs = 480f;

        /// <summary>Capi presenta con la pata: empieza (ms desde que se abre) [P].</summary>
        public const float CapiStartMs = 200f;

        /// <summary>Capi presenta con la pata: 18 fotogramas a 24 por segundo (07 §6.1, <c>capi_present</c>).</summary>
        public const float CapiPresentMs = 750f;

        /// <summary>Onda al asomar: duración (ms desde <see cref="RosaSurfaceStartMs"/>) [P].</summary>
        public const float RippleMs = 700f;

        /// <summary>
        /// Fin de la entrada: desde aquí todo queda quieto (Rosa también, sin vaivén) y <see cref="Tick"/> ya no pinta nada hasta
        /// que sale.
        /// </summary>
        public const float EnterMs = SprayStartMs + SprayMs;

        /// <summary>Salida: Rosa se sumerge y después la tarjeta se funde (ms) [P].</summary>
        public const float ExitMs = 340f;

        /// <summary>
        /// Rosa se sumerge en esta fracción de la salida (el pico por delante y el lomo el último), antes del fundido de la tarjeta.
        /// </summary>
        public const float DiveEnd = 0.62f;

        /// <summary>
        /// La tarjeta solo se funde en los últimos ms de la salida [P] (revisión de la captura 69e): el lienzo funde cada gráfico
        /// por separado, así que con un fundido largo se veían el agua a través de Capi y las cuias a través de la tarjeta.
        /// </summary>
        public const float FadeTailMs = 120f;

        /// <summary>Escala de la tarjeta al terminar de fundirse [P].</summary>
        public const float ExitScale = 0.92f;

        /// <summary>Giro de Rosa al sumergirse (grados, antihorario): el pico baja hacia el agua y entra primero [P].</summary>
        public const float DiveTiltDeg = 24f;

        /// <summary>Desplazamiento de Rosa hacia la izquierda al sumergirse (dp), hacia donde mira [P].</summary>
        public const float DiveShiftDp = 10f;

        /// <summary>Salida con Reducir movimiento: solo el fundido (ms), como la tarjeta previa.</summary>
        public const float ExitReducedMs = LevelCardView.FadeOutMs;

        /// <summary>Pata colgando, al empezar (grados; 0 = horizontal hacia la derecha).</summary>
        public const float PawDownDeg = -82f;

        /// <summary>Pata presentando (grados): tendida hacia delante, casi horizontal, hacia Rosa y el río [P].</summary>
        public const float PawUpDeg = 4f;

        /// <summary>Alto mínimo de la tarjeta (dp) [P]: su cuerpo cae en el alto compartido de 281 dp de la superficie de madera.</summary>
        public const float MinHeightDp = LevelCardView.BiteCardMinHeightDp;

        /// <summary>Alto de la viñeta sin el aire que la lleva al alto mínimo (dp).</summary>
        public const float SceneDp = 150f;

        /// <summary>Línea del agua donde asoma Rosa (fracción del alto de la viñeta desde arriba).</summary>
        public const float RosaWaterline = 0.71f;

        /// <summary>Eje de Rosa (fracción del ancho de la viñeta).</summary>
        public const float RosaX = 0.70f;

        /// <summary>Eje de Capi (fracción del ancho de la viñeta), en lo alto del banco.</summary>
        public const float CapiX = ProceduralArt.BannerSceneCapiX;

        /// <summary>Lo que se ve de Rosa sobre el agua (fracción de su alto).</summary>
        public const float RosaAbove = 0.74f;

        /// <summary>Tamaño de la cabecera y de su título (sp): con el nombre si cabe así; si no, «Mundo {n}».</summary>
        public const float TitleSp = 24f;

        /// <summary>Tamaño de la línea del lugar (sp), que baja hasta <see cref="PlaceMinSp"/> si no cabe en 2 líneas.</summary>
        public const float PlaceSp = 18f;

        /// <summary>Tamaño mínimo de la línea del lugar (sp).</summary>
        public const float PlaceMinSp = 15f;

        /// <summary>Ancho de la franja del viaje de mundo (dp) [P] (M3.6), centrada bajo la cabecera.</summary>
        public const float StripWidthDp = 296f;

        /// <summary>Alto de la franja del viaje de mundo (dp) [P] (M3.6).</summary>
        public const float StripHeightDp = 112f;

        /// <summary>Tamaño de la pregunta del mundo sobre la franja (sp) [P]; baja hasta 12 si no cabe en 2 líneas.</summary>
        public const float QuestionSp = 15f;

        /// <summary>Tamaño del texto de «Ver mapa» (sp) [P].</summary>
        public const float MapButtonSp = 16f;

        /// <summary>Ancho mínimo de «Ver mapa» (dp) [P].</summary>
        public const float MapButtonMinWidthDp = 168f;

        /// <summary>Lado mínimo de un control (07 §1.2 y §7.8): el alto táctil de «Ver mapa».</summary>
        public const float MinTouchDp = 48f;

        /// <summary>
        /// El viaje empieza a los <see cref="TravelDelayMs"/> de entrar la tarjeta (ms) [P], cuando ya se ve entera y Capi empieza a
        /// presentar.
        /// </summary>
        public const float TravelDelayMs = 250f;

        /// <summary>
        /// Lo que tarda la canoa en recorrer el tramo de la gran parada (ms) [P]: lo que deja el tope de
        /// <see cref="MapConfig.WorldTravelInBannerMaxMs"/> (≤ 4 s, D-013, punto 7) tras <see cref="TravelDelayMs"/> y el izado de
        /// <see cref="MapConfig.FlagRaiseMs"/>, así que el viaje entero cabe siempre en el tope.
        /// </summary>
        public const float CanoeTravelMs = MapConfig.WorldTravelInBannerMaxMs - TravelDelayMs - MapConfig.FlagRaiseMs;

        private const float PadDp = 16f;
        private const float HeaderDp = 40f;
        private const float TitleInsetDp = 8f;
        private const float FitSlackDp = 4f;
        private const float SceneEdgeDp = 2f;
        private const float PopFrom = 0.92f;
        private const float QuestionGapDp = 4f;
        private const float StripGapDp = 8f;
        private const float MapButtonGapDp = 8f;
        private const float MapButtonPadDp = 20f;

        private readonly ProceduralArt _art;
        private readonly Action _onTap;
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _dim;
        private readonly RectTransform _pop;
        private readonly CanvasGroup _popGroup;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly Image _sceneEdge;
        private readonly RectTransform _sceneMask;
        private readonly Image _scene;
        private readonly Image _flatWater;
        private readonly RectTransform _rosaClip;
        private readonly Image _rosa;
        private readonly Image _foam;
        private readonly Image _spray;
        private readonly Image _ripple;
        private readonly RectTransform _capi;
        private readonly Image _paw;
        private readonly TextMeshProUGUI _place;
        private readonly TextMeshProUGUI _question;
        private readonly BannerTravelStrip _strip;
        private readonly ReliefParts _mapButton;
        private readonly TextMeshProUGUI _mapLabel;
        private readonly Action? _onMap;
        private BannerTravel? _travel;
        private bool _travelSkipped;
        private float _mapButtonW;
        private int _world;
        private float _fontScale = 1f;
        private float _cardW;
        private float _cardH;
        private string _text = string.Empty;
        private string _placeText = string.Empty;
        private bool _nameInTitle;
        private Color _water;
        private float _ms = -1f;
        private float _exitMs = -1f;
        private bool _dismissing;
        private bool _entered;
        private bool _enteredFired;
        private bool _settled;
        private bool _reduceMotion;
        private bool _artReady;
        private string? _fallback;
        private bool _cardShownFired;
        private bool _veilEarly;
        private long _requestTicks;
        private float _artWaitMs;
        private float _canvasW = 360f;
        private float _canvasH = 780f;
        private float _safeTop = 32f;
        private float _safeBottom = 24f;
        private float _sceneW;
        private float _sceneH;
        private float _fadeFrom = 1f;
        private float _cardFrom = 1f;
        private float _scaleFrom = 1f;
        private float _rosaRise;

        /// <summary>
        /// Crea el banner, oculto, bajo <paramref name="parent"/>; <paramref name="onTap"/> recibe el toque y
        /// <paramref name="onMap"/>, «Ver mapa» del viaje de mundo (M3.6; sin él, el botón no hace nada). No pide ningún arte
        /// (las mascotas y la viñeta se generan al abrirlo, <see cref="Show"/>) ni construye ninguna malla del viaje.
        /// </summary>
        public WorldBannerView(Transform parent, ProceduralArt art, Action onTap, Action? onMap = null)
        {
            _art = art;
            _onTap = onTap ?? throw new ArgumentNullException(nameof(onTap));
            _onMap = onMap;
            _root = UiBuilder.Rect("WorldBanner", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _dim = ModalKit.Dim("Dim", _root, DimAlpha);
            _dim.gameObject.AddComponent<PointerTapRelay>().Bind(OnTap);
            _pop = UiBuilder.Rect("Pop", _root);
            _popGroup = _pop.gameObject.AddComponent<CanvasGroup>(); // la tarjeta, aparte del velo: el velo sale antes si el arte tarda
            _panel = ModalKit.Card("Panel", _pop, art, out _surface);
            _title = ModalKit.Header(_panel, art, string.Empty, out _header);
            _title.enableAutoSizing = false; // a 24 sp: con el nombre si cabe; si no, «Mundo {n}» (Layout)
            _title.fontSize = TitleSp;

            // Viñeta del río con el canto de madera oscura; Rosa en su recorte (todo lo de debajo de la línea del agua no se ve)
            // y, encima, la espuma que tapa el corte. Los sprites de las mascotas, al abrirlo (EnsureArt).
            _sceneEdge = UiBuilder.Sliced("SceneEdge", _panel, art.RoundedRect, Palette.FamilyWood.Outline);
            _sceneEdge.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / (ProceduralArt.BannerSceneRadiusDp + SceneEdgeDp);
            _sceneMask = UiBuilder.Rect("Scene", _panel);
            _sceneMask.gameObject.AddComponent<RectMask2D>();
            _scene = UiBuilder.Image("River", _sceneMask, null, Color.white);

            // Respaldo sin arte: la viñeta del color plano del agua, con las esquinas de la viñeta pintada (Fallback).
            _flatWater = UiBuilder.Sliced("FlatWater", _sceneMask, art.RoundedRect, Color.white);
            _flatWater.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / ProceduralArt.BannerSceneRadiusDp;
            _flatWater.gameObject.SetActive(false);
            _ripple = UiBuilder.Image("Ripple", _sceneMask, null, new Color(1f, 1f, 1f, 0f));
            _rosaClip = UiBuilder.Rect("RosaClip", _sceneMask);
            _rosaClip.gameObject.AddComponent<RectMask2D>();
            _rosa = UiBuilder.Image("Rosa", _rosaClip, null, Color.white);
            _foam = UiBuilder.Image("Foam", _sceneMask, null, new Color(1f, 1f, 1f, 0f));
            _spray = UiBuilder.Image("Spray", _sceneMask, null, new Color(1f, 1f, 1f, 0f));

            // Capi, en el banco de arena de la izquierda: el cuerpo va dibujado en la viñeta y aquí solo queda su cuadrado, sin
            // gráfico, donde va la pata, delante del cuerpo, que gira en el hombro.
            _capi = UiBuilder.Rect("Capi", _panel);
            _paw = UiBuilder.Image("CapiPaw", _capi, null, Color.white);

            _place = ModalKit.Line("Place", _panel, string.Empty, PlaceSp, Palette.FamilyWood.Deep);
            _place.enableAutoSizing = false;

            // El viaje de mundo (M3.6; Q4-D): la pregunta del mundo, la franja del Mapa con su tramo y «Ver mapa». Ocultos sin
            // viaje; las mallas de la franja, solo al abrir un banner con viaje.
            _question = ModalKit.Line("WorldQuestion", _panel, string.Empty, QuestionSp, Palette.FamilyWood.Deep);
            _question.gameObject.SetActive(false);
            _strip = new BannerTravelStrip(_panel, art);
            _mapButton = UiBuilder.Relief("MapButton", _panel, art.CtaWood, OnMapPressed);
            _mapLabel = UiBuilder.Label("Label", _mapButton.Face, TextStyle.PillWood, UiText.MapOpen, MapButtonSp, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(_mapLabel.rectTransform, art.CtaWood);
            UiBuilder.AutoSize(_mapLabel, UiBuilder.MinTextSp, MapButtonSp);
            _mapButton.Relief.Bind(_mapButton.Face, _mapButton.Base, _mapButton.FaceImage, art.CtaWood, null, null, _mapLabel);
            ShowMapButton(false);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Raíz del banner (con el velo), estirada a la pantalla.</summary>
        public RectTransform Root => _root;

        /// <summary>
        /// Escala de la fuente del sistema, entre 1 y <see cref="LevelCardView.MaxFontScale"/> (02 §12), como la de la tarjeta
        /// previa. Quien la cambia con el banner abierto llama después a <see cref="Relayout"/>.
        /// </summary>
        public float FontScale
        {
            get => _fontScale;
            set => _fontScale = Mathf.Clamp(value, 1f, LevelCardView.MaxFontScale);
        }

        /// <summary>Medidas de la superficie de madera de la tarjeta colocada (dp; las de <c>ModalKit.Size</c> compartida).</summary>
        public Vector2 SurfaceSizeDp => new Vector2(_cardW - 16f, ModalKit.SharedSurfaceHeight(_cardH - 16f));

        /// <summary>El banner se ve (también mientras sale).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>El banner está saliendo (Rosa se sumerge y se funde); un toque más termina la salida en el acto.</summary>
        public bool Dismissing => _dismissing;

        /// <summary>Terminó la entrada y está quieto: <see cref="Tick"/> no pinta nada hasta que sale.</summary>
        public bool Settled => _settled;

        /// <summary>
        /// El arte se pinta en un hilo del grupo y el banner espera a tenerlo (el juego); false, en el acto al abrirlo (los
        /// tests y las capturas que miden instantes exactos: <c>LaunchOptions.BannerArtSyncOverride</c>). Lo que se pinta en el
        /// acto, también la superficie de madera de la tarjeta, cuenta como pintado en el hilo principal
        /// (<c>ProceduralArt.ModalSurfaceOriginOf</c> da <c>MainThread</c>, y <c>OtherThread</c> en un hilo del grupo; revisión
        /// de T52, U1): el diagnóstico de los tests no confunde los dos caminos.
        /// </summary>
        public bool AsyncArt { get; set; } = true;

        /// <summary>El arte del banner abierto ya está puesto y la entrada corre (o ya terminó).</summary>
        public bool ArtReady => _artReady;

        /// <summary>
        /// La tarjeta del banner abierto se ve (o está entrando): con su arte (<see cref="ArtReady"/>) o con el respaldo
        /// (<see cref="Fallback"/>). Antes, solo el velo, y los toques se ignoran.
        /// </summary>
        public bool CardShown => _artReady || _fallback != null;

        /// <summary>El banner está abierto con el velo, esperando su arte (aún sin tarjeta; los toques se ignoran).</summary>
        public bool Waiting => Visible && !CardShown;

        /// <summary>
        /// Por qué entró sin arte (<see cref="FallbackError"/>, <see cref="FallbackQueue"/> o <see cref="FallbackTimeout"/>), o
        /// <see cref="FallbackNone"/> (con su arte, esperándolo o cerrado).
        /// </summary>
        public string Fallback => _fallback ?? FallbackNone;

        /// <summary>
        /// Tiempo límite del arte (ms de reloj real); por defecto <see cref="ArtTimeoutMs"/> [P]. Los tests lo bajan
        /// (<c>LaunchOptions.BannerArtTimeoutMsOverride</c>) o lo suben para no depender de la máquina.
        /// </summary>
        public float ArtTimeoutLimitMs { get; set; } = ArtTimeoutMs;

        /// <summary>
        /// Lo que esperó el banner abierto a su arte o a su respaldo: ms de reloj real (<see cref="System.Diagnostics.Stopwatch"/>)
        /// desde que lo pidió al abrirse hasta que la tarjeta empezó a entrar (≈ 0 si ya estaba).
        /// </summary>
        public float ArtWaitMs => _artWaitMs;

        /// <summary>Mundo que presenta (0 si no hay).</summary>
        public int World => _world;

        /// <summary>«Mundo {n}: {nombre} · {país} · {río}» (clave <c>world_banner</c>), o null si no se ve.</summary>
        public string? Text => Visible ? _text : null;

        /// <summary>Título de la cabecera: «Mundo {n}: {nombre}» o, si el nombre no cabe a 24 sp, «Mundo {n}».</summary>
        public string TitleText => _title.text;

        /// <summary>
        /// Línea del lugar sin el corte de línea ni la negrita: «{país} · {río}» o, con «Mundo {n}» en la cabecera,
        /// «{nombre} · {país} · {río}». La cabecera y la línea forman <see cref="Text"/>.
        /// </summary>
        public string PlaceText => _placeText;

        /// <summary>El nombre del mundo va en la cabecera («Mundo {n}: {nombre}»); si no, abre la línea del lugar en negrita.</summary>
        public bool NameInTitle => _nameInTitle;

        /// <summary>Velo a pantalla completa que recibe el toque (tests).</summary>
        public Image Dim => _dim;

        /// <summary>Cuerpo de la tarjeta (tests y AF_LAYOUT).</summary>
        public RectTransform Panel => _panel;

        /// <summary>Superficie de madera del cuerpo (tests: alto compartido).</summary>
        public Image Surface => _surface;

        /// <summary>Viñeta del río (tests de solapes).</summary>
        public RectTransform Scene => _sceneMask;

        /// <summary>Sprite de la viñeta (tests: el agua del mundo; null cerrado, esperando el arte o con el respaldo).</summary>
        public Sprite? SceneSprite => _scene.sprite;

        /// <summary>Viñeta del respaldo, del color plano del agua (tests; activa solo con <see cref="Fallback"/>).</summary>
        public Image FlatWater => _flatWater;

        /// <summary>Cuadrado de Capi en la viñeta (el cuerpo va dibujado en ella; aquí cuelga la pata; tests).</summary>
        public RectTransform CapiRect => _capi;

        /// <summary>Pata que presenta (tests).</summary>
        public Image Paw => _paw;

        /// <summary>Rosa (tests).</summary>
        public Image Rosa => _rosa;

        /// <summary>Espuma sobre el corte del cuerpo de Rosa con el agua (tests).</summary>
        public Image Foam => _foam;

        /// <summary>Soplo de la exhalación (tests).</summary>
        public Image Spray => _spray;

        /// <summary>Recorte de Rosa: su borde de abajo es la línea del agua (tests).</summary>
        public RectTransform RosaClip => _rosaClip;

        /// <summary>Línea del lugar (tests de desbordes).</summary>
        public TextMeshProUGUI PlaceLabel => _place;

        /// <summary>Título (tests de desbordes).</summary>
        public TextMeshProUGUI TitleLabel => _title;

        /// <summary>Ángulo de la pata (grados; <see cref="PawDownDeg"/> → <see cref="PawUpDeg"/>).</summary>
        public float PawAngle => _paw.rectTransform.localEulerAngles.z > 180f ? _paw.rectTransform.localEulerAngles.z - 360f : _paw.rectTransform.localEulerAngles.z;

        /// <summary>Cuánto ha asomado Rosa (0 = bajo el agua, 1 = fuera).</summary>
        public float RosaRise => _rosaRise;

        /// <summary>Opacidad de la tarjeta (fundidos; 0 mientras espera el arte): la del velo por la suya.</summary>
        public float Alpha => CardShown ? _group.alpha * _popGroup.alpha : 0f;

        /// <summary>Opacidad del velo (1 mientras espera el arte: sale en el acto).</summary>
        public float VeilAlpha => _group.alpha;

        /// <summary>Giro de Rosa (grados; positivo, antihorario: al sumergirse, el pico hacia el agua).</summary>
        public float RosaTilt => _rosa.rectTransform.localEulerAngles.z > 180f ? _rosa.rectTransform.localEulerAngles.z - 360f : _rosa.rectTransform.localEulerAngles.z;

        /// <summary>El banner recibe toques (también mientras sale: un segundo toque termina la salida).</summary>
        public bool BlocksRaycasts => _group.blocksRaycasts;

        /// <summary>Contenedor que escala la tarjeta en la entrada y en la salida (tests).</summary>
        public RectTransform Pop => _pop;

        /// <summary>Viaje de mundo del banner abierto (M3.6), o null: el banner de siempre.</summary>
        public BannerTravel? Travel => Visible ? _travel : null;

        /// <summary>
        /// Lo que dura el viaje del banner abierto desde que entra la tarjeta (ms; <c>travel_ms</c> de la sonda): la espera, la
        /// canoa y, si se iza, la bandera (≤ <see cref="MapConfig.WorldTravelInBannerMaxMs"/>); con Reducir movimiento, el
        /// fundido de <see cref="MapConfig.ReducedMotionFadeMs"/>; 0 sin viaje.
        /// </summary>
        public float TravelMs
        {
            get
            {
                if (Travel == null)
                {
                    return 0f;
                }

                if (_reduceMotion)
                {
                    return MapConfig.ReducedMotionFadeMs;
                }

                return TravelDelayMs + CanoeTravelMs + (_strip.FlagSprite != null ? MapConfig.FlagRaiseMs : 0f);
            }
        }

        /// <summary>El viaje del banner abierto está en marcha: el próximo toque lo completa en vez de cerrar (M3.6).</summary>
        public bool TravelRunning => Travel != null && CardShown && !_dismissing && !_travelSkipped && _ms < TravelMs;

        /// <summary>El viaje del banner abierto terminó (o se completó con un toque).</summary>
        public bool TravelDone => Travel != null && CardShown && (_travelSkipped || _ms >= TravelMs);

        /// <summary>
        /// Bandera que iza la canoa del viaje abierto (ISO2 de <c>flag_raise</c>), solo si se pinta: con su PNG oficial o, con
        /// <see cref="MapArtRule"/>, la simplificada (<see cref="ProceduralArt.MapFlag"/>); null si no hay (o en release sin
        /// el PNG).
        /// </summary>
        public string? TravelFlag => Travel != null && _strip.FlagSprite != null ? Travel.Flag : null;

        /// <summary>La franja del viaje (tests: medidas, solapes y la posición de la canoa).</summary>
        public BannerTravelStrip TravelStrip => _strip;

        /// <summary>Pregunta del mundo sobre la franja (tests; inactiva sin viaje o sin pregunta).</summary>
        public TextMeshProUGUI QuestionLabel => _question;

        /// <summary>«Ver mapa» (tests; inactivo y sin recibir toques sin viaje).</summary>
        public Button MapButton => _mapButton.Button;

        /// <summary>Texto de «Ver mapa» (tests).</summary>
        public TextMeshProUGUI MapButtonLabel => _mapLabel;

        /// <summary>
        /// Completa el viaje (el primer toque, o «Ver mapa»): la canoa en la gran parada, la bandera izada y la entrada entera,
        /// quieto en el acto. Sin efecto sin viaje en marcha.
        /// </summary>
        public void CompleteTravel()
        {
            if (Travel == null || !CardShown || _dismissing || _travelSkipped)
            {
                return;
            }

            _travelSkipped = true;
            _ms = Mathf.Max(_ms, Mathf.Max(SettleMs(), TravelMs));
            Apply();
            _settled = true;
        }

        /// <summary>Duración de la salida (ms): la que el flujo espera antes de la tarjeta previa o la intro.</summary>
        public static float ExitDurationMs(bool reduceMotion)
        {
            return reduceMotion ? ExitReducedMs : ExitMs;
        }

        /// <summary>
        /// Segunda agua de la viñeta del mundo <paramref name="world"/> [P]: el mundo 3, Manaos, es «Negro + Solimões (negra /
        /// blanca)» (glosario §8), el Encuentro de las Aguas: el agua base del mundo (la negra, té oscuro) a la izquierda y la
        /// blanca (café con leche) a la derecha, con una costura suave. Los demás mundos de la 1.0, una sola agua. Pendiente de
        /// revisión del asesor cultural.
        /// </summary>
        public static Color? MeetingWater(int world)
        {
            return world == 3 ? Palette.WaterWhite : (Color?)null;
        }

        /// <summary>Gráficos del banner que reciben toques (tests: solo el velo).</summary>
        public List<Graphic> RaycastTargets()
        {
            var list = new List<Graphic>();
            Graphic[] all = _root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].raycastTarget)
                {
                    list.Add(all[i]);
                }
            }

            return list;
        }

        /// <summary>
        /// Muestra el banner del mundo <paramref name="world"/> con el agua <paramref name="water"/> del mundo, colocado en la
        /// pantalla de <paramref name="canvasW"/> × <paramref name="canvasH"/> dp con la zona segura dada; entra con un fundido,
        /// Rosa asoma y Capi presenta (todo quieto con <paramref name="reduceMotion"/>). Aquí se pide el arte (si no se pidió
        /// antes con <see cref="Prefetch"/>) y empieza a contar su tiempo límite; si no está, sale solo el velo
        /// (<see cref="Waiting"/>) hasta que llega o hasta el respaldo. Con <paramref name="travel"/>, el viaje de mundo dentro
        /// (M3.6; Q4-D): la pregunta, la franja del Mapa con el tramo de la gran parada y «Ver mapa»; sin él, el banner de siempre.
        /// </summary>
        public void Show(int world, Color water, bool reduceMotion, float canvasW, float canvasH, float safeTop, float safeBottom, BannerTravel? travel = null)
        {
            _world = world;
            _text = UiText.WorldBanner(world);
            _water = water;
            _reduceMotion = reduceMotion;
            _travel = travel;
            _travelSkipped = false;
            if (travel != null)
            {
                SetText(_mapLabel, UiText.MapOpen); // el idioma de ahora (Ajustes puede cambiarlo entre dos banners)
            }
            _root.gameObject.SetActive(true);
            _dismissing = false;
            _entered = false;
            _enteredFired = false;
            _settled = false;
            _exitMs = -1f;
            _ms = -1f;
            _artReady = false;
            _fallback = null;
            _cardShownFired = false;
            _veilEarly = false;
            _artWaitMs = 0f;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _group.alpha = 1f; // el velo: en el acto si el arte tarda; si ya está, la entrada lo funde desde 0 con la tarjeta
            _popGroup.alpha = 1f;
            SetCardActive(true); // colocada aunque aún no se vea
            _requestTicks = System.Diagnostics.Stopwatch.GetTimestamp(); // desde aquí cuentan el tiempo límite y wait_ms
            Layout(canvasW, canvasH, safeTop, safeBottom); // pide el arte con las medidas de la viñeta
            if (!TryApplyArt())
            {
                // Aún sin arte: el velo ya, la tarjeta cuando llegue (o con el respaldo). Los toques se ignoran mientras.
                _veilEarly = true;
                SetCardActive(false);
            }
        }

        /// <summary>
        /// Pide ya, con el banner cerrado, el arte del banner del mundo <paramref name="world"/> que se abrirá en una pantalla de
        /// <paramref name="canvasW"/> dp de ancho (la victoria del nivel anterior o «Jugar» en el inicio; GameScreen): con las
        /// mismas medidas que <see cref="Show"/>, así que la petición de Show es la misma y el arte, pintado en otro hilo mientras
        /// tanto, ya está al abrirlo. Sin efecto con el banner abierto. No crea ninguna textura (eso, al abrirlo). Con
        /// <paramref name="travel"/>, el del viaje que llevará (la tarjeta es más alta y la viñeta puede cambiar de alto).
        /// </summary>
        public void Prefetch(int world, Color water, float canvasW, BannerTravel? travel = null)
        {
            if (Visible || world <= 0)
            {
                return;
            }

            Measure(world, canvasW, travel, out float cardW, out float inner, out _, out _, out float sceneH, out _, out _, out float cardH);
            _art.RequestBannerArt(inner, sceneH, water, MeetingWater(world), world, true, cardW - 16f, ModalKit.SharedSurfaceHeight(cardH - 16f));
        }

        /// <summary>
        /// Coloca el banner en la pantalla de <paramref name="canvasW"/> × <paramref name="canvasH"/> dp: tan ancho como la
        /// tarjeta previa, centrado a lo ancho y al 45 % del alto libre, dentro de la zona segura.
        /// </summary>
        public void Layout(float canvasW, float canvasH, float safeTop, float safeBottom)
        {
            _canvasW = canvasW;
            _canvasH = canvasH;
            _safeTop = safeTop;
            _safeBottom = safeBottom;
            if (_world == 0)
            {
                return;
            }

            float y = PadDp;
            Measure(_world, canvasW, _travel, out float cardW, out float inner, out float headerH, out float questionH, out float sceneH, out float placeH,
                out float buttonH, out float cardH);
            UiBuilder.Place(_header.rectTransform, PadDp, y, inner, headerH);
            y += headerH + 8f;

            // El viaje (M3.6): la pregunta del mundo encima y la franja del Mapa, bajo la cabecera y antes de la viñeta.
            SetActive(_question.gameObject, _travel != null && questionH > 0f);
            if (_travel != null)
            {
                if (questionH > 0f)
                {
                    UiBuilder.Place(_question.rectTransform, PadDp, y, inner, questionH);
                    y += questionH + QuestionGapDp;
                }

                float stripW = Mathf.Min(StripWidthDp, inner);
                _strip.Place(PadDp + ((inner - stripW) * 0.5f), y, stripW, StripHeightDp);
                _strip.Build(_travel, _reduceMotion);
                y += StripHeightDp + StripGapDp;
            }
            else
            {
                _strip.Hide();
            }

            float sceneY = y;
            _sceneW = inner;
            _sceneH = sceneH;
            UiBuilder.Place(_sceneEdge.rectTransform, PadDp - SceneEdgeDp, sceneY - SceneEdgeDp, inner + (2f * SceneEdgeDp), sceneH + (2f * SceneEdgeDp));
            UiBuilder.Place(_sceneMask, PadDp, sceneY, inner, sceneH);
            UiBuilder.Place(_scene.rectTransform, 0f, 0f, inner, sceneH);
            UiBuilder.Place(_flatWater.rectTransform, 0f, 0f, inner, sceneH);
            if (_artReady)
            {
                // Otra pantalla con el banner ya a la vista (rara vez): la viñeta nueva, en el acto.
                _scene.sprite = _art.BannerScene(inner, sceneH, _water, MeetingWater(_world), _world);
            }
            else if (_fallback == null)
            {
                // Al abrirlo: la viñeta (con Capi ya dibujado), las mascotas y la superficie de madera de la tarjeta, fuera del
                // hilo principal si AsyncArt. Con el respaldo ya no se pide nada: lo que llegara después se descartaría.
                _art.RequestBannerArt(inner, sceneH, _water, MeetingWater(_world), _world, AsyncArt, cardW - 16f, ModalKit.SharedSurfaceHeight(cardH - 16f));
            }

            y += sceneH + 10f;
            UiBuilder.Place(_place.rectTransform, PadDp, y, inner, placeH);
            y += placeH;

            // «Ver mapa», centrado bajo la línea del lugar, con su alto táctil de 48 dp y la cara del botón de madera centrada.
            ShowMapButton(_travel != null);
            if (_travel != null)
            {
                float face = _art.CtaWood.HeightDp;
                UiBuilder.Place((RectTransform)_mapButton.Button.transform, PadDp + ((inner - _mapButtonW) * 0.5f), y + MapButtonGapDp, _mapButtonW, buttonH);
                RectTransform visual = (RectTransform)_mapButton.Face.parent;
                visual.anchoredPosition = new Vector2(0f, -Mathf.Max(0f, (buttonH - face) * 0.5f));
            }

            // Rosa: recortada en la línea del agua; su pie (pivote) baja hasta que solo asoma RosaAbove de su alto. La espuma,
            // centrada en la línea del agua sobre el corte del lomo.
            float waterline = sceneH * RosaWaterline;
            UiBuilder.Place(_rosaClip, 0f, 0f, inner, waterline);
            _rosa.rectTransform.anchorMin = new Vector2(0f, 1f);
            _rosa.rectTransform.anchorMax = new Vector2(0f, 1f);
            _rosa.rectTransform.pivot = new Vector2(0.5f, 0f);
            _rosa.rectTransform.sizeDelta = new Vector2(ProceduralArt.RosaWidthDp, ProceduralArt.RosaHeightDp);
            Center(_ripple.rectTransform, inner * RosaX, waterline, ProceduralArt.RosaRippleWidthDp, ProceduralArt.RosaRippleHeightDp);
            float foamX = (inner * RosaX) + ((ProceduralArt.RosaCutCenter - 0.5f) * ProceduralArt.RosaWidthDp);
            Center(_foam.rectTransform, foamX, waterline, ProceduralArt.RosaFoamWidthDp, ProceduralArt.RosaFoamHeightDp);
            _spray.rectTransform.anchorMin = new Vector2(0f, 1f);
            _spray.rectTransform.anchorMax = new Vector2(0f, 1f);
            _spray.rectTransform.pivot = new Vector2(0.5f, 0f);
            _spray.rectTransform.sizeDelta = new Vector2(ProceduralArt.RosaSprayDp, ProceduralArt.RosaSprayDp);

            // El cuadrado de Capi, donde la viñeta lo dibuja sentado en el banco (su pivote abajo en el centro: las patas), y la
            // pata en su hombro.
            float bank = sceneY + (sceneH * ProceduralArt.BannerSceneBankTop);
            RectTransform capi = _capi;
            capi.anchorMin = new Vector2(0f, 1f);
            capi.anchorMax = new Vector2(0f, 1f);
            capi.pivot = new Vector2(0.5f, 0f);
            capi.sizeDelta = new Vector2(ProceduralArt.CapiPresentDp, ProceduralArt.CapiPresentDp);
            capi.anchoredPosition = new Vector2(PadDp + (inner * CapiX), -(bank + ProceduralArt.BannerSceneCapiFootDp));
            RectTransform paw = _paw.rectTransform;
            Vector2 shoulder = ProceduralArt.CapiShoulder;
            paw.anchorMin = new Vector2(shoulder.x, shoulder.y);
            paw.anchorMax = new Vector2(shoulder.x, shoulder.y);
            paw.pivot = ProceduralArt.CapiPawPivot;
            paw.sizeDelta = new Vector2(ProceduralArt.CapiPawLengthDp, ProceduralArt.CapiPawThickDp);
            paw.anchoredPosition = Vector2.zero;

            float top = Mathf.Round((canvasH - cardH) * 0.45f);
            top = Mathf.Clamp(top, safeTop + 8f, Mathf.Max(safeTop + 8f, canvasH - safeBottom - 8f - cardH));
            float left = (canvasW - cardW) * 0.5f;
            _pop.anchorMin = new Vector2(0f, 1f);
            _pop.anchorMax = new Vector2(0f, 1f);
            _pop.pivot = new Vector2(0.5f, 0.5f);
            _pop.sizeDelta = new Vector2(cardW, cardH);
            _pop.anchoredPosition = new Vector2(left + (cardW * 0.5f), -(top + (cardH * 0.5f)));

            // La superficie de madera, solo con la tarjeta a la vista: mientras espera su arte, el mismo trabajo la pinta en otro
            // hilo (StartEntry la pone al entrar); pedirla aquí la pintaría en el hilo principal (T52).
            _cardW = cardW;
            _cardH = cardH;
            UiBuilder.Place(_panel, 0f, 0f, cardW, cardH);
            if (CardShown)
            {
                ApplySurface();
            }

            Apply();
        }

        /// <summary>Vuelve a colocar el banner abierto con la última pantalla.</summary>
        public void Relayout()
        {
            Layout(_canvasW, _canvasH, _safeTop, _safeBottom);
        }

        /// <summary>
        /// Empieza la salida: Rosa se sumerge y después la tarjeta se funde. Sigue recibiendo el toque (el velo): un segundo
        /// toque la termina en el acto (GameScreen).
        /// </summary>
        public void Dismiss()
        {
            if (!Visible || _dismissing)
            {
                return;
            }

            _dismissing = true;
            _settled = false;
            _fadeFrom = _group.alpha;
            _cardFrom = _popGroup.alpha;
            _scaleFrom = _pop.localScale.x;
            _exitMs = 0f;
        }

        /// <summary>
        /// True una sola vez, cuando la tarjeta del banner abierto empieza a entrar: con su arte o con el respaldo (para el
        /// <c>AF_STATE world_banner_art</c>, y para que el flujo deje de ignorar los toques).
        /// </summary>
        public bool ConsumeCardShown()
        {
            if (!CardShown || _cardShownFired)
            {
                return false;
            }

            _cardShownFired = true;
            return true;
        }

        /// <summary>Lo oculta en el acto (al terminar la salida o al cargar otro nivel) y suelta su arte.</summary>
        public void HideNow()
        {
            _dismissing = false;
            _settled = false;
            _ms = -1f;
            _exitMs = -1f;
            _artReady = false;
            _fallback = null;
            _cardShownFired = false;
            _veilEarly = false;
            _world = 0;
            _travel = null;
            _travelSkipped = false;
            _strip.Hide();
            SetActive(_question.gameObject, false);
            ShowMapButton(false);
            _group.alpha = 1f;
            _popGroup.alpha = 1f;
            _pop.localScale = Vector3.one;
            SetCardActive(true);
            ShowArtPieces(true);
            if (_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }

            ReleaseArt();
        }

        /// <summary>True una sola vez, cuando la tarjeta termina de entrar (para el AF_LAYOUT del banner).</summary>
        public bool ConsumeEntered()
        {
            if (!_entered || _enteredFired)
            {
                return false;
            }

            _enteredFired = true;
            return true;
        }

        /// <summary>
        /// Avanza la entrada (fundido, Rosa, el soplo y la pata de Capi) y la salida. Entre las dos, con todo quieto
        /// (<see cref="Settled"/>), no pinta nada. Mientras espera el arte, solo mira si llegó o si pasó su tiempo límite (reloj
        /// real; sin asignaciones).
        /// </summary>
        public void Tick(float dtMs)
        {
            if (!Visible)
            {
                return;
            }

            if (!CardShown)
            {
                // Esperando el arte que se pinta en otro hilo: la tarjeta entra cuando llega o con el respaldo (desde t = 0).
                if (!_dismissing)
                {
                    TryApplyArt();
                }

                return;
            }

            if (_ms < 0f)
            {
                return;
            }

            _ms += dtMs;
            if (_dismissing)
            {
                _exitMs += dtMs;
                if (_exitMs >= (_reduceMotion ? ExitReducedMs : ExitMs))
                {
                    HideNow();
                    return;
                }
            }

            if (!_entered && _ms >= FadeInMs)
            {
                _entered = true;
            }

            if (_settled)
            {
                return; // quieto: ninguna transformada ni ningún color que cambie (guía 8)
            }

            Apply();
            if (!_dismissing && (_ms >= SettleMs() || _travelSkipped))
            {
                _settled = true; // Apply acaba de pintar el estado final de la entrada (y del viaje)
            }
        }

        // Desde cuándo (ms desde que entra la tarjeta) todo queda quieto: el fin de la entrada y, con viaje, el fin del viaje.
        private float SettleMs()
        {
            float entry = _reduceMotion || _fallback != null ? FadeInMs : EnterMs;
            return _travel != null ? Mathf.Max(entry, TravelMs) : entry;
        }

        // Si el arte pedido al abrirlo ya está (en el acto o pintado en otro hilo), lo pone y empieza la entrada desde t = 0; si
        // no llegará (el trabajo falló o no se pudo encolar) o pasó el tiempo límite, la entrada empieza con el respaldo. True si
        // la tarjeta ya se ve.
        private bool TryApplyArt()
        {
            if (CardShown)
            {
                return true;
            }

            if (_world == 0)
            {
                return false;
            }

            if (_art.TryTakeBannerArt())
            {
                _scene.sprite = _art.BannerSceneSprite;
                EnsureArt();
                _artReady = true;
                StartEntry();
                return true;
            }

            BannerArtFailure failure = _art.BannerArtFailure;
            if (failure != BannerArtFailure.None)
            {
                StartFallback(failure == BannerArtFailure.Queue ? FallbackQueue : FallbackError);
                return true;
            }

            if (!_art.BannerArtPending)
            {
                StartFallback(FallbackError); // nada pendiente ni listo: no llegará nunca
                return true;
            }

            if (ElapsedMs() >= ArtTimeoutLimitMs)
            {
                _art.AbandonBannerArt(); // el trabajo para y lo que pinte ya no se recoge
                StartFallback(FallbackTimeout);
                return true;
            }

            return false;
        }

        // Sin arte (D-012 sin las mascotas): la tarjeta, la placa y el texto, con la viñeta del color plano del agua y sin Rosa ni
        // Capi; nada de lo que llegue después se pone.
        private void StartFallback(string reason)
        {
            _fallback = reason;
            _scene.sprite = null;
            _scene.enabled = false;
            _flatWater.color = _water;
            _flatWater.gameObject.SetActive(true);
            ShowArtPieces(false);
            StartEntry();
        }

        // La tarjeta empieza a entrar desde t = 0 (con su arte o con el respaldo): lo que esperó, en reloj real.
        private void StartEntry()
        {
            ApplySurface(); // ya en la caché si llegó el arte (se pintó con él); con el respaldo, aquí
            _artWaitMs = (float)ElapsedMs();
            _ms = 0f;
            SetCardActive(true);
            Apply();
        }

        // La superficie de madera de la tarjeta colocada: la de la escala de altos compartidos, estirada (ModalKit.Size).
        private void ApplySurface()
        {
            ModalKit.Size(_panel, _surface, _art, 0f, 0f, _cardW, _cardH, true);
        }

        // Milisegundos de reloj real desde que el banner pidió su arte (sin asignaciones).
        private double ElapsedMs()
        {
            long ticks = System.Diagnostics.Stopwatch.GetTimestamp() - _requestTicks;
            return ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        }

        private void SetCardActive(bool active)
        {
            if (_pop.gameObject.activeSelf != active)
            {
                _pop.gameObject.SetActive(active);
            }
        }

        // Rosa (con la espuma, el soplo y la onda) y la pata de Capi: fuera con el respaldo, de vuelta al cerrarse.
        private void ShowArtPieces(bool on)
        {
            SetActive(_rosaClip.gameObject, on);
            SetActive(_foam.gameObject, on);
            SetActive(_spray.gameObject, on);
            SetActive(_ripple.gameObject, on);
            SetActive(_capi.gameObject, on);
            if (on)
            {
                _scene.enabled = true;
                SetActive(_flatWater.gameObject, false);
            }
        }

        // Las mascotas, la onda, la espuma y el soplo, ya generados con la viñeta (ProceduralArt.RequestBannerArt): construir la
        // pantalla o jugar un nivel sin banner no genera ninguna textura.
        private void EnsureArt()
        {
            if (_ripple.sprite == null)
            {
                _ripple.sprite = _art.RosaRipple;
            }

            if (_rosa.sprite == null)
            {
                _rosa.sprite = _art.Rosa;
            }

            if (_foam.sprite == null)
            {
                _foam.sprite = _art.RosaFoam;
            }

            if (_spray.sprite == null)
            {
                _spray.sprite = _art.RosaSpray;
            }

            if (_paw.sprite == null)
            {
                _paw.sprite = _art.CapiPaw;
            }

            _strip.SetGuideSprite(_rosa.sprite); // el guía de la franja, con el mismo sprite de Rosa ya pintado (sin él, sin guía)
        }

        // Al cerrarse: fuera las referencias y el arte del banner (cada banner sale una vez por mundo; el siguiente lo genera).
        private void ReleaseArt()
        {
            _scene.sprite = null;
            _ripple.sprite = null;
            _rosa.sprite = null;
            _foam.sprite = null;
            _spray.sprite = null;
            _paw.sprite = null;
            _strip.SetGuideSprite(null);
            _art.ReleaseBannerArt();
        }

        // Pinta el instante actual de la entrada o de la salida.
        private void Apply()
        {
            if (_world == 0 || !CardShown)
            {
                return; // aún sin tarjeta: solo el velo, que Show ya puso
            }

            float ms = Mathf.Max(0f, _ms);
            bool still = _reduceMotion;

            // La tarjeta: fundido y leve rebote (sin rebote con Reducir movimiento); al salir, fundido desde donde estaba. Si el
            // velo salió antes (esperando el arte), se queda entero y solo entra la tarjeta.
            float enter = Mathf.Clamp01(ms / FadeInMs);
            float card = Ease.Out(enter);
            float veil = _veilEarly ? 1f : card;
            card = _veilEarly ? card : 1f;
            float k = still ? 1f : Mathf.LerpUnclamped(PopFrom, 1f, Ease.OutBack(enter, 1.6f));
            float rise = still ? 1f : Ease.OutBack(Mathf.Clamp01((ms - RosaSurfaceStartMs) / RosaSurfaceMs), 1.2f);
            float dive = 0f;
            if (_dismissing)
            {
                // Primero Rosa se sumerge con la tarjeta entera; después, solo en los últimos FadeTailMs, la tarjeta se funde y
                // se encoge (con Reducir movimiento, solo el fundido).
                float exitMs = still ? ExitReducedMs : ExitMs;
                float fade = still ? Mathf.Clamp01(_exitMs / exitMs) : Mathf.Clamp01((_exitMs - (exitMs - FadeTailMs)) / FadeTailMs);
                veil = _fadeFrom * (1f - Ease.In(fade));
                card = _cardFrom;
                k = still ? _scaleFrom : _scaleFrom * Mathf.Lerp(1f, ExitScale, Ease.Out(fade));
                dive = still ? 0f : Ease.In(Mathf.Clamp01(_exitMs / (DiveEnd * exitMs)));
            }

            _group.alpha = veil;
            _popGroup.alpha = card;
            _pop.localScale = new Vector3(k, k, 1f);
            if (_travel != null)
            {
                // El viaje (M3.6): completado, en su estado final; si no, en el instante ms (también con el respaldo).
                _strip.Apply(_travelSkipped ? float.MaxValue : ms, _reduceMotion);
            }

            if (_fallback != null)
            {
                return; // sin Rosa ni Capi
            }

            // Rosa: asoma desde bajo el agua y se queda quieta; al cerrar, se sumerge.
            _rosaRise = Mathf.Clamp01(rise) * (1f - dive);
            float waterline = _sceneH * RosaWaterline;
            float hidden = ProceduralArt.RosaHeightDp * (1f - RosaAbove);
            float below = ProceduralArt.RosaHeightDp + 4f;
            float pivotBelow = Mathf.LerpUnclamped(below, hidden, rise) + ((below - hidden) * dive);
            _rosa.rectTransform.anchoredPosition = new Vector2((_sceneW * RosaX) - (DiveShiftDp * dive), -(waterline + pivotBelow));

            // Al sumergirse gira hacia delante (antihorario: mira a la izquierda), así que el pico entra primero en el agua y el
            // lomo, lo último; el hundimiento pesa más que el giro, así que la cola cortada del sprite nunca asoma.
            _rosa.rectTransform.localEulerAngles = new Vector3(0f, 0f, dive * DiveTiltDeg);

            // Espuma sobre el corte del lomo con el agua, con Rosa fuera; se apaga al sumergirse.
            _foam.color = new Color(1f, 1f, 1f, Mathf.Clamp01((_rosaRise - 0.55f) / 0.45f));

            // Ondas en el agua al asomar y al sumergirse.
            float ring = 0f;
            float ringScale = 1f;
            if (!still)
            {
                float t0 = Mathf.Clamp01((ms - RosaSurfaceStartMs) / RippleMs);
                if (t0 > 0f && t0 < 1f && !_dismissing)
                {
                    ring = 0.9f * (1f - t0);
                    ringScale = 0.55f + (0.75f * Ease.Out(t0));
                }
                else if (_dismissing)
                {
                    float t1 = Mathf.Clamp01(_exitMs / (ExitMs - (0.5f * FadeTailMs)));
                    ring = 0.9f * Mathf.Sin(t1 * Mathf.PI);
                    ringScale = 0.6f + (0.6f * t1);
                }
            }

            _ripple.color = new Color(1f, 1f, 1f, ring);
            _ripple.rectTransform.localScale = new Vector3(ringScale, ringScale, 1f);

            // Soplo de la exhalación: el abanico se abre desde el respiradero, pegado a lo alto de la cabeza.
            float sprayT = still || _dismissing ? 1f : Mathf.Clamp01((ms - SprayStartMs) / SprayMs);
            float sprayA = sprayT <= 0f || sprayT >= 1f ? 0f : Mathf.Sin(sprayT * Mathf.PI) * 0.95f;
            Vector2 blow = ProceduralArt.RosaBlowhole;
            float bx = (_sceneW * RosaX) + ((blow.x - 0.5f) * ProceduralArt.RosaWidthDp);
            float by = waterline + pivotBelow - (blow.y * ProceduralArt.RosaHeightDp);
            _spray.rectTransform.anchoredPosition = new Vector2(bx, -by);
            float sprayK = 0.45f + (0.65f * Ease.Out(sprayT));
            _spray.rectTransform.localScale = new Vector3(sprayK, sprayK, 1f);
            _spray.color = new Color(1f, 1f, 1f, sprayA);

            // Capi: la pata sube del costado a presentar (el cuerpo, quieto, va dibujado en la viñeta).
            float present = still ? 1f : Ease.OutBack(Mathf.Clamp01((ms - CapiStartMs) / CapiPresentMs), 1.4f);
            float angle = Mathf.LerpUnclamped(PawDownDeg, PawUpDeg, present);
            _paw.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        // Las medidas de la tarjeta del mundo world en una pantalla de canvasW dp con la escala de ahora (las mismas para Show y
        // para Prefetch, así que la petición del arte es la misma): coloca la cabecera «Mundo {n}: {nombre}» (la clave hasta el
        // primer « · ») si cabe a 24 sp por la escala o, si no, «Mundo {n}» con el nombre abriendo la línea del lugar en negrita
        // (así el nombre del mundo nunca pesa menos que su número); parte el lugar en 1 o 2 líneas; y devuelve el ancho de la
        // tarjeta, el interior, el alto de la cabecera (crece con su título, 02 §12: «los contenedores crecen»), el de la viñeta,
        // el de la línea del lugar y el de la tarjeta.
        // Con el viaje (travel, M3.6), además: la pregunta del mundo (questionH; 0 sin texto), la franja de StripHeightDp bajo ella
        // y «Ver mapa» bajo el lugar (buttonH), y la tarjeta crece con ellos (la viñeta solo llega al alto mínimo si aún falta).
        private void Measure(int world, float canvasW, BannerTravel? travel, out float cardW, out float inner, out float headerH, out float questionH,
            out float sceneH, out float placeH, out float buttonH, out float cardH)
        {
            float s = _fontScale;
            cardW = Mathf.Min(LevelCardView.MaxWidthDp, canvasW - (2f * LevelCardView.SideMarginDp));
            inner = cardW - (2f * PadDp);
            _title.fontSize = TitleSp * s;
            string heading = UiText.WorldHeading(world);
            _nameInTitle = WidthOf(_title, heading) <= inner - (2f * TitleInsetDp) - FitSlackDp;
            SetText(_title, _nameInTitle ? heading : UiText.WorldTitle(world));
            _placeText = _nameInTitle ? UiText.WorldPlace(world) : UiText.WorldPlaceNamed(world);

            // Con la escala, la placa de la cabecera crece con ella y, como en la tarjeta previa, al menos hasta el alto de línea de
            // su título (Baloo 2: ≈ 1,6 veces el tamaño); a 100 % se queda en sus 40 dp de siempre.
            headerH = HeaderDp;
            if (s > 1f)
            {
                float titleH = Mathf.Ceil(_title.GetPreferredValues(_title.text, float.PositiveInfinity, float.PositiveInfinity).y);
                headerH = Mathf.Max(HeaderDp * s, titleH + 2f);
            }

            // El lugar, centrado bajo la viñeta, en 1 línea o en 2 cortadas en uno de sus {sep}; la viñeta crece hasta el alto
            // mínimo. Con «Mundo {n}» en la cabecera, el nombre abre la línea en negrita (la negrita va en su plantilla).
            FitPlace(world, !_nameInTitle, inner - FitSlackDp, s);
            placeH = Mathf.Ceil(_place.GetPreferredValues(_place.text, inner, float.PositiveInfinity).y) + 2f;
            float sceneY = PadDp + headerH + 8f;
            questionH = 0f;
            buttonH = 0f;
            if (travel != null)
            {
                questionH = FitQuestion(travel.QuestionText, inner, s);
                sceneY += (questionH > 0f ? questionH + QuestionGapDp : 0f) + StripHeightDp + StripGapDp;
                _mapLabel.fontSize = MapButtonSp * s;
                _mapLabel.fontSizeMax = MapButtonSp * s;
                float labelW = WidthOf(_mapLabel, _mapLabel.text);
                _mapButtonW = Mathf.Min(inner, Mathf.Max(MapButtonMinWidthDp, Mathf.Ceil(labelW + (2f * MapButtonPadDp))));
                buttonH = Mathf.Max(MinTouchDp, _art.CtaWood.HeightDp);
            }

            float tail = 10f + placeH + (buttonH > 0f ? MapButtonGapDp + buttonH : 0f) + PadDp + 4f;
            float rest = SceneDp + tail;
            sceneH = SceneDp + Mathf.Max(0f, MinHeightDp - (sceneY + rest));
            cardH = Mathf.Ceil(sceneY + sceneH + tail);
        }

        // La pregunta del mundo (map_world_question) sobre la franja, centrada, en 1 o 2 líneas de 15 sp por la escala, o más
        // pequeña hasta 12 si no cabe (02 §12): devuelve su alto, o 0 sin texto. Asigna (TMP), pero solo al colocar el banner.
        private float FitQuestion(string text, float inner, float scale)
        {
            if (text.Length == 0)
            {
                SetText(_question, string.Empty);
                return 0f;
            }

            SetText(_question, text);
            _question.enableAutoSizing = false;
            float sp = QuestionSp * scale;
            float min = Mathf.Max(UiBuilder.MinTextSp, UiBuilder.MinTextSp * scale);
            for (; sp > min; sp -= 1f)
            {
                _question.fontSize = sp;
                float oneLine = _question.GetPreferredValues("Á", float.PositiveInfinity, float.PositiveInfinity).y;
                if (_question.GetPreferredValues(text, inner, float.PositiveInfinity).y <= (2f * oneLine) + 1f)
                {
                    break; // 2 líneas como mucho
                }
            }

            _question.fontSize = Mathf.Max(sp, min);
            return Mathf.Ceil(_question.GetPreferredValues(text, inner, float.PositiveInfinity).y) + 2f;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }

        // La línea del lugar del mundo world (con el nombre delante si named) en 1 línea o, si no cabe, en 2 cortadas en el {sep}
        // más equilibrado de su plantilla (nunca dentro de un nombre), de 18 a 15 sp por la escala scale. Asigna (cadenas nuevas),
        // pero solo al colocar el banner.
        private void FitPlace(int world, bool named, float room, float scale)
        {
            string line = UiText.WorldPlaceLine(world, named, 0);
            for (float sp = PlaceSp; sp >= PlaceMinSp; sp -= 1f)
            {
                _place.fontSize = sp * scale;
                if (WidthOf(_place, line) <= room)
                {
                    SetText(_place, line);
                    return;
                }

                string? split = BestSplit(world, named, room);
                if (split != null)
                {
                    SetText(_place, split);
                    return;
                }
            }

            // Ni a 15 sp caben las dos mitades: el corte más equilibrado, y TMP parte lo que sobre.
            SetText(_place, BestSplit(world, named, float.PositiveInfinity) ?? line);
        }

        // De los {sep} de la línea del lugar, el que da la línea más larga más corta (y que quepa en room), con el salto de línea
        // en él y « · » en los demás (UiText.WorldPlaceLine); o null. Cada línea se mide por separado, como siempre: el texto
        // que se pinta es el de la plantilla, sin tocar.
        private string? BestSplit(int world, bool named, float room)
        {
            string? best = null;
            float bestW = float.MaxValue;
            int cuts = UiText.WorldPlaceSeparators(named);
            for (int k = 0; k < cuts; k++)
            {
                string split = UiText.WorldPlaceLine(world, named, 1 << k);
                int lineBreak = split.IndexOf('\n');
                float w = Mathf.Max(WidthOf(_place, split.Substring(0, lineBreak)), WidthOf(_place, split.Substring(lineBreak + 1)));
                if (w <= room && w < bestW)
                {
                    bestW = w;
                    best = split;
                }
            }

            return best;
        }

        // El toque, también mientras sale (GameScreen termina la salida en el acto con el segundo); mientras espera el arte, nada.
        private void OnTap()
        {
            if (Visible && CardShown)
            {
                _onTap();
            }
        }

        // «Ver mapa» (M3.6): solo con la tarjeta a la vista y sin salir; quien abre el Mapa (GameScreen) completa antes el viaje.
        private void OnMapPressed()
        {
            if (Visible && CardShown && !_dismissing && _travel != null)
            {
                _onMap?.Invoke();
            }
        }

        // «Ver mapa» a la vista y recibiendo toques, o fuera (inactivo y sin recibir toques: sin viaje, el velo es lo único que los
        // recibe, como siempre).
        private void ShowMapButton(bool on)
        {
            GameObject go = _mapButton.Button.gameObject;
            SetActive(go, on);
            Graphic? hit = _mapButton.Button.targetGraphic;
            if (hit != null && hit.raycastTarget != on)
            {
                hit.raycastTarget = on;
            }
        }

        private static void Center(RectTransform rt, float x, float yFromTop, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -yFromTop);
        }

        private static void SetText(TextMeshProUGUI t, string text)
        {
            if (!string.Equals(t.text, text, StringComparison.Ordinal))
            {
                t.text = text;
            }
        }

        // Ancho de text en una sola línea, al tamaño actual de t.
        private static float WidthOf(TextMeshProUGUI t, string text)
        {
            return t.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
        }
    }

    /// <summary>
    /// Viaje de mundo dentro del banner (D-013, punto 7; Q4-D; 02 §6, fila Banner de mundo; propuesta del Mapa §4.2, §5.3 y §6.1;
    /// tarea M3.6), ya resuelto contra los datos del Mapa: el tramo (<c>leg</c>) de la gran parada del mundo que abre el nivel,
    /// desde la parada anterior, con su largo por agua, sus puntos de paso, la bandera que iza la canoa al llegar
    /// (<c>flag_raise</c>), la pregunta del mundo (<c>map_world_question</c>), el guía de cada lado (<see cref="RiverJourney.GuideFor"/>)
    /// y, si cambia, su relevo en el punto de paso <see cref="RelayPassingId"/> (P-M8 de D-023), y el agua que ya llena el Mapa
    /// (<see cref="RiverMapScreen.ComputeFill"/> con el tramo entero). Inmutable; se construye al pedir o abrir el banner, nunca
    /// por fotograma (<see cref="For"/>), y <see cref="PointAt"/> no asigna memoria.
    /// </summary>
    internal sealed class BannerTravel
    {
        /// <summary>Punto de paso del relevo del guía (P-M8: los rápidos de Teotônio; propuesta del Mapa §6.1, Q18-A).</summary>
        public const string RelayPassingId = "teotonio";

        private readonly float[] _pieceStart;
        private readonly float[] _pieceLength;
        private readonly string[] _passingIds;
        private readonly string[] _passingNames;
        private readonly float[] _passingFractions;
        private readonly float[] _fillFrom;

        private BannerTravel(int level, int world, RiverMap map, RiverStopRoute route, RiverStop stop, RiverStop from, float[] pieceStart,
            float[] pieceLength, float length, string questionText, float relay, string[] passingIds, string[] passingNames, float[] passingFractions,
            float[] fillFrom)
        {
            Level = level;
            World = world;
            Map = map;
            Route = route;
            Stop = stop;
            From = from;
            _pieceStart = pieceStart;
            _pieceLength = pieceLength;
            LengthUnits = length;
            QuestionText = questionText;
            GuideFrom = RiverJourney.GuideFor(from.World);
            GuideTo = RiverJourney.GuideFor(stop.World);
            RelayFraction = string.Equals(GuideFrom, GuideTo, StringComparison.Ordinal) ? -1f : relay;
            _passingIds = passingIds;
            _passingNames = passingNames;
            _passingFractions = passingFractions;
            _fillFrom = fillFrom;
        }

        /// <summary>Nivel que abre el banner (el primero del mundo).</summary>
        public int Level { get; }

        /// <summary>Mundo del banner (<see cref="WorldBanners.WorldAt"/>).</summary>
        public int World { get; }

        /// <summary>Geometría del Mapa.</summary>
        public RiverMap Map { get; }

        /// <summary>Ruta de la gran parada (su <c>leg</c>).</summary>
        public RiverStopRoute Route { get; }

        /// <summary>Gran parada a la que llega la canoa (la del nivel).</summary>
        public RiverStop Stop { get; }

        /// <summary>Parada de salida (la anterior).</summary>
        public RiverStop From { get; }

        /// <summary>Largo del tramo por agua (unidades del marco, W = 1).</summary>
        public float LengthUnits { get; }

        /// <summary>Bandera que se iza al llegar (ISO2 de <c>flag_raise</c>), o null si no cambia de país.</summary>
        public string? Flag => Stop.FlagRaise;

        /// <summary>«{mundo} · {pregunta}» (<c>map_world_question</c>), o "" si el mundo no tiene pregunta o su texto no se muestra.</summary>
        public string QuestionText { get; }

        /// <summary>Guía de la parada de salida (<see cref="RiverJourney.GuideRosa"/> o <see cref="RiverJourney.GuideBufeo"/>).</summary>
        public string GuideFrom { get; }

        /// <summary>Guía de la gran parada.</summary>
        public string GuideTo { get; }

        /// <summary>
        /// Fracción del tramo (0-1, por largo de arco) en la que se relevan los guías: la del punto de paso
        /// <see cref="RelayPassingId"/> si el tramo lo pasa; 0 (al salir) si cambia el guía y el tramo no lo pasa [P]; −1 si el guía
        /// no cambia.
        /// </summary>
        public float RelayFraction { get; }

        /// <summary>Hay relevo del guía en este tramo.</summary>
        public bool HasRelay => RelayFraction >= 0f;

        /// <summary>Puntos de paso del tramo con nombre que mostrar, en orden.</summary>
        public int PassingCount => _passingIds.Length;

        /// <summary>
        /// Lo que ya está lleno del Mapa al abrir el banner, por línea (la s desde la que va llena hasta su boca, o
        /// <see cref="RiverMapScreen.NoFill"/>): el lleno de <c>max_level</c> = nivel − 1 con el tramo de la gran parada entero.
        /// No lo modifiques.
        /// </summary>
        public float[] FillFrom => _fillFrom;

        /// <summary>
        /// Viaje del banner del nivel <paramref name="level"/> con estos datos, o null (el banner de siempre): sin datos, si el
        /// nivel no abre un mundo con banner desde el 2, si su parada no existe o es la primera, o si su tramo no tiene largo. No
        /// mira <c>ui_map_autopen</c> ni el 11 (<see cref="MapMoments.TravelInBanner"/>, que aplica quien lo pide). Asigna (los
        /// textos y las tablas), así que solo al pedir o abrir el banner.
        /// </summary>
        public static BannerTravel? For(int level, RiverMap? map, RiverRoutes? routes, RiverStopCatalog? stops)
        {
            int world = WorldBanners.WorldAt(level);
            if (world == 0 || map == null || routes == null || stops == null)
            {
                return null;
            }

            RiverStop? stop = stops.StopOf(level);
            RiverStopRoute? route = stop != null && stop.N >= 1 ? routes.At(stop.N) : null;
            RiverStop? from = stop != null ? stops.At(stop.N - 1) : null;
            if (stop == null || route == null || from == null)
            {
                return null;
            }

            IReadOnlyList<RiverPathPiece> path = route.Path;
            var start = new float[path.Count];
            var length = new float[path.Count];
            float total = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                RiverPathPiece piece = path[i];
                start[i] = total;
                length[i] = Mathf.Abs(piece.S1 - piece.S0) * map.Lines[piece.Line].Length;
                total += length[i];
            }

            if (total <= 0f)
            {
                return null; // sin largo, nada que recorrer
            }

            // La pregunta del mundo, solo con texto (sin el visto bueno del asesor, en release es "": sin pregunta).
            string question = stop.WorldQuestion != null ? Loc.Fact(stop.WorldQuestion) : string.Empty;
            string questionText = question.Length > 0 ? UiText.MapWorldQuestion(world, question) : string.Empty;

            // Los puntos de paso del tramo, por su posición en él; el de Teotônio, además, para el relevo.
            float relay = 0f;
            var ids = new List<string>();
            var names = new List<string>();
            var fractions = new List<float>();
            IReadOnlyList<string> via = route.Via;
            for (int v = 0; v < via.Count; v++)
            {
                RiverPassingPoint? point = map.PassingPoint(via[v]);
                if (point == null || !TryFraction(path, start, length, total, point.At, out float fraction))
                {
                    continue;
                }

                if (string.Equals(point.Id, RelayPassingId, StringComparison.Ordinal))
                {
                    relay = fraction;
                }

                string name = Loc.Fact(point.NameKey);
                if (name.Length > 0)
                {
                    ids.Add(point.Id);
                    names.Add(name);
                    fractions.Add(fraction);
                }
            }

            // El Mapa ya lleno hasta el nivel anterior (la llegada a la gran parada fue al ganarlo), con el tramo entero.
            var fill = new float[map.Lines.Count];
            RiverMapScreen.ComputeFill(map, routes, Mathf.Max(0, level - 1), fill);
            for (int i = 0; i < path.Count; i++)
            {
                RiverPathPiece piece = path[i];
                if (piece.S0 != piece.S1)
                {
                    fill[piece.Line] = Mathf.Min(fill[piece.Line], Mathf.Clamp01(Mathf.Min(piece.S0, piece.S1)));
                }
            }

            return new BannerTravel(level, world, map, route, stop, from, start, length, total, questionText, relay, ids.ToArray(), names.ToArray(),
                fractions.ToArray(), fill);
        }

        /// <summary>Id del punto de paso <paramref name="i"/>.</summary>
        public string PassingId(int i)
        {
            return _passingIds[i];
        }

        /// <summary>Nombre (ya en el idioma) del punto de paso <paramref name="i"/>.</summary>
        public string PassingName(int i)
        {
            return _passingNames[i];
        }

        /// <summary>Fracción del tramo en la que está el punto de paso <paramref name="i"/>.</summary>
        public float PassingFraction(int i)
        {
            return _passingFractions[i];
        }

        /// <summary>Guía que va delante en la fracción <paramref name="fraction"/> del tramo: el de salida antes del relevo.</summary>
        public string GuideAt(float fraction)
        {
            return HasRelay && fraction < RelayFraction ? GuideFrom : GuideTo;
        }

        /// <summary>
        /// Punto del tramo a la fracción <paramref name="fraction"/> (0-1, por largo de arco), en unidades del marco, por sus
        /// piezas en orden (también las que remontan). Sin asignaciones.
        /// </summary>
        public Vector2 PointAt(float fraction)
        {
            IReadOnlyList<RiverPathPiece> path = Route.Path;
            float target = Mathf.Clamp01(fraction) * LengthUnits;
            for (int i = 0; i < path.Count; i++)
            {
                float len = _pieceLength[i];
                if (target <= _pieceStart[i] + len || i == path.Count - 1)
                {
                    RiverPathPiece piece = path[i];
                    float t = len > 0f ? Mathf.Clamp01((target - _pieceStart[i]) / len) : 1f;
                    Map.Lines[piece.Line].PointAt(piece.S0 + ((piece.S1 - piece.S0) * t), out float x, out float y);
                    return new Vector2(x, y);
                }
            }

            return RiverMapScreen.PlacePoint(Map, Route.At);
        }

        // Fracción del tramo en la que está el punto place: en la primera pieza de su línea que lo cubre (con un margen mínimo
        // por el redondeo de los datos).
        private static bool TryFraction(IReadOnlyList<RiverPathPiece> path, float[] start, float[] length, float total, RiverMapPlace place, out float fraction)
        {
            const float slack = 1e-4f;
            for (int i = 0; i < path.Count; i++)
            {
                RiverPathPiece piece = path[i];
                float lo = Mathf.Min(piece.S0, piece.S1);
                float hi = Mathf.Max(piece.S0, piece.S1);
                if (piece.Line != place.Line || place.S < lo - slack || place.S > hi + slack || hi <= lo)
                {
                    continue;
                }

                float t = Mathf.Clamp01(Mathf.Abs(place.S - piece.S0) / (hi - lo));
                fraction = (start[i] + (t * length[i])) / total;
                return true;
            }

            fraction = 0f;
            return false;
        }
    }

    /// <summary>
    /// Franja del viaje de mundo dentro del banner (M3.6; D-013, punto 7: «la canoa cruza por agua real en una franja del
    /// Mapa»): un trozo del Mapa de <see cref="WorldBannerView.StripWidthDp"/> × <see cref="WorldBannerView.StripHeightDp"/> dp
    /// [P] con el norte arriba (el contenido del Mapa girado 90°, como «Norte arriba»: los Andes a la izquierda y el mar a la
    /// derecha), encuadrado en el tramo de la gran parada, con la tierra, el vidrio de los ríos y el agua que ya llena el Mapa
    /// (las mismas cintas que la pantalla del Mapa, <see cref="RiverRibbonGraphic"/> y <see cref="LandGraphic"/>, en la vista
    /// Mundo). Encima, sin girar: la canoa de Capi (<see cref="ProceduralArt.MapCanoe"/>) que recorre el tramo, el guía que nada
    /// delante (Rosa; el bufeo boliviano, el mismo sprite con otra paleta [P], en los mundos 7 y 8) con su relevo, los nombres
    /// de paso que salen sin pararse y la bandera que se iza en la popa al llegar (<see cref="ProceduralArt.MapFlag"/>).
    /// </summary>
    /// <remarks>
    /// Va en un lienzo anidado propio (guía 8, docs/art/direccion-visual-v2.md: lo que se mueve, en Canvas propios), recortado por
    /// un <see cref="RectMask2D"/> con las esquinas de la viñeta. Las mallas se construyen solo al abrir un banner con viaje
    /// (<see cref="Build"/>), con una cinta por línea del Mapa reutilizada (las que no entran en la franja, inactivas);
    /// <see cref="Apply"/> solo mueve y funde, sin asignaciones.
    /// </remarks>
    internal sealed class BannerTravelStrip
    {
        /// <summary>Margen horizontal del encuadre (dp) [P]: la media canoa y algo de río.</summary>
        public const float PadXDp = 28f;

        /// <summary>Margen vertical del encuadre (dp) [P]: el mástil con la bandera sobre la canoa.</summary>
        public const float PadYDp = 30f;

        /// <summary>Ancho del guía (dp) [P]; el alto sigue el sprite de Rosa.</summary>
        public const float GuideWidthDp = 22f;

        /// <summary>El guía nada este tramo por delante de la canoa (dp) [P].</summary>
        public const float GuideLeadDp = 26f;

        /// <summary>Al llegar, el guía espera junto a la gran parada, este tanto más arriba que la canoa (dp) [P].</summary>
        public const float GuideRestUpDp = 14f;

        /// <summary>El relevo: el guía nuevo aparece y el anterior se queda mientras la canoa recorre este tramo (dp) [P].</summary>
        public const float RelayRampDp = 24f;

        /// <summary>Tamaño de los nombres de paso (sp): el mínimo de 02 §12.</summary>
        public const float PassingSp = 12f;

        /// <summary>Un nombre de paso se ve mientras la canoa está a menos de este tramo de su punto (dp) [P].</summary>
        public const float PassingWindowDp = 36f;

        /// <summary>Altura del centro del nombre de paso sobre su punto (dp) [P]: por encima de la canoa, que llega a 21 dp.</summary>
        public const float PassingUpDp = 30f;

        /// <summary>Alto del mástil de popa (dp) [P].</summary>
        public const float MastHeightDp = 17f;

        /// <summary>Paleta del bufeo boliviano sobre el sprite de Rosa (multiplica su color) [P, pendiente del asesor cultural].</summary>
        public static readonly Color BufeoTint = new Color(0.80f, 0.86f, 0.97f, 1f);

        private const float MastWidthDp = 1.6f;
        private const float SternFraction = 0.32f;
        private const float EdgeDp = 2f;
        private const float VisibleMarginDp = 12f;
        private const int FrameSamples = 64;

        private readonly ProceduralArt _art;
        private readonly Image _edge;
        private readonly RectTransform _mask;
        private readonly CanvasGroup _fade;
        private readonly Canvas _canvas;
        private readonly RectTransform _layer;
        private readonly RectTransform _content;
        private readonly LandGraphic _land;
        private readonly RectTransform _glassLayer;
        private readonly RectTransform _casingLayer;
        private readonly RectTransform _coreLayer;
        private readonly RectTransform _overlay;
        private readonly Image _guideA;
        private readonly Image _guideB;
        private readonly Image _mast;
        private readonly Image _flag;
        private readonly Image _canoe;
        private readonly RiverMapScreen.FrameCornersGraphic _corners;
        private readonly List<RiverRibbonGraphic> _glass = new List<RiverRibbonGraphic>();
        private readonly List<RiverRibbonGraphic> _casing = new List<RiverRibbonGraphic>();
        private readonly List<RiverRibbonGraphic> _core = new List<RiverRibbonGraphic>();
        private readonly List<TextMeshProUGUI> _passing = new List<TextMeshProUGUI>();
        private float[] _passingDp = Array.Empty<float>();
        private BannerTravel? _built;
        private float _w;
        private float _h;
        private float _dpu;
        private Vector2 _center;
        private float _totalDp;
        private float _relayDp = -1f;
        private Sprite? _guideSprite;
        private Sprite? _flagSprite;
        private float _fraction;
        private float _flagRaise;
        private float _guideAAlpha;
        private float _guideBAlpha;
        private int _canoeFacing = -1;
        private double _buildMs;

        /// <summary>Crea la franja, oculta, en el cuerpo <paramref name="panel"/> del banner. Ninguna malla hasta <see cref="Build"/>.</summary>
        public BannerTravelStrip(RectTransform panel, ProceduralArt art)
        {
            _art = art;
            _edge = UiBuilder.Sliced("TravelEdge", panel, art.RoundedRect, Palette.FamilyWood.Outline);
            _edge.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / (ProceduralArt.BannerSceneRadiusDp + EdgeDp);
            _mask = UiBuilder.Rect("Travel", panel);
            _mask.gameObject.AddComponent<RectMask2D>();
            _fade = _mask.gameObject.AddComponent<CanvasGroup>();
            _fade.blocksRaycasts = false;
            _fade.interactable = false;

            var go = new GameObject("TravelCanvas", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(_mask, false);
            _layer = (RectTransform)go.transform;
            Stretch(_layer);
            _canvas = go.GetComponent<Canvas>();
            _canvas.overrideSorting = false; // sin orden propio: el RectMask2D de la franja lo recorta
            _canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

            Image ground = UiBuilder.Image("Ground", _layer, null, RiverMapScreen.GroundColor);
            Stretch(ground.rectTransform);

            // El Mapa, girado 90° (norte arriba), con el origen de MapSpace en su centro.
            _content = UiBuilder.Rect("Map", _layer);
            CenterRect(_content);
            _content.localEulerAngles = new Vector3(0f, 0f, 90f);
            var landGo = new GameObject("Land", typeof(RectTransform), typeof(CanvasRenderer), typeof(LandGraphic));
            landGo.transform.SetParent(_content, false);
            Stretch((RectTransform)landGo.transform);
            _land = landGo.GetComponent<LandGraphic>();
            _land.Setup();
            _glassLayer = Layer("Glass", _content);
            _casingLayer = Layer("FillCasing", _content);
            _coreLayer = Layer("Fill", _content);

            // Encima, sin girar: los nombres de paso, los guías, la popa con el mástil y la bandera, y la canoa.
            _overlay = UiBuilder.Rect("Overlay", _layer);
            CenterRect(_overlay);
            _guideA = Piece("GuideA", null, new Vector2(0.5f, 0.3f));
            _guideB = Piece("GuideB", null, new Vector2(0.5f, 0.3f));
            _mast = Piece("Mast", null, new Vector2(0.5f, 0f));
            _mast.color = Palette.FamilyWood.Deep;
            _flag = Piece("Flag", null, new Vector2(0f, 0.5f));
            _canoe = Piece("Canoe", null, new Vector2(0.5f, RiverMapScreen.CanoePivotY));
            _canoe.preserveAspect = true;

            _corners = RiverMapScreen.FrameCornersGraphic.Create("TravelCorners", panel, ProceduralArt.BannerSceneRadiusDp, Palette.FamilyWood.Outline);
            Hide();
        }

        /// <summary>Recorte de la franja (tests: medidas y solapes).</summary>
        public RectTransform Root => _mask;

        /// <summary>El Mapa girado (tests: el punto del Mapa bajo la canoa).</summary>
        public RectTransform Content => _content;

        /// <summary>Capa sin girar de la canoa, los guías, la bandera y los nombres.</summary>
        public RectTransform Overlay => _overlay;

        /// <summary>Lienzo anidado de la franja.</summary>
        public Canvas Canvas => _canvas;

        /// <summary>La franja se ve.</summary>
        public bool Visible => _mask.gameObject.activeSelf;

        /// <summary>Viaje que pinta, o null.</summary>
        public BannerTravel? Built => Visible ? _built : null;

        /// <summary>dp por unidad del marco del encuadre.</summary>
        public float DpPerUnit => _dpu;

        /// <summary>Punto del marco en el centro de la franja (unidades del marco; el origen local del Mapa girado).</summary>
        public Vector2 MapCenter => _center;

        /// <summary>Largo del tramo en la franja (dp).</summary>
        public float TotalDp => _totalDp;

        /// <summary>Lo que costó <see cref="Build"/> en el hilo principal (ms; sin la malla, que se hace al pintar el lienzo).</summary>
        public double BuildMs => _buildMs;

        /// <summary>La canoa de Capi.</summary>
        public Image Canoe => _canoe;

        /// <summary>Fracción del tramo en que va la canoa (0 al salir, 1 en la gran parada).</summary>
        public float CanoeFraction => _fraction;

        /// <summary>La canoa mira a la derecha (1) o a la izquierda (−1).</summary>
        public int CanoeFacing => _canoeFacing;

        /// <summary>Bandera de la popa (sin sprite si no hay bandera que izar o en release sin el PNG oficial).</summary>
        public Image Flag => _flag;

        /// <summary>Mástil de la popa.</summary>
        public Image Mast => _mast;

        /// <summary>Sprite de la bandera (null: sin bandera).</summary>
        public Sprite? FlagSprite => Visible ? _flagSprite : null;

        /// <summary>Cuánto se ha izado la bandera (0-1).</summary>
        public float FlagRaise => _flagRaise;

        /// <summary>Guía de la salida (antes del relevo; sin relevo, el único).</summary>
        public Image GuideA => _guideA;

        /// <summary>Guía de la llegada tras el relevo (inactivo sin relevo).</summary>
        public Image GuideB => _guideB;

        /// <summary>Opacidad del guía de la salida (0 al terminar el relevo).</summary>
        public float GuideAAlpha => _guideAAlpha;

        /// <summary>Opacidad del guía de la llegada (1 al terminar el relevo).</summary>
        public float GuideBAlpha => _guideBAlpha;

        /// <summary>Cintas de vidrio activas (las líneas que entran en la franja).</summary>
        public int GlassCount => ActiveCount(_glass);

        /// <summary>Cintas de agua activas (las líneas llenas que entran en la franja).</summary>
        public int WaterCount => ActiveCount(_core);

        /// <summary>Vértices de la tierra en la última malla.</summary>
        public int LandVertexCount => _land.LastVertexCount;

        /// <summary>Nombres de paso del viaje.</summary>
        public int PassingCount => _built != null ? _built.PassingCount : 0;

        /// <summary>Nombre de paso <paramref name="i"/>.</summary>
        public TextMeshProUGUI Passing(int i)
        {
            return _passing[i];
        }

        /// <summary>Opacidad del nombre de paso <paramref name="i"/>.</summary>
        public float PassingAlpha(int i)
        {
            return _passing[i].canvasRenderer.GetAlpha();
        }

        /// <summary>Punto del Mapa <paramref name="map"/> (unidades del marco) en la capa sin girar (dp, origen en el centro). Sin asignaciones.</summary>
        public Vector2 StripPoint(Vector2 map)
        {
            return new Vector2((map.y - _center.y) * _dpu, (map.x - _center.x) * _dpu);
        }

        /// <summary>Coloca la franja en el cuerpo del banner (dp desde su esquina de arriba a la izquierda).</summary>
        public void Place(float x, float y, float w, float h)
        {
            UiBuilder.Place(_edge.rectTransform, x - EdgeDp, y - EdgeDp, w + (2f * EdgeDp), h + (2f * EdgeDp));
            UiBuilder.Place(_mask, x, y, w, h);
            UiBuilder.Place(_corners.rectTransform, x, y, w, h);
            _content.sizeDelta = new Vector2(h, w); // girado: su ancho es el alto de la franja
            _overlay.sizeDelta = new Vector2(w, h);
            if (w != _w || h != _h)
            {
                _w = w;
                _h = h;
                _built = null; // otro encuadre
            }
        }

        /// <summary>
        /// Construye el viaje <paramref name="travel"/> en la franja colocada (si cambió): el encuadre del tramo, la tierra, el
        /// vidrio de las líneas que entran, el agua ya llena, los nombres de paso y la bandera. Asigna lo justo la primera vez;
        /// después reutiliza las cintas de cada línea.
        /// </summary>
        public void Build(BannerTravel travel, bool reduceMotion)
        {
            SetActive(_edge.gameObject, true);
            SetActive(_mask.gameObject, true);
            SetActive(_corners.gameObject, true);
            if (ReferenceEquals(travel, _built))
            {
                return;
            }

            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            _built = travel;
            RiverMap map = travel.Map;
            Frame(travel);
            var space = new MapSpace(_dpu, new Vector2(-_center.x * _dpu, _center.y * _dpu));
            _land.SetLand(map.Land, space);

            IReadOnlyList<RiverLine> lines = map.Lines;
            while (_glass.Count < lines.Count)
            {
                _glass.Add(NewRibbon("Glass_" + _glass.Count, _glassLayer, RibbonPart.Whole));
                _casing.Add(NewRibbon("FillCasing_" + _casing.Count, _casingLayer, RibbonPart.Casing));
                _core.Add(NewRibbon("Fill_" + _core.Count, _coreLayer, RibbonPart.Core));
            }

            float[] fill = travel.FillFrom;
            for (int i = 0; i < _glass.Count; i++)
            {
                RiverLine? line = i < lines.Count ? lines[i] : null;
                bool visible = line != null && LineInView(line);
                bool filled = visible && fill[i] < RiverMapScreen.NoFill;
                SetupRibbon(_glass[i], visible ? line : null, RibbonLook.Glass, 0f, 1f, space);
                SetupRibbon(_casing[i], filled ? line : null, RibbonLook.Water, filled ? fill[i] : 1f, 1f, space);
                SetupRibbon(_core[i], filled ? line : null, RibbonLook.Water, filled ? fill[i] : 1f, 1f, space);
                if (visible)
                {
                    _glass[i].SetGlassBodyAlpha(RiverMapScreen.MapGlassBodyAlpha);
                }

                if (filled && line!.Kind == RiverLineKind.Main)
                {
                    _core[i].transform.SetAsLastSibling(); // el cauce encima: las bocas quedan debajo del río grande
                }
            }

            _totalDp = travel.LengthUnits * _dpu;
            _relayDp = travel.HasRelay ? travel.RelayFraction * _totalDp : -1f;
            BuildPassing(travel);

            _canoe.sprite = _art.MapCanoe;
            _canoe.rectTransform.sizeDelta = new Vector2(ProceduralArt.MapCanoeWidthDp, ProceduralArt.MapCanoeHeightDp);
            float guideH = GuideWidthDp * ProceduralArt.RosaHeightDp / ProceduralArt.RosaWidthDp;
            _guideA.rectTransform.sizeDelta = new Vector2(GuideWidthDp, guideH);
            _guideB.rectTransform.sizeDelta = new Vector2(GuideWidthDp, guideH);
            _guideA.color = GuideColor(travel.HasRelay ? travel.GuideFrom : travel.GuideTo);
            _guideB.color = GuideColor(travel.GuideTo);
            _flagSprite = travel.Flag != null ? _art.MapFlag(travel.Flag) : null;
            _flag.sprite = _flagSprite;
            _flag.rectTransform.sizeDelta = new Vector2(ProceduralArt.MapFlagWidthDp, ProceduralArt.MapFlagHeightDp);
            _mast.rectTransform.sizeDelta = new Vector2(MastWidthDp, MastHeightDp);
            SetActive(_flag.gameObject, _flagSprite != null);
            SetActive(_mast.gameObject, _flagSprite != null);
            ApplyGuideSprites();
            _buildMs = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            Apply(0f, reduceMotion); // el instante 0 (con Reducir movimiento, el final aún sin fundir)
        }

        /// <summary>Oculta la franja (sin viaje); las mallas y las cintas se quedan para el siguiente.</summary>
        public void Hide()
        {
            SetActive(_edge.gameObject, false);
            SetActive(_mask.gameObject, false);
            SetActive(_corners.gameObject, false);
            _built = null;
            _flagSprite = null;
            _flag.sprite = null;
            _fraction = 0f;
            _flagRaise = 0f;
        }

        /// <summary>
        /// Sprite del guía (el de Rosa del banner, ya pintado con su arte; null: sin guía, como con el respaldo de D-012 o al
        /// soltar el arte).
        /// </summary>
        public void SetGuideSprite(Sprite? sprite)
        {
            _guideSprite = sprite;
            ApplyGuideSprites();
        }

        /// <summary>
        /// Pinta el instante <paramref name="ms"/> del viaje (desde que entra la tarjeta; <see cref="float.MaxValue"/>: el final):
        /// la espera de <see cref="WorldBannerView.TravelDelayMs"/>, la canoa en <see cref="WorldBannerView.CanoeTravelMs"/> con
        /// los guías delante y los nombres de paso, y la bandera en <see cref="MapConfig.FlagRaiseMs"/>. Con Reducir movimiento,
        /// el final con un fundido de <see cref="MapConfig.ReducedMotionFadeMs"/>. Sin asignaciones.
        /// </summary>
        public void Apply(float ms, bool reduceMotion)
        {
            BannerTravel? travel = _built;
            if (travel == null || !Visible)
            {
                return;
            }

            float canoeT = reduceMotion ? 1f : Mathf.Clamp01((ms - WorldBannerView.TravelDelayMs) / WorldBannerView.CanoeTravelMs);
            _fraction = canoeT >= 1f ? 1f : Ease.InOut(canoeT);
            float flagT = reduceMotion ? 1f : Mathf.Clamp01((ms - WorldBannerView.TravelDelayMs - WorldBannerView.CanoeTravelMs) / MapConfig.FlagRaiseMs);
            _flagRaise = _flagSprite != null ? flagT : 0f;
            _fade.alpha = reduceMotion ? Mathf.Clamp01(ms / MapConfig.ReducedMotionFadeMs) : 1f;

            // La canoa, en su punto del tramo, con la proa hacia donde va.
            float arc = _fraction * _totalDp;
            Vector2 canoe = PointAtDp(travel, arc);
            _canoeFacing = FacingAt(travel, arc, _canoeFacing);
            RectTransform c = _canoe.rectTransform;
            c.anchoredPosition = canoe;
            c.localScale = new Vector3(_canoeFacing, 1f, 1f);

            // El mástil en la popa y la bandera que sube por él, ondeando hacia popa.
            if (_flagSprite != null)
            {
                float stern = -_canoeFacing * ProceduralArt.MapCanoeWidthDp * SternFraction;
                Vector2 foot = canoe + new Vector2(stern, 2f);
                _mast.rectTransform.anchoredPosition = foot;
                float low = foot.y + (ProceduralArt.MapFlagHeightDp * 0.5f) + 1f;
                float high = foot.y + MastHeightDp - (ProceduralArt.MapFlagHeightDp * 0.5f);
                RectTransform f = _flag.rectTransform;
                f.anchoredPosition = new Vector2(foot.x, Mathf.Lerp(low, high, Ease.Out(_flagRaise)));
                f.localScale = new Vector3(-_canoeFacing, 1f, 1f);
                float a = Mathf.Clamp01(_flagRaise * 4f);
                _flag.canvasRenderer.SetAlpha(a);
                _mast.canvasRenderer.SetAlpha(a);
            }

            // Los guías: delante de la canoa por el agua; al llegar, esperan junto a la gran parada. Con relevo, el de la salida se
            // queda en su punto y el de la llegada aparece y sigue.
            float lead = Mathf.Min(arc + GuideLeadDp, _totalDp);
            float rest = Mathf.Clamp01((arc + GuideLeadDp - _totalDp) / GuideLeadDp) * GuideRestUpDp;
            if (_relayDp >= 0f)
            {
                float ramp = Mathf.Clamp01((arc - _relayDp) / RelayRampDp);
                _guideAAlpha = 1f - ramp;
                _guideBAlpha = ramp;
                PlaceGuide(_guideA, travel, Mathf.Min(lead, _relayDp), Mathf.Min(lead, _relayDp) < lead ? 0f : rest, _guideAAlpha);
                PlaceGuide(_guideB, travel, lead, rest, _guideBAlpha);
            }
            else
            {
                _guideAAlpha = 1f;
                _guideBAlpha = 0f;
                PlaceGuide(_guideA, travel, lead, rest, 1f);
            }

            // Los nombres de paso: salen al acercarse la canoa y se van al dejarlos atrás, sin pararse.
            int n = travel.PassingCount;
            for (int i = 0; i < n; i++)
            {
                float a = canoeT >= 1f ? 0f : Mathf.Clamp01(1f - (Mathf.Abs(arc - _passingDp[i]) / PassingWindowDp));
                _passing[i].canvasRenderer.SetAlpha(a);
            }
        }

        // El encuadre: la caja del tramo con el norte arriba (x de la franja = y del marco, el este; y de la franja = x del marco,
        // el norte), en la franja menos sus márgenes, entre 1× y el zoom máximo de la vista Mundo.
        private void Frame(BannerTravel travel)
        {
            float minE = float.MaxValue;
            float maxE = float.MinValue;
            float minN = float.MaxValue;
            float maxN = float.MinValue;
            for (int i = 0; i <= FrameSamples; i++)
            {
                Vector2 p = travel.PointAt(i / (float)FrameSamples);
                minE = Mathf.Min(minE, p.y);
                maxE = Mathf.Max(maxE, p.y);
                minN = Mathf.Min(minN, p.x);
                maxN = Mathf.Max(maxN, p.x);
            }

            float spanE = Mathf.Max(maxE - minE, 1e-4f);
            float spanN = Mathf.Max(maxN - minN, 1e-4f);
            float dpu = Mathf.Min((_w - (2f * PadXDp)) / spanE, (_h - (2f * PadYDp)) / spanN);
            _dpu = Mathf.Clamp(dpu, MapSpace.BasinWidthDp, MapSpace.BasinWidthDp * RiverMapScreen.WorldZoomMax);
            _center = new Vector2((minN + maxN) * 0.5f, (minE + maxE) * 0.5f);
        }

        // La línea entra en la franja (algún tramo de su polilínea toca la franja con un margen). Sin asignaciones.
        private bool LineInView(RiverLine line)
        {
            float hx = (_w * 0.5f) + VisibleMarginDp;
            float hy = (_h * 0.5f) + VisibleMarginDp;
            int count = line.PointCount;
            Vector2 prev = StripPoint(new Vector2(line.X(0), line.Y(0)));
            if (count == 1)
            {
                return Mathf.Abs(prev.x) <= hx && Mathf.Abs(prev.y) <= hy;
            }

            for (int i = 1; i < count; i++)
            {
                Vector2 p = StripPoint(new Vector2(line.X(i), line.Y(i)));
                if (Mathf.Max(prev.x, p.x) >= -hx && Mathf.Min(prev.x, p.x) <= hx && Mathf.Max(prev.y, p.y) >= -hy && Mathf.Min(prev.y, p.y) <= hy)
                {
                    return true;
                }

                prev = p;
            }

            return false;
        }

        // Un nombre por punto de paso con texto, ya escrito y colocado sobre su punto (dentro de la franja); se ven con SetAlpha.
        private void BuildPassing(BannerTravel travel)
        {
            int n = travel.PassingCount;
            while (_passing.Count < n)
            {
                TextMeshProUGUI label = UiBuilder.Label("Passing_" + _passing.Count, _overlay, TextStyle.PillWood, string.Empty, PassingSp, Palette.Cream, TextAlignmentOptions.Center);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                RectTransform rt = label.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                label.transform.SetSiblingIndex(0); // debajo de los guías y de la canoa
                _passing.Add(label);
            }

            if (_passingDp.Length < n)
            {
                _passingDp = new float[n];
            }

            for (int i = 0; i < _passing.Count; i++)
            {
                TextMeshProUGUI label = _passing[i];
                bool used = i < n;
                SetActive(label.gameObject, used);
                if (!used)
                {
                    continue;
                }

                label.text = travel.PassingName(i);
                Vector2 size = label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity);
                float w = Mathf.Ceil(size.x) + 2f;
                float h = Mathf.Ceil(size.y);
                label.rectTransform.sizeDelta = new Vector2(w, h);
                _passingDp[i] = travel.PassingFraction(i) * _totalDp;
                Vector2 p = StripPoint(travel.PointAt(travel.PassingFraction(i)));
                float x = Mathf.Clamp(p.x, -(_w * 0.5f) + (w * 0.5f) + 4f, (_w * 0.5f) - (w * 0.5f) - 4f);
                float y = p.y + PassingUpDp + (h * 0.5f) <= (_h * 0.5f) - 2f ? p.y + PassingUpDp : p.y - PassingUpDp;
                label.rectTransform.anchoredPosition = new Vector2(x, y);
                label.canvasRenderer.SetAlpha(0f);
            }
        }

        // Punto de la franja a arc dp del tramo.
        private Vector2 PointAtDp(BannerTravel travel, float arc)
        {
            return StripPoint(travel.PointAt(_totalDp > 0f ? arc / _totalDp : 1f));
        }

        // Hacia dónde va el tramo en arc (1 a la derecha, −1 a la izquierda); si va casi en vertical, como iba (previous).
        private int FacingAt(BannerTravel travel, float arc, int previous)
        {
            const float step = 12f; // dp: una ventana larga, para que un quiebro de la polilínea junto a la parada no le dé la vuelta
            Vector2 a = PointAtDp(travel, Mathf.Max(0f, arc - step));
            Vector2 b = PointAtDp(travel, Mathf.Min(_totalDp, arc + step));
            float dx = b.x - a.x;
            return Mathf.Abs(dx) < 0.25f ? previous : (dx > 0f ? 1 : -1);
        }

        // Un guía en arc dp del tramo, rest dp más arriba, mirando hacia donde va (Rosa mira a la izquierda en su sprite).
        private void PlaceGuide(Image guide, BannerTravel travel, float arc, float rest, float alpha)
        {
            RectTransform rt = guide.rectTransform;
            rt.anchoredPosition = PointAtDp(travel, arc) + new Vector2(0f, rest);
            int facing = FacingAt(travel, arc, _canoeFacing);
            rt.localScale = new Vector3(-facing, 1f, 1f);
            guide.canvasRenderer.SetAlpha(alpha);
        }

        private void ApplyGuideSprites()
        {
            BannerTravel? travel = _built;
            bool any = _guideSprite != null && travel != null;
            _guideA.sprite = _guideSprite;
            _guideB.sprite = _guideSprite;
            SetActive(_guideA.gameObject, any);
            SetActive(_guideB.gameObject, any && travel!.HasRelay);
        }

        // Rosa con su color; el bufeo boliviano, el mismo sprite con otra paleta [P].
        private static Color GuideColor(string guide)
        {
            return string.Equals(guide, RiverJourney.GuideBufeo, StringComparison.Ordinal) ? BufeoTint : Color.white;
        }

        private static void SetupRibbon(RiverRibbonGraphic ribbon, RiverLine? line, RibbonLook look, float s0, float s1, MapSpace space)
        {
            bool on = line != null;
            SetActive(ribbon.gameObject, on);
            if (!on)
            {
                return;
            }

            ribbon.SetLine(line);
            ribbon.SetLook(look);
            ribbon.SetFill(s0, s1);
            ribbon.SetSpace(space);
            ribbon.SetWidth(RiverMapScreen.RibbonWidthDp(MapView.World, line!.Class));
        }

        private static RiverRibbonGraphic NewRibbon(string name, RectTransform parent, RibbonPart part)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RiverRibbonGraphic));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            RiverRibbonGraphic ribbon = go.GetComponent<RiverRibbonGraphic>();
            ribbon.Setup();
            ribbon.SetPart(part);
            go.SetActive(false);
            return ribbon;
        }

        private Image Piece(string name, Sprite? sprite, Vector2 pivot)
        {
            Image img = UiBuilder.Image(name, _overlay, sprite, Color.white);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            return img;
        }

        private static RectTransform Layer(string name, RectTransform parent)
        {
            RectTransform rt = UiBuilder.Rect(name, parent);
            Stretch(rt);
            return rt;
        }

        private static int ActiveCount(List<RiverRibbonGraphic> ribbons)
        {
            int n = 0;
            for (int i = 0; i < ribbons.Count; i++)
            {
                if (ribbons[i].gameObject.activeSelf)
                {
                    n++;
                }
            }

            return n;
        }

        // Estira rt a su padre con el pivote en el centro: el origen local (el de MapSpace y de la capa) en el centro de la franja.
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void CenterRect(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }
    }
}
