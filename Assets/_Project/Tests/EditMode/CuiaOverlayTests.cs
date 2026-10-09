#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation;
using AF.Presentation.Mechanics;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Contrato de overlays por cuia (arquitectura del paso 1 §5; 07 §4.3 «Bloqueada por mecánica»; 02 §9.1): la fábrica,
    /// las anclas de <see cref="CuiaView"/>, el overlay perezoso que se oculta al abrirse la mecánica y que no se repinta
    /// mientras anima. <see cref="CuiaView"/> en un Canvas de prueba; el arte procedural se crea una vez (≈ 300 ms).
    /// </summary>
    public sealed class CuiaOverlayTests
    {
        private static readonly MechanicType[] Types =
        {
            MechanicType.Hoja, MechanicType.Brote, MechanicType.Liana, MechanicType.Matapi, MechanicType.Remolino,
            MechanicType.Lluvia, MechanicType.Pirana,
        };

        private ProceduralArt _art = null!;
        private GameObject _canvas = null!;

        [OneTimeSetUp]
        public void CreateArt()
        {
            _art = new ProceduralArt("white");
            _canvas = new GameObject("CuiaOverlayCanvas", typeof(RectTransform), typeof(Canvas));
        }

        [OneTimeTearDown]
        public void DisposeArt()
        {
            Object.DestroyImmediate(_canvas);
            _art.Dispose();
        }

        [TearDown]
        public void DestroyViews()
        {
            Transform root = _canvas.transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        [Test]
        public void Factory_OneOverlayPerMechanic_NoneIsNull()
        {
            Assert.That(CuiaOverlays.Create(MechanicType.None), Is.Null);
            for (int i = 0; i < Types.Length; i++)
            {
                CuiaOverlay? overlay = CuiaOverlays.Create(Types[i]);
                Assert.That(overlay, Is.Not.Null, Types[i].ToString());
                Assert.That(overlay!.Type, Is.EqualTo(Types[i]));
                Assert.That(overlay.IsAnimating, Is.False, "sin pasos animados en la ola 1");
                Assert.That(overlay.StepDurationMs(MechanicStepKind.Open), Is.EqualTo(0f));
            }
        }

        [Test]
        public void StepKinds_AreTheContract_WithoutCaveReceive()
        {
            string[] expected = { "Reveal", "MudWash", "Receive", "Open", "Seal", "Release", "RainDrop", "RainWait", "RainEnd", "Bite", "BiteLost" };
            Assert.That(Enum.GetNames(typeof(MechanicStepKind)), Is.EqualTo(expected), "el mordisco lo ejecuta la cueva: no hay CaveReceive");
        }

        [Test]
        public void Anchors_InContractOrder()
        {
            CuiaView view = NewView();
            CuiaOverlayHost host = view.OverlayHost;
            Assert.That(host.Index, Is.EqualTo(0));
            Assert.That(host.Capacity, Is.EqualTo(4));
            Assert.That(host.Board, Is.Null, "el tablero lo cablea la cola de pasos (ola 2)");
            Assert.That(host.BoardBackLayer, Is.Null);
            Assert.That(host.BoardFrontLayer, Is.Null);
            Assert.That(host.SlotBack.parent, Is.SameAs(view.Root));
            Assert.That(host.SlotFront.parent, Is.SameAs(view.Root));
            Assert.That(host.SlotBack.GetSiblingIndex(), Is.LessThan(view.Body.GetSiblingIndex()), "SlotBack, antes del cuerpo");
            Assert.That(host.SlotFront.GetSiblingIndex(), Is.GreaterThan(view.Body.GetSiblingIndex()), "SlotFront, tras el cuerpo");
            Assert.That(host.BodyBack.parent, Is.SameAs(view.Body));
            Assert.That(host.BodyFront.parent, Is.SameAs(view.Body));
            Assert.That(Sibling(view, "GlassBack"), Is.LessThan(host.BodyBack.GetSiblingIndex()));
            Assert.That(host.BodyBack.GetSiblingIndex(), Is.LessThan(view.Liquid.transform.GetSiblingIndex()), "BodyBack, entre el vidrio trasero y el líquido");
            Assert.That(Sibling(view, "DoseMarks"), Is.LessThan(host.BodyFront.GetSiblingIndex()));
            Assert.That(host.BodyFront.GetSiblingIndex(), Is.LessThan(Sibling(view, "SealCap")), "BodyFront, tras las marcas y antes de la tapa");
            Assert.That(view.Murky.Root.parent, Is.SameAs(view.Body), "las marcas cuelgan del cuerpo");
            Assert.That(view.Murky.Root.GetSiblingIndex(), Is.GreaterThan(view.Liquid.transform.GetSiblingIndex()), "encima del líquido");
        }

        [Test]
        public void Overlay_PerMechanicType_ShownWhenClosed()
        {
            for (int i = 0; i < Types.Length; i++)
            {
                CuiaView view = NewView();
                view.ShowContent(Sample(Types[i]), false);
                CuiaOverlay? overlay = view.Overlay;
                Assert.That(overlay, Is.Not.Null, Types[i].ToString());
                Assert.That(overlay!.Type, Is.EqualTo(Types[i]));
                Assert.That(overlay.GetType(), Is.EqualTo(CuiaOverlays.Create(Types[i])!.GetType()));
                Assert.That(overlay.AttachedHost, Is.SameAs(view.OverlayHost));
                Assert.That(overlay.Visible, Is.True, Types[i] + " cerrada: su overlay se ve");
            }
        }

        [Test]
        public void Overlay_HiddenAfterOpen()
        {
            for (int i = 0; i < Types.Length; i++)
            {
                CuiaView view = NewView();
                Cuia closed = Sample(Types[i]);
                view.ShowContent(closed, false);
                view.ShowContent(closed.WithoutMechanic(), false);
                Assert.That(view.Overlay!.Visible, Is.False, Types[i] + " abierta: su overlay se oculta");
                view.ShowContent(closed, false);
                Assert.That(view.Overlay.Visible, Is.True, Types[i] + ": Marea la devuelve");
            }
        }

        [Test]
        public void ShowContent_SkipsOverlayWhileAnimating()
        {
            CuiaView view = NewView();
            var probe = new ProbeOverlay();
            view.UseOverlay(probe);
            Assert.That(probe.Attached, Is.True);
            Assert.That(view.Overlay, Is.SameAs(probe));
            Cuia hoja = Sample(MechanicType.Hoja);
            view.ShowContent(hoja, false);
            Assert.That(probe.Shows, Is.EqualTo(1));

            probe.Animating = true;
            Cuia opened = Cuia.Create(4, new[] { 4 });
            view.ShowContent(opened, false);
            Assert.That(probe.Shows, Is.EqualTo(1), "mientras anima, ShowContent no lo repinta");
            Assert.That(view.LiquidLevel, Is.EqualTo(1f).Within(0.001f), "pero sí pinta el líquido");
            Assert.That(view.ShownCuia, Is.EqualTo(opened));

            probe.Animating = false;
            view.ShowContent(opened, false);
            Assert.That(probe.Shows, Is.EqualTo(2), "el ShowContent del final del paso cierra");
            Assert.That(probe.Visible, Is.False);
            Assert.That(() => view.UseOverlay(new ProbeOverlay()), Throws.InvalidOperationException, "un overlay por mecánica");
        }

        // Una cuia válida y cerrada de cada mecánica (02 §9.1; D-007).
        internal static Cuia Sample(MechanicType type)
        {
            switch (type)
            {
                case MechanicType.Hoja:
                    return Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 4, flags: Cuia.FlagClosed);
                case MechanicType.Brote:
                    return Cuia.Create(4, new[] { 3, 5 }).WithMechanic(MechanicType.Brote, flags: Cuia.FlagClosed);
                case MechanicType.Liana:
                    return Cuia.Create(4, new[] { 1, 2 }).WithMechanic(MechanicType.Liana, key: 8);
                case MechanicType.Matapi:
                    return Cuia.Create(4, new[] { 6 }).WithMechanic(MechanicType.Matapi);
                case MechanicType.Remolino:
                    return Cuia.Create(4, new[] { 7 }).WithMechanic(MechanicType.Remolino, pairId: 1);
                case MechanicType.Lluvia:
                    return Cuia.Create(4, new[] { 10, 10 }).WithMechanic(MechanicType.Lluvia, key: 10, n: 2, k: 3, counter: 1);
                case MechanicType.Pirana:
                    return Cuia.Create(4, new[] { 2 }).WithMechanic(MechanicType.Pirana, k: 4, counter: 2);
                default:
                    return Cuia.Empty(4);
            }
        }

        private static int Sibling(CuiaView view, string name)
        {
            Transform? child = view.Body.Find(name);
            Assert.That(child, Is.Not.Null, name);
            return child!.GetSiblingIndex();
        }

        private CuiaView NewView(int capacity = 4)
        {
            return CuiaView.Create((RectTransform)_canvas.transform, 0, capacity, _art, _ => { }, (_, __) => { });
        }

        // Overlay de prueba: cuenta los Show y anima cuando se le pide.
        private sealed class ProbeOverlay : CuiaOverlay
        {
            public bool Animating;

            public int Shows;

            public bool Attached;

            public override MechanicType Type => MechanicType.Hoja;

            public override bool IsAnimating => Animating;

            public override void Show(Cuia cuia, bool sealedNow)
            {
                Shows++;
                Visible = cuia.Mechanic == Type;
            }

            protected override void OnAttach()
            {
                Attached = true;
            }
        }
    }
}
