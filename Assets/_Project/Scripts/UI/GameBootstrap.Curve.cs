#nullable enable
using System;
using System.Collections.Generic;
using AF.Levels;
using AF.RemoteConfig;
using AF.RemoteConfig.Binding;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La curva 2 de la 1.1 en el arranque (D-068, P1 B y P10 A; 08 §4.3, §4.4 y §9; fase F4): los paquetes de la curva
    /// (<c>curva_{mm}_v{v}</c>, <c>kind</c> = <c>curve</c>) y su reserva (<c>reserva_curva_{mm}_v{v}</c>) de la escena, y el
    /// selector con <c>lvl_curve2_enabled</c> (Remote Config) y <c>progress.curve2_from_level</c> (el perfil, ya migrado al
    /// esquema 3 y fusionado con la nube). Se resuelve una vez, antes de crear el catálogo: lo que cambie a mitad de sesión (la
    /// clave descargada, una fusión con la nube que baje el campo) se aplica en el arranque siguiente, como
    /// <c>mech_enabled_*</c> (08 §2.3) [P]. Sin paquetes de la curva (hasta F5), nada cambia.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        [SerializeField]
        private TextAsset[] curveLevelPacks = Array.Empty<TextAsset>();

        [SerializeField]
        private TextAsset[] curveReserveLevelPacks = Array.Empty<TextAsset>();

        /// <summary>Paquetes de la curva 2 asignados, en orden (escena generada por <c>GameSceneBuilder</c>; D-068).</summary>
        public IReadOnlyList<TextAsset> CurvePackAssets => curveLevelPacks;

        /// <summary>Paquetes de reserva de la curva 2 asignados, en orden (D-068).</summary>
        public IReadOnlyList<TextAsset> CurveReservePackAssets => curveReserveLevelPacks;

        /// <summary>Asigna los paquetes de la curva 2 y su reserva (escena generada por <c>GameSceneBuilder</c>; D-068).</summary>
        public void ConfigureCurvePacks(IReadOnlyList<TextAsset>? curve, IReadOnlyList<TextAsset>? reserve)
        {
            curveLevelPacks = Copy(curve);
            curveReserveLevelPacks = Copy(reserve);
        }

        private static TextAsset[] Copy(IReadOnlyList<TextAsset>? list)
        {
            if (list == null)
            {
                return Array.Empty<TextAsset>();
            }

            var copy = new TextAsset[list.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = list[i];
            }

            return copy;
        }

        // El selector: lvl_curve2_enabled (el del test, el de Remote Config o el default de 08 §9) y curve2_from_level del perfil.
        private CurveSelection ResolveCurveSelection()
        {
            bool enabled = CurveSelection.EnabledDefault;
            if (LaunchOptions.Curve2EnabledOverride.HasValue)
            {
                enabled = LaunchOptions.Curve2EnabledOverride.Value;
            }
            else
            {
                RcSnapshot? rc = _rc?.Freeze();
                if (rc != null)
                {
                    var errors = new List<string>();
                    enabled = RcLevelBinder.Curve2Enabled(rc, errors);
                    for (int i = 0; i < errors.Count; i++)
                    {
                        Debug.LogWarning(LogPrefix + "Remote Config: " + errors[i]);
                    }
                }
            }

            Profile? profile = _profile;
            int from = profile != null ? profile.Progress.Curve2FromLevel : 1;
            return new CurveSelection(enabled, from);
        }

        // Los paquetes de la curva 2 (curve = true) o de su reserva: los del test o los de la escena. Uno roto o de otro tipo no
        // rompe el arranque: se avisa y se sigue sin él (sus slots se quedan con la curva 1).
        private List<LevelPack> LoadCurvePacks(bool curve)
        {
            var packs = new List<LevelPack>();
            string[]? texts = curve ? LaunchOptions.CurvePackJsonOverride : LaunchOptions.CurveReservePackJsonOverride;
            TextAsset[] assets = curve ? curveLevelPacks : curveReserveLevelPacks;
            int count = texts != null ? texts.Length : assets.Length;
            for (int i = 0; i < count; i++)
            {
                string? text = texts != null ? texts[i] : assets[i] != null ? assets[i].text : null;
                if (text == null)
                {
                    continue;
                }

                try
                {
                    LevelPack pack = LevelPackReader.Read(text);
                    bool ok = curve ? pack.IsCurve : pack.IsReserve;
                    if (!ok)
                    {
                        Debug.LogError(LogPrefix + pack.PackId + " no es un paquete " + (curve ? "de la curva 2" : "de reserva de la curva 2")
                            + " (kind " + pack.Kind + "); se ignora.");
                        continue;
                    }

                    packs.Add(pack);
                }
                catch (LevelFormatException e)
                {
                    Debug.LogError(LogPrefix + "un paquete " + (curve ? "de la curva 2" : "de reserva de la curva 2") + " no es válido: " + e.Message
                        + " Se sigue sin él.");
                }
            }

            return packs;
        }
    }
}
