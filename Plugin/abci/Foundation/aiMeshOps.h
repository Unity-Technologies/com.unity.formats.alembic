#pragma once
#include "RawVector.h"

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


