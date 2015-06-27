#ifndef aiPolyMesh_h
#define aiPolyMesh_h

struct aiPolyMeshSummary
{
    uint32_t index_count;
    uint32_t vertex_count;
    uint8_t has_normals;
    uint8_t has_uvs;
    uint8_t has_velocities;
    uint8_t is_notmal_indexed;
    uint8_t is_uv_indexed;

    aiPolyMeshSummary() { memset(this, 0 , sizeof(*this)); }
};

struct aiSplitedMeshData
{
    int num_faces;
    int num_indices;
    int num_vertices;
    int begin_face;
    int begin_index;
    int triangulated_index_count;

    int *dst_index;
    abcV3 *dst_vertices;
    abcV3 *dst_normals;
    abcV2 *dst_uvs;
    abcV3 *dst_velocities;

    aiSplitedMeshData() { memset(this, 0, sizeof(*this)); }
};

struct aiTextureMeshData
{
    // in
    int tex_width;

    // out
    int index_count;
    int vertex_count;
    int is_normal_indexed;
    int is_uv_indexed;
    int pad;
    void *tex_indices;
    void *tex_vertices;
    void *tex_normals;
    void *tex_uvs;
    void *tex_velocities;

    aiTextureMeshData() { memset(this, 0, sizeof(*this)); }
};


class aiPolyMeshSample : public aiSampleBase
{
typedef aiSampleBase super;
public:
    aiPolyMeshSample(aiPolyMesh *schema, aiIndex index);

    bool        hasNormals() const;
    bool        hasUVs() const;
    bool        hasVelocities() const;
    bool        isNormalIndexed() const;
    bool        isUVIndexed() const;
    uint32_t    getIndexCount() const;
    uint32_t    getVertexCount() const;

    void        copyIndices(int *dst) const;
    void        copyVertices(abcV3 *dst) const;
    void        copyVelocities(abcV3 *dst) const;
    void        copyNormals(abcV3 *dst) const;
    void        copyUVs(abcV2 *dst) const;

    void        getSummary(aiPolyMeshSummary &o_summary) const;
    bool        getSplitedMeshInfo(aiSplitedMeshData &o_sp, const aiSplitedMeshData& prev, int max_vertices) const;
    void        copySplitedMesh(aiSplitedMeshData &o_sp) const;
    void        copySplitedIndices(int *dst, const aiSplitedMeshData &smi) const;
    void        copySplitedVertices(abcV3 *dst, const aiSplitedMeshData &smi) const;
    void        copySplitedNormals(abcV3 *dst, const aiSplitedMeshData &smi) const;
    void        copySplitedUVs(abcV2 *dst, const aiSplitedMeshData &smi) const;

#ifdef aiSupportTextureMesh
    void        copyMeshToTexture(aiTextureMeshData &dst) const;
#endif // aiSupportTextureMesh

public:
    Abc::Int32ArraySamplePtr        m_indices;
    Abc::Int32ArraySamplePtr        m_counts;
    Abc::P3fArraySamplePtr          m_positions;
    AbcGeom::IN3fGeomParam::Sample  m_normals;
    AbcGeom::IV2fGeomParam::Sample  m_uvs;
    Abc::V3fArraySamplePtr          m_velocities;

    mutable std::vector<float> m_buf;
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
    Sample* readSample(float time) override;
    void debugDump() const override;

    int         getTopologyVariance() const;
    uint32_t    getPeakIndexCount() const;
    uint32_t    getPeakVertexCount() const;

private:
    mutable uint32_t m_peak_index_count;
    mutable uint32_t m_peak_vertex_count;
};

#endif // aiPolyMesh_h
