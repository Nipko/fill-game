#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Inventario de desbordes en en-XA (D-018, nivel 2; propuesta de idiomas §3.7 y §3.8; tarea L2b): con la
    /// pseudolocalización (el español alargado un 35 % y entre corchetes; <see cref="LocPseudo"/>), a 360 × 640 dp (la pantalla
    /// 16:9 de 02 §5, que <c>GameScreen</c> maqueta en 438,75 × 780 dp; <see cref="HudLabelTests.Screen640"/>) y con la fuente al
    /// 100 %, recorre las pantallas de <see cref="PseudoLocWalk"/> (el inicio con el botón Jugar de un nivel Difícil y su cinta,
    /// la tarjeta previa, el banner de mundo, la bandeja de comodines, la victoria, Aguas estancadas en las ramas A, B y C,
    /// ¿Seguro? y Nivel fallido) y <b>lista</b> sin fallar cada texto visible que se sale de su caja o, en la cinta «×3»/«×4», de
    /// su franja de texto, que no cabe ni al tamaño mínimo del autoajuste, que baja de 12 sp (02 §12) o que, en en-XA, sale sin
    /// corchetes (no viene de la tabla). Desde L4, la compuerta que falla es <see cref="PseudoLocOverflowTests"/> (en-XA al 100 %
    /// y al 130 %).
    /// La misma pasada en es-419 es la referencia: lo que ya pasa en español no es de la traducción.
    /// <para>Deja en <c>Logs/snapshots/&lt;idioma&gt;/</c> una captura por pantalla (<c>inv_*.png</c>) y el inventario
    /// (<c>inventory.txt</c>, una línea por texto con problemas; también en el log, <c>AF_LOC_INV</c>). Explícito: necesita
    /// gráficos, como las capturas (<c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.PseudoLocInventoryTests</c> sin <c>-nographics</c>).</para>
    /// </summary>
    [Explicit("Inventario de desbordes en en-XA (L2b); necesita dispositivo gráfico. No falla: la compuerta es PseudoLocOverflowTests (L4).")]
    public sealed class PseudoLocInventoryTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Inventory_EnXA_360x640_100()
        {
            yield return Walk(LocLocale.Pseudo);
        }

        [UnityTest]
        public IEnumerator Inventory_Es419_360x640_100()
        {
            yield return Walk(LocLocale.SourceLocale);
        }

        private static IEnumerator Walk(string locale)
        {
            string dir = SnapshotKit.Prepare(locale);
            var inventory = new LocInventory(locale, 1f);
            yield return PseudoLocWalk.Run(locale, 1f, dir, inventory);
            string report = inventory.Write(dir, "inventory.txt");
            Debug.Log("AF_LOC_INV " + locale + ": " + inventory.Scanned.ToString(CultureInfo.InvariantCulture) + " textos mirados, "
                + inventory.Problems.ToString(CultureInfo.InvariantCulture) + " con problemas; " + report);
            Assert.That(inventory.Scanned, Is.GreaterThan(50), "textos visibles mirados");
            Assert.That(File.Exists(report), Is.True);
        }
    }

    /// <summary>
    /// Lo que apuntan <see cref="PseudoLocInventoryTests"/> y <see cref="PseudoLocOverflowTests"/> de cada pantalla: los textos visibles (activos, con alfa) y, de cada
    /// uno con problemas, qué le pasa, su ruta en la jerarquía, su tamaño de letra, su caja y su texto.
    /// </summary>
    internal sealed class LocInventory
    {
        /// <summary>Margen en unidades del lienzo (dp) antes de contar un texto como fuera de su caja.</summary>
        public const float Tolerance = 1f;

        /// <summary>Tamaño mínimo de texto (02 §12): 12 sp, que con la escala de fuente son 12 × <see cref="Scale"/> dp.</summary>
        public const float MinSp = 12f;

        private readonly string _locale;
        private readonly List<string> _lines = new List<string>();
        private readonly List<string> _notes = new List<string>();
        private readonly List<string> _known = new List<string>();

        public LocInventory(string locale, float scale)
        {
            _locale = locale;
            Scale = scale;
        }

        /// <summary>Escala de la fuente de la pasada (1 = 100 %; 02 §12: hasta 1,3).</summary>
        public float Scale { get; }

        /// <summary>Una línea por texto con problemas (pantalla, ruta, problemas, tamaño, caja y texto).</summary>
        public IReadOnlyList<string> Lines => _lines;

        /// <summary>Textos visibles mirados.</summary>
        public int Scanned { get; private set; }

        /// <summary>Textos con algún problema.</summary>
        public int Problems => _lines.Count;

        /// <summary>
        /// Los problemas de un solo texto, como los apunta <see cref="Scan"/> (vacío si no tiene ninguno): para los tests que
        /// miran una pieza concreta (tarea L4).
        /// </summary>
        public string ProblemsOf(TMP_Text text)
        {
            Canvas.ForceUpdateCanvases();
            text.ForceMeshUpdate();
            return Describe(text);
        }

        /// <summary>
        /// Avisos de contenido que no son desbordes (L4): el tutorial de la tarjeta previa en más de 2 líneas a 100 % (02 §12: es
        /// un límite del texto, que su caja ya acoge sin salirse; a 130 % puede pasar de 2 por diseño: <c>LevelCardView</c>).
        /// </summary>
        public IReadOnlyList<string> Notes => _notes;

        /// <summary>
        /// Desbordes pendientes del dueño, que no cuentan en <see cref="Lines"/> (revisión de L4): el texto de la insignia del
        /// nivel («LevelBadge/LevelTitle») que no cabe en su placa a 20 sp (× escala), el mínimo de la guía 3.5 (D-003). Pendiente
        /// de rediseño: la placa no puede crecer y bajar de 20 sp contradice la guía. Solo si el texto sigue a 20 sp (× escala) o
        /// más: por debajo, es un problema como cualquier otro.
        /// </summary>
        public IReadOnlyList<string> KnownIssues => _known;

        /// <summary>Mínimo del texto de la insignia del nivel (guía 3.5: 26 dp, mínimo 20; nunca se recorta).</summary>
        public const float BadgeMinSp = 20f;

        /// <summary>Mira los textos visibles de la escena y apunta los que tienen problemas, con el nombre de la pantalla.</summary>
        public void Scan(string screen)
        {
            Canvas.ForceUpdateCanvases();
            TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var found = new List<string>();
            var notes = new List<string>();
            var known = new List<string>();
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text) || !Visible(text))
                {
                    continue;
                }

                text.ForceMeshUpdate();
                Scanned++;
                string problems = Describe(text);
                if (problems.Length > 0)
                {
                    if (IsBadgeAtGuideMin(text))
                    {
                        known.Add(Line(screen, text, "pendiente del dueño (insignia a 20 sp, guía 3.5): " + problems));
                    }
                    else
                    {
                        found.Add(Line(screen, text, problems));
                    }
                }

                if (text.name == "Tutorial" && Scale <= 1f && text.textInfo.lineCount > LevelCardView.MaxTutorialLines)
                {
                    notes.Add(Line(screen, text, "tutorial en " + text.textInfo.lineCount.ToString(CultureInfo.InvariantCulture) + " líneas (02 §12: ≤ 2)"));
                }
            }

            found.Sort(StringComparer.Ordinal);
            _lines.AddRange(found);
            notes.Sort(StringComparer.Ordinal);
            _notes.AddRange(notes);
            known.Sort(StringComparer.Ordinal);
            _known.AddRange(known);
        }

        // El texto de la insignia del nivel a su mínimo de la guía 3.5 o más (KnownIssues).
        private bool IsBadgeAtGuideMin(TMP_Text text)
        {
            Transform? parent = text.transform.parent;
            return text.name == "LevelTitle" && parent != null && parent.name == "LevelBadge" && text.fontSize >= (BadgeMinSp * Scale) - 0.01f;
        }

        private static string Line(string screen, TMP_Text text, string what)
        {
            Rect rect = text.rectTransform.rect;
            return screen + "\t" + PathOf(text.transform) + "\t" + what + "\t" + text.fontSize.ToString("0.#", CultureInfo.InvariantCulture)
                + " sp\t" + rect.width.ToString("0.#", CultureInfo.InvariantCulture) + " × " + rect.height.ToString("0.#", CultureInfo.InvariantCulture)
                + " dp\t«" + text.text.Replace("\n", "\\n") + "»";
        }

        /// <summary>Escribe el inventario en <paramref name="dir"/>/<paramref name="fileName"/> y devuelve su ruta.</summary>
        public string Write(string dir, string fileName)
        {
            var sb = new StringBuilder();
            sb.Append("# Inventario de textos (L2b y L4): ").Append(_locale).Append(", 360 × 640 dp (lienzo de 438,75 × 780 dp), fuente al ")
                .Append(Mathf.RoundToInt(Scale * 100f).ToString(CultureInfo.InvariantCulture)).Append(" %\n");
            sb.Append("# Textos visibles mirados: ").Append(Scanned.ToString(CultureInfo.InvariantCulture))
                .Append("; con problemas: ").Append(_lines.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("# pantalla\truta\tproblemas\ttamaño\tcaja\ttexto\n");
            foreach (string line in _lines)
            {
                sb.Append(line).Append('\n');
                Debug.Log("AF_LOC_INV " + _locale + "\t" + line);
            }

            sb.Append("# Pendientes del dueño (no cuentan en la compuerta): ").Append(_known.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (string line in _known)
            {
                sb.Append(line).Append('\n');
            }

            sb.Append("# Avisos de contenido (no son desbordes): ").Append(_notes.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (string note in _notes)
            {
                sb.Append(note).Append('\n');
            }

            string path = Path.Combine(dir, fileName);
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }

        private string Describe(TMP_Text text)
        {
            var problems = new List<string>();
            Rect rect = text.rectTransform.rect;
            Bounds bounds = text.textBounds;
            bool wide = bounds.size.x > 0f && (bounds.min.x < rect.xMin - Tolerance || bounds.max.x > rect.xMax + Tolerance);

            // A lo alto: con varias líneas, lo que dice TextMeshPro; con una, solo si la línea se sale más de un cuarto de su
            // tamaño (una etiqueta de una línea en una caja justa no es un desborde: el ascendente y el descendente sobran).
            float excess = bounds.size.y > 0f ? Mathf.Max(rect.yMin - bounds.min.y, bounds.max.y - rect.yMax) : 0f;
            bool tall = text.textInfo.lineCount > 1
                ? text.isTextOverflowing || excess > Tolerance
                : excess > Mathf.Max(Tolerance, 0.25f * text.fontSize);
            if (wide)
            {
                problems.Add("se sale a lo ancho");
            }

            // La cinta «×3»/«×4» (DifficultyChip en el botón Jugar y en el HUD; LevelCardView en la tarjeta previa): la cifra
            // tiene que caber en la franja de texto del sprite (ProceduralArt.RibbonTagTextDp: entre el remache y la cola en V),
            // aunque su caja sea más ancha (LevelCardView la agranda a su texto y la centra en la franja). Si no, tapa la muesca
            // y el remache y la cinta deja de leerse como banderín (D-003).
            float band = RibbonBandWidth(text);
            if (band > 0f && bounds.size.x > band + Tolerance)
            {
                problems.Add("se sale de la franja de la cinta (" + bounds.size.x.ToString("0.#", CultureInfo.InvariantCulture) + " > "
                    + band.ToString("0.#", CultureInfo.InvariantCulture) + " dp)");
            }

            if (tall)
            {
                problems.Add("se sale a lo alto");
            }

            if (text.enableAutoSizing && text.fontSize <= text.fontSizeMin + 0.01f && (wide || tall))
            {
                problems.Add("no cabe ni al mínimo del autoajuste");
            }

            if (text.fontSize < (MinSp * Scale) - 0.01f)
            {
                problems.Add("menos de 12 sp");
            }

            if (_locale == LocLocale.Pseudo && HasLetters(text.text) && text.text.IndexOf('[') < 0)
            {
                problems.Add("sin corchetes (no viene de la tabla)");
            }

            int broken = BrokenWordLine(text);
            if (broken > 0)
            {
                problems.Add("se parte a media palabra en la línea " + broken.ToString(CultureInfo.InvariantCulture));
            }

            return string.Join("; ", problems);
        }

        /// <summary>
        /// Primera línea (desde 1) de <paramref name="text"/> que acaba por dentro de una palabra, o 0 si ninguna (prueba de la
        /// ola 3 en el teléfono: «Reinici / ar» al 130 %; tarea FX5). Solo con el corte de líneas activado
        /// (<see cref="TextWrappingModes.Normal"/> o <see cref="TextWrappingModes.PreserveWhitespace"/>): cada línea, salvo la
        /// última, acaba en un espacio o un salto, o justo antes de uno, o tras un guion (los compuestos como
        /// «sauim-de-coleira» se pueden cortar ahí). Lee <c>textInfo</c> tal como está: quien llama fuerza antes la malla.
        /// </summary>
        public static int BrokenWordLine(TMP_Text text)
        {
            if (text.textWrappingMode != TextWrappingModes.Normal && text.textWrappingMode != TextWrappingModes.PreserveWhitespace)
            {
                return 0;
            }

            TMP_TextInfo info = text.textInfo;
            for (int i = 0; i < info.lineCount - 1; i++)
            {
                int last = info.lineInfo[i].lastCharacterIndex;
                if (last < 0 || last + 1 >= info.characterCount)
                {
                    continue;
                }

                char end = info.characterInfo[last].character;
                char next = info.characterInfo[last + 1].character;
                if (!char.IsWhiteSpace(end) && !char.IsWhiteSpace(next) && end != '-' && end != '­' && end != '‐' && end != '​')
                {
                    return i + 1;
                }
            }

            return 0;
        }

        // Ancho de la franja de texto de la cinta (dp) si el texto es la cifra de una cinta «×3»/«×4» («Ribbon/Value»); 0 si no.
        // La escala sale del alto del sprite de la cinta (RibbonTagHeightDp al 100 %; 0,75 en el chip compacto, × la escala de
        // texto en la tarjeta previa) y la franja es la de DifficultyChip.RibbonTextBandFor: RibbonTagTextDp a su ancho de
        // siempre y, en la cinta alargada (L4), entre el clavo y la cola, sin pisar ninguno de los dos.
        private static float RibbonBandWidth(TMP_Text text)
        {
            Transform? parent = text.transform.parent;
            if (text.name != "Value" || parent == null || parent.name != "Ribbon" || !(parent is RectTransform ribbon))
            {
                return 0f;
            }

            float scale = ribbon.rect.height / ProceduralArt.RibbonTagHeightDp;
            return DifficultyChip.RibbonTextBandFor(ribbon.rect.width / scale).width * scale;
        }

        private static bool Visible(TMP_Text text)
        {
            if (text.color.a < 0.01f || text.canvasRenderer.GetInheritedAlpha() < 0.01f)
            {
                return false;
            }

            Canvas? canvas = text.canvas;
            return canvas != null && canvas.isActiveAndEnabled;
        }

        private static bool HasLetters(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsLetter(s[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (Transform? at = t; at != null; at = at.parent)
            {
                parts.Add(at.name);
            }

            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
