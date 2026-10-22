package com.amazonfill.billing;

/**
 * Avisos de {@link AfBilling} hacia C# (AF.IAP.PlayBilling.BillingListenerProxy, un AndroidJavaProxy). Se llaman siempre en el
 * hilo principal de Android; C# los pasa al hilo de Unity. Solo enteros y cadenas, para que la conversión por JNI sea directa.
 *
 * <p>Las listas van como texto: una fila por línea ('\n') y campos separados por tabuladores ('\t').
 * <ul>
 *   <li>Producto: {@code sku \t moneda ISO 4217 \t importe en micros}.</li>
 *   <li>Compra: {@code sku \t purchaseToken \t estado (1 PURCHASED, 2 PENDING) \t purchaseTime en ms \t reconocida (1/0)}.</li>
 * </ul>
 * Los códigos son los de {@code BillingClient.BillingResponseCode} (0 = OK). El token nunca se escribe en el log.
 */
public interface AfBillingListener {
    /** Respuesta de {@link AfBilling#connect()}: código de {@code onBillingSetupFinished}. */
    void onSetupFinished(int code);

    /** {@code onBillingServiceDisconnected}: las consultas en vuelo ya no responderán. */
    void onDisconnected();

    /** Respuesta de {@link AfBilling#queryProducts(int, String)}; {@code rows} es null si {@code code} no es OK. */
    void onProducts(int requestId, int code, String rows);

    /** Respuesta de {@link AfBilling#queryPurchases(int)}; {@code rows} es null si {@code code} no es OK. */
    void onPurchases(int requestId, int code, String rows);

    /** {@code PurchasesUpdatedListener}: aviso en vivo de la compra abierta o de un pago pendiente que Play cobró. */
    void onPurchasesUpdated(int code, String rows);

    /** {@code launchBillingFlow} no abrió la hoja (o el producto no estaba cargado). */
    void onLaunchFailed(String sku, int code);

    /** Respuesta de {@link AfBilling#consume(String)}. */
    void onConsumed(String token, int code);

    /** Respuesta de {@link AfBilling#acknowledge(String)}. */
    void onAcknowledged(String token, int code);
}
