#ifndef PseudoInstancing_h
#define PseudoInstancing_h

int         _NumInstances;
float4      _ModelScale;
float4      _TransScale;
int         _BatchBegin;

sampler2D   _PositionBuffer;
sampler2D   _IDBuffer;
float4 _PositionBuffer_TexelSize;


int     GetNumInstances()       { return _NumInstances; }
float3  GetModelScale()         { return _ModelScale.xyz; }
float3  GetTransScale()         { return _TransScale.xyz; }
int     GetInstanceID(float i)  { return i + _BatchBegin; }

float4  InstanceTexcoord(int i)         { return float4(_PositionBuffer_TexelSize.xy*float2(fmod(i, _PositionBuffer_TexelSize.z) + 0.5, floor(i / _PositionBuffer_TexelSize.z) + 0.5), 0.0, 0.0); }
float3  GetInstanceTranslation(int i)   { return tex2Dlod(_PositionBuffer, InstanceTexcoord(i)).xyz; }
float   GetObjectID(int i)              { return tex2Dlod(_IDBuffer, InstanceTexcoord(i)).x; }

#endif // PseudoInstancing_h

