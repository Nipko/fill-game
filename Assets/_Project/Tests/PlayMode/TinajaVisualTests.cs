#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La Tinaja y su cuia en el cliente (tarea TJ2; M13, D-019; 02 §5 y §9.1; 07 §4.3 y §4.5; propuesta de la Tinaja, b.2 «Qué
    /// se ve» y «Aspecto», y c.6; contrato 27): la vasija opaca con la boca (tinte e icono de la fruta de arriba), el chip «×n»,
    /// las bandas de las capas reveladas y, vacía, boca abajo; su cuia con el poste del jirau y el asa; el jirau bajo las dos
    /// ranuras. La tinaja <b>nunca se pinta como turbia</b>: ni el pardo #8A7B6A, ni el «?», ni la insignia «?×n», tampoco al
    /// revelar ni durante la intro. El arte provisional sale con <see cref="MapArtRule"/>; el estado, siempre. En el banco de
    /// <see cref="MechanicTestKit"/> con el 323 del paquete de muestra (su cuia en la 1 y la tinaja en la 2) y, en la escena,
    /// la pareja en la misma fila (02 §5) también cuando el reparto de D-006 la partiría.
    /// </summary>
    public sealed class TinajaVisualTests
    {
        internal const int CucharonIndex = 1;
        internal const int TinajaIndex = 2;

        private const int Urucum = 5; // índices de motor (glosario §9)
        private const int CamuCamu = 6;
        private const int Jenipapo = 9;
        private const int Lulo = 11;

        private static ProceduralArt? s_art;
        private MechanicTestKit.Rig? _rig;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        /// <summary>
        /// El tablero del 323 del paquete de muestra (pack_schema 3; <c>Fixtures/pack3_sample.json</c>): 10 cuias, su cuia vacía
        /// en la 1 y la tinaja en la 2 con [jenipapo, lulo, jenipapo], las dos de abajo ocultas.
        /// </summary>
        internal static BoardState Board323()
        {
            BoardBuildResult result = BoardBuilder.Build(Caps323(), Cuias323(), Attrs323(CucharonIndex, TinajaIndex), new[] { new[] { TinajaIndex, 0 }, new[] { TinajaIndex, 1 } });
            Assert.That(result.Error, Is.Null);
            Assert.That(LevelValidation.Validate(result.State!), Is.Null, "el 323, válido (M13)");
            return result.State!;
        }

        /// <summary>
        /// Paquete de prueba (pack_schema 3) con el tablero del 323, la pareja en <paramref name="cucharon"/> y
        /// <paramref name="tinaja"/> (contiguas) y las demás cuias en el orden del 323. Sin <c>intro_mechanic</c>: sin tarjeta.
        /// </summary>
        internal static string Pack323(int cucharon, int tinaja)
        {
            int[] caps = Caps323();
            int[][] cuias = Cuias323();
            var order = new List<int>();
            for (int i = 0; i < caps.Length; i++)
            {
                if (i != CucharonIndex && i != TinajaIndex)
                {
                    order.Add(i);
                }
            }

            var finalOrder = new int[caps.Length];
            int next = 0;
            for (int i = 0; i < caps.Length; i++)
            {
                finalOrder[i] = i == cucharon ? CucharonIndex : (i == tinaja ? TinajaIndex : order[next++]);
            }

            var capsSb = new StringBuilder();
            var cuiasSb = new StringBuilder();
            var attrsSb = new StringBuilder();
            for (int i = 0; i < caps.Length; i++)
            {
                string sep = i == 0 ? string.Empty : ", ";
                int from = finalOrder[i];
                capsSb.Append(sep).Append(caps[from].ToString(CultureInfo.InvariantCulture));
                cuiasSb.Append(sep).Append('[');
                for (int k = 0; k < cuias[from].Length; k++)
                {
                    cuiasSb.Append(k == 0 ? string.Empty : ", ").Append(cuias[from][k].ToString(CultureInfo.InvariantCulture));
                }

                cuiasSb.Append(']');
                attrsSb.Append(sep).Append(i == cucharon ? "{ \"type\": \"cucharon\" }" : (i == tinaja ? "{ \"type\": \"tinaja\" }" : "null"));
            }

            string t = tinaja.ToString(CultureInfo.InvariantCulture);
            return "{ \"pack_schema\": 3, \"pack_id\": \"test_m07_v1_n0323\", \"kind\": \"campaign\", \"world\": 7, \"world_id\": \"w07_madre_de_dios\", "
                + "\"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"m07_v1_n0323\", \"level\": 323, \"world\": 7, \"lap\": 1, "
                + "\"water_base\": \"white\", \"palette\": [\"urucum\", \"camu_camu\", \"bacaba\", \"jenipapo\", \"lulo\", \"cacao\"], \"units_per_color\": 4, "
                + "\"caps\": [" + capsSb + "], \"cuias\": [" + cuiasSb + "], \"attrs\": [" + attrsSb + "], "
                + "\"murky\": true, \"hidden\": [[" + t + ", 0], [" + t + ", 1]], \"difficulty\": { \"par\": 16 }, \"relief\": [] } ] }";
        }

        [UnityTest]
        public IEnumerator Factory_TinajaAndItsCuia_HaveTheirOverlays_WithoutStepsUntilTJ3()
        {
            yield return null;
            CuiaOverlay? tinaja = CuiaOverlays.Create(MechanicType.Tinaja);
            CuiaOverlay? cucharon = CuiaOverlays.Create(MechanicType.Cucharon);
            Assert.That(tinaja, Is.InstanceOf<TinajaOverlay>());
            Assert.That(cucharon, Is.InstanceOf<CucharonOverlay>());
            Assert.That(tinaja!.Type, Is.EqualTo(MechanicType.Tinaja));
            Assert.That(cucharon!.Type, Is.EqualTo(MechanicType.Cucharon));
            foreach (MechanicStepKind kind in System.Enum.GetValues(typeof(MechanicStepKind)))
            {
                Assert.That(tinaja.StepDurationMs(kind), Is.EqualTo(0f), "sin pasos propios hasta TJ3: " + kind);
                Assert.That(cucharon.StepDurationMs(kind), Is.EqualTo(0f), "sin pasos propios hasta TJ3: " + kind);
            }

            Assert.That(CuiaOverlays.Create(MechanicType.HojaAtada), Is.InstanceOf<HojaOverlay>(), "los casos de antes, igual");
            Assert.That(CuiaOverlays.Create(MechanicType.Pirana), Is.InstanceOf<PiranaOverlay>());
        }

        [UnityTest]
        public IEnumerator Tinaja_NeverPaintedAsMurkyWater_Contract27()
        {
            yield return null;
            BoardState board = Board323();
            _rig = new MechanicTestKit.Rig(Art, board);
            CuiaView view = _rig.Views[TinajaIndex];
            AssertNotMurky(view, board[TinajaIndex], "al cargar");

            // El revelado de Aguas turbias no corre en la tinaja: ni por la regla ni con la Luciérnaga (su boca la anima TJ3).
            Assert.That(view.PlayReveal(0b11, RevealStyle.Rule), Is.EqualTo(0f));
            Assert.That(view.PlayReveal(0b11, RevealStyle.Firefly), Is.EqualTo(0f));
            Assert.That(view.MarksAnimating, Is.False);
            AssertNotMurky(view, board[TinajaIndex], "tras pedir un revelado");

            // Sacar (la tinaja vierte en su cuia, R7 revela la de debajo) y Marea, con la cola real, fotograma a fotograma.
            BoardState after = _rig.Pour(board, TinajaIndex, CucharonIndex, out PourVisual _);
            Assert.That(after[TinajaIndex].Count, Is.EqualTo(2));
            Assert.That(after[TinajaIndex].IsHiddenAt(1), Is.False, "R7: la nueva boca, a la vista");
            for (int t = 0; t < 4000 && !_rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                Assert.That(view.Murky.BadgeVisible, Is.False, "sin «?×n» a los " + t + " ms");
                for (int k = 0; k < view.Capacity; k++)
                {
                    Assert.That(view.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.None), "capa " + k + " sin aspecto turbio a los " + t + " ms");
                    Assert.That(view.DoseColor(k), Is.Not.EqualTo(Palette.Murky), "capa " + k + " sin el pardo a los " + t + " ms");
                }
            }

            Assert.That(_rig.Animator.IsIdle, Is.True);
            AssertNotMurky(view, after[TinajaIndex], "tras sacar");
            var overlay = (TinajaOverlay)view.OverlayOf(MechanicType.Tinaja)!;
            Assert.That(overlay.MouthTint, Is.EqualTo(Lulo), "la nueva boca");
            Assert.That(overlay.ChipCount, Is.EqualTo(2));

            // Control: una cuia turbia de verdad (sin tinaja) sí lleva el pardo, el «?» y la insignia; si no, el test no probaría
            // nada.
            BoardState murky = new BoardState(new[] { Cuia.Create(4, new[] { Urucum, CamuCamu, Lulo }, 0b011), Cuia.Empty(4) });
            using (var control = new MechanicTestKit.Rig(Art, murky))
            {
                CuiaView mv = control.Views[0];
                Assert.That(mv.Murky.BadgeCount, Is.EqualTo(2));
                Assert.That(mv.DoseColor(0), Is.EqualTo(Palette.Murky));
                Assert.That(mv.Liquid.MurkyLookAt(0), Is.EqualTo(LiquidGraphic.MurkyLook.Hidden));
                Assert.That(MurkyMarks.IsMurkyWater(murky[0]), Is.True);
            }

            Assert.That(MurkyMarks.IsMurkyWater(board[TinajaIndex]), Is.False);
            Assert.That(Palette.HiddenDose(true), Is.EqualTo(Palette.TinajaClay));
            Assert.That(Palette.HiddenDose(false), Is.EqualTo(Palette.Murky));
        }

        [UnityTest]
        public IEnumerator Tinaja_ShowsMouthChipAndRevealedBands_AndNeverWhatIsHidden()
        {
            yield return null;
            BoardState board = Board323();
            _rig = new MechanicTestKit.Rig(Art, board);
            CuiaView view = _rig.Views[TinajaIndex];
            var tin = (TinajaOverlay)view.OverlayOf(MechanicType.Tinaja)!;

            // Al empezar: la boca con el jenipapo de arriba (tinte e icono), «×3» y ninguna banda.
            Assert.That(tin.Visible && tin.MouthVisible, Is.True);
            Assert.That(tin.MouthTint, Is.EqualTo(Jenipapo));
            Assert.That(Opaque(tin.MouthColor), Is.EqualTo(Opaque(Palette.Tint(Jenipapo))), "la boca, del tinte de la fruta de arriba");
            Assert.That(tin.MouthGlyphVisible, Is.True);
            Assert.That(tin.MouthGlyphSprite, Is.SameAs(Art.TinajaGlyph(Jenipapo)), "y su icono de Patrones de fruta");
            Assert.That(tin.MouthGlyphSprite!.name, Is.EqualTo("mech_tinaja_glyph_jenipapo"));
            Assert.That(tin.ChipVisible, Is.True);
            Assert.That(tin.ChipCount, Is.EqualTo(3), "«×n» con las dosis dentro, contando la boca");
            Assert.That(tin.ChipDigitSprite, Is.SameAs(Art.MurkyBadgeDigit(3)));
            Assert.That(tin.BandMask, Is.EqualTo(0));
            Assert.That(Art.HasTinajaGlyph(Lulo), Is.True, "los iconos de sus frutas, preparados al cargar");

            // La capa 1 revelada (Marea tras sacar, o la Luciérnaga): su banda de luz, a su altura y con su icono.
            Cuia tinaja = board[TinajaIndex];
            view.ShowContent(tinaja.WithRevealed(0b10), false);
            Assert.That(tin.BandMask, Is.EqualTo(0b10));
            Assert.That(tin.Band(1).gameObject.activeSelf && tin.BandGlyph(1).gameObject.activeSelf, Is.True);
            Assert.That(tin.Band(0).gameObject.activeSelf, Is.False, "la de abajo sigue oculta");
            Assert.That(Opaque(tin.Band(1).color), Is.EqualTo(Opaque(ColorMath.Lighten(Palette.Tint(Lulo), TinajaOverlay.BandLighten))));
            Assert.That(tin.BandGlyph(1).sprite, Is.SameAs(Art.TinajaGlyph(Lulo)));
            float u = view.Unit;
            Assert.That(tin.Band(1).rectTransform.anchoredPosition.y / u, Is.EqualTo(TinajaOverlay.BandCenterU(1)).Within(0.001f), "a la altura de su dosis");

            // Todo revelado (Luciérnaga): las dos de abajo.
            view.ShowContent(tinaja.WithRevealed(0b11), false);
            Assert.That(tin.BandMask, Is.EqualTo(0b11));
            Assert.That(tin.BandCount, Is.EqualTo(2));

            // Llena de 5 con 4 ocultas: «×5»; con todo a la vista, cuatro bandas.
            Cuia full = Cuia.Create(5, new[] { Urucum, CamuCamu, Jenipapo, Lulo, Urucum }, 0b1111).WithMechanic(MechanicType.Tinaja);
            view.ShowContent(full, false);
            Assert.That(tin.ChipCount, Is.EqualTo(5));
            Assert.That(tin.MouthTint, Is.EqualTo(Urucum));
            Assert.That(tin.BandMask, Is.EqualTo(0));
            view.ShowContent(full.WithRevealed(0b1111), false);
            Assert.That(tin.BandMask, Is.EqualTo(0b1111));

            // Lo oculto nunca se ve: con [urucum, camu camu] ocultos bajo el jenipapo, ninguna pieza lleva su tinte ni su icono.
            Cuia secret = Cuia.Create(5, new[] { Urucum, CamuCamu, Jenipapo }, 0b011).WithMechanic(MechanicType.Tinaja);
            view.ShowContent(secret, false);
            var forbidden = new[]
            {
                Opaque(Palette.Tint(Urucum)), Opaque(Palette.Tint(CamuCamu)),
                Opaque(ColorMath.Lighten(Palette.Tint(Urucum), TinajaOverlay.BandLighten)), Opaque(ColorMath.Lighten(Palette.Tint(CamuCamu), TinajaOverlay.BandLighten)),
            };
            foreach (Image img in ActiveImages(view))
            {
                for (int f = 0; f < forbidden.Length; f++)
                {
                    Assert.That(Opaque(img.color), Is.Not.EqualTo(forbidden[f]), img.name + " enseña una fruta oculta");
                }

                string sprite = img.sprite != null ? img.sprite.name : string.Empty;
                Assert.That(sprite, Does.Not.EndWith("_urucum").And.Not.EndWith("_camu_camu"), img.name + " lleva el icono de una oculta");
            }

            for (int k = 0; k < 2; k++)
            {
                Assert.That(view.DoseColor(k), Is.EqualTo(Palette.TinajaClay), "la oculta " + k + ", del barro");
                Assert.That(view.Liquid.DoseColorAt(k), Is.EqualTo(Palette.TinajaClay));
            }
        }

        [UnityTest]
        public IEnumerator Tinaja_EmptyIsUpsideDown_ItsCuiaReleased_AndMareaHangsItBack()
        {
            yield return null;
            BoardState board = Board323();
            _rig = new MechanicTestKit.Rig(Art, board);
            CuiaView tv = _rig.Views[TinajaIndex];
            CuiaView cv = _rig.Views[CucharonIndex];
            var tin = (TinajaOverlay)tv.OverlayOf(MechanicType.Tinaja)!;
            var cuch = (CucharonOverlay)cv.OverlayOf(MechanicType.Cucharon)!;
            Assert.That(tin.UpsideDown, Is.False);
            Assert.That(tin.BodyVisible && tin.JirauVisible, Is.True);
            Assert.That(cuch.Visible && cuch.PostVisible && cuch.AsaVisible, Is.True, "su cuia, colgada del jirau");

            // La suelta (b.2 regla 8): la tinaja vacía queda inerte boca abajo sobre el jirau; su cuia, una Cuia chica normal.
            tv.ShowContent(Cuia.Empty(5).WithMechanic(MechanicType.Tinaja), false);
            cv.ShowContent(Cuia.Create(2, new[] { Lulo, Jenipapo }), false);
            Assert.That(tin.Visible, Is.True, "la tinaja vacía conserva su tipo");
            Assert.That(tin.UpsideDown, Is.True, "boca abajo");
            Assert.That(tin.BodyVisible, Is.True);
            Assert.That(tin.MouthVisible || tin.ChipVisible || tin.BandCount > 0, Is.False, "sin boca, sin chip y sin bandas");
            Assert.That(tin.JirauVisible, Is.True, "el jirau es permanente (07 §4.3)");
            Assert.That(cuch.Visible || cuch.PostVisible || cuch.AsaVisible, Is.False, "su cuia ya no cuelga");
            Assert.That(tv.Murky.BadgeVisible, Is.False);

            // Marea (todo vive en Cuia.Packed): la tinaja vuelve a tener dosis y su cuia se vuelve a colgar.
            tv.ShowContent(board[TinajaIndex], false);
            cv.ShowContent(board[CucharonIndex], false);
            Assert.That(tin.UpsideDown, Is.False);
            Assert.That(tin.MouthVisible && tin.ChipVisible, Is.True);
            Assert.That(cuch.Visible && cuch.PostVisible && cuch.AsaVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator MapArtRule_GatesTheProvisionalArt_NotTheState()
        {
            yield return null;
            MapArtRule.ForceProvisional = false; // como en release sin el arte del artista
            BoardState board = Board323();
            _rig = new MechanicTestKit.Rig(Art, board);
            var tin = (TinajaOverlay)_rig.Views[TinajaIndex].OverlayOf(MechanicType.Tinaja)!;
            var cuch = (CucharonOverlay)_rig.Views[CucharonIndex].OverlayOf(MechanicType.Cucharon)!;
            Assert.That(tin.BodyVisible || tin.JirauVisible, Is.False, "sin el cuerpo ni el jirau provisionales");
            Assert.That(cuch.PostVisible || cuch.AsaVisible, Is.False, "sin el poste ni el asa provisionales");
            Assert.That(tin.MouthVisible && tin.ChipVisible, Is.True, "la boca y el chip los pinta el código según el estado");
            AssertNotMurky(_rig.Views[TinajaIndex], board[TinajaIndex], "sin arte");

            MapArtRule.ForceProvisional = true; // Editor y desarrollo
            _rig.Views[TinajaIndex].ShowContent(board[TinajaIndex], false);
            _rig.Views[CucharonIndex].ShowContent(board[CucharonIndex], false);
            Assert.That(tin.BodyVisible && tin.JirauVisible && cuch.PostVisible && cuch.AsaVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator Jirau_UnderBothSlots_PostAwayFromTheTinaja()
        {
            yield return null;
            BoardState board = Board323();
            _rig = new MechanicTestKit.Rig(Art, board);
            CuiaView tv = _rig.Views[TinajaIndex];
            CuiaView cv = _rig.Views[CucharonIndex];
            var tin = (TinajaOverlay)tv.OverlayOf(MechanicType.Tinaja)!;
            var cuch = (CucharonOverlay)cv.OverlayOf(MechanicType.Cucharon)!;
            Assert.That(tin.PartnerIndex, Is.EqualTo(CucharonIndex));

            // El jirau va bajo las dos ranuras (de la de su cuia, a la izquierda, a la de la tinaja), con lo alto en la línea de
            // pie, y no se mete en las ranuras de al lado.
            ICuiaBoard b = tv.OverlayHost.Board!;
            Rect jirau = tin.JirauRect;
            float u = tv.Unit;
            Assert.That(jirau.xMin, Is.LessThan(b.Foot(CucharonIndex).x - (0.5f * cv.Unit * CuiaGeometry.WidthU)), "bajo la ranura de su cuia");
            Assert.That(jirau.xMax, Is.GreaterThan(b.Foot(TinajaIndex).x + (0.5f * u * CuiaGeometry.WidthU)), "y bajo la de la tinaja");
            Assert.That(jirau.xMin, Is.GreaterThanOrEqualTo(b.SlotRect(CucharonIndex).xMin - 0.01f));
            Assert.That(jirau.xMax, Is.LessThanOrEqualTo(b.SlotRect(TinajaIndex).xMax + 0.01f));
            Assert.That(jirau.xMin, Is.LessThanOrEqualTo(b.Foot(CucharonIndex).x - (ProceduralArt.TinajaPostXU * cv.Unit)), "llega al poste, que se apoya en él");
            Assert.That(jirau.xMax, Is.EqualTo(b.Foot(TinajaIndex).x + (TinajaOverlay.JirauTinajaExtentU * u)).Within(0.01f), "y por el lado de la tinaja, sin llegar al canto de la ranura (el marco)");
            Assert.That(jirau.yMax, Is.EqualTo(b.Foot(TinajaIndex).y + (TinajaOverlay.JirauTopAboveFootU * u)).Within(0.01f));
            Assert.That(jirau.height, Is.EqualTo(TinajaOverlay.JirauHeightU * u).Within(0.01f));
            Transform? root = tv.OverlayHost.BoardBackLayer!.Find("TinajaJirau" + TinajaIndex);
            Assert.That(root, Is.Not.Null, "en la capa de tablero de atrás, en una raíz propia");
            var rootRt = (RectTransform)root!;
            Assert.That(rootRt.anchorMin, Is.EqualTo(new Vector2(0f, 1f)), "anclada arriba a la izquierda, como las ranuras");

            // Lo que se pinta: el centro del jirau en el mundo cae bajo el punto medio de los dos pies (no solo en las cuentas).
            Vector3 world = tv.OverlayHost.BoardBackLayer!.TransformPoint(new Vector3(jirau.center.x, jirau.center.y, 0f));
            Vector3 drawn = root!.Find("TinajaJirau").GetComponent<RectTransform>().position;
            Assert.That(Vector3.Distance(world, drawn), Is.LessThan(0.01f), "la imagen está donde dice JirauRect");
            float halfW = 0.5f * jirau.width * tv.OverlayHost.BoardBackLayer!.lossyScale.x;
            Assert.That(drawn.x - halfW, Is.LessThan(cv.Root.position.x), "lo que se pinta va bajo el pie de su cuia");
            Assert.That(drawn.x + halfW, Is.GreaterThan(tv.Root.position.x), "y bajo el de la tinaja");
            Assert.That(drawn.y, Is.LessThan(tv.Root.position.y), "bajo la línea de pie");
            Assert.That(drawn.y, Is.GreaterThan(tv.Root.position.y - (TinajaOverlay.JirauHeightU * u)), "pegado a ella");

            // El poste, del lado contrario a la tinaja (nada entre las dos piezas: 07 §4.3) y dentro de su ranura; el asa, de la
            // boca de su cuia al gancho del travesaño.
            Assert.That(cuch.PostSide, Is.EqualTo(-1f), "la tinaja está a la derecha: el poste, a la izquierda");
            Rect post = cuch.PostRectU;
            Assert.That(post.xMax, Is.LessThan(0.25f), "del lado contrario");
            float halfSlotU = 0.5f * (b.SlotRect(CucharonIndex).width / cv.Unit);
            Assert.That(-post.xMin, Is.LessThanOrEqualTo(halfSlotU), "dentro de su ranura");
            Assert.That(cuch.AsaRectU.yMin, Is.LessThan(CuiaGeometry.HeightU(2)), "el asa sale del labio de su cuia");
            Assert.That(cuch.AsaRectU.yMax, Is.LessThanOrEqualTo(ProceduralArt.TinajaPostArmU), "y llega al gancho, bajo el travesaño");

            // Con la tinaja a la izquierda de su cuia, el poste va a la derecha.
            BoardBuildResult mirrored = BoardBuilder.Build(new[] { 4, 5, 2, 4 }, new[] { new[] { 11, 11, 13 }, new[] { 9, 11, 9 }, new int[0], new int[0] },
                new[] { MechanicSpec.None, new MechanicSpec(MechanicType.Tinaja), new MechanicSpec(MechanicType.Cucharon), MechanicSpec.None }, new[] { new[] { 1, 0 }, new[] { 1, 1 } });
            Assert.That(mirrored.Error, Is.Null);
            using (var other = new MechanicTestKit.Rig(Art, mirrored.State!))
            {
                var c2 = (CucharonOverlay)other.Views[2].OverlayOf(MechanicType.Cucharon)!;
                var t2 = (TinajaOverlay)other.Views[1].OverlayOf(MechanicType.Tinaja)!;
                Assert.That(c2.PostSide, Is.EqualTo(1f));
                Assert.That(c2.PostRectU.xMin, Is.GreaterThan(-0.25f));
                Assert.That(t2.PartnerIndex, Is.EqualTo(2));
            }
        }

        [UnityTest]
        public IEnumerator Shape_CoversTheGlass_MouthAndChipSizes_07_4_3()
        {
            yield return null;

            // Opaca: la silueta tapa el vidrio de la Cuia alta de 5 a cualquier altura (las ocultas no asoman por el canto).
            float height = CuiaGeometry.HeightU(LevelValidation.TinajaCapacity);
            float maxHalf = 0f;
            for (float y = 0f; y <= height; y += 0.005f)
            {
                float glass = CuiaGeometry.OuterHalfWidth(y, LevelValidation.TinajaCapacity);
                float jar = ProceduralArt.TinajaSilhouetteHalfWidthU(y);
                Assert.That(jar, Is.GreaterThanOrEqualTo(glass + 0.02f), "a " + y.ToString("0.000", CultureInfo.InvariantCulture) + " u");
                maxHalf = Mathf.Max(maxHalf, jar);
            }

            // Más alta que ancha, con el alto de la Cuia alta de 5 (07 §4.5) y dentro de la ranura (≥ 1 / 0,70 del ancho).
            float top = ProceduralArt.TinajaLipCenterU + ProceduralArt.TinajaLipRyU;
            Assert.That(top, Is.InRange(height, height + 0.2f), "su alto, el de la Cuia alta de 5");
            Assert.That(top, Is.GreaterThan(2f * (2f * maxHalf)), "más del doble de alta que ancha: nunca redonda y baja (07 §4.3)");
            Assert.That(2f * maxHalf, Is.LessThan(CuiaGeometry.WidthU / 0.70f), "cabe en su ranura");
            Assert.That(ProceduralArt.TinajaBodyHeightU, Is.GreaterThanOrEqualTo(top));
            Assert.That(0.5f * ProceduralArt.TinajaBodyWidthU, Is.GreaterThan(maxHalf));

            // La boca: 0,70 del ancho de la cuia o más (≈ 34 dp con la de 48 dp), dentro del hueco del labio, con el icono de
            // 14-16 dp (07 §4.3).
            const float u48 = 48f / CuiaGeometry.WidthU;
            Assert.That(ProceduralArt.TinajaMouthWidthU, Is.GreaterThanOrEqualTo(0.70f * CuiaGeometry.WidthU));
            Assert.That(ProceduralArt.TinajaMouthWidthU * u48, Is.InRange(33f, 35f));
            Assert.That(0.5f * ProceduralArt.TinajaMouthWidthU, Is.LessThanOrEqualTo(ProceduralArt.TinajaOpeningRxU));
            Assert.That(0.5f * ProceduralArt.TinajaMouthHeightU, Is.LessThanOrEqualTo(ProceduralArt.TinajaOpeningRyU));
            Assert.That(0.9f * ProceduralArt.TinajaMouthGlyphU * u48, Is.InRange(14f, 16f), "la silueta del icono (0,9 de su celda)");
            Assert.That(ProceduralArt.TinajaMouthGlyphU, Is.LessThanOrEqualTo(ProceduralArt.TinajaMouthHeightU), "el icono cabe en el agua de la boca");

            // El chip: texto de 14 sp o más, marfil sobre madera a ≥ 5:1, en el hombro: entre la banda más alta (la capa 3) y la
            // boca con la cuia de 48 dp.
            float digitDp = ProceduralArt.MurkyBadgeDp * ProceduralArt.TinajaChipGlyphScale * (28f / 42f); // el dígito ocupa 28 de los 42 px de su celda
            Assert.That(digitDp, Is.GreaterThanOrEqualTo(10f), "la cifra mide como un texto de 14 sp");
            Assert.That(ColorMath.ContrastRatio(Palette.UiPaper, ProceduralArt.TinajaChipWood), Is.GreaterThanOrEqualTo(5f));
            float chipHalfU = 0.5f * ProceduralArt.TinajaChipHeightDp / u48;
            Assert.That(TinajaOverlay.ChipCenterU - chipHalfU, Is.GreaterThan(TinajaOverlay.BandCenterU(3) + (0.5f * ProceduralArt.TinajaBandHeightU)));
            Assert.That(TinajaOverlay.ChipCenterU + chipHalfU, Is.LessThan(ProceduralArt.TinajaMouthCenterU - (0.5f * ProceduralArt.TinajaMouthHeightU)));

            // En el banco (cuias de 40 dp), la boca y el chip se colocan con esas medidas.
            _rig = new MechanicTestKit.Rig(Art, Board323());
            CuiaView view = _rig.Views[TinajaIndex];
            var tin = (TinajaOverlay)view.OverlayOf(MechanicType.Tinaja)!;
            Assert.That(tin.MouthRectU.width, Is.EqualTo(ProceduralArt.TinajaMouthWidthU).Within(0.001f));
            Assert.That(tin.MouthRectU.center.y, Is.EqualTo(ProceduralArt.TinajaMouthCenterU).Within(0.001f));
            Assert.That(tin.MouthGlyphRectU.center.x, Is.EqualTo(tin.MouthRectU.center.x).Within(0.001f), "el icono, en el centro de la boca");
            Assert.That(tin.MouthGlyphRectU.center.y, Is.EqualTo(tin.MouthRectU.center.y).Within(0.001f));
            Assert.That(tin.ChipRectDp.height, Is.EqualTo(ProceduralArt.TinajaChipHeightDp).Within(0.001f));
            Assert.That(tin.ChipGlyphHeightDp, Is.EqualTo(ProceduralArt.MurkyBadgeDp * ProceduralArt.TinajaChipGlyphScale).Within(0.001f));
            Assert.That(tin.BodyRectU.yMin, Is.EqualTo(0f).Within(0.001f), "la vasija se apoya en el pie");
        }

        [UnityTest]
        public IEnumerator SurfaceColors_AtLeast10DeltaE00FromEveryTint_AndClayInItsRange()
        {
            yield return null;
            _rig = new MechanicTestKit.Rig(Art, Board323());
            var colors = new List<Color>();
            _rig.Views[TinajaIndex].OverlayOf(MechanicType.Tinaja)!.CollectSurfaceColors(colors);
            int tinajaColors = colors.Count;
            _rig.Views[CucharonIndex].OverlayOf(MechanicType.Cucharon)!.CollectSurfaceColors(colors);
            Assert.That(tinajaColors, Is.GreaterThan(0));
            Assert.That(colors.Count, Is.GreaterThan(tinajaColors));
            Assert.That(colors, Does.Contain(Palette.TinajaClay));
            for (int c = 0; c < colors.Count; c++)
            {
                for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
                {
                    Assert.That(ColorMath.DeltaE00(colors[c], Palette.Tint(tint)), Is.GreaterThanOrEqualTo(10f), "color " + c + " frente al tinte " + tint);
                }
            }

            // El barro: entre #C4906E y #C7A182 (07 §4.3), lejos del pardo de las turbias y del terracota vetado #B5653A.
            Color32[] clays = { Palette.TinajaClay, Palette.TinajaClayLight, Palette.TinajaClayShade };
            for (int i = 0; i < clays.Length; i++)
            {
                Assert.That(clays[i].r, Is.InRange(0xC4, 0xC7));
                Assert.That(clays[i].g, Is.InRange(0x90, 0xA1));
                Assert.That(clays[i].b, Is.InRange(0x6E, 0x82));
                Assert.That(ColorMath.DeltaE00(clays[i], Palette.Murky), Is.GreaterThanOrEqualTo(15f), "no es el pardo de Aguas turbias");
                Assert.That(ColorMath.DeltaE00(clays[i], Palette.Hex(0xB5653A)), Is.GreaterThanOrEqualTo(10f), "no es el terracota vetado");
            }
        }

        [UnityTest]
        public IEnumerator NothingChangesOnLift_D002()
        {
            yield return null;
            _rig = new MechanicTestKit.Rig(Art, Board323());
            int[] pair = { TinajaIndex, CucharonIndex };
            for (int p = 0; p < pair.Length; p++)
            {
                CuiaView view = _rig.Views[pair[p]];
                _rig.Run(48);
                string before = Signature(view);
                view.SetLifted(true);
                _rig.Run(320);
                Assert.That(view.IsLifted, Is.True);
                Assert.That(Signature(view), Is.EqualTo(before), "cuia " + pair[p] + ": nada del overlay ni del jirau cambia al seleccionar (D-002)");
                view.SetLifted(false);
                _rig.Run(200);
            }
        }

        [UnityTest]
        public IEnumerator ShowContentAndTick_DoNotAllocate_08_5_4()
        {
            yield return null;
            BoardState board = Board323();
            Cuia[] states =
            {
                board[TinajaIndex], board[TinajaIndex].WithRevealed(0b11), Cuia.Create(5, new[] { Jenipapo, Lulo }, 0b01).WithMechanic(MechanicType.Tinaja),
                Cuia.Empty(5).WithMechanic(MechanicType.Tinaja),
            };
            Cuia[] cuias = { board[CucharonIndex], Cuia.Create(2, new[] { Jenipapo }).WithMechanic(MechanicType.Cucharon), Cuia.Create(2, new[] { Jenipapo, Lulo }) };

            // Calentar en un banco y medir en otro (MechanicTestKit.Rig.Dispose).
            using (var warm = new MechanicTestKit.Rig(Art, board))
            {
                Exercise(warm, states, cuias);
            }

            _rig = new MechanicTestKit.Rig(Art, board);
            Exercise(_rig, states, cuias);
            MechanicTestKit.Rig rig = _rig;
            Assert.That(() => Exercise(rig, states, cuias), Is.Not.AllocatingGCMemory());
        }

        [UnityTest]
        public IEnumerator Level323_PairInTheSameRow_NoMurkyDuringTheIntro()
        {
            // El 323 tal cual (su cuia en la 1 y la tinaja en la 2) y con la pareja en la 4 y la 5, donde el reparto de D-006
            // (5/5) cortaría entre las dos: en los dos casos van en la misma fila y contiguas (02 §5).
            int[][] pairs = { new[] { 1, 2 }, new[] { 4, 5 } };
            for (int p = 0; p < pairs.Length; p++)
            {
                int cucharon = pairs[p][0];
                int tinaja = pairs[p][1];
                LaunchOptions.Reset();
                yield return PlayModeHelpers.LoadGameWithPack(Pack323(cucharon, tinaja), 323, new MemoryProgressStore(323, 322));
                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                GameScreen screen = boot.View!;
                LevelFlow flow = boot.Flow!;
                Assert.That(flow.Session.Initial[tinaja].Mechanic, Is.EqualTo(MechanicType.Tinaja));
                Assert.That(flow.Session.Initial[cucharon].Mechanic, Is.EqualTo(MechanicType.Cucharon));
                Assert.That(screen.TinajaPair, Is.EqualTo(cucharon));

                // La intro (Llenar y teñir, D-010), fotograma a fotograma: las ocultas de la tinaja nunca son agua turbia.
                CuiaView tv = screen.Views[tinaja];
                for (int t = 0; t < 12000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
                {
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                    Assert.That(tv.Murky.BadgeVisible, Is.False, "sin «?×n» en la intro (" + t + " ms)");
                    for (int k = 0; k < 2; k++)
                    {
                        Assert.That(tv.DoseColor(k), Is.Not.EqualTo(Palette.Murky), "capa " + k + " sin el pardo en la intro (" + t + " ms)");
                        Assert.That(tv.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.None), "capa " + k + " sin «?» en la intro (" + t + " ms)");
                    }
                }

                if (flow.Phase != FlowPhase.Playing)
                {
                    boot.TapAnywhere(); // por si la intro espera un toque
                    for (int t = 0; t < 4000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
                    {
                        PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                    }
                }

                PlayModeHelpers.StepUntilIdle(boot, 4000);
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
                BoardLayoutResult layout = screen.Layout!;
                Assert.That(layout.RowOf[cucharon], Is.EqualTo(layout.RowOf[tinaja]), "pareja " + cucharon + "-" + tinaja + ": la misma fila");
                Assert.That(layout.CenterX[tinaja] - layout.CenterX[cucharon], Is.EqualTo(layout.SlotWidth).Within(0.01f), "contiguas");
                if (p == 1)
                {
                    Assert.That(layout.PerRow[0], Is.Not.EqualTo(5), "el corte de 5/5 se movió");
                }

                var tin = tv.OverlayOf(MechanicType.Tinaja) as TinajaOverlay;
                Assert.That(tin, Is.Not.Null);
                Assert.That(tin!.Visible && tin.MouthVisible && tin.BodyVisible && tin.JirauVisible, Is.True, "en el Editor sale el arte provisional");
                Assert.That(tin.ChipCount, Is.EqualTo(3));
                Assert.That(tin.PartnerIndex, Is.EqualTo(cucharon));
                Assert.That(screen.Views[cucharon].OverlayOf(MechanicType.Cucharon), Is.InstanceOf<CucharonOverlay>());
                for (int i = 0; i < screen.Views.Length; i++)
                {
                    Assert.That(screen.Views[i].Murky.BadgeCount, Is.EqualTo(0), "ninguna insignia «?×n» en un nivel con tinaja (cuia " + i + ")");
                }

                AssertNotMurky(tv, flow.Session.Current[tinaja], "jugando, pareja " + cucharon + "-" + tinaja);
            }
        }

        // Ninguna capa oculta de la tinaja lleva el pardo, el aspecto turbio ni el «?», y no hay insignia «?×n»; las ocultas van
        // en el barro, mate y sin tinte.
        internal static void AssertNotMurky(CuiaView view, Cuia shown, string when)
        {
            Assert.That(view.Murky.BadgeCount, Is.EqualTo(0), when + ": sin insignia «?×n»");
            Assert.That(view.Murky.BadgeVisible, Is.False, when);
            for (int k = 0; k < shown.Count; k++)
            {
                Assert.That(view.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.None), when + ": capa " + k + " sin aspecto turbio");
                Assert.That(view.Liquid.MurkyMarkDrawn(k), Is.False, when + ": capa " + k + " sin «?»");
                Assert.That(view.DoseColor(k), Is.Not.EqualTo(Palette.Murky), when + ": capa " + k + " sin el pardo");
                if (shown.IsHiddenAt(k))
                {
                    Assert.That(view.DoseColor(k), Is.EqualTo(Palette.TinajaClay), when + ": la oculta " + k + ", del barro");
                    Assert.That(view.Liquid.IsMatteAt(k), Is.True, when + ": mate, sin brillo de agua");
                    Assert.That(view.Liquid.DoseColorAt(k), Is.Not.EqualTo(Palette.Tint(shown.ColorAt(k))), when + ": la oculta " + k + " nunca enseña su tinte");
                }
            }
        }

        private static void Exercise(MechanicTestKit.Rig rig, Cuia[] tinajas, Cuia[] cuias)
        {
            CuiaView tv = rig.Views[TinajaIndex];
            CuiaView cv = rig.Views[CucharonIndex];
            for (int i = 0; i < tinajas.Length; i++)
            {
                tv.ShowContent(tinajas[i], false);
                tv.Tick(16f);
            }

            for (int i = 0; i < cuias.Length; i++)
            {
                cv.ShowContent(cuias[i], false);
                cv.Tick(16f);
            }

            tv.ShowContent(tinajas[0], false);
            cv.ShowContent(cuias[0], false);
            tv.NotifyBoardLayout();
            cv.NotifyBoardLayout();
        }

        private static int[] Caps323()
        {
            return new[] { 4, 2, 5, 4, 4, 4, 4, 4, 4, 4 };
        }

        private static int[][] Cuias323()
        {
            return new[]
            {
                new[] { 11, 11, 13 }, new int[0], new[] { 9, 11, 9 }, new[] { 7, 9, 5 }, new[] { 6, 6, 11, 5 }, new[] { 5, 7, 7, 9 },
                new[] { 13, 13, 5, 13 }, new int[0], new[] { 6, 6, 7 }, new int[0],
            };
        }

        private static MechanicSpec[] Attrs323(int cucharon, int tinaja)
        {
            var attrs = new MechanicSpec[10];
            for (int i = 0; i < attrs.Length; i++)
            {
                attrs[i] = i == cucharon ? new MechanicSpec(MechanicType.Cucharon) : (i == tinaja ? new MechanicSpec(MechanicType.Tinaja) : MechanicSpec.None);
            }

            return attrs;
        }

        private static Color Opaque(Color c)
        {
            return new Color(c.r, c.g, c.b, 1f);
        }

        // Imágenes a la vista del overlay de la cuia (sus cuatro anclas) y de las capas de tablero.
        private static List<Image> ActiveImages(CuiaView view)
        {
            var list = new List<Image>();
            CuiaOverlayHost host = view.OverlayHost;
            Transform[] roots = { host.SlotBack, host.BodyBack, host.BodyFront, host.SlotFront, host.BoardBackLayer!, host.BoardFrontLayer! };
            for (int r = 0; r < roots.Length; r++)
            {
                Image[] images = roots[r].GetComponentsInChildren<Image>(false);
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i].enabled && images[i].color.a > 0.001f)
                    {
                        list.Add(images[i]);
                    }
                }
            }

            return list;
        }

        // Estado de las piezas del overlay (anclas relativas a la cuia) y del jirau (capa de tablero): activación, pose, color
        // y sprite. El cuerpo de la cuia sube al seleccionarla (02 §4.1), pero nada de lo que pinta el overlay cambia (D-002).
        private static string Signature(CuiaView view)
        {
            var sb = new StringBuilder(1024);
            CuiaOverlayHost host = view.OverlayHost;
            Transform[] roots = { host.SlotBack, host.BodyBack, host.BodyFront, host.SlotFront, host.BoardBackLayer!, host.BoardFrontLayer! };
            for (int r = 0; r < roots.Length; r++)
            {
                Transform[] nodes = roots[r].GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < nodes.Length; i++)
                {
                    Transform node = nodes[i];
                    sb.Append(node.name).Append(node.gameObject.activeSelf ? '+' : '-');
                    if (node is RectTransform rt)
                    {
                        sb.Append(rt.anchoredPosition.ToString("F3")).Append(rt.sizeDelta.ToString("F3"));
                    }

                    sb.Append(node.localScale.ToString("F3")).Append(node.localEulerAngles.ToString("F3"));
                    var image = node.GetComponent<Image>();
                    if (image != null)
                    {
                        sb.Append(image.color.ToString("F3")).Append(image.sprite != null ? image.sprite.name : "-");
                    }

                    sb.Append('|');
                }
            }

            return sb.ToString();
        }
    }
}
