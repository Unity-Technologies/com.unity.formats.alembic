#include "pch.h"
#include "AlembicImporter.h"
#include "aiGeometry.h"


aiXForm::aiXForm() {}

aiXForm::aiXForm(abcObject obj, Abc::ISampleSelector ss)
    : m_reverse_x(true)
{
    AbcGeom::IXform xf(obj, Abc::kWrapExisting);
    m_schema = xf.getSchema();
    m_schema.get(m_sample, ss);
    m_inherits = m_schema.getInheritsXforms(ss);
}

void aiXForm::enableReverseX(bool v)
{
    m_reverse_x = v;
}

bool aiXForm::getInherits() const
{
    return m_inherits;
}

abcV3 aiXForm::getPosition() const
{
    abcV3 ret = m_sample.getTranslation();
    if (m_reverse_x) {
        ret.x *= -1.0f;
    }
    return ret;
}

abcV3 aiXForm::getAxis() const
{
    abcV3 ret = m_sample.getAxis();
    if (m_reverse_x) {
        ret.x *= -1.0f;
    }
    return ret;
}

float aiXForm::getAngle() const
{
    float ret = m_sample.getAngle();
    if (m_reverse_x) {
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


aiPolyMesh::aiPolyMesh() {}

aiPolyMesh::aiPolyMesh(abcObject obj, Abc::ISampleSelector ss)
    : m_reverse_x(true)
    , m_triangulate(true)
    , m_reverse_index(false)
{
    AbcGeom::IPolyMesh pm(obj, Abc::kWrapExisting);
    m_schema = pm.getSchema();
    m_schema.getFaceIndicesProperty().get(m_indices, ss);
    m_schema.getFaceCountsProperty().get(m_counts, ss);
    m_schema.getPositionsProperty().get(m_positions, ss);
    if (m_schema.getVelocitiesProperty().valid()) {
        m_schema.getVelocitiesProperty().get(m_velocities, ss);
    }
    if (!m_schema.isConstant()) {
        aiDebugLog("warning: topology is not consant\n");
    }
}

void aiPolyMesh::enableReverseX(bool v)
{
    m_reverse_x = v;
}

void aiPolyMesh::enableTriangulate(bool v)
{
    m_triangulate = v;
}

void aiPolyMesh::enableReverseIndex(bool v)
{
    m_reverse_index = v;
}


bool aiPolyMesh::isTopologyConstant() const
{
    return m_schema.isConstant();
}

bool aiPolyMesh::isTopologyConstantTriangles() const
{
    return m_schema.isConstant() && (*m_counts)[0] == 3;
}

bool aiPolyMesh::isNormalIndexed() const
{
    return m_schema.getNormalsParam().isIndexed();
}

bool aiPolyMesh::isUVIndexed() const
{
    return m_schema.getUVsParam().isIndexed();
}

uint32_t aiPolyMesh::getIndexCount() const
{
    if (m_triangulate)
    {
        uint32_t r = 0;
        const auto &counts = *m_counts;
        size_t n = counts.size();
        for (size_t fi = 0; fi < n; ++fi) {
            int ngon = counts[fi];
            r += (ngon - 2) * 3;
        }
        return r;
    }
    else
    {
        return m_indices->size();
    }
}

uint32_t aiPolyMesh::getVertexCount() const
{
    return m_positions->size();
}

void aiPolyMesh::copyIndices(int *dst) const
{
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;

    if (m_triangulate)
    {
        uint32_t a = 0;
        uint32_t b = 0;
        uint32_t i1 = m_reverse_index ? 2 : 1;
        uint32_t i2 = m_reverse_index ? 1 : 2;
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
        if (m_reverse_index) {
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

void aiPolyMesh::copyVertices(abcV3 *dst) const
{
    const auto &cont = *m_positions;
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        dst[i] = cont[i];
    }
    if (m_reverse_x) {
        for (size_t i = 0; i < n; ++i) {
            dst[i].x *= -1.0f;
        }
    }
}

bool aiPolyMesh::getSplitedMeshInfo(aiSplitedMeshInfo &o_smi, const aiSplitedMeshInfo& prev, int max_vertices) const
{
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;
    size_t nc = counts.size();

    aiSplitedMeshInfo smi = { 0 };
    smi.begin_face = prev.begin_face + prev.num_faces;
    smi.begin_index = prev.begin_index + prev.num_indices;

    bool is_end = true;
    uint32_t a = 0;
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
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;

    uint32_t a = 0;
    uint32_t b = 0;
    uint32_t i1 = m_reverse_index ? 2 : 1;
    uint32_t i2 = m_reverse_index ? 1 : 2;
    for (size_t fi = 0; fi < smi.num_faces; ++fi) {
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
    for (size_t fi = 0; fi < smi.num_faces; ++fi) {
        int ngon = counts[smi.begin_face + fi];
        for (int ni = 0; ni < ngon; ++ni) {
            dst[a + ni] = positions[indices[a + ni + smi.begin_index]];
        }
        a += ngon;
    }
}


aiCurves::aiCurves() {}

aiCurves::aiCurves(abcObject obj, Abc::ISampleSelector ss)
    : m_reverse_x(true)
{
    AbcGeom::ICurves curves(obj, Abc::kWrapExisting);
    m_schema = curves.getSchema();
}

void aiCurves::enableReverseX(bool v)
{
    m_reverse_x = v;
}


aiPoints::aiPoints() {}

aiPoints::aiPoints(abcObject obj, Abc::ISampleSelector ss)
    : m_reverse_x(true)
{
    AbcGeom::IPoints points(obj, Abc::kWrapExisting);
    m_schema = points.getSchema();
}

void aiPoints::enableReverseX(bool v)
{
    m_reverse_x = v;
}



aiCamera::aiCamera() {}

aiCamera::aiCamera(abcObject obj, Abc::ISampleSelector ss)
{
    AbcGeom::ICamera cam(obj, Abc::kWrapExisting);
    m_schema = cam.getSchema();
    m_schema.get(m_sample, ss);
}

void aiCamera::getParams(aiCameraParams &o_params)
{
    o_params.near_clipping_plane = m_sample.getNearClippingPlane();
    o_params.far_clipping_plane = m_sample.getFarClippingPlane();
    o_params.field_of_view = m_sample.getFieldOfView();
    o_params.focus_distance = m_sample.getFocusDistance() * 0.1f; // centimeter to meter
    o_params.focal_length = m_sample.getFocalLength() * 0.01f; // milimeter to meter
}

aiMaterial::aiMaterial() {}

aiMaterial::aiMaterial(abcObject obj, Abc::ISampleSelector ss)
{
    AbcMaterial::IMaterial material(obj, Abc::kWrapExisting);
    m_schema = material.getSchema();
}
