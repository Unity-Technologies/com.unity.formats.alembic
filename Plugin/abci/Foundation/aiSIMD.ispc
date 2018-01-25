#include "ispcmath.h"

export void Scale(uniform float dst[], uniform const int num, uniform const float scale)
{
    const uniform int num_loops = num / (C*4);

    {
        uniform float * uniform fv = (uniform float * uniform)dst;
        for(uniform int i=0; i < num_loops; ++i) {
            uniform int i4 = i*4;
            fv[C*(i4+0) + I] = fv[C*(i4+0) + I] * scale;
            fv[C*(i4+1) + I] = fv[C*(i4+1) + I] * scale;
            fv[C*(i4+2) + I] = fv[C*(i4+2) + I] * scale;
            fv[C*(i4+3) + I] = fv[C*(i4+3) + I] * scale;
        }
    }

    for(uniform int i=num_loops*(C*4); i < num; ++i) {
        dst[i] *= scale;
    }
}

export void InvertX3(uniform float3 dst[], uniform const int num)
{
    const uniform int num_loops = num / C;

    {
        uniform float _c[3][C];
        _c[0][I] = select((C*0 + I)%3==0, -1.0f, 1.0f);
        _c[1][I] = select((C*1 + I)%3==0, -1.0f, 1.0f);
        _c[2][I] = select((C*2 + I)%3==0, -1.0f, 1.0f);

        uniform float * uniform fv = (uniform float * uniform)dst;
        for(uniform int i=0; i < num_loops; ++i) {
            uniform int i3 = i*3;
            fv[C*(i3+0) + I] = fv[C*(i3+0) + I] * _c[0][I];
            fv[C*(i3+1) + I] = fv[C*(i3+1) + I] * _c[1][I];
            fv[C*(i3+2) + I] = fv[C*(i3+2) + I] * _c[2][I];
        }
    }

    for(uniform int i=num_loops*C; i < num; ++i) {
        dst[i].x *= -1.0f;
    }
}

export void MinMax3(
    uniform float3& dst_min,
    uniform float3& dst_max,
    uniform const float3 src[], uniform const int num)
{
    if(num == 0) { return; }

    uniform float3 rmin = src[0], rmax = src[0];

    const uniform int block_size = C;
    const uniform int num_loops = num / block_size;
    if(num_loops > 1) {
        const uniform float * uniform fv = (const uniform float * uniform)src;
        uniform float tmin[3][C];
        uniform float tmax[3][C];
        tmin[0][I] = tmax[0][I] = fv[C*0 + I];
        tmin[1][I] = tmax[1][I] = fv[C*1 + I];
        tmin[2][I] = tmax[2][I] = fv[C*2 + I];

        for(uniform int i=1; i < num_loops; ++i) {
            uniform const int i3 = i*3;

            float _0 = fv[C*(i3+0) + I];
            tmin[0][I] = min(tmin[0][I], _0);
            tmax[0][I] = max(tmax[0][I], _0);

            float _1 = fv[C*(i3+1) + I];
            tmin[1][I] = min(tmin[1][I], _1);
            tmax[1][I] = max(tmax[1][I], _1);

            float _2 = fv[C*(i3+2) + I];
            tmin[2][I] = min(tmin[2][I], _2);
            tmax[2][I] = max(tmax[2][I], _2);
        }

        float x,y,z;
        aos_to_soa3((uniform float*)&tmin[0], &x, &y, &z);
        rmin.x = reduce_min(x);
        rmin.y = reduce_min(y);
        rmin.z = reduce_min(z);

        aos_to_soa3((uniform float*)&tmax[0], &x, &y, &z);
        rmax.x = reduce_max(x);
        rmax.y = reduce_max(y);
        rmax.z = reduce_max(z);
    }

    for(uniform int i=num_loops*block_size; i < num; ++i) {
        uniform float3 t = src[i];
        rmin.x = min(rmin.x, t.x);
        rmin.y = min(rmin.y, t.y);
        rmin.z = min(rmin.z, t.z);
        rmax.x = max(rmax.x, t.x);
        rmax.y = max(rmax.y, t.y);
        rmax.z = max(rmax.z, t.z);
    }

    dst_min = rmin;
    dst_max = rmax;
}

