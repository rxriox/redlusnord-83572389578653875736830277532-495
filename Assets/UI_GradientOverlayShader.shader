Shader "UI/Custom/URP_GradientOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _GradientColor ("Gradient Color", Color) = (0,0,0,1)
        _GradientDirection ("Direction", Float) = 4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "ForwardLit"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            Lighting Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _GradientColor;
                float _GradientDirection;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                float gradient = 0;

                float direction = _GradientDirection;
                if (direction == 0) gradient = 1.0 - IN.uv.y;
                else if (direction == 1) gradient = (1.0 - IN.uv.x + 1.0 - IN.uv.y) / 2.0;
                else if (direction == 2) gradient = 1.0 - IN.uv.x;
                else if (direction == 3) gradient = (1.0 - IN.uv.x + IN.uv.y) / 2.0;
                else if (direction == 4) gradient = IN.uv.y;
                else if (direction == 5) gradient = (IN.uv.x + IN.uv.y) / 2.0;
                else if (direction == 6) gradient = IN.uv.x;
                else if (direction == 7) gradient = (IN.uv.x + 1.0 - IN.uv.y) / 2.0;

                half4 gradientOverlay = lerp(half4(_GradientColor.rgb, 0), _GradientColor, gradient);
                
                return half4(originalColor.rgb * (1.0 - gradientOverlay.a) + gradientOverlay.rgb * gradientOverlay.a, originalColor.a);
            }
            ENDHLSL
        }
    }
}