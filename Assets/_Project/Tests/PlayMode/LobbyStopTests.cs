#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Ads;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Levels;
using AF.Platform;
using AF.Presentation;
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
    /// El Mapa del río en el inicio, en la escena <c>Game</c> real (tarea M2.1; D-013, punto 6, Q17-A; 06 §4.7; propuesta del
    /// personaje g.2 y d.11):
    /// <list type="bullet">
    /// <item>el fondo es el de la parada de «Jugar» (en el 58, Manaos), el mismo que el del tablero de ese nivel;</item>
    /// <item>«Faltan {n} niveles para {lugar}» bajo «Jugar», en Nunito Bold de 14 sp, correcto en el 51, el 58, el 60 y el 195
    /// (hacia Mazán, la parada 20; M4.5a), y oculto con el fin de contenido, sin parada siguiente en el catálogo (uno de prueba
    /// que acaba en la 19) y con Sin canoas;</item>
    /// <item>la ventanita de casa: «a unos 1.480 km» en Manaos, sin píldora en Belém, oculta con Sin canoas, con la hoja de
    /// compra delante y con el anuncio simulado delante, y sin toques;</item>
    /// <item>sin catálogo, el inicio de siempre;</item>
    /// <item>sin solapes a 360 × 640 ni a 360 × 800 dp (y, a 360 × 800, sin tapar el héroe del fondo);</item>
    /// <item>la línea <c>AF_LAYOUT</c> del inicio con <c>stop_remaining</c> y <c>home_window</c>.</item>
    /// </list>
    /// </summary>
    public sealed class LobbyStopTests
    {
        private const string Nbsp = " ";
        private static readonly DateTime T0 = new DateTime(2026, 10, 15, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator PlayLevel58_ShowsManaos_TheSameBackdropAsTheBoardOf58()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(lobby.RiverStops, Is.SameAs(boot.RiverStops), "el inicio tiene el catálogo del arranque");
            StopBackdrop backdrop = lobby.StopBackdrop;
            RiverStop? stop = backdrop.Stop;
            Assert.That(stop, Is.Not.Null);
            Assert.That(stop!.Id, Is.EqualTo("manaos"), "06 §4.7: el fondo de la parada actual");
            Assert.That(backdrop.Root.gameObject.activeInHierarchy, Is.True);
            Assert.That(backdrop.Fading, Is.False, "corte directo");
            Assert.That(lobby.PanoramaTint, Is.EqualTo(backdrop.LutColor), "la luz de la parada sobre el panorama");
            string hero = backdrop.HeroSource;

            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level));
            yield return null;
            StopBackdrop board = boot.View!.StopBackdrop;
            Assert.That(board.Stop, Is.SameAs(stop), "el mismo sitio que el tablero del 58");
            Assert.That(board.LutColor, Is.EqualTo(backdrop.LutColor));
            Assert.That(board.HeroSource, Is.EqualTo(hero));
            Assert.That(board.Water, Is.EqualTo(backdrop.Water), "con la misma agua del mundo");
        }

        [UnityTest]
        public IEnumerator StopRemaining_IsRightAt51_58And60_InNunitoBoldOf14sp()
        {
            var cases = new (int Level, string Text)[]
            {
                (51, "Faltan 10 niveles para Anavilhanas"),
                (58, "Faltan 3 niveles para Anavilhanas"),
                (60, "Falta 1 nivel para Anavilhanas"),
            };
            foreach ((int level, string text) in cases)
            {
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(level)));
                LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
                yield return null;
                Assert.That(lobby.StopRemainingShown, Is.True, "nivel " + level);
                Assert.That(lobby.StopRemainingText, Is.EqualTo(text), "nivel " + level + ": con el plural de la tabla");
                TextMeshProUGUI label = lobby.StopRemainingLabel;
                Assert.That(label.fontSize, Is.EqualTo(LobbyScreen.StopRemainingSp * UiBuilder.TextScale).Within(0.01f));
                Assert.That(LobbyScreen.StopRemainingSp, Is.EqualTo(14f));
                TMP_FontAsset? bold = UiFonts.Font(TextStyle.PlainBold);
                if (bold != null)
                {
                    Assert.That(label.font, Is.SameAs(bold), "Nunito Bold");
                }

                Assert.That(label.raycastTarget, Is.False);
                Rect play = CanvasRect((RectTransform)lobby.PlayButton.transform, lobby);
                Rect remaining = CanvasRect(lobby.StopRemainingRect, lobby);
                Assert.That(remaining.yMin, Is.GreaterThanOrEqualTo(play.yMax - 0.5f), "bajo «Jugar»");
                Assert.That(remaining.center.x, Is.EqualTo(play.center.x).Within(1f), "centrado con «Jugar»");
                Assert.That(remaining.width, Is.LessThanOrEqualTo(play.width + 0.5f), "nunca más ancha que «Jugar»");
            }
        }

        [UnityTest]
        public IEnumerator StopRemaining_WithTheLongestName_StaysInThePlayColumn_InTwoLines()
        {
            // En el 28 (Alter do Chão) la parada siguiente es la de nombre más largo: la placa no pasa del ancho de «Jugar» y el
            // texto baja a una segunda línea, a 14 sp y sin autoajuste, entero dentro de la placa.
            GameScreen.SimulatedScreen = new Vector4(1080, 1920, 96f, 48f);
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(28)));
            LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(lobby.StopRemainingShown, Is.True);
            Assert.That(lobby.StopRemainingText, Is.EqualTo("Faltan 3 niveles para Floresta Nacional do Tapajós"));
            TextMeshProUGUI label = lobby.StopRemainingLabel;
            Assert.That(label.enableAutoSizing, Is.False);
            Assert.That(label.fontSize, Is.EqualTo(LobbyScreen.StopRemainingSp * UiBuilder.TextScale).Within(0.01f));

            Rect play = CanvasRect((RectTransform)lobby.PlayButton.transform, lobby);
            Rect remaining = CanvasRect(lobby.StopRemainingRect, lobby);
            Assert.That(remaining.width, Is.LessThanOrEqualTo(play.width + 0.5f), "la columna de «Jugar»");
            Assert.That(remaining.center.x, Is.EqualTo(play.center.x).Within(1f));
            Assert.That(remaining.yMin, Is.GreaterThanOrEqualTo(play.yMax - 0.5f));

            label.ForceMeshUpdate();
            Assert.That(label.textInfo.lineCount, Is.EqualTo(2), "en dos líneas");
            Assert.That(label.isTextOverflowing, Is.False, "entero");
            Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(lobby.StopRemainingRect.rect.height + 0.5f), "dentro de la placa");
        }

        [UnityTest]
        public IEnumerator StopRemaining_HidesAtTheContentEnd_AndWithoutANextStop_AndWithoutCatalogTheLobbyIsAsBefore()
        {
            // Con las 42 paradas de 1-420 (M4.5a), del 191 al 200 la parada siguiente (la 20, Mazán) está en el catálogo y el
            // pack de 201-300 está publicado: «Faltan…» hacia Mazán.
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(195)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            RiverStopCatalog full = lobby.RiverStops!;
            Assert.That(lobby.StopBackdrop.Stop!.N, Is.EqualTo(19));
            Assert.That(full.At(20)!.Id, Is.EqualTo("mazan"), "premisa: la parada 20 está en el catálogo");
            Assert.That(boot.App!.PlayLevelAvailable, Is.True, "premisa: el 195 está publicado");
            Assert.That(lobby.StopRemainingShown, Is.True, "con la parada siguiente en el catálogo");
            Assert.That(lobby.StopRemainingText, Is.EqualTo("Faltan 6 niveles para Mazán"));
            Assert.That(lobby.HomeWindow.Visible, Is.True);

            // Sin parada siguiente en el catálogo (uno de prueba con las paradas 0-19, que acaba en el mundo 5) y con el 195
            // publicado, para no confundirlo con el fin de contenido: sin «Faltan…», con la ventanita.
            lobby.RiverStops = Truncated(full, 20);
            yield return null;
            Assert.That(lobby.RiverStops!.At(20), Is.Null, "premisa: sin la parada 20");
            Assert.That(boot.App.PlayLevelAvailable, Is.True, "premisa: no es el fin de contenido");
            Assert.That(lobby.StopBackdrop.Stop!.N, Is.EqualTo(19));
            Assert.That(lobby.StopRemainingShown, Is.False, "sin la parada siguiente en el catálogo");
            Assert.That(lobby.HomeWindow.Visible, Is.True);

            // Sin catálogo: el inicio de siempre (sin fondo de parada, sin «Faltan…» y sin ventanita).
            lobby.RiverStops = null;
            yield return null;
            Assert.That(lobby.StopBackdrop.Stop, Is.Null);
            Assert.That(lobby.StopBackdrop.Root.gameObject.activeSelf, Is.False);
            Assert.That(lobby.PanoramaTint, Is.EqualTo(Color.white), "el panorama sin tinte, como antes");
            Assert.That(lobby.StopRemainingShown, Is.False);
            Assert.That(lobby.HomeWindow.Visible, Is.False);
            Assert.That(lobby.PlayLabel, Is.EqualTo("Jugar · Nivel 195"));

            // Fin de contenido con la parada actual y la siguiente en el catálogo: un catálogo de niveles que acaba en el 1 y el
            // perfil en el 58 (Manaos, con Anavilhanas detrás). «Faltan…» está armado, pero el 58 no está publicado: no sale.
            LaunchOptions.LevelPackJsonOverride = PackEndingAtLevel1();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58)));
            boot = GameBootstrap.Current!;
            lobby = boot.Lobby!;
            yield return null;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.App.PlayLevel, Is.EqualTo(58));
            Assert.That(boot.App.PlayLevelAvailable, Is.False, "premisa: el 58 no está publicado");
            RiverStop? current = lobby.StopBackdrop.Stop;
            Assert.That(current, Is.Not.Null, "premisa: la parada actual está en el catálogo");
            Assert.That(current!.Id, Is.EqualTo("manaos"));
            Assert.That(lobby.RiverStops!.At(current.N + 1), Is.Not.Null, "premisa: la parada siguiente está en el catálogo");
            Assert.That(lobby.StopRemainingText, Is.EqualTo("Faltan 3 niveles para Anavilhanas"), "premisa: «Faltan…» armado");
            Assert.That(lobby.StopRemainingShown, Is.False, "sin nivel publicado no hay «Faltan…»");
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
            yield return null;
            Assert.That(lobby.StopRemainingShown, Is.False, "oculto con «El río sigue creciendo»");
            Assert.That(boot.App.CloseOverlay(), Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            yield return null;
            Assert.That(lobby.StopRemainingShown, Is.False, "de vuelta en el inicio, el 58 sigue sin publicar");
        }

        [UnityTest]
        public IEnumerator HomeWindow_SaysAbout1480KmInManaos_AndHasNoPillInBelem()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58)));
            LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
            yield return null;
            HomeWindow home = lobby.HomeWindow;
            Assert.That(home.Visible, Is.True, "Q17-A: TiTino en la ventanita de casa");
            Assert.That(home.Stop!.Id, Is.EqualTo("manaos"));
            Assert.That(home.PillShown, Is.True);
            Assert.That(home.DistanceText, Is.EqualTo("Casa · a unos 1.480" + Nbsp + "km"));
            Assert.That(home.PortraitSprite, Is.Not.Null);
            foreach (Graphic g in home.Root.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(g.raycastTarget, Is.False, g.name + ": sin toques hasta M3.3");
            }

            foreach (Graphic g in home.PillRect.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(g.raycastTarget, Is.False, g.name + ": sin toques");
            }

            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(1)));
            lobby = GameBootstrap.Current!.Lobby!;
            yield return null;
            Assert.That(lobby.HomeWindow.Visible, Is.True, "en Belém, la ventanita");
            Assert.That(lobby.HomeWindow.PillShown, Is.False, "en casa: sin píldora [P]");
            Assert.That(lobby.StopRemainingText, Is.EqualTo("Faltan 10 niveles para Santarém"));
        }

        [UnityTest]
        public IEnumerator HomeWindow_HidesWithNoCanoes_AndWithThePurchaseSheet()
        {
            var clock = new ManualClock(T0);
            Profile profile = ProfileAt(58, clock);
            profile.Wallet.Canoes.Count = 0;
            profile.Wallet.Canoes.NextRegenAt = T0 + TimeSpan.FromMinutes(18);
            LaunchOptions.ClockOverride = clock;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            Assert.That(lobby.HomeWindow.Visible, Is.True);
            Assert.That(lobby.StopRemainingShown, Is.True);

            // Sin canoas: ni la ventanita ni «Faltan…» (d.11: TiTino nunca en Sin canoas).
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            yield return null;
            Assert.That(lobby.HomeWindow.Visible, Is.False, "oculta con Sin canoas");
            Assert.That(lobby.HomeWindow.PillShown, Is.False);
            Assert.That(lobby.StopRemainingShown, Is.False);
            lobby.BackButton.onClick.Invoke();
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            yield return null;
            Assert.That(lobby.HomeWindow.Visible, Is.True, "vuelve con el inicio");
            Assert.That(lobby.StopRemainingShown, Is.True);

            // La hoja «Compra simulada» delante del inicio (g.2, regla 9): la ventanita se va mientras dure.
            StoreSheet sheet = boot.StoreSheetView!;
            boot.Store!.Purchase(new PurchaseRequest(StoreCatalog.NoAds), r => { });
            Assert.That(sheet.Visible, Is.True);
            yield return null;
            Assert.That(lobby.CommerceOpen, Is.True);
            Assert.That(lobby.HomeWindow.Visible, Is.False, "oculta con la hoja de compra");
            sheet.CancelButton.onClick.Invoke();
            Assert.That(sheet.Visible, Is.False);
            yield return null;
            Assert.That(lobby.CommerceOpen, Is.False);
            Assert.That(lobby.HomeWindow.Visible, Is.True);
        }

        [UnityTest]
        public IEnumerator HomeWindow_HidesWithTheSimulatedAdOverTheLobby()
        {
            var clock = new ManualClock(T0);
            LaunchOptions.ClockOverride = clock;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58, clock)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(lobby.CommerceOpen, Is.False);
            Assert.That(lobby.HomeWindow.Visible, Is.True);

            // El anuncio simulado delante del inicio (g.2, regla 9; d.11): la ventanita se va mientras dure, sin Sin canoas
            // de por medio (la otra rama de CommerceOpen, la de AdOverlay).
            AdOverlay ad = boot.AdOverlayView!;
            Assert.That(boot.Ads!.CheckRewarded(AdPlacement.Hint).IsAllowed, Is.True);
            AdResult? result = null;
            boot.Ads.ShowRewarded(AdPlacement.Hint, r => result = r);
            Assert.That(ad.Visible, Is.True, "el anuncio simulado está delante del inicio");
            yield return null;
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "sin otra pantalla debajo");
            Assert.That(boot.StoreSheetView == null || !boot.StoreSheetView.Visible, Is.True, "sin la hoja de compra");
            Assert.That(lobby.CommerceOpen, Is.True);
            Assert.That(lobby.HomeWindow.Visible, Is.False, "oculta con el anuncio");
            Assert.That(lobby.HomeWindow.PillShown, Is.False);

            // Se cierra al acabar la cuenta atrás: la ventanita vuelve.
            ad.Tick((float)FakeAdService.RewardedDuration.TotalMilliseconds);
            ad.CloseButton.onClick.Invoke();
            Assert.That(ad.Visible, Is.False);
            Assert.That(result, Is.Not.Null, "el anuncio avisa al cerrarse");
            yield return null;
            Assert.That(lobby.CommerceOpen, Is.False);
            Assert.That(lobby.HomeWindow.Visible, Is.True, "vuelve con el inicio");
        }

        [UnityTest]
        public IEnumerator Layout_HasNoOverlaps_At360x640And360x800()
        {
            var clock = new ManualClock(T0);
            Profile profile = ProfileAt(58, clock);
            profile.Wallet.Canoes.Count = 3;
            profile.Wallet.Canoes.NextRegenAt = T0 + TimeSpan.FromMinutes(10);
            profile.Wallet.Seeds = 12345;
            LaunchOptions.ClockOverride = clock;
            var store = new MemoryProfileStore(profile);
            foreach (Vector2Int size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2400) })
            {
                GameScreen.SimulatedScreen = new Vector4(size.x, size.y, 96f, 48f);
                yield return PlayModeHelpers.LoadApp(store);
                LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
                yield return null;
                Canvas.ForceUpdateCanvases();
                string where = size.x + " × " + size.y;
                Assert.That(lobby.HomeWindow.Visible, Is.True, where);
                Assert.That(lobby.HomeWindow.PillShown, Is.True, where);
                Assert.That(lobby.StopRemainingShown, Is.True, where);
                Assert.That(lobby.CanoesShown, Is.True, where);

                var named = new List<KeyValuePair<string, Rect>>
                {
                    new KeyValuePair<string, Rect>("semillas", CanvasRect(lobby.SeedsRect, lobby)),
                    new KeyValuePair<string, Rect>("canoas", CanvasRect((RectTransform)lobby.CanoesButton.transform, lobby)),
                    new KeyValuePair<string, Rect>("ajustes", CanvasRect((RectTransform)lobby.SettingsButton.transform, lobby)),
                    new KeyValuePair<string, Rect>("jugar", CanvasRect((RectTransform)lobby.PlayButton.transform, lobby)),
                    new KeyValuePair<string, Rect>("inicio", CanvasRect(lobby.HomeTabRect, lobby)),
                    new KeyValuePair<string, Rect>("faltan", CanvasRect(lobby.StopRemainingRect, lobby)),
                    new KeyValuePair<string, Rect>("ventanita", CanvasRect(lobby.HomeWindow.Root, lobby)),
                    new KeyValuePair<string, Rect>("píldora de casa", CanvasRect(lobby.HomeWindow.PillRect, lobby)),
                };
                if (lobby.ChipShown)
                {
                    named.Add(new KeyValuePair<string, Rect>("chip", CanvasRect(lobby.ChipRect, lobby)));
                }

                float w = lobby.CanvasWidth;
                float h = lobby.CanvasHeight;
                for (int i = 0; i < named.Count; i++)
                {
                    Rect r = named[i].Value;
                    Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.5f), where + ": " + named[i].Key + " dentro de la pantalla");
                    Assert.That(r.xMax, Is.LessThanOrEqualTo(w + 0.5f), where + ": " + named[i].Key + " dentro de la pantalla");
                    Assert.That(r.yMin, Is.GreaterThanOrEqualTo(lobby.SafeTop - 0.5f), where + ": " + named[i].Key + " bajo el recorte");
                    Assert.That(r.yMax, Is.LessThanOrEqualTo(h - lobby.SafeBottom + 0.5f), where + ": " + named[i].Key + " sobre la zona de gestos");
                    for (int j = i + 1; j < named.Count; j++)
                    {
                        Assert.That(Shrunk(named[i].Value).Overlaps(Shrunk(named[j].Value)), Is.False, where + ": " + named[i].Key + " y " + named[j].Key + " se solapan");
                    }
                }

                // El texto de «Faltan…» y el de la píldora caben en su caja y no bajan de 12 sp.
                Assert.That(lobby.StopRemainingLabel.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), where);
                Assert.That(UiBuilder.MinTextWidth(lobby.StopRemainingLabel), Is.LessThanOrEqualTo(lobby.StopRemainingRect.rect.width + 0.5f), where);

                // A 360 × 800 dp (20:9, los teléfonos de hoy), la ventanita y su píldora no tapan el héroe del fondo.
                Rect hero = lobby.StopBackdrop.HeroRect;
                Debug.Log("AF_LOBBY " + where + ": héroe " + hero + ", ventanita " + named[6].Value + ", píldora " + named[7].Value + ", faltan " + named[5].Value);
                if (size.y == 2400 && hero.width > 0f)
                {
                    Assert.That(Shrunk(named[6].Value).Overlaps(hero), Is.False, where + ": la ventanita tapa el héroe");
                    Assert.That(Shrunk(named[7].Value).Overlaps(hero), Is.False, where + ": la píldora tapa el héroe");
                }
            }
        }

        [UnityTest]
        public IEnumerator AfLayout_OfTheLobby_PublishesStopRemainingAndHomeWindow()
        {
            using var layout = new AfLayoutLog();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58)));
            LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
            yield return null;
            string? line = layout.LastOf("lobby", 58);
            Assert.That(line, Is.Not.Null, "AF_LAYOUT del inicio");
            string[] ids = AfLayoutLog.ButtonIds(line!);
            Assert.That(ids, Does.Contain("stop_remaining"));
            Assert.That(ids, Does.Contain("home_window"));
            Assert.That(AfLayoutLog.Button(line!, "home_window")!.Value, Is.EqualTo(AfLayoutLog.Published(lobby.HomeWindow.Root)));
            Assert.That(AfLayoutLog.Button(line!, "stop_remaining")!.Value, Is.EqualTo(AfLayoutLog.Published(lobby.StopRemainingRect)));
        }

        private static Profile ProfileAt(int nextLevel, IClock? clock = null)
        {
            Profile profile = Wallet.NewProfile(clock ?? new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        // Catálogo de prueba con las paradas 0 … count − 1 de stops (las mismas instancias).
        private static RiverStopCatalog Truncated(RiverStopCatalog stops, int count)
        {
            var kept = new RiverStop[count];
            for (int n = 0; n < count; n++)
            {
                kept[n] = stops.At(n)!;
            }

            return new RiverStopCatalog(stops.Source, kept);
        }

        // Paquete de niveles de un solo nivel, el 1: el catálogo acaba ahí y todo lo demás es fin de contenido.
        private static string PackEndingAtLevel1()
        {
            const string level = "{ \"id\": \"m01_v1_n0001\", \"level\": 1, \"world\": 1, \"lap\": 1, \"water_base\": \"white\", "
                + "\"palette\": [\"acai\", \"aguaje\"], \"units_per_color\": 4, \"caps\": [4, 4, 4], "
                + "\"cuias\": [[8, 8, 8, 4], [4, 4, 4, 8], []], \"attrs\": [null, null, null], \"murky\": false, \"hidden\": [], "
                + "\"difficulty\": { \"label\": \"normal\", \"par\": 3 }, \"relief\": [] }";
            return "{ \"subset_of\": \"af-level/1\", \"pack_id\": \"test_content_end\", \"kind\": \"campaign\", \"world\": 1, "
                + "\"world_id\": \"w01_estuario\", \"lap\": 1, \"water_base\": \"white\", \"levels\": [ " + level + " ] }";
        }

        // Medio dp menos por lado: dos cajas que solo se tocan no se solapan.
        private static Rect Shrunk(Rect r)
        {
            return Rect.MinMaxRect(r.xMin + 0.5f, r.yMin + 0.5f, r.xMax - 0.5f, r.yMax - 0.5f);
        }

        // Rectángulo de rt en dp del lienzo del inicio, con origen en su esquina superior izquierda y la y hacia abajo.
        private static Rect CanvasRect(RectTransform rt, LobbyScreen lobby)
        {
            var root = (RectTransform)lobby.Canvas.transform;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Rect bounds = root.rect;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = root.InverseTransformPoint(corners[i]);
                float x = local.x - bounds.xMin;
                float y = bounds.yMax - local.y;
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
