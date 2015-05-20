#ifndef aiGeometry_h
#define aiGeometry_h


class aiXForm
{
public:
    aiXForm();
    aiXForm(abcObject obj, Abc::ISampleSelector ss);

    void        enableReverseX(bool v);

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

    bool m_reverse_x;
};


class aiPolyMesh
{
public:
    aiPolyMesh();
    aiPolyMesh(abcObject obj, Abc::ISampleSelector ss);

    void        enableReverseX(bool v);
    void        enableTriangulate(bool v);
    void        enableReverseIndex(bool v);

    bool        isTopologyConstant() const;
    bool        isTopologyConstantTriangles() const;
    bool        isNormalIndexed() const;
    bool        isUVIndexed() const;
    uint32_t    getIndexCount() const;
    uint32_t    getVertexCount() const;
    void        copyIndices(int *dst) const;
    void        copyVertices(abcV3 *dst) const;

    bool        getSplitedMeshInfo(aiSplitedMeshInfo &o_sp, const aiSplitedMeshInfo& prev, int max_vertices) const;
    void        copySplitedIndices(int *dst, const aiSplitedMeshInfo &smi) const;
    void        copySplitedVertices(abcV3 *dst, const aiSplitedMeshInfo &smi) const;

private:
    AbcGeom::IPolyMeshSchema m_schema;
    Abc::Int32ArraySamplePtr m_indices;
    Abc::Int32ArraySamplePtr m_counts;
    Abc::P3fArraySamplePtr m_positions;
    Abc::V3fArraySamplePtr m_velocities;
    bool m_reverse_x;
    bool m_triangulate;
    bool m_reverse_index;
};


class aiCurves
{
public:
    aiCurves();
    aiCurves(abcObject obj, Abc::ISampleSelector ss);

    void        enableReverseX(bool v);

private:
    AbcGeom::ICurvesSchema m_schema;
    bool m_reverse_x;
};


class aiPoints
{
public:
    aiPoints();
    aiPoints(abcObject obj, Abc::ISampleSelector ss);

    void        enableReverseX(bool v);

private:
    AbcGeom::IPointsSchema m_schema;
    bool m_reverse_x;
};



struct aiCameraParams
{
    float near_clipping_plane;
    float far_clipping_plane;
    float field_of_view;
    float focus_distance;
    float focal_length;
};

class aiCamera
{
public:
    aiCamera();
    aiCamera(abcObject obj, Abc::ISampleSelector ss);

    void getParams(aiCameraParams &o_params);

private:
    AbcGeom::ICameraSchema m_schema;
    AbcGeom::CameraSample m_sample;
};


class aiMaterial
{
public:
    aiMaterial();
    aiMaterial(abcObject obj, Abc::ISampleSelector ss);

    // Maya の alembic エクスポータがマテリアル情報を書き出せないようなので保留。

private:
    AbcMaterial::IMaterialSchema m_schema;
};



#endif // aiGeometry_h
