// Vidrio frontal de la cuia para uGUI (docs/art/direccion-visual-v2.md 4.1; GDD 08 §5.1: "brillo especular estático").
// El contorno, el fresnel, las franjas especulares, la sombra interior de la base y las marcas de dosis van horneados en
// la textura (ProceduralArt, a la densidad de la pantalla): este shader solo los compone y aplica _Boost, el "+10 % de
// brillo del vidrio" de la selección (02 §4.1), que Image.color no puede dar porque sus canales llegan como máximo a 1.
// Solo el origen usa el material con _Boost > 1 (D-002). Sin palabras clave ni fresnel por píxel (08 §5.4).
Shader "AF/UI/Glass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Boost ("Boost", Float) = 1
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
            "CanUseSpriteAtlas" = "True"
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
            Name "AFGlass"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            half _Boost;
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
                o.texcoord = v.texcoord;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 c = tex2D(_MainTex, i.texcoord) * i.color;
                c.rgb = saturate(c.rgb * _Boost);
                c.a = saturate(c.a * _Boost);
                return c;
            }
            ENDCG
        }
    }
}
