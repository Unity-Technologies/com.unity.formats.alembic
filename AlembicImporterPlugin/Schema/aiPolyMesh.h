#ifndef aiPolyMesh_h
#define aiPolyMesh_h

class aiPolyMesh : public aiSchema
{
typedef aiSchema super;
public:
    aiPolyMesh();
    aiPolyMesh(aiObject *obj);
    virtual ~aiPolyMesh();

    void updateSample() override;

    int         getTopologyVariance() const;
    bool        hasNormals() const;
    bool        hasUVs() const;

    uint32_t    getSplitCount() const;
    uint32_t    getSplitCount(bool forceRefresh);
    
    uint32_t    getVertexBufferLength(uint32_t splitIndex) const;
    void        fillVertexBuffer(uint32_t splitIndex, abcV3 *positions, abcV3 *normals, abcV2 *uvs, abcV4 *T);
    
    uint32_t    prepareSubmeshes(const aiFacesets *facesets);
    uint32_t    getSplitSubmeshCount(uint32_t splitIndex) const;
    bool        getNextSubmesh(aiSubmeshInfo &smi);
    void        fillSubmeshIndices(int *dst, const aiSubmeshInfo &smi) const;

private:

    void updateSplits();
    bool updateUVs(Abc::ISampleSelector &ss);
    bool updateNormals(Abc::ISampleSelector &ss);

    bool smoothNormalsRequired() const;
    bool smoothNormalsUpdateRequired() const;
    void updateSmoothNormals();

    bool tangentsRequired() const;
    bool tangentsUpdateRequired() const;
    bool tangentsUseSmoothNormals() const;
    void updateTangents(bool smooth, const Abc::V3f *inN, bool indexedNormals);

    typedef std::set<size_t> Faceset;
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
        size_t indicesCount;
        size_t submeshCount;

        inline SplitInfo(size_t ff=0, size_t io=0)
            : firstFace(ff)
            , lastFace(ff)
            , indexOffset(io)
            , indicesCount(0)
            , submeshCount(0)
        {
        }
    };

    struct Submesh
    {
        // original faceset if had to be splitted
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
    };

    typedef std::deque<Submesh> Submeshes;

private:

    AbcGeom::IPolyMeshSchema m_schema;
    Abc::Int32ArraySamplePtr m_indices;
    Abc::Int32ArraySamplePtr m_counts;
    Abc::P3fArraySamplePtr m_positions;
    AbcGeom::IN3fGeomParam::Sample m_normals;
    AbcGeom::IV2fGeomParam::Sample m_uvs;
    Abc::V3fArraySamplePtr m_velocities;

    Submeshes m_submeshes;
    Submeshes::iterator m_curSubmesh;

    // Need to split mesh to work around the 65000 vertices per mesh limit
    std::vector<int> m_faceSplitIndices;
    std::vector<SplitInfo> m_splits;

    bool m_smoothNormalsDirty;
    size_t m_smoothNormalsCount;
    Abc::V3f *m_smoothNormals;
    bool m_smoothNormalsCCW;

    bool m_tangentsDirty;
    size_t m_tangentIndicesCount;
    int *m_tangentIndices;
    size_t m_tangentsCount;
    Imath::V4f *m_tangents;
    bool m_tangentsSmooth;
    bool m_tangentsCCW;
    bool m_tangentsUseSmoothNormals;
};

#endif
