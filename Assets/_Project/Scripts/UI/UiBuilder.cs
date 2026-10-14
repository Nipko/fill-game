#nullable enable
using System;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Piezas de un botón con relieve creado por <see cref="UiBuilder.Relief"/>.</summary>
    internal readonly struct ReliefParts
    {
        public ReliefParts(Button button, RectTransform face, ReliefButton relief, Image? baseImage, Image faceImage, ReliefSprites sprites)
        {
            Button = button;
            Face = face;
            Relief = relief;
            Base = baseImage;
            FaceImage = faceImage;
            Sprites = sprites;
        }

        public Button Button { get; }

        public RectTransform Face { get; }

        public ReliefButton Relief { get; }

        public Image? Base { get; }

        public Image FaceImage { get; }

        public ReliefSprites Sprites { get; }
    }

    /// <summary>Utilidades para construir la UI por código (sin prefabs; docs/dev/prototipo-v0.md c; guía visual v2, 5).</summary>
    internal static class UiBuilder
    {
        /// <summary>
        /// Tamaño mínimo de un texto (sp; 02 §12, pregunta 36 (a), D-020): ningún texto se encoge por debajo para caber; donde
        /// no quepa, va en dos líneas o su caja crece. Con la escala de texto, son 12 × <see cref="TextScale"/> dp.
        /// </summary>
        public const float MinTextSp = 12f;

        /// <summary>Escala de texto más alta que se respeta (02 §12: «por encima del 130 % se fija ese tope»).</summary>
        public const float MaxTextScale = 1.3f;

        /// <summary>
        /// Escala de texto global [P] (02 §12: la escala de fuente del sistema, de 1 a <see cref="MaxTextScale"/>; tarea A1):
        /// multiplica el tamaño que recibe <see cref="Label"/> y el mínimo y el máximo de <see cref="AutoSize"/>, así que un
        /// texto de 12 sp mide 12 × escala dp, como en Android. Con 1, la de siempre, no cambia nada. La fija
        /// <see cref="LaunchOptions.FontScaleOverride"/> (tests) o, en desarrollo, el extra <c>af_font_scale</c>; la escala de
        /// Android aún no llega al juego. Se lee al crear cada texto (y al llamar a <see cref="AutoSize"/>): los que ya existen
        /// no cambian. La tarjeta previa y el banner de mundo llevan además su propia escala (<c>LevelCardView.FontScale</c> y
        /// <c>WorldBannerView.FontScale</c>), que fija el tamaño de sus textos al colocarlos.
        /// </summary>
        public static float TextScale => LaunchOptions.ResolveTextScale();

        /// <summary>Contenedor estirado al padre.</summary>
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            return rt;
        }

        /// <summary>Estira <paramref name="rt"/> a su padre.</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Coloca <paramref name="rt"/> en (x, y desde arriba) con tamaño w × h, anclado arriba a la izquierda.</summary>
        public static void Place(RectTransform rt, float x, float yTop, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -yTop);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>Imagen (sin raycast salvo que se pida).</summary>
        public static Image Image(string name, Transform parent, Sprite? sprite, Color color, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Imagen 9-slice (los bordes del sprite miden en dp lo que dicta su PPU).</summary>
        public static Image Sliced(string name, Transform parent, Sprite sprite, Color color)
        {
            Image img = Image(name, parent, sprite, color);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.fillCenter = true;
            return img;
        }

        /// <summary>
        /// Texto TextMeshPro con la fuente y el material de <paramref name="style"/> (guía 5.6), de <paramref name="size"/> sp
        /// (<paramref name="size"/> × <see cref="TextScale"/> dp). Sin alfa en el color.
        /// </summary>
        public static TextMeshProUGUI Label(string name, Transform parent, TextStyle style, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset? font = UiFonts.Font(style);
            if (font != null)
            {
                text.font = font;
            }

            Material? material = UiFonts.Material(style);
            if (material != null)
            {
                text.fontSharedMaterial = material;
            }

            text.text = value;
            text.fontSize = size * TextScale;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// Ajusta el texto a su caja como la guía 3.5 y 5.5: el menor entre <paramref name="max"/> y el mayor que quepa,
        /// con un mínimo de <paramref name="min"/> (en sp: los dos, por <see cref="TextScale"/>); nunca se recorta.
        /// </summary>
        public static void AutoSize(TextMeshProUGUI text, float min, float max)
        {
            float scale = TextScale;
            text.enableAutoSizing = true;
            text.fontSizeMin = min * scale;
            text.fontSizeMax = max * scale;
        }

        /// <summary>
        /// Alto que necesita el texto de ahora de <paramref name="text"/> en una caja de <paramref name="width"/> dp de ancho
        /// (sin sus márgenes) a su tamaño más pequeño: el mínimo del autoajuste o, sin él, su tamaño (dp). Lo usan las cajas que
        /// crecen (02 §12: donde el texto no cabe ni a su mínimo, su caja crece; donde cabe, nada cambia). Mide con
        /// TextMeshPro al colocar, nunca por fotograma; 0 sin texto.
        /// </summary>
        public static float MinTextHeight(TextMeshProUGUI text, float width)
        {
            string value = text.text;
            if (string.IsNullOrEmpty(value))
            {
                return 0f;
            }

            if (!text.enableAutoSizing)
            {
                return text.GetPreferredValues(value, width, float.PositiveInfinity).y;
            }

            // TextMeshPro mide los textos con autoajuste a su máximo: se mide con el máximo en el mínimo y se deja como estaba.
            float max = text.fontSizeMax;
            text.fontSizeMax = text.fontSizeMin;
            float height = text.GetPreferredValues(value, width, float.PositiveInfinity).y;
            text.fontSizeMax = max;
            return height;
        }

        /// <summary>
        /// Botón con relieve (guía 5.1): el objeto con <see cref="Button"/> es el área táctil (invisible); dentro, la base
        /// fija (sombra, canto y contorno) y la cara, que baja al pulsar con su contenido. El contenido (glifo, etiqueta)
        /// se añade bajo <see cref="ReliefParts.Face"/> y después se llama a <see cref="ReliefButton.Bind"/>.
        /// </summary>
        public static ReliefParts Relief(string name, Transform parent, ReliefSprites sprites, Action onClick)
        {
            Button button = BareButton(name, parent, onClick);
            ReliefButton relief = button.gameObject.AddComponent<ReliefButton>();
            RectTransform visual = Rect("Visual", button.transform);
            visual.anchorMin = new Vector2(0f, 1f);
            visual.anchorMax = new Vector2(1f, 1f);
            visual.pivot = new Vector2(0.5f, 1f);
            visual.offsetMin = Vector2.zero;
            visual.offsetMax = Vector2.zero;
            visual.sizeDelta = new Vector2(0f, sprites.HeightDp);
            visual.anchoredPosition = Vector2.zero;
            Image? baseImage = null;
            if (sprites.Base != null)
            {
                baseImage = sprites.Sliced ? Sliced("Base", visual, sprites.Base, Color.white) : Image("Base", visual, sprites.Base, Color.white);
                Stretch(baseImage.rectTransform);
            }

            RectTransform face = Rect("Face", visual);
            Image faceImage = sprites.Sliced ? Sliced("FaceImage", face, sprites.Face, Color.white) : Image("FaceImage", face, sprites.Face, Color.white);
            Stretch(faceImage.rectTransform);
            return new ReliefParts(button, face, relief, baseImage, faceImage, sprites);
        }

        /// <summary>Botón sin fondo (enlace o icono) con área táctil invisible.</summary>
        public static Button BareButton(string name, Transform parent, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(HitArea));
            go.transform.SetParent(parent, false);
            HitArea hit = go.GetComponent<HitArea>();
            hit.raycastTarget = true;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>
        /// Cuerpo de un modal (guía 5.3): sombra, superficie de madera clara pulida (por código, al tamaño del panel con
        /// <see cref="ProceduralArt.ModalSurfaceFor"/>) y marco 9-slice. Devuelve la superficie para ajustarla al tamaño.
        /// </summary>
        public static Image ModalBody(RectTransform panel, ProceduralArt art)
        {
            Image shadow = Sliced("Shadow", panel, art.SoftShadow, new Color(1f, 1f, 1f, 0.35f));
            Stretch(shadow.rectTransform);
            shadow.rectTransform.offsetMin = new Vector2(-14f, -20f);
            shadow.rectTransform.offsetMax = new Vector2(14f, 8f);
            Image surface = Image("Surface", panel, art.ModalSurfaceFor(312f, 100f), Color.white);
            Stretch(surface.rectTransform);
            surface.rectTransform.offsetMin = new Vector2(8f, 8f);
            surface.rectTransform.offsetMax = new Vector2(-8f, -8f);
            Image frame = Sliced("Frame", panel, art.ModalPanel, Color.white);
            Stretch(frame.rectTransform);
            return surface;
        }

        /// <summary>Etiqueta de CTA: centrada al 56 % de la cara y dentro de la franja del 25-85 % (guía 5.1 y 2.4).</summary>
        public static void PlaceCtaLabel(RectTransform label, ReliefSprites sprites)
        {
            PlaceCtaLabel(label, sprites.FaceHeightDp);
        }

        /// <summary>
        /// Etiqueta de CTA en una cara de <paramref name="face"/> dp de alto (la de un CTA que crece con la escala de texto,
        /// <c>ModalKit.FaceHeightOf</c>): centrada al 56 % de la cara y con el 60 % de su alto (guía 5.1 y 2.4).
        /// </summary>
        public static void PlaceCtaLabel(RectTransform label, float face)
        {
            label.anchorMin = new Vector2(0f, 1f);
            label.anchorMax = new Vector2(1f, 1f);
            label.pivot = new Vector2(0.5f, 0.5f);
            label.sizeDelta = new Vector2(-24f, face * 0.60f);
            label.anchoredPosition = new Vector2(0f, -(face * 0.56f));
        }

        /// <summary>
        /// Activa o desactiva un botón. Con relieve: plano y desaturado, con silueta y etiqueta legibles (guía 5.1); sin
        /// relieve (enlace): el texto pasa a <c>deshabilitado_texto</c>. Nunca con alfa en el texto (07 §7.8).
        /// </summary>
        public static void SetEnabled(Button button, bool enabled)
        {
            ReliefButton? relief = button.GetComponent<ReliefButton>();
            if (relief != null)
            {
                relief.SetInteractable(enabled);
                return;
            }

            if (button.interactable != enabled)
            {
                button.interactable = enabled;
            }
        }
    }
}
