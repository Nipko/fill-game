#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AF.EditorTools;
using AF.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Vertical solo en los teléfonos (añadido de D-059 del 2026-11-06, Q-BB-1 (b), decidida por el dueño; 02 §5, «Orientación»):
    /// la decisión teléfono/tableta de <see cref="OrientationPolicy"/>, lo que pide al arrancar y al cambiar la pantalla, el plugin
    /// <c>AfSplashHold</c> que lo pide antes del primer fotograma, y el manifiesto sin restricción (<see cref="ProjectSetup"/>).
    /// </summary>
    public sealed class OrientationPolicyTests
    {
        private static string SplashJava => File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Plugins", "Android", "AfSplashHold.androidlib",
            "src", "main", "java", "com", "amazonfill", "splash", "AfSplashHold.java"));

        // Lo último que pidió la política (como UnityScreenTarget): null al empezar, aunque el dispositivo esté en vertical.
        private sealed class FakeTarget : OrientationPolicy.ITarget
        {
            public ScreenOrientation? Requested { get; private set; }

            public List<ScreenOrientation> Requests { get; } = new List<ScreenOrientation>();

            public void Request(ScreenOrientation orientation)
            {
                Requested = orientation;
                Requests.Add(orientation);
            }
        }

        // Lado menor en dp de dispositivos reales: teléfonos (de 320 a 480), un plegable cerrado y abierto, tabletas y un Chromebook.
        [TestCase(320, ScreenOrientation.Portrait, TestName = "Teléfono pequeño (320 dp): vertical")]
        [TestCase(360, ScreenOrientation.Portrait, TestName = "Teléfono (360 dp): vertical")]
        [TestCase(411, ScreenOrientation.Portrait, TestName = "Teléfono (411 dp): vertical")]
        [TestCase(480, ScreenOrientation.Portrait, TestName = "Teléfono grande o plegable cerrado (480 dp): vertical")]
        [TestCase(599, ScreenOrientation.Portrait, TestName = "Justo por debajo de 600 dp: vertical")]
        [TestCase(600, ScreenOrientation.AutoRotation, TestName = "600 dp, tableta de 7 pulgadas: gira")]
        [TestCase(673, ScreenOrientation.AutoRotation, TestName = "Plegable abierto (673 dp): gira")]
        [TestCase(800, ScreenOrientation.AutoRotation, TestName = "Tableta de 10 pulgadas (800 dp): gira")]
        [TestCase(1080, ScreenOrientation.AutoRotation, TestName = "Chromebook (1080 dp): gira")]
        public void Decide_PhonesArePortrait_TheRestRotates(int smallestWidthDp, ScreenOrientation expected)
        {
            Assert.That(OrientationPolicy.Decide(smallestWidthDp), Is.EqualTo(expected));
            Assert.That(OrientationPolicy.IsPhone(smallestWidthDp), Is.EqualTo(expected == ScreenOrientation.Portrait));
        }

        [Test]
        public void Decide_WithoutAReading_TouchesNothing()
        {
            Assert.That(OrientationPolicy.Decide(0), Is.Null);
            Assert.That(OrientationPolicy.Decide(-1), Is.Null);
            var target = new FakeTarget();
            Assert.That(OrientationPolicy.Apply(0, target), Is.Null);
            Assert.That(target.Requests, Is.Empty, "sin lectura no se toca: manda lo que pidió el plugin");
        }

        [Test]
        public void AtLaunch_ItAlwaysAsks_PortraitOnAPhone_RotationOnATablet()
        {
            // El reproductor arranca con la autorrotación de los Player Settings (el manifiesto no restringe) y, con autorrotación,
            // Screen.orientation da la orientación física: un teléfono que ya está en vertical también tiene que pedir vertical, o se
            // quedaría girando (revisión del carril). OnLaunch pide siempre, con o sin el teléfono tumbado.
            var phone = new FakeTarget();
            Assert.That(OrientationPolicy.Apply(393, phone), Is.EqualTo(ScreenOrientation.Portrait));
            Assert.That(phone.Requests, Is.EqualTo(new[] { ScreenOrientation.Portrait }));
            Assert.That(OrientationPolicy.Apply(393, phone), Is.Null, "el vigía, con la misma decisión, no vuelve a pedir");

            var tablet = new FakeTarget();
            Assert.That(OrientationPolicy.Apply(800, tablet), Is.EqualTo(ScreenOrientation.AutoRotation), "también la tableta pide la suya");
            Assert.That(tablet.Requests, Is.EqualTo(new[] { ScreenOrientation.AutoRotation }));
        }

        [Test]
        public void AFoldable_OpeningAndClosing_SwitchesBetweenRotationAndPortrait()
        {
            var screen = new FakeTarget();
            Assert.That(OrientationPolicy.Apply(411, screen), Is.EqualTo(ScreenOrientation.Portrait), "cerrado al arrancar: vertical");
            Assert.That(OrientationPolicy.Apply(673, screen), Is.EqualTo(ScreenOrientation.AutoRotation), "al abrirlo, gira");
            Assert.That(OrientationPolicy.Apply(673, screen), Is.Null, "girar abierto no pide nada más");
            // Cerrado estando en vertical: la pantalla física ya está en vertical, pero lo pedido era autorrotación, así que pide vertical.
            Assert.That(OrientationPolicy.Apply(411, screen), Is.EqualTo(ScreenOrientation.Portrait), "al cerrarlo, otra vez vertical");
            Assert.That(screen.Requests, Is.EqualTo(new[] { ScreenOrientation.Portrait, ScreenOrientation.AutoRotation, ScreenOrientation.Portrait }));
        }

        [Test]
        public void TheLaunchHooks_RunBeforeTheSplashScreen_AndAfterTheFirstScene()
        {
            MethodInfo? launch = typeof(OrientationPolicy).GetMethod("OnLaunch", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(launch, Is.Not.Null);
            var first = launch!.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            Assert.That(first, Is.Not.Null);
            Assert.That(first!.loadType, Is.EqualTo(RuntimeInitializeLoadType.BeforeSplashScreen), "lo antes que corre C#");

            MethodInfo? watcher = typeof(OrientationPolicy).GetMethod("StartWatcher", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(watcher, Is.Not.Null);
            Assert.That(watcher!.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>()!.loadType, Is.EqualTo(RuntimeInitializeLoadType.AfterSceneLoad));

            Assert.That(OrientationPolicy.ReadDeviceSmallestWidthDp(), Is.EqualTo(0), "en el Editor no se lee nada (y no se toca)");
        }

        [Test]
        public void ThePlugin_LocksPhonesInOnCreate_BeforeTheFirstFrame_WithTheSameThreshold()
        {
            string java = SplashJava;
            StringAssert.Contains("public static final int TABLET_MIN_SMALLEST_WIDTH_DP = " + OrientationPolicy.TabletMinSmallestWidthDp + ";", java,
                "la misma frontera en Java y en C#");
            StringAssert.Contains("activity.setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_PORTRAIT);", java);
            StringAssert.Contains("getMaximumWindowMetrics()", java, "la pantalla del dispositivo, no la ventana de la pantalla dividida");
            StringAssert.Contains("public static int deviceSmallestWidthDp(Activity activity)", java, "lo lee C# por JNI");

            // En onActivityCreated (dentro del onCreate de la actividad, antes de su primer fotograma) y antes de la guarda del
            // arranque en frío: también al recrear la actividad.
            int created = java.IndexOf("public void onActivityCreated(Activity activity, Bundle savedInstanceState) {", System.StringComparison.Ordinal);
            int locks = java.IndexOf("lockPortraitOnPhones(activity);", created, System.StringComparison.Ordinal);
            int seen = java.IndexOf("if (seen) {", created, System.StringComparison.Ordinal);
            Assert.That(created, Is.GreaterThan(0));
            Assert.That(locks, Is.GreaterThan(created), "lo pide en onActivityCreated");
            Assert.That(locks, Is.LessThan(seen), "antes de la guarda de la primera actividad");
        }

        [Test]
        public void ProjectSetup_TheManifestDoesNotRestrictTheOrientation()
        {
            Assert.That(ProjectSetup.Orientation, Is.EqualTo(UIOrientation.AutoRotation), "Q-BB-1 (b): sin screenOrientation restrictivo");
            Assert.That(ProjectSetup.AutorotateAll, Is.True, "las cuatro orientaciones");
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.AutoRotation), "aplicado con -Method Setup");
            Assert.That(PlayerSettings.allowedAutorotateToPortrait && PlayerSettings.allowedAutorotateToPortraitUpsideDown
                && PlayerSettings.allowedAutorotateToLandscapeLeft && PlayerSettings.allowedAutorotateToLandscapeRight, Is.True);
        }
    }
}
