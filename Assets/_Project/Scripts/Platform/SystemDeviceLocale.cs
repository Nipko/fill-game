#nullable enable
using System;
using System.Globalization;

namespace AF.Platform
{
    /// <summary>
    /// Idioma del sistema según la cultura de .NET, sin motor: el del Editor y el respaldo de la lectura de Android (en
    /// IL2CPP la cultura puede llegar vacía; hay que medirlo en el teléfono, D-018 Q8-A [NV]). Es la <b>única</b> lectura
    /// de la cultura del dispositivo en <c>AF.Platform</c> y <c>AF.Save</c> (un test de guarda lo comprueba). Usa la
    /// cultura de la interfaz y, si no trae región, la regional del mismo idioma (las cifras van con la región).
    /// </summary>
    public sealed class SystemDeviceLocale : IDeviceLocale
    {
        /// <summary>Instancia compartida (no tiene estado: lee la cultura en cada consulta).</summary>
        public static readonly SystemDeviceLocale Instance = new SystemDeviceLocale();

        /// <inheritdoc />
        public string LanguageTag => TagOf(CultureInfo.CurrentUICulture, CultureInfo.CurrentCulture);

        /// <summary>
        /// Etiqueta de <paramref name="ui"/> (la cultura de la interfaz). Si no tiene región y <paramref name="regional"/>
        /// es del mismo idioma y la tiene, la de <paramref name="regional"/>; si <paramref name="ui"/> es la invariante, la
        /// de <paramref name="regional"/>; si las dos lo son, <c>""</c>.
        /// </summary>
        public static string TagOf(CultureInfo? ui, CultureInfo? regional)
        {
            string uiTag = ui?.Name ?? string.Empty;
            string regionalTag = regional?.Name ?? string.Empty;
            if (uiTag.Length == 0)
            {
                return regionalTag;
            }

            if (uiTag.IndexOf('-') < 0 && regionalTag.StartsWith(uiTag + "-", StringComparison.OrdinalIgnoreCase))
            {
                return regionalTag;
            }

            return uiTag;
        }
    }
}
