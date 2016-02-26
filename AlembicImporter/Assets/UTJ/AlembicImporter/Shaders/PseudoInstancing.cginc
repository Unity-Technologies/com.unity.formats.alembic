#ifndef PseudoInstancing_h
#define PseudoInstancing_h

int         _NumInstances;
float4      _ModelScale;
float4      _TransScale;
int         _BatchBegin;

sampler2D   _PositionBuffer;
sampler2D   _VelocityBuffer;
sampler2D   _IDBuffer;
float4      _PositionBuffer_TexelSize;
float4      _CountRate; // x: count rate, y: 1.0 / count rate


int     GetNumInstances()       { return _NumInstances; }
float3  GetModelScale()         { return _ModelScale.xyz; }
float3  GetTransScale()         { return _TransScale.xyz; }
float   GetInstanceID(float i)  { return i + _BatchBegin; }

float4  InstanceTexcoord(float i)
{
    i = i * _CountRate.y;
    float2 coord = float2(
        fmod(i, _PositionBuffer_TexelSize.z) + 0.5,
        floor(i / _PositionBuffer_TexelSize.z) + 0.5);
    return float4(_PositionBuffer_TexelSize.xy * coord, 0.0, 0.0);
}

float3  GetInstanceTranslation(float i) { return tex2Dlod(_PositionBuffer, InstanceTexcoord(i)).xyz; }
float3  GetInstanceVelocity(float i)    { return tex2Dlod(_VelocityBuffer, InstanceTexcoord(i)).xyz; }
float   GetObjectID(float i)            { return tex2Dlod(_IDBuffer, InstanceTexcoord(i)).x; }

#endif // PseudoInstancing_h

