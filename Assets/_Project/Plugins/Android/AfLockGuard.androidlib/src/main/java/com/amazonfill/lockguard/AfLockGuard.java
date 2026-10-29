package com.amazonfill.lockguard;

import android.annotation.TargetApi;
import android.app.Activity;
import android.app.Application;
import android.content.pm.ApplicationInfo;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.view.Window;
import android.view.WindowManager;

/**
 * El juego nunca se muestra ni se juega sobre la pantalla de bloqueo (observacion 9 de la jornada 6 de la prueba en el
 * telefono, 2026-10-28; nota tecnica en docs/dev/decisiones.md).
 *
 * <p>Causa: la variante de desarrollo del reproductor de Unity 6000.3.25f1 (la que lleva un build con
 * {@code BuildOptions.Development}, como {@code dev.apk}) en el constructor de {@code UnityPlayer}
 * ({@code developmentPlayerInitialize}) anade a su ventana ({@code Window.addFlags}) FLAG_TURN_SCREEN_ON, FLAG_SHOW_WHEN_LOCKED y
 * FLAG_DISMISS_KEYGUARD, y su dialogo del teclado en pantalla pone a true {@code Activity.setShowWhenLocked} y
 * {@code setTurnScreenOn} (Android 9+) sin deshacerlo. No hay ajuste del Player ni meta-data que lo apague; la variante de
 * release deja esos metodos vacios. Como la prueba en el telefono se hace con el build de desarrollo, se quita aqui, en todos
 * los builds (en release no hay nada que quitar).
 *
 * <p>Como: en cada paso del ciclo de vida de la actividad de Unity (despues de crearla, al empezar, al reanudar, al pausar y al
 * parar) se quitan esas tres banderas de su ventana y, en Android 8.1+, se llama a {@code setShowWhenLocked(false)} y
 * {@code setTurnScreenOn(false)}. Al pausar y al parar (antes de que se apague la pantalla) es lo que cuenta: al volver a
 * encenderla, Android ensena la pantalla de bloqueo y no el juego. Solo la actividad de Unity: la pantalla de un anuncio de
 * Google ({@code com.google.android.gms.ads.AdActivity}) no es nuestra y no se toca.
 *
 * <p>Sonda de desarrollo: solo en un build depurable ({@code FLAG_DEBUGGABLE}), al reanudar la actividad de Unity escribe en
 * logcat {@code AF_LOCK {json}} con las banderas de su ventana despues de limpiarlas ({@code window_flags},
 * {@code show_when_locked}, {@code turn_screen_on}, {@code dismiss_keyguard}), las que encontro puestas ({@code found}) y
 * cuantas veces tuvo que quitarlas ({@code cleared}). Nada de red, disco ni identificadores.
 */
public final class AfLockGuard {
    /** Las tres banderas de ventana que dejan ver y usar el juego sobre la pantalla de bloqueo. */
    @SuppressWarnings("deprecation")
    public static final int LOCK_FLAGS = WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED
            | WindowManager.LayoutParams.FLAG_TURN_SCREEN_ON
            | WindowManager.LayoutParams.FLAG_DISMISS_KEYGUARD;

    /** La etiqueta de logcat de la sonda de desarrollo. */
    public static final String PROBE_TAG = "AF_LOCK";

    private static boolean installed;

    // Solo en el hilo principal (los callbacks); se lee en la misma sonda.
    private static int clearedCount;
    private static int lastFound;

    private static final Application.ActivityLifecycleCallbacks CALLBACKS = new Application.ActivityLifecycleCallbacks() {
        @Override
        public void onActivityCreated(Activity activity, Bundle savedInstanceState) {
            guard(activity);
        }

        // Android 10+: despues del onCreate de Unity, donde el reproductor de desarrollo pone las banderas.
        @Override
        public void onActivityPostCreated(Activity activity, Bundle savedInstanceState) {
            guard(activity);
        }

        @Override
        public void onActivityStarted(Activity activity) {
            guard(activity);
        }

        @Override
        public void onActivityResumed(Activity activity) {
            if (guard(activity)) {
                probe(activity);
            }
        }

        @Override
        public void onActivityPaused(Activity activity) {
            guard(activity);
        }

        @Override
        public void onActivityStopped(Activity activity) {
            guard(activity);
        }

        @Override
        public void onActivitySaveInstanceState(Activity activity, Bundle outState) {
        }

        @Override
        public void onActivityDestroyed(Activity activity) {
        }
    };

    private AfLockGuard() {
    }

    /** Lo llama {@link AfLockGuardProvider} al arrancar el proceso, antes de crear ninguna actividad. */
    static void install(Application app) {
        if (installed || app == null) {
            return;
        }

        installed = true;
        app.registerActivityLifecycleCallbacks(CALLBACKS);
    }

    // Quita el modo sobre la pantalla de bloqueo si es la actividad de Unity; true si lo es.
    private static boolean guard(Activity activity) {
        if (activity == null || !isUnity(activity)) {
            return false;
        }

        Window window = activity.getWindow();
        if (window != null) {
            int found = window.getAttributes().flags & LOCK_FLAGS;
            if (found != 0) {
                lastFound = found;
                clearedCount++;
                window.clearFlags(LOCK_FLAGS);
            }
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O_MR1) {
            Api27.clear(activity);
        }

        return true;
    }

    // La actividad de Unity (UnityPlayerGameActivity o UnityPlayerActivity, o una subclase), sin reflexion de metodos.
    private static boolean isUnity(Activity activity) {
        for (Class<?> type = activity.getClass(); type != null; type = type.getSuperclass()) {
            String name = type.getName();
            if ("com.unity3d.player.UnityPlayerGameActivity".equals(name) || "com.unity3d.player.UnityPlayerActivity".equals(name)) {
                return true;
            }
        }

        return false;
    }

    // Solo en builds depurables: la sonda AF_LOCK para verificar en el telefono (adb logcat -s AF_LOCK).
    @SuppressWarnings("deprecation")
    private static void probe(Activity activity) {
        ApplicationInfo info = activity.getApplicationInfo();
        if (info == null || (info.flags & ApplicationInfo.FLAG_DEBUGGABLE) == 0) {
            return;
        }

        Window window = activity.getWindow();
        int flags = window == null ? 0 : window.getAttributes().flags;
        String json = "{\"window_flags\":\"0x" + Integer.toHexString(flags) + "\""
                + ",\"show_when_locked\":" + ((flags & WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED) != 0)
                + ",\"turn_screen_on\":" + ((flags & WindowManager.LayoutParams.FLAG_TURN_SCREEN_ON) != 0)
                + ",\"dismiss_keyguard\":" + ((flags & WindowManager.LayoutParams.FLAG_DISMISS_KEYGUARD) != 0)
                + ",\"activity_cleared\":" + (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O_MR1)
                + ",\"found\":\"0x" + Integer.toHexString(lastFound) + "\""
                + ",\"cleared\":" + clearedCount
                + ",\"api\":" + Build.VERSION.SDK_INT + "}";
        Log.i(PROBE_TAG, json);
    }

    // Lo de Android 8.1+ (API 27) aparte, para que Android 8.0 no lo cargue nunca.
    @TargetApi(27)
    private static final class Api27 {
        private Api27() {
        }

        static void clear(Activity activity) {
            activity.setShowWhenLocked(false);
            activity.setTurnScreenOn(false);
        }
    }
}
