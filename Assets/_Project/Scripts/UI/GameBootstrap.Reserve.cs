#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.RemoteConfig;
using AF.RemoteConfig.Binding;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La reserva del 20 % en el arranque (D-033, añadido «la reserva del 20 %», Q-B11-7 a; 03 §17; D-020, pregunta 31 b): los
    /// paquetes de reserva de la escena (<c>reserva_03_v1</c> … <c>reserva_09_v1</c>, <c>kind</c> = <c>reserve</c>) y las claves
    /// de Remote Config del catálogo, <c>mech_enabled.*</c> y <c>level_overrides</c> (<see cref="RcLevelBinder"/>), que se leen
    /// antes de crear el catálogo. Los tableros de reserva nunca son niveles de campaña: solo se buscan al resolver
    /// <c>level_overrides</c>, y solo si la tabla trae alguna entrada (sin entradas no se lee ningún paquete de reserva).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        [SerializeField]
        private TextAsset[] reserveLevelPacks = Array.Empty<TextAsset>();

        /// <summary>Paquetes de reserva asignados, en orden (escena generada por <c>GameSceneBuilder</c>).</summary>
        public IReadOnlyList<TextAsset> ReservePackAssets => reserveLevelPacks;

        /// <summary>Asigna los paquetes de reserva (escena generada por <c>GameSceneBuilder</c>; D-033, Q-B11-7 a).</summary>
        public void ConfigureReservePacks(IReadOnlyList<TextAsset>? packs)
        {
            if (packs == null)
            {
                reserveLevelPacks = Array.Empty<TextAsset>();
                return;
            }

            var copy = new TextAsset[packs.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = packs[i];
            }

            reserveLevelPacks = copy;
        }

        // mech_enabled.* del catálogo: los defaults compilados (GameConfig) combinados con los de Remote Config (la sesión ya lo
        // tiene activado: SetUpRemoteConfig) y, en desarrollo, con af_mech_off.
        private MechanicSwitches ResolveCatalogSwitches()
        {
            MechanicSwitches baseline = GameConfig.MechEnabled;
            RcSnapshot? rc = _rc?.Freeze();
            if (rc != null)
            {
                var errors = new List<string>();
                baseline = RcLevelBinder.MechEnabled(rc, baseline, errors);
                for (int i = 0; i < errors.Count; i++)
                {
                    Debug.LogWarning(LogPrefix + "Remote Config: " + errors[i]);
                }
            }

            return LaunchOptions.ResolveMechSwitches(baseline);
        }

        // level_overrides del catálogo: el del test, el de Remote Config o la fuente local, en ese orden.
        private string ResolveCatalogOverridesJson()
        {
            RcSnapshot? rc = _rc?.Freeze();
            return LaunchOptions.ResolveLevelOverridesJson(rc != null ? RcLevelBinder.LevelOverridesJson(rc) : null);
        }

        // Paquetes donde se buscan los ids de level_overrides: los de campaña y, si la tabla tiene alguna entrada, los de reserva
        // (los del test o los de la escena). Un paquete de reserva roto o que no es de reserva no rompe el arranque: se avisa y se
        // sigue sin él (sus overrides quedan sin resolver y LevelOverrides lo anota).
        private List<LevelPack> OverridePool(IReadOnlyList<LevelPack> campaign, string overridesJson)
        {
            var pool = new List<LevelPack>(campaign);
            if (!HasEntries(overridesJson))
            {
                return pool;
            }

            string[]? texts = LaunchOptions.ReservePackJsonOverride;
            int count = texts != null ? texts.Length : reserveLevelPacks.Length;
            for (int i = 0; i < count; i++)
            {
                string? text = texts != null ? texts[i] : reserveLevelPacks[i] != null ? reserveLevelPacks[i].text : null;
                if (text == null)
                {
                    continue;
                }

                try
                {
                    LevelPack pack = LevelPackReader.Read(text);
                    if (!pack.IsReserve)
                    {
                        Debug.LogError(LogPrefix + pack.PackId + " no es un paquete de reserva (kind " + pack.Kind + "); se ignora.");
                        continue;
                    }

                    pool.Add(pack);
                }
                catch (LevelFormatException e)
                {
                    Debug.LogError(LogPrefix + "un paquete de reserva no es válido: " + e.Message + " Se sigue sin él.");
                }
            }

            return pool;
        }

        // La tabla trae alguna entrada: un objeto JSON con algo dentro (lo demás lo decide LevelOverrides.Create).
        private static bool HasEntries(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                return true; // no es un objeto: LevelOverrides lo anota como problema
            }

            for (int i = 1; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (c == '}')
                {
                    return false;
                }

                if (!char.IsWhiteSpace(c))
                {
                    return true;
                }
            }

            return true;
        }
    }
}
