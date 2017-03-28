Shader "Alembic/Billboard Instanced" {
    Properties {
        //_AlembicID("AlembicID", Float) = 0.0

        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Int) = 4
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Int) = 10

        _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        [Toggle(_VIEW_PLANE_PROJECTION)] _ViewPlaneProjection("View Plane Projection", Float) = 0
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
            #pragma multi_compile ___ _VIEW_PLANE_PROJECTION
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

            UNITY_INSTANCING_CBUFFER_START (Props)
                UNITY_DEFINE_INSTANCED_PROP (float, _AlembicID)
            UNITY_INSTANCING_CBUFFER_END

            float3 GetObjectPosition()
            {
                return float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]);
            }

            float3x3 ToF33(float4x4 v)
            {
                // (float3x3)v don't compile on some platforms
                return float3x3(
                    v[0][0], v[0][1], v[0][2],
                    v[1][0], v[1][1], v[1][2],
                    v[2][0], v[2][1], v[2][2]);
            }

            float3x3 Look33(float3 dir, float3 up)
            {
                float3 z = dir;
                float3 x = normalize(cross(up, z));
                float3 y = cross(z, x);
                return float3x3(
                    x.x, y.x, z.x,
                    x.y, y.y, z.y,
                    x.z, y.z, z.z);
            }

            struct Plane
            {
                float3 normal;
                float distance;
            };

            float DistancePointPlane(float3 pos, Plane plane)
            {
                return dot(pos, plane.normal) + plane.distance;
            }

            float3 ProjectToPlane(float3 pos, Plane plane)
            {
                float d = DistancePointPlane(pos, plane);
                return pos - d*plane.normal;
            }


            void ApplyBillboardTransform(inout float4 vertex)
            {
                float3 pos = GetObjectPosition();
                float3 camera_pos = _WorldSpaceCameraPos.xyz;
                float3 look = normalize(pos - camera_pos);
                float3 up = float3(0.0, 1.0, 0.0);

                vertex.xyz = mul(Look33(look, up), vertex.xyz);
                vertex.xyz = mul(ToF33(unity_ObjectToWorld), vertex.xyz);
                vertex.xyz += pos;
                vertex = mul(UNITY_MATRIX_VP, vertex);
            }

            bool ApplyViewPlaneBillboardTransform(inout float4 vertex)
            {
                float3 pos = GetObjectPosition();
                float4 vp = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
                if (vp.z < 0.0) {
                    // object is opposite side of camera
                    vertex.xyz *= 0.0;
                    return false;
                }

                float aspect = _ScreenParams.x / _ScreenParams.y;
                float3 camera_pos = _WorldSpaceCameraPos.xyz;
                float3 look = normalize(camera_pos - pos);
                Plane view_plane = { look, 1.0 };
                pos = camera_pos + ProjectToPlane(pos - camera_pos, view_plane);
                vertex.xyz = mul(ToF33(unity_ObjectToWorld), vertex.xyz);
                vertex.y *= -aspect;
                vertex.xy += vp.xy / vp.w;
                vertex.zw = float2(0.0, 1.0);
                return true;
            }


            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID (v);

                float4 vert = v.vertex;
#if _VIEW_PLANE_PROJECTION
                ApplyViewPlaneBillboardTransform(vert);
#else
                ApplyBillboardTransform(vert);
#endif
                o.vertex = vert;
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
