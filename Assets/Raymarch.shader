Shader "Raymarch"
{
    Properties
    {
        _MainTex("Texture", 3D) = "white" {}
        _Alpha("Alpha", float) = 0.02
        _StepSize("Step Size", float) = 0.01
        _ColorTint("ColorTint", Color) = (1,1,1,1)
        _ColorMap("ColorMap", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One OneMinusSrcAlpha
        ZTest Always
        ZWrite Off
        LOD 100

        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define MAX_STEP_COUNT 2048

            #define EPSILON 0.00001f

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 vectorToSurface : TEXCOORD0;
                float2 screenuv : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler3D _MainTex;
            float4 _MainTex_ST;
            float _Alpha;
            float _StepSize;
            float4 _ColorTint;
            sampler2D _ColorMap;
            float4 _ColorMap_ST;
            sampler2D _CameraDepthTexture;

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vectorToSurface = worldVertex - _WorldSpaceCameraPos;

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.screenuv = ((o.vertex.xy / o.vertex.w) + 1) / 2;

                o.screenPosition = o.vertex;

                UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                return o;
            }

            float4 BlendUnder(float4 color, float4 newColor)
            {
                color.rgb += (1.0 - color.a) * newColor.a * newColor.rgb;
                color.a += (1.0 - color.a) * newColor.a;
                return color;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 screenUV = (i.screenPosition.xy / i.screenPosition.w) * 0.5f + 0.5f;
                float depth = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, screenUV)));
                float screenDepth = DecodeFloatRG(tex2D(_CameraDepthTexture, i.screenuv).zw);
                float3 rayOrigin = mul(unity_WorldToObject, _WorldSpaceCameraPos);
                float3 rayDirection = mul(unity_WorldToObject, float4(normalize(i.vectorToSurface), 1));
                float4 color = float4(0, 0, 0, 0);
                float3 samplePosition = rayOrigin;

                for (int i = 0; i < MAX_STEP_COUNT; i++)
                {
                    if (depth >= i * _StepSize && max(abs(samplePosition.x), max(abs(samplePosition.y), abs(samplePosition.z))) < 0.5f + EPSILON)
                    {
                        float4 sampledColor = tex3D(_MainTex, samplePosition + float3(0.5f, 0.5f, 0.5f));
                        sampledColor.rgb = tex2D(_ColorMap, sampledColor.rg).rgb;
                        sampledColor.a *= _Alpha;
                        color = BlendUnder(color, sampledColor);
                    }
                    samplePosition += rayDirection * _StepSize;
                }

                return color * _ColorTint;
            }
            ENDCG
        }
    }
}
