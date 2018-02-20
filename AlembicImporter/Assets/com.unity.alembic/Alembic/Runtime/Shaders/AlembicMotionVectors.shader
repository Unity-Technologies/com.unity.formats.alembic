// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/AlembicMotionVectors"
{
    SubShader
    {
        Pass 
        {
            Name "MotionVectors"
            Tags{ "LightMode" = "MotionVectors" }

            Cull [_CullMode]
            ZTest LEqual
            ZWrite Off

            CGPROGRAM
            #pragma vertex VertMotionVectors
            #pragma fragment FragMotionVectors
            #include "AlembicMotionVectors.cginc"
            ENDCG
        }
    }
}
