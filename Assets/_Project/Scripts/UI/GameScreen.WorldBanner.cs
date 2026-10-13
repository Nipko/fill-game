#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.GameFlow;
using AF.Presentation;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial del banner de entrada a un mundo (07 §2.2, regla 1; 02 §6; glosario §10, <c>world_banner</c>; decisión del dueño
    /// del 2026-10-11) sin editar GameScreen.cs: implementa <see cref="IWorldBannerListener"/> y pinta el banner con
    /// <see cref="WorldBannerView"/>. Lo llaman los ganchos <c>Cards*</c> de GameScreen.Cards.cs (los métodos parciales solo
    /// admiten una implementación).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo</b>: el banner es LEVEL_CARD con <see cref="LevelFlow.WorldBanner"/> (LevelFlow.Cards.cs): llega tras
    /// <c>OnLevelLoaded</c> y los regalos, la intro se retiene en t = 0 detrás de él como detrás de la tarjeta previa, y 1 toque
    /// llama a <see cref="LevelFlow.DismissWorldBanner"/>, que empieza su salida: el flujo sigue en LEVEL_CARD
    /// (<see cref="LevelFlow.WorldBannerClosing"/>) los <see cref="WorldBannerView.ExitDurationMs"/> que esta parcial le fija al
    /// abrirlo (<see cref="LevelFlow.WorldBannerExitMs"/>), y solo después abre la tarjeta previa si el nivel la tiene o arranca
    /// la intro: «Llenar y teñir» nunca empieza debajo del banner (07 §5.1). Un segundo toque durante la salida la termina en el
    /// acto. Al ser LEVEL_CARD, los avisos esperan a que termine (la Luciérnaga regalada, <c>ShowRevealGiftIfReady</c>), la
    /// pausa queda inerte bajo el velo y nada de debajo recibe el toque.</para>
    /// <para><b>Arte</b>: se pinta fuera del hilo principal (<see cref="WorldBannerView.AsyncArt"/>; 08 §5.4) y el banner entra
    /// cuando llega; un toque antes de que llegue lo salta en el acto, sin salida (no hay nada a la vista). Los tests que miden
    /// instantes exactos lo piden en el acto con <see cref="LaunchOptions.BannerArtSyncOverride"/>.</para>
    /// <para>En desarrollo (los documenta docs/dev/prototipo-v0.md): al abrirse, <c>AF_STATE {"event":"world_banner",…}</c>
    /// con <c>level</c>, <c>world</c>, <c>text</c> (la clave entera), <c>then</c> (<c>"level_card"</c> si al cerrarlo sale la
    /// tarjeta previa, o <c>"intro"</c>) y <c>reduce_motion</c> (la versión quieta); cuando llega su arte y empieza a entrar,
    /// <c>AF_STATE {"event":"world_banner_art",…}</c> con <c>level</c>, <c>world</c>, <c>async</c>, <c>wait_ms</c> (lo que
    /// esperó), <c>mascots_ms</c> (lo que costó pintarlo) y <c>main_ms</c> (lo que ocupó el hilo principal); al tocarlo,
    /// <c>AF_STATE {"event":"world_banner_closed",…}</c> con <c>level</c>, <c>world</c> y <c>next</c>, con los mismos
    /// valores que <c>then</c> y antes de que se abra lo que sigue (la línea <c>level_card</c>, si la hay, sale después); y, al
    /// terminar la entrada, <c>AF_LAYOUT {"screen":"world_banner",…}</c> con el id <c>world_banner</c> y el rectángulo de la
    /// tarjeta (cualquier toque la cierra).</para>
    /// </remarks>
    public sealed partial class GameScreen : IWorldBannerListener
    {
        /// <summary>Lo que sigue al banner (<c>then</c> y <c>next</c> de la sonda): la tarjeta previa.</summary>
        internal const string BannerNextLevelCard = "level_card";

        /// <summary>Lo que sigue al banner (<c>then</c> y <c>next</c> de la sonda): la intro.</summary>
        internal const string BannerNextIntro = "intro";

        private WorldBannerView _worldBanner = null!;
        private string? _lastBannerProbe;
        private string? _lastBannerClosedProbe;
        private string? _lastBannerArtProbe;
        private int _worldBannerShown;

        /// <summary>Banner de entrada a un mundo (tests y capturas).</summary>
        internal WorldBannerView WorldBannerView => _worldBanner;

        /// <summary>Campos de la última línea <c>AF_STATE world_banner</c> (tests; null si aún no hubo banner o sin sonda).</summary>
        internal string? LastWorldBannerProbe => _lastBannerProbe;

        /// <summary>
        /// Campos de la última línea <c>AF_STATE world_banner_closed</c> (tests; null si aún no se cerró ninguno o sin sonda).
        /// </summary>
        internal string? LastWorldBannerClosedProbe => _lastBannerClosedProbe;

        /// <summary>
        /// Campos de la última línea <c>AF_STATE world_banner_art</c> (tests; null si aún no llegó el arte de ninguno o sin sonda).
        /// </summary>
        internal string? LastWorldBannerArtProbe => _lastBannerArtProbe;

        /// <summary>Banners abiertos desde que existe la pantalla (tests: no sale al reintentar ni al reanudar).</summary>
        internal int WorldBannersShown => _worldBannerShown;

        /// <inheritdoc />
        public void OnWorldBanner(int world, int level)
        {
            LevelFlow flow = RequireFlow();
            HoldIntroForCard(flow); // el tablero espera detrás en el fotograma 0 de la intro, como con la tarjeta previa

            // La tarjeta previa o la intro, solo cuando el banner haya salido del todo (07 §5.1: antes de la intro, no encima).
            flow.WorldBannerExitMs = Mathf.CeilToInt(WorldBannerView.ExitDurationMs(flow.ReduceMotion));
            Color water = Palette.WaterBase(flow.Level.WaterBase);
            _worldBanner.AsyncArt = LaunchOptions.BannerArtSyncOverride != true; // el arte, fuera del fotograma de la carga
            _worldBanner.Show(world, water, flow.ReduceMotion, _canvasW, _canvasH, _safeTop, _safeBottom);
            _worldBanner.Root.SetAsLastSibling(); // por encima de todo: el toque en cualquier punto es suyo
            _worldBannerShown++;
            _lastBannerProbe = DevProbe.Enabled ? BannerProbeJson(world, level) : null;
            if (_lastBannerProbe != null)
            {
                _probe.Event("world_banner", _lastBannerProbe);
            }

            if (_worldBanner.ConsumeArtApplied())
            {
                EmitBannerArt(flow); // en el acto: el arte ya estaba o se pidió sin hilo
            }
        }

        private void BannerBuild()
        {
            _worldBanner = new WorldBannerView(_root, _art, OnWorldBannerTapped);
        }

        private void BannerLayout()
        {
            if (_worldBanner.Visible)
            {
                _worldBanner.Layout(_canvasW, _canvasH, _safeTop, _safeBottom);
            }
        }

        private void BannerTick(float dtMs)
        {
            if (!_worldBanner.Visible)
            {
                return;
            }

            LevelFlow? flow = _flow;
            bool inBanner = flow != null && flow.HasLevel && flow.Phase == FlowPhase.LevelCard && flow.WorldBanner != 0;
            if (!inBanner)
            {
                // El flujo ya siguió (terminó la salida, un segundo toque la cortó): la tarjeta previa o la intro ya están en
                // marcha, así que el banner se quita en el acto y nunca queda encima de ellas.
                if (!_worldBanner.Dismissing && flow != null && flow.HasLevel)
                {
                    EmitBannerClosed(flow);
                }

                _worldBanner.HideNow();
                return;
            }

            if (flow!.WorldBannerClosing && !_worldBanner.Dismissing)
            {
                StartBannerExit(flow); // el flujo lo cerró por otro camino (TapAnywhere, TapCuia, el fondo…): su salida
                if (!_worldBanner.Visible)
                {
                    return;
                }
            }

            _worldBanner.Tick(dtMs);
            if (_worldBanner.ConsumeArtApplied())
            {
                EmitBannerArt(flow);
            }

            if (_worldBanner.ConsumeEntered())
            {
                EmitBannerLayout();
            }
        }

        private void BannerLevelLoaded()
        {
            _worldBanner.HideNow(); // otro nivel (o el mismo reanudado): fuera el banner anterior
        }

        private void BannerLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (_worldBanner.Visible && !_worldBanner.Dismissing)
            {
                ids.Add("world_banner");
                rects.Add(_worldBanner.Panel);
            }
        }

        // 1 toque en cualquier punto: el flujo empieza la salida (la tarjeta previa o la intro esperan a que termine) y el banner
        // sale. Un segundo toque durante la salida la termina en el acto.
        private void OnWorldBannerTapped()
        {
            if (!_worldBanner.Visible)
            {
                return;
            }

            LevelFlow flow = RequireFlow();
            if (_worldBanner.Dismissing)
            {
                flow.DismissWorldBanner(); // con el banner saliendo, sigue ya con la tarjeta previa o la intro
                _worldBanner.HideNow();
                return;
            }

            StartBannerExit(flow);
        }

        // La salida del banner: la línea world_banner_closed antes de que el flujo siga (con lo que vendrá detrás), el flujo
        // empieza la salida (si no la empezó ya por otro camino) y el banner sale. Si su arte aún no había llegado, no hay nada a
        // la vista que ver salir: sin salida (WorldBannerExitMs = 0), así que el flujo sigue en el acto con el toque o, si lo
        // cerró por otro camino, en su propio Tick de este fotograma (la vista no lo hace avanzar desde su Tick), y el banner
        // se quita.
        private void StartBannerExit(LevelFlow flow)
        {
            EmitBannerClosed(flow);
            bool art = _worldBanner.ArtReady;
            if (!art)
            {
                flow.WorldBannerExitMs = 0;
            }

            if (!flow.WorldBannerClosing)
            {
                flow.DismissWorldBanner();
            }

            if (!art)
            {
                _worldBanner.HideNow();
                return;
            }

            _worldBanner.Dismiss();
        }

        // AF_STATE world_banner_art: cuándo llegó el arte del banner y lo que costó (08 §5.4; ProceduralArt.RequestBannerArt).
        private void EmitBannerArt(LevelFlow flow)
        {
            if (!DevProbe.Enabled || !flow.HasLevel)
            {
                return;
            }

            ProceduralArt art = _art;
            var sb = new StringBuilder(160);
            sb.Append("{\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"world\":").Append(_worldBanner.World.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"async\":").Append(_worldBanner.AsyncArt ? "true" : "false");
            sb.Append(",\"wait_ms\":").Append(_worldBanner.ArtWaitMs.ToString("0", CultureInfo.InvariantCulture));
            sb.Append(",\"mascots_ms\":").Append(art.MascotsMs.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(",\"main_ms\":").Append(art.MascotsMainMs.ToString("0.0", CultureInfo.InvariantCulture)).Append('}');
            _lastBannerArtProbe = sb.ToString();
            _probe.Event("world_banner_art", _lastBannerArtProbe);
        }

        // AF_STATE world_banner_closed: nivel, mundo y lo que sigue, con los valores de "then" (BannerNext).
        private void EmitBannerClosed(LevelFlow flow)
        {
            if (!DevProbe.Enabled || !flow.HasLevel)
            {
                return;
            }

            _lastBannerClosedProbe = "{\"level\":" + flow.LevelNumber.ToString(CultureInfo.InvariantCulture)
                + ",\"world\":" + _worldBanner.World.ToString(CultureInfo.InvariantCulture)
                + ",\"next\":\"" + BannerNext(flow) + "\"}";
            _probe.Event("world_banner_closed", _lastBannerClosedProbe);
        }

        // Lo que sigue al banner, con un solo vocabulario para "then" y "next": la tarjeta previa ("level_card") o la intro
        // ("intro"). Con el banner abierto o saliendo, lo que saldrá al terminar (LevelFlow.WorldBannerThenCard); ya cerrado, lo
        // que está en marcha.
        private static string BannerNext(LevelFlow flow)
        {
            bool card = flow.WorldBanner != 0 ? flow.WorldBannerThenCard : flow.LevelCard != null;
            return card ? BannerNextLevelCard : BannerNextIntro;
        }

        private void EmitBannerLayout()
        {
            LevelFlow? flow = _flow;
            if (!DevProbe.Enabled || flow == null || !flow.HasLevel)
            {
                return;
            }

            _layoutIds.Clear();
            _layoutRects.Clear();
            _layoutIds.Add("world_banner");
            _layoutRects.Add(_worldBanner.Panel);
            _probe.Layout("world_banner", flow.LevelNumber, null, _layoutIds.ToArray(), _layoutRects.ToArray());
        }

        // AF_STATE world_banner: nivel, mundo, texto (world_banner del glosario §10), lo que sale al cerrarlo (la tarjeta previa o
        // la intro: BannerNext) y si es la versión quieta. Lo que costó su arte va aparte, cuando llega (world_banner_art).
        private string BannerProbeJson(int world, int level)
        {
            LevelFlow flow = RequireFlow();
            var sb = new StringBuilder(176);
            sb.Append("{\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"world\":").Append(world.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"text\":\"").Append(UiText.WorldBanner(world)).Append('"');
            sb.Append(",\"then\":\"").Append(BannerNext(flow)).Append('"');
            sb.Append(",\"reduce_motion\":").Append(flow.ReduceMotion ? "true" : "false").Append('}');
            return sb.ToString();
        }
    }
}
