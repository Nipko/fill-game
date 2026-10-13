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
    /// llama a <see cref="LevelFlow.DismissWorldBanner"/>, que abre la tarjeta previa si el nivel la tiene o arranca la intro.
    /// Al ser LEVEL_CARD, los avisos esperan a que termine (la Luciérnaga regalada, <c>ShowRevealGiftIfReady</c>), la pausa queda
    /// inerte bajo el velo y nada de debajo recibe el toque.</para>
    /// <para>En desarrollo, al abrirse, <c>AF_STATE {"event":"world_banner",…}</c> con <c>level</c>, <c>world</c>,
    /// <c>text</c> (la clave entera), <c>then</c> (<c>"level_card"</c> si al cerrarlo sale la tarjeta previa, o
    /// <c>"intro"</c>) y <c>reduce_motion</c> (la versión quieta); al cerrarse, <c>AF_STATE
    /// {"event":"world_banner_closed",…}</c> con <c>level</c>, <c>world</c> y <c>next</c> (la tarjeta o la fase que
    /// sigue); y, al terminar la entrada, <c>AF_LAYOUT {"screen":"world_banner",…}</c> con el id <c>world_banner</c> y el
    /// rectángulo de la tarjeta (cualquier toque la cierra). Pendiente de la sesión principal al integrar (B3 toca esos
    /// archivos en paralelo): anotarlos en DevProbe.cs y en docs/dev/prototipo-v0.md.</para>
    /// </remarks>
    public sealed partial class GameScreen : IWorldBannerListener
    {
        private WorldBannerView _worldBanner = null!;
        private string? _lastBannerProbe;
        private int _worldBannerShown;

        /// <summary>Banner de entrada a un mundo (tests y capturas).</summary>
        internal WorldBannerView WorldBannerView => _worldBanner;

        /// <summary>Campos de la última línea <c>AF_STATE world_banner</c> (tests; null si aún no hubo banner o sin sonda).</summary>
        internal string? LastWorldBannerProbe => _lastBannerProbe;

        /// <summary>Banners abiertos desde que existe la pantalla (tests: no sale al reintentar ni al reanudar).</summary>
        internal int WorldBannersShown => _worldBannerShown;

        /// <inheritdoc />
        public void OnWorldBanner(int world, int level)
        {
            LevelFlow flow = RequireFlow();
            HoldIntroForCard(flow); // el tablero espera detrás en el fotograma 0 de la intro, como con la tarjeta previa
            Color water = Palette.WaterBase(flow.Level.WaterBase);
            _worldBanner.Show(world, water, flow.ReduceMotion, _canvasW, _canvasH, _safeTop, _safeBottom);
            _worldBanner.Root.SetAsLastSibling(); // por encima de todo: el toque en cualquier punto es suyo
            _worldBannerShown++;
            _lastBannerProbe = DevProbe.Enabled ? BannerProbeJson(world, level) : null;
            if (_lastBannerProbe != null)
            {
                _probe.Event("world_banner", _lastBannerProbe);
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
            bool open = flow != null && flow.HasLevel && flow.Phase == FlowPhase.LevelCard && flow.WorldBanner != 0;
            if (!_worldBanner.Dismissing && !open)
            {
                CloseBanner(flow); // el flujo lo cerró por otro camino (TapAnywhere, TapCuia, el fondo…)
            }

            _worldBanner.Tick(dtMs);
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

        // 1 toque en cualquier punto: directo al flujo, que abre la tarjeta previa o arranca la intro; el banner sale sin bloquear
        // el toque siguiente.
        private void OnWorldBannerTapped()
        {
            if (!_worldBanner.Visible || _worldBanner.Dismissing)
            {
                return;
            }

            LevelFlow flow = RequireFlow();
            flow.DismissWorldBanner();
            CloseBanner(flow);
        }

        private void CloseBanner(LevelFlow? flow)
        {
            int world = _worldBanner.World;
            _worldBanner.Dismiss();
            if (DevProbe.Enabled && flow != null && flow.HasLevel)
            {
                _probe.Event("world_banner_closed", "{\"level\":" + flow.LevelNumber.ToString(CultureInfo.InvariantCulture)
                    + ",\"world\":" + world.ToString(CultureInfo.InvariantCulture)
                    + ",\"next\":\"" + (flow.LevelCard != null ? "level_card" : FlowPhaseNames.Of(flow.Phase)) + "\"}");
            }
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
        // la intro: LevelFlow.WorldBannerThenCard) y si es la versión quieta.
        private string BannerProbeJson(int world, int level)
        {
            LevelFlow flow = RequireFlow();
            var sb = new StringBuilder(176);
            sb.Append("{\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"world\":").Append(world.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"text\":\"").Append(UiText.WorldBanner(world)).Append('"');
            sb.Append(",\"then\":\"").Append(flow.WorldBannerThenCard ? "level_card" : "intro").Append('"');
            sb.Append(",\"reduce_motion\":").Append(flow.ReduceMotion ? "true" : "false").Append('}');
            return sb.ToString();
        }
    }
}
