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
    bool reverseIndex = m_obj->getReverseIndex();
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;

    if (m_obj->getTriangulate())
    {
        uint32_t a = 0;
        uint32_t b = 0;
        uint32_t i1 = reverseIndex ? 2 : 1;
        uint32_t i2 = reverseIndex ? 1 : 2;
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
        if (reverseIndex) {
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

bool aiPolyMesh::getSplitedMeshInfo(aiSplitedMeshInfo &outSmi, const aiSplitedMeshInfo& prev, int maxVertices) const
{
    const auto &counts = *m_counts;
    size_t nc = counts.size();

    aiSplitedMeshInfo smi = { 0 };
    smi.beginFace = prev.beginFace + prev.numFaces;
    smi.beginIndex = prev.beginIndex + prev.numIndices;

    bool isEnd = true;
    int a = 0;
    for (size_t i = smi.beginFace; i < nc; ++i) {
        int ngon = counts[i];
        if (a + ngon >= maxVertices) {
            isEnd = false;
            break;
        }

        a += ngon;
        smi.numFaces++;
        smi.numIndices = a;
        smi.triangulatedIndexCount += (ngon - 2) * 3;
    }

    smi.numVertices = a;
    outSmi = smi;
    return isEnd;
}

void aiPolyMesh::copySplitedIndices(int *dst, const aiSplitedMeshInfo &smi) const
{
    bool reverseIndex = m_obj->getReverseIndex();
    const auto &counts = *m_counts;

    uint32_t a = 0;
    uint32_t b = 0;
    uint32_t i1 = reverseIndex ? 2 : 1;
    uint32_t i2 = reverseIndex ? 1 : 2;
    for (int fi = 0; fi < smi.numFaces; ++fi) {
        int ngon = counts[smi.beginFace + fi];
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
    for (int fi = 0; fi < smi.numFaces; ++fi) {
        int ngon = counts[smi.beginFace + fi];
        for (int ni = 0; ni < ngon; ++ni) {
            dst[a + ni] = positions[indices[a + ni + smi.beginIndex]];
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
    float xScale = (m_obj->getReverseX() ? -1.0f : 1.0f);
    
    if (m_normals.getScope() == AbcGeom::kFacevaryingScope)
    {
        copySplitedNormals(dst, *(m_normals.getIndices()), smi, xScale);
    }
    else
    {
        copySplitedNormals(dst, *m_indices, smi, xScale);
    }
}

void aiPolyMesh::copySplitedUVs(abcV2 *dst, const aiSplitedMeshInfo &smi) const
{
    const auto &counts = *m_counts;
    const auto &uvs = *m_uvs.getVals();
    const auto &indices = *m_uvs.getIndices();

    uint32_t a = 0;
    for (int fi = 0; fi < smi.numFaces; ++fi) {
        int ngon = counts[smi.beginFace + fi];
        for (int ni = 0; ni < ngon; ++ni) {
            dst[a + ni] = uvs[indices[a + ni + smi.beginIndex]];
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
        if (m_faceSplitIndices.size() != m_counts->size() || force_refresh)
        {
            updateSplits();
        }
    }
    else
    {
        m_splits.clear();
        m_faceSplitIndices.clear();
    }

    return (uint32_t) m_splits.size();
}

void aiPolyMesh::updateSplits()
{
    const auto &counts = *m_counts;

    m_faceSplitIndices.resize(counts.size());

    m_splits.clear();
    m_splits.reserve(1 + m_indices->size() / 65000);

    int splitIndex = 0;
    size_t ncounts = counts.size();
    size_t indexOffset = 0;

    m_splits.push_back(SplitInfo());

    SplitInfo *curSplit = &(m_splits.back());
    
    for (size_t i=0; i<ncounts; ++i)
    {
        size_t nv = (size_t) counts[i];

        if (curSplit->indicesCount + nv > 65000)
        {
            m_splits.push_back(SplitInfo(i, indexOffset));
            
            ++splitIndex;

            curSplit = &(m_splits.back());
        }
        
        m_faceSplitIndices[i] = splitIndex;

        curSplit->lastFace = i;
        curSplit->indicesCount += nv;

        indexOffset += nv;
    }
}

uint32_t aiPolyMesh::getVertexBufferLength(uint32_t splitIndex) const
{
    if (splitIndex >= m_splits.size())
    {
        return 0;
    }
    else
    {
        return (uint32_t) m_splits[splitIndex].indicesCount;
    }
}

void aiPolyMesh::fillVertexBuffer(uint32_t splitIndex, abcV3 *P, abcV3 *N, abcV2 *UV) const
{
    if (splitIndex >= m_splits.size())
    {
        return;
    }

    bool copyNormals = (m_normals.valid() && N);
    bool copyUvs = (m_uvs.valid() && UV);
    float xScale = (m_obj->getReverseX() ? -1.0f : 1.0f);

    const SplitInfo &split = m_splits[splitIndex];

    const auto &counts = *m_counts;
    const auto &indices = *m_indices;
    const auto &positions = *m_positions;

    size_t k = 0;
    size_t o = split.indexOffset;

    if (copyNormals && copyUvs)
    {
        const auto &normals = *(m_normals.getVals());
        const auto &uvs = *(m_uvs.getVals());
        const auto &uvIndices = *(m_uvs.getIndices());

        if (m_normals.getScope() == AbcGeom::kFacevaryingScope)
        {
            const auto &nIndices = *(m_normals.getIndices());
            
            for (size_t i=split.firstFace; i<=split.lastFace; ++i)
            {
                int nv = counts[i];
                for (int j = 0; j < nv; ++j, ++o, ++k)
                {
                    P[k] = positions[indices[o]];
                    P[k].x *= xScale;
                    N[k] = normals[nIndices[o]];
                    N[k].x *= xScale;
                    UV[k] = uvs[uvIndices[o]];
                }
            }
        }
        else
        {
            const auto &nIndices = *m_indices;
            
            for (size_t i=split.firstFace; i<=split.lastFace; ++i)
            {
                int nv = counts[i];
                for (int j = 0; j < nv; ++j, ++o, ++k)
                {
                    P[k] = positions[indices[o]];
                    P[k].x *= xScale;
                    N[k] = normals[nIndices[o]];
                    N[k].x *= xScale;
                    UV[k] = uvs[uvIndices[o]];
                }
            }
        }
    }
    else if (copyNormals)
    {
        const auto &normals = *(m_normals.getVals());

        if (m_normals.getScope() == AbcGeom::kFacevaryingScope)
        {
            const auto &nIndices = *(m_normals.getIndices());
            
            for (size_t i=split.firstFace; i<=split.lastFace; ++i)
            {
                int nv = counts[i];
                for (int j = 0; j < nv; ++j, ++o, ++k)
                {
                    P[k] = positions[indices[o]];
                    P[k].x *= xScale;
                    N[k] = normals[nIndices[o]];
                    N[k].x *= xScale;
                }
            }
        }
        else
        {
            const auto &nIndices = *m_indices;
            
            for (size_t i=split.firstFace; i<=split.lastFace; ++i)
            {
                int nv = counts[i];
                for (int j = 0; j < nv; ++j, ++o, ++k)
                {
                    P[k] = positions[indices[o]];
                    P[k].x *= xScale;
                    N[k] = normals[nIndices[o]];
                    N[k].x *= xScale;
                }
            }
        }
    }
    else if (copyUvs)
    {
        const auto &uvs = *(m_uvs.getVals());
        const auto &uvIndices = *(m_uvs.getIndices());

        for (size_t i=split.firstFace; i<=split.lastFace; ++i)
        {
            int nv = counts[i];
            for (int j = 0; j < nv; ++j, ++o, ++k)
            {
                P[k] = positions[indices[o]];
                P[k].x *= xScale;
                UV[k] = uvs[uvIndices[o]];
            }
        }
    }
    else
    {
        for (size_t i=split.firstFace; i<=split.lastFace; ++i)
        {
            int nv = counts[i];
            for (int j = 0; j < nv; ++j, ++o, ++k)
            {
                P[k] = positions[indices[o]];
                P[k].x *= xScale;
            }
        }
    }
}

uint32_t aiPolyMesh::prepareSubmeshes(const aiFacesets *inFacesets)
{
    const auto &counts = *m_counts;

    Facesets facesets;
    std::map<size_t, int> facesetIndices;
    
    m_submeshes.clear();

    if (inFacesets && inFacesets->count > 0)
    {
        size_t index = 0;
        int defaultFacesetIndex = -1;

        facesets.resize(inFacesets->count);

        for (int i=0; i<inFacesets->count; ++i)
        {
            Faceset &faceset = facesets[i];

            if (inFacesets->faceCounts[i] == 0)
            {
                defaultFacesetIndex = i;
            }
            else
            {
                for (int j=0; j<inFacesets->faceCounts[i]; ++j)
                {
                    size_t f = size_t(inFacesets->faceIndices[index++]);

                    faceset.insert(f);

                    facesetIndices[f] = i;
                }
            }
        }

        for (size_t i=0; i<counts.size(); ++i)
        {
            if (facesetIndices.find(i) == facesetIndices.end())
            {
                facesetIndices[i] = defaultFacesetIndex;
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

                facesetIndices[i] = -1;
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
            submesh.triangleCount += (counts[i] - 2);
        }

        m_splits[0].submeshCount = 1;
    }
    else
    {
        int vertexIndex = 0;
        Submesh *curMesh = 0;
        const Util::uint32_t *uvIndices = 0;
        const Abc::V2f *uvValues = 0;

        const auto &indices = *m_indices;
        
        if (m_uvs.valid())
        {
            uvValues = m_uvs.getVals()->get();
            uvIndices = m_uvs.getIndices()->get();
        }

        std::map<SubmeshID, size_t> submeshIndices;
        std::map<SubmeshID, size_t>::iterator submeshIndexIt;

        std::vector<int> splitSubmeshIndices(nsplits, 0);

        for (size_t i=0; i<counts.size(); ++i)
        {
            int nv = counts[i];
            
            if (nv == 0)
            {
                continue;
            }

            int facesetIndex = facesetIndices[i];
            int splitIndex = m_faceSplitIndices[i];

            SplitInfo &split = m_splits[splitIndex];

            // Compute submesh ID based on face's average UV coordinate and it faceset index
            float uAcc = 0.0f;
            float vAcc = 0.0f;
            float invNv = 1.0f / float(nv);

            if (uvValues)
            {
                for (int j=0; j<nv; ++j)
                {
                    Abc::V2f uv = uvValues[uvIndices[vertexIndex + j]];
                    uAcc += uv.x;
                    vAcc += uv.y;
                }
            }

            SubmeshID sid(uAcc * invNv, vAcc * invNv, facesetIndex, splitIndex);

            submeshIndexIt = submeshIndices.find(sid);

            if (submeshIndexIt == submeshIndices.end())
            {
                submeshIndices[sid] = m_submeshes.size();

                m_submeshes.push_back(Submesh(facesetIndex, splitIndex));

                curMesh = &(m_submeshes.back());

                curMesh->index = splitSubmeshIndices[splitIndex]++;
                curMesh->vertexIndices.reserve(indices.size());

                split.submeshCount = splitSubmeshIndices[splitIndex];
            }
            else
            {
                curMesh = &(m_submeshes[submeshIndexIt->second]);
            }

            curMesh->faces.insert(i);
            curMesh->triangleCount += (nv - 2);

            for (int j=0; j<nv; ++j, ++vertexIndex)
            {
                curMesh->vertexIndices.push_back(vertexIndex - split.indexOffset);
            }
        }

        for (size_t i=0; i<m_submeshes.size(); ++i)
        {
            m_submeshes[i].vertexIndices.shrink_to_fit();
        }
    }

    m_curSubmesh = m_submeshes.begin();

    return (uint32_t) m_submeshes.size();
}

uint32_t aiPolyMesh::getSplitSubmeshCount(uint32_t splitIndex) const
{
    if (splitIndex >= m_splits.size())
    {
        return 0;
    }
    else
    {
        return (uint32_t) m_splits[splitIndex].submeshCount;
    }
}

bool aiPolyMesh::getNextSubmesh(aiSubmeshInfo &smi)
{
    if (m_curSubmesh == m_submeshes.end())
    {
        return false;
    }
    else
    {
        Submesh &submesh = *m_curSubmesh;

        smi.index = int(m_curSubmesh - m_submeshes.begin());
        smi.splitIndex = submesh.splitIndex;
        smi.splitSubmeshIndex = submesh.index;
        smi.facesetIndex = submesh.facesetIndex;
        smi.triangleCount = int(submesh.triangleCount);

        ++m_curSubmesh;

        return true;
    }
}

void aiPolyMesh::fillSubmeshIndices(int *dst, const aiSubmeshInfo &smi) const
{
    Submeshes::const_iterator it = m_submeshes.begin() + smi.index;
    
    if (it != m_submeshes.end())
    {
        bool reverseIndex = m_obj->getReverseIndex();
        const auto &counts = *m_counts;
        const Submesh &submesh = *it;

        int index = 0;
        int i1 = (reverseIndex ? 2 : 1);
        int i2 = (reverseIndex ? 1 : 2);
        int offset = 0;
        
        if (submesh.faces.size() == 0 && submesh.vertexIndices.size() == 0)
        {
            // single submesh case, faces and vertexIndices not populated

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
                    dst[offset + 0] = submesh.vertexIndices[index];
                    dst[offset + 1] = submesh.vertexIndices[index + ti + i1];
                    dst[offset + 2] = submesh.vertexIndices[index + ti + i2];
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

void aiCamera::getParams(aiCameraParams &params)
{
    static float sRad2Deg = 180.0f / float(M_PI);

    float verticalAperture = (float) m_sample.getVerticalAperture();
    float focalLength = (float) m_sample.getFocalLength();

    params.nearClippingPlane = (float) m_sample.getNearClippingPlane();
    params.farClippingPlane = (float) m_sample.getFarClippingPlane();
    // CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    params.fieldOfView = 2.0f * atanf(verticalAperture * 10.0f / (2.0f * focalLength)) * sRad2Deg;
    params.focusDistance = (float) m_sample.getFocusDistance() * 0.1f; // centimeter to meter
    params.focalLength = focalLength * 0.01f; // milimeter to meter
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
