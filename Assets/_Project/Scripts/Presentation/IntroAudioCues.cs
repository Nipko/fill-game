#nullable enable
using AF.Audio;

namespace AF.Presentation
{
    /// <summary>
    /// Sonidos de «Llenar y teñir» (07 §9.2 con D-010; 02 §4.2; la lista, §1; tarea AU1), sincronizados con el plan de la intro
    /// (<see cref="IntroChoreography"/>): <c>sfx_river_rise</c> al empezar, un <c>sfx_fill</c> por fila al empezar a llenarse su
    /// primera cuia, un <c>sfx_fruit_plip</c> en el impacto de cada fruta, un <c>sfx_tint</c> afinado a la fruta en la floración
    /// de cada racha (el servicio deja como mucho 6 a la vez) y, como mucho, un chillido de miquito al entrar la tropa, nunca
    /// mientras se llenan las cuias. Con «Reducir movimiento» (fundido de 0,25 s, sin fruta que caiga) no suena nada. Saltar la
    /// intro calla los que faltaban. Sin asignaciones: banderas en arrays fijos.
    /// </summary>
    public sealed class IntroAudioCues
    {
        private readonly bool[] _fruitDone = new bool[IntroChoreography.MaxFruits];
        private readonly bool[] _runDone = new bool[IntroChoreography.MaxRuns];
        private readonly bool[] _rowDone = new bool[IntroInput.MaxRows];
        private readonly float[] _rowFillMs = new float[IntroInput.MaxRows];
        private IntroChoreography? _plan;
        private bool _riverDone;
        private bool _chirpDone;
        private float _chirpMs;

        /// <summary>Hay sonidos pendientes de esta intro.</summary>
        public bool Active => _plan != null;

        /// <summary>Prepara los sonidos de la intro que empieza con <paramref name="plan"/>.</summary>
        public void Begin(IntroChoreography plan, IntroInput input, bool reduceMotion)
        {
            Stop();
            if (reduceMotion)
            {
                return;
            }

            _plan = plan;
            _riverDone = false;
            for (int k = 0; k < _fruitDone.Length; k++)
            {
                _fruitDone[k] = false;
            }

            for (int r = 0; r < _runDone.Length; r++)
            {
                _runDone[r] = false;
            }

            // Cada fila suena cuando empieza a llenarse su primera cuia.
            float firstFill = float.MaxValue;
            float lastFill = float.MinValue;
            for (int r = 0; r < _rowDone.Length; r++)
            {
                _rowDone[r] = true;
                _rowFillMs[r] = float.MaxValue;
            }

            for (int i = 0; i < plan.CuiaCount && i < input.Count; i++)
            {
                float fill = plan.FillStartMs[i];
                if (plan.PassMs[i] < 0f)
                {
                    continue; // cuia que queda seca
                }

                int row = input.Row[i];
                if ((uint)row < (uint)_rowFillMs.Length)
                {
                    _rowDone[row] = false;
                    if (fill < _rowFillMs[row])
                    {
                        _rowFillMs[row] = fill;
                    }
                }

                if (fill < firstFill)
                {
                    firstFill = fill;
                }

                if (plan.FillEndMs[i] > lastFill)
                {
                    lastFill = plan.FillEndMs[i];
                }
            }

            // Un chillido como mucho, al entrar la tropa, y solo si cae fuera del llenado (la lista, §1).
            _chirpDone = true;
            if (plan.MonkeyCount > 0)
            {
                float enter = plan.MonkeyEnterStartMs[0];
                for (int m = 1; m < plan.MonkeyCount; m++)
                {
                    if (plan.MonkeyEnterStartMs[m] < enter)
                    {
                        enter = plan.MonkeyEnterStartMs[m];
                    }
                }

                bool duringFill = firstFill <= lastFill && enter >= firstFill && enter <= lastFill;
                if (!duringFill)
                {
                    _chirpMs = enter;
                    _chirpDone = false;
                }
            }
        }

        /// <summary>Llega la intro a <paramref name="timeMs"/>: suena lo que toca hasta ahí.</summary>
        public void Tick(float timeMs)
        {
            IntroChoreography? plan = _plan;
            if (plan == null)
            {
                return;
            }

            if (!_riverDone)
            {
                _riverDone = true;
                AudioCues.Play(SfxId.RiverRise);
            }

            for (int r = 0; r < _rowDone.Length; r++)
            {
                if (!_rowDone[r] && timeMs >= _rowFillMs[r])
                {
                    _rowDone[r] = true;
                    AudioCues.Play(SfxId.Fill);
                }
            }

            if (!_chirpDone && timeMs >= _chirpMs)
            {
                _chirpDone = true;
                AudioCues.Play(SfxId.MonkeyChirp);
            }

            for (int k = 0; k < plan.FruitCount && k < _fruitDone.Length; k++)
            {
                if (!_fruitDone[k] && timeMs >= plan.FruitImpactMs[k])
                {
                    _fruitDone[k] = true;
                    AudioCues.Play(SfxId.FruitPlip);
                }
            }

            for (int run = 0; run < plan.RunCount && run < _runDone.Length; run++)
            {
                // Solo las rachas que florecen (las ocultas y el Barro no forman racha; −1: sin floración).
                if (!_runDone[run] && plan.RunBloomMs[run] >= 0f && timeMs >= plan.RunBloomMs[run])
                {
                    _runDone[run] = true;
                    AudioCues.PlayFruit(SfxId.Tint, plan.RunTint[run]);
                }
            }
        }

        /// <summary>Se acabó la intro (o se saltó): lo que faltaba ya no suena.</summary>
        public void Stop()
        {
            _plan = null;
        }
    }
}
