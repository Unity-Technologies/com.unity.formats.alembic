#pragma once
#include "../Foundation/aiMeshOps.h"

using abcFaceSetSchemas = std::vector<AbcGeom::IFaceSetSchema>;
using abcFaceSetSamples = std::vector<AbcGeom::IFaceSetSchema::Sample>;

class Topology
{
public:
    Topology();
    void clear();

    int getTriangulatedIndexCount() const;
    int getSplitCount() const;

    int getSplitVertexCount(int split_index) const;
    int getSubmeshCount() const;
    int getSubmeshCount(int split_index) const;

    void onTopologyUpdate(const aiConfig &config, aiPolyMeshSample& sample);
    RawVector<int>& getOffsets();

public:
    Abc::Int32ArraySamplePtr m_indices_orig;
    Abc::Int32ArraySamplePtr m_counts;
    RawVector<int> m_offsets;
    RawVector<int> m_material_ids;

    MeshRefiner m_refiner;
    RawVector<int> m_remap_points, m_remap_normals, m_remap_uv0, m_remap_uv1, m_remap_colors;

    int m_triangulated_index_count = 0;
    bool m_freshly_read_topology_data = false;
    bool m_use_32bit_index_buffer = false;
};
using TopologyPtr = std::shared_ptr<Topology>;


class aiPolyMeshSample : public aiSampleBase
{
using super = aiSampleBase;
using schema_t = aiPolyMesh;
public:
    aiPolyMeshSample(aiPolyMesh *schema, TopologyPtr topo, bool ownTopo );

    void updateConfig(const aiConfig &config, bool &topology_changed, bool &data_changed) override;
    
    bool hasNormals() const;
    bool hasUVs() const;
    bool hasVelocities() const;
    bool hasTangents() const;
    bool computeNormalsRequired() const;
    bool computeTangentsRequired() const;

    void getSummary(bool force_refresh, aiMeshSampleSummary &summary, aiPolyMeshSample* sample) const;
    void getDataPointer(aiPolyMeshData &data) const;

    void interpolatePoints();
    void computeNormals(const aiConfig &config);
    void interpolateNormals();
    void computeTangents(const aiConfig &config);

    void prepareSplits();
    int getSplitVertexCount(int split_index) const;
    void fillSplitVertices(int split_index, aiPolyMeshData &data);

    int getSubmeshCount(int split_index) const;
    void getSubmeshSummary(int split_index, int submesh_index, aiSubmeshSummary &summary);
    void fillSubmeshIndices(int split_index, int submesh_index, aiSubmeshData &data) const;

public:
    Abc::P3fArraySamplePtr m_points_orig, m_points_orig2;
    Abc::V3fArraySamplePtr m_velocities_orig;
    AbcGeom::IN3fGeomParam::Sample m_normals_orig, m_normals_orig2;
    AbcGeom::IV2fGeomParam::Sample m_uv0_orig, m_uv1_orig;
    AbcGeom::IC4fGeomParam::Sample m_colors_orig;
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
    Sample* newSample();
    Sample* readSample(const uint64_t idx, bool &topology_changed) override;

    void getSummary(aiMeshSummary &summary) const;

private:
    std::unique_ptr<AbcGeom::IV2fGeomParam> m_uv1_param;
    std::unique_ptr<AbcGeom::IC4fGeomParam> m_colors_param;

    TopologyPtr m_shared_topology;
    abcFaceSetSchemas m_facesets;
    AbcGeom::IN3fGeomParam::Sample m_constant_normals;
    AbcGeom::IV2fGeomParam::Sample m_constant_uv0;
    AbcGeom::IV2fGeomParam::Sample m_constant_uv1;
    AbcGeom::IC4fGeomParam::Sample m_constant_colors;

    bool m_ignore_normals = false;
    bool m_ignore_uvs = false;
};
