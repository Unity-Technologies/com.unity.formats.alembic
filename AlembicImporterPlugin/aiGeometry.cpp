#include "pch.h"
#include "AlembicImporter.h"
#include "aiGeometry.h"
#include "aiObject.h"


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
    float ret = float(m_sample.getAngle());
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



aiPolyMesh::aiPolyMesh() {}

aiPolyMesh::aiPolyMesh(aiObject *obj)
    : super(obj)
{
    AbcGeom::IPolyMesh pm(obj->getAbcObject(), Abc::kWrapExisting);
    m_schema = pm.getSchema();
}

void aiPolyMesh::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());

    bool hasValidTopology = (m_counts && m_indices);

    if (m_schema.isConstant())
    {
        if (hasValidTopology)
        {
            // If have have a valid topology, we'll have read at least the positions too
            return;
        }
    }
    
    // only sample topology if we don't already have valid topology data or the mesh has varying topology
    if (!hasValidTopology || m_schema.getTopologyVariance() == AbcGeom::kHomogeneousTopology)
    {
        m_schema.getFaceIndicesProperty().get(m_indices, ss);
        m_schema.getFaceCountsProperty().get(m_counts, ss);

        // invalidate normals and uvs
        m_normals.reset();
        m_uvs.reset();
    }

    // positions and velocities change with time if shema is not constant
    m_schema.getPositionsProperty().get(m_positions, ss);

    if (m_schema.getVelocitiesProperty().valid())
    {
        m_schema.getVelocitiesProperty().get(m_velocities, ss);
    }

    // normals and uvs sampling may differ from that of the schema
    auto nparam = m_schema.getNormalsParam();
    if (nparam.valid() &&
        (nparam.getScope() == AbcGeom::kFacevaryingScope ||
         nparam.getScope() == AbcGeom::kVaryingScope ||
         nparam.getScope() == AbcGeom::kVertexScope))
    {
        // do not re-read normals if sampling is constant and we already have valid normals
        if (!m_normals.valid() || !nparam.isConstant())
        {
            nparam.getIndexed(m_normals, ss);
        }
    }

    auto uvparam = m_schema.getUVsParam();
    if (uvparam.valid() &&
        uvparam.getScope() == AbcGeom::kFacevaryingScope)
    {
        // do not re-read uvs if sampling is constant and we already have valid uvs
        if (!m_uvs.valid() || !uvparam.isConstant())
        {
            uvparam.getIndexed(m_uvs, ss);
        }
    }
}

bool aiPolyMesh::isTopologyConstant() const
{
    return m_schema.isConstant();
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

uint32_t aiPolyMesh::getIndexCount() const
{
    if (m_obj->getTriangulate())
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
        return uint32_t(m_indices->size());
    }
}

