#pragma once

using abcFaceSetSchemas = std::vector<AbcGeom::IFaceSetSchema>;
using abcFaceSetSamples = std::vector<AbcGeom::IFaceSetSchema::Sample>;

struct SplitInfo
{
    int firstFace = 0;
    int lastFace = 0;
    int indexOffset = 0;
    int vertexCount = 0;
    int submeshCount = 0;

    inline SplitInfo(int ff=0, int io=0)
        : firstFace(ff)
        , lastFace(ff)
        , indexOffset(io)
    {
    }

    SplitInfo(const SplitInfo&) = default;
    SplitInfo& operator=(const SplitInfo&) = default;
};

struct Submesh
{
    RawVector<int> faces;
    RawVector<int> vertexIndices;
    int triangleCount = 0;
    int facesetIndex = 0;
    int splitIndex = 0;
    int index = 0; // submesh index in split

    inline Submesh(int fsi=-1, int si=0)
        : facesetIndex(fsi)
        , splitIndex(si)
    {
    }

    Submesh(const Submesh&) = default;
    Submesh& operator=(const Submesh&) = default;
};

typedef std::deque<Submesh> Submeshes;

struct TangentKey
{
    Abc::V3f N;
    Abc::V2f UV;
    
    inline TangentKey()
        : N(0.0f, 0.0f, 0.0f)
        , UV(0.0f, 0.0f)
    {
    }
    
    inline TangentKey(const Abc::V3f &iN, const Abc::V2f &iUV)
        : N(iN)
        , UV(iUV)
    {
    }
    
    TangentKey(const TangentKey&) = default;
    TangentKey& operator=(const TangentKey&) = default;
    
    inline bool operator<(const TangentKey &rhs) const
    {
        if (N.x < rhs.N.x) return true;
        if (N.x > rhs.N.x) return false;

        if (N.y < rhs.N.y) return true;
        if (N.y > rhs.N.y) return false;

        if (N.z < rhs.N.z) return true;
        if (N.z > rhs.N.z) return false;

        if (UV.x < rhs.UV.x) return true;
        if (UV.x > rhs.UV.x) return false;

        return (UV.y < rhs.UV.y);
    }

    std::string toString() const
    {
        std::ostringstream oss;
        oss << "N=" << N << ", UV=" << UV;
        return oss.str();
    }
};

typedef std::map<TangentKey, int> TangentIndexMap;

class Topology
{
public:
    Topology();
    void clear();

    int getTriangulatedIndexCount() const;
    int getSplitCount() const;
    int getSplitCount(aiPolyMeshSample * meshSample, bool forceRefresh);
    void updateSplits(aiPolyMeshSample * meshSample);

    int getVertexBufferLength(int splitIndex) const;
    int prepareSubmeshes(abcFaceSetSamples& fs, aiPolyMeshSample* sample);
    int getSplitSubmeshCount(int splitIndex) const;

    inline Submeshes::iterator submeshBegin() { return m_submeshes.begin(); }
    inline Submeshes::iterator submeshEnd() { return m_submeshes.end(); }

    inline Submeshes::const_iterator submeshBegin() const { return m_submeshes.begin(); }
    inline Submeshes::const_iterator submeshEnd() const { return m_submeshes.end(); }

    inline void EnableVertexSharing(bool value) { m_vertexSharingEnabled = value; }
    inline void Enable32BitsIndexbuffers(bool value) { m_use32BitsIndexBuffer = value; }
    inline void TreatVertexExtraDataAsStatic(bool value) { m_TreatVertexExtraDataAsStatic = value; }

public:
    Abc::Int32ArraySamplePtr m_faceIndices;
    Abc::Int32ArraySamplePtr m_vertexCountPerFace;

