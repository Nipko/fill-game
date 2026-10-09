#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Marca una propiedad de una clase de cifras (<c>EconomyConfig</c>, <c>BoosterConfig</c>, <c>AdsConfig</c>,
    /// <c>StoreConfig</c>) con su clave exacta en la tabla única de Remote Config (GDD 08 §9). Los tests de cada módulo
    /// leen esa tabla y comprueban la clave, el tipo y el default de cada propiedad marcada, como guarda contra cifras
    /// inventadas.
    /// <para>
    /// Vive en <c>AF.Platform</c>, el ensamblado base sin dependencias, para que la usen a la vez los proveedores
    /// (<c>AF.Ads</c>) y sus consumidores (<c>AF.Economy</c>, <c>AF.Boosters</c>) sin invertir la columna «Depende de» de
    /// 08 §2.2. Pasará a <c>AF.RemoteConfig</c> cuando ese módulo exista.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class RemoteConfigKeyAttribute : Attribute
    {
        /// <summary>Marca la propiedad con la clave <paramref name="key"/>.</summary>
        public RemoteConfigKeyAttribute(string key)
        {
            Key = key;
        }

        /// <summary>Clave de 08 §9.</summary>
        public string Key { get; }
    }
}
