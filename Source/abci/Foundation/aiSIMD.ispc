#define ispcmathEstimate
#include "ispcmath.h"

export void Scale(uniform
float dst[], uniform
const int num, uniform
const float scale
)
{
const uniform int num_loops = num / (C * 4);

{
uniform float* uniform
fv = (uniform
float* uniform
)
dst;
for(
uniform int i = 0;
i<num_loops;
++i) {
uniform int i4 = i * 4;
fv[
C* (i4
+0) + I] = fv[
C* (i4
+0) + I] *
scale;
fv[
C* (i4
+1) + I] = fv[
C* (i4
+1) + I] *
scale;
fv[
C* (i4
+2) + I] = fv[
C* (i4
+2) + I] *
scale;
fv[
C* (i4
+3) + I] = fv[
C* (i4
+3) + I] *
scale;
}
}

for(
uniform int i = num_loops * (C * 4);
i<num;
++i) {
dst[i] *=
scale;
}
}

export void Normalize(uniform
float3 dst[], uniform
const int num
)
{
uniform int num_simd = num & ~(C - 1);

for (
uniform int bi = 0;
bi<num_simd;
bi += C) {
float3 n;
aos_to_soa3((uniform
float*)&dst[bi], &n.x, &n.y, &n.z);
n = normalize(n);
soa_to_aos3(n
.x, n.y, n.z, (uniform float*)&dst[bi]);
}

for (
uniform int i = num_simd;
i<num;
++i) {
dst[i] =
normalize(dst[i]);
}
}

export void MinMax3(
    uniform
float3& dst_min,
    uniform
float3& dst_max,
    uniform
const float3 src[], uniform
const int num
)
{
if(num == 0) {
return; }

uniform float3
rmin = src[0], rmax = src[0];

const uniform int block_size = C;
const uniform int num_loops = num / block_size;
if(num_loops > 1) {
const uniform float* uniform
fv =
(
const uniform float* uniform
)
src;
uniform float tmin[3][C];
uniform float tmax[3][C];
tmin[0][I] = tmax[0][I] = fv[C*0 + I];
tmin[1][I] = tmax[1][I] = fv[C*1 + I];
tmin[2][I] = tmax[2][I] = fv[C*2 + I];

for(
uniform int i = 1;
i<num_loops;
++i) {
uniform const int i3 = i * 3;

float _0 = fv[C * (i3 + 0) + I];
tmin[0][I] =
min(tmin[0][I], _0
);
tmax[0][I] =
max(tmax[0][I], _0
);

float _1 = fv[C * (i3 + 1) + I];
tmin[1][I] =
min(tmin[1][I], _1
);
tmax[1][I] =
max(tmax[1][I], _1
);

float _2 = fv[C * (i3 + 2) + I];
tmin[2][I] =
min(tmin[2][I], _2
);
tmax[2][I] =
max(tmax[2][I], _2
);
}

float x, y, z;
aos_to_soa3((uniform
float*)&tmin[0], &x, &y, &z);
rmin.
x = reduce_min(x);
rmin.
y = reduce_min(y);
rmin.
z = reduce_min(z);

aos_to_soa3((uniform
float*)&tmax[0], &x, &y, &z);
rmax.
x = reduce_max(x);
rmax.
y = reduce_max(y);
rmax.
z = reduce_max(z);
}

for(
uniform int i = num_loops * block_size;
i<num;
++i) {
uniform float3
t = src[i];
rmin.
x = min(rmin.x, t.x);
rmin.
y = min(rmin.y, t.y);
rmin.
z = min(rmin.z, t.z);
rmax.
x = max(rmax.x, t.x);
rmax.
y = max(rmax.y, t.y);
rmax.
z = max(rmax.z, t.z);
}

dst_min = rmin;
dst_max = rmax;
}

export void Lerp(uniform
float dst[], uniform
const float src1[], uniform
const float src2[], uniform
const int num, uniform
float w
)
{
uniform float iw = 1.0f - w;
foreach(i = 0
... num) {
dst[i] = src1[i] * iw + src2[i] *
w;
}
}

export void GenerateVelocities(
    uniform
float3 dst[],
    uniform
const float3 p1[],
    uniform
const float3 p2[],
    uniform
const int num,
    uniform
float motion_scale
)
{
foreach(i = 0
... num) {
dst[i] = (p2[i] - p1[i]) *
motion_scale;
}
}

static inline void NormalizeSoAToAoS(uniform
float3 dst[],
    uniform
float srcx[], uniform
float srcy[], uniform
float srcz[], uniform
const int num
)
{
uniform int num_simd = num & ~(C - 1);

for (
uniform int bi = 0;
bi<num_simd;
bi += C) {
int i = bi + I;
float3 n = { srcx[i], srcy[i], srcz[i] };
n = normalize(n);
soa_to_aos3(n
.x, n.y, n.z, (uniform float*)&dst[bi]);
}

for (
uniform int i = num_simd;
i<num;
++i) {
uniform float3
n = { srcx[i], srcy[i], srcz[i] };
dst[i] =
normalize(n);
}
}

