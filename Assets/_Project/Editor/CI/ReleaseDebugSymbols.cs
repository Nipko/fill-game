#nullable enable
#if UNITY_ANDROID
using Unity.Android.Types;
using UnityEditor.Android;
#endif

namespace AF.EditorTools
{
    /// <summary>
    /// Símbolos nativos del build de release dentro del AAB (D-059, añadido del 2026-11-06: Play Console avisó de que faltaban, con
    /// el archivo de desofuscación; <c>docs/dev/release.md</c>, «R8 y símbolos»). Con la tabla de símbolos
    /// (<c>debugSymbolLevel SYMBOL_TABLE</c>) dentro del bundle (<c>BUNDLE-METADATA/com.android.tools.build.debugsymbols</c>), Play
    /// simboliza los fallos y ANR nativos de <c>libil2cpp.so</c>, <c>libunity.so</c> y los SDK sin subir nada a mano. No cambia lo
    /// que se descarga el jugador (Play no los sirve) y no lleva secretos.
    ///
    /// El ajuste vive en <c>EditorUserBuildSettings</c> (la carpeta <c>Library/</c> de cada equipo), no en <c>ProjectSettings/</c>:
    /// por eso <see cref="BuildRelease"/> lo pone en cada build de release y de ensayo y lo deja como estaba al terminar
    /// (<c>dev.apk</c> sigue sin símbolos, que lo harían más lento de construir).
    /// La API (<c>UnityEditor.Android.UserBuildSettings</c>) solo compila con la plataforma Android activa: fuera de ella
    /// <see cref="Available"/> es false y el build de release sale con error.
    /// </summary>
    internal static class ReleaseDebugSymbols
    {
#if UNITY_ANDROID
        public const bool Available = true;

        /// <summary>Estado anterior, para restaurarlo.</summary>
        public readonly struct State
        {
            public State(DebugSymbolLevel level, DebugSymbolFormat format)
            {
                Level = level;
                Format = format;
            }

            public DebugSymbolLevel Level { get; }

            public DebugSymbolFormat Format { get; }
        }

        public static State Capture()
        {
            return new State(UserBuildSettings.DebugSymbols.level, UserBuildSettings.DebugSymbols.format);
        }

        /// <summary>Tabla de símbolos dentro del bundle.</summary>
        public static void Apply()
        {
            UserBuildSettings.DebugSymbols.level = DebugSymbolLevel.SymbolTable;
            UserBuildSettings.DebugSymbols.format = DebugSymbolFormat.IncludeInBundle;
        }

        public static void Restore(State state)
        {
            UserBuildSettings.DebugSymbols.level = state.Level;
            UserBuildSettings.DebugSymbols.format = state.Format;
        }

        public static string Describe()
        {
            return "simbolos=" + UserBuildSettings.DebugSymbols.level + "/" + UserBuildSettings.DebugSymbols.format;
        }
#else
        public const bool Available = false;

        public readonly struct State
        {
        }

        public static State Capture()
        {
            return default;
        }

        public static void Apply()
        {
        }

        public static void Restore(State state)
        {
        }

        public static string Describe()
        {
            return "simbolos=no disponibles (plataforma activa distinta de Android)";
        }
#endif
    }
}
