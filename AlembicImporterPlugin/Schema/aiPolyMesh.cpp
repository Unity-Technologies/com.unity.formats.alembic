#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiCamera.h"
#include "aiPolyMesh.h"
#include "aiXForm.h"

// ---

static inline int CalculateIndexCount(Abc::Int32ArraySample &counts)
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

// ---

Topology::Topology()
    : tangentIndicesCount(0)
    , tangentIndices(0)
    , tangentsCount(0)
{
    indices.reset();
    counts.reset();
}

Topology::~Topology()
{
    if (tangentIndices)
    {
        delete[] tangentIndices;
    }
}

void Topology::clear()
{
    aiLogger::Info("Topology::clear()");
    indices.reset();
    counts.reset();

    if (tangentIndices)
    {
        delete[] tangentIndices;
        tangentIndices = 0;
    }
    tangentIndicesCount = 0;
    tangentsCount = 0;

    submeshes.clear();
    faceSplitIndices.clear();
    splits.clear();
}

int Topology::getSplitCount() const
{
    return (int) splits.size();
}

int Topology::getSplitCount(bool forceRefresh)
{
    if (counts && indices)
    {
        if (faceSplitIndices.size() != counts->size() || forceRefresh)
        {
            updateSplits();
        }
    }
    else
    {
        splits.clear();
        faceSplitIndices.clear();
    }

    return (int) splits.size();
}

void Topology::updateSplits()
{
    DebugLog("Topology::updateSplits()");
    
    int splitIndex = 0;
    size_t indexOffset = 0;
    size_t ncounts = counts->size();

    faceSplitIndices.resize(ncounts);

    splits.clear();
    splits.reserve(1 + indices->size() / 65000);
    splits.push_back(SplitInfo());

    SplitInfo *curSplit = &(splits.back());
    
    for (size_t i=0; i<ncounts; ++i)
    {
        size_t nv = (size_t) counts->get()[i];

        if (curSplit->indicesCount + nv > 65000)
        {
            splits.push_back(SplitInfo(i, indexOffset));
            
            ++splitIndex;

            curSplit = &(splits.back());
        }
        
        faceSplitIndices[i] = splitIndex;

        curSplit->lastFace = i;
        curSplit->indicesCount += nv;

        indexOffset += nv;
    }
}

int Topology::getVertexBufferLength(int splitIndex) const
{
    if (splitIndex < 0 || size_t(splitIndex) >= splits.size())
    {
        return 0;
    }
    else
    {
        return (int) splits[splitIndex].indicesCount;
    }
}

int Topology::prepareSubmeshes(const AbcGeom::IV2fGeomParam::Sample &uvs,
                               const aiFacesets &inFacesets,
                               bool submeshPerUVTile)
{
    DebugLog("Topology::prepareSubmeshes()");
    
    Facesets facesets;
    std::map<size_t, int> facesetIndices;
    
    submeshes.clear();

    if (inFacesets.count > 0)
    {
        size_t index = 0;
        int defaultFacesetIndex = -1;

        facesets.resize(inFacesets.count);

        for (int i=0; i<inFacesets.count; ++i)
        {
            Faceset &faceset = facesets[i];

            if (inFacesets.faceCounts[i] == 0)
            {
                defaultFacesetIndex = i;
            }
            else
            {
                for (int j=0; j<inFacesets.faceCounts[i]; ++j)
                {
                    size_t f = size_t(inFacesets.faceIndices[index++]);

                    faceset.insert(f);

                    facesetIndices[f] = i;
                }
            }
        }

        for (size_t i=0; i<counts->size(); ++i)
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
        if (uvs.valid() && submeshPerUVTile)
        {
            facesets.resize(1);
            
            Faceset &faceset = facesets[0];
            
            for (size_t i=0; i<counts->size(); ++i)
            {
                faceset.insert(i);

                facesetIndices[i] = -1;
            }
        }
    }

    int nsplits = getSplitCount(false);

    if (facesets.size() == 0 && nsplits == 1)
    {
        // no facesets, no uvs, no splits
        submeshes.push_back(Submesh());
        
        Submesh &submesh = submeshes.back();

        for (size_t i=0; i<counts->size(); ++i)
        {
            submesh.triangleCount += (counts->get()[i] - 2);
        }

        splits[0].submeshCount = 1;
    }
    else
    {
        int vertexIndex = 0;
        Submesh *curMesh = 0;
        const Util::uint32_t *uvIndices = 0;
        const Abc::V2f *uvValues = 0;

        if (uvs.valid() && submeshPerUVTile)
        {
            uvValues = uvs.getVals()->get();
            uvIndices = uvs.getIndices()->get();
        }

        std::map<SubmeshKey, size_t> submeshIndices;
        std::map<SubmeshKey, size_t>::iterator submeshIndexIt;

        std::vector<int> splitSubmeshIndices(nsplits, 0);

        for (size_t i=0; i<counts->size(); ++i)
        {
            int nv = counts->get()[i];
            
            if (nv == 0)
            {
                continue;
            }

            int facesetIndex = facesetIndices[i];
            int splitIndex = faceSplitIndices[i];

            SplitInfo &split = splits[splitIndex];

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

            SubmeshKey sid(uAcc * invNv, vAcc * invNv, facesetIndex, splitIndex);

            submeshIndexIt = submeshIndices.find(sid);

            if (submeshIndexIt == submeshIndices.end())
            {
                submeshIndices[sid] = submeshes.size();

                submeshes.push_back(Submesh(facesetIndex, splitIndex));

                curMesh = &(submeshes.back());

                curMesh->index = splitSubmeshIndices[splitIndex]++;
                curMesh->vertexIndices.reserve(indices->size());

                split.submeshCount = splitSubmeshIndices[splitIndex];
            }
            else
            {
                curMesh = &(submeshes[submeshIndexIt->second]);
            }

            curMesh->faces.insert(i);
            curMesh->triangleCount += (nv - 2);

            for (int j=0; j<nv; ++j, ++vertexIndex)
            {
                curMesh->vertexIndices.push_back(vertexIndex - split.indexOffset);
            }
        }

        for (size_t i=0; i<submeshes.size(); ++i)
        {
            submeshes[i].vertexIndices.shrink_to_fit();
        }
    }

    return (int) submeshes.size();
}