export void GeneratePointNormals(uniform const int face_start_offsets[],
    uniform const int face_vertex_counts[],
    uniform const int face_indices[],
    uniform const float positions[],
    uniform float normals[],
    uniform const int remapped_indices[],
    uniform const int face_count,
    uniform const int remap_count,
    uniform const int orig_point_count)
{
    float* uniform
    tmp_normals = uniform
    new float[orig_point_count * 3];
    zeroclear(tmp_normals, orig_point_count * 3);
    foreach(face_index = 0 ...
    face_count) {
        int face_vert_count = face_vertex_counts[face_index];
        int face_start_index = face_start_offsets[face_index];
        if (face_vert_count < 3)
        {
            continue;
        }
        for (int tri = 0; tri < face_vert_count - 2; ++tri)
        {
            int i1 = face_indices[face_start_index + tri];
            int i2 = face_indices[face_start_index + tri + 1];
            int i3 = face_indices[face_start_index + tri + 2];

            float3 p1 = {
                positions[i1 * 3],
                positions[i1 * 3 + 1],
                positions[i1 * 3 + 2]
            };
            float3 p2 = {
                positions[i2 * 3],
                positions[i2 * 3 + 1],
                positions[i2 * 3 + 2]
            };
            float3 p3 = {
                positions[i3 * 3],
                positions[i3 * 3 + 1],
                positions[i3 * 3 + 2]
            };
            float3 tri_normal = cross(p3 - p1, p2 - p1);
            tmp_normals[i1 * 3] += tri_normal.x;
            tmp_normals[i1 * 3 + 1] += tri_normal.y;
            tmp_normals[i1 * 3 + 2] += tri_normal.z;

            tmp_normals[i2 * 3] += tri_normal.x;
            tmp_normals[i2 * 3 + 1] += tri_normal.y;
            tmp_normals[i2 * 3 + 2] += tri_normal.z;

            tmp_normals[i3 * 3] += tri_normal.x;
            tmp_normals[i3 * 3 + 1] += tri_normal.y;
            tmp_normals[i3 * 3 + 2] += tri_normal.z;
        }
    }
    foreach(index = 0 ...
    remap_count) {
        int remap = remapped_indices[index];
        float3 n = {
            tmp_normals[remap * 3],
            tmp_normals[remap * 3 + 1],
            tmp_normals[remap * 3 + 2]
        };
        n = normalize(n);
        normals[index * 3] = -n.x;
        normals[index * 3 + 1] = n.y;
        normals[index * 3 + 2] = n.z;
    }
    delete[] tmp_normals;
}

#if 0
export void GenerateNormalsPolygonIndexed(uniform float3 dst[],
    uniform const float3 points[], uniform const int indices[], uniform const int counts[], uniform const int offsets[],
    uniform const int num_points, uniform const int num_faces)
{
    uniform int num_points_aligned = (num_points + (C - 1)) & ~(C - 1);
    float * uniform mem_tmp = uniform new float[num_points_aligned * 3];
    zeroclear(mem_tmp, num_points_aligned * 3);
    float * uniform tnx = mem_tmp + num_points_aligned * 0;
    float * uniform tny = mem_tmp + num_points_aligned * 1;
    float * uniform tnz = mem_tmp + num_points_aligned * 2;

    uniform int num_faces_simd = num_faces & ~(C - 1);

    // SIMD pass
    for (uniform int bi = 0; bi < num_faces_simd; bi += C) {
        int fi = bi + I;
        int count = counts[fi];
        if (count < 3) { continue; }

        int offset = offsets[fi];
        int num_trignales = count - 2;

        float3 n = float3_(0,0,0);
        float3 p0 = points[indices[offset]];
        for (int ti = 0; ti < num_trignales; ++ti) {
            float3 p1 = points[indices[offset + ti + 1]];
            float3 p2 = points[indices[offset + ti + 2]];
            n = n + cross(p1 - p0, p2 - p0);
        }
        if (count > 3) {
            n = normalize(n);
        }

        for (uniform int ci = 0; ci<C; ++ci) {
            uniform float3 cn = { extract(n.x, ci), extract(n.y, ci), extract(n.z, ci) };
            uniform int coffset = offsets[bi + ci];
            for (uniform int i = 0; i<count; ++i) {
                uniform int ix = indices[coffset + i];
                tnx[ix] += cn.x;
                tny[ix] += cn.y;
                tnz[ix] += cn.z;
            }
        }
    }

    // non-SIMD pass
    for (uniform int fi = num_faces_simd; fi < num_faces; ++fi) {
        uniform int count = counts[fi];
        if (count < 3) { continue; }

        uniform int offset = offsets[fi];
        uniform int num_trignales = count - 2;

        uniform float3 n = float3_(0, 0, 0);
        uniform float3 p0 = points[indices[offset]];
        for (uniform int ti = 0; ti < num_trignales; ++ti) {
            uniform float3 p1 = points[indices[offset + ti + 1]];
            uniform float3 p2 = points[indices[offset + ti + 2]];
            n = n + cross(p1 - p0, p2 - p0);
        }
        if (count > 3) {
            n = normalize(n);
        }

        for (uniform int i = 0; i<count; ++i) {
            uniform int ix = indices[offset + i];
            tnx[ix] += n.x;
            tny[ix] += n.y;
            tnz[ix] += n.z;
        }
    }

    NormalizeSoAToAoS(dst, tnx, tny, tnz, num_points);
    delete[] mem_tmp;
}
#endif

