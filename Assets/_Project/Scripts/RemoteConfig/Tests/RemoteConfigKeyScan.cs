#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using AF.Platform;

namespace AF.RemoteConfig.Tests
{
    /// <summary>
    /// Los tipos del cliente con propiedades <c>[RemoteConfigKey]</c>: se descubren recorriendo enteros los ensamblados de las
    /// clases de cifras (Economy, Boosters, Ads, IAP, LiveOps y Platform), así que una clase nueva (<c>ShuffleConfig</c> de B2,
    /// <c>SkipConfig</c> de B3…) aparece sola en las pruebas en cuanto llega.
    /// </summary>
    internal static class RemoteConfigKeyScan
    {
        /// <summary>Los ensamblados de las clases de cifras de hoy: el escaneo tiene que verlos todos (mínimo).</summary>
        public static IReadOnlyList<Assembly> Known()
        {
            var assemblies = new List<Assembly>();
            foreach (Type known in new[]
            {
                typeof(AF.Economy.EconomyConfig), typeof(AF.Boosters.BoosterConfig), typeof(AF.Ads.AdsConfig), typeof(AF.IAP.StoreConfig),
                typeof(AF.LiveOps.DailyChallengeConfig), typeof(ClockSecurityConfig),
            })
            {
                if (!assemblies.Contains(known.Assembly))
                {
                    assemblies.Add(known.Assembly);
                }
            }

            return assemblies;
        }

        /// <summary>
        /// Los ensamblados que se recorren: el de <see cref="RemoteConfigKeyAttribute"/> y todos los cargados que lo referencian
        /// (así entra solo, por ejemplo, AF.GameFlow cuando <c>GameConfig</c> lleve sus claves), menos los de pruebas
        /// (<c>*.Tests</c> y <c>AF.Tests.*</c>), que no son cifras del cliente.
        /// </summary>
        public static IReadOnlyList<Assembly> Assemblies()
        {
            Assembly attributeAsm = typeof(RemoteConfigKeyAttribute).Assembly;
            string attributeName = attributeAsm.GetName().Name;
            var assemblies = new List<Assembly> { attributeAsm };
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm == attributeAsm || asm.IsDynamic)
                {
                    continue;
                }

                string name = asm.GetName().Name;
                if (name.EndsWith(".Tests", StringComparison.Ordinal) || name.StartsWith("AF.Tests", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (AssemblyName reference in asm.GetReferencedAssemblies())
                {
                    if (string.Equals(reference.Name, attributeName, StringComparison.Ordinal))
                    {
                        assemblies.Add(asm);
                        break;
                    }
                }
            }

            return assemblies;
        }

        /// <summary>Cada tipo con alguna propiedad <c>[RemoteConfigKey]</c> y sus claves, en orden de nombre.</summary>
        public static SortedDictionary<string, KeyValuePair<Type, List<string>>> Types()
        {
            var found = new SortedDictionary<string, KeyValuePair<Type, List<string>>>(StringComparer.Ordinal);
            foreach (Assembly asm in Assemblies())
            {
                foreach (Type type in asm.GetTypes())
                {
                    var keys = new List<string>();
                    foreach (PropertyInfo p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        RemoteConfigKeyAttribute? a = p.GetCustomAttribute<RemoteConfigKeyAttribute>();
                        if (a != null)
                        {
                            keys.Add(a.Key);
                        }
                    }

                    if (keys.Count > 0)
                    {
                        found[type.FullName!] = new KeyValuePair<Type, List<string>>(type, keys);
                    }
                }
            }

            return found;
        }
    }
}