int Topology::getSplitSubmeshCount(int splitIndex) const
{
    if (splitIndex < 0 || size_t(splitIndex) >= splits.size())
    {
        return 0;
    }
    else
    {
        return (int) splits[splitIndex].submeshCount;
    }
}

// ---

aiPolyMeshSample::aiPolyMeshSample(aiPolyMesh *schema, Topology *topo, bool ownTopo)
    : super(schema)
    , m_topology(topo)
    , m_ownTopology(ownTopo)
    , m_smoothNormalsCount(0)
    , m_smoothNormals(0)
    , m_tangentsCount(0)
    , m_tangents(0)
{
}

aiPolyMeshSample::~aiPolyMeshSample()
{
    if (m_topology && m_ownTopology)
    {
        delete m_topology;
    }

    if (m_smoothNormals)
    {
        delete[] m_smoothNormals;
    }

    if (m_tangents)
    {
        delete[] m_tangents;
    }
}

bool aiPolyMeshSample::hasNormals() const
{
    switch (m_config.normalsMode)
    {
    case NM_ReadFromFile:
        return m_normals.valid();
        break;
    case NM_Ignore:
        return false;
        break;
    default:
        return (m_normals.valid() || m_smoothNormals);
    }
}

bool aiPolyMeshSample::hasUVs() const
{
    return m_uvs.valid();
}

bool aiPolyMeshSample::hasTangents() const
{
    return (m_config.tangentsMode != TM_None && hasUVs() && m_tangents && m_topology->tangentIndices);
}

bool aiPolyMeshSample::smoothNormalsRequired() const
{
    return (m_config.normalsMode == NM_AlwaysCompute ||
            m_config.tangentsMode == TM_Smooth ||
            (!m_normals.valid() && m_config.normalsMode == NM_ComputeIfMissing));
}

bool aiPolyMeshSample::tangentsRequired() const
{
    return (m_config.tangentsMode != TM_None);
}

void aiPolyMeshSample::computeSmoothNormals(const aiConfig &config)
{
    aiLogger::Info("%s: Compute smooth normals", getSchema()->getObject()->getFullName());

    size_t smoothNormalsCount = m_positions->size();

    if (!m_smoothNormals)
    {
        m_smoothNormals = new Abc::V3f[smoothNormalsCount];
    }
    else if (m_smoothNormalsCount != smoothNormalsCount)
    {
        delete[] m_smoothNormals;
        m_smoothNormals = new Abc::V3f[smoothNormalsCount];
    }
    
    // always reset as V3f default constructor doesn't initialize its members
    memset(m_smoothNormals, 0, smoothNormalsCount * sizeof(Abc::V3f));

    m_smoothNormalsCount = smoothNormalsCount;

    const auto &counts = *(m_topology->counts);
    const auto &indices = *(m_topology->indices);
    const auto &positions = *m_positions;

    size_t nf = counts.size();
    size_t off = 0;
    bool ccw = config.swapFaceWinding;
    int ti1 = ccw ? 2 : 1;
    int ti2 = ccw ? 1 : 2;
    Abc::V3f N, dP1, dP2;

    for (size_t f=0; f<nf; ++f)
    {
        int nfv = counts[f];

        if (nfv >= 3)
        {
            // Compute average normal for current face
            N.setValue(0.0f, 0.0f, 0.0f);

            const Abc::V3f &P0 = positions[indices[off]];
            
            for (int fv=0; fv<nfv-2; ++fv)
            {
                const Abc::V3f &P1 = positions[indices[off + fv + ti1]];
                const Abc::V3f &P2 = positions[indices[off + fv + ti2]];

                dP1 = P1 - P0;
                dP2 = P2 - P0;
                
                N += dP2.cross(dP1).normalize();
            }

            if (nfv > 3)
            {
                N.normalize();
            }

            // Accumulate for all vertices participating to this face
            for (int fv=0; fv<nfv; ++fv)
            {
                m_smoothNormals[indices[off + fv]] += N;
            }
        }

        off += nfv;
    }

    // Normalize normal vectors
    for (size_t i=0; i<m_smoothNormalsCount; ++i)
    {
        m_smoothNormals[i].normalize();
    }
}

