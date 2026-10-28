package com.amazonfill.splash;

import android.app.Activity;
import android.app.Application;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.Process;
import android.os.SystemClock;
import android.view.View;
import android.view.ViewTreeObserver;
import android.view.Window;
import java.lang.reflect.Method;

/**
 * Retiene la pantalla de arranque del sistema (el icono v2 sobre el petroleo) hasta que la intro de la marca ya esta en la
 * pantalla (anadido de D-051 del 2026-10-28: «si hagamoslo asi»). Sin el, entre el icono y la intro se veia el petroleo liso de
 * la ventana de Unity (jornada 5: ~1,3 s en la primera apertura, ~0,45 s en frio).
 *
 * <p>Como funciona, solo con API de la plataforma (la misma que usa la biblioteca core-splashscreen de Jetpack para setKeepOnScreenCondition):
 * <ul>
 * <li>Android quita la pantalla del sistema (Android 12+) o la ventana de arranque con el fondo del tema (Android 8-11, el mismo
 * icono sobre el petroleo: af_launch_window) cuando la ventana de la actividad pinta su primer fotograma. Un
 * {@link ViewTreeObserver.OnPreDrawListener} en la ventana de la actividad de Unity que devuelve false aplaza ese fotograma: la
 * pantalla del sistema se queda. La SurfaceView de Unity se crea igual (su propio OnPreDrawListener sigue corriendo) y Unity pinta
 * en ella por detras.</li>
 * <li>Unity solo arranca su bucle con el foco de la ventana, y Android no da el foco a una ventana que aun no se ha pintado: sin
 * mas, la retencion se esperaria a si misma hasta el tope. Por eso, mientras retiene, Unity corre sin foco
 * ({@code UnityPlayer.setRunWithoutFocus(true)}, API publica de Unity, por reflexion) y, en cuanto la ventana recibe el foco tras
 * soltarla (o si la actividad se pausa), vuelve a lo de siempre ({@code false}). Si no se puede (otra actividad, otra version de
 * Unity), no se retiene nada.</li>
 * <li>Se suelta con {@link #release(String)} (el juego, por JNI, cuando la intro ya esta en la pantalla) o, como mucho, a los
 * {@link #CAP_MS} ms: la pantalla nunca se queda colgada.</li>
 * </ul>
 * Solo la primera actividad que empieza en el proceso (el arranque en frio); nada de red, disco, registros ni identificadores.
 */
public final class AfSplashHold {
    /** Tope de la retencion (ms desde que empieza la actividad de Unity) [P]. AF.UI.AndroidOsSplash.CapMs lo repite. */
    public static final long CAP_MS = 3000L;

    /** Sin retencion: aun no empezo la actividad, no es la de Unity o Unity no deja correr sin foco. */
    public static final String RESULT_NONE = "none";

    /** Reteniendo: la pantalla del sistema sigue delante. */
    public static final String RESULT_PENDING = "pending";

    /** Soltada por el juego ({@link #release(String)}). */
    public static final String RESULT_SIGNAL = "signal";

    /** Soltada por el tope ({@link #CAP_MS}). */
    public static final String RESULT_CAP = "cap";

    /** Soltada porque la actividad se destruyo antes. */
    public static final String RESULT_GONE = "gone";

    private static final Handler MAIN = new Handler(Looper.getMainLooper());

    // Todo el estado se cambia en el hilo principal; los volatile se leen desde el de Unity (las sondas).
    private static boolean installed;
    private static boolean armed;
    private static boolean runWithoutFocus;
    private static Activity holder;
    private static View decor;
    private static long startedAt = -1L;
    private static long releasedAt = -1L;
    private static volatile String result = RESULT_NONE;
    private static volatile String signal = "";
    private static volatile long heldMs = -1L;
    private static volatile long procReleaseMs = -1L;
    private static volatile long focusAfterMs = -1L;

    private static final ViewTreeObserver.OnPreDrawListener HOLD = () -> !RESULT_PENDING.equals(result);

    private static final ViewTreeObserver.OnWindowFocusChangeListener FOCUS = hasFocus -> {
        if (hasFocus) {
            onFocus();
        }
    };

    private static final Runnable CAP = () -> finish(RESULT_CAP, "");

    private static final Runnable RESTORE = AfSplashHold::restoreFocusRule;

    private static final Application.ActivityLifecycleCallbacks CALLBACKS = new Application.ActivityLifecycleCallbacks() {
        @Override
        public void onActivityCreated(Activity activity, Bundle savedInstanceState) {
        }

        @Override
        public void onActivityStarted(Activity activity) {
            // Despues de onCreate (la ventana ya tiene su contenido y Unity su UnityPlayer) y antes de onResume, que es donde Unity
            // decide si arranca su bucle: ahi ya tiene que poder correr sin foco.
            arm(activity);
        }

        @Override
        public void onActivityResumed(Activity activity) {
        }

        @Override
        public void onActivityPaused(Activity activity) {
            if (activity == holder) {
                restoreFocusRule(); // fuera de la pantalla, Unity se pausa como siempre
            }
        }

        @Override
        public void onActivityStopped(Activity activity) {
        }

        @Override
        public void onActivitySaveInstanceState(Activity activity, Bundle outState) {
        }

        @Override
        public void onActivityDestroyed(Activity activity) {
            if (activity == holder) {
                finish(RESULT_GONE, "");
                restoreFocusRule();
                holder = null;
                decor = null;
            }
        }
    };

