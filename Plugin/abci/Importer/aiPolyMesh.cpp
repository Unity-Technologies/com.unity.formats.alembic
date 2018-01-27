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
static inline void GenerateOffsetTable(RawVector<int>& dst, const Container& counts)
{
    size_t size = counts.size();
    dst.resize_discard(size);
    int total = 0;
    for (size_t i = 0; i < size; ++i) {
        dst[i] = total;
        total += counts[i];
    }
}

template<class T, class IndexArray>
inline void CopyWithIndices(T *dst, const T *src, const IndexArray& indices)
{
    if (!dst || !src) { return; }
    size_t size = indices.size();
    for (size_t i = 0; i < (int)size; ++i) {
        dst[i] = src[indices[i]];
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
    m_offsets.clear();
    m_material_ids.clear();
    m_refiner.clear();
    m_triangulated_index_count = 0;
}

int Topology::getTriangulatedIndexCount() const
{
    return m_triangulated_index_count;
}

int Topology::getSplitCount() const
{
    return (int)m_refiner.splits.size();
}

int Topology::getSplitVertexCount(int split_index) const
{
    return (int)m_refiner.splits[split_index].num_vertices;
}

int Topology::getSubmeshCount() const
{
    return (int)m_refiner.submeshes.size();
}

int Topology::getSubmeshCount(int split_index) const
{
    return (int)m_refiner.splits[split_index].num_submeshes;
}

void Topology::onTopologyUpdate(const aiConfig &config, aiPolyMeshSample& sample)
{
    if (config.turn_quad_edges) {
        // todo
    }

    auto& refiner = m_refiner;
    refiner.clear();

    refiner.split_unit = m_use_32bit_index_buffer ? MAX_VERTEX_SPLIT_COUNT_32 : MAX_VERTEX_SPLIT_COUNT_16;
    refiner.counts = { m_counts->get(), m_counts->size() };
    refiner.indices = { m_indices_orig->get(), m_indices_orig->size() };
    refiner.points = { (float3*)sample.m_points_orig->get(), sample.m_points_orig->size() };

    if (sample.m_normals_orig.valid()) {
        IArray<abcV3> normals { sample.m_normals_orig.getVals()->get(), sample.m_normals_orig.getVals()->size() };
        if (sample.m_normals_orig.isIndexed()) {
            IArray<int> normal_indices { (int*)sample.m_normals_orig.getIndices()->get(), sample.m_normals_orig.getIndices()->size() };
            refiner.addIndexedAttribute<abcV3>(normals, normal_indices, sample.m_normals, m_remap_normals);
        }
        else if (normals.size() == refiner.indices.size()) {
            refiner.addExpandedAttribute<abcV3>(normals, sample.m_normals, m_remap_normals);
        }
        else if (normals.size() == refiner.points.size()) {
            refiner.addIndexedAttribute<abcV3>(normals, refiner.indices, sample.m_normals, m_remap_normals);
        }
        else {
            DebugLog("Invalid attribute");
        }
    }

    if (sample.m_uv0_orig.valid()) {
        IArray<abcV2> uv0 { sample.m_uv0_orig.getVals()->get(), sample.m_uv0_orig.getVals()->size() };
        if (sample.m_uv0_orig.isIndexed()) {
            IArray<int> uv0_indices{ (int*)sample.m_uv0_orig.getIndices()->get(), sample.m_uv0_orig.getIndices()->size() };
            refiner.addIndexedAttribute<abcV2>(uv0, uv0_indices, sample.m_uv0, m_remap_uv0);
        }
        else if (uv0.size() == refiner.indices.size()) {
            refiner.addExpandedAttribute<abcV2>(uv0, sample.m_uv0, m_remap_uv0);
        }
        else if (uv0.size() == refiner.points.size()) {
            refiner.addIndexedAttribute<abcV2>(uv0, refiner.indices, sample.m_uv0, m_remap_uv0);
        }
        else {
            DebugLog("Invalid attribute");
        }
    }

    if (sample.m_uv1_orig.valid()) {
        IArray<abcV2> uv1{ sample.m_uv1_orig.getVals()->get(), sample.m_uv1_orig.getVals()->size() };
        if (sample.m_uv1_orig.isIndexed()) {
            IArray<int> uv1_indices{ (int*)sample.m_uv1_orig.getIndices()->get(), sample.m_uv1_orig.getIndices()->size() };
            refiner.addIndexedAttribute<abcV2>(uv1, uv1_indices, sample.m_uv1, m_remap_uv1);
        }
        else if (uv1.size() == refiner.indices.size()) {
            refiner.addExpandedAttribute<abcV2>(uv1, sample.m_uv1, m_remap_uv1);
        }
        else if (uv1.size() == refiner.points.size()) {
            refiner.addIndexedAttribute<abcV2>(uv1, refiner.indices, sample.m_uv1, m_remap_uv1);
        }
        else {
            DebugLog("Invalid attribute");
        }
    }

    if (sample.m_colors_orig.valid()) {
        IArray<abcC4> colors{ sample.m_colors_orig.getVals()->get(), sample.m_colors_orig.getVals()->size() };
        if (sample.m_colors_orig.isIndexed()) {
            IArray<int> colors_indices{ (int*)sample.m_colors_orig.getIndices()->get(), sample.m_colors_orig.getIndices()->size() };
            refiner.addIndexedAttribute<abcC4>(colors, colors_indices, sample.m_colors, m_remap_colors);
        }
        else if (colors.size() == refiner.indices.size()) {
            refiner.addExpandedAttribute<abcC4>(colors, sample.m_colors, m_remap_colors);
        }
        else if (colors.size() == refiner.points.size()) {
            refiner.addIndexedAttribute<abcC4>(colors, refiner.indices, sample.m_colors, m_remap_colors);
        }
        else {
            DebugLog("Invalid attribute");
        }
    }

    // use face set index as material id
    m_material_ids.resize(refiner.counts.size(), -1);
    for (size_t fsi = 0; fsi < sample.m_facesets.size(); ++fsi) {
        auto& faces = *sample.m_facesets[fsi].getFaces();
        size_t num_faces = faces.size();
        for (size_t fi = 0; fi < num_faces; ++fi) {
            m_material_ids[faces[fi]] = (int)fsi;
        }
    }

    refiner.refine();
    refiner.triangulate(config.swap_face_winding);
    refiner.genSubmeshes(m_material_ids);

    m_remap_points.swap(refiner.new2old_points);

    m_offsets.clear();
    m_freshly_read_topology_data = true;
}

RawVector<int>& Topology::getOffsets()
{
    if (m_offsets.empty())
        GenerateOffsetTable(m_offsets, *m_counts);
    return m_offsets;
}

aiPolyMeshSample::aiPolyMeshSample(aiPolyMesh *schema, TopologyPtr topo, bool ownTopo)
    : super(schema)
    , m_topology(topo)
    , m_own_topology(ownTopo)
{
}

bool aiPolyMeshSample::hasVelocities() const
{
    return !m_schema->hasVaryingTopology() && m_config.interpolate_samples;
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

bool aiPolyMeshSample::hasTangents() const
{
    return (m_config.tangents_mode != aiTangentsMode::None && hasUV0() && hasNormals() && !m_tangents.empty());
}

bool aiPolyMeshSample::hasUV0() const
{
    return m_uv0_orig.valid();
}

bool aiPolyMeshSample::hasUV1() const
{
    return m_uv1_orig.valid();
}

bool aiPolyMeshSample::hasColors() const
{
    return m_colors_orig.valid();
}

bool aiPolyMeshSample::computeNormalsRequired() const
{
    return (m_config.normals_mode == aiNormalsMode::AlwaysCompute ||
            (!m_normals_orig.valid() && m_config.normals_mode == aiNormalsMode::ComputeIfMissing));
}

bool aiPolyMeshSample::computeTangentsRequired() const
{
    return (m_config.tangents_mode != aiTangentsMode::None);
}

void aiPolyMeshSample::interpolatePoints()
{
}

void aiPolyMeshSample::computeNormals(const aiConfig &config)
{
    DebugLog("%s: Compute smooth normals", getSchema()->getObject()->getFullName());

    const auto &indices = m_topology->m_refiner.new_indices_triangulated;
    m_normals.resize_zeroclear(m_points.size());
    GenerateNormals(m_normals.data(), m_points.data(), indices.data(), (int)m_points.size(), (int)indices.size() / 3);
}

void aiPolyMeshSample::interpolateNormals()
{
    if (!m_normals_orig.valid() || !m_normals_orig2.valid())
        return;

    const auto *n1 = m_normals_orig.getVals()->get();
    const auto *n2 = m_normals_orig2.getVals()->get();
    auto& remap = m_topology->m_remap_normals;
    float w = (float)m_current_time_offset;
    float iw = 1.0f - w;

    auto n = remap.size();
    m_normals.resize_discard(n);
    for (size_t i = 0; i < n; ++i) {
        int ri = remap[i];
        m_normals[i] = n1[ri] * iw + n2[ri] * w;
    }
}

void aiPolyMeshSample::computeTangents(const aiConfig &config)
{
    const auto &indices = m_topology->m_refiner.new_indices_triangulated;
    m_tangents.resize_zeroclear(m_points.size());
    GenerateTangents(m_tangents.data(), m_points.data(), m_uv0.data(), m_normals.data(), indices.data(), (int)m_points.size(), (int)indices.size() / 3);
}

void aiPolyMeshSample::updateConfig(const aiConfig &config, bool &topology_changed, bool &data_changed)
{
    DebugLog("aiPolyMeshSample::updateConfig()");
    
    topology_changed = (config.swap_face_winding != m_config.swap_face_winding);
    data_changed = (config.swap_handedness != m_config.swap_handedness);

    bool compute_normals_required = (config.normals_mode == aiNormalsMode::AlwaysCompute ||
                                  (!m_normals_orig.valid() && config.normals_mode == aiNormalsMode::ComputeIfMissing));
    
    if (compute_normals_required) {
        if (m_normals.empty() || topology_changed) {
            computeNormals(config);
            data_changed = true;
        }
    }
    else if(m_normals_orig.valid()) {
        auto& remap = m_topology->m_remap_normals;
        auto n = remap.size();
        m_normals.resize_discard(n);
        const auto *n1 = m_normals_orig.getVals()->get();
        for (size_t i = 0; i < n; ++i) {
            m_normals[i] = n1[remap[i]];
        }
    }
    else {
        m_normals.clear();
    }

    bool tangents_required = (m_uv0_orig.valid() && config.tangents_mode != aiTangentsMode::None);
    if (tangents_required) {
        if (!m_normals.empty() && !m_uv0.empty()) {
            bool tangents_mode_changed = (config.tangents_mode != m_config.tangents_mode);
            if (m_tangents.empty() || tangents_mode_changed || topology_changed) {
                computeTangents(config);
                data_changed = true;
            }
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

    summary.split_count = m_topology->getSplitCount();
    summary.submesh_count = m_topology->getSubmeshCount();
    summary.has_velocities = hasVelocities();
    summary.has_normals = hasNormals();
    summary.has_tangents = hasTangents();
    summary.has_uv0 = hasUV0();
    summary.has_uv1 = hasUV1();
    summary.has_colors = hasColors();
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

    if (m_uv0_orig) {
        dst.uv_count = (int)m_uv0_orig.getVals()->size();
        dst.uv0 = (abcV2*)m_uv0_orig.getVals()->get();
        dst.uv_index_count = m_uv0_orig.isIndexed() ? (int)m_uv0_orig.getIndices()->size() : 0;
        if (dst.uv_index_count) {
            dst.uv_indices = (int*)m_uv0_orig.getIndices()->get();
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
            dst.triangulated_index_count = m_topology->m_triangulated_index_count;
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

void aiPolyMeshSample::prepareSplits()
{
}

void aiPolyMeshSample::fillSplitVertices(int split_index, aiPolyMeshData &data)
{
    DebugLog("aiPolyMeshSample::fillVertexBuffer(split_index=%d)", split_index);
    
    auto& schema = *dynamic_cast<schema_t*>(getSchema());
    auto& splits = m_topology->m_refiner.splits;
    if (split_index < 0 || size_t(split_index) >= splits.size() || splits[split_index].num_vertices == 0)
        return;

    bool copy_normals = (hasNormals() && data.normals);
    bool copy_uv0 = (hasUV0() && data.uv0);
    bool copy_tangents = (hasTangents() && data.tangents);
    
    bool use_abc_normals = (m_normals_orig.valid() && (m_config.normals_mode == aiNormalsMode::ReadFromFile || m_config.normals_mode == aiNormalsMode::ComputeIfMissing));
    bool interpolate = m_config.interpolate_samples && !schema.isConstant() && !schema.hasVaryingTopology();

    auto& refiner = m_topology->m_refiner;
    auto& split = refiner.splits[split_index];

    if (interpolate) {
        m_velocities.resize_discard(m_points_orig->size());
        GenerateVelocities(m_velocities.data(),
            m_points_orig->get(), m_points_orig2->get(), (int)m_points_orig->size(),
            (float)m_current_time_interval, m_config.vertex_motion_scale);
    }

    if (data.points) {
        if (interpolate)
            Lerp(data.points, m_points.data(), m_points2.data(), (int)m_points.size(), (float)m_current_time_offset);
        else
            m_points.copy_to(data.points, split.num_vertices, split.offset_vertices);
        if (m_config.swap_handedness) { SwapHandedness(data.points, split.num_vertices); }
    }

    if (data.normals) {
        if (copy_normals) {
            if (interpolate) {
                if (use_abc_normals) {
                    Lerp(data.normals, m_normals.data(), m_normals2.data(), (int)m_normals.size(), (float)m_current_time_offset);
                }
                else {
                    computeNormals(m_config);
                    m_normals.copy_to(data.normals, split.num_vertices, split.offset_vertices);
                }
            }
            else {
                m_normals.copy_to(data.normals, split.num_vertices, split.offset_vertices);
            }
            if (m_config.swap_handedness) { SwapHandedness(data.normals, split.num_vertices); }
        }
        else {
            memset(data.normals, 0, split.num_vertices * sizeof(abcV3));
        }
    }

    if (data.tangents)
    {
        if (copy_tangents) {
            if (interpolate)
                computeTangents(m_config);
            m_tangents.copy_to(data.tangents, split.num_vertices, split.offset_vertices);
            if (m_config.swap_handedness) { SwapHandedness(data.tangents, split.num_vertices); }
        }
        else {
            memset(data.tangents, 0, split.num_vertices * sizeof(abcV4));
        }
    }

    if (data.uv0) {
        if (copy_uv0)
            m_uv0.copy_to(data.uv0, split.num_vertices, split.offset_vertices);
        else
            memset(data.uv0, 0, split.num_vertices * sizeof(abcV2));
    }

    if (data.uv1) {
        if (!m_uv1.empty())
            m_uv1.copy_to(data.uv1, split.num_vertices, split.offset_vertices);
        else
            memset(data.uv1, 0, split.num_vertices * sizeof(abcV2));
    }

    if (data.colors) {
        if (!m_colors.empty())
            m_colors.copy_to((abcC4*)data.colors, split.num_vertices, split.offset_vertices);
        else
            memset(data.colors, 0, split.num_vertices * sizeof(abcV4));
    }

    {
        abcV3 bbmin, bbmax;
        MinMax(bbmin, bbmax, data.points, split.num_vertices);
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
    if (!data.indices)
        return;

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

    // find color and uv1 params (Maya's extension attributes)
    auto geom_params = m_schema.getArbGeomParams();
    if (geom_params.valid()) {
        size_t num_geom_params = geom_params.getNumProperties();
        for (size_t i = 0; i < num_geom_params; ++i) {
            auto& header = geom_params.getPropertyHeader(i);
            if (header.getName() == "rgba" && AbcGeom::IC4fGeomParam::matches(header)) {
                m_colors_param.reset(new AbcGeom::IC4fGeomParam(geom_params, "rgba"));
            }
            else if (header.getName() == "uv1" && AbcGeom::IV2fGeomParam::matches(header)) {
                m_uv1_param.reset(new AbcGeom::IV2fGeomParam(geom_params, "uv1"));
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
    auto& refiner = topology.m_refiner;
    bool interpolate = m_config.interpolate_samples && !isConstant() && !hasVaryingTopology();

    topology_changed = m_varying_topology;
    topology.m_use_32bit_index_buffer = m_config.use_32bit_index_buffer;

    // topology
    if (!topology.m_counts || m_varying_topology) {
        m_schema.getFaceCountsProperty().get(topology.m_counts, ss);
        topology.m_triangulated_index_count = CalculateTriangulatedIndexCount(*topology.m_counts);
        topology_changed = true;
    }
    if (!topology.m_indices_orig || m_varying_topology) {
        m_schema.getFaceIndicesProperty().get(topology.m_indices_orig, ss);
        topology_changed = true;
    }

    // face sets
    if (topology_changed && !m_facesets.empty()) {
        sample->m_facesets.resize(m_facesets.size());
        for (size_t fi = 0; fi < m_facesets.size(); ++fi) {
            m_facesets[fi].get(sample->m_facesets[fi], ss);
        }
    }

    // points
    m_schema.getPositionsProperty().get(sample->m_points_orig, ss);
    if (interpolate) {
        m_schema.getPositionsProperty().get(sample->m_points_orig2, ss2);
    }
    else {
        sample->m_points_orig2.reset();
        sample->m_velocities_orig.reset();
        auto velocities_prop = m_schema.getVelocitiesProperty();
        if (velocities_prop.valid()) {
            velocities_prop.get(sample->m_velocities_orig, ss);
        }
    }

    // normals
    sample->m_normals_orig.reset();
    sample->m_normals_orig2.reset();
    auto normals_param = m_schema.getNormalsParam();
    if (!m_ignore_normals && normals_param.valid()) {
        if (normals_param.isConstant()) {
            if (!m_constant_normals.valid()) {
                DebugLog("  Read normals (constant)");
                normals_param.getIndexed(m_constant_normals, ss);
            }
            sample->m_normals_orig = m_constant_normals;
            sample->m_normals_orig2 = m_constant_normals;
        }
        else {
            DebugLog("  Read normals");
            normals_param.getIndexed(sample->m_normals_orig, ss);
            if (interpolate) {
                normals_param.getIndexed(sample->m_normals_orig2, ss2);
            }
        }
    }

    // uv0
    sample->m_uv0_orig.reset();
    auto uv0_param = m_schema.getUVsParam();
    if (!m_ignore_uvs && uv0_param.valid()) {
        if (uv0_param.isConstant()) {
            if (!m_constant_uv0.valid()) {
                DebugLog("  Read uv0 (constant)");
                uv0_param.getIndexed(m_constant_uv0, ss);
            }
            sample->m_uv0_orig = m_constant_uv0;
        }
        else {
            DebugLog("  Read uv0");
            uv0_param.getIndexed(sample->m_uv0_orig, ss);
        }
    }

    // uv1
    sample->m_uv1_orig.reset();
    if (m_uv1_param && m_uv1_param->valid()) {
        if (m_uv1_param->isConstant()) {
            if (!m_constant_uv1.valid()) {
                DebugLog("  Read uv1 (constant)");
                m_uv1_param->getIndexed(m_constant_uv1, ss);
            }
            sample->m_uv1_orig = m_constant_uv1;
        }
        else {
            DebugLog("  Read uv1");
            m_uv1_param->getIndexed(sample->m_uv1_orig, ss);
        }
    }

    // colors
    sample->m_colors_orig.reset();
    if (m_colors_param && m_colors_param->valid()) {
        if (m_colors_param->isConstant()) {
            if (!m_constant_colors.valid()) {
                DebugLog("  Read colors (constant)");
                m_colors_param->getIndexed(m_constant_colors, ss);
            }
            sample->m_colors_orig = m_constant_colors;
        }
        else {
            DebugLog("  Read colors");
            m_colors_param->getIndexed(sample->m_colors_orig, ss);
        }
    }

    auto bounds_param = m_schema.getSelfBoundsProperty();
    if (bounds_param)
        bounds_param.get(sample->m_bounds, ss);

    if (topology_changed) {
        // update topology!
        topology.onTopologyUpdate(m_config, *sample);
        sample->m_points.swap((RawVector<abcV3>&)refiner.new_points);
    }
    else {
        // make remapped vertex buffer
        {
            auto& remap = topology.m_remap_points;
            sample->m_points.resize_discard(remap.size());
            CopyWithIndices(sample->m_points.data(), sample->m_points_orig->get(), remap);
        }
        if (sample->m_normals_orig.valid()) {
            auto& remap = topology.m_remap_normals;
            sample->m_normals.resize_discard(remap.size());
            CopyWithIndices(sample->m_normals.data(), sample->m_normals_orig.getVals()->get(), remap);
        }
        if (sample->m_uv0_orig.valid()) {
            auto& remap = topology.m_remap_uv0;
            sample->m_uv0.resize_discard(remap.size());
            CopyWithIndices(sample->m_uv0.data(), sample->m_uv0_orig.getVals()->get(), remap);
        }
        if (sample->m_uv1_orig.valid()) {
            auto& remap = topology.m_remap_uv1;
            sample->m_uv1.resize_discard(remap.size());
            CopyWithIndices(sample->m_uv1.data(), sample->m_uv1_orig.getVals()->get(), remap);
        }
        if (sample->m_colors_orig.valid()) {
            auto& remap = topology.m_remap_colors;
            sample->m_colors.resize_discard(remap.size());
            CopyWithIndices(sample->m_colors.data(), sample->m_colors_orig.getVals()->get(), remap);
        }
    }
    if (interpolate) {
        {
            auto& remap = topology.m_remap_points;
            sample->m_points2.resize_discard(remap.size());
            CopyWithIndices(sample->m_points2.data(), sample->m_points_orig2->get(), remap);
        }
        if (sample->m_normals_orig2.valid()) {
            auto& remap = topology.m_remap_normals;
            sample->m_normals2.resize_discard(remap.size());
            CopyWithIndices(sample->m_normals2.data(), sample->m_normals_orig2.getVals()->get(), remap);
        }
    }


    // compute normals / tangents if needed
    if (!interpolate) {
        if (sample->computeNormalsRequired())
            sample->computeNormals(m_config);
        if (sample->computeTangentsRequired() && !sample->m_normals.empty() && !sample->m_uv0.empty())
            sample->computeTangents(m_config);
    }


    return sample;
}

void aiPolyMesh::getSummary(aiMeshSummary &summary) const
{
    DebugLog("aiPolyMesh::getSummary()");
    
    summary.topology_variance = static_cast<int>(m_schema.getTopologyVariance());
}
