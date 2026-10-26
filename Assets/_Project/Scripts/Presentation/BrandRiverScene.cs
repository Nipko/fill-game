#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// «El Amazonas que se tiñe» para la intro y la pantalla de arranque en vertical (añadido de D-051, tercera pasada, a petición
    /// del dueño: «que se entienda más el río Amazonas, con meandros»). Compone por código, para un lienzo de
    /// <see cref="Width"/> × <see cref="Height"/> dp, un río ancho de agua blanca (#B08A5A, glosario §9) que serpentea desde el
    /// horizonte, fino entre la selva, hasta el primer plano, con orillas de selva en copas a los lados, playas de arena en el
    /// interior de las curvas, una isla y ondas; y, sobre él, las piezas del icono v2 (<see cref="BrandEmblem"/>): la cuia de
    /// vidrio que vierte el chorro sobre un punto del río (<see cref="Impact"/>), con sus aros, y el açaí que tiñe el cauce río
    /// abajo desde el impacto hacia el primer plano, en <see cref="ChunkCount"/> tramos que la intro enciende uno tras otro.
    /// Referencias: la propuesta A del icono (<c>docs/art/marca-oficial/v2/bocetos/a-meandro.svg</c>) y el río del inicio
    /// (D-050). Todo en dp, con y hacia abajo; nada por fotograma.
    /// </summary>
    public sealed class BrandRiverScene
    {
        /// <summary>Tramos del açaí río abajo (la intro los enciende en orden) [P].</summary>
        public const int ChunkCount = 7;

        /// <summary>Dónde vierte la cuia, a lo largo del río (0 en el horizonte, 1 abajo del todo) [P].</summary>
        public const float ImpactV = 0.36f;

        // El eje del río: (u, v) con u la fracción del ancho y v la del cauce, del horizonte (0) al borde de abajo (1) [P].
        private static readonly float[] CtrlU = { 0.56f, 0.64f, 0.52f, 0.37f, 0.47f, 0.64f, 0.64f, 0.48f, 0.36f, 0.44f };
        private static readonly float[] CtrlV = { -0.02f, 0.06f, 0.16f, 0.27f, 0.38f, 0.50f, 0.63f, 0.75f, 0.88f, 1.04f };

        private const int Samples = 260;
        private const float Perspective = 1.3f;
        private const float HorizonAt = 0.30f;
        private const float HorizonAtShort = 0.36f;
        private const float FarHalfWidthDp = 4f;
        private const float NearWidthShare = 0.42f;
        private const float CuiaWidthShare = 0.40f;
        private const float CuiaMaxWidthDp = 152f;

        private static readonly Color WaterTop = Palette.Hex(0x9C7447);
        private static readonly Color WaterMid = Palette.Hex(0xB08A5A);
        private static readonly Color WaterLow = Palette.Hex(0xB48E5E);
        private static readonly Color WaterLight = Palette.Hex(0xD8BA8A);
        private static readonly Color WaterWave = Palette.Hex(0xE6CB9E);
        private static readonly Color AcaiTop = Palette.Hex(0x4E1F6E);
        private static readonly Color AcaiMid = Palette.Hex(0x6A2C91);
        private static readonly Color AcaiLow = Palette.Hex(0x7E3AAE);
        private static readonly Color AcaiRim = Palette.Hex(0x8A4FA8);
        private static readonly Color AcaiLight = Palette.Hex(0xA866DA);
        private static readonly Color AcaiWave = Palette.Hex(0xCDA0F0);
        private static readonly Color Sand = Palette.Hex(0xE6C88E);
        private static readonly Color SandWet = Palette.Hex(0xB89560);
        private static readonly Color BankDark = Palette.Hex(0x0A3428);
        private static readonly Color LandTop = Palette.Hex(0x2A6658);
        private static readonly Color BankFar = Palette.Hex(0x2C6A52);
        private static readonly Color BankNear = Palette.Hex(0x1F6438);
        private static readonly Color FringeFar = Palette.Hex(0x3A7C62);
        private static readonly Color FringeNear = Palette.Hex(0x2A7A3E);
        private static readonly Color FringeLightFar = Palette.Hex(0x5E9C82);
        private static readonly Color FringeLightNear = Palette.Hex(0x5DB24E);
        private static readonly Color LandLow = Palette.Hex(0x0B3326);
        private static readonly Color PalmColor = Palette.Hex(0x0E4232);

        // Copas de la selva, de lejos (la bruma del horizonte) a cerca: sombra, copa (dos tonos) y luz.
        private static readonly Color CrownShadowFar = Palette.Hex(0x1B5248);
        private static readonly Color CrownShadowNear = Palette.Hex(0x082A1E);
        private static readonly Color CrownBodyFar = Palette.Hex(0x2F7262);
        private static readonly Color CrownBodyNear = Palette.Hex(0x17603A);
        private static readonly Color CrownBodyAltFar = Palette.Hex(0x3A7C66);
        private static readonly Color CrownBodyAltNear = Palette.Hex(0x226E36);
        private static readonly Color CrownLightFar = Palette.Hex(0x5C9A84);
        private static readonly Color CrownLightNear = Palette.Hex(0x5DB24E);

        // La isla: su selva y sus copas, del verde de cerca.
        private static readonly Color[] CrownShadow = { CrownShadowNear, CrownShadowNear, CrownShadowNear };
        private static readonly Color[] CrownBody = { CrownBodyNear, CrownBodyNear, CrownBodyNear };
        private static readonly Color[] CrownLight = { CrownLightNear, CrownLightNear, CrownLightNear };

        private readonly float[] _x = new float[Samples];
        private readonly float[] _y = new float[Samples];
        private readonly float[] _half = new float[Samples];
        private readonly float[] _nx = new float[Samples];
        private readonly float[] _ny = new float[Samples];
        private readonly float[] _beachL = new float[Samples];
        private readonly float[] _beachR = new float[Samples];
        private readonly float[] _edge = new float[Samples];
        private readonly float[] _arc = new float[Samples];
        private readonly float[] _curv = new float[Samples];
        private readonly int[] _chunkStart = new int[ChunkCount + 1];
        private readonly int _impactIndex;

        /// <summary>Compone la escena para un lienzo de <paramref name="width"/> × <paramref name="height"/> dp.</summary>
        public BrandRiverScene(float width, float height)
        {
            Width = width;
            Height = height;
            ShortScreen = height < 2f * width;
            HorizonY = Mathf.Round(height * (ShortScreen ? HorizonAtShort : HorizonAt));
            BottomY = height + 24f;
            BuildAxis();
            _impactIndex = IndexAtV(ImpactV);
            Impact = new Vector2(_x[_impactIndex], _y[_impactIndex]);

            // La cuia: el impacto del icono cae en el del río.
            CuiaScale = Mathf.Min(width * CuiaWidthShare, CuiaMaxWidthDp) / 440f;
            CuiaTx = Impact.x - (BrandEmblem.ImpactX * CuiaScale);
            CuiaTy = Impact.y - (BrandEmblem.ImpactY * CuiaScale);

            // Los tramos del açaí, de igual largo a lo largo del cauce, del impacto al final.
            float a0 = _arc[_impactIndex];
            float a1 = _arc[Samples - 1];
            int i = _impactIndex;
            _chunkStart[0] = _impactIndex;
            for (int k = 1; k < ChunkCount; k++)
            {
                float target = a0 + ((a1 - a0) * k / ChunkCount);
                while (i < Samples - 1 && _arc[i] < target)
                {
                    i++;
                }

                _chunkStart[k] = i;
            }

            _chunkStart[ChunkCount] = Samples - 1;
        }

        /// <summary>Ancho del lienzo (dp).</summary>
        public float Width { get; }

        /// <summary>Alto del lienzo (dp).</summary>
        public float Height { get; }

        /// <summary>Pantalla baja (16:9): el horizonte baja para que TiTino quepa encima.</summary>
        public bool ShortScreen { get; }

        /// <summary>Línea del horizonte (dp desde arriba): ahí nace el río, fino, entre la selva.</summary>
        public float HorizonY { get; }

        /// <summary>Hasta dónde llega el río por abajo (dp; por debajo del borde de la pantalla).</summary>
        public float BottomY { get; }

        /// <summary>Punto del río donde cae el chorro (dp).</summary>
        public Vector2 Impact { get; }

        /// <summary>dp por unidad del icono para la cuia, el chorro y los aros.</summary>
        public float CuiaScale { get; }

        /// <summary>x (dp) de la unidad 0 del icono.</summary>
        public float CuiaTx { get; }

        /// <summary>y (dp) de la unidad 0 del icono.</summary>
        public float CuiaTy { get; }

        /// <summary>Lo más alto de la selva del horizonte (dp desde arriba).</summary>
        public float SkylineDp => HorizonY - CrownRadius(0f) - 1f;

        /// <summary>La ventana de la base: la selva, el río blanco, las playas, las palmeras y las ondas (dp).</summary>
        public Rect BaseWindow => Rect.MinMaxRect(0f, HorizonY - 70f, Width, Height);

        /// <summary>La ventana del tramo <paramref name="k"/> del açaí (dp).</summary>
        public Rect ChunkWindow(int k)
        {
            int i0 = _chunkStart[k];
            int i1 = Mathf.Min(Samples - 1, _chunkStart[k + 1] + 2 + TailOf(k));
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = i0; i <= i1; i++)
            {
                float h = _half[i] + 2f;
                xMin = Mathf.Min(xMin, _x[i] - h);
                xMax = Mathf.Max(xMax, _x[i] + h);
                yMin = Mathf.Min(yMin, _y[i] - h);
                yMax = Mathf.Max(yMax, _y[i] + h);
            }

            return Rect.MinMaxRect(Mathf.Max(0f, Mathf.Floor(xMin)), Mathf.Floor(yMin), Mathf.Min(Width, Mathf.Ceil(xMax)), Mathf.Min(Height, Mathf.Ceil(yMax)));
        }

        /// <summary>La ventana de la isla (dp).</summary>
        public Rect IslandWindow
        {
            get
            {
                IslandFrame(out Vector2 c, out _, out _, out float len, out float thick);
                float r = len + 6f;
                return Rect.MinMaxRect(c.x - r, c.y - r, c.x + r, c.y + r);
            }
        }

        /// <summary>La ventana de una pieza del icono (<see cref="BrandEmblem.Impact"/>, Stream o Cuia), en dp.</summary>
        public Rect DataWindow(int group)
        {
            Rect b = BrandEmblem.GroupBounds(group);
            float s = CuiaScale;
            return Rect.MinMaxRect((b.xMin * s) + CuiaTx - 2f, (b.yMin * s) + CuiaTy - 2f, (b.xMax * s) + CuiaTx + 2f, (b.yMax * s) + CuiaTy + 2f);
        }

        /// <summary>Rasteriza la base a <paramref name="pxPerDp"/> px por dp.</summary>
        public Texture2D RenderBase(float pxPerDp)
        {
            Rect win = BaseWindow;
            var canvas = new VectorCanvas(win, pxPerDp);

            // La selva: el suelo (de la bruma del horizonte al verde hondo de cerca), con matas sueltas y poco contraste; el perfil
            // del horizonte, con las palmeras de açaí del icono detrás.
            canvas.FillBand(HorizonY - 1f, Height, VectorCanvas.Paint.Vertical(LandTop, LandLow, HorizonY, Height));
            DrawPalm(canvas, BrandEmblem.PalmA, Width * 0.09f, HorizonY + 3f, 56f);
            DrawPalm(canvas, BrandEmblem.PalmB, Width * 0.27f, HorizonY + 3f, 42f);
            DrawPalm(canvas, BrandEmblem.PalmA, Width * 0.95f, HorizonY + 3f, 34f);
            DrawLand(canvas);

            // El cauce, como en la propuesta A del icono: la selva de la orilla (una franja verde con su borde de copas), la línea
            // oscura de la orilla, las playas y el agua blanca con su luz y sus ondas.
            var poly = new PolyBuilder();
            AddBand(poly, 0, Samples - 1, i => _half[i] + _beachL[i] + _edge[i] + Bank(i), i => _half[i] + _beachR[i] + _edge[i] + Bank(i));
            poly.Fill(canvas, VectorCanvas.Paint.Vertical(BankFar, BankNear, HorizonY, Height));
            DrawBankFringe(canvas);
            AddBand(poly, 0, Samples - 1, i => _half[i] + _beachL[i] + _edge[i], i => _half[i] + _beachR[i] + _edge[i]);
            poly.Fill(canvas, VectorCanvas.Paint.Solid(BankDark));
            AddBand(poly, 0, Samples - 1, i => _half[i] + _beachL[i], i => _half[i] + _beachR[i]);
            poly.Fill(canvas, VectorCanvas.Paint.Vertical(SandWet, Sand, HorizonY, Height));
            AddBand(poly, 0, Samples - 1, i => _half[i], i => _half[i]);
            poly.Fill(canvas, new VectorCanvas.Paint(WaterTop, WaterMid, WaterLow, 0f, 0.35f, 1f, HorizonY, Height, 1f));
            AddBand(poly, 0, Samples - 1, i => _half[i] * 0.30f, i => _half[i] * 0.06f);
            poly.Fill(canvas, VectorCanvas.Paint.Solid(WaterLight, 0.30f));
            AddWaves(poly, 0.06f, 1.0f, 0.9f, 11);
            poly.Fill(canvas, VectorCanvas.Paint.Solid(WaterWave, 0.75f));
            return canvas.ToTexture("AF_RiverBase");
        }

        /// <summary>Rasteriza el tramo <paramref name="k"/> del açaí a <paramref name="pxPerDp"/>.</summary>
        public Texture2D RenderChunk(int k, float pxPerDp)
        {
            Rect win = ChunkWindow(k);
            var canvas = new VectorCanvas(win, pxPerDp);
            int i0 = _chunkStart[k];
            int i1 = Mathf.Min(Samples - 1, _chunkStart[k + 1] + 2); // se solapan un poco: sin costuras
            var poly = new PolyBuilder();
            var acai = new VectorCanvas.Paint(AcaiTop, AcaiMid, AcaiLow, 0f, 0.25f, 1f, Impact.y, Height, 1f);
            AddBand(poly, i0, i1, i => _half[i] * Ramp(i, 0.035f), i => _half[i] * Ramp(i, 0.035f));
            poly.Fill(canvas, VectorCanvas.Paint.Solid(AcaiRim, 0.45f));
            AddBand(poly, i0, i1, i => _half[i] * 0.975f * Ramp(i, 0.035f), i => _half[i] * 0.975f * Ramp(i, 0.035f));
            poly.Fill(canvas, acai);

            // El frente fundido: el açaí sigue río abajo dentro del tramo siguiente y se desvanece (de arriba abajo, como baja el río).
            int tail = TailOf(k);
            if (tail > 0)
            {
                int b = Mathf.Min(Samples - 1, i1 + tail);
                Color solid = AcaiMid;
                var fade = new VectorCanvas.Paint(solid, new Color(solid.r, solid.g, solid.b, 0.45f), new Color(solid.r, solid.g, solid.b, 0f),
                    0f, 0.45f, 1f, _y[i1 - 1], _y[b], 1f);
                AddBand(poly, i1 - 1, b, i => _half[i] * 0.975f, i => _half[i] * 0.975f);
                poly.Fill(canvas, fade);
            }

            AddBand(poly, i0, i1, i => _half[i] * 0.22f * Ramp(i, 0.10f), i => _half[i] * 0.02f * Ramp(i, 0.10f));
            poly.Fill(canvas, VectorCanvas.Paint.Solid(AcaiLight, 0.28f));
            AddWaves(poly, VOf(i0) + 0.02f, VOf(_chunkStart[k + 1]), 0.75f, 37 + k);
            poly.Fill(canvas, VectorCanvas.Paint.Solid(AcaiWave, 0.75f));
            return canvas.ToTexture("AF_RiverAcai" + k);
        }

        /// <summary>Rasteriza la isla (arena, selva y dos copas), por encima del açaí.</summary>
        public Texture2D RenderIsland(float pxPerDp)
        {
            var canvas = new VectorCanvas(IslandWindow, pxPerDp);
            IslandFrame(out Vector2 c, out Vector2 t, out Vector2 n, out float len, out float thick);
            var poly = new PolyBuilder();
            AddLens(poly, c, t, n, len + 1.5f, thick + 1.5f);
            poly.Fill(canvas, VectorCanvas.Paint.Solid(BankDark, 0.55f));
            AddLens(poly, c, t, n, len, thick);
            poly.Fill(canvas, VectorCanvas.Paint.Vertical(Sand, SandWet, c.y - thick, c.y + thick));
            AddLens(poly, c - (n * (thick * 0.18f)), t, n, len * 0.72f, thick * 0.62f);
            poly.Fill(canvas, VectorCanvas.Paint.Solid(CrownBody[2]));
            float r = thick * 0.75f;
            for (int k = 0; k < 3; k++)
            {
                Vector2 p = c + (t * (len * (-0.35f + (0.32f * k)))) - (n * (thick * 0.35f)) - new Vector2(0f, r * 0.35f);
                float rr = r * (k == 1 ? 1.15f : 0.85f);
                poly.Circle(p.x, p.y + (rr * 0.2f), rr);
                poly.Fill(canvas, VectorCanvas.Paint.Solid(CrownShadow[2]));
                poly.Circle(p.x, p.y, rr);
                poly.Fill(canvas, VectorCanvas.Paint.Solid(CrownBody[2]));
                poly.Circle(p.x - (rr * 0.28f), p.y - (rr * 0.3f), rr * 0.5f);
                poly.Fill(canvas, VectorCanvas.Paint.Solid(CrownLight[2], 0.5f));
            }

            return canvas.ToTexture("AF_RiverIsland");
        }

        /// <summary>Rasteriza una pieza del icono (impacto, chorro o cuia) en su sitio sobre el río.</summary>
        public Texture2D RenderData(int group, float pxPerDp)
        {
            return BrandEmblem.Render(group, DataWindow(group), pxPerDp, CuiaScale, CuiaTx, CuiaTy, "AF_Emblem" + group);
        }

        // ------------------------------------------------------------------------------------------------- el eje y sus orillas

        private void BuildAxis()
        {
            int segments = CtrlU.Length - 1;
            for (int i = 0; i < Samples; i++)
            {
                float g = i / (float)(Samples - 1) * segments;
                int seg = Mathf.Min(segments - 1, (int)g);
                float t = g - seg;
                float u = CatmullRom(CtrlU, seg, t);
                float v = CatmullRom(CtrlV, seg, t);
                _x[i] = u * Width;
                _y[i] = YOfV(v);
                float vv = Mathf.Clamp01(v);
                _half[i] = FarHalfWidthDp + (((NearWidthShare * Width) - FarHalfWidthDp) * Mathf.Pow(vv, 1.15f));
                _edge[i] = 0.8f + (2.6f * vv);
            }

            _arc[0] = 0f;
            for (int i = 0; i < Samples; i++)
            {
                int a = Mathf.Max(0, i - 1);
                int b = Mathf.Min(Samples - 1, i + 1);
                float tx = _x[b] - _x[a];
                float ty = _y[b] - _y[a];
                float l = Mathf.Max(1e-4f, Mathf.Sqrt((tx * tx) + (ty * ty)));
                _nx[i] = -ty / l; // a la izquierda de la corriente (que baja)
                _ny[i] = tx / l;
                if (i > 0)
                {
                    _arc[i] = _arc[i - 1] + Vector2.Distance(new Vector2(_x[i - 1], _y[i - 1]), new Vector2(_x[i], _y[i]));
                }
            }

            // Las playas, en el interior de las curvas: tanto más anchas cuanto más cerrada la curva respecto del ancho del río.
            var raw = new float[Samples];
            for (int i = 1; i < Samples - 1; i++)
            {
                // Curvatura con signo: lo que gira la tangente T = (ny, −nx) hacia la normal n (la izquierda de la corriente) por
                // unidad de largo. Positiva: la curva se cierra hacia la izquierda, y ahí, en su interior, va la playa.
                float ds = Mathf.Max(1e-3f, _arc[i + 1] - _arc[i - 1]);
                float dTx = _ny[i + 1] - _ny[i - 1];
                float dTy = -_nx[i + 1] + _nx[i - 1];
                raw[i] = ((dTx * _nx[i]) + (dTy * _ny[i])) / ds;
            }

            for (int i = 0; i < Samples; i++)
            {
                float sum = 0f;
                int n = 0;
                for (int j = i - 24; j <= i + 24; j++)
                {
                    if (j >= 0 && j < Samples)
                    {
                        sum += raw[j];
                        n++;
                    }
                }

                float k = sum / n;
                float strength = Mathf.Clamp01(Mathf.Abs(k) * _half[i] * 3.0f);
                float beach = _half[i] * 0.24f * Mathf.SmoothStep(0.15f, 0.95f, strength);
                float radius = 1f / Mathf.Max(1e-4f, Mathf.Abs(k));
                beach = Mathf.Min(beach, Mathf.Max(0f, (radius - _half[i]) * 0.45f));
                _beachL[i] = k > 0f ? beach : 0f;
                _beachR[i] = k < 0f ? beach : 0f;
                _curv[i] = k;
            }
        }

        private static float CatmullRom(float[] p, int seg, float t)
        {
            float p0 = p[Mathf.Max(0, seg - 1)];
            float p1 = p[seg];
            float p2 = p[seg + 1];
            float p3 = p[Mathf.Min(p.Length - 1, seg + 2)];
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1) + ((-p0 + p2) * t) + (((2f * p0) - (5f * p1) + (4f * p2) - p3) * t2) + ((-p0 + (3f * p1) - (3f * p2) + p3) * t3));
        }

        // v (a lo largo del cauce) → y (dp): lo lejano, más apretado (perspectiva).
        private float YOfV(float v)
        {
            float a = Mathf.Pow(Mathf.Abs(v), Perspective);
            return HorizonY + ((BottomY - HorizonY) * (v < 0f ? -a : a));
        }

        private float VOf(int i)
        {
            float t = Mathf.Max(0f, (_y[i] - HorizonY) / (BottomY - HorizonY));
            return Mathf.Pow(t, 1f / Perspective);
        }

        private int IndexAtV(float v)
        {
            float y = YOfV(v);
            int best = 0;
            for (int i = 0; i < Samples; i++)
            {
                if (_y[i] <= y)
                {
                    best = i;
                }
            }

            return best;
        }

        // Muestras del frente fundido del tramo k (dentro del tramo siguiente); 0 en el último.
        private int TailOf(int k)
        {
            if (k >= ChunkCount - 1)
            {
                return 0;
            }

            return Mathf.Max(2, (_chunkStart[k + 2 > ChunkCount ? ChunkCount : k + 2] - _chunkStart[k + 1]) / 2);
        }

        // El açaí se abre desde el impacto: 0 en él y 1 a <paramref name="reach"/> de v río abajo.
        private float Ramp(int i, float reach)
        {
            float v = VOf(i) - ImpactV;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v + 0.004f) / reach));
        }

        private delegate float Offset(int i);

        // Una orilla en el interior de la curva se acerca, sin llegar, al 90 % de su radio (si no, el borde se cruza y hace picos):
        // hasta el 60 % queda igual y de ahí en adelante se frena con suavidad, sin quiebros.
        private float InsideBend(int i, float offset, float side)
        {
            float k = _curv[i] * side;
            if (k <= 1e-4f)
            {
                return offset;
            }

            float radius = 1f / k;
            float knee = 0.6f * radius;
            if (offset <= knee)
            {
                return offset;
            }

            float room = 0.3f * radius;
            float extra = offset - knee;
            return knee + (room * extra / (room + extra));
        }

        // Banda del cauce entre las muestras i0 e i1: a la izquierda de la corriente left(i) y a la derecha right(i).
        private void AddBand(PolyBuilder poly, int i0, int i1, Offset left, Offset right)
        {
            poly.Begin();
            for (int i = i0; i <= i1; i++)
            {
                float l = InsideBend(i, left(i), 1f);
                poly.Point(_x[i] + (_nx[i] * l), _y[i] + (_ny[i] * l));
            }

            for (int i = i1; i >= i0; i--)
            {
                float r = InsideBend(i, right(i), -1f);
                poly.Point(_x[i] - (_nx[i] * r), _y[i] - (_ny[i] * r));
            }

            poly.End();
        }

        // Ondas cortas (dos jorobas) entre v0 y v1, repartidas a lo ancho del agua, de tamaño según la profundidad.
        private void AddWaves(PolyBuilder poly, float v0, float v1, float spread, int seed)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt((v1 - v0) * 14f));
            for (int k = 0; k < count; k++)
            {
                float v = v0 + ((v1 - v0) * ((k + 0.5f + ((ArtNoise.Hash(k, 1, seed) - 0.5f) * 0.6f)) / count));
                int i = IndexAtV(Mathf.Clamp(v, 0f, 1f));
                float across = (ArtNoise.Hash(k, 2, seed) - 0.5f) * spread;
                float cx = _x[i] + (_nx[i] * _half[i] * across);
                float cy = _y[i] + (_ny[i] * _half[i] * across);
                float len = Mathf.Max(5f, _half[i] * 0.42f);
                float amp = len * 0.13f;
                float width = Mathf.Max(1.1f, len * 0.07f);
                poly.Squiggle(cx - (len * 0.5f), cy, len, amp, width, ArtNoise.Hash(k, 3, seed) < 0.4f ? 1 : 2);
            }
        }

        private void IslandFrame(out Vector2 c, out Vector2 t, out Vector2 n, out float len, out float thick)
        {
            int i = IndexAtV(0.66f);
            n = new Vector2(_nx[i], _ny[i]);
            t = new Vector2(_ny[i], -_nx[i]);
            if (t.y < 0f)
            {
                t = -t;
            }

            c = new Vector2(_x[i], _y[i]) + (n * (_half[i] * 0.18f));
            len = _half[i] * 0.62f;
            thick = _half[i] * 0.17f;
        }

        // Lente de arena (o de selva) de semieje len a lo largo de t y thick a lo ancho.
        private static void AddLens(PolyBuilder poly, Vector2 c, Vector2 t, Vector2 n, float len, float thick)
        {
            poly.Begin();
            const int m = 40;
            for (int k = 0; k < m; k++)
            {
                float a = 2f * Mathf.PI * k / m;
                float along = Mathf.Cos(a);
                float sn = Mathf.Sin(a);
                float side = Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 0.75f) * (1f - (0.25f * along));
                Vector2 p = c + (t * (along * len)) + (n * (side * thick));
                poly.Point(p.x, p.y);
            }

            poly.End();
        }

        private void DrawPalm(VectorCanvas canvas, int group, float x, float yFoot, float heightDp)
        {
            Rect b = BrandEmblem.GroupBounds(group);
            float s = heightDp / Mathf.Max(1f, b.height);
            float tx = x - (b.center.x * s);
            float ty = yFoot - (b.yMax * s);
            BrandEmblem.Draw(canvas, group, s, tx, ty, PalmColor);
        }

        private static float CrownRadius(float f)
        {
            return Mathf.Lerp(5f, 40f, Mathf.Pow(Mathf.Clamp01(f), 1.1f));
        }

        // El suelo de la selva: el perfil de copas del horizonte (en la bruma) y matas sueltas, grandes y de poco contraste, cada vez
        // mayores hacia el primer plano, fuera del cauce.
        private void DrawLand(VectorCanvas canvas)
        {
            var shadow = new PolyBuilder();
            var body = new PolyBuilder();
            var light = new PolyBuilder();

            // El horizonte: una fila de matas que sobresale de la línea.
            float x = -6f;
            int col = 0;
            while (x < Width + 8f)
            {
                float r = 6f + (4f * ArtNoise.Hash(col, 0, 61));
                Mound(body, x, HorizonY + 1f, r);
                x += r * (1.2f + (0.5f * ArtNoise.Hash(col, 1, 61)));
                col++;
            }

            body.Fill(canvas, VectorCanvas.Paint.Solid(CrownBodyFar));

            float span = Height + 40f - HorizonY;
            float y = HorizonY + 14f;
            int row = 0;
            while (y < Height + 40f)
            {
                float f = Mathf.Clamp01((y - HorizonY) / span);
                float r = CrownRadius(f);
                int i = NearestByY(y);
                x = -r * ArtNoise.Hash(row, 0, 71) * 2f;
                col = 0;
                while (x < Width + r)
                {
                    float rr = r * (0.75f + (0.5f * ArtNoise.Hash(row, col, 79)));
                    float dx = x - _x[i];
                    bool leftOfFlow = (dx * _nx[i]) > 0f;
                    float room = (_half[i] + (leftOfFlow ? _beachL[i] : _beachR[i]) + Bank(i)) / Mathf.Max(0.35f, Mathf.Abs(_nx[i]));
                    if (Mathf.Abs(dx) > room + (rr * 0.5f) && ArtNoise.Hash(row, col, 97) < 0.78f)
                    {
                        float cy = y + ((ArtNoise.Hash(row, col, 73) - 0.5f) * r);
                        Mound(shadow, x + (rr * 0.1f), cy + (rr * 0.25f), rr);
                        Mound(body, x, cy, rr);
                        light.Circle(x - (rr * 0.3f), cy - (rr * 0.32f), rr * 0.36f);
                    }

                    x += rr * (1.45f + (0.9f * ArtNoise.Hash(row, col, 83)));
                    col++;
                }

                shadow.Fill(canvas, VectorCanvas.Paint.Solid(Color.Lerp(CrownShadowFar, CrownShadowNear, f), 0.55f));
                body.Fill(canvas, VectorCanvas.Paint.Solid(Color.Lerp(CrownBodyFar, CrownBodyNear, f), 0.75f));
                light.Fill(canvas, VectorCanvas.Paint.Solid(Color.Lerp(CrownLightFar, CrownLightNear, f), 0.18f));
                y += r * 1.05f;
                row++;
            }
        }

        // El borde de la selva de la orilla: matas a lo largo de las dos orillas, por fuera de la franja verde, de tamaño según la
        // profundidad, con su luz arriba.
        private void DrawBankFringe(VectorCanvas canvas)
        {
            var body = new PolyBuilder();
            var light = new PolyBuilder();
            int bands = 4;
            for (int band = 0; band < bands; band++)
            {
                float f0 = band / (float)bands;
                float f1 = (band + 1) / (float)bands;
                for (int side = -1; side <= 1; side += 2)
                {
                    float next = 0f;
                    for (int i = 0; i < Samples; i++)
                    {
                        float f = VOf(i);
                        if (f < f0 || f >= f1 || _arc[i] < next)
                        {
                            continue;
                        }

                        float off = _half[i] + (side > 0 ? _beachL[i] : _beachR[i]) + _edge[i] + Bank(i);
                        float r = Mathf.Max(2.5f, Bank(i) * 0.55f) * (0.8f + (0.4f * ArtNoise.Hash(i, side + 2, 101)));
                        float px = _x[i] + (_nx[i] * side * (off - (r * 0.2f)));
                        float py = _y[i] + (_ny[i] * side * (off - (r * 0.2f)));
                        Mound(body, px, py, r);
                        light.Circle(px - (r * 0.3f), py - (r * 0.35f), r * 0.38f);
                        next = _arc[i] + (r * (1.1f + (0.5f * ArtNoise.Hash(i, side + 5, 103))));
                    }
                }

                float fm = (f0 + f1) * 0.5f;
                body.Fill(canvas, VectorCanvas.Paint.Solid(Color.Lerp(FringeFar, FringeNear, fm)));
                light.Fill(canvas, VectorCanvas.Paint.Solid(Color.Lerp(FringeLightFar, FringeLightNear, fm), 0.35f));
            }
        }

        // La franja de selva de la orilla (dp): algo más ancha cerca.
        private float Bank(int i)
        {
            return 2.5f + (_half[i] * 0.28f);
        }

        // Una mata: un círculo grande y dos menores a los lados, algo más bajos.
        private static void Mound(PolyBuilder poly, float x, float y, float r)
        {
            poly.Circle(x, y, r * 0.82f);
            poly.Circle(x - (r * 0.48f), y + (r * 0.18f), r * 0.6f);
            poly.Circle(x + (r * 0.5f), y + (r * 0.14f), r * 0.64f);
        }

        private int NearestByY(float y)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < Samples; i++)
            {
                float d = Mathf.Abs(_y[i] - y);
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Acumula contornos (dp) y los rellena de una vez en un <see cref="VectorCanvas"/>.</summary>
        private sealed class PolyBuilder
        {
            private readonly List<float> _pts = new List<float>();
            private readonly List<int> _ends = new List<int>();

            public void Begin()
            {
            }

            public void Point(float x, float y)
            {
                _pts.Add(x);
                _pts.Add(y);
            }

            public void End()
            {
                _ends.Add(_pts.Count / 2);
            }

            public void Circle(float cx, float cy, float r)
            {
                int n = Mathf.Clamp(Mathf.RoundToInt(r * 1.6f), 10, 28);
                for (int k = 0; k < n; k++)
                {
                    float a = 2f * Mathf.PI * k / n;
                    Point(cx + (r * Mathf.Cos(a)), cy + (r * Mathf.Sin(a)));
                }

                End();
            }

            // Onda de jorobas: un trazo de ancho w por y = cy − amp · |sen| a lo largo de len, como las del icono.
            public void Squiggle(float x0, float cy, float len, float amp, float w, int humps)
            {
                const int m = 18;
                var top = new Vector2[m + 1];
                for (int k = 0; k <= m; k++)
                {
                    float t = k / (float)m;
                    top[k] = new Vector2(x0 + (t * len), cy - (amp * Mathf.Sin(t * Mathf.PI * humps)));
                }

                for (int k = 0; k <= m; k++)
                {
                    Vector2 d = top[Mathf.Min(m, k + 1)] - top[Mathf.Max(0, k - 1)];
                    var nrm = new Vector2(-d.y, d.x).normalized * (w * 0.5f);
                    Point(top[k].x + nrm.x, top[k].y + nrm.y);
                }

                for (int k = m; k >= 0; k--)
                {
                    Vector2 d = top[Mathf.Min(m, k + 1)] - top[Mathf.Max(0, k - 1)];
                    var nrm = new Vector2(-d.y, d.x).normalized * (w * 0.5f);
                    Point(top[k].x - nrm.x, top[k].y - nrm.y);
                }

                End();
            }

            public void Fill(VectorCanvas canvas, in VectorCanvas.Paint paint)
            {
                if (_ends.Count > 0)
                {
                    canvas.Fill(_pts.ToArray(), _ends.ToArray(), 0, _ends.Count, 1f, 0f, 0f, paint);
                }

                _pts.Clear();
                _ends.Clear();
            }
        }
    }
}
