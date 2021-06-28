#include "pch.h"
#include "aiMath.h"
#include "AlignedVector.h"
#include <numeric>

// ispc implementation
#ifdef aiEnableISPC
#include "aiSIMD.h"

void ApplyScaleISPC(abcV3 *dst, int num, float scale)
{
    ispc::Scale((float*)dst, num * 3, scale);
}

void NormalizeISPC(abcV3 *dst, int num)
{
    ispc::Normalize((ispc::float3*)dst, num);
}

void LerpISPC(float *dst, const float * v1, const float * v2, int num, float w)
{
	ispc::Lerp((float*)dst, (float*)v1, (float*)v2, num, w);
}
void LerpISPC(abcV2 *dst, const abcV2 * v1, const abcV2 * v2, int num, float w)
{
    ispc::Lerp((float*)dst, (float*)v1, (float*)v2, num * 2, w);
}

void LerpISPC(abcV3 *dst, const abcV3 * v1, const abcV3 * v2, int num, float w)
{
    ispc::Lerp((float*)dst, (float*)v1, (float*)v2, num * 3, w);
}

void LerpISPC(abcC4 *dst, const abcC4 * v1, const abcC4 * v2, int num, float w)
{
    ispc::Lerp((float*)dst, (float*)v1, (float*)v2, num * 4, w);
}

void GenerateVelocitiesISPC(abcV3 *dst, const abcV3 *p1, const abcV3 *p2, int num, float motion_scale)
{
    ispc::GenerateVelocities(
        (ispc::float3*)dst, (ispc::float3*)p1, (ispc::float3*)p2, num, motion_scale);
}

void MinMaxISPC(abcV3 & min, abcV3 & max, const abcV3 * points, int num)
{
    ispc::MinMax3((ispc::float3&)min, (ispc::float3&)max, (const ispc::float3*)points, num);
}


void GenerateTangentsISPC(abcV4 *dst,
    const abcV3 *points, const abcV2 *uv, const abcV3 *normals, const int *indices,
    int num_points, int num_triangles)
{
    ispc::GenerateTangentsTriangleIndexed(
        (ispc::float4*)dst, (const ispc::float3*)points, (const ispc::float2*)uv, (const ispc::float3*)normals, indices, num_points, num_triangles);
}

void GeneratePointNormalsISPC(const int *face_start_offsets, const int *face_vertex_counts, const int *face_indices,
    const float *positions, float *normals, const int *remapped_indices, const int face_count, const int remapped_count, const int orig_point_count)
{
    std::vector<float> temp_normals(orig_point_count * 3, 0);
    memset(normals, 0, remapped_count * 3 * sizeof(float));
    ispc::GeneratePointNormals(face_start_offsets, face_vertex_counts, face_indices, positions, normals, remapped_indices, face_count, remapped_count, orig_point_count);
}

#endif // aiEnableISPC


// < generic implementation

void ApplyScaleGeneric(abcV3 *dst, int num, float scale)
{
    for (int i = 0; i < num; ++i)
    {
        dst[i] *= scale;
    }
}

void LerpGeneric(float *dst, const float *v1, const float *v2, int num, float w)
{
	float iw = 1.0f - w;
	for (int i = 0; i < num; ++i)
	{
		dst[i] = (v1[i] * iw) + (v2[i] * w);
	}
}

void LerpGeneric(abcV2 *dst, const abcV2 *v1, const abcV2 *v2, int num, float w)
{
    float iw = 1.0f - w;
    for (int i = 0; i < num; ++i)
    {
        dst[i] = (v1[i] * iw) + (v2[i] * w);
    }
}

void LerpGeneric(abcV3 *dst, const abcV3 *v1, const abcV3 *v2, int num, float w)
{
    float iw = 1.0f - w;
    for (int i = 0; i < num; ++i)
    {
        dst[i] = (v1[i] * iw) + (v2[i] * w);
    }
}

void LerpGeneric(abcC4 *dst, const abcC4 *v1, const abcC4 *v2, int num, float w)
{
    float iw = 1.0f - w;
    for (int i = 0; i < num; ++i)
    {
        dst[i] = (v1[i] * iw) + (v2[i] * w);
    }
}

void NormalizeGeneric(abcV3 *dst_, int num)
{
    auto *dst = (float3*)dst_;
    for (int i = 0; i < num; ++i)
    {
        dst[i] = normalize(dst[i]);
    }
}

void GenerateVelocitiesGeneric(abcV3 *dst, const abcV3 *p1, const abcV3 *p2, int num, float motion_scale)
{
    for (int i = 0; i < num; ++i)
    {
        dst[i] = (p2[i] - p1[i]) * motion_scale;
    }
}

void MinMaxGeneric(abcV3 &dst_min, abcV3 &dst_max, const abcV3 *src_, int num)
{
    if (num == 0) { return; }

    auto *src = (const float3*)src_;
    auto rmin = src[0];
    auto rmax = src[0];
    for (int i = 1; i < num; ++i)
    {
        auto t = src[i];
        rmin = min(rmin, t);
        rmax = max(rmax, t);
    }
    dst_min = (abcV3&)rmin;
    dst_max = (abcV3&)rmax;
}

