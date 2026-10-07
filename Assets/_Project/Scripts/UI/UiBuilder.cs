#nullable enable
using System;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Utilidades para construir la UI por código (sin prefabs; docs/dev/prototipo-v0.md c).</summary>
    internal static class UiBuilder
    {
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

        /// <summary>Imagen 9-slice redondeada con radio <paramref name="radiusDp"/>.</summary>
        public static Image Rounded(string name, Transform parent, ProceduralArt art, Color color, float radiusDp, bool raycast = false)
        {
            Image img = Image(name, parent, art.RoundedRect, color, raycast);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / Mathf.Max(1f, radiusDp);
            return img;
        }

        /// <summary>Texto de UI con la fuente <c>LegacyRuntime.ttf</c>.</summary>
        public static Text Text(string name, Transform parent, Font font, string value, int size, Color color, TextAnchor anchor, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// Botón "piedra de río" (07 §7.3): rectángulo redondeado plano de radio 14 dp con borde inferior 2 dp más
        /// oscuro. Devuelve el contenedor; la cara (con el componente <see cref="Button"/>) es su segundo hijo.
        /// </summary>
        public static Button StoneButton(string name, Transform parent, ProceduralArt art, Color face, Action onClick)
        {
            RectTransform root = Rect(name, parent);
            root.gameObject.AddComponent<CanvasGroup>();
            Color edgeColor = face * 0.72f;
            edgeColor.a = face.a;
            Image edge = Rounded("Edge", root, art, edgeColor, 14f);
            Stretch(edge.rectTransform);
            edge.rectTransform.offsetMin = new Vector2(0f, -2f);
            edge.rectTransform.offsetMax = new Vector2(0f, -2f);
            Image faceImg = Rounded("Face", root, art, face, 14f, true);
            Stretch(faceImg.rectTransform);
            Button button = faceImg.gameObject.AddComponent<Button>();
            button.targetGraphic = faceImg;
            ConfigureColors(button);
            button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>Botón sin fondo (enlace o icono) con área táctil invisible.</summary>
        public static Button BareButton(string name, Transform parent, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(HitArea), typeof(CanvasGroup));
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

        /// <summary>Activa o desactiva un botón con alfa 40 % al desactivarlo (07 §7.3).</summary>
        public static void SetEnabled(Button button, bool enabled)
        {
            if (button.interactable != enabled)
            {
                button.interactable = enabled;
            }

            CanvasGroup? group = button.GetComponentInParent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = enabled ? 1f : 0.4f;
            }
        }

        private static void ConfigureColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }
    }
}
