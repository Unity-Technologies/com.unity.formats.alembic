#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiPolyMesh.h"
#include "../Foundation/aiMisc.h"
#include "../Foundation/aiMath.h"


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

template<class T>
inline void Remap(RawVector<T>& dst, const T *src, const RawVector<int>& indices)
{
    dst.resize_discard(indices.size());
    CopyWithIndices(dst.data(), src, indices);
}

template<class T>
inline void Lerp(RawVector<T>& dst, const RawVector<T>& src1, const RawVector<T>& src2, float w)
{
    if (src1.size() != src2.size()) {
        DebugError("something is wrong!!");
        return;
    }
    dst.resize_discard(src1.size());
    Lerp(dst.data(), src1.data(), src2.data(), (int)src1.size(), w);
}


aiMeshTopology::aiMeshTopology()
{
}

void aiMeshTopology::clear()
{
    DebugLog("Topology::clear()");
    m_indices_sp.reset();
    m_counts_sp.reset();
    m_faceset_sps.clear();

    m_refiner.clear();
    m_material_ids.clear();
    m_vertex_count = 0;
    m_index_count = 0;
}


int aiMeshTopology::getSplitCount() const
{
    return (int)m_refiner.splits.size();
}
int aiMeshTopology::getVertexCount() const
{
    return m_vertex_count;
}
int aiMeshTopology::getIndexCount() const
{
    return m_index_count;
}

int aiMeshTopology::getSplitVertexCount(int split_index) const
{
    return (int)m_refiner.splits[split_index].vertex_count;
}

int aiMeshTopology::getSubmeshCount() const
{
    return (int)m_refiner.submeshes.size();
}

int aiMeshTopology::getSubmeshCount(int split_index) const
{
    return (int)m_refiner.splits[split_index].submesh_count;
}

aiPolyMeshSample::aiPolyMeshSample(aiPolyMesh *schema, TopologyPtr topo)
    : super(schema)
    , m_topology(topo)
{
}

aiPolyMeshSample::~aiPolyMeshSample()
{
    waitAsync();
}

void aiPolyMeshSample::reset()
{
    m_points_sp.reset(); m_points_sp2.reset();
    m_velocities_sp.reset();
    m_normals_sp.reset(); m_normals_sp2.reset();
    m_uv0_sp.reset(); m_uv1_sp.reset();
    m_colors_sp.reset();

    m_points_ref.reset();
    m_velocities_ref.reset();
    m_uv0_ref.reset();
    m_uv1_ref.reset();
    m_normals_ref.reset();
    m_tangents_ref.reset();
    m_colors_ref.reset();
}

void aiPolyMeshSample::getSummary(aiMeshSampleSummary &dst) const
{
    dst.visibility = visibility;
    dst.split_count   = m_topology->getSplitCount();
    dst.submesh_count = m_topology->getSubmeshCount();
    dst.vertex_count  = m_topology->getVertexCount();
    dst.index_count   = m_topology->getIndexCount();
    dst.topology_changed = m_topology_changed;
}

void aiPolyMeshSample::getSplitSummaries(aiMeshSplitSummary  *dst) const
{
    auto& refiner = m_topology->m_refiner;
    for (int i = 0; i < (int)refiner.splits.size(); ++i) {
        auto& src = refiner.splits[i];
        dst[i].submesh_count  = src.submesh_count;
        dst[i].submesh_offset = src.submesh_offset;
        dst[i].vertex_count   = src.vertex_count;
        dst[i].vertex_offset  = src.vertex_offset;
        dst[i].index_count    = src.index_count;
        dst[i].index_offset   = src.index_offset;
    }
}

void aiPolyMeshSample::getSubmeshSummaries(aiSubmeshSummary *dst) const
{
    auto& refiner = m_topology->m_refiner;
    for (int i = 0; i < (int)refiner.submeshes.size(); ++i) {
        auto& src = refiner.submeshes[i];
        dst[i].split_index   = src.split_index;
        dst[i].submesh_index = src.submesh_index;
        dst[i].index_count   = src.index_count;
    }
}

