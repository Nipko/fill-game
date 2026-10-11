#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Líneas <c>AF_LAYOUT</c> de la sonda de desarrollo capturadas del log, como las lee <c>adb logcat</c> en el teléfono (T-A y
    /// T-B de la prueba del 2026-10-09): la última de cada pantalla, sus botones y sus cuias en píxeles con el origen arriba a la
    /// izquierda (DevProbe.Layout), si el centro publicado cae en lo que se ve ahora y el «toque» en ese centro, que en el
    /// teléfono es <c>adb shell input tap x y</c>. Se crea en el SetUp y se suelta en el TearDown.
    /// </summary>
    internal sealed class AfLayoutLog : IDisposable
    {
        private const string Prefix = "AF_LAYOUT ";

        private static readonly Regex ButtonRx = new Regex("\\{\"id\":\"([a-z_]+)\",\"x\":(-?\\d+),\"y\":(-?\\d+),\"w\":(-?\\d+),\"h\":(-?\\d+)\\}");
        private static readonly Regex CuiaRx = new Regex("\\{\"i\":(\\d+),\"x\":(-?\\d+),\"y\":(-?\\d+),\"w\":(-?\\d+),\"h\":(-?\\d+)\\}");

        private readonly List<string> _lines = new List<string>();

        public AfLayoutLog()
        {
            Application.logMessageReceived += OnLog;
        }

        /// <summary>Líneas capturadas desde el último <see cref="Clear"/>.</summary>
        public int Count => _lines.Count;

        /// <summary>La última línea AF_LAYOUT, de cualquier pantalla (la que usaría quien toca por adb), o null.</summary>
        public string? Last => _lines.Count == 0 ? null : _lines[_lines.Count - 1];

        public void Dispose()
        {
            Application.logMessageReceived -= OnLog;
        }

        public void Clear()
        {
            _lines.Clear();
        }

        /// <summary>La última línea de la pantalla <paramref name="screen"/> en el nivel <paramref name="level"/>, o null.</summary>
        public string? LastOf(string screen, int level)
        {
            string tag = Tag(screen, level);
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                if (_lines[i].StartsWith(tag, StringComparison.Ordinal))
                {
                    return _lines[i];
                }
            }

            return null;
        }

        /// <summary>Líneas de la pantalla <paramref name="screen"/> en el nivel <paramref name="level"/>, en orden.</summary>
        public List<string> AllOf(string screen, int level)
        {
            string tag = Tag(screen, level);
            var result = new List<string>();
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].StartsWith(tag, StringComparison.Ordinal))
                {
                    result.Add(_lines[i]);
                }
            }

            return result;
        }

        /// <summary>La última línea, que tiene que ser de <paramref name="screen"/> en el nivel <paramref name="level"/>.</summary>
        public string RequireLast(string screen, int level, string what)
        {
            string? last = Last;
            Assert.That(last, Is.Not.Null, what + ": ninguna AF_LAYOUT");
            Assert.That(last!.StartsWith(Tag(screen, level), StringComparison.Ordinal), Is.True, what + ": la última AF_LAYOUT es de «" + screen + "» del " + level + ": " + last);
            return last;
        }

        /// <summary>Ids de los botones de la línea, en orden.</summary>
        public static string[] ButtonIds(string line)
        {
            MatchCollection all = ButtonRx.Matches(line);
            var ids = new string[all.Count];
            for (int i = 0; i < all.Count; i++)
            {
                ids[i] = all[i].Groups[1].Value;
            }

            return ids;
        }

        /// <summary>Rectángulo publicado del botón <paramref name="id"/> (x, y desde arriba, w, h en píxeles), o null.</summary>
        public static RectInt? Button(string line, string id)
        {
            foreach (Match m in ButtonRx.Matches(line))
            {
                if (m.Groups[1].Value == id)
                {
                    return ToRect(m);
                }
            }

            return null;
        }

        /// <summary>Rectángulos publicados de las cuias, por índice.</summary>
        public static RectInt[] Cuias(string line)
        {
            MatchCollection all = CuiaRx.Matches(line);
            var rects = new RectInt[all.Count];
            for (int i = 0; i < all.Count; i++)
            {
                Assert.That(int.Parse(all[i].Groups[1].Value, CultureInfo.InvariantCulture), Is.EqualTo(i), "cuias en orden");
                rects[i] = ToRect(all[i]);
            }

            return rects;
        }

        /// <summary>Lo que DevProbe publicaría ahora para <paramref name="rt"/> (mismo redondeo que DevProbe.AppendRect).</summary>
        public static RectInt Published(RectTransform rt)
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float minX = Mathf.Min(corners[0].x, corners[2].x);
            float maxX = Mathf.Max(corners[0].x, corners[2].x);
            float minY = Mathf.Min(corners[0].y, corners[2].y);
            float maxY = Mathf.Max(corners[0].y, corners[2].y);
            return new RectInt(Mathf.RoundToInt(minX), Mathf.RoundToInt(Screen.height - maxY), Mathf.RoundToInt(maxX - minX), Mathf.RoundToInt(maxY - minY));
        }

        /// <summary>Centro publicado del botón <paramref name="id"/> en coordenadas de pantalla de Unity (origen abajo a la izquierda).</summary>
        public static Vector2 Center(string line, string id)
        {
            RectInt? r = Button(line, id);
            Assert.That(r.HasValue, Is.True, "AF_LAYOUT con «" + id + "»: " + line);
            RectInt rect = r!.Value;
            return new Vector2(rect.x + (rect.width * 0.5f), Screen.height - (rect.y + (rect.height * 0.5f)));
        }

        /// <summary>
        /// El centro publicado del botón <paramref name="id"/> cae dentro de <paramref name="rt"/> tal como está ahora (lo que
        /// se toca por adb es lo que se ve) y a ±<paramref name="tolerancePx"/> px de su centro real.
        /// </summary>
        public static void AssertCenterInside(string line, string id, RectTransform rt, float tolerancePx = 2f)
        {
            Assert.That(rt.gameObject.activeInHierarchy, Is.True, id + ": a la vista");
            Vector2 center = Center(line, id);
            Rect world = WorldRect(rt);
            Assert.That(world.Contains(center), Is.True, id + ": el centro publicado " + center + " cae dentro de " + world + " (" + line + ")");
            Assert.That(Vector2.Distance(center, world.center), Is.LessThanOrEqualTo(tolerancePx), id + ": a ±" + tolerancePx + " px del centro dibujado");
        }

        /// <summary>
        /// Toca <paramref name="button"/> en el centro que publicó la línea para <paramref name="id"/>, como
        /// <c>adb shell input tap</c>: el centro tiene que caer dentro del botón y el clic llega por el EventSystem (Button
        /// comprueba que esté activo e interactivo).
        /// </summary>
        public static void Tap(string line, string id, Button button)
        {
            AssertCenterInside(line, id, (RectTransform)button.transform);
            var tap = new PointerEventData(EventSystem.current) { position = Center(line, id), button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, tap, ExecuteEvents.pointerClickHandler);
        }

        public static Rect WorldRect(RectTransform rt)
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(
                Mathf.Min(corners[0].x, corners[2].x),
                Mathf.Min(corners[0].y, corners[2].y),
                Mathf.Max(corners[0].x, corners[2].x),
                Mathf.Max(corners[0].y, corners[2].y));
        }

        private static string Tag(string screen, int level)
        {
            return Prefix + "{\"screen\":\"" + screen + "\",\"level\":" + level.ToString(CultureInfo.InvariantCulture) + ",";
        }

        private static RectInt ToRect(Match m)
        {
            return new RectInt(
                int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture),
                int.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture));
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition.StartsWith(Prefix, StringComparison.Ordinal))
            {
                _lines.Add(condition);
            }
        }
    }
}
