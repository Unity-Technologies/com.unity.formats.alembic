#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiPolyMesh.h"
#include <unordered_map>
#include "../Foundation/aiMisc.h"


#define MAX_VERTEX_SPLIT_COUNT_16 65000
#define MAX_VERTEX_SPLIT_COUNT_32 2000000000

static inline int CalculateTriangulatedIndexCount(Abc::Int32ArraySample &counts)
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

Topology::Topology()
{
}

void Topology::clear()
{
    DebugLog("Topology::clear()");
    m_face_indices.reset();
    m_counts.reset();

    m_tangent_indices.clear();
    m_tangents_count = 0;

    m_submeshes.clear();
    m_face_split_indices.clear();
    m_splits.clear();
    m_indices_swaped_face_winding.clear();
    m_uv_indices_swaped_face_winding.clear();
}

int Topology::getTriangulatedIndexCount() const
{
    return m_triangulated_index_count;
}

int Topology::getSplitCount() const
{
    return (int) m_splits.size();
}

int Topology::getSplitCount(aiPolyMeshSample * meshSample, bool force_refresh)
{
    if (m_counts && m_face_indices) {
        if (m_face_split_indices.size() != m_counts->size() || force_refresh) {
            updateSplits(meshSample);
        }
    }
    else {
        m_splits.clear();
        m_face_split_indices.clear();
    }

    return (int) m_splits.size();
}

void Topology::updateSplits(aiPolyMeshSample * meshSample)
{
    DebugLog("Topology::updateSplits()");
    
    int split_index = 0;
    int indexOffset = 0;
    int faceCount = (int)m_counts->size();

    m_face_split_indices.resize(faceCount); // number of faces

    m_splits.clear();

    if (m_vertex_sharing_enabled && meshSample != nullptr && !meshSample->m_ownTopology) // only fixed topologies get this execution path
    {
        m_splits.push_back(SplitInfo());

        SplitInfo *curSplit = &(m_splits.back());
        for (size_t i = 0; i<faceCount; ++i)
            m_face_split_indices[i] = 0;

        curSplit->last_face = faceCount-1;
        curSplit->vertex_count = (int)m_fixed_topo_positions_indexes.size();
    }
    else
    {
        const size_t maxVertexSplitCount = m_use_32bit_index_buffer ? MAX_VERTEX_SPLIT_COUNT_32 : MAX_VERTEX_SPLIT_COUNT_16;
        m_splits.reserve(1 + m_face_indices->size() / maxVertexSplitCount);
        m_splits.push_back(SplitInfo());

        SplitInfo *curSplit = &(m_splits.back());
        for (int i = 0; i<faceCount; ++i) {
            int nv = m_counts->get()[i];

            if (curSplit->vertex_count + nv > maxVertexSplitCount) {
                m_splits.push_back(SplitInfo(i, indexOffset));
                ++split_index;

                curSplit = &(m_splits.back());
            }

            m_face_split_indices[i] = split_index; // assign a split ID/index to each face

            curSplit->last_face = i;
            curSplit->vertex_count += nv;

            indexOffset += nv;
        }
    }
}

int Topology::getVertexBufferLength(int split_index) const
{
    if (split_index < 0 || size_t(split_index) >= m_splits.size()) {
        return 0;
    }
    else {
        return (int) m_splits[split_index].vertex_count;
    }
}

int Topology::prepareSubmeshes(abcFaceSetSamples& face_sets, aiPolyMeshSample* sample)
{
    DebugLog("Topology::prepareSubmeshes()");
    
    const auto *counts = m_counts->get();
    const auto *indices = m_face_indexing_reindexed.data();
    int num_counts = (int)m_counts->size();
    int num_indices = (int)m_face_indexing_reindexed.size();
    int num_submeshes = (int)face_sets.size();

    m_submeshes.clear();
    int num_splits = getSplitCount(sample, false);
    if (face_sets.empty()) {
        for (int spi = 0; spi < num_splits; ++spi) {
            auto& split = m_splits[spi];
            split.submesh_count = 1;

            auto submesh = new Submesh();
            m_submeshes.emplace_back(submesh);
            submesh->split_index = spi;
            submesh->submesh_index = 0;
            for (int fi = split.first_face; fi <= split.last_face; ++fi) {
                int count = counts[fi];
                if (count < 3)
                    continue;

                submesh->triangle_count += (count - 2);
            }
        }
    }
    else {
        RawVector<int> face_to_submesh;
        RawVector<int> face_to_offset;
        {
            // setup table
            face_to_offset.resize_discard(num_counts);
            int total = 0;
            for (int fi = 0; fi < num_counts; ++fi) {
                face_to_offset[fi] = total;
                total += counts[fi];
            }

            face_to_submesh.resize_discard(num_counts);
            for (int smi = 0; smi < num_submeshes; ++smi) {
                auto& smfaces = *face_sets[smi].getFaces();
                int num_counts = (int)smfaces.size();
                for (int ci = 0; ci < num_counts; ++ci) {
                    face_to_submesh[smfaces[ci]] = smi;
                }
            }
        }

        // create submeshes
        for (int spi = 0; spi < num_splits; ++spi) {
            m_splits[spi].submesh_count = num_submeshes;
            for (int smi = 0; smi < num_submeshes; ++smi) {
                auto submesh = new Submesh();
                m_submeshes.emplace_back(submesh);
                submesh->split_index = spi;
                submesh->submesh_index = smi;
            }
        }

        // count how many triangles & indices each submesh has
        for (int spi = 0; spi < num_splits; ++spi) {
            int submesh_offset = num_submeshes * spi;
            auto& split = m_splits[spi];
            for (int fi = split.first_face; fi <= split.last_face; ++fi) {
                int count = counts[fi];
                if (count < 3)
                    continue;

                auto& submesh = *m_submeshes[face_to_submesh[fi] + submesh_offset];
                submesh.triangle_count += (count - 2);
                submesh.index_count += count;
                submesh.face_count++;
            }
        }

        // allocate indices
        for (auto& submesh : m_submeshes) {
            submesh->indices.resize_discard(submesh->index_count);
            submesh->faces.resize_discard(submesh->face_count);
            submesh->index_count = 0;
            submesh->face_count = 0;
        }

        // copy indices
        for (int spi = 0; spi < num_splits; ++spi) {
            int submesh_offset = num_submeshes * spi;
            auto& split = m_splits[spi];
            for (int fi = split.first_face; fi <= split.last_face; ++fi) {
                int count = counts[fi];
                if (count < 3)
                    continue;

                auto& submesh = *m_submeshes[face_to_submesh[fi] + submesh_offset];
                int offset = face_to_offset[fi];
                std::copy(indices + offset, indices + (offset + count), &submesh.indices[submesh.index_count]);
                submesh.index_count += count;
                submesh.faces[submesh.face_count++] = count;
            }
        }
    }
    return (int) m_submeshes.size();
}

int Topology::getSplitSubmeshCount(int split_index) const
{
    if (split_index < 0 || size_t(split_index) >= m_splits.size()) {
        return 0;
    }
    else {
        return (int) m_splits[split_index].submesh_count;
    }
}

aiPolyMeshSample::aiPolyMeshSample(aiPolyMesh *schema, TopologyPtr topo, bool ownTopo)
    : super(schema)
    , m_topology(topo)
    , m_ownTopology(ownTopo)
{
}

aiPolyMeshSample::~aiPolyMeshSample()
{
}

bool aiPolyMeshSample::hasNormals() const
{
    switch (m_config.normals_mode)
    {
    case aiNormalsMode::ReadFromFile:
        return m_normals.valid();
    case aiNormalsMode::Ignore:
        return false;
    default:
        return (m_normals.valid() || !m_smooth_normals.empty());
    }
}

bool aiPolyMeshSample::hasUVs() const
{
    return m_uvs.valid();
}

