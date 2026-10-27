package com.amazonfill.agesignals;

import android.content.Context;
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
 */
public final class AfAgeSignals {
    /** Respuesta hacia C# (AndroidJavaProxy), en cualquier hilo; C# la recoge en el de Unity. */
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
            AgeSignalsManager manager = AgeSignalsManagerFactory.create(context.getApplicationContext());
            manager.checkAgeSignals(AgeSignalsRequest.builder().build())
                    .addOnSuccessListener(result -> deliver(listener, toWire(result)))
                    .addOnFailureListener(e -> deliver(listener, errorWire(e)));
        } catch (Throwable t) {
            deliver(listener, "error");
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