void GenerateTangentsGeneric(abcV4 *dst_,
    const abcV3 *points_, const abcV2 *uv_, const abcV3 *normals_, const int *indices,
    int num_points, int num_triangles)
{
    auto *dst = (float4*)dst_;
    auto *points = (const float3*)points_;
    auto *uv = (const float2*)uv_;
    auto *normals = (const float3*)normals_;

    AlignedVector<float3> tangents, binormals;
    ResizeZeroClear(tangents,num_points);
    ResizeZeroClear(binormals,num_points);

    for (int ti = 0; ti < num_triangles; ++ti)
    {
        int ti3 = ti * 3;
        int idx[3] = { indices[ti3 + 0], indices[ti3 + 1], indices[ti3 + 2] };
        float3 v[3] = { points[idx[0]], points[idx[1]], points[idx[2]] };
        float2 u[3] = { uv[idx[0]], uv[idx[1]], uv[idx[2]] };
        float3 t[3];
        float3 b[3];
        compute_triangle_tangent(v, u, t, b);

        for (int i = 0; i < 3; ++i)
        {
            tangents[idx[i]] += t[i];
            binormals[idx[i]] += b[i];
        }
    }

    for (int vi = 0; vi < num_points; ++vi)
    {
        dst[vi] = orthogonalize_tangent(tangents[vi], binormals[vi], normals[vi]);
    }
}


void GeneratePointNormalsGeneric(const int *face_start_offsets, const int *face_vertex_counts, const int *face_indices,
    const float *positions, float *normals, const int *remapped_indices, const int face_count, const int remapped_count,
    const int orig_point_count)
{

    std::vector<abcV3> tmp_normals(orig_point_count, {0.f, 0.f, 0.f});
    for (size_t i = 0; i < face_count; ++i) {
        int face_vert_count = face_vertex_counts[i];
        int face_start_index = face_start_offsets[i];
        if (face_vert_count < 3) {
            continue;
        }
        for (int tri = 0; tri < face_vert_count - 2; ++tri) {
            int i1 = face_indices[face_start_index + tri];
            int i2 = face_indices[face_start_index + tri + 1];
            int i3 = face_indices[face_start_index + tri + 2];
            abcV3 p1 = reinterpret_cast<const abcV3*>(positions)[i1];
            abcV3 p2 = reinterpret_cast<const abcV3*>(positions)[i2];
            abcV3 p3 = reinterpret_cast<const abcV3*>(positions)[i3];
            abcV3 tri_normal = (p3 - p1).cross(p2 - p1);
            tmp_normals[i1] += tri_normal;
            tmp_normals[i2] += tri_normal;
            tmp_normals[i3] += tri_normal;
        }
    }
    for (size_t i = 0; i < remapped_count; ++i) {
        int remap = remapped_indices[i];
        abcV3 n = tmp_normals[remap].normalize();
        reinterpret_cast<abcV3*>(normals)[i] = abcV3{ -n.x, n.y, n.z};
    }
}

// > generic implementation


// SIMD can't make this faster
void SwapHandedness(abcV3 *dst, int num)
{
    for (int i = 0; i < num; ++i)
    {
        dst[i].x *= -1.0f;
    }
}

void SwapHandedness(abcV4 *dst, int num)
{
    for (int i = 0; i < num; ++i)
    {
        dst[i].x *= -1.0f;
    }
}

#ifdef aiEnableISPC
    #define Impl(Func, ...) Func##ISPC(__VA_ARGS__)
#else
    #define Impl(Func, ...) Func##Generic(__VA_ARGS__)
#endif

void ApplyScale(abcV3 *dst, int num, float scale)
{
    Impl(ApplyScale, dst, num, scale);
}

void Normalize(abcV3 *dst, int num)
{
    Impl(Normalize, dst, num);
}

void Lerp(float *dst, const float *v1, const float *v2, int num, float w)
{
	Impl(Lerp, dst, v1, v2, num, w);
}

void Lerp(abcV2 *dst, const abcV2 *v1, const abcV2 *v2, int num, float w)
{
    Impl(Lerp, dst, v1, v2, num, w);
}

void Lerp(abcV3 *dst, const abcV3 *v1, const abcV3 *v2, int num, float w)
{
    Impl(Lerp, dst, v1, v2, num, w);
}

void Lerp(abcC4 *dst, const abcC4 *v1, const abcC4 *v2, int num, float w)
{
    Impl(Lerp, dst, v1, v2, num, w);
}

void GenerateVelocities(abcV3 *dst, const abcV3 *p1, const abcV3 *p2, int num, float motion_scale)
{
    Impl(GenerateVelocities, dst, p1, p2, num, motion_scale);
}

void MinMax(abcV3 &min, abcV3 &max, const abcV3 *points, int num)
{
    Impl(MinMax, min, max, points, num);
}

void GenerateTangents(abcV4 *dst,
    const abcV3 *points, const abcV2 *uv, const abcV3 *normals, const int *indices,
    int num_points, int num_triangles)
{
    Impl(GenerateTangents, dst, points, uv, normals, indices, num_points, num_triangles);
}

void GeneratePointNormals(const int *face_vertex_counts, const int *face_indices, const abcV3 *points, abcV3 *normals,
        const int *remapped_point_indices, const int face_count, const int remapped_count, const int orig_point_count)
{
    std::vector<int> face_start_indices(face_count);
    std::partial_sum(face_vertex_counts, face_vertex_counts + face_count - 1, face_start_indices.begin() + 1);
    memset(normals, 0, remapped_count * 3 * sizeof(float));
    Impl(GeneratePointNormals, face_start_indices.data(), face_vertex_counts, face_indices,
            reinterpret_cast<const float*>(points), reinterpret_cast<float*>(normals),
            remapped_point_indices, face_count, remapped_count, orig_point_count);
}

#undef Impl
