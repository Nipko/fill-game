package com.amazonfill.splash;

import android.annotation.TargetApi;
import android.app.Activity;
import android.app.Application;
import android.content.res.TypedArray;
import android.graphics.drawable.Drawable;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.Process;
import android.os.SystemClock;
import android.view.View;
import android.view.ViewGroup;
import android.view.ViewParent;
import android.view.Window;
import android.widget.FrameLayout;
import android.window.SplashScreenView;

/**
 * Tapa el petroleo liso entre la pantalla de arranque del sistema y la intro de la marca con un velo nativo que no bloquea nada
 * (anadido de D-051 del 2026-10-28; segundo diseno por la observacion 1 de la jornada 6: la retencion que aplazaba el primer
 * fotograma de la ventana se esperaba a si misma y la soltaba siempre el tope).
 *
 * <p>Como funciona, solo con API de la plataforma:
 * <ul>
 * <li>La ventana de la actividad de Unity se pinta en cuanto Android quiere, como siempre: Android quita su pantalla de arranque,
 * la ventana recibe el foco y Unity arranca como en la jornada 5. Nada aplaza fotogramas ni toca el foco de Unity.</li>
 * <li><b>El velo propio:</b> al empezar la actividad ({@code onActivityStarted}, antes de su primer fotograma) se anade a la
 * DecorView, encima del contenido (y de la SurfaceView de Unity), una {@link View} a pantalla completa cuyo fondo es el
 * {@code android:windowBackground} del tema de la actividad: {@code af_launch_window} (el icono v2 a 240 dp recortado en un
 * circulo de 160 dp sobre el petroleo), el mismo dibujable y la misma caja que la ventana de arranque de Android 8-11. Una vista
 * normal se pinta encima del agujero de la SurfaceView, como la imagen de arranque de la propia Unity.</li>
 * <li><b>Android 12+, ademas, la propia pantalla del sistema:</b> en {@code onActivityCreated} se pide a Android que entregue su
 * pantalla de arranque a la actividad ({@code Activity.getSplashScreen().setOnExitAnimationListener}, API 31) en vez de quitarla;
 * Android la anade como {@code SplashScreenView} encima de la ventana ya pintada, con su icono exactamente donde estaba. Se deja
 * ahi, encima del velo propio, hasta soltar: el icono no puede saltar porque es la misma vista. Si Android no la entrega (un
 * arranque sin pantalla del sistema) o la entrega sin icono (Android 13+, de color liso, fuera del lanzador), se quita en el
 * acto y queda el velo propio.</li>
 * <li>Se suelta con {@link #release(String)} (el juego, por JNI, cuando la intro ya esta en la pantalla), con el tope de
 * {@link #CAP_MS} ms o al destruirse la actividad: las vistas se quitan sin fundido (nada se mueve: vale con Reducir
 * movimiento).</li>
 * <li>Los velos no se enfocan, no cuentan para la accesibilidad (TalkBack no los lee) y no son pulsables: los toques pasan a
 * Unity.</li>
 * </ul>
 * Solo la primera actividad del proceso, y solo si es la de Unity (el arranque en frio); nada de red, disco, registros ni
 * identificadores.
 */
public final class AfSplashHold {
    /** Tope del velo (ms desde que empieza la actividad de Unity) [P]. AF.UI.AndroidOsSplash.CapMs lo repite. */
    public static final long CAP_MS = 3000L;

    /** Sin velo: aun no empezo la actividad, no es la de Unity o su tema no tiene fondo de ventana. */
    public static final String RESULT_NONE = "none";

    /** Con el velo puesto. */
    public static final String RESULT_PENDING = "pending";

    /** Soltado por el juego ({@link #release(String)}). */
    public static final String RESULT_SIGNAL = "signal";

    /** Soltado por el tope ({@link #CAP_MS}). */
    public static final String RESULT_CAP = "cap";

