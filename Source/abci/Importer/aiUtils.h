#pragma once
#include <Foundation/RawVector.h>
template<class T, class U>
inline void Assign(RawVector<T>& dst, const U& src, int point_count)
{
    dst.resize_discard(point_count);
    auto src_data = src->get();
    auto dst_data = dst.data();
    memcpy(dst_data, src_data, point_count * sizeof(T));
}
