#nullable enable
using System.Collections.Generic;
using AF.Levels;

namespace AF.RemoteConfig.Binding
{
    /// <summary>
    /// Las claves de Remote Config del catálogo de niveles (08 §9): los kill switches <c>mech_enabled.*</c> y la tabla
    /// <c>level_overrides</c> (D-020, pregunta 31 b; tarea KS1; D-033, añadido «la reserva del 20 %», Q-B11-7 a). Se leen en el
    /// arranque, antes de crear el catálogo (<see cref="LevelCatalog"/>), y no cambian a mitad de sesión: lo que se descargue se
    /// aplica en el arranque siguiente (08 §2.3). Sin reflexión ni motor; separado de <see cref="RcConfigBinder"/> porque no son
    /// propiedades de una clase de cifras con <c>[RemoteConfigKey]</c>.
    /// </summary>
    public static class RcLevelBinder
    {
        /// <summary>
        /// Los kill switches de <paramref name="compiled"/> (los defaults compilados del cliente) con las mecánicas que Remote
        /// Config apaga: una mecánica queda encendida solo si lo está en los dos. Una clave que no se puede leer como bool deja la
        /// mecánica como estaba y se anota en <paramref name="errors"/>.
        /// </summary>
        public static MechanicSwitches MechEnabled(RcSnapshot rc, MechanicSwitches compiled, ICollection<string>? errors = null)
        {
            MechanicSwitches result = compiled;
            for (int i = 0; i < MechanicSwitches.Count; i++)
            {
                var mechanic = (MechanicSwitch)i;
                string key = MechanicSwitches.KeyOf(mechanic);
                if (!rc.TryGet(key, out bool enabled))
                {
                    errors?.Add(key + ": sin valor bool; queda el default compilado.");
                    continue;
                }

                if (!enabled)
                {
                    result = result.With(mechanic, false);
                }
            }

            return result;
        }

        /// <summary>
        /// El texto JSON de <c>level_overrides</c> (<c>{slot: level_id}</c>, default <c>{}</c>): los ids pueden ser de campaña o de
        /// un tablero de reserva (<c>m{mm}_v{v}_n{nnnn}_r{k}</c>). Sin valor, la fuente local (<see cref="LevelOverrides.LocalSource"/>).
        /// </summary>
        public static string LevelOverridesJson(RcSnapshot rc)
        {
            return rc.TryGet(LevelOverrides.Key, out string json) ? json : LevelOverrides.LocalSource;
        }
    }
}
