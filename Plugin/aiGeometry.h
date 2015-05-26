#ifndef aiGeometry_h
#define aiGeometry_h


class aiSchema
{
public:
    virtual ~aiSchema();
    virtual void updateSample() = 0;
};


class aiXForm : public aiSchema
{
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
    aiObject *m_obj;
    AbcGeom::IXformSchema m_schema;
    AbcGeom::XformSample m_sample;
    bool m_inherits;
};


class aiPolyMesh : public aiSchema
{
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

private:
    aiObject *m_obj;
    AbcGeom::IPolyMeshSchema m_schema;
    Abc::Int32ArraySamplePtr m_indices;
    Abc::Int32ArraySamplePtr m_counts;
    Abc::P3fArraySamplePtr m_positions;
    AbcGeom::IN3fGeomParam::Sample m_normals;
    AbcGeom::IV2fGeomParam::Sample m_uvs;
    Abc::V3fArraySamplePtr m_velocities;
};


class aiCurves : public aiSchema
{
public:
    aiCurves();
    aiCurves(aiObject *obj);
    void updateSample() override;

private:
    aiObject *m_obj;
    AbcGeom::ICurvesSchema m_schema;
};


class aiPoints : public aiSchema
{
public:
    aiPoints();
    aiPoints(aiObject *obj);
    void updateSample() override;

private:
    aiObject *m_obj;
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
public:
    aiCamera();
    aiCamera(aiObject *obj);
    void updateSample() override;

    void getParams(aiCameraParams &o_params);

private:
    aiObject *m_obj;
    AbcGeom::ICameraSchema m_schema;
    AbcGeom::CameraSample m_sample;
};


class aiMaterial : public aiSchema
{
public:
    aiMaterial();
    aiMaterial(aiObject *obj);
    void updateSample() override;

    // Maya の alembic エクスポータがマテリアル情報を書き出せないようなので保留。

private:
    aiObject *m_obj;
    AbcMaterial::IMaterialSchema m_schema;
};



#endif // aiGeometry_h