void aiPolyMeshSample::fillSplitVertices(int split_index, aiPolyMeshData &data) const
{
    auto& schema = *dynamic_cast<schema_t*>(getSchema());
    auto& summary = schema.getSummary();
    auto& splits = m_topology->m_refiner.splits;
    if (split_index < 0 || size_t(split_index) >= splits.size() || splits[split_index].vertex_count == 0)
        return;

    auto& refiner = m_topology->m_refiner;
    auto& split = refiner.splits[split_index];

    if (data.points) {
        m_points_ref.copy_to(data.points, split.vertex_count, split.vertex_offset);

        // bounds
        abcV3 bbmin, bbmax;
        MinMax(bbmin, bbmax, data.points, split.vertex_count);
        data.center = (bbmin + bbmax) * 0.5f;
        data.extents = bbmax - bbmin;
    }
    if (data.velocities) {
        // velocity can be empty even if summary.has_velocities is true (compute is enabled & first frame)
        if (summary.has_velocities && !m_velocities_ref.empty()) {
            m_velocities_ref.copy_to(data.velocities, split.vertex_count, split.vertex_offset);
        }
        else {
            memset(data.velocities, 0, split.vertex_count * sizeof(abcV3));
        }
    }

    if (data.normals) {
        if (summary.has_normals) {
            m_normals_ref.copy_to(data.normals, split.vertex_count, split.vertex_offset);
        }
        else {
            memset(data.normals, 0, split.vertex_count * sizeof(abcV3));
        }
    }

    if (data.tangents) {
        if(summary.has_tangents) {
            m_tangents_ref.copy_to(data.tangents, split.vertex_count, split.vertex_offset);
        }
        else {
            memset(data.tangents, 0, split.vertex_count * sizeof(abcV4));
        }
    }

    if (data.uv0) {
        if (!m_uv0_ref.empty())
            m_uv0_ref.copy_to(data.uv0, split.vertex_count, split.vertex_offset);
        else
            memset(data.uv0, 0, split.vertex_count * sizeof(abcV2));
    }

    if (data.uv1) {
        if (!m_uv1_ref.empty())
            m_uv1_ref.copy_to(data.uv1, split.vertex_count, split.vertex_offset);
        else
            memset(data.uv1, 0, split.vertex_count * sizeof(abcV2));
    }

    if (data.colors) {
        if (!m_colors_ref.empty())
            m_colors_ref.copy_to((abcC4*)data.colors, split.vertex_count, split.vertex_offset);
        else
            memset(data.colors, 0, split.vertex_count * sizeof(abcV4));
    }
}

void aiPolyMeshSample::fillSubmeshIndices(int submesh_index, aiSubmeshData &data) const
{
    if (!data.indices)
        return;

    auto& refiner = m_topology->m_refiner;
    auto& submesh = refiner.submeshes[submesh_index];
    refiner.new_indices_submeshes.copy_to(data.indices, submesh.index_count, submesh.index_offset);
}

void aiPolyMeshSample::fillVertexBuffer(aiPolyMeshData * vbs, aiSubmeshData * ibs)
{
    auto body = [this, vbs, ibs]() {
        auto& refiner = m_topology->m_refiner;
        for (int spi = 0; spi < (int)refiner.splits.size(); ++spi)
            fillSplitVertices(spi, vbs[spi]);
        for (int smi = 0; smi < (int)refiner.submeshes.size(); ++smi)
            fillSubmeshIndices(smi, ibs[smi]);
    };

    if (m_force_sync || !getConfig().async_load)
        body();
    else
        m_async_copy = std::async(std::launch::async, body);
}

void aiPolyMeshSample::waitAsync()
{
    if (m_async_copy.valid())
        m_async_copy.wait();
    m_force_sync = false;
}



