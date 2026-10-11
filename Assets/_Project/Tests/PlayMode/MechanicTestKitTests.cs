#nullable enable
using System.Collections;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El banco de <see cref="MechanicTestKit.Rig"/> se va entero al desecharlo, en el mismo fotograma (T21): las pruebas sin
    /// asignaciones calientan en un banco y miden en otro nuevo sin dejar pasar un fotograma, así que un banco desechado que
    /// siguiera vivo hasta el final del fotograma (o que dejara piezas fuera de su raíz) haría que el banco medido llegara a más
    /// gráficos vivos que el de calentamiento, y las colecciones globales de UGUI podrían crecer en mitad de la medida.
    /// </summary>
    public sealed class MechanicTestKitTests
    {
        private static ProceduralArt? s_art;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        [UnityTest]
        public IEnumerator Rig_DisposeReleasesAtOnce()
        {
            yield return null;
            int before = LiveGraphics();
            int beforeObjects = LiveTransforms();

            // La cadena gota → sello → liana de MechanicStepsTests.NoAllocDuringSteps: overlays, efectos y el cometa de la
            // Liana en la capa delantera del tablero.
            var caps = new[] { 4, 4, 4, 4, 4, 4 };
            int[][] cuias = { new[] { 10, 10, 10 }, new[] { 1, 2, 1, 2 }, new[] { 2, 1 }, new[] { 1 }, new[] { 2 }, new int[0] };
            MechanicSpec[] attrs =
            {
                new MechanicSpec(MechanicType.Lluvia, key: 10, n: 1, k: 2), new MechanicSpec(MechanicType.Liana, key: 10),
                MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None,
            };
            BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs);
            Assert.That(built.Error, Is.Null);
            BoardState start = built.State!;
            var rig = new MechanicTestKit.Rig(Art, start);
            CuiaView view;
            try
            {
                BoardState counted = rig.Pour(start, 3, 2, out _);
                rig.RunUntilIdle(3000);
                rig.Pour(counted, 2, 5, out PourVisual chain);
                Assert.That(chain.Events.LianaReleasedMask, Is.EqualTo(1 << 1), "gota → sello → liana");
                rig.RunUntilIdle(4000);
                Assert.That(rig.Animator.IsIdle, Is.True);
                Assert.That(LiveGraphics(), Is.GreaterThan(before), "el banco tiene gráficos");
                Assert.That(LiveTransforms(), Is.GreaterThan(beforeObjects), "y objetos");
                view = rig.Views[0];
            }
            finally
            {
                rig.Dispose();
            }

            Assert.That(view == null, Is.True, "las vistas se destruyen al desechar el banco, no al final del fotograma");
            Assert.That(LiveGraphics(), Is.EqualTo(before), "no queda ningún gráfico del banco, tampoco fuera de su raíz");
            Assert.That(LiveTransforms(), Is.EqualTo(beforeObjects), "ni ningún objeto (anclas, capas y raíces sin gráfico incluidas)");
        }

        // Gráficos cargados, también los ocultos y los de objetos inactivos.
        private static int LiveGraphics()
        {
            return Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        }

        // Objetos cargados (un Transform por objeto), también los inactivos: cuenta las anclas, las capas y las raíces de los
        // overlays, que no tienen gráfico.
        private static int LiveTransforms()
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        }
    }
}
