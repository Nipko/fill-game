#nullable enable
using System.Collections.Generic;
using System.IO;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Levels;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>Datos compartidos por los tests EditMode.</summary>
    internal static class TestPacks
    {
        /// <summary>Ruta del paquete del tutorial (docs/dev/prototipo-v0.md b).</summary>
        public static string TutorialPath => Path.Combine(Application.dataPath, "_Project/Data/Levels/tutorial_v0.json");

        /// <summary>
        /// Rutas de los 6 paquetes de la escena, en el orden de <c>GameSceneBuilder.LevelPackPaths</c>: el tutorial (1-10), la
        /// campaña 11-200 de <c>pack_schema</c> 2 (traspaso §6 pasos 1.1-1.2) y el mundo 6 (201-300, <c>pack_schema</c> 3; tarea
        /// C1a).
        /// </summary>
        public static string[] CampaignPaths => new[]
        {
            TutorialPath,
            Path.Combine(Application.dataPath, "_Project/Data/Levels/mundo_02_v1.json"),
            Path.Combine(Application.dataPath, "_Project/Data/Levels/mundo_03_v1.json"),
            Path.Combine(Application.dataPath, "_Project/Data/Levels/mundo_04_v1.json"),
            Path.Combine(Application.dataPath, "_Project/Data/Levels/mundo_05_v1.json"),
            Path.Combine(Application.dataPath, "_Project/Data/Levels/mundo_06_v1.json"),
        };

        private static LevelPack? _tutorial;
        private static LevelPack[]? _campaign;

        /// <summary>Paquete <c>tutorial_v0</c> leído del disco (una vez).</summary>
        public static LevelPack Tutorial => _tutorial ??= LevelPackReader.Read(File.ReadAllText(TutorialPath));

        /// <summary>Los 6 paquetes de <see cref="CampaignPaths"/> leídos del disco (una vez).</summary>
        public static IReadOnlyList<LevelPack> Campaign
        {
            get
            {
                if (_campaign == null)
                {
                    string[] paths = CampaignPaths;
                    var packs = new LevelPack[paths.Length];
                    for (int i = 0; i < paths.Length; i++)
                    {
                        packs[i] = LevelPackReader.Read(File.ReadAllText(paths[i]));
                    }

                    _campaign = packs;
                }

                return _campaign;
            }
        }

        /// <summary>Paquete mínimo válido con un nivel; <paramref name="levelJson"/> sustituye al nivel por defecto.</summary>
        public static string MinimalPack(string? levelJson = null, string header = "\"subset_of\": \"af-level/1\"")
        {
            string level = levelJson ?? MinimalLevel();
            return "{ " + header + ", \"pack_id\": \"t\", \"kind\": \"campaign\", \"world\": 1, \"world_id\": \"w01_estuario\", "
                + "\"lap\": 1, \"water_base\": \"white\", \"levels\": [ " + level + " ] }";
        }

        /// <summary>
        /// Nivel 1 de 02 §13 con los campos sustituibles; <paramref name="extra"/> añade campos al final del nivel (p. ej.
        /// <c>, "intro_mechanic": "hoja"</c>).
        /// </summary>
        public static string MinimalLevel(
            string cuias = "[[8, 8, 8, 4], [4, 4, 4, 8], []]",
            string caps = "[4, 4, 4]",
            string attrs = "[null, null, null]",
            string murky = "false",
            string hidden = "[]",
            string units = "4",
            string extra = "")
        {
            return "{ \"id\": \"m01_v1_n0001\", \"level\": 1, \"world\": 1, \"lap\": 1, \"water_base\": \"white\", "
                + "\"palette\": [\"acai\", \"aguaje\"], \"units_per_color\": " + units + ", \"caps\": " + caps
                + ", \"cuias\": " + cuias + ", \"attrs\": " + attrs + ", \"murky\": " + murky + ", \"hidden\": " + hidden
                + ", \"difficulty\": { \"par\": 3 }, \"relief\": []" + extra + " }";
        }

        /// <summary>
        /// Paquete <c>pack_schema</c> 2 (D-007; contrato 21) de un nivel con el tablero, las mecánicas (<paramref name="attrs"/>),
        /// las dosis ocultas (<paramref name="murky"/> y <paramref name="hidden"/>) y <c>units_per_color</c>
        /// (<paramref name="units"/>) indicados; por defecto, el nivel 1 de 02 §13 con una cuia vacía más (4 cuias). Con
        /// <paramref name="schema"/> = 1 sirve para probar lo que el esquema 1 no admite.
        /// </summary>
        public static string Schema2Pack(
            string attrs = "[null, null, null, null]",
            string cuias = "[[8, 8, 8, 4], [4, 4, 4, 8], [], []]",
            string caps = "[4, 4, 4, 4]",
            string murky = "false",
            string hidden = "[]",
            string units = "4",
            string extra = "",
            int schema = 2)
        {
            return MinimalPack(
                MinimalLevel(cuias, caps, attrs, murky, hidden, units, extra),
                "\"pack_schema\": " + schema.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Oyente que registra los eventos de <see cref="LevelFlow"/> en orden, también los avisos opcionales de la tarjeta previa
    /// (<see cref="ILevelCardListener"/>: <c>card:{Kind}</c>) y de la Luciérnaga (<see cref="IRevealListener"/>:
    /// <c>reveal_gift:{n}</c>, <c>reveal_select:on|off</c> y <c>revealed:{cuia}:{máscara}</c>).
    /// </summary>
    internal sealed class RecordingListener : ILevelFlowListener, ILevelCardListener, IRevealListener
    {
        public readonly List<string> Events = new List<string>();

        public readonly List<TapResult> Taps = new List<TapResult>();

        public readonly List<LevelCardInfo> Cards = new List<LevelCardInfo>();

        public void OnLevelCard(LevelCardInfo card)
        {
            Cards.Add(card);
            Events.Add("card:" + card.Kind);
        }

        public void OnRevealGifted(int units)
        {
            Events.Add("reveal_gift:" + units);
        }

        public void OnRevealSelection(bool selecting)
        {
            Events.Add(selecting ? "reveal_select:on" : "reveal_select:off");
        }

        public void OnRevealed(int cuia, int layerMask)
        {
            Events.Add("revealed:" + cuia + ":" + layerMask);
        }

        public void OnLevelLoaded(int level)
        {
            Events.Add("loaded:" + level);
        }

        public void OnIntroStarted(int durationMs, bool isShort)
        {
            Events.Add("intro:" + durationMs + (isShort ? ":short" : string.Empty));
        }

        public void OnIntroFinished(bool skipped)
        {
            Events.Add(skipped ? "ready:skipped" : "ready");
        }

        public void OnTap(TapResult result)
        {
            Taps.Add(result);
            Events.Add("tap:" + result.Kind);
        }

        public void OnUndo(Pour undone, bool fromStuckModal)
        {
            Events.Add("undo:" + undone.Source + "->" + undone.Target + (fromStuckModal ? ":modal" : string.Empty));
        }

        public void OnRestart(bool fromStuckModal)
        {
            Events.Add("restart" + (fromStuckModal ? ":modal" : string.Empty));
        }

        public void OnWin()
        {
            Events.Add("win");
        }

        public void OnStuck()
        {
            Events.Add("stuck");
        }

        public void OnCelebrationStarted()
        {
            Events.Add("celebration");
        }

        public void OnVictoryScreen()
        {
            Events.Add("victory");
        }

        public void OnStuckModalOpened()
        {
            Events.Add("modal");
        }

        public void OnCuiaAdded(int index, AddedCuiaKind kind)
        {
            Events.Add("cuia:" + index + ":" + kind);
        }

        public void OnExtraCuiaGifted(int units)
        {
            Events.Add("gift:" + units);
        }

        public void OnFunnelPhase(FlowPhase phase)
        {
            Events.Add("phase:" + phase);
        }

        public void OnFailResolved(FailOutcome outcome)
        {
            Events.Add("fail:" + outcome.Kind + ":" + outcome.FailsLevel + (outcome.CanoeLost ? ":canoe" : string.Empty));
        }

        public void OnRiverLowered(string reliefType, int bannerMs)
        {
            Events.Add("river:" + reliefType + ":" + bannerMs);
        }

        public void OnHarvestDoubled(int totalSeeds)
        {
            Events.Add("harvest_doubled:" + totalSeeds);
        }

        public void OnAdGate()
        {
            Events.Add("ad_gate");
        }

        public void OnResumed(FlowPhase phase)
        {
            Events.Add("resumed:" + phase);
        }

        public bool Saw(string evt)
        {
            return Events.Contains(evt);
        }
    }
}
