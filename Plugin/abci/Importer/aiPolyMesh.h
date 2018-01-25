#pragma once

using abcFaceSetSchemas = std::vector<AbcGeom::IFaceSetSchema>;
using abcFaceSetSamples = std::vector<AbcGeom::IFaceSetSchema::Sample>;

struct SplitInfo
{
    int first_face = 0;
    int last_face = 0;
    int index_offset = 0;
    int vertex_count = 0;
    int submesh_count = 0;

    inline SplitInfo(int ff=0, int io=0)
        : first_face(ff)
        , last_face(ff)
        , index_offset(io)
    {
    }

    SplitInfo(const SplitInfo&) = default;
    SplitInfo& operator=(const SplitInfo&) = default;
};

struct Submesh
{
    RawVector<int> faces;
    RawVector<uint32_t> indices;
    int index_count = 0;
    int triangle_count = 0;
    int face_count = 0;
    int split_index = 0;
    int submesh_index = 0; // submesh index in split
};
using SubmeshPtr = std::shared_ptr<Submesh>;
using SubmeshPtrs = std::vector<SubmeshPtr>;

class Topology
{
public:
    Topology();
    void clear();

    int getTriangulatedIndexCount() const;
    int getSplitCount() const;
    int getSplitCount(aiPolyMeshSample * meshSample, bool force_refresh);
    void updateSplits(aiPolyMeshSample * meshSample);

    int getVertexBufferLength(int split_index) const;
    int prepareSubmeshes(abcFaceSetSamples& fs, aiPolyMeshSample* sample);
    int getSplitSubmeshCount(int split_index) const;

    inline SubmeshPtrs::iterator submeshBegin() { return m_submeshes.begin(); }
    inline SubmeshPtrs::iterator submeshEnd() { return m_submeshes.end(); }

    inline SubmeshPtrs::const_iterator submeshBegin() const { return m_submeshes.begin(); }
    inline SubmeshPtrs::const_iterator submeshEnd() const { return m_submeshes.end(); }

    inline void enableVertexSharing(bool value) { m_vertex_sharing_enabled = value; }
    inline void enable32BitsIndexbuffers(bool value) { m_use_32bit_index_buffer = value; }
    inline void treatVertexExtraDataAsStatic(bool value) { m_treat_vertex_extra_data_as_static = value; }

public:
    Abc::Int32ArraySamplePtr m_face_indices;
    Abc::Int32ArraySamplePtr m_counts;
    RawVector<int>      m_offsets; // face -> index table

    RawVector<int32_t>  m_indices_swaped_face_winding;
    RawVector<uint32_t> m_uv_indices_swaped_face_winding;
    RawVector<int>      m_face_split_indices;
    RawVector<int>      m_tangent_indices;
    RawVector<uint32_t> m_fixed_topo_positions_indexes;
    RawVector<uint32_t> m_face_indexing_reindexed;
    std::vector<SplitInfo> m_splits;
    SubmeshPtrs m_submeshes;

    int m_triangulated_index_count = 0;
    int m_tangents_count = 0;

    bool m_vertex_sharing_enabled = false;
    bool m_freshly_read_topology_data = false;
    bool m_treat_vertex_extra_data_as_static = false;
    bool m_use_32bit_index_buffer = false;
};
using TopologyPtr = std::shared_ptr<Topology>;


class aiPolyMeshSample : public aiSampleBase
{
using super = aiSampleBase;
public:
    aiPolyMeshSample(aiPolyMesh *schema, TopologyPtr topo, bool ownTopo );
    virtual ~aiPolyMeshSample();

    void updateConfig(const aiConfig &config, bool &topology_changed, bool &data_changed) override;
    
    bool hasNormals() const;
    bool hasUVs() const;
    bool hasVelocities() const;
    bool hasTangents() const;
    bool smoothNormalsRequired() const;
    bool tangentsRequired() const;

    void getSummary(bool force_refresh, aiMeshSampleSummary &summary, aiPolyMeshSample* sample) const;
    void getDataPointer(aiPolyMeshData &data) const;
    void copyData(aiPolyMeshData &data);

    void computeTangentIndices(const aiConfig &config, const abcV3 *N, bool Nindexed) const;
    void computeTangents(const aiConfig &config, const abcV3 *N, bool Nindexed);
    void computeSmoothNormals(const aiConfig &config);

    int getVertexBufferLength(int split_index) const;
    void fillVertexBuffer(int split_index, aiPolyMeshData &data);

    int prepareSubmeshes(aiPolyMeshSample* sample);
    int getSplitSubmeshCount(int split_index) const;
    bool getNextSubmesh(aiSubmeshSummary &summary);
    void fillSubmeshIndices(const aiSubmeshSummary &summary, aiSubmeshData &data) const;

public:
    Abc::P3fArraySamplePtr m_points;
    Abc::P3fArraySamplePtr m_next_points;
    Abc::V3fArraySamplePtr m_velocities;
    AbcGeom::IN3fGeomParam::Sample m_normals;
    AbcGeom::IV2fGeomParam::Sample m_uvs;
    Abc::Box3d m_bounds;
    abcFaceSetSamples m_facesets;

    TopologyPtr m_topology;
    bool m_ownTopology = false;

    RawVector<abcV3> m_smooth_normals;
    RawVector<abcV4> m_tangents;

    SubmeshPtrs::iterator m_cur_submesh;
};


struct aiPolyMeshTraits
{
    using SampleT = aiPolyMeshSample;
    using AbcSchemaT = AbcGeom::IPolyMeshSchema;
};

class aiPolyMesh : public aiTSchema<aiPolyMeshTraits>
{
using super = aiTSchema<aiPolyMeshTraits>;
public:
    aiPolyMesh(aiObject *obj);
    Sample* newSample();
    Sample* readSample(const uint64_t idx, bool &topology_changed) override;

    int getPeakIndexCount() const;
    int getPeakTriangulatedIndexCount() const;
    int getPeakVertexCount() const;

    void getSummary(aiMeshSummary &summary) const;

private:
    void updatePeakIndexCount() const;
    void generateVerticesToFacesLookup(aiPolyMeshSample *sample) const;

private:
    mutable int m_peak_index_count;
    mutable int m_peak_triangulated_index_count;
    mutable int m_peak_vertex_count;

    bool m_ignore_normals;
    bool m_ignore_uvs;

    TopologyPtr m_shared_topology;
    AbcGeom::IN3fGeomParam::Sample m_shared_normals;
    AbcGeom::IV2fGeomParam::Sample m_shared_uvs;
    abcFaceSetSchemas m_facesets;
};