bool aiPolyMeshSample::hasVelocities() const
{
    return !m_schema->hasVaryingTopology() && m_config.interpolate_samples;
}

bool aiPolyMeshSample::hasTangents() const
{
    return (m_config.tangents_mode != aiTangentsMode::None && hasUVs() && !m_tangents.empty() && !m_topology->m_tangent_indices.empty());
}

bool aiPolyMeshSample::smoothNormalsRequired() const
{
    return (m_config.normals_mode == aiNormalsMode::AlwaysCompute ||
            m_config.tangents_mode == aiTangentsMode::Smooth ||
            (!m_normals.valid() && m_config.normals_mode == aiNormalsMode::ComputeIfMissing));
}

bool aiPolyMeshSample::tangentsRequired() const
{
    return (m_config.tangents_mode != aiTangentsMode::None);
}

void aiPolyMeshSample::computeSmoothNormals(const aiConfig &config)
{
    DebugLog("%s: Compute smooth normals", getSchema()->getObject()->getFullName());

    size_t smoothNormalsCount = m_points->size();
    m_smooth_normals.resize_zeroclear(smoothNormalsCount);

    const auto &counts = *(m_topology->m_counts);
    const auto &indices = *(m_topology->m_face_indices);
    const auto &positions = *m_points;

    size_t nf = counts.size();
    size_t off = 0;
    bool ccw = config.swap_face_winding;
    int ti1 = ccw ? 2 : 1;
    int ti2 = ccw ? 1 : 2;
    abcV3 N, dP1, dP2;

    for (size_t f=0; f<nf; ++f) {
        int nfv = counts[f];
        if (nfv >= 3) {
            // Compute average normal for current face
            N.setValue(0.0f, 0.0f, 0.0f);
            const abcV3 &P0 = positions[indices[off]];
            for (int fv=0; fv<nfv-2; ++fv) {
                auto &P1 = positions[indices[off + fv + ti1]];
                auto &P2 = positions[indices[off + fv + ti2]];

                dP1 = P1 - P0;
                dP2 = P2 - P0;
                
                N += dP2.cross(dP1).normalize();
            }

            if (nfv > 3) {
                N.normalize();
            }

            // Accumulate for all vertices participating to this face
            for (int fv=0; fv<nfv; ++fv) {
                m_smooth_normals[indices[off + fv]] += N;
            }
        }

        off += nfv;
    }

    // Normalize normal vectors
    for (abcV3& v : m_smooth_normals) { v.normalize(); }
}

void aiPolyMeshSample::computeTangentIndices(const aiConfig &config, const abcV3 *inN, bool indexedNormals) const
{
    const auto &counts = *(m_topology->m_counts);
    const auto &indices = *(m_topology->m_face_indices);
    const auto &uvVals = *(m_uvs.getVals());
    const auto &uvIdxs = *(m_uvs.getIndices());
    const auto *Nidxs = (indexedNormals ? m_normals.getIndices()->get() : 0);

    size_t tangentIndicesCount = indices.size();
    m_topology->m_tangent_indices.resize(tangentIndicesCount);
    
    if (config.tangents_mode == aiTangentsMode::Smooth) {
        for (size_t i=0; i<tangentIndicesCount; ++i) {
            m_topology->m_tangent_indices[i] = indices[i];
        }

        m_topology->m_tangents_count = (int)m_points->size();
    }
    DebugLog("%lu unique tangent(s)", m_topology->m_tangents_count);
}

void aiPolyMeshSample::computeTangents(const aiConfig &config, const abcV3 *inN, bool indexedNormals)
{
    DebugLog("%s: Compute %stangents", getSchema()->getObject()->getFullName(), (config.tangents_mode == aiTangentsMode::Smooth ? "smooth " : ""));

    const auto &counts = *(m_topology->m_counts);
    const auto &indices = *(m_topology->m_face_indices);
    const auto &positions = *m_points;
    const auto &uvVals = *(m_uvs.getVals());
    const auto &uvIdxs = *(m_uvs.getIndices());
    const Util::uint32_t *Nidxs = (indexedNormals ? m_normals.getIndices()->get() : 0);

    size_t nf = counts.size();
    size_t off = 0;
    bool ccw = config.swap_face_winding;
    int ti1 = (ccw ? 2 : 1);
    int ti2 = (ccw ? 1 : 2);

    size_t tangentsCount = m_topology->m_tangents_count;
    m_tangents.resize_zeroclear(tangentsCount);

    RawVector<int> tanNidxs;
    RawVector<abcV3> tan1;
    RawVector<abcV3> tan2;
    tanNidxs.resize_zeroclear(tangentsCount);
    tan1.resize_zeroclear(tangentsCount);
    tan2.resize_zeroclear(tangentsCount);

    abcV3 T, B, dP1, dP2, tmp;
    abcV2 dUV1, dUV2;

    for (size_t f=0; f<nf; ++f)
    {
        int nfv = counts[f];

        if (nfv >= 3)
        {
            // reset face tangent and bitangent
            T.setValue(0.0f, 0.0f, 0.0f);
            B.setValue(0.0f, 0.0f, 0.0f);

            const abcV3 &P0 = positions[indices[off]];
            const abcV2 &UV0 = uvVals[uvIdxs[off]];

            // for each triangle making up current polygon
            for (int fv=0; fv<nfv-2; ++fv)
            {
                const abcV3 &P1 = positions[indices[off + fv + ti1]];
                const abcV3 &P2 = positions[indices[off + fv + ti2]];

                const abcV2 &UV1 = uvVals[uvIdxs[off + fv + ti1]];
                const abcV2 &UV2 = uvVals[uvIdxs[off + fv + ti2]];

                dP1 = P1 - P0;
                dP2 = P2 - P0;
                
                dUV1 = UV1 - UV0;
                dUV2 = UV2 - UV0;

                float r = dUV1.x * dUV2.y - dUV1.y * dUV2.x;

                if (r != 0.0f)
                {
                    r = 1.0f / r;
                    
                    tmp.setValue(r * (dUV2.y * dP1.x - dUV1.y * dP2.x),
                                 r * (dUV2.y * dP1.y - dUV1.y * dP2.y),
                                 r * (dUV2.y * dP1.z - dUV1.y * dP2.z));
                    tmp.normalize();
                    // accumulate face tangent
                    T += tmp;

                    tmp.setValue(r * (dUV1.x * dP2.x - dUV2.x * dP1.x),
                                 r * (dUV1.x * dP2.y - dUV2.x * dP1.y),
                                 r * (dUV1.x * dP2.z - dUV2.x * dP1.z));
                    tmp.normalize();
                    // accumulte face bitangent
                    B += tmp;
                }
            }

            // normalize face tangent and bitangent if current polygon had to be splitted
            //   into several triangles
            if (nfv > 3)
            {
                T.normalize();
                B.normalize();
            }

            // accumulate normals, tangent and bitangent for each vertex
            for (int fv=0; fv<nfv; ++fv)
            {
                int v = m_topology->m_tangent_indices[off + fv];
                tan1[v] += T;
                tan2[v] += B;
                tanNidxs[v] = (Nidxs ? Nidxs[off + fv] : indices[off + fv]);
            }
        }

        off += nfv;
    }

    // compute final tangent space for each point
    for (size_t i=0; i<tangentsCount; ++i)
    {
        const abcV3 &Nv = inN[tanNidxs[i]];
        abcV3 &Tv = tan1[i];
        abcV3 &Bv = tan2[i];

        // Normalize Tv and Bv?
        
        T = Tv - Nv * Tv.dot(Nv);
        T.normalize();

        m_tangents[i].x = T.x;
        m_tangents[i].y = T.y;
        m_tangents[i].z = T.z;
        m_tangents[i].w = (Nv.cross(Tv).dot(Bv) < 0.0f
                            ? (m_config.swap_handedness ?  1.0f : -1.0f)
                            : (m_config.swap_handedness ? -1.0f :  1.0f));
    }
}

