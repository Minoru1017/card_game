Shader "UI/IrisBlackout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SnapshotTex ("Snapshot", 2D) = "black" {}
        _UseSnapshot ("Use Snapshot In Hole", Float) = 0
        _Radius ("Hole Radius", Range(0, 2)) = 0.75
        _Softness ("Edge Softness", Range(0, 0.1)) = 0.018
        _Aspect ("Aspect", Float) = 1.777

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
            Name "Default"
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
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _SnapshotTex;
            float4 _ClipRect;
            float _UseSnapshot;
            float _Radius;
            float _Softness;
            float _Aspect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.texcoord - float2(0.5, 0.5);
                p.x *= _Aspect;
                float dist = length(p);
                float edge = fwidth(dist) * 1.35 + _Softness;
                float outside = smoothstep(_Radius - edge, _Radius + edge, dist);

                if (outside < 0.001)
                {
                    if (_UseSnapshot > 0.5)
                        return tex2D(_SnapshotTex, i.texcoord) * i.color;
                    return fixed4(0, 0, 0, 0);
                }

                return fixed4(0, 0, 0, outside * i.color.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
