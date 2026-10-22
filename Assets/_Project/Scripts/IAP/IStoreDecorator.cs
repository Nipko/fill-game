#nullable enable

namespace AF.IAP
{
    /// <summary>
    /// Una tienda que envuelve a otra (consentimiento, tienda lista de Play, analítica): quien necesite la tienda verificada de debajo
    /// (para oír <see cref="VerifiedStoreService.VerificationChanged"/>) la encuentra bajando por <see cref="Inner"/>, sin conocer cada
    /// envoltorio.
    /// </summary>
    public interface IStoreDecorator
    {
        /// <summary>La tienda envuelta.</summary>
        IStoreService Inner { get; }
    }

    /// <summary>Busca bajo los envoltorios (<see cref="IStoreDecorator"/>).</summary>
    public static class StoreDecorators
    {
        /// <summary>Envoltorios como mucho (para no colgarse con un ciclo).</summary>
        public const int MaxDepth = 16;

        /// <summary>La primera tienda de tipo <typeparamref name="T"/> en la cadena que empieza en <paramref name="store"/>, o null.</summary>
        public static T? Find<T>(IStoreService? store)
            where T : class
        {
            IStoreService? current = store;
            for (int depth = 0; depth < MaxDepth && current != null; depth++)
            {
                if (current is T found)
                {
                    return found;
                }

                current = (current as IStoreDecorator)?.Inner;
            }

            return null;
        }
    }
}
