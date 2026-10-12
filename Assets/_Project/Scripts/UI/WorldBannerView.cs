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
    /// del dueño del 2026-10-11): una tarjeta simple de 1 toque con la cabecera «Mundo {n}», una viñeta del río del mundo (el agua
    /// de su tipo, 07 §2.1) en la que Capi presenta con la pata (<c>capi_present</c>) y Rosa asoma al abrir
    /// (<c>rosa_surface</c>, con su soplo) y se sumerge al cerrar (<c>rosa_dive</c>), y debajo «{nombre} · {país} · {río}». El
    /// texto entero es el de la clave: «Mundo {n}: {nombre} · {país} · {río}» (<see cref="Text"/>). Material de D-003: la
    /// tarjeta de madera clara con su marco de las tarjetas previas (<see cref="ModalKit"/>), la placa de madera oscura y la
    /// viñeta con el canto de madera; arte procedural provisional [P] (<c>ProceduralArt.Mascots</c>).
    /// </summary>
    /// <remarks>
    /// <para>Se cierra con 1 toque en cualquier punto (el velo cubre la pantalla; ningún botón de debajo recibe el toque) y sale
    /// sin bloquear toques: Rosa se sumerge mientras la tarjeta se funde (<see cref="ExitMs"/>), y el toque siguiente ya salta la
    /// intro o cierra la tarjeta previa que llega detrás. No se cierra sola, como la tarjeta previa [P].</para>
    /// <para>Con «Reducir movimiento» (07 §7.8): solo el fundido, con Capi ya presentando y Rosa ya fuera, sin soplo ni ondas.
    /// Sin asignaciones por fotograma: todo se crea una vez y se coloca al mostrarla o al cambiar la pantalla.</para>
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

        /// <summary>Fin de la entrada (todo quieto, salvo el leve vaivén de Rosa).</summary>
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

        private const float PadDp = 16f;
        private const float HeaderDp = 40f;
        private const float TitleSp = 24f;
        private const float PlaceSp = 18f;
        private const float PlaceMinSp = 15f;
        private const float SceneEdgeDp = 2f;
        private const float PopFrom = 0.92f;
        private const float RosaBobDp = 1.5f;
        private const float RosaBobMs = 2600f;

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
        private readonly Image _spray;
        private readonly Image _ripple;
        private readonly Image _capi;
        private readonly Image _paw;
        private readonly TextMeshProUGUI _place;
        private int _world;
        private string _text = string.Empty;
        private Color _water;
        private float _ms = -1f;
        private float _exitMs = -1f;
        private bool _dismissing;
        private bool _entered;
        private bool _enteredFired;
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

        /// <summary>Crea el banner, oculto, bajo <paramref name="parent"/>; <paramref name="onTap"/> recibe el toque.</summary>
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
            _title.fontSizeMax = TitleSp;
            _title.fontSize = TitleSp;

            // Viñeta del río con el canto de madera oscura; Rosa en su recorte (todo lo de debajo de la línea del agua no se ve).
            _sceneEdge = UiBuilder.Sliced("SceneEdge", _panel, art.RoundedRect, Palette.FamilyWood.Outline);
            _sceneEdge.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / (ProceduralArt.BannerSceneRadiusDp + SceneEdgeDp);
            _sceneMask = UiBuilder.Rect("Scene", _panel);
            _sceneMask.gameObject.AddComponent<RectMask2D>();
            _scene = UiBuilder.Image("River", _sceneMask, null, Color.white);
            _ripple = UiBuilder.Image("Ripple", _sceneMask, art.RosaRipple, new Color(1f, 1f, 1f, 0f));
            _rosaClip = UiBuilder.Rect("RosaClip", _sceneMask);
            _rosaClip.gameObject.AddComponent<RectMask2D>();
            _rosa = UiBuilder.Image("Rosa", _rosaClip, art.Rosa, Color.white);
            _spray = UiBuilder.Image("Spray", _sceneMask, art.RosaSpray, new Color(1f, 1f, 1f, 0f));

            // Capi, en el banco de arena de la izquierda; la pata va delante del cuerpo y gira en el hombro.
            _capi = UiBuilder.Image("Capi", _panel, art.CapiPresent, Color.white);
            _paw = UiBuilder.Image("CapiPaw", _capi.transform, art.CapiPaw, Color.white);

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

        /// <summary>Mundo que presenta (0 si no hay).</summary>
        public int World => _world;

        /// <summary>«Mundo {n}: {nombre} · {país} · {río}» (clave <c>world_banner</c>), o null si no se ve.</summary>
        public string? Text => Visible ? _text : null;

        /// <summary>Título de la cabecera («Mundo {n}»).</summary>
        public string TitleText => _title.text;

        /// <summary>Línea del lugar («{nombre} · {país} · {río}»).</summary>
        public string PlaceText => _place.text;

        /// <summary>Velo a pantalla completa que recibe el toque (tests).</summary>
        public Image Dim => _dim;

        /// <summary>Cuerpo de la tarjeta (tests y AF_LAYOUT).</summary>
        public RectTransform Panel => _panel;

        /// <summary>Superficie de madera del cuerpo (tests: alto compartido).</summary>
        public Image Surface => _surface;

        /// <summary>Viñeta del río (tests de solapes).</summary>
        public RectTransform Scene => _sceneMask;

        /// <summary>Sprite de la viñeta (tests: el agua del mundo).</summary>
        public Sprite? SceneSprite => _scene.sprite;

        /// <summary>Capi (cuerpo) en la viñeta (tests).</summary>
        public Image Capi => _capi;

        /// <summary>Pata que presenta (tests).</summary>
        public Image Paw => _paw;

        /// <summary>Rosa (tests).</summary>
        public Image Rosa => _rosa;

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
        /// Rosa asoma y Capi presenta (todo quieto con <paramref name="reduceMotion"/>).
        /// </summary>
        public void Show(int world, Color water, bool reduceMotion, float canvasW, float canvasH, float safeTop, float safeBottom)
        {
            _world = world;
            _text = UiText.WorldBanner(world);
            _title.text = UiText.WorldTitle(world);
            _place.text = UiText.WorldPlace(world);
            _water = water;
            _reduceMotion = reduceMotion;
            _root.gameObject.SetActive(true);
            _dismissing = false;
            _entered = false;
            _enteredFired = false;
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
            y += HeaderDp + 8f;
            float sceneY = y;

            // El lugar, centrado bajo la viñeta, en 1 o 2 líneas (a 18 sp; si no cabe en 2, más pequeño hasta 15).
            _place.fontSize = PlaceSp;
            for (float sp = PlaceSp; sp >= PlaceMinSp; sp -= 1f)
            {
                _place.fontSize = sp;
                if (LinesOf(_place, inner) <= 2)
                {
                    break;
                }
            }

            // Con 2 líneas, equilibradas: la caja se estrecha hasta el ancho más pequeño con las mismas líneas (sin una palabra
            // suelta debajo).
            float placeW = inner;
            int lines = LinesOf(_place, inner);
            if (lines >= 2)
            {
                float lo = inner * 0.3f;
                float hi = inner;
                for (int i = 0; i < 14; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    if (LinesOf(_place, mid) <= lines)
                    {
                        hi = mid;
                    }
                    else
                    {
                        lo = mid;
                    }
                }

                placeW = Mathf.Min(inner, Mathf.Ceil(hi) + 2f);
            }

            float placeH = Mathf.Ceil(_place.GetPreferredValues(_place.text, placeW, float.PositiveInfinity).y) + 2f;
            float rest = SceneDp + 10f + placeH + PadDp + 4f;
            float sceneH = SceneDp + Mathf.Max(0f, MinHeightDp - (y + rest));
            _sceneW = inner;
            _sceneH = sceneH;
            UiBuilder.Place(_sceneEdge.rectTransform, PadDp - SceneEdgeDp, sceneY - SceneEdgeDp, inner + (2f * SceneEdgeDp), sceneH + (2f * SceneEdgeDp));
            UiBuilder.Place(_sceneMask, PadDp, sceneY, inner, sceneH);
            UiBuilder.Place(_scene.rectTransform, 0f, 0f, inner, sceneH);
            _scene.sprite = _art.BannerScene(inner, sceneH, _water);
            y += sceneH + 10f;
            UiBuilder.Place(_place.rectTransform, PadDp + Mathf.Round((inner - placeW) * 0.5f), y, placeW, placeH);
            y += placeH;
            float cardH = Mathf.Ceil(y + PadDp + 4f);

            // Rosa: recortada en la línea del agua; su pie (pivote) baja hasta que solo asoma RosaAbove de su alto.
            float waterline = sceneH * RosaWaterline;
            UiBuilder.Place(_rosaClip, 0f, 0f, inner, waterline);
            _rosa.rectTransform.anchorMin = new Vector2(0f, 1f);
            _rosa.rectTransform.anchorMax = new Vector2(0f, 1f);
            _rosa.rectTransform.pivot = new Vector2(0.5f, 0f);
            _rosa.rectTransform.sizeDelta = new Vector2(ProceduralArt.RosaWidthDp, ProceduralArt.RosaHeightDp);
            Center(_ripple.rectTransform, inner * RosaX, waterline, ProceduralArt.RosaRippleWidthDp, ProceduralArt.RosaRippleHeightDp);
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
            Sprite pawSprite = _art.CapiPaw;
            paw.pivot = new Vector2(pawSprite.pivot.x / pawSprite.rect.width, 0.5f);
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
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _fadeFrom = _group.alpha;
            _scaleFrom = _pop.localScale.x;
            _exitMs = 0f;
        }

        /// <summary>Lo oculta en el acto (carga de otro nivel).</summary>
        public void HideNow()
        {
            _dismissing = false;
            _ms = -1f;
            _exitMs = -1f;
            _world = 0;
            _group.alpha = 1f;
            _pop.localScale = Vector3.one;
            if (_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
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

        /// <summary>Avanza la entrada (fundido, Rosa, el soplo y la pata de Capi), el vaivén de Rosa y la salida.</summary>
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

            Apply();
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

            // Rosa: asoma desde bajo el agua (con un vaivén leve una vez fuera) y se sumerge al cerrar.
            _rosaRise = Mathf.Clamp01(rise) * (1f - dive);
            float waterline = _sceneH * RosaWaterline;
            float hidden = ProceduralArt.RosaHeightDp * (1f - RosaAbove);
            float below = ProceduralArt.RosaHeightDp + 4f;
            float bob = still || !_entered || _dismissing ? 0f : RosaBobDp * Mathf.Sin(ms * (2f * Mathf.PI / RosaBobMs));
            float pivotBelow = Mathf.LerpUnclamped(below, hidden, rise) + ((below - hidden) * dive) - bob;
            _rosa.rectTransform.anchoredPosition = new Vector2(_sceneW * RosaX, -(waterline + pivotBelow));
            _rosa.rectTransform.localEulerAngles = new Vector3(0f, 0f, dive * -14f);

            // Ondas en el agua al asomar y al sumergirse.
            float ring = 0f;
            float ringScale = 1f;
            if (!still)
            {
                float t0 = Mathf.Clamp01((ms - RosaSurfaceStartMs) / 700f);
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

            // Soplo de la exhalación sobre el respiradero.
            float sprayT = still || _dismissing ? 1f : Mathf.Clamp01((ms - SprayStartMs) / SprayMs);
            float sprayA = sprayT <= 0f || sprayT >= 1f ? 0f : Mathf.Sin(sprayT * Mathf.PI) * 0.95f;
            Vector2 blow = ProceduralArt.RosaBlowhole;
            float bx = (_sceneW * RosaX) + ((blow.x - 0.5f) * ProceduralArt.RosaWidthDp);
            float by = waterline + pivotBelow - (blow.y * ProceduralArt.RosaHeightDp);
            _spray.rectTransform.anchoredPosition = new Vector2(bx, -(by - (8f * Ease.Out(sprayT))));
            float sprayK = 0.5f + (0.6f * Ease.Out(sprayT));
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

        // Líneas de t en una caja de width dp (el alto preferido entre el de una línea).
        private static int LinesOf(TextMeshProUGUI t, float width)
        {
            string text = t.text;
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            float one = t.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).y;
            if (one <= 0f)
            {
                return 1;
            }

            return Mathf.Max(1, Mathf.RoundToInt(t.GetPreferredValues(text, width, float.PositiveInfinity).y / one));
        }
    }
}
