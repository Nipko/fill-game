#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Efectos del tablero que pide la cola de animación, una cuia o el overlay de una mecánica (guía visual v2, 6.6 y 6.8;
    /// ficha de Magic Sort §3). Las posiciones van en coordenadas del contenedor del tablero (las de las capas de tablero) y
    /// <c>unitDp</c> es la u de la cuia en dp. Todos comparten un tope de <see cref="FxLayer.MaxParticles"/> partículas: lo que
    /// no cabe se descarta. Con <see cref="ReduceMotion"/>, ninguno emite.
    /// </summary>
    public interface IBoardFx
    {
        /// <summary>Salpicadura al caer una dosis en <paramref name="surface"/>: 4-6 gotas y un anillo (guía 6.6).</summary>
        void Splash(Vector2 surface, Color tint, float unitDp);

        /// <summary>Gotas del sellado alrededor del borde de una cuia (07 §5.3; guía 6.8).</summary>
        void SealDrops(Vector2 rim, Color tint, float unitDp);

        /// <summary>
        /// <paramref name="count"/> destellos de estrella de 4 puntas alrededor de <paramref name="pos"/> que crecen y se
        /// apagan en 300-400 ms [P] (ficha de Magic Sort §3.1: el «?» se disuelve con 3-4 destellos). Aditivos, del color
        /// <paramref name="color"/>; cuentan en el tope de brillos aditivos (<see cref="FxLayer.MaxAdditive"/>).
        /// </summary>
        void Sparkles(Vector2 pos, Color color, int count, float unitDp);

        /// <summary>
        /// <paramref name="count"/> esquirlas del color <paramref name="color"/> que saltan desde <paramref name="pos"/>,
        /// giran y caen (ficha de Magic Sort §3.4-§3.5: hielo que revienta, astillas del armario) [P].
        /// </summary>
        void Shards(Vector2 pos, Color color, int count, float unitDp);

        /// <summary>
        /// <paramref name="count"/> burbujas blancas que suben desde <paramref name="pos"/> ondulando y estallan (propuesta de
        /// D-007 b.2: el Remolino sube con burbujas; el revelado de Aguas turbias, con una) [P].
        /// </summary>
        void Bubbles(Vector2 pos, int count, float unitDp);

        /// <summary>Reducir movimiento (02 §4; 07 §7.8): con true no se emiten partículas.</summary>
        bool ReduceMotion { get; set; }
    }

    /// <summary>
    /// Capa de efectos del tablero en un Canvas anidado propio (guía 3.4, capa 7; 8: "Reconstrucción de Canvas"). Un pool
    /// común de <see cref="MaxParticles"/> imágenes para todos los emisores (salpicaduras, gotas del sellado, destellos,
    /// esquirlas y burbujas; arquitectura del paso 1 §9 y §12): cada plaza toma el sprite y el material de lo que emite, y
    /// lo que excede el tope se descarta sin asignar (los últimos). Va dentro del contenedor del tablero (se mueve con él) y
    /// nunca recibe toques.
    /// </summary>
    /// <remarks>
    /// <para>Sin asignaciones al emitir ni en <see cref="Tick"/> (08 §5.4): datos en arrays fijos, plazas libres en una pila
    /// y las imágenes se muestran y se ocultan con <c>enabled</c>, registradas desde el principio en su Canvas
    /// (<see cref="GraphicPool"/>). Un gráfico ancla vacío y siempre activo mantiene el registro del Canvas de efectos: sin
    /// él, al reordenar las capas del tablero (cada nivel) UGUI desregistraría las partículas ocultas, borraría el registro
    /// vacío y el primer efecto del nivel lo volvería a crear.</para>
    /// <para>Sin Bloom: los brillos son sprites aditivos (guía 7), como mucho <see cref="MaxAdditive"/> a la vez.</para>
    /// </remarks>
    public sealed class FxLayer : IBoardFx
    {
        /// <summary>Partículas vivas como mucho: las 60 de la guía (6 y 8) menos las 16 del ambiente.</summary>
        public const int MaxParticles = 44;

        /// <summary>Brillos aditivos a la vez (aros y destellos; guía 8: ≤ 8). Lo que excede se descarta.</summary>
        public const int MaxAdditive = 8;

        /// <summary>Vida mínima de un destello (ms) [P].</summary>
        public const float SparkleLifeMinMs = 300f;

        /// <summary>Vida máxima de un destello (ms) [P].</summary>
        public const float SparkleLifeMaxMs = 400f;

        /// <summary>Retardo máximo de un destello respecto de la llamada (ms): no salen todos a la vez [P].</summary>
        public const float SparkleDelayMaxMs = 90f;

        /// <summary>Vida mínima de una esquirla (ms) [P].</summary>
        public const float ShardLifeMinMs = 550f;

        /// <summary>Vida máxima de una esquirla (ms) [P].</summary>
        public const float ShardLifeMaxMs = 700f;

        /// <summary>Vida mínima de una burbuja (ms) [P].</summary>
        public const float BubbleLifeMinMs = 450f;

        /// <summary>Vida máxima de una burbuja (ms) [P].</summary>
        public const float BubbleLifeMaxMs = 650f;

        /// <summary>Retardo máximo de una burbuja (ms): suben en hilera [P].</summary>
        public const float BubbleDelayMaxMs = 160f;

        private const float DropLifeMs = 250f;
        private const float RingLifeMs = 300f;

        // Destello: llega a su tamaño (con un rebote de easeOutBack) en el 40 % de su vida y después mengua hasta el 25 % y se
        // apaga; aparece en el primer 15 %.
        private const float SparklePeak = 0.4f;
        private const float SparkleFadeIn = 0.15f;

        // Burbuja: aparece en el primer 12 % y estalla en el último 20 % (crece un 50 % y se apaga).
        private const float BubbleFadeIn = 0.12f;
        private const float BubblePop = 0.8f;
        private const float BubbleAlpha = 0.85f;

        private readonly RectTransform _root;
        private readonly Image[] _images = new Image[MaxParticles];
        private readonly RectTransform[] _rects = new RectTransform[MaxParticles];
        private readonly Kind[] _kind = new Kind[MaxParticles];
        private readonly Kind[] _dressed = new Kind[MaxParticles];
        private readonly Vector2[] _pos = new Vector2[MaxParticles];
        private readonly Vector2[] _vel = new Vector2[MaxParticles];
        private readonly float[] _age = new float[MaxParticles];
        private readonly float[] _delay = new float[MaxParticles];
        private readonly float[] _life = new float[MaxParticles];
        private readonly float[] _gravity = new float[MaxParticles];
        private readonly float[] _angle = new float[MaxParticles];
        private readonly float[] _spin = new float[MaxParticles];
        private readonly float[] _phase = new float[MaxParticles];
        private readonly float[] _sway = new float[MaxParticles];
        private readonly Color[] _color = new Color[MaxParticles];
        private readonly int[] _free = new int[MaxParticles];
        private readonly Sprite _dropSprite;
        private readonly Sprite _ringSprite;
        private readonly Sprite _sparkleSprite;
        private readonly Sprite _shardSprite;
        private readonly Sprite _bubbleSprite;
        private readonly Material? _additive;
        private uint _rng = 0x9E3779B9u;
        private int _freeCount;
        private int _additiveCount;
        private int _discarded;

        /// <summary>Crea la capa dentro de <paramref name="board"/> (contenedor del tablero, origen arriba a la izquierda).</summary>
        public FxLayer(RectTransform board, ProceduralArt art)
        {
            var go = new GameObject("Fx", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(board, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            _dropSprite = art.Drop;
            _ringSprite = art.Ring;
            _sparkleSprite = art.StarSparkle; // perezosos: se generan aquí y no al emitir
            _shardSprite = art.Shard;
            _bubbleSprite = art.Bubble;
            _additive = UiMaterials.Additive;

            Image anchor = NewImage("RegistryAnchor");
            anchor.color = Color.clear;
            anchor.canvasRenderer.cullTransparentMesh = true;
            anchor.enabled = true;
            for (int i = 0; i < MaxParticles; i++)
            {
                _images[i] = NewImage("Particle" + i);
                _rects[i] = _images[i].rectTransform;
                _age[i] = -1f;
                _free[i] = MaxParticles - 1 - i; // la plaza 0 sale primero
                GraphicPool.EnsureRegistered(_images[i]);
            }

            _freeCount = MaxParticles;
        }

        private enum Kind : byte
        {
            None,
            Drop,
            Ring,
            Sparkle,
            Shard,
            Bubble,
        }

        /// <summary>Raíz de la capa (para ordenarla delante de las cuias).</summary>
        public RectTransform Root => _root;

        /// <summary>Partículas vivas, contando las que esperan su retardo (≤ <see cref="MaxParticles"/>).</summary>
        public int ActiveCount => MaxParticles - _freeCount;

        /// <summary>Brillos aditivos vivos (aros y destellos; ≤ <see cref="MaxAdditive"/>).</summary>
        public int AdditiveCount => _additiveCount;

        /// <summary>Partículas descartadas por los topes desde que se creó la capa (diagnóstico y tests).</summary>
        public int DiscardedCount => _discarded;

        /// <inheritdoc />
        /// <remarks>Con true, ningún emisor emite (02 §4: sin partículas); lo que ya está en vuelo termina su vida.</remarks>
        public bool ReduceMotion { get; set; }

        /// <inheritdoc />
        public void Splash(Vector2 surface, Color tint, float unitDp)
        {
            if (ReduceMotion)
            {
                return;
            }

            // 4-6 gotas de 0,06-0,10 u lanzadas a 1,5-2,5 u/s hacia arriba (±35°) con gravedad de 18 u/s², y un anillo
            // elíptico que crece de 0,4 a 1,3 u con alfa 0,40 → 0 (guía 6.6).
            int n = 4 + (int)(Next() * 3f);
            for (int i = 0; i < n; i++)
            {
                float ang = (Next() - 0.5f) * 70f * Mathf.Deg2Rad;
                float speed = Mathf.Lerp(1.5f, 2.5f, Next()) * unitDp;
                EmitDrop(surface, new Vector2(Mathf.Sin(ang) * speed, Mathf.Cos(ang) * speed), tint, Mathf.Lerp(0.12f, 0.20f, Next()) * unitDp, 18f * unitDp);
            }

            EmitRing(surface, tint, unitDp);
        }

        /// <inheritdoc />
        public void SealDrops(Vector2 rim, Color tint, float unitDp)
        {
            if (ReduceMotion)
            {
                return;
            }

            int n = 6 + (int)(Next() * 3f);
            for (int i = 0; i < n; i++)
            {
                float ang = (((i / (float)n) - 0.5f) * 140f) * Mathf.Deg2Rad;
                float speed = Mathf.Lerp(1.6f, 2.6f, Next()) * unitDp;
                EmitDrop(rim, new Vector2(Mathf.Sin(ang) * speed, Mathf.Cos(ang) * speed), ColorMath.Lighten(tint, 0.25f), Mathf.Lerp(0.10f, 0.16f, Next()) * unitDp, 18f * unitDp);
            }
        }

        /// <inheritdoc />
        public void Sparkles(Vector2 pos, Color color, int count, float unitDp)
        {
            if (ReduceMotion)
            {
                return;
            }

            // [P] Repartidos en una elipse de 0,55 × 0,45 u, de 0,45-0,70 u de lado, con 0-90 ms de retardo, un giro suave
            // (±20° y ±60 °/s) y una deriva hacia arriba de 0,15-0,35 u/s.
            count = Mathf.Min(count, MaxParticles);
            for (int k = 0; k < count; k++)
            {
                int i = Claim(Kind.Sparkle);
                if (i < 0)
                {
                    continue;
                }

                float a = Next() * 2f * Mathf.PI;
                float r = Mathf.Sqrt(Next());
                var at = new Vector2(pos.x + (Mathf.Cos(a) * r * 0.55f * unitDp), pos.y + (Mathf.Sin(a) * r * 0.45f * unitDp));
                float side = Mathf.Lerp(0.45f, 0.70f, Next()) * unitDp;
                Launch(
                    i,
                    at,
                    new Vector2(0f, Mathf.Lerp(0.15f, 0.35f, Next()) * unitDp),
                    0f,
                    Mathf.Lerp(SparkleLifeMinMs, SparkleLifeMaxMs, Next()),
                    Next() * SparkleDelayMaxMs,
                    color,
                    new Vector2(side, side),
                    (Next() - 0.5f) * 40f,
                    (Next() - 0.5f) * 120f);
            }
        }

        /// <inheritdoc />
        public void Shards(Vector2 pos, Color color, int count, float unitDp)
        {
            if (ReduceMotion)
            {
                return;
            }

            // [P] Lanzadas a 3-5 u/s en un abanico de ±65° hacia arriba con gravedad de 16 u/s² (suben ≈ 0,5-0,8 u y caen
            // a un paso de cuia), de 0,18-0,30 u de largo, girando a 360-900 °/s; se apagan y menguan en el último 40 %.
            count = Mathf.Min(count, MaxParticles);
            for (int k = 0; k < count; k++)
            {
                int i = Claim(Kind.Shard);
                if (i < 0)
                {
                    continue;
                }

                float ang = (Next() - 0.5f) * 130f * Mathf.Deg2Rad;
                float speed = Mathf.Lerp(3f, 5f, Next()) * unitDp;
                float length = Mathf.Lerp(0.18f, 0.30f, Next()) * unitDp;
                float spin = Mathf.Lerp(360f, 900f, Next()) * (Next() < 0.5f ? -1f : 1f);
                Launch(
                    i,
                    new Vector2(pos.x + ((Next() - 0.5f) * 0.3f * unitDp), pos.y),
                    new Vector2(Mathf.Sin(ang) * speed, Mathf.Cos(ang) * speed),
                    16f * unitDp,
                    Mathf.Lerp(ShardLifeMinMs, ShardLifeMaxMs, Next()),
                    0f,
                    color,
                    new Vector2(length * 0.8f, length),
                    Next() * 360f,
                    spin);
            }
        }

        /// <inheritdoc />
        public void Bubbles(Vector2 pos, int count, float unitDp)
        {
            if (ReduceMotion)
            {
                return;
            }

            // [P] De 0,14-0,26 u, repartidas en ±0,35 u y 0,25 u de alto, con 0-160 ms de retardo; suben a 1,2-2,0 u/s y
            // aceleran 1,5 u/s², ondulan ±0,04-0,08 u a 3-4,5 Hz y estallan.
            count = Mathf.Min(count, MaxParticles);
            for (int k = 0; k < count; k++)
            {
                int i = Claim(Kind.Bubble);
                if (i < 0)
                {
                    continue;
                }

                float side = Mathf.Lerp(0.14f, 0.26f, Next()) * unitDp;
                Launch(
                    i,
                    new Vector2(pos.x + ((Next() - 0.5f) * 0.7f * unitDp), pos.y + (Next() * 0.25f * unitDp)),
                    new Vector2(0f, Mathf.Lerp(1.2f, 2.0f, Next()) * unitDp),
                    -1.5f * unitDp,
                    Mathf.Lerp(BubbleLifeMinMs, BubbleLifeMaxMs, Next()),
                    Next() * BubbleDelayMaxMs,
                    new Color(1f, 1f, 1f, BubbleAlpha),
                    new Vector2(side, side),
                    0f,
                    Mathf.Lerp(3f, 4.5f, Next()) * 2f * Mathf.PI); // en las burbujas, el giro es la frecuencia de la onda (rad/s)
                _phase[i] = Next() * 2f * Mathf.PI;
                _sway[i] = Mathf.Lerp(0.04f, 0.08f, Next()) * unitDp;
            }
        }

        /// <summary>Retira todos los efectos.</summary>
        public void Clear()
        {
            for (int i = 0; i < MaxParticles; i++)
            {
                if (_age[i] >= 0f)
                {
                    Release(i);
                }
            }
        }

        /// <summary>Avanza los efectos (y retira los que terminan).</summary>
        public void Tick(float dtMs)
        {
            for (int i = 0; i < MaxParticles; i++)
            {
                if (_age[i] < 0f)
                {
                    continue;
                }

                _age[i] += dtMs;
                float local = _age[i] - _delay[i];
                if (local <= 0f)
                {
                    continue; // aún espera su retardo (invisible)
                }

                if (local >= _life[i])
                {
                    Release(i);
                    continue;
                }

                // El primer fotograma tras el retardo solo avanza lo que pasa de él.
                float dt = Mathf.Min(dtMs, local) / 1000f;
                _vel[i].y -= _gravity[i] * dt;
                _pos[i] += _vel[i] * dt;
                _angle[i] += _spin[i] * dt;
                Render(i, local / _life[i], local);
            }
        }

        private void EmitDrop(Vector2 pos, Vector2 vel, Color tint, float sizeDp, float gravity)
        {
            int i = Claim(Kind.Drop);
            if (i >= 0)
            {
                Launch(i, pos, vel, gravity, DropLifeMs, 0f, new Color(tint.r, tint.g, tint.b, 1f), new Vector2(sizeDp, sizeDp), 0f, 0f);
            }
        }

        private void EmitRing(Vector2 pos, Color tint, float unitDp)
        {
            // El aro mide 2 × 0,55 u a escala 1 y crece de la escala 0,4 a la 1,3.
            int i = Claim(Kind.Ring);
            if (i >= 0)
            {
                Launch(i, pos, Vector2.zero, 0f, RingLifeMs, 0f, ColorMath.Lighten(tint, 0.55f), new Vector2(2f * unitDp, 0.55f * unitDp), 0f, 0f);
            }
        }

        // Toma una plaza libre para kind y le pone su sprite y su material, o devuelve −1 si no cabe (tope común o de
        // aditivos): se descarta sin asignar.
        private int Claim(Kind kind)
        {
            bool additive = IsAdditive(kind);
            if (_freeCount == 0 || (additive && _additiveCount >= MaxAdditive))
            {
                _discarded++;
                return -1;
            }

            int i = _free[--_freeCount];
            _kind[i] = kind;
            if (additive)
            {
                _additiveCount++;
            }

            if (_dressed[i] != kind)
            {
                // La imagen está oculta: cambiar el sprite o el material no la marca para reconstruir.
                Image img = _images[i];
                img.sprite = SpriteOf(kind);
                img.material = additive ? _additive : null;
                _dressed[i] = kind;
            }

            return i;
        }

        private void Launch(int i, Vector2 pos, Vector2 vel, float gravity, float lifeMs, float delayMs, Color color, Vector2 size, float angle, float spin)
        {
            _pos[i] = pos;
            _vel[i] = vel;
            _gravity[i] = gravity;
            _life[i] = lifeMs;
            _delay[i] = delayMs;
            _age[i] = 0f;
            _color[i] = color;
            _angle[i] = angle;
            _spin[i] = spin;
            _phase[i] = 0f;
            _sway[i] = 0f;
            RectTransform rt = _rects[i];
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            Render(i, 0f, 0f);
            _images[i].enabled = true;
        }

        // Pinta la plaza i en t = 0..1 de su vida (localMs desde que acabó su retardo).
        private void Render(int i, float t, float localMs)
        {
            RectTransform rt = _rects[i];
            Color c = _color[i];
            Vector2 at = _pos[i];
            switch (_kind[i])
            {
                case Kind.Drop:
                    c.a *= 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
                    break;
                case Kind.Ring:
                    SetScale(rt, Mathf.Lerp(0.4f, 1.3f, Ease.OutCubic(t)));
                    c.a = 0.40f * (1f - t);
                    break;
                case Kind.Sparkle:
                    if (t < SparklePeak)
                    {
                        SetScale(rt, Ease.OutBack(t / SparklePeak));
                        c.a *= Mathf.Clamp01(t / SparkleFadeIn);
                    }
                    else
                    {
                        float k = Ease.In((t - SparklePeak) / (1f - SparklePeak));
                        SetScale(rt, Mathf.Lerp(1f, 0.25f, k));
                        c.a *= 1f - k;
                    }

                    rt.localRotation = Quaternion.Euler(0f, 0f, _angle[i]);
                    break;
                case Kind.Shard:
                    float fade = Mathf.Clamp01((t - 0.6f) / 0.4f);
                    SetScale(rt, 1f - (0.3f * fade));
                    c.a *= 1f - fade;
                    rt.localRotation = Quaternion.Euler(0f, 0f, _angle[i]);
                    break;
                case Kind.Bubble:
                    at.x += Mathf.Sin(_phase[i] + (localMs * 0.001f * _spin[i])) * _sway[i];
                    if (t < BubbleFadeIn)
                    {
                        float e = t / BubbleFadeIn;
                        SetScale(rt, Mathf.Lerp(0.6f, 1f, e));
                        c.a *= e;
                    }
                    else if (t < BubblePop)
                    {
                        SetScale(rt, 1f);
                    }
                    else
                    {
                        float p = (t - BubblePop) / (1f - BubblePop);
                        SetScale(rt, 1f + (0.5f * Ease.Out(p)));
                        c.a *= 1f - p;
                    }

                    break;
            }

            rt.anchoredPosition = at;
            _images[i].color = c;
        }

        private void Release(int i)
        {
            _age[i] = -1f;
            _images[i].enabled = false;
            if (IsAdditive(_kind[i]))
            {
                _additiveCount--;
            }

            _kind[i] = Kind.None;
            _free[_freeCount++] = i;
        }

        private Sprite SpriteOf(Kind kind)
        {
            switch (kind)
            {
                case Kind.Ring:
                    return _ringSprite;
                case Kind.Sparkle:
                    return _sparkleSprite;
                case Kind.Shard:
                    return _shardSprite;
                case Kind.Bubble:
                    return _bubbleSprite;
                default:
                    return _dropSprite;
            }
        }

        private static bool IsAdditive(Kind kind)
        {
            return kind == Kind.Ring || kind == Kind.Sparkle;
        }

        private static void SetScale(RectTransform rt, float k)
        {
            rt.localScale = new Vector3(k, k, 1f);
        }

        private Image NewImage(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_root, false);
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.enabled = false; // el GameObject queda activo: se muestra y se oculta con enabled (sin asignar)
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            return img;
        }

        // xorshift32: solo presentación (no interviene en reglas ni en el núcleo).
        private float Next()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            return (_rng & 0x00FFFFFF) / 16777216f;
        }
    }
}
