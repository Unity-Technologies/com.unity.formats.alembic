#pragma once

void swap_handedness(abcV3 *dst, int num);
void apply_scale(abcV3 *dst, int num, float scale);
void lerp(abcV3 *dst, const abcV3 *v1, const abcV3 *v2, int num, float w);
void gen_velocity(abcV3 *dst, const abcV3 *p1, const abcV3 *p2, int num, float time_interval, float motion_scale);
void get_bounds(abcV3& min, abcV3& max, const abcV3 *points, int num);
void gen_normals(abcV3 *dst, const abcV3 *points, const int *indices, int num_points, int num_triangles);
void gen_tangents(abcV4 *dst,
    const abcV3 *points, const abcV2 *uv, const abcV3 *normals, const int *indices,
    int num_points, int num_triangles);

// for test and debug
void apply_scale_generic(abcV3 *dst, int num, float scale);
void apply_scale_ispc(abcV3 *dst, int num, float scale);
void lerp_generic(abcV3 *dst, const abcV3 *v1, const abcV3 *v2, int num, float w);
void lerp_ispc(abcV3 *dst, const abcV3 *v1, const abcV3 *v2, int num, float w);
void gen_velocity_generic(abcV3 *dst, const abcV3 *p1, const abcV3 *p2, int num, float time_interval, float motion_scale);
void gen_velocity_ispc(abcV3 *dst, const abcV3 *p1, const abcV3 *p2, int num, float time_interval, float motion_scale);
void get_bounds_generic(abcV3& min, abcV3& max, const abcV3 *points, int num);
void get_bounds_ispc(abcV3& min, abcV3& max, const abcV3 *points, int num);
void gen_normals_generic(abcV3 *dst, const abcV3 *points, const int *indices, int num_points, int num_triangles);
void gen_normals_ispc(abcV3 *dst, const abcV3 *points, const int *indices, int num_points, int num_triangles);
void gen_tangents_generic(abcV4 *dst,
    const abcV3 *points, const abcV2 *uv, const abcV3 *normals, const int *indices,
    int num_points, int num_triangles);
void gen_tangents_ispc(abcV4 *dst,
    const abcV3 *points, const abcV2 *uv, const abcV3 *normals, const int *indices,
    int num_points, int num_triangles);
