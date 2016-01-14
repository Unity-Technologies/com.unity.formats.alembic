#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiPolyMesh.h"

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
    : m_tangentIndicesCount(0)
    , m_tangentIndices(0)
    , m_tangentsCount(0)
{
    m_indices.reset();
    m_counts.reset();
}

Topology::~Topology()
{
    if (m_tangentIndices)
    {
        delete[] m_tangentIndices;
    }
}

void Topology::clear()
{
    aiLogger::Info("Topology::clear()");
    m_indices.reset();
    m_counts.reset();

    if (m_tangentIndices)
    {
        delete[] m_tangentIndices;
        m_tangentIndices = 0;
    }
    m_tangentIndicesCount = 0;
    m_tangentsCount = 0;

    m_submeshes.clear();
    m_faceSplitIndices.clear();
    m_splits.clear();
}

int Topology::getSplitCount() const
{
    return (int) m_splits.size();
}

int Topology::getSplitCount(bool forceRefresh)
{
    if (m_counts && m_indices)
    {
        if (m_faceSplitIndices.size() != m_counts->size() || forceRefresh)
        {
            updateSplits();
        }
    }
    else
    {
        m_splits.clear();
        m_faceSplitIndices.clear();
    }

    return (int) m_splits.size();
}

void Topology::updateSplits()
{
    DebugLog("Topology::updateSplits()");
    
    int splitIndex = 0;
    size_t indexOffset = 0;
    size_t ncounts = m_counts->size();

    m_faceSplitIndices.resize(ncounts);

    m_splits.clear();
    m_splits.reserve(1 + m_indices->size() / 65000);
    m_splits.push_back(SplitInfo());

    SplitInfo *curSplit = &(m_splits.back());
    
    for (size_t i=0; i<ncounts; ++i)
    {
        size_t nv = (size_t) m_counts->get()[i];

        if (curSplit->vertexCount + nv > 65000)
        {
            m_splits.push_back(SplitInfo(i, indexOffset));
            
            ++splitIndex;

            curSplit = &(m_splits.back());
        }
        
        m_faceSplitIndices[i] = splitIndex;

        curSplit->lastFace = i;
        curSplit->vertexCount += nv;

        indexOffset += nv;
    }
}

int Topology::getVertexBufferLength(int splitIndex) const
{
    if (splitIndex < 0 || size_t(splitIndex) >= m_splits.size())
    {
        return 0;
    }
    else
    {
        return (int) m_splits[splitIndex].vertexCount;
    }
}

