#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Idioma del teléfono fijado a mano, para tests y builds de desarrollo (p. ej. un teléfono en <c>pt-BR</c> o sin
    /// región). Solo cambia al asignar <see cref="LanguageTag"/>.
    /// </summary>
    public sealed class ManualDeviceLocale : IDeviceLocale
    {
        private string _languageTag = string.Empty;

        /// <summary>Crea el idioma con la etiqueta <paramref name="languageTag"/> (<c>""</c> = el sistema no la da).</summary>
        /// <exception cref="ArgumentNullException"><paramref name="languageTag"/> es null.</exception>
        public ManualDeviceLocale(string languageTag = "")
        {
            LanguageTag = languageTag;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Al asignar null.</exception>
        public string LanguageTag
        {
            get => _languageTag;
            set => _languageTag = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
