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
    /// «Sobre la Amazonía» en Ajustes (tarea F4; D-017 Q4-A: Ajustes › «Sobre la Amazonía» › «Frutas del río»; 07 §8.2: «una
    /// sección Sobre la Amazonía en Ajustes con la fuente de cada Dato del río»; propuesta de Frutas §2.3), en el hueco
    /// <see cref="AboutSlot"/>, al final de la lista: la cabecera «Sobre la Amazonía» (<c>settings_about_amazon</c> [P]),
    /// «Frutas del río» (<c>fact_sheet_title</c>) y una ficha por serie del calendario, en dos columnas y en su orden: el icono de
    /// la fruta y su nombre (<c>tint.&lt;tinte&gt;.name</c>, o <c>fact.&lt;río&gt;.name</c> en las series de río [P]); las que aún
    /// no empezaron (ninguna línea revelada), en silueta y con «Nivel {n}» (<c>fact_locked</c>), el nivel de su primera línea.
    /// Tocar una abre su ficha (<see cref="FruitChosen"/>; <see cref="FruitSheet"/>) encima de Ajustes, que sigue abierta debajo.
    /// Sin calendario, sin progreso o sin los textos de la ficha en el idioma de la UI (en release, sin aprobar;
    /// <see cref="UiText.FruitSheetVisible"/>) la sección no sale, y una serie sin nombre en ese idioma tampoco (D-018, Q10-A).
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Icono de la fruta en su ficha de la lista (dp).</summary>
        public const float FruitTileIconDp = 28f;

        /// <summary>Nombre (o «Nivel {n}») de una ficha de la lista (sp).</summary>
        public const float FruitTileSp = 14f;

        /// <summary>«Frutas del río», subtítulo bajo la cabecera de la sección (sp; revisión de F4): más pequeño y en tinta secundaria.</summary>
        public const float FruitsTitleSp = 14f;

        /// <summary>Hueco entre la cabecera «Sobre la Amazonía» y su subtítulo «Frutas del río» (dp; revisión de F4).</summary>
        public const float FruitsTitleGapDp = 4f;

        /// <summary>Líneas que puede pedir el nombre de una ficha en dos columnas; si alguna pide más, la lista va en una columna.</summary>
        public const int MaxTileLines = 2;

        /// <summary>Prefijo del id de una ficha de la lista en <c>AF_LAYOUT settings</c> (<c>fruit_acai</c>…).</summary>
        public const string FruitIdPrefix = "fruit_";

        private readonly List<FruitTile> _fruitTiles = new List<FruitTile>();
        private TextMeshProUGUI _aboutTitle = null!;
        private TextMeshProUGUI _fruitsTitle = null!;
        private int _fruitCount;

        /// <summary>Calendario de Frutas del río (lo da <c>GameBootstrap</c>; null = sin la sección).</summary>
        public FactCatalog? FruitCatalog { get; set; }

        /// <summary>Progreso con el que se decide qué series empezaron (lo da <c>GameBootstrap</c>; null = sin la sección).</summary>
        public IMapProgress? FruitProgress { get; set; }

        /// <summary>Al tocar la ficha de una serie: su sujeto (<c>acai</c>, <c>rio_napo</c>…). Lo fija <c>GameBootstrap</c>.</summary>
        public Action<string>? FruitChosen { get; set; }

        /// <summary>Se ve la sección «Sobre la Amazonía».</summary>
        public bool AboutShown => AboutSlot.gameObject.activeSelf;

        /// <summary>Cabecera de la sección («Sobre la Amazonía»).</summary>
        public string AboutTitle => _aboutTitle.text;

        /// <summary>«Frutas del río» bajo la cabecera.</summary>
        public string FruitsTitle => _fruitsTitle.text;

        /// <summary>Columnas de la lista: 2, o 1 si en dos algún nombre pidiera más de <see cref="MaxTileLines"/> líneas.</summary>
        public int FruitColumns { get; private set; } = 2;

        /// <summary>Fichas de la lista a la vista, en el orden del calendario.</summary>
        public int FruitCount => _fruitCount;

        /// <summary>Sujeto de la ficha <paramref name="i"/>.</summary>
        public string FruitSubject(int i)
        {
            return Tile(i).Subject;
        }

        /// <summary>Texto de la ficha <paramref name="i"/>: el nombre, o «Nivel {n}» si la serie no empezó.</summary>
        public string FruitLabel(int i)
        {
            return Tile(i).Label.text;
        }

        /// <summary>La serie de la ficha <paramref name="i"/> empezó (si no, va en silueta).</summary>
        public bool FruitStarted(int i)
        {
            return Tile(i).Started;
        }

        /// <summary>Botón de la ficha <paramref name="i"/>.</summary>
        public Button FruitButton(int i)
        {
            return Tile(i).Button;
        }

        /// <summary>Botón de la ficha de <paramref name="subject"/>, o null si no está a la vista.</summary>
        public Button? FruitButton(string subject)
        {
            for (int i = 0; i < _fruitCount; i++)
            {
                if (string.Equals(_fruitTiles[i].Subject, subject, StringComparison.Ordinal))
                {
                    return _fruitTiles[i].Button;
                }
            }

            return null;
        }

        /// <summary>Icono de la ficha <paramref name="i"/> (en silueta si la serie no empezó).</summary>
        public Image FruitIcon(int i)
        {
            return Tile(i).Icon;
        }

        // La cabecera y «Frutas del río», en el hueco (al construir la pantalla).
        private void BuildAbout()
        {
            _aboutTitle = UiBuilder.Label("AboutTitle", AboutSlot, TextStyle.PlainBold, UiText.SettingsAboutAmazon, SectionSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            UiBuilder.AutoSize(_aboutTitle, MinTextSp, SectionSp);
            _fruitsTitle = UiBuilder.Label("FruitsTitle", AboutSlot, TextStyle.Body, UiText.FactSheetTitle, FruitsTitleSp, FruitSheet.SoftInk, TextAlignmentOptions.MidlineLeft);
            UiBuilder.AutoSize(_fruitsTitle, MinTextSp, FruitsTitleSp);
        }

        // Las fichas de la lista con el calendario y el progreso de ahora (al abrir Ajustes).
        private void RefreshAbout()
        {
            _fruitCount = 0;
            FactCatalog? catalog = FruitCatalog;
            IMapProgress? progress = FruitProgress;
            if (catalog != null && progress != null && UiText.FruitSheetVisible)
            {
                IReadOnlyList<FactSeries> series = catalog.Series;
                for (int i = 0; i < series.Count; i++)
                {
                    FactSeries s = series[i];
                    string name = Loc.Fact(s.NameKey);
                    if (name.Length == 0)
                    {
                        continue; // sin nombre en el idioma de la UI (en release, sin aprobar): no sale
                    }

                    if (_fruitTiles.Count <= _fruitCount)
                    {
                        _fruitTiles.Add(new FruitTile(AboutSlot, _art, _fruitTiles.Count, subject => FruitChosen?.Invoke(subject)));
                    }

                    bool started = FruitSheet.UnlockedCount(s, progress) > 0;
                    _fruitTiles[_fruitCount].Bind(s, started ? name : UiText.FactLocked(s.FromLevel), started, _art);
                    _fruitCount++;
                }
            }

            for (int i = 0; i < _fruitTiles.Count; i++)
            {
                _fruitTiles[i].Button.gameObject.SetActive(i < _fruitCount);
            }

            AboutSlot.gameObject.SetActive(_fruitCount > 0);
        }

        // Coloca la sección desde y (contenido desplazable) y devuelve dónde acaba; sin la sección, y tal cual.
        private float LayoutAbout(float y, float w, float inner, float rowH, float titleH, float s)
        {
            if (!AboutShown)
            {
                return y;
            }

            float top = y;
            float local = 0f;
            PlaceTitle(_aboutTitle, 16f, local, inner, titleH, s);
            local += titleH + FruitsTitleGapDp;
            float subH = Mathf.Round((FruitsTitleSp * s) + 8f);
            _fruitsTitle.fontSizeMax = FruitsTitleSp * s;
            _fruitsTitle.fontSizeMin = MinTextSp;
            UiBuilder.Place(_fruitsTitle.rectTransform, 16f, local, inner, subH);
            local += subH + RowGapDp;

            // Dos columnas, todas las fichas del mismo alto (revisión de F4): el de la que más pide (el nombre crece en líneas,
            // nunca encoge), con el nombre centrado a lo alto. Si en dos columnas algún nombre pidiera más de MaxTileLines líneas
            // (la escala de texto alta, los idiomas largos), una sola columna [P]: ninguna ficha de tres líneas ni un «]» huérfano.
            float colW = Mathf.Floor((inner - RowGapDp) * 0.5f);
            FruitColumns = 2;
            for (int i = 0; i < _fruitCount; i++)
            {
                if (_fruitTiles[i].Lines(colW, s) > MaxTileLines)
                {
                    FruitColumns = 1;
                    colW = inner;
                    break;
                }
            }

            float h = rowH;
            for (int i = 0; i < _fruitCount; i++)
            {
                h = Mathf.Max(h, _fruitTiles[i].NeedDp(colW, s));
            }

            for (int i = 0; i < _fruitCount; i += FruitColumns)
            {
                _fruitTiles[i].Place(16f, local, colW, h, s);
                if (FruitColumns == 2 && i + 1 < _fruitCount)
                {
                    _fruitTiles[i + 1].Place(16f + inner - colW, local, colW, h, s);
                }

                _rowTops.Add(top + local);
                _rowHeights.Add(h);
                local += h + RowGapDp;
            }

            local -= RowGapDp;
            UiBuilder.Place(AboutSlot, 0f, top, w, local);
            return top + local;
        }

        /// <summary>Desplaza Ajustes hasta que la cabecera «Sobre la Amazonía» quede arriba del visor (capturas). Sin la sección o sin desplazamiento, nada.</summary>
        public void ScrollToAbout()
        {
            float range = _content.rect.height - _viewport.rect.height;
            if (!AboutShown || !_scroll.vertical || range <= 0f)
            {
                return;
            }

            float top = Mathf.Clamp(-AboutSlot.anchoredPosition.y - 8f, 0f, range);
            _scroll.verticalNormalizedPosition = 1f - (top / range);
            UpdateFades();
        }

        /// <summary>Posición del desplazamiento de Ajustes (1 arriba, 0 abajo; capturas: que dos capturas no enseñen lo mismo).</summary>
        public float ScrollPosition => _scroll.vertical ? _scroll.verticalNormalizedPosition : 1f;

        /// <summary>Alto de la ficha <paramref name="i"/> de la lista (dp): todas iguales.</summary>
        public float FruitTileHeight(int i)
        {
            return ((RectTransform)Tile(i).Button.transform).rect.height;
        }

        private IEnumerable<TextMeshProUGUI> AboutTexts()
        {
            if (!AboutShown)
            {
                yield break;
            }

            yield return _aboutTitle;
            yield return _fruitsTitle;
            for (int i = 0; i < _fruitCount; i++)
            {
                yield return _fruitTiles[i].Label;
            }
        }

        private int AboutLayoutCount => AboutShown ? _fruitCount : 0;

        private void AboutLayoutIds(string[] ids, RectTransform[] rects, int k)
        {
            for (int i = 0; i < AboutLayoutCount; i++, k++)
            {
                ids[k] = FruitIdPrefix + _fruitTiles[i].Subject;
                rects[k] = (RectTransform)_fruitTiles[i].Button.transform;
            }
        }

        private FruitTile Tile(int i)
        {
            if (i < 0 || i >= _fruitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i), i, "La lista enseña " + _fruitCount + " frutas.");
            }

            return _fruitTiles[i];
        }

        // Una ficha de la lista: placa crema con el borde de madera de las opciones, el icono y el nombre; toda la placa es el
        // área táctil (≥ 48 dp).
        private sealed class FruitTile
        {
            private const float PadDp = 10f;
            private const float IconGapDp = 8f;
            private readonly Image _iconBack;

            public FruitTile(Transform parent, ProceduralArt art, int index, Action<string> onClick)
            {
                Button = UiBuilder.BareButton("Fruit_" + index, parent, () => onClick(Subject));
                RectTransform root = (RectTransform)Button.transform;
                Image border = UiBuilder.Sliced("Border", root, art.RoundedRect, OptionBorder);
                UiBuilder.Stretch(border.rectTransform);
                border.pixelsPerUnitMultiplier = 24f / OptionRadiusDp;
                Image plate = UiBuilder.Sliced("Plate", root, art.RoundedRect, OptionPlate);
                UiBuilder.Stretch(plate.rectTransform);
                plate.rectTransform.offsetMin = new Vector2(OptionBorderDp, OptionBorderDp);
                plate.rectTransform.offsetMax = new Vector2(-OptionBorderDp, -OptionBorderDp);
                plate.pixelsPerUnitMultiplier = 24f / (OptionRadiusDp - OptionBorderDp);
                _iconBack = UiBuilder.Image("IconBack", root, art.Disc, FruitSheet.IconBack);
                Icon = UiBuilder.Image("Icon", root, null, Color.white);
                Icon.preserveAspect = true;
                Label = UiBuilder.Label("Label", root, TextStyle.PlainBold, string.Empty, FruitTileSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
                Label.textWrappingMode = TextWrappingModes.Normal;
            }

            public Button Button { get; }

            public Image Icon { get; }

            public TextMeshProUGUI Label { get; }

            public string Subject { get; private set; } = string.Empty;

            public bool Started { get; private set; }

            public void Bind(FactSeries series, string text, bool started, ProceduralArt art)
            {
                Subject = series.Subject;
                Started = started;
                Label.text = text;
                int tint = FactLine.TintOf(series.Subject);
                Sprite? fruit = tint > 0 ? art.FruitFlight(tint) : null;
                Icon.sprite = fruit ?? (series.Kind == FactSeriesKind.River ? art.Drop : null);
                Icon.color = !started ? FruitSheet.SilhouetteColor : fruit != null ? Color.white : FruitSheet.RiverDrop;
                Icon.gameObject.SetActive(Icon.sprite != null);
                _iconBack.gameObject.SetActive(Icon.sprite != null);
            }

            // Líneas que pide el nombre en una columna de colW dp, a 14 sp por la escala.
            public int Lines(float colW, float s)
            {
                Label.enableAutoSizing = false;
                Label.fontSize = FruitTileSp * s;
                float lw = LabelWidth(colW);
                float all = Label.GetPreferredValues(Label.text, lw, float.PositiveInfinity).y;
                float one = Label.GetPreferredValues("Ág", float.PositiveInfinity, float.PositiveInfinity).y;
                return one > 0f ? Mathf.Max(1, Mathf.RoundToInt(all / one)) : 1;
            }

            // Alto que pide el nombre en una columna de colW dp (más el relleno), a 14 sp por la escala.
            public float NeedDp(float colW, float s)
            {
                Label.enableAutoSizing = false;
                Label.fontSize = FruitTileSp * s;
                float lw = LabelWidth(colW);
                return Mathf.Ceil(Label.GetPreferredValues(Label.text, lw, float.PositiveInfinity).y) + (2f * PadDp);
            }

            public void Place(float x, float y, float w, float h, float s)
            {
                UiBuilder.Place((RectTransform)Button.transform, x, y, w, h);
                float iy = Mathf.Round((h - FruitTileIconDp) * 0.5f);
                float back = FruitTileIconDp - 4f;
                UiBuilder.Place(_iconBack.rectTransform, PadDp + 2f, iy + 2f, back, back);
                UiBuilder.Place(Icon.rectTransform, PadDp, iy, FruitTileIconDp, FruitTileIconDp);
                Label.enableAutoSizing = false;
                Label.fontSize = FruitTileSp * s;
                float lx = PadDp + FruitTileIconDp + IconGapDp;
                UiBuilder.Place(Label.rectTransform, lx, PadDp * 0.5f, LabelWidth(w), h - PadDp);
            }

            private static float LabelWidth(float w)
            {
                return w - PadDp - FruitTileIconDp - IconGapDp - PadDp;
            }
        }
    }
}
