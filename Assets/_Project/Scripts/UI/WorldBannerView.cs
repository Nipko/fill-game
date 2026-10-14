#nullable enable
using System;
using System.Collections.Generic;
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
    /// <para>La línea del lugar va en 1 línea o, si no cabe, en 2 con el corte en un « · » (nunca dentro de un nombre: «ríos» o
    /// «Negro +» no se quedan solos al final de la línea) a 18 sp o, si hace falta, más pequeña hasta 15.</para>
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

        private const float PadDp = 16f;
        private const float HeaderDp = 40f;
        private const float TitleInsetDp = 8f;
        private const float FitSlackDp = 4f;
        private const float SceneEdgeDp = 2f;
        private const float PopFrom = 0.92f;

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
        /// Crea el banner, oculto, bajo <paramref name="parent"/>; <paramref name="onTap"/> recibe el toque. No pide ningún arte
        /// (las mascotas y la viñeta se generan al abrirlo, <see cref="Show"/>).
        /// </summary>
        public WorldBannerView(Transform parent, ProceduralArt art, Action onTap)
        {
            _art = art;
            _onTap = onTap ?? throw new ArgumentNullException(nameof(onTap));
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
        /// (<see cref="Waiting"/>) hasta que llega o hasta el respaldo.
        /// </summary>
        public void Show(int world, Color water, bool reduceMotion, float canvasW, float canvasH, float safeTop, float safeBottom)
        {
            _world = world;
            _text = UiText.WorldBanner(world);
            _water = water;
            _reduceMotion = reduceMotion;
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
        /// tanto, ya está al abrirlo. Sin efecto con el banner abierto. No crea ninguna textura (eso, al abrirlo).
        /// </summary>
        public void Prefetch(int world, Color water, float canvasW)
        {
            if (Visible || world <= 0)
            {
                return;
            }

            Measure(world, canvasW, out float cardW, out float inner, out _, out float sceneH, out _, out float cardH);
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
            Measure(_world, canvasW, out float cardW, out float inner, out float headerH, out float sceneH, out float placeH, out float cardH);
            UiBuilder.Place(_header.rectTransform, PadDp, y, inner, headerH);
            y += headerH + 8f;
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
            if (!_dismissing && _ms >= (_reduceMotion || _fallback != null ? FadeInMs : EnterMs))
            {
                _settled = true; // Apply acaba de pintar el estado final de la entrada
            }
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
        private void Measure(int world, float canvasW, out float cardW, out float inner, out float headerH, out float sceneH, out float placeH, out float cardH)
        {
            float s = _fontScale;
            cardW = Mathf.Min(LevelCardView.MaxWidthDp, canvasW - (2f * LevelCardView.SideMarginDp));
            inner = cardW - (2f * PadDp);
            _title.fontSize = TitleSp * s;
            string heading = UiText.WorldHeading(world);
            _nameInTitle = WidthOf(_title, heading) <= inner - (2f * TitleInsetDp) - FitSlackDp;
            SetText(_title, _nameInTitle ? heading : UiText.WorldTitle(world));
            _placeText = _nameInTitle ? UiText.WorldPlace(world) : UiText.WorldPlaceNamed(world);
            string placeRich = _nameInTitle ? _placeText : "<b>" + UiText.WorldName(world) + "</b>" + UiText.WorldSeparator + UiText.WorldPlace(world);

            // Con la escala, la placa de la cabecera crece con ella y, como en la tarjeta previa, al menos hasta el alto de línea de
            // su título (Baloo 2: ≈ 1,6 veces el tamaño); a 100 % se queda en sus 40 dp de siempre.
            headerH = HeaderDp;
            if (s > 1f)
            {
                float titleH = Mathf.Ceil(_title.GetPreferredValues(_title.text, float.PositiveInfinity, float.PositiveInfinity).y);
                headerH = Mathf.Max(HeaderDp * s, titleH + 2f);
            }

            // El lugar, centrado bajo la viñeta, en 1 línea o en 2 cortadas en un « · »; la viñeta crece hasta el alto mínimo.
            FitPlace(placeRich, inner - FitSlackDp, s);
            placeH = Mathf.Ceil(_place.GetPreferredValues(_place.text, inner, float.PositiveInfinity).y) + 2f;
            float sceneY = PadDp + headerH + 8f;
            float rest = SceneDp + 10f + placeH + PadDp + 4f;
            sceneH = SceneDp + Mathf.Max(0f, MinHeightDp - (sceneY + rest));
            cardH = Mathf.Ceil(sceneY + sceneH + 10f + placeH + PadDp + 4f);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }

        // La línea del lugar en 1 línea o, si no cabe, en 2 cortadas en el « · » más equilibrado (nunca dentro de un nombre), de
        // 18 a 15 sp por la escala scale. Asigna (cadenas nuevas), pero solo al colocar el banner.
        private void FitPlace(string rich, float room, float scale)
        {
            for (float sp = PlaceSp; sp >= PlaceMinSp; sp -= 1f)
            {
                _place.fontSize = sp * scale;
                if (WidthOf(_place, rich) <= room)
                {
                    SetText(_place, rich);
                    return;
                }

                string? split = BestSplit(rich, room);
                if (split != null)
                {
                    SetText(_place, split);
                    return;
                }
            }

            // Ni a 15 sp caben las dos mitades: el corte más equilibrado, y TMP parte lo que sobre.
            SetText(_place, BestSplit(rich, float.PositiveInfinity) ?? rich);
        }

        // El corte en un « · » con la línea más larga más corta (y que quepa en room), o null.
        private string? BestSplit(string rich, float room)
        {
            string sep = UiText.WorldSeparator;
            string? best = null;
            float bestW = float.MaxValue;
            int i = rich.IndexOf(sep, StringComparison.Ordinal);
            while (i >= 0)
            {
                string a = rich.Substring(0, i);
                string b = rich.Substring(i + sep.Length);
                float w = Mathf.Max(WidthOf(_place, a), WidthOf(_place, b));
                if (w <= room && w < bestW)
                {
                    bestW = w;
                    best = a + "\n" + b;
                }

                i = rich.IndexOf(sep, i + sep.Length, StringComparison.Ordinal);
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
}
