#include "pch.h"
#include "aiMath.h"
#include "RawVector.h"


// ispc implementation
#ifdef aiEnableISPC
#include "aiSIMD.h"

void apply_scale_ispc(abcV3 *dst, int num, float scale)
{
    ispc::Scale((float*)dst, num*3, scale);
}

void lerp_ispc(abcV3 *dst, const abcV3 * v1, const abcV3 * v2, int num, float w)
{
    ispc::Lerp((float*)dst, (float*)v1, (float*)v2, num*3, w);
}

void gen_velocity_ispc(abcV3 *dst_pos, abcV3 *dst_vel,
    const abcV3 *p1, const abcV3 *p2, int num, float time_offset, float time_interval, float motion_scale)
{
    ispc::GenerateVelocity((ispc::float3*)dst_pos, (ispc::float3*)dst_vel,(ispc::float3*)p1, (ispc::float3*)p2, num,
        time_offset, time_interval, motion_scale);
}

void get_bounds_ispc(abcV3 & min, abcV3 & max, const abcV3 * points, int num)
{
    ispc::MinMax3((ispc::float3&)min, (ispc::float3&)max, (const ispc::float3*)points, num);
}

void gen_normals_ispc(abcV3 * dst, const abcV3 * points, const int * indices, int num_points, int num_triangles)
{
    ispc::GenerateNormalsTriangleIndexed((ispc::float3*)dst, (const ispc::float3*)points, indices, num_points, num_triangles);
}

void gen_tangents_ispc(abcV4 *dst,
    const abcV3 *points, const abcV2 *uv, const abcV3 *normals, const int *indices,
    int num_points, int num_triangles)
{
    ispc::GenerateTangentsTriangleIndexed(
        (ispc::float4*)dst, (const ispc::float3*)points, (const ispc::float2*)uv, (const ispc::float3*)normals, indices, num_points, num_triangles);
}

#endif // aiEnableISPC


// < generic implementation

void apply_scale_generic(abcV3 *dst, int num, float scale)
{
    for (int i = 0; i < num; ++i) {
        dst[i] *= scale;
    }
}

void lerp_generic(abcV3 *dst, const abcV3 *v1, const abcV3 *v2, int num, float w)
{
    float iw = 1.0f - w;
    for (int i = 0; i < num; ++i) {
        dst[i] = (v1[i] * iw) + (v2[i] * w);
    }
}

void gen_velocity_generic(abcV3 *dst_pos, abcV3 *dst_vel,
    const abcV3 *p1, const abcV3 *p2, int num, float time_offset, float time_interval, float motion_scale)
{
    float inv_time_interval = 1.0f / time_interval;
    float s = inv_time_interval * motion_scale;
    for (int i = 0; i < num; ++i) {
        auto p = p1[i];
        auto distance = p2[i] - p;
        dst_pos[i] = p + distance * time_offset;
        dst_vel[i] = distance * s;
    }
}

void get_bounds_generic(abcV3 &dst_min, abcV3 &dst_max, const abcV3 *src, int num)
{
    if (num == 0) { return; }
    auto rmin = src[0];
    auto rmax = src[0];
    for (int i = 1; i < num; ++i) {
        auto t = src[i];
        rmin.x = std::min(rmin.x, t.x);
        rmin.y = std::min(rmin.y, t.y);
        rmin.z = std::min(rmin.z, t.z);
        rmax.x = std::max(rmax.x, t.x);
        rmax.y = std::max(rmax.y, t.y);
        rmax.z = std::max(rmax.z, t.z);
    }
    dst_min = rmin;
    dst_max = rmax;
}

void gen_normals_generic(abcV3 *dst, const abcV3 *points, const int *indices, int num_points, int num_triangles)
{
    memset(dst, 0, sizeof(abcV3)*num_points);
    for (int ti = 0; ti < num_triangles; ++ti) {
        int ti3 = ti * 3;
        auto p0 = points[indices[ti3 + 0]];
        auto p1 = points[indices[ti3 + 1]];
        auto p2 = points[indices[ti3 + 2]];
        auto n = (p1 - p0).cross(p2 - p0);

        for (int i = 0; i < 3; ++i) {
            dst[indices[ti3 + i]] += n;
        }
    }
    for (int vi = 0; vi < num_points; ++vi) {
        dst[vi] = dst[vi].normalized();
    }
}


inline float angle_between(const abcV3& a, const abcV3& b)
{
    return std::acos(a.dot(b));
}

inline float angle_between2(const abcV3& pos1, const abcV3& pos2, const abcV3& center)
{
    return angle_between(
        (pos1 - center).normalized(),
        (pos2 - center).normalized());
}

