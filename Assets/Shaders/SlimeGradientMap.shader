// Gradient-map untuk layer body slime (render grayscale, lihat art/CLAUDE.md).
// Abu 40 = outline, 64/128/191/255 = band 1-4. Tiap state bahan = 1 material.
// Lit: ikut lampu 2D URP (gelap/obor), tapi tetap menonjol lewat _LightInfluence + _MinLight.
// _UseGradient = 0 untuk layer face (warna tekstur dipakai apa adanya).
Shader "GooNapped/SlimeGradientMap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [Toggle] _UseGradient ("Pakai gradient-map (matikan untuk face)", Float) = 1
        _Outline ("Outline (abu 40)", Color) = (0.06, 0.25, 0.16, 1)
        _Band1 ("Band 1 gelap (abu 64)", Color) = (0.20, 0.45, 0.40, 1)
        _Band2 ("Band 2 (abu 128)", Color) = (0.30, 0.65, 0.45, 1)
        _Band3 ("Band 3 (abu 191)", Color) = (0.47, 0.83, 0.55, 1)
        _Band4 ("Band 4 terang (abu 255)", Color) = (0.66, 0.93, 0.68, 1)
        _LightInfluence ("Pengaruh lampu 2D (0 = unlit)", Range(0,1)) = 0.75
        _MinLight ("Terang minimum (biar tetap kebaca)", Range(0,1)) = 0.45
        [HideInInspector] _MaskTex ("Mask", 2D) = "white" {}
        [HideInInspector] _NormalMap ("Normal Map", 2D) = "bump" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        half4 GradientMap(half4 t, float useGrad, half4 o, half4 b1, half4 b2, half4 b3, half4 b4)
        {
            if (useGrad < 0.5) return t;
            half g = t.r;
            half4 c = g < 0.20 ? o : g < 0.376 ? b1 : g < 0.627 ? b2 : g < 0.875 ? b3 : b4;
            c.a = t.a;
            return c;
        }
        ENDHLSL

        // Renderer 2D: ikut lampu
        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma multi_compile_instancing

            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings  { COMMON_2D_LIT_OUTPUTS half4 color : COLOR; };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Outline, _Band1, _Band2, _Band3, _Band4;
                float _UseGradient;
                half _LightInfluence, _MinLight;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                SetUpSpriteInstanceProperties();
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                Varyings o = CommonLitVertex(v);
                o.color = v.color * unity_SpriteColor;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 c = GradientMap(t, _UseGradient, _Outline, _Band1, _Band2, _Band3, _Band4) * i.color;

                SurfaceData2D s; InputData2D d;
                InitializeSurfaceData(c.rgb, c.a, half4(1,1,1,1), half3(0,0,1), s);
                InitializeInputData(i.uv, i.lightingUV, d);
                half4 lit = CombinedShapeLightShared(s, d);

                // jangan sampai lebih gelap dari _MinLight, lalu campur dengan warna asli
                half3 litC = max(lit.rgb, c.rgb * _MinLight);
                return half4(lerp(c.rgb, litC, _LightInfluence), c.a);
            }
            ENDHLSL
        }

        // Fallback (renderer non-2D / preview): unlit
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings  { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Outline, _Band1, _Band2, _Band3, _Band4;
                float _UseGradient;
                half _LightInfluence, _MinLight;
            CBUFFER_END
            Varyings vert (Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; o.color = v.color; return o; }
            half4 frag (Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return GradientMap(t, _UseGradient, _Outline, _Band1, _Band2, _Band3, _Band4) * i.color;
            }
            ENDHLSL
        }
    }
}