void aiPolyMeshSample::updateConfig(const aiConfig &config, bool &topology_changed, bool &data_changed)
{
    DebugLog("aiPolyMeshSample::updateConfig()");
    
    topology_changed = (config.swap_face_winding != m_config.swap_face_winding);
    data_changed = (config.swap_handedness != m_config.swap_handedness);

    bool smoothNormalsRequired = (config.normals_mode == aiNormalsMode::AlwaysCompute ||
                                  config.tangents_mode == aiTangentsMode::Smooth ||
                                  (!m_normals.valid() && config.normals_mode == aiNormalsMode::ComputeIfMissing));
    
    if (smoothNormalsRequired) {
        if (m_smooth_normals.empty() || topology_changed) {
            computeSmoothNormals(config);
            data_changed = true;
        }
    }
    else {
        if (!m_smooth_normals.empty()) {
            DebugLog("%s: Clear smooth normals", getSchema()->getObject()->getFullName());
            m_smooth_normals.clear();
            data_changed = true;
        }
    }

    bool tangentsRequired = (m_uvs.valid() && config.tangents_mode != aiTangentsMode::None);

    if (tangentsRequired) {
        bool tangentsModeChanged = (config.tangents_mode != m_config.tangents_mode);

        const abcV3 *N = nullptr;
        bool Nindexed = false;

        if (smoothNormalsRequired) {
            N = m_smooth_normals.data();
        }
        else if (m_normals.valid()) {
            N = m_normals.getVals()->get();
            Nindexed = (m_normals.getScope() == AbcGeom::kFacevaryingScope);
        }

        if (N) {
            // do not compute indices if they are cached, constant topology and valid
            if (m_topology->m_tangent_indices.empty() ||
                !config.cache_tangents_splits ||
                tangentsModeChanged)
            {
                computeTangentIndices(config, N, Nindexed);
            }
            if (m_tangents.empty() || 
                tangentsModeChanged ||
                topology_changed)
            {
                computeTangents(config, N, Nindexed);
                data_changed = true;
            }
        }
        else {
            tangentsRequired = false;
        }
    }
    
    if (!tangentsRequired) {
        if (!m_tangents.empty()) {
            DebugLog("%s: Clear tangents", getSchema()->getObject()->getFullName());

            m_tangents.clear();
            data_changed = true;
        }

        if (!m_topology->m_tangent_indices.empty() && (m_ownTopology || !config.cache_tangents_splits))
        {
            DebugLog("%s: Clear tangent indices", getSchema()->getObject()->getFullName());

            m_topology->m_tangent_indices.clear();
            m_topology->m_tangents_count = 0;
        }
    }

    if (topology_changed)
    {
        data_changed = true;
    }

    m_config = config;   
}

void aiPolyMeshSample::getSummary(bool force_refresh, aiMeshSampleSummary &summary, aiPolyMeshSample* sample) const
{
    DebugLog("aiPolyMeshSample::getSummary(force_refresh=%s)", force_refresh ? "true" : "false");
    
    summary.splitCount = m_topology->getSplitCount(sample, force_refresh);
    summary.hasNormals = hasNormals();
    summary.hasUVs = hasUVs();
    summary.hasTangents = hasTangents();
    summary.hasVelocities = hasVelocities();
}


void aiPolyMeshSample::getDataPointer(aiPolyMeshData &dst) const
{
    if (m_points) {
        dst.position_count = m_points->valid() ? (int)m_points->size() : 0;
        dst.points = (abcV3*)(m_points->get());
    }

    if (m_velocities) {
        dst.velocities = m_velocities->valid() ? (abcV3*)m_velocities->get() : nullptr;
    }

    if (m_normals) {
        dst.normal_count = (int)m_normals.getVals()->size();
        dst.normals = (abcV3*)m_normals.getVals()->get();
        dst.normal_index_count = m_normals.isIndexed() ? (int)m_normals.getIndices()->size() : 0;
        if (dst.normal_index_count) {
            dst.normal_indices = (int*)m_normals.getIndices()->get();
        }
    }

    if (m_uvs) {
        dst.uv_count = (int)m_uvs.getVals()->size();
        dst.uvs = (abcV2*)m_uvs.getVals()->get();
        dst.uv_index_count = m_uvs.isIndexed() ? (int)m_uvs.getIndices()->size() : 0;
        if (dst.uv_index_count) {
            dst.uv_indices = (int*)m_uvs.getIndices()->get();
        }
    }

    if (m_topology) {
        if (m_topology->m_face_indices) {
            dst.index_count = (int)m_topology->m_face_indices->size();
            dst.indices = (int*)m_topology->m_face_indices->get();
        }
        if (m_topology->m_counts) {
            dst.face_count = (int)m_topology->m_counts->size();
            dst.faces = (int*)m_topology->m_counts->get();
            dst.triangulated_index_count = m_topology->m_triangulated_index_count;
        }
    }

    dst.center = m_bounds.center();
    dst.size = m_bounds.size();
}

void aiPolyMeshSample::copyData(aiPolyMeshData &dst)
{
    aiPolyMeshData src;
    getDataPointer(src);

    // sadly, memcpy() is way faster than std::copy() on VC

    if (src.faces && dst.faces && dst.face_count >= src.face_count) {
        memcpy(dst.faces, src.faces, src.face_count * sizeof(*dst.faces));
        dst.face_count = src.face_count;
    }
    else {
        dst.face_count = 0;
    }

    if (src.points && dst.points && dst.position_count >= src.position_count) {
        memcpy(dst.points, src.points, src.position_count * sizeof(*dst.points));
        dst.position_count = src.position_count;
    }
    else {
        dst.position_count = 0;
    }

    if (src.velocities && dst.velocities && dst.position_count >= src.position_count) {
        memcpy(dst.velocities, src.velocities, src.position_count * sizeof(*dst.velocities));
    }

    if (src.interpolated_velocities_xy && dst.interpolated_velocities_xy && dst.position_count >= src.position_count)
    {
        memcpy(dst.interpolated_velocities_xy, src.interpolated_velocities_xy, src.position_count * sizeof(*dst.interpolated_velocities_xy));
    }

    if (src.interpolated_velocities_z && dst.interpolated_velocities_z && dst.position_count >= src.position_count)
    {
        memcpy(dst.interpolated_velocities_z, src.interpolated_velocities_z, src.position_count * sizeof(*dst.interpolated_velocities_z));
    }
    

    if (src.normals && dst.normals && dst.normal_count >= src.normal_count) {
        memcpy(dst.normals, src.normals, src.normal_count * sizeof(*dst.normals));
        dst.normal_count = src.normal_count;
    }
    else {
        dst.normal_count = 0;
    }

    if (src.uvs && dst.uvs && dst.uv_count >= src.uv_count) {
        memcpy(dst.uvs, src.uvs, src.uv_count * sizeof(*dst.uvs));
        dst.uv_count = src.uv_count;
    }
    else {
        dst.uv_count = 0;
    }

    auto copy_indices = [&](int *d, const int *s, int n) {
        memcpy(d, s, n * sizeof(int));
    };

    if (src.indices && dst.indices && dst.index_count >= src.index_count) {
        copy_indices(dst.indices, src.indices, src.index_count);
        dst.index_count = src.index_count;
    }
    if (src.normal_indices && dst.normal_indices && dst.normal_index_count >= src.normal_index_count) {
        copy_indices(dst.normal_indices, src.normal_indices, src.normal_index_count);
        dst.normal_index_count = src.normal_index_count;
    }
    if (src.uv_indices && dst.uv_indices && dst.uv_index_count >= src.uv_index_count) {
        copy_indices(dst.uv_indices, src.uv_indices, src.uv_index_count);
        dst.uv_index_count = src.uv_index_count;
    }

    dst.center = dst.center;
    dst.size = dst.size;
}