int Topology::prepareSubmeshes(const AbcGeom::IV2fGeomParam::Sample &uvs,
                               const aiFacesets &inFacesets,
                               bool submeshPerUVTile)
{
    DebugLog("Topology::prepareSubmeshes()");
    
    Facesets facesets;
    std::vector<int> facesetIndices; // index -> face table


    m_submeshes.clear();

    if (inFacesets.count > 0)
    {
        int index_count = 0;
        int max_index = 0;
        {
            int n = inFacesets.count;
            for (int i = 0; i < n; ++i)
            {
                int ngon = inFacesets.faceCounts[i];
                for (int j = 0; j < ngon; ++j)
                {
                    max_index = std::max<int>(max_index, inFacesets.faceIndices[index_count++]);
                }
            }
        }
        facesetIndices.resize(max_index, -1);


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

                    faceset.push_back(f);

                    facesetIndices[f] = i;
                }
            }
        }

        if (defaultFacesetIndex != -1) {
            facesetIndices.resize(m_counts->size(), -1);
            for (size_t i=0; i<m_counts->size(); ++i)
            {
                if (facesetIndices[i] == -1) {
                    facesetIndices[i] = defaultFacesetIndex;
                }
            }
        }
    }
    else
    {
        // don't even fill faceset if we have no UVs to tile split the mesh
        if (uvs.valid() && submeshPerUVTile)
        {
            facesets.resize(1);
            facesetIndices.resize(m_counts->size(), -1);

            Faceset &faceset = facesets[0];

            for (size_t i=0; i<m_counts->size(); ++i)
            {
                faceset.push_back(i);
            }
        }
    }

    int nsplits = getSplitCount(false);

    if (facesets.empty() && nsplits == 1)
    {
        // no facesets, no uvs, no splits
        m_submeshes.push_back(Submesh());
        
        Submesh &submesh = m_submeshes.back();

        for (size_t i=0; i<m_counts->size(); ++i)
        {
            submesh.triangleCount += (m_counts->get()[i] - 2);
        }

        m_splits[0].submeshCount = 1;
    }
    else
    {
        int vertexIndex = 0;
        Submesh *curMesh = 0;
        const Util::uint32_t *uvIndices = 0;
        const abcV2 *uvValues = 0;

        if (uvs.valid() && submeshPerUVTile)
        {
            uvValues = uvs.getVals()->get();
            uvIndices = uvs.getIndices()->get();
        }

        std::map<SubmeshKey, size_t> submeshIndices;

        std::vector<int> splitSubmeshIndices(nsplits, 0);

        for (size_t i=0; i<m_counts->size(); ++i)
        {
            int nv = m_counts->get()[i];
            
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
                    abcV2 uv = uvValues[uvIndices[vertexIndex + j]];
                    uAcc += uv.x;
                    vAcc += uv.y;
                }
            }

            SubmeshKey sid(uAcc * invNv, vAcc * invNv, facesetIndex, splitIndex);

            auto submeshIndexIt = submeshIndices.find(sid);

            if (submeshIndexIt == submeshIndices.end())
            {
                submeshIndices[sid] = m_submeshes.size();

                m_submeshes.push_back(Submesh(facesetIndex, splitIndex));

                curMesh = &(m_submeshes.back());

                curMesh->index = splitSubmeshIndices[splitIndex]++;
                curMesh->vertexIndices.reserve(m_indices->size());

                split.submeshCount = splitSubmeshIndices[splitIndex];
            }
            else
            {
                curMesh = &(m_submeshes[submeshIndexIt->second]);
            }

            curMesh->faces.push_back(i);
            curMesh->triangleCount += (nv - 2);

            for (int j=0; j<nv; ++j, ++vertexIndex)
            {
                curMesh->vertexIndices.push_back(vertexIndex - int(split.indexOffset));
            }
        }

        for (size_t i=0; i<m_submeshes.size(); ++i)
        {
            m_submeshes[i].vertexIndices.shrink_to_fit();
        }
    }

    return (int) m_submeshes.size();
}

