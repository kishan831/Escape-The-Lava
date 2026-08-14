// Premultiplied additive sprite for URP.
//
// Every glow in the game (lava halo, diamond aura, embers, shockwave, confetti) uses this so the
// bright areas stack instead of occluding each other, and so the Bloom override in the volume
// profile has something above the threshold to bleed.
Shader "EscapeTheLava/Additive Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        // rgb is premultiplied by alpha in the fragment stage, so plain "One One" gives a soft
        // additive that still respects the sprite's alpha falloff.
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            Name "AdditiveSprite"

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Declared in UnityPerMaterial so the shader stays SRP Batcher compatible.
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 color = texel * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
