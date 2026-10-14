#nullable enable
using System.Collections.Generic;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Tarjeta «¡Miquito a la vista!» (guion de la intro §4.5; recomendación Q10 A, pendiente de D-010): de la familia de los avisos del tablero (placa de
    /// madera clara, como «El río bajó»), con el retrato de la especie de 40 dp, el título, el nombre local y una línea de ≤ 50
    /// caracteres con un dato verificado. Sale después de la intro, nunca dentro de la carga, y cuando la tropa ya se ha ido de
    /// la rama (D-011, puntos 2 y 3: hasta entonces espera y las etiquetas del HUD siguen a la vista), de una en una con los
    /// avisos del tablero (la cola de GameScreen). Dura lo que se tarda en leer el título, el nombre y el dato
    /// (<see cref="AF.GameFlow.NoticeTiming"/>, 3-7 s; unos 5 s el mono ardilla), con fundido de entrada y salida; no captura
    /// toques (<c>raycastTarget</c> = false y <see cref="CanvasGroup.blocksRaycasts"/> = false) y no tapa cuias ni botones: va
    /// sobre la franja del panorama si mide al menos su alto + 4 dp o, si no, sobre el muelle por encima de la bandeja. Va en
    /// la capa de avisos de GameScreen: delante del tablero y del HUD, detrás del origen que vierte (D-006) y de los modales.
    /// «Avistamiento», nunca «nuevo»; sin confeti. Nombres y textos, pendientes del asesor cultural (07 §8.2). Ningún texto
    /// baja de 12 sp (07 §6.4 y 02 §12; pregunta 36 (a), D-020): el dato pasa a una segunda línea si no cabe y la tarjeta crece
    /// (<see cref="Measure"/>); con la escala de texto (<see cref="UiBuilder.TextScale"/>), las filas crecen con ella.
    /// </summary>
    internal sealed class SightingCard
    {
        /// <summary>
        /// Alto mínimo de la tarjeta a la escala de texto 1 (dp); crece con la línea del dato (<see cref="Measure"/>: 61 dp con
        /// el dato en una línea a 12 sp, que mide algo más de 16 dp) y con la escala.
        /// </summary>
        public const float HeightDp = 60f;

        /// <summary>Lado del retrato (dp).</summary>
        public const float PortraitDp = 40f;

        private const float FadeMs = 200f;

        // Columna del texto, a la derecha del retrato (dp desde el borde izquierdo de la tarjeta), y su aire a la derecha.
        private const float TextX = 11f + PortraitDp + 10f;
        private const float TextRightPadDp = 12f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _plate;
        private readonly Image _edge;
        private readonly Image _halo;
        private readonly Image _portrait;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _fact;
        private readonly float _scale;
        private float _leftMs = -1f;
        private float _totalMs;
        private float _height = HeightDp;
        private float _factHeight = 16f;

        public SightingCard(Transform parent, ProceduralArt art)
        {
            _scale = UiBuilder.TextScale;
            _root = UiBuilder.Rect("SightingCard", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _edge = UiBuilder.Sliced("Edge", _root, art.RoundedRect, Palette.FamilyWood.Base);
            _edge.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_edge.rectTransform);
            _edge.rectTransform.offsetMin = new Vector2(0f, -3f);
            _edge.rectTransform.offsetMax = new Vector2(0f, -3f);
            _plate = UiBuilder.Sliced("Plate", _root, art.RoundedRect, Palette.PanelLight);
            _plate.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_plate.rectTransform);
            _halo = UiBuilder.Image("PortraitBack", _root, art.Disc, Palette.PanelLightLow);
            UiBuilder.Place(_halo.rectTransform, 9f, 8f, PortraitDp + 4f, PortraitDp + 4f);
            _portrait = UiBuilder.Image("Portrait", _root, null, Color.white);
            _portrait.preserveAspect = true;
            UiBuilder.Place(_portrait.rectTransform, 11f, 10f, PortraitDp, PortraitDp);
            // Centrados a lo alto por su línea (Left), no por la línea media de las letras (MidlineLeft): con «¡» y la «q» del
            // título, las letras asomaban por arriba de su fila (A1).
            _title = UiBuilder.Label("Title", _root, TextStyle.PlainBold, UiText.FaunaSightedCard, 15f, Palette.FamilyWood.Base, TextAlignmentOptions.Left);
            _name = UiBuilder.Label("Name", _root, TextStyle.PlainBold, string.Empty, 14f, Palette.FamilyWood.Deep, TextAlignmentOptions.Left);
            _fact = UiBuilder.Label("Fact", _root, TextStyle.Body, string.Empty, 12f, Palette.FamilyWood.Deep, TextAlignmentOptions.Left);
            _fact.textWrappingMode = TextWrappingModes.Normal; // si no cabe a 12 sp, en dos líneas: la tarjeta crece
            UiBuilder.AutoSize(_title, UiBuilder.MinTextSp, 15f);
            UiBuilder.AutoSize(_name, UiBuilder.MinTextSp, 14f);
            UiBuilder.AutoSize(_fact, UiBuilder.MinTextSp, 12f);
            _root.gameObject.SetActive(false);
        }

        /// <summary>La tarjeta está a la vista.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Nombre que muestra.</summary>
        public string Name => _name.text;

        /// <summary>Línea del dato que muestra.</summary>
        public string Fact => _fact.text;

        /// <summary>Raíz (tests de exclusión).</summary>
        public RectTransform Root => _root;

        /// <summary>Opacidad actual del fundido.</summary>
        public float Alpha => _group.alpha;

        /// <summary>Duración de la tarjeta a la vista, con sus fundidos (ms; tests).</summary>
        public float DurationMs => _totalMs;

        /// <summary>Alto de la tarjeta (dp) con el último <see cref="Measure"/>.</summary>
        public float Height => _height;

        /// <summary>Título, nombre y dato (tests de tamaño y desbordes).</summary>
        public TextMeshProUGUI[] Texts => new[] { _title, _name, _fact };

        /// <summary>
        /// Muestra la tarjeta de la especie el tiempo de leerla: <see cref="AF.GameFlow.NoticeTiming.ReadMs(string, string, string)"/>
        /// con el título, el nombre y el dato (D-011, punto 2).
        /// </summary>
        public void Show(Sprite portrait, string name, string fact)
        {
            int ms = AF.GameFlow.NoticeTiming.ReadMs(UiText.FaunaSightedCard, name, fact);
            _portrait.sprite = portrait;
            _name.text = name;
            _fact.text = fact;
            _leftMs = ms;
            _totalMs = Mathf.Max(1f, ms);
            _group.alpha = 0f;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling(); // dentro de la capa de avisos: sigue detrás de la capa de vertido
        }

        /// <summary>
        /// Pone el nombre y el dato que va a mostrar y devuelve el alto de la tarjeta en un lienzo de <paramref name="canvasW"/>
        /// dp (<see cref="Height"/>): las filas del título, el nombre y el dato por la escala de texto, y la del dato, si a 12 sp
        /// no cabe en una línea, de las que necesite (pregunta 36 (a)); al menos <see cref="HeightDp"/> por la escala. Se llama
        /// antes de decidir dónde va (GameScreen.Notices.cs) y de <see cref="Place"/>.
        /// </summary>
        public float Measure(float canvasW, string name, string fact)
        {
            _name.text = name;
            _fact.text = fact;
            float s = _scale;
            _factHeight = Mathf.Max(16f * s, Mathf.Ceil(UiBuilder.MinTextHeight(_fact, TextWidth(canvasW))));
            _height = Mathf.Max(HeightDp * s, (38f * s) + _factHeight + (6f * s));
            return _height;
        }

        /// <summary>
        /// Coloca la tarjeta centrada con su borde superior en <paramref name="top"/> (dp desde arriba), del alto del último
        /// <see cref="Measure"/>.
        /// </summary>
        public void Place(float canvasW, float top)
        {
            float w = Mathf.Min(320f, canvasW - 32f);
            float s = _scale;
            UiBuilder.Place(_root, (canvasW - w) * 0.5f, top, w, _height);
            float textW = TextWidth(canvasW);
            // El título y el nombre, centrados a lo alto en 14 y 30,5 dp (por la escala) como siempre, en filas de 22 y 20 dp, que
            // caben enteros a 15 y 14 sp (con 18 y 17 se encogían y las letras asomaban por arriba; A1).
            UiBuilder.Place(_title.rectTransform, TextX, 3f * s, textW, 22f * s);
            UiBuilder.Place(_name.rectTransform, TextX, 20.5f * s, textW, 20f * s);
            UiBuilder.Place(_fact.rectTransform, TextX, 38f * s, textW, _factHeight);
        }

        private static float TextWidth(float canvasW)
        {
            return Mathf.Min(320f, canvasW - 32f) - TextX - TextRightPadDp;
        }

        public void Hide()
        {
            _leftMs = -1f;
            _root.gameObject.SetActive(false);
        }

        public void Tick(float dtMs)
        {
            if (_leftMs < 0f)
            {
                return;
            }

            _leftMs -= dtMs;
            float shown = _totalMs - _leftMs;
            _group.alpha = Mathf.Clamp01(Mathf.Min(shown, _leftMs) / FadeMs);
            if (_leftMs <= 0f)
            {
                Hide();
            }
        }

        /// <summary>Añade los gráficos de la tarjeta (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.Add(_plate);
            graphics.Add(_edge);
            graphics.Add(_halo);
            graphics.Add(_portrait);
            graphics.Add(_title);
            graphics.Add(_name);
            graphics.Add(_fact);
        }
    }
}
