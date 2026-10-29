package com.amazonfill.audiofocus;

import android.content.Context;
import android.media.AudioAttributes;
import android.media.AudioFocusRequest;
import android.media.AudioManager;
import android.os.Handler;
import android.os.Looper;

/**
 * El foco de audio del juego (observacion 10 de la jornada 6: con una alarma sonando, la musica del juego seguia de fondo; D-026,
 * anadido del 2026-10-29). Solo API de la plataforma ({@code android.media}, API 26: el minSdk), sin permisos ni dependencias.
 *
 * <p>Como funciona:
 * <ul>
 * <li>El juego pide el foco ({@link #request(Context, Listener)}) con {@code AUDIOFOCUS_GAIN}, {@code USAGE_GAME} y
 * {@code CONTENT_TYPE_MUSIC}, mientras esta en primer plano y con la Musica encendida, y lo suelta ({@link #abandon()}) al pasar a
 * segundo plano o al apagar la Musica (asi no corta la musica de otra app del jugador sin motivo).</li>
 * <li>Android avisa al {@link Listener} cuando otro toma el foco o lo devuelve: una alarma o el timbre de una llamada
 * ({@code AUDIOFOCUS_LOSS_TRANSIENT}), un aviso que se puede oir por encima ({@code AUDIOFOCUS_LOSS_TRANSIENT_CAN_DUCK}), otra app
 * de musica ({@code AUDIOFOCUS_LOSS}) y la vuelta ({@code AUDIOFOCUS_GAIN}). El aviso llega como texto ({@link #GAIN},
 * {@link #DUCK}, {@link #LOSS_TRANSIENT} o {@link #LOSS}), en el hilo principal de Android; C# lo guarda y lo aplica en su
 * fotograma (AF.Audio.Unity.AndroidAudioFocus).</li>
 * <li>{@code setWillPauseWhenDucked(true)}: con un aviso que se puede oir por encima, Android no baja el volumen el solo (el
 * ducking automatico de Android 8+), sino que avisa y el juego baja la musica al 20 %; asi lo decide y lo dice la sonda.</li>
 * </ul>
 * Nada de red, disco, registros ni identificadores.
 */
public final class AfAudioFocus {
    /** El juego tiene el foco: todo suena. */
    public static final String GAIN = "gain";

    /** Otro suena por encima un momento y deja oir lo de debajo: la musica baja. */
    public static final String DUCK = "duck";

    /** Otro toma el foco un momento (alarma, timbre de una llamada): la musica y los efectos se pausan hasta {@link #GAIN}. */
    public static final String LOSS_TRANSIENT = "loss_transient";

    /** Otro toma el foco sin plazo (otra app de musica): se pausa hasta volver a pedirlo. */
    public static final String LOSS = "loss";

    /** {@link #request}: Android no da el foco ahora (p. ej., durante una llamada). */
    public static final String DENIED = "denied";

    /** {@link #request}: no se pudo pedir (sin AudioManager). */
    public static final String ERROR = "error";

    /** Lo que avisa de los cambios del foco (lo implementa C# con un AndroidJavaProxy). */
    public interface Listener {
        /** {@link #GAIN}, {@link #DUCK}, {@link #LOSS_TRANSIENT} o {@link #LOSS}, en el hilo principal. */
        void onFocus(String state);
    }

    private static final Object LOCK = new Object();
    private static AudioManager manager;
    private static AudioFocusRequest pending;
    private static volatile Listener listener;
    private static volatile String last = "";

    private static final AudioManager.OnAudioFocusChangeListener CHANGES = AfAudioFocus::onChange;

    private AfAudioFocus() {
    }

    /**
     * Pide el foco (cualquier hilo). Devuelve {@link #GAIN} si Android lo da, {@link #DENIED} si no y {@link #ERROR} si no se
     * pudo pedir. Si ya se tenia, lo vuelve a pedir (tras un {@link #LOSS}, Android no lo devuelve solo).
     */
    public static String request(Context context, Listener target) {
        synchronized (LOCK) {
            try {
                if (manager == null) {
                    Context app = context == null ? null : context.getApplicationContext();
                    manager = app == null ? null : (AudioManager) app.getSystemService(Context.AUDIO_SERVICE);
                }

                if (manager == null) {
                    return ERROR;
                }

                listener = target;
                if (pending == null) {
                    AudioAttributes attributes = new AudioAttributes.Builder()
                        .setUsage(AudioAttributes.USAGE_GAME)
                        .setContentType(AudioAttributes.CONTENT_TYPE_MUSIC)
                        .build();
                    pending = new AudioFocusRequest.Builder(AudioManager.AUDIOFOCUS_GAIN)
                        .setAudioAttributes(attributes)
                        .setWillPauseWhenDucked(true)
                        .setAcceptsDelayedFocusGain(false)
                        .setOnAudioFocusChangeListener(CHANGES, new Handler(Looper.getMainLooper()))
                        .build();
                }

                int result = manager.requestAudioFocus(pending);
                String state = result == AudioManager.AUDIOFOCUS_REQUEST_GRANTED ? GAIN : DENIED;
                last = state;
                return state;
            } catch (RuntimeException e) {
                return ERROR;
            }
        }
    }

    /** Suelta el foco (cualquier hilo); sin efecto si no se pidio. Despues no llega ningun aviso. */
    public static void abandon() {
        synchronized (LOCK) {
            listener = null;
            last = "";
            if (manager != null && pending != null) {
                try {
                    manager.abandonAudioFocusRequest(pending);
                } catch (RuntimeException e) {
                    // el sistema ya lo solto
                }
            }
        }
    }

    /** El ultimo estado conocido (vacio sin pedir): solo para la sonda. */
    public static String last() {
        return last;
    }

    /** El texto de un cambio del foco de Android; vacio para los que no se usan. */
    static String name(int change) {
        switch (change) {
            case AudioManager.AUDIOFOCUS_GAIN:
            case AudioManager.AUDIOFOCUS_GAIN_TRANSIENT:
            case AudioManager.AUDIOFOCUS_GAIN_TRANSIENT_MAY_DUCK:
            case AudioManager.AUDIOFOCUS_GAIN_TRANSIENT_EXCLUSIVE:
                return GAIN;
            case AudioManager.AUDIOFOCUS_LOSS_TRANSIENT_CAN_DUCK:
                return DUCK;
            case AudioManager.AUDIOFOCUS_LOSS_TRANSIENT:
                return LOSS_TRANSIENT;
            case AudioManager.AUDIOFOCUS_LOSS:
                return LOSS;
            default:
                return "";
        }
    }

    // En el hilo principal (el Handler del pedido).
    private static void onChange(int change) {
        String state = name(change);
        Listener target = listener;
        if (state.isEmpty() || target == null) {
            return;
        }

        last = state;
        try {
            target.onFocus(state);
        } catch (RuntimeException e) {
            // C# ya no escucha (la escena se cerro): el siguiente pedido trae otro
        }
    }
}
