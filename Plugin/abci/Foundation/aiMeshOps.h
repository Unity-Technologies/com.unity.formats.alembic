#pragma once
#include "RawVector.h"
#include "aiIntrusiveArray.h"
#include "aiMath.h"


struct ConnectionData
{
    RawVector<int> v2f_counts;
    RawVector<int> v2f_offsets;
    RawVector<int> v2f_faces;
    RawVector<int> v2f_indices;

    RawVector<int> weld_map;
    RawVector<int> weld_counts;
    RawVector<int> weld_offsets;
    RawVector<int> weld_indices;

    void clear();
    void buildConnection(
        const IArray<int>& indices, const IArray<int>& counts, const IArray<float3>& vertices);

    // Body: [](int face_index, int index_index) -> void
    template<class Body>
    void eachConnectedFaces(int vi, const Body& body) const
    {
        int count = v2f_counts[vi];
        int offset = v2f_offsets[vi];
        for (int i = 0; i < count; ++i) {
            body(v2f_faces[offset + i], v2f_indices[offset + i]);
        }
    }

    // Body: [](int vertex_index) -> void
    template<class Body>
    void eachWeldedVertices(int vi, const Body& body) const
    {
        int count = weld_counts[vi];
        int offset = weld_offsets[vi];
        for (int i = 0; i < count; ++i) {
            body(weld_indices[offset + i]);
        }
    }
};


class MeshWelder
{
public:

    // compare_op: [](int vertex_index, int another_vertex_index) -> bool
    // weld_op: [](int vertex_index, int new_vertex_index) -> void
    template<class Compare, class Welder>
    int weld(abcV3 *points, int count, const Compare& compare_op, const Welder& weld_op);

    const RawVector<int>& getRemapTable() const;

private:
    RawVector<int> m_hash_table;
    RawVector<int> m_remap;
    abcV3 *m_points = nullptr;
    int m_count = 0;
    int m_hash_size = 0;

    static const int NIL = -1;
    void prepare(abcV3 *points, int count);
    static uint32_t hash(const abcV3& v);
};


struct MeshRefiner
{
    struct Submesh
    {
        int offset_indices = 0;
        int num_indices = 0; // triangulated
        int materialID = 0;
        int* faces_to_write = nullptr;
    };

    struct Split
    {
        int offset_vertices = 0;
        int offset_indices = 0;
        int offset_faces = 0;
        int offset_submeshes = 0;
        int num_vertices = 0;
        int num_indices = 0;
        int num_faces = 0;
        int num_indices_triangulated = 0;
        int num_submeshes = 0;
    };

    // inputs
    int split_unit = 0; // 0 == no split
    IArray<int> counts;
    IArray<int> indices;
    IArray<int> normal_indices;
    IArray<int> uv_indices;
    IArray<float3> points;
    IArray<float3> normals;
    IArray<float2> uvs;

    // outputs
    RawVector<int> old2new_indices; // old indices to new indices
    RawVector<int> new2old_points;  // new indices to old vertex data
    RawVector<int> new2old_normals; // 
    RawVector<int> new2old_uvs;     // 
    RawVector<int> new_indices;
    RawVector<int> new_indices_triangulated;
    RawVector<int> new_indices_submeshes;
    RawVector<float3> new_points;
    RawVector<float3> new_normals;
    RawVector<float2> new_uvs;
    RawVector<Split> splits;
    RawVector<Submesh> submeshes;
    ConnectionData connection;
    int num_new_indices = 0;

public:
    void refine();
    void triangulate(bool swap_faces);
    void genSubmeshes(IArray<int> materialIDs);
    void clear();

private:
    void buildConnection();

    template<class Body> void doRefine(const Body& body);
    template<class Hook> int findOrAddVertexPNU(int vi, const float3& p, const float3& n, const float2& u, const Hook& hook);
    template<class Hook> int findOrAddVertexPN(int vi, const float3& p, const float3& n, const Hook& hook);
    template<class Hook> int findOrAddVertexPU(int vi, const float3& p, const float2& u, const Hook& hook);
    template<class Hook> int findOrAddVertexP(int vi, const float3& p, const Hook& hook);
};







inline uint32_t MeshWelder::hash(const abcV3& value)
{
    auto* h = (const uint32_t*)(&value);
    uint32_t f = (h[0] + h[1] * 11 - (h[2] * 17)) & 0x7fffffff;
    return (f >> 22) ^ (f >> 12) ^ (f);
}

template<class Compare, class Welder>
inline int MeshWelder::weld(abcV3 *points, int count, const Compare & compare_op, const Welder & weld_op)
{
    prepare(points, count);

    int new_index = 0;
    int *next = &m_hash_table[m_hash_size];
    for (int vi = 0; vi < m_count; vi++) {
        auto& v = m_points[vi];
        uint32_t hash_value = hash(v) & (m_hash_size - 1);
        int offset = m_hash_table[hash_value];
        while (offset != NIL) {
            if (m_points[offset] == v) {
                if (compare_op(vi, offset))
                    break;
            }
            offset = next[offset];
        }

        if (offset == NIL) {
            m_remap[vi] = new_index;
            m_points[new_index] = v;
            weld_op(vi, new_index);
            next[new_index] = m_hash_table[hash_value];
            m_hash_table[hash_value] = new_index++;
        }
        else {
            m_remap[vi] = offset;
        }
    }
    return new_index;
}


