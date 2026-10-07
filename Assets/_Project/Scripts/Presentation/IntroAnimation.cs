#nullable enable
using System;
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Sumergir y teñir simplificada (GDD 02 §4.2; 07 §5.1; 08 §5.3; guía visual v2, 6.11): (a) las cuias vacías caen
    /// 2 u hasta su repisa, (b) el agua de inmersión de cada fila (dentro del panel, desde la cara de la repisa) sube
    /// 1,5 u y el agua base llena las cavidades al 70 %, (c) el agua baja, (d) cada dosis se tiñe en 0,10 s de arriba
    /// abajo con interpolación OKLab y las posiciones sin dosis se vacían. Fases de 1,4 s: 300 + 400 + 250 + ≤ 450 ms;
    /// de 0,8 s: 150 + 250 + 150 + 250 ms. Saltable con <see cref="Complete"/>.
    /// </summary>
    /// <remarks>Sin iconos de fruta (fase de jugabilidad). Una salpicadura por fila al sumergirse (≤ 20 partículas).</remarks>
    public sealed class IntroAnimation
    {
        /// <summary>Una franja se tiñe en 100 ms (02 §4.2).</summary>
        public const float StripeMs = 100f;

        /// <summary>Altura desde la que caen las cuias (07 §5.1): 2,0 u.</summary>
        public const float DropU = 2.0f;

        /// <summary>Subida del agua de inmersión (07 §5.1): 1,5 u.</summary>
        public const float RiverRiseU = 1.5f;

        /// <summary>Opacidad del agua base dentro de la cavidad (07 §5.1): 70 %.</summary>
        public const float WaterAlpha = 0.70f;

        private CuiaView[] _views = Array.Empty<CuiaView>();
        private Cuia[] _final = Array.Empty<Cuia>();
        private bool[] _sealed = Array.Empty<bool>();
        private RectTransform[] _rows = Array.Empty<RectTransform>();
        private ImmersionWater?[] _rowWater = Array.Empty<ImmersionWater?>();
        private IBoardFx? _fx;
        private bool _splashed;
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
        /// hasta el final. <paramref name="rowWater"/>: agua de inmersión de cada fila, con el pivote abajo en la cara de la
        /// repisa (la intro solo anima su alto); <paramref name="fx"/>: salpicaduras (opcional).
        /// </summary>
        public void Start(CuiaView[] views, BoardState board, Color water, RectTransform[] rowWater, int totalMs, IBoardFx? fx = null)
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
            _rows = rowWater;
            if (_rowWater.Length != rowWater.Length)
            {
                _rowWater = new ImmersionWater?[rowWater.Length];
            }

            float unit0 = n > 0 ? views[0].Unit : 24f;
            for (int r = 0; r < rowWater.Length; r++)
            {
                ImmersionWater? w = rowWater[r] != null ? rowWater[r].GetComponent<ImmersionWater>() : null;
                _rowWater[r] = w;
                if (w != null)
                {
                    w.SetWater(water);
                    w.SetUnit(unit0);
                    w.SetTime(0f);
                }
            }

            _fx = fx;
            _splashed = false;

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

            SetRowWater(0f, 24f);
        }

        private void SetRowWater(float rise, float unit)
        {
            float height = rise * RiverRiseU * unit;
            for (int r = 0; r < _rows.Length; r++)
            {
                RectTransform rt = _rows[r];
                bool on = height > 0.5f;
                if (rt.gameObject.activeSelf != on)
                {
                    rt.gameObject.SetActive(on);
                }

                if (on)
                {
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
                    ImmersionWater? w = r < _rowWater.Length ? _rowWater[r] : null;
                    if (w != null)
                    {
                        w.SetTime(_t); // onda y destellos del agua (guía 6.11)
                    }
                }
            }
        }

        private void Apply()
        {
            float t = _t;
            int n = _views.Length;
            float unit = n > 0 ? _views[0].Unit : 24f;

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

                SetRowWater(rise, unit);
                if (!_splashed && t >= _a && _fx != null)
                {
                    _splashed = true;
                    for (int r = 0; r < _rows.Length; r++)
                    {
                        _fx.Splash(_rows[r].anchoredPosition + new Vector2(0f, 0.3f * unit), _water, unit * 1.6f);
                    }
                }
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