void aiPolyMeshSample::computeTangentIndices(const aiConfig &config, const Abc::V3f *inN, bool indexedNormals)
{
    aiLogger::Info("%s: Compute tangent indices...", getSchema()->getObject()->getFullName());

    const auto &counts = *(m_topology->counts);
    const auto &indices = *(m_topology->indices);
    const auto &uvVals = *(m_uvs.getVals());
    const auto &uvIdxs = *(m_uvs.getIndices());
    const Util::uint32_t *Nidxs = (indexedNormals ? m_normals.getIndices()->get() : 0);

    size_t tangentIndicesCount = indices.size();

    if (!m_topology->tangentIndices)
    {
        m_topology->tangentIndices = new int[tangentIndicesCount];
    }
    else if (m_topology->tangentIndicesCount != tangentIndicesCount)
    {
        delete[] m_topology->tangentIndices;
        m_topology->tangentIndices = new int[tangentIndicesCount];
    }

    m_topology->tangentIndicesCount = tangentIndicesCount;
    
    if (config.tangentsMode == TM_Smooth)
    {
        for (size_t i=0; i<tangentIndicesCount; ++i)
        {
            m_topology->tangentIndices[i] = indices[i];
        }

        m_topology->tangentsCount = m_positions->size();
    }
    else
    {
        TangentIndexMap uniqueIndices;
        TangentIndexMap::iterator it;

        size_t nf = counts.size();

        for (size_t f=0, v=0; f<nf; ++f)
        {
            int nfv = counts[f];
        
            for (int fv=0; fv<nfv; ++fv, ++v)
            {
                TangentKey key(inN[Nidxs ? Nidxs[v] : indices[v]], uvVals[uvIdxs[v]]);
                
                it = uniqueIndices.find(key);
                
                if (it == uniqueIndices.end())
                {
                    int idx = (int) uniqueIndices.size();
                    
                    m_topology->tangentIndices[v] = idx;
                    uniqueIndices[key] = idx;
                }
                else
                {
                    m_topology->tangentIndices[v] = it->second;
                }
            }
        }

        m_topology->tangentsCount = uniqueIndices.size(); 
    }
    
    aiLogger::Info("%lu unique tangent(s)", m_topology->tangentsCount);
}

void aiPolyMeshSample::computeTangents(const aiConfig &config, const Abc::V3f *inN, bool indexedNormals)
{
    aiLogger::Info("%s: Compute %stangents", getSchema()->getObject()->getFullName(), (config.tangentsMode == TM_Smooth ? "smooth " : ""));

    const auto &counts = *(m_topology->counts);
    const auto &indices = *(m_topology->indices);
    const auto &positions = *m_positions;
    const auto &uvVals = *(m_uvs.getVals());
    const auto &uvIdxs = *(m_uvs.getIndices());
    const Util::uint32_t *Nidxs = (indexedNormals ? m_normals.getIndices()->get() : 0);

    size_t nf = counts.size();
    size_t off = 0;
    bool ccw = config.swapFaceWinding;
    int ti1 = (ccw ? 2 : 1);
    int ti2 = (ccw ? 1 : 2);    
    size_t tangentsCount = m_topology->tangentsCount;

    if (!m_tangents)
    {
        m_tangents = new Imath::V4f[tangentsCount];
    }
    else if (m_tangentsCount != tangentsCount)
    {
        delete[] m_tangents;
        m_tangents = new Imath::V4f[tangentsCount];       
    }

    m_tangentsCount = tangentsCount;

    Abc::V3f *tan1 = new Abc::V3f[2 * tangentsCount];
    Abc::V3f *tan2 = tan1 + tangentsCount;
    int *tanNidxs = new int[tangentsCount];
    Abc::V3f T, B, dP1, dP2, tmp;
    Abc::V2f dUV1, dUV2;

    memset(tan1, 0, 2 * tangentsCount * sizeof(Abc::V3f));

    for (size_t f=0; f<nf; ++f)
    {
        int nfv = counts[f];

        if (nfv >= 3)
        {
            // reset face tangent and bitangent
            T.setValue(0.0f, 0.0f, 0.0f);
            B.setValue(0.0f, 0.0f, 0.0f);

            const Abc::V3f &P0 = positions[indices[off]];
            const Abc::V2f &UV0 = uvVals[uvIdxs[off]];

            // for each triangle making up current polygon
            for (int fv=0; fv<nfv-2; ++fv)
            {
                const Abc::V3f &P1 = positions[indices[off + fv + ti1]];
                const Abc::V3f &P2 = positions[indices[off + fv + ti2]];

                const Abc::V2f &UV1 = uvVals[uvIdxs[off + fv + ti1]];
                const Abc::V2f &UV2 = uvVals[uvIdxs[off + fv + ti2]];

                dP1 = P1 - P0;
                dP2 = P2 - P0;
                
                dUV1 = UV1 - UV0;
                dUV2 = UV2 - UV0;

                float r = dUV1.x * dUV2.y - dUV1.y * dUV2.x;

                if (r != 0.0f)
                {
                    r = 1.0f / r;
                    
                    tmp.setValue(r * (dUV2.y * dP1.x - dUV1.y * dP2.x),
                                 r * (dUV2.y * dP1.y - dUV1.y * dP2.y),
                                 r * (dUV2.y * dP1.z - dUV1.y * dP2.z));
                    tmp.normalize();
                    // accumulate face tangent
                    T += tmp;

                    tmp.setValue(r * (dUV1.x * dP2.x - dUV2.x * dP1.x),
                                 r * (dUV1.x * dP2.y - dUV2.x * dP1.y),
                                 r * (dUV1.x * dP2.z - dUV2.x * dP1.z));
                    tmp.normalize();
                    // accumulte face bitangent
                    B += tmp;
                }
            }

            // normalize face tangent and bitangent if current polygon had to be splitted
            //   into several triangles
            if (nfv > 3)
            {
                T.normalize();
                B.normalize();
            }

            // accumulate normals, tangent and bitangent for each vertex
            for (int fv=0; fv<nfv; ++fv)
            {
                int v = m_topology->tangentIndices[off + fv];
                tan1[v] += T;
                tan2[v] += B;
                tanNidxs[v] = (Nidxs ? Nidxs[off + fv] : indices[off + fv]);
            }
        }

        off += nfv;
    }

    // compute final tangent space for each point
    for (size_t i=0; i<tangentsCount; ++i)
    {
        const Abc::V3f &Nv = inN[tanNidxs[i]];
        Abc::V3f &Tv = tan1[i];
        Abc::V3f &Bv = tan2[i];

        // Normalize Tv and Bv?
        
        T = Tv - Nv * Tv.dot(Nv);
        T.normalize();

        m_tangents[i].x = T.x;
        m_tangents[i].y = T.y;
        m_tangents[i].z = T.z;
        m_tangents[i].w = (Nv.cross(Tv).dot(Bv) < 0.0f
                            ? (m_config.swapHandedness ?  1.0 : -1.0)
                            : (m_config.swapHandedness ? -1.0 :  1.0));
    }

    delete[] tanNidxs;
    delete[] tan1;
}

