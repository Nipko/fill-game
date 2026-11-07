#nullable enable
using AF.Presentation;
using UnityEngine;

namespace AF.UI
{
    /// <summary>Gestos de Capi al remo (D-069, M2-3; propuesta del Mapa v2 §5.2). Los de un solo golpe duran <see cref="CapiDirector.DurationMs"/>.</summary>
    public enum CapiGesture
    {
        /// <summary>Quieto, con el remo en la mano (sin viaje).</summary>
        Rest = 0,

        /// <summary><c>capi_paddle</c>: rema (bucle de <see cref="CapiDirector.PaddleLoopMs"/> ms, «12 f»).</summary>
        Paddle = 1,

        /// <summary><c>capi_lookout</c>: la pata de visera (la anticipación de D-053; una boca).</summary>
        Lookout = 2,

        /// <summary><c>capi_wave</c>: saluda (un pueblo, la bandera; al abrir el Mapa sin viaje, una vez por sesión).</summary>
        Wave = 3,

        /// <summary><c>capi_paddle_hard</c>: rema fuerte, con salpicaduras (los rápidos).</summary>
        PaddleHard = 4,

        /// <summary><c>capi_cheer_oar</c>: el remo en alto (llegada a una parada).</summary>
        CheerOar = 5,

        /// <summary><c>capi_tie</c>: sube la mini cuia nueva y la ata a popa (tras la llegada).</summary>
        Tie = 6,

        /// <summary>Se asoma por la borda a ver el agua nueva (el Encuentro, un cambio de agua) [P].</summary>
        LeanOver = 7,
    }

    /// <summary>Emociones sin texto en la burbuja (D-069, P3 (a); propuesta §5.3). Nada que traducir.</summary>
    public enum CapiEmotion
    {
        /// <summary>Sin burbuja.</summary>
        None = 0,

        /// <summary>♪ remando.</summary>
        Note = 1,

        /// <summary>♥ al unirse alguien (M2-4).</summary>
        Heart = 2,

        /// <summary>! en el Encuentro, al cambiar el agua o en una boca.</summary>
        Bang = 3,

        /// <summary>? ante la niebla del fin de contenido.</summary>
        Question = 4,

        /// <summary>Una cuia: se llena la muestra.</summary>
        Sample = 5,
    }

    /// <summary>Lo que le pasa a Capi en el Mapa (propuesta §5.4), en el orden en que se cruza en el viaje.</summary>
    public enum CapiMoment
    {
        /// <summary>Se abre el Mapa sin viaje (una vez por sesión).</summary>
        OpenIdle = 0,

        /// <summary>La anticipación de D-053 (los 250 ms antes de salir).</summary>
        Lead = 1,

        /// <summary>La canoa sale.</summary>
        Depart = 2,

        /// <summary>Pasa un pueblo de paso: saluda y el pueblo enciende una luz.</summary>
        TownPass = 3,

        /// <summary>Pasa la boca de un afluente: mira el agua nueva que entra (!).</summary>
        Mouth = 4,

        /// <summary>Pasa unos rápidos: rema fuerte, con salpicaduras.</summary>
        Rapids = 5,

        /// <summary>Llega a una parada: el remo en alto y amarra la muestra.</summary>
        Arrival = 6,

        /// <summary>La parada tiene otra agua (el Encuentro de las Aguas): se asoma por la borda (!).</summary>
        WaterChange = 7,

        /// <summary>La parada iza una bandera nueva: la saluda.</summary>
        CountryChange = 8,

        /// <summary>La parada abre un mundo nuevo: la luz cambia con su fundido (!).</summary>
        NewWorld = 9,

        /// <summary>Se une alguien a la canoa (M2-4): ♥.</summary>
        Join = 10,

        /// <summary>El viaje del fin de contenido llega a la niebla: ?.</summary>
        ContentEnd = 11,
    }

    /// <summary>Postura del rig en un instante (lo que <see cref="CapiRigView"/> aplica a sus piezas). Ángulos en grados, distancias en dp de diseño.</summary>
    public struct CapiPose
    {
        /// <summary>Inclinación de Capi sobre la grupa (positivo hacia popa).</summary>
        public float BodyTilt;

        /// <summary>Lo que sube Capi en su asiento.</summary>
        public float BodyLift;

        /// <summary>Ángulo de la pata desde el hombro (0 hacia la proa, 90 arriba).</summary>
        public float Arm;