export void Lerp(
    uniform float dst[],
    uniform const float p1[],
    uniform const float p2[],
    uniform const int num,
    uniform float weight)
{
    const uniform float inv_weight = 1.0f - weight;

    const uniform int block_size = C;
    const uniform int num_blocks = num / block_size;

    // SIMD pass
    for (uniform int bi = 0; bi < num_blocks; ++bi) {
        const int i = C * bi + I;
        dst[i] = (p1[i] * inv_weight) + (p2[i] * weight);
    }

    // non-SIMD pass
    for (uniform int i = num_blocks * block_size; i < num; ++i) {
        dst[i] = (p1[i] * inv_weight) + (p2[i] * weight);
    }
}

export void GenerateVelocity(
    uniform float dst[],
    uniform const float p1[],
    uniform const float p2[],
    uniform const int num,
    uniform float time_interval,
    uniform float motion_scale)
{
    const uniform float inv_time_interval = rcp(time_interval);
    const uniform float s = inv_time_interval * motion_scale;

    const uniform int block_size = C;
    const uniform int num_blocks = num / block_size;

    // SIMD pass
    for (uniform int bi = 0; bi < num_blocks; ++bi) {
        const int i = C * bi + I;
        dst[i] = (p2[i] - p1[i]) * s;
    }

    // non-SIMD pass
    for (uniform int i = num_blocks * block_size; i < num; ++i) {
        dst[i] = (p2[i] - p1[i]) * s;
    }
}

static inline void NormalizeSoAToAoS(uniform float3 dst[],
    uniform float srcx[], uniform float srcy[], uniform float srcz[], uniform const int num)
{
    uniform int num_simd = num & ~(C - 1);

    for (uniform int bi = 0; bi < num_simd; bi += C) {
        int i = bi + I;
        float3 n = { srcx[i], srcy[i], srcz[i] };
        n = normalize(n);
        soa_to_aos3(n.x, n.y, n.z, (uniform float*)&dst[bi]);
    }

    for (uniform int i = num_simd; i < num; ++i) {
        uniform float3 n = { srcx[i], srcy[i], srcz[i] };
        dst[i] = normalize(n);
    }
}

export void GenerateNormalsTriangleIndexed(uniform float3 dst[],
    uniform const float3 vertices[], uniform const int indices[],
    uniform const int num_triangles, uniform const int num_vertices)
{
    uniform int num_vertices_aligned = (num_vertices + (C - 1)) & ~(C - 1);
    float * uniform mem_tmp = uniform new float[num_vertices_aligned * 3];
    zeroclear(mem_tmp, num_vertices_aligned * 3);
    float * uniform tnx = mem_tmp + num_vertices_aligned * 0;
    float * uniform tny = mem_tmp + num_vertices_aligned * 1;
    float * uniform tnz = mem_tmp + num_vertices_aligned * 2;

    uniform int num_triangles_simd = num_triangles & ~(C - 1);

    // SIMD pass
    for(uniform int bi=0; bi < num_triangles_simd; bi += C) {
        int ti = bi+I;
        int ti3 = ti * 3;
        float3 p0 = vertices[indices[ti3  + 0]];
        float3 p1 = vertices[indices[ti3  + 1]];
        float3 p2 = vertices[indices[ti3  + 2]];
        float3 n = cross(p1 - p0, p2 - p0);

        for(uniform int ci=0; ci<C; ++ci) {
            uniform float3 cn = {extract(n.x, ci), extract(n.y, ci), extract(n.z, ci)};
            for(uniform int i=0; i<3; ++i) {
                uniform int ix = indices[(bi + ci) * 3 + i];
                tnx[ix] += cn.x;
                tny[ix] += cn.y;
                tnz[ix] += cn.z;
            }
        }
    }

    // non-SIMD pass
    for(uniform int ti=num_triangles_simd; ti < num_triangles; ++ti) {
        uniform int ti3 = ti * 3;
        uniform int i0 = indices[ti3 + 0];
        uniform int i1 = indices[ti3 + 1];
        uniform int i2 = indices[ti3 + 2];
        uniform float3 p0 = vertices[i0];
        uniform float3 p1 = vertices[i1];
        uniform float3 p2 = vertices[i2];
        uniform float3 n = cross(p1 - p0, p2 - p0);

        for(uniform int i=0; i<3; ++i) {
            uniform int ix = indices[ti3 + i];
            tnx[ix] += n.x;
            tny[ix] += n.y;
            tnz[ix] += n.z;
        }
    }

    NormalizeSoAToAoS(dst, tnx, tny, tnz, num_vertices);
    delete[] mem_tmp;
}

