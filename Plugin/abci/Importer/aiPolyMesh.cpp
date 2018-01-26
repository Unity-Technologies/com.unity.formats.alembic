#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiPolyMesh.h"
#include <unordered_map>
#include "../Foundation/aiMisc.h"
#include "../Foundation/aiMath.h"


#define MAX_VERTEX_SPLIT_COUNT_16 65000
#define MAX_VERTEX_SPLIT_COUNT_32 2000000000

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

template<class Container>
static inline void GenerateOffsetTable(const Container& counts, RawVector<int>& dst)
{
    size_t size = counts.size();
    dst.resize_discard(size);
    int total = 0;
    for (size_t i = 0; i < size; ++i) {
        dst[i] = total;
        total += counts[i];
    }
}

Topology::Topology()
{
}

void Topology::clear()
{
    DebugLog("Topology::clear()");
    m_indices_orig.reset();
    m_counts.reset();
    m_indices.clear();
    m_refiner.clear();
}

int Topology::getTriangulatedIndexCount() const
{
    return (int)m_indices.size();
}

int Topology::getSplitCount() const
{
    return (int)m_refiner.splits.size();
}

int Topology::getSplitVertexCount(int split_index) const
{
    return (int)m_refiner.splits[split_index].num_vertices;
}

int Topology::getSubmeshCount(int split_index) const
{
    return (int)m_refiner.splits[split_index].num_submeshes;
}

aiPolyMeshSample::aiPolyMeshSample(aiPolyMesh *schema, TopologyPtr topo, bool ownTopo)
    : super(schema)
    , m_topology(topo)
    , m_own_topology(ownTopo)
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
        return m_normals_orig.valid();
    case aiNormalsMode::Ignore:
        return false;
    default:
        return !m_normals.empty();
    }
}

bool aiPolyMeshSample::hasUVs() const
{
    return m_uvs_orig.valid();
}

bool aiPolyMeshSample::hasVelocities() const
{
    return !m_schema->hasVaryingTopology() && m_config.interpolate_samples;
}

bool aiPolyMeshSample::hasTangents() const
{
    return (m_config.tangents_mode != aiTangentsMode::None && hasUVs() && hasNormals() && !m_tangents.empty());
}

bool aiPolyMeshSample::computeNormalsRequired() const
{
    return (m_config.normals_mode == aiNormalsMode::AlwaysCompute ||
            m_config.tangents_mode == aiTangentsMode::Smooth ||
            (!m_normals_orig.valid() && m_config.normals_mode == aiNormalsMode::ComputeIfMissing));
}

bool aiPolyMeshSample::computeTangentsRequired() const
{
    return (m_config.tangents_mode != aiTangentsMode::None);
}

void aiPolyMeshSample::computeNormals(const aiConfig &config)
{
    DebugLog("%s: Compute smooth normals", getSchema()->getObject()->getFullName());

    m_normals_generated.resize_zeroclear(m_points_orig->size());
    const auto &counts = *(m_topology->m_counts);
    const auto &indices = *(m_topology->m_indices_orig);
    const auto &positions = *m_points_orig;

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
                m_normals_generated[indices[off + fv]] += N;
            }
        }
        off += nfv;
    }

    // Normalize normal vectors
    for (abcV3& v : m_normals_generated) { v.normalize(); }
    m_normals = { m_normals_generated.data(), m_normals_generated.size() };
}


void aiPolyMeshSample::computeTangents(const aiConfig &config, const abcV3 *inN, bool indexed_normals)
{
    // todo
}

