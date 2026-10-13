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
    /// <para>Con «Reducir movimiento» (07 §7.8): solo el fundido, con Capi ya presentando y Rosa ya fuera, sin soplo ni ondas.
    /// Sin asignaciones por fotograma, y quieto del todo al terminar la entrada (<see cref="Settled"/>): Tick ya no toca ninguna
    /// transformada ni ningún color mientras sigue abierto, así que no obliga a rehacer los lotes del lienzo principal ni el
    /// recorte de las dos RectMask2D (guía 8, docs/art/direccion-visual-v2.md: el ambiente que se mueve va en Canvas propios).
    /// El arte (las mascotas y la viñeta con Capi) se pide al abrirlo y se libera al cerrarlo
    /// (<c>ProceduralArt.ReleaseBannerArt</c>): construir la pantalla no genera ninguna textura. El cuerpo de Capi va dibujado
    /// en la viñeta y no en una imagen aparte: el lienzo funde cada gráfico por separado, y a medio fundir el agua se veía a
    /// través de él (captura 69e); solo la pata, que se mueve, va aparte.</para>
    /// <para>El arte se pinta fuera del hilo principal (<see cref="AsyncArt"/>; <c>ProceduralArt.RequestBannerArt</c>): unos
    /// 230 ms de CPU en el Editor que, en el fotograma de la carga del nivel, lo habrían parado (08 §5.4). Mientras se pinta,
    /// el banner ya está abierto (el flujo en LEVEL_CARD, el velo recibe el toque) pero no se ve (<see cref="ArtReady"/> false,
    /// opacidad 0): detrás, el tablero en el fotograma 0 de la intro. Cuando el arte llega, entra como siempre desde t = 0. Un
    /// toque antes lo salta sin salida (no hay nada que ver salir; GameScreen).</para>
    /// </remarks>
    internal sealed class WorldBannerView
    {
        /// <summary>Opacidad del velo (la de la tarjeta previa).</summary>
        public const float DimAlpha = LevelCardView.DimAlpha;

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
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly Image _sceneEdge;
        private readonly RectTransform _sceneMask;
        private readonly Image _scene;
        private readonly RectTransform _rosaClip;
        private readonly Image _rosa;
        private readonly Image _foam;
        private readonly Image _spray;
        private readonly Image _ripple;
        private readonly RectTransform _capi;
        private readonly Image _paw;
        private readonly TextMeshProUGUI _place;
        private int _world;
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
        private bool _artApplied;
        private float _artWaitMs;
        private float _canvasW = 360f;
        private float _canvasH = 780f;
        private float _safeTop = 32f;
        private float _safeBottom = 24f;
        private float _sceneW;
        private float _sceneH;
        private float _fadeFrom = 1f;
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

        /// <summary>El banner se ve (también mientras sale).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>El banner está saliendo (Rosa se sumerge y se funde); un toque más termina la salida en el acto.</summary>
        public bool Dismissing => _dismissing;

        /// <summary>Terminó la entrada y está quieto: <see cref="Tick"/> no pinta nada hasta que sale.</summary>
        public bool Settled => _settled;

        /// <summary>
        /// El arte se pinta en un hilo del grupo y el banner espera a tenerlo (el juego); false, en el acto al abrirlo (los
        /// tests y las capturas que miden instantes exactos: <c>LaunchOptions.BannerArtSyncOverride</c>).
        /// </summary>
        public bool AsyncArt { get; set; } = true;

        /// <summary>El arte del banner abierto ya está puesto y la entrada corre (o ya terminó).</summary>
        public bool ArtReady => _artReady;

        /// <summary>Lo que esperó el banner abierto a su arte (ms de fotograma; 0 si llegó en el acto).</summary>
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

        /// <summary>Sprite de la viñeta (tests: el agua del mundo; null cerrado).</summary>
        public Sprite? SceneSprite => _scene.sprite;

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

        /// <summary>Opacidad del banner y del velo (fundidos).</summary>
        public float Alpha => _group.alpha;

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
        /// Rosa asoma y Capi presenta (todo quieto con <paramref name="reduceMotion"/>). Aquí se genera el arte (la primera vez
        /// desde que se cerró el último banner).
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
            _artApplied = false;
            _artWaitMs = 0f;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _group.alpha = 0f; // hasta que llegue el arte
            Layout(canvasW, canvasH, safeTop, safeBottom); // pide el arte con las medidas de la viñeta
            TryApplyArt();
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

            float cardW = Mathf.Min(LevelCardView.MaxWidthDp, canvasW - (2f * LevelCardView.SideMarginDp));
            float inner = cardW - (2f * PadDp);
            float y = PadDp;
            UiBuilder.Place(_header.rectTransform, PadDp, y, inner, HeaderDp);

            // Cabecera «Mundo {n}: {nombre}» (la clave hasta el primer « · ») si cabe a 24 sp; si no, «Mundo {n}» y el nombre abre
            // la línea del lugar, en negrita. Así el nombre del mundo nunca pesa menos que su número.
            string heading = UiText.WorldHeading(_world);
            _nameInTitle = WidthOf(_title, heading) <= inner - (2f * TitleInsetDp) - FitSlackDp;
            SetText(_title, _nameInTitle ? heading : UiText.WorldTitle(_world));
            _placeText = _nameInTitle ? UiText.WorldPlace(_world) : UiText.WorldPlaceNamed(_world);
            string placeRich = _nameInTitle ? _placeText : "<b>" + UiText.WorldName(_world) + "</b>" + UiText.WorldSeparator + UiText.WorldPlace(_world);
            y += HeaderDp + 8f;
            float sceneY = y;

            // El lugar, centrado bajo la viñeta, en 1 línea o en 2 cortadas en un « · ».
            FitPlace(placeRich, inner - FitSlackDp);
            float placeH = Mathf.Ceil(_place.GetPreferredValues(_place.text, inner, float.PositiveInfinity).y) + 2f;
            float rest = SceneDp + 10f + placeH + PadDp + 4f;
            float sceneH = SceneDp + Mathf.Max(0f, MinHeightDp - (y + rest));
            _sceneW = inner;
            _sceneH = sceneH;
            UiBuilder.Place(_sceneEdge.rectTransform, PadDp - SceneEdgeDp, sceneY - SceneEdgeDp, inner + (2f * SceneEdgeDp), sceneH + (2f * SceneEdgeDp));
            UiBuilder.Place(_sceneMask, PadDp, sceneY, inner, sceneH);
            UiBuilder.Place(_scene.rectTransform, 0f, 0f, inner, sceneH);
            if (_artReady)
            {
                // Otra pantalla con el banner ya a la vista (rara vez): la viñeta nueva, en el acto.
                _scene.sprite = _art.BannerScene(inner, sceneH, _water, MeetingWater(_world), _world);
            }
            else
            {
                // Al abrirlo: la viñeta (con Capi ya dibujado) y las mascotas, fuera del hilo principal si AsyncArt.
                _art.RequestBannerArt(inner, sceneH, _water, MeetingWater(_world), _world, AsyncArt);
            }

            y += sceneH + 10f;
            UiBuilder.Place(_place.rectTransform, PadDp, y, inner, placeH);
            y += placeH;
            float cardH = Mathf.Ceil(y + PadDp + 4f);

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
            ModalKit.Size(_panel, _surface, _art, 0f, 0f, cardW, cardH, true);
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
            _scaleFrom = _pop.localScale.x;
            _exitMs = 0f;
        }

        /// <summary>
        /// True una sola vez, cuando el arte del banner abierto llega y la entrada empieza (para el <c>AF_STATE
        /// world_banner_art</c>).
        /// </summary>
        public bool ConsumeArtApplied()
        {
            if (!_artApplied)
            {
                return false;
            }

            _artApplied = false;
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
            _artApplied = false;
            _world = 0;
            _group.alpha = 1f;
            _pop.localScale = Vector3.one;
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
        /// (<see cref="Settled"/>), no pinta nada.
        /// </summary>
        public void Tick(float dtMs)
        {
            if (!Visible)
            {
                return;
            }

            if (!_artReady)
            {
                // Esperando el arte que se pinta en otro hilo: sin entrada hasta que llega (entonces, desde t = 0).
                if (!_dismissing)
                {
                    _artWaitMs += dtMs;
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
            if (!_dismissing && _ms >= (_reduceMotion ? FadeInMs : EnterMs))
            {
                _settled = true; // Apply acaba de pintar el estado final de la entrada
            }
        }

        // Si el arte pedido al abrirlo ya está (en el acto o pintado en otro hilo), lo pone y empieza la entrada desde t = 0.
        private bool TryApplyArt()
        {
            if (_artReady)
            {
                return true;
            }

            if (_world == 0 || !_art.TryTakeBannerArt())
            {
                return false;
            }

            _scene.sprite = _art.BannerSceneSprite;
            EnsureArt();
            _artReady = true;
            _artApplied = true;
            _ms = 0f;
            Apply();
            return true;
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
            if (_world == 0)
            {
                return;
            }

            if (!_artReady)
            {
                _group.alpha = 0f; // aún sin arte: abierto (recibe el toque) pero sin verse
                return;
            }

            float ms = Mathf.Max(0f, _ms);
            bool still = _reduceMotion;

            // La tarjeta: fundido y leve rebote (sin rebote con Reducir movimiento); al salir, fundido desde donde estaba.
            float enter = Mathf.Clamp01(ms / FadeInMs);
            float alpha = Ease.Out(enter);
            float k = still ? 1f : Mathf.LerpUnclamped(PopFrom, 1f, Ease.OutBack(enter, 1.6f));
            float rise = still ? 1f : Ease.OutBack(Mathf.Clamp01((ms - RosaSurfaceStartMs) / RosaSurfaceMs), 1.2f);
            float dive = 0f;
            if (_dismissing)
            {
                // Primero Rosa se sumerge con la tarjeta entera; después, solo en los últimos FadeTailMs, la tarjeta se funde y
                // se encoge (con Reducir movimiento, solo el fundido).
                float exitMs = still ? ExitReducedMs : ExitMs;
                float fade = still ? Mathf.Clamp01(_exitMs / exitMs) : Mathf.Clamp01((_exitMs - (exitMs - FadeTailMs)) / FadeTailMs);
                alpha = _fadeFrom * (1f - Ease.In(fade));
                k = still ? _scaleFrom : _scaleFrom * Mathf.Lerp(1f, ExitScale, Ease.Out(fade));
                dive = still ? 0f : Ease.In(Mathf.Clamp01(_exitMs / (DiveEnd * exitMs)));
            }

            _group.alpha = alpha;
            _pop.localScale = new Vector3(k, k, 1f);

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

        // La línea del lugar en 1 línea o, si no cabe, en 2 cortadas en el « · » más equilibrado (nunca dentro de un nombre), de
        // 18 a 15 sp. Asigna (cadenas nuevas), pero solo al colocar el banner.
        private void FitPlace(string rich, float room)
        {
            for (float sp = PlaceSp; sp >= PlaceMinSp; sp -= 1f)
            {
                _place.fontSize = sp;
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

        // El toque, también mientras sale (GameScreen termina la salida en el acto con el segundo).
        private void OnTap()
        {
            if (Visible)
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