int aiPolyMeshSample::getVertexBufferLength(int split_index) const
{
    DebugLog("aiPolyMeshSample::getVertexBufferLength(split_index=%d)", split_index);
    
    return m_topology->getVertexBufferLength(split_index);
}

void aiPolyMeshSample::fillVertexBuffer(int split_index, aiPolyMeshData &data)
{
    DebugLog("aiPolyMeshSample::fillVertexBuffer(split_index=%d)", split_index);
    
    if (split_index < 0 || size_t(split_index) >= m_topology->m_splits.size() || m_topology->m_splits[split_index].vertex_count == 0)
    {
        return;
    }

    bool copy_normals = (hasNormals() && data.normals);
    bool copy_uvs = (hasUVs() && data.uvs);
    bool copy_tangents = (hasTangents() && data.tangents);
    
    bool use_abc_normals = (m_normals.valid() && (m_config.normals_mode == aiNormalsMode::ReadFromFile || m_config.normals_mode == aiNormalsMode::ComputeIfMissing));
    float xScale = (m_config.swap_handedness ? -1.0f : 1.0f);
    bool interpolate_points = hasVelocities() && m_next_points != nullptr;
    float time_offset = static_cast<float>(m_current_time_offset);
    float time_interval = static_cast<float>(m_current_time_interval);
    float vertex_motion_scale = static_cast<float>(m_config.vertex_motion_scale);
    
    const SplitInfo &split = m_topology->m_splits[split_index];
    const auto *faceCount = m_topology->m_counts->get();
    const auto *indices = m_config.turn_quad_edges ? m_topology->m_indices_swaped_face_winding.data() : m_topology->m_face_indices->get();
    const auto *points = m_points->get();
    const auto *next_points = interpolate_points ? m_next_points->get() : nullptr;

    size_t k = 0;
    size_t o = split.index_offset;
    
    // reset unused data arrays

    if (data.normals && !copy_normals)
    {
        DebugLog("%s: Reset normals", getSchema()->getObject()->getFullName());
        memset(data.normals, 0, split.vertex_count * sizeof(abcV3));
    }
    
    if (data.uvs && !copy_uvs)
    {
        DebugLog("%s: Reset UVs", getSchema()->getObject()->getFullName());
        memset(data.uvs, 0, split.vertex_count * sizeof(abcV2));
    }
    
    if (data.tangents && !copy_tangents)
    {
        DebugLog("%s: Reset tangents", getSchema()->getObject()->getFullName());
        memset(data.tangents, 0, split.vertex_count * sizeof(abcV4));
    }

    abcV3 bbmin = points[indices[o]];
    abcV3 bbmax = bbmin;

#define UPDATE_POSITIONS_AND_BOUNDS(srcIdx, dstIdx) \
    abcV3 &cP = data.points[dstIdx]; \
    cP = points[srcIdx]; \
    if (interpolate_points) \
    {\
        abcV3 distance = next_points[srcIdx] - points[srcIdx]; \
        abcV3 velocity = (distance / time_interval) * vertex_motion_scale; \
        cP+= distance * time_offset; \
        data.interpolated_velocities_xy[dstIdx].x = velocity.x*xScale; \
        data.interpolated_velocities_xy[dstIdx].y = velocity.y; \
        data.interpolated_velocities_z[dstIdx].x = velocity.z; \
    }\
    cP.x *= xScale; \
    if (cP.x < bbmin.x) bbmin.x = cP.x; \
    else if (cP.x > bbmax.x) bbmax.x = cP.x; \
    if (cP.y < bbmin.y) bbmin.y = cP.y; \
    else if (cP.y > bbmax.y) bbmax.y = cP.y; \
    if (cP.z < bbmin.z) bbmin.z = cP.z; \
    else if (cP.z > bbmax.z) bbmax.z = cP.z


    // fill data arrays
    if ( m_topology->m_vertex_sharing_enabled && m_topology->m_treat_vertex_extra_data_as_static && !m_topology->m_freshly_read_topology_data && m_topology->m_fixed_topo_positions_indexes.size())
    {
        for (size_t i = 0; i < m_topology->m_fixed_topo_positions_indexes.size(); i++)
        {
            UPDATE_POSITIONS_AND_BOUNDS(m_topology->m_fixed_topo_positions_indexes[i], i);
        }
    }
    else
    {
        m_topology->m_freshly_read_topology_data = false;
        if (copy_normals) {
            if (use_abc_normals) {
                const auto *normals = m_normals.getVals()->get();

                if (m_normals.getScope() == AbcGeom::kFacevaryingScope) {
                    const auto &nIndices = *(m_normals.getIndices());

                    if (copy_uvs) {
                        const auto *uvs = m_uvs.getVals()->get();
                        const auto *uvIndices = m_config.turn_quad_edges ? m_topology->m_uv_indices_swaped_face_winding.data() : m_uvs.getIndices()->get();

                        if (copy_tangents) {
                            for (size_t i = split.first_face; i <= split.last_face; ++i) {
                                int nv = faceCount[i];
                                for (int j = 0; j < nv; ++j, ++o, ++k) {
                                    if ( m_topology->m_fixed_topo_positions_indexes.size()) {
                                        size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                        size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                        UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                        data.normals[dstNdx] = normals[nIndices[o]];
                                        data.normals[dstNdx].x *= xScale;
                                        data.tangents[dstNdx] = m_tangents[m_topology->m_tangent_indices[o]];
                                        data.tangents[dstNdx].x *= xScale;
                                        data.uvs[dstNdx] = uvs[uvIndices[o]];
                                    }
                                    else {
                                        UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                                        data.normals[k] = normals[nIndices[o]];
                                        data.normals[k].x *= xScale;
                                        data.tangents[k] = m_tangents[m_topology->m_tangent_indices[o]];
                                        data.tangents[k].x *= xScale;
                                        data.uvs[k] = uvs[uvIndices[o]];
                                    }
                                }
                            }
                        }
                        else {
                            for (size_t i = split.first_face; i <= split.last_face; ++i) {
                                int nv = faceCount[i];
                                for (int j = 0; j < nv; ++j, ++o, ++k) {
                                    if( m_topology->m_fixed_topo_positions_indexes.size()) {
                                        size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                        size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                        UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                        data.normals[dstNdx] = normals[nIndices[o]];
                                        data.normals[dstNdx].x *= xScale;
                                        data.uvs[dstNdx] = uvs[uvIndices[o]];
                                    }
                                    else {
                                        UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                                        data.normals[k] = normals[nIndices[o]];
                                        data.normals[k].x *= xScale;
                                        data.uvs[k] = uvs[uvIndices[o]];
                                    }
                                }
                            }
                        }
                    }
                    else if (copy_tangents) {
                        for (size_t i=split.first_face; i<=split.last_face; ++i) {
                            int nv = faceCount[i];
                            for (int j = 0; j < nv; ++j, ++o, ++k) {
                                if ( m_topology->m_fixed_topo_positions_indexes.size()) {
                                    size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                    size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                    UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                    data.normals[dstNdx] = normals[nIndices[o]];
                                    data.normals[dstNdx].x *= xScale;
                                    data.tangents[dstNdx] = m_tangents[m_topology->m_tangent_indices[o]];
                                    data.tangents[dstNdx].x *= xScale;
                                }
                                else {
                                    UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                                    data.normals[k] = normals[nIndices[o]];
                                    data.normals[k].x *= xScale;
                                    data.tangents[k] = m_tangents[m_topology->m_tangent_indices[o]];
                                    data.tangents[k].x *= xScale;
                                }
                            }
                        }
                    }
                    else {
                        for (size_t i=split.first_face; i<=split.last_face; ++i) {
                            int nv = faceCount[i];
                            for (int j = 0; j < nv; ++j, ++o, ++k) {
                                if ( m_topology->m_fixed_topo_positions_indexes.size()) {
                                    size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                    size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                    UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                    data.normals[dstNdx] = normals[nIndices[o]];
                                    data.normals[dstNdx].x *= xScale;
                                }
                                else {
                                    UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                                    data.normals[k] = normals[nIndices[o]];
                                    data.normals[k].x *= xScale;
                                }
                            }
                        }
                    }
                }
                else {
                    if (copy_uvs) {
                        const auto *uvs = m_uvs.getVals()->get();
                        const auto *uvIndices = m_config.turn_quad_edges ? m_topology->m_uv_indices_swaped_face_winding.data() : m_uvs.getIndices()->get();
                    
                        if (copy_tangents) {
                            for (size_t i=split.first_face; i<=split.last_face; ++i) {
                                int nv = faceCount[i];
                                for (int j = 0; j < nv; ++j, ++o, ++k) {
                                    if (m_topology->m_fixed_topo_positions_indexes.size()) {
                                        size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                        size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                        UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                        data.normals[dstNdx] = normals[indices[o]];
                                        data.normals[dstNdx].x *= xScale;
                                        data.tangents[dstNdx] = m_tangents[m_topology->m_tangent_indices[o]];
                                        data.tangents[dstNdx].x *= xScale;
                                        data.uvs[dstNdx] = uvs[uvIndices[o]];
                                    }
                                    else {
                                        int v = indices[o];
                                        UPDATE_POSITIONS_AND_BOUNDS(v, k);
                                        data.normals[k] = normals[v];
                                        data.normals[k].x *= xScale;
                                        data.tangents[k] = m_tangents[m_topology->m_tangent_indices[o]];
                                        data.tangents[k].x *= xScale;
                                        data.uvs[k] = uvs[uvIndices[o]];
                                    }
                                }
                            }
                        }
                        else {
                            for (size_t i=split.first_face; i<=split.last_face; ++i) {
                                int nv = faceCount[i];
                                for (int j = 0; j < nv; ++j, ++o, ++k) {
                                    if ( m_topology->m_fixed_topo_positions_indexes.size()) {
                                        size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                        size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                        UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                        data.normals[dstNdx] = normals[indices[o]];
                                        data.normals[dstNdx].x *= xScale;
                                        data.uvs[dstNdx] = uvs[uvIndices[o]];
                                    }
                                    else {
                                        int v = indices[o];
                                        UPDATE_POSITIONS_AND_BOUNDS(v, k);
                                        data.normals[k] = normals[v];
                                        data.normals[k].x *= xScale;
                                        data.uvs[k] = uvs[uvIndices[o]];
                                    }
                                }
                            }
                        }
                    }
                    else if (copy_tangents) {
                        for (size_t i=split.first_face; i<=split.last_face; ++i) {
                            int nv = faceCount[i];
                            for (int j = 0; j < nv; ++j, ++o, ++k) {
                                if ( m_topology->m_fixed_topo_positions_indexes.size()) {
                                    size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                    size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                    UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                    data.normals[dstNdx] = normals[indices[o]];
                                    data.normals[dstNdx].x *= xScale;
                                    data.tangents[dstNdx] = m_tangents[m_topology->m_tangent_indices[o]];
                                    data.tangents[dstNdx].x *= xScale;
                                }
                                else {
                                    int v = indices[o];
                                    UPDATE_POSITIONS_AND_BOUNDS(v, k);
                                    data.normals[k] = normals[v];
                                    data.normals[k].x *= xScale;
                                    data.tangents[k] = m_tangents[m_topology->m_tangent_indices[o]];
                                    data.tangents[k].x *= xScale;
                                }
                            }
                        }
                    }
                    else {
                        for (size_t i=split.first_face; i<=split.last_face; ++i) {
                            int nv = faceCount[i];
                            for (int j = 0; j < nv; ++j, ++o, ++k) {
                                if ( m_topology->m_fixed_topo_positions_indexes.size()) {
                                    size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                    size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                    UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                    data.normals[dstNdx] = normals[indices[o]];
                                    data.normals[dstNdx].x *= xScale;
                                }
                                else {
                                    int v = indices[o];
                                    UPDATE_POSITIONS_AND_BOUNDS(v, k);
                                    data.normals[k] = normals[v];
                                    data.normals[k].x *= xScale;
                                }
                            }
                        }
                    }
                }
            }
            else {
                if (copy_uvs) {
                    const auto *uvs = m_uvs.getVals()->get();
                    const auto *uvIndices = m_config.turn_quad_edges ? m_topology->m_uv_indices_swaped_face_winding.data() : m_uvs.getIndices()->get();
                
                    if (copy_tangents) {
                        for (size_t i=split.first_face; i<=split.last_face; ++i) {
                            int nv = faceCount[i];
                            for (int j = 0; j < nv; ++j, ++o, ++k) {
                                if ( m_topology->m_fixed_topo_positions_indexes.size())
                                {
                                    size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                    size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                    UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                    data.normals[dstNdx] = m_smooth_normals[indices[o]];
                                    data.normals[dstNdx].x *= xScale;
                                    data.tangents[dstNdx] = m_tangents[m_topology->m_tangent_indices[o]];
                                    data.tangents[dstNdx].x *= xScale;
                                    data.uvs[dstNdx] = uvs[uvIndices[o]];
                                }
                                else {
                                    int v = indices[o];

                                    UPDATE_POSITIONS_AND_BOUNDS(v, k);
                                    data.normals[k] = m_smooth_normals[v];
                                    data.normals[k].x *= xScale;
                                    data.tangents[k] = m_tangents[m_topology->m_tangent_indices[o]];
                                    data.tangents[k].x *= xScale;
                                    data.uvs[k] = uvs[uvIndices[o]];
                                }
                            }
                        }
                    }
                    else {
                        for (size_t i=split.first_face; i<=split.last_face; ++i) {
                            int nv = faceCount[i];
                            for (int j = 0; j < nv; ++j, ++o, ++k) {
                                if (m_topology->m_fixed_topo_positions_indexes.size()) {
                                    size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                    size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                    UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                    data.normals[dstNdx] = m_smooth_normals[indices[o]];
                                    data.normals[dstNdx].x *= xScale;
                                    data.uvs[dstNdx] = uvs[uvIndices[o]];
                                }
                                else {
                                    int v = indices[o];
                                    UPDATE_POSITIONS_AND_BOUNDS(v, k);
                                    data.normals[k] = m_smooth_normals[v];
                                    data.normals[k].x *= xScale;
                                    data.uvs[k] = uvs[uvIndices[o]];
                                }
                            }
                        }
                    }
                }
                else if (copy_tangents) {
                    for (size_t i=split.first_face; i<=split.last_face; ++i) {
                        int nv = faceCount[i];
                        for (int j = 0; j < nv; ++j, ++o, ++k) {
                            if ( m_topology->m_fixed_topo_positions_indexes.size()) {
                                size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                data.normals[dstNdx] = m_smooth_normals[indices[o]];
                                data.normals[dstNdx].x *= xScale;
                                data.tangents[dstNdx] = m_tangents[m_topology->m_tangent_indices[o]];
                                data.tangents[dstNdx].x *= xScale;
                            }
                            else {
                                int v = indices[o];
                                UPDATE_POSITIONS_AND_BOUNDS(v, k);
                                data.normals[k] = m_smooth_normals[v];
                                data.normals[k].x *= xScale;
                                data.tangents[k] = m_tangents[m_topology->m_tangent_indices[o]];
                                data.tangents[k].x *= xScale;
                            }
                        }
                    }
                }
                else {
                    for (size_t i=split.first_face; i<=split.last_face; ++i) {
                        int nv = faceCount[i];
                        for (int j = 0; j < nv; ++j, ++o, ++k) {
                            if ( m_topology->m_fixed_topo_positions_indexes.size()) {
                                size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                data.normals[dstNdx] = m_smooth_normals[indices[o]];
                                data.normals[dstNdx].x *= xScale;
                            }
                            else {
                                int v = indices[o];
                                UPDATE_POSITIONS_AND_BOUNDS(v, k);
                                data.normals[k] = m_smooth_normals[v];
                                data.normals[k].x *= xScale;
                            }
                        }
                    }
                }
            }
        }
        else {
            if (copy_uvs) {
                const auto *uvs = m_uvs.getVals()->get();
                const auto *uvIndices = m_config.turn_quad_edges ? m_topology->m_uv_indices_swaped_face_winding.data() : m_uvs.getIndices()->get();
            
                if (copy_tangents) {
                    for (size_t i=split.first_face; i<=split.last_face; ++i) {
                        int nv = faceCount[i];
                        for (int j = 0; j < nv; ++j, ++o, ++k) {
                            if (m_topology->m_fixed_topo_positions_indexes.size()) {
                                size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                data.tangents[dstNdx] = m_tangents[m_topology->m_tangent_indices[o]];
                                data.tangents[dstNdx].x *= xScale;
                                data.uvs[dstNdx] = uvs[uvIndices[o]];
                            }
                            else {
                                UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                                data.tangents[k] = m_tangents[m_topology->m_tangent_indices[o]];
                                data.tangents[k].x *= xScale;
                                data.uvs[k] = uvs[uvIndices[o]];
                            }
                        }
                    }
                }
                else {
                    for (size_t i=split.first_face; i<=split.last_face; ++i) {
                        int nv = faceCount[i];
                        for (int j = 0; j < nv; ++j, ++o, ++k) {
                            if (m_topology->m_fixed_topo_positions_indexes.size()) {
                                size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                                size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                                UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                                data.uvs[dstNdx] = uvs[uvIndices[o]];
                            }
                            else {
                                UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                                data.uvs[k] = uvs[uvIndices[o]];
                            }
                        }
                    }
                }
            }
            else if (copy_tangents) {
                for (size_t i=split.first_face; i<=split.last_face; ++i) {
                    int nv = faceCount[i];
                    for (int j = 0; j < nv; ++j, ++o, ++k) {
                        if (m_topology->m_fixed_topo_positions_indexes.size()) {
                            size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                            size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                            UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                            data.tangents[dstNdx] = m_tangents[m_topology->m_tangent_indices[o]];
                            data.tangents[dstNdx].x *= xScale;
                        }
                        else {
                            UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                            data.tangents[k] = m_tangents[m_topology->m_tangent_indices[o]];
                            data.tangents[k].x *= xScale;
                        }
                    }
                }
            }
            else {
                for (size_t i=split.first_face; i<=split.last_face; ++i) {
                    int nv = faceCount[i];
                    for (int j = 0; j < nv; ++j, ++o, ++k) {
                        if (m_topology->m_fixed_topo_positions_indexes.size()) {
                            size_t dstNdx = m_topology->m_face_indexing_reindexed[o];
                            size_t srcNdx = m_topology->m_fixed_topo_positions_indexes[dstNdx];

                            UPDATE_POSITIONS_AND_BOUNDS(srcNdx, dstNdx);
                        }
                        else
                        {
                            UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                        }
                    }
                }
            }
        }
    }

#undef UPDATE_POSITIONS_AND_BOUNDS

    data.center = 0.5f * (bbmin + bbmax);
    data.size = bbmax - bbmin;
}

