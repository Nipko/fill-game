#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Rig procedural provisional del perezoso protagonista (tarea TI4; propuesta del personaje c.4-c.7 y paso 4 de (i); D-016;
    /// 07 §6.4): las 10 poses de c.6 con sus ID neutros, funciones puras del tiempo que no se salen de su caja; las hojas de
    /// celebrar (4 y 6) como piezas del rig; Reducir movimiento (pose quieta con el fundido de 150 ms); que nada se crea sin
    /// pedirlo; que en un build que no es de desarrollo no aparece sin el PNG de las piezas ni sin el de su rama (c.4); que el
    /// PNG de <c>fauna_bradypus_variegatus_partes</c> manda; que las hojas de <c>sloth_cheer_big</c> no tapan la cara ni el
    /// espéculo a media vuelta; las 10 piezas sin raycast; y que el bucle de cada fotograma no asigna.
    /// </summary>
    public sealed class SlothRigTests
    {
        private static readonly string[] ExpectedIds =
        {
            "sloth_idle", "sloth_wake", "sloth_wave", "sloth_present", "sloth_cheer", "sloth_cheer_big", "sloth_encourage",
            "sloth_look", "sloth_sunbathe", "sloth_sleep",
        };

        private static readonly float[] SampleMs = { 0f, 50f, 150f, 299f, 300f, 450f, 599f, 600f, 750f, 800f, 999f, 1000f, 1400f, 1500f, 2000f, 2600f, 4380f, 4499f, 10500f, 11000f, 30000f };

        private static float s_sink;

        // El arte procedural cuesta unos 3 s por instancia: uno para todo el fixture (T6, carril A), creado con
        // ArtSlots.ForceProcedural como lo pone SetUp. Los tests que miden lo que se crea (ThePieces_AreLazy…,
        // NotADevelopmentBuild… y ThePartsPng_Rules…) usan uno nuevo (Fresh) que TearDown libera.
        private static ProceduralArt? s_art;
        private ProceduralArt _art = null!;
        private GameObject? _canvasGo;
        private RectTransform _space = null!;
        private Texture2D? _pngTexture;
        private Sprite? _png;
        private Texture2D? _branchTexture;
        private Sprite? _branchPng;

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = null;
            ArtSlots.ForceProcedural = true; // sin los PNG del equipo: lo que sale es el provisional (o nada)
            _art = s_art ??= new ProceduralArt("white");
            _canvasGo = new GameObject("SlothRigTestCanvas", typeof(RectTransform), typeof(Canvas));
            _canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var layer = new GameObject("Layer", typeof(RectTransform));
            layer.transform.SetParent(_canvasGo.transform, false);
            _space = (RectTransform)layer.transform;
            _space.anchorMin = Vector2.zero;
            _space.anchorMax = Vector2.one;
            _space.sizeDelta = Vector2.zero;
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            ArtSlots.ForceProcedural = false;
            if (!ReferenceEquals(_art, s_art))
            {
                _art.Dispose();
            }

            if (_canvasGo != null)
            {
                Object.Destroy(_canvasGo);
            }

            if (_png != null)
            {
                Object.Destroy(_png);
            }

            if (_pngTexture != null)
            {
                Object.Destroy(_pngTexture);
            }

            if (_branchPng != null)
            {
                Object.Destroy(_branchPng);
            }

            if (_branchTexture != null)
            {
                Object.Destroy(_branchTexture);
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        // Un arte nuevo para el test que mide lo que se crea (TearDown lo libera).
        private ProceduralArt Fresh()
        {
            _art = new ProceduralArt("white");
            return _art;
        }

        [Test]
        public void TheTenPoses_HaveTheNeutralIdsOfC6()
        {
            var poses = (SlothPoseId[])Enum.GetValues(typeof(SlothPoseId));
            Assert.That(poses.Length, Is.EqualTo(10), "las 10 poses de la tarea (c.6 sin sloth_swim, que es opcional)");
            for (int i = 0; i < poses.Length; i++)
            {
                string id = SlothRig.IdOf(poses[i]);
                Assert.That(id, Is.EqualTo(ExpectedIds[i]));
                Assert.That(id.IndexOf("titino", StringComparison.OrdinalIgnoreCase), Is.LessThan(0), "ningún id lleva el nombre (T4-A)");
                Assert.That(id.IndexOf("tintalu", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
            }

            Assert.That(ProceduralArt.SlothPartsSlot, Is.EqualTo("fauna_bradypus_variegatus_partes"));
            Assert.That(ArtSlots.IsKnown(ProceduralArt.SlothPartsSlot), Is.True);
            Assert.That(ProceduralArt.SlothBranchSlot, Is.EqualTo("arbol_embauba"));
            Assert.That(SlothRig.PieceCount, Is.InRange(8, 10), "rig recortable de 8-10 piezas");
        }

        [Test]
        public void EveryPose_IsGenerated_FiniteAndInsideItsBox([Values] SlothPoseId pose, [Values(false, true)] bool reduceMotion)
        {
            for (int i = 0; i < SampleMs.Length; i++)
            {
                SlothPose p = SlothRig.Evaluate(pose, SampleMs[i], reduceMotion);
                string at = SlothRig.IdOf(pose) + " a " + SampleMs[i] + " ms";
                AssertInBox(p.TorsoCenter, at + ": torso");
                AssertInBox(p.HeadCenter, at + ": cabeza");
                AssertInBox(p.BackArmFrom, at);
                AssertInBox(p.BackArmTo, at);
                AssertInBox(p.FrontArmFrom, at);
                AssertInBox(p.FrontArmTo, at);
                AssertInBox(p.BackLegFrom, at);
                AssertInBox(p.BackLegTo, at);
                AssertInBox(p.FrontLegFrom, at);
                AssertInBox(p.FrontLegTo, at);
                Assert.That(p.TorsoSize.x, Is.InRange(0.3f, 0.7f), at);
                Assert.That(p.TorsoSize.y, Is.InRange(0.3f, 0.7f), at);
                Assert.That(p.BodySpin, Is.InRange(-1f, 1f), at);
                Assert.That(p.HeadTurn, Is.InRange(0f, 1f), at);
                Assert.That(p.Alpha, Is.InRange(0f, 1f), at);
                Assert.That(p.Leaves, Is.EqualTo(0).Or.EqualTo(SlothRig.CheerLeaves).Or.EqualTo(SlothRig.LeafCount), at);
            }
        }

        [Test]
        public void ThePoses_DoWhatC6Says()
        {
            SlothPose rest = SlothRig.Evaluate(SlothPoseId.Idle, 0f, false);
            Assert.That(rest.EyesClosed, Is.False);
            Assert.That(rest.FrontArmTo.y, Is.GreaterThan(0f), "colgado de su rama: las manos en la rama");
            Assert.That(rest.BackArmTo.y, Is.GreaterThan(0f));
            Assert.That(rest.FrontLegTo.y, Is.GreaterThan(0f), "abrazado a ella: también los pies en la rama (c.5, punto 7)");
            Assert.That(rest.BackLegTo.y, Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(rest.FrontLegTo.y - rest.FrontArmTo.y), Is.LessThan(0.01f), "a la misma altura que las manos");
            Assert.That(rest.HeadCenter.y, Is.LessThan(0f), "y el cuerpo bajo ella");
            Assert.That(rest.TorsoCenter.y, Is.LessThan(rest.HeadCenter.y), "boca abajo, colgado: nunca de pie");
            foreach (SlothPoseId hanging in new[] { SlothPoseId.Idle, SlothPoseId.Wake, SlothPoseId.Wave, SlothPoseId.Present, SlothPoseId.Cheer, SlothPoseId.Encourage, SlothPoseId.Look })
            {
                SlothPose h = SlothRig.Evaluate(hanging, 0f, true);
                Assert.That(h.FrontLegTo.y > 0f && h.BackLegTo.y > 0f, Is.True, SlothRig.IdOf(hanging) + ": los pies, agarrados a la rama");
            }

            Assert.That(SlothRig.Evaluate(SlothPoseId.Idle, SlothRig.BlinkEveryMs - 60f, false).EyesClosed, Is.True, "parpadea");
            Assert.That(SlothRig.Evaluate(SlothPoseId.Idle, 11000f, false).HeadAngle, Is.GreaterThan(5f), "gira la cabeza muy despacio");

            SlothPose waking = SlothRig.Evaluate(SlothPoseId.Wake, 0f, false);
            Assert.That(waking.EyesClosed, Is.True, "sloth_wake empieza dormido");
            SlothPose reach = SlothRig.Evaluate(SlothPoseId.Wake, 950f, false);
            Assert.That(reach.EyesClosed, Is.False);
            Assert.That(reach.FrontArmTo.y, Is.GreaterThan(0.12f), "estira un brazo hacia la rama de arriba");
            Assert.That(SlothRig.Evaluate(SlothPoseId.Wake, 1300f, false).HeadAngle, Is.GreaterThan(5f), "aún mira a un lado");
            Assert.That(SlothRig.Evaluate(SlothPoseId.Wake, SlothRig.WakeMs, false).HeadAngle, Is.EqualTo(0f).Within(1e-4f), "y gira la cabeza hacia ti");
            Assert.That(SlothRig.WakeMs, Is.LessThanOrEqualTo(2500f), "≤ 2,5 s");

            SlothPose wave = SlothRig.Evaluate(SlothPoseId.Wave, SlothRig.WaveMs / 2f, false);
            Assert.That(wave.FrontArmTo.y, Is.InRange(rest.FrontArmTo.y + 0.12f, rest.FrontArmTo.y + 0.16f), "suelta una garra de la rama y la sube 0,14 S: se ve a 64-96 dp");
            Assert.That(wave.FrontArmTo.x - rest.FrontArmTo.x, Is.InRange(0.15f, 0.21f), "hacia fuera, de lado, nunca hacia la cámara (c.5, punto 8)");
            Assert.That(wave.HeadTurn, Is.EqualTo(0f), "y te mira de frente");
            Assert.That(wave.HeadAngle, Is.EqualTo(0f));
            SlothPose presentAt = SlothRig.Evaluate(SlothPoseId.Present, SlothRig.PresentMs, false);
            float wrist = 0f;
            for (float ms = 0f; ms <= SlothRig.WaveMs; ms += 20f)
            {
                SlothPose w = SlothRig.Evaluate(SlothPoseId.Wave, ms, false);
                string at = "sloth_wave a " + ms + " ms";
                Assert.That(w.FrontArmTo.x, Is.GreaterThanOrEqualTo(rest.FrontArmTo.x - 1e-4f), at + ": nunca hacia dentro del agarre");
                Assert.That(w.FrontArmTo.y, Is.GreaterThanOrEqualTo(rest.FrontArmTo.y - 1e-4f), at + ": ni por debajo de la rama");
                Assert.That(Vector2.Distance(w.FrontArmTo, presentAt.FrontArmTo), Is.GreaterThan(0.3f), at + ": nunca llega al punto de presentar");
                Assert.That(Vector2.Distance(w.FrontArmTo, w.FrontArmFrom), Is.EqualTo(Vector2.Distance(Vector2.Lerp(rest.FrontArmTo, wave.FrontArmTo, Mathf.Sin(Mathf.PI * ms / SlothRig.WaveMs)), w.FrontArmFrom)).Within(1e-4f), at + ": el giro de la garra no estira el brazo");
                Vector2 straight = Vector2.Lerp(rest.FrontArmTo, wave.FrontArmTo, Mathf.Sin(Mathf.PI * ms / SlothRig.WaveMs)) - w.FrontArmFrom;
                wrist = Mathf.Max(wrist, Mathf.Abs(Vector2.SignedAngle(straight, w.FrontArmTo - w.FrontArmFrom)));
            }

            Assert.That(wrist, Is.InRange(9f, 10.01f), "con un giro lento de la garra de ±10°");

            SlothPose present = SlothRig.Evaluate(SlothPoseId.Present, SlothRig.PresentMs, false);
            Assert.That(present.HeadAngle, Is.LessThan(-5f), "ladea la cabeza hacia la instrucción");
            Assert.That(present.FrontArmTo.x, Is.GreaterThan(0.45f), "y la señala con la garra");

            SlothPose cheer = SlothRig.Evaluate(SlothPoseId.Cheer, SlothRig.CheerEntryMs, false);
            Assert.That(cheer.FrontArmTo.y, Is.GreaterThan(wave.FrontArmTo.y + 0.05f), "levanta la garra bien alta, sobre la cabeza: no se confunde con el saludo");
            Assert.That(cheer.FrontArmTo.x, Is.LessThan(wave.FrontArmTo.x - 0.25f), "el saludo va hacia fuera y la celebración, arriba");
            Assert.That(Vector2.Distance(cheer.FrontArmTo, wave.FrontArmTo), Is.GreaterThan(0.15f));
            for (float ms = 0f; ms <= SlothRig.WaveMs; ms += 20f)
            {
                Assert.That(Vector2.Distance(SlothRig.Evaluate(SlothPoseId.Wave, ms, false).FrontArmTo, cheer.FrontArmTo), Is.GreaterThan(0.15f), "el saludo nunca llega a la garra de celebrar (" + ms + " ms)");
            }

            SlothPose stretching = SlothRig.Evaluate(SlothPoseId.Wake, 950f, false);
            Assert.That(Vector2.Distance(cheer.FrontArmTo, stretching.FrontArmTo), Is.GreaterThan(0.15f), "ni con el despertar");
            float tilt = 0f;
            for (float ms = SlothRig.CheerEntryMs; ms < SlothRig.CheerEntryMs + SlothRig.CheerLoopMs; ms += 50f)
            {
                tilt = Mathf.Max(tilt, Mathf.Abs(SlothRig.Evaluate(SlothPoseId.Cheer, ms, false).HeadAngle));
            }

            Assert.That(tilt, Is.GreaterThan(7f), "con la cabeza ladeada ±8°");
            Assert.That(cheer.Leaves, Is.EqualTo(4), "caen 3-4 hojas");
            Assert.That(SlothRig.Evaluate(SlothPoseId.CheerBig, 400f, false).Leaves, Is.EqualTo(6), "y 2 más en la grande");
            SlothPose turn = SlothRig.Evaluate(SlothPoseId.CheerBig, SlothRig.CheerEntryMs + (SlothRig.CheerLoopMs / 2f), false);
            Assert.That(turn.BodySpin, Is.LessThan(-0.9f), "una vuelta colgado de una mano");
            Assert.That(turn.TorsoBack, Is.True, "a media vuelta, de espaldas: el lomo con el espéculo (c.5, punto 9)");
            Assert.That(turn.FrontLegTo.y, Is.LessThan(-0.5f), "colgado de una mano: suelta los pies");
            Assert.That(SlothRig.Evaluate(SlothPoseId.Cheer, SlothRig.CheerEntryMs + (SlothRig.CheerLoopMs / 2f), false).BodySpin, Is.EqualTo(1f), "la normal no da la vuelta");
            Assert.That(SlothRig.Evaluate(SlothPoseId.CheerBig, 2000f, false).BodySpin, Is.EqualTo(1f), "la vuelta solo en el primer bucle");

            SlothPose encourage = SlothRig.Evaluate(SlothPoseId.Encourage, 200f, false);
            Assert.That(encourage.BodyAngle, Is.InRange(6f, 8f), "se balancea en la rama: ≈ 7° a 0,2 s, visible a 64-96 dp");
            Assert.That(encourage.HeadCenter.y, Is.LessThan(rest.HeadCenter.y - 0.08f), "y asiente (0,07 S)");
            Assert.That(encourage.HeadAngle, Is.EqualTo(18f).Within(0.5f), "con 6° más de ladeo en la cabezada");
            float lateSwing = 0f;
            for (float ms = 500f; ms < SlothRig.EncourageMs; ms += 20f)
            {
                lateSwing = Mathf.Max(lateSwing, Mathf.Abs(SlothRig.Evaluate(SlothPoseId.Encourage, ms, false).BodyAngle));
            }

            Assert.That(lateSwing, Is.GreaterThan(3f), "el balanceo se apaga despacio, como (1 − u)^0,5");
            SlothPose encourageRest = SlothRig.Evaluate(SlothPoseId.Encourage, 5000f, false);
            Assert.That(encourageRest.HeadAngle, Is.EqualTo(12f).Within(1e-3f), "en reposo, la cabeza ladeada 12°");
            Assert.That(encourageRest.HeadCenter.y, Is.EqualTo(rest.HeadCenter.y - 0.03f).Within(0.006f), "y algo más baja: no se confunde con sloth_idle");
            SlothPose encourageStill = SlothRig.Evaluate(SlothPoseId.Encourage, 1000f, true);
            Assert.That(encourageStill.HeadAngle, Is.EqualTo(encourageRest.HeadAngle).Within(1e-3f), "con Reducir movimiento, el mismo reposo");
            Assert.That(encourageStill.HeadCenter.y, Is.EqualTo(encourageRest.HeadCenter.y).Within(0.006f));

            SlothPose look = SlothRig.Evaluate(SlothPoseId.Look, SlothRig.LookMs / 2f, false);
            Assert.That(look.HeadAngle, Is.InRange(15f, 25f), "gira la cabeza: poco ladeo, para que se lea como un giro");
            Assert.That(look.HeadTurn, Is.GreaterThan(0.7f), "y la pieza estrechada");

            SlothPose sun = SlothRig.Evaluate(SlothPoseId.Sunbathe, 0f, false);
            Assert.That(sun.TorsoCenter.y, Is.GreaterThan(0f), "al sol sobre la rama");
            Assert.That(Mathf.Abs(sun.TorsoAngle), Is.EqualTo(90f), "tumbado");
            Assert.That(sun.EyesClosed, Is.True);
            float claw = 0.06f; // la garra pasa media anchura de la extremidad más allá de la mano o el pie
            foreach (Vector2[] limb in new[] { new[] { sun.FrontArmFrom, sun.FrontArmTo }, new[] { sun.FrontLegFrom, sun.FrontLegTo } })
            {
                Vector2 tip = limb[1] + ((limb[1] - limb[0]).normalized * claw);
                Assert.That(tip.x, Is.InRange(sun.TorsoCenter.x - (0.5f * sun.TorsoSize.y), sun.TorsoCenter.x + (0.5f * sun.TorsoSize.y)), "sobre la barriga: dentro del torso");
                Assert.That(tip.y, Is.InRange(sun.TorsoCenter.y - (0.5f * sun.TorsoSize.x), sun.TorsoCenter.y + (0.5f * sun.TorsoSize.x) - 0.04f), "sin sobresalir por encima del torso");
            }

            Assert.That(sun.HeadAngle, Is.GreaterThanOrEqualTo(40f), "la cabeza, apoyada en la rama");
            SlothPose sleep = SlothRig.Evaluate(SlothPoseId.Sleep, 0f, false);
            Assert.That(sleep.TorsoCenter.y, Is.GreaterThan(0f), "en ovillo sobre la rama");
            Assert.That(sleep.EyesClosed, Is.True);
            Assert.That(sleep.TorsoBack, Is.True, "da la espalda (que el espéculo se vea lo mide TheSpeculum_IsInSight_WhenTheBackShows)");
            Assert.That(rest.TorsoBack || sun.TorsoBack, Is.False, "de frente, sin el espéculo (no es un collar: c.4)");
            Assert.That(sleep.Leaves + sun.Leaves + rest.Leaves, Is.EqualTo(0), "solo caen hojas al celebrar");
        }

        [Test]
        public void TheOneShots_EndInTheRest_AndNeverDelayAnything()
        {
            SlothPose rest = SlothRig.Evaluate(SlothPoseId.Idle, 0f, false);
            Assert.That(SlothRig.Evaluate(SlothPoseId.Wave, SlothRig.WaveMs, false).FrontArmTo, Is.EqualTo(rest.FrontArmTo), "sloth_wave: 0,8 s");
            Assert.That(SlothRig.Evaluate(SlothPoseId.Look, SlothRig.LookMs, false).HeadTurn, Is.EqualTo(0f), "sloth_look: 1 s");
            Assert.That(SlothRig.Evaluate(SlothPoseId.Look, SlothRig.LookMs, false).HeadAngle, Is.EqualTo(0f));
            Assert.That(SlothRig.Evaluate(SlothPoseId.Encourage, 5000f, false).BodyAngle, Is.EqualTo(0f), "sloth_encourage: 0,8 s y reposo");
            Assert.That(SlothRig.Evaluate(SlothPoseId.Present, 5000f, false).FrontArmTo, Is.EqualTo(SlothRig.Evaluate(SlothPoseId.Present, SlothRig.PresentMs, false).FrontArmTo), "sloth_present: 0,6 s y reposo");
            Assert.That(SlothRig.CheerEntryMs, Is.EqualTo(300f));
            Assert.That(SlothRig.CheerLoopMs, Is.EqualTo(1200f));
            Assert.That(SlothRig.IdleLoopMs, Is.EqualTo(3000f));
            Assert.That(SlothRig.BlinkEveryMs, Is.InRange(3000f, 6000f), "parpadea cada 3-6 s");
            Assert.That(SlothRig.IdleHeadTurnEveryMs, Is.InRange(15000f, 30000f), "gira la cabeza cada 15-30 s");
        }

        [Test]
        public void ReduceMotion_StillRestFrame_WithA150msFade([Values] SlothPoseId pose)
        {
            Assert.That(SlothRig.ReducedMotionFadeMs, Is.EqualTo(150));
            Assert.That(SlothRig.ReducedMotionFadeMs, Is.EqualTo(MapConfig.ReducedMotionFadeMs), "el mismo fundido que el Mapa");
            Assert.That(SlothRig.Evaluate(pose, 0f, true).Alpha, Is.EqualTo(0f));
            Assert.That(SlothRig.Evaluate(pose, 75f, true).Alpha, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(SlothRig.Evaluate(pose, 150f, true).Alpha, Is.EqualTo(1f));
            SlothPose a = SlothRig.Evaluate(pose, 200f, true);
            for (int i = 0; i < SampleMs.Length; i++)
            {
                SlothPose b = SlothRig.Evaluate(pose, Mathf.Max(150f, SampleMs[i]), true);
                Assert.That(b.TorsoCenter, Is.EqualTo(a.TorsoCenter), "quieta");
                Assert.That(b.TorsoSize, Is.EqualTo(a.TorsoSize), "sin respirar");
                Assert.That(b.HeadCenter, Is.EqualTo(a.HeadCenter));
                Assert.That(b.HeadAngle, Is.EqualTo(a.HeadAngle));
                Assert.That(b.FrontArmTo, Is.EqualTo(a.FrontArmTo));
                Assert.That(b.EyesClosed, Is.EqualTo(a.EyesClosed), "sin parpadeo");
                Assert.That(b.BodySpin, Is.EqualTo(1f), "sin vuelta");
                Assert.That(b.BodyAngle, Is.EqualTo(0f), "sin balanceo");
                Assert.That(b.Leaves, Is.EqualTo(0), "sin hojas que caen");
            }
        }

        [Test]
        public void Leaves_ArePiecesOfTheRig_FallingFromTheBranchAndFading()
        {
            for (int i = 0; i < SlothRig.LeafCount; i++)
            {
                for (float phase = 0f; phase < 1f; phase += 0.05f)
                {
                    SlothRig.LeafAt(i, phase, out Vector2 at, out float angle, out float alpha);
                    Assert.That(at.x, Is.InRange(-0.75f, 0.75f), "dentro de la rama");
                    Assert.That(at.y, Is.InRange(-1.0f, 0.03f), "caen desde la rama");
                    Assert.That(alpha, Is.InRange(0f, 1f));
                    Assert.That(float.IsNaN(angle), Is.False);
                }
            }

            SlothRig.LeafAt(0, 0f, out _, out _, out float start);
            SlothRig.LeafAt(0, 0.999f, out _, out _, out float end);
            Assert.That(start, Is.EqualTo(0f), "aparecen");
            Assert.That(end, Is.LessThan(0.01f), "y se funden");
        }

        [Test]
        public void ThePieces_AreLazy_NothingIsCreatedUntilAsked()
        {
            Fresh();
            Assert.That(_art.SlothRigArtBuilt, Is.False, "el constructor no crea el rig");
            Assert.That(_art.SlothBranchBuilt, Is.False, "ni la rama");
            int objects = SceneRectTransforms();

            SlothArt art = _art.SlothRigArtProcedural();
            Assert.That(_art.SlothRigArtBuilt, Is.True);
            Assert.That(_art.SlothBranchBuilt, Is.False, "pedir las piezas no crea la rama");
            Assert.That(_art.SlothPortraitBuilt, Is.False, "ni el retrato");
            Assert.That(_art.SlothRigArtProcedural(), Is.SameAs(art), "se crean una vez y se guardan");
            Assert.That(_art.SlothArtMs, Is.EqualTo(0d), "de la caché");
            Assert.That(art.Painted, Is.False);
            Assert.That(art.Head, Is.Not.Null);
            Assert.That(art.Torso, Is.Not.Null);
            Assert.That(art.Limb, Is.Not.Null);
            Assert.That(art.Eyelid, Is.Not.Null);
            Assert.That(art.Leaf, Is.Not.Null);
            Assert.That(art.TorsoBack, Is.Not.Null);
            Assert.That(art.TorsoBack, Is.Not.SameAs(art.Torso));
            Assert.That(art.Head.texture.width, Is.EqualTo(ProceduralArt.SlothAtlasPx), "un solo atlas");
            MapArtRule.ForceProvisional = true;
            Assert.That(_art.SlothBranchProcedural(), Is.Not.Null);
            Assert.That(_art.SlothRigArt(), Is.Not.Null);
            Assert.That(SceneRectTransforms(), Is.EqualTo(objects), "las piezas y la rama no crean ningún objeto en la escena: solo el rig, cuando se pide");
            SlothRig.Create(_space, _art.SlothRigArt(), 72f);
            Assert.That(SceneRectTransforms(), Is.GreaterThan(objects), "(el contador sí ve los objetos del rig)");

            Sprite branch = _art.SlothBranchProcedural();
            Assert.That(_art.SlothBranchBuilt, Is.True);
            Assert.That(branch.rect.width / branch.rect.height, Is.EqualTo(4f).Within(0.01f), "la proporción de arbol_embauba (1024 × 256)");

            _art.Dispose();
            Assert.That(_art.SlothRigArtBuilt, Is.False, "tras liberar, nada queda");
            Assert.That(_art.SlothRigArtProcedural(), Is.Not.SameAs(art), "y se vuelven a generar al pedirlas");
        }

        [Test]
        public void WithoutCodeArtOrThePng_TheSlothDoesNotAppear_AndInReleaseTheCodeOneDoes()
        {
            Fresh();
            int children = _space.childCount;
            MapArtRule.ForceProvisional = false; // forzado: hoy el arte por código sale en todos los builds, release incluido (D-052)
            Assert.That(_art.SlothRigArt(), Is.Null, "sin arte por código ni PNG: nada (07 §6.4)");
            Assert.That(_art.SlothRigArt(null, null, false), Is.Null);
            Assert.That(_art.SlothRigArt(MakePng(), null, false), Is.Null, "con las piezas y sin la rama, tampoco: nunca cuelga de nada (c.4)");
            Assert.That(_art.SlothRigArt(null, MakeBranchPng(), false), Is.Null, "ni con la rama y sin las piezas");
            Assert.That(_art.SlothRigArtBuilt, Is.False, "y el provisional no se crea");
            Assert.That(_art.SlothBranchBuilt, Is.False);
            Assert.That(SlothRig.Create(_space, _art.SlothRigArt(), 72f), Is.Null, "ni el rig");
            Assert.That(_space.childCount, Is.EqualTo(children), "ni ningún objeto");

            MapArtRule.ForceProvisional = null; // la regla real, la misma en release (D-052)
            SlothArt? dev = _art.SlothRigArt();
            Assert.That(MapArtRule.ShowProvisionalIn(false, false), Is.True, "release");
            Assert.That(dev, Is.Not.Null, "en release, sin PNG, el dibujo por código");
            Assert.That(dev!.Branch, Is.Not.Null, "con su rama");

            var rig = new SlothRig(_space);
            Assert.That(rig.SetArt(null, 72f), Is.False);
            rig.Play(SlothPoseId.Cheer, false);
            Assert.That(rig.Visible, Is.False, "sin piezas no se muestra nunca");
        }

        [Test]
        public void ThePartsPng_Rules_AndThenTheProvisionalIsNeverCreated()
        {
            Fresh();
            Sprite png = MakePng();
            Assert.That(_art.SlothRigArt(png, null, false), Is.Null, "sin arte por código, con las piezas y sin la rama: no sale (c.4; 07 §6.4)");
            Assert.That(_art.SlothRigArtBuilt, Is.False, "y no se crea nada");
            Assert.That(_art.SlothBranchBuilt, Is.False);
            Sprite branchPng = MakeBranchPng();
            SlothArt? painted = _art.SlothRigArt(png, branchPng, false);
            Assert.That(painted, Is.Not.Null, "con los dos PNG sale también sin arte por código");
            Assert.That(painted!.Painted, Is.True);
            Assert.That(painted.Head.texture, Is.SameAs(png.texture));
            Assert.That(painted.Head.rect, Is.EqualTo(new Rect(0f, 768f, 256f, 256f)), "celda de la cabeza: arriba a la izquierda");
            Assert.That(painted.Torso.rect, Is.EqualTo(new Rect(256f, 768f, 256f, 256f)));
            Assert.That(painted.Limb.rect, Is.EqualTo(new Rect(512f, 768f, 64f, 256f)));
            Assert.That(painted.Eyelid.rect, Is.EqualTo(new Rect(768f, 960f, 192f, 64f)));
            Assert.That(painted.Leaf.rect, Is.EqualTo(new Rect(0f, 512f, 256f, 256f)));
            Assert.That(painted.TorsoBack.rect, Is.EqualTo(new Rect(256f, 512f, 256f, 256f)));
            Assert.That(painted.Branch, Is.SameAs(branchPng), "con la rama de arbol_embauba");
            Assert.That(_art.SlothRigArtBuilt, Is.False, "con el PNG, las piezas provisionales no se crean");
            Assert.That(_art.SlothBranchBuilt, Is.False);
            Assert.That(_art.SlothArtMs, Is.EqualTo(0d), "del PNG: sin tiempo de generación");
            SlothArt? dev = _art.SlothRigArt(png, null, true);
            Assert.That(dev, Is.Not.Null);
            Assert.That(dev!.Head, Is.SameAs(painted.Head), "una vez por PNG: las mismas piezas");
            Assert.That(dev.Branch, Is.Not.Null, "en desarrollo, la rama provisional acompaña al PNG");
            Assert.That(dev.Branch, Is.Not.SameAs(branchPng));
            Assert.That(painted.Branch, Is.SameAs(branchPng), "sin cambiar lo que ya recibió la otra llamada (estado compartido)");
            Assert.That(_art.SlothRigArt(png, branchPng, false), Is.SameAs(painted), "y cada combinación recibe siempre el mismo");
            Assert.That(_art.SlothRigArt(png, null, true), Is.SameAs(dev));
            Assert.That(_art.SlothRigArt(png, branchPng, true), Is.SameAs(painted), "con la rama de su PNG, también en desarrollo");
            Assert.That(dev.Branch, Is.Not.Null);
            Assert.That(_art.SlothRigArtBuilt, Is.False);
        }

        [Test]
        public void TheRig_HasTenPieces_NoRaycast_AndPlaysEveryPose()
        {
            MapArtRule.ForceProvisional = true;
            SlothRig rig = SlothRig.Create(_space, _art.SlothRigArt(), 72f)!;
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.Visible, Is.False, "nace oculto: sale cuando se pide una pose");
            var graphics = new List<Graphic>();
            rig.CollectGraphics(graphics);
            Assert.That(graphics.Count, Is.EqualTo(SlothRig.BodyGraphics + SlothRig.LeafCount), "8 imágenes y las 6 hojas de la pieza hoja");
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, g.name + ": nunca bloquea un toque");
            }

            var rects = new List<Rect>();
            foreach (SlothPoseId pose in (SlothPoseId[])Enum.GetValues(typeof(SlothPoseId)))
            {
                rig.Place(180f, 300f);
                rig.Play(pose, false);
                Assert.That(rig.Visible, Is.True, SlothRig.IdOf(pose));
                Assert.That(rig.Pose, Is.EqualTo(pose));
                rig.Tick(400f);
                Assert.That(rig.ClockMs, Is.EqualTo(400f));
                int leaves = pose == SlothPoseId.Cheer ? SlothRig.CheerLeaves : (pose == SlothPoseId.CheerBig ? SlothRig.LeafCount : 0);
                Assert.That(rig.VisibleLeaves, Is.EqualTo(leaves), SlothRig.IdOf(pose) + ": hojas");
                rects.Clear();
                rig.CollectRects(_space, rects);
                Assert.That(rects.Count, Is.GreaterThanOrEqualTo(7), SlothRig.IdOf(pose));
                foreach (Rect r in rects)
                {
                    Assert.That(float.IsNaN(r.x) || float.IsNaN(r.width), Is.False);
                    Assert.That(Mathf.Abs(r.center.x - 180f), Is.LessThan(72f), SlothRig.IdOf(pose) + ": cerca de su rama");
                    Assert.That(Mathf.Abs(r.center.y - 300f), Is.LessThan(80f), SlothRig.IdOf(pose));
                }
            }

            rig.Play(SlothPoseId.Sleep, false);
            Assert.That(rig.Current.EyesClosed, Is.True);
            Assert.That(graphics[3].GetComponent<Image>().sprite, Is.SameAs(rig.Art!.TorsoBack), "en ovillo, el lomo");
            rig.Play(SlothPoseId.Idle, false);
            Assert.That(graphics[3].GetComponent<Image>().sprite, Is.SameAs(rig.Art!.Torso), "colgado, el vientre");
            rig.Play(SlothPoseId.Idle, true);
            Assert.That(rig.Alpha, Is.EqualTo(0f), "Reducir movimiento: entra con un fundido");
            rig.Tick(SlothRig.ReducedMotionFadeMs);
            Assert.That(rig.Alpha, Is.EqualTo(1f));
        }

        [Test]
        public void TheHead_IsAboutAThirdOfTheHangingHeight_AndTheBranchComesWithIt()
        {
            MapArtRule.ForceProvisional = true;
            SlothRig rig = SlothRig.Create(_space, _art.SlothRigArt(), 120f)!;
            rig.Place(180f, 200f);
            rig.Play(SlothPoseId.Idle, false);
            var graphics = new List<Graphic>();
            rig.CollectGraphics(graphics);
            Assert.That(graphics[0].name, Is.EqualTo("Branch"));
            Assert.That(graphics[0].enabled, Is.True, "su rama entra con él (c.4)");

            // Medido en el rig: las cajas de la rama, las 4 extremidades, el torso y la cabeza (el párpado y las hojas, ocultos).
            var rects = new List<Rect>();
            rig.CollectRects(_space, rects);
            Assert.That(rects.Count, Is.EqualTo(7), "rama, 4 extremidades, torso y cabeza");
            float top = float.MaxValue;
            float bottom = float.MinValue;
            for (int i = 1; i < rects.Count; i++)
            {
                top = Mathf.Min(top, rects[i].yMin);
                bottom = Mathf.Max(bottom, rects[i].yMax);
            }

            float hanging = bottom - top; // de las garras en la rama al fondo del cuerpo
            Assert.That(hanging, Is.InRange(0.85f * rig.Size, 1.1f * rig.Size), "S es el alto colgado");
            Rect head = rects[6];
            Assert.That(head.height, Is.EqualTo(rig.Art!.HeadS * rig.Size).Within(0.5f), "la caja de la cabeza");
            float skull = head.height * ProceduralArt.SlothSkullFraction; // el cráneo dibujado, sin los mechones
            Assert.That(skull / hanging, Is.InRange(0.3f, 0.4f), "cabeza ≈ 1/3 del alto colgado (c.5, punto 1)");
        }

        [Test]
        public void TheSpeculum_IsInSight_WhenTheBackShows([Values(false, true)] bool reduceMotion)
        {
            // c.5, puntos 6 y 9: en ovillo (de espaldas) el espéculo del macho no lo tapa la cabeza ni las extremidades de
            // delante, que se pintan encima del torso; en la media vuelta de sloth_cheer_big, su centro queda a la vista.
            MapArtRule.ForceProvisional = true;
            SlothArt art = _art.SlothRigArt()!;
            float headR = art.HeadS * ProceduralArt.SlothSkullFraction * 0.5f * 1.12f; // con los mechones y el contorno
            float limbR = art.LimbWidthS * 0.5f;
            for (float ms = 0f; ms <= 4000f; ms += 500f)
            {
                SlothPose sleep = SlothRig.Evaluate(SlothPoseId.Sleep, ms, reduceMotion);
                Assert.That(sleep.TorsoBack, Is.True);
                Rect spec = SpeculumBox(sleep);
                string at = "sloth_sleep a " + ms + " ms";
                Assert.That(DistanceTo(spec, sleep.HeadCenter), Is.GreaterThan(headR), at + ": la cabeza no tapa el espéculo");
                Assert.That(SegmentDistance(spec, sleep.FrontArmFrom, sleep.FrontArmTo), Is.GreaterThan(limbR), at + ": ni el brazo de delante");
                Assert.That(SegmentDistance(spec, sleep.FrontLegFrom, sleep.FrontLegTo), Is.GreaterThan(limbR), at + ": ni la pierna de delante");
            }

            if (!reduceMotion)
            {
                SlothPose turn = SlothRig.Evaluate(SlothPoseId.CheerBig, 900f, false);
                Assert.That(turn.TorsoBack, Is.True, "sloth_cheer_big a 900 ms: de espaldas");
                Assert.That(Vector2.Distance(SpeculumBox(turn).center, turn.HeadCenter), Is.GreaterThan(headR), "con el espéculo a la vista");
            }

            // Y el rig pinta el lomo en esa pose.
            SlothRig rig = SlothRig.Create(_space, art, 96f)!;
            rig.Play(SlothPoseId.Sleep, reduceMotion);
            var graphics = new List<Graphic>();
            rig.CollectGraphics(graphics);
            Assert.That(((Image)graphics[3]).sprite, Is.SameAs(art.TorsoBack));
        }

        [Test]
        public void CheerBig_TheLeavesFrameTheSloth_NeverCoveringTheFaceNorTheSpeculum()
        {
            // c.5, puntos 2, 6 y 9: la media vuelta existe para enseñar el lomo con el espéculo; las hojas, que se pintan delante
            // de todo, no tapan la cabeza (su caja, ya reflejada con el cuerpo) ni el espéculo mientras se ve el lomo.
            MapArtRule.ForceProvisional = true;
            SlothArt art = _art.SlothRigArt()!;
            int leaves = 0;
            int back = 0;
            float leftmost = 0f;
            for (float ms = SlothRig.CheerEntryMs; ms <= SlothRig.CheerEntryMs + SlothRig.CheerLoopMs; ms += 5f)
            {
                SlothPose p = SlothRig.Evaluate(SlothPoseId.CheerBig, ms, false);
                Assert.That(p.Leaves, Is.EqualTo(SlothRig.LeafCount));
                Rect head = HeadBoxInRoot(p, art.HeadS);
                Rect spec = SpeculumBoxInRoot(p);
                if (p.TorsoBack)
                {
                    back++;
                }

                for (int i = 0; i < p.Leaves; i++)
                {
                    SlothRig.LeafAt(p, i, out Vector2 at, out float angle, out float alpha);
                    if (alpha <= 0.001f)
                    {
                        continue;
                    }

                    leaves++;
                    leftmost = Mathf.Min(leftmost, at.x);
                    Rect leaf = RotatedBox(at, new Vector2(art.LeafS, art.LeafS), angle);
                    string where = "sloth_cheer_big a " + ms + " ms, hoja " + i;
                    Assert.That(leaf.Overlaps(head), Is.False, where + ": no tapa la cara");
                    if (p.TorsoBack)
                    {
                        Assert.That(leaf.Overlaps(spec), Is.False, where + ": ni el espéculo");
                    }
                }
            }

            Assert.That(leaves, Is.GreaterThan(100), "se comprobaron hojas a la vista");
            Assert.That(back, Is.GreaterThan(20), "y fotogramas de espaldas");
            Assert.That(leftmost, Is.GreaterThan(-0.9f), "sin irse lejos de su rama");

            // Fuera de la vuelta y en sloth_cheer, las columnas de siempre.
            SlothPose cheer = SlothRig.Evaluate(SlothPoseId.Cheer, 700f, false);
            for (int i = 0; i < SlothRig.CheerLeaves; i++)
            {
                SlothRig.LeafAt(cheer, i, out Vector2 a, out _, out _);
                SlothRig.LeafAt(i, cheer.LeafPhase, out Vector2 b, out _, out _);
                Assert.That(a, Is.EqualTo(b), "sloth_cheer: hoja " + i + " sin cambios");
            }
        }

        [Test]
        public void CheerBig_AtTheHalfTurnCapture_NoLeafRectTouchesTheHeadNorTheSpeculum([Values(96f, 79f)] float sizeDp)
        {
            // El fotograma de la captura (900 ms, BodySpin = −1) con el rig de verdad: cajas de las piezas en el lienzo.
            MapArtRule.ForceProvisional = true;
            SlothRig rig = SlothRig.Create(_space, _art.SlothRigArt(), sizeDp)!;
            rig.Place(180f, 300f);
            rig.Play(SlothPoseId.CheerBig, false);
            rig.Seek(900f);
            Assert.That(rig.Current.TorsoBack, Is.True, "de espaldas");
            var graphics = new List<Graphic>();
            rig.CollectGraphics(graphics);
            Rect head = CanvasRect(graphics[6].rectTransform);
            Rect torso = CanvasRect(graphics[3].rectTransform);
            Vector2 uv = ProceduralArt.SlothSpeculumCenterUv;
            Vector2 specSize = Vector2.Scale(ProceduralArt.SlothSpeculumSizeUv, torso.size);
            var specCenter = new Vector2(torso.center.x + ((uv.x - 0.5f) * torso.width), torso.center.y - ((uv.y - 0.5f) * torso.height));
            var spec = new Rect(specCenter - (specSize * 0.5f), specSize);
            int shown = 0;
            for (int i = SlothRig.BodyGraphics; i < graphics.Count; i++)
            {
                if (!graphics[i].enabled)
                {
                    continue;
                }

                shown++;
                Rect leaf = CanvasRect(graphics[i].rectTransform);
                Assert.That(leaf.Overlaps(head), Is.False, graphics[i].name + ": no tapa la cara");
                Assert.That(leaf.Overlaps(spec), Is.False, graphics[i].name + ": ni el espéculo");
            }

            Assert.That(shown, Is.GreaterThanOrEqualTo(4), "con sus hojas a la vista");
        }

        [Test]
        public void EvaluateAndTick_DoNotAllocate()
        {
            MapArtRule.ForceProvisional = true;
            SlothRig rig = SlothRig.Create(_space, _art.SlothRigArt(), 72f)!;
            rig.Place(180f, 300f);
            rig.Play(SlothPoseId.CheerBig, false);
            for (int i = 0; i < 300; i++)
            {
                rig.Tick(16f); // dos bucles enteros: las hojas ya entraron y salieron
            }

            TestDelegate evaluate = () =>
            {
                SlothPose p = SlothRig.Evaluate(SlothPoseId.CheerBig, 700f, false);
                SlothRig.LeafAt(3, 0.4f, out Vector2 at, out float angle, out float alpha);
                SlothRig.LeafAt(p, 2, out Vector2 at2, out _, out _);
                s_sink = p.HeadAngle + at.x + angle + alpha + at2.x;
            };
            Assert.That(evaluate, Is.Not.AllocatingGCMemory(), "las poses no asignan");
            TestDelegate tick = () => rig.Tick(16f);
            Assert.That(tick, Is.Not.AllocatingGCMemory(), "el fotograma del rig no asigna");
        }

        private static void AssertInBox(Vector2 v, string what)
        {
            Assert.That(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y), Is.False, what);
            Assert.That(v.x, Is.InRange(-0.8f, 0.8f), what + ": x");
            Assert.That(v.y, Is.InRange(-1.0f, 0.6f), what + ": y");
        }

        // Caja del espéculo en S (alineada a los ejes, la del rectángulo girado con el torso): el centro del torso más el desfase
        // de ProceduralArt.SlothSpeculumCenterUv escalado al torso.
        private static Rect SpeculumBox(in SlothPose p)
        {
            Vector2 uv = ProceduralArt.SlothSpeculumCenterUv;
            Vector2 half = Vector2.Scale(ProceduralArt.SlothSpeculumSizeUv, p.TorsoSize) * 0.5f;
            Quaternion r = Quaternion.Euler(0f, 0f, p.TorsoAngle);
            Vector2 offset = r * new Vector2((uv.x - 0.5f) * p.TorsoSize.x, (uv.y - 0.5f) * p.TorsoSize.y);
            float a = p.TorsoAngle * Mathf.Deg2Rad;
            float hx = (half.x * Mathf.Abs(Mathf.Cos(a))) + (half.y * Mathf.Abs(Mathf.Sin(a)));
            float hy = (half.x * Mathf.Abs(Mathf.Sin(a))) + (half.y * Mathf.Abs(Mathf.Cos(a)));
            Vector2 c = p.TorsoCenter + offset;
            return Rect.MinMaxRect(c.x - hx, c.y - hy, c.x + hx, c.y + hy);
        }

        private static float DistanceTo(Rect box, Vector2 point)
        {
            var nearest = new Vector2(Mathf.Clamp(point.x, box.xMin, box.xMax), Mathf.Clamp(point.y, box.yMin, box.yMax));
            return Vector2.Distance(nearest, point);
        }

        // Distancia de la caja al eje de una extremidad (de la articulación a la garra), muestreado.
        private static float SegmentDistance(Rect box, Vector2 from, Vector2 to)
        {
            float best = float.MaxValue;
            for (int i = 0; i <= 20; i++)
            {
                best = Mathf.Min(best, DistanceTo(box, Vector2.Lerp(from, to, i / 20f)));
            }

            return best;
        }

        // Un punto del cuerpo en la raíz del rig: T(p) = P + R · E · (p − P), recalculado aquí sin el código del rig.
        private static Vector2 ToRoot(in SlothPose p, Vector2 local)
        {
            var d = new Vector2(p.BodySpin * (local.x - p.BodyPivot.x), local.y - p.BodyPivot.y);
            return p.BodyPivot + (Vector2)(Quaternion.Euler(0f, 0f, p.BodyAngle) * d);
        }

        // Caja (en la raíz, S) de un rectángulo del cuerpo de centro c, mitades half y giro angle (grados), por sus esquinas.
        private static Rect BodyBoxInRoot(in SlothPose p, Vector2 c, Vector2 half, float angle)
        {
            Quaternion r = Quaternion.Euler(0f, 0f, angle);
            float x0 = float.MaxValue;
            float x1 = float.MinValue;
            float y0 = float.MaxValue;
            float y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector2(i % 2 == 0 ? -half.x : half.x, i < 2 ? -half.y : half.y);
                Vector2 q = ToRoot(p, c + (Vector2)(r * corner));
                x0 = Mathf.Min(x0, q.x);
                x1 = Mathf.Max(x1, q.x);
                y0 = Mathf.Min(y0, q.y);
                y1 = Mathf.Max(y1, q.y);
            }

            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        // La pieza entera de la cabeza (HeadS de lado, estrechada con HeadTurn), ya reflejada y girada con el cuerpo.
        private static Rect HeadBoxInRoot(in SlothPose p, float headS)
        {
            float k = 1f - (0.4f * Mathf.Clamp01(p.HeadTurn));
            return BodyBoxInRoot(p, p.HeadCenter, new Vector2(0.5f * headS * k, 0.5f * headS), p.HeadAngle);
        }

        // El espéculo en el lomo, ya reflejado y girado con el cuerpo.
        private static Rect SpeculumBoxInRoot(in SlothPose p)
        {
            Vector2 uv = ProceduralArt.SlothSpeculumCenterUv;
            Quaternion r = Quaternion.Euler(0f, 0f, p.TorsoAngle);
            Vector2 c = p.TorsoCenter + (Vector2)(r * new Vector2((uv.x - 0.5f) * p.TorsoSize.x, (uv.y - 0.5f) * p.TorsoSize.y));
            Vector2 half = Vector2.Scale(ProceduralArt.SlothSpeculumSizeUv, p.TorsoSize) * 0.5f;
            return BodyBoxInRoot(p, c, half, p.TorsoAngle);
        }

        // Caja alineada de un cuadrado girado (las hojas, en la raíz).
        private static Rect RotatedBox(Vector2 c, Vector2 size, float angle)
        {
            float a = angle * Mathf.Deg2Rad;
            float hx = 0.5f * ((size.x * Mathf.Abs(Mathf.Cos(a))) + (size.y * Mathf.Abs(Mathf.Sin(a))));
            float hy = 0.5f * ((size.x * Mathf.Abs(Mathf.Sin(a))) + (size.y * Mathf.Abs(Mathf.Cos(a))));
            return Rect.MinMaxRect(c.x - hx, c.y - hy, c.x + hx, c.y + hy);
        }

        // Caja de una pieza en dp del lienzo (y desde arriba), como SlothRig.CollectRects.
        private Rect CanvasRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Rect area = _space.rect;
            float x0 = float.MaxValue;
            float x1 = float.MinValue;
            float y0 = float.MaxValue;
            float y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 l = _space.InverseTransformPoint(corners[i]);
                x0 = Mathf.Min(x0, l.x - area.xMin);
                x1 = Mathf.Max(x1, l.x - area.xMin);
                y0 = Mathf.Min(y0, area.yMax - l.y);
                y1 = Mathf.Max(y1, area.yMax - l.y);
            }

            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        private static int SceneRectTransforms()
        {
            return Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        }

        private Sprite MakePng()
        {
            _pngTexture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            _png = Sprite.Create(_pngTexture, new Rect(0, 0, 1024, 1024), new Vector2(0.5f, 0.5f), 300f);
            return _png;
        }

        // Un PNG de arbol_embauba (1024 × 256).
        private Sprite MakeBranchPng()
        {
            _branchTexture = new Texture2D(1024, 256, TextureFormat.RGBA32, false);
            _branchPng = Sprite.Create(_branchTexture, new Rect(0, 0, 1024, 256), new Vector2(0.5f, 0.5f), 300f);
            return _branchPng;
        }
    }
}
