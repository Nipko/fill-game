#nullable enable
using System.Collections;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// «La intro nace del logo» (añadido de D-051 del 2026-10-28; <see cref="BrandIntroMorph"/>): el primer fotograma de la intro es
    /// el icono de la pantalla del sistema (el círculo de 160 dp con la vista de 240 dp, centrados, sobre el petróleo); el círculo se
    /// abre hasta cubrir la pantalla mientras la escena pasa del encuadre del icono al suyo; el açaí se recoge hacia la cuia y el
    /// vertido de siempre empieza; con Reducir movimiento, un fundido de 200 ms del icono a la escena quieta; y el total no pasa del
    /// tope.
    /// </summary>
    public sealed class BrandIntroMorphTests
    {
        // Un teléfono de 1080 × 2340 px a 2,625 px por dp (411 dp de ancho, como el SM-S938B): el lienzo mide 360 × 780 unidades
        // (3 px por unidad), así que 1 dp de Android son 0,875 unidades.
        private const float PhoneDensity = 2.625f;
        private const float PhoneUnitsPerDp = PhoneDensity / 3f;

        private GameObject? _host;
        private BootSplash? _splash;
        private bool _snap;

        [TearDown]
        public void TearDown()
        {
            _splash?.Dispose();
            _splash = null;
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            BrandIntro.FrameVisibleOverrideForTests = null;
            AndroidOsSplash.IconOverrideForTests = null;
            if (_snap)
            {
                SnapshotKit.Release();
                _snap = false;
            }
            if (_host != null)
            {
                Object.Destroy(_host);
            }

            _host = null;
        }

        [Test]
        public void TheTimeline_OpensThenPours_WithinTheCap()
        {
            Assert.That(BrandIntro.AnimMs, Is.LessThanOrEqualTo(3500), "unos 3,5 s, como pidió el dueño");
            Assert.That(BrandIntro.SettledMs, Is.LessThanOrEqualTo(2900), "y la carga no espera más que antes");
            Assert.That(BrandIntro.MorphMs, Is.InRange(500, 900), "«el logo se abre»");
            Assert.That(BrandIntro.IconFadeMs, Is.InRange(150, 200), "el resto del icono se va al petróleo en ~150-200 ms");
            Assert.That(BrandIntro.CoverFadeFromMs, Is.GreaterThanOrEqualTo(BrandIntro.IconFadeFromMs + BrandIntro.IconFadeMs),
                "el fondo de la escena sale del petróleo cuando el icono ya se fue: nunca dos ríos a la vez");
            Assert.That(BrandIntro.CoverFadeFromMs + BrandIntro.CoverFadeMs, Is.LessThanOrEqualTo(BrandIntro.PourFromMs), "la escena entera antes de verter");
            Assert.That(BrandIntro.AcaiRetractMs, Is.InRange(300, 500), "el açaí se recoge");
            Assert.That(BrandIntro.AcaiRetractFromMs + BrandIntro.AcaiRetractMs, Is.LessThanOrEqualTo(BrandIntro.PourFromMs), "recogido antes de verter");
            Assert.That(BrandIntro.StreamRetractFromMs, Is.LessThan(BrandIntro.PourFromMs), "el chorro vuelve a la boca justo antes");
            Assert.That(BrandIntro.PourFromMs, Is.LessThanOrEqualTo(BrandIntro.MorphMs), "y vierte mientras el círculo acaba de abrirse: un solo gesto");
            Assert.That(BrandIntro.FallMs - BrandIntro.PourFromMs, Is.EqualTo(700), "la caída del chorro");
            Assert.That(BrandIntro.ReduceMotionFadeMs, Is.EqualTo(200), "con Reducir movimiento, un fundido de 200 ms");
            Assert.That(BrandIntroMorph.IconCanvasDp, Is.EqualTo(240f), "la vista del icono adaptativo en la pantalla del sistema");
            Assert.That(BrandIntroMorph.IconMaskDp, Is.EqualTo(160f), "su círculo");
            Assert.That(BrandIntroMorph.IrisTextureSize, Is.EqualTo(256));
        }

        [Test]
        public void TheEasing_IsSmooth_WithoutOvershoot()
        {
            float last = 0f;
            for (int i = 0; i <= 100; i++)
            {
                float e = BrandIntro.EaseInOutCubic(i / 100f);
                Assert.That(e, Is.InRange(0f, 1f), "sin rebote");
                Assert.That(e, Is.GreaterThanOrEqualTo(last), "siempre hacia delante");
                last = e;
            }

            Assert.That(BrandIntro.EaseInOutCubic(0f), Is.EqualTo(0f));
            Assert.That(BrandIntro.EaseInOutCubic(1f), Is.EqualTo(1f));
            Assert.That(BrandIntro.EaseInOutCubic(0.5f), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [UnityTest]
        public IEnumerator FirstFrame_IsTheSystemSplashIcon_SameCircleSameCentre()
        {
            BrandIntro intro = NewIntro(PhoneDensity);
            yield return null;
            _splash!.Tick(16f);
            Assert.That(intro.Begun, Is.False);
            Assert.That(intro.MorphUnitsPerDp, Is.EqualTo(PhoneUnitsPerDp).Within(1e-4f), "dp de Android, no unidades del lienzo");
            Assert.That(intro.MorphDiameterDp, Is.EqualTo(160f).Within(0.01f), "el círculo de 160 dp de la pantalla del sistema");
            Assert.That(intro.MorphCenter.x, Is.EqualTo(180f).Within(0.01f), "centrado en la pantalla");
            Assert.That(intro.MorphCenter.y, Is.EqualTo(390f).Within(0.01f));
            Assert.That(intro.MorphIrisVisible, Is.True);
            Assert.That(intro.MorphIconAlpha, Is.EqualTo(1f), "la vista del icono, entera");

            // La vista del icono: 240 dp, con el mismo centro.
            Assert.That(intro.Morph.HasIcon, Is.True, "la vista del icono está en Resources");
            // La vista va en el grupo de la cuia (escalado y desplazado): su caja en la pantalla.
            RectTransform icon = intro.Morph.Icon.rectTransform;
            RectTransform top = intro.Morph.Top;
            float k = top.localScale.x;
            Assert.That(icon.sizeDelta.x * k, Is.EqualTo(240f * PhoneUnitsPerDp).Within(0.01f));
            Assert.That(icon.sizeDelta.y * k, Is.EqualTo(240f * PhoneUnitsPerDp).Within(0.01f));
            Assert.That(top.anchoredPosition.x + (k * (icon.anchoredPosition.x + (icon.sizeDelta.x * 0.5f))), Is.EqualTo(180f).Within(0.01f));
            Assert.That(-top.anchoredPosition.y + (k * (-icon.anchoredPosition.y + (icon.sizeDelta.y * 0.5f))), Is.EqualTo(390f).Within(0.01f));
            Assert.That(intro.Morph.Icon.sprite!.texture.width, Is.EqualTo(432), "los píxeles de la vista de 432 del icono");

            // El agujero del círculo está centrado igual; fuera, el petróleo.
            RectTransform iris = intro.Morph.Iris.rectTransform;
            Assert.That(iris.anchoredPosition.x + (iris.sizeDelta.x * 0.5f), Is.EqualTo(180f).Within(0.01f));
            Assert.That(-iris.anchoredPosition.y + (iris.sizeDelta.y * 0.5f), Is.EqualTo(390f).Within(0.01f));
            Assert.That((Color32)intro.Morph.Iris.color, Is.EqualTo(new Color32(255, 255, 255, 255)), "el petróleo va en la textura, no en el vértice");
            Assert.That(intro.Morph.Measured, Is.False, "sin medida (el Editor): la caja de la guía");
            Assert.That(intro.Morph.Rim.gameObject.activeSelf, Is.False, "sin el aro de luz: igual que el icono del sistema");
            Assert.That(intro.MorphSceneScale, Is.LessThan(1f), "la cuia, en el encuadre del icono");
            Assert.That(intro.Piece("EmblemCuia")!.GetComponent<Image>().color.a, Is.EqualTo(0f), "solo el icono, tal cual: su propia cuia");
            Assert.That(intro.MorphCoverAlpha, Is.EqualTo(1f), "el fondo de la escena, tapado por el petróleo");
            Assert.That(intro.MorphBackScale, Is.GreaterThanOrEqualTo(1f));

            // El icono ya tiene el río teñido y el chorro: la escena de debajo también.
            Assert.That(intro.TintFill, Is.EqualTo(1f));
            Assert.That(intro.StreamFill, Is.EqualTo(1f));
            Assert.That(intro.ImpactAlpha, Is.EqualTo(0f), "los aros del fotograma 0 son los del icono (los de la escena entran con el corte)");
        }

        [UnityTest]
        public IEnumerator WithoutTheDensity_TheCircleIs160CanvasUnits()
        {
            BrandIntro intro = NewIntro(null);
            yield return null;
            Assert.That(intro.MorphUnitsPerDp, Is.EqualTo(1f), "el Editor no sabe la densidad: 1 unidad por dp");
            Assert.That(intro.MorphDiameter, Is.EqualTo(160f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator TheLogoOpens_ToTheWholeScreen_AndTheSceneLandsInPlace()
        {
            BrandIntro intro = NewIntro(PhoneDensity);
            _splash!.BeginIntro(false);
            yield return null;
            float last = 0f;
            float lastScale = 0f;
            for (int i = 0; i <= 70; i++)
            {
                intro.Seek(BrandIntro.MorphMs * i / 70f);
                Assert.That(intro.MorphDiameter, Is.GreaterThanOrEqualTo(last), "el círculo solo crece");
                Assert.That(intro.MorphSceneScale, Is.GreaterThanOrEqualTo(lastScale), "la cuia solo se acerca a su encuadre");
                Assert.That(intro.MorphBackScale, Is.GreaterThanOrEqualTo(1f), "el fondo nunca enseña sus bordes");
                Assert.That(intro.MorphIconAlpha <= 0f || intro.MorphCoverAlpha >= 1f, Is.True,
                    "a los " + (BrandIntro.MorphMs * i / 70f) + " ms: con el icono a la vista, el fondo de la escena sigue tapado (nunca dos ríos a la vez)");
                last = intro.MorphDiameter;
                lastScale = intro.MorphSceneScale;
            }

            intro.Seek(BrandIntro.IconFadeFromMs + (BrandIntro.IconFadeMs * 0.5f));
            Assert.That(intro.MorphIconAlpha, Is.InRange(0.1f, 0.9f), "el resto del icono se va al petróleo");
            Assert.That(intro.Piece("EmblemCuia")!.GetComponent<Image>().color.a, Is.EqualTo(1f), "y la cuia se queda: el ancla");
            intro.Seek(BrandIntro.MorphMs * 0.5f);
            Assert.That(intro.MorphIrisVisible, Is.True, "a medias, aún con el círculo");
            Assert.That(intro.MorphIconAlpha, Is.EqualTo(0f), "el icono ya se fue");
            Assert.That(intro.MorphCoverAlpha, Is.LessThan(1f), "y la escena entra desde el petróleo");

            intro.Seek(BrandIntro.MorphMs);
            Assert.That(intro.MorphIrisVisible, Is.False, "el círculo cubre la pantalla: sin petróleo alrededor");
            float half = Mathf.Sqrt((180f * 180f) + (390f * 390f));
            Assert.That(intro.MorphDiameter, Is.GreaterThanOrEqualTo(2f * half), "hasta las esquinas");
            Assert.That(intro.MorphSceneScale, Is.EqualTo(1f), "la cuia, en su encuadre");
            Assert.That(intro.MorphBackScale, Is.EqualTo(1f), "y el fondo");
            Assert.That(intro.Scene.anchoredPosition.magnitude, Is.LessThan(1e-3f));
            Assert.That(intro.MorphCoverAlpha, Is.EqualTo(0f), "sin petróleo encima");
            Assert.That(intro.Morph.Rim.gameObject.activeSelf, Is.False, "el aro de luz se apaga al final");

            intro.Seek(BrandIntro.MorphMs * 0.5f);
            Assert.That(intro.Morph.Rim.gameObject.activeSelf, Is.True, "a mitad de camino, el borde de luz");
            Assert.That(intro.Morph.Rim.color.a, Is.InRange(0.01f, BrandIntroMorph.RimMaxAlpha), "fino y sutil");
        }

        [UnityTest]
        public IEnumerator TheAcai_GoesBackToTheCuia_ThenTheUsualPour()
        {
            BrandIntro intro = NewIntro(PhoneDensity);
            _splash!.BeginIntro(false);
            yield return null;
            intro.Seek(0f);
            Assert.That(intro.TintFill, Is.EqualTo(1f));
            float last = 1f;
            for (int i = 0; i <= 20; i++)
            {
                intro.Seek(BrandIntro.AcaiRetractFromMs + (BrandIntro.AcaiRetractMs * i / 20f));
                Assert.That(intro.TintFill, Is.LessThanOrEqualTo(last), "el açaí solo se recoge");
                last = intro.TintFill;
            }

            Assert.That(intro.TintFill, Is.EqualTo(0f), "recogido entero hacia la cuia");
            Image far = intro.Scene.Find("EmblemAcai" + (BrandRiverScene.ChunkCount - 1)).GetComponent<Image>();
            Image near = intro.Scene.Find("EmblemAcai0").GetComponent<Image>();
            intro.Seek(BrandIntro.AcaiRetractFromMs + (BrandIntro.AcaiRetractMs * 0.5f));
            Assert.That(far.color.a, Is.LessThan(near.color.a), "se va primero el primer plano: hacia la boca de la cuia");

            intro.Seek(BrandIntro.PourFromMs - 1f);
            Assert.That(intro.StreamFill, Is.LessThan(0.05f), "el chorro volvió a la boca");
            Assert.That(intro.ImpactAlpha, Is.EqualTo(0f), "y los aros se deshicieron");
            intro.Seek(BrandIntro.PourFromMs);
            Assert.That(intro.StreamFill, Is.EqualTo(0f), "y vuelve a salir de la boca");
            intro.Seek(BrandIntro.FallMs);
            Assert.That(intro.StreamFill, Is.EqualTo(1f), "llega al agua");
            intro.Seek(BrandIntro.MotionMs);
            Assert.That(intro.TintFill, Is.EqualTo(1f), "y el açaí tiñe el río otra vez");
        }

        [UnityTest]
        public IEnumerator Playing_OpensTheLogo_AndNotesTheProbe()
        {
            BrandIntro intro = NewIntro(PhoneDensity);
            _splash!.BeginIntro(false);
            double until = Time.realtimeSinceStartupAsDouble + ((BrandIntro.MorphMs + 400) / 1000.0);
            while (Time.realtimeSinceStartupAsDouble < until)
            {
                yield return null;
                _splash.Tick(16f);
            }

            Assert.That(intro.Playing, Is.True);
            Assert.That(intro.MorphIrisVisible, Is.False, "abierto a toda la pantalla");
            BrandIntroReport report = _splash.EndIntro()!;
            Assert.That(report.MorphMode, Is.EqualTo("open"));
            Assert.That(report.MorphMs, Is.GreaterThanOrEqualTo((long)BrandIntro.MorphMs), "morph_ms: lo que tardó en abrirse");
            Assert.That(report.MorphFrames, Is.GreaterThan(1), "morph_frames");
            Assert.That(report.MorphStartDiamDp, Is.EqualTo(160f).Within(0.01f), "morph_start_diam_dp");
            Assert.That(report.MorphUnitsPerDp, Is.EqualTo(PhoneUnitsPerDp).Within(1e-4f));
            Assert.That(report.MorphBuildMs, Is.GreaterThanOrEqualTo(0L), "morph_build_ms");

            string? line = new DevProbe().FormatBrandIntro(report);
            Assert.That(line, Is.Not.Null);
            StringAssert.Contains("\"morph_mode\":\"open\"", line);
            StringAssert.Contains("\"morph_start_diam_dp\":160.0", line);
            StringAssert.Contains("\"morph_frames\":", line);
            StringAssert.Contains("\"morph_ms\":", line);
            StringAssert.Contains("\"morph_build_ms\":" + report.MorphBuildMs, line);
            StringAssert.EndsWith("\"wait_capped\":false}", line);
        }

        [UnityTest]
        public IEnumerator ReduceMotion_FadesTheIconIntoTheStillScene_WithoutOpening()
        {
            bool visible = false;
            BrandIntro.FrameVisibleOverrideForTests = () => visible;
            BrandIntro intro = NewIntro(PhoneDensity);
            _splash!.BeginIntro(true);
            for (int i = 0; i < 4; i++)
            {
                yield return null;
                _splash.Tick(16f);
            }

            Assert.That(intro.ClockMs, Is.EqualTo((float)BrandIntro.AnimMs), "la escena quieta, entera");
            Assert.That(intro.MorphIconAlpha, Is.EqualTo(1f), "hasta verse en la pantalla, el icono del sistema encima");
            Assert.That(intro.MorphIrisVisible, Is.True);
            Assert.That(intro.MorphDiameterDp, Is.EqualTo(160f).Within(0.01f));
            Assert.That(intro.WordmarkAlpha, Is.EqualTo(0f), "sin el nombre sobre el icono");

            visible = true;
            double until = Time.realtimeSinceStartupAsDouble + ((BrandIntro.ReduceMotionFadeMs + 80) / 1000.0);
            bool sawHalf = false;
            while (Time.realtimeSinceStartupAsDouble < until)
            {
                yield return null;
                _splash.Tick(16f);
                Assert.That(intro.MorphDiameterDp, Is.EqualTo(160f).Within(0.01f), "el círculo no se abre: solo se funde");
                Assert.That(intro.MorphSceneScale, Is.EqualTo(1f), "la escena no se mueve");
                sawHalf |= intro.MorphIconAlpha > 0f && intro.MorphIconAlpha < 1f;
            }

            Assert.That(intro.MorphIconAlpha, Is.EqualTo(0f), "fundido en 200 ms");
            Assert.That(intro.MorphIrisVisible, Is.False);
            Assert.That(intro.WordmarkAlpha, Is.EqualTo(1f), "con el nombre");
            Assert.That(sawHalf || Application.isBatchMode, Is.True, "un fundido, no un corte");
            BrandIntroReport report = _splash.EndIntro()!;
            Assert.That(report.MorphMode, Is.EqualTo("fade"));
            Assert.That(report.MorphMs, Is.EqualTo(-1L), "no se abrió");
        }

        [UnityTest]
        public IEnumerator TheVeilGoes_AndTheLogoStartsOpening_OnTheSameTick()
        {
            bool visible = false;
            BrandIntro.FrameVisibleOverrideForTests = () => visible;
            BrandIntro intro = NewIntro(PhoneDensity);
            _splash!.BeginIntro(false);
            for (int i = 0; i < 3; i++)
            {
                yield return null;
                _splash.Tick(16f);
                Assert.That(AndroidOsSplash.SignalFor(true, true, intro.OnScreen, i + 1), Is.Null, "sin verse aún, el velo sigue");
                Assert.That(intro.Playing, Is.False);
                Assert.That(intro.MorphDiameterDp, Is.EqualTo(160f).Within(0.01f), "debajo del velo, el mismo icono");
            }

            visible = true;
            yield return null;
            _splash.Tick(16f);
            Assert.That(AndroidOsSplash.SignalFor(true, true, intro.OnScreen, 4), Is.EqualTo(AndroidOsSplash.ByIntro), "el velo se suelta en este tick");
            Assert.That(intro.Playing, Is.True, "y en el mismo tick empieza la apertura");
            Assert.That(intro.ClockMs, Is.EqualTo(0f), "desde su instante 0: el icono, igual que el velo");
            Assert.That(intro.MorphDiameterDp, Is.EqualTo(160f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator FirstFrame_UsesTheMeasuredSystemIcon_486PxCentred()
        {
            // Jornada 6, observación 11: el SM-S938B pinta el icono del sistema a ~486 px (≈ 185 dp), no a los 420 de la guía. Con la
            // caja que mide Java, el fotograma 0 tiene ese diámetro y ese centro (1080 × 2340 px: 3 px por unidad del lienzo).
            AndroidOsSplash.IconOverrideForTests = new SplashIconBox(297, 927, 486, 486, 0, 0, 1080, 2340);
            BrandIntro intro = NewIntro(PhoneDensity);
            yield return null;
            _splash!.Tick(16f);
            Assert.That(intro.Morph.Measured, Is.True);
            Assert.That(intro.MorphDiameter, Is.EqualTo(162f).Within(0.01f), "486 px / 3 px por unidad");
            Assert.That(intro.MorphDiameterDp, Is.EqualTo(486f / PhoneDensity).Within(0.01f), "≈ 185 dp");
            Assert.That(intro.MorphCenter.x, Is.EqualTo(180f).Within(0.01f));
            Assert.That(intro.MorphCenter.y, Is.EqualTo(390f).Within(0.01f));
            Assert.That(intro.Morph.StartDiameterPx, Is.EqualTo(486f).Within(0.01f), "en px de pantalla, los del sistema");
            Assert.That(intro.Morph.StartCenterPx.x, Is.EqualTo(540f).Within(0.01f));
            Assert.That(intro.Morph.StartCenterPx.y, Is.EqualTo(1170f).Within(0.01f));
            AssertIconView(intro, 162f * 1.5f, new Vector2(180f, 390f));

            BrandIntroReport report = _splash.EndIntro()!;
            Assert.That(report.MorphGeometry, Is.EqualTo("measured"));
            Assert.That(report.MorphStartDiamPx, Is.EqualTo(486f).Within(0.01f));
            Assert.That(report.MorphStartCenterPx.x, Is.EqualTo(540f).Within(0.01f));
            Assert.That(report.MorphStartCenterPx.y, Is.EqualTo(1170f).Within(0.01f));
            string? line = new DevProbe().FormatBrandIntro(report);
            StringAssert.Contains("\"morph_geometry\":\"measured\",\"morph_start_diam_px\":486.0,\"morph_start_xy_px\":[540.0,1170.0]", line);
        }

        [UnityTest]
        public IEnumerator FirstFrame_FollowsAnOffCentreBox_AndStillOpensToTheCorners()
        {
            AndroidOsSplash.IconOverrideForTests = new SplashIconBox(100, 200, 486, 480, 0, 0, 1080, 2340);
            BrandIntro intro = NewIntro(PhoneDensity);
            _splash!.BeginIntro(false);
            yield return null;
            intro.Seek(0f);
            Vector2 centre = new Vector2(343f / 3f, 440f / 3f);
            Assert.That(intro.MorphDiameter, Is.EqualTo(160f).Within(0.01f), "el lado menor: 480 px");
            Assert.That(intro.MorphCenter.x, Is.EqualTo(centre.x).Within(0.01f));
            Assert.That(intro.MorphCenter.y, Is.EqualTo(centre.y).Within(0.01f));
            AssertIconView(intro, 240f, centre);
            RectTransform iris = intro.Morph.Iris.rectTransform;
            Assert.That(iris.anchoredPosition.x + (iris.sizeDelta.x * 0.5f), Is.EqualTo(centre.x).Within(0.01f));
            Assert.That(-iris.anchoredPosition.y + (iris.sizeDelta.y * 0.5f), Is.EqualTo(centre.y).Within(0.01f));
            intro.Seek(BrandIntro.MorphMs);
            float fx = 360f - centre.x;
            float fy = 780f - centre.y;
            Assert.That(intro.MorphDiameter, Is.GreaterThanOrEqualTo(2f * Mathf.Sqrt((fx * fx) + (fy * fy))), "hasta la esquina más lejana");
            Assert.That(intro.MorphIrisVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator WithoutAMeasure_TheGuideBox_AndALateMeasure_RelaysFrameZeroBeforeTheVeilGoes()
        {
            yield return LateSystemBox(null);
        }

        [UnityTest]
        public IEnumerator TheOwnVeilBoxFirst_ThenTheSystemBox_RelaysFrameZeroBeforeTheVeilGoes()
        {
            // El caso real de Android 12+ (revisión de ebfc14c4): el velo propio publica primero la caja de af_launch_window (160 dp,
            // 420 px, centrada) y la del sistema (486 px) llega al colocarse su vista, ya con el primer fotograma pintado.
            yield return LateSystemBox(new SplashIconBox(330, 960, 420, 420, 0, 0, 1080, 2340));
        }

        [UnityTest]
        public IEnumerator FrameZero_IsRelaidAtMostTwice()
        {
            bool visible = false;
            BrandIntro.FrameVisibleOverrideForTests = () => visible;
            AndroidOsSplash.IconOverrideForTests = new SplashIconBox(330, 960, 420, 420, 0, 0, 1080, 2340);
            BrandIntro intro = NewIntro(PhoneDensity);
            _splash!.BeginIntro(false);
            var boxes = new[] { 450, 486, 500 };
            var expected = new[] { 150f, 162f, 162f };
            for (int i = 0; i < boxes.Length; i++)
            {
                int d = boxes[i];
                AndroidOsSplash.IconOverrideForTests = new SplashIconBox(540 - (d / 2), 1170 - (d / 2), d, d, 0, 0, 1080, 2340);
                for (int t = 0; t < BrandIntro.IconPollFrames; t++)
                {
                    yield return null;
                    _splash.Tick(16f);
                }

                Assert.That(intro.MorphDiameter, Is.EqualTo(expected[i]).Within(0.01f), "caja " + d + " px: como mucho dos rehechos");
            }

            Assert.That(intro.OnScreen, Is.False);
            Assert.That(new SplashIconBox(540 - 243, 1170 - 243, 486, 486, 0, 0, 1080, 2340).DiffersFrom(new SplashIconBox(298, 928, 486, 486, 0, 0, 1080, 2340), 1), Is.False, "1 px no cuenta");
            Assert.That(new SplashIconBox(297, 927, 486, 486, 0, 0, 1080, 2340).DiffersFrom(new SplashIconBox(297, 927, 489, 486, 0, 0, 1080, 2340), 1), Is.True);
            Assert.That(new SplashIconBox(297, 927, 486, 486, 0, 0, 1080, 2340).DiffersFrom(new SplashIconBox(297, 927, 486, 486, 0, 0, 1080, 2400), 1), Is.True, "el marco también");
            Assert.That(SplashIconBox.None.DiffersFrom(new SplashIconBox(297, 927, 486, 486, 0, 0, 1080, 2340), 1), Is.True, "de nada a una medida");
        }

        // El fotograma 0 empieza con start (null: sin medida, la guía) y la caja del sistema llega después del primer fotograma: se
        // rehace, la espera de la GPU vuelve a empezar y la sonda da los 486 px.
        private IEnumerator LateSystemBox(SplashIconBox? start)
        {
            bool visible = false;
            BrandIntro.FrameVisibleOverrideForTests = () => visible;
            AndroidOsSplash.IconOverrideForTests = start;
            BrandIntro intro = NewIntro(PhoneDensity);
            _splash!.BeginIntro(false);
            yield return null;
            _splash.Tick(16f);
            Assert.That(intro.Morph.Measured, Is.EqualTo(start.HasValue));
            Assert.That(intro.MorphDiameterDp, Is.EqualTo(160f).Within(0.01f), "160 dp: la guía o la caja de af_launch_window");
            Assert.That(intro.Morph.StartDiameterPx, Is.EqualTo(420f).Within(0.01f), "160 dp a 2,625 px por dp");
            yield return null;
            _splash.Tick(16f);
            Assert.That(intro.FenceRequestedAtTick, Is.GreaterThan(0), "ya esperaba a ver el fotograma 0 de 160 dp");

            // Java midió la del sistema (la vista del icono se colocó tarde): se rehace el fotograma 0 y se vuelve a esperar a verlo.
            AndroidOsSplash.IconOverrideForTests = new SplashIconBox(297, 927, 486, 486, 0, 0, 1080, 2340);
            visible = true; // la GPU ya terminó el fotograma de 160 dp, pero ese ya no vale
            bool relaid = false;
            for (int t = 0; t < BrandIntro.IconPollFrames && !relaid; t++)
            {
                yield return null;
                _splash.Tick(16f);
                relaid = Mathf.Abs(intro.MorphDiameter - 162f) < 0.01f;
                if (!relaid)
                {
                    Assert.That(intro.OnScreen, Is.False, "mientras no se mira la caja, la espera tampoco acaba: el velo sigue");
                }
            }

            Assert.That(relaid, Is.True, "se rehace en " + BrandIntro.IconPollFrames + " fotogramas como mucho");
            Assert.That(intro.Morph.Measured, Is.True);
            Assert.That(intro.FenceRequestedAtTick, Is.EqualTo(-1), "la espera vuelve a empezar con el fotograma nuevo");
            Assert.That(intro.OnScreen, Is.False, "el velo no se suelta sobre el fotograma de 160 dp");
            Assert.That(intro.Playing, Is.False);
            yield return null;
            _splash.Tick(16f);
            Assert.That(intro.OnScreen, Is.True);
            Assert.That(intro.Playing, Is.True);
            BrandIntroReport report = _splash.EndIntro()!;
            Assert.That(report.MorphStartDiamPx, Is.EqualTo(486f).Within(0.01f), "la sonda dice el círculo con que empezó de verdad");
            Assert.That(report.MorphStartCenterPx.x, Is.EqualTo(540f).Within(0.01f));
            Assert.That(report.MorphStartCenterPx.y, Is.EqualTo(1170f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator ThePetroleum_IsInTheTextures_WithWhiteVertices()
        {
            // Jornada 6, observación 11: en el espacio de color lineal, el color de vértice se guarda en lineal a 8 bits y el #14444A
            // salía (22,69,73). Todo el petróleo de la intro va en texturas sRGB, con el vértice blanco.
            Assert.That(QualitySettings.activeColorSpace, Is.EqualTo(ColorSpace.Linear), "la causa: el proyecto es lineal");
            BrandIntro intro = NewIntro(PhoneDensity);
            yield return null;
            Sprite petroleum = intro.Morph.PetroleumSprite;
            Assert.That(petroleum.texture.isDataSRGB, Is.True, "sRGB: la GPU la decodifica sin redondear a 8 bits en lineal");
            foreach (Color32 c in petroleum.texture.GetPixels32())
            {
                Assert.That(c, Is.EqualTo(BrandIntro.Petroleum));
            }

            foreach (string name in new[] { "Petroleum", "MorphCover", "MorphBar0", "MorphBar1", "MorphBar2", "MorphBar3" })
            {
                Image image = intro.Root.Find(name).GetComponent<Image>();
                Assert.That(image.sprite, Is.SameAs(petroleum), name);
                Color32 v = image.color;
                Assert.That(new Color32(v.r, v.g, v.b, 255), Is.EqualTo(new Color32(255, 255, 255, 255)), name + ": vértice blanco");
            }

            // Lo que queda al quitarse la intro: el cielo y el velo de la pantalla de arranque, con el mismo petróleo exacto.
            foreach (string path in new[] { "Sky", "Veil/VeilSky" })
            {
                Image image = _splash!.Root.Find(path).GetComponent<Image>();
                Assert.That(image.sprite, Is.Not.Null, path + ": el petróleo va en una textura");
                Assert.That(image.sprite!.texture.isDataSRGB, Is.True, path);
                foreach (Color32 c in image.sprite.texture.GetPixels32())
                {
                    Assert.That(c, Is.EqualTo(BrandIntro.Petroleum), path);
                }

                Color32 v = image.color;
                Assert.That(new Color32(v.r, v.g, v.b, 255), Is.EqualTo(new Color32(255, 255, 255, 255)), path + ": vértice blanco");
            }

            Assert.That(intro.Morph.Iris.sprite!.texture.isDataSRGB, Is.True);
            Color32 corner = BrandIntroMorph.RingPixel(false, 0, 0);
            Assert.That(new Color32(corner.r, corner.g, corner.b, 255), Is.EqualTo(BrandIntro.Petroleum), "el círculo lleva el petróleo en su textura");
            Assert.That(corner.a, Is.EqualTo(255), "fuera del agujero, opaco");
            int mid = BrandIntroMorph.IrisTextureSize / 2;
            Assert.That(BrandIntroMorph.RingPixel(false, mid, mid).a, Is.EqualTo(0), "dentro, transparente");
        }

        [UnityTest]
        public IEnumerator FrameZero_RendersThePetroleumExactly()
        {
            // Lee el fotograma 0 pintado (la cámara de las capturas sobre una RenderTexture sRGB) y compara el petróleo con
            // (20,68,74) ±1. Sin dispositivo gráfico (-nographics) no se pinta nada: se omite (lo cubre el test de las texturas).
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Sin dispositivo gráfico (-nographics): no hay píxeles que leer.");
            }

            SnapshotKit.Prepare();
            _snap = true;
            GameScreen.SimulatedPxPerDp = PhoneDensity;
            AndroidOsSplash.IconOverrideForTests = new SplashIconBox(297, 927, 486, 486, 0, 0, 1080, 2340);
            _host ??= new GameObject("BrandIntroMorphTestHost");
            _splash = new BootSplash(_host.transform, withSloth: false, withIntro: true);
            SnapshotKit.AttachOverlay(_splash.Canvas);
            yield return null;
            Color32[] px = SnapshotKit.CapturePixels();
            int w = SnapshotKit.Width;
            int h = SnapshotKit.Height;
            Assert.That(px.Length, Is.EqualTo(w * h));

            // Las filas empiezan por abajo. Puntos del petróleo: una banda (esquina), la textura del círculo (en diagonal, fuera del
            // agujero de 243 px de radio) y otra banda (debajo del círculo).
            var points = new[] { new Vector2Int(8, 8), new Vector2Int(540 + 200, 1170 + 200), new Vector2Int(540, 1170 + 420), new Vector2Int(w - 8, h - 8) };
            var target = (Color32)BrandIntro.Petroleum;
            foreach (Vector2Int p in points)
            {
                Color32 c = px[((h - 1 - p.y) * w) + p.x];
                string where = "(" + p.x + "," + p.y + ") = (" + c.r + "," + c.g + "," + c.b + ")";
                Assert.That(Mathf.Abs(c.r - target.r), Is.LessThanOrEqualTo(1), where);
                Assert.That(Mathf.Abs(c.g - target.g), Is.LessThanOrEqualTo(1), where);
                Assert.That(Mathf.Abs(c.b - target.b), Is.LessThanOrEqualTo(1), where);
            }

            // Y dentro del círculo, la vista del icono: no es petróleo liso.
            Color32 inside = px[((h - 1 - 1170) * w) + 540];
            Assert.That(inside.r != target.r || inside.g != target.g || inside.b != target.b, Is.True, "el centro enseña el icono");
        }

        // Jornada 6, observación 12: la caja del SM-S938B (504 px en 540, 1170) con una máscara squircle conocida (superelipse n = 4,
        // ejes a 497 de 504 px).
        private static readonly float SquircleAxis = 497f / 504f;
        private static readonly float SquircleDiag = SquircleAxis * Mathf.Sqrt(2f) * Mathf.Pow(2f, -0.25f);

        private static SplashIconBox SquircleBox()
        {
            return new SplashIconBox(288, 918, 504, 504, 0, 0, 1080, 2340, Mathf.RoundToInt(SquircleAxis * 10000f), Mathf.RoundToInt(SquircleDiag * 10000f), 10400);
        }

        [UnityTest]
        public IEnumerator FrameZero_CalksTheSystemSquircle_WithinOnePixel_AndOpensToTheCircle()
        {
            AndroidOsSplash.IconOverrideForTests = SquircleBox();
            BrandIntro intro = NewIntro(PhoneDensity);
            _splash!.BeginIntro(false);
            yield return null;
            _splash.Tick(16f);
            intro.Seek(0f);
            BrandIntroMorph morph = intro.Morph;
            Assert.That(morph.Shape.Name, Is.EqualTo("squircle"));
            Assert.That(morph.ShapeDrawn, Is.True, "el agujero del fotograma 0 es la textura de la forma");
            Assert.That(morph.ShapeProgress, Is.EqualTo(0f));
            Assert.That(morph.Iris.sprite!.texture, Is.SameAs(morph.ShapeTexture));
            Assert.That(morph.ShapeTexture!.isDataSRGB, Is.True, "el petróleo exacto, como el del círculo");

            // El modelo, en px de pantalla (3 px por unidad).
            float px = morph.PxPerUnit;
            Assert.That(2f * morph.HoleRadius(0f) * px, Is.EqualTo(497f).Within(1f), "ejes");
            Assert.That(2f * morph.HoleRadius(Mathf.PI * 0.5f) * px, Is.EqualTo(497f).Within(1f));
            Assert.That(2f * morph.HoleRadius(Mathf.PI * 0.25f) * px, Is.EqualTo(SquircleDiag * 504f).Within(1f), "diagonales");

            // Lo pintado: la textura de la forma, estirada al cuadrado del círculo, con el borde (opacidad 0,5) a ±1 px.
            Color32[] tex = morph.ShapeTexture.GetPixels32();
            int n = BrandIntroMorph.IrisTextureSize;
            int half = n / 2;
            float screenPerTex = morph.Iris.rectTransform.sizeDelta.x * px / n;
            float axisEdge = Edge(i => tex[(half * n) + half + i].a, i => i + 0.5f);
            float axisEdgeLeft = Edge(i => tex[(half * n) + half - 1 - i].a, i => i + 0.5f);
            float diagEdge = Edge(i => tex[((half + i) * n) + half + i].a, i => (i + 0.5f) * Mathf.Sqrt(2f));
            float diagEdge2 = Edge(i => tex[((half - 1 - i) * n) + half + i].a, i => (i + 0.5f) * Mathf.Sqrt(2f));
            Assert.That(2f * axisEdge * screenPerTex, Is.EqualTo(497f).Within(1f), "ejes, en la textura");
            Assert.That(2f * axisEdgeLeft * screenPerTex, Is.EqualTo(497f).Within(1f));
            Assert.That(2f * diagEdge * screenPerTex, Is.EqualTo(SquircleDiag * 504f).Within(1f), "diagonales, en la textura");
            Assert.That(2f * diagEdge2 * screenPerTex, Is.EqualTo(SquircleDiag * 504f).Within(1f));

            // La apertura: el agujero nunca encoge en ningún ángulo (cada 10 ms) y acaba en el círculo de siempre.
            var prev = new float[37];
            bool circle = false;
            for (int ms = 0; ms <= BrandIntro.MorphMs; ms += 10)
            {
                intro.Seek(ms);
                if (!morph.IrisVisible)
                {
                    break;
                }

                for (int k = 0; k <= 36; k++)
                {
                    float r = morph.HoleRadius(k * 2.5f * Mathf.Deg2Rad);
                    if (ms > 0)
                    {
                        Assert.That(r, Is.GreaterThanOrEqualTo(prev[k] - 0.001f), ms + " ms, " + (k * 2.5f) + " grados: ningún píxel de icono pasa a petróleo");
                    }

                    prev[k] = r;
                }

                if (morph.ShapeProgress >= 1f)
                {
                    circle = true;
                    Assert.That(morph.ShapeDrawn, Is.False, ms + " ms: ya el círculo de siempre");
                    Assert.That(morph.ShapeExponent, Is.EqualTo(2f));
                    Assert.That(morph.HoleRadius(0.3f), Is.EqualTo(morph.Diameter * 0.5f).Within(1e-3f));
                }
            }

            Assert.That(circle, Is.True, "la forma acaba en el círculo dentro de la apertura");
            intro.Seek(BrandIntro.MorphMs * 0.5f);
            Assert.That(morph.ShapeProgress, Is.EqualTo(1f), "a mitad de la apertura ya es el círculo");

            BrandIntroReport report = _splash.EndIntro()!;
            Assert.That(report.MorphShape, Is.EqualTo("squircle"));
            Assert.That(report.MorphShapeN, Is.EqualTo(4f).Within(0.02f));
            string? line = new DevProbe().FormatBrandIntro(report);
            StringAssert.Contains("\"morph_shape\":\"squircle\",\"morph_shape_n\":4.00,\"morph_shape_px\":[497.0,", line);
        }

        [UnityTest]
        public IEnumerator WithoutAMask_FrameZeroIsTheCircle_AndTheProbeSaysSo()
        {
            AndroidOsSplash.IconOverrideForTests = new SplashIconBox(288, 918, 504, 504, 0, 0, 1080, 2340);
            BrandIntro intro = NewIntro(PhoneDensity);
            _splash!.BeginIntro(false);
            yield return null;
            _splash.Tick(16f);
            intro.Seek(0f);
            Assert.That(intro.Morph.Shape.Name, Is.EqualTo("circle"));
            Assert.That(intro.Morph.ShapeDrawn, Is.False, "la textura del círculo de siempre");
            Assert.That(intro.Morph.ShapeTexture, Is.Null, "sin máscara no se crea la textura de la forma");
            Assert.That(2f * intro.Morph.HoleRadius(Mathf.PI * 0.25f) * intro.Morph.PxPerUnit, Is.EqualTo(504f).Within(0.01f));
            BrandIntroReport report = _splash.EndIntro()!;
            string? line = new DevProbe().FormatBrandIntro(report);
            StringAssert.Contains("\"morph_geometry\":\"measured\"", line);
            StringAssert.Contains("\"morph_shape\":\"circle\",\"morph_shape_n\":2.00,\"morph_shape_px\":[504.0,504.0],\"morph_shape_fit_px\":0.0", line);
        }

        [UnityTest]
        public IEnumerator ReduceMotion_KeepsTheSquircle_WhileItFades()
        {
            AndroidOsSplash.IconOverrideForTests = SquircleBox();
            BrandIntro intro = NewIntro(PhoneDensity);
            BrandIntro.FrameVisibleOverrideForTests = () => false;
            _splash!.BeginIntro(true);
            for (int i = 0; i < 4; i++)
            {
                yield return null;
                _splash.Tick(16f);
            }

            Assert.That(intro.MorphIconAlpha, Is.EqualTo(1f));
            Assert.That(intro.Morph.ShapeDrawn, Is.True, "con Reducir movimiento el agujero no se abre: la forma del sistema se funde");
            Assert.That(2f * intro.Morph.HoleRadius(0f) * intro.Morph.PxPerUnit, Is.EqualTo(497f).Within(1f));
            Assert.That(intro.Morph.Rim.gameObject.activeSelf, Is.False);
        }

        // El radio (en las unidades de radiusAt) donde la opacidad cruza 0,5 al alejarse del centro, interpolado entre dos píxeles.
        private static float Edge(System.Func<int, byte> alphaAt, System.Func<int, float> radiusAt)
        {
            for (int i = 1; i < 128; i++)
            {
                float a0 = alphaAt(i - 1) / 255f;
                float a1 = alphaAt(i) / 255f;
                if (a0 < 0.5f && a1 >= 0.5f)
                {
                    float t = (0.5f - a0) / (a1 - a0);
                    return Mathf.Lerp(radiusAt(i - 1), radiusAt(i), t);
                }
            }

            return -1f;
        }

        // La vista del icono en la pantalla (unidades): su lado y su centro.
        private static void AssertIconView(BrandIntro intro, float side, Vector2 centre)
        {
            RectTransform icon = intro.Morph.Icon.rectTransform;
            RectTransform top = intro.Morph.Top;
            float k = top.localScale.x;
            Assert.That(icon.sizeDelta.x * k, Is.EqualTo(side).Within(0.01f), "la vista del icono: 1,5 veces el círculo");
            Assert.That(top.anchoredPosition.x + (k * (icon.anchoredPosition.x + (icon.sizeDelta.x * 0.5f))), Is.EqualTo(centre.x).Within(0.01f));
            Assert.That(-top.anchoredPosition.y + (k * (-icon.anchoredPosition.y + (icon.sizeDelta.y * 0.5f))), Is.EqualTo(centre.y).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator TheIconVista_LoadsAsA432Png()
        {
            var asset = Resources.Load<TextAsset>(BrandIntroMorph.IconResource);
            Assert.That(asset, Is.Not.Null, "Resources/AF/Brand/af_icon_vista.bytes");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(tex.LoadImage(asset!.bytes), Is.True, "un PNG");
            Assert.That(tex.width, Is.EqualTo(432));
            Assert.That(tex.height, Is.EqualTo(432));
            Object.Destroy(tex);
            Resources.UnloadAsset(asset);
            yield return null;
        }

        private BrandIntro NewIntro(float? density)
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            GameScreen.SimulatedPxPerDp = density;
            _host ??= new GameObject("BrandIntroMorphTestHost");
            _splash?.Dispose();
            _splash = new BootSplash(_host.transform, withSloth: false, withIntro: true);
            return _splash.Intro!;
        }
    }
}
