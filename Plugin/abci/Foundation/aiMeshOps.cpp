#include "pch.h"
#include "aiMeshOps.h"


namespace impl
{
    template<class Indices, class Counts>
    inline void BuildConnection(
        ConnectionData& connection, const Indices& indices, const Counts& counts, const IArray<float3>& vertices)
    {
        size_t num_points = vertices.size();
        size_t num_faces = counts.size();
        size_t num_indices = indices.size();

        connection.v2f_offsets.resize_discard(num_points);
        connection.v2f_faces.resize_discard(num_indices);
        connection.v2f_indices.resize_discard(num_indices);

        connection.v2f_counts.resize_zeroclear(num_points);
        {
            int ii = 0;
            for (size_t fi = 0; fi < num_faces; ++fi) {
                int c = counts[fi];
                for (int ci = 0; ci < c; ++ci) {
                    connection.v2f_counts[indices[ii + ci]]++;
                }
                ii += c;
            }

            int offset = 0;
            for (size_t i = 0; i < num_points; ++i) {
                connection.v2f_offsets[i] = offset;
                offset += connection.v2f_counts[i];
            }
        }

        connection.v2f_counts.zeroclear();
        {
            int i = 0;
            for (int fi = 0; fi < (int)num_faces; ++fi) {
                int c = counts[fi];
                for (int ci = 0; ci < c; ++ci) {
                    int vi = indices[i + ci];
                    int ti = connection.v2f_offsets[vi] + connection.v2f_counts[vi]++;
                    connection.v2f_faces[ti] = fi;
                    connection.v2f_indices[ti] = i + ci;
                }
                i += c;
            }
        }
    }
}

void ConnectionData::clear()
{
    v2f_counts.clear();
    v2f_offsets.clear();
    v2f_faces.clear();
    v2f_indices.clear();

    weld_map.clear();
    weld_counts.clear();
    weld_offsets.clear();
    weld_indices.clear();
}

void ConnectionData::buildConnection(
    const IArray<int>& indices, const IArray<int>& counts, const IArray<float3>& vertices)
{
    size_t num_points = vertices.size();
    size_t num_faces = counts.size();
    size_t num_indices = indices.size();

    v2f_offsets.resize_discard(num_points);
    v2f_faces.resize_discard(num_indices);
    v2f_indices.resize_discard(num_indices);

    v2f_counts.resize_zeroclear(num_points);
    {
        int ii = 0;
        for (size_t fi = 0; fi < num_faces; ++fi) {
            int c = counts[fi];
            for (int ci = 0; ci < c; ++ci) {
                v2f_counts[indices[ii + ci]]++;
            }
            ii += c;
        }

        int offset = 0;
        for (size_t i = 0; i < num_points; ++i) {
            v2f_offsets[i] = offset;
            offset += v2f_counts[i];
        }
    }

    v2f_counts.zeroclear();
    {
        int i = 0;
        for (int fi = 0; fi < (int)num_faces; ++fi) {
            int c = counts[fi];
            for (int ci = 0; ci < c; ++ci) {
                int vi = indices[i + ci];
                int ti = v2f_offsets[vi] + v2f_counts[vi]++;
                v2f_faces[ti] = fi;
                v2f_indices[ti] = i + ci;
            }
            i += c;
        }
    }
}


static inline int next_power_of_two(uint32_t v)
{
    v--;
    v |= v >> 1;
    v |= v >> 2;
    v |= v >> 4;
    v |= v >> 8;
    v |= v >> 16;
    v++;
    return v + (v == 0);
}

void MeshWelder::prepare(abcV3 * points, int count)
{
    m_points = points;
    m_count = count;

    m_hash_size = next_power_of_two(m_count);
    m_hash_table.resize_discard(m_hash_size + m_count);
    memset(m_hash_table.data(), NIL, m_hash_size * sizeof(int));

    m_remap.resize_discard(m_count);
}

const RawVector<int>& MeshWelder::getRemapTable() const
{
    return m_remap;
}



void MeshRefiner::triangulate(bool swap_faces)
{
    auto& dst = new_indices_triangulated;
    dst.resize(num_indices_triangulated);

    const int i1 = swap_faces ? 2 : 1;
    const int i2 = swap_faces ? 1 : 2;
    size_t num_faces = counts.size();

    int n = 0;
    int i = 0;
    for (size_t fi = 0; fi < num_faces; ++fi) {
        int count = counts[fi];
        for (int ni = 0; ni < count - 2; ++ni) {
            dst[i + 0] = indices[n + 0];
            dst[i + 1] = indices[n + ni + i1];
            dst[i + 2] = indices[n + ni + i2];
            i += 3;
        }
        n += count;
    }
}

