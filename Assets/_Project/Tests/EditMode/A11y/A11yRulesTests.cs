#nullable enable
using System.Collections.Generic;
using AF.Platform;
using AF.RemoteConfig;
using AF.RemoteConfig.Binding;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Reglas de accesibilidad sin motor del bloque B9 (<see cref="A11yRules"/>, <see cref="A11yConfig"/> y su lector de Remote
    /// Config): la escala de texto del sistema (02 §12: hasta el 130 %), «Reducir movimiento» (07 §7.8; 08 §9
    /// <c>ui_reduce_motion_default</c> y <c>anim_reduce_motion_factor</c>), «Alto contraste» al 35 % (07 §3, regla 5; D-033,
    /// Q-N4 (a)) y las cifras de C9 (02 §3.1).
    /// </summary>
    public sealed class A11yRulesTests
    {
        [TestCase(1f, 1f)]
        [TestCase(1.15f, 1.15f)]
        [TestCase(1.3f, 1.3f)]
        [TestCase(1.5f, 1.3f)]
        [TestCase(2f, 1.3f)]
        [TestCase(0.85f, 1f)]
        [TestCase(float.NaN, 1f)]
        [TestCase(float.PositiveInfinity, 1f)]
        public void TextScale_FollowsTheSystem_UpTo130Percent(float system, float expected)
        {
            Assert.That(A11yRules.TextScaleFor(system), Is.EqualTo(expected).Within(1e-6f), "02 §12: hasta el 130 %, por encima el tope");
        }

        [Test]
        public void ReduceMotion_ThePlayersChoiceWins()
        {
            Assert.That(A11yRules.ReduceMotion(true, false, 1f), Is.True);
            Assert.That(A11yRules.ReduceMotion(false, true, 0f), Is.False, "elegido «no» en Ajustes: ni Remote Config ni el sistema lo encienden");
        }

        [Test]
        public void ReduceMotion_Auto_FollowsRemoteConfig_OrTheSystemWithoutAnimations()
        {
            Assert.That(A11yRules.ReduceMotion(null, false, 1f), Is.False, "08 §9: ui_reduce_motion_default = false");
            Assert.That(A11yRules.ReduceMotion(null, true, 1f), Is.True);
            Assert.That(A11yRules.ReduceMotion(null, false, 0f), Is.True, "[P] Q-B9-3: «Quitar animaciones» del teléfono");
            Assert.That(A11yRules.ReduceMotion(null, false, 0.5f), Is.False, "animaciones a la mitad: no es quitarlas");
            Assert.That(A11yRules.ReduceMotion(null, false, float.NaN), Is.False, "sin saberlo, como el default");
        }

        [Test]
        public void MotionFactor_IsTheRemoteFactorOnlyWithReduceMotion()
        {
            Assert.That(A11yRules.MotionFactor(true, 0.5), Is.EqualTo(0.5f));
            Assert.That(A11yRules.MotionFactor(false, 0.5), Is.EqualTo(1f));
        }

        [Test]
        public void HighContrast_PutsThePatternAt35Percent_OtherwiseTheLevels()
        {
            Assert.That(A11yRules.HighContrastPatternOpacity, Is.EqualTo(0.35f), "07 §3, regla 5; D-033, Q-N4 (a)");
            Assert.That(A11yRules.PatternOpacity(true, 0.15f), Is.EqualTo(0.35f));
            Assert.That(A11yRules.PatternOpacity(true, 0.20f), Is.EqualTo(0.35f));
            Assert.That(A11yRules.PatternOpacity(false, 0.15f), Is.EqualTo(0.15f));
            Assert.That(A11yRules.PatternOpacity(false, 0.20f), Is.EqualTo(0.20f));
            Assert.That(A11yRules.HighContrastPatternOpacity, Is.LessThanOrEqualTo(0.35f), "08 §9: ui_pattern_opacity, rango 0,15-0,35");
        }

        [Test]
        public void Hold_Is400ms_AndTheSlopIsTheDragThreshold()
        {
            Assert.That(A11yRules.HoldMs, Is.EqualTo(400), "02 §3.1, C9");
            Assert.That(A11yRules.HoldSlopDp, Is.EqualTo(24f), "[P] Q-B9-4: el umbral de C10 (02 §3.1)");
        }

        [Test]
        public void A11yConfigDefaults_AreThoseOf08Section9()
        {
            RemoteConfigDefaults table = RemoteConfigDefaults.Compiled;
            var rc = new RemoteConfigService();
            RcSnapshot snapshot = rc.Freeze();
            A11yConfig defaults = A11yConfig.Defaults;
            Assert.That(table.Find("ui_reduce_motion_default"), Is.Not.Null);
            Assert.That(table.Find("anim_reduce_motion_factor"), Is.Not.Null);
            Assert.That(defaults.UiReduceMotionDefault, Is.EqualTo(snapshot.Get<bool>("ui_reduce_motion_default")), "08 §9: false");
            Assert.That(defaults.AnimReduceMotionFactor, Is.EqualTo(snapshot.Get<double>("anim_reduce_motion_factor")).Within(1e-9), "08 §9: 0,5");
            Assert.That(A11yConfigBinder.Keys, Is.EqualTo(new[] { "ui_reduce_motion_default", "anim_reduce_motion_factor" }));
        }

        [Test]
        public void Binder_WithTheCompiledDefaults_IsTheDefaults()
        {
            var errors = new List<string>();
            A11yConfig c = A11yConfigBinder.Bind(new RemoteConfigService().Freeze(), errors);
            Assert.That(errors, Is.Empty);
            Assert.That(c.UiReduceMotionDefault, Is.False);
            Assert.That(c.AnimReduceMotionFactor, Is.EqualTo(0.5).Within(1e-9));
        }

        [Test]
        public void Binder_ReadsTheRemoteValues()
        {
            A11yConfig c = A11yConfigBinder.Bind(Snapshot("ui_reduce_motion_default", "true", "anim_reduce_motion_factor", "0.3"));
            Assert.That(c.UiReduceMotionDefault, Is.True);
            Assert.That(c.AnimReduceMotionFactor, Is.EqualTo(0.3).Within(1e-9));
        }

        [Test]
        public void Binder_AFactorOutsideTheRange_FallsBackToTheDefaults()
        {
            var errors = new List<string>();
            A11yConfig c = A11yConfigBinder.Bind(Snapshot("ui_reduce_motion_default", "true", "anim_reduce_motion_factor", "0.9"), errors);
            Assert.That(c.AnimReduceMotionFactor, Is.EqualTo(0.5).Within(1e-9), "08 §9: rango 0,3-0,7 (Q-B7-7: la clase entera a Defaults)");
            Assert.That(c.UiReduceMotionDefault, Is.False);
            Assert.That(errors, Has.Count.EqualTo(1));
        }

        [Test]
        public void Binder_AValueThatIsNotABool_KeepsThatDefault()
        {
            var errors = new List<string>();
            A11yConfig c = A11yConfigBinder.Bind(Snapshot("ui_reduce_motion_default", "\"si\""), errors);
            Assert.That(c.UiReduceMotionDefault, Is.False, "un valor que no es bool no enciende nada: se queda el default de 08 §9");
        }

        [Test]
        public void FakeSystemScale_IsTheEditorPhone()
        {
            var fake = new FakeSystemScale();
            Assert.That(fake.FontScale, Is.EqualTo(1f));
            Assert.That(fake.AnimatorDurationScale, Is.EqualTo(1f));
            Assert.That(AF.UI.AndroidSystemScale.ForPlatform(), Is.InstanceOf<FakeSystemScale>(), "en el Editor, la implementación falsa");
        }

        private static RcSnapshot Snapshot(params string[] kv)
        {
            var d = new Dictionary<string, string>(System.StringComparer.Ordinal);
            for (int i = 0; i < kv.Length; i += 2)
            {
                d[kv[i]] = kv[i + 1];
            }

            var rc = new RemoteConfigService();
            rc.Activate(d);
            return rc.Freeze();
        }
    }
}
