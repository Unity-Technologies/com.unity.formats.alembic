#include "pch.h"
#include "aiMath.h"


// ispc implementation
#ifdef aiEnableISPC
#include "aiSIMD.h"

void scale_ispc(abcV3 *dst, float scale, int num)
{
    ispc::Scale((float*)dst, num*3, scale);
}

void lerp_ispc(abcV3 *dst, const abcV3 * v1, const abcV3 * v2, int num, float w)
{
    ispc::Lerp((float*)dst, (float*)v1, (float*)v2, num*3, w);
}

void gen_velocity_ispc(abcV3 * dst, const abcV3 * p1, const abcV3 * p2, int num, float time_interval, float motion_scale)
{
    ispc::GenerateVelocity((float*)dst, (float*)p1, (float*)p2, num, time_interval, motion_scale);
}

void get_bounds_ispc(abcV3 & min, abcV3 & max, const abcV3 * points, int num)
{
    ispc::MinMax3((ispc::float3&)min, (ispc::float3&)max, (const ispc::float3*)points, num);
}

void gen_normals_ispc(abcV3 * dst, const abcV3 * points, const int * indices, int num_points, int num_triangles)
{
    ispc::GenerateNormalsTriangleIndexed((ispc::float3*)dst, (const ispc::float3*)points, indices, num_triangles, num_points);
}
#endif // aiEnableISPC


// < generic implementation

void scale_generic(abcV3 *dst, float scale, int num)
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

void gen_velocity_generic(abcV3 *dst, const abcV3 *p1, const abcV3 *p2, int num, float time_interval, float motion_scale)
{
    float inv_time_interval = 1.0f / time_interval;
    float s = inv_time_interval * motion_scale;
    for (int i = 0; i < num; ++i) {
        dst[i] = (p2[i] - p1[i]) * s;
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

// > generic implementation


#ifdef aiEnableISPC
    #define Impl(Func, ...) Func##_ispc(__VA_ARGS__)
#else
    #define Impl(Func, ...) Func##_generic(__VA_ARGS__)
#endif

void scale(abcV3 *dst, float scale, int num)
{
    Impl(scale, dst, scale, num);
}

void lerp(abcV3 *dst, const abcV3 *v1, const abcV3 *v2, int num, float w)
{
    Impl(lerp, dst, v1, v2, num, w);
}

void gen_velocity(abcV3 *dst, const abcV3 *p1, const abcV3 *p2, int num, float time_interval, float motion_scale)
{
    Impl(gen_velocity, dst, p1, p2, num, time_interval, motion_scale);
}

void get_bounds(abcV3 &min, abcV3 &max, const abcV3 *points, int num)
{
    Impl(get_bounds, min, max, points, num);
}

void gen_normals(abcV3 *dst, const abcV3 *points, const int *indices, int num_points, int num_triangles)
{
    Impl(gen_normals, dst, points, indices, num_points, num_triangles);
}
#undef Impl