int aiPolyMeshSample::prepareSubmeshes(aiPolyMeshSample* sample)
{
    DebugLog("aiPolyMeshSample::prepateSubmeshes()");

    int rv = m_topology->prepareSubmeshes(m_facesets, sample);
    m_cur_submesh = m_topology->submeshBegin();
    return rv;
}

int aiPolyMeshSample::getSplitSubmeshCount(int split_index) const
{
    DebugLog("aiPolyMeshSample::getSplitSubmeshCount()");
    
    return m_topology->getSplitSubmeshCount(split_index);
}

bool aiPolyMeshSample::getNextSubmesh(aiSubmeshSummary &summary)
{
    DebugLog("aiPolyMeshSample::getNextSubmesh()");
    
    if (m_cur_submesh == m_topology->submeshEnd()) {
        return false;
    }
    else {
        Submesh &submesh = **m_cur_submesh;
        summary.index = int(m_cur_submesh - m_topology->submeshBegin());
        summary.split_index = submesh.split_index;
        summary.split_submesh_index = submesh.submesh_index;
        summary.triangle_count = int(submesh.triangle_count);

        ++m_cur_submesh;
        return true;
    }
}

void aiPolyMeshSample::fillSubmeshIndices(const aiSubmeshSummary &summary, aiSubmeshData &data) const
{
    DebugLog("aiPolyMeshSample::fillSubmeshIndices()");
    
    auto it = m_topology->submeshBegin() + summary.index;
    if (it != m_topology->submeshEnd()) {
        bool ccw = m_config.swap_face_winding;
        const auto &counts = *(m_topology->m_counts);
        const auto &submesh = **it;

        int index = 0;
        int i1 = (ccw ? 2 : 1);
        int i2 = (ccw ? 1 : 2);
        int offset = 0;
        
        if (submesh.faces.empty() && submesh.indices.empty()) {
            // single submesh case, faces and vertexIndices not populated
            for (size_t i=0; i<counts.size(); ++i) {
                int nv = counts[i];
                int nt = nv - 2;
                if (m_topology->m_fixed_topo_positions_indexes.size() > 0) {
                    for (int ti = 0; ti<nt; ++ti) {
                        data.indices[offset + 0] = m_topology->m_face_indexing_reindexed[index];
                        data.indices[offset + 1] = m_topology->m_face_indexing_reindexed[index + ti + i1];
                        data.indices[offset + 2] = m_topology->m_face_indexing_reindexed[index + ti + i2];
                        offset += 3;
                    }
                }
                else {
                    for (int ti = 0; ti<nt; ++ti) {
                        data.indices[offset + 0] = index;
                        data.indices[offset + 1] = index + ti + i1;
                        data.indices[offset + 2] = index + ti + i2;
                        offset += 3;
                    }
                }
                index += nv;
            }
        }
        else {
            for (int32_t f : submesh.faces) {
                int nv = counts[f];
                int nt = nv - 2;
                for (int ti = 0; ti < nt; ++ti) {
                    data.indices[offset + 0] = submesh.indices[index];
                    data.indices[offset + 1] = submesh.indices[index + ti + i1];
                    data.indices[offset + 2] = submesh.indices[index + ti + i2];
                    offset += 3;
                }
                index += nv;
            }
        }
    }
}

