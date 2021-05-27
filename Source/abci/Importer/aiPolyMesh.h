#pragma once
#include "aiMeshSchema.h"

class aiPolyMeshSample : public aiMeshSample<aiPolyMesh>
{
public:
    aiPolyMeshSample(aiPolyMesh* schema, TopologyPtr topo);
    ~aiPolyMeshSample();
};

struct aiPolyMeshTraits
{
    using SampleT = aiPolyMeshSample;
    using AbcSchemaT = AbcGeom::IPolyMeshSchema;
};

class aiPolyMesh : public aiMeshSchema<aiPolyMeshTraits, aiPolyMeshSample>
{
public:
    aiPolyMesh(aiObject *parent, const abcObject &abc);
    ~aiPolyMesh() override;

    aiPolyMeshSample* newSample() override;
};

template<class Container>
static inline int CalculateTriangulatedIndexCount(const Container& counts)
{
    int r = 0;
    size_t n = counts.size();
    for (size_t fi = 0; fi < n; ++fi)
    {
        int ngon = counts[fi];
        r += (ngon - 2) * 3;
    }
    return r;
}

template<class T, class IndexArray>
inline void CopyWithIndices(T *dst, const T *src, const IndexArray& indices)
{
    if (!dst || !src) { return; }
    size_t size = indices.size();
    for (size_t i = 0; i < (int)size; ++i)
    {
        dst[i] = src[indices[i]];
    }
}

template<class T, class AbcArraySample>
inline void Remap(RawVector<T>& dst, const AbcArraySample& src, const RawVector<int>& indices)
{
    if (indices.empty())
    {
        dst.assign(src.get(), src.get() + src.size());
    }
    else
    {
        dst.resize_discard(indices.size());
        CopyWithIndices(dst.data(), src.get(), indices);
    }
}

template<class T>
inline void Lerp(RawVector<T>& dst, const RawVector<T>& src1, const RawVector<T>& src2, float w)
{
    if (src1.size() != src2.size())
    {
        DebugError("something is wrong!!");
        return;
    }
    dst.resize_discard(src1.size());
    Lerp(dst.data(), src1.data(), src2.data(), (int)src1.size(), w);
}

