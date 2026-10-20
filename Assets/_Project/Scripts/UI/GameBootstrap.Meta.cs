#nullable enable
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Presentation;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Arranque de la meta de B14 (tanda 5), parcial nueva con una sola llamada en <c>GameBootstrap.cs</c> (<see cref="SetUpMeta"/>,
    /// tras <c>SetUpStreak</c>):
    /// <list type="bullet">
    /// <item><description><b>Skins por hito</b> (06 §4.5; 07 §4.6; D-033, Q-M8 (a)): crea el <see cref="SkinService"/> sobre el perfil
    /// y la vía síncrona del lote (como la cartera), da las de los mundos ya cerrados que falten (<see cref="SkinService.Backfill"/>),
    /// pinta las cuias con la puesta (<see cref="ProceduralArt.CuiaSkin"/>) y lo inyecta en el flujo de campaña
    /// (<see cref="LevelFlow.UseSkins"/>). Al ganar una, el aviso «{skin} · puesta» (<c>skin_equipped_toast</c>) sale arriba, en su
    /// propio lienzo, en la pantalla siguiente (el nivel siguiente, cuyas cuias ya la llevan, o el inicio): sobre la victoria
    /// taparía el bocadillo de TiTino, y así no se tocan sus archivos. La pantalla de elegir llega con el Mercado (B5): <see cref="SkinService.Select"/> ya está.</description></item>
    /// <item><description><b>Álbum de frutas</b> (08 §4.3 y §8.2): crea el <see cref="FruitAlbum"/> sobre el lote del perfil y lo
    /// inyecta en el flujo; la sonda <c>fruit_discovered</c> sale por cada tinte nuevo de cada carga.</description></item>
    /// <item><description><b>El Mapa y Ajustes</b>: la Canasta de cada parada con las reglas de B1a (<see cref="RiverMapScreen.BasketFor"/>)
    /// y el catálogo de paradas para «Datos del río» (<see cref="SettingsScreen.RiverStops"/>).</description></item>
    /// </list>
    /// <see cref="MetaRunner"/> avanza el aviso y las sondas cada fotograma (<c>GameBootstrap.Update</c> no se toca). Sin
    /// asignaciones mientras nada cambia.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Orden del lienzo del aviso de skin: sobre el nivel, el inicio, el Mapa (45) y la ficha de fruta (46).</summary>
        public const int MetaToastSortingOrder = 48;

        /// <summary>El aviso «{skin} · puesta» va este tanto por debajo del borde de arriba de la zona segura (dp) [P].</summary>
        public const float MetaToastTopDp = 64f;

        /// <summary>Texto del aviso de skin (sp).</summary>
        public const float MetaToastSp = 16f;

        private SkinService? _skinService;
        private FruitAlbum? _fruitAlbum;
        private OverlayCanvas? _metaCanvas;
        private Toast? _skinToast;
        private int _skinSerialSeen;
        private int _skinToastLoad;
        private SkinUnlock? _skinToastPending;
        private int _fruitSeen;

        /// <summary>Servicio de skins (null antes de <see cref="SetUpMeta"/>).</summary>
        public SkinService? Skins => _skinService;

        /// <summary>Álbum de frutas (null antes de <see cref="SetUpMeta"/>).</summary>
        public FruitAlbum? FruitAlbum => _fruitAlbum;

        /// <summary>El aviso de skin (null hasta la primera skin ganada; tests).</summary>
        internal Toast? SkinToast => _skinToast;

        /// <summary>El arte compartido de la escena (capturas: la skin con que se construyen las cuias).</summary>
        internal ProceduralArt? Art => _art;

        /// <summary>El lienzo del aviso de skin (null hasta la primera skin ganada; capturas).</summary>
        internal Canvas? MetaToastCanvas => _metaCanvas?.Canvas;

        /// <summary>Veces que se escribió <c>af_skin_unlock</c> (tests).</summary>
        internal int SkinUnlockCount { get; private set; }

        /// <summary>Veces que se escribió <c>af_fruit_discovered</c>, una por tinte (tests).</summary>
        internal int FruitDiscoveredCount { get; private set; }

        /// <summary>
        /// Crea los servicios de B14 y los engancha (ver el resumen). Devuelve false, sin crear nada, si aún no hay arranque; una
        /// segunda llamada no hace nada y devuelve true.
        /// </summary>
        public bool SetUpMeta()
        {
            if (_skinService != null)
            {
                return true;
            }

            Profile? profile = _profile;
            ProfileSaveBatch? batch = _saveBatch;
            LevelFlow? flow = _flow;
            if (profile == null || batch == null || flow == null || _art == null || _progress == null)
            {
                Debug.LogError(LogPrefix + "SetUpMeta antes del arranque: skins y álbum sin crear.");
                return false;
            }

            _skinService = new SkinService(profile, batch.Durable);
            int backfilled = _skinService.Backfill(_progress.MaxLevel);
            _art.CuiaSkin = CuiaSkin.For(_skinService.Equipped);
            flow.UseSkins(_skinService);
            _fruitAlbum = new FruitAlbum(profile, batch);
            flow.UseAlbum(_fruitAlbum);
            _fruitSeen = flow.FruitDiscoveryCount;
            _skinSerialSeen = flow.SkinUnlockSerial;
            _probe?.SkinState(_skinService.Equipped, profile.Entitlements.Skins, backfilled);

            if (_mapScreen != null)
            {
                _mapScreen.BasketFor = MapBasketFor;
            }

            if (_settingsScreen != null)
            {
                _settingsScreen.RiverStops = RiverStops;
            }

            MetaRunner runner = gameObject.GetComponent<MetaRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<MetaRunner>();
            }

            runner.Bind(this);
            return true;
        }

        /// <summary>
        /// Lo de B14 que pasa cada fotograma (lo llama <see cref="MetaRunner"/>): la skin ganada en la última victoria (sus cuias en
        /// el nivel siguiente, la sonda y el aviso), las frutas descubiertas en la última carga (la sonda) y el aviso a la vista.
        /// </summary>
        public void TickMeta(float dtMs)
        {
            LevelFlow? flow = _flow;
            if (flow == null)
            {
                return;
            }

            if (flow.SkinUnlockSerial != _skinSerialSeen && flow.LatestSkinUnlock != null)
            {
                _skinSerialSeen = flow.SkinUnlockSerial;
                SkinUnlock unlock = flow.LatestSkinUnlock;
                if (_art != null && _skinService != null)
                {
                    _art.CuiaSkin = CuiaSkin.For(_skinService.Equipped); // las cuias del nivel siguiente ya la llevan
                }

                SkinUnlockCount++;
                _probe?.SkinUnlock(unlock.SkinId, unlock.Source, unlock.Level, unlock.Equipped);
                _skinToastPending = unlock;
                _skinToastLoad = flow.LoadSerial;
            }

            // El aviso sale en la pantalla siguiente (el nivel siguiente, con sus cuias ya con la skin, o el inicio), no sobre la
            // victoria, donde taparía el bocadillo de TiTino y se sumaría a su panel; y tras el banner de mundo y la tarjeta previa
            // (LEVEL_CARD), que tras el 40, el 80 y el 140 siempre abren el 41, el 81 y el 141 (D-012): con ellos abiertos, espera.
            if (_skinToastPending != null
                && ((_app != null && _app.Screen != AppScreen.Level)
                    || (flow.LoadSerial != _skinToastLoad && flow.Phase != FlowPhase.LevelCard && flow.WorldBanner == 0)))
            {
                ShowSkinToast(_skinToastPending);
                _skinToastPending = null;
            }

            if (flow.FruitDiscoveryCount != _fruitSeen)
            {
                _fruitSeen = flow.FruitDiscoveryCount;
                IReadOnlyList<int> found = flow.LastFruitsDiscovered;
                for (int i = 0; i < found.Count; i++)
                {
                    FruitDiscoveredCount++;
                    _probe?.FruitDiscovered(AF.GameFlow.FruitAlbum.TintId(found[i]), found[i], flow.LevelNumber, _fruitAlbum!.Fruits);
                }
            }

            Toast? toast = _skinToast;
            if (toast != null && toast.Visible)
            {
                if (_metaCanvas!.Measure())
                {
                    PlaceSkinToast();
                }

                toast.Tick(dtMs);
            }
        }

        // «{skin} · puesta» (Q-M8 (a)) arriba, en su lienzo; sin nombre en el idioma de la UI, sin aviso (la skin ya va puesta).
        private void ShowSkinToast(SkinUnlock unlock)
        {
            string name = UiText.SkinName(unlock.SkinId);
            if (name.Length == 0 || _art == null)
            {
                return;
            }

            if (_metaCanvas == null)
            {
                _metaCanvas = new OverlayCanvas("MetaToastCanvas", transform, MetaToastSortingOrder);
                _skinToast = new Toast("SkinToast", _metaCanvas.Root, _art, MetaToastSp);
            }

            _metaCanvas.Measure();
            string text = UiText.SkinEquippedToast(name);
            _skinToast!.Show(text, Mathf.Max(2500f, NoticeTiming.ReadMs(text)));
            PlaceSkinToast();
        }

        private void PlaceSkinToast()
        {
            OverlayCanvas canvas = _metaCanvas!;
            GameScreen.ReadScreen(out int sw, out int sh, out Rect safe);
            float scale = sw > 0 ? sw / Mathf.Max(1f, canvas.Width) : 1f;
            float safeTop = Mathf.Max(GameScreen.MinSafeTop, sh > 0 ? (sh - safe.yMax) / scale : 0f);
            float w = Mathf.Min(Toast.FullWidthDp(canvas.Width), Mathf.Max(160f, _skinToast!.FitWidthDp(canvas.Width)));
            float h = Mathf.Max(_skinToast.MinHeightDp(canvas.Width), 44f);
            _skinToast.PlaceAt(Mathf.Round((canvas.Width - w) * 0.5f), safeTop + MetaToastTopDp, w, h);
        }

        // La Canasta del nivel 10k con las reglas de B1a (la misma configuración y el mismo desbloqueo que el pago).
        private BasketContents MapBasketFor(int level)
        {
            StreakService? streak = _streakService;
            int unlock = streak?.BasketUnlockLevel ?? _dailyService?.Config.LiveUnlockLevelDaily ?? DailyChallengeConfig.Defaults.LiveUnlockLevelDaily;
            return BasketRules.For(level, unlock, streak?.Basket ?? BasketConfig.Defaults);
        }
    }
}
