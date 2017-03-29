Shader "Alembic/Billboard Instanced" {
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
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling maxcount:1024
            #include "UnityCG.cginc"

            fixed4 _Color;
            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 texcoord : TEXCOORD0;
            };

#ifdef UNITY_SUPPORT_INSTANCING
            UNITY_INSTANCING_CBUFFER_START (Props)
                UNITY_DEFINE_INSTANCED_PROP (float, _AlembicID)
            UNITY_INSTANCING_CBUFFER_END
#endif
           
            v2f vert (appdata v)
            {
#ifdef UNITY_SUPPORT_INSTANCING
                UNITY_SETUP_INSTANCE_ID(v);
#endif

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
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
