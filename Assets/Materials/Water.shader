Shader "Custom/Water"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow fullforwardshadows
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup
        #include "UnityCG.cginc"

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
        
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<float3> positionBuffer;
        #endif
        uniform float scale;

        struct Input {
            int i;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void setup() {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            float3 p = positionBuffer[unity_InstanceID];
            unity_ObjectToWorld._11_21_31_41 = float4(scale, 0, 0, 0);
            unity_ObjectToWorld._12_22_32_42 = float4(0, scale, 0, 0);
            unity_ObjectToWorld._13_23_33_43 = float4(0, 0, scale, 0);
            
            unity_ObjectToWorld._14_24_34_44 = float4(p, 1);
            unity_WorldToObject._14_24_34_44 = float4(-p, 1);
            unity_WorldToObject._11_22_33 = 1.0f / unity_ObjectToWorld._11_22_33;
            #endif
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
