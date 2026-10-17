Shader "Custom/UI Hologram"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        _BackgroundColor("Background Color", Color) = (0.0,0.8,1.0,1)
        _LineColor("Line Color", Color) = (1,1,1,1)

        _LineDensity("Line Density", Float) = 120
        _LineWidth("Line Width", Range(0.001,10)) = 0.03
        _ScrollSpeed("Scroll Speed", Float) = -0.15
        _LineStrength("Line Strength", Range(0,2)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _BackgroundColor;
            float4 _LineColor;

            float _LineDensity;
            float _LineWidth;
            float _ScrollSpeed;
            float _LineStrength;

            // Set globally every frame by UnscaledTimeSetter.cs.
            //
            // When the game is paused (timeScale = 0), _Time.y freezes,
            // but Time.unscaledTime keeps running. UI like this holo
            // effect should keep animating, so we use this value.
            //
            // Falls back to _Time.y when the global is 0 so the material
            // still animates in the editor (where the setter never runs).
            float _UnscaledTime;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float t =
                    _UnscaledTime > 0.0
                    ? _UnscaledTime
                    : _Time.y;

                // Slight hologram shimmer
                uv.x += sin(uv.y * 80 + t * 6) * 0.002;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // Moving scanlines
                float y = frac(uv.y * _LineDensity + t * _ScrollSpeed);

                float scanLine = smoothstep(1.0 - _LineWidth, 1.0, y);

                // Background
                float3 color = _BackgroundColor.rgb;

                // White scanlines
                color = lerp(color,
                             _LineColor.rgb,
                             scanLine * _LineStrength);

                return float4(color, tex.a * _BackgroundColor.a);
            }

            ENDHLSL
        }
    }
}