export void GenerateTangentsTriangleIndexed(uniform
float4 dst[],
    uniform
const float3 points[], uniform
const float2 uv[], uniform
const float3 normals[], uniform
const int indices[],
    uniform
const int num_points, uniform
const int num_triangles
)
{
uniform int num_points_aligned = (num_points + (C - 1)) & ~(C - 1);
float* uniform
mem_tmp = uniform
new float[num_points_aligned * 6];
zeroclear(mem_tmp, num_points_aligned
* 6);
float* uniform
ttx = mem_tmp + num_points_aligned * 0;
float* uniform
tty = mem_tmp + num_points_aligned * 1;
float* uniform
ttz = mem_tmp + num_points_aligned * 2;
float* uniform
tbx = mem_tmp + num_points_aligned * 3;
float* uniform
tby = mem_tmp + num_points_aligned * 4;
float* uniform
tbz = mem_tmp + num_points_aligned * 5;

uniform int num_triangles_simd = num_triangles & ~(C - 1);
uniform int num_points_simd = num_points & ~(C - 1);

// SIMD pass
for(
uniform int bi = 0;
bi<num_triangles_simd;
bi += C) {
int ti = bi + I;
int ti3 = ti * 3;
float3 v[3] = {
    points[indices[ti3 + 0]],
    points[indices[ti3 + 1]],
    points[indices[ti3 + 2]],
};
float2 u[3] = {
    uv[indices[ti3 + 0]],
    uv[indices[ti3 + 1]],
    uv[indices[ti3 + 2]],
};
float3 t[3];
float3 b[3];
compute_triangle_tangents(v, u, t, b
);

for(
uniform int ci = 0;
ci<C;
++ci) {
for(
uniform int i = 0;
i<3; ++i) {
uniform int ix = indices[(bi + ci) * 3 + i];
ttx[ix]+=
extract(t[i]
.x, ci); tty[ix]+=
extract(t[i]
.y, ci); ttz[ix]+=
extract(t[i]
.z, ci);
tbx[ix]+=
extract(b[i]
.x, ci); tby[ix]+=
extract(b[i]
.y, ci); tbz[ix]+=
extract(b[i]
.z, ci);
}
}
}

// non-SIMD pass
for(
uniform int ti = num_triangles_simd;
ti<num_triangles;
++ti) {
uniform int ti3 = ti * 3;
uniform float3
v[3] = {
points[indices[ti3  + 0]],
points[indices[ti3  + 1]],
points[indices[ti3  + 2]],
};
uniform float2
u[3] = {
uv[indices[ti3  + 0]],
uv[indices[ti3  + 1]],
uv[indices[ti3  + 2]],
};
uniform float3
t[3];
uniform float3
b[3];
compute_triangle_tangents(v, u, t, b
);

for(
uniform int i = 0;
i<3; ++i) {
uniform int ix = indices[ti3 + i];
ttx[ix]+=t[i].
x;
tty[ix]+=t[i].
y;
ttz[ix]+=t[i].
z;
tbx[ix]+=b[i].
x;
tby[ix]+=b[i].
y;
tbz[ix]+=b[i].
z;
}
}


// SIMD pass
for(
uniform int bi = 0;
bi<num_points_simd;
bi += C) {
int vi = bi + I;
float3 t = float3_(ttx[vi], tty[vi], ttz[vi]);
float3 b = float3_(tbx[vi], tby[vi], tbz[vi]);
float3 n;
aos_to_soa3((uniform
float*)&normals[bi], &n.x, &n.y, &n.z);

float4 result = orthogonalize_tangent(t, b, n);
soa_to_aos4(result
.x, result.y, result.z, result.w, (uniform float*)&dst[bi]);
}

// non-SIMD pass
for(
uniform int vi = num_points_simd;
vi<num_points;
++vi) {
uniform float3
t = float3_(ttx[vi], tty[vi], ttz[vi]);
uniform float3
b = float3_(tbx[vi], tby[vi], tbz[vi]);
uniform float3
n = normals[vi];
dst[vi] =
orthogonalize_tangent(t, b, n
);
}

delete[]
mem_tmp;
}