        /// <summary>Ángulo del remo cuando lo lleva la mano (0 vertical, positivo con la pala hacia popa).</summary>
        public float Paddle;

        /// <summary>El remo va en la mano (si no, descansa en la canoa).</summary>
        public bool PaddleHeld;

        /// <summary>Recorrido de la mini cuia que se amarra, de la proa (0) a la popa (1); negativo, no está.</summary>
        public float Sample;

        /// <summary>Opacidad de las salpicaduras (0-1).</summary>
        public float Splash;
    }

    /// <summary>
    /// Elige y encadena los gestos y la emoción de Capi en el Mapa v2 (D-069, M2-3; propuesta §5.2-§5.4 y §7.3), sin Unity: lo usa
    /// <see cref="RiverMapScreen"/> y lo prueban los tests de EditMode.
    /// <list type="bullet">
    /// <item><description><b>Reacciones</b> (<see cref="React"/>): cada momento pone en cola sus gestos de un golpe
    /// (<see cref="GesturesFor"/>); por debajo, el bucle de remar si la canoa viaja (<see cref="Traveling"/>) o quieto.</description></item>
    /// <item><description><b>Una emoción como mucho por viaje</b> (§5.3): <see cref="PickEmotion"/> elige la del viaje entre las que
    /// trae (? &gt; ♥ &gt; ! &gt; cuia &gt; ♪) y sale en el primer momento que la lleva, <see cref="EmotionMs"/> ms.</description></item>
    /// <item><description><b>Reducir movimiento</b> (§7.3): sin bucles ni recorridos; el gesto se ve en su postura clave y quieto
    /// <see cref="StillMs"/> ms, solo el primero de cada reacción, y la emoción también, quieta.</description></item>
    /// </list>
    /// Sin asignaciones tras construirse.
    /// </summary>
    public sealed class CapiDirector
    {
        /// <summary>Periodo del bucle de remar (ms) [P]: «12 f» a 20 fotogramas por segundo.</summary>
        public const float PaddleLoopMs = 600f;

        /// <summary>Periodo del remo fuerte (ms) [P].</summary>
        public const float PaddleHardLoopMs = 300f;

        /// <summary>Lo que dura la emoción en la burbuja (ms; propuesta §5.3).</summary>
        public const float EmotionMs = 1200f;

        /// <summary>Lo que se ve quieto un gesto o una emoción con Reducir movimiento (ms; propuesta §7.3).</summary>
        public const float StillMs = 1200f;

        /// <summary>Entrada de la burbuja (ms) [P].</summary>
        public const float BubbleInMs = 150f;

        /// <summary>Salida de la burbuja (ms) [P].</summary>
        public const float BubbleOutMs = 200f;

        /// <summary>Gestos en cola como mucho.</summary>
        public const int QueueSize = 8;

        private readonly CapiGesture[] _queue = new CapiGesture[QueueSize];
        private readonly CapiGesture[] _scratch = new CapiGesture[3];
        private int _queueHead;
        private int _queueCount;
        private CapiGesture _current = CapiGesture.Rest;
        private float _currentMs;
        private float _loopMs;
        private CapiEmotion _planned;
        private bool _emotionShown;
        private CapiEmotion _emotion;
        private float _emotionMs = -1f;

        /// <summary>La canoa viaja: por debajo de los gestos de un golpe, el bucle de remar.</summary>
        public bool Traveling { get; set; }

        /// <summary>Reducir movimiento (07 §7.8; propuesta §7.3).</summary>
        public bool Still { get; private set; }

        /// <summary>El gesto que se ve.</summary>
        public CapiGesture Current => _current;

        /// <summary>Avance del gesto de un golpe (0-1); en el bucle, su fase.</summary>
        public float Phase
        {
            get
            {
                if (IsLoop(_current))
                {
                    float period = _current == CapiGesture.PaddleHard ? PaddleHardLoopMs : PaddleLoopMs;
                    return Still ? 0.25f : (_loopMs % period) / period;
                }

                if (Still)
                {
                    return KeyPhase(_current);
                }

                float d = DurationMs(_current);
                return d > 0f ? Mathf.Clamp01(_currentMs / d) : 0f;
            }
        }

        /// <summary>La emoción de la burbuja (None sin burbuja).</summary>
        public CapiEmotion Emotion => _emotionMs >= 0f ? _emotion : CapiEmotion.None;

