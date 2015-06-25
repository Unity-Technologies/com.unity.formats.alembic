#ifndef aiGeometry_h
#define aiGeometry_h


class aiSchema
{
public:
    aiSchema();
    aiSchema(aiObject *obj);
    virtual ~aiSchema();
    virtual void updateSample() = 0;

protected:
    aiObject *m_obj;
};


class aiXForm : public aiSchema
{
typedef aiSchema super;
public:
    aiXForm();
    aiXForm(aiObject *obj);
    void updateSample() override;

    bool        getInherits() const;
    abcV3       getPosition() const;
    abcV3       getAxis() const;
    float       getAngle() const;
    abcV3       getScale() const;
    abcM44      getMatrix() const;

private:
    AbcGeom::IXformSchema m_schema;
    AbcGeom::XformSample m_sample;
    bool m_inherits;
};


class aiPolyMesh : public aiSchema
{
typedef aiSchema super;
public:
    aiPolyMesh();
    aiPolyMesh(aiObject *obj);
    void updateSample() override;

    void        setCurrentTime(float t);
    void        enableReverseX(bool v);
    void        enableTriangulate(bool v);
    void        enableReverseIndex(bool v);

    int         getTopologyVariance() const;
    bool        hasNormals() const;
    bool        hasUVs() const;

    uint32_t    getIndexCount() const;
    uint32_t    getVertexCount() const;
    void        copyIndices(int *dst) const;
    void        copyVertices(abcV3 *dst) const;
    void        copyNormals(abcV3 *dst) const;
    void        copyUVs(abcV2 *dst) const;

    bool        getSplitedMeshInfo(aiSplitedMeshInfo &o_sp, const aiSplitedMeshInfo& prev, int max_vertices) const;
    void        copySplitedIndices(int *dst, const aiSplitedMeshInfo &smi) const;
    void        copySplitedVertices(abcV3 *dst, const aiSplitedMeshInfo &smi) const;
    void        copySplitedNormals(abcV3 *dst, const aiSplitedMeshInfo &smi) const;
    void        copySplitedUVs(abcV2 *dst, const aiSplitedMeshInfo &smi) const;

    uint32_t    getSplitCount() const;
    uint32_t    getSplitCount(bool force_refresh);
    uint32_t    getVertexBufferLength(uint32_t split_index) const;
    void        fillVertexBuffer(uint32_t split_index, abcV3 *positions, abcV3 *normals, abcV2 *uvs) const;
    uint32_t    prepareSubmeshes(const aiFacesets *facesets);
    uint32_t    getSplitSubmeshCount(uint32_t split_index) const;
    bool        getNextSubmesh(aiSubmeshInfo &o_smi);
    void        fillSubmeshIndices(int *dst, const aiSubmeshInfo &smi) const;

private:

    template <typename NormalIndexArray>
    void copySplitedNormals(abcV3 *dst, const NormalIndexArray &indices, const aiSplitedMeshInfo &smi, float x_scale) const
    {
        const auto &counts = *m_counts;
        const auto &normals = *m_normals.getVals();
        
        uint32_t a = 0;
        
        for (int fi = 0; fi < smi.num_faces; ++fi)
        {
            int ngon = counts[smi.begin_face + fi];
            for (int ni = 0; ni < ngon; ++ni)
            {
                dst[a + ni] = normals[indices[a + ni + smi.begin_index]];
                dst[a + ni].x *= x_scale;
            }
            a += ngon;
        }
    }

    void updateSplits();

    typedef std::set<size_t> Faceset;
    typedef std::vector<Faceset> Facesets;
    
    struct SubmeshID
    {
        int u_tile;
        int v_tile;
        int faceset_index;
        int split_index;

        inline SubmeshID(float u, float v, int fi=-1, int si=0)
            : u_tile((int)floor(u))
            , v_tile((int)floor(v))
            , faceset_index(fi)
            , split_index(si)
        {
        }

        inline bool operator<(const SubmeshID &rhs) const
        {
            if (split_index < rhs.split_index)
            {
                return true;
            }
            else if (split_index == rhs.split_index)
            {
                if (faceset_index < rhs.faceset_index)
                {
                    return true;
                }
                else if (faceset_index == rhs.faceset_index)
                {
                    if (u_tile < rhs.u_tile)
                    {
                        return true;
                    }
                    else if (u_tile == rhs.u_tile)
                    {
                        return (v_tile < rhs.v_tile);
                    }
                }
            }
            return false;
        }
    };

    struct SplitInfo
    {
        size_t first_face;
        size_t last_face;
        size_t index_offset;
        size_t indices_count;
        size_t submesh_count;

        inline SplitInfo(size_t ff=0, size_t io=0)
            : first_face(ff)
            , last_face(ff)
            , index_offset(io)
            , indices_count(0)
            , submesh_count(0)
        {
        }
    };

    struct Submesh
    {
        // original faceset if had to be splitted
        Faceset faces;
        std::vector<int> vertex_indices;
        size_t triangle_count;
        int faceset_index;
        int split_index;
        int index; // submesh index in split

        inline Submesh(int fsi=-1, int si=0)
            : triangle_count(0)
            , faceset_index(fsi)
            , split_index(si)
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
    Submeshes::iterator m_cur_submesh;

    // Need to split mesh to work around the 65000 vertices per mesh limit
    std::vector<int> m_face_split_indices;
    std::vector<SplitInfo> m_splits;
};


class aiCurves : public aiSchema
{
typedef aiSchema super;
public:
    aiCurves();
    aiCurves(aiObject *obj);
    void updateSample() override;

private:
    AbcGeom::ICurvesSchema m_schema;
};


class aiPoints : public aiSchema
{
typedef aiSchema super;
public:
    aiPoints();
    aiPoints(aiObject *obj);
    void updateSample() override;

private:
    AbcGeom::IPointsSchema m_schema;
};



struct aiCameraParams
{
    float near_clipping_plane;
    float far_clipping_plane;
    float field_of_view;
    float focus_distance;
    float focal_length;
};

class aiCamera : public aiSchema
{
typedef aiSchema super;
public:
    aiCamera();
    aiCamera(aiObject *obj);
    void updateSample() override;

    void getParams(aiCameraParams &o_params);

private:
    AbcGeom::ICameraSchema m_schema;
    AbcGeom::CameraSample m_sample;
};


class aiLight : public aiSchema
{
typedef aiSchema super;
public:
    aiLight();
    aiLight(aiObject *obj);
    void updateSample() override;

private:
    AbcGeom::ILightSchema m_schema;
};


class aiMaterial : public aiSchema
{
typedef aiSchema super;
public:
    aiMaterial();
    aiMaterial(aiObject *obj);
    void updateSample() override;

    // Maya の alembic エクスポータがマテリアル情報を書き出せないようなので保留。

private:
    AbcMaterial::IMaterialSchema m_schema;
};

#endif // aiGeometry_h
