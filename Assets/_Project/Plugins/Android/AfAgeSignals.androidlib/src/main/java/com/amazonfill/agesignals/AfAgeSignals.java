package com.amazonfill.agesignals;

import android.content.Context;
import android.os.Handler;
import android.os.HandlerThread;
import com.google.android.play.agesignals.AgeSignalsException;
import com.google.android.play.agesignals.AgeSignalsManager;
import com.google.android.play.agesignals.AgeSignalsManagerFactory;
import com.google.android.play.agesignals.AgeSignalsRequest;
import com.google.android.play.agesignals.AgeSignalsResult;

/**
 * Puente de Play Age Signals API 0.0.4 hacia C# (AF.UI.AndroidAgeSignals; T-L3). Pide la franja de edad a Google Play una vez y
 * la entrega a {@link Listener#onResult(String)} con el formato de {@code AF.Platform.AgeSignal.Parse}:
 * <ul>
 *   <li>{@code range:<inferior>[:<superior>]}: la franja (en la 0.0.4, {@code ageUpper()} es null en la banda alta, 18+);</li>
 *   <li>{@code none}: sin franja (no compartida, sin verificar o fuera de los territorios con señal: edades null);</li>
 *   <li>{@code error[:<código>]}: la llamada falló (código de {@code AgeSignalsErrorCode}).</li>
 * </ul>
 * Solo se leen {@code ageLower()} y {@code ageUpper()}: nunca el identificador de instalación ni otros campos. La señal solo sirve para dar
 * una experiencia adecuada a la edad, nunca para analítica ni publicidad (T-L4). Nada se escribe en el log.
 *
 * <p>Hilos: la respuesta sale desde un hilo de fondo propio ({@value #CALLBACK_THREAD}), nunca desde el hilo principal de Android
 * (donde la {@code Task} de Play entrega por defecto). El oyente es un AndroidJavaProxy de C# y llamarlo espera al hilo de Unity: si
 * el jugador sale de la app mientras Play responde, Unity está en pausa y, desde el hilo principal, eso sería el interbloqueo (y
 * ANR) de Play Billing en la 1.0.0 (10001) (AfBilling, 2026-10-31; docs/dev/iap.md). Que el hilo de avisos espere a Unity no
 * bloquea nada.
 */
public final class AfAgeSignals {
    /** Nombre del hilo de fondo que entrega la respuesta al {@link Listener}. */
    static final String CALLBACK_THREAD = "AfAgeSignals-callbacks";

    private static final Object LOCK = new Object();
    /** El hilo de las respuestas: uno para todo el proceso, creado con el primer pedido. */
    private static Handler callbacks;

    /** Respuesta hacia C# (AndroidJavaProxy), en el hilo {@value #CALLBACK_THREAD}; C# la recoge en el de Unity. */
    public interface Listener {
        void onResult(String wire);
    }

    private AfAgeSignals() {
    }

    /** Pide la señal; la respuesta llega una sola vez por {@code listener}. Nunca lanza: un fallo llega como {@code error}. */
    public static void request(Context context, final Listener listener) {
        if (listener == null) {
            return;
        }

        try {
            final Handler handler = callbacks();
            AgeSignalsManager manager = AgeSignalsManagerFactory.create(context.getApplicationContext());
            // Los oyentes de la Task corren en el hilo de avisos (el Executor), no en el principal.
            manager.checkAgeSignals(AgeSignalsRequest.builder().build())
                    .addOnSuccessListener(handler::post, result -> deliver(listener, toWire(result)))
                    .addOnFailureListener(handler::post, e -> deliver(listener, errorWire(e)));
        } catch (Throwable t) {
            deliver(listener, "error"); // en el hilo que pidió (el de Unity), nunca el principal
        }
    }

    // El Handler del hilo de avisos; lo crea el primer pedido.
    private static Handler callbacks() {
        synchronized (LOCK) {
            if (callbacks == null) {
                HandlerThread thread = new HandlerThread(CALLBACK_THREAD);
                thread.start();
                callbacks = new Handler(thread.getLooper());
            }

            return callbacks;
        }
    }

    /** La franja en el formato del puente. */
    static String toWire(AgeSignalsResult result) {
        if (result == null) {
            return "none";
        }

        Integer lower = result.ageLower();
        Integer upper = result.ageUpper();
        if (lower == null || lower < 0) {
            return "none";
        }

        if (upper == null) {
            return "range:" + lower;
        }

        return upper < lower ? "error" : "range:" + lower + ":" + upper;
    }

    static String errorWire(Exception e) {
        if (e instanceof AgeSignalsException) {
            return "error:" + ((AgeSignalsException) e).getErrorCode();
        }

        return "error";
    }

    private static void deliver(Listener listener, String wire) {
        try {
            listener.onResult(wire);
        } catch (Throwable ignored) {
            // C# no debe tumbar el hilo de Play.
        }
    }
}
