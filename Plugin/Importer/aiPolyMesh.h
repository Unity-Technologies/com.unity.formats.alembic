#ifndef aiPolyMesh_h
#define aiPolyMesh_h

typedef std::vector<size_t> Faceset;
typedef std::vector<Faceset> Facesets;

struct SubmeshKey
{
    int uTile;
    int vTile;
    int facesetIndex;
    int splitIndex;

    inline SubmeshKey(float u, float v, int fi=-1, int si=0)
        : uTile((int)floor(u))
        , vTile((int)floor(v))
        , facesetIndex(fi)
        , splitIndex(si)
    {
    }

    SubmeshKey(const SubmeshKey&) = default;
    SubmeshKey& operator=(const SubmeshKey&) = default;

    inline bool operator<(const SubmeshKey &rhs) const
    {
        if (splitIndex < rhs.splitIndex) return true;
        if (splitIndex > rhs.splitIndex) return false;
        
        if (facesetIndex < rhs.facesetIndex) return true;
        if (facesetIndex > rhs.facesetIndex) return false;
        
        if (uTile < rhs.uTile) return true;
        if (uTile > rhs.uTile) return false;

        return (vTile < rhs.vTile);
    }
};

struct SplitInfo
{
    size_t firstFace;
    size_t lastFace;
    size_t indexOffset;
    size_t vertexCount;
    size_t submeshCount;

    inline SplitInfo(size_t ff=0, size_t io=0)
        : firstFace(ff)
        , lastFace(ff)
        , indexOffset(io)
        , vertexCount(0)
        , submeshCount(0)
    {
    }

    SplitInfo(const SplitInfo&) = default;
    SplitInfo& operator=(const SplitInfo&) = default;
};

struct Submesh
{
    Faceset faces;
    std::vector<int> vertexIndices;
    size_t triangleCount;
    int facesetIndex;
    int splitIndex;
    int index; // submesh index in split

    inline Submesh(int fsi=-1, int si=0)
        : triangleCount(0)
        , facesetIndex(fsi)
        , splitIndex(si)
        , index(0)
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
    ~Topology();

    int getSplitCount() const;
    int getSplitCount(bool forceRefresh);

    void clear();
    
    void updateSplits();

    int getVertexBufferLength(int splitIndex) const;
    int prepareSubmeshes(const AbcGeom::IV2fGeomParam::Sample &uvs, const aiFacesets &inFacesets, bool submeshPerUVTile);
    int getSplitSubmeshCount(int splitIndex) const;

    inline Submeshes::iterator submeshBegin() { return m_submeshes.begin(); }
    inline Submeshes::iterator submeshEnd() { return m_submeshes.end(); }

    inline Submeshes::const_iterator submeshBegin() const { return m_submeshes.begin(); }
    inline Submeshes::const_iterator submeshEnd() const { return m_submeshes.end(); }

public:
    Abc::Int32ArraySamplePtr m_indices;
    Abc::Int32ArraySamplePtr m_counts;

    Submeshes m_submeshes;

    std::vector<int> m_faceSplitIndices;
    std::vector<SplitInfo> m_splits;

    size_t m_tangentIndicesCount;
    int *m_tangentIndices;
    size_t m_tangentsCount;
};

// ---

class aiPolyMeshSample : public aiSampleBase
{
typedef aiSampleBase super;
public:
    aiPolyMeshSample(aiPolyMesh *schema, Topology *topo, bool ownTopo);
    virtual ~aiPolyMeshSample();

    void updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged) override;
    
    bool hasNormals() const;
    bool hasUVs() const;
    bool hasTangents() const;
    bool smoothNormalsRequired() const;
    bool tangentsRequired() const;

    void getSummary(bool forceRefresh, aiMeshSampleSummary &summary) const;
    void getDataPointer(aiMeshSampleData &data);
    void copyData(aiMeshSampleData &data);

    void computeTangentIndices(const aiConfig &config, const abcV3 *N, bool Nindexed);
    void computeTangents(const aiConfig &config, const abcV3 *N, bool Nindexed);
    void computeSmoothNormals(const aiConfig &config);

    int getVertexBufferLength(int splitIndex) const;
    void fillVertexBuffer(int splitIndex, aiMeshSampleData &data);

    int prepareSubmeshes(const aiFacesets &inFacesets);
    int getSplitSubmeshCount(int splitIndex) const;
    bool getNextSubmesh(aiSubmeshSummary &summary);
    void fillSubmeshIndices(const aiSubmeshSummary &summary, aiSubmeshData &data) const;

public:
    Topology *m_topology;
    bool m_ownTopology;

    Abc::P3fArraySamplePtr m_positions;
    Abc::V3fArraySamplePtr m_velocities;
    AbcGeom::IN3fGeomParam::Sample m_normals;
    AbcGeom::IV2fGeomParam::Sample m_uvs;
    Abc::Box3d m_bounds;

    size_t m_smoothNormalsCount;
    abcV3 *m_smoothNormals; 
    size_t m_tangentsCount;
    abcV4 *m_tangents;

    Submeshes::iterator m_curSubmesh;
};


struct aiPolyMeshTraits
{
    typedef aiPolyMeshSample SampleT;
    typedef AbcGeom::IPolyMeshSchema AbcSchemaT;
};

class aiPolyMesh : public aiTSchema<aiPolyMeshTraits>
{
typedef aiTSchema<aiPolyMeshTraits> super;
public:
    aiPolyMesh(aiObject *obj);

    Sample* newSample();
    Sample* readSample(float time, bool &topologyChanged) override;

    int getTopologyVariance() const;
    int getPeakIndexCount() const;
    int getPeakVertexCount() const;

    void getSummary(aiMeshSummary &summary) const;

private:
    mutable int m_peakIndexCount;
    mutable int m_peakVertexCount;

    bool m_ignoreNormals;
    bool m_ignoreUVs;

    Topology m_sharedTopology;
    AbcGeom::IN3fGeomParam::Sample m_sharedNormals;
    AbcGeom::IV2fGeomParam::Sample m_sharedUVs;
};

#endif
