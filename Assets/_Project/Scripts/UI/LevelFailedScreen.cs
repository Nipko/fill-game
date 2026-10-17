#nullable enable
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Nivel fallido [P] (paso 3 del embudo de D-008; GDD 04 §6.6 wireframe «Paso 3»; 02 §2 LEVEL_FAILED; tarea B2-3), con la
    /// composición de «Level Failed!» de Magic Sort (ficha §4.4 y §4.8: cabecera «Level N» con X, franja «Level Failed!», el
    /// premio que no se cobra y un botón grande de reintentar) y el material de D-003: cesta de semillas vacía en lugar del
    /// pedestal de monedas, sin corazón roto ni calaveras. «Nivel {n}», «Nivel fallido», «Premio: {20 · 60 · 80} semillas»
    /// (<c>eco_reward_*</c>; no se cobra), «Reintentar» [P] y X → inicio. Sin intersticial antes ni después (05 §10). La cabecera
    /// lleva la etiqueta del nivel (propuesta-mecanicas-dificultad.md §c.1; D-009; como la tarjeta roja de Magic Sort en sus
    /// SUPER HARD, ficha §3.6, sin calavera): ámbar con la rana en Difícil, índigo con el jaguar en Muy difícil y la madera de
    /// siempre en normal (<see cref="SetLabel"/>, aplicada en el siguiente <see cref="Show"/>).
    /// <para><b>TiTino</b> (tarea TI6b; propuesta de TiTino d.5 y paso 6b de (i); D-016, T5-B; 04 §6.6): es la única pantalla del
    /// embudo en la que sale (nunca en Aguas estancadas, ¿Seguro? ni Sin canoas, que tienen oferta de pago). Lo decide
    /// GameScreen.Funnel.cs con el patrón de <see cref="SetLabel"/> (<see cref="SetSloth"/>, justo antes de abrir el modal):</para>
    /// <list type="bullet">
    /// <item>cuelga de su rama con <c>sloth_encourage</c> en el hueco izquierdo de la cesta vacía, que no se quita: a
    /// <see cref="SlothSizeDp"/> dp como mucho, con todo lo que mueve su gesto (medido con las piezas del rig) dentro del hueco
    /// entre la franja «Nivel fallido» y la línea de abajo, y sin tocar la cesta (<see cref="SlothBasketGapDp"/>). Su rama
    /// entra por el marco izquierdo de la tarjeta [P]: con la rama de 1,5 S, 64 dp no caben dentro del hueco de 88 dp de d.5;
    /// si ni así cabe (un lienzo estrecho), se encoge, y por debajo de <see cref="SlothMinSizeDp"/> no sale;</item>
    /// <item>la frase, solo en el primer fallo del nivel, va como línea de la propia tarjeta bajo la cesta: el ancho útil de 296
    /// dp, <see cref="PhraseSp"/> sp en ≤ <see cref="PhraseMaxLines"/> líneas, nunca menos de 12 sp (02 §12), con las dos líneas
    /// equilibradas; la tarjeta crece en alto. Si con la escala de texto ya no cabe entre el pie del HUD y
    /// <c>safeBottom</c> (<see cref="PhraseFits"/>), solo el gesto;</item>
    /// <item>«Reintentar» sale a la vez que hoy (la tarjeta no anima su entrada); TiTino no captura toques
    /// (<c>raycastTarget</c> = false en todas sus piezas) ni tapa nada.</item>
    /// </list>
    /// Perezoso: sin TiTino no se crea ni el rig ni la línea; sin su arte (release sin el PNG final), nunca sale.
    /// </summary>
    internal sealed class LevelFailedScreen
    {
        /// <summary>Tamaño S de TiTino en Nivel fallido, como mucho (propuesta de TiTino c.4 y d.5: 64 dp).</summary>
        public const float SlothSizeDp = 64f;

        /// <summary>Por debajo de este tamaño TiTino no sale en Nivel fallido (dp) [P].</summary>
        public const float SlothMinSizeDp = 40f;

        /// <summary>Aire entre lo que mueve TiTino y la cesta (dp) [P].</summary>
        public const float SlothBasketGapDp = 4f;

        /// <summary>Aire de TiTino con la franja «Nivel fallido» (dp) [P].</summary>
        public const float SlothStripGapDp = 4f;

        /// <summary>Texto de la frase (d.5: 14 sp; nunca menos de <see cref="UiBuilder.MinTextSp"/>).</summary>
        public const float PhraseSp = 14f;

        /// <summary>Líneas de la frase como mucho (d.5: ≤ 2).</summary>
        public const int PhraseMaxLines = 2;

        /// <summary>Aire entre la frase y el premio (dp) [P].</summary>
        public const float PhraseGapDp = 6f;

        // La cesta: 120 × 90 dp, y el hueco que deja hasta la línea de abajo.
        private const float BasketW = 120f;
        private const float BasketH = 90f;
        private const float BasketRowDp = 94f;
        private const float StripToBasketDp = 10f;

        // Holgura del ancho equilibrado de la frase (dp): la de redondeo de TextMeshPro, para que no salte de línea.
        private const float FitSlackDp = 2f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly Image _badge;
        private readonly ReliefParts _close;
        private readonly Image _strip;
        private readonly TextMeshProUGUI _stripLabel;
        private readonly Image _basket;
        private readonly TextMeshProUGUI _reward;
        private readonly ReliefParts _retry;
        private readonly List<Rect> _envelopeRects = new List<Rect>(16);
        private string _label = LevelDefinition.LabelNormal;
        private Color? _band;
        private SlothRig? _sloth;
        private SlothArt? _slothArt;
        private bool _slothArtMissing;
        private TextMeshProUGUI? _phrase;
        private SlothGesture _slothGesture;
        private string? _slothText;
        private bool _slothReduceMotion;
        private bool _slothPlaced;
        private bool _phrasePlaced;
        private float _phraseSize;
        private int _phraseLines;
        private float _topLimit = float.NegativeInfinity;
        private float _bottomLimit = float.PositiveInfinity;
        private bool _envelopeMeasured;
        private float _envLeft;
        private float _envRight;
        private float _envTop;
        private float _envBottom;

        public LevelFailedScreen(Transform parent, ProceduralArt art, IGameActions actions)
        {
            _art = art;
            _root = UiBuilder.Rect("LevelFailed", parent);
            ModalKit.Dim("Dim", _root, 0.55f);
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            _title = ModalKit.Header(_panel, art, UiText.Level(1), out _header);
            _badge = ModalKit.HeaderBadge(_header);
            _close = ModalKit.Close("CloseButton", _panel, art, () => actions.PressLevelFailedClose());
            _strip = UiBuilder.Sliced("Strip", _panel, art.WoodPlate, Color.white);
            _stripLabel = UiBuilder.Label("Label", _strip.transform, TextStyle.PillWood, UiText.LevelFailed, 20f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_stripLabel.rectTransform);
            _basket = UiBuilder.Image("Basket", _panel, art.EmptyBasket, Color.white);
            _basket.preserveAspect = true;
            _reward = ModalKit.Line("Reward", _panel, string.Empty, 18f, Palette.FamilyWood.Base);
            _retry = ModalKit.Cta("RetryButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, UiText.Retry, 24f, () => actions.PressRetry(), out _);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Raíz del modal, hija del lienzo de la pantalla de nivel (tests de orden de capas).</summary>
        public RectTransform Root => _root;

        /// <summary>La tarjeta (tests: dp desde arriba a la izquierda del lienzo con su <see cref="RectTransform.anchoredPosition"/>).</summary>
        public RectTransform Panel => _panel;

        public Button RetryButton => _retry.Button;

        public Button CloseButton => _close.Button;

        public string TitleText => _title.text;

        public string RewardText => _reward.text;

        /// <summary><c>difficulty.label</c> que viste la cabecera en el siguiente <see cref="Show"/> (o en el actual).</summary>
        public string Label => _label;

        /// <summary>Banda de la cabecera aplicada en el último <see cref="Show"/> (<c>ui_ambar</c> o <c>ui_indigo</c>; null = madera).</summary>
        public Color? HeaderBand => _band;

        /// <summary>Color del título de la cabecera («Nivel {n}»).</summary>
        public Color TitleColor => _title.color;

        /// <summary>
        /// Material del título de la cabecera (tests): el de <c>TextStyle.ModalTitle</c>, con contorno y sombra, sobre la madera
        /// y el índigo; el de la fuente, sin contorno, con la tinta sobre el ámbar (<c>ModalKit.SetTitleColor</c>).
        /// </summary>
        internal Material? TitleMaterial => _title.fontSharedMaterial;

        /// <summary>Sello de la etiqueta en la cabecera, o null si no se ve.</summary>
        public Sprite? HeaderBadge => _badge.gameObject.activeSelf ? _badge.sprite : null;

        /// <summary>TiTino está a la vista en la tarjeta.</summary>
        public bool SlothVisible => Visible && _sloth != null && _sloth.Visible;

        /// <summary>El rig de TiTino (null hasta el primer fallo en el que sale, o sin arte).</summary>
        public SlothRig? Sloth => _sloth;

        /// <summary>Tamaño S de TiTino ahora (dp; 0 si no se ve).</summary>
        public float SlothSize => SlothVisible ? _sloth!.Size : 0f;

        /// <summary>La frase de TiTino está a la vista bajo la cesta.</summary>
        public bool PhraseShown => Visible && _phrase != null && _phrase.gameObject.activeSelf;

        /// <summary>Texto de la frase a la vista («» si no hay).</summary>
        public string PhraseText => PhraseShown ? _phrase!.text : string.Empty;

        /// <summary>La línea de la frase (tests de tamaño; null hasta la primera frase).</summary>
        public TextMeshProUGUI? PhraseLabel => _phrase;

        /// <summary>Líneas de la frase a la vista (0 sin frase).</summary>
        public int PhraseLines => PhraseShown ? _phraseLines : 0;

        /// <summary>Tamaño del texto de la frase a la vista (dp; 0 sin frase).</summary>
        public float PhraseFontDp => PhraseShown ? _phraseSize : 0f;

        /// <summary>
        /// Guarda la etiqueta del nivel (<c>difficulty.label</c>: <c>normal</c>, <c>hard</c> o <c>very_hard</c>) para la cabecera
        /// del siguiente <see cref="Show"/>. La llama GameScreen (GameScreen.Funnel.cs) justo antes de abrir el modal.
        /// </summary>
        public void SetLabel(string? difficultyLabel)
        {
            _label = string.IsNullOrEmpty(difficultyLabel) ? LevelDefinition.LabelNormal : difficultyLabel!;
        }

        /// <summary>
        /// TiTino en el siguiente <see cref="Show"/> (tarea TI6b; d.5): <paramref name="gesture"/> (<see cref="SlothGesture.None"/>:
        /// no sale) y la frase <paramref name="text"/> bajo la cesta (null: solo el gesto), con o sin Reducir movimiento. La llama
        /// GameScreen (GameScreen.Funnel.cs) justo antes de abrir el modal, como <see cref="SetLabel"/>.
        /// </summary>
        public void SetSloth(SlothGesture gesture, string? text, bool reduceMotion)
        {
            _slothGesture = gesture;
            _slothText = string.IsNullOrEmpty(text) ? null : text;
            _slothReduceMotion = reduceMotion;
        }

        /// <summary>
        /// Dónde puede ir la tarjeta con la frase (dp desde arriba): por debajo de <paramref name="topDp"/> (el pie del HUD, donde va
        /// el saldo del embudo) y por encima de <paramref name="bottomDp"/> (<c>safeBottom</c>). Lo mira <see cref="PhraseFits"/>.
        /// </summary>
        public void SetBounds(float topDp, float bottomDp)
        {
            _topLimit = topDp;
            _bottomLimit = bottomDp;
        }

        /// <summary>
        /// Crea el rig de TiTino la primera vez que hace falta, con la regla del arte (sin arte final en release, nunca: false).
        /// </summary>
        public bool EnsureSlothArt()
        {
            if (_sloth != null)
            {
                return true;
            }

            if (_slothArtMissing)
            {
                return false;
            }

            _slothArt = _art.SlothRigArt();
            if (_slothArt == null)
            {
                _slothArtMissing = true;
                return false;
            }

            _sloth = SlothRig.Create(_panel, _slothArt, SlothSizeDp);
            return _sloth != null;
        }

        /// <summary>
        /// Si la frase <paramref name="text"/> cabe con TiTino en un lienzo de <paramref name="canvasW"/> × <paramref name="canvasH"/>
        /// dp (d.5): en ≤ <see cref="PhraseMaxLines"/> líneas a 12 sp o más, con TiTino en su hueco, y con la tarjeta, que crece,
        /// dentro de <see cref="SetBounds"/>. Mide el texto (crea la línea la primera vez): solo al abrir el modal, nunca por
        /// fotograma.
        /// </summary>
        public bool PhraseFits(string text, float canvasW, float canvasH)
        {
            if (string.IsNullOrEmpty(text) || !EnsureSlothArt())
            {
                return false;
            }

            SlothGesture gesture = _slothGesture;
            string? previous = _slothText;
            _slothGesture = SlothGesture.Encourage;
            _slothText = text;
            float height = Arrange(canvasW);
            bool placed = _slothPlaced && _phrasePlaced;
            _slothGesture = gesture;
            _slothText = previous;
            float top = PanelTop(canvasH, height);
            return placed && top >= _topLimit - 0.01f && top + height <= _bottomLimit + 0.01f;
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Muestra el nivel <paramref name="level"/> con el premio <paramref name="reward"/> que no se cobra.</summary>
        public void Show(int level, int reward, float canvasW, float canvasH)
        {
            _root.gameObject.SetActive(true);
            _title.text = UiText.Level(level);
            _reward.text = UiText.LevelFailedReward(reward);
            _band = ModalKit.ApplyHeaderLabel(_header, _title, _badge, _art, _label);
            float y = Arrange(canvasW);
            if (_slothPlaced)
            {
                _sloth!.Play(SlothPoseId.Encourage, _slothReduceMotion); // al abrir la tarjeta (d.2), ya en su sitio (Arrange)
            }
            else
            {
                _sloth?.SetVisible(false);
            }

            if (_phrase != null && _phrase.gameObject.activeSelf != _phrasePlaced)
            {
                _phrase.gameObject.SetActive(_phrasePlaced);
            }

            float width = Mathf.Min(328f, canvasW - 32f);

            // Con la frase la tarjeta cambia de alto (una o dos líneas, la escala): superficie compartida, como la tarjeta previa.
            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, PanelTop(canvasH, y), width, y, _phrasePlaced);
        }

        /// <summary>Avanza el gesto de TiTino (<c>sloth_encourage</c>: 0,8 s y reposo). Sin asignaciones.</summary>
        public void Tick(float dtMs)
        {
            if (_sloth != null && _sloth.Visible && Visible)
            {
                _sloth.Tick(dtMs);
            }
        }

        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            ids.Add("retry");
            rects.Add((RectTransform)_retry.Button.transform);
            ids.Add("close");
            rects.Add((RectTransform)_close.Button.transform);
        }

        /// <summary>
        /// Añade las cajas de las piezas a la vista de TiTino, en dp de <paramref name="space"/> con la y desde arriba (una capa a
        /// pantalla completa con el pivote en el centro, como <see cref="Root"/>; tests de solapes).
        /// </summary>
        public void CollectSlothRects(RectTransform space, List<Rect> rects)
        {
            if (SlothVisible)
            {
                _sloth!.CollectRects(space, rects);
            }
        }

        /// <summary>Lo demás de la tarjeta a la vista, con su nombre (tests de solapes).</summary>
        public void CollectParts(List<(string, RectTransform)> parts)
        {
            parts.Add(("cabecera", _header.rectTransform));
            parts.Add(("X", (RectTransform)_close.Button.transform));
            parts.Add(("franja «Nivel fallido»", _strip.rectTransform));
            parts.Add(("cesta", _basket.rectTransform));
            parts.Add(("premio", _reward.rectTransform));
            parts.Add(("Reintentar", (RectTransform)_retry.Button.transform));
            if (PhraseShown)
            {
                parts.Add(("frase", _phrase!.rectTransform));
            }
        }

        /// <summary>Añade los gráficos de TiTino y de su frase (tests de raycast).</summary>
        public void CollectSlothGraphics(List<Graphic> graphics)
        {
            _sloth?.CollectGraphics(graphics);
            if (_phrase != null)
            {
                graphics.Add(_phrase);
            }
        }

        // Arriba de la tarjeta de alto height en un lienzo de canvasH dp: como siempre, un 45 % del hueco que deja.
        private static float PanelTop(float canvasH, float height)
        {
            return Mathf.Round((canvasH - height) * 0.45f);
        }

        // Coloca todo dentro de la tarjeta (dp desde su esquina de arriba a la izquierda) y devuelve su alto: la cabecera, la
        // franja, la cesta con TiTino a su izquierda, la frase, el premio y «Reintentar». Mide la frase solo si cambió.
        private float Arrange(float canvasW)
        {
            float width = Mathf.Min(328f, canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 52f;
            UiBuilder.Place(_strip.rectTransform, 24f, y, width - 48f, 40f);
            y += 40f + StripToBasketDp;
            float basketLeft = (width - BasketW) * 0.5f;
            UiBuilder.Place(_basket.rectTransform, basketLeft, y, BasketW, BasketH);
            _slothPlaced = _slothGesture != SlothGesture.None && PlaceSloth(basketLeft, y);
            y += BasketRowDp;
            _phrasePlaced = false;
            if (_slothPlaced && _slothText != null)
            {
                float phrase = PlacePhrase(_slothText, 16f, y, inner);
                if (phrase > 0f)
                {
                    _phrasePlaced = true;
                    y += phrase + PhraseGapDp;
                }
            }

            y += ModalKit.PlaceLine(_reward, 16f, y, inner, 24f) + 10f;
            ModalKit.Place(_retry, 16f, y, inner);
            y += ModalKit.HeightOf(_retry) + 14f;
            return y;
        }

        // TiTino en el hueco izquierdo de la cesta (d.5): con todo lo que mueve sloth_encourage dentro del hueco entre la franja
        // y la línea de abajo, de la izquierda de la tarjeta (su rama entra por el marco) a SlothBasketGapDp de la cesta. A
        // SlothSizeDp como mucho; false si no cabe ni a SlothMinSizeDp o sin arte.
        private bool PlaceSloth(float basketLeft, float basketTop)
        {
            if (!EnsureSlothArt())
            {
                return false;
            }

            MeasureEnvelope();
            float left = 0f;
            float right = basketLeft - SlothBasketGapDp;
            float top = basketTop - StripToBasketDp + SlothStripGapDp;
            float bottom = basketTop + BasketRowDp - 2f;
            float spanX = _envRight - _envLeft;
            float spanY = _envBottom - _envTop;
            float s = Mathf.Floor(Mathf.Min(SlothSizeDp, Mathf.Min((right - left) / spanX, (bottom - top) / spanY)));
            if (s < SlothMinSizeDp)
            {
                return false;
            }

            SlothRig rig = _sloth!;
            if (!Mathf.Approximately(rig.Size, s))
            {
                rig.SetArt(_slothArt, s);
            }

            float x = left - (_envLeft * s) + (((right - left) - (spanX * s)) * 0.5f);
            float yDown = top - (_envTop * s) + (((bottom - top) - (spanY * s)) * 0.5f);
            rig.Place(x, yDown);
            return true;
        }

        // Lo que ocupa TiTino con sloth_encourage (de la entrada a su reposo, y quieto con Reducir movimiento), medido una vez con
        // las piezas del rig, en S y con la línea de la rama en el origen (y hacia abajo).
        private void MeasureEnvelope()
        {
            if (_envelopeMeasured)
            {
                return;
            }

            SlothRig rig = _sloth!;
            const float S = 100f;
            bool rootActive = _root.gameObject.activeSelf;
            if (!rootActive)
            {
                _root.gameObject.SetActive(true); // CollectRects solo mide piezas activas; nada se dibuja en este fotograma
            }

            bool wasVisible = rig.Visible;
            rig.SetArt(_slothArt, S);
            rig.SetVisible(true);
            rig.Place(0f, 0f);
            float x0 = float.MaxValue;
            float x1 = float.MinValue;
            float y0 = float.MaxValue;
            float y1 = float.MinValue;
            for (int k = 0; k <= 26; k++)
            {
                SlothPose pose = k < 25
                    ? SlothRig.Evaluate(SlothPoseId.Encourage, k * (SlothRig.EncourageMs / 20f), false)
                    : SlothRig.Evaluate(SlothPoseId.Encourage, SlothRig.ReducedMotionFadeMs, true);
                rig.Apply(in pose);
                _envelopeRects.Clear();
                rig.CollectRects(_panel, _envelopeRects);
                for (int i = 0; i < _envelopeRects.Count; i++)
                {
                    Rect r = _envelopeRects[i];
                    x0 = Mathf.Min(x0, r.xMin);
                    x1 = Mathf.Max(x1, r.xMax);
                    y0 = Mathf.Min(y0, r.yMin);
                    y1 = Mathf.Max(y1, r.yMax);
                }
            }

            rig.SetVisible(wasVisible);
            if (!rootActive)
            {
                _root.gameObject.SetActive(false);
            }

            if (x1 > x0 && y1 > y0)
            {
                _envLeft = x0 / S;
                _envRight = x1 / S;
                _envTop = y0 / S;
                _envBottom = y1 / S;
            }
            else
            {
                // Sin piezas medibles (no debería pasar): la caja de la victoria (SlothVictory), que la contiene.
                _envLeft = -SlothVictory.RigLeftS;
                _envRight = SlothVictory.BranchHalfS;
                _envTop = -SlothVictory.RigUpS;
                _envBottom = SlothVictory.RigDownS;
            }

            _envelopeMeasured = true;
        }

        // La frase bajo la cesta en (x, y) con width dp: de PhraseSp sp hacia abajo, el primer tamaño en el que cabe en
        // PhraseMaxLines líneas, nunca por debajo de 12 sp; si ni así cabe, −1 (no va). Con dos líneas, el ancho más estrecho que
        // las mantiene (equilibradas, sin palabra sola en la última), centrada. Devuelve el alto.
        private float PlacePhrase(string text, float x, float y, float width)
        {
            TextMeshProUGUI label = EnsurePhrase();
            if (!string.Equals(label.text, text, System.StringComparison.Ordinal))
            {
                label.text = text;
            }

            float scale = UiBuilder.TextScale;
            float min = UiBuilder.MinTextSp * scale;
            float size = PhraseSp * scale;
            int lines = int.MaxValue;
            Vector2 block = Vector2.zero;
            for (float sp = PhraseSp; sp >= UiBuilder.MinTextSp - 0.01f; sp -= 0.5f)
            {
                size = Mathf.Max(min, sp * scale);
                label.fontSize = size;
                block = label.GetPreferredValues(text, width, float.PositiveInfinity);
                lines = LinesOf(label, block.y);
                if (lines <= PhraseMaxLines)
                {
                    break;
                }
            }

            if (lines > PhraseMaxLines)
            {
                return -1f;
            }

            float w = width;
            if (lines >= 2)
            {
                float single = label.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
                w = Mathf.Min(width, BalancedWidth(label, text, width, single, lines, block.y) + FitSlackDp);
            }

            _phraseSize = size;
            _phraseLines = lines;
            float h = Mathf.Ceil(block.y);
            UiBuilder.Place(label.rectTransform, x + ((width - w) * 0.5f), y, w, h);
            return h;
        }

        private TextMeshProUGUI EnsurePhrase()
        {
            if (_phrase != null)
            {
                return _phrase;
            }

            _phrase = UiBuilder.Label("SlothLine", _panel, TextStyle.PlainBold, string.Empty, PhraseSp, Palette.FamilyWood.Deep, TextAlignmentOptions.Center);
            _phrase.textWrappingMode = TextWrappingModes.Normal;
            _phrase.enableAutoSizing = false;
            _phrase.gameObject.SetActive(false);
            return _phrase;
        }

        // Ancho más estrecho (dp) en el que text sigue en sus lines líneas (alto height): las líneas quedan parejas y no queda una
        // palabra sola en la última (como el bocadillo de la victoria, SlothVictory). El texto no se toca. Solo al medir.
        private static float BalancedWidth(TextMeshProUGUI label, string text, float inner, float single, int lines, float height)
        {
            float lo = Mathf.Min(inner, Mathf.Max(1f, (single / lines) - 1f));
            float hi = inner;
            for (int i = 0; i < 12 && hi - lo > 0.5f; i++)
            {
                float mid = 0.5f * (lo + hi);
                if (label.GetPreferredValues(text, mid, float.PositiveInfinity).y <= height + 0.5f)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid;
                }
            }

            return hi;
        }

        // Líneas que ocupa un bloque de height dp con el tamaño de ahora: el alto de una línea y el avance entre dos.
        private static int LinesOf(TextMeshProUGUI label, float height)
        {
            float one = label.GetPreferredValues("Ág", float.PositiveInfinity, float.PositiveInfinity).y;
            float two = label.GetPreferredValues("Ág\nÁg", float.PositiveInfinity, float.PositiveInfinity).y;
            float advance = Mathf.Max(1f, two - one);
            return 1 + Mathf.Max(0, Mathf.RoundToInt((height - one) / advance));
        }
    }
}
