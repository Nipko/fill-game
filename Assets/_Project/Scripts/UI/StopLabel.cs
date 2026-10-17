#nullable enable
using System.Collections.Generic;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Rótulo del lugar, «{lugar} · {país}» (<c>stop_label</c>; D-013, punto 8; 07 §2.1; propuesta del Mapa §10.5, punto 5; tarea
    /// M2.3), en el primer nivel de cada parada (<see cref="AF.GameFlow.StopLabelRule"/>). Sale por la cola de avisos de GameScreen
    /// (GameScreen.Notices.cs), uno cada vez, y dura lo que se tarda en leerlo (<see cref="AF.GameFlow.NoticeTiming"/>, D-011), con
    /// una entrada y una salida de <see cref="FadeMs"/> ms. No captura toques (<see cref="CanvasGroup.blocksRaycasts"/> = false y
    /// ningún gráfico es <c>raycastTarget</c>). Dos formas [P]:
    /// <list type="bullet">
    /// <item><description><b>Sobre el horizonte</b> (tableros de 1 y 2 filas): Baloo 2 ExtraBold de <see cref="TextSp"/> sp en
    /// crema con el contorno de 2,5 dp y la sombra de madera de la insignia del nivel (<see cref="TextStyle.Badge"/>), centrado a
    /// lo ancho, con su borde de arriba <see cref="AboveHorizonDp"/> dp sobre el horizonte del panorama (nunca a menos de
    /// <see cref="HudGapDp"/> dp del pie de la fila del HUD) y sin bajar del canto de arriba del panel: en la franja del panorama,
    /// donde se ve el sitio. El contorno oscuro separa la letra de cualquier cielo o héroe (<see cref="ContrastPairs"/>).</description></item>
    /// <item><description><b>Placa bajo el HUD</b> (tableros de 3 filas, que no dejan franja: el panel empieza bajo el HUD): una
    /// placa de madera oscura con canto de <c>madera_contorno</c>, ajustada al texto (Baloo 2 ExtraBold de <see cref="PlateTextSp"/>
    /// sp en crema con contorno, <see cref="TextStyle.ModalTitle"/>; de alto <see cref="PlateHeightEm"/> veces el texto, con la
    /// tinta centrada), clavada al marco del panel (su borde de arriba <see cref="PlateOverFrameDp"/> dp por encima del canto
    /// interior del marco: 3 dp dentro de la madera; nunca sobre la fila del HUD) y al menos <see cref="PlateCuiaGapDp"/> dp
    /// por encima de lo más alto de la fila 1 en reposo (los corchos y las piezas de las mecánicas que asoman sobre la boca,
    /// como el medallón de la Hoja): monta sobre la barra de arriba del marco, con aire sobre las cuias y sin tapar sus bocas.
    /// Si no cabe, el texto se encoge, nunca por debajo de 12 sp (02 §12).
    /// <b>Arreglos de la prueba de la ola 2 en el teléfono (observación 3; tarea FX4)</b> [P]: con la placa a la vista, nada de
    /// la fila 1 puede quedar bajo ella. Entre la placa clavada al marco y la cuia elevada no caben ni 12 sp (en 360 × 780 dp
    /// quedan ~12 dp, y la placa de una línea a 12 sp mide 17 + <see cref="PlateLiftAirDp"/> de aire), así que al elevar una
    /// cuia de la fila 1 que quede bajo la placa, esta se oculta en el acto y se descarta, como el aviso ante un modal
    /// (GameScreen.Notices.cs). Si ni en reposo cabe a 12 sp sobre toda la fila 1 (la Hoja en la fila 1, como en el 171: su
    /// medallón sube 0,70 u sobre la boca), va a la misma altura pero de lado, centrada en el tramo libre más ancho de la fila 1
    /// (con <see cref="PlateSideAirDp"/> dp a cada lado de lo que asoma); si en ningún tramo cabe en una línea a 12 sp, va sobre
    /// el muelle, bajo el panel y por encima de la bandeja, como la tarjeta del miquito; si tampoco cabe ahí, no sale (nunca
    /// se recorta el texto).</description></item>
    /// </list>
    /// El texto se mide al enseñarlo y al cambiar de sitio, nunca por fotograma (sin asignaciones en <see cref="Tick"/>).
    /// </summary>
    internal sealed class StopLabel
    {
        /// <summary>El borde de arriba del rótulo, sobre el horizonte del panorama (dp; plan del Mapa, M2.3) [P].</summary>
        public const float AboveHorizonDp = 28f;

        /// <summary>Texto del rótulo sobre el horizonte (sp; plan del Mapa, M2.3) [P].</summary>
        public const float TextSp = 18f;

        /// <summary>Texto de la placa bajo el HUD (sp) [P].</summary>
        public const float PlateTextSp = 14f;

        /// <summary>Margen a cada lado del lienzo (dp).</summary>
        public const float SideDp = 16f;

        /// <summary>Aire entre el pie de la fila del HUD y el rótulo sobre el horizonte (dp) [P].</summary>
        public const float HudGapDp = 2f;

        /// <summary>Aire entre el rótulo sobre el horizonte y el canto de arriba del panel (dp) [P].</summary>
        public const float CuiaGapDp = 2f;

        /// <summary>
        /// Lo que la placa monta sobre el marco del panel, desde su canto interior tal como lo cuenta <c>PanelFrameDp</c> (dp;
        /// revisión final de M2.3: con el borde de arriba justo donde acaba la madera clara, tangente, no se leía ni como pieza del
        /// marco ni como hueco) [P]: tapa el canto oscuro interior del marco (~2 dp, que cuenta en <c>PanelFrameDp</c>) y 3 dp de
        /// la madera de la barra de arriba, sin subir del pie de la fila del HUD; así se lee como una placa clavada al marco.
        /// </summary>
        public const float PlateOverFrameDp = 5f;

        /// <summary>
        /// Aire mínimo entre la placa y lo más alto de la fila 1 en reposo, corchos y piezas de las mecánicas incluidos (dp;
        /// revisión de M2.3: a 2-3 dp la placa parecía un choque con las bocas). Para respetarlo el texto se encoge hasta 12 sp;
        /// si ni así cabe, la placa va sobre el muelle (GameScreen.Notices.cs; tarea FX4).
        /// </summary>
        public const float PlateCuiaGapDp = 6f;

        /// <summary>
        /// Aire mínimo entre la placa y lo más alto de la fila 1 elevada (la cuia, su corcho y las piezas de su mecánica; dp; tarea
        /// FX4, observación 3 de la prueba de la ola 2) [P]: si una cuia de la fila 1 se eleva y llega a menos, la placa se oculta.
        /// </summary>
        public const float PlateLiftAirDp = 4f;

        /// <summary>
        /// Aire mínimo a cada lado entre la placa de lado y lo que asoma de la fila 1 sin dejarle sitio encima (dp; revisión de
        /// FX4) [P]: la placa se centra en el tramo libre de la fila 1 con este aire a los lados.
        /// </summary>
        public const float PlateSideAirDp = 4f;

        /// <summary>
        /// Lo que sube como mucho una cuia al elevarse (dp): <see cref="BoardLayout.Lift"/> con el rebote de la curva de la
        /// elevación (<c>Ease.OutBack</c>, que pasa ~10 % del final; <c>CuiaView.Tick</c>).
        /// </summary>
        public const float LiftReachDp = BoardLayout.Lift * 1.1f;

        /// <summary>
        /// Alto de la placa de una línea, en veces el tamaño de su texto [P]: ajustada a la tinta del texto (Baloo 2 trae mucho
        /// aire en su caja de línea, ~1,6 veces el tamaño), con un margen visible arriba y abajo. Con 14 sp, 19 dp.
        /// </summary>
        public const float PlateHeightEm = 1.35f;

        /// <summary>Aire del texto a cada lado dentro de la placa (dp) [P].</summary>
        public const float PlatePadXDp = 12f;

        /// <summary>Canto de la placa (dp).</summary>
        public const float PlateEdgeDp = 1.5f;

        /// <summary>Entrada y salida (ms), como el aviso.</summary>
        public const float FadeMs = 150f;

        // Holgura del ancho de una línea medida (redondeo de TextMeshPro), para que no salte a dos líneas.
        private const float FitSlackDp = 2f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _edge;
        private readonly Image _plate;
        private readonly TextMeshProUGUI _horizonText;
        private readonly TextMeshProUGUI _plateText;
        private readonly float _plateMaxSize;
        private readonly float _plateMinSize;
        private float _leftMs = -1f;
        private float _totalMs;
        private bool _onPlate;
        private Rect _rect;

        public StopLabel(Transform parent, ProceduralArt art)
        {
            _root = UiBuilder.Rect("StopLabel", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _edge = UiBuilder.Sliced("Edge", _root, art.RoundedRect, Palette.FamilyWood.Outline);
            _edge.pixelsPerUnitMultiplier = 24f / 10f;
            UiBuilder.Stretch(_edge.rectTransform);
            _plate = UiBuilder.Sliced("Plate", _root, art.RoundedRect, Palette.FamilyWood.Deep);
            _plate.pixelsPerUnitMultiplier = 24f / 10f;
            UiBuilder.Stretch(_plate.rectTransform);
            _plate.rectTransform.offsetMin = new Vector2(PlateEdgeDp, PlateEdgeDp);
            _plate.rectTransform.offsetMax = new Vector2(-PlateEdgeDp, -PlateEdgeDp);

            _horizonText = NewText("Text", TextStyle.Badge, TextSp);
            _plateText = NewText("PlateText", TextStyle.ModalTitle, PlateTextSp);
            _plateMaxSize = _plateText.fontSizeMax; // PlateTextSp y 12 sp con la escala de texto (A1)
            _plateMinSize = _plateText.fontSizeMin;
            _plateText.enableAutoSizing = false; // el tamaño lo fija PlaceAsPlate, con el alto de la placa
            _plateText.alignment = TextAlignmentOptions.Midline;
            _root.gameObject.SetActive(false);
        }

        /// <summary>El rótulo se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Texto del rótulo («Floresta Nacional do Tapajós · Brasil»).</summary>
        public string Text => Label.text;

        /// <summary>Va como placa bajo el HUD (3 filas); si no, sobre el horizonte.</summary>
        public bool OnPlate => _onPlate;

        /// <summary>Duración del rótulo a la vista, con sus fundidos (ms; tests).</summary>
        public float DurationMs => _totalMs;

        /// <summary>Ms que le quedan a la vista (−1 si no se ve).</summary>
        public float LeftMs => _leftMs;

        /// <summary>Opacidad del rótulo (fundidos; tests).</summary>
        public float Alpha => _group.alpha;

        /// <summary>El rótulo bloquea los toques (siempre false: los deja pasar; tests).</summary>
        public bool BlocksRaycasts => _group.blocksRaycasts;

        /// <summary>Rectángulo del rótulo (la placa o la caja del texto), en dp desde arriba a la izquierda del lienzo.</summary>
        public Rect Rect => _rect;

        /// <summary>El texto que se ve (el de la placa o el del horizonte; tests).</summary>
        public TextMeshProUGUI Label => _onPlate ? _plateText : _horizonText;

        /// <summary>Raíz del rótulo (tests de capas).</summary>
        public RectTransform Root => _root;

        /// <summary>
        /// Pares texto / fondo de la comprobación ≥ 4,5:1 (WCAG 1.4.3; criterio de M2.3): el crema sobre el contorno de la letra
        /// (lo que la separa del cielo o del héroe sobre el horizonte) y sobre la madera de la placa.
        /// </summary>
        public static (string Name, Color Text, Color Background)[] ContrastPairs()
        {
            return new[]
            {
                ("rótulo sobre el horizonte: crema sobre su contorno", (Color)Palette.Cream, Palette.FamilyWood.Outline),
                ("placa bajo el HUD: crema sobre la madera", (Color)Palette.Cream, Palette.FamilyWood.Deep),
            };
        }

        /// <summary>
        /// Enseña <paramref name="text"/> durante <paramref name="ms"/> ms sobre el horizonte: centrado a lo ancho de un lienzo de
        /// <paramref name="canvasW"/> dp, con su borde de arriba <see cref="AboveHorizonDp"/> dp sobre <paramref name="horizonY"/>
        /// y no más arriba de <paramref name="minTop"/> (el pie de la fila del HUD + <see cref="HudGapDp"/>); su borde de abajo no
        /// pasa de <paramref name="maxBottom"/> (el canto de arriba del panel − <see cref="CuiaGapDp"/>): si ahí no llega, sube lo
        /// justo (sin pasar de <paramref name="minTop"/>), y si ni así cabe, el texto se encoge (12 sp como mucho).
        /// </summary>
        public void ShowOnHorizon(string text, float ms, float canvasW, float horizonY, float minTop, float maxBottom)
        {
            Begin(text, ms, false);
            PlaceOnHorizon(canvasW, horizonY, minTop, maxBottom);
        }

        /// <summary>Vuelve a colocar el rótulo sobre el horizonte (cambio de lienzo o de tablero con el rótulo a la vista).</summary>
        public void PlaceOnHorizon(float canvasW, float horizonY, float minTop, float maxBottom)
        {
            float width = Mathf.Max(0f, canvasW - (2f * SideDp));
            float height = Mathf.Ceil(_horizonText.GetPreferredValues(_horizonText.text, width, float.PositiveInfinity).y);

            // Con el panel muy arriba (un lienzo apaisado), sube lo justo para no entrar en él, sin pasar del HUD.
            float top = Mathf.Max(Mathf.Min(horizonY - AboveHorizonDp, maxBottom - height), minTop);
            if (top + height > maxBottom)
            {
                float min = Mathf.Ceil(UiBuilder.MinTextHeight(_horizonText, width));
                height = Mathf.Max(min, maxBottom - top);
            }

            SetRect(SideDp, top, width, height);
            UiBuilder.Stretch(_horizonText.rectTransform);
        }

        /// <summary>
        /// Empieza a enseñar <paramref name="text"/> durante <paramref name="ms"/> ms como placa bajo el HUD, sin colocarla: quien
        /// la enseña la coloca después con <see cref="PlaceAsPlate"/> en el primer sitio donde quepa (clavada al marco, de lado o
        /// sobre el muelle; GameScreen.Notices.cs) o, si no cabe en ninguno, la oculta.
        /// </summary>
        public void BeginPlate(string text, float ms)
        {
            Begin(text, ms, true);
        }

        /// <summary>
        /// Alto de la placa de una línea con el texto más pequeño (12 sp con la escala de texto; dp): lo menos que necesita.
        /// </summary>
        public float MinPlateHeight => Mathf.Ceil((_plateMinSize * PlateHeightEm) - 0.01f);

        /// <summary>
        /// Coloca la placa (al enseñarla, o al cambiar de lienzo o de tablero con el rótulo a la vista) en el tramo de
        /// <paramref name="left"/> a <paramref name="right"/> (dp desde la izquierda del lienzo), centrada en él. El tamaño del
        /// texto es el mayor, hasta <see cref="PlateTextSp"/> sp, con el que la placa de una línea (<see cref="PlateHeightEm"/>
        /// veces el texto) cabe entre <paramref name="top"/> y <paramref name="maxBottom"/> y a lo ancho del tramo, nunca menos de
        /// 12 sp. Si ni a 12 sp cabe en una línea: con <paramref name="wrap"/>, va a lo ancho del tramo en varias, con la caja de
        /// línea entera; sin él, no se coloca y devuelve false. Devuelve si la placa cabe: su borde de abajo no pasa de
        /// <paramref name="maxBottom"/> (tarea FX4).
        /// </summary>
        public bool PlaceAsPlate(float left, float right, float top, float maxBottom, bool wrap)
        {
            float maxWidth = Mathf.Max(0f, right - left);
            float inner = Mathf.Max(0f, maxWidth - (2f * PlatePadXDp));
            string text = _plateText.text;
            float size = Mathf.Clamp(Mathf.Floor(maxBottom - top) / PlateHeightEm, _plateMinSize, _plateMaxSize);
            _plateText.textWrappingMode = TextWrappingModes.NoWrap;
            _plateText.fontSize = size;
            float line = _plateText.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x + FitSlackDp;
            if (line > inner && size > _plateMinSize)
            {
                size = Mathf.Max(_plateMinSize, size * (inner - FitSlackDp - 1f) / (line - FitSlackDp)); // 1 dp de margen al redondeo
                _plateText.fontSize = size;
                line = _plateText.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x + FitSlackDp;
            }

            float width;
            float height;
            if (line <= inner)
            {
                width = Mathf.Ceil(line) + (2f * PlatePadXDp);
                height = Mathf.Ceil((size * PlateHeightEm) - 0.01f);
            }
            else if (wrap)
            {
                _plateText.textWrappingMode = TextWrappingModes.Normal;
                width = maxWidth;
                height = Mathf.Ceil(_plateText.GetPreferredValues(text, inner, float.PositiveInfinity).y);
            }
            else
            {
                return false; // de lado, solo en una línea
            }

            SetRect(left + ((maxWidth - width) * 0.5f), top, width, height);

            // La caja del texto sobresale de la placa arriba y abajo (cabe la caja de línea entera, más alta que la placa) y la
            // tinta se centra por su geometría (Midline), no por la caja de línea.
            RectTransform rt = _plateText.rectTransform;
            UiBuilder.Stretch(rt);
            rt.offsetMin = new Vector2(PlatePadXDp, -size);
            rt.offsetMax = new Vector2(-PlatePadXDp, size);
            return _rect.yMax <= maxBottom + 0.01f;
        }

        /// <summary>
        /// Baja la placa ya colocada hasta <paramref name="top"/> (dp desde arriba), sin medir otra vez el texto: centrarla en el
        /// hueco del muelle (tarea FX4).
        /// </summary>
        public void MovePlateTo(float top)
        {
            SetRect(_rect.x, top, _rect.width, _rect.height);
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

        /// <summary>Añade los gráficos del rótulo (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.Add(_edge);
            graphics.Add(_plate);
            graphics.Add(_horizonText);
            graphics.Add(_plateText);
        }

        private TextMeshProUGUI NewText(string name, TextStyle style, float size)
        {
            TextMeshProUGUI t = UiBuilder.Label(name, _root, style, string.Empty, size, Palette.Cream, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(t, UiBuilder.MinTextSp, size);
            UiBuilder.Stretch(t.rectTransform);
            return t;
        }

        private void Begin(string text, float ms, bool plate)
        {
            _onPlate = plate;
            _edge.gameObject.SetActive(plate);
            _plate.gameObject.SetActive(plate);
            _plateText.gameObject.SetActive(plate);
            _horizonText.gameObject.SetActive(!plate);
            Label.text = text;
            _leftMs = ms;
            _totalMs = Mathf.Max(1f, ms);
            _group.alpha = 0f;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
        }

        private void SetRect(float left, float top, float width, float height)
        {
            _rect = new Rect(left, top, width, height);
            UiBuilder.Place(_root, left, top, width, height);
        }
    }
}
