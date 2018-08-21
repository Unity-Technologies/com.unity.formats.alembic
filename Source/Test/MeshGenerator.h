#pragma once

#include "../abci/abci.h"
#include "../abci/Foundation/aiMath.h"

void GenerateCylinderMesh(
    std::vector<int>& counts,
    std::vector<int>& indices,
    std::vector<float3> &points,
    std::vector<float2> &uv,
    float radius, float height,
    int cseg, int hseg);

void GenerateIcoSphereMesh(
    std::vector<int>& counts,
    std::vector<int>& indices,
    std::vector<float3>& points,
    std::vector<float2>& uv,
    float radius,
    int iteration);
