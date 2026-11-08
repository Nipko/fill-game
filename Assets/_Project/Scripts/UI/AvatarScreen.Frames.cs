#nullable enable
using System;
using System.Collections.Generic;
using AF.LiveOps;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// La fila de los marcos de país de «Tu avatar» (Q-M6 de D-033, «se enseña en la 1.1», con su añadido del 2026-11-08) [P]: solo si el
    /// jugador ya ganó alguno, encima de la rejilla de los animales y dentro del mismo desplazamiento, con el subtítulo «Marcos de país»
    /// y, debajo, «Animales». Una celda «Sin marco» y una por marco ganado, por mundo; cada una, el avatar elegido (del atlas) con ese
    /// marco encima, el nombre del mundo debajo, el aro de hoja y la ✓ en la puesta y la chapa «Nuevo» en la que no se vio. Tocar una la
    /// pone en el acto, sin confirmar, como un avatar (lo hace <c>GameBootstrap</c> con <see cref="FrameTapped"/>). Los marcos de las
    /// celdas son texturas de la pantalla (<see cref="FramePx"/> px) que se sueltan al cerrarla.
    /// </summary>
    internal sealed partial class AvatarScreen
    {
        /// <summary>Id de la celda «Sin marco» en la sonda y en los tests.</summary>
        public const string FrameNoneId = "frame_none";

        private const float SectionSp = 15f;

        private readonly List<FrameCell> _frameCells = new List<FrameCell>(AvatarFrames.Count + 1);
        private readonly Dictionary<int, Sprite> _frameSprites = new Dictionary<int, Sprite>();
        private TextMeshProUGUI _framesTitle = null!;
        private TextMeshProUGUI _animalsTitle = null!;
        private ProceduralArt _frameArt = null!;
        private AvatarFrameService? _frames;

        /// <summary>Los marcos del jugador (lo pone <c>GameBootstrap</c> antes de abrir; null sin marcos).</summary>
        public AvatarFrameService? Frames
        {
            get => _frames;
            set => _frames = value;
        }

        /// <summary>Lado en px de los marcos de las celdas (64 dp a la densidad de la pantalla).</summary>
        public int FramePx { get; set; } = 192;

        /// <summary>Al tocar una celda de marco: su id, o null en «Sin marco».</summary>
        public Action<string?>? FrameTapped { get; set; }

        /// <summary>Se ve la fila de los marcos.</summary>
        public bool FramesShown => _framesTitle.gameObject.activeSelf;

        /// <summary>Las celdas de marco a la vista, en orden (<see cref="FrameNoneId"/> y los <c>frame_wNN</c>).</summary>
        public IReadOnlyList<string> ShownFrameIds
        {
            get
            {
                var ids = new List<string>(_frameCells.Count);
                for (int i = 0; i < _frameCells.Count; i++)
                {
                    if (_frameCells[i].Root.gameObject.activeSelf)
                    {
                        ids.Add(_frameCells[i].Id);
                    }
                }

                return ids;
            }
        }

        /// <summary>El botón de la celda de marco <paramref name="id"/> (<see cref="FrameNoneId"/> o <c>frame_wNN</c>).</summary>
        public Button FrameCellButton(string id) => FrameCellOf(id).Button;

        /// <summary>El recuadro de la celda de marco <paramref name="id"/>.</summary>
        internal RectTransform FrameCellRect(string id) => FrameCellOf(id).Root;

        /// <summary>La celda de marco <paramref name="id"/> es la puesta.</summary>
        public bool FrameCellChosen(string id) => FrameCellOf(id).ChosenShown;

        /// <summary>La celda de marco <paramref name="id"/> lleva «Nuevo».</summary>
        public bool FrameCellNew(string id) => FrameCellOf(id).NewShown;

        /// <summary>Lo que lee TalkBack de la celda de marco <paramref name="id"/>.</summary>
        public string FrameCellA11y(string id) => FrameCellOf(id).Reading;

        /// <summary>El marco que pinta la celda <paramref name="id"/> (null en «Sin marco»).</summary>
        internal Sprite? FrameCellOverlay(string id) => FrameCellOf(id).OverlaySprite;

        /// <summary>El subtítulo «Animales» (tests de composición).</summary>
        internal RectTransform AnimalsTitleRect => _animalsTitle.rectTransform;

        /// <summary>Se puso otro marco: el rebote (o el fundido) del retrato de la cabecera, como al elegir un avatar.</summary>
        public void FrameChosen()
        {
            _chooseMs = 0f;
            _balloonCell = -1;
            _balloon.gameObject.SetActive(false);
        }

        private void BuildFrames(ProceduralArt art)
        {
            _frameArt = art;
            _framesTitle = UiBuilder.Label("FramesTitle", _content, TextStyle.PlainBold, string.Empty, SectionSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            _animalsTitle = UiBuilder.Label("AnimalsTitle", _content, TextStyle.PlainBold, string.Empty, SectionSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            _framesTitle.gameObject.SetActive(false);
            _animalsTitle.gameObject.SetActive(false);
            _frameCells.Add(new FrameCell(this, null, _content, art));
            for (int i = 0; i < AvatarFrames.Count; i++)
            {
                _frameCells.Add(new FrameCell(this, AvatarFrames.All[i], _content, art));
            }
        }

        private void RefreshFrames(AvatarAtlas atlas, AvatarInfo current)
        {
            AvatarFrameService? frames = _frames;
            List<AvatarFrameInfo>? owned = frames?.Owned();
            bool shown = owned != null && owned.Count > 0;
            _framesTitle.gameObject.SetActive(shown);
            _animalsTitle.gameObject.SetActive(shown);
            _framesTitle.text = shown ? AvatarText.FramesTitle : string.Empty;
            _animalsTitle.text = shown ? AvatarText.AnimalsTitle : string.Empty;
            AvatarFrameInfo? on = shown ? frames!.Current : null;
            Sprite portrait = atlas.Cell(current.Index);
            for (int i = 0; i < _frameCells.Count; i++)
            {
                FrameCell cell = _frameCells[i];
                bool visible = shown && (cell.Frame == null || Contains(owned!, cell.Frame));
                cell.Root.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                bool chosen = ReferenceEquals(cell.Frame, on) || (cell.Frame != null && on != null && cell.Frame.World == on.World);
                bool isNew = cell.Frame != null && !frames!.Data.IsFrameSeen(cell.Frame.Id);
                Sprite? overlay = cell.Frame != null ? FrameSprite(cell.Frame) : null;
                cell.Fill(portrait, overlay, chosen, isNew);
            }
        }

        private static bool Contains(List<AvatarFrameInfo> list, AvatarFrameInfo frame)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].World == frame.World)
                {
                    return true;
                }
            }

            return false;
        }

        private Sprite FrameSprite(AvatarFrameInfo frame)
        {
            if (!_frameSprites.TryGetValue(frame.World, out Sprite sprite) || sprite == null)
            {
                sprite = ProceduralArt.AvatarFrameSprite(frame.World, frame.Country, FramePx);
                _frameSprites[frame.World] = sprite;
            }

            return sprite;
        }

        /// <summary>
        /// Destruye los marcos de las celdas sin tocar la UI (que puede estar ya destruida): al destruir la escena con «Tu avatar»
        /// abierta (una recarga), cuando su <see cref="Close"/> no llega.
        /// </summary>
        public void ReleaseFrameArt()
        {
            foreach (KeyValuePair<int, Sprite> pair in _frameSprites)
            {
                ProceduralArt.DestroyAvatarFrameSprite(pair.Value);
            }

            _frameSprites.Clear();
        }

        private void ReleaseFrames()
        {
            for (int i = 0; i < _frameCells.Count; i++)
            {
                _frameCells[i].Fill(null, null, false, false);
            }

            foreach (KeyValuePair<int, Sprite> pair in _frameSprites)
            {
                ProceduralArt.DestroyAvatarFrameSprite(pair.Value);
            }

            _frameSprites.Clear();
        }

        // Coloca la fila de los marcos arriba del contenido; devuelve dónde empiezan los animales.
        private float LayoutFrames(float colW, float gridW, float s)
        {
            if (!_framesTitle.gameObject.activeSelf)
            {
                return 0f;
            }

            Fit(_framesTitle, SectionSp * s, s);
            Fit(_animalsTitle, SectionSp * s, s);
            float titleH = Mathf.Ceil(Mathf.Max(SectionSp * s * 1.4f, UiBuilder.MinTextHeight(_framesTitle, gridW - 12f)));
            float cy = 4f;
            UiBuilder.Place(_framesTitle.rectTransform, 6f, cy, gridW - 12f, titleH);
            cy += titleH;
            int col = 0;
            float rowH = 0f;
            var row = new List<FrameCell>(_columns);
            for (int i = 0; i < _frameCells.Count; i++)
            {
                FrameCell cell = _frameCells[i];
                if (!cell.Root.gameObject.activeSelf)
                {
                    continue;
                }

                row.Add(cell);
                rowH = Mathf.Max(rowH, cell.Measure(colW - 2f, s));
                col++;
                if (col == _columns)
                {
                    cy = PlaceFrameRow(row, colW, cy, rowH, s);
                    row.Clear();
                    col = 0;
                    rowH = 0f;
                }
            }

            if (row.Count > 0)
            {
                cy = PlaceFrameRow(row, colW, cy, rowH, s);
            }

            float animalsH = Mathf.Ceil(Mathf.Max(SectionSp * s * 1.4f, UiBuilder.MinTextHeight(_animalsTitle, gridW - 12f)));
            UiBuilder.Place(_animalsTitle.rectTransform, 6f, cy + 4f, gridW - 12f, animalsH);
            return cy + 4f + animalsH;
        }

        private static float PlaceFrameRow(List<FrameCell> row, float colW, float cy, float rowH, float s)
        {
            for (int c = 0; c < row.Count; c++)
            {
                row[c].Layout(c * colW, cy, colW, rowH, s);
            }

            return cy + rowH;
        }

        private IEnumerable<TextMeshProUGUI> FrameTexts()
        {
            if (!_framesTitle.gameObject.activeSelf)
            {
                yield break;
            }

            yield return _framesTitle;
            yield return _animalsTitle;
            for (int i = 0; i < _frameCells.Count; i++)
            {
                if (_frameCells[i].Root.gameObject.activeSelf)
                {
                    foreach (TextMeshProUGUI t in _frameCells[i].Texts())
                    {
                        yield return t;
                    }
                }
            }
        }

        private void AddFrameProbeIds(List<string> ids, List<RectTransform> rects)
        {
            for (int i = 0; i < _frameCells.Count; i++)
            {
                FrameCell cell = _frameCells[i];
                if (cell.Root.gameObject.activeSelf && !A11yScroll.ClippedOut(cell.Root))
                {
                    ids.Add(cell.Id);
                    rects.Add(cell.Root);
                }
            }
        }

        private FrameCell FrameCellOf(string id)
        {
            for (int i = 0; i < _frameCells.Count; i++)
            {
                if (string.Equals(_frameCells[i].Id, id, StringComparison.Ordinal))
                {
                    return _frameCells[i];
                }
            }

            throw new ArgumentException("Marco desconocido: " + id, nameof(id));
        }

        private void OnFrameCell(FrameCell cell)
        {
            FrameTapped?.Invoke(cell.Frame?.Id);
        }

        /// <summary>Una celda de marco: el avatar elegido con el marco encima, la chapa ✓ o «Nuevo» y el nombre del mundo.</summary>
        private sealed class FrameCell
        {
            private readonly Image _ring;
            private readonly Image _portrait;
            private readonly Image _overlay;
            private readonly RectTransform _check;
            private readonly RectTransform _newChip;
            private readonly TextMeshProUGUI _newText;
            private readonly TextMeshProUGUI _name;

            public FrameCell(AvatarScreen owner, AvatarFrameInfo? frame, Transform parent, ProceduralArt art)
            {
                Frame = frame;
                Id = frame?.Id ?? FrameNoneId;
                Button = UiBuilder.BareButton("Frame_" + Id, parent, () => owner.OnFrameCell(this));
                Root = (RectTransform)Button.transform;
                _ring = UiBuilder.Image("Ring", Root, art.Disc, Palette.FamilyLeaf.Light);
                _portrait = UiBuilder.Image("Portrait", Root, null, Color.white);
                _portrait.preserveAspect = true;
                _overlay = UiBuilder.Image("Frame", Root, null, Color.white);
                _overlay.preserveAspect = true;
                _overlay.raycastTarget = false;
                _check = UiBuilder.Rect("Check", Root);
                Image checkDisc = UiBuilder.Image("Disc", _check, art.Disc, Palette.FamilyLeaf.Deep);
                UiBuilder.Stretch(checkDisc.rectTransform);
                Image checkMark = UiBuilder.Image("Mark", _check, art.AvatarCheck, Palette.Cream);
                UiBuilder.Stretch(checkMark.rectTransform);
                checkMark.rectTransform.offsetMin = new Vector2(2f, 2f);
                checkMark.rectTransform.offsetMax = new Vector2(-2f, -2f);
                _newChip = UiBuilder.Rect("New", Root);
                Image chip = UiBuilder.Sliced("Plate", _newChip, art.RoundedRect, Palette.Heliconia);
                UiBuilder.Stretch(chip.rectTransform);
                _newText = UiBuilder.Label("Text", _newChip, TextStyle.PlainBold, AvatarText.New, 12f, Palette.Cream, TextAlignmentOptions.Center);
                _name = UiBuilder.Label("Name", Root, TextStyle.Body, string.Empty, NameSp, Palette.FamilyWood.Deep, TextAlignmentOptions.Top);
                _name.textWrappingMode = TextWrappingModes.Normal;
                A11yLabels.Set(Button, () => Reading, () => ChosenShown);
                A11yLabels.Set(_name, () => string.Empty);
                A11yLabels.Set(_newText, () => string.Empty);
                Root.gameObject.SetActive(false);
            }

            public AvatarFrameInfo? Frame { get; }

            public string Id { get; }

            public Button Button { get; }

            public RectTransform Root { get; }

            public string Reading { get; private set; } = string.Empty;

            public bool ChosenShown { get; private set; }

            public bool NewShown { get; private set; }

            public Sprite? OverlaySprite => _overlay.gameObject.activeSelf ? _overlay.sprite : null;

            public IEnumerable<TextMeshProUGUI> Texts()
            {
                yield return _name;
                if (_newChip.gameObject.activeSelf)
                {
                    yield return _newText;
                }
            }

            public void Fill(Sprite? portrait, Sprite? overlay, bool chosen, bool isNew)
            {
                _portrait.sprite = portrait;
                _overlay.sprite = overlay;
                _overlay.gameObject.SetActive(overlay != null);
                ChosenShown = chosen;
                NewShown = isNew;
                _ring.gameObject.SetActive(chosen);
                _check.gameObject.SetActive(chosen);
                _newChip.gameObject.SetActive(isNew && !chosen);
                _newText.text = AvatarText.New;
                _name.text = AvatarText.FrameName(Frame);
                Reading = AvatarText.FrameCellA11y(Frame, chosen, isNew);
            }

            public float Measure(float textW, float s)
            {
                Fit(_name, NameSp * s, s);
                float nameH = Mathf.Ceil(Mathf.Max(NameSp * s * 1.25f, UiBuilder.MinTextHeight(_name, textW)));
                return Mathf.Max(AvatarSlotMinDp, 6f + CellPortraitDp + 4f + nameH + 8f);
            }

            public void Layout(float x, float y, float w, float h, float s)
            {
                UiBuilder.Place(Root, x, y, w, h);
                float px = (w - CellPortraitDp) * 0.5f;
                const float ring = 3f;
                UiBuilder.Place(_ring.rectTransform, px - ring, 6f - ring, CellPortraitDp + (2f * ring), CellPortraitDp + (2f * ring));
                UiBuilder.Place(_portrait.rectTransform, px, 6f, CellPortraitDp, CellPortraitDp);
                UiBuilder.Place(_overlay.rectTransform, px, 6f, CellPortraitDp, CellPortraitDp);
                const float chip = 22f;
                UiBuilder.Place(_check, px + CellPortraitDp - chip + 4f, 6f + CellPortraitDp - chip + 2f, chip, chip);
                _newText.fontSize = Mathf.Max(UiBuilder.MinTextSp, 12f) * s;
                float newW = Mathf.Min(w - 4f, Mathf.Ceil(UiBuilder.MinTextWidth(_newText)) + 12f);
                float newH = Mathf.Ceil((_newText.fontSize * 1.3f) + 2f);
                UiBuilder.Place(_newChip, (w - newW) * 0.5f, 2f, newW, newH);
                UiBuilder.Place(_newText.rectTransform, 0f, 0f, newW, newH);
                float textW = w - 2f;
                float nameH = Mathf.Ceil(Mathf.Max(NameSp * s * 1.25f, UiBuilder.MinTextHeight(_name, textW)));
                UiBuilder.Place(_name.rectTransform, 1f, 6f + CellPortraitDp + 4f, textW, nameH);
            }
        }
    }
}
