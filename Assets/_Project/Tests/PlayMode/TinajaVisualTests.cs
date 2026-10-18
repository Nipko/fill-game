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
    /// revelar ni durante la intro. El arte provisional (cuerpo, jirau, poste y asa) sale con <see cref="MapArtRule"/>, y la boca
    /// y las bandas, solo con el cuerpo; en release, del overlay solo sale el chip «×n», en el hombro del vidrio. En el banco de
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
        public IEnumerator Factory_TinajaAndItsCuia_HaveTheirOverlays_WithTheirStepsFromTJ3()
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
                // TJ3: la tinaja revela su boca y se tumba en la suelta; su cuia baja del jirau. Nada más.
                float jar = kind == MechanicStepKind.Reveal ? TinajaOverlay.RevealStepMs
                    : (kind == MechanicStepKind.Upend ? TinajaOverlay.UpendHoldMs + TinajaOverlay.UpendMs : 0f);
                float cuia = kind == MechanicStepKind.Unhook ? CucharonOverlay.UnhookMs : 0f;
                Assert.That(tinaja.StepDurationMs(kind), Is.EqualTo(jar), "pasos de la tinaja: " + kind);
                Assert.That(cucharon.StepDurationMs(kind), Is.EqualTo(cuia), "pasos de su cuia: " + kind);
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
            Assert.That(tin.Band(1).color, Is.EqualTo(Opaque(Palette.Tint(Lulo))), "el HEX del tinte, sin aclarar y opaco");
            Assert.That(tin.BandLight(1).gameObject.activeSelf, Is.True, "con su filo de luz");
            Assert.That(tin.BandLight(1).color, Is.EqualTo(TinajaOverlay.BandEdgeLight));
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
                Opaque(ColorMath.Darken(Palette.Tint(Urucum), TinajaOverlay.BandRimDarken)), Opaque(ColorMath.Darken(Palette.Tint(CamuCamu), TinajaOverlay.BandRimDarken)),
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
            Assert.That(tv.GlassHidden, Is.True, "con el cuerpo de barro, el vidrio de la cuia no se ve (no es una vasija en una botella)");
            Assert.That(cv.GlassHidden, Is.False, "su cuia es de vidrio");
            Assert.That(cuch.Visible && cuch.PostVisible && cuch.AsaVisible, Is.True, "su cuia, colgada del jirau");

            // La suelta (b.2 regla 8): la tinaja vacía queda inerte boca abajo sobre el jirau; su cuia, una Cuia chica normal.
            tv.ShowContent(Cuia.Empty(5).WithMechanic(MechanicType.Tinaja), false);
            cv.ShowContent(Cuia.Create(2, new[] { Lulo, Jenipapo }), false);
            Assert.That(tin.Visible, Is.True, "la tinaja vacía conserva su tipo");
            Assert.That(tin.UpsideDown, Is.True, "boca abajo");
            Assert.That(tin.BodyVisible, Is.True);
            Assert.That(tin.MouthVisible || tin.ChipVisible || tin.BandCount > 0, Is.False, "sin boca, sin chip y sin bandas");
            Assert.That(tin.JirauVisible, Is.True, "el jirau es permanente (07 §4.3)");
            Assert.That(tv.GlassHidden, Is.True, "boca abajo, el vidrio tampoco asoma por los lados de su boca");
            string[] glass = { "GlassBack", "GlassFront", "DoseMarks" };
            for (int g = 0; g < glass.Length; g++)
            {
                Image piece = tv.Body.Find(glass[g]).GetComponent<Image>();
                Assert.That(piece.enabled, Is.False, glass[g] + ", oculto bajo el barro");
            }

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
            CuiaView tv = _rig.Views[TinajaIndex];
            Assert.That(tin.BodyVisible || tin.JirauVisible, Is.False, "sin el cuerpo ni el jirau provisionales");
            Assert.That(cuch.PostVisible || cuch.AsaVisible, Is.False, "sin el poste ni el asa provisionales");

            // Sin la vasija, la boca, su brillo, su icono y las bandas (hechas para su silueta) no salen: el vidrio ya enseña la
            // cima y lo revelado en su líquido. Solo el chip «×n», en el hombro del vidrio.
            Assert.That(tin.MouthVisible || tin.MouthGlyphVisible, Is.False, "sin boca flotando sobre el cuello del vidrio");
            Assert.That(tv.GlassHidden, Is.False, "se ve el vidrio");
            Assert.That(tin.ChipVisible, Is.True, "el chip lo pinta el código según el estado");
            Assert.That(tin.ChipCount, Is.EqualTo(3));
            float u = tv.Unit;
            Assert.That(tin.ChipRectDp.center.y / u, Is.EqualTo(TinajaOverlay.GlassChipCenterU(LevelValidation.TinajaCapacity)).Within(0.001f), "en el hombro del vidrio");
            Assert.That(TinajaOverlay.GlassChipCenterU(LevelValidation.TinajaCapacity), Is.GreaterThan(CuiaGeometry.CavityTopU(LevelValidation.TinajaCapacity)), "sobre el líquido, sin tapar la cima");
            tv.ShowContent(board[TinajaIndex].WithRevealed(0b11), false);
            Assert.That(tin.BandCount, Is.EqualTo(0), "sin bandas encima de las dosis reveladas del vidrio (no salen dos veces)");
            Assert.That(tin.BandMask, Is.EqualTo(0));
            foreach (Image img in ActiveImages(tv))
            {
                string sprite = img.sprite != null ? img.sprite.name : string.Empty;
                Assert.That(sprite, Does.Not.StartWith("mech_tinaja_glyph_").And.Not.StartWith("mech_tinaja_band").And.Not.StartWith("mech_tinaja_mouth"), img.name + ": sin piezas de la vasija sin su cuerpo");
            }

            tv.ShowContent(board[TinajaIndex], false);
            AssertNotMurky(tv, board[TinajaIndex], "sin arte");

            MapArtRule.ForceProvisional = true; // Editor y desarrollo
            tv.ShowContent(board[TinajaIndex], false);
            _rig.Views[CucharonIndex].ShowContent(board[CucharonIndex], false);
            Assert.That(tin.BodyVisible && tin.JirauVisible && cuch.PostVisible && cuch.AsaVisible, Is.True);
            Assert.That(tin.MouthVisible && tin.MouthGlyphVisible && tin.ChipVisible, Is.True, "con la vasija, su boca y el chip");
            Assert.That(tin.ChipRectDp.center.y / u, Is.EqualTo(TinajaOverlay.ChipCenterU).Within(0.001f), "en el hombro de la vasija");
            Assert.That(tv.GlassHidden, Is.True);
            tv.ShowContent(board[TinajaIndex].WithRevealed(0b10), false);
            Assert.That(tin.BandMask, Is.EqualTo(0b10), "con la vasija, la banda de luz de lo revelado");
        }

        [UnityTest]
        public IEnumerator Partner_IsTheOneOfTheInitialBoard_AfterResumingPastTheRelease()
        {
            // Reanudar tras la suelta: la tinaja vacía (la 2), su cuia ya sin tipo a la derecha (la 3) y otra Cuia chica de 2 a la
            // izquierda (la 1). Mirando solo el tablero mostrado no se sabe cuál es (la suelta le quitó el tipo); con la pareja
            // del tablero inicial, que pasa GameScreen, el jirau va bajo las dos ranuras de la pareja (07 §4.3; 02 §5).
            yield return null;
            var board = new BoardState(new[]
            {
                Cuia.Create(4, new[] { Lulo, Lulo, Urucum }), Cuia.Create(2, new[] { Jenipapo, Lulo }),
                Cuia.Empty(LevelValidation.TinajaCapacity).WithMechanic(MechanicType.Tinaja), Cuia.Empty(2), Cuia.Empty(4),
            });
            _rig = new MechanicTestKit.Rig(Art, board);
            CuiaView tv = _rig.Views[2];
            var tin = (TinajaOverlay)tv.OverlayOf(MechanicType.Tinaja)!;

            tv.SetTinajaPartner(3);
            Assert.That(tv.TinajaPartner, Is.EqualTo(3));
            Assert.That(tin.PartnerIndex, Is.EqualTo(3), "la pareja del tablero inicial manda");
            ICuiaBoard b = tv.OverlayHost.Board!;
            Rect jirau = tin.JirauRect;
            Assert.That(jirau.xMax, Is.GreaterThan(b.Foot(3).x), "bajo la ranura de su cuia, a la derecha");
            Assert.That(jirau.xMin, Is.GreaterThan(b.Foot(1).x + (0.5f * _rig.Views[1].Unit * CuiaGeometry.WidthU)), "y no bajo la otra Cuia chica de 2");

            // Ni Marea ni otro Show la cambian; una vista nueva la recibe antes de unir su overlay (GameScreen la fija al
            // construir el tablero, antes de BoardAnimator.Bind).
            tv.ShowContent(Cuia.Create(5, new[] { Jenipapo }).WithMechanic(MechanicType.Tinaja), false);
            Assert.That(tin.PartnerIndex, Is.EqualTo(3));
            using (var other = new MechanicTestKit.Rig(Art, board, views => views[2].SetTinajaPartner(3)))
            {
                var t2 = (TinajaOverlay)other.Views[2].OverlayOf(MechanicType.Tinaja)!;
                Assert.That(t2.PartnerIndex, Is.EqualTo(3));
                Assert.That(t2.JirauRect.xMax, Is.GreaterThan(other.Views[2].OverlayHost.Board!.Foot(3).x));
            }
        }

        [UnityTest]
        public IEnumerator Selected_TinajaLifts8dp_WithA2DegreeSway_07_4_3()
        {
            yield return null;
            _rig = new MechanicTestKit.Rig(Art, Board323());
            CuiaView tv = _rig.Views[TinajaIndex];
            CuiaView other = _rig.Views[0];
            Assert.That(tv.LiftAmountDp, Is.EqualTo(CuiaView.TinajaLiftDp));
            Assert.That(other.LiftAmountDp, Is.EqualTo(CuiaView.LiftDp));
            Assert.That(CuiaView.TinajaLiftDp, Is.EqualTo(8f), "07 §4.3: sube 8 dp (pesa)");

            // Propuesta de la Tinaja, b.2, «Animación y sonido»: sube 8 dp y se asienta con un balanceo de 2°, en 100 + 150 ms. El
            // banco usa anim_lift_ms = 100 (08 §9).
            const float liftMs = 100f;
            Assert.That(CuiaView.TinajaSwayMs, Is.EqualTo(150f), "el asiento con balanceo, 150 ms");
            tv.SetLiftDuration(liftMs);
            tv.SetLifted(true);
            float maxAngle = 0f;
            float maxY = 0f;
            int firstSway = -1;
            int lastSway = -1;
            float yAtFirstSway = 0f;
            for (int t = PlayModeHelpers.FrameMs; t <= 600; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                float angle = Mathf.Abs(tv.BodyAngle);
                if (angle > 0.0001f)
                {
                    if (firstSway < 0)
                    {
                        firstSway = t;
                        yAtFirstSway = tv.BodyOffset.y;
                    }

                    lastSway = t;
                }

                maxAngle = Mathf.Max(maxAngle, angle);
                maxY = Mathf.Max(maxY, tv.BodyOffset.y);
            }

            Assert.That(maxAngle, Is.InRange(1f, CuiaView.TinajaSwayDeg + 0.01f), "un balanceo de 2° como mucho");
            Assert.That(firstSway, Is.GreaterThanOrEqualTo((int)liftMs), "sin balanceo durante la subida: empieza al terminarla");
            Assert.That(yAtFirstSway, Is.EqualTo(CuiaView.TinajaLiftDp).Within(0.01f), "ya arriba, a 8 dp");
            int settled = lastSway + PlayModeHelpers.FrameMs; // el primer fotograma quieto
            Assert.That(settled, Is.InRange(250 - PlayModeHelpers.FrameMs, 250 + (2 * PlayModeHelpers.FrameMs)), "100 + 150 ms: unos 250 ms en total");
            Assert.That(tv.BodyAngle, Is.EqualTo(0f).Within(0.001f), "y se apaga");
            Assert.That(tv.BodyOffset.y, Is.EqualTo(CuiaView.TinajaLiftDp).Within(0.01f), "asentada a 8 dp");
            Assert.That(maxY, Is.LessThan(CuiaView.LiftDp), "nunca sube como una cuia normal");
            tv.SetLifted(false);
            _rig.Run(200);
            Assert.That(tv.BodyOffset.y, Is.EqualTo(0f).Within(0.01f));

            other.SetLifted(true);
            _rig.Run(400);
            Assert.That(other.BodyOffset.y, Is.EqualTo(CuiaView.LiftDp).Within(0.01f), "las demás, 16 dp y sin balanceo");
            Assert.That(other.BodyAngle, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Glyphs_AllTintsPreparedOnLoad_EvenWithTheTinajaEmpty_08_5_4()
        {
            // Una partida reanudada tras la suelta (la tinaja vacía): al cargar se preparan los iconos de los 14 tintes, así que
            // cuando Marea le devuelva dosis no se genera ninguna textura a mitad de un paso.
            yield return null;
            using (var art = new ProceduralArt("white"))
            {
                var board = new BoardState(new[]
                {
                    Cuia.Create(4, new[] { Lulo, Lulo, Urucum }), Cuia.Create(2, new[] { Jenipapo, Lulo }),
                    Cuia.Empty(LevelValidation.TinajaCapacity).WithMechanic(MechanicType.Tinaja), Cuia.Empty(4),
                });
                using (var rig = new MechanicTestKit.Rig(art, board))
                {
                    for (int tint = Cuia.MinColor; tint <= Cuia.MaxTintColor; tint++)
                    {
                        Assert.That(art.HasTinajaGlyph(tint), Is.True, "icono del tinte " + tint + ", preparado al cargar");
                    }

                    CuiaView tv = rig.Views[2];
                    Cuia back = Cuia.Create(5, new[] { Jenipapo, Lulo, CamuCamu }, 0b011).WithMechanic(MechanicType.Tinaja).WithRevealed(0b10);
                    using (var warm = new MechanicTestKit.Rig(Art, board))
                    {
                        warm.Views[2].ShowContent(back, false); // calienta lo que no es del arte (como ShowContentAndTick_DoNotAllocate)
                        warm.Views[2].ShowContent(board[2], false);
                    }

                    Assert.That(() => tv.ShowContent(back, false), Is.Not.AllocatingGCMemory(), "Marea tras la suelta, sin asignaciones");
                }
            }
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
            Assert.That(jirau.height, Is.EqualTo(TinajaOverlay.JirauHeightDp(u)).Within(0.01f));
            Assert.That(jirau.yMin, Is.GreaterThanOrEqualTo(b.Foot(TinajaIndex).y - 3f - 0.01f), "ninguna vara baja de la cara superior de la repisa (pie ± 3 dp)");
            Assert.That(jirau.yMax, Is.LessThanOrEqualTo(b.Foot(TinajaIndex).y + 3f), "ni sube por encima de ella");
            Assert.That(TinajaOverlay.JirauTopAboveFootU * u, Is.LessThan(1f), "su vara de arriba, justo bajo el pie: las dos piezas se apoyan en ella");
            Transform? root = tv.OverlayHost.BoardBackLayer!.Find("TinajaJirau" + TinajaIndex);
            Assert.That(root, Is.Not.Null, "en la capa de tablero de atrás, en una raíz propia");
            var rootRt = (RectTransform)root!;
            Assert.That(rootRt.anchorMin, Is.EqualTo(new Vector2(0f, 1f)), "anclada arriba a la izquierda, como las ranuras");
            Assert.That(root.childCount, Is.EqualTo(1), "solo las varas: nada de cordón, liana ni nudo entre las dos piezas (b.2 «Aspecto»)");
            Assert.That(root.Find("TinajaJirauTie"), Is.Null, "sin atadura en el punto medio entre la tinaja y su cuia");

            // Lo que se pinta: el centro del jirau en el mundo cae bajo el punto medio de los dos pies (no solo en las cuentas).
            Vector3 world = tv.OverlayHost.BoardBackLayer!.TransformPoint(new Vector3(jirau.center.x, jirau.center.y, 0f));
            Vector3 drawn = root!.Find("TinajaJirau").GetComponent<RectTransform>().position;
            Assert.That(Vector3.Distance(world, drawn), Is.LessThan(0.01f), "la imagen está donde dice JirauRect");
            float halfW = 0.5f * jirau.width * tv.OverlayHost.BoardBackLayer!.lossyScale.x;
            Assert.That(drawn.x - halfW, Is.LessThan(cv.Root.position.x), "lo que se pinta va bajo el pie de su cuia");
            Assert.That(drawn.x + halfW, Is.GreaterThan(tv.Root.position.x), "y bajo el de la tinaja");
            Assert.That(drawn.y, Is.LessThan(tv.Root.position.y), "bajo la línea de pie");
            Assert.That(drawn.y, Is.GreaterThan(tv.Root.position.y - TinajaOverlay.JirauHeightDp(u)), "pegado a ella");

            // El poste, del lado contrario a la tinaja (nada entre las dos piezas: 07 §4.3) y dentro de su ranura; el asa, de
            // hombro a hombro de su cuia, apoyada en el gancho.
            Assert.That(cuch.PostSide, Is.EqualTo(-1f), "la tinaja está a la derecha: el poste, a la izquierda");
            Rect post = cuch.PostRectU;
            Assert.That(post.xMax, Is.LessThan(0.25f), "del lado contrario");
            float halfSlotU = 0.5f * (b.SlotRect(CucharonIndex).width / cv.Unit);
            Assert.That(-post.xMin, Is.LessThanOrEqualTo(halfSlotU), "dentro de su ranura");
            AssertHangsWithoutGallows(cuch);

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

            // Opaca: la silueta tapa el vidrio de la Cuia alta de 5 hasta lo alto de su cuello (las ocultas no asoman por el canto);
            // el borde de fibra de encima no hace falta, porque con el cuerpo a la vista el vidrio entero se oculta (CuiaView).
            float height = CuiaGeometry.HeightU(LevelValidation.TinajaCapacity);
            float neckTop = CuiaGeometry.CavityTopU(LevelValidation.TinajaCapacity) + CuiaGeometry.ShoulderU + CuiaGeometry.NeckU;
            float maxHalf = 0f;
            for (float y = 0f; y <= height; y += 0.005f)
            {
                maxHalf = Mathf.Max(maxHalf, ProceduralArt.TinajaSilhouetteHalfWidthU(y));
                if (y > neckTop)
                {
                    continue;
                }

                float glass = CuiaGeometry.OuterHalfWidth(y, LevelValidation.TinajaCapacity);
                float jar = ProceduralArt.TinajaSilhouetteHalfWidthU(y);
                Assert.That(jar, Is.GreaterThanOrEqualTo(glass + 0.02f), "a " + y.ToString("0.000", CultureInfo.InvariantCulture) + " u");
            }

            // Más alta que ancha, con el alto de la Cuia alta de 5 (07 §4.5) y dentro de la ranura (≥ 1 / 0,70 del ancho).
            float top = ProceduralArt.TinajaLipCenterU + ProceduralArt.TinajaLipRyU;
            Assert.That(top, Is.EqualTo(height).Within(0.001f), "su alto, el de la Cuia alta de 5: 6,60 u (02 §5; 07 §4.5)");
            Assert.That(ProceduralArt.TinajaTopU, Is.EqualTo(6.60f).Within(0.001f));
            Assert.That(ProceduralArt.TinajaSilhouetteHalfWidthU(height + 0.01f), Is.EqualTo(0f), "nada por encima de 6,60 u");
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
            float digitTopDp = (0.5f * ProceduralArt.TinajaChipHeightDp) + (0.5f * digitDp);
            float glossBottomDp = ProceduralArt.TinajaChipHeightDp - ProceduralArt.TinajaChipGlossTopDp - ProceduralArt.TinajaChipGlossDp;
            Assert.That(digitTopDp, Is.LessThan(glossBottomDp - 0.5f), "el brillo del chip, por encima de la cifra: toda la cifra, sobre la madera a ≥ 5:1");
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
                float pitch = layout.CenterX[tinaja] - layout.CenterX[cucharon];
                Assert.That(pitch, Is.GreaterThan(layout.CuiaWidth).And.LessThanOrEqualTo(layout.SlotWidth + 0.01f), "contiguas");
                Assert.That(screen.TinajaIndex, Is.EqualTo(tinaja));
                Assert.That(screen.Views[tinaja].TinajaPartner, Is.EqualTo(cucharon), "la pantalla pasa la pareja del tablero inicial");
                if (p == 1)
                {
                    Assert.That(layout.PerRow[0], Is.Not.EqualTo(5), "el corte de 5/5 se movió");
                }

                // La vasija y el jirau, dentro de lo que ocupa el vidrio y sobre la repisa: a ≥ 2 dp de su punta y lejos del marco
                // como el vidrio (el marco se adelgaza con ContentLeft).
                float w = (2f * layout.PanelX) + layout.PanelWidth;
                float uT = screen.Views[tinaja].Unit;
                float bodyL = layout.CenterX[tinaja] - (0.5f * ProceduralArt.TinajaBodyWidthU * uT);
                float bodyR = layout.CenterX[tinaja] + (0.5f * ProceduralArt.TinajaBodyWidthU * uT);
                Assert.That(bodyL, Is.GreaterThanOrEqualTo(layout.ContentLeft - 0.01f));
                Assert.That(bodyR, Is.LessThanOrEqualTo(w - layout.ContentLeft + 0.01f), "pareja " + cucharon + "-" + tinaja + ": la vasija no llega al marco");
                float shelfL = layout.PanelX + screen.PanelFrameDp + 6f;
                float shelfR = layout.PanelX + layout.PanelWidth - screen.PanelFrameDp - 6f;
                float glassLeft = float.MaxValue;
                for (int i = 0; i < screen.Views.Length; i++)
                {
                    if (i != tinaja && i != cucharon)
                    {
                        glassLeft = Mathf.Min(glassLeft, layout.CenterX[i] - (0.5f * layout.CuiaWidth));
                    }
                }

                Assert.That(layout.ContentLeft, Is.GreaterThanOrEqualTo(glassLeft - 0.01f), "la pareja no pide más sitio que el vidrio: el marco, como sin tinaja");
                ICuiaBoard board = tv.OverlayHost.Board!;
                Rect jirau = TinajaOf(tv).JirauRect;
                float toCanvas = layout.CenterX[tinaja] - board.Foot(tinaja).x;
                Assert.That(jirau.xMin + toCanvas, Is.GreaterThanOrEqualTo(shelfL + 2f - 0.01f), "el jirau, sobre la repisa");
                Assert.That(jirau.xMax + toCanvas, Is.LessThanOrEqualTo(shelfR - 2f + 0.01f), "el jirau, sobre la repisa");

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

        [UnityTest]
        public IEnumerator Bands_KeepTheirTintHex_ForAll14Tints()
        {
            // La banda tiene que emparejarse con su fruta: el relleno, con el HEX del tinte opaco (aclararlo lavaba los oscuros:
            // el jenipapo salía gris lavanda y el cacao cerca del pardo de Aguas turbias, vetado en la tinaja por 07 §4.3), más
            // cerca de su tinte que de cualquier otro, lejos del pardo #8A7B6A y del barro; y, si el tinte queda cerca del barro
            // (hoy el Agua del Río Negro), un contorno de 1 dp con el tinte oscurecido que sí se separa de él.
            yield return null;
            _rig = new MechanicTestKit.Rig(Art, Board323());
            CuiaView tv = _rig.Views[TinajaIndex];
            var tin = TinajaOf(tv);
            int rims = 0;
            for (int tint = Cuia.MinColor; tint <= Cuia.MaxTintColor; tint++)
            {
                int top = tint == Lulo ? Jenipapo : Lulo;
                tv.ShowContent(Cuia.Create(5, new[] { tint, top }).WithMechanic(MechanicType.Tinaja), false);
                Assert.That(tin.BandMask, Is.EqualTo(0b1), "tinte " + tint + ": su banda");
                Color band = tin.Band(0).color;
                Assert.That(band.a, Is.EqualTo(1f), "tinte " + tint + ": opaca");
                Assert.That(Opaque(band), Is.EqualTo(Opaque(Palette.Tint(tint))), "tinte " + tint + ": el HEX del tinte");
                float own = ColorMath.DeltaE00(band, Palette.Tint(tint));
                for (int other = Cuia.MinColor; other <= Cuia.MaxTintColor; other++)
                {
                    if (other != tint)
                    {
                        Assert.That(own, Is.LessThan(ColorMath.DeltaE00(band, Palette.Tint(other))), "tinte " + tint + ": más cerca de su tinte que del " + other);
                    }
                }

                Assert.That(ColorMath.DeltaE00(band, Palette.Murky), Is.GreaterThanOrEqualTo(15f), "tinte " + tint + ": lejos del pardo de Aguas turbias");
                Assert.That(ColorMath.DeltaE00(band, Palette.TinajaClay), Is.GreaterThanOrEqualTo(12f), "tinte " + tint + ": se ve sobre el barro");
                bool rim = TinajaOverlay.BandNeedsRim(tint);
                Assert.That(tin.BandRim(0).gameObject.activeSelf, Is.EqualTo(rim), "tinte " + tint + ": contorno solo si hace falta");
                Color edge = rim ? tin.BandRim(0).color : band; // lo que separa la banda del barro alrededor
                if (rim)
                {
                    rims++;
                    Assert.That(Opaque(edge), Is.EqualTo(Opaque(ColorMath.Darken(Palette.Tint(tint), TinajaOverlay.BandRimDarken))));
                }

                for (int b = 0; b < TinajaOverlay.BandBodyTones.Length; b++)
                {
                    Assert.That(ColorMath.DeltaE00(edge, TinajaOverlay.BandBodyTones[b]), Is.GreaterThanOrEqualTo(TinajaOverlay.BandRimMinDeltaE), "tinte " + tint + ": su canto frente al barro " + b);
                }
            }

            Assert.That(TinajaOverlay.BandNeedsRim(14), Is.True, "el Agua del Río Negro, cerca del barro de la sombra, lleva contorno");
            Assert.That(rims, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Release_WithoutTheArt_TheRowKeepsTheUniformSlots_02_5()
        {
            // En release, hasta el arte, no hay vasija ni poste: la fila de la pareja no se aprieta y el marco no se adelgaza por
            // piezas que no se dibujan (02 §5; 07 §4.1: ranura = ancho útil / cuias de la fila más larga). El corte entre filas sí
            // se mueve para que la pareja vaya junta.
            int[][] pairs = { new[] { 1, 2 }, new[] { 4, 5 } };
            for (int p = 0; p < pairs.Length; p++)
            {
                int cucharon = pairs[p][0];
                int tinaja = pairs[p][1];
                LaunchOptions.Reset();
                MapArtRule.ForceProvisional = false;
                yield return PlayModeHelpers.LoadGameWithPack(Pack323(cucharon, tinaja), 323, new MemoryProgressStore(323, 322));
                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                GameScreen screen = boot.View!;
                Assert.That(screen.TinajaIndex, Is.EqualTo(tinaja));
                Assert.That(screen.LayoutTinajaIndex, Is.EqualTo(-1), "sin el arte, el layout no hace sitio a la vasija ni al poste");
                BoardLayoutResult layout = screen.Layout!;
                Assert.That(layout.RowOf[cucharon], Is.EqualTo(layout.RowOf[tinaja]), "pareja " + cucharon + "-" + tinaja + ": la misma fila");
                for (int i = 0; i + 1 < screen.Views.Length; i++)
                {
                    if (layout.RowOf[i] == layout.RowOf[i + 1])
                    {
                        Assert.That(layout.CenterX[i + 1] - layout.CenterX[i], Is.EqualTo(layout.SlotWidth).Within(0.01f), "pareja " + cucharon + "-" + tinaja + ": el paso de la ranura entre " + i + " y " + (i + 1));
                    }
                }

                float glassLeft = float.MaxValue;
                for (int i = 0; i < screen.Views.Length; i++)
                {
                    glassLeft = Mathf.Min(glassLeft, layout.CenterX[i] - (0.5f * layout.CuiaWidth));
                }

                Assert.That(layout.ContentLeft, Is.EqualTo(glassLeft).Within(0.01f), "el marco, como sin tinaja");
                if (p == 1)
                {
                    Assert.That(layout.PerRow[0], Is.Not.EqualTo(5), "el corte de 5/5 se movió también sin el arte");
                }

                // Con el arte (Editor y desarrollo), la fila se aprieta si la vasija o el poste caen en una punta.
                MapArtRule.ForceProvisional = true;
                Assert.That(screen.LayoutTinajaIndex, Is.EqualTo(tinaja));
            }
        }

        // La cuia cuelga como el cazo de la casa, sin silueta de horca (07 §8; 07 §4.3 y b.2): un poste trasero bajo del lado
        // contrario a la tinaja, sin travesaño, con un gancho corto fuera del cuello; el asa, un arco de fibra de hombro a hombro
        // que pasa holgado sobre el cuello y el borde de fibra y se apoya en el gancho. Ningún tramo horizontal sobre el cuello y
        // ninguna cuerda hasta el cuello ni el corcho.
        private static void AssertHangsWithoutGallows(CucharonOverlay cuch)
        {
            const int cap = LevelValidation.CucharonCapacity;
            float rimTop = CuiaGeometry.HeightU(cap);
            Assert.That(ProceduralArt.TinajaPostTopU, Is.InRange(rimTop, 4.0f + 0.001f), "un poste bajo: la punta, a 4,0 u");
            Assert.That(ProceduralArt.TinajaPostXU, Is.EqualTo(1.19f).Within(0.001f));
            Assert.That(cuch.PostRectU.yMax, Is.LessThanOrEqualTo(ProceduralArt.TinajaPostTopU + 0.15f), "sin nada por encima de la punta");
            Assert.That(ProceduralArt.TinajaAsaTopU, Is.LessThanOrEqualTo(3.95f), "la cima del asa, como mucho a 3,95 u");
            Assert.That(ProceduralArt.TinajaAsaYAt(0f), Is.EqualTo(ProceduralArt.TinajaAsaTopU).Within(0.001f));

            // El asa sale de los dos hombros (donde el vidrio mide ±0,80 u), sobre el líquido y bajo el cuello.
            Assert.That(CuiaGeometry.OuterHalfWidth(ProceduralArt.TinajaAsaFootU, cap), Is.EqualTo(ProceduralArt.TinajaAsaHalfU).Within(0.02f), "arranca en los hombros");
            Assert.That(ProceduralArt.TinajaAsaFootU, Is.InRange(CuiaGeometry.CavityTopU(cap), CuiaGeometry.CavityTopU(cap) + CuiaGeometry.ShoulderU));
            Assert.That(ProceduralArt.TinajaAsaYAt(ProceduralArt.TinajaAsaHalfU), Is.EqualTo(ProceduralArt.TinajaAsaFootU).Within(0.001f));
            Assert.That(cuch.AsaRectU.xMin, Is.LessThanOrEqualTo(-ProceduralArt.TinajaAsaHalfU));
            Assert.That(cuch.AsaRectU.xMax, Is.GreaterThanOrEqualTo(ProceduralArt.TinajaAsaHalfU));
            Assert.That(cuch.AsaRectU.yMin, Is.LessThan(ProceduralArt.TinajaAsaFootU));
            Assert.That(cuch.AsaRectU.yMax, Is.LessThanOrEqualTo(ProceduralArt.TinajaPostTopU + 0.001f));

            // Sobre el cuello y el borde de fibra, el asa pasa holgada (≥ 0,10 u): no toca el cuello ni el corcho.
            float neckZone = CuiaGeometry.RimHalfWidthU + 0.05f;
            for (float x = -neckZone; x <= neckZone; x += 0.01f)
            {
                Assert.That(ProceduralArt.TinajaAsaYAt(x), Is.GreaterThanOrEqualTo(rimTop + 0.10f), "el asa a " + x.ToString("0.00", CultureInfo.InvariantCulture) + " u, sobre el cuello");
            }

            // El gancho: fuera del cuello y del borde de fibra, más bajo que la cima del asa y justo bajo su flanco (se apoya).
            Vector2 hook = ProceduralArt.TinajaHookU;
            Assert.That(hook.x, Is.GreaterThanOrEqualTo(CuiaGeometry.RimHalfWidthU + 0.15f), "el gancho no pasa sobre el cuello");
            Assert.That(hook.x, Is.LessThan(ProceduralArt.TinajaPostXU));
            Assert.That(hook.y, Is.LessThan(ProceduralArt.TinajaAsaTopU), "más bajo que la cima del asa");
            float asaAtHook = ProceduralArt.TinajaAsaYAt(hook.x);
            Assert.That(asaAtHook - hook.y, Is.InRange(0.02f, 0.12f), "bajo el flanco del asa, que se apoya en él");
        }

        private static TinajaOverlay TinajaOf(CuiaView view)
        {
            return (TinajaOverlay)view.OverlayOf(MechanicType.Tinaja)!;
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
