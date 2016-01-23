Shader "Utility/DataViz" {
   Properties {
      [Enum(N,0,UV,1,T,2,Color,3)] _Data ("Data", Float) = 0
      [Enum(Object,0,World,1,Camera,2)] _Space ("Space", Float) = 0
      _RemapVectorMin ("Remap Vector Min", Float) = -1
      _RemapVectorMax ("Remap Vector Max", Float) = 1
      [Enum(UV0,0,UV1,1,UV2,2,UV3,3)] _UVSet ("UV Set", Float) = 0
   }
   SubShader {
      Pass {
         CGPROGRAM
         #pragma vertex vshd
         #pragma fragment fshd
         #pragma target 3.0

         #include "UnityCG.cginc"

         struct VSHDout {
            float4 position : SV_POSITION;
            float4 data : TEXCOORD0;
         };

         uniform float _Data;
         uniform float _Space;
         uniform float _UVSet;
         uniform float _RemapVectorMin;
         uniform float _RemapVectorMax;

         VSHDout vshd(appdata_full i) {
            VSHDout o;

            o.position = mul(UNITY_MATRIX_MVP, i.vertex);

            if (_Data == 0)
            {
               // Normal
               float3 N;
               if (_Space == 0)
               {
                  // Object
                  N = i.normal;
               }
               else if (_Space == 1)
               {
                  // World
                  N = UnityObjectToWorldNormal(i.normal.xyz);
               }
               else
               {
                  // Camera
                  N = normalize(UNITY_MATRIX_MV[0].xyz * i.normal.x + UNITY_MATRIX_MV[1].xyz * i.normal.y + UNITY_MATRIX_MV[2].xyz * i.normal.z);
               }
               o.data.xyz = _RemapVectorMin + ((N + 1.0f) / 2.0) * (_RemapVectorMax - _RemapVectorMin);
               o.data.a = 1.0f;
            }
            else if (_Data == 1)
            {
               // UVs
               if (_UVSet == 0)
               {
                  o.data.xy = i.texcoord.xy;
               }
               else if (_UVSet == 1)
               {
                  o.data.xy = i.texcoord1.xy;
               }
               else if (_UVSet == 2)
               {
                  o.data.xy = i.texcoord2.xy;
               }
               else if (_UVSet == 3)
               {
                  o.data.xy = i.texcoord3.xy;
               }
               else
               {
                  o.data.x = 0.0f;
                  o.data.y = 0.0f;
               }
               o.data.z = 0.0f;
               o.data.a = 1.0f;
            }
            else if (_Data == 2)
            {
               // Tangents
               float3 T;
               if (_Space == 0)
               {
                  // Object
                  T = i.tangent;
               }
               else if (_Space == 1)
               {
                  // World
                  T = UnityObjectToWorldDir(i.tangent.xyz);
               }
               else
               {
                  // Camera
                  T = normalize(mul((float3x3)UNITY_MATRIX_MV, i.tangent.xyz));
               }
               o.data.xyz = _RemapVectorMin + ((T + 1.0f) / 2.0) * (_RemapVectorMax - _RemapVectorMin);
               o.data.a = 1.0f;
            }
            else
            {
               // Colors
               o.data = i.color;
            }

            return o;
         }

         float4 fshd(VSHDout i) : SV_Target {
            return i.data;
         }
         ENDCG
      }
   }
   FallBack Off
}