        /// <summary>Milisegundos que lleva la burbuja a la vista (−1 sin burbuja).</summary>
        public float EmotionElapsedMs => _emotionMs;

        /// <summary>La emoción elegida para este viaje (None si ninguna).</summary>
        public CapiEmotion PlannedEmotion => _planned;

        /// <summary>Gestos que esperan en la cola.</summary>
        public int Queued => _queueCount;

        /// <summary>Hay algo que pintar a 60 fps: un gesto de un golpe, el bucle o la burbuja.</summary>
        public bool Animating => (_current != CapiGesture.Rest) || _emotionMs >= 0f || _queueCount > 0;

        /// <summary>Lo que dura un gesto de un golpe (ms; propuesta §5.2); el remo fuerte, tres golpes [P]. 0 en los bucles.</summary>
        public static float DurationMs(CapiGesture g)
        {
            switch (g)
            {
                case CapiGesture.Lookout:
                    return 250f;
                case CapiGesture.Wave:
                    return 600f;
                case CapiGesture.PaddleHard:
                    return 3f * PaddleHardLoopMs;
                case CapiGesture.CheerOar:
                    return 500f;
                case CapiGesture.Tie:
                    return 400f;
                case CapiGesture.LeanOver:
                    return 700f; // [P]
                default:
                    return 0f;
            }
        }

        /// <summary>El gesto se repite (remar).</summary>
        public static bool IsLoop(CapiGesture g)
        {
            return g == CapiGesture.Paddle;
        }

        /// <summary>La fase de la postura clave de cada gesto (la que se ve quieta con Reducir movimiento) [P].</summary>
        public static float KeyPhase(CapiGesture g)
        {
            switch (g)
            {
                case CapiGesture.Lookout:
                    return 1f;
                case CapiGesture.Tie:
                    return 0.6f;
                case CapiGesture.PaddleHard:
                    return 0.25f;
                default:
                    return 0.5f;
            }
        }

        /// <summary>
        /// Los gestos de un golpe de <paramref name="moment"/> (propuesta §5.4), en <paramref name="into"/> (≥ 3); devuelve cuántos.
        /// </summary>
        public static int GesturesFor(CapiMoment moment, CapiGesture[] into)
        {
            switch (moment)
            {
                case CapiMoment.OpenIdle:
                case CapiMoment.TownPass:
                case CapiMoment.CountryChange:
                    into[0] = CapiGesture.Wave;
                    return 1;
                case CapiMoment.Lead:
                case CapiMoment.Mouth:
                case CapiMoment.ContentEnd:
                    into[0] = CapiGesture.Lookout;
                    return 1;
                case CapiMoment.Rapids:
                    into[0] = CapiGesture.PaddleHard;
                    return 1;
                case CapiMoment.Arrival:
                    into[0] = CapiGesture.CheerOar;
                    into[1] = CapiGesture.Tie;
                    return 2;
                case CapiMoment.WaterChange:
                    into[0] = CapiGesture.LeanOver;
                    return 1;
                default:
                    return 0; // Depart, NewWorld y Join: solo la emoción (y la luz o la tripulación)
            }
        }

        /// <summary>La emoción que puede traer <paramref name="moment"/> (propuesta §5.3).</summary>
        public static CapiEmotion EmotionFor(CapiMoment moment)
        {
            switch (moment)
            {
                case CapiMoment.Depart:
                    return CapiEmotion.Note;
                case CapiMoment.Mouth:
                case CapiMoment.WaterChange:
                case CapiMoment.NewWorld:
                    return CapiEmotion.Bang;
                case CapiMoment.Arrival:
                    return CapiEmotion.Sample;
                case CapiMoment.Join:
                    return CapiEmotion.Heart;
                case CapiMoment.ContentEnd:
                    return CapiEmotion.Question;
                default:
                    return CapiEmotion.None;
            }
        }

        /// <summary>Bit de <paramref name="moment"/> para <see cref="PickEmotion"/>.</summary>
        public static int Bit(CapiMoment moment)
        {
            return 1 << (int)moment;
        }

