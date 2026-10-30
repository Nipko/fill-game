#nullable enable
using System;
using System.Collections.Generic;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Las dos entradas del bloque B5 en el inicio, en un lienzo propio encima de él (orden <see cref="SortingOrder"/>, como la
    /// tarjeta del Reto) y colocadas junto a lo que ya pinta <see cref="LobbyScreen"/>, para no tocar sus archivos:
    /// <list type="bullet">
    /// <item><description><b>Entrada del Mercado</b> (06 §4.7; desde <c>max_level</c> ≥ 10): en la barra de abajo, a la izquierda de
    /// «Inicio», simétrica al Mapa [P]. Desde la observación 24 de la jornada 6 (decisión del dueño del 2026-10-30), el puesto del
    /// Mercado por código (<see cref="ProceduralArt.IconStall"/>: toldo a rayas, mostrador y fruta; antes, solo una semilla) y, debajo,
    /// sobre la barra, el rótulo «Mercado» (<see cref="MarketText.NavLabel"/>), dentro de su área táctil, para que se entienda que
    /// ahí se compra.</description></item>
    /// <item><description><b>Cántaro</b> (05 §4.1: «icono en el HUD desde el nivel 20»): desde D-050, una tinaja de barro
    /// (<see cref="ProceduralArt.DockJar"/>) apoyada en el muelle del inicio, a la izquierda, en el sitio que le guarda el inicio
    /// (<see cref="LobbyScreen.PiggyAnchorRect"/>), con lo que guarda en su placa a los pies; a 3.000, «¡Lleno!» con un punto. Un
    /// toque abre su tarjeta (05 §5: «≥ 1.000 y toque en el icono»).</description></item>
    /// </list>
    /// Con el enganche de <c>LobbyScreen</c> pasarían a su barra y a su cabecera. Sin asignaciones mientras nada cambia.
    /// </summary>
    internal sealed class MarketLobbyIcons
    {
        /// <summary>Orden del lienzo: encima del inicio (0) y junto a la tarjeta del Reto (5).</summary>
        public const int SortingOrder = 6;

        /// <summary>Ancho del icono del Mercado (dp), como el del Mapa.</summary>
        public const float MarketWidthDp = 64f;

        /// <summary>Hueco entre «Inicio» y el icono del Mercado (dp), como el del Mapa.</summary>
        public const float MarketGapDp = 12f;

        /// <summary>Área táctil mínima (07 §7.8): el icono baja hasta tener 48 dp, como el del Mapa (M3.3).</summary>
        public const float MinTouchDp = 48f;

        /// <summary>Tamaño del rótulo «Mercado» (sp, × escala): el tope de su autoajuste; el suelo es 12 sp × escala (02 §12).</summary>
        public const float LabelSp = 13f;

        /// <summary>Aire mínimo entre el rótulo y el borde de la pantalla o la pestaña «Inicio» (dp).</summary>
        public const float LabelMarginDp = 4f;

        /// <summary>Ancho del Cántaro (dp) [P]: la tinaja de barro apoyada en el muelle y la placa de la cifra (D-050, punto 6).</summary>
        public const float PiggyWidthDp = 64f;

        /// <summary>Alto del Cántaro (dp) [P]: la tinaja y, a sus pies, la cifra; más que el área táctil mínima (07 §7.8).</summary>
        public const float PiggyHeightDp = 74f;

        /// <summary>Hueco entre Ajustes y el Cántaro (dp) [P].</summary>
        public const float PiggyGapDp = 8f;

        private readonly OverlayCanvas _canvas;
        private readonly ReliefParts _market;
        private readonly Image _marketGlyph;
        private readonly TextMeshProUGUI _marketLabel;
        private readonly float _marketVisualDp;
        private readonly Button _piggy;
        private readonly Image _piggyJar;
        private readonly TextMeshProUGUI _piggyLabel;
        private readonly RectTransform _piggyDot;
        private readonly Vector3[] _corners = new Vector3[4];
        private Vector3 _homeSeen;
        private Vector3 _seedsSeen;
        private int _piggySeedsShown = -1;
        private int _piggyFullShown = -1;

        public MarketLobbyIcons(Transform host, ProceduralArt art, Action onMarket, Action onPiggy)
        {
            _canvas = new OverlayCanvas("MarketIconsCanvas", host, SortingOrder);
            _market = UiBuilder.Relief("MarketTab", _canvas.Root, art.CtaWood, onMarket);
            _marketVisualDp = art.CtaWood.HeightDp;
            // Observación 24 de la jornada 6: el puesto del Mercado (toldo, mostrador y fruta), no la semilla, y el rótulo debajo.
            _marketGlyph = UiBuilder.Image("Icon", _market.Face, art.IconStall, Color.white);
            _marketGlyph.preserveAspect = true;
            float faceH = art.CtaWood.FaceHeightDp;
            float glyphH = Mathf.Min(ProceduralArt.StallHeightDp, faceH - 6f);
            float glyphW = glyphH * (ProceduralArt.StallWidthDp / ProceduralArt.StallHeightDp);
            UiBuilder.Place(_marketGlyph.rectTransform, (MarketWidthDp - glyphW) * 0.5f, (faceH - glyphH) * 0.5f, glyphW, glyphH);
            _market.Relief.Bind(_market.Face, _market.Base, _market.FaceImage, art.CtaWood, _marketGlyph, null, null);
            _marketLabel = UiBuilder.Label("Label", _market.Button.transform, TextStyle.PillWood, MarketText.NavLabel, LabelSp, Palette.Cream, TextAlignmentOptions.Center);
            _marketLabel.textWrappingMode = TextWrappingModes.NoWrap; // una palabra: nunca se parte (02 §12)
            UiBuilder.AutoSize(_marketLabel, UiBuilder.MinTextSp, LabelSp);
            A11yLabels.Set(_market.Button, () => MarketText.NavLabel); // TalkBack: «Mercado», no un botón sin nombre
            _market.Button.gameObject.SetActive(false);

            _piggy = UiBuilder.BareButton("PiggyChip", _canvas.Root, onPiggy);
            // D-050: la tinaja de barro (ProceduralArt.DockJar) apoyada en el muelle y, a sus pies, la placa de madera con lo que guarda.
            _piggyJar = UiBuilder.Image("Jar", _piggy.transform, art.DockJar, Color.white);
            _piggyJar.preserveAspect = true;
            UiBuilder.Place(_piggyJar.rectTransform, (PiggyWidthDp - ProceduralArt.DockJarWidthDp) * 0.5f, 0f, ProceduralArt.DockJarWidthDp, ProceduralArt.DockJarHeightDp);
            float s = Mathf.Max(1f, UiBuilder.TextScale);
            float plateH = Mathf.Ceil(20f * s);
            Image plate = UiBuilder.Sliced("Plate", _piggy.transform, art.WoodPlate, Color.white);
            UiBuilder.Place(plate.rectTransform, 0f, PiggyHeightDp - plateH, PiggyWidthDp, plateH);
            _piggyLabel = UiBuilder.Label("Seeds", plate.transform, TextStyle.PillWood, string.Empty, 13f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_piggyLabel.rectTransform);
            _piggyLabel.rectTransform.offsetMin = new Vector2(4f, 0f);
            _piggyLabel.rectTransform.offsetMax = new Vector2(-4f, 0f);
            UiBuilder.AutoSize(_piggyLabel, UiBuilder.MinTextSp, 13f);
            _piggyDot = UiBuilder.Rect("FullDot", _piggy.transform);
            Image dot = UiBuilder.Image("Dot", _piggyDot, art.Disc, Palette.Heliconia);
            UiBuilder.Stretch(dot.rectTransform);
            UiBuilder.Place(_piggyDot, PiggyWidthDp - 16f, 2f, 14f, 14f);
            _piggyDot.gameObject.SetActive(false);
            _piggy.gameObject.SetActive(false);
        }

        /// <summary>Lienzo (capturas).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>El icono del Mercado está a la vista.</summary>
        public bool MarketShown => _market.Button.gameObject.activeSelf;

        /// <summary>El Cántaro está a la vista.</summary>
        public bool PiggyShown => _piggy.gameObject.activeSelf;

        /// <summary>Lo que dice el Cántaro.</summary>
        public string PiggyText => _piggyLabel.text;

        /// <summary>El punto de «¡Lleno!» está a la vista.</summary>
        public bool PiggyFullDotShown => _piggyDot.gameObject.activeSelf;

        public Button MarketButton => _market.Button;

        public Button PiggyButton => _piggy;

        public RectTransform MarketRect => (RectTransform)_market.Button.transform;

        /// <summary>El rótulo «Mercado» bajo el puesto (tests).</summary>
        public TextMeshProUGUI MarketLabel => _marketLabel;

        /// <summary>El icono del puesto (tests).</summary>
        public Image MarketGlyph => _marketGlyph;

        public RectTransform PiggyRect => (RectTransform)_piggy.transform;

        /// <summary>
        /// Enseña u oculta las dos entradas y las coloca junto a <paramref name="homeTab"/> y <paramref name="settings"/> del inicio
        /// (sus rectángulos en pantalla; <paramref name="settings"/> es, desde D-046, el sitio del Cántaro que guarda el inicio, a la
        /// izquierda del botón de Sonido: el Cántaro se pone encima). El Cántaro dice <paramref name="piggySeeds"/> o, lleno, «¡Lleno!» con el punto.
        /// </summary>
        public void Sync(bool market, bool piggy, int piggySeeds, bool piggyFull, RectTransform? homeTab, RectTransform? settings)
        {
            _canvas.Measure();
            bool marketShown = market && homeTab != null;
            if (_market.Button.gameObject.activeSelf != marketShown)
            {
                _market.Button.gameObject.SetActive(marketShown);
            }

            if (marketShown)
            {
                ToCanvas(homeTab!, out float left, out float top, out float height, out _);
                PlaceMarket(left, top, height);
            }

            bool piggyShown = piggy && settings != null;
            if (_piggy.gameObject.activeSelf != piggyShown)
            {
                _piggy.gameObject.SetActive(piggyShown);
            }

            if (!piggyShown)
            {
                return;
            }

            // En el sitio que le guarda el inicio (LobbyScreen.PiggyAnchorRect): desde D-050, la tinaja de barro apoyada en el muelle, a la
            // izquierda, entre el agua y «Jugar».
            ToCanvas(settings!, out float slotLeft, out float slotTop, out _, out _);
            UiBuilder.Place((RectTransform)_piggy.transform, slotLeft, slotTop, PiggyWidthDp, PiggyHeightDp);
            int full = piggyFull ? 1 : 0;
            if (piggySeeds != _piggySeedsShown || full != _piggyFullShown)
            {
                _piggySeedsShown = piggySeeds;
                _piggyFullShown = full;
                _piggyLabel.text = piggyFull ? MarketText.PiggyFull : Loc.Number(piggySeeds); // «¡Lleno!» a eco_piggy_cap (05 §4.1)
                _piggyDot.gameObject.SetActive(piggyFull);
            }
        }

        // La entrada del Mercado a la izquierda de «Inicio» (left, top y height: los de la pestaña «Inicio» en dp de este lienzo): el
        // puesto en su madera, a la altura de «Inicio», y el rótulo debajo, sobre la barra, centrado con el puesto; puede ser más ancho
        // que la madera, pero no llega al borde de la pantalla ni a «Inicio» (LabelMarginDp). El área táctil cubre los dos.
        private void PlaceMarket(float left, float top, float height)
        {
            float x = left - MarketGapDp - MarketWidthDp;
            _marketLabel.text = MarketText.NavLabel; // el idioma pudo cambiar en Ajustes
            float labelH = Mathf.Ceil((LabelSp + 5f) * UiBuilder.TextScale);
            float visual = Mathf.Max(height, _marketVisualDp);
            float labelY = visual - 4f;
            float buttonH = Mathf.Max(MinTouchDp, labelY + labelH);
            UiBuilder.Place((RectTransform)_market.Button.transform, x, top, MarketWidthDp, buttonH);
            // Centrado con el puesto si cabe; si el rótulo pide más (un idioma largo, la letra grande), se ensancha y se corre lo justo
            // para quedar entre el borde de la pantalla y «Inicio».
            float center = x + (MarketWidthDp * 0.5f);
            float spanMin = LabelMarginDp;
            float spanMax = Mathf.Max(spanMin + MarketWidthDp, left - LabelMarginDp);
            float half = Mathf.Max(MarketWidthDp * 0.5f, Mathf.Min(center - spanMin, spanMax - center));
            float wanted = Mathf.Ceil(_marketLabel.GetPreferredValues(_marketLabel.text).x) + 4f;
            float labelW = Mathf.Min(spanMax - spanMin, Mathf.Max(Mathf.Floor(2f * half), wanted));
            float labelX = Mathf.Clamp(center - (labelW * 0.5f), spanMin, spanMax - labelW);
            UiBuilder.Place(_marketLabel.rectTransform, labelX - x, labelY, labelW, labelH);
        }

        // El rectángulo de otro lienzo en las coordenadas de este (dp desde arriba a la izquierda), por su posición en el mundo:
        // vale igual con la pantalla real que con la simulada de los tests.
        private void ToCanvas(RectTransform target, out float left, out float top, out float height, out float right)
        {
            target.GetWorldCorners(_corners);
            Rect root = _canvas.Root.rect;
            Vector3 bottomLeft = _canvas.Root.InverseTransformPoint(_corners[0]);
            Vector3 topLeft = _canvas.Root.InverseTransformPoint(_corners[1]);
            Vector3 topRight = _canvas.Root.InverseTransformPoint(_corners[2]);
            left = bottomLeft.x - root.xMin;
            top = root.yMax - topLeft.y;
            height = topLeft.y - bottomLeft.y;
            right = topRight.x - root.xMin;
        }

        /// <summary>
        /// Las referencias del inicio (<paramref name="homeTab"/> y <paramref name="settings"/>) se movieron en pantalla desde la última
        /// vez (la escala del lienzo del inicio se aplica en su primer fotograma; la rotación): hay que recolocar. Sin asignaciones.
        /// </summary>
        public bool TargetsMoved(RectTransform? homeTab, RectTransform? settings)
        {
            bool moved = false;
            if (homeTab != null)
            {
                homeTab.GetWorldCorners(_corners);
                moved |= _corners[1] != _homeSeen;
                _homeSeen = _corners[1];
            }

            if (settings != null)
            {
                settings.GetWorldCorners(_corners);
                moved |= _corners[0] != _seedsSeen;
                _seedsSeen = _corners[0];
            }

            return moved;
        }

        /// <summary>Vuelve a medir la pantalla; true si cambió (hay que recolocar con <see cref="Sync"/>).</summary>
        public bool Remeasure()
        {
            return _canvas.Measure();
        }

        /// <summary>Oculta las dos.</summary>
        public void Hide()
        {
            if (_market.Button.gameObject.activeSelf)
            {
                _market.Button.gameObject.SetActive(false);
            }

            if (_piggy.gameObject.activeSelf)
            {
                _piggy.gameObject.SetActive(false);
            }
        }

        /// <summary>Botones visibles para <c>AF_LAYOUT</c> (sonda de desarrollo).</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            if (MarketShown)
            {
                ids.Add("market_tab");
                rects.Add(MarketRect);
            }

            if (PiggyShown)
            {
                ids.Add("piggy_chip");
                rects.Add(PiggyRect);
            }
        }
    }
}
