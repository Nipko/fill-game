#nullable enable
using System;
using AF.Economy;

namespace AF.GameFlow
{
    /// <summary>
    /// La Crecida, la Hoja escudo y la Canasta en el flujo del nivel (B1a, A6; 06 §4.3-§4.4; 05 §2.1 y §2.4; 04 §2.6, §2.8, §5.2
    /// y §8; nota <c>docs/dev/crecida-canasta.md</c>, enganches E29, E30, E31 y E34). Toda la lógica vive aquí y en
    /// <see cref="StreakService"/>; en los archivos existentes solo hay dos llamadas (parche
    /// <c>docs/dev/traspaso/spark-enganches/S2-flow.diff</c>):
    /// <list type="bullet">
    /// <item><description><c>ResolveWin</c> (E30) lee <c>max_level</c> antes de <c>SaveWin</c> y, tras <see cref="Transaction.Win"/>,
    /// llama a <see cref="OnWinStreak"/>, dentro del mismo lote del perfil (la Crecida y su pago salen en la misma escritura que la
    /// victoria).</description></item>
    /// <item><description><c>Fail</c> (E29, E31) llama a <see cref="OnHardFailStreak"/> tras <see cref="FailResolve.Resolve"/>
    /// (después de <see cref="Transaction.HardFail"/>) y <b>antes</b> de <see cref="ILevelFlowListener.OnFailResolved"/>: la vista
    /// lee <see cref="LastStreakResult"/> (<c>streak_broken</c> y <c>shield_used</c> de <c>af_level_fail</c>, Q-C7; el toast de la
    /// hoja).</description></item>
    /// </list>
    /// El Salto del Boto (E34, B3) aún no existe en el cliente: cuando llegue, llama a <see cref="OnSkipStreak"/> al confirmarse,
    /// <b>dentro del mismo <c>using (Saves())</c> en el que guarda el salto y antes de guardarlo</b>: así la Canasta y el progreso
    /// salen en una sola escritura del perfil y la comparación con <c>max_level</c> ve el valor anterior al salto.
    /// <para>
    /// <b>El Reto del río no cuenta</b> (06 §4.3 «si not nivel.es_campana: return»; C1, Q-C2 (a)): en un flujo de
    /// <see cref="CreateDaily"/> (<see cref="IsDaily"/>) el servicio recibe <c>isCampaign = false</c>, así que ni suma, ni rompe,
    /// ni gasta la hoja, ni paga Canasta ni x2 del río. Sin servicio (<see cref="UseStreak"/> sin llamar o con null: los tests del
    /// flujo, el editor) no hace nada.
    /// </para>
    /// </summary>
    public sealed partial class LevelFlow
    {
        private StreakService? _streak;
        private StreakServiceResult? _lastStreak;

        /// <summary>Servicio de la Crecida y la Canasta (<see cref="UseStreak"/>), o null.</summary>
        public StreakService? Streak => _streak;

        /// <summary>
        /// Resultado de la Crecida del último fin de nivel (victoria, fallo duro o Salto del Boto) de este flujo, o null si no hubo
        /// servicio o aún no pasó nada. Se lee en <see cref="ILevelFlowListener.OnWin"/> y
        /// <see cref="ILevelFlowListener.OnFailResolved"/> (hito pagado, Canasta abierta, hoja gastada, racha rota) y lo usan la
        /// vista, las sondas y TiTino (E33). Un fallo blando lo deja en null.
        /// </summary>
        public StreakServiceResult? LastStreakResult => _lastStreak;

        /// <summary>
        /// Punto de inyección (E32): el arranque (<c>GameBootstrap.Streak.cs</c>, A7) crea el <see cref="StreakService"/> sobre el
        /// mismo perfil y la misma cartera de campaña y lo pasa aquí. Null lo quita. Se puede llamar en cualquier momento; vale
        /// desde el siguiente fin de nivel.
        /// </summary>
        public void UseStreak(StreakService? streak)
        {
            _streak = streak;
            _lastStreak = null;
        }

        /// <summary>
        /// Estado de la Crecida para el nivel cargado (barra del HUD, réplica en la victoria; B1b), o null sin servicio o sin nivel.
        /// En el Reto, <see cref="StreakSnapshot.Active"/> es false (sin barra, E25). Lee el almacén: no lo llames en cada fotograma.
        /// </summary>
        public StreakSnapshot? PeekStreak()
        {
            if (_streak == null || LevelNumber < 1)
            {
                return null;
            }

            return _streak.Peek(LevelNumber, !IsDaily);
        }

