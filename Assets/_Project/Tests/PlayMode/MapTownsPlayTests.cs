#nullable enable
using System;
using System.Collections;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El Mapa del río v2, bloque M2-2 «Pueblos de paso» (D-069; <c>docs/dev/propuesta-mapa-rio-v2.md</c> §4, §7.2, §7.3 y §9.5), en la
    /// escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>los pueblos de los tramos abiertos con su viñeta; tocar uno pasado abre su ficha («{lugar} · {país} · río {río}»), con
    /// su área táctil de 48 dp y su etiqueta de TalkBack («Coari · pueblo de paso · Brasil»); uno que falta no se toca;</item>
    /// <item>los rótulos solo a un tramo o menos de la canoa, y en la última parada alcanzada y la siguiente;</item>
    /// <item>la excursión local de un tramo de 0 km: las boyas por el lazo y la canoa fuera de la parada;</item>
    /// <item>con <c>ui_map_towns</c> apagada, el Mapa de M2-1 (los puntos de la 1.0, la fila corta de boyas, sin atlas);</item>
    /// <item>el atlas se suelta al cerrar (§7.2).</item>
    /// </list>
    /// </summary>
    public sealed class MapTownsPlayTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 7, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameBootstrap.A11yHostOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator TapAPassedTown_OpensItsCard_WithItsTouchAreaAndTalkBack()
        {
            yield return OpenAt(76, true); // max 75: la canoa va hacia Leticia; Coari (68) ya se pasó
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            yield return null;
            Assert.That(map.TownsActive, Is.True, "ui_map_towns encendida por defecto (08 §9)");
            Assert.That(map.TownArt.Built, Is.True);
            Assert.That(map.TownCount, Is.GreaterThan(10), "los pueblos de los mundos 1-4");
            int coari = map.TownIndexOf("coari");
            Assert.That(coari, Is.GreaterThanOrEqualTo(0));
            Assert.That(map.TownPassed(coari), Is.True, "Coari se pasa en el 68 (f = 0,73 del tramo 61-70)");
            Assert.That(A11yLabels.TryGet(map.TownTouch(coari).gameObject, out string label, out _), Is.True);
            Assert.That(label, Is.EqualTo("Coari · pueblo de paso · Brasil"), "propuesta §7.3");
            int tabatinga = map.TownIndexOf("tabatinga");
            Assert.That(tabatinga, Is.GreaterThanOrEqualTo(0), "el tramo de la canoa (71-80) se ve, aunque Leticia sea del mundo 4");
            Assert.That(map.TownPassed(tabatinga), Is.False, "Tabatinga, al final del tramo de Leticia: aún no");
            Assert.That(map.TownPassed(map.TownIndexOf("fonte_boa")), Is.True, "Fonte Boa, en el 73");
            Assert.That(map.TownIndexOf("almeirim"), Is.GreaterThanOrEqualTo(0), "los pueblos de los mundos anteriores");
            Assert.That(map.TownIndexOf("indiana"), Is.EqualTo(-1), "los del mundo 5, aún cerrado, no");
            Assert.That(A11yLabels.TryGet(map.TownTouch(tabatinga).gameObject, out string city, out _), Is.True);
            Assert.That(city, Is.EqualTo("Tabatinga · ciudad de paso · Brasil"));

            int shown = -1;
            for (int i = 0; i < map.TownCount && shown < 0; i++)
            {
                if (map.TownInView(i) && map.TownPassed(i))
                {
                    shown = i;
                }
            }

            Assert.That(shown, Is.GreaterThanOrEqualTo(0), "algún pueblo pasado a la vista");
            Rect touch = map.TownTouch(shown).rect;
            Assert.That(touch.width, Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp));
            Assert.That(touch.height, Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp));
            Assert.That(A11yLabels.TryGet(map.TownTouch(shown).gameObject, out string shownLabel, out _), Is.True);
            Assert.That(shownLabel, Is.EqualTo(TownText.A11yTown(map.TownAt(shown))), "su área táctil lleva la etiqueta de TalkBack");

            Assert.That(map.TapTown(shown), Is.True);
            yield return null;
            Assert.That(map.CardOpen, Is.True, "abre su ficha");
            StopCard card = map.Card!;
            Assert.That(card.Passing, Is.SameAs(map.TownAt(shown)));
            string name = AF.Localization.Loc.Fact(map.TownAt(shown).NameKey);
            StringAssert.StartsWith(name, card.TitleText, "nombre · país · río");
            if (AF.Localization.Loc.Fact(map.TownAt(shown).RiverKey).Length > 0)
            {
                StringAssert.Contains(AF.Localization.Loc.Fact(map.TownAt(shown).RiverKey), card.TitleText, "con su río");
            }
            card.Hide();
            yield return null;
            Assert.That(map.TapTown(tabatinga), Is.False, "un pueblo que falta no abre ficha");
            Assert.That(boot.CloseRiverMap(), Is.True);
        }

        [UnityTest]
        public IEnumerator Labels_OnlyNearTheCanoe_AndOnTheLastAndNextStops()
        {
            yield return OpenAt(58, true); // max 57: la canoa va de Manaos (5) a Anavilhanas (6)
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            yield return null;
            // Manaos (5), la última alcanzada, va en abanico con el Encuentro (4): sin rótulo, para no pisarse.
            Assert.That(map.StopLabelVisible(6), Is.True, "la siguiente");
            Assert.That(map.StopLabelVisible(4), Is.False);
            Assert.That(map.StopLabelVisible(7), Is.False, "Tefé, más allá: sin rótulo");
            for (int i = 0; i < map.TownCount; i++)
            {
                string id = map.TownAt(i).Id;
                if (id == "obidos" || id == "parintins" || id == "breves" || id == "monte_alegre")
                {
                    Assert.That(map.TownLabelVisible(i), Is.False, id + ": a más de un tramo, solo su viñeta (§9.5)");
                }
            }

            int manacapuru = map.TownIndexOf("manacapuru");
            Assert.That(manacapuru, Is.GreaterThanOrEqualTo(0), "el tramo de Tefé, el siguiente al de la canoa");
            Assert.That(map.TownPassed(manacapuru), Is.False);
            Assert.That(map.TownLabelText(manacapuru), Is.EqualTo("Manacapuru"), "a un tramo de la canoa: con rótulo (si no pisa nada)");
            int labels = 0;
            for (int i = 0; i < map.TownCount; i++)
            {
                labels += map.TownLabelVisible(i) ? 1 : 0;
            }

            Assert.That(labels, Is.GreaterThan(0), "algún pueblo cerca de la canoa lleva rótulo");

            for (int i = 0; i < map.TownCount; i++)
            {
                if (map.TownShown(i))
                {
                    Assert.That(map.TownVignetteDp(i), Is.EqualTo(RiverMapScreen.TownWidthDp(map.Zoom)), "la viñeta a la medida del zoom (§9.5)");
                }
            }
            Assert.That(GameBootstrap.Current!.CloseRiverMap(), Is.True);
        }

        [UnityTest]
        public IEnumerator Excursion_InAZeroLeg_TheBuoysLoopAndTheCanoeLeavesTheStop()
        {
            yield return OpenAt(86, true); // max 85: Leticia al atardecer (81-90), un tramo de 0 km
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            yield return null;
            Assert.That(map.BuoysCompact, Is.False, "las 10 boyas por el lazo de la excursión, no en la fila corta");
            Vector2 stop = RiverMapScreen.PlacePoint(boot.RiverMapData!, boot.RiverRoutes!.At(9)!.At);
            Assert.That(Vector2.Distance(map.CanoeMapPoint, stop), Is.GreaterThan(1e-5f), "la canoa, de excursión");
            Assert.That(Vector2.Distance(map.BuoyMapPoint(9), stop), Is.LessThan(1e-4f), "la boya del 90, de vuelta en la parada");
            Assert.That(Vector2.Distance(map.BuoyMapPoint(1), map.BuoyMapPoint(7)), Is.GreaterThan(1e-5f), "ida y vuelta por orillas distintas");
            Assert.That(boot.CloseRiverMap(), Is.True);

            // Con ui_map_towns apagada: como M2-1, la fila corta bajo la parada y la canoa en ella.
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapTowns = false };
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
            Assert.That(map.TownsActive, Is.False);
            Assert.That(map.BuoysCompact, Is.True);
            Assert.That(Vector2.Distance(map.CanoeMapPoint, stop), Is.LessThan(1e-5f));
            Assert.That(boot.CloseRiverMap(), Is.True);
        }

        [UnityTest]
        public IEnumerator TownsOff_LooksLikeM21_AndTheAtlasIsReleasedOnClose()
        {
            yield return OpenAt(76, false);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            yield return null;
            Assert.That(map.LiveActive, Is.True, "el río vivo sigue");
            Assert.That(map.TownsActive, Is.False);
            Assert.That(map.TownCount, Is.EqualTo(0));
            Assert.That(map.TownArt.Built, Is.False, "sin atlas de pueblos");
            Assert.That(map.StopLabelVisible(7), Is.False, "sin los rótulos nuevos");
            bool coariDot = false;
            for (int i = 0; i < map.PassingCount; i++)
            {
                coariDot |= map.PassingAt(i).Id == "coari";
            }

            Assert.That(coariDot, Is.True, "Coari, con el punto de la 1.0");
            Assert.That(boot.CloseRiverMap(), Is.True);

            // Encendida: los pueblos salen con su viñeta y ya no como punto; al cerrar, el atlas se suelta.
            GameBootstrap.MapLiveConfigOverride = null;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
            Assert.That(map.TownsActive, Is.True);
            for (int i = 0; i < map.PassingCount; i++)
            {
                Assert.That(map.PassingAt(i).IsTown, Is.False, "los puntos de la 1.0 quedan para las bocas y los rápidos");
            }

            Assert.That(map.TownArt.TextureBytes, Is.LessThanOrEqualTo(3L * 1024L * 1024L), "§7.2");
            Assert.That(map.LiveArt.TextureBytes + map.TownArt.TextureBytes, Is.LessThanOrEqualTo(6L * 1024L * 1024L), "+6 MiB con el Mapa abierto");
            Assert.That(boot.CloseRiverMap(), Is.True);
            yield return null;
            Assert.That(map.TownArt.Built, Is.False, "se suelta al cerrar");
        }

        [UnityTest]
        public IEnumerator ReviewedNames_AreVignettesWithTowns_AndDotsWithout()
        {
            yield return OpenAt(76, true);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(boot.CloseRiverMap(), Is.True);
            LocCatalog catalog = Loc.Catalog;
            string locale = Loc.Locale;
            try
            {
                // Como en release: los nombres nuevos, aprobados en la revisión 4 de Gemini (D-054, 2026-11-09), ya salen.
                Loc.Configure(catalog, locale, false);
                Assert.That(Loc.Fact("map.stop.manacapuru.name"), Is.EqualTo("Manacapuru"), "revisión 4 de D-054");
                Assert.That(Loc.Fact("map.stop.coari.name"), Is.EqualTo("Coari"), "los de la 1.0, aprobados");
                foreach (bool towns in new[] { true, false })
                {
                    GameBootstrap.MapLiveConfigOverride = towns ? null : new MapLiveConfig { UiMapTowns = false };
                    Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
                    yield return null;
                    string where = towns ? "con ui_map_towns" : "sin ui_map_towns";
                    Assert.That(map.TownIndexOf("manacapuru") >= 0, Is.EqualTo(towns), where + ": viñeta solo con los pueblos");
                    // Con nombre aprobado, sin los pueblos sale como punto de la 1.0 (RiverMapScreen.BuildPassing); con ellos, solo su viñeta.
                    Assert.That(HasDot(map, "manacapuru"), Is.EqualTo(!towns), where + ": punto de la 1.0 solo sin los pueblos");
                    for (int i = 0; i < map.PassingCount; i++)
                    {
                        Assert.That(Loc.Fact(map.PassingAt(i).NameKey), Is.Not.Empty, where + ": ningún punto sin nombre");
                    }

                    Assert.That(towns ? map.TownIndexOf("coari") >= 0 : HasDot(map, "coari"), Is.True, where + ": Coari sí");
                    Assert.That(boot.CloseRiverMap(), Is.True);
                    yield return null;
                }
            }
            finally
            {
                Loc.Configure(catalog, locale, true);
            }
        }

        private static bool HasDot(RiverMapScreen map, string id)
        {
            for (int i = 0; i < map.PassingCount; i++)
            {
                if (map.PassingAt(i).Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerator OpenAt(int playLevel, bool towns)
        {
            GameBootstrap.MapLiveConfigOverride = towns ? null : new MapLiveConfig { UiMapTowns = false };
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = playLevel;
            profile.Progress.MaxLevel = playLevel - 1;
            profile.Progress.MapSeenLevel = playLevel - 1; // sin «ponerse al día»
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            yield return null;
            Assert.That(GameBootstrap.Current!.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
        }
    }

    /// <summary>
    /// Capturas de M2-2 (D-069; propuesta del Mapa v2 §9.3): tres tramos a 360 dp con <c>ui_map_towns</c> encendida (<c>pueblos</c>)
    /// y apagada (<c>m21</c>): el Estuario y el bajo Amazonas (jugando el 11, en Santarém: Almeirim, Prainha y Monte Alegre ya pasados), Manaos → Tefé (jugando el
    /// 66: Manacapuru pasado, Codajás y Coari por pasar) y la excursión de Sandoval al aguajal (jugando el 316, un tramo de 0 km).
    /// Los PNG quedan en <c>Logs/snapshots/es-419/m22_&lt;tramo&gt;_360dp_&lt;pueblos|m21&gt;.png</c>, con su línea <c>AF_MAP</c>.
    /// Explícito: necesita gráficos (sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas del Mapa v2 (M2-2); necesitan dispositivo gráfico.")]
    public sealed class MapTownsSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 7, 15, 0, 0, DateTimeKind.Utc);

        private static readonly (string Name, int Play)[] Stretches =
        {
            ("estuario", 11),
            ("manaos_tefe", 66),
            ("sandoval_excursion", 316),
        };

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureThreeStretches_At360dp_WithAndWithoutTowns()
        {
            string dir = SnapshotKit.Prepare();
            foreach ((string name, int play) in Stretches)
            {
                foreach (bool towns in new[] { true, false })
                {
                    GameScreen.SimulatedPxPerDp = 3f;
                    GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapTowns = towns };
                    Profile profile = Wallet.NewProfile(new ManualClock(T0));
                    profile.Progress.NextLevel = play;
                    profile.Progress.MaxLevel = play - 1;
                    profile.Progress.MapSeenLevel = play - 1;
                    profile.Wallet.Seeds = 1250;
                    yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
                    GameBootstrap boot = GameBootstrap.Current!;
                    SnapshotKit.AttachLobby(boot);
                    yield return null;
                    Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True, name);
                    RiverMapScreen map = boot.RiverMapView!;
                    Assert.That(map.TownsActive, Is.EqualTo(towns));
                    SnapshotKit.AttachOverlay(map.Canvas);
                    yield return SnapshotKit.Wait(0.4f);
                    string file = "m22_" + name + "_360dp_" + (towns ? "pueblos" : "m21");
                    SnapshotKit.Save(dir, file);
                    int shown = 0;
                    int labels = 0;
                    for (int i = 0; i < map.TownCount; i++)
                    {
                        shown += map.TownShown(i) ? 1 : 0;
                        labels += map.TownLabelVisible(i) ? 1 : 0;
                    }

                    Debug.Log("AF_MAP " + file + ": pueblos " + map.TownCount + ", a la vista " + shown + ", con rótulo " + labels
                        + ", zoom " + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture) + ", boyas en fila " + map.BuoysCompact
                        + ", arte " + ((map.LiveArt.TextureBytes + map.TownArt.TextureBytes) / 1024L) + " KiB");
                    boot.CloseRiverMap();
                    yield return null;
                }
            }
        }
    }
}
