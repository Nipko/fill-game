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

        private static LevelPack? _tutorial;

        /// <summary>Paquete <c>tutorial_v0</c> leído del disco (una vez).</summary>
        public static LevelPack Tutorial => _tutorial ??= LevelPackReader.Read(File.ReadAllText(TutorialPath));

        /// <summary>Paquete mínimo válido con un nivel; <paramref name="levelJson"/> sustituye al nivel por defecto.</summary>
        public static string MinimalPack(string? levelJson = null, string header = "\"subset_of\": \"af-level/1\"")
        {
            string level = levelJson ?? MinimalLevel();
            return "{ " + header + ", \"pack_id\": \"t\", \"kind\": \"campaign\", \"world\": 1, \"world_id\": \"w01_estuario\", "
                + "\"lap\": 1, \"water_base\": \"white\", \"levels\": [ " + level + " ] }";
        }

        /// <summary>Nivel 1 de 02 §13 con los campos sustituibles.</summary>
        public static string MinimalLevel(
            string cuias = "[[8, 8, 8, 4], [4, 4, 4, 8], []]",
            string caps = "[4, 4, 4]",
            string attrs = "[null, null, null]",
            string murky = "false",
            string hidden = "[]",
            string units = "4")
        {
            return "{ \"id\": \"m01_v1_n0001\", \"level\": 1, \"world\": 1, \"lap\": 1, \"water_base\": \"white\", "
                + "\"palette\": [\"acai\", \"aguaje\"], \"units_per_color\": " + units + ", \"caps\": " + caps
                + ", \"cuias\": " + cuias + ", \"attrs\": " + attrs + ", \"murky\": " + murky + ", \"hidden\": " + hidden
                + ", \"difficulty\": { \"par\": 3 }, \"relief\": [] }";
        }
    }

    /// <summary>Oyente que registra los eventos de <see cref="LevelFlow"/> en orden.</summary>
    internal sealed class RecordingListener : ILevelFlowListener
    {
        public readonly List<string> Events = new List<string>();

        public readonly List<TapResult> Taps = new List<TapResult>();

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

        public bool Saw(string evt)
        {
            return Events.Contains(evt);
        }
    }
}