void aiPolyMeshSample::updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged)
{
    DebugLog("aiPolyMeshSample::updateConfig()");
    
    topoChanged = (config.swapFaceWinding != m_config.swapFaceWinding || config.submeshPerUVTile != m_config.submeshPerUVTile);
    dataChanged = (config.swapHandedness != m_config.swapHandedness);

    bool smoothNormalsRequired = (config.normalsMode == NM_AlwaysCompute ||
                                  config.tangentsMode == TM_Smooth ||
                                  (!m_normals.valid() && config.normalsMode == NM_ComputeIfMissing));
    
    if (smoothNormalsRequired)
    {
        if (!m_smoothNormals || topoChanged)
        {
            computeSmoothNormals(config);
            dataChanged = true;
        }
    }
    else
    {
        if (m_smoothNormals)
        {
            aiLogger::Info("%s: Clear smooth normals", getSchema()->getObject()->getFullName());
            delete[] m_smoothNormals;
            m_smoothNormals = 0;
            m_smoothNormalsCount = 0;
            dataChanged = true;
        }
    }

    bool tangentsRequired = (m_uvs.valid() && config.tangentsMode != TM_None);

    if (tangentsRequired)
    {
        bool tangentsModeChanged = (config.tangentsMode != m_config.tangentsMode);

        const Abc::V3f *N = 0;
        bool Nindexed = false;

        if (smoothNormalsRequired)
        {
            N = m_smoothNormals;
        }
        else if (m_normals.valid())
        {
            N = m_normals.getVals()->get();
            Nindexed = (m_normals.getScope() == AbcGeom::kFacevaryingScope);
        }

        if (N)
        {
            // do not compute indices if they are cached, constant topology and valid
            if (!m_topology->tangentIndices ||
                !config.cacheTangentsSplits ||
                tangentsModeChanged)
            {
                computeTangentIndices(config, N, Nindexed);
            }
            if (!m_tangents || 
                tangentsModeChanged ||
                topoChanged)
            {
                computeTangents(config, N, Nindexed);
                dataChanged = true;
            }
        }
        else
        {
            tangentsRequired = false;
        }
    }
    
    if (!tangentsRequired)
    {
        if (m_tangents)
        {
            aiLogger::Info("%s: Clear tangents", getSchema()->getObject()->getFullName());
            
            delete[] m_tangents;
            m_tangents = 0;
            m_tangentsCount = 0;
            dataChanged = true;
        }

        if (m_topology->tangentIndices && (m_ownTopology || !config.cacheTangentsSplits))
        {
            aiLogger::Info("%s: Clear tangent indices", getSchema()->getObject()->getFullName());
            
            delete[] m_topology->tangentIndices;
            m_topology->tangentIndices = 0;
            m_topology->tangentIndicesCount = 0;
            m_topology->tangentsCount = 0;
        }
    }

    if (topoChanged)
    {
        dataChanged = true;
    }

    m_config = config;   
}