// ---

aiPolyMesh::aiPolyMesh(aiObject *obj)
    : super(obj)
    , m_peak_index_count(0)
    , m_peak_triangulated_index_count(0)
    , m_peak_vertex_count(0)
    , m_ignore_normals(false)
    , m_ignore_uvs(false)
{
    m_constant = m_schema.isConstant();

    auto normals = m_schema.getNormalsParam();
    if (normals.valid()) {
        auto scope = normals.getScope();
        if (scope != AbcGeom::kUnknownScope) {
            if (!normals.isConstant()) {
                m_constant = false;
            }
        }
        else {
            m_ignore_normals = true;
        }
    }

    auto uvs = m_schema.getUVsParam();
    if (uvs.valid()) {
        auto scope = uvs.getScope();
        if (scope != AbcGeom::kUnknownScope) {
            if (!uvs.isConstant()) {
                m_constant = false;
            }
        }
        else {
            m_ignore_uvs = true;
        }
    }

    m_varying_topology = (m_schema.getTopologyVariance() == AbcGeom::kHeterogeneousTopology);

    // find FaceSetSchema in children
    size_t num_children = m_obj->getAbcObject().getNumChildren();
    for (size_t i = 0; i < num_children; ++i) {
        auto& child = m_obj->getAbcObject().getChild(i);
        if (!child.valid())
            continue;

        if (AbcGeom::IFaceSetSchema::matches(child.getMetaData())) {
            auto so = Abc::ISchemaObject<AbcGeom::IFaceSetSchema>(child, Abc::kWrapExisting);
            auto faceset = so.getSchema();
            // check if time sampling and variance are same
            if (faceset.isConstant() == m_schema.isConstant() &&
                faceset.getTimeSampling() == m_schema.getTimeSampling() &&
                faceset.getNumSamples() == m_schema.getNumSamples())
            {
                m_facesets.push_back(faceset);
            }
        }
    }

    DebugLog("aiPolyMesh::aiPolyMesh(constant=%s, varyingTopology=%s)",
             (m_constant ? "true" : "false"),
             (m_varying_topology ? "true" : "false"));
}

