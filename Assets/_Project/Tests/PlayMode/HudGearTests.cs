#nullable enable
using System.Collections;
using AF.Audio;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El botón de pausa del nivel dibuja el engranaje de Ajustes del inicio (observación de la prueba de la 10001, añadido de D-063
    /// del 2026-10-31): el mismo sprite que <c>LobbyScreen</c>, en el disco de madera de arriba a la izquierda, con su área de
    /// ≥ 48 dp; tocarlo abre la misma pausa (con Música y Sonido) y Atrás sigue abriéndola y cerrándola (D-063).
    /// </summary>
    public sealed class HudGearTests
    {
        [SetUp]
        public void SetUp()
        {
            AudioCues.Override = new FakeAudioService();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            SnapshotKit.Release();
            AudioCues.Override = null;
            AudioCues.Install(null);
        }

        [UnityTest]
        public IEnumerator PauseButton_DrawsTheLobbyGear_AndStillOpensThePause()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return Canvas16x9LayoutTests.LoadPlaying(4);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));

            Image icon = PauseIcon(view);
            Assert.That(icon.sprite, Is.SameAs(boot.Art!.IconGear), "el engranaje de Ajustes del inicio, no las dos barras");
            Assert.That(icon.sprite.name, Is.EqualTo("icon_gear"));
            Assert.That(icon.gameObject.activeInHierarchy, Is.True);
            Assert.That(icon.rectTransform.rect.width, Is.EqualTo(26f).Within(0.01f), "26 dp en el disco, como en el inicio");
            Assert.That(icon.rectTransform.rect.height, Is.EqualTo(26f).Within(0.01f));

            Canvas.ForceUpdateCanvases();
            Vector2 area = SizeIn((RectTransform)view.PauseButton.transform, (RectTransform)view.Canvas.transform);
            Assert.That(area.x >= 47.5f && area.y >= 47.5f, Is.True, "área táctil ≥ 48 dp (07 §7.8): " + area);

            // La acción no cambia: abre la pausa (con Música y Sonido) y «Continuar» vuelve al juego.
            Assert.That(view.Pause.Visible, Is.False);
            view.PauseButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.True, "el engranaje abre la pausa");
            Assert.That(view.Pause.Audio.Visible, Is.True, "con Música y Sonido (D-046, punto 2)");
            view.Pause.ResumeButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.False);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));

            // Atrás de Android (D-063), igual que antes.
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(view.Pause.Visible, Is.True, "Atrás abre la pausa, como su botón");
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(view.Pause.Visible, Is.False, "y en la pausa vuelve al juego");
        }

        /// <summary>
        /// Captura de revisión del HUD con el engranaje (<c>Logs/snapshots/es-419/hud_gear_01_level.png</c>; el disco de cerca en
        /// <c>hud_gear_02_top.png</c>). Necesita gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
        /// AF.Tests.PlayMode.HudGearTests</c> sin <c>-nographics</c>.
        /// </summary>
        [UnityTest]
        [Explicit("Captura visual manual; necesita dispositivo gráfico.")]
        public IEnumerator CaptureHudGear_Es419()
        {
            string dir = SnapshotKit.Prepare();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return Canvas16x9LayoutTests.LoadPlaying(4);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "hud_gear_01_level");
            SnapshotKit.SaveTop(dir, "hud_gear_02_top", 420);
        }

        private static Image PauseIcon(GameScreen view)
        {
            Image[] images = view.PauseButton.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].name == "Icon")
                {
                    return images[i];
                }
            }

            Assert.Fail("el botón de pausa no tiene su icono");
            return null!;
        }

        // Tamaño de rt en las unidades de root (dp del lienzo).
        private static Vector2 SizeIn(RectTransform rt, RectTransform root)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 a = root.InverseTransformPoint(corners[0]);
            Vector3 c = root.InverseTransformPoint(corners[2]);
            return new Vector2(Mathf.Abs(c.x - a.x), Mathf.Abs(c.y - a.y));
        }
    }
}
