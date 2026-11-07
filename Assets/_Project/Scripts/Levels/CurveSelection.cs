#nullable enable
using System;
using System.Globalization;

namespace AF.Levels
{
    /// <summary>
    /// Selector de la curva 2 de la 1.1 (D-068, P1 B y P10 A; 08 §4.3, §4.4 y §9): qué niveles de campaña se sirven con el
    /// tablero de la curva 2 (los packs <c>curva_{mm}_v{v}</c>, <c>kind</c> = <c>curve</c>) en lugar del de la curva 1. Valor
    /// inmutable, sin motor y sin asignaciones.
    /// </summary>
    /// <remarks>
    /// <para>Un nivel va con la curva 2 si <c>lvl_curve2_enabled</c> está encendida (<see cref="Enabled"/>), el nivel está en
    /// 301-700 (<see cref="FirstLevel"/>-<see cref="LastLevel"/>: desde el 701 la campaña nace con la curva 2 y no hay
    /// sustitución) y es <see cref="FromLevel"/> o posterior: <c>progress.curve2_from_level</c>, el siguiente nivel que el jugador
    /// no había jugado al abrir la 1.1 por primera vez. Lo de antes no cambia de tablero, tampoco al rejugarlo desde el Mapa (08
    /// §4.3). Con la clave apagada (el segmento A del A/B), la curva 1 en todos los niveles, sin tocar el perfil.</para>
    /// <para>Que el slot tenga tablero de la curva 2 lo decide <see cref="LevelCatalog"/> con los packs que reciba; sin packs de
    /// la curva, este selector no cambia nada.</para>
    /// </remarks>
    public readonly struct CurveSelection : IEquatable<CurveSelection>
    {
        /// <summary>Primer nivel que la curva 2 sustituye (D-068, P1 B).</summary>
        public const int FirstLevel = 301;

        /// <summary>Último nivel que la curva 2 sustituye: desde el 701 los packs nacen con ella (08 §4.4).</summary>
        public const int LastLevel = 700;

        /// <summary>Clave de Remote Config (08 §9).</summary>
        public const string EnabledKey = "lvl_curve2_enabled";

        /// <summary>Default de <c>lvl_curve2_enabled</c> (08 §9): encendida en la 1.1.</summary>
        public const bool EnabledDefault = true;

        /// <summary>Crea el selector.</summary>
        /// <param name="enabled"><c>lvl_curve2_enabled</c>.</param>
        /// <param name="fromLevel"><c>progress.curve2_from_level</c> (≥ 1; un valor menor se lee como 1).</param>
        public CurveSelection(bool enabled, int fromLevel)
        {
            Enabled = enabled;
            FromLevel = Math.Max(1, fromLevel);
        }

        /// <summary>Sin curva 2: la curva 1 en todos los niveles (el valor por defecto).</summary>
        public static CurveSelection Off => default;

        /// <summary><c>lvl_curve2_enabled</c>.</summary>
        public bool Enabled { get; }

        /// <summary><c>progress.curve2_from_level</c> (0 en <see cref="Off"/>).</summary>
        public int FromLevel { get; }

        /// <summary>El nivel de campaña <paramref name="level"/> se juega con la curva 2 si su slot la tiene.</summary>
        public bool UsesCurve2(int level)
        {
            return Enabled && FromLevel > 0 && level >= FromLevel && level >= FirstLevel && level <= LastLevel;
        }

        /// <summary>
        /// El nivel de campaña <paramref name="level"/> está en la zona que la curva 2 puede sustituir (301-700), con o sin el
        /// selector encendido.
        /// </summary>
        public static bool InRange(int level)
        {
            return level >= FirstLevel && level <= LastLevel;
        }

        /// <inheritdoc />
        public bool Equals(CurveSelection other)
        {
            return Enabled == other.Enabled && FromLevel == other.FromLevel;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is CurveSelection other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (FromLevel * 2) + (Enabled ? 1 : 0);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Enabled ? "curva 2 desde el " + FromLevel.ToString(CultureInfo.InvariantCulture) : "curva 2 apagada";
        }
    }
}