void aiPolyMeshSample::getSummary(bool forceRefresh, aiMeshSampleSummary &summary) const
{
    DebugLog("aiPolyMeshSample::getSummary(forceRefresh=%s)", forceRefresh ? "true" : "false");
    
    summary.splitCount = m_topology->getSplitCount(forceRefresh);

    summary.hasNormals = hasNormals();

    summary.hasUVs = hasUVs();
    
    summary.hasTangents = hasTangents();
}

int aiPolyMeshSample::getVertexBufferLength(int splitIndex) const
{
    DebugLog("aiPolyMeshSample::getVertexBufferLength(splitIndex=%d)", splitIndex);
    
    return m_topology->getVertexBufferLength(splitIndex);
}

void aiPolyMeshSample::fillVertexBuffer(int splitIndex, aiMeshSampleData &data)
{
    DebugLog("aiPolyMeshSample::fillVertexBuffer(splitIndex=%d)", splitIndex);
    
    if (splitIndex < 0 || size_t(splitIndex) >= m_topology->splits.size() || m_topology->splits[splitIndex].indicesCount == 0)
    {
        return;
    }

    bool copyNormals = (hasNormals() && data.normals);
    bool copyUvs = (hasUVs() && data.uvs);
    bool copyTangents = (hasTangents() && data.tangents);
    
    bool useAbcNormals = (m_normals.valid() && (m_config.normalsMode == NM_ReadFromFile || m_config.normalsMode == NM_ComputeIfMissing));
    float xScale = (m_config.swapHandedness ? -1.0f : 1.0f);

    const SplitInfo &split = m_topology->splits[splitIndex];
    const auto &counts = *(m_topology->counts);
    const auto &indices = *(m_topology->indices);
    const auto &positions = *m_positions;

    size_t k = 0;
    size_t o = split.indexOffset;
    
    // reset unused data arrays

    if (data.normals && !copyNormals)
    {
        aiLogger::Info("%s: Reset normals", getSchema()->getObject()->getFullName());
        memset(data.normals, 0, split.indicesCount * sizeof(Abc::V3f));
    }
    
    if (data.uvs && !copyUvs)
    {
        aiLogger::Info("%s: Reset UVs", getSchema()->getObject()->getFullName());
        memset(data.uvs, 0, split.indicesCount * sizeof(Abc::V2f));
    }
    
    if (data.tangents && !copyTangents)
    {
        aiLogger::Info("%s: Reset tangents", getSchema()->getObject()->getFullName());
        memset(data.tangents, 0, split.indicesCount * sizeof(Imath::V4f));
    }

    abcV3 bbmin = positions[indices[o]];
    abcV3 bbmax = bbmin;

#define UPDATE_POSITIONS_AND_BOUNDS(srcIdx, dstIdx) \
    abcV3 &cP = data.positions[dstIdx]; \
    cP = positions[srcIdx]; \
    cP.x *= xScale; \
    if (cP.x < bbmin.x) bbmin.x = cP.x; \
    else if (cP.x > bbmax.x) bbmax.x = cP.x; \
    if (cP.y < bbmin.y) bbmin.y = cP.y; \
    else if (cP.y > bbmax.y) bbmax.y = cP.y; \
    if (cP.z < bbmin.z) bbmin.z = cP.z; \
    else if (cP.z > bbmax.z) bbmax.z = cP.z

    
    // fill data arrays

    if (copyNormals)
    {
        if (useAbcNormals)
        {
            const auto &normals = *(m_normals.getVals());

            if (m_normals.getScope() == AbcGeom::kFacevaryingScope)
            {
                const auto &nIndices = *(m_normals.getIndices());
                
                if (copyUvs)
                {
                    const auto &uvs = *(m_uvs.getVals());
                    const auto &uvIndices = *(m_uvs.getIndices());
                    
                    if (copyTangents)
                    {
                        for (size_t i=split.firstFace; i<=split.lastFace; ++i)
                        {
                            int nv = counts[i];
                            for (int j = 0; j < nv; ++j, ++o, ++k)
                            {
                                UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                                data.normals[k] = normals[nIndices[o]];
                                data.normals[k].x *= xScale;
                                data.tangents[k] = m_tangents[m_topology->tangentIndices[o]];
                                data.tangents[k].x *= xScale;
                                data.uvs[k] = uvs[uvIndices[o]];
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
                                UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                                data.normals[k] = normals[nIndices[o]];
                                data.normals[k].x *= xScale;
                                data.uvs[k] = uvs[uvIndices[o]];
                            }
                        }
                    }
                }
                else if (copyTangents)
                {
                    for (size_t i=split.firstFace; i<=split.lastFace; ++i)
                    {
                        int nv = counts[i];
                        for (int j = 0; j < nv; ++j, ++o, ++k)
                        {
                            UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                            data.normals[k] = normals[nIndices[o]];
                            data.normals[k].x *= xScale;
                            data.tangents[k] = m_tangents[m_topology->tangentIndices[o]];
                            data.tangents[k].x *= xScale;
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
                            UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                            data.normals[k] = normals[nIndices[o]];
                            data.normals[k].x *= xScale;
                        }
                    }
                }
            }
            else
            {
                if (copyUvs)
                {
                    const auto &uvs = *(m_uvs.getVals());
                    const auto &uvIndices = *(m_uvs.getIndices());
                    
                    if (copyTangents)
                    {
                        for (size_t i=split.firstFace; i<=split.lastFace; ++i)
                        {
                            int nv = counts[i];
                            for (int j = 0; j < nv; ++j, ++o, ++k)
                            {
                                int v = indices[o];
                                UPDATE_POSITIONS_AND_BOUNDS(v, k);
                                data.normals[k] = normals[v];
                                data.normals[k].x *= xScale;
                                data.tangents[k] = m_tangents[m_topology->tangentIndices[o]];
                                data.tangents[k].x *= xScale;
                                data.uvs[k] = uvs[uvIndices[o]];
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
                                int v = indices[o];
                                UPDATE_POSITIONS_AND_BOUNDS(v, k);
                                data.normals[k] = normals[v];
                                data.normals[k].x *= xScale;
                                data.uvs[k] = uvs[uvIndices[o]];
                            }
                        }
                    }
                }
                else if (copyTangents)
                {
                    for (size_t i=split.firstFace; i<=split.lastFace; ++i)
                    {
                        int nv = counts[i];
                        for (int j = 0; j < nv; ++j, ++o, ++k)
                        {
                            int v = indices[o];
                            UPDATE_POSITIONS_AND_BOUNDS(v, k);
                            data.normals[k] = normals[v];
                            data.normals[k].x *= xScale;
                            data.tangents[k] = m_tangents[m_topology->tangentIndices[o]];
                            data.tangents[k].x *= xScale;
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
                            int v = indices[o];
                            UPDATE_POSITIONS_AND_BOUNDS(v, k);
                            data.normals[k] = normals[v];
                            data.normals[k].x *= xScale;
                        }
                    }
                }
            }
        }
        else
        {
            if (copyUvs)
            {
                const auto &uvs = *(m_uvs.getVals());
                const auto &uvIndices = *(m_uvs.getIndices());
                
                if (copyTangents)
                {
                    for (size_t i=split.firstFace; i<=split.lastFace; ++i)
                    {
                        int nv = counts[i];
                        for (int j = 0; j < nv; ++j, ++o, ++k)
                        {
                            int v = indices[o];
                            UPDATE_POSITIONS_AND_BOUNDS(v, k);
                            data.normals[k] = m_smoothNormals[v];
                            data.normals[k].x *= xScale;
                            data.tangents[k] = m_tangents[m_topology->tangentIndices[o]];
                            data.tangents[k].x *= xScale;
                            data.uvs[k] = uvs[uvIndices[o]];
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
                            int v = indices[o];
                            UPDATE_POSITIONS_AND_BOUNDS(v, k);
                            data.normals[k] = m_smoothNormals[v];
                            data.normals[k].x *= xScale;
                            data.uvs[k] = uvs[uvIndices[o]];
                        }
                    }
                }
            }
            else if (copyTangents)
            {
                for (size_t i=split.firstFace; i<=split.lastFace; ++i)
                {
                    int nv = counts[i];
                    for (int j = 0; j < nv; ++j, ++o, ++k)
                    {
                        int v = indices[o];
                        UPDATE_POSITIONS_AND_BOUNDS(v, k);
                        data.normals[k] = m_smoothNormals[v];
                        data.normals[k].x *= xScale;
                        data.tangents[k] = m_tangents[m_topology->tangentIndices[o]];
                        data.tangents[k].x *= xScale;
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
                        int v = indices[o];
                        UPDATE_POSITIONS_AND_BOUNDS(v, k);
                        data.normals[k] = m_smoothNormals[v];
                        data.normals[k].x *= xScale;
                    }
                }
            }
        }
    }
    else
    {
        if (copyUvs)
        {
            const auto &uvs = *(m_uvs.getVals());
            const auto &uvIndices = *(m_uvs.getIndices());
            
            if (copyTangents)
            {
                for (size_t i=split.firstFace; i<=split.lastFace; ++i)
                {
                    int nv = counts[i];
                    for (int j = 0; j < nv; ++j, ++o, ++k)
                    {
                        UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                        data.tangents[k] = m_tangents[m_topology->tangentIndices[o]];
                        data.tangents[k].x *= xScale;
                        data.uvs[k] = uvs[uvIndices[o]];
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
                        UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                        data.uvs[k] = uvs[uvIndices[o]];
                    }
                }
            }
        }
        else if (copyTangents)
        {
            for (size_t i=split.firstFace; i<=split.lastFace; ++i)
            {
                int nv = counts[i];
                for (int j = 0; j < nv; ++j, ++o, ++k)
                {
                    UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                    data.tangents[k] = m_tangents[m_topology->tangentIndices[o]];
                    data.tangents[k].x *= xScale;
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
                    UPDATE_POSITIONS_AND_BOUNDS(indices[o], k);
                }
            }
        }
    }

#undef UPDATE_POSITIONS_AND_BOUNDS

    data.center = 0.5f * (bbmin + bbmax);
    data.size = bbmax - bbmin;
}

