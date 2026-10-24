#nullable enable
using System;
using AF.LiveOps;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Bandera dibujada por código (D-046; liga.md §4: «dibujadas por código como el resto del arte provisional»), en un recuadro
    /// de proporción 3:2 recortado por <see cref="RectMask2D"/>: franjas y formas sencillas de las de los países amazónicos y los
    /// mercados principales (<see cref="LeagueText.FlagCountries"/>); para el resto, una placa neutra con el código ISO. Nunca va
    /// sola: quien la pone escribe el nombre del país al lado. Es decorativa para el lector de pantalla (la fila lleva el nombre).
    /// Solo rehace sus piezas al cambiar de país.
    /// </summary>
    internal sealed class LeagueFlag
    {
        /// <summary>Proporción ancho / alto.</summary>
        public const float Aspect = 1.5f;

        private static readonly Color Red = new Color32(0xCE, 0x11, 0x26, 0xFF);
        private static readonly Color White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        private static readonly Color Blue = new Color32(0x00, 0x38, 0x93, 0xFF);
        private static readonly Color Navy = new Color32(0x0A, 0x31, 0x61, 0xFF);
        private static readonly Color Yellow = new Color32(0xFC, 0xD1, 0x16, 0xFF);
        private static readonly Color Green = new Color32(0x00, 0x8C, 0x45, 0xFF);
        private static readonly Color SkyBlue = new Color32(0x74, 0xAC, 0xDF, 0xFF);
        private static readonly Color Sun = new Color32(0xF6, 0xB4, 0x0E, 0xFF);
        private static readonly Color Brown = new Color32(0x8C, 0x5A, 0x2B, 0xFF);
        private static readonly Color Neutral = new Color32(0xD9, 0xCF, 0xBE, 0xFF);

        private readonly RectTransform _root;
        private readonly ProceduralArt _art;
        private string? _code = "?";
        private TextMeshProUGUI? _label;

        public LeagueFlag(string name, Transform parent, ProceduralArt art)
        {
            _art = art;
            _root = UiBuilder.Rect(name, parent);
            _root.gameObject.AddComponent<RectMask2D>();
        }

        public RectTransform Root => _root;

        /// <summary>El país que pinta (null = nada).</summary>
        public string? Code => _code;

        /// <summary>Lleva un dibujo propio (no la placa neutra).</summary>
        public static bool HasDrawing(string? code) => code != null && Array.IndexOf(LeagueText.FlagCountries, code) >= 0;

        /// <summary>Pinta la bandera de <paramref name="code"/> en <paramref name="w"/> × <paramref name="h"/> dp (null: nada).</summary>
        public void Show(string? code, float w, float h)
        {
            if (!string.Equals(code, _code, StringComparison.Ordinal))
            {
                _code = code;
                Rebuild(w, h);
            }
            else if (!Mathf.Approximately(_root.rect.width, w) || !Mathf.Approximately(_root.rect.height, h))
            {
                Rebuild(w, h);
            }

            _root.gameObject.SetActive(code != null);
        }

        private void Rebuild(float w, float h)
        {
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_root.GetChild(i).gameObject);
                _root.GetChild(i).SetParent(null, false);
            }

            _label = null;
            Vector2 size = new Vector2(w, h);
            _root.sizeDelta = size;
            if (_code == null)
            {
                return;
            }

            switch (_code)
            {
                case "CO":
                    HBands(w, h, new[] { Yellow, Blue, Red }, new[] { 0.5f, 0.25f, 0.25f });
                    break;
                case "EC":
                    HBands(w, h, new[] { Yellow, Blue, Red }, new[] { 0.5f, 0.25f, 0.25f });
                    Disc(w * 0.5f, h * 0.5f, h * 0.36f, Brown);
                    break;
                case "VE":
                    HBands(w, h, new[] { Yellow, Blue, Red }, new[] { 1f / 3f, 1f / 3f, 1f / 3f });
                    Disc(w * 0.5f, h * 0.5f, h * 0.18f, White);
                    break;
                case "BO":
                    HBands(w, h, new[] { Red, Yellow, Green }, new[] { 1f / 3f, 1f / 3f, 1f / 3f });
                    break;
                case "PE":
                    VBands(w, h, new[] { Red, White, Red });
                    break;
                case "MX":
                    VBands(w, h, new[] { Green, White, Red });
                    Disc(w * 0.5f, h * 0.5f, h * 0.3f, Brown);
                    break;
                case "AR":
                    HBands(w, h, new[] { SkyBlue, White, SkyBlue }, new[] { 1f / 3f, 1f / 3f, 1f / 3f });
                    Disc(w * 0.5f, h * 0.5f, h * 0.24f, Sun);
                    break;
                case "SR":
                    HBands(w, h, new[] { Green, White, Red, White, Green }, new[] { 0.2f, 0.1f, 0.4f, 0.1f, 0.2f });
                    Disc(w * 0.5f, h * 0.5f, h * 0.24f, Yellow);
                    break;
                case "CL":
                    HBands(w, h, new[] { White, Red }, new[] { 0.5f, 0.5f });
                    Rect(0f, 0f, h * 0.5f, h * 0.5f, Blue);
                    Disc(h * 0.25f, h * 0.25f, h * 0.18f, White);
                    break;
                case "US":
                    float stripe = h / 7f;
                    for (int i = 0; i < 7; i++)
                    {
                        Rect(0f, i * stripe, w, stripe, i % 2 == 0 ? Red : White);
                    }

                    Rect(0f, 0f, w * 0.45f, stripe * 4f, Navy);
                    break;
                case "BR":
                    Rect(0f, 0f, w, h, Green);
                    Diamond(w * 0.5f, h * 0.5f, w * 0.78f, h * 0.74f, Yellow);
                    Disc(w * 0.5f, h * 0.5f, h * 0.44f, Navy);
                    break;
                case "GY":
                    Rect(0f, 0f, w, h, Green);
                    Diamond(0f, h * 0.5f, w * 1.9f, h * 1.05f, White);
                    Diamond(0f, h * 0.5f, w * 1.75f, h * 0.9f, Yellow);
                    Diamond(0f, h * 0.5f, w * 0.95f, h * 0.95f, Red);
                    break;
                default:
                    Rect(0f, 0f, w, h, Neutral);
                    _label = UiBuilder.Label("Code", _root, TextStyle.PlainBold, _code, Mathf.Max(UiBuilder.MinTextSp, h * 0.62f), Palette.UiInk, TextAlignmentOptions.Center);
                    UiBuilder.Place(_label.rectTransform, 0f, 0f, w, h);
                    A11yLabels.Set(_label, () => string.Empty); // el nombre va al lado y en la etiqueta de la fila
                    break;
            }

            // Un filete fino para que el blanco no se pierda sobre la crema.
            var edge = new Color(0f, 0f, 0f, 0.25f);
            Rect(0f, 0f, w, 1f, edge);
            Rect(0f, h - 1f, w, 1f, edge);
            Rect(0f, 0f, 1f, h, edge);
            Rect(w - 1f, 0f, 1f, h, edge);
        }

        private void HBands(float w, float h, Color[] colors, float[] shares)
        {
            float y = 0f;
            for (int i = 0; i < colors.Length; i++)
            {
                float bh = i == colors.Length - 1 ? h - y : Mathf.Round(h * shares[i] * 10f) / 10f;
                Rect(0f, y, w, bh, colors[i]);
                y += bh;
            }
        }

        private void VBands(float w, float h, Color[] colors)
        {
            float bw = w / colors.Length;
            for (int i = 0; i < colors.Length; i++)
            {
                Rect(i * bw, 0f, bw + 0.5f, h, colors[i]);
            }
        }

        private void Rect(float x, float y, float w, float h, Color color)
        {
            Image img = UiBuilder.Image("Band", _root, null, color);
            UiBuilder.Place(img.rectTransform, x, y, w, h);
        }

        private void Disc(float cx, float cy, float d, Color color)
        {
            Image img = UiBuilder.Image("Disc", _root, _art.Disc, color);
            UiBuilder.Place(img.rectTransform, cx - (d * 0.5f), cy - (d * 0.5f), d, d);
        }

        // Rombo de ancho w y alto h centrado en (cx, cy): un cuadrado girado 45° dentro de un contenedor aplastado en vertical.
        private void Diamond(float cx, float cy, float w, float h, Color color)
        {
            RectTransform holder = UiBuilder.Rect("Diamond", _root);
            UiBuilder.Place(holder, cx - (w * 0.5f), cy - (w * 0.5f), w, w);
            holder.pivot = new Vector2(0.5f, 0.5f);
            holder.anchoredPosition = new Vector2(cx, -cy);
            holder.localScale = new Vector3(1f, h / w, 1f);
            Image img = UiBuilder.Image("Shape", holder, null, color);
            RectTransform rt = img.rectTransform;
            float side = w / Mathf.Sqrt(2f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(side, side);
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
    }
}
