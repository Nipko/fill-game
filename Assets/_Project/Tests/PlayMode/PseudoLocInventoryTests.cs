#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
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
    /// 100 %, recorre el inicio (con el botón Jugar de un nivel Difícil y su cinta), la tarjeta previa, el banner de mundo, la
    /// victoria, Aguas estancadas (ramas A, B y C), ¿Seguro? y Nivel fallido, y <b>lista</b> sin fallar cada texto visible que
    /// se sale de su caja o, en la cinta «×3»/«×4», de su franja de texto, que no cabe ni al tamaño mínimo del autoajuste, que
    /// baja de 12 sp (02 §12) o que, en en-XA, sale sin corchetes (no viene de la tabla). Los arregla L4.
    /// La misma pasada en es-419 es la referencia: lo que ya pasa en español no es de la traducción.
    /// <para>Deja en <c>Logs/snapshots/&lt;idioma&gt;/</c> una captura por pantalla (<c>inv_*.png</c>) y el inventario
    /// (<c>inventory.txt</c>, una línea por texto con problemas; también en el log, <c>AF_LOC_INV</c>). Explícito: necesita
    /// gráficos, como las capturas (<c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.PseudoLocInventoryTests</c> sin <c>-nographics</c>).</para>
    /// </summary>
    [Explicit("Inventario de desbordes en en-XA (L2b); necesita dispositivo gráfico. No falla: lo arregla L4.")]
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
            FunnelSetup.Hooks(); // antes de Prepare: limpia los ganchos y fijaría es-419
            string dir = SnapshotKit.Prepare(locale);
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            var inventory = new LocInventory(locale);

            // Inicio: nivel 4 guardado con 1.240 semillas y, con af_level_offset = 30, el 31 con 3/5 canoas y su cronómetro.
            Profile p4 = Wallet.NewProfile(FunnelSetup.Clock);
            p4.Progress.NextLevel = 4;
            p4.Progress.MaxLevel = 3;
            p4.Wallet.Seeds = 1240;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p4));
            yield return Shoot(dir, inventory, "inv_inicio_l4", lobby: true);

            LaunchOptions.LevelOffsetOverride = 30;
            Profile p31 = FunnelSetup.Profile(2380, 3);
            p31.Progress.NextLevel = 31;
            p31.Progress.MaxLevel = 30;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p31));
            yield return Shoot(dir, inventory, "inv_inicio_l31", lobby: true);
            LaunchOptions.LevelOffsetOverride = null;

            // Inicio en el 40 (Difícil): el botón Jugar con el chip y la cinta «×3» del premio multiplicado (D-009; 07 §7.7).
            Profile p40 = Wallet.NewProfile(FunnelSetup.Clock);
            p40.Progress.NextLevel = 40;
            p40.Progress.MaxLevel = 39;
            p40.Wallet.Seeds = 1240;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p40));
            Assert.That(GameBootstrap.Current!.Lobby!.ChipRibbonText, Is.Not.Empty, "la cinta del botón Jugar en el 40");
            yield return Shoot(dir, inventory, "inv_inicio_l40", lobby: true);

            // Tarjeta previa: Difícil (40), el estreno combinado de Aguas turbias (100) y el de la Piraña (183).
            foreach (int level in new[] { 40, 100, 183 })
            {
                yield return CardSetup.Load(level);
                Assert.That(GameBootstrap.Current!.View!.LevelCard.Visible, Is.True, "tarjeta del " + level);
                yield return Shoot(dir, inventory, "inv_tarjeta_l" + level.ToString(CultureInfo.InvariantCulture), lobby: false);
            }

            // Banner de mundo: el 41, ya quieto.
            yield return CardSetup.Load(41);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.WorldBannerView.Visible, Is.True);
            PlayModeHelpers.TickExact(boot, 1100);
            yield return Shoot(dir, inventory, "inv_banner_l41", lobby: false);

            // Victoria del 11 (tablero 1 con af_level_offset = 10): Cosecha y Cosecha doble.
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            boot = GameBootstrap.Current!;
            yield return WinRealTime(boot);
            yield return SnapshotKit.Wait(1.2f);
            yield return Shoot(dir, inventory, "inv_victoria_l11", lobby: false);

            // Aguas estancadas en el 31 (tablero 6 con af_level_offset = 25): rama A con 1.240 semillas, ¿Seguro? y Nivel fallido.
            yield return LoadFunnel(31, 1240);
            boot = GameBootstrap.Current!;
            yield return StuckRealTime(boot);
            yield return Shoot(dir, inventory, "inv_aguas_estancadas_l31_a", lobby: false);
            boot.PressGiveUp();
            yield return SnapshotKit.Wait(0.3f);
            yield return Shoot(dir, inventory, "inv_seguro_l31", lobby: false);
            boot.ConfirmGiveUp();
            yield return SnapshotKit.Wait(0.3f);
            yield return Shoot(dir, inventory, "inv_nivel_fallido_l31", lobby: false);

            // Rama B (sin saldo: Puñado y Balsa) y rama C (nivel 6, antes de las canoas).
            yield return LoadFunnel(31, 140);
            boot = GameBootstrap.Current!;
            yield return StuckRealTime(boot);
            yield return Shoot(dir, inventory, "inv_aguas_estancadas_l31_b", lobby: false);

            LaunchOptions.LevelOffsetOverride = null;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(310));
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore(6, 5));
            boot = GameBootstrap.Current!;
            yield return StuckRealTime(boot);
            yield return Shoot(dir, inventory, "inv_aguas_estancadas_l6_c", lobby: false);

            string report = inventory.Write(dir);
            Debug.Log("AF_LOC_INV " + locale + ": " + inventory.Scanned.ToString(CultureInfo.InvariantCulture) + " textos mirados, "
                + inventory.Problems.ToString(CultureInfo.InvariantCulture) + " con problemas; " + report);
            Assert.That(inventory.Scanned, Is.GreaterThan(50), "textos visibles mirados");
            Assert.That(File.Exists(report), Is.True);
        }

        // Pinta la pantalla en la cámara de las capturas, guarda el PNG y apunta sus textos.
        private static IEnumerator Shoot(string dir, LocInventory inventory, string name, bool lobby)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            if (lobby)
            {
                SnapshotKit.AttachLobby(boot);
            }
            else
            {
                SnapshotKit.Attach(boot);
            }

            if (boot.AdOverlayView != null)
            {
                SnapshotKit.AttachOverlay(boot.AdOverlayView.Canvas);
            }

            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            inventory.Scan(name);
        }

        private static IEnumerator LoadFunnel(int level, int seeds)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds, 3));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
        }

        // Tablero 6: 3 → 4 deja Aguas estancadas; espera al modal y a que el panel termine de subir.
        private static IEnumerator StuckRealTime(GameBootstrap boot)
        {
            boot.TapAnywhere();
            boot.TapCuia(3);
            boot.TapCuia(4);
            LevelFlow flow = boot.Flow!;
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.StuckModal, "modal");
            yield return SnapshotKit.Wait(0.45f);
        }

        // Victoria con la solución del solver, en tiempo real.
        private static IEnumerator WinRealTime(GameBootstrap boot)
        {
            boot.TapAnywhere();
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            for (int i = 0; i < solution.MoveCount; i++)
            {
                boot.TapCuia(solution.GetMove(i).Source);
                boot.TapCuia(solution.GetMove(i).Target);
            }

            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.VictoryScreen, "victoria");
        }
    }

    /// <summary>
    /// Lo que apunta <see cref="PseudoLocInventoryTests"/> de cada pantalla: los textos visibles (activos, con alfa) y, de cada
    /// uno con problemas, qué le pasa, su ruta en la jerarquía, su tamaño de letra, su caja y su texto.
    /// </summary>
    internal sealed class LocInventory
    {
        /// <summary>Margen en unidades del lienzo (dp) antes de contar un texto como fuera de su caja.</summary>
        public const float Tolerance = 1f;

        /// <summary>Tamaño mínimo de texto (02 §12): 12 sp.</summary>
        public const float MinSp = 12f;

        private readonly string _locale;
        private readonly List<string> _lines = new List<string>();

        public LocInventory(string locale)
        {
            _locale = locale;
        }

        /// <summary>Textos visibles mirados.</summary>
        public int Scanned { get; private set; }

        /// <summary>Textos con algún problema.</summary>
        public int Problems => _lines.Count;

        /// <summary>Mira los textos visibles de la escena y apunta los que tienen problemas, con el nombre de la pantalla.</summary>
        public void Scan(string screen)
        {
            Canvas.ForceUpdateCanvases();
            TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var found = new List<string>();
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
                if (problems.Length == 0)
                {
                    continue;
                }

                Rect rect = text.rectTransform.rect;
                found.Add(screen + "\t" + PathOf(text.transform) + "\t" + problems + "\t" + text.fontSize.ToString("0.#", CultureInfo.InvariantCulture)
                    + " sp\t" + rect.width.ToString("0.#", CultureInfo.InvariantCulture) + " × " + rect.height.ToString("0.#", CultureInfo.InvariantCulture)
                    + " dp\t«" + text.text.Replace("\n", "\\n") + "»");
            }

            found.Sort(StringComparer.Ordinal);
            _lines.AddRange(found);
        }

        /// <summary>Escribe <c>inventory.txt</c> en <paramref name="dir"/> y devuelve su ruta.</summary>
        public string Write(string dir)
        {
            var sb = new StringBuilder();
            sb.Append("# Inventario de textos (L2b): ").Append(_locale).Append(", 360 × 640 dp (lienzo de 438,75 × 780 dp), fuente al 100 %\n");
            sb.Append("# Textos visibles mirados: ").Append(Scanned.ToString(CultureInfo.InvariantCulture))
                .Append("; con problemas: ").Append(_lines.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("# pantalla\truta\tproblemas\ttamaño\tcaja\ttexto\n");
            foreach (string line in _lines)
            {
                sb.Append(line).Append('\n');
                Debug.Log("AF_LOC_INV " + _locale + "\t" + line);
            }

            string path = Path.Combine(dir, "inventory.txt");
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

            if (text.fontSize < MinSp - 0.01f)
            {
                problems.Add("menos de 12 sp");
            }

            if (text.name == "Tutorial" && text.textInfo.lineCount > LevelCardView.MaxTutorialLines)
            {
                problems.Add("tutorial en " + text.textInfo.lineCount.ToString(CultureInfo.InvariantCulture) + " líneas (02 §12: ≤ 2)");
            }

            if (_locale == LocLocale.Pseudo && HasLetters(text.text) && text.text.IndexOf('[') < 0)
            {
                problems.Add("sin corchetes (no viene de la tabla)");
            }

            return string.Join("; ", problems);
        }

        // Ancho de la franja de texto de la cinta (dp) si el texto es la cifra de una cinta «×3»/«×4» («Ribbon/Value»); 0 si no.
        // La escala sale del alto del sprite de la cinta (RibbonTagHeightDp al 100 %; 0,75 en el chip compacto, × la escala de
        // texto en la tarjeta previa) y la franja es el ancho de la cinta menos el remache y la cola (RibbonTagWidthDp −
        // RibbonTagTextDp.width, 11 dp al 100 %): así sigue valiendo si L4 alarga la cinta para que quepa la cifra.
        private static float RibbonBandWidth(TMP_Text text)
        {
            Transform? parent = text.transform.parent;
            if (text.name != "Value" || parent == null || parent.name != "Ribbon" || !(parent is RectTransform ribbon))
            {
                return 0f;
            }

            float scale = ribbon.rect.height / ProceduralArt.RibbonTagHeightDp;
            return ribbon.rect.width - ((ProceduralArt.RibbonTagWidthDp - ProceduralArt.RibbonTagTextDp.width) * scale);
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
