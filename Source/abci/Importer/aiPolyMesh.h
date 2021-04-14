#pragma once
#include "aiMeshOps.h"

using abcFaceSetSchemas = std::vector<AbcGeom::IFaceSetSchema>;
using abcFaceSetSamples = std::vector<AbcGeom::IFaceSetSchema::Sample>;


struct aiMeshSummaryInternal : public aiMeshSummary
{
    bool has_velocities_prop = false;
    bool has_normals_prop = false;
    bool has_uv0_prop = false;
    bool has_uv1_prop = false;
    bool has_rgba_prop = false;
    bool has_rgb_prop = false;

    bool interpolate_points = false;
    bool interpolate_normals = false;
    bool interpolate_uv0 = false;
    bool interpolate_uv1 = false;
    bool interpolate_rgba = false;
    bool interpolate_rgb = false;
    bool compute_normals = false;
    bool compute_tangents = false;
    bool compute_velocities = false;
};

class aiMeshTopology
{
 public:
    aiMeshTopology();
    void clear();

    int getSplitCount() const;
    int getVertexCount() const;
    int getIndexCount() const;

    int getSplitVertexCount(int split_index) const;
    int getSubmeshCount() const;
    int getSubmeshCount(int split_index) const;

 public:
    Abc::Int32ArraySamplePtr m_indices_sp;
    Abc::Int32ArraySamplePtr m_counts_sp;
    abcFaceSetSamples m_faceset_sps;
    RawVector<int> m_material_ids;
    std::vector<std::string> m_faceset_names;

    MeshRefiner m_refiner;
    RawVector<int> m_remap_points;
    RawVector<int> m_remap_normals;
    RawVector<int> m_remap_uv0, m_remap_uv1;
    RawVector<int> m_remap_rgba;
    RawVector<int> m_remap_rgb;

    int m_vertex_count = 0;
    int m_index_count = 0; // triangulated
};
using TopologyPtr = std::shared_ptr<aiMeshTopology>;

class aiPolyMeshSample : public aiSample
{
    using super = aiSample;
    using schema_t = aiPolyMesh;
 public:
    aiPolyMeshSample(aiPolyMesh* schema, TopologyPtr topo);
    ~aiPolyMeshSample();
    void reset();

    void getSummary(aiMeshSampleSummary& dst) const;
    void getSplitSummaries(aiMeshSplitSummary* dst) const;
    void getSubmeshSummaries(aiSubmeshSummary* dst) const;

    void fillSplitVertices(int split_index, aiPolyMeshData& data) const;
    void fillSubmeshIndices(int submesh_index, aiSubmeshData& data) const;
    void fillVertexBuffer(aiPolyMeshData* vbs, aiSubmeshData* ibs);

 public:
    Abc::P3fArraySamplePtr m_points_sp, m_points_sp2;
    Abc::V3fArraySamplePtr m_velocities_sp;
    AbcGeom::IN3fGeomParam::Sample m_normals_sp, m_normals_sp2;
    AbcGeom::IV2fGeomParam::Sample m_uv0_sp, m_uv0_sp2;
    AbcGeom::IV2fGeomParam::Sample m_uv1_sp, m_uv1_sp2;
    AbcGeom::IC4fGeomParam::Sample m_rgba_sp, m_rgba_sp2;
    AbcGeom::IC3fGeomParam::Sample m_rgb_sp, m_rgb_sp2;
    Abc::Box3d m_bounds;

    IArray<abcV3> m_points_ref;
    IArray<abcV3> m_velocities_ref;
    IArray<abcV2> m_uv0_ref, m_uv1_ref;
    IArray<abcV3> m_normals_ref;
    IArray<abcV4> m_tangents_ref;
    IArray<abcC4> m_rgba_ref;
    IArray<abcC3> m_rgb_ref;

    RawVector<abcV3> m_points, m_points2, m_points_int, m_points_prev;
    RawVector<abcV3> m_velocities;
    RawVector<abcV2> m_uv0, m_uv02, m_uv0_int;
    RawVector<abcV2> m_uv1, m_uv12, m_uv1_int;
    RawVector<abcV3> m_normals, m_normals2, m_normals_int;
    RawVector<abcV4> m_tangents;
    RawVector<abcC4> m_rgba, m_rgba2, m_rgba_int;
    RawVector<abcC3> m_rgb, m_rgb2, m_rgb_int;

    TopologyPtr m_topology;
    bool m_topology_changed = false;

    std::future<void> m_async_copy;
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
    aiPolyMesh(aiObject* parent, const abcObject& abc);
    ~aiPolyMesh() override;
    void updateSummary();
    const aiMeshSummaryInternal& getSummary() const;

    Sample* newSample() override;
    void readSampleBody(Sample& sample, uint64_t idx) override;
    void cookSampleBody(Sample& sample) override;

    void onTopologyChange(aiPolyMeshSample& sample);
    void onTopologyDetermined();

 public:
    RawVector<abcV3> m_constant_points;
    RawVector<abcV3> m_constant_velocities;
    RawVector<abcV3> m_constant_normals;
    RawVector<abcV4> m_constant_tangents;
    RawVector<abcV2> m_constant_uv0;
    RawVector<abcV2> m_constant_uv1;
    RawVector<abcC4> m_constant_rgba;
    RawVector<abcC3> m_constant_rgb;

 private:
    aiMeshSummaryInternal m_summary;
    AbcGeom::IV2fGeomParam m_uv1_param;
    AbcGeom::IC4fGeomParam m_rgba_param;
    AbcGeom::IC3fGeomParam m_rgb_param;

    TopologyPtr m_shared_topology;
    abcFaceSetSchemas m_facesets;
    bool m_varying_topology = false;
};
