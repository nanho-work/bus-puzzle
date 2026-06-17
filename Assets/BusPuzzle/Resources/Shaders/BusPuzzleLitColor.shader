Shader "BusPuzzle/Lit Color"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull [_Cull]
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Smoothness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                half3 normalWorld : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normalWorld = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half3 normalWorld = normalize(i.normalWorld);
                half3 keyDir = normalize(half3(-0.24, 0.82, -0.52));
                half3 fillDir = normalize(half3(0.42, 0.55, 0.28));
                half key = saturate(dot(normalWorld, keyDir));
                half fill = saturate(dot(normalWorld, fillDir)) * 0.10;
                half topLift = saturate(normalWorld.y) * 0.14;
                half shade = 0.58 + key * 0.42 + fill + topLift;
                half sheen = pow(saturate(dot(normalWorld, keyDir)), 10.0) * _Smoothness * 0.18;
                half rim = pow(1.0 - saturate(dot(normalWorld, half3(0, 1, 0))), 3.0) * _Smoothness * 0.035;
                fixed4 color = _Color;
                color.rgb = saturate(color.rgb * shade + sheen + rim);
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