    /** Soltado porque la actividad se destruyo antes. */
    public static final String RESULT_GONE = "gone";

    /** {@link #veil()}: sin velo. */
    public static final String VEIL_NONE = "none";

    /** {@link #veil()}: solo el velo propio (Android 8-11, o Android 12+ sin pantalla del sistema entregada). */
    public static final String VEIL_OWN = "own";

    /** {@link #veil()}: Android 12+ entrego su pantalla de arranque y se quedo encima del velo propio. */
    public static final String VEIL_SYSTEM = "system";

    private static final Handler MAIN = new Handler(Looper.getMainLooper());

    // Todo el estado se cambia en el hilo principal; los volatile se leen desde el de Unity (las sondas).
    private static boolean installed;
    private static boolean seen;
    private static boolean armed;
    private static Activity candidate;
    private static View ownVeil;
    private static View systemVeil;
    private static long startedAt = -1L;
    private static volatile String result = RESULT_NONE;
    private static volatile String signal = "";
    private static volatile String veil = VEIL_NONE;
    private static volatile long heldMs = -1L;
    private static volatile long procReleaseMs = -1L;

    private static final Runnable CAP = () -> finish(RESULT_CAP, "");

    private static final Application.ActivityLifecycleCallbacks CALLBACKS = new Application.ActivityLifecycleCallbacks() {
        @Override
        public void onActivityCreated(Activity activity, Bundle savedInstanceState) {
            // Dentro de onCreate (antes del primer fotograma): ahi Android pide que se registre el oyente de su pantalla.
            if (seen) {
                return; // solo el arranque en frio: la primera actividad del proceso
            }

            seen = true;
            if (!isUnity(activity)) {
                return; // p. ej., Android restaura otra actividad primero: sin velo
            }

            candidate = activity;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                Api31.keepSystemSplash(activity);
            }
        }

        @Override
        public void onActivityStarted(Activity activity) {
            // Despues de onCreate (la ventana ya tiene su contenido y la SurfaceView de Unity) y antes de su primer fotograma.
            if (activity == candidate && !armed) {
                armed = true;
                arm(activity);
            }
        }

        @Override
        public void onActivityResumed(Activity activity) {
        }

        @Override
        public void onActivityPaused(Activity activity) {
        }

        @Override
        public void onActivityStopped(Activity activity) {
        }

        @Override
        public void onActivitySaveInstanceState(Activity activity, Bundle outState) {
        }