int Topology::getSplitSubmeshCount(int splitIndex) const
{
    if (splitIndex < 0 || size_t(splitIndex) >= m_splits.size())
    {
        return 0;
    }
    else
    {
        return (int) m_splits[splitIndex].submeshCount;
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
    return (m_config.tangentsMode != TM_None && hasUVs() && m_tangents && m_topology->m_tangentIndices);
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

    const auto &counts = *(m_topology->m_counts);
    const auto &indices = *(m_topology->m_indices);
    const auto &positions = *m_positions;

    size_t nf = counts.size();
    size_t off = 0;
    bool ccw = config.swapFaceWinding;
    int ti1 = ccw ? 2 : 1;
    int ti2 = ccw ? 1 : 2;
    abcV3 N, dP1, dP2;

    for (size_t f=0; f<nf; ++f)
    {
        int nfv = counts[f];

        if (nfv >= 3)
        {
            // Compute average normal for current face
            N.setValue(0.0f, 0.0f, 0.0f);

            const abcV3 &P0 = positions[indices[off]];
            
            for (int fv=0; fv<nfv-2; ++fv)
            {
                const abcV3 &P1 = positions[indices[off + fv + ti1]];
                const abcV3 &P2 = positions[indices[off + fv + ti2]];

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

void aiPolyMeshSample::computeTangentIndices(const aiConfig &config, const abcV3 *inN, bool indexedNormals)
{
    aiLogger::Info("%s: Compute tangent indices...", getSchema()->getObject()->getFullName());

    const auto &counts = *(m_topology->m_counts);
    const auto &indices = *(m_topology->m_indices);
    const auto &uvVals = *(m_uvs.getVals());
    const auto &uvIdxs = *(m_uvs.getIndices());
    const Util::uint32_t *Nidxs = (indexedNormals ? m_normals.getIndices()->get() : 0);

    size_t tangentIndicesCount = indices.size();

    if (!m_topology->m_tangentIndices)
    {
        m_topology->m_tangentIndices = new int[tangentIndicesCount];
    }
    else if (m_topology->m_tangentIndicesCount != tangentIndicesCount)
    {
        delete[] m_topology->m_tangentIndices;
        m_topology->m_tangentIndices = new int[tangentIndicesCount];
    }

    m_topology->m_tangentIndicesCount = tangentIndicesCount;
    
    if (config.tangentsMode == TM_Smooth)
    {
        for (size_t i=0; i<tangentIndicesCount; ++i)
        {
            m_topology->m_tangentIndices[i] = indices[i];
        }

        m_topology->m_tangentsCount = m_positions->size();
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
                    
                    m_topology->m_tangentIndices[v] = idx;
                    uniqueIndices[key] = idx;
                }
                else
                {
                    m_topology->m_tangentIndices[v] = it->second;
                }
            }
        }

        m_topology->m_tangentsCount = uniqueIndices.size(); 
    }
    
    aiLogger::Info("%lu unique tangent(s)", m_topology->m_tangentsCount);
}

void aiPolyMeshSample::computeTangents(const aiConfig &config, const abcV3 *inN, bool indexedNormals)
{
    aiLogger::Info("%s: Compute %stangents", getSchema()->getObject()->getFullName(), (config.tangentsMode == TM_Smooth ? "smooth " : ""));

    const auto &counts = *(m_topology->m_counts);
    const auto &indices = *(m_topology->m_indices);
    const auto &positions = *m_positions;
    const auto &uvVals = *(m_uvs.getVals());
    const auto &uvIdxs = *(m_uvs.getIndices());
    const Util::uint32_t *Nidxs = (indexedNormals ? m_normals.getIndices()->get() : 0);

    size_t nf = counts.size();
    size_t off = 0;
    bool ccw = config.swapFaceWinding;
    int ti1 = (ccw ? 2 : 1);
    int ti2 = (ccw ? 1 : 2);    
    size_t tangentsCount = m_topology->m_tangentsCount;

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

    abcV3 *tan1 = new Abc::V3f[2 * tangentsCount];
    abcV3 *tan2 = tan1 + tangentsCount;
    int *tanNidxs = new int[tangentsCount];
    abcV3 T, B, dP1, dP2, tmp;
    abcV2 dUV1, dUV2;

    memset(tan1, 0, 2 * tangentsCount * sizeof(Abc::V3f));

    for (size_t f=0; f<nf; ++f)
    {
        int nfv = counts[f];

        if (nfv >= 3)
        {
            // reset face tangent and bitangent
            T.setValue(0.0f, 0.0f, 0.0f);
            B.setValue(0.0f, 0.0f, 0.0f);

            const abcV3 &P0 = positions[indices[off]];
            const abcV2 &UV0 = uvVals[uvIdxs[off]];

            // for each triangle making up current polygon
            for (int fv=0; fv<nfv-2; ++fv)
            {
                const abcV3 &P1 = positions[indices[off + fv + ti1]];
                const abcV3 &P2 = positions[indices[off + fv + ti2]];

                const abcV2 &UV1 = uvVals[uvIdxs[off + fv + ti1]];
                const abcV2 &UV2 = uvVals[uvIdxs[off + fv + ti2]];

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
                int v = m_topology->m_tangentIndices[off + fv];
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
        const abcV3 &Nv = inN[tanNidxs[i]];
        abcV3 &Tv = tan1[i];
        abcV3 &Bv = tan2[i];

        // Normalize Tv and Bv?
        
        T = Tv - Nv * Tv.dot(Nv);
        T.normalize();

        m_tangents[i].x = T.x;
        m_tangents[i].y = T.y;
        m_tangents[i].z = T.z;
        m_tangents[i].w = (Nv.cross(Tv).dot(Bv) < 0.0f
                            ? (m_config.swapHandedness ?  1.0f : -1.0f)
                            : (m_config.swapHandedness ? -1.0f :  1.0f));
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

        const abcV3 *N = 0;
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
            if (!m_topology->m_tangentIndices ||
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

        if (m_topology->m_tangentIndices && (m_ownTopology || !config.cacheTangentsSplits))
        {
            aiLogger::Info("%s: Clear tangent indices", getSchema()->getObject()->getFullName());
            
            delete[] m_topology->m_tangentIndices;
            m_topology->m_tangentIndices = 0;
            m_topology->m_tangentIndicesCount = 0;
            m_topology->m_tangentsCount = 0;
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


void aiPolyMeshSample::getDataPointer(aiMeshSampleData &dst)
{
    if (m_positions) {
        dst.positionCount = m_positions->valid() ? m_positions->size() : 0;
        dst.positions = const_cast<abcV3*>(m_positions->get());
    }

    if (m_velocities) {
        dst.velocities = m_velocities->valid() ? (abcV3*)m_velocities->get() : nullptr;
    }

    if (m_normals) {
        dst.normalCount = m_normals.getVals()->size();
        dst.normals = (abcV3*)m_normals.getVals()->get();
        dst.normalIndexCount = m_normals.isIndexed() ? m_normals.getIndices()->size() : 0;
        if (dst.normalIndexCount) {
            dst.normalIndices = (int*)m_normals.getIndices()->get();
        }
    }

    if (m_uvs) {
        dst.uvCount = m_uvs.getVals()->size();
        dst.uvs = (abcV2*)m_uvs.getVals()->get();
        dst.uvIndexCount = m_uvs.isIndexed() ? m_uvs.getIndices()->size() : 0;
        if (dst.uvIndexCount) {
            dst.uvIndices = (int*)m_uvs.getIndices()->get();
        }
    }

    if (m_topology) {
        if (m_topology->m_indices) {
            dst.indexCount = m_topology->m_indices->size();
            dst.indices = (int*)m_topology->m_indices->get();
        }
        if (m_topology->m_counts) {
            dst.faces = (int*)m_topology->m_counts->get();
            dst.faceCount = m_topology->m_counts->size();
        }
    }

    dst.center = m_bounds.center();
    dst.size = m_bounds.size();
}

void aiPolyMeshSample::copyData(aiMeshSampleData &dst)
{
    const aiMeshSampleData src;
    getDataPointer((aiMeshSampleData&)src);

    auto swap_handedness_if_needed = [&](abcV3 *d, int n) {
        if (m_config.swapHandedness) {
            for (int i = 0; i < n; ++i) { d[i].x *= -1.0f; }
        }
    };

    // sadly, memcpy() is way faster than std::copy()

    if (src.faces && dst.faces && dst.faceCount >= src.faceCount) {
        memcpy(dst.faces, src.faces, src.faceCount * sizeof(*dst.faces));
    }

    if (src.positions && dst.positions && dst.positionCount >= src.positionCount) {
        memcpy(dst.positions, src.positions, src.positionCount * sizeof(*dst.positions));
        swap_handedness_if_needed(dst.positions, src.positionCount);
    }

    if (src.velocities && dst.velocities && dst.positionCount >= src.positionCount) {
        memcpy(dst.velocities, src.velocities, src.positionCount * sizeof(*dst.velocities));
        swap_handedness_if_needed(dst.velocities, src.positionCount);
    }

    if (src.normals && dst.normals && dst.normalCount >= src.normalCount) {
        memcpy(dst.normals, src.normals, src.normalCount * sizeof(*dst.normals));
        swap_handedness_if_needed(dst.normals, src.normalCount);
    }

    if (src.uvs && dst.uvs && dst.uvCount >= src.uvCount) {
        memcpy(dst.uvs, src.uvs, src.uvCount * sizeof(*dst.uvs));
    }


    auto copy_indices = [&](int *d, const int *s, int n) {
        // swap faces if needed
        if (m_config.swapFaceWinding) {
            int i = 0;
            for (int fi = 0; fi < src.faceCount; ++fi) {
                int ngon = src.faces[i];
                for (int ni = 0; ni < ngon; ++ni) {
                    int ini = ngon - ni - 1;
                    d[i + ni] = s[i + ini];
                }
                i += ngon;
            }
        }
        else {
            memcpy(d, s, n * sizeof(int));
        }
    };

    if (src.indices && dst.indices && dst.indexCount >= src.indexCount) {
        copy_indices(dst.indices, src.indices, src.indexCount);
    }
    if (src.normalIndices && dst.normalIndices && dst.normalIndexCount >= src.normalIndexCount) {
        copy_indices(dst.normalIndices, src.normalIndices, src.normalIndexCount);
    }
    if (src.uvIndices && dst.uvIndices && dst.uvIndexCount >= src.uvIndexCount) {
        copy_indices(dst.uvIndices, src.uvIndices, src.uvIndexCount);
    }

    dst.center = dst.center;
    dst.size = dst.size;
}

int aiPolyMeshSample::getVertexBufferLength(int splitIndex) const
{
    DebugLog("aiPolyMeshSample::getVertexBufferLength(splitIndex=%d)", splitIndex);
    
    return m_topology->getVertexBufferLength(splitIndex);
}

void aiPolyMeshSample::fillVertexBuffer(int splitIndex, aiMeshSampleData &data)
{
    DebugLog("aiPolyMeshSample::fillVertexBuffer(splitIndex=%d)", splitIndex);
    
    if (splitIndex < 0 || size_t(splitIndex) >= m_topology->m_splits.size() || m_topology->m_splits[splitIndex].vertexCount == 0)
    {
        return;
    }

    bool copyNormals = (hasNormals() && data.normals);
    bool copyUvs = (hasUVs() && data.uvs);
    bool copyTangents = (hasTangents() && data.tangents);
    
    bool useAbcNormals = (m_normals.valid() && (m_config.normalsMode == NM_ReadFromFile || m_config.normalsMode == NM_ComputeIfMissing));
    float xScale = (m_config.swapHandedness ? -1.0f : 1.0f);

    const SplitInfo &split = m_topology->m_splits[splitIndex];
    const auto &counts = *(m_topology->m_counts);
    const auto &indices = *(m_topology->m_indices);
    const auto &positions = *m_positions;

    size_t k = 0;
    size_t o = split.indexOffset;
    
    // reset unused data arrays

    if (data.normals && !copyNormals)
    {
        aiLogger::Info("%s: Reset normals", getSchema()->getObject()->getFullName());
        memset(data.normals, 0, split.vertexCount * sizeof(abcV3));
    }
    
    if (data.uvs && !copyUvs)
    {
        aiLogger::Info("%s: Reset UVs", getSchema()->getObject()->getFullName());
        memset(data.uvs, 0, split.vertexCount * sizeof(abcV2));
    }
    
    if (data.tangents && !copyTangents)
    {
        aiLogger::Info("%s: Reset tangents", getSchema()->getObject()->getFullName());
        memset(data.tangents, 0, split.vertexCount * sizeof(abcV4));
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
                                data.tangents[k] = m_tangents[m_topology->m_tangentIndices[o]];
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
                            data.tangents[k] = m_tangents[m_topology->m_tangentIndices[o]];
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
                                data.tangents[k] = m_tangents[m_topology->m_tangentIndices[o]];
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
                            data.tangents[k] = m_tangents[m_topology->m_tangentIndices[o]];
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
                            data.tangents[k] = m_tangents[m_topology->m_tangentIndices[o]];
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
                        data.tangents[k] = m_tangents[m_topology->m_tangentIndices[o]];
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
                        data.tangents[k] = m_tangents[m_topology->m_tangentIndices[o]];
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
                    data.tangents[k] = m_tangents[m_topology->m_tangentIndices[o]];
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
        const auto &counts = *(m_topology->m_counts);
        const Submesh &submesh = *it;

        int index = 0;
        int i1 = (ccw ? 2 : 1);
        int i2 = (ccw ? 1 : 2);
        int offset = 0;
        
        if (submesh.faces.empty() && submesh.vertexIndices.empty())
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
            for (const size_t& f : submesh.faces)
            {
                int nv = counts[f];
                
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

    if (!ret->m_topology->m_counts || m_varyingTopology)
    {
        DebugLog("  Read face counts");
        m_schema.getFaceCountsProperty().get(ret->m_topology->m_counts, ss);
        topologyChanged = true;
    }

    if (!ret->m_topology->m_indices || m_varyingTopology)
    {
        DebugLog("  Read face indices");
        m_schema.getFaceIndicesProperty().get(ret->m_topology->m_indices, ss);
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

    auto boundsParam = m_schema.getSelfBoundsProperty();
    if (boundsParam) {
        boundsParam.get(ret->m_bounds, ss);
    }

    bool smoothNormalsRequired = ret->smoothNormalsRequired();

    if (smoothNormalsRequired)
    {
        ret->computeSmoothNormals(m_config);
    }

    if (ret->tangentsRequired())
    {
        const abcV3 *normals = 0;
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
            if (!ret->m_topology->m_tangentIndices || !m_config.cacheTangentsSplits)
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
        
        int numSamples = (int)indicesProp.getNumSamples();

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

        int numSamples = (int)positionsProp.getNumSamples();

        if (numSamples == 0)
        {
            return 0;
        }
        else if (positionsProp.isConstant())
        {
            positionsProp.getDimensions(dim, Abc::ISampleSelector(int64_t(0)));
            
            m_peakVertexCount = (int)dim.numPoints();
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
    summary.peakVertexCount = getPeakVertexCount();
    summary.peakIndexCount = getPeakIndexCount();
    summary.peakSubmeshCount = ceildiv(summary.peakIndexCount, 64998);
}

