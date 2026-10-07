#nullable enable
using System;
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Sumergir y teñir simplificada (GDD 02 §4.2; 07 §5.1; 08 §5.3): (a) las cuias vacías caen 2 u hasta su ranura,
    /// (b) la banda de río sube 1,5 u y el agua base llena las cavidades al 70 %, (c) la banda baja, (d) cada dosis se
    /// tiñe en 0,10 s de arriba abajo con interpolación OKLab y las posiciones sin dosis se vacían. Fases de 1,4 s:
    /// 300 + 400 + 250 + ≤ 450 ms; de 0,8 s: 150 + 250 + 150 + 250 ms. Saltable con <see cref="Complete"/>.
    /// </summary>
    /// <remarks>Sin iconos de fruta ni partículas (arte final fuera del prototipo v0).</remarks>
    public sealed class IntroAnimation
    {
        /// <summary>Una franja se tiñe en 100 ms (02 §4.2).</summary>
        public const float StripeMs = 100f;

        /// <summary>Altura desde la que caen las cuias (07 §5.1): 2,0 u.</summary>
        public const float DropU = 2.0f;

        /// <summary>Subida de la banda de río (07 §5.1): 1,5 u.</summary>
        public const float RiverRiseU = 1.5f;

        /// <summary>Opacidad del agua base dentro de la cavidad (07 §5.1): 70 %.</summary>
        public const float WaterAlpha = 0.70f;

        private CuiaView[] _views = Array.Empty<CuiaView>();
        private Cuia[] _final = Array.Empty<Cuia>();
        private bool[] _sealed = Array.Empty<bool>();
        private RectTransform? _river;
        private Vector2 _riverRest;
        private Color _water;
        private float _t;
        private float _a;
        private float _b;
        private float _c;
        private float _d;
        private float _dropStagger;
        private float _dropMs;
        private float _dDose;
        private float _dCuia;

        /// <summary>La animación está en curso.</summary>
        public bool Running { get; private set; }

        /// <summary>
        /// Arranca la intro de <paramref name="totalMs"/> para <paramref name="board"/>; las vistas quedan bloqueadas
        /// hasta el final.
        /// </summary>
        public void Start(CuiaView[] views, BoardState board, Color water, RectTransform? river, int totalMs)
        {
            Complete();
            _views = views;
            int n = views.Length;
            if (_final.Length != n)
            {
                _final = new Cuia[n];
                _sealed = new bool[n];
            }

            int maxCap = 1;
            for (int i = 0; i < n; i++)
            {
                _final[i] = board[i];
                _sealed[i] = board.IsSealed(i);
                maxCap = Mathf.Max(maxCap, views[i].Capacity);
            }

            _water = new Color(water.r, water.g, water.b, WaterAlpha);
            _river = river;
            _riverRest = river != null ? river.anchoredPosition : Vector2.zero;

            bool longVersion = totalMs >= 1000;
            float scale = totalMs / (longVersion ? 1400f : 800f);
            _a = (longVersion ? 300f : 150f) * scale;
            _b = (longVersion ? 400f : 250f) * scale;
            _c = (longVersion ? 250f : 150f) * scale;
            _d = Mathf.Max(StripeMs, totalMs - _a - _b - _c);
            _dropStagger = Mathf.Min(longVersion ? 15f : 8f, (_a * 0.3f) / Mathf.Max(1, n - 1));
            _dropMs = Mathf.Max(1f, _a - (_dropStagger * (n - 1)));
            _dDose = Mathf.Min(longVersion ? 80f : 40f, (_d - StripeMs) / Mathf.Max(1, maxCap - 1));
            _dCuia = Mathf.Max(0f, (_d - StripeMs - (_dDose * (maxCap - 1))) / Mathf.Max(1, n - 1));
            _t = 0f;
            Running = true;
            for (int i = 0; i < n; i++)
            {
                views[i].Lock();
                views[i].SetSealed(false);
                for (int k = 0; k < views[i].Capacity; k++)
                {
                    views[i].SetDose(k, Color.clear, 0f, 0);
                }
            }

            Apply();
        }

        /// <summary>Avanza la intro.</summary>
        public void Tick(float dtMs)
        {
            if (!Running)
            {
                return;
            }

            _t += dtMs;
            if (_t >= _a + _b + _c + _d)
            {
                Complete();
                return;
            }

            Apply();
        }

        /// <summary>Relee la posición de reposo de la banda de río tras un cambio de layout durante la intro.</summary>
        public void RefreshRiverRest()
        {
            if (_river != null)
            {
                _riverRest = _river.anchoredPosition;
            }
        }

        /// <summary>Salta al estado final en el mismo fotograma (02 §4.2).</summary>
        public void Complete()
        {
            if (!Running)
            {
                return;
            }

            Running = false;
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].Unlock();
                _views[i].ShowContent(_final[i], _sealed[i]);
            }

            if (_river != null)
            {
                _river.anchoredPosition = _riverRest;
            }
        }

        private void Apply()
        {
            float t = _t;
            int n = _views.Length;
            float unit = n > 0 ? _views[0].Unit : 24f;

            if (_river != null)
            {
                float rise;
                if (t < _a)
                {
                    rise = 0f;
                }
                else if (t < _a + _b)
                {
                    rise = Ease.Out((t - _a) / _b);
                }
                else if (t < _a + _b + _c)
                {
                    rise = 1f - Ease.InOut((t - _a - _b) / _c);
                }
                else
                {
                    rise = 0f;
                }

                _river.anchoredPosition = _riverRest + new Vector2(0f, rise * RiverRiseU * unit);
            }

            float fillLevel = Mathf.Clamp01((t - _a) / (_b * 0.7f));
            float tintStart = _a + _b + _c;
            for (int i = 0; i < n; i++)
            {
                CuiaView v = _views[i];
                float drop = Mathf.Clamp01((t - (_dropStagger * i)) / _dropMs);
                v.SetPose(new Vector2(0f, DropU * v.Unit * (1f - Ease.In(drop))), 0f);

                int cap = v.Capacity;
                Cuia final = _final[i];
                float level = fillLevel * cap;
                for (int k = 0; k < cap; k++)
                {
                    float baseFill = Mathf.Clamp01(level - k);
                    float tk = tintStart + (_dCuia * i) + (_dDose * (cap - 1 - k)); // de arriba abajo (07 §5.1)
                    float s = Mathf.Clamp01((t - tk) / StripeMs);
                    if (t < tintStart || s <= 0f)
                    {
                        v.SetDose(k, _water, baseFill, 0);
                    }
                    else if (k < final.Count)
                    {
                        int tint = final.ColorAt(k);
                        v.SetDose(k, ColorMath.OkLabLerp(_water, Palette.Tint(tint), s), 1f, tint);
                    }
                    else
                    {
                        v.SetDose(k, new Color(_water.r, _water.g, _water.b, WaterAlpha * (1f - s)), 1f - s, 0);
                    }
                }
            }
        }
    }
}