        /// <summary>
        /// Salto del Boto confirmado en el nivel cargado (E34; K9; 04 §2.6): si es un <c>10k</c> de campaña superado por primera
        /// vez, paga su Canasta de nivel; no toca la Crecida (C11). Contrato para el Salto (B3): llamarlo <b>dentro del mismo
        /// <c>using (Saves())</c> en el que guarda el salto y antes de guardarlo</b>. Dentro, porque así la Canasta y el progreso
        /// salen en una sola escritura del perfil (y este método pide <see cref="ProfileSaveBatch.RequireDurable"/>: es fin de
        /// nivel, T-D); antes, porque compara con el <c>max_level</c> de ahora para no pagar dos veces la Canasta de un nivel ya
        /// superado (K8). Devuelve el resultado (también en <see cref="LastStreakResult"/>), o null sin servicio, sin nivel, en el Reto
        /// (no hay Salto: 04 §2.6 e) o con el nivel ya superado.
        /// </summary>
        public StreakServiceResult? OnSkipStreak()
        {
            _lastStreak = null;
            if (_streak == null || LevelNumber < 1 || IsDaily || LevelNumber <= _progress.LoadMaxLevel())
            {
                return null;
            }

            using (Saves())
            {
                SaveBatch?.RequireDurable();
                _lastStreak = _streak.ApplySkip(LevelNumber, true);
            }

            return _lastStreak;
        }

        // E30: en ResolveWin, tras Transaction.Win y dentro del lote. maxLevelBefore = max_level leído antes de SaveWin (la
        // Canasta solo se paga la primera vez que se supera el 10k: reintentar tras un fallo y ganar paga una vez, y rejugar un
        // nivel ya superado no paga). Con el x2 del río crecido aplicado (Q-C1 (a)), la Cosecha queda doblada: HarvestSeeds lo
        // incluye y DoubleHarvestOffered es false (Q-C9 (a): ni botón de vídeo ni gasto de ads_rv_cap_double_harvest).
        private void OnWinStreak(int maxLevelBefore)
        {
            _lastStreak = null;
            if (_streak == null)
            {
                return;
            }

            StreakServiceResult result = _streak.ApplyWin(LevelNumber, !IsDaily, maxLevelBefore, _harvestTier);
            _lastStreak = result;
            if (result.Has(StreakServiceEvents.FreeDoubleHarvest) && _harvestBase > 0)
            {
                _harvestDoubled = true;
                _harvest += result.DoubleHarvestSeeds;
            }
        }

        /// <summary>
        /// Tarjeta de la Crecida (04 §4: «Tarjeta de la Crecida al cargar el 25»; tarea B1b) para el nivel cargado, o null: solo en
        /// la campaña, con el servicio, con <c>biz_feature_win_streak</c> encendido, en el nivel de <c>streak_unlock_level</c> y en
        /// la entrada al nivel (no al reintentar, como las tarjetas de estreno: <see cref="LevelCardOnRetry"/> no la saca). La pide
        /// <c>TryShowLevelCard</c> solo si el nivel no tiene tarjeta propia (etiqueta o estreno), que gana [P].
        /// </summary>
        private LevelCardInfo? StreakCardFor(bool isShort)
        {
            StreakService? streak = _streak;
            if (isShort || streak == null || IsDaily || !streak.Config.BizFeatureWinStreak || _levelNumber != streak.Config.StreakUnlockLevel)
            {
                return null;
            }

            return LevelCardInfo.ForStreak(_levelNumber);
        }

        // Al cargar un nivel (ResetHarvest, desde Load; parche S2): el resultado del nivel anterior ya no vale.
        private void ResetStreakResult()
        {
            _lastStreak = null;
        }

        // E29/E31: en Fail, tras FailResolve.Resolve (Transaction.HardFail ya aplicado) y antes de avisar a la vista. Solo el
        // fallo duro toca la Crecida (C9-C11); el blando deja LastStreakResult en null.
        private void OnHardFailStreak(FailKind kind)
        {
            _lastStreak = null;
            if (_streak == null || kind != FailKind.Hard)
            {
                return;
            }

            _lastStreak = _streak.ApplyHardFail(LevelNumber, !IsDaily);
        }
    }
}