    private AfSplashHold() {
    }

    /** Lo llama {@link AfSplashHoldProvider} al arrancar el proceso, antes de crear ninguna actividad. */
    static void install(Application app) {
        if (installed || app == null) {
            return;
        }

        installed = true;
        app.registerActivityLifecycleCallbacks(CALLBACKS);
    }

    /**
     * Suelta la pantalla del sistema (cualquier hilo; JNI desde AF.UI.AndroidOsSplash). {@code by} dice que la solto en el juego
     * (intro, splash, no_splash o boot_end). Sin efecto si no esta reteniendo.
     */
    public static void release(String by) {
        final String why = by == null ? "" : by;
        MAIN.post(() -> finish(RESULT_SIGNAL, why));
    }

    /** none, pending, signal, cap o gone. */
    public static String result() {
        return result;
    }

    /** Lo que dijo el juego al soltarla ({@link #release(String)}); vacio si no la solto el. */
    public static String signal() {
        return signal;
    }

    /** Lo que se retuvo (ms desde que empezo la actividad de Unity hasta soltarla); -1 si no se retuvo o sigue. */
    public static long heldMs() {
        return heldMs;
    }

    /** Del arranque del proceso a soltarla (ms); -1 si no. */
    public static long procReleaseMs() {
        return procReleaseMs;
    }

    /** De soltarla a que la ventana recibio el foco (ms); -1 si aun no. */
    public static long focusAfterMs() {
        return focusAfterMs;
    }

    private static void arm(Activity activity) {
        if (armed) {
            return; // solo el arranque en frio: la primera actividad que empieza en el proceso
        }

        armed = true;
        Window window = activity.getWindow();
        View view = window == null ? null : window.getDecorView();
        if (view == null || !setRunWithoutFocus(activity, true)) {
            return; // no es la actividad de Unity, o no deja correr sin foco: retener la dejaria quieta hasta el tope
        }

        runWithoutFocus = true;
        holder = activity;
        decor = view;
        startedAt = SystemClock.uptimeMillis();
        result = RESULT_PENDING;
        view.getViewTreeObserver().addOnPreDrawListener(HOLD);
        MAIN.postDelayed(CAP, CAP_MS);
    }

    // En el hilo principal.
    private static void finish(String why, String by) {
        if (!RESULT_PENDING.equals(result)) {
            return;
        }

        long now = SystemClock.uptimeMillis();
        releasedAt = now;
        heldMs = now - startedAt;
        procReleaseMs = SystemClock.elapsedRealtime() - Process.getStartElapsedRealtime();
        signal = by;
        result = why;
        MAIN.removeCallbacks(CAP);
        View view = decor;
        if (view == null) {
            return;
        }

        ViewTreeObserver observer = view.getViewTreeObserver();
        if (observer.isAlive()) {
            observer.removeOnPreDrawListener(HOLD);
        }

        view.invalidate(); // el fotograma aplazado sale ya
        if (RESULT_GONE.equals(why)) {
            return;
        }

        if (view.hasWindowFocus()) {
            onFocus();
        } else if (observer.isAlive()) {
            observer.addOnWindowFocusChangeListener(FOCUS);
        }
    }

    // La ventana ya tiene el foco. La regla de Unity vuelve despues de que su onWindowFocusChanged lo anote (por eso, en un post).
    private static void onFocus() {
        if (releasedAt >= 0L && focusAfterMs < 0L) {
            focusAfterMs = SystemClock.uptimeMillis() - releasedAt;
        }

        View view = decor;
        if (view != null) {
            ViewTreeObserver observer = view.getViewTreeObserver();
            if (observer.isAlive()) {
                observer.removeOnWindowFocusChangeListener(FOCUS);
            }
        }

        MAIN.post(RESTORE);
    }

    private static void restoreFocusRule() {
        if (!runWithoutFocus) {
            return;
        }

        runWithoutFocus = false;
        Activity activity = holder;
        if (activity != null) {
            setRunWithoutFocus(activity, false);
        }
    }

    // UnityPlayerGameActivity.getUnityPlayerConnection().setRunWithoutFocus(on): API publica de Unity, por reflexion para no
    // compilar contra sus clases. False si la actividad no es la de Unity o falla.
    private static boolean setRunWithoutFocus(Activity activity, boolean on) {
        try {
            Method connection = activity.getClass().getMethod("getUnityPlayerConnection");
            Object player = connection.invoke(activity);
            if (player == null) {
                return false;
            }

            Method set = player.getClass().getMethod("setRunWithoutFocus", boolean.class);
            set.invoke(player, on);
            return true;
        } catch (ReflectiveOperationException | RuntimeException e) {
            return false;
        }
    }
}