export void GenerateTangentsTriangleIndexed(uniform float4 dst[],
    uniform const float3 vertices[], uniform const float2 uv[], uniform const float3 normals[], uniform const int indices[],
    uniform const int num_triangles, uniform const int num_vertices)
{
    uniform int num_vertices_aligned = (num_vertices + (C - 1)) & ~(C - 1);
    float * uniform mem_tmp = uniform new float[num_vertices_aligned * 6];
    zeroclear(mem_tmp, num_vertices_aligned * 6);
    float * uniform ttx = mem_tmp + num_vertices_aligned * 0;
    float * uniform tty = mem_tmp + num_vertices_aligned * 1;
    float * uniform ttz = mem_tmp + num_vertices_aligned * 2;
    float * uniform tbx = mem_tmp + num_vertices_aligned * 3;
    float * uniform tby = mem_tmp + num_vertices_aligned * 4;
    float * uniform tbz = mem_tmp + num_vertices_aligned * 5;

    uniform int num_triangles_simd = num_triangles & ~(C - 1);
    uniform int num_vertices_simd = num_vertices & ~(C - 1);

    // SIMD pass
    for(uniform int bi=0; bi < num_triangles_simd; bi += C) {
        int ti = bi+I;
        int ti3 = ti*3;
        float3 v[3] = {
             vertices[indices[ti3  + 0]],
             vertices[indices[ti3  + 1]],
             vertices[indices[ti3  + 2]],
        };
        float2 u[3] = {
             uv[indices[ti3  + 0]],
             uv[indices[ti3  + 1]],
             uv[indices[ti3  + 2]],
        };
        float3 t[3];
        float3 b[3];
        compute_triangle_tangents(v, u, t, b);

        for(uniform int ci=0; ci<C; ++ci) {
            for(uniform int i=0; i<3; ++i) {
                uniform int ix = indices[(bi+ci)*3 + i];
                ttx[ix]+=extract(t[i].x, ci); tty[ix]+=extract(t[i].y, ci); ttz[ix]+=extract(t[i].z, ci);
                tbx[ix]+=extract(b[i].x, ci); tby[ix]+=extract(b[i].y, ci); tbz[ix]+=extract(b[i].z, ci);
            }
        }
    }

    // non-SIMD pass
    for(uniform int ti=num_triangles_simd; ti < num_triangles; ++ti) {
        uniform int ti3 = ti*3;
        uniform float3 v[3] = {
             vertices[indices[ti3  + 0]],
             vertices[indices[ti3  + 1]],
             vertices[indices[ti3  + 2]],
        };
        uniform float2 u[3] = {
             uv[indices[ti3  + 0]],
             uv[indices[ti3  + 1]],
             uv[indices[ti3  + 2]],
        };
        uniform float3 t[3];
        uniform float3 b[3];
        compute_triangle_tangents(v, u, t, b);

        for(uniform int i=0; i<3; ++i) {
            uniform int ix = indices[ti3 + i];
            ttx[ix]+=t[i].x; tty[ix]+=t[i].y; ttz[ix]+=t[i].z;
            tbx[ix]+=b[i].x; tby[ix]+=b[i].y; tbz[ix]+=b[i].z;
        }
    }


    // SIMD pass
    for(uniform int bi=0; bi < num_vertices_simd; bi += C) {
        int vi = bi+I;
        float3 t = float3_(ttx[vi], tty[vi], ttz[vi]);
        float3 b = float3_(tbx[vi], tby[vi], tbz[vi]);
        float3 n; aos_to_soa3((uniform float*)&normals[bi], &n.x, &n.y, &n.z);

        float4 result = orthogonalize_tangent(t, b, n);
        soa_to_aos4(result.x, result.y, result.z, result.w, (uniform float*)&dst[bi]);
    }

    // non-SIMD pass
    for(uniform int vi=num_vertices_simd; vi < num_vertices; ++vi) {
        uniform float3 t = float3_(ttx[vi], tty[vi], ttz[vi]);
        uniform float3 b = float3_(tbx[vi], tby[vi], tbz[vi]);
        uniform float3 n = normals[vi];
        dst[vi] = orthogonalize_tangent(t, b, n);
    }

    delete[] mem_tmp;
}
