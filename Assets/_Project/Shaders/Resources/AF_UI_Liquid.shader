// Líquido de la cuia para uGUI (GDD 08 §5.2; docs/art/direccion-visual-v2.md 4.2): degradado por dosis (±4 % de L),
// oscurecido lateral, franja especular, franja secundaria y brillo vertical como color opaco precalculado (un brillo por
// punto, 2.5), patrón de accesibilidad por tinte (07 §3), cara superior en elipse (D-006, como Magic Sort: alto
// _AFTopFaceU, ancho de la cavidad, color aclarado del menisco y una línea clara al fondo), fronteras entre dosis y fondo
// del líquido en arco con la flecha del borde delantero de esa elipse (Magic Sort, d_002340: en el eje todas las dosis
// miden 1 u, también la de arriba), onda tras verter (A0 = 0,03 u, exp(-t / 0,25 s), lambda 1,30 u, fase 2,5 u/s) y
// destello del sellado (+15 %). El reloj de la onda es _Time.y salvo que ShaderClock lo congele (_AFClockFrozen = 1: capturas
// deterministas con _AFTime).
//
// uGUI no admite MaterialPropertyBlock en un CanvasRenderer: los datos por cuia y por dosis llegan por vértice
// (LiquidGraphic.cs) y todas las cuias comparten un material, así que van en un solo lote. Solo canales UV: el Canvas
// gira NORMAL y TANGENT con el objeto (la cuia se inclina al verter), así que no sirven para datos. Sin palabras clave
// (shader_feature) que el build pudiera eliminar. Coste: 1 muestra (patrón) y unas 57 ALU por píxel (el recorte a la
// silueta añade unas 12; por encima de las 40 de la guía 8: pendiente de medir en un dispositivo de clase A). Las dosis
// turbias hacen 2 muestras (la trama o, al disolverse, el patrón de la fruta que se revela; y el «?») y unas 30 ALU más;
// nunca más de 2 muestras por píxel. Sin Bloom.
//
// Canales (en u de la cuia, x de 0 a 2 con el centro en 1; y desde el pie):
//   COLOR      color de la dosis (el Canvas lo pasa a lineal)
//   TEXCOORD0  x, y local (vidrio: brillos y recorte), y de capa (dosis sin inclinar: degradado, patrón, menisco y
//              superficie), alto de la cuia. En reposo las dos y coinciden.
//   TEXCOORD1  rgb: color de la franja principal (ya en el espacio del shader); a: destello del sellado, o, en una dosis
//              turbia (nunca sellada, R5), la x de la «?» en pantalla (u de capa desde el centro de la cuerda de su banda)
//   TEXCOORD2  superficie del líquido, amplitud de la onda, fase de la onda, patrón + 4 · índice de la dosis (patrón: -1
//              sin patrón, 0 marcas en tinta, 1 marcas en marfil, 2 turbia; 2 + 0,45 · avance al disolverse la «?» por la
//              regla y 2 - 0,45 · avance al girar con la Luciérnaga)
//   TEXCOORD3  rgb: color del menisco (espacio del shader); a: fila del patrón en el atlas (la turbia en las dosis turbias
//              quietas o girando; al disolverse por la regla, la de la fruta que se revela + 0,5 si sus marcas van en marfil,
//              LiquidGraphic.FruitRowCode)
//   _DpU       1 dp en u (todas las cuias de un nivel miden lo mismo); _PatternRows: filas del atlas; _MurkyRow: la turbia
//
// Aguas turbias (07 §4.3; 08 §5.2; LiquidGraphic.MurkyLook): equivale a _HiddenMask (cada banda turbia lleva el patrón 2;
// también ocultas no contiguas) y a _MurkyColor = #8A7B6A (el color de la banda), que no pueden ir como propiedades porque
// uGUI no admite MaterialPropertyBlock y todas las cuias comparten material. La fila turbia del atlas (lineal) lleva en A la
// trama diagonal (repetible, dos teselas por u) y en R y G el «?» marfil de 0,5 u y su halo de tinta, una sola vez y
// centrado en la fila. Sobre el pardo: trama al 25 % hacia la tinta y «?» marfil con el halo de tinta al 55 %, en
// coordenadas de pantalla relativas a su banda (derechos aunque la cuia se incline, y del tamaño de la banda), sin la
// mezcla fija del 15 % de los patrones de fruta. Al disolverse por la regla, fundido cruzado sin saltos (T15): la trama se
// va, el «?» se disuelve y después entra el patrón de la fruta en sus coordenadas y con su 15 %, así que al terminar la banda
// es la dosis normal de esa fruta (LiquidGraphic.MurkyHatchAmount y FruitPatternWeight son los espejos en CPU).
//   _AF*U      geometría de la cuia (globales que fija LiquidGraphic.ApplyShaderGeometry desde CuiaGeometry, la única
//              fuente): fondo y medio ancho de la cavidad, medio ancho del cuello, hombro, alto sobre la cavidad, borde y
//              alto de la cara superior
//
// Con la cuia inclinada (al verter) las capas siguen horizontales y llenan la cavidad girada desde su punto más bajo
// (LiquidGraphic.cs); aquí se recortan a la silueta interior (cavidad recta con el fondo en arco, hombro y cuello) con
// antialias.
//
// Arcos (CuiaGeometry.FaceSagU): flecha = 0,5 · _AFTopFaceU · sqrt(1 − (X / medio ancho)²), 0,20 u en el eje. La malla de
// cada dosis baja esa flecha bajo su frontera (LiquidGraphic) y aquí se ve solo sobre su arco (y de capa + flecha ≥ fondo
// de la dosis); como cada dosis se dibuja después de la de debajo, la tapa entre la recta y el arco. La de abajo del todo
// se recorta al fondo de la cavidad en arco (y local).
Shader "AF/UI/Liquid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Pattern Atlas", 2D) = "white" {}
        _DpU ("Un dp en u", Float) = 0.0417
        _PatternRows ("Filas del atlas", Float) = 16
        _MurkyRow ("Fila turbia del atlas", Float) = 15
        // Vector y no Color: ya vienen en el espacio del shader (LiquidGraphic.Setup, SetVector); una propiedad Color se
        // volvería a pasar a lineal en el proyecto Linear.
        _InkLin ("Tinta en lineal", Vector) = (0.024, 0.015, 0.009, 1)
        _PaperLin ("Marfil en lineal", Vector) = (0.905, 0.831, 0.701, 1)
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
                float4 lit : TEXCOORD1; // float: en las dosis turbias, a es la x de la «?»
                float4 surf : TEXCOORD2;
                half4 men : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float _DpU;
            float _PatternRows;
            float _MurkyRow;
            half4 _InkLin;
            half4 _PaperLin;
            int _UIVertexColorAlwaysGammaSpace;

            // Geometría de la cuia (globales, CuiaGeometry vía LiquidGraphic.ApplyShaderGeometry; D-006).
            float _AFCavityBottomU;
            float _AFCavityHalfU;
            float _AFNeckHalfU;
            float _AFShoulderU;
            float _AFAboveCavityU;
            float _AFRimU;
            float _AFTopFaceU;

            // Reloj (ShaderClock): _Time.y en el juego; el tiempo de la intro, congelado, en las capturas deterministas.
            float _AFTime;
            float _AFClockFrozen;

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
                float doseBottom = _AFCavityBottomU + k;

                // Flecha de los arcos (CuiaGeometry.FaceSagU): medio alto de la cara superior en el eje, 0 en las paredes.
                float ex = saturate(abs(x - 1.0) / _AFCavityHalfU);
                float faceHalf = 0.5 * _AFTopFaceU * sqrt(saturate(1.0 - (ex * ex)));

                // Recorte a la silueta interior: cavidad recta, hombro hasta el cuello y cuello; entre el fondo en arco y
                // el labio (alto - borde). En reposo la malla ya está dentro y el recorte solo hace el arco del fondo.
                float cavityTop = heightU - _AFAboveCavityU;
                float sh = saturate((yl - cavityTop) / _AFShoulderU);
                float innerHw = lerp(_AFCavityHalfU, _AFNeckHalfU, sh * sh * (3.0 - (2.0 * sh)));
                float pixX = max(fwidth(x), 1e-4);
                float pixY = max(fwidth(yl), 1e-4);
                float clip = saturate(((innerHw - abs(x - 1.0)) / pixX) + 1.0)
                    * saturate(((yl - (_AFCavityBottomU - faceHalf)) / pixY) + 0.5)
                    * saturate((((heightU - _AFRimU) - yl) / pixY) + 1.0);

                // Superficie con la onda tras verter y cara superior en elipse (D-006): centrada en la superficie, del ancho
                // de la cavidad y _AFTopFaceU de alto; su mitad trasera sube sobre la superficie y la delantera es la cara.
                float wall = _AFCavityHalfU - abs(x - 1.0);
                float clockS = _AFClockFrozen > 0.5 ? _AFTime : _Time.y; // selección exacta: con lerp, _Time.y grande deja un ulp de error
                float wave = i.surf.y * sin(((x - 1.0) * 4.8332) - (clockS * 12.083) + i.surf.z);
                float top = i.surf.x + wave;
                float topEdge = top + faceHalf;
                float pix = max(fwidth(y), 1e-4);
                float alpha = saturate(((topEdge - y) / pix) + 0.5);

                // Frontera inferior en arco (como Magic Sort): la dosis k > 0 se ve desde el borde delantero de una elipse
                // igual a la de la cara superior, faceHalf bajo su nivel en el eje; tapa la de debajo entre la recta y el
                // arco. La dosis 0 la recorta el fondo en arco (clip). ya = y de capa medida desde el arco.
                float ya = y + faceHalf;
                if (k > 0.5)
                {
                    alpha *= saturate(((ya - doseBottom) / pix) + 0.5);
                }

                // Degradado vertical por dosis: ±4 % de L (OKLab) es ±12 % en lineal; el centro de la dosis, exacto (en el
                // arco, como la banda).
                float t = saturate(ya - doseBottom);
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
                // gamma (raíz cuadrada) como Palette.PatternMark, encima de los brillos (VA-05b). En una dosis turbia, la misma
                // muestra lee la trama de la fila turbia (_MurkyRow), al 25 % hacia la tinta (07 §4.3) y sin la mezcla fija del
                // 15 %. Aguas turbias: coordenadas de pantalla de su banda, en u de capa (gx: x de la «?» desde el centro de la
                // cuerda; gy: desde el centro de la banda, entre sus arcos), así que la trama y la «?» siguen a la banda
                // cuando la cuia se inclina. v > 0: revelado por la regla; v < 0: Luciérnaga.
                float rowH = 1.0 / _PatternRows;
                bool murky = patternMode > 1.5;
                float gx = i.lit.a;
                float gy = ya - doseBottom - 0.5;
                float v = (patternMode - 2.0) / 0.45;
                float dissolve = murky ? saturate(v) : 0.0;
                float turn = murky ? saturate(-v) : 0.0;
                if (patternMode > -0.5)
                {
                    // Fundido cruzado del revelado por la regla (sus espejos en CPU, LiquidGraphic.MurkyHatchAmount y
                    // FruitPatternWeight: cámbialos juntos): la trama se va entre 0,18 y 0,55 del avance y después, ya sin
                    // trama, entra el patrón de la fruta (men.a = su fila + 0,5 si sus marcas van en marfil) entre 0,64 y 1,
                    // en las coordenadas y con el 15 % del patrón normal: al terminar es el de su dosis. Las dos fases no se
                    // solapan, así que una sola muestra lee la trama o el patrón. Con la Luciérnaga, la trama se va al girar.
                    half hatch = murky ? 0.25 * (1.0 - smoothstep(0.18, 0.55, dissolve)) * (1.0 - smoothstep(0.3, 1.0, turn)) : 0.0;
                    bool fruit = !murky || hatch <= 0.0;
                    float fruitRow = murky ? floor(i.men.a + 0.25) : i.men.a;
                    half paper = murky ? (half)(2.0 * (i.men.a - fruitRow)) : (half)patternMode;
                    // Dos teselas por u (ProceduralArt.PatternTileU = 0,5 u): desde la pared izquierda de la cavidad o, la
                    // trama, desde el centro de la cuerda de la banda.
                    float pu = fruit ? (x - (1.0 - _AFCavityHalfU)) * 2.0 : gx * 2.0;
                    float row = fruit ? fruitRow : _MurkyRow;
                    float2 puv = float2(pu, (row * rowH) + (clamp(ya - doseBottom, 0.004, 0.996) * rowH));
                    half m = tex2D(_MainTex, puv).a;
                    half3 baseCol = fruit ? lerp(_InkLin.rgb, _PaperLin.rgb, paper) : _InkLin.rgb;
                    half amount = fruit ? 0.15 * (murky ? smoothstep(0.64, 1.0, dissolve) : 1.0) : hatch;
                    half3 g = lerp(sqrt(col), sqrt(baseCol), amount * m);
                    col = g * g;
                }

                // «?» marfil de 0,5 u con su halo de tinta al 55 % (07 §4.3; glosario §6.2), una vez y centrada en la banda
                // (R: marfil; G: halo). Por la regla crece, ondula y se disuelve; con la Luciérnaga gira hasta quedar de
                // canto. Segunda y última muestra (tex2Dlod: el atlas no tiene mipmaps y así puede ir dentro de la rama).
                // «fade» tiene su espejo en CPU, LiquidGraphic.MurkyMarkOpacity (MarkFadeFrom y MarkFadeTo): cámbialos juntos.
                if (murky)
                {
                    float grow = 1.0 + (0.45 * smoothstep(0.0, 1.0, dissolve));
                    float wobble = 0.05 * sin(3.14159 * dissolve) * sin((gy * 22.0) + (dissolve * 9.0));
                    float squeeze = max(cos(1.5708 * turn), 0.001);
                    float qx = (gx + wobble) / (grow * squeeze);
                    float qy = gy / grow;
                    float fade = (1.0 - smoothstep(0.35, 0.85, dissolve)) * step(turn, 0.999) * step(abs(qx), 0.25);
                    float2 quv = float2((qx * 2.0) + 0.5, (_MurkyRow * rowH) + (clamp(qy + 0.5, 0.004, 0.996) * rowH));
                    half2 q = tex2Dlod(_MainTex, float4(quv, 0.0, 0.0)).rg;
                    col = lerp(col, _InkLin.rgb, 0.55 * q.g * fade);
                    col = lerp(col, _PaperLin.rgb, q.r * fade);
                }

                // Cara superior (D-006): la elipse entera con el color aclarado del menisco, más clara que el tinte, y una
                // línea clara de 1 dp en su borde trasero.
                float inFace = saturate(((y - (top - faceHalf)) / pix) + 0.5);
                col = lerp(col, i.men.rgb, inFace);
                float rim = saturate(1.0 - ((topEdge - y) / dpU));
                col = lerp(col, half3(1.0, 1.0, 1.0), 0.35 * rim * inFace);

                // Destello del sellado: +15 % de luminosidad durante 100 ms (07 §5.3). En una dosis turbia lit.a es la x de la
                // «?» (una cuia con ocultas nunca se sella, R5).
                col += (1.0 - col) * (murky ? 0.0 : i.lit.a);

                return half4(col, alpha * clip * i.color.a);
            }
            ENDCG
        }
    }
}
