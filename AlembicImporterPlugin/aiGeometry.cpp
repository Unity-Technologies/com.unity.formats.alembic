#include "pch.h"
#include "AlembicImporter.h"
#include "aiGeometry.h"
#include "aiObject.h"
#include "aiContext.h"


aiSchema::aiSchema() : m_obj(nullptr) {}
aiSchema::aiSchema(aiObject *obj) : m_obj(obj) {}
aiSchema::~aiSchema() {}



aiXForm::aiXForm() {}

aiXForm::aiXForm(aiObject *obj)
    : super(obj)
{
    AbcGeom::IXform xf(obj->getAbcObject(), Abc::kWrapExisting);
    m_schema = xf.getSchema();
}

void aiXForm::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());
    m_schema.get(m_sample, ss);
    m_inherits = m_schema.getInheritsXforms(ss);
}


void aiXForm::debugDump() const
{

}

bool aiXForm::getInherits() const
{
    return m_inherits;
}

abcV3 aiXForm::getPosition() const
{
    abcV3 ret = m_sample.getTranslation();
    if (m_obj->getReverseX()) {
        ret.x *= -1.0f;
    }
    return ret;
}

abcV3 aiXForm::getAxis() const
{
    abcV3 ret = m_sample.getAxis();
    if (m_obj->getReverseX()) {
        ret.x *= -1.0f;
    }
    return ret;
}

float aiXForm::getAngle() const
{
    float ret = m_sample.getAngle();
    if (m_obj->getReverseX()) {
        ret *= -1.0f;
    }
    return ret;
}

abcV3 aiXForm::getScale() const
{
    return abcV3(m_sample.getScale());
}

abcM44 aiXForm::getMatrix() const
{
    return abcM44(m_sample.getMatrix());
}



aiPolyMesh::aiPolyMesh()
    : m_peak_index_count(0)
    , m_peak_vertex_count(0)
    , m_task_running(0)
{}

aiPolyMesh::aiPolyMesh(aiObject *obj)
    : super(obj)
{
    AbcGeom::IPolyMesh pm(obj->getAbcObject(), Abc::kWrapExisting);
    m_schema = pm.getSchema();
}

void aiPolyMesh::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());
    m_schema.getFaceIndicesProperty().get(m_indices, ss);
    m_schema.getFaceCountsProperty().get(m_counts, ss);
    m_schema.getPositionsProperty().get(m_positions, ss);

    m_velocities.reset();
    if (m_schema.getVelocitiesProperty().valid()) {
        m_schema.getVelocitiesProperty().get(m_velocities, ss);
    }

    m_normals.reset();
    if (m_schema.getNormalsParam().valid()) {
        m_schema.getNormalsParam().getIndexed(m_normals, ss);
    }

    m_uvs.reset();
    if (m_schema.getUVsParam().valid()) {
        m_schema.getUVsParam().getIndexed(m_uvs, ss);
    }
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

    aiDebugLog("toology variance: %s", topology_variance_s[m_schema.getTopologyVariance()]);
    if (getTopologyVariance()==AbcGeom::kConstantTopology) {
        aiDebugLog(" (%d)", (*m_counts)[0]);
    }
    aiDebugLog("\n");
    aiDebugLog("has normals: %s\n", hasNormals() ? true_s : false_s);
    aiDebugLog("has uvs: %s\n", hasUVs() ? true_s : false_s);
    aiDebugLog("has velocities: %s\n", hasVelocities() ? true_s : false_s);

    aiDebugLog("index count: %u", getIndexCount());
    if (getTopologyVariance() == AbcGeom::kHeterogeneousTopology) {
        aiDebugLog(" (peak %u)", getPeakIndexCount());
    }
    aiDebugLog("\n");

    aiDebugLog("vertex count: %u", getVertexCount());
    if (getTopologyVariance() == AbcGeom::kHeterogeneousTopology) {
        aiDebugLog(" (peak %u)", getPeakVertexCount());
    }
    aiDebugLog("\n");
}

int aiPolyMesh::getTopologyVariance() const
{
    return (int)m_schema.getTopologyVariance();
}

bool aiPolyMesh::isTopologyConstantTriangles() const
{
    return m_schema.isConstant() && (*m_counts)[0] == 3;
}

bool aiPolyMesh::hasNormals() const
{
    return m_normals.valid();
}

bool aiPolyMesh::hasUVs() const
{
    return m_uvs.valid();
}

bool aiPolyMesh::hasVelocities() const
{
    return m_velocities && m_velocities->valid();
}

bool aiPolyMesh::isNormalIndexed() const
{
    return m_normals.valid() && m_normals.isIndexed();
}

bool aiPolyMesh::isUVIndexed() const
{
    return m_uvs.valid() && m_uvs.isIndexed();
}