int aiPolyMeshSample::prepareSubmeshes(const aiFacesets &inFacesets)
{
    DebugLog("aiPolyMeshSample::prepateSubmeshes()");
    
    int rv = m_topology->prepareSubmeshes(m_uvs, inFacesets, m_config.submeshPerUVTile);

    m_curSubmesh = m_topology->submeshBegin();

    return rv;
}

int aiPolyMeshSample::getSplitSubmeshCount(int splitIndex) const
{
    DebugLog("aiPolyMeshSample::getSplitSubmeshCount()");
    
    return m_topology->getSplitSubmeshCount(splitIndex);
}

bool aiPolyMeshSample::getNextSubmesh(aiSubmeshSummary &summary)
{
    DebugLog("aiPolyMeshSample::getNextSubmesh()");
    
    if (m_curSubmesh == m_topology->submeshEnd())
    {
        return false;
    }
    else
    {
        Submesh &submesh = *m_curSubmesh;

        summary.index = int(m_curSubmesh - m_topology->submeshBegin());
        summary.splitIndex = submesh.splitIndex;
        summary.splitSubmeshIndex = submesh.index;
        summary.facesetIndex = submesh.facesetIndex;
        summary.triangleCount = int(submesh.triangleCount);

        ++m_curSubmesh;

        return true;
    }
}

