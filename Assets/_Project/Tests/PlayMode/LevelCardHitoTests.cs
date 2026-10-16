#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Tarjeta previa del hito p20 con «Si lo superas, llegas a {lugar}» (tarea M2.5; 02 §10 y 07 §7.7; D-013, punto 8;
    /// glosario §10, <c>map_hito_preview</c>), con el catálogo real de 1-420 (M4.5a), el de paradas del juego y las tarjetas
    /// encendidas (<see cref="LaunchOptions.LevelCardsOverride"/>, con <see cref="CardSetup"/>): la regla (solo 40, 60, 80, 100,
    /// 120, 140, 160, 180 y 200 en 1-200: L ≥ 40, L mod 20 = 0 y <c>ArrivalOnWin(L)</c>; el 200 lleva a Mazán, el 220 a Nuevo
    /// Rocafuerte y el 440 aún no tiene parada); la fila con el sitio correcto,
    /// su silueta de 64 × 22 dp en su ventanita (el Encuentro del 40, con su pieza por código; los demás, con su silueta
    /// provisional, que solo sale con <see cref="MapArtRule"/>), la línea en Nunito Bold de 14 sp en 2 líneas como mucho y nunca
    /// por debajo de 12, sin desbordes y dentro de la tarjeta, que crece y se sigue cerrando con 1 toque; el 30 y el 50 siguen sin
    /// tarjeta y una tarjeta etiquetada que no es hito (el 46 y el 52) no cambia; sin el nombre del lugar en el idioma no hay fila; y el 100 cabe a 360 × 780 y a «360 × 640» (16:9), a 100 %
    /// y al 130 %, y también en un lienzo literal de 360 × 640 dp; si no cupiera, la fila se omitiría (<see cref="LevelCardView.HitoOmitted"/>).
    /// </summary>
    public sealed class LevelCardHitoTests
    {
        /// <summary>Hitos p20 de 40-180 con parada (sin el 100, la tarjeta combinada, que va aparte) y el id de esa parada.</summary>
        internal static readonly (int Level, string Stop)[] Hitos =
        {
            (40, "encontro"),
            (60, "anavilhanas"),
            (80, "leticia"),
            (120, "tarapoto"),
            (140, "iquitos"),
            (160, "allpahuayo_mishana"),
            (180, "pacaya_samiria"),
        };

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780; // 360 × 780 dp con zona segura
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Rule_OnlyTheP20HitosWithAStop()
        {
            yield return CardSetup.Load(40);
            RiverStopCatalog stops = GameBootstrap.Current!.View!.RiverStops!;
            Assert.That(stops, Is.Not.Null, "el juego carga las paradas de 1-200");
            var levels = new List<int>();
            for (int level = 1; level <= 200; level++)
            {
                RiverStop? stop = LevelCardView.HitoStopFor(level, stops);
                if (stop != null)
                {
                    levels.Add(level);
                    Assert.That(stop, Is.SameAs(stops.ArrivalOnWin(level)), "la parada a la que se llega al ganar el " + level);
                }
            }

            Assert.That(levels, Is.EqualTo(new[] { 40, 60, 80, 100, 120, 140, 160, 180, 200 }), "el 20 no (L ≥ 40); el 200 lleva a Mazán (M4.5a)");
            Assert.That(LevelCardView.HitoStopFor(40, null), Is.Null, "sin catálogo, sin fila");
            Assert.That(LevelCardView.HitoStopFor(220, stops)!.Id, Is.EqualTo("nuevo_rocafuerte"), "201-420 ya tienen paradas (M4.5a)");
            Assert.That(LevelCardView.HitoStopFor(440, stops), Is.Null, "sin paradas de 421 en adelante, sin fila");
        }

        [UnityTest]
        public IEnumerator Hitos_CarryTheRowWithTheRightPlace()
        {
            yield return CardSetup.Load(Hitos[0].Level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            foreach ((int level, string id) in Hitos)
            {
                if (level != Hitos[0].Level)
                {
                    boot.Flow!.Load(level);
                    PlayModeHelpers.Step(boot, 300);
                }

                string where = "nivel " + level;
                LevelCardView card = view.LevelCard;
                Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard), where);
                Assert.That(card.Visible, Is.True, where);
                Assert.That(card.Card!.Level, Is.EqualTo(level), where);
                Assert.That(card.TitleText, Is.EqualTo(level == 40 ? "Difícil" : "Muy difícil"), "el hito sigue con su etiqueta: " + where);
                Assert.That(card.RewardText, Is.Not.Null, "y con su premio: " + where);

                RiverStop stop = card.HitoStop!;
                Assert.That(stop, Is.Not.Null, where);
                Assert.That(stop.Id, Is.EqualTo(id), where);
                Assert.That(stop.N, Is.EqualTo(level / 10), "la parada k = L / 10: " + where);
                string place = Loc.Fact(stop.NameKey);
                Assert.That(place, Is.Not.Empty, "nombre del lugar en es-419: " + where);
                Assert.That(card.HitoText, Is.EqualTo("Si lo superas, llegas a " + place), where);
                Assert.That(card.HitoText, Is.EqualTo(UiText.MapHitoPreview(stop)), where);
                Assert.That(card.HitoOmitted, Is.False, where);

                // La silueta: el Encuentro (40) con su pieza por código; los demás, con la silueta provisional (en el Editor).
                Assert.That(card.HitoSilhouette, Is.EqualTo(level == 40 ? LevelCardView.HitoSilhouetteCode : LevelCardView.HitoSilhouetteShape), where);
                Assert.That(card.HitoScene.IsEmpty, Is.False, where);
                AssertRow(view, where);

                // AF_STATE level_card con la parada y el origen de la silueta.
                Assert.That(view.LastCardProbe, Does.Contain("\"hito\":\"" + id + "\""), where);
                Assert.That(view.LastCardProbe, Does.Contain("\"hito_silhouette\":\"" + card.HitoSilhouette + "\""), where);
                Assert.That(view.LastCardProbe, Does.Contain("\"hito_omitted\":false"), where);
            }

            // La tarjeta crece y se sigue cerrando con 1 toque: la intro y después Playing.
            boot.Flow!.Load(40);
            PlayModeHelpers.Step(boot, 300);
            LevelCardView last = view.LevelCard;
            float with = last.Panel.rect.height;
            LevelCardInfo info = last.Card!;
            last.Show(info, false, view.CanvasWidth, view.CardCanvasHeight, view.SafeTopDp, view.CardSafeBottom);
            float without = last.Panel.rect.height;
            Assert.That(with, Is.GreaterThan(without + 30f), "la fila añade su alto a la tarjeta del 40");
            Assert.That(last.HitoText, Is.Null, "sin parada, la misma tarjeta no lleva la fila");
            boot.Flow.Load(40);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(view.LevelCard.HitoText, Is.Not.Null);
            CardSetup.TapCard(view.LevelCard);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            CardSetup.StepUntilPlaying(boot);
            Assert.That(view.LevelCard.Visible, Is.False);

            // Ya oculta, la tarjeta no anuncia la fila de la anterior.
            Assert.That(view.LevelCard.HitoStop, Is.Null, "oculta, sin parada");
            Assert.That(view.LevelCard.HitoText, Is.Null, "oculta, sin línea");
            Assert.That(view.LevelCard.HitoRowRect, Is.Null, "oculta, sin fila");
            Assert.That(view.LevelCard.HitoWindowRect, Is.Null, "oculta, sin ventanita");
            Assert.That(view.LevelCard.HitoSilhouette, Is.EqualTo(LevelCardView.HitoSilhouetteNone), "oculta, sin silueta");
            Assert.That(view.LevelCard.HitoLines, Is.EqualTo(0), "oculta, sin líneas");
        }

        [UnityTest]
        public IEnumerator Release_WithoutProvisional_TheLineGoesAlone_ButTheEncuentroKeepsItsWaters()
        {
            // P-M5 (MapArtRule): en release, sin el PNG del héroe, la silueta provisional no sale; la línea sí, sola y centrada. La
            // pieza por código de las dos aguas del Encuentro ya es definitiva.
            MapArtRule.ForceProvisional = false;
            yield return CardSetup.Load(60);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelCardView card = view.LevelCard;
            Assert.That(card.HitoText, Is.Not.Null);
            Assert.That(card.HitoSilhouette, Is.EqualTo(LevelCardView.HitoSilhouetteNone));
            Assert.That(card.HitoWindowRect, Is.Null, "sin silueta, sin ventanita");
            Assert.That(card.HitoLine.alignment, Is.EqualTo(TextAlignmentOptions.Midline));
            Rect panel = VictorySetup.CanvasRect(card.Panel, (RectTransform)view.Canvas.transform);
            Rect row = VictorySetup.CanvasRect(card.HitoRowRect!, (RectTransform)view.Canvas.transform);
            Assert.That(row.center.x, Is.EqualTo(panel.center.x).Within(1f), "la línea sola, centrada");
            CardSetup.AssertNoOverflow(view);
            CardSetup.AssertInsidePanel(view);

            boot.Flow!.Load(40);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(card.HitoSilhouette, Is.EqualTo(LevelCardView.HitoSilhouetteCode));
            Assert.That(card.HitoWindowRect, Is.Not.Null);

            // Con el PNG del héroe en su ranura, el PNG manda siempre, también en release.
            Sprite png = view.CardArt.IconSeed;
            StopBackdrop.HeroSpriteOverride = hero => hero == "bg_hero_anavilhanas" ? png : null;
            try
            {
                boot.Flow.Load(60);
                PlayModeHelpers.Step(boot, 300);
                Assert.That(card.HitoSilhouette, Is.EqualTo(LevelCardView.HitoSilhouettePng));
                Assert.That(card.HitoHeroSprite, Is.SameAs(png));
                AssertRow(view, "60 con el PNG");
            }
            finally
            {
                StopBackdrop.HeroSpriteOverride = null;
            }
        }

        [UnityTest]
        public IEnumerator Level30And50_Unchanged()
        {
            foreach (int level in new[] { 30, 50 })
            {
                yield return CardSetup.Load(level);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                LevelCardView card = view.LevelCard;
                string where = "nivel " + level;
                Assert.That(LevelCardView.HitoStopFor(level, view.RiverStops), Is.Null, where);
                Assert.That(card.HitoStop, Is.Null, where);
                Assert.That(card.HitoText, Is.Null, where);
                Assert.That(card.HitoRowRect, Is.Null, where);
                Assert.That(card.HitoSilhouette, Is.EqualTo(LevelCardView.HitoSilhouetteNone), where);

                // El 30 y el 50 son «normal» y sin estreno: no tienen tarjeta, ni antes ni ahora.
                Assert.That(card.Visible, Is.False, "sin tarjeta: " + where);
                Assert.That(boot.Flow!.Phase, Is.Not.EqualTo(FlowPhase.LevelCard), where);
                Assert.That(view.LastCardProbe, Is.Null, "sin tarjeta, sin sonda: " + where);
            }
        }

        [UnityTest]
        public IEnumerator TaggedCardThatIsNotAHito_Unchanged()
        {
            // Una tarjeta etiquetada que no es hito p20 (el 46, «Difícil» de p6, y el 52, de p12) sale igual que con el Show sin
            // parada: mismo alto y sin fila.
            foreach (int level in new[] { 46, 52 })
            {
                yield return CardSetup.Load(level);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                LevelCardView card = view.LevelCard;
                string where = "nivel " + level;
                Assert.That(card.Visible, Is.True, "tarjeta de dificultad: " + where);
                Assert.That(card.Card!.Level, Is.EqualTo(level), where);
                Assert.That(LevelCardView.HitoStopFor(level, view.RiverStops), Is.Null, where);
                Assert.That(card.HitoText, Is.Null, where);
                Assert.That(card.HitoRowRect, Is.Null, where);
                Assert.That(view.LastCardProbe, Does.Contain("\"hito\":null"), where);
                Assert.That(view.LastCardProbe, Does.Contain("\"hito_silhouette\":\"" + LevelCardView.HitoSilhouetteNone + "\""), where);
                Assert.That(view.LastCardProbe, Does.Contain("\"hito_omitted\":false"), where);
                float height = card.Panel.rect.height;
                card.Show(card.Card!, false, view.CanvasWidth, view.CardCanvasHeight, view.SafeTopDp, view.CardSafeBottom);
                Assert.That(card.Panel.rect.height, Is.EqualTo(height).Within(0.01f), "mismo alto que sin la fila: " + where);
                Assert.That(card.HitoText, Is.Null, where);
            }
        }

        [UnityTest]
        public IEnumerator NoPlaceName_NoRow()
        {
            // Sin el nombre del lugar en el idioma (en release, un nombre sin el visto bueno del asesor es «»), no hay fila: nunca
            // «Si lo superas, llegas a » colgando.
            yield return CardSetup.Load(60);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            RiverStopCatalog stops = view.RiverStops!;
            view.RiverStops = null;
            try
            {
                boot.Flow!.Load(60);
                PlayModeHelpers.Step(boot, 300);
                Assert.That(view.LevelCard.Visible, Is.True);
                Assert.That(view.LevelCard.HitoText, Is.Null, "sin catálogo de paradas, sin fila");
            }
            finally
            {
                view.RiverStops = stops;
            }

            LocCatalog catalog = Loc.Catalog;
            string locale = Loc.Locale;
            bool unapproved = Loc.ShowUnapproved;
            string? tag = Loc.RequestedNumberTag;
            Loc.Configure(catalog, locale, false, tag); // como en release: lo no aprobado no se muestra
            try
            {
                Assert.That(Loc.Fact(stops.At(6)!.NameKey), Is.Empty, "Anavilhanas está pendiente del asesor");
                boot.Flow!.Load(60);
                PlayModeHelpers.Step(boot, 300);
                Assert.That(view.LevelCard.Visible, Is.True);
                Assert.That(view.LevelCard.HitoStop, Is.SameAs(stops.At(6)));
                Assert.That(view.LevelCard.HitoText, Is.Null, "sin nombre aprobado, sin fila");
                Assert.That(view.LastCardProbe, Does.Contain("\"hito\":null"));
            }
            finally
            {
                Loc.Configure(catalog, locale, unapproved, tag);
            }
        }

        [UnityTest]
        public IEnumerator Level100_Fits_InTwoScreens_At100And130()
        {
            // Plan del Mapa, M2.5, punto 2: la tarjeta combinada del 100 (dificultad, tutorial de Aguas turbias, Luciérnaga y
            // regalo) con la fila tiene que caber a 360 × 640 y al 130 %; si no, se omite la fila.
            var report = new System.Text.StringBuilder();
            foreach (Vector4 screen in new[] { HudLabelTests.Screen780, HudLabelTests.Screen640 })
            {
                GameScreen.SimulatedScreen = screen;
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    yield return CardSetup.Load(100);
                    GameBootstrap boot = GameBootstrap.Current!;
                    GameScreen view = boot.View!;
                    LevelCardView card = view.LevelCard;
                    if (scale != 1f)
                    {
                        view.CardFontScale = scale;
                        PlayModeHelpers.Step(boot, 50);
                    }

                    string where = "100 a " + screen.x + " × " + screen.y + " px y " + scale.ToString("0.0", CultureInfo.InvariantCulture);
                    Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Combined), where);
                    Assert.That(card.HitoStop!.Id, Is.EqualTo("amacayacu"), where);
                    Assert.That(card.HitoOmitted, Is.False, "cabe: " + where);
                    Assert.That(card.HitoText, Is.EqualTo(UiText.MapHitoPreview(card.HitoStop)), where);
                    Assert.That(card.TutorialText, Is.EqualTo(UiText.TutorialMurky), "el tutorial sigue entero: " + where);
                    Assert.That(card.FireflyText, Is.Not.Null, where);
                    AssertRow(view, where, scale);
                    CardSetup.AssertPanelOnScreen(view);
                    report.Append(where).Append(": ").Append(card.Panel.rect.height.ToString("0", CultureInfo.InvariantCulture))
                        .Append(" dp de ").Append(view.CardCanvasHeight.ToString("0", CultureInfo.InvariantCulture)).Append("; ");
                }
            }

            Debug.Log("AF_M25 tarjeta del 100 con la fila: " + report);
        }

        [UnityTest]
        public IEnumerator Level100_Fits_InALiteral360x640Canvas_At130_OrTheRowIsOmitted()
        {
            // El lienzo del juego mide siempre al menos 360 × 780 dp (02 §5); aquí, la vista sola en 360 × 640 dp literales con la
            // zona segura de 32 y 16 dp. Y, en el límite: el alto libre (el lienzo menos la zona segura y 16 dp de aire) igual al
            // de la tarjeta con la fila, la fila sale; con 1 dp menos, o justo el de la tarjeta sin ella, se omite y la tarjeta queda
            // como sin ella.
            yield return CardSetup.Load(100);
            GameScreen view = GameBootstrap.Current!.View!;
            LevelCardView card = view.LevelCard;
            LevelCardInfo info = card.Card!;
            RiverStop stop = card.HitoStop!;
            card.FontScale = 1.3f;
            card.Show(info, stop, "white", false, 360f, 640f, 32f, 16f);
            Assert.That(card.HitoOmitted, Is.False, "el 100 cabe en 360 × 640 dp al 130 %");
            Assert.That(card.HitoText, Is.Not.Null);
            float height = card.Panel.rect.height;
            Assert.That(height, Is.LessThanOrEqualTo(640f - 32f - 16f - 16f), "dentro del alto libre");
            Debug.Log("AF_M25 tarjeta del 100 al 130 % en 360 × 640 dp: " + height.ToString("0", CultureInfo.InvariantCulture) + " dp");

            card.Show(info, null, string.Empty, false, 360f, 640f, 32f, 16f);
            float bare = card.Panel.rect.height;
            Assert.That(height, Is.GreaterThan(bare), "la fila añade su alto");
            const float reserved = 32f + 16f + 16f; // zona segura arriba y abajo, y el aire de la colocación

            // El alto libre, justo el de la tarjeta con la fila: cabe y la fila sale.
            card.Show(info, stop, "white", false, 360f, height + reserved, 32f, 16f);
            Assert.That(card.HitoOmitted, Is.False, "con el alto libre justo para la tarjeta con la fila, la fila sale");
            Assert.That(card.HitoText, Is.Not.Null);
            Assert.That(card.HitoRowRect, Is.Not.Null);
            Assert.That(card.Panel.rect.height, Is.EqualTo(height).Within(0.01f), "la misma tarjeta con la fila");

            // 1 dp menos: ya no cabe y la fila se omite.
            card.Show(info, stop, "white", false, 360f, height + reserved - 1f, 32f, 16f);
            Assert.That(card.HitoOmitted, Is.True, "con 1 dp menos, la fila se omite");
            Assert.That(card.HitoText, Is.Null);
            Assert.That(card.Panel.rect.height, Is.EqualTo(bare).Within(0.01f), "y la tarjeta queda como sin ella");

            // El alto libre, justo el de la tarjeta sin la fila.
            card.Show(info, stop, "white", false, 360f, bare + reserved, 32f, 16f);
            Assert.That(card.HitoOmitted, Is.True, "sin sitio, la fila se omite");
            Assert.That(card.HitoText, Is.Null);
            Assert.That(card.HitoRowRect, Is.Null);
            Assert.That(card.Panel.rect.height, Is.EqualTo(bare).Within(0.01f), "y la tarjeta queda como sin ella");
        }

        [UnityTest]
        public IEnumerator Probe_Relayout_RepublishesTheCardLayout()
        {
            // Al recolocar la tarjeta abierta (otra escala u otra pantalla), AF_LAYOUT level_card vuelve a salir con los
            // rectángulos de ahora, el de la tarjeta y el de la fila (level_card_hito), para que quien toca por adb no use los de
            // antes; y AF_STATE level_card, con la escala nueva.
            using (var log = new AfLayoutLog())
            {
                yield return CardSetup.Load(40);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                LevelCardView card = view.LevelCard;
                string? first = log.LastOf("level_card", 40);
                Assert.That(first, Is.Not.Null, "AF_LAYOUT level_card al terminar la entrada");
                AssertCardLayout(first!, card, "al entrar");

                log.Clear();
                view.CardFontScale = 1.3f;
                Assert.That(log.AllOf("level_card", 40).Count, Is.EqualTo(1), "una línea nueva al cambiar la escala");
                string scaled = log.LastOf("level_card", 40)!;
                AssertCardLayout(scaled, card, "al 130 %");
                Assert.That(AfLayoutLog.Button(scaled, "level_card")!.Value.height, Is.GreaterThan(AfLayoutLog.Button(first!, "level_card")!.Value.height), "la tarjeta crece al 130 %");
                Assert.That(view.LastCardProbe, Does.Contain("\"scale\":1.30"));

                log.Clear();
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
                PlayModeHelpers.Step(boot, 50);
                Assert.That(log.AllOf("level_card", 40).Count, Is.EqualTo(1), "una línea nueva al cambiar la pantalla");
                AssertCardLayout(log.LastOf("level_card", 40)!, card, "en 16:9");
            }
        }

        // La línea AF_LAYOUT level_card publica la tarjeta y su fila del hito tal como se ven ahora.
        private static void AssertCardLayout(string line, LevelCardView card, string where)
        {
            Assert.That(AfLayoutLog.ButtonIds(line), Is.EqualTo(new[] { "level_card", "level_card_hito" }), where + ": " + line);
            Assert.That(AfLayoutLog.Button(line, "level_card"), Is.EqualTo(AfLayoutLog.Published(card.Panel)), "la tarjeta: " + where);
            Assert.That(AfLayoutLog.Button(line, "level_card_hito"), Is.EqualTo(AfLayoutLog.Published(card.HitoRowRect!)), "la fila: " + where);
        }

        [UnityTest]
        public IEnumerator Hitos_FontScale130_NoOverflow()
        {
            // 02 §12: hasta el 130 % sin desbordes ni recortes; la línea crece (no se encoge) y la ventanita no cambia.
            foreach (Vector4 screen in new[] { HudLabelTests.Screen780, HudLabelTests.Screen640 })
            {
                GameScreen.SimulatedScreen = screen;
                foreach (int level in new[] { 40, 160 })
                {
                    yield return CardSetup.Load(level);
                    GameBootstrap boot = GameBootstrap.Current!;
                    GameScreen view = boot.View!;
                    LevelCardView card = view.LevelCard;
                    float baseSp = card.HitoBaseSp;
                    float baseHeight = card.Panel.rect.height;
                    view.CardFontScale = 1.3f;
                    PlayModeHelpers.Step(boot, 50);
                    string where = "nivel " + level + " a " + screen.x + " × " + screen.y;
                    Assert.That(card.HitoLine.fontSize, Is.EqualTo(baseSp * 1.3f).Within(0.01f), where);
                    Assert.That(card.Panel.rect.height, Is.GreaterThan(baseHeight), where);
                    Assert.That(view.LastCardProbe, Does.Contain("\"scale\":1.30"), "la sonda, con la escala nueva: " + where);
                    Assert.That(view.LastCardProbe, Does.Contain("\"hito_omitted\":false"), where);
                    AssertRow(view, where, 1.3f);
                    CardSetup.AssertPanelOnScreen(view);
                }
            }
        }

        [UnityTest]
        public IEnumerator HitoCards_SurfacesStayShared()
        {
            // Memoria (revisión de T13): con la fila del hito, las superficies de madera de las tarjetas de 1-200 siguen saliendo
            // de la escala de altos compartidos (ModalKit.SharedSurfaceHeight), estiradas como mucho √1,25 veces. La fila lleva los
            // hitos a otros dos altos de la escala [P]: 225 dp (los del 40 al 180) y 352 (el 100 con el regalo de la Luciérnaga),
            // así que en 1-200 hay como mucho 5 superficies, no 3 (las 144, 180 y 281 dp de siempre siguen igual).
            yield return CardSetup.Load(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelCardView card = view.LevelCard;
            LevelCatalog catalog = boot.Flow!.Catalog;
            var surfaces = new SortedSet<int>();
            var hitoSurfaces = new SortedSet<int>();
            var heights = new SortedSet<int>();
            float worst = 1f;
            for (int level = 1; level <= 200; level++)
            {
                LevelDefinition? definition = catalog.Find(level);
                if (definition == null)
                {
                    continue;
                }

                for (int pass = 0; pass < (level == 100 ? 2 : 1); pass++)
                {
                    LevelCardInfo? info = LevelCardInfo.For(definition, level, AF.Economy.EconomyConfig.Defaults, level == 100 && pass == 0);
                    if (info == null)
                    {
                        continue;
                    }

                    RiverStop? stop = LevelCardView.HitoStopFor(level, view.RiverStops);
                    card.Show(info, stop, "white", false, view.CanvasWidth, view.CardCanvasHeight, view.SafeTopDp, view.CardSafeBottom);
                    Assert.That(card.HitoText != null, Is.EqualTo(stop != null), "fila del hito en el " + level);
                    float body = card.Panel.rect.height - 16f;
                    Sprite surface = card.Surface.sprite;
                    float surfaceDp = surface.rect.height * 100f / surface.pixelsPerUnit;
                    Assert.That(surfaceDp, Is.EqualTo(ModalKit.SharedSurfaceHeight(body)).Within(0.5f), "de la escala de altos en el " + level);
                    worst = Mathf.Max(worst, Mathf.Max(body / surfaceDp, surfaceDp / body));
                    surfaces.Add(Mathf.RoundToInt(surfaceDp));
                    if (stop != null)
                    {
                        hitoSurfaces.Add(Mathf.RoundToInt(surfaceDp));
                    }

                    heights.Add(Mathf.RoundToInt(card.Panel.rect.height));
                }
            }

            Debug.Log("AF_M25 altos de las tarjetas de 1-200 con la fila del hito: " + string.Join(", ", heights) + " dp; superficies "
                + string.Join(", ", surfaces) + " dp (las de los hitos: " + string.Join(", ", hitoSurfaces) + "); estirado máximo "
                + worst.ToString("F3", CultureInfo.InvariantCulture));
            Assert.That(surfaces.Count, Is.LessThanOrEqualTo(5), "superficies distintas en 1-200 con la fila del hito");
            Assert.That(hitoSurfaces.Count, Is.LessThanOrEqualTo(3), "los hitos, en pocos altos");
            Assert.That(worst, Is.LessThanOrEqualTo(Mathf.Sqrt(ModalKit.SharedSurfaceStep) + 0.005f), "estirado máximo");
        }

        // La fila: ventanita de 64 × 22 dp de vidrio (más su marco) a la izquierda de la línea, las dos dentro de la tarjeta y bajo
        // todo lo demás; la línea en Nunito Bold y tinta, a 14 o 12 sp por la escala, en 2 líneas como mucho a 100 %, sin
        // desbordes.
        private static void AssertRow(GameScreen view, string where, float scale = 1f)
        {
            LevelCardView card = view.LevelCard;
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            Assert.That(card.HitoRowRect, Is.Not.Null, where);
            TextMeshProUGUI line = card.HitoLine;
            Assert.That(line.font, Is.SameAs(UiFonts.Font(TextStyle.PlainBold)), "Nunito Bold: " + where);
            Assert.That(line.color, Is.EqualTo((Color)Palette.Ink), "en tinta: " + where);
            Assert.That(card.HitoBaseSp, Is.EqualTo(14f).Or.EqualTo(12f), where);
            Assert.That(line.fontSize, Is.EqualTo(card.HitoBaseSp * scale).Within(0.01f), where);
            Assert.That(line.fontSize, Is.GreaterThanOrEqualTo(12f), "nunca menos de 12 sp: " + where);
            int lines = CardSetup.LineCount(line);
            Assert.That(card.HitoLines, Is.EqualTo(lines), "la medida de la vista coincide con TMP: " + where);
            if (scale == 1f)
            {
                Assert.That(lines, Is.InRange(1, LevelCardView.MaxHitoLines), "2 líneas como mucho: " + where);
            }

            Assert.That(line.textInfo.characterCount, Is.EqualTo(line.text.Length), "ningún carácter recortado: " + where);
            CardSetup.AssertNoOverflow(view);
            CardSetup.AssertInsidePanel(view);

            Rect panel = VictorySetup.CanvasRect(card.Panel, root);
            Rect row = VictorySetup.CanvasRect(card.HitoRowRect!, root);
            Assert.That(row.xMin, Is.GreaterThanOrEqualTo(panel.xMin + 7.5f), "la fila dentro del marco: " + where);
            Assert.That(row.xMax, Is.LessThanOrEqualTo(panel.xMax - 7.5f), where);
            Assert.That(row.yMax, Is.LessThanOrEqualTo(panel.yMax - 7.5f), where);
            Assert.That(row.center.x, Is.EqualTo(panel.center.x).Within(1f), "centrada: " + where);

            // Abajo: debajo de todo lo demás de la tarjeta.
            foreach (RectTransform? above in new[] { card.HeaderRect, card.RewardRowRect, card.GiftRect, card.MedallionRect })
            {
                if (above != null)
                {
                    Assert.That(VictorySetup.CanvasRect(above, root).yMax, Is.LessThanOrEqualTo(row.yMin + 0.5f), above.name + " encima de la fila: " + where);
                }
            }

            foreach (TextMeshProUGUI t in card.VisibleTexts())
            {
                if (t != line)
                {
                    Assert.That(VictorySetup.CanvasRect(t.rectTransform, root).yMax, Is.LessThanOrEqualTo(row.yMin + 0.5f), "«" + t.text + "» encima de la fila: " + where);
                }
            }

            for (int i = 0; i < card.IconCount; i++)
            {
                Assert.That(VictorySetup.CanvasRect(card.IconRect(i), root).yMax, Is.LessThanOrEqualTo(row.yMin + 0.5f), "icono encima de la fila: " + where);
            }

            RectTransform? window = card.HitoWindowRect;
            if (window != null)
            {
                Rect glass = VictorySetup.CanvasRect(card.HitoGlass, root);
                Assert.That(glass.width, Is.EqualTo(LevelCardView.HitoSilhouetteWidthDp).Within(0.01f), "silueta de 64 dp: " + where);
                Assert.That(glass.height, Is.EqualTo(LevelCardView.HitoSilhouetteHeightDp).Within(0.01f), "silueta de 22 dp: " + where);
                Rect w = VictorySetup.CanvasRect(window, root);
                Rect text = VictorySetup.CanvasRect(line.rectTransform, root);
                Assert.That(w.xMax, Is.LessThanOrEqualTo(text.xMin), "la ventanita a la izquierda de la línea: " + where);
                Assert.That(w.yMin, Is.GreaterThanOrEqualTo(row.yMin - 0.5f), where);
                Assert.That(w.yMax, Is.LessThanOrEqualTo(row.yMax + 0.5f), where);
                // La malla, centrada en el vidrio: el héroe ampliado (HitoHeroZoom) sobresale por los lados y el vidrio lo recorta.
                Assert.That(card.HitoGlass.GetComponent<UnityEngine.UI.RectMask2D>(), Is.Not.Null, "el vidrio recorta la escena: " + where);
                float half = LevelCardView.HitoSilhouetteWidthDp * 0.5f * LevelCardView.HitoHeroZoom;
                float mid = LevelCardView.HitoSilhouetteWidthDp * 0.5f;
                Assert.That(card.HitoScene.MinX, Is.GreaterThanOrEqualTo(mid - half - 0.01f), "la malla de la ventanita, en torno al vidrio: " + where);
                Assert.That(card.HitoScene.MaxX, Is.LessThanOrEqualTo(mid + half + 0.01f), where);
                Assert.That(card.HitoScene.MinX, Is.LessThanOrEqualTo(0.01f), "el cielo llena el vidrio: " + where);
                Assert.That(card.HitoScene.MaxX, Is.GreaterThanOrEqualTo(LevelCardView.HitoSilhouetteWidthDp - 0.01f), where);
            }
        }
    }
}