inline uint32_t CalculateIndexCount(
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

uint32_t aiPolyMesh::getIndexCount() const
{
    return CalculateIndexCount(*m_counts, *m_indices, m_obj->getTriangulate());
}

uint32_t aiPolyMesh::getVertexCount() const
{
    return m_positions->size();
}

uint32_t aiPolyMesh::getPeakIndexCount() const
{
    // ˆê‰ž caching ‚µ‚Ä‚¨‚­

    if (m_peak_index_count == 0) {
        Abc::Int32ArraySamplePtr counts;
        Abc::Int32ArraySamplePtr indices;
        auto &index_prop = m_schema.getFaceIndicesProperty();
        auto &count_prop = m_schema.getFaceCountsProperty();
        int num_samples = index_prop.getNumSamples();
        if (num_samples == 0) { return 0; }

        if (index_prop.isConstant()) {
            index_prop.get(indices, Abc::ISampleSelector(int64_t(0)));
            count_prop.get(counts, Abc::ISampleSelector(int64_t(0)));
        }
        else {
            int i_max = 0;
            int c_max = 0;
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
        m_peak_index_count = CalculateIndexCount(*counts, *indices, m_obj->getTriangulate());
    }

    return m_peak_index_count;
}

uint32_t aiPolyMesh::getPeakVertexCount() const
{
    if (m_peak_vertex_count == 0) {
        Abc::P3fArraySamplePtr positions;
        auto &positions_prop = m_schema.getPositionsProperty();
        int num_samples = positions_prop.getNumSamples();
        if (num_samples == 0) { return 0; }

        if (positions_prop.isConstant()) {
            positions_prop.get(positions, Abc::ISampleSelector(int64_t(0)));
        }
        else {
            int i_max = 0;
            int c_max = 0;
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

void aiPolyMesh::copyIndices(int *dst) const
{
    bool reverse_index = m_obj->getReverseIndex();
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;

    if (m_obj->getTriangulate())
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


template<class VecT>
void aiPolyMesh::copyVertices(VecT *dst) const
{
    if (!m_positions) { return; }

    const auto &cont = *m_positions;
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        (abcV3&)dst[i] = cont[i];
    }
    if (m_obj->getReverseX()) {
        for (size_t i = 0; i < n; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}
template void aiPolyMesh::copyVertices<abcV3>(abcV3 *dst) const;
template void aiPolyMesh::copyVertices<abcV4>(abcV4 *dst) const;


template<class VecT>
void aiPolyMesh::copyVelocities(VecT *dst) const
{
    if (!m_velocities) { return; }

    const auto &cont = *m_velocities;
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        (abcV3&)dst[i] = cont[i];
    }
    if (m_obj->getReverseX()) {
        for (size_t i = 0; i < n; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}
template void aiPolyMesh::copyVelocities<abcV3>(abcV3 *dst) const;
template void aiPolyMesh::copyVelocities<abcV4>(abcV4 *dst) const;


template<class VecT>
void aiPolyMesh::copyNormals(VecT *dst) const
{
    if (!m_normals) { return; }

    const auto &cont = *m_normals.getVals();
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        (abcV3&)dst[i] = cont[i];
    }
    if (m_obj->getReverseX()) {
        for (size_t i = 0; i < n; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}
template void aiPolyMesh::copyNormals<abcV3>(abcV3 *dst) const;
template void aiPolyMesh::copyNormals<abcV4>(abcV4 *dst) const;


void aiPolyMesh::copyUVs(abcV2 *dst) const
{
    if (!m_uvs) { return; }

    const auto &cont = *m_uvs.getVals();
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        dst[i] = cont[i];
    }
    if (m_obj->getReverseX()) {
        for (size_t i = 0; i < n; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}




#ifdef aiSupportTextureMesh
#include "GraphicsDevice//aiGraphicsDevice.h"

void aiPolyMesh::copyMeshToTexture(aiTextureMeshData &dst) const
{
    aiIGraphicsDevice *dev = aiGetGraphicsDevice();
    if (!dev) { return; }

    dst.is_normal_indexed = isNormalIndexed();
    dst.is_uv_indexed = isUVIndexed();
    dst.index_count = getIndexCount();
    dst.vertex_count = getVertexCount();

    if (dst.tex_indices) {
        uint32_t n = dst.index_count;
        m_buf.resize(n);
        copyIndices((int*)&m_buf[0]);
        dev->writeTexture(dst.tex_indices, dst.tex_width, ceildiv<uint32_t>(n, dst.tex_width), aiE_RInt, &m_buf[0], n*sizeof(int));
    }

    if (dst.tex_vertices && m_positions) {
        uint32_t n = dst.vertex_count;
        m_buf.resize(n * 4);
        copyVertices((abcV4*)&m_buf[0]);
        dev->writeTexture(dst.tex_vertices, dst.tex_width, ceildiv<uint32_t>(n, dst.tex_width), aiE_ARGBFloat, &m_buf[0], n*sizeof(abcV4));
    }

    if (dst.tex_normals && m_normals) {
        uint32_t n = m_normals.getVals()->size();
        m_buf.resize(n * 4);
        copyNormals((abcV4*)&m_buf[0]);
        dev->writeTexture(dst.tex_normals, dst.tex_width, ceildiv<uint32_t>(n, dst.tex_width), aiE_ARGBFloat, &m_buf[0], n*sizeof(abcV4));
    }

    if (dst.tex_uvs && m_uvs) {
        uint32_t n = m_uvs.getVals()->size();
        m_buf.resize(n * 2);
        copyUVs((abcV2*)&m_buf[0]);
        dev->writeTexture(dst.tex_uvs, dst.tex_width, ceildiv<uint32_t>(n, dst.tex_width), aiE_RGFloat, &m_buf[0], n*sizeof(abcV2));
    }

    if (dst.tex_velocities && m_velocities && m_velocities->valid()) {
        uint32_t n = m_velocities->size();
        m_buf.resize(n * 4);
        copyVertices((abcV4*)&m_buf[0]);
        dev->writeTexture(dst.tex_velocities, dst.tex_width, ceildiv<uint32_t>(n, dst.tex_width), aiE_ARGBFloat, &m_buf[0], n*sizeof(abcV4));
    }
}

void aiPolyMesh::beginCopyMeshToTexture(aiTextureMeshData &dst) const
{
    ++m_task_running;
    m_obj->getContext()->enqueueTask([this, &dst](){
        copyMeshToTexture(dst);
        --m_task_running;
    });
}

void aiPolyMesh::endCopyMeshToTexture() const
{
    while (m_task_running.load()!=0) {
        std::this_thread::sleep_for(std::chrono::microseconds(100));
    }
}

#endif // aiSupportTextureMesh



bool aiPolyMesh::getSplitedMeshInfo(aiSplitedMeshInfo &o_smi, const aiSplitedMeshInfo& prev, int max_vertices) const
{
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;
    size_t nc = counts.size();

    aiSplitedMeshInfo smi = { 0 };
    smi.begin_face = prev.begin_face + prev.num_faces;
    smi.begin_index = prev.begin_index + prev.num_indices;

    bool is_end = true;
    int a = 0;
    for (size_t i = smi.begin_face; i < nc; ++i) {
        int ngon = counts[i];
        if (a + ngon >= max_vertices) {
            is_end = false;
            break;
        }

        a += ngon;
        smi.num_faces++;
        smi.num_indices = a;
        smi.triangulated_index_count += (ngon - 2) * 3;
    }

    smi.num_vertices = a;
    o_smi = smi;
    return is_end;
}

void aiPolyMesh::copySplitedIndices(int *dst, const aiSplitedMeshInfo &smi) const
{
    bool reverse_index = m_obj->getReverseIndex();
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

void aiPolyMesh::copySplitedVertices(abcV3 *dst, const aiSplitedMeshInfo &smi) const
{
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
    if (m_obj->getReverseX()) {
        for (size_t i = 0; i < a; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}

void aiPolyMesh::copySplitedNormals(abcV3 *dst, const aiSplitedMeshInfo &smi) const
{
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
    if (m_obj->getReverseX()) {
        for (size_t i = 0; i < a; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}

void aiPolyMesh::copySplitedUVs(abcV2 *dst, const aiSplitedMeshInfo &smi) const
{
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




aiCurves::aiCurves() {}

aiCurves::aiCurves(aiObject *obj)
    : super(obj)
{
    AbcGeom::ICurves curves(obj->getAbcObject(), Abc::kWrapExisting);
    m_schema = curves.getSchema();
}

void aiCurves::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());
    // todo
}



aiPoints::aiPoints() {}

aiPoints::aiPoints(aiObject *obj)
    : super(obj)
{
    AbcGeom::IPoints points(obj->getAbcObject(), Abc::kWrapExisting);
    m_schema = points.getSchema();
}

void aiPoints::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());
    // todo
}



aiCamera::aiCamera() {}

aiCamera::aiCamera(aiObject *obj)
    : super(obj)
{
    AbcGeom::ICamera cam(obj->getAbcObject(), Abc::kWrapExisting);
    m_schema = cam.getSchema();
}

void aiCamera::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());
    // todo
}

void aiCamera::debugDump() const
{

}

void aiCamera::getParams(aiCameraParams &o_params)
{
    o_params.near_clipping_plane = m_sample.getNearClippingPlane();
    o_params.far_clipping_plane = m_sample.getFarClippingPlane();
    o_params.field_of_view = m_sample.getFieldOfView();
    o_params.focus_distance = m_sample.getFocusDistance() * 0.1f; // centimeter to meter
    o_params.focal_length = m_sample.getFocalLength() * 0.01f; // milimeter to meter
}


aiLight::aiLight() {}
aiLight::aiLight(aiObject *obj)
    : super(obj)
{
}

void aiLight::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());
    // todo
}



aiMaterial::aiMaterial() {}

aiMaterial::aiMaterial(aiObject *obj)
    : super(obj)
{
    AbcMaterial::IMaterial material(obj->getAbcObject(), Abc::kWrapExisting);
    m_schema = material.getSchema();
}

void aiMaterial::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());
    // todo
}
