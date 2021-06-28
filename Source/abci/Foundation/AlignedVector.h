#pragma once
#include<vector>
#include<memory>


template <class T>
using AlignedVector = std::vector<T, std::allocator<T> >;

template <class T>
void CopyTo(const AlignedVector<T> &src, T* dst)
{
    memcpy(dst, src.data(), sizeof(T) * src.size());
}

template <class T>
void CopyTo(const AlignedVector<T> &src, T* dst, size_t length, size_t offset = 0)
{
    memcpy(dst, src.data() + offset, sizeof(T) * length);
}

template<class T, class U>
void Assign(AlignedVector<T>& dst, const U& src, int point_count)
{
    dst.resize(point_count);
    auto src_data = src->get();
    auto dst_data = dst.data();
    memcpy(dst_data, src_data, point_count * sizeof(T));
}

template <class T>
void ResizeZeroClear(AlignedVector<T> &src, int count)
{
    src.resize(count);
    memset(src.data(), 0, sizeof(T) * src.capacity());
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
inline void Remap(AlignedVector<T>& dst, const AbcArraySample& src, const AlignedVector<int>& indices)
{
    if (indices.empty())
    {
        dst.assign(src.get(), src.get() + src.size());
    }
    else
    {
        dst.resize(indices.size());
        CopyWithIndices(dst.data(), src.get(), indices);
    }
}

template<class T, class U>
inline void Remap(AlignedVector<T>& dst, const U& src, const AlignedVector<std::pair<float, int> >& sort_data)
{
    dst.resize(sort_data.size());
    size_t count = std::min<size_t>(sort_data.size(), src->size());
    auto src_data = src->get();
    for (size_t i = 0; i < count; ++i)
    {
        dst[i] = (T) src_data[sort_data[i].second];
    }
}

template<class T>
inline void Lerp(AlignedVector<T>& dst, const AlignedVector<T>& src1, const AlignedVector<T>& src2, float w)
{
    if (src1.size() != src2.size())
    {
        return;
    }
    dst.resize(src1.size());
    Lerp(dst.data(), src1.data(), src2.data(), (int)src1.size(), w);
}

