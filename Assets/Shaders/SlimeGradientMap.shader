// Gradient-map untuk layer body slime (render grayscale, lihat art/CLAUDE.md).
// Abu 40 = outline, 64/128/191/255 = band 1-4. Tiap state bahan = 1 material (atau ganti warna runtime).
Shader "GooNapped/SlimeGradientMap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Outline ("Outline (abu 40)", Color) = (0.06, 0.25, 0.16, 1)
        _Band1 ("Band 1 gelap (abu 64)", Color) = (0.20, 0.45, 0.40, 1)
        _Band2 ("Band 2 (abu 128)", Color) = (0.30, 0.65, 0.45, 1)
        _Band3 ("Band 3 (abu 191)", Color) = (0.47, 0.83, 0.55, 1)
        _Band4 ("Band 4 terang (abu 255)", Color) = (0.66, 0.93, 0.68, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings  { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Outline, _Band1, _Band2, _Band3, _Band4;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half g = t.r;
                half4 c = g < 0.20 ? _Outline
                        : g < 0.376 ? _Band1
                        : g < 0.627 ? _Band2
                        : g < 0.875 ? _Band3
                        : _Band4;
                c.a = t.a;
                return c * i.color;
            }
            ENDHLSL
        }
    }
}
