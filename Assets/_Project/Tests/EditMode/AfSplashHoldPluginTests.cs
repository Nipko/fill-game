#nullable enable
using System;
using System.IO;
using System.Text.RegularExpressions;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La pantalla de arranque del sistema hasta la intro (añadido de D-051 del 2026-10-28): el plugin propio
    /// <c>AfSplashHold.androidlib</c> es solo Java con API de la plataforma (sin androidx, dependencias, permisos, red, disco ni
    /// registros), su proveedor no se exporta, tiene un tope de 3 s y el contrato de nombres con <see cref="AndroidOsSplash"/> cuadra;
    /// desde la observación 1 de la jornada 6, es un velo que no bloquea (sin <c>OnPreDrawListener</c> ni <c>setRunWithoutFocus</c>),
    /// no enfocable ni para TalkBack; y cuándo lo suelta el juego (<see cref="AndroidOsSplash.SignalFor"/>).
    /// </summary>
    public sealed class AfSplashHoldPluginTests
    {
        private static string PluginDir => Path.Combine(Application.dataPath, "_Project", "Plugins", "Android", "AfSplashHold.androidlib");

        private static string JavaDir => Path.Combine(PluginDir, "src", "main", "java", "com", "amazonfill", "splash");

        private static string Hold => File.ReadAllText(Path.Combine(JavaDir, "AfSplashHold.java"));

        [Test]
        public void Plugin_IsJavaOnly_WithPlatformApiOnly_AndNoDataOrNetwork()
        {
            Assert.That(Directory.GetFiles(PluginDir, "*.kt", SearchOption.AllDirectories), Is.Empty, "sin Kotlin");
            string[] files = Directory.GetFiles(PluginDir, "*.java", SearchOption.AllDirectories);
            Assert.That(files, Has.Length.EqualTo(2), "AfSplashHold y su proveedor");
            string[] banned =
            {
                "java.net", "javax.net", "okhttp", "HttpURLConnection", "Socket", "http:", "https:", "WebView", "android.util.Log", "Log.",
                "System.out", "printStackTrace", "SharedPreferences", "FileOutputStream", "getAdvertisingId", "Settings.Secure", "androidx",
            };
            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                foreach (string word in banned)
                {
                    Assert.That(text.Contains(word), Is.False, Path.GetFileName(file) + ": " + word);
                }

                foreach (Match m in Regex.Matches(text, @"^import\s+([\w\.]+);", RegexOptions.Multiline))
                {
                    string import = m.Groups[1].Value;
                    bool allowed = import.StartsWith("android.", StringComparison.Ordinal);
                    Assert.That(allowed, Is.True, Path.GetFileName(file) + " importa " + import);
                }
            }

            string gradle = File.ReadAllText(Path.Combine(PluginDir, "build.gradle"));
            Assert.That(gradle, Does.Contain("namespace 'com.amazonfill.splash'"), "AGP 9: namespace en cada .androidlib");
            Assert.That(gradle, Does.Contain("minSdk 26"));
            Assert.That(gradle, Does.Not.Contain("dependencies"), "regla 4: nada de dependencias nuevas");
        }

        [Test]
        public void Manifest_HasOnlyTheUnexportedProvider_AndNoPermissions()
        {
            string path = Path.Combine(PluginDir, "src", "main", "AndroidManifest.xml");
            Assert.DoesNotThrow(() => new System.Xml.XmlDocument().Load(path), "XML válido (un «--» en un comentario rompe el build)");
            string manifest = File.ReadAllText(path);
            Assert.That(manifest, Does.Not.Contain("uses-permission"));
            Assert.That(manifest, Does.Not.Contain("<activity"), "no sustituye a la actividad de Unity");
            StringAssert.Contains("android:name=\"com.amazonfill.splash.AfSplashHoldProvider\"", manifest);
            StringAssert.Contains("android:authorities=\"${applicationId}.afsplashhold\"", manifest);
            StringAssert.Contains("android:exported=\"false\"", manifest);
        }

        [Test]
        public void TheBridge_MatchesTheCSharpSide_AndHasTheCap()
        {
            string hold = Hold;
            Assert.That(AndroidOsSplash.BridgeClass, Is.EqualTo("com.amazonfill.splash.AfSplashHold"));
            StringAssert.Contains("package com.amazonfill.splash;", hold);
            StringAssert.Contains("public static final long CAP_MS = " + AndroidOsSplash.CapMs + "L;", hold);
            Assert.That(AndroidOsSplash.CapMs, Is.EqualTo(3000), "tope de seguridad: nunca se queda colgada");
            foreach (string method in new[] { "release(String by)", "String result()", "long heldMs()", "long procReleaseMs()", "long focusAfterMs()", "String veil()" })
            {
                StringAssert.Contains("public static " + (method.StartsWith("release", StringComparison.Ordinal) ? "void " : string.Empty) + method, hold, method);
            }

            foreach (string result in new[] { "none", "pending", "signal", "cap", "gone", "own", "system" })
            {
                StringAssert.Contains("= \"" + result + "\";", hold, result);
            }

            StringAssert.Contains("MAIN.postDelayed(CAP, CAP_MS);", hold);
            Assert.That(Regex.IsMatch(hold, @"public static long focusAfterMs\(\)\s*\{\s*return -1L;"), Is.True, "el velo no toca el foco: la sonda queda en -1");
        }

        [Test]
        public void TheIconBox_IsMeasuredFromTheSystemIconView_OrTheOwnVeil_AndExposedToCSharp()
        {
            // Jornada 6, observación 11: el primer fotograma de la intro usa los píxeles del icono que se vio, no los de la guía.
            string hold = Hold.Replace("\r\n", "\n");
            StringAssert.Contains("public static int[] iconBox() {\n        return box.clone();", hold, "JNI: AndroidOsSplash.ReadIcon");
            StringAssert.Contains("public static final int BOX_LENGTH = 8;", hold);
            StringAssert.Contains("public static final int OWN_MASK_DP = " + (int)BrandIntroMorph.IconMaskDp + ";", hold, "el círculo de af_launch_window, el de la guía en C#");

            // El del sistema: la vista del icono entregada, en px de pantalla con su escala, y el marco de la DecorView.
            StringAssert.Contains("return ((SplashScreenView) splash).getIconView();", hold);
            Assert.That(Regex.IsMatch(hold, @"veil = VEIL_SYSTEM;\s*View icon = Api31\.iconView\(splash\);\s*systemIcon = icon;\s*icon\.addOnLayoutChangeListener\(ICON_LAYOUT\);\s*measureSystem\(icon\);"), Is.True,
                "se mide al entregarla y cada vez que se coloca");
            StringAssert.Contains("icon.getLocationOnScreen(at);", hold);
            StringAssert.Contains("Math.round(icon.getWidth() * Math.abs(icon.getScaleX()))", hold);
            StringAssert.Contains("Math.round(icon.getHeight() * Math.abs(icon.getScaleY()))", hold);
            StringAssert.Contains("box = new int[] {at[0], at[1], w, h, ref[0], ref[1], root.getWidth(), root.getHeight()};", hold);

            // El propio: la caja conocida de af_launch_window, centrada en la DecorView.
            StringAssert.Contains("view.addOnLayoutChangeListener(OWN_LAYOUT);", hold);
            StringAssert.Contains("int d = Math.round(OWN_MASK_DP * own.getResources().getDisplayMetrics().density);", hold);
            StringAssert.Contains("box = new int[] {cx - (d / 2), cy - (d / 2), d, d, ref[0], ref[1], root.getWidth(), root.getHeight()};", hold);
            StringAssert.Contains("removeOnLayoutChangeListener(ICON_LAYOUT)", hold, "al soltar, sin oyentes colgados");
            StringAssert.Contains("removeOnLayoutChangeListener(OWN_LAYOUT)", hold);

            // El C#: los 8 enteros en el mismo orden.
            SplashIconBox b = SplashIconBox.FromArray(new[] { 297, 927, 486, 484, 0, 0, 1080, 2340 });
            Assert.That(b.Measured, Is.True);
            Assert.That(b.Diameter, Is.EqualTo(484f), "el lado menor: el círculo inscrito");
            Assert.That(b.Center, Is.EqualTo(new Vector2(540f, 1169f)));
            Assert.That(SplashIconBox.FromArray(new int[8]).Measured, Is.False, "todo 0: sin medida");
            Assert.That(SplashIconBox.FromArray(null).Measured, Is.False);
            Assert.That(SplashIconBox.FromArray(new[] { 1, 2, 3 }).Measured, Is.False);
            Assert.That(AndroidOsSplash.ReadIcon().Measured, Is.False, "en el Editor no hay medida: la guía de 240/160 dp");
        }

        [Test]
        public void TheCap_IsWellUnderFiveSeconds()
        {
            // El umbral de arranque lento de Android vitals es 5 s: el velo nunca puede alargar el arranque hasta ahí.
            Assert.That(AndroidOsSplash.CapMs, Is.LessThan(5000));
            Assert.That(AndroidOsSplash.CapMs, Is.GreaterThan(0));
        }

        [Test]
        public void TheVeil_DoesNotBlockTheWindow_NorTouchTheFocus()
        {
            // Jornada 6, observación 1: aplazar el primer fotograma (OnPreDrawListener que devuelve false) y hacer correr a Unity sin
            // foco se esperaba a sí mismo hasta el tope. El velo no aplaza nada ni toca el foco ni usa reflexión.
            string hold = Hold;
            foreach (string word in new[] { "OnPreDrawListener", "addOnPreDrawListener", "setRunWithoutFocus", "getUnityPlayerConnection", "java.lang.reflect", "OnWindowFocusChangeListener", "getMethod(" })
            {
                Assert.That(hold.Contains(word), Is.False, word);
            }

            // Una vista encima de la DecorView con el fondo de ventana del tema (af_launch_window), puesta antes del primer fotograma.
            StringAssert.Contains("android.R.attr.windowBackground", hold);
            StringAssert.Contains("view.setBackground(background);", hold);
            StringAssert.Contains("((ViewGroup) decor).addView(view, new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT));", hold);
            StringAssert.Contains("public void onActivityStarted(Activity activity) {", hold);
            StringAssert.Contains("removeView(own)", hold, "se quita al soltar");

            // Android 12+: la propia pantalla del sistema, entregada a la actividad, se queda encima hasta soltar (el icono no salta).
            StringAssert.Contains("Build.VERSION.SDK_INT >= Build.VERSION_CODES.S", hold);
            StringAssert.Contains("getSplashScreen().setOnExitAnimationListener(AfSplashHold::onSystemSplash)", hold);
            StringAssert.Contains("((SplashScreenView) splash).remove();", hold);
            StringAssert.Contains("@TargetApi(31)\n    private static final class Api31", hold.Replace("\r\n", "\n"));

            // Sin icono (Android 13+ la entrega de color liso fuera del lanzador, p. ej. desde una notificación) no se queda: se quita
            // en el acto y manda el velo propio (os_splash_veil = own).
            StringAssert.Contains("if (RESULT_PENDING.equals(result) && candidate != null && Api31.hasIcon(splash)) {", hold);
            StringAssert.Contains("return splash instanceof SplashScreenView && ((SplashScreenView) splash).getIconView() != null;", hold);
            Assert.That(Regex.IsMatch(hold, @"Api31\.hasIcon\(splash\)\) \{\s*makeInert\(splash\);\s*systemVeil = splash;\s*veil = VEIL_SYSTEM;[^}]*return;\s*\}\s*Api31\.remove\(splash\);"), Is.True, "solo con icono se queda; si no, se quita ya");

            // Ni foco, ni TalkBack, ni toques: los dos velos pasan por makeInert.
            foreach (string inert in new[] { "setFocusable(false)", "setFocusableInTouchMode(false)", "setClickable(false)", "setLongClickable(false)", "IMPORTANT_FOR_ACCESSIBILITY_NO_HIDE_DESCENDANTS" })
            {
                StringAssert.Contains(inert, hold, inert);
            }

            Assert.That(Regex.Matches(hold, @"makeInert\((view|splash)\);").Count, Is.EqualTo(2), "el velo propio y el del sistema");

            // Solo la actividad de Unity y una vez por proceso; sin fundido (nada se mueve con Reducir movimiento).
            StringAssert.Contains("\"com.unity3d.player.UnityPlayerGameActivity\"", hold);
            StringAssert.Contains("if (seen) {", hold);
            foreach (string anim in new[] { "animate(", "Animator", "setAlpha(" })
            {
                Assert.That(hold.Contains(anim), Is.False, anim);
            }
        }

        [Test]
        public void TheGame_ReleasesItWhenTheIntroIsOnScreen_OrWithoutIntro_OrWithoutSplash()
        {
            Assert.That(AndroidOsSplash.SignalFor(true, true, false, 50), Is.Null, "con la intro: espera a su primer fotograma en la pantalla");
            Assert.That(AndroidOsSplash.SignalFor(true, true, true, 3), Is.EqualTo(AndroidOsSplash.ByIntro), "también quieta (Reducir movimiento) o con caché");
            Assert.That(AndroidOsSplash.SignalFor(true, false, false, 1), Is.Null, "sin intro: el primer fotograma aún no se pintó");
            Assert.That(AndroidOsSplash.SignalFor(true, false, false, 2), Is.EqualTo(AndroidOsSplash.BySplash));
            Assert.That(AndroidOsSplash.SignalFor(false, false, false, 1), Is.EqualTo(AndroidOsSplash.ByNoSplash), "una recarga de la escena no espera");
        }

        [Test]
        public void InTheEditor_ReleaseDoesNothing_AndReadSaysNoHold()
        {
            Assert.DoesNotThrow(() => AndroidOsSplash.Release(AndroidOsSplash.ByIntro));
            OsSplashInfo info = AndroidOsSplash.Read();
            Assert.That(info.Result, Is.EqualTo("none"));
            Assert.That(info.HeldMs, Is.EqualTo(-1L));
            Assert.That(info.ProcReleaseMs, Is.EqualTo(-1L));
            Assert.That(info.FocusAfterMs, Is.EqualTo(-1L));
            Assert.That(info.Veil, Is.EqualTo("none"));
        }
    }
}
