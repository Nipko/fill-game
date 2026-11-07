#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Mechanics;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La curva 2 de la 1.1 en el arranque real (D-068, fase F5; 08 §4.3, §4.4 y §9): la escena que genera
    /// <c>GameSceneBuilder.CreateGameScene</c> enlaza los packs <c>curva_07..09_v1</c> y su reserva, y con
    /// <c>lvl_curve2_enabled</c> el 303 sale del pack de la curva 2 con el estreno del Aguapé y el 363 con el de la Lancha. Con la
    /// clave apagada (el segmento A del A/B, P10 A) salen los tableros de la 1.0 (<c>mundo_07_v1</c>), sin las mecánicas de la 1.1.
    /// Sin paquetes del test: los de la escena.
    /// </summary>
    public sealed class CurveBootTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator TheScene_LinksTheCurvePacks_AndServes303And363FromTheCurve2()
        {
            LaunchOptions.Curve2EnabledOverride = true;
            yield return Load(303);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(Names(boot.CurvePackAssets), Is.EqualTo(new[] { "curva_07_v1", "curva_08_v1", "curva_09_v1" }),
                "la escena lleva los packs de la curva 2 de 301-700");
            Assert.That(Names(boot.CurveReservePackAssets), Is.EqualTo(new[] { "reserva_curva_07_v1", "reserva_curva_08_v1", "reserva_curva_09_v1" }),
                "y su reserva");
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Level.Id, Is.EqualTo("m07_v1_n0303_c2"));
            Assert.That(flow.Level.Replaces, Is.EqualTo("m07_v1_n0303"));
            Assert.That(flow.Level.Curve, Is.EqualTo(LevelDefinition.Curve2));
            Assert.That(flow.Level.IntroMechanic, Is.EqualTo("aguape"), "el estreno del Aguapé");
            Assert.That(Count(flow.Level, MechanicType.Aguape), Is.GreaterThanOrEqualTo(2), "un grupo del Aguapé (2-3 cuias)");
            Assert.That(flow.Catalog.Count, Is.EqualTo(700), "la curva 2 no añade niveles de campaña");

            LaunchOptions.Reset();
            LaunchOptions.Curve2EnabledOverride = true;
            yield return Load(363);
            flow = GameBootstrap.Current!.Flow!;
            Assert.That(flow.Level.Id, Is.EqualTo("m07_v1_n0363_c2"));
            Assert.That(flow.Level.Curve, Is.EqualTo(LevelDefinition.Curve2));
            Assert.That(flow.Level.IntroMechanic, Is.EqualTo("lancha"), "el estreno de la Lancha");
            Assert.That(Count(flow.Level, MechanicType.Lancha), Is.EqualTo(1), "una lancha");
            Assert.That(flow.Level.CuiaCount, Is.EqualTo(10), "con K 6 en 10 cuias (D-068)");
        }

        [UnityTest]
        public IEnumerator WithTheKeyOff_303And363AreThe10Boards()
        {
            LaunchOptions.Curve2EnabledOverride = false;
            yield return Load(303);
            LevelFlow flow = GameBootstrap.Current!.Flow!;
            Assert.That(flow.Level.Id, Is.EqualTo("m07_v1_n0303"), "lvl_curve2_enabled apagada: la curva 1");
            Assert.That(flow.Level.Curve, Is.EqualTo(LevelDefinition.Curve1));
            Assert.That(Count(flow.Level, MechanicType.Aguape), Is.EqualTo(0));

            LaunchOptions.Reset();
            LaunchOptions.Curve2EnabledOverride = false;
            yield return Load(363);
            flow = GameBootstrap.Current!.Flow!;
            Assert.That(flow.Level.Id, Is.EqualTo("m07_v1_n0363"));
            Assert.That(flow.Level.Curve, Is.EqualTo(LevelDefinition.Curve1));
            Assert.That(Count(flow.Level, MechanicType.Lancha), Is.EqualTo(0));
        }

        // Un perfil nuevo (curve2_from_level = 1) con el nivel en curso.
        private static IEnumerator Load(int level)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
        }

        private static int Count(LevelDefinition level, MechanicType type)
        {
            int n = 0;
            for (int i = 0; i < level.Attrs.Count; i++)
            {
                n += level.Attrs[i].Type == type ? 1 : 0;
            }

            return n;
        }

        private static string[] Names(IReadOnlyList<UnityEngine.TextAsset> assets)
        {
            var names = new string[assets.Count];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = assets[i] != null ? assets[i].name : "(null)";
            }

            return names;
        }
    }
}
