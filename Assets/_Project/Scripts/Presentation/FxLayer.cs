#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>Efectos del tablero que pide la cola de animación o una cuia (guía visual v2, 6.6 y 6.8).</summary>
    public interface IBoardFx
    {
        /// <summary>Salpicadura al caer una dosis en <paramref name="surface"/> (coordenadas del contenedor del tablero).</summary>
        void Splash(Vector2 surface, Color tint, float unitDp);

        /// <summary>Gotas del sellado alrededor del borde de una cuia (07 §5.3).</summary>
        void SealDrops(Vector2 rim, Color tint, float unitDp);
    }

    /// <summary>
    /// Capa de efectos del tablero en un Canvas anidado propio (guía 3.4, capa 7; 8: "Reconstrucción de Canvas"), con
    /// imágenes reutilizadas y sin asignaciones por fotograma: gotas balísticas con borde claro y anillos elípticos
    /// aditivos. Va dentro del contenedor del tablero (se mueve con él) y nunca recibe toques.
    /// </summary>
    public sealed class FxLayer : IBoardFx
    {
        private const int MaxDrops = 28;
        private const int MaxRings = 6;
        private const float DropLifeMs = 250f;
        private const float RingLifeMs = 300f;

        private readonly RectTransform _root;
        private readonly Image[] _drops = new Image[MaxDrops];
        private readonly Vector2[] _dropPos = new Vector2[MaxDrops];
        private readonly Vector2[] _dropVel = new Vector2[MaxDrops];
        private readonly float[] _dropAge = new float[MaxDrops];
        private readonly float[] _dropGravity = new float[MaxDrops];
        private readonly Color[] _dropColor = new Color[MaxDrops];
        private readonly Image[] _rings = new Image[MaxRings];
        private readonly Vector2[] _ringPos = new Vector2[MaxRings];
        private readonly float[] _ringAge = new float[MaxRings];
        private readonly float[] _ringUnit = new float[MaxRings];
        private readonly Color[] _ringColor = new Color[MaxRings];
        private uint _rng = 0x9E3779B9u;
        private int _nextDrop;
        private int _nextRing;
        private int _active;

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
            for (int i = 0; i < MaxRings; i++)
            {
                _rings[i] = NewImage("SplashRing" + i, art.Ring, UiMaterials.Additive);
                _ringAge[i] = -1f;
            }

            for (int i = 0; i < MaxDrops; i++)
            {
                _drops[i] = NewImage("Drop" + i, art.Drop, null);
                _dropAge[i] = -1f;
            }
        }

        /// <summary>Raíz de la capa (para ordenarla delante de las cuias).</summary>
        public RectTransform Root => _root;

        /// <summary>Partículas vivas (presupuesto de 60, guía 6).</summary>
        public int ActiveCount => _active;

        /// <inheritdoc />
        public void Splash(Vector2 surface, Color tint, float unitDp)
        {
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
            int n = 6 + (int)(Next() * 3f);
            for (int i = 0; i < n; i++)
            {
                float ang = (((i / (float)n) - 0.5f) * 140f) * Mathf.Deg2Rad;
                float speed = Mathf.Lerp(1.6f, 2.6f, Next()) * unitDp;
                EmitDrop(rim, new Vector2(Mathf.Sin(ang) * speed, Mathf.Cos(ang) * speed), ColorMath.Lighten(tint, 0.25f), Mathf.Lerp(0.10f, 0.16f, Next()) * unitDp, 18f * unitDp);
            }
        }

        /// <summary>Retira todos los efectos.</summary>
        public void Clear()
        {
            for (int i = 0; i < MaxDrops; i++)
            {
                _dropAge[i] = -1f;
                SetActive(_drops[i], false);
            }

            for (int i = 0; i < MaxRings; i++)
            {
                _ringAge[i] = -1f;
                SetActive(_rings[i], false);
            }

            _active = 0;
        }

        /// <summary>Avanza los efectos.</summary>
        public void Tick(float dtMs)
        {
            float dt = dtMs / 1000f;
            int active = 0;
            for (int i = 0; i < MaxDrops; i++)
            {
                if (_dropAge[i] < 0f)
                {
                    continue;
                }

                _dropAge[i] += dtMs;
                if (_dropAge[i] >= DropLifeMs)
                {
                    _dropAge[i] = -1f;
                    SetActive(_drops[i], false);
                    continue;
                }

                active++;
                _dropVel[i].y -= _dropGravity[i] * dt;
                _dropPos[i] += _dropVel[i] * dt;
                Image img = _drops[i];
                img.rectTransform.anchoredPosition = _dropPos[i];
                Color c = _dropColor[i];
                c.a *= 1f - Mathf.Clamp01((_dropAge[i] - (DropLifeMs * 0.6f)) / (DropLifeMs * 0.4f));
                img.color = c;
            }

            for (int i = 0; i < MaxRings; i++)
            {
                if (_ringAge[i] < 0f)
                {
                    continue;
                }

                _ringAge[i] += dtMs;
                float t = _ringAge[i] / RingLifeMs;
                if (t >= 1f)
                {
                    _ringAge[i] = -1f;
                    SetActive(_rings[i], false);
                    continue;
                }

                active++;
                float e = Ease.OutCubic(t);
                float w = Mathf.Lerp(0.4f, 1.3f, e) * _ringUnit[i];
                Image img = _rings[i];
                img.rectTransform.sizeDelta = new Vector2(w * 2f, w * 0.55f);
                Color c = _ringColor[i];
                c.a = 0.40f * (1f - t);
                img.color = c;
            }

            _active = active;
        }

        private void EmitDrop(Vector2 pos, Vector2 vel, Color tint, float sizeDp, float gravity)
        {
            int i = _nextDrop;
            _nextDrop = (_nextDrop + 1) % MaxDrops;
            _dropPos[i] = pos;
            _dropVel[i] = vel;
            _dropAge[i] = 0f;
            _dropGravity[i] = gravity;
            _dropColor[i] = new Color(tint.r, tint.g, tint.b, 1f);
            Image img = _drops[i];
            img.rectTransform.sizeDelta = new Vector2(sizeDp, sizeDp);
            img.rectTransform.anchoredPosition = pos;
            img.color = _dropColor[i];
            SetActive(img, true);
        }

        private void EmitRing(Vector2 pos, Color tint, float unitDp)
        {
            int i = _nextRing;
            _nextRing = (_nextRing + 1) % MaxRings;
            _ringPos[i] = pos;
            _ringAge[i] = 0f;
            _ringUnit[i] = unitDp;
            _ringColor[i] = ColorMath.Lighten(tint, 0.55f);
            Image img = _rings[i];
            img.rectTransform.anchoredPosition = pos;
            img.rectTransform.sizeDelta = new Vector2(0.8f * unitDp, 0.22f * unitDp);
            img.color = new Color(_ringColor[i].r, _ringColor[i].g, _ringColor[i].b, 0.4f);
            SetActive(img, true);
        }

        private Image NewImage(string name, Sprite sprite, Material? material)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_root, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (material != null)
            {
                img.material = material;
            }

            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            go.SetActive(false);
            return img;
        }

        private static void SetActive(Image img, bool on)
        {
            if (img.gameObject.activeSelf != on)
            {
                img.gameObject.SetActive(on);
            }
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