aiPolyMesh::Sample* aiPolyMesh::newSample()
{
    Sample *sample = getSample();    
    if (!sample) {
        if (dontUseCache() || !m_varying_topology) {
            if (!m_shared_topology)
                m_shared_topology.reset(new Topology());
            sample = new Sample(this, m_shared_topology, false);
        }
        else {
            sample = new Sample(this, TopologyPtr(new Topology()), true);
        }
    }
    else {
        if (m_varying_topology) {
            sample->m_topology->clear();
        }
    }
    return sample;
}

aiPolyMesh::Sample* aiPolyMesh::readSample(const uint64_t idx, bool &topology_changed)
{
    auto ss = aiIndexToSampleSelector(idx);
    auto ss2 = aiIndexToSampleSelector(idx + 1);
    DebugLog("aiPolyMesh::readSample(t=%d)", idx);
    
    Sample *sample = newSample();
    auto topology = sample->m_topology;

    topology_changed = m_varying_topology;
    topology->EnableVertexSharing(m_config.share_vertices && !m_varying_topology);
    topology->Enable32BitsIndexbuffers(m_config.use_32bit_index_buffer);
    topology->TreatVertexExtraDataAsStatic(m_config.treat_vertex_extra_data_as_static && !m_varying_topology);

    if (!topology->m_counts || m_varying_topology) {
        m_schema.getFaceCountsProperty().get(topology->m_counts, ss);
        topology->m_triangulated_index_count = CalculateTriangulatedIndexCount(*topology->m_counts);
        topology_changed = true;
    }
    if (!topology->m_face_indices || m_varying_topology) {
        m_schema.getFaceIndicesProperty().get(topology->m_face_indices, ss);
        topology_changed = true;
    }
    if (topology_changed && !m_facesets.empty()) {
        sample->m_facesets.resize(m_facesets.size());
        for (size_t fi = 0; fi < m_facesets.size(); ++fi) {
            m_facesets[fi].get(sample->m_facesets[fi], ss);
        }
    }
    m_schema.getPositionsProperty().get(sample->m_points, ss);

    if (!m_varying_topology && m_config.interpolate_samples) {
        m_schema.getPositionsProperty().get(sample->m_next_points, ss2);
    }

    sample->m_velocities.reset();
    auto velocitiesProp = m_schema.getVelocitiesProperty();
    if (velocitiesProp.valid()) {
        velocitiesProp.get(sample->m_velocities, ss);
    }

    bool smoothNormalsRequired = sample->smoothNormalsRequired();

    sample->m_normals.reset();
    auto normalsParam = m_schema.getNormalsParam();
    if (!m_ignore_normals && normalsParam.valid()) {
        if (normalsParam.isConstant()) {
            if (!m_shared_normals.valid()) {
                DebugLog("  Read normals (constant)");
                normalsParam.getIndexed(m_shared_normals, ss);
            }
            sample->m_normals = m_shared_normals;
        }
        else {
            DebugLog("  Read normals");
            normalsParam.getIndexed(sample->m_normals, ss);
        }
    }

    sample->m_uvs.reset();
    auto uvsParam = m_schema.getUVsParam();
    if (!m_ignore_uvs && uvsParam.valid()) {
        if (uvsParam.isConstant()) {
            if (!m_shared_uvs.valid()) {
                DebugLog("  Read uvs (constant)");
                uvsParam.getIndexed(m_shared_uvs, ss);
            }

            sample->m_uvs = m_shared_uvs;
        }
        else {
            DebugLog("  Read uvs");
            uvsParam.getIndexed(sample->m_uvs, ss);
        }
    }

    auto boundsParam = m_schema.getSelfBoundsProperty();
    if (boundsParam) {
        boundsParam.get(sample->m_bounds, ss);
    }

    if (smoothNormalsRequired) {
        sample->computeSmoothNormals(m_config);
    }

    if (sample->tangentsRequired()) {
        const abcV3 *normals = nullptr;
        bool indexedNormals = false;
        
        if (smoothNormalsRequired) {
            normals = sample->m_smooth_normals.data();
        }
        else if (sample->m_normals.valid()) {
            normals = sample->m_normals.getVals()->get();
            indexedNormals = (sample->m_normals.getScope() == AbcGeom::kFacevaryingScope);
        }

        if (normals && sample->m_uvs.valid()) {
            // topology may be shared, check tangent indices
            if (topology->m_tangent_indices.empty() || !m_config.cache_tangents_splits) {
                sample->computeTangentIndices(m_config, normals, indexedNormals);
            }
            sample->computeTangents(m_config, normals, indexedNormals);
        }
    }

    if (m_config.turn_quad_edges) {
        if (m_varying_topology || topology->m_indices_swaped_face_winding.size() == 0) {
            auto faces = topology->m_counts;
            auto totalFaces = faces->size();
            
            auto * facesIndices = topology->m_face_indices->get();
            topology->m_indices_swaped_face_winding.reserve(topology->m_face_indices->size());
            
            auto index = 0;
            const uint32_t indexRemap[4] = {3,0,1,2};
            for (uint32_t faceIndex = 0; faceIndex < totalFaces; faceIndex++) {
                auto faceSize = faces->get()[faceIndex];
                if (faceSize == 4) {
                    for (auto i = 0; i < faceSize; i++) {
                        topology->m_indices_swaped_face_winding.push_back(facesIndices[index + indexRemap[i]]);
                    }
                }
                else {
                    for (auto i = 0; i < faceSize; i++) {
                        topology->m_indices_swaped_face_winding.push_back(facesIndices[index + i]);
                    }
                }
                index += faceSize;
            }

            if (sample->m_uvs.valid()) {
                index = 0;
                const auto& uvIndices = *sample->m_uvs.getIndices();
                topology->m_uv_indices_swaped_face_winding.reserve(sample->m_uvs.getIndices()->size());

                for (size_t faceIndex = 0; faceIndex < totalFaces; faceIndex++) {
                    auto faceSize = faces->get()[faceIndex];
                    if (faceSize == 4) {
                        for (auto i = 0; i < faceSize; i++) {
                            topology->m_uv_indices_swaped_face_winding.push_back(uvIndices[index + indexRemap[i]]);
                        }
                    }
                    else {
                        for (auto i = 0; i < faceSize; i++) {
                            topology->m_uv_indices_swaped_face_winding.push_back(uvIndices[index+i]);
                        }
                    }
                    index += faceSize;
                }
            }
        }
    }
    else if (topology->m_indices_swaped_face_winding.size()>0) {
        topology->m_indices_swaped_face_winding.clear();
    }

    if (m_config.share_vertices && !m_varying_topology && sample != nullptr && !sample->m_ownTopology && topology_changed)
        generateVerticesToFacesLookup(sample);

    return sample;
}

