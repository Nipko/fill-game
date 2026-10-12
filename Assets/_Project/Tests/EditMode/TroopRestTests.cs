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
    /// Reposo de la tropa tras la última suelta, ya en juego (D-011, punto 3): el gesto de cada miquito se elige de forma
    /// determinista por nivel (variación F13) y con los rasgos de reposo de la especie (<see cref="TroopLayer.ChooseRest"/>),
    /// y las poses de reposo (<see cref="MonkeyRig.SitRest"/>, <see cref="MonkeyRig.HangRest"/>) son gestos de animal: la
    /// cabeza nunca baja hacia el tablero (D-002), ninguna mano sube por encima de la cabeza ni se estira hacia delante
    /// (sin saludar ni señalar, D-010 punto 6) y la cola sentada nunca cuelga hacia el panel.
    /// </summary>
    public sealed class TroopRestTests
    {
        private static readonly RestGesture[] SitGestures = { RestGesture.Scratch, RestGesture.LookAround, RestGesture.Groom, RestGesture.EatFruit, RestGesture.SwingTail };

        private static readonly RestGesture[] HangGestures = { RestGesture.EatFruit, RestGesture.Scratch, RestGesture.LookAround, RestGesture.SwingBody };

        private static ProceduralArt? s_art;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [OneTimeTearDown]
        public void DisposeArt()
        {
            s_art?.Dispose();
            s_art = null;
        }

        [Test]
        public void ChooseRest_IsDeterministic_AndRotatesWithTheLevelVariation()
        {
            for (int variant = 0; variant < 3; variant++)
            {
                for (int count = 1; count <= IntroChoreography.MaxMonkeys; count++)
                {
                    for (int m = 0; m < count; m++)
                    {
                        RestGesture a = TroopLayer.ChooseRest(variant, m, count, TroopModel.Row, false, false, false, false);
                        Assert.That(TroopLayer.ChooseRest(variant, m, count, TroopModel.Row, false, false, false, false), Is.EqualTo(a), "mismo nivel, mismo gesto");
                        Assert.That(a, Is.Not.EqualTo(RestGesture.None));
                        Assert.That(System.Array.IndexOf(SitGestures, a), Is.GreaterThanOrEqualTo(0), "sentado: " + a);
                        Assert.That(a, Is.Not.EqualTo(RestGesture.SwingTail), "sin cola_balancin no balancea la cola");
                    }
                }

                // El primer miquito conserva el reposo de la variación de antes: 0 rascarse, 1 mirar al cielo, 2 acicalarse.
                RestGesture first = TroopLayer.ChooseRest(variant, 0, 3, TroopModel.Row, false, false, false, false);
                Assert.That(first, Is.EqualTo(variant == 0 ? RestGesture.Scratch : (variant == 1 ? RestGesture.LookAround : RestGesture.Groom)));
            }

            // La tropa de 3 hace tres gestos distintos y, de un nivel a otro, el primero cambia.
            var seen = new HashSet<RestGesture>();
            for (int m = 0; m < 3; m++)
            {
                seen.Add(TroopLayer.ChooseRest(1, m, 3, TroopModel.Row, false, false, false, false));
            }

            Assert.That(seen.Count, Is.EqualTo(3));
            var plan = new IntroChoreography();
            var variants = new HashSet<int>();
            for (int level = 1; level <= 30; level++)
            {
                IntroInput input = IntroChoreographyTests.Input(7, 780f, 800f, IntroPerch.Branch);
                input.Seed = level;
                plan.Plan(input);
                variants.Add(plan.RestVariant);
            }

            Assert.That(variants.Count, Is.EqualTo(3), "la variación por nivel recorre los tres reposos");
        }

        [Test]
        public void ChooseRest_FollowsTheSpeciesTraits()
        {
            for (int variant = 0; variant < 3; variant++)
            {
                for (int count = 2; count <= 5; count++)
                {
                    int swing = 0;
                    int eat = 0;
                    int body = 0;
                    for (int m = 0; m < count; m++)
                    {
                        // cola_balancin (mono ardilla): uno de la tropa, el que marca la variación, balancea la cola.
                        RestGesture tail = TroopLayer.ChooseRest(variant, m, count, TroopModel.Row, false, true, false, false);
                        swing += tail == RestGesture.SwingTail ? 1 : 0;
                        Assert.That(tail == RestGesture.SwingTail, Is.EqualTo(m == variant % count), "cola_balancin, miquito " + m);

                        // La pareja de colas_entrelazadas: la cola va trenzada, nunca se balancea.
                        Assert.That(TroopLayer.ChooseRest(variant, m, count, TroopModel.Shake, true, true, false, false), Is.Not.EqualTo(RestGesture.SwingTail));

                        // Churuco (lleva la fruta abrazada, colgado): come.
                        RestGesture woolly = TroopLayer.ChooseRest(variant, m, count, TroopModel.Hang, false, false, true, true);
                        eat += woolly == RestGesture.EatFruit && m == variant % count ? 1 : 0;

                        // Colgados de la cola (maquisapa): se balancean.
                        RestGesture spider = TroopLayer.ChooseRest(variant, m, count, TroopModel.Hang, false, false, false, true);
                        body += spider == RestGesture.SwingBody && m == variant % count ? 1 : 0;

                        // Colgados: solo los gestos del modelo cuelga (nada de acicalarse sentado ni de balancear la cola).
                        RestGesture hang = TroopLayer.ChooseRest(variant, m, count, TroopModel.Hang, false, true, false, false);
                        Assert.That(System.Array.IndexOf(HangGestures, hang), Is.GreaterThanOrEqualTo(0), "colgado: " + hang);
                        Assert.That(System.Array.IndexOf(HangGestures, spider), Is.GreaterThanOrEqualTo(0));
                        Assert.That(System.Array.IndexOf(HangGestures, woolly), Is.GreaterThanOrEqualTo(0));
                    }

                    Assert.That(swing, Is.EqualTo(1), "un miquito balancea la cola");
                    Assert.That(eat, Is.EqualTo(1), "el churuco come la fruta");
                    Assert.That(body, Is.EqualTo(1), "el de cola prensil se balancea");
                }
            }
        }

        /// <summary>
        /// Poses de reposo de cada especie del catálogo, sentada y colgada, con cada gesto durante 3 s: la cabeza nunca baja
        /// (D-002), ninguna mano sube por encima de la cabeza ni se aleja del cuerpo (sin saludar ni señalar, D-010 punto 6), la
        /// cola sentada nunca baja de la rama y el gesto se nota (algo se mueve con el tiempo).
        /// </summary>
        [Test]
        public void RestPoses_AreAnimalGestures_ForEverySpecies()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            foreach (FaunaSpecies species in catalog.Species)
            {
                MonkeyArt art = Art.MonkeyArtFor(species);
                foreach (RestGesture gesture in SitGestures)
                {
                    AssertGesture(art, gesture, false, species.Id);
                }

                foreach (RestGesture gesture in HangGestures)
                {
                    AssertGesture(art, gesture, true, species.Id);
                }
            }
        }

        [Test]
        public void EatFruit_BringsTheFruitToTheMouth_AndSwingTail_MovesOnlyTheTail()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            MonkeyArt squirrel = Art.MonkeyArtFor(catalog.Find("saimiri_collinsi")!);
            MonkeyPose bite = MonkeyRig.SitRest(squirrel, 1f, RestGesture.EatFruit, 1f, MonkeyRig.EatCycleMs * 0.5f);
            Assert.That(Vector2.Distance(bite.FrontArmTo, MonkeyRig.SitMouthS), Is.LessThan(0.01f), "en el bocado, la mano en la boca");
            MonkeyPose chew = MonkeyRig.SitRest(squirrel, 1f, RestGesture.EatFruit, 1f, 0f);
            Assert.That(Vector2.Distance(chew.FrontArmTo, MonkeyRig.SitChewS), Is.LessThan(0.01f), "entre bocados, delante del pecho");

            // Los dos extremos del ciclo común de gestos (revisión del 2026-10-11: todos los gestos lentos comparten la onda).
            MonkeyPose a = MonkeyRig.SitRest(squirrel, 1f, RestGesture.SwingTail, 1f, MonkeyRig.GestureCycleMs * 0.5f);
            MonkeyPose b = MonkeyRig.SitRest(squirrel, 1f, RestGesture.SwingTail, 1f, 0f);
            Assert.That(Vector2.Distance(a.Tail3, b.Tail3), Is.GreaterThan(0.15f), "la punta de la cola va de un lado a otro");
            Assert.That(a.Tail0, Is.EqualTo(b.Tail0), "la base de la cola no se mueve");
            Assert.That(a.TorsoCenter, Is.EqualTo(b.TorsoCenter), "el cuerpo sigue quieto");
            Assert.That(a.HeadAngle, Is.EqualTo(0f));

            // Mezcla: con amount 0 es la pose sentada de siempre (sin saltos al empezar y al acabar el reposo).
            MonkeyPose none = MonkeyRig.SitRest(squirrel, 1f, RestGesture.EatFruit, 0f, 300f);
            MonkeyPose sit = MonkeyRig.Sit(squirrel, 1f, 0f, -1, 0f);
            Assert.That(none.FrontArmTo, Is.EqualTo(sit.FrontArmTo));
            Assert.That(none.Tail3, Is.EqualTo(sit.Tail3));
        }

        /// <summary>
        /// Gestos que se ven a 48 dp (revisión del 2026-10-11: en el teléfono la mitad no se notaban): mirar alrededor sube la
        /// cabeza ≥ 25° y luego mira atrás por la rama (la pieza reflejada, un poco hacia arriba, nunca hacia abajo); rascarse
        /// lleva la mano a la oreja, por detrás del centro de la cabeza y sin subir sobre ella, y rasca a 3-4 Hz con ≥ 0,08 S de
        /// recorrido a cada lado; sentado y colgado. Los extremos de la onda caen en mitad de ciclo y al empezarlo.
        /// </summary>
        [Test]
        public void LookAround_AndScratch_ReadAt48dp()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            foreach (string id in new[] { "saimiri_collinsi", "mico_argentatus", "lagothrix_lagothricha", "ateles_chamek" })
            {
                MonkeyArt art = Art.MonkeyArtFor(catalog.Find(id)!);
                foreach (bool hanging in new[] { false, true })
                {
                    string where = id + (hanging ? " colgado" : " sentado");
                    MonkeyPose up = Rest(art, hanging, RestGesture.LookAround, MonkeyRig.GestureCycleMs * 0.5f);
                    Assert.That(up.HeadAngle, Is.GreaterThanOrEqualTo(25f), where + ": mira al cielo");
                    Assert.That(up.HeadBack, Is.False);
                    MonkeyPose back = Rest(art, hanging, RestGesture.LookAround, 0f);
                    Assert.That(back.HeadBack, Is.True, where + ": mira atrás por la rama");
                    Assert.That(back.HeadAngle, Is.InRange(0f, MonkeyRig.LookBackDeg + 0.001f), where + ": un poco hacia arriba, nunca hacia abajo");

                    float lo = float.MaxValue;
                    float hi = float.MinValue;
                    int crossings = 0;
                    float prev = 0f;
                    for (float t = 0f; t < 1000f; t += 5f)
                    {
                        MonkeyPose p = Rest(art, hanging, RestGesture.Scratch, t);
                        Vector2 hand = p.FrontArmTo;
                        float skullTop = p.HeadCenter.y + (art.HeadS * ((43.5f - 6f) / 128f));
                        Assert.That(hand.y, Is.LessThanOrEqualTo(skullTop), where + ": la mano no sube por encima de la cabeza a " + t + " ms");
                        Assert.That(hand.x, Is.LessThan(p.HeadCenter.x - 0.08f), where + ": en la oreja, por detrás de la cara, a " + t + " ms");
                        Assert.That(Mathf.Abs(hand.y - p.HeadCenter.y), Is.LessThanOrEqualTo(0.5f * art.HeadS), where + ": al costado de la cabeza");
                        lo = Mathf.Min(lo, hand.y);
                        hi = Mathf.Max(hi, hand.y);
                        float d = hand.y - p.HeadCenter.y - 0.02f;
                        crossings += t > 0f && Mathf.Sign(d) != Mathf.Sign(prev) ? 1 : 0;
                        prev = d;
                    }

                    Assert.That(hi - lo, Is.GreaterThanOrEqualTo(2f * 0.08f - 0.001f), where + ": rasca con ≥ 0,08 S a cada lado");
                    Assert.That(crossings / 2f, Is.InRange(3f, 4f), where + ": a 3-4 rascadas por segundo");
                }
            }
        }

        /// <summary>
        /// Colgado (revisión del 2026-10-11: se leía como una figura colgada de una cuerda atada a la cabeza): la mano que agarra
        /// está en la liana (el agarre), a un lado de la cabeza y nunca encima de ella; el brazo sale del hombro a 20-30° y se ve
        /// en un largo como el de la cabeza; las piernas, recogidas (los pies delante de las caderas); la cola prensil sube a la
        /// liana y se enrosca bajo la mano; y nada baja de la mano de soltar (A7), también al soltar.
        /// </summary>
        [Test]
        public void Hang_GripsTheLianaBesideTheHead_WithTheLegsTucked()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            foreach (FaunaSpecies species in catalog.Species)
            {
                MonkeyArt art = Art.MonkeyArtFor(species);
                MonkeyPose p = MonkeyRig.Hang(art, 0f, 0f);
                string id = species.Id;
                float skullR = art.HeadS * 43.5f / 128f;
                Vector2 skull = p.HeadCenter - (new Vector2(4f, 6f) * (art.HeadS / 128f));
                Assert.That(skull.x - skullR, Is.GreaterThan(0.01f), id + ": la cabeza no queda bajo el agarre");
                Assert.That(skull.y + skullR, Is.LessThan(0f), id + ": la cabeza, por debajo de la mano que agarra");
                bool armGrip = art.ArmUp || !art.TailCurls;
                if (armGrip)
                {
                    Assert.That(p.BackArmTo, Is.EqualTo(Vector2.zero), id + ": la mano, en la liana");
                    Vector2 arm = p.BackArmFrom - p.BackArmTo;
                    float deg = Mathf.Atan2(arm.x, -arm.y) * Mathf.Rad2Deg;
                    Assert.That(deg, Is.InRange(20f, 30f), id + ": el brazo sale del hombro a 20-30°");
                    float visible = 0f;
                    for (int i = 0; i <= 400; i++)
                    {
                        Vector2 q = Vector2.Lerp(p.BackArmTo, p.BackArmFrom, i / 400f);
                        if (Vector2.Distance(q, skull) <= skullR + (0.5f * art.LimbWidthS))
                        {
                            break;
                        }

                        visible = Vector2.Distance(q, p.BackArmTo);
                    }

                    Assert.That(visible + (0.5f * art.LimbWidthS), Is.GreaterThanOrEqualTo(2f * skullR), id + ": el brazo se ve en un largo como el de la cabeza");
                }
                else
                {
                    Assert.That(Vector2.Distance(p.Tail3, Vector2.zero), Is.LessThan(0.06f), id + ": colgado de la cola, la cola en la liana");
                }

                if (art.TailCurls)
                {
                    Assert.That(Vector2.Distance(p.Tail3, Vector2.zero), Is.LessThan(0.06f), id + ": la cola prensil, enroscada bajo la mano");
                }

                Assert.That(p.BackLegTo.x, Is.GreaterThan(p.BackLegFrom.x + 0.05f), id + ": piernas recogidas");
                Assert.That(p.FrontLegTo.x, Is.GreaterThan(p.FrontLegFrom.x + 0.05f), id + ": piernas recogidas");
                foreach (float lift in new[] { 0f, 1f })
                {
                    MonkeyPose q = MonkeyRig.Hang(art, lift, 0f);
                    float lowest = Mathf.Min(Mathf.Min(q.BackLegTo.y, q.FrontLegTo.y), Mathf.Min(Mathf.Min(q.Tail1.y, q.Tail2.y), Mathf.Min(q.Tail3.y, q.FrontArmTo.y)));
                    Assert.That(lowest, Is.GreaterThanOrEqualTo(MonkeyRig.HangHandS.y - 0.001f), id + ": nada baja de la mano de soltar (A7)");
                }

                Assert.That(Vector2.Distance(MonkeyRig.Hang(art, 1f, 0f).FrontArmTo, MonkeyRig.HangHandS), Is.LessThan(1e-4f), id + ": suelta desde la mano de soltar de siempre");
            }
        }

        private static MonkeyPose Rest(MonkeyArt art, bool hanging, RestGesture gesture, float clockMs)
        {
            return hanging ? MonkeyRig.HangRest(art, 0f, gesture, 1f, clockMs) : MonkeyRig.SitRest(art, 1f, gesture, 1f, clockMs);
        }

        private static void AssertGesture(MonkeyArt art, RestGesture gesture, bool hanging, string id)
        {
            float minTail = float.MaxValue;
            float maxTail = float.MinValue;
            float minHead = float.MaxValue;
            float maxHead = float.MinValue;
            float minHand = float.MaxValue;
            float maxHand = float.MinValue;
            for (float t = 0f; t <= 3000f; t += 25f)
            {
                foreach (float amount in new[] { 0.5f, 1f })
                {
                    MonkeyPose p = hanging ? MonkeyRig.HangRest(art, 0f, gesture, amount, t) : MonkeyRig.SitRest(art, 1f, gesture, amount, t);
                    MonkeyPose rest = hanging ? MonkeyRig.HangRest(art, 0f, gesture, 0f, t) : MonkeyRig.SitRest(art, 1f, gesture, 0f, t);
                    string where = id + ", " + gesture + (hanging ? " colgado" : " sentado") + " a " + t + " ms";
                    Assert.That(p.HeadAngle, Is.GreaterThanOrEqualTo(-0.001f), "la cabeza nunca baja hacia el tablero: " + where);
                    float headTop = p.HeadCenter.y + (0.5f * art.HeadS) + 0.05f;
                    for (int h = 0; h < 2; h++)
                    {
                        Vector2 hand = h == 0 ? p.FrontArmTo : p.BackArmTo;
                        if (hand == (h == 0 ? rest.FrontArmTo : rest.BackArmTo))
                        {
                            continue; // la mano que el gesto no mueve (colgado, la que agarra la rama)
                        }

                        Assert.That(hand.y, Is.LessThanOrEqualTo(headTop), "ninguna mano por encima de la cabeza (saludar): " + where);
                        Assert.That(Mathf.Abs(hand.x - p.TorsoCenter.x), Is.LessThanOrEqualTo(0.45f), "ninguna mano lejos del cuerpo (señalar): " + where);
                    }

                    if (!hanging)
                    {
                        Assert.That(Mathf.Min(Mathf.Min(p.Tail1.y, p.Tail2.y), p.Tail3.y), Is.GreaterThanOrEqualTo(0f), "la cola no cuelga hacia el panel: " + where);
                    }

                    if (amount >= 1f)
                    {
                        minTail = Mathf.Min(minTail, p.Tail3.x + p.Tail3.y);
                        maxTail = Mathf.Max(maxTail, p.Tail3.x + p.Tail3.y);
                        minHead = Mathf.Min(minHead, p.HeadAngle);
                        maxHead = Mathf.Max(maxHead, p.HeadAngle);
                        float h = p.FrontArmTo.x + p.FrontArmTo.y + p.BackArmTo.x + p.BackArmTo.y;
                        minHand = Mathf.Min(minHand, h);
                        maxHand = Mathf.Max(maxHand, h);
                    }
                }
            }

            if (gesture == RestGesture.SwingBody)
            {
                return; // el balanceo colgado lo da la oscilación de la tropa (TroopLayer), no la pose
            }

            float change = (maxTail - minTail) + ((maxHead - minHead) / 14f) + (maxHand - minHand);
            Assert.That(change, Is.GreaterThan(0.02f), id + ", " + gesture + (hanging ? " colgado" : " sentado") + ": el gesto se nota");
        }
    }
}
