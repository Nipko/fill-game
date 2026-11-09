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
    /// El estreno de la Liga del río (añadido de D-046 del 2026-11-06; D-059, añadido del 2026-10-31, punto 3: «que se anuncie al
    /// jugador y que no pase desapercibida, con una presentación más vistosa»): una pantalla completa, en su propio lienzo, que sale
    /// una sola vez por jugador. Sobre un velo índigo casi opaco, los rayos de luz que giran despacio, el halo de oro, el emblema de
    /// la Liga (<see cref="ProceduralArt.LeagueEmblem"/>: la copa de oro en el medallón índigo con su corona de hojas) que entra con
    /// escala y rebote, destellos, una lluvia ligera de hojas, pétalos y plumas, el título «¡Llegó la Liga del río!», la línea que
    /// explica qué es, «Ver la Liga» (Hoja, el principal) y «Luego» (enlace). Suena su fanfarria de la 1.1: el remate
    /// <c>sting_league_debut</c> (con Música) y <c>sfx_confetti</c> (con Sonido; <c>docs/audio/encargo-fanfarria-liga.md</c> §3).
    /// </summary>
    /// <remarks>
    /// <para>Sin X: el velo no la cierra; el Atrás de Android es «Luego» (<c>GameBootstrap.PressLeagueDebutBack</c>). Los botones
    /// reciben toques desde que se ven (<see cref="ButtonsFromMs"/>), así un toque impaciente no abre la Liga sin leerla.</para>
    /// <para>Con «Reducir movimiento» (07 §7.8) sale ya entera y quieta: sin escala, rebote, giro, pulso, destellos que parpadean
    /// ni lluvia. TalkBack lee el lienzo de arriba (el velo tapa lo de debajo) y quien la abre anuncia el título. Textos ≥ 12 sp
    /// con la escala de texto hasta el 130 %: la columna crece y, si no cabe, el emblema se encoge.</para>
    /// <para>El arte se pide al abrirla y se libera al cerrarla (<see cref="ProceduralArt.ReleaseLeagueDebutArt"/>). Sin
    /// asignaciones por fotograma: <see cref="Tick"/> solo mueve transformadas y colores; al terminar la entrada solo giran los
    /// rayos y late el halo.</para>
    /// </remarks>
    internal sealed class LeagueDebutScreen
    {
        /// <summary>
        /// Orden del lienzo: encima de la victoria, de la tarjeta de oferta (28), del Mercado (30), de la libreta (32) y del
        /// pre-prompt de push (34, que nunca sale a la vez); por debajo de la Liga (38), de Ajustes (40), del Mapa (45), de la hoja
        /// de compra (50), de un anuncio (60) y del consentimiento (65).
        /// </summary>
        public const int SortingOrder = 35;

        /// <summary>Opacidad final del velo índigo [P].</summary>
        public const float BackdropAlpha = 0.94f;

        /// <summary>Fundido del velo (ms) [P].</summary>
        public const float BackdropMs = 220f;

        /// <summary>El emblema empieza a entrar (ms) [P].</summary>
        public const float EmblemFromMs = 80f;

        /// <summary>Entrada del emblema: escala con rebote (ms) [P].</summary>
        public const float EmblemMs = 560f;

        /// <summary>El título empieza a entrar (ms) [P].</summary>
        public const float TitleFromMs = 420f;

        /// <summary>La línea empieza a entrar (ms) [P].</summary>
        public const float BodyFromMs = 560f;

        /// <summary>Los botones empiezan a entrar y reciben toques (ms) [P].</summary>
        public const float ButtonsFromMs = 700f;

        /// <summary>Fundido de cada texto o botón (ms) [P].</summary>
        public const float FadeMs = 300f;

        /// <summary>Fin de la entrada (ms): desde aquí solo giran los rayos y late el halo.</summary>
        public const float EnterMs = ButtonsFromMs + FadeMs;

        /// <summary>Fin de la lluvia de hojas (ms) [P]: cae una vez.</summary>
        public const float ConfettiEndMs = 4200f;

        /// <summary>Giro de los rayos (grados por segundo) [P].</summary>
        public const float RaysDegPerSecond = 7f;

        /// <summary>Piezas de la lluvia [P].</summary>
        public const int ConfettiCount = 18;

        /// <summary>Ancho máximo de la columna de texto y botones (dp).</summary>
        public const float ColumnMaxDp = 320f;

        /// <summary>Cuerpo del título (sp).</summary>
        public const float TitleSp = 30f;

        /// <summary>Cuerpo de la línea (sp).</summary>
        public const float BodySp = 17f;

        private static readonly Color BackdropColor = new Color(0.06f, 0.08f, 0.20f, 0f);
        private static readonly Color Gold = new Color(0.99f, 0.82f, 0.40f, 1f);

        private readonly ProceduralArt _art;
        private readonly OverlayCanvas _canvas;
        private readonly Image _backdrop;
        private readonly Image _rays;
        private readonly Image _glow;
        private readonly Image _emblem;
        private readonly Image[] _sparkles = new Image[4];
        private readonly RectTransform _confettiRoot;
        private readonly Image[] _confetti = new Image[ConfettiCount];
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _body;
        private readonly ReliefParts _open;
        private readonly TextMeshProUGUI _openLabel;
        private readonly CanvasGroup _openGroup;
        private readonly Button _later;
        private readonly TextMeshProUGUI _laterLabel;
        private readonly CanvasGroup _laterGroup;
        private readonly Action _onOpen;
        private readonly Action _onLater;
        private float _clockMs;
        private bool _reduceMotion;
        private float _emblemDp = ProceduralArt.LeagueEmblemDp;
        private float _titleY;
        private float _bodyY;

        public LeagueDebutScreen(Transform host, ProceduralArt art, Action onOpen, Action onLater)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _onOpen = onOpen ?? throw new ArgumentNullException(nameof(onOpen));
            _onLater = onLater ?? throw new ArgumentNullException(nameof(onLater));
            _canvas = new OverlayCanvas("LeagueDebutCanvas", host, SortingOrder);
            _backdrop = UiBuilder.Image("Backdrop", _canvas.Root, null, BackdropColor, true); // recibe los toques: lo de debajo no se toca
            ScreenColumn.Bleed(_backdrop.rectTransform); // de borde a borde (añadido de D-059)
            _rays = UiBuilder.Image("Rays", _canvas.Root, null, new Color(Gold.r, Gold.g, Gold.b, 0f));
            _glow = UiBuilder.Image("Glow", _canvas.Root, art.Glow, new Color(Gold.r, Gold.g, Gold.b, 0f));
            _confettiRoot = UiBuilder.Rect("Confetti", _canvas.Root);
            for (int i = 0; i < ConfettiCount; i++)
            {
                Sprite piece = (i % 3) == 0 ? art.LeafBit : ((i % 3) == 1 ? art.Petal : art.Feather);
                Color tint = (i % 3) == 2 ? ((i % 2) == 0 ? Palette.FeatherRed : Palette.FeatherBlue) : Color.white;
                tint.a = 0f;
                _confetti[i] = UiBuilder.Image("Piece" + i, _confettiRoot, piece, tint);
                _confetti[i].preserveAspect = true;
            }

            _emblem = UiBuilder.Image("Emblem", _canvas.Root, null, Color.white);
            _emblem.preserveAspect = true;
            for (int i = 0; i < _sparkles.Length; i++)
            {
                _sparkles[i] = UiBuilder.Image("Sparkle" + i, _canvas.Root, null, new Color(1f, 0.97f, 0.85f, 0f));
            }

            _title = UiBuilder.Label("Title", _canvas.Root, TextStyle.Ribbon, string.Empty, TitleSp, Palette.Cream, TextAlignmentOptions.Center);
            _title.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_title, 20f, TitleSp);
            _body = ModalKit.Line("Body", _canvas.Root, string.Empty, BodySp, Palette.Cream);
            _open = ModalKit.Cta("OpenButton", _canvas.Root, art.CtaLeaf, TextStyle.CtaLeaf, string.Empty, 21f, () => Answer(true), out _openLabel);
            _openGroup = _open.Button.gameObject.AddComponent<CanvasGroup>();
            _later = ModalKit.Link("LaterButton", _canvas.Root, string.Empty, () => Answer(false), out _laterLabel);
            _laterLabel.color = Palette.Cream;
            _laterLabel.fontSizeMin = UiBuilder.MinTextSp;
            _laterGroup = _later.gameObject.AddComponent<CanvasGroup>();
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Está abierta.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Lienzo (capturas).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>Ancho de la pantalla en dp (el del lienzo con su escala).</summary>
        public float ScreenWidthDp => _canvas.Width;

        /// <summary>Alto de la pantalla en dp.</summary>
        public float ScreenHeightDp => _canvas.Height;

        /// <summary>Cuántas veces se abrió (tests).</summary>
        public int ShownCount { get; private set; }

        /// <summary>Se abrió con «Reducir movimiento».</summary>
        public bool ReduceMotion => _reduceMotion;

        /// <summary>Tiempo desde que se abrió (ms).</summary>
        public float ClockMs => _clockMs;

        /// <summary>La entrada terminó (todo a la vista y quieto, salvo los rayos y el halo).</summary>
        public bool Settled => _reduceMotion || _clockMs >= EnterMs;

        public string TitleText => _title.text;

        public string BodyText => _body.text;

        public string OpenText => _openLabel.text;

        public string LaterText => _laterLabel.text;

        /// <summary>«Ver la Liga».</summary>
        public Button OpenButton => _open.Button;

        /// <summary>«Luego».</summary>
        public Button LaterButton => _later;

        /// <summary>El título (tests).</summary>
        public RectTransform TitleRect => _title.rectTransform;

        /// <summary>La línea (tests).</summary>
        public RectTransform BodyRect => _body.rectTransform;

        /// <summary>El emblema (tests: dentro de la pantalla, escala).</summary>
        public RectTransform EmblemRect => _emblem.rectTransform;

        /// <summary>Escala de ahora del emblema.</summary>
        public float EmblemScale => _emblem.rectTransform.localScale.x;

        /// <summary>Ángulo de ahora de los rayos (grados).</summary>
        public float RaysAngle => _rays.rectTransform.localEulerAngles.z;

        /// <summary>Piezas de la lluvia a la vista ahora.</summary>
        public int ConfettiVisible
        {
            get
            {
                int n = 0;
                if (!_confettiRoot.gameObject.activeSelf)
                {
                    return 0;
                }

                for (int i = 0; i < _confetti.Length; i++)
                {
                    if (_confetti[i].gameObject.activeSelf && _confetti[i].color.a > 0.01f)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Los botones reciben toques (desde <see cref="ButtonsFromMs"/>; con Reducir movimiento, en el acto).</summary>
        public bool ButtonsLive => _openGroup.blocksRaycasts;

        /// <summary>Todos los textos (tests: ≥ 12 sp y sin recortes).</summary>
        public IEnumerable<TextMeshProUGUI> Texts()
        {
            yield return _title;
            yield return _body;
            yield return _openLabel;
            yield return _laterLabel;
        }

        /// <summary>Abre el estreno (abierto, no hace nada). Pide el arte y deja la entrada en t = 0 (o entera con <paramref name="reduceMotion"/>).</summary>
        public void Open(bool reduceMotion)
        {
            if (Visible)
            {
                return;
            }

            ShownCount++;
            _reduceMotion = reduceMotion;
            _clockMs = 0f;
            _canvas.Canvas.gameObject.SetActive(true);
            _canvas.Measure();
            _emblem.sprite = _art.LeagueEmblem;
            _rays.sprite = _art.LeagueRays;
            _sparkles[0].sprite = _art.StarSparkle;
            for (int i = 1; i < _sparkles.Length; i++)
            {
                _sparkles[i].sprite = _sparkles[0].sprite;
            }

            _title.text = LeagueText.DebutTitle;
            _body.text = LeagueText.DebutBody;
            _openLabel.text = LeagueText.DebutOpen;
            _laterLabel.text = LeagueText.DebutLater;
            Layout();
            Apply();
        }

        /// <summary>Cierra sin responder (borrado de cuenta, escena que se va). Cerrada, no hace nada.</summary>
        public void Close()
        {
            if (!Visible)
            {
                return;
            }

            _canvas.Canvas.gameObject.SetActive(false);
            _emblem.sprite = null;
            _rays.sprite = null;
            _art.ReleaseLeagueDebutArt();
        }

        /// <summary>«Ver la Liga» (<paramref name="open"/>) o «Luego»: cierra y avisa a quien la abrió. Cerrada, no hace nada.</summary>
        public void Answer(bool open)
        {
            if (!Visible)
            {
                return;
            }

            Close();
            if (open)
            {
                _onOpen();
            }
            else
            {
                _onLater();
            }
        }

        /// <summary>Avanza la animación <paramref name="dtMs"/> ms y, si cambió la pantalla, vuelve a colocar.</summary>
        public void Tick(float dtMs)
        {
            if (!Visible)
            {
                return;
            }

            if (_canvas.Measure())
            {
                Layout();
            }

            _clockMs += Mathf.Max(0f, dtMs);
            Apply();
        }

        /// <summary>Botones visibles para <c>AF_LAYOUT</c> (sonda de desarrollo).</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            if (!Visible)
            {
                return;
            }

            ids.Add("league_debut_open");
            rects.Add((RectTransform)_open.Button.transform);
            ids.Add("league_debut_later");
            rects.Add((RectTransform)_later.transform);
        }

        private void Layout()
        {
            float w = _canvas.Width;
            float h = _canvas.Height - _canvas.SafeTop - _canvas.SafeBottom;
            float column = Mathf.Min(ColumnMaxDp, w - 40f);
            float x = Mathf.Round((w - column) * 0.5f);

            float titleH = Mathf.Ceil(UiBuilder.MinTextHeight(_title, column));
            float bodyH = ModalKit.PlaceLine(_body, x, 0f, column, 48f);
            float ctaH = ModalKit.HeightOf(_open);
            float linkH = Mathf.Max(48f, Mathf.Ceil(UiBuilder.MinTextHeight(_laterLabel, column)) + 12f);
            float textBlock = 22f + titleH + 10f + bodyH + 28f + ctaH + 6f + linkH;

            // El emblema se encoge si la columna no cabe (al 130 % en 640 dp), nunca por debajo de 88 dp.
            _emblemDp = Mathf.Clamp(h - textBlock - 48f, 88f, ProceduralArt.LeagueEmblemDp);
            float total = _emblemDp + textBlock;
            float y = _canvas.SafeTop + Mathf.Max(16f, Mathf.Round((h - total) * 0.42f));

            float cx = w * 0.5f;
            float emblemCy = y + (_emblemDp * 0.5f);
            Center(_emblem.rectTransform, cx, emblemCy, _emblemDp, _emblemDp);
            float raysDp = Mathf.Max(ProceduralArt.LeagueRaysDp, Mathf.Max(w, h) * 0.9f);
            Center(_rays.rectTransform, cx, emblemCy, raysDp, raysDp);
            Center(_glow.rectTransform, cx, emblemCy, _emblemDp * 2.1f, _emblemDp * 2.1f);
            float r = _emblemDp * 0.56f;
            Center(_sparkles[0].rectTransform, cx - (r * 0.80f), emblemCy - (r * 0.70f), 26f, 26f);
            Center(_sparkles[1].rectTransform, cx + (r * 0.92f), emblemCy - (r * 0.30f), 20f, 20f);
            Center(_sparkles[2].rectTransform, cx + (r * 0.55f), emblemCy + (r * 0.85f), 18f, 18f);
            Center(_sparkles[3].rectTransform, cx - (r * 0.98f), emblemCy + (r * 0.40f), 16f, 16f);

            y += _emblemDp + 22f;
            _titleY = y;
            UiBuilder.Place(_title.rectTransform, x, y, column, titleH);
            y += titleH + 10f;
            _bodyY = y;
            ModalKit.PlaceLine(_body, x, y, column, 48f);
            y += bodyH + 28f;
            float ctaW = Mathf.Min(296f, column);
            ModalKit.Place(_open, Mathf.Round((w - ctaW) * 0.5f), y, ctaW);
            y += ctaH + 6f;
            UiBuilder.Place((RectTransform)_later.transform, Mathf.Round((w - ctaW) * 0.5f), y, ctaW, linkH);
            UiBuilder.Stretch(_confettiRoot);
        }

        private static void Center(RectTransform rt, float cx, float cyTop, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, -cyTop);
            rt.sizeDelta = new Vector2(w, h);
        }

        // Pinta el instante _clockMs (o el final, con Reducir movimiento). Sin asignaciones.
        private void Apply()
        {
            float t = _reduceMotion ? float.MaxValue : _clockMs;

            _backdrop.color = WithAlpha(BackdropColor, BackdropAlpha * Ease(t, 0f, BackdropMs));

            float emblemIn = Ease(t, EmblemFromMs, EmblemMs);
            float emblemU = Mathf.Clamp01((t - EmblemFromMs) / EmblemMs);
            float scale = _reduceMotion ? 1f : Mathf.LerpUnclamped(0.25f, 1f, BackOut(emblemU));
            _emblem.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _emblem.color = WithAlpha(Color.white, Mathf.Clamp01(emblemIn * 2.5f));

            float glowPulse = _reduceMotion ? 1f : 1f + (0.06f * Mathf.Sin(_clockMs * 0.0035f));
            _glow.rectTransform.localScale = new Vector3(glowPulse * scale, glowPulse * scale, 1f);
            _glow.color = WithAlpha(Gold, 0.55f * emblemIn);

            _rays.color = WithAlpha(Gold, 0.32f * Ease(t, 200f, 500f));
            float angle = _reduceMotion ? 0f : Mathf.Repeat(_clockMs * 0.001f * RaysDegPerSecond, 360f);
            _rays.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);

            for (int i = 0; i < _sparkles.Length; i++)
            {
                float a;
                float s;
                if (_reduceMotion)
                {
                    a = 0.8f;
                    s = 1f;
                }
                else
                {
                    float phase = Mathf.Repeat(((_clockMs - EmblemFromMs - EmblemMs) * 0.0011f) + (i * 0.27f), 1f);
                    float wave = _clockMs < EmblemFromMs + EmblemMs ? 0f : Mathf.Sin(phase * Mathf.PI);
                    a = wave * wave;
                    s = 0.5f + (0.6f * wave);
                }

                _sparkles[i].color = WithAlpha(_sparkles[i].color, a);
                _sparkles[i].rectTransform.localScale = new Vector3(s, s, 1f);
            }

            FadeUp(_title, t, TitleFromMs, _titleY);
            FadeUp(_body, t, BodyFromMs, _bodyY);
            float buttons = Ease(t, ButtonsFromMs, FadeMs);
            _openGroup.alpha = buttons;
            _laterGroup.alpha = buttons;
            bool live = t >= ButtonsFromMs;
            _openGroup.blocksRaycasts = live;
            _laterGroup.blocksRaycasts = live;

            ApplyConfetti();
        }

        private void FadeUp(TextMeshProUGUI text, float t, float fromMs, float y)
        {
            float k = Ease(t, fromMs, FadeMs);
            text.alpha = k;
            RectTransform rt = text.rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -(y + ((1f - k) * 14f)));
        }

        // La lluvia: cada pieza cae una vez desde arriba con su vaivén y su giro; con Reducir movimiento no hay lluvia.
        private void ApplyConfetti()
        {
            bool on = !_reduceMotion && _clockMs < ConfettiEndMs;
            if (_confettiRoot.gameObject.activeSelf != on)
            {
                _confettiRoot.gameObject.SetActive(on);
            }

            if (!on)
            {
                return;
            }

            float w = _canvas.Width;
            float h = _canvas.Height;
            for (int i = 0; i < _confetti.Length; i++)
            {
                // Valores fijos por pieza (sin azar: la misma lluvia cada vez).
                float lane = ((i * 37) % 100) / 100f;
                float delay = 250f + (((i * 53) % 17) * 70f);
                float fall = 2200f + (((i * 29) % 11) * 120f);
                float size = 14f + ((i * 7) % 9);
                float u = (_clockMs - delay) / fall;
                Image piece = _confetti[i];
                if (u <= 0f || u >= 1f)
                {
                    piece.color = WithAlpha(piece.color, 0f);
                    continue;
                }

                float sway = Mathf.Sin((u * 6.5f) + i) * 18f;
                float x = (lane * (w + 40f)) - 20f + sway;
                float y = -30f + (u * (h + 60f));
                RectTransform rt = piece.rectTransform;
                Center(rt, x, y, size, size);
                rt.localEulerAngles = new Vector3(0f, 0f, (u * 360f * (i % 2 == 0 ? 1.4f : -1.1f)) + (i * 23f));
                float fade = Mathf.Clamp01(u * 8f) * Mathf.Clamp01((1f - u) * 5f);
                piece.color = WithAlpha(piece.color, 0.9f * fade);
            }
        }

        private static float Ease(float t, float from, float duration)
        {
            float u = Mathf.Clamp01((t - from) / duration);
            return 1f - ((1f - u) * (1f - u) * (1f - u));
        }

        // Rebote al final (easeOutBack): pasa un poco de 1 y vuelve.
        private static float BackOut(float u)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float v = u - 1f;
            return 1f + (c3 * v * v * v) + (c1 * v * v);
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
