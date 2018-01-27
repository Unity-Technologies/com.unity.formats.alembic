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
    m_indices_sp.reset();
    m_counts_sp.reset();
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
    refiner.counts = { m_counts_sp->get(), m_counts_sp->size() };
    refiner.indices = { m_indices_sp->get(), m_indices_sp->size() };
    refiner.points = { (float3*)sample.m_points_sp->get(), sample.m_points_sp->size() };

    if (sample.m_normals_sp.valid()) {
        IArray<abcV3> normals { sample.m_normals_sp.getVals()->get(), sample.m_normals_sp.getVals()->size() };
        if (sample.m_normals_sp.isIndexed()) {
            IArray<int> normal_indices { (int*)sample.m_normals_sp.getIndices()->get(), sample.m_normals_sp.getIndices()->size() };
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

    if (sample.m_uv0_sp.valid()) {
        IArray<abcV2> uv0 { sample.m_uv0_sp.getVals()->get(), sample.m_uv0_sp.getVals()->size() };
        if (sample.m_uv0_sp.isIndexed()) {
            IArray<int> uv0_indices{ (int*)sample.m_uv0_sp.getIndices()->get(), sample.m_uv0_sp.getIndices()->size() };
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

    if (sample.m_uv1_sp.valid()) {
        IArray<abcV2> uv1{ sample.m_uv1_sp.getVals()->get(), sample.m_uv1_sp.getVals()->size() };
        if (sample.m_uv1_sp.isIndexed()) {
            IArray<int> uv1_indices{ (int*)sample.m_uv1_sp.getIndices()->get(), sample.m_uv1_sp.getIndices()->size() };
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

    if (sample.m_colors_sp.valid()) {
        IArray<abcC4> colors{ sample.m_colors_sp.getVals()->get(), sample.m_colors_sp.getVals()->size() };
        if (sample.m_colors_sp.isIndexed()) {
            IArray<int> colors_indices{ (int*)sample.m_colors_sp.getIndices()->get(), sample.m_colors_sp.getIndices()->size() };
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

    m_freshly_read_topology_data = true;
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
        return m_normals_sp.valid();
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
    return m_uv0_sp.valid();
}

bool aiPolyMeshSample::hasUV1() const
{
    return m_uv1_sp.valid();
}

bool aiPolyMeshSample::hasColors() const
{
    return m_colors_sp.valid();
}

bool aiPolyMeshSample::computeNormalsRequired() const
{
    return (m_config.normals_mode == aiNormalsMode::AlwaysCompute ||
            (!m_normals_sp.valid() && m_config.normals_mode == aiNormalsMode::ComputeIfMissing));
}

bool aiPolyMeshSample::computeTangentsRequired() const
{
    return (m_config.tangents_mode != aiTangentsMode::None);
}

void aiPolyMeshSample::computeNormals(const aiConfig &config)
{
    DebugLog("%s: Compute smooth normals", getSchema()->getObject()->getFullName());

    const auto &indices = m_topology->m_refiner.new_indices_triangulated;
    m_normals.resize_zeroclear(m_points.size());
    GenerateNormals(m_normals.data(), m_points.data(), indices.data(), (int)m_points.size(), (int)indices.size() / 3);
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
                                  (!m_normals_sp.valid() && config.normals_mode == aiNormalsMode::ComputeIfMissing));
    
    if (compute_normals_required) {
        if (m_normals.empty() || topology_changed) {
            computeNormals(config);
            data_changed = true;
        }
    }
    else if(m_normals_sp.valid()) {
        auto& remap = m_topology->m_remap_normals;
        auto n = remap.size();
        m_normals.resize_discard(n);
        const auto *n1 = m_normals_sp.getVals()->get();
        for (size_t i = 0; i < n; ++i) {
            m_normals[i] = n1[remap[i]];
        }
    }
    else {
        m_normals.clear();
    }

    bool tangents_required = (m_uv0_sp.valid() && config.tangents_mode != aiTangentsMode::None);
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
        data_changed = true;

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

int aiPolyMeshSample::getSplitVertexCount(int split_index) const
{
    DebugLog("aiPolyMeshSample::getSplitVertexCount(split_index=%d)", split_index);
    
    return m_topology->getSplitVertexCount(split_index);
}

void aiPolyMeshSample::prepareSplits()
{
}

void aiPolyMeshSample::fillSplitVertices(int split_index, aiPolyMeshData &data)
{
    DebugLog("aiPolyMeshSample::fillVertexBuffer(split_index=%d)", split_index);
    
    auto& schema = *dynamic_cast<schema_t*>(getSchema());
    auto& summary = schema.getSummary();
    auto& splits = m_topology->m_refiner.splits;
    if (split_index < 0 || size_t(split_index) >= splits.size() || splits[split_index].num_vertices == 0)
        return;

    auto& refiner = m_topology->m_refiner;
    auto& split = refiner.splits[split_index];

    if (data.points) {
        if (summary.interpolate_points)
            Lerp(data.points, m_points.data(), m_points2.data(), split.num_vertices, split.offset_vertices, (float)m_current_time_offset);
        else
            m_points.copy_to(data.points, split.num_vertices, split.offset_vertices);
    }
    if (data.velocities) {
        if (summary.compute_velocities) {
            GenerateVelocities(data.velocities,
                m_points.data(), m_points2.data(), split.num_vertices, split.offset_vertices,
                (float)m_current_time_interval, m_config.vertex_motion_scale);
        }
        else if (!m_velocities.empty()) {
            m_velocities.copy_to(data.points, split.num_vertices, split.offset_vertices);
        }
    }

    if (data.normals) {
        if (summary.has_normals) {
            if (summary.interpolate_normals) {
                Lerp(data.normals, m_normals.data(), m_normals2.data(), split.num_vertices, split.offset_vertices, (float)m_current_time_offset);
            }
            else if (summary.compute_normals) {
                computeNormals(m_config);
                m_normals.copy_to(data.normals, split.num_vertices, split.offset_vertices);
            }
            else if (!m_normals.empty()) {
                m_normals.copy_to(data.normals, split.num_vertices, split.offset_vertices);
            }
        }
        else {
            memset(data.normals, 0, split.num_vertices * sizeof(abcV3));
        }
    }

    if (data.tangents)
    {
        if(summary.has_tangents) {
            if (summary.interpolate_normals)
                computeTangents(m_config);
            m_tangents.copy_to(data.tangents, split.num_vertices, split.offset_vertices);
        }
        else {
            memset(data.tangents, 0, split.num_vertices * sizeof(abcV4));
        }
    }

    if (data.uv0) {
        if (!m_uv0.empty())
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
        data.center = (bbmin + bbmax) * 0.5f;
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
    // find color and uv1 params (Maya's extension attributes)
    auto geom_params = m_schema.getArbGeomParams();
    if (geom_params.valid()) {
        size_t num_geom_params = geom_params.getNumProperties();
        for (size_t i = 0; i < num_geom_params; ++i) {
            auto& header = geom_params.getPropertyHeader(i);
            if (header.getName() == "rgba" && AbcGeom::IC4fGeomParam::matches(header)) {
                // colors
                m_colors_param.reset(new AbcGeom::IC4fGeomParam(geom_params, "rgba"));
            }
            else if (header.getName() == "uv1" && AbcGeom::IV2fGeomParam::matches(header)) {
                // uv1
                m_uv1_param.reset(new AbcGeom::IV2fGeomParam(geom_params, "uv1"));
            }
        }
    }

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

    updateSummary();

    DebugLog("aiPolyMesh::aiPolyMesh(constant=%s, varyingTopology=%s)",
             (m_constant ? "true" : "false"),
             (m_varying_topology ? "true" : "false"));
}

void aiPolyMesh::updateSummary()
{
    m_constant = m_schema.isConstant();
    m_summary.topology_variance = (aiTopologyVariance)m_schema.getTopologyVariance();
    m_varying_topology = (m_schema.getTopologyVariance() == AbcGeom::kHeterogeneousTopology);
    auto& summary = m_summary;

    // reset
    summary = {};

    // points
    auto points = m_schema.getPositionsProperty();
    if (points.valid()) {
        summary.constant_points = points.isConstant();
        if (!summary.constant_points)
            m_constant = false;
    }

    // normals
    auto normals = m_schema.getNormalsParam();
    if (normals.valid() && normals.getScope() != AbcGeom::kUnknownScope) {
        summary.has_normals_prop = true;
        summary.has_normals = true;
        summary.constant_normals = normals.isConstant();
        if (!summary.constant_normals)
            m_constant = false;
    }

    // uv0
    auto uvs = m_schema.getUVsParam();
    if (uvs.valid() && uvs.getScope() != AbcGeom::kUnknownScope) {
        summary.has_uv0_prop = true;
        summary.has_uv0 = true;
        summary.constant_uv0 = uvs.isConstant();
        if (!summary.constant_uv0)
            m_constant = false;
    }

    // uv1
    if (m_uv1_param && m_uv1_param->valid()) {
        summary.has_uv1_prop = true;
        summary.has_uv1 = true;
        summary.constant_uv1 = m_uv1_param->isConstant();
        if (!summary.constant_uv1)
            m_constant = false;
    }

    // colors
    if (m_colors_param && m_colors_param->valid()) {
        summary.has_colors_prop = true;
        summary.has_colors = true;
        summary.constant_colors = m_colors_param->isConstant();
        if (!summary.constant_colors)
            m_constant = false;
    }


    bool interpolate = m_config.interpolate_samples && !isConstant() && !hasVaryingTopology();
    summary.interpolate_points = interpolate;

    // velocities
    if (interpolate) {
        summary.has_velocities = true;
        summary.compute_velocities = true;
    }
    else {
        auto velocities = m_schema.getVelocitiesProperty();
        if (velocities.valid()) {
            summary.has_velocities_prop = true;
            summary.has_velocities = true;
            summary.constant_velocities = velocities.isConstant();
        }
    }

    // normals - interpolate or compute?
    if (summary.has_normals) {
        summary.interpolate_normals = interpolate;
    }
    else {
        summary.compute_normals =
            m_config.normals_mode == aiNormalsMode::AlwaysCompute ||
            (!summary.has_normals && m_config.normals_mode == aiNormalsMode::ComputeIfMissing);
        if (summary.compute_normals) {
            summary.has_normals = true;
            summary.constant_normals = summary.constant_points;
        }
    }

    // tangents
    if (m_config.tangents_mode == aiTangentsMode::Smooth && summary.has_normals && summary.has_uv0) {
        summary.has_tangents = true;
        if (summary.constant_points && summary.constant_normals && summary.constant_uv0) {
            summary.constant_tangents = true;
        }
    }
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
    auto& summary = m_summary;

    topology_changed = m_varying_topology;
    topology.m_use_32bit_index_buffer = m_config.use_32bit_index_buffer;

    // topology
    if (!topology.m_counts_sp || m_varying_topology) {
        m_schema.getFaceCountsProperty().get(topology.m_counts_sp, ss);
        topology.m_triangulated_index_count = CalculateTriangulatedIndexCount(*topology.m_counts_sp);
        topology_changed = true;
    }
    if (!topology.m_indices_sp || m_varying_topology) {
        m_schema.getFaceIndicesProperty().get(topology.m_indices_sp, ss);
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
    m_schema.getPositionsProperty().get(sample->m_points_sp, ss);
    if (summary.interpolate_points) {
        m_schema.getPositionsProperty().get(sample->m_points_sp2, ss2);
    }
    else {
        sample->m_points_sp2.reset();
        sample->m_velocities_sp.reset();
        if (summary.has_velocities_prop) {
            m_schema.getVelocitiesProperty().get(sample->m_velocities_sp, ss);
        }
    }

    // normals
    if (m_constant_normals.empty() && summary.has_normals_prop && !summary.compute_normals) {
        auto normals_param = m_schema.getNormalsParam();
        normals_param.getIndexed(sample->m_normals_sp, ss);
        if (summary.interpolate_normals) {
            normals_param.getIndexed(sample->m_normals_sp2, ss2);
        }
    }

    // uv0
    if (m_constant_uv0.empty() && summary.has_uv0_prop) {
        m_schema.getUVsParam().getIndexed(sample->m_uv0_sp, ss);
    }

    // uv1
    if (m_constant_uv1.empty() && summary.has_uv1_prop) {
        m_uv1_param->getIndexed(sample->m_uv1_sp, ss);
    }

    // colors
    if (m_constant_colors.empty() && summary.has_colors_prop) {
        m_colors_param->getIndexed(sample->m_colors_sp, ss);
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
            CopyWithIndices(sample->m_points.data(), sample->m_points_sp->get(), remap);
        }
        if (!summary.compute_velocities && sample->m_velocities_sp && sample->m_velocities_sp->valid()) {
            auto& remap = topology.m_remap_points;
            sample->m_velocities.resize_discard(remap.size());
            CopyWithIndices(sample->m_velocities.data(), sample->m_velocities_sp->get(), remap);
        }
        if (!summary.compute_normals && sample->m_normals_sp.valid()) {
            auto& remap = topology.m_remap_normals;
            sample->m_normals.resize_discard(remap.size());
            CopyWithIndices(sample->m_normals.data(), sample->m_normals_sp.getVals()->get(), remap);
        }
        if (sample->m_uv0_sp.valid()) {
            auto& remap = topology.m_remap_uv0;
            sample->m_uv0.resize_discard(remap.size());
            CopyWithIndices(sample->m_uv0.data(), sample->m_uv0_sp.getVals()->get(), remap);
        }
        if (sample->m_uv1_sp.valid()) {
            auto& remap = topology.m_remap_uv1;
            sample->m_uv1.resize_discard(remap.size());
            CopyWithIndices(sample->m_uv1.data(), sample->m_uv1_sp.getVals()->get(), remap);
        }
        if (sample->m_colors_sp.valid()) {
            auto& remap = topology.m_remap_colors;
            sample->m_colors.resize_discard(remap.size());
            CopyWithIndices(sample->m_colors.data(), sample->m_colors_sp.getVals()->get(), remap);
        }
    }

    if (summary.interpolate_points) {
        auto& remap = topology.m_remap_points;
        sample->m_points2.resize_discard(remap.size());
        CopyWithIndices(sample->m_points2.data(), sample->m_points_sp2->get(), remap);
    }
    if (summary.interpolate_normals) {
        auto& remap = topology.m_remap_normals;
        sample->m_normals2.resize_discard(remap.size());
        CopyWithIndices(sample->m_normals2.data(), sample->m_normals_sp2.getVals()->get(), remap);
    }

    // compute normals / tangents
    // if interpolation is enabled, this will be done in fillVertexBuffer()
    if (!summary.interpolate_normals) {
        if (sample->computeNormalsRequired())
            sample->computeNormals(m_config);
        if (sample->computeTangentsRequired() && !sample->m_normals.empty() && !sample->m_uv0.empty())
            sample->computeTangents(m_config);
    }

    if (m_config.swap_handedness) {
        SwapHandedness(sample->m_points.data(), (int)sample->m_points.size());
        SwapHandedness(sample->m_points2.data(), (int)sample->m_points2.size());
        SwapHandedness(sample->m_velocities.data(), (int)sample->m_velocities.size());
        SwapHandedness(sample->m_normals.data(), (int)sample->m_normals.size());
        SwapHandedness(sample->m_normals2.data(), (int)sample->m_normals2.size());
        SwapHandedness(sample->m_tangents.data(), (int)sample->m_tangents.size());
    }

    return sample;
}

const aiMeshSummaryInternal & aiPolyMesh::getSummary() const
{
    return m_summary;
}

void aiPolyMesh::getSummary(aiMeshSummary &summary) const
{
    DebugLog("aiPolyMesh::getSummary()");
    summary = m_summary;
}