        /// <summary>
        /// La emoción del viaje con los momentos de <paramref name="moments"/> (bits de <see cref="Bit"/>): una como mucho (propuesta
        /// §5.3), con la prioridad ? &gt; ♥ &gt; ! &gt; cuia &gt; ♪ [P]: lo raro manda sobre lo de siempre.
        /// </summary>
        public static CapiEmotion PickEmotion(int moments)
        {
            CapiEmotion best = CapiEmotion.None;
            int bestRank = 0;
            for (int m = 0; m <= (int)CapiMoment.ContentEnd; m++)
            {
                if ((moments & (1 << m)) == 0)
                {
                    continue;
                }

                CapiEmotion e = EmotionFor((CapiMoment)m);
                int rank = Rank(e);
                if (rank > bestRank)
                {
                    best = e;
                    bestRank = rank;
                }
            }

            return best;
        }

        private static int Rank(CapiEmotion e)
        {
            switch (e)
            {
                case CapiEmotion.Question:
                    return 5;
                case CapiEmotion.Heart:
                    return 4;
                case CapiEmotion.Bang:
                    return 3;
                case CapiEmotion.Sample:
                    return 2;
                case CapiEmotion.Note:
                    return 1;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Empieza (al abrir el Mapa): sin cola ni burbuja, quieto, con <paramref name="still"/> (Reducir movimiento) y la emoción del
        /// viaje <paramref name="planned"/> (de <see cref="PickEmotion"/>).
        /// </summary>
        public void Begin(bool still, CapiEmotion planned)
        {
            Still = still;
            _planned = planned;
            _emotionShown = false;
            _emotion = CapiEmotion.None;
            _emotionMs = -1f;
            _queueHead = 0;
            _queueCount = 0;
            _current = CapiGesture.Rest;
            _currentMs = 0f;
            _loopMs = 0f;
            Traveling = false;
        }

        /// <summary>
        /// Reacciona a <paramref name="moment"/>: sus gestos a la cola (con Reducir movimiento, solo el primero) y, si trae la emoción
        /// del viaje y aún no salió, la burbuja. Sin asignaciones.
        /// </summary>
        public void React(CapiMoment moment)
        {
            int n = GesturesFor(moment, _scratch);
            for (int i = 0; i < n && (!Still || i == 0); i++)
            {
                Enqueue(_scratch[i]);
            }

            CapiEmotion e = EmotionFor(moment);
            if (e != CapiEmotion.None && e == _planned && !_emotionShown)
            {
                _emotionShown = true;
                _emotion = e;
                _emotionMs = 0f;
            }

            if (!IsOneShot(_current))
            {
                Next();
            }
        }

        /// <summary>
        /// Saca ya la emoción del viaje (si hay y aún no salió): con Reducir movimiento no hay viaje que cruce sus momentos, así que
        /// se ve quieta al abrir, con el gesto del tramo.
        /// </summary>
        public void ShowPlanned()
        {
            if (_planned == CapiEmotion.None || _emotionShown)
            {
                return;
            }

            _emotionShown = true;
            _emotion = _planned;
            _emotionMs = 0f;
        }

        /// <summary>Avanza <paramref name="dtMs"/> ms: el gesto de un golpe acaba y entra el siguiente; la burbuja se va a su tiempo.</summary>
        public void Tick(float dtMs)
        {
            float dt = Mathf.Max(0f, dtMs);
            _loopMs += Still ? 0f : dt;
            if (_emotionMs >= 0f)
            {
                _emotionMs += dt;
                if (_emotionMs >= (Still ? StillMs : EmotionMs))
                {
                    _emotionMs = -1f;
                }
            }

            if (IsOneShot(_current))
            {
                _currentMs += dt;
                float d = Still ? StillMs : DurationMs(_current);
                if (_currentMs >= d)
                {
                    Next();
                }

                return;
            }

            CapiGesture baseGesture = Traveling && !Still ? CapiGesture.Paddle : CapiGesture.Rest;
            if (_queueCount > 0)
            {
                Next();
            }
            else if (_current != baseGesture)
            {
                _current = baseGesture;
                _currentMs = 0f;
            }
        }

        /// <summary>Corta lo que haya (al cerrar o al saltar el viaje): ni cola ni burbuja, quieto.</summary>
        public void Stop()
        {
            _queueHead = 0;
            _queueCount = 0;
            _current = CapiGesture.Rest;
            _currentMs = 0f;
            _emotionMs = -1f;
            Traveling = false;
        }

        /// <summary>
        /// Escala de la burbuja (0-1) a <paramref name="elapsedMs"/> ms de su entrada: crece con un pequeño rebote en
        /// <see cref="BubbleInMs"/> y encoge en los últimos <see cref="BubbleOutMs"/>; quieta (1) con Reducir movimiento.
        /// </summary>
        public static float BubbleScale(float elapsedMs, bool still)
        {
            if (elapsedMs < 0f)
            {
                return 0f;
            }

            if (still)
            {
                return 1f;
            }

            if (elapsedMs < BubbleInMs)
            {
                float t = elapsedMs / BubbleInMs;
                return Mathf.Clamp(Ease.OutBack(t), 0f, 1.15f);
            }

            float left = EmotionMs - elapsedMs;
            return left < BubbleOutMs ? Mathf.Clamp01(left / BubbleOutMs) : 1f;
        }

        /// <summary>
        /// La postura de <paramref name="g"/> a la fase <paramref name="phase"/> (0-1) [P]. Sin asignaciones; con la misma entrada,
        /// la misma salida (Reducir movimiento pide la de <see cref="KeyPhase"/>).
        /// </summary>
        public static CapiPose Pose(CapiGesture g, float phase)
        {
            float t = Mathf.Clamp01(phase);
            float wave = Mathf.Sin(t * 2f * Mathf.PI);
            var p = new CapiPose { Arm = -30f, Paddle = 25f, PaddleHeld = true, Sample = -1f };
            switch (g)
            {
                case CapiGesture.Paddle:
                    p.Arm = -32f + (14f * wave);
                    p.Paddle = 22f + (22f * wave);
                    p.BodyTilt = 3f * wave;
                    break;
                case CapiGesture.PaddleHard:
                    float hard = Mathf.Sin(t * 3f * 2f * Mathf.PI);
                    p.Arm = -34f + (22f * hard);
                    p.Paddle = 20f + (34f * hard);
                    p.BodyTilt = -5f + (5f * hard);
                    p.Splash = 0.5f + (0.5f * Mathf.Abs(hard));
                    break;
                case CapiGesture.Lookout:
                    float up = Ease.OutCubic(Mathf.Clamp01(t * 1.6f));
                    p.Arm = Mathf.Lerp(-30f, 62f, up); // la mano a la frente, sobre el ojo
                    p.PaddleHeld = false;
                    p.BodyTilt = -4f * up;
                    p.BodyLift = 1f * up;
                    break;
                case CapiGesture.Wave:
                    float raise = Mathf.Clamp01(Mathf.Min(t, 1f - t) * 6f);
                    p.Arm = Mathf.Lerp(-30f, 80f + (20f * Mathf.Sin(t * 4f * Mathf.PI)), raise);
                    p.PaddleHeld = false;
                    p.BodyLift = 1f * raise;
                    break;
                case CapiGesture.CheerOar:
                    float cheer = Mathf.Sin(t * Mathf.PI);
                    p.Arm = Mathf.Lerp(-30f, 90f, cheer);
                    p.Paddle = Mathf.Lerp(25f, 180f, cheer);
                    p.BodyLift = 2.5f * cheer;
                    break;
                case CapiGesture.Tie:
                    p.Arm = Mathf.Lerp(-30f, 200f, Mathf.Sin(t * Mathf.PI));
                    p.PaddleHeld = false;
                    p.BodyTilt = 9f * Mathf.Sin(t * Mathf.PI);
                    p.Sample = t;
                    break;
                case CapiGesture.LeanOver:
                    float lean = Mathf.Sin(t * Mathf.PI);
                    p.BodyTilt = -14f * lean;
                    p.BodyLift = -1f * lean;
                    p.Arm = Mathf.Lerp(-30f, -70f, lean);
                    p.PaddleHeld = false;
                    break;
            }

            return p;
        }

        private static bool IsOneShot(CapiGesture g)
        {
            return g != CapiGesture.Rest && g != CapiGesture.Paddle;
        }

        private void Enqueue(CapiGesture g)
        {
            if (_queueCount >= QueueSize)
            {
                return; // la cola llena: se pierde (nunca bloquea «Jugar»)
            }

            _queue[(_queueHead + _queueCount) % QueueSize] = g;
            _queueCount++;
        }

        private void Next()
        {
            if (_queueCount > 0)
            {
                _current = _queue[_queueHead];
                _queueHead = (_queueHead + 1) % QueueSize;
                _queueCount--;
                _currentMs = 0f;
                return;
            }

            _current = Traveling && !Still ? CapiGesture.Paddle : CapiGesture.Rest;
            _currentMs = 0f;
        }
    }
}
