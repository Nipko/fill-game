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
    /// <item><description><b>Icono del Mercado</b> (06 §4.7: «iconos Mercado / Mapa del río», sin texto; desde <c>max_level</c> ≥ 10):
    /// en la barra de abajo, a la izquierda de «Inicio», simétrico al del Mapa [P]. Glifo provisional (la semilla) hasta el «puesto de
    /// Ver-o-Peso» de 07 §5 (arte del artista).</description></item>
    /// <item><description><b>Cántaro</b> (05 §4.1: «icono en el HUD desde el nivel 20»): bajo el saldo de semillas del inicio [P]
    /// (Q-B5-7), con lo que guarda; a 3.000, «¡Lleno!» con un punto. Un toque abre su tarjeta (05 §5: «≥ 1.000 y toque en el
    /// icono»).</description></item>
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

        /// <summary>Tamaño del Cántaro (dp) [P].</summary>
        public const float PiggyWidthDp = 92f;

        /// <summary>Alto del Cántaro (dp): el área táctil mínima (07 §7.8).</summary>
        public const float PiggyHeightDp = 48f;

        private readonly OverlayCanvas _canvas;
        private readonly ReliefParts _market;
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
            Image glyph = UiBuilder.Image("Icon", _market.Face, art.IconSeed, Color.white);
            glyph.preserveAspect = true;
            UiBuilder.Place(glyph.rectTransform, 16f, 6f, MarketWidthDp - 32f, 32f);
            _market.Relief.Bind(_market.Face, _market.Base, _market.FaceImage, art.CtaWood, glyph, null, null);
            _market.Button.gameObject.SetActive(false);

            _piggy = UiBuilder.BareButton("PiggyChip", _canvas.Root, onPiggy);
            Image plate = UiBuilder.Sliced("Plate", _piggy.transform, art.WoodPlate, Color.white);
            UiBuilder.Stretch(plate.rectTransform);
            _piggyJar = UiBuilder.Image("Jar", _piggy.transform, art.Disc, Palette.TinajaClay);
            UiBuilder.Place(_piggyJar.rectTransform, 8f, 10f, 28f, 28f);
            Image seed = UiBuilder.Image("Seed", _piggyJar.transform, art.IconSeed, Color.white);
            seed.preserveAspect = true;
            UiBuilder.Place(seed.rectTransform, 5f, 5f, 18f, 18f);
            _piggyLabel = UiBuilder.Label("Seeds", _piggy.transform, TextStyle.PillWood, string.Empty, 15f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Place(_piggyLabel.rectTransform, 38f, 8f, PiggyWidthDp - 44f, 32f);
            UiBuilder.AutoSize(_piggyLabel, UiBuilder.MinTextSp, 15f);
            _piggyDot = UiBuilder.Rect("FullDot", _piggy.transform);
            Image dot = UiBuilder.Image("Dot", _piggyDot, art.Disc, Palette.Heliconia);
            UiBuilder.Stretch(dot.rectTransform);
            UiBuilder.Place(_piggyDot, PiggyWidthDp - 12f, -2f, 14f, 14f);
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

        public RectTransform PiggyRect => (RectTransform)_piggy.transform;

        /// <summary>
        /// Enseña u oculta las dos entradas y las coloca junto a <paramref name="homeTab"/> y <paramref name="seeds"/> del inicio
        /// (sus rectángulos en pantalla). El Cántaro dice <paramref name="piggySeeds"/> o, lleno, el punto.
        /// </summary>
        public void Sync(bool market, bool piggy, int piggySeeds, bool piggyFull, RectTransform? homeTab, RectTransform? seeds)
        {
            _canvas.Measure();
            bool marketShown = market && homeTab != null;
            if (_market.Button.gameObject.activeSelf != marketShown)
            {
                _market.Button.gameObject.SetActive(marketShown);
            }

            if (marketShown)
            {
                ToCanvas(homeTab!, out float left, out float top, out float height);
                UiBuilder.Place((RectTransform)_market.Button.transform, left - MarketGapDp - MarketWidthDp, top, MarketWidthDp, height);
            }

            bool piggyShown = piggy && seeds != null;
            if (_piggy.gameObject.activeSelf != piggyShown)
            {
                _piggy.gameObject.SetActive(piggyShown);
            }

            if (!piggyShown)
            {
                return;
            }

            ToCanvas(seeds!, out float x, out float seedsTop, out float seedsHeight);
            float y = seedsTop + seedsHeight + 6f;
            UiBuilder.Place((RectTransform)_piggy.transform, x, y, PiggyWidthDp, PiggyHeightDp);
            if (piggySeeds != _piggySeedsShown)
            {
                _piggySeedsShown = piggySeeds;
                _piggyLabel.text = Loc.Number(piggySeeds);
            }

            int full = piggyFull ? 1 : 0;
            if (full != _piggyFullShown)
            {
                _piggyFullShown = full;
                _piggyDot.gameObject.SetActive(piggyFull);
            }
        }

        // El rectángulo de otro lienzo en las coordenadas de este (dp desde arriba a la izquierda), por su posición en el mundo:
        // vale igual con la pantalla real que con la simulada de los tests.
        private void ToCanvas(RectTransform target, out float left, out float top, out float height)
        {
            target.GetWorldCorners(_corners);
            Rect root = _canvas.Root.rect;
            Vector3 bottomLeft = _canvas.Root.InverseTransformPoint(_corners[0]);
            Vector3 topLeft = _canvas.Root.InverseTransformPoint(_corners[1]);
            left = bottomLeft.x - root.xMin;
            top = root.yMax - topLeft.y;
            height = topLeft.y - bottomLeft.y;
        }

        /// <summary>
        /// Las referencias del inicio (<paramref name="homeTab"/> y <paramref name="seeds"/>) se movieron en pantalla desde la última
        /// vez (la escala del lienzo del inicio se aplica en su primer fotograma; la rotación): hay que recolocar. Sin asignaciones.
        /// </summary>
        public bool TargetsMoved(RectTransform? homeTab, RectTransform? seeds)
        {
            bool moved = false;
            if (homeTab != null)
            {
                homeTab.GetWorldCorners(_corners);
                moved |= _corners[1] != _homeSeen;
                _homeSeen = _corners[1];
            }

            if (seeds != null)
            {
                seeds.GetWorldCorners(_corners);
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
