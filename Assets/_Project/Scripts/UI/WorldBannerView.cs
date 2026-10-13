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
    /// viñeta con el canto de madera; arte procedural provisional [P] (<c>ProceduralArt.Mascots</c>).
    /// </summary>
    /// <remarks>
    /// <para>Se cierra con 1 toque en cualquier punto (el velo cubre la pantalla; ningún botón de debajo recibe el toque) y sale
    /// sin bloquear toques: Rosa se sumerge mientras la tarjeta se funde (<see cref="ExitMs"/>), y el toque siguiente ya salta la
    /// intro o cierra la tarjeta previa que llega detrás. No se cierra sola, como la tarjeta previa [P].</para>
    /// <para>La línea del lugar va en 1 línea o, si no cabe, en 2 con el corte en un « · » (nunca dentro de un nombre: «ríos» o
    /// «Negro +» no se quedan solos al final de la línea) a 18 sp o, si hace falta, más pequeña hasta 15.</para>
    /// <para>Con «Reducir movimiento» (07 §7.8): solo el fundido, con Capi ya presentando y Rosa ya fuera, sin soplo ni ondas.
    /// Sin asignaciones por fotograma, y quieto del todo al terminar la entrada (<see cref="Settled"/>): Tick ya no toca ninguna
    /// transformada ni ningún color mientras sigue abierto, así que no obliga a rehacer los lotes del lienzo principal ni el
    /// recorte de las dos RectMask2D (guía 8, docs/art/direccion-visual-v2.md: el ambiente que se mueve va en Canvas propios).
    /// El arte (las mascotas y la viñeta) se pide al abrirlo y se libera al cerrarlo (<c>ProceduralArt.ReleaseBannerArt</c>):
    /// construir la pantalla no genera ninguna textura.</para>
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

        /// <summary>Soplo de la exhalación: duración (ms) [P].</summary>
        public const float SprayMs = 650f;

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

        /// <summary>Salida: Rosa se sumerge y la tarjeta se funde (ms) [P].</summary>
        public const float ExitMs = 340f;

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

        /// <summary>Eje de Capi (fracción del ancho de la viñeta).</summary>
        public const float CapiX = 0.17f;

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
        private readonly Image _capi;
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

            // Capi, en el banco de arena de la izquierda; la pata va delante del cuerpo y gira en el hombro.
            _capi = UiBuilder.Image("Capi", _panel, null, Color.white);
            _paw = UiBuilder.Image("CapiPaw", _capi.transform, null, Color.white);

            _place = ModalKit.Line("Place", _panel, string.Empty, PlaceSp, Palette.FamilyWood.Deep);
            _place.enableAutoSizing = false;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Raíz del banner (con el velo), estirada a la pantalla.</summary>
        public RectTransform Root => _root;

        /// <summary>El banner se ve (también mientras sale).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>El banner está saliendo: ya no recibe toques.</summary>
        public bool Dismissing => _dismissing;

        /// <summary>Terminó la entrada y está quieto: <see cref="Tick"/> no pinta nada hasta que sale.</summary>
        public bool Settled => _settled;

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

        /// <summary>Capi (cuerpo) en la viñeta (tests).</summary>
        public Image Capi => _capi;

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

        /// <summary>El banner recibe toques (no mientras sale).</summary>
        public bool BlocksRaycasts => _group.blocksRaycasts;

        /// <summary>Contenedor que escala la tarjeta en la entrada (tests).</summary>
        public RectTransform Pop => _pop;

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
            EnsureArt();
            _root.gameObject.SetActive(true);
            _dismissing = false;
            _entered = false;
            _enteredFired = false;
            _settled = false;
            _exitMs = -1f;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _group.alpha = 1f;
            Layout(canvasW, canvasH, safeTop, safeBottom);
            _ms = 0f;
            Apply();
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
            _scene.sprite = _art.BannerScene(inner, sceneH, _water);
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

            // Capi de pie en el banco (su pivote abajo en el centro) y la pata en su hombro.
            float bank = sceneY + (sceneH * ProceduralArt.BannerSceneBankTop);
            RectTransform capi = _capi.rectTransform;
            capi.anchorMin = new Vector2(0f, 1f);
            capi.anchorMax = new Vector2(0f, 1f);
            capi.pivot = new Vector2(0.5f, 0f);
            capi.sizeDelta = new Vector2(ProceduralArt.CapiPresentDp, ProceduralArt.CapiPresentDp);
            capi.anchoredPosition = new Vector2(PadDp + (inner * CapiX), -(bank + 3f));
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

        /// <summary>Empieza la salida: deja de recibir toques; Rosa se sumerge y la tarjeta se funde.</summary>
        public void Dismiss()
        {
            if (!Visible || _dismissing)
            {
                return;
            }

            _dismissing = true;
            _settled = false;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _fadeFrom = _group.alpha;
            _scaleFrom = _pop.localScale.x;
            _exitMs = 0f;
        }

        /// <summary>Lo oculta en el acto (al terminar la salida o al cargar otro nivel) y suelta su arte.</summary>
        public void HideNow()
        {
            _dismissing = false;
            _settled = false;
            _ms = -1f;
            _exitMs = -1f;
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
            if (!Visible || _ms < 0f)
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

        // Las mascotas, la onda, la espuma y el soplo se piden al abrir el banner (la viñeta, en Layout): construir la pantalla o
        // jugar un nivel sin banner no genera ninguna textura.
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

            if (_capi.sprite == null)
            {
                _capi.sprite = _art.CapiPresent;
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
            _capi.sprite = null;
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
                float exitT = Mathf.Clamp01(_exitMs / (still ? ExitReducedMs : ExitMs));
                float fade = still ? exitT : Mathf.Clamp01((exitT - 0.3f) / 0.7f);
                alpha = _fadeFrom * (1f - Ease.Out(fade));
                k = _scaleFrom;
                dive = still ? 0f : Ease.In(Mathf.Clamp01(exitT / 0.75f));
            }

            _group.alpha = alpha;
            _pop.localScale = new Vector3(k, k, 1f);

            // Rosa: asoma desde bajo el agua y se queda quieta; al cerrar, se sumerge.
            _rosaRise = Mathf.Clamp01(rise) * (1f - dive);
            float waterline = _sceneH * RosaWaterline;
            float hidden = ProceduralArt.RosaHeightDp * (1f - RosaAbove);
            float below = ProceduralArt.RosaHeightDp + 4f;
            float pivotBelow = Mathf.LerpUnclamped(below, hidden, rise) + ((below - hidden) * dive);
            _rosa.rectTransform.anchoredPosition = new Vector2(_sceneW * RosaX, -(waterline + pivotBelow));
            _rosa.rectTransform.localEulerAngles = new Vector3(0f, 0f, dive * -14f);

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
                    float t1 = Mathf.Clamp01(_exitMs / ExitMs);
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

            // Capi: la pata sube del costado a presentar, con un leve impulso del cuerpo.
            float present = still ? 1f : Ease.OutBack(Mathf.Clamp01((ms - CapiStartMs) / CapiPresentMs), 1.4f);
            float angle = Mathf.LerpUnclamped(PawDownDeg, PawUpDeg, present);
            _paw.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
            float lift = still ? 0f : 2f * Mathf.Sin(Mathf.Clamp01((ms - CapiStartMs) / CapiPresentMs) * Mathf.PI);
            Vector2 capiPos = _capi.rectTransform.anchoredPosition;
            float bankY = -((_sceneMask.anchoredPosition.y * -1f) + (_sceneH * ProceduralArt.BannerSceneBankTop) + 3f);
            _capi.rectTransform.anchoredPosition = new Vector2(capiPos.x, bankY + lift);
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

        private void OnTap()
        {
            if (Visible && !_dismissing)
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
