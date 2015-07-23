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

    uint32_t    getIndexCount() const;
    uint32_t    getVertexCount() const;
    void        copyIndices(int *dst) const;
    void        copyVertices(abcV3 *dst) const;
    void        copyNormals(abcV3 *dst) const;
    void        copyUVs(abcV2 *dst) const;

    bool        getSplitedMeshInfo(aiSplitedMeshInfo &sp, const aiSplitedMeshInfo& prev, int maxVertices) const;
    void        copySplitedIndices(int *dst, const aiSplitedMeshInfo &smi) const;
    void        copySplitedVertices(abcV3 *dst, const aiSplitedMeshInfo &smi) const;
    void        copySplitedNormals(abcV3 *dst, const aiSplitedMeshInfo &smi) const;
    void        copySplitedUVs(abcV2 *dst, const aiSplitedMeshInfo &smi) const;

    uint32_t    getSplitCount() const;
    uint32_t    getSplitCount(bool forceRefresh);
    uint32_t    getVertexBufferLength(uint32_t splitIndex) const;
    void        fillVertexBuffer(uint32_t splitIndex, abcV3 *positions, abcV3 *normals, abcV2 *uvs, abcV4 *T);
    uint32_t    prepareSubmeshes(const aiFacesets *facesets);
    uint32_t    getSplitSubmeshCount(uint32_t splitIndex) const;
    bool        getNextSubmesh(aiSubmeshInfo &smi);
    void        fillSubmeshIndices(int *dst, const aiSubmeshInfo &smi) const;

private:

    template <typename NormalIndexArray>
    void copySplitedNormals(abcV3 *dst, const NormalIndexArray &indices, const aiSplitedMeshInfo &smi, float xScale) const
    {
        const auto &counts = *m_counts;
        const auto &normals = *m_normals.getVals();
        
        uint32_t a = 0;
        
        for (int fi = 0; fi < smi.numFaces; ++fi)
        {
            int ngon = counts[smi.beginFace + fi];
            for (int ni = 0; ni < ngon; ++ni)
            {
                dst[a + ni] = normals[indices[a + ni + smi.beginIndex]];
                dst[a + ni].x *= xScale;
            }
            a += ngon;
        }
    }

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
    
    struct SubmeshID
    {
        int uTile;
        int vTile;
        int facesetIndex;
        int splitIndex;

        inline SubmeshID(float u, float v, int fi=-1, int si=0)
            : uTile((int)floor(u))
            , vTile((int)floor(v))
            , facesetIndex(fi)
            , splitIndex(si)
        {
        }

        inline bool operator<(const SubmeshID &rhs) const
        {
            if (splitIndex < rhs.splitIndex)
            {
                return true;
            }
            else if (splitIndex == rhs.splitIndex)
            {
                if (facesetIndex < rhs.facesetIndex)
                {
                    return true;
                }
                else if (facesetIndex == rhs.facesetIndex)
                {
                    if (uTile < rhs.uTile)
                    {
                        return true;
                    }
                    else if (uTile == rhs.uTile)
                    {
                        return (vTile < rhs.vTile);
                    }
                }
            }
            return false;
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
