// Líquido de la cuia para uGUI (GDD 08 §5.2; docs/art/direccion-visual-v2.md 4.2): degradado por dosis (±4 % de L),
// oscurecido lateral, franja especular, franja secundaria y brillo vertical como color opaco precalculado (un brillo por
// punto, 2.5), patrón de accesibilidad por tinte (07 §3), menisco con línea clara y curvatura cóncava, onda tras verter
// (A0 = 0,03 u, exp(-t / 0,25 s), lambda 1,30 u, fase 2,5 u/s) y destello del sellado (+15 %).
//
// uGUI no admite MaterialPropertyBlock en un CanvasRenderer: los datos por cuia y por dosis llegan por vértice
// (LiquidGraphic.cs) y todas las cuias comparten un material, así que van en un solo lote. Solo canales UV: el Canvas
// gira NORMAL y TANGENT con el objeto (la cuia se inclina al verter), así que no sirven para datos. Sin palabras clave
// (shader_feature) que el build pudiera eliminar. Coste: 1 muestra (patrón) y unas 57 ALU por píxel (el recorte a la
// silueta añade unas 12; por encima de las 40 de la guía 8: pendiente de medir en un dispositivo de clase A).
//
// Canales (en u de la cuia, x de 0 a 2 con el centro en 1; y desde el pie):
//   COLOR      color de la dosis (el Canvas lo pasa a lineal)
//   TEXCOORD0  x, y local (vidrio: brillos y recorte), y de capa (dosis sin inclinar: degradado, patrón, menisco y
//              superficie), alto de la cuia. En reposo las dos y coinciden.
//   TEXCOORD1  rgb: color de la franja principal (ya en el espacio del shader); a: destello del sellado
//   TEXCOORD2  superficie del líquido, amplitud de la onda, fase de la onda, patrón + 4 · índice de la dosis (patrón: -1
//              sin patrón, 0 marcas en tinta, 1 marcas en marfil)
//   TEXCOORD3  rgb: color del menisco (espacio del shader); a: fila del patrón en el atlas
//   _DpU       1 dp en u (todas las cuias de un nivel miden lo mismo); _PatternRows: filas del atlas
//
// Con la cuia inclinada (al verter) las capas siguen horizontales y llenan la cavidad girada desde su punto más bajo
// (LiquidGraphic.cs); aquí se recortan a la silueta interior (cavidad recta, hombro y cuello) con antialias.
Shader "AF/UI/Liquid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Pattern Atlas", 2D) = "white" {}
        _DpU ("Un dp en u", Float) = 0.0417
        _PatternRows ("Filas del atlas", Float) = 15
        _InkLin ("Tinta en lineal", Color) = (0.024, 0.015, 0.009, 1)
        _PaperLin ("Marfil en lineal", Color) = (0.905, 0.831, 0.693, 1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "AFLiquid"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 uv3 : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                half4 lit : TEXCOORD1;
                float4 surf : TEXCOORD2;
                half4 men : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float _DpU;
            float _PatternRows;
            half4 _InkLin;
            half4 _PaperLin;
            int _UIVertexColorAlwaysGammaSpace;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                half4 c = v.color;
                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                {
                    c.rgb = UIGammaToLinear(c.rgb);
                }

                o.color = c;
                o.uv0 = v.uv0;
                o.lit = v.uv1;
                o.surf = v.uv2;
                o.men = v.uv3;
                return o;
            }

            float segDist(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - (ba * h));
            }

            half4 frag(v2f i) : SV_Target
            {
                float x = i.uv0.x;
                float yl = i.uv0.y;  // y local (vidrio)
                float y = i.uv0.z;   // y de capa (dosis)
                float heightU = i.uv0.w;
                float dpU = _DpU;
                float k = floor((i.surf.w + 1.5) / 4.0);
                float patternMode = i.surf.w - (4.0 * k);
                float doseBottom = 0.6 + k;

                // Recorte a la silueta interior: cavidad de 1,30 u, hombro de 0,60 u hasta 0,80 u y cuello; entre el fondo
                // (0,60 u) y el labio (alto - 0,25 u). En reposo la malla ya está dentro y el recorte no cambia nada.
                float cavityTop = heightU - 1.40;
                float sh = saturate((yl - cavityTop) / 0.60);
                float innerHw = lerp(0.65, 0.40, sh * sh * (3.0 - (2.0 * sh)));
                float pixX = max(fwidth(x), 1e-4);
                float pixY = max(fwidth(yl), 1e-4);
                float clip = saturate(((innerHw - abs(x - 1.0)) / pixX) + 1.0)
                    * saturate(((yl - 0.6) / pixY) + 1.0)
                    * saturate((((heightU - 0.25) - yl) / pixY) + 1.0);

                // Superficie: onda tras verter y curvatura cóncava junto a las paredes (guía 4.2).
                float wall = 0.65 - abs(x - 1.0);
                float wave = i.surf.y * sin(((x - 1.0) * 4.8332) - (_Time.y * 12.083) + i.surf.z);
                float top = i.surf.x + wave + (0.04 * saturate(1.0 - (wall / 0.12)));
                float pix = max(fwidth(y), 1e-4);
                float alpha = saturate(((top - y) / pix) + 0.5);

                // Degradado vertical por dosis: ±4 % de L (OKLab) es ±12 % en lineal; el centro de la dosis, exacto.
                float t = saturate(y - doseBottom);
                half3 col = i.color.rgb * (1.0 + (0.24 * (t - 0.5)));

                // Oscurecido lateral: 0,20 u por pared, negro al 20 % (07 §4.2).
                col *= 1.0 - (0.20 * saturate(1.0 - (wall / 0.20)));

                // Brillos: franja principal de (0,25; alto - 1) a (0,55; 1), gaussiana sigma 0,045 u; secundaria +0,16 u
                // en x (sigma 0,0125); brillo vertical en x 0,76-0,88. Un brillo por punto (no se solapan).
                float2 p = float2(x, yl);
                float2 a = float2(0.25, heightU - 1.0);
                float2 b = float2(0.55, 1.0);
                float d1 = segDist(p, a, b);
                float d2 = segDist(p, a + float2(0.16, 0.0), b + float2(0.16, 0.0));
                float hlMain = exp(-(d1 * d1) / 0.00405) * step(d1, 0.09);
                float hlSec = exp(-(d2 * d2) / 0.0003125) * step(d2, 0.025);
                float hlVert = smoothstep(0.74, 0.78, x) * (1.0 - smoothstep(0.86, 0.90, x));
                float hl = max(hlMain, max(hlSec * 0.5, hlVert * 0.5));
                col = lerp(col, i.lit.rgb, hl);

                // Patrón del tinte (07 §3): marcas al 15 % hacia tinta o marfil, mezcladas en un espacio aproximadamente
                // gamma (raíz cuadrada) como Palette.PatternMark, encima de los brillos (VA-05b).
                if (patternMode > -0.5)
                {
                    float rowH = 1.0 / _PatternRows;
                    float2 puv = float2((x - 0.35) * 2.0, (i.men.a * rowH) + (clamp(y - doseBottom, 0.004, 0.996) * rowH));
                    half m = tex2D(_MainTex, puv).a;
                    half3 baseCol = lerp(_InkLin.rgb, _PaperLin.rgb, (half)patternMode);
                    half3 g = lerp(sqrt(col), sqrt(baseCol), 0.15 * m);
                    col = g * g;
                }

                // Menisco: 0,08 u (mínimo 2 dp) aclarado y una línea clara de 1 dp en el borde.
                float menH = max(0.08, 2.0 * dpU);
                float inMen = saturate(((y - (top - menH)) / pix) + 0.5);
                col = lerp(col, i.men.rgb, inMen);
                float rim = saturate(1.0 - ((top - y) / dpU));
                col = lerp(col, half3(1.0, 1.0, 1.0), 0.45 * rim * inMen);

                // Destello del sellado: +15 % de luminosidad durante 100 ms (07 §5.3).
                col += (1.0 - col) * i.lit.a;

                return half4(col, alpha * clip * i.color.a);
            }
            ENDCG
        }
    }
}
