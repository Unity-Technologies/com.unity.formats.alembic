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
            ZWrite On

            CGPROGRAM
            #pragma vertex VertMotionVectors
            #pragma fragment FragMotionVectors
            #include "AlembicMotionVectors.cginc"
            ENDCG
        }
    }
}
