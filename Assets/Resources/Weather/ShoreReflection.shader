Shader "UnderGrub/Shore Reflection"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Weather("Weather", Float) = 0
        _HasWater("Reflective tiles", Float) = 0
        _MaskTex("Light mask", 2D) = "white" {}
        _NormalMap("Normal map", 2D) = "bump" {}
        [PerRendererData] _WaterSurface("Water surface", 2D) = "black" {}
        _PlayerTex("Reflected sprite", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _ZWrite("ZWrite", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex ShoreVertex
            #pragma fragment ShoreFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_LIT_OUTPUTS half4 color : COLOR; float2 worldXY : TEXCOORD6; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            #include "VillageSky.hlsl"
            TEXTURE2D(_WaterSurface); SAMPLER(sampler_WaterSurface);
            TEXTURE2D(_PlayerTex); SAMPLER(sampler_PlayerTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Weather, _HasWater;
                float4 _MainTex_TexelSize;
                float4x4 _ReflectionToPlayer;
                float4 _PlayerRect, _PlayerUVX, _PlayerUVY, _ReflectionFlip;
            CBUFFER_END

            Varyings ShoreVertex(Attributes v)
            {
                Varyings o = CommonLitVertex(v);
                o.color = v.color * _Color;
                o.worldXY = TransformObjectToWorld(v.positionOS).xy;
                return o;
            }

            half4 ShoreFragment(Varyings i) : SV_Target
            {
                half4 tile = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half surface = _HasWater * SAMPLE_TEXTURE2D(_WaterSurface, sampler_WaterSurface, i.uv).r;
                float2 local = mul(_ReflectionToPlayer, float4(i.worldXY, 0, 1)).xy;
                // A reflection is twice the bank height below the original feet.
                float depth = -local.y - 2 * _ReflectionFlip.w;
                if (surface > .5 && depth >= 0 && depth < 4 && abs(local.x) < 4)
                {
                    float row = floor(depth / max(.001, _ReflectionFlip.z));
                    float ripple = round(sin(row * .7 - _Time.y * 2.3) * saturate(depth * 2)) * _ReflectionFlip.z;
                    float3 reflected = float3((local.x + ripple) * _ReflectionFlip.x, depth * 1.12 * _ReflectionFlip.y, 1);
                    float2 uv = float2(dot(reflected, _PlayerUVX.xyz), dot(reflected, _PlayerUVY.xyz));
                    if (all(uv > _PlayerRect.xy) && all(uv < _PlayerRect.zw))
                    {
                        half4 actor = SAMPLE_TEXTURE2D(_PlayerTex, sampler_PlayerTex, uv);
                        half strength = lerp(.65, .26, saturate(depth * .65));
                        tile.rgb = lerp(tile.rgb, actor.rgb * half3(.73, .87, .96), actor.a * strength);
                    }
                }
                float2 pixelWorld = floor(i.worldXY * 16)/16;
                if (_Weather == 1)
                {
                    // One-texel displacement on small bands of hot ground. Reject
                    // transparent neighbours to keep sprite silhouettes and atlas borders intact.
                    float wave = sin(pixelWorld.y * 6.4 - _Time.y * 2.7 + sin(pixelWorld.x*.9));
                    if (abs(wave) > .97 && SkyNoise(pixelWorld*.62 - float2(0,_Time.y*.21)) > .67)
                    {
                        half4 neighbour = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,
                            i.uv + float2(sign(wave)*_MainTex_TexelSize.x,0));
                        tile.rgb = lerp(tile.rgb, neighbour.rgb, .20*step(.99,neighbour.a)*step(.99,tile.a));
                    }
                    half green = saturate((tile.g - max(tile.r, tile.b))*3);
                    tile.rgb = lerp(tile.rgb, half3(min(1.,tile.r + .11),tile.g*.98,tile.b*.86), green*.7);
                }
                if (_Weather == 2)
                    tile.rgb *= lerp(half3(1,1,1),half3(.69,.76,.83),CloudShadow(pixelWorld));
                if (_Weather == 3)
                {
                    float2 delta; float phase;
                    RainEvent(pixelWorld,delta,phase);
                    tile.rgb = lerp(tile.rgb, half3(.7,.87,.98), RainImpact(delta,phase,surface>.5));
                }
                if (_Weather == 4)
                    tile.rgb = lerp(tile.rgb, dot(tile.rgb,half3(.299,.587,.114)).xxx, .14);
                if (_Weather == 5 && surface>.5)
                {
                    float glint = sin(pixelWorld.x*5.3 + sin(pixelWorld.y*2.1) + _Time.y*1.4);
                    tile.rgb += half3(.065,.085,.09)*smoothstep(.92,1.,glint)*smoothstep(.63,.82,SkyNoise(pixelWorld));
                }
                SurfaceData2D data;
                InputData2D lighting;
                InitializeSurfaceData(tile.rgb, tile.a, SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv),
                    UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv)), data);
                InitializeInputData(i.uv, i.lightingUV, lighting);
                return CombinedShapeLightShared(data, lighting);
            }
            ENDHLSL
        }
    }
}
