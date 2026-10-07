// Chorro del vertido para uGUI (02 §4.1; 07 §5.2; docs/art/direccion-visual-v2.md 6.5): cinta sobre la Bézier del labio a
// la superficie, núcleo del tinte (+10 % de L, precalculado en el color de vértice) con volumen redondeado, vetas que
// corren a 3 u/s y una línea especular central de 1 dp (blanco al 40 %). Sin palabras clave; unas 20 ALU por píxel.
//
//   COLOR      color del núcleo (el Canvas lo pasa a lineal)
//   TEXCOORD0  x: posición transversal (-1 a 1); y: distancia a lo largo en u; z: medio grosor de 1 dp en unidades x
Shader "AF/UI/Stream"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
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
            Name "AFStream"
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

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
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float ax = abs(i.uv0.x);
                float aa = max(fwidth(i.uv0.x), 1e-4);
                float alpha = saturate((1.0 - ax) / aa);

                // Volumen: el chorro es un cilindro de líquido, más oscuro en los bordes.
                half3 col = i.color.rgb * (0.78 + (0.22 * sqrt(saturate(1.0 - (ax * ax)))));

                // Vetas que corren hacia abajo a 3 u/s (9 rad/u × 3 u/s).
                float flow = sin((i.uv0.y * 9.0) - (_Time.y * 27.0) + (i.uv0.x * 1.7));
                col *= 1.0 + (0.06 * flow);

                // Línea especular de 1 dp, algo desplazada hacia la luz (arriba a la izquierda, 07 §1.2).
                float lineW = max(i.uv0.z, aa);
                float spec = saturate(1.0 - (abs(i.uv0.x + 0.22) / lineW));
                col = lerp(col, half3(1.0, 1.0, 1.0), 0.40 * spec * (0.75 + (0.25 * flow)));

                return half4(col, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