void MeshRefiner::genSubmesh(IArray<int> materialIDs)
{
    submeshes.clear();

    new_indices_submeshes.resize(new_indices_triangulated.size());
    const int *faces_to_read = new_indices_triangulated.data();
    int *faces_to_write = new_indices_submeshes.data();

    int num_splits = (int)splits.size();
    int offset_faces = 0;
    RawVector<Submesh> submeshes;

    for (int si = 0; si < num_splits; ++si) {
        auto& split = splits[si];

        // count triangle indices
        for (int fi = 0; fi < split.num_faces; ++fi) {
            int mid = materialIDs[offset_faces + fi] + 1; // -1 == no material. adjust to it
            while (mid >= (int)submeshes.size()) {
                int id = (int)submeshes.size();
                submeshes.push_back({});
                submeshes.back().materialID = id - 1;
            }
            submeshes[mid].num_indices += (counts[fi] - 2) * 3;
        }

        for (int mi = 0; mi < (int)submeshes.size(); ++mi) {
            auto& sm = submeshes[mi];
            sm.faces_to_write = faces_to_write;
            sm.offset_indices = (int)std::distance(new_indices_submeshes.data(), faces_to_write);
            faces_to_write += sm.num_indices;
        }

        // copy triangles
        for (int fi = 0; fi < split.num_faces; ++fi) {
            int mid = materialIDs[offset_faces + fi] + 1;
            int count = counts[offset_faces + fi];
            int nidx = (count - 2) * 3;
            for (int i = 0; i < nidx; ++i) {
                *(submeshes[mid].faces_to_write++) = *(faces_to_read++);
            }
        }

        for (int mi = 0; mi < (int)submeshes.size(); ++mi) {
            if (submeshes[mi].num_indices > 0) {
                ++split.num_submeshes;
                submeshes.push_back(submeshes[mi]);
            }
        }

        offset_faces += split.num_faces;
        submeshes.clear();
    }
}

template<class Body>
void MeshRefiner::doRefine(const Body& body)
{
    buildConnection();

    int num_indices = (int)indices.size();
    new_points.reserve(num_indices);
    if (!normals.empty()) { new_normals.reserve(num_indices); }
    if (!uv.empty()) { new_uv.reserve(num_indices); }
    new_indices.reserve(num_indices);

    old2new_indices.resize(num_indices, -1);

    int num_faces_total = (int)counts.size();
    int offset_faces = 0;
    int offset_indices = 0;
    int offset_vertices = 0;
    int num_faces = 0;
    int num_indices_triangulated = 0;

    auto add_new_split = [&]() {
        auto split = Split{};
        split.offset_faces = offset_faces;
        split.offset_indices = offset_indices;
        split.offset_vertices = offset_vertices;
        split.num_faces = num_faces;
        split.num_indices_triangulated = num_indices_triangulated;
        split.num_vertices = (int)new_points.size() - offset_vertices;
        split.num_indices = (int)new_indices.size() - offset_indices;
        splits.push_back(split);

        offset_faces += split.num_faces;
        offset_indices += split.num_indices;
        offset_vertices += split.num_vertices;
        num_faces = 0;
        num_indices_triangulated = 0;
    };

    for (int fi = 0; fi < num_faces_total; ++fi) {
        int offset = offsets[fi];
        int count = counts[fi];

        if (split_unit > 0 && (int)new_points.size() - offset_vertices + count > split_unit) {
            add_new_split();

            // clear vertex cache
            memset(old2new_indices.data(), -1, old2new_indices.size() * sizeof(int));
        }

        for (int ci = 0; ci < count; ++ci) {
            int i = offset + ci;
            int vi = indices[i];
            int ni = body(vi, i);
            new_indices.push_back(ni - offset_vertices);
        }
        ++num_faces;
        num_indices_triangulated += (count - 2) * 3;
    }
    add_new_split();

    this->num_indices_triangulated = num_indices_triangulated;
}

