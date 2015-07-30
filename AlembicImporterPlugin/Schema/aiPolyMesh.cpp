#include "pch.h"
#include "AlembicImporter.h"
#include "aiSchema.h"
#include "aiObject.h"
#include "aiContext.h"
#include "aiPolyMesh.h"


static inline uint32_t CalculateIndexCount(
    Abc::Int32ArraySample &counts,
    Abc::Int32ArraySample &indices,
    bool triangulate)
{
    if (triangulate)
    {
        uint32_t r = 0;
        size_t n = counts.size();
        for (size_t fi = 0; fi < n; ++fi) {
            int ngon = counts[fi];
            r += (ngon - 2) * 3;
        }
        return r;
    }
    else
    {
        return indices.size();
    }
}





aiPolyMeshSample::aiPolyMeshSample(aiPolyMesh *schema, float time)
    : super(schema, time)
{
}

bool aiPolyMeshSample::hasNormals() const
{
    return m_normals.valid();
}

bool aiPolyMeshSample::hasUVs() const
{
    return m_uvs.valid();
}

bool aiPolyMeshSample::hasVelocities() const
{
    return m_velocities && m_velocities->valid();
}

bool aiPolyMeshSample::isNormalIndexed() const
{
    return m_normals.valid() && m_normals.isIndexed();
}

bool aiPolyMeshSample::isUVIndexed() const
{
    return m_uvs.valid() && m_uvs.isIndexed();
}

uint32_t aiPolyMeshSample::getIndexCount() const
{
    return CalculateIndexCount(*m_counts, *m_indices, m_schema->getImportConfig().triangulate);
}

uint32_t aiPolyMeshSample::getVertexCount() const
{
    return m_positions->size();
}



void aiPolyMeshSample::getSummary(aiPolyMeshSampleSummary &o_summary) const
{
    o_summary.has_normals = hasNormals();
    o_summary.has_uvs = hasUVs();
    o_summary.has_velocities = hasVelocities();
    o_summary.is_normals_indexed = isNormalIndexed();
    o_summary.is_uvs_indexed = isUVIndexed();
    o_summary.index_count = getIndexCount();
    o_summary.vertex_count = getVertexCount();
}

void aiPolyMeshSample::copyIndices(int *dst) const
{
    bool reverse_index = m_schema->getImportConfig().revert_face;
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;

    if (m_schema->getImportConfig().triangulate)
    {
        uint32_t a = 0;
        uint32_t b = 0;
        uint32_t i1 = reverse_index ? 2 : 1;
        uint32_t i2 = reverse_index ? 1 : 2;
        size_t n = counts.size();
        for (size_t fi = 0; fi < n; ++fi) {
            int ngon = counts[fi];
            for (int ni = 0; ni < (ngon - 2); ++ni) {
                dst[b + 0] = std::max<int>(indices[a], 0);
                dst[b + 1] = std::max<int>(indices[a + i1 + ni], 0);
                dst[b + 2] = std::max<int>(indices[a + i2 + ni], 0);
                b += 3;
            }
            a += ngon;
        }
    }
    else
    {
        size_t n = indices.size();
        if (reverse_index) {
            for (size_t i = 0; i < n; ++i) {
                dst[i] = indices[n - i - 1];
            }
        }
        else {
            for (size_t i = 0; i < n; ++i) {
                dst[i] = indices[i];
            }
        }
    }
}


