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

    bool        isTopologyConstant() const;
    bool        isTopologyConstantTriangles() const;
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

    void        prepareSubmeshes(int max_vertices);
    bool        getNextSubmesh(aiSubmeshInfo &o_smi);
    void        copySubmeshIndices(int *dst, const aiSubmeshInfo &smi) const;
    void        copySubmeshVertices(abcV3 *dst, const aiSubmeshInfo &smi) const;
    void        copySubmeshNormals(abcV3 *dst, const aiSubmeshInfo &smi) const;
    void        copySubmeshUVs(abcV2 *dst, const aiSubmeshInfo &smi) const;

private:
    // may be a little more that just that then
    // submesh should also contain vertex indices
    typedef std::set<size_t> Faceset;
    
    typedef Alembic::Util::int64_t UVTileID;

    struct Submesh
    {
        // original faceset if had to be splitted
        Faceset faces;
        std::vector<int> vertex_indices;
        std::vector<int> normal_indices;
        std::vector<int> uv_indices;
        size_t triangle_count;
    };

    typedef std::deque<Submesh> Submeshes;

    struct IndexPassthrough
    {
        inline size_t operator[](size_t idx) const { return idx; }
    };

    UVTileID uvTileID(float u, float v) const;

    static const UVTileID InvalidUVTileID;

    bool fillUVTileFacesets(std::vector<UVTileID> &face_uvtile);

    template <typename NormalIndexArray, typename UVIndexArray>
    void fillSubmeshIndices(const std::vector<UVTileID> &face_uvtile,
                            const NormalIndexArray &normal_indices,
                            const UVIndexArray &uv_indices,
                            size_t max_vertices)
    {
        const auto &counts = *m_counts;
        const auto &indices = *m_indices;

        m_submeshes.clear();

        std::map<UVTileID, std::vector<size_t> > uvtile_submesh_indices;

        size_t vertex_index = 0;
        Submesh *cur_mesh = 0;

        for (size_t f=0; f<counts.size(); ++f)
        {
            int nv = counts[f];

            if (nv == 0)
            {
                continue;
            }

            std::vector<size_t> &submesh_indices = uvtile_submesh_indices[face_uvtile[f]];

            if (submesh_indices.size() == 0 ||
                m_submeshes[submesh_indices.back()].vertex_indices.size() + nv > max_vertices)
            {
                submesh_indices.push_back(m_submeshes.size());
                m_submeshes.push_back(Submesh());

                // allocate indices for position, normals and indices
                m_submeshes.back().vertex_indices.reserve(indices.size());
                m_submeshes.back().normal_indices.reserve(indices.size());
                m_submeshes.back().uv_indices.reserve(indices.size());
            }
            
            cur_mesh = &(m_submeshes[submesh_indices.back()]);
            cur_mesh->faces.insert(f);
            cur_mesh->triangle_count += (nv - 2);

            for (int v=0; v<nv; ++v)
            {
                cur_mesh->vertex_indices.push_back(indices[vertex_index + v]);
                cur_mesh->normal_indices.push_back(normal_indices[vertex_index + v]);
                cur_mesh->uv_indices.push_back(uv_indices[vertex_index + v]);
            }

            vertex_index += nv;
        }
    }

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