        @Override
        public void onActivityDestroyed(Activity activity) {
            if (activity == candidate) {
                finish(RESULT_GONE, "");
                candidate = null;
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
     * Quita el velo (cualquier hilo; JNI desde AF.UI.AndroidOsSplash). {@code by} dice que lo solto en el juego (intro, splash,
     * no_splash o boot_end). Sin efecto si no hay velo.
     */
    public static void release(String by) {
        final String why = by == null ? "" : by;
        MAIN.post(() -> finish(RESULT_SIGNAL, why));
    }

    /** none, pending, signal, cap o gone. */
    public static String result() {
        return result;
    }

    /** Lo que dijo el juego al soltarlo ({@link #release(String)}); vacio si no lo solto el. */
    public static String signal() {
        return signal;
    }

    /** none, own o system: que velo el arranque. */
    public static String veil() {
        return veil;
    }

    /** Lo que duro el velo (ms desde que empezo la actividad de Unity hasta quitarlo); -1 si no hubo o sigue. */
    public static long heldMs() {
        return heldMs;
    }

    /** Del arranque del proceso a quitar el velo (ms); -1 si no. */
    public static long procReleaseMs() {
        return procReleaseMs;
    }

    /**
     * Siempre -1: el velo no toca el foco de la ventana (la retencion anterior lo media de soltarla al foco). Se queda para no
     * cambiar el puente con C# ni la sonda os_splash_focus_ms.
     */
    public static long focusAfterMs() {
        return -1L;
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

    private static void arm(Activity activity) {
        Window window = activity.getWindow();
        View decor = window == null ? null : window.getDecorView();
        Drawable background = windowBackground(activity);
        if (!(decor instanceof ViewGroup) || background == null) {
            return; // sin velo: la pantalla del sistema se quita como siempre
        }

        View view = new View(activity);
        view.setBackground(background);
        makeInert(view);
        ((ViewGroup) decor).addView(view, new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT));
        ownVeil = view;
        startedAt = SystemClock.uptimeMillis();
        if (VEIL_NONE.equals(veil)) {
            veil = VEIL_OWN;
        }

        result = RESULT_PENDING;
        MAIN.postDelayed(CAP, CAP_MS);
    }

    // El fondo de ventana del tema de la actividad (AfLaunchTheme: @drawable/af_launch_window), un dibujable nuevo.
    private static Drawable windowBackground(Activity activity) {
        try {
            TypedArray attrs = activity.getTheme().obtainStyledAttributes(new int[] {android.R.attr.windowBackground});
            try {
                return attrs.getDrawable(0);
            } finally {
                attrs.recycle();
            }
        } catch (RuntimeException e) {
            return null;
        }
    }

    // Ni foco, ni TalkBack, ni toques: el velo solo se ve.
    static void makeInert(View view) {
        view.setFocusable(false);
        view.setFocusableInTouchMode(false);
        view.setClickable(false);
        view.setLongClickable(false);
        view.setSoundEffectsEnabled(false);
        view.setImportantForAccessibility(View.IMPORTANT_FOR_ACCESSIBILITY_NO_HIDE_DESCENDANTS);
    }

    // Android 12+ entrega su pantalla de arranque (hilo principal). Se queda encima mientras haya velo y si trae el icono; si no,
    // se quita ya. Android 13+ la entrega sin icono, de color liso, en los lanzamientos que no vienen del lanzador (p. ej., desde
    // una notificacion): ahi manda el velo propio, con el icono (os_splash_veil = own).
    static void onSystemSplash(View splash) {
        if (RESULT_PENDING.equals(result) && candidate != null && Api31.hasIcon(splash)) {
            makeInert(splash);
            systemVeil = splash;
            veil = VEIL_SYSTEM;
            return;
        }

        Api31.remove(splash);
    }

    // En el hilo principal.
    private static void finish(String why, String by) {
        if (!RESULT_PENDING.equals(result)) {
            return;
        }

        heldMs = SystemClock.uptimeMillis() - startedAt;
        procReleaseMs = SystemClock.elapsedRealtime() - Process.getStartElapsedRealtime();
        signal = by;
        result = why;
        MAIN.removeCallbacks(CAP);
        View own = ownVeil;
        View system = systemVeil;
        ownVeil = null;
        systemVeil = null;
        if (system != null) {
            Api31.remove(system);
        }

        if (own != null) {
            ViewParent parent = own.getParent();
            if (parent instanceof ViewGroup) {
                ((ViewGroup) parent).removeView(own);
            }
        }
    }

    // Lo de Android 12+ (API 31) aparte, para que Android 8-11 no lo cargue nunca.
    @TargetApi(31)
    private static final class Api31 {
        private Api31() {
        }

        static void keepSystemSplash(Activity activity) {
            try {
                activity.getSplashScreen().setOnExitAnimationListener(AfSplashHold::onSystemSplash);
            } catch (RuntimeException e) {
                // sin la pantalla del sistema entregada: queda el velo propio
            }
        }

        static boolean hasIcon(View splash) {
            return splash instanceof SplashScreenView && ((SplashScreenView) splash).getIconView() != null;
        }

        static void remove(View splash) {
            try {
                if (splash instanceof SplashScreenView) {
                    ((SplashScreenView) splash).remove();
                }
            } catch (RuntimeException e) {
                // la actividad ya se fue: la vista se va con su ventana
            }
        }
    }
}