void aiPolyMeshSample::fillSubmeshIndices(const aiSubmeshSummary &summary, aiSubmeshData &data) const
{
    DebugLog("aiPolyMeshSample::fillSubmeshIndices()");
    
    Submeshes::const_iterator it = m_topology->submeshBegin() + summary.index;
    
    if (it != m_topology->submeshEnd())
    {
        bool ccw = m_config.swapFaceWinding;
        const auto &counts = *(m_topology->counts);
        const Submesh &submesh = *it;

        int index = 0;
        int i1 = (ccw ? 2 : 1);
        int i2 = (ccw ? 1 : 2);
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
                    data.indices[offset + 0] = index;
                    data.indices[offset + 1] = index + ti + i1;
                    data.indices[offset + 2] = index + ti + i2;
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
                    data.indices[offset + 0] = submesh.vertexIndices[index];
                    data.indices[offset + 1] = submesh.vertexIndices[index + ti + i1];
                    data.indices[offset + 2] = submesh.vertexIndices[index + ti + i2];
                    offset += 3;
                }

                index += nv;
            }
        }
    }
}

// ---

aiPolyMesh::aiPolyMesh(aiObject *obj)
    : super(obj)
    , m_peakIndexCount(0)
    , m_peakVertexCount(0)
    , m_ignoreNormals(false)
    , m_ignoreUVs(false)
{
    m_constant = m_schema.isConstant();

    auto normals = m_schema.getNormalsParam();
    if (normals.valid())
    {
        if (normals.getScope() == AbcGeom::kFacevaryingScope ||
            normals.getScope() == AbcGeom::kVaryingScope ||
            normals.getScope() == AbcGeom::kVertexScope)
        {
            if (!normals.isConstant())
            {
                m_constant = false;
            }
        }
        else
        {
            m_ignoreNormals = true;
        }
    }

    auto uvs = m_schema.getUVsParam();
    if (uvs.valid())
    {
        if (uvs.getScope() == AbcGeom::kFacevaryingScope)
        {
            if (!uvs.isConstant())
            {
                m_constant = false;
            }
        }
        else
        {
            m_ignoreUVs = true;
        }
    }

    m_varyingTopology = (m_schema.getTopologyVariance() == AbcGeom::kHeterogeneousTopology);
    
    DebugLog("aiPolyMesh::aiPolyMesh(constant=%s, varyingTopology=%s)",
             (m_constant ? "true" : "false"),
             (m_varyingTopology ? "true" : "false"));
}

aiPolyMesh::Sample* aiPolyMesh::newSample()
{
    Sample *sample = getSample();
    
    if (!sample)
    {
        if (dontUseCache() || !m_varyingTopology)
        {
            sample = new Sample(this, &m_sharedTopology, false);
        }
        else
        {
            sample = new Sample(this, new Topology(), true);
        }
    }
    else
    {
        if (m_varyingTopology)
        {
            sample->m_topology->clear();
        }
    }
    
    return sample;
}

