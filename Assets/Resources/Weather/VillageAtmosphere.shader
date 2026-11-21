Shader "UnderGrub/Village Atmosphere"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _Weather("Weather", Float) = 0
        _Daylight("Daylight", Float) = 1
        _Rainbow("Post-rain rainbow", Float) = 0
        _RainbowArc("World centre and radii", Vector) = (0,0,7.5,4.6)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags {"LightMode"="Universal2D"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "VillageSky.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Weather, _Daylight, _Rainbow;
                float4 _RainbowArc;
            CBUFFER_END
            struct In { float4 positionOS : POSITION; };
            struct Out { float4 clip : SV_POSITION; float2 world : TEXCOORD0; float4 screen : TEXCOORD1; };
            Out Vert(In v)
            {
                Out o;
                float3 world = TransformObjectToWorld(v.positionOS.xyz);
                o.world = world.xy;
                o.clip = TransformWorldToHClip(world);
                o.screen = ComputeScreenPos(o.clip);
                return o;
            }
            half4 Frag(Out i) : SV_Target
            {
                float2 world = floor(i.world * 16)/16;
                float2 drift = _Time.y * float2(.27,.08);
                float strength = lerp(.27,1,_Daylight);
                if (_Weather == 3)
                {
                    float2 delta; float phase; RainEvent(world,delta,phase);
                    return half4(.69,.84,.95,RainStreak(delta,phase)*.69*strength);
                }
                if (_Weather == 4)
                {
                    float2 uv = abs(i.screen.xy/i.screen.w*2-1);
                    float edge = smoothstep(.45,1.08,length(uv*float2(.89,1)));
                    float mist = SkyNoise((world-drift)*.31)*.68 + SkyNoise((world+drift*.35)*.65)*.32;
                    // Elongated banks roll across the ground independently of
                    // the camera, as in the dungeon. Leave gaps between banks.
                    float2 p = (world - _Time.y*float2(.34,.025))*float2(.27,.70);
                    float density = SkyNoise(p)*.69 + SkyNoise(p*2.15+11)*.31;
                    float bank = smoothstep(.48,.77,density);
                    return half4(.72,.79,.84,(.014 + bank*(.12+.045*edge) + edge*(.25+.30*mist))*strength);
                }
                // Narrow and broad sun shafts share the same diagonal light direction.
                float across = dot(world,float2(.57,.28)) + _Time.y*.045;
                float sunlight = smoothstep(.49,.76,SkyNoise(float2(across,7.8))*.68
                    + SkyNoise(float2(across*2.9,12.3))*.32);
                half alpha = (.012 + sunlight*.065)*_Daylight;
                half3 glow = lerp(half3(.71,.87,1),half3(1,.94,.75),sunlight);
                if (_Rainbow > .5 && _Daylight > .02)
                {
                    // A distant, partial bow above the action. Its edges fade
                    // into the air, with the warm colours on the outer band.
                    float2 arc = (world - _RainbowArc.xy)/_RainbowArc.zw;
                    float radial = (length(arc)-.95)/.093;
                    float band = saturate(radial);
                    float visibility = smoothstep(-.12,.13,radial)*(1-smoothstep(.88,1.12,radial))
                        * smoothstep(.06,.8,arc.y) * (1-smoothstep(.15,.95,abs(arc.x)));
                    half3 rainbow = lerp(half3(.63,.62,.94),half3(.49,.81,.94),smoothstep(0,.24,band));
                    rainbow = lerp(rainbow,half3(.61,.91,.61),smoothstep(.25,.5,band));
                    rainbow = lerp(rainbow,half3(1,.88,.53),smoothstep(.52,.74,band));
                    rainbow = lerp(rainbow,half3(.99,.62,.58),smoothstep(.75,1.,band));
                    half arcAlpha = visibility*.27*_Daylight;
                    half combined = alpha + arcAlpha*(1-alpha);
                    glow = (glow*alpha*(1-arcAlpha)+rainbow*arcAlpha)/max(.001,combined);
                    alpha = combined;
                }
                return half4(glow,alpha);
            }
            ENDHLSL
        }
    }
}
