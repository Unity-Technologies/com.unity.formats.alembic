Shader "Alembic/Standard Instanced" {
    Properties {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        #pragma multi_compile_instancing
        #pragma instancing_options assumeuniformscaling maxcount:1023 procedural:setup
        #include "PointRenderer.cginc"

        fixed4 _Color;
        sampler2D _MainTex;

        struct Input {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        void setup()
        {
            float size = GetPointSize();
            float3 pos = GetAlembicPoint();

            unity_ObjectToWorld._11_21_31_41 = float4(size, 0, 0, 0);
            unity_ObjectToWorld._12_22_32_42 = float4(0, size, 0, 0);
            unity_ObjectToWorld._13_23_33_43 = float4(0, 0, size, 0);
            unity_ObjectToWorld._14_24_34_44 = float4(pos, 1);
            unity_WorldToObject = unity_ObjectToWorld;
            unity_WorldToObject._14_24_34 *= -1;
            unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
        }
#endif

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
}
