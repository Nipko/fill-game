#nullable enable
using System;
using AF.Localization;

namespace AF.IAP.Tests
{
    /// <summary>
    /// Fija la región de las cifras de <see cref="Loc"/> durante un test (D-025: los precios salen con el formato de la región
    /// del teléfono) y deja la que había al terminar: la fachada es estática y la comparten todos los tests.
    /// </summary>
    internal readonly struct NumberRegionScope : IDisposable
    {
        private readonly string? _previous;

        private NumberRegionScope(string? previous)
        {
            _previous = previous;
        }

        /// <summary>Cifras con la región de <paramref name="tag"/> («es-CO», «en-US»…) hasta <see cref="Dispose"/>.</summary>
        public static NumberRegionScope Of(string tag)
        {
            var scope = new NumberRegionScope(Loc.RequestedNumberTag);
            Loc.SetNumberTag(tag);
            return scope;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Loc.SetNumberTag(_previous);
        }
    }
}