// generates two lookup tables:
//  m_FaceIndexingReindexed         : for each face in the abc sample, hold an index value to lookup in m_FixedTopoPositionsIndexes, that will give final position index.
//  m_FixedTopoPositionsIndexes     : list of resulting positions. value is index into the abc "position" vector. size is greter than or equal to "position" array.
void aiPolyMesh::generateVerticesToFacesLookup(aiPolyMeshSample *sample) const
{
    auto topology = sample->m_topology;
    auto  faces = topology->m_counts;

    auto * facesIndices = m_config.turn_quad_edges ?
        topology->m_indices_swaped_face_winding.data() : topology->m_face_indices->get();
    uint32_t totalFaces = static_cast<uint32_t>(faces->size());

    // 1st, figure out which face uses which vertices (for sharing identification)
    std::unordered_map< uint32_t, std::vector<uint32_t>> indexesOfFacesValues;
    uint32_t facesIndicesCursor = 0;
    for (uint32_t faceIndex = 0; faceIndex < totalFaces; faceIndex++)
    {
        uint32_t faceSize = (uint32_t)(faces->get()[faceIndex]);
        for (uint32_t i = 0; i < faceSize; ++i, ++facesIndicesCursor)
            indexesOfFacesValues[ facesIndices[facesIndicesCursor] ].push_back(facesIndicesCursor);
    }

    // 2nd, figure out which vertex can be merged, which cannot.
    // If all faces targetting a vertex give it the same normal and UV, then it can be shared.
    const abcV3 * normals = sample->m_smooth_normals.empty() && sample->m_normals.valid() ?  sample->m_normals.getVals()->get() : sample->m_smooth_normals.data();
    bool normalsIndexed = !sample->m_smooth_normals.empty() ? false : sample->m_normals.valid() && (sample->m_normals.getScope() == AbcGeom::kFacevaryingScope);
    const uint32_t *Nidxs = normalsIndexed ? sample->m_normals.getIndices()->get() : (uint32_t*)(topology->m_face_indices->get());
    
    bool hasUVs = sample->m_uvs.valid();
    const auto *uvVals = sample->m_uvs.getVals()->get(); 
    const auto *uvIdxs = m_config.turn_quad_edges || !hasUVs ? topology->m_uv_indices_swaped_face_winding.data() : sample->m_uvs.getIndices()->get();

    topology->m_fixed_topo_positions_indexes.clear();
    topology->m_face_indexing_reindexed.clear();
    topology->m_face_indexing_reindexed.resize(topology->m_face_indices->size() );
    topology->m_fixed_topo_positions_indexes.reserve(sample->m_points->size());
    topology->m_freshly_read_topology_data = true;

    auto itr = indexesOfFacesValues.begin();
    while (itr != indexesOfFacesValues.end())
    {
        auto& vertexUsages = itr->second;
        uint32_t vertexUsageIndex = 0;
        size_t vertexUsageMaxIndex = itr->second.size();
        const abcV2 * prevUV = nullptr;
        const abcV3 * prevN = nullptr;
        bool share = true;
        do
        {
            uint32_t index = vertexUsages[vertexUsageIndex];
            // same Normal?
            if( normals )
            {
                const abcV3 & N = normals[Nidxs ? Nidxs[index] : index];
                if (prevN == nullptr)
                    prevN = &N;
                else
                    share = N == *prevN;
            }
            // Same UV?
            if (hasUVs)
            {
                const Abc::V2f & uv = uvVals[uvIdxs[index]];
                if (prevUV == nullptr)
                    prevUV = &uv;
                else
                    share = uv == *prevUV;
            }
        }
        while (share && ++vertexUsageIndex < vertexUsageMaxIndex);

        // Verdict is in for this vertex.
        if (share)
            topology->m_fixed_topo_positions_indexes.push_back(itr->first);

        auto indexItr = itr->second.begin();
        while( indexItr != itr->second.end() )
        {
            if (!share)
                topology->m_fixed_topo_positions_indexes.push_back(itr->first);

            topology->m_face_indexing_reindexed[*indexItr] = (uint32_t)topology->m_fixed_topo_positions_indexes.size() - 1;

            ++indexItr;
        }
        ++itr;
    }

    // We now have a lookup for face value indexes that re-routes to shared indexes when possible!
    // Splitting does not work with shared vertices, if the resulting mesh still exceeds the splitting threshold, then disable vertex sharing.
    const int maxVertexSplitCount = topology->m_use_32bit_index_buffer ? MAX_VERTEX_SPLIT_COUNT_32 : MAX_VERTEX_SPLIT_COUNT_16;
    if(topology->m_fixed_topo_positions_indexes.size() / maxVertexSplitCount>0)
    {
        topology->m_vertex_sharing_enabled = false;
        topology->m_fixed_topo_positions_indexes.clear();
        topology->m_face_indexing_reindexed.clear();
    }
}

void aiPolyMesh::updatePeakIndexCount() const
{
    if (m_peak_index_count != 0) { return; }

    DebugLog("aiPolyMesh::updateMaxIndex()");

    Util::Dimensions dim;
    Abc::Int32ArraySamplePtr counts;

    auto indicesProp = m_schema.getFaceIndicesProperty();
    auto countsProp = m_schema.getFaceCountsProperty();

    int numSamples = static_cast<int>(indicesProp.getNumSamples());
    if (numSamples == 0) { return; }

    size_t cMax = 0;
    if (indicesProp.isConstant())
    {
        auto ss = Abc::ISampleSelector(int64_t(0));
        countsProp.get(counts, ss);
        indicesProp.getDimensions(dim, ss);
        cMax = dim.numPoints();
    }
    else
    {
        DebugLog("Checking %d sample(s)", numSamples);
        int iMax = 0;

        for (int i = 0; i < numSamples; ++i) {
            indicesProp.getDimensions(dim, Abc::ISampleSelector(int64_t(i)));
            size_t numIndices = dim.numPoints();
            if (numIndices > cMax) {
                cMax = numIndices;
                iMax = i;
            }
        }

        countsProp.get(counts, Abc::ISampleSelector(int64_t(iMax)));
    }

    m_peak_index_count = (int)cMax;
    m_peak_triangulated_index_count = CalculateTriangulatedIndexCount(*counts);
}

int aiPolyMesh::getPeakIndexCount() const
{
    updatePeakIndexCount();
    return m_peak_triangulated_index_count;
}

int aiPolyMesh::getPeakTriangulatedIndexCount() const
{
    updatePeakIndexCount();
    return m_peak_triangulated_index_count;
}

int aiPolyMesh::getPeakVertexCount() const
{
    if (m_peak_vertex_count == 0) {
        DebugLog("aiPolyMesh::getPeakVertexCount()");
        
        Util::Dimensions dim;
        auto positionsProp = m_schema.getPositionsProperty();
        int numSamples = (int)positionsProp.getNumSamples();

        if (numSamples == 0) {
            return 0;
        }
        else if (positionsProp.isConstant()) {
            positionsProp.getDimensions(dim, Abc::ISampleSelector(int64_t(0)));
            m_peak_vertex_count = (int)dim.numPoints();
        }
        else {
            m_peak_vertex_count = 0;
            for (int i = 0; i < numSamples; ++i) {
                positionsProp.getDimensions(dim, Abc::ISampleSelector(int64_t(i)));                
                size_t numVertices = dim.numPoints();
                if (numVertices > size_t(m_peak_vertex_count)) {
                    m_peak_vertex_count = int(numVertices);
                }
            }
        }
    }
    return m_peak_vertex_count;
}

void aiPolyMesh::getSummary(aiMeshSummary &summary) const
{
    DebugLog("aiPolyMesh::getSummary()");
    
    summary.topology_variance = static_cast<int>(m_schema.getTopologyVariance());
    summary.peak_vertex_count = getPeakVertexCount();
    summary.peak_index_count = getPeakIndexCount();
    summary.peak_triangulated_index_count = getPeakTriangulatedIndexCount();
    summary.peak_submesh_count = ceildiv(summary.peak_index_count, 64998);
}
