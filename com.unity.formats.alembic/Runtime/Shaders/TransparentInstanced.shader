Shader "Alembic/Points Transparent" {
    Properties {
        //_AlembicID("AlembicID", Float) = 0.0

        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Int) = 4
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Int) = 10

        _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
        ZTest[_ZTest]
        ZWrite[_ZWrite]
        Blend[_SrcBlend][_DstBlend]
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #include "PointRenderer.cginc"

            fixed4 _Color;
            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 texcoord : TEXCOORD0;
                uint iid : SV_InstanceID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 texcoord : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                float4 vertex = v.vertex;
                vertex = mul(mul(UNITY_MATRIX_VP, GetPointMatrix(v.iid)), vertex);

                v2f o;
                o.vertex = vertex;
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord.xy) * _Color;
                return c;
            }
            ENDCG
        }
    }
}