aiPolyMesh::aiPolyMesh(aiObject *parent, const abcObject &abc)
    : super(parent, abc)
{
    // find color and uv1 params (Maya's extension)
    auto geom_params = m_schema.getArbGeomParams();
    if (geom_params.valid()) {
        size_t num_geom_params = geom_params.getNumProperties();
        for (size_t i = 0; i < num_geom_params; ++i) {
            auto& header = geom_params.getPropertyHeader(i);
            if (header.getName() == "rgba" && AbcGeom::IC4fGeomParam::matches(header)) {
                // colors
                m_colors_param = AbcGeom::IC4fGeomParam(geom_params, "rgba");
            }
            else if (header.getName() == "uv1" && AbcGeom::IV2fGeomParam::matches(header)) {
                // uv1
                m_uv1_param = AbcGeom::IV2fGeomParam(geom_params, "uv1");
            }
        }
    }

    // find face set schema in children
    size_t num_children = getAbcObject().getNumChildren();
    for (size_t i = 0; i < num_children; ++i) {
        auto child = getAbcObject().getChild(i);
        if (child.valid() && AbcGeom::IFaceSetSchema::matches(child.getMetaData())) {
            auto so = Abc::ISchemaObject<AbcGeom::IFaceSetSchema>(child, Abc::kWrapExisting);
            m_facesets.push_back(so.getSchema());
        }
    }

    updateSummary();
}

aiPolyMesh::~aiPolyMesh()
{
    waitAsync();
}

void aiPolyMesh::updateSummary()
{
    m_summary.topology_variance = (aiTopologyVariance)m_schema.getTopologyVariance();
    m_varying_topology = (m_schema.getTopologyVariance() == AbcGeom::kHeterogeneousTopology);
    auto& summary = m_summary;
    auto& config = getConfig();

    summary = {};
    m_constant = m_schema.isConstant();

    // m_schema.isConstant() doesn't consider custom properties. check them
    if (m_visibility_prop.valid() && !m_visibility_prop.isConstant()) {
        m_constant = false;
    }

    // points
    auto points = m_schema.getPositionsProperty();
    if (points.valid()) {
        summary.constant_points = points.isConstant();
        if (!summary.constant_points)
            m_constant = false;
    }

    // normals
    auto normals = m_schema.getNormalsParam();
    if (normals.valid() && normals.getNumSamples() > 0 && normals.getScope() != AbcGeom::kUnknownScope) {
        summary.has_normals_prop = true;
        summary.has_normals = true;
        summary.constant_normals = normals.isConstant();
        if (!summary.constant_normals)
            m_constant = false;
    }

    // uv0
    auto uvs = m_schema.getUVsParam();
    if (uvs.valid() && uvs.getNumSamples() > 0 && uvs.getScope() != AbcGeom::kUnknownScope) {
        summary.has_uv0_prop = true;
        summary.has_uv0 = true;
        summary.constant_uv0 = uvs.isConstant();
        if (!summary.constant_uv0)
            m_constant = false;
    }

    // uv1
    if (m_uv1_param.valid() && m_uv1_param.getNumSamples() > 0) {
        summary.has_uv1_prop = true;
        summary.has_uv1 = true;
        summary.constant_uv1 = m_uv1_param.isConstant();
        if (!summary.constant_uv1)
            m_constant = false;
    }

    // colors
    if (m_colors_param.valid() && m_colors_param.getNumSamples() > 0) {
        summary.has_colors_prop = true;
        summary.has_colors = true;
        summary.constant_colors = m_colors_param.isConstant();
        if (!summary.constant_colors)
            m_constant = false;
    }


    bool interpolate = config.interpolate_samples && !m_constant && !m_varying_topology;
    summary.interpolate_points = interpolate && !summary.constant_points;

    // velocities
    if (interpolate) {
        summary.has_velocities = true;
        summary.compute_velocities = true;
    }
    else {
        auto velocities = m_schema.getVelocitiesProperty();
        if (velocities.valid() && velocities.getNumSamples() > 0) {
            summary.has_velocities_prop = true;
            summary.has_velocities = true;
            summary.constant_velocities = velocities.isConstant();
        }
    }

    // normals - interpolate or compute?
    if (!summary.constant_normals) {
        if (summary.has_normals && config.normals_mode != aiNormalsMode::AlwaysCompute) {
            summary.interpolate_normals = interpolate;
        }
        else {
            summary.compute_normals =
                config.normals_mode == aiNormalsMode::AlwaysCompute ||
                (!summary.has_normals && config.normals_mode == aiNormalsMode::ComputeIfMissing);
            if (summary.compute_normals) {
                summary.has_normals = true;
                summary.constant_normals = summary.constant_points;
            }
        }
    }

    // tangents
    if (config.tangents_mode == aiTangentsMode::Compute && summary.has_normals && summary.has_uv0) {
        summary.has_tangents = true;
        summary.compute_tangents = true;
        if (summary.constant_points && summary.constant_normals && summary.constant_uv0) {
            summary.constant_tangents = true;
        }
    }

    if (interpolate) {
        if (summary.has_uv0_prop && !summary.constant_uv0)
            summary.interpolate_uv0 = true;
        if (summary.has_uv1_prop && !summary.constant_uv1)
            summary.interpolate_uv1 = true;
        if (summary.has_colors_prop && !summary.constant_colors)
            summary.interpolate_colors = true;
    }
}

