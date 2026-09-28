Shader "SkillsExtended/ElectronicsGauge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Fill ("Coherence", Range(0, 1)) = 1
        _Mirror ("Mirror", Float) = 0
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 world : TEXCOORD1;
            };
            float4 _ClipRect;
            float _Fill, _Mirror;
            v2f vert(appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float x = lerp(i.uv.x, 1 - i.uv.x, _Mirror) * 40;
                float y = (i.uv.y - .5) * 114;
                float center = 33 - 22 * sqrt(saturate(1 - (y / 67) * (y / 67)));
                float side = x - center;
                float coverageX = saturate(.5 - (abs(side) - 7) / max(fwidth(side), .0001));
                float band = floor(y / 6 + .5);
                float coverageY = saturate(.5 - (abs(y - band * 6) - 2) / max(fwidth(y), .0001));
                float index = band + 8;
                float valid = step(0, index) * step(index, 16);
                // Fade the final complete segment; never crop its geometry in half.
                float level = saturate(saturate(_Fill) * 17 - index);
                i.color.a *= coverageX * coverageY * valid * level;
                #ifdef UNITY_UI_CLIP_RECT
                i.color.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return i.color;
            }
            ENDCG
        }
    }
}
