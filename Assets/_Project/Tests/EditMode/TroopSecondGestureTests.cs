#nullable enable
using System.Collections.Generic;
using System.IO;
using AF.Levels;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Segundo gesto de los miquitos (F13 de la propuesta de fauna, decidida en D-029 para la 1.1; añadido de D-029 del
    /// 2026-11-08): cada especie de los mundos largos (6-11) tiene el suyo en el catálogo; sale desde la mitad de su primer mundo
    /// largo y en niveles alternos, de forma determinista; el lector rechaza el campo donde no toca; cada gesto cabe en su modelo
    /// de suelta y en el reposo de siempre; y sus poses son de animal (la cabeza nunca baja, D-002; nada baja de la mano de soltar,
    /// A7).
    /// </summary>
    public sealed class TroopSecondGestureTests
    {
        private static ProceduralArt? s_art;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        private static string CatalogJson => File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json"));

        private static FaunaCatalog Catalog => FaunaCatalogReader.Read(CatalogJson);

        [OneTimeTearDown]
        public void DisposeArt()
        {
            s_art?.Dispose();
            s_art = null;
        }

        [Test]
        public void MidLevels_AreTheMiddleOfEachLongWorld()
        {
            int[] expected = { 251, 361, 491, 631, 761, 871 };
            for (int w = FaunaGestureRule.FirstLongWorld; w <= FaunaGestureRule.LastLongWorld; w++)
            {
                int mid = FaunaGestureRule.MidLevel(w);
                Assert.That(mid, Is.EqualTo(expected[w - FaunaGestureRule.FirstLongWorld]), "mundo " + w);
                Assert.That(WorldTable.WorldOf(mid), Is.EqualTo(w), "la mitad está dentro del mundo " + w);
                Assert.That(mid - WorldTable.FirstLevel(w), Is.EqualTo(WorldTable.LastLevelOf(w) - mid + 1), "mitad exacta del mundo " + w);
            }

            Assert.That(FaunaGestureRule.IsLongWorld(5), Is.False);
            Assert.That(FaunaGestureRule.IsLongWorld(12), Is.False);
            Assert.That(FaunaGestureRule.FromLevel(new[] { 8, 9 }), Is.EqualTo(491), "la especie de 8-9 lo estrena en la mitad del 8");
            Assert.That(FaunaGestureRule.FromLevel(new[] { 5 }), Is.EqualTo(0));
        }

        [Test]
        public void EverySpeciesOfTheLongWorlds_HasItsOwnSecondGesture()
        {
            var expected = new Dictionary<string, (FaunaSecondGesture Gesture, int From)>
            {
                ["plecturocebus_discolor"] = (FaunaSecondGesture.Duet, 251),
                ["saguinus_imperator"] = (FaunaSecondGesture.Hops, 361),
                ["ateles_chamek"] = (FaunaSecondGesture.TailHang, 491),
                ["aotus_trivirgatus"] = (FaunaSecondGesture.Stretch, 761),
                ["pithecia_pithecia"] = (FaunaSecondGesture.Leap, 871),
            };
            var seen = new HashSet<FaunaSecondGesture>();
            foreach (FaunaSpecies sp in Catalog.Species)
            {
                bool longWorld = false;
                foreach (int w in sp.Worlds)
                {
                    longWorld |= FaunaGestureRule.IsLongWorld(w);
                }

                if (!longWorld)
                {
                    Assert.That(sp.SecondGesture, Is.EqualTo(FaunaSecondGesture.None), sp.Id + ": sin mundo largo, sin segundo gesto");
                    Assert.That(sp.SecondGestureFrom, Is.EqualTo(0));
                    continue;
                }

                Assert.That(expected.ContainsKey(sp.Id), Is.True, sp.Id + " vive en un mundo largo");
                Assert.That(sp.SecondGesture, Is.EqualTo(expected[sp.Id].Gesture), sp.Id);
                Assert.That(sp.SecondGestureFrom, Is.EqualTo(expected[sp.Id].From), sp.Id);
                Assert.That(seen.Add(sp.SecondGesture), Is.True, sp.Id + ": un gesto distinto por especie");

                // Cabe en el modelo de suelta de la especie (el dúo en cualquiera; colgarse de la cola solo colgado…).
                TroopModel model = sp.Model == FaunaModel.Hang ? TroopModel.Hang : (sp.Model == FaunaModel.Shake ? TroopModel.Shake : TroopModel.Row);
                Assert.That(TroopLayer.SecondRest(sp.SecondGesture, model), Is.Not.EqualTo(RestGesture.None), sp.Id + ": su gesto cabe en su modelo");
            }

            Assert.That(seen.Count, Is.EqualTo(expected.Count), "las cinco especies de 201-920");
        }

        [Test]
        public void Rotation_IsDeterministic_DebutsAtTheMiddle_AndAlternates()
        {
            FaunaCatalog catalog = Catalog;
            foreach (FaunaSpecies sp in catalog.Species)
            {
                if (sp.SecondGesture == FaunaSecondGesture.None)
                {
                    for (int level = sp.FromLevel; level <= sp.ToLevel; level++)
                    {
                        Assert.That(FaunaGestureRule.UsesSecond(sp, level), Is.False, sp.Id + " " + level);
                    }

                    continue;
                }

                int from = sp.SecondGestureFrom;
                int count = 0;
                for (int level = sp.FromLevel; level <= sp.ToLevel; level++)
                {
                    bool second = FaunaGestureRule.UsesSecond(sp, level);
                    Assert.That(FaunaGestureRule.UsesSecond(sp, level), Is.EqualTo(second), "mismo nivel, mismo gesto");
                    if (level < from)
                    {
                        Assert.That(second, Is.False, sp.Id + ": antes de la mitad, el primero (" + level + ")");
                        continue;
                    }

                    Assert.That(second, Is.EqualTo((level - from) % 2 == 0), sp.Id + ": alterno desde " + from + " (" + level + ")");
                    count += second ? 1 : 0;
                }

                Assert.That(FaunaGestureRule.UsesSecond(sp, from), Is.True, sp.Id + ": la mitad lo estrena");
                Assert.That(FaunaGestureRule.UsesSecond(sp, from + 1), Is.False, sp.Id + ": y el siguiente vuelve al primero");
                Assert.That(FaunaGestureRule.UsesSecond(sp, sp.ToLevel + 1), Is.False, sp.Id + ": fuera de su tramo, nunca");
                Assert.That(count, Is.EqualTo(((sp.ToLevel - from) / 2) + 1), sp.Id + ": la mitad de los niveles desde el estreno");
            }

            // 421-700 (el hueco de F13): el maquisapa lo hace desde el 491 y en el 631 (la mitad del 9) le toca también.
            FaunaSpecies ateles = catalog.Find("ateles_chamek")!;
            Assert.That(FaunaGestureRule.UsesSecond(ateles, 490), Is.False);
            Assert.That(FaunaGestureRule.UsesSecond(ateles, 491), Is.True);
            Assert.That(FaunaGestureRule.UsesSecond(ateles, 631), Is.True);
            Assert.That(FaunaGestureRule.UsesSecond(ateles, 700), Is.False);
            Assert.That(FaunaGestureRule.UsesSecond(null, 491), Is.False);

            // El extra de desarrollo af_troop_gesture: 1 el primero, 2 el segundo (si la especie lo tiene), 0 la regla.
            Assert.That(FaunaGestureRule.UsesSecond(ateles, 491, 1), Is.False);
            Assert.That(FaunaGestureRule.UsesSecond(ateles, 422, 2), Is.True);
            Assert.That(FaunaGestureRule.UsesSecond(ateles, 492, 0), Is.False);
            Assert.That(FaunaGestureRule.UsesSecond(catalog.Find("saimiri_collinsi"), 5, 2), Is.False, "sin segundo gesto no se fuerza");
        }

        [Test]
        public void Reader_ReadsTheFieldInTheLongWorlds_AndRejectsItElsewhere()
        {
            string json = CatalogJson;
            FaunaCatalog without = FaunaCatalogReader.Read(json.Replace("      \"segundo_gesto\": \"duo\",\n", string.Empty));
            FaunaSpecies titi = without.Find("plecturocebus_discolor")!;
            Assert.That(titi.SecondGesture, Is.EqualTo(FaunaSecondGesture.None), "sin el campo, solo el primer gesto");
            Assert.That(FaunaGestureRule.UsesSecond(titi, 251), Is.False);
            Assert.That(() => FaunaCatalogReader.Read(json.Replace("\"segundo_gesto\": \"vuelo\"", "\"segundo_gesto\": \"saludo\"")),
                Throws.TypeOf<FaunaFormatException>().With.Message.Contains("saludo"), "un gesto que no existe");
            string squirrel = "\"rasgos_reposo\": [\"mascara_redondeada\", \"cola_balancin\"],\n";
            int at = json.IndexOf(squirrel, System.StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThan(0));
            string extra = json.Substring(0, at + squirrel.Length) + "      \"segundo_gesto\": \"brincos\",\n" + json.Substring(at + squirrel.Length);
            Assert.That(() => FaunaCatalogReader.Read(extra), Throws.TypeOf<FaunaFormatException>().With.Message.Contains("mundos largos"),
                "el mono ardilla del mundo 1 no tiene segundo gesto");
            foreach (FaunaSecondGesture g in new[] { FaunaSecondGesture.Duet, FaunaSecondGesture.Hops, FaunaSecondGesture.TailHang, FaunaSecondGesture.Stretch, FaunaSecondGesture.Leap })
            {
                Assert.That(FaunaGestureRule.TryParse(FaunaGestureRule.NameOf(g), out FaunaSecondGesture back), Is.True);
                Assert.That(back, Is.EqualTo(g));
            }
        }

        [Test]
        public void SecondRest_FitsTheModel()
        {
            Assert.That(TroopLayer.SecondRest(FaunaSecondGesture.Duet, TroopModel.Shake), Is.EqualTo(RestGesture.Duet));
            Assert.That(TroopLayer.SecondRest(FaunaSecondGesture.Hops, TroopModel.Row), Is.EqualTo(RestGesture.Hop));
            Assert.That(TroopLayer.SecondRest(FaunaSecondGesture.TailHang, TroopModel.Hang), Is.EqualTo(RestGesture.TailHang));
            Assert.That(TroopLayer.SecondRest(FaunaSecondGesture.TailHang, TroopModel.Row), Is.EqualTo(RestGesture.None), "sentado (franja < 64 dp) no se cuelga de la cola");
            Assert.That(TroopLayer.SecondRest(FaunaSecondGesture.Stretch, TroopModel.Row), Is.EqualTo(RestGesture.Stretch));
            Assert.That(TroopLayer.SecondRest(FaunaSecondGesture.Leap, TroopModel.Row), Is.EqualTo(RestGesture.Leap));
            Assert.That(TroopLayer.SecondRest(FaunaSecondGesture.Leap, TroopModel.Hang), Is.EqualTo(RestGesture.None));
            Assert.That(TroopLayer.SecondRest(FaunaSecondGesture.None, TroopModel.Row), Is.EqualTo(RestGesture.None));
        }

        /// <summary>
        /// Brincos y vuelo caben en el reposo de siempre (≈ 1.500 ms tras la última suelta): empiezan tras el fundido de entrada y
        /// acaban antes del de salida, así que el miquito está en su sitio cuando sale; si no caben, no empiezan.
        /// </summary>
        [Test]
        public void HopsAndLeap_FitInsideTheRest_AndEndOnTheSeat()
        {
            float window = IntroChoreography.RestMs - IntroChoreography.GestureTailMs;
            for (int m = 0; m < 3; m++)
            {
                int hops = TroopLayer.HopsFor(m, window);
                Assert.That(hops, Is.EqualTo(TroopLayer.MaxHops), "miquito " + m + ": dos brincos en el reposo de siempre");
                float peak = 0f;
                for (float u = 0f; u <= window; u += 5f)
                {
                    float air = TroopLayer.HopAir(u, m, hops);
                    Assert.That(air, Is.InRange(0f, 1f));
                    peak = Mathf.Max(peak, air);
                    if (u <= TroopLayer.RestBlendMs || u >= window - TroopLayer.RestBlendMs)
                    {
                        Assert.That(air, Is.EqualTo(0f), "en la rama durante los fundidos: " + u + " ms");
                    }
                }

                Assert.That(peak, Is.GreaterThan(0.95f), "el brinco llega arriba");
            }

            Assert.That(TroopLayer.HopsFor(0, 2f * TroopLayer.RestBlendMs), Is.EqualTo(0), "sin tiempo, sin brincos");

            for (int m = 0; m < 2; m++)
            {
                Assert.That(TroopLayer.LeapFits(m, window), Is.True, "miquito " + m + ": el vuelo cabe en el reposo de siempre");
                float far = 0f;
                for (float u = 0f; u <= window; u += 5f)
                {
                    float along = TroopLayer.LeapAt(u, m, out float air, out float k, out bool back);
                    Assert.That(along, Is.InRange(0f, 1f));
                    Assert.That(air, Is.InRange(0f, 1f));
                    Assert.That(k, Is.InRange(0f, 1f));
                    far = Mathf.Max(far, along);
                    if (u >= window - TroopLayer.RestBlendMs)
                    {
                        Assert.That(along, Is.EqualTo(0f), "de vuelta en su sitio antes del fundido de salida: " + u + " ms");
                    }
                }

                Assert.That(far, Is.EqualTo(1f), "llega al otro sitio");
            }

            Assert.That(TroopLayer.LeapFits(0, TroopLayer.LeapSpanMs), Is.False, "sin los fundidos no cabe");
        }

        /// <summary>
        /// Poses del segundo gesto en cada especie: la cabeza nunca baja (D-002) ni pasa de 28°; ninguna mano por encima de la
        /// cabeza ni lejos del cuerpo (sin saludar ni señalar, D-010 punto 6); colgado, nada baja de la mano de soltar (A7); y el
        /// gesto se nota frente al reposo de siempre.
        /// </summary>
        [Test]
        public void SecondPoses_AreAnimalGestures_AndRead()
        {
            foreach (FaunaSpecies sp in Catalog.Species)
            {
                MonkeyArt art = Art.MonkeyArtFor(sp);
                foreach (RestGesture g in new[] { RestGesture.Duet, RestGesture.Stretch })
                {
                    MonkeyPose plain = MonkeyRig.SitRest(art, 1f, g, 0f, 0f);
                    float change = 0f;
                    for (float t = 0f; t <= 2000f; t += 25f)
                    {
                        MonkeyPose p = MonkeyRig.SitRest(art, 1f, g, 1f, t);
                        string where = sp.Id + ", " + g + " a " + t + " ms";
                        Assert.That(p.HeadAngle, Is.InRange(-0.001f, MonkeyRig.LookUpDeg + 0.001f), "la cabeza nunca baja: " + where);
                        AssertHands(p, art, where);
                        change = Mathf.Max(change, Mathf.Abs(p.HeadAngle - plain.HeadAngle) / 14f + (p.FrontArmTo - plain.FrontArmTo).magnitude
                            + Mathf.Abs(p.TorsoAngle - plain.TorsoAngle) / 14f);
                    }

                    Assert.That(change, Is.GreaterThan(1f), sp.Id + ", " + g + ": se nota (no un matiz)");
                }

                MonkeyPose hang = MonkeyRig.Hang(art, 0f, 0f);
                for (float t = 0f; t <= 2000f; t += 25f)
                {
                    MonkeyPose p = MonkeyRig.HangRest(art, 0f, RestGesture.TailHang, 1f, t);
                    string where = sp.Id + ", colgado de la cola a " + t + " ms";
                    Assert.That(Mathf.Min(p.FrontArmTo.y, p.BackArmTo.y), Is.GreaterThanOrEqualTo(MonkeyRig.HangHandS.y - 0.001f), "nada baja de la mano de soltar (A7): " + where);
                    Assert.That(p.HeadAngle, Is.GreaterThanOrEqualTo(-0.001f), where);
                    AssertHands(p, art, where);
                    Assert.That(Vector2.Distance(p.FrontArmTo, hang.FrontArmTo), Is.GreaterThan(0.15f), "abre el brazo libre: " + where);
                }

                for (float k = 0f; k <= 1f; k += 0.05f)
                {
                    MonkeyPose p = MonkeyRig.Leap(art, k);
                    Assert.That(p.HeadAngle, Is.GreaterThanOrEqualTo(0f), sp.Id + ": en el vuelo la cabeza no baja");
                    AssertHands(p, art, sp.Id + ", vuelo " + k);
                }
            }

            // Con los de la liana: soltar la mano que agarra es lo que se ve (cuelga solo de la cola).
            FaunaSpecies ateles = Catalog.Find("ateles_chamek")!;
            MonkeyArt a = Art.MonkeyArtFor(ateles);
            Assert.That(MonkeyRig.Hang(a, 0f, 0f).BackArmTo, Is.EqualTo(Vector2.zero), "colgado de siempre: la mano en la liana");
            Assert.That(MonkeyRig.HangRest(a, 0f, RestGesture.TailHang, 1f, 0f).BackArmTo.magnitude, Is.GreaterThan(0.3f), "suelta la liana");

            // El dúo va al compás en toda la tropa: el mismo golpe en el mismo reloj.
            Assert.That(MonkeyRig.DuetBeat(0f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(MonkeyRig.DuetHeadDeg + MonkeyRig.DuetBeatDeg, Is.LessThanOrEqualTo(MonkeyRig.LookUpDeg));
        }

        private static void AssertHands(in MonkeyPose p, MonkeyArt art, string where)
        {
            float headTop = p.HeadCenter.y + (0.5f * art.HeadS) + 0.05f;
            Assert.That(p.FrontArmTo.y, Is.LessThanOrEqualTo(headTop), "la mano de delante no sube sobre la cabeza: " + where);
            Assert.That(Mathf.Abs(p.FrontArmTo.x - p.TorsoCenter.x), Is.LessThanOrEqualTo(0.45f), "ninguna mano lejos del cuerpo (señalar): " + where);
            Assert.That(Mathf.Abs(p.BackArmTo.x - p.TorsoCenter.x), Is.LessThanOrEqualTo(0.45f), "ninguna mano lejos del cuerpo: " + where);
        }
    }
}
