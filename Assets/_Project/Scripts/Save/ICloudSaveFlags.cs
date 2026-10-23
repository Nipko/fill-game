#nullable enable
using System;

namespace AF.Save
{
    /// <summary>
    /// Marcas persistentes de la nube (D-042; <c>docs/dev/play-games.md</c> §5), fuera del perfil porque tienen que sobrevivir a
    /// «Eliminar cuenta y datos» y no viajan en el blob. En el teléfono, en <c>PlayerPrefs</c>; en el editor y en los tests, en memoria.
    /// </summary>
    public interface ICloudSaveFlags
    {
        /// <summary>
        /// Hay que borrar <c>af_main</c> de la nube: lo marca el borrado de la cuenta (08 §11) y lo limpia <see cref="CloudErase"/>
        /// cuando comprueba que el snapshot está vacío. Mientras siga, no se fusiona ni se sube nada (nota B8 §8).
        /// </summary>
        bool DeletePending { get; set; }

        /// <summary>
        /// La última subida no consta en la nube: falló, o se hizo sin red y Play puede no volver a subirla (bug #3379 del plugin).
        /// La sincronización siguiente con red vuelve a subir el perfil aunque la fusión no lo pida.
        /// </summary>
        bool ResyncPending { get; set; }
    }

    /// <summary><see cref="ICloudSaveFlags"/> en memoria (editor y tests).</summary>
    public sealed class MemoryCloudSaveFlags : ICloudSaveFlags
    {
        /// <inheritdoc />
        public bool DeletePending { get; set; }

        /// <inheritdoc />
        public bool ResyncPending { get; set; }
    }

    /// <summary>Una nube que sabe si su última subida está pendiente de volver a subir (bug #3379; <see cref="CloudSync.AtStartup"/>).</summary>
    public interface ICloudResync
    {
        /// <summary>La sincronización siguiente sube el perfil aunque la fusión no lo pida.</summary>
        bool NeedsResync { get; }
    }

    /// <summary>Una nube con inicio de sesión silencioso (Play Games; la falsa ya nace con o sin sesión).</summary>
    public interface ICloudSignIn
    {
        /// <summary>Inicia sesión sin interfaz; <paramref name="done"/> dice si hay sesión.</summary>
        void SignIn(Action<bool> done);
    }
}