const aiMeshSummaryInternal& aiPolyMesh::getSummary() const
{
    return m_summary;
}

aiPolyMesh::Sample* aiPolyMesh::newSample()
{
    if (!m_varying_topology) {
        if (!m_shared_topology)
            m_shared_topology.reset(new aiMeshTopology());
        return new Sample(this, m_shared_topology);
    }
    else {
        return new Sample(this, TopologyPtr(new aiMeshTopology()));
    }
}

void aiPolyMesh::updateSample(const abcSampleSelector& ss)
{
    m_async_load.reset();

    super::updateSample(ss);
    if (m_async_load.ready())
        getContext()->queueAsync(m_async_load);
}

void aiPolyMesh::readSample(Sample& sample, uint64_t idx)
{
    m_force_update_local = m_force_update;

    auto body = [this, &sample, idx]() {
        readSampleBody(sample, idx);
    };

    if (m_force_sync || !getConfig().async_load)
        body();
    else
        m_async_load.m_read = body;
}

void aiPolyMesh::cookSample(Sample& sample)
{
    auto body = [this, &sample]() {
        cookSampleBody(sample);
    };

    if (m_force_sync || !getConfig().async_load)
        body();
    else
        m_async_load.m_cook = body;
}

void aiPolyMesh::waitAsync()
{
    m_async_load.wait();
}

void aiPolyMesh::readSampleBody(Sample& sample, uint64_t idx)
{
    auto ss = aiIndexToSampleSelector(idx);
    auto ss2 = aiIndexToSampleSelector(idx + 1);

    readVisibility(sample, ss);
    
    auto& topology = *sample.m_topology;
    auto& refiner = topology.m_refiner;
    auto& summary = m_summary;

    bool topology_changed = m_varying_topology || m_force_update_local;

    if (topology_changed)
        topology.clear();

    // topology
    if (!topology.m_counts_sp || topology_changed) {
        m_schema.getFaceCountsProperty().get(topology.m_counts_sp, ss);
        topology_changed = true;
    }
    if (!topology.m_indices_sp || topology_changed) {
        m_schema.getFaceIndicesProperty().get(topology.m_indices_sp, ss);
        topology_changed = true;
    }

    // face sets
    if (!m_facesets.empty() && topology_changed) {
        topology.m_faceset_sps.resize(m_facesets.size());
        for (size_t fi = 0; fi < m_facesets.size(); ++fi) {
            m_facesets[fi].get(topology.m_faceset_sps[fi], ss);
        }
    }

    // points
    if (m_constant_points.empty()) {
        auto param = m_schema.getPositionsProperty();
        param.get(sample.m_points_sp, ss);
        if (summary.interpolate_points) {
            param.get(sample.m_points_sp2, ss2);
        }
        else {
            if (summary.has_velocities_prop) {
                m_schema.getVelocitiesProperty().get(sample.m_velocities_sp, ss);
            }
        }
    }

    // normals
    if (m_constant_normals.empty() && summary.has_normals_prop && !summary.compute_normals) {
        auto param = m_schema.getNormalsParam();
        param.getIndexed(sample.m_normals_sp, ss);
        if (summary.interpolate_normals) {
            param.getIndexed(sample.m_normals_sp2, ss2);
        }
    }

    // uv0
    if (m_constant_uv0.empty() && summary.has_uv0_prop) {
        auto param = m_schema.getUVsParam();
        param.getIndexed(sample.m_uv0_sp, ss);
        if (summary.interpolate_uv0) {
            param.getIndexed(sample.m_uv0_sp2, ss2);
        }
    }

    // uv1
    if (m_constant_uv1.empty() && summary.has_uv1_prop) {
        m_uv1_param.getIndexed(sample.m_uv1_sp, ss);
        if (summary.interpolate_uv1) {
            m_uv1_param.getIndexed(sample.m_uv1_sp2, ss2);
        }
    }

    // colors
    if (m_constant_colors.empty() && summary.has_colors_prop) {
        m_colors_param.getIndexed(sample.m_colors_sp, ss);
        if (summary.interpolate_colors) {
            m_colors_param.getIndexed(sample.m_colors_sp2, ss2);
        }
    }

    auto bounds_param = m_schema.getSelfBoundsProperty();
    if (bounds_param)
        bounds_param.get(sample.m_bounds, ss);

    sample.m_topology_changed = topology_changed;
}