void aiPolyMeshSample::copyVertices(abcV3 *dst) const
{
    if (!m_positions) { return; }

    const auto &cont = *m_positions;
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        (abcV3&)dst[i] = cont[i];
    }
    if (m_schema->getImportConfig().revert_x) {
        for (size_t i = 0; i < n; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}


void aiPolyMeshSample::copyVelocities(abcV3 *dst) const
{
    if (!m_velocities) { return; }

    const auto &cont = *m_velocities;
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        (abcV3&)dst[i] = cont[i];
    }
    if (m_schema->getImportConfig().revert_x) {
        for (size_t i = 0; i < n; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}


void aiPolyMeshSample::copyNormals(abcV3 *dst) const
{
    if (!m_normals) { return; }

    const auto &cont = *m_normals.getVals();
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        (abcV3&)dst[i] = cont[i];
    }
    if (m_schema->getImportConfig().revert_x) {
        for (size_t i = 0; i < n; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}


void aiPolyMeshSample::copyUVs(abcV2 *dst) const
{
    if (!m_uvs) { return; }

    const auto &cont = *m_uvs.getVals();
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        dst[i] = cont[i];
    }
    if (m_schema->getImportConfig().revert_x) {
        for (size_t i = 0; i < n; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}


#ifdef aiSupportTextureMesh
#include "GraphicsDevice/aiGraphicsDevice.h"

void aiPolyMeshSample::copyMeshToTexture(aiTextureMeshData &dst) const
{
    aiIGraphicsDevice *dev = aiGetGraphicsDevice();
    if (!dev || !m_indices) { return; }

    //aiDebugLogVerbose("void aiPolyMeshSample::copyMeshToTexture(): %s\n", getSchema()->getObject()->getName());

    dst.is_normal_indexed = isNormalIndexed();
    dst.is_uv_indexed = isUVIndexed();
    dst.index_count = getIndexCount();
    dst.vertex_count = getVertexCount();

    if (dst.tex_indices) {
        uint32_t n = dst.index_count;
        uint32_t w = dst.tex_width;
        m_buf.resize(ceilup<uint32_t>(n, w));
        copyIndices((int*)&m_buf[0]);
        dev->writeTexture(dst.tex_indices, w, ceildiv<uint32_t>(n, w), aiE_RInt, &m_buf[0], n*sizeof(int));
    }

    if (dst.tex_vertices && m_positions) {
        uint32_t n = dst.vertex_count * 3;
        uint32_t w = dst.tex_width * 3;
        m_buf.resize(ceilup<uint32_t>(n, w));
        copyVertices((abcV3*)&m_buf[0]);
        dev->writeTexture(dst.tex_vertices, w, ceildiv<uint32_t>(n, w), aiE_RFloat, &m_buf[0], n*sizeof(float));
    }

    if (dst.tex_normals && m_normals) {
        uint32_t n = m_normals.getVals()->size() * 3;
        uint32_t w = dst.tex_width * 3;
        m_buf.resize(ceilup<uint32_t>(n, w));
        copyNormals((abcV3*)&m_buf[0]);
        dev->writeTexture(dst.tex_normals, w, ceildiv<uint32_t>(n, w), aiE_RFloat, &m_buf[0], n*sizeof(float));
    }

    if (dst.tex_uvs && m_uvs) {
        uint32_t n = m_uvs.getVals()->size() * 2;
        uint32_t w = dst.tex_width * 2;
        m_buf.resize(ceilup<uint32_t>(n, w));
        copyUVs((abcV2*)&m_buf[0]);
        dev->writeTexture(dst.tex_uvs, w, ceildiv<uint32_t>(n, w), aiE_RFloat, &m_buf[0], n*sizeof(float));
    }

    if (dst.tex_velocities && m_velocities && m_velocities->valid()) {
        uint32_t n = m_velocities->size() * 3;
        uint32_t w = dst.tex_width * 3;
        m_buf.resize(ceilup<uint32_t>(n, w));
        copyVertices((abcV3*)&m_buf[0]);
        dev->writeTexture(dst.tex_velocities, w, ceildiv<uint32_t>(n, w), aiE_RFloat, &m_buf[0], n*sizeof(float));
    }
}

#endif // aiSupportTextureMesh



bool aiPolyMeshSample::getSplitedMeshInfo(aiSplitedMeshData &o_smi, const aiSplitedMeshData& prev, int max_vertices) const
{
    if (!m_counts) { return false; }
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;
    size_t nc = counts.size();

    aiSplitedMeshData smi;
    smi.begin_face = prev.begin_face + prev.num_faces;
    smi.begin_index = prev.begin_index + prev.num_indices;

    bool is_end = true;
    int a = 0;
    for (size_t i = smi.begin_face; i < nc; ++i) {
        int ngon = counts[i];
        int tic = (ngon - 2) * 3;
        if (a + ngon >= max_vertices) {
            is_end = false;
            break;
        }

        a += ngon;
        smi.num_faces++;
        smi.num_indices = a;
        smi.triangulated_index_count += tic;
    }

    smi.num_vertices = a;
    o_smi = smi;
    return is_end;
}

void aiPolyMeshSample::copySplitedMesh(aiSplitedMeshData &o_smi) const
{
    if (o_smi.dst_index) {
        copySplitedIndices(o_smi.dst_index, o_smi);
    }
    if (o_smi.dst_vertices) {
        copySplitedVertices(o_smi.dst_vertices, o_smi);
    }
    if (o_smi.dst_normals) {
        copySplitedNormals(o_smi.dst_normals, o_smi);
    }
    if (o_smi.dst_uvs) {
        copySplitedUVs(o_smi.dst_uvs, o_smi);
    }
}

void aiPolyMeshSample::copySplitedIndices(int *dst, const aiSplitedMeshData &smi) const
{
    if (!m_counts) { return; }
    bool reverse_index = m_schema->getImportConfig().revert_face;
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;

    uint32_t a = 0;
    uint32_t b = 0;
    uint32_t i1 = reverse_index ? 2 : 1;
    uint32_t i2 = reverse_index ? 1 : 2;
    for (int fi = 0; fi < smi.num_faces; ++fi) {
        int ngon = counts[smi.begin_face + fi];
        for (int ni = 0; ni < (ngon - 2); ++ni) {
            dst[b + 0] = a;
            dst[b + 1] = a + i1 + ni;
            dst[b + 2] = a + i2 + ni;
            b += 3;
        }
        a += ngon;
    }
}

void aiPolyMeshSample::copySplitedVertices(abcV3 *dst, const aiSplitedMeshData &smi) const
{
    if (!m_counts) { return; }
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;
    const auto &positions = *m_positions;

    uint32_t a = 0;
    for (int fi = 0; fi < smi.num_faces; ++fi) {
        int ngon = counts[smi.begin_face + fi];
        for (int ni = 0; ni < ngon; ++ni) {
            dst[a + ni] = positions[indices[a + ni + smi.begin_index]];
        }
        a += ngon;
    }
    if (m_schema->getImportConfig().revert_x) {
        for (size_t i = 0; i < a; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}

void aiPolyMeshSample::copySplitedNormals(abcV3 *dst, const aiSplitedMeshData &smi) const
{
    if (!m_counts) { return; }
    const auto &counts = *m_counts;
    const auto &normals = *m_normals.getVals();
    const auto &indices = *m_normals.getIndices();

    uint32_t a = 0;
    if (m_normals.isIndexed())
    {
        for (int fi = 0; fi < smi.num_faces; ++fi) {
            int ngon = counts[smi.begin_face + fi];
            for (int ni = 0; ni < ngon; ++ni) {
                dst[a + ni] = normals[indices[a + ni + smi.begin_index]];
            }
            a += ngon;
        }
    }
    else
    {
        for (int fi = 0; fi < smi.num_faces; ++fi) {
            int ngon = counts[smi.begin_face + fi];
            for (int ni = 0; ni < ngon; ++ni) {
                dst[a + ni] = normals[a + ni];
            }
            a += ngon;
        }
    }
    if (m_schema->getImportConfig().revert_x) {
        for (size_t i = 0; i < a; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}

void aiPolyMeshSample::copySplitedUVs(abcV2 *dst, const aiSplitedMeshData &smi) const
{
    if (!m_counts) { return; }
    const auto &counts = *m_counts;
    const auto &uvs = *m_uvs.getVals();
    const auto &indices = *m_uvs.getIndices();

    if (m_uvs.isIndexed())
    {
        uint32_t a = 0;
        for (int fi = 0; fi < smi.num_faces; ++fi) {
            int ngon = counts[smi.begin_face + fi];
            for (int ni = 0; ni < ngon; ++ni) {
                dst[a + ni] = uvs[indices[a + ni + smi.begin_index]];
            }
            a += ngon;
        }
    }
    else
    {
        uint32_t a = 0;
        for (int fi = 0; fi < smi.num_faces; ++fi) {
            int ngon = counts[smi.begin_face + fi];
            for (int ni = 0; ni < ngon; ++ni) {
                dst[a + ni] = uvs[a + ni];
            }
            a += ngon;
        }
    }
}




aiPolyMesh::aiPolyMesh(aiObject *obj)
    : super(obj)
    , m_peak_index_count(0)
    , m_peak_vertex_count(0)
{
    AbcGeom::IPolyMesh pm(obj->getAbcObject(), Abc::kWrapExisting);
    m_schema = pm.getSchema();
}


aiPolyMesh::Sample* aiPolyMesh::readSample(float time)
{
    Sample *ret = new Sample(this, time);

    auto ss = makeSampleSelector(time);
    m_schema.getFaceIndicesProperty().get(ret->m_indices, ss);
    m_schema.getFaceCountsProperty().get(ret->m_counts, ss);
    m_schema.getPositionsProperty().get(ret->m_positions, ss);

    ret->m_velocities.reset();
    if (m_schema.getVelocitiesProperty().valid()) {
        m_schema.getVelocitiesProperty().get(ret->m_velocities, ss);
    }

    ret->m_normals.reset();
    auto normal_param = m_schema.getNormalsParam();
    if (normal_param.valid()) {
        if (normal_param.isIndexed()) {
            normal_param.getIndexed(ret->m_normals, ss);
        }
        else {
            normal_param.getExpanded(ret->m_normals, ss);
        }
    }

    ret->m_uvs.reset();
    auto uv_param = m_schema.getUVsParam();
    if (uv_param.valid()) {
        if (uv_param.isIndexed()) {
            uv_param.getIndexed(ret->m_uvs, ss);
        }
        else {
            uv_param.getExpanded(ret->m_uvs, ss);
        }
    }
    return ret;
}

void aiPolyMesh::debugDump() const
{
    aiDebugLog("- poly mesh\n");

    const char *topology_variance_s[] = {
        "kConstantTopology",
        "kHomogeneousTopology",
        "kHeterogeneousTopology"
    };
    const char true_s[] = "true";
    const char false_s[] = "false";

    const Sample *s = findSample(0);
    if (s) {
        aiDebugLog("toology variance: %s", topology_variance_s[m_schema.getTopologyVariance()]);
        if (getTopologyVariance() == AbcGeom::kConstantTopology) {
            aiDebugLog(" (%d)", (*s->m_counts)[0]);
        }
        aiDebugLog("\n");
        aiDebugLog("has normals: %s\n", s->hasNormals() ? true_s : false_s);
        aiDebugLog("has uvs: %s\n", s->hasUVs() ? true_s : false_s);
        aiDebugLog("has velocities: %s\n", s->hasVelocities() ? true_s : false_s);

        aiDebugLog("index count: %u", s->getIndexCount());
        if (getTopologyVariance() == AbcGeom::kHeterogeneousTopology) {
            aiDebugLog(" (peak %u)", getPeakIndexCount());
        }
        aiDebugLog("\n");

        aiDebugLog("vertex count: %u", s->getVertexCount());
        if (getTopologyVariance() == AbcGeom::kHeterogeneousTopology) {
            aiDebugLog(" (peak %u)", getPeakVertexCount());
        }
        aiDebugLog("\n");
    }
}

int aiPolyMesh::getTopologyVariance() const
{
    return (int)m_schema.getTopologyVariance();
}

uint32_t aiPolyMesh::getPeakIndexCount() const
{
    // ˆê‰ž caching ‚µ‚Ä‚¨‚­

    if (m_peak_index_count == 0) {
        Abc::Int32ArraySamplePtr counts;
        Abc::Int32ArraySamplePtr indices;
        auto index_prop = m_schema.getFaceIndicesProperty();
        auto count_prop = m_schema.getFaceCountsProperty();
        int num_samples = index_prop.getNumSamples();
        if (num_samples == 0) { return 0; }

        if (index_prop.isConstant()) {
            index_prop.get(indices, Abc::ISampleSelector(int64_t(0)));
            count_prop.get(counts, Abc::ISampleSelector(int64_t(0)));
        }
        else {
            int i_max = 0;
            size_t c_max = 0;
            for (int i = 0; i < num_samples; ++i) {
                index_prop.get(indices, Abc::ISampleSelector(int64_t(i)));
                if (indices->size() > c_max) {
                    c_max = indices->size();
                    i_max = i;
                }
            }
            index_prop.get(indices, Abc::ISampleSelector(int64_t(i_max)));
            count_prop.get(counts, Abc::ISampleSelector(int64_t(i_max)));
        }
        m_peak_index_count = CalculateIndexCount(*counts, *indices, getImportConfig().triangulate);
    }

    return m_peak_index_count;
}

uint32_t aiPolyMesh::getPeakVertexCount() const
{
    if (m_peak_vertex_count == 0) {
        Abc::P3fArraySamplePtr positions;
        auto positions_prop = m_schema.getPositionsProperty();
        int num_samples = positions_prop.getNumSamples();
        if (num_samples == 0) { return 0; }

        if (positions_prop.isConstant()) {
            positions_prop.get(positions, Abc::ISampleSelector(int64_t(0)));
        }
        else {
            int i_max = 0;
            size_t c_max = 0;
            for (int i = 0; i < num_samples; ++i) {
                positions_prop.get(positions, Abc::ISampleSelector(int64_t(i)));
                if (positions->size() > c_max) {
                    c_max = positions->size();
                    i_max = i;
                }
            }
            positions_prop.get(positions, Abc::ISampleSelector(int64_t(i_max)));
        }
        m_peak_vertex_count = positions->size();
    }
    return m_peak_vertex_count;
}

void aiPolyMesh::getSummary(aiPolyMeshSchemaSummary &o_summary) const
{
    o_summary.topology_variance = getTopologyVariance();
    o_summary.peak_index_count = getPeakIndexCount();
    o_summary.peak_vertex_count = getPeakVertexCount();

    const auto &normal_param = m_schema.getNormalsParam();
    o_summary.has_normals = normal_param.valid();
    o_summary.is_normals_indexed = normal_param.valid() && normal_param.isIndexed();

    const auto &uv_param = m_schema.getUVsParam();
    o_summary.has_uvs = uv_param.valid();
    o_summary.is_uvs_indexed = uv_param.valid() && uv_param.isIndexed();
}