uint32_t aiPolyMesh::getVertexCount() const
{
    return uint32_t(m_positions->size());
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

void aiPolyMesh::copyVertices(abcV3 *dst) const
{
    const auto &cont = *m_positions;
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

void aiPolyMesh::copyNormals(abcV3 *) const
{
    // todo
}

void aiPolyMesh::copyUVs(abcV2 *) const
{
    // todo
}

bool aiPolyMesh::getSplitedMeshInfo(aiSplitedMeshInfo &o_smi, const aiSplitedMeshInfo& prev, int max_vertices) const
{
    const auto &counts = *m_counts;
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
    for (int fi = 0; fi < smi.num_faces; ++fi) {
        int ngon = counts[smi.begin_face + fi];
        for (int ni = 0; ni < ngon; ++ni) {
            dst[a + ni] = normals[indices[a + ni + smi.begin_index]];
        }
        a += ngon;
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

    uint32_t a = 0;
    for (int fi = 0; fi < smi.num_faces; ++fi) {
        int ngon = counts[smi.begin_face + fi];
        for (int ni = 0; ni < ngon; ++ni) {
            dst[a + ni] = uvs[indices[a + ni + smi.begin_index]];
        }
        a += ngon;
    }
}

const aiPolyMesh::UVTileID aiPolyMesh::InvalidUVTileID = 99999999999;

aiPolyMesh::UVTileID aiPolyMesh::uvTileID(float u, float v) const
{
    UVTileID utile = UVTileID(floor(u));
    UVTileID vtile = UVTileID(floor(v));

    // i've never seen a mesh with a thousand tile in V direction
    return (1000 * utile + vtile);
}

bool aiPolyMesh::fillUVTileFacesets(std::vector<aiPolyMesh::UVTileID> &face_uvtile)
{
    face_uvtile.clear();

    if (m_uvs.valid())
    {
        const Abc::Int32ArraySample &counts = *m_counts;
        const Abc::V2fArraySample &uv_values = *(m_uvs.getVals());
        const Abc::UInt32ArraySample &uv_indices = *(m_uvs.getIndices());

        face_uvtile.resize(counts.size(), InvalidUVTileID);

        for (size_t i=0, fv=0; i<counts.size(); ++i)
        {
            int nv = counts[i];

            if (nv > 0)
            {
                float u_acc = 0.0f;
                float v_acc = 0.0f;
                float inv_nv = 1.0f / float(nv);

                for (int v=0; v<nv; ++v, ++fv)
                {
                    Abc::V2f uv = uv_values[uv_indices[fv]];
                    u_acc += uv.x;
                    v_acc += uv.y;
                }

                UVTileID tid = uvTileID(u_acc * inv_nv, v_acc * inv_nv);
                
                face_uvtile[i] = tid;
            }
        }

        return true;
    }
    else
    {
        return false;
    }
}

void aiPolyMesh::prepareSubmeshes(int max_vertices)
{
    // Build face sets based on UV tiles

    std::vector<UVTileID> face_uvtile;

    if (!fillUVTileFacesets(face_uvtile))
    {
        face_uvtile.resize(m_counts->size(), InvalidUVTileID);
    }

    // Create a submesh for each face set (also splitting based on vertex count)

    IndexPassthrough index_passthrough;

    if (m_normals.valid())
    {
        if (m_normals.getScope() == AbcGeom::kFacevaryingScope)
        {
            if (m_uvs.valid())
            {
                fillSubmeshIndices(face_uvtile, *(m_normals.getIndices()), *(m_uvs.getIndices()), size_t(max_vertices));
            }
            else
            {
                fillSubmeshIndices(face_uvtile, *(m_normals.getIndices()), index_passthrough, size_t(max_vertices));
            }
        }
        else
        {
            if (m_uvs.valid())
            {
                fillSubmeshIndices(face_uvtile, *m_indices, *(m_uvs.getIndices()), size_t(max_vertices));
            }
            else
            {
                fillSubmeshIndices(face_uvtile, *m_indices, index_passthrough, size_t(max_vertices));
            }
        }
    }
    else
    {
        if (m_uvs.valid())
        {
            fillSubmeshIndices(face_uvtile, index_passthrough, *(m_uvs.getIndices()), size_t(max_vertices));
        }
        else
        {
            fillSubmeshIndices(face_uvtile, index_passthrough, index_passthrough, size_t(max_vertices));
        }
    }

    // Optimize allocated memory

    for (size_t i=0; i<m_submeshes.size(); ++i)
    {
        m_submeshes[i].vertex_indices.shrink_to_fit();
        
        if (m_normals.valid())
        {
            m_submeshes[i].normal_indices.shrink_to_fit();
        }
        else
        {
            m_submeshes[i].normal_indices.clear();
        }

        if (m_uvs.valid())
        {
            m_submeshes[i].uv_indices.shrink_to_fit();
        }
        else
        {
            m_submeshes[i].uv_indices.clear();
        }
    }

    m_cur_submesh = m_submeshes.begin();
}

bool aiPolyMesh::getNextSubmesh(aiSubmeshInfo &o_smi)
{
    if (m_cur_submesh == m_submeshes.end())
    {
        return false;
    }
    else
    {
        Submesh &submesh = *m_cur_submesh;

        o_smi.index = int(m_cur_submesh - m_submeshes.begin());
        o_smi.face_count = int(submesh.faces.size());
        o_smi.vertex_count = int(submesh.vertex_indices.size());
        o_smi.triangle_count = int(submesh.triangle_count);

        ++m_cur_submesh;

        return true;
    }
}

void aiPolyMesh::copySubmeshIndices(int *dst, const aiSubmeshInfo &smi) const
{
    Submeshes::const_iterator it = m_submeshes.begin() + smi.index;
    
    if (it != m_submeshes.end())
    {
        bool reverse_index = m_obj->getReverseIndex();
        const auto &counts = *m_counts;
        const Submesh &submesh = *it;

        // Note: do not need m_indices as vertex/normals/uvs are duplicated and re-index per submesh

        int index = 0;
        int i1 = (reverse_index ? 2 : 1);
        int i2 = (reverse_index ? 1 : 2);
        int offset = 0;
        
        for (Faceset::const_iterator fit = submesh.faces.begin(); fit != submesh.faces.end(); ++fit)
        {
            int nv = counts[*fit];
            
            // Triangle faning
            int nt = nv - 2;
            for (int ti = 0; ti < nt; ++it)
            {
                dst[offset + 0] = index;
                dst[offset + 1] = index + ti + i1;
                dst[offset + 2] = index + ti + i2;
                offset += 3;
            }

            index += nv;
        }
    }
}

void aiPolyMesh::copySubmeshVertices(abcV3 *dst, const aiSubmeshInfo &smi) const
{
    Submeshes::const_iterator it = m_submeshes.begin() + smi.index;
    
    if (it != m_submeshes.end())
    {
        const auto &counts = *m_counts;
        const auto &positions = *m_positions;
        const Submesh &submesh = *it;

        size_t offset = 0;
        float x_scale = (m_obj->getReverseX() ? -1.0f : 1.0f);

        for (Faceset::const_iterator fit = submesh.faces.begin(); fit != submesh.faces.end(); ++fit)
        {
            int nv = counts[*fit];

            for (int fv = 0; fv < nv; ++fv, ++offset)
            {
                dst[offset] = positions[submesh.vertex_indices[offset]];
                dst[offset].x *= x_scale;
            }
        }
    }
}

void aiPolyMesh::copySubmeshNormals(abcV3 *dst, const aiSubmeshInfo &smi) const
{
    Submeshes::const_iterator it = m_submeshes.begin() + smi.index;
    
    if (it != m_submeshes.end())
    {
        const auto &counts = *m_counts;
        const auto &normals = *m_normals.getVals();
        const Submesh &submesh = *it;

        size_t offset = 0;
        float x_scale = (m_obj->getReverseX() ? -1.0f : 1.0f);

        for (Faceset::const_iterator fit = submesh.faces.begin(); fit != submesh.faces.end(); ++fit)
        {
            int nv = counts[*fit];

            for (int fv = 0; fv < nv; ++fv, ++offset)
            {
                dst[offset] = normals[submesh.normal_indices[offset]];
                dst[offset].x *= x_scale;
            }
        }
    }
}

void aiPolyMesh::copySubmeshUVs(abcV2 *dst, const aiSubmeshInfo &smi) const
{
    Submeshes::const_iterator it = m_submeshes.begin() + smi.index;
    
    if (it != m_submeshes.end())
    {
        const auto &counts = *m_counts;
        const auto &uvs = *m_uvs.getVals();
        const Submesh &submesh = *it;

        size_t offset = 0;

        for (Faceset::const_iterator fit = submesh.faces.begin(); fit != submesh.faces.end(); ++fit)
        {
            int nv = counts[*fit];

            for (int fv = 0; fv < nv; ++fv, ++offset)
            {
                dst[offset] = uvs[submesh.uv_indices[offset]];
            }
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