inline void compute_triangle_tangent(
    const abcV3(&vertices)[3], const abcV2(&uv)[3], abcV3(&dst_tangent)[3], abcV3(&dst_binormal)[3])
{
    auto p = vertices[1] - vertices[0];
    auto q = vertices[2] - vertices[0];
    auto s = abcV2{ uv[1].x - uv[0].x, uv[2].x - uv[0].x };
    auto t = abcV2{ uv[1].y - uv[0].y, uv[2].y - uv[0].y };

    float div = s.x * t.y - s.y * t.x;
    float area = abs(div);
    float rdiv = 1.0f / div;
    s *= rdiv;
    t *= rdiv;

    auto tangent = abcV3{
        t.y * p.x - t.x * q.x,
            t.y * p.y - t.x * q.y,
            t.y * p.z - t.x * q.z
    }.normalized() * area;
    auto binormal = abcV3{
        s.x * q.x - s.y * p.x,
            s.x * q.y - s.y * p.y,
            s.x * q.z - s.y * p.z
    }.normalized() * area;

    float angles[3] = {
        angle_between2(vertices[2], vertices[1], vertices[0]),
        angle_between2(vertices[0], vertices[2], vertices[1]),
        angle_between2(vertices[1], vertices[0], vertices[2]),
    };
    for (int v = 0; v < 3; ++v)
    {
        dst_tangent[v] = tangent * angles[v];
        dst_binormal[v] = binormal * angles[v];
    }
}

abcV4 orthogonalize_tangent(abcV3 tangent, abcV3 binormal, abcV3 normal)
{
    float NdotT = normal.dot(tangent);
    tangent -= normal * NdotT;
    float magT = tangent.length();
    tangent = tangent / magT;

    float NdotB = normal.dot(binormal);
    float TdotB = tangent.dot(binormal) * magT;
    binormal -= normal * NdotB - tangent * TdotB;;
    float magB = binormal.length();
    binormal = binormal / magB;

    float sign = normal.cross(tangent).dot(binormal) > 0.0f ? 1.0f : -1.0f;
    return { tangent.x, tangent.y, tangent.z, sign };
}

void gen_tangents_generic(abcV4 *dst,
    const abcV3 *points, const abcV2 *uv, const abcV3 *normals, const int *indices,
    int num_points, int num_triangles)
{
    RawVector<abcV3> tangents, binormals;
    tangents.resize_zeroclear(num_points);
    binormals.resize_zeroclear(num_points);

    for (int ti = 0; ti < num_triangles; ++ti) {
        int ti3 = ti * 3;
        int idx[3] = { indices[ti3 + 0], indices[ti3 + 1], indices[ti3 + 2] };
        abcV3 v[3] = { points[idx[0]], points[idx[1]], points[idx[2]] };
        abcV2 u[3] = { uv[idx[0]], uv[idx[1]], uv[idx[2]] };
        abcV3 t[3];
        abcV3 b[3];
        compute_triangle_tangent(v, u, t, b);

        for (int i = 0; i < 3; ++i) {
            tangents[idx[i]] += t[i];
            binormals[idx[i]] += b[i];
        }
    }

    for (int vi = 0; vi < num_points; ++vi) {
        dst[vi] = orthogonalize_tangent(tangents[vi], binormals[vi], normals[vi]);
    }
}


// > generic implementation


// SIMD can't make this faster
void swap_handedness(abcV3 *dst, int num)
{
    for (int i = 0; i < num; ++i) {
        dst[i].x *= -1.0f;
    }
}

#ifdef aiEnableISPC
    #define Impl(Func, ...) Func##_ispc(__VA_ARGS__)
#else
    #define Impl(Func, ...) Func##_generic(__VA_ARGS__)
#endif

void apply_scale(abcV3 *dst, int num, float scale)
{
    Impl(apply_scale, dst, num, scale);
}

void lerp(abcV3 *dst, const abcV3 *v1, const abcV3 *v2, int num, float w)
{
    Impl(lerp, dst, v1, v2, num, w);
}

void gen_velocity(abcV3 *dst_pos, abcV3 *dst_vel,
    const abcV3 *p1, const abcV3 *p2, int num, float time_offset, float time_interval, float motion_scale)
{
    Impl(gen_velocity, dst_pos, dst_vel, p1, p2, num, time_offset, time_interval, motion_scale);
}

void get_bounds(abcV3 &min, abcV3 &max, const abcV3 *points, int num)
{
    Impl(get_bounds, min, max, points, num);
}

void gen_normals(abcV3 *dst, const abcV3 *points, const int *indices, int num_points, int num_triangles)
{
    Impl(gen_normals, dst, points, indices, num_points, num_triangles);
}

void gen_tangents(abcV4 *dst,
    const abcV3 *points, const abcV2 *uv, const abcV3 *normals, const int *indices,
    int num_points, int num_triangles)
{
    Impl(gen_tangents, dst, points, uv, normals, indices, num_points, num_triangles);
}

#undef Impl