void MeshRefiner::refine()
{
    // clear previous data
    {
        old2new_indices.clear();
        new2old_points.clear();
        new2old_normals.clear();
        new2old_uvs.clear();
        offsets.clear();
        new_indices.clear();
        new_indices_triangulated.clear();
        new_indices_submeshes.clear();
        new_points.clear();
        new_normals.clear();
        new_uv.clear();
        splits.clear();
        submeshes.clear();
        num_indices_triangulated = 0;
    }

    int num_points = (int)points.size();
    int num_indices = (int)indices.size();
    int num_normals = (int)normals.size();
    int num_uv = (int)uv.size();

    if (!uv.empty()) {
        if (!normals.empty()) {
            if (num_normals == num_indices && num_uv == num_indices) {
                doRefine([this](int vi, int i) {
                    return findOrAddVertexPNU(vi, points[vi], normals[i], uv[i], [&]() {
                        new2old_points.push_back(vi);
                        new2old_normals.push_back(i);
                        new2old_uvs.push_back(i);
                    });
                });
            }
            else if (num_normals == num_indices && num_uv == num_points) {
                doRefine([this](int vi, int i) {
                    return findOrAddVertexPNU(vi, points[vi], normals[i], uv[vi], [&]() {
                        new2old_points.push_back(vi);
                        new2old_normals.push_back(i);
                        new2old_uvs.push_back(vi);
                    });
                });
            }
            else if (num_normals == num_points && num_uv == num_indices) {
                doRefine([this](int vi, int i) {
                    return findOrAddVertexPNU(vi, points[vi], normals[vi], uv[i], [&]() {
                        new2old_points.push_back(vi);
                        new2old_normals.push_back(vi);
                        new2old_uvs.push_back(i);
                    });
                });
            }
            else if (num_normals == num_points && num_uv == num_points) {
                doRefine([this](int vi, int) {
                    return findOrAddVertexPNU(vi, points[vi], normals[vi], uv[vi], [&]() {
                        new2old_points.push_back(vi);
                        new2old_normals.push_back(vi);
                        new2old_uvs.push_back(vi);
                    });
                });
            }
        }
        else {
            if (num_uv == num_indices) {
                doRefine([this](int vi, int i) {
                    return findOrAddVertexPU(vi, points[vi], uv[i], [&]() {
                        new2old_points.push_back(vi);
                        new2old_uvs.push_back(i);
                    });
                });
            }
            else if (num_uv == num_points) {
                doRefine([this](int vi, int) {
                    return findOrAddVertexPU(vi, points[vi], uv[vi], [&]() {
                        new2old_points.push_back(vi);
                        new2old_uvs.push_back(vi);
                    });
                });
            }
        }
    }
    else if (!normals.empty()) {
        if (num_normals == num_indices) {
            doRefine([this](int vi, int i) {
                return findOrAddVertexPN(vi, points[vi], normals[i], [&]() {
                    new2old_points.push_back(vi);
                    new2old_normals.push_back(i);
                });
            });
        }
        else if (num_normals == num_points) {
            doRefine([this](int vi, int) {
                return findOrAddVertexPN(vi, points[vi], normals[vi], [&]() {
                    new2old_points.push_back(vi);
                    new2old_normals.push_back(vi);
                });
            });
        }
    }
    else {
        doRefine([this](int vi, int) {
            return findOrAddVertexP(vi, points[vi], [&]() {
                new2old_points.push_back(vi);
            });
        });
    }
}

void MeshRefiner::buildConnection()
{
    // skip if already built
    if (connection.v2f_counts.size() == points.size()) { return; }

    connection.buildConnection(indices, counts, points);
}

template<class Hook>
int MeshRefiner::findOrAddVertexPNU(int vi, const float3& p, const float3& n, const float2& u, const Hook& hook)
{
    int offset = connection.v2f_offsets[vi];
    int count = connection.v2f_counts[vi];
    for (int ci = 0; ci < count; ++ci) {
        int& ni = old2new_indices[connection.v2f_indices[offset + ci]];
        if (ni != -1 && new_points[ni]==p && new_normals[ni]==n && new_uv[ni]==u) {
            return ni;
        }
        else if (ni == -1) {
            ni = (int)new_points.size();
            new_points.push_back(p);
            new_normals.push_back(n);
            new_uv.push_back(u);
            hook();
            return ni;
        }
    }
    return 0;
}

template<class Hook>
int MeshRefiner::findOrAddVertexPN(int vi, const float3& p, const float3& n, const Hook& hook)
{
    int offset = connection.v2f_offsets[vi];
    int count = connection.v2f_counts[vi];
    for (int ci = 0; ci < count; ++ci) {
        int& ni = old2new_indices[connection.v2f_indices[offset + ci]];
        if (ni != -1 && new_points[ni]==p && new_normals[ni]==n) {
            return ni;
        }
        else if (ni == -1) {
            ni = (int)new_points.size();
            new_points.push_back(p);
            new_normals.push_back(n);
            return ni;
        }
    }
    return 0;
}

template<class Hook>
int MeshRefiner::findOrAddVertexPU(int vi, const float3& p, const float2& u, const Hook& hook)
{
    int offset = connection.v2f_offsets[vi];
    int count = connection.v2f_counts[vi];
    for (int ci = 0; ci < count; ++ci) {
        int& ni = old2new_indices[connection.v2f_indices[offset + ci]];
        if (ni != -1 && new_points[ni]==p && new_uv[ni]==u) {
            return ni;
        }
        else if (ni == -1) {
            ni = (int)new_points.size();
            new_points.push_back(p);
            new_uv.push_back(u);
            return ni;
        }
    }
    return 0;
}

template<class Hook>
int MeshRefiner::findOrAddVertexP(int vi, const float3 & p, const Hook& hook)
{
    int offset = connection.v2f_offsets[vi];
    int count = connection.v2f_counts[vi];
    for (int ci = 0; ci < count; ++ci) {
        int& ni = old2new_indices[connection.v2f_indices[offset + ci]];
        if (ni != -1 && new_points[ni] == p) {
            return ni;
        }
        else if (ni == -1) {
            ni = (int)new_points.size();
            new_points.push_back(p);
            return ni;
        }
    }
    return 0;
}
