#nullable enable
using System;
using AF.IAP;

namespace AF.UI
{
    /// <summary>
    /// Tienda que, tras cada compra acreditada, pide una subida del perfil a la nube (08 §11, fila «Nube Android 1.0»: «tras cada
    /// compra»; D-042). Lo demás pasa tal cual a la tienda envuelta.
    /// </summary>
    public sealed class CloudUploadStoreService : IStoreService, IStoreDecorator
    {
        private readonly IStoreService _inner;
        private readonly Action _upload;

        /// <summary>Envuelve <paramref name="inner"/>; <paramref name="upload"/> sube el perfil.</summary>
        public CloudUploadStoreService(IStoreService inner, Action upload)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _upload = upload ?? throw new ArgumentNullException(nameof(upload));
        }

        /// <inheritdoc />
        public IStoreService Inner => _inner;

        /// <inheritdoc />
        public bool IsAvailable => _inner.IsAvailable;

        /// <inheritdoc />
        public bool IsPurchasing => _inner.IsPurchasing;

        /// <inheritdoc />
        public StoreCatalog Catalog => _inner.Catalog;

        /// <inheritdoc />
        public LocalPrice PriceOf(string sku) => _inner.PriceOf(sku);

        /// <inheritdoc />
        public void Purchase(PurchaseRequest request, Action<PurchaseResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            _inner.Purchase(request, result =>
            {
                onDone(result); // primero la acreditación y la UI; luego la subida, con el perfil ya guardado
                if (result.IsPurchased)
                {
                    _upload();
                }
            });
        }
    }
}