#if 0
export void GenerateTangentsPolygonIndexed(uniform float4 dst[],
    uniform const float3 points[], uniform const float2 uv[], uniform const float3 normals[],
    uniform const int indices[], uniform const int counts[], uniform const int offsets[],
    uniform const int num_points, uniform const int num_faces)
{
    uniform int num_points_aligned = (num_points + (C - 1)) & ~(C - 1);
    float * uniform mem_tmp = uniform new float[num_points_aligned * 6];
    zeroclear(mem_tmp, num_points_aligned * 6);
    float * uniform ttx = mem_tmp + num_points_aligned * 0;
    float * uniform tty = mem_tmp + num_points_aligned * 1;
    float * uniform ttz = mem_tmp + num_points_aligned * 2;
    float * uniform tbx = mem_tmp + num_points_aligned * 3;
    float * uniform tby = mem_tmp + num_points_aligned * 4;
    float * uniform tbz = mem_tmp + num_points_aligned * 5;

    uniform int num_faces_simd = num_faces & ~(C - 1);
    uniform int num_points_simd = num_points & ~(C - 1);

    // SIMD pass
    for (uniform int bi = 0; bi < num_faces_simd; bi += C) {
        int fi = bi + I;
        int count = counts[fi];
        if (count < 3) { continue; }

        int offset = offsets[fi];
        int num_trignales = count - 2;

        float3 v[3], t[3], b[3];
        float2 u[3];
        v[0] = points[indices[offset]];
        u[0] = uv[indices[offset]];
        for (int ti = 0; ti < num_trignales; ++ti) {
            v[1] = points[indices[offset + ti + 1]];
            v[2] = points[indices[offset + ti + 2]];
            u[1] = uv[indices[offset + ti + 1]];
            u[2] = uv[indices[offset + ti + 2]];
            compute_triangle_tangents(v, u, t, b);

            for (uniform int ci = 0; ci<C; ++ci) {
                uniform int coffset = offsets[bi + ci];
                for (uniform int i = 0; i<count; ++i) {
                    uniform int ix = indices[coffset + i];
                    ttx[ix] += extract(t[i].x, ci); tty[ix] += extract(t[i].y, ci); ttz[ix] += extract(t[i].z, ci);
                    tbx[ix] += extract(b[i].x, ci); tby[ix] += extract(b[i].y, ci); tbz[ix] += extract(b[i].z, ci);
                }
            }
        }
    }

    // non-SIMD pass
    for (uniform int fi = num_faces_simd; fi < num_faces; ++fi) {
        uniform int count = counts[fi];
        if (count < 3) { continue; }

        uniform int offset = offsets[fi];
        uniform int num_trignales = count - 2;

        uniform float3 v[3], t[3], b[3];
        uniform float2 u[3];
        v[0] = points[indices[offset]];
        u[0] = uv[indices[offset]];
        for (uniform int ti = 0; ti < num_trignales; ++ti) {
            v[1] = points[indices[offset + ti + 1]];
            v[2] = points[indices[offset + ti + 2]];
            u[1] = uv[indices[offset + ti + 1]];
            u[2] = uv[indices[offset + ti + 2]];
            compute_triangle_tangents(v, u, t, b);

            for (uniform int i = 0; i<count; ++i) {
                uniform int ix = indices[offset + i];
                ttx[ix] += t[i].x; tty[ix] += t[i].y; ttz[ix] += t[i].z;
                tbx[ix] += b[i].x; tby[ix] += b[i].y; tbz[ix] += b[i].z;
            }
        }
    }


    // SIMD pass
    for (uniform int bi = 0; bi < num_points_simd; bi += C) {
        int vi = bi + I;
        float3 t = float3_(ttx[vi], tty[vi], ttz[vi]);
        float3 b = float3_(tbx[vi], tby[vi], tbz[vi]);
        float3 n; aos_to_soa3((uniform float*)&normals[bi], &n.x, &n.y, &n.z);

        float4 result = orthogonalize_tangent(t, b, n);
        soa_to_aos4(result.x, result.y, result.z, result.w, (uniform float*)&dst[bi]);
    }

    // non-SIMD pass
    for (uniform int vi = num_points_simd; vi < num_points; ++vi) {
        uniform float3 t = float3_(ttx[vi], tty[vi], ttz[vi]);
        uniform float3 b = float3_(tbx[vi], tby[vi], tbz[vi]);
        uniform float3 n = normals[vi];
        dst[vi] = orthogonalize_tangent(t, b, n);
    }

    delete[] mem_tmp;
}
#endif
