#nullable enable
using System;
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// Almacén del perfil del modo de captura (D-066; spark-marketing §6 E1): lee el perfil guardado del almacén de siempre
    /// (<see cref="Inner"/>) y nunca escribe ni borra nada. Lo que pase durante la toma (los vertidos y la victoria del tablero
    /// cargado desde JSON, la cartera de <c>af_seeds</c>, el progreso de <c>af_max_level</c>, los ajustes) vive solo en memoria:
    /// al cerrar la app, el perfil del teléfono de pruebas sigue como estaba. La nube también queda fuera (la nube falsa sin
    /// sesión: <c>GameBootstrap.CloudSession.cs</c>).
    /// </summary>
    public sealed class CaptureProfileStore : IProfileStore
    {
        /// <summary>Envuelve <paramref name="inner"/>.</summary>
        public CaptureProfileStore(IProfileStore inner)
        {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>El almacén real, del que solo se lee.</summary>
        public IProfileStore Inner { get; }

        /// <summary>Escrituras que se descartaron (tests y sonda).</summary>
        public int DiscardedSaves { get; private set; }

        /// <inheritdoc />
        public ProfileLoadResult Load()
        {
            return Inner.Load();
        }

        /// <summary>No escribe: la toma no toca el perfil guardado.</summary>
        public void Save(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            DiscardedSaves++;
        }

        /// <summary>No borra: ni «Eliminar cuenta y datos» toca el perfil guardado durante una toma.</summary>
        public void Delete()
        {
        }
    }
}
