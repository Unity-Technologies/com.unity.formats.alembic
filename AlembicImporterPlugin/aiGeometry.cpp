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
    if (!hasValidTopology || m_schema.getTopologyVariance() == AbcGeom::kHeterogeneousTopology)
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

int aiPolyMesh::getTopologyVariance() const
{
    return (int) m_schema.getTopologyVariance();
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
    float x_scale = (m_obj->getReverseX() ? -1.0f : 1.0f);
    
    if (m_normals.getScope() == AbcGeom::kFacevaryingScope)
    {
        copySplitedNormals(dst, *(m_normals.getIndices()), smi, x_scale);
    }
    else
    {
        copySplitedNormals(dst, *m_indices, smi, x_scale);
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

uint32_t aiPolyMesh::getSplitCount() const
{
    return (uint32_t) m_splits.size();
}

uint32_t aiPolyMesh::getSplitCount(bool force_refresh)
{
    if (m_counts && m_indices)
    {
        if (m_face_split_indices.size() != m_counts->size() || force_refresh)
        {
            updateSplits();
        }
    }
    else
    {
        m_splits.clear();
        m_face_split_indices.clear();
    }

    return (uint32_t) m_splits.size();
}

void aiPolyMesh::updateSplits()
{
    const auto &counts = *m_counts;

    m_face_split_indices.resize(counts.size());

    m_splits.clear();
    m_splits.reserve(1 + m_indices->size() / 65000);

    int split_index = 0;
    size_t ncounts = counts.size();
    size_t index_offset = 0;

    m_splits.push_back(SplitInfo());

    SplitInfo *cur_split = &(m_splits.back());
    
    for (size_t i=0; i<ncounts; ++i)
    {
        size_t nv = (size_t) counts[i];

        if (cur_split->indices_count + nv > 65000)
        {
            m_splits.push_back(SplitInfo(i, index_offset));
            
            ++split_index;

            cur_split = &(m_splits.back());
        }
        
        m_face_split_indices[i] = split_index;

        cur_split->last_face = i;
        cur_split->indices_count += nv;

        index_offset += nv;
    }
}

uint32_t aiPolyMesh::getVertexBufferLength(uint32_t split_index) const
{
    if (split_index >= m_splits.size())
    {
        return 0;
    }
    else
    {
        return (uint32_t) m_splits[split_index].indices_count;
    }
}

void aiPolyMesh::fillVertexBuffer(uint32_t split_index, abcV3 *P, abcV3 *N, abcV2 *UV) const
{
    if (split_index >= m_splits.size())
    {
        return;
    }

    bool copy_normals = (m_normals.valid() && N);
    bool copy_uvs = (m_uvs.valid() && UV);
    float x_scale = (m_obj->getReverseX() ? -1.0f : 1.0f);

    const SplitInfo &split = m_splits[split_index];

    const auto &counts = *m_counts;
    const auto &indices = *m_indices;
    const auto &positions = *m_positions;

    size_t k = 0;
    size_t o = split.index_offset;

    if (copy_normals && copy_uvs)
    {
        const auto &normals = *(m_normals.getVals());
        const auto &uvs = *(m_uvs.getVals());
        const auto &uv_indices = *(m_uvs.getIndices());

        if (m_normals.getScope() == AbcGeom::kFacevaryingScope)
        {
            const auto &n_indices = *(m_normals.getIndices());
            
            for (size_t i=split.first_face; i<=split.last_face; ++i)
            {
                int nv = counts[i];
                for (int j = 0; j < nv; ++j, ++o, ++k)
                {
                    P[k] = positions[indices[o]];
                    P[k].x *= x_scale;
                    N[k] = normals[n_indices[o]];
                    N[k].x *= x_scale;
                    UV[k] = uvs[uv_indices[o]];
                }
            }
        }
        else
        {
            const auto &n_indices = *m_indices;
            
            for (size_t i=split.first_face; i<=split.last_face; ++i)
            {
                int nv = counts[i];
                for (int j = 0; j < nv; ++j, ++o, ++k)
                {
                    P[k] = positions[indices[o]];
                    P[k].x *= x_scale;
                    N[k] = normals[n_indices[o]];
                    N[k].x *= x_scale;
                    UV[k] = uvs[uv_indices[o]];
                }
            }
        }
    }
    else if (copy_normals)
    {
        const auto &normals = *(m_normals.getVals());

        if (m_normals.getScope() == AbcGeom::kFacevaryingScope)
        {
            const auto &n_indices = *(m_normals.getIndices());
            
            for (size_t i=split.first_face; i<=split.last_face; ++i)
            {
                int nv = counts[i];
                for (int j = 0; j < nv; ++j, ++o, ++k)
                {
                    P[k] = positions[indices[o]];
                    P[k].x *= x_scale;
                    N[k] = normals[n_indices[o]];
                    N[k].x *= x_scale;
                }
            }
        }
        else
        {
            const auto &n_indices = *m_indices;
            
            for (size_t i=split.first_face; i<=split.last_face; ++i)
            {
                int nv = counts[i];
                for (int j = 0; j < nv; ++j, ++o, ++k)
                {
                    P[k] = positions[indices[o]];
                    P[k].x *= x_scale;
                    N[k] = normals[n_indices[o]];
                    N[k].x *= x_scale;
                }
            }
        }
    }
    else if (copy_uvs)
    {
        const auto &uvs = *(m_uvs.getVals());
        const auto &uv_indices = *(m_uvs.getIndices());

        for (size_t i=split.first_face; i<=split.last_face; ++i)
        {
            int nv = counts[i];
            for (int j = 0; j < nv; ++j, ++o, ++k)
            {
                P[k] = positions[indices[o]];
                P[k].x *= x_scale;
                UV[k] = uvs[uv_indices[o]];
            }
        }
    }
    else
    {
        for (size_t i=split.first_face; i<=split.last_face; ++i)
        {
            int nv = counts[i];
            for (int j = 0; j < nv; ++j, ++o, ++k)
            {
                P[k] = positions[indices[o]];
                P[k].x *= x_scale;
            }
        }
    }
}

uint32_t aiPolyMesh::prepareSubmeshes(const aiFacesets *in_facesets)
{
    const auto &counts = *m_counts;

    Facesets facesets;
    std::map<size_t, int> faceset_indices;
    
    m_submeshes.clear();

    if (in_facesets && in_facesets->count > 0)
    {
        size_t index = 0;
        int default_faceset_index = -1;

        facesets.resize(in_facesets->count);

        for (int i=0; i<in_facesets->count; ++i)
        {
            Faceset &faceset = facesets[i];

            if (in_facesets->face_counts[i] == 0)
            {
                default_faceset_index = i;
            }
            else
            {
                for (int j=0; j<in_facesets->face_counts[i]; ++j)
                {
                    size_t f = size_t(in_facesets->face_indices[index++]);

                    faceset.insert(f);

                    faceset_indices[f] = i;
                }
            }
        }

        for (size_t i=0; i<counts.size(); ++i)
        {
            if (faceset_indices.find(i) == faceset_indices.end())
            {
                faceset_indices[i] = default_faceset_index;
            }
        }
    }
    else
    {
        // don't even fill faceset if we have no UVs to tile split the mesh
        if (m_uvs.valid())
        {
            facesets.resize(1);
            
            Faceset &faceset = facesets[0];
            
            for (size_t i=0; i<counts.size(); ++i)
            {
                faceset.insert(i);

                faceset_indices[i] = -1;
            }
        }
    }

    uint32_t nsplits = getSplitCount(false);

    if (facesets.size() == 0 && nsplits == 1)
    {
        // no facesets, no uvs, no splits
        m_submeshes.push_back(Submesh());
        
        Submesh &submesh = m_submeshes.back();

        for (size_t i=0; i<counts.size(); ++i)
        {
            submesh.triangle_count += (counts[i] - 2);
        }

        m_splits[0].submesh_count = 1;
    }
    else
    {
        int vertex_index = 0;
        Submesh *cur_mesh = 0;
        const Util::uint32_t *uv_indices = 0;
        const Abc::V2f *uv_values = 0;

        const auto &indices = *m_indices;
        
        if (m_uvs.valid())
        {
            uv_values = m_uvs.getVals()->get();
            uv_indices = m_uvs.getIndices()->get();
        }

        std::map<SubmeshID, size_t> submesh_indices;
        std::map<SubmeshID, size_t>::iterator submesh_index_it;

        std::vector<int> split_submesh_indices(nsplits, 0);

        for (size_t i=0; i<counts.size(); ++i)
        {
            int nv = counts[i];
            
            if (nv == 0)
            {
                continue;
            }

            int faceset_index = faceset_indices[i];
            int split_index = m_face_split_indices[i];

            SplitInfo &split = m_splits[split_index];

            // Compute submesh ID based on face's average UV coordinate and it faceset index
            float u_acc = 0.0f;
            float v_acc = 0.0f;
            float inv_nv = 1.0f / float(nv);

            if (uv_values)
            {
                for (int j=0; j<nv; ++j)
                {
                    Abc::V2f uv = uv_values[uv_indices[vertex_index + j]];
                    u_acc += uv.x;
                    v_acc += uv.y;
                }
            }

            SubmeshID sid(u_acc * inv_nv, v_acc * inv_nv, faceset_index, split_index);

            submesh_index_it = submesh_indices.find(sid);

            if (submesh_index_it == submesh_indices.end())
            {
                submesh_indices[sid] = m_submeshes.size();

                m_submeshes.push_back(Submesh(faceset_index, split_index));

                cur_mesh = &(m_submeshes.back());

                cur_mesh->index = split_submesh_indices[split_index]++;
                cur_mesh->vertex_indices.reserve(indices.size());

                split.submesh_count = split_submesh_indices[split_index];
            }
            else
            {
                cur_mesh = &(m_submeshes[submesh_index_it->second]);
            }

            cur_mesh->faces.insert(i);
            cur_mesh->triangle_count += (nv - 2);

            for (int j=0; j<nv; ++j, ++vertex_index)
            {
                cur_mesh->vertex_indices.push_back(vertex_index - split.index_offset);
            }
        }

        for (size_t i=0; i<m_submeshes.size(); ++i)
        {
            m_submeshes[i].vertex_indices.shrink_to_fit();
        }
    }

    m_cur_submesh = m_submeshes.begin();

    return (uint32_t) m_submeshes.size();
}

uint32_t aiPolyMesh::getSplitSubmeshCount(uint32_t split_index) const
{
    if (split_index >= m_splits.size())
    {
        return 0;
    }
    else
    {
        return (uint32_t) m_splits[split_index].submesh_count;
    }
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
        o_smi.split_index = submesh.split_index;
        o_smi.split_submesh_index = submesh.index;
        o_smi.faceset_index = submesh.faceset_index;
        o_smi.triangle_count = int(submesh.triangle_count);

        ++m_cur_submesh;

        return true;
    }
}

void aiPolyMesh::fillSubmeshIndices(int *dst, const aiSubmeshInfo &smi) const
{
    Submeshes::const_iterator it = m_submeshes.begin() + smi.index;
    
    if (it != m_submeshes.end())
    {
        bool reverse_index = m_obj->getReverseIndex();
        const auto &counts = *m_counts;
        const Submesh &submesh = *it;

        int index = 0;
        int i1 = (reverse_index ? 2 : 1);
        int i2 = (reverse_index ? 1 : 2);
        int offset = 0;
        
        if (submesh.faces.size() == 0 && submesh.vertex_indices.size() == 0)
        {
            // single submesh case, faces and vertex_indices not populated

            for (size_t i=0; i<counts.size(); ++i)
            {
                int nv = counts[i];
                
                int nt = nv - 2;
                for (int ti=0; ti<nt; ++ti)
                {
                    dst[offset + 0] = index;
                    dst[offset + 1] = index + ti + i1;
                    dst[offset + 2] = index + ti + i2;
                    offset += 3;
                }

                index += nv;
            }
        }
        else
        {
            for (Faceset::const_iterator fit = submesh.faces.begin(); fit != submesh.faces.end(); ++fit)
            {
                int nv = counts[*fit];
                
                // Triangle faning
                int nt = nv - 2;
                for (int ti = 0; ti < nt; ++ti)
                {
                    dst[offset + 0] = submesh.vertex_indices[index];
                    dst[offset + 1] = submesh.vertex_indices[index + ti + i1];
                    dst[offset + 2] = submesh.vertex_indices[index + ti + i2];
                    offset += 3;
                }

                index += nv;
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
}

void aiCamera::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());

    if (!m_schema.valid())
    {
        AbcGeom::ICamera cam(m_obj->getAbcObject(), Abc::kWrapExisting);
        m_schema = cam.getSchema();
    }
    else if (m_schema.isConstant())
    {
        return;
    }

    m_schema.get(m_sample, ss);
}

void aiCamera::getParams(aiCameraParams &o_params)
{
    static float sRad2Deg = 180.0f / float(M_PI);

    float vertical_aperture = (float) m_sample.getVerticalAperture();
    float focal_length = (float) m_sample.getFocalLength();

    o_params.near_clipping_plane = (float) m_sample.getNearClippingPlane();
    o_params.far_clipping_plane = (float) m_sample.getFarClippingPlane();
    // CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    o_params.field_of_view = 2.0f * atanf(vertical_aperture * 10.0f / (2.0f * focal_length)) * sRad2Deg;
    o_params.focus_distance = (float) m_sample.getFocusDistance() * 0.1f; // centimeter to meter
    o_params.focal_length = focal_length * 0.01f; // milimeter to meter
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
