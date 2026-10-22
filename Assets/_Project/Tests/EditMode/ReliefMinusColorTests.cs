#nullable enable
using System.Collections.Generic;
using AF.Levels;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// V3 (tarea B11; D-033, Q-N2 (a): «desde el 31 el generador produce siempre una variante <c>minus_color</c> para el alivio con
    /// Cuia extra»): con la Cuia extra aplicada, el tercer fallo sirve la variante de −1 color, nunca la de +1 vacío (04 §2.2,
    /// caso b; <c>FailResolve.ChooseRelief</c>), así que un nivel con solo <c>plus_empty</c> se quedaba sin alivio. Los paquetes
    /// publicados (1-700) llevan <c>minus_color</c> en todos los niveles desde el 31 salvo <see cref="NoMinusColor"/> (pregunta
    /// Q-B11-3), y el orden de <c>relief[]</c> sigue la regla de 03 §13.
    /// </summary>
    public sealed class ReliefMinusColorTests
    {
        /// <summary>
        /// Niveles sin ninguna eliminación de un color que alivie (03 §17 #8) ni con la segunda pasada del generador
        /// (<c>Relief.MinusColorSecondBudget</c>): el 680, hito de 14 cuias con K = 13 (D-027), con solo dos frutas que se pueden
        /// quitar y ninguna que baje el fallo del bot 0,20 (pregunta Q-B11-3).
        /// </summary>
        private static readonly int[] NoMinusColor = { 680 };

        [Test]
        public void From31_EveryLevelHasAMinusColorVariant_QN2a()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            var missing = new List<int>();
            for (int level = 31; level <= catalog.LastLevel; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                Assert.That(def.Relief.Count, Is.GreaterThan(0), "03 §13: relief no vacío desde el 31 (nivel " + level + ")");
                bool minus = false;
                for (int i = 0; i < def.Relief.Count; i++)
                {
                    minus |= def.Relief[i].Type == ReliefBoard.TypeMinusColor;
                }

                if (!minus)
                {
                    missing.Add(level);
                }
            }

            Assert.That(missing, Is.EqualTo(NoMinusColor), "niveles desde el 31 sin minus_color");
        }

        [Test]
        public void MinusColor_HasAnIntegerPar_08_4_1()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            for (int level = 31; level <= catalog.LastLevel; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                for (int i = 0; i < def.Relief.Count; i++)
                {
                    ReliefBoard r = def.Relief[i];
                    if (r.Type == ReliefBoard.TypeMinusColor)
                    {
                        Assert.That(r.Par.HasValue && r.Par.Value >= 1, Is.True, "08 §4.1: par entero ≥ 1 en minus_color (nivel " + level + ")");
                        Assert.That(r.CuiaCount, Is.EqualTo(def.CuiaCount), "−1 color: las mismas cuias (nivel " + level + ")");
                    }
                    else
                    {
                        Assert.That(r.CuiaCount, Is.EqualTo(def.CuiaCount + 1), "+1 vacío: una cuia más (nivel " + level + ")");
                    }
                }
            }
        }
    }
}
