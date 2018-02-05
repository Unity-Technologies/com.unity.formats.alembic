Shader "Hidden/Alembic/MotionVectors"
{
    SubShader
    {
        Pass 
        {
            Name "MOTIONVECTORS"
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