aiPolyMesh::Sample* aiPolyMesh::readSample(float time, bool &topologyChanged)
{
    DebugLog("aiPolyMesh::readSample(t=%f)", time);
    
    Sample *ret = newSample();

    auto ss = MakeSampleSelector(time);

    topologyChanged = m_varyingTopology;

    if (!ret->m_topology->counts || m_varyingTopology)
    {
        DebugLog("  Read face counts");
        m_schema.getFaceCountsProperty().get(ret->m_topology->counts, ss);
        topologyChanged = true;
    }

    if (!ret->m_topology->indices || m_varyingTopology)
    {
        DebugLog("  Read face indices");
        m_schema.getFaceIndicesProperty().get(ret->m_topology->indices, ss);
        topologyChanged = true;
    }

    DebugLog("  Read positions");
    m_schema.getPositionsProperty().get(ret->m_positions, ss);

    ret->m_velocities.reset();
    auto velocitiesProp = m_schema.getVelocitiesProperty();
    if (velocitiesProp.valid())
    {
        DebugLog("  Read velocities");
        velocitiesProp.get(ret->m_velocities, ss);
    }

    ret->m_normals.reset();
    auto normalsParam = m_schema.getNormalsParam();
    if (!m_ignoreNormals && normalsParam.valid())
    {
        if (normalsParam.isConstant())
        {
            if (!m_sharedNormals.valid())
            {
                DebugLog("  Read normals (constant)");
                normalsParam.getIndexed(m_sharedNormals, ss);
            }

            ret->m_normals = m_sharedNormals;
        }
        else
        {
            DebugLog("  Read normals");
            normalsParam.getIndexed(ret->m_normals, ss);
        }
    }

    ret->m_uvs.reset();
    auto uvsParam = m_schema.getUVsParam();
    if (!m_ignoreUVs && uvsParam.valid())
    {
        if (uvsParam.isConstant())
        {
            if (!m_sharedUVs.valid())
            {
                DebugLog("  Read uvs (constant)");
                uvsParam.getIndexed(m_sharedUVs, ss);
            }

            ret->m_uvs = m_sharedUVs;
        }
        else
        {
            DebugLog("  Read uvs");
            uvsParam.getIndexed(ret->m_uvs, ss);
        }
    }

    bool smoothNormalsRequired = ret->smoothNormalsRequired();

    if (smoothNormalsRequired)
    {
        ret->computeSmoothNormals(m_config);
    }

    if (ret->tangentsRequired())
    {
        const Abc::V3f *normals = 0;
        bool indexedNormals = false;
        
        if (smoothNormalsRequired)
        {
            normals = ret->m_smoothNormals;
        }
        else if (ret->m_normals.valid())
        {
            normals = ret->m_normals.getVals()->get();
            indexedNormals = (ret->m_normals.getScope() == AbcGeom::kFacevaryingScope);
        }

        if (normals && ret->m_uvs.valid())
        {
            // topology may be shared, check tangent indices
            if (!ret->m_topology->tangentIndices || !m_config.cacheTangentsSplits)
            {
                ret->computeTangentIndices(m_config, normals, indexedNormals);
            }

            ret->computeTangents(m_config, normals, indexedNormals);
        }
    }

    return ret;
}

int aiPolyMesh::getTopologyVariance() const
{
    return (int) m_schema.getTopologyVariance();
}

int aiPolyMesh::getPeakIndexCount() const
{
    if (m_peakIndexCount == 0)
    {
        DebugLog("aiPolyMesh::getPeakIndexCount()");
        
        Util::Dimensions dim;
        Abc::Int32ArraySamplePtr counts;

        auto indicesProp = m_schema.getFaceIndicesProperty();
        auto countsProp = m_schema.getFaceCountsProperty();
        
        int numSamples = indicesProp.getNumSamples();

        if (numSamples == 0)
        {
            return 0;
        }
        else if (indicesProp.isConstant())
        {
            countsProp.get(counts, Abc::ISampleSelector(int64_t(0)));
        }
        else
        {
            aiLogger::Info("Checking %d sample(s)", numSamples);
            
            int iMax = 0;
            size_t cMax = 0;

            for (int i = 0; i < numSamples; ++i)
            {
                indicesProp.getDimensions(dim, Abc::ISampleSelector(int64_t(i)));
                
                size_t numIndices = dim.numPoints();

                if (numIndices > cMax)
                {
                    cMax = numIndices;
                    iMax = i;
                }
            }

            countsProp.get(counts, Abc::ISampleSelector(int64_t(iMax)));
        }

        m_peakIndexCount = CalculateIndexCount(*counts);
    }

    return m_peakIndexCount;
}

int aiPolyMesh::getPeakVertexCount() const
{
    if (m_peakVertexCount == 0)
    {
        DebugLog("aiPolyMesh::getPeakVertexCount()");
        
        Util::Dimensions dim;

        auto positionsProp = m_schema.getPositionsProperty();

        int numSamples = positionsProp.getNumSamples();

        if (numSamples == 0)
        {
            return 0;
        }
        else if (positionsProp.isConstant())
        {
            positionsProp.getDimensions(dim, Abc::ISampleSelector(int64_t(0)));
            
            m_peakVertexCount = dim.numPoints();
        }
        else
        {
            m_peakVertexCount = 0;

            for (int i = 0; i < numSamples; ++i)
            {
                positionsProp.getDimensions(dim, Abc::ISampleSelector(int64_t(i)));
                
                size_t numVertices = dim.numPoints();

                if (numVertices > size_t(m_peakVertexCount))
                {
                    m_peakVertexCount = int(numVertices);
                }
            }
        }
    }

    return m_peakVertexCount;
}

void aiPolyMesh::getSummary(aiMeshSummary &summary) const
{
    DebugLog("aiPolyMesh::getSummary()");
    
    summary.topologyVariance = getTopologyVariance();
}


