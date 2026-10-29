#nullable enable
using System.Collections;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
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

        [TearDown]
        public void TearDown()
        {
            _splash?.Dispose();
            _splash = null;
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            BrandIntro.FrameVisibleOverrideForTests = null;
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
            Assert.That((Color32)intro.Morph.Iris.color, Is.EqualTo(BrandIntro.Petroleum));
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