void aiPolyMesh::cookSampleBody(Sample& sample)
{
    auto& topology = *sample.m_topology;
    auto& refiner = topology.m_refiner;
    auto& config = getConfig();
    auto& summary = getSummary();

    // interpolation can't work with varying topology
    if (m_varying_topology && !m_sample_index_changed)
        return;

    if (sample.m_topology_changed) {
        onTopologyChange(sample);
    }
    else if(m_sample_index_changed) {
        onTopologyDetermined();

        // make remapped vertex buffer
        if (!m_constant_points.empty()) {
            sample.m_points_ref = m_constant_points;
        }
        else {
            Remap(sample.m_points, sample.m_points_sp->get(), topology.m_remap_points);
            if (config.swap_handedness)
                SwapHandedness(sample.m_points.data(), (int)sample.m_points.size());
            if (config.scale_factor != 1.0f)
                ApplyScale(sample.m_points.data(), (int)sample.m_points.size(), config.scale_factor);
            sample.m_points_ref = sample.m_points;
        }

        if (!m_constant_normals.empty()) {
            sample.m_normals_ref = m_constant_normals;
        }
        else if (!summary.compute_normals && summary.has_normals_prop) {
            Remap(sample.m_normals, sample.m_normals_sp.getVals()->get(), topology.m_remap_normals);
            if (config.swap_handedness)
                SwapHandedness(sample.m_normals.data(), (int)sample.m_normals.size());
            sample.m_normals_ref = sample.m_normals;
        }

        if (!m_constant_tangents.empty()) {
            sample.m_tangents_ref = m_constant_tangents;
        }

        if (!m_constant_uv0.empty()) {
            sample.m_uv0_ref = m_constant_uv0;
        }
        else if (summary.has_uv0_prop) {
            Remap(sample.m_uv0, sample.m_uv0_sp.getVals()->get(), topology.m_remap_uv0);
            sample.m_uv0_ref = sample.m_uv0;
        }

        if (!m_constant_uv1.empty()) {
            sample.m_uv1_ref = m_constant_uv1;
        }
        else if (summary.has_uv1_prop) {
            Remap(sample.m_uv1, sample.m_uv1_sp.getVals()->get(), topology.m_remap_uv1);
            sample.m_uv1_ref = sample.m_uv1;
        }

        if (!m_constant_colors.empty()) {
            sample.m_colors_ref = m_constant_colors;
        }
        else if (summary.has_colors_prop) {
            Remap(sample.m_colors, sample.m_colors_sp.getVals()->get(), topology.m_remap_colors);
            sample.m_colors_ref = sample.m_colors;
        }
    }
    else {
        onTopologyDetermined();
    }

    if (m_sample_index_changed) {
        // both in the case of topology changed or sample index changed

        if (summary.interpolate_points) {
            Remap(sample.m_points2, sample.m_points_sp2->get(), topology.m_remap_points);
            if (config.swap_handedness)
                SwapHandedness(sample.m_points2.data(), (int)sample.m_points2.size());
            if (config.scale_factor != 1.0f)
                ApplyScale(sample.m_points2.data(), (int)sample.m_points2.size(), config.scale_factor);
        }

        if (summary.interpolate_normals) {
            Remap(sample.m_normals2, sample.m_normals_sp2.getVals()->get(), topology.m_remap_normals);
            if (config.swap_handedness)
                SwapHandedness(sample.m_normals2.data(), (int)sample.m_normals2.size());
        }

        if (summary.interpolate_uv0) {
            Remap(sample.m_uv02, sample.m_uv0_sp2.getVals()->get(), topology.m_remap_uv0);
        }

        if (summary.interpolate_uv1) {
            Remap(sample.m_uv12, sample.m_uv1_sp2.getVals()->get(), topology.m_remap_uv1);
        }

        if (summary.interpolate_colors) {
            Remap(sample.m_colors2, sample.m_colors_sp2.getVals()->get(), topology.m_remap_colors);
        }

        if (!m_constant_velocities.empty()) {
            sample.m_velocities_ref = m_constant_velocities;
        }
        else if (!summary.compute_velocities && summary.has_velocities_prop) {
            auto& dst = summary.constant_velocities ? m_constant_velocities : sample.m_velocities;
            Remap(dst, sample.m_velocities_sp->get(), topology.m_remap_points);
            if (config.swap_handedness)
                SwapHandedness(dst.data(), (int)dst.size());
            if (config.scale_factor != 1.0f)
                ApplyScale(dst.data(), (int)dst.size(), config.scale_factor);
            sample.m_velocities_ref = dst;
        }
    }

    // interpolate or compute data

    // points
    if (summary.interpolate_points) {
        if (summary.compute_velocities)
            sample.m_points_int.swap(sample.m_points_prev);

        Lerp(sample.m_points_int, sample.m_points, sample.m_points2, m_current_time_offset);
        sample.m_points_ref = sample.m_points_int;

        if (summary.compute_velocities) {
            sample.m_velocities.resize_discard(sample.m_points.size());
            if (sample.m_points_int.size() == sample.m_points_prev.size()) {
                GenerateVelocities(sample.m_velocities.data(), sample.m_points_int.data(), sample.m_points_prev.data(),
                    (int)sample.m_points_int.size(), config.vertex_motion_scale);
            }
            else {
                sample.m_velocities.zeroclear();
            }
            sample.m_velocities_ref = sample.m_velocities;
        }
    }

    // normals
    if (!m_constant_normals.empty()) {
        // do nothing
    }
    else if(summary.interpolate_normals) {
        Lerp(sample.m_normals_int, sample.m_normals, sample.m_normals2, (float)m_current_time_offset);
        Normalize(sample.m_normals_int.data(), (int)sample.m_normals.size());
        sample.m_normals_ref = sample.m_normals_int;
    }
    else if (summary.compute_normals && (m_sample_index_changed || summary.interpolate_points)) {
        if (sample.m_points_ref.empty()) {
            DebugError("something is wrong!!");
        }
        const auto &indices = topology.m_refiner.new_indices_triangulated;
        sample.m_normals.resize_discard(sample.m_points_ref.size());
        GenerateNormals(sample.m_normals.data(), sample.m_points_ref.data(), indices.data(),
            (int)sample.m_points_ref.size(), (int)indices.size() / 3);
        sample.m_normals_ref = sample.m_normals;
    }

    // tangents
    if (!m_constant_tangents.empty()) {
        // do nothing
    }
    else if (summary.compute_tangents && (m_sample_index_changed || summary.interpolate_points || summary.interpolate_normals)) {
        if (sample.m_points_ref.empty() || sample.m_uv0_ref.empty() || sample.m_normals_ref.empty()) {
            DebugError("something is wrong!!");
        }
        const auto &indices = topology.m_refiner.new_indices_triangulated;
        sample.m_tangents.resize_discard(sample.m_points_ref.size());
        GenerateTangents(sample.m_tangents.data(), sample.m_points_ref.data(), sample.m_uv0_ref.data(), sample.m_normals_ref.data(),
            indices.data(), (int)sample.m_points_ref.size(), (int)indices.size() / 3);
        sample.m_tangents_ref = sample.m_tangents;
    }

    // uv0
    if (summary.interpolate_uv0) {
        Lerp(sample.m_uv0_int, sample.m_uv0, sample.m_uv02, m_current_time_offset);
        sample.m_uv0_ref = sample.m_uv0_int;
    }

    // uv1
    if (summary.interpolate_uv1) {
        Lerp(sample.m_uv1_int, sample.m_uv1, sample.m_uv12, m_current_time_offset);
        sample.m_uv1_ref = sample.m_uv1_int;
    }

    // colors
    if (summary.interpolate_colors) {
        Lerp(sample.m_colors_int, sample.m_colors, sample.m_colors2, m_current_time_offset);
        sample.m_colors_ref = sample.m_colors_int;
    }
}

