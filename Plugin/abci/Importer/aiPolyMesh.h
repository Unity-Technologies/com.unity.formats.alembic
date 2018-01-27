#pragma once
#include "../Foundation/aiMeshOps.h"

using abcFaceSetSchemas = std::vector<AbcGeom::IFaceSetSchema>;
using abcFaceSetSamples = std::vector<AbcGeom::IFaceSetSchema::Sample>;
template<class T> using SharedVector = std::shared_ptr<RawVector<T>>;

struct aiMeshSummaryInternal : public aiMeshSummary
{
    bool has_velocities_prop = false;
    bool has_normals_prop = false;
    bool has_uv0_prop = false;
    bool has_uv1_prop = false;
    bool has_colors_prop = false;

    bool interpolate_points = false;
    bool interpolate_normals = false;
    bool compute_normals = false;
    bool compute_tangents = false;
    bool compute_velocities = false;
};

class aiMeshTopology
{
public:
    aiMeshTopology();
    void clear();

    int getTriangulatedIndexCount() const;
    int getSplitCount() const;

    int getSplitVertexCount(int split_index) const;
    int getSubmeshCount() const;
    int getSubmeshCount(int split_index) const;

    void onTopologyUpdate(const aiConfig &config, aiPolyMeshSample& sample);

public:
    Abc::Int32ArraySamplePtr m_indices_sp;
    Abc::Int32ArraySamplePtr m_counts_sp;
    RawVector<int> m_material_ids;

    MeshRefiner m_refiner;
    RawVector<int> m_remap_points, m_remap_normals, m_remap_uv0, m_remap_uv1, m_remap_colors;

    int m_triangulated_index_count = 0;
    bool m_freshly_read_topology_data = false;
    bool m_use_32bit_index_buffer = false;
};
using TopologyPtr = std::shared_ptr<aiMeshTopology>;


class aiPolyMeshSample : public aiSampleBase
{
using super = aiSampleBase;
using schema_t = aiPolyMesh;
public:
    aiPolyMeshSample(aiPolyMesh *schema, TopologyPtr topo, bool ownTopo );

    void updateConfig(const aiConfig &config, bool &topology_changed, bool &data_changed) override;
    
    void getSummary(bool force_refresh, aiMeshSampleSummary &summary, aiPolyMeshSample* sample) const;

    void computeNormals(const aiConfig &config);
    void computeTangents(const aiConfig &config);

    int getSplitVertexCount(int split_index) const;
    void prepareSplits();
    void fillSplitVertices(int split_index, aiPolyMeshData &data);

    int getSubmeshCount(int split_index) const;
    void getSubmeshSummary(int split_index, int submesh_index, aiSubmeshSummary &summary);
    void fillSubmeshIndices(int split_index, int submesh_index, aiSubmeshData &data) const;

public:
    Abc::P3fArraySamplePtr m_points_sp, m_points_sp2;
    Abc::V3fArraySamplePtr m_velocities_sp;
    AbcGeom::IN3fGeomParam::Sample m_normals_sp, m_normals_sp2;
    AbcGeom::IV2fGeomParam::Sample m_uv0_sp, m_uv1_sp;
    AbcGeom::IC4fGeomParam::Sample m_colors_sp;
    Abc::Box3d m_bounds;
    abcFaceSetSamples m_facesets;

    RawVector<abcV3> m_points, m_points2;
    RawVector<abcV3> m_velocities;
    RawVector<abcV2> m_uv0, m_uv1;
    RawVector<abcV3> m_normals, m_normals2;
    RawVector<abcV4> m_tangents;
    RawVector<abcC4> m_colors;

    TopologyPtr m_topology;
    bool m_own_topology = false;
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
    void updateSummary();

    Sample* newSample();
    Sample* readSample(const uint64_t idx, bool &topology_changed) override;

    const aiMeshSummaryInternal& getSummary() const;
    void getSummary(aiMeshSummary &summary) const;

private:
    aiMeshSummaryInternal m_summary;
    std::unique_ptr<AbcGeom::IV2fGeomParam> m_uv1_param;
    std::unique_ptr<AbcGeom::IC4fGeomParam> m_colors_param;

    TopologyPtr m_shared_topology;
    abcFaceSetSchemas m_facesets;
    RawVector<abcV3> m_constant_points;
    RawVector<abcV3> m_constant_velocities;
    RawVector<abcV3> m_constant_normals;
    RawVector<abcV4> m_constant_tangents;
    RawVector<abcV2> m_constant_uv0;
    RawVector<abcV2> m_constant_uv1;
    RawVector<abcC4> m_constant_colors;

    bool m_ignore_normals = false;
    bool m_ignore_uvs = false;
};
