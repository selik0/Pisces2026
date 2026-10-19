Shader "UI/Default Grayscale Circle Mask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Grayscale ("Grayscale", Range(0, 1)) = 0
        _CircleRadius ("Circle Radius", Range(0, 1)) = 1
        _EdgeSoftness ("Edge Softness", Range(0.5, 2)) = 1
        _UVRect ("Sprite UV Rect (Min X, Min Y, Max X, Max Y)", Vector) = (0,0,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [Toggle(PIXELSNAP_ON)] _PixelSnap ("Pixel Snap", Float) = 0
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile_local _ ETC1_EXTERNAL_ALPHA

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
            sampler2D _AlphaTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float _EnableExternalAlpha;
            float4 _ClipRect;
            float4 _MainTex_ST;
            half _Grayscale;
            float _CircleRadius;
            half _EdgeSoftness;
            float4 _UVRect;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);

                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif

                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half CircleMaskAlpha(float2 textureUV)
            {
                if (_CircleRadius <= 0.0)
                {
                    return 0.0h;
                }

                float2 uvRange = max(_UVRect.zw - _UVRect.xy, float2(0.0001, 0.0001));
                float2 normalizedUV = saturate((textureUV - _UVRect.xy) / uvRange);
                float maximumRadius = length(float2(0.5, 0.5));
                float2 localPosition = normalizedUV - 0.5;
                float radius = saturate(_CircleRadius) * maximumRadius;
                float signedDistance = length(localPosition) - radius;
                float edgeWidth = max(fwidth(signedDistance) * max(_EdgeSoftness, 0.5h), 0.0001);
                return 1.0h - smoothstep(-edgeWidth, edgeWidth, signedDistance);
            }

            fixed4 SampleUITexture(float2 textureUV)
            {
                fixed4 color = tex2D(_MainTex, textureUV);

                #ifdef ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D(_AlphaTex, textureUV);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
                #endif

                return color;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = (SampleUITexture(input.texcoord) + _TextureSampleAdd) * input.color;

                half luminance = dot(color.rgb, half3(0.299h, 0.587h, 0.114h));
                color.rgb = lerp(color.rgb, luminance.xxx, saturate(_Grayscale));
                color.a *= CircleMaskAlpha(input.texcoord);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001h);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