void aiPolyMesh::onTopologyChange(aiPolyMeshSample & sample)
{
    auto& summary = m_summary;
    auto& topology = *sample.m_topology;
    auto& refiner = topology.m_refiner;
    auto& config = getConfig();

    refiner.clear();
    refiner.split_unit = config.split_unit;
    refiner.counts = { topology.m_counts_sp->get(), topology.m_counts_sp->size() };
    refiner.indices = { topology.m_indices_sp->get(), topology.m_indices_sp->size() };
    refiner.points = { (float3*)sample.m_points_sp->get(), sample.m_points_sp->size() };

    bool weld_normals = false;
    bool weld_uv0 = false;
    bool weld_uv1 = false;
    bool weld_colors = false;

    if (sample.m_normals_sp.valid() && !summary.compute_normals) {
        IArray<abcV3> src{ sample.m_normals_sp.getVals()->get(), sample.m_normals_sp.getVals()->size() };
        auto& dst = summary.constant_normals ? m_constant_normals : sample.m_normals;

        weld_normals = true;
        if (sample.m_normals_sp.isIndexed()) {
            IArray<int> indices{ (int*)sample.m_normals_sp.getIndices()->get(), sample.m_normals_sp.getIndices()->size() };
            refiner.addIndexedAttribute<abcV3>(src, indices, dst, topology.m_remap_normals);
        }
        else if (src.size() == refiner.indices.size()) {
            refiner.addExpandedAttribute<abcV3>(src, dst, topology.m_remap_normals);
        }
        else if (src.size() == refiner.points.size()) {
            refiner.addIndexedAttribute<abcV3>(src, refiner.indices, dst, topology.m_remap_normals);
        }
        else {
            DebugLog("Invalid attribute");
            weld_normals = false;
        }
    }

    if (sample.m_uv0_sp.valid()) {
        IArray<abcV2> src{ sample.m_uv0_sp.getVals()->get(), sample.m_uv0_sp.getVals()->size() };
        auto& dst = summary.constant_uv0 ? m_constant_uv0 : sample.m_uv0;

        weld_uv0 = true;
        if (sample.m_uv0_sp.isIndexed()) {
            IArray<int> indices{ (int*)sample.m_uv0_sp.getIndices()->get(), sample.m_uv0_sp.getIndices()->size() };
            refiner.addIndexedAttribute<abcV2>(src, indices, dst, topology.m_remap_uv0);
        }
        else if (src.size() == refiner.indices.size()) {
            refiner.addExpandedAttribute<abcV2>(src, dst, topology.m_remap_uv0);
        }
        else if (src.size() == refiner.points.size()) {
            refiner.addIndexedAttribute<abcV2>(src, refiner.indices, dst, topology.m_remap_uv0);
        }
        else {
            DebugLog("Invalid attribute");
            weld_uv0 = false;
        }
    }

    if (sample.m_uv1_sp.valid()) {
        IArray<abcV2> src{ sample.m_uv1_sp.getVals()->get(), sample.m_uv1_sp.getVals()->size() };
        auto& dst = summary.constant_uv1 ? m_constant_uv1 : sample.m_uv1;

        weld_uv1 = true;
        if (sample.m_uv1_sp.isIndexed()) {
            IArray<int> uv1_indices{ (int*)sample.m_uv1_sp.getIndices()->get(), sample.m_uv1_sp.getIndices()->size() };
            refiner.addIndexedAttribute<abcV2>(src, uv1_indices, dst, topology.m_remap_uv1);
        }
        else if (src.size() == refiner.indices.size()) {
            refiner.addExpandedAttribute<abcV2>(src, dst, topology.m_remap_uv1);
        }
        else if (src.size() == refiner.points.size()) {
            refiner.addIndexedAttribute<abcV2>(src, refiner.indices, dst, topology.m_remap_uv1);
        }
        else {
            DebugLog("Invalid attribute");
            weld_uv1 = false;
        }
    }

    if (sample.m_colors_sp.valid()) {
        IArray<abcC4> src{ sample.m_colors_sp.getVals()->get(), sample.m_colors_sp.getVals()->size() };
        auto& dst = summary.constant_colors ? m_constant_colors : sample.m_colors;

        weld_colors = true;
        if (sample.m_colors_sp.isIndexed()) {
            IArray<int> colors_indices{ (int*)sample.m_colors_sp.getIndices()->get(), sample.m_colors_sp.getIndices()->size() };
            refiner.addIndexedAttribute<abcC4>(src, colors_indices, dst, topology.m_remap_colors);
        }
        else if (src.size() == refiner.indices.size()) {
            refiner.addExpandedAttribute<abcC4>(src, dst, topology.m_remap_colors);
        }
        else if (src.size() == refiner.points.size()) {
            refiner.addIndexedAttribute<abcC4>(src, refiner.indices, dst, topology.m_remap_colors);
        }
        else {
            DebugLog("Invalid attribute");
            weld_colors = false;
        }
    }


    refiner.refine();
    refiner.triangulate(config.swap_face_winding, config.turn_quad_edges);

    // generate submeshes
    if (!topology.m_faceset_sps.empty()) {
        // use face set index as material id
        topology.m_material_ids.resize(refiner.counts.size(), -1);
        for (size_t fsi = 0; fsi < topology.m_faceset_sps.size(); ++fsi) {
            auto& faces = *topology.m_faceset_sps[fsi].getFaces();
            size_t num_faces = faces.size();
            for (size_t fi = 0; fi < num_faces; ++fi) {
                topology.m_material_ids[faces[fi]] = (int)fsi;
            }
        }
        refiner.genSubmeshes(topology.m_material_ids);
    }
    else {
        // no face sets present. one split == one submesh
        refiner.genSubmeshes();
    }

    topology.m_index_count = (int)refiner.new_indices_triangulated.size();
    topology.m_vertex_count = (int)refiner.new_points.size();
    onTopologyDetermined();


    topology.m_remap_points.swap(refiner.new2old_points);
    if (summary.constant_points) {
        m_constant_points.swap((RawVector<abcV3>&)refiner.new_points);
        sample.m_points_ref = m_constant_points;
    }
    else {
        sample.m_points.swap((RawVector<abcV3>&)refiner.new_points);
        sample.m_points_ref = sample.m_points;
    }
    if (config.swap_handedness)
        SwapHandedness(sample.m_points_ref.data(), (int)sample.m_points_ref.size());
    if (config.scale_factor != 1.0f)
        ApplyScale(sample.m_points_ref.data(), (int)sample.m_points_ref.size(), config.scale_factor);


    if (weld_normals) {
        sample.m_normals_ref = !m_constant_normals.empty() ? m_constant_normals : sample.m_normals;
        if (config.swap_handedness)
            SwapHandedness(sample.m_normals_ref.data(), (int)sample.m_normals_ref.size());
    }

    if (weld_uv0)
        sample.m_uv0_ref = !m_constant_uv0.empty() ? m_constant_uv0 : sample.m_uv0;

    if (weld_uv1)
        sample.m_uv1_ref = !m_constant_uv1.empty() ? m_constant_uv1 : sample.m_uv1;

    if (weld_colors)
        sample.m_colors_ref = !m_constant_colors.empty() ? m_constant_colors : sample.m_colors;

    if (summary.constant_normals && summary.compute_normals) {
        const auto &indices = topology.m_refiner.new_indices_triangulated;
        m_constant_normals.resize_discard(m_constant_points.size());
        GenerateNormals(m_constant_normals.data(), m_constant_points.data(), indices.data(), (int)m_constant_points.size(), (int)indices.size() / 3);
        sample.m_normals_ref = m_constant_normals;
    }
    if (summary.constant_tangents && summary.compute_tangents) {
        const auto &indices = topology.m_refiner.new_indices_triangulated;
        m_constant_tangents.resize_discard(m_constant_points.size());
        GenerateTangents(m_constant_tangents.data(), m_constant_points.data(), m_constant_uv0.data(), m_constant_normals.data(),
            indices.data(), (int)m_constant_points.size(), (int)indices.size() / 3);
        sample.m_tangents_ref = m_constant_tangents;
    }

    // velocities are done in later part of cookSampleBody()
}

void aiPolyMesh::onTopologyDetermined()
{
    // nothing to do for now
    // maybe I will need to notify C# side for optimization
}

