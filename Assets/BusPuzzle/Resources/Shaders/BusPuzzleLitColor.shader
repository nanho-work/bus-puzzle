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
                half3 lightDir = normalize(half3(-0.35, 0.78, -0.48));
                half diffuse = saturate(dot(normalWorld, lightDir));
                half topLift = saturate(normalWorld.y) * 0.18;
                half shade = 0.64 + diffuse * 0.34 + topLift;
                half sheen = pow(saturate(dot(normalWorld, normalize(half3(-0.62, 0.70, 0.34)))), 12.0) * _Smoothness * 0.14;
                fixed4 color = _Color;
                color.rgb = saturate(color.rgb * shade + sheen);
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