void aiPolyMeshSample::updateConfig(const aiConfig &config, bool &topology_changed, bool &data_changed)
{
    DebugLog("aiPolyMeshSample::updateConfig()");
    
    topology_changed = (config.swap_face_winding != m_config.swap_face_winding);
    data_changed = (config.swap_handedness != m_config.swap_handedness);

    bool compute_normals_required = (config.normals_mode == aiNormalsMode::AlwaysCompute ||
                                  config.tangents_mode == aiTangentsMode::Smooth ||
                                  (!m_normals_orig.valid() && config.normals_mode == aiNormalsMode::ComputeIfMissing));
    
    if (compute_normals_required) {
        if (m_normals_generated.empty() || topology_changed) {
            computeNormals(config);
            data_changed = true;
        }
    }
    else {
        if (!m_normals_generated.empty()) {
            DebugLog("%s: Clear smooth normals", getSchema()->getObject()->getFullName());
            m_normals_generated.clear();
            data_changed = true;
        }
    }

    bool tangents_required = (m_uvs_orig.valid() && config.tangents_mode != aiTangentsMode::None);

    if (tangents_required) {
        bool tangents_mode_changed = (config.tangents_mode != m_config.tangents_mode);

        const abcV3 *N = nullptr;
        bool Nindexed = false;

        if (compute_normals_required) {
            N = m_normals_generated.data();
        }
        else if (m_normals_orig.valid()) {
            N = m_normals_orig.getVals()->get();
            Nindexed = (m_normals_orig.getScope() == AbcGeom::kFacevaryingScope);
        }

        if (N) {
            if (m_tangents.empty() || tangents_mode_changed || topology_changed) {
                computeTangents(config, N, Nindexed);
                data_changed = true;
            }
        }
        else {
            tangents_required = false;
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
    
    summary.splitCount = m_topology->getSplitCount();
    summary.has_normals = hasNormals();
    summary.has_uvs = hasUVs();
    summary.has_tangents = hasTangents();
    summary.has_velocities = hasVelocities();
}


void aiPolyMeshSample::getDataPointer(aiPolyMeshData &dst) const
{
    if (m_points_orig) {
        dst.position_count = m_points_orig->valid() ? (int)m_points_orig->size() : 0;
        dst.points = (abcV3*)(m_points_orig->get());
    }

    if (m_velocities_orig) {
        dst.velocities = m_velocities_orig->valid() ? (abcV3*)m_velocities_orig->get() : nullptr;
    }

    if (m_normals_orig) {
        dst.normal_count = (int)m_normals_orig.getVals()->size();
        dst.normals = (abcV3*)m_normals_orig.getVals()->get();
        dst.normal_index_count = m_normals_orig.isIndexed() ? (int)m_normals_orig.getIndices()->size() : 0;
        if (dst.normal_index_count) {
            dst.normal_indices = (int*)m_normals_orig.getIndices()->get();
        }
    }

    if (m_uvs_orig) {
        dst.uv_count = (int)m_uvs_orig.getVals()->size();
        dst.uvs = (abcV2*)m_uvs_orig.getVals()->get();
        dst.uv_index_count = m_uvs_orig.isIndexed() ? (int)m_uvs_orig.getIndices()->size() : 0;
        if (dst.uv_index_count) {
            dst.uv_indices = (int*)m_uvs_orig.getIndices()->get();
        }
    }

    if (m_topology) {
        if (m_topology->m_indices_orig) {
            dst.index_count = (int)m_topology->m_indices_orig->size();
            dst.indices = (int*)m_topology->m_indices_orig->get();
        }
        if (m_topology->m_counts) {
            dst.face_count = (int)m_topology->m_counts->size();
            dst.faces = (int*)m_topology->m_counts->get();
            dst.triangulated_index_count = (int)m_topology->m_indices.size();
        }
    }

    dst.center = m_bounds.center();
    dst.size = m_bounds.size();
}

int aiPolyMeshSample::getSplitVertexCount(int split_index) const
{
    DebugLog("aiPolyMeshSample::getVertexBufferLength(split_index=%d)", split_index);
    
    return m_topology->getSplitVertexCount(split_index);
}

void aiPolyMeshSample::fillSplitVertices(int split_index, aiPolyMeshData &data)
{
    DebugLog("aiPolyMeshSample::fillVertexBuffer(split_index=%d)", split_index);
    
    auto& splits = m_topology->m_refiner.splits;
    if (split_index < 0 || size_t(split_index) >= splits.size() || splits[split_index].num_vertices == 0)
        return;

    bool copy_normals = (hasNormals() && data.normals);
    bool copy_uvs = (hasUVs() && data.uvs);
    bool copy_tangents = (hasTangents() && data.tangents);
    
    bool use_abc_normals = (m_normals_orig.valid() && (m_config.normals_mode == aiNormalsMode::ReadFromFile || m_config.normals_mode == aiNormalsMode::ComputeIfMissing));
    bool interpolate = hasVelocities() && m_points_next != nullptr;

    auto& refiner = m_topology->m_refiner;
    auto& split = refiner.splits[split_index];

    if (interpolate) {
        m_points_generated.resize_discard(m_points_orig->size());
        m_velocity_generated.resize_discard(m_points_orig->size());
        m_points = { m_points_generated.data(), m_points_generated.size() };
        m_velocities = { m_velocity_generated.data(), m_velocity_generated.size() };
        gen_velocity(
            m_points_generated.data(), m_velocity_generated.data(),
            m_points_orig->get(), m_points_next->get(), (int)m_points_orig->size(),
            (float)m_current_time_offset, (float)m_current_time_interval, m_config.vertex_motion_scale);
    }

    if (data.points) {
        IArray<int> remap{ &refiner.new2old_points[split.offset_vertices], (size_t)split.num_vertices };
        CopyWithIndices(data.points, m_points.data(), remap);
        if (m_config.swap_handedness) { swap_handedness(data.points, split.num_vertices); }
    }
    if (data.normals) {
        if (copy_normals) {
            IArray<int> remap{ &refiner.new2old_normals[split.offset_vertices], (size_t)split.num_vertices };
            CopyWithIndices(data.normals, m_normals.data(), remap);
            if (m_config.swap_handedness) { swap_handedness(data.normals, split.num_vertices); }
        }
        else {
            memset(data.normals, 0, split.num_vertices * sizeof(float3));
        }
    }
    if (data.uvs) {
        if (copy_uvs) {
            IArray<int> remap{ &refiner.new2old_uvs[split.offset_vertices], (size_t)split.num_vertices };
            CopyWithIndices(data.uvs, m_uvs_orig.getVals()->get(), remap);
        }
        else {
            memset(data.uvs, 0, split.num_vertices * sizeof(abcV2));
        }
    }
    if (data.tangents)
    {
        if (copy_tangents) {
            // todo
            //if (m_config.swap_handedness) { swap_handedness(data.normals, split.num_vertices); }
        }
        else {
            memset(data.tangents, 0, split.num_vertices * sizeof(abcV4));
        }
    }

    {
        abcV3 bbmin, bbmax;
        get_bounds(bbmin, bbmax, data.points, split.num_vertices);
        data.center = 0.5f * (bbmin + bbmax);
        data.size = bbmax - bbmin;
    }

    m_topology->m_freshly_read_topology_data = false;
}

int aiPolyMeshSample::getSubmeshCount(int split_index) const
{
    return m_topology->getSubmeshCount(split_index);
}

void aiPolyMeshSample::getSubmeshSummary(int split_index, int submesh_index, aiSubmeshSummary &summary)
{
    auto& refiner = m_topology->m_refiner;
    auto& split = refiner.splits[split_index];
    auto& submesh = refiner.submeshes[split.offset_submeshes + submesh_index];

    summary.split_index = submesh.split_index;
    summary.submesh_index = submesh.submesh_index;
    summary.index_count = submesh.num_indices;
}

void aiPolyMeshSample::fillSubmeshIndices(int split_index, int submesh_index, aiSubmeshData &data) const
{
    auto& refiner = m_topology->m_refiner;
    auto& split = refiner.splits[split_index];
    auto& submesh = refiner.submeshes[split.offset_submeshes + submesh_index];
    refiner.new_indices_submeshes.copy_to(data.indices, submesh.num_indices, submesh.offset_indices);
}

// ---

aiPolyMesh::aiPolyMesh(aiObject *obj)
    : super(obj)
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
    auto& topology = *sample->m_topology;

    topology_changed = m_varying_topology;
    topology.m_use_32bit_index_buffer = m_config.use_32bit_index_buffer;

    if (!topology.m_counts || m_varying_topology) {
        m_schema.getFaceCountsProperty().get(topology.m_counts, ss);
        topology.m_indices.resize_discard(CalculateTriangulatedIndexCount(*topology.m_counts));
        topology_changed = true;
    }
    if (!topology.m_indices_orig || m_varying_topology) {
        m_schema.getFaceIndicesProperty().get(topology.m_indices_orig, ss);
        topology_changed = true;
    }
    if (topology_changed && !m_facesets.empty()) {
        sample->m_facesets.resize(m_facesets.size());
        for (size_t fi = 0; fi < m_facesets.size(); ++fi) {
            m_facesets[fi].get(sample->m_facesets[fi], ss);
        }
    }

    m_schema.getPositionsProperty().get(sample->m_points_orig, ss);
    if (!m_varying_topology && m_config.interpolate_samples) {
        m_schema.getPositionsProperty().get(sample->m_points_next, ss2);
    }
    else {
        sample->m_points = { sample->m_points_orig->get(), sample->m_points_orig->size() };

        sample->m_velocities_orig.reset();
        auto velocities_prop = m_schema.getVelocitiesProperty();
        if (velocities_prop.valid()) {
            velocities_prop.get(sample->m_velocities_orig, ss);
            sample->m_velocities = { sample->m_velocities_orig->get(), sample->m_velocities_orig->size() };
        }
    }

    sample->m_uvs_orig.reset();
    auto uvs_param = m_schema.getUVsParam();
    if (!m_ignore_uvs && uvs_param.valid()) {
        if (uvs_param.isConstant()) {
            if (!m_constant_uvs.valid()) {
                DebugLog("  Read uvs (constant)");
                uvs_param.getIndexed(m_constant_uvs, ss);
            }
            sample->m_uvs_orig = m_constant_uvs;
        }
        else {
            DebugLog("  Read uvs");
            uvs_param.getIndexed(sample->m_uvs_orig, ss);
        }
    }

    bool compute_normals_required = sample->computeNormalsRequired();
    bool compute_tangents_required = sample->computeTangentsRequired();

    sample->m_normals_orig.reset();
    auto normals_param = m_schema.getNormalsParam();
    if (!m_ignore_normals && normals_param.valid()) {
        if (normals_param.isConstant()) {
            if (!m_constant_normals.valid()) {
                DebugLog("  Read normals (constant)");
                normals_param.getIndexed(m_constant_normals, ss);
            }
            sample->m_normals_orig = m_constant_normals;
        }
        else {
            DebugLog("  Read normals");
            normals_param.getIndexed(sample->m_normals_orig, ss);
        }
        sample->m_normals = { sample->m_normals_orig.getVals()->get(), sample->m_normals_orig.getVals()->size() };
    }

    if (compute_normals_required)
        sample->computeNormals(m_config);

    if (compute_tangents_required) {
        const abcV3 *normals = nullptr;
        bool indexed_normals = false;
        
        if (compute_normals_required) {
            normals = sample->m_normals_generated.data();
        }
        else if (sample->m_normals_orig.valid()) {
            normals = sample->m_normals_orig.getVals()->get();
            indexed_normals = (sample->m_normals_orig.getScope() == AbcGeom::kFacevaryingScope);
        }

        if (normals && sample->m_uvs_orig.valid()) {
            sample->computeTangents(m_config, normals, indexed_normals);
        }
    }

    auto bounds_param = m_schema.getSelfBoundsProperty();
    if (bounds_param)
        bounds_param.get(sample->m_bounds, ss);

    if (m_config.turn_quad_edges) {
        // todo
    }

    if (topology_changed)
        generateSplitAndSubmeshes(sample);

    topology.m_freshly_read_topology_data = topology_changed;
    return sample;
}

void aiPolyMesh::generateSplitAndSubmeshes(aiPolyMeshSample *sample) const
{
    auto& topology = *sample->m_topology;
    auto& refiner = topology.m_refiner;
    refiner.clear();

    refiner.split_unit = topology.m_use_32bit_index_buffer ? MAX_VERTEX_SPLIT_COUNT_32 : MAX_VERTEX_SPLIT_COUNT_16;
    refiner.counts = { topology.m_counts->get(), topology.m_counts->size() };
    refiner.indices = { topology.m_indices_orig->get(), topology.m_indices_orig->size() };
    refiner.points = (IArray<float3>&)sample->m_points;

    refiner.normals = (IArray<float3>&)sample->m_normals;
    if (sample->m_normals_orig.valid() && sample->m_normals_orig.getIndices()->valid())
        refiner.normal_indices = { (int*)sample->m_normals_orig.getIndices()->get(), sample->m_normals_orig.getIndices()->size() };
    else if (refiner.normals.size() == refiner.points.size())
        refiner.normal_indices = refiner.indices;

    if (sample->m_uvs_orig.valid()) {
        refiner.uvs = { (float2*)sample->m_uvs_orig.getVals()->get(), sample->m_uvs_orig.getVals()->size() };
        if (sample->m_uvs_orig.getIndices()->valid())
            refiner.uv_indices = { (int*)sample->m_uvs_orig.getIndices()->get(), sample->m_uvs_orig.getIndices()->size() };
        else if (refiner.uvs.size() == refiner.points.size())
            refiner.uv_indices = refiner.indices;
    }

    // use face set index as material id
    topology.m_material_ids.clear();
    topology.m_material_ids.resize(refiner.counts.size(), -1);
    for (size_t fsi = 0; fsi < sample->m_facesets.size(); ++fsi) {
        auto& faces = *sample->m_facesets[fsi].getFaces();
        size_t num_faces = faces.size();
        for (size_t fi = 0; fi < num_faces; ++fi) {
            topology.m_material_ids[faces[fi]] = (int)fsi;
        }
    }

    refiner.refine();
    refiner.triangulate(m_config.swap_face_winding);
    refiner.genSubmeshes(topology.m_material_ids);

    topology.m_freshly_read_topology_data = true;
}

void aiPolyMesh::getSummary(aiMeshSummary &summary) const
{
    DebugLog("aiPolyMesh::getSummary()");
    
    summary.topology_variance = static_cast<int>(m_schema.getTopologyVariance());
}
