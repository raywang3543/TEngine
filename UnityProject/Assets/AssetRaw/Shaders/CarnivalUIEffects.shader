Shader "Carnival/UIEffects"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.08, 0.03, 0.12, 1)
        _ColorB ("Color B", Color) = (1.00, 0.20, 0.48, 1)
        _AccentColor ("Accent", Color) = (1.00, 0.78, 0.28, 1)
        _EffectMode ("Effect Mode", Float) = 0
        _Speed ("Speed", Float) = 1
        _Intensity ("Intensity", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma target 3.5
            #pragma multi_compile_local _ _UIE_FORCE_GAMMA
            #pragma multi_compile_local _ _UIE_TEXTURE_SLOT_COUNT_1 _UIE_TEXTURE_SLOT_COUNT_2 _UIE_TEXTURE_SLOT_COUNT_4
            #pragma multi_compile_local _ _UIE_RENDER_TYPE_SOLID _UIE_RENDER_TYPE_TEXTURE _UIE_RENDER_TYPE_TEXT _UIE_RENDER_TYPE_GRADIENT
            #pragma vertex uie_std_vert
            #pragma fragment CarnivalFragment
            #include "Internal/UnityUIE.cginc"

            float4 _ColorA;
            float4 _ColorB;
            float4 _AccentColor;
            float _EffectMode;
            float _Speed;
            float _Intensity;

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            half3 DrawAmbient(float2 uv, float time)
            {
                float2 centered = uv - 0.5;
                float distanceFromCenter = length(centered);
                float angle = atan2(centered.y, centered.x);
                float waves = sin(distanceFromCenter * 18.0 - time * 1.4 + angle * 2.0);
                float glow = smoothstep(0.75, 0.0, distanceFromCenter);
                float sparkle = step(0.985, Hash21(floor(uv * 36.0) + floor(time * 0.35)));
                float blend = saturate(0.36 + waves * 0.16 + glow * 0.34);
                float3 color = lerp(_ColorA.rgb, _ColorB.rgb, blend);
                color += _AccentColor.rgb * sparkle * 0.45;
                return color * _Intensity;
            }

            half3 DrawFelt(float2 uv, float time)
            {
                float2 centered = uv - 0.5;
                float radius = length(centered);
                float ring = 0.5 + 0.5 * sin(radius * 42.0 - time * 1.8);
                float scan = 0.5 + 0.5 * sin((uv.x + uv.y) * 32.0 + time);
                float edgeGlow = smoothstep(0.72, 0.16, radius);
                float blend = saturate(ring * 0.18 + scan * 0.10 + edgeGlow * 0.24);
                float3 color = lerp(_ColorA.rgb, _ColorB.rgb, blend);
                return color * _Intensity;
            }

            half3 DrawAccent(float2 uv, float time)
            {
                float sweepPosition = frac(time * 0.22) * 1.8 - 0.4;
                float sweep = 1.0 - smoothstep(0.0, 0.16, abs(uv.x - sweepPosition));
                float pulse = 0.78 + sin(time * 2.2) * 0.08;
                float vertical = smoothstep(0.0, 0.45, uv.y) *
                                 smoothstep(1.0, 0.55, uv.y);
                float3 color = lerp(_ColorA.rgb, _ColorB.rgb, uv.x);
                color = color * pulse + _AccentColor.rgb * sweep * vertical * 0.75;
                return color * _Intensity;
            }

            UIE_FRAG_T CarnivalFragment(v2f input) : SV_Target
            {
                UIE_FRAG_T baseColor = uie_std_frag(input);
                float2 uv = input.pos.xy / _ScreenParams.xy;
                float time = _Time.y * _Speed;
                half3 effectColor;

                if (_EffectMode < 0.5)
                    effectColor = DrawAmbient(uv, time);
                else if (_EffectMode < 1.5)
                    effectColor = DrawFelt(uv, time);
                else
                    effectColor = DrawAccent(uv, time);

                return UIE_FRAG_T(effectColor * baseColor.rgb, baseColor.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
