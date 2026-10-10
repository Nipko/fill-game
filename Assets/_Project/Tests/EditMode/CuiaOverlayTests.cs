#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Session;
using AF.Presentation;
using AF.Presentation.Mechanics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Contrato de overlays por cuia (arquitectura del paso 1 §5; 07 §4.3 «Bloqueada por mecánica»; 02 §9.1; propuesta de
    /// D-007 b.2): la fábrica, las anclas de <see cref="CuiaView"/>, el overlay perezoso que se ve con la mecánica cerrada, se
    /// oculta al abrirse, no cambia al seleccionar (D-002) y no se repinta mientras anima; los contadores de Lluvia y Piraña
    /// iguales a <see cref="LevelCounter.Of"/>; y los colores de las superficies de más de 2 dp a ΔE00 ≥ 10 de los 14 tintes
    /// (direccion-visual-v2 §2.4, regla 1; 07 §3, regla 6), que sigue valiendo cuando cada mecánica reescriba su overlay.
    /// <see cref="CuiaView"/> en un Canvas de prueba (con <see cref="BoardBuilder"/> para los contadores); el arte procedural
    /// se crea una vez (≈ 300 ms en EditMode). Desde T07 la vista solo crea overlays con tablero (<see cref="CuiaView.BindBoard"/>):
    /// cada vista suelta se une a un <see cref="SoloBoard"/> con sus dos capas, como hace <c>BoardAnimator.Bind</c> en el juego.
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
                Assert.That(overlay.IsAnimating, Is.False, "recién creado, sin paso en curso");
                float open = Types[i] == MechanicType.Hoja ? HojaOverlay.OpenStepMs : (Types[i] == MechanicType.Brote ? BroteOverlay.OpenStepMs : 0f);
                Assert.That(overlay.StepDurationMs(MechanicStepKind.Open), Is.EqualTo(open), "solo la Hoja y el Brote tienen paso Open propio (T11)");
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
            Assert.That(host.Board, Is.Not.Null, "el tablero lo cablea BindBoard (T07)");
            Assert.That(host.BoardBackLayer, Is.Not.Null);
            Assert.That(host.BoardFrontLayer, Is.Not.Null);
            CuiaView loose = NewView(4, false);
            Assert.That(loose.OverlayHost.Board, Is.Null, "sin BindBoard, sin tablero");
            loose.ShowContent(Sample(MechanicType.Hoja), false);
            Assert.That(loose.Overlay, Is.Null, "sin tablero no se crea ningún overlay");
            BindToSoloBoard(loose);
            Assert.That(loose.Overlay, Is.Not.Null, "se crea en BindBoard, con el último estado mostrado");
            Assert.That(loose.Overlay!.Visible, Is.True);
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
                Assert.That(VisibleGraphics(view.OverlayHost), Is.GreaterThan(0), Types[i] + ": alguna pieza a la vista");
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
                Assert.That(VisibleGraphics(view.OverlayHost), Is.EqualTo(0), Types[i] + " abierta: ninguna pieza a la vista");
                view.ShowContent(closed, false);
                Assert.That(view.Overlay.Visible, Is.True, Types[i] + ": Marea la devuelve");
            }
        }

        [Test]
        public void Overlay_UnchangedOnLift_D002()
        {
            for (int i = 0; i < Types.Length; i++)
            {
                CuiaView view = NewView();
                view.ShowContent(Sample(Types[i]), false);
                view.Tick(16f);
                string before = Signature(view);
                view.SetLifted(true);
                for (int f = 0; f < 20; f++)
                {
                    view.Tick(16f);
                }

                Assert.That(view.IsLifted, Is.True);
                Assert.That(Signature(view), Is.EqualTo(before), Types[i] + ": nada del overlay cambia al seleccionar (D-002)");
            }
        }

        [Test]
        public void Lluvia_PipsMatchLevelCounter() // 02 §9.1 M6; Q18 B; LevelCounter.Of
        {
            BoardBuildResult built = BoardBuilder.Build(
                new[] { 4, 4, 4 },
                new[] { new[] { 10, 10 }, new[] { 1, 1, 1, 1 }, Array.Empty<int>() },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 2, k: 3), MechanicSpec.None, MechanicSpec.None });
            Assert.That(built.Error, Is.Null);
            BoardState start = built.State!;
            BoardState[] states =
            {
                start,
                With(start, 0, start[0].WithMechanic(MechanicType.Lluvia, key: 10, n: 2, k: 3, counter: 1)),
                With(start, 0, start[0].WithMechanic(MechanicType.Lluvia, key: 10, n: 2, k: 3, counter: 2)),
                With(start, 0, Cuia.Create(4, new[] { 10, 10, 10 }).WithMechanic(MechanicType.Lluvia, key: 10, n: 1, k: 3)),
                With(start, 0, Cuia.Create(4, new[] { 10, 10, 10, 1 }).WithMechanic(MechanicType.Lluvia, key: 10, n: 1, k: 3, counter: 2)),
            };

            CuiaView view = NewView();
            for (int s = 0; s < states.Length; s++)
            {
                LevelCounter counter = LevelCounter.Of(states[s]);
                Assert.That(counter.Type, Is.EqualTo(MechanicType.Lluvia));
                view.ShowContent(states[s][counter.CuiaIndex], false);
                var rain = (LluviaOverlay)view.Overlay!;
                Assert.That(rain.PipCount, Is.EqualTo(counter.Rhythm), "estado " + s + ": un punto por vertido del ritmo k");
                Assert.That(rain.FilledPips, Is.EqualTo(counter.Elapsed), "estado " + s + ": llenos los vertidos útiles contados");
                Assert.That(rain.DropCount, Is.EqualTo(counter.PendingDrops), "estado " + s + ": una gotita por gota pendiente");
            }

            view.ShowContent(states[0][0].WithoutMechanic(), false);
            Assert.That(view.Overlay!.Visible, Is.False, "con la última gota se va la nube");
        }

        [Test]
        public void Lluvia_CloudStaysInsideThe32dpTouchArea([Values(48f, 64f)] float cuiaWidth) // 02 §3.3
        {
            CuiaView view = NewView();
            view.SetGeometry(new Vector2(100f, -300f), cuiaWidth, cuiaWidth + 16f);
            view.ShowContent(Sample(MechanicType.Lluvia), false);
            var rain = (LluviaOverlay)view.Overlay!;
            Assert.That(rain.CloudTopAboveMouthDp, Is.LessThanOrEqualTo(CuiaView.TouchHeadroomDp));
            Assert.That(rain.CloudTopAboveMouthDp, Is.GreaterThan(0f));
        }

        [Test]
        public void Pirana_PipsMatchLevelCounter() // 02 §9.1 M7; Q18 B; LevelCounter.Of
        {
            BoardState start = PiranaBoard();
            CuiaView view = NewView();
            for (int c = 0; c < 4; c++)
            {
                BoardState state = With(start, 0, start[0].WithMechanic(MechanicType.Pirana, k: 4, counter: c));
                LevelCounter counter = LevelCounter.Of(state);
                view.ShowContent(state[counter.CuiaIndex], false);
                var cave = (PiranaOverlay)view.Overlay!;
                Assert.That(cave.PipCount, Is.EqualTo(counter.Rhythm), "contador " + c);
                Assert.That(cave.FilledPips, Is.EqualTo(counter.Elapsed), "contador " + c);
            }
        }

        [Test]
        public void Pirana_DimmedWhenCaveFull() // respuesta Q-C del dueño: con la cueva llena el mordisco se perdería
        {
            BoardState start = PiranaBoard();
            CuiaView view = NewView();
            view.ShowContent(start[0], false);
            var cave = (PiranaOverlay)view.Overlay!;
            Assert.That(LevelCounter.Of(start).Blocked, Is.False);
            Assert.That(cave.Dimmed, Is.False);
            Assert.That(cave.StonesAlpha, Is.EqualTo(1f));

            BoardState full = With(start, 0, Cuia.Create(4, new[] { 2, 3, 2, 3 }).WithMechanic(MechanicType.Pirana, k: 4, counter: 1));
            Assert.That(LevelCounter.Of(full).Blocked, Is.True);
            view.ShowContent(full[0], false);
            Assert.That(cave.Dimmed, Is.True, "cueva llena: atenuada");
            Assert.That(cave.StonesAlpha, Is.EqualTo(PiranaOverlay.DimAlpha).Within(0.001f));

            view.ShowContent(start[0], false);
            Assert.That(cave.Dimmed, Is.False, "con hueco, de nuevo entera");
        }

        [Test]
        public void CounterPips_EmptyRingLegibleAt48dp([Values(MechanicType.Lluvia, MechanicType.Pirana)] MechanicType type) // 02 §5: 48 dp con 6 cuias o más
        {
            // Lo que distingue un vertido contado de uno pendiente es el aro vacío: su trazo y su hueco, de 1 dp como mínimo
            // en la cuia más estrecha.
            CuiaView view = NewView();
            view.SetGeometry(new Vector2(100f, -300f), 48f, 64f);
            view.ShowContent(Sample(type), false);
            int empties = 0;
            CuiaOverlayHost host = view.OverlayHost;
            Transform[] anchors = { host.SlotBack, host.BodyBack, host.BodyFront, host.SlotFront };
            for (int a = 0; a < anchors.Length; a++)
            {
                Image[] images = anchors[a].GetComponentsInChildren<Image>(false);
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i].sprite != _art.CounterPipEmpty)
                    {
                        continue;
                    }

                    Vector2 size = images[i].rectTransform.sizeDelta;
                    float sideDp = Mathf.Min(size.x, size.y);
                    Assert.That(sideDp * ProceduralArt.CounterPipRingFraction, Is.GreaterThanOrEqualTo(1f), type + ": trazo del aro (dp) de " + images[i].name);
                    Assert.That(sideDp * ProceduralArt.CounterPipHoleFraction, Is.GreaterThanOrEqualTo(1f), type + ": hueco del aro (dp) de " + images[i].name);
                    empties++;
                }
            }

            Assert.That(empties, Is.GreaterThan(0), type + ": algún vertido pendiente a la vista");
        }

        [Test]
        public void SurfaceColors_AtLeast10DeltaE00FromEveryTint() // direccion-visual-v2 §2.4, regla 1; 07 §3, regla 6
        {
            var colors = new List<Color>();
            for (int i = 0; i < Types.Length; i++)
            {
                CuiaView view = NewView();
                view.ShowContent(Sample(Types[i]), false);
                colors.Clear();
                view.Overlay!.CollectSurfaceColors(colors);
                bool mustReport = Types[i] != MechanicType.Brote && Types[i] != MechanicType.Matapi;
                if (mustReport)
                {
                    Assert.That(colors.Count, Is.GreaterThan(0), Types[i] + ": hoja, liana, espiral, nube y piedras cuentan");
                }

                for (int c = 0; c < colors.Count; c++)
                {
                    for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
                    {
                        float de = ColorMath.DeltaE00(colors[c], Palette.Tint(tint));
                        Assert.That(de, Is.GreaterThanOrEqualTo(10f), Types[i] + ", color " + c + " frente al tinte " + tint);
                    }

                    if (Types[i] == MechanicType.Remolino)
                    {
                        var turquoise = new Color(Palette.IconTurquoise.r, Palette.IconTurquoise.g, Palette.IconTurquoise.b, 1f);
                        Assert.That(ColorMath.DeltaE00(colors[c], turquoise), Is.GreaterThanOrEqualTo(10f), "el Remolino nunca es turquesa");
                    }
                }
            }
        }

        [Test]
        public void Liana_FruitOrCountDots() // b.2 M3; Liana de cuenta (b.3)
        {
            CuiaView view = NewView();
            view.ShowContent(Sample(MechanicType.Liana), false);
            var liana = (LianaOverlay)view.Overlay!;
            Assert.That(liana.ShownKey, Is.EqualTo(8), "con fruta: la llave cuelga del nudo");
            Assert.That(liana.CountDots, Is.EqualTo(0));
            Assert.That(liana.WrapCount, Is.EqualTo(3));

            view.ShowContent(Cuia.Create(4, new[] { 1, 2 }).WithMechanic(MechanicType.Liana, n: 2), false);
            Assert.That(liana.ShownKey, Is.EqualTo(0));
            Assert.That(liana.CountDots, Is.EqualTo(2), "de cuenta: n puntos en el nudo");
        }

        [Test]
        public void Brote_BudTakesTheTopColor() // 02 §9.1 M2
        {
            CuiaView view = NewView();
            view.ShowContent(Sample(MechanicType.Brote), false);
            var brote = (BroteOverlay)view.Overlay!;
            Assert.That(brote.BudColor, Is.EqualTo(Palette.Tint(5)), "el capullo, del color de la cima");
        }

        [Test]
        public void Remolino_PairDots_OnlyFromPairOne()
        {
            CuiaView view = NewView();
            view.ShowContent(Cuia.Create(4, new[] { 7 }).WithMechanic(MechanicType.Remolino, pairId: 0), false);
            var whirl = (RemolinoOverlay)view.Overlay!;
            Assert.That(whirl.PairDots, Is.EqualTo(0), "la primera pareja, sin puntos");
            view.ShowContent(Cuia.Create(4, new[] { 7 }).WithMechanic(MechanicType.Remolino, pairId: 2), false);
            Assert.That(whirl.PairDots, Is.EqualTo(3), "id + 1 puntos");
        }

        [Test]
        public void InFlight_HidesWhatHangsFromTheBody_ButNotTheSlot()
        {
            var layer = new GameObject("PourLayer", typeof(RectTransform));
            layer.transform.SetParent(_canvas.transform, false);
            CuiaView rain = NewView();
            rain.ShowContent(Sample(MechanicType.Lluvia), false);
            rain.MoveBodyTo((RectTransform)layer.transform);
            Assert.That(VisibleGraphics(rain.OverlayHost), Is.EqualTo(0), "la nube no se inclina con la cuia");
            rain.ReturnBody();
            Assert.That(VisibleGraphics(rain.OverlayHost), Is.GreaterThan(0), "al volver, reaparece");

            CuiaView whirl = NewView();
            whirl.ShowContent(Sample(MechanicType.Remolino), false);
            int shown = VisibleGraphics(whirl.OverlayHost);
            whirl.MoveBodyTo((RectTransform)layer.transform);
            Assert.That(VisibleGraphics(whirl.OverlayHost), Is.EqualTo(shown), "el remanso se queda en la ranura");
            whirl.ReturnBody();
        }

        [Test]
        public void Tick_AndShowContent_DoNotAllocate() // 08 §5.4
        {
            var views = new CuiaView[Types.Length];
            var samples = new Cuia[Types.Length];
            for (int i = 0; i < Types.Length; i++)
            {
                views[i] = NewView();
                samples[i] = Sample(Types[i]);
                views[i].ShowContent(samples[i], false);
                views[i].Tick(16f);
            }

            Assert.That(() =>
            {
                for (int i = 0; i < views.Length; i++)
                {
                    views[i].Tick(16f);
                    views[i].ShowContent(samples[i], false);
                }
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void MechanicIcons_32dp_ForEveryType()
        {
            Assert.That(_art.MechanicIcon(MechanicType.None), Is.Null);
            for (int i = 0; i < Types.Length; i++)
            {
                Sprite? icon = _art.MechanicIcon(Types[i]);
                Assert.That(icon, Is.Not.Null, Types[i].ToString());
                Assert.That(icon!.rect.width * 100f / icon.pixelsPerUnit, Is.EqualTo(ProceduralArt.MechanicIconDp).Within(0.01f), "32 dp");
                Assert.That(_art.MechanicIcon(Types[i]), Is.SameAs(icon), "se genera una vez");
            }

            Assert.That(_art.MurkyIcon, Is.Not.Null);
            Assert.That(_art.MudIcon, Is.Not.Null);
            Assert.That(_art.CounterPip, Is.Not.Null);
            Assert.That(_art.DropIcon, Is.Not.Null);
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

        private static BoardState PiranaBoard()
        {
            BoardBuildResult built = BoardBuilder.Build(
                new[] { 4, 4, 4 },
                new[] { new[] { 2 }, new[] { 1, 1, 1 }, new[] { 2, 2, 2 } },
                new[] { new MechanicSpec(MechanicType.Pirana, k: 4), MechanicSpec.None, MechanicSpec.None });
            Assert.That(built.Error, Is.Null);
            return built.State!;
        }

        private static BoardState With(BoardState state, int index, Cuia cuia)
        {
            var cuias = new Cuia[state.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = i == index ? cuia : state[i];
            }

            return new BoardState(cuias);
        }

        // Piezas de overlay a la vista bajo las cuatro anclas.
        private static int VisibleGraphics(CuiaOverlayHost host)
        {
            return VisibleIn(host.SlotBack) + VisibleIn(host.BodyBack) + VisibleIn(host.BodyFront) + VisibleIn(host.SlotFront);
        }

        private static int VisibleIn(Transform anchor)
        {
            int count = 0;
            Graphic[] graphics = anchor.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].enabled && graphics[i].gameObject.activeInHierarchy && graphics[i].color.a > 0.001f)
                {
                    count++;
                }
            }

            return count;
        }

        // Estado visible de los overlays y de las marcas: activación, pose relativa a su ancla, color y sprite de cada pieza.
        private static string Signature(CuiaView view)
        {
            var sb = new StringBuilder(512);
            CuiaOverlayHost host = view.OverlayHost;
            Append(sb, host.SlotBack);
            Append(sb, host.BodyBack);
            Append(sb, host.BodyFront);
            Append(sb, host.SlotFront);
            Append(sb, view.Murky.Root);
            Append(sb, view.Mud.Root);
            return sb.ToString();
        }

        private static void Append(StringBuilder sb, Transform anchor)
        {
            Transform[] nodes = anchor.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform node = nodes[i];
                sb.Append(node.name).Append(node.gameObject.activeSelf ? '+' : '-');
                if (node is RectTransform rt)
                {
                    sb.Append(rt.anchoredPosition.ToString("F3")).Append(rt.sizeDelta.ToString("F3"));
                }

                sb.Append(node.localScale.ToString("F3")).Append(node.localEulerAngles.ToString("F3"));
                Graphic? graphic = node.GetComponent<Graphic>();
                if (graphic != null)
                {
                    sb.Append(graphic.enabled ? 'E' : 'D').Append(graphic.color.ToString("F3"));
                    if (graphic is Image image && image.sprite != null)
                    {
                        sb.Append(image.sprite.name);
                    }
                }

                sb.Append('|');
            }
        }

        private static int Sibling(CuiaView view, string name)
        {
            Transform? child = view.Body.Find(name);
            Assert.That(child, Is.Not.Null, name);
            return child!.GetSiblingIndex();
        }

        private CuiaView NewView(int capacity = 4, bool bind = true)
        {
            CuiaView view = CuiaView.Create((RectTransform)_canvas.transform, 0, capacity, _art, _ => { }, (_, __) => { });
            if (bind)
            {
                BindToSoloBoard(view);
            }

            return view;
        }

        // Une la vista a un tablero de una sola cuia con dos capas en el lienzo de prueba (como BoardAnimator.Bind).
        private void BindToSoloBoard(CuiaView view)
        {
            var back = new GameObject("MechanicBack", typeof(RectTransform));
            back.transform.SetParent(_canvas.transform, false);
            var front = new GameObject("MechanicFront", typeof(RectTransform));
            front.transform.SetParent(_canvas.transform, false);
            view.BindBoard(new SoloBoard(view), (RectTransform)back.transform, (RectTransform)front.transform);
        }

        // Tablero de una sola cuia para las vistas sueltas de estos tests.
        private sealed class SoloBoard : ICuiaBoard
        {
            private readonly CuiaView _view;

            public SoloBoard(CuiaView view)
            {
                _view = view;
            }

            public int Count => 1;

            public int LayoutVersion => 0;

            public Cuia ShownCuia(int i)
            {
                return _view.ShownCuia;
            }

            public Vector2 BodyCenter(int i)
            {
                return _view.FootPosition + new Vector2(0f, 0.5f * _view.HeightU * _view.Unit);
            }

            public Vector2 Foot(int i)
            {
                return _view.FootPosition;
            }

            public Rect SlotRect(int i)
            {
                return new Rect(_view.FootPosition, _view.Root.sizeDelta);
            }

            public void SetDose(int i, int dose, Color color, float fill, int pattern)
            {
                _view.SetDose(dose, color, fill, pattern);
            }

            public void ShowContent(int i, Cuia cuia, bool sealedNow)
            {
                _view.ShowContent(cuia, sealedNow);
            }
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
