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
    int getAllSubmeshCount();

    int getSplitVertexCount(int split_index) const;
    int getSubmeshCount(int split_index) const;

public:
    Abc::Int32ArraySamplePtr m_indices_orig;
    Abc::Int32ArraySamplePtr m_counts;

    RawVector<int> m_indices; // triangulated
    RawVector<int> m_material_ids;

    MeshRefiner m_refiner;

    bool m_freshly_read_topology_data = false;
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
    bool computeNormalsRequired() const;
    bool computeTangentsRequired() const;

    void getSummary(bool force_refresh, aiMeshSampleSummary &summary, aiPolyMeshSample* sample) const;
    void getDataPointer(aiPolyMeshData &data) const;
    void copyData(aiPolyMeshData &data);

    void computeNormals(const aiConfig &config);
    void computeTangents(const aiConfig &config, const abcV3 *N, bool Nindexed);

    int getSplitVertexCount(int split_index) const;
    void fillSplitVertices(int split_index, aiPolyMeshData &data);

    int getAllSubmeshCount() const;
    int getSubmeshCount(int split_index) const;
    void getSubmeshSummary(int split_index, int submesh_index, aiSubmeshSummary &summary);
    void fillSubmeshIndices(int split_index, int submesh_index, aiSubmeshData &data) const;

public:
    Abc::P3fArraySamplePtr m_points;
    Abc::P3fArraySamplePtr m_next_points;
    Abc::V3fArraySamplePtr m_velocities;
    AbcGeom::IN3fGeomParam::Sample m_normals_orig;
    AbcGeom::IV2fGeomParam::Sample m_uvs_orig;
    Abc::Box3d m_bounds;
    abcFaceSetSamples m_facesets;

    TopologyPtr m_topology;
    bool m_own_topology = false;

    RawVector<abcV2> m_uvs;
    RawVector<abcV3> m_normals;
    RawVector<abcV4> m_tangents;
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
    void generateSplitAndSubmeshes(aiPolyMeshSample *sample) const;

private:
    bool m_ignore_normals = false;
    bool m_ignore_uvs = false;

    TopologyPtr m_shared_topology;
    AbcGeom::IN3fGeomParam::Sample m_constant_normals;
    AbcGeom::IV2fGeomParam::Sample m_constant_uvs;
    abcFaceSetSchemas m_facesets;
};
