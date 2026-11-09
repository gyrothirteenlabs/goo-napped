// GooNapped/PixelToonLit
// Shader umum untuk semua sprite: cahaya real-time dari Light 2D URP (pakai normal map kalau ada),
// lalu dipotong jadi beberapa band supaya tetap terlihat pixel art (tanpa gradasi halus).
//
// Aturan art: sprite digambar FLAT (tanpa cahaya/bayangan di-bake). Bentuk 3D datang dari _NormalMap
// (secondary texture sprite bernama "_NormalMap"). Lihat docs/panduan-art-desainer.md.
//
// Mode:
//  - _UseGradient = 1 (body slime): sprite grayscale; abu < 0.2 = outline, sisanya badan.
//    Intensitas cahaya dipetakan ke 4 warna palet (_Band1..4) = warna state bahan.
//  - _UseGradient = 0 (lingkungan, prop, face): warna sprite x cahaya yang sudah di-band.
Shader "GooNapped/PixelToonLit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (albedo flat)", 2D) = "white" {}
        _NormalMap ("Normal Map (secondary texture)", 2D) = "bump" {}
        [HideInInspector] _MaskTex ("Mask", 2D) = "white" {}

        [Header(Toon lighting)]
        _Steps ("Jumlah band cahaya (mode biasa)", Range(1,8)) = 4
        _MinLight ("Terang minimum", Range(0,1)) = 0.35
        _LightTint ("Seberapa kuat warna lampu mewarnai", Range(0,1)) = 0.8
        _LightInfluence ("Pengaruh lampu (0 = unlit)", Range(0,1)) = 1

        [Header(Gradient map slime)]
        [Toggle] _UseGradient ("Pakai gradient-map (body slime)", Float) = 0
        _Outline ("Outline", Color) = (0.06, 0.25, 0.16, 1)
        _Band1 ("Band 1 gelap", Color) = (0.18, 0.48, 0.42, 1)
        _Band2 ("Band 2", Color) = (0.25, 0.66, 0.42, 1)
        _Band3 ("Band 3", Color) = (0.44, 0.82, 0.49, 1)
        _Band4 ("Band 4 terang", Color) = (0.71, 0.95, 0.60, 1)
        _BandEdges ("Batas band (intensitas cahaya 1|2, 2|3, 3|4)", Vector) = (0.3, 0.55, 0.85, 0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #define TOON_PROPS \
            half4 _Outline, _Band1, _Band2, _Band3, _Band4; \
            half4 _BandEdges; \
            float _UseGradient; \
            half _Steps, _MinLight, _LightTint, _LightInfluence;
        ENDHLSL

        // ---- Renderer 2D: warna + cahaya ----
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
                TOON_PROPS
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
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                if (t.a < 0.01) discard;

                // cahaya total di pixel ini (permukaan putih) = multiply + additive dari semua Light 2D
                SurfaceData2D s; InputData2D d;
                InitializeSurfaceData(half3(1,1,1), 1, half4(1,1,1,1), half3(0,0,1), s);
                InitializeInputData(i.uv, i.lightingUV, d);
                half3 L = CombinedShapeLightShared(s, d).rgb;

                half inten = max(max(L.r, L.g), L.b);
                half3 hue = L / max(inten, 1e-3);
                hue = lerp(half3(1,1,1), hue, _LightTint);
                inten = lerp(1, inten, _LightInfluence);

                half3 col;
                if (_UseGradient > 0.5)
                {
                    if (t.r < 0.2)
                        col = _Outline.rgb * lerp(1, hue, 0.5);
                    else
                    {
                        half3 band = inten < _BandEdges.x ? _Band1.rgb
                                   : inten < _BandEdges.y ? _Band2.rgb
                                   : inten < _BandEdges.z ? _Band3.rgb
                                   : _Band4.rgb;
                        col = band * hue;
                    }
                }
                else
                {
                    half q = floor(inten * _Steps + 0.5) / _Steps;   // band cahaya
                    q = max(q, _MinLight);
                    col = t.rgb * hue * q;
                }
                return half4(col, t.a);
            }
            ENDHLSL
        }

        // ---- Renderer 2D: tulis normal untuk lampu yang pakai normal map ----
        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { COMMON_2D_NORMALS_INPUTS float4 color : COLOR; };
            struct Varyings  { COMMON_2D_NORMALS_OUTPUTS half4 color : COLOR; };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                TOON_PROPS
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                SetUpSpriteInstanceProperties();
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                Varyings o = CommonNormalsVertex(v);
                o.color = v.color * unity_SpriteColor;
                // sprite yang di-flipX: arah kiri-kanan normal ikut dibalik
                o.tangentWS *= unity_SpriteProps.x;
                return o;
            }

            half4 frag (Varyings i) : SV_Target { return CommonNormalsFragment(i, i.color); }
            ENDHLSL
        }

        // ---- Fallback (renderer non-2D / preview): unlit ----
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
                TOON_PROPS
            CBUFFER_END
            Varyings vert (Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; o.color = v.color; return o; }
            half4 frag (Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                if (_UseGradient > 0.5) t.rgb = t.r < 0.2 ? _Outline.rgb : _Band3.rgb;
                return t;
            }
            ENDHLSL
        }
    }
}
