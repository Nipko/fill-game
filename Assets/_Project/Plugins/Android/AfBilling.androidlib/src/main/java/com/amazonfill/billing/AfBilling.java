package com.amazonfill.billing;

import android.app.Activity;
import android.content.Context;
import android.os.Handler;
import android.os.Looper;

import com.android.billingclient.api.AcknowledgePurchaseParams;
import com.android.billingclient.api.BillingClient;
import com.android.billingclient.api.BillingClientStateListener;
import com.android.billingclient.api.BillingFlowParams;
import com.android.billingclient.api.BillingResult;
import com.android.billingclient.api.ConsumeParams;
import com.android.billingclient.api.GetBillingConfigParams;
import com.android.billingclient.api.PendingPurchasesParams;
import com.android.billingclient.api.ProductDetails;
import com.android.billingclient.api.Purchase;
import com.android.billingclient.api.PurchasesUpdatedListener;
import com.android.billingclient.api.QueryProductDetailsParams;
import com.android.billingclient.api.QueryProductDetailsResult;
import com.android.billingclient.api.QueryPurchasesParams;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Plugin propio de Play Billing de Amazon Fill sobre Play Billing Library 9.0.0 (D-040, añadido de Q-IAP-1; docs/dev/iap.md).
 *
 * <p>Solo traduce entre C# (AF.IAP.PlayBilling.NativePlayStorePort) y {@link BillingClient}: no decide nada, no acredita nada,
 * no guarda nada en disco y no escribe nada en el log. <b>Solo habla con Play</b> (el servicio de facturación de la Play Store, a
 * través de la biblioteca); no abre ninguna conexión propia ni conoce ningún otro host.
 *
 * <p>Todas las llamadas pasan al hilo principal de Android, donde también llegan los avisos de {@link BillingClient}; así el estado
 * de esta clase solo se toca en un hilo. Las respuestas salen por {@link AfBillingListener}.
 *
 * <p>Productos integrados ({@code INAPP}) solamente; compras pendientes de pago habilitadas
 * ({@code enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts())}) y reconexión automática
 * ({@code enableAutoServiceReconnection}).
 */
public final class AfBilling implements PurchasesUpdatedListener {
    private static final int OK = BillingClient.BillingResponseCode.OK;
    /** Espera a que termine una reconexión automática de la biblioteca: hasta 60 vueltas de 500 ms (30 s). */
    private static final int RECONNECT_POLLS = 60;
    private static final long RECONNECT_POLL_MS = 500L;
    /** Antes de abrir la hoja sin conexión: hasta 10 vueltas de 500 ms (5 s). */
    private static final int LAUNCH_POLLS = 10;

    private final Handler main = new Handler(Looper.getMainLooper());
    private final Context context;
    private final AfBillingListener listener;
    /**
     * Los productos de la última consulta que respondió (los únicos que se venden). Se sustituye entero solo cuando llega la
     * respuesta de la consulta más reciente, nunca al lanzarla: una compra abierta mientras un Refresh está en vuelo usa los de
     * la consulta anterior, que son los que enseña el Mercado.
     */
    private Map<String, ProductDetails> details = new HashMap<>();
    private BillingClient client;
    private boolean connecting;
    private boolean waitingReconnect;
    private int latestProductsRequest;
    private boolean released;

    /**
     * @param context  contexto de la app (se usa el de la aplicación).
     * @param listener avisos hacia C#.
     */
    public AfBilling(Context context, AfBillingListener listener) {
        if (context == null || listener == null) {
            throw new IllegalArgumentException("context y listener son obligatorios");
        }

        this.context = context.getApplicationContext();
        this.listener = listener;
        main.post(this::create);
    }

