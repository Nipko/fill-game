#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Tarea FX16b (D-032, puntos 2 y 6; decisión del dueño del 2026-10-20), en la escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item><description>Victoria compacta en 16:9 (punto 2; <c>GameScreen.VictoryCompact</c>): a 360 × 640 dp, al 100 % y al 130 %,
    /// en es-419 y en en-XA, en las victorias que midió DC3 (el 127, el 34, el 39 con la Cosecha doble y el chip del 40, y el 20
    /// con SingleKey) y en las del 20 y el 80 con su llegada, la cinta, la ranura del Dato del río y «Siguiente» quedan bajo la
    /// cabecera del HUD; ningún texto de la victoria baja de 12 sp × la escala, ningún botón de 48 dp, «Inicio» y «Siguiente» no
    /// se pisan, nada se sale del panel y «Siguiente» queda a la misma altura en todas las victorias de cada idioma y escala
    /// (Q7-A; en el escalón 3, con «Ver mapa», más estrecho y con el mismo canto derecho [P]). La compactación va por escalones y
    /// solo cuando hace falta: en es-419 al 100 % con la política del Dato del río por defecto no hay ninguno (con SingleKey, que no
    /// es la de por defecto, sí), y a 360 × 780 dp ninguno en ningún caso («sin cambiar 360 × 780»).</description></item>
    /// <item><description>El botón del inicio tras acabar el contenido (punto 6): con el Reto del río desbloqueado, «Jugar · Reto
    /// del río» (<c>play_daily</c>), que abre el Reto; sin él, «Jugar» (<c>play</c>), que abre «El río sigue creciendo», como
    /// antes; también tras ganar el 700.</description></item>
    /// </list>
    /// No necesitan gráficos. Las capturas son las de <see cref="Fx16bSnapshotTests"/>.
    /// </summary>
    public sealed class Fx16bTests
    {
        // Las victorias de 16:9 que midió DC3 (el 20 con SingleKey en negativo) y las del 20 y el 80 con su llegada.
        internal static readonly int[] Cases = { 127, 34, 39, -20, 20, 80 };

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
            if (!Loc.IsConfigured || Loc.Locale != LocLocale.SpanishLatinAmerica)
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        // ============================================================================================ punto 2: victoria compacta

        [UnityTest]
        public IEnumerator Victory16x9_RibbonSlotAndNext_UnderTheHeader_NothingBelow12Sp_Or48Dp_AndNextStaysPut(
            [Values(LocLocale.SpanishLatinAmerica, LocLocale.Pseudo)] string locale, [Values(1f, 1.3f)] float scale)
        {
            float next = -1f;
            int compact = -1;
            foreach (int code in Cases)
            {
                int level = code < 0 ? -code : code;
                bool singleKey = code < 0;
                string what = level + (singleKey ? " con SingleKey" : string.Empty) + " en " + locale + " al " + Mathf.RoundToInt(scale * 100f) + " % a 360 × 640";
                yield return FactLineTests.Win(level, v =>
                {
                    if (singleKey)
                    {
                        v.RiverPolicyOverride = RiverFactPolicy.SingleKey;
                    }
                }, locale, scale, true);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                PlayModeHelpers.Step(boot, 1600);
                Assert.That(Loc.Locale, Is.EqualTo(locale), what);
                Assert.That(view.CardCanvasHeight, Is.EqualTo(GameScreen.MinHeight).Within(0.01f), what + ": 360 × 640 dp");
                Assert.That(view.VictorySlot.ContentDp, Is.GreaterThan(0f), what + ": con la ranura del Dato del río");
                if (level == 39)
                {
                    Assert.That(view.DoubleHarvestShown, Is.True, what + ": Cosecha doble");
                    Assert.That(view.NextChip.Visible, Is.True, what + ": chip del 40");
                }

                TestContext.WriteLine(what + ": escalón " + view.VictoryCompact + ", panel " + view.VictoryPanelHeight + " dp (tope "
                    + view.VictoryPanelBudgetDp + "; el más alto posible " + view.VictoryWorstPanelDp(view.VictoryCompact) + "), cinta "
                    + (view.VictoryPanelTopDp - 38f) + ", cabecera " + view.SlothHeaderBottomDp + ", Siguiente " + view.NextTopDp
                    + ", ranura " + view.VictorySlot.ContentDp + " dp");
                AssertCompactVictory(view, what);
                if (view.Arrival != null && view.Arrival.Kind != ArrivalKind.None)
                {
                    VictoryArrivalTests.AssertFits(view, what);
                }

                // «Ver mapa» con la llegada (D-013; 06 §4.5): siempre fuera de la fila; en la fila del escalón 3, si «Siguiente» conserva
                // sitio para su etiqueta (si no, no sale: pregunta abierta de D-032).
                bool arrival = view.Arrival != null && view.Arrival.Stop != null
                    && (view.Arrival.Kind == ArrivalKind.Arrival || view.Arrival.Kind == ArrivalKind.Place);
                if (arrival && view.RiverMapOpenable)
                {
                    TestContext.WriteLine(what + ": «Ver mapa» " + (view.VictoryMapShown ? "sale" : "NO sale (no cabe en la fila)"));
                    Assert.That(view.VictoryMapShown, Is.EqualTo(view.VictoryCompact < GameScreen.VictoryCompactRow || view.VictoryRowFitsMap()),
                        what + ": «Ver mapa» sale si cabe");
                    Assert.That(view.VictoryMapDropped, Is.EqualTo(!view.VictoryMapShown), what);
                }

                if (view.FruitLine != null && view.FruitLine.Shown)
                {
                    FactLineTests.AssertFits(view, what);
                }

                // La compactación solo cuando hace falta: el escalón de antes no cabía.
                int step = view.VictoryCompact;
                if (step > 0)
                {
                    Assert.That(view.VictoryWorstPanelDp(step - 1), Is.GreaterThan(view.VictoryPanelBudgetDp), what + ": el escalón anterior no cabía");
                }

                if (locale == LocLocale.SpanishLatinAmerica && scale < 1.01f && !singleKey)
                {
                    // Hasta B1b, la victoria de siempre; con las barras de la Crecida y la Canasta del panel único (D-033, punto 10),
                    // en una fila a media anchura, solo los márgenes justos del escalón 1 (medido: 476 dp de un tope de 480).
                    Assert.That(step, Is.LessThanOrEqualTo(GameScreen.VictoryCompactMargins), what + ": en es-419 al 100 %, como mucho los márgenes justos");
                    Assert.That(view.VictoryBarsDropped, Is.False, what + ": con las barras");
                }

                if (!singleKey)
                {
                    // Q7-A: «Siguiente» a la misma altura en todas las victorias de este idioma y esta escala (SingleKey cambia la ranura).
                    if (next < 0f)
                    {
                        next = view.NextTopDp;
                        compact = step;
                    }
                    else
                    {
                        Assert.That(view.NextTopDp, Is.EqualTo(next).Within(0.01f), what + ": Siguiente a la misma altura");
                        Assert.That(step, Is.EqualTo(compact), what + ": el mismo escalón");
                    }
                }

                GameScreen.SimulatedScreen = null;
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
            }
        }

        [UnityTest]
        public IEnumerator Victory780_NeverCompact([Values(LocLocale.SpanishLatinAmerica, LocLocale.Pseudo)] string locale, [Values(1f, 1.3f)] float scale)
        {
            foreach (int level in new[] { 39, 20 })
            {
                string what = level + " en " + locale + " al " + Mathf.RoundToInt(scale * 100f) + " % a 360 × 780";
                GameScreen.SimulatedScreen = HudLabelTests.Screen780;
                yield return FactLineTests.Win(level, null, locale, scale);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                PlayModeHelpers.Step(boot, 1600);
                Assert.That(view.CardCanvasHeight, Is.EqualTo(GameScreen.ReferenceHeight).Within(0.01f), what);
                // 360 × 780 no compacta (D-032, punto 2), salvo, desde las barras de B1b (D-033, punto 10), en-XA al 130 %: los márgenes
                // justos del escalón 1 [P] (pregunta abierta de B1b).
                bool pseudo130 = locale == LocLocale.Pseudo && scale > 1.01f;
                Assert.That(view.VictoryCompact, Is.EqualTo(pseudo130 ? GameScreen.VictoryCompactMargins : 0), what + ": la victoria de siempre");
                Assert.That(view.VictoryBarsDropped, Is.False, what + ": con las barras");
                Assert.That(view.VictorySlotWidthDp, Is.EqualTo(view.VictoryPanelWidthDp - (2f * VictoryLineSlot.SideDp)).Within(0.01f), what);
                Assert.That(view.VictorySlot.ReserveDp, Is.EqualTo(view.VictorySlot.ReserveFor(view.VictorySlot.ContentDp)).Within(0.01f), what);
                Assert.That(view.HomeBottomDp, Is.GreaterThan(view.NextTopDp + ((RectTransform)view.NextButton.transform).rect.height), what + ": Inicio bajo Siguiente");
                if (level == 20)
                {
                    Assert.That(view.VictoryMapShown, Is.True, what + ": «Ver mapa» con la llegada");
                }

                FactLineTests.AssertRibbonUnderTheHeader(view, what);
                GameScreen.SimulatedScreen = null;
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
            }
        }

        [UnityTest]
        public IEnumerator Victory16x9_RowStep_HomeLeftOfNext_VerMapaBetween_AndTheChipOverNext()
        {
            yield return FactLineTests.Win(20, null, LocLocale.SpanishLatinAmerica, 1.3f, true);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(view.VictoryCompact, Is.EqualTo(GameScreen.VictoryCompactRow), "es-419 al 130 % a 640: la fila");
            RectTransform home = (RectTransform)view.HomeButton.transform;
            RectTransform next = (RectTransform)view.NextButton.transform;
            Assert.That(home.anchoredPosition.x + home.rect.width, Is.LessThanOrEqualTo(next.anchoredPosition.x + 0.01f), "Inicio a la izquierda de Siguiente");
            Assert.That(Mathf.Abs((-home.anchoredPosition.y + (home.rect.height * 0.5f)) - (-next.anchoredPosition.y + (next.rect.height * 0.5f))),
                Is.LessThan(0.6f), "en la misma fila, centrados a lo alto");
            Assert.That(view.Arrival!.Kind, Is.Not.EqualTo(ArrivalKind.None), "con su llegada");
            Assert.That(view.RiverMapOpenable, Is.True, "con el Mapa");

            // «Ver mapa» (D-013; 06 §4.5), entre «Inicio» y «Siguiente», en la misma fila: la fila no cambia de alto.
            Assert.That(view.VictoryMapShown, Is.True, "es-419 al 130 % a 640: «Ver mapa» cabe en la fila");
            RectTransform map = (RectTransform)view.VictoryMapButton.transform;
            Assert.That(home.anchoredPosition.x + home.rect.width, Is.LessThanOrEqualTo(map.anchoredPosition.x + 0.01f), "Inicio a la izquierda de Ver mapa");
            Assert.That(map.anchoredPosition.x + map.rect.width, Is.LessThanOrEqualTo(next.anchoredPosition.x + 0.01f), "Ver mapa a la izquierda de Siguiente");
            Assert.That(Mathf.Abs((-map.anchoredPosition.y + (map.rect.height * 0.5f)) - (-next.anchoredPosition.y + (next.rect.height * 0.5f))),
                Is.LessThan(0.6f), "Ver mapa en la misma fila");
            Assert.That(map.rect.height, Is.GreaterThanOrEqualTo(48f - 0.01f));
            Assert.That(view.VictoryNextCenterXDp, Is.EqualTo(next.anchoredPosition.x + (next.rect.width * 0.5f)).Within(0.01f));
            float nextFace = ((RectTransform)view.NextButton.transform).rect.height;
            Assert.That(nextFace, Is.GreaterThanOrEqualTo(48f));
            foreach (Button b in new[] { view.NextButton, view.HomeButton, view.VictoryMapButton })
            {
                TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>(true);
                label.ForceMeshUpdate();
                if (b == view.NextButton)
                {
                    Assert.That(label.isTextOverflowing, Is.False, "«Siguiente» cabe en su botón");
                }

                // A lo ancho, en una línea y a su tamaño más pequeño (el autoajuste no baja de ahí; los secundarios de madera tienen 48 dp
                // de alto en todas las victorias: su alto no cambia aquí).
                float wide = UiBuilder.MinTextWidth(label);
                Assert.That(wide, Is.LessThanOrEqualTo(label.rectTransform.rect.width + 0.5f), "«" + label.text + "» cabe a lo ancho en su botón");
                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * UiBuilder.TextScale) - 0.01f), "«" + label.text + "» ≥ 12 sp");
            }

            AssertCompactVictory(view, "20 en es-419 al 130 % a 640");
            float nextTop20 = view.NextTopDp;
            float nextRight20 = next.anchoredPosition.x + next.rect.width;

            // En el 39, el chip del 40 cuelga sobre el centro de «Siguiente».
            yield return FactLineTests.Win(39, null, LocLocale.SpanishLatinAmerica, 1.3f, true);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(view.NextChip.Visible, Is.True);
            next = (RectTransform)view.NextButton.transform;
            Assert.That(view.VictoryNextCenterXDp, Is.EqualTo(next.anchoredPosition.x + (next.rect.width * 0.5f)).Within(0.01f), "el chip, sobre Siguiente");
            AssertCompactVictory(view, "39 en es-419 al 130 % a 640");

            // Q7-A en el escalón 3: «Siguiente» a la misma altura y con el mismo canto derecho en la victoria con «Ver mapa» (el 20) y
            // en la de sin él (el 39); con «Ver mapa» empieza más a la derecha y es más estrecho [P] (D-032, punto 2).
            Assert.That(view.VictoryCompact, Is.EqualTo(GameScreen.VictoryCompactRow), "39 en es-419 al 130 % a 640: la fila");
            Assert.That(view.VictoryMapShown, Is.False, "el 39, sin llegada");
            Assert.That(view.NextTopDp, Is.EqualTo(nextTop20).Within(0.01f), "Siguiente a la misma altura en el 20 y el 39");
            Assert.That(next.anchoredPosition.x + next.rect.width, Is.EqualTo(nextRight20).Within(0.01f), "Siguiente con el mismo canto derecho en el 20 y el 39");
        }

        // El marco del panel de victoria (dp) y el aire mínimo del texto de la línea de fruta dentro de él (revisión de FX16b).
        private const float FruitTextFrameDp = 8f;
        private const float FruitTextAirDp = 8f;

        // Lo de la victoria compacta (D-032, punto 2): la cinta, la ranura y Siguiente bajo la cabecera; nada se sale del panel;
        // ningún texto bajo 12 sp × la escala ni botón bajo 48 dp; Inicio y Siguiente sin pisarse.
        internal static void AssertCompactVictory(GameScreen view, string what)
        {
            float header = view.SlothHeaderBottomDp;
            FactLineTests.AssertRibbonUnderTheHeader(view, what + ": la cinta, bajo la cabecera");
            Assert.That(view.NextTopDp, Is.GreaterThanOrEqualTo(header), what + ": Siguiente, bajo la cabecera");
            Assert.That(view.SlotTopDp, Is.GreaterThanOrEqualTo(header), what + ": la ranura, bajo la cabecera");
            Assert.That(view.SlotTopDp + view.VictorySlot.ReserveDp, Is.LessThanOrEqualTo(view.VictoryPanelBottomDp + 0.01f), what + ": la ranura, dentro del panel");
            Assert.That(view.VictoryPanelHeight, Is.LessThanOrEqualTo(view.VictoryPanelBudgetDp + 0.01f), what + ": el panel cabe");

            var buttons = new List<Button> { view.NextButton, view.HomeButton };
            if (view.DoubleHarvestShown)
            {
                buttons.Add(view.DoubleHarvestButton);
            }

            if (view.VictoryMapShown)
            {
                buttons.Add(view.VictoryMapButton);
            }

            var rects = new List<Rect>();
            foreach (Button b in buttons)
            {
                RectTransform rt = (RectTransform)b.transform;
                Assert.That(rt.rect.height, Is.GreaterThanOrEqualTo(48f - 0.01f), what + ": " + b.name + " ≥ 48 dp de alto");
                Assert.That(rt.rect.width, Is.GreaterThanOrEqualTo(48f - 0.01f), what + ": " + b.name + " ≥ 48 dp de ancho");
                var r = new Rect(rt.anchoredPosition.x, -rt.anchoredPosition.y, rt.rect.width, rt.rect.height);
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.01f), what + ": " + b.name + " dentro del panel");
                Assert.That(r.xMax, Is.LessThanOrEqualTo(view.VictoryPanelWidthDp + 0.01f), what + ": " + b.name + " dentro del panel");
                Assert.That(r.yMax, Is.LessThanOrEqualTo(view.VictoryPanelHeight + 0.01f), what + ": " + b.name + " dentro del panel");
                foreach (Rect other in rects)
                {
                    bool overlap = r.xMin < other.xMax - 0.01f && other.xMin < r.xMax - 0.01f && r.yMin < other.yMax - 0.01f && other.yMin < r.yMax - 0.01f;
                    Assert.That(overlap, Is.False, what + ": " + b.name + " no pisa otro botón");
                }

                rects.Add(r);
            }

            // El texto de la línea de fruta (sin placa propia) a 8 dp o más del borde interior del panel (su marco, 8 dp): 16 dp del
            // canto, también cuando la ranura se ensancha en el escalón 2 (FactLine.InsetDp).
            if (view.FruitLine != null && view.FruitLine.Shown && view.FruitLine.Root != null)
            {
                RectTransform panel = view.VictoryPanelRect;
                Rect pr = panel.rect;
                foreach (TextMeshProUGUI t in view.FruitLine.Root.GetComponentsInChildren<TextMeshProUGUI>(false))
                {
                    if (string.IsNullOrEmpty(t.text) || !t.enabled)
                    {
                        continue;
                    }

                    t.ForceMeshUpdate();
                    Bounds b = t.textBounds;
                    Vector3 lo = panel.InverseTransformPoint(t.rectTransform.TransformPoint(b.min));
                    Vector3 hi = panel.InverseTransformPoint(t.rectTransform.TransformPoint(b.max));
                    float air = Mathf.Min(lo.x - pr.xMin, pr.xMax - hi.x) - FruitTextFrameDp;
                    Assert.That(air, Is.GreaterThanOrEqualTo(FruitTextAirDp - 0.5f), what + ": «" + t.name + "» de la línea de fruta, a " + air + " dp del marco");
                }
            }

            float min = (UiBuilder.MinTextSp * UiBuilder.TextScale) - 0.01f;
            foreach (TextMeshProUGUI t in view.VictoryPanelRect.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                if (string.IsNullOrEmpty(t.text) || !t.enabled)
                {
                    continue;
                }

                t.ForceMeshUpdate();
                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(min), what + ": «" + t.name + "» ≥ 12 sp");
            }
        }

        // ============================================================================================ punto 6: el botón del inicio

        [UnityTest]
        public IEnumerator AfterTheContent_WithTheRetoUnlocked_PlayIsJugarRetoDelRio_AndOpensTheReto()
        {
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            yield return LoadAfter700();
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.App.PlayLevelAvailable, Is.False, "el 701 no está publicado");
            Assert.That(boot.DailyPlayUnlocked, Is.True, "max_level 700 ≥ live_unlock_level_daily");
            Assert.That(lobby.PlayMode, Is.EqualTo(PlayButtonMode.Daily));
            Assert.That(lobby.PlayLabel, Is.EqualTo("Jugar · Reto del río"));
            Assert.That(lobby.PlayLabel, Is.EqualTo(UiText.PlayDaily));

            lobby.PlayButton.onClick.Invoke();
            Assert.That(boot.DailyScreen!.IsOpen, Is.True, "abre el Reto, como su tarjeta");
            Assert.That(boot.DailyScreen.Flow!.IsDaily, Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "no «El río sigue creciendo»");
            Assert.That(lobby.OverlayVisible, Is.False);
            Assert.That(boot.Flow!.HasLevel && boot.Flow.LevelNumber > 700, Is.False, "ningún 701");
        }

        [UnityTest]
        public IEnumerator AfterTheContent_WithoutTheReto_PlayIsJugar_AndOpensTheRiverKeepsGrowing()
        {
            GameBootstrap.DailyPoolDisabledOverride = true; // sin el Reto en el build (D-008)
            yield return LoadAfter700();
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(boot.DailyPlayUnlocked, Is.False);
            Assert.That(lobby.PlayMode, Is.EqualTo(PlayButtonMode.ContentEnd));
            Assert.That(lobby.PlayLabel, Is.EqualTo("Jugar"), "la clave play, ya aprobada");
            Assert.That(lobby.PlayLabel, Is.EqualTo(UiText.Play));

            lobby.PlayButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.ContentEnd), "«El río sigue creciendo», como antes");
            Assert.That(lobby.OverlayVisible, Is.True);
            Assert.That(lobby.OverlayTitle, Is.EqualTo(UiText.ContentEndTitle));
        }

        // Con el día de hoy ya sellado o pendiente de validar, «Jugar · Reto del río» abre lo que abriría su tarjeta: el calendario
        // de sellos (D-032, punto 6), nunca «El río sigue creciendo» ni el 701 (revisión de FX16b).
        [UnityTest]
        public IEnumerator AfterTheContent_WithTodaySealedOrPending_JugarRetoDelRio_OpensTheCalendar([Values(true, false)] bool sealedDay)
        {
            const string today = "2026-10-20";
            LaunchOptions.ClockOverride = new ManualClock(new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            var record = new DailyChallengeRecord { StreakDays = 1 };
            if (sealedDay)
            {
                record.LastClaimedDate = today;
                record.StampsMonth = "2026-10";
                record.StampDays.Add(20);
            }
            else
            {
                record.PendingClaims.Add(new DailyClaimEntry(today, 1));
            }

            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore(record);
            yield return LoadAfter700();
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            DailyCardState expected = sealedDay ? DailyCardState.Sealed : DailyCardState.PendingValidation;
            Assert.That(boot.DailyService!.CardState(700), Is.EqualTo(expected), "el día de hoy, " + (sealedDay ? "sellado" : "pendiente de validar"));
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(lobby.PlayMode, Is.EqualTo(PlayButtonMode.Daily));
            Assert.That(lobby.PlayLabel, Is.EqualTo(UiText.PlayDaily));

            Assert.That(boot.PressPlay(), Is.True);
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Assert.That(daily.Calendar.Visible, Is.True, "el calendario de sellos, como su tarjeta");
            Assert.That(daily.IsOpen, Is.True);
            Assert.That(daily.Flow, Is.Null, "ningún tablero del Reto: hoy ya se jugó");
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "nunca «El río sigue creciendo»");
            Assert.That(lobby.OverlayVisible, Is.False);
            Assert.That(boot.Flow!.HasLevel && boot.Flow.LevelNumber > 700, Is.False, "ningún 701");
        }

        [UnityTest]
        public IEnumerator Winning700_ThenHome_TheLobbyOffersTheReto_OrJugarWithoutIt([Values(true, false)] bool reto)
        {
            if (reto)
            {
                GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
                GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            }
            else
            {
                GameBootstrap.DailyPoolDisabledOverride = true;
            }

            yield return FactLineTests.Win(700);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Catalog.LastLevel, Is.EqualTo(700), "el último nivel publicado");
            Assert.That(boot.PressHome(), Is.True, "Inicio en la victoria del 700");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(lobby.PlayMode, Is.EqualTo(reto ? PlayButtonMode.Daily : PlayButtonMode.ContentEnd));
            Assert.That(lobby.PlayLabel, Is.EqualTo(reto ? "Jugar · Reto del río" : "Jugar"));
            Assert.That(lobby.PlayLabel, Is.Not.EqualTo(UiText.PlayLevel(701)), "nunca «Jugar · Nivel 701»");

            Assert.That(boot.PressPlay(), Is.True);
            if (reto)
            {
                Assert.That(boot.DailyScreen!.IsOpen, Is.True, "el Reto");
                Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            }
            else
            {
                Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.ContentEnd), "«El río sigue creciendo»");
            }
        }

        [Test]
        public void PlayModeFor_TheLevel_ThenTheRetoOrJugar()
        {
            Assert.That(LobbyScreen.PlayModeFor(true, true), Is.EqualTo(PlayButtonMode.Level), "con nivel publicado, «Jugar · Nivel {n}»");
            Assert.That(LobbyScreen.PlayModeFor(true, false), Is.EqualTo(PlayButtonMode.Level));
            Assert.That(LobbyScreen.PlayModeFor(false, true), Is.EqualTo(PlayButtonMode.Daily));
            Assert.That(LobbyScreen.PlayModeFor(false, false), Is.EqualTo(PlayButtonMode.ContentEnd));
            Assert.That(LobbyScreen.PlayLabelFor(PlayButtonMode.Level, 26), Is.EqualTo(UiText.PlayLevel(26)));
            Assert.That(LobbyScreen.PlayLabelFor(PlayButtonMode.Daily, 701), Is.EqualTo(UiText.PlayDaily));
            Assert.That(LobbyScreen.PlayLabelFor(PlayButtonMode.ContentEnd, 701), Is.EqualTo(UiText.Play));
        }

        // El inicio de quien ya ganó el 700 (next_level 701, max_level 700, el Mapa visto).
        internal static IEnumerator LoadAfter700()
        {
            Profile profile = Wallet.NewProfile(new ManualClock(new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc)));
            profile.Progress.NextLevel = 701;
            profile.Progress.MaxLevel = 700;
            profile.Progress.MapSeenLevel = 700;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
        }
    }
}