    std::vector<int32_t> m_indicesSwapedFaceWinding;
    std::vector<uint32_t> m_UvIndicesSwapedFaceWinding;
    std::vector<int> m_faceSplitIndices;
    std::vector<int> m_tangentIndices;
    std::vector<SplitInfo> m_splits;
    std::vector<uint32_t> m_FixedTopoPositionsIndexes;
    std::vector<uint32_t> m_FaceIndexingReindexed;
    Submeshes m_submeshes;

    int m_triangulatedIndexCount = 0;
    int m_tangentsCount = 0;

    bool m_vertexSharingEnabled = false;
    bool m_FreshlyReadTopologyData = false;
    bool m_TreatVertexExtraDataAsStatic = false;
    bool m_use32BitsIndexBuffer = false;
};

// ---

class aiPolyMeshSample : public aiSampleBase
{
typedef aiSampleBase super;
public:
    aiPolyMeshSample(aiPolyMesh *schema, Topology *topo, bool ownTopo );
    virtual ~aiPolyMeshSample();

    void updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged) override;
    
    bool hasNormals() const;
    bool hasUVs() const;
    bool hasVelocities() const;
    bool hasTangents() const;
    bool smoothNormalsRequired() const;
    bool tangentsRequired() const;

    void getSummary(bool forceRefresh, aiMeshSampleSummary &summary, aiPolyMeshSample* sample) const;
    void getDataPointer(aiPolyMeshData &data) const;
    void copyData(aiPolyMeshData &data);
    void copyDataWithTriangulation(aiPolyMeshData &data, bool always_expand_indices);

    void computeTangentIndices(const aiConfig &config, const abcV3 *N, bool Nindexed) const;
    void computeTangents(const aiConfig &config, const abcV3 *N, bool Nindexed);
    void computeSmoothNormals(const aiConfig &config);

    int getVertexBufferLength(int splitIndex) const;
    void fillVertexBuffer(int splitIndex, aiPolyMeshData &data);

    int prepareSubmeshes(aiPolyMeshSample* sample);
    int getSplitSubmeshCount(int splitIndex) const;
    bool getNextSubmesh(aiSubmeshSummary &summary);
    void fillSubmeshIndices(const aiSubmeshSummary &summary, aiSubmeshData &data) const;

public:
    Abc::P3fArraySamplePtr m_positions;
    Abc::P3fArraySamplePtr m_nextPositions;
    Abc::V3fArraySamplePtr m_velocities;
    AbcGeom::IN3fGeomParam::Sample m_normals;
    AbcGeom::IV2fGeomParam::Sample m_uvs;
    Abc::Box3d m_bounds;
    abcFaceSetSamples m_facesets;

    Topology *m_topology = nullptr;
    bool m_ownTopology = false;

    std::vector<abcV3> m_smoothNormals;
    std::vector<abcV4> m_tangents;

    Submeshes::iterator m_curSubmesh;
};


struct aiPolyMeshTraits
{
    typedef aiPolyMeshSample SampleT;
    typedef AbcGeom::IPolyMeshSchema AbcSchemaT;
};

class aiPolyMesh : public aiTSchema<aiPolyMeshTraits>
{
using super = aiTSchema<aiPolyMeshTraits>;
public:
    aiPolyMesh(aiObject *obj);
    Sample* newSample();
    Sample* readSample(const uint64_t idx, bool &topologyChanged) override;

    int getPeakIndexCount() const;
    int getPeakTriangulatedIndexCount() const;
    int getPeakVertexCount() const;

    void getSummary(aiMeshSummary &summary) const;

private:
    void updatePeakIndexCount() const;
    void GenerateVerticesToFacesLookup(aiPolyMeshSample *sample) const;

private:
    mutable int m_peakIndexCount;
    mutable int m_peakTriangulatedIndexCount;
    mutable int m_peakVertexCount;

    bool m_ignoreNormals;
    bool m_ignoreUVs;

    Topology m_sharedTopology;
    AbcGeom::IN3fGeomParam::Sample m_sharedNormals;
    AbcGeom::IV2fGeomParam::Sample m_sharedUVs;
    abcFaceSetSchemas m_facesets;
};