    private void create() {
        if (released || client != null) {
            return;
        }

        client = BillingClient.newBuilder(context)
                .setListener(this)
                .enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts().build())
                .enableAutoServiceReconnection()
                .build();
    }

    /** Conecta con Play; responde con {@link AfBillingListener#onSetupFinished(int)} (OK en el acto si ya estaba conectado). */
    public void connect() {
        main.post(() -> {
            if (released) {
                return;
            }

            create();
            if (client.isReady()) {
                listener.onSetupFinished(OK);
                return;
            }

            if (connecting || waitingReconnect) {
                return; // la respuesta de la conexión en curso vale para todos los que esperan
            }

            startOrWait(0);
        });
    }

    private void startOrWait(final int polls) {
        waitingReconnect = false;
        if (released) {
            return;
        }

        if (client.isReady()) {
            listener.onSetupFinished(OK);
            return;
        }

        if (client.getConnectionState() == BillingClient.ConnectionState.CONNECTING) {
            // La biblioteca ya se está reconectando sola (enableAutoServiceReconnection): otro startConnection respondería
            // DEVELOPER_ERROR. Se espera a que termine (una sola espera para todos); si no termina a tiempo, se responde «sin
            // servicio» (el próximo Refresh lo vuelve a intentar).
            if (polls < RECONNECT_POLLS) {
                waitingReconnect = true;
                main.postDelayed(() -> startOrWait(polls + 1), RECONNECT_POLL_MS);
            } else {
                listener.onSetupFinished(BillingClient.BillingResponseCode.SERVICE_UNAVAILABLE);
            }

            return;
        }

        connecting = true;
        try {
            client.startConnection(new BillingClientStateListener() {
                @Override
                public void onBillingSetupFinished(BillingResult result) {
                    main.post(() -> {
                        connecting = false;
                        if (!released) {
                            listener.onSetupFinished(code(result));
                        }
                    });
                }

                @Override
                public void onBillingServiceDisconnected() {
                    main.post(() -> {
                        connecting = false;
                        if (!released) {
                            listener.onDisconnected();
                        }
                    });
                }
            });
        } catch (RuntimeException e) {
            connecting = false;
            listener.onSetupFinished(BillingClient.BillingResponseCode.ERROR);
        }
    }

    /**
     * Pide los productos integrados de {@code skus} (uno por línea). Responde con {@link AfBillingListener#onProducts}: los que
     * tienen oferta de compra única, con su moneda y su importe en micros (nunca la cadena ya formateada de Play).
     */
    public void queryProducts(final int requestId, final String skus) {
        main.post(() -> {
            if (released) {
                return;
            }

            create();
            latestProductsRequest = requestId; // details sigue con la consulta anterior hasta que responda esta (onProductDetails)
            List<QueryProductDetailsParams.Product> products = new ArrayList<>();
            for (String sku : lines(skus)) {
                products.add(QueryProductDetailsParams.Product.newBuilder()
                        .setProductId(sku)
                        .setProductType(BillingClient.ProductType.INAPP)
                        .build());
            }

            if (products.isEmpty()) {
                details = new HashMap<>(); // nada pedido: nada que vender
                listener.onProducts(requestId, OK, "");
                return;
            }

            try {
                client.queryProductDetailsAsync(
                        QueryProductDetailsParams.newBuilder().setProductList(products).build(),
                        (result, found) -> main.post(() -> onProductDetails(requestId, result, found)));
            } catch (RuntimeException e) {
                listener.onProducts(requestId, BillingClient.BillingResponseCode.ERROR, null);
            }
        });
    }

    private void onProductDetails(int requestId, BillingResult result, QueryProductDetailsResult found) {
        if (released) {
            return;
        }

        int code = code(result);
        if (requestId != latestProductsRequest) {
            listener.onProducts(requestId, code, null); // una consulta anterior: C# ya no la espera y no toca los productos
            return;
        }

        if (code != OK || found == null) {
            details = new HashMap<>(); // la consulta más reciente falló: no se vende nada (C# queda sin Play hasta otro intento)
            listener.onProducts(requestId, code == OK ? BillingClient.BillingResponseCode.ERROR : code, null);
            return;
        }

        Map<String, ProductDetails> fresh = new HashMap<>(); // solo se vende lo que Play devuelva a esta consulta
        StringBuilder rows = new StringBuilder();
        List<ProductDetails> list = found.getProductDetailsList();
        if (list != null) {
            for (ProductDetails pd : list) {
                ProductDetails.OneTimePurchaseOfferDetails offer = pd == null ? null : pd.getOneTimePurchaseOfferDetails();
                String sku = pd == null ? null : pd.getProductId();
                String currency = offer == null ? null : offer.getPriceCurrencyCode();
                if (!clean(sku) || !clean(currency)) {
                    continue;
                }

                fresh.put(sku, pd);
                appendRow(rows, sku, currency, Long.toString(offer.getPriceAmountMicros()));
            }
        }

        details = fresh;
        listener.onProducts(requestId, OK, rows.toString());
    }

    /**
     * Pide la configuración de facturación de la cuenta de Play ({@code getBillingConfigAsync}) y responde con
     * {@link AfBillingListener#onBillingConfig} solo su país ({@code countryCode}, ISO 3166-1 alfa-2): el país de la cuenta, segunda
     * fuente del país legal de ConsentCountry (D-057, P4 (a); docs/dev/consentimiento.md). No guarda nada.
     */
    public void queryBillingConfig(final int requestId) {
        main.post(() -> {
            if (released) {
                return;
            }

            create();
            try {
                client.getBillingConfigAsync(GetBillingConfigParams.newBuilder().build(),
                        (result, config) -> main.post(() -> {
                            if (released) {
                                return;
                            }

                            int code = code(result);
                            String country = code == OK && config != null ? config.getCountryCode() : null;
                            listener.onBillingConfig(requestId, code, clean(country) ? country : null);
                        }));
            } catch (RuntimeException e) {
                listener.onBillingConfig(requestId, BillingClient.BillingResponseCode.ERROR, null);
            }
        });
    }

    /** Consulta las compras de productos integrados de la cuenta; responde con {@link AfBillingListener#onPurchases}. */
    public void queryPurchases(final int requestId) {
        main.post(() -> {
            if (released) {
                return;
            }

            create();
            try {
                client.queryPurchasesAsync(
                        QueryPurchasesParams.newBuilder().setProductType(BillingClient.ProductType.INAPP).build(),
                        (result, purchases) -> main.post(() -> {
                            if (released) {
                                return;
                            }

                            int code = code(result);
                            listener.onPurchases(requestId, code, code == OK ? rows(purchases) : null);
                        }));
            } catch (RuntimeException e) {
                listener.onPurchases(requestId, BillingClient.BillingResponseCode.ERROR, null);
            }
        });
    }

    /**
     * Abre la hoja de Play para {@code sku} (ya cargado con {@link #queryProducts}) con {@code setObfuscatedAccountId} =
     * {@code obfuscatedAccountId} si no es null ni vacío (el UID anónimo; 08 §10.1). El resultado llega por
     * {@link #onPurchasesUpdated}; si la hoja no se abre, por {@link AfBillingListener#onLaunchFailed}.
     */
    public void launch(final Activity activity, final String sku, final String obfuscatedAccountId) {
        main.post(() -> launchWhenReady(activity, sku, obfuscatedAccountId, 0));
    }

    private void launchWhenReady(final Activity activity, final String sku, final String obfuscatedAccountId, final int polls) {
        if (released) {
            return;
        }

        create();
        if (!client.isReady() && polls < LAUNCH_POLLS) {
            // Sin conexión (el servicio se cayó): la biblioteca se reconecta sola; se espera un poco antes de abrir la hoja, para no
            // responder SERVICE_DISCONNECTED mientras se reconecta. Si no llega, launchBillingFlow responde con su código.
            if (client.getConnectionState() == BillingClient.ConnectionState.DISCONNECTED && !connecting && !waitingReconnect) {
                startOrWait(0);
            }

            main.postDelayed(() -> launchWhenReady(activity, sku, obfuscatedAccountId, polls + 1), RECONNECT_POLL_MS);
            return;
        }

        ProductDetails pd = sku == null ? null : details.get(sku);
        if (pd == null || activity == null) {
            listener.onLaunchFailed(sku, BillingClient.BillingResponseCode.ITEM_UNAVAILABLE);
            return;
        }

        // Si tras la espera sigue sin conexión, launchBillingFlow (con enableAutoServiceReconnection) lo intenta; si no puede, devuelve
        // el código del fallo y la hoja no se abre.
        try {
            BillingFlowParams.ProductDetailsParams.Builder product =
                    BillingFlowParams.ProductDetailsParams.newBuilder().setProductDetails(pd);
            ProductDetails.OneTimePurchaseOfferDetails offer = pd.getOneTimePurchaseOfferDetails();
            if (offer != null && offer.getOfferToken() != null && !offer.getOfferToken().isEmpty()) {
                product.setOfferToken(offer.getOfferToken()); // la oferta cuyo precio enseña el Mercado
            }

            BillingFlowParams.Builder flow = BillingFlowParams.newBuilder()
                    .setProductDetailsParamsList(Collections.singletonList(product.build()));
            if (obfuscatedAccountId != null && !obfuscatedAccountId.isEmpty()) {
                flow.setObfuscatedAccountId(obfuscatedAccountId);
            }

            int code = code(client.launchBillingFlow(activity, flow.build()));
            if (code != OK) {
                listener.onLaunchFailed(sku, code);
            }
        } catch (RuntimeException e) {
            listener.onLaunchFailed(sku, BillingClient.BillingResponseCode.ERROR);
        }
    }

    /** Consume un consumible ya acreditado; responde con {@link AfBillingListener#onConsumed}. */
    public void consume(final String token) {
        main.post(() -> {
            if (released) {
                return;
            }

            create();
            if (!clean(token)) {
                listener.onConsumed(token, BillingClient.BillingResponseCode.DEVELOPER_ERROR);
                return;
            }

            try {
                client.consumeAsync(ConsumeParams.newBuilder().setPurchaseToken(token).build(),
                        (result, consumedToken) -> main.post(() -> {
                            if (!released) {
                                listener.onConsumed(token, code(result));
                            }
                        }));
            } catch (RuntimeException e) {
                listener.onConsumed(token, BillingClient.BillingResponseCode.ERROR);
            }
        });
    }

    /** Reconoce sin consumir (no consumibles al terminar; cualquier compra en el deferred_ack); responde con {@link AfBillingListener#onAcknowledged}. */
    public void acknowledge(final String token) {
        main.post(() -> {
            if (released) {
                return;
            }

            create();
            if (!clean(token)) {
                listener.onAcknowledged(token, BillingClient.BillingResponseCode.DEVELOPER_ERROR);
                return;
            }

            try {
                client.acknowledgePurchase(AcknowledgePurchaseParams.newBuilder().setPurchaseToken(token).build(),
                        result -> main.post(() -> {
                            if (!released) {
                                listener.onAcknowledged(token, code(result));
                            }
                        }));
            } catch (RuntimeException e) {
                listener.onAcknowledged(token, BillingClient.BillingResponseCode.ERROR);
            }
        });
    }

    /** Suelta la conexión con Play (al recargar la escena); después no avisa de nada más. */
    public void release() {
        main.post(() -> {
            if (released) {
                return;
            }

            released = true;
            details.clear();
            if (client != null) {
                try {
                    client.endConnection();
                } catch (RuntimeException e) {
                    // ya desconectado: nada que soltar
                }
            }
        });
    }

    @Override
    public void onPurchasesUpdated(BillingResult result, List<Purchase> purchases) {
        // Play lo llama en el hilo principal; se encola igual para conservar el orden con el resto de avisos.
        main.post(() -> {
            if (!released) {
                int code = code(result);
                listener.onPurchasesUpdated(code, code == OK ? rows(purchases) : null);
            }
        });
    }

    private static String rows(List<Purchase> purchases) {
        StringBuilder rows = new StringBuilder();
        if (purchases == null) {
            return "";
        }

        for (Purchase p : purchases) {
            if (p == null) {
                continue;
            }

            List<String> products = p.getProducts();
            String sku = products == null || products.size() != 1 ? null : products.get(0); // compras de un solo producto
            if (p.getQuantity() != 1) {
                continue; // varias unidades: no se acredita ni se termina (Q-BILL-4; sin reconocer, Play la reembolsa)
            }
            String token = p.getPurchaseToken();
            int state = p.getPurchaseState();
            if (!clean(sku) || !clean(token)
                    || (state != Purchase.PurchaseState.PURCHASED && state != Purchase.PurchaseState.PENDING)) {
                continue;
            }

            appendRow(rows, sku, token, Integer.toString(state), Long.toString(p.getPurchaseTime()), p.isAcknowledged() ? "1" : "0");
        }

        return rows.toString();
    }

    private static void appendRow(StringBuilder rows, String... fields) {
        if (rows.length() > 0) {
            rows.append('\n');
        }

        for (int i = 0; i < fields.length; i++) {
            if (i > 0) {
                rows.append('\t');
            }

            rows.append(fields[i]);
        }
    }

    private static List<String> lines(String text) {
        List<String> list = new ArrayList<>();
        if (text == null) {
            return list;
        }

        for (String line : text.split("\n")) {
            if (clean(line)) {
                list.add(line);
            }
        }

        return list;
    }

    /** No vacío y sin separadores de las filas. */
    private static boolean clean(String value) {
        return value != null && !value.isEmpty() && value.indexOf('\t') < 0 && value.indexOf('\n') < 0 && value.indexOf('\r') < 0;
    }

    private static int code(BillingResult result) {
        return result == null ? BillingClient.BillingResponseCode.ERROR : result.getResponseCode();
    }
}
